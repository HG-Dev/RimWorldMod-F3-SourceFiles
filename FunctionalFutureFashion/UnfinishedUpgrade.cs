using RimWorld;
using Verse;

namespace HG.FFF
{
    public class UnfinishedUpgrade : UnfinishedThing
    {
        public override string LabelNoCount => "FFF.UnfinishedUpgrade".Translate(this.Recipe.label);
        public override string DescriptionDetailed => this.Recipe.description;
        public override string DescriptionFlavor => this.Recipe.description;
    }
}