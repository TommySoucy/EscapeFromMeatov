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

        private AudioSource buttonClickAudio;

        public void Init()
        {
            Transform summaryElements = transform.GetChild(0).GetChild(0).GetChild(0);
            summaryIcon = summaryElements.GetChild(0).GetComponent<Image>();
            summaryAmountStackText = summaryElements.GetChild(0).GetChild(0).GetComponent<Text>();
            summaryNameText = summaryElements.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            summaryNeededForTotalText = summaryElements.GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>();
            summaryWishlist = summaryElements.GetChild(1).GetChild(2).gameObject;
            summaryInsuredIcon = summaryElements.GetChild(0).GetChild(1).gameObject;
            summaryInsuredBorder = summaryElements.GetChild(0).GetChild(2).gameObject;
            summaryNeededIcons[0] = summaryElements.GetChild(0).GetChild(3).GetChild(0).gameObject;
            summaryNeededIcons[1] = summaryElements.GetChild(0).GetChild(3).GetChild(1).gameObject;
            summaryNeededIcons[2] = summaryElements.GetChild(0).GetChild(3).GetChild(2).gameObject;
            summaryNeededIcons[3] = summaryElements.GetChild(0).GetChild(3).GetChild(3).gameObject;

            Transform fullContent = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            fullIcon = fullContent.GetChild(1).GetComponent<Image>();
            fullAmountStackText = fullContent.GetChild(1).GetChild(0).GetComponent<Text>();
            fullNameText = fullContent.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            fullNeededForNone = fullContent.GetChild(3).gameObject;
            fullWishlist = fullContent.GetChild(4).gameObject;
            fullNeededForTotal = fullContent.GetChild(5).gameObject;
            fullNeededForTotalText = fullContent.GetChild(5).GetChild(0).GetComponent<Text>();
            fullInsuredIcon = fullContent.GetChild(1).GetChild(1).gameObject;
            fullInsuredBorder = fullContent.GetChild(1).GetChild(2).gameObject;
            summaryNeededIcons[0] = fullContent.GetChild(1).GetChild(3).GetChild(0).gameObject;
            summaryNeededIcons[1] = fullContent.GetChild(1).GetChild(3).GetChild(1).gameObject;
            summaryNeededIcons[2] = fullContent.GetChild(1).GetChild(3).GetChild(2).gameObject;
            summaryNeededIcons[3] = fullContent.GetChild(1).GetChild(3).GetChild(3).gameObject;
            fullDescriptionText = fullContent.GetChild(7).GetChild(0).GetComponent<Text>();
            compatibleMagsTitle = fullContent.GetChild(8).gameObject;
            compatibleMags = fullContent.GetChild(9).gameObject;
            compatibleMagsText = fullContent.GetChild(9).GetChild(0).GetComponent<Text>();
            compatibleAmmoTitle = fullContent.GetChild(10).gameObject;
            compatibleAmmo = fullContent.GetChild(11).gameObject;
            compatibleAmmoText = fullContent.GetChild(11).GetChild(0).GetComponent<Text>();

            // Set exit button
            EFM_PointableButton exitButton = transform.GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_PointableButton>();
            exitButton.SetButton();
            exitButton.MaxPointingRange = 30;
            exitButton.hoverSound = transform.GetChild(0).GetChild(1).GetChild(3).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(0).GetChild(1).GetChild(4).GetComponent<AudioSource>();
            exitButton.Button.onClick.AddListener(() => { OnExitClick(); });

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
            if (descriptionPack.maxStack != -1) 
            {
                summaryAmountStackText.gameObject.SetActive(true);
                summaryAmountStackText.text = descriptionPack.stack.ToString() + "/" + descriptionPack.maxStack;
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

            // Full
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
            fullNeededForNone.SetActive(descriptionPack.amountRequired == 0);
            fullWishlist.SetActive(descriptionPack.onWishlist);
            fullNeededForTotal.SetActive(descriptionPack.amountRequired > 0);
            if (descriptionPack.amountRequired > 0)
            {
                fullNeededForTotalText.text = "Total: (" + descriptionPack.amount + "/" + descriptionPack.amountRequired + ")";
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
            bool addCompatibleMags = descriptionPack.compatibleAmmoContainers != null && descriptionPack.compatibleAmmoContainers.Count > 0;
            compatibleMagsTitle.SetActive(addCompatibleMags);
            compatibleMags.SetActive(addCompatibleMags);
            if (addCompatibleMags)
            {
                string compatibleAmmoContainersString = "";
                foreach (KeyValuePair<string, int> ammoContainer in descriptionPack.compatibleAmmoContainers)
                {
                    compatibleAmmoContainersString += "- "+ammoContainer.Key +" ("+ammoContainer.Value+")\n";
                }
                compatibleMagsText.text = compatibleAmmoContainersString;
            }
            bool addCompatibleAmmo = descriptionPack.compatibleAmmo != null && descriptionPack.compatibleAmmo.Count > 0;
            compatibleAmmoTitle.SetActive(addCompatibleAmmo);
            compatibleAmmo.SetActive(addCompatibleAmmo);
            if (addCompatibleAmmo)
            {
                string compatibleAmmoString = "";
                foreach (KeyValuePair<string, int> ammo in descriptionPack.compatibleAmmo)
                {
                    compatibleAmmoString += "- "+ammo.Key +" ("+ammo.Value+")\n";
                }
                compatibleAmmoText.text = compatibleAmmoString;
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
