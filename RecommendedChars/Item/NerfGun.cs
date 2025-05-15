namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    class ITM_NerfGun : Item
    {
        public ItemObject nextStage;

        public override bool Use(PlayerManager pm)
        {
            Destroy(gameObject);

            if (pm.jumpropes.Count == 0) return false;

            bool fail = true;
            for (int i = pm.jumpropes.Count-1; i >= 0; i--)
            {
                if (pm.jumpropes[i] is CircleJumprope)
                {
                    fail = false;
                    pm.jumpropes[i].End(false);
                }
            }
            if (fail) return false;

            if (nextStage)
            {
                pm.itm.SetItem(nextStage, pm.itm.selectedItem);
                return false;
            }
            return true;
        }
    }
}
