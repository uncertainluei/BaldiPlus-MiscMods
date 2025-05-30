using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.PedanticBugFixes.Patches
{
    [HarmonyPatch(typeof(EnvironmentController), "TilesLeftToRight")]
    class MultiPosterFix
    {
        private static bool IsCellAvailable(Cell cell, Direction dir) => !Directions.OpenDirectionsFromBin(cell.ConstBin).Contains(dir) && !cell.WallSoftCovered(dir);
        private static readonly MethodInfo cellCoverInfo = AccessTools.Method(typeof(MultiPosterFix), "IsCellAvailable");

#if false
        private static void Postfix(EnvironmentController __instance, ref List<Cell> __result, Direction dir)
        {
            Debug.Log($"TilesLeftToRight executued, length {__result.Count}, at direction {dir}");
            foreach (Cell cell1 in __result)
            {
                cell1.tile.meshRenderer.material.mainTexture = __instance.tilePre.meshRenderer.material.mainTexture;
                Debug.Log(cell1.position.x + ", " + cell1.position.z);
            }
            Debug.Log("");
        }
#endif

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            byte patches = 0;
            CodeInstruction[] array = instructions.ToArray();
            int length = array.Length;

            for (int i = 0; i < length; i++)
            {
                if (patches < 2 &&
                    array[i].opcode == OpCodes.Brfalse &&
                    array[i - 1].opcode == OpCodes.Callvirt &&
                    array[i - 2].opcode == OpCodes.Ldarg_2 &&
                    array[i - 3].opcode == OpCodes.Ldloc_3 &&
                    array[i - 4].opcode == OpCodes.Brfalse)
                {
                    patches++;
                    yield return new CodeInstruction(OpCodes.Brtrue)
                    {
                        operand = array[i-4].operand
                    };
                    // && IsCellCovered()
                    yield return new CodeInstruction(OpCodes.Ldloc_3);
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Call, cellCoverInfo);
                    yield return new CodeInstruction(OpCodes.Brtrue)
                    {
                        operand = array[i].operand
                    };
                    continue;
                }
                yield return array[i];
            }

            if (patches < 2)
                Debug.LogError("Transpiler \"PedanticBugFixes.MultiPosterFix.Transpiler\" did not go through!");

            yield break;
        }
    }
}
