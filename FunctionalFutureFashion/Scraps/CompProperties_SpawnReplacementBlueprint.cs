//#define SHOW_OLD_STUFF
#if SHOW_OLD_STUFF
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace HG.FFF
{
    public class CompProperties_SpawnReplacementBlueprint : CompProperties
    {
        public CompProperties_SpawnReplacementBlueprint()
        {
            this.compClass = typeof(CompSpawnReplacementBlueprint);
            upgradeThingDefs = new();
        }

        public List<ThingDef> upgradeThingDefs;

        public Thing GetOrMakeReplacementStepForParent(Thing parent, ThingDef step)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (step == null) throw new ArgumentNullException(nameof(step));
            
            if (!upgradeThingDefs.Contains(step))
                throw new KeyNotFoundException(
                    $"{step?.defName ?? "<null>"} not a serialized option for {parent.def.defName}");
        
            if (step.thingClass != typeof(Building_Upgrade))
                throw new InvalidCastException(
                    $"Temporary upgrade {step.defName} creates a building that is not {nameof(Building_Upgrade)}");
            
            if (step.virtualDefParent == null)
                throw new ArgumentNullException(nameof(ThingDef.virtualDefParent),
                    "Virtual def parent required to replace temporary building");
            
            if (step.blueprintDef == null)
                Log.Error($"{step.defName} has no blueprint def");

            Thing result = null;
            foreach (var thing in parent.Position.GetThingList(parent.Map))
            {
                if (thing.def == step || thing.def.entityDefToBuild == step)
                {
                    result = thing;
                    continue;
                }

                if (thing is Blueprint or Frame)
                    thing.Destroy(DestroyMode.Refund);
            }
            
            result ??= GenConstruct.PlaceBlueprintForBuild(step,
                                parent.Position, parent.Map, parent.Rotation,
                                parent.Faction, parent.Stuff, sendBPSpawnedSignal: false);
            Find.Selector.ClearSelection();
            Find.Selector.Select(result);
            
            return result;
        }
    }
}
#endif