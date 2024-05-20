using FistVR;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionListEntryUI : MonoBehaviour
    {
        public ItemDescriptionUI owner;

        public GameObject fulfilledIcon;
        public GameObject unfulfilledIcon;
        public Text amount;
        public Text entryName;
        public Text entryInfo;
        public Button button;

        public void OnFillClicked()
        {

        }

        public void SetTask(ItemDescriptionUI owner, Task task, long currentCount, int neededCount)
        {
            // Note that an entry like this one does not subscribe to events to update its amount in realtime
            // We consider item descriptions to be shortlived and as such, don't need to be updated
            // Up to date numbers will simply be displayed when we make a new description of the item
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = task.name;
            entryName.color = Mod.neededForColors[0];
            entryInfo.text = task.trader.name;
        }

        public void SetAreaLevel(ItemDescriptionUI owner, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
                entryName.color = Mod.neededForAreaFulfilledColor;
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
                entryName.color = Mod.neededForColors[1];
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = Area.IndexToName(areaIndex) + " lvl " + level;
            entryInfo.gameObject.SetActive(false);
        }

        public void SetBarter(ItemDescriptionUI owner, Barter barter, int traderIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = barter.itemData.name;
            entryName.color = Mod.neededForColors[3];
            entryInfo.text = Mod.traders[traderIndex].name + " lvl " + level;
        }

        public void SetProduction(ItemDescriptionUI owner, Production production, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            if(Mod.GetItemData(production.endProduct, out MeatovItemData itemData))
            {
                entryName.text = itemData.name;
            }
            entryName.color = Mod.neededForColors[4];
            entryInfo.text = Area.IndexToName(areaIndex) + " lvl " + level;
        }

        public void SetAmmoContainer(ItemDescriptionUI owner, MeatovItem physicalItem, MeatovItemData containerData, int hideoutCount, int playerCount, bool isMag)
        {
            this.owner = owner;
            fulfilledIcon.SetActive(false);
            unfulfilledIcon.SetActive(false);
            entryName.text = containerData.name;
            if(physicalItem == null)
            {
                button.gameObject.SetActive(false);
            }
            else
            {
                button.gameObject.SetActive(true);
                string containerID = containerData.H3ID;
                bool mag = isMag;
                button.onClick.AddListener(() => OnLoadAmmoContainerClicked(containerID, mag));
            }
            if (HideoutController.instance != null)
            {
                amount.text = "(" + (hideoutCount + playerCount) + ")";
            }
            else
            {
                amount.text = "(" + playerCount + ")";
            }
        }

        public void OnLoadAmmoContainerClicked(string containerID, bool mag)
        {
            if (owner.descriptionPack.item != null)
            {
                if(owner.descriptionPack.item.physObj is FVRFireArm)
                {
                    FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                    if(asFA.Magazine != null || asFA.Clip != null)
                    {
                        return;
                    }
                }
                else if(owner.descriptionPack.item.physObj is AttachableFirearmPhysicalObject)
                {
                    AttachableFirearmPhysicalObject asAFA = owner.descriptionPack.item.physObj as AttachableFirearmPhysicalObject;
                    if (asAFA.FA.Magazine != null || asAFA.FA.Clip != null)
                    {
                        return;
                    }
                }
                else // Not an item we can load ammo container in anyway
                {
                    Mod.LogError("Attempted to description ammo container load " + containerID + " into " + owner.descriptionPack.item.name + " which is not FVRFirearm nor AttachableFirearmPhysicalObject");
                    return;
                }
            }
            else // Should not have had load button, but item could have been destroyed since description creation
            {
                return;
            }

            if(Mod.playerInventoryItems.TryGetValue(containerID, out List<MeatovItem> containerList))
            {
                if (mag)
                {
                    for (int i = 0; i < containerList.Count; ++i)
                    {
                        FVRFireArmMagazine asMag = containerList[i].physObj as FVRFireArmMagazine;
                        if (asMag != null && asMag.FireArm == null && asMag.AttachableFireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadMag(asMag);
                            }
                            else if (owner.descriptionPack.item.physObj is AttachableFirearmPhysicalObject)
                            {
                                AttachableFirearmPhysicalObject asAFA = owner.descriptionPack.item.physObj as AttachableFirearmPhysicalObject;
                                asAFA.FA.LoadMag(asMag);
                            }
                            else // Can't load clip into attachable firearm
                            {
                                return;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < containerList.Count; ++i)
                    {
                        FVRFireArmClip asClip = containerList[i].physObj as FVRFireArmClip;
                        if (asClip != null && asClip.FireArm == null)
                        {
                            if (owner.descriptionPack.item.physObj is FVRFireArm)
                            {
                                FVRFireArm asFA = owner.descriptionPack.item.physObj as FVRFireArm;
                                asFA.LoadClip(asClip);
                            }
                            else // Can't load clip into attachable firearm
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
