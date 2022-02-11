using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class EFM_BaseAreaManager : MonoBehaviour
    {
        public static bool generatorRunning;

        public EFM_Base_Manager baseManager;
        private bool init;

        public int areaIndex;
        public int level;
        public bool constructing;
        public float constructTime; // What time the construction has started so we can check how long it has been since then and decide if it is done
        public List<GameObject> slotItems; // Slots that could contain items, like generator that could have gas cans
        public List<EFM_AreaProduction> productions;
        public float constructionTimer; // How much time is actually left on the construction

        // UI
        public GameObject areaCanvas;
        public AudioSource buttonClickSound;
        public List<List<GameObject>> areaRequirementParents;

        public void Init()
        {
            InitUI();

            init = true;
        }

        public void InitUI()
        {
            // Attach an area canvas to the area
            areaCanvas = Instantiate(EFM_Base_Manager.areaCanvasPrefab, transform.GetChild(transform.childCount - 2));

            // Summary button
            areaCanvas.transform.GetChild(0).GetChild(2).GetComponent<Button>().onClick.AddListener(OnSummaryClicked);

            // Full, Close button
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(OnFullCloseClicked);

            // Set area canvas defaults
            string areaName = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_name"].ToString();

            // Area summary Icon
            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = EFM_Base_Manager.areaIcons[areaIndex];

            // Area summary Name
            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = areaName;

            // Area summary elite background
            if (areaIndex == 13 || areaIndex == 14 || areaIndex == 17 || areaIndex == 18 || areaIndex == 19)
            {
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
            }

            // Area full Icon
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = EFM_Base_Manager.areaIcons[areaIndex];

            // Area full Name
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = areaName;

            // Set area state based on loaded base and areasDB
            UpdateAreaState();
        }

        public void Update()
        {
            if (init)
            {
                UpdateProductions();

                UpdateConstruction();
            }
        }

        private void UpdateProductions()
        {
            if (((Mod.areasDB.areaDefaults[areaIndex].needsFuel && generatorRunning) || !Mod.areasDB.areaDefaults[areaIndex].needsFuel) && productions != null && productions.Count > 0)
            {
                for (int i = productions.Count - 1; i >= 0; --i)
                {

                    productions[i].timeLeft -= Time.deltaTime;
                    if (productions[i].timeLeft <= 0)
                    {
                        // TODO: Deal with production completion
                        // Should give the item to player by instantiating it and parenting it to a predetermined point on the corresponding area
                        productions.RemoveAt(i);
                        if (productions.Count == 0)
                        {
                            // TODO: Set areacanvas producing backgrounds to inactive
                            break;
                        }
                    }
                }
            }
        }

        private void UpdateConstruction()
        {
            if (constructing)
            {
                constructionTimer -= Time.deltaTime;
                if (constructionTimer <= 0)
                {
                    constructing = false;
                    constructionTimer = 0;
                    constructTime = 0;
                    ++level;
                    UpdateAreaState();
                }
            }
        }

        // TODO: This should only be called on an area if the area it self changes. 
        // Obviously though UI should change when, for example, changes in amount of items in inventory required by an area has changed, this will be done else where
        // so as to not have to refresh the entire state UI every time player drops an item in the hideout and so on
        public void UpdateAreaState()
        {
            Mod.instance.LogInfo("UpdateAreaStateCalled on area: " + areaIndex);
            // Destroy any existing extra requirement parent
            if(areaRequirementParents == null)
            {
                areaRequirementParents = new List<List<GameObject>>();
                for(int i=0; i < 4; ++i)
                {
                    areaRequirementParents.Add(new List<GameObject>());
                    areaRequirementParents[i].Add(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(i + 2).gameObject);
                }
            }
            foreach(List<GameObject> list in areaRequirementParents)
            {
                if(list.Count > 1)
                {
                    for(int i=list.Count - 1; i >= 1; --i)
                    {
                        Destroy(list[i]);
                        list.RemoveAt(i);
                    }
                }
            }

            if (constructing)
            {
                Mod.instance.LogInfo("\tConstructing");
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundNormalSprite; // Icon background color
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(true); // In construction icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon

                // Full top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundNormalSprite; // Icon background color
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(true); // In construction icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon

                if (level == 0)
                {
                    Mod.instance.LogInfo("\t\tLevel 0");
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(true); // Constructing icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Constructing"; // Status text

                    // Full top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(true); // Constructing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconConstructing; // Status text
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Constructing"; // Status text

                    // Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_"+areaIndex+"_stage_1_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[1].bonuses, "FUTURE BONUSES");
                    
                    // Full middle 2
                    // Nothing, when constructing the first level we show that description and future bonuses on middle

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.GetComponent<Collider>().enabled = false; // Disable button

                    // Full bottom 2
                    // No access to bottom 2 when constructing
                }
                else
                {
                    Mod.instance.LogInfo("\t\tLevel 1+");
                    // Summary 
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true); // Upgrading icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Upgrading"; // Status text

                    // Full top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true); // Upgrading icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconUpgrading; // Status text
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Upgrading"; // Status text

                    // Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_"+level+"_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[level].bonuses, "CURRENT BONUSES");

                    // Full middle 2
                    areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].bonuses, "FUTURE BONUSES");

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.transform.GetChild(1).GetComponent<Text>().text = "Level "+(level+1); // Next level bottom button
                    bottomButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);

                    // Full bottom 2
                    Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                    GameObject bottom2BackButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                    bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);

                    GameObject bottom2UpgradeButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrading..."; // Upgrade bottom button
                    bottom2UpgradeButton.GetComponent<Collider>().enabled = false; // Disable upgrade button because curently upgrading
                }
            }
            else // Not in construction or in upgrade process
            {
                Mod.instance.LogInfo("\tNot constructing");
                bool requirementsFullfilled = GetRequirementsFullfilled(true, false);

                // Summary and Full top
                if (generatorRunning)
                {
                    Mod.instance.LogInfo("\t\tgen running");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Out of fuel icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Out of fuel icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = null; // Status icon

                    if (productions != null && productions.Count > 0)
                    {
                        Mod.instance.LogInfo("\t\t\tGot productions");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true); // Producing icon background
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true); // Producing icon background
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Producing icon

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconProducing; // Status icon

                        if (productions.Count > 1)
                        {
                            Mod.instance.LogInfo("\t\t\t\tGot more");
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true); // Producing icon
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = productions.Count.ToString(); // Production count
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true); // Producing icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = productions.Count.ToString(); // Production count
                        }
                    }
                    else if (productions != null && productions.Count > 0)
                    {
                        Mod.instance.LogInfo("\t\t\tno productions");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon

                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                    }
                }
                else
                {
                    Mod.instance.LogInfo("\t\tgen not running");

                    if (Mod.areasDB.areaDefaults[areaIndex].needsFuel)
                    {
                        Mod.instance.LogInfo("\t\t\tbut needs fuel");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconOutOfFuel; // Status icon
                    }

                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon

                    Mod.instance.LogInfo("0");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                }

                Mod.instance.LogInfo("0");
                //Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false); // In construction icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon

                Mod.instance.LogInfo("0");
                // Full top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false); // In construction icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon

                Mod.instance.LogInfo("0");
                if (level == 0)
                {
                    Mod.instance.LogInfo("\t\t\tlevel 0");
                    // Summary and full top
                    if (requirementsFullfilled)
                    {
                        Mod.instance.LogInfo("\t\t\t\trequs fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundAvailableSprite; // Icon background color
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundAvailableSprite; // Icon background color
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconUnlocked; // Status text
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\t\t\trequs not fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundLockedSprite; // Icon background color
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Locked"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundLockedSprite; // Icon background color
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconLocked; // Status text
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Locked"; // Status text
                    }
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(!requirementsFullfilled); // Locked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(requirementsFullfilled); // Unlocked icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(!requirementsFullfilled); // Locked icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(requirementsFullfilled); // Unlocked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon

                    // We want to show level 1 information on middle
                    // Full middle
                    Mod.instance.LogInfo("\t\t\t0");
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_1_description"].ToString();
                    SetRequirements(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1), Mod.areasDB.areaDefaults[areaIndex].stages[1].requirements);
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[1].bonuses, "FUTURE BONUSES");

                    Mod.instance.LogInfo("\t\t\t0");
                    // Full middle 2
                    // There is no full middle 2 on a level 0 area which isn't in construction

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomConstructButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomConstructButton.transform.GetChild(1).GetComponent<Text>().text = "Construct"; // Construct bottom button
                    bottomConstructButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);

                    // Full bottom 2
                    // There is no full bottom 2 on a level 0 area which isn't in construction
                }
                else
                {
                    Mod.instance.LogInfo("\t\t\tlevel 1+");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundNormalSprite; // Icon background color
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaBackgroundNormalSprite; // Icon background color
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(requirementsFullfilled); // Ready to upgrade icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(requirementsFullfilled); // Ready to upgrade icon

                    // Summary and full top
                    if (requirementsFullfilled)
                    {
                        Mod.instance.LogInfo("\t\t\t\treqs fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconReadyUpgrade; // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\t\t\treqs not fulfilled");
                        if (Mod.areasDB.areaDefaults[areaIndex].needsFuel && !generatorRunning)
                        {
                            Mod.instance.LogInfo("\t\t\t\t\tneed fuel but gen not running");
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconOutOfFuel; // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                        }
                        else
                        {
                            Mod.instance.LogInfo("\t\t\t\t\tnot need fuel or gen running");
                            if (productions != null && productions.Count > 0)
                            {
                                Mod.instance.LogInfo("\t\t\t\t\t\thas production");
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconProducing; // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                            }
                            else
                            {
                                Mod.instance.LogInfo("\t\t\t\t\t\thas production");
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = null; // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                            }
                        }
                    }

                    // Middle will show current level (no requirements) and middle 2 will show next level (with requirements), if there is a next level
                    //Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + level + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[level].bonuses, "CURRENT BONUSES");

                    // Full middle 2, and full bottom and bottom 2
                    if (Mod.areasDB.areaDefaults[areaIndex].stages.Count >= level) // Check if we have a next level
                    {
                        Mod.instance.LogInfo("\t\thas a next level");
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                        SetRequirements(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1), Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].requirements);
                        SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].bonuses, "FUTURE BONUSES");

                        Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                        GameObject bottomNextLevelButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                        bottomNextLevelButton.transform.GetChild(1).GetComponent<Text>().text = "Level " + (level + 1); // Next level bottom button
                        bottomNextLevelButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);

                        Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                        GameObject bottom2BackButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                        bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                        
                        GameObject bottom2UpgradeButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrade"; // Upgrade bottom button
                        if (GetRequirementsFullfilled(true, true))
                        {
                            bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                        }
                        else
                        {
                            bottom2UpgradeButton.GetComponent<Collider>().enabled = false;
                        }
                    }
                }
            }
        }

        public void SetRequirements(Transform parentRequirementsPanel, List<Requirement> requirements)
        {
            Mod.instance.LogInfo("set requirements called");
            parentRequirementsPanel.gameObject.SetActive(true); // Requirements panel
            if (requirements != null && requirements.Count > 0)
            {
                foreach (Requirement requirement in requirements)
                {
                    switch (requirement.type)
                    {
                        case "Area":
                            Mod.instance.LogInfo("\tarea");
                            // Make a new area requirements parent is necessary to fit another area requirement
                            Transform areaRequirementParentToUse;
                            if (areaRequirementParents[0][areaRequirementParents[0].Count - 1].transform.childCount == 2)
                            {
                                areaRequirementParentToUse = Instantiate(EFM_Base_Manager.areaRequirementsPrefab, areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1)).transform;
                            }
                            else
                            {
                                areaRequirementParentToUse = areaRequirementParents[0][areaRequirementParents[0].Count - 1].transform;
                            }
                            areaRequirementParentToUse.gameObject.SetActive(true);

                            GameObject areaRequirement = Instantiate(EFM_Base_Manager.areaRequirementPrefab, areaRequirementParentToUse);

                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = EFM_Base_Manager.areaIcons[requirement.areaType]; // Area icon
                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(5).GetComponent<Text>().text = "0" + requirement.requiredLevel.ToString(); // Area level
                            Text areaRequirementNameText = areaRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>();
                            areaRequirementNameText.text = Mod.localDB["interface"]["hideout_area_" + requirement.areaType + "_name"].ToString(); // Area name

                            Mod.instance.LogInfo("\t0");
                            // Check if requirement is met
                            if (baseManager.baseAreaManagers[requirement.areaType].level >= requirement.requiredLevel)
                            {
                                Mod.instance.LogInfo("\t1");
                                areaRequirementNameText.color = Color.white;
                                Mod.instance.LogInfo("\t1");
                                areaRequirement.transform.GetChild(2).GetComponent<Image>().sprite = EFM_Base_Manager.requirementFulfilled;
                                Mod.instance.LogInfo("\t1");
                            }
                            else
                            {
                                Mod.instance.LogInfo("\t2");
                                areaRequirementNameText.color = new Color(1, 0.27f, 0.27f);
                                Mod.instance.LogInfo("\t2");
                                areaRequirement.transform.GetChild(2).GetComponent<Image>().sprite = EFM_Base_Manager.requirementLocked;
                                Mod.instance.LogInfo("\t2");
                            }
                            break;
                        case "Item":
                            Mod.instance.LogInfo("\titem");
                            // Make a new item requirements parent is necessary to fit another item requirement
                            Transform itemRequirementParentToUse;
                            if (areaRequirementParents[1][areaRequirementParents[1].Count - 1].transform.childCount == 5)
                            {
                                itemRequirementParentToUse = Instantiate(EFM_Base_Manager.itemRequirementsPrefab, areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1)).transform;
                            }
                            else
                            {
                                itemRequirementParentToUse = areaRequirementParents[1][areaRequirementParents[1].Count - 1].transform;
                            }
                            itemRequirementParentToUse.gameObject.SetActive(true);

                            Mod.instance.LogInfo("\t0");
                            GameObject itemRequirement = Instantiate(EFM_Base_Manager.itemRequirementPrefab, itemRequirementParentToUse);

                            GameObject itemMesh = null;
                            FVRObject itemObjectWrapper = null;
                            if (Mod.itemMap.ContainsKey(requirement.templateId))
                            {
                                Mod.instance.LogInfo("\t\t0");
                                if (int.TryParse(Mod.itemMap[requirement.templateId], out int tryParseResult))
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    GameObject itemPrefab = Mod.itemPrefabs[tryParseResult];
                                    itemMesh = Instantiate(itemPrefab, itemRequirement.transform.GetChild(0).GetChild(3));
                                    FVRObject itemMeshObjectWrapper = itemMesh.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                                    itemMeshObjectWrapper.ItemID = tryParseResult.ToString();
                                    itemObjectWrapper = itemMeshObjectWrapper;

                                    Destroy(itemMesh.transform.GetChild(0).GetChild(itemMesh.transform.GetChild(0).childCount - 1).gameObject); // Destroy collisions
                                    Destroy(itemMesh.transform.GetChild(3).gameObject); // Destroy interactives
                                }
                                else // Is not a custom item
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    GameObject itemPrefab = IM.OD[Mod.itemMap[requirement.templateId]].GetGameObject();
                                    itemMesh = Instantiate(itemPrefab, itemRequirement.transform.GetChild(0).GetChild(3));
                                    FVRObject itemMeshObjectWrapper = itemMesh.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                                    itemMeshObjectWrapper.ItemID = Mod.itemMap[requirement.templateId];
                                    itemObjectWrapper = itemMeshObjectWrapper;

                                    Collider[] cols = itemMesh.GetComponentsInChildren<Collider>();
                                    foreach (Collider col in cols)
                                    {
                                        Destroy(col);
                                    }
                                }
                                Mod.instance.LogInfo("\t\t0");

                                int itemAmountNeeded = (int)requirement.count;
                                int itemAmountInInventory = 0;
                                Mod.instance.LogInfo("\t\t base manager null?: "+(baseManager == null));
                                Mod.instance.LogInfo("\t\t base inventory null?: "+(baseManager.baseInventory == null));
                                if (baseManager.baseInventory.ContainsKey(itemObjectWrapper.ItemID))
                                {
                                    itemAmountInInventory = baseManager.baseInventory[itemObjectWrapper.ItemID];
                                }
                                itemRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = Mathf.Min(itemAmountNeeded, itemAmountInInventory).ToString() + "/" + itemAmountNeeded; // Area level

                                // Check if requirement is met
                                if (itemAmountInInventory >= itemAmountNeeded)
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementFulfilled;
                                }
                                else
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementLocked;
                                }
                            }
                            break;
                        case "TraderLoyalty":
                            Mod.instance.LogInfo("\ttrader");
                            // Make a new trader requirements parent if necessary to fit another trader requirement
                            Transform traderRequirementParentToUse;
                            Mod.instance.LogInfo("\t0");
                            if (areaRequirementParents[2][areaRequirementParents[2].Count - 1].transform.childCount == 2)
                            {
                                traderRequirementParentToUse = Instantiate(EFM_Base_Manager.traderRequirementsPrefab, areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1)).transform;
                            }
                            else
                            {
                                traderRequirementParentToUse = areaRequirementParents[2][areaRequirementParents[2].Count - 1].transform;
                            }
                            Mod.instance.LogInfo("\t0");
                            traderRequirementParentToUse.gameObject.SetActive(true);

                            Mod.instance.LogInfo("\t0");
                            GameObject traderRequirement = Instantiate(EFM_Base_Manager.traderRequirementPrefab, traderRequirementParentToUse);

                            Mod.instance.LogInfo("\t0");
                            int traderIndex = EFM_TraderStatus.IDToIndex(requirement.traderId);
                            Mod.instance.LogInfo("\t0");
                            traderRequirement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[traderIndex]; // Trader avatar
                            Mod.instance.LogInfo("\t0");
                            if (requirement.loyaltyLevel == 4)
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = String.Concat(Enumerable.Repeat("I", (int)requirement.loyaltyLevel));
                            }
                            Mod.instance.LogInfo("\t0");
                            // Check if requirement is met
                            Mod.instance.LogInfo("\t0");
                            if (baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() >= requirement.loyaltyLevel)
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementFulfilled;
                            }
                            else
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementLocked;
                            }
                            Mod.instance.LogInfo("\t0");
                            break;
                        case "Skill":
                            Mod.instance.LogInfo("\tskill");
                            // Make a new skill requirements parent if necessary to fit another skill requirement
                            Transform skillRequirementParentToUse;
                            if (areaRequirementParents[3][areaRequirementParents[3].Count - 1].transform.childCount == 4)
                            {
                                skillRequirementParentToUse = Instantiate(EFM_Base_Manager.skillRequirementsPrefab, areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1)).transform;
                            }
                            else
                            {
                                skillRequirementParentToUse = areaRequirementParents[3][areaRequirementParents[3].Count - 1].transform;
                            }
                            skillRequirementParentToUse.gameObject.SetActive(true);

                            GameObject skillRequirement = Instantiate(EFM_Base_Manager.skillRequirementPrefab, skillRequirementParentToUse);

                            skillRequirement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.skillIcons[(int)requirement.skillIndex]; // Skill icon
                            if (requirement.skillLevel == 51)
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = requirement.skillLevel.ToString();
                            }

                            // Check if requirement is met
                            float skillLevel = baseManager.data.skills[(int)requirement.skillIndex].progress / 100;
                            if (skillLevel >= requirement.skillLevel)
                            {
                                skillRequirement.transform.GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementFulfilled;
                            }
                            else
                            {
                                skillRequirement.transform.GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementLocked;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void SetBonuses(Transform parent, List<Bonus> bonuses, string label)
        {
            if (bonuses != null && bonuses.Count > 0)
            {
                parent.gameObject.SetActive(true); // Bonuses
                parent.GetChild(0).GetComponent<Text>().text = label; // Bonuses label

                foreach (Bonus areaBonus in bonuses)
                {
                    GameObject bonus = Instantiate(EFM_Base_Manager.bonusPrefab, parent);
                    Sprite bonusIcon = null;
                    if (areaBonus.icon != null && !areaBonus.icon.Equals(""))
                    {
                        bonusIcon = EFM_Base_Manager.bonusIcons[areaBonus.icon];
                    }
                    else
                    {
                        bonusIcon = EFM_Base_Manager.bonusIcons[areaBonus.type];
                    }
                    bonus.transform.GetChild(0).GetComponent<Image>().sprite = bonusIcon; // Bonus icon
                    if (areaBonus.type.Equals("TextBonus"))
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_" + areaBonus.id].ToString();
                    }
                    else
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_" + areaBonus.type].ToString(); // Bonus description
                    }
                    string bonusEffect = "";
                    switch (areaBonus.type)
                    {
                        case "ExperienceRate":
                        case "EnergyRegeneration":
                        case "HealthRegeneration":
                        case "HydrationRegeneration":
                        case "QuestMoneyReward":
                        case "MaximumEnergyReserve":
                            bonusEffect = "+" + areaBonus.value + "%";
                            break;
                        case "FuelConsumption":
                        case "DebuffEndDelay":
                        case "ScavCooldownTimer":
                        case "InsuranceReturnTime":
                        case "RagfairCommission":
                            bonusEffect = "" + areaBonus.value + "%";
                            break;
                        case "AdditionalSlots":
                            bonusEffect = "+" + areaBonus.value;
                            break;
                        case "SkillGroupLevelingBoost":
                            bonusEffect = areaBonus.skillType + ", +" + areaBonus.value + "%";
                            break;
                        case "StashSize":
                            if (areaBonus.templateId.Equals("566abbc34bdc2d92178b4576"))
                            {
                                bonusEffect = "A desk with a few shelves you found lying around";
                            }
                            else if (areaBonus.templateId.Equals("5811ce572459770cba1a34ea"))
                            {
                                bonusEffect = "Some wooden shelves";
                            }
                            else if (areaBonus.templateId.Equals("5811ce662459770f6f490f32"))
                            {
                                bonusEffect = "Some actual storage shelves";
                            }
                            else if (areaBonus.templateId.Equals("5811ce772459770e9e5f9532"))
                            {
                                bonusEffect = "More shelves";
                            }
                            break;
                    }
                    bonus.transform.GetChild(2).GetComponent<Text>().text = bonusEffect; // Bonus effect
                }
            }
        }

        public bool GetRequirementFullfilled(Requirement requirement)
        {
            switch (requirement.type)
            {
                case "Area":
                    Mod.instance.LogInfo("\t\t\t\tArea");
                    return baseManager.baseAreaManagers[requirement.areaType].level >= requirement.requiredLevel;
                case "Item":
                    Mod.instance.LogInfo("\t\t\t\tItem");
                    string itemID = "";
                    if (Mod.itemMap.ContainsKey(requirement.templateId))
                    {
                        if (int.TryParse(Mod.itemMap[requirement.templateId], out int tryParseResult))
                        {
                            itemID = tryParseResult.ToString();
                        }
                        else // Is not a custom item
                        {
                            itemID = Mod.itemMap[requirement.templateId];
                        }

                        int itemAmountNeeded = (int)requirement.count;
                        int itemAmountInInventory = baseManager.baseInventory[itemID];

                        return itemAmountInInventory >= itemAmountNeeded;
                    }
                    else
                    {
                        return true;
                    }
                case "TraderLoyalty":
                    Mod.instance.LogInfo("\t\t\t\tTraderLoyalty");
                    int traderIndex = EFM_TraderStatus.IDToIndex(requirement.traderId);
                    return baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() >= requirement.loyaltyLevel;
                case "Skill":
                    Mod.instance.LogInfo("\t\t\t\tSkill");
                    float skillLevel = baseManager.data.skills[(int)requirement.skillIndex].progress / 100;
                    return skillLevel >= requirement.skillLevel;
                default:
                    return true;
            }
        }

        public bool GetRequirementsFullfilled(bool all, bool nextLevel, int requirementTypeIndex = 0)
        {
            Mod.instance.LogInfo("Get requs fulfilled called on area: "+areaIndex+" with all: "+all+", nextLevel: "+nextLevel+", and requirementTypeIndex: "+requirementTypeIndex);
            int levelToUse = level + (nextLevel ? 1 : 0);

            if (all)
            {
                Mod.instance.LogInfo("\tAll");
                // Check if there are requirements
                if (Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse] != null && Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse].requirements != null)
                {
                    Mod.instance.LogInfo("\t\tThere are requirements");
                    foreach (Requirement requirement in Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse].requirements)
                    {
                        Mod.instance.LogInfo("\t\t\tChecking requirement of type: "+requirement.type);
                        if (!GetRequirementFullfilled(requirement))
                        {
                            Mod.instance.LogInfo("\t\t\t\t\tRequirement not fulfilled, returning false");
                            return false;
                        }
                    }
                }
            }
            else if(Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse] != null && Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse].requirements != null)
            {
                foreach (Requirement requirement in Mod.areasDB.areaDefaults[areaIndex].stages[levelToUse].requirements)
                {
                    if(requirementTypeIndex == 0 && requirement.type.Equals("Area"))
                    {
                        if(baseManager.baseAreaManagers[requirement.areaType].level < requirement.requiredLevel)
                        {
                            return false;
                        }
                    }
                    else if (requirementTypeIndex == 1 && requirement.type.Equals("Item"))
                    {
                        if (Mod.itemMap.ContainsKey(requirement.templateId))
                        {
                            string itemID = "";
                            if (int.TryParse(Mod.itemMap[requirement.templateId], out int tryParseResult))
                            {
                                itemID = tryParseResult.ToString();
                            }
                            else // Is not a custom item
                            {
                                itemID = Mod.itemMap[requirement.templateId];
                            }

                            int itemAmountNeeded = (int)requirement.count;
                            int itemAmountInInventory = baseManager.baseInventory[itemID];

                            if(itemAmountInInventory < itemAmountNeeded)
                            {
                                return false;
                            }
                        }
                    }
                    else if (requirementTypeIndex == 2 && requirement.type.Equals("TraderLoyalty"))
                    {
                        int traderIndex = EFM_TraderStatus.IDToIndex(requirement.traderId);
                        if(baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() < requirement.loyaltyLevel)
                        {
                            return false;
                        }
                    }
                    else if (requirementTypeIndex == 3 && requirement.type.Equals("Skill"))
                    {
                        float skillLevel = baseManager.data.skills[(int)requirement.skillIndex].progress / 100;
                        if(skillLevel < requirement.skillLevel)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void OnSummaryClicked()
        {
            areaCanvas.transform.GetChild(0).gameObject.SetActive(false);
            areaCanvas.transform.GetChild(1).gameObject.SetActive(true);
            buttonClickSound.Play();
        }

        public void OnFullCloseClicked()
        {
            areaCanvas.transform.GetChild(1).gameObject.SetActive(false);
            areaCanvas.transform.GetChild(0).gameObject.SetActive(true);
            buttonClickSound.Play();
        }

        public void OnNextLevelClicked()
        {
            areaCanvas.transform.GetChild(1).GetChild(1).gameObject.SetActive(false); // Middle
            areaCanvas.transform.GetChild(1).GetChild(3).gameObject.SetActive(false); // Bottom
            areaCanvas.transform.GetChild(1).GetChild(2).gameObject.SetActive(true); // Middle2
            areaCanvas.transform.GetChild(1).GetChild(4).gameObject.SetActive(true); // Bottom2
            buttonClickSound.Play();
        }

        public void OnPreviousLevelClicked()
        {
            areaCanvas.transform.GetChild(1).GetChild(1).gameObject.SetActive(true); // Middle
            areaCanvas.transform.GetChild(1).GetChild(3).gameObject.SetActive(true); // Bottom
            areaCanvas.transform.GetChild(1).GetChild(2).gameObject.SetActive(false); // Middle2
            areaCanvas.transform.GetChild(1).GetChild(4).gameObject.SetActive(false); // Bottom2
            buttonClickSound.Play();
        }

        public void OnConstructClicked()
        {
            buttonClickSound.Play();

            // Remove items from inventory
            //TODO: Check for stackable items before removing, maybe we only want to remove part of the stack
            foreach (Requirement requirement in Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].requirements)
            {
                if (requirement.type.Equals("Item"))
                {
                    string actualID = Mod.itemMap[requirement.templateId];
                    baseManager.baseInventory[actualID] = baseManager.baseInventory[actualID] - (int)requirement.count;
                    List<GameObject> objectList = baseManager.baseInventoryObjects[actualID];
                    for (int i = objectList.Count - 1, j = (int)requirement.count; i >= 0 && j > 0; --i, --j)
                    {
                        GameObject toDestroy = objectList[objectList.Count - 1];
                        objectList.RemoveAt(objectList.Count - 1);
                        Destroy(toDestroy);
                    }
                }
            }

            if (Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].constructionTime == 0)
            {
                ++level;
                UpdateAreaState();
            }
            else
            {
                constructing = true;
                constructTime = baseManager.GetTimeSeconds();
                constructionTimer = Mod.areasDB.areaDefaults[areaIndex].stages[level + 1].constructionTime;
            }
        }
    }
}
