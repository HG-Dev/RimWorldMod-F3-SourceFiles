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

        public bool showContentsInInspectPane = true;
        public bool requiresPower = true;
        public ThingDef thingToSpawn;
        public ThingDef thingStuff = null;
        public IntRange spawnIntervalRange = new IntRange(250, 250);
        public int spawnBatchCount = 1;
        public int maxContained = 0;
        public int minBeforeUnload = 1;
        public IntRange spawnSlowdownRange = new IntRange(3, 20);
        public SoundDef soundWorking;

        public int MaxContained => maxContained > 0 ? maxContained : thingToSpawn.stackLimit;
        public bool HasUnloadMinLimit => minBeforeUnload > 1;
        public bool HasLifetimeSpawnLimit => spawnSlowdownRange.max > 0;

        public float CalculateLifetimeSlowdownProgress(int spawnedSoFar)
        {
            if (spawnSlowdownRange.max < 0)
                return 0;
            var origin = spawnedSoFar - spawnSlowdownRange.min;
            if (origin <= 0)
                return 0;
            return (float)origin / spawnSlowdownRange.max;
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            if (soundWorking == null) soundWorking = SoundDefOf.Interact_CleanFilth;
        }

    }
}