using System;
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
        public static readonly float volumePrecisionMultiplier = 10000; // Volumes are stored as floats but are processed as ints to avoid precision errors, this is how precise we should be when converting, this goes down to 10th of a mililiter

        // Live data
        public static Mod modInstance;
        public static int currentLocationIndex = -1; // This will be used by custom item wrapper and vanilla item descr. in their Start(). Shoud only ever be 1(base) or 2(raid). If want to spawn an item in player inventory, will have to set it manually
        public static AssetBundle[] assetsBundles;
        public static AssetBundle defaultAssetsBundle;
        public static AssetBundle mainMenuBundle;
        public static AssetBundle hideoutBundle;
        public static AssetBundleCreateRequest currentRaidBundleRequest;
        public static List<GameObject> securedMainSceneComponents;
        public static List<GameObject> securedObjects;
        public static FVRInteractiveObject securedLeftHandInteractable;
        public static FVRInteractiveObject securedRightHandInteractable;
        public static int saveSlotIndex = -1;
        public static int currentQuickBeltConfiguration = -1;
        public static int firstCustomConfigIndex = -1;
        public static bool beginInteractingEquipRig;
        public static Hand rightHand;
        public static Hand leftHand;
        public static List<List<FVRQuickBeltSlot>> otherActiveSlots;
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
        public static float distanceTravelledSprinting;
        public static float distanceTravelledWalking;
        public static GameObject[] itemsInPocketSlots;
        public static FVRQuickBeltSlot[] pocketSlots;
        public static ShoulderStorage leftShoulderSlot;
        public static ShoulderStorage rightShoulderSlot;
        public static GameObject leftShoulderObject;
        public static GameObject rightShoulderObject;
        public static Dictionary<string, int> baseInventory;
        public static Raid_Manager currentRaidManager;
        public static Dictionary<string, int>[] requiredPerArea;
        public static List<string> wishList;
        public static Dictionary<FireArmMagazineType, Dictionary<string, int>> magazinesByType;
        public static Dictionary<FireArmClipType, Dictionary<string, int>> clipsByType;
        public static Dictionary<FireArmRoundType, Dictionary<string, int>> roundsByType; // TODO: should use the ID of the round, not the name
        public static Dictionary<ItemRarity, List<string>> itemsByRarity;
        public static Dictionary<string, List<string>> itemsByParents;
        public static List<string> usedRoundIDs;
        public static Dictionary<string, int> ammoBoxByAmmoID;
        public static Dictionary<string, int> requiredForQuest;
        public static Dictionary<string, List<DescriptionManager>> activeDescriptionsByItemID;
        public static List<AreaSlot> areaSlots;
        public static bool areaSlotShouldUpdate = true;
        public static List<AreaBonus> activeBonuses;
        public static TraderStatus[] traderStatuses;
        public static CategoryTreeNode itemCategories;
        public static Dictionary<string, List<string>> itemAncestors;
        public static Dictionary<string, int> lowestBuyValueByItem;
        public static bool amountChoiceUIUp;
        public static MeatovItem splittingItem;
        public static bool preventLoadMagUpdateLists; // Flag to prevent load mag patches to update lists before they are initialized
        public static List<KeyValuePair<GameObject, object>> attachmentLocalTransform;
        public static int attachmentCheckNeeded;
        public static List<FVRInteractiveObject> physObjColResetList;
        public static int physObjColResetNeeded;
        public static List<List<bool>> triggeredExplorationTriggers;
        public static GameObject[] scavRaidReturnItems; // Hands, Equipment, Right shoulder, pockets
        public static List<List<TraderTaskReward>> rewardsToGive;
        public static Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>> taskStartConditionsByType = new Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>>();
        public static Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>> taskStartCounterConditionsByType = new Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>>();
        public static Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>> taskCompletionConditionsByType = new Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>>();
        public static Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>> taskCompletionCounterConditionsByType = new Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>>();
        public static Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>> taskFailConditionsByType = new Dictionary<TraderTaskCondition.ConditionType, List<TraderTaskCondition>>();
        public static Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>> taskFailCounterConditionsByType = new Dictionary<TraderTaskCounterCondition.CounterConditionType, List<TraderTaskCounterCondition>>();
        public static Dictionary<string, Dictionary<string, List<TraderTaskCondition>>> taskLeaveItemConditionsByItemIDByZone = new Dictionary<string, Dictionary<string, List<TraderTaskCondition>>>();
        public static Dictionary<string, List<TraderTaskCondition>> taskVisitPlaceConditionsByZone = new Dictionary<string, List<TraderTaskCondition>>();
        public static Dictionary<string, List<TraderTaskCounterCondition>> taskVisitPlaceCounterConditionsByZone = new Dictionary<string, List<TraderTaskCounterCondition>>();
        public static Dictionary<Effect.EffectType, List<TraderTaskCounterCondition>> taskHealthEffectCounterConditionsByEffectType = new Dictionary<Effect.EffectType, List<TraderTaskCounterCondition>>();
        public static Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>> taskShotsCounterConditionsByBodyPart = new Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>>();
        public static Dictionary<string, List<TraderTaskCounterCondition>> taskUseItemCounterConditionsByItemID = new Dictionary<string, List<TraderTaskCounterCondition>>();
        public static Dictionary<string, List<TraderTaskCondition>> taskFindItemConditionsByItemID = new Dictionary<string, List<TraderTaskCondition>>();
        public static Dictionary<int, List<TraderTaskCondition>> taskSkillConditionsBySkillIndex = new Dictionary<int, List<TraderTaskCondition>>();
        public static Dictionary<string, Dictionary<string, List<TraderTaskCondition>>> currentTaskLeaveItemConditionsByItemIDByZone;
        public static Dictionary<string, List<TraderTaskCondition>> currentTaskVisitPlaceConditionsByZone;
        public static Dictionary<string, List<TraderTaskCounterCondition>> currentTaskVisitPlaceCounterConditionsByZone;
        public static Dictionary<Effect.EffectType, List<TraderTaskCounterCondition>> currentHealthEffectCounterConditionsByEffectType;
        public static Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>> currentShotsCounterConditionsByBodyPart;
        public static Dictionary<string, List<TraderTaskCounterCondition>> currentUseItemCounterConditionsByItemID;
        public static GameObject instantiatedItem;
        public static Dictionary<FVRInteractiveObject, MeatovItem> meatovItemByInteractive = new Dictionary<FVRInteractiveObject, MeatovItem>();

        // Player
        public static Dictionary<string, int> playerInventory;
        public static Dictionary<string, List<GameObject>> playerInventoryObjects;
        public static List<InsuredSet> insuredItems;
        public static bool dead;
        public static readonly float[] defaultMaxHealth = { 35, 85, 70, 60, 60, 65, 65 };
        public static float[] currentMaxHealth = { 35, 85, 70, 60, 60, 65, 65 };
        public static float[] health; // 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
        public static float[] hideoutHealthRates = { 0.6125f, 1.4f, 1.225f, 1.05f, 1.05f, 1.1375f, 1.1375f }; // Hideout default healthrates
        public static float[] currentHealthRates = new float[7]; // Should change depending on whether we are in raid or hideout
        public static float[] currentNonLethalHealthRates = new float[7]; // Should change depending on whether we are in raid or hideout
        public static readonly float raidEnergyRate = -3.2f;
        public static readonly float raidHydrationRate = -2.6f;
        public static readonly float hideoutEnergyRate = 1;
        public static readonly float hideoutHydrationRate = 1;
        public static float currentEnergyRate = 1;
        public static float currentHydrationRate = 1;
        public static Text[] partHealthTexts;
        public static Image[] partHealthImages;
        public static Text healthText;
        public static Text healthDeltaText;
        public static float hydration = 100;
        public static float maxHydration = 100;
        public static Text hydrationText;
        public static Text hydrationDeltaText;
        public static float energy = 100;
        public static float maxEnergy = 100;
        public static Text energyText;
        public static Text energyDeltaText;
        public static float staminaTimer = 0;
        public static float stamina = 100;
        public static float maxStamina = 100;
        public static float currentMaxStamina = 100;
        public static Skill[] skills;
        public static int level = 1;
        public static int experience = 0;
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
        public static GameObject quickBeltSlotPrefab;
        public static GameObject rectQuickBeltSlotPrefab;
        public static Material quickSlotHoverMaterial;
        public static Material quickSlotConstantMaterial;
        public static List<GameObject> itemPrefabs;
        public static Dictionary<string, string> itemNames;
        public static Dictionary<string, string> itemDescriptions;
        public static Dictionary<string, int> itemWeights;
        public static Dictionary<string, int> itemVolumes;
        public static GameObject doorLeftPrefab;
        public static GameObject doorRightPrefab;
        public static GameObject doorDoublePrefab;
        public static bool initDoors = true;
        public static Dictionary<string, Sprite> itemIcons;
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
        public static Sprite cartridgeIcon;
        public static Sprite[] playerLevelIcons;
        public static AudioClip[] barbedWireClips;
        public static Sprite[] skillIcons;

        // DB
        public static JArray areasDB;
        public static JObject localeDB;
        public static JObject vanillaItemData;
        public static Dictionary<string, ItemMapEntry> itemMap;
        public static JObject[] traderBaseDB;
        public static JObject[] traderAssortDB;
        public static JObject[] traderQuestAssortDB;
        public static JArray[] traderCategoriesDB;
        public static JObject globalDB;
        public static JObject questDB;
        public static JArray XPPerLevel;
        public static JObject[] locationsLootDB;
        public static JObject[] locationsBaseDB;
        public static JArray lootContainerDB;
        public static JObject dynamicLootTable;
        public static JObject staticLootTable;
        public static JObject defaultItemsData;
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

        public enum ItemType
        {
            Generic = 0,
            BodyArmor = 1,
            Rig = 2,
            ArmoredRig = 3,
            Helmet = 4,
            Backpack = 5,
            Container = 6,
            Pouch = 7,
            AmmoBox = 8,
            Money = 9,
            Consumable = 10,
            Key = 11,
            Earpiece = 12,
            FaceCover = 13,
            Eyewear = 14,
            Headwear = 15,

            LootContainer = 16,

            DogTag = 17
        }

        public enum WeaponClass
        {
            Pistol = 0,
            Revolver = 1,
            SMG = 2,
            Assault = 3,
            Shotgun = 4,
            Sniper = 5,
            LMG = 6,
            HMG = 7,
            Launcher = 8,
            AttachedLauncher = 9,
            DMR = 10
        }

        public enum ItemRarity
        {
            Common,
            Rare,
            Superrare,
            Not_exist
        }

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
                                currentRaidManager.ForceSpawnNextAI();
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
                                UIController.LoadHideout();
                                break;
                            case 6: // Start factory raid
                                Mod.LogInfo("\tDebug: Start factory raid");
                                Mod.chosenMapIndex = -1;
                                GameObject.Find("Hideout").GetComponent<HideoutController>().OnConfirmRaidClicked();
                                break;
                            case 7: // Load autosave
                                Mod.LogInfo("\tDebug: Load autosave");
                                UIController.LoadHideout(5);
                                break;
                            case 8: // Load latest save
                                Mod.LogInfo("\tDebug: Load latest save");
                                UIController.LoadHideout(-1, true);
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
                                currentRaidManager.KillPlayer();
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

                                Raid_Manager.currentManager.ResetHealthEffectCounterConditions();

                                UIController.LoadHideout(5); // Load autosave, which is right before the start of raid

                                Raid_Manager.currentManager.extracted = true;
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
                        }
                    }
                }
            }
