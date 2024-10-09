using FistVR;
using UnityEngine;

namespace EFM
{
    public class EquipmentSlot : FVRQuickBeltSlot
    {
        public static bool wearingHeadwear;
        public static MeatovItem currentHeadwear;
        public static bool wearingBodyArmor;
        public static MeatovItem currentArmor;
        public static bool wearingRig;
        public static MeatovItem currentRig;
        public static bool wearingArmoredRig;
        public static bool wearingBackpack;
        public static MeatovItem currentBackpack;
        public static bool wearingPouch;
        public static MeatovItem currentPouch;
        public static bool wearingEarpiece;
        public static MeatovItem currentEarpiece;
        public static bool wearingFaceCover;
        public static MeatovItem currentFaceCover;
        public static bool wearingEyewear;
        public static MeatovItem currentEyewear;

        public MeatovItem.ItemType equipmentType;

        public static void WearEquipment(MeatovItem item)
        {
            Mod.LogInfo("WearEquipment called on " + item.gameObject.name);
            if (item != null)
            {
                // Close if necessary
                if (item.open)
                {
                    item.ToggleMode(false);
                }

                switch (item.itemType)
                {
                    case MeatovItem.ItemType.Helmet:
                    case MeatovItem.ItemType.Headwear:
                        if (!wearingHeadwear)
                        {
                            wearingHeadwear = true;
                            currentHeadwear = item;
                        }
                        break;
                    case MeatovItem.ItemType.Rig:
                        if (!wearingRig && !wearingArmoredRig)
                        {
                            EquipRig(item);
                            wearingRig = true;
                        }
                        break;
                    case MeatovItem.ItemType.BodyArmor:
                        if (!wearingBodyArmor)
                        {
                            wearingBodyArmor = true;
                            currentArmor = item;
                        }
                        break;
                    case MeatovItem.ItemType.ArmoredRig:
                        if (!wearingArmoredRig && !wearingRig)
                        {
                            EquipRig(item);
                            wearingArmoredRig = true;
                            currentArmor = item;
                            currentRig = item;
                        }
                        break;
                    case MeatovItem.ItemType.Backpack:
                        if (!wearingBackpack)
                        {
                            wearingBackpack = true;
                            currentBackpack = item;
                        }
                        break;
                    case MeatovItem.ItemType.Pouch:
                        if (!wearingPouch)
                        {
                            wearingPouch = true;
                            currentPouch = item;
                        }
                        break;
                    case MeatovItem.ItemType.Earpiece:
                        if (!wearingEarpiece)
                        {
                            wearingEarpiece = true;
                            currentEarpiece = item;
                        }
                        break;
                    case MeatovItem.ItemType.FaceCover:
                        if (!wearingFaceCover)
                        {
                            wearingFaceCover = true;
                            currentFaceCover = item;
                        }
                        break;
                    case MeatovItem.ItemType.Eyewear:
                        if (!wearingEyewear)
                        {
                            wearingEyewear = true;
                            currentEyewear = item;
                        }
                        break;
                    default:
                        Mod.LogError("This object has invalid item type");
                        break;
                }
            }
            else
            {
                Mod.LogWarning("Could not find custom item wrapper on current object in equip. slot");
            }
        }

        public static void TakeOffEquipment(MeatovItem item)
        {
            //EFM_CustomItemWrapper customItemWrapper = objectLastFrame.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (item != null)
            {
                switch (item.itemType)
                {
                    case MeatovItem.ItemType.Helmet:
                    case MeatovItem.ItemType.Headwear:
                        wearingHeadwear = false;
                        currentHeadwear = null;
                        break;
                    case MeatovItem.ItemType.Rig:
                        UnequipRig();
                        wearingRig = false;
                        break;
                    case MeatovItem.ItemType.BodyArmor:
                        wearingBodyArmor = false;
                        currentArmor = null;
                        break;
                    case MeatovItem.ItemType.ArmoredRig:
                        UnequipRig();
                        wearingArmoredRig = false;
                        currentArmor = null;
                        break;
                    case MeatovItem.ItemType.Backpack:
                        wearingBackpack = false;
                        currentBackpack = null;
                        break;
                    case MeatovItem.ItemType.Pouch:
                        wearingPouch = false;
                        currentPouch = null;
                        break;
                    case MeatovItem.ItemType.Earpiece:
                        wearingEarpiece = false;
                        currentEarpiece = null;
                        break;
                    case MeatovItem.ItemType.FaceCover:
                        wearingFaceCover = false;
                        currentFaceCover = null;
                        break;
                    case MeatovItem.ItemType.Eyewear:
                        wearingEyewear = false;
                        currentEyewear = null;
                        break;
                    default:
                        Mod.LogError("This object has invalid item type");
                        break;
                }
            }
            else
            {
                Mod.LogWarning("Could not find custom item wrapper on current object in equip. slot");
            }
        }

        private static void EquipRig(MeatovItem item)
        {
            Mod.LogInfo("Equip rig called on "+item.itemName);
            // Load the config
            GM.CurrentPlayerBody.ConfigureQuickbelt(item.configurationIndex);

            // Load items into their slots
            for (int i = 0; i < item.itemsInSlots.Length; ++i)
            {
                if (item.itemsInSlots[i] != null)
                {
                    FVRPhysicalObject physicalObject = item.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                    SetQuickBeltSlotPatch.dontProcessRigWeight = true; // Dont want to add the weight of this item to the rig as we set its slot, the item is already in the rig
                    physicalObject.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i + 6]);
                    SetQuickBeltSlotPatch.dontProcessRigWeight = false;
                    physicalObject.transform.localScale = Vector3.one;

                    // Note that when we equip a rig, it is closed automatically prior to putting it into the slot
                    // That will set the items inactive, and we are setting them active again here
                    item.itemsInSlots[i].gameObject.SetActive(true);
                }
            }

            currentRig = item;
        }

        private static void UnequipRig()
        {
            if(currentRig != null)
            {
                Mod.LogInfo("UnequipRig called on " + currentRig.itemName);
                // Load the config
                ConfigureQuickbeltPatch.overrideIndex = true;
                ConfigureQuickbeltPatch.actualConfigIndex = -1;
                GM.CurrentPlayerBody.ConfigureQuickbelt(-1);

                // Load items into their slots
                for (int i = 0; i < currentRig.itemsInSlots.Length; ++i)
                {
                    if (currentRig.itemsInSlots[i] != null)
                    {
                        MeatovItem item = currentRig.itemsInSlots[i];
                        SetQuickBeltSlotPatch.dontProcessRigWeight = true; // Dont want to add the weight of this item to the rig as we set its slot, the item is already in the rig
                        item.physObj.SetQuickBeltSlot(currentRig.rigSlots[i]);
                        SetQuickBeltSlotPatch.dontProcessRigWeight = false;
                        item.physObj.transform.localScale = Vector3.one;
                        currentRig.itemsInSlots[i].gameObject.SetActive(currentRig.open);
                    }
                }

                currentRig = null;
            }
        }

        public static void Clear()
        {
            wearingHeadwear = false;
            wearingArmoredRig = false;
            wearingBodyArmor = false;
            wearingRig = false;
            wearingBackpack = false;
            wearingPouch = false;

            currentHeadwear = null;
            currentArmor = null;
            currentRig = null;
            currentBackpack = null;
            currentPouch = null;
        }
    }
}
