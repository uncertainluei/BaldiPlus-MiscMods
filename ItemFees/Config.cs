using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.SaveSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    class ItemFeesCosts
    {
        [JsonIgnore]
        private static ItemFeesCosts instance;

        [JsonIgnore]
        private static Dictionary<Items, int> defaultCosts = new Dictionary<Items, int>()
        {
            {Items.Quarter, 20},
            {Items.Scissors, 30},
            {Items.DetentionKey, 30},
            {Items.ZestyBar, 45},
            {Items.DietBsoda, 45},
            {Items.Wd40, 45},
            {Items.AlarmClock, 45},
            {Items.Boots, 50},
            {Items.NanaPeel, 50},
            {Items.Tape, 60},
            {Items.ChalkEraser, 60},
            {Items.PrincipalWhistle, 60},
            {Items.GrapplingHook, 60},
            {Items.DoorLock, 75},
            {Items.Bsoda, 75},
            {Items.Nametag, 75},
            {Items.PortalPoster, 100},
            {Items.Teleporter, 150},
            {Items.BusPass, 150},
            {Items.Apple, 250}
        };

        [JsonIgnore]
        private Dictionary<Items, int> costsEnum;

        public Dictionary<string, int> costs;

        public static void Reload()
        {
            if (instance == null)
                instance = new ItemFeesCosts();

            instance.costsEnum = defaultCosts;
            if (File.Exists(ItemFeesPlugin.costsConfigPath))
            {   
                try
                {
                    instance.costs = null;
                    JsonConvert.PopulateObject(File.ReadAllText(ItemFeesPlugin.costsConfigPath), instance);
                    if (instance.costs != null)
                    {
                        instance.costsEnum.Clear();
                        instance.costs.Do(x =>
                        {
                            instance.costsEnum.Add(EnumExtensions.GetFromExtendedName<Items>(x.Key), x.Value);
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"ItemFees Costs Config failed to serialize! Exception:\n{e}");
                }
            }
            else
            {
                instance.costs = new Dictionary<string, int>();
                instance.costsEnum.Do(x => instance.costs.Add(x.Key.ToStringExtended(), x.Value));
                File.WriteAllText(ItemFeesPlugin.costsConfigPath, JsonConvert.SerializeObject(instance, Formatting.Indented));
            }
        }

        public static Dictionary<Items, int> ItemCosts => instance.costsEnum;
    }
}
