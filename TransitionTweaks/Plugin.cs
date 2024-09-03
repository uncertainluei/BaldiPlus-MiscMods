using System.Collections.Generic;
using System.IO;
using MTM101BaldAPI.AssetTools;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using MTM101BaldAPI;
using UnityEngine.Networking;
using System.Linq;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using System.Reflection;

namespace LuisRandomness.BBPTransitionTweaks
{
    [BepInPlugin(ModGuid, "BB+ Transition Tweaks", ModVersion)]
    public class TransitionTweaksPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "io.github.luisrandomness.bbp_transition_tweaks";
        public const string ModVersion = "2024.1.0.0";

        // CONFIGURATION
        internal static ConfigEntry<OverrideTransitionMode> config_overrideMode;
        internal static ConfigEntry<float> config_speedMul;

        void Awake()
        {
            InitConfigValues();

            new Harmony(ModGuid).PatchAllConditionals();
        }

        void InitConfigValues()
        {
            config_overrideMode = Config.Bind(
                "Transitions",
                "overrideMode",
                OverrideTransitionMode.Default,
                "The transition type to override all transition types with. Default will not override the type and None flat out removes the transitions altogether.");
            config_speedMul = Config.Bind(
                "Transitions",
                "speedMultiplier",
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
        internal static MethodInfo endTransition = AccessTools.Method(typeof(GlobalCam), "EndTransition");

        internal static bool Prefix(GlobalCam __instance, ref UiTransition type, ref float duration)
        {
            duration /= TransitionTweaksPlugin.config_speedMul.Value;

            OverrideTransitionMode mode = TransitionTweaksPlugin.config_overrideMode.Value;
            switch (mode)
            {
                case OverrideTransitionMode.None:
                    endTransition.Invoke(__instance, new object[0]);
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