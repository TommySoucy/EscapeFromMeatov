using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_VanillaItemDescriptor : MonoBehaviour
    {
        public string H3ID; // ID of item in H3VR
        public string tarkovID; // ID of item in Tarkov
        public string description; // Item's description
        public float lootExperience; // Experience gained upon pickup
        public Mod.ItemRarity rarity; // Rarity of item
        public float spawnChance; // Spawn chance of item in percentage (0-100) generate rand float between 0 and 100, make a list of any item that has spawnchance > generated float, take a random item out of the list, this is the one we want to spawn
        public int creditCost; // Value of item in rubles(?)
        public Sprite sprite; // Icon for this item, only assigned if it has no custom icon
        public string parent; // The parent ID of this item, the category this item is really
        public bool looted; // Whether this item has been looted before
    }
}
