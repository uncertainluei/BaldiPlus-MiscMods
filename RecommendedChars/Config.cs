using BepInEx.Configuration;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    static class RecommendedCharactersConfig
    {
        internal static ConfigEntry<bool> moduleCircle;
        internal static ConfigEntry<bool> moduleGottaBully;
        internal static ConfigEntry<bool> moduleArtsWWires;
#if DEBUG
        internal static ConfigEntry<bool> moduleCaAprilFools;

        internal static ConfigEntry<bool> npcCherryBsoda;
#endif

        internal static ConfigEntry<bool> intendedWiresBehavior;
        internal static ConfigEntry<bool> ogWiresSprites;

        internal static ConfigEntry<bool> guaranteeSpawnChar;


        internal static void BindConfig(ConfigFile config)
        {
            moduleCircle = config.Bind(
                "Modules",
                "Circle",
                true,
                "Adds Circle and Nerf Gun from TCMG's Basics.");
            moduleGottaBully = config.Bind(
                "Modules",
                "GottaBully",
                true,
                "Adds Gotta Bully from Playtime's Swapped Basics.");
            moduleArtsWWires = config.Bind(
                "Modules",
                "ArtsWWires",
                true,
                "Adds Arts with Wires, character sprites and audio from Kinzo/Kracc's 1st Prize Mania and Playtime's Swapped Basics respectively.");
#if DEBUG
            moduleCaAprilFools = config.Bind(
                "Modules",
                "CaAprilFools",
                true,
                "Adds a few features from the April Fools updates from the Chaos Awakens Minecraft mod.");

            npcCherryBsoda = config.Bind(
                "Misc",
                "NpcCherryBsoda",
                false,
                "Cherry BSODA will not push the player, and thus rather act like a BSODA that bounces.");
#endif
            intendedWiresBehavior = config.Bind(
                "Misc",
                "IntendedWiresBehavior",
                true,
                "Arts with Wires will not reset their stare time when the player stops looking at it.");
            ogWiresSprites = config.Bind(
                "Misc",
                "OriginalWiresSprites",
                false,
                "Arts with Wires will use the sprites used in Playtime's Swapped Basics instead.");

            guaranteeSpawnChar = config.Bind(
                "Misc",
                "GuaranteeCharacterSpawn",
                false,
                "All added NPCs will be guaranteed to spawn in their designated floor.");
        }
    }
}
