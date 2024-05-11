using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class AreaUI : MonoBehaviour
    {
        public Area area;
        [NonSerialized]
        public bool inSummary = true;
        [NonSerialized]
        public bool onNextPage;
        public GameObject summary;
        public Image summaryIcon;
        public Text summaryName;
        public GameObject full;
        public Image fullIcon;
        public Text fullName;
        public AudioSource buttonClickSound;
        public Sprite[] statusSprites; // Locked, Unlocked, Constructing, Producing, ReadyToUpgrade, Upgrading, OutofFuel
        public Sprite[] borderSprites; // White, Green, Red
        public Image summaryIconBorder;
        public GameObject summaryIconEliteBackground;
        public GameObject summaryIconProductionBackground;
        public GameObject summaryIconProgressBackground;
        public Text summaryIconCurentLevel;
        public Text summaryIconNextLevel;
        public GameObject summaryIconLocked;
        public GameObject summaryIconUnlocked;
        public GameObject summaryIconOutOfFuel;
        public GameObject summaryIconReadyToUpgrade;
        public GameObject summaryIconConstructing;
        public GameObject summaryIconUpgrading;
        public GameObject summaryIconProducingPanel;
        public GameObject summaryIconProducingPanelImage;
        public Text summaryIconProducingPanelText;
        public Text summaryStatusText;
        public Image fullIconBorder;
        public GameObject fullIconEliteBackground;
        public GameObject fullIconProductionBackground;
        public GameObject fullIconProgressBackground;
        public Text fullIconCurentLevel;
        public Text fullIconNextLevel;
        public GameObject fullIconLocked;
        public GameObject fullIconUnlocked;
        public GameObject fullIconOutOfFuel;
        public GameObject fullIconReadyToUpgrade;
        public GameObject fullIconConstructing;
        public GameObject fullIconUpgrading;
        public GameObject fullIconProducingPanel;
        public GameObject fullIconProducingPanelImage;
        public Text fullIconProducingPanelText;
        public Text fullStatusText;
        public Image fullStatusImage;
        public Text fullDescription;
        public GameObject productionPanel;
        public GameObject productionPanelContainer;
        public GameObject productionViewPrefab;
        public GameObject farmingViewPrefab;
        public GameObject scavCaseViewPrefab;
        public GameObject requirementPanel;
        public Text requirementPanelText;
        public GameObject areaRequirementPanel;
        public GameObject areaRequirementPrefab;
        public GameObject itemRequirementPanel;
        public GameObject itemRequirementPrefab;
        public GameObject traderRequirementPanel;
        public GameObject traderRequirementPrefab;
        public GameObject skillRequirementPanel;
        public GameObject skillRequirementPrefab;
        public GameObject bonusPanel;
        public Text bonusTitle;
        public GameObject bonusPrefab;
        public Text fullFutureDescription;
        public GameObject futureRequirementPanel;
        public Text futureRequirementPanelText;
        public GameObject futureAreaRequirementPanel;
        public GameObject futureAreaRequirementPrefab;
        public GameObject futureItemRequirementPanel;
        public GameObject futureItemRequirementPrefab;
        public GameObject futureTraderRequirementPanel;
        public GameObject futureTraderRequirementPrefab;
        public GameObject futureSkillRequirementPanel;
        public GameObject futureSkillRequirementPrefab;
        public GameObject futureBonusPanel;
        public GameObject futureBonusPrefab;
        public GameObject constructButton;
        public GameObject levelButton;
        public Text levelButtonText;
        public GameObject backButton;
        public GameObject upgradeButton;
        public RectTransform currentContent;
        public RectTransform futureContent;
        public GameObject upgradeConfirmDialog;
        public GameObject warningDialog;
        public GameObject blockDialog;
        public AudioSource genericAudioSource;
        public AudioClip[] genericAudioClips; // AreaSelected, UpgradeBegin, UpgradeComplete, ItemInstalled, ItemStarted, ItemComplete

        public void Init()
        {
            // Make sure both contents are inactive
            currentContent.gameObject.SetActive(false);
            futureContent.gameObject.SetActive(false);

            // Main icons and area name
            if(area.controller.areaIcons != null && area.controller.areaIcons.Length > 0)
            {
                summaryIcon.sprite = area.controller.areaIcons[area.index];
                summaryName.text = area.controller.areaNames[area.index];
                fullIcon.sprite = area.controller.areaIcons[area.index];
                fullName.text = area.controller.areaNames[area.index];
            }

            UpdateStatusTexts();
            UpdateStatusIcons();
            UpdateDescriptions();
            UpdateProductions();
            UpdateRequirements();
            UpdateBonuses();
            UpdateBottomButtons();

            // Once content set, reenable current
            // Ensures its HoverScrollProcessor.OnEnable is called
            currentContent.gameObject.SetActive(true);
        }

        public void UpdateStatusTexts()
        {
            if (area.upgrading)
            {
                if (area.currentLevel == area.startLevel)
                {
                    summaryStatusText.text = "Contructing... ("+Mod.FormatTimeString(area.upgradeTimeLeft)+")";
                    fullStatusText.text = "Contructing... ("+Mod.FormatTimeString(area.upgradeTimeLeft)+")";
                    fullStatusImage.sprite = statusSprites[2];
                }
                else
                {
                    summaryStatusText.text = "Upgrading... (" + Mod.FormatTimeString(area.upgradeTimeLeft) + ")";
                    fullStatusText.text = "Upgrading... (" + Mod.FormatTimeString(area.upgradeTimeLeft) + ")";
                    fullStatusImage.sprite = statusSprites[5];
                }
            }
            else
            {
                if (area.AllRequirementsFulfilled())
                {
                    if (area.currentLevel == area.startLevel)
                    {
                        summaryStatusText.text = "Ready to Construct";
                        fullStatusText.text = "Ready to Construct";
                        fullStatusImage.sprite = statusSprites[1];
                    }
                    else
                    {
                        summaryStatusText.text = "Ready to Upgrade";
                        fullStatusText.text = "Ready to Upgrade";
                        fullStatusImage.sprite = statusSprites[4];
                    }
                }
                else
                {
                    if (area.powered)
                    {
                        if(area.currentLevel == area.startLevel)
                        {
                            summaryStatusText.text = "Stand By";
                            fullStatusText.text = "Stand By";
                            fullStatusImage.sprite = null;
                        }
                        else
                        {
                            if (area.activeProductions.Count > 0)
                            {
                                summaryStatusText.text = "Crafting (" + area.activeProductions.Count + ")";
                                fullStatusText.text = "Crafting (" + area.activeProductions.Count + ")";
                                fullStatusImage.sprite = statusSprites[3];
                            }
                            else
                            {
                                summaryStatusText.text = "Stand By";
                                fullStatusText.text = "Stand By";
                                fullStatusImage.sprite = null;
                            }
                        }
                    }
                    else
                    {
                        if (area.currentLevel == area.startLevel)
                        {
                            summaryStatusText.text = "Stand By";
                            fullStatusText.text = "Stand By";
                            fullStatusImage.sprite = null;
                        }
                        else
                        {
                            if (area.requiresPower)
                            {
                                summaryStatusText.text = "Out of Fuel";
                                fullStatusText.text = "Out of Fuel";
                                fullStatusImage.sprite = statusSprites[6];
                            }
                            else
                            {
                                summaryStatusText.text = "Stand By";
                                fullStatusText.text = "Stand By";
                                fullStatusImage.sprite = null;
                            }
                        }
                    }
                }
            }
        }

        public void UpdateStatusIcons()
        {
            // Border
            if(area.currentLevel > area.startLevel)
            {
                summaryIconBorder.sprite = borderSprites[0];
                fullIconBorder.sprite = borderSprites[0];
            }
            else
            {
                if (area.AllRequirementsFulfilled())
                {
                    summaryIconBorder.sprite = borderSprites[1];
                    fullIconBorder.sprite = borderSprites[1];
                }
                else
                {
                    summaryIconBorder.sprite = borderSprites[2];
                    fullIconBorder.sprite = borderSprites[2];
                }
            }

            // Background
            if (area.upgrading)
            {
                summaryIconProductionBackground.SetActive(false);
                summaryIconProgressBackground.SetActive(true);
                fullIconProductionBackground.SetActive(false);
                fullIconProgressBackground.SetActive(true);
            }
            else
            {
                summaryIconProductionBackground.SetActive(area.hasReadyProduction);
                summaryIconProgressBackground.SetActive(false);
                fullIconProductionBackground.SetActive(area.hasReadyProduction);
                fullIconProgressBackground.SetActive(false);
            }

            // Bottom right, current level stuff
            if (area.currentLevel == area.startLevel)
            {
                summaryIconCurentLevel.gameObject.SetActive(false);
                fullIconCurentLevel.gameObject.SetActive(false);
                if (area.AllRequirementsFulfilled())
                {
                    summaryIconUnlocked.SetActive(true);
                    summaryIconLocked.SetActive(false);
                    fullIconUnlocked.SetActive(true);
                    fullIconLocked.SetActive(false);
                }
                else
                {
                    summaryIconUnlocked.SetActive(false);
                    summaryIconLocked.SetActive(true);
                    fullIconUnlocked.SetActive(false);
                    fullIconLocked.SetActive(true);
                }
            }
            else
            {
                summaryIconCurentLevel.gameObject.SetActive(true);
                summaryIconCurentLevel.text = area.currentLevel.ToString("00");
                fullIconCurentLevel.gameObject.SetActive(true);
                fullIconCurentLevel.text = area.currentLevel.ToString("00");
                summaryIconLocked.SetActive(false);
                fullIconLocked.SetActive(false);
                summaryIconUnlocked.SetActive(false);
                fullIconUnlocked.SetActive(false);
            }

            // Top right, status
            summaryIconConstructing.SetActive(false);
            summaryIconReadyToUpgrade.SetActive(false);
            summaryIconUpgrading.SetActive(false);
            summaryIconProducingPanel.SetActive(false);
            fullIconConstructing.SetActive(false);
            fullIconReadyToUpgrade.SetActive(false);
            fullIconUpgrading.SetActive(false);
            fullIconProducingPanel.SetActive(false);
            if (area.upgrading)
            {
                if (area.currentLevel > area.startLevel)
                {
                    summaryIconConstructing.SetActive(true);
                    fullIconConstructing.SetActive(true);
                }
                else
                {
                    summaryIconUpgrading.SetActive(true);
                    fullIconUpgrading.SetActive(true);
                }
            }
            else
            {
                if (area.AllRequirementsFulfilled() && area.currentLevel > area.startLevel)
                {
                    summaryIconReadyToUpgrade.SetActive(true);
                    fullIconReadyToUpgrade.SetActive(true);
                }
                else if (area.activeProductions.Count > 0)
                {
                    summaryIconProducingPanel.SetActive(true);
                    fullIconProducingPanel.SetActive(true);
                }
            }

            // Top left, power
            summaryIconConstructing.SetActive(false);
            summaryIconReadyToUpgrade.SetActive(false);
            summaryIconUpgrading.SetActive(false);
            summaryIconProducingPanel.SetActive(false);
            fullIconConstructing.SetActive(false);
            fullIconReadyToUpgrade.SetActive(false);
            fullIconUpgrading.SetActive(false);
            fullIconProducingPanel.SetActive(false);
            if (area.currentLevel > area.startLevel && area.requiresPower)
            {
                if (area.powered)
                {
                    summaryIconOutOfFuel.SetActive(false);
                    fullIconOutOfFuel.SetActive(false);
                }
                else
                {
                    summaryIconOutOfFuel.SetActive(true);
                    fullIconOutOfFuel.SetActive(true);
                }
            }
            else
            {
                summaryIconOutOfFuel.SetActive(false);
                fullIconOutOfFuel.SetActive(false);
            }
        }

        public void UpdateDescriptions()
        {
            string currentDescription = null;
            string futureDescription = null;
            if(Mod.localeDB["hideout_area_" + area.index + "_stage_"+area.currentLevel+"_description"] != null)
            {
                currentDescription = Mod.localeDB["hideout_area_" + area.index + "_stage_" + area.currentLevel + "_description"].ToString();
            }
            if(Mod.localeDB["hideout_area_" + area.index + "_stage_"+(area.currentLevel+1).ToString()+"_description"] != null)
            {
                futureDescription = Mod.localeDB["hideout_area_" + area.index + "_stage_" + (area.currentLevel + 1).ToString() + "_description"].ToString();
            }
            if (area.currentLevel == area.startLevel)
            {
                if(futureDescription == null)
                {
                    fullDescription.gameObject.SetActive(false);
                }
                else
                {
                    fullDescription.gameObject.SetActive(true);
                    fullDescription.text = futureDescription;
                }
            }
            else if(area.currentLevel > area.startLevel)
            {
                if(currentDescription == null)
                {
                    fullDescription.gameObject.SetActive(false);
                }
                else
                {
                    fullDescription.gameObject.SetActive(true);
                    fullDescription.text = currentDescription;
                }
                if(futureDescription == null)
                {
                    fullFutureDescription.gameObject.SetActive(false);
                }
                else
                {
                    fullFutureDescription.gameObject.SetActive(true);
                    fullFutureDescription.text = futureDescription;
                }
            }
        }

        public void UpdateRequirements()
        {
            if (area.currentLevel == area.startLevel)
            {
                requirementPanel.SetActive(true);
                futureRequirementPanel.SetActive(false);
            }
            else
            {
                requirementPanel.SetActive(false);
                futureRequirementPanel.SetActive(area.currentLevel < area.levels.Length - 1);
            }
            UpdateCurrentRequirements();
            UpdateFutureRequirements();
        }

        public void UpdateProductions()
        {
            productionPanel.SetActive(area.currentLevel != area.startLevel);

            // Destroy any existing productions
            while (productionPanelContainer.transform.childCount > 3) // 3: title + 3 production types
            {
                Transform currentChild = productionPanelContainer.transform.GetChild(3);
                currentChild.parent = null;
                Destroy(currentChild.gameObject);
            }

            if (productionPanel.activeSelf)
            {
                for(int i=0; i <= area.currentLevel; ++i)
                {
                    List<Production> currentProductions = area.productionsPerLevel[i];
                    for(int j=0; j < currentProductions.Count; ++j)
                    {
                        Production currentProduction = currentProductions[j];
                        if(area.index == 14) // Scav case
                        {
                            GameObject scavCaseProduction = Instantiate(scavCaseViewPrefab, productionPanelContainer.transform);
                            scavCaseProduction.SetActive(currentProduction.AllUnlockRequirementsFulfilled());
                            ScavCaseView scavCaseProductionView = scavCaseProduction.GetComponent<ScavCaseView>();
                            currentProduction.scavCaseUI = scavCaseProductionView;
                            scavCaseProductionView.timePanel.requiredTime.text = Mod.FormatTimeString(currentProduction.progressBaseTime);

                            // Add requirements
                            for (int k = 0; k < currentProduction.requirements.Count; ++k)
                            {
                                if (currentProduction.requirements[k].requirementType == Requirement.RequirementType.Item)
                                {
                                    // Add new requirement
                                    RequirementItemView itemRequirement = Instantiate(scavCaseProductionView.requirementItemViewPrefab, scavCaseProductionView.requirementsPanel).GetComponent<RequirementItemView>();

                                    itemRequirement.itemView.SetItemData(currentProduction.requirements[k].item);

                                    if (HideoutController.inventory.TryGetValue(currentProduction.requirements[k].item.H3ID, out int itemInventoryCount))
                                    {
                                        itemRequirement.amount.text = Mathf.Max(itemInventoryCount, currentProduction.requirements[k].itemCount).ToString() + "/" + currentProduction.requirements[k].itemCount;
                                    }
                                    else
                                    {
                                        itemRequirement.amount.text = "0/" + currentProduction.requirements[k].itemCount;
                                    }
                                    itemRequirement.fulfilledIcon.SetActive(currentProduction.requirements[k].fulfilled);
                                    itemRequirement.unfulfilledIcon.SetActive(!currentProduction.requirements[k].fulfilled);

                                    currentProduction.requirements[k].itemRequirementUI = itemRequirement;
                                    itemRequirement.gameObject.SetActive(true);
                                }
                            }

                            // Set buttons
                            if (currentProduction.readyCount > 0)
                            {
                                scavCaseProductionView.getButton.SetActive(true);
                                if (currentProduction.readyCount < currentProduction.limit)
                                {
                                    scavCaseProductionView.startButton.SetActive(false);
                                }
                            }
                            else
                            {
                                scavCaseProductionView.getButton.SetActive(false);
                                scavCaseProductionView.startButton.SetActive(currentProduction.AllRequirementsFulfilled());
                            }

                            // Set status
                            if (currentProduction.inProduction)
                            {
                                scavCaseProductionView.timePanel.percentage.gameObject.SetActive(true);
                                scavCaseProductionView.timePanel.percentage.text = ((int)currentProduction.progress).ToString() + "%";
                                scavCaseProductionView.productionStatus.SetActive(true);
                                scavCaseProductionView.productionStatusText.text = "Collecting\n("+Mod.FormatTimeString(currentProduction.timeLeft)+")...";
                            }
                            else
                            {
                                scavCaseProductionView.productionStatus.SetActive(false);
                                scavCaseProductionView.timePanel.percentage.gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            if (currentProduction.continuous) // Farming
                            {
                                GameObject farmingProduction = Instantiate(farmingViewPrefab, productionPanelContainer.transform);
                                farmingProduction.SetActive(currentProduction.AllUnlockRequirementsFulfilled());
                                FarmingView farmingView = farmingProduction.GetComponent<FarmingView>();
                                currentProduction.farmingUI = farmingView;
                                farmingView.timePanel.requiredTime.text = Mod.FormatTimeString(currentProduction.progressBaseTime);

                                // Set requirement
                                // Note: This makes assumption that a farming production only ever has a single requirement
                                if (currentProduction.requirements[0].requirementType == Requirement.RequirementType.Item
                                    || currentProduction.requirements[0].requirementType == Requirement.RequirementType.Resource)
                                {
                                    RequirementItemView itemRequirement = farmingView.installedItemView;
                                    ResultItemView itemRequirementStash = farmingView.stashItemView;
                                    currentProduction.requirements[0].stashItemUI = itemRequirementStash;

                                    itemRequirement.itemView.SetItemData(currentProduction.requirements[0].item);
                                    itemRequirementStash.itemView.SetItemData(currentProduction.requirements[0].item);

                                    int itemCount = 0;
                                    if(currentProduction.requirements[0].requirementType == Requirement.RequirementType.Item)
                                    {
                                        for (int l = 0; l < area.areaSlotsPerLevel[area.currentLevel].Length; ++l)
                                        {
                                            if (area.areaSlotsPerLevel[area.currentLevel][i].item != null)
                                            {
                                                ++itemCount;
                                            }
                                        }
                                    }
                                    else // Resource
                                    {
                                        for (int l = 0; l < area.areaSlotsPerLevel[area.currentLevel].Length; ++l)
                                        {
                                            if (area.areaSlotsPerLevel[area.currentLevel][i].item != null)
                                            {
                                                ++area.areaSlotsPerLevel[area.currentLevel][i].item.amount;
                                            }
                                        }
                                    }
                                    itemRequirement.amount.text = itemCount.ToString()+"\n(INSTALLED)";

                                    if (HideoutController.inventory.TryGetValue(currentProduction.requirements[0].item.H3ID, out int itemInventoryCount))
                                    {
                                        itemRequirementStash.amount.text = itemInventoryCount.ToString()+"\n(STASH)";
                                    }
                                    else
                                    {
                                        itemRequirementStash.amount.text = "0\n(STASH)";
                                    }

                                    itemRequirement.gameObject.SetActive(true);
                                }

                                // Set buttons
                                if (currentProduction.readyCount > 0)
                                {
                                    farmingView.getButton.SetActive(true);
                                }
                                else
                                {
                                    farmingView.getButton.SetActive(false);
                                }

                                // Set status
                                if (currentProduction.inProduction)
                                {
                                    farmingView.timePanel.percentage.gameObject.SetActive(true);
                                    farmingView.timePanel.percentage.text = ((int)currentProduction.progress).ToString() + "%";
                                    farmingView.productionStatus.SetActive(true);
                                    farmingView.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(currentProduction.timeLeft) + ")...";
                                }
                                else
                                {
                                    farmingView.productionStatus.SetActive(false);
                                    farmingView.timePanel.percentage.gameObject.SetActive(false);
                                }
                            }
                            else // Normal production
                            {
                                GameObject productionObject = Instantiate(productionViewPrefab, productionPanelContainer.transform);
                                productionObject.SetActive(currentProduction.AllUnlockRequirementsFulfilled());
                                ProductionView productionView = productionObject.GetComponent<ProductionView>();
                                currentProduction.productionUI = productionView;
                                productionView.timePanel.requiredTime.text = Mod.FormatTimeString(currentProduction.progressBaseTime);

                                // Add requirements
                                for (int k = 0; k < currentProduction.requirements.Count; ++k)
                                {
                                    if (currentProduction.requirements[k].requirementType == Requirement.RequirementType.Item
                                        || currentProduction.requirements[k].requirementType == Requirement.RequirementType.Resource
                                        || currentProduction.requirements[k].requirementType == Requirement.RequirementType.Tool)
                                    {
                                        // Add new requirement
                                        RequirementItemView itemRequirement = Instantiate(productionView.requirementItemViewPrefab, productionView.requirementsPanel).GetComponent<RequirementItemView>();

                                        itemRequirement.itemView.SetItemData(currentProduction.requirements[k].item);

                                        if (HideoutController.inventory.TryGetValue(currentProduction.requirements[k].item.H3ID, out int itemInventoryCount))
                                        {
                                            itemRequirement.amount.text = Mathf.Max(itemInventoryCount, currentProduction.requirements[k].itemCount).ToString() + "/" + currentProduction.requirements[k].itemCount;
                                        }
                                        else
                                        {
                                            itemRequirement.amount.text = "0/" + currentProduction.requirements[k].itemCount;
                                        }
                                        itemRequirement.fulfilledIcon.SetActive(currentProduction.requirements[k].fulfilled);
                                        itemRequirement.unfulfilledIcon.SetActive(!currentProduction.requirements[k].fulfilled);

                                        currentProduction.requirements[k].itemRequirementUI = itemRequirement;
                                        itemRequirement.gameObject.SetActive(true);
                                    }
                                }

                                // Set buttons
                                if (currentProduction.readyCount > 0)
                                {
                                    productionView.getButton.SetActive(true);
                                    if (currentProduction.readyCount < currentProduction.limit)
                                    {
                                        productionView.startButton.SetActive(false);
                                    }
                                }
                                else
                                {
                                    productionView.getButton.SetActive(false);
                                    productionView.startButton.SetActive(currentProduction.AllRequirementsFulfilled());
                                }

                                // Set status
                                if (currentProduction.inProduction)
                                {
                                    productionView.timePanel.percentage.gameObject.SetActive(true);
                                    productionView.timePanel.percentage.text = ((int)currentProduction.progress).ToString() + "%";
                                    productionView.productionStatus.SetActive(true);
                                    productionView.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(currentProduction.timeLeft) + ")...";
                                }
                                else
                                {
                                    productionView.productionStatus.SetActive(false);
                                    productionView.timePanel.percentage.gameObject.SetActive(false);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void UpdateBonuses()
        {
            if(area.currentLevel == area.startLevel)
            {
                bonusPanel.SetActive(true);
                bonusTitle.text = "FUTURE BONUSES";
                futureBonusPanel.SetActive(false);
                UpdateCurrentBonuses(area.currentLevel + 1);
            }
            else
            {
                bonusPanel.SetActive(true);
                bonusTitle.text = "CURRENT BONUSES";
                futureBonusPanel.SetActive(area.currentLevel < area.levels.Length - 1);
                UpdateCurrentBonuses(area.currentLevel);
            }
            UpdateFutureBonuses();
        }

        public void UpdateCurrentBonuses(int level)
        {
            // Destroy any existing bonuses
            while(bonusPanel.transform.childCount > 2)
            {
                Transform currentChild = bonusPanel.transform.GetChild(2);
                currentChild.parent = null;
                Destroy(currentChild.gameObject);
            }

            if (bonusPanel.activeSelf)
            {
                Bonus[] bonuses = area.bonusesPerLevel[level];

                for(int i=0; i < bonuses.Length; ++i)
                {
                    BonusUI bonus = Instantiate(bonusPrefab, bonusPanel.transform).GetComponent<BonusUI>();
                    bonus.bonusIcon.gameObject.SetActive(true);
                    switch (bonuses[i].iconPath)
                    {
                        case "/files/Hideout/icon_hideout_fuelslots.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[4];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_generic.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[2];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_meds.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[3];
                            break;
                        case "/files/Hideout/icon_hideout_scavitem.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[5];
                            break;
                        case "/files/Hideout/icon_hideout_videocardslots.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[8];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_bitcoin.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[1];
                            break;
                        case "/files/Hideout/icon_hideout_shootingrangeunlock.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[6];
                            break;
                        case "/files/Hideout/icon_hideout_unlocked.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[7];
                            break;
                        default:
                            bonus.bonusIcon.gameObject.SetActive(false);
                            break;
                    }
                    if (bonuses[i].bonusType == Bonus.BonusType.ExperienceRate 
                        || bonuses[i].bonusType == Bonus.BonusType.SkillGroupLevelingBoost)
                    {
                        bonus.bonusIcon.gameObject.SetActive(true);
                        bonus.bonusIcon.sprite = bonus.bonusIcons[0];
                    }
                    if (Mod.localeDB["hideout_" + bonuses[i].ID] != null)
                    {
                        bonus.description.text = Mod.localeDB["hideout_" + bonuses[i].ID].ToString();
                    }
                    else if(Mod.localeDB["hideout_" + bonuses[i].bonusType.ToString()] != null)
                    {
                        bonus.description.text = Mod.localeDB["hideout_" + bonuses[i].bonusType.ToString()].ToString();
                    }
                    else
                    {
                        Mod.LogError("DEV: Could not get bonus description. Bonus ID: " + bonuses[i].ID);
                    }
                    switch (bonuses[i].bonusType)
                    {
                        case Bonus.BonusType.EnergyRegeneration:
                        case Bonus.BonusType.DebuffEndDelay:
                        case Bonus.BonusType.RepairArmorBonus:
                        case Bonus.BonusType.HydrationRegeneration:
                        case Bonus.BonusType.HealthRegeneration:
                        case Bonus.BonusType.ScavCooldownTimer:
                        case Bonus.BonusType.QuestMoneyReward:
                        case Bonus.BonusType.InsuranceReturnTime:
                        case Bonus.BonusType.RagfairCommission:
                        case Bonus.BonusType.ExperienceRate:
                        case Bonus.BonusType.FuelConsumption:
                        case Bonus.BonusType.RepairWeaponBonus:
                            if (bonuses[i].value > 0)
                            {
                                bonus.effect.text = "+" + bonuses[i].value + "%";
                            }
                            break;
                        case Bonus.BonusType.SkillGroupLevelingBoost:
                            if (bonuses[i].value > 0)
                            {
                                bonus.effect.text = "+" + bonuses[i].value + "%";
                            }
                            bonus.description.text = bonuses[i].skillType.ToString() +" "+ bonus.description.text;
                            break;
                        case Bonus.BonusType.AdditionalSlots:
                        case Bonus.BonusType.MaximumEnergyReserve:
                            bonus.effect.text = "+" + bonuses[i].value;
                            break;
                        case Bonus.BonusType.UnlockArmorRepair:
                        case Bonus.BonusType.StashSize:
                        case Bonus.BonusType.TextBonus:
                        case Bonus.BonusType.UnlockWeaponModification:
                        case Bonus.BonusType.UnlockWeaponRepair:
                            bonus.effect.gameObject.SetActive(false);
                            break;
                    }

                    bonuses[i].bonusUI = bonus;
                    bonus.gameObject.SetActive(true);
                }
            }
        }

        public void UpdateFutureBonuses()
        {
            // Destroy any existing bonuses
            while(futureBonusPanel.transform.childCount > 2)
            {
                Transform currentChild = futureBonusPanel.transform.GetChild(2);
                currentChild.parent = null;
                Destroy(currentChild.gameObject);
            }

            if (futureBonusPanel.activeSelf)
            {
                Bonus[] bonuses = area.bonusesPerLevel[area.currentLevel + 1];

                for(int i=0; i < bonuses.Length; ++i)
                {
                    BonusUI bonus = Instantiate(futureBonusPrefab, futureBonusPanel.transform).GetComponent<BonusUI>();
                    bonus.bonusIcon.gameObject.SetActive(true);
                    switch (bonuses[i].iconPath)
                    {
                        case "/files/Hideout/icon_hideout_fuelslots.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[4];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_generic.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[2];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_meds.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[3];
                            break;
                        case "/files/Hideout/icon_hideout_scavitem.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[5];
                            break;
                        case "/files/Hideout/icon_hideout_videocardslots.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[8];
                            break;
                        case "/files/Hideout/icon_hideout_createitem_bitcoin.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[1];
                            break;
                        case "/files/Hideout/icon_hideout_shootingrangeunlock.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[6];
                            break;
                        case "/files/Hideout/icon_hideout_unlocked.png":
                            bonus.bonusIcon.sprite = bonus.bonusIcons[7];
                            break;
                        default:
                            bonus.bonusIcon.gameObject.SetActive(false);
                            break;
                    }
                    if (bonuses[i].bonusType == Bonus.BonusType.ExperienceRate 
                        || bonuses[i].bonusType == Bonus.BonusType.SkillGroupLevelingBoost)
                    {
                        bonus.bonusIcon.gameObject.SetActive(true);
                        bonus.bonusIcon.sprite = bonus.bonusIcons[0];
                    }
                    if (Mod.localeDB["hideout_" + bonuses[i].ID] != null)
                    {
                        bonus.description.text = Mod.localeDB["hideout_" + bonuses[i].ID].ToString();
                    }
                    else if(Mod.localeDB["hideout_" + bonuses[i].bonusType.ToString()] != null)
                    {
                        bonus.description.text = Mod.localeDB["hideout_" + bonuses[i].bonusType.ToString()].ToString();
                    }
                    else
                    {
                        Mod.LogError("DEV: Could not get bonus description. Bonus ID: " + bonuses[i].ID);
                    }
                    switch (bonuses[i].bonusType)
                    {
                        case Bonus.BonusType.EnergyRegeneration:
                        case Bonus.BonusType.DebuffEndDelay:
                        case Bonus.BonusType.RepairArmorBonus:
                        case Bonus.BonusType.HydrationRegeneration:
                        case Bonus.BonusType.HealthRegeneration:
                        case Bonus.BonusType.ScavCooldownTimer:
                        case Bonus.BonusType.QuestMoneyReward:
                        case Bonus.BonusType.InsuranceReturnTime:
                        case Bonus.BonusType.RagfairCommission:
                        case Bonus.BonusType.ExperienceRate:
                        case Bonus.BonusType.FuelConsumption:
                        case Bonus.BonusType.RepairWeaponBonus:
                            if (bonuses[i].value > 0)
                            {
                                bonus.effect.text = "+" + bonuses[i].value + "%";
                            }
                            break;
                        case Bonus.BonusType.SkillGroupLevelingBoost:
                            if (bonuses[i].value > 0)
                            {
                                bonus.effect.text = "+" + bonuses[i].value + "%";
                            }
                            bonus.description.text = bonuses[i].skillType.ToString() +" "+ bonus.description.text;
                            break;
                        case Bonus.BonusType.AdditionalSlots:
                        case Bonus.BonusType.MaximumEnergyReserve:
                            bonus.effect.text = "+" + bonuses[i].value;
                            break;
                        case Bonus.BonusType.UnlockArmorRepair:
                        case Bonus.BonusType.StashSize:
                        case Bonus.BonusType.TextBonus:
                        case Bonus.BonusType.UnlockWeaponModification:
                        case Bonus.BonusType.UnlockWeaponRepair:
                            bonus.effect.gameObject.SetActive(false);
                            break;
                    }

                    bonuses[i].bonusUI = bonus;
                    bonus.gameObject.SetActive(true);
                }
            }
        }

        public void UpdateCurrentRequirements()
        {
            // Destroy any existing requirements
            while(requirementPanel.transform.childCount > 5) // 5: title + 4 requirement panels
            {
                Transform currentChild = requirementPanel.transform.GetChild(5);
                currentChild.parent = null;
                Destroy(currentChild.gameObject);
            }

            if (requirementPanel.activeSelf)
            {
                Dictionary<Requirement.RequirementType, List<Requirement>> requirements = area.requirementsByTypePerLevel[area.currentLevel + 1];

                // Area requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Area, out List<Requirement> areaRequirements))
                {
                    Transform currentAreaRequirementParent = null;
                    for(int i=0; i< areaRequirements.Count; ++i)
                    {
                        if (areaRequirements[i].areaIndex == area.index)
                        {
                            // Area requirements may list our own area's previous level, skip in that case
                            continue;
                        }
                        else
                        {
                            // Make new parent if current is full or non existent
                            if(currentAreaRequirementParent == null || currentAreaRequirementParent.childCount > 2)
                            {
                                currentAreaRequirementParent = Instantiate(areaRequirementPanel, requirementPanel.transform).transform;
                                currentAreaRequirementParent.gameObject.SetActive(true);
                            }

                            // Add new requirement
                            AreaRequirement areaRequirement = Instantiate(areaRequirementPrefab, currentAreaRequirementParent).GetComponent<AreaRequirement>();
                            areaRequirement.areaIcon.sprite = areaRequirement.areaIcons[areaRequirements[i].areaIndex];
                            areaRequirement.requiredLevel.text = areaRequirements[i].areaLevel.ToString("00");
                            if(Mod.localeDB["hideout_area_" + area.index + "_name"] == null)
                            {
                                areaRequirement.areaName.text = "UNKNOWN";
                            }
                            else
                            {
                                areaRequirement.areaName.text = Mod.localeDB["hideout_area_" + areaRequirements[i].areaIndex + "_name"].ToString();
                            }
                            areaRequirement.fulfilled.SetActive(areaRequirements[i].fulfilled);
                            areaRequirement.unfulfilled.SetActive(!areaRequirements[i].fulfilled);
                            areaRequirements[i].areaRequirementUI = areaRequirement;
                            areaRequirement.gameObject.SetActive(true);
                        }
                    }
                }

                // Item requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Item, out List<Requirement> itemRequirements))
                {
                    Transform currentItemRequirementParent = null;
                    for(int i=0; i< itemRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if (currentItemRequirementParent == null || currentItemRequirementParent.childCount > 5)
                        {
                            currentItemRequirementParent = Instantiate(itemRequirementPanel, requirementPanel.transform).transform;
                            currentItemRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        RequirementItemView itemRequirement = Instantiate(itemRequirementPrefab, currentItemRequirementParent).GetComponent<RequirementItemView>();

                        itemRequirement.itemView.SetItemData(itemRequirements[i].item);

                        if(HideoutController.inventory.TryGetValue(itemRequirements[i].item.H3ID, out int itemInventoryCount))
                        {
                            itemRequirement.amount.text = Mathf.Max(itemInventoryCount, itemRequirements[i].itemCount).ToString() + "/" + itemRequirements[i].itemCount;
                        }
                        else
                        {
                            itemRequirement.amount.text = "0/" + itemRequirements[i].itemCount;
                        }
                        itemRequirement.fulfilledIcon.SetActive(itemRequirements[i].fulfilled);
                        itemRequirement.unfulfilledIcon.SetActive(!itemRequirements[i].fulfilled);

                        itemRequirements[i].itemRequirementUI = itemRequirement;
                        itemRequirement.gameObject.SetActive(true);
                    }
                }

                // Trader requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Trader, out List<Requirement> traderRequirements))
                {
                    Transform currentTraderRequirementParent = null;
                    for(int i=0; i< traderRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if(currentTraderRequirementParent == null || currentTraderRequirementParent.childCount > 3)
                        {
                            currentTraderRequirementParent = Instantiate(traderRequirementPanel, requirementPanel.transform).transform;
                            currentTraderRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        TraderRequirement traderRequirement = Instantiate(traderRequirementPrefab, currentTraderRequirementParent).GetComponent<TraderRequirement>();

                        traderRequirement.traderIcon.sprite = traderRequirement.traderIcons[traderRequirements[i].trader.index];
                        if(traderRequirements[i].traderLevel == traderRequirements[i].trader.levels.Length - 1)
                        {
                            traderRequirement.elite.SetActive(true);
                            traderRequirement.rankText.gameObject.SetActive(false);
                        }
                        else
                        {
                            traderRequirement.elite.SetActive(false);
                            traderRequirement.rankText.gameObject.SetActive(true);
                            traderRequirement.rankText.text = Trader.LevelToRoman(traderRequirements[i].traderLevel);
                        }
                        traderRequirement.fulfilled.SetActive(traderRequirements[i].fulfilled);
                        traderRequirement.unfulfilled.SetActive(!traderRequirements[i].fulfilled);

                        traderRequirements[i].traderRequirementUI = traderRequirement;
                        traderRequirement.gameObject.SetActive(true);
                    }
                }

                // Skill requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Skill, out List<Requirement> skillRequirements))
                {
                    Transform currentSkillRequirementParent = null;
                    for(int i=0; i< skillRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if(currentSkillRequirementParent == null || currentSkillRequirementParent.childCount > 3)
                        {
                            currentSkillRequirementParent = Instantiate(skillRequirementPanel, requirementPanel.transform).transform;
                            currentSkillRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        SkillRequirement skillRequirement = Instantiate(skillRequirementPrefab, currentSkillRequirementParent).GetComponent<SkillRequirement>();

                        skillRequirement.skillIcon.sprite = skillRequirement.skillIcons[skillRequirements[i].skillIndex];
                        if(skillRequirements[i].skillLevel == 51)
                        {
                            skillRequirement.elite.SetActive(true);
                            skillRequirement.rankText.gameObject.SetActive(false);
                        }
                        else
                        {
                            skillRequirement.elite.SetActive(false);
                            skillRequirement.rankText.gameObject.SetActive(true);
                            skillRequirement.rankText.text = skillRequirements[i].skillLevel.ToString("00");
                        }
                        skillRequirement.fulfilled.SetActive(skillRequirements[i].fulfilled);
                        skillRequirement.unfulfilled.SetActive(!skillRequirements[i].fulfilled);

                        skillRequirements[i].skillRequirementUI = skillRequirement;
                        skillRequirement.gameObject.SetActive(true);
                    }
                }
            }
        }

        public void UpdateFutureRequirements()
        {
            // Destroy any existing requirements
            while (futureRequirementPanel.transform.childCount > 5) // 5: title + 4 requirement panels
            {
                Transform currentChild = futureRequirementPanel.transform.GetChild(5);
                currentChild.parent = null;
                Destroy(currentChild.gameObject);
            }

            if (futureRequirementPanel.activeSelf)
            {
                Dictionary<Requirement.RequirementType, List<Requirement>> requirements = area.requirementsByTypePerLevel[area.currentLevel + 1];

                // Area requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Area, out List<Requirement> areaRequirements))
                {
                    Transform currentAreaRequirementParent = null;
                    for (int i = 0; i < areaRequirements.Count; ++i)
                    {
                        if (areaRequirements[i].areaIndex == area.index)
                        {
                            // Area requirements may list our own area's previous level, skip in that case
                            continue;
                        }
                        else
                        {
                            // Make new parent if current is full or non existent
                            if (currentAreaRequirementParent == null || currentAreaRequirementParent.childCount > 2)
                            {
                                currentAreaRequirementParent = Instantiate(futureAreaRequirementPanel, futureRequirementPanel.transform).transform;
                                currentAreaRequirementParent.gameObject.SetActive(true);
                            }

                            // Add new requirement
                            AreaRequirement areaRequirement = Instantiate(futureAreaRequirementPrefab, currentAreaRequirementParent).GetComponent<AreaRequirement>();
                            areaRequirement.areaIcon.sprite = areaRequirement.areaIcons[areaRequirements[i].areaIndex];
                            areaRequirement.requiredLevel.text = areaRequirements[i].areaLevel.ToString("00");
                            if (Mod.localeDB["hideout_area_" + area.index + "_name"] == null)
                            {
                                areaRequirement.areaName.text = "UNKNOWN";
                            }
                            else
                            {
                                areaRequirement.areaName.text = Mod.localeDB["hideout_area_" + areaRequirements[i].areaIndex + "_name"].ToString();
                            }
                            areaRequirement.fulfilled.SetActive(areaRequirements[i].fulfilled);
                            areaRequirement.unfulfilled.SetActive(!areaRequirements[i].fulfilled);
                            areaRequirements[i].areaRequirementUI = areaRequirement;
                            areaRequirement.gameObject.SetActive(true);
                        }
                    }
                }

                // Item requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Item, out List<Requirement> itemRequirements))
                {
                    Transform currentItemRequirementParent = null;
                    for (int i = 0; i < itemRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if (currentItemRequirementParent == null || currentItemRequirementParent.childCount > 5)
                        {
                            currentItemRequirementParent = Instantiate(futureItemRequirementPanel, futureRequirementPanel.transform).transform;
                            currentItemRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        RequirementItemView itemRequirement = Instantiate(futureItemRequirementPrefab, currentItemRequirementParent).GetComponent<RequirementItemView>();

                        itemRequirement.itemView.SetItemData(itemRequirements[i].item);

                        if (HideoutController.inventory.TryGetValue(itemRequirements[i].item.H3ID, out int itemInventoryCount))
                        {
                            itemRequirement.amount.text = Mathf.Max(itemInventoryCount, itemRequirements[i].itemCount).ToString() + "/" + itemRequirements[i].itemCount;
                        }
                        else
                        {
                            itemRequirement.amount.text = "0/" + itemRequirements[i].itemCount;
                        }
                        itemRequirement.fulfilledIcon.SetActive(itemRequirements[i].fulfilled);
                        itemRequirement.unfulfilledIcon.SetActive(!itemRequirements[i].fulfilled);

                        itemRequirements[i].itemRequirementUI = itemRequirement;
                        itemRequirement.gameObject.SetActive(true);
                    }
                }

                // Trader requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Trader, out List<Requirement> traderRequirements))
                {
                    Transform currentTraderRequirementParent = null;
                    for (int i = 0; i < traderRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if (currentTraderRequirementParent == null || currentTraderRequirementParent.childCount > 3)
                        {
                            currentTraderRequirementParent = Instantiate(futureTraderRequirementPanel, futureRequirementPanel.transform).transform;
                            currentTraderRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        TraderRequirement traderRequirement = Instantiate(futureTraderRequirementPrefab, currentTraderRequirementParent).GetComponent<TraderRequirement>();

                        traderRequirement.traderIcon.sprite = traderRequirement.traderIcons[traderRequirements[i].trader.index];
                        if (traderRequirements[i].traderLevel == traderRequirements[i].trader.levels.Length - 1)
                        {
                            traderRequirement.elite.SetActive(true);
                            traderRequirement.rankText.gameObject.SetActive(false);
                        }
                        else
                        {
                            traderRequirement.elite.SetActive(false);
                            traderRequirement.rankText.gameObject.SetActive(true);
                            traderRequirement.rankText.text = Trader.LevelToRoman(traderRequirements[i].traderLevel);
                        }
                        traderRequirement.fulfilled.SetActive(traderRequirements[i].fulfilled);
                        traderRequirement.unfulfilled.SetActive(!traderRequirements[i].fulfilled);

                        traderRequirements[i].traderRequirementUI = traderRequirement;
                        traderRequirement.gameObject.SetActive(true);
                    }
                }

                // Skill requirements
                if (requirements.TryGetValue(Requirement.RequirementType.Skill, out List<Requirement> skillRequirements))
                {
                    Transform currentSkillRequirementParent = null;
                    for (int i = 0; i < skillRequirements.Count; ++i)
                    {
                        // Make new parent if current is full or non existent
                        if (currentSkillRequirementParent == null || currentSkillRequirementParent.childCount > 3)
                        {
                            currentSkillRequirementParent = Instantiate(futureSkillRequirementPanel, futureRequirementPanel.transform).transform;
                            currentSkillRequirementParent.gameObject.SetActive(true);
                        }

                        // Add new requirement
                        SkillRequirement skillRequirement = Instantiate(futureSkillRequirementPrefab, currentSkillRequirementParent).GetComponent<SkillRequirement>();

                        skillRequirement.skillIcon.sprite = skillRequirement.skillIcons[skillRequirements[i].skillIndex];
                        if (skillRequirements[i].skillLevel == 51)
                        {
                            skillRequirement.elite.SetActive(true);
                            skillRequirement.rankText.gameObject.SetActive(false);
                        }
                        else
                        {
                            skillRequirement.elite.SetActive(false);
                            skillRequirement.rankText.gameObject.SetActive(true);
                            skillRequirement.rankText.text = skillRequirements[i].skillLevel.ToString("00");
                        }
                        skillRequirement.fulfilled.SetActive(skillRequirements[i].fulfilled);
                        skillRequirement.unfulfilled.SetActive(!skillRequirements[i].fulfilled);

                        skillRequirements[i].skillRequirementUI = skillRequirement;
                        skillRequirement.gameObject.SetActive(true);
                    }
                }
            }
        }

        public void UpdateBottomButtons()
        {
            if(area.currentLevel == area.startLevel)
            {
                if (!area.upgrading)
                {
                    constructButton.SetActive(area.AllRequirementsFulfilled());
                }
                levelButton.SetActive(false);
                backButton.SetActive(false);
                upgradeButton.SetActive(false);
            }
            else
            {
                constructButton.SetActive(false);
                if (currentContent.gameObject.activeSelf)
                {
                    levelButton.SetActive(true);
                    backButton.SetActive(false);
                    upgradeButton.SetActive(false);
                }
                else
                {
                    levelButton.SetActive(false);
                    backButton.SetActive(true);
                    upgradeButton.SetActive(area.AllRequirementsFulfilled());
                }
            }
        }

        public void OnSummaryClicked()
        {
            buttonClickSound.Play();
            genericAudioSource.PlayOneShot(genericAudioClips[0]);
            OpenUI();
        }

        public void OnCloseClicked()
        {
            buttonClickSound.Play();
            CloseUI();
        }

        public void ToggleUI()
        {
            if (inSummary)
            {
                OpenUI();
            }
            else
            {
                CloseUI();
            }
        }

        public void OpenUI()
        {
            summary.SetActive(false);
            full.SetActive(true);
            inSummary = false;
        }

        public void CloseUI()
        {
            summary.SetActive(true);
            full.SetActive(false);
            inSummary = true;
        }

        public void OnConstructClicked()
        {
            buttonClickSound.Play();

            if(!upgradeConfirmDialog.activeSelf
                && !warningDialog.activeSelf
                && !blockDialog.activeSelf)
            {
                upgradeConfirmDialog.SetActive(true);
            }

            area.SetUpgradeCheckProcessors(true);
        }

        public void OnLevelClicked()
        {
            buttonClickSound.Play();

            // Page
            currentContent.gameObject.SetActive(false);
            futureContent.gameObject.SetActive(true);

            // Buttons
            levelButton.SetActive(false);
            backButton.SetActive(true);
            upgradeButton.SetActive(area.AllRequirementsFulfilled());
        }

        public void OnBackClicked()
        {
            buttonClickSound.Play();

            // Page
            currentContent.gameObject.SetActive(true);
            futureContent.gameObject.SetActive(false);

            // Buttons
            levelButton.SetActive(true);
            backButton.SetActive(false);
            upgradeButton.SetActive(false);
        }

        public void OnUpgradeClicked()
        {
            buttonClickSound.Play();

            if (!upgradeConfirmDialog.activeSelf
                && !warningDialog.activeSelf
                && !blockDialog.activeSelf)
            {
                upgradeConfirmDialog.SetActive(true);
            }

            area.SetUpgradeCheckProcessors(true);
        }

        public void OnUpgradeConfirmConfirmClicked()
        {
            buttonClickSound.Play();

            area.BeginUpgrade();

            genericAudioSource.PlayOneShot(genericAudioClips[1]);

            upgradeConfirmDialog.SetActive(false);

            UpdateStatusIcons();
            UpdateStatusTexts();
            UpdateBottomButtons();
        }

        public void OnUpgradeConfirmCancelClicked()
        {
            buttonClickSound.Play();

            upgradeConfirmDialog.SetActive(false);

            area.SetUpgradeCheckProcessors(false);
        }

        public void OnWarningContinueClicked()
        {
            buttonClickSound.Play();

            warningDialog.SetActive(false);
            upgradeConfirmDialog.SetActive(true);
        }

        public void OnWarningCancelClicked()
        {
            buttonClickSound.Play();

            warningDialog.SetActive(false);

            area.SetUpgradeCheckProcessors(false);
        }

        public void OnBlockCancelClicked()
        {
            buttonClickSound.Play();

            blockDialog.SetActive(false);

            area.SetUpgradeCheckProcessors(false);
        }

        public void PlaySlotInputSound()
        {
            genericAudioSource.PlayOneShot(genericAudioClips[3]);
        }
    }
}