#endif
        }

        private void DumpLayers()
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

        public void LoadAssets()
        {
            LogInfo("Loading Main assets", false);
            // Load assets
            quickBeltSlotPrefab = assetsBundles[1].LoadAsset<GameObject>("QuickBeltSlot");
            rectQuickBeltSlotPrefab = assetsBundles[1].LoadAsset<GameObject>("RectQuickBeltSlot");
            consumeUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ConsumeUI");
            stackSplitUIPrefab = assetsBundles[0].LoadAsset<GameObject>("StackSplitUI");
            extractionUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionUI");
            extractionLimitUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionLimitUI");
            extractionCardPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionCard");
            itemDescriptionUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ItemDescriptionUI");
            neededForPrefab = assetsBundles[0].LoadAsset<GameObject>("NeededForText");
            ammoContainsPrefab = assetsBundles[0].LoadAsset<GameObject>("ContainsText");
            cartridgeIcon = assetsBundles[0].LoadAsset<Sprite>("ItemCartridge_Icon");
            playerLevelIcons = new Sprite[16];
            for (int i = 1; i <= 16; ++i)
            {
                playerLevelIcons[i - 1] = assetsBundles[0].LoadAsset<Sprite>("rank" + (i * 5));
            }
            barbedWireClips = new AudioClip[3];
            for (int i = 0; i < 3; ++i)
            {
                barbedWireClips[i] = assetsBundles[0].LoadAsset<AudioClip>("barbwire" + (i + 1));
            }
            skillIcons = new Sprite[64];
            skillIcons[0] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_endurance");
            skillIcons[1] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_strength");
            skillIcons[2] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_vitality");
            skillIcons[3] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_health");
            skillIcons[4] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_stressresistance");
            skillIcons[5] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_metabolism");
            skillIcons[6] = assetsBundles[0].LoadAsset<Sprite>("skill_physical_immunity");
            skillIcons[7] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_perception");
            skillIcons[8] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_intellect");
            skillIcons[9] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_attention");
            skillIcons[10] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_charisma");
            skillIcons[11] = assetsBundles[0].LoadAsset<Sprite>("skill_mental_memory");
            skillIcons[12] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_pistols");
            skillIcons[13] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_revolvers");
            skillIcons[14] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_smgs");
            skillIcons[15] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_assaultrifles");
            skillIcons[16] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_shotguns");
            skillIcons[17] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_sniperrifles");
            skillIcons[18] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_lmgs");
            skillIcons[19] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_hmgs");
            skillIcons[20] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_launchers");
            skillIcons[21] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_ugls");
            skillIcons[22] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_grenades");
            skillIcons[23] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_melee");
            skillIcons[24] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_dmrs");
            skillIcons[25] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_recoilcontrol");
            skillIcons[26] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_weapondrawing");
            skillIcons[27] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_troubleshooting");
            skillIcons[28] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_surgery");
            skillIcons[29] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_covertmovement");
            skillIcons[30] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_search");
            skillIcons[31] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_magdrills");
            skillIcons[32] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_sniping");
            skillIcons[33] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_pronemovement");
            skillIcons[34] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_fieldmedical");
            skillIcons[35] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_basicmedical");
            skillIcons[36] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_lightarmor");
            skillIcons[37] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_heavyarmor");
            skillIcons[38] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_basicweaponmodding");
            skillIcons[39] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_advancedweaponmodding");
            skillIcons[40] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_nightoperations");
            skillIcons[41] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_silentoperations");
            skillIcons[42] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_lockpicking");
            skillIcons[43] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_weapontreatment");
            skillIcons[44] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_freetrading");
            skillIcons[45] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_auctions");
            skillIcons[46] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_cleanoperations");
            skillIcons[47] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_barter");
            skillIcons[48] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_shadowconnections");
            skillIcons[49] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_taskperformance");
            skillIcons[50] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_crafting");
            skillIcons[51] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_hideoutmanagement");
            skillIcons[52] = assetsBundles[0].LoadAsset<Sprite>("skill_combat_weaponswitch");
            skillIcons[53] = assetsBundles[0].LoadAsset<Sprite>("skill_practical_equipmentmanagement");
            skillIcons[54] = assetsBundles[0].LoadAsset<Sprite>("skill_special_bear_aksystems");
            skillIcons[55] = assetsBundles[0].LoadAsset<Sprite>("skill_special_bear_assaultoperations");
            skillIcons[56] = assetsBundles[0].LoadAsset<Sprite>("skill_special_bear_authority");
            skillIcons[57] = assetsBundles[0].LoadAsset<Sprite>("skill_special_bear_heavycaliber");
            skillIcons[58] = assetsBundles[0].LoadAsset<Sprite>("skill_special_bear_rawpower");
            skillIcons[59] = assetsBundles[0].LoadAsset<Sprite>("skill_special_usec_arsystems");
            skillIcons[60] = assetsBundles[0].LoadAsset<Sprite>("skill_special_usec_deepweaponmodding");
            skillIcons[61] = assetsBundles[0].LoadAsset<Sprite>("skill_special_usec_longrangeoptics");
            skillIcons[62] = assetsBundles[0].LoadAsset<Sprite>("skill_special_usec_negotiations");
            skillIcons[63] = assetsBundles[0].LoadAsset<Sprite>("skill_special_usec_tactics");

            /*
            // Load prefabs and assets
            areaCanvasPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaCanvas");
            areaCanvasBottomButtonPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaCanvasBottomButton");
            areaRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaRequirement");
            itemRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("ItemRequirement");
            skillRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("SkillRequirement");
            traderRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("TraderRequirement");
            areaRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaRequirements");
            itemRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("ItemRequirements");
            skillRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("SkillRequirements");
            traderRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("TraderRequirements");
            bonusPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("Bonus");
            areaBackgroundNormalSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_default_back");
            areaBackgroundLockedSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_locked_back");
            areaBackgroundAvailableSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_default_back_green");
            areaBackgroundEliteSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_elite_back");
            areaStatusIconUpgrading = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_upgrading");
            areaStatusIconConstructing = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_constructing");
            areaStatusIconLocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_lock");
            areaStatusIconUnlocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_unlocked");
            areaStatusIconReadyUpgrade = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_ready_to_upgrade");
            areaStatusIconProducing = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_producing");
            areaStatusIconOutOfFuel = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_out_of_fuel");
            requirementFulfilled = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_requirement_fulfilled");
            requirementLocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_requirement_locked");
            emptyItemSlotIcon = Mod.baseAssetsBundle.LoadAsset<Sprite>("slot_empty_fill");
            dollarCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_dollars");
            euroCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_euros");
            roubleCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_roubles");
            barterSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_currency_barter");
            experienceSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_experience_big");
            standingSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("standing_icon");
            traderAvatars = new Sprite[8];
            traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_russian_small");
            traderAvatars[1] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_therapist_small");
            traderAvatars[2] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_fence_small");
            traderAvatars[3] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ah_small");
            traderAvatars[4] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_peacekeeper_small");
            traderAvatars[5] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_tech_small");
            traderAvatars[6] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ragman_small");
            traderAvatars[7] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_jaeger_small");
            areaIcons = new Sprite[22];
            areaIcons[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_vents");
            areaIcons[1] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_security");
            areaIcons[2] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_watercloset");
            areaIcons[3] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_stash");
            areaIcons[4] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_generators");
            areaIcons[5] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_heating");
            areaIcons[6] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_rain_collector");
            areaIcons[7] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_medstation");
            areaIcons[8] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_kitchen");
            areaIcons[9] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_restplace");
            areaIcons[10] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_workbench");
            areaIcons[11] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_intelligence_center");
            areaIcons[12] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_shooting_range");
            areaIcons[13] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_library");
            areaIcons[14] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_scav_case");
            areaIcons[15] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_illumination");
            areaIcons[16] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_placeoffame");
            areaIcons[17] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_afu");
            areaIcons[18] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_solarpower");
            areaIcons[19] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_boozegen");
            areaIcons[20] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_bitcoinfarm");
            areaIcons[21] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_christmas_illumination");
            bonusIcons = new Dictionary<string, Sprite>();
            bonusIcons.Add("ExperienceRate", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_exp_small"));
            bonusIcons.Add("HealthRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_medical"));
            bonusIcons.Add("/files/Hideout/icon_hideout_createitem_meds.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_meds"));
            bonusIcons.Add("/files/Hideout/icon_hideout_videocardslots.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_videocardslots"));
            bonusIcons.Add("/files/Hideout/icon_hideout_createitem_bitcoin.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_bitcoin"));
            bonusIcons.Add("/files/Hideout/icon_hideout_unlocked.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_unlocked"));
            bonusIcons.Add("FuelConsumption", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_fuelconsumption"));
            bonusIcons.Add("/files/Hideout/icon_hideout_fuelslots.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_fuelslots"));
            bonusIcons.Add("EnergyRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_energy"));
            bonusIcons.Add("HydrationRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_hydration"));
            bonusIcons.Add("/files/Hideout/icon_hideout_shootingrangeunlock.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_shootingrangeunlock"));
            bonusIcons.Add("DebuffEndDelay", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
            bonusIcons.Add("UnlockWeaponModification", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_weaponmodunlock"));
            bonusIcons.Add("SkillGroupLevelingBoost", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
            bonusIcons.Add("AdditionalSlots", Mod.baseAssetsBundle.LoadAsset<Sprite>("skills_grid_icon"));
            bonusIcons.Add("StashSize", Mod.baseAssetsBundle.LoadAsset<Sprite>("skills_grid_icon"));
            bonusIcons.Add("/files/Hideout/icon_hideout_scavitem.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_scavitem"));
            bonusIcons.Add("ScavCooldownTimer", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_time"));
            bonusIcons.Add("InsuranceReturnTime", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_time"));
            bonusIcons.Add("QuestMoneyReward", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money"));
            bonusIcons.Add("RagfairCommission", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money"));
            bonusIcons.Add("/files/Hideout/icon_hideout_createitem_generic.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_generic"));
            bonusIcons.Add("MaximumEnergyReserve", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_batterycharge"));
            bonusIcons.Add("UnlockArmorRepair", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_itemtype_gear_armor"));
            bonusIcons.Add("UnlockWeaponRepair", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_itemtype_weapon"));
            */
            // Load pockets configuration
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            List<GameObject> rigConfigurations = new List<GameObject>();
            pocketSlots = new FVRQuickBeltSlot[4];
            int rigIndex = 0;
            GameObject pocketsConfiguration = assetsBundles[1].LoadAsset<GameObject>("PocketsConfiguration");
            rigConfigurations.Add(pocketsConfiguration);
            for (int j = 0; j < pocketsConfiguration.transform.childCount; ++j)
            {
                GameObject slotObject = pocketsConfiguration.transform.GetChild(j).gameObject;
                slotObject.tag = "QuickbeltSlot";
                slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields

                string[] splitSlotName = slotObject.name.Split('_');
                FVRQuickBeltSlot.QuickbeltSlotShape slotShape = splitSlotName[1].Contains("Rect") ? FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear : FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;

                FVRQuickBeltSlot slotComponent = slotObject.AddComponent<FVRQuickBeltSlot>();
                slotComponent.QuickbeltRoot = slotObject.transform;
                slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                slotComponent.HoverGeo.SetActive(false);
                slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                slotComponent.Shape = slotShape;

                switch (splitSlotName[0])
                {
                    case "Small":
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Small;
                        break;
                    case "Medium":
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Medium;
                        break;
                    case "Large":
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Large;
                        break;
                    case "Massive":
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Massive;
                        break;
                    case "CantCarryBig":
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                        break;
                    default:
                        break;
                }
                slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;

                // Set slot glow materials
                slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = quickSlotHoverMaterial;
                slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = quickSlotConstantMaterial;

                slotObject.SetActive(true);
            }

            pocketsConfigIndex = ManagerSingleton<GM>.Instance.QuickbeltConfigurations.Length + rigIndex++;
            itemsInPocketSlots = new GameObject[4];

            LogInfo("Loading item prefabs...");
            // Load custom item prefabs
            otherActiveSlots = new List<List<FVRQuickBeltSlot>>();
            itemPrefabs = new List<GameObject>();
            itemNames = new Dictionary<string, string>();
            itemDescriptions = new Dictionary<string, string>();
            itemWeights = new Dictionary<string, int>();
            itemVolumes = new Dictionary<string, int>();
            itemIcons = new Dictionary<string, Sprite>();
            defaultItemsData = JObject.Parse(File.ReadAllText(Mod.path + "/database/DefaultItemData.json"));
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            itemSounds = new Dictionary<string, AudioClip[]>();
            string[] soundCategories = new string[] { "drop", "pickup", "offline_use", "open", "use", "use_loop" };
            for (int i = 0; i < ((JArray)defaultItemsData["ItemDefaults"]).Count; ++i)
            {
                LogInfo("\tLoading Item" + i);
                int assetBundleIndex = i > 500 ? 1 : 0;
                GameObject itemPrefab = assetsBundles[assetBundleIndex].LoadAsset<GameObject>("Item" + i);
                itemPrefab.name = defaultItemsData["ItemDefaults"][i]["DefaultPhysicalObject"]["DefaultObjectWrapper"]["DisplayName"].ToString();

                itemPrefabs.Add(itemPrefab);
                itemNames.Add(i.ToString(), itemPrefab.name);

                Sprite itemIcon = assetsBundles[assetBundleIndex].LoadAsset<Sprite>("Item" + i + "_Icon");
                itemIcons.Add(i.ToString(), itemIcon);

                int itemType = ((int)defaultItemsData["ItemDefaults"][i]["ItemType"]);

                // Create an FVRPhysicalObject and FVRObject to fill with the item's default data
                FVRPhysicalObject itemPhysicalObject = null;
                if (itemType == 8) // Ammo box needs to be magazine
                {
                    itemPhysicalObject = itemPrefab.AddComponent<FVRFireArmMagazine>();
                }
                else if (itemType == 11)
                {
                    itemPhysicalObject = itemPrefab.AddComponent<LockKey>();
                }
                else
                {
                    itemPhysicalObject = itemPrefab.AddComponent<FVRPhysicalObject>();
                }
                FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper = ScriptableObject.CreateInstance<FVRObject>();

                JToken defaultPhysicalObject = defaultItemsData["ItemDefaults"][i]["DefaultPhysicalObject"];
                itemPhysicalObject.SpawnLockable = (bool)defaultPhysicalObject["SpawnLockable"];
                itemPhysicalObject.Harnessable = (bool)defaultPhysicalObject["Harnessable"];
                itemPhysicalObject.HandlingReleaseIntoSlotSound = (HandlingReleaseIntoSlotType)((int)defaultPhysicalObject["HandlingReleaseIntoSlotSound"]);
                itemPhysicalObject.Size = (FVRPhysicalObject.FVRPhysicalObjectSize)((int)defaultPhysicalObject["Size"]);
                itemPhysicalObject.QBSlotType = (FVRQuickBeltSlot.QuickbeltSlotType)((int)defaultPhysicalObject["QBSlotType"]);
                itemPhysicalObject.DoesReleaseOverrideVelocity = (bool)defaultPhysicalObject["DoesReleaseOverrideVelocity"];
                itemPhysicalObject.DoesReleaseAddVelocity = (bool)defaultPhysicalObject["DoesReleaseAddVelocity"];
                itemPhysicalObject.ThrowVelMultiplier = (float)defaultPhysicalObject["ThrowVelMultiplier"];
                itemPhysicalObject.ThrowAngMultiplier = (float)defaultPhysicalObject["ThrowAngMultiplier"];
                itemPhysicalObject.MoveIntensity = (float)defaultPhysicalObject["MoveIntensity"];
                itemPhysicalObject.RotIntensity = (float)defaultPhysicalObject["RotIntensity"];
                itemPhysicalObject.UsesGravity = (bool)defaultPhysicalObject["UsesGravity"];
                itemPhysicalObject.DistantGrabbable = (bool)defaultPhysicalObject["DistantGrabbable"];
                itemPhysicalObject.IsDebug = (bool)defaultPhysicalObject["IsDebug"];
                itemPhysicalObject.IsAltHeld = (bool)defaultPhysicalObject["IsAltHeld"];
                itemPhysicalObject.IsKinematicLocked = (bool)defaultPhysicalObject["IsKinematicLocked"];
                itemPhysicalObject.DoesQuickbeltSlotFollowHead = (bool)defaultPhysicalObject["DoesQuickbeltSlotFollowHead"];
                itemPhysicalObject.IsPickUpLocked = (bool)defaultPhysicalObject["IsPickUpLocked"];
                itemPhysicalObject.OverridesObjectToHand = (FVRPhysicalObject.ObjectToHandOverrideMode)((int)defaultPhysicalObject["OverridesObjectToHand"]);
                itemPhysicalObject.PoseOverride = itemPrefab.transform.GetChild(1);
                if (!itemPhysicalObject.PoseOverride.gameObject.activeSelf)
                {
                    itemPhysicalObject.PoseOverride = null;
                }
                //itemPhysicalObject.PoseOverride_Touch = itemPrefab.transform.GetChild(4);
                //if (!itemPhysicalObject.PoseOverride_Touch.gameObject.activeSelf)
                //{
                //    itemPhysicalObject.PoseOverride_Touch = null;
                //}
                itemPhysicalObject.QBPoseOverride = itemPrefab.transform.GetChild(2);
                itemPhysicalObject.UseGrabPointChild = true; // Makes sure the item will be held where the player grabs it instead of at pose override

                JToken defaultObjectWrapper = defaultPhysicalObject["DefaultObjectWrapper"];
                itemObjectWrapper.ItemID = i.ToString();
                itemObjectWrapper.DisplayName = defaultObjectWrapper["DisplayName"].ToString();
                itemObjectWrapper.Category = (FVRObject.ObjectCategory)((int)defaultObjectWrapper["Category"]);
                itemObjectWrapper.Mass = itemPrefab.GetComponent<Rigidbody>().mass;
                itemWeights.Add(i.ToString(), (int)(itemObjectWrapper.Mass * 1000));
                itemObjectWrapper.MagazineCapacity = (int)defaultObjectWrapper["MagazineCapacity"];
                itemObjectWrapper.RequiresPicatinnySight = (bool)defaultObjectWrapper["RequiresPicatinnySight"];
                itemObjectWrapper.TagEra = (FVRObject.OTagEra)(int)defaultObjectWrapper["TagEra"];
                itemObjectWrapper.TagSet = (FVRObject.OTagSet)(int)defaultObjectWrapper["TagSet"];
                itemObjectWrapper.TagFirearmSize = (FVRObject.OTagFirearmSize)(int)defaultObjectWrapper["TagFirearmSize"];
                itemObjectWrapper.TagFirearmAction = (FVRObject.OTagFirearmAction)(int)defaultObjectWrapper["TagFirearmAction"];
                itemObjectWrapper.TagFirearmRoundPower = (FVRObject.OTagFirearmRoundPower)(int)defaultObjectWrapper["TagFirearmRoundPower"];
                itemObjectWrapper.TagFirearmCountryOfOrigin = (FVRObject.OTagFirearmCountryOfOrigin)(int)defaultObjectWrapper["TagFirearmCountryOfOrigin"];
                itemObjectWrapper.TagAttachmentMount = (FVRObject.OTagFirearmMount)((int)defaultObjectWrapper["TagAttachmentMount"]);
                itemObjectWrapper.TagAttachmentFeature = (FVRObject.OTagAttachmentFeature)((int)defaultObjectWrapper["TagAttachmentFeature"]);
                itemObjectWrapper.TagMeleeStyle = (FVRObject.OTagMeleeStyle)((int)defaultObjectWrapper["TagMeleeStyle"]);
                itemObjectWrapper.TagMeleeHandedness = (FVRObject.OTagMeleeHandedness)((int)defaultObjectWrapper["TagMeleeHandedness"]);
                itemObjectWrapper.TagPowerupType = (FVRObject.OTagPowerupType)((int)defaultObjectWrapper["TagPowerupType"]);
                itemObjectWrapper.TagThrownType = (FVRObject.OTagThrownType)((int)defaultObjectWrapper["TagThrownType"]);
                itemObjectWrapper.MagazineType = (FireArmMagazineType)((int)defaultObjectWrapper["MagazineType"]);
                itemObjectWrapper.CreditCost = (int)defaultObjectWrapper["CreditCost"];
                itemObjectWrapper.OSple = (bool)defaultObjectWrapper["OSple"];

                // Add custom item wrapper
                MeatovItem customItemWrapper = itemPrefab.AddComponent<MeatovItem>();
                customItemWrapper.H3ID = i.ToString();
                customItemWrapper.itemType = (ItemType)(int)defaultItemsData["ItemDefaults"][i]["ItemType"];
                float[] tempVolumes = defaultItemsData["ItemDefaults"][i]["Volumes"].ToObject<float[]>();
                customItemWrapper.volumes = new int[tempVolumes.Length];
                for (int volIndex = 0; volIndex < tempVolumes.Length; ++volIndex)
                {
                    customItemWrapper.volumes[volIndex] = (int)(tempVolumes[volIndex] * Mod.volumePrecisionMultiplier);
                }

                itemVolumes.Add(i.ToString(), customItemWrapper.volumes[0]);
                customItemWrapper.parents = defaultItemsData["ItemDefaults"][i]["parents"] != null ? defaultItemsData["ItemDefaults"][i]["parents"].ToObject<List<String>>() : new List<string>();
                if (itemAncestors == null)
                {
                    itemAncestors = new Dictionary<string, List<string>>();
                }
                itemAncestors.Add(customItemWrapper.H3ID, customItemWrapper.parents);
                customItemWrapper.itemName = itemObjectWrapper.DisplayName;
                customItemWrapper.description = defaultItemsData["ItemDefaults"][i]["description"] != null ? defaultItemsData["ItemDefaults"][i]["description"].ToString() : "";
                itemDescriptions.Add(i.ToString(), customItemWrapper.description);
                customItemWrapper.lootExperience = (int)defaultItemsData["ItemDefaults"][i]["lootExperience"];
                customItemWrapper.spawnChance = (float)defaultItemsData["ItemDefaults"][i]["spawnChance"];
                customItemWrapper.rarity = ItemRarityStringToEnum(defaultItemsData["ItemDefaults"][i]["rarity"].ToString());
                if (itemsByRarity.ContainsKey(customItemWrapper.rarity))
                {
                    itemsByRarity[customItemWrapper.rarity].Add(customItemWrapper.H3ID);
                }
                else
                {
                    itemsByRarity.Add(customItemWrapper.rarity, new List<string>() { customItemWrapper.H3ID });
                }
                if (defaultItemsData["ItemDefaults"][i]["MaxAmount"] != null)
                {
                    customItemWrapper.maxAmount = (int)defaultItemsData["ItemDefaults"][i]["MaxAmount"];
                    customItemWrapper._amount = customItemWrapper.maxAmount;
                }
                if (defaultItemsData["ItemDefaults"][i]["BlocksEarpiece"] != null)
                {
                    customItemWrapper.blocksEarpiece = (bool)defaultItemsData["ItemDefaults"][i]["BlocksEarpiece"];
                    customItemWrapper.blocksEyewear = (bool)defaultItemsData["ItemDefaults"][i]["BlocksEyewear"];
                    customItemWrapper.blocksFaceCover = (bool)defaultItemsData["ItemDefaults"][i]["BlocksFaceCover"];
                    customItemWrapper.blocksHeadwear = (bool)defaultItemsData["ItemDefaults"][i]["BlocksHeadwear"];
                }
                string itemSound = defaultItemsData["ItemDefaults"][i]["itemSound"].ToString();
                if (!itemSound.Equals("None"))
                {
                    AudioClip[] sounds;
                    if (!itemSounds.ContainsKey(itemSound))
                    {
                        sounds = new AudioClip[soundCategories.Length];
                        int count = 0;
                        for (int j = 0; j < sounds.Length; ++j)
                        {
                            sounds[j] = assetsBundles[0].LoadAsset<AudioClip>(itemSound + "_" + soundCategories[j]);
                            count += sounds[j] == null ? 0 : 1;
                        }
                        if (count == 0)
                        {
                            Mod.LogError("No item sound found for category: " + itemSound);
                            itemPhysicalObject.HandlingGrabSound = HandlingGrabType.Generic;
                            itemPhysicalObject.HandlingReleaseSound = HandlingReleaseType.HardSmooth;
                        }
                        itemSounds.Add(itemSound, sounds);
                        customItemWrapper.itemSounds = sounds;
                    }
                    else
                    {
                        sounds = itemSounds[itemSound];
                    }
                    customItemWrapper.itemSounds = sounds;
                }
                else
                {
                    itemPhysicalObject.HandlingGrabSound = HandlingGrabType.Generic;
                    itemPhysicalObject.HandlingReleaseSound = HandlingReleaseType.HardSmooth;
                }

                AddCategories(customItemWrapper.parents);

                foreach (string parent in customItemWrapper.parents)
                {
                    if (itemsByParents.ContainsKey(parent))
                    {
                        itemsByParents[parent].Add(customItemWrapper.H3ID);
                    }
                    else
                    {
                        itemsByParents.Add(parent, new List<string>() { customItemWrapper.H3ID });
                    }
                }

                // Add an EFM_OtherInteractable to each child under Interactives recursively and add them to interactiveSets
                List<GameObject> interactiveSets = new List<GameObject>();
                foreach (Transform interactive in itemPrefab.transform.GetChild(3))
                {
                    interactiveSets.Add(MakeItemInteractiveSet(interactive, itemPhysicalObject));
                }
                customItemWrapper.interactiveSets = interactiveSets.ToArray();

                // Fill customItemWrapper models
                List<GameObject> models = new List<GameObject>();
                foreach (Transform model in itemPrefab.transform.GetChild(0))
                {
                    models.Add(model.gameObject);
                }
                customItemWrapper.models = models.ToArray();

                // Add PMat depending on properties
                PMat pMat = itemPrefab.AddComponent<PMat>();
                pMat.Def = ScriptableObject.CreateInstance<PMaterialDefinition>();
                pMat.Def.material = (PMaterial)Enum.Parse(typeof(PMaterial), defaultItemsData["ItemDefaults"][i]["MaterialProperties"]["PMaterial"].ToString());
                pMat.Def.soundCategory = (PMatSoundCategory)Enum.Parse(typeof(PMatSoundCategory), defaultItemsData["ItemDefaults"][i]["MaterialProperties"]["PMatSoundCategory"].ToString());
                pMat.Def.impactCategory = (PMatImpactEffectCategory)Enum.Parse(typeof(PMatImpactEffectCategory), defaultItemsData["ItemDefaults"][i]["MaterialProperties"]["PMatImpactEffectCategory"].ToString());

                // Armor
                if (itemType == 1 || itemType == 3)
                {
                    customItemWrapper.coverage = (float)defaultItemsData["ItemDefaults"][i]["ArmorAndRigProperties"]["Coverage"];
                    customItemWrapper.damageResist = (float)defaultItemsData["ItemDefaults"][i]["ArmorAndRigProperties"]["DamageResist"];
                    customItemWrapper.maxArmor = (float)defaultItemsData["ItemDefaults"][i]["ArmorAndRigProperties"]["Armor"];
                    customItemWrapper.armor = customItemWrapper.maxArmor;
                }

                // Rig
                if (itemType == 2 || itemType == 3)
                {
                    // Setup the slots for the player rig config and the rig config
                    int slotCount = 0;
                    GameObject quickBeltConfiguration = assetsBundles[assetBundleIndex].LoadAsset<GameObject>("Item" + i + "Configuration");
                    customItemWrapper.rigSlots = new List<FVRQuickBeltSlot>();
                    for (int j = 0; j < quickBeltConfiguration.transform.childCount; ++j)
                    {
                        GameObject slotObject = quickBeltConfiguration.transform.GetChild(j).gameObject;
                        slotObject.tag = "QuickbeltSlot";
                        slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields

                        GameObject rigSlotObject = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).GetChild(0).GetChild(j).gameObject;
                        slotObject.tag = "QuickbeltSlot";
                        slotObject.SetActive(false);

                        string[] splitSlotName = slotObject.name.Split('_');
                        FVRQuickBeltSlot.QuickbeltSlotShape slotShape = splitSlotName[1].Contains("Rect") ? FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear : FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;

                        FVRQuickBeltSlot slotComponent = slotObject.AddComponent<FVRQuickBeltSlot>();
                        slotComponent.QuickbeltRoot = slotObject.transform;
                        slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                        slotComponent.HoverGeo.SetActive(false);
                        slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                        slotComponent.Shape = slotShape;

                        FVRQuickBeltSlot rigSlotComponent = rigSlotObject.AddComponent<FVRQuickBeltSlot>();
                        customItemWrapper.rigSlots.Add(rigSlotComponent);
                        rigSlotComponent.QuickbeltRoot = rigSlotObject.transform;
                        rigSlotComponent.HoverGeo = rigSlotObject.transform.GetChild(0).GetChild(0).gameObject;
                        rigSlotComponent.HoverGeo.SetActive(false);
                        rigSlotComponent.PoseOverride = rigSlotObject.transform.GetChild(0).GetChild(2);
                        rigSlotComponent.Shape = slotShape;
                        if (slotShape == FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear)
                        {
                            slotComponent.RectBounds = slotObject.transform.GetChild(0);
                            rigSlotComponent.RectBounds = rigSlotObject.transform.GetChild(0);
                        }
                        switch (splitSlotName[0])
                        {
                            case "Small":
                                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Small;
                                rigSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Small;
                                break;
                            case "Medium":
                                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Medium;
                                rigSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Medium;
                                break;
                            case "Large":
                                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Large;
                                rigSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Large;
                                break;
                            case "Massive":
                                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Massive;
                                rigSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.Massive;
                                break;
                            case "CantCarryBig":
                                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                                rigSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                                break;
                            default:
                                break;
                        }
                        slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
                        rigSlotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;

                        // Set slot glow materials
                        slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = quickSlotHoverMaterial;
                        slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = quickSlotConstantMaterial;

                        // Set slot glow materials
                        rigSlotComponent.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = quickSlotHoverMaterial;
                        rigSlotComponent.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = quickSlotConstantMaterial;

                        ++slotCount;

                        slotObject.SetActive(true);
                        rigSlotObject.SetActive(true);
                    }

                    rigConfigurations.Add(quickBeltConfiguration);

                    customItemWrapper.configurationIndex = ManagerSingleton<GM>.Instance.QuickbeltConfigurations.Length + rigIndex++;
                    customItemWrapper.itemsInSlots = new GameObject[slotCount];
                    customItemWrapper.configurationRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2);

                    //if (Mod.rightHand.fvrHand.CMode == ControlMode.Index || Mod.rightHand.fvrHand.CMode == ControlMode.Oculus)
                    //{
                    //    customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride_Touch;
                    //    if (customItemWrapper.rightHandPoseOverride != null)
                    //    {
                    //        customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride_Touch.GetChild(0);
                    //    }
                    //}
                    //else
                    {
                        customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride;
                        if (customItemWrapper.rightHandPoseOverride != null)
                        {
                            customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride.GetChild(0);
                        }
                    }
                }

                // Backpack
                if (itemType == 5)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
                    EFM_MainContainer mainContainerScript = customItemWrapper.mainContainer.AddComponent<EFM_MainContainer>();
                    mainContainerScript.parentCIW = customItemWrapper;
                    List<Renderer> mainContainerRenderers = new List<Renderer>();
                    mainContainerRenderers.Add(mainContainer.GetComponent<Renderer>());
                    mainContainerRenderers[mainContainerRenderers.Count - 1].material = quickSlotConstantMaterial;
                    for (int j = 0; j < mainContainer.transform.childCount; ++j)
                    {
                        mainContainerRenderers.Add(mainContainer.transform.GetChild(j).GetComponent<Renderer>());
                        mainContainerRenderers[mainContainerRenderers.Count - 1].material = quickSlotConstantMaterial;
                    }
                    customItemWrapper.mainContainerRenderers = mainContainerRenderers.ToArray();

                    // Set pose overrides
                    //if (Mod.rightHand.fvrHand.CMode == ControlMode.Index || Mod.rightHand.fvrHand.CMode == ControlMode.Oculus)
                    //{
                    //    customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride_Touch;
                    //    if (customItemWrapper.rightHandPoseOverride != null)
                    //    {
                    //        customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride_Touch.GetChild(0);
                    //    }
                    //}
                    //else
                    {
                        customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride;
                        if (customItemWrapper.rightHandPoseOverride != null)
                        {
                            customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride.GetChild(0);
                        }
                    }

                    // Set backpack settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.containerItemRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (int)((float)defaultItemsData["ItemDefaults"][i]["BackpackProperties"]["MaxVolume"] * Mod.volumePrecisionMultiplier);

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Container
                if (itemType == 6)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
                    EFM_MainContainer mainContainerScript = customItemWrapper.mainContainer.AddComponent<EFM_MainContainer>();
                    mainContainerScript.parentCIW = customItemWrapper;
                    Renderer[] mainContainerRenderer = new Renderer[1];
                    mainContainerRenderer[0] = mainContainer.GetComponent<Renderer>();
                    mainContainerRenderer[0].material = quickSlotConstantMaterial;
                    customItemWrapper.mainContainerRenderers = mainContainerRenderer;

                    // Set container settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.containerItemRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (int)((float)defaultItemsData["ItemDefaults"][i]["ContainerProperties"]["MaxVolume"] * Mod.volumePrecisionMultiplier);

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Pouch
                if (itemType == 7)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
                    EFM_MainContainer mainContainerScript = customItemWrapper.mainContainer.AddComponent<EFM_MainContainer>();
                    mainContainerScript.parentCIW = customItemWrapper;
                    Renderer[] mainContainerRenderer = new Renderer[1];
                    mainContainerRenderer[0] = mainContainer.GetComponent<Renderer>();
                    mainContainerRenderer[0].material = quickSlotConstantMaterial;
                    customItemWrapper.mainContainerRenderers = mainContainerRenderer;

                    // Set pouch settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.containerItemRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (int)((float)defaultItemsData["ItemDefaults"][i]["ContainerProperties"]["MaxVolume"] * Mod.volumePrecisionMultiplier);

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Ammobox
                if (itemType == 8)
                {
                    // Make sure keys use the poseoverride when grabbing, easier to add rounds to and round extract transform is positionned accordingly
                    itemPhysicalObject.UseGrabPointChild = false;

                    if (!defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["roundType"].ToString().Equals("none"))
                    {
                        customItemWrapper.roundClass = (FireArmRoundClass)Enum.Parse(typeof(FireArmRoundClass), defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["roundClass"].ToString());
                        customItemWrapper.maxAmount = (int)defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["maxStack"];

                        FVRFireArmMagazine fireArmMagazine = (FVRFireArmMagazine)itemPhysicalObject;
                        fireArmMagazine.Profile = ScriptableObject.CreateInstance<FVRFirearmAudioSet>();
                        fireArmMagazine.Profile.MagazineInsertRound = new AudioEvent();
                        fireArmMagazine.Profile.MagazineEjectRound = new AudioEvent();
                        fireArmMagazine.Viz = itemPrefab.transform;
                        fireArmMagazine.DisplayBullets = new GameObject[0];
                        fireArmMagazine.MagazineType = FireArmMagazineType.mNone;
                        fireArmMagazine.RoundEjectionPos = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2);
                        fireArmMagazine.RoundType = (FireArmRoundType)Enum.Parse(typeof(FireArmRoundType), defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["roundType"].ToString());
                        fireArmMagazine.m_capacity = (int)defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["maxStack"];
                        fireArmMagazine.m_numRounds = 0;

                        FVRFireArmMagazineReloadTrigger reloadTrigger = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1).gameObject.AddComponent<FVRFireArmMagazineReloadTrigger>();
                        reloadTrigger.Magazine = fireArmMagazine;
                        reloadTrigger.tag = "FVRFireArmMagazineReloadTrigger";

                        if (!ammoBoxByAmmoID.ContainsKey(defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["cartridge"].ToString()))
                        {
                            ammoBoxByAmmoID.Add(defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["cartridge"].ToString(), i);
                        }
                    }
                    else // Generic ammo box, does not have specific round type (yet, should be given one when spawned) and reload trigger
                    {
                        FVRFireArmMagazine fireArmMagazine = (FVRFireArmMagazine)itemPhysicalObject;
                        fireArmMagazine.Profile = ScriptableObject.CreateInstance<FVRFirearmAudioSet>();
                        fireArmMagazine.Profile.MagazineInsertRound = new AudioEvent();
                        fireArmMagazine.Profile.MagazineEjectRound = new AudioEvent();
                        fireArmMagazine.Viz = itemPrefab.transform;
                        fireArmMagazine.DisplayBullets = new GameObject[0];
                        fireArmMagazine.MagazineType = FireArmMagazineType.mNone;
                        fireArmMagazine.RoundEjectionPos = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2);
                        fireArmMagazine.RoundType = FireArmRoundType.a106_25mmR; // Just a default one
                        fireArmMagazine.m_capacity = (int)defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["maxStack"];
                        fireArmMagazine.m_numRounds = 0;
                    }
                }

                // Money
                if (itemType == 9)
                {
                    // Make sure money use the poseoverride when grabbing, easier to stack
                    itemPhysicalObject.UseGrabPointChild = false;

                    // Add stacktriggers
                    Transform triggerRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.stackTriggers = new GameObject[3];
                    for (int j = 0; j < 3; ++j)
                    {
                        GameObject triggerObject = triggerRoot.GetChild(j).gameObject;
                        StackTrigger currentStackTrigger = triggerObject.AddComponent<StackTrigger>();
                        currentStackTrigger.stackableWrapper = customItemWrapper;

                        customItemWrapper.stackTriggers[j] = triggerObject;
                    }

                    // Set stack defaults
                    customItemWrapper.maxStack = (int)defaultItemsData["ItemDefaults"][i]["stackMaxSize"];
                }

                // Consumable
                if (itemType == 10)
                {
                    // Set use time
                    customItemWrapper.useTime = (float)defaultItemsData["ItemDefaults"][i]["consumableUseTime"];

                    // Set amount rate
                    if (defaultItemsData["ItemDefaults"][i]["hpResourceRate"] != null)
                    {
                        customItemWrapper.amountRate = (float)defaultItemsData["ItemDefaults"][i]["hpResourceRate"];
                    }

                    // Set effects
                    customItemWrapper.consumeEffects = new List<EFM_Effect_Consumable>();
                    if (defaultItemsData["ItemDefaults"][i]["effectsDamage"] != null)
                    {
                        // Damage effects
                        Dictionary<string, JToken> damageEffects = defaultItemsData["ItemDefaults"][i]["effectsDamage"].ToObject<Dictionary<string, JToken>>();
                        foreach (KeyValuePair<string, JToken> damageEntry in damageEffects)
                        {
                            EFM_Effect_Consumable consumableEffect = new EFM_Effect_Consumable();
                            customItemWrapper.consumeEffects.Add(consumableEffect);
                            consumableEffect.delay = (float)damageEntry.Value["delay"];
                            consumableEffect.duration = (float)damageEntry.Value["duration"];
                            if (damageEntry.Value["cost"] != null)
                            {
                                consumableEffect.cost = (int)damageEntry.Value["cost"];
                            }
                            switch (damageEntry.Key)
                            {
                                case "RadExposure":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.RadExposure;
                                    break;
                                case "Pain":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Pain;
                                    break;
                                case "Contusion":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Contusion;
                                    consumableEffect.fadeOut = (float)damageEntry.Value["fadeOut"];
                                    break;
                                case "Intoxication":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Intoxication;
                                    consumableEffect.fadeOut = (float)damageEntry.Value["fadeOut"];
                                    break;
                                case "LightBleeding":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.LightBleeding;
                                    consumableEffect.fadeOut = (float)damageEntry.Value["fadeOut"];
                                    break;
                                case "Fracture":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Fracture;
                                    consumableEffect.fadeOut = (float)damageEntry.Value["fadeOut"];
                                    break;
                                case "DestroyedPart":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.DestroyedPart;
                                    consumableEffect.healthPenaltyMax = (float)damageEntry.Value["healthPenaltyMax"] / 100;
                                    consumableEffect.healthPenaltyMin = (float)damageEntry.Value["healthPenaltyMin"] / 100;
                                    break;
                                case "HeavyBleeding":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.HeavyBleeding;
                                    consumableEffect.fadeOut = (float)damageEntry.Value["fadeOut"];
                                    break;
                            }
                        }
                    }

                    if (defaultItemsData["ItemDefaults"][i]["effects_health"] != null)
                    {
                        // Health effects
                        Dictionary<string, JToken> healthEffects = defaultItemsData["ItemDefaults"][i]["effects_health"].ToObject<Dictionary<string, JToken>>();
                        foreach (KeyValuePair<string, JToken> healthEntry in healthEffects)
                        {
                            EFM_Effect_Consumable consumableEffect = new EFM_Effect_Consumable();
                            customItemWrapper.consumeEffects.Add(consumableEffect);
                            consumableEffect.value = (float)healthEntry.Value["value"];
                            switch (healthEntry.Key)
                            {
                                case "Hydration":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Hydration;
                                    break;
                                case "Energy":
                                    consumableEffect.effectType = EFM_Effect_Consumable.EffectConsumable.Energy;
                                    break;
                            }
                        }
                    }

                    // Set buffs
                    customItemWrapper.effects = new List<EFM_Effect_Buff>();
                    if (defaultItemsData["ItemDefaults"][i]["stimulatorBuffs"] != null)
                    {
                        JArray buffs = (JArray)globalDB["config"]["Health"]["Effects"]["Stimulator"]["Buffs"][defaultItemsData["ItemDefaults"][i]["stimulatorBuffs"].ToString()];
                        foreach (JToken buff in buffs)
                        {
                            EFM_Effect_Buff currentBuff = new EFM_Effect_Buff();
                            currentBuff.effectType = (Effect.EffectType)Enum.Parse(typeof(Effect.EffectType), buff["BuffType"].ToString());
                            currentBuff.chance = (float)buff["Chance"];
                            currentBuff.delay = (float)buff["Delay"];
                            currentBuff.duration = (float)buff["Duration"];
                            currentBuff.value = (float)buff["Value"];
                            currentBuff.absolute = (bool)buff["AbsoluteValue"];
                            if (currentBuff.effectType == Effect.EffectType.SkillRate)
                            {
                                currentBuff.skillIndex = Mod.SkillNameToIndex(buff["SkillName"].ToString());
                            }
                            customItemWrapper.effects.Add(currentBuff);
                        }
                    }
                }

                // Key
                if (itemType == 11)
                {
                    // Make sure keys use the poseoverride when grabbing
                    itemPhysicalObject.UseGrabPointChild = false;

                    // Setup lock key script
                    LockKey lockKey = (LockKey)itemPhysicalObject;
                    lockKey.KeyTipPoint = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                }

                // Dogtag
                if (itemType == 17)
                {
                    customItemWrapper.USEC = (bool)defaultItemsData["ItemDefaults"][i]["USEC"];
                }
            }

            SetVanillaItems();

            // Add custom quick belt configs we made to GM's array of quick belt configurations
            List<GameObject> customQuickBeltConfigurations = new List<GameObject>();
            customQuickBeltConfigurations.AddRange(ManagerSingleton<GM>.Instance.QuickbeltConfigurations);
            firstCustomConfigIndex = customQuickBeltConfigurations.Count;
            customQuickBeltConfigurations.AddRange(rigConfigurations);
            ManagerSingleton<GM>.Instance.QuickbeltConfigurations = customQuickBeltConfigurations.ToArray();
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

        public static IEnumerator SetVanillaIcon(string ID, Image image)
        {
            // TODO: Maybe set a loading icon sprite by default to indicate to player 
            image.sprite = null;
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
            }
            FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
            image.sprite = physObj is FVRFireArmRound ? Mod.cartridgeIcon : IM.GetSpawnerID(physObj.ObjectWrapper.SpawnedFromId).Sprite;
        }

        private void SetFilterListsFor(MeatovItem customItemWrapper, int index)
        {
            customItemWrapper.whiteList = new List<string>();
            customItemWrapper.blackList = new List<string>();
            if (defaultItemsData["ItemDefaults"][index]["ContainerProperties"]["WhiteList"] != null)
            {
                foreach (JToken whiteListElement in defaultItemsData["ItemDefaults"][index]["ContainerProperties"]["WhiteList"])
                {
                    if (itemMap.ContainsKey(whiteListElement.ToString()))
                    {
                        ItemMapEntry entry = itemMap[whiteListElement.ToString()];
                        switch (entry.mode)
                        {
                            case 0:
                                customItemWrapper.whiteList.Add(entry.ID);
                                break;
                            case 1:
                                customItemWrapper.whiteList.AddRange(entry.modulIDs);
                                break;
                            case 2:
                                customItemWrapper.whiteList.Add(entry.otherModID);
                                break;
                        }
                    }
                    else
                    {
                        customItemWrapper.whiteList.Add(whiteListElement.ToString());
                    }
                }
            }
            if (defaultItemsData["ItemDefaults"][index]["ContainerProperties"]["BlackList"] != null)
            {
                foreach (JToken blackListElement in defaultItemsData["ItemDefaults"][index]["ContainerProperties"]["BlackList"])
                {
                    if (itemMap.ContainsKey(blackListElement.ToString()))
                    {
                        ItemMapEntry entry = itemMap[blackListElement.ToString()];
                        switch (entry.mode)
                        {
                            case 0:
                                customItemWrapper.blackList.Add(entry.ID);
                                break;
                            case 1:
                                customItemWrapper.blackList.AddRange(entry.modulIDs);
                                break;
                            case 2:
                                customItemWrapper.blackList.Add(entry.otherModID);
                                break;
                        }
                    }
                    else
                    {
                        customItemWrapper.blackList.Add(blackListElement.ToString());
                    }
                }
            }
        }

        private void LoadDB()
        {
            areasDB = JArray.Parse(File.ReadAllText(path + "/database/hideout/areas.json"));
            localeDB = JObject.Parse(File.ReadAllText(path + "/database/locales/global/en.json"));
            vanillaItemData = JObject.Parse(File.ReadAllText(path + "/database/DefaultItemData.json"));
            ParseItemMap();

            //if (Mod.itemsByParents.ContainsKey(parent))
            //{
            //    Mod.itemsByParents[parent].Add(H3ID);
            //}
            //else
            //{
            //    Mod.itemsByParents.Add(parent, new List<string>() { H3ID });
            //}

            //traderBaseDB = new JObject[8];
            //traderAssortDB = new JObject[8];
            //traderQuestAssortDB = new JObject[8];
            //traderCategoriesDB = new JArray[8];
            //for (int i = 0; i < 9; ++i)
            //{
            //    string traderID = TraderStatus.IndexToID(i);
            //    traderBaseDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/traders/" + traderID + "/base.json"));
            //    if(File.Exists(Mod.path + "/database/traders/" + traderID + "/assort.json"))
            //    {
            //        traderAssortDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/traders/" + traderID + "/assort.json"));
            //    }
            //    if(File.Exists(Mod.path + "/database/traders/" + traderID + "/questassort.json"))
            //    {
            //        traderQuestAssortDB[i] = JObject.Parse(File.ReadAllText(Mod.path + "/database/traders/" + traderID + "/questassort.json"));
            //    }

            //    // TODO: Review, we dont currently use the categories right now because I thought these were the categories of items we coudl sell
            //    // to the trader but apparently they are jsut UI stuff, IDs only used for UI locale
            //    // We need to find actual sell IDs or just keep using the current method we have of deciding whichi tems we can sell, which is
            //    // that we can only sell to them items of type of items they sell themselves, unless its fence, to whom we can sell anything at reduced price
            //    //traderCategoriesDB[i] = JArray.Parse(File.ReadAllText("BepInEx/Plugins/database/Traders/" + traderID + "/categories.json"));
            //}
            //globalDB = JObject.Parse(File.ReadAllText(Mod.path + "/database/globals.json"));
            //MovementManagerUpdatePatch.damagePerMeter = (float)Mod.globalDB["config"]["Health"]["Falling"]["DamagePerMeter"];
            //MovementManagerUpdatePatch.safeHeight = (float)Mod.globalDB["config"]["Health"]["Falling"]["SafeHeight"];
            //questDB = JObject.Parse(File.ReadAllText(Mod.path + "/database/templates/quests.json"));
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

            ////LoadSkillVars();
        }

        private void LoadSkillVars()
        {
            JToken skillsSettings = globalDB["config"]["SkillsSettings"];
            Skill.skillProgressRate = (float)skillsSettings["SkillProgressRate"];
            Skill.weaponSkillProgressRate = (float)skillsSettings["WeaponSkillProgressRate"];

            // HideoutManagement
            Skill.skillPointsPerAreaUpgrade = (float)skillsSettings["HideoutManagement"]["SkillPointsPerAreaUpgrade"];
            Skill.skillPointsPerCraft = (float)skillsSettings["HideoutManagement"]["SkillPointsPerCraft"];
            Skill.generatorPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["ResourceSpent"];
            Skill.AFUPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["ResourceSpent"];
            Skill.waterCollectorPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["ResourceSpent"];
            Skill.solarPowerPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["ResourceSpent"];
            Skill.consumptionReductionPerLevel = (float)skillsSettings["HideoutManagement"]["ConsumptionReductionPerLevel"];
            Skill.skillBoostPercent = (float)skillsSettings["HideoutManagement"]["SkillBoostPercent"];

            // Crafting
            Skill.pointsPerHourCrafting = ((float)skillsSettings["Crafting"]["PointsPerCraftingCycle"] + (float)skillsSettings["Crafting"]["PointsPerUniqueCraftCycle"]) / (float)skillsSettings["Crafting"]["CraftingCycleHours"];
            Skill.craftTimeReductionPerLevel = (float)skillsSettings["Crafting"]["CraftTimeReductionPerLevel"];
            Skill.productionTimeReductionPerLevel = (float)skillsSettings["Crafting"]["ProductionTimeReductionPerLevel"];
            Skill.eliteExtraProductions = (float)skillsSettings["Crafting"]["EliteExtraProductions"];

            // Metabolism
            Skill.hydrationRecoveryRate = (float)skillsSettings["Metabolism"]["HydrationRecoveryRate"];
            Skill.energyRecoveryRate = (float)skillsSettings["Metabolism"]["EnergyRecoveryRate"];
            Skill.increasePositiveEffectDurationRate = (float)skillsSettings["Metabolism"]["IncreasePositiveEffectDurationRate"];
            Skill.decreaseNegativeEffectDurationRate = (float)skillsSettings["Metabolism"]["DecreaseNegativeEffectDurationRate"];
            Skill.decreasePoisonDurationRate = (float)skillsSettings["Metabolism"]["DecreasePoisonDurationRate"];

            // Immunity
            Skill.immunityMiscEffects = (float)skillsSettings["Immunity"]["ImmunityMiscEffects"];
            Skill.immunityPoisonBuff = (float)skillsSettings["Immunity"]["ImmunityPoisonBuff"];
            Skill.immunityPainKiller = (float)skillsSettings["Immunity"]["ImmunityPainKiller"];
            Skill.healthNegativeEffect = (float)skillsSettings["Immunity"]["HealthNegativeEffect"];
            Skill.stimulatorNegativeBuff = (float)skillsSettings["Immunity"]["StimulatorNegativeBuff"];

            // Endurance
            Skill.movementAction = (float)skillsSettings["Endurance"]["MovementAction"];
            Skill.sprintAction = (float)skillsSettings["Endurance"]["SprintAction"];
            Skill.gainPerFatigueStack = (float)skillsSettings["Endurance"]["GainPerFatigueStack"];

            // Strength
            Skill.sprintActionMin = (float)skillsSettings["Strength"]["SprintActionMin"];
            Skill.sprintActionMax = (float)skillsSettings["Strength"]["SprintActionMax"];
            Skill.movementActionMin = (float)skillsSettings["Strength"]["MovementActionMin"];
            Skill.movementActionMax = (float)skillsSettings["Strength"]["MovementActionMax"];
            Skill.pushUpMin = (float)skillsSettings["Strength"]["PushUpMin"];
            Skill.pushUpMax = (float)skillsSettings["Strength"]["PushUpMax"];
            Skill.fistfightAction = (float)skillsSettings["Strength"]["FistfightAction"];
            Skill.throwAction = (float)skillsSettings["Strength"]["ThrowAction"];

            // Vitality
            Skill.damageTakenAction = (float)skillsSettings["Vitality"]["DamageTakenAction"];
            Skill.vitalityHealthNegativeEffect = (float)skillsSettings["Vitality"]["HealthNegativeEffect"];

            // Health
            Skill.skillProgress = (float)skillsSettings["Health"]["SkillProgress"];

            // StressResistance
            Skill.stressResistanceHealthNegativeEffect = (float)skillsSettings["StressResistance"]["HealthNegativeEffect"];
            Skill.lowHPDuration = (float)skillsSettings["StressResistance"]["LowHPDuration"];

            // Throwing
            Skill.throwingThrowAction = (float)skillsSettings["Throwing"]["ThrowAction"];

            // RecoilControl
            Skill.recoilAction = (float)skillsSettings["RecoilControl"]["RecoilAction"];
            Skill.recoilBonusPerLevel = (float)skillsSettings["RecoilControl"]["RecoilBonusPerLevel"];

            // Pistol
            Skill.pistolWeaponReloadAction = (float)skillsSettings["Pistol"]["WeaponReloadAction"];
            Skill.pistolWeaponShotAction = (float)skillsSettings["Pistol"]["WeaponShotAction"];
            Skill.pistolWeaponChamberAction = (float)skillsSettings["Pistol"]["WeaponChamberAction"];

            // Revolver, uses Pistol values
            Skill.revolverWeaponReloadAction = (float)skillsSettings["Pistol"]["WeaponReloadAction"];
            Skill.revolverWeaponShotAction = (float)skillsSettings["Pistol"]["WeaponShotAction"];
            Skill.revolverWeaponChamberAction = (float)skillsSettings["Pistol"]["WeaponChamberAction"];

            // SMG, uses assault values
            Skill.SMGWeaponReloadAction = (float)skillsSettings["Assault"]["WeaponReloadAction"];
            Skill.SMGWeaponShotAction = (float)skillsSettings["Assault"]["WeaponShotAction"];
            Skill.SMGWeaponChamberAction = (float)skillsSettings["Assault"]["WeaponChamberAction"];

            // Assault
            Skill.assaultWeaponReloadAction = (float)skillsSettings["Assault"]["WeaponReloadAction"];
            Skill.assaultWeaponShotAction = (float)skillsSettings["Assault"]["WeaponShotAction"];
            Skill.assaultWeaponChamberAction = (float)skillsSettings["Assault"]["WeaponChamberAction"];

            // Shotgun
            Skill.shotgunWeaponReloadAction = (float)skillsSettings["Shotgun"]["WeaponReloadAction"];
            Skill.shotgunWeaponShotAction = (float)skillsSettings["Shotgun"]["WeaponShotAction"];
            Skill.shotgunWeaponChamberAction = (float)skillsSettings["Shotgun"]["WeaponChamberAction"];

            // Sniper
            Skill.sniperWeaponReloadAction = (float)skillsSettings["Sniper"]["WeaponReloadAction"];
            Skill.sniperWeaponShotAction = (float)skillsSettings["Sniper"]["WeaponShotAction"];
            Skill.sniperWeaponChamberAction = (float)skillsSettings["Sniper"]["WeaponChamberAction"];

            // HMG, uses DMR values
            Skill.HMGWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            Skill.HMGWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            Skill.HMGWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // LMG, uses DMR values
            Skill.LMGWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            Skill.LMGWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            Skill.LMGWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // Launcher, uses DMR values
            Skill.launcherWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            Skill.launcherWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            Skill.launcherWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // AttachedLauncher, uses DMR values
            Skill.attachedLauncherWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            Skill.attachedLauncherWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            Skill.attachedLauncherWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // DMR
            Skill.DMRWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            Skill.DMRWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            Skill.DMRWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // CovertMovement
            Skill.covertMovementAction = (float)skillsSettings["CovertMovement"]["MovementAction"];

            // Search
            Skill.searchAction = (float)skillsSettings["Search"]["SearchAction"];
            Skill.findAction = (float)skillsSettings["Search"]["FindAction"];

            // MagDrills
            Skill.raidLoadedAmmoAction = (float)skillsSettings["MagDrills"]["RaidLoadedAmmoAction"];
            Skill.raidUnloadedAmmoAction = (float)skillsSettings["MagDrills"]["RaidUnloadedAmmoAction"];
            Skill.magazineCheckAction = (float)skillsSettings["MagDrills"]["MagazineCheckAction"];

            // Perception
            Skill.onlineAction = (float)skillsSettings["Perception"]["OnlineAction"];
            Skill.uniqueLoot = (float)skillsSettings["Perception"]["UniqueLoot"];

            // Intellect
            Skill.examineAction = (float)skillsSettings["Intellect"]["ExamineAction"];
            Skill.intellectSkillProgress = (float)skillsSettings["Intellect"]["SkillProgress"];

            // Attention
            Skill.examineWithInstruction = (float)skillsSettings["Attention"]["ExamineWithInstruction"];
            Skill.findActionFalse = (float)skillsSettings["Attention"]["FindActionFalse"];
            Skill.findActionTrue = (float)skillsSettings["Attention"]["FindActionTrue"];

            // Charisma
            Skill.skillProgressInt = (float)skillsSettings["Charisma"]["SkillProgressInt"];
            Skill.skillProgressAtn = (float)skillsSettings["Charisma"]["SkillProgressAtn"];
            Skill.skillProgressPer = (float)skillsSettings["Charisma"]["SkillProgressPer"];

            // Memory
            Skill.anySkillUp = (float)skillsSettings["Memory"]["AnySkillUp"];
            Skill.memorySkillProgress = (float)skillsSettings["Memory"]["SkillProgress"];

            // Surgery
            Skill.surgeryAction = (float)skillsSettings["Surgery"]["SurgeryAction"];
            Skill.surgerySkillProgress = (float)skillsSettings["Surgery"]["SkillProgress"];

            // AimDrills
            Skill.weaponShotAction = (float)skillsSettings["AimDrills"]["WeaponShotAction"];
        }

        private void LoadDefaultAssets()
        {
            defaultAssetsBundle = AssetBundle.LoadFromFile(Mod.path + "/Assets/EFMDefaultAssets.ab");
            mainMenuPointable = defaultAssetsBundle.LoadAsset<GameObject>("MainMenuPointable");

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
            itemIcons.Add("DingPack", assetsBundles[0].LoadAsset<Sprite>("ItemDingPack_Icon"));
            itemIcons.Add("FlipzoLighter", assetsBundles[0].LoadAsset<Sprite>("ItemFlipzoLighter_Icon"));
            itemIcons.Add("GarageTool_FlatheadScrewdriver", assetsBundles[0].LoadAsset<Sprite>("ItemGarageTool_FlatheadScrewdriver_Icon"));
            itemIcons.Add("GarageTool_WrenchTwoSided", assetsBundles[0].LoadAsset<Sprite>("ItemGarageTool_WrenchTwoSided_Icon"));
            itemIcons.Add("HairsprayCan", assetsBundles[0].LoadAsset<Sprite>("ItemHairsprayCan_Icon"));
            itemIcons.Add("Matchbox", assetsBundles[0].LoadAsset<Sprite>("ItemMatchbox_Icon"));
        }

        private static ItemRarity ItemRarityStringToEnum(string name)
        {
            switch (name)
            {
                case "Common":
                    return ItemRarity.Common;
                case "Rare":
                    return ItemRarity.Rare;
                case "Superrare":
                    return ItemRarity.Superrare;
                case "Not_exist":
                    return ItemRarity.Not_exist;
                default:
                    return ItemRarity.Not_exist;
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
                newEntry.modulIDs = item.Value["Modul"].ToObject<string[]>();
                newEntry.otherModID = item.Value["OtherMod"].ToString();

                bool gotModul = false;
                if(newEntry.modulIDs.Length > 0)
                {
                    bool missing = false;
                    foreach(string modulID in newEntry.modulIDs)
                    {
                        // TODO: Review if this is the correct way of checking if the items are installed
                        // Will have to check if the gun mods load their items in OD or somewhere else
                        if (!IM.OD.ContainsKey(modulID))
                        {
                            missing = true;
                            break;
                        }
                    }
                    if (!missing)
                    {
                        gotModul = true;
                        newEntry.mode = 1;
                    }
                }
                if(!gotModul && newEntry.otherModID != null && !newEntry.otherModID.Equals(""))
                {
                    if (IM.OD.ContainsKey(newEntry.otherModID))
                    {
                        newEntry.mode = 2;
                    }
                }

                itemMap.Add(item.Key, newEntry);
            }
        }

        private GameObject MakeItemInteractiveSet(Transform root, FVRPhysicalObject itemPhysicalObject)
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

        public static void UpdatePlayerInventory()
        {
            if (playerInventory == null)
            {
                playerInventory = new Dictionary<string, int>();
                playerInventoryObjects = new Dictionary<string, List<GameObject>>();
            }

            playerInventory.Clear();
            playerInventoryObjects.Clear();

            // Check equipment
            for(int i=0; i < StatusUI.instance.equipmentSlots.Length; ++i)
            {
                if (StatusUI.instance.equipmentSlots[i].CurObject != null)
                {
                    AddToPlayerInventory(StatusUI.instance.equipmentSlots[i].CurObject.transform, true);
                }
            }

            // Check quickbelt slots, only the first 4, the rest will be processed directly from the equipped rig while processing equipment above
            for (int i = 0; i < 4; ++i)
            {
                if (GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject != null)
                {
                    AddToPlayerInventory(GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.transform, true);
                }
            }

            // Check right shoulder slot, left not necessary because already processed with backpack while processing equipment above
            if (rightShoulderObject != null)
            {
                AddToPlayerInventory(rightShoulderObject.transform, true);
            }

            // Check hands
            if (leftHand.fvrHand.CurrentInteractable != null)
            {
                AddToPlayerInventory(leftHand.fvrHand.CurrentInteractable.transform, true);
            }
            if (rightHand.fvrHand.CurrentInteractable != null)
            {
                AddToPlayerInventory(rightHand.fvrHand.CurrentInteractable.transform, true);
            }
        }

        public static void AddToPlayerInventory(Transform item, bool updateTypeLists)
        {
            MeatovItem customItemWrapper = item.GetComponent<MeatovItem>();
            FVRPhysicalObject physObj = item.GetComponent<FVRPhysicalObject>();
            if (physObj == null || physObj.ObjectWrapper == null)
            {
                return; // Grenade pin for example, has no wrapper
            }
            string itemID = physObj.ObjectWrapper.ItemID;
            if (playerInventory.ContainsKey(itemID))
            {
                playerInventory[itemID] += customItemWrapper != null ? customItemWrapper.stack : 1;
                playerInventoryObjects[itemID].Add(item.gameObject);
            }
            else
            {
                playerInventory.Add(itemID, customItemWrapper != null ? customItemWrapper.stack : 1);
                playerInventoryObjects.Add(itemID, new List<GameObject> { item.gameObject });
            }

            if (updateTypeLists)
            {
                if (customItemWrapper != null)
                {
                    if (customItemWrapper.itemType == ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine boxMagazine = customItemWrapper.GetComponent<FVRFireArmMagazine>();
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
                    else if (customItemWrapper.physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
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
                    else if (customItemWrapper.physObj is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = customItemWrapper.physObj as FVRFireArmClip;
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
                    else if (customItemWrapper.physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = customItemWrapper.physObj as FVRFireArmRound;
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

            // Check for more items that may be contained inside this one
            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == ItemType.Backpack || customItemWrapper.itemType == ItemType.Container || customItemWrapper.itemType == ItemType.Pouch)
                {
                    foreach (Transform innerItem in customItemWrapper.containerItemRoot)
                    {
                        AddToPlayerInventory(innerItem, updateTypeLists);
                    }
                }
                else if (customItemWrapper.itemType == ItemType.Rig || customItemWrapper.itemType == ItemType.ArmoredRig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            AddToPlayerInventory(innerItem.transform, updateTypeLists);
                        }
                    }
                }
                else if (physObj is FVRFireArm)
                {
                    FVRFireArm asFireArm = (FVRFireArm)physObj;

                    // Ammo container
                    if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                    {
                        AddToPlayerInventory(asFireArm.Magazine.transform, updateTypeLists);
                    }
                    else if (asFireArm.UsesClips && asFireArm.Clip != null)
                    {
                        AddToPlayerInventory(asFireArm.Clip.transform, updateTypeLists);
                    }

                    // Attachments
                    if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                    {
                        foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                        {
                            AddToPlayerInventory(attachment.transform, updateTypeLists);
                        }
                    }
                }
                else if (physObj is FVRFireArmAttachment)
                {
                    FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)physObj;

                    if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
                    {
                        foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
                        {
                            AddToPlayerInventory(attachment.transform, updateTypeLists);
                        }
                    }
                }
            }
        }

        public static void RemoveFromPlayerInventory(Transform item, bool updateTypeLists)
        {
            MeatovItem customItemWrapper = item.GetComponent<MeatovItem>();
            FVRPhysicalObject physObj = item.GetComponent<FVRPhysicalObject>();
            if (physObj == null || physObj.ObjectWrapper == null)
            {
                return; // Grenade pin for example, has no wrapper
            }
            string itemID = physObj.ObjectWrapper.ItemID;
            if (playerInventory.ContainsKey(itemID))
            {
                playerInventory[itemID] -= customItemWrapper != null ? customItemWrapper.stack : 1;
                playerInventoryObjects[itemID].Remove(item.gameObject);
            }
            else
            {
                Mod.LogError("Attempting to remove " + itemID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (playerInventory[itemID] == 0)
            {
                playerInventory.Remove(itemID);
                playerInventoryObjects.Remove(itemID);
            }

            if (updateTypeLists)
            {
                if (customItemWrapper != null)
                {
                    if (customItemWrapper.itemType == ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine boxMagazine = customItemWrapper.GetComponent<FVRFireArmMagazine>();
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
                                    Mod.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo name was not found in roundsByType:\n" + Environment.StackTrace);
                                }
                            }
                            else
                            {
                                Mod.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo type was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                    }
                    else if (physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = physObj as FVRFireArmMagazine;
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
                                Mod.LogError("Attempting to remove " + itemID + "  which is mag from player inventory but its name was not found in magazinesByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is mag from player inventory but its type was not found in magazinesByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (physObj is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = physObj as FVRFireArmClip;
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
                                Mod.LogError("Attempting to remove " + itemID + "  which is clip from player inventory but its name was not found in clipsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is clip from player inventory but its type was not found in clipsByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = physObj as FVRFireArmRound;
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
                                Mod.LogError("Attempting to remove " + itemID + "  which is round from player inventory but its name was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is round from player inventory but its type was not found in roundsByType:\n" + Environment.StackTrace);
                        }
                    }
                }
            }

            // Check for more items that may be contained inside this one
            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == ItemType.Backpack || customItemWrapper.itemType == ItemType.Container || customItemWrapper.itemType == ItemType.Pouch)
                {
                    foreach (Transform innerItem in customItemWrapper.containerItemRoot)
                    {
                        RemoveFromPlayerInventory(innerItem, updateTypeLists);
                    }
                }
                else if (customItemWrapper.itemType == ItemType.Rig || customItemWrapper.itemType == ItemType.ArmoredRig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            RemoveFromPlayerInventory(innerItem.transform, updateTypeLists);
                        }
                    }
                }
                else if (physObj is FVRFireArm)
                {
                    FVRFireArm asFireArm = (FVRFireArm)physObj;

                    // Ammo container
                    if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                    {
                        RemoveFromPlayerInventory(asFireArm.Magazine.transform, updateTypeLists);
                    }
                    else if (asFireArm.UsesClips && asFireArm.Clip != null)
                    {
                        RemoveFromPlayerInventory(asFireArm.Clip.transform, updateTypeLists);
                    }

                    // Attachments
                    if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                    {
                        foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                        {
                            RemoveFromPlayerInventory(attachment.transform, updateTypeLists);
                        }
                    }
                }
                else if (physObj is FVRFireArmAttachment)
                {
                    FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)physObj;

                    if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
                    {
                        foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
                        {
                            RemoveFromPlayerInventory(attachment.transform, updateTypeLists);
                        }
                    }
                }
            }
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

            int preLevel = level;
            experience += xp;
            int XPForNextLevel = (int)XPPerLevel[level]["exp"]; // XP for current level would be at level - 1
            while (experience >= XPForNextLevel)
            {
                ++level;
                experience -= XPForNextLevel;
                XPForNextLevel = (int)XPPerLevel[level]["exp"];
            }

            // Update UI if necessary
            if (preLevel != level)
            {
                StatusUI.instance.UpdatePlayerLevel();
                if (currentLocationIndex == 1) // In hideout
                {
                    HideoutController.instance.UpdateBasedOnPlayerLevel();
                }

                // Update level task conditions
                if (taskStartConditionsByType.ContainsKey(TraderTaskCondition.ConditionType.Level))
                {
                    foreach (TraderTaskCondition condition in taskStartConditionsByType[TraderTaskCondition.ConditionType.Level])
                    {
                        TraderStatus.UpdateConditionFulfillment(condition);
                    }
                }
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

            if (postLevel != preLevel && Mod.taskSkillConditionsBySkillIndex.ContainsKey(skillIndex))
            {
                foreach (TraderTaskCondition condition in Mod.taskSkillConditionsBySkillIndex[skillIndex])
                {
                    TraderStatus.UpdateConditionFulfillment(condition);
                }
            }

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
            itemsByParents = new Dictionary<string, List<string>>();
            requiredForQuest = new Dictionary<string, int>();
            wishList = new List<string>();
            itemsByRarity = new Dictionary<ItemRarity, List<string>>();

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
                }
                else // Not loading into meatov scene
                {
                    // Unsecure scene components
                    foreach (GameObject go in securedMainSceneComponents)
                    {
                        SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
                    }
                    securedMainSceneComponents.Clear();
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
                            for(int i=0; i < assetsBundles.Length; ++i)
                            {
                                if (assetsBundles[i] != null)
                                {
                                    assetsBundles[i].Unload(true);
                                }
                            }
                            assetsBundles = null;
                        }

                        Mod.currentLocationIndex = -1;
                        inMeatovScene = false;
                        HideoutController.instance = null;
                        LoadMainMenu();
                        Mod.currentLocationIndex = -1;
                        break;
                    case "MeatovMainMenu":
                        //Mod.currentHideoutManager = null;
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

        public static int SkillNameToIndex(string name)
        {
            switch (name)
            {
                case "Endurance":
                    return 0;
                case "Strength":
                    return 1;
                case "Vitality":
                    return 2;
                case "Health":
                    return 3;
                case "StressResistance":
                    return 4;
                case "Metabolism":
                    return 5;
                case "Immunity":
                    return 6;
                case "Perception":
                    return 7;
                case "Intellect":
                    return 8;
                case "Attention":
                    return 9;
                case "Charisma":
                    return 10;
                case "Memory":
                    return 11;
                case "Pistols":
                    return 12;
                case "Revolvers":
                    return 13;
                case "SMG":
                    return 14;
                case "Assault":
                    return 15;
                case "Shotgun":
                    return 16;
                case "Sniper":
                    return 17;
                case "LMG":
                    return 18;
                case "HMG":
                    return 19;
                case "Launcher":
                    return 20;
                case "AttachedLauncher":
                    return 21;
                case "Throwing":
                    return 22;
                case "Melee":
                    return 23;
                case "DMR":
                    return 24;
                case "RecoilControl":
                    return 25;
                case "AimDrills":
                    return 26;
                case "Troubleshooting":
                    return 27;
                case "Surgery":
                    return 28;
                case "CovertMovement":
                    return 29;
                case "Search":
                    return 30;
                case "MagDrills":
                    return 31;
                case "Sniping":
                    return 32;
                case "ProneMovement":
                    return 33;
                case "FieldMedicine":
                    return 34;
                case "FirstAid":
                    return 35;
                case "LightVests":
                    return 36;
                case "HeavyVests":
                    return 37;
                case "WeaponModding":
                    return 38;
                case "AdvancedModding":
                    return 39;
                case "NightOps":
                    return 40;
                case "SilentOps":
                    return 41;
                case "Lockpicking":
                    return 42;
                case "WeaponTreatment":
                    return 43;
                case "FreeTrading":
                    return 44;
                case "Auctions":
                    return 45;
                case "Cleanoperations":
                    return 46;
                case "Barter":
                    return 47;
                case "Shadowconnections":
                    return 48;
                case "Taskperformance":
                    return 49;
                case "Crafting":
                    return 50;
                case "HideoutManagement":
                    return 51;
                case "WeaponSwitch":
                    return 52;
                case "EquipmentManagement":
                    return 53;
                case "AKSystems":
                    return 54;
                case "AssaultOperations":
                    return 55;
                case "Authority":
                    return 56;
                case "HeavyCaliber":
                    return 57;
                case "RawPower":
                    return 58;
                case "ARSystems":
                    return 59;
                case "DeepWeaponModding":
                    return 60;
                case "LongRangeOptics":
                    return 61;
                case "Negotiations":
                    return 62;
                case "Tactics":
                    return 63;
                default:
                    Mod.LogError("SkillNameToIndex received name: " + name);
                    return 0;
            }
        }

        public static string SkillIndexToName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Endurance";
                case 1:
                    return "Strength";
                case 2:
                    return "Vitality";
                case 3:
                    return "Health";
                case 4:
                    return "StressResistance";
                case 5:
                    return "Metabolism";
                case 6:
                    return "Immunity";
                case 7:
                    return "Perception";
                case 8:
                    return "Intellect";
                case 9:
                    return "Attention";
                case 10:
                    return "Charisma";
                case 11:
                    return "Memory";
                case 12:
                    return "Pistols";
                case 13:
                    return "Revolvers";
                case 14:
                    return "SMG";
                case 15:
                    return "Assault";
                case 16:
                    return "Shotgun";
                case 17:
                    return "Sniper";
                case 18:
                    return "LMG";
                case 19:
                    return "HMG";
                case 20:
                    return "Launcher";
                case 21:
                    return "AttachedLauncher";
                case 22:
                    return "Throwing";
                case 23:
                    return "Melee";
                case 24:
                    return "DMR";
                case 25:
                    return "RecoilControl";
                case 26:
                    return "AimDrills";
                case 27:
                    return "Troubleshooting";
                case 28:
                    return "Surgery";
                case 29:
                    return "CovertMovement";
                case 30:
                    return "Search";
                case 31:
                    return "MagDrills";
                case 32:
                    return "Sniping";
                case 33:
                    return "ProneMovement";
                case 34:
                    return "FieldMedicine";
                case 35:
                    return "FirstAid";
                case 36:
                    return "LightVests";
                case 37:
                    return "HeavyVests";
                case 38:
                    return "WeaponModding";
                case 39:
                    return "AdvancedModding";
                case 40:
                    return "NightOps";
                case 41:
                    return "SilentOps";
                case 42:
                    return "Lockpicking";
                case 43:
                    return "WeaponTreatment";
                case 44:
                    return "FreeTrading";
                case 45:
                    return "Auctions";
                case 46:
                    return "Cleanoperations";
                case 47:
                    return "Barter";
                case 48:
                    return "Shadowconnections";
                case 49:
                    return "Taskperformance";
                case 50:
                    return "Crafting";
                case 51:
                    return "HideoutManagement";
                case 52:
                    return "WeaponSwitch";
                case 53:
                    return "EquipmentManagement";
                case 54:
                    return "AKSystems";
                case 55:
                    return "AssaultOperations";
                case 56:
                    return "Authority";
                case 57:
                    return "HeavyCaliber";
                case 58:
                    return "RawPower";
                case 59:
                    return "ARSystems";
                case 60:
                    return "DeepWeaponModding";
                case 61:
                    return "LongRangeOptics";
                case 62:
                    return "Negotiations";
                case 63:
                    return "Tactics";
                default:
                    Mod.LogError("SkillIndexToName received index: " + index);
                    return "";
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

        public static float GetRaritySpawnChanceMultiplier(Mod.ItemRarity rarity)
        {
            switch (rarity)
            {
                case Mod.ItemRarity.Common:
                    return 0.85f;
                case Mod.ItemRarity.Rare:
                    return 0.3f;
                case Mod.ItemRarity.Superrare:
                    return 0.05f;
                case Mod.ItemRarity.Not_exist:
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
                // The specific item is specified as fitting or not, we dont need to check the list of ancestors
                //return !collidingContainerWrapper.blackList.Contains(IDToUse);

                // In truth, a specific ID in whitelist should mean that this item fits. The specific ID should never be specified in both white and black lists
                return true;
            }

            // Check ancestors
            for (int i = 0; i < parentsToUse.Count; ++i)
            {
                // If whitelist contains the ancestor ID
                if (whiteList.Contains(parentsToUse[i]))
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

            // Getting this far would mean that the item's ID nor any of its ancestors are in the whitelist, so doesn't fit
            return false;
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

        public static void H3MP_OnInstantiationTrack(GameObject go)
        {
            // This or one of our instantiation patches will happen first
            // In this case, we want to make sure that if the item is a MeatovItem, we add the script
            // before H3MP tracks it
            if(instantiatedItem == go)
            {
                // We already setup this item, no need to do it here
                // It is ready to be tracked by H3MP
                return;
            }
            else
            {
                // This item was caught by H3MP's instantiation patches abd must be setup now
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
    }

    public class ItemMapEntry
    {
        public int mode = 0; // 0 vanilla, 1 Modul, 2 other mod

        public string ID;
        public string[] modulIDs;
        public string otherModID;
    }

}
