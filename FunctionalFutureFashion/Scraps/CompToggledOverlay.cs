using UnityEngine;
using Verse;

/*namespace HG.FFF
{
    public class CompToggledOverlay : ThingComp
    {
        public CompProperties_ToggledOverlay Props
        {
            get
            {
                return (CompProperties_ToggledOverlay)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(this.parent.DrawPos + Altitudes.AltIncVect, this.parent.Rotation.AsQuat, Props.size);
            Graphics.DrawMesh(MeshPool.plane10, matrix, Props.overlayGraphic, 0);
        }
    }
}
*/