using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace HG.FFF
{
    /// <summary>
    /// Essentially, a "mech gestator" for anything thing that can be created via recipe.
    /// If def.skyfaller.speedCurve is defined, this curve is used to determine a "lifetime processing speed",
    /// and at the end of the curve, the "EndOfLifetime" signal is broadcast.
    /// </summary>
    /// <remarks>
    /// Sound parameters
    /// building.openingStartedSound = Begin forming
    /// building.soundDoorOpenManual = Insert ingredient(s)
    /// subcoreScannerWorking = work sustained?
    /// building.soundDispense = Complete forming
    /// </remarks>
    [StaticConstructorOnStartup, UsedImplicitly]
    public class Building_ThingMaker : Building_WorkTableAutonomous
    {
        public const string ProductTag = "ThingMakerProduct";
        public const string EndOfLifetimeSignal = "EndOfLifetime";
        
        private byte _speed = 1;
        private Sustainer workingSound;
        private CompPowerTrader _powerComp;
        private bool _noDefinedRecipes;
        public CompPowerTrader Power => _powerComp ??= GetComp<CompPowerTrader>();

        private int _thingsMade;
        private List<Action<Thing>> _productWatchers = new();
        
        private bool UseLifetimeWorkSpeed => def.skyfaller?.speedCurve != null;
        public float CurrentWorkSpeed => _speed * (UseLifetimeWorkSpeed ? def.skyfaller.speedCurve.Evaluate(_thingsMade) : 1);


        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            innerContainer.OnContentsChanged += RejectUnfinishedThingsInsideContainer;
            _noDefinedRecipes = !def.AllRecipes.Any();
        }

        private void RejectUnfinishedThingsInsideContainer()
        {
            var unfinishedThing = innerContainer.FirstOrDefault(t => t is UnfinishedThing);
            if (unfinishedThing is not null)
                innerContainer.TryDrop(unfinishedThing, ThingPlaceMode.Direct, out _);
        }

        public override void Notify_StartForming(Pawn billDoer)
        {
            base.Notify_StartForming(billDoer);
            var soundToPlay = def.building.openingStartedSound ?? SoundDefOf.CryptosleepCasket_Accept;
            soundToPlay.PlayOneShot(this);
        }
        
        public override void Notify_FormingCompleted()
        {
            var soundToPlay = def.building.soundDispense ?? SoundDefOf.CryptosleepCasket_Eject;
            soundToPlay.PlayOneShot(this);
            
            innerContainer.TryConsumeRecipeIngredients(activeBill.recipe);
            _thingsMade++;

            if (!activeBill.recipe.products.Any() 
                || !UseLifetimeWorkSpeed
                || _thingsMade < Mathf.RoundToInt(def.skyfaller.speedCurve.Points.Last().x))
                return;
            
            // Set up EndOfLifetime watcher
            var firstProduct = activeBill.recipe.products.First();
            Action<Thing> watcherInstance = ProductWatcher;
            _productWatchers.Add(watcherInstance);
            MapHeld.thingListChangedCallbacks.onThingAdded += watcherInstance;

            void ProductWatcher(Thing addedThing)
            {
                if (firstProduct.thingDef != addedThing.def || firstProduct.count != addedThing.stackCount) 
                    return;
                
                foreach (var watcher in _productWatchers)
                    MapHeld.thingListChangedCallbacks.onThingAdded -= watcher;
                Log.Warning(EndOfLifetimeSignal);
                BroadcastCompSignal(EndOfLifetimeSignal);
            }
        }

        public override void Notify_HauledTo(Pawn hauler, Thing thing, int count)
        {
            base.Notify_HauledTo(hauler, thing, count);
            var soundToPlay = def.building.soundDoorOpenManual ?? SoundDefOf.Artillery_ShellLoaded;
            soundToPlay.PlayOneShot(this);
        }

        public override bool CanWork()
        {
            // If any non autonomous bills exist, stop processing
            var found = billStack.Bills.FirstOrDefault(b => b is not Bill_Autonomous);
            if (found != null)
            {
                return false;
            }
                
            return billStack.Count > 0;
        }

        void TickRareInternal()
        {
            if (Power == null)
                return;
            
            // Zero power during upgrades
            if (!ReferenceEquals(billStack.FirstShouldDoNow, activeBill)
                && billStack.FirstShouldDoNow is Bill_ProductionWithUft)
            {
                Power.PowerOutput = 0;
                return;
            }

            this.overrideGraphicIndex = activeBill is { State: >= FormingState.Formed } ? 1 : 0;
            
            // Bill tick
            switch (activeBill)
            {
                case null:
                    break;
                case {State: not FormingState.Forming}:
                    Power.PowerOutput = -Power.Props.idlePowerDraw;
                    break;
                case {State: FormingState.Forming} when CanWork():
                    Power.PowerOutput = -Power.Props.PowerConsumption;
                    activeBill.formingTicks -= (GenTicks.TickRareInterval - 1) * CurrentWorkSpeed;
                    activeBill.BillTick();
                    break;
            }
        }

        protected override void Tick()
        {
            base.Tick();

            if (CanWork())
                // Regurgitate an amount of ticks based on current speed
                activeBill.formingTicks += 1 - CurrentWorkSpeed;
            
            if (!this.IsHashIntervalTick(250))
                return;

            TickRareInternal();
        }

        public override void TickRare()
        {
            base.TickRare();

            TickRareInternal();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref _thingsMade, "tmc");
        }
        
        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            string compString = InspectStringPartsFromComps(); 
            if (!compString.NullOrEmpty())
                sb.Append(compString);

            string inspectStringExtra = GetInspectStringExtra();
            if (!inspectStringExtra.NullOrEmpty())
                sb.AppendInNewLine(inspectStringExtra);

            var correctBill = CanWork();
            if (correctBill)
                if (activeBill != null)
                    InspectModifiedFormingTicks(activeBill, sb, CurrentWorkSpeed);
