using RimWorld;
using RimWorld.SketchGen;
using System.Collections.Generic;
using Verse;

namespace HG.FFF
{
    public class SketchResolver_FedScoutWreck : SketchResolver
    {
        public static CellRect Size => CellRect.FromLimits(IntVec3.Zero, new IntVec3(30, 0, 20));

        protected override bool CanResolveInt(SketchResolveParams parms)
        {
            return parms.sketch != null;
        }

        protected override void ResolveInt(SketchResolveParams parms)
        {
            //var origin = new IntVec3(146, 0, 120);
            Sketch sketch = new Sketch();
            sketch.AddThing(ThingDefOf.Ship_Beam, new IntVec3(10, 0, 9), Rot4.East);
            sketch.AddThing(ThingDefOf.Ship_Beam, new IntVec3(17, 0, 9), Rot4.East);
            for (int y = 4; y < 15; y++)
                sketch.AddThing(ThingDefOf.PowerConduit, new IntVec3(14, 0, y), Rot4.North);
            sketch.AddThing(ThingDefOf.AncientDisplayBank, new IntVec3(6, 0, 6), Rot4.North);
            sketch.AddThing(ThingDefOf.Table1x2c, new IntVec3(6, 0, 11), Rot4.East, ThingDefOf.Steel, quality: QualityCategory.Poor);
            sketch.AddThing(ThingDefOf.AncientLamp, new IntVec3(5, 0, 11), Rot4.South);
            sketch.AddThing(ThingDefOf.AncientMachine, new IntVec3(20, 0, 1), Rot4.East);
            sketch.AddThing(ThingDefOf.Door, new IntVec3(14, 0, 15), Rot4.South);
            sketch.AddThing(ThingDefOf.Door, new IntVec3(14, 0, 11), Rot4.South);

            if (UnityEngine.Random.value > 0.5f)
                sketch.AddThing(ThingDefOf.Door, new IntVec3(8, 0, 7), Rot4.South);
            else
                sketch.AddThing(ThingDefOf.Door, new IntVec3(8, 0, 10), Rot4.South);

            if (UnityEngine.Random.value > 0.5f)
                sketch.AddThing(ThingDefOf.Door, new IntVec3(20, 0, 7), Rot4.East);
            else
                sketch.AddThing(ThingDefOf.Door, new IntVec3(20, 0, 10), Rot4.East);

            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(9, 0, 11), Rot4.East);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(11, 0, 11), Rot4.East);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(16, 0, 11), Rot4.East);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(18, 0, 11), Rot4.East);

            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(9, 0, 6), Rot4.West);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(11, 0, 6), Rot4.West);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(16, 0, 6), Rot4.West);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.Ship_CryptosleepCasket, new(18, 0, 6), Rot4.West);

            for (int x = 5; x <= 27; x++)
                for (int y = 6; y <= 11; y++)
                    if (UnityEngine.Random.value > 0.1f)
                        sketch.AddTerrain(TerrainDefOf.MetalTile, new(x, 0, y));

            for (int x = 0; x <= 4; x++)
                for (int y = 7; y <= 10; y++)
                    if (UnityEngine.Random.value > 0.5f)
                        sketch.AddTerrain(TerrainDefOf.MetalTile, new(x, 0, y));


            foreach (var (pos, thing, spawnChance) in GetChanceSingleSpawnPositions())
            {
                if (UnityEngine.Random.value < spawnChance)
                {
                    sketch.AddTerrain(TerrainDefOf.MetalTile, pos);
                    sketch.AddThing(thing, pos, Rot4.North);
                }
                else if (spawnChance > 0.55f)
                {
                    sketch.AddThing(ThingDefOf.ChunkSlagSteel, pos, Rot4.Random);
                }  
            }

            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.AncientSpacerCrate, new(4, 0, 10), Rot4.West);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.AncientSpacerCrate, new(15, 0, 4), Rot4.West);
            if (UnityEngine.Random.value > 0.55f)
                sketch.AddThing(ThingDefOf.AncientSpacerCrate, new(13, 0, 16), Rot4.West);

            sketch.AddThing(ThingDefOf.Turret_AutoChargeBlaster, new(18, 0, 11), Rot4.South);
            parms.sketch.Merge(sketch, true);
        }

        private static IEnumerable<(IntVec3 pos, ThingDef thing, float distPercent)> GetChanceSingleSpawnPositions()
        {
            var thing = ModLister.CheckOdyssey("Ancient launch pad") ? ThingDefOf.GravshipHull : ThingDefOf.Wall;
            var center = new IntVec3(14, 0, 8);
            var effectiveRadius = 20f;
            var y = 12;
            var x = 4;
            var pos = new IntVec3(x, 0, y);
            for (x = 4; x <= 9; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 15; x <= 28; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
                
            y = 5;
            for (x = 4; x <= 9; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 15; x <= 28; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (y = 6; y <= 11; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            pos = new IntVec3(4, 0, 11);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(8, 0, 11);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(13, 0, 11);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(15, 0, 11);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(20, 0, 11);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(4, 0, 6);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(8, 0, 6);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(13, 0, 6);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(15, 0, 6);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            pos = new IntVec3(20, 0, 6);
            yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            for (x = 0, y = 10; x <= 4; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 0, y = 7; x <= 4; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 0, y = 8; y <= 9; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 12, y = 2; y <= 4; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 16, y = 2; y <= 4; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 12, y = 13; y <= 15; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 16, y = 13; y <= 15; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 12, y = 2; y <= 4; y++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
            for (x = 18, y = 16; x <= 20; x++)
            {
                pos = new IntVec3(x, 0, y);
                yield return new(pos, thing, 1 - pos.DistanceTo(center) / effectiveRadius);
            }
        }
    }
}
