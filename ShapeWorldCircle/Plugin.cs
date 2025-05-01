using BepInEx;

using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.UI;

namespace UncertainLuei.BaldiPlus.ShapeWorldCircle
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    class ShapeWorldCirclePlugin : BaseUnityPlugin
    {
        public const string ModName = "TCMG's Circle";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.shapeworld";
        public const string ModVersion = "1.0";

        private void Awake()
        {
            // Store save game system to initialize for later
            ShapeWorldSaveGameIO saveGameSystem = new ShapeWorldSaveGameIO(Info);
            ModdedSaveGame.AddSaveHandler(saveGameSystem);

            // Load localization file
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Lang_En.json"), Language.English);

            assetMan = new AssetManager();

            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadAssets(), false);
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorAddend);
            GeneratorManagement.RegisterFieldTripLootChange(this, FieldTripLootChange);

            Harmony harmony = new Harmony(ModGuid);
            harmony.PatchAllConditionals();
        }

        private AssetManager assetMan;
        internal static Character circleCharEnum;

        private IEnumerator LoadAssets()
        {
            yield return 3;
            yield return "Loading textures and audio";

            assetMan.AddRange(AssetLoader.TexturesFromMod(this, "*.png", "Textures"));

            string[] files = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(this), "Audio"), "*.wav");
            for (int i = 0; i < files.Length; i++)
            {
                AudioClip aud = AssetLoader.AudioClipFromFile(files[i], AudioType.WAV);
                assetMan.Add(aud.name, aud);
                aud.name = "ShapeWorld_" + aud.name;
            }

            yield return "Creating Nerf Gun item";

            ItemMetaData nerfGunMeta = new ItemMetaData(Info, new ItemObject[0])
            {
                flags = ItemFlags.MultipleUse
            };

            Items nerfGunEnum = EnumExtensions.ExtendEnum<Items>("ShapeWorld_NerfGun");

            ItemBuilder nerfGunBuilder = new ItemBuilder(Info)
            .SetNameAndDescription("ShapeWorld_Itm_NerfGun2", "ShapeWorld_Desc_NerfGun")
            .SetEnum(nerfGunEnum)
            .SetMeta(nerfGunMeta)
            .SetSprites(AssetLoader.SpriteFromTexture2D(assetMan.Get<Texture2D>("NerfGunItem_Small"), 25f), AssetLoader.SpriteFromTexture2D(assetMan.Get<Texture2D>("NerfGunItem_Large"), 50f))
            .SetShopPrice(500)
            .SetGeneratorCost(50)
            .SetItemComponent<ITM_NerfGun>();

            ItemObject nerfItm = nerfGunBuilder.Build();
            nerfItm.name = "ShapeWorld NerfGun2";
            nerfItm.item.name = "Itm_NerfGun2";
            assetMan.Add("NerfGunItem", nerfItm);

            nerfGunBuilder.SetNameAndDescription("ShapeWorld_Itm_NerfGun1", "ShapeWorld_Desc_NerfGun");
            ItemObject nerfItm1 = nerfGunBuilder.Build();
            nerfItm1.name = "ShapeWorld NerfGun1";
            nerfItm1.item.name = "Itm_NerfGun1";
            ((ITM_NerfGun)nerfItm.item).leftover = nerfItm1;

            assetMan.Add("NerfGunPoster", ObjectCreators.CreatePosterObject(assetMan.Get<Texture2D>("NerfGunPoster"), new PosterTextData[0]));
            assetMan.Get<PosterObject>("NerfGunPoster").name = "NerfGunPoster";

            yield return "Creating Circle NPC";

            CircleNpc circle = CloneComponent<Playtime, CircleNpc>(Instantiate((Playtime)NPCMetaStorage.Instance.Get(Character.Playtime).value, MTM101BaldiDevAPI.prefabTransform));
            circle.name = "ShapeWorld Circle";

            circleCharEnum = EnumExtensions.ExtendEnum<Character>("ShapeWorld_Circle");
            circle.character = circleCharEnum;

            circle.animator.enabled = false;

            circle.audMan.subtitleColor = new Color(52f / 255f, 182f / 255f, 69f / 255f);
            circle.audCount = new SoundObject[9];
            for (int i = 0; i < 9; i++)
                circle.audCount[i] = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>($"Circle_{i + 1}"), $"Vfx_Playtime_{i + 1}", SoundType.Voice, circle.audMan.subtitleColor);

            circle.audLetsPlay = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_LetsPlay"), "Vfx_Playtime_LetsPlay", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audGo = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_ReadyGo"), "ShapeWorld_Circle_Go", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audOops = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Oops"), "ShapeWorld_Circle_Oops", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audCongrats = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Congrats"), "ShapeWorld_Circle_Congrats", SoundType.Voice, circle.audMan.subtitleColor);
            circle.audSad = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Sad"), "ShapeWorld_Circle_Sad", SoundType.Voice, circle.audMan.subtitleColor);

            circle.audCalls = new SoundObject[]
            {
                ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Random1"), "ShapeWorld_Circle_Random", SoundType.Voice, circle.audMan.subtitleColor),
                ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Random2"), "ShapeWorld_Circle_Random", SoundType.Voice, circle.audMan.subtitleColor)
            };

            PropagatedAudioManager music = circle.GetComponents<PropagatedAudioManager>()[1];
            music.soundOnStart[0] = ObjectCreators.CreateSoundObject(assetMan.Get<AudioClip>("Circle_Music"), "ShapeWorld_Circle_Music", SoundType.Effect, circle.audMan.subtitleColor);
            music.subtitleColor = circle.audMan.subtitleColor = new Color(52f / 255f, 182f / 255f, 69f / 255f);

            circle.looker.npc = circle;
            circle.navigator.npc = circle;

            // The default speed was 500 but this should flow better in-game
            circle.normSpeed = 90f;
            circle.runSpeed = 90f;

            circle.poster = ObjectCreators.CreateCharacterPoster(assetMan.Get<Texture2D>("CirclePoster"), "ShapeWorld_Pst_Circle1", "ShapeWorld_Pst_Circle2");
            circle.poster.name = "CirclePoster";

            circle.sprite = circle.spriteRenderer[0];
            Sprite[] circleSprites = AssetLoader.SpritesFromSpritesheet(2,1,100f, new Vector2(0.5f,0.5f), assetMan.Get<Texture2D>("CircleSprites"));
            circle.normal = circleSprites[0];
            circle.sprite.sprite = circle.normal;

            circle.sad = circleSprites[1];

            CircleJumprope jumprope = CloneComponent<Jumprope, CircleJumprope>(Instantiate(circle.jumpropePre, MTM101BaldiDevAPI.prefabTransform));
            circle.jumpropePre = jumprope;

            jumprope.name = "ShapeWorld Circle_Jumprope";
            jumprope.ropeAnimator = jumprope.animator.gameObject.AddComponent<CustomSpriteAnimator>();
            jumprope.ropeAnimator.spriteRenderer = jumprope.ropeAnimator.GetComponentInChildren<SpriteRenderer>();
            CircleJumprope.ropeAnimation = new Dictionary<string, Sprite[]> { { "JumpRope", AssetLoader.SpritesFromSpritesheet(4, 4, 1f, new Vector2(0.5f, 0.5f), assetMan.Get<Texture2D>("CircleRainbow")) } };
            jumprope.ropeDelay = 0f;
            jumprope.ropeTime = 1f;
            jumprope.maxJumps = 10;
            jumprope.startVal = 43;

            assetMan.Add("CircleNpc", circle);
            NPCMetadata circleMeta = new NPCMetadata(Info, new NPC[] { circle }, circle.name, NPCMetaStorage.Instance.Get(Character.Playtime).flags | NPCFlags.MakeNoise, new string[] { "student" });
            NPCMetaStorage.Instance.Add(circleMeta);
            yield break;
        }

        private void GeneratorAddend(string title, int id, SceneObject scene)
        {
            CustomLevelObject[] lvls = scene.GetCustomLevelObjects();
            
            if (title.StartsWith("F"))
            {
                if (id > 0)
                {
                    scene.shopItems = scene.shopItems.AddToArray(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });

                    foreach (CustomLevelObject lvl in lvls)
                    {
                        lvl.posters = lvl.posters.AddToArray(new WeightedPosterObject() { selection = assetMan.Get<PosterObject>("NerfGunPoster"), weight = 75 });
                        lvl.potentialItems = lvl.potentialItems.AddToArray(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });
                    }
                }

                switch (id)
                {
                    case 0:
                        // A 1 in 1000 chance is kinda impossible to predict so instead it's pretty low weight
                        scene.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<CircleNpc>("CircleNpc"), weight = 3 });
                        break;
                    case 1:
                        
                        scene.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<CircleNpc>("CircleNpc"), weight = 75 });
                        break;
                    default:
                        scene.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<CircleNpc>("CircleNpc"), weight = 100 });
                        break;
                }

                return;
            }

            if (title == "END")
            {
                scene.shopItems = scene.shopItems.AddToArray(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("NerfGunItem"), weight = 25 });
                scene.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<CircleNpc>("CircleNpc"), weight = 100 });

                foreach (CustomLevelObject lvl in lvls)
                {
                    lvl.posters = lvl.posters.AddToArray(new WeightedPosterObject() { selection = assetMan.Get<PosterObject>("NerfGunPoster"), weight = 100 });
                    lvl.potentialItems = lvl.potentialItems.AddToArray(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("NerfGunItem"), weight = 50 });
                }
            }
        }

        private void FieldTripLootChange(FieldTrips fieldTrip, FieldTripLoot table)
        {
            table.potentialItems.Add(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("NerfGunItem"), weight = 100 });
        }

        private X CloneComponent<T, X>(T original) where T : MonoBehaviour where X : T
        {
            X val = original.gameObject.AddComponent<X>();

            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToArray();
            foreach (FieldInfo fieldInfo in fields)
                fieldInfo.SetValue(val, fieldInfo.GetValue(original));

            DestroyImmediate(original);
            return val;
        }


        public class ShapeWorldSaveGameIO : ModdedSaveGameIOBinary
        {
            public ShapeWorldSaveGameIO(PluginInfo info)
            {
                this.info = info;
            }

            private readonly PluginInfo info;
            public override PluginInfo pluginInfo => info;

            public override void Load(BinaryReader reader)
            {
                reader.ReadByte();
            }

            public override void Save(BinaryWriter writer)
            {
                writer.Write((byte)0);
            }

            public override void Reset()
            {
            }
        }
    }
}