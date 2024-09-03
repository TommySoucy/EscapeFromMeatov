using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class LootContainer : MonoBehaviour
    {
        public enum Mode
        {
            StaticLootData,
            SpecificItems,
            Rarity,
            Category
        }

        public ContainmentVolume volume;

        public float presenceProbability; // Probability of this loot container to even exist
        public int spawnAttemptCount; // Number of attempts to make of spawning an item in this loot container
        public int maxItemCount = -1; // -1 is unlimited
        public int maxVolume = -1; // -1 is unlimited

        public float emptyProbability;
        public Mode mode;

        // StaticLootData
        public string staticLootID; // ID of this loot container in DB/loot/staticLoot.json

        // SpecificItems
        public int totalProbabilityWeight = -1; // Total of all probability weight
        public List<string> items;
        public List<int> itemProbabilityWeight; // Spawn probability weight
        public List<int> itemStack; // Max stack

        // Rarity
        public MeatovItem.ItemRarity rarity;

        // Category
        public string category;

        TODO
    }
}
