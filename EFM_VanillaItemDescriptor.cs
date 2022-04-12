using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_VanillaItemDescriptor : MonoBehaviour, EFM_Describable
    {
        public string H3ID; // ID of item in H3VR
        public string tarkovID; // ID of item in Tarkov
        public string description; // Item's description
        public int lootExperience; // Experience gained upon pickup
        public Mod.ItemRarity rarity; // Rarity of item
        public float spawnChance; // Spawn chance of item in percentage (0-100) generate rand float between 0 and 100, make a list of any item that has spawnchance > generated float, take a random item out of the list, this is the one we want to spawn
        public int creditCost; // Value of item in rubles(?)
        public Sprite sprite; // Icon for this item, only assigned if it has no custom icon
        public string parent; // The parent ID of this item, the category this item is really
        public bool looted; // Whether this item has been looted before
        public string itemName;
        public int compatibilityValue; // 0: Does not need mag or round, 1: Needs mag, 2: Needs round, 3: Needs both
        public bool usesMags; // Could be clip
        public bool usesAmmoContainers; // Could be internal mag or revolver
        public FireArmMagazineType magType;
        public FireArmClipType clipType;
        public FireArmRoundType roundType;
        public DescriptionPack descriptionPack;
        public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
        public int locationIndex; // 0: Player inventory, 1: Base, 2: Raid. This is to keep track of where an item is in general
        public EFM_DescriptionManager descriptionManager; // The current description manager displaying this item's description
        private bool _insured;
        public bool insured
        {
            get { return _insured; }
            set
            {
                _insured = value;
                if (descriptionManager != null)
                {
                    descriptionManager.SetDescriptionPack();
                }
            }
        }

        public void Start()
        {
            if (takeCurrentLocation)
            {
                locationIndex = Mod.currentLocationIndex;
            }

            descriptionPack = new DescriptionPack();
            descriptionPack.isCustom = false;
            descriptionPack.vanillaItem = this;
            descriptionPack.name = itemName;
            descriptionPack.description = description;
            descriptionPack.icon = Mod.itemIcons[H3ID];
        }

        public DescriptionPack GetDescriptionPack()
        {
            descriptionPack.amount = Mod.baseInventory[H3ID] + Mod.playerInventory[H3ID];
            for (int i = 0; i < 22; ++i)
            {
                if (Mod.requiredPerArea[i] != null && Mod.requiredPerArea[i].ContainsKey(H3ID))
                {
                    descriptionPack.amountRequired += Mod.requiredPerArea[i][H3ID];
                    descriptionPack.amountRequiredPerArea[i] = Mod.requiredPerArea[i][H3ID];

                }
                else
                {
                    descriptionPack.amountRequiredPerArea[i] = 0;
                }
            }
            descriptionPack.onWishlist = Mod.wishList.Contains(H3ID);
            descriptionPack.insured = insured;

            if (compatibilityValue == 3 || compatibilityValue == 1) 
            {
                if (usesAmmoContainers)
                {
                    if (usesMags)
                    {
                        if (Mod.magazinesByType.ContainsKey(magType))
                        {
                            descriptionPack.compatibleAmmoContainers = Mod.magazinesByType[magType];
                        }
                        else
                        {
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                    else
                    {
                        if (Mod.clipsByType.ContainsKey(clipType))
                        {
                            descriptionPack.compatibleAmmoContainers = Mod.clipsByType[clipType];
                        }
                        else
                        {
                            descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                        }
                    }
                }
            }
            if(compatibilityValue == 3 || compatibilityValue == 2)
            {
                if (Mod.roundsByType.ContainsKey(roundType))
                {
                    descriptionPack.compatibleAmmo = Mod.roundsByType[roundType];
                }
                else
                {
                    descriptionPack.compatibleAmmo = new Dictionary<string, int>();
                }
            }
            descriptionPack.amountRequiredQuest = Mod.requiredForQuest[H3ID];

            return descriptionPack;
        }
    }
}
