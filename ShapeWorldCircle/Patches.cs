using HarmonyLib;

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.ShapeWorldCircle.Patches
{
    [HarmonyPatch(typeof(ITM_Scissors), "Use")]
    class ScissorsCannotCutRainbowsPatch
    {
        private static readonly MethodInfo jumpropeCheckMethod = AccessTools.Method(typeof(ScissorsCannotCutRainbowsPatch), "HasCutAnyJumpropes");

        private static bool HasCutAnyJumpropes(PlayerManager pm)
        {
            bool success = false;
            for (int i = pm.jumpropes.Count - 1; i >= 0; i--)
            {
                if (!(pm.jumpropes[i] is CircleJumprope))
                {
                    success = true;
                    pm.jumpropes[i].End(false);
                }
            }
            return success;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;

            CodeInstruction[] array = instructions.ToArray();
            int length = array.Length, i = 0;

            for (; i < length; i++)
            {
                yield return array[i];

                if (array[i].opcode == OpCodes.Ble &&
                    array[i - 1].opcode == OpCodes.Ldc_I4_0 &&
                    array[i - 2].opcode == OpCodes.Callvirt &&
                    array[i - 3].opcode == OpCodes.Ldfld &&
                    array[i - 4].opcode == OpCodes.Ldarg_1)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, jumpropeCheckMethod);
                    yield return new CodeInstruction(OpCodes.Brfalse_S, array[i].operand);
                    break;
                }
            }
            for (i++; i < length; i++)
            {
                if (array[i].opcode == OpCodes.Bgt)
                {
                    patched = true;
                    break;
                }
            }
            for (i++; i < length; i++)
            {
                yield return array[i];
            }

            if (!patched)
                Debug.LogError("Transpiler \"ShapeWorldCircle.ScissorsCannotCutRainbowsPatch.Transpiler\" did not go through!");

            yield break;
        }
    }

    [HarmonyPatch(typeof(Playtime), "EndJumprope")]
    class CircleEndJumpropePatch
    {
        private static void Postfix(Playtime __instance, bool won)
        {
            if (!won && __instance.Character == ShapeWorldCirclePlugin.circleCharEnum)
            {
                // Re-disable the animator for good measure
                __instance.animator.enabled = false;

                CircleNpc circle = (CircleNpc)__instance;
                circle.sprite.sprite = circle.sad;
            }
        }
    }

    [HarmonyPatch(typeof(Playtime), "EndCooldown")]
    class CircleEndCooldownPatch
    {
        private static void Postfix(Playtime __instance)
        {
            if (__instance.Character == ShapeWorldCirclePlugin.circleCharEnum)
            {
                // Re-disable the animator for good measure
                __instance.animator.enabled = false;

                CircleNpc circle = (CircleNpc)__instance;
                circle.sprite.sprite = circle.normal;
            }
        }
    }
}
