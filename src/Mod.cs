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
using ModularWorkshop;
using System.Security.AccessControl;
using System.Linq;

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
        public static readonly string itemParentID = "54009119af1c881c07000029";
        public static readonly float headshotDamageMultiplier = 1.2f;
        public static readonly float handDamageResist = 0.5f;
        //public static readonly float[] sizeVolumes = { 1, 2, 5, 30, 0, 50}; // 0: Small, 1: Medium, 2: Large, 3: Massive, 4: None, 5: CantCarryBig
        public static int[] neededForPriorities = new int[] { 2, 3, 4, 0, 1 }; // Task, Area, Wishlist, Barter, Production
        public static Color[] neededForColors = new Color[] { Color.yellow, Color.red, Color.cyan, Color.magenta, Color.blue }; // Task, Area, Wishlist, Barter, Production
        public static Color neededForAreaFulfilledColor = Color.green;
        public static bool checkmarkFutureAreas = false;
        public static bool checkmarkAreaFulfillledMinimum = false;
        public static bool checkmarkFutureProductions = false;
        public static bool checkmarkFutureQuests = false;
        public static bool checkmarkFutureBarters = false;
        public static bool checkmarkShowBarter = false;
        public static bool checkmarkShowProduction = false;
        public static int[] rarityWeights = { 80, 15, 5, 0 }; // Common, Rare, Superrare, Not_Exist

        // Live data
        public static Mod modInstance;
        public static int currentLocationIndex = -1;  // 0: Player inventory, 1: Hideout, 2: Raid (Obvious game cannot be happening in player inventory, that is reserved for items)
        public static AssetBundle defaultBundle;
        public static AssetBundle mainMenuBundle;
        public static AssetBundle hideoutBundle;
        public static AssetBundleCreateRequest hideoutBundleRequest;
        public static AssetBundle hideoutAssetsBundle;
        public static AssetBundleCreateRequest hideoutAssetsBundleRequest;
        public static AssetBundle[] hideoutAreaBundles;
        public static AssetBundleCreateRequest[] hideoutAreaBundleRequests;
        public static AssetBundle itemIconsBundle;
        public static AssetBundleCreateRequest itemIconsBundleRequest;
        public static AssetBundle[] itemsBundles;
        public static AssetBundleCreateRequest[] itemsBundlesRequests;
        public static AssetBundle playerBundle;
        public static AssetBundleCreateRequest playerBundleRequest;
        public static AssetBundleCreateRequest raidMapBundleRequest;
        public static List<AssetBundleCreateRequest> raidMapAdditiveBundleRequests;
        public static List<AssetBundleCreateRequest> raidMapPrefabBundleRequests;
        public static List<GameObject> securedMainSceneComponents;
        public static List<GameObject> securedObjects; // Other objects that needed to be secured that do not require special handling
        public static MeatovItem[] securedItems; // Left hand, Right hand, Backpack, BodyArmor, EarPiece, HeadWear, FaceCover, EyeWear, Rig, Pouch, Right shoulder, Pockets
        public static List<MeatovItem> securedSlotItems;
        public static int saveSlotIndex = -1;
        public static int currentQuickBeltConfiguration = -1;
        public static int firstCustomConfigIndex = -1;
        public static Hand rightHand;
        public static Hand leftHand;
        public static List<List<RigSlot>> looseRigSlots;
        public static string mapChoiceName;
        public static bool charChoicePMC; // false is Scav
        public static bool timeChoiceIs0; // false is 1
        public static bool PMCSpawnTogether;
        public static HideoutController.FinishRaidState raidState;
        public static bool justFinishedRaid;
        public static Dictionary<string, int> killList;
        public static int lootingExp = 0;
        public static int healingExp = 0;
        public static int explorationExp = 0;
        public static int raidExp = 0;
        public static float raidTime = 0;
        public static bool loadingToMeatovScene;
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
        public static RaidManager currentRaidManager;
        public static Dictionary<string, int>[] requiredPerArea;
        public static List<MeatovItemData> wishList;
        public static Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>> ammoBoxesByRoundClassByRoundType; // Ammo boxes (key) and their round count (value), corresponding to round type and class
        public static Dictionary<MeatovItem.ItemRarity, List<MeatovItemData>> itemsByRarity;
        public static Dictionary<string, List<MeatovItemData>> itemsByParents;
        public static Trader[] traders; // Prapor, Therapist, Fence, Skier, Peacekeeper, Mechanic, Ragman, Jaeger, Lightkeeper
        public static Dictionary<int, List<Task>> tasksByTraderIndex;
        public static CategoryTreeNode itemCategories;
        public static bool amountChoiceUIUp;
        public static MeatovItem splittingItem;
        public static bool preventLoadMagUpdateLists; // Flag to prevent load mag patches to update lists before they are initialized
        public static int attachmentCheckNeeded;
        public static List<FVRInteractiveObject> physObjColResetList;
        public static int physObjColResetNeeded;
        public static List<List<bool>> triggeredExplorationTriggers;
        public static GameObject instantiatedItem;
        public static bool skipNextInstantiation;
        public static Dictionary<FVRInteractiveObject, MeatovItem> meatovItemByInteractive = new Dictionary<FVRInteractiveObject, MeatovItem>();
        public static Dictionary<string, Dictionary<string, byte>> noneModulParts; // Dict of "None" modul workshop parts by part ID by group ID
        public static string usedExtraction;
        public static RaidManager.RaidStatus raidStatus;

        // Player
        private static int _level = 1;
        public static int level 
        {
            get { return _level; }
            set 
            {
                int preValue = _level;
                _level = value; 
                if(preValue != _level)
                {
                    OnPlayerLevelChangedInvoke();
                }
            }
        }
        private static int _experience;
        public static int experience
        {
            get { return _experience; }
            set
            {
                int preValue = _experience;
                _experience = value;
                if (level >= XPPerLevel.Length)
                {
                    return;
                }
                while (_experience >= XPPerLevel[level])
                {
                    _experience -= XPPerLevel[level];
                    ++level;
                    preValue = -1; // To force call OnPlayerExperienceChangedInvoke

                    if (level >= XPPerLevel.Length)
                    {
                        break;
                    }
                }
                if (preValue != _experience)
                {
                    OnPlayerExperienceChangedInvoke();
                }
            }
        }
        public static bool dead;
        // Parts arrays: 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
        public static float[] defaultMaxHealth;
        private static float[] currentMaxHealth;
        private static float[] health; 
        // Base health rates are based on solid value addition (bleed, healing, etc.) from bonuses, effects, and skills
        // Note that health rates are split into positive and negative because we only want to apply health regen bonus
        // to positive part of health rate
        private static float[] basePositiveHealthRates;
        // Lethal healthrates will result in death if head or chest reach 0
        private static float[] baseNegativeHealthRates;
        // Note that non lethal healthrate is only ever negative
        // NonLethal healthrates will not result in death if head or chest reach 0, but will cause death if total health reaches 0
        private static float[] baseNonLethalHealthRates; 
        // Current health rates are base affected by percentages from bonuses, effects, and skills
        private static float[] currentHealthRates;
        private static float[] currentNonLethalHealthRates;
        private static float _energy;
        public static float energy
        {
            set
            {
                float preValue = _energy;
                _energy = value;
                if ((int)preValue != (int)_energy)
                {
                    OnEnergyChangedInvoke();
                }
            }
            get
            {
                return _energy;
            }
        }
        private static float _baseMaxEnergy;
        public static float baseMaxEnergy
        {
            get { return _baseMaxEnergy; }
            set
            {
                float preValue = _baseMaxEnergy;
                _baseMaxEnergy = value;
                if(preValue != _baseMaxEnergy)
                {
                    currentMaxEnergy = _baseMaxEnergy + Bonus.maximumEnergyReserve;
                }
            }
        }
        private static float _currentMaxEnergy;
        public static float currentMaxEnergy
        {
            get
            {
                return _currentMaxEnergy;
            }
            set
            {
                float preValue = _currentMaxEnergy;
                _currentMaxEnergy = value;
                if ((int)preValue != (int)_currentMaxEnergy)
                {
                    OnEnergyChangedInvoke();
                }
            }
        }
        private static float _hydration;
        public static float hydration
        {
            set 
            {
                float preValue = _hydration;
                _hydration = value;
                if((int)preValue != (int)_hydration)
                {
                    OnHydrationChangedInvoke();
                }
            }
            get
            {
                return _hydration;
            }
        }
        private static float _baseMaxHydration;
        public static float baseMaxHydration
        {
            get { return _baseMaxHydration; }
            set
            {
                float preValue = _baseMaxHydration;
                _baseMaxHydration = value;
                if (preValue != _baseMaxHydration)
                {
                    currentMaxHydration = _baseMaxHydration;
                }
            }
        }
        private static float _currentMaxHydration;
        public static float currentMaxHydration
        {
            get
            {
                return _currentMaxHydration;
            }
            set
            {
                float preValue = _currentMaxHydration;
                _currentMaxHydration = value;
                if ((int)preValue != (int)_currentMaxHydration)
                {
                    OnHydrationChangedInvoke();
                }
            }
        }
        public static readonly float raidEnergyRate = -3.2f; // TODO: Move this to RaidController and set it on LoadDB globals>config>Health>Effects>Existence
        public static readonly float raidHydrationRate = -2.6f; // TODO: Move this to RaidController and set it on LoadDB globals>config>Health>Effects>Existence
        private static float _baseEnergyRate;
        public static float baseEnergyRate // Affect by solid values (Effects)
        {
            set
            {
                float preValue = _baseEnergyRate;
                _baseEnergyRate = value;
                if(preValue != _baseEnergyRate)
                {
                    currentEnergyRate = baseEnergyRate + baseEnergyRate / 100 * Bonus.energyRegeneration;
                }
            }
            get
            {
                return _baseEnergyRate;
            }
        }
        private static float _currentEnergyRate;
        public static float currentEnergyRate // Base affected by percentage values (Bonus, Skill)
        {
            set
            {
                float preValue = _currentEnergyRate;
                _currentEnergyRate = value;
                if(preValue != _currentEnergyRate)
                {
                    OnEnergyRateChangedInvoke();
                }
            }
            get
            {
                return _currentEnergyRate;
            }
        }
        private static float _baseHydrationRate;
        public static float baseHydrationRate
        {
            set
            {
                float preValue = _baseHydrationRate;
                _baseHydrationRate = value;
                if (preValue != _baseHydrationRate)
                {
                    currentHydrationRate = baseHydrationRate + baseHydrationRate / 100 * Bonus.hydrationRegeneration;
                }
            }
            get
            {
                return _baseHydrationRate;
            }
        }
        private static float _currentHydrationRate;
        public static float currentHydrationRate
        {
            set
            {
                float preValue = _currentHydrationRate;
                _currentHydrationRate = value;
                if (preValue != _currentHydrationRate)
                {
                    OnHydrationRateChangedInvoke();
                }
            }
            get
            {
                return _currentEnergyRate;
            }
        }
        public static Dictionary<string, int> playerInventory;
        public static Dictionary<string, List<MeatovItem>> playerInventoryItems;
        public static Dictionary<string, int> playerFIRInventory;
        public static Dictionary<string, List<MeatovItem>> playerFIRInventoryItems;
        //public static List<InsuredSet> insuredItems;
        public static float staminaTimer = 0;
        public static float stamina = 100;
        private static float _baseMaxStamina; // Affected by solid amounts
        public static float baseMaxStamina
        {
            get { return _baseMaxStamina; }
            set
            {
                float preValue = _baseMaxStamina;
                _baseMaxStamina = value;
                if(preValue != _baseMaxStamina)
                {
                    currentMaxStamina = _baseMaxStamina;
                }
            }
        }
        public static float currentMaxStamina; // Based on baseMaxStamina, affected further by percentages
        public static Skill[] skills;
        public static float sprintStaminaDrain;
        public static float overweightStaminaDrain = 4f;
        public static float jumpStaminaDrain;
        private static float _baseStaminaRate; // Affected by solid amounts
        public static float baseStaminaRate
        {
            get { return _baseStaminaRate; }
            set
            {
                float preValue = _baseStaminaRate;
                _baseStaminaRate = value;
                if(preValue != _baseStaminaRate)
                {
                    currentStaminaRate = _baseStaminaRate;
                }
            }
        }
        public static float currentStaminaRate; // Based on staminaRate, affected further by percentages
        public static Effect dehydrationEffect;
        public static Effect fatigueEffect;
        public static Effect overweightFatigueEffect;
        public static ConsumeUI consumeUI;
        public static Text consumeUIText;
        public static StackSplitUI stackSplitUI;
        public static GameObject extractionUI;
        public static Text extractionUIText;
        public static GameObject extractionLimitUI;
        public static Text extractionLimitUIText;
        public static GameObject staminaBarUI;
        private static int _totalKillCount;
        public static int totalKillCount
        {
            set
            {
                int preValue = _totalKillCount;
                _totalKillCount = value;
                if(preValue != _totalKillCount)
                {
                    OnKillCountChangedInvoke();
                }
            }
            get
            {
                return _totalKillCount;
            }
        }
        private static int _totalDeathCount;
        public static int totalDeathCount
        {
            set
            {
                int preValue = _totalDeathCount;
                _totalDeathCount = value;
                if (preValue != _totalDeathCount)
                {
                    OnDeathCountChangedInvoke();
                }
            }
            get
            {
                return _totalDeathCount;
            }
        }
        private static int _totalRaidCount;
        public static int totalRaidCount
        {
            set
            {
                int preValue = _totalRaidCount;
                _totalRaidCount = value;
                if (preValue != _totalRaidCount)
                {
                    OnRaidCountChangedInvoke();
                }
            }
            get
            {
                return _totalRaidCount;
            }
        }
        private static int _runthroughRaidCount;
        public static int runthroughRaidCount
        {
            set
            {
                int preValue = _runthroughRaidCount;
                _runthroughRaidCount = value;
                if (preValue != _runthroughRaidCount)
                {
                    OnRunthroughRaidCountChangedInvoke();
                }
            }
            get
            {
                return _runthroughRaidCount;
            }
        }
        private static int _survivedRaidCount;
        public static int survivedRaidCount
        {
            set
            {
                int preValue = _survivedRaidCount;
                _survivedRaidCount = value;
                if (preValue != _survivedRaidCount)
                {
                    OnSurvivedRaidCountChangedInvoke();
                }
            }
            get
            {
                return _survivedRaidCount;
            }
        }
        private static int _MIARaidCount;
        public static int MIARaidCount
        {
            set
            {
                int preValue = _MIARaidCount;
                _MIARaidCount = value;
                if (preValue != _MIARaidCount)
                {
                    OnMIARaidCountChangedInvoke();
                }
            }
            get
            {
                return _MIARaidCount;
            }
        }
        private static int _KIARaidCount;
        public static int KIARaidCount
        {
            set
            {
                int preValue = _KIARaidCount;
                _KIARaidCount = value;
                if (preValue != _KIARaidCount)
                {
                    OnKIARaidCountChangedInvoke();
                }
            }
            get
            {
                return _KIARaidCount;
            }
        }
        public static int failedRaidCount;
        private static int _weight = 0; // Overencumbered at currentWeightLimit / 2, Critical overencumbered at currentWeightLimit
        public static int weight
        {
            set
            {
                int preValue = _weight;
                _weight = value;
                if(_weight != preValue)
                {
                    OnPlayerWeightChangedInvoke();
                }
            }
            get
            {
                return _weight;
            }
        }
        private static int _baseWeightLimit = 55000;
        public static int baseWeightLimit
        {
            set
            {
                int preValue = _baseWeightLimit;
                _baseWeightLimit = value;
                if (_baseWeightLimit != preValue)
                {
                    currentWeightLimit = baseWeightLimit;
                }
            }
            get
            {
                return _baseWeightLimit;
            }
        }
        private static int _currentWeightLimit = 55000;
        public static int currentWeightLimit
        {
            set
            {
                int preValue = _currentWeightLimit;
                _currentWeightLimit = value;
                if (_currentWeightLimit != preValue)
                {
                    OnPlayerWeightChangedInvoke();
                }
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
        public static Material highlightMaterial;
        public static Material quickSlotHoverMaterial;
        public static Material quickSlotConstantMaterial;
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
        public static Sprite questionMarkIcon;
        public static Sprite emptyCellIcon;

        // DB
        public static JArray areasDB;
        public static JArray productionsDB;
        public static JArray scavCaseProductionsDB;
        public static JObject hideoutsettingsDB;
        public static JObject localeDB;
        public static JObject itemDB;
        public static JObject[] traderBaseDB;
        public static JObject[] traderAssortDB;
        public static JObject[] traderQuestAssortDB;
        public static JObject globalDB;
        public static JObject questDB;
        public static int[] XPPerLevel;
        public static JObject[] locationsLootDB;
        public static JObject[] locationsBaseDB;
        public static JArray lootContainerDB;
        public static JObject dynamicLootTable;
        public static JObject staticLootTable;
        public static Dictionary<string, JToken> oldItemMap; // A map of what every item in tarkov points to in EFM, and a reason why not if it doesn't or if it is wrong
        public static Dictionary<string, MeatovItemData> defaultItemData; // All item data by tarkov ID
        public static Dictionary<string, List<MeatovItemData>> defaultItemDataByH3ID; // All item data by H3ID, note that this is a list, because multiple Tarkov IDs can point to the same H3ID
        public static MeatovItemData[] customItemData; // Custom item data by index
        public static Dictionary<string, List<MeatovItemData>> vanillaItemData; // Vanilla item data by H3ID
        public static Dictionary<string, Dictionary<string, List<MeatovItemData>>> modItemsByPartByGroup; // Item data by mod group and part. This is a list of data because multiple mod items may point to the same part
        public static Dictionary<FireArmRoundType, List<MeatovItemData>> roundDefaultItemDataByRoundType; // Round item data by round type
        public static Dictionary<FireArmMagazineType, List<MeatovItemData>> magDefaultItemDataByMagType; // Mag item data by mag type
        public static Dictionary<FireArmClipType, List<MeatovItemData>> clipDefaultItemDataByClipType; // Clip item data by clip type, And i realized after implementing this, there are no clips in tarkov
        public static Dictionary<string, JObject> lootContainersByName;
        public static Dictionary<string, AudioClip[]> itemSounds;
        public static Dictionary<string, string> availableRaidMaps = new Dictionary<string, string>();
        public static Dictionary<string, List<string>> availableRaidMapAdditives = new Dictionary<string, List<string>>();
        public static Dictionary<string, List<string>> availableRaidMapPrefabs = new Dictionary<string, List<string>>();
        public static Dictionary<string, Dictionary<string, int>> raidMapEntryRequirements = new Dictionary<string, Dictionary<string, int>>();
        public static Dictionary<string, JObject> botData;

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
        public delegate void OnPartHealthChangedDelegate(int index);
        public static event OnPartHealthChangedDelegate OnPartHealthChanged;
        public delegate void OnPartCurrentMaxHealthChangedDelegate(int index);
        public static event OnPartCurrentMaxHealthChangedDelegate OnPartCurrentMaxHealthChanged;
        public delegate void OnPlayerExperienceChangedDelegate();
        public static event OnPlayerExperienceChangedDelegate OnPlayerExperienceChanged;
        public delegate void OnHydrationChangedDelegate();
        public static event OnHydrationChangedDelegate OnHydrationChanged;
        public delegate void OnEnergyChangedDelegate();
        public static event OnEnergyChangedDelegate OnEnergyChanged;
        public delegate void OnHydrationRateChangedDelegate();
        public static event OnHydrationRateChangedDelegate OnHydrationRateChanged;
        public delegate void OnEnergyRateChangedDelegate();
        public static event OnEnergyRateChangedDelegate OnEnergyRateChanged;
        public delegate void OnCurrentHealthRateChangedDelegate(int index);
        public static event OnCurrentHealthRateChangedDelegate OnCurrentHealthRateChanged;
        public delegate void OnPlayerWeightChangedDelegate();
        public static event OnPlayerWeightChangedDelegate OnPlayerWeightChanged;
        public delegate void OnKillCountChangedDelegate();
        public static event OnKillCountChangedDelegate OnKillCountChanged;
        public delegate void OnDeathCountChangedDelegate();
        public static event OnDeathCountChangedDelegate OnDeathCountChanged;
        public delegate void OnRaidCountChangedDelegate();
        public static event OnRaidCountChangedDelegate OnRaidCountChanged;
        public delegate void OnRunthroughRaidCountChangedDelegate();
        public static event OnRunthroughRaidCountChangedDelegate OnRunthroughRaidCountChanged;
        public delegate void OnSurvivedRaidCountChangedDelegate();
        public static event OnSurvivedRaidCountChangedDelegate OnSurvivedRaidCountChanged;
        public delegate void OnMIARaidCountChangedDelegate();
        public static event OnMIARaidCountChangedDelegate OnMIARaidCountChanged;
        public delegate void OnKIARaidCountChangedDelegate();
        public static event OnKIARaidCountChangedDelegate OnKIARaidCountChanged;
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
        public delegate void OnPlayerItemInventoryChangedDelegate(MeatovItemData itemData, int difference);
        public static event OnPlayerItemInventoryChangedDelegate OnPlayerItemInventoryChanged;

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

            // Sub to H3MP events
            H3MP.Mod.OnConnection += Networking.OnConnection;
            H3MP.Networking.Client.OnDisconnect += Networking.OnDisconnection;
            H3MP.Networking.Server.OnServerClose += Networking.OnDisconnection;

            // Register tracked types
            TODO e: // Add our own data to H3MP's modular parts
            if (!H3MP.Mod.trackedObjectTypesByName.ContainsKey("TrackedDoorData"))
            {
                H3MP.Mod.modInstance.AddTrackedType(typeof(TrackedDoorData));
            }
            if (!H3MP.Mod.trackedObjectTypesByName.ContainsKey("TrackedLCCoverData"))
            {
                H3MP.Mod.modInstance.AddTrackedType(typeof(TrackedLCCoverData));
            }
            if (!H3MP.Mod.trackedObjectTypesByName.ContainsKey("TrackedLCSliderData"))
            {
                H3MP.Mod.modInstance.AddTrackedType(typeof(TrackedLCSliderData));
            }
            if (!H3MP.Mod.trackedObjectTypesByName.ContainsKey("TrackedLootContainerData"))
            {
                H3MP.Mod.modInstance.AddTrackedType(typeof(TrackedLootContainerData));
            }
            if (!H3MP.Mod.trackedObjectTypesByName.ContainsKey("TrackedMeatovItemData"))
            {
                H3MP.Mod.modInstance.AddTrackedType(typeof(TrackedMeatovItemData));
            }

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
                                Mod.mapChoiceName = "RaidDev";
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
                                List<string> lines = new List<string>();
                                foreach (KeyValuePair<string, FVRObject> entry in IM.OD)
                                {
                                    lines.Add(entry.Key);
                                }
                                File.WriteAllLines(path + "/database/itemiddump.txt", lines.ToArray());
                                break;
                            case 18: // Dump spawner IDs
                                Mod.LogInfo("\tDebug: Dumping spawner IDs");
                                foreach (KeyValuePair<string, ItemSpawnerID> entry in IM.Instance.SpawnerIDDic)
                                {
                                    Mod.LogInfo(entry.Key);
                                }
                                break;
                            case 19: // Dump modul parts
                                Mod.LogInfo("\tDebug: Dumping modul parts");
                                foreach (KeyValuePair<string, ModularWorkshopPartsDefinition> entry in ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary)
                                {
                                    Mod.LogInfo(entry.Key);
                                    if (entry.Value != null)
                                    {
                                        try
                                        {
                                            Dictionary<string, GameObject> partsDict = entry.Value.PartsDictionary;
                                            foreach (KeyValuePair<string, GameObject> innerEntry in partsDict)
                                            {
                                                Mod.LogInfo("\t" + innerEntry.Key);
                                            }
                                        }
                                        catch { }
                                    }
                                }
                                break;
                            case 20: // Go to indoor range
                                Mod.LogInfo("\tDebug: Loading indoor range");
                                SteamVR_LoadLevel.Begin("IndoorRange", false, 0.5f, 0f, 0f, 0f, 1f);
                                break;
                            case 21: // Generate default item data
                                Mod.LogInfo("\tDebug: Generate default item data");

                                List<string> logLines = new List<string>();
                                Dictionary<string, JToken> itemMap = JObject.Parse(File.ReadAllText(path + "/database/ItemMap.json")).ToObject<Dictionary<string, JToken>>();
                                JObject oldItemData = JObject.Parse(File.ReadAllText(path + "/database/OldDefaultItemData.json"));
                                JObject previousNewItemData = null;
                                if(File.Exists(path + "/database/NewDefaultItemData.json"))
                                {
                                    previousNewItemData = JObject.Parse(File.ReadAllText(path + "/database/NewDefaultItemData.json"));
                                }
                                JObject locale = JObject.Parse(File.ReadAllText(path + "/database/locales/global/en.json"));
                                JArray marketDumpDB = JArray.Parse(File.ReadAllText(path + "/database/templates/tarkovMarketItemsDump.json"));
                                Dictionary<string, JToken> marketDump = new Dictionary<string, JToken>();
                                for(int i=0; i < marketDumpDB.Count; ++i)
                                {
                                    marketDump.Add(marketDumpDB[i]["_id"].ToString(), marketDumpDB[i]);
                                }
                                JObject itemDB = JObject.Parse(File.ReadAllText(path + "/database/templates/items.json"));
                                JObject pricesDB = JObject.Parse(File.ReadAllText(path + "/database/templates/prices.json"));

                                JArray oldCustomItemData = oldItemData["customItemData"] as JArray;
                                Dictionary<string, JToken> oldVanillaItemData = oldItemData["vanillaItemData"].ToObject<Dictionary<string, JToken>>();
                                Dictionary<string, Dictionary<string, JToken>> oldModItemData = oldItemData["modItemData"].ToObject<Dictionary<string, Dictionary<string, JToken>>>();

                                logLines.Add("Building old data dict by tarkov ID");
                                Mod.LogInfo("\tBuilding old data dict by tarkov ID");

                                Dictionary<string, JToken> oldItemDataByTarkovID = new Dictionary<string, JToken>();

                                for(int i=0; i < oldCustomItemData.Count; ++i)
                                {
                                    // Skip if don't have tarkov ID
                                    if(oldCustomItemData[i]["tarkovID"] == null)
                                    {
                                        continue;
                                    }

                                    // Check for duplicate
                                    if (oldItemDataByTarkovID.TryGetValue(oldCustomItemData[i]["tarkovID"].ToString(), out JToken alreadyContained))
                                    {
                                        Mod.LogError("Old custom item data for " + oldCustomItemData[i]["H3ID"].ToString()+" already has its tarkov ID ("+ oldCustomItemData[i]["tarkovID"].ToString() + ") claimed by "+ alreadyContained["H3ID"]);
                                        continue;
                                    }

                                    oldItemDataByTarkovID.Add(oldCustomItemData[i]["tarkovID"].ToString(), oldCustomItemData[i]);
                                }

                                foreach(KeyValuePair<string, JToken> oldVanillaEntry in oldVanillaItemData)
                                {
                                    // Check for duplicate
                                    if (oldItemDataByTarkovID.TryGetValue(oldVanillaEntry.Value["tarkovID"].ToString(), out JToken alreadyContained))
                                    {
                                        logLines.Add("ERROR: Old vanilla item data for " + oldVanillaEntry.Key + " already has its tarkov ID (" + oldVanillaEntry.Value["tarkovID"].ToString() + ") claimed by " + alreadyContained["H3ID"]);
                                        Mod.LogError("Old vanilla item data for " + oldVanillaEntry.Key + " already has its tarkov ID (" + oldVanillaEntry.Value["tarkovID"].ToString() + ") claimed by " + alreadyContained["H3ID"]);
                                        continue;
                                    }

                                    oldItemDataByTarkovID.Add(oldVanillaEntry.Value["tarkovID"].ToString(), oldVanillaEntry.Value);
                                }

                                foreach (KeyValuePair<string, Dictionary<string,JToken>> oldModOuterEntry in oldModItemData)
                                {
                                    foreach (KeyValuePair<string, JToken> oldModEntry in oldModOuterEntry.Value)
                                    {
                                        // Check for duplicate
                                        if (oldItemDataByTarkovID.TryGetValue(oldModEntry.Value["tarkovID"].ToString(), out JToken alreadyContained))
                                        {
                                            logLines.Add("ERROR: Old mod item data for " + oldModOuterEntry.Key + ":" + oldModEntry.Key + " already has its tarkov ID (" + oldModEntry.Value["tarkovID"].ToString() + ") claimed by " + alreadyContained["H3ID"]);
                                            Mod.LogError("Old mod item data for " + oldModOuterEntry.Key + ":"+ oldModEntry .Key+ " already has its tarkov ID (" + oldModEntry.Value["tarkovID"].ToString() + ") claimed by " + alreadyContained["H3ID"]);
                                            continue;
                                        }

                                        oldItemDataByTarkovID.Add(oldModEntry.Value["tarkovID"].ToString(), oldModEntry.Value);
                                    }
                                }

                                logLines.Add("Building new default item data");
                                Mod.LogInfo("\tBuilding new default item data");

                                JObject defaultItemData = new JObject();
                                if(previousNewItemData != null)
                                {
                                    defaultItemData = previousNewItemData;
                                }
                                Dictionary<string, List<string>> IDDict = new Dictionary<string, List<string>>();

                                // Rewrite old data if necessary
                                if(previousNewItemData == null)
                                {
                                    foreach (KeyValuePair<string, JToken> oldEntry in oldItemDataByTarkovID)
                                    {
                                        Mod.LogInfo("\t\tRewriting old: " + oldEntry.Key);
                                        // If 868 (ModulWorkshop part), must set data correctly
                                        if (oldEntry.Value["index"] != null && ((int)oldEntry.Value["index"]) == 868 && !oldEntry.Value["H3ID"].ToString().Equals("868"))
                                        {
                                            string[] IDSplit = oldEntry.Value["H3ID"].ToString().Split(':');
                                            oldEntry.Value["modulGroup"] = IDSplit[1];
                                            oldEntry.Value["modulPart"] = IDSplit[2];
                                            oldEntry.Value["H3ID"] = "868";
                                        }
                                        else
                                        {
                                            if (IDDict.TryGetValue(oldEntry.Value["H3ID"].ToString(), out List<string> IDList))
                                            {
                                                IDList.Add(oldEntry.Key);
                                            }
                                            else
                                            {
                                                IDDict.Add(oldEntry.Value["H3ID"].ToString(), new List<string>() { oldEntry.Key });
                                            }
                                        }

                                        defaultItemData[oldEntry.Key] = oldEntry.Value;
                                    }
                                }

                                // Write new data
                                int newIteration = 0;
                                int tempIt = 0;
                                foreach(KeyValuePair<string, JToken> itemMapEntry in itemMap)
                                {
                                    ++tempIt;
                                    if (newIteration >= 500)
                                    {
                                        break;
                                    }

                                    Mod.LogInfo("\t\tWriting new: " + itemMapEntry.Key + ": " + tempIt + "/" + itemMap.Count);

                                    // Skip any entries we already wrote a new default item data entry for
                                    if (previousNewItemData != null && previousNewItemData[itemMapEntry.Key] != null)
                                    {
                                        Mod.LogInfo("\t\t\tSkipped, Already written");
                                        continue;
                                    }
                                    ++newIteration;
                                    // Skip any that don't have an H3ID
                                    if (itemMapEntry.Value["H3ID"] == null)
                                    {
                                        Mod.LogInfo("\t\t\tSkipped, no ID");
                                        continue;
                                    }

                                    // Custom item data is written manually when I create the asset
                                    // Non custom items can only be weapon, mod, or generic

                                    // Only process non custom items, or 868
                                    int parsedID = -1;
                                    string H3ID = itemMapEntry.Value["H3ID"].ToString();

                                    Mod.LogInfo("\t\t\tH3ID: " + H3ID);

                                    if (!int.TryParse(H3ID, out parsedID) || parsedID == 868)
                                    {
                                        if (IDDict.TryGetValue(H3ID, out List<string> IDList))
                                        {
                                            IDList.Add(itemMapEntry.Key);
                                        }
                                        else
                                        {
                                            IDDict.Add(H3ID, new List<string>() { itemMapEntry.Key });
                                        }

                                        Mod.LogInfo("\t\t\tNot custom");
                                        GameObject prefab = null;
                                        if(parsedID == 868)
                                        {
                                            prefab = GetItemPrefab(868);
                                        }
                                        else if(IM.OD.TryGetValue(H3ID, out FVRObject prefabObject))
                                        {
                                            prefab = prefabObject.GetGameObject();
                                        }
                                        else
                                        {
                                            logLines.Add("ERROR: Could not get prefab for "+ H3ID+":"+ itemMapEntry.Key);
                                            Mod.LogError("Could not get prefab for " + H3ID + ":" + itemMapEntry.Key);
                                            continue;
                                        }
                                        Mod.LogInfo("\t\t\tGot prefab");

                                        GameObject prefabInstance = Instantiate(prefab);

                                        FVRPhysicalObject physObj = prefabInstance.GetComponent<FVRPhysicalObject>();

                                        JObject newItemData = new JObject();

                                        Mod.LogInfo("\t\t\tWriting data");
                                        newItemData["tarkovID"] = itemMapEntry.Key;
                                        newItemData["H3ID"] = H3ID;
                                        if(physObj.IDSpawnedFrom == null)
                                        {
                                            if(physObj.ObjectWrapper == null)
                                            {
                                                if (IM.HasSpawnedID(H3ID))
                                                {
                                                    newItemData["H3SpawnerID"] = IM.GetSpawnerID(H3ID).ItemID;
                                                }
                                                else
                                                {
                                                    logLines.Add("ERROR: Could not get spawner ID for " + H3ID + ":" + itemMapEntry.Key);
                                                    Mod.LogError("Could not get spawner ID for " + H3ID + ":" + itemMapEntry.Key);

                                                    newItemData["H3SpawnerID"] = null;
                                                }
                                            }
                                            else
                                            {
                                                if(physObj.ObjectWrapper.SpawnedFromId == null || physObj.ObjectWrapper.SpawnedFromId.Equals(""))
                                                {
                                                    if (IM.HasSpawnedID(H3ID))
                                                    {
                                                        newItemData["H3SpawnerID"] = IM.GetSpawnerID(H3ID).ItemID;
                                                    }
                                                    else
                                                    {
                                                        logLines.Add("ERROR: Could not get spawner ID for " + H3ID + ":" + itemMapEntry.Key);
                                                        Mod.LogError("Could not get spawner ID for " + H3ID + ":" + itemMapEntry.Key);

                                                        newItemData["H3SpawnerID"] = null;
                                                    }
                                                }
                                                else
                                                {
                                                    newItemData["H3SpawnerID"] = physObj.ObjectWrapper.SpawnedFromId;
                                                }
                                            }
                                        }
                                        else
                                        {
                                            newItemData["H3SpawnerID"] = physObj.IDSpawnedFrom.ItemID;
                                        }
                                        if(parsedID == 868)
                                        {
                                            newItemData["index"] = 868;
                                        }
                                        else
                                        {
                                            newItemData["index"] = -1;
                                        }
                                        if (locale[itemMapEntry.Key+" Name"] == null)
                                        {
                                            logLines.Add("WARNING: Could not get tarkov name from locale for "+ H3ID+":"+ itemMapEntry.Key);
                                            Mod.LogWarning("Could not get tarkov name from locale for " + H3ID + ":" + itemMapEntry.Key);

                                            if(physObj.ObjectWrapper == null)
                                            {
                                                newItemData["name"] = H3ID;
                                            }
                                            else
                                            {
                                                newItemData["name"] = physObj.ObjectWrapper.DisplayName;
                                            }
                                        }
                                        else
                                        {
                                            newItemData["name"] = locale[itemMapEntry.Key + " Name"];
                                        }
                                        if (locale[itemMapEntry.Key + " Description"] == null)
                                        {
                                            logLines.Add("WARNING: Could not get tarkov description from locale for " + H3ID + ":" + itemMapEntry.Key);
                                            Mod.LogWarning("Could not get tarkov description from locale for " + H3ID + ":" + itemMapEntry.Key);

                                            newItemData["description"] = H3ID;
                                        }
                                        else
                                        {
                                            newItemData["description"] = locale[itemMapEntry.Key + " Description"];
                                        }
                                        JArray parents = new JArray();
                                        List<string> parentList = new List<string>();
                                        if (itemDB.TryGetValue(itemMapEntry.Key, out JToken itemDBData))
                                        {
                                            Mod.LogInfo("\t\t\t\tGetting data from itemDB");
                                            newItemData["lootExperience"] = itemDBData["_props"]["LootExperience"];
                                            JToken currentParent = itemDBData;
                                            while (!currentParent["_parent"].ToString().Equals(""))
                                            {
                                                Mod.LogInfo("\t\t\t\t\tAdding parent "+ currentParent["_parent"].ToString());
                                                parents.Add(currentParent["_parent"].ToString());
                                                parentList.Add(currentParent["_parent"].ToString());
                                                currentParent = itemDB[currentParent["_parent"].ToString()];
                                            }
                                            newItemData["parents"] = parents;
                                            Mod.LogInfo("\t\t\t\t\tWrote parents");
                                            newItemData["weight"] = (int)(((float)itemDBData["_props"]["Weight"])*1000);
                                            newItemData["canSellOnRagfair"] = itemDBData["_props"]["CanSellOnRagfair"];
                                            if(itemDBData["_props"]["RecoilForceUp"] != null)
                                            {
                                                newItemData["recoilVertical"] = itemDBData["_props"]["RecoilForceUp"];
                                            }
                                            if(itemDBData["_props"]["RecoilForceBack"] != null)
                                            {
                                                newItemData["recoilHorizontal"] = itemDBData["_props"]["RecoilForceBack"];
                                            }
                                            if(itemDBData["_props"]["SightingRange"] != null)
                                            {
                                                newItemData["sightingRange"] = itemDBData["_props"]["SightingRange"];
                                            }
                                            if(itemDBData["_props"]["Ergonomics"] != null)
                                            {
                                                newItemData["ergonomicsModifier"] = itemDBData["_props"]["Ergonomics"];
                                            }
                                            if(itemDBData["_props"]["Recoil"] != null)
                                            {
                                                newItemData["recoilModifier"] = itemDBData["_props"]["Recoil"];
                                            }
                                        }
                                        else
                                        {
                                            logLines.Add("ERROR: Could not get item DB data for " + H3ID + ":" + itemMapEntry.Key);
                                            Mod.LogError("Could not get item DB data for " + H3ID + ":" + itemMapEntry.Key);

                                            newItemData["lootExperience"] = 20;
                                            parents.Add(Mod.itemParentID);
                                            parentList.Add(Mod.itemParentID);
                                            newItemData["parents"] = parents;
                                            newItemData["weight"] = 0;
                                            newItemData["canSellOnRagfair"] = false;
                                            newItemData["recoilVertical"] = 0;
                                            newItemData["recoilHorizontal"] = 0;
                                            newItemData["sightingRange"] = 0;
                                            newItemData["ergonomicsModifier"] = 0;
                                            newItemData["recoilModifier"] = 0;
                                        }
                                        Mod.LogInfo("\t\t\t\t\t0");
                                        if (parsedID == 868 || physObj is FVRFireArmAttachment)
                                        {
                                            newItemData["itemType"] = "Mod";
                                            newItemData["compatibilityValue"] = 0;

                                            Mod.LogInfo("\t\t\t\t\t1");
                                            if (physObj is AttachableFirearmPhysicalObject)
                                            {
                                                AttachableFirearmPhysicalObject asAF = physObj as AttachableFirearmPhysicalObject;
                                                newItemData["usesMags"] = asAF.FA.UsesMagazines;
                                                newItemData["usesAmmoContainers"] = asAF.FA.UsesMagazines || asAF.FA.UsesClips;
                                                newItemData["magType"] = asAF.FA.UsesMagazines ? asAF.FA.MagazineType.ToString() : FireArmMagazineType.mNone.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                                newItemData["roundType"] = asAF.FA.RoundType.ToString();

                                                if (parentList.Contains("55818b014bdc2ddc698b456b") || parentList.Contains("5447bedf4bdc2d87278b4568"))
                                                {
                                                    newItemData["weaponclass"] = MeatovItem.WeaponClass.AttachedLauncher.ToString();
                                                }
                                                else
                                                {
                                                    logLines.Add("ERROR: Weapon class for " + H3ID + ":" + itemMapEntry.Key + " was not found");
                                                    Mod.LogError("Weapon class for " + H3ID + ":" + itemMapEntry.Key + " was not found");

                                                    newItemData["weaponclass"] = "UNKNOWN";
                                                }
                                            }
                                            else
                                            {
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = false;
                                                newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                                newItemData["roundType"] = FireArmRoundType.a106_25mmR.ToString();
                                                newItemData["weaponclass"] = "None";
                                            }
                                        }
                                        else if(physObj is FVRFireArm)
                                        {
                                            Mod.LogInfo("\t\t\t\t\t2");
                                            newItemData["itemType"] = "Weapon";

                                            FVRFireArm asFireArm = physObj as FVRFireArm;
                                            newItemData["roundType"] = asFireArm.RoundType.ToString();
                                            if (asFireArm.UsesMagazines)
                                            {
                                                newItemData["compatibilityValue"] = 1;
                                                newItemData["usesMags"] = true;
                                                newItemData["usesAmmoContainers"] = true;
                                                newItemData["magType"] = asFireArm.MagazineType.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                            }
                                            else if (asFireArm.UsesClips)
                                            {
                                                newItemData["compatibilityValue"] = 1;
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = true;
                                                newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                                newItemData["clipType"] = asFireArm.ClipType.ToString();
                                            }
                                            else
                                            {
                                                newItemData["compatibilityValue"] = 2;
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = false;
                                                newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                            }

                                            if (parentList.Contains("5447b5cf4bdc2d65278b4567"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Pistol.ToString();
                                            }
                                            else if (parentList.Contains("617f1ef5e8b54b0998387733"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Revolver.ToString();
                                            }
                                            else if (parentList.Contains("5447b5e04bdc2d62278b4567"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.SMG.ToString();
                                            }
                                            else if (parentList.Contains("5447b5f14bdc2d61278b4567") || parentList.Contains("5447b5fc4bdc2d87278b4567"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Assault.ToString();
                                            }
                                            else if (parentList.Contains("5447b5fc4bdc2d87278b4567"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Shotgun.ToString();
                                            }
                                            else if (parentList.Contains("5447b6254bdc2dc3278b4568"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Sniper.ToString();
                                            }
                                            else if (parentList.Contains("5447bed64bdc2d97278b4568"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.LMG.ToString();
                                            }
                                            else if (parentList.Contains("55818b014bdc2ddc698b456b") || parentList.Contains("5447bedf4bdc2d87278b4568"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.Launcher.ToString();
                                            }
                                            else if (parentList.Contains("5447b6194bdc2d67278b4567"))
                                            {
                                                newItemData["weaponclass"] = MeatovItem.WeaponClass.DMR.ToString();
                                            }
                                            else
                                            {
                                                logLines.Add("ERROR: Weapon class for " + H3ID + ":" + itemMapEntry.Key + " was not found");
                                                Mod.LogError("Weapon class for " + H3ID + ":" + itemMapEntry.Key + " was not found");

                                                newItemData["weaponclass"] = "UNKNOWN";
                                            }
                                        }
                                        else if(physObj is FVRMeleeWeapon)
                                        {
                                            Mod.LogInfo("\t\t\t\t\t3");
                                            newItemData["itemType"] = "Weapon";
                                            newItemData["compatibilityValue"] = 0;
                                            newItemData["usesMags"] = false;
                                            newItemData["usesAmmoContainers"] = false;
                                            newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                            newItemData["clipType"] = FireArmClipType.None.ToString();
                                            newItemData["roundType"] = FireArmRoundType.a106_25mmR.ToString();
                                            newItemData["weaponclass"] = "None";
                                        }
                                        else
                                        {
                                            Mod.LogInfo("\t\t\t\t\t4");
                                            logLines.Add("WARNING: Item type for " + H3ID + ":" + itemMapEntry.Key+" was set to generic");
                                            Mod.LogWarning("Item type for " + H3ID + ":" + itemMapEntry.Key+" was set to generic");

                                            newItemData["itemType"] = "Generic";

                                            if(physObj is FVRFireArmMagazine)
                                            {
                                                newItemData["compatibilityValue"] = 2;
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = false;
                                                FVRFireArmMagazine asMag = physObj as FVRFireArmMagazine;
                                                newItemData["magType"] = asMag.MagazineType.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                                newItemData["roundType"] = asMag.RoundType.ToString();
                                            }
                                            else if(physObj is FVRFireArmClip)
                                            {
                                                newItemData["compatibilityValue"] = 2;
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = false;
                                                newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                                FVRFireArmClip asClip = physObj as FVRFireArmClip;
                                                newItemData["clipType"] = asClip.ClipType.ToString();
                                                newItemData["roundType"] = asClip.RoundType.ToString();
                                            }
                                            else
                                            {
                                                newItemData["compatibilityValue"] = 0;
                                                newItemData["usesMags"] = false;
                                                newItemData["usesAmmoContainers"] = false;
                                                newItemData["magType"] = FireArmMagazineType.mNone.ToString();
                                                newItemData["clipType"] = FireArmClipType.None.ToString();
                                                newItemData["roundType"] = FireArmRoundType.a106_25mmR.ToString();
                                            }

                                            newItemData["weaponclass"] = "None";
                                        }
                                        Mod.LogInfo("\t\t\t\t\t0");
                                        newItemData["blocksEarpiece"] = false;
                                        newItemData["blocksEyewear"] = false;
                                        newItemData["blocksFaceCover"] = false;
                                        newItemData["blocksHeadwear"] = false;
                                        newItemData["coverage"] = 0f;
                                        newItemData["damageResist"] = 0f;
                                        newItemData["maxArmor"] = 0f;
                                        newItemData["smallSlotCount"] = 0f;
                                        newItemData["mediumSlotCount"] = 0f;
                                        newItemData["maxVolume"] = 0f;
                                        newItemData["cartridge"] = FireArmRoundType.a106_25mmR.ToString();
                                        newItemData["roundClass"] = FireArmRoundClass.FMJ.ToString();
                                        newItemData["maxStack"] = 1;
                                        newItemData["maxAmount"] = 0;
                                        newItemData["useTime"] = 0f;
                                        newItemData["amountRate"] = -1f;
                                        newItemData["consumeEffects"] = new JArray();
                                        newItemData["buffEffects"] = new JArray();
                                        Mod.LogInfo("\t\t\t\t\t0");
                                        JArray colorArray = new JArray();
                                        if (marketDump.TryGetValue(itemMapEntry.Key, out JToken marketDumpData))
                                        {
                                            Mod.LogInfo("\t\t\t\t\t1");
                                            newItemData["rarity"] = marketDumpData["_props"]["Rarity"];
                                            newItemData["value"] = marketDumpData["_props"]["CreditsPrice"];
                                            if(parsedID == 868)
                                            {
                                                string colorString = marketDumpData["_props"]["BackgroundColor"].ToString();
                                                switch (colorString)
                                                {
                                                    case "default":
                                                    case "black":
                                                        colorArray.Add(0.1f);
                                                        colorArray.Add(0.1f);
                                                        colorArray.Add(0.1f);
                                                        break;
                                                    case "grey":
                                                        colorArray.Add(0.4f);
                                                        colorArray.Add(0.4f);
                                                        colorArray.Add(0.4f);
                                                        break;
                                                    case "violet":
                                                        colorArray.Add(0f);
                                                        colorArray.Add(0.612f);
                                                        colorArray.Add(1f);
                                                        break;
                                                    case "yellow":
                                                        colorArray.Add(0.725f);
                                                        colorArray.Add(0f);
                                                        colorArray.Add(1f);
                                                        break;
                                                    case "orange":
                                                        colorArray.Add(0.522f);
                                                        colorArray.Add(0f);
                                                        colorArray.Add(1f);
                                                        break;
                                                    case "blue":
                                                        colorArray.Add(0.169f);
                                                        colorArray.Add(0.255f);
                                                        colorArray.Add(0.67f);
                                                        break;
                                                    default:
                                                        logLines.Add("ERROR: Could not get color \"" + colorString + "\" for " + H3ID + ":" + itemMapEntry.Key);
                                                        Mod.LogError(" Could not get color \"" + colorString + "\" for " + H3ID + ":" + itemMapEntry.Key);
                                                        colorArray.Add(0.1f);
                                                        colorArray.Add(0.1f);
                                                        colorArray.Add(0.1f);
                                                        break;
                                                }
                                            }
                                            else
                                            {
                                                colorArray.Add(0.1f);
                                                colorArray.Add(0.1f);
                                                colorArray.Add(0.1f);
                                            }
                                        }
                                        else
                                        {
                                            Mod.LogInfo("\t\t\t\t\t2");
                                            newItemData["rarity"] = MeatovItem.ItemRarity.Common.ToString(); ;
                                            if (pricesDB[itemMapEntry.Key] == null)
                                            {
                                                logLines.Add("ERROR: Could not get value for " + H3ID + ":" + itemMapEntry.Key);
                                                Mod.LogError("Could not get value for " + H3ID + ":" + itemMapEntry.Key);

                                                newItemData["value"] = 10000;
                                            }
                                            else
                                            {
                                                newItemData["value"] = pricesDB[itemMapEntry.Key];
                                            }

                                            colorArray.Add(0.1f);
                                            colorArray.Add(0.1f);
                                            colorArray.Add(0.1f);
                                        }
                                        newItemData["color"] = colorArray;

                                        Mod.LogInfo("\t\t\t\t\t0");
                                        MeshFilter[] meshFilters = prefabInstance.GetComponentsInChildren<MeshFilter>();
                                        List<Vector3> vertices = new List<Vector3>();
                                        for(int i=0; i < meshFilters.Length; ++i)
                                        {
                                            // Only count volume of visible meshes
                                            MeshRenderer mr = meshFilters[i].GetComponent<MeshRenderer>();
                                            if (mr == null || !mr.enabled)
                                            {
                                                continue;
                                            }

                                            if (meshFilters[i].sharedMesh != null)
                                            {
                                                int vertexOffset = vertices.Count;
                                                vertices.AddRange(meshFilters[i].sharedMesh.vertices);
                                                for (int k = vertexOffset; k < vertices.Count; ++k)
                                                {
                                                    Vector3 vertex = vertices[k];

                                                    vertex = new Vector3(vertex.x * meshFilters[i].transform.localScale.x, vertex.y * meshFilters[i].transform.localScale.y, vertex.z * meshFilters[i].transform.localScale.z);
                                                    vertex = meshFilters[i].transform.localRotation * vertex;
                                                    vertex += meshFilters[i].transform.localPosition;

                                                    ApplyTransform(ref vertex, meshFilters[i].transform.parent);

                                                    vertices[k] = vertex;
                                                }
                                            }
                                        }
                                        ConvexHullCalculator convexCalc = new ConvexHullCalculator();
                                        List<Vector3> convexVertices = new List<Vector3>();
                                        List<int> convexTriangles = new List<int>();
                                        List<Vector3> convexNormals = new List<Vector3>();
                                        bool generatedHull = false;
                                        try
                                        {
                                            generatedHull = convexCalc.GenerateHull(vertices, true, ref convexVertices, ref convexTriangles, ref convexNormals);
                                        }
                                        catch { }

                                        Mod.LogInfo("\t\t\t\t\t0");
                                        JArray volumes = new JArray();
                                        if (generatedHull)
                                        {
                                            float calculatedVolume = VolumeOfMesh(convexVertices.ToArray(), convexTriangles.ToArray());
                                            Mod.LogInfo("Calculated volume: " + calculatedVolume);
                                            Mod.LogInfo("Calculated volume * 1000000: " + (calculatedVolume * 1000000));
                                            Mod.LogInfo("Calculated volume as int: " + (int)(calculatedVolume * 1000000));
                                            volumes.Add((int)(calculatedVolume * 1000000));
                                        }
                                        else
                                        {
                                            volumes.Add(1);
                                            logLines.Add("ERROR: Reached max hull iterations for " + H3ID + ":" + itemMapEntry.Key);
                                        }
                                        newItemData["volumes"] = volumes;

                                        Mod.LogInfo("\t\t\t\t\t0");
                                        JArray dimensions = new JArray();
                                        if (parsedID == 868)
                                        {
                                            if (itemMapEntry.Value["ModulGroup"] != null)
                                            {
                                                Mod.LogInfo("\t\t\t\tGot modul group");
                                                newItemData["modulGroup"] = itemMapEntry.Value["ModulGroup"].ToString();
                                                newItemData["modulPart"] = itemMapEntry.Value["ModulPart"].ToString();

                                                if (ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary.TryGetValue(newItemData["modulGroup"].ToString(), out ModularWorkshopPartsDefinition parts)
                                                    && parts.PartsDictionary.TryGetValue(newItemData["modulPart"].ToString(), out GameObject partPrefab))
                                                {
                                                    Mod.LogInfo("\t\t\t\t\tModul part prefab for " + newItemData["modulGroup"].ToString() + ":" + newItemData["modulPart"].ToString() + " -> " + (partPrefab == null ? "null" : partPrefab.name));
                                                    if (partPrefab == null)
                                                    {
                                                        Mod.LogError("Entry for part prefab in dicts but prefab null");
                                                    }
                                                    else
                                                    {
                                                        GameObject parentGameObject = new GameObject();
                                                        GameObject partInstance = Instantiate(partPrefab, parentGameObject.transform);

                                                        Mod.LogInfo("\t\t\t\t\t0");
                                                        MeshFilter[] meshfilters = parentGameObject.GetComponentsInChildren<MeshFilter>();

                                                        Mod.LogInfo("\t\t\t\t\t0");
                                                        Vector2 x = new Vector2(float.MaxValue, float.MinValue);
                                                        Vector2 y = new Vector2(float.MaxValue, float.MinValue);
                                                        Vector2 z = new Vector2(float.MaxValue, float.MinValue);
                                                        for (int j = 0; j < meshfilters.Length; ++j)
                                                        {
                                                            Vector3[] verts = meshfilters[j].mesh.vertices;
                                                            for (int i = 0; i < verts.Length; ++i)
                                                            {
                                                                if (verts[i].x < x.x)
                                                                {
                                                                    x.x = verts[i].x;
                                                                }
                                                                else if (verts[i].x > x.y)
                                                                {
                                                                    x.y = verts[i].x;
                                                                }
                                                                if (verts[i].y < y.x)
                                                                {
                                                                    y.x = verts[i].y;
                                                                }
                                                                else if (verts[i].y > y.y)
                                                                {
                                                                    y.y = verts[i].y;
                                                                }
                                                                if (verts[i].z < z.x)
                                                                {
                                                                    z.x = verts[i].z;
                                                                }
                                                                else if (verts[i].z > z.y)
                                                                {
                                                                    z.y = verts[i].z;
                                                                }
                                                            }
                                                        }

                                                        Mod.LogInfo("\t\t\t\t\t0");
                                                        dimensions.Add(x.y - x.x);
                                                        dimensions.Add(y.y - y.x);
                                                        dimensions.Add(z.y - z.x);

                                                        Destroy(parentGameObject);
                                                    }
                                                }
                                                else
                                                {
                                                    Mod.LogError("Could not get part " + itemMapEntry.Key + ":" + itemMapEntry.Value["ModulGroup"].ToString() + ":" + itemMapEntry.Value["ModulPart"].ToString() + " to set dimensions");
                                                    logLines.Add("ERROR: Could not get part " + itemMapEntry.Key + ":" + itemMapEntry.Value["ModulGroup"].ToString() + ":" + itemMapEntry.Value["ModulPart"].ToString() + " to set dimensions");
                                                }
                                            }
                                            else if (itemMapEntry.Value["Note"] != null)
                                            {
                                                string note = itemMapEntry.Value["Note"].ToString();
                                                int mapIndex = note.IndexOf("MW map");
                                                if (mapIndex != -1)
                                                {
                                                    string sub = note.Substring(mapIndex + 7);
                                                    string[] split = sub.Split(':');

                                                    newItemData["modulGroup"] = split[0];
                                                    newItemData["modulPart"] = split[1];

                                                    if (ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary.TryGetValue(split[0], out ModularWorkshopPartsDefinition parts)
                                                        && parts.PartsDictionary.TryGetValue(split[1], out GameObject partPrefab))
                                                    {
                                                        Mod.LogInfo("\t\t\t\t\tModul part prefab for " + split[0] + ":" + split[1] + " -> " + (partPrefab == null ? "null" : partPrefab.name));
                                                        if (partPrefab == null)
                                                        {
                                                            Mod.LogError("Entry for part prefab in dicts but prefab null");
                                                        }
                                                        else
                                                        {
                                                            GameObject parentGameObject = new GameObject();
                                                            GameObject partInstance = Instantiate(partPrefab, parentGameObject.transform);

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            MeshFilter[] meshfilters = parentGameObject.GetComponentsInChildren<MeshFilter>();

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            Vector2 x = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 y = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 z = new Vector2(float.MaxValue, float.MinValue);
                                                            for (int j = 0; j < meshfilters.Length; ++j)
                                                            {
                                                                Vector3[] verts = meshfilters[j].mesh.vertices;
                                                                for (int i = 0; i < verts.Length; ++i)
                                                                {
                                                                    if (verts[i].x < x.x)
                                                                    {
                                                                        x.x = verts[i].x;
                                                                    }
                                                                    else if (verts[i].x > x.y)
                                                                    {
                                                                        x.y = verts[i].x;
                                                                    }
                                                                    if (verts[i].y < y.x)
                                                                    {
                                                                        y.x = verts[i].y;
                                                                    }
                                                                    else if (verts[i].y > y.y)
                                                                    {
                                                                        y.y = verts[i].y;
                                                                    }
                                                                    if (verts[i].z < z.x)
                                                                    {
                                                                        z.x = verts[i].z;
                                                                    }
                                                                    else if (verts[i].z > z.y)
                                                                    {
                                                                        z.y = verts[i].z;
                                                                    }
                                                                }
                                                            }

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            dimensions.Add(x.y - x.x);
                                                            dimensions.Add(y.y - y.x);
                                                            dimensions.Add(z.y - z.x);

                                                            Destroy(parentGameObject);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Mod.LogError("Could not get part " + itemMapEntry.Key + ":" + split[0] + ":" + split[1] + " to set dimensions");
                                                        logLines.Add("ERROR: Could not get part " + itemMapEntry.Key + ":" + split[0] + ":" + split[1] + " to set dimensions");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                newItemData["modulGroup"] = null;
                                                newItemData["modulPart"] = null;

                                                dimensions.Add(0f);
                                                dimensions.Add(0f);
                                                dimensions.Add(0f);
                                            }
                                        }
                                        else
                                        {
                                            newItemData["modulGroup"] = null;
                                            newItemData["modulPart"] = null;

                                            dimensions.Add(0f);
                                            dimensions.Add(0f);
                                            dimensions.Add(0f);
                                        }
                                        newItemData["dimensions"] = dimensions;
                                        

                                        Mod.LogInfo("\t\t\t\t\t0");
                                        defaultItemData[itemMapEntry.Key] = newItemData;

                                        Destroy(prefabInstance);
                                    }
                                }

                                logLines.Add("INFO: Entries with more than 1 usage of an H3ID");
                                Mod.LogInfo("Entries with more than 1 usage of an H3ID");
                                foreach (KeyValuePair<string, List<string>> entry in IDDict)
                                {
                                    if (entry.Value.Count > 1)
                                    {
                                        logLines.Add("\t" + entry.Key);
                                        Mod.LogInfo("\t" + entry.Key);
                                        for (int i = 0; i < entry.Value.Count; ++i)
                                        {
                                            Mod.LogInfo("\t\t" + entry.Value[i]);
                                        }
                                    }
                                }

                                logLines.Add("INFO: Writing new default item file");
                                Mod.LogInfo("Writing new default item file");

                                File.WriteAllText(path + "/database/NewDefaultItemData.json", defaultItemData.ToString());
                                File.WriteAllLines(path + "/database/DefaultItemDataGenerationLog.txt", logLines.ToArray());

                                logLines.Add("INFO: New default item file written");
                                Mod.LogInfo("New default item file written");
                                break;
                            case 22:// Fix new default item data
                                Mod.LogInfo("\tDebug: Fix new default item data");
                                JObject newItemDataToFix = JObject.Parse(File.ReadAllText(path + "/database/NewDefaultItemData.json"));
                                Dictionary<string, JToken> newItemDataDict = newItemDataToFix.ToObject<Dictionary<string, JToken>>();
                                Dictionary<string, JToken> fixItemMap = JObject.Parse(File.ReadAllText(path + "/database/ItemMap.json")).ToObject<Dictionary<string, JToken>>();
                                foreach (KeyValuePair<string, JToken> entry in newItemDataDict)
                                {
                                    string H3ID = entry.Value["H3ID"].ToString();
                                    Mod.LogInfo("\t\tEntry: " + entry.Key + ":" + H3ID+", spawner ID: "+(entry.Value["H3SpawnerID"] == null ? "null" : entry.Value["H3SpawnerID"].ToString()));

                                    if (entry.Value["H3SpawnerID"] != null && entry.Value["H3SpawnerID"].Type == JTokenType.Null)
                                    {
                                        Mod.LogInfo("\t\t\tNo spawn ID");
                                        int parsed = 0;
                                        if (!int.TryParse(H3ID, out parsed))
                                        {
                                            Mod.LogInfo("\t\t\t\tNot custom");
                                            FVRPhysicalObject physObj = null;
                                            if (IM.OD.TryGetValue(H3ID, out FVRObject prefabObject))
                                            {
                                                Mod.LogInfo("\t\t\t\t\tGot prefab");
                                                GameObject prefab = prefabObject.GetGameObject();
                                                physObj = Instantiate(prefab).GetComponentInChildren<FVRPhysicalObject>();
                                                if (physObj == null)
                                                {
                                                    Mod.LogError("Could not get physobj to fix spawn ID on " + H3ID + ":" + entry.Key);
                                                }
                                                else
                                                {
                                                    Mod.LogInfo("\t\t\t\t\t\tGot physobj");
                                                    if (physObj.IDSpawnedFrom == null)
                                                    {
                                                        if (physObj.ObjectWrapper == null)
                                                        {
                                                            if (IM.HasSpawnedID(H3ID))
                                                            {
                                                                entry.Value["H3SpawnerID"] = IM.GetSpawnerID(H3ID).ItemID;
                                                            }
                                                            else
                                                            {
                                                                Mod.LogError("Could not get spawner ID for " + H3ID + ":" + entry.Key);

                                                                entry.Value["H3SpawnerID"] = null;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (physObj.ObjectWrapper.SpawnedFromId == null || physObj.ObjectWrapper.SpawnedFromId.Equals(""))
                                                            {
                                                                if (IM.HasSpawnedID(H3ID))
                                                                {
                                                                    entry.Value["H3SpawnerID"] = IM.GetSpawnerID(H3ID).ItemID;
                                                                }
                                                                else
                                                                {
                                                                    Mod.LogError("Could not get spawner ID for " + H3ID + ":" + entry.Key);

                                                                    entry.Value["H3SpawnerID"] = null;
                                                                }
                                                            }
                                                            else
                                                            {
                                                                entry.Value["H3SpawnerID"] = physObj.ObjectWrapper.SpawnedFromId;
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        entry.Value["H3SpawnerID"] = physObj.IDSpawnedFrom.ItemID;
                                                    }

                                                    Destroy(physObj.gameObject);
                                                }
                                            }
                                            else
                                            {
                                                Mod.LogError("Could not get prefab to fix spawn ID on " + H3ID + ":" + entry.Key);
                                            }
                                        }
                                    }

                                    if(fixItemMap.TryGetValue(entry.Key, out JToken itemMapEntry))
                                    {
                                        if (H3ID.Equals("868"))
                                        {
                                            Mod.LogInfo("\t\t\tIs 868");
                                            if (itemMapEntry["ModulGroup"] != null)
                                            {
                                                if (entry.Value["modulGroup"] == null)
                                                {
                                                    Mod.LogInfo("\t\t\t\tGot modul group");
                                                    entry.Value["modulGroup"] = itemMapEntry["ModulGroup"].ToString();
                                                    entry.Value["modulPart"] = itemMapEntry["ModulPart"].ToString();

                                                    if (ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary.TryGetValue(entry.Value["modulGroup"].ToString(), out ModularWorkshopPartsDefinition parts)
                                                        && parts.PartsDictionary.TryGetValue(entry.Value["modulPart"].ToString(), out GameObject partPrefab))
                                                    {
                                                        Mod.LogInfo("\t\t\t\t\tModul part prefab for " + entry.Value["modulGroup"].ToString() + ":" + entry.Value["modulPart"].ToString() + " -> " + (partPrefab == null ? "null" : partPrefab.name));
                                                        if (partPrefab == null)
                                                        {
                                                            Mod.LogError("Entry for part prefab in dicts but prefab null");
                                                        }
                                                        else
                                                        {
                                                            GameObject parentGameObject = new GameObject();
                                                            GameObject partInstance = Instantiate(partPrefab, parentGameObject.transform);

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            MeshFilter[] meshfilters = parentGameObject.GetComponentsInChildren<MeshFilter>();

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            Vector2 x = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 y = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 z = new Vector2(float.MaxValue, float.MinValue);
                                                            for (int j = 0; j < meshfilters.Length; ++j)
                                                            {
                                                                Vector3[] verts = meshfilters[j].mesh.vertices;
                                                                for (int i = 0; i < verts.Length; ++i)
                                                                {
                                                                    if (verts[i].x < x.x)
                                                                    {
                                                                        x.x = verts[i].x;
                                                                    }
                                                                    else if (verts[i].x > x.y)
                                                                    {
                                                                        x.y = verts[i].x;
                                                                    }
                                                                    if (verts[i].y < y.x)
                                                                    {
                                                                        y.x = verts[i].y;
                                                                    }
                                                                    else if (verts[i].y > y.y)
                                                                    {
                                                                        y.y = verts[i].y;
                                                                    }
                                                                    if (verts[i].z < z.x)
                                                                    {
                                                                        z.x = verts[i].z;
                                                                    }
                                                                    else if (verts[i].z > z.y)
                                                                    {
                                                                        z.y = verts[i].z;
                                                                    }
                                                                }
                                                            }

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            entry.Value["dimensions"][0] = x.y - x.x;
                                                            entry.Value["dimensions"][1] = y.y - y.x;
                                                            entry.Value["dimensions"][2] = z.y - z.x;

                                                            Destroy(parentGameObject);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Mod.LogError("Could not get part " + entry.Key + ":" + entry.Value["modulGroup"].ToString() + ":" + entry.Value["modulPart"].ToString() + " to fix dimensions");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                Mod.LogInfo("\t\t\t\tNo modul group ERROR not supposed to have 868 item map entry without modulGroup, only reason this could happen is that new data is wrong, remove this entry from new data and regenerate");
                                                entry.Value["modulGroup"] = null;
                                                entry.Value["modulPart"] = null;
                                            }
                                        }
                                        else if (itemMapEntry["Note"] != null)
                                        {
                                            if (entry.Value["modulGroup"] == null)
                                            {
                                                Mod.LogInfo("\t\t\tGot note");
                                                string note = itemMapEntry["Note"].ToString();
                                                int mapIndex = note.IndexOf("MW map");
                                                if (mapIndex != -1)
                                                {
                                                    Mod.LogInfo("\t\t\t\tHas mod mapping");
                                                    string sub = note.Substring(mapIndex + 7);
                                                    string[] split = sub.Split(':');

                                                    entry.Value["modulGroup"] = split[0];
                                                    entry.Value["modulPart"] = split[1];

                                                    if (ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary.TryGetValue(entry.Value["modulGroup"].ToString(), out ModularWorkshopPartsDefinition parts)
                                                        && parts.PartsDictionary.TryGetValue(entry.Value["modulPart"].ToString(), out GameObject partPrefab))
                                                    {
                                                        Mod.LogInfo("\t\t\t\t\tModul part prefab for " + entry.Value["modulGroup"].ToString() + ":" + entry.Value["modulPart"].ToString() + " -> " + (partPrefab == null ? "null" : partPrefab.name));
                                                        if (partPrefab == null)
                                                        {
                                                            Mod.LogError("Entry for part prefab in dicts but prefab null");
                                                        }
                                                        else
                                                        {
                                                            GameObject parentGameObject = new GameObject();
                                                            GameObject partInstance = Instantiate(partPrefab, parentGameObject.transform);

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            MeshFilter[] meshfilters = parentGameObject.GetComponentsInChildren<MeshFilter>();

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            Vector2 x = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 y = new Vector2(float.MaxValue, float.MinValue);
                                                            Vector2 z = new Vector2(float.MaxValue, float.MinValue);
                                                            for (int j = 0; j < meshfilters.Length; ++j)
                                                            {
                                                                Vector3[] verts = meshfilters[j].mesh.vertices;
                                                                for (int i = 0; i < verts.Length; ++i)
                                                                {
                                                                    if (verts[i].x < x.x)
                                                                    {
                                                                        x.x = verts[i].x;
                                                                    }
                                                                    else if (verts[i].x > x.y)
                                                                    {
                                                                        x.y = verts[i].x;
                                                                    }
                                                                    if (verts[i].y < y.x)
                                                                    {
                                                                        y.x = verts[i].y;
                                                                    }
                                                                    else if (verts[i].y > y.y)
                                                                    {
                                                                        y.y = verts[i].y;
                                                                    }
                                                                    if (verts[i].z < z.x)
                                                                    {
                                                                        z.x = verts[i].z;
                                                                    }
                                                                    else if (verts[i].z > z.y)
                                                                    {
                                                                        z.y = verts[i].z;
                                                                    }
                                                                }
                                                            }

                                                            Mod.LogInfo("\t\t\t\t\t0");
                                                            entry.Value["dimensions"][0] = x.y - x.x;
                                                            entry.Value["dimensions"][1] = y.y - y.x;
                                                            entry.Value["dimensions"][2] = z.y - z.x;

                                                            Destroy(parentGameObject);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Mod.LogError("Could not get part " + entry.Key + ":" + entry.Value["modulGroup"].ToString() + ":" + entry.Value["modulPart"].ToString() + " to fix dimensions");
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            Mod.LogInfo("\t\t\tNot mod item");
                                            entry.Value["modulGroup"] = null;
                                            entry.Value["modulPart"] = null;
                                        }
                                    }

                                    newItemDataToFix[entry.Key] = entry.Value;
                                }

                                File.WriteAllText(path + "/database/NewDefaultItemData.json", newItemDataToFix.ToString());
                                break;
                            case 23: // Fix white and black lists
                                Mod.LogInfo("\tDebug: Fix white and black lists");
                                JObject itemDataToFix = JObject.Parse(File.ReadAllText(path + "/database/DefaultItemData.json"));
                                Dictionary<string, JToken> itemDataDict = itemDataToFix.ToObject<Dictionary<string, JToken>>();
                                JObject itemsDB = JObject.Parse(File.ReadAllText(path + "/database/templates/items.json"));
                                foreach(KeyValuePair<string, JToken> item in itemDataDict)
                                {
                                    JToken itemDBData = itemsDB[item.Key];
                                    if(itemDBData == null)
                                    {
                                        Mod.LogError("Missing item DB data for "+item.Key);
                                        continue;
                                    }
                                    if (item.Value["whiteList"] != null)
                                    {
                                        if(itemDBData["_props"]["Grids"] == null || itemDBData["_props"]["Grids"][0]["_props"]["filters"][0]["Filter"] == null)
                                        {
                                            Mod.LogError("Item DB data for " + item.Key+" is missing grids or whitelist");
                                        }
                                        else
                                        {
                                            item.Value["whiteList"] = itemDBData["_props"]["Grids"][0]["_props"]["filters"][0]["Filter"];
                                        }
                                    }
                                    if (item.Value["blackList"] != null)
                                    {
                                        if (itemDBData["_props"]["Grids"] == null || itemDBData["_props"]["Grids"][0]["_props"]["filters"][0]["ExcludedFilter"] == null)
                                        {
                                            Mod.LogError("Item DB data for " + item.Key + " is missing grids or blacklist");
                                        }
                                        else
                                        {
                                            item.Value["blackList"] = itemDBData["_props"]["Grids"][0]["_props"]["filters"][0]["ExcludedFilter"];
                                        }
                                    }

                                    itemDataToFix[item.Key] = item.Value;
                                }

                                File.WriteAllText(path + "/database/DefaultItemData.json", itemDataToFix.ToString());
                                break;
                            case 24: // Get all implemented item IDs missing from IM.OD
                                Mod.LogInfo("\tDebug: Get all implemented item IDs missing from IM.OD");
                                foreach(KeyValuePair<string, MeatovItemData> implementedEntry in Mod.defaultItemData)
                                {
                                    if (!IM.OD.ContainsKey(implementedEntry.Value.H3ID))
                                    {
                                        Mod.LogError("\tMissing item: " + implementedEntry.Value.H3ID);
                                    }
                                }
                                break;
                        }
                    }
                }
            }
#endif
        }

        static void ApplyTransform(ref Vector3 v, Transform t)
        {
            if (t == null)
            {
                return;
            }

            v = new Vector3(v.x * t.localScale.x, v.y * t.localScale.y, v.z * t.localScale.z);
            v = t.localRotation * v;
            v += t.localPosition;

            ApplyTransform(ref v, t.parent);
        }

        public static float DebugCalcVolumeOfObject(GameObject prefabInstance)
        {
            Mod.LogInfo("DebugCalcVolumeOfObject: "+prefabInstance.name);
            MeshFilter[] meshFilters = prefabInstance.GetComponentsInChildren<MeshFilter>();
            List<Vector3> vertices = new List<Vector3>();
            for (int i = 0; i < meshFilters.Length; ++i)
            {
                if (meshFilters[i].sharedMesh != null)
                {
                    int vertexOffset = vertices.Count;
                    vertices.AddRange(meshFilters[i].sharedMesh.vertices);
                    for (int k = vertexOffset; k < vertices.Count; ++k)
                    {
                        Vector3 vertex = vertices[k];

                        vertex = new Vector3(vertex.x * meshFilters[i].transform.localScale.x, vertex.y * meshFilters[i].transform.localScale.y, vertex.z * meshFilters[i].transform.localScale.z);
                        vertex = meshFilters[i].transform.localRotation * vertex;
                        vertex += meshFilters[i].transform.localPosition;

                        ApplyTransform(ref vertex, meshFilters[i].transform.parent);

                        vertices[k] = vertex;
                    }
                }
            }
            ConvexHullCalculator convexCalc = new ConvexHullCalculator();
            List<Vector3> convexVertices = new List<Vector3>();
            List<int> convexTriangles = new List<int>();
            List<Vector3> convexNormals = new List<Vector3>();
            Mod.LogInfo("\tGenerating hull");
            convexCalc.GenerateHull(vertices, true, ref convexVertices, ref convexTriangles, ref convexNormals);

            float volume = VolumeOfMesh(convexVertices.ToArray(), convexTriangles.ToArray());
            Mod.LogInfo("\tVolume: "+ volume);

            return volume;
        }

        public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float v321 = p3.x * p2.y * p1.z;
            float v231 = p2.x * p3.y * p1.z;
            float v312 = p3.x * p1.y * p2.z;
            float v132 = p1.x * p3.y * p2.z;
            float v213 = p2.x * p1.y * p3.z;
            float v123 = p1.x * p2.y * p3.z;

            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

        public static float VolumeOfMesh(Vector3[] vertices, int[] triangles)
        {
            float volume = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 p1 = vertices[triangles[i + 0]];
                Vector3 p2 = vertices[triangles[i + 1]];
                Vector3 p3 = vertices[triangles[i + 2]];
                volume += SignedVolumeOfTriangle(p1, p2, p3);
            }
            return Mathf.Abs(volume);
        }

        public static float GetHealthCount()
        {
            return health.Length;
        }

        public static float GetHealth(int index)
        {
            return health[index];
        }

        public static float[] GetHealthArray()
        {
            return health;
        }

        public static void SetHealth(int index, float value)
        {
            float preValue = health[index];
            health[index] = value;
            if ((int)value != (int)preValue)
            {
                OnPartHealthChangedInvoke(index);
            }
        }

        public static void SetHealthArray(float[] value)
        {
            health = value;
            OnPartHealthChangedInvoke(-1);
        }

        public static void SetCurrentMaxHealthArray(float[] value)
        {
            currentMaxHealth = value;
            OnPartCurrentMaxHealthChangedInvoke(-1);
        }

        public static float GetCurrentMaxHealth(int index)
        {
            if(currentMaxHealth == null)
            {
                return -1;
            }

            return currentMaxHealth[index];
        }

        public static float[] GetCurrentMaxHealthArray()
        {
            return currentMaxHealth;
        }

        public static void SetCurrentMaxHealth(int index, float value)
        {
            if (currentMaxHealth == null)
            {
                return;
            }

            float preValue = currentMaxHealth[index];
            currentMaxHealth[index] = value;
            if (value != preValue)
            {
                OnPartCurrentMaxHealthChangedInvoke(index);
            }
        }

        public static float GetBasePositiveHealthRate(int index)
        {
            if (basePositiveHealthRates == null)
            {
                return 0;
            }

            return basePositiveHealthRates[index];
        }

        public static float GetBaseNegativeHealthRate(int index)
        {
            if (baseNegativeHealthRates == null)
            {
                return 0;
            }

            return baseNegativeHealthRates[index];
        }

        public static float GetCurrentHealthRate(int index)
        {
            if (currentHealthRates == null)
            {
                return 0;
            }

            return currentHealthRates[index];
        }

        public static float GetBaseNonLethalHealthRate(int index)
        {
            if (baseNonLethalHealthRates == null)
            {
                return 0;
            }

            return baseNonLethalHealthRates[index];
        }

        public static float GetCurrentNonLethalHealthRate(int index)
        {
            if (currentNonLethalHealthRates == null)
            {
                return 0;
            }

            return currentNonLethalHealthRates[index];
        }

        public static float[] GetBasePositiveHealthRateArray()
        {
            return basePositiveHealthRates;
        }

        public static float[] GetBaseNegativeHealthRateArray()
        {
            return baseNegativeHealthRates;
        }

        public static float[] GetBaseNonLethalHealthRateArray()
        {
            return baseNonLethalHealthRates;
        }

        public static float[] GetCurrentHealthRateArray()
        {
            return currentHealthRates;
        }

        public static float[] GetCurrentNonLethalHealthRateArray()
        {
            return currentNonLethalHealthRates;
        }

        public static void SetBasePositiveHealthRate(int index, float value)
        {
            if (basePositiveHealthRates == null)
            {
                return;
            }

            float preValue = basePositiveHealthRates[index];
            basePositiveHealthRates[index] = value;
            if (value != preValue)
            {
                // Recalculate current based on bonus
                SetCurrentHealthRate(index, GetBasePositiveHealthRate(index) + (GetBasePositiveHealthRate(index) / 100 * Bonus.healthRegeneration) + GetBaseNegativeHealthRate(index));
            }
        }

        public static void SetBaseNegativeHealthRate(int index, float value)
        {
            if (baseNegativeHealthRates == null)
            {
                return;
            }

            float preValue = baseNegativeHealthRates[index];
            baseNegativeHealthRates[index] = value;
            if (value != preValue)
            {
                // Recalculate current based on bonus
                SetCurrentHealthRate(index, GetBasePositiveHealthRate(index) + (GetBasePositiveHealthRate(index) / 100 * Bonus.healthRegeneration) + GetBaseNegativeHealthRate(index));
            }
        }

        public static void SetBaseNonLethalHealthRate(int index, float value)
        {
            if (baseNonLethalHealthRates == null)
            {
                return;
            }

            float preValue = baseNonLethalHealthRates[index];
            baseNonLethalHealthRates[index] = value;
            if (value != preValue)
            {
                SetCurrentNonLethalHealthRate(index, GetBaseNonLethalHealthRate(index));
            }
        }

        public static void SetCurrentHealthRate(int index, float value)
        {
            if (currentHealthRates == null)
            {
                return;
            }

            float preValue = currentHealthRates[index];
            currentHealthRates[index] = value;
            if (value != preValue)
            {
                OnCurrentHealthRateChangedInvoke(index);
            }
        }

        public static void SetCurrentNonLethalHealthRate(int index, float value)
        {
            if (currentNonLethalHealthRates == null)
            {
                return;
            }

            float preValue = currentNonLethalHealthRates[index];
            currentNonLethalHealthRates[index] = value;
            if (value != preValue)
            {
                OnCurrentHealthRateChangedInvoke(index);
            }
        }

        public static void SetBasePositiveHealthRateArray(float[] value)
        {
            for(int i = 0; i < GetHealthCount(); ++i)
            {
                SetBasePositiveHealthRate(i, value[i]);
            }
        }

        public static void SetBaseNegativeHealthRateArray(float[] value)
        {
            for(int i = 0; i < GetHealthCount(); ++i)
            {
                SetBaseNegativeHealthRate(i, value[i]);
            }
        }

        public static void SetBaseNonLethalHealthRateArray(float[] value)
        {
            for(int i = 0; i < GetHealthCount(); ++i)
            {
                SetBaseNonLethalHealthRate(i, value[i]);
            }
        }

        public static void SetCurrentHealthRateArray(float[] value)
        {
            for(int i = 0; i < GetHealthCount(); ++i)
            {
                SetCurrentHealthRate(i, value[i]);
            }
        }

        public static void SetCurrentNonLethalHealthRateArray(float[] value)
        {
            for(int i = 0; i < GetHealthCount(); ++i)
            {
                SetCurrentNonLethalHealthRate(i, value[i]);
            }
        }

        public static void OnPlayerLevelChangedInvoke()
        {
            if(OnPlayerLevelChanged != null)
            {
                OnPlayerLevelChanged();
            }
        }

        public static void OnPartHealthChangedInvoke(int index)
        {
            if(OnPartHealthChanged != null)
            {
                OnPartHealthChanged(index);
            }
        }

        public static void OnPartCurrentMaxHealthChangedInvoke(int index)
        {
            if(OnPartCurrentMaxHealthChanged != null)
            {
                OnPartCurrentMaxHealthChanged(index);
            }
        }

        public static void OnCurrentHealthRateChangedInvoke(int index)
        {
            if(OnCurrentHealthRateChanged != null)
            {
                OnCurrentHealthRateChanged(index);
            }
        }

        public static void OnPlayerWeightChangedInvoke()
        {
            if(OnPlayerWeightChanged != null)
            {
                OnPlayerWeightChanged();
            }
        }

        public static void OnPlayerExperienceChangedInvoke()
        {
            if(OnPlayerExperienceChanged != null)
            {
                OnPlayerExperienceChanged();
            }
        }

        public static void OnEnergyChangedInvoke()
        {
            if(OnEnergyChanged != null)
            {
                OnEnergyChanged();
            }
        }

        public static void OnHydrationChangedInvoke()
        {
            if(OnHydrationChanged != null)
            {
                OnHydrationChanged();
            }
        }

        public static void OnHydrationRateChangedInvoke()
        {
            if(OnHydrationRateChanged != null)
            {
                OnHydrationRateChanged();
            }
        }

        public static void OnEnergyRateChangedInvoke()
        {
            if(OnEnergyRateChanged != null)
            {
                OnEnergyRateChanged();
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

        public static void OnKillCountChangedInvoke()
        {
            if(OnKillCountChanged != null)
            {
                OnKillCountChanged();
            }
        }

        public static void OnDeathCountChangedInvoke()
        {
            if(OnDeathCountChanged != null)
            {
                OnDeathCountChanged();
            }
        }

        public static void OnRaidCountChangedInvoke()
        {
            if(OnRaidCountChanged != null)
            {
                OnRaidCountChanged();
            }
        }

        public static void OnRunthroughRaidCountChangedInvoke()
        {
            if(OnRunthroughRaidCountChanged != null)
            {
                OnRunthroughRaidCountChanged();
            }
        }

        public static void OnSurvivedRaidCountChangedInvoke()
        {
            if(OnSurvivedRaidCountChanged != null)
            {
                OnSurvivedRaidCountChanged();
            }
        }

        public static void OnMIARaidCountChangedInvoke()
        {
            if(OnMIARaidCountChanged != null)
            {
                OnMIARaidCountChanged();
            }
        }

        public static void OnKIARaidCountChangedInvoke()
        {
            if(OnKIARaidCountChanged != null)
            {
                OnKIARaidCountChanged();
            }
        }

        public static void OnPlayerItemInventoryChangedInvoke(MeatovItemData itemData, int difference)
        {
            if(OnPlayerItemInventoryChanged != null)
            {
                OnPlayerItemInventoryChanged(itemData, difference);
            }
            itemData.OnPlayerItemInventoryChangedInvoke(difference);
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

        public void DumpModulParts(IModularWeapon weapon)
        {
            Mod.LogInfo("Dumping modular parts");
            foreach (KeyValuePair<string, ModularWeaponPartsAttachmentPoint> point in weapon.AllAttachmentPoints)
            {
                Mod.LogInfo("\t"+point.Key+":"+point.Value.SelectedModularWeaponPart);
            }
        }

        private void LoadConfig()
        {
            LogInfo("Loading config...", false);
            config = JObject.Parse(File.ReadAllText(path+"/Config.json"));
            LogInfo("Config loaded", false);
        }

        private void LoadDB()
        {
            globalDB = JObject.Parse(File.ReadAllText(path + "/database/globals.json"));

            JToken globalFallingData = globalDB["config"]["Health"]["Falling"];
            StatusUI.damagePerMeter = (float)globalFallingData["DamagePerMeter"];
            StatusUI.safeHeight = (float)globalFallingData["SafeHeight"];

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
            HideoutController.defaultHealthRates[0] = ((float)globalRegens["BodyHealth"]["Head"]["Value"]) / 60;
            HideoutController.defaultHealthRates[1] = ((float)globalRegens["BodyHealth"]["Chest"]["Value"]) / 60;
            HideoutController.defaultHealthRates[2] = ((float)globalRegens["BodyHealth"]["Stomach"]["Value"]) / 60;
            HideoutController.defaultHealthRates[3] = ((float)globalRegens["BodyHealth"]["LeftArm"]["Value"]) / 60;
            HideoutController.defaultHealthRates[4] = ((float)globalRegens["BodyHealth"]["RightArm"]["Value"]) / 60;
            HideoutController.defaultHealthRates[5] = ((float)globalRegens["BodyHealth"]["LeftLeg"]["Value"]) / 60;
            HideoutController.defaultHealthRates[6] = ((float)globalRegens["BodyHealth"]["RightLeg"]["Value"]) / 60;
            HideoutController.defaultEnergyRate = ((float)globalRegens["Energy"]) / 60;
            HideoutController.defaultHydrationRate = ((float)globalRegens["Hydration"]) / 60;

            JToken globalHealthFactors = globalDB["config"]["Health"]["ProfileHealthSettings"]["HealthFactorsSettings"];
            baseMaxEnergy = (float)globalHealthFactors["Energy"]["Maximum"];
            baseMaxHydration = (float)globalHealthFactors["Hydration"]["Maximum"];

            JToken globalStaminaSettings = globalDB["config"]["Stamina"];
            baseMaxStamina = (float)globalStaminaSettings["Capacity"];
            baseStaminaRate = (float)globalStaminaSettings["BaseRestorationRate"];
            jumpStaminaDrain = (float)globalStaminaSettings["JumpConsumption"];
            sprintStaminaDrain = (float)globalStaminaSettings["SprintDrainRate"];

            skills = new Skill[64];
            for(int i=0; i < skills.Length; ++i)
            {
                skills[i] = new Skill();
                skills[i].index = i;
                skills[i].displayName = Skill.SkillIndexToDisplayName(i);
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
            Area.fuelConsumptionRate = (float)hideoutsettingsDB["generatorFuelFlowRate"];
            Area.filterConsumptionRate = (float)hideoutsettingsDB["airFilterUnitFlowRate"];
            localeDB = JObject.Parse(File.ReadAllText(path + "/database/locales/global/en.json"));
            itemDB = JObject.Parse(File.ReadAllText(path + "/database/templates/items.json"));
            ParseDefaultItemData();
            ParseNoneModulParts();
            LoadBotData();

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

            // Must be done after loading trader data because we need assort data and barters to be set
            BuildCategoriesTree();

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
            JArray xpTable = globalDB["config"]["exp"]["level"]["exp_table"] as JArray;
            XPPerLevel = new int[xpTable.Count];
            for(int i=0; i< xpTable.Count; ++i)
            {
                XPPerLevel[i] = (int)xpTable[i]["exp"];
            }
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

        public void LoadBotData()
        {
            botData = new Dictionary<string, JObject>();

            string[] typeFiles = Directory.GetFiles(path + "/database/bots/types");
            for(int i=0; i < typeFiles.Length; ++i)
            {
                botData.Add(Path.GetFileNameWithoutExtension(typeFiles[i]), JObject.Parse(File.ReadAllText(typeFiles[i])));
            }
        }

        public void ParseNoneModulParts()
        {
            noneModulParts = new Dictionary<string, Dictionary<string, byte>>();

            Dictionary<string, JToken> nonPartData = JObject.Parse(File.ReadAllText(path + "/database/NoneModulParts.json")).ToObject<Dictionary<string, JToken>>();
            foreach(KeyValuePair<string, JToken> entry in nonPartData)
            {
                noneModulParts.Add(entry.Key, new Dictionary<string, byte>() { { entry.Value.ToString(), 0 } });
            }
        }

        private void ParseDefaultItemData()
        {
            oldItemMap = JObject.Parse(File.ReadAllText(path + "/database/ItemMap.json")).ToObject<Dictionary<string, JToken>>();
            JObject data = JObject.Parse(File.ReadAllText(path + "/database/DefaultItemData.json"));
            Dictionary<string, JToken> defaultItemDataDB = data.ToObject<Dictionary<string, JToken>>();
            defaultItemData = new Dictionary<string, MeatovItemData>();
            defaultItemDataByH3ID = new Dictionary<string, List<MeatovItemData>>();
            roundDefaultItemDataByRoundType = new Dictionary<FireArmRoundType, List<MeatovItemData>>();
            magDefaultItemDataByMagType = new Dictionary<FireArmMagazineType, List<MeatovItemData>>();
            clipDefaultItemDataByClipType = new Dictionary<FireArmClipType, List<MeatovItemData>>();

            modItemsByPartByGroup = new Dictionary<string, Dictionary<string, List<MeatovItemData>>>();
            itemsByParents = new Dictionary<string, List<MeatovItemData>>();
            itemsByRarity = new Dictionary<MeatovItem.ItemRarity, List<MeatovItemData>>();
            List<MeatovItemData> customItemDataList = new List<MeatovItemData>();
            int highestCustomIndex = 0;
            vanillaItemData = new Dictionary<string, List<MeatovItemData>>();

            foreach (KeyValuePair<string, JToken> defaultItemDataEntry in defaultItemDataDB)
            {
                MeatovItemData currentItemData = new MeatovItemData(defaultItemDataEntry.Value);
                defaultItemData.Add(defaultItemDataEntry.Key, currentItemData);
                if(defaultItemDataByH3ID.TryGetValue(currentItemData.H3ID, out List<MeatovItemData> itemDataList))
                {
                    currentItemData.defaultItemDataIndex = itemDataList.Count;
                    itemDataList.Add(currentItemData);
                }
                else
                {
                    currentItemData.defaultItemDataIndex = 0;
                    defaultItemDataByH3ID.Add(currentItemData.H3ID, new List<MeatovItemData>() { currentItemData });
                }

                if(currentItemData.itemType == MeatovItem.ItemType.Round)
                {
                    if (roundDefaultItemDataByRoundType.TryGetValue(currentItemData.roundType, out List<MeatovItemData> roundItemDataList))
                    {
                        roundItemDataList.Add(currentItemData);
                    }
                    else
                    {
                        roundDefaultItemDataByRoundType.Add(currentItemData.roundType, new List<MeatovItemData>() { currentItemData });
                    }
                }

                for(int i = 0; i < currentItemData.parents.Length; ++i)
                {
                    // If item is a mag
                    TODO: // Make a Magazine ItemType and set all mags in data
                    if (currentItemData.parents[i].Equals("5448bc234bdc2d3c308b4569"))
                    {
                        if (magDefaultItemDataByMagType.TryGetValue(currentItemData.magType, out List<MeatovItemData> magItemDataList))
                        {
                            magItemDataList.Add(currentItemData);
                        }
                        else
                        {
                            magDefaultItemDataByMagType.Add(currentItemData.magType, new List<MeatovItemData>() { currentItemData });
                        }
                    }
                }

                int parsedID = -1;
                if(int.TryParse(currentItemData.H3ID, out parsedID))
                {
                    if (defaultItemDataEntry.Value["modulGroup"] == null || defaultItemDataEntry.Value["modulGroup"].Type == JTokenType.Null)
                    {
                        // Custom item
                        customItemDataList.Add(currentItemData);
                        if(currentItemData.index > highestCustomIndex)
                        {
                            highestCustomIndex = currentItemData.index;
                        }
                    }
                    else
                    {
                        // 868 mod item
                        if (modItemsByPartByGroup.TryGetValue(currentItemData.modGroup, out Dictionary<string, List<MeatovItemData>> partDict))
                        {
                            if(partDict.TryGetValue(currentItemData.modPart, out List<MeatovItemData> partList))
                            {
                                currentItemData.modDictIndex = partList.Count;
                                partList.Add(currentItemData);
                            }
                            else
                            {
                                currentItemData.modDictIndex = 0;
                                partDict.Add(currentItemData.modPart, new List<MeatovItemData>() { currentItemData });
                            }
                        }
                        else
                        {
                            currentItemData.modDictIndex = 0;
                            modItemsByPartByGroup.Add(currentItemData.modGroup, new Dictionary<string, List<MeatovItemData>>() { { currentItemData.modPart, new List<MeatovItemData>() { currentItemData } } });
                        }
                    }
                }
                else
                {
                    // Vanilla item
                    if (vanillaItemData.TryGetValue(currentItemData.H3ID, out List<MeatovItemData> vanillaItemDataList))
                    {
                        currentItemData.vanillaItemDataIndex = itemDataList.Count;
                        itemDataList.Add(currentItemData);
                    }
                    else
                    {
                        currentItemData.defaultItemDataIndex = 0;
                        vanillaItemData.Add(currentItemData.H3ID, new List<MeatovItemData>() { currentItemData });
                    }

                    if (defaultItemDataEntry.Value["modulGroup"] != null && defaultItemDataEntry.Value["modulGroup"].Type != JTokenType.Null)
                    {
                        // Also mod item
                        if (modItemsByPartByGroup.TryGetValue(currentItemData.modGroup, out Dictionary<string, List<MeatovItemData>> partDict))
                        {
                            if (partDict.TryGetValue(currentItemData.modPart, out List<MeatovItemData> partList))
                            {
                                currentItemData.modDictIndex = partList.Count;
                                partList.Add(currentItemData);
                            }
                            else
                            {
                                currentItemData.modDictIndex = 0;
                                partDict.Add(currentItemData.modPart, new List<MeatovItemData>() { currentItemData });
                            }
                        }
                        else
                        {
                            currentItemData.modDictIndex = 0;
                            modItemsByPartByGroup.Add(currentItemData.modGroup, new Dictionary<string, List<MeatovItemData>>() { { currentItemData.modPart, new List<MeatovItemData>() { currentItemData } } });
                        }
                    }
                }
            }

            // Set custom item data array
            customItemData = new MeatovItemData[highestCustomIndex + 1];
            for(int i=0; i < customItemDataList.Count; ++i)
            {
                customItemData[customItemDataList[i].index] = customItemDataList[i];
            }
        }

        public void BuildCategoriesTree()
        {
            if (itemCategories == null)
            {
                itemCategories = new CategoryTreeNode(null, itemParentID, "Item");
            }
            // Note that a particular item will appear in the list corresponding to all of its ancestors, not only its direct parent
            foreach(KeyValuePair<string, List<MeatovItemData>> parentEntry in itemsByParents)
            {
                // Build list of all ancestors starting from this parent
                List<string> parents = new List<string>() { parentEntry.Key };
                JToken nextParentToken = itemDB[parentEntry.Key]["_parent"];
                while (nextParentToken != null && !nextParentToken.ToString().Equals(""))
                {
                    parents.Add(nextParentToken.ToString());
                    nextParentToken = itemDB[nextParentToken.ToString()]["_parent"];
                }

                // Make sure this parent and all ancestors are in the tree
                CategoryTreeNode previousParentNode = itemCategories;
                for (int i = parents.Count-1; i >= 0; --i)
                {
                    // Find the first ancestor ID that isn't in tree yet
                    CategoryTreeNode currentParentNode = itemCategories.FindChild(parents[i]);
                    if (currentParentNode == null)
                    {
                        // If this parent isn't in the tree yet, add it and all parents under it to the tree
                        for(int j=i; j >= 0; --j)
                        {
                            string name = null;
                            if(localeDB[parents[j] + " Name"] != null)
                            {
                                name = localeDB[parents[j] + " Name"].ToString();
                            }
                            else
                            {
                                name = GetCorrectCategoryName(itemDB[parents[j]]["_name"].ToString());
                            }
                            previousParentNode = new CategoryTreeNode(previousParentNode, parents[i], name);
                        }
                        break;
                    }
                    else
                    {
                        previousParentNode = currentParentNode;
                    }
                }
            }
        }

        public static string GetCorrectCategoryName(string name)
        {
            switch (name)
            {
                case "KeyMechanical":
                    return "Mechanical Keys";
                case "RepairKits":
                    return "Repair Kits";
                case "FaceCover":
                    return "Face Covers";
                case "RadioTransmitter":
                    return "Radio Transmitters";
                default:
                    return name;
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
            int difference = stackDifference;

            if (stackOnly)
            {
                if (playerInventory.ContainsKey(item.tarkovID))
                {
                    playerInventory[item.tarkovID] += stackDifference;
                    // Note that we don't update player weight here, it will instead be updated on item's OnCurrentWeightChanged event when its stack changes

                    if(playerInventory[item.tarkovID] <= 0)
                    {
                        Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        playerInventory.Remove(item.tarkovID);
                        playerInventoryItems.Remove(item.tarkovID);
                    }
                }
                else
                {
                    Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n"+Environment.StackTrace);
                }

                if (item.foundInRaid)
                {
                    if (playerFIRInventory.ContainsKey(item.tarkovID))
                    {
                        playerFIRInventory[item.tarkovID] += stackDifference;

                        if (playerFIRInventory[item.tarkovID] <= 0)
                        {
                            Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                            playerFIRInventory.Remove(item.tarkovID);
                            playerFIRInventoryItems.Remove(item.tarkovID);
                        }
                    }
                    else
                    {
                        Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n" + Environment.StackTrace);
                    }
                }
            }
            else
            {
                difference = item.stack;
                if (playerInventory.ContainsKey(item.tarkovID))
                {
                    playerInventory[item.tarkovID] += item.stack;
                    playerInventoryItems[item.tarkovID].Add(item);
                }
                else
                {
                    playerInventory.Add(item.tarkovID, item.stack);
                    playerInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
                }
                weight += item.currentWeight;
                item.OnCurrentWeightChanged += OnItemCurrentWeightChanged;

                if (item.foundInRaid)
                {
                    if (playerFIRInventory.ContainsKey(item.tarkovID))
                    {
                        playerFIRInventory[item.tarkovID] += item.stack;
                        playerFIRInventoryItems[item.tarkovID].Add(item);
                    }
                    else
                    {
                        playerFIRInventory.Add(item.tarkovID, item.stack);
                        playerFIRInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
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
                        
                        if(ammoBoxesByRoundClassByRoundType.TryGetValue(boxMagazine.RoundType, out Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>> midDict))
                        {
                            if(midDict.TryGetValue(loadedRound.LR_Class, out Dictionary<MeatovItem, int> boxDict))
                            {
                                int count = 0;
                                if(boxDict.TryGetValue(item, out count))
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

            OnPlayerItemInventoryChangedInvoke(item.itemData, difference);
        }

        public static void AddToPlayerFIRInventory(MeatovItem item)
        {
            if (!item.foundInRaid)
            {
                return;
            }

            if (playerFIRInventory.ContainsKey(item.tarkovID))
            {
                playerFIRInventory[item.tarkovID] += item.stack;
                playerFIRInventoryItems[item.tarkovID].Add(item);
            }
            else
            {
                playerFIRInventory.Add(item.tarkovID, item.stack);
                playerFIRInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
            }
        }

        public static void RemoveFromPlayerInventory(MeatovItem item)
        {
            Mod.LogInfo("\tRemoving item " + item.tarkovID + " with IID: " + item.GetInstanceID()+" from player inventory");
            int difference = -item.stack;

            if (playerInventory.ContainsKey(item.tarkovID))
            {
                playerInventory[item.tarkovID] -= item.stack;
                playerInventoryItems[item.tarkovID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove " + item.tarkovID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (playerInventory[item.tarkovID] == 0)
            {
                playerInventory.Remove(item.tarkovID);
                playerInventoryItems.Remove(item.tarkovID);
            }
            weight -= item.currentWeight;
            item.OnCurrentWeightChanged -= OnItemCurrentWeightChanged;

            if (item.foundInRaid)
            {
                if (playerFIRInventory.ContainsKey(item.tarkovID))
                {
                    playerFIRInventory[item.tarkovID] -= item.stack;
                    playerFIRInventoryItems[item.tarkovID].Remove(item);
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.tarkovID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
                    return;
                }
                if (playerFIRInventory[item.tarkovID] == 0)
                {
                    playerFIRInventory.Remove(item.tarkovID);
                    playerFIRInventoryItems.Remove(item.tarkovID);
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
                    if(ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class][item] == 0)
                    {
                        ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class].Remove(item);
                        if(ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType][loadedRound.LR_Class].Count == 0)
                        {
                            ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType].Remove(loadedRound.LR_Class);
                            if(ammoBoxesByRoundClassByRoundType[boxMagazine.RoundType].Count == 0)
                            {
                                ammoBoxesByRoundClassByRoundType.Remove(boxMagazine.RoundType);
                            }
                        }
                    }
                }
            }

            OnPlayerItemInventoryChangedInvoke(item.itemData, difference);
        }

        public static void RemoveFromPlayerFIRInventory(MeatovItem item)
        {
            if (item.foundInRaid)
            {
                return;
            }

            if (playerFIRInventory.ContainsKey(item.tarkovID))
            {
                playerFIRInventory[item.tarkovID] -= item.stack;
                playerFIRInventoryItems[item.tarkovID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove " + item.tarkovID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (playerFIRInventory[item.tarkovID] == 0)
            {
                playerFIRInventory.Remove(item.tarkovID);
                playerFIRInventoryItems.Remove(item.tarkovID);
            }
        }

        public static void DetachAllAttachmentsFromMount(FVRFireArmAttachmentMount mount, ref List<FVRFireArmAttachment> attachments)
        {
            foreach (FVRFireArmAttachment fvrfireArmAttachment in mount.AttachmentsList.ToArray())
            {
                foreach (FVRFireArmAttachmentMount mount2 in fvrfireArmAttachment.AttachmentMounts)
                {
                    DetachAllAttachmentsFromMount(mount2, ref attachments);
                }
                fvrfireArmAttachment.SetAllCollidersToLayer(false, "Default");
                fvrfireArmAttachment.DetachFromMount();

                attachments.Add(fvrfireArmAttachment);
            }
        }

        public static void OnItemCurrentWeightChanged(MeatovItem item, int preValue)
        {
            weight += item.currentWeight - preValue;
        }

        public static void AddExperience(int xp, int type = 0 /*0: General (kill, raid result, etc.), 1: Looting, 2: Healing, 3: Exploration*/, string notifMsg = null)
        {
            if(xp == 0)
            {
                return;
            }

            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && !Mod.charChoicePMC)
            {
                return;
            }

            // Add skill and area bonuses
            int actualXP = (int)(xp + xp / 100.0f * Bonus.experienceRate);

            experience += actualXP;
            int XPForNextLevel = XPPerLevel[level];
            while (experience >= XPForNextLevel)
            {
                ++level;
                experience -= XPForNextLevel;
                XPForNextLevel = XPPerLevel[level];
            }

            if (type == 1)
            {
                Mod.lootingExp += actualXP;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} looting experience.", actualXP));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, actualXP));
                }
            }
            else if (type == 2)
            {
                Mod.healingExp += actualXP;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} healing experience.", actualXP));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, actualXP));
                }
            }
            else if (type == 3)
            {
                Mod.explorationExp += actualXP;
                if (notifMsg == null)
                {
                    StatusUI.instance.AddNotification(string.Format("Gained {0} exploration experience.", actualXP));
                }
                else
                {
                    StatusUI.instance.AddNotification(string.Format(notifMsg, actualXP));
                }
            }
            else
            {
                StatusUI.instance.AddNotification(string.Format("Gained {0} experience.", actualXP));
            }

            Mod.raidExp += actualXP;
        }

        public static void AddSkillExp(float xp, int skillIndex)
        {
            // Globals SkillsSettings
            // Skip if in scav raid
            Skill skill = Mod.skills[skillIndex];
            if (Mod.currentLocationIndex == 1 || !Mod.charChoicePMC || skill.raidProgress >= 300) // Max 3 levels per raid TODO: should be unique to each skill
            {
                return;
            }

            int intPreProgress = (int)skill.progress;
            float preLevel = (int)(skill.progress / 100);

            float actualAmountToAdd = xp * ((skillIndex >= 12 && skillIndex <= 24) ? Skill.weaponSkillProgressRate : Skill.skillProgressRate);
            actualAmountToAdd += actualAmountToAdd * (Bonus.skillGroupLevelingBoost.ContainsKey(skill.skillType) ? Bonus.skillGroupLevelingBoost[skill.skillType] : 0);
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
                Mod.baseMaxStamina += (postLevel - preLevel);

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
            }
            else if (skillIndex == 1)
            {
                //Mod.skillWeightLimitBonus += (postLevel - preLevel);
                Mod.currentWeightLimit = Mod.baseWeightLimit;

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
            }

            if (intPreProgress < (int)skill.progress)
            {
            }
        }

        private void Init()
        {
            // Setup player data
            health = new float[7];
            basePositiveHealthRates = new float[7];
            baseNegativeHealthRates = new float[7];
            baseNonLethalHealthRates = new float[7];
            currentHealthRates = new float[7];
            currentNonLethalHealthRates = new float[7];

            // Instantiate lists
            ammoBoxesByRoundClassByRoundType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, Dictionary<MeatovItem, int>>>();

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
                // Set instance to 0 if leaving raid or if going to MeatovMainMenu
                if(GameObject.FindObjectOfType<RaidManager>() != null || H3MP.Patches.LoadLevelBeginPatch.loadingLevel.Equals("MeatovMainMenu"))
                {
                    H3MP.GameManager.SetInstance(0);
                }

                // Loading into meatov scene
                if (loadingToMeatovScene || H3MP.Patches.LoadLevelBeginPatch.loadingLevel.Equals("MeatovMainMenu"))
                {
                    loadingToMeatovScene = true;

                    // Secure scene components if haven't already
                    if (securedMainSceneComponents == null || securedMainSceneComponents.Count == 0)
                    {
                        SecureMainSceneComponents();
                    }

                    // Increase limit of pixel light count to something we are never going to reach
                    // We should never use more than what we have at lvl 3 illumnation in hideout + a few considering possible flashlights, etc.
                    QualitySettings.pixelLightCount = 100;
                }
                else // Not loading into meatov scene
                {
                    // Reset pixel light count
                    GM.RefreshQuality();
                }
            }
            else // Done loading
            {
                // No matter where we're going, don't want to keep objects secured
                // This wouldn't have been a problem normally, just keep the main scene stuff secured
                // as long as we are in meatov scenes, but problem arose when we start grabbing items,
                // which get moved to DontDestroyedOnLoad and then get lost there
                // The simplest solution is to keep things as unsecured as possible, only securing them when moving scenes
                UnsecureMainSceneComponents();

                inMeatovScene = loadingToMeatovScene;
                loadingToMeatovScene = false;

                // Finish loading raid map if necessary
                RaidManager raidManager = GameObject.FindObjectOfType<RaidManager>();
                if (raidManager != null)
                {
                    if(raidMapAdditiveBundleRequests != null)
                    {
                        for(int i=0; i < raidMapAdditiveBundleRequests.Count; ++i)
                        {
                            string[] additiveScenes = raidMapAdditiveBundleRequests[i].assetBundle.GetAllScenePaths();
                            for(int j=0; j < additiveScenes.Length; ++j)
                            {
                                SceneManager.LoadScene(additiveScenes[j], LoadSceneMode.Additive);
                            }
                        }
                    }
                    if(raidMapPrefabBundleRequests != null)
                    {
                        for(int i=0; i < raidMapPrefabBundleRequests.Count; ++i)
                        {
                            GameObject[] mapPrefabs = raidMapAdditiveBundleRequests[i].assetBundle.LoadAllAssets<GameObject>();
                            for(int j=0; j < mapPrefabs.Length; ++j)
                            {
                                GameObject.Instantiate(mapPrefabs[j]);
                            }
                        }
                    }
                }

                switch (H3MP.Patches.LoadLevelBeginPatch.loadingLevel)
                {
                    case "MainMenu3":
                        // Unload hideout bundle
                        if (hideoutBundle != null)
                        {
                            hideoutBundle.Unload(true);
                            hideoutBundle = null;
                            hideoutAssetsBundle.Unload(true);
                            hideoutAssetsBundle = null;
                            for(int i=0; i < hideoutAreaBundles.Length; ++i)
                            {
                                hideoutAreaBundles[i].Unload(true);
                                hideoutAreaBundles[i] = null;
                            }
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

        public static void UnsecureMainSceneComponents()
        {
            if (securedMainSceneComponents == null)
            {
                return;
            }

            for(int i=0; i < securedMainSceneComponents.Count; ++i)
            {
                if (securedMainSceneComponents[i].transform.parent == null)
                {
                    SceneManager.MoveGameObjectToScene(securedMainSceneComponents[i], SceneManager.GetActiveScene());
                }
            }

            securedMainSceneComponents.Clear();
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

        public static void SecureItems(string destination, bool pouchOnly = false)
        {
            TODO: // Verify that if we are securing the player body by securing main scene components, do we need to secure stuff if hands and slots?
                  // We shouldn't need to if the slot items are attached to the player body, because i think status ui also is attached to player(?)
            if(securedObjects == null)
            {
                securedObjects = new List<GameObject>();
                securedItems = new MeatovItem[8];
                securedSlotItems = new List<MeatovItem>();
            }
            else
            {
                securedObjects.Clear();
                for (int i = 0; i < securedItems.Length; ++i)
                {
                    securedItems[i] = null;
                }
                securedSlotItems.Clear();
            }


            if (pouchOnly)
            {
                if(StatusUI.instance.equipmentSlots[7].CurObject != null)
                {
                    securedItems[9] = StatusUI.instance.equipmentSlots[7].CurObject.GetComponent<MeatovItem>();
                    securedItems[9].physObj.SetQuickBeltSlot(null);
                    DontDestroyOnLoad(securedItems[9].gameObject);

                    if (securedItems[9].trackedMeatovItemData != null)
                    {
                        securedItems[9].trackedMeatovItemData.SetScene(destination, true);
                    }

                    SecureAdditionalObjects(securedItems[9], destination);
                }
            }
            else
            {
                if (Mod.leftHand.heldItem != null)
                {
                    MeatovItem item = Mod.leftHand.heldItem;

                    item.physObj.ForceBreakInteraction();
                    DontDestroyOnLoad(item.gameObject);
                    securedItems[0] = item;

                    if (item.trackedMeatovItemData != null)
                    {
                        item.trackedMeatovItemData.SetScene(destination, true);
                    }

                    SecureAdditionalObjects(item, destination);
                }
                if (Mod.rightHand.heldItem != null)
                {
                    MeatovItem item = Mod.rightHand.heldItem;

                    item.physObj.ForceBreakInteraction();
                    DontDestroyOnLoad(item.gameObject);
                    securedItems[1] = item;

                    if (item.trackedMeatovItemData != null)
                    {
                        item.trackedMeatovItemData.SetScene(destination, true);
                    }

                    SecureAdditionalObjects(item, destination);
                }
                for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++i)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject == null)
                    {
                        securedSlotItems.Add(null);
                    }
                    else
                    {
                        MeatovItem item = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.GetComponent<MeatovItem>();
                        if (item == null)
                        {
                            securedSlotItems.Add(null);
                        }
                        else
                        {
                            securedSlotItems.Add(item);

                            item.physObj.SetQuickBeltSlot(null);
                            DontDestroyOnLoad(item.gameObject);

                            if (item.trackedMeatovItemData != null)
                            {
                                item.trackedMeatovItemData.SetScene(destination, true);
                            }

                            SecureAdditionalObjects(item, destination);
                        }
                    }
                }
                for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Added.Count; ++i)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Added[i].CurObject == null)
                    {
                        securedSlotItems.Add(null);
                    }
                    else
                    {
                        MeatovItem item = GM.CurrentPlayerBody.QBSlots_Added[i].CurObject.GetComponent<MeatovItem>();
                        if (item == null)
                        {
                            securedSlotItems.Add(null);
                        }
                        else
                        {
                            securedSlotItems.Add(item);

                            item.physObj.SetQuickBeltSlot(null);
                            DontDestroyOnLoad(item.gameObject);

                            if (item.trackedMeatovItemData != null)
                            {
                                item.trackedMeatovItemData.SetScene(destination, true);
                            }

                            SecureAdditionalObjects(item, destination);
                        }
                    }
                }
                for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
                {
                    if (StatusUI.instance.equipmentSlots[i].CurObject != null)
                    {
                        MeatovItem item = StatusUI.instance.equipmentSlots[i].CurObject.GetComponent<MeatovItem>();
                        item.physObj.SetQuickBeltSlot(null);
                        DontDestroyOnLoad(item.gameObject);
                        securedItems[i + 2] = item;

                        if (item.trackedMeatovItemData != null)
                        {
                            item.trackedMeatovItemData.SetScene(destination, true);
                        }

                        SecureAdditionalObjects(item, destination);
                    }
                }
            }
        }

        public static void SecureAdditionalObjects(MeatovItem item, string destination)
        {
            // Items inside a rig will not be attached to the rig, so much secure them separately
            TODO: // Verify if this is still necessary
            if (item.itemType == MeatovItem.ItemType.Rig || item.itemType == MeatovItem.ItemType.ArmoredRig)
            {
                foreach (MeatovItem innerItem in item.itemsInSlots)
                {
                    // Check if not already secured
                    if (innerItem != null && !innerItem.gameObject.scene.name.Equals("DontDestroyOnLoad"))
                    {
                        DontDestroyOnLoad(innerItem.gameObject);
                        securedObjects.Add(innerItem.gameObject);
                        if (innerItem.trackedMeatovItemData != null)
                        {
                            innerItem.trackedMeatovItemData.SetScene(destination, true);
                        }
                        SecureAdditionalObjects(innerItem, destination);
                    }
                }
            }

            // Secure any laser pointer hit objects
            LaserPointer[] laserPointers = item.GetComponentsInChildren<LaserPointer>();
            foreach (LaserPointer laserPointer in laserPointers)
            {
                if (laserPointer.BeamHitPoint != null && laserPointer.BeamHitPoint.transform.parent == null)
                {
                    DontDestroyOnLoad(laserPointer.BeamHitPoint);
                    securedObjects.Add(laserPointer.BeamHitPoint);
                }
            }
        }

        public static void UnsecureItems(bool scav = false)
        {
            // Unsecure generic secured objects
            if(securedObjects != null)
            {
                for (int i = 0; i < securedObjects.Count; ++i)
                {
                    SceneManager.MoveGameObjectToScene(securedObjects[i], SceneManager.GetActiveScene());
                }
                securedObjects.Clear();
            }

            // Unsecure hand and equipment items
            for(int i=0; i < securedItems.Length; ++i)
            {
                SceneManager.MoveGameObjectToScene(securedItems[i].gameObject, SceneManager.GetActiveScene());

                if (scav)
                {
                    securedItems[i].transform.parent = HideoutController.instance.scavReturnNode;
                }
                else
                {
                    if (i == 0)
                    {
                        Mod.leftHand.fvrHand.ForceSetInteractable(securedItems[i].physObj);
                    }
                    else if(i == 1)
                    {
                        Mod.rightHand.fvrHand.ForceSetInteractable(securedItems[i].physObj);
                    }
                    else
                    {
                        securedItems[i].physObj.SetQuickBeltSlot(StatusUI.instance.equipmentSlots[i - 2]);
                    }
                }
            }

            // Unsecure slot items
            for (int i = 0; i < securedSlotItems.Count; ++i)
            {
                if(securedSlotItems[i] != null)
                {
                    SceneManager.MoveGameObjectToScene(securedSlotItems[i].gameObject, SceneManager.GetActiveScene());

                    if (scav)
                    {
                        securedItems[i].transform.parent = HideoutController.instance.scavReturnNode;
                    }
                    else
                    {
                        securedItems[i].physObj.SetQuickBeltSlot(i < GM.CurrentPlayerBody.QBSlots_Internal.Count ? GM.CurrentPlayerBody.QBSlots_Internal[i] : GM.CurrentPlayerBody.QBSlots_Added[i + GM.CurrentPlayerBody.QBSlots_Internal.Count]);
                    }
                }
            }
        }

        public static GameObject GetItemPrefab(int index)
        {
            int bundleIndex = index / 200;
            return itemsBundles[bundleIndex].LoadAsset<GameObject>("Item" + index);
        }

        public static int GetRoundWeight(FireArmRoundType roundType)
        {
            FVRFireArmRoundDisplayData.DisplayDataClass displayClass = AM.SRoundDisplayDataDic[roundType].GetDisplayClass(AM.GetDefaultRoundClass(roundType));
            if(defaultItemDataByH3ID.TryGetValue(displayClass.ObjectID.ItemID, out List<MeatovItemData> itemDataList))
            {
                // Take first, all in the list whould be the same
                return itemDataList[0].weight;
            }
            else
            {
                // Default 15g if couldn't find item data
                return 15;
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

        public static long GetItemCountInInventories(string tarkovID)
        {
            long count = 0;
            int intCount = 0;
            HideoutController.inventory.TryGetValue(tarkovID, out intCount);
            count += intCount;
            playerInventory.TryGetValue(tarkovID, out intCount);
            count += intCount;

            return count;
        }

        public static long GetFIRItemCountInInventories(string tarkovID)
        {
            long count = 0;
            int intCount = 0;
            HideoutController.FIRInventory.TryGetValue(tarkovID, out intCount);
            count += intCount;
            playerFIRInventory.TryGetValue(tarkovID, out intCount);
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
            if(whiteList == null || whiteList.Count == 0)
            {
                // Note whitelist, return true as long as item not described in blacklist
                if (blackList == null || blackList.Count == 0)
                {
                    return true;
                }
                else
                {
                    // Must check if any prior ancestor IDs are in the blacklist
                    // If an prior ancestor or the item's ID is found in the blacklist, return false, the item does not fit
                    for (int j = 0; j < parentsToUse.Count; ++j)
                    {
                        if (blackList.Contains(parentsToUse[j]))
                        {
                            return false;
                        }
                    }
                    return !blackList.Contains(IDToUse);
                }
            }
            else
            {
                for (int i = 0; i < parentsToUse.Count; ++i)
                {
                    // If whitelist contains the ancestor ID
                    if (whiteList.Contains(parentsToUse[i]))
                    {
                        if (blackList == null || blackList.Count == 0)
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
            }

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

        public static void SetIcon(MeatovItemData itemData, Image icon)
        {
            int parsedID = -1;
            if (int.TryParse(itemData.H3ID, out parsedID))
            {
                if(parsedID == 868)
                {
                    bool gotIcon = false;
                    if(ModularWorkshopManager.ModularWorkshopPartsGroupsDictionary.TryGetValue(itemData.modGroup, out ModularWorkshopPartsDefinition partDef))
                    {
                        if (partDef.PartsDictionary.TryGetValue(itemData.modPart, out GameObject weaponPart))
                        {
                            ModularWeaponPart weaponPartScript = weaponPart.GetComponent<ModularWeaponPart>();
                            if(weaponPartScript != null)
                            {
                                icon.sprite = weaponPartScript.Icon;
                                gotIcon = true;
                            }
                        }
                    }
                    if (!gotIcon)
                    {
                        Mod.LogError("Mod.SetIcon could not load modul mod " + itemData.tarkovID + ":" + itemData.H3ID + " icon");
                        icon.sprite = Mod.questionMarkIcon;
                    }
                }
                else
                {
                    Sprite sprite = Mod.itemIconsBundle.LoadAsset<Sprite>("Item" + parsedID + "_Icon");
                    if (sprite == null)
                    {
                        Mod.LogError("Mod.SetIcon could not load custom item " + itemData.tarkovID + ":" + parsedID + " icon");
                        icon.sprite = Mod.questionMarkIcon;
                    }
                    else
                    {
                        icon.sprite = sprite;
                    }
                }
            }
            else
            {
                if (itemData.H3SpawnerID != null && IM.HasSpawnedID(itemData.H3SpawnerID))
                {
                    icon.sprite = IM.GetSpawnerID(itemData.H3SpawnerID).Sprite;
                }
                else // Could not get icon from item data (spawner ID)
                {
                    Mod.LogError("Could not get icon for " + itemData.tarkovID+":"+ itemData.H3ID);
                    icon.sprite = Mod.questionMarkIcon;
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

        public static bool ItemIDToCurrencyIndex(string itemID, out int index)
        {
            switch (itemID)
            {
                case "5449016a4bdc2d6f028b456f":
                case "203":
                    index = 0;
                    return true;
                case "5696686a4bdc2da3298b456a":
                case "201":
                    index = 1;
                    return true;
                case "569668774bdc2da2298b4568":
                case "202":
                    index = 2;
                    return true;
                default:
                    index = -1;
                    return false;
            }
        }

        public static string TarkovIDtoH3ID(string tarkovID)
        {
            if(defaultItemData.TryGetValue(tarkovID, out MeatovItemData itemData))
            {
                return itemData.H3ID;
            }
            else
            {
                return tarkovID;
            }
        }

        public static void H3MP_OnInstantiationTrack(GameObject go)
        {
            if (skipNextInstantiation || !Mod.inMeatovScene)
            { 
                skipNextInstantiation = false;
                return;
            }

            // This or one of our instantiation patches will happen first
            // In this case, we want to make sure that if the item is a MeatovItem, we add the script
            // before H3MP tracks it
            if (instantiatedItem == go)
            {
                // We already setup this item, no need to do it here
                // It is ready to be tracked by H3MP
                instantiatedItem = null;
                return;
            }
            else
            {
                // This item was caught by H3MP's instantiation patches and must be setup now
                MeatovItem meatovItem = go.GetComponent<MeatovItem>();
                FVRPhysicalObject physObj = go.GetComponent<FVRPhysicalObject>();
                if (meatovItem == null && physObj != null)
                {
                    MeatovItem.Setup(physObj);
                    instantiatedItem = go;
                }
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

        public static void AddRaidMap(string mapName, string bundleName)
        {
            if (availableRaidMaps.TryGetValue(mapName, out string existingFullPath))
            {
                Mod.LogError("Could not add raid map " + mapName + ":" + bundleName + ", a map with the samename is already loaded with full path: " + existingFullPath);
                return;
            }

            // Find plugins directory
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo currentParentDirectory = Directory.GetParent(currentDirectory);
            while (currentParentDirectory != null && !currentParentDirectory.Name.Equals("plugins"))
            {
                currentParentDirectory = currentParentDirectory.Parent;
            }
            if (!currentParentDirectory.Name.Equals("plugins"))
            {
                Mod.LogError("Could not find plugins folder to look for map " + mapName + ":" + bundleName);
                return;
            }

            // Find full path to bundle
            if(FindBundle(bundleName, currentParentDirectory.FullName, out string fullPath))
            {
                // Add if found
                availableRaidMaps.Add(mapName, fullPath);
            }
            else
            {
                Mod.LogError("Could not find map bundle:" + bundleName);
            }
        }

        public static void AddRaidMapAdditive(string mapName, string bundleName)
        {
            // Find plugins directory
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo currentParentDirectory = Directory.GetParent(currentDirectory);
            while (currentParentDirectory != null && !currentParentDirectory.Name.Equals("plugins"))
            {
                currentParentDirectory = currentParentDirectory.Parent;
            }
            if (!currentParentDirectory.Name.Equals("plugins"))
            {
                Mod.LogError("Could not find plugins folder to look for map additive " + mapName + ":" + bundleName);
                return;
            }

            // Find full path to bundle
            if(FindBundle(bundleName, currentParentDirectory.FullName, out string fullPath))
            {
                // Add if found
                if(availableRaidMapAdditives.TryGetValue(mapName, out List<string> existingAdditives))
                {
                    existingAdditives.Add(fullPath);
                }
                else
                {
                    availableRaidMapAdditives.Add(mapName, new List<string> { fullPath });
                }
            }
            else
            {
                Mod.LogError("Could not find map additive bundle:" + bundleName);
            }
        }

        public static void AddRaidMapPrefab(string mapName, string bundleName)
        {
            // Find plugins directory
            string currentDirectory = Directory.GetCurrentDirectory();
            DirectoryInfo currentParentDirectory = Directory.GetParent(currentDirectory);
            while (currentParentDirectory != null && !currentParentDirectory.Name.Equals("plugins"))
            {
                currentParentDirectory = currentParentDirectory.Parent;
            }
            if (!currentParentDirectory.Name.Equals("plugins"))
            {
                Mod.LogError("Could not find plugins folder to look for map prefab " + mapName + ":" + bundleName);
                return;
            }

            // Find full path to bundle
            if(FindBundle(bundleName, currentParentDirectory.FullName, out string fullPath))
            {
                // Add if found
                if(availableRaidMapPrefabs.TryGetValue(mapName, out List<string> existingAdditives))
                {
                    existingAdditives.Add(fullPath);
                }
                else
                {
                    availableRaidMapPrefabs.Add(mapName, new List<string> { fullPath });
                }
            }
            else
            {
                Mod.LogError("Could not find map prefab bundle:" + bundleName);
            }
        }

        public static void AddRaidMapRequirements(string mapName, string requirementItemsString, string requirementCountsString)
        {
            List<string> requirementItems = new List<string>();
            List<int> requirementCounts = new List<int>();

            string[] itemSplit = requirementItemsString.Split(',');
            string[] countSplit = requirementCountsString.Split(',');
            for(int i=0; i < itemSplit.Length; ++i)
            {
                requirementItems.Add(itemSplit[i]);
                requirementCounts.Add(int.Parse(countSplit[i]));
            }

            if (requirementItems == null || requirementItems.Count == 0)
            {
                return;
            }

            if (availableRaidMaps.ContainsKey(mapName))
            {
                if (raidMapEntryRequirements.ContainsKey(mapName))
                {
                    Mod.LogError("Could not add entry requirements for raid map: " + mapName + ", requirements already exist");
                }
                else
                {
                    Dictionary<string, int> reqDict = new Dictionary<string, int>();
                    for(int i=0; i < requirementItems.Count; ++i)
                    {
                        reqDict.Add(requirementItems[i], requirementCounts[i]);
                    }
                    raidMapEntryRequirements.Add(mapName, reqDict);
                }
            }
            else
            {
                Mod.LogError("Could not add entry requirements for raid map: " + mapName+", map missing");
            }
        }

        public static bool FindBundle(string bundleName, string directory, out string fullPath)
        {
            string[] files = Directory.GetFiles(directory);
            for (int i=0; i < files.Length; ++i)
            {
                if (files[i].EndsWith(bundleName))
                {
                    fullPath = files[i];
                    return true;
                }
            }

            string[] dirs = Directory.GetDirectories(directory);
            for (int i=0; i < dirs.Length; ++i)
            {
                if(FindBundle(bundleName, dirs[i], out fullPath))
                {
                    return true;
                }
            }

            fullPath = null;
            return false;
        }
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
