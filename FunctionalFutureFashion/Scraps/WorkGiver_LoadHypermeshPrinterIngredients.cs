#if false
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace HG.FFF
{
    public class WorkGiver_LoadHypermeshPrinterIngredients : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest =>
            ThingRequest.ForGroup(ThingRequestGroup.PotentialBillGiver);
        
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            if (PotentialWorkThingsGlobal(pawn).EnumerableNullOrEmpty()) return true;

            return false;
        }

        // Filter out applicable buildings early for some measure of performance
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (var printerDef in ResourceBank.AllHypermeshPrinters.Value)
            {
                foreach (var device in pawn.Map.listerBuildings.AllBuildingsColonistOfDef(printerDef))
                {
                    yield return device;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var canReserve = pawn.CanReserve(t, 2, -1, null, forced);
            if (canReserve && t is Building_HypermeshPrinter_Old printer)
            {
                var report = printer.CanPawnAddIngredient(pawn, out _);
                if (!report.Accepted)
                {
                    JobFailReason.Is(report.Reason);
                }

                return report.Accepted;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (t is not Building_HypermeshPrinter_Old printer || !printer.CanPawnAddIngredient(pawn, out var next))
                return null;

            Job job = HaulAIUtility.HaulToContainerJob(pawn, next.Thing, t);
            job.count = next.Count;
            return job;
        }
    }
}
#endif