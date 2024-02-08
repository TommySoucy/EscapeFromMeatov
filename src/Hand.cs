using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Hand : MonoBehaviour
    {
        public Hand otherHand;
        public MeatovItem collidingContainerWrapper;
        public MeatovItem collidingTogglableWrapper;
        private List<Collider> togglableColliders;
        public TradeVolume collidingTradeVolume;
        private Collider tradeVolumeCollider;
        public Switch collidingSwitch;
        private List<Collider> switchColliders;
        public bool hoverValid;
        public FVRViveHand fvrHand;
        public bool consuming;
        public bool leaving;

        // Held item
        public bool hasScript;
        public bool custom;
        public MeatovItem CIW;
        public VanillaItemDescriptor VID;
        public bool updateGrabbity;

        private void Awake()
        {
            fvrHand = transform.GetComponent<FVRViveHand>();
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
            }
            else if(fvrHand.CurrentInteractable != null && hasScript)
            {
                if (custom)
                {
                    CIW.TakeInput(fvrHand, this);
                }
                else
                {
                    VID.TakeInput(fvrHand, this);
                }
            }

            if (updateGrabbity)
            {
                if (!fvrHand.Grabbity_HoverSphere.gameObject.activeSelf)
                {
                    fvrHand.Grabbity_HoverSphere.gameObject.SetActive(true);
                }
                fvrHand.Grabbity_HoverSphere.position = fvrHand.PoseOverride.position;
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            if (switchColliders.Contains(collider) || togglableColliders.Contains(collider))
            {
                return;
            }

            bool mustBuzz = false;
            if (collider.gameObject.GetComponent<Switch>() != null)
            {
                collidingSwitch = collider.gameObject.GetComponent<Switch>();
                switchColliders.Add(collider);
                mustBuzz = true;
            }
            else if (collider.transform.parent != null && collider.transform.parent.GetComponent<TradeVolume>() != null)
            {
                collidingTradeVolume = collider.transform.parent.GetComponent<TradeVolume>();
                tradeVolumeCollider = collider;
                mustBuzz = true;
            }
            else if (collider.transform.parent != null)
            {
                if (collider.gameObject.name.Equals("Interactive"))
                {
                    MeatovItem lootContainerCIW = collider.transform.parent.GetComponent<MeatovItem>();
                    if (lootContainerCIW != null && lootContainerCIW.itemType == Mod.ItemType.LootContainer)
                    {
                        collidingTogglableWrapper = lootContainerCIW;
                        togglableColliders.Add(collider);
                        mustBuzz = true;
                    }
                }
                else if (collider.transform.parent.name.Equals("Interactives") && collider.transform.parent.parent != null)
                {
                    MeatovItem lootContainerCIW = collider.transform.parent.parent.GetComponent<MeatovItem>();
                    if (lootContainerCIW != null && lootContainerCIW.itemType == Mod.ItemType.LootContainer)
                    {
                        collidingTogglableWrapper = lootContainerCIW;
                        togglableColliders.Add(collider);
                        mustBuzz = true;
                    }
                }
                else if (collider.transform.parent.parent != null)
                {
                    if (collider.transform.parent.parent.name.Equals("Interactives") && collider.transform.parent.parent.parent != null)
                    {
                        MeatovItem itemCIW = collider.transform.parent.parent.parent.GetComponent<MeatovItem>();
                        if (itemCIW != null && (fvrHand.CurrentInteractable == null || !fvrHand.CurrentInteractable.Equals(itemCIW.physObj)) &&
                            (itemCIW.itemType == Mod.ItemType.Container ||
                             itemCIW.itemType == Mod.ItemType.Backpack ||
                             itemCIW.itemType == Mod.ItemType.Pouch))
                        {
                            collidingTogglableWrapper = itemCIW;
                            togglableColliders.Add(collider);
                            mustBuzz = true;
                        }
                    }
                }
            }

            EFM_MainContainer mainContainer = collider.GetComponent<EFM_MainContainer>();
            bool newMainContainer = false;
            if (mainContainer != null && collidingContainerWrapper != mainContainer.parentCIW)
            {
                MeatovItem customItemWrapper = mainContainer.parentCIW;
                if ((fvrHand.CurrentInteractable == null || !fvrHand.CurrentInteractable.Equals(customItemWrapper.physObj)) &&
                    (customItemWrapper.itemType == Mod.ItemType.Backpack ||
                     customItemWrapper.itemType == Mod.ItemType.Container ||
                     customItemWrapper.itemType == Mod.ItemType.Pouch))
                {
                    Mod.LogInfo("\tGot container CIW");
                    // Clear the previous colliding container if we had one
                    if (collidingContainerWrapper != null)
                    {
                        Mod.LogInfo("\t\tAlready had a colliding container, clearing");
                        hoverValid = false;
                        collidingContainerWrapper.SetContainerHovered(false);
                    }

                    newMainContainer = true;
                    collidingContainerWrapper = customItemWrapper;
                    Mod.LogInfo("\tcollidingContainerWrapper now has ID: "+ collidingContainerWrapper.ID);
                }
            }

            // Set container hovered if necessary
            if (newMainContainer && collidingContainerWrapper.canInsertItems)
            {
                Mod.LogInfo("\tCan insert items into it");
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf)
                {
                    Mod.LogInfo("\t\tmain container is active");
                    // Set material, if this hand is also holding something that fits in the container, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
                        Mod.LogInfo("\t\t\tWe are holding something");
                        int volumeToUse = 0;
                        string IDToUse = "";
                        List<string> parentsToUse = null;
                        FVRPhysicalObject physicalObject = fvrHand.CurrentInteractable as FVRPhysicalObject;
                        MeatovItem heldCustomItemWrapper = fvrHand.CurrentInteractable.GetComponent<MeatovItem>();
                        VanillaItemDescriptor heldVanillaItemDescriptor = fvrHand.CurrentInteractable.GetComponent<VanillaItemDescriptor>();
                        if (heldCustomItemWrapper != null)
                        {
                            volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];

                            IDToUse = heldCustomItemWrapper.ID;
                            parentsToUse = heldCustomItemWrapper.parents;
                        }
                        else
                        {
                            volumeToUse = heldVanillaItemDescriptor.volume;

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
                                if (Mod.IDDescribedInList(IDToUse, parentsToUse, collidingContainerWrapper.whiteList, collidingContainerWrapper.blackList))
                                {
                                    Mod.LogInfo("\t\t\t\tIt fits in container, setting valid");
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
                }
            }
            else if(mustBuzz)
            {
                fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
            }
        }

        private void OnTriggerStay()
        {
            // Check if dropped the held item
            if (collidingContainerWrapper != null)
            {
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf && !hoverValid)
                {
                    // Container is active but hover invalid
                    // Set material in case the hand dropped what it had
                    // Set material, if this hand is also holding something that fits in the container, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
                        int volumeToUse = 0;
                        string IDToUse = "";
                        List<string> parentsToUse = null;
                        FVRPhysicalObject physicalObject = fvrHand.CurrentInteractable as FVRPhysicalObject;
                        MeatovItem heldCustomItemWrapper = fvrHand.CurrentInteractable.GetComponent<MeatovItem>();
                        VanillaItemDescriptor heldVanillaItemDescriptor = fvrHand.CurrentInteractable.GetComponent<VanillaItemDescriptor>();
                        if (heldCustomItemWrapper != null)
                        {
                            volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];

                            IDToUse = heldCustomItemWrapper.ID;
                            parentsToUse = heldCustomItemWrapper.parents;
                        }
                        else
                        {
                            volumeToUse = heldVanillaItemDescriptor.volume;

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
                                if (Mod.IDDescribedInList(IDToUse, parentsToUse, collidingContainerWrapper.whiteList, collidingContainerWrapper.blackList))
                                {
                                    Mod.LogInfo("\t\t\t\tIt fits in container, setting valid");
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
                        else if (!otherHand.hoverValid || otherHand.collidingContainerWrapper != collidingContainerWrapper)
                        {
                            hoverValid = false;
                            collidingContainerWrapper.SetContainerHovered(false);
                        }
                    }
                }
                else if(!collidingContainerWrapper.mainContainer.activeSelf && hoverValid && (!otherHand.hoverValid || otherHand.collidingContainerWrapper != collidingContainerWrapper))
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
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
            EFM_MainContainer mainContainer = collider.GetComponent<EFM_MainContainer>();
            if (mainContainer != null && collidingContainerWrapper == mainContainer.parentCIW)
            {
                if (!otherHand.hoverValid || otherHand.collidingContainerWrapper != collidingContainerWrapper)
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
                }

                collidingContainerWrapper = null;
            }
            if (collidingSwitch != null && switchColliders.Remove(collider) && switchColliders.Count == 0)
            {
                collidingSwitch = null;
            }
            if (collidingTradeVolume != null && tradeVolumeCollider.Equals(collider))
            {
                collidingTradeVolume.SetContainerHovered(false);
                collidingTradeVolume = null;
                tradeVolumeCollider = null;
            }
            if (collidingTogglableWrapper != null && togglableColliders.Remove(collider) && togglableColliders.Count == 0)
            {
                collidingTogglableWrapper = null;
            }
        }
    }
}
