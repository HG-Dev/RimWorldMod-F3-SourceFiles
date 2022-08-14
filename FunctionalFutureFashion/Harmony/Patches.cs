using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace HG.FFF.Harmony
{
    /// <summary>
    /// This patches the HasSpaceSuitSlow method in SOS2 so that OnSkin apparel can benefit from the EVA tag.
    /// </summary>
    public class Patches
    {
        public static MethodInfo OldSpaceSuitMethodInfo;
        public static MethodInfo ModernSpaceSuitMethodInfo;
        public static MethodInfo GoFastMethodInfo;
        public static MethodInfo TwoColorGetColoredVersionMethodInfo;

        public static HashSet<int> _cache_mobilityenabled = new HashSet<int>();

        public static void TryAllPatchesForSOS2(HarmonyLib.Harmony harm)
        {
            //Verse.Graphic_Random.GetColoredVersion(Shader, Color, Color)
            Verse.Log.Message("Try all patches");
            TwoColorGetColoredVersionMethodInfo = AccessTools.Method("Verse.Graphic_Random:GetColoredVersion", new Type[] { typeof(Shader), typeof(Color), typeof(Color) });
                                                  //AccessTools.Method("Verse.Graphic_Random.GetColoredVersion", new Type[] { typeof(Shader), typeof(Color), typeof(Color) });

            if (TwoColorGetColoredVersionMethodInfo != null)
            {
                if (null == harm.Patch(TwoColorGetColoredVersionMethodInfo,
                    prefix: new HarmonyMethod(typeof(Patches), nameof(Patches.ComplexRandomGraphicPrefix))));
                Verse.Log.Error("<color=grey>[HG]</color> Failed to patch Graphic_Random:GetColoredVersion for two-colored apparel (could be annoying)");
            }
            else
            {
                Verse.Log.Warning("<color=grey>[HG]</color> Failed to patch Graphic_Random:GetColoredVersion for two-colored apparel (could be annoying)");
            }

            if (Verse.ModLister.HasActiveModWithName("Save Our Ship 2"))
            {
                Verse.Log.Message("<color=grey>[HG]</color> Performing SOS2 patches");
                OldSpaceSuitMethodInfo = AccessTools.Method("SaveOurShip2.ShipInteriorMod2:HasSpaceSuitSlow", new Type[] { typeof(Verse.Pawn) });
                ModernSpaceSuitMethodInfo = AccessTools.Method("SaveOurShip2.ShipInteriorMod2:EVAlevelSlow", new Type[] { typeof(Verse.Pawn) });
                GoFastMethodInfo = AccessTools.Method("SaveOurShip2.H_SpaceZoomies:GoFast", new Type[] { typeof(Verse.AI.Pawn_PathFollower), typeof(Verse.Pawn) });
                
                if (ModernSpaceSuitMethodInfo != null)
                {
                    if (null == harm.Patch(ModernSpaceSuitMethodInfo,
                        transpiler: new HarmonyMethod(typeof(Patches), nameof(HasSpaceSuitSlowTranspilerMK3))))
                        Verse.Log.Error("<color=grey>[HG]</color> Failed to patch SOS2 EVAlevelSlow method");
                }
                else if (OldSpaceSuitMethodInfo != null)
                {
                    if (null == harm.Patch(OldSpaceSuitMethodInfo,
                        transpiler: new HarmonyMethod(typeof(Patches), nameof(HasSpaceSuitSlowTranspilerMK3))))
                        Verse.Log.Error("<color=grey>[HG]</color> Failed to patch SOS2 HasSpaceSuitSlow method");
                }
                else
                {
                    Verse.Log.Error("<color=grey>[HG]</color> Could not get a lock on SOS2 EVA test method");
                }

                if (GoFastMethodInfo == null)
                    Verse.Log.Error("<color=grey>[HG]</color> Could not get a lock on SOS2 GoFast method");
                else
                {
                    if (null == harm.Patch(GoFastMethodInfo,
                        transpiler: new HarmonyMethod(typeof(Patches), nameof(Patches.GoFasterTranspiler))))
                        Verse.Log.Error("<color=grey>[HG]</color> Failed to patch SOS2 GoFast method");
                }
            }
            else
            {
                //SOS2 was not detected
            }
        }

        public static IEnumerable<CodeInstruction> HasSpaceSuitSlowTranspilerMK3(IEnumerable<CodeInstruction> instructions,
    ILGenerator generator)
        {
            var list = instructions.ToList();
            //Verse.Log.Message("[HG] Beginning HasSpaceSuitSlowTranspilerMK3");

            // Create and initialize mobility assist found flag
            var v_mobilityAssistFound = generator.DeclareLocal(typeof(bool));
            var boolInitIndex = list.FindIndex(c => c.opcode == OpCodes.Stloc_1) + 1;
            //Verse.Log.Message("[HG] Mobility assist flag created at index " + boolInitIndex.ToString());
            list.InsertRange(boolInitIndex, new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Stloc, v_mobilityAssistFound)
            });

            var evaLandmarkIndex = list.FindIndex(op => op.opcode == OpCodes.Ldstr && op.operand.ToString() == "EVA");
            //Verse.Log.Message("[HG] Found EVA landmark index at " + evaLandmarkIndex.ToString());
            object o_apparel = null;

            int landmarkSearchState = 0;
            int i_LOAD_APPAREL = 0;
            int i_SUIT_FOUND = 0;
            int i_NO_HELMET_FOUND_BRANCH = 0;
            int i_SUIT_CHECK_START = 0;
            int i_HELMET_FOUND = 0;
            int i_ENDFINALLY = 0;

            for (int i = evaLandmarkIndex; i < list.Count && i_ENDFINALLY == 0; i++)
            {
                switch (landmarkSearchState)
                {
                    case 1:
                        // Load apparel. This differs between versions,
                        // so make sure we have the right one by looking for
                        // a following load field.
                        if (list[i].opcode == OpCodes.Ldloc_S && list[i+1].opcode == OpCodes.Ldfld)
                        {
                            i_LOAD_APPAREL = i;
                            o_apparel = list[i].operand;
                            landmarkSearchState++;
                        }
                        break;
                    case 0:
                        // Where we load "one" onto the stack
                        // to save to the helmet found flag.
                        if (list[i].opcode == OpCodes.Ldc_I4_1)
                        {
                            i_HELMET_FOUND = i;
                            i_NO_HELMET_FOUND_BRANCH = i - 1;
                            landmarkSearchState++;
                        }
                        break;
                    case 2:
                        // The "load apparel" immediately following "helmet found."
                        if (list[i].opcode == OpCodes.Ldloc_S)
                        {
                            if (list[i].operand != o_apparel)
                            {
                                Verse.Log.Error("<color=grey>[HG]</color> Failed to patch SOS2 EVAlevelSlow method: o_apparel mismatch");
                                return instructions;
                            }
                            i_SUIT_CHECK_START = i;
                            landmarkSearchState++;
                        }
                        break;
                    case 3:
                        if (list[i].opcode == OpCodes.Ldc_I4_1)
                        {
                            i_SUIT_FOUND = i;
                            landmarkSearchState++;
                        }
                        break;
                    case 4:
                        if (list[i].opcode == OpCodes.Endfinally)
                        {
                            i_ENDFINALLY = i;
                            landmarkSearchState++;
                        }
                        break;
                }
            }

            if (i_ENDFINALLY == 0)
            {
                Verse.Log.Error("<color=grey>[HG]</color> Unable to find all spacesuit check landmark instructions. Has SOS2 been updated?");
                return instructions;
            }
            /*else
            {
                Verse.Log.Message("<color=grey>[HG]</color> Space Suit Transpiler: Landmark search finished.");
                Verse.Log.Message($"{i_HELMET_FOUND} {i_LOAD_APPAREL} {i_SUIT_CHECK_START} {i_SUIT_FOUND} {i_ENDFINALLY}");
            }*/

            Label l_checkMobility = generator.DefineLabel();
            Label l_checkHypermesh = generator.DefineLabel();
            Label l_evaSuitFound = generator.DefineLabel();
            list[i_NO_HELMET_FOUND_BRANCH].operand = l_checkMobility;
            list[i_SUIT_FOUND].labels.Add(l_evaSuitFound);

            var f_pawnId = AccessTools.Field(typeof(Verse.Pawn), nameof(Verse.Pawn.thingIDNumber));

            var m_isHypermesh = AccessTools.Method(typeof(Patches), nameof(Patches.IsApparelHypermesh), new Type[] { typeof(RimWorld.Apparel) });
            var m_isMobilityAssist = AccessTools.Method(typeof(Patches), nameof(Patches.IsApparelMobilityAssist), new Type[] { typeof(RimWorld.Apparel) });
            var m_updateMobilityCache = AccessTools.Method(typeof(Patches), nameof(Patches.UpdateMobilityCache), new Type[] { typeof(int), typeof(bool) });

            var checkInstructions = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldloc_S, o_apparel),
                new CodeInstruction(OpCodes.Call, m_isMobilityAssist),
                new CodeInstruction(OpCodes.Brfalse_S, l_checkHypermesh),
                new CodeInstruction(OpCodes.Ldc_I4_1),
                new CodeInstruction(OpCodes.Stloc, v_mobilityAssistFound),
                new CodeInstruction(OpCodes.Ldloc_S, o_apparel),
                new CodeInstruction(OpCodes.Call, m_isHypermesh),
                new CodeInstruction(OpCodes.Brtrue_S, l_evaSuitFound),
            };

            checkInstructions.First(c => c.opcode == OpCodes.Ldloc_S).labels.Add(l_checkMobility);
            checkInstructions.Last(c => c.opcode == OpCodes.Ldloc_S).labels.Add(l_checkHypermesh);
            //checkInstructions[0].labels.Add(l_checkMobility);
            //checkInstructions[5].labels.Add(l_checkHypermesh);

            // Cache mobility
            var cacheMobilityInstructions = new List<CodeInstruction>()
            {
                new CodeInstruction(OpCodes.Ldarg_0), // Load reference to pawn
                new CodeInstruction(OpCodes.Ldfld, f_pawnId),
                new CodeInstruction(OpCodes.Ldloc, v_mobilityAssistFound),
                new CodeInstruction(OpCodes.Call, m_updateMobilityCache)
            };

            list.InsertRange(i_ENDFINALLY, cacheMobilityInstructions);
            list.InsertRange(i_SUIT_CHECK_START, checkInstructions);

            return list;
        }

        public static void UpdateMobilityCache(int pawnId, bool enabled)
        {
            if (enabled)
                _cache_mobilityenabled.Add(pawnId);
            else
                _cache_mobilityenabled.Remove(pawnId);
        }

        public static IEnumerable<CodeInstruction> GoFasterTranspiler(IEnumerable<CodeInstruction> instructions,
    ILGenerator generator)
        {
            var list = instructions.ToList();

            var v_nodeAcceleration = generator.DeclareLocal(typeof(int));
            var v_divisor = generator.DeclareLocal(typeof(float));

            // Get final brfalse.s
            var insertionIndex = list.FindLastIndex(op => op.opcode == OpCodes.Brfalse_S);

            // If the final brfalse was not found and/or the opcode following it is not Ldarg_0
            if (insertionIndex <= 0)
            {
                Verse.Log.Error("[HG] The composition of SOS2::GoFast has changed (Brfalse not found). Cannot finish patch.");
                return list;
            }

            insertionIndex++;

            if (list[insertionIndex].opcode != OpCodes.Ldarg_0 && list[insertionIndex + 1].opcode != OpCodes.Ldarg_0)
            {
                Verse.Log.Error($"[HG] The composition of SOS2::GoFast has changed (insertionIndex {insertionIndex} points to {list[insertionIndex].opcode}). Cannot finish patch.");
                return list;
            }

            List<string> problemsFound = new List<string>();
            Label l_clampValue = generator.DefineLabel();
            Label l_executeDivision = generator.DefineLabel();
            list[insertionIndex].labels.Add(l_executeDivision);

            FieldInfo f_currentPath = AccessTools.Field(typeof(Verse.AI.Pawn_PathFollower), nameof(Verse.AI.Pawn_PathFollower.curPath));

            if (f_currentPath == null)
                problemsFound.Add("f_currentPath");

            MethodInfo m_isMobilityAssisted = AccessTools.Method(typeof(Patches), nameof(Patches.isMobilityAssisted), new Type[] { typeof(Verse.Pawn) });
            MethodInfo m_debugMessage = AccessTools.Method(typeof(Patches), nameof(Patches.printFloat), new Type[] { typeof(float) });
            MethodInfo m_intToFloat = AccessTools.Method(typeof(System.Convert), nameof(System.Convert.ToSingle), new Type[] { typeof(int) });
            MethodInfo m_getPathNodesLeft = typeof(Verse.AI.PawnPath).GetMethod($"get_{nameof(Verse.AI.PawnPath.NodesLeftCount)}");
            MethodInfo m_getPathNodesConsumed = typeof(Verse.AI.PawnPath).GetMethod($"get_{nameof(Verse.AI.PawnPath.NodesConsumedCount)}");
            MethodInfo m_clamp = typeof(Mathf).GetMethod(nameof(Mathf.Clamp), new Type[] { typeof(float), typeof(float), typeof(float) });

            if (m_getPathNodesLeft == null)
                problemsFound.Add("m_getPathNodesLeft problem");

            if (problemsFound.Count > 0)
            {
                Verse.Log.Error("[HG] Cannot finish SOS2::GoFast patch: " + string.Join(",", problemsFound));
                return list;
            }

            byte totalLoadFloats = 0;
            for (int i = insertionIndex; i < list.Count; i++)
            {
                if (list[i].opcode == OpCodes.Ldc_R4)
                {
                    list[i].opcode = OpCodes.Ldloc;
                    list[i].operand = v_divisor;
                    totalLoadFloats++;

                    if (totalLoadFloats >= 2)
                        break;
                }
            }

            var additionalInstructions = new CodeInstruction[]
            {
                new CodeInstruction(OpCodes.Ldc_R4, 4f), // Prepare divisor
                new CodeInstruction(OpCodes.Stloc, v_divisor),

                new CodeInstruction(OpCodes.Ldarg_1), // Get reference to pawn
                new CodeInstruction(OpCodes.Call, m_isMobilityAssisted),
                new CodeInstruction(OpCodes.Brfalse, l_executeDivision), // If just wearing EVA equipment, jump to division

                new CodeInstruction(OpCodes.Ldarg_0), // Get reference to path
                new CodeInstruction(OpCodes.Ldfld, f_currentPath),
                new CodeInstruction(OpCodes.Call, m_getPathNodesConsumed), // Begin by assuming we are revving up
                new CodeInstruction(OpCodes.Dup), // Duplicate the value for the comparison afterwards
                new CodeInstruction(OpCodes.Stloc, v_nodeAcceleration), // Save PathNodesConsumed for now
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, f_currentPath),
                new CodeInstruction(OpCodes.Call, m_getPathNodesLeft),
                new CodeInstruction(OpCodes.Cgt), // If nodes left is bigger than nodes consumed, ignore it
                new CodeInstruction(OpCodes.Brfalse, l_clampValue),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldfld, f_currentPath),
                new CodeInstruction(OpCodes.Call, m_getPathNodesLeft),
                new CodeInstruction(OpCodes.Stloc, v_nodeAcceleration), // Save PathNodesLeft since it was smaller
                new CodeInstruction(OpCodes.Nop),
                new CodeInstruction(OpCodes.Ldloc, v_nodeAcceleration),
                new CodeInstruction(OpCodes.Call, m_intToFloat),
                new CodeInstruction(OpCodes.Ldc_R4, 4f),
                new CodeInstruction(OpCodes.Ldc_R4, 16f),
                new CodeInstruction(OpCodes.Call, m_clamp), // Clamp the value so that it's between 4 (EVA default) and 12
                new CodeInstruction(OpCodes.Stloc, v_divisor)

                // Apply clamp to divisor
            };

            additionalInstructions.First(c => c.opcode == OpCodes.Nop).labels.Add(l_clampValue);

            list.InsertRange(insertionIndex, additionalInstructions);

            return list;
        }

        public static bool ComplexRandomGraphicPrefix(Verse.Graphic_Random __instance, Verse.Graphic __result, ref Shader newShader, ref Color newColor, ref Color newColorTwo)
        {
            Verse.Log.Message("Congrats!");
            if (IsComplexCutout(newShader))
            {
                __result = Verse.GraphicDatabase.Get<Verse.Graphic_Random>(
                    __instance.path,
                    newShader,
                    __instance.drawSize,
                    newColor,
                    newColorTwo,
                    __instance.data, null);
                return false;
            }
            return true;
        }
        /*
ILGenerator generator)
        {
            var list = instructions.ToList();
            var m_isCutoutComplexShader = AccessTools.Method(typeof(Patches), nameof(Patches.IsComplexCutout), new Type[] { typeof(int), typeof(bool) });
            Label l_originalIm = generator.DefineLabel();

            // Begin by examining the shader: if it is a ComplexCutout, allow for the use of colorTwo.
            var additionalInstructions = new CodeInstruction[]
            {
                // Load shader reference
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, m_isCutoutComplexShader),
                new CodeInstruction(OpCodes.Brfalse, l_executeDivision), // If just wearing EVA equipment, jump to division

                // Apply clamp to divisor
            };

            return list;
        }*/

        public static void printFloat(float value)
        {
            if (value > 4f)
                Verse.Log.Message(value.ToString());
        }

        public static bool isMobilityAssisted(Verse.Pawn pawn)
        {
            return _cache_mobilityenabled.Contains(pawn.thingIDNumber);
        }

        public static bool IsApparelMobilityAssist(RimWorld.Apparel testApparel)
        {
            return testApparel.def.apparel.tags.Contains("Mobility");
        }

        public static bool IsApparelHypermesh(RimWorld.Apparel testApparel)
        {
            return testApparel.def.apparel.tags.Contains("Hypermesh");
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