// using RimWorld;
// using RimWorld.Planet;
// using RimWorld.QuestGen;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Text;
// using System.Threading.Tasks;
// using Verse;
// using Verse.Grammar;
//
// namespace HG.FFF
// {
//     public class SitePartWorker_FedSurvivor : SitePartWorker
//     {
//         public override void Notify_GeneratedByQuestGen(SitePart part, Slate slate, List<Rule> outExtraDescriptionRules, Dictionary<string, string> outExtraDescriptionConstants)
//         {
//             base.Notify_GeneratedByQuestGen(part, slate, outExtraDescriptionRules, outExtraDescriptionConstants);
//             part.site.AllComps.Add(new FedSurvivorComp());
//             var pawn = ResourceBank.CreateFederationSurvivor(part.site.Tile);
//             part.things = new ThingOwner<Pawn>(part, true);
//             part.things.TryAdd(pawn, true);
//             slate.Set<Pawn>("survivor", pawn);
//         }
//
//         public override string GetPostProcessedThreatLabel(Site site, SitePart sitePart)
//         {
//             var text = new StringBuilder(base.GetPostProcessedThreatLabel(site, sitePart));
//             if (sitePart.things != null && sitePart.things.Any)
//             {
//                 text.Append(": ");
//                 text.Append(sitePart.things[0].LabelShortCap);
//             }
//             if (site.HasWorldObjectTimeout)
//             {
//                 text.Append(" (");
//                 text.Append("DurationLeft".Translate(site.WorldObjectTimeoutTicksLeft.ToStringTicksToPeriod()));
//                 text.Append(")");
//             }
//             return text.ToString();
//         }
//
//         //public override void PostDestroy(SitePart sitePart) UNNECESSARY
//     }
// }
