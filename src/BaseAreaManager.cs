using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class BaseAreaManager : MonoBehaviour
    {
        public static bool generatorRunning;

        public HideoutController baseManager;
        private bool init;

        public int areaIndex;
        public int level;
        public bool active; // whether the area is active (could be inactive if needs generator but generator not running, or needs slot filled but none are)
        public bool constructing;
        //public float constructTime; // What time the construction has started so we can check how long it has been since then and decide if it is done
        private bool slotsInit;
        public bool needsFuel;
        public float consumptionTimer;
        public GameObject[] slotItems; // Slots that could contain items, like generator that could have gas cans
        public List<List<AreaSlot>> slots;
        public Dictionary<string, Dictionary<string, AreaProduction>> productionsByItemIDByProdID; // Productions that can be activated on this area
        public Dictionary<string, AreaProduction> activeProductions = new Dictionary<string, AreaProduction>(); // Currently active productions
        public List<EFM_ScavCaseProduction> activeScavCaseProductions;
        public float constructionTimer; // How much time is actually left on the construction
        // TODO: Keep list of necessary elements that will need to be updated when items are sold/bought/thrown away/crafted/crafted with/used, when skills level up, when trader rep changes, when areas constructed or leveled up, when production timeleft updated
        // This will include area construction and upgrade reqs/crafting recipe availability counts and reqs
        public Dictionary<string, List<Transform>> produceViewByItemID; // Produce views dependent on item ID, the ones we need to update when count of item changes in inventory
        public Dictionary<string, List<Transform>> farmingViewByItemID; // Farming views dependent on item ID, the ones we need to update when count of item changes in inventory
        public Dictionary<string, List<Transform>> itemRequirementsByItemID; // Area item requirements dependent on item ID, the ones we need to update when count of item changes in inventory
        public Dictionary<int, List<Transform>> areaRequirementsByAreaIndex; // Area requirements dependent on area index, the ones we need to update when level of an area changes
        public List<AreaRequirement> itemRequirements;
        public List<AreaRequirement> areaRequirements;
        public List<AreaRequirement> skillRequirements;
        public List<AreaRequirement> traderRequirements;
        public List<AreaBonus> bonuses;
        public AreaUpgradeCheckProcessor[,] upgradeCheckProcessors;
        public GameObject[] upgradeDialogs = new GameObject[3]; // Block, warning, confirm
        public AudioSource[] productionAudioSourceByLevel = new AudioSource[22];
        public AudioSource[] slotAudioSourceByLevel = new AudioSource[22];
        public AudioSource[] generatorAudioSourceByLevel = new AudioSource[4];
        public AudioSource medStationFridgeAudioSource;
        public AudioSource[] kitchenFridgeAudioSourceByLevel = new AudioSource[4];
        public AudioSource[] kitchenPotAudioSourceByLevel = new AudioSource[4];
        public AudioSource[] restSpaceTVAudioSourceByLevel = new AudioSource[4];
        public AudioSource restSpacePSAudioSource;
        public AudioSource intelCenterPCAudioSource;
        public AudioSource intelCenterHDDAudioSource;
        public AudioSource AFUAudioSource;
        public AudioSource boozeGenAudioSource;
        public AudioSource[] bitcoinFarmAudioSourceByLevel = new AudioSource[4];

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
            needsFuel = (bool)Mod.areasDB[areaIndex]["needsFuel"];

            // Init area specific hierarchy stuff
            AreaSoundManager areaSoundManager;
            switch (areaIndex)
            {
                case 2:
                    Mod.LogInfo("\t2");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    break;
                case 4:
                    Mod.LogInfo("\t4");
                    slotAudioSourceByLevel[1] = transform.GetChild(1).GetChild(3).GetChild(3).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[2] = transform.GetChild(2).GetChild(3).GetChild(3).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[3] = transform.GetChild(3).GetChild(2).GetChild(4).GetComponent<AudioSource>();
                    generatorAudioSourceByLevel[1] = transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = generatorAudioSourceByLevel[1].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.generatorLevel1And2Audio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 5.5f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(5.5f, 6.5f);
                    generatorAudioSourceByLevel[2] = transform.GetChild(2).GetChild(3).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = generatorAudioSourceByLevel[2].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.generatorLevel1And2Audio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 5.5f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(5.5f, 6.5f);
                    generatorAudioSourceByLevel[3] = transform.GetChild(3).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = generatorAudioSourceByLevel[3].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.generatorLevel3Audio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 5.5f);
                    areaSoundManager.workingRange = new Vector2(5.5f, 19);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(19, 23.5f);
                    break;
                case 6:
                    Mod.LogInfo("\t6");
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(2).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    break;
                case 7:
                    Mod.LogInfo("\t7");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    medStationFridgeAudioSource = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    areaSoundManager = medStationFridgeAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.medStationLevel3Audio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 2.5f);
                    areaSoundManager.workingRange = new Vector2(2.5f, 8.5f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(8.5f, 10.8f);
                    break;
                case 8:
                    Mod.LogInfo("\t8");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(2).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(2).GetComponent<AudioSource>();
                    kitchenFridgeAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = kitchenFridgeAudioSourceByLevel[1].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.kitchenFridgeAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 12);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(12, 13.5f);
                    kitchenFridgeAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = kitchenFridgeAudioSourceByLevel[2].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.kitchenFridgeAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 12);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(12, 13.5f);
                    kitchenFridgeAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = kitchenFridgeAudioSourceByLevel[3].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.kitchenFridgeAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 12);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(12, 13.5f);
                    kitchenPotAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    areaSoundManager = kitchenPotAudioSourceByLevel[2].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.kitchenPotAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 18.1f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(18.1f, 22.5f);
                    kitchenPotAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    areaSoundManager = kitchenPotAudioSourceByLevel[3].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.kitchenPotAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 1);
                    areaSoundManager.workingRange = new Vector2(1, 18.1f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(18.1f, 22.5f);
                    break;
                case 9:
                    Mod.LogInfo("\t9");
                    restSpaceTVAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = restSpaceTVAudioSourceByLevel[2].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.restSpaceTracks[1];
                    areaSoundManager.workingRange = new Vector2(0, 330);
                    restSpaceTVAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = restSpaceTVAudioSourceByLevel[3].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.restSpaceTracks[1];
                    areaSoundManager.workingRange = new Vector2(0, 330);
                    restSpacePSAudioSource = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    areaSoundManager = restSpacePSAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.restSpacePSAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 13);
                    areaSoundManager.workingRange = new Vector2(13, 29);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(29, 32);
                    break;
                case 10:
                    Mod.LogInfo("\t10");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    break;
                case 11:
                    Mod.LogInfo("\t11");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(3).GetComponent<AudioSource>();
                    intelCenterPCAudioSource = transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = intelCenterPCAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.intelCenterPCAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 23);
                    areaSoundManager.workingRange = new Vector2(23, 32);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(32, 48);
                    intelCenterHDDAudioSource = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    areaSoundManager = intelCenterHDDAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.intelCenterHDDAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 9.5f);
                    areaSoundManager.workingRange = new Vector2(9.5f, 17.5f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(17.5f, 24);
                    break;
                case 17:
                    Mod.LogInfo("\t17");
                    slotAudioSourceByLevel[1] = transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    AFUAudioSource = transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = AFUAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.AFUAudio;
                    areaSoundManager.workingRange = new Vector2(0, 14.3f);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(14.3f, 18.5f);
                    break;
                case 19:
                    Mod.LogInfo("\t19");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    boozeGenAudioSource = transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = boozeGenAudioSource.gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.boozeGenAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 3.2f);
                    areaSoundManager.workingRange = new Vector2(3.2f, 12);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(12, 30);
                    break;
                case 20:
                    Mod.LogInfo("\t20");
                    productionAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(1).GetComponent<AudioSource>();
                    productionAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(1).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(2).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(2).GetComponent<AudioSource>();
                    slotAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(2).GetComponent<AudioSource>();
                    bitcoinFarmAudioSourceByLevel[1] = transform.GetChild(1).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = bitcoinFarmAudioSourceByLevel[1].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.bitcoinFarmAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 6.5f);
                    areaSoundManager.workingRange = new Vector2(6.5f, 23);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(23, 28);
                    bitcoinFarmAudioSourceByLevel[2] = transform.GetChild(2).GetChild(2).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = bitcoinFarmAudioSourceByLevel[2].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.bitcoinFarmAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 6.5f);
                    areaSoundManager.workingRange = new Vector2(6.5f, 23);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(23, 28);
                    bitcoinFarmAudioSourceByLevel[3] = transform.GetChild(3).GetChild(1).GetChild(0).GetComponent<AudioSource>();
                    areaSoundManager = bitcoinFarmAudioSourceByLevel[3].gameObject.AddComponent<AreaSoundManager>();
                    areaSoundManager.clip = HideoutController.bitcoinFarmAudio;
                    areaSoundManager.hasStart = true;
                    areaSoundManager.startRange = new Vector2(0, 6.5f);
                    areaSoundManager.workingRange = new Vector2(6.5f, 23);
                    areaSoundManager.hasEnd = true;
                    areaSoundManager.endRange = new Vector2(23, 28);
                    break;
            }

            InitUI();

            if (areaIndex == 4)
            {
                Switch generatorSwitch = transform.GetChild(1).GetChild(1).gameObject.AddComponent<Switch>();
                generatorSwitch = transform.GetChild(2).GetChild(1).gameObject.AddComponent<Switch>();
                generatorSwitch = transform.GetChild(3).GetChild(1).gameObject.AddComponent<Switch>();
            }

            if(areaIndex == 3)
            {
                if (level != 1)
                {
                    transform.GetChild(1).gameObject.SetActive(false);
                    transform.GetChild(level).gameObject.SetActive(true);
                }
            }
            else if(level != 0)
            {
                transform.GetChild(0).gameObject.SetActive(false);
                transform.GetChild(level).gameObject.SetActive(true);
                if (transform.GetChild(transform.childCount - 1).childCount > 0)
                {
                    transform.GetChild(transform.childCount - 1).GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(transform.childCount - 1).GetChild(level).gameObject.SetActive(true);
                }
            }

            // Init consumption based on save time, production timeleft and construction timer are set depending on it when loaded
            if (active)
            {
                int consumeAmount = 0;
                long secondsSinceSave = baseManager.GetTimeSeconds() - (long)HideoutController.loadedData["time"];
                switch (areaIndex)
                {
                    case 4: // Generator
                        consumptionTimer = 757.89f;
                        break;
                    case 6: // Water collector
                        consumptionTimer = 295.45f;
                        break;
                    case 17: // AFU
                        consumptionTimer = 211.76f;
                        break;
                    default:
                        break;
                }
                consumeAmount = (int)(secondsSinceSave / consumptionTimer);

                // Consume units from the first items in slots that have an amount for as long as we have an amount to consume
                for (int i = 0; i < slots[level].Count && consumeAmount > 0; ++i)
                {
                    //if (slots[level][i].CurObject != null)
                    //{
                    //    MeatovItemWrapper CIW = slots[level][i].CurObject.GetComponent<MeatovItemWrapper>();
                    //    if (CIW.amount > consumeAmount)
                    //    {
                    //        CIW.amount -= consumeAmount;
                    //        consumeAmount = 0;
                    //    }
                    //    else if (CIW.amount == consumeAmount)
                    //    {
                    //        CIW.amount = 0;
                    //        consumeAmount = 0;
                    //    }
                    //    else // CIW.amount < consumeAmount
                    //    {
                    //        CIW.amount = 0;
                    //        consumeAmount -= consumeAmount;
                    //    }

                    //    if(consumeAmount == 0)
                    //    {
                    //        break;
                    //    }
                    //}
                }

                if(consumeAmount > 0)
                {
                    active = false;
                    if (areaIndex == 4) // Generator
                    {
                        generatorRunning = false;

                        // Update based on fuel the areas that come before this one. The other ones were not intialized yet so no need to update
                        for (int j = 0; j < areaIndex; ++j) 
                        {
                            baseManager.baseAreaManagers[j].UpdateBasedOnFuel();
                        }
                    }
                }
            }

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

                UpdateConsumption();

                UpdateConstruction();
            }
        }

        private void UpdateProductions()
        {
            if (!needsFuel || generatorRunning)
            {
                if (activeProductions != null && activeProductions.Count != 0)
                {
                    List<string> completed = new List<string>();
                    foreach (KeyValuePair<string, AreaProduction> activeProduction in activeProductions)
                    {
                        // TODO: EFM_Skill.pointsPerHourCrafting / 3600 * Time.deltaTime would be way too small,
                        // should prob keep a counter and every minute or something we add the proper amount
                        Mod.AddSkillExp(Mathf.Max(Skill.pointsPerHourCrafting / 3600 * Time.deltaTime, 0.00001f), 50);
                        activeProduction.Value.timeLeft -= Time.deltaTime;
                        if (activeProduction.Value.timeLeft <= 0)
                        {
                            bool mustUpdateUI = true;
                            if (activeProduction.Value.continuous)
                            {
                                // Increment production count
                                ++activeProduction.Value.productionCount;

                                // Check if production is at limit
                                if(activeProduction.Value.productionLimitCount == activeProduction.Value.productionCount)
                                {
                                    // Disable production status
                                    activeProduction.Value.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(false);

                                    // Disable production
                                    activeProduction.Value.active = false;
                                    completed.Add(activeProduction.Key);
                                }
                                else // Production not at limit
                                {
                                    mustUpdateUI = false;

                                    // Reset timer
                                    activeProduction.Value.timeLeft = activeProduction.Value.productionTime - (activeProduction.Value.productionTime * (Skill.productionTimeReductionPerLevel * (Mod.skills[50].currentProgress/100) / 100));

                                    // Update timeLeft on production status
                                    int[] formattedTimeLeft = FormatTime(activeProduction.Value.timeLeft);
                                    activeProduction.Value.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                                }

                                // Update production count in UI
                                activeProduction.Value.transform.GetChild(1).GetChild(4).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = activeProduction.Value.productionCount.ToString() + "/" + activeProduction.Value.productionLimitCount;

                                // Enable Get Items button
                                activeProduction.Value.transform.GetChild(1).GetChild(5).GetChild(0).gameObject.SetActive(true);
                            }
                            else
                            {
                                // Disable production status and enable get items button
                                activeProduction.Value.transform.GetChild(1).GetChild(4).GetChild(2).gameObject.SetActive(false);
                                activeProduction.Value.transform.GetChild(1).GetChild(4).GetChild(1).gameObject.SetActive(true);

                                activeProduction.Value.active = false;
                                completed.Add(activeProduction.Key);
                            }

                            // Add skill points
                            Mod.AddSkillExp(Skill.skillPointsPerCraft, 51);
                            Mod.AddSkillExp(Skill.intellectSkillProgress, 8);

                            if (mustUpdateUI)
                            {
                                UpdateStateElements();
                            }

                            PlayProductionReadySound();
                        }
                        else
                        {
                            // Update timeLeft on production status
                            int[] formattedTimeLeft = FormatTime(activeProduction.Value.timeLeft);
                            if (activeProduction.Value.continuous)
                            {
                                activeProduction.Value.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                            }
                            else
                            {
                                activeProduction.Value.transform.GetChild(1).GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                            }
                        }
                    }
                    foreach (string ID in completed)
                    {
                        activeProductions.Remove(ID);
                    }
                }
                if(activeScavCaseProductions != null && activeScavCaseProductions.Count != 0)
                {
                    for(int i = activeScavCaseProductions.Count - 1; i >= 0; --i)
                    {
                        activeScavCaseProductions[i].timeLeft -= Time.deltaTime;
                        if (activeScavCaseProductions[i].timeLeft <= 0)
                        {
                            Transform outputVolume = transform.GetChild(transform.childCount - 3);
                            BoxCollider outputVolumeCollider = outputVolume.GetComponent<BoxCollider>();
                            foreach (KeyValuePair<Mod.ItemRarity, Vector2Int> productDef in activeScavCaseProductions[i].products)
                            {
                                int amount = UnityEngine.Random.Range(productDef.Value.x, productDef.Value.y);
                                for(int j = 0; j < amount; ++j)
                                {
                                    string itemID = Mod.itemsByRarity[productDef.Key][UnityEngine.Random.Range(0, Mod.itemsByRarity[productDef.Key].Count - 1)];
                                    if(int.TryParse(itemID, out int parseResult))
                                    {
                                        GameObject itemObject = Instantiate(Mod.itemPrefabs[parseResult], outputVolume.transform);
                                        itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-outputVolumeCollider.size.x / 2, outputVolumeCollider.size.x / 2),
                                                                                         UnityEngine.Random.Range(-outputVolumeCollider.size.y / 2, outputVolumeCollider.size.y / 2),
                                                                                         UnityEngine.Random.Range(-outputVolumeCollider.size.z / 2, outputVolumeCollider.size.z / 2));
                                        itemObject.transform.localRotation = UnityEngine.Random.rotation;

                                        MeatovItem CIW = itemObject.GetComponent<MeatovItem>();
                                        CIW.foundInRaid = true;
                                        BeginInteractionPatch.SetItemLocationIndex(1, CIW, null);
                                        if (CIW.maxAmount > 0)
                                        {
                                            CIW.amount = CIW.maxAmount;
                                        }
                                        if(CIW.itemType == Mod.ItemType.Money)
                                        {
                                            CIW.stack = UnityEngine.Random.Range(250, 2500);
                                        }
                                        if(CIW.itemType == Mod.ItemType.AmmoBox)
                                        {
                                            FVRFireArmMagazine asMagazine = CIW.physObj as FVRFireArmMagazine;
                                            for (int k = 0; k < CIW.maxAmount; ++k)
                                            {
                                                asMagazine.AddRound(CIW.roundClass, false, false);
                                            }
                                        }

                                        // Add to inventory
                                        HideoutController.instance.AddToBaseInventory(itemObject.transform, true);

                                        // Update all areas based on the item
                                        foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
                                        {
                                            baseAreaManager.UpdateBasedOnItem(itemID);
                                        }
                                    }
                                    else
                                    {
                                        AnvilManager.Run(SpawnVanillaItem(itemID, 1, outputVolumeCollider, "scav", null));
                                    }
                                }
                            }

                            PlayProductionReadySound();

                            activeScavCaseProductions.RemoveAt(i);
                        }
                    }
                }
            }
        }

        public void PlayProductionReadySound()
        {
            if(HideoutController.areaProductionSounds[areaIndex, 1] != null && productionAudioSourceByLevel[level] != null)
            {
                productionAudioSourceByLevel[level].PlayOneShot(HideoutController.areaProductionSounds[areaIndex, 1]);
            }
        }

        public void PlayProductionStartSound()
        {
            if(HideoutController.areaProductionSounds[areaIndex, 0] != null && productionAudioSourceByLevel[level] != null)
            {
                productionAudioSourceByLevel[level].PlayOneShot(HideoutController.areaProductionSounds[areaIndex, 0]);
            }
        }

        public void UpdateStateElements()
        {
            if (constructing)
            {
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Disable production panel
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(true); // Enable progress icon background

                // Top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon

                int[] formattedTimeLeft = FormatTime(constructionTimer);
                if (level == 0) 
                {
                    string constructionTimerString = String.Format("In construction ({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);

                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(true);
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false);
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = constructionTimerString;

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconConstructing;
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = constructionTimerString;
                }
                else
                {
                    string upgradingTimerString = String.Format("Upgrading ({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);

                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false);
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true);
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = upgradingTimerString;

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconUpgrading;
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = upgradingTimerString; // Status text
                }
            }
            else // Not construcing, now check if need fuel
            {
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false); // Disable progress icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Disable constructing icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Disable upgrading icon

                if (needsFuel)
                {
                    if (generatorRunning) // We need fuel but generator is running, so now check for production
                    {
                        // Check if currently producing and how many productions are done and waiting to be collected
                        int doneCount = 0;
                        int totalCount = 0;
                        foreach (KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in productionsByItemIDByProdID)
                        {
                            foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                            {
                                if (production.Value.active)
                                {
                                    ++totalCount;
                                }
                                else
                                {
                                    if (production.Value.productionCount > 0)
                                    {
                                        ++doneCount;
                                    }
                                }
                            }
                        }
                        if (totalCount > 0 && doneCount < totalCount)
                        {
                            // Summary
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Enable production panel
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true);
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = doneCount.ToString() + "/" + totalCount;

                            // Top
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing (" + doneCount + "/" + totalCount + ")"; // Status text
                        }
                        else
                        {
                            // Summary
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false);
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                            // Top
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                        }
                    }
                    else
                    {
                        // Summary
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Enable production panel
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                        // Top
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconOutOfFuel; // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                    }
                }
                else // Don't need fuel, so now check for production
                {
                    // Check if currently producing and how many productions are done and waiting to be collected
                    int doneCount = 0;
                    int totalCount = 0;
                    foreach (KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in productionsByItemIDByProdID)
                    {
                        foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                        {
                            if (production.Value.active)
                            {
                                ++totalCount;
                            }
                            else
                            {
                                if (production.Value.productionCount > 0)
                                {
                                    ++doneCount;
                                }
                            }
                        }
                    }
                    if (totalCount > 0 && doneCount < totalCount)
                    {
                        // Summary
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Enable production panel
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true);
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = doneCount.ToString() + "/" + totalCount;

                        // Top
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing (" + doneCount + "/" + totalCount + ")"; // Status text
                    }
                    else
                    {
                        // Summary
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false);
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                        // Top
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                    }
                }
            }
        }

        private void UpdateConsumption()
        {
            if (active)
            {
                bool consume = false;
                switch (areaIndex)
                {
                    case 4: // Generator
                        consumptionTimer -= Time.deltaTime;
                        if(consumptionTimer <= 0)
                        {
                            consume = true;
                            consumptionTimer = 757.89f
                                             + 757.89f * (Mod.skills[51].currentProgress/100) * Skill.consumptionReductionPerLevel 
                                             + 757.89f * (HideoutController.currentFuelConsumption + HideoutController.currentFuelConsumption * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100));
                        }
                        break;
                    case 6: // Water collector
                        consumptionTimer -= Time.deltaTime;
                        if (consumptionTimer <= 0)
                        {
                            consume = true;
                            consumptionTimer = 295.45f + 295.45f * (Mod.skills[51].currentProgress / 100) * Skill.consumptionReductionPerLevel;
                        }
                        break;
                    case 17: // AFU
                        consumptionTimer -= Time.deltaTime;
                        if (consumptionTimer <= 0)
                        {
                            consume = true;
                            consumptionTimer = 211.76f + 211.76f * (Mod.skills[51].currentProgress / 100) * Skill.consumptionReductionPerLevel;
                        }
                        break;
                    default:
                        break;
                }
                if (consume)
                {
                    // Consume a unit from the first item in slots that has an amount
                    for(int i=0; i < slots[level].Count; ++i)
                    {
                        //if (slots[level][i].CurObject != null)
                        //{
                        //    MeatovItemWrapper CIW = slots[level][i].CurObject.GetComponent<MeatovItemWrapper>();
                        //    if(CIW.amount > 0)
                        //    {
                        //        --CIW.amount;

                        //        switch (areaIndex)
                        //        {
                        //            case 4:
                        //                Mod.AddSkillExp(Skill.generatorPointsPerResourceSpent, 51);
                        //                if(baseManager.baseAreaManagers[18].level == 1)
                        //                {
                        //                    Mod.AddSkillExp(Skill.solarPowerPointsPerResourceSpent, 51);
                        //                }
                        //                break;
                        //            case 6:
                        //                Mod.AddSkillExp(Skill.waterCollectorPointsPerResourceSpent, 51);
                        //                break;
                        //            case 17:
                        //                Mod.AddSkillExp(Skill.AFUPointsPerResourceSpent, 51);
                        //                break;
                        //        }

                        //        // Make inactive if no other slots have item with amount in them
                        //        if(CIW.amount == 0)
                        //        {
                        //            bool foundAmount = false;
                        //            for (int j = 0; j < slots[level].Count; ++j)
                        //            {
                        //                if (slots[level][j].CurObject != null)
                        //                {
                        //                    MeatovItemWrapper innerCIW = slots[level][j].CurObject.GetComponent<MeatovItemWrapper>();
                        //                    if (CIW.amount > 0)
                        //                    {
                        //                        foundAmount = true;
                        //                        break;
                        //                    }
                        //                }
                        //            }
                        //            if (!foundAmount)
                        //            {
                        //                active = false;
                        //                if(areaIndex == 4) // Generator
                        //                {
                        //                    generatorRunning = false;

                        //                    foreach(BaseAreaManager areaManager in baseManager.baseAreaManagers)
                        //                    {
                        //                        areaManager.UpdateBasedOnFuel();
                        //                    }
                        //                }
                        //            }
                        //        }
                        //        break;
                        //    }
                        //}
                    }
                }
            }
        }

        public static void ToggleGenerator()
        {
            if (generatorRunning)
            {
                generatorRunning = false;

                foreach (BaseAreaManager areaManager in HideoutController.instance.baseAreaManagers)
                {
                    areaManager.UpdateBasedOnFuel();
                }
            }
            else
            {
                BaseAreaManager generatorManager = HideoutController.instance.baseAreaManagers[4];
                foreach (AreaSlot slot in generatorManager.slots[generatorManager.level])
                {
                    //if (slot.CurObject != null && slot.CurObject.GetComponent<MeatovItemWrapper>().amount > 0)
                    //{
                    //    generatorRunning = true;

                    //    foreach (BaseAreaManager areaManager in Mod.currentHideoutManager.baseAreaManagers)
                    //    {
                    //        areaManager.UpdateBasedOnFuel();
                    //    }
                    //    return;
                    //}
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

                    Upgrade();
                }
            }
        }
        
        public void UpdateAreaState()
        {
            // Check for preexisting areaCanvas
            if(areaCanvas != null)
            {
                areaCanvas.transform.parent = null;
                Destroy(areaCanvas);
                areaCanvas = null;
            }

            // Attach an area canvas to the area
            areaCanvas = Instantiate(HideoutController.areaCanvasPrefab, transform.GetChild(transform.childCount - 2));

            // Set full background pointable
            FVRPointable backgroundPointable = transform.GetChild(transform.childCount - 2).GetChild(0).GetChild(1).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 30;

            // Set button click sound
            buttonClickSound = areaCanvas.transform.GetChild(3).GetComponent<AudioSource>();

            // Full, Close button
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(1).GetComponent<Button>().onClick.AddListener(OnFullCloseClicked);

            // Set area canvas defaults
            string areaName = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_name"].ToString();

            // Area summary Icon
            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = HideoutController.areaIcons[areaIndex];

            // Area summary Name
            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = areaName;

            // Area summary elite background
            if (areaIndex == 13 || areaIndex == 14 || areaIndex == 17 || areaIndex == 18 || areaIndex == 19)
            {
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
            }

            // Area full Icon
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = HideoutController.areaIcons[areaIndex];

            // Area full Name
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = areaName;

            Mod.LogInfo("UpdateAreaStateCalled on area: " + areaIndex);

            // Destroy any existing extra requirement parent in middle
            if(areaRequirementMiddleParents == null)
            {
                areaRequirementMiddleParents = new List<List<GameObject>>();
            }
            else
            {
                areaRequirementMiddleParents.Clear();
            }
            for (int i = 0; i < 4; ++i)
            {
                areaRequirementMiddleParents.Add(new List<GameObject>());
                areaRequirementMiddleParents[i].Add(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2).GetChild(i + 2).gameObject);
            }

            // Do same for middle 2
            if(areaRequirementMiddle2Parents == null)
            {
                areaRequirementMiddle2Parents = new List<List<GameObject>>();
            }
            else
            {
                areaRequirementMiddle2Parents.Clear();
            }
            for (int i = 0; i < 4; ++i)
            {
                areaRequirementMiddle2Parents.Add(new List<GameObject>());
                areaRequirementMiddle2Parents[i].Add(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(i + 2).gameObject);
            }

            // Setup
            if (constructing)
            {
                Mod.LogInfo("\tConstructing");
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Icon background color
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(true); // In construction icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon

                // Full top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Icon background color
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(true); // In construction icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Locked icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Unlocked icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(false); // Ready to upgrade icon

                if (level == 0)
                {
                    Mod.LogInfo("\t\tLevel 0");
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(true); // Constructing icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Constructing"; // Status text

                    // Full top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(true); // Constructing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconConstructing; // Status text
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Constructing"; // Status text

                    // Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_"+areaIndex+"_stage_1_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(3), true, Mod.areasDB[areaIndex]["stages"]["1"]["bonuses"], "FUTURE BONUSES");
                    
                    // Full middle 2
                    // Nothing, when constructing the first level we show that description and future bonuses on middle

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.GetComponent<Collider>().enabled = false; // Disable button
                    bottomButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                    bottomButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                    bottomButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);

                    // Full bottom 2
                    // No access to bottom 2 when constructing
                }
                else
                {
                    Mod.LogInfo("\t\tLevel 1+");
                    // Summary 
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true); // Upgrading icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Upgrading"; // Status text
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).gameObject.SetActive(true); // Current level
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(4).GetComponent<Text>().text = "0" + level; // Current level

                    // Full top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(true); // Upgrading icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconUpgrading; // Status text
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Upgrading"; // Status text

                    // Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_stage_"+level+"_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(3), true, Mod.areasDB[areaIndex]["stages"][level.ToString()]["bonuses"], "CURRENT BONUSES");
                    SetProductions();

                    // Full middle 2
                    areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(3), false, Mod.areasDB[areaIndex]["stages"][(level + 1).ToString()]["bonuses"], "FUTURE BONUSES");

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom);
                    bottomButton.transform.GetChild(1).GetComponent<Text>().text = "Level "+(level+1); // Next level bottom button
                    bottomButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);
                    bottomButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                    // Full bottom 2
                    Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                    GameObject bottom2BackButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                    bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                    bottom2BackButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                    GameObject bottom2UpgradeButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom2);
                    bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrading..."; // Upgrade bottom button
                    bottom2UpgradeButton.GetComponent<Collider>().enabled = false; // Disable upgrade button because curently upgrading
                    bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                    bottom2UpgradeButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);
                    bottom2UpgradeButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                }

                if (generatorRunning)
                {
                    BeginGeneratorDependentAudio();
                }
            }
            else // Not in construction or in upgrade process
            {
                Mod.LogInfo("\tNot constructing");

                // Summary and Full top
                if (generatorRunning)
                {
                    Mod.LogInfo("\t\tgen running");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Out of fuel icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Out of fuel icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = null; // Status icon

                    if (activeProductions != null && activeProductions.Count > 0)
                    {
                        Mod.LogInfo("\t\t\tGot productions");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true); // Producing icon background
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true); // Producing icon background
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Producing icon

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon

                        if (activeProductions.Count > 1)
                        {
                            Mod.LogInfo("\t\t\t\tGot more");
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true); // Producing icon
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = activeProductions.Count.ToString(); // Production count
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true); // Producing icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = activeProductions.Count.ToString(); // Production count
                        }
                    }
                    else if (activeProductions != null && activeProductions.Count > 0)
                    {
                        Mod.LogInfo("\t\t\tno productions");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon

                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                    }

                    BeginGeneratorDependentAudio();
                }
                else
                {
                    Mod.LogInfo("\t\tgen not running");
                    if (level > 0 && (bool)Mod.areasDB[areaIndex]["needsFuel"])
                    {
                        Mod.LogInfo("\t\t\tbut needs fuel");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Out of fuel icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconOutOfFuel; // Status icon
                    }

                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false); // Producing icon background
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Producing icon

                    Mod.LogInfo("0");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false); // Producing icon
                }

                Mod.LogInfo("0");
                //Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false); // In construction icon background
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon

                Mod.LogInfo("0");
                // Full top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(false); // In construction icon background
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(10).gameObject.SetActive(false); // Constructing icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(11).gameObject.SetActive(false); // Upgrading icon

                Mod.LogInfo("0");
                bool requirementsFullfilled = GetRequirementsFullfilled(true, true);
                if (level == 0)
                {
                    Mod.LogInfo("\t\t\tlevel 0");
                    // Summary and full top
                    if (requirementsFullfilled)
                    {
                        Mod.LogInfo("\t\t\t\trequs fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundAvailableSprite; // Icon background color
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundAvailableSprite; // Icon background color
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconUnlocked; // Status text
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text
                    }
                    else
                    {
                        Mod.LogInfo("\t\t\t\trequs not fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundLockedSprite; // Icon background color
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Locked"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundLockedSprite; // Icon background color
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconLocked; // Status text
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
                    Mod.LogInfo("\t\t\t0");
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_stage_1_description"].ToString();
                    SetRequirements(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(2), true, Mod.areasDB[areaIndex]["stages"]["1"]["requirements"]);
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(3), true, Mod.areasDB[areaIndex]["stages"]["1"]["bonuses"], "FUTURE BONUSES");
                    SetProductions();
                    Mod.LogWarning("MIDDLEHEIGHT = " + middleHeight);
                    if (middleHeight > 360)
                    {
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject.SetActive(true); // Only down should be activated at first
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<HoverScroll>().rate = 350 / (middleHeight - 350);
                        areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<HoverScroll>().rate = 350 / (middleHeight - 350);
                    }

                    Mod.LogInfo("\t\t\t0");
                    // Full middle 2
                    // There is no full middle 2 on a level 0 area which isn't in construction

                    // Full bottom
                    Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                    GameObject bottomConstructButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom);
                    bottomConstructButton.transform.GetChild(1).GetComponent<Text>().text = "Construct"; // Construct bottom button
                    bottomConstructButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                    bottomConstructButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);
                    if(!requirementsFullfilled)
                    {
                        bottomConstructButton.GetComponent<Collider>().enabled = false;
                        bottomConstructButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                    }

                    // Full bottom 2
                    // There is no full bottom 2 on a level 0 area which isn't in construction
                }
                else
                {
                    Mod.LogInfo("\t\t\tlevel 1+");
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Icon background color
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Icon background color
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
                        Mod.LogInfo("\t\t\t\treqs fulfilled");
                        areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text

                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconReadyUpgrade; // Status icon
                        areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text
                    }
                    else
                    {
                        Mod.LogInfo("\t\t\t\treqs not fulfilled");
                        if ((bool)Mod.areasDB[areaIndex]["needsFuel"] && !generatorRunning)
                        {
                            Mod.LogInfo("\t\t\t\t\tneed fuel but gen not running");
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconOutOfFuel; // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                        }
                        else
                        {
                            Mod.LogInfo("\t\t\t\t\tnot need fuel or gen running");
                            if (activeProductions != null && activeProductions.Count > 0)
                            {
                                Mod.LogInfo("\t\t\t\t\t\thas production");
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
                            }
                            else
                            {
                                Mod.LogInfo("\t\t\t\t\t\tdoes not have production");
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = null; // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On standby"; // Status text
                            }
                        }
                    }

                    // Middle will show current level (no requirements) and middle 2 will show next level (with requirements), if there is a next level
                    //Full middle
                    areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_stage_" + level + "_description"].ToString();
                    SetBonuses(areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(3), true, Mod.areasDB[areaIndex]["stages"][level.ToString()]["bonuses"], "CURRENT BONUSES");
                    SetProductions();

                    // Full middle 2, and full bottom and bottom 2
                    Dictionary<string, JObject> stages = Mod.areasDB[areaIndex]["stages"].ToObject<Dictionary<string, JObject>>();
                    if (stages.Count >= level) // Check if we have a next level
                    {
                        Mod.LogInfo("\t\thas a next level");
                        areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_area_" + areaIndex + "_stage_" + (level + 1) + "_description"].ToString();
                        SetRequirements(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(1), false, Mod.areasDB[areaIndex]["stages"][(level + 1).ToString()]["requirements"]);
                        SetBonuses(areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(0).GetChild(2), false, Mod.areasDB[areaIndex]["stages"][(level + 1).ToString()]["bonuses"], "FUTURE BONUSES");
                        if (middle2Height > 350)
                        {
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject.SetActive(true);
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<HoverScroll>().rate = 350 / (middle2Height - 350);
                            areaCanvas.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(3).GetComponent<HoverScroll>().rate = 350 / (middle2Height - 350);
                        }

                        Transform bottom = areaCanvas.transform.GetChild(1).GetChild(3);
                        GameObject bottomNextLevelButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom);
                        bottomNextLevelButton.transform.GetChild(1).GetComponent<Text>().text = "Level " + (level + 1); // Next level bottom button
                        bottomNextLevelButton.GetComponent<Button>().onClick.AddListener(OnNextLevelClicked);
                        bottomNextLevelButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                        Transform bottom2 = areaCanvas.transform.GetChild(1).GetChild(4);
                        GameObject bottom2BackButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2BackButton.transform.GetChild(1).GetComponent<Text>().text = "Back"; // Back bottom button
                        bottom2BackButton.GetComponent<Button>().onClick.AddListener(OnPreviousLevelClicked);
                        bottom2BackButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();

                        GameObject bottom2UpgradeButton = Instantiate(HideoutController.areaCanvasBottomButtonPrefab, bottom2);
                        bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().text = "Upgrade"; // Upgrade bottom button
                        bottom2UpgradeButton.GetComponent<PointableButton>().hoverSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
                        bottom2UpgradeButton.GetComponent<Button>().onClick.AddListener(OnConstructClicked);
                        if(!GetRequirementsFullfilled(true, true))
                        {
                            bottom2UpgradeButton.GetComponent<Collider>().enabled = false;
                            bottom2UpgradeButton.transform.GetChild(1).GetComponent<Text>().color = Color.black;
                        }
                    }
                }
            }

            // Dialogs
            AudioSource hoverButtonSound = areaCanvas.transform.GetChild(2).GetComponent<AudioSource>();
            // Upgrade confirm
            upgradeDialogs[2] = areaCanvas.transform.GetChild(1).GetChild(7).gameObject;
            PointableButton upgradeConfirmCancelButton = upgradeDialogs[2].transform.GetChild(0).gameObject.AddComponent<PointableButton>();
            upgradeConfirmCancelButton.SetButton();
            upgradeConfirmCancelButton.MaxPointingRange = 30;
            upgradeConfirmCancelButton.hoverSound = hoverButtonSound;
            upgradeConfirmCancelButton.Button.onClick.AddListener(OnUpgradeCancelClicked);
            //upgradeConfirmCancelButton.GetComponent<Collider>().enabled = false;
            PointableButton upgradeConfirmConfirmButton = upgradeDialogs[2].transform.GetChild(1).gameObject.AddComponent<PointableButton>();
            upgradeConfirmConfirmButton.SetButton();
            upgradeConfirmConfirmButton.MaxPointingRange = 30;
            upgradeConfirmConfirmButton.hoverSound = hoverButtonSound;
            upgradeConfirmConfirmButton.Button.onClick.AddListener(OnUpgradeConfirmConfirmClicked);
            //upgradeConfirmConfirmButton.GetComponent<Collider>().enabled = false;
            // Upgrade warning
            upgradeDialogs[1] = areaCanvas.transform.GetChild(1).GetChild(8).gameObject;
            PointableButton upgradeWarningCancelButton = upgradeDialogs[1].transform.GetChild(0).gameObject.AddComponent<PointableButton>();
            upgradeWarningCancelButton.SetButton();
            upgradeWarningCancelButton.MaxPointingRange = 30;
            upgradeWarningCancelButton.hoverSound = hoverButtonSound;
            upgradeWarningCancelButton.Button.onClick.AddListener(OnUpgradeCancelClicked);
            //upgradeWarningCancelButton.GetComponent<Collider>().enabled = false;
            PointableButton upgradeWarningContinueButton = upgradeDialogs[1].transform.GetChild(1).gameObject.AddComponent<PointableButton>();
            upgradeWarningContinueButton.SetButton();
            upgradeWarningContinueButton.MaxPointingRange = 30;
            upgradeWarningContinueButton.hoverSound = hoverButtonSound;
            upgradeWarningContinueButton.Button.onClick.AddListener(OnUpgradeWarningContinueClicked);
            //upgradeWarningContinueButton.GetComponent<Collider>().enabled = false;
            // Upgrade blocked
            upgradeDialogs[0] = areaCanvas.transform.GetChild(1).GetChild(9).gameObject;
            PointableButton upgradeBlockedCancelButton = upgradeDialogs[0].transform.GetChild(0).gameObject.AddComponent<PointableButton>();
            upgradeBlockedCancelButton.SetButton();
            upgradeBlockedCancelButton.MaxPointingRange = 30;
            upgradeBlockedCancelButton.hoverSound = hoverButtonSound;
            upgradeBlockedCancelButton.Button.onClick.AddListener(OnUpgradeCancelClicked);
        }

        public void BeginGeneratorDependentAudio()
        {
            switch (areaIndex)
            {
                case 4:
                    if (generatorAudioSourceByLevel[level] != null)
                    {
                        generatorAudioSourceByLevel[level].GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 7:
                    if (level == 3)
                    {
                        medStationFridgeAudioSource.GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 8:
                    if (kitchenFridgeAudioSourceByLevel[level] != null)
                    {
                        kitchenFridgeAudioSourceByLevel[level].GetComponent<AreaSoundManager>().Begin();
                    }
                    if (kitchenPotAudioSourceByLevel[level] != null)
                    {
                        kitchenPotAudioSourceByLevel[level].GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 9:
                    if(restSpaceTVAudioSourceByLevel[level] != null)
                    {
                        restSpaceTVAudioSourceByLevel[level].GetComponent<AreaSoundManager>().Begin();
                    }
                    if(level == 3)
                    {
                        restSpacePSAudioSource.GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 11:
                    if(level == 3)
                    {
                        intelCenterPCAudioSource.GetComponent<AreaSoundManager>().Begin();
                        intelCenterHDDAudioSource.GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 17:
                    if (level == 1)
                    {
                        AFUAudioSource.GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 19:
                    if (level == 1)
                    {
                        boozeGenAudioSource.GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
                case 20:
                    if (bitcoinFarmAudioSourceByLevel[level] != null)
                    {
                        bitcoinFarmAudioSourceByLevel[level].GetComponent<AreaSoundManager>().Begin();
                    }
                    break;
            }
        }

        public void StopGeneratorDependentAudio()
        {
            switch (areaIndex)
            {
                case 4:
                    if (generatorAudioSourceByLevel[level] != null)
                    {
                        generatorAudioSourceByLevel[level].GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 7:
                    if (level == 3)
                    {
                        medStationFridgeAudioSource.GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 8:
                    if (kitchenFridgeAudioSourceByLevel[level] != null)
                    {
                        kitchenFridgeAudioSourceByLevel[level].GetComponent<AreaSoundManager>().End();
                    }
                    if (kitchenPotAudioSourceByLevel[level] != null)
                    {
                        kitchenPotAudioSourceByLevel[level].GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 9:
                    if(restSpaceTVAudioSourceByLevel[level] != null)
                    {
                        restSpaceTVAudioSourceByLevel[level].GetComponent<AreaSoundManager>().End();
                    }
                    if(level == 3)
                    {
                        restSpacePSAudioSource.GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 11:
                    if(level == 3)
                    {
                        intelCenterPCAudioSource.GetComponent<AreaSoundManager>().End();
                        intelCenterHDDAudioSource.GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 17:
                    if (level == 1)
                    {
                        AFUAudioSource.GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 19:
                    if (level == 1)
                    {
                        boozeGenAudioSource.GetComponent<AreaSoundManager>().End();
                    }
                    break;
                case 20:
                    if (bitcoinFarmAudioSourceByLevel[level] != null)
                    {
                        bitcoinFarmAudioSourceByLevel[level].GetComponent<AreaSoundManager>().End();
                    }
                    break;
            }
        }

        public void SetProductions()
        {
            Mod.LogInfo("SetProductions called on "+areaIndex+" at level "+level);
            // Reset productions
            if (this.productionsByItemIDByProdID == null)
            {
                this.productionsByItemIDByProdID = new Dictionary<string, Dictionary<string, AreaProduction>>();
            }
            else
            {
                foreach (KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in this.productionsByItemIDByProdID)
                {
                    foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                    {
                        Destroy(production.Value.gameObject);
                    }
                }
                this.productionsByItemIDByProdID.Clear();
            }
            if (farmingViewByItemID == null)
            {
                farmingViewByItemID = new Dictionary<string, List<Transform>>();
                produceViewByItemID = new Dictionary<string, List<Transform>>();
            }
            else
            {
                farmingViewByItemID.Clear();
                produceViewByItemID.Clear();
            }
            Mod.LogInfo("0");

            // Get all productions for this area including previous levels
            JArray productions = new JArray();
            for (int i = 0; i <= level; ++i)
            {
                foreach(JToken production in Mod.areasDB[areaIndex]["stages"][i.ToString()]["productions"] as JArray)
                {
                    productions.Add(production);
                }
            }

            Mod.LogInfo("0");
            // Init slots if necessary and set items in slotItems to their corresponding slots
            if (!slotsInit) 
            {
                if (Mod.areaSlots == null)
                {
                    Mod.areaSlots = new List<AreaSlot>();
                }
                slots = new List<List<AreaSlot>>();
                for (int slotsLevel = 0; slotsLevel < transform.GetChild(transform.childCount - 1).childCount; ++slotsLevel)
                {
                    slots.Add(new List<AreaSlot>());
                    Transform slotsParent = transform.GetChild(transform.childCount - 1).GetChild(slotsLevel);
                    for (int slotIndex = 0; slotIndex < slotsParent.childCount; ++slotIndex)
                    {
                        GameObject slotObject = slotsParent.GetChild(slotIndex).gameObject;
                        slotObject.tag = "QuickbeltSlot";
                        slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields

                        AreaSlot slotComponent = slotObject.AddComponent<AreaSlot>();
                        //slotComponent.areaIndex = areaIndex;
                        //slotComponent.areaLevel = level;
                        //slotComponent.slotIndex = slotIndex;
                        //slotComponent.QuickbeltRoot = slotObject.transform;
                        //slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                        //slotComponent.HoverGeo.SetActive(false);
                        //slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                        //slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear;
                        //slotComponent.RectBounds = slotObject.transform.GetChild(0);
                        //slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                        //slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
                        //if(areaIndex == 4) // Generator
                        //{
                        //    slotComponent.filter = new List<string>();
                        //    slotComponent.filter.Add("62");
                        //    slotComponent.filter.Add("63");
                        //}
                        //else if(areaIndex == 6) // Water collector
                        //{
                        //    slotComponent.filter = new List<string>();
                        //    slotComponent.filter.Add("101");
                        //}
                        //else if(areaIndex == 14) // Scav case
                        //{
                        //    slotComponent.filter = new List<string>();
                        //    slotComponent.filter.Add("181");
                        //    slotComponent.filter.Add("190");
                        //    slotComponent.filter.Add("203");
                        //}
                        //else if(areaIndex == 17) // AFU
                        //{
                        //    slotComponent.filter = new List<string>();
                        //    slotComponent.filter.Add("89");
                        //}
                        //else if(areaIndex == 20) // Bitcoin farm
                        //{
                        //    slotComponent.filter = new List<string>();
                        //    slotComponent.filter.Add("159");
                        //}

                        // Set slot sphere materials
                        slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                        slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;

                        // Reactivate slot
                        slotObject.SetActive(true);

                        Mod.areaSlots.Add(slotComponent);
                        slots[slotsLevel].Add(slotComponent);
                    }
                }
                slotsInit = true;
            }
            Mod.LogInfo("0");
            if (slotItems != null)
            {
                for (int slotItemIndex = 0; slotItemIndex < slotItems.Length; ++slotItemIndex)
                {
                    if (slotItems[slotItemIndex] != null)
                    {
                        FVRPhysicalObject slotItemPhysObj = slotItems[slotItemIndex].GetComponentInChildren<FVRPhysicalObject>();
                        //slotItemPhysObj.SetQuickBeltSlot(slots[level][slotItemIndex]);
                        //slotItemPhysObj.SetParentage(slots[level][slotItemIndex].QuickbeltRoot);
                    }
                }
            }
            Mod.LogInfo("0");

            // Farming view can be on bitcoin farm and water collector
            // Bitcoin doesn't use up any resources but it needs GPUs to functions, the amount of which affects the bitcoin mining rate
            // Water collector uses up 1 unit off a water filter every 295.45 seconds and produces superwater every 66 units
            Transform productionsParent = areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1);
            GameObject produceView = productionsParent.GetChild(0).gameObject;
            GameObject farmingView = productionsParent.GetChild(1).gameObject;
            JToken areaData = null;
            Mod.LogInfo("Loading production data from save");
            if (HideoutController.loadedData["areas"] != null)
            {
                Mod.LogInfo("Data has areas");
                areaData = HideoutController.loadedData["areas"][areaIndex];
                Mod.LogInfo("Got specific area");
            }
            JToken loadedProductions = null;
            if (areaData != null && areaData["productions"] != null)
            {
                Mod.LogInfo("Area has productions");
                loadedProductions = areaData["productions"];
            }
            else
            {
                Mod.LogInfo("Area does not have productions");
                loadedProductions = new JArray();
            }
            Dictionary<string, Dictionary<string, JToken>> productionsDictByItemIDByProdID = new Dictionary<string, Dictionary<string, JToken>>();
            Mod.LogInfo("Loading productions");
            foreach (JToken loadedProduction in loadedProductions)
            {
                string[] endProducts = null;
                ItemMapEntry itemMapEntry = Mod.itemMap[loadedProduction["endProduct"].ToString()];
                switch (itemMapEntry.mode)
                {
                    case 0:
                        endProducts = new string[] { itemMapEntry.ID };
                        break;
                    case 1:
                        endProducts = itemMapEntry.modulIDs;
                        break;
                    case 2:
                        endProducts = new string[] { itemMapEntry.otherModID };
                        break;
                }

                foreach(string endProduct in endProducts)
                {
                    if (productionsDictByItemIDByProdID.ContainsKey(loadedProduction["_id"].ToString()))
                    {
                        productionsDictByItemIDByProdID[loadedProduction["_id"].ToString()].Add(endProduct, loadedProduction);
                    }
                    else
                    {
                        Dictionary<string, JToken> newDict = new Dictionary<string, JToken>();
                        newDict.Add(endProduct, loadedProduction);
                        productionsDictByItemIDByProdID.Add(loadedProduction["_id"].ToString(), newDict);
                    }
                }
            }
            Mod.LogInfo("Done loading productions");

            Mod.LogInfo("0");
            if (productions.Count > 0)
            {
                Mod.LogInfo("Apparently, area index: " + areaIndex + " have " + productions.Count + " productions as level "+level);
                productionsParent.parent.gameObject.SetActive(true);

                middleHeight += 106.4f; // scroll view content spacing + production top padding + production bottom padding + header height + container spacing
            }
            bool firstProduction = true;
            Mod.LogInfo("0");
            long secondsSinceSave = HideoutController.loadedData["time"] != null ? baseManager.GetTimeSeconds() - (long)HideoutController.loadedData["time"] : 0;
            foreach (JObject production in productions)
            {
                Mod.LogInfo("1");
                AreaProduction productionScript = null;
                GameObject newFarmingView = null;
                GameObject newProduceView = null;
                if ((bool)production["continuous"])
                {
                    string[] endProducts = null;
                    ItemMapEntry itemMapEntry = Mod.itemMap[production["endProduct"].ToString()];
                    switch (itemMapEntry.mode)
                    {
                        case 0:
                            endProducts = new string[] { itemMapEntry.ID };
                            break;
                        case 1:
                            endProducts = itemMapEntry.modulIDs;
                            break;
                        case 2:
                            endProducts = new string[] { itemMapEntry.otherModID };
                            break;
                    }

                    foreach (string endProduct in endProducts)
                    {
                        Mod.LogInfo("2");
                        newFarmingView = Instantiate(farmingView, productionsParent);
                        middleHeight += 99.8f; // farming view height
                        if (!firstProduction)
                        {
                            middleHeight += 18; // spacing
                        }
                        productionScript = newFarmingView.AddComponent<AreaProduction>();

                        Mod.LogInfo("2");
                        productionScript.continuous = true;
                        productionScript.ID = production["_id"].ToString();
                        productionScript.productionTime = (float)production["productionTime"];
                        productionScript.endProduct = endProduct;
                        productionScript.count = (int)production["count"];
                        productionScript.productionLimitCount = (int)production["productionLimitCount"];

                        Mod.LogInfo("2");
                        // Init UI
                        if (Mod.itemIcons.ContainsKey(productionScript.endProduct))
                        {
                            newFarmingView.transform.GetChild(1).GetChild(4).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[productionScript.endProduct];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(productionScript.endProduct, newFarmingView.transform.GetChild(1).GetChild(4).GetChild(0).GetChild(2).GetComponent<Image>()));
                        }
                        newFarmingView.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnFarmingViewSetAllClick(productionScript.ID, endProduct); });
                        newFarmingView.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<Button>().onClick.AddListener(() => { OnFarmingViewSetOneClick(productionScript.ID, endProduct); });
                        newFarmingView.transform.GetChild(1).GetChild(1).GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnFarmingViewRemoveOneClick(productionScript.ID, endProduct); });
                        newFarmingView.transform.GetChild(1).GetChild(5).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnProductionGetItemsClick(productionScript.ID, endProduct); });

                        // Setup end product itemIcon
                        ItemIcon currentItemIconScript = newFarmingView.transform.GetChild(1).GetChild(4).GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                        currentItemIconScript.itemID = productionScript.endProduct;
                        currentItemIconScript.itemName = Mod.itemNames[productionScript.endProduct];
                        currentItemIconScript.description = Mod.itemDescriptions[productionScript.endProduct];
                        currentItemIconScript.weight = Mod.itemWeights[productionScript.endProduct];
                        currentItemIconScript.volume = Mod.itemVolumes[productionScript.endProduct];

                        Mod.LogInfo("2");
                        // Depending on save data
                        // If data about this production has been saved
                        if (productionsDictByItemIDByProdID.ContainsKey(productionScript.ID) && productionsDictByItemIDByProdID[productionScript.ID].ContainsKey(endProduct))
                        {
                            Mod.LogInfo("3");
                            activeProductions.Add(productionScript.ID, productionScript);

                            productionScript.active = true;
                            productionScript.timeLeft = (float)productionsDictByItemIDByProdID[productionScript.ID][endProduct]["timeLeft"] - secondsSinceSave;
                            productionScript.productionCount = (int)productionsDictByItemIDByProdID[productionScript.ID][endProduct]["productionCount"];

                            Mod.LogInfo("3");
                            // Set production count
                            newFarmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = productionScript.productionCount.ToString() + "/" + productionScript.productionLimitCount;

                            Mod.LogInfo("3");
                            // Set installed item count
                            int installedCount = GetFilledSlotCount();
                            newFarmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = installedCount.ToString();

                            Mod.LogInfo("3");
                            // It is possible we have an unclaimed amount of item
                            if (productionScript.productionCount > 0)
                            {
                                // Activate get items button
                                newFarmingView.transform.GetChild(1).GetChild(5).GetChild(0).gameObject.SetActive(true);
                            }

                            Mod.LogInfo("3");
                            // If have necessary items to produce and not at production limit yet
                            if (installedCount > 0 && productionScript.productionCount < productionScript.productionLimitCount)
                            {
                                // Activate production status
                                newFarmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);

                                // Also set initial string
                                bool needsFuel = (bool)Mod.areasDB[areaIndex]["needsFuel"];
                                if ((needsFuel && generatorRunning) || !needsFuel)
                                {
                                    // Production is in progress
                                    int[] formattedTimeLeft = FormatTime(productionScript.timeLeft);
                                    newFarmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                                }
                                else
                                {
                                    // Production is paused
                                    int[] formattedTimeLeft = FormatTime(productionScript.timeLeft);
                                    newFarmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                                }
                            }
                        }

                        // Fill requirements
                        productionScript.requirements = new List<AreaProductionRequirement>();
                        foreach (JObject requirement in production["requirements"])
                        {
                            Mod.LogInfo("2");
                            if (requirement["type"].ToString().Equals("Resource"))
                            {
                                Mod.LogInfo("3");
                                AreaProductionRequirement currentRequirement = new AreaProductionRequirement();
                                currentRequirement.resource = true;
                                ItemMapEntry requirementItemMapEntry = Mod.itemMap[requirement["templateId"].ToString()];
                                switch (requirementItemMapEntry.mode)
                                {
                                    case 0:
                                        currentRequirement.IDs = new string[] { requirementItemMapEntry.ID };
                                        break;
                                    case 1:
                                        currentRequirement.IDs = requirementItemMapEntry.modulIDs;
                                        break;
                                    case 2:
                                        currentRequirement.IDs = new string[] { requirementItemMapEntry.otherModID };
                                        break;
                                }
                                if (Mod.itemIcons.ContainsKey(currentRequirement.IDs[0]))
                                {
                                    Sprite icon = Mod.itemIcons[currentRequirement.IDs[0]];
                                    newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetComponent<Image>().sprite = icon;
                                    newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = icon;
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(currentRequirement.IDs[0], newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetComponent<Image>()));
                                    AnvilManager.Run(Mod.SetVanillaIcon(currentRequirement.IDs[0], newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>()));
                                }
                                currentRequirement.count = (int)requirement["resource"];

                                // Setup farm cost itemIcon
                                string itemName = Mod.itemNames[currentRequirement.IDs[0]];
                                string itemDescription = Mod.itemDescriptions[currentRequirement.IDs[0]];
                                int itemWeight = Mod.itemWeights[currentRequirement.IDs[0]];
                                int itemVolume = Mod.itemVolumes[currentRequirement.IDs[0]];
                                ItemIcon costItemIconScript = newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                costItemIconScript.itemID = currentRequirement.IDs[0];
                                costItemIconScript.itemName = itemName;
                                costItemIconScript.description = itemDescription;
                                costItemIconScript.weight = itemWeight;
                                costItemIconScript.volume = itemVolume;
                                ItemIcon costInstalledItemIconScript = newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                costInstalledItemIconScript.itemID = currentRequirement.IDs[0];
                                costInstalledItemIconScript.itemName = itemName;
                                costInstalledItemIconScript.description = itemDescription;
                                costInstalledItemIconScript.weight = itemWeight;
                                costInstalledItemIconScript.volume = itemVolume;

                                Mod.LogInfo("3");
                                productionScript.requirements.Add(currentRequirement);

                                Mod.LogInfo("3");
                                int amountInInventory = 0;
                                foreach (string possibleReqItem in currentRequirement.IDs)
                                {
                                    amountInInventory += (Mod.baseInventory.ContainsKey(possibleReqItem) ? Mod.baseInventory[possibleReqItem] : 0);
                                    amountInInventory += (Mod.playerInventory.ContainsKey(possibleReqItem) ? Mod.playerInventory[possibleReqItem] : 0);

                                    if (farmingViewByItemID.ContainsKey(possibleReqItem))
                                    {
                                        farmingViewByItemID[possibleReqItem].Add(newFarmingView.transform);
                                    }
                                    else
                                    {
                                        farmingViewByItemID.Add(possibleReqItem, new List<Transform>() { newFarmingView.transform });
                                    }
                                }
                                newFarmingView.transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = amountInInventory.ToString() + " (STASH)";
                                Mod.LogInfo("3");
                            }
                            else if (requirement["type"].ToString().Equals("Item"))
                            {
                                Mod.LogInfo("4");
                                AreaProductionRequirement currentRequirement = new AreaProductionRequirement();
                                ItemMapEntry requirementItemMapEntry = Mod.itemMap[requirement["templateId"].ToString()];
                                switch (requirementItemMapEntry.mode)
                                {
                                    case 0:
                                        currentRequirement.IDs = new string[] { requirementItemMapEntry.ID };
                                        break;
                                    case 1:
                                        currentRequirement.IDs = requirementItemMapEntry.modulIDs;
                                        break;
                                    case 2:
                                        currentRequirement.IDs = new string[] { requirementItemMapEntry.otherModID };
                                        break;
                                }
                                currentRequirement.count = (int)requirement["count"];
                                currentRequirement.isFunctional = (bool)requirement["isFunctional"];

                                Mod.LogInfo("4");
                                productionScript.requirements.Add(currentRequirement);

                                Mod.LogInfo("4");
                                int amountInInventory = 0;
                                foreach (string possibleReqItem in currentRequirement.IDs)
                                {
                                    amountInInventory += (Mod.baseInventory.ContainsKey(possibleReqItem) ? Mod.baseInventory[possibleReqItem] : 0);
                                    amountInInventory += (Mod.playerInventory.ContainsKey(possibleReqItem) ? Mod.playerInventory[possibleReqItem] : 0);
                                }
                                Mod.LogInfo("4");
                                if (newFarmingView != null)
                                {
                                    newFarmingView.transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = amountInInventory.ToString() + " (STASH)";

                                    if (Mod.itemIcons.ContainsKey(currentRequirement.IDs[0]))
                                    {
                                        Sprite icon = Mod.itemIcons[currentRequirement.IDs[0]];
                                        newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetComponent<Image>().sprite = icon;
                                        newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = icon;
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(currentRequirement.IDs[0], newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).GetComponent<Image>()));
                                        AnvilManager.Run(Mod.SetVanillaIcon(currentRequirement.IDs[0], newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>()));
                                    }

                                    foreach (string possibleReqItem in currentRequirement.IDs)
                                    {
                                        if (farmingViewByItemID.ContainsKey(possibleReqItem))
                                        {
                                            farmingViewByItemID[possibleReqItem].Add(newFarmingView.transform);
                                        }
                                        else
                                        {
                                            farmingViewByItemID.Add(possibleReqItem, new List<Transform>() { newFarmingView.transform });
                                        }
                                    }

                                    // Setup farm cost itemIcon
                                    string itemName = Mod.itemNames[currentRequirement.IDs[0]];
                                    string itemDescription = Mod.itemDescriptions[currentRequirement.IDs[0]];
                                    int itemWeight = Mod.itemWeights[currentRequirement.IDs[0]];
                                    int itemVolume = Mod.itemVolumes[currentRequirement.IDs[0]];
                                    ItemIcon costItemIconScript = newFarmingView.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                    costItemIconScript.itemID = currentRequirement.IDs[0];
                                    costItemIconScript.itemName = itemName;
                                    costItemIconScript.description = itemDescription;
                                    costItemIconScript.weight = itemWeight;
                                    costItemIconScript.volume = itemVolume;
                                    ItemIcon costInstalledItemIconScript = newFarmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                    costInstalledItemIconScript.itemID = currentRequirement.IDs[0];
                                    costInstalledItemIconScript.itemName = itemName;
                                    costInstalledItemIconScript.description = itemDescription;
                                    costInstalledItemIconScript.weight = itemWeight;
                                    costInstalledItemIconScript.volume = itemVolume;
                                }
                                else
                                {
                                    GameObject newRequirement = Instantiate(newProduceView.transform.GetChild(1).GetChild(0).gameObject, newProduceView.transform.GetChild(1));
                                    if (Mod.itemIcons.ContainsKey(currentRequirement.IDs[0]))
                                    {
                                        newRequirement.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currentRequirement.IDs[0]];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(currentRequirement.IDs[0], newRequirement.transform.GetChild(0).GetChild(2).GetComponent<Image>()));
                                    }
                                    newRequirement.transform.GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = Mathf.Min(amountInInventory, currentRequirement.count).ToString() + "/" + currentRequirement.count;
                                    if (amountInInventory > currentRequirement.count)
                                    {
                                        newRequirement.transform.GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false);
                                        newRequirement.transform.GetChild(1).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                    }

                                    foreach (string possibleReqItem in currentRequirement.IDs)
                                    {
                                        if (produceViewByItemID.ContainsKey(possibleReqItem))
                                        {
                                            produceViewByItemID[possibleReqItem].Add(newProduceView.transform);
                                        }
                                        else
                                        {
                                            produceViewByItemID.Add(possibleReqItem, new List<Transform>() { newProduceView.transform });
                                        }
                                    }

                                    // Setup production cost itemIcon
                                    ItemIcon costItemIconScript = newRequirement.transform.GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                    costItemIconScript.itemID = currentRequirement.IDs[0];
                                    costItemIconScript.itemName = Mod.itemNames[currentRequirement.IDs[0]];
                                    costItemIconScript.description = Mod.itemDescriptions[currentRequirement.IDs[0]];
                                    costItemIconScript.weight = Mod.itemWeights[currentRequirement.IDs[0]];
                                    costItemIconScript.volume = Mod.itemVolumes[currentRequirement.IDs[0]];
                                }
                            }
                        }
                        Mod.LogInfo("1");

                        // Add the production to the list
                        if (this.productionsByItemIDByProdID.ContainsKey(productionScript.ID))
                        {
                            this.productionsByItemIDByProdID[productionScript.ID].Add(endProduct, productionScript);
                        }
                        else
                        {
                            Dictionary<string, AreaProduction> newDict = new Dictionary<string, AreaProduction>();
                            newDict.Add(endProduct, productionScript);
                            this.productionsByItemIDByProdID.Add(productionScript.ID, newDict);
                        }

                        firstProduction = false;
                    }
                }
                else
                {
                    string[] endProducts = null;
                    ItemMapEntry itemMapEntry = Mod.itemMap[production["endProduct"].ToString()];
                    switch (itemMapEntry.mode)
                    {
                        case 0:
                            endProducts = new string[] { itemMapEntry.ID };
                            break;
                        case 1:
                            endProducts = itemMapEntry.modulIDs;
                            break;
                        case 2:
                            endProducts = new string[] { itemMapEntry.otherModID };
                            break;
                    }

                    foreach (string endProduct in endProducts)
                    {
                        Mod.LogInfo("4");
                        newProduceView = Instantiate(produceView, productionsParent);
                        middleHeight += 141.8f; // produce view height
                        if (!firstProduction)
                        {
                            middleHeight += 18; // spacing
                        }
                        productionScript = newProduceView.AddComponent<AreaProduction>();

                        Mod.LogInfo("4");
                        productionScript.continuous = true;
                        productionScript.ID = production["_id"].ToString();
                        productionScript.productionTime = (float)production["productionTime"];
                        productionScript.endProduct = endProduct;
                        productionScript.count = (int)production["count"];

                        Mod.LogInfo("4");
                        // Init UI
                        if (Mod.itemIcons.ContainsKey(productionScript.endProduct))
                        {
                            newProduceView.transform.GetChild(3).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[productionScript.endProduct];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(productionScript.endProduct, newProduceView.transform.GetChild(3).GetChild(0).GetChild(2).GetComponent<Image>()));
                        }
                        newProduceView.transform.GetChild(3).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = productionScript.count.ToString();
                        newProduceView.transform.GetChild(4).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnProduceViewStartClick(productionScript.ID, endProduct); });
                        newProduceView.transform.GetChild(4).GetChild(1).GetComponent<Button>().onClick.AddListener(() => { OnProductionGetItemsClick(productionScript.ID, endProduct); });

                        Mod.LogInfo("4");
                        // Depending on save data
                        // If data about this production has been saved
                        if (productionsDictByItemIDByProdID.ContainsKey(productionScript.ID) && productionsDictByItemIDByProdID[productionScript.ID].ContainsKey(endProduct))
                        {
                            Mod.LogInfo("5");
                            activeProductions.Add(productionScript.ID, productionScript);

                            productionScript.active = true;
                            productionScript.timeLeft = (float)productionsDictByItemIDByProdID[productionScript.ID][endProduct]["timeLeft"];

                            Mod.LogInfo("5");
                            // Deactivate start button because for sure we cant start production if already active
                            newProduceView.transform.GetChild(4).GetChild(0).gameObject.SetActive(false);

                            Mod.LogInfo("5");
                            // It is possible we have an unclaimed amount of items
                            if (productionScript.timeLeft <= 0)
                            {
                                // Activate get items button
                                newProduceView.transform.GetChild(4).GetChild(1).gameObject.SetActive(true);
                            }
                            else // productionScript.timeLeft > 0
                            {
                                // Activate production status
                                newProduceView.transform.GetChild(4).GetChild(2).gameObject.SetActive(true);

                                // Also set initial string
                                bool needsFuel = (bool)Mod.areasDB[areaIndex]["needsFuel"];
                                if ((needsFuel && generatorRunning) || !needsFuel)
                                {
                                    // Production is in progress
                                    int hours = (int)(productionScript.timeLeft / 3600);
                                    int minutes = (int)((productionScript.timeLeft % 3600) / 60);
                                    int seconds = (int)((productionScript.timeLeft % 3600) % 60);
                                    newProduceView.transform.GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", hours, minutes, seconds);
                                }
                                else
                                {
                                    // Production is paused
                                    int hours = (int)(productionScript.timeLeft / 3600);
                                    int minutes = (int)((productionScript.timeLeft % 3600) / 60);
                                    int seconds = (int)((productionScript.timeLeft % 3600) % 60);
                                    newProduceView.transform.GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})", hours, minutes, seconds);
                                }
                            }
                        }
                    }
                }
                Mod.LogInfo("1");
            }
            Mod.LogInfo("0");

            // Setup scav case view after because it is not specified in the area's production list
            if (areaIndex == 14)
            {
                GameObject scavCaseView = productionsParent.GetChild(2).gameObject;
                middleHeight += 75; // scav case view height
                scavCaseView.SetActive(true);

                // Init UI
                scavCaseView.transform.GetChild(4).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnScavCaseViewStartClick(); });

                // Depending on save data
                // If data about this production has been saved
                foreach(JToken scavCaseProduction in loadedProductions)
                {
                    EFM_ScavCaseProduction newScavCaseProduction = new EFM_ScavCaseProduction();
                    activeScavCaseProductions.Add(newScavCaseProduction);

                    newScavCaseProduction.timeLeft = (float)scavCaseProduction["timeLeft"];
                    newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
                    if (scavCaseProduction["products"]["common"] != null)
                    {
                        newScavCaseProduction.products.Add(Mod.ItemRarity.Common, new Vector2Int((int)scavCaseProduction["products"]["common"]["min"], (int)scavCaseProduction["products"]["common"]["max"]));
                    }
                    if (scavCaseProduction["products"]["rare"] != null)
                    {
                        newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int((int)scavCaseProduction["products"]["common"]["rare"], (int)scavCaseProduction["products"]["common"]["rare"]));
                    }
                    if (scavCaseProduction["products"]["superrare"] != null)
                    {
                        newScavCaseProduction.products.Add(Mod.ItemRarity.Superrare, new Vector2Int((int)scavCaseProduction["products"]["common"]["superrare"], (int)scavCaseProduction["products"]["common"]["superrare"]));
                    }
                }

                UpdateBasedOnSlots();
            }
        }

        public void SetRequirements(Transform parentRequirementsPanel, bool middle, JToken requirements)
        {
            if (Mod.requiredPerArea == null)
            {
                Mod.requiredPerArea = new Dictionary<string, int>[22];
            }
            if (Mod.requiredPerArea[areaIndex] == null)
            {
                Mod.requiredPerArea[areaIndex] = new Dictionary<string, int>();
            }
            else
            {
                Mod.requiredPerArea[areaIndex].Clear();
            }
            if(areaRequirements == null)
            {
                areaRequirements = new List<AreaRequirement>();
                itemRequirements = new List<AreaRequirement>();
                traderRequirements = new List<AreaRequirement>();
                skillRequirements = new List<AreaRequirement>();
            }
            else
            {
                areaRequirements.Clear();
                itemRequirements.Clear();
                traderRequirements.Clear();
                skillRequirements.Clear();
            }
            if (itemRequirementsByItemID == null)
            {
                itemRequirementsByItemID = new Dictionary<string, List<Transform>>();
            }
            else
            {
                itemRequirementsByItemID.Clear();
            }

            Mod.LogInfo("set requirements called with list of "+((JArray)requirements).Count+" requirements");
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
                            Mod.LogInfo("\tarea");
                            // Make a new area requirements parent is necessary to fit another area requirement
                            Transform areaRequirementParentToUse;
                            if (listToUse[0][listToUse[0].Count - 1].transform.childCount == 2)
                            {
                                areaRequirementParentToUse = Instantiate(HideoutController.areaRequirementsPrefab, parentRequirementsPanel).transform;
                                areaRequirementParentToUse.SetSiblingIndex(3);
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

                            GameObject areaRequirement = Instantiate(HideoutController.areaRequirementPrefab, areaRequirementParentToUse);
                            AreaRequirement areaRequirementScript = areaRequirement.AddComponent<AreaRequirement>();
                            areaRequirements.Add(areaRequirementScript);
                            //areaRequirementScript.requirementType = AreaRequirement.RequirementType.Area;
                            int requiredLevel = (int)requirement["requiredLevel"];
                            int requiredAreaIndex = (int)requirement["areaType"];
                            //areaRequirementScript.level = requiredLevel;
                            //areaRequirementScript.index = requiredAreaIndex;

                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(3).GetComponent<Image>().sprite = HideoutController.areaIcons[(int)requirement["areaType"]]; // Area icon
                            areaRequirement.transform.GetChild(0).GetChild(0).GetChild(5).GetComponent<Text>().text = "0" + requirement["requiredLevel"].ToString(); // Area level
                            Text areaRequirementNameText = areaRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>();
                            areaRequirementNameText.text = Mod.localeDB["interface"]["hideout_area_" + requirement["areaType"] + "_name"].ToString(); // Area name

                            if (areaRequirementsByAreaIndex == null)
                            {
                                areaRequirementsByAreaIndex = new Dictionary<int, List<Transform>>();
                            }
                            if (areaRequirementsByAreaIndex.ContainsKey(requiredAreaIndex))
                            {
                                areaRequirementsByAreaIndex[requiredAreaIndex].Add(areaRequirement.transform);
                            }
                            else
                            {
                                areaRequirementsByAreaIndex.Add(requiredAreaIndex, new List<Transform>() { areaRequirement.transform });
                            }

                            Mod.LogInfo("\t0");
                            // Check if requirement is met
                            if (baseManager.baseAreaManagers[(int)requirement["areaType"]].level >= requiredLevel)
                            {
                                Mod.LogInfo("\t1");
                                areaRequirementNameText.color = Color.white;
                                Mod.LogInfo("\t1");
                                areaRequirement.transform.GetChild(2).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                                Mod.LogInfo("\t1");
                            }
                            else
                            {
                                Mod.LogInfo("\t2");
                                areaRequirementNameText.color = new Color(1, 0.27f, 0.27f);
                                Mod.LogInfo("\t2");
                                areaRequirement.transform.GetChild(2).GetComponent<Image>().sprite = HideoutController.requirementLocked;
                                Mod.LogInfo("\t2");
                            }
                            break;
                        case "Item":
                            Mod.LogInfo("\titem");
                            // Make a new item requirements parent if necessary to fit another item requirement
                            Transform itemRequirementParentToUse;
                            if (listToUse[1][listToUse[1].Count - 1].transform.childCount == 5)
                            {
                                itemRequirementParentToUse = Instantiate(HideoutController.itemRequirementsPrefab, parentRequirementsPanel).transform;
                                itemRequirementParentToUse.SetSiblingIndex(4);
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

                            Mod.LogInfo("\t0");
                            GameObject itemRequirement = Instantiate(HideoutController.itemRequirementPrefab, itemRequirementParentToUse);
                            AreaRequirement itemRequirementScript = itemRequirement.AddComponent<AreaRequirement>();
                            //itemRequirementScript.requirementType = AreaRequirement.RequirementType.Item;
                            int itemAmountNeeded = (int)requirement["count"];
                            string itemTemplateID = requirement["templateId"].ToString();
                            //itemRequirementScript.count = itemAmountNeeded;
                            //itemRequirementScript.itemID = itemTemplateID;

                            if (Mod.itemMap.ContainsKey(itemTemplateID))
                            {
                                Mod.LogInfo("\t\t0");
                                string[] actualIDs = null;
                                ItemMapEntry itemMapEntry = Mod.itemMap[itemTemplateID];
                                switch (itemMapEntry.mode)
                                {
                                    case 0:
                                        actualIDs = new string[] { itemMapEntry.ID };
                                        break;
                                    case 1:
                                        actualIDs = itemMapEntry.modulIDs;
                                        break;
                                    case 2:
                                        actualIDs = new string[] { itemMapEntry.otherModID };
                                        break;
                                }
                                if (Mod.itemIcons.ContainsKey(actualIDs[0]))
                                {
                                    itemRequirement.transform.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[actualIDs[0]];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(actualIDs[0], itemRequirement.transform.GetChild(0).GetChild(2).GetComponent<Image>()));
                                }
                                Mod.LogInfo("\t\t0");

                                // Setup item req itemIcon
                                ItemIcon reqItemIconScript = itemRequirement.transform.GetChild(0).GetChild(2).gameObject.AddComponent<ItemIcon>();
                                reqItemIconScript.itemID = actualIDs[0];
                                reqItemIconScript.itemName = Mod.itemNames[actualIDs[0]];
                                reqItemIconScript.description = Mod.itemDescriptions[actualIDs[0]];
                                reqItemIconScript.weight = Mod.itemWeights[actualIDs[0]];
                                reqItemIconScript.volume = Mod.itemVolumes[actualIDs[0]];

                                int itemAmountInInventory = 0;
                                Mod.LogInfo("\t\t base manager null?: "+(baseManager == null));
                                Mod.LogInfo("\t\t base inventory null?: "+(Mod.baseInventory == null));
                                foreach (string itemReqID in actualIDs)
                                {
                                    if (Mod.baseInventory.ContainsKey(itemReqID))
                                    {
                                        Mod.LogInfo("\t\t\t0");
                                        itemAmountInInventory = Mod.baseInventory[itemReqID];
                                    }
                                    if (Mod.playerInventory.ContainsKey(itemReqID))
                                    {
                                        Mod.LogInfo("\t\t\t0");
                                        itemAmountInInventory += Mod.playerInventory[itemReqID];
                                    }

                                    if (Mod.requiredPerArea[areaIndex].ContainsKey(itemReqID))
                                    {
                                        Mod.requiredPerArea[areaIndex][itemReqID] = itemAmountNeeded;
                                    }
                                    else
                                    {
                                        Mod.requiredPerArea[areaIndex].Add(itemReqID, itemAmountNeeded);
                                    }

                                    if (itemRequirementsByItemID.ContainsKey(itemReqID))
                                    {
                                        itemRequirementsByItemID[itemReqID].Add(itemRequirement.transform);
                                    }
                                    else
                                    {
                                        itemRequirementsByItemID.Add(itemReqID, new List<Transform>() { itemRequirement.transform });
                                    }
                                }

                                Mod.LogInfo("\t\t0");
                                itemRequirement.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = Mathf.Min(itemAmountNeeded, itemAmountInInventory).ToString() + "/" + itemAmountNeeded; // Area level

                                Mod.LogInfo("\t\t0");
                                // Check if requirement is met
                                if (itemAmountInInventory >= itemAmountNeeded)
                                {
                                    Mod.LogInfo("\t\t\t0");
                                    Mod.LogInfo("\t\t\tSetting requirement to fullfilled with sprite null?: "+(HideoutController.requirementFulfilled == null));
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\t1");
                                    Mod.LogInfo("\t\t\tSetting requirement to locked with sprite null?: " + (HideoutController.requirementLocked == null));
                                    itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementLocked;
                                    Mod.LogInfo("\t\t\tSprite after setting null?: " + (itemRequirement.transform.GetChild(1).GetChild(1).GetComponent<Image>().sprite == null));
                                }
                                Mod.LogInfo("\t\t0");

                                itemRequirements.Add(itemRequirementScript);
                            }
                            else
                            {
                                Destroy(itemRequirement);
                                Mod.LogError("Area " + areaIndex + " requires item with ID: " + requirement["templateId"].ToString() + " but this item is not in the item map!");
                            }
                            break;
                        case "TraderLoyalty":
                            Mod.LogInfo("\ttrader");
                            // Make a new trader requirements parent if necessary to fit another trader requirement
                            Transform traderRequirementParentToUse;
                            Mod.LogInfo("\t0");
                            if (listToUse[2][listToUse[2].Count - 1].transform.childCount == 2)
                            {
                                traderRequirementParentToUse = Instantiate(HideoutController.traderRequirementsPrefab, parentRequirementsPanel).transform;
                                traderRequirementParentToUse.SetSiblingIndex(5);
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
                            Mod.LogInfo("\t0");
                            traderRequirementParentToUse.gameObject.SetActive(true);

                            Mod.LogInfo("\t0");
                            GameObject traderRequirement = Instantiate(HideoutController.traderRequirementPrefab, traderRequirementParentToUse);
                            AreaRequirement traderRequirementScript = traderRequirement.AddComponent<AreaRequirement>();
                            traderRequirements.Add(traderRequirementScript);
                            //traderRequirementScript.requirementType = AreaRequirement.RequirementType.Trader;
                            int traderRequiredLevel = (int)requirement["loyaltyLevel"];
                            int traderRequirementIndex = TraderStatus.IDToIndex(requirement["traderId"].ToString());
                            //traderRequirementScript.level = traderRequiredLevel;
                            //traderRequirementScript.index = traderRequirementIndex;

                            Mod.LogInfo("\t0");
                            Mod.LogInfo("\t0");
                            traderRequirement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[traderRequirementIndex]; // Trader avatar
                            Mod.LogInfo("\t0");
                            if (traderRequiredLevel == 4)
                            {
                                Mod.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                Mod.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                traderRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = TraderStatus.LoyaltyLevelToRoman((int)requirement["loyaltyLevel"]);
                            }
                            Mod.LogInfo("\t0");
                            // Check if requirement is met
                            Mod.LogInfo("\t0");
                            if (Mod.traderStatuses[traderRequirementIndex].GetLoyaltyLevel() >= traderRequiredLevel)
                            {
                                Mod.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                            }
                            else
                            {
                                Mod.LogInfo("\t\t0");
                                traderRequirement.transform.GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementLocked;
                            }
                            Mod.LogInfo("\t0");
                            break;
                        case "Skill":
                            Mod.LogInfo("\tskill");
                            // Make a new skill requirements parent if necessary to fit another skill requirement
                            Transform skillRequirementParentToUse;
                            if (listToUse[3][listToUse[3].Count - 1].transform.childCount == 4)
                            {
                                skillRequirementParentToUse = Instantiate(HideoutController.skillRequirementsPrefab, parentRequirementsPanel).transform;
                                skillRequirementParentToUse.SetAsLastSibling();
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

                            GameObject skillRequirement = Instantiate(HideoutController.skillRequirementPrefab, skillRequirementParentToUse);
                            AreaRequirement skillRequirementScript = skillRequirement.AddComponent<AreaRequirement>();
                            skillRequirements.Add(skillRequirementScript);
                            //skillRequirementScript.requirementType = AreaRequirement.RequirementType.Skill;
                            int skillRequiredLevel = (int)requirement["skillLevel"];
                            int skillRequirementIndex = Mod.SkillNameToIndex(requirement["skillName"].ToString());
                            //skillRequirementScript.level = skillRequiredLevel;
                            //skillRequirementScript.index = skillRequirementIndex;

                            skillRequirement.transform.GetChild(0).GetComponent<Image>().sprite = Mod.skillIcons[skillRequirementIndex]; // Skill icon
                            if (skillRequirementIndex == 51)
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            }
                            else // Use text instead of elite symbol
                            {
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                                skillRequirement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = requirement["skillLevel"].ToString();
                            }

                            // Check if requirement is met
                            float skillLevel = Mod.skills[skillRequirementIndex].currentProgress / 100;
                            if (skillLevel >= skillRequiredLevel)
                            {
                                skillRequirement.transform.GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                            }
                            else
                            {
                                skillRequirement.transform.GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementLocked;
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
            if(this.bonuses == null)
            {
                this.bonuses = new List<AreaBonus>();
            }
            else
            {
                this.bonuses.Clear();
            }

            // Add all bonuses to this.bonuses, including ones from previous levels
            JArray stagesData = Mod.areasDB[areaIndex]["stages"] as JArray;
            for(int i = 0; i <= level; ++i)
            {
                foreach (JToken bonusData in Mod.areasDB[areaIndex]["stages"][i.ToString()]["bonuses"])
                {
                    AreaBonus bonus = new AreaBonus();
                    bonus.bonusType = (AreaBonus.BonusType)Enum.Parse(typeof(AreaBonus.BonusType), bonusData["type"].ToString());
                    bonus.value = (float)bonusData["value"];
                    if(bonus.bonusType == AreaBonus.BonusType.SkillGroupLevelingBoost)
                    {
                        bonus.skillType = (Skill.SkillType)Enum.Parse(typeof(Skill.SkillType), bonusData["skillType"].ToString());
                    }
                }
            }

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
                    GameObject bonus = Instantiate(HideoutController.bonusPrefab, parent);
                    Sprite bonusIcon = null;
                    if (areaBonus["icon"] != null && !areaBonus["icon"].ToString().Equals(""))
                    {
                        string areaBonusIcon = areaBonus["icon"].ToString();
                        if (HideoutController.bonusIcons.ContainsKey(areaBonusIcon))
                        {
                            bonusIcon = HideoutController.bonusIcons[areaBonusIcon];
                        }
                        else
                        {
                            Mod.LogError("Missing bonusicon: " + areaBonusIcon + " for area: " + areaIndex);
                        }
                    }
                    else
                    {
                        string areaBonusIcon = areaBonus["type"].ToString();
                        if (HideoutController.bonusIcons.ContainsKey(areaBonusIcon))
                        {
                            bonusIcon = HideoutController.bonusIcons[areaBonus["type"].ToString()];
                        }
                        else
                        {
                            Mod.LogError("Missing bonusicon: " + areaBonusIcon + " for area: " + areaIndex);
                        }
                    }
                    bonus.transform.GetChild(0).GetComponent<Image>().sprite = bonusIcon; // Bonus icon
                    if (areaBonus["type"].ToString().Equals("TextBonus"))
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_" + areaBonus["id"]].ToString();
                    }
                    else
                    {
                        bonus.transform.GetChild(1).GetComponent<Text>().text = Mod.localeDB["interface"]["hideout_" + areaBonus["type"]].ToString(); // Bonus description
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
                                bonusEffect = "Some open crates and boxes you found lying around";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce572459770cba1a34ea"))
                            {
                                bonusEffect = "Some shelves and a gun safe";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce662459770f6f490f32"))
                            {
                                bonusEffect = "More shelves and space for you firearms";
                            }
                            else if (areaBonus["templateId"].ToString().Equals("5811ce772459770e9e5f9532"))
                            {
                                bonusEffect = "More shelves, gun safes/racks, and a convenient key cabinet";
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
                    Mod.LogInfo("\t\t\t\tArea");
                    return baseManager.baseAreaManagers[(int)requirement["areaType"]].level >= (int)requirement["requiredLevel"];
                case "Item":
                    Mod.LogInfo("\t\t\t\tItem: "+requirement["templateId"]+":");
                    if (Mod.itemMap.ContainsKey(requirement["templateId"].ToString()))
                    {
                        string[] itemIDs = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[requirement["templateId"].ToString()];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                itemIDs = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                itemIDs = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                itemIDs = new string[] { itemMapEntry.otherModID };
                                break;
                        }

                        int itemAmountNeeded = (int)requirement["count"];

                        // Have to check each item object because if it has CIW, if it is container or rig, can only count if it has no contents
                        int itemAmountInInventory = 0;

                        foreach (string itemID in itemIDs)
                        {
                            if (Mod.baseInventory.ContainsKey(itemID) && Mod.baseInventory[itemID] > 0)
                            {
                                foreach (GameObject obj in baseManager.baseInventoryObjects[itemID])
                                {
                                    MeatovItem CIW = obj.GetComponent<MeatovItem>();
                                    if (CIW != null)
                                    {
                                        if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                        {
                                            bool containsItem = false;
                                            foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                            {
                                                if (itemInSlot != null)
                                                {
                                                    containsItem = true;
                                                    break;
                                                }
                                            }

                                            if (!containsItem)
                                            {
                                                ++itemAmountInInventory;
                                            }
                                        }
                                        else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                        {
                                            if (CIW.containerItemRoot.childCount == 0)
                                            {
                                                ++itemAmountInInventory;
                                            }
                                        }
                                        else if (CIW.stack > 0)
                                        {
                                            itemAmountInInventory += CIW.stack;
                                        }
                                        else
                                        {
                                            ++itemAmountInInventory;
                                        }
                                    }
                                    else
                                    {
                                        ++itemAmountInInventory;
                                    }
                                }
                            }
                            if (Mod.playerInventory.ContainsKey(itemID) && Mod.playerInventory[itemID] > 0)
                            {
                                foreach (GameObject obj in Mod.playerInventoryObjects[itemID])
                                {
                                    MeatovItem CIW = obj.GetComponent<MeatovItem>();
                                    if (CIW != null)
                                    {
                                        if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                        {
                                            bool containsItem = false;
                                            foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                            {
                                                if (itemInSlot != null)
                                                {
                                                    containsItem = true;
                                                    break;
                                                }
                                            }

                                            if (!containsItem)
                                            {
                                                ++itemAmountInInventory;
                                            }
                                        }
                                        else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                        {
                                            if (CIW.containerItemRoot.childCount == 0)
                                            {
                                                ++itemAmountInInventory;
                                            }
                                        }
                                        else if (CIW.stack > 0)
                                        {
                                            itemAmountInInventory += CIW.stack;
                                        }
                                        else
                                        {
                                            ++itemAmountInInventory;
                                        }
                                    }
                                    else
                                    {
                                        ++itemAmountInInventory;
                                    }
                                }
                            }
                        }

                        return itemAmountInInventory >= itemAmountNeeded;
                    }
                    else
                    {
                        return true;
                    }
                case "TraderLoyalty":
                    Mod.LogInfo("\t\t\t\tTraderLoyalty");
                    int traderIndex = TraderStatus.IDToIndex(requirement["traderId"].ToString());
                    return Mod.traderStatuses[traderIndex].GetLoyaltyLevel() >= (int)requirement["loyaltyLevel"];
                case "Skill":
                    Mod.LogInfo("\t\t\t\tSkill");
                    float skillLevel = Mod.skills[Mod.SkillNameToIndex(requirement["skillName"].ToString())].currentProgress / 100;
                    return skillLevel >= (int)requirement["skillLevel"];
                default:
                    return true;
            }
        }

        //public bool GetRequirementFullfilled(AreaRequirement requirement)
        //{
        //    switch (requirement.requirementType)
        //    {
        //        case AreaRequirement.RequirementType.Area:
        //            return baseManager.baseAreaManagers[requirement.index].level >= requirement.level;
        //        case AreaRequirement.RequirementType.Item:
        //            string itemID = requirement.itemID;
        //            int itemAmountNeeded = requirement.count;

        //            // Have to check each item object because if it has CIW, if it is container or rig, can only count if it has no contents
        //            int itemAmountInInventory = 0;
        //            if (Mod.baseInventory.ContainsKey(itemID) && Mod.baseInventory[itemID] > 0)
        //            {
        //                foreach(GameObject obj in baseManager.baseInventoryObjects[itemID])
        //                {
        //                    MeatovItemWrapper CIW = obj.GetComponent<MeatovItemWrapper>();
        //                    if(CIW != null)
        //                    {
        //                        if(CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
        //                        {
        //                            bool containsItem = false;
        //                            foreach(GameObject itemInSlot in CIW.itemsInSlots)
        //                            {
        //                                if(itemInSlot != null)
        //                                {
        //                                    containsItem = true;
        //                                    break;
        //                                }
        //                            }

        //                            if (!containsItem)
        //                            {
        //                                ++itemAmountInInventory;
        //                            }
        //                        }
        //                        else if(CIW.itemType == Mod.ItemType.Backpack ||CIW.itemType == Mod.ItemType.Container ||CIW.itemType == Mod.ItemType.Pouch)
        //                        {
        //                            if (CIW.containerItemRoot.childCount == 0)
        //                            {
        //                                ++itemAmountInInventory;
        //                            }
        //                        }
        //                        else if(CIW.stack > 0)
        //                        {
        //                            itemAmountInInventory += CIW.stack;
        //                        }
        //                        else
        //                        {
        //                            ++itemAmountInInventory;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        ++itemAmountInInventory;
        //                    }
        //                }
        //            }
        //            if (Mod.playerInventory.ContainsKey(itemID) && Mod.playerInventory[itemID] > 0)
        //            {
        //                foreach (GameObject obj in Mod.playerInventoryObjects[itemID])
        //                {
        //                    MeatovItemWrapper CIW = obj.GetComponent<MeatovItemWrapper>();
        //                    if (CIW != null)
        //                    {
        //                        if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
        //                        {
        //                            bool containsItem = false;
        //                            foreach (GameObject itemInSlot in CIW.itemsInSlots)
        //                            {
        //                                if (itemInSlot != null)
        //                                {
        //                                    containsItem = true;
        //                                    break;
        //                                }
        //                            }

        //                            if (!containsItem)
        //                            {
        //                                ++itemAmountInInventory;
        //                            }
        //                        }
        //                        else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
        //                        {
        //                            if (CIW.containerItemRoot.childCount == 0)
        //                            {
        //                                ++itemAmountInInventory;
        //                            }
        //                        }
        //                        else if (CIW.stack > 0)
        //                        {
        //                            itemAmountInInventory += CIW.stack;
        //                        }
        //                        else
        //                        {
        //                            ++itemAmountInInventory;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        ++itemAmountInInventory;
        //                    }
        //                }
        //            }

        //            return itemAmountInInventory >= itemAmountNeeded;
        //        case AreaRequirement.RequirementType.Trader:
        //            return Mod.traderStatuses[requirement.index].GetLoyaltyLevel() >= requirement.level;
        //        case AreaRequirement.RequirementType.Skill:
        //            float skillLevel = Mod.skills[requirement.index].currentProgress / 100;
        //            return skillLevel >= requirement.level;
        //        default:
        //            return true;
        //    }
        //}

        public bool GetRequirementsFullfilled(bool all, bool nextLevel, int requirementTypeIndex = 0)
        {
            if (constructing)
            {
                return false;
            }

            Mod.LogInfo("Get requs fulfilled called on area: "+areaIndex+" with all: "+all+", nextLevel: "+nextLevel+" ("+(level + 1)+"), and requirementTypeIndex: "+requirementTypeIndex);
            int levelToUse = level + (nextLevel ? 1 : 0);

            if(nextLevel || areaRequirements == null)
            {
                if (all)
                {
                    Mod.LogInfo("\tAll");
                    // Check if there are requirements
                    if (Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()] != null && Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()]["requirements"] != null)
                    {
                        Mod.LogInfo("\t\tThere are requirements");
                        foreach (JToken requirement in Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()]["requirements"])
                        {
                            Mod.LogInfo("\t\t\tChecking requirement of type: " + requirement["type"]);
                            if (!GetRequirementFullfilled(requirement))
                            {
                                Mod.LogInfo("\t\t\t\t\tRequirement not fulfilled, returning false");
                                return false;
                            }
                        }
                    }
                }
                else if (Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()] != null && Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()]["requirements"] != null)
                {
                    foreach (JToken requirement in Mod.areasDB[areaIndex]["stages"][levelToUse.ToString()]["requirements"])
                    {
                        if (requirementTypeIndex == 0 && requirement["type"].ToString().Equals("Area"))
                        {
                            if (baseManager.baseAreaManagers[(int)requirement["areaType"]].level < (int)requirement["requiredLevel"])
                            {
                                return false;
                            }
                        }
                        else if (requirementTypeIndex == 1 && requirement["type"].ToString().Equals("Item"))
                        {
                            if (Mod.itemMap.ContainsKey(requirement["templateId"].ToString()))
                            {
                                string[] itemIDs = null;
                                ItemMapEntry itemMapEntry = Mod.itemMap[requirement["templateId"].ToString()];
                                switch (itemMapEntry.mode)
                                {
                                    case 0:
                                        itemIDs = new string[] { itemMapEntry.ID };
                                        break;
                                    case 1:
                                        itemIDs = itemMapEntry.modulIDs;
                                        break;
                                    case 2:
                                        itemIDs = new string[] { itemMapEntry.otherModID };
                                        break;
                                }

                                int itemAmountNeeded = (int)requirement["count"];
                                int itemAmountInInventory = 0;
                                foreach(string itemID in itemIDs)
                                {
                                    itemAmountInInventory += Mod.baseInventory.ContainsKey(itemID) ? Mod.baseInventory[itemID] : 0;
                                    itemAmountInInventory += Mod.playerInventory.ContainsKey(itemID) ? Mod.playerInventory[itemID] : 0;
                                }

                                if (itemAmountInInventory < itemAmountNeeded)
                                {
                                    return false;
                                }
                            }
                        }
                        else if (requirementTypeIndex == 2 && requirement["type"].ToString().Equals("TraderLoyalty"))
                        {
                            int traderIndex = TraderStatus.IDToIndex(requirement["traderId"].ToString());
                            if (Mod.traderStatuses[traderIndex].GetLoyaltyLevel() < (int)requirement["loyaltyLevel"])
                            {
                                return false;
                            }
                        }
                        else if (requirementTypeIndex == 3 && requirement["type"].ToString().Equals("Skill"))
                        {
                            float skillLevel = Mod.skills[Mod.SkillNameToIndex(requirement["skillName"].ToString())].currentProgress / 100;
                            if (skillLevel < (int)requirement["skillLevel"])
                            {
                                return false;
                            }
                        }
                    }
                }
            }
            else
            {
                if (all)
                {
                    //foreach(AreaRequirement requirement in areaRequirements)
                    //{
                    //    if (!GetRequirementFullfilled(requirement))
                    //    {
                    //        return false;
                    //    }
                    //}
                    //foreach(AreaRequirement requirement in itemRequirements)
                    //{
                    //    if (!GetRequirementFullfilled(requirement))
                    //    {
                    //        return false;
                    //    }
                    //}
                    //foreach(AreaRequirement requirement in traderRequirements)
                    //{
                    //    if (!GetRequirementFullfilled(requirement))
                    //    {
                    //        return false;
                    //    }
                    //}
                    //foreach(AreaRequirement requirement in skillRequirements)
                    //{
                    //    if (!GetRequirementFullfilled(requirement))
                    //    {
                    //        return false;
                    //    }
                    //}
                }
                else
                {
                    //switch (requirementTypeIndex)
                    //{
                    //    case 0:
                    //        foreach (AreaRequirement requirement in areaRequirements)
                    //        {
                    //            if (!GetRequirementFullfilled(requirement))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        break;
                    //    case 1:
                    //        foreach (AreaRequirement requirement in itemRequirements)
                    //        {
                    //            if (!GetRequirementFullfilled(requirement))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        break;
                    //    case 2:
                    //        foreach (AreaRequirement requirement in traderRequirements)
                    //        {
                    //            if (!GetRequirementFullfilled(requirement))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        break;
                    //    case 3:
                    //        foreach (AreaRequirement requirement in skillRequirements)
                    //        {
                    //            if (!GetRequirementFullfilled(requirement))
                    //            {
                    //                return false;
                    //            }
                    //        }
                    //        break;
                    //    default:
                    //        return false;
                    //}
                }
            }

            return true;
        }

        public void OnFullCloseClicked()
        {
            areaCanvas.transform.GetChild(1).gameObject.SetActive(false);
            areaCanvas.transform.GetChild(0).gameObject.SetActive(true);
            inSummary = true;
            buttonClickSound.Play();

            // Disable upgrade check processors if active
            upgradeCheckProcessors[level, 0].gameObject.SetActive(false);
            upgradeCheckProcessors[level, 1].gameObject.SetActive(false);
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

            // Activate upgrade confirm dialog
            upgradeDialogs[2].SetActive(true);

            // Activate upgrade check processors
            upgradeCheckProcessors[level, 0].gameObject.SetActive(true);
            upgradeCheckProcessors[level, 1].gameObject.SetActive(true);
        }
        
        public void OnUpgradeConfirmConfirmClicked()
        {
            buttonClickSound.Play();

            // Remove items from inventory
            foreach (JToken requirement in Mod.areasDB[areaIndex]["stages"][(level+1).ToString()]["requirements"])
            {
                if (requirement["type"].ToString().Equals("Item"))
                {
                    string[] actualIDs = null;
                    ItemMapEntry itemMapEntry = Mod.itemMap[requirement["templateId"].ToString()];
                    switch (itemMapEntry.mode)
                    {
                        case 0:
                            actualIDs = new string[] { itemMapEntry.ID };
                            break;
                        case 1:
                            actualIDs = itemMapEntry.modulIDs;
                            break;
                        case 2:
                            actualIDs = new string[] { itemMapEntry.otherModID };
                            break;
                    }
                    int amountToRemove = (int)requirement["count"];
                    int amountToRemoveFromBase = 0;
                    int amountToRemoveFromPlayer = 0;
                    int amountInBaseInventory = 0;
                    foreach(string actualID in actualIDs)
                    {
                        if (Mod.baseInventory.ContainsKey(actualID))
                        {
                            amountInBaseInventory += Mod.baseInventory[actualID];
                        }
                    }
                    if(amountInBaseInventory >= amountToRemove)
                    {
                        amountToRemoveFromBase = amountToRemove;
                    }
                    else
                    {
                        amountToRemoveFromBase = amountInBaseInventory;
                        amountToRemoveFromPlayer = amountToRemove - amountInBaseInventory;
                    }
                    List<string> actualIDsUsed = new List<string>();
                    int actualIDIndex = 0;
                    int amountLeftToRemoveFromBase = amountToRemoveFromBase;
                    while (amountLeftToRemoveFromBase > 0)
                    {
                        string actualID = actualIDs[actualIDIndex++];
                        if (!Mod.baseInventory.ContainsKey(actualID))
                        {
                            continue;
                        }
                        else
                        {
                            actualIDsUsed.Add(actualID);
                        }
                        List<GameObject> objectList = baseManager.baseInventoryObjects[actualID];
                        for (int i = objectList.Count - 1; i >= 0 && amountLeftToRemoveFromBase > 0; --i)
                        {
                            GameObject toCheck = objectList[objectList.Count - 1];
                            MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                            VanillaItemDescriptor VID = toCheck.GetComponent<VanillaItemDescriptor>();
                            if (CIW != null)
                            {
                                if(CIW.stack > 0)
                                {
                                    if(CIW.stack > amountToRemoveFromBase)
                                    {
                                        CIW.stack = CIW.stack - amountToRemoveFromBase;
                                        Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - amountToRemoveFromBase;
                                        amountLeftToRemoveFromBase = 0;
                                    }
                                    else // CIW.stack <= amountToRemoveFromBase
                                    {
                                        Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - CIW.stack;
                                        amountLeftToRemoveFromBase -= CIW.stack;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else if(CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                {
                                    bool containsItem = false;
                                    foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                    {
                                        if (itemInSlot != null)
                                        {
                                            containsItem = true;
                                            break;
                                        }
                                    }

                                    if (!containsItem)
                                    {
                                        Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - 1;
                                        --amountLeftToRemoveFromBase;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else if(CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                {
                                    if (CIW.containerItemRoot.childCount == 0)
                                    {
                                        Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - 1;
                                        --amountLeftToRemoveFromBase;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else
                                {
                                    Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - 1;
                                    --amountLeftToRemoveFromBase;
                                    objectList.RemoveAt(objectList.Count - 1);
                                    CIW.physObj.SetQuickBeltSlot(null);
                                    CIW.destroyed = true;
                                    HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                    Destroy(toCheck);
                                }
                            }
                            else
                            {
                                Mod.baseInventory[actualID] = Mod.baseInventory[actualID] - 1;
                                --amountLeftToRemoveFromBase;
                                objectList.RemoveAt(objectList.Count - 1);
                                VID.physObj.SetQuickBeltSlot(null);
                                VID.destroyed = true;
                                HideoutController.RemoveFromContainer(toCheck.transform, null, VID);
                                Destroy(toCheck);
                            }
                        }
                    }

                    actualIDIndex = 0;
                    int amountLeftToRemoveFromPlayer = amountToRemoveFromPlayer;
                    while (amountLeftToRemoveFromPlayer > 0)
                    {
                        string actualID = actualIDs[actualIDIndex++];
                        if (!Mod.playerInventory.ContainsKey(actualID))
                        {
                            continue;
                        }
                        else
                        {
                            actualIDsUsed.Add(actualID);
                        }
                        List<GameObject> objectList = Mod.playerInventoryObjects[actualID];
                        for (int i = objectList.Count - 1; i >= 0 && amountLeftToRemoveFromPlayer > 0; --i)
                        {
                            GameObject toCheck = objectList[objectList.Count - 1];
                            MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                            VanillaItemDescriptor VID = toCheck.GetComponent<VanillaItemDescriptor>();
                            if (CIW != null)
                            {
                                if (CIW.stack > 0)
                                {
                                    if (CIW.stack > amountToRemoveFromPlayer)
                                    {
                                        CIW.stack = CIW.stack - amountToRemoveFromPlayer;
                                        Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - amountToRemoveFromPlayer;
                                        amountLeftToRemoveFromPlayer = 0;
                                    }
                                    else // CIW.stack <= amountToRemoveFromBase
                                    {
                                        Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - CIW.stack;
                                        amountLeftToRemoveFromPlayer -= CIW.stack;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.physObj.ForceBreakInteraction();
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= CIW.currentWeight;
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                {
                                    bool containsItem = false;
                                    foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                    {
                                        if (itemInSlot != null)
                                        {
                                            containsItem = true;
                                            break;
                                        }
                                    }

                                    if (!containsItem)
                                    {
                                        Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - 1;
                                        --amountLeftToRemoveFromPlayer;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.physObj.ForceBreakInteraction();
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= CIW.currentWeight;
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                {
                                    if (CIW.containerItemRoot.childCount == 0)
                                    {
                                        Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - 1;
                                        --amountLeftToRemoveFromPlayer;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        CIW.physObj.ForceBreakInteraction();
                                        CIW.destroyed = true;
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= CIW.currentWeight;
                                    }
                                }
                                else
                                {
                                    Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - 1;
                                    --amountLeftToRemoveFromPlayer;
                                    objectList.RemoveAt(objectList.Count - 1);
                                    CIW.physObj.SetQuickBeltSlot(null);
                                    CIW.physObj.ForceBreakInteraction();
                                    CIW.destroyed = true;
                                    HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                    Destroy(toCheck);
                                    Mod.weight -= CIW.currentWeight;
                                }
                            }
                            else // VID != null
                            {
                                Mod.playerInventory[actualID] = Mod.playerInventory[actualID] - 1;
                                --amountLeftToRemoveFromPlayer;
                                objectList.RemoveAt(objectList.Count - 1);
                                VID.physObj.SetQuickBeltSlot(null);
                                VID.physObj.ForceBreakInteraction();
                                VID.destroyed = true;
                                HideoutController.RemoveFromContainer(toCheck.transform, null, VID);
                                Destroy(toCheck);
                                Mod.weight -= VID.currentWeight;
                            }
                            //else // should never happen, every item is supposed to have a CIW or VID
                            //{
                            //    --j;
                            //    objectList.RemoveAt(objectList.Count - 1);
                            //    Destroy(toCheck);
                            //}
                        }
                    }

                    foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
                    {
                        foreach (string actualIDUsed in actualIDsUsed)
                        {
                            baseAreaManager.UpdateBasedOnItem(actualIDUsed);
                        }
                    }
                }
            }
            // Remove required for area entry, because it will be refilled when we update the area state
            Mod.requiredPerArea[areaIndex] = null;

            if ((int)Mod.areasDB[areaIndex]["stages"][(level + 1).ToString()]["constructionTime"] == 0)
            {
                Upgrade();
            }
            else
            {
                constructing = true;
                constructionTimer = (int)Mod.areasDB[areaIndex]["stages"][(level + 1).ToString()]["constructionTime"];
            }

            // Add skill points
            Mod.AddSkillExp(Skill.skillPointsPerAreaUpgrade, 51);
        }

        public void OnUpgradeCancelClicked()
        {
            buttonClickSound.Play();

            // Disable all upgrade dialogs THIS WILL BE DONE DIRECTLY BY DISABLING THE PROCESSORS
            //foreach(GameObject upgradeDialog in upgradeDialogs)
            //{
            //    upgradeDialog.SetActive(false);
            //}

            // Disable upgrade check processors
            //baseManager.activeCheckProcessors[0].gameObject.SetActive(false);
            //baseManager.activeCheckProcessors[1].gameObject.SetActive(false);
        }

        public void OnUpgradeWarningContinueClicked()
        {
            buttonClickSound.Play();

            // Disable warning, enable upgrade confirm
            upgradeDialogs[1].SetActive(false);
            upgradeDialogs[2].SetActive(true);
        }

        public void Upgrade()
        {
            transform.GetChild(level).gameObject.SetActive(false);
            transform.GetChild(level + 1).gameObject.SetActive(true);

            // Transfer items to new slots and make slotItems bigger depending on new slot count
            if (level < slots.Count)
            {
                if (slots[level] != null && slots[level].Count > 0)
                {
                    for (int i = 0; i < slots[level].Count; ++i)
                    {
                        Mod.areaSlotShouldUpdate = false;
                        //slots[level][i].CurObject.SetQuickBeltSlot(slots[level + 1][i]);
                    }
                }
                if (slots[level + 1] != null && slots[level + 1].Count > 0)
                {
                    transform.GetChild(transform.childCount - 1).GetChild(level).gameObject.SetActive(false);
                    transform.GetChild(transform.childCount - 1).GetChild(level + 1).gameObject.SetActive(true);
                    GameObject[] newSlotItems = new GameObject[slots[level + 1].Count];
                    for(int i =0; i < slotItems.Length; ++i)
                    {
                        newSlotItems[i] = slotItems[i];
                    }
                    slotItems = newSlotItems;
                }
            }

            SetEffectsActive(false);

            ++level;

            transform.GetChild(level).GetComponent<AudioSource>().Play();

            UpdateAreaState();
            foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
            {
                baseAreaManager.UpdateBasedOnAreaLevel(areaIndex, level);
            }
            SetEffectsActive(true);
        }

        public void OnFarmingViewSetAllClick(string productionID, string endProduct)
        {
            string[] requiredItemIDs = productionsByItemIDByProdID[productionID][endProduct].requirements[0].IDs;
            bool custom = false;

            int totalAmountInInventory = 0;
            int totalSlotsToFillCount = 0;
            foreach (string requiredItemID in requiredItemIDs)
            {
                int amountInPlayerInventory = 0;
                int amountInBaseInventory = 0;
                if (Mod.baseInventory.ContainsKey(requiredItemID))
                {
                    foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[requiredItemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            custom = true;
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else if (itemCIW.maxStack > 0)
                            {
                                amountInBaseInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                        else
                        {
                            ++amountInBaseInventory;
                        }
                    }
                }
                if (Mod.playerInventory.ContainsKey(requiredItemID))
                {
                    foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[requiredItemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            custom = true;
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else if (itemCIW.maxStack > 0)
                            {
                                amountInPlayerInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                        else
                        {
                            ++amountInPlayerInventory;
                        }
                    }
                }

                int amountInInventory = amountInBaseInventory + amountInPlayerInventory;
                totalAmountInInventory += amountInInventory;
                if (amountInInventory == 0)
                {
                    return;
                }

                // Check number of slots available 
                int filledSlotCount = GetFilledSlotCount();
                int slotsToFillCount = slots[level].Count() - filledSlotCount;
                totalSlotsToFillCount += slotsToFillCount;
                if (slotsToFillCount == 0)
                {
                    return;
                }

                // Get item's prefab CIW if it is a custom item
                MeatovItem CIW = null;
                if (custom)
                {
                    CIW = Mod.itemPrefabs[int.Parse(requiredItemID)].GetComponentInChildren<MeatovItem>();
                }

                // If item custom and has an amount
                if (custom && CIW.maxAmount != 0)
                {
                    // Take the most used up instances of this item we can find
                    for (int i = 0; i < slotsToFillCount && i < amountInInventory; ++i)
                    {
                        int currentLeastAmount = CIW.maxAmount;
                        GameObject leastInstanceObject = null;
                        List<GameObject> listToUse = null;

                        // Choose which list of objects to use
                        if (amountInBaseInventory > 0)
                        {
                            listToUse = baseManager.baseInventoryObjects[requiredItemID];
                        }
                        else // Have to take from player inventory
                        {
                            listToUse = Mod.playerInventoryObjects[requiredItemID];
                        }

                        // Find least amount object that has more than 0 amount
                        for (int j = listToUse.Count - 1; j >= 0; --j)
                        {
                            MeatovItem instanceCIW = listToUse[j].GetComponentInChildren<MeatovItem>();
                            if (instanceCIW.amount > 0 && instanceCIW.amount <= currentLeastAmount)
                            {
                                currentLeastAmount = instanceCIW.amount;
                                leastInstanceObject = listToUse[j];
                            }
                        }

                        // Set the item's slot to the first available one
                        for (int slotIndex = 0; slotIndex < slots[level].Count; ++slotIndex)
                        {
                            //if (slots[level][slotIndex].CurObject == null)
                            //{
                            //    FVRPhysicalObject slotItemPhysObj = leastInstanceObject.GetComponentInChildren<FVRPhysicalObject>();
                            //    Mod.areaSlotShouldUpdate = false;
                            //    slotItemPhysObj.SetQuickBeltSlot(slots[level][slotIndex]);
                            //    slotItemPhysObj.SetParentage(slots[level][slotIndex].QuickbeltRoot);
                            //    ++filledSlotCount;

                            //    MeatovItemWrapper instanceCIW = slotItemPhysObj.GetComponent<MeatovItemWrapper>();
                            //    HideoutController.RemoveFromContainer(leastInstanceObject.transform, instanceCIW, null);
                            //    int preLocationIndex = instanceCIW.locationIndex;
                            //    BeginInteractionPatch.SetItemLocationIndex(3, instanceCIW, null);
                            //    if (preLocationIndex == 0) // If was on player
                            //    {
                            //        amountInInventory -= instanceCIW.stack;
                            //        Mod.RemoveFromPlayerInventory(instanceCIW.transform, true);
                            //    }
                            //    else // Was in hideout
                            //    {
                            //        amountInInventory -= instanceCIW.stack;
                            //        Mod.currentHideoutManager.RemoveFromBaseInventory(instanceCIW.transform, true);
                            //    }
                            //    break;
                            //}
                        }
                    }
                }
                else // either vanilla or does not have an amount
                {
                    for (int i = 0; i < slotsToFillCount && i < (amountInBaseInventory + amountInPlayerInventory); ++i)
                    {
                        GameObject instanceObject = null;
                        List<GameObject> listToUse = null;

                        // Choose which list of objects to use
                        if (amountInBaseInventory > 0)
                        {
                            listToUse = baseManager.baseInventoryObjects[requiredItemID];
                        }
                        else // Have to take from player inventory
                        {
                            listToUse = Mod.playerInventoryObjects[requiredItemID];
                        }

                        // Take the last item from the list
                        instanceObject = listToUse[listToUse.Count - 1];

                        // Set the item's slot to the first available one
                        for (int slotIndex = 0; slotIndex < slots[level].Count; ++slotIndex)
                        {
                            //if (slots[level][slotIndex].CurObject == null)
                            //{
                            //    FVRPhysicalObject slotItemPhysObj = instanceObject.GetComponentInChildren<FVRPhysicalObject>();
                            //    Mod.areaSlotShouldUpdate = false;
                            //    slotItemPhysObj.SetQuickBeltSlot(slots[level][slotIndex]);
                            //    slotItemPhysObj.SetParentage(slots[level][slotIndex].QuickbeltRoot);
                            //    ++filledSlotCount;

                            //    MeatovItemWrapper instanceCIW = slotItemPhysObj.GetComponent<MeatovItemWrapper>();
                            //    VanillaItemDescriptor instanceVID = slotItemPhysObj.GetComponent<VanillaItemDescriptor>();
                            //    HideoutController.RemoveFromContainer(instanceObject.transform, null, instanceVID);
                            //    int preLocationIndex = -1;
                            //    if (custom)
                            //    {
                            //        preLocationIndex = instanceCIW.locationIndex;
                            //    }
                            //    else
                            //    {
                            //        preLocationIndex = instanceVID.locationIndex;
                            //    }
                            //    BeginInteractionPatch.SetItemLocationIndex(3, instanceCIW, instanceVID);
                            //    if (preLocationIndex == 0) // If was on player
                            //    {
                            //        int amountToRemove = custom ? instanceCIW.stack : 1;
                            //        amountInInventory -= amountToRemove;
                            //        Mod.RemoveFromPlayerInventory(instanceCIW.transform, true);
                            //    }
                            //    else // Was in hideout
                            //    {
                            //        int amountToRemove = custom ? instanceCIW.stack : 1;
                            //        amountInInventory -= amountToRemove;
                            //        Mod.currentHideoutManager.RemoveFromBaseInventory(instanceObject.transform, true);
                            //    }
                            //    break;
                            //}
                        }
                    }
                }

                foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
                {
                    baseAreaManager.UpdateBasedOnItem(requiredItemID, true, amountInInventory);
                }
            }

            // Update UI of the production itself
            GameObject farmingView = productionsByItemIDByProdID[productionID][endProduct].gameObject;
            int installedAmount = Mathf.Min(totalAmountInInventory, totalSlotsToFillCount);
            productionsByItemIDByProdID[productionID][endProduct].installedCount += installedAmount;

            if(installedAmount > 0)
            {
                PlaySlotInputSound();
            }

            farmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = productionsByItemIDByProdID[productionID][endProduct].installedCount.ToString();
            if(productionsByItemIDByProdID[productionID][endProduct].installedCount > 0)
            {
                if (Mod.itemIcons.ContainsKey(requiredItemIDs[0]))
                {
                    farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[requiredItemIDs[0]];
                }
                else
                {
                    AnvilManager.Run(Mod.SetVanillaIcon(requiredItemIDs[0], farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>()));
                }
                if (!productionsByItemIDByProdID[productionID][endProduct].active)
                {
                    productionsByItemIDByProdID[productionID][endProduct].active = true;

                    // Only set timeLeft to productinTime if there isnt already some time left, because that would mean that 
                    // the production was already in progress but it did not have an item in its slot
                    if(productionsByItemIDByProdID[productionID][endProduct].timeLeft <= 0)
                    {
                        productionsByItemIDByProdID[productionID][endProduct].timeLeft = productionsByItemIDByProdID[productionID][endProduct].productionTime - (productionsByItemIDByProdID[productionID][endProduct].productionTime * (Skill.productionTimeReductionPerLevel * (Mod.skills[50].currentProgress / 100) / 100));
                    }

                    activeProductions.Add(productionID, productionsByItemIDByProdID[productionID][endProduct]);

                    PlayProductionStartSound();
                }

                // Set production status
                farmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);
                int[] formattedTimeLeft = FormatTime(productionsByItemIDByProdID[productionID][endProduct].timeLeft);
                if (generatorRunning) 
                {
                    farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                }
                else
                {
                    farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                }
            }
            else
            {
                farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = HideoutController.emptyItemSlotIcon;
                if (productionsByItemIDByProdID[productionID][endProduct].active)
                {
                    productionsByItemIDByProdID[productionID][endProduct].active = false;

                    activeProductions.Remove(productionID);
                }
            }
        }
        
        public void OnFarmingViewSetOneClick(string productionID, string endProduct)
        {
            // Just return if we have none of necessary item in inventory
            string[] requiredItemIDs = productionsByItemIDByProdID[productionID][endProduct].requirements[0].IDs;
            bool custom = false;

            foreach (string requiredItemID in requiredItemIDs)
            {
                int amountInPlayerInventory = 0;
                int amountInBaseInventory = 0;
                if (Mod.baseInventory.ContainsKey(requiredItemID))
                {
                    foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[requiredItemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            custom = true;
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else if (itemCIW.maxStack > 0)
                            {
                                amountInBaseInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                        else
                        {
                            ++amountInBaseInventory;
                        }
                    }
                }
                if (Mod.playerInventory.ContainsKey(requiredItemID))
                {
                    foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[requiredItemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            custom = true;
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else if (itemCIW.maxStack > 0)
                            {
                                amountInPlayerInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                        else
                        {
                            ++amountInPlayerInventory;
                        }
                    }
                }

                int amountInInventory = amountInBaseInventory + amountInPlayerInventory;
                if (amountInInventory == 0)
                {
                    return;
                }

                // Check number of slots available 
                int filledSlotCount = GetFilledSlotCount();
                int slotsToFillCount = slots[level].Count() - filledSlotCount;
                if (slotsToFillCount == 0)
                {
                    return;
                }

                // Get item's prefab CIW if it is a custom item
                MeatovItem CIW = null;
                if (custom)
                {
                    CIW = Mod.itemPrefabs[int.Parse(requiredItemID)].GetComponentInChildren<MeatovItem>();
                }

                // If item custom and has an amount
                if (custom && CIW.maxAmount != 0)
                {
                    // Take the most used up instances of this item we can find
                    int currentLeastAmount = CIW.maxAmount;
                    GameObject leastInstanceObject = null;
                    List<GameObject> listToUse = null;

                    // Choose which list of objects to use
                    if (amountInBaseInventory > 0)
                    {
                        listToUse = baseManager.baseInventoryObjects[requiredItemID];
                    }
                    else // Have to take from player inventory
                    {
                        listToUse = Mod.playerInventoryObjects[requiredItemID];
                    }

                    // Find least amount object that has more than 0 amount
                    for (int j = listToUse.Count - 1; j >= 0; --j)
                    {
                        MeatovItem instanceCIW = listToUse[j].GetComponentInChildren<MeatovItem>();
                        if (instanceCIW.amount > 0 && instanceCIW.amount <= currentLeastAmount)
                        {
                            currentLeastAmount = instanceCIW.amount;
                            leastInstanceObject = listToUse[j];
                        }
                    }

                    // Set the item's slot to the first available one
                    for (int slotIndex = 0; slotIndex < slots[level].Count; ++slotIndex)
                    {
                        //if (slots[level][slotIndex].CurObject == null)
                        //{
                        //    FVRPhysicalObject slotItemPhysObj = leastInstanceObject.GetComponentInChildren<FVRPhysicalObject>();
                        //    Mod.areaSlotShouldUpdate = false;
                        //    slotItemPhysObj.SetQuickBeltSlot(slots[level][slotIndex]);
                        //    slotItemPhysObj.SetParentage(slots[level][slotIndex].QuickbeltRoot);
                        //    ++filledSlotCount;

                        //    MeatovItemWrapper instanceCIW = slotItemPhysObj.GetComponent<MeatovItemWrapper>();
                        //    HideoutController.RemoveFromContainer(leastInstanceObject.transform, instanceCIW, null);
                        //    int preLocationIndex = instanceCIW.locationIndex;
                        //    BeginInteractionPatch.SetItemLocationIndex(3, instanceCIW, null);
                        //    if (preLocationIndex == 0) // If was on player
                        //    {
                        //        amountInInventory -= instanceCIW.stack;
                        //        Mod.RemoveFromPlayerInventory(instanceCIW.transform, true);
                        //    }
                        //    else // Was in hideout
                        //    {
                        //        amountInInventory -= instanceCIW.stack;
                        //        Mod.currentHideoutManager.RemoveFromBaseInventory(instanceCIW.transform, true);
                        //    }
                        //    break;
                        //}
                    }
                }
                else // either vanilla or does not have an amount
                {
                    GameObject instanceObject = null;
                    List<GameObject> listToUse = null;

                    // Choose which list of objects to use
                    if (amountInBaseInventory > 0)
                    {
                        listToUse = baseManager.baseInventoryObjects[requiredItemID];
                    }
                    else // Have to take from player inventory
                    {
                        listToUse = Mod.playerInventoryObjects[requiredItemID];
                    }

                    // Take the last item from the list
                    instanceObject = listToUse[listToUse.Count - 1];

                    // Set the item's slot to the first available one
                    for (int slotIndex = 0; slotIndex < slots[level].Count; ++slotIndex)
                    {
                        //if (slots[level][slotIndex].CurObject == null)
                        //{
                        //    FVRPhysicalObject slotItemPhysObj = instanceObject.GetComponentInChildren<FVRPhysicalObject>();
                        //    Mod.areaSlotShouldUpdate = false;
                        //    slotItemPhysObj.SetQuickBeltSlot(slots[level][slotIndex]);
                        //    slotItemPhysObj.SetParentage(slots[level][slotIndex].QuickbeltRoot);
                        //    ++filledSlotCount;

                        //    MeatovItemWrapper instanceCIW = slotItemPhysObj.GetComponent<MeatovItemWrapper>();
                        //    VanillaItemDescriptor instanceVID = slotItemPhysObj.GetComponent<VanillaItemDescriptor>();
                        //    HideoutController.RemoveFromContainer(instanceObject.transform, null, instanceVID);
                        //    int preLocationIndex = -1;
                        //    if (custom)
                        //    {
                        //        preLocationIndex = instanceCIW.locationIndex;
                        //    }
                        //    else
                        //    {
                        //        preLocationIndex = instanceVID.locationIndex;
                        //    }
                        //    BeginInteractionPatch.SetItemLocationIndex(3, instanceCIW, instanceVID);
                        //    if (preLocationIndex == 0) // If was on player
                        //    {
                        //        int amountToRemove = custom ? instanceCIW.stack : 1;
                        //        amountInInventory -= amountToRemove;
                        //        Mod.RemoveFromPlayerInventory(instanceCIW.transform, true);
                        //    }
                        //    else // Was in hideout
                        //    {
                        //        int amountToRemove = custom ? instanceCIW.stack : 1;
                        //        amountInInventory -= amountToRemove;
                        //        Mod.currentHideoutManager.RemoveFromBaseInventory(instanceObject.transform, true);
                        //    }
                        //    break;
                        //}
                    }
                }

                foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
                {
                    baseAreaManager.UpdateBasedOnItem(requiredItemID, true, amountInInventory);
                }
            }

            // Update UI of the production itself
            GameObject farmingView = productionsByItemIDByProdID[productionID][endProduct].gameObject;
            productionsByItemIDByProdID[productionID][endProduct].installedCount += 1;

            PlaySlotInputSound();

            farmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = productionsByItemIDByProdID[productionID][endProduct].installedCount.ToString();
            if (Mod.itemIcons.ContainsKey(requiredItemIDs[0]))
            {
                farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[requiredItemIDs[0]];
            }
            else
            {
                AnvilManager.Run(Mod.SetVanillaIcon(requiredItemIDs[0], farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>()));
            }
            if (!productionsByItemIDByProdID[productionID][endProduct].active)
            {
                productionsByItemIDByProdID[productionID][endProduct].active = true;

                // Only set timeLeft to productinTime if there isnt already some time left, because that would mean that 
                // the production was already in progress but it did not have an item in its slot
                if(productionsByItemIDByProdID[productionID][endProduct].timeLeft <= 0)
                {
                    productionsByItemIDByProdID[productionID][endProduct].timeLeft = productionsByItemIDByProdID[productionID][endProduct].productionTime - (productionsByItemIDByProdID[productionID][endProduct].productionTime * (Skill.productionTimeReductionPerLevel * (Mod.skills[50].currentProgress / 100) / 100));
                }

                activeProductions.Add(productionID, productionsByItemIDByProdID[productionID][endProduct]);

                PlayProductionStartSound();
            }

            // Set production status
            farmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);
            int[] formattedTimeLeft = FormatTime(productionsByItemIDByProdID[productionID][endProduct].timeLeft);
            if (generatorRunning) 
            {
                farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
            }
            else
            {
                farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
            }
        }

        public void PlaySlotInputSound()
        {
            if (HideoutController.areaSlotSounds[areaIndex] != null && slotAudioSourceByLevel[level] != null)
            {
                slotAudioSourceByLevel[level].PlayOneShot(HideoutController.areaSlotSounds[areaIndex]);
            }
        }

        public void OnFarmingViewRemoveOneClick(string productionID, string endProduct)
        {
            // Check if there are filled slots
            // If custom item, take the least used up amount one
            // remove it from the slot, attach it to the outputVolume at random rotation and position (unless water collector, put filter WITH col a meter above the volume position or something)
            float currentHighestAmount = 0;
            GameObject itemToRemove = null;
            MeatovItem itemCIW = null;
            bool custom = false;
            string itemID = "";
            //foreach(FVRQuickBeltSlot slot in slots[level])
            //{
            //    if(slot.CurObject != null)
            //    {
            //        itemCIW = slot.CurObject.GetComponent<MeatovItemWrapper>();
            //        if(itemCIW != null)
            //        {
            //            itemID = itemCIW.ID;
            //            custom = true;
            //            if(itemCIW.maxAmount > 0)
            //            {
            //                if (itemCIW.amount > currentHighestAmount)
            //                {
            //                    currentHighestAmount = itemCIW.amount;
            //                    itemToRemove = itemCIW.gameObject;
            //                }
            //            }
            //            else
            //            {
            //                itemToRemove = itemCIW.gameObject;
            //                break;
            //            }
            //        }
            //        else
            //        {
            //            itemToRemove = slot.CurObject.gameObject;
            //            break;
            //        }
            //    }
            //}
            if(itemToRemove == null)
            {
                return;
            }

            // Remove from slot
            // Disable col
            Transform outputVolume = transform.GetChild(transform.childCount - 3);
            if (custom)
            {
                itemCIW.physObj.SetQuickBeltSlot(null);
                itemCIW.physObj.SetParentage(outputVolume);
                itemCIW.hideoutSpawned = true;
                foreach (Collider col in itemCIW.colliders)
                {
                    col.enabled = false;
                }
                BeginInteractionPatch.SetItemLocationIndex(1, itemCIW, null);
            }
            else
            {
                VanillaItemDescriptor itemVID = itemToRemove.GetComponent<VanillaItemDescriptor>();
                itemID = itemVID.H3ID;
                itemVID.physObj.SetQuickBeltSlot(null);
                itemVID.physObj.SetParentage(outputVolume);
                itemVID.hideoutSpawned = true;
                itemVID.physObj.SetAllCollidersToLayer(false, "NoCol");
                BeginInteractionPatch.SetItemLocationIndex(1, null, itemVID);
            }

            // Attach to output volume
            if(areaIndex == 6)
            {
                // Water collector, do something different depending on item ID
                if (itemID.Equals("184"))
                {
                    // Superwater, set exactly at volume transform
                    itemToRemove.transform.position = outputVolume.position;
                    itemToRemove.transform.rotation = outputVolume.rotation;
                }
                else
                {
                    // Water filter, set in upper part of volume
                    BoxCollider boxCollider = outputVolume.GetComponent<BoxCollider>();
                    itemToRemove.transform.position = outputVolume.position + Vector3.up * (boxCollider.size.y / 2);
                    itemToRemove.transform.localPosition += new Vector3(UnityEngine.Random.Range(-0.5f, 0.5f), 0, UnityEngine.Random.Range(-0.5f, 0.5f));
                    itemToRemove.transform.rotation = UnityEngine.Random.rotation;
                }
            }
            else
            {
                // Set random position and rotation in volume
                BoxCollider boxCollider = outputVolume.GetComponent<BoxCollider>();
                itemToRemove.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
                                                                   UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
                                                                   UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
                itemToRemove.transform.rotation = UnityEngine.Random.rotation;
            }

            // Update all areas based on the item
            foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
            {
                baseAreaManager.UpdateBasedOnItem(itemID);
            }

            // Update UI of the production itself
            GameObject farmingView = productionsByItemIDByProdID[productionID][endProduct].gameObject;
            productionsByItemIDByProdID[productionID][endProduct].installedCount -= 1;

            farmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = productionsByItemIDByProdID[productionID][endProduct].installedCount.ToString();
            if (productionsByItemIDByProdID[productionID][endProduct].installedCount == 0)
            {
                farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = HideoutController.emptyItemSlotIcon;
                if (productionsByItemIDByProdID[productionID][endProduct].active)
                {
                    productionsByItemIDByProdID[productionID][endProduct].active = false;

                    activeProductions.Remove(productionID);
                }
            }
        }

        public void OnProductionGetItemsClick(string productionID, string endProduct)
        {
            // Spawn necessary number of items
            // UpdateBasedOnItem with spawned item ID
            // Resume production if it was continuous and at limit
            // Reset production if it was not continuous
            // MAke sure getitems button is disabled
            AreaProduction production = productionsByItemIDByProdID[productionID][endProduct];
            string itemID = production.endProduct;
            int amount = production.productionCount;
            Transform outputVolume = transform.GetChild(transform.childCount - 3);
            BoxCollider outputVolumeCollider = transform.GetChild(transform.childCount - 3).GetComponent<BoxCollider>();
            List<GameObject> objectsList = new List<GameObject>();
            if (int.TryParse(itemID, out int parseResult))
            {
                GameObject itemPrefab = Mod.itemPrefabs[parseResult];
                for (int i = 0; i < amount; ++i)
                {
                    GameObject itemObject = Instantiate(itemPrefab, outputVolume.transform);
                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-outputVolumeCollider.size.x / 2, outputVolumeCollider.size.x / 2),
                                                                     UnityEngine.Random.Range(-outputVolumeCollider.size.y / 2, outputVolumeCollider.size.y / 2),
                                                                     UnityEngine.Random.Range(-outputVolumeCollider.size.z / 2, outputVolumeCollider.size.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

                    MeatovItem CIW = itemObject.GetComponent<MeatovItem>();
                    BeginInteractionPatch.SetItemLocationIndex(1, CIW, null);
                    if (CIW.maxAmount > 0)
                    {
                        CIW.amount = CIW.maxAmount;
                    }

                    objectsList.Add(itemObject);
                }

                // Update production
                production.productionCount = 0;
                if (production.continuous)
                {
                    // Set string of production count
                    production.transform.GetChild(1).GetChild(4).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = "0/" + production.productionLimitCount;

                    // Deactivate getItems button
                    production.transform.GetChild(1).GetChild(5).GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    // Deactivate getItems button
                    production.transform.GetChild(4).GetChild(1).gameObject.SetActive(false);

                    // Check if it can be started again, set active start button accordingly
                    int totalAmount = 0;
                    int amountInPlayerInventory = 0;
                    int amountInBaseInventory = 0;
                    // Get amount of USABLE instances of this item in inventory. Usable must have amount > 0 left in it if custom
                    if (Mod.baseInventory.ContainsKey(itemID))
                    {
                        foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[itemID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInBaseInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInBaseInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                    }
                    if (Mod.playerInventory.ContainsKey(itemID))
                    {
                        foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[itemID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInPlayerInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInPlayerInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                    }
                    totalAmount = amountInBaseInventory + amountInPlayerInventory;
                    bool requirementsFulfilled = true;
                    for (int i = 0; i < production.requirements.Count; ++i)
                    {
                        if (requirementsFulfilled && production.requirements[i].count > totalAmount)
                        {
                            requirementsFulfilled = false;
                        }
                    }
                    production.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
                    if (requirementsFulfilled)
                    {
                        production.transform.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = true;
                        production.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.white;
                    }
                    else
                    {
                        production.transform.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
                        production.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;
                    }
                }
            }
            else
            {
                AnvilManager.Run(SpawnVanillaItem(itemID, amount, outputVolumeCollider, productionID, endProduct));
            }

            production.productionCount = 0;
        }

        public void OnProduceViewStartClick(string productionID, string endProduct)
        {
            // For each requirement remove needed amount of item from inventory
            // Update UI of requirements and of the production itself to indicate it is active
            // Set production logically as active
            AreaProduction production = productionsByItemIDByProdID[productionID][endProduct];
            foreach (AreaProductionRequirement requirement in production.requirements)
            {
                string[] requiredItemIDs = requirement.IDs;
                int amountToRemove = requirement.count;

                foreach (string requiredItemID in requiredItemIDs)
                {
                    int amountInPlayerInventory = 0;
                    int amountInBaseInventory = 0;
                    int amountToRemoveFromBase = 0;
                    int amountToRemoveFromPlayer = 0;
                    if (Mod.baseInventory.ContainsKey(requiredItemID))
                    {
                        foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[requiredItemID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInBaseInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInBaseInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                    }
                    if (Mod.playerInventory.ContainsKey(requiredItemID))
                    {
                        foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[requiredItemID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInPlayerInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInPlayerInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                    }

                    if(amountInBaseInventory >= amountToRemove)
                    {
                        amountToRemoveFromBase = amountToRemove;
                    }
                    else
                    {
                        amountToRemoveFromBase = amountInBaseInventory;
                        amountToRemoveFromPlayer = Mathf.Min(amountToRemove - amountToRemoveFromBase, amountInPlayerInventory);
                    }

                    if (amountToRemoveFromBase > 0)
                    {
                        amountInBaseInventory -= amountToRemoveFromBase;
                        Mod.baseInventory[requiredItemID] = Mod.baseInventory[requiredItemID] - amountToRemoveFromBase;
                        List<GameObject> objectList = baseManager.baseInventoryObjects[requiredItemID];
                        for (int i = objectList.Count - 1, j = amountToRemoveFromBase; i >= 0 && j > 0; --i)
                        {
                            GameObject toCheck = objectList[objectList.Count - 1];
                            MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                            if (CIW != null)
                            {
                                if (CIW.stack > 0)
                                {
                                    if (CIW.stack > amountToRemoveFromBase)
                                    {
                                        CIW.stack = CIW.stack - amountToRemoveFromBase;
                                    }
                                    else // CIW.stack <= amountToRemoveFromBase
                                    {
                                        j -= CIW.stack;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                {
                                    bool containsItem = false;
                                    foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                    {
                                        if (itemInSlot != null)
                                        {
                                            containsItem = true;
                                            break;
                                        }
                                    }

                                    if (!containsItem)
                                    {
                                        --j;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                {
                                    if (CIW.containerItemRoot.childCount == 0)
                                    {
                                        --j;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                    }
                                }
                                else
                                {
                                    --j;
                                    objectList.RemoveAt(objectList.Count - 1);
                                    CIW.physObj.SetQuickBeltSlot(null);
                                    HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                    Destroy(toCheck);
                                }
                            }
                            else
                            {
                                --j;
                                objectList.RemoveAt(objectList.Count - 1);
                                VanillaItemDescriptor VID = toCheck.GetComponent<VanillaItemDescriptor>();
                                VID.physObj.SetQuickBeltSlot(null);
                                HideoutController.RemoveFromContainer(toCheck.transform, null, VID);
                                Destroy(toCheck);
                            }
                        }
                    }
                    if (amountToRemoveFromPlayer > 0)
                    {
                        amountInPlayerInventory -= amountToRemoveFromPlayer;
                        Mod.playerInventory[requiredItemID] = Mod.playerInventory[requiredItemID] - amountToRemoveFromPlayer;
                        List<GameObject> objectList = Mod.playerInventoryObjects[requiredItemID];
                        for (int i = objectList.Count - 1, j = amountToRemoveFromPlayer; i >= 0 && j > 0; --i)
                        {
                            GameObject toCheck = objectList[objectList.Count - 1];
                            MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                            VanillaItemDescriptor VID = toCheck.GetComponent<VanillaItemDescriptor>();
                            if (CIW != null)
                            {
                                if (CIW.stack > 0)
                                {
                                    if (CIW.stack > amountToRemoveFromPlayer)
                                    {
                                        CIW.stack = CIW.stack - amountToRemoveFromPlayer;
                                    }
                                    else // CIW.stack <= amountToRemoveFromBase
                                    {
                                        j -= CIW.stack;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= CIW.currentWeight;
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                                {
                                    bool containsItem = false;
                                    foreach (GameObject itemInSlot in CIW.itemsInSlots)
                                    {
                                        if (itemInSlot != null)
                                        {
                                            containsItem = true;
                                            break;
                                        }
                                    }

                                    if (!containsItem)
                                    {
                                        --j;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= VID.currentWeight;
                                    }
                                }
                                else if (CIW.itemType == Mod.ItemType.Backpack || CIW.itemType == Mod.ItemType.Container || CIW.itemType == Mod.ItemType.Pouch)
                                {
                                    if (CIW.containerItemRoot.childCount == 0)
                                    {
                                        --j;
                                        objectList.RemoveAt(objectList.Count - 1);
                                        CIW.physObj.SetQuickBeltSlot(null);
                                        HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                        Destroy(toCheck);
                                        Mod.weight -= VID.currentWeight;
                                    }
                                }
                                else
                                {
                                    --j;
                                    objectList.RemoveAt(objectList.Count - 1);
                                    CIW.physObj.SetQuickBeltSlot(null);
                                    HideoutController.RemoveFromContainer(toCheck.transform, CIW, null);
                                    Destroy(toCheck);
                                    Mod.weight -= CIW.currentWeight;
                                }
                            }
                            else // VID != null
                            {
                                --j;
                                objectList.RemoveAt(objectList.Count - 1);
                                VID.physObj.SetQuickBeltSlot(null);
                                HideoutController.RemoveFromContainer(toCheck.transform, null, VID);
                                Destroy(toCheck);
                                Mod.weight -= VID.currentWeight;
                            }
                        }
                    }

                    amountToRemove -= (amountToRemoveFromBase + amountToRemoveFromPlayer);

                    // Update all requirement UI
                    foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
                    {
                        baseAreaManager.UpdateBasedOnItem(requiredItemID, false, (amountInBaseInventory - amountToRemoveFromBase) + (amountInPlayerInventory - amountToRemoveFromPlayer));
                    }

                    if(amountToRemove > 0)
                    {
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            // Update current produce view UI
            production.transform.GetChild(4).GetChild(0).gameObject.SetActive(false); // Disable start button
            production.transform.GetChild(4).GetChild(2).gameObject.SetActive(true); // Enable production status
            int[] formattedTimeLeft = FormatTime(production.productionTime); // Set production string
            production.transform.GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);

            // Set production active
            production.active = true;
            production.timeLeft = production.productionTime - (production.productionTime * (Skill.craftTimeReductionPerLevel * (Mod.skills[50].currentProgress / 100) / 100));
            activeProductions.Add(productionID, production);

            PlayProductionStartSound();
        }

        public void OnScavCaseViewStartClick()
        {
            // Check item in slot
            //MeatovItemWrapper itemCIW = slots[1][0].CurObject.GetComponent<MeatovItemWrapper>();
            //EFM_ScavCaseProduction newScavCaseProduction = new EFM_ScavCaseProduction();
            //if (itemCIW.ID.Equals("203")) // Roubles
            //{
            //    if(itemCIW.stack >= 95000)
            //    {
            //        // 8100s turnaround, Superrare min 1 max 2, Rare min 1 max 3
            //        newScavCaseProduction.timeLeft = 8100;
            //        newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Superrare, new Vector2Int(1, 2));
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int(1, 3));
            //    }
            //    else if(itemCIW.stack >= 15000)
            //    {
            //        // 7700s turnaround, Rare min 1 max 3, Common min 1 max 1
            //        newScavCaseProduction.timeLeft = 7700;
            //        newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int(1, 3));
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Common, new Vector2Int(1, 1));
            //    }
            //    else if(itemCIW.stack >= 2500)
            //    {
            //        // 2500s turnaround, Rare min 0 max 1, Common min 1 max 2
            //        newScavCaseProduction.timeLeft = 2500;
            //        newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int(0, 1));
            //        newScavCaseProduction.products.Add(Mod.ItemRarity.Common, new Vector2Int(1, 2));
            //    }
            //}
            //else if (itemCIW.ID.Equals("181")) // Moonshine
            //{
            //    // 16800s turnaround, Superrare min 3 max 5, Rare min 1 max 1
            //    newScavCaseProduction.timeLeft = 16800;
            //    newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
            //    newScavCaseProduction.products.Add(Mod.ItemRarity.Superrare, new Vector2Int(3, 5));
            //    newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int(1, 1));
            //}
            //else if (itemCIW.ID.Equals("190")) // Intelligence folder
            //{
            //    // 19200s turnaround, Superrare min 2 max 3, Rare min 2 max 4
            //    newScavCaseProduction.timeLeft = 19200;
            //    newScavCaseProduction.products = new Dictionary<Mod.ItemRarity, Vector2Int>();
            //    newScavCaseProduction.products.Add(Mod.ItemRarity.Superrare, new Vector2Int(2, 3));
            //    newScavCaseProduction.products.Add(Mod.ItemRarity.Rare, new Vector2Int(2, 4));
            //}

            if (activeScavCaseProductions == null)
            {
                activeScavCaseProductions = new List<EFM_ScavCaseProduction>();
            }
            //activeScavCaseProductions.Add(newScavCaseProduction);

            // Disable start button
            Transform scavCaseView = areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(2);
            scavCaseView.transform.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
            scavCaseView.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;

            PlayProductionStartSound();
        }

        private IEnumerator SpawnVanillaItem(string ID, int count, BoxCollider outputVolume, string fromProductionID, string endProduct)
        {
            yield return IM.OD[ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.LogWarning("Attempted to get vanilla prefab for " + ID + ", but the prefab had been destroyed, refreshing cache...");

                IM.OD[ID].RefreshCache();
                itemPrefab = IM.OD[ID].GetGameObject();
            }
            if (itemPrefab == null)
            {
                Mod.LogError("Attempted to get vanilla prefab for " + ID + ", but the prefab had been destroyed, refreshing cache did nothing");
                yield break;
            }
            VanillaItemDescriptor prefabVID = itemPrefab.GetComponent<VanillaItemDescriptor>();
            GameObject itemObject = null;
            bool spawnedSmallBox = false;
            bool spawnedBigBox = false;
            if (Mod.usedRoundIDs.Contains(prefabVID.H3ID))
            {
                // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of round
                if (count > 1)
                {
                    int countLeft = count;
                    float boxCountLeft = count / 120;
                    while (boxCountLeft > 0)
                    {
                        int amount = 0;
                        if (countLeft > 30)
                        {
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[716], outputVolume.transform);
                            HideoutController.instance.AddToBaseInventory(itemObject.transform, true);

                            if (countLeft <= 120)
                            {
                                amount = countLeft;
                                countLeft = 0;
                            }
                            else
                            {
                                amount = 120;
                                countLeft -= 120;
                            }

                            spawnedBigBox = true;
                        }
                        else
                        {
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[715], outputVolume.transform);

                            HideoutController.instance.AddToBaseInventory(itemObject.transform, true);

                            amount = countLeft;
                            countLeft = 0;

                            spawnedSmallBox = true;
                        }

                        MeatovItem itemCIW = itemObject.GetComponent<MeatovItem>();
                        itemCIW.foundInRaid = true;
                        FVRFireArmMagazine asMagazine = itemCIW.physObj as FVRFireArmMagazine;
                        FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                        asMagazine.RoundType = round.RoundType;
                        itemCIW.roundClass = round.RoundClass;
                        for (int j = 0; j < amount; ++j)
                        {
                            asMagazine.AddRound(itemCIW.roundClass, false, false);
                        }

                        itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-outputVolume.size.x / 2, outputVolume.size.x / 2),
                                                                         UnityEngine.Random.Range(-outputVolume.size.y / 2, outputVolume.size.y / 2),
                                                                         UnityEngine.Random.Range(-outputVolume.size.z / 2, outputVolume.size.z / 2));
                        itemObject.transform.localRotation = UnityEngine.Random.rotation;

                        BeginInteractionPatch.SetItemLocationIndex(1, itemCIW, null);

                        boxCountLeft = countLeft / 120;
                    }
                }
                else // Single round, spawn as normal
                {
                    itemObject = GameObject.Instantiate(itemPrefab, outputVolume.transform);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-outputVolume.size.x / 2, outputVolume.size.x / 2),
                                                                     UnityEngine.Random.Range(-outputVolume.size.y / 2, outputVolume.size.y / 2),
                                                                     UnityEngine.Random.Range(-outputVolume.size.z / 2, outputVolume.size.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

                    VanillaItemDescriptor VID = itemObject.GetComponent<VanillaItemDescriptor>();
                    VID.foundInRaid = true;
                    BeginInteractionPatch.SetItemLocationIndex(1, null, VID);

                    HideoutController.instance.AddToBaseInventory(VID.transform, true);
                }
            }
            else // Not a round, spawn as normal
            {
                for (int i = 0; i < count; ++i)
                {
                    itemObject = GameObject.Instantiate(itemPrefab, outputVolume.transform);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-outputVolume.size.x / 2, outputVolume.size.x / 2),
                                                                     UnityEngine.Random.Range(-outputVolume.size.y / 2, outputVolume.size.y / 2),
                                                                     UnityEngine.Random.Range(-outputVolume.size.z / 2, outputVolume.size.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

                    VanillaItemDescriptor VID = itemObject.GetComponent<VanillaItemDescriptor>();
                    VID.foundInRaid = true;
                    BeginInteractionPatch.SetItemLocationIndex(1, null, VID);

                    HideoutController.instance.AddToBaseInventory(VID.transform, true);
                }
            }

            // Update all areas based on the item
            foreach (BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
            {
                if (spawnedSmallBox || spawnedBigBox)
                {
                    if (spawnedSmallBox)
                    {
                        baseAreaManager.UpdateBasedOnItem("715");
                    }
                    if (spawnedBigBox)
                    {
                        baseAreaManager.UpdateBasedOnItem("716");
                    }
                }
                else
                {
                    baseAreaManager.UpdateBasedOnItem(ID);
                }
            }

            // Update production
            if (!fromProductionID.Equals("scav"))
            {
                AreaProduction production = productionsByItemIDByProdID[fromProductionID][endProduct];
                production.productionCount = 0;
                if (production.continuous)
                {
                    // Set string of production count
                    production.transform.GetChild(1).GetChild(4).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = "0/" + production.productionLimitCount;

                    // Deactivate getItems button
                    production.transform.GetChild(1).GetChild(5).GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    // Deactivate getItems button
                    production.transform.GetChild(4).GetChild(1).gameObject.SetActive(false);

                    // Check if it can be started again, set active start button accordingly
                    int totalAmount = 0;
                    int amountInPlayerInventory = 0;
                    int amountInBaseInventory = 0;
                    // Get amount of USABLE instances of this item in inventory. Usable must have amount > 0 left in it if custom
                    if (Mod.baseInventory.ContainsKey(ID))
                    {
                        foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[ID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInBaseInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInBaseInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                    }
                    if (Mod.playerInventory.ContainsKey(ID))
                    {
                        foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[ID])
                        {
                            MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                            if (itemCIW != null)
                            {
                                if (itemCIW.maxAmount > 0)
                                {
                                    if (itemCIW.amount > 0)
                                    {
                                        ++amountInPlayerInventory;
                                    }
                                }
                                else if (itemCIW.maxStack > 0)
                                {
                                    amountInPlayerInventory += itemCIW.stack;
                                }
                                else
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                    }
                    totalAmount = amountInBaseInventory + amountInPlayerInventory;
                    bool requirementsFulfilled = true;
                    for (int i = 0; i < production.requirements.Count; ++i)
                    {
                        if (requirementsFulfilled && production.requirements[i].count > totalAmount)
                        {
                            requirementsFulfilled = false;
                        }
                    }
                    production.transform.GetChild(4).GetChild(0).gameObject.SetActive(true);
                    if (requirementsFulfilled)
                    {
                        production.transform.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = true;
                        production.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.white;
                    }
                    else
                    {
                        production.transform.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
                        production.transform.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;
                    }
                }
            }
            yield break;
        }

        public int GetFilledSlotCount()
        {
            int filledCount = 0;
            foreach (GameObject slotItem in slotItems)
            {
                if (slotItem != null)
                {
                    ++filledCount;
                }
            }
            return filledCount;
        }

        public void UpdateBasedOnItem(string itemID, bool amountSpecified = false, int amount = 0)
        {
            Mod.LogInfo("UpdateBasedOnItem called with item: "+itemID+" amount "+amount);
            // Amount of usable instances of this item left
            int totalAmount = amount;
            if (!amountSpecified)
            {
                int amountInPlayerInventory = 0;
                int amountInBaseInventory = 0;
                // Get amount of USABLE instances of this item in inventory. Usable must have amount > 0 left in it if custom
                if (Mod.baseInventory.ContainsKey(itemID))
                {
                    foreach (GameObject itemInstanceObject in baseManager.baseInventoryObjects[itemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInBaseInventory;
                                }
                            }
                            else if(itemCIW.maxStack > 0)
                            {
                                amountInBaseInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInBaseInventory;
                            }
                        }
                        else
                        {
                            ++amountInBaseInventory;
                        }
                    }
                }
                if (Mod.playerInventory.ContainsKey(itemID))
                {
                    foreach (GameObject itemInstanceObject in Mod.playerInventoryObjects[itemID])
                    {
                        MeatovItem itemCIW = itemInstanceObject.GetComponentInChildren<MeatovItem>();
                        if (itemCIW != null)
                        {
                            if (itemCIW.maxAmount > 0)
                            {
                                if (itemCIW.amount > 0)
                                {
                                    ++amountInPlayerInventory;
                                }
                            }
                            else if (itemCIW.maxStack > 0)
                            {
                                amountInPlayerInventory += itemCIW.stack;
                            }
                            else
                            {
                                ++amountInPlayerInventory;
                            }
                        }
                        else
                        {
                            ++amountInPlayerInventory;
                        }
                    }
                }
                totalAmount = amountInBaseInventory + amountInPlayerInventory;
            }

            Mod.LogInfo("After amount: "+totalAmount);
            // Update UI corresponding to this item
            if (farmingViewByItemID != null && farmingViewByItemID.ContainsKey(itemID))
            {
                List<Transform> farmingViews = farmingViewByItemID[itemID];
                foreach (Transform currentFarmingView in farmingViews)
                {
                    currentFarmingView.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = totalAmount.ToString() + "(STASH)";
                }
            }
            if (produceViewByItemID != null && produceViewByItemID.ContainsKey(itemID))
            {
                List<Transform> produceViews = produceViewByItemID[itemID];
                foreach (Transform currentProduceView in produceViews)
                {
                    // Find index of requirement in production that is the item
                    // While we're at it also check if requirements are now fulfilled
                    AreaProduction production = currentProduceView.GetComponent<AreaProduction>();
                    AreaProductionRequirement requirement = null;
                    int requirementIndex = -1;
                    bool requirementsFulfilled = true;
                    for (int i = 0; i < production.requirements.Count; ++i)
                    {
                        if (production.requirements[i].IDs.Contains(itemID))
                        {
                            requirement = production.requirements[i];
                            requirementIndex = i;
                        }

                        if (requirementsFulfilled && production.requirements[i].count > totalAmount)
                        {
                            requirementsFulfilled = false;
                        }
                    }

                    // Update UI accordingly
                    currentProduceView.GetChild(1).GetChild(requirementIndex + 1).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = Mathf.Min(requirement.count, totalAmount).ToString() + "/" + requirement.count;
                    if (totalAmount >= requirement.count)
                    {
                        currentProduceView.GetChild(1).GetChild(requirementIndex + 1).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false);
                        currentProduceView.GetChild(1).GetChild(requirementIndex + 1).GetChild(1).GetChild(1).GetChild(1).gameObject.SetActive(true);
                    }
                    else // totalAmount < requirement.count
                    {
                        currentProduceView.GetChild(1).GetChild(requirementIndex + 1).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true);
                        currentProduceView.GetChild(1).GetChild(requirementIndex + 1).GetChild(1).GetChild(1).GetChild(1).gameObject.SetActive(false);
                    }

                    // Update state of production, maybe it can now be started
                    if (!production.active)
                    {
                        if (requirementsFulfilled)
                        {
                            currentProduceView.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = true;
                            currentProduceView.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.white;
                        }
                        else
                        {
                            currentProduceView.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
                            currentProduceView.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;
                        }
                    }
                }
            }
            if (itemRequirementsByItemID != null && itemRequirementsByItemID.ContainsKey(itemID))
            {
                List<Transform> itemRequirements = itemRequirementsByItemID[itemID];
                foreach (Transform currentItemRequirement in itemRequirements)
                {
                    AreaRequirement requirementScript = currentItemRequirement.GetComponent<AreaRequirement>();
                    //currentItemRequirement.GetChild(1).GetChild(0).GetComponent<Text>().text = Mathf.Min(totalAmount, requirementScript.count).ToString() + "/" + requirementScript.count;

                    //if (totalAmount >= requirementScript.count)
                    //{
                    //    currentItemRequirement.GetChild(1).GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                    //}
                    //else
                    //{
                    //    currentItemRequirement.GetChild(1).GetChild(1).GetComponent<Image>().sprite = HideoutController.requirementLocked;
                    //}
                }
            }
            UpdateUpgradableStatus();
        }

        public void UpdateBasedOnAreaLevel(int index, int newLevel)
        {
            if (areaRequirementsByAreaIndex != null && areaRequirementsByAreaIndex.ContainsKey(index))
            {
                List<Transform> requirementTransforms = areaRequirementsByAreaIndex[index];
                foreach (Transform currentRequirementTransform in requirementTransforms)
                {
                    AreaRequirement requirementScript = currentRequirementTransform.GetComponent<AreaRequirement>();

                    //if (newLevel >= requirementScript.level)
                    //{
                    //    currentRequirementTransform.GetChild(1).GetChild(0).GetComponent<Text>().color = Color.white;
                    //    currentRequirementTransform.GetChild(2).GetComponent<Image>().sprite = HideoutController.requirementFulfilled;
                    //}
                    //else
                    //{
                    //    currentRequirementTransform.GetChild(1).GetChild(0).GetComponent<Text>().color = Color.red;
                    //    currentRequirementTransform.GetChild(2).GetComponent<Image>().sprite = HideoutController.requirementLocked;
                    //}
                }
                UpdateUpgradableStatus();
            }
        }

        public void UpdateBasedOnSlots()
        {
            // In case of scav case
            if(areaIndex == 14)
            {
                Transform scavCaseView = areaCanvas.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(2);
                // Update UI
                //if (slots[1][0].CurObject == null)
                //{
                //    // Disable start button
                //    scavCaseView.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
                //    scavCaseView.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;
                //}
                //else
                //{
                //    // Check if item is something we can start production with, enable/disable start button accordingly
                //    MeatovItemWrapper itemCIW = slots[1][0].CurObject.GetComponent<MeatovItemWrapper>();
                //    if ((itemCIW.ID.Equals("203") && itemCIW.stack >= 2500) || // Roubles
                //        itemCIW.ID.Equals("181") || // Moonshine
                //        itemCIW.ID.Equals("190")) // Intelligence folder
                //    {
                //        // Enable start button
                //        scavCaseView.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = true;
                //        scavCaseView.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.white;
                //    }
                //    else
                //    {
                //        // Disable start button
                //        scavCaseView.GetChild(4).GetChild(0).GetComponent<Collider>().enabled = false;
                //        scavCaseView.GetChild(4).GetChild(0).GetChild(0).GetComponent<Text>().color = Color.gray;
                //    }
                //}

                return;
            }

            int filledSlotCount = 0;
            foreach (AreaSlot areaSlot in slots[level])
            {
                //if (areaSlot.CurObject != null)
                //{
                //    MeatovItemWrapper CIW = areaSlot.CurObject.GetComponent<MeatovItemWrapper>();
                //    if (CIW != null)
                //    {
                //        if (CIW.maxAmount > 0)
                //        {
                //            if (CIW.amount > 0)
                //            {
                //                ++filledSlotCount;
                //            }
                //        }
                //        else
                //        {
                //            ++filledSlotCount;
                //        }
                //    }
                //    else
                //    {
                //        ++filledSlotCount;
                //    }
                //}
            }

            // If area has continuous production, we must update the farming view's UI and maybe start it
            // Here we assume a single continuous production, considering they use the slots at inputs, there should not be mroe than 1
            foreach (KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in productionsByItemIDByProdID)
            {
                foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                {
                    if (production.Value.continuous)
                    {
                        Transform farmingView = production.Value.transform;

                        // Update UI and logic
                        farmingView.transform.GetChild(1).GetChild(2).GetChild(1).GetChild(0).GetChild(0).GetComponent<Text>().text = production.Value.installedCount.ToString();
                        if (filledSlotCount > 0)
                        {
                            if (Mod.itemIcons.ContainsKey(production.Value.requirements[0].IDs[0]))
                            {
                                farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[production.Value.requirements[0].IDs[0]];
                            }
                            else
                            {
                                AnvilManager.Run(Mod.SetVanillaIcon(production.Value.requirements[0].IDs[0], farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>()));
                            }
                            if (!production.Value.active)
                            {
                                production.Value.active = true;

                                // Only set timeLeft to productinTime if there isnt already some time left, because that would mean that 
                                // the production was already in progress but it did not have an item in its slot
                                if (production.Value.timeLeft <= 0)
                                {
                                    production.Value.timeLeft = production.Value.productionTime - (production.Value.productionTime * (Skill.productionTimeReductionPerLevel * (Mod.skills[50].currentProgress / 100) / 100));
                                }

                                activeProductions.Add(production.Key, production.Value);
                            }

                            // Set production status
                            farmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);
                            int[] formattedTimeLeft = FormatTime(production.Value.timeLeft);
                            if (generatorRunning)
                            {
                                farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                            }
                            else
                            {
                                farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                            }
                        }
                        else
                        {
                            farmingView.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).GetComponent<Image>().sprite = HideoutController.emptyItemSlotIcon;
                            if (production.Value.active)
                            {
                                production.Value.active = false;

                                activeProductions.Remove(production.Key);
                            }
                        }

                        break;
                    }
                }
            }

            // For any area with slots, enable or disable the area depending on slot content
            // Enabling or disabling the area will respectively affect the bonuses applied
            if (needsFuel)
            {
                if (generatorRunning)
                {
                    SetEffectsActive(filledSlotCount > 0);
                }
            }
        }

        public void UpdateUpgradableStatus()
        {
            Mod.LogInfo("Update upgradable status called");
            if (GetRequirementsFullfilled(true, true))
            {
                Mod.LogInfo("Update upgradable status FULFILLED");
                // Also implies !constructing

                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundAvailableSprite; // Summary Icon background color
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundAvailableSprite; // Full Icon background color

                if (level == 0)
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(false); // Disable locked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(true); // Enable unlocked icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconUnlocked; // Status icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Construct"; // Status text

                    // Enable Construct button, should be only one on bottom
                    areaCanvas.transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<Collider>().enabled = true;
                    areaCanvas.transform.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
                }
                else
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(9).gameObject.SetActive(true); // Enable ready for upgrade icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Disable producing panel
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconReadyUpgrade; // Status icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Ready to Upgrade"; // Status text

                    // Enable Upgrade button, should be second one on bottom 2
                    areaCanvas.transform.GetChild(1).GetChild(4).GetChild(1).GetComponent<Collider>().enabled = true;
                    areaCanvas.transform.GetChild(1).GetChild(4).GetChild(1).GetChild(1).GetComponent<Text>().color = Color.white;
                }
            }
            else
            {
                if (level == 0)
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundLockedSprite; // Summary Icon background color
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Locked"; // Status text
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(6).gameObject.SetActive(true); // Enable locked icon
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(7).gameObject.SetActive(false); // Disable unlocked icon

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundLockedSprite; // Icon background color
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconLocked; // Status icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Locked"; // Status text

                    // Disable Construct button, should be only one on bottom
                    areaCanvas.transform.GetChild(1).GetChild(3).GetChild(0).GetComponent<Collider>().enabled = false;
                    areaCanvas.transform.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
                }
                else
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Summary Icon background color

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaBackgroundNormalSprite; // Icon background color

                    // Dependent on fuel
                    if ((bool)Mod.areasDB[areaIndex]["needsFuel"])
                    {
                        if (generatorRunning)
                        {
                            if (activeProductions != null && activeProductions.Count > 0)
                            {
                                // Summary
                                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Enable production panel
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text

                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon

                                // Dependent on production
                                int doneCount = 0;
                                int totalCount = 0;
                                foreach(KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in productionsByItemIDByProdID)
                                {
                                    foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                                    {
                                        if (production.Value.active)
                                        {
                                            ++totalCount;
                                        }
                                        else
                                        {
                                            if (production.Value.productionCount > 0)
                                            {
                                                ++doneCount;
                                            }
                                        }
                                    }
                                }
                                if (totalCount > 0 && doneCount < totalCount)
                                {
                                    // Summary
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true);
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true);
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = doneCount.ToString() + "/" + totalCount;

                                    // Top
                                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing (" + doneCount + "/" + totalCount + ")"; // Status text
                                }
                                else // No active productions
                                {
                                    // Summary
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Enable production panel
                                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false);
                                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                                }
                            }
                            else
                            {
                                // Summary
                                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Disable out of fuel icon
                                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text

                                // Top
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                            }
                        }
                        else
                        {
                            // Summary
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(true); // Enable out of fuel icon
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out Of Fuel"; // Status text

                            // Top
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true);
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconOutOfFuel; // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out Of Fuel"; // Status text
                        }
                    }
                    else
                    {
                        if (activeProductions != null && activeProductions.Count > 0)
                        {
                            // Summary
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Enable production panel
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text

                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon
                        }
                        else
                        {
                            // Summary
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false); // Disable production panel
                            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Disable out of fuel icon
                            areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text

                            // Top
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                        }
                    }

                    // Disable Upgrade button, should be second one on bottom 2
                    areaCanvas.transform.GetChild(1).GetChild(4).GetChild(1).GetComponent<Collider>().enabled = false;
                    areaCanvas.transform.GetChild(1).GetChild(4).GetChild(1).GetChild(1).GetComponent<Text>().color = Color.gray;
                }
            }
        }

        public void UpdateBasedOnFuel()
        {
            if (needsFuel)
            {
                if (generatorRunning)
                {
                    int filledSlotCount = 0;
                    // TODO: Review, here we make the assumption that an area that requires fuel AND has slots, requires slots to be filled in order to function
                    // This means that something like the water collector's bonuses will be active when generator is on up to level 3, at which it gains a slot
                    // that must be filled with a water filter in order to function. Is that how we want it to function? Or should the slot being filled
                    // only affect if production of purified water if functionning?
                    if (slots != null && slots.Count > 0)
                    {
                        foreach (AreaSlot areaSlot in slots[level])
                        {
                            //if (areaSlot.CurObject != null)
                            //{
                            //    MeatovItemWrapper CIW = areaSlot.CurObject.GetComponent<MeatovItemWrapper>();
                            //    if (CIW != null)
                            //    {
                            //        if (CIW.maxAmount > 0)
                            //        {
                            //            if (CIW.amount > 0)
                            //            {
                            //                ++filledSlotCount;
                            //            }
                            //        }
                            //        else
                            //        {
                            //            ++filledSlotCount;
                            //        }
                            //    }
                            //    else
                            //    {
                            //        ++filledSlotCount;
                            //    }
                            //}
                        }
                        if (filledSlotCount > 0)
                        {
                            // Make sure out of fuel icons are disabled, effects are enabled, and paused productions are resumed
                            SetEffectsActive(true);
                            ResumeProductions();
                            SetOutOfFuelUI(false);

                            BeginGeneratorDependentAudio();
                        }
                    }
                    else // Needs fuel but does not have slots, therefore its funcitoning does not depend on slots being filled
                    {
                        // Make sure out of fuel icons are disabled, effects are enabled, and paused productions are resumed
                        SetEffectsActive(true);
                        ResumeProductions();
                        SetOutOfFuelUI(false);

                        BeginGeneratorDependentAudio();
                    }
                }
                else
                {
                    // Make sure out of fuel icons are enabled, effects are disabled, and active productions are paused
                    SetEffectsActive(false);
                    PauseProductions();
                    SetOutOfFuelUI(true);

                    StopGeneratorDependentAudio();
                }
            }
        }

        public void SetOutOfFuelUI(bool active)
        {
            areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(active); // Summary Out of fuel icon
            areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(active); // Top Out of fuel icon

            if (active)
            {
                areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconOutOfFuel; // Status icon
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Out of Fuel"; // Status text
            }
            else
            {
                if (activeProductions != null && activeProductions.Count > 0)
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true); // Enable production panel
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text

                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(true); // Status icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Image>().sprite = HideoutController.areaStatusIconProducing; // Status icon
                }
                else
                {
                    // Summary
                    areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(8).gameObject.SetActive(false); // Disable out of fuel icon
                    areaCanvas.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text

                    // Top
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0).gameObject.SetActive(false); // Status icon
                    areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "On Stand By"; // Status text
                }
            }
        }

        public void SetEffectsActive(bool active)
        {
            foreach(AreaBonus bonus in bonuses)
            {
                bonus.SetActive(active);
            }
        }

        public void ResumeProductions()
        {
            Mod.LogInfo("Resume productions called on area: " + areaIndex);
            if(productionsByItemIDByProdID == null)
            {
                return;
            }
            Mod.LogInfo("\tHas productions");

            foreach (KeyValuePair<string, Dictionary<string, AreaProduction>> productionEntry in productionsByItemIDByProdID)
            {
                foreach (KeyValuePair<string, AreaProduction> production in productionEntry.Value)
                {
                    Mod.LogInfo("\t\tProduction: " + production.Key);
                    if (production.Value.timeLeft > 0 && !activeProductions.ContainsKey(production.Key))
                    {
                        Mod.LogInfo("\t\t\tNot active but should be");
                        if (production.Value.continuous)
                        {
                            Mod.LogInfo("\t\t\t\tContinuous");
                            GameObject farmingView = production.Value.gameObject;

                            production.Value.active = true;

                            // Only set timeLeft to productionTime if there isnt already some time left, 0 timeLeft could happen if we paused it the frame it reached <= 0
                            if (production.Value.timeLeft <= 0)
                            {
                                production.Value.timeLeft = production.Value.productionTime - (production.Value.productionTime * (Skill.productionTimeReductionPerLevel * (Mod.skills[50].currentProgress / 100) / 100)); ;
                            }

                            activeProductions.Add(production.Key, production.Value);

                            // Set production status
                            farmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);
                            int[] formattedTimeLeft = FormatTime(production.Value.timeLeft);

                            farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                        }
                        else if (production.Value.timeLeft > 0)
                        {
                            Mod.LogInfo("\t\t\t\tNon Continuous");
                            production.Value.active = true;
                            activeProductions.Add(production.Key, production.Value);

                            GameObject produceView = production.Value.gameObject;
                            int[] formattedTimeLeft = FormatTime(production.Value.timeLeft);

                            produceView.transform.GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Producing\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                        }
                    }
                }
            }

            Mod.LogInfo("\tSetting production UI");
            // Set Production UI
            int doneCount = 0;
            int totalCount = 0;
            int continuousCount = 0;
            foreach (KeyValuePair<string, AreaProduction> production in activeProductions)
            {
                if (production.Value.continuous)
                {
                    ++continuousCount;
                    continue;
                }
                else if (production.Value.timeLeft <= 0)
                {
                    ++doneCount;
                }
                ++totalCount;
            }
            Mod.LogInfo("\tDoneCount: "+doneCount+", continuousCount: "+continuousCount);
            if (doneCount > 0)
            {
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true);
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = doneCount.ToString() + "/" + totalCount;

                // Top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing (" + doneCount + "/" + totalCount + ")"; // Status text
            }
            else if (continuousCount > 0)
            {
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                // Top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
            }
        }

        public void PauseProductions()
        {
            foreach(KeyValuePair<string, AreaProduction> production in activeProductions)
            {
                production.Value.active = false;
                if (production.Value.continuous)
                {
                    GameObject farmingView = production.Value.gameObject;

                    // Set production status
                    farmingView.transform.GetChild(1).GetChild(5).GetChild(1).gameObject.SetActive(true);
                    int[] formattedTimeLeft = FormatTime(production.Value.timeLeft);

                    farmingView.transform.GetChild(1).GetChild(5).GetChild(1).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                }
                else if (production.Value.timeLeft > 0)
                {
                    GameObject produceView = production.Value.gameObject;
                    int[] formattedTimeLeft = FormatTime(production.Value.timeLeft);

                    produceView.transform.GetChild(4).GetChild(2).GetChild(0).GetComponent<Text>().text = String.Format("Paused\n({0:00}:{1:00}:{2:00})...", formattedTimeLeft[0], formattedTimeLeft[1], formattedTimeLeft[2]);
                }
            }
            activeProductions.Clear();

            // Set Production UI
            int doneCount = 0;
            int totalCount = 0;
            int continuousCount = 0;
            foreach (KeyValuePair<string, AreaProduction> production in activeProductions)
            {
                if (production.Value.continuous)
                {
                    ++continuousCount;
                    continue;
                }
                else if (production.Value.timeLeft <= 0)
                {
                    ++doneCount;
                }
                ++totalCount;
            }
            if (doneCount > 0)
            {
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(true);
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).GetComponent<Text>().text = doneCount.ToString() + "/" + totalCount;

                // Top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing (" + doneCount + "/" + totalCount + ")"; // Status text
            }
            else if (continuousCount > 0)
            {
                // Summary
                areaCanvas.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(12).GetChild(0).gameObject.SetActive(false);

                // Top
                areaCanvas.transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = "Producing"; // Status text
            }
        }

        public int[] FormatTime(float totalSeconds)
        {
            int hours = (int)(totalSeconds / 3600);
            int minutes = (int)((totalSeconds % 3600) / 60);
            int seconds = (int)((totalSeconds % 3600) % 60);

            return new int[] { hours, minutes, seconds };
        }
    }

    public class AreaSoundManager : MonoBehaviour
    {
        public AudioClip clip;
        private AudioClip startClip;
        private AudioClip workingClip;
        private AudioClip endClip;
        private AudioSource source;

        public bool hasStart;
        public bool hasEnd;

        public Vector2 startRange;
        public Vector2 workingRange;
        public Vector2 endRange;

        private bool starting;
        private double workingStartTime;

        public void Begin()
        {
            if(source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (workingClip == null)
            {
                if (hasStart)
                {
                    int startSamplesLength = (int)(clip.frequency * (startRange.y - startRange.x));
                    startClip = AudioClip.Create("StartClip", startSamplesLength, clip.channels, clip.frequency, false);
                    float[] startData = new float[startSamplesLength * clip.channels];
                    clip.GetData(startData, (int)(clip.frequency * startRange.x));
                    startClip.SetData(startData, 0);
                }

                int workingSamplesLength = (int)(clip.frequency * (workingRange.y - workingRange.x));
                workingClip = AudioClip.Create("WorkingClip", workingSamplesLength, clip.channels, clip.frequency, false);
                float[] workingData = new float[workingSamplesLength * clip.channels];
                clip.GetData(workingData, (int)(clip.frequency * workingRange.x));
                workingClip.SetData(workingData, 0);

                if (hasEnd)
                {
                    int endSamplesLength = (int)(clip.frequency * (endRange.y - endRange.x));
                    endClip = AudioClip.Create("EndClip", endSamplesLength, clip.channels, clip.frequency, false);
                    float[] endData = new float[endSamplesLength * clip.channels];
                    clip.GetData(endData, (int)(clip.frequency * endRange.x));
                    endClip.SetData(endData, 0);
                }
            }

            if (hasStart)
            {
                source.clip = startClip;
                workingStartTime = AudioSettings.dspTime + startClip.length;
                starting = true;
            }
            else
            {
                source.clip = workingClip;
                source.loop = true;
            }
            source.Play();
        }

        public void Update()
        {
            if (starting)
            {
                if(AudioSettings.dspTime+0.01f >= workingStartTime)
                {
                    source.clip = workingClip;
                    source.loop = true;
                    source.Play();
                    starting = false;
                }
            }
        }

        public void End()
        {
            if (hasEnd)
            {
                source.clip = endClip;
                source.loop = false;
                source.Play();
            }
            else
            {
                source.Stop();
            }
            starting = false;
        }

        public void Reset()
        {
            startClip = null;
            workingClip = null;
            endClip = null;
        }
    }
}
