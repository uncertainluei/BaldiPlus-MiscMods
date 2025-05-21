﻿using HarmonyLib;
using UnityEngine;

namespace UncertainLuei.BaldiPlus.PedanticBugFixes.Patches
{
    [HarmonyPatch(typeof(Map))]
    class MapPatches
    {
        [HarmonyPatch("Find"), HarmonyPostfix]
        private static void Find(int posX, int posZ, RoomController room, MapTile[,] ___tiles)
        {
            if (___tiles[posX, posZ] == null) return;

            MapTile mapTile;
            foreach (Transform child in ___tiles[posX,posZ].transform)
            {
                if (!child.TryGetComponent(out mapTile)) continue;

                if (mapTile.SpriteRenderer.sprite.name.StartsWith("Icon_Door"))
                    mapTile.SpriteRenderer.color = room.color;
            }
        }
    }
}
