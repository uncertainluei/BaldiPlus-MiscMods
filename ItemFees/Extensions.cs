using MTM101BaldAPI.Registers;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    static class ItemFeesExtensions
    {
        public static int GetUsageCost(this ItemObject itm)
        {
            if (ItemFeesConfig.ItemCosts.TryGetValue(itm.itemType, out int price))
                return price;

            return itm.price / 10;
        }

        public static int GetUsageCost(this ItemManager itmMan)
        {
            return itmMan.GetUsageCost(itmMan.selectedItem);
        }
        public static int GetUsageCost(this ItemManager itmMan, int slot)
        {
            return itmMan.items[slot].GetUsageCost();
        }

        private static Items lastItem;
        private static int lastItemCost;

        public static int GetUsageCost(this Items itemType)
        {
            // If it's the same item type then do not bother recalculating
            if (itemType != lastItem)
            {
                ItemMetaData md = ItemMetaStorage.Instance.FindByEnum(itemType);
                lastItem = itemType;

                // Grab the total UsageCost
                lastItemCost = 0;
                foreach (ItemObject itm in md.itemObjects)
                {
                    if (lastItemCost < itm.GetUsageCost())
                        lastItemCost = itm.GetUsageCost();
                }
            }

            return lastItemCost;
        }

        public static bool CanAfford(this PlayerManager pm, Items itemType)
        {
            return CoreGameManager.Instance.GetPoints(pm.playerNumber) >= itemType.GetUsageCost();
        }

        public static string GetDescription(this ItemObject itm)
        {
            ItemMetaData meta = itm.GetMeta();
            if (meta != null && ItemFeesPlugin.descOverrides.TryGetValue(meta, out string newDesc))
                return string.Format(LocalizationManager.Instance.GetLocalizedText(newDesc), itm.GetUsageCost());

            return LocalizationManager.Instance.GetLocalizedText(itm.descKey);
        }
    }
}
