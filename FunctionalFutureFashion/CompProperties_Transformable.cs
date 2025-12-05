using System;
using System.Collections.Generic;
using Verse;

namespace HG.FFF
{
    public class CompProperties_Transformable : CompProperties
    {
        public const string TRANSFORM_ON_INIT = "OnInitialize";
        
        public CompProperties_Transformable()
        {
            compClass = typeof(CompTransformable);
        }

        public bool AnySignalTransform(string signal, out ThingDef become)
        {
            foreach (var pair in signalPairs)
            {
                if (pair.Key.Equals(signal, StringComparison.InvariantCultureIgnoreCase))
                {
                    become = pair.Value;
                    return true;
                }
            }

            become = null;
            return false;
        }
        
        public List<RecipeDef> recipes = new ();
        public Dictionary<string, ThingDef> signalPairs = new();
    }
}