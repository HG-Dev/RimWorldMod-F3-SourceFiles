using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace HG.FFF
{
    /// <summary>
    /// Used on recipes to specify a Thing that the target should
    /// become upon bill completion.
    /// </summary>
    [UsedImplicitly, PublicAPI]
    public class RecipeTransformableExtension : DefModExtension
    {
        public ThingDef thingToBecome;
        public QualityCategory quality;
    }
}