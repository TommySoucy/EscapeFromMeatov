using FistVR;
using UnityEngine;

namespace EFM
{
    public class Hand : MonoBehaviour
    {
        public Hand otherHand;
        public MeatovItem collidingTogglableItem;
        public LootContainer collidingTogglableLC;
        public Collider togglableCollider;
        public ContainmentVolume collidingVolume;
        public Collider volumeCollider;
        public FVRViveHand fvrHand;
        public bool consuming;
        public bool leaving;

        // Held item
        public bool hasScript;
        public MeatovItem heldItem;
        public bool updateInteractionSphere;

        public ItemDescriptionUI description;
        public IDescribable currentDescribable;

        private void Awake()
        {
            fvrHand = transform.GetComponent<FVRViveHand>();
        }

        private void Update()
        {
            if (fvrHand.CurrentInteractable == null && (collidingTogglableItem != null || collidingTogglableLC != null))
            {
                if (fvrHand.IsInStreamlinedMode)
                {
                    if (fvrHand.Input.AXButtonDown)
                    {
                        if(collidingTogglableItem != null)
                        {
                            switch (collidingTogglableItem.itemType)
                            {
                                case MeatovItem.ItemType.ArmoredRig:
                                case MeatovItem.ItemType.Rig:
                                case MeatovItem.ItemType.Backpack:
                                case MeatovItem.ItemType.BodyArmor:
                                case MeatovItem.ItemType.Container:
                                case MeatovItem.ItemType.Pouch:
                                    collidingTogglableItem.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            collidingTogglableLC.ToggleMode();
                        }
                    }
                }
                else
                {
                    Vector2 touchpadAxes = fvrHand.Input.TouchpadAxes;

                    // If touchpad has started being pressed this frame
                    if (fvrHand.Input.TouchpadDown)
                    {
                        Vector2 TouchpadClickInitiation = touchpadAxes;
                        if (touchpadAxes.magnitude > 0.3f)
                        {
                            if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
                            {
                                if (collidingTogglableItem != null)
                                {
                                    switch (collidingTogglableItem.itemType)
                                    {
                                        case MeatovItem.ItemType.ArmoredRig:
                                        case MeatovItem.ItemType.Rig:
                                        case MeatovItem.ItemType.Backpack:
                                        case MeatovItem.ItemType.BodyArmor:
                                        case MeatovItem.ItemType.Container:
                                        case MeatovItem.ItemType.Pouch:
                                            collidingTogglableItem.ToggleMode(false, fvrHand.IsThisTheRightHand);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                else
                                {
                                    collidingTogglableLC.ToggleMode();
                                }
                            }
                        }
                    }
                }
            }
            else if(heldItem != null)
            {
                heldItem.TakeInput(fvrHand, this);
            }

            if (updateInteractionSphere)
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
            ContainmentVolume volume = collider.GetComponent<ContainmentVolume>();
            if (volume != null)
            {
                Mod.LogInfo("Hand entered volume " + volume.name+", heldItem null?: "+(heldItem == null)+ ", volumeCollider null?: " + (volumeCollider == null)+ ", otherHand.collidingVolume: " + (otherHand.collidingVolume == null ? "null": otherHand.collidingVolume.name));
                if (heldItem != null && volumeCollider == null && (otherHand.collidingVolume == null || otherHand.collidingVolume != volume) && volume.Offer(heldItem))
                {
                    collidingVolume = volume;
                    volumeCollider = collider;
                    fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                }
            }
            else
            {
                if(togglableCollider == null && heldItem == null)
                {
                    MeatovItem meatovItem = collider.GetComponent<MeatovItem>();
                    if (meatovItem = null)
                    {
                        OtherInteractable otherInteractable = collider.GetComponent<OtherInteractable>();
                        if (otherInteractable != null)
                        {
                            meatovItem = otherInteractable.ownerItem;
                        }
                    }

                    if (meatovItem != null)
                    {
                        if (meatovItem.itemType == MeatovItem.ItemType.Container
                            || meatovItem.itemType == MeatovItem.ItemType.Backpack
                            || meatovItem.itemType == MeatovItem.ItemType.Pouch)
                        {
                            collidingTogglableItem = meatovItem;
                            togglableCollider = collider;
                            fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                        }
                    }
                    else
                    {
                        LootContainer lc = collider.GetComponent<LootContainer>();
                        if(lc != null && lc.togglable)
                        {
                            collidingTogglableLC = lc;
                            togglableCollider = collider;
                            fvrHand.Buzz(fvrHand.Buzzer.Buzz_OnHoverInteractive);
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if(collider == volumeCollider)
            {
                collidingVolume.Unoffer();
                collidingVolume = null;
                volumeCollider = null;
            }
            if(collider == togglableCollider)
            {
                collidingVolume = null;
                volumeCollider = null;
                collidingTogglableItem = null;
                collidingTogglableLC = null;
            }
        }

        public void SetDescribable(IDescribable describable)
        {
            if (currentDescribable == describable)
            {
                return;
            }

            currentDescribable = describable;

            if (description == null)
            {
                description = Instantiate(Mod.itemDescriptionUIPrefab, transform).GetComponent<ItemDescriptionUI>();
                description.hand = this;
            }

            description.SetDescriptionPack(describable.GetDescriptionPack());
        }
    }
}
