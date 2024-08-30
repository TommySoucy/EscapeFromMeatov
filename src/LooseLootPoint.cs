using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class LooseLootPoint : MonoBehaviour
    {
        public enum Mode
        {
            SpecificItems,
            Rarity,
            Category
        }

        public float probability; // 0 to 1
        public bool isKinematic; // Whether the spawnd loot should be set to kinematic (No physics)
        public bool randomRotation;
        public List<KeyValuePair<Vector3, Vector3>> transformData; // Key: Position, Value: Rotation (Rotation only used if !randomRotation)
        public Mode mode;
        public int totalProbabilityWeight = -1; // Total of all probability weight for specific items mode
        public List<string> items; // Only used if mode == SpecificItems, Item Tarkov ID
        public List<int> itemProbabilityWeight; // Only used if mode == SpecificItems, Spawn probability weight
        public List<int> itemStack; // Only used if mode == SpecificItems, Max stack
        public MeatovItem.ItemRarity rarity; // Only used if mode == Rarity
        public string category; // Only used if mode == Category
    }
}
