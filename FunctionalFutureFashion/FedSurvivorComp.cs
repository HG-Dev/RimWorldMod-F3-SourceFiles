using RimWorld.Planet;
using Verse;

namespace HG.FFF
{
    public class FedSurvivorComp : ImportantPawnComp
    {
        protected override string PawnSaveKey => "survivor";

        protected override void RemovePawnOnWorldObjectRemoved()
        {
            if (!this.pawn.Any)
                return;

            pawn.ClearAndDestroyContentsOrPassToWorld(DestroyMode.Vanish);
        }

        public override string CompInspectStringExtra()
        {
            if (!pawn.Any) return null;
            if ("Survivor".TryTranslate(out var stdTranslation))
                return stdTranslation + ": " + pawn[0].LabelCap;
            return "FFF.Survivor".Translate() + ": " + pawn[0].LabelCap;
        }
    }
}
