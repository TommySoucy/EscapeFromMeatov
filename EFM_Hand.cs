using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class EFM_Hand : MonoBehaviour
    {
        public EFM_Hand otherHand;
        public EFM_CustomItemWrapper collidingBackpackWrapper;
        public bool hoverValid;
        private List<Collider> colliders;
        private FVRViveHand fvrHand;

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
                if (customItemWrapper != null && customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    collidingBackpackWrapper = customItemWrapper;
                    colliders.Add(collider);
                }
            }
        }

        private void OnTriggerStay(Collider collider)
        {
            if(collidingBackpackWrapper != null)
            {
                // Verify backpack mode
                if (collidingBackpackWrapper.mainContainer.activeSelf)
                {
                    // Set material, if this hand is also holding something that fits in the pack, set the material to hovered
                    if (fvrHand.CurrentInteractable != null && fvrHand.CurrentInteractable is FVRPhysicalObject)
                    {
                        float volumeToUse = 0;
                        FVRPhysicalObject physicalObject = fvrHand.CurrentInteractable as FVRPhysicalObject;
                        EFM_CustomItemWrapper heldCustomItemWrapper = fvrHand.CurrentInteractable.GetComponent<EFM_CustomItemWrapper>();
                        if (heldCustomItemWrapper != null)
                        {
                            volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];
                        }
                        else
                        {
                            volumeToUse = Mod.sizeVolumes[(int)physicalObject.Size];
                        }

                        // Check if volume fits in bag
                        if (collidingBackpackWrapper.containingVolume + volumeToUse <= collidingBackpackWrapper.maxVolume)
                        {
                            hoverValid = true;
                            collidingBackpackWrapper.SetBackpackHovered(true);
                        }
                        else if (!otherHand.hoverValid)
                        {
                            hoverValid = false;
                            collidingBackpackWrapper.SetBackpackHovered(false);
                        }
                    }
                    else if (!otherHand.hoverValid)
                    {
                        hoverValid = false;
                        collidingBackpackWrapper.SetBackpackHovered(false);
                    }
                }
                else // Backpack closed
                {
                    hoverValid = false;
                    collidingBackpackWrapper.SetBackpackHovered(false);
                    collidingBackpackWrapper = null;
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if(collidingBackpackWrapper != null && colliders.Remove(collider) && colliders.Count > 0)
            {
                if (!otherHand.hoverValid)
                {
                    hoverValid = false;
                    collidingBackpackWrapper.SetBackpackHovered(false);
                }

                collidingBackpackWrapper = null;
            }
        }
    }
}
