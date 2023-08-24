using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class DescriptionManager : MonoBehaviour
    {
        public DescriptionPack descriptionPack;
        public bool isFull;
        private bool destroying;

        public Image summaryIcon;
        public Text summaryAmountStackText;
        public Text summaryNameText;
        public Text summaryNeededForTotalText;
        public GameObject summaryWishlist;
        public GameObject summaryInsuredIcon;
        public GameObject summaryInsuredBorder;
        public GameObject[] summaryNeededIcons = new GameObject[5]; // Quest, AreaFulfilled, AreaRequired, Wish, FoundInRaid
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
        public GameObject[] fullNeededIcons = new GameObject[5]; // Quest, AreaFulfilled, AreaRequired, Wish, FoundInRaid
        public Text fullDescriptionText;
        public GameObject compatibleMagsTitle;
        public GameObject compatibleMags;
        public Text compatibleMagsText;
        public GameObject compatibleAmmoTitle;
        public GameObject compatibleAmmo;
        public Text compatibleAmmoText;
        public Text propertiesText;
        public HoverScroll downHoverScroll;
        public HoverScroll upHoverScroll;
        public GameObject ammoContainsTitle;
        public Image wishlistButtonImage;

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
            summaryNeededIcons[4] = summaryElements.GetChild(0).GetChild(3).GetChild(4).gameObject;
            summaryWeightText = summaryElements.GetChild(1).GetChild(3).GetChild(0).GetComponent<Text>();
            summaryVolumeText = summaryElements.GetChild(1).GetChild(4).GetChild(0).GetComponent<Text>();

            Transform fullContent = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            fullIcon = fullContent.GetChild(1).GetComponent<Image>();
            fullAmountStackText = fullContent.GetChild(1).GetChild(0).GetComponent<Text>();
            fullNameText = fullContent.GetChild(0).GetChild(0).GetComponent<Text>();
            fullNeededForNone = fullContent.GetChild(5).gameObject;
            fullWishlist = fullContent.GetChild(6).gameObject;
            fullNeededForTotal = fullContent.GetChild(7).gameObject;
            fullNeededForTotalText = fullContent.GetChild(7).GetChild(0).GetComponent<Text>();
            fullInsuredIcon = fullContent.GetChild(1).GetChild(1).gameObject;
            fullInsuredBorder = fullContent.GetChild(1).GetChild(2).gameObject;
            fullNeededIcons[0] = fullContent.GetChild(1).GetChild(3).GetChild(0).gameObject;
            fullNeededIcons[1] = fullContent.GetChild(1).GetChild(3).GetChild(1).gameObject;
            fullNeededIcons[2] = fullContent.GetChild(1).GetChild(3).GetChild(2).gameObject;
            fullNeededIcons[3] = fullContent.GetChild(1).GetChild(3).GetChild(3).gameObject;
            fullNeededIcons[4] = fullContent.GetChild(1).GetChild(3).GetChild(4).gameObject;
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
            PointableButton exitButton = transform.GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<PointableButton>();
            exitButton.SetButton();
            exitButton.MaxPointingRange = 30;
            exitButton.hoverSound = transform.GetChild(0).GetChild(1).GetChild(3).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(0).GetChild(1).GetChild(4).GetComponent<AudioSource>();
            exitButton.Button.onClick.AddListener(() => { OnExitClick(); });

            // Set Wishlist button (color when active: FFD800FF)
            PointableButton wishlistButton = transform.GetChild(0).GetChild(1).GetChild(5).gameObject.AddComponent<PointableButton>();
            wishlistButtonImage = wishlistButton.GetComponent<Image>();
            wishlistButton.SetButton();
            wishlistButton.MaxPointingRange = 30;
            wishlistButton.hoverSound = transform.GetChild(0).GetChild(1).GetChild(3).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(0).GetChild(1).GetChild(4).GetComponent<AudioSource>();
            wishlistButton.Button.onClick.AddListener(() => { OnWishlistClick(); });

            // Set background pointable
            FVRPointable backgroundPointable = transform.GetChild(0).GetChild(1).GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 30;

            // Set hover scrolls
            downHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.AddComponent<HoverScroll>();
            upHoverScroll = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.AddComponent<HoverScroll>();
            downHoverScroll.MaxPointingRange = 30;
            downHoverScroll.hoverSound = exitButton.hoverSound;
            downHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            downHoverScroll.other = upHoverScroll;
            downHoverScroll.rate = 0.5f;
            upHoverScroll.MaxPointingRange = 30;
            upHoverScroll.hoverSound = exitButton.hoverSound;
            upHoverScroll.scrollbar = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
            upHoverScroll.other = downHoverScroll;
            upHoverScroll.up = true;
            upHoverScroll.rate = 0.5f;

            // Inactive by default
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if(!destroying && Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 10)
            {
                Destroy(gameObject);
                destroying = true;
            }
        }

        public void SetDescriptionPack(DescriptionPack descriptionPack = null)
        {
            if(descriptionPack == null)
            {
                if (this.descriptionPack == null)
                {
                    return;
                }
                else
                {
                    if (this.descriptionPack.isPhysical)
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
                    else
                    {
                        this.descriptionPack = this.descriptionPack.nonPhysDescribable.GetDescriptionPack();
                    }
                }
            }
            else
            {
                this.descriptionPack = descriptionPack;

                if(this.descriptionPack.itemType == Mod.ItemType.LootContainer)
                {
                    return;
                }

                if (this.descriptionPack.isPhysical)
                {
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
                else
                {
                    this.descriptionPack.nonPhysDescribable.SetDescriptionManager(this);
                }
            }

            // Set icons
            if (this.descriptionPack.isPhysical)
            {
                summaryIcon.sprite = this.descriptionPack.icon; 
                fullIcon.sprite = this.descriptionPack.icon;
            }
            else
            {
                if (Mod.itemIcons.ContainsKey(this.descriptionPack.ID))
                {
                    Sprite currentSprite = Mod.itemIcons[this.descriptionPack.ID];
                    summaryIcon.sprite = currentSprite;
                    fullIcon.sprite = currentSprite;
                }
                else
                {
                    AnvilManager.Run(Mod.SetVanillaIcon(this.descriptionPack.ID, summaryIcon));
                    AnvilManager.Run(Mod.SetVanillaIcon(this.descriptionPack.ID, fullIcon));
                }
            }

            // Summary
            if (!this.descriptionPack.isPhysical)
            {
                // TODO: Implement specific item types with specific details, like dogtags that need to have a level field
                //if (this.descriptionPack.itemType == Mod.ItemType.DogTag)
                //{
                //    summaryAmountStackText.gameObject.SetActive(true);
                //    summaryAmountStackText.text = this.descriptionPack.level.ToString();
                //}
                //else
                {
                    summaryAmountStackText.gameObject.SetActive(false);
                }
            }
            else if (this.descriptionPack.isCustom)
            {
                if (this.descriptionPack.customItem.itemType == Mod.ItemType.Money)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = this.descriptionPack.stack.ToString();
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.Consumable)
                {
                    if (this.descriptionPack.maxStack > 0)
                    {
                        summaryAmountStackText.gameObject.SetActive(true);
                        summaryAmountStackText.text = this.descriptionPack.stack.ToString() + "/" + this.descriptionPack.maxStack;
                    }
                    else
                    {
                        summaryAmountStackText.gameObject.SetActive(false);
                    }
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.Backpack || this.descriptionPack.customItem.itemType == Mod.ItemType.Container || this.descriptionPack.customItem.itemType == Mod.ItemType.Pouch)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = (this.descriptionPack.containingVolume / Mod.volumePrecisionMultiplier).ToString() + "/" + (this.descriptionPack.maxVolume / Mod.volumePrecisionMultiplier);
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.AmmoBox)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = this.descriptionPack.stack.ToString() + "/" + this.descriptionPack.maxStack;
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.BodyArmor || this.descriptionPack.customItem.itemType == Mod.ItemType.ArmoredRig)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = this.descriptionPack.customItem.armor.ToString() + "/" + this.descriptionPack.customItem.maxArmor;
                }
                else
                {
                    summaryAmountStackText.gameObject.SetActive(false);
                }
            }
            else if(this.descriptionPack.maxStack > 0) // Mags and clips
            {
                summaryAmountStackText.gameObject.SetActive(true);
                summaryAmountStackText.text = this.descriptionPack.stack.ToString() + "/" + this.descriptionPack.maxStack;
            }
            else
            {
                summaryAmountStackText.gameObject.SetActive(false);
            }
            summaryNameText.text = this.descriptionPack.name;
            if (this.descriptionPack.amountRequired > 0)
            {
                summaryNeededForTotalText.gameObject.SetActive(true);
                summaryNeededForTotalText.text = "Total: (" + this.descriptionPack.amount + "/" + this.descriptionPack.amountRequired + ")";
            }
            else
            {
                summaryNeededForTotalText.gameObject.SetActive(false);
            }
            if (this.descriptionPack.onWishlist)
            {
                summaryWishlist.SetActive(true);
                wishlistButtonImage.color = new Color(1, 0.84706f, 0);
            }
            else
            {
                summaryWishlist.SetActive(false);
                wishlistButtonImage.color = Color.black;
            }
            summaryInsuredIcon.SetActive(this.descriptionPack.insured);
            summaryInsuredBorder.SetActive(this.descriptionPack.insured);
            summaryNeededIcons[0].SetActive(this.descriptionPack.amountRequiredQuest > 0);
            //if (this.descriptionPack.amountRequired > 0)
            //{
            //    if (this.descriptionPack.amount >= this.descriptionPack.amountRequired)
            //    {
            //        summaryNeededIcons[1].SetActive(true);
            //        summaryNeededIcons[2].SetActive(false);
            //    }
            //    else
            //    {
            //        summaryNeededIcons[1].SetActive(false);
            //        summaryNeededIcons[2].SetActive(true);
            //    }
            //}
            //else
            //{
            //    summaryNeededIcons[1].SetActive(false);
            //    summaryNeededIcons[2].SetActive(false);
            //}
            summaryNeededIcons[3].SetActive(this.descriptionPack.onWishlist);
            summaryWeightText.text = (this.descriptionPack.weight / 1000.0f).ToString()+"kg";
            summaryVolumeText.text = (this.descriptionPack.volume / Mod.volumePrecisionMultiplier).ToString()+"L";

            // Full
            float descriptionHeight = 640; // Top and bottom padding (25+25) + Icon (300) + Icon spacing (20) + Needed for title (55) + Spacing (20) + Desc. title (55) + Spacing (20) + Name spacing (20) + Properties (55) + Spacing (20)
            if (!this.descriptionPack.isPhysical)
            {
                // TODO: Implement specific item types with specific details, like dogtags that need to have a level field
                //if (this.descriptionPack.itemType == Mod.ItemType.DogTag)
                //{
                //    summaryAmountStackText.gameObject.SetActive(true);
                //    summaryAmountStackText.text = this.descriptionPack.level.ToString();
                //}
            }
            else if (this.descriptionPack.isCustom)
            {
                if (this.descriptionPack.customItem.itemType == Mod.ItemType.Money)
                {
                    fullAmountStackText.gameObject.SetActive(true);
                    fullAmountStackText.text = this.descriptionPack.stack.ToString();
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.Consumable)
                {
                    if (this.descriptionPack.maxStack > 0)
                    {
                        fullAmountStackText.gameObject.SetActive(true);
                        fullAmountStackText.text = this.descriptionPack.stack.ToString() + "/" + this.descriptionPack.maxStack;
                    }
                    else
                    {
                        fullAmountStackText.gameObject.SetActive(false);
                    }
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.Backpack || this.descriptionPack.customItem.itemType == Mod.ItemType.Container || this.descriptionPack.customItem.itemType == Mod.ItemType.Pouch)
                {
                    fullAmountStackText.gameObject.SetActive(true);
                    fullAmountStackText.text = this.descriptionPack.containingVolume.ToString() + "/" + this.descriptionPack.maxVolume;
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.AmmoBox)
                {
                    fullAmountStackText.gameObject.SetActive(true);
                    fullAmountStackText.text = this.descriptionPack.stack.ToString() + "/" + this.descriptionPack.maxStack;
                }
                else if (this.descriptionPack.customItem.itemType == Mod.ItemType.BodyArmor || this.descriptionPack.customItem.itemType == Mod.ItemType.ArmoredRig)
                {
                    summaryAmountStackText.gameObject.SetActive(true);
                    summaryAmountStackText.text = this.descriptionPack.customItem.armor.ToString() + "/" + this.descriptionPack.customItem.maxArmor;
                }
                else
                {
                    fullAmountStackText.gameObject.SetActive(false);
                }
            }
            else
            {
                fullAmountStackText.gameObject.SetActive(false);
            }
            fullNameText.text = this.descriptionPack.name;
            descriptionHeight += fullNameText.preferredHeight;
            if (this.descriptionPack.amountRequired == 0)
            {
                descriptionHeight += 47;
                fullNeededForNone.SetActive(true);
            }
            else
            {
                fullNeededForNone.SetActive(false);
            }
            if (this.descriptionPack.onWishlist)
            {
                descriptionHeight += 47;
                fullWishlist.SetActive(true);
                transform.GetChild(0).GetChild(1).GetChild(5).GetComponent<Image>().color = new Color(1, 0.84706f, 0);
            }
            else
            {
                fullWishlist.SetActive(false);
                transform.GetChild(0).GetChild(1).GetChild(5).GetComponent<Image>().color = Color.black;
            }
            if (this.descriptionPack.amountRequired > 0)
            {
                descriptionHeight += 47;
                fullNeededForTotal.SetActive(true);

                fullNeededForTotalText.text = "Total: (" + this.descriptionPack.amount + "/" + this.descriptionPack.amountRequired + ")";
                if (this.descriptionPack.amount >= this.descriptionPack.amountRequired)
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
                for (int i= areaNeededForTexts.Count-1; i>=0; --i)
                {
                    Destroy(areaNeededForTexts[i]);
                }
                areaNeededForTexts.Clear();
            }
            bool shouldHaveBlueCheckMark = false;
            bool shouldHaveGreenCheckMark = false;
            for (int i = 0; i < 22; ++i)
            {
                if (this.descriptionPack.amountRequiredPerArea[i] > 0)
                {
                    // For each area that requires this item we want to add a NeededForText instance and set its text and color correctly
                    // Also keep a reference to these texts so we can remove them easily when updating the UI
                    GameObject neededForInstance = Instantiate(Mod.neededForPrefab, transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0));
                    neededForInstance.transform.SetSiblingIndex(7);
                    Text neededForInstanceText = neededForInstance.transform.GetChild(0).GetComponent<Text>();
                    neededForInstanceText.text = "- " + Mod.localDB["interface"]["hideout_area_" + i + "_name"].ToString() + "("+ this.descriptionPack.amount+ "/"+ this.descriptionPack.amountRequiredPerArea[i] + ")";
                    if (this.descriptionPack.amount >= this.descriptionPack.amountRequiredPerArea[i])
                    {
                        neededForInstanceText.color = Color.green;
                        shouldHaveGreenCheckMark = true;
                    }
                    else
                    {
                        neededForInstanceText.color = Color.blue;
                        shouldHaveBlueCheckMark = true;
                    }
                    areaNeededForTexts.Add(neededForInstance);
                    descriptionHeight += 47;
                }
            }
            fullInsuredIcon.SetActive(this.descriptionPack.insured);
            fullInsuredBorder.SetActive(this.descriptionPack.insured);
            fullNeededIcons[0].SetActive(this.descriptionPack.amountRequiredQuest > 0);
            fullNeededIcons[1].SetActive(shouldHaveGreenCheckMark);
            fullNeededIcons[2].SetActive(shouldHaveBlueCheckMark);
            // Do the summary checkmarks here because dependent on info we only get for full
            summaryNeededIcons[1].SetActive(shouldHaveGreenCheckMark);
            summaryNeededIcons[2].SetActive(shouldHaveBlueCheckMark);
            fullNeededIcons[3].SetActive(this.descriptionPack.onWishlist);
            fullDescriptionText.text = this.descriptionPack.description;
            descriptionHeight += fullDescriptionText.preferredHeight;
            bool addCompatibleMags = this.descriptionPack.compatibleAmmoContainers != null && this.descriptionPack.compatibleAmmoContainers.Count > 0;
            if (addCompatibleMags)
            {
                compatibleMagsTitle.SetActive(true);
                descriptionHeight += 55;
                compatibleMags.SetActive(true);

                string compatibleAmmoContainersString = "";
                foreach (KeyValuePair<string, int> ammoContainer in this.descriptionPack.compatibleAmmoContainers)
                {
                    compatibleAmmoContainersString += "- "+ammoContainer.Key +" ("+ammoContainer.Value+")\n";
                }
                compatibleMagsText.text = compatibleAmmoContainersString;
                descriptionHeight += compatibleMagsText.preferredHeight;
            }
            else
            {
                compatibleMagsTitle.SetActive(false);
                compatibleMags.SetActive(false);
            }
            bool addCompatibleAmmo = this.descriptionPack.compatibleAmmo != null && this.descriptionPack.compatibleAmmo.Count > 0;
            if (addCompatibleAmmo)
            {
                compatibleAmmoTitle.SetActive(true);
                descriptionHeight += 55;
                compatibleAmmo.SetActive(true);

                string compatibleAmmoString = "";
                foreach (KeyValuePair<string, int> ammo in this.descriptionPack.compatibleAmmo)
                {
                    compatibleAmmoString += "- "+ammo.Key +" ("+ammo.Value+")\n";
                }
                compatibleAmmoText.text = compatibleAmmoString;
                descriptionHeight += compatibleAmmoText.preferredHeight;
            }
            else
            {
                compatibleAmmoTitle.SetActive(false);
                compatibleAmmo.SetActive(false);
            }
            propertiesText.text = "Weight: " + (this.descriptionPack.weight / 1000.0f) + "kg, Volume: " + (this.descriptionPack.volume / Mod.volumePrecisionMultiplier)+"L ";

            if (this.descriptionPack.containedAmmoClassesByType != null)
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
                if (this.descriptionPack.containedAmmoClassesByType.Count > 0)
                {
                    ammoContainsTitle.SetActive(true);
                    descriptionHeight += 55;
                    foreach (KeyValuePair<FireArmRoundType, Dictionary<FireArmRoundClass, int>> typeEntry in this.descriptionPack.containedAmmoClassesByType)
                    {
                        foreach (KeyValuePair<FireArmRoundClass, int> entry in typeEntry.Value)
                        {
                            GameObject containsInstance = Instantiate(Mod.ammoContainsPrefab, transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0));
                            containsInstance.transform.SetSiblingIndex(4);
                            Text neededForInstanceText = containsInstance.transform.GetChild(0).GetComponent<Text>();
                            neededForInstanceText.text = AM.GetFullRoundName(typeEntry.Key, entry.Key) + " (" + entry.Value.ToString() + ")";
                            ammoContainsTexts.Add(containsInstance);
                            descriptionHeight += 47;
                        }
                    }
                }
                else
                {
                    ammoContainsTitle.SetActive(false);
                }
            }
            else if(ammoContainsTitle.activeSelf)
            {
                for (int i = ammoContainsTexts.Count - 1; i >= 0; --i)
                {
                    Destroy(ammoContainsTexts[i]);
                }
                ammoContainsTexts.Clear();
                ammoContainsTitle.SetActive(false);
            }

            // Set hoverscrolls depending on description height
            if (descriptionHeight > 1050)
            {
                Mod.LogInfo("Item full desc hoverscrolls activated for height of "+descriptionHeight);
                upHoverScroll.gameObject.SetActive(false); // Only down should be activated at first
                downHoverScroll.gameObject.SetActive(true);

                // We want to move set amount every second, we know the height = 1 so if want to move half of height per second,
                // we want to move at rate of amount we want/height, this will give us a fraction of scroll bar to move per second
                upHoverScroll.rate = 1050 / (descriptionHeight - 1050);
                downHoverScroll.rate = 1050 / (descriptionHeight - 1050);
            }
        }

        public void OpenFull()
        {
            isFull = true;
            transform.GetChild(0).GetChild(0).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
            transform.parent = null;

            if (Mod.activeDescriptionsByItemID.ContainsKey(descriptionPack.ID))
            {
                Mod.activeDescriptionsByItemID[descriptionPack.ID].Add(this);
            }
            else
            {
                Mod.activeDescriptionsByItemID.Add(descriptionPack.ID, new List<DescriptionManager>() { this });
            }
        }

        public void OnExitClick()
        {
            buttonClickAudio.Play();
            Mod.activeDescriptionsByItemID[descriptionPack.ID].Remove(this);
            if(Mod.activeDescriptionsByItemID[descriptionPack.ID].Count == 0)
            {
                Mod.activeDescriptionsByItemID.Remove(descriptionPack.ID);
            }
            Destroy(gameObject);
        }

        public void OnWishlistClick()
        {
            buttonClickAudio.Play();

            string itemID;
            if (descriptionPack.isPhysical)
            {
                if (descriptionPack.isCustom)
                {
                    itemID = descriptionPack.customItem.ID;
                }
                else
                {
                    itemID = descriptionPack.vanillaItem.H3ID;
                }
            }
            else
            {
                itemID = descriptionPack.ID;
            }

            if (descriptionPack.onWishlist)
            {
                Mod.wishList.Remove(itemID);

                wishlistButtonImage.color = new Color(1, 0.84706f, 0);
                fullWishlist.SetActive(true);
                summaryWishlist.SetActive(true);
                fullNeededIcons[3].SetActive(true);
                summaryNeededIcons[3].SetActive(true);

                if (Mod.activeDescriptionsByItemID.ContainsKey(descriptionPack.ID))
                {
                    foreach (DescriptionManager descManager in Mod.activeDescriptionsByItemID[descriptionPack.ID])
                    {
                        descManager.wishlistButtonImage.color = new Color(1, 0.84706f, 0);
                        descManager.fullWishlist.SetActive(true);
                        descManager.fullNeededIcons[3].SetActive(true);
                    }
                }
            }
            else
            {
                Mod.wishList.Add(itemID);

                wishlistButtonImage.color = Color.black;
                fullWishlist.SetActive(false);
                summaryWishlist.SetActive(false);
                fullNeededIcons[3].SetActive(false);
                summaryNeededIcons[3].SetActive(false);

                if (Mod.activeDescriptionsByItemID.ContainsKey(descriptionPack.ID))
                {
                    foreach (DescriptionManager descManager in Mod.activeDescriptionsByItemID[descriptionPack.ID])
                    {
                        descManager.wishlistButtonImage.color = Color.black;
                        descManager.fullWishlist.SetActive(false);
                        descManager.fullNeededIcons[3].SetActive(false);
                    }
                }
            }

            // If currently in hideout, otherwise we are in raid, and we dont need to modify the market because it doesnt exist
            if(Mod.currentLocationIndex == 1)
            {
                if (descriptionPack.onWishlist)
                {
                    Destroy(Mod.currentBaseManager.marketManager.wishListItemViewsByID[itemID]);
                    Mod.currentBaseManager.marketManager.wishListItemViewsByID.Remove(itemID);

                    if (Mod.currentBaseManager.marketManager.ragFairItemBuyViewsByID.ContainsKey(itemID))
                    {
                        List<GameObject> itemViewsList = Mod.currentBaseManager.marketManager.ragFairItemBuyViewsByID[itemID];
                        foreach (GameObject itemView in itemViewsList)
                        {
                            itemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = Color.black;
                        }
                    }
                }
                else
                {
                    Mod.currentBaseManager.marketManager.AddItemToWishlist(itemID);
                }
            }

            descriptionPack.onWishlist = !descriptionPack.onWishlist;
        }
    }
}