#if VERBOSE
                else
                    sb.AppendInNewLine(("Null bill"));
            else
                sb.AppendInNewLine($"Non-automated bill found or no AnyShouldDoNow: {billStack.AnyShouldDoNow}");
#endif
            
            return sb.ToString().TrimEndNewlines();
        }
        
        public static void InspectModifiedFormingTicks(Bill_Autonomous bill, StringBuilder sb, float scalar)
        {
            sb.AppendLineIfNotEmpty();
            if (bill.State == FormingState.Forming || bill.State == FormingState.Preparing)
                sb.AppendTagged("FinishesIn".Translate() + ": " + ((int) (bill.formingTicks / scalar)).ToStringTicksToPeriod());
            if (bill.State != FormingState.Formed)
                return;
            sb.AppendLine("Finished".Translate());
        }

        // TODO: check if Biotech translations are unavailable in core
        protected override string GetInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder(base.GetInspectStringExtra());
            
            if (_noDefinedRecipes)
            {
                sb.AppendLineIfNotEmpty();
                sb.Append("FFF.NoRecipes".Translate());
                return sb.ToString();
            }
            
            sb.Append("FFF.ProductionState".Translate(_thingsMade, CurrentWorkSpeed.ToStringPercent()));

            if (activeBill is { State: >= FormingState.Forming })
            {
                if (DebugSettings.ShowDevGizmos)
                    sb.Append($" {activeBill.formingTicks}");
                return sb.ToString();
            }
            
            if (innerContainer.TotalStackCount > 0)
                sb.AppendInNewLine(string.Join(", ", innerContainer.Select(t => t.LabelCap)));
            
            return sb.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos())
                yield return gizmo;
            
            if (DebugSettings.ShowDevGizmos)
            {
                if (billStack.AnyShouldDoNow)
                {
                    yield return new Command_Action()
                    {
                        defaultLabel = "DEV: Spawn ingredients",
                        defaultDesc =
                            "Create the ingredients necessary for the first active bill and store them internally",
                        icon = TexCommand.Install,
                        action = () =>
                        {
                            if (billStack.FirstShouldDoNow is { } bill)
                            {
                                var ingredients = new List<IngredientCount>();
                                bill.MakeIngredientsListInProcessingOrder(ingredients);
                                innerContainer.AddLoot(ingredients, FloatRange.One, 1);
                            }
                        }
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

        protected override void DrawAt(Vector3 drawLoc, bool flip = false)
        {
            base.DrawAt(drawLoc, flip);
            GenDraw.FillableBarRequest barDrawData = BarDrawData;
            barDrawData.center = drawLoc;
            barDrawData.fillPercent = CurrentBillFormingPercent;
            barDrawData.filledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.98f, 0.46f, 0f));
            barDrawData.unfilledMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0f, 0f, 0f, 0f));
            barDrawData.rotation = base.Rotation;
            GenDraw.DrawFillableBar(barDrawData);
        }
    }
}