using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class EFM_Hand : MonoBehaviour
    {
        public EFM_Hand otherHand;
        public EFM_CustomItemWrapper collidingContainerWrapper;
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

        private void OnTriggerEnter(Collider collider)
        {
            Mod.instance.LogInfo("Entered trigger: " + collider.name);

            Transform mainContainerTransform = null;
            if (collider.gameObject.name.Equals("MainContainer"))
            {
                mainContainerTransform = collider.transform;
            }
            else if (collider.transform.parent.name.Equals("MainContainer"))
            {
                mainContainerTransform = collider.transform.parent;
            }

            if (mainContainerTransform != null)
            {
                EFM_CustomItemWrapper customItemWrapper = mainContainerTransform.parent.GetComponent<EFM_CustomItemWrapper>();
                if (customItemWrapper != null && 
                    (customItemWrapper.itemType == Mod.ItemType.Backpack ||
                     customItemWrapper.itemType == Mod.ItemType.Container ||
                     customItemWrapper.itemType == Mod.ItemType.Pouch))
                {
                    collidingContainerWrapper = customItemWrapper;
                    colliders.Add(collider);
                }
            }

            // Set container hovered is necessary
            if (collidingContainerWrapper != null)
            {
                // Verify container mode
                if (collidingContainerWrapper.mainContainer.activeSelf)
                {
                    // Set material, if this hand is also holding something that fits in the container, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
                        float volumeToUse = 0;
                        string IDToUse = "";
                        string parentToUse = "";
                        FVRPhysicalObject physicalObject = fvrHand.CurrentInteractable as FVRPhysicalObject;
                        EFM_CustomItemWrapper heldCustomItemWrapper = fvrHand.CurrentInteractable.GetComponent<EFM_CustomItemWrapper>();
                        EFM_VanillaItemDescriptor heldVanillaItemDescriptor = fvrHand.CurrentInteractable.GetComponent<EFM_VanillaItemDescriptor>();
                        if (heldCustomItemWrapper != null)
                        {
                            volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];

                            IDToUse = heldCustomItemWrapper.ID;
                            parentToUse = heldCustomItemWrapper.parent;
                        }
                        else
                        {
                            volumeToUse = Mod.sizeVolumes[(int)physicalObject.Size];

                            if (heldVanillaItemDescriptor != null)
                            {
                                IDToUse = heldVanillaItemDescriptor.H3ID;
                                parentToUse = heldVanillaItemDescriptor.parent;
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
                            // Also check if the container can contain the item through filters
                            if (collidingContainerWrapper.whiteList != null)
                            {
                                // If whitelist includes item and blacklist doesn't
                                if ((collidingContainerWrapper.whiteList.Contains("54009119af1c881c07000029") || // This ID indicates any item
                                    collidingContainerWrapper.whiteList.Contains(heldCustomItemWrapper.parent) || // The ID of the item's category
                                    collidingContainerWrapper.whiteList.Contains(heldCustomItemWrapper.ID)) && // The ID of the item
                                    (!collidingContainerWrapper.blackList.Contains(heldCustomItemWrapper.ID) &&
                                    !collidingContainerWrapper.blackList.Contains(heldCustomItemWrapper.parent))) 
                                {
                                    hoverValid = true;
                                    collidingContainerWrapper.SetContainerHovered(true);
                                }
                            }
                            else
                            {
                                hoverValid = true;
                                collidingContainerWrapper.SetContainerHovered(true);
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
        }

        private void OnTriggerExit(Collider collider)
        {
            if(collidingContainerWrapper != null && colliders.Remove(collider) && colliders.Count > 0)
            {
                if (!otherHand.hoverValid)
                {
                    hoverValid = false;
                    collidingContainerWrapper.SetContainerHovered(false);
                }

                collidingContainerWrapper = null;
            }
        }
    }
}
