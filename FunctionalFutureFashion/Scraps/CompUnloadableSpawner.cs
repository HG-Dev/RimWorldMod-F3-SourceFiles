#if false
#define DEBUG

using System;
using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HG.FFF
{
    public class CompUnloadableSpawner : ThingComp, IThingHolder, IOpenable
    {
        public const string SIGNAL_WORKING = "UnloadableIsWorking";
        public const string SIGNAL_CANCEL_WORKING = "UnloadableStoppedWorking";
        public const string SIGNAL_UNLOAD_READY = "UnloadableIsReady";
        public const string SIGNAL_CANCEL_UNLOAD_READY = "UnloadableNoLongerReady";
        public const string SIGNAL_CAPACITY_FILLED = "UnloadableAtCapacity";
        public const string SIGNAL_CANCEL_CAPACITY_FILLED = "UnloadableCanSpawn";
        public const string SIGNAL_NEW_PRODUCT = "UnloadableProduced";
        public const string SIGNAL_MAX_LIFETIME = "UnloadableMaxLifetime";

        public static readonly CachedTexture EjectIcon = new CachedTexture("UI/Commands/PodEject");
        
        public delegate bool TryStartSequence(int spawnIndex);
        public delegate bool TryNextProgress(int elapsedTicks, int totalTicksUntilSpawn, int spawnIndex);
        private CompProperties_UnloadableSpawner Props => (CompProperties_UnloadableSpawner)this.props;
        
        private int ticksElapsed;
        private int totalThingsSpawned;
        private bool unloadingEnabled = true;
        private ThingOwner<Thing> output;

        public static CompUnloadableSpawner CreateBackupVoidSpawner(ThingWithComps thingWithComps)
        {
            #if DEBUG
            Log.Warning($"Creating void UnloadableSpawner for {thingWithComps.def.defName}. Verify its comp list");
            #endif
            var spawner = new CompUnloadableSpawner()
            {
                parent = thingWithComps,
                props = new CompProperties_UnloadableSpawner()
                {
                    hideGizmos = true
                }
            };
            spawner.props.ResolveReferences(null);
            spawner.output = new(spawner);
            return spawner;
        }

        [Unsaved]
        private Sustainer _audio;
        [Unsaved] 
        private byte _speed = 1;
        
        [Unsaved]
        private bool _sufficientAmountForUnload;
        public bool SufficientAmountForUnload
        {
            get
            {
                var stackCount = output.TotalStackCountOfDef(Props.thingToSpawn);
                var prevValue = _sufficientAmountForUnload;
                _sufficientAmountForUnload = stackCount >= UnityEngine.Mathf.Max(Props.minBeforeUnload, 1);
                if (_sufficientAmountForUnload != prevValue)
                {
#if DEBUG
                    Log.Message("[HG] SufficientAmountForUnload changed. Contains " + stackCount);
#endif
                    if (_sufficientAmountForUnload) parent.BroadcastCompSignal(SIGNAL_UNLOAD_READY);
                    else parent.BroadcastCompSignal(SIGNAL_CANCEL_UNLOAD_READY);
                }
                
                return _sufficientAmountForUnload;
            }
        }

        [Unsaved]
        private bool _filledToCapacity;
        public bool FilledToCapacity
        {
            get
            {
                var stackCount = output.TotalStackCountOfDef(Props.thingToSpawn);
                var prevValue = _filledToCapacity;
                _filledToCapacity = stackCount >= Props.MaxContained;
                if (prevValue != _filledToCapacity)
                {
#if DEBUG
                    Log.Message($"FTC change: {stackCount} vs. {Props.MaxContained}");
#endif
                    if (_filledToCapacity) parent.BroadcastCompSignal(SIGNAL_CAPACITY_FILLED);
                    else parent.BroadcastCompSignal(SIGNAL_CANCEL_CAPACITY_FILLED);
                }

                return _filledToCapacity;
            }
        }

        [Unsaved]
        private bool _working;
        public bool Working
        {
            get
            {
                return _working;
            }
            private set
            {
                if (_working != value)
                {
                    _working = value;
                    parent.BroadcastCompSignal(value ? SIGNAL_WORKING : SIGNAL_CANCEL_WORKING);
                }    
            }
        }

        public bool GizmosHidden => Props.hideGizmos;
        public int MaxContained => Props.MaxContained;
        public int CurrentContainedCount => output.TotalStackCountOfDef(Props.thingToSpawn);
        public bool ShouldUnload => unloadingEnabled && SufficientAmountForUnload;
        public float ProgressToNextBatchSpawn => ticksElapsed / (float)CurrentTicksPerSpawn;
        public int TotalThingsSpawned => totalThingsSpawned;
        public float LifetimeSlowdownFactor => Props.CalculateLifetimeSlowdownProgress(TotalThingsSpawned);
        public int CurrentTicksPerSpawn => Mathf.Max(1, Props.spawnIntervalRange.Lerped(LifetimeSlowdownFactor));

        public TryNextProgress TryStep = CannotValidate;
        public TryStartSequence TryStart = CannotValidate;

        private static bool CannotValidate(int _, int __, int ___) => false;
        private static bool CannotValidate(int _) => false;

        public int TicksUntilItemComplete => CurrentTicksPerSpawn - ticksElapsed;

        public override void Initialize(CompProperties initProps)
        { 
            output ??= new ThingOwner<Thing>(this)
            {
               dontTickContents = true,
            };
            base.Initialize(initProps);
        }

        public override void CompTickRare()
        {
            var currentTicksPerSpawn = CurrentTicksPerSpawn;
            
                      // Stop when filled to capacity
            Working = !FilledToCapacity 
                      // Stop if start request is denied
                      && (ticksElapsed > 0 || TryStart(totalThingsSpawned))
                      // Stop if step request is denied
                      && TryStep(ticksElapsed + GenTicks.TickRareInterval, currentTicksPerSpawn, totalThingsSpawned);
            
            if (!Working)
                return;
            
            ticksElapsed += GenTicks.TickRareInterval * _speed;
            if (ticksElapsed >= currentTicksPerSpawn)
            {
                // Try spawning product(s) into output ThingOwner
                var spawned = output.TryAdd(ThingMaker.MakeThing(Props.thingToSpawn, Props.thingStuff), Props.spawnBatchCount);
                totalThingsSpawned += spawned;
                if (spawned > 0)
                {
                    Props.soundComplete.PlayOneShot(SoundInfo.InMap(parent));
                    parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
                    if (totalThingsSpawned >= Props.spawnSlowdownRange.max)
                        parent.BroadcastCompSignal(SIGNAL_MAX_LIFETIME);
                }
#if DEBUG
                else
                {
                    Log.Warning($"Attempted to spawn products for {nameof(CompUnloadableSpawner)} on {parent.def.defName}, but container did not accept batch");
                }
#endif
                    
                ticksElapsed = 0;
                return;
            }

            // Play "working" loop
            if (_audio == null || _audio.Ended)
            {
                _audio = Props.soundWorking.TrySpawnSustainer(SoundInfo.InMap(parent, MaintenanceType.PerTickRare));
            }
        }

        /// <summary>
        /// If the batch isn't essentially finished already, cancel its production and give the
        /// 0-to-1 progress value identifying how close it was to completion.
        /// </summary>
        /// <returns>Whether ticksElapsed was reset and Working was set to 'false'.</returns>
        public bool TryCancelSpawnInProgress(out float finalProgress)
        {
            finalProgress = ProgressToNextBatchSpawn;
            if (Mathf.Approximately(finalProgress, 1))
                return false; // If the spawn is essentially finished, allow it to occur.
            Working = false;
            ticksElapsed = 0;
            return true;
        }
        
        private void DevTrySpawnSingle()
        {
            if (!output.TryAdd(ThingMaker.MakeThing(Props.thingToSpawn, Props.thingStuff))) 
                return;
            
            totalThingsSpawned++;
            parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
            _ = SufficientAmountForUnload;
        }

        private void DevTrySpawnBatch()
        {
            var spawned = output.TryAdd(ThingMaker.MakeThing(Props.thingToSpawn, Props.thingStuff), Props.spawnBatchCount);
            if (spawned <= 0)
                return;
            
            totalThingsSpawned += spawned;
            parent.BroadcastCompSignal(SIGNAL_NEW_PRODUCT);
            _ = SufficientAmountForUnload;
        }

        public List<Thing> EjectProductsNearby() => EjectProductsNearby(parent.MapHeld, parent.InteractionCell);
        public List<Thing> EjectProductsNearby(Map map, IntVec3 position)
        {
            if (output.NullOrEmpty())
            {
                SoundDefOf.FloatMenu_Cancel.PlayOneShot(SoundInfo.InMap(parent));
#if DEBUG
                Log.Warning($"Attempted to eject products of {nameof(CompUnloadableSpawner)} on {parent.def.defName}, but container was empty");
#endif
                return new List<Thing>();
            }
            
            map ??= parent.Map;
            var stacks = new List<Thing>(output.Count);
            output.TryDropAll(position, map, ThingPlaceMode.Near, (thing, i) => stacks.Add(thing));
            _ = FilledToCapacity;
            _ = SufficientAmountForUnload;
            return stacks;
        }

        public AcceptanceReport CanPawnUnload(Pawn pawn)
        {
            var reachable = pawn.CanReach(parent, Verse.AI.PathEndMode.Touch, Danger.Deadly);
            if (!reachable)
                return "CannotReach".Translate();

            if (SufficientAmountForUnload)
            {
                if (!unloadingEnabled)
                    return "ForbiddenLower".Translate();

                if (parent.IsBurning())
                    return "BurningLower".Translate();

                return true;
            }

            if (Working)
            {
                if (Props.minBeforeUnload > 1)
                    return "FFF.BatchInProgress".Translate(Props.minBeforeUnload);

                return "FermentationProgress".Translate(ProgressToNextBatchSpawn.ToStringPercent(),
                    Props.minBeforeUnload - CurrentContainedCount);
            }

            return "InactiveFacility".Translate();
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            _audio?.End();
            EjectProductsNearby(map, parent.Position);
            base.PostDeSpawn(map, mode);
        }

        public override void PostExposeData()
        {
            var cct = CurrentContainedCount;
            Scribe_Values.Look(ref cct, "cct");
            Scribe_Values.Look(ref this.totalThingsSpawned, "tts");
            Scribe_Values.Look(ref this.unloadingEnabled, "ue", true);

            var diff = cct - CurrentContainedCount;
            if (diff > 0)
            {
                output.TryAdd(ThingMaker.MakeThing(Props.thingToSpawn, Props.thingStuff), diff);
#if DEBUG
                Log.Message($"Added {diff} product to {nameof(CompUnloadableSpawner)} on {parent.def.defName} following XML read");
#endif
            }

            _ = FilledToCapacity;
            _ = SufficientAmountForUnload;
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (Props.hideGizmos)
                yield break;
            
            var cct = CurrentContainedCount;
            yield return new Command_Toggle
            {
                defaultLabel = "FFF.ToggleUnloading".Translate(),
                defaultDesc = "FFF.ToggleUnloadingDesc".Translate(),
                isActive = () => this.unloadingEnabled,
                toggleAction = () => this.unloadingEnabled = !this.unloadingEnabled,
                activateSound = SoundDefOf.Tick_Tiny,
                icon = unloadingEnabled ? TexCommand.ForbidOff : TexCommand.ForbidOn
            };
            if (cct >= 1)
            {
                yield return new Command_Action
                {
                    defaultLabel = "FFF.EjectContents".Translate(),
                    defaultDesc = "FFF.EjectContentsDesc".Translate(Find.ActiveLanguageWorker.Pluralize(Props.thingToSpawn.label, cct)),
                    action = () => EjectProductsNearby(),
                    activateSound = SoundDefOf.Tick_Tiny,
                    icon = EjectIcon.Texture
            };
            }

            if (DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DEV: +1 " + this.Props.thingToSpawn.label,
                    icon = TexCommand.Install,
                    action = DevTrySpawnSingle
                };
                var batchCount = Props.spawnBatchCount;
                if (batchCount > 1)
                {
                    yield return new Command_Action
                    {
                        defaultLabel = $"DEV: +{batchCount} {Props.thingToSpawn.label}",
                        icon = TexCommand.Install,
                        action = DevTrySpawnBatch
                    };
                }

                yield return new Command_ActionWithLimitedUseCount()
                {
                    defaultLabel = "DEV: Speed",
                    defaultDesc = "Set speed scale (in multiples of two)",
                    maxUsesGetter = () => 16,
                    usesLeftGetter = () => _speed,
                    action = () =>
                    {
                        _speed *= 2;
                        if (_speed > 16 || _speed == 0)
                            _speed = 1;
                    },
                    icon = TexCommand.Draft
                };
            }
        }

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
        }

        public ThingOwner GetDirectlyHeldThings() => output;
        
        public void Open() => EjectProductsNearby();
        public bool CanOpen => SufficientAmountForUnload;
        public int OpenTicks => Props.thingExtractTicks;
    }
}
#endif