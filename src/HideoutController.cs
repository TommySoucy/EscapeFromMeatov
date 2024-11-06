using FistVR;
using H3MP;
using H3MP.Networking;
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
        public Text loadingCountdownText;
        public AudioSource clickAudio;
        public Text scavTimerText;
        public Collider scavButtonCollider;
        public Text scavButtonText;
        public GameObject charChoicePanel;
        public GameObject[] saveConfirmTexts;
        public Transform[] optionPages;
        public Transform mapListParent;
        public GameObject mapListNextButton;
        public GameObject mapListPreviousButton;
        public GameObject mapListEntryPrefab;
        public int mapListPage;
        public Image raidReportRankIcon;
        public Text raidReportLevel;
        public Text raidReportExperience;
        public RectTransform raidReportBarFill;
        public GameObject raidReportKillsParent;
        public GameObject raidReportKillEntry;
        public GameObject raidReportExplorationParent;
        public GameObject raidReportHealingParent;
        public GameObject raidReportLootingParent;
        public HoverScrollProcessor raidReportHoverScrollProcessor;
        public Text raidReportStatus;
        public Color[] raidReportTotalBackgroundColors; // Success, Run Through, KIA, MIA
        public Image raidReportTotalBackground;
        public Text raidReportTotalExp;
        public Text[] treatmentPartHealths;
        public Image[] treatmentPartImages;
        public GameObject[] treatmentFractureIcons;
        public GameObject[] treatmentLightBleedIcons;
        public GameObject[] treatmentHeavyBleedIcons;
        public Text treatmentTotalHeatlh;
        public Text treatmentInventoryMoney;
        public Text treatmentTotalCost;
        public Transform treatmentListParent;
        public GameObject treatmentPartEntry;
        public GameObject treatmentPartSubEntry;
        public GameObject treatmentApplyButton;
        public HoverScrollProcessor treatmentHoverScrollProcessor;
        public Dictionary<int, GameObject> treatmentParentsByIndex; // <part index, treatment object parent>
        public Dictionary<int, Dictionary<Effect.EffectType, GameObject>> treatmentObjectsByEffectTypeByParent; // <part index, <effect type, treatment object>>
        public Dictionary<GameObject, Dictionary<GameObject, int>> treatmentObjectsByParent; // <parent, <object, cost>>
        public Dictionary<GameObject, GameObject> treatmentParentsByObject; // <object, parent>
        public Dictionary<GameObject, int> treatmentCosts; // <object, cost>
        public Dictionary<GameObject, bool> treatmentSelected; // <object, selected>
        // H3MP
        public GameObject instancesPage;
        public Transform instancesParent;
        public GameObject instancesNextButton;
        public GameObject instancesPreviousButton;
        public GameObject instanceEntryPrefab;
        public GameObject newInstance0Page;
        public Text newInstancePMCCheckText;
        public Text newInstanceScavCheckText;
        public Text newInstanceScavTimerText;
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
        public Text joinInstanceScavTimerText;
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
        public GameObject waitingInstancePlayerListPreviousButton;
        public GameObject waitingInstancePlayerListNextButton;
        public GameObject waitForDeployPage;
        public Text waitForDeployCountText;
        public GameObject countdownPage;
        public Text countdownText;

        // Assets
        public static GameObject areaCanvasPrefab; // AreaCanvas

        // Live data
        public static JObject loadedData;
        public static float[] defaultHealthRates;
        public static float defaultEnergyRate;
        public static float defaultHydrationRate;
        public static DateTime saveTime;
        public static double secondsSinceSave;
        public float time;
        public bool cancelRaidLoad;
        public bool countdownDeploy;
        public bool waitForDeploy;
        public float deployTimer;
        public float deployTime = 5;
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
        public int totalTreatmentCost;
        public MarketManager marketManager;
        public static bool inMarket; // whether we are currently in market mode or in area UI mode
        public GCManager GCManager;
        public int instancesListPage;
        public int newInstanceMapListPage;
        public int waitingPlayerListPage;
        public bool saved;

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

            // Reset current max health
            Mod.SetCurrentMaxHealthArray(Mod.defaultMaxHealth);

            // Remove raid rates if necessary
            if (Mod.justFinishedRaid)
            {
                Mod.baseEnergyRate -= RaidManager.defaultEnergyRate;
                Mod.baseHydrationRate -= RaidManager.defaultHydrationRate;
            }

            // Add hideout rates
            for (int i = 0; i < Mod.GetHealthCount(); ++i)
            {
                Mod.SetBasePositiveHealthRate(i, Mod.GetBasePositiveHealthRate(i) + defaultHealthRates[i]);
            }
            Mod.baseEnergyRate += defaultEnergyRate;
            Mod.baseHydrationRate += defaultHydrationRate;

            Mod.LogInfo("\t0");
            if (StatusUI.instance == null)
            {
                SetupPlayerRig();
            }

            ammoBoxesByRoundClassByRoundType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>>();

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
            if(Area.areaDatas == null)
            {
                Area.areaDatas = new AreaData[highestIndex + 1];
                for (int i = 0; i < Area.areaDatas.Length; ++i)
                {
                    Area.areaDatas[i] = new AreaData(i);
                }
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
            InitTime();
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

            // Sub to UI events
            foreach(KeyValuePair<string, string> raidMapEntry in Mod.availableRaidMaps)
            {
                if(Mod.raidMapEntryRequirements.TryGetValue(raidMapEntry.Key, out Dictionary<string, int> requirements))
                {
                    foreach(KeyValuePair<string, int> requirement in requirements)
                    {
                        if(Mod.defaultItemData.TryGetValue(requirement.Key, out MeatovItemData itemData))
                        {
                            itemData.OnPlayerItemInventoryChanged += OnPlayerItemInventoryChanged;
                        }
                        else
                        {
                            Mod.LogError("Could not get item data for raid map " + raidMapEntry.Key + " requirement of x" + requirement.Value + " " + requirement.Key);
                        }
                    }
                }
            }

            if (Mod.justFinishedRaid)
            {
                FinishRaid(Mod.raidStatus); // This will save on autosave
            }

            Mod.justFinishedRaid = false;
            init = true;

            Mod.LogInfo("\t0");
            InitUI();
        }

        public void Update()
        {
            if (!saved)
            {
                Mod.saveSlotIndex = 5;
                Save();
                saved = true;
            }

            if (init)
            {
                UpdateTime();
            }

            UpdateScavTimer();

            Effect.UpdateStatic();

            //UpdateInsuredSets();

            UpdateRates();

            // Handle raid loading process
            if (cancelRaidLoad)
            {
                countdownDeploy = false;
                cancelRaidLoad = false;
                SetPage(0);

                // Unload raid map assets
                if(Mod.raidMapBundle != null)
                {
                    Mod.raidMapBundle.Unload(true);
                    Mod.raidMapBundle = null;
                }
                if (Mod.raidMapAdditiveBundles != null)
                {
                    for (int i = 0; i < Mod.raidMapAdditiveBundles.Count; ++i)
                    {
                        if(Mod.raidMapAdditiveBundles[i] != null)
                        {
                            Mod.raidMapAdditiveBundles[i].Unload(true);
                        }
                    }
                    Mod.raidMapAdditiveBundles = null;
                }
                if (Mod.raidMapPrefabBundles != null)
                {
                    for (int i = 0; i < Mod.raidMapPrefabBundles.Count; ++i)
                    {
                        if (Mod.raidMapPrefabBundles[i] != null)
                        {
                            Mod.raidMapPrefabBundles[i].Unload(true);
                        }
                    }
                    Mod.raidMapPrefabBundles = null;
                }
            }
            else if (countdownDeploy)
            {
                deployTimer -= Time.deltaTime;

                loadingCountdownText.text = Mod.FormatTimeString(deployTimer);
                countdownText.text = Mod.FormatTimeString(deployTimer);

                if (deployTimer <= 0)
                {
                    bool securedItems = false;
                    try
                    {
                        // Check if fulfill map requirements, if don't, cancel load
                        bool canLoad = true;
                        if (Mod.raidMapEntryRequirements.TryGetValue(Mod.mapChoiceName, out Dictionary<string, int> itemRequirementsCheck))
                        {
                            foreach (KeyValuePair<string, int> item in itemRequirementsCheck)
                            {
                                int currentCount = 0;
                                if (!Mod.playerInventory.TryGetValue(item.Key, out currentCount) || currentCount < item.Value)
                                {
                                    canLoad = false;
                                    break;
                                }
                            }
                        }

                        if (canLoad)
                        {
                            // Consume requirements
                            if (Mod.raidMapEntryRequirements.TryGetValue(Mod.mapChoiceName, out Dictionary<string, int> itemRequirements))
                            {
                                foreach (KeyValuePair<string, int> itemRequirement in itemRequirements)
                                {
                                    List<MeatovItem> potentialItems = Mod.playerInventoryItems[itemRequirement.Key];
                                    int countLeft = itemRequirement.Value;
                                    while (countLeft > 0)
                                    {
                                        MeatovItem item = potentialItems[potentialItems.Count - 1];
                                        if (item.stack > countLeft)
                                        {
                                            item.stack -= countLeft;
                                            countLeft = 0;
                                            break;
                                        }
                                        else
                                        {
                                            countLeft -= item.stack;
                                            if (!item.DetachChildren())
                                            {
                                                item.Destroy();
                                            }
                                        }
                                    }
                                }
                            }

                            // Autosave before starting the raid
                            Mod.saveSlotIndex = 5;
                            Save();

                            // Secure all items player has
                            if (Mod.charChoicePMC)
                            {
                                Mod.SecureInventory();
                                securedItems = true;
                            }

                            // Load raid map assets
                            Mod.mapChoiceName = Networking.currentInstance == null ? Mod.mapChoiceName : Networking.currentInstance.map;
                            Mod.raidMapBundle = AssetBundle.LoadFromFile(Mod.availableRaidMaps[Mod.mapChoiceName]);
                            if (Mod.availableRaidMapAdditives.TryGetValue(Mod.mapChoiceName, out List<string> additivePaths))
                            {
                                Mod.raidMapAdditiveBundles = new List<AssetBundle>();
                                for (int i = 0; i < additivePaths.Count; ++i)
                                {
                                    Mod.raidMapAdditiveBundles.Add(AssetBundle.LoadFromFile(additivePaths[i]));
                                }
                            }
                            else
                            {
                                Mod.raidMapAdditiveBundles = null;
                            }
                            if (Mod.availableRaidMapPrefabs.TryGetValue(Mod.mapChoiceName, out List<string> prefabPaths))
                            {
                                Mod.raidMapPrefabBundles = new List<AssetBundle>();
                                for (int i = 0; i < prefabPaths.Count; ++i)
                                {
                                    Mod.raidMapPrefabBundles.Add(AssetBundle.LoadFromFile(prefabPaths[i]));
                                }
                            }
                            else
                            {
                                Mod.raidMapPrefabBundles = null;
                            }

                            // Load raid scene
                            Mod.loadingToMeatovScene = true;
                            Mod.unloadHideout = true;
                            Mod.keepInstance = true;
                            SteamVR_LoadLevel.Begin(Mod.mapChoiceName, false, 0.5f, 0f, 0f, 0f, 1f);
                            countdownDeploy = false;
                        }
                        else
                        {
                            if(Networking.currentInstance != null)
                            {
                                GameManager.SetInstance(0);
                            }
                            cancelRaidLoad = true;
                            Mod.LogError("Got to raid scene load but could not fulfill all requirements, cancelling");
                        }
                    }
                    catch(Exception ex)
                    {
                        if (Networking.currentInstance != null)
                        {
                            GameManager.SetInstance(0);
                        }
                        cancelRaidLoad = true;
                        if (securedItems)
                        {
                            Mod.UnsecureInventory();
                        }
                        Mod.LogError("Could not load level: "+Mod.mapChoiceName+", cancelling: "+ ex.Message+"\n"+ ex.StackTrace);
                    }
                }
            }
        }

        public void UpdateRates()
        {
            for(int i=0; i < Mod.GetHealthCount(); ++i)
            {
                // Min ensures we cannot lose health in hideout, but we cannot gain any if negative health rate is greater than positive
                Mod.SetHealth(i, Mathf.Max(0, Mathf.Min(Mod.GetCurrentMaxHealth(i), Mod.GetHealth(i) + Mathf.Max(0, Mod.GetCurrentHealthRate(i) + Mod.GetCurrentNonLethalHealthRate(i)) * Time.deltaTime)));
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
                newInstanceScavTimerText.gameObject.SetActive(false);
                joinInstanceScavTimerText.gameObject.SetActive(false);
            }
            else
            {
                // Update scav timer text
                string formattedString = Mod.FormatTimeString(scavTimer);
                scavTimerText.text = formattedString;
                newInstanceScavTimerText.text = formattedString;
                joinInstanceScavTimerText.text = formattedString;

                if (!scavTimerText.gameObject.activeSelf)
                {
                    // Disable Scav button, Enable scav timer text
                    scavButtonCollider.enabled = false;
                    scavButtonText.color = Color.grey;
                    scavTimerText.gameObject.SetActive(true);
                }

                if (!newInstanceScavTimerText.gameObject.activeSelf)
                {
                    scavTimerText.gameObject.SetActive(true);
                }
                if (!joinInstanceScavTimerText.gameObject.activeSelf)
                {
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
            time += UnityEngine.Time.deltaTime * Mod.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            string formattedTime0 = Mod.FormatTimeString(time);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            string formattedTime1 = Mod.FormatTimeString(offsetTime);
            timeChoice1.text = formattedTime1;

            if (Mod.timeChoiceIs0)
            {
                waitingInstanceTimeText.text = "Time: " + formattedTime0;
            }
            else
            {
                waitingInstanceTimeText.text = "Time: " + formattedTime1;
            }

            confirmChosenTime.text = Mod.timeChoiceIs0 ? formattedTime0 : formattedTime1;
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
            // Extraction UI
            Mod.extractionUI = Instantiate(Mod.extractionUIPrefab, GM.CurrentPlayerBody.Head);
            Mod.extractionUIText = Mod.extractionUI.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
            Mod.extractionUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            Mod.extractionUI.transform.localPosition = new Vector3(0, 0.4f, 0.6f);
            Mod.extractionUI.SetActive(false);
            // Extraction limit UI
            Mod.extractionLimitUI = Instantiate(Mod.extractionLimitUIPrefab, GM.CurrentPlayerBody.Head);
            Mod.extractionLimitUIText = Mod.extractionLimitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            Mod.extractionLimitUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            Mod.extractionLimitUI.transform.localPosition = new Vector3(0, 0.38f, 0.6f);
            Mod.extractionLimitUI.SetActive(false);
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
            //GM.CurrentMovementManager.Mode = FVRMovementManager.MovementMode.TwinStick;
            //GM.Options.MovementOptions.Touchpad_Confirm = FVRMovementManager.TwoAxisMovementConfirm.OnTouch;
            //GM.Options.ControlOptions.CCM = ControlOptions.CoreControlMode.Streamlined;

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

            // Init hideout inventories
            // Note that this must happen before we load player items because custom items
            // will be instantiated, have their data set (which adds to to hideout inventory),
            // and only then get put into their slot (which will again update their location)
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

            // Load the save time
            saveTime = new DateTime((long)HideoutController.loadedData["time"]);
            secondsSinceSave = (float)DateTime.UtcNow.Subtract(saveTime).TotalSeconds;
            float minutesSinceSave = (float)secondsSinceSave / 60.0f;

            Mod.LogInfo("\t0");
            // Load player data only if we don't want to use the data from the PMC raid we just finished
            if (!Mod.justFinishedRaid || !Mod.charChoicePMC)
            {
                Mod.LogInfo("\t\tNot finished raid or was scav, loading player from save");
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
            else if (Mod.justFinishedRaid) // Just finished raid as PMC
            {
                Mod.LogInfo("\t\tFinished raid, not loading player from save");
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }

                // If we were PMC in raid, unsecure secured items
                if (Mod.charChoicePMC)
                {
                    Mod.UnsecureInventory();
                }

                // Set any parts health to 1 if they are at 0
                // Set it to a third of max if failed raid
                for (int i = 0; i < Mod.GetHealthCount(); ++i)
                {
                    if (Mod.raidStatus == RaidManager.RaidStatus.MIA || Mod.raidStatus == RaidManager.RaidStatus.KIA)
                    {
                         // Note that max health gets reset in awake
                        Mod.SetHealth(i, Mod.GetCurrentMaxHealth(i) / 3);
                    }
                    else if (Mod.GetHealth(i) == 0)
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

            // Load hideout items
            LoadHideoutItems();
            Mod.LogInfo("\t0");

            // Load trader data
            JArray traderDataArray = loadedData["hideout"]["traders"] as JArray;
            for(int i=0; i < Mod.traders.Length; ++i)
            {
                Mod.traders[i].LoadData(traderDataArray[i]);
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
                if (loadedData["triggeredExplorationTriggers"] != null)
                {
                    Mod.triggeredExperienceTriggers = loadedData["triggeredExplorationTriggers"].ToObject<List<string>>();
                }
                else
                {
                    Mod.triggeredExperienceTriggers = new List<string>();
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

            // Load task data only after loading all trader data and checkmark data because some task conditions are dependent on trader live data
            // and checkmark data will have to update on events based on task data we load
            for (int i = 0; i < Mod.traders.Length; ++i)
            {
                for (int j = 0; j < Mod.traders[i].tasks.Count; ++j)
                {
                    if (traderDataArray[i]["tasks"][Mod.traders[i].tasks[j].ID] == null)
                    {
                        Mod.traders[i].tasks[j].LoadData(null);
                    }
                    else
                    {
                        Mod.traders[i].tasks[j].LoadData(traderDataArray[i]["tasks"][Mod.traders[i].tasks[j].ID]);
                    }
                }
            }

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
                            string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                            if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int j = 1; j < objs.Count; ++j)
                            {
                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    currentTarkovID = vanillaCustomData["children"][j - 1]["tarkovID"].ToString();
                                    if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                    {
                                        childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                    }
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][j - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][j - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][j - 1]["foundInRaid"];
                                }
                            }

                            if (meatovItem.physObj != null && meatovItem.physObj.RootRigidbody != null)
                            {
                                meatovItem.physObj.RootRigidbody.isKinematic = true;
                            }
                            meatovItem.transform.position = new Vector3((float)looseItemData["posX"], (float)looseItemData["posY"], (float)looseItemData["posZ"]);
                            meatovItem.transform.rotation = Quaternion.Euler((float)looseItemData["rotX"], (float)looseItemData["rotY"], (float)looseItemData["rotZ"]);
                            if (meatovItem.physObj != null && meatovItem.physObj.RootRigidbody != null)
                            {
                                meatovItem.physObj.RootRigidbody.isKinematic = false;
                            }
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(looseItemData, del);

                    if (loadedItem != null)
                    {
                        if (loadedItem.physObj != null && loadedItem.physObj.RootRigidbody != null)
                        {
                            loadedItem.physObj.RootRigidbody.isKinematic = true;
                        }
                        loadedItem.transform.position = new Vector3((float)looseItemData["posX"], (float)looseItemData["posY"], (float)looseItemData["posZ"]);
                        loadedItem.transform.rotation = Quaternion.Euler((float)looseItemData["rotX"], (float)looseItemData["rotY"], (float)looseItemData["rotZ"]);
                        if (loadedItem.physObj != null && loadedItem.physObj.RootRigidbody != null)
                        {
                            loadedItem.physObj.RootRigidbody.isKinematic = false;
                        }
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
                            string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                            if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int j = 1; j < objs.Count; ++j)
                            {
                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    currentTarkovID = vanillaCustomData["children"][j - 1]["tarkovID"].ToString();
                                    if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                    {
                                        childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                    }
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
                            JToken level = areaLevels[j];
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
                                                string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                                                if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                                                {
                                                    meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                                }
                                                meatovItem.insured = (bool)vanillaCustomData["insured"];
                                                meatovItem.looted = (bool)vanillaCustomData["looted"];
                                                meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                                                for (int m = 1; m < objs.Count; ++m)
                                                {
                                                    MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                                    if (childMeatovItem != null)
                                                    {
                                                        currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                                        if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                                        {
                                                            childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                                        }
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
                                    if (slots[k] != null && slots[k].Type != JTokenType.Null)
                                    {
                                        JToken slotItemData = slots[k];
                                        JToken vanillaCustomData = slotItemData["vanillaCustomData"];
                                        VaultSystem.ReturnObjectListDelegate del = objs =>
                                        {
                                            // Here, assume objs[0] is the root item
                                            MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                                            if (meatovItem != null)
                                            {
                                                // Set live data
                                                string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                                                if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                                                {
                                                    meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                                }
                                                meatovItem.insured = (bool)vanillaCustomData["insured"];
                                                meatovItem.looted = (bool)vanillaCustomData["looted"];
                                                meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                                                for (int m = 1; m < objs.Count; ++m)
                                                {
                                                    MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                                    if (childMeatovItem != null)
                                                    {
                                                        currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                                        if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                                        {
                                                            childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                                        }
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
                            string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                            if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                    if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                    {
                                        childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                    }
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            objs[0].StoreAndDestroyRigidbody();
                            objs[0].SetParentage(scavReturnNode);
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(scavItemData, del);

                    if (loadedItem != null)
                    {
                        loadedItem.physObj.StoreAndDestroyRigidbody();
                        loadedItem.physObj.SetParentage(scavReturnNode);
                    }
                }
            }
        }

        public void LoadPlayerItems()
        {
            Mod.LogInfo("LoadPlayerItems");
            // If have secured items, unsecured them on scav return node
            Mod.UnsecureInventory(true);

            if (loadedData["leftHand"] != null && loadedData["leftHand"].Type != JTokenType.Null)
            {
                Mod.LogInfo("\tLeft hand");
                // In case item is vanilla, in which case we use the vault system to save it,
                // we will only be getting the instantiated item later
                // We must write a delegate in order to put it in the correct hand once we do
                JToken vanillaCustomData = null;
                if (loadedData["leftHand"]["vanillaCustomData"] != null)
                {
                    vanillaCustomData = loadedData["leftHand"]["vanillaCustomData"];
                }
                VaultSystem.ReturnObjectListDelegate del = objs =>
                {
                    // Here, assume objs[0] is the root item
                    MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                    if (meatovItem != null)
                    {
                        // Set live data
                        string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                        if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                        {
                            meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                        }
                        meatovItem.insured = (bool)vanillaCustomData["insured"];
                        meatovItem.looted = (bool)vanillaCustomData["looted"];
                        meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                        for (int m = 1; m < objs.Count; ++m)
                        {
                            MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                            if (childMeatovItem != null)
                            {
                                currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                }
                                childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                            }
                        }

                        objs[0].BeginInteraction(Mod.leftHand.fvrHand);
                        Mod.leftHand.fvrHand.ForceSetInteractable(objs[0]);
                        meatovItem.UpdateInventories(false, false, false);
                    }
                };

                // In case item is custom, it will be returned right away and we can handle it here
                MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["leftHand"], del);

                if(loadedItem != null)
                {
                    loadedItem.physObj.BeginInteraction(Mod.leftHand.fvrHand);
                    Mod.leftHand.fvrHand.ForceSetInteractable(loadedItem.physObj);
                    loadedItem.UpdateInventories(false, false, false);
                }
            }
            if(loadedData["rightHand"] != null && loadedData["rightHand"].Type != JTokenType.Null)
            {
                Mod.LogInfo("\tRight hand");
                JToken vanillaCustomData = null;
                if (loadedData["rightHand"]["vanillaCustomData"] != null)
                {
                    vanillaCustomData = loadedData["rightHand"]["vanillaCustomData"];
                }
                VaultSystem.ReturnObjectListDelegate del = objs =>
                {
                    // Here, assume objs[0] is the root item
                    MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                    if (meatovItem != null)
                    {
                        // Set live data
                        string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                        if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                        {
                            meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                        }
                        meatovItem.insured = (bool)vanillaCustomData["insured"];
                        meatovItem.looted = (bool)vanillaCustomData["looted"];
                        meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                        for (int m = 1; m < objs.Count; ++m)
                        {
                            MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                            if (childMeatovItem != null)
                            {
                                currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                {
                                    childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                }
                                childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                            }
                        }

                        objs[0].BeginInteraction(Mod.rightHand.fvrHand);
                        Mod.rightHand.fvrHand.ForceSetInteractable(objs[0]);
                        meatovItem.UpdateInventories(false, false, false);
                    }
                };

                MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["rightHand"], del);

                if(loadedItem != null)
                {
                    loadedItem.physObj.BeginInteraction(Mod.rightHand.fvrHand);
                    Mod.rightHand.fvrHand.ForceSetInteractable(loadedItem.physObj);
                    loadedItem.UpdateInventories(false, false, false);
                }
            }
            // Note that we must load equipment before loading QBS items
            // This is so that if we have a rig it will have been loaded
            for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
            {
                if (loadedData["equipment"+i] != null && loadedData["equipment" + i].Type != JTokenType.Null)
                {
                    Mod.LogInfo("\tEquipment"+i);
                    JToken vanillaCustomData = null;
                    if (loadedData["equipment" + i]["vanillaCustomData"] != null)
                    {
                        vanillaCustomData = loadedData["equipment" + i]["vanillaCustomData"];
                    }
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                            if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                    if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                    {
                                        childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                    }
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            objs[0].SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i]);
                            meatovItem.UpdateInventories(false, false, false);
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["equipment" + i], del);

                    if (loadedItem != null)
                    {
                        loadedItem.physObj.SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i]);
                        loadedItem.UpdateInventories(false, false, false);
                    }
                }
            }
            // Get player body in case it wasn't set yet
            FVRPlayerBody playerBodyToUse = null;
            if (GM.CurrentPlayerBody == null)
            {
                Mod.LogWarning("DEV: PRELOADING OF PLAYERBODY WHEN LOADING PLAYER ITEMS WAS NEEDED!");
                playerBodyToUse = FindObjectOfType<FVRPlayerBody>();
            }
            else
            {
                Mod.LogWarning("DEV: Preloading of playerbody when loading player items was not needed!");
                playerBodyToUse = GM.CurrentPlayerBody;
            }
            for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++i)
            {
                if (loadedData["slot"+i] != null && loadedData["slot" + i].Type != JTokenType.Null)
                {
                    Mod.LogInfo("\tSlot" + i);
                    JToken vanillaCustomData = null;
                    if (loadedData["slot" + i]["vanillaCustomData"] != null)
                    {
                        vanillaCustomData = loadedData["slot" + i]["vanillaCustomData"];
                    }
                    int slotIndex = i;
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Set live data
                            string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                            if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                            for (int m = 1; m < objs.Count; ++m)
                            {
                                MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                                if (childMeatovItem != null)
                                {
                                    currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                                    if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                                    {
                                        childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                                    }
                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                                }
                            }

                            objs[0].SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[slotIndex]);
                            meatovItem.UpdateInventories(false, false, false);
                        }
                    };

                    MeatovItem loadedItem = MeatovItem.Deserialize(loadedData["slot" + i], del);

                    if (loadedItem != null)
                    {
                        loadedItem.physObj.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i]);
                        loadedItem.UpdateInventories(false, false, false);
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

            if (Mod.justFinishedRaid && Mod.charChoicePMC)
            {
                // Raid Report
                SetPage(8);
                Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;
                Mod.OnPlayerExperienceChanged += OnPlayerExperienceChanged;
                OnPlayerLevelChanged();
                OnPlayerExperienceChanged();
                if (Mod.raidKills != null && Mod.raidKills.Count > 0)
                {
                    raidReportKillsParent.SetActive(true);
                    for (int i = 0; i < Mod.raidKills.Count; ++i)
                    {
                        GameObject killEntryObject = Instantiate(raidReportKillEntry, raidReportKillsParent.transform);
                        killEntryObject.SetActive(true);
                        if (Mod.raidKills[i].enemyTarget == ConditionCounter.EnemyTarget.Savage)
                        {
                            killEntryObject.transform.GetChild(0).GetComponent<Text>().text = "Scav";
                            killEntryObject.transform.GetChild(1).GetComponent<Text>().text = "";
                        }
                        else // PMC
                        {
                            killEntryObject.transform.GetChild(0).GetComponent<Text>().text = "lvl. " + Mod.raidKills[i].level;
                            killEntryObject.transform.GetChild(1).GetComponent<Text>().text = Mod.raidKills[i].name;
                        }
                        int killXP = Mod.raidKills[i].baseExperienceReward + (Mod.raidKills[i].bodyPart == ConditionCounter.TargetBodyPart.Head ? 200 : 0);
                        killEntryObject.transform.GetChild(2).GetComponent<Text>().text = killXP.ToString()+"xp";
                    }
                }
                if (Mod.explorationExp > 0)
                {
                    raidReportExplorationParent.SetActive(true);
                    raidReportExplorationParent.transform.GetChild(0).GetComponent<Text>().text = "EXPLORATION: " + Mod.explorationExp + "xp";
                }
                if (Mod.healingExp > 0)
                {
                    raidReportHealingParent.SetActive(true);
                    raidReportHealingParent.transform.GetChild(0).GetComponent<Text>().text = "HEALING: " + Mod.healingExp + "xp";
                }
                if (Mod.lootingExp > 0)
                {
                    raidReportLootingParent.SetActive(true);
                    raidReportLootingParent.transform.GetChild(0).GetComponent<Text>().text = "LOOTING: " + Mod.healingExp + "xp";
                }
                switch (Mod.raidStatus)
                {
                    case RaidManager.RaidStatus.Success:
                        raidReportStatus.text = "Survived";
                        break;
                    case RaidManager.RaidStatus.RunThrough:
                        raidReportStatus.text = "Run Through";
                        break;
                    case RaidManager.RaidStatus.KIA:
                        raidReportStatus.text = "KIA";
                        break;
                    case RaidManager.RaidStatus.MIA:
                        raidReportStatus.text = "MIA";
                        break;
                }
                raidReportTotalBackground.color = raidReportTotalBackgroundColors[(int)Mod.raidStatus];
                raidReportTotalExp.text = Mod.raidExp.ToString();
                raidReportHoverScrollProcessor.mustUpdateMiddleHeight = 1;

                // Treatment
                if (treatmentCosts == null)
                {
                    treatmentCosts = new Dictionary<GameObject, int>();
                    treatmentObjectsByParent = new Dictionary<GameObject, Dictionary<GameObject, int>>();
                    treatmentParentsByObject = new Dictionary<GameObject, GameObject>();
                    treatmentObjectsByEffectTypeByParent = new Dictionary<int, Dictionary<Effect.EffectType, GameObject>>();
                    treatmentParentsByIndex = new Dictionary<int, GameObject>();
                    treatmentSelected = new Dictionary<GameObject, bool>();
                }
                Mod.OnPartHealthChanged += OnPartHealthChanged;
                Mod.OnPartCurrentMaxHealthChanged += OnPartHealthChanged;
                OnPartHealthChanged(-1);
                SetEffecTreatments();
                Effect.OnEffectRemoved += OnPartEffectRemoved;
                treatmentHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            }
        }

        public void OnRaidReportNextClicked()
        {
            Mod.OnPlayerLevelChanged -= OnPlayerLevelChanged;
            Mod.OnPlayerExperienceChanged -= OnPlayerExperienceChanged;
            SetPage(9);
            clickAudio.Play();
        }

        public void OnTreatmentNextClicked()
        {
            Mod.OnPartHealthChanged -= OnPartHealthChanged;
            Mod.OnPartCurrentMaxHealthChanged -= OnPartHealthChanged;
            SetPage(0);
            clickAudio.Play();
        }

        public void OnTreatmentApplyClicked()
        {
            // Consume cost
            int countLeft = totalTreatmentCost;
            if (Mod.playerInventoryItems.TryGetValue("5449016a4bdc2d6f028b456f", out List<MeatovItem> playerRoubles))
            {
                while (countLeft > 0 && playerRoubles.Count > 0)
                {
                    MeatovItem item = playerRoubles[playerRoubles.Count - 1];
                    if (item.stack > countLeft)
                    {
                        item.stack -= countLeft;
                        countLeft = 0;
                        break;
                    }
                    else
                    {
                        countLeft -= item.stack;
                        if (!item.DetachChildren())
                        {
                            item.Destroy();
                        }
                    }
                }
            }
            if(countLeft > 0 && inventoryItems.TryGetValue("5449016a4bdc2d6f028b456f", out List<MeatovItem> hideoutRoubles))
            {
                while (countLeft > 0 && hideoutRoubles.Count > 0)
                {
                    MeatovItem item = hideoutRoubles[hideoutRoubles.Count - 1];
                    if (item.stack > countLeft)
                    {
                        item.stack -= countLeft;
                        countLeft = 0;
                        break;
                    }
                    else
                    {
                        countLeft -= item.stack;
                        if (!item.DetachChildren())
                        {
                            item.Destroy();
                        }
                    }
                }
            }

            // Find parts to heal and effects to remove
            Dictionary<Effect.EffectType, List<int>> effectsToRemove = new Dictionary<Effect.EffectType, List<int>>();
            List<int> partsToHeal = new List<int>();
            foreach (KeyValuePair<int, Dictionary<Effect.EffectType, GameObject>> outer in treatmentObjectsByEffectTypeByParent)
            {
                foreach(KeyValuePair<Effect.EffectType, GameObject> inner in outer.Value)
                {
                    bool selected = false;
                    if(treatmentSelected.TryGetValue(inner.Value, out selected) && selected)
                    {
                        GameObject parentTreatment = treatmentParentsByObject[inner.Value];
                        if(treatmentSelected.TryGetValue(parentTreatment, out selected) && selected)
                        {
                            if(inner.Key == Effect.EffectType.HealthRate)
                            {
                                partsToHeal.Add(outer.Key);
                            }
                            else
                            {
                                if(effectsToRemove.TryGetValue(inner.Key, out List<int> parts))
                                {
                                    parts.Add(outer.Key);
                                }
                                else
                                {
                                    effectsToRemove.Add(inner.Key, new List<int>() { outer.Key });
                                }
                            }
                        }
                    }
                }
            }

            // Heal parts, Remove effects
            for(int i=0; i < partsToHeal.Count; ++i)
            {
                Mod.SetHealth(partsToHeal[i], Mod.GetCurrentMaxHealth(partsToHeal[i]));
            }
            foreach(KeyValuePair<Effect.EffectType, List<int>> entry in effectsToRemove)
            {
                if(Effect.effectsByType.TryGetValue(entry.Key, out List<Effect> effects))
                {
                    for (int i = effects.Count - 1; i >= 0; --i)
                    {
                        if (entry.Value.Contains(effects[i].partIndex))
                        {
                            Effect.RemoveEffect(effects[i], null);
                        }
                    }
                }
            }

            UpdateTotalTreatmentCost();
        }

        public void SetEffecTreatments()
        {
            // Only care about bleeding and fracture effects
            if (Effect.effectsByType.TryGetValue(Effect.EffectType.LightBleeding, out List<Effect> lightBleedEffectList))
            {
                for (int i = 0; i < lightBleedEffectList.Count; ++i)
                {
                    if (lightBleedEffectList[i].partIndex != -1)
                    {
                        treatmentLightBleedIcons[lightBleedEffectList[i].partIndex].SetActive(true);

                        GameObject newTreatmentObject = Instantiate(treatmentPartSubEntry, treatmentListParent);
                        int cost = (int)Mod.globalDB["config"]["Health"]["LightBleeding"]["RemovePrice"];
                        treatmentCosts.Add(newTreatmentObject, cost);
                        treatmentSelected.Add(newTreatmentObject, true);
                        newTreatmentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentObject); });
                        newTreatmentObject.transform.GetChild(1).GetComponent<Text>().text = "Light bleeding";
                        newTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();

                        GameObject newTreatmentParentObject = null;
                        if (treatmentParentsByIndex.TryGetValue(lightBleedEffectList[i].partIndex, out newTreatmentParentObject)) // Parent exists
                        {
                            newTreatmentObject.transform.SetSiblingIndex(newTreatmentParentObject.transform.GetSiblingIndex() + 1);
                            treatmentObjectsByParent[newTreatmentParentObject].Add(newTreatmentObject, cost);
                            treatmentObjectsByEffectTypeByParent[lightBleedEffectList[i].partIndex].Add(Effect.EffectType.LightBleeding, newTreatmentObject);

                            int totalCost = 0;
                            foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[newTreatmentParentObject])
                            {
                                totalCost += objectEntry.Value;
                            }
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                        }
                        else // Parent does not exist
                        {
                            newTreatmentParentObject = Instantiate(treatmentPartEntry, treatmentListParent);
                            newTreatmentObject.transform.SetAsLastSibling();
                            treatmentSelected.Add(newTreatmentParentObject, true);
                            treatmentObjectsByParent.Add(newTreatmentParentObject, new Dictionary<GameObject, int>() { { newTreatmentObject, cost } });
                            treatmentObjectsByEffectTypeByParent.Add(lightBleedEffectList[i].partIndex, new Dictionary<Effect.EffectType, GameObject>() { { Effect.EffectType.HealthRate, newTreatmentObject } });
                            treatmentParentsByIndex.Add(lightBleedEffectList[i].partIndex, newTreatmentParentObject);
                            newTreatmentParentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentParentObject); });
                            newTreatmentParentObject.transform.GetChild(1).GetComponent<Text>().text = Mod.GetBodyPartName(lightBleedEffectList[i].partIndex);
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();
                        }
                        treatmentParentsByObject.Add(newTreatmentObject, newTreatmentParentObject);
                    }
                }
            }
            if (Effect.effectsByType.TryGetValue(Effect.EffectType.HeavyBleeding, out List<Effect> heavyBleedEffectList))
            {
                for (int i = 0; i < heavyBleedEffectList.Count; ++i)
                {
                    if (heavyBleedEffectList[i].partIndex != -1)
                    {
                        treatmentHeavyBleedIcons[heavyBleedEffectList[i].partIndex].SetActive(true);

                        GameObject newTreatmentObject = Instantiate(treatmentPartSubEntry, treatmentListParent);
                        int cost = (int)Mod.globalDB["config"]["Health"]["HeavyBleeding"]["RemovePrice"];
                        treatmentCosts.Add(newTreatmentObject, cost);
                        treatmentSelected.Add(newTreatmentObject, true);
                        newTreatmentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentObject); });
                        newTreatmentObject.transform.GetChild(1).GetComponent<Text>().text = "Heavy bleeding";
                        newTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();

                        GameObject newTreatmentParentObject = null;
                        if (treatmentParentsByIndex.TryGetValue(heavyBleedEffectList[i].partIndex, out newTreatmentParentObject)) // Parent exists
                        {
                            newTreatmentObject.transform.SetSiblingIndex(newTreatmentParentObject.transform.GetSiblingIndex() + 1);
                            treatmentObjectsByParent[newTreatmentParentObject].Add(newTreatmentObject, cost);
                            treatmentObjectsByEffectTypeByParent[heavyBleedEffectList[i].partIndex].Add(Effect.EffectType.HeavyBleeding, newTreatmentObject);

                            int totalCost = 0;
                            foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[newTreatmentParentObject])
                            {
                                totalCost += objectEntry.Value;
                            }
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                        }
                        else // Parent does not exist
                        {
                            newTreatmentParentObject = Instantiate(treatmentPartEntry, treatmentListParent);
                            newTreatmentObject.transform.SetAsLastSibling();
                            treatmentSelected.Add(newTreatmentParentObject, true);
                            treatmentObjectsByParent.Add(newTreatmentParentObject, new Dictionary<GameObject, int>() { { newTreatmentObject, cost } });
                            treatmentObjectsByEffectTypeByParent.Add(heavyBleedEffectList[i].partIndex, new Dictionary<Effect.EffectType, GameObject>() { { Effect.EffectType.HeavyBleeding, newTreatmentObject } });
                            treatmentParentsByIndex.Add(heavyBleedEffectList[i].partIndex, newTreatmentParentObject);
                            newTreatmentParentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentParentObject); });
                            newTreatmentParentObject.transform.GetChild(1).GetComponent<Text>().text = Mod.GetBodyPartName(heavyBleedEffectList[i].partIndex);
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();
                        }
                        treatmentParentsByObject.Add(newTreatmentObject, newTreatmentParentObject);
                    }
                }
            }
            if (Effect.effectsByType.TryGetValue(Effect.EffectType.HeavyBleeding, out List<Effect> fractureEffectList))
            {
                for (int i = 0; i < fractureEffectList.Count; ++i) 
                {
                    if (fractureEffectList[i].partIndex != -1)
                    {
                        treatmentFractureIcons[fractureEffectList[i].partIndex].SetActive(true);

                        GameObject newTreatmentObject = Instantiate(treatmentPartSubEntry, treatmentListParent);
                        int cost = (int)Mod.globalDB["config"]["Health"]["Fracture"]["RemovePrice"];
                        treatmentCosts.Add(newTreatmentObject, cost);
                        treatmentSelected.Add(newTreatmentObject, true);
                        newTreatmentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentObject); });
                        newTreatmentObject.transform.GetChild(1).GetComponent<Text>().text = "Fracture";
                        newTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();

                        GameObject newTreatmentParentObject = null;
                        if (treatmentParentsByIndex.TryGetValue(fractureEffectList[i].partIndex, out newTreatmentParentObject)) // Parent exists
                        {
                            newTreatmentObject.transform.SetSiblingIndex(newTreatmentParentObject.transform.GetSiblingIndex() + 1);
                            treatmentObjectsByParent[newTreatmentParentObject].Add(newTreatmentObject, cost);
                            treatmentObjectsByEffectTypeByParent[fractureEffectList[i].partIndex].Add(Effect.EffectType.Fracture, newTreatmentObject);

                            int totalCost = 0;
                            foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[newTreatmentParentObject])
                            {
                                totalCost += objectEntry.Value;
                            }
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                        }
                        else // Parent does not exist
                        {
                            newTreatmentParentObject = Instantiate(treatmentPartEntry, treatmentListParent);
                            newTreatmentObject.transform.SetAsLastSibling();
                            treatmentSelected.Add(newTreatmentParentObject, true);
                            treatmentObjectsByParent.Add(newTreatmentParentObject, new Dictionary<GameObject, int>() { { newTreatmentObject, cost } });
                            treatmentObjectsByEffectTypeByParent.Add(fractureEffectList[i].partIndex, new Dictionary<Effect.EffectType, GameObject>() { { Effect.EffectType.Fracture, newTreatmentObject } });
                            treatmentParentsByIndex.Add(fractureEffectList[i].partIndex, newTreatmentParentObject);
                            newTreatmentParentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentParentObject); });
                            newTreatmentParentObject.transform.GetChild(1).GetComponent<Text>().text = Mod.GetBodyPartName(fractureEffectList[i].partIndex);
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();
                        }
                        treatmentParentsByObject.Add(newTreatmentObject, newTreatmentParentObject);
                    }
                }
            }

            UpdateTotalTreatmentCost();
        }

        public void OnPartEffectRemoved(Effect effect)
        {
            if(effect.partIndex == -1 || (effect.effectType != Effect.EffectType.LightBleeding 
                                          && effect.effectType != Effect.EffectType.HeavyBleeding
                                          && effect.effectType != Effect.EffectType.Fracture))
            {
                return;
            }

            if(effect.effectType == Effect.EffectType.LightBleeding)
            {
                treatmentLightBleedIcons[effect.partIndex].SetActive(false);
            }
            else if(effect.effectType == Effect.EffectType.HeavyBleeding)
            {
                treatmentHeavyBleedIcons[effect.partIndex].SetActive(false);
            }
            else if(effect.effectType == Effect.EffectType.Fracture)
            {
                treatmentFractureIcons[effect.partIndex].SetActive(false);
            }

            // Make sure part has no treatment of that effect
            if (treatmentObjectsByEffectTypeByParent.TryGetValue(effect.partIndex, out Dictionary<Effect.EffectType, GameObject> treatmentObjectsByEffectType)
                && treatmentObjectsByEffectType.TryGetValue(effect.effectType, out GameObject treatmentObject))
            {
                // Find parent
                if (treatmentParentsByObject.TryGetValue(treatmentObject, out GameObject parentTreatmentObject))
                {
                    // Remove from parent
                    treatmentParentsByObject.Remove(treatmentObject);
                    treatmentObjectsByParent[parentTreatmentObject].Remove(treatmentObject);

                    // If parent has more treatments, only remove the one for this effect, and recalculate total part cost
                    // Otherwise, remove parent
                    if (treatmentObjectsByParent[parentTreatmentObject].Count > 0)
                    {
                        treatmentObjectsByEffectType.Remove(effect.effectType);

                        int totalCost = 0;
                        foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[parentTreatmentObject])
                        {
                            totalCost += objectEntry.Value;
                        }
                        parentTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                    }
                    else
                    {
                        treatmentObjectsByParent.Remove(parentTreatmentObject);
                        treatmentObjectsByEffectTypeByParent.Remove(effect.partIndex);
                        treatmentParentsByIndex.Remove(effect.partIndex);
                        Destroy(parentTreatmentObject);
                    }
                }
                Destroy(treatmentObject);

                UpdateTotalTreatmentCost();
            }
            treatmentHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void OnPartHealthChanged(int index)
        {
            if (Mod.GetCurrentMaxHealthArray() == null || Mod.GetHealthArray() == null)
            {
                return;
            }

            if (index == -1)
            {
                for (int i = 0; i < Mod.GetHealthCount(); ++i)
                {
                    OnPartHealthChanged(i);
                }
            }
            else
            {
                treatmentPartImages[index].color = Color.Lerp(Color.red, Color.white, Mod.GetHealth(index) / Mod.GetCurrentMaxHealth(index));
                treatmentPartHealths[index].text = ((int)Mod.GetHealth(index)).ToString() + "/" + ((int)Mod.GetCurrentMaxHealth(index));

                float total = 0;
                float totalMax = 0;
                for (int i = 0; i < Mod.GetHealthCount(); ++i)
                {
                    total += Mod.GetHealth(i);
                    totalMax += Mod.GetCurrentMaxHealth(i);
                }
                treatmentTotalHeatlh.text = ((int)total).ToString() + "/" + ((int)totalMax);

                if (Mod.GetHealth(index) == Mod.GetCurrentMaxHealth(index))
                {
                    // Make sure part has no health treatment
                    if(treatmentObjectsByEffectTypeByParent.TryGetValue(index, out Dictionary<Effect.EffectType, GameObject> treatmentObjectsByEffectType)
                        && treatmentObjectsByEffectType.TryGetValue(Effect.EffectType.HealthRate, out GameObject treatmentObject))
                    {
                        // Find parent
                        if(treatmentParentsByObject.TryGetValue(treatmentObject, out GameObject parentTreatmentObject))
                        {
                            // Remove from parent
                            treatmentParentsByObject.Remove(treatmentObject);
                            treatmentObjectsByParent[parentTreatmentObject].Remove(treatmentObject);

                            // If parent has more treatments, only remove the health one, and recalculate total part cost
                            // Otherwise, remove parent
                            if (treatmentObjectsByParent[parentTreatmentObject].Count > 0)
                            {
                                treatmentObjectsByEffectType.Remove(Effect.EffectType.HealthRate);

                                int totalCost = 0;
                                foreach(KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[parentTreatmentObject])
                                {
                                    totalCost += objectEntry.Value;
                                }
                                parentTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                            }
                            else
                            {
                                treatmentObjectsByParent.Remove(parentTreatmentObject);
                                treatmentObjectsByEffectTypeByParent.Remove(index);
                                treatmentParentsByIndex.Remove(index);
                                Destroy(parentTreatmentObject);
                            }
                        }
                        Destroy(treatmentObject);
                    }
                }
                else
                {
                    // Make sure part has health treatment
                    if (treatmentObjectsByEffectTypeByParent.TryGetValue(index, out Dictionary<Effect.EffectType, GameObject> treatmentObjectsByEffectType)
                        && treatmentObjectsByEffectType.TryGetValue(Effect.EffectType.HealthRate, out GameObject treatmentObject))
                    {
                        // Treatment already exists under this parent, recalculate cost
                        int cost = (int)(Mod.GetCurrentMaxHealth(index) - Mod.GetHealth(index)) * (int)Mod.globalDB["config"]["Health"]["HealPrice"]["HealthPointPrice"];
                        treatmentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();
                        treatmentCosts[treatmentObject] = cost;

                        // Then recalculate parent cost
                        if (treatmentParentsByObject.TryGetValue(treatmentObject, out GameObject parentTreatmentObject))
                        {
                            treatmentObjectsByParent[parentTreatmentObject][treatmentObject] = cost;

                            int totalCost = 0;
                            foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[parentTreatmentObject])
                            {
                                totalCost += objectEntry.Value;
                            }
                            parentTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                        }
                    }
                    else // Dont yet have health treatment under this parent or parent does not yet exist
                    {
                        GameObject newTreatmentObject = Instantiate(treatmentPartSubEntry, treatmentListParent);
                        int cost = (int)(Mod.GetCurrentMaxHealth(index) - Mod.GetHealth(index)) * (int)Mod.globalDB["config"]["Health"]["HealPrice"]["HealthPointPrice"];
                        treatmentCosts.Add(newTreatmentObject, cost);
                        treatmentSelected.Add(newTreatmentObject, true);
                        newTreatmentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentObject); });
                        newTreatmentObject.transform.GetChild(1).GetComponent<Text>().text = "Health";
                        newTreatmentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();

                        GameObject newTreatmentParentObject = null;
                        if (treatmentObjectsByEffectType == null) // Parent does not exist
                        {
                            newTreatmentParentObject = Instantiate(treatmentPartEntry, treatmentListParent);
                            newTreatmentObject.transform.SetAsLastSibling();
                            treatmentSelected.Add(newTreatmentParentObject, true);
                            treatmentObjectsByParent.Add(newTreatmentParentObject, new Dictionary<GameObject, int>() { { newTreatmentObject, cost } });
                            treatmentObjectsByEffectTypeByParent.Add(index, new Dictionary<Effect.EffectType, GameObject>() { { Effect.EffectType.HealthRate, newTreatmentObject } });
                            treatmentParentsByIndex.Add(index, newTreatmentParentObject);
                            newTreatmentParentObject.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnTreatmentCheckToggle(newTreatmentParentObject); });
                            newTreatmentParentObject.transform.GetChild(1).GetComponent<Text>().text = Mod.GetBodyPartName(index);
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = cost.ToString();
                        }
                        else // Parent exists
                        {
                            newTreatmentParentObject = treatmentParentsByIndex[index];
                            newTreatmentObject.transform.SetSiblingIndex(newTreatmentParentObject.transform.GetSiblingIndex() + 1);
                            treatmentObjectsByParent[newTreatmentParentObject].Add(newTreatmentObject, cost);
                            treatmentObjectsByEffectType.Add(Effect.EffectType.HealthRate, newTreatmentObject);

                            int totalCost = 0;
                            foreach (KeyValuePair<GameObject, int> objectEntry in treatmentObjectsByParent[newTreatmentParentObject])
                            {
                                totalCost += objectEntry.Value;
                            }
                            newTreatmentParentObject.transform.GetChild(3).GetComponent<Text>().text = totalCost.ToString();
                        }
                        treatmentParentsByObject.Add(newTreatmentObject, newTreatmentParentObject);
                    }
                }

                UpdateTotalTreatmentCost();
            }
            treatmentHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void UpdateTotalTreatmentCost()
        {
            int totalCost = 0;
            foreach(KeyValuePair<GameObject, Dictionary<GameObject, int>> entry in treatmentObjectsByParent)
            {
                if (treatmentSelected[entry.Key])
                {
                    foreach(KeyValuePair<GameObject, int> subEntry in entry.Value)
                    {
                        if (treatmentSelected[subEntry.Key])
                        {
                            totalCost += subEntry.Value;
                        }
                    }
                }
            }

            treatmentTotalCost.text = "Selected Total: " + totalCost.ToString();
            totalTreatmentCost = totalCost;

            treatmentApplyButton.SetActive(Mod.GetItemCountInInventories("5449016a4bdc2d6f028b456f") >= totalCost);
        }

        public void OnTreatmentCheckToggle(GameObject treatmentObject)
        {
            GameObject checkTextObject = treatmentObject.transform.GetChild(0).GetChild(0).gameObject;
            checkTextObject.gameObject.SetActive(!checkTextObject.activeSelf);
            treatmentSelected[treatmentObject] = checkTextObject.activeSelf;

            UpdateTotalTreatmentCost();
        }

        public void OnPlayerLevelChanged()
        {
            raidReportRankIcon.sprite = StatusUI.instance.levelSprites[Mod.level / 5];
            raidReportLevel.text = Mod.level.ToString();
        }

        public void OnPlayerExperienceChanged()
        {
            raidReportExperience.text = Mod.experience.ToString() + "/" + (Mod.level >= Mod.XPPerLevel.Length ? "INFINITY" : Mod.XPPerLevel[Mod.level].ToString());
            raidReportBarFill.sizeDelta = new Vector2(Mod.level >= Mod.XPPerLevel.Length ? 0 : Mod.experience / (float)Mod.XPPerLevel[Mod.level] * 450f, 12.8f);
        }

        public void UpdateLoadButtonList()
        {
            // Call a fetch because new saves could have been made
            Mod.FetchAvailableSaveFiles();

            // Set buttons activated depending on presence of save files
            if (Mod.availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    loadButtons[i].gameObject.SetActive(Mod.availableSaveFiles.Contains(i));
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
            int scaledTime = (int)((clampedTime * Mod.meatovTimeMultiplier) % 86400);
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
            Mod.LoadHideout(slotIndex);
        }

        public void OnRaidClicked()
        {
            clickAudio.Play();
            if (H3MP.Networking.Client.isFullyConnected)
            {
                pages[0].SetActive(false);
                instancesPage.SetActive(true);

                instancesListPage = 0;
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
                    Mod.loadingToMeatovScene = true;
                    Mod.unloadHideout = true;
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

            waitForDeployPage.SetActive(false);
            countdownPage.SetActive(false);

            cancelRaidLoad = true;

            GameManager.SetInstance(0);
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

            Mod.charChoicePMC = true;
            newInstancePMCCheckText.text = "X";
            newInstanceScavCheckText.text = "";
            Mod.timeChoiceIs0 = true;
            timeChoice0CheckText.text = "X";
            timeChoice1CheckText.text = "";
            PopulateNewInstanceMapList();
            Mod.PMCSpawnTogether = true;
            newInstanceSpawnTogetherCheckText.text = "X";
            Mod.mapChoiceName = null;
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

        public void OnMapListNextClicked()
        {
            clickAudio.Play();
            ++mapListPage;

            PopulateMapList();

            if (mapListPage == Mod.availableRaidMaps.Count / 6)
            {
                mapListNextButton.SetActive(false);
            }
            mapListPreviousButton.SetActive(true);
        }

        public void OnMapListPreviousClicked()
        {
            clickAudio.Play();
            --instancesListPage;

            PopulateMapList();

            if (mapListPage == 0)
            {
                mapListPreviousButton.SetActive(false);
            }
            mapListNextButton.SetActive(true);
        }

        public void OnPlayerItemInventoryChanged(int difference)
        {
            if (instancesPage.activeSelf)
            {
                PopulateInstancesList();
            }
            if (pages[5].activeSelf)
            {
                PopulateMapList();
            }
            if (newInstance1Page.activeSelf)
            {
                PopulateNewInstanceMapList();
            }
            if (waitingInstancePage.activeSelf)
            {
                UpdateStartButton();
            }
        }

        public void PopulateInstancesList()
        {
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

            instancesNextButton.SetActive(instancesListPage < maxPage);
            instancesPreviousButton.SetActive(instancesListPage > 0);

            int i = -1;
            foreach(KeyValuePair<int, RaidInstance> instanceDataEntry in Networking.raidInstances)
            {
                if (instancesParent.childCount == 7)
                {
                    break;
                }

                ++i;
                if (i < instancesListPage * 6)
                {
                    continue;
                }

                // Can only join an instance with a map we have installed
                bool unfulfilled = false;
                if (!Mod.availableRaidMaps.ContainsKey(instanceDataEntry.Value.map))
                {
                    unfulfilled = true;
                }

                // Can only join an instance with a map we have requirements for
                if (!unfulfilled && Mod.raidMapEntryRequirements.TryGetValue(instanceDataEntry.Value.map, out Dictionary<string, int> itemRequirements))
                {
                    foreach(KeyValuePair<string, int> item in itemRequirements)
                    {
                        int currentCount = 0;
                        if(!Mod.playerInventory.TryGetValue(item.Key, out currentCount) || currentCount < item.Value)
                        {
                            unfulfilled = true;
                            break;
                        }
                    }
                }

                // Can only join instance if we can choose PMC (Not ongoing) or scav (our scav timer is over and scav return node has been cleared)
                if(!unfulfilled && !instanceDataEntry.Value.waiting && (scavTimer > 0 || scavReturnNode.childCount > 0))
                {
                    unfulfilled = true;
                }

                GameObject newInstanceListEntry = Instantiate(instanceEntryPrefab, instancesParent);
                newInstanceListEntry.SetActive(true);

                int index = instanceDataEntry.Key;
                newInstanceListEntry.GetComponentInChildren<Text>().text = "Instance " + instanceDataEntry.Value.ID;
                if (unfulfilled)
                {
                    newInstanceListEntry.GetComponentInChildren<Text>().color = Color.red;
                }
                else
                {
                    newInstanceListEntry.GetComponentInChildren<Button>().onClick.AddListener(() => { OnInstanceClicked(index); });
                }
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
                Mod.charChoicePMC = true;
                joinInstancePMCCheckText.text = "X";
                joinInstanceScavCheckText.text = "";
            }
            else
            {
                Mod.charChoicePMC = false;
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
                Mod.charChoicePMC = true;

                joinInstancePMCCheckText.text = "X";
                joinInstanceScavCheckText.text = "";
            }
            else
            {
                Mod.charChoicePMC = false;

                joinInstancePMCCheckText.text = "";
                joinInstanceScavCheckText.text = "X";
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
            if(scavTimer > 0 || scavReturnNode.childCount > 0)
            {
                Mod.charChoicePMC = true;

                joinInstancePMCCheckText.text = "X";
                joinInstanceScavCheckText.text = "";
            }
            else
            {
                clickAudio.Play();
                Mod.charChoicePMC = false;

                joinInstancePMCCheckText.text = "";
                joinInstanceScavCheckText.text = "X";
            }
        }

        public void OnNewInstanceCharPMCClicked()
        {
            clickAudio.Play();

            Mod.charChoicePMC = true;

            newInstancePMCCheckText.text = "X";
            newInstanceScavCheckText.text = "";
        }

        public void OnNewInstanceCharScavClicked()
        {
            if (scavTimer > 0 || scavReturnNode.childCount > 0)
            {
                Mod.charChoicePMC = true;

                newInstancePMCCheckText.text = "X";
                newInstanceScavCheckText.text = "";
            }
            else
            {
                clickAudio.Play();

                Mod.charChoicePMC = false;

                newInstancePMCCheckText.text = "";
                newInstanceScavCheckText.text = "X";
            }
        }

        public void OnNewInstanceTime0Clicked()
        {
            clickAudio.Play();

            Mod.timeChoiceIs0 = true;

            timeChoice0CheckText.text = "X";
            timeChoice1CheckText.text = "";

            PopulateNewInstanceMapList();
        }

        public void OnNewInstanceTime1Clicked()
        {
            clickAudio.Play();

            Mod.timeChoiceIs0 = false;

            timeChoice0CheckText.text = "";
            timeChoice1CheckText.text = "X";

            PopulateNewInstanceMapList();
        }

        public void OnNewInstanceSpawnTogetherClicked()
        {
            clickAudio.Play();

            Mod.PMCSpawnTogether = !Mod.PMCSpawnTogether;

            newInstanceSpawnTogetherCheckText.text = Mod.PMCSpawnTogether ? "X" : "";
        }

        public void PopulateNewInstanceMapList()
        {
            while (newInstanceMapListParent.childCount > 1)
            {
                Transform child = newInstanceMapListParent.GetChild(1);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int maxPage = (Mod.availableRaidMaps.Count - 1) / 6;
            if (newInstanceMapListPage > maxPage)
            {
                newInstanceMapListPage = maxPage;
            }

            newInstanceNextButton.SetActive(newInstanceMapListPage < maxPage);
            newInstancePreviousButton.SetActive(newInstanceMapListPage > 0);

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

                bool unfulfilled = false;
                bool day = false;
                if (Mod.availableRaidMapDay.TryGetValue(raidMap.Key, out day))
                {
                    // Day is from 21600 to 64800
                    // Night is from 0 to 21600 and 64800 to 86400
                    float actualTime = Mod.timeChoiceIs0 ? time : (time + 43200) % 86400;
                    if (day)
                    {
                        unfulfilled = (actualTime >= 0 && actualTime < 21600) || (actualTime >= 64800 && actualTime <= 86400);
                    }
                    else
                    {
                        unfulfilled = actualTime >= 21600 && actualTime < 64800;
                    }
                }
                if (Mod.raidMapEntryRequirements.TryGetValue(raidMap.Key, out Dictionary<string, int> itemRequirements))
                {
                    foreach (KeyValuePair<string, int> item in itemRequirements)
                    {
                        int currentCount = 0;
                        if (!Mod.playerInventory.TryGetValue(item.Key, out currentCount) || currentCount < item.Value)
                        {
                            unfulfilled = true;
                            break;
                        }
                    }
                }

                GameObject newInstanceMapListEntry = Instantiate(newInstanceMapListEntryPrefab, newInstanceMapListParent);
                newInstanceMapListEntry.SetActive(true);

                string mapName = raidMap.Key;
                newInstanceMapListEntry.GetComponentInChildren<Text>().text = mapName;
                if (unfulfilled)
                {
                    newInstanceMapListEntry.GetComponentInChildren<Text>().color = Color.red;
                }
                else
                {
                    newInstanceMapListEntry.GetComponentInChildren<Button>().onClick.AddListener(() => { OnNewInstanceMapClicked(mapName); });
                }

                if (Mod.mapChoiceName != null && Mod.mapChoiceName.Equals(mapName))
                {
                    newInstanceMapListEntry.GetComponentInChildren<Image>().color = Color.white;
                    newInstanceMapListEntry.GetComponentInChildren<Text>().color = Color.black;
                }
            }
        }

        public void PopulateMapList()
        {
            while (mapListParent.childCount > 1)
            {
                Transform child = mapListParent.GetChild(1);
                child.SetParent(null);
                Destroy(child.gameObject);
            }

            int maxPage = (Mod.availableRaidMaps.Count - 1) / 6;
            if (mapListPage > maxPage)
            {
                mapListPage = maxPage;
            }

            mapListNextButton.SetActive(mapListPage < maxPage);
            mapListPreviousButton.SetActive(mapListPage > 0);

            int i = -1;
            foreach (KeyValuePair<string, string> raidMap in Mod.availableRaidMaps)
            {
                ++i;
                if (i < mapListPage * 6)
                {
                    continue;
                }

                if (mapListParent.childCount == 7)
                {
                    break;
                }

                bool unfulfilled = false;
                bool day = false;
                if(Mod.availableRaidMapDay.TryGetValue(raidMap.Key, out day))
                {

                    float actualTime = Mod.timeChoiceIs0 ? time : (time + 43200) % 86400;
                    // Day is from 21600 to 64800
                    // Night is from 0 to 21600 and 64800 to 86400
                    if (day)
                    {
                        unfulfilled = (actualTime >= 0 && actualTime < 21600) || (actualTime >= 64800 && actualTime <= 86400);
                    }
                    else
                    {
                        unfulfilled = actualTime >= 21600 && actualTime < 64800;
                    }
                }
                if (!unfulfilled && Mod.raidMapEntryRequirements.TryGetValue(raidMap.Key, out Dictionary<string, int> itemRequirements))
                {
                    foreach (KeyValuePair<string, int> item in itemRequirements)
                    {
                        int currentCount = 0;
                        if (!Mod.playerInventory.TryGetValue(item.Key, out currentCount) || currentCount < item.Value)
                        {
                            unfulfilled = true;
                            break;
                        }
                    }
                }

                GameObject newMapListEntry = Instantiate(mapListEntryPrefab, mapListParent);
                newMapListEntry.SetActive(true);

                string mapName = raidMap.Key;
                newMapListEntry.GetComponentInChildren<Text>().text = mapName;
                if (unfulfilled)
                {
                    newMapListEntry.GetComponentInChildren<Text>().color = Color.red;
                }
                else
                {
                    newMapListEntry.GetComponentInChildren<Button>().onClick.AddListener(() => { OnMapClicked(mapName); });
                }

                if (Mod.mapChoiceName != null && Mod.mapChoiceName.Equals(mapName))
                {
                    newMapListEntry.GetComponentInChildren<Image>().color = Color.white;
                    newMapListEntry.GetComponentInChildren<Text>().color = Color.black;
                }
            }
        }

        public void OnNewInstanceMapClicked(string name)
        {
            Mod.mapChoiceName = name;

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
            RaidInstance raidInstance = Networking.AddInstance(Mod.mapChoiceName, Mod.timeChoiceIs0, Mod.PMCSpawnTogether);
        }

        // Note that this is the only method we will ever use to join en EFM raid instance
        // So we don't need to sub to H3MP's instance joined event to handle raid instances
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

                waitingInstanceCharText.text = "Character: " + (Mod.charChoicePMC ? "PMC" : "Scav");
                waitingInstanceMapText.text = "Map: " + Mod.mapChoiceName;
                waitingInstanceSpawnTogetherText.text = "PMCs spawn together: " + Mod.PMCSpawnTogether;
                UpdateStartButton();

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

        public void UpdateStartButton()
        {
            if(waitingInstancePage.activeSelf || waitingInstancePlayerListPage.activeSelf)
            {
                bool allFulfilled = true;
                if (Mod.raidMapEntryRequirements.TryGetValue(Networking.currentInstance.map, out Dictionary<string, int> itemRequirements))
                {
                    foreach (KeyValuePair<string, int> item in itemRequirements)
                    {
                        int currentCount = 0;
                        if (!Mod.playerInventory.TryGetValue(item.Key, out currentCount) || currentCount < item.Value)
                        {
                            allFulfilled = false;
                            break;
                        }
                    }
                }
                waitingInstanceStartButton.SetActive(Networking.currentInstance.players[0] == GameManager.ID && allFulfilled);
            }
        }

        public static void OnInstanceLeft(int instance, int destination)
        {
            if(Networking.raidInstances.TryGetValue(instance, out RaidInstance raidInstance))
            {
                raidInstance.players.Remove(H3MP.GameManager.ID);

                if (raidInstance.players.Count == 0)
                {
                    Networking.raidInstances.Remove(H3MP.GameManager.instance);

                    if(HideoutController.instance != null)
                    {
                        HideoutController.instance.PopulateInstancesList();
                    }
                }

                Networking.currentInstance = null;
                Networking.spawnRequested = false;
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

            waitingInstancePlayerListNextButton.SetActive(waitingPlayerListPage < maxPage);
            waitingInstancePlayerListPreviousButton.SetActive(waitingPlayerListPage > 0);

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

            UpdateStartButton();
        }

        public void OnWaitingInstancePlayerListPreviousClicked()
        {
            clickAudio.Play();

            --waitingPlayerListPage;
            PopulatePlayerList();

            if (waitingPlayerListPage == 0)
            {
                waitingInstancePlayerListPreviousButton.SetActive(false);
            }
            waitingInstancePlayerListNextButton.SetActive(true);
        }

        public void OnWaitingInstancePlayerListNextClicked()
        {
            clickAudio.Play();

            ++waitingPlayerListPage;
            PopulatePlayerList();

            if (waitingPlayerListPage == Networking.currentInstance.players.Count / 6)
            {
                waitingInstancePlayerListNextButton.SetActive(false);
            }
            waitingInstancePlayerListPreviousButton.SetActive(true);
        }

        public void OnWaitingInstancePlayerListBackClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(true);
            waitingInstancePlayerListPage.SetActive(false);

            UpdateStartButton();
        }

        public void OnWaitingInstanceStartClicked()
        {
            clickAudio.Play();

            waitingInstancePage.SetActive(false);
            waitingInstancePlayerListPage.SetActive(false);
            countdownPage.SetActive(true);
            countdownDeploy = true;
            deployTimer = deployTime;

            // Send waiting packet to everyone else if we are the instance host
            if (Networking.currentInstance.players[0] == GameManager.ID)
            {
                using (Packet packet = new Packet(Networking.setInstanceWaitingPacketID))
                {
                    packet.Write(Networking.currentInstance.ID);
                    packet.Write(false);

                    if (ThreadManager.host)
                    {
                        ServerSend.SendTCPDataToAll(packet, true);
                    }
                    else
                    {
                        ClientSend.SendTCPData(packet, true);
                    }
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

            Mod.charChoicePMC = charIndex == 0;

            // Update chosen char text
            confirmChosenCharacter.text = Mod.charChoicePMC ? "PMC" : "Scav";
            loadingChosenCharacter.text = confirmChosenCharacter.text;

            if (scavReturnNode.childCount > 0)
            {
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

            Mod.mapChoiceName = mapName;

            // Update chosen map text
            confirmChosenMap.text = mapName;
            loadingChosenMap.text = mapName;

            SetPage(6);
        }

        public void OnTimeClicked(int timeIndex)
        {
            clickAudio.Play();

            Mod.timeChoiceIs0 = timeIndex == 0;
            SetPage(5);
            mapListPage = 0;
            PopulateMapList();
        }

        public void OnConfirmRaidClicked()
        {
            clickAudio.Play();

            countdownDeploy = true;
            deployTimer = deployTime;

            SetPage(7);
        }

        public void OnCancelRaidLoadClicked()
        {
            clickAudio.Play();

            cancelRaidLoad = true;
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
            ConfigureQuickbeltPatch.overrideIndex = true;
            ConfigureQuickbeltPatch.actualConfigIndex = -2;
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

            Mod.LogInfo("\t0");
            // Write skills
            for (int i = 0; i < 64; ++i)
            {
                loadedData["skills"][i] = Mod.skills[i].progress;
            }
            Mod.LogInfo("\t0");

            // Save player items
            // Hands
            // Don't want to save as hand item if item has quickbeltslot, which would mean it is harnessed
            JObject serializedItem = null;
            if(Mod.leftHand.heldItem != null && Mod.leftHand.heldItem.physObj.QuickbeltSlot == null)
            {
                serializedItem = Mod.leftHand.heldItem.Serialize();
            }
            loadedData["leftHand"] = serializedItem;
            Mod.LogInfo("\t0");
            serializedItem = null;
            if (Mod.rightHand.heldItem != null && Mod.rightHand.heldItem.physObj.QuickbeltSlot == null)
            {
                serializedItem = Mod.rightHand.heldItem.Serialize();
            }
            loadedData["rightHand"] = serializedItem;
            Mod.LogInfo("\t0");
            // Equipment
            for (int i=0; i< StatusUI.instance.equipmentSlots.Length; ++i)
            {
                FVRPhysicalObject physObj = StatusUI.instance.equipmentSlots[i].CurObject;
                if(physObj == null)
                {
                    serializedItem = null;
                }
                else
                {
                    MeatovItem meatovItem = physObj.GetComponent<MeatovItem>();
                    ++SetQuickBeltSlotPatch.fullSkip;
                    physObj.SetQuickBeltSlot(null);
                    // Pass false to serialize to prevent saving equipped rig slot items
                    // since they will instea be saved as QBS items
                    serializedItem = meatovItem == null ? null : meatovItem.Serialize(false);
                    physObj.SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i]);
                    --SetQuickBeltSlotPatch.fullSkip;
                }
                loadedData["equipment"+i] = serializedItem;
            }
            Mod.LogInfo("\t0");
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
                    ++SetQuickBeltSlotPatch.fullSkip;
                    physObj.SetQuickBeltSlot(null);
                    serializedItem = meatovItem == null ? null : meatovItem.Serialize();
                    physObj.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[i]);
                    --SetQuickBeltSlotPatch.fullSkip;
                }
                loadedData["slot" + i] = serializedItem;
            }
            Mod.LogInfo("\t0");

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
            Mod.LogInfo("\t0");
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
            loadedData["hideout"]["tradeVolumeItems"] = tradeVolumeItems;
            Mod.LogInfo("\t0");
            // Area items
            JArray areas = new JArray();
            for(int i=0; i < areaController.areas.Length; ++i)
            {
                JObject area = new JObject();
                if(areaController.areas[i] != null)
                {
                    // Levels
                    JArray areaLevels = new JArray();
                    for(int j=0; j < areaController.areas[i].levels.Length; ++j)
                    {
                        JObject level = new JObject();
                        JArray volumes = new JArray();
                        JArray slots = new JArray();
                        // Volumes
                        for (int k=0; k< areaController.areas[i].levels[j].areaVolumes.Length; ++k)
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
                        // Slots
                        for (int k = 0; k < areaController.areas[i].levels[j].areaSlots.Length; ++k)
                        {
                            JObject serialized = null;
                            FVRPhysicalObject physObj = areaController.areas[i].levels[j].areaSlots[k].CurObject;
                            if (physObj != null)
                            {
                                MeatovItem meatovItem = physObj.GetComponent<MeatovItem>();

                                ++SetQuickBeltSlotPatch.fullSkip;
                                physObj.SetQuickBeltSlot(null);
                                serializedItem = meatovItem == null ? null : meatovItem.Serialize();
                                physObj.SetQuickBeltSlot(areaController.areas[i].levels[j].areaSlots[k]);
                                --SetQuickBeltSlotPatch.fullSkip;
                            }
                            slots.Add(serialized);
                        }
                        level["slots"] = slots;
                        areaLevels.Add(level);
                    }
                    area["levels"] = areaLevels;
                }
                areas.Add(area);
            }
            loadedData["hideout"]["areas"] = areas;
            Mod.LogInfo("\t0");
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
            Mod.LogInfo("\t0");

            // Save traders
            JArray traders = new JArray();
            for (int i=0; i < Mod.traders.Length; ++i)
            {
                JObject trader = new JObject();
                Mod.traders[i].Save(trader);
                traders.Add(trader);
            }
            loadedData["hideout"]["traders"] = traders;
            Mod.LogInfo("\t0");

            // Save areas
            for (int i = 0; i < areaController.areas.Length; ++i)
            {
                if (areaController.areas[i] != null)
                {
                    // Note area array and object have already been added when saving area items above
                    areaController.areas[i].Save(areas[i]);
                }
            }
            Mod.LogInfo("\t0");

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
            loadedData["triggeredExplorationTriggers"] = JArray.FromObject(Mod.triggeredExperienceTriggers);

            Mod.LogInfo("\t0");
            JArray wishlist = new JArray();
            for(int i=0; i < Mod.wishList.Count; ++i)
            {
                wishlist.Add(Mod.wishList[i].tarkovID);
            }
            loadedData["whishlist"] = wishlist;
            Mod.LogInfo("\t0");

            SaveDataToFile();
            Mod.LogInfo("Saved hideout");
            UpdateLoadButtonList();
        }

        public void FinishRaid(RaidManager.RaidStatus state)
        {
            if (Mod.charChoicePMC)
            {
                // Increment raid counters
                ++Mod.totalRaidCount;
                switch (state)
                {
                    case RaidManager.RaidStatus.RunThrough:
                        ++Mod.runthroughRaidCount;
                        ++Mod.survivedRaidCount;
                        break;
                    case RaidManager.RaidStatus.Success:
                        ++Mod.survivedRaidCount;
                        break;
                    case RaidManager.RaidStatus.MIA:
                        ++Mod.MIARaidCount;
                        ++Mod.failedRaidCount;
                        break;
                    case RaidManager.RaidStatus.KIA:
                        ++Mod.KIARaidCount;
                        ++Mod.failedRaidCount;
                        break;
                    default:
                        break;
                }

                if (Mod.raidStatus == RaidManager.RaidStatus.KIA || Mod.raidStatus == RaidManager.RaidStatus.MIA)
                {
                    // Remove all effects
                    Effect.RemoveAllEffects();

                    for (int i = 0; i < Mod.GetHealthCount(); ++i)
                    {
                        Mod.SetHealth(i, 0.3f * Mod.GetCurrentMaxHealth(i));
                    }

                    Mod.hydration = 0.3f * Mod.currentMaxHydration;
                    Mod.energy = 0.3f * Mod.currentMaxEnergy;

                    Mod.AddExperience(100 + (int)(Mod.raidTime / 60 * 10)); // Fail exp + bonus of 10 exp / min
                }
                else if (Mod.raidStatus == RaidManager.RaidStatus.Success)
                {
                    Mod.AddExperience(600 + (int)(Mod.raidTime / 60 * 10)); // Survive exp + bonus of 10 exp / min
                }
            }
            else
            {
                scavTimer = defaultScavTime;
            }
        }

        private void SaveDataToFile()
        {
            File.WriteAllText(Mod.path + "/Saves/" + (Mod.saveSlotIndex == 5 ? "AutoSave" : "Slot" + Mod.saveSlotIndex) + ".sav", loadedData.ToString());
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

        public void OnDestroy()
        {
            // Unsub from UI events
            foreach (KeyValuePair<string, string> raidMapEntry in Mod.availableRaidMaps)
            {
                if (Mod.raidMapEntryRequirements.TryGetValue(raidMapEntry.Key, out Dictionary<string, int> requirements))
                {
                    foreach (KeyValuePair<string, int> requirement in requirements)
                    {
                        if (Mod.defaultItemData.TryGetValue(requirement.Key, out MeatovItemData itemData))
                        {
                            itemData.OnPlayerItemInventoryChanged -= OnPlayerItemInventoryChanged;
                        }
                    }
                }
            }
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
