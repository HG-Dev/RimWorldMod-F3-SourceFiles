using JetBrains.Annotations;
using Verse;

namespace HG.FFF
{
    [UsedImplicitly]
    public class Command_ActionWithLimitedQueue : Command_Action
    {
        public System.Func<int> queueCountGetter;
        public System.Func<int> queueCapacityGetter;

        public override string TopRightLabel
        {
            get
            {
                int num = this.queueCountGetter();
                string str1 = num.ToString();
                num = this.queueCapacityGetter();
                string str2 = num.ToString();
                return $"{str1} / {str2}";
            }
        }

        public void UpdateQueueCapacity()
        {
            if (queueCountGetter() < queueCapacityGetter())
                return;
            Disable("CommandNoUsesLeft".Translate());
        }
    }
}