using BepInEx;

using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;

using UncertainLuei.CaudexLib;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEngine;
using UncertainLuei.CaudexLib.Registers;
using UncertainLuei.CaudexLib.Util.Extensions;

using UncertainLuei.BaldiPlus.ItemFees.Patches;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    [BepInAutoPlugin(ModGuid, "Item Fees")]
    [BepInDependency("io.github.uncertainluei.caudexlib")]
    public partial class ItemFeesPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "io.github.uncertainluei.baldiplus.itemfees";

        internal static string costsConfigPath;
        internal static SoundObject errorSound;

        private void Awake()
        {
            // Create config path
            costsConfigPath = Config.ConfigFilePath;
            costsConfigPath = costsConfigPath.Remove(costsConfigPath.Length - 3) + "json";

            CaudexEvents.OnItemUse += UseItemPatches.UsageYtpPenalty;

            // Store save game system to initialize for later
            ItemFeesSaveGameIO saveGameSystem = new(Info);
            ModdedSaveGame.AddSaveHandler(saveGameSystem);

            // Load localization file
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Lang_En.json"), Language.English);

            LoadingEvents.RegisterOnAssetsLoaded(Info, GetAssets(), LoadingEventOrder.Pre);
            // Costs config is in post so it can deal with extended enums
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad(saveGameSystem), LoadingEventOrder.Post);

            Harmony harmony = new(ModGuid);
            harmony.PatchAllConditionals();
        }

        private IEnumerator GetAssets()
        {
            yield return 2;
            yield return "Grabbing error sound";
            errorSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.name == "ErrorMaybe" && x.GetInstanceID() >= 0);

            errorSound = Instantiate(errorSound);
            errorSound.name = "ErrorMaybe_Item";
            errorSound.soundKey = "ItemFees_Sfx_Error";

            yield return "Creating item description overrides";
            ItemMetaStorage.Instance.FindByEnum(Items.Apple).AddDescOverride(DescOverride);
            ItemMetaStorage.Instance.FindByEnum(Items.BusPass).AddDescOverride(DescOverride);
            ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).AddDescOverride(DescOverride);
            ItemMetaStorage.Instance.Find(x => x.tags.Contains("shape_key")).AddDescOverride(DescOverride);
            yield break;
        }

        private string DescOverride(ItemObject itm, bool localized)
        {
            string result = "ItemFees_" + itm.descKey;
            if (localized)
                result = string.Format(result.Localize(), itm.GetUsageCost());
            return result;
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

        public class ItemFeesSaveGameIO(PluginInfo info) : ModdedSaveGameIOBinary
        {
            public void Ready()
            {
                ready = true;
            }

            private bool ready = false;
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