﻿using System;
using System.IO;
using UnityEngine;
using System.Reflection;
using BepInEx;
using FistVR;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using H3MP;

namespace EFM
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Mod : BaseUnityPlugin
    {
        // BepinEx
        public const string pluginGuid = "VIP.TommySoucy.EscapeFromMeatov";
        public const string pluginName = "EscapeFromMeatov";
        public const string pluginVersion = "1.0.0";

        // Constants
        public static readonly float headshotDamageMultiplier = 1.2f;
        public static readonly float handDamageResist = 0.5f;
        //public static readonly float[] sizeVolumes = { 1, 2, 5, 30, 0, 50}; // 0: Small, 1: Medium, 2: Large, 3: Massive, 4: None, 5: CantCarryBig
        public static readonly float volumePrecisionMultiplier = 1000; // Volumes are stored as floats but are processed as ints to avoid precision errors, this is how precise we should be when converting, this goes down to 10th of a mililiter
        public static int neededForQuestPriority = 2;
        public static int neededForAreaPriority = 3;
        public static int neededForWishlistPriority = 4;
        public static int neededForBarterPriority = 0;
        public static int neededForProductionPriority = 1;
        public static bool checkmarkFutureAreas = false;
        public static bool checkmarkFutureProductions = false;
        public static bool checkmarkFutureQuests = false;

        // Live data
        public static Mod modInstance;
        public static int currentLocationIndex = -1; // This will be used by custom item wrapper and vanilla item descr. in their Start(). Shoud only ever be 1(base) or 2(raid). If want to spawn an item in player inventory, will have to set it manually
        public static AssetBundle defaultBundle;
        public static AssetBundle mainMenuBundle;
        public static AssetBundle hideoutBundle;
        public static AssetBundleCreateRequest hideoutBundleRequest;
        public static AssetBundle itemIconsBundle;
        public static AssetBundleCreateRequest itemIconsBundleRequest;
        public static AssetBundle[] itemsBundles;
        public static AssetBundleCreateRequest[] itemsBundlesRequests;
        public static AssetBundle playerBundle;
        public static AssetBundleCreateRequest playerBundleRequest;
        public static AssetBundleCreateRequest currentRaidBundleRequest;
        public static List<GameObject> securedMainSceneComponents;
        public static List<GameObject> securedObjects;
        public static FVRInteractiveObject securedLeftHandInteractable;
        public static FVRInteractiveObject securedRightHandInteractable;
        public static int saveSlotIndex = -1;
        public static int currentQuickBeltConfiguration = -1;
        public static int firstCustomConfigIndex = -1;
        public static Hand rightHand;
        public static Hand leftHand;
        public static List<List<RigSlot>> looseRigSlots;
        public static int chosenCharIndex;
        public static int chosenMapIndex;
        public static int chosenTimeIndex;
        public static string chosenMapName;
        public static HideoutController.FinishRaidState raidState;
        public static bool justFinishedRaid;
        public static Dictionary<string, int> killList;
        public static int lootingExp = 0;
        public static int healingExp = 0;
        public static int explorationExp = 0;
        public static float raidTime = 0;
        public static bool inMeatovScene;
        public static int pocketsConfigIndex;
        public static Dictionary<int, int> quickbeltConfigurationIndices;
        public static float distanceTravelledSprinting;
        public static float distanceTravelledWalking;
        public static MeatovItem[] itemsInPocketSlots;
        public static FVRQuickBeltSlot[] pocketSlots;
        public static ShoulderStorage leftShoulderSlot;
        public static ShoulderStorage rightShoulderSlot;
        public static GameObject leftShoulderObject;
        public static GameObject rightShoulderObject;
        public static Raid_Manager currentRaidManager;
        public static Dictionary<string, int>[] requiredPerArea;
        public static List<string> wishList;
        public static Dictionary<FireArmMagazineType, Dictionary<string, int>> magazinesByType;
        public static Dictionary<FireArmClipType, Dictionary<string, int>> clipsByType;
        public static Dictionary<FireArmRoundType, Dictionary<string, int>> roundsByType; // TODO: should use the ID of the round, not the name
        public static Dictionary<MeatovItem.ItemRarity, List<string>> itemsByRarity;
        public static Dictionary<string, List<MeatovItemData>> itemsByParents;
        public static List<string> usedRoundIDs;
        public static Dictionary<string, int> ammoBoxByAmmoID;
        public static Dictionary<string, int> requiredForQuest;
        public static Dictionary<string, List<DescriptionManager>> activeDescriptionsByItemID;
        public static List<AreaBonus> activeBonuses;
        public static Trader[] traders;
        public static Dictionary<int, List<Task>> tasksByTraderIndex;
        public static CategoryTreeNode itemCategories;
        public static Dictionary<string, List<string>> itemAncestors;
        public static Dictionary<string, int> lowestBuyValueByItem;
        public static bool amountChoiceUIUp;
        public static MeatovItem splittingItem;
        public static bool preventLoadMagUpdateLists; // Flag to prevent load mag patches to update lists before they are initialized
        public static int attachmentCheckNeeded;
        public static List<FVRInteractiveObject> physObjColResetList;
        public static int physObjColResetNeeded;
        public static List<List<bool>> triggeredExplorationTriggers;
        public static GameObject[] scavRaidReturnItems; // Hands, Equipment, Right shoulder, pockets
        public static GameObject instantiatedItem;
        public static Dictionary<FVRInteractiveObject, MeatovItem> meatovItemByInteractive = new Dictionary<FVRInteractiveObject, MeatovItem>();

        // Player
        private static int _level = 1;
        public static int level 
        {
            get { return _level; }
            set { _level = value; OnPlayerLevelChangedInvoke(); }
        }
        private static int _experience = 0;
        public static int experience
        {
            get { return _experience; }
            set { _experience = value; OnPlayerExperienceChangedInvoke(); }
        }
        public static bool dead;
        // Parts arrays: 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
        public static float[] defaultMaxHealth;
        public static float[] currentMaxHealth;
        public static float[] health; 
        public static float[] currentHealthRates;
        public static float[] currentNonLethalHealthRates;
        public static float energy;
        public static float defaultMaxEnergy;
        public static float currentMaxEnergy;
        public static float hydration;
        public static float defaultMaxHydration;
        public static float currentMaxHydration;
        public static readonly float raidEnergyRate = -3.2f; // TODO: Move this to RaidController and set it on LoadDB globals>config>Health>Effects>Existence
        public static readonly float raidHydrationRate = -2.6f; // TODO: Move this to RaidController and set it on LoadDB globals>config>Health>Effects>Existence
        public static float currentEnergyRate;
        public static float currentHydrationRate;
        public static Dictionary<string, int> playerInventory;
        public static Dictionary<string, List<MeatovItem>> playerInventoryItems;
        //public static List<InsuredSet> insuredItems;
        public static float staminaTimer = 0;
        public static float stamina = 100;
        public static float maxStamina = 100;
        public static float currentMaxStamina = 100;
        public static Skill[] skills;
        public static float sprintStaminaDrain = 4.1f;
        public static float overweightStaminaDrain = 4f;
        public static float staminaRestoration = 4.4f;
        public static float jumpStaminaDrain = 16;
        public static float currentStaminaEffect = 0;
        public static float baseWeightLimit = 55000;
        public static float effectWeightLimitBonus = 0;
        public static float skillWeightLimitBonus = 0;
        public static float currentDamageModifier = 1;
        public static int stomachBloodLossCount = 0; // TODO: If this is 0, in hideout we will regen health otherwise not, in raid we will multiply energy and hydration rate by 5
        public static float temperatureOffset = 0;
        public static bool fatigue = false;
        public static Effect dehydrationEffect;
        public static Effect fatigueEffect;
        public static Effect overweightFatigueEffect;
        public static GameObject consumeUI;
        public static Text consumeUIText;
        public static GameObject stackSplitUI;
        public static Text stackSplitUIText;
        public static Transform stackSplitUICursor;
        public static GameObject extractionUI;
        public static Text extractionUIText;
        public static GameObject extractionLimitUI;
        public static Text extractionLimitUIText;
        public static GameObject leftDescriptionUI;
        public static DescriptionManager leftDescriptionManager;
        public static GameObject rightDescriptionUI;
        public static DescriptionManager rightDescriptionManager;
        public static GameObject staminaBarUI;
        public static int totalRaidCount;
        public static int runThroughRaidCount;
        public static int survivedRaidCount;
        public static int MIARaidCount;
        public static int KIARaidCount;
        public static int failedRaidCount;
        private static int _weight = 0;
        public static int weight
        {
            set
            {
                _weight = value;
                StatusUI.instance.UpdateWeight();
            }
            get
            {
                return _weight;
            }
        }
        private static int _currentWeightLimit = 55000;
        public static int currentWeightLimit
        {
            set
            {
                _currentWeightLimit = value;
                StatusUI.instance.UpdateWeight();
            }
            get
            {
                return _currentWeightLimit;
            }
        }

        // Assets
        public static string path;
        public static JObject config;
        public static bool assetLoaded;
        public static GameObject scenePrefab_Menu;
        public static GameObject mainMenuPointable;
        public static Material quickSlotHoverMaterial;
        public static Material quickSlotConstantMaterial;
        public static Dictionary<string, string> itemNames;
        public static Dictionary<string, string> itemDescriptions;
        public static Dictionary<string, int> itemWeights;
        public static Dictionary<string, int> itemVolumes;
        public static GameObject doorLeftPrefab;
        public static GameObject doorRightPrefab;
        public static GameObject doorDoublePrefab;
        public static bool initDoors = true;
        public static GameObject playerStatusUIPrefab;
        public static GameObject extractionUIPrefab;
        public static GameObject extractionLimitUIPrefab;
        public static GameObject extractionCardPrefab;
        public static GameObject consumeUIPrefab;
        public static GameObject stackSplitUIPrefab;
        public static GameObject itemDescriptionUIPrefab;
        public static GameObject neededForPrefab;
        public static GameObject ammoContainsPrefab;
        public static GameObject staminaBarPrefab;
        public static GameObject devItemSpawnerPrefab;
        public static Sprite cartridgeIcon;
        public static Sprite[] playerLevelIcons;
        public static AudioClip[] barbedWireClips;
        public static Sprite[] skillIcons;

        // DB
        public static JArray areasDB;
        public static JArray productionsDB;
        public static JArray scavCaseProductionsDB;
        public static JObject hideoutsettingsDB;
        public static JObject localeDB;
        public static JObject itemDB;
        public static Dictionary<string, ItemMapEntry> itemMap;
        public static JObject[] traderBaseDB;
        public static JObject[] traderAssortDB;
        public static JObject[] traderQuestAssortDB;
        public static JObject globalDB;
        public static JObject questDB;
        public static JArray XPPerLevel;
        public static JObject[] locationsLootDB;
        public static JObject[] locationsBaseDB;
        public static JArray lootContainerDB;
        public static JObject dynamicLootTable;
        public static JObject staticLootTable;
        public static MeatovItemData[] customItemData;
        public static Dictionary<string, MeatovItemData> vanillaItemData;
        public static Dictionary<string, JObject> lootContainersByName;
        public static Dictionary<string, AudioClip[]> itemSounds;

        // Debug
        public static bool waitingForDebugCode;
        public static string debugCode;
        public static bool spawnItems = true;
        public static bool spawnAI = true;
        public static bool spawnOnlyFirstAI = false;
        public static bool spawnedFirstAI = false;
        public static bool forceSpawnAI = false;

        // Events
        public delegate void OnPlayerLevelChangedDelegate();
        public static event OnPlayerLevelChangedDelegate OnPlayerLevelChanged;
        public delegate void OnPlayerExperienceChangedDelegate();
        public static event OnPlayerExperienceChangedDelegate OnPlayerExperienceChanged;
        public delegate void OnKillDelegate(KillData killData);
        public static event OnKillDelegate OnKill;
        public delegate void OnShotDelegate(ShotData shotData);
        public static event OnShotDelegate OnShot;
        public delegate void OnRaidExitDelegate(ConditionCounter.ExitStatus status, string exitID);
        public static event OnRaidExitDelegate OnRaidExit;
        public delegate void OnPlaceVisitedDelegate(string placeID);
        public static event OnPlaceVisitedDelegate OnPlaceVisited;
        public delegate void OnFlareLaunchedDelegate(string placeID);
        public static event OnFlareLaunchedDelegate OnFlareLaunched;

        public void Start()
        {
            modInstance = this;

            path = Path.GetDirectoryName(Assembly.GetAssembly(typeof(Mod)).Location);
            Mod.LogInfo("Meatov path found: " + path, false);

            H3MP.Mod.OnInstantiationTrack += H3MP_OnInstantiationTrack;

            LoadConfig();

            LoadDB();

            LoadDefaultAssets();

            PatchController.DoPatching();

            Init();
        }

        public void Update()
        {
#if DEBUG
            if (waitingForDebugCode)
            {
                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    debugCode += "0";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad1))
                {
                    debugCode += "1";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad2))
                {
                    debugCode += "2";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad3))
                {
                    debugCode += "3";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad4))
                {
                    debugCode += "4";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad5))
                {
                    debugCode += "5";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad6))
                {
                    debugCode += "6";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad7))
                {
                    debugCode += "7";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad8))
                {
                    debugCode += "8";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
                else if(Input.GetKeyDown(KeyCode.Keypad9))
                {
                    debugCode += "9";
                    Mod.LogInfo("DebugCode: " + debugCode);
                }
            }
            if(Input.GetKeyDown(KeyCode.F10))
            {
                waitingForDebugCode = !waitingForDebugCode;
                if (waitingForDebugCode)
                {
                    debugCode = string.Empty;
                }
                else
                {
                    Mod.LogInfo("Activating DebugCode: " + debugCode);
                    if (debugCode != string.Empty && int.TryParse(debugCode, out int code))
                    {
                        switch (code)
                        {
                            case 0: // Toggle raid AI
                                spawnAI = !spawnAI;
                                Mod.LogInfo("\tDebug: Toggle raid AI, enabled: "+ spawnAI);
                                break;
                            case 1: // Force next AI spawn
                                Mod.LogInfo("\tDebug: Force next AI spawn");
                                //currentRaidManager.ForceSpawnNextAI();
                                break;
                            case 2: // Toggle raid only first AI
                                spawnOnlyFirstAI = !spawnOnlyFirstAI;
                                Mod.LogInfo("\tDebug: Toggle raid only first AI, enabled: "+ spawnOnlyFirstAI);
                                break;
                            case 3: // Toggle raid item spawn
                                spawnItems = !spawnItems;
                                Mod.LogInfo("\tDebug: Toggle raid item spawn, enabled: "+ spawnItems);
                                break;
                            case 4: // Load to meatov menu
                                Mod.LogInfo("\tDebug: Load to meatov menu");
                                SteamVR_LoadLevel.Begin("MeatovMainMenu", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 5: // Start new meatov game
                                Mod.LogInfo("\tDebug: Start new meatov game");
                                FindObjectOfType<MenuController>().OnNewGameClicked();
                                break;
                            case 6: // Start factory raid
                                Mod.LogInfo("\tDebug: Start factory raid");
                                Mod.chosenMapIndex = -1;
                                GameObject.Find("Hideout").GetComponent<HideoutController>().OnConfirmRaidClicked();
                                break;
                            case 7: // Load autosave
                                Mod.LogInfo("\tDebug: Load autosave");
                                GameObject.Find("Hideout").GetComponent<MenuController>().OnLoadSlotClicked(5);
                                break;
                            case 8: // Load latest save
                                Mod.LogInfo("\tDebug: Load latest save");
                                GameObject.Find("Hideout").GetComponent<MenuController>().OnContinueClicked();
                                break;
                            case 9: // Save on slot 0
                                Mod.LogInfo("\tDebug: Save on slot 0");
                                if (GameObject.Find("Hideout") != null)
                                {
                                    GameObject.Find("Hideout").GetComponent<HideoutController>().OnSaveSlotClicked(0);
                                }
                                break;
                            case 10: // Load save 0
                                Mod.LogInfo("\tDebug: Load save 0");
                                UIController.LoadHideout(0);
                                break;
                            case 11: // Kill player
                                Mod.LogInfo("\tDebug: Kill player");
                                //currentRaidManager.KillPlayer();
                                break;
                            case 12: // Write PatchHashes
                                Mod.LogInfo("\tDebug: Write PatchHashes");
                                string dest = path + "/PatchHashes" + DateTimeOffset.Now.ToString().Replace("/", ".").Replace(":", ".") + ".json";
                                File.Copy(path + "/PatchHashes.json", dest);
                                Mod.LogWarning("Writing new hashes to file!");
                                File.WriteAllText(path + "/PatchHashes.json", JObject.FromObject(PatchController.hashes).ToString());
                                break;
                            case 13: // Dump layers
                                Mod.LogInfo("\tDebug: Dump layers");
                                DumpLayers();
                                break;
                            case 14: // Survive raid
                                Mod.LogInfo("\tDebug: Survive raid");
                                Mod.justFinishedRaid = true;
                                Mod.raidState = HideoutController.FinishRaidState.Survived;

                                // Disable extraction list and timer
                                StatusUI.instance.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
                                StatusUI.instance.extractionTimer.color = Color.black;
                                Mod.extractionLimitUI.SetActive(false);
                                StatusUI.instance.Close();
                                Mod.extractionUI.SetActive(false);

                                //Raid_Manager.currentManager.ResetHealthEffectCounterConditions();

                                UIController.LoadHideout(5); // Load autosave, which is right before the start of raid

                                //Raid_Manager.currentManager.extracted = true;
                                break;
                            case 15: // GC Collect
                                Mod.LogInfo("\tDebug: GC Collect");
                                if(HideoutController.instance != null)
                                {
                                    HideoutController.instance.GCManager.gc_collect();
                                }
                                break;
                            case 16: // GC get total memory
                                Mod.LogInfo("\tDebug: GC get total memory");
                                if (HideoutController.instance != null)
                                {
                                    Mod.LogInfo("\tMemory: "+ HideoutController.instance.GCManager.gc_get_total_memory(false));
                                }
                                break;
                            case 17: // Dump item IDs
                                Mod.LogInfo("\tDebug: Dumping item IDs");
                                foreach (KeyValuePair<string, ItemSpawnerID> entry in IM.Instance.SpawnerIDDic)
                                {
                                    Mod.LogInfo(entry.Key);
                                }
                                break;
                        }
                    }
                }
            }
#endif
        }

        public static void OnPlayerLevelChangedInvoke()
        {
            if(OnPlayerLevelChanged != null)
            {
                OnPlayerLevelChanged();
            }
        }

        public static void OnPlayerExperienceChangedInvoke()
        {
            if(OnPlayerExperienceChanged != null)
            {
                OnPlayerExperienceChanged();
            }
        }

        public static void OnRaidExitInvoke(ConditionCounter.ExitStatus status, string exitID)
        {
            if(OnRaidExit != null)
            {
                OnRaidExit(status, exitID);
            }
        }

        public static void OnKillInvoke(KillData killData)
        {
            if(OnKill != null)
            {
                OnKill(killData);
            }
        }

        public static void OnShotInvoke(ShotData shotData)
        {
            if(OnShot != null)
            {
                OnShot(shotData);
            }
        }

        public static void OnPlaceVisitedInvoke(string placeID)
        {
            if(OnPlaceVisited != null)
            {
                OnPlaceVisited(placeID);
            }
        }

        public static void OnFlareLaunchedInvoke(string placeID)
        {
            if(OnFlareLaunched != null)
            {
                OnFlareLaunched(placeID);
            }
        }

        public void DumpLayers()
        {
            Dictionary<int, int> _masksByLayer;
            _masksByLayer = new Dictionary<int, int>();
            for (int i = 0; i < 32; i++)
            {
                int mask = 0;
                for (int j = 0; j < 32; j++)
                {
                    if (!Physics.GetIgnoreLayerCollision(i, j))
                    {
                        mask |= 1 << j;
                    }
                }
                _masksByLayer.Add(i, mask);
            }

            for (int i = 0; i < 32; ++i)
            {
                Logger.LogInfo("Mask for layer " + i + ": " + _masksByLayer[i]);
            }
        }

        private void LoadConfig()
        {
            LogInfo("Loading config...", false);
            config = JObject.Parse(File.ReadAllText(path+"/Config.json"));
            LogInfo("Config loaded", false);
        }

        private void AddCategories(List<string> parents)
        {
            // If necessary, init categ tree root with item ID
            if (itemCategories == null)
            {
                itemCategories = new CategoryTreeNode(null, "54009119af1c881c07000029", "Item");
            }
            CategoryTreeNode currentParent = itemCategories;
            for (int i = parents.Count - 2; i >= 0; i--)
            {
                string ID = parents[i];
                CategoryTreeNode foundChild = currentParent.FindChild(ID);
                if (foundChild != null)
                {
                    currentParent = foundChild;
                }
                else
                {
                    string name = localeDB["templates"][ID]["Name"].ToString();
                    CategoryTreeNode newNode = new CategoryTreeNode(currentParent, ID, name);
                    currentParent.children.Add(newNode);
                    currentParent = newNode;
                }
            }
        }

        private void LoadDB()
        {
            globalDB = JObject.Parse(File.ReadAllText(path + "/database/globals.json"));

            JToken globalPartsHealth = globalDB["config"]["Health"]["ProfileHealthSettings"]["BodyPartsSettings"];
            defaultMaxHealth = new float[7];
            defaultMaxHealth[0] = (float)globalPartsHealth["Head"]["Default"];
            defaultMaxHealth[1] = (float)globalPartsHealth["Chest"]["Default"];
            defaultMaxHealth[2] = (float)globalPartsHealth["Stomach"]["Default"];
            defaultMaxHealth[3] = (float)globalPartsHealth["LeftArm"]["Default"];
            defaultMaxHealth[4] = (float)globalPartsHealth["RightArm"]["Default"];
            defaultMaxHealth[5] = (float)globalPartsHealth["LeftLeg"]["Default"];
            defaultMaxHealth[6] = (float)globalPartsHealth["RightLeg"]["Default"];

            JToken globalRegens = globalDB["config"]["Health"]["Effects"]["Regeneration"];
            HideoutController.defaultHealthRates = new float[7];
            HideoutController.defaultHealthRates[0] = (float)globalRegens["BodyHealth"]["Head"]["Value"];
            HideoutController.defaultHealthRates[1] = (float)globalRegens["BodyHealth"]["Chest"]["Value"];
            HideoutController.defaultHealthRates[2] = (float)globalRegens["BodyHealth"]["Stomach"]["Value"];
            HideoutController.defaultHealthRates[3] = (float)globalRegens["BodyHealth"]["LeftArm"]["Value"];
            HideoutController.defaultHealthRates[4] = (float)globalRegens["BodyHealth"]["RightArm"]["Value"];
            HideoutController.defaultHealthRates[5] = (float)globalRegens["BodyHealth"]["LeftLeg"]["Value"];
            HideoutController.defaultHealthRates[6] = (float)globalRegens["BodyHealth"]["RightLeg"]["Value"];
            HideoutController.defaultEnergyRate = (float)globalRegens["Energy"];
            HideoutController.defaultHydrationRate = (float)globalRegens["Hydration"];

            JToken globalHealthFactors = globalDB["config"]["Health"]["ProfileHealthSettings"]["HealthFactorsSettings"];
            defaultMaxEnergy = (float)globalHealthFactors["Energy"]["Default"];
            defaultMaxHydration = (float)globalHealthFactors["Hydration"]["Default"];

            skills = new Skill[64];
            for(int i=0; i < skills.Length; ++i)
            {
                skills[i] = new Skill();
                if (i >= 0 && i <= 6)
                {
                    skills[i].skillType = Skill.SkillType.Physical;
                }
                else if (i >= 7 && i <= 11)
                {
                    skills[i].skillType = Skill.SkillType.Mental;
                }
                else if (i >= 12 && i <= 27)
                {
                    skills[i].skillType = Skill.SkillType.Combat;
                }
                else if (i >= 28 && i <= 53)
                {
                    skills[i].skillType = Skill.SkillType.Practical;
                }
                else if (i >= 54 && i <= 63)
                {
                    skills[i].skillType = Skill.SkillType.Special;
                }
            }
            LoadSkillVars();

            areasDB = JArray.Parse(File.ReadAllText(path + "/database/hideout/areas.json"));
            productionsDB = JArray.Parse(File.ReadAllText(path + "/database/hideout/production.json"));
            scavCaseProductionsDB = JArray.Parse(File.ReadAllText(path + "/database/hideout/scavcase.json"));
            hideoutsettingsDB = JObject.Parse(File.ReadAllText(path + "/database/hideout/settings.json"));
            Area.GPUBoostRate = (float)hideoutsettingsDB["gpuBoostRate"];
            localeDB = JObject.Parse(File.ReadAllText(path + "/database/locales/global/en.json"));
            itemDB = JObject.Parse(File.ReadAllText(path + "/database/templates/items.json"));
            ParseDefaultItemData();
            ParseItemMap();

            traders = new Trader[9];
            traderBaseDB = new JObject[9];
            traderAssortDB = new JObject[9];
            for (int i = 0; i < 9; ++i)
            {
                string traderID = Trader.IndexToID(i);
                traderBaseDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/traders/" + traderID + "/base.json"));
                if (File.Exists(Mod.path + "/database/traders/" + traderID + "/assort.json"))
                {
                    traderAssortDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/traders/" + traderID + "/assort.json"));
                }

                traders[i] = new Trader(i, traderID);
            }

            tasksByTraderIndex = new Dictionary<int, List<Task>>();
            questDB = JObject.Parse(File.ReadAllText(Mod.path + "/database/templates/quests.json"));
            Dictionary<string, JToken> allQuests = questDB.ToObject<Dictionary<string, JToken>>();
            foreach (KeyValuePair<string, JToken> questData in allQuests)
            {
                Task newTask = new Task(questData);
                newTask.trader.tasks.Add(newTask);
            }

            //MovementManagerUpdatePatch.damagePerMeter = (float)Mod.globalDB["config"]["Health"]["Falling"]["DamagePerMeter"];
            //MovementManagerUpdatePatch.safeHeight = (float)Mod.globalDB["config"]["Health"]["Falling"]["SafeHeight"];
            //XPPerLevel = (JArray)globalDB["config"]["exp"]["level"]["exp_table"];
            //locationsLootDB = new JObject[12];
            //locationsBaseDB = new JObject[12];
            //string[] locationBaseFiles = Directory.GetFiles(Mod.path + "/database/locations/base");
            //for (int i=0; i < 13; ++i)
            //{
            //    locationsLootDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/locations/" + Mod.LocationIndexToDataName(i) + "/looseLoot.json"));
            //    locationsLootDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/locations/" + Mod.LocationIndexToDataName(i) + "/base.json"));
            //}
            //lootContainerDB = JArray.Parse(File.ReadAllText(Mod.path + "/database/loot/staticContainers.json"));
            ////dynamicLootTable = JObject.Parse(File.ReadAllText(Mod.path + "/database/Locations/DynamicLootTable.json"));
            //staticLootTable = JObject.Parse(File.ReadAllText(Mod.path + "/database/loot/staticLoot.json"));
            ////lootContainersByName = new Dictionary<string, JObject>();
            ////foreach (JToken container in lootContainerDB)
            ////{
            ////    lootContainersByName.Add(container["_name"].ToString(), (JObject)container);
            ////}

        }

        public void LoadSkillVars()
        {
            JToken globalSkillsSettings = globalDB["config"]["SkillsSettings"];
            Skill.skillProgressRate = (float)globalSkillsSettings["SkillProgressRate"];
            Skill.weaponSkillProgressRate = (float)globalSkillsSettings["WeaponSkillProgressRate"];

            // Endurance
            Skill.movementAction = (float)globalSkillsSettings["Endurance"]["MovementAction"];
            Skill.sprintAction = (float)globalSkillsSettings["Endurance"]["SprintAction"];
            Skill.gainPerFatigueStack = (float)globalSkillsSettings["Endurance"]["GainPerFatigueStack"];

            // HideoutManagement
            Skill.skillPointsPerAreaUpgrade = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsPerAreaUpgrade"];
            Skill.skillPointsPerCraft = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsPerCraft"];
            Skill.generatorPointsPerResourceSpent = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["PointsGained"] / (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["ResourceSpent"];
            Skill.AFUPointsPerResourceSpent = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["PointsGained"] / (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["ResourceSpent"];
            Skill.waterCollectorPointsPerResourceSpent = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["PointsGained"] / (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["ResourceSpent"];
            Skill.solarPowerPointsPerResourceSpent = (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["PointsGained"] / (float)globalSkillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["ResourceSpent"];
            Skill.consumptionReductionPerLevel = (float)globalSkillsSettings["HideoutManagement"]["ConsumptionReductionPerLevel"];
            Skill.skillBoostPercent = (float)globalSkillsSettings["HideoutManagement"]["SkillBoostPercent"];

            // Crafting
            Skill.pointsPerHourCrafting = ((float)globalSkillsSettings["Crafting"]["PointsPerCraftingCycle"] + (float)globalSkillsSettings["Crafting"]["PointsPerUniqueCraftCycle"]) / (float)globalSkillsSettings["Crafting"]["CraftingCycleHours"];
            Skill.craftTimeReductionPerLevel = (float)globalSkillsSettings["Crafting"]["CraftTimeReductionPerLevel"];
            Skill.productionTimeReductionPerLevel = (float)globalSkillsSettings["Crafting"]["ProductionTimeReductionPerLevel"];
            Skill.eliteExtraProductions = (float)globalSkillsSettings["Crafting"]["EliteExtraProductions"];

            // Metabolism
            Skill.hydrationRecoveryRate = (float)globalSkillsSettings["Metabolism"]["HydrationRecoveryRate"];
            Skill.energyRecoveryRate = (float)globalSkillsSettings["Metabolism"]["EnergyRecoveryRate"];
            Skill.increasePositiveEffectDurationRate = (float)globalSkillsSettings["Metabolism"]["IncreasePositiveEffectDurationRate"];
            Skill.decreaseNegativeEffectDurationRate = (float)globalSkillsSettings["Metabolism"]["DecreaseNegativeEffectDurationRate"];
            Skill.decreasePoisonDurationRate = (float)globalSkillsSettings["Metabolism"]["DecreasePoisonDurationRate"];

            // Immunity
            Skill.immunityMiscEffects = (float)globalSkillsSettings["Immunity"]["ImmunityMiscEffects"];
            Skill.immunityPoisonBuff = (float)globalSkillsSettings["Immunity"]["ImmunityPoisonBuff"];
            Skill.immunityPainKiller = (float)globalSkillsSettings["Immunity"]["ImmunityPainKiller"];
            Skill.healthNegativeEffect = (float)globalSkillsSettings["Immunity"]["HealthNegativeEffect"];
            Skill.stimulatorNegativeBuff = (float)globalSkillsSettings["Immunity"]["StimulatorNegativeBuff"];

            // Strength
            Skill.sprintActionMin = (float)globalSkillsSettings["Strength"]["SprintActionMin"];
            Skill.sprintActionMax = (float)globalSkillsSettings["Strength"]["SprintActionMax"];
            Skill.movementActionMin = (float)globalSkillsSettings["Strength"]["MovementActionMin"];
            Skill.movementActionMax = (float)globalSkillsSettings["Strength"]["MovementActionMax"];
            Skill.pushUpMin = (float)globalSkillsSettings["Strength"]["PushUpMin"];
            Skill.pushUpMax = (float)globalSkillsSettings["Strength"]["PushUpMax"];
            Skill.fistfightAction = (float)globalSkillsSettings["Strength"]["FistfightAction"];
            Skill.throwAction = (float)globalSkillsSettings["Strength"]["ThrowAction"];

            // Vitality
            Skill.damageTakenAction = (float)globalSkillsSettings["Vitality"]["DamageTakenAction"];
            Skill.vitalityHealthNegativeEffect = (float)globalSkillsSettings["Vitality"]["HealthNegativeEffect"];

            // Health
            Skill.skillProgress = (float)globalSkillsSettings["Health"]["SkillProgress"];

            // StressResistance
            Skill.stressResistanceHealthNegativeEffect = (float)globalSkillsSettings["StressResistance"]["HealthNegativeEffect"];
            Skill.lowHPDuration = (float)globalSkillsSettings["StressResistance"]["LowHPDuration"];

            // Throwing
            Skill.throwingThrowAction = (float)globalSkillsSettings["Throwing"]["ThrowAction"];

            // RecoilControl
            Skill.recoilAction = (float)globalSkillsSettings["RecoilControl"]["RecoilAction"];
            Skill.recoilBonusPerLevel = (float)globalSkillsSettings["RecoilControl"]["RecoilBonusPerLevel"];

            // Pistol
            Skill.pistolWeaponReloadAction = (float)globalSkillsSettings["Pistol"]["WeaponReloadAction"];
            Skill.pistolWeaponShotAction = (float)globalSkillsSettings["Pistol"]["WeaponShotAction"];
            Skill.pistolWeaponChamberAction = (float)globalSkillsSettings["Pistol"]["WeaponChamberAction"];

            // Revolver, uses Pistol values
            Skill.revolverWeaponReloadAction = (float)globalSkillsSettings["Pistol"]["WeaponReloadAction"];
            Skill.revolverWeaponShotAction = (float)globalSkillsSettings["Pistol"]["WeaponShotAction"];
            Skill.revolverWeaponChamberAction = (float)globalSkillsSettings["Pistol"]["WeaponChamberAction"];

            // SMG, uses assault values
            Skill.SMGWeaponReloadAction = (float)globalSkillsSettings["Assault"]["WeaponReloadAction"];
            Skill.SMGWeaponShotAction = (float)globalSkillsSettings["Assault"]["WeaponShotAction"];
            Skill.SMGWeaponChamberAction = (float)globalSkillsSettings["Assault"]["WeaponChamberAction"];

            // Assault
            Skill.assaultWeaponReloadAction = (float)globalSkillsSettings["Assault"]["WeaponReloadAction"];
            Skill.assaultWeaponShotAction = (float)globalSkillsSettings["Assault"]["WeaponShotAction"];
            Skill.assaultWeaponChamberAction = (float)globalSkillsSettings["Assault"]["WeaponChamberAction"];

            // Shotgun
            Skill.shotgunWeaponReloadAction = (float)globalSkillsSettings["Shotgun"]["WeaponReloadAction"];
            Skill.shotgunWeaponShotAction = (float)globalSkillsSettings["Shotgun"]["WeaponShotAction"];
            Skill.shotgunWeaponChamberAction = (float)globalSkillsSettings["Shotgun"]["WeaponChamberAction"];

            // Sniper
            Skill.sniperWeaponReloadAction = (float)globalSkillsSettings["Sniper"]["WeaponReloadAction"];
            Skill.sniperWeaponShotAction = (float)globalSkillsSettings["Sniper"]["WeaponShotAction"];
            Skill.sniperWeaponChamberAction = (float)globalSkillsSettings["Sniper"]["WeaponChamberAction"];

            // HMG, uses DMR values
            Skill.HMGWeaponReloadAction = (float)globalSkillsSettings["DMR"]["WeaponReloadAction"];
            Skill.HMGWeaponShotAction = (float)globalSkillsSettings["DMR"]["WeaponShotAction"];
            Skill.HMGWeaponChamberAction = (float)globalSkillsSettings["DMR"]["WeaponChamberAction"];

            // LMG, uses DMR values
            Skill.LMGWeaponReloadAction = (float)globalSkillsSettings["DMR"]["WeaponReloadAction"];
            Skill.LMGWeaponShotAction = (float)globalSkillsSettings["DMR"]["WeaponShotAction"];
            Skill.LMGWeaponChamberAction = (float)globalSkillsSettings["DMR"]["WeaponChamberAction"];

            // Launcher, uses DMR values
            Skill.launcherWeaponReloadAction = (float)globalSkillsSettings["DMR"]["WeaponReloadAction"];
            Skill.launcherWeaponShotAction = (float)globalSkillsSettings["DMR"]["WeaponShotAction"];
            Skill.launcherWeaponChamberAction = (float)globalSkillsSettings["DMR"]["WeaponChamberAction"];

            // AttachedLauncher, uses DMR values
            Skill.attachedLauncherWeaponReloadAction = (float)globalSkillsSettings["DMR"]["WeaponReloadAction"];
            Skill.attachedLauncherWeaponShotAction = (float)globalSkillsSettings["DMR"]["WeaponShotAction"];
            Skill.attachedLauncherWeaponChamberAction = (float)globalSkillsSettings["DMR"]["WeaponChamberAction"];

            // DMR
            Skill.DMRWeaponReloadAction = (float)globalSkillsSettings["DMR"]["WeaponReloadAction"];
            Skill.DMRWeaponShotAction = (float)globalSkillsSettings["DMR"]["WeaponShotAction"];
            Skill.DMRWeaponChamberAction = (float)globalSkillsSettings["DMR"]["WeaponChamberAction"];

            // CovertMovement
            Skill.covertMovementAction = (float)globalSkillsSettings["CovertMovement"]["MovementAction"];

            // Search
            Skill.searchAction = (float)globalSkillsSettings["Search"]["SearchAction"];
            Skill.findAction = (float)globalSkillsSettings["Search"]["FindAction"];

            // MagDrills
            Skill.raidLoadedAmmoAction = (float)globalSkillsSettings["MagDrills"]["RaidLoadedAmmoAction"];
            Skill.raidUnloadedAmmoAction = (float)globalSkillsSettings["MagDrills"]["RaidUnloadedAmmoAction"];
            Skill.magazineCheckAction = (float)globalSkillsSettings["MagDrills"]["MagazineCheckAction"];

            // Perception
            Skill.onlineAction = (float)globalSkillsSettings["Perception"]["OnlineAction"];
            Skill.uniqueLoot = (float)globalSkillsSettings["Perception"]["UniqueLoot"];

            // Intellect
            Skill.examineAction = (float)globalSkillsSettings["Intellect"]["ExamineAction"];
            Skill.intellectSkillProgress = (float)globalSkillsSettings["Intellect"]["SkillProgress"];

            // Attention
            Skill.examineWithInstruction = (float)globalSkillsSettings["Attention"]["ExamineWithInstruction"];
            Skill.findActionFalse = (float)globalSkillsSettings["Attention"]["FindActionFalse"];
            Skill.findActionTrue = (float)globalSkillsSettings["Attention"]["FindActionTrue"];

            // Charisma
            Skill.skillProgressInt = (float)globalSkillsSettings["Charisma"]["SkillProgressInt"];
            Skill.skillProgressAtn = (float)globalSkillsSettings["Charisma"]["SkillProgressAtn"];
            Skill.skillProgressPer = (float)globalSkillsSettings["Charisma"]["SkillProgressPer"];

            // Memory
            Skill.anySkillUp = (float)globalSkillsSettings["Memory"]["AnySkillUp"];
            Skill.memorySkillProgress = (float)globalSkillsSettings["Memory"]["SkillProgress"];

            // Surgery
            Skill.surgeryAction = (float)globalSkillsSettings["Surgery"]["SurgeryAction"];
            Skill.surgerySkillProgress = (float)globalSkillsSettings["Surgery"]["SkillProgress"];

            // AimDrills
            Skill.weaponShotAction = (float)globalSkillsSettings["AimDrills"]["WeaponShotAction"];
        }

        private void LoadDefaultAssets()
        {
            defaultBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMDefaults.ab");
            mainMenuPointable = defaultBundle.LoadAsset<GameObject>("MainMenuPointable");

            mainMenuBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMMainMenu.ab");
            string[] bundledScenes = mainMenuBundle.GetAllScenePaths();
            Mod.LogInfo("Got " + bundledScenes.Length + " bundled scenes");
            for(int i=0; i < bundledScenes.Length; ++i)
            {
                Mod.LogInfo(i.ToString()+" : " + bundledScenes[i]);
            }
        }

        private void SetVanillaItems()
        {
            // Start by loading custom-vanilla icons
        }

        private static MeatovItem.ItemRarity ItemRarityStringToEnum(string name)
        {
            switch (name)
            {
                case "Common":
                    return MeatovItem.ItemRarity.Common;
                case "Rare":
                    return MeatovItem.ItemRarity.Rare;
                case "Superrare":
                    return MeatovItem.ItemRarity.Superrare;
                case "Not_exist":
                    return MeatovItem.ItemRarity.Not_exist;
                default:
                    return MeatovItem.ItemRarity.Not_exist;
            }
        }

        private void ParseDefaultItemData()
        {
            JObject data = JObject.Parse(File.ReadAllText(path + "/database/DefaultItemData.json"));

            itemsByParents = new Dictionary<string, List<MeatovItemData>>();

            JArray customData = data["customItemData"] as JArray;
            customItemData = new MeatovItemData[customData.Count];
            // Here, we start at 3 because previous are placeholder for test items
            for (int i=3; i < customData.Count; ++i)
            {
                customItemData[i] = new MeatovItemData(customData[i]);
            }

            Dictionary<string,JToken> vanillaData = data["vanillaItemData"].ToObject<Dictionary<string, JToken>>();
            vanillaItemData = new Dictionary<string, MeatovItemData>();
            foreach(KeyValuePair<string,JToken> entry in vanillaData)
            {
                vanillaItemData.Add(entry.Key, new MeatovItemData(entry.Value));
            }
        }

        private void ParseItemMap()
        {
            itemMap = new Dictionary<string, ItemMapEntry>();

            Dictionary<string, JObject> itemMapData = JObject.Parse(File.ReadAllText(Mod.path + "/database/ItemMap.json")).ToObject<Dictionary<string, JObject>>();

            foreach(KeyValuePair<string, JObject> item in itemMapData)
            {
                ItemMapEntry newEntry = new ItemMapEntry();
                newEntry.ID = item.Value["H3ID"].ToString();
                newEntry.moddedID = item.Value["OtherMod"].ToString();

                newEntry.modded = newEntry.moddedID != null && !newEntry.moddedID.Equals("") && IM.OD.ContainsKey(newEntry.moddedID);

                itemMap.Add(item.Key, newEntry);
            }
        }

        public GameObject MakeItemInteractiveSet(Transform root, FVRPhysicalObject itemPhysicalObject)
        {
            // Make the root interactable
            OtherInteractable otherInteractable = root.gameObject.AddComponent<OtherInteractable>();
            otherInteractable.interactiveObject = itemPhysicalObject;

            // Make its children interactable
            foreach (Transform interactive in root)
            {
                MakeItemInteractiveSet(interactive, itemPhysicalObject);
            }

            return root.gameObject;
        }

        public static void AddToAll(FVRInteractiveObject interactiveObject, MeatovItem CIW)
        {
            if (CIW != null && !CIW.inAll)
            {
                typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(interactiveObject, FVRInteractiveObject.All.Count);
                FVRInteractiveObject.All.Add(interactiveObject);

                CIW.inAll = true;
            }
        }

        public static void RemoveFromAll(FVRInteractiveObject interactiveObject, MeatovItem MI)
        {
            if (MI != null && (MI.inAll || interactiveObject == null))
            {
                if (interactiveObject == null)
                {
                    interactiveObject = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
                }
                FieldInfo indexField = typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic);
                int currentIndex = (int)indexField.GetValue(interactiveObject);

                FVRInteractiveObject.All[currentIndex] = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
                indexField.SetValue(FVRInteractiveObject.All[currentIndex], currentIndex);
                FVRInteractiveObject.All.RemoveAt(FVRInteractiveObject.All.Count - 1);

                indexField.SetValue(interactiveObject, -1);

                MI.inAll = false;
            }
            else if(MI == null)
            {
                if (interactiveObject == null)
                {
                    interactiveObject = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
                }
                FieldInfo indexField = typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic);
                int currentIndex = (int)indexField.GetValue(interactiveObject);

                FVRInteractiveObject.All[currentIndex] = FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1];
                indexField.SetValue(FVRInteractiveObject.All[currentIndex], currentIndex);
                FVRInteractiveObject.All.RemoveAt(FVRInteractiveObject.All.Count - 1);

                indexField.SetValue(interactiveObject, -1);
            }
        }

        public static void AddToPlayerInventory(MeatovItem item, bool stackOnly = false, int stackDifference = 0)
        {
            if (stackOnly)
            {
                if (playerInventory.ContainsKey(item.H3ID))
                {
                    playerInventory[item.H3ID] += stackDifference;

                    if(playerInventory[item.H3ID] <= 0)
                    {
                        Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        playerInventory.Remove(item.H3ID);
                        playerInventoryItems.Remove(item.H3ID);
                    }
                }
                else
                {
                    Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n"+Environment.StackTrace);
                }
            }
            else
            {
                if (playerInventory.ContainsKey(item.H3ID))
                {
                    playerInventory[item.H3ID] += item.stack;
                    playerInventoryItems[item.H3ID].Add(item);
                }
                else
                {
                    playerInventory.Add(item.H3ID, item.stack);
                    playerInventoryItems.Add(item.H3ID, new List<MeatovItem> { item });
                }

                if (item.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    FVRFireArmMagazine boxMagazine = item.physObj as FVRFireArmMagazine;
                    foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                    {
                        if (loadedRound == null || loadedRound.LR_ObjectWrapper == null)
                        {
                            break;
                        }
                        string roundName = loadedRound.LR_ObjectWrapper.DisplayName;

                        if (roundsByType.ContainsKey(boxMagazine.RoundType))
                        {
                            if (roundsByType[boxMagazine.RoundType] == null)
                            {
                                roundsByType[boxMagazine.RoundType] = new Dictionary<string, int>();
                                roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                            }
                            else
                            {
                                if (roundsByType[boxMagazine.RoundType].ContainsKey(roundName))
                                {
                                    roundsByType[boxMagazine.RoundType][roundName] += 1;
                                }
                                else
                                {
                                    roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                                }
                            }
                        }
                        else
                        {
                            roundsByType.Add(boxMagazine.RoundType, new Dictionary<string, int>());
                            roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                        }
                    }
                }
                else if (item.physObj is FVRFireArmMagazine)
                {
                    FVRFireArmMagazine asMagazine = item.physObj as FVRFireArmMagazine;
                    if (magazinesByType.ContainsKey(asMagazine.MagazineType))
                    {
                        if (magazinesByType[asMagazine.MagazineType] == null)
                        {
                            magazinesByType[asMagazine.MagazineType] = new Dictionary<string, int>();
                            magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                        }
                        else
                        {
                            if (magazinesByType[asMagazine.MagazineType].ContainsKey(asMagazine.ObjectWrapper.DisplayName))
                            {
                                magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                            }
                        }
                    }
                    else
                    {
                        magazinesByType.Add(asMagazine.MagazineType, new Dictionary<string, int>());
                        magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                    }
                }
                else if (item.physObj is FVRFireArmClip)
                {
                    FVRFireArmClip asClip = item.physObj as FVRFireArmClip;
                    if (clipsByType.ContainsKey(asClip.ClipType))
                    {
                        if (clipsByType[asClip.ClipType] == null)
                        {
                            clipsByType[asClip.ClipType] = new Dictionary<string, int>();
                            clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                        }
                        else
                        {
                            if (clipsByType[asClip.ClipType].ContainsKey(asClip.ObjectWrapper.DisplayName))
                            {
                                clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                            }
                        }
                    }
                    else
                    {
                        clipsByType.Add(asClip.ClipType, new Dictionary<string, int>());
                        clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                    }
                }
                else if (item.physObj is FVRFireArmRound)
                {
                    FVRFireArmRound asRound = item.physObj as FVRFireArmRound;
                    if (roundsByType.ContainsKey(asRound.RoundType))
                    {
                        if (roundsByType[asRound.RoundType] == null)
                        {
                            roundsByType[asRound.RoundType] = new Dictionary<string, int>();
                            roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                        }
                        else
                        {
                            if (roundsByType[asRound.RoundType].ContainsKey(asRound.ObjectWrapper.DisplayName))
                            {
                                roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                            }
                        }
                    }
                    else
                    {
                        roundsByType.Add(asRound.RoundType, new Dictionary<string, int>());
                        roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                    }
                }
            }
        }

        public static void RemoveFromPlayerInventory(MeatovItem item)
        {
            if (playerInventory.ContainsKey(item.H3ID))
            {
                playerInventory[item.H3ID] -= item.stack;
                playerInventoryItems[item.H3ID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove " + item.H3ID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (playerInventory[item.H3ID] == 0)
            {
                playerInventory.Remove(item.H3ID);
                playerInventoryItems.Remove(item.H3ID);
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

                    string roundName = loadedRound.LR_ObjectWrapper.DisplayName;

                    if (roundsByType.ContainsKey(boxMagazine.RoundType))
                    {
                        if (roundsByType[boxMagazine.RoundType].ContainsKey(roundName))
                        {
                            roundsByType[boxMagazine.RoundType][roundName] -= 1;
                            if (roundsByType[boxMagazine.RoundType][roundName] == 0)
                            {
                                roundsByType[boxMagazine.RoundType].Remove(roundName);
                            }
                            if (roundsByType[boxMagazine.RoundType].Count == 0)
                            {
                                roundsByType.Remove(boxMagazine.RoundType);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + item.H3ID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo name was not found in roundsByType:\n" + Environment.StackTrace);
                        }
                    }
                    else
                    {
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo type was not found in roundsByType:\n" + Environment.StackTrace);
                    }
                }
            }
            else if (item.physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asMagazine = item.physObj as FVRFireArmMagazine;
                if (magazinesByType.ContainsKey(asMagazine.MagazineType))
                {
                    if (magazinesByType[asMagazine.MagazineType].ContainsKey(asMagazine.ObjectWrapper.DisplayName))
                    {
                        magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] -= 1;
                        if (magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] == 0)
                        {
                            magazinesByType[asMagazine.MagazineType].Remove(asMagazine.ObjectWrapper.DisplayName);
                        }
                        if (magazinesByType[asMagazine.MagazineType].Count == 0)
                        {
                            magazinesByType.Remove(asMagazine.MagazineType);
                        }
                    }
                    else
                    {
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is mag from player inventory but its name was not found in magazinesByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is mag from player inventory but its type was not found in magazinesByType:\n" + Environment.StackTrace);
                }
            }
            else if (item.physObj is FVRFireArmClip)
            {
                FVRFireArmClip asClip = item.physObj as FVRFireArmClip;
                if (clipsByType.ContainsKey(asClip.ClipType))
                {
                    if (clipsByType[asClip.ClipType].ContainsKey(asClip.ObjectWrapper.DisplayName))
                    {
                        clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] -= 1;
                        if (clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] == 0)
                        {
                            clipsByType[asClip.ClipType].Remove(asClip.ObjectWrapper.DisplayName);
                        }
                        if (clipsByType[asClip.ClipType].Count == 0)
                        {
                            clipsByType.Remove(asClip.ClipType);
                        }
                    }
                    else
                    {
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is clip from player inventory but its name was not found in clipsByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is clip from player inventory but its type was not found in clipsByType:\n" + Environment.StackTrace);
                }
            }
            else if (item.physObj is FVRFireArmRound)
            {
                FVRFireArmRound asRound = item.physObj as FVRFireArmRound;
                if (roundsByType.ContainsKey(asRound.RoundType))
                {
                    if (roundsByType[asRound.RoundType].ContainsKey(asRound.ObjectWrapper.DisplayName))
                    {
                        roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] -= 1;
                        if (roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] == 0)
                        {
                            roundsByType[asRound.RoundType].Remove(asRound.ObjectWrapper.DisplayName);
                        }
                        if (roundsByType[asRound.RoundType].Count == 0)
                        {
                            roundsByType.Remove(asRound.RoundType);
                        }
                    }
                    else
                    {
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is round from player inventory but its name was not found in roundsByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is round from player inventory but its type was not found in roundsByType:\n" + Environment.StackTrace);
                }
            }

            // Check for more items that may be contained inside this one
            // NOTE: This is now handled by MeatovItem.UpdateInventories() and parent tracking system
            //if (customItemWrapper != null)
            //{
            //    if (customItemWrapper.itemType == MeatovItem.ItemType.Backpack || customItemWrapper.itemType == MeatovItem.ItemType.Container || customItemWrapper.itemType == MeatovItem.ItemType.Pouch)
            //    {
            //        foreach (Transform innerItem in customItemWrapper.containerItemRoot)
            //        {
            //            RemoveFromPlayerInventory(innerItem, updateTypeLists);
            //        }
            //    }
            //    else if (customItemWrapper.itemType == MeatovItem.ItemType.Rig || customItemWrapper.itemType == MeatovItem.ItemType.ArmoredRig)
            //    {
            //        foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
            //        {
            //            if (innerItem != null)
            //            {
            //                RemoveFromPlayerInventory(innerItem.transform, updateTypeLists);
            //            }
            //        }
            //    }
            //    else if (physObj is FVRFireArm)
            //    {
            //        FVRFireArm asFireArm = (FVRFireArm)physObj;

            //        // Ammo container
            //        if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
            //        {
            //            RemoveFromPlayerInventory(asFireArm.Magazine.transform, updateTypeLists);
            //        }
            //        else if (asFireArm.UsesClips && asFireArm.Clip != null)
            //        {
            //            RemoveFromPlayerInventory(asFireArm.Clip.transform, updateTypeLists);
            //        }

            //        // Attachments
            //        if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
            //        {
            //            foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
            //            {
            //                RemoveFromPlayerInventory(attachment.transform, updateTypeLists);
            //            }
            //        }
            //    }
            //    else if (physObj is FVRFireArmAttachment)
            //    {
            //        FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)physObj;

            //        if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
            //        {
            //            foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
            //            {
            //                RemoveFromPlayerInventory(attachment.transform, updateTypeLists);
            //            }
            //        }
            //    }
            //}
        }

        public static void AddExperience(int xp, int type = 0 /*0: General (kill, raid result, etc.), 1: Looting, 2: Healing, 3: Exploration*/, string notifMsg = null)
        {
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && Mod.chosenCharIndex == 1)
            {
                return;
            }

            // Add skill and area bonuses
            xp = (int)(xp * (HideoutController.currentExperienceRate + HideoutController.currentExperienceRate * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100)));

            experience += xp;
            int XPForNextLevel = (int)XPPerLevel[level]["exp"]; // XP for current level would be at level - 1
            while (experience >= XPForNextLevel)
            {
                ++level;
                experience -= XPForNextLevel;
                XPForNextLevel = (int)XPPerLevel[level]["exp"];
            }

            if (type == 1)
            {
                Mod.lootingExp += xp;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} looting experience.", xp));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else if (type == 2)
            {
                Mod.healingExp += xp;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} healing experience.", xp));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else if (type == 3)
            {
                Mod.explorationExp += xp;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} exploration experience.", xp));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else
            {
                StatusUI.instance.AddNotification(string.Format("Gained {0} experience.", xp));
            }
        }

        public static void AddSkillExp(float xp, int skillIndex)
        {
            // Globals SkillsSettings
            // Skip if in scav raid
            Skill skill = Mod.skills[skillIndex];
            if (Mod.currentLocationIndex == 1 || Mod.chosenCharIndex == 1 || skill.raidProgress >= 300) // Max 3 levels per raid TODO: should be unique to each skill
            {
                return;
            }

            int intPreProgress = (int)skill.progress;
            float preLevel = (int)(skill.progress / 100);

            float actualAmountToAdd = xp * ((skillIndex >= 12 && skillIndex <= 24) ? Skill.weaponSkillProgressRate : Skill.skillProgressRate);
            actualAmountToAdd += actualAmountToAdd * (HideoutController.currentSkillGroupLevelingBoosts.ContainsKey(skill.skillType) ? HideoutController.currentSkillGroupLevelingBoosts[skill.skillType] : 0);
            actualAmountToAdd *= skill.dimishingReturns ? 0.5f : 1;

            skill.progress += actualAmountToAdd;
            skill.currentProgress += actualAmountToAdd;

            skill.raidProgress += actualAmountToAdd;
            if (skill.raidProgress >= 200) // dimishing returns at 2 levels per raid TODO: should be unique to each skill
            {
                skill.dimishingReturns = true;
                skill.increasing = false;
            }
            else
            {
                skill.increasing = true;
            }

            if (skill.skillType == Skill.SkillType.Practical || skill.skillType == Skill.SkillType.Physical)
            {
                float memoryAmount = Skill.memorySkillProgress / Skill.anySkillUp * actualAmountToAdd;
                Mod.skills[11].progress += memoryAmount;
                Mod.skills[11].currentProgress += memoryAmount;

                StatusUI.instance.UpdateSkillUI(11);
            }

            float postLevel = (int)(skill.progress / 100);

            //if (postLevel != preLevel && Mod.taskSkillConditionsBySkillIndex.ContainsKey(skillIndex))
            //{
            //    foreach (TraderTaskCondition condition in Mod.taskSkillConditionsBySkillIndex[skillIndex])
            //    {
            //        TraderStatus.UpdateConditionFulfillment(condition);
            //    }
            //}

            // Skill specific stuff
            if (skillIndex == 0)
            {
                Mod.maxStamina += (postLevel - preLevel);

                float healthAmount = Skill.skillProgress * actualAmountToAdd;
                Mod.skills[3].progress += healthAmount;
                Mod.skills[3].currentProgress += healthAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[3].raidProgress += actualAmountToAdd;
                    if (Mod.skills[3].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[3].dimishingReturns = true;
                        Mod.skills[3].increasing = false;
                    }
                    else
                    {
                        Mod.skills[3].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(3);
            }
            else if (skillIndex == 1)
            {
                Mod.skillWeightLimitBonus += (postLevel - preLevel);
                Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);

                float healthAmount = Skill.skillProgress * actualAmountToAdd;
                Mod.skills[3].progress += healthAmount;
                Mod.skills[3].currentProgress += healthAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[3].raidProgress += actualAmountToAdd;
                    if (Mod.skills[3].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[3].dimishingReturns = true;
                        Mod.skills[3].increasing = false;
                    }
                    else
                    {
                        Mod.skills[3].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(3);
            }
            else if (skillIndex == 2)
            {
                float healthAmount = Skill.skillProgress * actualAmountToAdd;
                Mod.skills[3].progress += healthAmount;
                Mod.skills[3].currentProgress += healthAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[3].raidProgress += actualAmountToAdd;
                    if (Mod.skills[3].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[3].dimishingReturns = true;
                        Mod.skills[3].increasing = false;
                    }
                    else
                    {
                        Mod.skills[3].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(3);
            }
            else if (skillIndex == 7)
            {
                float charismaAmount = Skill.skillProgressPer * actualAmountToAdd;
                Mod.skills[10].progress += charismaAmount;
                Mod.skills[10].currentProgress += charismaAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[10].raidProgress += actualAmountToAdd;
                    if (Mod.skills[10].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[10].dimishingReturns = true;
                        Mod.skills[10].increasing = false;
                    }
                    else
                    {
                        Mod.skills[10].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(10);
            }
            else if (skillIndex == 8)
            {
                float charismaAmount = Skill.skillProgressInt * actualAmountToAdd;
                Mod.skills[10].progress += charismaAmount;
                Mod.skills[10].currentProgress += charismaAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[10].raidProgress += actualAmountToAdd;
                    if (Mod.skills[10].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[10].dimishingReturns = true;
                        Mod.skills[10].increasing = false;
                    }
                    else
                    {
                        Mod.skills[10].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(10);
            }
            else if (skillIndex == 9)
            {
                float charismaAmount = Skill.skillProgressAtn * actualAmountToAdd;
                Mod.skills[10].progress += charismaAmount;
                Mod.skills[10].currentProgress += charismaAmount;

                if (Mod.currentLocationIndex == 2)
                {
                    Mod.skills[10].raidProgress += actualAmountToAdd;
                    if (Mod.skills[10].raidProgress >= 100) // dimishing returns at 1 level per raid TODO: should be unique to each skill
                    {
                        Mod.skills[10].dimishingReturns = true;
                        Mod.skills[10].increasing = false;
                    }
                    else
                    {
                        Mod.skills[10].increasing = true;
                    }
                }

                StatusUI.instance.UpdateSkillUI(10);
            }

            if (intPreProgress < (int)skill.progress)
            {
                StatusUI.instance.UpdateSkillUI(skillIndex);
            }
        }

        private void Init()
        {
            // Setup player data
            health = new float[7];

            // Instantiate lists
            magazinesByType = new Dictionary<FireArmMagazineType, Dictionary<string, int>>();
            clipsByType = new Dictionary<FireArmClipType, Dictionary<string, int>>();
            roundsByType = new Dictionary<FireArmRoundType, Dictionary<string, int>>();
            activeDescriptionsByItemID = new Dictionary<string, List<DescriptionManager>>();
            usedRoundIDs = new List<string>();
            ammoBoxByAmmoID = new Dictionary<string, int>();
            requiredForQuest = new Dictionary<string, int>();
            wishList = new List<string>();
            itemsByRarity = new Dictionary<MeatovItem.ItemRarity, List<string>>();

            // Subscribe to events
            //SceneManager.sceneLoaded += OnSceneLoaded;
            SteamVR_Events.Loading.Listen(OnSceneLoadedVR);

            // Initially load main menu
            LoadMainMenu();
        }

        public void OnSceneLoadedVR(bool loading)
        {
            if (loading) // Started loading
            {
                // Loading into meatov scene
                if (H3MP.Patches.LoadLevelBeginPatch.loadingLevel.Contains("Meatov"))
                {
                    // Secure scene components if haven't already
                    if(securedMainSceneComponents == null || securedMainSceneComponents.Count == 0)
                    {
                        SecureMainSceneComponents();
                    }

                    // Increase limit of pixel light count to something we are never going to reach
                    // We should never use more than what we have at lvl 3 illumnation in hideout + a few considering possible flashlights, etc.
                    QualitySettings.pixelLightCount = 100;
                }
                else // Not loading into meatov scene
                {
                    if(securedMainSceneComponents != null)
                    {
                        // Unsecure scene components
                        foreach (GameObject go in securedMainSceneComponents)
                        {
                            SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
                        }
                        securedMainSceneComponents.Clear();
                    }

                    // Reset pixel light count
                    GM.RefreshQuality();
                }
            }
            else // Done loading
            {
                inMeatovScene = H3MP.Patches.LoadLevelBeginPatch.loadingLevel.Contains("Meatov");

                switch (H3MP.Patches.LoadLevelBeginPatch.loadingLevel)
                {
                    case "MainMenu3":
                        // Unload hideout bundle
                        if (hideoutBundle != null)
                        {
                            hideoutBundle.Unload(true);
                            hideoutBundle = null;
                            for(int i=0; i < itemsBundles.Length; ++i)
                            {
                                itemsBundles[i].Unload(true);
                                itemsBundles[i] = null;
                            }
                            itemIconsBundle.Unload(true);
                            itemIconsBundle = null;
                            playerBundle.Unload(true);
                            playerBundle = null;
                        }

                        inMeatovScene = false;
                        HideoutController.instance = null;
                        LoadMainMenu();
                        Mod.currentLocationIndex = -1;
                        break;
                    case "MeatovMainMenu":
                        break;
                    case "MeatovHideout":
                        // UnsecureObjects();

                        // Call a GC collect
                        //baseManager.GCManager.gc_collect();
                        break;
                    case "MeatovFactory":
                        //Mod.currentLocationIndex = 2;
                        //UnsecureObjects();

                        //GameObject raidRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];

                        //Raid_Manager raidManager = raidRoot.AddComponent<Raid_Manager>();
                        //raidManager.Init();

                        //GM.CurrentMovementManager.TeleportToPoint(currentRaidManager.spawnPoint.position, true, currentRaidManager.spawnPoint.rotation.eulerAngles);

                        // Unload the map's asset bundle
                        //Mod.currentRaidBundleRequest.assetBundle.Unload(false);

                        // Call a GC collect
                        //raidRoot.GetComponent<Raid_Manager>().GCManager.gc_collect();
                        break;
                    default:
                        Mod.currentLocationIndex = -1;
                        break;
                }
            }
        }

        public static void SecureMainSceneComponents()
        {
            if (securedMainSceneComponents == null)
            {
                securedMainSceneComponents = new List<GameObject>();
            }
            securedMainSceneComponents.Clear();

            // Secure the cameraRig
            GameObject cameraRig = GameObject.Find("[CameraRig]Fixed");
            securedMainSceneComponents.Add(cameraRig);
            GameObject.DontDestroyOnLoad(cameraRig);

            // Secure grabbity spheres
            FVRViveHand rightViveHand = cameraRig.transform.GetChild(0).gameObject.GetComponent<FVRViveHand>();
            FVRViveHand leftViveHand = cameraRig.transform.GetChild(1).gameObject.GetComponent<FVRViveHand>();
            securedMainSceneComponents.Add(rightViveHand.Grabbity_HoverSphere.gameObject);
            securedMainSceneComponents.Add(rightViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_GrabSphere.gameObject);
            securedMainSceneComponents.Add(leftViveHand.Grabbity_HoverSphere.gameObject);
            securedMainSceneComponents.Add(leftViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_GrabSphere.gameObject);

            // Secure MovementManager objects
            securedMainSceneComponents.Add(GM.CurrentMovementManager.MovementRig.gameObject);
            GameObject.DontDestroyOnLoad(GM.CurrentMovementManager.MovementRig.gameObject);
            // Movement arrows could be attached to movement manager if they are activated when we start loading
            // So only add them to the list if their parent is null
            GameObject touchPadArrows = GM.CurrentMovementManager.m_touchpadArrows;
            if (touchPadArrows.transform.parent == null)
            {
                securedMainSceneComponents.Add(touchPadArrows);
                GameObject.DontDestroyOnLoad(touchPadArrows);
            }
            GameObject joystickTPArrows = GM.CurrentMovementManager.m_joystickTPArrows;
            if (joystickTPArrows.transform.parent == null)
            {
                securedMainSceneComponents.Add(joystickTPArrows);
                GameObject.DontDestroyOnLoad(joystickTPArrows);
            }
            GameObject twinStickArrowsLeft = GM.CurrentMovementManager.m_twinStickArrowsLeft;
            if (twinStickArrowsLeft.transform.parent == null)
            {
                securedMainSceneComponents.Add(twinStickArrowsLeft);
                GameObject.DontDestroyOnLoad(twinStickArrowsLeft);
            }
            GameObject twinStickArrowsRight = GM.CurrentMovementManager.m_twinStickArrowsRight;
            if (twinStickArrowsRight.transform.parent == null)
            {
                securedMainSceneComponents.Add(twinStickArrowsRight);
                GameObject.DontDestroyOnLoad(twinStickArrowsRight);
            }
            GameObject floorHelper = GM.CurrentMovementManager.m_floorHelper;
            securedMainSceneComponents.Add(floorHelper);
            GameObject.DontDestroyOnLoad(floorHelper);
        }

        private void LoadMainMenu()
        {
            // Create a MainMenuScenePointable for our level
            GameObject currentPointable = Instantiate<GameObject>(mainMenuPointable);
            currentPointable.name = mainMenuPointable.name;
            MainMenuScenePointable pointableInstance = currentPointable.GetComponent<MainMenuScenePointable>();
            pointableInstance.Screen = GameObject.Find("LevelLoadScreen").GetComponent<MainMenuScreen>();
            currentPointable.transform.position = new Vector3(-0.8909f, 1.4746f, 0.8927f);
            currentPointable.transform.rotation = Quaternion.Euler(21.7584f, 315.6502f, 0);
            currentPointable.transform.localScale = new Vector3(0.3371f, 0.208f, 1);

            // Set LOD bias to default
            QualitySettings.lodBias = 2;
        }

        private void UnsecureObjects()
        {
            if (securedObjects == null)
            {
                securedObjects = new List<GameObject>();
                return;
            }

            foreach (GameObject go in securedObjects)
            {
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            }
            securedObjects.Clear();

            if (Mod.justFinishedRaid && Mod.chosenCharIndex == 1)
            {
                // Make sure that all scav return items are in their proper return nodes
                Transform returnNodeParent = HideoutController.instance.transform.GetChild(1).GetChild(25);
                for (int i = 0; i < 15; ++i)
                {
                    FVRPhysicalObject itemPhysObj = Mod.scavRaidReturnItems[i].GetComponent<FVRPhysicalObject>();
                    itemPhysObj.StoreAndDestroyRigidbody();
                    Transform currentNode = returnNodeParent.GetChild(i);
                    Mod.scavRaidReturnItems[i].transform.parent = currentNode;
                    Mod.scavRaidReturnItems[i].transform.position = currentNode.position;
                    Mod.scavRaidReturnItems[i].transform.rotation = currentNode.rotation;

                    if (i == 8) // Rig
                    {
                        MeatovItem CIW = Mod.scavRaidReturnItems[i].GetComponent<MeatovItem>();
                        if (!CIW.open)
                        {
                            CIW.ToggleMode(false);
                        }
                    }
                }
            }
            else
            {
                // Make sure secured hand objects are put in hands
                if (Mod.physObjColResetList == null)
                {
                    Mod.physObjColResetList = new List<FVRInteractiveObject>();
                }
                else
                {
                    Mod.physObjColResetList.Clear();
                }
                if (leftHand != null && leftHand.fvrHand != null)
                {
                    if (securedLeftHandInteractable != null)
                    {
                        // Set item's cols to NoCol for now so that it doesn't collide with anything on its way to the hand
                        Mod.physObjColResetList.Add(securedLeftHandInteractable);
                        Mod.physObjColResetNeeded = 5;
                        EndInteractionPatch.ignoreEndInteraction = true;
                    }
                    leftHand.fvrHand.ForceSetInteractable(securedLeftHandInteractable);
                    securedLeftHandInteractable = null;
                    if (securedRightHandInteractable != null)
                    {
                        // Set item's cols to NoCol for now so that it doesn't collide with anything on its way to the hand
                        Mod.physObjColResetList.Add(securedRightHandInteractable);
                        Mod.physObjColResetNeeded = 5;
                        EndInteractionPatch.ignoreEndInteraction = true;
                    }
                    rightHand.fvrHand.ForceSetInteractable(securedRightHandInteractable);
                    securedRightHandInteractable = null;
                }
            }
        }

        public static GameObject GetItemPrefab(int index)
        {
            int bundleIndex = index / 300;
            return itemsBundles[bundleIndex].LoadAsset<GameObject>("Item" + index);
        }

        public static bool GetItemData(string H3ID, out MeatovItemData itemData)
        {
            int index = -1;
            if(int.TryParse(H3ID, out index))
            {
                itemData = customItemData[index];
                return itemData != null;
            }
            else
            {
                return vanillaItemData.TryGetValue(H3ID, out itemData);
            }
        }

        public static string GetBodyPartName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Head";
                case 1:
                    return "Thorax";
                case 2:
                    return "Stomach";
                case 3:
                    return "Left Arm";
                case 4:
                    return "Right Arm";
                case 5:
                    return "Left Leg";
                case 6:
                    return "Right Leg";
                default:
                    return "None";
            }
        }

        public static string LocationIndexToDataName(int index)
        {
            switch (index)
            {
                case 0:
                    return "bigmap";
                case 1:
                    return "factory4_day";
                case 2:
                    return "factory4_night";
                case 3:
                    return "interchange";
                case 4:
                    return "laboratory";
                case 5:
                    return "lighthouse";
                case 6:
                    return "rezervbase";
                case 7:
                    return "shoreline";
                case 8:
                    return "suburbs";
                case 9:
                    return "tarkovstreets";
                case 10:
                    return "terminal";
                case 11:
                    return "town";
                case 12:
                    return "woods";
            }
            return null;
        }

        public static string FormatTimeString(float time)
        {
            int hours = (int)(time / 3600);
            int minutes = (int)(time % 3600 / 60);
            int seconds = (int)(time % 3600 % 60);
            return String.Format("{0:00}:{1:00}:{2:00}", hours, minutes, seconds);
        }

        public static long GetItemCountInInventories(string H3ID)
        {
            long count = 0;
            int intCount = 0;
            HideoutController.inventory.TryGetValue(H3ID, out intCount);
            count += intCount;
            playerInventory.TryGetValue(H3ID, out intCount);
            count += intCount;

            return count;
        }

        public static float GetRaritySpawnChanceMultiplier(MeatovItem.ItemRarity rarity)
        {
            switch (rarity)
            {
                case MeatovItem.ItemRarity.Common:
                    return 0.85f;
                case MeatovItem.ItemRarity.Rare:
                    return 0.3f;
                case MeatovItem.ItemRarity.Superrare:
                    return 0.05f;
                case MeatovItem.ItemRarity.Not_exist:
                    return 0;
                default:
                    return 1;
            }
        }

        public static bool IDDescribedInList(string IDToUse, List<string> parentsToUse, List<string> whiteList, List<string> blackList)
        {
            // If an item's corresponding ID is specified in whitelist, then that is the ID we must check the blacklist against.
            // If the blacklist contains any of the item's more specific IDs than the ID in the whitelist, then the item does not fit. Otherwise, it fits.

            // Check item ID
            if (whiteList.Contains(IDToUse))
            {
                // A specific item ID in whitelist should mean that this item fits
                // A specific item ID should obviously never be specified in both white and black lists
                return true;
            }

            // Check ancestors
            for (int i = 0; i < parentsToUse.Count; ++i)
            {
                // If whitelist contains the ancestor ID
                if (whiteList.Contains(parentsToUse[i]))
                {
                    if(blackList == null)
                    {
                        return true;
                    }
                    else
                    {
                        // Must check if any prior ancestor IDs are in the blacklist
                        // If an prior ancestor or the item's ID is found in the blacklist, return false, the item does not fit
                        for (int j = 0; j < i; ++j)
                        {
                            if (blackList.Contains(parentsToUse[j]))
                            {
                                return false;
                            }
                        }
                        return !blackList.Contains(IDToUse);
                    }
                }
            }

            // Getting this far would mean that the item's ID nor any of its ancestors are in the whitelist, so doesn't fit
            // Note that this means anything that uses a whitelist should at least specify the global item ID 54009119af1c881c07000029
            return false;
        }

        public static bool IDDescribedInLists(string IDToUse, List<string> parentsToUse, List<List<string>> whiteLists, List<List<string>> blackLists)
        {
            bool described = false;
            for(int i=0; i<whiteLists.Count && i < blackLists.Count; ++i)
            {
                described |= IDDescribedInList(IDToUse, parentsToUse, whiteLists[i], blackLists[i]);
            }
            return described;
        }

        public static void SetIcon(string itemID, Image icon)
        {
            int parsedID = -1;
            if (int.TryParse(itemID, out parsedID))
            {
                icon.sprite = Mod.itemIconsBundle.LoadAsset<Sprite>("Item" + parsedID + "_Icon");
            }
            else
            {
                if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData) && itemData.H3SpawnerID != null && IM.HasSpawnedID(itemData.H3SpawnerID))
                {
                    icon.sprite = IM.GetSpawnerID(itemData.H3SpawnerID).Sprite;
                }
                else // Could not get icon from item data (spawner ID)
                {
                    Sprite sprite = Mod.itemIconsBundle.LoadAsset<Sprite>("Item" + itemID + "_Icon");
                    if (sprite == null)
                    {
                        Mod.LogError("DEV: Could not get icon for " + itemID);
                    }
                    else
                    {
                        icon.sprite = sprite;
                    }
                }
            }
        }

        public static void LogInfo(string message, bool debug = true)
        {
            if (debug)
            {
#if DEBUG
                modInstance.Logger.LogInfo(message);
#endif
            }
            else
            {
                modInstance.Logger.LogInfo(message);
            }
        }

        public static void LogWarning(string message)
        {
            modInstance.Logger.LogWarning(message);
        }

        public static void LogError(string message)
        {
            modInstance.Logger.LogError(message);
        }

        public static int ItemIDToCurrencyIndex(string itemID)
        {
            switch (itemID)
            {
                case "5449016a4bdc2d6f028b456f":
                    return 0;
                case "5696686a4bdc2da3298b456a":
                    return 1;
                case "569668774bdc2da2298b4568":
                    return 2;
                default:
                    Mod.LogError("DEV: ItemIDToCurrencyIndex could not find index for given ID: " + itemID);
                    return 0;
            }
        }

        public static string TarkovIDtoH3ID(string tarkovID)
        {
            if(itemMap.TryGetValue(tarkovID, out ItemMapEntry entry))
            {
                if (entry.modded)
                {
                    return entry.moddedID;
                }
                else
                {
                    return entry.ID;
                }
            }
            else
            {
                return tarkovID;
            }
        }

        public static void H3MP_OnInstantiationTrack(GameObject go)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // This or one of our instantiation patches will happen first
            // In this case, we want to make sure that if the item is a MeatovItem, we add the script
            // before H3MP tracks it
            if (instantiatedItem == go)
            {
                // We already setup this item, no need to do it here
                // It is ready to be tracked by H3MP
                return;
            }
            else
            {
                // This item was caught by H3MP's instantiation patches and must be setup now
                FVRPhysicalObject physicalObject = go.GetComponent<FVRPhysicalObject>();
                if (physicalObject != null && physicalObject.ObjectWrapper != null)
                {
                    // Must setup the item if it is vanilla
                    if (!physicalObject.ObjectWrapper.ItemID.StartsWith("Meatov"))
                    {
                        MeatovItem.Setup(physicalObject);
                    }
                }
                instantiatedItem = null;
            }
        }

        public static string FormatCompleteMoneyString(long amount)
        {
            string s = amount.ToString();
            int charCount = 0;
            for (int i = s.Length - 1; i >= 0; --i)
            {
                if (charCount != 0 && charCount % 3 == 0)
                {
                    s = s.Insert(i + 1, " ");
                }
                ++charCount;
            }
            return s;
        }

        public static string FormatMoneyString(long amount)
        {
            if (amount < 1000L)
            {
                return amount.ToString();
            }
            if (amount < 1000000L)
            {
                long num = amount / 1000L;
                long num2 = amount / 100L % 10L;
                return num + ((num2 != 0L) ? ("." + num2) : "") + "k";
            }
            long num3 = amount / 1000000L;
            long num4 = amount / 100000L % 10L;
            return num3 + ((num4 != 0L) ? ("." + num4) : "") + "M";
        }
    }

    public class ItemMapEntry
    {
        public bool modded;

        public string ID;
        public string moddedID;
    }

    public class Vector2Int
    {
        public int x;
        public int y;

        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }
}
