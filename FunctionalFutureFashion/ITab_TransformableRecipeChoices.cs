using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace HG.FFF
{
    [StaticConstructorOnStartup, UsedImplicitly]
    public class ITab_TransformableRecipeChoices : ITab
    {
        private const float StdLineHeight = 22f;
        private static readonly Vector2 WinSize = new Vector2(420, 480);
        private static readonly Rect TileSize = Rect.MinMaxRect(0, 0, 64, 64);

        private Vector2 scrollPosition;
        
        public ITab_TransformableRecipeChoices()
        {
            this.size = WinSize;
            this.labelKey = "FFF.Upgrade";
        }

        private CompTransformable LastSelectedTransformable = null; 
        
        protected override void FillTab()
        {
            var viewRect = new Rect(0f, StdLineHeight, size.x, size.y - StdLineHeight).ContractedBy(10f);
            if (!ReferenceEquals(LastSelectedTransformable.parent, SelThing))
            {
                Widgets.DrawMenuSection(viewRect);
                Widgets.LabelFit(viewRect, "Loading");
                OnOpen();
                return;
            }
            if (LastSelectedTransformable.PotentialTransformations.Count < 1)
            {
                Widgets.LabelFit(viewRect, "DEV: No options found");
                return;
            }

            var tableRect = new Rect(viewRect) { width = viewRect.width - 16, height = TileSize.height * LastSelectedTransformable.PotentialTransformations.Count };
            Widgets.BeginGroup(viewRect);
            Widgets.BeginScrollView(viewRect.AtZero(), ref scrollPosition, tableRect.AtZero());

            LastSelectedTransformable.HasActiveTransformationBill(out _, out int activeIndex);
            for (int i = 0; i < LastSelectedTransformable.PotentialTransformations.Count; i++)
            {
                var rowRect = new Rect(tableRect) { y = TileSize.width * i, height = TileSize.height };
                DrawOptionRow(rowRect, LastSelectedTransformable, i, activeIndex == i);
            }

            Widgets.EndScrollView();
            Widgets.EndGroup();
        }
        
        public override void OnOpen()
        {
            LastSelectedTransformable = null;
            if (SelThing.TryGetComp<CompTransformable>(out var transformable))
            {
                LastSelectedTransformable = transformable;
            }
            else
            {
                Log.Error($"Tried to open {nameof(ITab_TransformableRecipeChoices)} for a {SelThing.def.defName}, which lacks a transformable comp");
                return;
            }
            
            scrollPosition = Vector2.zero;
            base.OnOpen();
        }

        private static System.Action NoAvailableAction(string src) => () => Verse.Log.Error("No action set for " + src);  
        private void DrawOptionRow(Rect rowArea, CompTransformable transformable, int index, bool isActive)
        {
            if (isActive)
                Widgets.DrawLightHighlight(rowArea);
            else 
                Widgets.DrawHighlightIfMouseover(rowArea);

            Widgets.BeginGroup(rowArea);
            var option = transformable.PotentialTransformations[index];
            // TODO: Assign icon specifically
            var iconTex = isActive ? Widgets.CheckboxOffTex : Widgets.GetIconFor(option.Recipe.ingredients.Last().FixedIngredient) ?? TexCommand.Install;
            var iconTooltip = isActive ? "CancelButton".TranslateSimple() : option.Recipe.description;
            var buttonForeground = option.ExtInfo.quality.ToColor();
            var buttonBackground = Widgets.MenuSectionBGFillColor;
            
            Widgets.DrawBoxSolidWithOutline(TileSize.ContractedBy(6f), buttonBackground, buttonForeground);
            Widgets.DrawTextureFitted(TileSize.ContractedBy(7f), Widgets.ButtonSubtleAtlas, 1f);
            if (Widgets.ButtonImage(TileSize.ContractedBy(10f), iconTex, tooltip: iconTooltip))
            {
                transformable.DeleteTransformationBills();
                
                if (isActive) return;
                
                transformable.CreateTransformationBill(index);
            }

            var skillLabelText = option.Recipe.MinSkillString;
            var skillLabelWidth = option.Recipe.skillRequirements.Any() ? Text.CalcSize(skillLabelText).x : 0;
            new Rect(TileSize.xMax, 0, rowArea.width - TileSize.width, rowArea.height * 0.5f)
                .ContractedBy(4f).SplitVertically(rowArea.width - TileSize.width - skillLabelWidth, out Rect recipeLabelRect, out Rect skillLabelRect );
            
            Text.Anchor = TextAnchor.MiddleLeft;
            using (var _ = new TextBlock(GameFont.Small, option.ExtInfo.quality.ToColor()))
                Widgets.LabelFit(recipeLabelRect, option.Recipe.LabelCap);

            Text.Anchor = TextAnchor.MiddleRight;
            if (skillLabelWidth > float.Epsilon)
            {
                // Not sure why the skillLabelRect cuts off the top of the text.
                using (var _ = new TextBlock(GameFont.Small, newColor: Widgets.NormalOptionColor))
                    Widgets.LabelFit(skillLabelRect.ExpandedBy(0, rowArea.height * 0.5f), skillLabelText);    
            }

            recipeLabelRect.y += recipeLabelRect.height + 2f;
            GenUI.DrawElementStack(recipeLabelRect, recipeLabelRect.height, option.Recipe.ingredients,
                IngredientChipDrawer,
                GetChipSize);
            
            Widgets.EndGroup();

            Text.Anchor = TextAnchor.UpperLeft;
        }
        
        static void IngredientChipDrawer(Rect rect, IngredientCount element)
        {
            rect.SplitVerticallyWithMargin(out Rect iconArea, out Rect labelArea, out _, 4f, StdLineHeight);
            Widgets.DrawBoxSolid(rect, Widgets.WindowBGFillColor);
            Widgets.DrawHighlightIfMouseover(iconArea);
            TooltipHandler.TipRegion(rect, element.FixedIngredient.LabelCap);
            Widgets.DefIcon(iconArea, element.FixedIngredient);
            Widgets.LabelFit(labelArea, $"x {element.GetBaseCount()}");
        }
        private static float GetChipSize(IngredientCount element)
        {
            return Text.CalcSize( $"x {element.GetBaseCount()}").x + StdLineHeight + 8f;
        }
    }
}