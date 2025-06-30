using RimWorld;
using System.Linq;
using Verse;
using Verse.AI.Group;

/*namespace HG.FFF
{
    public class IncidentWorker_StartFedScoutWreckQuest : IncidentWorker_GiveQuest
    {
        const int MAX_TICKS_SINCE_TRADE = 60_000; // one day

        [Unsaved(false)]
        private bool keyItemObtained;
        public override float BaseChanceThisGame => keyItemObtained ? 0f : def.baseChance;

        public override float ChanceFactorNow(IIncidentTarget target)
        {
            if (keyItemObtained) return 0f;

            var taleManager = Find.TaleManager;
            taleManager.LogTales();
            foreach (var tale in Find.TaleManager.AllTalesListForReading)
            {
                if (tale.def != TaleDefOf.TradedWith || tale.AgeTicks < MAX_TICKS_SINCE_TRADE)
                    continue;
                //var tale.DominantPawn
            }
            return base.ChanceFactorNow(target);
        }

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (parms.forced) return true;
            if (keyItemObtained) return false;

            if (def.minPopulation > 0 && PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists
                .Count() < def.minPopulation)
                return false;

            if (parms.target is not Map someMap)
            {
                Log.ErrorOnce($"[HG] Scout wreck incident worker: expected parms target to be map, it was {parms.target.GetType()}", 239586283);
                return false;
            }

            return someMap.lordManager.lords.ContainsAny(LordJobIsTradeEvent);
        }

        private static bool LordJobIsTradeEvent(Lord lord) => lord.LordJob is LordJob_TradeWithColony;
    }
}
*/