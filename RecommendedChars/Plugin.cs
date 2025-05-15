using BepInEx;
using BepInEx.Logging;

using HarmonyLib;

using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    class RecommendedCharsPlugin : BaseUnityPlugin
    {
        public const string ModName = "Luei's Recommended Character Pack";
        public const string ModGuid = "io.github.uncertainluei.baldiplus.recommendedchars";
        public const string ModVersion = "1.1.1";

        public static readonly AssetManager AssetMan = new AssetManager();
        internal static RecommendedCharsPlugin Plugin { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        private void Awake()
        {
            Plugin = this;
            Log = Logger;

            // Read the config values and remove disabled modules
            RecommendedCharactersConfig.BindConfig(Config);
            Modules = Modules.Where(x => x.Enabled).ToArray();

            RecommendedCharsSaveGameIO saveGameSystem = new RecommendedCharsSaveGameIO(Info);
            ModdedSaveGame.AddSaveHandler(saveGameSystem);

            // Load localization file
            AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "Lang_En.json"), Language.English);

            LoadingEvents.RegisterOnAssetsLoaded(Info, LoadModules(), false);
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorAddend);
            GeneratorManagement.RegisterFieldTripLootChange(this, FieldTripLootChange);

            Harmony harmony = new Harmony(ModGuid);
            harmony.PatchAllConditionals();
        }

        public Module[] Modules { get; private set; } = new Module[]
        {
            new Module_Circle(),
            new Module_GottaBully(),
            new Module_ArtsWithWires(),
#if DEBUG
            new Module_CaAprilFools()
#endif
        };

        internal static X CloneComponent<T, X>(T original) where T : MonoBehaviour where X : T
        {
            X val = original.gameObject.AddComponent<X>();
            
            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToArray();
            foreach (FieldInfo fieldInfo in fields)
                fieldInfo.SetValue(val, fieldInfo.GetValue(original));

            GameObject.DestroyImmediate(original);
            return val;
        }

        internal static void AddAudioClipsToAssetMan(string path, string prefix)
        {
            string[] files = Directory.GetFiles(path, "*.wav");
            for (int i = 0; i < files.Length; i++)
            {
                AudioClip aud = AssetLoader.AudioClipFromFile(files[i], AudioType.WAV);
                AssetMan.Add(prefix + aud.name, aud);
                aud.name = "RecChars_" + aud.name;
            }
        }

        private IEnumerator LoadModules()
        {
            yield return Modules.Length;

            foreach (Module module in Modules)
            { 
                yield return $"Loading module \"{module.Name}\"";
                module.LoadAction?.Invoke();
            }
        }

        private void GeneratorAddend(string title, int id, SceneObject scene)
        {
            CustomLevelObject[] lvls = scene.GetCustomLevelObjects();

            foreach (Module module in Modules)
            {
                module.FloorAddendAction?.Invoke(title, id, scene);
                foreach (CustomLevelObject lvl in lvls)
                    module.LevelAddendAction?.Invoke(title, id, lvl);
            }
        }

        private void FieldTripLootChange(FieldTrips fieldTrip, FieldTripLoot table)
        {
            foreach (Module module in Modules)
                module.FieldTripLootAction?.Invoke(fieldTrip, table);
        }
    }

    public class RecommendedCharsSaveGameIO : ModdedSaveGameIOBinary
    {
        public RecommendedCharsSaveGameIO(PluginInfo info)
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

        public override string[] GenerateTags()
        {
            List<string> tags = new List<string>();
            
            foreach (Module module in RecommendedCharsPlugin.Plugin.Modules)
                tags.Add(module.Name);
            if (tags.Count > 0)
            {
                if (RecommendedCharactersConfig.guaranteeSpawnChar.Value)
                    tags.Add("GuaranteedSpawn");
                if ( RecommendedCharactersConfig.intendedWiresBehavior.Value)
                    tags.Add("GuaranteedSpawn");
            }

            return tags.ToArray();
        }

        public override string DisplayTags(string[] tags)
        {
            string display = "<b>Modules:</b> ";
            bool addComma = false;

            foreach (string tag in tags)
            {
                if (tag == "GuaranteedSpawn")
                {
                    display = "<b>Guaranteed Character Spawns</b>\n" + display;
                    break;
                }

                if (addComma)
                    display += ", ";
                addComma = true;

                display += tag;
            }
            return display;
        }
    }
}