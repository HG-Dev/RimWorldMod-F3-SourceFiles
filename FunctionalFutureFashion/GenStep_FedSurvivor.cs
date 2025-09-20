// using JetBrains.Annotations;
// using RimWorld;
// using Verse;
//
// namespace HG.FFF
// {
//     [PublicAPI]
//     public class GenStep_FedSurvivor : GenStep_Scatterer
//     {
//         public override int SeedPart => 652156;
//         
//         protected override bool CanScatterAt(IntVec3 loc, Map map)
//         {
//             return loc.Walkable(map);
//         }
//
//         protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
//         {
//             Log.Message("[HG] Scattering survivor at " + loc.ToString());
//             if ((parms.sitePart?.things?.Any ?? false) 
//                 && parms.sitePart.things.Take(parms.sitePart.things[0]) is Pawn singlePawnToSpawn)
//             {
//                 // Okay
//             }
//             else if (map.Parent.GetComponent<FedSurvivorComp>() is { } comp && comp.pawn.Any)
//             {
//                 Log.Warning("Site part did not receieve pawn as parameter, using FedSurvivorComp");
//                 singlePawnToSpawn = comp.pawn.Take(comp.pawn[0]);
//             }
//             else
//             {
//                 Log.Message("[HG] Needed to use DownedRefugeeQuestUtility fallback. :(");
//                 return;
//                 // singlePawnToSpawn = DownedRefugeeQuestUtility.GenerateRefugee(map.Tile, PawnKindDefOf.SpaceRefugee);
//                 // var suit = ResourceBank.MakeAppropriateSuitForPawn(singlePawnToSpawn);
//                 // singlePawnToSpawn.apparel.Wear(suit);
//             }
//
//             if (singlePawnToSpawn.MapHeld != map)
//             {
//                 Log.Error("Survivor gen step noticed the pawn to spawn wasn't on the correct map");
//             }
//             
//             singlePawnToSpawn.SetPositionDirect(loc);
//         }
//     }
// }
