#if false
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HG.FFF
{
    /// <summary>
    /// A temporary object that immediately replaces itself with its virtualDefParent.
    /// Also destroys a previous building that exists at its virtualDefParent's altitude layer.
    /// </summary>
    /// <remarks>Currently, things generated through Building_Upgrade automatically join player faction</remarks>
    public class Building_Upgrade : Building
    {
        private Thing thingToSpawn;
        
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            foreach (var building in Position.GetThingList(map))
            {
                if (GenSpawn.SpawningWipes(def.virtualDefParent, building.def))
                {
                    building.Destroy();
                }
            }
            Log.Message("Spawning Building_Upgrade; making final thing: "+def.virtualDefParent.defName);
            // thingToSpawn = ThingMaker.MakeThing(def.virtualDefParent, this.Stuff);
            // thingToSpawn.SetFactionDirect(Faction.OfPlayer);
        }

        public override void TickRare()
        {
            if (Find.Selector.IsSelected(this))
                Find.Selector.Deselect(this);
            
            if (thingToSpawn == null)
            {
                Log.Error($"{nameof(Building_Upgrade)} -- thingToSpawn was null");
                return;
            }
            
            //var result = GenSpawn.Spawn(thingToSpawn, Position, Map, Rotation);
            //if (result == null)
                Log.Error($"{nameof(Building_Upgrade)} -- failed to create " + def.virtualDefParent.defName);
            //Destroy();
        }

        public override string GetInspectString()
        {
            return def.description;
        }
    }
}
#endif