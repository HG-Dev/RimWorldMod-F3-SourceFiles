using RimWorld;
using Verse;

namespace HG.FFF
{
    public class CompProperties_PostPostMakeLoot : CompProperties
    {
        public CompProperties_PostPostMakeLoot()
        {
            compClass = typeof(CompPostPostMakeLoot);
        }

        public RecipeDef src;
    }
}