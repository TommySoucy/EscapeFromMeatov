using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class EFM_Hand : MonoBehaviour
    {
        public EFM_Hand otherHand;
        public EFM_CustomItemWrapper collidingContainerWrapper;
        public EFM_CustomItemWrapper collidingTogglableWrapper;
        public EFM_TradeVolume collidingTradeVolume;
        public EFM_Switch collidingSwitch;
        public bool hoverValid;
        private List<Collider> colliders;
        public FVRViveHand fvrHand;
        public bool consuming;
        public GameObject currentHeldItem;

        private void Awake()
        {
            fvrHand = transform.GetComponent<FVRViveHand>();
            colliders = new List<Collider>();
        }

        private void Update()
        {
            if (fvrHand.CurrentInteractable == null && collidingTogglableWrapper != null)
            {
                if (fvrHand.IsInStreamlinedMode)
                {
                    if (fvrHand.Input.TriggerDown || fvrHand.Input.IsGrabDown)
                    {
                        switch (collidingTogglableWrapper.itemType)
                        {
                            case Mod.ItemType.ArmoredRig:
                            case Mod.ItemType.Rig:
                            case Mod.ItemType.Backpack:
                            case Mod.ItemType.BodyArmor:
                            case Mod.ItemType.Container:
                            case Mod.ItemType.Pouch:
                            case Mod.ItemType.LootContainer:
                                collidingTogglableWrapper.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                break;
                            default:
                                break;
                        }
                    }
                    else if (fvrHand.Input.AXButtonDown)
                    {
                        switch (collidingTogglableWrapper.itemType)
                        {
                            case Mod.ItemType.ArmoredRig:
                            case Mod.ItemType.Rig:
                            case Mod.ItemType.Backpack:
                            case Mod.ItemType.BodyArmor:
                            case Mod.ItemType.Container:
                            case Mod.ItemType.Pouch:
                            case Mod.ItemType.LootContainer:
                                collidingTogglableWrapper.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    if (fvrHand.Input.TriggerDown || fvrHand.Input.IsGrabDown)
                    {
                        switch (collidingTogglableWrapper.itemType)
                        {
                            case Mod.ItemType.ArmoredRig:
                            case Mod.ItemType.Rig:
                            case Mod.ItemType.Backpack:
                            case Mod.ItemType.BodyArmor:
                            case Mod.ItemType.Container:
                            case Mod.ItemType.Pouch:
                            case Mod.ItemType.LootContainer:
                                collidingTogglableWrapper.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        Vector2 touchpadAxes = fvrHand.Input.TouchpadAxes;

                        // If touchpad has started being pressed this frame
                        if (fvrHand.Input.TouchpadDown)
                        {
                            Vector2 TouchpadClickInitiation = touchpadAxes;
                            if (touchpadAxes.magnitude > 0.2f)
                            {
                                if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
                                {
                                    switch (collidingTogglableWrapper.itemType)
                                    {
                                        case Mod.ItemType.ArmoredRig:
                                        case Mod.ItemType.Rig:
                                        case Mod.ItemType.Backpack:
                                        case Mod.ItemType.BodyArmor:
                                        case Mod.ItemType.Container:
                                        case Mod.ItemType.Pouch:
                                        case Mod.ItemType.LootContainer:
                                            collidingTogglableWrapper.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else if(fvrHand.CurrentInteractable == null && collidingSwitch != null)
            {
                // TODO: maybe change this to trigger instead of interaction button we use for containers and other custom functional items
                if (fvrHand.IsInStreamlinedMode)
                {
                    if(fvrHand.Input.TriggerDown || fvrHand.Input.IsGrabDown)
                    {
                        collidingSwitch.Activate();
                    }
                    else if (fvrHand.Input.AXButtonDown)
                    {
                        collidingSwitch.Activate();
                    }
                }
                else
                {
                    if (fvrHand.Input.TriggerDown || fvrHand.Input.IsGrabDown)
                    {
                        collidingSwitch.Activate();
                    }
                    else
                    {
                        Vector2 touchpadAxes = fvrHand.Input.TouchpadAxes;

                        // If touchpad has started being pressed this frame
                        if (fvrHand.Input.TouchpadDown)
                        {
                            Vector2 TouchpadClickInitiation = touchpadAxes;
                            if (touchpadAxes.magnitude > 0.2f)
                            {
                                if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
                                {
                                    collidingSwitch.Activate();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            Transform mainContainerTransform = null;
            if (collider.gameObject.name.Equals("MainContainer"))
            {
                Mod.instance.LogInfo("Hand entered collider named MainContainer");
                mainContainerTransform = collider.transform;
            }
            else if (collider.gameObject.GetComponent<EFM_Switch>() != null)
            {
                collidingSwitch = collider.gameObject.GetComponent<EFM_Switch>();
                colliders.Add(collider);
            }
            else if (collider.transform.parent != null && collider.transform.parent.GetComponent<EFM_TradeVolume>() != null)
            {
                collidingTradeVolume = collider.transform.parent.GetComponent<EFM_TradeVolume>();
                colliders.Add(collider);
            }
            else if (collider.transform.parent != null)
            {
                if (collider.transform.parent.name.Equals("MainContainer"))
                {
                    mainContainerTransform = collider.transform.parent;
                }
                else if (collider.gameObject.name.Equals("Interactive"))
                {
                    EFM_CustomItemWrapper lootContainerCIW = collider.transform.parent.GetComponent<EFM_CustomItemWrapper>();
                    if (lootContainerCIW != null && lootContainerCIW.itemType == Mod.ItemType.LootContainer)
                    {
                        collidingTogglableWrapper = lootContainerCIW;
                        colliders.Add(collider);
                    }
                }
                else if (collider.transform.parent.name.Equals("Interactives") && collider.transform.parent.parent != null)
                {
                    EFM_CustomItemWrapper lootContainerCIW = collider.transform.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                    if (lootContainerCIW != null && lootContainerCIW.itemType == Mod.ItemType.LootContainer)
                    {
                        collidingTogglableWrapper = lootContainerCIW;
                        colliders.Add(collider);
                    }
                }
                else if (collider.transform.parent.parent != null)
                {
                    if (collider.transform.parent.parent.name.Equals("Interactives") && collider.transform.parent.parent.parent != null)
                    {
                        EFM_CustomItemWrapper itemCIW = collider.transform.parent.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                        if (itemCIW != null && (fvrHand.CurrentInteractable == null || !fvrHand.CurrentInteractable.Equals(itemCIW.physObj)) &&
                            (itemCIW.itemType == Mod.ItemType.Container ||
                             itemCIW.itemType == Mod.ItemType.Backpack ||
                             itemCIW.itemType == Mod.ItemType.Pouch))
                        {
                            collidingTogglableWrapper = itemCIW;
                            colliders.Add(collider);
                        }
                    }
                }
            }

            if (mainContainerTransform != null)
            {
                EFM_CustomItemWrapper customItemWrapper = mainContainerTransform.parent.GetComponent<EFM_CustomItemWrapper>();
                if (customItemWrapper != null && (fvrHand.CurrentInteractable == null || !fvrHand.CurrentInteractable.Equals(customItemWrapper.physObj)) &&
                    (customItemWrapper.itemType == Mod.ItemType.Backpack ||
                     customItemWrapper.itemType == Mod.ItemType.Container ||
                     customItemWrapper.itemType == Mod.ItemType.Pouch))
                {
                    Mod.instance.LogInfo("\tMainCollider is part of some kind of container");
                    collidingContainerWrapper = customItemWrapper;
                    colliders.Add(collider);
                }
            }

            // Set container hovered if necessary
            if (collidingContainerWrapper != null && collidingContainerWrapper.canInsertItems)
            {
                Mod.instance.LogInfo("\tCna insert items into this container");
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf)
                {
                    Mod.instance.LogInfo("\t\tContainer is active");
                    // Set material, if this hand is also holding something that fits in the container, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
                        Mod.instance.LogInfo("\t\t\tThis hand is holding an item");
                        float volumeToUse = 0;
                        string IDToUse = "";
                        List<string> parentsToUse = null;
                        FVRPhysicalObject physicalObject = fvrHand.CurrentInteractable as FVRPhysicalObject;
                        EFM_CustomItemWrapper heldCustomItemWrapper = fvrHand.CurrentInteractable.GetComponent<EFM_CustomItemWrapper>();
                        EFM_VanillaItemDescriptor heldVanillaItemDescriptor = fvrHand.CurrentInteractable.GetComponent<EFM_VanillaItemDescriptor>();
                        if (heldCustomItemWrapper != null)
                        {
                            volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];

                            IDToUse = heldCustomItemWrapper.ID;
                            parentsToUse = heldCustomItemWrapper.parents;
                        }
                        else
                        {
                            volumeToUse = Mod.sizeVolumes[(int)physicalObject.Size];

                            if (heldVanillaItemDescriptor != null)
                            {
                                IDToUse = heldVanillaItemDescriptor.H3ID;
                                parentsToUse = heldVanillaItemDescriptor.parents;
                            }
                            else
                            {
                                Mod.instance.LogError("Non described item held in hand while entering container!");
                                return;
                            }
                        }

                        // Check if volume fits in bag
                        if (collidingContainerWrapper.containingVolume + volumeToUse <= collidingContainerWrapper.maxVolume)
                        {
                            Mod.instance.LogInfo("\t\t\t\tVolume fits");
                            // Also check if the container can contain the item through filters
                            if (collidingContainerWrapper.whiteList != null)
                            {
                                // If whitelist includes item and blacklist doesn't
                                if (EFM_CustomItemWrapper.ItemFitsInContainer(IDToUse, parentsToUse, collidingContainerWrapper.whiteList, collidingContainerWrapper.blackList))
                                {
                                    Mod.instance.LogInfo("\t\t\t\tpasses filter");
                                    hoverValid = true;
                                    collidingContainerWrapper.SetContainerHovered(true);
                                    fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                                }
                                else
                                {
                                    hoverValid = false;
                                    collidingContainerWrapper.SetContainerHovered(false);
                                }
                            }
                            else
                            {
                                Mod.instance.LogInfo("\t\t\t\t\tContainer does not have whitelist");
                                hoverValid = true;
                                collidingContainerWrapper.SetContainerHovered(true);
                                fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                            }
                        }
                        else if (!otherHand.hoverValid)
                        {
                            hoverValid = false;
                            collidingContainerWrapper.SetContainerHovered(false);
                        }
                    }
                    else if (!otherHand.hoverValid)
                    {
                        hoverValid = false;
                        collidingContainerWrapper.SetContainerHovered(false);
                    }
                }
                else // Backpack closed
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
                    collidingContainerWrapper = null;
                }
            }
            else if(collidingSwitch != null || collidingTogglableWrapper != null)
            {
                fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
            }
            else if(collidingTradeVolume != null)
            {
                fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                collidingTradeVolume.SetContainerHovered(true);
            }
        }

        private void OnTriggerStay()
        {
            // Check if dropped the held item
            if (collidingContainerWrapper != null)
            {
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf)
                {
                    // Set material in case the hand dropped what it had
                    if (fvrHand.CurrentInteractable == null)
                    {
                        hoverValid = false;
                        collidingContainerWrapper.SetContainerHovered(false);
                    }
                }
            }
            else if(collidingTradeVolume != null)
            {
                // Set material in case the hand dropped what it had
                if (fvrHand.CurrentInteractable == null)
                {
                    hoverValid = false;
                    collidingTradeVolume.SetContainerHovered(false);
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if(collidingContainerWrapper != null && colliders.Remove(collider))
            {
                if (!otherHand.hoverValid)
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
                }

                collidingContainerWrapper = null;
            }
            else if(collidingSwitch != null && colliders.Remove(collider))
            {
                collidingSwitch = null;
            }
            else if(collidingTradeVolume != null && colliders.Remove(collider))
            {
                collidingTradeVolume.SetContainerHovered(false);
                collidingTradeVolume = null;
            }
            else if (collidingTogglableWrapper != null && colliders.Remove(collider))
            {
                collidingTogglableWrapper = null;
            }
        }
    }
}
