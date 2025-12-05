using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    // public enum PrinterQuality : byte
    // {
    //     Defunct,
    //     Ancient,
    //     JuryRigged,
    //     Refurbished,
    //     Optimized,
    //     Modernized,
    //     Mechanite
    // }
    
    public static class ThingDefExtensions
    {
        public static IReadOnlyList<IngredientCount> GetFixedIngredientsOrDefault(this ThingDef thingDef)
        {
            var list = thingDef?.building?.subcoreScannerFixedIngredients ?? new List<IngredientCount>();
            foreach (var ingredient in list)
                ingredient.ResolveReferences(); // Necessary to set allowed defs / is fixed ingredient
            return list;
        }

        public static bool TryConsumeRecipeIngredients(this ThingOwner thingOwner, RecipeDef recipe)
        {
            var quotas = recipe.ingredients.Select(QuotaState.FromIngredientCount).ToArray();
            var quotasMet = 0;
            
            // Consume ingredients
            for (int i = 0; i < quotas.Length; i++)
            {
                var ingredient = recipe.ingredients[i];
                foreach (var ownedIngredient in thingOwner)
                {
                    var potentialIngredientDef = ownedIngredient.def;
                    if (!ingredient.filter.AllowedThingDefs.Contains(potentialIngredientDef))
                        continue;

                    var valuePerUnit = recipe.IngredientValueGetter.ValuePerUnitOf(potentialIngredientDef);
                    quotas[i] += Mathf.RoundToInt(ownedIngredient.stackCount * valuePerUnit);
                    ownedIngredient.stackCount = quotas[i].AmountExcess;
                    if (quotas[i].IsMet)
                    {
                        quotasMet++;
                        break; // Jump to next ingredient
                    }
                }
            }

            thingOwner.RemoveAll(thing => thing.stackCount <= 0);
            return quotas.Length == quotasMet;
        }

        public static List<Thing> AddLoot(this ThingOwner container, List<IngredientCount> lootSrc, FloatRange lootCountScalar, int seed)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            var lootSpawned = 0;
            var lootCount = lootSrc.Count; 
            var results = new List<Thing>(lootCount);
            
            for (int i = 0; i < lootCount; i++)
            {
                var fixedIngredient = lootSrc[i].FixedIngredient;
                var fixedIngredientCount = Mathf.FloorToInt(lootSrc[i].GetBaseCount());
                IntRange countRange = new IntRange(Mathf.FloorToInt(fixedIngredientCount * lootCountScalar.min), Mathf.CeilToInt(fixedIngredientCount * lootCountScalar.max));
                var lootScale = Verse.Rand.RangeSeeded(0f, 1f, seed + i);
                var stackCount = countRange.Lerped(lootScale);
                if (stackCount < 1)
                    continue;
                
                var lootThing = ThingMaker.MakeThing(fixedIngredient);
                lootThing.stackCount = countRange.Lerped(lootScale);
                if (container.TryAdd(lootThing))
                {
                    results.Add(lootThing);
                    lootSpawned++;
                }
                else
                    Log.Error($"Failed to add {fixedIngredient.label}x{countRange.Lerped(lootScale)} to a container");
            }

            return results;
        }

        private static readonly Color[] QualityColors = new Color[]
        {
            new Color32(150, 125, 68, byte.MaxValue),
            new Color(0.6f, 0.6f, 0.6f),
            new Color(0.8f, 0.85f, 1f),
            new Color32(31, 212, 49, byte.MaxValue),
            new Color32(76, 210, 242, byte.MaxValue),
            new Color32(198, 103, 237, byte.MaxValue),
            new Color32(212, 198, 30, byte.MaxValue)
        };
        public static UnityEngine.Color ToColor(this QualityCategory quality)
        {
            return QualityColors[(byte)quality];
        }
    }
}