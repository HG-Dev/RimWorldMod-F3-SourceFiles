// using System;
// using System.Collections.Generic;
// using RimWorld;
// using RimWorld.Planet;
// using RimWorld.QuestGen;
// using RimWorld.SketchGen;
// using UnityEngine;
// using Verse;
//
// namespace HG.FFF
// {
//     /// <summary>
//     /// Expects "siteTile" and "points" to be set before running.
//     /// </summary>
//     public sealed class QuestNode_FedScoutQuest : QuestNode
//     {
//         private static SimpleCurve ThreatPointsOverPointsCurve => new SimpleCurve
//         {
//             new CurvePoint(35f, 38.5f),
//             new CurvePoint(400f, 165f),
//             new CurvePoint(10000f, 4125f)
//         };
//
//         private const string EnemyListAlias = "enemies";
//         private const string TurretNestAlias = "nest";
//         private static readonly IntRange AttackDelayRange = new IntRange(2500, 5000); 
//         
//         private PrefabDef HideoutDef = ResourceBank.PrefabDefOf.FFF_FedScoutHideout_SouthwestEntrance;
//         private SitePartDef FedScoutSitePartDef = ResourceBank.SitePartDefOf.FFF_FedScoutWreckSite;
//         private PlanetTile SiteTile = PlanetTile.Invalid;
//         private float QuestPoints = 1;
//         private float ThreatPoints = 2000;
//         private Pawn Survivor;
//         private Faction EnemyOfSurvivorFaction = Faction.OfMechanoids;
//         private Faction SharedEnemyFaction = Faction.OfPlayer;
//         private PawnKindDef AttackingAnimalKind = null;
//         
//         private bool FactionsAreNotNull => EnemyOfSurvivorFaction != null && SharedEnemyFaction != null;
//
//         private LayoutSketch GetHideoutSketch(Map map)
//         {
//             var rocks = Find.World.NaturalRockTypesIn(map.Tile);
//             var wallStuff = rocks.RandomElementWithFallback(ResourceBank.ThingDefOf.Limestone);
//             var sketch = new LayoutSketch()
//             {
//                 wallStuff = wallStuff,
//                 defaultAffordanceTerrain = ResourceBank.TerrainDefOf.RockTerrainFromThingDef(wallStuff),
//             };
//
//             return null;
//         }
//         
//         private LayoutSketch GetShipSketch(Map map)
//         {
//             var rocks = Find.World.NaturalRockTypesIn(map.Tile);
//             var wallStuff = rocks.RandomElementWithFallback(ResourceBank.ThingDefOf.Limestone);
//             var sketch = new LayoutSketch()
//             {
//                 wallStuff = wallStuff,
//                 defaultAffordanceTerrain = ResourceBank.TerrainDefOf.RockTerrainFromThingDef(wallStuff),
//                 
//             };
//             var resolver = new SketchResolver_FedScoutWreck();
//             resolver.Resolve(new SketchResolveParams() {sketch = sketch, });
//             return sketch;
//         }
//         
//         
//         protected override void RunInt()
//         {
//             if (!FactionsAreNotNull)
//                 throw new InvalidOperationException("[HG] QuestNode_FFF_FedScoutQuest -- TestRunInt wasn't run before RunInt");
//             
//             Slate slate = QuestGen.slate;
//             Quest quest = QuestGen.quest;
//             float points = slate.Get("points", 0f);
//             QuestGen.GenerateNewSignal("RaidArrives");
//
//             //LayoutStructureSketch ancientLayoutStructureSketch = QuestSetupComplex(quest, points);
//             var siteDef = ResourceBank.SitePartDefOf.FFF_FedScoutWreckSite;
//             var siteDefWithParams = new SitePartDefWithParams(siteDef, new SitePartParams()
//             {
//                 turretsCount = Mathf.Min(Mathf.RoundToInt(ThreatPoints / 1000f), 1),
//                 points = QuestPoints,
//                 threatPoints = ThreatPoints,
//             });
//             foreach (var genStep in siteDef.ExtraGenSteps)
//             {
//                 Log.Message("[HG] FedScoutQuest site gen step: " + genStep.defName);
//             }
//             Site site = QuestGen_Sites.GenerateSite(Gen.YieldSingle(siteDefWithParams), SiteTile,
//                 RimWorld.Faction.OfAncients, true,
//                 worldObjectDef: ResourceBank.WorldObjectDefOf.FFF_FedScoutWreckSiteWorldObject);
//             site.desiredThreatPoints = ThreatPoints;
//             site.doorsAlwaysOpenForPlayerPawns = true;
//             site.parts[0].things = new ThingOwner<Pawn>()
//             {
//                 contentsLookMode = LookMode.Deep, 
//                 dontTickContents = true, 
//                 InnerListForReading = { Survivor },
//                 removeContentsIfDestroyed = true
//             };
//             quest.SpawnWorldObject(site);
//             TimedDetectionRaids component = site.GetComponent<TimedDetectionRaids>();
//             if (component != null)
//             {
//                 component.alertRaidsArrivingIn = true;
//             }
//             // Potential method for ending quest
//             //string inSignal = QuestGenUtility.HardcodedSignalWithQuestID("site.Destroyed");
//             //quest.End(QuestEndOutcome.Unknown, 0, null, inSignal);
//             slate.Set("site", site);
//         }
//
//         // Can we find everything we need to generate the quest?
//         protected override bool TestRunInt(Slate slate)
//         {
//             var tileFound = slate.TryGet("siteTile", out SiteTile);
//             if (!tileFound)
//                 Log.Error($"[HG] {nameof(QuestNode_FedScoutQuest)} -- site tile not found. Run QuestNode_GetSiteTile first");
//             if (!slate.TryGet("points", out QuestPoints))
//                 Log.Error($"[HG] {nameof(QuestNode_FedScoutQuest)} -- No points assigned to quest creation");
//
//             ThreatPoints = Find.Storyteller.difficulty.allowViolentQuests
//                 ? ThreatPointsOverPointsCurve.Evaluate(QuestPoints)
//                 : 0f;
//             
//             EnemyOfSurvivorFaction = Find.FactionManager.AllFactionsVisible.RandomElementByWeight(WeightSelector);
//             var allFactions = new List<Faction>() { EnemyOfSurvivorFaction };
//             
//             if (UnityEngine.Random.value > 0.5f && 
//                 ManhunterPackGenStepUtility.TryGetAnimalsKind(ThreatPoints, SiteTile, out AttackingAnimalKind))
//             {
//                 // Using animals instead
//                 SharedEnemyFaction = null;
//             }
//             else
//             {
//                 SharedEnemyFaction = Find.FactionManager.RandomEnemyFaction();
//                 allFactions.Add(SharedEnemyFaction);
//             }
//             
//             Survivor = ResourceBank.CreateFederationSurvivor(SiteTile);
//             
//             slate.Set("sharedEnemy", SharedEnemyFaction);
//             slate.Set("survivorEnemy", EnemyOfSurvivorFaction);
//             slate.Set("attackingAnimalKind", AttackingAnimalKind);
//             slate.Set("survivor", Survivor);
//             
//             var questPart = new QuestPart_Hyperlinks()
//             {
//                 thingDefs = new List<ThingDef>() {ResourceBank.ThingDefOf.HypermeshPrinter},
//                 factions = allFactions,
//                 pawns = new List<Pawn>() {Survivor}
//             };
//             QuestGen.quest.AddPart(questPart);
//
//             return tileFound && FactionsAreNotNull;
//             
//             float WeightSelector(Faction faction)
//             {
//                 if (faction.IsPlayer || faction.defeated || faction.deactivated)
//                     return 1f;
//                 if (faction.allowRoyalFavorRewards)
//                     return 1f;
//                 return 0.25f;
//             }
//         }
//     }
// }