using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.ShapeWorldCircle
{
    class ITM_NerfGun : Item
    {
        public ItemObject leftover;

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

            if (leftover)
            {
                pm.itm.SetItem(leftover, pm.itm.selectedItem);
                return false;
            }
            return true;
        }
    }
}
