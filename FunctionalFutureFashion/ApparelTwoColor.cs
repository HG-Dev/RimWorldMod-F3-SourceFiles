using Verse;
using RimWorld;
using UnityEngine;

namespace HG.FFF
{
    class ApparelTwoColor : Apparel
    {
        public override Color DrawColor
        {
            get
            {
                Color drawColor = Color.white;
                CompColorable comp = this.GetComp<CompColorable>();
                if (comp != null && comp.Active)
                {
                    drawColor = comp.Color;
                }
                else if (StyleDef != null && StyleDef.color != default(Color))
                {
                    drawColor = StyleDef.color;
                }
                else
                {
                    return drawColor;
                }

                // Give it a bit more visual interest by increasing saturation
                Color.RGBToHSV(drawColor, out float hue, out float sat, out float value);
                return Color.HSVToRGB(hue, Mathf.Clamp01(sat * 1.25f), value);
		    }
            set
            {
                this.SetColor(value, true);
            }
        }

        public override Color DrawColorTwo
        {
            get
            {
                if (this.Stuff != null)
                    return this.def.GetColorForStuff(this.Stuff);

                if (this.def.graphicData != null)
                    return this.def.graphicData.colorTwo;

                return Color.white;
            }
        }
    }

}
