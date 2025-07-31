using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

using UnityEngine;

using HarmonyLib;
using MTM101BaldAPI.Registers;

namespace UncertainLuei.BaldiPlus.ItemFees.Patches
{
    [HarmonyPatch(typeof(ItemManager), "UseItem")]
    class UseItemPatches
    {
        public static readonly MethodInfo ytpPenaltyMethod = AccessTools.Method(typeof(UseItemPatches), "UsageYtpPenalty");
        public static void UsageYtpPenalty(ItemManager itemMan, ItemObject itm)
        {
            // Do not run if it's the tutorial level
            if (ItemFeesExtensions.IsTutorial) return;

            ItemMetaData meta = itm.GetMeta();

            // If an item with NoUses is used in special interactions (i.e. giving the Bus Pass to Johnny, YTPs are not revoked)
            if (meta == null || meta.flags != ItemFlags.NoUses)
                CoreGameManager.Instance.AddPoints(-itm.GetUsageCost(), itemMan.pm.playerNumber, true, false);
        }

        private static bool Prefix(ItemManager __instance)
        {
            if (__instance.disabled && !__instance.items[__instance.selectedItem].overrideDisabled) return true;

            ItemMetaData meta = __instance.items[__instance.selectedItem].GetMeta();
            if (meta != null && meta.flags != ItemFlags.NoUses &&
                !__instance.CanAffordSlot())
            {
                CoreGameManager.Instance.audMan.PlaySingle(ItemFeesPlugin.errorSound);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    class ItemManagerDisplayPatches
    {
        private static readonly MethodInfo setItemAvailabilityMethod = AccessTools.Method(typeof(ItemManagerDisplayPatches), "SetItemAvailability");
        private static void SetItemAvailability(HudManager hud, ItemManager itmMan, int slot)
        {
            if (hud && hud.itemSprites[slot])
                hud.itemSprites[slot].color = itmMan.CanAffordSlot(slot) ? Color.white : Color.gray;
        }
        public static void SetAllItemAvailability(HudManager hud, ItemManager itemMan)
        {
            for (int i = 0; i <= itemMan.maxItem; i++)
                SetItemAvailability(hud, itemMan, i);
        }

        [HarmonyPatch("UpdateItems"), HarmonyPostfix]
        private static void UpdateItemsPostfix(ItemManager __instance)
        {
            HudManager hud = CoreGameManager.Instance.GetHud(__instance.pm.playerNumber);
            SetAllItemAvailability(hud, __instance);
        }

        [HarmonyPatch("SetItem"), HarmonyPostfix]
        private static void SetItemPostfix(ItemManager __instance, int slot)
        {
            SetItemAvailability(CoreGameManager.Instance.GetHud(__instance.pm.playerNumber), __instance, slot);
        }

        [HarmonyPatch("AddItem", typeof(ItemObject)), HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> AddItemTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            CodeInstruction[] array = instructions.ToArray();
            int length = array.Length;

            for (int i = 0; i < length; i++)
            {
                yield return array[i];
                if (!patched &&
                    array[i].opcode == OpCodes.Callvirt &&
                    array[i+1].opcode == OpCodes.Ldarg_0 &&
                    array[i+2].opcode == OpCodes.Call &&
                    array[i+3].opcode == OpCodes.Ret)
                {
                    patched = true;

                    // SetItemAvailability(CoreGameManager.Instance.GetHud(this.pm.playerNumber), this, num);
                    yield return array[i-8];
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return array[i-6];
                    yield return array[i-5];
                    yield return array[i-4];
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Call, setItemAvailabilityMethod);
                }
            }

            if (!patched)
                Debug.LogError("Transpiler \"ItemFees.ItemManagerDisplayPatches.AddItemTranspiler\" did not go through!");

            yield break;
        }
    }

    [HarmonyPatch(typeof(CoreGameManager), "AddPoints", typeof(int), typeof(int), typeof(bool), typeof(bool))]
    class UpdatePointsPatch
    {
        private static void Postfix(CoreGameManager __instance, int player)
        {
            if (__instance.huds == null || __instance.huds.Length <= player || __instance.GetHud(player) == null) return;
            if (__instance.players == null || __instance.players.Length <= player || __instance.GetPlayer(player) == null) return;

            // Update inventory to reflect current items
            ItemManagerDisplayPatches.SetAllItemAvailability(__instance.GetHud(player), __instance.GetPlayer(player).itm);
        }
    }

    [HarmonyPatch(typeof(Baldi_Chase), "OnStateTriggerStay")]
    class BaldiTakeApplePatch
    {
        private static void YtpPenalty(int player)
        {
            // The apple will take half of your YTPs (or at least its default amount) if Baldi takes it away
            int cost = -Mathf.Max(Items.Apple.GetUsageCost(), CoreGameManager.Instance.GetPoints(player)/2);
            CoreGameManager.Instance.AddPoints(cost, player, true);
        }
        
        private static readonly MethodInfo ytpPenaltyMethod = AccessTools.Method(typeof(BaldiTakeApplePatch), "YtpPenalty");
        private static readonly MethodInfo testAppleMethod = AccessTools.Method(typeof(ItemFeesExtensions), "CanAffordItemType");
        private static readonly FieldInfo playerNumberField = AccessTools.Field(typeof(PlayerManager), "playerNumber");

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            CodeInstruction[] array = instructions.ToArray();
            int length = array.Length;

            for (int i = 0; i < length; i++)
            {
                yield return array[i];
                if (!patched &&
                    array[i].opcode == OpCodes.Brfalse &&
                    array[i-1].opcode == OpCodes.Callvirt &&
                    array[i-2].opcode == OpCodes.Ldc_I4_S &&
                    array[i-3].opcode == OpCodes.Ldloc_2)
                {
                    patched = true;
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return array[i-2];
                    yield return new CodeInstruction(OpCodes.Call, testAppleMethod);
                    yield return array[i];
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldfld, playerNumberField);
                    yield return new CodeInstruction(OpCodes.Call, ytpPenaltyMethod);
                }
            }

            if (!patched)
                Debug.LogError("Transpiler \"ItemFees.BaldiTakeApplePatch.Transpiler\" did not go through!");

            yield break;
        }
    }

    [HarmonyPatch(typeof(FieldTripEntranceRoomFunction), "StartFieldTrip")]
    class BusPassCheckPatch
    {
        private static bool Prefix(PlayerManager player, bool ___unlocked, AudioManager ___baldiAudioManager, SoundObject ___audNoPass)
        {
            if (___unlocked) return true;

            if (!player.CanAffordItemType(Items.BusPass))
            {
                ___baldiAudioManager.FlushQueue(true);
                ___baldiAudioManager.QueueAudio(___audNoPass);
                return false;
            }
            else if (player.itm.Has(Items.BusPass))
            {
                CoreGameManager.Instance.AddPoints(-Mathf.Max(Items.BusPass.GetUsageCost()), player.playerNumber, true, false);
            }
            return true;
        }
    }
}
