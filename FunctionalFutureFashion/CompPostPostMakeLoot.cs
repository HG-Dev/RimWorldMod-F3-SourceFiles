using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    public class CompPostPostMakeLoot : ThingComp
    {
        private CompProperties_PostPostMakeLoot Props => (CompProperties_PostPostMakeLoot)this.props;

        [Unsaved(false)]
        private bool spawned;

        public override void PostPostMake()
        {
            if (Props?.src is not {} lootSrc)
                return;
            
            if (parent.TryGetInnerInteractableThingOwner() is not { } container || spawned) 
                return;
            
            lootSrc.ResolveReferences();
            spawned = true;
            container.AddLoot(Props.src.ingredients, new FloatRange(0.33f, 1f),
                parent.MapHeld?.Tile.tileId ?? GenTicks.TicksGame);
        }


        // private FloatMenu GenerateUpgradeOptionsMenu()
        // {
        //     StringBuilder sb = new();
        //     List<FloatMenuOption> list = new();
        //     for (int i = 0; i < Props.recipes.Count; i++)
        //     {
        //         var option = Props.recipes[i];
        //         var icon = option.UIIconThing.uiIcon;
        //
        //         AcceptanceReport canBuild = true;
        //
        //         foreach (var prerequisite in option.researchPrerequisites.OrElseEmptyEnumerable())
        //         {
        //             if (!prerequisite.IsFinished)
        //             {
        //                 canBuild = "NotStudied".Translate(prerequisite);
        //                 break;
        //             }
        //         }
        //         
        //         void PlaceBlueprint()
        //         {
        //
        //         }
        //
        //         var space = option.label.Trim().LastIndexOf(' ');
        //         sb.Clear();
        //         sb.Append(option.label.Substring(space+1).CapitalizeFirst());
        //         if (!canBuild.Accepted)
        //             sb.Append($" - {canBuild.Reason}");
        //         
        //         var menuOption = new FloatMenuOption(sb.ToString(), PlaceBlueprint, icon, Color.white)
        //         {
        //             Disabled = !canBuild.Accepted
        //         };
        //         list.Add(menuOption);
        //     }
        //
        //     return new FloatMenu(list);
        // }
    }
}