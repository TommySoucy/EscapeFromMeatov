using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class EFM_DescriptionManager : MonoBehaviour
    {
        public DescriptionPack descriptionPack;
        public bool isFull;

        public Image summaryIcon;
        public Text summaryAmountStackText;
        public Text summaryNameText;
        public Text summaryNeededForTotalText;
        public GameObject summaryWishlist;
        public GameObject summaryInsuredIcon;
        public GameObject summaryInsuredBorder;
        public GameObject[] summaryNeededIcons = new GameObject[4]; // Quest, AreaFulfilled, AreaRequired, Wish 
        public Text summaryWeightText;
        public Text summaryVolumeText;

        public Image fullIcon;
        public Text fullAmountStackText;
        public Text fullNameText;
        public GameObject fullNeededForNone;
        public GameObject fullWishlist;
        public GameObject fullNeededForTotal;
        public Text fullNeededForTotalText;
        public GameObject fullInsuredIcon;
        public GameObject fullInsuredBorder;
        public GameObject[] fullNeededIcons = new GameObject[4]; // Quest, AreaFulfilled, AreaRequired, Wish 
        public Text fullDescriptionText;
        public GameObject compatibleMagsTitle;
        public GameObject compatibleMags;
        public Text compatibleMagsText;
        public GameObject compatibleAmmoTitle;
        public GameObject compatibleAmmo;
        public Text compatibleAmmoText;
        public Text propertiesText;
        public EFM_HoverScroll upHoverScroll;
        public EFM_HoverScroll downHoverScroll;
        public GameObject ammoContainsTitle;

        private AudioSource buttonClickAudio;
        private List<GameObject> areaNeededForTexts;
        private List<GameObject> ammoContainsTexts;

        public void Init()
        {
            // Transform the description properly
            transform.localScale = Vector3.one * 0.0003f;
            transform.localPosition = Vector3.up * 0.1f;
            transform.localRotation = Quaternion.Euler(25, 0, 0);

            Transform summaryElements = transform.GetChild(0).GetChild(0).GetChild(0);
            summaryIcon = summaryElements.GetChild(0).GetComponent<Image>();
            summaryAmountStackText = summaryElements.GetChild(0).GetChild(0).GetComponent<Text>();
            summaryNameText = summaryElements.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            summaryNeededForTotalText = summaryElements.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>();
            summaryWishlist = summaryElements.GetChild(1).GetChild(2).gameObject;
            summaryInsuredIcon = summaryElements.GetChild(0).GetChild(1).gameObject;
            summaryInsuredBorder = summaryElements.GetChild(0).GetChild(2).gameObject;
            summaryNeededIcons[0] = summaryElements.GetChild(0).GetChild(3).GetChild(0).gameObject;
            summaryNeededIcons[1] = summaryElements.GetChild(0).GetChild(3).GetChild(1).gameObject;
            summaryNeededIcons[2] = summaryElements.GetChild(0).GetChild(3).GetChild(2).gameObject;
            summaryNeededIcons[3] = summaryElements.GetChild(0).GetChild(3).GetChild(3).gameObject;
            summaryWeightText = summaryElements.GetChild(1).GetChild(3).GetChild(0).GetComponent<Text>();
            summaryVolumeText = summaryElements.GetChild(1).GetChild(4).GetChild(0).GetComponent<Text>();

            Transform fullContent = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            fullIcon = fullContent.GetChild(1).GetComponent<Image>();
            fullAmountStackText = fullContent.GetChild(1).GetChild(0).GetComponent<Text>();
            fullNameText = fullContent.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            fullNeededForNone = fullContent.GetChild(5).gameObject;
            fullWishlist = fullContent.GetChild(6).gameObject;
            fullNeededForTotal = fullContent.GetChild(7).gameObject;
            fullNeededForTotalText = fullContent.GetChild(7).GetChild(0).GetComponent<Text>();
            fullInsuredIcon = fullContent.GetChild(1).GetChild(1).gameObject;
            fullInsuredBorder = fullContent.GetChild(1).GetChild(2).gameObject;
            summaryNeededIcons[0] = fullContent.GetChild(1).GetChild(3).GetChild(0).gameObject;
            summaryNeededIcons[1] = fullContent.GetChild(1).GetChild(3).GetChild(1).gameObject;
            summaryNeededIcons[2] = fullContent.GetChild(1).GetChild(3).GetChild(2).gameObject;
            summaryNeededIcons[3] = fullContent.GetChild(1).GetChild(3).GetChild(3).gameObject;
            fullDescriptionText = fullContent.GetChild(9).GetChild(0).GetComponent<Text>();
            compatibleMagsTitle = fullContent.GetChild(10).gameObject;
            compatibleMags = fullContent.GetChild(11).gameObject;
            compatibleMagsText = fullContent.GetChild(11).GetChild(0).GetComponent<Text>();
            compatibleAmmoTitle = fullContent.GetChild(12).gameObject;
            compatibleAmmo = fullContent.GetChild(13).gameObject;
            compatibleAmmoText = fullContent.GetChild(13).GetChild(0).GetComponent<Text>();
            propertiesText = fullContent.GetChild(2).GetChild(0).GetComponent<Text>();
            ammoContainsTitle = fullContent.GetChild(3).gameObject;

            // Set exit button
            EFM_PointableButton exitButton = transform.GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_PointableButton>();
            exitButton.SetButton();
            exitButton.MaxPointingRange = 30;
            exitButton.hoverSound = transform.GetChild(0).GetChild(1).GetChild(3).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(0).GetChild(1).GetChild(4).GetComponent<AudioSource>();
            exitButton.Button.onClick.AddListener(() => { OnExitClick(); });

            // Set hover scrolls
            upHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_HoverScroll>();
            downHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_HoverScroll>();
            upHoverScroll.MaxPointingRange = 30;
            upHoverScroll.hoverSound = exitButton.hoverSound;
            upHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            upHoverScroll.other = downHoverScroll;
            upHoverScroll.up = true;
            upHoverScroll.rate = 0.5f;
            downHoverScroll.MaxPointingRange = 30;
            downHoverScroll.hoverSound = exitButton.hoverSound;
            downHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            downHoverScroll.other = upHoverScroll;
            downHoverScroll.up = false;
            downHoverScroll.rate = 0.5f;

            // Inactive by default
            gameObject.SetActive(false);

            // Add to active descriptions list
            Mod.activeDescriptions.Add(this);
        }

        public void SetDescriptionPack(DescriptionPack descriptionPack = null)
        {
            if(descriptionPack == null)
            {
                if(this.descriptionPack == null)
                {
                    return;
                }
                else
                {
                    if (this.descriptionPack.isCustom)
                    {
                        if (this.descriptionPack.customItem != null)
                        {
                            this.descriptionPack = this.descriptionPack.customItem.GetDescriptionPack();
                        }
                    }
                    else
                    {
                        if (this.descriptionPack.vanillaItem != null)
                        {
                            this.descriptionPack = this.descriptionPack.vanillaItem.GetDescriptionPack();
                        }
                    }
                }
            }
            else
            {
                this.descriptionPack = descriptionPack;
                if (this.descriptionPack.isCustom)
                {
                    if (this.descriptionPack.customItem != null)
                    {
                        this.descriptionPack.customItem.descriptionManager = this;
                    }
                }
                else
                {
                    if (this.descriptionPack.vanillaItem != null)
                    {
                        this.descriptionPack.vanillaItem.descriptionManager = this;
                    }
                }
            }

            // Summary
            summaryIcon.sprite = descriptionPack.icon;
            if (descriptionPack.isCustom)
            {
                if (descriptionPack.customItem.itemType == Mod.ItemType.Money)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = descriptionPack.stack.ToString();
                }
                else if (descriptionPack.customItem.itemType == Mod.ItemType.Consumable)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = descriptionPack.stack.ToString() + "/" + descriptionPack.maxStack;
                }
                else if (descriptionPack.customItem.itemType == Mod.ItemType.Backpack || descriptionPack.customItem.itemType == Mod.ItemType.Container || descriptionPack.customItem.itemType == Mod.ItemType.Pouch)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = descriptionPack.containingVolume.ToString() + "/" + descriptionPack.maxVolume;
                }
                else if (descriptionPack.customItem.itemType == Mod.ItemType.AmmoBox)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = descriptionPack.stack.ToString() + "/" + descriptionPack.maxStack;
                }
                else
                {
                    summaryAmountStackText.gameObject.SetActive(false);
                }
            }
            else
            {
                summaryAmountStackText.gameObject.SetActive(false);
            }
            summaryNameText.text = descriptionPack.name;
            if(descriptionPack.amountRequired > 0)
            {
                summaryNeededForTotalText.gameObject.SetActive(true);
                summaryNeededForTotalText.text = "Total: (" + descriptionPack.amount + "/" + descriptionPack.amountRequired + ")";
            }
            else
            {
                summaryNeededForTotalText.gameObject.SetActive(false);
            }
            summaryWishlist.SetActive(descriptionPack.onWishlist);
            summaryInsuredIcon.SetActive(descriptionPack.insured);
            summaryInsuredBorder.SetActive(descriptionPack.insured);
            summaryNeededIcons[0].SetActive(descriptionPack.amountRequiredQuest > 0);
            if (descriptionPack.amountRequired > 0)
            {
                if (descriptionPack.amount >= descriptionPack.amountRequired)
                {
                    summaryNeededIcons[1].SetActive(true);
                }
                else
                {
                    summaryNeededIcons[2].SetActive(true);
                }
            }
            else
            {
                summaryNeededIcons[1].SetActive(false);
                summaryNeededIcons[2].SetActive(false);
            }
            summaryNeededIcons[3].SetActive(descriptionPack.onWishlist);
            summaryWeightText.text = descriptionPack.weight.ToString();
            summaryVolumeText.text = descriptionPack.volume.ToString();

            // Full
            float descriptionHeight = 615; // Top and bottom padding (25+25) + Icon (300) + Icon spacing (20) + Needed for title (55) + Spacing (20) + Desc. title (55) + Spacing (20) + Name spacing (20) + Properties (55) + Spacing (20)
            fullIcon.sprite = descriptionPack.icon;
            if (descriptionPack.maxStack != -1)
            {
                fullAmountStackText.gameObject.SetActive(true);
                fullAmountStackText.text = descriptionPack.stack.ToString() + "/" + descriptionPack.maxStack;
            }
            else
            {
                fullAmountStackText.gameObject.SetActive(false);
            }
            fullNameText.text = descriptionPack.name;
            descriptionHeight += fullNameText.rectTransform.sizeDelta.y;
            if(descriptionPack.amountRequired == 0)
            {
                descriptionHeight += 47;
                fullNeededForNone.SetActive(true);
            }
            else
            {
                fullNeededForNone.SetActive(false);
            }
            if(descriptionPack.onWishlist)
            {
                descriptionHeight += 47;
                fullWishlist.SetActive(true);
            }
            else
            {
                fullWishlist.SetActive(false);
            }
            if (descriptionPack.amountRequired > 0)
            {
                descriptionHeight += 47;
                fullNeededForTotal.SetActive(true);

                fullNeededForTotalText.text = "Total: (" + descriptionPack.amount + "/" + descriptionPack.amountRequired + ")";
                if(descriptionPack.amount >= descriptionPack.amountRequired)
                {
                    fullNeededForTotalText.color = Color.green;
                }
                else
                {
                    fullNeededForTotalText.color = Color.blue;
                }
            }
            else
            {
                fullNeededForTotal.SetActive(false);
            }
            if (areaNeededForTexts == null)
            {
                areaNeededForTexts = new List<GameObject>();
            }
            else
            {
                for(int i= areaNeededForTexts.Count-1; i>=0; --i)
                {
                    Destroy(areaNeededForTexts[i]);
                }
                areaNeededForTexts.Clear();
            }
            for (int i = 0; i < 22; ++i)
            {
                if(descriptionPack.amountRequiredPerArea[i] > 0)
                {
                    // For each area that requires this item we want to add a NeededForText instance and set its text and color correctly
                    // Also keep a reference to these texts so we can remove them easily when updating the UI
                    GameObject neededForInstance = Instantiate(Mod.neededForPrefab, transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0));
                    neededForInstance.transform.SetSiblingIndex(7);
                    Text neededForInstanceText = neededForInstance.transform.GetChild(0).GetComponent<Text>();
                    neededForInstanceText.text = "- " + Mod.localDB["interface"]["hideout_area_" + i + "_name"].ToString() + "("+ descriptionPack.amount+ "/"+ descriptionPack.amountRequiredPerArea[i] + ")";
                    if (descriptionPack.amount >= descriptionPack.amountRequiredPerArea[i])
                    {
                        neededForInstanceText.color = Color.green;
                    }
                    else
                    {
                        neededForInstanceText.color = Color.blue;
                    }
                    areaNeededForTexts.Add(neededForInstance);
                    descriptionHeight += 47;
                }
            }
            fullInsuredIcon.SetActive(descriptionPack.insured);
            fullInsuredBorder.SetActive(descriptionPack.insured);
            fullNeededIcons[0].SetActive(descriptionPack.amountRequiredQuest > 0);
            if (descriptionPack.amountRequired > 0)
            {
                if (descriptionPack.amount >= descriptionPack.amountRequired)
                {
                    fullNeededIcons[1].SetActive(true);
                }
                else
                {
                    fullNeededIcons[2].SetActive(true);
                }
            }
            else
            {
                fullNeededIcons[1].SetActive(false);
                fullNeededIcons[2].SetActive(false);
            }
            fullNeededIcons[3].SetActive(descriptionPack.onWishlist);
            fullDescriptionText.text = descriptionPack.description;
            descriptionHeight += fullDescriptionText.rectTransform.sizeDelta.y;
            bool addCompatibleMags = descriptionPack.compatibleAmmoContainers != null && descriptionPack.compatibleAmmoContainers.Count > 0;
            if (addCompatibleMags)
            {
                compatibleMagsTitle.SetActive(true);
                descriptionHeight += 55;
                compatibleMags.SetActive(true);

                string compatibleAmmoContainersString = "";
                foreach (KeyValuePair<string, int> ammoContainer in descriptionPack.compatibleAmmoContainers)
                {
                    compatibleAmmoContainersString += "- "+ammoContainer.Key +" ("+ammoContainer.Value+")\n";
                }
                compatibleMagsText.text = compatibleAmmoContainersString;
                descriptionHeight += compatibleMagsText.rectTransform.sizeDelta.y;
            }
            else
            {
                compatibleMagsTitle.SetActive(false);
                compatibleMags.SetActive(false);
            }
            bool addCompatibleAmmo = descriptionPack.compatibleAmmo != null && descriptionPack.compatibleAmmo.Count > 0;
            if (addCompatibleAmmo)
            {
                compatibleAmmoTitle.SetActive(true);
                descriptionHeight += 55;
                compatibleAmmo.SetActive(true);

                string compatibleAmmoString = "";
                foreach (KeyValuePair<string, int> ammo in descriptionPack.compatibleAmmo)
                {
                    compatibleAmmoString += "- "+ammo.Key +" ("+ammo.Value+")\n";
                }
                compatibleAmmoText.text = compatibleAmmoString;
                descriptionHeight += compatibleAmmoText.rectTransform.sizeDelta.y;
            }
            else
            {
                compatibleAmmoTitle.SetActive(false);
                compatibleAmmo.SetActive(false);
            }
            propertiesText.text = "Weight: " + descriptionPack.weight + "kg, Volume: " + descriptionPack.volume;

            if (descriptionPack.containedAmmoClasses != null)
            {
                if (ammoContainsTexts == null)
                {
                    ammoContainsTexts = new List<GameObject>();
                }
                else
                {
                    for (int i = ammoContainsTexts.Count - 1; i >= 0; --i)
                    {
                        Destroy(ammoContainsTexts[i]);
                    }
                    ammoContainsTexts.Clear();
                }
                if (descriptionPack.containedAmmoClasses.Count > 0)
                {
                    ammoContainsTitle.SetActive(true);
                    foreach (KeyValuePair<string, int> entry in descriptionPack.containedAmmoClasses)
                    {
                        GameObject containsInstance = Instantiate(Mod.ammoContainsPrefab, transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0));
                        containsInstance.transform.SetSiblingIndex(4);
                        Text neededForInstanceText = containsInstance.transform.GetChild(0).GetComponent<Text>();
                        neededForInstanceText.text = entry.Key + "("+entry.Value+")";
                        ammoContainsTexts.Add(containsInstance);
                        descriptionHeight += 47;
                    }
                }
                else
                {
                    ammoContainsTitle.SetActive(false);
                }
            }

            // Set hoverscrolls depending on description height
            if (descriptionHeight > 1000)
            {
                downHoverScroll.gameObject.SetActive(true); // Only down should be activated at first

                // We want to move set amount every second, we know the height = 1 so if want to move half of height per second,
                // we want to move at rate of amount we want/height, this will give us a fraction of scroll bar to move per second
                downHoverScroll.rate = 500 / descriptionHeight;
                upHoverScroll.rate = 500 / descriptionHeight;
            }
        }

        public void OpenFull()
        {
            isFull = true;
            transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            transform.parent = null;
        }

        public void OnExitClick()
        {
            buttonClickAudio.Play();
            Mod.activeDescriptions.Remove(this);
            Destroy(gameObject);
        }
    }
}
