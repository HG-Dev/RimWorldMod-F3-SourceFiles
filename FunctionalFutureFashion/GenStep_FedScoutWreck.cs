// using RimWorld;
// using RimWorld.SketchGen;
// using Verse;
//
// namespace HG.FFF
// {
//     public class GenStep_FedScoutWreck : GenStep_ScattererBestFit
//     {
//         private LayoutStructureSketch sketch;
//         public override int SeedPart => 254651365;
//
//         public override bool CollisionAt(IntVec3 cell, Map map)
//         {
//             throw new System.NotImplementedException();
//         }
//         
//         public override void Generate(Map map, GenStepParams parms)
//         {
//             count = 1;
//             nearMapCenter = true;
//             sketch = parms.sitePart.parms.ancientLayoutStructureSketch;
//             if (sketch?.structureLayout == null)
//             {
//                 //TryRecoverEmptySketch(parms);
//             }
//             base.Generate(map, parms);
//         }
//
//         protected override bool CanScatterAt(IntVec3 c, Map map)
//         {
//             return true;
//             // if (!base.base.CanScatterAt(c, map) || !c.Standable(map) || c.Roofed(map) || !map.reachability.CanReachMapEdge(c, TraverseParms.For(TraverseMode.PassDoors)))
//             //     return false;
//             // CellRect cellRect = new CellRect(c.x - GenStep_EscapeShip.EscapeShipSizeWidth.min / 2, c.z - GenStep_EscapeShip.EscapeShipSizeHeight.min / 2, GenStep_EscapeShip.EscapeShipSizeWidth.min, GenStep_EscapeShip.EscapeShipSizeHeight.min);
//             // if (!cellRect.FullyContainedWithin(new CellRect(0, 0, map.Size.x, map.Size.z)))
//             //     return false;
//             // foreach (IntVec3 c1 in cellRect)
//             // {
//             //     TerrainDef terrainDef = map.terrainGrid.TerrainAt(c1);
//             //     if (!c1.GetAffordances(map).Contains(TerrainAffordanceDefOf.Heavy) && (terrainDef.driesTo == null || !terrainDef.driesTo.affordances.Contains(TerrainAffordanceDefOf.Heavy)))
//             //         return false;
//             // }
//             // return true;
//         }
//
//         protected override IntVec2 Size { get; }
//
//         protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
//         {
//             Log.Message("[HG] Spawning FedScoutWreck");
//             SketchResolver_FedScoutWreck sketcher = new SketchResolver_FedScoutWreck();
//             var sketchParams = new SketchResolveParams();
//             sketchParams.sketch = new Sketch();
//             sketcher.Resolve(sketchParams);
//             sketchParams.sketch.Spawn(map, c, Faction.OfAncients, forceTerrainAffordance: true, buildRoofsInstantly: true);
//         }
//     }
// }
//
// /*
// using RimWorld.BaseGen;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using Verse;
// using Verse.AI.Group;
// using RimWorld.Planet;
// using RimWorld;
//
// namespace SaveOurShip2
// {
//     class GenStep_DownedShip : GenStep_Scatterer
//     {
//
//         public override int SeedPart
//         {
//             get
//             {
//                 return 694201337;
//             }
//         }
//
//         protected override bool CanScatterAt(IntVec3 c, Map map)
//         {
//             return true;
//         }
//
//         protected override void ScatterAt(IntVec3 c, Map map, GenStepParams stepparams, int stackCount = 1)
//         {
//             List<Building> cores = new List<Building>();
//             int rarity = Rand.RangeInclusive(1, 2);
//             //limited to 100x100 due to unsettable map size, no fleets
//             ShipDef ship = DefDatabase<ShipDef>.AllDefs.Where(def => def.ships.NullOrEmpty() && !def.neverRandom && !def.spaceSite && !def.neverWreck && def.rarityLevel <= rarity && def.sizeX < 100 && def.sizeZ < 100).RandomElement();
//             ShipInteriorMod2.GenerateShip(ship, map, null, Faction.OfAncients, null, out cores, false, true, 4, (map.Size.x - ship.sizeX) / 2, (map.Size.z - ship.sizeZ) / 2);
//         }
//     }
// }
// */