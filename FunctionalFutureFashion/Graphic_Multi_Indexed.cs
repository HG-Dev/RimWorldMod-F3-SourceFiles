#define VERBOSE
using System;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    public class Graphic_Multi_Indexed : Graphic_Collection
    {
        public override Material MatSingle => subGraphics[0].MatSingle;
        public int SubGraphicCount => subGraphics.Length;

        public override Material MatAt(Rot4 rot, Thing thing = null)
        {
            var graphic = GetMultiGraphicForThing(thing);
            return graphic.MatAt(rot, thing);
        }

        public override Material MatSingleFor(Thing thing)
        {
            return GetMultiGraphicForThing(thing).MatSingle;
        }

        Graphic_Multi GetMultiGraphicForThing(Thing thing)
        {
            var valueOrDefault = thing?.OverrideGraphicIndex ?? default;

            if (subGraphics[valueOrDefault % SubGraphicCount] is Graphic_Multi multiGraphic)
                return multiGraphic;
            else
                throw new InvalidCastException("Graphic_Multi_Indexed should consist of ALL Graphic_Multi subgraphics");
        }

        public override void DrawWorker(Vector3 loc, Rot4 rot, ThingDef thingDef, Thing thing, float extraRotation)
        {
            var graphic = GetMultiGraphicForThing(thing);
            graphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
            if (base.ShadowGraphic != null)
                base.ShadowGraphic.DrawWorker(loc, rot, thingDef, thing, extraRotation);
        }

        public override string ToString()
        {
            return String.Concat(new String[]
            {
                "MultiIndexed(path=",
                path,
                ", count=",
                SubGraphicCount.ToString(),
                ")"
            });
        }
    }
}
