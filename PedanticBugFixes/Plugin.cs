using BepInEx;
using HarmonyLib;

namespace UncertainLuei.BaldiPlus.PedanticBugFixes
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class BugFixesPlugin : BaseUnityPlugin
    {
        public const string ModName = "Pedantic Bug Fixes";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.pedanticbugfixes";
        public const string ModVersion = "1.0";

        void Awake()
        {
            InitConfigValues();

            new Harmony(ModGuid).PatchAll();
        }

        void InitConfigValues()
        {
        }
    }
}