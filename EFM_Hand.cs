using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class EFM_Hand : MonoBehaviour
    {
        public EFM_Hand otherHand;
        public EFM_CustomItemWrapper collidingContainerWrapper;
        private List<Collider> containerColliders;
        public EFM_CustomItemWrapper collidingTogglableWrapper;
        private List<Collider> togglableColliders;
        public EFM_TradeVolume collidingTradeVolume;
        private Collider tradeVolumeCollider;
        public EFM_Switch collidingSwitch;
        private List<Collider> switchColliders;
        public bool hoverValid;
        public FVRViveHand fvrHand;
        public bool consuming;
        public GameObject currentHeldItem;

        private void Awake()
        {
            fvrHand = transform.GetComponent<FVRViveHand>();
            containerColliders = new List<Collider>();
            togglableColliders = new List<Collider>();
            switchColliders = new List<Collider>();
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
            else if(fvrHand.CurrentInteractable != null)
            {
                if (fvrHand.CurrentInteractable.GetComponent<EFM_CustomItemWrapper>())
                {
                    fvrHand.CurrentInteractable.GetComponent<EFM_CustomItemWrapper>().TakeInput();
                }
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            Transform mainContainerTransform = null;
            if (collider.gameObject.name.Equals("MainContainer"))
            {
                mainContainerTransform = collider.transform;
            }
            else if (collider.gameObject.GetComponent<EFM_Switch>() != null)
            {
                collidingSwitch = collider.gameObject.GetComponent<EFM_Switch>();
                switchColliders.Add(collider);
            }
            else if (collider.transform.parent != null && collider.transform.parent.GetComponent<EFM_TradeVolume>() != null)
            {
                collidingTradeVolume = collider.transform.parent.GetComponent<EFM_TradeVolume>();
                tradeVolumeCollider = collider;
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
                        togglableColliders.Add(collider);
                    }
                }
                else if (collider.transform.parent.name.Equals("Interactives") && collider.transform.parent.parent != null)
                {
                    EFM_CustomItemWrapper lootContainerCIW = collider.transform.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                    if (lootContainerCIW != null && lootContainerCIW.itemType == Mod.ItemType.LootContainer)
                    {
                        collidingTogglableWrapper = lootContainerCIW;
                        togglableColliders.Add(collider);
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
                            togglableColliders.Add(collider);
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
                    // Clear the previous colliding container if we had one
                    if(collidingContainerWrapper != null)
                    {
                        hoverValid = false;
                        collidingContainerWrapper.SetContainerHovered(false);
                        containerColliders.Clear();
                    }

                    collidingContainerWrapper = customItemWrapper;
                    containerColliders.Add(collider);
                }
            }

            // Set container hovered if necessary
            if (collidingContainerWrapper != null && collidingContainerWrapper.canInsertItems)
            {
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf)
                {
                    // Set material, if this hand is also holding something that fits in the container, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
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
                                return;
                            }
                        }

                        // Check if volume fits in bag
                        if (collidingContainerWrapper.containingVolume + volumeToUse <= collidingContainerWrapper.maxVolume)
                        {
                            // Also check if the container can contain the item through filters
                            if (collidingContainerWrapper.whiteList != null)
                            {
                                // If whitelist includes item and blacklist doesn't
                                if (EFM_CustomItemWrapper.ItemFitsInContainer(IDToUse, parentsToUse, collidingContainerWrapper.whiteList, collidingContainerWrapper.blackList))
                                {
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
            if (collidingContainerWrapper != null && containerColliders.Remove(collider) && containerColliders.Count == 0)
            {
                if (!otherHand.hoverValid)
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
                }

                collidingContainerWrapper = null;
            }
            else if (collidingSwitch != null && switchColliders.Remove(collider) && switchColliders.Count == 0)
            {
                collidingSwitch = null;
            }
            else if (collidingTradeVolume != null && tradeVolumeCollider.Equals(collider))
            {
                collidingTradeVolume.SetContainerHovered(false);
                collidingTradeVolume = null;
                tradeVolumeCollider = null;
            }
            else if (collidingTogglableWrapper != null && togglableColliders.Remove(collider) && togglableColliders.Count == 0)
            {
                collidingTogglableWrapper = null;
            }
        }
    }
}
