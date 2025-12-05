using Verse;

namespace HG.FFF
{
    public readonly struct RecipeDefExt
    {
        public RecipeDefExt(RecipeDef recipe)
        {
            Recipe = recipe;
            ExtInfo = recipe.GetModExtension<RecipeTransformableExtension>();
        }
        
        public readonly RecipeDef Recipe;
        public readonly RecipeTransformableExtension ExtInfo;
    }
}