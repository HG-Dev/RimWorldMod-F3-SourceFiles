#if false
using RimWorld;
using Verse;

namespace HG.FFF
{
    public class CompProperties_UnloadableSpawner : CompProperties
    {
        public CompProperties_UnloadableSpawner()
        {
            this.compClass = typeof(CompUnloadableSpawner);
        }

        public bool hideGizmos = false;
        public ThingDef thingToSpawn;
        public ThingDef thingStuff = null;
        public int thingExtractTicks = 200;
        public IntRange spawnIntervalRange = new IntRange(250, 2500);
        public int spawnBatchCount = 1;
        public int maxContained = 0;
        public int minBeforeUnload = 1;
        public IntRange spawnSlowdownRange = new IntRange(3, 20);
        public SoundDef soundWorking;
        public SoundDef soundComplete;

        public int MaxContained => maxContained > 0 ? maxContained : thingToSpawn.stackLimit;

        public float CalculateLifetimeSlowdownProgress(int spawnedSoFar)
        {
            if (spawnSlowdownRange.max < 0)
                return 0;
            var origin = spawnedSoFar - spawnSlowdownRange.min;
            if (origin <= 0)
                return 0;
            return (float)origin / spawnSlowdownRange.max;
        }

        public override void ResolveReferences(ThingDef _)
        {
            if (thingToSpawn == null) thingToSpawn = ThingDefOf.ChunkMechanoidSlag;
            if (soundWorking == null) soundWorking = SoundDefOf.Interact_Sow;
            if (soundComplete == null) soundComplete = SoundDefOf.CryptosleepCasket_Accept;
        }

    }
}
#endif