using UnityEngine;
using Verse;

namespace HG.FFF
{
    public class CompProperties_ToggledOverlay : CompProperties
    {
        public Vector3 size;
        [NoTranslate]
        public string graphicPath;
        
        public Material Graphic { get; private set; }

        public override void ResolveReferences(ThingDef parentDef)
        {
            var stdTexPath = parentDef.graphicData.texPath;
            base.ResolveReferences(parentDef);
            //LongEventHandler.ExecuteWhenFinished((Action)(() => this.overlayGraphic = MaterialPool.MatFrom(this.graphicPath)));
        }
    }
}
