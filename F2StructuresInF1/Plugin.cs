using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;

namespace UncertainLuei.BaldiPlus.F2StructsInF1
{
    [BepInAutoPlugin(ModGuid, ModName)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "8.2.0.0")]

    partial class FTwoStructsPlugin : BaseUnityPlugin
    {
        internal const string ModGuid = "io.github.uncertainluei.baldiplus.f2structsinf1";
        internal const string ModName = "F2 Structures in F1";

        internal static ManualLogSource Log { get; private set; }

        internal static ConfigEntry<bool> configClassrooms;
        internal static ConfigEntry<bool> configStudents;

        private void Awake()
        {
            BindConfig();

            FTwoStructsSaveGameIO saveGameSystem = new(Info);
            ModdedSaveGame.AddSaveHandler(saveGameSystem);

            LoadingEvents.RegisterOnAssetsLoaded(Info, GrabBaseAssets(), LoadingEventOrder.Pre);

            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorAddend);
            GeneratorManagement.Register(this, GenerationModType.Finalizer, GeneratorFinalizer);

            Harmony harmony = new(ModGuid);
            harmony.PatchAllConditionals();
        }

        private void BindConfig()
        {
            configClassrooms = Config.Bind<bool>("Structures", "Classrooms", true, "Adds the F2 classrooms");
            configStudents = Config.Bind<bool>("Structures", "StudentSpawner", true, "Adds the Student Spawner");
        }

        private StructureWithParameters studentStruct;

        private IEnumerator GrabBaseAssets()
        {
            yield return 1;
            yield return "Grabbing F2 student structure";
            SceneObject f2 = SceneObjectMetaStorage.Instance.Find(x => x.tags.Contains("main") && x.number == 1).value;
            studentStruct = f2.levelObject.forcedStructures.First(x => x.prefab is Structure_StudentSpawner);
        }

        private void GeneratorAddend(string title, int id, SceneObject scene)
        {
            if (!title.StartsWith("F") || id > 0) return;

            CustomLevelObject[] lvls = scene.GetCustomLevelObjects();
            foreach (CustomLevelObject lvl in lvls)
            {
                if (configStudents.Value)
                    lvl.forcedStructures = lvl.forcedStructures.AddToArray(studentStruct);
            }
        }

        private bool _hasRooms;
        private WeightedRoomAsset[] _rooms;
        private void GeneratorFinalizer(string title, int id, SceneObject scene)
        {
            if (!_hasRooms)
            {
                _hasRooms = true;
                SceneObject f2 = SceneObjectMetaStorage.Instance.Find(x => x.tags.Contains("main") && x.number == 1).value;
                List<WeightedRoomAsset> rooms2 = [];
                rooms2.AddRange(f2.GetCustomLevelObjects()[0].roomGroup.First(x => x.potentialRooms[0].selection.category == RoomCategory.Class).potentialRooms);
                _rooms = rooms2.ToArray();
            }
            if (!title.StartsWith("F") || id > 0) return;

            CustomLevelObject[] lvls = scene.GetCustomLevelObjects();
            RoomGroup roomGroup;
            foreach (CustomLevelObject lvl in lvls)
            {
                if (!configClassrooms.Value) continue;
                roomGroup = lvl.roomGroup.First(x => x.potentialRooms[0].selection.category == RoomCategory.Class);
                roomGroup.potentialRooms = roomGroup.potentialRooms.AddRangeToArray(_rooms);
            }
        }
    }

    public class FTwoStructsSaveGameIO(PluginInfo info) : ModdedSaveGameIOBinary
    {
        private readonly PluginInfo info = info;
        public override PluginInfo pluginInfo => info;

        public override void OnCGMCreated(CoreGameManager cgm, bool savedGame)
        {
        }

        public override void Load(BinaryReader reader)
        {
            reader.ReadByte();
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write(0);
        }

        public override void Reset()
        {
        }

        public override string[] GenerateTags()
        {
            List<string> tags = [];
            return [.. tags];
        }

        public override string DisplayTags(string[] tags)
        {
            string display = "";
            return display;
        }
    }
}