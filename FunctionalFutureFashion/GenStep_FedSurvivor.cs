using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace HG.FFF
{
    public class GenStep_FedSurvivor : GenStep_Scatterer
    {
        public override int SeedPart => 652156;


        protected override bool CanScatterAt(IntVec3 loc, Map map)
        {
            if (!base.CanScatterAt(loc, map)) return false;

            if (loc.SupportsStructureType(map, TerrainAffordanceDefOf.Heavy))
                return false;

            foreach (IntVec3 surroundingCell in CellRect.CenteredOn(loc, 8, 8))
            {
                if (!surroundingCell.InBounds(map) || surroundingCell.GetEdifice(map) != null)
                {
                    return false;
                }
            }

            return false;
        }

        protected override void ScatterAt(IntVec3 loc, Map map, GenStepParams parms, int count = 1)
        {
            if ((parms.sitePart?.things?.Any ?? false) 
                && parms.sitePart.things.Take(parms.sitePart.things[0]) is Pawn singlePawnToSpawn)
            {
                // Okay
            }
            else if (map.Parent.GetComponent<FedSurvivorComp>() is FedSurvivorComp comp && comp.pawn.Any)
            {
                singlePawnToSpawn = comp.pawn.Take(comp.pawn[0]);
            }
            else
            {
                Log.Message("[HG] Needed to use DownedRefugeeQuestUtility fallback. :(");
                singlePawnToSpawn = DownedRefugeeQuestUtility.GenerateRefugee(map.Tile, PawnKindDefOf.SpaceRefugee);
            }

            var suit = ResourceBank.MakeAppropriateSuitForPawn(singlePawnToSpawn);
            singlePawnToSpawn.apparel.Wear(suit);
        }
    }
}
