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
        public Sprite[] modTypes; // Auxilary, Barrel, Bipod, Charge, Light/Laser, GasBlock, Handguard, IronSight, Launcher, Mag, Rail, Muzzle, Grip, RailCover, Receiver Sight, Stock Tactical
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
        public Sprite[] currencyIcons; // RUB, USD, EUR
        public Image infoValueIcon;
        public Text infoValueText;
        public GameObject toolIcon;
        public GameObject toolBorder;

        public void SetItem(MeatovItem item)
        {
            this.item = item;

            infoFoundInRaidCheckmark.SetActive(item.foundInRaid);
            infoInsuredIcon.SetActive(item.insured);
            insuredBorder.SetActive(item.insured);
            infoCountText.text = item.maxAmount > 0 ? item.amount.ToString() : item.stack.ToString();
            infoValueIcon.gameObject.SetActive(false);
            infoValueText.gameObject.SetActive(false);
            toolIcon.SetActive(false);
            toolBorder.SetActive(false);

            SetItemData(item.itemData);
        }

        public void SetItemData(MeatovItemData itemData,
                                bool hasInsuredOverride = false, bool insuredOverride = false,
                                bool hasCountOverride = false, string countOverride = null,
                                bool hasValueOverride = false, int currencyIconIndexOverride = 0, int valueOverride = 0, 
                                bool hasToolOveride = false, bool isToolOverride = false)
        {
            if(this.itemData != null)
            {
                this.itemData.OnNeededForChanged -= OnNeededForChanged;
            }

            this.itemData = itemData;

            // Set static data
            Mod.SetIcon(itemData.H3ID, itemIcon);
            infoSpecial.SetActive(itemData.itemType == MeatovItem.ItemType.Pouch);
            if(itemData.GetCheckmark(out Color color))
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
                infoNeededForCheckmark.color = color;
            }
            else
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
            }
            itemData.OnNeededForChanged += OnNeededForChanged;

            // Set overrides
            if (hasInsuredOverride)
            {
                infoInsuredIcon.SetActive(insuredOverride);
                insuredBorder.SetActive(insuredOverride);
            }
            if (hasCountOverride && countOverride != null)
            {
                infoCountText.text = countOverride;
            }
            if (hasValueOverride)
            {
                infoValueIcon.sprite = currencyIcons[currencyIconIndexOverride];
                infoValueText.text = valueOverride.ToString();
            }
            if (hasToolOveride)
            {
                toolIcon.SetActive(isToolOverride);
                toolBorder.SetActive(isToolOverride);
            }
        }

        public DescriptionPack GetDescriptionPack()
        {
            // TODO
            return new DescriptionPack();
        }

        public void SetDescriptionManager(DescriptionManager descriptionManager)
        {
            // TODO
        }

        public void OnNeededForChanged(int index)
        {
            if (itemData.GetCheckmark(out Color color))
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
                infoNeededForCheckmark.color = color;
            }
            else
            {
                infoNeededForCheckmark.gameObject.SetActive(true);
            }
        }

        public void OnDestroy()
        {
            if (itemData != null)
            {
                itemData.OnNeededForChanged -= OnNeededForChanged;
            }
        }
    }
}
