using System;
using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Verse;
// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace HG.FFF
{
    [StaticConstructorOnStartup]
    internal static class ResourceBank
    {
        [DefOf, PublicAPI]
        public static class ThingDefOf
        {
            public static ThingDef FFF_HypermeshPrinterAnyQuality;
            public static ThingDef FFF_HypermeshPrinterAncient;
            public static ThingDef FFF_HypermeshPrinterJuryRigged;
            public static ThingDef FFF_HypermeshPrinterRefurbished;
            public static ThingDef FFF_HypermeshPrinterModernized;
            [MayRequireBiotech] public static ThingDef FFF_HypermeshPrinterMechanite;
            public static ThingDef FFF_PilotSuit;
            public static ThingDef FFF_CrewSuit;
            public static ThingDef FFF_RegalSuit;
            public static ThingDef FFF_SealedProtosuit;
            public static ThingDef FFF_Hypermesh_Suit;
        }
        
        public static Lazy<List<ThingDef>> AllHypermeshPrinters = new(() => new List<ThingDef>()
        {
            ThingDefOf.FFF_HypermeshPrinterAncient, ThingDefOf.FFF_HypermeshPrinterJuryRigged,
            ThingDefOf.FFF_HypermeshPrinterRefurbished
        });
        
        // [DefOf]
        // public static class TerrainDefOf
        // {
        //     public static TerrainDef Sandstone_RoughHewn;
        //     //public static TerrainDef Sandstone_Rough;
        //     public static TerrainDef Limestone_RoughHewn;
        //     //public static TerrainDef Limestone_Rough;
        //     public static TerrainDef Slate_RoughHewn;
        //     //public static TerrainDef Slate_Rough;
        //     public static TerrainDef Marble_RoughHewn;
        //     //public static TerrainDef Marble_Rough;
        //
        //     public static TerrainDef RockTerrainFromThingDef(ThingDef thingDef, byte roughnessLevel = 1)
        //     {
        //         switch (thingDef.defName)
        //         {
        //             case "Limestone" when roughnessLevel is 1:
        //                 return Limestone_RoughHewn;
        //             case "Sandstone" when roughnessLevel is 1:
        //                 return Sandstone_RoughHewn;
        //             case "Marble" when roughnessLevel is 1:
        //                 return Marble_RoughHewn;
        //             case "Slate" when roughnessLevel is 1:
        //                 return Slate_RoughHewn;
        //             default:
        //                 throw new ArgumentOutOfRangeException($"No associated terrain for thingDef <{thingDef.defName}>");
        //         }
        //     }
        // }

        public static Apparel MakeAppropriateSuitForPawn(Pawn pawn)
        {
            // Skill
            var wardening = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Warden);
            var research = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Research);
            var crafting = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Crafting);
            var construction = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Construction);
            var hunting = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Hunting);
            var mining = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Mining);
            var medical = pawn.skills.AverageOfRelevantSkillsFor(WorkTypeDefOf.Doctor);

            KeyValuePair<ThingDef, float>[] options = new KeyValuePair<ThingDef, float>[4];
            options[0] = new(ThingDefOf.FFF_RegalSuit, Mathf.Max(wardening, research));
            options[1] = new(ThingDefOf.FFF_CrewSuit, Mathf.Max(construction, mining));
            options[2] = new(ThingDefOf.FFF_PilotSuit, Mathf.Max(hunting, crafting));
            options[3] = new(ThingDefOf.FFF_SealedProtosuit, medical);
            var best = options.MaxBy(pair => pair.Value).Key;

            return ThingMaker.MakeThing(best) as Apparel;
        }

        [DefOf, PublicAPI]
        public static class RecipeDefOf
        {
            public static RecipeDef AutoMake_Protosuit;
        }

        private static readonly NameTriple CreatorName = new("Ian", "Hagu", "Haguewood");

        public static Pawn CreateFederationSurvivor(PlanetTile location)
        {
            PawnKindDef pawnKind = PawnKindDefOf.SpaceRefugee;
            var faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Ancients);
            var request = new PawnGenerationRequest(pawnKind, faction,
                PawnGenerationContext.NonPlayer, location, forceGenerateNewPawn: true, allowDead: false, allowDowned: true,
                canGeneratePawnRelations: false, mustBeCapableOfViolence: false, 0, forceAddFreeWarmLayerIfNeeded: true, allowGay: true,
                allowPregnant: false, allowFood: true, allowAddictions: true, inhabitant: false, certainlyBeenInCryptosleep: true,
                worldPawnFactionDoesntMatter: false, biocodeWeaponChance: 1f, 
                prohibitedTraits: new []{ TraitDefOf.DislikesMen, TraitDefOf.DislikesWomen, TraitDefOf.Nudist, TraitDefOf.BodyPurist, TraitDefOf.Pyromaniac },
                biologicalAgeRange: new FloatRange(20, 50), forceRecruitable: true);

            var creator = !CreatorName.UsedThisGame;
            if (creator)
            {
                request.AllowGay = false;
                request.MustBeCapableOfViolence = true;
                request.FixedGender = Gender.Male;
            }

            var pawn = PawnGenerator.GeneratePawn(request);
            if (creator)
                pawn.Name = CreatorName;

            pawn.apparel.Wear(MakeAppropriateSuitForPawn(pawn), false);
            
            return pawn;
        }
    }
}
