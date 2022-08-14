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
                Color stdColor = Color.white;

                if (StyleDef != null && StyleDef.color != default(Color))
                {
                    stdColor = StyleDef.color;
                }
                else
                {
                    CompColorable comp = this.GetComp<CompColorable>();
                    if (comp != null && comp.Active)
                        return comp.Color;
                }

                if (stdColor != Color.white)
                {
                    Color.RGBToHSV(stdColor, out float hue, out float sat, out float value);
                    stdColor = Color.HSVToRGB(hue, Mathf.Clamp01(sat * 1.2f), value);
                }

                return stdColor;
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
