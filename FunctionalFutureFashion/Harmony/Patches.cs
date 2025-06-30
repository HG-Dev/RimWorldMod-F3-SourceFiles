#define VERBOSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace HG.FFF.Harmony
{
    public class Patches
    {
        public enum VersionOfSOS2
        {
            Unknown,
            //SteamTwoPointFiveOrOlder, retired
            //ExperimentalPreNineties, retired
            ExperimentalPostNineties,
            SteamTwoPointSevenOrNewer
        }

        public static VersionOfSOS2 Version = VersionOfSOS2.Unknown;
        public static MethodInfo OldSpaceSuitMethodInfo;
        public static MethodInfo ModernSpaceSuitMethodInfo;
        public static MethodInfo GoFastMethodInfo;
        public static MethodInfo TwoColorGetColoredVersionMethodInfo;
        public static MethodInfo GetGraphicApparelRecordMethodInfo;


        public static void TryAllApparelPatches(HarmonyLib.Harmony harm)
        {
#if VERBOSE
            Verse.Log.Message("<color=grey>[HG]</color> FFF::TryAllApparelPatches");
#endif
            TwoColorGetColoredVersionMethodInfo = AccessTools.Method("Verse.Graphic_Random:GetColoredVersion",
                new Type[] { typeof(Shader), typeof(UnityEngine.Color), typeof(UnityEngine.Color) });

            var t_ApparelGraphicRecordSetter = typeof(RimWorld.ApparelGraphicRecordGetter);

            GetGraphicApparelRecordMethodInfo = AccessTools.Method(t_ApparelGraphicRecordSetter, "TryGetGraphicApparel",
                new Type[] { typeof(RimWorld.Apparel), typeof(RimWorld.BodyTypeDef), typeof(bool), typeof(RimWorld.ApparelGraphicRecord).MakeByRefType() });

            if (TwoColorGetColoredVersionMethodInfo != null)
            {
                if (null == harm.Patch(TwoColorGetColoredVersionMethodInfo,
                    prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.ComplexRandomGraphicPrefix))))
                    Verse.Log.Error("<color=grey>[HG]</color> Failed to patch Graphic_Random:GetColoredVersion for two-colored apparel.\nYou'll see a harmless error when the game spawns randomized F3 apparel.");
            }
            else
            {
                Verse.Log.Warning("<color=grey>[HG]</color> Failed to patch Graphic_Random:GetColoredVersion for two-colored apparel.\nYou'll see a harmless error when the game spawns randomized F3 apparel.");
            }

            if (GetGraphicApparelRecordMethodInfo != null)
            {
                if (null == harm.Patch(GetGraphicApparelRecordMethodInfo,
                    transpiler: new HarmonyMethod(typeof(Patches), nameof(Patches.TryGetGraphicApparelTranspiler))))
                    Verse.Log.Error("<color=grey>[HG]</color> Failed to patch ApparelGraphicRecordSetter:TryGetGraphicApparel for two-colored apparel.\nSome visual functionality will be lost.");
            }
            else
            {
                Verse.Log.Warning("<color=grey>[HG]</color> Failed to patch ApparelGraphicRecordGetter:TryGetGraphicApparel for two-colored apparel.\nSome visual functionality will be lost.");
            }
        }

        public static void TryAllPatchesForSOS2(HarmonyLib.Harmony harm)
        {
            // TEST: Get all shaders
            if (!Verse.ModLister.HasActiveModWithName("Save Our Ship 2"))
            {
                Verse.Log.Message("<color=grey>[HG]</color> <color=#008080FF>Save Our Ship 2</color> not detected.");
                return;
            }

            {
                var sosMod = Verse.ModLister.GetActiveModWithIdentifier("kentington.saveourship2");
                if (sosMod == null)
                {
                    Verse.Log.Message("<color=grey>[HG]</color> <color=#008080FF>Save Our Ship 2</color> metadata not found. If you have both SOS2 and SOS2EXP in the mod list, remove the one you aren't using.");
                    return;
                }
                var authors = sosMod.Authors.Select(s => s.Replace(" ", "")).ToHashSet();
                if (authors.NullOrEmpty())
                {
                    Verse.Log.Message("<color=grey>[HG]</color> <color=#008080FF>Save Our Ship 2</color> author metadata not found.");
                    return;
                }

                /*if (authors.Contains("SonicTHI") && !authors.Contains("Owlchemist"))
                {
                        Verse.Log.Message("<color=grey>[HG]</color> <color=#008080FF>Save Our Ship 2</color> detected. Movement patches will not be performed.");
                        if (sosMod.SupportedVersionsReadOnly.Any(v => v.Minor == 3))
                            Version = VersionOfSOS2.SteamTwoPointSevenOrNewer;
                        else
                            Version = VersionOfSOS2.ExperimentalPostNineties;
                        return;
                }
                else
                {
                    Verse.Log.Message("<color=grey>[HG]</color> Ancient <color=#008080FF>Save Our Ship 2</color> detected. Support for old SOS2 has ended.");
                    Version = VersionOfSOS2.Unknown;
                }*/
            }
        }

        public static bool ComplexRandomGraphicPrefix(Verse.Graphic_Random __instance, Shader newShader, Color newColor, Color newColorTwo, Verse.Graphic __result)
        {
            if (IsComplexCutout(newShader))
            {
                __result = Verse.GraphicDatabase.Get<Verse.Graphic_Random>(
                    __instance.path,
                    newShader,
                    __instance.drawSize,
                    newColor,
                    newColorTwo,
                    __instance.data, null);
#if VERBOSE
                Verse.Log.Message("<color=grey>[HG]</color> ComplexRandomGraphicPrefix: Encountered a complex cutout! " + __instance.path);
                Verse.Log.Message("<color=grey>[HG]</color> The full Graphic_Random type is : " + __instance.GetType().FullName.ToString());
                if (__result is null)
                    Verse.Log.Error("<color=grey>[HG]</color> Failed to obtain graphic!");
#endif
                return !(__result is null);
            } 
            else if (newColorTwo != Color.white)
            {
                Verse.Log.Error("[HG] Graphic_Random prefix: a non-default color two was given, but the graphic doesn't use a cutout shader: " + __instance.path);
            }
            return true;
        }

        public static IEnumerable<CodeInstruction> TryGetGraphicApparelTranspiler(IEnumerable<CodeInstruction> instructions,
ILGenerator generator)
        {
            var list = instructions.ToList();
            // Find GraphicDatabase.Get location
            MethodInfo m_DrawColorTwo = AccessTools.PropertyGetter(typeof(Verse.Thing), "DrawColorTwo");

            MethodInfo m_GraphicDatabaseGetter = AccessTools.Method(typeof(Verse.GraphicDatabase), "Get",
                new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color), typeof(Color) }, new Type[] { typeof(Verse.Graphic_Multi) });

            for (int i = 0; i < list.Count-3; i++)
            {
                // Acquire GraphicDatabase:Get sequence
                if (list[i].operand is MethodInfo originalMethodInfo)
                {
                    if (originalMethodInfo.Name == "Get" && originalMethodInfo.DeclaringType.Name == "GraphicDatabase")
                    {
                        list[i] = new CodeInstruction(OpCodes.Call, m_GraphicDatabaseGetter);
                        list.InsertRange(i, new CodeInstruction[]
                        {
                            new CodeInstruction(OpCodes.Ldarg_0),
                            new CodeInstruction(OpCodes.Callvirt, m_DrawColorTwo)
                        });
                        break;
                    }
                }
            }

            return list;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsComplexCutout(Shader shader)
        {
            return shader == Verse.ShaderDatabase.CutoutComplex;
        }
    }

}

// SCRAPS
//var f_apparelDef = AccessTools.Field(typeof(RimWorld.Apparel), nameof(RimWorld.Apparel.def));
//var f_apparelProps = AccessTools.Field(typeof(Verse.ThingDef), nameof(Verse.ThingDef.apparel));
//var f_apparelTags = AccessTools.Field(typeof(RimWorld.ApparelProperties), nameof(RimWorld.ApparelProperties.tags));
//var f_mobilityCache = AccessTools.Field(typeof(Patches), nameof(Patches._cache_mobilityenabled));
//            var m_containsString = AccessTools.Method(typeof(List<string>), nameof(List<string>.Contains), new Type[] { typeof(string) });