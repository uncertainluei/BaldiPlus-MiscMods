using MTM101BaldAPI.Registers;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    static class ItemFeesExtensions
    {
        public static int GetUsageCost(this ItemObject itm)
        {
            if (ItemFeesCosts.ItemCosts.TryGetValue(itm.itemType, out int price))
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

        private static Items _lastItem;
        private static int _lastCost;

        public static int GetUsageCost(this Items itemType)
        {
            // If it's the same item type then do not bother recalculating
            if (itemType != _lastItem)
            {
                ItemMetaData md = ItemMetaStorage.Instance.FindByEnum(itemType);
                _lastItem = itemType;

                // Grab the total UsageCost
                _lastCost = 0;
                // Get the highest price
                foreach (ItemObject itm in md.itemObjects)
                {
                    if (_lastCost < itm.GetUsageCost())
                        _lastCost = itm.GetUsageCost();
                }
            }

            return _lastCost;
        }

        public static bool CanAffordItemType(this PlayerManager pm, Items itemType)
        {
            return IsTutorial || CoreGameManager.Instance.GetPoints(pm.playerNumber) >= itemType.GetUsageCost();
        }

        public static bool CanAffordSlot(this ItemManager itm)
        {
            return itm.CanAffordSlot(itm.selectedItem);
        }

        public static bool CanAffordSlot(this ItemManager itm, int slot)
        {
            return IsTutorial || CoreGameManager.Instance.GetPoints(itm.pm.playerNumber) >= itm.GetUsageCost(slot);
        }

        private static BaseGameManager _gameMan;
        private static bool _isTutorial;
        public static bool IsTutorial
        {
            get
            {
                if (BaseGameManager.Instance != _gameMan)
                {
                    _gameMan = BaseGameManager.Instance;
                    _isTutorial = _gameMan is TutorialGameManager;
                }
                return _isTutorial;
            }
        }

        public static bool HasDescriptionOverride(this ItemObject itm, out string newText)
        {
            newText = "";

            ItemMetaData meta = itm.GetMeta();
            if (meta != null && ItemFeesPlugin.descOverrides.TryGetValue(meta, out string newDesc))
            {
                newText = string.Format(LocalizationManager.Instance.GetLocalizedText(newDesc), itm.GetUsageCost());
                return true;
            }
            return false;
        }
    }
}
