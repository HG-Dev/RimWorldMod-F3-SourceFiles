#if false
#define UNITY_ASSERTIONS
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace HG.FFF
{
    /// <summary>
    /// Should have two interaction cells: one for unloading, and one for ingredient insertion.
    /// Ejecting ingredients will eject from the ingredient-side of the device if it is walkable or a thing holder;
    /// otherwise, it will eject all over itself.
    /// </summary>
    /// <remarks>
    /// While powered, 
    /// </remarks>
    public sealed class Building_HypermeshPrinter_Old : Building, IThingHolder, IOpenable
    {
        private const int MAX_QUEUED_JOBS = 3;
        private const string LOOTABLE_TAG = "Lootable";
        public static readonly CachedTexture QueueOneIcon = new CachedTexture("UI/Gizmos/icons_queue");
        public static readonly CachedTexture QueueInfiniteIcon = new CachedTexture("UI/Gizmos/icons_repeat");
        public static readonly CachedTexture StopIcon = new CachedTexture("UI/Gizmos/icons_stop");
        public static readonly CachedTexture EjectIngredientsIcon = new CachedTexture("UI/Designators/EjectFuel");
        
        [Unsaved]
        private CompPowerTrader _powerComp;
        public CompPowerTrader Power => _powerComp ??= GetComp<CompPowerTrader>() ?? new CompPowerTrader()
        {
            parent = this,
            powerOutputInt = -500,
            props = new CompProperties_Power()
            {
                idlePowerDraw = -50
            }
        };
        
        [Unsaved]
        private CompUnloadableSpawner _unloadComp;
        public CompUnloadableSpawner Unloadable => _unloadComp ??=
            GetComp<CompUnloadableSpawner>() ?? CompUnloadableSpawner.CreateBackupVoidSpawner(this);

        [Unsaved] 
        private CompPostPostMakeLoot postPostMakeLoot;
        public CompPostPostMakeLoot UpgradeBench => postPostMakeLoot ?? GetComp<CompPostPostMakeLoot>();

        [Unsaved] private IReadOnlyList<IngredientCount> _ingredientCache;
        private IReadOnlyList<IngredientCount> IngredientCache =>
            _ingredientCache ??= this.def.GetFixedIngredientsOrDefault();

        private byte queuedJobCount;
        private bool infiniteJobs;
        private ThingOwner<Thing> ingredientHolder;
        
        [Unsaved]
        private bool _debugDisableNeedForIngredients;
        [Unsaved] 
        private (int ticks, Pawn pawn, ThingCount nextIngredent)? _nextFetchedIngredientCache = null;
        [Unsaved] 
        private bool _isUpgradingCache;

        public byte JobsQueued => infiniteJobs ? (byte)2 : queuedJobCount;
        public bool ReadyForHauling => Unloadable.ShouldUnload;
        public bool ReadyForSingleJob => GetIngredientQuotasForSingleJob().All(iq => iq.quota.IsMet);
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            Log.Message("SpawnSetup for hypermesh printer");
            
            ingredientHolder ??= new (this)
            {
                dontTickContents = true,
                contentsLookMode = LookMode.Deep
            };
            
            base.SpawnSetup(map, respawningAfterLoad);
            
            Unloadable.TryStep = TryProductionStep;
            Unloadable.TryStart = TryProductionStart;
            overrideGraphicIndex ??= 0;
        }

        // Need to use PostPostMake so that the Spawn completes
        public override void PostPostMake()
        {
            ingredientHolder ??= new (this)
            {
                dontTickContents = true
            };
            
            
            base.PostPostMake();
        }

        public override bool IsWorking()
        {
            return _unloadComp != null && _unloadComp.Working;
        }

        // public override void TickRare()
        // {
        //     if (_isUpgradingCache) // Set false if no upgrades exist @ position
        //         _isUpgradingCache = Position.GetThingList(Map)
        //             .ContainsAny((thing) => Upgrader.AllUpgrades.Contains(thing.def.entityDefToBuild));
        //     base.TickRare();
        // }

        private bool TryProductionStart(int batchIndex)
        {
            if (Power is not { PowerOn: true }) return false;
            if (JobsQueued is 0) return false;
            
            var recipe = GetIngredientQuotasForSingleJob().ToArray();
            // Check quota
            for (int i = 0; i < recipe.Length; i++)
            {
                if (!recipe[i].quota.IsMet)
                    return false;
            }
            // Consume ingredients
            for (int i = 0; i < recipe.Length; i++)
            {
                var ingredient = recipe[i].ingredient;
                var quota = recipe[i].quota.Zeroed;
                foreach (var held in ingredientHolder)
                {
                    if (held.def != ingredient)
                        continue;

                    quota += held.stackCount;
                    held.stackCount = quota.AmountExcess;
                    if (quota.IsMet) 
                        break; // Jump to next ingredient
                }
            }

            if (queuedJobCount >= 1)
                queuedJobCount--;
            
            return true;
        }

        private bool TryProductionStep(int elapsedTicks, int totalTicksUntilSpawn, int batchIndex)
        {
            //Log.Message($"TryProductionStep: {elapsedTicks}/{totalTicksUntilSpawn} -- {batchIndex} -- POWER ON: {Power is {PowerOn: true}}");
            return Power is { PowerOn: true };   
        }

        private bool SetOutputCanBeRetrievedGraphic(bool outputInTray)
        {
            var previousIndex = overrideGraphicIndex;
            overrideGraphicIndex = outputInTray ? 1 : 0;
            var didChange = previousIndex != overrideGraphicIndex;
            if (didChange)
                DirtyMapMesh(Map);

#if DEBUG
            if (Graphic is Graphic_Indexed indexedGraphic)
            {
                Log.Message("[HG] Subgraphic count: " + indexedGraphic.SubGraphicsCount);
                for (int i = 0; i < indexedGraphic.SubGraphicsCount; i++)
                {
                    if (indexedGraphic.SubGraphicAtIndex(i) is Graphic_Multi multiGraphic)
                    {
                        Log.Message($"[HG] SubgraphicMulti {i}: {multiGraphic.GraphicPath}, sdr {multiGraphic.ShouldDrawRotated}, wf {multiGraphic.WestFlipped} ef {multiGraphic.EastFlipped}, ");
                    }
                    var subgraph = indexedGraphic.SubGraphicAtIndex(i);
                }
            }
#endif
            
            return didChange;
        }

        protected override void ReceiveCompSignal(string signal)
        {
            Log.Message(signal);
            switch (signal)
            {
                case CompUnloadableSpawner.SIGNAL_WORKING: // MAXIMUM POWER
                    Power.PowerOutput = -Power.Props.PowerConsumption;
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_Text, 1f, false, 0f);
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_WORKING: // idle power
                    Power.PowerOutput = -Power.Props.idlePowerDraw;
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_Stun, 1f, false, 0f);
                    break;
                case CompUnloadableSpawner.SIGNAL_NEW_PRODUCT:
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_Text);
                    break;
                case CompUnloadableSpawner.SIGNAL_CAPACITY_FILLED:
                    // Show blinking light?
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_CAPACITY_FILLED:
                    // Was previously filled to capacity; no longer is. Show two green blinking lights
                    break;
                case CompUnloadableSpawner.SIGNAL_UNLOAD_READY:
                    SetOutputCanBeRetrievedGraphic(true);
                    if (Unloadable.ShouldUnload)
                        MapHeld.designationManager.AddDesignation(new Designation(this, DesignationDefOf.Open));
                    // Show product graphic with green light
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_UNLOAD_READY:
                    SetOutputCanBeRetrievedGraphic(false);
                    MapHeld.designationManager.TryRemoveDesignationOn(this, DesignationDefOf.Open);
                    // Show product graphic with red light if product exists,
                    // hide product graphic if none exist
                    break;
                // case CompSpawnReplacementBlueprint.SIGNAL_BLUEPRINT_PLACED:
                //     _isUpgradingCache = true;
                //     Power.PowerOn = false;
                //     Unloadable.EjectProductsNearby();
                //     StopAndEjectIngredients();
                //     break;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());

            if (_isUpgradingCache)
            {
                stringBuilder.AppendInNewLine("FFF.DownForMaintenance".Translate());
                return stringBuilder.ToString();
            }

            if (Unloadable.GizmosHidden)
            {
                stringBuilder.AppendInNewLine("BrokenDown".Translate());
                return stringBuilder.ToString();
            }

            // STATUS
            foreach (var (ingredient, quota) in GetIngredientQuotasForSingleJob())
            {
                stringBuilder.AppendInNewLine($" - {ingredient.LabelCap} {quota.Amount} / {quota.AmountRequired}");
            }

            if (Unloadable.Working)
            {
                var progress = Unloadable.ProgressToNextBatchSpawn;
                if (queuedJobCount < 1)
                {
                    stringBuilder.AppendInNewLine("FFF.PrintingProgress".Translate(progress.ToStringPercent(), 
                        Unloadable.TicksUntilItemComplete.ToStringTicksToPeriod()));
                }
                else if (infiniteJobs)
                {
                    stringBuilder.AppendInNewLine("FFF.PrintingProgressRunIndefinitely".Translate(progress.ToStringPercent(), 
                        Unloadable.TicksUntilItemComplete.ToStringTicksToPeriod()));
                }
                else
                {
                    stringBuilder.AppendInNewLine("FFF.PrintingProgressJobsQueued".Translate(JobsQueued, progress.ToStringPercent(), 
                        Unloadable.TicksUntilItemComplete.ToStringTicksToPeriod()));
                }
            }
            else if (JobsQueued > 0)
            {
                if (!Power.PowerOn)
                {
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append(
                        "FFF.PrintingPaused".Translate(
                            "NoPower".Translate()));
                }
                else if (Unloadable.FilledToCapacity)
                {
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append(
                        "FFF.PrintingPaused".Translate(
                            "FFF.FilledToCapacity".Translate(Unloadable.MaxContained)));
                }
                else if (GetIngredientQuotasForSingleJob().Any(q => !q.quota.IsMet))
                {
                    stringBuilder.AppendLineIfNotEmpty();
                    stringBuilder.Append(
                        "FFF.PrintingPaused".Translate(
                            "FFF.NoIngredients".Translate()));
                }
            }

            // STATS
            if (Unloadable.TotalThingsSpawned > Unloadable.CurrentContainedCount)
            {
                stringBuilder.AppendInNewLine($"{Unloadable.TotalThingsSpawned} total dispensed");
            }
            if (DebugSettings.ShowDevGizmos)
            {
                stringBuilder.AppendInNewLine($"{Unloadable.TicksUntilItemComplete} ticks until complete");
            }

            #if DEBUG
            var output = stringBuilder.ToString().Split('\n');
            for (int i = 0; i < output.Length; i++)
            {
                if (string.IsNullOrEmpty(output[i]))
                    Log.ErrorOnce("Found an empty line in output -- " + i, typeof(Building_HypermeshPrinter_Old).GetHashCode());
            }
            #endif

            return stringBuilder.ToString();
        }
        
        // Note: Recipe count defines a selection of ingredients or one fixed ingredient and their required amount.
        public IEnumerable<(ThingDef ingredient, QuotaState quota)> GetIngredientQuotasForSingleJob()
        {
            if (_debugDisableNeedForIngredients)
                    yield break;
            
            foreach (var recipePart in IngredientCache)
            {
                var count = ingredientHolder.TotalStackCountOfDef(recipePart.FixedIngredient);
                yield return new (recipePart.FixedIngredient, 
                    new(count, Mathf.FloorToInt(recipePart.GetBaseCount())));
            }
        }

        public IEnumerable<(ThingDef ingredient, QuotaState quota)> GetIngredientQuotasForQueuedJobs()
        {
            if (_debugDisableNeedForIngredients || JobsQueued < 1)
                yield break;
            
            foreach (var recipePart in IngredientCache)
            {
                var count = ingredientHolder.TotalStackCountOfDef(recipePart.FixedIngredient);
                yield return new (recipePart.FixedIngredient, 
                    new(count, Mathf.FloorToInt(recipePart.GetBaseCount() * JobsQueued)));
            }
        }
        
        public AcceptanceReport CanPawnAddIngredient(Pawn pawn, out ThingCount nextIngredient)
        {
            nextIngredient = new ThingCount(this, 0);
            
            if (_nextFetchedIngredientCache.HasValue 
                && _nextFetchedIngredientCache.Value.pawn == pawn 
                && GenTicks.TicksGame - _nextFetchedIngredientCache.Value.ticks < GenTicks.TicksPerRealSecond)
            {
                // Use cache for job scans
                nextIngredient = _nextFetchedIngredientCache.Value.nextIngredent;
            }
            
            _nextFetchedIngredientCache = null;
            if (nextIngredient.Thing != this) // Return early if cache was valid
                return true;
            
            var reachable = pawn.CanReach(this, Verse.AI.PathEndMode.Touch, Danger.Deadly);
            if (!reachable)
                return "CannotReach".Translate();

            if (JobsQueued < 1)
                return "FFF.QueueJobFirst".Translate();

            var missing = GetIngredientQuotasForQueuedJobs()
                .Where(iq => !iq.quota.IsMet)
                .OrderBy(iq => iq.quota.PercentageMet)
                .ToArray();

            if (missing.Length < 1)
            {
                return "FFF.SufficientIngredients".Translate(JobsQueued + 1);
            }

            var searchRange = Mathf.Max(256, Map.Size.LengthHorizontal * 0.5f);
            foreach (var (ingredient, quota) in missing)
            {
                var pickupCount = Mathf.Min(quota.AmountMissing, ingredient.stackLimit);
                
                bool Validator(Thing thing)
                {
                    if (thing.IsForbidden(pawn)) return false;
                    if (pickupCount > thing.stackCount * 4) return false;
                    return pawn.CanReserve(thing, 1, pickupCount > thing.stackCount ? -1 : pickupCount);
                }

                var stack = GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForDef(ingredient),
                    PathEndMode.ClosestTouch, TraverseParms.For(pawn, Danger.None), searchRange, Validator);

                if (stack != null)
                {
                    nextIngredient = new ThingCount(stack, Mathf.Min(stack.stackCount, pickupCount));
                    _nextFetchedIngredientCache = new(GenTicks.TicksGame, pawn, nextIngredient);
                    return true;
                }
            }

            return "FFF.IngredientsScatteredOrInsufficient".Translate();
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;

            if (Unloadable.GizmosHidden)
                yield break;
            
            // queue print job + load ingredients, toggle run indefinitely, stop, then eject ingredients
            var sb = new StringBuilder();
            var currentTicksPerBatch = Unloadable.CurrentTicksPerSpawn;
            if (queuedJobCount is 0)
                sb.Append(currentTicksPerBatch.ToStringTicksToPeriod(false, true, false, false));
            else
                sb.Append((Mathf.Min(MAX_QUEUED_JOBS, queuedJobCount + 1) * currentTicksPerBatch)
                    .ToStringTicksToPeriod(false, true, false, false));
            var queueCommand = new Command_ActionWithLimitedQueue()
            {
                defaultLabel = "FFF.QueuePrintJob".Translate(),
                defaultDesc = sb.ToString(),
                activateSound = SoundDefOf.Tick_Tiny,
                queueCapacityGetter = () => MAX_QUEUED_JOBS,
                queueCountGetter = () => infiniteJobs ? MAX_QUEUED_JOBS : queuedJobCount,
                action = () =>
                {
                    queuedJobCount = (byte)Mathf.Min(MAX_QUEUED_JOBS, (infiniteJobs ? 0 : queuedJobCount) + 1);
                    infiniteJobs = false;
                },
                icon = QueueOneIcon.Texture,
                alsoClickIfOtherInGroupClicked = true
            };
            queueCommand.UpdateQueueCapacity();
            queueCommand.Disabled |= _isUpgradingCache;
            yield return queueCommand;

            var toggleCommand = new Command_Toggle()
            {
                defaultLabel = "FFF.ToggleRunIndefinitely".Translate(),
                defaultDesc = "FFF.ToggleRunIndefinitelyDesc".Translate(),
                isActive = () => this.infiniteJobs,
                toggleAction = () =>
                {
                    this.infiniteJobs = !this.infiniteJobs;
                },
                activateSound = SoundDefOf.Tick_Tiny,
                icon = infiniteJobs ? StopIcon.Texture : QueueInfiniteIcon.Texture
            };
            toggleCommand.Disabled = _isUpgradingCache;
            yield return toggleCommand;

            if (ingredientHolder != null && ingredientHolder.Any())
            {
                var ejectCommand = new Command_Action()
                {
                    defaultLabel = "FFF.EjectIngredients".Translate(),
                    defaultDesc = "FFF.EjectIngredientsDesc".Translate(),
                    Disabled = ingredientHolder.NullOrEmpty(),
                    disabledReason = "NoIngredients".Translate(),
                    activateSound = SoundDefOf.CancelMode,
                    action = () =>
                    {
                        if (Unloadable.Working)
                            Find.WindowStack.Add(
                                Dialog_MessageBox.CreateConfirmation(
                                    "FFF.EjectIngredientsWarning".Translate(),
                                    StopAndEjectIngredients, true));
                    },
                    icon = EjectIngredientsIcon.Texture
                };
                yield return ejectCommand;
            }
            
            if (!DebugSettings.ShowDevGizmos)
                yield break;

            yield return new Command_Toggle()
            {
                defaultLabel = "DEV: Don't use ingredients",
                activateIfAmbiguous = false,
                activateSound = _debugDisableNeedForIngredients
                    ? SoundDefOf.Checkbox_TurnedOff
                    : SoundDefOf.Checkbox_TurnedOn,
                isActive = () => _debugDisableNeedForIngredients,
                toggleAction = () => _debugDisableNeedForIngredients = !_debugDisableNeedForIngredients,
                icon = _debugDisableNeedForIngredients ? TexCommand.ForbidOn : TexCommand.ForbidOff
            };
        }

        /// <summary>
        /// Expel all stocked ingredients plus theoretically remaining ingredients from an in-progress item spawn.
        /// Reset the spawner.
        /// </summary>
        private void StopAndEjectIngredients()
        {
            infiniteJobs = false;
            queuedJobCount = 0;
            var ejectSpot = InteractionCells.Last();
            
            // Remove empty stacks
            ingredientHolder.RemoveAll(t => t.stackCount < 1);
            
            // Expel stocked ingredients
            ingredientHolder.TryDropAll(ejectSpot, Map, ThingPlaceMode.Near);
            
            if (!Unloadable.TryCancelSpawnInProgress(out float progress))
                return;

            var unconsumed = Mathf.Clamp01(1 - progress);
            
            // If progress is greater than zero, we "reclaim" some of the ingredients
            foreach (var recipePart in IngredientCache)
            {
                var amt = Mathf.FloorToInt(recipePart.GetBaseCount() * unconsumed);
                if (amt < 1) continue;
                var stack = ThingMaker.MakeThing(recipePart.FixedIngredient);
                stack.stackCount = amt;
                GenPlace.TryPlaceThing(stack, ejectSpot, Map, ThingPlaceMode.Near);
            }
        }

        public override void DeSpawn(DestroyMode mode)
        {
            StopAndEjectIngredients();
            base.DeSpawn(mode);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref queuedJobCount, "qj");
            Scribe_Values.Look(ref infiniteJobs, "ij");
            Scribe_Deep.Look(ref this.ingredientHolder, "iC", new object[]
            {
                this
            });
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            outChildren.Add(Unloadable);
        }
        
        public ThingOwner GetDirectlyHeldThings()
        {
            return this.ingredientHolder;
        }

        public bool ApparelSourceEnabled => Unloadable.ShouldUnload;
        public void Open()
        {
            _unloadComp.Open();
        }

        public bool CanOpen => _unloadComp.CanOpen;

        public int OpenTicks => _unloadComp.OpenTicks;
    }
}
#endif
