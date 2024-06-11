using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemView : MonoBehaviour, IDescribable
    {
        [NonSerialized]
        public MeatovItem item;
        public MeatovItemData itemData;
        public Image itemIcon;
        public GameObject infoMod;
        public Sprite[] modTypes; // Auxilary, Barrel, Bipod, Charge, Light/Laser, GasBlock, Handguard, IronSight, Launcher, Mag, Rail, Muzzle, Grip, RailCover, Receiver, Sight, Stock, Tactical
        [NonSerialized]
        public Image modType;
        public GameObject infoSecure;
        public GameObject infoLocked;
        public GameObject infoSpecial;
        public Text infoShortNameText;

        public Image infoNeededForCheckmark;
        public GameObject infoFoundInRaidCheckmark;
        public GameObject infoInsuredIcon;
        public GameObject insuredBorder;
        public Text infoCountText;
        public GameObject infoValue;
        public Sprite[] currencyIcons; // RUB, USD, EUR, Item
        public Image infoValueIcon;
        public Text infoValueText;
        public GameObject toolIcon;
        public GameObject toolBorder;

        public void SetItem(MeatovItem item, bool displayValue = false, int currencyIndex = 0, int valueOverride = -1)
        {
            if(this.item != null)
            {
                this.item.OnInsuredChanged -= OnInsuredChanged;
                this.item.OnFIRStatusChanged -= OnFIRStatusChanged;
                this.item.OnContainingVolumeChanged -= OnContainingVolumeChanged;
                this.item.OnStackChanged -= OnStackChanged;
                this.item.OnAmountChanged -= OnAmountChanged;
            }

            this.item = item;

            this.item.OnInsuredChanged += OnInsuredChanged;
            this.item.OnFIRStatusChanged += OnFIRStatusChanged;
            this.item.OnContainingVolumeChanged += OnContainingVolumeChanged;
            this.item.OnStackChanged += OnStackChanged;
            this.item.OnAmountChanged += OnAmountChanged;

            SetItemData(item.itemData);

            infoFoundInRaidCheckmark.SetActive(item.foundInRaid);
            infoInsuredIcon.SetActive(item.insured);
            insuredBorder.SetActive(item.insured);
            infoCountText.text = item.maxAmount > 0 ? item.amount.ToString() : item.stack.ToString();
            infoValueIcon.gameObject.SetActive(displayValue);
            infoValueText.gameObject.SetActive(displayValue);
            infoValueText.text = (valueOverride == -1 ? item.itemData.value : valueOverride).ToString();
            if(item.itemType == MeatovItem.ItemType.Consumable)
            {
                infoCountText.gameObject.SetActive(true);
                infoCountText.text = item.amount.ToString()+"/"+item.maxAmount;
            }
            else if(item.maxStack > 1)
            {
                infoCountText.gameObject.SetActive(true);
                infoCountText.text = item.stack.ToString();
            }
            else if(item.maxVolume > 0)
            {
                infoCountText.gameObject.SetActive(true);
                infoCountText.text = item.containingVolume.ToString() + "/" + item.maxVolume;
            }
            else
            {
                infoCountText.gameObject.SetActive(false);
            }
            toolIcon.SetActive(false);
            toolBorder.SetActive(false);
        }

        public void ResetItemView()
        {
            if (item != null)
            {
                item.OnInsuredChanged -= OnInsuredChanged;
                item.OnFIRStatusChanged -= OnFIRStatusChanged;
                item.OnContainingVolumeChanged -= OnContainingVolumeChanged;
                item.OnStackChanged -= OnStackChanged;
                item.OnAmountChanged -= OnAmountChanged;
            }

            item = null;

            infoFoundInRaidCheckmark.SetActive(false);
            infoInsuredIcon.SetActive(false);
            insuredBorder.SetActive(false);
            infoValueIcon.gameObject.SetActive(false);
            infoValueText.gameObject.SetActive(false);
            infoCountText.gameObject.SetActive(false);
            toolIcon.SetActive(false);
            toolBorder.SetActive(false);

            SetItemData(null);
        }

        public void SetItemData(MeatovItemData itemData,
                                bool hasInsuredOverride = false, bool insuredOverride = false,
                                bool hasCountOverride = false, string countOverride = null,
                                bool hasValueOverride = false, int currencyIconIndexOverride = 0, int valueOverride = 0, 
                                bool hasToolOveride = false, bool isToolOverride = false)
        {
            if (this.itemData != null)
            {
                this.itemData.OnNeededForChanged -= OnNeededForChanged;
                this.itemData.OnMinimumUpgradeAmountChanged -= OnMinimumUpgradeAmountChanged;
                this.itemData.OnHideoutItemInventoryChanged -= OnItemInventoryChanged;
                this.itemData.OnPlayerItemInventoryChanged -= OnItemInventoryChanged;
            }
            this.itemData = itemData;

            if (itemData == null)
            {
                itemIcon.sprite = Mod.emptyCellIcon;

                infoSpecial.SetActive(false);
                infoNeededForCheckmark.gameObject.SetActive(false);
                infoFoundInRaidCheckmark.SetActive(false);
                infoInsuredIcon.SetActive(false);
                insuredBorder.SetActive(false);
                infoCountText.text = "";
                infoValueIcon.gameObject.SetActive(false);
                infoValueText.gameObject.SetActive(false);
                toolIcon.SetActive(false);
                toolBorder.SetActive(false);
            }
            else
            {
                // Set static data
                Mod.SetIcon(itemData.H3ID, itemIcon);
                infoSpecial.SetActive(itemData.itemType == MeatovItem.ItemType.Pouch);
                if (itemData.GetCheckmark(out Color color))
                {
                    infoNeededForCheckmark.gameObject.SetActive(true);
                    infoNeededForCheckmark.color = color;
                }
                else
                {
                    infoNeededForCheckmark.gameObject.SetActive(false);
                }

                itemData.OnNeededForChanged += OnNeededForChanged;
                itemData.OnMinimumUpgradeAmountChanged += OnMinimumUpgradeAmountChanged;
                itemData.OnHideoutItemInventoryChanged += OnItemInventoryChanged;
                itemData.OnPlayerItemInventoryChanged += OnItemInventoryChanged;

                // Set overrides
                if (hasInsuredOverride)
                {
                    infoInsuredIcon.SetActive(insuredOverride);
                    insuredBorder.SetActive(insuredOverride);
                }
                else
                {
                    infoInsuredIcon.SetActive(false);
                    insuredBorder.SetActive(false);
                }
                if (hasCountOverride && countOverride != null)
                {
                    infoCountText.gameObject.SetActive(true);
                    infoCountText.text = countOverride;
                }
                else
                {
                    infoCountText.gameObject.SetActive(false);
                }
                if (hasValueOverride)
                {
                    infoValueIcon.gameObject.SetActive(true);
                    infoValueIcon.sprite = currencyIcons[currencyIconIndexOverride];
                    infoValueText.gameObject.SetActive(true);
                    infoValueText.text = valueOverride.ToString();
                }
                else
                {
                    infoValueIcon.gameObject.SetActive(false);
                    infoValueText.gameObject.SetActive(false);
                }
                if (hasToolOveride)
                {
                    toolIcon.SetActive(isToolOverride);
                    toolBorder.SetActive(isToolOverride);
                }
                else
                {
                    toolIcon.SetActive(false);
                    toolBorder.SetActive(false);
                }
            }
        }

        public DescriptionPack GetDescriptionPack()
        {
            DescriptionPack newPack = new DescriptionPack();

            newPack.itemData = itemData;
            newPack.item = item; // Note that item could be null

            return newPack;
        }

        public void OnNeededForChanged(int index)
        {
            OnMinimumUpgradeAmountChanged();
        }

        public void OnMinimumUpgradeAmountChanged()
        {
            if (itemData.GetCheckmark(out Color color))
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
                infoNeededForCheckmark.color = color;
            }
            else
            {
                infoNeededForCheckmark.gameObject.SetActive(false);
            }
        }

        public void OnItemInventoryChanged(int difference)
        {
            if (itemData.GetCheckmark(out Color color))
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
                infoNeededForCheckmark.color = color;
            }
            else
            {
                infoNeededForCheckmark.gameObject.SetActive(false);
            }
        }

        public void OnInsuredChanged()
        {
            infoInsuredIcon.SetActive(item.insured);
            insuredBorder.SetActive(item.insured);
        }

        public void OnFIRStatusChanged()
        {
            infoFoundInRaidCheckmark.SetActive(item.foundInRaid);
        }

        public void OnContainingVolumeChanged()
        {
            infoCountText.gameObject.SetActive(true);
            infoCountText.text = item.containingVolume.ToString() + "/" + item.maxVolume;
        }

        public void OnStackChanged()
        {
            infoCountText.gameObject.SetActive(true);
            infoCountText.text = item.stack.ToString();
        }

        public void OnAmountChanged()
        {
            infoCountText.gameObject.SetActive(true);
            infoCountText.text = item.amount.ToString() + "/" + item.maxAmount;
        }

        public void OnDestroy()
        {
            if (item != null)
            {
                item.OnInsuredChanged -= OnInsuredChanged;
                item.OnFIRStatusChanged -= OnFIRStatusChanged;
                item.OnContainingVolumeChanged -= OnContainingVolumeChanged;
                item.OnStackChanged -= OnStackChanged;
                item.OnAmountChanged -= OnAmountChanged;
            }

            if (itemData != null)
            {
                itemData.OnNeededForChanged -= OnNeededForChanged;
                itemData.OnMinimumUpgradeAmountChanged -= OnMinimumUpgradeAmountChanged;
                itemData.OnHideoutItemInventoryChanged -= OnItemInventoryChanged;
                itemData.OnPlayerItemInventoryChanged -= OnItemInventoryChanged;
            }
        }
    }
}
