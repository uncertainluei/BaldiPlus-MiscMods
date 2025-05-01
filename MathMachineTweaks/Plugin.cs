using MTM101BaldAPI.AssetTools;
using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using MTM101BaldAPI;

namespace UncertainLuei.BaldiPlus.MathMachineTweaks
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", BepInDependency.DependencyFlags.HardDependency)]
    public class MathMachineTweaksPlugin : BaseUnityPlugin
    {
        public const string ModName = "Math Machine Tweaks";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.mathmachinetweaks";
        public const string ModVersion = "1.0";

        internal static AssetManager assetMan = new AssetManager();

        // CONFIGURATION
        internal static ConfigEntry<byte> config_mathProblemCount;
        internal static ConfigEntry<bool> config_mathBonusAward;
        internal static ConfigEntry<int> config_mathBonusAwardMinimum;

        void Awake()
        {
            InitConfigValues();

            new Harmony(ModGuid).PatchAllConditionals();
        }

        void InitConfigValues()
        {
            config_mathProblemCount = Config.Bind(
                "Misc.MathMachines",
                "problemCount",
                (byte)1,
                "(1-10) Minimum amount of problems the Math Machines will get by default. If machine's preset value is higher, the setting is ignored.\nDeveloper's note: If you put anything higher than 3, you must be a masochist.");

            config_mathBonusAward = Config.Bind(
                "Misc.MathMachines.BonusAward",
                "enabled",
                true,
                "Awards a rare item if all Bonus Questions are completed.");
            config_mathBonusAwardMinimum = Config.Bind(
                "Misc.MathMachines.BonusAward",
                "minimumMachines",
                5,
                "Minimum number of Math Machines required for Bonus Questions to give an award.");
        }
    }
}