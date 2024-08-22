﻿using FistVR;
using H3MP;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class HideoutController : UIController
    {
        public static HideoutController instance;

        public enum FinishRaidState
        {
            Survived,

            // These are fails
            RunThrough,
            MIA,
            KIA
        }

        // Objects
        public Transform spawn;
        public AreaController areaController;
        public Transform scavReturnNode;
        public Switch[] switches; // UI, Trade, Light

        // UI
        public Button loadButton;
        public Button[] loadButtons;
        public GameObject[] pages;
        public int pageIndex;
        public GameObject scavBlock;
        public Text raidCountdownTitle;
        public Text timeChoice0;
        public Text timeChoice1;
        public Text confirmChosenCharacter;
        public Text confirmChosenMap;
        public Text confirmChosenTime;
        public Text loadingChosenCharacter;
        public Text loadingChosenMap;
        public Text loadingChosenTime;
        public AudioSource clickAudio;
        public Transform medicalScreen;
        public Transform medicalScreenPartImagesParent;
        public Transform[] medicalScreenPartImages;
        public Text[] medicalScreenPartHealthTexts;
        public Text medicalScreenTotalHealthText;
        public Text totalTreatmentPriceText;
        public Text scavTimerText;
        public Collider scavButtonCollider;
        public Text scavButtonText;
        public GameObject charChoicePanel;
        public GameObject[] saveConfirmTexts;
        public Transform[] optionPages;
        // H3MP
        public GameObject instancesPage;
        public Transform instancesParent;
        public GameObject instancesNextButton;
        public GameObject instancesPreviousButton;
        public GameObject instanceEntryPrefab;
        public GameObject newInstance0Page;
        public Text newInstancePMCCheckText;
        public Text newInstanceScavCheckText;
        public Text newInstanceSpawnTogetherCheckText;
        public Text timeChoice0CheckText;
        public Text timeChoice1CheckText;
        public GameObject newInstance1Page;
        public Transform newInstanceMapListParent;
        public GameObject newInstanceMapListEntryPrefab;
        public GameObject newInstancePreviousButton;
        public GameObject newInstanceNextButton;
        public GameObject joinInstancePage;
        public Text joinInstanceTitle;
        public Text joinInstancePMCCheckText;
        public Text joinInstanceScavCheckText;
        public GameObject waitingServerPage;
        public GameObject waitingInstancePage;
        public Text waitingInstanceCharText;
        public Text waitingInstanceMapText;
        public Text waitingInstanceSpawnTogetherText;
        public Text waitingInstanceTimeText;
        public GameObject waitingInstancePlayerListPage;
        public Transform waitingInstancePlayerListParent;
        public GameObject waitingInstancePlayerListEntryPrefab;
        public GameObject waitingInstanceStartButton;
        public GameObject loadingPage;
        public Text loadingPercentageText;
        public GameObject waitForDeployPage;
        public Text waitForDeployCountText;
        public GameObject countdownPage;
        public Text countdownText;
        public GameObject cancelPage;

        // Assets
        public static GameObject areaCanvasPrefab; // AreaCanvas
        public static GameObject areaCanvasBottomButtonPrefab; // AreaCanvasBottomButton
        public static GameObject areaRequirementPrefab; // AreaRequirement
        public static GameObject itemRequirementPrefab; // ItemRequirement
        public static GameObject skillRequirementPrefab; // SkillRequirement
        public static GameObject traderRequirementPrefab; // TraderRequirement
        public static GameObject areaRequirementsPrefab; // AreaRequirements
        public static GameObject itemRequirementsPrefab; // ItemRequirements
        public static GameObject skillRequirementsPrefab; // SkillRequirements
        public static GameObject traderRequirementsPrefab; // TraderRequirements
        public static GameObject bonusPrefab; // Bonus
        public static Sprite areaBackgroundNormalSprite; // area_icon_default_back
        public static Sprite areaBackgroundLockedSprite; // area_icon_locked_back
        public static Sprite areaBackgroundAvailableSprite; // area_icon_default_back_green
        public static Sprite areaBackgroundEliteSprite; // area_icon_elite_back
        public static Sprite areaStatusIconUpgrading; // icon_status_upgrading
        public static Sprite areaStatusIconConstructing; // icon_status_constructing
        public static Sprite areaStatusIconLocked; // icon_lock
        public static Sprite areaStatusIconUnlocked; // icon_status_unlocked
        public static Sprite areaStatusIconReadyUpgrade; // icon_status_ready_to_upgrade
        public static Sprite areaStatusIconProducing; // icon_status_producing
        public static Sprite areaStatusIconOutOfFuel; // icon_out_of_fuel
        public static Sprite requirementFulfilled; // icon_requirement_fulfilled
        public static Sprite requirementLocked; // icon_requirement_locked
        public static Sprite[] traderAvatars; // avatar_russian_small, avatar_therapist_small, avatar_fence_small, avatar_ah_small, avatar_peacekeeper_small, avatar_tech_small, avatar_ragman_small, avatar_jaeger_small
        public static Sprite[] areaIcons; // icon_vents, icon_security, icon_watercloset, icon_stash, icon_generators, icon_heating, icon_rain_collector, icon_medstation, icon_kitchen, icon_restplace, icon_workbench, icon_intelligence_center, icon_shooting_range, icon_library, icon_scav_case, icon_illumination, icon_placeoffame, icon_afu, icon_solarpower, icon_boozegen, icon_bitcionfarm, icon_christmas_illumination
        public static Dictionary<string, Sprite> bonusIcons;
        public static Sprite emptyItemSlotIcon;
        public static Sprite dollarCurrencySprite;
        public static Sprite euroCurrencySprite;
        public static Sprite roubleCurrencySprite;
        public static Sprite barterSprite;
        public static Sprite experienceSprite;
        public static Sprite standingSprite;
        public static AudioClip[,] areaProductionSounds;
        public static AudioClip[] areaSlotSounds;
        public static AudioClip generatorLevel1And2Audio;
        public static AudioClip generatorLevel3Audio;
        public static AudioClip medStationLevel3Audio;
        public static AudioClip kitchenPotAudio;
        public static AudioClip kitchenFridgeAudio;
        public static AudioClip[] restSpaceTracks;
        public static AudioClip restSpacePSAudio;
        public static AudioClip intelCenterPCAudio;
        public static AudioClip intelCenterHDDAudio;
        public static AudioClip AFUAudio;
        public static AudioClip boozeGenAudio;
        public static AudioClip bitcoinFarmAudio;

        // Live data
        public static float[] defaultHealthRates;
        public static float defaultEnergyRate;
        public static float defaultHydrationRate;
        public static DateTime saveTime;
        public static double secondsSinceSave;
        public float time;
        private bool cancelRaidLoad;
        private bool loadingRaid;
        public bool countdownDeploy;
        public bool waitForDeploy;
        public float deployTimer;
        public float deployTime = 0; // TODO: Should be 10 but set to 0 for faster debugging
        private int insuredSetIndex = 0;
        private float defaultScavTime = 900; // seconds, 15m
        private float scavTimer;
        public static Dictionary<string, int> inventory;
        public static Dictionary<string, int> FIRInventory;
        public Dictionary<string, List<MeatovItem>> inventoryItems;
        public Dictionary<string, List<MeatovItem>> FIRInventoryItems;
        public Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> ammoBoxesByRoundClassByRoundType;
        private Dictionary<int, int[]> fullPartConditions;
        private Dictionary<int, GameObject> medicalPartElements;
        private int totalMedicalTreatmentPrice;
        public MarketManager marketManager;
        public static bool inMarket; // whether we are currently in market mode or in area UI mode
        public GCManager GCManager;
        public int instancesListPage;
        public int newInstanceMapListPage;
        public int waitingPlayerListPage;
        public bool charChoicePMC; // false is Scav
        public bool timeChoiceIs0; // false is 1
        public bool PMCSpawnTogether;
        public string mapChoiceName;

        public delegate void OnHideoutInventoryChangedDelegate();
        public event OnHideoutInventoryChangedDelegate OnHideoutInventoryChanged;
        public delegate void OnHideoutItemInventoryChangedDelegate(MeatovItemData itemData, int difference);
        public event OnHideoutItemInventoryChangedDelegate OnHideoutItemInventoryChanged;

        public override void Awake()
        {
            Mod.LogInfo("Hideout awake");
            base.Awake();

            instance = this;

            Mod.currentLocationIndex = 1;

            // TP Player
            GM.CurrentMovementManager.TeleportToPoint(spawn.position, false, spawn.rotation.eulerAngles);

            Mod.dead = false;

            Mod.LogInfo("\t0");
            if (StatusUI.instance == null)
            {
                SetupPlayerRig();
            }

            ammoBoxesByRoundClassByRoundType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>>();

            // Set live data
            //float totalMaxHealth = 0;
            //for (int i = 0; i < 7; ++i)
            //{
            //    Mod.currentMaxHealth[i] = Mod.defaultMaxHealth[i];
            //    totalMaxHealth += Mod.currentMaxHealth[i];
            //    Mod.currentHealthRates[i] += Mod.hideoutHealthRates[i];
            //}
            //GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);
            //if (Mod.justFinishedRaid)
            //{
            //    Mod.currentEnergyRate -= Mod.raidEnergyRate;
            //    Mod.currentHydrationRate -= Mod.raidHydrationRate;
            //}
            //Mod.currentEnergyRate += Mod.hideoutEnergyRate;
            //Mod.currentHydrationRate += Mod.hideoutHydrationRate;
            //if (currentSkillGroupLevelingBoosts == null)
            //{
            //    currentSkillGroupLevelingBoosts = new Dictionary<Skill.SkillType, float>();
            //}

            Mod.LogInfo("\t0");
            // Spawn areas
            Dictionary<int, Area> areas = new Dictionary<int, Area>();
            int highestIndex = -1;
            for(int i=0; i< Mod.hideoutAreaBundles.Length; ++i)
            {
                Mod.LogInfo("\t\tArea bundle "+i);
                string[] areaNames = Mod.hideoutAreaBundles[i].GetAllAssetNames();
                for(int j = 0; j < areaNames.Length; ++j)
                {
                    Mod.LogInfo("\t\tArea: " + areaNames[j]);
                    string areaName = areaNames[j];
                    string[] split = areaName.Split('_')[0].Split('/');
                    int areaIndex = int.Parse(split[split.Length - 1]);
                    Area area = Instantiate(Mod.hideoutAreaBundles[i].LoadAsset<GameObject>(areaName)).GetComponent<Area>();
                    areas.Add(areaIndex, area);
                    if(areaIndex > highestIndex)
                    {
                        highestIndex = areaIndex;
                    }

                    area.UI = Instantiate(areaCanvasPrefab, area.UIRoot).GetComponent<AreaUI>();
                    area.UI.area = area;
                    switches[0].gameObjects.Add(area.UI.gameObject);
                    switches[1].negativeGameObjects.Add(area.UI.transform.parent.gameObject);
                    area.controller = areaController;

                    for(int k=0; k < area.levels.Length; ++k)
                    {
                        if (area.levels[k].areaUpgradeCheckProcessors != null && area.levels[k].areaUpgradeCheckProcessors.Length > 0)
                        {
                            area.levels[k].areaUpgradeCheckProcessors[0].areaUI = area.UI;
                            area.levels[k].areaUpgradeCheckProcessors[1].areaUI = area.UI;
                        }
                    }

                    // Illumination specific
                    if(areaIndex == 15)
                    {
                        switches[2].gameObjects = new List<GameObject>(area.objectsToToggle);
                    }
                }
            }
            areaController.areas = new Area[highestIndex + 1];
            foreach (KeyValuePair<int, Area> areaEntry in areas)
            {
                areaController.areas[areaEntry.Key] = areaEntry.Value;
            }
            for(int i=0; i < areaController.areas.Length; ++i)
            {
                if(areaController.areas[i] != null)
                {
                    areaController.areas[i].LoadStaticData();
                }
            }

            Mod.LogInfo("\t0");
            ProcessData();

            Mod.LogInfo("\t0");
            InitUI();

            Mod.LogInfo("\t0");
            InitTime();

            Mod.LogInfo("\t0");
            if (Mod.justFinishedRaid && Mod.chosenCharIndex == 0)
            {
                FinishRaid(Mod.raidState); // This will save on autosave

                // Set any parts health to 1 if they are at 0
                for (int i = 0; i < 7; ++i)
                {
                    if (Mod.GetHealth(i) == 0)
                    {
                        Mod.SetHealth(i, 1);
                    }
                }

                // Add movement skill exp
                Mod.AddSkillExp(Mod.distanceTravelledSprinting * Skill.sprintAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(Mod.distanceTravelledSprinting * UnityEngine.Random.Range(Skill.sprintActionMin, Skill.sprintActionMax), 1);
                }
                Mod.AddSkillExp(Mod.distanceTravelledWalking * Skill.movementAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(Mod.distanceTravelledWalking * UnityEngine.Random.Range(Skill.movementActionMin, Skill.movementActionMax), 1);
                }
            }
            Mod.LogInfo("\t0");

            // Give any existing rewards to player now
            //if (Mod.rewardsToGive != null && Mod.rewardsToGive.Count > 0)
            //{
            //    foreach (List<TraderTaskReward> rewards in Mod.rewardsToGive)
            //    {
            //        marketManager.GivePlayerRewards(rewards);
            //    }
            //    Mod.rewardsToGive = null;
            //}

            if (Mod.justFinishedRaid)
            {
                // Remove raid rates
                // TODO: // Will need to have RaidController setup
            }

            // Add hideout rates
            for(int i=0; i<Mod.GetHealthCount(); ++i)
            {
                Mod.SetBasePositiveHealthRate(i, Mod.GetBasePositiveHealthRate(i) + defaultHealthRates[i]);
            }
            Mod.baseEnergyRate += defaultEnergyRate;
            Mod.baseHydrationRate += defaultHydrationRate;

            Mod.justFinishedRaid = false;
            init = true;
            Mod.LogInfo("\t0");
        }

        public override void Update()
        {
            base.Update();

            if (init)
            {
                UpdateTime();
            }

            //UpdateScavTimer();

            Effect.UpdateStatic();

            //UpdateInsuredSets();

            UpdateRates();

            // Handle raid loading process
            TODO e: // review single player loading process to ensure it fuctions still, and use the new pages and stuff as well
            if (cancelRaidLoad)
            {
                loadingRaid = false;
                countdownDeploy = false;
                waitForDeploy = false;

                // Wait until the raid map is done loading before unloading it
                bool doneLoading = true;
                doneLoading &= Mod.raidMapBundleRequest.isDone;
                if (Mod.raidMapAdditiveBundleRequests != null)
                {
                    for (int i = 0; i < Mod.raidMapAdditiveBundleRequests.Count; ++i)
                    {
                        doneLoading &= Mod.raidMapAdditiveBundleRequests[i].isDone;
                    }
                }
                if (Mod.raidMapPrefabBundleRequests != null)
                {
                    for (int i = 0; i < Mod.raidMapPrefabBundleRequests.Count; ++i)
                    {
                        doneLoading &= Mod.raidMapPrefabBundleRequests[i].isDone;
                    }
                }
                if (doneLoading)
                {
                    if (Mod.raidMapBundleRequest.assetBundle != null)
                    {
                        Mod.raidMapBundleRequest.assetBundle.Unload(true);
                    }
                    if (Mod.raidMapAdditiveBundleRequests != null)
                    {
                        for (int i = 0; i < Mod.raidMapAdditiveBundleRequests.Count; ++i)
                        {
                            if(Mod.raidMapAdditiveBundleRequests[i].assetBundle != null)
                            {
                                Mod.raidMapAdditiveBundleRequests[i].assetBundle.Unload(true);
                            }
                        }
                    }
                    if (Mod.raidMapPrefabBundleRequests != null)
                    {
                        for (int i = 0; i < Mod.raidMapPrefabBundleRequests.Count; ++i)
                        {
                            if (Mod.raidMapPrefabBundleRequests[i].assetBundle != null)
                            {
                                Mod.raidMapPrefabBundleRequests[i].assetBundle.Unload(true);
                            }
                        }
                    }
                    cancelRaidLoad = false;
                    cancelPage.SetActive(false);
                    SetPage(0);
                }
            }
            else if (countdownDeploy)
            {
                deployTimer -= Time.deltaTime;

                countdownText.text = Mod.FormatTimeString(deployTimer);

                if (deployTimer <= 0)
                {
                    // Autosave before starting the raid
                    Mod.saveSlotIndex = 5;
                    Save();

                    try
                    {
                        SteamVR_LoadLevel.Begin(mapChoiceName, false, 0.5f, 0f, 0f, 0f, 1f);
                        countdownDeploy = false;
                    }
                    catch(Exception ex)
                    {
                        Mod.LogError("Could not load level: "+mapChoiceName+", cancelling: "+ ex.Message+"\n"+ ex.StackTrace);
                        cancelRaidLoad = true;
                    }
                }
            }
            else if (waitForDeploy)
            {
                int readyCount = 0;
                for(int i=0; i < Networking.currentInstance.players.Count; ++i)
                {
                    if (Networking.currentInstance.readyPlayers.Contains(Networking.currentInstance.players[i]))
                    {
                        ++readyCount;
                    }
                }

                if (readyCount == Networking.currentInstance.players.Count)
                {
                    waitForDeployPage.SetActive(false);
                    waitForDeploy = false;
                    countdownPage.SetActive(true);
                    countdownDeploy = true;
                }
                else
                {
                    waitForDeployCountText.text = readyCount.ToString() + "/" + Networking.currentInstance.players.Count;
                }
            }
            else if (loadingRaid)
            {
                bool doneLoading = true;
                float progress = 0;
                doneLoading &= Mod.raidMapBundleRequest.isDone;
                progress += Mod.raidMapBundleRequest.progress;
                if (Mod.raidMapAdditiveBundleRequests != null)
                {
                    for(int i=0; i < Mod.raidMapAdditiveBundleRequests.Count; ++i)
                    {
                        doneLoading &= Mod.raidMapAdditiveBundleRequests[i].isDone;
                        progress += Mod.raidMapAdditiveBundleRequests[i].progress;
                    }
                }
                if (Mod.raidMapPrefabBundleRequests != null)
                {
                    for(int i=0; i < Mod.raidMapPrefabBundleRequests.Count; ++i)
                    {
                        doneLoading &= Mod.raidMapPrefabBundleRequests[i].isDone;
                        progress += Mod.raidMapPrefabBundleRequests[i].progress;
                    }
                }
                progress /= (Mod.raidMapAdditiveBundleRequests == null ? 0 : Mod.raidMapAdditiveBundleRequests.Count) + (Mod.raidMapPrefabBundleRequests == null ? 0 : Mod.raidMapPrefabBundleRequests.Count) + 1;

                if (doneLoading)
                {
                    if (Mod.raidMapBundleRequest.assetBundle != null)
                    {
                        deployTimer = deployTime;

                        loadingRaid = false;
                        loadingPage.SetActive(false);
                        if (Networking.currentInstance == null || Networking.currentInstance.AllPlayersReady())
                        {
                            countdownPage.SetActive(true);
                            countdownDeploy = true;
                        }
                        else
                        {
                            waitForDeployPage.SetActive(true);
                            waitForDeploy = true;
                        }
                    }
                    else
                    {
                        Mod.LogError("Could not load raid map bundle, cancelling");
                        cancelRaidLoad = true;
                    }
                }
                else
                {
                    loadingPercentageText.text = (progress * 100).ToString() + "%";
                }
            }
        }

        public void UpdateRates()
        {
            for(int i=0; i < Mod.GetHealthCount(); ++i)
            {
                // Min ensures we cannot lose health in hideout, but we cannot gain any if negative health rate is greater than positive
                Mod.SetHealth(i, Mathf.Max(0, Mathf.Min(Mod.GetCurrentMaxHealth(i), Mod.GetHealth(i) + Mathf.Min(0, Mod.GetCurrentHealthRate(i) + Mod.GetCurrentNonLethalHealthRate(i)) * Time.deltaTime)));
            }
            Mod.hydration = Mathf.Max(0, Mathf.Min(Mod.currentMaxHydration, Mod.hydration + Mod.currentHydrationRate * Time.deltaTime));
            Mod.energy = Mathf.Max(0, Mathf.Min(Mod.currentMaxEnergy, Mod.energy + Mod.currentEnergyRate * Time.deltaTime));
        }

        public void SetPage(int index)
        {
            pages[pageIndex].SetActive(false);
            pages[index].SetActive(true);
            pageIndex = index;
        }

        private void UpdateScavTimer()
        {
            scavTimer -= Time.deltaTime;
            if (scavTimer <= 0)
            {
                // Enable Scav button, disable scav timer text
                scavButtonCollider.enabled = true;
                scavButtonText.color = Color.white;
                scavTimerText.gameObject.SetActive(false);
            }
            else if (charChoicePanel.activeSelf)
            {
                // Update scav timer text
                scavTimerText.text = Mod.FormatTimeString(scavTimer);

                if (!scavTimerText.gameObject.activeSelf)
                {
                    // Disable Scav button, Enable scav timer text
                    scavButtonCollider.enabled = false;
                    scavButtonText.color = Color.grey;
                    scavTimerText.gameObject.SetActive(true);
                }
            }
        }

        //private void UpdateInsuredSets()
        //{
        //    if (Mod.insuredItems != null && Mod.insuredItems.Count > 0)
        //    {
        //        if (insuredSetIndex >= Mod.insuredItems.Count)
        //        {
        //            insuredSetIndex = 0;
        //        }

        //        if (Mod.insuredItems[insuredSetIndex].returnTime <= GetTimeSeconds())
        //        {
        //            foreach (KeyValuePair<string, int> insuredToSpawn in Mod.insuredItems[insuredSetIndex].items)
        //            {
        //                marketManager.SpawnItem(insuredToSpawn.Key, insuredToSpawn.Value);
        //            }

        //            Mod.insuredItems[insuredSetIndex] = Mod.insuredItems[Mod.insuredItems.Count - 1];
        //            Mod.insuredItems.RemoveAt(insuredSetIndex);
        //        }
        //        else
        //        {
        //            ++insuredSetIndex;
        //        }
        //    }
        //}

        //private void UpdateEffects()
        //{
        //    // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
        //    bool[] heal = new bool[7];
        //    for (int i = 0; i < 7; ++i)
        //    {
        //        heal[i] = true;
        //    }
        //    for (int i = Effect.effects.Count; i >= 0; --i)
        //    {
        //        if (Effect.effects.Count == 0)
        //        {
        //            break;
        //        }
        //        else if (i >= Effect.effects.Count)
        //        {
        //            continue;
        //        }

        //        Effect effect = Effect.effects[i];
        //        if (effect.active)
        //        {
        //            if (effect.hasTimer)
        //            {
        //                effect.timer -= Time.deltaTime;
        //                if (effect.timer <= 0)
        //                {
        //                    effect.active = false;

        //                    // Unapply effect
        //                    switch (effect.effectType)
        //                    {
        //                        case Effect.EffectType.SkillRate:
        //                            Mod.skills[effect.skillIndex].currentProgress -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.EnergyRate:
        //                            Mod.currentEnergyRate -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.HydrationRate:
        //                            Mod.currentHydrationRate -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.MaxStamina:
        //                            Mod.currentMaxStamina -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.StaminaRate:
        //                            Mod.currentStaminaEffect -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.HandsTremor:
        //                            // TODO: Stop tremors if there are not other tremor effects
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.QuantumTunnelling:
        //                            // TODO: Stop QuantumTunnelling
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.HealthRate:
        //                            float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
        //                            if (effect.partIndex == -1)
        //                            {
        //                                for (int j = 0; j < 7; ++j)
        //                                {
        //                                    arrayToUse[j] -= effect.value / 7;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                arrayToUse[effect.partIndex] -= effect.value;
        //                            }
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.RemoveAllBloodLosses:
        //                            // Reactivate all bleeding 
        //                            // Not necessary because when we disabled them we used the disable timer
        //                            break;
        //                        case Effect.EffectType.Contusion:
        //                            bool otherContusions = false;
        //                            foreach (Effect contusionEffectCheck in Effect.effects)
        //                            {
        //                                if (contusionEffectCheck.active && contusionEffectCheck.effectType == Effect.EffectType.Contusion)
        //                                {
        //                                    otherContusions = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!otherContusions)
        //                            {
        //                                // Enable haptic feedback
        //                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Enabled;
        //                                // TODO: also set volume to full
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                                }
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.WeightLimit:
        //                            Mod.effectWeightLimitBonus -= effect.value * 1000;
        //                            Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.DamageModifier:
        //                            Mod.currentDamageModifier -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.Pain:
        //                            // Remove all tremors caused by this pain and disable tremors if no other tremors active
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasPainTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasPainTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasPainTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.StomachBloodloss:
        //                            --Mod.stomachBloodLossCount;
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.UnknownToxin:
        //                            // Remove all effects caused by this toxin
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                if (causedEffect.effectType == Effect.EffectType.HealthRate)
        //                                {
        //                                    for (int j = 0; j < 7; ++j)
        //                                    {
        //                                        Mod.currentHealthRates[j] -= causedEffect.value / 7;
        //                                    }
        //                                }
        //                                // Could go two layers deep
        //                                foreach (Effect causedCausedEffect in effect.caused)
        //                                {
        //                                    Effect.effects.Remove(causedCausedEffect);
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasToxinTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasToxinTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasToxinTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.BodyTemperature:
        //                            Mod.temperatureOffset -= effect.value;
        //                            break;
        //                        case Effect.EffectType.Antidote:
        //                            // Will remove toxin on ativation, does nothing after
        //                            break;
        //                        case Effect.EffectType.LightBleeding:
        //                        case Effect.EffectType.HeavyBleeding:
        //                            // Remove all effects caused by this bleeding
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                if (causedEffect.effectType == Effect.EffectType.HealthRate)
        //                                {
        //                                    if (causedEffect.partIndex == -1)
        //                                    {
        //                                        for (int j = 0; j < 7; ++j)
        //                                        {
        //                                            Mod.currentNonLethalHealthRates[j] -= causedEffect.value;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
        //                                    }
        //                                }
        //                                else // Energy rate
        //                                {
        //                                    Mod.currentEnergyRate -= causedEffect.value;
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.Fracture:
        //                            // Remove all effects caused by this fracture
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                // Could go two layers deep
        //                                foreach (Effect causedCausedEffect in effect.caused)
        //                                {
        //                                    Effect.effects.Remove(causedCausedEffect);
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasFractureTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasFractureTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasFractureTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                    }

        //                    Effect.effects.RemoveAt(i);

        //                    continue;
        //                }
        //            }

        //            if (effect.active && effect.partIndex != -1 && (effect.effectType == Effect.EffectType.LightBleeding ||
        //                                                           effect.effectType == Effect.EffectType.HeavyBleeding ||
        //                                                           (effect.effectType == Effect.EffectType.HealthRate && effect.value < 0)))
        //            {
        //                heal[effect.partIndex] = false;
        //            }
        //        }
        //        else
        //        {
        //            bool effectJustActivated = false;
        //            if (effect.delay > 0)
        //            {
        //                effect.delay -= Time.deltaTime;
        //            }
        //            else if (effect.inactiveTimer <= 0)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }
        //            if (effect.inactiveTimer > 0)
        //            {
        //                effect.inactiveTimer -= Time.deltaTime;
        //            }
        //            else if (effect.delay <= 0)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }
        //            if (effect.hideoutOnly)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }

        //            // Apply effect if it just started being active
        //            if (effectJustActivated)
        //            {
        //                switch (effect.effectType)
        //                {
        //                    case Effect.EffectType.SkillRate:
        //                        Mod.skills[effect.skillIndex].currentProgress += effect.value;
        //                        break;
        //                    case Effect.EffectType.EnergyRate:
        //                        Mod.currentEnergyRate += effect.value;
        //                        break;
        //                    case Effect.EffectType.HydrationRate:
        //                        Mod.currentHydrationRate += effect.value;
        //                        break;
        //                    case Effect.EffectType.MaxStamina:
        //                        Mod.currentMaxStamina += effect.value;
        //                        break;
        //                    case Effect.EffectType.StaminaRate:
        //                        Mod.currentStaminaEffect += effect.value;
        //                        break;
        //                    case Effect.EffectType.HandsTremor:
        //                        // TODO: Begin tremors if there isnt already another active one
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.QuantumTunnelling:
        //                        // TODO: Begin quantumtunneling if there isnt already another active one
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HealthRate:
        //                        float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
        //                        if (effect.partIndex == -1)
        //                        {
        //                            for (int j = 0; j < 7; ++j)
        //                            {
        //                                arrayToUse[j] += effect.value / 7;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            arrayToUse[effect.partIndex] += effect.value;
        //                        }
        //                        break;
        //                    case Effect.EffectType.RemoveAllBloodLosses:
        //                        // Deactivate all bleeding using disable timer
        //                        foreach (Effect bleedEffect in Effect.effects)
        //                        {
        //                            if (bleedEffect.effectType == Effect.EffectType.LightBleeding || bleedEffect.effectType == Effect.EffectType.HeavyBleeding)
        //                            {
        //                                bleedEffect.active = false;
        //                                bleedEffect.inactiveTimer = effect.timer;

        //                                // Unapply the healthrate caused by this bleed
        //                                Effect causedHealthRate = bleedEffect.caused[0];
        //                                if (causedHealthRate.nonLethal)
        //                                {
        //                                    Mod.currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
        //                                }
        //                                else
        //                                {
        //                                    Mod.currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
        //                                }
        //                                Effect causedEnergyRate = bleedEffect.caused[1];
        //                                Mod.currentEnergyRate -= causedEnergyRate.value;
        //                                bleedEffect.caused.Clear();
        //                                Effect.effects.Remove(causedHealthRate);
        //                                Effect.effects.Remove(causedEnergyRate);
        //                            }
        //                        }
        //                        break;
        //                    case Effect.EffectType.Contusion:
        //                        // Disable haptic feedback
        //                        GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
        //                        // TODO: also set volume to 0.33 * volume
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.WeightLimit:
        //                        Mod.effectWeightLimitBonus += effect.value * 1000;
        //                        Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
        //                        break;
        //                    case Effect.EffectType.DamageModifier:
        //                        Mod.currentDamageModifier += effect.value;
        //                        break;
        //                    case Effect.EffectType.Pain:
        //                        if (UnityEngine.Random.value < 1 - (Mod.skills[4].currentProgress / 10000))
        //                        {
        //                            // Add a tremor effect
        //                            Effect newTremor = new Effect();
        //                            newTremor.effectType = Effect.EffectType.HandsTremor;
        //                            newTremor.delay = 5;
        //                            newTremor.hasTimer = effect.hasTimer;
        //                            newTremor.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                            - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                            Effect.effects.Add(newTremor);
        //                            effect.caused.Add(newTremor);
        //                        }

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(true);
        //                        }

        //                        Mod.AddSkillExp(Skill.stressResistanceHealthNegativeEffect, 4);
        //                        break;
        //                    case Effect.EffectType.StomachBloodloss:
        //                        ++Mod.stomachBloodLossCount;
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.UnknownToxin:
        //                        // Add a pain effect
        //                        Effect newToxinPain = new Effect();
        //                        newToxinPain.effectType = Effect.EffectType.Pain;
        //                        newToxinPain.delay = 5;
        //                        newToxinPain.hasTimer = effect.hasTimer;
        //                        newToxinPain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                           - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newToxinPain.partIndex = 0;
        //                        Effect.effects.Add(newToxinPain);
        //                        effect.caused.Add(newToxinPain);
        //                        // Add a health rate effect
        //                        Effect newToxinHealthRate = new Effect();
        //                        newToxinHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newToxinHealthRate.delay = 5;
        //                        newToxinHealthRate.value = -25 + 25 * (Skill.immunityPoisonBuff * (Mod.skills[6].currentProgress / 100) / 100);
        //                        newToxinHealthRate.hasTimer = effect.hasTimer;
        //                        newToxinHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        Effect.effects.Add(newToxinHealthRate);
        //                        effect.caused.Add(newToxinHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.BodyTemperature:
        //                        Mod.temperatureOffset += effect.value;
        //                        break;
        //                    case Effect.EffectType.Antidote:
        //                        // Will remove toxin on ativation, does nothing after
        //                        for (int j = Effect.effects.Count; j >= 0; --j)
        //                        {
        //                            if (Effect.effects[j].effectType == Effect.EffectType.UnknownToxin)
        //                            {
        //                                Effect.effects.RemoveAt(j);
        //                                break;
        //                            }
        //                        }
        //                        break;
        //                    case Effect.EffectType.LightBleeding:
        //                        // Add a health rate effect
        //                        Effect newLightBleedingHealthRate = new Effect();
        //                        newLightBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newLightBleedingHealthRate.delay = 5;
        //                        newLightBleedingHealthRate.value = -8;
        //                        newLightBleedingHealthRate.hasTimer = effect.hasTimer;
        //                        newLightBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newLightBleedingHealthRate.partIndex = effect.partIndex;
        //                        newLightBleedingHealthRate.nonLethal = true;
        //                        Effect.effects.Add(newLightBleedingHealthRate);
        //                        effect.caused.Add(newLightBleedingHealthRate);
        //                        // Add a energy rate effect
        //                        Effect newLightBleedingEnergyRate = new Effect();
        //                        newLightBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newLightBleedingEnergyRate.delay = 5;
        //                        newLightBleedingEnergyRate.value = -5;
        //                        newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
        //                        newLightBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newLightBleedingEnergyRate.partIndex = effect.partIndex;
        //                        Effect.effects.Add(newLightBleedingEnergyRate);
        //                        effect.caused.Add(newLightBleedingEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyBleeding:
        //                        // Add a health rate effect
        //                        Effect newHeavyBleedingHealthRate = new Effect();
        //                        newHeavyBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyBleedingHealthRate.delay = 5;
        //                        newHeavyBleedingHealthRate.value = -13.5f;
        //                        newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
        //                        newHeavyBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newHeavyBleedingHealthRate.nonLethal = true;
        //                        Effect.effects.Add(newHeavyBleedingHealthRate);
        //                        effect.caused.Add(newHeavyBleedingHealthRate);
        //                        // Add a energy rate effect
        //                        Effect newHeavyBleedingEnergyRate = new Effect();
        //                        newHeavyBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newHeavyBleedingEnergyRate.delay = 5;
        //                        newHeavyBleedingEnergyRate.value = -6;
        //                        newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
        //                        newHeavyBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newHeavyBleedingEnergyRate.partIndex = effect.partIndex;
        //                        Effect.effects.Add(newHeavyBleedingEnergyRate);
        //                        effect.caused.Add(newHeavyBleedingEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Fracture:
        //                        // Add a pain effect
        //                        Effect newFracturePain = new Effect();
        //                        newFracturePain.effectType = Effect.EffectType.Pain;
        //                        newFracturePain.delay = 5;
        //                        newFracturePain.hasTimer = effect.hasTimer;
        //                        newFracturePain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                              - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        Effect.effects.Add(newFracturePain);
        //                        effect.caused.Add(newFracturePain);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Dehydration:
        //                        // Add a HealthRate effect
        //                        Effect newDehydrationHealthRate = new Effect();
        //                        newDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newDehydrationHealthRate.value = -60;
        //                        newDehydrationHealthRate.delay = 5;
        //                        newDehydrationHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newDehydrationHealthRate);
        //                        effect.caused.Add(newDehydrationHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyDehydration:
        //                        // Add a HealthRate effect
        //                        Effect newHeavyDehydrationHealthRate = new Effect();
        //                        newHeavyDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyDehydrationHealthRate.value = -350;
        //                        newHeavyDehydrationHealthRate.delay = 5;
        //                        newHeavyDehydrationHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newHeavyDehydrationHealthRate);
        //                        effect.caused.Add(newHeavyDehydrationHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Fatigue:
        //                        Mod.fatigue = true;

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyFatigue:
        //                        // Add a HealthRate effect
        //                        Effect newHeavyFatigueHealthRate = new Effect();
        //                        newHeavyFatigueHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyFatigueHealthRate.value = -30;
        //                        newHeavyFatigueHealthRate.delay = 5;
        //                        newHeavyFatigueHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newHeavyFatigueHealthRate);
        //                        effect.caused.Add(newHeavyFatigueHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.OverweightFatigue:
        //                        // Add a EnergyRate effect
        //                        Effect newOverweightFatigueEnergyRate = new Effect();
        //                        newOverweightFatigueEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newOverweightFatigueEnergyRate.value = -4;
        //                        newOverweightFatigueEnergyRate.delay = 5;
        //                        newOverweightFatigueEnergyRate.hasTimer = false;
        //                        Effect.effects.Add(newOverweightFatigueEnergyRate);
        //                        effect.caused.Add(newOverweightFatigueEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    // Apply health, energy, and hydration rates
        //    float healthDelta = 0;
        //    float health = 0;
        //    float maxHealthTotal = 0;
        //    for (int i = 0; i < 7; ++i)
        //    {
        //        maxHealthTotal += Mod.currentMaxHealth[i];
        //        float currentHealthDelta = Mod.currentHealthRates[i] + Mod.currentNonLethalHealthRates[i];
        //        if (heal[i] && currentHealthDelta > 0)
        //        {
        //            Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthDelta * (Time.deltaTime / 60), 1, Mod.currentMaxHealth[i]);

        //            healthDelta += currentHealthDelta;
        //            health += Mod.health[i];
        //        }
        //        StatusUI.instance.partHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
        //        StatusUI.instance.partHealthImages[i].color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);

        //        if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
        //        {
        //            medicalScreenPartHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
        //            medicalScreenPartImages[i].GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);
        //        }
        //    }
        //    if (healthDelta != 0)
        //    {
        //        if (!StatusUI.instance.healthDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.healthDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.healthDelta.text = (healthDelta > 0 ? "+" : "") + String.Format("{0:0.#}/min", healthDelta);
        //    }
        //    else if (StatusUI.instance.healthDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.healthDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.healthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal);
        //    if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
        //    {
        //        medicalScreenTotalHealthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal); ;
        //    }
        //    GM.CurrentPlayerBody.SetHealthThreshold(maxHealthTotal);
        //    GM.CurrentPlayerBody.Health = health; // This must be done after setting health threshold because setting health threshold also sets health

        //    if (Mod.currentHydrationRate > 0)
        //    {
        //        Mod.hydration = Mathf.Clamp(Mod.hydration + Mod.currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);

        //        if (!StatusUI.instance.hydrationDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.hydrationDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.hydrationDelta.text = (Mod.currentHydrationRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentHydrationRate);
        //    }
        //    else if (StatusUI.instance.hydrationDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.hydrationDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.hydrationText.text = String.Format("{0:0}/{1}", Mod.hydration, Mod.maxHydration);
        //    if (Mod.hydration > 20)
        //    {
        //        // Remove any dehydration effect
        //        if (Mod.dehydrationEffect != null)
        //        {
        //            // Disable 
        //            if (Mod.dehydrationEffect.caused.Count > 0)
        //            {
        //                for (int j = 0; j < 7; ++j)
        //                {
        //                    Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
        //                }
        //                Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
        //            }
        //            Effect.effects.Remove(Mod.dehydrationEffect);
        //        }

        //        if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //        {
        //            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(false);
        //        }
        //    }

        //    if (Mod.currentEnergyRate > 0)
        //    {
        //        Mod.energy = Mathf.Clamp(Mod.energy + Mod.currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);

        //        if (!StatusUI.instance.energyDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.energyDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.energyDelta.text = (Mod.currentEnergyRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentEnergyRate);
        //    }
        //    else if (StatusUI.instance.energyDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.energyDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.energyText.text = String.Format("{0:0}/{1}", Mod.energy, Mod.maxEnergy);
        //    if (Mod.energy > 20)
        //    {
        //        // Remove any fatigue effect
        //        if (Mod.fatigueEffect != null)
        //        {
        //            // Disable 
        //            if (Mod.fatigueEffect.caused.Count > 0)
        //            {
        //                for (int j = 0; j < 7; ++j)
        //                {
        //                    Mod.currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
        //                }
        //                Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
        //            }
        //            Effect.effects.Remove(Mod.fatigueEffect);
        //        }

        //        if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //        {
        //            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(false);
        //        }
        //    }
        //}

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * UIController.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            string formattedTime0 = Mod.FormatTimeString(time);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            string formattedTime1 = Mod.FormatTimeString(offsetTime);
            timeChoice1.text = formattedTime1;

            if (timeChoiceIs0)
            {
                waitingInstanceTimeText.text = "Time: " + formattedTime0;
            }
            else
            {
                waitingInstanceTimeText.text = "Time: " + formattedTime1;
            }

            confirmChosenTime.text = Mod.chosenTimeIndex == 0 ? formattedTime0 : formattedTime1;
            loadingChosenTime.text = confirmChosenTime.text;
        }

        private void SetupPlayerRig()
        {
            Mod.LogInfo("Setting up player rig, current player body null?: " +(GM.CurrentPlayerBody == null)+ ", current player root null?: " + (GM.CurrentPlayerBody == null));

            // Player status
            Instantiate(Mod.playerStatusUIPrefab, GM.CurrentPlayerRoot);
            // Consumable indicator
            Mod.consumeUI = Instantiate(Mod.consumeUIPrefab, GM.CurrentPlayerRoot).GetComponent<ConsumeUI>();
            Mod.consumeUI.gameObject.SetActive(false);
            // Stack split UI
            Mod.stackSplitUI = Instantiate(Mod.stackSplitUIPrefab, GM.CurrentPlayerRoot).GetComponent<StackSplitUI>();
            Mod.stackSplitUI.gameObject.SetActive(false);
            //// Extraction UI
            //Mod.extractionUI = Instantiate(Mod.extractionUIPrefab, GM.CurrentPlayerRoot);
            //Mod.extractionUIText = Mod.extractionUI.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
            //Mod.extractionUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            //Mod.extractionUI.SetActive(false);
            //// Extraction limit UI
            //Mod.extractionLimitUI = Instantiate(Mod.extractionLimitUIPrefab, GM.CurrentPlayerRoot);
            //Mod.extractionLimitUIText = Mod.extractionLimitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            //Mod.extractionLimitUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            //Mod.extractionLimitUI.SetActive(false);
            //// Stamina bar
            Mod.staminaBarUI = Instantiate(Mod.staminaBarPrefab, GM.CurrentPlayerBody.Head);
            Mod.staminaBarUI.transform.localRotation = Quaternion.Euler(-25, 0, 0);
            Mod.staminaBarUI.transform.localPosition = new Vector3(0, -0.4f, 0.6f);
            Mod.staminaBarUI.transform.localScale = Vector3.one * 0.0015f;

            // Add our own hand component to each hand
            Mod.rightHand = GM.CurrentPlayerBody.RightHand.gameObject.AddComponent<Hand>();
            Mod.leftHand = GM.CurrentPlayerBody.LeftHand.gameObject.AddComponent<Hand>();
            Mod.rightHand.otherHand = Mod.leftHand;
            Mod.leftHand.otherHand = Mod.rightHand;

            // Set movement control
            GM.CurrentMovementManager.Mode = FVRMovementManager.MovementMode.TwinStick;
            GM.Options.MovementOptions.Touchpad_Confirm = FVRMovementManager.TwoAxisMovementConfirm.OnTouch;
            GM.Options.ControlOptions.CCM = ControlOptions.CoreControlMode.Streamlined;

            // Disable wrist menus
            //Mod.rightHand.fvrHand.DisableWristMenu();
            //Mod.leftHand.fvrHand.DisableWristMenu();

            // Set QB config to pockets
            Mod.pocketSlots = new FVRQuickBeltSlot[4];
            Mod.itemsInPocketSlots = new MeatovItem[4];
            GM.CurrentPlayerBody.ConfigureQuickbelt(Mod.pocketsConfigIndex);
        }

        public void ProcessData()
        {
            Mod.LogInfo("Processdata");
            // Note that when hideout is loaded the scene is loaded and this is the only time ProcessData is called
            // If already in the scene, it will be reloaded
            // Certain elements of the hideout, like Areas, load their own data, so we don't need to do it here

            // Clear other active slots since we shouldn't have any on load
            if(Mod.looseRigSlots == null)
            {
                Mod.looseRigSlots = new List<List<RigSlot>>();
            }
            else
            {
                Mod.looseRigSlots.Clear();
            }

            // Load the save time
            saveTime = new DateTime((long)HideoutController.loadedData["time"]);
            secondsSinceSave = (float)DateTime.UtcNow.Subtract(saveTime).TotalSeconds;
            float minutesSinceSave = (float)secondsSinceSave / 60.0f;

            Mod.LogInfo("\t0");
            // Load player data only if we don't want to use the data from the PMC raid we just finished
            if (!Mod.justFinishedRaid || Mod.chosenCharIndex == 1)
            {
                Mod.LogInfo("\t\tNot finished raid");
                Mod.level = (int)loadedData["level"];
                Mod.experience = (int)loadedData["experience"];
                Mod.SetHealthArray(loadedData["health"].ToObject<float[]>());
                Mod.LogInfo("\t\t0");
                Mod.hydration = Mathf.Min((float)loadedData["hydration"] + Mod.currentHydrationRate * minutesSinceSave, Mod.currentMaxHydration);
                Mod.energy = Mathf.Min((float)loadedData["energy"] + Mod.currentEnergyRate * minutesSinceSave, Mod.currentMaxEnergy);
                Mod.stamina = Mod.currentMaxStamina;
                Mod.totalKillCount = (int)loadedData["totalKillCount"];
                Mod.totalDeathCount = (int)loadedData["totalDeathCount"];
                Mod.totalRaidCount = (int)loadedData["totalRaidCount"];
                Mod.runthroughRaidCount = (int)loadedData["runthroughRaidCount"];
                Mod.survivedRaidCount = (int)loadedData["survivedRaidCount"];
                Mod.MIARaidCount = (int)loadedData["MIARaidCount"];
                Mod.KIARaidCount = (int)loadedData["KIARaidCount"];
                Mod.failedRaidCount = (int)loadedData["failedRaidCount"];
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].progress = (float)loadedData["skills"][i];
                    Mod.skills[i].currentProgress = Mod.skills[i].progress;
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }
                Mod.LogInfo("\t\t0");

                // Player items
                if (Mod.playerInventory == null)
                {
                    Mod.playerInventory = new Dictionary<string, int>();
                    Mod.playerInventoryItems = new Dictionary<string, List<MeatovItem>>();
                    Mod.playerFIRInventory = new Dictionary<string, int>();
                    Mod.playerFIRInventoryItems = new Dictionary<string, List<MeatovItem>>();
                }
                Mod.LogInfo("\t\t0");

                // Note that status UI must have been instantiated and awoken before loading player Items
                // This happens before call to ProcessData, on awake of hideoutcontroller
                LoadPlayerItems();
            }
            else if (Mod.justFinishedRaid)
            {
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }
            }
            Mod.LogInfo("\t0");

            // Load hideout items
            if (inventoryItems == null)
            {
                inventory = new Dictionary<string, int>();
                inventoryItems = new Dictionary<string, List<MeatovItem>>();
                FIRInventory = new Dictionary<string, int>();
                FIRInventoryItems = new Dictionary<string, List<MeatovItem>>();
                marketManager.inventory = new Dictionary<string, int>();
                marketManager.inventoryItems = new Dictionary<string, List<MeatovItem>>();
                marketManager.FIRInventory = new Dictionary<string, int>();
                marketManager.FIRInventoryItems = new Dictionary<string, List<MeatovItem>>();
            }
            else
            {
                inventory.Clear();
                inventoryItems.Clear();
                FIRInventory.Clear();
                FIRInventoryItems.Clear();
                marketManager.inventory.Clear();
                marketManager.inventoryItems.Clear();
                marketManager.FIRInventory.Clear();
                marketManager.FIRInventoryItems.Clear();
            }

            LoadHideoutItems();
            Mod.LogInfo("\t0");

            // Load trader data
            JArray traderDataArray = loadedData["hideout"]["traders"] as JArray;
            for(int i=0; i < Mod.traders.Length; ++i)
            {
                Mod.traders[i].LoadData(traderDataArray[i]);
            }
            // Load task data only after loading all trader data because some task conditions are dependent on trader live data
            for (int i = 0; i < Mod.traders.Length; ++i)
            {
                for (int j = 0; j < Mod.traders[i].tasks.Count; ++j)
                {
                    if(traderDataArray[i]["tasks"][Mod.traders[i].tasks[j].ID] == null)
                    {
                        Mod.traders[i].tasks[j].LoadData(null);
                    }
                    else
                    {
                        Mod.traders[i].tasks[j].LoadData(traderDataArray[i]["tasks"][Mod.traders[i].tasks[j].ID]);
                    }
                }
            }
            Mod.LogInfo("\t0");

            //TraderStatus.fenceRestockTimer = (float)loadedData["fenceRestockTimer"] - secondsSinceSave;

            scavTimer = (float)((long)loadedData["hideout"]["scavTimer"] - secondsSinceSave);

            // Check for insuredSets
            //if (Mod.insuredItems == null)
            //{
            //    Mod.insuredItems = new List<InsuredSet>();
            //}
            //if (loadedData["insuredSets"] != null)
            //{
            //    JArray loadedInsuredSets = (JArray)loadedData["insuredSets"];

            //    for (int i = 0; i < loadedInsuredSets.Count; ++i)
            //    {
            //        InsuredSet newInsuredSet = new InsuredSet();
            //        newInsuredSet.returnTime = (long)loadedInsuredSets[i]["returnTime"];
            //        newInsuredSet.items = loadedInsuredSets[i]["items"].ToObject<Dictionary<string, int>>();
            //        Mod.insuredItems.Add(newInsuredSet);
            //    }
            //}

            Mod.LogInfo("\t0");
            Mod.preventLoadMagUpdateLists = false;

            // Load triggered exploration triggers if not loading in from raid
            if (!Mod.justFinishedRaid)
            {
                if (Mod.triggeredExplorationTriggers == null)
                {
                    Mod.triggeredExplorationTriggers = new List<List<bool>>();
                }
                else
                {
                    Mod.triggeredExplorationTriggers.Clear();
                }
                if (loadedData["triggeredExplorationTriggers"] != null)
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        Mod.triggeredExplorationTriggers.Add(loadedData["triggeredExplorationTriggers"][i].ToObject<List<bool>>());
                    }
                }
                else
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        Mod.triggeredExplorationTriggers.Add(new List<bool>());
                    }
                }
            }
            Mod.LogInfo("\t0");

            // Load wishlist
            JArray wishlistArray = loadedData["wishlist"] as JArray;
            Mod.wishList = new List<MeatovItemData>();
            for (int i=0; i < wishlistArray.Count; ++i)
            {
                if (Mod.defaultItemData.TryGetValue(wishlistArray[i].ToString(), out MeatovItemData itemData))
                {
                    Mod.wishList.Add(itemData);
                }
            }
            Mod.LogInfo("\t0");

            // Get what each meatov item is needed for now that we have all the necessary data
            foreach (KeyValuePair<string, MeatovItemData> itemData in Mod.defaultItemData)
            {
                itemData.Value.InitCheckmarkData();
            }
            Mod.LogInfo("\t0");

            // Load areas
            // Areas get loaded on Area.Start() which will always happen after process data, which happens in Awake()
        }

        public void LoadHideoutItems()
        {
            if (loadedData["hideout"]["looseItems"] != null)
            {
                JArray looseItems = loadedData["hideout"]["looseItems"] as JArray;
                for (int i = 0; i < looseItems.Count; ++i)
                {
                    JToken looseItemData = looseItems[i];
                    JToken vanillaCustomData = looseItemData["vanillaCustomData"];
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int j = 1; j < objs.Count; ++j)
                            {
                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][j - 1]["tarkovID"].ToString()]);
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][j - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][j - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][j - 1]["foundInRaid"];
                                }
                            }

                            meatovItem.transform.position = new Vector3((float)looseItemData["posX"], (float)looseItemData["posY"], (float)looseItemData["posZ"]);
                            meatovItem.transform.rotation = Quaternion.Euler((float)looseItemData["rotX"], (float)looseItemData["rotY"], (float)looseItemData["rotZ"]);
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(looseItemData, del);

                    if (loadedItem != null)
                    {
                        loadedItem.transform.position = new Vector3((float)looseItemData["posX"], (float)looseItemData["posY"], (float)looseItemData["posZ"]);
                        loadedItem.transform.rotation = Quaternion.Euler((float)looseItemData["rotX"], (float)looseItemData["rotY"], (float)looseItemData["rotZ"]);
                    }
                }
            }
            if (loadedData["hideout"]["tradeVolumeItems"] != null)
            {
                JArray tradeItems = loadedData["hideout"]["tradeVolumeItems"] as JArray;
                for (int i = 0; i < tradeItems.Count; ++i)
                {
                    JToken tradeItemData = tradeItems[i];
                    JToken vanillaCustomData = tradeItemData["vanillaCustomData"];
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int j = 1; j < objs.Count; ++j)
                            {
                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][j - 1]["tarkovID"].ToString()]);
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][j - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][j - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][j - 1]["foundInRaid"];
                                }
                            }

                            marketManager.tradeVolume.AddItem(meatovItem);
                            meatovItem.transform.localPosition = new Vector3((float)tradeItemData["posX"], (float)tradeItemData["posY"], (float)tradeItemData["posZ"]);
                            meatovItem.transform.localRotation = Quaternion.Euler((float)tradeItemData["rotX"], (float)tradeItemData["rotY"], (float)tradeItemData["rotZ"]);
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(tradeItemData, del);

                    if (loadedItem != null)
                    {
                        marketManager.tradeVolume.AddItem(loadedItem);
                        loadedItem.transform.localPosition = new Vector3((float)tradeItemData["posX"], (float)tradeItemData["posY"], (float)tradeItemData["posZ"]);
                        loadedItem.transform.localRotation = Quaternion.Euler((float)tradeItemData["rotX"], (float)tradeItemData["rotY"], (float)tradeItemData["rotZ"]);
                    }
                }
            }
            if (loadedData["hideout"]["areas"] != null)
            {
                JArray areaData = loadedData["hideout"]["areas"] as JArray;
                for (int i = 0; i < areaData.Count; ++i)
                {
                    JToken area = areaData[i];
                    if (area["levels"] != null)
                    {
                        JArray areaLevels = area["levels"] as JArray;
                        for (int j = 0; j < areaLevels.Count; ++j)
                        {
                            JToken level = area["levels"][j];
                            if (level["volumes"] != null)
                            {
                                JArray volumes = level["volumes"] as JArray;
                                for (int k = 0; k < volumes.Count; ++k)
                                {
                                    JArray volumeItems = volumes[k] as JArray;
                                    for (int l = 0; l < volumeItems.Count; ++l)
                                    {
                                        JToken volumeItemData = volumeItems[i];
                                        JToken vanillaCustomData = volumeItemData["vanillaCustomData"];
                                        VaultSystem.ReturnObjectListDelegate del = objs =>
                                        {
                                            // Here, assume objs[0] is the root item
                                            MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                                            if (meatovItem != null)
                                            {
                                                // Set live data
                                                meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                                                meatovItem.insured = (bool)vanillaCustomData["insured"];
                                                meatovItem.looted = (bool)vanillaCustomData["looted"];
                                                meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                                                for (int m = 1; m < objs.Count; ++m)
                                                {
                                                    MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                                    if (childMeatovItem != null)
                                                    {
                                                        childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                                        childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                                        childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                                        childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                                    }
                                                }

                                                areaController.areas[i].levels[j].areaVolumes[k].AddItem(meatovItem);
                                                meatovItem.transform.localPosition = new Vector3((float)volumeItemData["posX"], (float)volumeItemData["posY"], (float)volumeItemData["posZ"]);
                                                meatovItem.transform.localRotation = Quaternion.Euler((float)volumeItemData["rotX"], (float)volumeItemData["rotY"], (float)volumeItemData["rotZ"]);
                                            }
                                        };

                                        MeatovItem loadedItem = MeatovItem.Deserialize(volumeItemData, del);

                                        if (loadedItem != null)
                                        {
                                            areaController.areas[i].levels[j].areaVolumes[k].AddItem(loadedItem);
                                            loadedItem.transform.localPosition = new Vector3((float)volumeItemData["posX"], (float)volumeItemData["posY"], (float)volumeItemData["posZ"]);
                                            loadedItem.transform.localRotation = Quaternion.Euler((float)volumeItemData["rotX"], (float)volumeItemData["rotY"], (float)volumeItemData["rotZ"]);
                                        }
                                    }
                                }
                            }
                            if (level["slots"] != null)
                            {
                                JArray slots = level["slots"] as JArray;
                                for (int k = 0; k < slots.Count; ++k)
                                {
                                    JArray slotItems = slots[k] as JArray;
                                    for (int l = 0; l < slotItems.Count; ++l)
                                    {
                                        JToken slotItemData = slotItems[i];
                                        JToken vanillaCustomData = slotItemData["vanillaCustomData"];
                                        VaultSystem.ReturnObjectListDelegate del = objs =>
                                        {
                                            // Here, assume objs[0] is the root item
                                            MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                                            if (meatovItem != null)
                                            {
                                                // Set live data
                                                meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                                                meatovItem.insured = (bool)vanillaCustomData["insured"];
                                                meatovItem.looted = (bool)vanillaCustomData["looted"];
                                                meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                                                for (int m = 1; m < objs.Count; ++m)
                                                {
                                                    MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                                    if (childMeatovItem != null)
                                                    {
                                                        childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                                        childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                                        childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                                        childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                                    }
                                                }

                                                objs[0].SetQuickBeltSlot(areaController.areas[i].levels[j].areaSlots[k]);
                                            }
                                        };

                                        MeatovItem loadedItem = MeatovItem.Deserialize(slotItemData, del);

                                        if (loadedItem != null)
                                        {
                                            loadedItem.physObj.SetQuickBeltSlot(areaController.areas[i].levels[j].areaSlots[k]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            if (loadedData["hideout"]["scavReturnItems"] != null)
            {
                JArray scavItems = loadedData["hideout"]["scavReturnItems"] as JArray;
                for (int i = 0; i < scavItems.Count; ++i)
                {
                    JToken scavItemData = scavItems[i];
                    JToken vanillaCustomData = scavItemData["vanillaCustomData"];
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            meatovItem.transform.parent = scavReturnNode;
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(scavItemData, del);

                    if (loadedItem != null)
                    {
                        loadedItem.transform.parent = scavReturnNode;
                    }
                }
            }
        }

        public void LoadPlayerItems()
        {
            if(loadedData["leftHand"] != null)
            {
                // In case item is vanilla, in which case we use the vault system to save it,
                // we will only be getting the instantiated item later
                // We must write a delegate in order to put it in the correct hand once we do
                JToken vanillaCustomData = loadedData["leftHand"]["vanillaCustomData"];
                VaultSystem.ReturnObjectListDelegate del = objs =>
                {
                    // Here, assume objs[0] is the root item
                    MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                    if (meatovItem != null)
                    {
                        // Set live data
                        meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                        meatovItem.insured = (bool)vanillaCustomData["insured"];
                        meatovItem.looted = (bool)vanillaCustomData["looted"];
                        meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                        for (int m = 1; m < objs.Count; ++m)
                        {
                            MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                            if (childMeatovItem != null)
                            {
                                childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                            }
                        }

                        Mod.leftHand.fvrHand.ForceSetInteractable(objs[0]);
                        meatovItem.UpdateInventories();
                    }
                };

                // In case item is custom, it will be returned right away and we can handle it here
                MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["leftHand"], del);

                if(loadedItem != null)
                {
                    Mod.leftHand.fvrHand.ForceSetInteractable(loadedItem.physObj);
                    loadedItem.UpdateInventories();
                }
            }
            if(loadedData["rightHand"] != null)
            {
                JToken vanillaCustomData = loadedData["rightHand"]["vanillaCustomData"];
                VaultSystem.ReturnObjectListDelegate del = objs =>
                {
                    // Here, assume objs[0] is the root item
                    MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                    if (meatovItem != null)
                    {
                        // Set live data
                        meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                        meatovItem.insured = (bool)vanillaCustomData["insured"];
                        meatovItem.looted = (bool)vanillaCustomData["looted"];
                        meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                        for (int m = 1; m < objs.Count; ++m)
                        {
                            MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                            if (childMeatovItem != null)
                            {
                                childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                            }
                        }

                        Mod.rightHand.fvrHand.ForceSetInteractable(objs[0]);
                        meatovItem.UpdateInventories();
                    }
                };

                MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["rightHand"], del);

                if(loadedItem != null)
                {
                    Mod.rightHand.fvrHand.ForceSetInteractable(loadedItem.physObj);
                    loadedItem.UpdateInventories();
                }
            }
            // Note that we must load equipment before loading QBS items
            // This is so that if we have a rig it will have been loaded
            for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
            {
                if (loadedData["equipment"+i] != null)
                {
                    JToken vanillaCustomData = loadedData["equipment" + i]["vanillaCustomData"];
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            objs[0].SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i]);
                            meatovItem.UpdateInventories();
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["equipment" + i], del);

                    if (loadedItem != null)
                    {
                        loadedItem.physObj.SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i]);
                        loadedItem.UpdateInventories();
                    }
                }
            }
            // Get player body in case it wasn't set yet
            FVRPlayerBody playerBodyToUse = null;
            if (GM.CurrentPlayerBody == null)
            {
                Mod.LogInfo("DEV: PRELOADING OF PLAYERBODY WHEN LOADING PLAYER ITEMS WAS NEEDED!");
                playerBodyToUse = FindObjectOfType<FVRPlayerBody>();
            }
            else
            {
                Mod.LogWarning("DEV: Preloading of playerbody when loading player items was not needed!");
                playerBodyToUse = GM.CurrentPlayerBody;
            }
            for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++i)
            {
                if (loadedData["slot"+i] != null)
                {
                    JToken vanillaCustomData = loadedData["slot" + i]["vanillaCustomData"];
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            objs[0].SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i]);
                            meatovItem.UpdateInventories();
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["slot" + i], del);

                    if (loadedItem != null)
                    {
                        loadedItem.physObj.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i]);
                        loadedItem.UpdateInventories();
                    }
                }
            }
        }

        private void SetupTutorial()
        {
            Transform tutorialTransform = transform.GetChild(0).GetChild(1);
            tutorialTransform.gameObject.SetActive(true);
            AudioSource hoverAudio = tutorialTransform.GetChild(21).GetComponent<AudioSource>();
            FVRPointable backgroundPointable = tutorialTransform.gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;
            Transform controlsParent = null;
            switch (Mod.leftHand.fvrHand.CMode)
            {
                case ControlMode.Index:
                    controlsParent = tutorialTransform.GetChild(1);
                    break;
                case ControlMode.Oculus:
                    controlsParent = tutorialTransform.GetChild(2);
                    break;
                case ControlMode.Vive:
                    controlsParent = tutorialTransform.GetChild(3);
                    break;
                case ControlMode.WMR:
                    controlsParent = tutorialTransform.GetChild(4);
                    break;
            }

            // Setup welcome
            // Skip
            PointableButton pointableButton = tutorialTransform.GetChild(0).GetChild(1).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
            // Next
            pointableButton = tutorialTransform.GetChild(0).GetChild(2).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(tutorialTransform.GetChild(0), controlsParent); });

            // Setup controls buttons
            for (int i = 0; i < controlsParent.childCount; ++i)
            {
                // Skip
                pointableButton = controlsParent.GetChild(i).GetChild(1).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
                // Next
                pointableButton = controlsParent.GetChild(i).GetChild(2).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                Transform toHide = null;
                Transform toShow = null;
                if (i == controlsParent.childCount - 1)
                {
                    toHide = controlsParent;
                    toShow = tutorialTransform.GetChild(5);

                }
                else
                {
                    toHide = controlsParent.GetChild(i);
                    toShow = controlsParent.GetChild(i + 1);
                }
                pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(toHide, toShow); });
            }

            // Setup remaining tutorial screens
            for (int i = 5; i < 18; ++i)
            {
                // Skip
                pointableButton = tutorialTransform.GetChild(i).GetChild(1).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
                // Next
                pointableButton = tutorialTransform.GetChild(i).GetChild(2).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                Transform toHide = tutorialTransform.GetChild(i);
                Transform toShow = tutorialTransform.GetChild(i + 1);
                pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(toHide, toShow); });
            }

            // Setup end
            // Skip
            pointableButton = tutorialTransform.GetChild(18).GetChild(1).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
            // Next
            pointableButton = tutorialTransform.GetChild(18).GetChild(2).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(tutorialTransform, null); });
            // Donate
            pointableButton = tutorialTransform.GetChild(18).GetChild(3).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnDonateClick(tutorialTransform.GetChild(18).GetChild(5).gameObject); });
        }

        public void AddToInventory(MeatovItem item, bool stackOnly = false, int stackDifference = 0)
        {
            int difference = stackDifference;
            // StackOnly should be true if not item location was changed, but the stack count has
            if (stackOnly)
            {
                if (inventory.ContainsKey(item.tarkovID))
                {
                    inventory[item.tarkovID] += stackDifference;

                    if (inventory[item.tarkovID] <= 0)
                    {
                        Mod.LogError("DEV: Hideout AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        inventory.Remove(item.tarkovID);
                        inventoryItems.Remove(item.tarkovID);
                    }
                }
                else
                {
                    Mod.LogError("DEV: Hideout AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n" + Environment.StackTrace);
                }

                if (item.foundInRaid)
                {
                    if (FIRInventory.ContainsKey(item.tarkovID))
                    {
                        FIRInventory[item.tarkovID] += stackDifference;

                        if (FIRInventory[item.tarkovID] <= 0)
                        {
                            Mod.LogError("DEV: Hideout AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                            FIRInventory.Remove(item.tarkovID);
                            FIRInventoryItems.Remove(item.tarkovID);
                        }
                    }
                    else
                    {
                        Mod.LogError("DEV: Hideout AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n" + Environment.StackTrace);
                    }
                }
            }
            else
            {
                difference = item.stack;
                if (inventory.ContainsKey(item.tarkovID))
                {
                    inventory[item.tarkovID] += item.stack;
                    inventoryItems[item.tarkovID].Add(item);
                }
                else
                {
                    inventory.Add(item.tarkovID, item.stack);
                    inventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
                }

                if (item.foundInRaid)
                {
                    if (FIRInventory.ContainsKey(item.tarkovID))
                    {
                        FIRInventory[item.tarkovID] += item.stack;
                        FIRInventoryItems[item.tarkovID].Add(item);
                    }
                    else
                    {
                        FIRInventory.Add(item.tarkovID, item.stack);
                        FIRInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
                    }
                }

                if (item.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    FVRFireArmMagazine boxMagazine = item.physObj as FVRFireArmMagazine;
                    foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                    {
                        if (loadedRound == null)
                        {
                            break;
                        }

                        if (ammoBoxesByRoundClassByRoundType.TryGetValue(boxMagazine.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                        {
                            if (midDict.TryGetValue(loadedRound.LR_Class, out Dictionary<MeatovItem, int> boxDict))
                            {
                                int count = 0;
                                if (boxDict.TryGetValue(item, out count))
                                {
                                    ++boxDict[item];
                                }
                                else
                                {
                                    boxDict.Add(item, 1);
                                }
                            }
                            else
                            {
                                Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                                newBoxDict.Add(item, 1);
                                midDict.Add(loadedRound.LR_Class, newBoxDict);
                            }
                        }
                        else
                        {
                            Dictionary<MeatovItem, int> newBoxDict = new Dictionary<MeatovItem, int>();
                            newBoxDict.Add(item, 1);
                            Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> newMidDict = new Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>();
                            newMidDict.Add(loadedRound.LR_Class, newBoxDict);
                            ammoBoxesByRoundClassByRoundType.Add(boxMagazine.RoundType, newMidDict);
                        }
                    }
                }
            }

            OnHideoutInventoryChangedInvoke();
            OnHideoutItemInventoryChangedInvoke(item.itemData, difference);
        }

        public void AddToFIRInventory(MeatovItem item)
        {
            if (!item.foundInRaid)
            {
                return;
            }

            if (FIRInventory.ContainsKey(item.tarkovID))
            {
                FIRInventory[item.tarkovID] += item.stack;
                FIRInventoryItems[item.tarkovID].Add(item);
            }
            else
            {
                FIRInventory.Add(item.tarkovID, item.stack);
                FIRInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
            }
        }

        public void RemoveFromInventory(MeatovItem item)
        {
            Mod.LogInfo("\tRemoving item "+item.tarkovID + ":" + item.H3ID + " with IID: " + item.GetInstanceID() + " from hideout inventory");
            int difference = -item.stack;

            if (inventory.ContainsKey(item.tarkovID))
            {
                inventory[item.tarkovID] -= item.stack;
                inventoryItems[item.tarkovID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove "+item.tarkovID + ":" + item.H3ID + " from hideout inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (inventory[item.tarkovID] == 0)
            {
                inventory.Remove(item.tarkovID);
                inventoryItems.Remove(item.tarkovID);
            }

            if (item.foundInRaid)
            {
                if (FIRInventory.ContainsKey(item.tarkovID))
                {
                    FIRInventory[item.tarkovID] -= item.stack;
                    FIRInventoryItems[item.tarkovID].Remove(item);
                }
                else
                {
                    Mod.LogError("Attempting to remove "+item.tarkovID + ":" + item.H3ID + " from hideout inventory but key was not found in it:\n" + Environment.StackTrace);
                    return;
                }
                if (FIRInventory[item.tarkovID] == 0)
                {
                    FIRInventory.Remove(item.tarkovID);
                    FIRInventoryItems.Remove(item.tarkovID);
                }
            }

            if (item.itemType == MeatovItem.ItemType.AmmoBox)
            {
                FVRFireArmMagazine boxMagazine = item.physObj as FVRFireArmMagazine;
                foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                {
                    if (loadedRound == null)
                    {
                        break;
                    }

                    --ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class][item];
                    if (ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class][item] == 0)
                    {
                        ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class].Remove(item);
                        if (ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class].Count == 0)
                        {
                            ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType].Remove(loadedRound.LR_Class);
                            if (ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType].Count == 0)
                            {
                                ammoBoxesByRoundClassByRoundType.Remove(boxMagazine.RoundType);
                            }
                        }
                    }
                }
            }

            OnHideoutInventoryChangedInvoke();
            OnHideoutItemInventoryChangedInvoke(item.itemData, difference);
        }

        public void RemoveFromFIRInventory(MeatovItem item)
        {
            if (item.foundInRaid)
            {
                return;
            }

            if (FIRInventory.ContainsKey(item.tarkovID))
            {
                FIRInventory[item.tarkovID] -= item.stack;
                FIRInventoryItems[item.tarkovID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove "+item.tarkovID + ":" + item.H3ID + " from hideout inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (FIRInventory[item.tarkovID] == 0)
            {
                FIRInventory.Remove(item.tarkovID);
                FIRInventoryItems.Remove(item.tarkovID);
            }
        }

        private void AddAttachments(FVRPhysicalObject physicalObject, JToken loadedPhysicalObject)
        {
            if (loadedPhysicalObject["AttachmentsList"] == null)
            {
                return;
            }

            Transform root = physicalObject.transform;
            JArray loadedAttachmentsList = ((JArray)loadedPhysicalObject["AttachmentsList"]);
            for (int i = 0; i < loadedAttachmentsList.Count; ++i)
            {
                JToken currentPhysicalObject = loadedAttachmentsList[i];
                int parsedID = -1;
                GameObject prefabToUse = null;
                if (int.TryParse(currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString(), out parsedID))
                {
                    // Custom item, fetch from our own assets
                    prefabToUse = Mod.GetItemPrefab(parsedID);
                }
                else
                {
                    // Vanilla item, fetch from game assets
                    prefabToUse = IM.OD[currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString()].GetGameObject();
                }

                GameObject itemObject = Instantiate<GameObject>(prefabToUse, root);

                FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
                FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;

                // Fill data
                // GameObject
                itemObject.transform.localPosition = new Vector3((float)currentPhysicalObject["positionX"], (float)currentPhysicalObject["positionY"], (float)currentPhysicalObject["positionZ"]);
                itemObject.transform.localRotation = Quaternion.Euler(new Vector3((float)currentPhysicalObject["rotationX"], (float)currentPhysicalObject["rotationY"], (float)currentPhysicalObject["rotationZ"]));
                itemObject.transform.parent = null;

                // PhysicalObject
                itemPhysicalObject.m_isSpawnLock = (bool)currentPhysicalObject["m_isSpawnLock"];
                itemPhysicalObject.m_isHardnessed = (bool)currentPhysicalObject["m_isHarnessed"];
                itemPhysicalObject.IsKinematicLocked = (bool)currentPhysicalObject["IsKinematicLocked"];
                itemPhysicalObject.IsInWater = (bool)currentPhysicalObject["IsInWater"];
                FVRFireArmAttachment itemAttachment = itemPhysicalObject as FVRFireArmAttachment;
                itemAttachment.AttachToMount(physicalObject.AttachmentMounts[(int)currentPhysicalObject["mountIndex"]], false);
                if (itemAttachment is Suppressor)
                {
                    (itemAttachment as Suppressor).AutoMountWell();
                }
                AddAttachments(itemPhysicalObject, currentPhysicalObject);

                // ObjectWrapper
                itemObjectWrapper.ItemID = currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString();
            }
        }

        public override void InitUI()
        {
            UpdateLoadButtonList();

            if (Mod.justFinishedRaid)
            {
                if (Mod.chosenCharIndex == 0)
                {
                    // Spawn with less health and energy/hydration if killed or MIA
                    int raidResultExp = 0; // TODO: Add bonus for how long we survived
                    if (Mod.raidState == FinishRaidState.KIA || Mod.raidState == FinishRaidState.MIA)
                    {
                        for (int i = 0; i < Mod.GetHealthCount(); ++i)
                        {
                            Mod.SetHealth(i, 0.3f * Mod.GetCurrentMaxHealth(i));
                        }

                        Mod.hydration = 0.3f * Mod.currentMaxHydration;
                        Mod.energy = 0.3f * Mod.currentMaxEnergy;

                        // Remove all effects
                        Effect.RemoveAllEffects();
                    }
                    else if (Mod.raidState == FinishRaidState.Survived)
                    {
                        raidResultExp = 600 + (int)(Mod.raidTime / 60 * 10); // Survive exp + bonus of 10 exp / min
                        Mod.AddExperience(600);
                    }

                    Mod.LogInfo("Base init: Just finished raid");
                    Transform raidReportScreen = transform.GetChild(0).GetChild(0).GetChild(12);
                    medicalScreen = transform.GetChild(0).GetChild(0).GetChild(13);
                    float raidReportListHeight = 141; // Default height not including None kill
                    float medicalListHeight = 26;

                    // Activate raid report, deactivate hideout menu
                    transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true);

                    // Set raid result in title
                    string raidResultString = Mod.raidState == FinishRaidState.RunThrough ? "Run Through" : Mod.raidState.ToString();
                    raidReportScreen.GetChild(0).GetComponent<Text>().text = "Raid Report: " + raidResultString;

                    // Set experience elements
                    raidReportScreen.GetChild(2).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
                    raidReportScreen.GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
                    int expForNextLevel = 0;
                    for (int i = 0; i < Mod.level; ++i)
                    {
                        expForNextLevel += Mod.XPPerLevel[Mod.level];
                    }
                    raidReportScreen.GetChild(2).GetChild(1).GetChild(1).GetComponent<Image>().rectTransform.sizeDelta = new Vector2(450 * (Mod.experience / (float)expForNextLevel), 12.8f);
                    raidReportScreen.GetChild(2).GetChild(1).GetChild(2).GetComponent<Text>().text = Mod.experience.ToString() + "/" + expForNextLevel;

                    Transform listContent = raidReportScreen.GetChild(3).GetChild(0).GetChild(0);
                    int expTotal = 0;

                    Mod.LogInfo("\tSet raid report xp");
                    // Fill kill list
                    int killCount = 0;
                    if (Mod.killList != null && Mod.killList.Count > 0)
                    {
                        Mod.LogInfo("\tHave kills, adding to list");
                        // Disable none
                        listContent.GetChild(0).GetChild(1).gameObject.SetActive(false);

                        // Add each kill
                        foreach (KeyValuePair<string, int> kill in Mod.killList)
                        {
                            GameObject killElement = Instantiate(listContent.GetChild(0).GetChild(2).gameObject, listContent.GetChild(0));
                            killElement.SetActive(true);
                            killElement.transform.GetChild(0).GetComponent<Text>().text = kill.Key;
                            killElement.transform.GetChild(1).GetComponent<Text>().text = kill.Value.ToString() + " exp";

                            expTotal += kill.Value;

                            raidReportListHeight += 21;
                            ++killCount;
                        }
                    }
                    else
                    {
                        Mod.LogInfo("\tNo kills");
                        raidReportListHeight += 21; // Add none kill
                    }

                    // Set other
                    listContent.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = Mod.lootingExp.ToString() + " exp";
                    expTotal += Mod.lootingExp;
                    listContent.GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().text = Mod.healingExp.ToString() + " exp";
                    expTotal += Mod.healingExp;
                    listContent.GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().text = Mod.explorationExp.ToString() + " exp";
                    expTotal += Mod.explorationExp;
                    listContent.GetChild(1).GetChild(4).GetChild(0).GetComponent<Text>().text = "Raid Result (" + raidResultString + ")";
                    listContent.GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().text = raidResultExp.ToString() + " exp";
                    expTotal += raidResultExp;

                    Mod.LogInfo("\tSet other xp");
                    // Set total
                    listContent.GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = expTotal.ToString() + " exp";

                    Mod.LogInfo("\tSetting hoverscrolls, killcount: " + killCount);
                    // Set hover scrolls
                    if (killCount >= 8)
                    {
                        Mod.LogInfo("\t\tkillcount >= 8, activating kill list hover scrolls");
                        HoverScroll raidReportListDownHoverScroll = raidReportScreen.GetChild(3).GetChild(2).gameObject.AddComponent<HoverScroll>();
                        HoverScroll raidReportListUpHoverScroll = raidReportScreen.GetChild(3).GetChild(3).gameObject.AddComponent<HoverScroll>();
                        raidReportListDownHoverScroll.MaxPointingRange = 30;
                        //raidReportListDownHoverScroll.hoverSound = hoverAudio;
                        raidReportListDownHoverScroll.scrollbar = raidReportScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        raidReportListDownHoverScroll.other = raidReportListUpHoverScroll;
                        raidReportListDownHoverScroll.up = false;
                        raidReportListUpHoverScroll.MaxPointingRange = 30;
                        //raidReportListUpHoverScroll.hoverSound = hoverAudio;
                        raidReportListUpHoverScroll.scrollbar = raidReportScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        raidReportListUpHoverScroll.other = raidReportListDownHoverScroll;
                        raidReportListUpHoverScroll.up = true;

                        raidReportListUpHoverScroll.rate = 309 / (raidReportListHeight - 309);
                        raidReportListDownHoverScroll.rate = 309 / (raidReportListHeight - 309);
                        raidReportListDownHoverScroll.gameObject.SetActive(true);
                    }
                    Mod.LogInfo("\tSet hoverscrolls, setting medical");


                    //// Set medical body
                    //Dictionary<int, bool[]> partConditions = new Dictionary<int, bool[]>();
                    //foreach (Effect effect in Effect.effects)
                    //{
                    //    if (effect.partIndex > -1)
                    //    {
                    //        if (effect.effectType == Effect.EffectType.LightBleeding)
                    //        {
                    //            if (partConditions.ContainsKey(effect.partIndex))
                    //            {
                    //                partConditions[effect.partIndex][0] = true;
                    //            }
                    //            else
                    //            {
                    //                partConditions.Add(effect.partIndex, new bool[] { true, false, false });
                    //            }
                    //        }
                    //        else if (effect.effectType == Effect.EffectType.HeavyBleeding)
                    //        {
                    //            if (partConditions.ContainsKey(effect.partIndex))
                    //            {
                    //                partConditions[effect.partIndex][1] = true;
                    //            }
                    //            else
                    //            {
                    //                partConditions.Add(effect.partIndex, new bool[] { false, true, false });
                    //            }
                    //        }
                    //        else if (effect.effectType == Effect.EffectType.Fracture)
                    //        {
                    //            if (partConditions.ContainsKey(effect.partIndex))
                    //            {
                    //                partConditions[effect.partIndex][2] = true;
                    //            }
                    //            else
                    //            {
                    //                partConditions.Add(effect.partIndex, new bool[] { false, false, true });
                    //            }
                    //        }
                    //    }
                    //}
                    Mod.LogInfo("\tSet body");

                    // Process parts
                    medicalScreenPartImagesParent = medicalScreen.GetChild(4).GetChild(0);
                    Transform partInfoParent = medicalScreen.GetChild(4).GetChild(1);
                    Transform medicalListContent = medicalScreen.GetChild(3).GetChild(0).GetChild(0);
                    medicalScreenPartImages = new Transform[7];
                    medicalScreenPartHealthTexts = new Text[7];
                    totalMedicalTreatmentPrice = 0;
                    fullPartConditions = new Dictionary<int, int[]>();
                    medicalPartElements = new Dictionary<int, GameObject>();
                    int breakPartPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["BreakPart"]["RemovePrice"];
                    int fracturePrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["Fracture"]["RemovePrice"];
                    int heavyBleedingPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["RemovePrice"];
                    int lightBleedingPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["RemovePrice"];
                    int healthPrice = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["HealthPointPrice"];
                    int trialLevels = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["TrialLevels"];
                    int trialRaids = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["TrialRaids"];
                    int[] otherConditionCosts = new int[] { lightBleedingPrice, heavyBleedingPrice, fracturePrice };
                    Mod.LogInfo("\tInit part process");
                    for (int partIndex = 0; partIndex < 7; ++partIndex)
                    {
                        // Set part color
                        medicalScreenPartImages[partIndex] = medicalScreenPartImagesParent.GetChild(partIndex);
                        if (Mod.GetHealth(partIndex) == 0)
                        {
                            medicalScreenPartImages[partIndex].GetComponent<Image>().color = Color.black;
                        }
                        else
                        {
                            medicalScreenPartImages[partIndex].GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mod.GetHealth(partIndex) / Mod.GetCurrentMaxHealth(partIndex));
                        }

                        // Set part info
                        medicalScreenPartHealthTexts[partIndex] = partInfoParent.GetChild(partIndex).GetChild(1).GetComponent<Text>();
                        medicalScreenPartHealthTexts[partIndex].text = String.Format("{0:0}", Mod.GetHealth(partIndex)) + "/" + String.Format("{0:0}", Mod.GetCurrentMaxHealth(partIndex));
                        //if (partConditions.ContainsKey(partIndex))
                        //{
                        //    for (int i = 0; i < partConditions[partIndex].Length; ++i)
                        //    {
                        //        partInfoParent.GetChild(partIndex).GetChild(3).GetChild(i).gameObject.SetActive(partConditions[partIndex][i]);
                        //    }
                        //}

                        //if (partConditions.ContainsKey(partIndex) || Mod.GetHealth(partIndex) < Mod.GetCurrentMaxHealth(partIndex))
                        //{
                        //    fullPartConditions.Add(partIndex, new int[5]);

                        //    // Add to list
                        //    GameObject partElement = Instantiate(medicalListContent.GetChild(0).gameObject, medicalListContent);
                        //    partElement.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.GetBodyPartName(partIndex);
                        //    partElement.SetActive(true);

                        //    // Setup logic
                        //    PointableButton pointableButton = partElement.transform.GetChild(0).gameObject.AddComponent<PointableButton>();
                        //    pointableButton.SetButton();
                        //    int currentPartIndex = partIndex;
                        //    pointableButton.Button.onClick.AddListener(() => { ToggleMedicalPart(currentPartIndex); });
                        //    pointableButton.MaxPointingRange = 20;
                        //    //pointableButton.hoverSound = hoverAudio;

                        //    medicalPartElements.Add(partIndex, partElement);

                        //    medicalListHeight += 27;

                        //    // Process health and destroyed
                        //    int partTotalCost = 0;
                        //    if (Mod.GetHealth(partIndex) < Mod.GetCurrentMaxHealth(partIndex))
                        //    {
                        //        if (Mod.GetHealth(partIndex) <= 0)
                        //        {
                        //            int cost = (int)(breakPartPrice + healthPrice * Mod.GetCurrentMaxHealth(partIndex));
                        //            fullPartConditions[partIndex][4] = cost;
                        //            totalMedicalTreatmentPrice += cost;
                        //            partTotalCost += cost;

                        //            partElement.transform.GetChild(5).gameObject.SetActive(true);
                        //            //partElement.transform.GetChild(5).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                        //            PointableButton destroyedPartButton = partElement.transform.GetChild(5).gameObject.AddComponent<PointableButton>();
                        //            destroyedPartButton.SetButton();
                        //            destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 4); });
                        //            destroyedPartButton.MaxPointingRange = 20;
                        //            //destroyedPartButton.hoverSound = hoverAudio;
                        //        }
                        //        else // Not destroyed but damaged
                        //        {
                        //            int hpToHeal = (int)(Mod.GetCurrentMaxHealth(partIndex) - Mod.GetHealth(partIndex));
                        //            int cost = healthPrice * hpToHeal;
                        //            fullPartConditions[partIndex][0] = cost;
                        //            totalMedicalTreatmentPrice += cost;
                        //            partTotalCost += cost;

                        //            partElement.transform.GetChild(1).gameObject.SetActive(true);
                        //            //partElement.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);
                        //            partElement.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "Health (" + hpToHeal + ")";

                        //            PointableButton destroyedPartButton = partElement.transform.GetChild(1).gameObject.AddComponent<PointableButton>();
                        //            destroyedPartButton.SetButton();
                        //            destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 0); });
                        //            destroyedPartButton.MaxPointingRange = 20;
                        //            //destroyedPartButton.hoverSound = hoverAudio;
                        //        }

                        //        medicalListHeight += 22;
                        //    }

                        //    // Process other conditions
                        //    if (partConditions.ContainsKey(partIndex))
                        //    {
                        //        for (int i = 0; i < 3; ++i)
                        //        {
                        //            if (partConditions[partIndex][i])
                        //            {
                        //                int cost = otherConditionCosts[i];
                        //                fullPartConditions[partIndex][i + 1] = cost;
                        //                totalMedicalTreatmentPrice += cost;
                        //                partTotalCost += cost;

                        //                partElement.transform.GetChild(i + 2).gameObject.SetActive(true);
                        //                //partElement.transform.GetChild(i + 2).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                        //                PointableButton destroyedPartButton = partElement.transform.GetChild(i + 2).gameObject.AddComponent<PointableButton>();
                        //                destroyedPartButton.SetButton();
                        //                destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, i + 1); });
                        //                destroyedPartButton.MaxPointingRange = 20;
                        //                //destroyedPartButton.hoverSound = hoverAudio;

                        //                medicalListHeight += 22;
                        //            }
                        //        }
                        //    }

                        //    // Set part total cost
                        //    //partElement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(partTotalCost);
                        //}
                    }
                    Mod.LogInfo("\tProcessed parts");

                    // Setup total and put as last sibling
                    totalTreatmentPriceText = medicalListContent.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                    medicalListContent.GetChild(1).SetAsLastSibling();

                    // Set total health
                    float totalHealth = 0;
                    for (int i = 0; i < Mod.GetHealthCount(); ++i) 
                    {
                        totalHealth += Mod.GetHealth(i);
                    }
                    medicalScreenTotalHealthText = medicalScreen.GetChild(4).GetChild(2).GetChild(1).GetComponent<Text>();
                    medicalScreenTotalHealthText.text = totalHealth.ToString() + "/440";

                    //medicalScreen.GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));

                    Mod.LogInfo("\tSet total health");
                    // Set hover scrolls
                    if (medicalListHeight >= 377)
                    {
                        HoverScroll medicalListDownHoverScroll = medicalScreen.GetChild(3).GetChild(2).gameObject.AddComponent<HoverScroll>();
                        HoverScroll merdicalListUpHoverScroll = medicalScreen.GetChild(3).GetChild(3).gameObject.AddComponent<HoverScroll>();
                        medicalListDownHoverScroll.MaxPointingRange = 30;
                        //medicalListDownHoverScroll.hoverSound = hoverAudio;
                        medicalListDownHoverScroll.scrollbar = medicalScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        medicalListDownHoverScroll.other = merdicalListUpHoverScroll;
                        medicalListDownHoverScroll.up = false;
                        merdicalListUpHoverScroll.MaxPointingRange = 30;
                        //merdicalListUpHoverScroll.hoverSound = hoverAudio;
                        merdicalListUpHoverScroll.scrollbar = medicalScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        merdicalListUpHoverScroll.other = medicalListDownHoverScroll;
                        merdicalListUpHoverScroll.up = true;

                        merdicalListUpHoverScroll.rate = 377 / (medicalListHeight - 377);
                        medicalListDownHoverScroll.rate = 377 / (medicalListHeight - 377);
                        medicalListDownHoverScroll.gameObject.SetActive(true);
                    }
                    Mod.LogInfo("\tSet hoverscrolls");

                    UpdateTreatmentApply();

                    // Enable after raid collider to keep player in security room until they're done with after raid report
                    transform.GetChild(1).GetChild(23).GetChild(3).gameObject.SetActive(true);
                }
                else // Finished scav raid
                {
                    scavTimer = (defaultScavTime - defaultScavTime / 100 * Bonus.scavCooldownTimer);
                }
            }

            // Init status skills
            //Transform skillList = StatusUI.instance.transform.GetChild(9).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            //GameObject skillPairPrefab = skillList.GetChild(0).gameObject;
            //GameObject skillPrefab = skillPairPrefab.transform.GetChild(0).gameObject;
            //Transform currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
            //currentSkillPair.gameObject.SetActive(true);
            //StatusUI.instance.skills = new SkillUI[Mod.skills.Length];
            //for (int i = 0; i < Mod.skills.Length; ++i)
            //{
            //    if (currentSkillPair.childCount == 3)
            //    {
            //        currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
            //        currentSkillPair.gameObject.SetActive(true);
            //    }

            //    SkillUI skillUI = new SkillUI();
            //    GameObject currentSkill = Instantiate(skillPrefab, currentSkillPair);
            //    currentSkill.SetActive(true);
            //    currentSkill.transform.GetChild(0).GetComponent<Image>().sprite = Mod.skillIcons[i];
            //    skillUI.text = currentSkill.transform.GetChild(1).GetChild(0).GetComponent<Text>();
            //    skillUI.text.text = String.Format("{0} lvl. {1:0} ({2:0}/100)", Mod.SkillIndexToName(i), (int)(Mod.skills[i].currentProgress / 100), Mod.skills[i].currentProgress % 100);
            //    skillUI.progressBarRectTransform = currentSkill.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<RectTransform>();
            //    skillUI.progressBarRectTransform.sizeDelta = new Vector2(Mod.skills[i].currentProgress % 100, 4.73f);
            //    skillUI.diminishingReturns = currentSkill.transform.GetChild(1).GetChild(2).gameObject;
            //    skillUI.increasing = currentSkill.transform.GetChild(1).GetChild(3).gameObject;
            //    StatusUI.instance.skills[i] = skillUI;
            //}
        }

        private void UpdateTreatmentApply()
        {
            //int playerRoubleCount = (inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0);
            //if (playerRoubleCount < totalMedicalTreatmentPrice)
            //{
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = false;
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.gray;
            //}
            //else
            //{
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = true;
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.white;
            //}
        }

        public void ToggleMedicalPart(int partIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price
                medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                for (int i = 0; i < 5; ++i)
                {
                    if (medicalPartElements[partIndex].transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                    {
                        totalMedicalTreatmentPrice -= fullPartConditions[partIndex][i];
                    }
                }
                //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
            }
            else
            {
                // Activate checkmark and add to price
                medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
                for (int i = 0; i < 5; ++i)
                {
                    if (medicalPartElements[partIndex].transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                    {
                        totalMedicalTreatmentPrice += fullPartConditions[partIndex][i];
                    }
                }
                //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
            }

            UpdateTreatmentApply();
        }

        public void ToggleMedicalPartCondition(int partIndex, int conditionIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(false);
                if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice -= fullPartConditions[partIndex][conditionIndex];
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }
            else
            {
                // Activate checkmark and add to price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice += fullPartConditions[partIndex][conditionIndex];
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }

            UpdateTreatmentApply();
        }

        public void UpdateLoadButtonList()
        {
            // Call a fetch because new saves could have been made
            FetchAvailableSaveFiles();

            // Set buttons activated depending on presence of save files
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    loadButtons[i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
                loadButton.gameObject.SetActive(true);
            }
            else
            {
                loadButton.gameObject.SetActive(false);
            }
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        private void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * UIController.meatovTimeMultiplier) % 86400);
            time = scaledTime;
        }

        public void OnTutorialSkipClick()
        {
            clickAudio.Play();
            transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        }

        public void OnTutorialNextClick(Transform current, Transform next)
        {
            clickAudio.Play();
            current.gameObject.SetActive(false);
            if (next != null)
            {
                next.gameObject.SetActive(true);
            }
        }

        public void OnDonateClick(GameObject text)
        {
            clickAudio.Play();
            Application.OpenURL("https://ko-fi.com/tommysoucy");
            text.SetActive(true);
            text.AddComponent<TimedDisabler>();
        }

        public void OnSaveClicked()
        {
            SetPage(2);
            clickAudio.Play();
        }

        public void OnSaveSlotClicked(int slotIndex)
        {
            clickAudio.Play();
            Mod.saveSlotIndex = slotIndex;
            saveConfirmTexts[slotIndex].SetActive(true);
            saveConfirmTexts[slotIndex].AddComponent<TimedDisabler>();
            Save();
        }

        public void OnLoadClicked()
        {
            clickAudio.Play();
            SetPage(1);
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            clickAudio.Play();
            ResetPlayerRig();
            LoadHideout(slotIndex);
        }

        public void OnRaidClicked()
        {
            clickAudio.Play();
            if (H3MP.Networking.Client.isFullyConnected)
            {
                pages[0].SetActive(false);
                instancesPage.SetActive(true);

                instancesListPage = 0;
                instancesNextButton.SetActive(Networking.raidInstances.Count > 6);
                instancesPreviousButton.SetActive(false);
                PopulateInstancesList();
            }
            else
            {
                SetPage(3);
            }
        }

        public void OnCreditsClicked()
        {
            clickAudio.Play();
            SetPage(10);
        }

        public void OnDonatePanelClicked()
        {
            clickAudio.Play();
            SetPage(11);
        }

        public void OnOptionsClicked()
        {
            clickAudio.Play();
            SetPage(12);
        }

        public void OnBackClicked()
        {
            clickAudio.Play();
            switch (pageIndex)
            {
                case 0:
                    SteamVR_LoadLevel.Begin("MeatovMainMenu", false, 0.5f, 0f, 0f, 0f, 1f);
                    break;
                case 4:
                case 5:
                case 6:
                    SetPage(pageIndex - 1);
                    break;
                default:
                    SetPage(0);
                    break;
            }
        }

        public void OnStartingBackClicked()
        {
            clickAudio.Play();

            loadingPage.SetActive(false);
            waitForDeployPage.SetActive(false);
            countdownPage.SetActive(false);

            cancelPage.SetActive(true);

            GameManager.SetInstance(0);

            cancelRaidLoad = true;
        }

        public void OnInstancesBackClicked()
        {
            clickAudio.Play();
            SetPage(0);
        }

        public void OnInstancesNewInstanceClicked()
        {
            clickAudio.Play();

            instancesPage.SetActive(false);
            newInstance0Page.SetActive(true);
            newInstanceNextButton.SetActive(true);
            newInstancePreviousButton.SetActive(false);

            charChoicePMC = true;
            newInstancePMCCheckText.text = "X";
            newInstanceScavCheckText.text = "";
            timeChoiceIs0 = true;
            timeChoice0CheckText.text = "X";
            timeChoice1CheckText.text = "";
            PMCSpawnTogether = true;
            newInstanceSpawnTogetherCheckText.text = "X";
            mapChoiceName = null;
        }

        public void OnInstancesNextClicked()
        {
            clickAudio.Play();
            ++instancesListPage;

            PopulateInstancesList();

            if (instancesListPage == Networking.raidInstances.Count / 6)
            {
                instancesNextButton.SetActive(false);
            }
            instancesPreviousButton.SetActive(true);
        }

        public void OnInstancesPreviousClicked()
        {
            clickAudio.Play();
            --instancesListPage;

            PopulateInstancesList();

            if(instancesListPage == 0)
            {
                instancesPreviousButton.SetActive(false);
            }
            instancesNextButton.SetActive(true);
        }

        public void PopulateInstancesList()
        {
            TODO: // Add check for whether we have the map installed and if we have the map's entry requirements
            while (instancesParent.childCount > 1)
            {
                Transform child = instancesParent.GetChild(1);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int maxPage = (Networking.raidInstances.Count - 1) / 6;
            if(instancesListPage > maxPage)
            {
                instancesListPage = maxPage;
            }

            int i = -1;
            foreach(KeyValuePair<int, RaidInstance> instanceDataEntry in Networking.raidInstances)
            {
                ++i;
                if (i < instancesListPage * 6)
                {
                    continue;
                }

                if (instancesParent.childCount == 7)
                {
                    break;
                }

                GameObject newInstanceListEntry = Instantiate(instanceEntryPrefab, instancesParent);
                newInstanceListEntry.SetActive(true);

                int index = instanceDataEntry.Key;
                newInstanceListEntry.GetComponentInChildren<Text>().text = "Instance " + instanceDataEntry.Value.ID;
                newInstanceListEntry.GetComponentInChildren<Button>().onClick.AddListener(() => { OnInstanceClicked(index); });
            }
        }

        public void OnInstanceClicked(int ID)
        {
            clickAudio.Play();
            instancesPage.SetActive(false);
            joinInstancePage.SetActive(true);

            Networking.potentialInstance = Networking.raidInstances[ID];
            joinInstanceTitle.text = "Join Instance "+ ID;
            if (Networking.potentialInstance.waiting)
            {
                charChoicePMC = true;
                joinInstancePMCCheckText.text = "X";
                joinInstanceScavCheckText.text = "";
            }
            else
            {
                charChoicePMC = false;
                joinInstancePMCCheckText.text = "";
                joinInstanceScavCheckText.text = "X";
            }
        }

        public void OnJoinInstanceBackClicked()
        {
            clickAudio.Play();
            instancesPage.SetActive(true);
            joinInstancePage.SetActive(false);
        }

        public void OnJoinInstanceCharPMCClicked()
        {
            if (Networking.potentialInstance.waiting)
            {
                clickAudio.Play();
                charChoicePMC = true;

                newInstancePMCCheckText.text = "X";
                newInstanceScavCheckText.text = "";
            }
            else
            {
                charChoicePMC = false;

                newInstancePMCCheckText.text = "";
                newInstanceScavCheckText.text = "X";
            }
        }

        public void OnJoinInstanceConfirmClicked()
        {
            clickAudio.Play();

            joinInstancePage.SetActive(false);
            waitingServerPage.SetActive(true);

            SetInstance(Networking.potentialInstance);
        }

        public void OnJoinInstanceCharScavClicked()
        {
            clickAudio.Play();
            charChoicePMC = false;

            newInstancePMCCheckText.text = "";
            newInstanceScavCheckText.text = "X";
        }

        public void OnNewInstanceCharPMCClicked()
        {
            clickAudio.Play();

            charChoicePMC = true;

            newInstancePMCCheckText.text = "X";
            newInstanceScavCheckText.text = "";
        }

        public void OnNewInstanceCharScavClicked()
        {
            clickAudio.Play();

            charChoicePMC = false;

            newInstancePMCCheckText.text = "";
            newInstanceScavCheckText.text = "X";
        }

        public void OnNewInstanceTime0Clicked()
        {
            clickAudio.Play();

            timeChoiceIs0 = true;

            timeChoice0CheckText.text = "X";
            timeChoice1CheckText.text = "";
        }

        public void OnNewInstanceTime1Clicked()
        {
            clickAudio.Play();

            timeChoiceIs0 = false;

            timeChoice0CheckText.text = "";
            timeChoice1CheckText.text = "X";
        }

        public void OnNewInstanceSpawnTogetherClicked()
        {
            clickAudio.Play();

            PMCSpawnTogether = !PMCSpawnTogether;

            newInstanceSpawnTogetherCheckText.text = PMCSpawnTogether ? "X" : "";
        }

        public void PopulateNewInstanceMapList()
        {
            while (newInstanceMapListParent.childCount > 1)
            {
                Transform child = newInstanceMapListParent.GetChild(1);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int i = -1;
            foreach (KeyValuePair<string, string> raidMap in Mod.availableRaidMaps)
            {
                ++i;
                if (i < newInstanceMapListPage * 6)
                {
                    continue;
                }

                if (newInstanceMapListParent.childCount == 7)
                {
                    break;
                }

                GameObject newInstanceMapListEntry = Instantiate(newInstanceMapListEntryPrefab, newInstanceMapListParent);
                newInstanceMapListEntry.SetActive(true);

                string mapName = raidMap.Key;
                newInstanceMapListEntry.GetComponentInChildren<Text>().text = mapName;
                newInstanceMapListEntry.GetComponentInChildren<Button>().onClick.AddListener(() => { OnNewInstanceMapClicked(mapName); });

                if (mapChoiceName.Equals("mapName"))
                {
                    newInstanceMapListEntry.GetComponentInChildren<Image>().color = Color.white;
                    newInstanceMapListEntry.GetComponentInChildren<Text>().color = Color.black;
                }
            }
        }

        public void OnNewInstanceMapClicked(string name)
        {
            mapChoiceName = name;

            PopulateNewInstanceMapList();
        }

        public void OnNewInstance0NextClicked()
        {
            clickAudio.Play();

            newInstance0Page.SetActive(false);
            newInstance1Page.SetActive(true);

            newInstanceMapListPage = 0;
            newInstanceNextButton.SetActive(Mod.availableRaidMaps.Count > 6);
            instancesPreviousButton.SetActive(false);
            PopulateNewInstanceMapList();
        }

        public void OnNewInstance1PreviousClicked()
        {
            clickAudio.Play();

            --newInstanceMapListPage;
            PopulateNewInstanceMapList();

            if(newInstanceMapListPage == 0)
            {
                newInstancePreviousButton.SetActive(false);
            }
            newInstanceNextButton.SetActive(true);
        }

        public void OnNewInstance1NextClicked()
        {
            clickAudio.Play();

            ++newInstanceMapListPage;
            PopulateNewInstanceMapList();

            if (newInstanceMapListPage == Mod.availableRaidMaps.Count / 6)
            {
                newInstanceNextButton.SetActive(false);
            }
            newInstancePreviousButton.SetActive(true);
        }

        public void OnNewInstanceBackClicked()
        {
            clickAudio.Play();

            if (newInstance1Page.activeSelf)
            {
                newInstance0Page.SetActive(true);
                newInstanceNextButton.SetActive(true);
                newInstance1Page.SetActive(false);
            }
            else
            {
                instancesPage.SetActive(true);
                newInstance0Page.SetActive(false);
            }
        }

        public void OnNewInstanceConfirmClicked()
        {
            clickAudio.Play();

            newInstance0Page.SetActive(false);
            newInstance1Page.SetActive(false);
            waitingServerPage.SetActive(true);

            Networking.setLatestInstance = true;
            RaidInstance raidInstance = Networking.AddInstance(mapChoiceName, timeChoiceIs0, PMCSpawnTogether);
        }

        public void SetInstance(RaidInstance instance)
        {
            if (waitingServerPage.activeSelf)
            {
                waitingServerPage.SetActive(false);
                waitingInstancePage.SetActive(true);

                if(Networking.raidInstances.TryGetValue(H3MP.GameManager.instance, out RaidInstance previousRaidInstance))
                {
                    previousRaidInstance.players.Remove(H3MP.GameManager.ID);

                    if(previousRaidInstance.players.Count == 0)
                    {
                        Networking.raidInstances.Remove(H3MP.GameManager.instance);

                        HideoutController.instance.PopulateInstancesList();
                    }
                }

                GameManager.SetInstance(instance.ID);

                if (!instance.players.Contains(H3MP.GameManager.ID))
                {
                    instance.players.Add(H3MP.GameManager.ID);
                }

                waitingInstanceCharText.text = "Character: " + (charChoicePMC ? "PMC" : "Scav");
                waitingInstanceMapText.text = "Map: " + mapChoiceName;
                waitingInstanceSpawnTogetherText.text = "PMCs spawn together: " + PMCSpawnTogether;
                waitingInstanceStartButton.SetActive(Networking.currentInstance.players[0] == GameManager.ID);

                Networking.currentInstance = instance;

                // Populate player list
                PopulatePlayerList();
                for (int i = 0; i < instance.players.Count; ++i)
                {
                    GameObject newPlayer = Instantiate<GameObject>(waitingInstancePlayerListEntryPrefab, waitingInstancePlayerListParent);
                    if (GameManager.players.ContainsKey(instance.players[i]))
                    {
                        newPlayer.transform.GetComponentInChildren<Text>().text = GameManager.players[instance.players[i]].username + (i == 0 ? " (Host)" : "");
                    }
                    else
                    {
                        newPlayer.transform.GetComponentInChildren<Text>().text = H3MP.Mod.config["Username"].ToString() + (i == 0 ? " (Host)" : "");
                    }
                    newPlayer.SetActive(true);
                }
            }
        }

        public void PopulatePlayerList()
        {
            while (waitingInstancePlayerListParent.childCount > 1)
            {
                Transform child = waitingInstancePlayerListParent.GetChild(1);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int maxPage = (Networking.currentInstance.players.Count - 1) / 6;
            if (waitingPlayerListPage > maxPage)
            {
                waitingPlayerListPage = maxPage;
            }

            for (int i= waitingPlayerListPage * 6; i< Networking.currentInstance.players.Count;++i)
            {
                if (waitingInstancePlayerListParent.childCount == 7)
                {
                    break;
                }

                GameObject newInstanceListEntry = Instantiate(waitingInstancePlayerListEntryPrefab, waitingInstancePlayerListParent);
                newInstanceListEntry.SetActive(true);

                if (GameManager.players.ContainsKey(Networking.currentInstance.players[i]))
                {
                    newInstanceListEntry.GetComponentInChildren<Text>().text = GameManager.players[Networking.currentInstance.players[i]].username + (i == 0 ? " (Host)" : "");
                }
                else
                {
                    newInstanceListEntry.GetComponentInChildren<Text>().text = H3MP.Mod.config["Username"].ToString() + (i == 0 ? " (Host)" : "");
                }
            }
        }

        public void OnWaitingBackClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(false);
            waitingServerPage.SetActive(false);
            SetPage(0);

            H3MP.GameManager.SetInstance(0);
            Networking.currentInstance = null;
        }

        public void OnWaitingInstancePlayerListClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(false);
            waitingInstancePlayerListPage.SetActive(true);

            waitingInstanceStartButton.SetActive(Networking.currentInstance.players[0] == GameManager.ID);
        }

        public void OnWaitingInstancePlayerListBackClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(true);
            waitingInstancePlayerListPage.SetActive(false);

            waitingInstanceStartButton.SetActive(Networking.currentInstance.players[0] == GameManager.ID);
        }

        public void OnWaitingInstanceStartClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(false);
            waitingInstancePlayerListPage.SetActive(false);
            loadingPage.SetActive(true);

            loadingRaid = true;
            Mod.raidMapBundleRequest = AssetBundle.LoadFromFileAsync(Mod.availableRaidMaps[Networking.currentInstance.map]);
            if(Mod.availableRaidMapAdditives.TryGetValue(Networking.currentInstance.map, out List<string> additivePaths))
            {
                Mod.raidMapAdditiveBundleRequests = new List<AssetBundleCreateRequest>();
                for(int i=0; i< additivePaths.Count; ++i)
                {
                    Mod.raidMapAdditiveBundleRequests.Add(AssetBundle.LoadFromFileAsync(additivePaths[i]));
                }
            }
            if(Mod.availableRaidMapPrefabs.TryGetValue(Networking.currentInstance.map, out List<string> prefabPaths))
            {
                Mod.raidMapPrefabBundleRequests = new List<AssetBundleCreateRequest>();
                for(int i=0; i< prefabPaths.Count; ++i)
                {
                    Mod.raidMapPrefabBundleRequests.Add(AssetBundle.LoadFromFileAsync(prefabPaths[i]));
                }
            }
        }

        public void OnOptionsNextClicked()
        {
            clickAudio.Play();
            for (int i = 0; i < optionPages.Length; ++i)
            {
                if (optionPages[i].gameObject.activeSelf)
                {
                    optionPages[i].gameObject.SetActive(false);
                    optionPages[i + 1].gameObject.SetActive(true);

                    if (i + 1 == optionPages.Length - 1)
                    {
                        //buttons[12][1].gameObject.SetActive(false);
                    }

                    //buttons[12][2].gameObject.SetActive(true);

                    break;
                }
            }
        }

        public void OnOptionsPreviousClicked()
        {
            clickAudio.Play();
            for (int i = 0; i < optionPages.Length; ++i)
            {
                if (optionPages[i].gameObject.activeSelf)
                {
                    optionPages[i].gameObject.SetActive(false);
                    optionPages[i - 1].gameObject.SetActive(true);

                    if (i - 1 == 0)
                    {
                        //buttons[12][2].gameObject.SetActive(false);
                    }

                    //buttons[12][1].gameObject.SetActive(true);

                    break;
                }
            }
        }

        public void OnScavBlockOKClicked()
        {
            clickAudio.Play();

            //buttons[3][0].GetComponent<Collider>().enabled = true;
            //buttons[3][1].GetComponent<Collider>().enabled = true;
        }

        public void OnCharClicked(int charIndex)
        {
            clickAudio.Play();

            Mod.chosenCharIndex = charIndex;

            // Update chosen char text
            confirmChosenCharacter.text = charIndex == 0 ? "PMC" : "Scav";
            loadingChosenCharacter.text = confirmChosenCharacter.text;

            bool previousScavItemFound = false;
            Transform scavRaidItemNodeParent = transform.GetChild(1).GetChild(25);
            foreach (Transform child in scavRaidItemNodeParent)
            {
                if (child.childCount > 0)
                {
                    previousScavItemFound = true;
                    break;
                }
            }

            if (previousScavItemFound)
            {
                //buttons[3][0].GetComponent<Collider>().enabled = false;
                //buttons[3][1].GetComponent<Collider>().enabled = false;

                scavBlock.gameObject.SetActive(true);
            }
            else
            {
                SetPage(4);
            }
        }

        public void OnMapClicked(string mapName)
        {
            clickAudio.Play();

            Mod.chosenMapName = mapName;

            // Update chosen map text
            switch (Mod.chosenMapIndex)
            {
                case 0:
                    confirmChosenMap.text = "Factory";
                    loadingChosenMap.text = "Factory";
                    break;
                default:
                    break;
            }

            SetPage(5);
        }

        public void OnTimeClicked(int timeIndex)
        {
            clickAudio.Play();

            Mod.chosenTimeIndex = timeIndex;
            SetPage(6);
        }

        public void OnConfirmRaidClicked()
        {
            clickAudio.Play();

            SetPage(7);

            // Begin loading raid map
            switch (Mod.chosenMapIndex)
            {
                case 0:
                    loadingRaid = true;
                    Mod.chosenMapName = "Factory";
                    Mod.raidMapBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/EscapeFromMeatovFactory.ab");
                    break;
                default:
                    loadingRaid = true;
                    Mod.chosenMapIndex = 0;
                    Mod.chosenCharIndex = 0;
                    Mod.chosenTimeIndex = 0;
                    Mod.chosenMapName = "Factory";
                    confirmChosenMap.text = "Factory";
                    loadingChosenMap.text = "Factory";
                    Mod.raidMapBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/EscapeFromMeatovFactory.ab");
                    break;
            }
        }

        public void OnCancelRaidLoadClicked()
        {
            clickAudio.Play();

            cancelRaidLoad = true;
        }

        public void OnRaidReportNextClicked()
        {
            clickAudio.Play();

            transform.GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetChild(13).gameObject.SetActive(true);
        }

        public void OnMedicalNextClicked()
        {
            clickAudio.Play();

            transform.GetChild(0).GetChild(0).GetChild(13).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);

            // Disable after raid collider
            transform.GetChild(1).GetChild(23).GetChild(3).gameObject.SetActive(false);
        }

        public void OnMedicalApplyClicked()
        {
            clickAudio.Play();

            // Check which parts are active, which conditions are active for each of those, fix them
            foreach (KeyValuePair<int, GameObject> partElement in medicalPartElements)
            {
                if (partElement.Value.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        if (partElement.Value.transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                        {
                            if (i == 0) // Health
                            {
                                Mod.SetHealth(partElement.Key, Mod.GetCurrentMaxHealth(partElement.Key));

                                // Update display
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(0).GetChild(partElement.Key).GetComponent<Image>().color = Color.white;
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(1).GetComponent<Text>().text = Mod.GetCurrentMaxHealth(partElement.Key).ToString() + "/" + Mod.GetCurrentMaxHealth(partElement.Key);
                            }
                            else if (i == 1) // LightBleeding
                            {
                                Effect.RemoveEffects(Effect.EffectType.LightBleeding);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(0).gameObject.SetActive(false);
                            }
                            else if (i == 2) // HeavyBleeding
                            {
                                Effect.RemoveEffects(Effect.EffectType.HeavyBleeding);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(1).gameObject.SetActive(false);
                            }
                            else if (i == 3) // Fracture
                            {
                                Effect.RemoveEffects(Effect.EffectType.Fracture);

                                // Update fracture icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(2).gameObject.SetActive(false);
                            }
                            else if (i == 4) // DestroyedPart
                            {
                                Effect.RemoveEffects(Effect.EffectType.DestroyedPart);

                                Mod.SetHealth(partElement.Key, Mod.GetCurrentMaxHealth(partElement.Key));

                                // Update display
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(0).GetChild(partElement.Key).GetComponent<Image>().color = Color.white;
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(1).GetComponent<Text>().text = Mod.GetCurrentMaxHealth(partElement.Key).ToString() + "/" + Mod.GetCurrentMaxHealth(partElement.Key);
                            }
                        }
                    }

                    Destroy(partElement.Value);
                }
            }

            // Remove money from player
            int amountToRemoveFromBase = 0;
            int amountToRemoveFromPlayer = 0;
            if (inventoryItems.ContainsKey("203"))
            {
                if (inventoryItems["203"].Count >= totalMedicalTreatmentPrice)
                {
                    amountToRemoveFromBase = totalMedicalTreatmentPrice;
                }
                else
                {
                    amountToRemoveFromBase = inventoryItems["203"].Count;
                    amountToRemoveFromPlayer = totalMedicalTreatmentPrice - inventoryItems["203"].Count;
                }
            }
            else
            {
                amountToRemoveFromPlayer = totalMedicalTreatmentPrice;
            }
            if (amountToRemoveFromBase > 0)
            {
                //inventory["203"] = inventory["203"].Count - amountToRemoveFromBase;
                //List<GameObject> objectList = inventory["203"].Count;
                //for (int i = objectList.Count - 1, j = amountToRemoveFromBase; i >= 0 && j > 0; --i)
                //{
                //    GameObject toCheck = objectList[objectList.Count - 1];
                //    MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                //    if (CIW.stack > amountToRemoveFromBase)
                //    {
                //        CIW.stack = CIW.stack - amountToRemoveFromBase;
                //        j = 0;
                //    }
                //    else // CIW.stack <= amountToRemoveFromBase
                //    {
                //        j -= CIW.stack;
                //        objectList.RemoveAt(objectList.Count - 1);
                //        CIW.physObj.SetQuickBeltSlot(null);
                //        CIW.destroyed = true;
                //        Destroy(toCheck);
                //    }
                //}
            }
            if (amountToRemoveFromPlayer > 0)
            {
                Mod.playerInventory["203"] = Mod.playerInventory["203"] - amountToRemoveFromPlayer;
                List<MeatovItem> objectList = Mod.playerInventoryItems["203"];
                for (int i = objectList.Count - 1, j = amountToRemoveFromPlayer; i >= 0 && j > 0; --i)
                {
                    MeatovItem toCheck = objectList[objectList.Count - 1];
                    if (toCheck.stack > amountToRemoveFromPlayer)
                    {
                        toCheck.stack = toCheck.stack - amountToRemoveFromPlayer;
                        j = 0;
                    }
                    else // CIW.stack <= amountToRemoveFromBase
                    {
                        j -= toCheck.stack;
                        objectList.RemoveAt(objectList.Count - 1);
                        toCheck.physObj.SetQuickBeltSlot(null);
                        toCheck.physObj.ForceBreakInteraction();
                        toCheck.Destroy();
                        Mod.weight -= toCheck.currentWeight;
                    }
                }
            }

            //foreach (BaseAreaManager baseAreaManager in baseAreaManagers)
            //{
            //    baseAreaManager.UpdateBasedOnItem("203");
            //}

            //transform.GetChild(0).GetChild(0).GetChild(13).GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));
        }

        private void ResetPlayerRig()
        {
            // Destroy and reset rig and equipment slots
            EquipmentSlot.Clear();
            for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
            {
                if (StatusUI.instance.equipmentSlots[i] != null && StatusUI.instance.equipmentSlots[i].CurObject != null)
                {
                    Destroy(StatusUI.instance.equipmentSlots[i].CurObject.gameObject);
                }
            }
            GM.CurrentPlayerBody.ConfigureQuickbelt(-2); // -2 in order to destroy the objects on belt as well
        }

        public void Save()
        {
            Mod.LogInfo("Saving hideout");

            // Write time
            loadedData["time"] = DateTime.UtcNow.Ticks;

            // Write player status
            loadedData["level"] = Mod.level;
            loadedData["experience"] = Mod.experience;
            loadedData["health"] = JArray.FromObject(Mod.GetHealthArray());
            loadedData["hydration"] = Mod.hydration;
            loadedData["energy"] = Mod.energy;
            loadedData["weight"] = Mod.weight;
            loadedData["totalRaidCount"] = Mod.totalRaidCount;
            loadedData["runthroughRaidCount"] = Mod.runthroughRaidCount;
            loadedData["survivedRaidCount"] = Mod.survivedRaidCount;
            loadedData["MIARaidCount"] = Mod.MIARaidCount;
            loadedData["KIARaidCount"] = Mod.KIARaidCount;
            loadedData["failedRaidCount"] = Mod.failedRaidCount;
            loadedData["hideout"]["scavTimer"] = scavTimer;

            // Write skills
            JArray skillData = new JArray();
            for (int i = 0; i < 64; ++i)
            {
                skillData.Add(new JObject());
                loadedData["skills"][i]["progress"] = Mod.skills[i].progress;
            }
            loadedData["skills"] = new JArray();

            // Save player items
            // Hands
            // Don't want to save as hand item if item has quickbeltslot, which would mean it is harnessed
            JObject serializedItem = null;
            if(Mod.leftHand.heldItem != null && Mod.leftHand.heldItem.physObj.QuickbeltSlot == null)
            {
                serializedItem = Mod.leftHand.heldItem.Serialize();
            }
            loadedData["leftHand"] = serializedItem;
            serializedItem = null;
            if (Mod.rightHand.heldItem != null && Mod.rightHand.heldItem.physObj.QuickbeltSlot == null)
            {
                serializedItem = Mod.rightHand.heldItem.Serialize();
            }
            loadedData["rightHand"] = serializedItem;
            // Equipment
            for(int i=0; i< StatusUI.instance.equipmentSlots.Length; ++i)
            {
                FVRPhysicalObject physObj = StatusUI.instance.equipmentSlots[0].CurObject;
                if(physObj == null)
                {
                    serializedItem = null;
                }
                else
                {
                    MeatovItem meatovItem = physObj.GetComponent<MeatovItem>();
                    serializedItem = meatovItem == null ? null : meatovItem.Serialize();
                }
                loadedData["equipment"+i] = serializedItem;
            }
            // QBS
            for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++i)
            {
                FVRPhysicalObject physObj = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject;
                if (physObj == null)
                {
                    serializedItem = null;
                }
                else
                {
                    MeatovItem meatovItem = physObj.GetComponent<MeatovItem>();
                    serializedItem = meatovItem == null ? null : meatovItem.Serialize();
                }
                loadedData["slot" + i] = serializedItem;
            }

            // Save hideout items
            // Loose items
            JArray looseItems = new JArray();
            foreach (KeyValuePair<string, List<MeatovItem>> entry in inventoryItems)
            {
                for(int i=0; i < entry.Value.Count; ++i)
                {
                    // Item considered loose if no parent, not in volume, and not in slot
                    if (entry.Value[i].parent == null && entry.Value[i].parentVolume == null && entry.Value[i].physObj.QuickbeltSlot == null)
                    {
                        JObject serialized = entry.Value[i].Serialize();
                        serialized["posX"] = entry.Value[i].transform.position.x;
                        serialized["posY"] = entry.Value[i].transform.position.y;
                        serialized["posZ"] = entry.Value[i].transform.position.z;
                        serialized["rotX"] = entry.Value[i].transform.rotation.eulerAngles.x;
                        serialized["rotY"] = entry.Value[i].transform.rotation.eulerAngles.y;
                        serialized["rotZ"] = entry.Value[i].transform.rotation.eulerAngles.z;
                        looseItems.Add(serialized);
                    }
                }
            }
            loadedData["hideout"]["looseItems"] = looseItems;
            // Trade volume
            JArray tradeVolumeItems = new JArray();
            foreach (KeyValuePair<string, List<MeatovItem>> entry in marketManager.tradeVolume.inventoryItems)
            {
                for (int i = 0; i < entry.Value.Count; ++i)
                {
                    // Only want to serialize root item so only consider inventory items without parent
                    if (entry.Value[i].parent == null)
                    {
                        JObject serialized = entry.Value[i].Serialize();
                        serialized["posX"] = entry.Value[i].transform.localPosition.x;
                        serialized["posY"] = entry.Value[i].transform.localPosition.y;
                        serialized["posZ"] = entry.Value[i].transform.localPosition.z;
                        serialized["rotX"] = entry.Value[i].transform.localRotation.eulerAngles.x;
                        serialized["rotY"] = entry.Value[i].transform.localRotation.eulerAngles.y;
                        serialized["rotZ"] = entry.Value[i].transform.localRotation.eulerAngles.z;
                        tradeVolumeItems.Add(serialized);
                    }
                }
            }
            loadedData["hideout"]["tradeVolumeItems"] = looseItems;
            // Area items
            JArray areas = new JArray();
            for(int i=0; i < areaController.areas.Length; ++i)
            {
                JObject area = new JObject();
                // Levels
                JArray areaLevels = new JArray();
                // Volumes
                for(int j=0; j < areaController.areas[i].levels.Length; ++j)
                {
                    JObject level = new JObject();
                    JArray volumes = new JArray();
                    JArray slots = new JArray();
                    for (int k=0; k< areaController.areas[i].levels[j].areaVolumes.Length; ++j)
                    {
                        JArray areaVolumeItems = new JArray();
                        foreach (KeyValuePair<string, List<MeatovItem>> entry in areaController.areas[i].levels[j].areaVolumes[k].inventoryItems)
                        {
                            for (int l = 0; l < entry.Value.Count; ++l)
                            {
                                // Only want to serialize root item so only consider inventory items without parent
                                if (entry.Value[l].parent == null)
                                {
                                    JObject serialized = entry.Value[l].Serialize();
                                    serialized["posX"] = entry.Value[l].transform.localPosition.x;
                                    serialized["posY"] = entry.Value[l].transform.localPosition.y;
                                    serialized["posZ"] = entry.Value[l].transform.localPosition.z;
                                    serialized["rotX"] = entry.Value[l].transform.localRotation.eulerAngles.x;
                                    serialized["rotY"] = entry.Value[l].transform.localRotation.eulerAngles.y;
                                    serialized["rotZ"] = entry.Value[l].transform.localRotation.eulerAngles.z;
                                    areaVolumeItems.Add(serialized);
                                }
                            }
                        }
                        volumes.Add(areaVolumeItems);
                    }
                    level["volumes"] = volumes;
                    for (int k = 0; k < areaController.areas[i].levels[j].areaSlots.Length; ++j)
                    {
                        JObject serialized = null;
                        if (areaController.areas[i].levels[j].areaSlots[k].CurObject != null)
                        {
                            MeatovItem meatovItem = areaController.areas[i].levels[j].areaSlots[k].CurObject.GetComponent<MeatovItem>();
                            serializedItem = meatovItem == null ? null : meatovItem.Serialize();
                        }
                        slots.Add(serialized);
                    }
                    level["slots"] = slots;
                    areaLevels.Add(level);
                }
                area["levels"] = areaLevels;
                areas.Add(area);
            }
            loadedData["hideout"]["areas"] = areas;
            // Scav return items
            JArray scavReturnItemArr = new JArray();
            MeatovItem[] scavReturnItems = scavReturnNode.GetComponentsInChildren<MeatovItem>();
            for(int i = 0; i < scavReturnItems.Length; ++i)
            {
                // Only want to serialize root item so only consider inventory items without parent
                if (scavReturnItems[i].parent == null)
                {
                    scavReturnItemArr.Add(scavReturnItems[i].Serialize());
                }
            }
            loadedData["hideout"]["scavReturnItems"] = scavReturnItemArr;

            // Save traders
            JArray traders = new JArray();
            for (int i=0; i < Mod.traders.Length; ++i)
            {
                JObject trader = new JObject();
                Mod.traders[i].Save(trader);
                traders.Add(trader);
            }
            loadedData["hideout"]["traders"] = traders;

            // Save areas
            for (int i = 0; i < areaController.areas.Length; ++i)
            {
                // Note area array and object have already been added when saving area items above
                areaController.areas[i].Save(areas[i]);
            }

            // Save insuredSets
            //Mod.insuredItems = new List<InsuredSet>();
            //if (Mod.insuredItems != null)
            //{
            //    JArray savedInsuredSets = new JArray();
            //    loadedData["insuredSets"] = savedInsuredSets;

            //    for (int i = 0; i < Mod.insuredItems.Count; ++i)
            //    {
            //        JObject newSavedInsuredSet = new JObject();
            //        newSavedInsuredSet["returnTime"] = Mod.insuredItems[i].returnTime;
            //        newSavedInsuredSet["items"] = JObject.FromObject(Mod.insuredItems[i].items);
            //        savedInsuredSets.Add(newSavedInsuredSet);
            //    }
            //}

            // Save triggered exploration triggers
            JArray savedExperiencetriggers = new JArray();
            loadedData["triggeredExplorationTriggers"] = savedExperiencetriggers;
            for (int i = 0; i < 12; ++i)
            {
                savedExperiencetriggers.Add(JArray.FromObject(Mod.triggeredExplorationTriggers[i]));
            }

            loadedData["whishlist"] = JArray.FromObject(Mod.wishList);

            SaveDataToFile();
            Mod.LogInfo("Saved hideout");
            UpdateLoadButtonList();
        }

        public void FinishRaid(FinishRaidState state)
        {
            // Increment raid counters
            ++Mod.totalRaidCount;
            switch (state)
            {
                case FinishRaidState.RunThrough:
                    ++Mod.runthroughRaidCount;
                    ++Mod.survivedRaidCount;
                    break;
                case FinishRaidState.Survived:
                    ++Mod.survivedRaidCount;
                    break;
                case FinishRaidState.MIA:
                    ++Mod.MIARaidCount;
                    ++Mod.failedRaidCount;
                    break;
                case FinishRaidState.KIA:
                    ++Mod.KIARaidCount;
                    ++Mod.failedRaidCount;
                    break;
                default:
                    break;
            }

            // Save the base
            Mod.saveSlotIndex = 5;
            Save();
        }

        private void SaveDataToFile()
        {
            File.WriteAllText(Mod.path + "/EscapeFromMeatov/" + (Mod.saveSlotIndex == 5 ? "AutoSave" : "Slot" + Mod.saveSlotIndex) + ".sav", loadedData.ToString());
        }

        public void OnHideoutInventoryChangedInvoke()
        {
            // Raise event
            if (OnHideoutInventoryChanged != null)
            {
                OnHideoutInventoryChanged();
            }
        }

        public void OnHideoutItemInventoryChangedInvoke(MeatovItemData itemData, int difference)
        {
            // Raise event
            if (OnHideoutItemInventoryChanged != null)
            {
                OnHideoutItemInventoryChanged(itemData, difference);
            }
            itemData.OnHideoutItemInventoryChangedInvoke(difference);
        }
    }

    public class TimedDisabler : MonoBehaviour
    {
        public float time = 1;
        public float timer;

        public void Start()
        {
            timer = time;
        }

        public void Update()
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                gameObject.SetActive(false);
                Destroy(this);
            }
        }
    }
}
