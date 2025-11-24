
using UnityEngine;

namespace UncertainLuei.BaldiPlus.ItemFees
{
    class SwingDoor_Points : SwingDoor, IClickable<int>
    {
        public int requiredPoints;
        public Material pointDoorOverlay;

        private bool isPointDoor, originalLockOverride;
        private Material[] originalOverlays = [];

        public override void Initialize()
        {
            base.Initialize();

            isPointDoor = true;

            originalLockOverride = acceptsLockItem;
            acceptsLockItem = false;

            if (originalOverlays.Length == 0)
            {
                originalOverlays = new Material[overlayLocked.Length];
                for (int i = 0; i < overlayLocked.Length; i++)
                {
                    originalOverlays[i] = overlayLocked[i];
                    overlayLocked[i] = pointDoorOverlay;
                }
            }
            Lock(cancelTimer: true);
        }

        public void Clicked(int player)
        {
            if (ClickableHidden()) return;
            if (CoreGameManager.Instance.GetPoints(player) < requiredPoints)
                return;

            isPointDoor = false;
            CoreGameManager.Instance.AddPoints(-requiredPoints, player, false, false, false);
            acceptsLockItem = originalLockOverride;
            for (int i = 0; i < overlayLocked.Length; i++)
            {
                overlayLocked[i] = originalOverlays[i];
            }
            Unlock();
        }

        public void ClickableSighted(int player)
        {}
        public void ClickableUnsighted(int player)
        {}
        public bool ClickableHidden() => !isPointDoor;
        public bool ClickableRequiresNormalHeight() => false;
    }
}