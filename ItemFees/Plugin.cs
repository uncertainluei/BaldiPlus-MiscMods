using BepInEx;

using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    class ItemFeesPlugin : BaseUnityPlugin
    {
        public const string ModName = "Item Fees";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.itemfees";
        public const string ModVersion = "2024.1";

        public static Dictionary<ItemMetaData, string> descOverrides = new Dictionary<ItemMetaData, string>();

        internal static string costsConfigPath;
        internal static SoundObject errorSound;

        private void Awake()
        {
            // Create config path
            costsConfigPath = Config.ConfigFilePath;
            costsConfigPath = costsConfigPath.Remove(costsConfigPath.Length - 3) + "json";

            // Store save game system to initialize for later
            ItemFeesSaveGameIO saveGameSystem = new ItemFeesSaveGameIO(Info);
            ModdedSaveGame.AddSaveHandler(saveGameSystem);

            // Load localization file
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Lang_En.json"), Language.English);

            LoadingEvents.RegisterOnAssetsLoaded(Info, GetAssets(), false);
            // Costs config is in post so it can deal with extended enums
            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadCostsConfig(saveGameSystem), true);

            Harmony harmony = new Harmony(ModGuid);
            harmony.PatchAllConditionals();
        }

        private IEnumerator GetAssets()
        {
            yield return 2;
            yield return "Grabbing error sound";
            errorSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.name == "ErrorMaybe");
            yield return "Creating item description overrides";
            descOverrides.Add(ItemMetaStorage.Instance.FindByEnum(Items.Apple), "ItemFees_Desc_Apple");
            descOverrides.Add(ItemMetaStorage.Instance.FindByEnum(Items.BusPass), "ItemFees_Desc_BusPass");

            // Fix grappling hook cost not being the same as the initial 5-use version
            ItemMetaData grappleMeta = ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook);
            descOverrides.Add(grappleMeta, "ItemFees_Desc_GrapplingHook");
            for (int i = 1; i < 5; i++)
            {
                grappleMeta.itemObjects[i].price = grappleMeta.itemObjects[0].price;
            }
            yield break;
        }

        private IEnumerator LoadCostsConfig(ItemFeesSaveGameIO saveGameSystem)
        {
            yield return 1;
            yield return "Loading costs config file";
            ItemFeesConfig.Reload();
            saveGameSystem.Ready();
            ModdedFileManager.Instance.RegenerateTags();
            yield break;
        }

        public class ItemFeesSaveGameIO : ModdedSaveGameIOBinary
        {
            public ItemFeesSaveGameIO(PluginInfo info)
            {
                this.info = info;
                ready = false;
            }
            
            public void Ready()
            {
                ready = true;
            }

            private PluginInfo info;
            private bool ready;
            public override PluginInfo pluginInfo => info;

            public override void Load(BinaryReader reader)
            {
                reader.ReadByte();
            }

            public override void Reset()
            {
            }

            public override void Save(BinaryWriter writer)
            {
                writer.Write((byte)0);
            }

            public override string[] GenerateTags()
            {
                if (!ready) return new string[0];

                string hash = "";
                ItemFeesConfig.ItemCosts.Do(x => hash += ((int)x.Key).ToString() + "_" + x.Value.ToString() + "_");
                return new string[] { Hash128.Compute(hash).ToString() };
            }

            public override string DisplayTags(string[] tags)
            {
                return $"Config hash: {tags[0]}";
            }

            public override bool TagsReady()
            {
                return ready;
            }
        }
    }
}