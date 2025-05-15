using BepInEx;
using BepInEx.Configuration;

using HarmonyLib;

using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.UI;

using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;

using UncertainLuei.BaldiPlus.RecommendedChars.Patches;

using UnityEngine;
using UnityEngine.UI;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public abstract class Module
    {
        // These three are here for convenience sake
        protected static AssetManager AssetMan => RecommendedCharsPlugin.AssetMan;
        internal static RecommendedCharsPlugin Plugin => RecommendedCharsPlugin.Plugin;
        internal static PluginInfo Info => Plugin.Info;

        public bool Enabled => ConfigEntry != null && ConfigEntry.Value;
        public abstract string Name { get; }
        public virtual string SaveTag => Name;
        protected abstract ConfigEntry<bool> ConfigEntry { get; }

        public virtual Action LoadAction => null;
        public virtual Action<string, int, SceneObject> FloorAddendAction => null;
        public virtual Action<string, int, CustomLevelObject> LevelAddendAction => null;
        public virtual Action<FieldTrips, FieldTripLoot> FieldTripLootAction => null;
    }

    public class Module_Circle : Module
    {
        public override string Name => "TCMGB Circle";

        public override Action LoadAction => Load;
        public override Action<string, int, SceneObject> FloorAddendAction => FloorAddend;
        public override Action<string, int, CustomLevelObject> LevelAddendAction => FloorAddendLvl;
        public override Action<FieldTrips, FieldTripLoot> FieldTripLootAction => FieldTripLootChange;

        protected override ConfigEntry<bool> ConfigEntry => RecommendedCharactersConfig.moduleCircle;

        private void Load()
        {
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Item", "NerfGun"), x => "NerfGun/" + x.name);
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Npc", "Circle"), x => "CircleTex/" + x.name);

            RecommendedCharsPlugin.AddAudioClipsToAssetMan(Path.Combine(AssetLoader.GetModPath(Plugin), "Audio", "Circle"), "CircleAud/");

            LoadNerfGun();            
            LoadCircle();
        }
        private void LoadNerfGun()
        {
            ItemMetaData nerfGunMeta = new ItemMetaData(Info, new ItemObject[0])
            {
                flags = ItemFlags.MultipleUse
            };

            Items nerfGunEnum = EnumExtensions.ExtendEnum<Items>("RecChars_NerfGun");

            ItemBuilder nerfGunBuilder = new ItemBuilder(Info)
            .SetNameAndDescription("RecChars_Itm_NerfGun2", "RecChars_Desc_NerfGun")
            .SetEnum(nerfGunEnum)
            .SetMeta(nerfGunMeta)
            .SetSprites(AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("NerfGun/NerfGun_Small"), 25f), AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("NerfGun/NerfGun_Large"), 50f))
            .SetShopPrice(750)
            .SetGeneratorCost(75)
            .SetItemComponent<ITM_NerfGun>();

            ItemObject nerfItm = nerfGunBuilder.Build();
            nerfItm.name = "RecChars NerfGun2";
            nerfItm.item.name = "Itm_NerfGun2";
            AssetMan.Add("NerfGunItem", nerfItm);

            nerfGunBuilder.SetNameAndDescription("RecChars_Itm_NerfGun1", "RecChars_Desc_NerfGun");
            ItemObject nerfItm1 = nerfGunBuilder.Build();
            nerfItm1.name = "RecChars NerfGun1";
            nerfItm1.item.name = "Itm_NerfGun1";
            ((ITM_NerfGun)nerfItm.item).nextStage = nerfItm1;

            AssetMan.Add("NerfGunPoster", ObjectCreators.CreatePosterObject(AssetMan.Get<Texture2D>("NerfGun/hnt_nerfgun"), new PosterTextData[0]));
            AssetMan.Get<PosterObject>("NerfGunPoster").name = "NerfGunPoster";

            // Reverse itemObject list so the (2) variant is always selected first
            nerfGunMeta.itemObjects = nerfGunMeta.itemObjects.Reverse().ToArray();
        }

        private void LoadCircle()
        {
            CircleNpc circle = RecommendedCharsPlugin.CloneComponent<Playtime, CircleNpc>(GameObject.Instantiate((Playtime)NPCMetaStorage.Instance.Get(Character.Playtime).value, MTM101BaldiDevAPI.prefabTransform));
            circle.name = "ShapeWorld Circle";

            CircleNpc.charEnum = EnumExtensions.ExtendEnum<Character>("RecChars_Circle");
            PineDebugNpcIconPatch.icons.Add(CircleNpc.charEnum, AssetMan.Get<Texture2D>("CircleTex/BorderCircle"));

            circle.character = CircleNpc.charEnum;
            circle.looker.npc = circle;
            circle.navigator.npc = circle;

            circle.animator.enabled = false;

            circle.audMan.subtitleColor = new Color(52f / 255f, 182f / 255f, 69f / 255f);
            circle.audCount = new SoundObject[9];
            for (int i = 0; i < 9; i++)
                circle.audCount[i] = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>($"CircleAud/Circle_{i + 1}"), $"Vfx_Playtime_{i + 1}", SoundType.Voice, circle.audMan.subtitleColor);

            circle.audLetsPlay = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_LetsPlay"), "Vfx_Playtime_LetsPlay", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audGo = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_ReadyGo"), "RecChars_Circle_Go", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audOops = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Oops"), "RecChars_Circle_Oops", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audCongrats = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Congrats"), "RecChars_Circle_Congrats", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audSad = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Sad"), "RecChars_Circle_Sad", SoundType.Voice, circle.audMan.subtitleColor);

            circle.audCalls = new SoundObject[]
            {
                ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Random1"), "RecChars_Circle_Random", SoundType.Voice, circle.audMan.subtitleColor),
                ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Random2"), "RecChars_Circle_Random", SoundType.Voice, circle.audMan.subtitleColor)
            };

            PropagatedAudioManager music = circle.GetComponents<PropagatedAudioManager>()[1];
            music.soundOnStart[0] = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("CircleAud/Circle_Music"), "RecChars_Circle_Music", SoundType.Effect, circle.audMan.subtitleColor);
            music.subtitleColor = circle.audMan.subtitleColor = new Color(52f / 255f, 182f / 255f, 69f / 255f);

            // The default speed was 500 but this should flow better in-game
            circle.normSpeed = 90f;
            circle.runSpeed = 90f;

            circle.poster = ObjectCreators.CreateCharacterPoster(AssetMan.Get<Texture2D>("CircleTex/pri_circle"), "RecChars_Pst_Circle1", "RecChars_Pst_Circle2");
            circle.poster.textData[1].font = BaldiFonts.ComicSans18.FontAsset();
            circle.poster.textData[1].fontSize = 18;
            circle.poster.name = "CirclePoster";

            circle.sprite = circle.spriteRenderer[0];
            Sprite[] sprites = AssetLoader.SpritesFromSpritesheet(2, 1, 100f, new Vector2(0.5f, 0.5f), AssetMan.Get<Texture2D>("CircleTex/CircleSprites"));
            circle.sprNormal = sprites[0];
            circle.sprite.sprite = circle.sprNormal;

            circle.sprSad = sprites[1];

            CircleJumprope jumprope = RecommendedCharsPlugin.CloneComponent<Jumprope, CircleJumprope>(GameObject.Instantiate(circle.jumpropePre, MTM101BaldiDevAPI.prefabTransform));
            circle.jumpropePre = jumprope;

            jumprope.name = "ShapeWorld Circle_Jumprope";
            jumprope.ropeAnimator = jumprope.animator.gameObject.AddComponent<CustomSpriteAnimator>();
            jumprope.ropeAnimator.spriteRenderer = jumprope.ropeAnimator.GetComponentInChildren<SpriteRenderer>();
            CircleJumprope.ropeAnimation = new Dictionary<string, Sprite[]> { { "JumpRope", AssetLoader.SpritesFromSpritesheet(4, 4, 1f, new Vector2(0.5f, 0.5f), AssetMan.Get<Texture2D>("CircleTex/CircleRainbow")) } };
            jumprope.ropeDelay = 0f;
            jumprope.ropeTime = 1f;
            jumprope.maxJumps = 10;
            jumprope.startVal = 100;
            jumprope.penaltyVal = 10;

            AssetMan.Add("CircleNpc", circle);
            NPCMetadata circleMeta = new NPCMetadata(Info, new NPC[] { circle }, circle.name, NPCMetaStorage.Instance.Get(Character.Playtime).flags | NPCFlags.MakeNoise, new string[] { "student" });
            NPCMetaStorage.Instance.Add(circleMeta);
        }

        private void FloorAddend(string title, int id, SceneObject scene)
        {
            if (title == "END")
            {
                scene.MarkAsNeverUnload();
                scene.shopItems = scene.shopItems.AddToArray(new WeightedItemObject() { selection = AssetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });
                AddToNpcs(scene, 100, true);
                return;
            }

            if (title.StartsWith("F"))
            {
                scene.MarkAsNeverUnload();

                switch (id)
                {
                    case 0:
                        // A 1 in 1000 chance is kinda impossible to predict so instead it's pretty low weight, also if you have guaranteed spawns it only spawns on F2
                        if (!RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                            scene.potentialNPCs.Add(AssetMan.Get<CircleNpc>("CircleNpc").Weighted(3));
                        return;
                    case 1:
                        AddToNpcs(scene, 75);
                        break;
                    default:
                        AddToNpcs(scene, 100);
                        break;
                }

                scene.shopItems = scene.shopItems.AddToArray(new WeightedItemObject() { selection = AssetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });
            }
        }

        private void AddToNpcs(SceneObject scene, int weight, bool endless = false)
        {
            if (!RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                scene.potentialNPCs.Add(AssetMan.Get<CircleNpc>("CircleNpc").Weighted(weight));
            else if (endless || scene.levelNo == 1)
            {
                scene.forcedNpcs = scene.forcedNpcs.AddToArray(AssetMan.Get<CircleNpc>("CircleNpc"));
                scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
            }
        }

        private void FloorAddendLvl(string title, int id, LevelObject lvl)
        {
            if (title == "END")
            {
                lvl.posters = lvl.posters.AddToArray(new WeightedPosterObject() { selection = AssetMan.Get<PosterObject>("NerfGunPoster"), weight = 100 });
                lvl.potentialItems = lvl.potentialItems.AddToArray(new WeightedItemObject() { selection = AssetMan.Get<ItemObject>("NerfGunItem"), weight = 50 });
                return;
            }

            if (title.StartsWith("F") && id > 0)
            {
                lvl.posters = lvl.posters.AddToArray(new WeightedPosterObject() { selection = AssetMan.Get<PosterObject>("NerfGunPoster"), weight = 75 });
                lvl.potentialItems = lvl.potentialItems.AddToArray(new WeightedItemObject() { selection = AssetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });
            }
        }

        private void FieldTripLootChange(FieldTrips fieldTrip, FieldTripLoot table)
        {
            table.potentialItems.Add(new WeightedItemObject() { selection = AssetMan.Get<ItemObject>("NerfGunItem"), weight = 100 });
        }
    }

    public class Module_GottaBully : Module
    {
        public override string Name => "Gotta Bully";

        public override Action LoadAction => Load;
        public override Action<string, int, SceneObject> FloorAddendAction => FloorAddend;

        protected override ConfigEntry<bool> ConfigEntry => RecommendedCharactersConfig.moduleGottaBully;

        private void Load()
        {
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Room", "SwapCloset"), x => "SwapCloset/" + x.name);
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Npc", "GottaBully"), x => "GottaBullyTex/" + x.name);

            RecommendedCharsPlugin.AddAudioClipsToAssetMan(Path.Combine(AssetLoader.GetModPath(Plugin), "Audio", "GottaBully"), "GottaBullyAud/");

            GottaBully gottaBully = LoadGottaBully();
            LoadSwapCloset(gottaBully);
        }

        private GottaBully LoadGottaBully()
        {
            GottaBully gottaBully = RecommendedCharsPlugin.CloneComponent<GottaSweep, GottaBully>(GameObject.Instantiate((GottaSweep)NPCMetaStorage.Instance.Get(Character.Sweep).value, MTM101BaldiDevAPI.prefabTransform));
            gottaBully.name = "GottaBully";

            gottaBully.character = EnumExtensions.ExtendEnum<Character>("RecChars_GottaBully");
            PineDebugNpcIconPatch.icons.Add(gottaBully.character, AssetMan.Get<Texture2D>("GottaBullyTex/BorderGottaBully"));

            gottaBully.looker.npc = gottaBully;
            gottaBully.navigator.npc = gottaBully;

            gottaBully.speed *= 0.9f;
            gottaBully.minDelay = 150f;
            gottaBully.maxDelay = 250f;

            // Reference for item rejection (yeah that's pretty much it)
            gottaBully.bullyReference = (Bully)NPCMetaStorage.Instance.Get(Character.Bully).value;

            gottaBully.poster = ObjectCreators.CreateCharacterPoster(AssetMan.Get<Texture2D>("GottaBullyTex/pri_gbully"), "RecChars_Pst_GottaBully1", "RecChars_Pst_GottaBully2");
            gottaBully.poster.name = "GottaBullyPoster";

            gottaBully.spriteRenderer[0].sprite = AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("GottaBullyTex/GottaBully"), 26f);
            gottaBully.audMan.subtitleColor = new Color(198f / 255f, 136f / 255f, 91f / 255f);
            gottaBully.audIntro = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("GottaBullyAud/GB_Intro"), "RecChars_GottaBully_Intro", SoundType.Voice, gottaBully.audMan.subtitleColor);
            gottaBully.audSweep = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("GottaBullyAud/GB_GottaBully"), "RecChars_GottaBully_Sweep", SoundType.Voice, gottaBully.audMan.subtitleColor);

            AssetMan.Add("GottaBullyNpc", gottaBully);
            NPCMetadata gottaBullyMeta = new NPCMetadata(Info, new NPC[] { gottaBully }, gottaBully.name, NPCMetaStorage.Instance.Get(Character.Sweep).flags | NPCFlags.MakeNoise, new string[0]);
            NPCMetaStorage.Instance.Add(gottaBullyMeta);
            return gottaBully;
        }

        private void LoadSwapCloset(GottaBully gottaBully)
        {
            RoomAsset bullyRoomAsset = RoomAsset.CreateInstance<RoomAsset>();
            ((ScriptableObject)bullyRoomAsset).name = "Room_SwappedCloset_0";
            bullyRoomAsset.name = "SwappedCloset_0";
            bullyRoomAsset.category = EnumExtensions.ExtendEnum<RoomCategory>("RecChars_SwappedCloset");
            bullyRoomAsset.hasActivity = false;
            bullyRoomAsset.posterChance = 0.25f;

            bullyRoomAsset.ceilTex = Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "Carpet" && x.GetInstanceID() >= 0);
            bullyRoomAsset.florTex = AssetMan.Get<Texture2D>("SwapCloset/SwappedFloor");
            bullyRoomAsset.wallTex = AssetMan.Get<Texture2D>("SwapCloset/SwappedWall");
            bullyRoomAsset.doorMats = ObjectCreators.CreateDoorDataObject("SwapDoor", AssetMan.Get<Texture2D>("SwapCloset/SwappedDoor_Open"), AssetMan.Get<Texture2D>("SwapCloset/SwappedDoor_Closed"));
            bullyRoomAsset.potentialDoorPositions = new List<IntVector2>() { new IntVector2(0, 0) };

            bullyRoomAsset.cells = new List<CellData>()
            {
                new CellData() { pos = new IntVector2(0,0), type=12},
                new CellData() { pos = new IntVector2(1,0), type=6},
                new CellData() { pos = new IntVector2(0,1), type=9},
                new CellData() { pos = new IntVector2(1,1), type=3},
            };

            bullyRoomAsset.standardLightCells.Add(new IntVector2(0, 0));
            bullyRoomAsset.entitySafeCells.Add(new IntVector2(0, 1));
            bullyRoomAsset.eventSafeCells.Add(new IntVector2(0, 0));
            bullyRoomAsset.lightPre = gottaBully.potentialRoomAssets[0].selection.lightPre;
            bullyRoomAsset.color = gottaBully.audMan.subtitleColor;

            PosterObject tapliasmyChalkboard = ObjectCreators.CreatePosterObject(AssetMan.Get<Texture2D>("SwapCloset/SubToTapliasmy"), new PosterTextData[0]);
            tapliasmyChalkboard.name = "NotAChk_SubToTapliasmy";
            List<WeightedPosterObject> bullyRoomPoster = new List<WeightedPosterObject>
            {
                new WeightedPosterObject() { weight = 100, selection = tapliasmyChalkboard}
            };
            bullyRoomAsset.posters = bullyRoomPoster;

            gottaBully.potentialRoomAssets = new WeightedRoomAsset[]
            {
                new WeightedRoomAsset() { weight = 100, selection = bullyRoomAsset },
                new WeightedRoomAsset() { weight = 100 }
            };

            bullyRoomAsset = GameObject.Instantiate(bullyRoomAsset);
            ((ScriptableObject)bullyRoomAsset).name = "Room_SwappedCloset_1";
            bullyRoomAsset.name = "SwappedCloset_1";

            bullyRoomAsset.cells = new List<CellData>()
            {
                new CellData() { pos = new IntVector2(0,0), type=14},
                new CellData() { pos = new IntVector2(0,1), type=10},
                new CellData() { pos = new IntVector2(0,2), type=10},
                new CellData() { pos = new IntVector2(0,3), type=11},
            };

            bullyRoomAsset.entitySafeCells[0] = new IntVector2(0, 2);
            gottaBully.potentialRoomAssets[1].selection = bullyRoomAsset;
        }

        private void FloorAddend(string title, int id, SceneObject scene)
        {
            if (title == "END")
            {
                scene.MarkAsNeverUnload();
                if (!RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                    scene.potentialNPCs.CopyCharacterWeight(Character.LookAt, AssetMan.Get<GottaBully>("GottaBullyNpc"));
                else
                {
                    scene.forcedNpcs = scene.forcedNpcs.AddToArray(AssetMan.Get<GottaBully>("GottaBullyNpc"));
                    scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
                }
                return;
            }

            if (title.StartsWith("F") && id > 1)
            {
                scene.MarkAsNeverUnload();

                if (!RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                    scene.potentialNPCs.CopyCharacterWeight(Character.LookAt, AssetMan.Get<GottaBully>("GottaBullyNpc"));
                else if (id == 2)
                {
                    scene.forcedNpcs = scene.forcedNpcs.AddToArray(AssetMan.Get<GottaBully>("GottaBullyNpc"));
                    scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
                }
            }
        }
    }

    public class Module_ArtsWithWires : Module
    {
        public override string Name => "Arts with Wires";
        public override string SaveTag => Name + (RecommendedCharactersConfig.intendedWiresBehavior.Value ? " (v1.1.1+)" : "");

        public override Action LoadAction => Load;
        public override Action<string, int, SceneObject> FloorAddendAction => FloorAddend;

        protected override ConfigEntry<bool> ConfigEntry => RecommendedCharactersConfig.moduleArtsWWires;

        private void Load()
        {
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Npc", "ArtsWWires"), x => "WiresTex/" + x.name);
            RecommendedCharsPlugin.AddAudioClipsToAssetMan(Path.Combine(AssetLoader.GetModPath(Plugin), "Audio", "ArtsWWires"), "WiresAud/");

            string suffix = RecommendedCharactersConfig.ogWiresSprites.Value ? "_Old" : "";

            ArtsWithWires artsWithWires = new NPCBuilder<ArtsWithWires>(Info)
                .SetName("ArtsWithWires")   
                .SetEnum("RecChars_ArtsWithWires")
                .SetPoster(AssetMan.Get<Texture2D>("WiresTex/pri_wires"+suffix), "RecChars_Pst_Wires1", "RecChars_Pst_Wires2")
                .AddMetaFlag(NPCFlags.Standard)
                .AddLooker()
                .AddTrigger()
                .Build();

            PineDebugNpcIconPatch.icons.Add(artsWithWires.character, AssetMan.Get<Texture2D>("WiresTex/BorderWires"+suffix));

            Sprite[] sprites = AssetLoader.SpritesFromSpritesheet(2, 1, 50f, new Vector2(0.5f, 0.5f), AssetMan.Get<Texture2D>("WiresTex/WiresSprites"+suffix));

            artsWithWires.sprite = artsWithWires.spriteRenderer[0];
            artsWithWires.sprite.transform.localPosition = Vector3.zero;

            artsWithWires.sprite.sprite = sprites[0];
            artsWithWires.sprNormal = sprites[0];
            artsWithWires.sprAngry = sprites[1];

            artsWithWires.audMan = artsWithWires.GetComponent<AudioManager>();
            artsWithWires.audMan.subtitleColor = new Color(138f / 255f, 22f / 255f, 15f / 255f);

            artsWithWires.audIntro = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("WiresAud/AWW_Intro"), "RecChars_Wires_Intro", SoundType.Effect, artsWithWires.audMan.subtitleColor);
            artsWithWires.audLoop = ObjectCreators.CreateSoundObject(AssetMan.Get<AudioClip>("WiresAud/AWW_Loop"), "RecChars_Wires_Intro", SoundType.Effect, artsWithWires.audMan.subtitleColor);

            artsWithWires.stareStacks = RecommendedCharactersConfig.intendedWiresBehavior.Value;

            Jumprope jumpropeCopy = GameObject.Instantiate(((Playtime)NPCMetaStorage.Instance.Get(Character.Playtime).value).jumpropePre, MTM101BaldiDevAPI.prefabTransform);
            jumpropeCopy.name = "ArtsWithWires GrabbingGame";
            jumpropeCopy.enabled = false;

            artsWithWires.gamePrefab = jumpropeCopy.gameObject.AddComponent<GrabbingGame>();
            artsWithWires.gamePrefab.textCanvas = jumpropeCopy.textCanvas;
            artsWithWires.gamePrefab.textScaler = jumpropeCopy.textScaler;
            artsWithWires.gamePrefab.instructionsTmp = jumpropeCopy.instructionsTmp;
            artsWithWires.gamePrefab.instructionsTmp.rectTransform.anchoredPosition = Vector2.up * 32f;

            GameObject.DestroyImmediate(jumpropeCopy.countTmp.gameObject);
            GameObject.DestroyImmediate(jumpropeCopy.ropeCanvas.gameObject);
            GameObject.DestroyImmediate(jumpropeCopy);

            Sprite background = Sprite.Create(AssetMan.Get<Texture2D>("WiresTex/WiresGrabMeter"), new Rect(0, 0, 136, 24), Vector2.one / 2f);
            background.name = "WiresGrabMeter_Background";

            artsWithWires.gamePrefab.needle = UIHelpers.CreateImage(background, artsWithWires.gamePrefab.textCanvas.transform, default, false).rectTransform;
            artsWithWires.gamePrefab.needle.anchorMin = Vector2.one / 2f;
            artsWithWires.gamePrefab.needle.anchorMax = artsWithWires.gamePrefab.needle.anchorMin;
            artsWithWires.gamePrefab.needle.pivot = artsWithWires.gamePrefab.needle.anchorMin;
            artsWithWires.gamePrefab.needle.anchoredPosition = Vector2.up * -32f;
            artsWithWires.gamePrefab.needle.name = "Meter_BG";

            background = Sprite.Create(AssetMan.Get<Texture2D>("WiresTex/WiresGrabMeter"), new Rect(136, 0, 8, 24), Vector2.one / 2f);
            background.name = "WiresGrabMeter_Needle";

            artsWithWires.gamePrefab.needle = GameObject.Instantiate(artsWithWires.gamePrefab.needle, artsWithWires.gamePrefab.needle.parent, false);
            artsWithWires.gamePrefab.needle.GetComponent<Image>().sprite = background;
            artsWithWires.gamePrefab.needle.sizeDelta = new Vector2(8f, 24f);
            artsWithWires.gamePrefab.needle.name = "Meter_Needle";

            AssetMan.Add("ArtsWithWiresNpc", artsWithWires);
        }

        private void FloorAddend(string title, int id, SceneObject scene)
        {
            if (title == "END")
            {
                scene.MarkAsNeverUnload();

                if (RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                {
                    scene.forcedNpcs = scene.forcedNpcs.AddToArray(AssetMan.Get<ArtsWithWires>("ArtsWithWiresNpc"));
                    scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
                }
                else
                    scene.potentialNPCs.CopyCharacterWeight(Character.DrReflex, AssetMan.Get<ArtsWithWires>("ArtsWithWiresNpc"));
                return;
            }

            if (title.StartsWith("F") && id > 0)
            {
                scene.MarkAsNeverUnload();

                if (!RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                {
                    scene.potentialNPCs.CopyCharacterWeight(Character.DrReflex, AssetMan.Get<ArtsWithWires>("ArtsWithWiresNpc"));
                }
                else if (id == 1)
                {
                    scene.forcedNpcs = scene.forcedNpcs.AddToArray(AssetMan.Get<ArtsWithWires>("ArtsWithWiresNpc"));
                    scene.additionalNPCs = Mathf.Max(scene.additionalNPCs - 1, 0);
                }
            }
        }
    }

#if DEBUG
    public class Module_CaAprilFools : Module
    {
        public override string Name => "Man Meme Coin";
        public override string SaveTag => Name + (RecommendedCharactersConfig.npcCherryBsoda.Value ? "(Cherry BSODA pushes NPCs)" : "");

        public override Action LoadAction => Load;
        public override Action<string, int, SceneObject> FloorAddendAction => FloorAddend;

        protected override ConfigEntry<bool> ConfigEntry => RecommendedCharactersConfig.moduleCaAprilFools;

        private void Load()
        {
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Item", "CAItems"), x => "CAItems/" + x.name);
            AssetMan.AddRange(AssetLoader.TexturesFromMod(Plugin, "*.png", "Textures", "Npc", "MMCoin"), x => "MMCoinTex/" + x.name);

            AudioClip boing = AssetLoader.AudioClipFromMod(Plugin, "Audio", "Boing.wav");
            AssetMan.Add("Boing", ObjectCreators.CreateSoundObject(boing, "RecChars_Sfx_Boing", SoundType.Effect, Color.white));
            LoadItems();
        }

        private void LoadItems()
        {
            // Flamin' Hot Cheetos
            ItemObject cheetos = new ItemBuilder(Info)
            .SetNameAndDescription("RecChars_Itm_FlaminHotCheetos", "RecChars_Desc_FlaminHotCheetos")
            .SetEnum("RecChars_FlaminHotCheetos")
            .SetMeta(ItemFlags.Persists, new string[] { "food" })
            .SetSprites(AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/FlaminHotCheetos_Small"), 25f), AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/FlaminHotCheetos_Large"), 50f))
            .SetShopPrice(750)
            .SetGeneratorCost(80)
            .SetItemComponent<ITM_FlaminHotCheetos>()
            .Build();

            cheetos.name = "RecChars FlaminHotCheetos";

            ITM_FlaminHotCheetos cheetosItm = (ITM_FlaminHotCheetos)cheetos.item;
            cheetosItm.name = "Itm_FlaminHotCheetos";
            cheetosItm.gaugeSprite = cheetos.itemSpriteSmall;
            cheetosItm.audEat = ((ITM_ZestyBar)ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value.item).audEat;

            // Cherry BSODA
            ItemObject cherryBsoda = new ItemBuilder(Info)
            .SetNameAndDescription("RecChars_Itm_CherryBsoda", RecommendedCharactersConfig.npcCherryBsoda.Value ? "RecChars_Desc_CherryBsoda_NoPlayerPush" : "RecChars_Desc_CherryBsoda")
            .SetEnum("RecChars_CherryBsoda")
            .SetMeta(ItemFlags.Persists | ItemFlags.CreatesEntity, new string[] { "drink" })
            .SetSprites(AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/CherryBsoda_Small"), 25f), AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/CherryBsoda_Large"), 50f))
            .SetShopPrice(750)
            .SetGeneratorCost(80)
            .Build();

            cherryBsoda.name = "RecChars CherryBsoda";

            ITM_BSODA bsodaClone = GameObject.Instantiate((ITM_BSODA)ItemMetaStorage.Instance.FindByEnum(Items.Bsoda).value.item, MTM101BaldiDevAPI.prefabTransform);
            bsodaClone.enabled = false;
            bsodaClone.spriteRenderer.sprite = AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/CherryBsoda_Spray"), 12f);
            bsodaClone.speed = 40f;
            bsodaClone.time = 10f;

            ITM_CherryBsoda cherryBsodaUse;
            if (!RecommendedCharactersConfig.npcCherryBsoda.Value)
                cherryBsodaUse = bsodaClone.gameObject.AddComponent<ITM_CherryBsoda>();
            else
                cherryBsodaUse = bsodaClone.gameObject.AddComponent<ITM_CherryBsoda_PushesNpcs>();

            cherryBsoda.item = cherryBsodaUse;
            cherryBsoda.item.name = "Itm_CherryBsoda";

            cherryBsodaUse.bsoda = bsodaClone;
            cherryBsodaUse.boing = AssetMan.Get<SoundObject>("Boing");

            ITM_GrapplingHook hook = (ITM_GrapplingHook)ItemMetaStorage.Instance.FindByEnum(Items.GrapplingHook).value.item;
            bsodaClone.entity.collisionLayerMask = hook.entity.collisionLayerMask;
            cherryBsodaUse.layerMask = hook.layerMask;


            // Ultimate Apple
            ItemObject ultiApple = new ItemBuilder(Info)
            .SetNameAndDescription("RecChars_Itm_UltimateApple", "RecChars_Desc_UltimateApple")
            .SetEnum("RecChars_UltimateApple")
            .SetMeta(ItemFlags.NoUses, new string[] { "food" })
            .SetSprites(AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/UltimateApple_Small"), 25f), AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/UltimateApple_Large"), 50f))
            .SetShopPrice(2500)
            .SetGeneratorCost(100)
            .Build();

            ultiApple.name = "RecChars UltimateApple";
            ultiApple.item = ItemMetaStorage.Instance.FindByEnum(Items.Apple).value.item;

            Baldi_UltimateApple.ultiAppleEnum = ultiApple.itemType;
            Baldi_UltimateApple.ultiAppleSprites = AssetLoader.SpritesFromSpritesheet(2, 1, 32f, new Vector2(0.5f, 0.5f), AssetMan.Get<Texture2D>("CAItems/BaldiUltimateApple"));

            // Can of Mangles
            ItemMetaData manglesMeta = new ItemMetaData(Info, new ItemObject[0]);
            manglesMeta.flags = ItemFlags.MultipleUse;
            manglesMeta.tags.Add("food");

            ItemBuilder manglesBuilder = new ItemBuilder(Info)
            .SetNameAndDescription("RecChars_Itm_Mangles1", "RecChars_Desc_Mangles")
            .SetEnum("RecChars_Mangles")
            .SetMeta(manglesMeta)
            .SetSprites(AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/Mangles_Small"), 25f), AssetLoader.SpriteFromTexture2D(AssetMan.Get<Texture2D>("CAItems/Mangles_Large"), 50f))
            .SetShopPrice(500)
            .SetGeneratorCost(60)
            .SetItemComponent<ITM_Mangles>();

            ItemObject manglesItemObject = manglesBuilder.Build();
            manglesItemObject.name = "RecChars Mangles1";
            ITM_Mangles manglesItm = (ITM_Mangles)manglesItemObject.item;
            manglesItm.name = "Itm_Mangles1";
            manglesItm.audEat = cheetosItm.audEat;

            manglesBuilder.SetNameAndDescription("RecChars_Itm_Mangles2", "RecChars_Desc_Mangles");
            ItemObject manglesItemObject2 = manglesBuilder.Build();
            manglesItemObject2.name = "RecChars Mangles2";
            manglesItm = (ITM_Mangles)manglesItemObject2.item;
            manglesItm.name = "Itm_Mangles2";
            manglesItm.audEat = cheetosItm.audEat;
            manglesItm.nextStage = manglesItemObject;

            manglesBuilder.SetNameAndDescription("RecChars_Itm_Mangles3", "RecChars_Desc_Mangles");
            manglesItemObject = manglesItemObject2;
            manglesItemObject2 = manglesBuilder.Build();
            manglesItemObject2.name = "RecChars Mangles3";
            manglesItm = (ITM_Mangles)manglesItemObject2.item;
            manglesItm.name = "Itm_Mangles3";
            manglesItm.audEat = cheetosItm.audEat;
            manglesItm.nextStage = manglesItemObject;
        }

        private void FloorAddend(string title, int id, SceneObject scene)
        {
        }
    }
#endif
}
