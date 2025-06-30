using RimWorld;
using System.Text;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    public class Building_HypermeshPrinter : Building
    {
        [Unsaved]
        private CompBreakdownable _breakComp;
        public CompBreakdownable BreakComp => _breakComp ?? (_breakComp = GetComp<CompBreakdownable>());

        [Unsaved]
        private CompPowerTrader _powerComp;
        public CompPowerTrader Power => _powerComp ?? (_powerComp = GetComp<CompPowerTrader>());

        [Unsaved]
        private CompRefuelable _fuelComp;
        public CompRefuelable FuelComp => _fuelComp ?? (_fuelComp = GetComp<CompRefuelable>());

        private const string REFUELED = "Refueled";

        [Unsaved]
        private CompUnloadableSpawner _unloadComp;
        public CompUnloadableSpawner Unloadable => _unloadComp ?? (_unloadComp = GetComp<CompUnloadableSpawner>());

        private const int FuelPerSpawnBatch = 50;
        private float fuelUsedByBatch;

        [Unsaved]
        private float? fuelAtStartOfBatch;

        public bool ReadyForHauling => Unloadable.ShouldUnload;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            Unloadable.StepValidator = TryUseFuelIfPowered;
            FuelComp.TargetFuelLevel = FuelPerSpawnBatch;
            if (Unloadable.TotalThingsSpawned + Unloadable.ProgressToNextBatchSpawn < float.Epsilon)
                fuelAtStartOfBatch = FuelComp.Fuel;
            overrideGraphicIndex = overrideGraphicIndex ?? 0;
        }

        public override bool IsWorking()
        {
            return Unloadable.Working;
        }

        public bool TryUseFuelIfPowered(int elapsedTicks, int totalTicksUntilSpawn, int batchIndex)
        {
            if (Power == null || !Power.PowerOn)
                return false;

            if (FuelComp == null || !FuelComp.HasFuel)
                return false;

            if (FuelComp.Fuel < (FuelPerSpawnBatch - fuelUsedByBatch))
                return false;

            elapsedTicks = Mathf.Max(0, elapsedTicks);
            float fuelUsedPreviously = fuelUsedByBatch;
            fuelUsedByBatch = ((float)elapsedTicks / totalTicksUntilSpawn) * FuelPerSpawnBatch;

            Log.Message($"{batchIndex}:{totalTicksUntilSpawn} -- {elapsedTicks} = {fuelUsedByBatch}");

            // Decrease contained fuel, approaching FuelPerSpawn
            FuelComp.ConsumeFuel(fuelUsedByBatch - fuelUsedPreviously);

            return true;
        }

        public bool SetFilledGraphic(bool filled)
        {
            var previousIndex = overrideGraphicIndex;
            overrideGraphicIndex = filled ? 1 : 0;
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
            switch (signal)
            {
                case REFUELED:
                    fuelAtStartOfBatch = null;
                    break;
                case CompUnloadableSpawner.SIGNAL_WORKING: // MAXIMUM POWER
                    Power.PowerOutput = -Power.Props.PowerConsumption;
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_Text, 1f, false, 0f);
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_WORKING: // idle power
                    Power.PowerOutput = -Power.Props.idlePowerDraw;
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_Stun, 1f, false, 0f);
                    break;
                case CompUnloadableSpawner.SIGNAL_NEW_PRODUCT:
                    var remainingFuelToConsume = FuelPerSpawnBatch - fuelUsedByBatch;
                    FuelComp.ConsumeFuel(remainingFuelToConsume);

                    if (fuelAtStartOfBatch.HasValue)
                        Debug.Assert(Mathf.Approximately(fuelAtStartOfBatch.Value - FuelPerSpawnBatch, FuelComp.Fuel),
                            $"[HG] Fuel levels should be at {fuelAtStartOfBatch.Value - FuelPerSpawnBatch}");

                    fuelUsedByBatch = 0;
                    fuelAtStartOfBatch = FuelComp.Fuel;
                    //MoteMaker.MakeStaticMote(this.DrawPos, base.MapHeld, ThingDefOf.Mote_IncineratorBurst, 1f, false, 0f);
                    if (Unloadable.LifetimeSlowdownFactor > 0)
                        BreakComp.DoBreakdown();
                    // Show product graphic
                    break;
                case CompUnloadableSpawner.SIGNAL_CAPACITY_FILLED:
                    // Show blinking light?
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_CAPACITY_FILLED:
                    // Show two green blinking lights
                    break;
                case CompUnloadableSpawner.SIGNAL_UNLOAD_READY:
                    SetFilledGraphic(true);
                    // Show product graphic with green light
                    break;
                case CompUnloadableSpawner.SIGNAL_CANCEL_UNLOAD_READY:
                    SetFilledGraphic(false);
                    // Show product graphic with red light if product exists,
                    // hide product graphic if none exist
                    break;
            }
        }

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder(base.GetInspectString());

            // STATUS
            if (Unloadable.Working)
            {
                var progress = Unloadable.ProgressToNextBatchSpawn;
                stringBuilder.Append("\n" + "FermentationProgress".Translate(progress.ToStringPercent(), 
                    Unloadable.TicksUntilItemComplete.ToStringTicksToPeriod(true, false, true, true, false)));
            }
            else if (Unloadable.FilledToCapacity)
            {
                stringBuilder.Append($"\nPrinting paused: requires unloading ({Unloadable.MaxContained} max)");
            }
            else if (BreakComp.BrokenDown)
            {
                stringBuilder.Append($"\nPrinting paused: replace toner cartridge");
            }

            // STATS
            if (Unloadable.TotalThingsSpawned > Unloadable.CurrentContainedCount)
            {
                stringBuilder.Append($"\n{Unloadable.TotalThingsSpawned} total dispensed");
            }
            if (DebugSettings.ShowDevGizmos)
            {
                var amt = Unloadable.TicksUntilItemComplete < CompUnloadableSpawner.DEFAULT_TOTAL_TICKS_UNTIL_SPAWN 
                    ? Unloadable.TicksUntilItemComplete.ToString() 
                    : "Waiting to set";
                stringBuilder.Append($"\n{amt} ticks until complete");
            }

            var output = stringBuilder.ToString().Split('\n');
            for (int i = 0; i < output.Length; i++)
            {
                if (string.IsNullOrEmpty(output[i]))
                    Log.Error("Found an empty line in output -- " + i);
            }

            return stringBuilder.ToString();
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref fuelUsedByBatch, "fUsed", 0, false);
            base.ExposeData();
        }
    }
}
