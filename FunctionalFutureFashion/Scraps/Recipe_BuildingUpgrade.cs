#if false
using System.Collections.Generic;
using Verse;

namespace HG.FFF
{
    public class Recipe_BuildingUpgrade : RecipeWorker
    {
        public override void ConsumeIngredient(Thing ingredient, RecipeDef recipe, Map map)
        {
            Log.Message($"Consume ingredient {ingredient.ToStringSafe()} for {recipe.defName}");
            base.ConsumeIngredient(ingredient, recipe, map);
        }

        public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
        {
            Log.Message($"Notify iteration completed by {billDoer.NameFullColored} using {ingredients.ToStringSafeEnumerable()}");
            base.Notify_IterationCompleted(billDoer, ingredients);
        }
    }
}
#endif