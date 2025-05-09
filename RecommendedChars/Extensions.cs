using MTM101BaldAPI;

using System.Collections.Generic;
using System.Linq;

namespace UncertainLuei.BaldiPlus.RecommendedChars
{
    public static class WeightedHelperExtensions
    {
        public static T Weighted<X, T>(this X selection, int weight) where T : WeightedSelection<X>, new()
        {
            return new T() { selection = selection, weight = weight };
        }
        public static WeightedNPC Weighted(this NPC selection, int weight) => selection.Weighted<NPC, WeightedNPC>(weight);
        public static WeightedItemObject Weighted(this ItemObject selection, int weight) => selection.Weighted<ItemObject, WeightedItemObject>(weight);

        // Done for future-proofing
        public static void CopyCharacterWeight(this List<WeightedNPC> list, Character characterToCopy, NPC newNpc)
        {
            WeightedNPC weightedToCopy = list.First(x => x.selection?.character == characterToCopy);

            if (weightedToCopy == null)
            {
                RecommendedCharsPlugin.Log.LogWarning($"No character with enum {characterToCopy.ToStringExtended()} exists to copy in the list!");
                return;
            }
            list.Add(newNpc.Weighted(weightedToCopy.weight));
        }
    }
}
