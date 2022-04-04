using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

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
        public List<List<GameObject>> areaRequirementMiddleParents;
        public List<List<GameObject>> areaRequirementMiddle2Parents;
        private bool inSummary = true;
        private float middleHeight = 50; // Init with 10 top padding, 20 bot padding, and 20 height for description
        private float middle2Height = 50;

        public void Init()
        {
            InitUI();

            init = true;
        }

        public void InitUI()
        {
            // Set area state based on loaded base and areasDB
            UpdateAreaState();
        }

        public void Update()
        {
            if (init)
            {
                UpdateProductions();

                UpdateConstruction();

                // Very demanding
                //UpdateUIRotation();
            }
        }

        private void UpdateUIRotation()
        {
            if (inSummary) // Look in all axes if in summary
            {
                areaCanvas.transform.rotation = Quaternion.LookRotation((areaCanvas.transform.position - GM.CurrentPlayerBody.Head.position).normalized);
            }
            else // Look only in x and Z if in full
            {
                Vector3 lookVector = areaCanvas.transform.position - GM.CurrentPlayerBody.Head.position;
                lookVector.y = 0;
                lookVector.Normalize();
                areaCanvas.transform.rotation = Quaternion.LookRotation(lookVector);
            }
        }

        private void UpdateProductions()
        {
            if (((bool)Mod.areasDB["areaDefaults"][areaIndex]["needsFuel"] && generatorRunning) || !(bool)Mod.areasDB["areaDefaults"][areaIndex]["needsFuel"] && productions != null && productions.Count > 0)
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
            // Check for preexisting areaCanvas
            if(areaCanvas != null)
            {
                Destroy(areaCanvas);
            }

            // Attach an area canvas to the area
            areaCanvas = Instantiate(EFM_Base_Manager.areaCanvasPrefab, transform.GetChild(transform.childCount - 2));

            // Set button click sound
            buttonClickSound = areaCanvas.transform.GetChild(3).GetComponent<AudioSource>();

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

            Mod.instance.LogInfo("UpdateAreaStateCalled on area: " + areaIndex);

            // Destroy any existing extra requirement parent in middle
            if(areaRequirementMiddleParents == null)
            {
                areaRequirementMiddleParents = new List<List<GameObject>>();
                for(int i=0; i < 4; ++i)
                {
                    areaRequirementMiddleParents.Add(new List<GameObject>());
                    areaRequirementMiddleParents[i].Add(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(i + 2).gameObject);
                }
            }
            foreach(List<GameObject> list in areaRequirementMiddleParents)
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

            // Do same for middle 2
            if(areaRequirementMiddle2Parents == null)
            {
                areaRequirementMiddle2Parents = new List<List<GameObject>>();
                for(int i=0; i < 4; ++i)
                {
                    areaRequirementMiddle2Parents.Add(new List<GameObject>());
                    areaRequirementMiddle2Parents[i].Add(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(i + 2).gameObject);
                }
            }
            foreach(List<GameObject> list in areaRequirementMiddle2Parents)
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
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), true, Mod.areasDB["areaDefaults"][areaIndex]["stages"][1]["bonuses"], "FUTURE BONUSES");
                    
                    // Full middle 2
                    // Nothing, when constructing the first level we show that description and future bonuses on middle

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.GetComponent<Collider>().enabled = false; // Disable button
                    bottomButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                    bottomButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

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
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).gameObject.SetActive(true); // Current level
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).GetComponent<Text>().text = "0" + level; // Current level

                    // Full top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true); // Upgrading icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.areaStatusIconUpgrading; // Status text
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Upgrading"; // Status text

                    // Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_"+level+"_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), true, Mod.areasDB["areaDefaults"][areaIndex]["stages"][level]["bonuses"], "CURRENT BONUSES");

                    // Full middle 2
                    areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(2), false, Mod.areasDB["areaDefaults"][areaIndex]["stages"][level + 1]["bonuses"], "FUTURE BONUSES");

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.transform.GetChild(1).GetComponent<Text>().text = "Level "+(level+1); // Next level bottom button
                    bottomButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);
                    bottomButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                    // Full bottom 2
                    Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                    GameObject bottom2BackButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                    bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                    bottom2BackButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                    GameObject bottom2UpgradeButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrading..."; // Upgrade bottom button
                    bottom2UpgradeButton.GetComponent<Collider>().enabled = false; // Disable upgrade button because curently upgrading
                    bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().color = Color.black; 
                    bottom2UpgradeButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                }
            }
            else // Not in construction or in upgrade process
            {
                Mod.instance.LogInfo("\tNot constructing");

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
                    if (level > 0 && (bool)Mod.areasDB["areaDefaults"][areaIndex]["needsFuel"])
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
                bool requirementsFullfilled = GetRequirementsFullfilled(true, true);
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
                    SetRequirements(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1), true, Mod.areasDB["areaDefaults"][areaIndex]["stages"][1]["requirements"]);
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), true, Mod.areasDB["areaDefaults"][areaIndex]["stages"][1]["bonuses"], "FUTURE BONUSES");
                    Mod.instance.LogWarning("MIDDLEHEIGHT = " + middleHeight);
                    if (middleHeight > 360)
                    {
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject.SetActive(true); // Only down should be activated at first
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<EFM_HoverScroll>().rate = 350 / (middleHeight - 350);
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<EFM_HoverScroll>().rate = 350 / (middleHeight - 350);
                    }

                    Mod.instance.LogInfo("\t\t\t0");
                    // Full middle 2
                    // There is no full middle 2 on a level 0 area which isn't in construction

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomConstructButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                    bottomConstructButton.transform.GetChild(1).GetComponent<Text>().text = "Construct"; // Construct bottom button
                    if (requirementsFullfilled)
                    {
                        bottomConstructButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);
                        bottomConstructButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                    }
                    else
                    {
                        bottomConstructButton.GetComponent<Collider>().enabled = false;
                        bottomConstructButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                    }

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
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).gameObject.SetActive(true); // Current level
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).GetComponent<Text>().text = "0"+level; // Current level

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
                        if ((bool)Mod.areasDB["areaDefaults"][areaIndex]["needsFuel"] && !generatorRunning)
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
                                Mod.instance.LogInfo("\t\t\t\t\t\tdoes not have production");
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = null; // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                            }
                        }
                    }

                    // Middle will show current level (no requirements) and middle 2 will show next level (with requirements), if there is a next level
                    //Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + level + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), true, Mod.areasDB["areaDefaults"][areaIndex]["stages"][level]["bonuses"], "CURRENT BONUSES");

                    // Full middle 2, and full bottom and bottom 2
                    if (Mod.areasDB["areaDefaults"][areaIndex].Value<JArray>("stages").Count >= level) // Check if we have a next level
                    {
                        Mod.instance.LogInfo("\t\thas a next level");
                        areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                        SetRequirements(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(1), false, Mod.areasDB["areaDefaults"][areaIndex]["stages"][level + 1]["requirements"]);
                        SetBonuses(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(2), false, Mod.areasDB["areaDefaults"][areaIndex]["stages"][level + 1]["bonuses"], "FUTURE BONUSES");
                        if (middle2Height > 350)
                        {
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject.SetActive(true);
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<EFM_HoverScroll>().rate = 350 / (middle2Height - 350);
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(3).GetComponent<EFM_HoverScroll>().rate = 350 / (middle2Height - 350);
                        }

                        Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                        GameObject bottomNextLevelButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom);
                        bottomNextLevelButton.transform.GetChild(1).GetComponent<Text>().text = "Level " + (level + 1); // Next level bottom button
                        bottomNextLevelButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);
                        bottomNextLevelButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                        Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                        GameObject bottom2BackButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                        bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                        bottom2BackButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                        GameObject bottom2UpgradeButton = Instantiate(EFM_Base_Manager.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrade"; // Upgrade bottom button
                        bottom2UpgradeButton.GetComponent<EFM_PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                        if (GetRequirementsFullfilled(true, true))
                        {
                            bottom2UpgradeButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                        }
                        else
                        {
                            bottom2UpgradeButton.GetComponent<Collider>().enabled = false;
                            bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                        }
                    }
                }
            }
        }

        public void SetRequirements(Transform parentRequirementsPanel, bool middle, JToken requirements)
        {
            Mod.instance.LogInfo("set requirements called with list of "+((JArray)requirements).Count+" requirements");
            if (requirements != null && ((JArray)requirements).Count > 0)
            {
                List<List<GameObject>> listToUse = middle ? areaRequirementMiddleParents : areaRequirementMiddle2Parents;
                parentRequirementsPanel.gameObject.SetActive(true); // Requirements panel
                if (middle)
                {
                    middleHeight += 96; // Content spacing + requirements + req top and bot padding
                }
                else
                {
                    middle2Height += 96; // Content spacing + requirements + req top and bot padding
                }

                foreach (JToken requirement in ((JArray)requirements))
                {
                    switch (requirement["type"].ToString())
                    {
                        case "Area":
                            Mod.instance.LogInfo("\tarea");
                            // Make a new area requirements parent is necessary to fit another area requirement
                            Transform areaRequirementParentToUse;
                            if (listToUse[0][listToUse[0].Count - 1].transform.childCount == 2)
                            {
                                areaRequirementParentToUse = Instantiate(EFM_Base_Manager.areaRequirementsPrefab, parentRequirementsPanel).transform;
                                listToUse[0].Add(areaRequirementParentToUse.gameObject); // Add the new parent to the corresponding list
                                if (middle)
                                {
                                    middleHeight += 226.3f; // Content spacing + requirements + req top and bot padding + req spacing + area req
                                }
                                else
                                {
                                    middle2Height += 226.3f; // Content spacing + requirements + req top and bot padding + req spacing + area req
                                }
                            }
                            else
                            {
                                areaRequirementParentToUse = listToUse[0][listToUse[0].Count - 1].transform;
                                if (areaRequirementParentToUse.childCount == 0 && // Only add the height if this will be the first req we add to this parent
                                    listToUse[0].Count - 1 == 0) // and if this is still the first parent because if we added a new parent weve added the height already
                                {
                                    if (middle)
                                    {
                                        middleHeight += 130.3f; // req spacing + area req
                                    }
                                    else
                                    {
                                        middle2Height += 130.3f; // req spacing + area req
                                    }
                                }
                            }
                            areaRequirementParentToUse.gameObject.SetActive(true);

                            GameObject areaRequirement = Instantiate(EFM_Base_Manager.areaRequirementPrefab, areaRequirementParentToUse);

                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = EFM_Base_Manager.areaIcons[(int)requirement["areaType"]]; // Area icon
                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(5).GetComponent<Text>().text = "0" + requirement["requiredLevel"].ToString(); // Area level
                            Text areaRequirementNameText = areaRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>();
                            areaRequirementNameText.text = Mod.localDB["interface"]["hideout_area_" + requirement["areaType"] + "_name"].ToString(); // Area name

                            Mod.instance.LogInfo("\t0");
                            // Check if requirement is met
                            if (baseManager.baseAreaManagers[(int)requirement["areaType"]].level >= (int)requirement["requiredLevel"])
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
                            // Make a new item requirements parent if necessary to fit another item requirement
                            Transform itemRequirementParentToUse;
                            if (listToUse[1][listToUse[1].Count - 1].transform.childCount == 5)
                            {
                                itemRequirementParentToUse = Instantiate(EFM_Base_Manager.itemRequirementsPrefab, parentRequirementsPanel).transform;
                                listToUse[1].Add(itemRequirementParentToUse.gameObject);
                                if (middle)
                                {
                                    middleHeight += 246.3f; // Content spacing + requirements + req top and bot padding + req spacing + item req
                                }
                                else
                                {
                                    middle2Height += 246.3f; // Content spacing + requirements + req top and bot padding + req spacing + item req
                                }
                            }
                            else
                            {
                                itemRequirementParentToUse = listToUse[1][listToUse[1].Count - 1].transform;
                                if (itemRequirementParentToUse.childCount == 0 && listToUse[1].Count - 1 == 0)
                                {
                                    if (middle)
                                    {
                                        middleHeight += 150.3f; // req spacing + item req
                                    }
                                    else
                                    {
                                        middle2Height += 150.3f; // req spacing + item req
                                    }
                                }
                            }
                            itemRequirementParentToUse.gameObject.SetActive(true);

                            Mod.instance.LogInfo("\t0");
                            GameObject itemRequirement = Instantiate(EFM_Base_Manager.itemRequirementPrefab, itemRequirementParentToUse);

                            string itemTemplateID = requirement["templateId"].ToString();
                            if (Mod.itemMap.ContainsKey(itemTemplateID))
                            {
                                Mod.instance.LogInfo("\t\t0");
                                if (Mod.itemIcons.ContainsKey(Mod.itemMap[itemTemplateID]))
                                {
                                    itemRequirement.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[Mod.itemMap[itemTemplateID]];
                                }
                                else
                                {
                                    Mod.instance.LogError("Area " + areaIndex + " requires item with mapped ID: " + Mod.itemMap[itemTemplateID] + " but this item does not have an icon!");
                                    // Output missing item icon
                                    if (Mod.instance.debug)
                                    {
                                        string text = "Area with index = " + areaIndex + " requirement missing icon for item with mapped ID: "+ Mod.itemMap[itemTemplateID] + "\n";
                                        File.AppendAllText("BepinEx/Plugins/EscapeFromMeatov/ErrorLog.txt", text);
                                    }
                                }
                                Mod.instance.LogInfo("\t\t0");

                                int itemAmountNeeded = (int)requirement["count"];
                                int itemAmountInInventory = 0;
                                Mod.instance.LogInfo("\t\t base manager null?: "+(baseManager == null));
                                Mod.instance.LogInfo("\t\t base inventory null?: "+(baseManager.baseInventory == null));
                                if (baseManager.baseInventory.ContainsKey(Mod.itemMap[itemTemplateID]))
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    itemAmountInInventory = baseManager.baseInventory[Mod.itemMap[itemTemplateID]];
                                }
                                Mod.instance.LogInfo("\t\t0");
                                itemRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = Mathf.Min(itemAmountNeeded, itemAmountInInventory).ToString() + "/" + itemAmountNeeded; // Area level

                                Mod.instance.LogInfo("\t\t0");
                                // Check if requirement is met
                                if (itemAmountInInventory >= itemAmountNeeded)
                                {
                                    Mod.instance.LogInfo("\t\t\t0");
                                    Mod.instance.LogInfo("\t\t\tSetting requirement to fullfilled with sprite null?: "+(EFM_Base_Manager.requirementFulfilled == null));
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementFulfilled;
                                }
                                else
                                {
                                    Mod.instance.LogInfo("\t\t\t1");
                                    Mod.instance.LogInfo("\t\t\tSetting requirement to locked with sprite null?: " + (EFM_Base_Manager.requirementLocked == null));
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = EFM_Base_Manager.requirementLocked;
                                    Mod.instance.LogInfo("\t\t\tSprite after setting null?: " + (itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite == null));
                                }
                                Mod.instance.LogInfo("\t\t0");
                            }
                            else
                            {
                                Mod.instance.LogError("Area " + areaIndex + " requires item with ID: " + requirement["templateId"].ToString() + " but this item is not in the item map!");
                            }
                            break;
                        case "TraderLoyalty":
                            Mod.instance.LogInfo("\ttrader");
                            // Make a new trader requirements parent if necessary to fit another trader requirement
                            Transform traderRequirementParentToUse;
                            Mod.instance.LogInfo("\t0");
                            if (listToUse[2][listToUse[2].Count - 1].transform.childCount == 2)
                            {
                                traderRequirementParentToUse = Instantiate(EFM_Base_Manager.traderRequirementsPrefab, parentRequirementsPanel).transform;
                                listToUse[2].Add(traderRequirementParentToUse.gameObject);
                                if (middle)
                                {
                                    middleHeight += 326.3f; // Content spacing + requirements + req top and bot padding + req spacing + trader req
                                }
                                else
                                {
                                    middle2Height += 326.3f; // Content spacing + requirements + req top and bot padding + req spacing + trader req
                                }
                            }
                            else
                            {
                                traderRequirementParentToUse = listToUse[2][listToUse[2].Count - 1].transform;
                                if (traderRequirementParentToUse.childCount == 0 && listToUse[2].Count - 1 == 0) // Only add the height if this will be the first req we add to this parent
                                {
                                    if (middle)
                                    {
                                        middleHeight += 230.3f; // req spacing + trader req
                                    }
                                    else
                                    {
                                        middle2Height += 230.3f; // req spacing + trader req
                                    }
                                }
                            }
                            Mod.instance.LogInfo("\t0");
                            traderRequirementParentToUse.gameObject.SetActive(true);

                            Mod.instance.LogInfo("\t0");
                            GameObject traderRequirement = Instantiate(EFM_Base_Manager.traderRequirementPrefab, traderRequirementParentToUse);

                            Mod.instance.LogInfo("\t0");
                            int traderIndex = EFM_TraderStatus.IDToIndex(requirement["traderId"].ToString());
                            Mod.instance.LogInfo("\t0");
                            traderRequirement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[traderIndex]; // Trader avatar
                            Mod.instance.LogInfo("\t0");
                            if ((int)requirement["loyaltyLevel"] == 4)
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                Mod.instance.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = String.Concat(Enumerable.Repeat("I", (int)requirement["loyaltyLevel"]));
                            }
                            Mod.instance.LogInfo("\t0");
                            // Check if requirement is met
                            Mod.instance.LogInfo("\t0");
                            if (baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() >= (int)requirement["loyaltyLevel"])
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
                            if (listToUse[3][listToUse[3].Count - 1].transform.childCount == 4)
                            {
                                skillRequirementParentToUse = Instantiate(EFM_Base_Manager.skillRequirementsPrefab, parentRequirementsPanel).transform;
                                listToUse[3].Add(skillRequirementParentToUse.gameObject);
                                if (middle)
                                {
                                    middleHeight += 285.3f; // Content spacing + requirements + req top and bot padding + req spacing + skill req
                                }
                                else
                                {
                                    middle2Height += 285.3f; // Content spacing + requirements + req top and bot padding + req spacing + skill req
                                }
                            }
                            else
                            {
                                skillRequirementParentToUse = listToUse[3][listToUse[3].Count - 1].transform;
                                if (skillRequirementParentToUse.childCount == 0 && listToUse[3].Count - 1 == 0) // Only add the height if this will be the first req we add to this parent
                                {
                                    if (middle)
                                    {
                                        middleHeight += 189.3f; // req spacing + skill req
                                    }
                                    else
                                    {
                                        middle2Height += 189.3f; // req spacing + skill req
                                    }
                                }
                            }
                            skillRequirementParentToUse.gameObject.SetActive(true);

                            GameObject skillRequirement = Instantiate(EFM_Base_Manager.skillRequirementPrefab, skillRequirementParentToUse);

                            skillRequirement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.skillIcons[(int)requirement["skillIndex"]]; // Skill icon
                            if ((int)requirement["skillIndex"] == 51)
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = requirement["skillIndex"].ToString();
                            }

                            // Check if requirement is met
                            float skillLevel = Mod.skills[(int)requirement["skillIndex"]].currentProgress / 100;
                            if (skillLevel >= (int)requirement["skillIndex"])
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

        public void SetBonuses(Transform parent, bool middle, JToken bonuses, string label)
        {
            if (bonuses != null && ((JArray)bonuses).Count > 0)
            {
                parent.gameObject.SetActive(true); // Bonuses
                parent.GetChild(0).GetComponent<Text>().text = label; // Bonuses label
                if (middle)
                {
                    middleHeight += 72; // Content spacing + bonuses
                }
                else
                {
                    middle2Height += 72; // Content spacing + bonuses
                }

                foreach (JToken areaBonus in bonuses)
                {
                    if (middle)
                    {
                        middleHeight += 27; // Bonuses spacing + bonus
                    }
                    else
                    {
                        middle2Height += 27; // Bonuses spacing + bonus
                    }
                    GameObject bonus = Instantiate(EFM_Base_Manager.bonusPrefab, parent);
                    Sprite bonusIcon = null;
                    if (areaBonus["icon"] != null && !areaBonus["icon"].ToString().Equals(""))
                    {
                        bonusIcon = EFM_Base_Manager.bonusIcons[areaBonus["icon"].ToString()];
                    }
                    else
                    {
                        bonusIcon = EFM_Base_Manager.bonusIcons[areaBonus["type"].ToString()];
                    }
                    bonus.transform.GetChild(0).GetComponent<Image>().sprite = bonusIcon; // Bonus icon
                    if (areaBonus["type"].ToString().Equals("TextBonus"))
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_" + areaBonus["id"]].ToString();
                    }
                    else
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localDB["interface"]["hideout_" + areaBonus["type"]].ToString(); // Bonus description
                    }
                    string bonusEffect = "";
                    int bonusValue = (int)areaBonus["value"];
                    switch (areaBonus["type"].ToString())
                    {
                        case "ExperienceRate":
                        case "QuestMoneyReward":
                        case "MaximumEnergyReserve":
                            bonusEffect = "+" + bonusValue + "%";
                            break;
                        case "EnergyRegeneration":
                            bonusEffect = "+" + bonusValue + " EP/hr";
                            break;
                        case "HealthRegeneration":
                            bonusEffect = "+" + bonusValue + " HP/hr";
                            break;
                        case "HydrationRegeneration":
                            bonusEffect = "+" + bonusValue + " WP/hr";
                            break;
                        case "FuelConsumption":
                        case "DebuffEndDelay":
                        case "ScavCooldownTimer":
                        case "InsuranceReturnTime":
                        case "RagfairCommission":
                            bonusEffect = "" + bonusValue + "%";
                            break;
                        case "AdditionalSlots":
                            bonusEffect = "+" + bonusValue;
                            break;
                        case "SkillGroupLevelingBoost":
                            bonusEffect = areaBonus["skillType"] + ", +" + bonusValue + "%";
                            break;
                        case "StashSize":
                            if (areaBonus["templateId"].ToString().Equals("566abbc34bdc2d92178b4576"))
                            {
                                bonusEffect = "A desk with a few shelves you found lying around";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce572459770cba1a34ea"))
                            {
                                bonusEffect = "Some wooden shelves";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce662459770f6f490f32"))
                            {
                                bonusEffect = "Some actual storage shelves";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce772459770e9e5f9532"))
                            {
                                bonusEffect = "More shelves";
                            }
                            break;
                    }
                    bonus.transform.GetChild(2).GetComponent<Text>().text = bonusEffect; // Bonus effect
                }
            }
        }

        public bool GetRequirementFullfilled(JToken requirement)
        {
            switch (requirement["type"].ToString())
            {
                case "Area":
                    Mod.instance.LogInfo("\t\t\t\tArea");
                    return baseManager.baseAreaManagers[(int)requirement["areaType"]].level >= (int)requirement["requiredLevel"];
                case "Item":
                    Mod.instance.LogInfo("\t\t\t\tItem: "+requirement["templateId"]+":");
                    string itemID = "";
                    if (Mod.itemMap.ContainsKey(requirement["templateId"].ToString()))
                    {
                        if (int.TryParse(Mod.itemMap[requirement["templateId"].ToString()], out int tryParseResult))
                        {
                            itemID = tryParseResult.ToString();
                        }
                        else // Is not a custom item
                        {
                            itemID = Mod.itemMap[requirement["templateId"].ToString()];
                        }

                        int itemAmountNeeded = (int)requirement["count"];
                        int itemAmountInInventory = baseManager.baseInventory.ContainsKey(itemID) ? baseManager.baseInventory[itemID] : 0;

                        return itemAmountInInventory >= itemAmountNeeded;
                    }
                    else
                    {
                        return true;
                    }
                case "TraderLoyalty":
                    Mod.instance.LogInfo("\t\t\t\tTraderLoyalty");
                    int traderIndex = EFM_TraderStatus.IDToIndex(requirement["traderId"].ToString());
                    return baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() >= (int)requirement["loyaltyLevel"];
                case "Skill":
                    Mod.instance.LogInfo("\t\t\t\tSkill");
                    float skillLevel = Mod.skills[(int)requirement["skillIndex"]].currentProgress / 100;
                    return skillLevel >= (int)requirement["skillLevel"];
                default:
                    return true;
            }
        }

        public bool GetRequirementsFullfilled(bool all, bool nextLevel, int requirementTypeIndex = 0)
        {
            Mod.instance.LogInfo("Get requs fulfilled called on area: "+areaIndex+" with all: "+all+", nextLevel: "+nextLevel+" ("+(level + 1)+"), and requirementTypeIndex: "+requirementTypeIndex);
            int levelToUse = level + (nextLevel ? 1 : 0);

            if (all)
            {
                Mod.instance.LogInfo("\tAll");
                // Check if there are requirements
                if (Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse] != null && Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse]["requirements"] != null)
                {
                    Mod.instance.LogInfo("\t\tThere are requirements");
                    foreach (JToken requirement in Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse]["requirements"])
                    {
                        Mod.instance.LogInfo("\t\t\tChecking requirement of type: "+requirement["type"]);
                        if (!GetRequirementFullfilled(requirement))
                        {
                            Mod.instance.LogInfo("\t\t\t\t\tRequirement not fulfilled, returning false");
                            return false;
                        }
                    }
                }
            }
            else if(Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse] != null && Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse]["requirements"] != null)
            {
                foreach (JToken requirement in Mod.areasDB["areaDefaults"][areaIndex]["stages"][levelToUse]["requirements"])
                {
                    if(requirementTypeIndex == 0 && requirement["type"].ToString().Equals("Area"))
                    {
                        if(baseManager.baseAreaManagers[(int)requirement["areaType"]].level < (int)requirement["requiredLevel"])
                        {
                            return false;
                        }
                    }
                    else if (requirementTypeIndex == 1 && requirement["type"].ToString().Equals("Item"))
                    {
                        if (Mod.itemMap.ContainsKey(requirement["templateId"].ToString()))
                        {
                            string itemID = "";
                            if (int.TryParse(Mod.itemMap[requirement["templateId"].ToString()], out int tryParseResult))
                            {
                                itemID = tryParseResult.ToString();
                            }
                            else // Is not a custom item
                            {
                                itemID = Mod.itemMap[requirement["templateId"].ToString()];
                            }

                            int itemAmountNeeded = (int)requirement["count"];
                            int itemAmountInInventory = baseManager.baseInventory[itemID];

                            if(itemAmountInInventory < itemAmountNeeded)
                            {
                                return false;
                            }
                        }
                    }
                    else if (requirementTypeIndex == 2 && requirement["type"].ToString().Equals("TraderLoyalty"))
                    {
                        int traderIndex = EFM_TraderStatus.IDToIndex(requirement["traderId"].ToString());
                        if(baseManager.traderStatuses[traderIndex].GetLoyaltyLevel() < (int)requirement["loyaltyLevel"])
                        {
                            return false;
                        }
                    }
                    else if (requirementTypeIndex == 3 && requirement["type"].ToString().Equals("Skill"))
                    {
                        float skillLevel = Mod.skills[(int)requirement["skillIndex"]].currentProgress / 100;
                        if(skillLevel < (int)requirement["skillLevel"])
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
            inSummary = false;
            buttonClickSound.Play();
        }

        public void OnFullCloseClicked()
        {
            areaCanvas.transform.GetChild(1).gameObject.SetActive(false);
            areaCanvas.transform.GetChild(0).gameObject.SetActive(true);
            inSummary = true;
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
            foreach (JToken requirement in Mod.areasDB["areaDefaults"][areaIndex]["stages"][level+1]["requirements"])
            {
                if (requirement["type"].ToString().Equals("Item"))
                {
                    string actualID = Mod.itemMap[requirement["templateId"].ToString()];
                    baseManager.baseInventory[actualID] = baseManager.baseInventory[actualID] - (int)requirement["count"];
                    List<GameObject> objectList = baseManager.baseInventoryObjects[actualID];
                    for (int i = objectList.Count - 1, j = (int)requirement["count"]; i >= 0 && j > 0; --i, --j)
                    {
                        GameObject toDestroy = objectList[objectList.Count - 1];
                        objectList.RemoveAt(objectList.Count - 1);
                        Destroy(toDestroy);
                    }
                }
            }

            if ((int)Mod.areasDB["areaDefaults"][areaIndex]["stages"][level + 1]["constructionTime"] == 0)
            {
                ++level;
                UpdateAreaState();
            }
            else
            {
                constructing = true;
                constructTime = baseManager.GetTimeSeconds();
                constructionTimer = (int)Mod.areasDB["areaDefaults"][areaIndex]["stages"][level + 1]["constructionTime"];
            }
        }
    }
}
