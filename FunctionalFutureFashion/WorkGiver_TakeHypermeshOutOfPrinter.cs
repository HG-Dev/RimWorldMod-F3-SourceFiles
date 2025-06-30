using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace HG.FFF
{
    public class WorkGiver_UnloadHypermeshPrinter : RimWorld.WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(ResourceBank.ThingDefOf.HypermeshPrinter);

        // Temporarily disabled while figuring out
        public override bool ShouldSkip(Pawn pawn, bool forced = false)
        {
            foreach (var thing in PotentialWorkThingsGlobal(pawn))
            {
                if (thing.TryGetComp<CompUnloadableSpawner>(out var unloadable))
                {
                    if (unloadable.ShouldUnload && pawn.Position.DistanceTo(thing.Position) < 128)
                        return false;
                }
            }
            return true;
        }

        // Filter out applicable buildings early for some measure of performance
        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            return pawn.Map.listerBuildings.AllBuildingsColonistOfDef(ResourceBank.ThingDefOf.HypermeshPrinter);
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            var canReserve = pawn.CanReserve(t, 1, -1, null, forced);
            if (canReserve && t is Building_HypermeshPrinter printer)
            {
                var report = printer.Unloadable.CanPawnUnload(pawn);
                if (!report.Accepted)
                {
                    Log.Error(report.Reason);
                    JobFailReason.Is(report.Reason, null);
                }

                return report.Accepted;
            }
            return false;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(ResourceBank.JobDefOf.HaulFromUnloadable, t, expiryInterval: 800);
        }
    }
}
