using FistVR;
using System;
using System.Reflection;
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

        public static void WearEquipment(MeatovItem customItemWrapper)
        {
            Mod.LogInfo("WearEquipment called on " + customItemWrapper.gameObject.name);
            //EFM_CustomItemWrapper customItemWrapper = CurObject.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                // Close if necessary
                if (customItemWrapper.open)
                {
                    customItemWrapper.ToggleMode(false);
                }

                switch (customItemWrapper.itemType)
                {
                    case MeatovItem.ItemType.Helmet:
                    case MeatovItem.ItemType.Headwear:
                        if (!wearingHeadwear)
                        {
                            wearingHeadwear = true;
                            currentHeadwear = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.Rig:
                        if (!wearingRig && !wearingArmoredRig)
                        {
                            EquipRig(customItemWrapper);
                            wearingRig = true;
                        }
                        break;
                    case MeatovItem.ItemType.BodyArmor:
                        if (!wearingBodyArmor)
                        {
                            wearingBodyArmor = true;
                            currentArmor = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.ArmoredRig:
                        if (!wearingArmoredRig && !wearingRig)
                        {
                            EquipRig(customItemWrapper);
                            wearingArmoredRig = true;
                            currentArmor = customItemWrapper;
                            currentRig = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.Backpack:
                        if (!wearingBackpack)
                        {
                            wearingBackpack = true;
                            currentBackpack = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.Pouch:
                        if (!wearingPouch)
                        {
                            wearingPouch = true;
                            currentPouch = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.Earpiece:
                        if (!wearingEarpiece)
                        {
                            wearingEarpiece = true;
                            currentEarpiece = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.FaceCover:
                        if (!wearingFaceCover)
                        {
                            wearingFaceCover = true;
                            currentFaceCover = customItemWrapper;
                        }
                        break;
                    case MeatovItem.ItemType.Eyewear:
                        if (!wearingEyewear)
                        {
                            wearingEyewear = true;
                            currentEyewear = customItemWrapper;
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

        public static void TakeOffEquipment(MeatovItem customItemWrapper)
        {
            //EFM_CustomItemWrapper customItemWrapper = objectLastFrame.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                switch (customItemWrapper.itemType)
                {
                    case MeatovItem.ItemType.Helmet:
                    case MeatovItem.ItemType.Headwear:
                        wearingHeadwear = false;
                        currentHeadwear = null;
                        break;
                    case MeatovItem.ItemType.Rig:
                        // Transfer item objects from config to the rig object
                        for (int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                        {
                            if(customItemWrapper.itemsInSlots[i] != null)
                            {
                                customItemWrapper.itemsInSlots[i].SetActive(false);
                            }
                        }
                        GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
                        wearingRig = false;
                        currentRig = null; // This needs to be done after ConfigureQuickbelt(-1) because it is used in it
                        break;
                    case MeatovItem.ItemType.BodyArmor:
                        wearingBodyArmor = false;
                        currentArmor = null;
                        break;
                    case MeatovItem.ItemType.ArmoredRig:
                        // Transfer item objects from config to the rig object
                        for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
                        {
                            if (customItemWrapper.itemsInSlots[i] != null)
                            {
                                customItemWrapper.itemsInSlots[i].SetActive(false);
                            }
                        }
                        GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
                        wearingArmoredRig = false;
                        currentRig = null; // This needs to be done after ConfigureQuickbelt(-1) because it is used in it
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

        private static void EquipRig(MeatovItem customItemWrapper)
        {
            Mod.LogInfo("Equip rig called on "+customItemWrapper.gameObject.name);
            // Load the config
            GM.CurrentPlayerBody.ConfigureQuickbelt(customItemWrapper.configurationIndex);

            // Load items into their slots
            for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
            {
                if (customItemWrapper.itemsInSlots[i] != null)
                {
                    FVRPhysicalObject physicalObject = customItemWrapper.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                    physicalObject.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i + 4]);
                    physicalObject.SetParentage(null);
                    physicalObject.transform.localScale = Vector3.one;
                    customItemWrapper.itemsInSlots[i].transform.localPosition = Vector3.zero;
                    customItemWrapper.itemsInSlots[i].transform.localRotation = Quaternion.identity;
                    FieldInfo grabPointTransformField = typeof(FVRPhysicalObject).GetField("m_grabPointTransform", BindingFlags.NonPublic | BindingFlags.Instance);
                    Transform m_grabPointTransform = (Transform)grabPointTransformField.GetValue(physicalObject);
                    if (m_grabPointTransform != null)
                    {
                        if (physicalObject.QBPoseOverride != null)
                        {
                            m_grabPointTransform.position = physicalObject.QBPoseOverride.position;
                            m_grabPointTransform.rotation = physicalObject.QBPoseOverride.rotation;
                        }
                        else if (physicalObject.PoseOverride != null)
                        {
                            m_grabPointTransform.position = physicalObject.PoseOverride.position;
                            m_grabPointTransform.rotation = physicalObject.PoseOverride.rotation;
                        }
                    }
                    customItemWrapper.itemsInSlots[i].SetActive(true);
                }
            }

            currentRig = customItemWrapper;
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

        public MeatovItem.ItemType equipmentType;
    }
}
