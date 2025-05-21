using HarmonyLib;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.PedanticBugFixes.Patches
{
    [HarmonyPatch]
    class ItemUsePatches
    {
        [HarmonyPatch(typeof(ITM_YTPs), "Use"), HarmonyPostfix]
        private static void PointsPickup(ITM_YTPs __instance)
        {
            if (__instance != null)
                GameObject.Destroy(__instance.gameObject);
        }
    }
}
