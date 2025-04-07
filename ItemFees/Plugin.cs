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
        public const string ModVersion = "1.0";

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
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad(saveGameSystem), true);

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
            descOverrides.Add(ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook), "ItemFees_Desc_GrapplingHook");
            yield break;
        }

        private IEnumerator PostLoad(ItemFeesSaveGameIO saveGameSystem)
        {
            yield return 2;
            yield return "Re-adjusting multi-use item prices";
            int price;
            foreach (ItemMetaData meta in ItemMetaStorage.Instance.GetAllWithFlags(ItemFlags.MultipleUse))
            {
                price = meta.value.price;
                meta.itemObjects.Do(x =>
                {
                    if (!x.name.EndsWith("_Tutorial")) // Do not include the tutorial variant
                        x.price = price;
                });
            }
            yield return "Loading costs config file";
            ItemFeesCosts.Reload();
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

            private readonly PluginInfo info;
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
                ItemFeesCosts.ItemCosts.Do(x => hash += ((int)x.Key).ToString() + "_" + x.Value.ToString() + "_");
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