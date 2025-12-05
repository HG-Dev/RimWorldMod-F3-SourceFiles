#if false
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace HG.FFF
{
    public class Blueprint_Upgrade : Blueprint_Build
    {
        protected override Thing MakeSolidThing(out bool shouldSelect)
        {
            Frame to = new Frame()
            {
                def = def.entityDefToBuild.frameDef,
            };
            to.SetStuffDirect(Stuff);
            to.PostMake();
            to.PostPostMake();
            to.StyleSourcePrecept = this.StyleSourcePrecept;
            to.StyleDef = this.StyleDef;
            to.glowerColorOverride = this.glowerColorOverride;
            shouldSelect = false;
            this.Map.enrouteManager.SendReservations((IHaulEnroute) this, (IHaulEnroute) to);
            return (Thing) to;
        }

        public override bool TryReplaceWithSolidThing(Pawn workerPawn, out Thing createdThing, out bool jobEnded)
        {
            var previouslySelected = Find.Selector.IsSelected(this);
            Find.Selector.Deselect(this);
            jobEnded = false;
            createdThing = null;
            var args = (Position, Map, Rotation);
            
            // Create frame
            createdThing = MakeSolidThing(out bool shouldSelect);
            if (createdThing.def.CanHaveFaction)
                createdThing.SetFactionDirect(workerPawn.Faction);
            createdThing.DrawColor = def.highlightColor ?? createdThing.DrawColor;
            
            if (!Destroyed)
                Destroy(); // Clear blueprint
            
            var thing = GenSpawn.Spawn(createdThing, args.Position, args.Map, args.Rotation);
            
            if (thing == null) 
                return false;
            
            if (previouslySelected || shouldSelect)
                Find.Selector.Select(thing);

            foreach (Pawn pawn in args.Map.mapPawns.AllPawnsSpawned)
            {
                pawn.pather.NotifyThingTransformed(this, thing);
            }
                
            return true;
        }
        
        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Take(1);
        }

        public override string GetInspectString()
        {
            return def.description;
        }
    }
}
#endif