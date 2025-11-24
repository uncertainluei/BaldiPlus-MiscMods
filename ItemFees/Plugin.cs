using BepInEx;

using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;

using PlusLevelStudio;
using PlusStudioLevelLoader;

using System.Collections;
using System.IO;
using System.Linq;

using UnityEngine;
using UncertainLuei.CaudexLib.Registers;
using UncertainLuei.CaudexLib.Util;
using UncertainLuei.CaudexLib.Util.Extensions;

using UncertainLuei.BaldiPlus.ItemFees.Patches;
using System;
using PlusLevelStudio.Editor;
using PlusLevelStudio.Editor.Tools;
using BepInEx.Bootstrap;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    [BepInAutoPlugin(ModGuid, "Item Fees")]
    [BepInDependency("io.github.uncertainluei.caudexlib", "0.1.2")]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    public partial class ItemFeesPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "io.github.uncertainluei.baldiplus.itemfees";

        internal static string costsConfigPath;
        internal static SoundObject errorSound;

        internal static Sticker itemBonusSticker;
        internal static SwingDoor_Points[] pointDoors = new SwingDoor_Points[2];

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
            CaudexAssetLoader.LocalizationFromEmbeddedResource(Language.English, "Lang/English.json5");

            LoadingEvents.RegisterOnAssetsLoaded(Info, CreateAssets(), LoadingEventOrder.Pre);

            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudioloader"))
                LoadingEvents.RegisterOnAssetsLoaded(Info, RegisterToLoader(), LoadingEventOrder.Pre);
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
                LoadingEvents.RegisterOnAssetsLoaded(Info, RegisterToStudio(), LoadingEventOrder.Pre);

            GeneratorManagement.Register(this, GenerationModType.Addend, AddStickerToGenerator);
            // Costs config is in post so it can deal with extended enums
            LoadingEvents.RegisterOnAssetsLoaded(Info, PostLoad(saveGameSystem), LoadingEventOrder.Post);

            Harmony harmony = new(ModGuid);
            harmony.PatchAllConditionals();
        }

        private IEnumerator CreateAssets()
        {
            yield return 4;
            yield return "Grabbing error sound";
            errorSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.name == "ErrorMaybe" && x.GetInstanceID() >= 0);

            errorSound = Instantiate(errorSound);
            errorSound.name = "ErrorMaybe_Item";
            errorSound.soundKey = "ItemFees_Sfx_Error";

            yield return "Creating item description overrides";

            static string descOverride(ItemObject itm, bool localized)
            {
                string result = "ItemFees_" + itm.descKey;
                if (localized)
                    result = string.Format(result.Localize(), itm.GetUsageCost());
                return result;
            }
            ItemMetaStorage.Instance.FindByEnum(Items.Apple).AddDescOverride(descOverride);
            ItemMetaStorage.Instance.FindByEnum(Items.BusPass).AddDescOverride(descOverride);
            ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).AddDescOverride(descOverride);
            ItemMetaStorage.Instance.Find(x => x.tags.Contains("shape_key")).AddDescOverride(descOverride);

            yield return "Adding point doors";
            var swingDoor = Resources.FindObjectsOfTypeAll<SwingDoor>().First(x => x.GetInstanceID() >= 0 && x.name == "Door_Swinging");
            swingDoor = Instantiate(swingDoor, MTM101BaldiDevAPI.prefabTransform);
            pointDoors[0] = swingDoor.gameObject.SwapComponent<SwingDoor, SwingDoor_Points>(swingDoor, false);
            pointDoors[0].name = "PointDoor_250";
            pointDoors[0].requiredPoints = 250;
            pointDoors[0].pointDoorOverlay = new Material(pointDoors[0].overlayShut[0]);
            pointDoors[0].pointDoorOverlay.name = "PointDoor_250";
            pointDoors[0].pointDoorOverlay.mainTexture = CaudexAssetLoader.TextureFromEmbeddedResource("PointDoor_250.png");

            pointDoors[1] = Instantiate(pointDoors[0], MTM101BaldiDevAPI.prefabTransform);
            pointDoors[1].name = "PointDoor_500";
            pointDoors[1].requiredPoints = 500;
            pointDoors[1].pointDoorOverlay = new Material(pointDoors[0].pointDoorOverlay);
            pointDoors[1].pointDoorOverlay.name = "PointDoor_500";
            pointDoors[1].pointDoorOverlay.mainTexture = CaudexAssetLoader.TextureFromEmbeddedResource("PointDoor_500.png");

            yield return "Adding sticker";
            itemBonusSticker = new StickerBuilder<ExtendedStickerData>(Info)
            .SetSprite(AssetLoader.SpriteFromTexture2D(CaudexAssetLoader.TextureFromEmbeddedResource("Sticker_ItemBonus.png"), 100f))
            .SetEnum("ItemFees_ItemBonus")
            .SetAsBonusSticker()
            .SetDuplicateOddsMultiplier(0.5f)
            .SetValueCap(10)
            .Build().sticker;
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

        private void AddStickerToGenerator(string name, int id, SceneObject sceneObj)
        {
            if (sceneObj.GetMeta()?.tags.Contains("endless") != true)
            {
                sceneObj.potentialStickers = sceneObj.potentialStickers.AddToArray(new WeightedSticker(itemBonusSticker, 100));
                sceneObj.MarkAsNeverUnload();
            }
        }

        private static IEnumerator RegisterToLoader()
        {
            yield return 1;
            yield return "Adding support for Level Loader";
            LevelLoaderPlugin.Instance.tileBasedObjectPrefabs.Add("itemfees_pointdoor_250", pointDoors[0]);
            LevelLoaderPlugin.Instance.tileBasedObjectPrefabs.Add("itemfees_pointdoor_500", pointDoors[1]);
            LevelLoaderPlugin.Instance.stickerAliases.Add("itemfees_itembonus", itemBonusSticker);
        }

        private static IEnumerator RegisterToStudio()
        {
            yield return 1;
            yield return "Adding support for Level Studio";
            LevelStudioPlugin.Instance.stickerSprites.Add("itemfees_itembonus",
                AssetLoader.SpriteFromTexture2D(CaudexAssetLoader.TextureFromEmbeddedResource("Editor/Sticker_ItemBonus.png"), 100f));
            LevelStudioPlugin.Instance.selectableStickers.Add("itemfees_itembonus");

            EditorInterface.AddDoor<DoorDisplay>("itemfees_pointdoor_250", DoorIngameStatus.AlwaysObject, pointDoors[0].mask[0], [pointDoors[0].pointDoorOverlay, pointDoors[0].pointDoorOverlay]);
            EditorInterface.AddDoor<DoorDisplay>("itemfees_pointdoor_500", DoorIngameStatus.AlwaysObject, pointDoors[1].mask[0], [pointDoors[1].pointDoorOverlay, pointDoors[1].pointDoorOverlay]);

            EditorInterfaceModes.AddModeCallback((EditorMode mode, bool includeNonCompliant) => {
                EditorInterfaceModes.InsertToolsInCategory(mode, "doors", "door_coinswinging", [
                    new DoorTool("itemfees_pointdoor_250", AssetLoader.SpriteFromTexture2D(CaudexAssetLoader.TextureFromEmbeddedResource("Editor/PointDoor_250.png"), 100f)),
                    new DoorTool("itemfees_pointdoor_500", AssetLoader.SpriteFromTexture2D(CaudexAssetLoader.TextureFromEmbeddedResource("Editor/PointDoor_500.png"), 100f))
                ]);
            });
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
                if (!ready) return [];

                string hash = "";
                ItemFeesCosts.ItemCosts.Do(x => hash += ((int)x.Key).ToString() + "_" + x.Value.ToString() + "_");
                return [Hash128.Compute(hash).ToString()];
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