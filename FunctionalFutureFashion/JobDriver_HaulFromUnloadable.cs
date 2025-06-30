using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace HG.FFF
{
    public class JobDriver_HaulFromUnloadable : JobDriver
    {
        private const TargetIndex UnloadableIndex = TargetIndex.A;
        private const TargetIndex ProtomeshIndex = TargetIndex.B;
        private const TargetIndex StorageIndex = TargetIndex.C;

        private const int ExtractDuration = 200;
        protected Thing UnloadableThing => job.GetTarget(UnloadableIndex).Thing;
        private CompUnloadableSpawner Unloadable;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!UnloadableThing.TryGetComp(out Unloadable))
            {
                if (errorOnFailed)
                    Log.Error($"Failed to reserve {job.GetTarget(UnloadableIndex).ToStringSafe()} for unload job because it lacks a {nameof(CompUnloadableSpawner)}");
                return false;
            }
            var report = Unloadable.CanPawnUnload(pawn);
            if (!report.Accepted)
            {
                if (errorOnFailed)
                    Log.Error($"[HG] Printer unload failed: {report.Reason}. WorkGiver should be modified to avoid this situation.");
                return false;
            }

            return pawn.Reserve(UnloadableThing, job, 1, -1, null, errorOnFailed, false);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(UnloadableIndex, PathEndMode.Touch);
            yield return Toils_General.Wait(ExtractDuration, UnloadableIndex)
                .FailOnDespawnedNullOrForbidden(UnloadableIndex)
                .FailOnBurningImmobile(UnloadableIndex)
                .FailOn(() => !Unloadable.ShouldUnload)
                .WithProgressBarToilDelay(UnloadableIndex);
            Toil toil = ToilMaker.MakeToil("TakeUnloadableProduct");
            toil.initAction = () =>
            {
                var stacks = Unloadable.EjectContentsNearby();
                if (stacks.Count < 1)
                    EndJobWith(JobCondition.Incompletable);

                var primaryThing = stacks[0];
                StoragePriority currentPriority = StoreUtility.CurrentStoragePriorityOf(primaryThing);
                if (StoreUtility.TryFindBestBetterStoreCellFor(primaryThing, pawn, Map, currentPriority, pawn.Faction, out IntVec3 cell, true))
                {
                    job.SetTarget(StorageIndex, cell);
                    job.SetTarget(ProtomeshIndex, primaryThing);
                    job.count = primaryThing.stackCount;
                    return;
                }
                EndJobWith(JobCondition.Incompletable);
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            yield return Toils_Reserve.Reserve(ProtomeshIndex);
            yield return Toils_Reserve.Reserve(StorageIndex);
            yield return Toils_Goto.GotoThing(ProtomeshIndex, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(ProtomeshIndex);
            Toil carryToCell = Toils_Haul.CarryHauledThingToCell(StorageIndex, PathEndMode.ClosestTouch);
            yield return carryToCell;
            yield return Toils_Haul.PlaceHauledThingInCell(StorageIndex, carryToCell, true, false);
        }
    }
}
