using HG.FFF.Harmony;
using Verse;

namespace HG.FFF
{
    [StaticConstructorOnStartup]
    public class FunctionalFutureFashion : Mod
    {
        public static HarmonyLib.Harmony HarmonyInstance;

        public FunctionalFutureFashion(ModContentPack content) : base(content)
        {
            HarmonyInstance = new HarmonyLib.Harmony("HG.Originals.F3");
            LongEventHandler.ExecuteWhenFinished(Initialize);
        }

        public static void Initialize()
        {
            Patches.TryAllPatchesForSOS2(HarmonyInstance);
        }
    }
}
