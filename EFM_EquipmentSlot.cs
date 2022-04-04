using FistVR;
using System.Reflection;
using UnityEngine;

namespace EFM
{
    public class EFM_EquipmentSlot : FVRQuickBeltSlot
    {
        public static bool wearingHelmet;
        public static EFM_CustomItemWrapper currentHelmet;
        public static bool wearingBodyArmor;
        public static EFM_CustomItemWrapper currentArmor;
        public static bool wearingRig;
        public static EFM_CustomItemWrapper currentRig;
        public static bool wearingArmoredRig;
        public static bool wearingBackpack;
        public static EFM_CustomItemWrapper currentBackpack;
        public static bool wearingPouch;
        public static EFM_CustomItemWrapper currentPouch;

        public static void WearEquipment(EFM_CustomItemWrapper customItemWrapper)
        {
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
                    case Mod.ItemType.Helmet:
                        if (!wearingHelmet)
                        {
                            // TODO: Equip it physically if necessary
                            wearingHelmet = true;
                            currentHelmet = customItemWrapper;
                        }
                        break;
                    case Mod.ItemType.Rig:
                        if (!wearingRig && !wearingArmoredRig)
                        {
                            // TODO: Equip it physically if necessary
                            EquipRig(customItemWrapper);
                            wearingRig = true;
                        }
                        break;
                    case Mod.ItemType.BodyArmor:
                        if (!wearingBodyArmor)
                        {
                            // TODO: Equip it physically if necessary
                            wearingBodyArmor = true;
                            currentArmor = customItemWrapper;
                        }
                        break;
                    case Mod.ItemType.ArmoredRig:
                        if (!wearingArmoredRig && !wearingRig)
                        {
                            // TODO: Equip it physically if necessary
                            EquipRig(customItemWrapper);
                            wearingArmoredRig = true;
                            currentArmor = customItemWrapper;
                        }
                        break;
                    case Mod.ItemType.Backpack:
                        if (!wearingBackpack)
                        {
                            // TODO: Set backpack inactive
                            // TODO: Equip it physically if necessary
                            wearingBackpack = true;
                            currentBackpack = customItemWrapper;
                        }
                        break;
                    case Mod.ItemType.Pouch:
                        if (!wearingPouch)
                        {
                            // TODO: Set Pouch inactive
                            wearingPouch = true;
                            currentPouch = customItemWrapper;
                        }
                        break;
                    default:
                        Mod.instance.LogError("This object has invalid item type");
                        break;
                }
            }
            else
            {
                Mod.instance.LogWarning("Could not find custom item wrapper on current object in equip. slot");
            }
        }

        public static void TakeOffEquipment(EFM_CustomItemWrapper customItemWrapper)
        {
            //EFM_CustomItemWrapper customItemWrapper = objectLastFrame.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                switch (customItemWrapper.itemType)
                {
                    case Mod.ItemType.Helmet:
                        // TODO: Remove it physically if necessary
                        wearingHelmet = false;
                        currentHelmet = null;
                        break;
                    case Mod.ItemType.Rig:
                        // TODO: Remove it physically if necessary
                        // Transfer item objects from config to the rig object
                        for (int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                        {
                            if(customItemWrapper.itemsInSlots[i] != null)
                            {
                                customItemWrapper.itemsInSlots[i].SetActive(false);
                                customItemWrapper.itemsInSlots[i].transform.parent = customItemWrapper.itemObjectsRoot;
                            }
                        }
                        GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
                        wearingRig = false;
                        currentRig = null; // This needs to be done after ConfigureQuickbelt(-1) because it is used in it
                        break;
                    case Mod.ItemType.BodyArmor:
                        // TODO: Remove it physically if necessary
                        wearingBodyArmor = false;
                        currentArmor = null;
                        break;
                    case Mod.ItemType.ArmoredRig:
                        // TODO: Remove it physically if necessary
                        // Transfer item objects from config to the rig object
                        for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
                        {
                            if (customItemWrapper.itemsInSlots[i] != null)
                            {
                                customItemWrapper.itemsInSlots[i].SetActive(false);
                                customItemWrapper.itemsInSlots[i].transform.parent = customItemWrapper.itemObjectsRoot;
                            }
                        }
                        GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
                        wearingArmoredRig = false;
                        currentRig = null; // This needs to be done after ConfigureQuickbelt(-1) because it is used in it
                        currentArmor = null;
                        break;
                    case Mod.ItemType.Backpack:
                        // TODO: Set backpack active or change its model? so that it shows properly in the slot
                        // TODO: Remove it physically if necessary
                        wearingBackpack = false;
                        currentBackpack = null;
                        break;
                    case Mod.ItemType.Pouch:
                        // TODO: Set Pouch active so that it shows properly in the slot
                        wearingPouch = false;
                        currentPouch = null;
                        break;
                    default:
                        Mod.instance.LogError("This object has invalid item type");
                        break;
                }
            }
            else
            {
                Mod.instance.LogWarning("Could not find custom item wrapper on current object in equip. slot");
            }
        }

        private static void EquipRig(EFM_CustomItemWrapper customItemWrapper)
        {
            Mod.instance.LogInfo("EquipRig called with item name: "+customItemWrapper.name);

            // Load the config
            GM.CurrentPlayerBody.ConfigureQuickbelt(customItemWrapper.configurationIndex);

            // Load items into their slots
            for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
            {
                if (customItemWrapper.itemsInSlots[i] != null)
                {
                    Mod.instance.LogInfo("Item in slot "+i+" not null: " + customItemWrapper.name);
                    FVRPhysicalObject physicalObject = customItemWrapper.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                    physicalObject.SetQuickBeltSlot(GM.CurrentPlayerBody.QuickbeltSlots[i]);
                    physicalObject.SetParentage(GM.CurrentPlayerBody.QuickbeltSlots[i].gameObject.transform);
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
            wearingHelmet = false;
            wearingArmoredRig = false;
            wearingBodyArmor = false;
            wearingRig = false;
            wearingBackpack = false;
            wearingPouch = false;

            currentHelmet = null;
            currentArmor = null;
            currentRig = null;
            currentBackpack = null;
            currentPouch = null;
        }
    }
}
