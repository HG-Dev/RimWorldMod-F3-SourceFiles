//#define SHOW_OLD_STUFF
#if SHOW_OLD_STUFF
using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    public class CompSpawnReplacementBlueprint : ThingComp
    {
        public const string SIGNAL_BLUEPRINT_PLACED = "BlueprintPlaced";
        public const string SIGNAL_FRAME_REMOVED = "FrameRemoved";

        private CompProperties_SpawnReplacementBlueprint Props => (CompProperties_SpawnReplacementBlueprint)props;

        public IReadOnlyList<ThingDef> AllUpgrades => Props.upgradeThingDefs;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            foreach (var blueprint in Props.upgradeThingDefs)
                blueprint.ResolveReferences();
        }

        private FloatMenu GenerateUpgradeOptionsMenu()
        {
            StringBuilder sb = new();
            List<FloatMenuOption> list = new();
            for (int i = 0; i < Props.upgradeThingDefs.Count; i++)
            {
                var option = Props.upgradeThingDefs[i];
                var icon = option.costList[0].thingDef.uiIcon;
                AcceptanceReport canBuild = true;

                IReadOnlyList<ResearchProjectDef> prerequisites = (IReadOnlyList<ResearchProjectDef>)option.researchPrerequisites 
                                                                  ?? Array.Empty<ResearchProjectDef>();
                foreach (var prerequisite in prerequisites)
                {
                    if (!prerequisite.IsFinished)
                    {
                        canBuild = "NotStudied".Translate(prerequisite);
                        break;
                    }
                }

                if (!canBuild.Accepted) goto CreateMenuOption;
                
                var skillLevelSatisfied = false;
                foreach (var pawn in parent.Map.mapPawns.FreeColonists)
                {
                    if (pawn.skills.GetSkill(SkillDefOf.Construction).Level >= option.constructionSkillPrerequisite)
                    {
                        skillLevelSatisfied = true;
                        break;
                    }
                }
                skillLevelSatisfied |= MechanitorUtility.AnyPlayerMechCanDoWork(WorkTypeDefOf.Construction,
                    option.constructionSkillPrerequisite, out _);
                    
                if (!skillLevelSatisfied)
                {
                    canBuild = "NoColonistWithAllSkillsForConstructing".Translate(Faction.OfPlayer.def.pawnsPlural) + $" ({option.constructionSkillPrerequisite})";
                    goto CreateMenuOption;
                }
                
                IReadOnlyList<ThingDefCountClass> resources = (IReadOnlyList<ThingDefCountClass>)option.costList 
                                                              ?? Array.Empty<ThingDefCountClass>();
                foreach (var resource in resources)
                {
                    if (!parent.Map.listerThings.AnyThingWithDef(resource.thingDef))
                    {
                        canBuild = "MissingMaterials".Translate(resource.LabelCap);
                        break;
                    }
                }
                
                CreateMenuOption:

                void PlaceBlueprint()
                {
                    Props.GetOrMakeReplacementStepForParent(parent, option);
                    parent.BroadcastCompSignal(SIGNAL_BLUEPRINT_PLACED);
                }

                var space = option.label.Trim().LastIndexOf(' ');
                sb.Clear();
                sb.Append(option.label.Substring(space+1).CapitalizeFirst());
                if (!canBuild.Accepted)
                    sb.Append($" - {canBuild.Reason}");
                
                var menuOption = new FloatMenuOption(sb.ToString(), PlaceBlueprint, icon, Color.white)
                {
                    Disabled = !canBuild.Accepted
                };
                list.Add(menuOption);
            }

            return new FloatMenu(list);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (parent is not { SpawnedOrAnyParentSpawned: true }
                || parent.Faction != Faction.OfPlayer 
                || Props.upgradeThingDefs.NullOrEmpty())
                yield break;
            
            yield return new Command_Action
            {
                defaultLabel = "FFF.Upgrade".Translate(),
                defaultDesc = "FFF.UpgradeDesc".Translate(),
                action = () =>
                {
                    var menu = GenerateUpgradeOptionsMenu();
                    Find.WindowStack.Add(menu);
                },
                activateSound = SoundDefOf.Tick_Tiny,
                icon = TexCommand.Draft
            };
        }
    }
}
#endif