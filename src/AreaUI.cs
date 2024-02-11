using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class AreaUI : MonoBehaviour
    {
        public Area area;
        [NonSerialized]
        public bool inSummary;
        public GameObject summary;
        public GameObject full;
        public AudioSource buttonClickSound;
        public Sprite[] statusSprites; // Locked, Unlocked, Constructing, Producing, ReadyToUpgrade, Upgrading, OutofFuel
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
        public RectTransform visibleHeightTransform;
        public HoverScroll upHoverscroll;
        public HoverScroll downHoverscroll;
        public GameObject upgradeConfirmDialog;
        public GameObject warningDialog;
        public GameObject blockDialog;
        public AudioSource genericAudioSource;
        public AudioClip[] genericAudioClips; // AreaSelected, UpgradeBegin, UpgradeComplete, ItemInstalled, ItemStarted, ItemComplete

        public void Init()
        {
            cont from ere // init ui based on area data (reqs and bonuses)
        }

        public void OnSummaryClicked()
        {
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
            // TODO
        }

        public void OnLevelClicked()
        {
            // TODO
        }

        public void OnBackClicked()
        {
            // TODO
        }

        public void OnUpgradeClicked()
        {
            // TODO
        }

        public void OnUpgradeConfirmConfirmClicked()
        {

        }

        public void OnUpgradeConfirmCancelClicked()
        {

        }

        public void OnWarningContinueClicked()
        {

        }

        public void OnWarningCancelClicked()
        {

        }

        public void OnBlockCancelClicked()
        {

        }
    }
}
