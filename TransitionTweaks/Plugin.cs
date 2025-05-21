using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.TransitionTweaks
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class TransitionTweaksPlugin : BaseUnityPlugin
    {
        public const string ModName = "Transition Tweaks";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.transitiontweaks";
        public const string ModVersion = "1.0";

        // CONFIGURATION
        internal static ConfigEntry<OverrideTransitionMode> config_overrideMode;
        internal static ConfigEntry<float> config_speedMul;

        void Awake()
        {
            InitConfigValues();

            new Harmony(ModGuid).PatchAll();
        }

        void InitConfigValues()
        {
            config_overrideMode = Config.Bind(
                "Transitions",
                "OverrideMode",
                OverrideTransitionMode.Default,
                "The transition type to override all transition types with. Default will not override the type and None flat out removes the transitions altogether.");
            config_speedMul = Config.Bind(
                "Transitions",
                "SpeedMultiplier",
                2F,
                "The speed of the transition in comparison to the original duration.");
        }
    }

    public enum OverrideTransitionMode : sbyte
    {
        Default = -1,
        None = -2,
        SwipeRandom = -3,
        Dither = UiTransition.Dither,
        SwipeLeft = UiTransition.SwipeLeft,
        SwipeRight = UiTransition.SwipeRight,
    }

    [HarmonyPatch(typeof(GlobalCam))]
    [HarmonyPatch("Transition")]
    public class TransitionPatch
    {
        internal static bool Prefix(GlobalCam __instance, ref UiTransition type, ref float duration)
        {
            duration /= TransitionTweaksPlugin.config_speedMul.Value;

            OverrideTransitionMode mode = TransitionTweaksPlugin.config_overrideMode.Value;
            switch (mode)
            {
                case OverrideTransitionMode.None:
                    __instance.EndTransition();
                    return false;
                case OverrideTransitionMode.Default:
                    return true;

                case OverrideTransitionMode.SwipeRandom:
                    type = UiTransition.SwipeLeft + Mathf.RoundToInt(Random.value);
                    return true;

                default:
                    type = (UiTransition)mode;
                    return true;
            }
        }
    }

    [HarmonyPatch(typeof(GlobalCam))]
    [HarmonyPatch("FadeIn")]
    public class FadeInPatch
    {
        internal static bool Prefix(GlobalCam __instance, ref UiTransition type, ref float duration)
        {
            return TransitionPatch.Prefix(__instance, ref type, ref duration);
        }
    }
}