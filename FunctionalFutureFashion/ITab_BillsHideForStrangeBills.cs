using RimWorld;
using Verse;

namespace HG.FFF
{
    public class ITab_BillsHideForStrangeBills : ITab_Bills
    {
        public override bool Hidden => BillGiverHasAnyStrangeBills(SelThing);

        private static bool BillGiverHasAnyStrangeBills(Thing thing)
        {
            if (thing is not IBillGiver billGiver)
                return false;

            foreach (var bill in billGiver.BillStack.Bills)
            {
                if (!thing.def.AllRecipes.Contains(bill.recipe))
                    return true;
            }

            return false;
        }
    }
}