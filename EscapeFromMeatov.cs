using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using BepInEx;
using FistVR;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;
using UnityEngine.Audio;
using static FistVR.SM;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR;
using UnityEngine.UI;
using System.Reflection.Emit;
using System.Collections;
using UnityEngine.EventSystems;

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
        public static Mod instance;
        public static int currentLocationIndex = -1; // This will be used by custom item wrapper and vanilla item descr. in their Start(). Shoud only ever be 1(base) or 2(raid). If want to spawn an item in player inventory, will have to set it manually
        public static AssetBundle[] assetsBundles;
        public static AssetBundle defaultAssetsBundle;
        public static AssetBundle menuBundle;
        public static AssetBundle baseAssetsBundle;
        public static AssetBundle baseBundle;
        public static AssetBundleCreateRequest currentRaidBundleRequest;
        public static MainMenuSceneDef sceneDef;
        public static List<GameObject> securedObjects;
        public static FVRInteractiveObject securedLeftHandInteractable;
        public static FVRInteractiveObject securedRightHandInteractable;
        public static int saveSlotIndex = -1;
        public static int currentQuickBeltConfiguration = -1;
        public static int firstCustomConfigIndex = -1;
        public static List<EFM_EquipmentSlot> equipmentSlots;
        public static bool beginInteractingEquipRig;
        public static EFM_Hand rightHand;
        public static EFM_Hand leftHand;
        public static List<List<FVRQuickBeltSlot>> otherActiveSlots;
        public static int chosenCharIndex;
        public static int chosenMapIndex;
        public static int chosenTimeIndex;
        public static EFM_Base_Manager.FinishRaidState raidState;
        public static bool justFinishedRaid;
        public static Dictionary<string, int> killList;
        public static int lootingExp = 0;
        public static int healingExp = 0;
        public static int explorationExp = 0;
        public static float raidTime = 0;
        public static bool grillHouseSecure;
        public static bool isGrillhouse;
        public static bool inMeatovScene;
        public static int pocketsConfigIndex;
        public static GameObject[] itemsInPocketSlots;
        public static FVRQuickBeltSlot[] pocketSlots;
        public static EFM_ShoulderStorage leftShoulderSlot;
        public static EFM_ShoulderStorage rightShoulderSlot;
        public static GameObject leftShoulderObject;
        public static GameObject rightShoulderObject;
        public static Dictionary<string, int> baseInventory;
        public static EFM_Base_Manager currentBaseManager;
        public static EFM_Raid_Manager currentRaidManager;
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
        public static Dictionary<string, List<EFM_DescriptionManager>> activeDescriptionsByItemID;
        public static List<EFM_AreaSlot> areaSlots;
        public static bool areaSlotShouldUpdate = true;
        public static List<EFM_AreaBonus> activeBonuses;
        public static EFM_TraderStatus[] traderStatuses;
        public static EFM_CategoryTreeNode itemCategories;
        public static Dictionary<string, List<string>> itemAncestors;
        public static Dictionary<string, int> lowestBuyValueByItem;
        public static bool amountChoiceUIUp;
        public static EFM_CustomItemWrapper splittingItem;
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
        public static Dictionary<EFM_Effect.EffectType, List<TraderTaskCounterCondition>> taskHealthEffectCounterConditionsByEffectType = new Dictionary<EFM_Effect.EffectType, List<TraderTaskCounterCondition>>();
        public static Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>> taskShotsCounterConditionsByBodyPart = new Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>>();
        public static Dictionary<string, List<TraderTaskCounterCondition>> taskUseItemCounterConditionsByItemID = new Dictionary<string, List<TraderTaskCounterCondition>>();
        public static Dictionary<string, List<TraderTaskCondition>> taskFindItemConditionsByItemID = new Dictionary<string, List<TraderTaskCondition>>();
        public static Dictionary<int, List<TraderTaskCondition>> taskSkillConditionsBySkillIndex = new Dictionary<int, List<TraderTaskCondition>>();
        public static Dictionary<string, Dictionary<string, List<TraderTaskCondition>>> currentTaskLeaveItemConditionsByItemIDByZone;
        public static Dictionary<string, List<TraderTaskCondition>> currentTaskVisitPlaceConditionsByZone;
        public static Dictionary<string, List<TraderTaskCounterCondition>> currentTaskVisitPlaceCounterConditionsByZone;
        public static Dictionary<EFM_Effect.EffectType, List<TraderTaskCounterCondition>> currentHealthEffectCounterConditionsByEffectType;
        public static Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>> currentShotsCounterConditionsByBodyPart;
        public static Dictionary<string, List<TraderTaskCounterCondition>> currentUseItemCounterConditionsByItemID;

        // Player
        public static GameObject playerStatusUI;
        public static EFM_PlayerStatusManager playerStatusManager;
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
        public static EFM_Skill[] skills;
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
        public static EFM_Effect dehydrationEffect;
        public static EFM_Effect fatigueEffect;
        public static EFM_Effect overweightFatigueEffect;
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
        public static EFM_DescriptionManager leftDescriptionManager;
        public static GameObject rightDescriptionUI;
        public static EFM_DescriptionManager rightDescriptionManager;
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
                playerStatusManager.UpdateWeight();
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
                playerStatusManager.UpdateWeight();
            }
            get
            {
                return _currentWeightLimit;
            }
        }

        // Assets
        public static bool assetLoaded;
        public static Sprite sceneDefImage;
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
        public static JObject localDB;
        public static Dictionary<string, ItemMapEntry> itemMap;
        public static JObject[] traderBaseDB;
        public static JObject[] traderAssortDB;
        public static JArray[] traderCategoriesDB;
        public static JObject globalDB;
        public static JObject questDB;
        public static JArray XPPerLevel;
        public static JObject mapData;
        public static JObject[] locationsLootDB;
        public static JObject[] locationsBaseDB;
        public static JArray lootContainerDB;
        public static JObject dynamicLootTable;
        public static JObject staticLootTable;
        public static JObject defaultItemsData;
        public static Dictionary<string, EFM_VanillaItemDescriptor> vanillaItems;
        public static Dictionary<string, JObject> lootContainersByName;
        public static Dictionary<string, AudioClip[]> itemSounds;

        // Config settings
        public static JObject config;
        public static GameObject physicsDoorsToggleCheckmark;
        public static bool physicsDoors;

        // DEBUG
        public bool debug;

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
            instance = this;

            LoadConfig();

            LoadDB();

            LoadDefaultAssets();

            DoPatching();

            Init();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                debug = !debug;
                LogWarning("Debug active: " + debug);
            }
            if (debug)
            {
                if (Input.GetKeyDown(KeyCode.L))
                {
                    SteamVR_LoadLevel.Begin("MeatovMenuScene", false, 0.5f, 0f, 0f, 0f, 1f);
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    // Loads new game
                    EFM_Manager.LoadBase();
                }

                if (Input.GetKeyDown(KeyCode.N))
                {
                    SteamVR_LoadLevel.Begin("Grillhouse_2Story", false, 0.5f, 0f, 0f, 0f, 1f);
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    FieldInfo genericPoolField = typeof(SM).GetField("m_pool_generic", BindingFlags.NonPublic | BindingFlags.Instance);
                    Logger.LogInfo("Generic pool null?: " + (genericPoolField.GetValue(ManagerSingleton<SM>.Instance) == null));
                }

                if (Input.GetKeyDown(KeyCode.O))
                {
                    Mod.chosenMapIndex = -1;
                    GameObject.Find("Hideout").GetComponent<EFM_Base_Manager>().OnConfirmRaidClicked();
                }

                if (Input.GetKeyDown(KeyCode.J))
                {
                    EFM_Manager.LoadBase(5);
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    EFM_Manager.LoadBase(-1, true);
                }

                if (Input.GetKeyDown(KeyCode.K))
                {
                    if (GameObject.Find("Hideout") != null)
                    {
                        GameObject.Find("Hideout").GetComponent<EFM_Base_Manager>().OnSaveSlotClicked(0);
                    }
                }

                if (Input.GetKeyDown(KeyCode.I))
                {
                    EFM_Manager.LoadBase(0);
                }

                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    currentRaidManager.KillPlayer();
                }

                if (Input.GetKeyDown(KeyCode.Keypad2))
                {
                    GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject root in roots)
                    {
                        DestroyLODs(root.transform);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad3))
                {
                    GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject root in roots)
                    {
                        SetProbeSettings(root.transform, UnityEngine.Rendering.LightProbeUsage.Off, UnityEngine.Rendering.ReflectionProbeUsage.Off);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad4))
                {
                    GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject root in roots)
                    {
                        SetProbeSettings(root.transform, UnityEngine.Rendering.LightProbeUsage.BlendProbes, UnityEngine.Rendering.ReflectionProbeUsage.BlendProbes);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad5))
                {
                    GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject root in roots)
                    {
                        DestroyGraphicRayCasters(root.transform);
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad6))
                {
                    GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (GameObject root in roots)
                    {
                        SetRaycastTarget(root.transform);
                    }
                }
            }
        }

        private void DestroyGraphicRayCasters(Transform root)
        {
            GraphicRaycaster rc = root.GetComponent<GraphicRaycaster>();
            if (rc != null)
            {
                Destroy(rc);
            }
            foreach (Transform child in root)
            {
                DestroyGraphicRayCasters(child);
            }
        }

        private void SetRaycastTarget(Transform root)
        {
            Graphic[] rc = root.GetComponents<Graphic>();
            if (rc != null)
            {
                foreach (Graphic g in rc)
                {
                    if (g != null)
                    {
                        g.raycastTarget = false;
                    }
                }
            }
            foreach (Transform child in root)
            {
                SetRaycastTarget(child);
            }
        }

        private void SetProbeSettings(Transform root, UnityEngine.Rendering.LightProbeUsage light, UnityEngine.Rendering.ReflectionProbeUsage reflect)
        {
            MeshRenderer mr = root.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.lightProbeUsage = light;
                mr.reflectionProbeUsage = reflect;
            }
            foreach (Transform child in root)
            {
                SetProbeSettings(child, light, reflect);
            }
        }

        private void DestroyLODs(Transform root)
        {
            LogInfo("destroy LODs on " + root.name);
            LODGroup lodGroup = root.GetComponent<LODGroup>();
            if (lodGroup != null)
            {
                LOD[] lods = lodGroup.GetLODs();
                if (lods != null)
                {
                    for (int i = 1; i < lods.Length; ++i)
                    {
                        Renderer[] renderers = lods[i].renderers;
                        if (renderers != null)
                        {
                            for (int j = renderers.Length - 1; j >= 0; --j)
                            {
                                Renderer renderer = renderers[j];
                                if (renderer != null)
                                {
                                    Destroy(renderer.gameObject);
                                }
                            }
                        }
                    }
                }
                Destroy(lodGroup);
            }

            foreach (Transform child in root)
            {
                DestroyLODs(child);
            }
        }

        private void DumpObjectIDs()
        {
            foreach (string key in IM.OD.Keys)
            {
                Logger.LogInfo("key: " + key + ": " + IM.OD[key].DisplayName);
            }
        }

        private void DumpResourceMaterials()
        {
            Material[] mats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (Material mat in mats)
            {
                LogInfo(mat.name);
            }
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

        private void DumpDirectory(string path, int level)
        {
            string levelString = "";
            for (int i = 0; i < level; ++i)
            {
                levelString += "\t";
            }

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles("*.*");
            foreach (FileInfo f in files)
            {
                Logger.LogInfo(levelString + "File: " + f.FullName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories();
            foreach (DirectoryInfo d in dirs)
            {
                Logger.LogInfo(levelString + "Directory: " + d.FullName);
                DumpDirectory(d.FullName, level + 1);
            }
        }

        private void LoadConfig()
        {
            config = JObject.Parse(File.ReadAllText("BepinEx/Plugins/EscapeFromMeatov/DB/EscapeFromMeatovConfig.json"));

            // Load settings
            physicsDoors = (bool)config["PhysicalDoors"];
        }

        public void LoadAssets()
        {
            LogInfo("Loading assets and scene bundles");
            // Load mod's AssetBundle
            assetsBundles = new AssetBundle[2];
            assetsBundles[0] = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovAssets0.ab");
            assetsBundles[1] = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovAssets1.ab");
            menuBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovMenu.ab");

            LogInfo("Loading Main assets");
            // Load assets
            quickBeltSlotPrefab = assetsBundles[1].LoadAsset<GameObject>("QuickBeltSlot");
            rectQuickBeltSlotPrefab = assetsBundles[1].LoadAsset<GameObject>("RectQuickBeltSlot");
            playerStatusUIPrefab = assetsBundles[0].LoadAsset<GameObject>("StatusUI");
            consumeUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ConsumeUI");
            stackSplitUIPrefab = assetsBundles[0].LoadAsset<GameObject>("StackSplitUI");
            extractionUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionUI");
            extractionLimitUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionLimitUI");
            extractionCardPrefab = assetsBundles[0].LoadAsset<GameObject>("ExtractionCard");
            itemDescriptionUIPrefab = assetsBundles[0].LoadAsset<GameObject>("ItemDescriptionUI");
            neededForPrefab = assetsBundles[0].LoadAsset<GameObject>("NeededForText");
            ammoContainsPrefab = assetsBundles[0].LoadAsset<GameObject>("ContainsText");
            staminaBarPrefab = assetsBundles[0].LoadAsset<GameObject>("StaminaBar");
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
            defaultItemsData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/DefaultItemData.json"));
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
                EFM_CustomItemWrapper customItemWrapper = itemPrefab.AddComponent<EFM_CustomItemWrapper>();
                customItemWrapper.ID = i.ToString();
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
                itemAncestors.Add(customItemWrapper.ID, customItemWrapper.parents);
                customItemWrapper.itemName = itemObjectWrapper.DisplayName;
                customItemWrapper.description = defaultItemsData["ItemDefaults"][i]["description"] != null ? defaultItemsData["ItemDefaults"][i]["description"].ToString() : "";
                itemDescriptions.Add(i.ToString(), customItemWrapper.description);
                customItemWrapper.lootExperience = (int)defaultItemsData["ItemDefaults"][i]["lootExperience"];
                customItemWrapper.spawnChance = (float)defaultItemsData["ItemDefaults"][i]["spawnChance"];
                customItemWrapper.rarity = ItemRarityStringToEnum(defaultItemsData["ItemDefaults"][i]["rarity"].ToString());
                if (itemsByRarity.ContainsKey(customItemWrapper.rarity))
                {
                    itemsByRarity[customItemWrapper.rarity].Add(customItemWrapper.ID);
                }
                else
                {
                    itemsByRarity.Add(customItemWrapper.rarity, new List<string>() { customItemWrapper.ID });
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
                            LogError("No item sound found for category: " + itemSound);
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
                        itemsByParents[parent].Add(customItemWrapper.ID);
                    }
                    else
                    {
                        itemsByParents.Add(parent, new List<string>() { customItemWrapper.ID });
                    }
                }

                // Fill customItemWrapper Colliders
                List<Collider> colliders = new List<Collider>();
                foreach (Transform collider in itemPrefab.transform.GetChild(0).GetChild(itemPrefab.transform.GetChild(0).childCount - 1))
                {
                    colliders.Add(collider.GetComponent<Collider>());
                }
                customItemWrapper.colliders = colliders.ToArray();

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
                        EFM_StackTrigger currentStackTrigger = triggerObject.AddComponent<EFM_StackTrigger>();
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
                            currentBuff.effectType = (EFM_Effect.EffectType)Enum.Parse(typeof(EFM_Effect.EffectType), buff["BuffType"].ToString());
                            currentBuff.chance = (float)buff["Chance"];
                            currentBuff.delay = (float)buff["Delay"];
                            currentBuff.duration = (float)buff["Duration"];
                            currentBuff.value = (float)buff["Value"];
                            currentBuff.absolute = (bool)buff["AbsoluteValue"];
                            if (currentBuff.effectType == EFM_Effect.EffectType.SkillRate)
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
                itemCategories = new EFM_CategoryTreeNode(null, "54009119af1c881c07000029", "Item");
            }
            EFM_CategoryTreeNode currentParent = itemCategories;
            for (int i = parents.Count - 2; i >= 0; i--)
            {
                string ID = parents[i];
                EFM_CategoryTreeNode foundChild = currentParent.FindChild(ID);
                if (foundChild != null)
                {
                    currentParent = foundChild;
                }
                else
                {
                    string name = localDB["templates"][ID]["Name"].ToString();
                    EFM_CategoryTreeNode newNode = new EFM_CategoryTreeNode(currentParent, ID, name);
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
                Mod.instance.LogWarning("Attempted to get vanilla prefab for " + ID + ", but the prefab had been destroyed, refreshing cache...");

                IM.OD[ID].RefreshCache();
                do
                {
                    Mod.instance.LogInfo("Waiting for cache refresh...");
                    itemPrefab = IM.OD[ID].GetGameObject();
                } while (itemPrefab == null);
            }
            FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
            image.sprite = physObj is FVRFireArmRound ? Mod.cartridgeIcon : IM.GetSpawnerID(physObj.ObjectWrapper.SpawnedFromId).Sprite;
        }

        private void SetFilterListsFor(EFM_CustomItemWrapper customItemWrapper, int index)
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
            areasDB = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Areas.json"));
            localDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Locale.json"));
            ParseItemMap();
            traderBaseDB = new JObject[8];
            traderAssortDB = new JObject[8];
            traderCategoriesDB = new JArray[8];
            for (int i = 0; i < 8; ++i)
            {
                string traderID = EFM_TraderStatus.IndexToID(i);
                traderBaseDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Traders/" + traderID + "/base.json"));
                traderAssortDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Traders/" + traderID + "/assort.json"));

                // TODO: Review, we dont currently use the categories right now because I thought these were the categories of items we coudl sell
                // to the trader but apparently they are jsut UI stuff, IDs only used for UI locale
                // We need to find actual sell IDs or just keep using the current method we have of deciding whichi tems we can sell, which is
                // that we can only sell to them items of type of items they sell themselves, unless its fence, to whom we can sell anything at reduced price
                //traderCategoriesDB[i] = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Traders/" + traderID + "/categories.json"));
            }
            globalDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Globals.json"));
            MovementManagerUpdatePatch.damagePerMeter = (float)Mod.globalDB["config"]["Health"]["Falling"]["DamagePerMeter"];
            MovementManagerUpdatePatch.safeHeight = (float)Mod.globalDB["config"]["Health"]["Falling"]["SafeHeight"];
            questDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Quests.json"));
            XPPerLevel = (JArray)globalDB["config"]["exp"]["level"]["exp_table"];
            mapData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/EscapeFromMeatovMapData.json"));
            locationsLootDB = new JObject[12];
            locationsBaseDB = new JObject[12];
            string[] locationLootFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov/DB/Locations/loot");
            string[] locationBaseFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov/DB/Locations/base");
            // TODO: 12.12? loc loot files are missing data for items that spawn with parent
            // Take factory day lootpoint (101)1736994 for example
            for (int i=0; i < 12; ++i)
            {
                string fileName = Mod.LocationIndexToDataName(i) + ".json";
                foreach (string locationLootFile in locationLootFiles)
                {
                    if (locationLootFile.EndsWith(fileName))
                    {
                        locationsLootDB[i] = JObject.Parse(File.ReadAllText(locationLootFile));
                    }
                }
                foreach (string locationBaseFile in locationBaseFiles)
                {
                    if (locationBaseFile.EndsWith(fileName))
                    {
                        locationsBaseDB[i] = JObject.Parse(File.ReadAllText(locationBaseFile));
                    }
                }
            }
            lootContainerDB = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/LootContainer.json"));
            dynamicLootTable = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Locations/DynamicLootTable.json"));
            staticLootTable = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DB/Locations/StaticLootTable.json"));
            lootContainersByName = new Dictionary<string, JObject>();
            foreach (JToken container in lootContainerDB)
            {
                lootContainersByName.Add(container["_name"].ToString(), (JObject)container);
            }

            LoadSkillVars();
        }

        private void LoadSkillVars()
        {
            JToken skillsSettings = globalDB["config"]["SkillsSettings"];
            EFM_Skill.skillProgressRate = (float)skillsSettings["SkillProgressRate"];
            EFM_Skill.weaponSkillProgressRate = (float)skillsSettings["WeaponSkillProgressRate"];

            // HideoutManagement
            EFM_Skill.skillPointsPerAreaUpgrade = (float)skillsSettings["HideoutManagement"]["SkillPointsPerAreaUpgrade"];
            EFM_Skill.skillPointsPerCraft = (float)skillsSettings["HideoutManagement"]["SkillPointsPerCraft"];
            EFM_Skill.generatorPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["Generator"]["ResourceSpent"];
            EFM_Skill.AFUPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["AirFilteringUnit"]["ResourceSpent"];
            EFM_Skill.waterCollectorPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["WaterCollector"]["ResourceSpent"];
            EFM_Skill.solarPowerPointsPerResourceSpent = (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["PointsGained"] / (float)skillsSettings["HideoutManagement"]["SkillPointsRate"]["SolarPower"]["ResourceSpent"];
            EFM_Skill.consumptionReductionPerLevel = (float)skillsSettings["HideoutManagement"]["ConsumptionReductionPerLevel"];
            EFM_Skill.skillBoostPercent = (float)skillsSettings["HideoutManagement"]["SkillBoostPercent"];

            // Crafting
            EFM_Skill.pointsPerHourCrafting = ((float)skillsSettings["Crafting"]["PointsPerCraftingCycle"] + (float)skillsSettings["Crafting"]["PointsPerUniqueCraftCycle"]) / (float)skillsSettings["Crafting"]["CraftingCycleHours"];
            EFM_Skill.craftTimeReductionPerLevel = (float)skillsSettings["Crafting"]["CraftTimeReductionPerLevel"];
            EFM_Skill.productionTimeReductionPerLevel = (float)skillsSettings["Crafting"]["ProductionTimeReductionPerLevel"];
            EFM_Skill.eliteExtraProductions = (float)skillsSettings["Crafting"]["EliteExtraProductions"];

            // Metabolism
            EFM_Skill.hydrationRecoveryRate = (float)skillsSettings["Metabolism"]["HydrationRecoveryRate"];
            EFM_Skill.energyRecoveryRate = (float)skillsSettings["Metabolism"]["EnergyRecoveryRate"];
            EFM_Skill.increasePositiveEffectDurationRate = (float)skillsSettings["Metabolism"]["IncreasePositiveEffectDurationRate"];
            EFM_Skill.decreaseNegativeEffectDurationRate = (float)skillsSettings["Metabolism"]["DecreaseNegativeEffectDurationRate"];
            EFM_Skill.decreasePoisonDurationRate = (float)skillsSettings["Metabolism"]["DecreasePoisonDurationRate"];

            // Immunity
            EFM_Skill.immunityMiscEffects = (float)skillsSettings["Immunity"]["ImmunityMiscEffects"];
            EFM_Skill.immunityPoisonBuff = (float)skillsSettings["Immunity"]["ImmunityPoisonBuff"];
            EFM_Skill.immunityPainKiller = (float)skillsSettings["Immunity"]["ImmunityPainKiller"];
            EFM_Skill.healthNegativeEffect = (float)skillsSettings["Immunity"]["HealthNegativeEffect"];
            EFM_Skill.stimulatorNegativeBuff = (float)skillsSettings["Immunity"]["StimulatorNegativeBuff"];

            // Endurance
            EFM_Skill.movementAction = (float)skillsSettings["Endurance"]["MovementAction"];
            EFM_Skill.sprintAction = (float)skillsSettings["Endurance"]["SprintAction"];
            EFM_Skill.gainPerFatigueStack = (float)skillsSettings["Endurance"]["GainPerFatigueStack"];

            // Strength
            EFM_Skill.sprintActionMin = (float)skillsSettings["Strength"]["SprintActionMin"];
            EFM_Skill.sprintActionMax = (float)skillsSettings["Strength"]["SprintActionMax"];
            EFM_Skill.movementActionMin = (float)skillsSettings["Strength"]["MovementActionMin"];
            EFM_Skill.movementActionMax = (float)skillsSettings["Strength"]["MovementActionMax"];
            EFM_Skill.pushUpMin = (float)skillsSettings["Strength"]["PushUpMin"];
            EFM_Skill.pushUpMax = (float)skillsSettings["Strength"]["PushUpMax"];
            EFM_Skill.fistfightAction = (float)skillsSettings["Strength"]["FistfightAction"];
            EFM_Skill.throwAction = (float)skillsSettings["Strength"]["ThrowAction"];

            // Vitality
            EFM_Skill.damageTakenAction = (float)skillsSettings["Vitality"]["DamageTakenAction"];
            EFM_Skill.vitalityHealthNegativeEffect = (float)skillsSettings["Vitality"]["HealthNegativeEffect"];

            // Health
            EFM_Skill.skillProgress = (float)skillsSettings["Health"]["SkillProgress"];

            // StressResistance
            EFM_Skill.stressResistanceHealthNegativeEffect = (float)skillsSettings["StressResistance"]["HealthNegativeEffect"];
            EFM_Skill.lowHPDuration = (float)skillsSettings["StressResistance"]["LowHPDuration"];

            // Throwing
            EFM_Skill.throwingThrowAction = (float)skillsSettings["Throwing"]["ThrowAction"];

            // RecoilControl
            EFM_Skill.recoilAction = (float)skillsSettings["RecoilControl"]["RecoilAction"];
            EFM_Skill.recoilBonusPerLevel = (float)skillsSettings["RecoilControl"]["RecoilBonusPerLevel"];

            // Pistol
            EFM_Skill.pistolWeaponReloadAction = (float)skillsSettings["Pistol"]["WeaponReloadAction"];
            EFM_Skill.pistolWeaponShotAction = (float)skillsSettings["Pistol"]["WeaponShotAction"];
            EFM_Skill.pistolWeaponChamberAction = (float)skillsSettings["Pistol"]["WeaponChamberAction"];

            // Revolver, uses Pistol values
            EFM_Skill.revolverWeaponReloadAction = (float)skillsSettings["Pistol"]["WeaponReloadAction"];
            EFM_Skill.revolverWeaponShotAction = (float)skillsSettings["Pistol"]["WeaponShotAction"];
            EFM_Skill.revolverWeaponChamberAction = (float)skillsSettings["Pistol"]["WeaponChamberAction"];

            // SMG, uses assault values
            EFM_Skill.SMGWeaponReloadAction = (float)skillsSettings["Assault"]["WeaponReloadAction"];
            EFM_Skill.SMGWeaponShotAction = (float)skillsSettings["Assault"]["WeaponShotAction"];
            EFM_Skill.SMGWeaponChamberAction = (float)skillsSettings["Assault"]["WeaponChamberAction"];

            // Assault
            EFM_Skill.assaultWeaponReloadAction = (float)skillsSettings["Assault"]["WeaponReloadAction"];
            EFM_Skill.assaultWeaponShotAction = (float)skillsSettings["Assault"]["WeaponShotAction"];
            EFM_Skill.assaultWeaponChamberAction = (float)skillsSettings["Assault"]["WeaponChamberAction"];

            // Shotgun
            EFM_Skill.shotgunWeaponReloadAction = (float)skillsSettings["Shotgun"]["WeaponReloadAction"];
            EFM_Skill.shotgunWeaponShotAction = (float)skillsSettings["Shotgun"]["WeaponShotAction"];
            EFM_Skill.shotgunWeaponChamberAction = (float)skillsSettings["Shotgun"]["WeaponChamberAction"];

            // Sniper
            EFM_Skill.sniperWeaponReloadAction = (float)skillsSettings["Sniper"]["WeaponReloadAction"];
            EFM_Skill.sniperWeaponShotAction = (float)skillsSettings["Sniper"]["WeaponShotAction"];
            EFM_Skill.sniperWeaponChamberAction = (float)skillsSettings["Sniper"]["WeaponChamberAction"];

            // HMG, uses DMR values
            EFM_Skill.HMGWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            EFM_Skill.HMGWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            EFM_Skill.HMGWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // LMG, uses DMR values
            EFM_Skill.LMGWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            EFM_Skill.LMGWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            EFM_Skill.LMGWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // Launcher, uses DMR values
            EFM_Skill.launcherWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            EFM_Skill.launcherWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            EFM_Skill.launcherWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // AttachedLauncher, uses DMR values
            EFM_Skill.attachedLauncherWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            EFM_Skill.attachedLauncherWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            EFM_Skill.attachedLauncherWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // DMR
            EFM_Skill.DMRWeaponReloadAction = (float)skillsSettings["DMR"]["WeaponReloadAction"];
            EFM_Skill.DMRWeaponShotAction = (float)skillsSettings["DMR"]["WeaponShotAction"];
            EFM_Skill.DMRWeaponChamberAction = (float)skillsSettings["DMR"]["WeaponChamberAction"];

            // CovertMovement
            EFM_Skill.covertMovementAction = (float)skillsSettings["CovertMovement"]["MovementAction"];

            // Search
            EFM_Skill.searchAction = (float)skillsSettings["Search"]["SearchAction"];
            EFM_Skill.findAction = (float)skillsSettings["Search"]["FindAction"];

            // MagDrills
            EFM_Skill.raidLoadedAmmoAction = (float)skillsSettings["MagDrills"]["RaidLoadedAmmoAction"];
            EFM_Skill.raidUnloadedAmmoAction = (float)skillsSettings["MagDrills"]["RaidUnloadedAmmoAction"];
            EFM_Skill.magazineCheckAction = (float)skillsSettings["MagDrills"]["MagazineCheckAction"];

            // Perception
            EFM_Skill.onlineAction = (float)skillsSettings["Perception"]["OnlineAction"];
            EFM_Skill.uniqueLoot = (float)skillsSettings["Perception"]["UniqueLoot"];

            // Intellect
            EFM_Skill.examineAction = (float)skillsSettings["Intellect"]["ExamineAction"];
            EFM_Skill.intellectSkillProgress = (float)skillsSettings["Intellect"]["SkillProgress"];

            // Attention
            EFM_Skill.examineWithInstruction = (float)skillsSettings["Attention"]["ExamineWithInstruction"];
            EFM_Skill.findActionFalse = (float)skillsSettings["Attention"]["FindActionFalse"];
            EFM_Skill.findActionTrue = (float)skillsSettings["Attention"]["FindActionTrue"];

            // Charisma
            EFM_Skill.skillProgressInt = (float)skillsSettings["Charisma"]["SkillProgressInt"];
            EFM_Skill.skillProgressAtn = (float)skillsSettings["Charisma"]["SkillProgressAtn"];
            EFM_Skill.skillProgressPer = (float)skillsSettings["Charisma"]["SkillProgressPer"];

            // Memory
            EFM_Skill.anySkillUp = (float)skillsSettings["Memory"]["AnySkillUp"];
            EFM_Skill.memorySkillProgress = (float)skillsSettings["Memory"]["SkillProgress"];

            // Surgery
            EFM_Skill.surgeryAction = (float)skillsSettings["Surgery"]["SurgeryAction"];
            EFM_Skill.surgerySkillProgress = (float)skillsSettings["Surgery"]["SkillProgress"];

            // AimDrills
            EFM_Skill.weaponShotAction = (float)skillsSettings["AimDrills"]["WeaponShotAction"];
        }

        private void LoadDefaultAssets()
        {
            defaultAssetsBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovDefaultAssets.ab");

            sceneDefImage = defaultAssetsBundle.LoadAsset<Sprite>("MeatovThumbnail");
            mainMenuPointable = defaultAssetsBundle.LoadAsset<GameObject>("MeatovPointable");
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

            vanillaItems = new Dictionary<string, EFM_VanillaItemDescriptor>();
            JArray vanillaItemsRaw = (JArray)defaultItemsData["VanillaItems"];

            foreach (JToken vanillaItemRaw in vanillaItemsRaw)
            {
                string H3ID = vanillaItemRaw["H3ID"].ToString();
                Mod.instance.LogInfo("Setting vanilla Item: " + H3ID);
                GameObject itemPrefab = IM.OD[H3ID].GetGameObject();
                EFM_VanillaItemDescriptor descriptor = itemPrefab.AddComponent<EFM_VanillaItemDescriptor>();
                itemWeights.Add(H3ID, (int)(itemPrefab.GetComponent<Rigidbody>().mass * 1000));
                descriptor.H3ID = vanillaItemRaw["H3ID"].ToString();
                descriptor.tarkovID = vanillaItemRaw["TarkovID"].ToString();
                descriptor.description = vanillaItemRaw["Description"].ToString();
                itemDescriptions.Add(H3ID, descriptor.description);
                descriptor.lootExperience = (int)vanillaItemRaw["LootExperience"];
                descriptor.rarity = ItemRarityStringToEnum(vanillaItemRaw["Rarity"].ToString());
                if (itemsByRarity.ContainsKey(descriptor.rarity))
                {
                    itemsByRarity[descriptor.rarity].Add(descriptor.H3ID);
                }
                else
                {
                    itemsByRarity.Add(descriptor.rarity, new List<string>() { descriptor.H3ID });
                }
                descriptor.spawnChance = (float)vanillaItemRaw["SpawnChance"];
                descriptor.creditCost = (int)vanillaItemRaw["CreditCost"];
                descriptor.parents = vanillaItemRaw["parents"].ToObject<List<string>>();
                itemAncestors.Add(descriptor.H3ID, descriptor.parents);
                descriptor.lootExperience = (int)vanillaItemRaw["LootExperience"];

                foreach (string parent in descriptor.parents)
                {
                    if (itemsByParents.ContainsKey(parent))
                    {
                        itemsByParents[parent].Add(H3ID);
                    }
                    else
                    {
                        itemsByParents.Add(parent, new List<string>() { H3ID });
                    }

                    if (parent.Equals("5447b5cf4bdc2d65278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.Pistol;
                    }
                    else if (parent.Equals("617f1ef5e8b54b0998387733"))
                    {
                        descriptor.weaponClass = WeaponClass.Revolver;
                    }
                    else if (parent.Equals("5447b5e04bdc2d62278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.SMG;
                    }
                    else if (parent.Equals("5447b5f14bdc2d61278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.Assault;
                    }
                    else if (parent.Equals("5447b6094bdc2dc3278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.Shotgun;
                    }
                    else if (parent.Equals("5447b6254bdc2dc3278b4568"))
                    {
                        descriptor.weaponClass = WeaponClass.Sniper;
                    }
                    else if (parent.Equals("5447b5fc4bdc2d87278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.LMG;
                    }
                    else if (parent.Equals("5447bed64bdc2d97278b4568"))
                    {
                        descriptor.weaponClass = WeaponClass.HMG;
                    }
                    else if (parent.Equals("5447bedf4bdc2d87278b4568"))
                    {
                        descriptor.weaponClass = WeaponClass.Launcher;
                    }
                    else if (parent.Equals("5447bee84bdc2dc3278b4569"))
                    {
                        descriptor.weaponClass = WeaponClass.AttachedLauncher;
                    }
                    else if (parent.Equals("5447b6194bdc2d67278b4567"))
                    {
                        descriptor.weaponClass = WeaponClass.DMR;
                    }
                }

                FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
                descriptor.itemName = physObj.ObjectWrapper.DisplayName;
                itemNames.Add(H3ID, descriptor.itemName);
                descriptor.volume = (int)((float)vanillaItemRaw["Volume"] * Mod.volumePrecisionMultiplier);
                itemVolumes.Add(H3ID, descriptor.volume);
                if (physObj is FVRFireArm)
                {
                    FVRFireArm asFireArm = physObj as FVRFireArm;
                    descriptor.compatibilityValue = 3;
                    if (asFireArm.UsesMagazines)
                    {
                        descriptor.usesAmmoContainers = true;
                        descriptor.usesMags = true;
                        descriptor.magType = asFireArm.MagazineType;
                    }
                    else if (asFireArm.UsesClips)
                    {
                        descriptor.usesAmmoContainers = true;
                        descriptor.usesMags = false;
                        descriptor.clipType = asFireArm.ClipType;
                    }
                    descriptor.roundType = asFireArm.RoundType;
                }
                else if (physObj is FVRFireArmMagazine)
                {
                    descriptor.compatibilityValue = 1;
                    descriptor.roundType = (physObj as FVRFireArmMagazine).RoundType;
                }
                else if (physObj is FVRFireArmClip)
                {
                    descriptor.compatibilityValue = 1;
                    descriptor.roundType = (physObj as FVRFireArmClip).RoundType;
                }
                else if (physObj is Speedloader)
                {
                    descriptor.compatibilityValue = 1;
                    descriptor.roundType = (physObj as Speedloader).Chambers[0].Type;
                }
                else if (physObj is FVRFireArmRound)
                {
                    usedRoundIDs.Add(H3ID);

                    // TODO: Figure out how to get a round's compatible mags efficiently so we can list them in the round's description
                }

                vanillaItems.Add(descriptor.H3ID, descriptor);
            }
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

            Dictionary<string, JObject> itemMapData = JObject.Parse(File.ReadAllText("BepinEx/Plugins/EscapeFromMeatov/DB/ItemMap.json")).ToObject<Dictionary<string, JObject>>();

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
            EFM_OtherInteractable otherInteractable = root.gameObject.AddComponent<EFM_OtherInteractable>();
            otherInteractable.interactiveObject = itemPhysicalObject;

            // Make its children interactable
            foreach (Transform interactive in root)
            {
                MakeItemInteractiveSet(interactive, itemPhysicalObject);
            }

            return root.gameObject;
        }

        public static void AddToAll(FVRInteractiveObject interactiveObject, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            if (CIW != null && !CIW.inAll)
            {
                typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(interactiveObject, FVRInteractiveObject.All.Count);
                FVRInteractiveObject.All.Add(interactiveObject);

                CIW.inAll = true;
            }
            else if (VID != null && !VID.inAll)
            {
                typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(interactiveObject, FVRInteractiveObject.All.Count);
                FVRInteractiveObject.All.Add(interactiveObject);

                VID.inAll = true;
            }
        }

        public static void RemoveFromAll(FVRInteractiveObject interactiveObject, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            if (CIW != null && (CIW.inAll || interactiveObject == null))
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

                CIW.inAll = false;
            }
            else if (VID != null && (VID.inAll || interactiveObject == null))
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

                VID.inAll = false;
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
            foreach (EFM_EquipmentSlot equipSlot in equipmentSlots)
            {
                if (equipSlot.CurObject != null)
                {
                    AddToPlayerInventory(equipSlot.CurObject.transform, true);
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
            EFM_CustomItemWrapper customItemWrapper = item.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor vanillaItemDescriptor = item.GetComponent<EFM_VanillaItemDescriptor>();
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
                }

                if (vanillaItemDescriptor != null)
                {
                    if (vanillaItemDescriptor.physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = vanillaItemDescriptor.physObj as FVRFireArmMagazine;
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
                    else if (vanillaItemDescriptor.physObj is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = vanillaItemDescriptor.physObj as FVRFireArmClip;
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
                    else if (vanillaItemDescriptor.physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = vanillaItemDescriptor.physObj as FVRFireArmRound;
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
            }
            else if (vanillaItemDescriptor != null)
            {
                if (physObj is FVRFireArm)
                {
                    FVRFireArm asFireArm = (FVRFireArm)physObj;

                    // Ammo container
                    if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                    {
                        EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Magazine.GetComponent<EFM_VanillaItemDescriptor>();
                        if (ammoContainerVID != null)
                        {
                            AddToPlayerInventory(asFireArm.Magazine.transform, updateTypeLists);
                        }
                        //else Mag could be internal, not interactable, and so, no VID
                    }
                    else if (asFireArm.UsesClips && asFireArm.Clip != null)
                    {
                        EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Clip.GetComponent<EFM_VanillaItemDescriptor>();
                        if (ammoContainerVID != null)
                        {
                            AddToPlayerInventory(asFireArm.Clip.transform, updateTypeLists);
                        }
                        //else Clip could be internal, not interactable, and so, no VID
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
            EFM_CustomItemWrapper customItemWrapper = item.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor vanillaItemDescriptor = item.GetComponent<EFM_VanillaItemDescriptor>();
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
                Mod.instance.LogError("Attempting to remove " + itemID + " from player inventory but key was not found in it:\n" + Environment.StackTrace);
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
                                    Mod.instance.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo name was not found in roundsByType:\n" + Environment.StackTrace);
                                }
                            }
                            else
                            {
                                Mod.instance.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from player inventory but ammo type was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                    }
                }

                if (vanillaItemDescriptor != null)
                {
                    if (vanillaItemDescriptor.physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = vanillaItemDescriptor.physObj as FVRFireArmMagazine;
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
                                Mod.instance.LogError("Attempting to remove " + itemID + "  which is mag from player inventory but its name was not found in magazinesByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Attempting to remove " + itemID + "  which is mag from player inventory but its type was not found in magazinesByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = vanillaItemDescriptor.physObj as FVRFireArmClip;
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
                                Mod.instance.LogError("Attempting to remove " + itemID + "  which is clip from player inventory but its name was not found in clipsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Attempting to remove " + itemID + "  which is clip from player inventory but its type was not found in clipsByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = vanillaItemDescriptor.physObj as FVRFireArmRound;
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
                                Mod.instance.LogError("Attempting to remove " + itemID + "  which is round from player inventory but its name was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Attempting to remove " + itemID + "  which is round from player inventory but its type was not found in roundsByType:\n" + Environment.StackTrace);
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
            }
            else if (vanillaItemDescriptor != null)
            {
                if (physObj is FVRFireArm)
                {
                    FVRFireArm asFireArm = (FVRFireArm)physObj;

                    // Ammo container
                    if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                    {
                        EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Magazine.GetComponent<EFM_VanillaItemDescriptor>();
                        if (ammoContainerVID != null)
                        {
                            RemoveFromPlayerInventory(asFireArm.Magazine.transform, updateTypeLists);
                        }
                        //else Mag could be internal, not interactable, and so, no VID
                    }
                    else if (asFireArm.UsesClips && asFireArm.Clip != null)
                    {
                        EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Clip.GetComponent<EFM_VanillaItemDescriptor>();
                        if (ammoContainerVID != null)
                        {
                            RemoveFromPlayerInventory(asFireArm.Clip.transform, updateTypeLists);
                        }
                        //else Clip could be internal, not interactable, and so, no VID
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
            xp = (int)(xp * (EFM_Base_Manager.currentExperienceRate + EFM_Base_Manager.currentExperienceRate * (EFM_Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100)));

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
                playerStatusManager.UpdatePlayerLevel();
                if (currentLocationIndex == 1) // In hideout
                {
                    currentBaseManager.UpdateBasedOnPlayerLevel();
                }

                // Update level task conditions
                if (taskStartConditionsByType.ContainsKey(TraderTaskCondition.ConditionType.Level))
                {
                    foreach (TraderTaskCondition condition in taskStartConditionsByType[TraderTaskCondition.ConditionType.Level])
                    {
                        EFM_TraderStatus.UpdateConditionFulfillment(condition);
                    }
                }
            }

            if (type == 1)
            {
                Mod.lootingExp += xp;
                if (notifMsg == null)
                {
                    playerStatusManager.AddNotification(string.Format("Gained {0} looting experience.", xp));
                }
                else
                {
                    playerStatusManager.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else if (type == 2)
            {
                Mod.healingExp += xp;
                if (notifMsg == null)
                {
                    playerStatusManager.AddNotification(string.Format("Gained {0} healing experience.", xp));
                }
                else
                {
                    playerStatusManager.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else if (type == 3)
            {
                Mod.explorationExp += xp;
                if (notifMsg == null)
                {
                    playerStatusManager.AddNotification(string.Format("Gained {0} exploration experience.", xp));
                }
                else
                {
                    playerStatusManager.AddNotification(string.Format(notifMsg, xp));
                }
            }
            else
            {
                playerStatusManager.AddNotification(string.Format("Gained {0} experience.", xp));
            }
        }

        public static void AddSkillExp(float xp, int skillIndex)
        {
            // Globals SkillsSettings
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 1 || Mod.chosenCharIndex == 1 || Mod.skills[skillIndex].raidProgress >= 300) // Max 3 levels per raid TODO: should be unique to each skill
            {
                return;
            }

            float preLevel = Mod.skills[skillIndex].progress / 100;

            float actualAmountToAdd = xp * ((skillIndex >= 12 && skillIndex <= 24) ? EFM_Skill.weaponSkillProgressRate : EFM_Skill.skillProgressRate);
            actualAmountToAdd += actualAmountToAdd * (EFM_Base_Manager.currentSkillGroupLevelingBoosts.ContainsKey(Mod.skills[skillIndex].skillType) ? EFM_Base_Manager.currentSkillGroupLevelingBoosts[Mod.skills[skillIndex].skillType] : 0);
            actualAmountToAdd *= Mod.skills[skillIndex].dimishingReturns ? 0.5f : 1;

            Mod.skills[skillIndex].progress += actualAmountToAdd;
            Mod.skills[skillIndex].currentProgress += actualAmountToAdd;

            Mod.skills[skillIndex].raidProgress += actualAmountToAdd;
            if (Mod.skills[skillIndex].raidProgress >= 200) // dimishing returns at 2 levels per raid TODO: should be unique to each skill
            {
                Mod.skills[skillIndex].dimishingReturns = true;
                Mod.skills[skillIndex].increasing = false;
            }
            else
            {
                Mod.skills[skillIndex].increasing = true;
            }

            if (Mod.skills[skillIndex].skillType == EFM_Skill.SkillType.Practical || Mod.skills[skillIndex].skillType == EFM_Skill.SkillType.Physical)
            {
                float memoryAmount = EFM_Skill.memorySkillProgress / EFM_Skill.anySkillUp * actualAmountToAdd;
                Mod.skills[11].progress += memoryAmount;
                Mod.skills[11].currentProgress += memoryAmount;

                Mod.playerStatusManager.UpdateSkillUI(11);
            }

            float postLevel = Mod.skills[skillIndex].progress / 100;

            if (postLevel != preLevel)
            {
                foreach (TraderTaskCondition condition in Mod.taskSkillConditionsBySkillIndex[skillIndex])
                {
                    EFM_TraderStatus.UpdateConditionFulfillment(condition);
                }
            }

            // Skill specific stuff
            if (skillIndex == 0)
            {
                Mod.maxStamina += (postLevel - preLevel);

                float healthAmount = EFM_Skill.skillProgress * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(3);
            }
            else if (skillIndex == 1)
            {
                Mod.skillWeightLimitBonus += (postLevel - preLevel);
                Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);

                float healthAmount = EFM_Skill.skillProgress * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(3);
            }
            else if (skillIndex == 2)
            {
                float healthAmount = EFM_Skill.skillProgress * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(3);
            }
            else if (skillIndex == 7)
            {
                float charismaAmount = EFM_Skill.skillProgressPer * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(10);
            }
            else if (skillIndex == 8)
            {
                float charismaAmount = EFM_Skill.skillProgressInt * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(10);
            }
            else if (skillIndex == 9)
            {
                float charismaAmount = EFM_Skill.skillProgressAtn * actualAmountToAdd;
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

                Mod.playerStatusManager.UpdateSkillUI(10);
            }

            Mod.playerStatusManager.UpdateSkillUI(skillIndex);
        }

        private void DoPatching()
        {
            var harmony = new HarmonyLib.Harmony("VIP.TommySoucy.EscapeFromMeatov");

            // LoadLevelBeginPatch
            MethodInfo loadLevelBeginPatchOriginal = typeof(SteamVR_LoadLevel).GetMethod("Begin", BindingFlags.Public | BindingFlags.Static);
            MethodInfo loadLevelBeginPatchPrefix = typeof(LoadLevelBeginPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(loadLevelBeginPatchOriginal, new HarmonyMethod(loadLevelBeginPatchPrefix));

            // EndInteractionPatch
            MethodInfo endInteractionPatchOriginal = typeof(FVRInteractiveObject).GetMethod("EndInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo endInteractionPatchPostfix = typeof(EndInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(endInteractionPatchOriginal, null, new HarmonyMethod(endInteractionPatchPostfix));

            // ConfigureQuickbeltPatch
            MethodInfo configureQuickbeltPatchOriginal = typeof(FVRPlayerBody).GetMethod("ConfigureQuickbelt", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo configureQuickbeltPatchPrefix = typeof(ConfigureQuickbeltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo configureQuickbeltPatchPostfix = typeof(ConfigureQuickbeltPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(configureQuickbeltPatchOriginal, new HarmonyMethod(configureQuickbeltPatchPrefix), new HarmonyMethod(configureQuickbeltPatchPostfix));

            // TestQuickbeltPatch
            MethodInfo testQuickbeltPatchOriginal = typeof(FVRViveHand).GetMethod("TestQuickBeltDistances", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo testQuickbeltPatchPrefix = typeof(TestQuickbeltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(testQuickbeltPatchOriginal, new HarmonyMethod(testQuickbeltPatchPrefix));

            // SetQuickBeltSlotPatch
            MethodInfo setQuickBeltSlotPatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetQuickBeltSlot", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo setQuickBeltSlotPatchPrefix = typeof(SetQuickBeltSlotPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo setQuickBeltSlotPatchPostfix = typeof(SetQuickBeltSlotPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(setQuickBeltSlotPatchOriginal, new HarmonyMethod(setQuickBeltSlotPatchPrefix), new HarmonyMethod(setQuickBeltSlotPatchPostfix));

            // BeginInteractionPatch
            MethodInfo beginInteractionPatchOriginal = typeof(FVRPhysicalObject).GetMethod("BeginInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo beginInteractionPatchPrefix = typeof(BeginInteractionPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo beginInteractionPatchPostfix = typeof(BeginInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(beginInteractionPatchOriginal, new HarmonyMethod(beginInteractionPatchPrefix), new HarmonyMethod(beginInteractionPatchPostfix));

            // DamagePatch
            MethodInfo damagePatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(Damage) }, null);
            MethodInfo damagePatchPrefix = typeof(DamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(damagePatchOriginal, new HarmonyMethod(damagePatchPrefix));

            // DamageFloatPatch
            MethodInfo damageFloatPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(float) }, null);
            MethodInfo damageFloatPatchPrefix = typeof(DamageFloatPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(damageFloatPatchOriginal, new HarmonyMethod(damageFloatPatchPrefix));

            // DamageDealtPatch
            MethodInfo damageDealtPatchOriginal = typeof(FVRPlayerHitbox).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(DamageDealt) }, null);
            MethodInfo damageDealtPatchPrefix = typeof(DamageDealtPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(damageDealtPatchOriginal, new HarmonyMethod(damageDealtPatchPrefix));

            // HandTestColliderPatch
            MethodInfo handTestColliderPatchOriginal = typeof(FVRViveHand).GetMethod("TestCollider", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handTestColliderPatchPrefix = typeof(HandTestColliderPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handTestColliderPatchOriginal, new HarmonyMethod(handTestColliderPatchPrefix));

            // HandTriggerExitPatch
            MethodInfo handTriggerExitPatchOriginal = typeof(FVRViveHand).GetMethod("HandTriggerExit", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handTriggerExitPatchPrefix = typeof(HandTriggerExitPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handTriggerExitPatchOriginal, new HarmonyMethod(handTriggerExitPatchPrefix));

            // KeyForwardBackPatch
            MethodInfo keyForwardBackPatchOriginal = typeof(SideHingedDestructibleDoorDeadBoltKey).GetMethod("KeyForwardBack", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo keyForwardBackPatchPrefix = typeof(KeyForwardBackPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(keyForwardBackPatchOriginal, new HarmonyMethod(keyForwardBackPatchPrefix));

            // UpdateDisplayBasedOnTypePatch
            MethodInfo updateDisplayBasedOnTypePatchOriginal = typeof(SideHingedDestructibleDoorDeadBoltKey).GetMethod("UpdateDisplayBasedOnType", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo updateDisplayBasedOnTypePatchPrefix = typeof(UpdateDisplayBasedOnTypePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(updateDisplayBasedOnTypePatchOriginal, new HarmonyMethod(updateDisplayBasedOnTypePatchPrefix));

            // DoorInitPatch
            MethodInfo doorInitPatchOriginal = typeof(SideHingedDestructibleDoor).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo doorInitPatchPrefix = typeof(DoorInitPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(doorInitPatchOriginal, new HarmonyMethod(doorInitPatchPrefix));

            // DeadBoltAwakePatch
            MethodInfo deadBoltAwakePatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo deadBoltAwakePatchPostfix = typeof(DeadBoltAwakePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(deadBoltAwakePatchOriginal, null, new HarmonyMethod(deadBoltAwakePatchPostfix));

            // DeadBoltFVRFixedUpdatePatch
            MethodInfo deadBoltFVRFixedUpdatePatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("FVRFixedUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo deadBoltFVRFixedUpdatePatchPostfix = typeof(DeadBoltFVRFixedUpdatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(deadBoltFVRFixedUpdatePatchOriginal, null, new HarmonyMethod(deadBoltFVRFixedUpdatePatchPostfix));

            // InteractiveSetAllLayersPatch
            MethodInfo interactiveSetAllLayersPatchOriginal = typeof(FVRInteractiveObject).GetMethod("SetAllCollidersToLayer", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo interactiveSetAllLayersPatchPrefix = typeof(InteractiveSetAllLayersPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(interactiveSetAllLayersPatchOriginal, new HarmonyMethod(interactiveSetAllLayersPatchPrefix));

            // HandUpdatePatch
            MethodInfo handUpdatePatchOriginal = typeof(FVRViveHand).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo handUpdatePatchPrefix = typeof(HandUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handUpdatePatchOriginal, new HarmonyMethod(handUpdatePatchPrefix));

            // MagazineUpdateInteractionPatch
            MethodInfo magazineUpdateInteractionPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("UpdateInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo magazineUpdateInteractionPatchPostfix = typeof(MagazineUpdateInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magazineUpdateInteractionPatchTranspiler = typeof(MagazineUpdateInteractionPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magazineUpdateInteractionPatchOriginal, null, new HarmonyMethod(magazineUpdateInteractionPatchPostfix), new HarmonyMethod(magazineUpdateInteractionPatchTranspiler));

            // ClipUpdateInteractionPatch
            MethodInfo clipUpdateInteractionPatchOriginal = typeof(FVRFireArmClip).GetMethod("UpdateInteraction", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipUpdateInteractionPatchPostfix = typeof(ClipUpdateInteractionPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipUpdateInteractionPatchTranspiler = typeof(ClipUpdateInteractionPatch).GetMethod("Transpiler", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipUpdateInteractionPatchOriginal, null, new HarmonyMethod(clipUpdateInteractionPatchPostfix), new HarmonyMethod(clipUpdateInteractionPatchTranspiler));

            // MovementManagerJumpPatch
            MethodInfo movementManagerJumpPatchOriginal = typeof(FVRMovementManager).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerJumpPatchPrefix = typeof(MovementManagerJumpPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(movementManagerJumpPatchOriginal, new HarmonyMethod(movementManagerJumpPatchPrefix));

            // MovementManagerTwinstickPatch
            MethodInfo movementManagerTwinstickPatchOriginal = typeof(FVRMovementManager).GetMethod("HandUpdateTwinstick", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo movementManagerTwinstickPatchPrefix = typeof(MovementManagerUpdatePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(movementManagerTwinstickPatchOriginal, new HarmonyMethod(movementManagerTwinstickPatchPrefix));

            // ChamberSetRoundPatch
            MethodInfo chamberSetRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("SetRound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo chamberSetRoundPatchPrefix = typeof(ChamberSetRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(chamberSetRoundPatchOriginal, new HarmonyMethod(chamberSetRoundPatchPrefix));

            // MagRemoveRoundPatch
            MethodInfo magRemoveRoundPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo magRemoveRoundPatchPrefix = typeof(MagRemoveRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magRemoveRoundPatchPostfix = typeof(MagRemoveRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magRemoveRoundPatchOriginal, new HarmonyMethod(magRemoveRoundPatchPrefix), new HarmonyMethod(magRemoveRoundPatchPostfix));

            // MagRemoveRoundBoolPatch
            MethodInfo magRemoveRoundBoolPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo magRemoveRoundBoolPatchPrefix = typeof(MagRemoveRoundBoolPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magRemoveRoundBoolPatchPostfix = typeof(MagRemoveRoundBoolPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magRemoveRoundBoolPatchOriginal, new HarmonyMethod(magRemoveRoundBoolPatchPrefix), new HarmonyMethod(magRemoveRoundBoolPatchPostfix));

            // MagRemoveRoundIntPatch
            MethodInfo magRemoveRoundIntPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(int) }, null);
            MethodInfo magRemoveRoundIntPatchPrefix = typeof(MagRemoveRoundIntPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magRemoveRoundIntPatchPostfix = typeof(MagRemoveRoundIntPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magRemoveRoundIntPatchOriginal, new HarmonyMethod(magRemoveRoundIntPatchPrefix), new HarmonyMethod(magRemoveRoundIntPatchPostfix));

            // ClipRemoveRoundPatch
            MethodInfo clipRemoveRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { }, null);
            MethodInfo clipRemoveRoundPatchPrefix = typeof(ClipRemoveRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipRemoveRoundPatchPostfix = typeof(ClipRemoveRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipRemoveRoundPatchOriginal, new HarmonyMethod(clipRemoveRoundPatchPrefix), new HarmonyMethod(clipRemoveRoundPatchPostfix));

            // ClipRemoveRoundBoolPatch
            MethodInfo clipRemoveRoundBoolPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            MethodInfo clipRemoveRoundBoolPatchPrefix = typeof(ClipRemoveRoundBoolPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipRemoveRoundBoolPatchPostfix = typeof(ClipRemoveRoundBoolPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipRemoveRoundBoolPatchOriginal, new HarmonyMethod(clipRemoveRoundBoolPatchPrefix), new HarmonyMethod(clipRemoveRoundBoolPatchPostfix));

            // ClipRemoveRoundClassPatch
            MethodInfo clipRemoveRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("RemoveRoundReturnClass", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo clipRemoveRoundClassPatchPrefix = typeof(ClipRemoveRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipRemoveRoundClassPatchPostfix = typeof(ClipRemoveRoundClassPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipRemoveRoundClassPatchOriginal, new HarmonyMethod(clipRemoveRoundClassPatchPrefix), new HarmonyMethod(clipRemoveRoundClassPatchPostfix));

            // FireArmLoadMagPatch
            MethodInfo fireArmLoadMagPatchOriginal = typeof(FVRFireArm).GetMethod("LoadMag", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmLoadMagPatchPrefix = typeof(FireArmLoadMagPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmLoadMagPatchOriginal, new HarmonyMethod(fireArmLoadMagPatchPrefix));

            // FireArmEjectMagPatch
            MethodInfo fireArmEjectMagPatchOriginal = typeof(FVRFireArm).GetMethod("EjectMag", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmEjectMagPatchPrefix = typeof(FireArmEjectMagPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireArmEjectMagPatchPostfix = typeof(FireArmEjectMagPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmEjectMagPatchOriginal, new HarmonyMethod(fireArmEjectMagPatchPrefix), new HarmonyMethod(fireArmEjectMagPatchPostfix));

            // FireArmLoadClipPatch
            MethodInfo fireArmLoadClipPatchOriginal = typeof(FVRFireArm).GetMethod("LoadClip", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmLoadClipPatchPrefix = typeof(FireArmLoadClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmLoadClipPatchOriginal, new HarmonyMethod(fireArmLoadClipPatchPrefix));

            // FireArmEjectClipPatch
            MethodInfo fireArmEjectClipPatchOriginal = typeof(FVRFireArm).GetMethod("EjectClip", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmEjectClipPatchPrefix = typeof(FireArmEjectClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo fireArmEjectClipPatchPostfix = typeof(FireArmEjectClipPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmEjectClipPatchOriginal, new HarmonyMethod(fireArmEjectClipPatchPrefix), new HarmonyMethod(fireArmEjectClipPatchPostfix));

            // MagAddRoundPatch
            MethodInfo magAddRoundPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundPatchPrefix = typeof(MagAddRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magAddRoundPatchPostfix = typeof(MagAddRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magAddRoundPatchOriginal, new HarmonyMethod(magAddRoundPatchPrefix), new HarmonyMethod(magAddRoundPatchPostfix));

            // MagAddRoundClassPatch
            MethodInfo magAddRoundClassPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo magAddRoundClassPatchPrefix = typeof(MagAddRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo magAddRoundClassPatchPostfix = typeof(MagAddRoundClassPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(magAddRoundClassPatchOriginal, new HarmonyMethod(magAddRoundClassPatchPrefix), new HarmonyMethod(magAddRoundClassPatchPostfix));

            // ClipAddRoundPatch
            MethodInfo clipAddRoundPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FVRFireArmRound), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundPatchPrefix = typeof(ClipAddRoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipAddRoundPatchPostfix = typeof(ClipAddRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipAddRoundPatchOriginal, new HarmonyMethod(clipAddRoundPatchPrefix), new HarmonyMethod(clipAddRoundPatchPostfix));

            // ClipAddRoundClassPatch
            MethodInfo clipAddRoundClassPatchOriginal = typeof(FVRFireArmClip).GetMethod("AddRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(FireArmRoundClass), typeof(bool), typeof(bool) }, null);
            MethodInfo clipAddRoundClassPatchPrefix = typeof(ClipAddRoundClassPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo clipAddRoundClassPatchPostfix = typeof(ClipAddRoundClassPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(clipAddRoundClassPatchOriginal, new HarmonyMethod(clipAddRoundClassPatchPrefix), new HarmonyMethod(clipAddRoundClassPatchPostfix));

            // AttachmentMountRegisterPatch
            MethodInfo attachmentMountRegisterPatchOriginal = typeof(FVRFireArmAttachmentMount).GetMethod("RegisterAttachment", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo attachmentMountRegisterPatchPrefix = typeof(AttachmentMountRegisterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(attachmentMountRegisterPatchOriginal, new HarmonyMethod(attachmentMountRegisterPatchPrefix));

            // AttachmentMountDeRegisterPatch
            MethodInfo attachmentMountDeRegisterPatchOriginal = typeof(FVRFireArmAttachmentMount).GetMethod("DeRegisterAttachment", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo attachmentMountDeRegisterPatchPrefix = typeof(AttachmentMountDeRegisterPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(attachmentMountDeRegisterPatchOriginal, new HarmonyMethod(attachmentMountDeRegisterPatchPrefix));

            // EntityCheckPatch
            MethodInfo entityCheckPatchOriginal = typeof(AIManager).GetMethod("EntityCheck", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo entityCheckPatchPrefix = typeof(EntityCheckPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(entityCheckPatchOriginal, new HarmonyMethod(entityCheckPatchPrefix));

            // ChamberEjectRoundPatch
            MethodInfo chamberEjectRoundPatchOriginal = typeof(FVRFireArmChamber).GetMethod("EjectRound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo chamberEjectRoundPatchPostfix = typeof(ChamberEjectRoundPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(chamberEjectRoundPatchOriginal, null, new HarmonyMethod(chamberEjectRoundPatchPostfix));

            // GlobalFixedUpdatePatch
            MethodInfo globalFixedUpdatePatchOriginal = typeof(FVRInteractiveObject).GetMethod("GlobalFixedUpdate", BindingFlags.Public | BindingFlags.Static);
            MethodInfo globalFixedUpdatePatchPostfix = typeof(GlobalFixedUpdatePatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(globalFixedUpdatePatchOriginal, null, new HarmonyMethod(globalFixedUpdatePatchPostfix));

            // IsPointInsideSphereGeoPatch
            MethodInfo isPointInsideSphereGeoPatchOriginal = typeof(FVRQuickBeltSlot).GetMethod("IsPointInsideSphereGeo", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo isPointInsideSphereGeoPatchPrefix = typeof(IsPointInsideSphereGeoPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(isPointInsideSphereGeoPatchOriginal, new HarmonyMethod(isPointInsideSphereGeoPatchPrefix));

            // PlayGrabSoundPatch
            MethodInfo playGrabSoundPatchOriginal = typeof(FVRInteractiveObject).GetMethod("PlayGrabSound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo playGrabSoundPatchPrefix = typeof(PlayGrabSoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(playGrabSoundPatchOriginal, new HarmonyMethod(playGrabSoundPatchPrefix));

            // PlayReleaseSoundPatch
            MethodInfo playReleaseSoundPatchOriginal = typeof(FVRInteractiveObject).GetMethod("PlayReleaseSound", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo playReleaseSoundPatchPrefix = typeof(PlayReleaseSoundPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(playReleaseSoundPatchOriginal, new HarmonyMethod(playReleaseSoundPatchPrefix));

            // FireArmFirePatch
            MethodInfo fireArmFirePatchOriginal = typeof(FVRFireArm).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmFirePatchPrefix = typeof(FireArmFirePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmFirePatchOriginal, new HarmonyMethod(fireArmFirePatchPrefix));

            // FireArmRecoilPatch
            MethodInfo fireArmRecoilPatchOriginal = typeof(FVRFireArm).GetMethod("Recoil", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmRecoilPatchPrefix = typeof(FireArmRecoilPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmRecoilPatchOriginal, new HarmonyMethod(fireArmRecoilPatchPrefix));

            // HandCurrentInteractableSetPatch
            MethodInfo handCurrentInteractableSetPatchOriginal = typeof(FVRViveHand).GetMethod("set_CurrentInteractable", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo handCurrentInteractableSetPatchPostfix = typeof(HandCurrentInteractableSetPatch).GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(handCurrentInteractableSetPatchOriginal, null, new HarmonyMethod(handCurrentInteractableSetPatchPostfix));

            // SosigLinkDamagePatch
            MethodInfo sosigLinkDamagePatchOriginal = typeof(SosigLink).GetMethod("Damage", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo sosigLinkDamagePatchPrefix = typeof(SosigLinkDamagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(sosigLinkDamagePatchOriginal, new HarmonyMethod(sosigLinkDamagePatchPrefix));

            // PlayerBodyHealPercentPatch
            MethodInfo playerBodyHealPercentPatchOriginal = typeof(FVRPlayerBody).GetMethod("HealPercent", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo playerBodyHealPercentPatchPrefix = typeof(PlayerBodyHealPercentPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(playerBodyHealPercentPatchOriginal, new HarmonyMethod(playerBodyHealPercentPatchPrefix));

            //// UpdateModeTwoAxisPatch
            //MethodInfo updateModeTwoAxisPatchOriginal = typeof(FVRMovementManager).GetMethod("UpdateModeTwoAxis", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo updateModeTwoAxisPatchPrefix = typeof(UpdateModeTwoAxisPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(updateModeTwoAxisPatchOriginal, new HarmonyMethod(updateModeTwoAxisPatchPrefix));

            //// SetActivePatch
            //MethodInfo setActivePatchOriginal = typeof(UnityEngine.GameObject).GetMethod("SetActive", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setActivePatchPrefix = typeof(SetActivePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setActivePatchOriginal, new HarmonyMethod(setActivePatchPrefix));

            //// DestroyPatch
            //MethodInfo destroyPatchOriginal = typeof(UnityEngine.Object).GetMethod("Destroy", BindingFlags.Public | BindingFlags.Static, null, CallingConventions.Any, new Type[] { typeof(UnityEngine.Object) }, null);
            //MethodInfo destroyPatchPrefix = typeof(DestroyPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(destroyPatchOriginal, new HarmonyMethod(destroyPatchPrefix));

            //// SetParentagePatch
            //MethodInfo setParentagePatchOriginal = typeof(FVRPhysicalObject).GetMethod("SetParentage", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo setParentagePatchPrefix = typeof(SetParentagePatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(setParentagePatchOriginal, new HarmonyMethod(setParentagePatchPrefix));

            //// DeadBoltPatch
            //MethodInfo deadBoltPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("TurnBolt", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltPatchPrefix = typeof(DeadBoltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltPatchOriginal, new HarmonyMethod(deadBoltPatchPrefix));

            //// DeadBoltLastHandPatch
            //MethodInfo deadBoltLastHandPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("SetStartingLastHandForward", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltLastHandPatchPrefix = typeof(DeadBoltLastHandPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltLastHandPatchOriginal, new HarmonyMethod(deadBoltLastHandPatchPrefix));

            // DequeueAndPlayDebugPatch
            MethodInfo dequeueAndPlayDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("DequeueAndPlay", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo dequeueAndPlayDebugPatchPrefix = typeof(DequeueAndPlayDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(dequeueAndPlayDebugPatchOriginal, new HarmonyMethod(dequeueAndPlayDebugPatchPrefix));

            //// EventSystemUpdateDebugPatch
            //MethodInfo eventSystemUpdateDebugPatchOriginal = typeof(EventSystem).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo eventSystemUpdateDebugPatchPrefix = typeof(EventSystemUpdateDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(eventSystemUpdateDebugPatchOriginal, new HarmonyMethod(eventSystemUpdateDebugPatchPrefix));

            //// inputModuleProcessDebugPatch
            //MethodInfo inputModuleProcessDebugPatchOriginal = typeof(StandaloneInputModule).GetMethod("Process", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo inputModuleProcessDebugPatchPrefix = typeof(inputModuleProcessDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(inputModuleProcessDebugPatchOriginal, new HarmonyMethod(inputModuleProcessDebugPatchPrefix));

            //// InteractiveGlobalUpdateDebugPatch
            //MethodInfo interactiveGlobalUpdateDebugPatchOriginal = typeof(FVRInteractiveObject).GetMethod("GlobalUpdate", BindingFlags.Public | BindingFlags.Static);
            //MethodInfo interactiveGlobalUpdateDebugPatchPrefix = typeof(InteractiveGlobalUpdateDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(interactiveGlobalUpdateDebugPatchOriginal, new HarmonyMethod(interactiveGlobalUpdateDebugPatchPrefix));

            //// PlayClipDebugPatch
            //MethodInfo playClipDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("PlayClip", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo playClipDebugDebugPatchPrefix = typeof(PlayClipDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(playClipDebugPatchOriginal, new HarmonyMethod(playClipDebugDebugPatchPrefix));

            //// InstantiateAndEnqueueDebugPatch
            //MethodInfo instantiateAndEnqueueDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("InstantiateAndEnqueue", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo instantiateAndEnqueueDebugPatchPrefix = typeof(InstantiateAndEnqueueDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(instantiateAndEnqueueDebugPatchOriginal, new HarmonyMethod(instantiateAndEnqueueDebugPatchPrefix));

            //// ChamberFireDebugPatch
            //MethodInfo chamberFireDebugPatchOriginal = typeof(FVRFireArmChamber).GetMethod("Fire", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo chamberFireDebugPatchPrefix = typeof(ChamberFireDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(chamberFireDebugPatchOriginal, new HarmonyMethod(chamberFireDebugPatchPrefix));
        }

        private void Init()
        {
            // Setup player data
            health = new float[7];

            // Instantiate lists
            magazinesByType = new Dictionary<FireArmMagazineType, Dictionary<string, int>>();
            clipsByType = new Dictionary<FireArmClipType, Dictionary<string, int>>();
            roundsByType = new Dictionary<FireArmRoundType, Dictionary<string, int>>();
            activeDescriptionsByItemID = new Dictionary<string, List<EFM_DescriptionManager>>();
            usedRoundIDs = new List<string>();
            ammoBoxByAmmoID = new Dictionary<string, int>();
            itemsByParents = new Dictionary<string, List<string>>();
            requiredForQuest = new Dictionary<string, int>();
            wishList = new List<string>();
            itemsByRarity = new Dictionary<ItemRarity, List<string>>();

            // Subscribe to events
            SceneManager.sceneLoaded += OnSceneLoaded;
            SteamVR_Events.Loading.Listen(OnSceneLoadedVR);

            // Create scene def
            sceneDef = ScriptableObject.CreateInstance<MainMenuSceneDef>();
            sceneDef.name = "MeatovSceneScreen";
            sceneDef.SceneName = "MeatovMenuScene";
            sceneDef.Name = "Escape from Meatov";
            sceneDef.Desciption = "Enter Meatov, loot, attempt escape. Upgrade your base, complete quests, trade, and go again. Good luck.";
            sceneDef.Image = sceneDefImage;
            sceneDef.Type = "Escape";

            // Initially load main menu
            LoadMainMenu();
        }

        public void OnSceneLoadedVR(bool loading)
        {
            if (!loading && isGrillhouse && grillHouseSecure)
            {
                isGrillhouse = false;
                LoadLevelBeginPatch.SecureObjects();
                grillHouseSecure = false;
                SteamVR_LoadLevel.Begin("MeatovMenuScene", false, 0.5f, 0f, 0f, 0f, 1f);
            }
        }

        public void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            if (loadedScene.name.Equals("MainMenu3"))
            {
                Mod.currentLocationIndex = -1;
                inMeatovScene = false;
                Mod.currentBaseManager = null;
                LoadMainMenu();
            }
            else if (loadedScene.name.Equals("MeatovMenuScene"))
            {
                inMeatovScene = true;
                Mod.currentBaseManager = null;
                UnsecureObjects();
                LoadMeatov();
            }
            else if (loadedScene.name.Equals("Grillhouse_2Story"))
            {
                inMeatovScene = false;
                if (grillHouseSecure)
                {
                    isGrillhouse = true;
                }
            }
            else if (loadedScene.name.Equals("MeatovHideoutScene"))
            {
                Mod.currentLocationIndex = 1;
                inMeatovScene = true;
                UnsecureObjects();

                GameObject baseRoot = GameObject.Find("Hideout");

                EFM_Base_Manager baseManager = baseRoot.AddComponent<EFM_Base_Manager>();
                baseManager.data = EFM_Manager.loadedData;
                baseManager.Init();

                Transform spawnPoint = baseRoot.transform.GetChild(baseRoot.transform.childCount - 1).GetChild(0);
                GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);

                // Also set respawn to spawn point
                GM.CurrentSceneSettings.DeathResetPoint = spawnPoint;

                // Call a GC collect
                baseManager.GCManager.gc_collect();
            }
            else if (loadedScene.name.Equals("MeatovFactoryScene") /*|| other raid scenes*/)
            {
                Mod.currentLocationIndex = 2;
                inMeatovScene = true;
                Mod.currentBaseManager = null;
                UnsecureObjects();

                GameObject raidRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];

                EFM_Raid_Manager raidManager = raidRoot.AddComponent<EFM_Raid_Manager>();
                raidManager.Init();

                GM.CurrentMovementManager.TeleportToPoint(raidManager.spawnPoint.position, true, raidManager.spawnPoint.rotation.eulerAngles);

                // Unload the map's asset bundle
                Mod.currentRaidBundleRequest.assetBundle.Unload(false);

                // Call a GC collect
                raidRoot.GetComponent<EFM_Raid_Manager>().GCManager.gc_collect();
            }
            else
            {
                Mod.currentLocationIndex = -1;
                inMeatovScene = false;
                Mod.currentBaseManager = null;
            }
        }

        private void LoadMainMenu()
        {
            // Create a MainMenuScenePointable for our level
            GameObject currentPointable = Instantiate<GameObject>(mainMenuPointable);
            currentPointable.name = mainMenuPointable.name;
            MainMenuScenePointable pointableInstance = currentPointable.AddComponent<MainMenuScenePointable>();
            pointableInstance.Def = sceneDef;
            pointableInstance.Screen = GameObject.Find("LevelLoadScreen").GetComponent<MainMenuScreen>();
            pointableInstance.MaxPointingRange = 30;
            currentPointable.transform.position = new Vector3(-12.14f, 9.5f, 4.88f);
            currentPointable.transform.rotation = Quaternion.Euler(0, 300, 0);

            GM.CurrentSceneSettings.MaxPointingDistance = 30;

            // Set LOD bias to default
            QualitySettings.lodBias = 2;
        }

        private void LoadMeatov()
        {
            // Set LOD bias
            // TODO: This is dependent on which headseat is being used
            QualitySettings.lodBias = 18;

            GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Realistic;

            // Get root
            GameObject menuRoot = GameObject.Find("Menu");

            // TP Player
            Transform spawnPoint = menuRoot.transform.GetChild(menuRoot.transform.childCount - 1).GetChild(0);
            GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);

            // Also set respawn to spawn point
            GM.CurrentSceneSettings.DeathResetPoint = spawnPoint;

            // Init menu
            EFM_Menu_Manager menuManager = menuRoot.AddComponent<EFM_Menu_Manager>();
            menuManager.Init();
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
                Transform returnNodeParent = Mod.currentBaseManager.transform.GetChild(1).GetChild(25);
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
                        EFM_CustomItemWrapper CIW = Mod.scavRaidReturnItems[i].GetComponent<EFM_CustomItemWrapper>();
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

        public void LogError(string error)
        {
            Logger.LogError(error);
        }

        public void LogWarning(string warning)
        {
            Logger.LogWarning(warning);
        }

        public void LogInfo(string info)
        {
            Logger.LogInfo(info);
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
                    Mod.instance.LogError("SkillNameToIndex received name: " + name);
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
                    Mod.instance.LogError("SkillIndexToName received index: " + index);
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
                    return "factory4_day";
                case 1:
                    return "bigmap";
                case 2:
                    return "interchange";
                case 3:
                    return "laboratory";
                case 4:
                    return "woods";
                case 5:
                    return "shoreline";
                case 6:
                    return "rezervbase";
                case 7:
                    return "lighthouse";
                case 8:
                    return "tarkovstreets";
                case 9:
                    return "suburbs";
                case 10:
                    return "terminal";
                case 11:
                    return "town";
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
    }

    public class ItemMapEntry
    {
        public int mode = 0; // 0 vanilla, 1 Modul, 2 other mod

        public string ID;
        public string[] modulIDs;
        public string otherModID;
    }

    #region GamePatches
    // Patches SteamVR_LoadLevel.Begin() So we can keep certain objects from other scenes
    class LoadLevelBeginPatch
    {
        static void Prefix(ref string levelName)
        {
            Mod.instance.LogInfo("load level prefix called with levelname: "+levelName);

            if (levelName.Equals("MeatovMenuScene"))
            {
                if (SceneManager.GetActiveScene().name.Equals("MainMenu3"))
                {
                    Mod.instance.LogInfo("need to proxy through grillhouse");
                    Mod.initDoors = false;
                    levelName = "Grillhouse_2Story";
                    Mod.grillHouseSecure = true;
                }
                else
                {
                    Mod.instance.LogInfo("proxied through grillhouse if was necessary, now loading meatov menu");
                    if (!Mod.assetLoaded)
                    {
                        Mod.instance.LogInfo("Opening meatov scene but assets not loaded yet, loading assets first...");
                        Mod.instance.LoadAssets();
                        Mod.assetLoaded = true;
                    }

                    SecureObjects();
                }
            }
            else if (levelName.Equals("MeatovHideoutScene"))
            {
                SecureObjects(Mod.justFinishedRaid);
            }
            else if (levelName.Equals("MeatovFactoryScene") /*TODO: || Other raid scenes*/)
            {
                SecureObjects(Mod.chosenCharIndex == 0); // Dont want to secure equipment if scav raid
            }
        }

        private static void SecureObject(GameObject objectToSecure)
        {
            Mod.instance.LogInfo("Securing " + objectToSecure.name);
            Mod.securedObjects.Add(objectToSecure);
            GameObject.DontDestroyOnLoad(objectToSecure);
            EFM_CustomItemWrapper CIW  = objectToSecure.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor VID  = objectToSecure.GetComponent<EFM_VanillaItemDescriptor>();
            if (CIW != null)
            {
                // Items inside a rig will not be attached to the rig, so much secure them separately
                if (CIW.itemType == Mod.ItemType.Rig || CIW.itemType == Mod.ItemType.ArmoredRig)
                {
                    foreach (GameObject innerItem in CIW.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            SecureObject(innerItem);
                        }
                    }
                }
            }
            else if(VID != null)
            {
                // Secure any laser pointer hit objects
                LaserPointer[] laserPointers = objectToSecure.GetComponentsInChildren<LaserPointer>();
                foreach(LaserPointer laserPointer in laserPointers)
                {
                    if(laserPointer.BeamHitPoint != null && laserPointer.BeamHitPoint.transform.parent == null)
                    {
                        SecureObject(laserPointer.BeamHitPoint);
                    }
                }
            }
        }

        public static void SecureObjects(bool secureEquipment = false)
        {
            if (Mod.securedObjects == null)
            {
                Mod.securedObjects = new List<GameObject>();
            }
            Mod.securedObjects.Clear();

            // Secure the cameraRig
            GameObject cameraRig = GameObject.Find("[CameraRig]Fixed");
            Mod.securedObjects.Add(cameraRig);
            GameObject.DontDestroyOnLoad(cameraRig);

            if (secureEquipment)
            {
                // Secure held objects
                if (Mod.leftHand != null && Mod.leftHand.fvrHand != null)
                {
                    // harnessed will be a root item and will be secured alongside the rig
                    // not harnessed will be dropped in the world following endInteraction and needs to be secured separately
                    Mod.securedLeftHandInteractable = Mod.leftHand.fvrHand.CurrentInteractable;
                    if (Mod.securedLeftHandInteractable != null)
                    {
                        EndInteractionPatch.ignoreEndInteraction = true;
                        Mod.securedLeftHandInteractable.EndInteraction(Mod.leftHand.fvrHand);
                        if (Mod.securedLeftHandInteractable is FVRPhysicalObject && !(Mod.securedLeftHandInteractable as FVRPhysicalObject).m_isHardnessed)
                        {
                            SecureObject(Mod.securedLeftHandInteractable.gameObject);
                        }
                    }

                    Mod.securedRightHandInteractable = Mod.rightHand.fvrHand.CurrentInteractable;
                    if (Mod.securedRightHandInteractable != null)
                    {
                        EndInteractionPatch.ignoreEndInteraction = true;
                        Mod.securedRightHandInteractable.EndInteraction(Mod.rightHand.fvrHand);
                        if (Mod.securedRightHandInteractable is FVRPhysicalObject && !(Mod.securedRightHandInteractable as FVRPhysicalObject).m_isHardnessed)
                        {
                            SecureObject(Mod.securedRightHandInteractable.gameObject);
                        }
                    }
                }

                // Secure equipment
                if (Mod.equipmentSlots != null)
                {
                    foreach (EFM_EquipmentSlot equipSlot in Mod.equipmentSlots)
                    {
                        if (equipSlot != null && equipSlot.CurObject != null)
                        {
                            SecureObject(equipSlot.CurObject.gameObject);
                        }
                    }
                }

                // Secure pocket contents
                if (Mod.itemsInPocketSlots != null)
                {
                    foreach (GameObject itemInPocket in Mod.itemsInPocketSlots)
                    {
                        if (itemInPocket != null)
                        {
                            SecureObject(itemInPocket);
                        }
                    }
                }

                // Secure right shoulder content
                if (Mod.rightShoulderObject != null)
                {
                    SecureObject(Mod.rightShoulderObject);
                }
            }

            // If leaving hideout and dont want to secure equipment
            // Or if just coming back from raid (which will secure equipment) and it was scav raid
            if((!Mod.justFinishedRaid && !secureEquipment) || (Mod.justFinishedRaid && Mod.chosenCharIndex == 1)) // Make sure all items are removed from player logically
            {
                Mod.scavRaidReturnItems = new GameObject[15];

                // Drop items in hand
                if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject))
                {
                    Mod.scavRaidReturnItems[0] = GM.CurrentMovementManager.Hands[0].CurrentInteractable.gameObject;
                    GM.CurrentMovementManager.Hands[0].CurrentInteractable.ForceBreakInteraction();
                }
                if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[1].CurrentInteractable is FVRPhysicalObject))
                {
                    Mod.scavRaidReturnItems[1] = GM.CurrentMovementManager.Hands[0].CurrentInteractable.gameObject;
                    GM.CurrentMovementManager.Hands[1].CurrentInteractable.ForceBreakInteraction();
                }

                // Unequip all equipment
                if (EFM_EquipmentSlot.wearingBackpack)
                {
                    Mod.scavRaidReturnItems[2] = EFM_EquipmentSlot.currentBackpack.gameObject;
                    EFM_CustomItemWrapper backpackCIW = EFM_EquipmentSlot.currentBackpack;
                    FVRPhysicalObject backpackPhysObj = backpackCIW.GetComponent<FVRPhysicalObject>();
                    backpackPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(backpackCIW);
                }
                if (EFM_EquipmentSlot.wearingBodyArmor)
                {
                    Mod.scavRaidReturnItems[3] = EFM_EquipmentSlot.currentArmor.gameObject;
                    EFM_CustomItemWrapper bodyArmorCIW = EFM_EquipmentSlot.currentArmor;
                    FVRPhysicalObject bodyArmorPhysObj = bodyArmorCIW.GetComponent<FVRPhysicalObject>();
                    bodyArmorPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(bodyArmorCIW);
                }
                if (EFM_EquipmentSlot.wearingEarpiece)
                {
                    Mod.scavRaidReturnItems[4] = EFM_EquipmentSlot.currentEarpiece.gameObject;
                    EFM_CustomItemWrapper earPieceCIW = EFM_EquipmentSlot.currentEarpiece;
                    FVRPhysicalObject earPiecePhysObj = earPieceCIW.GetComponent<FVRPhysicalObject>();
                    earPiecePhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(earPieceCIW);
                }
                if (EFM_EquipmentSlot.wearingHeadwear)
                {
                    Mod.scavRaidReturnItems[5] = EFM_EquipmentSlot.currentHeadwear.gameObject;
                    EFM_CustomItemWrapper headWearCIW = EFM_EquipmentSlot.currentHeadwear;
                    FVRPhysicalObject headWearPhysObj = headWearCIW.GetComponent<FVRPhysicalObject>();
                    headWearPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(headWearCIW);
                }
                if (EFM_EquipmentSlot.wearingFaceCover)
                {
                    Mod.scavRaidReturnItems[6] = EFM_EquipmentSlot.currentFaceCover.gameObject;
                    EFM_CustomItemWrapper faceCoverCIW = EFM_EquipmentSlot.currentFaceCover;
                    FVRPhysicalObject faceCoverPhysObj = faceCoverCIW.GetComponent<FVRPhysicalObject>();
                    faceCoverPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(faceCoverCIW);
                }
                if (EFM_EquipmentSlot.wearingEyewear)
                {
                    Mod.scavRaidReturnItems[7] = EFM_EquipmentSlot.currentEyewear.gameObject;
                    EFM_CustomItemWrapper eyeWearCIW = EFM_EquipmentSlot.currentEyewear;
                    FVRPhysicalObject eyeWearPhysObj = eyeWearCIW.GetComponent<FVRPhysicalObject>();
                    eyeWearPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(eyeWearCIW);
                }
                if (EFM_EquipmentSlot.wearingRig)
                {
                    Mod.scavRaidReturnItems[8] = EFM_EquipmentSlot.currentRig.gameObject;
                    EFM_CustomItemWrapper rigCIW = EFM_EquipmentSlot.currentRig;
                    FVRPhysicalObject rigPhysObj = rigCIW.GetComponent<FVRPhysicalObject>();
                    rigPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(rigCIW);
                }
                if (EFM_EquipmentSlot.wearingPouch)
                {
                    Mod.scavRaidReturnItems[9] = EFM_EquipmentSlot.currentPouch.gameObject;
                    EFM_CustomItemWrapper pouchCIW = EFM_EquipmentSlot.currentPouch;
                    FVRPhysicalObject pouchPhysObj = pouchCIW.GetComponent<FVRPhysicalObject>();
                    pouchPhysObj.SetQuickBeltSlot(null);
                    EFM_EquipmentSlot.TakeOffEquipment(pouchCIW);
                }

                // Right shoulder object
                if (Mod.rightShoulderObject != null)
                {
                    Mod.scavRaidReturnItems[10] = Mod.rightShoulderObject;
                    EFM_VanillaItemDescriptor rightShoulderVID = Mod.rightShoulderObject.GetComponent<EFM_VanillaItemDescriptor>();
                    FVRPhysicalObject rightShoulderPhysObj = rightShoulderVID.GetComponent<FVRPhysicalObject>();
                    rightShoulderPhysObj.SetQuickBeltSlot(null);
                    Mod.rightShoulderObject = null;
                }

                // Remove pockets' contents
                if (GM.CurrentPlayerBody.QBSlots_Internal != null && GM.CurrentPlayerBody.QBSlots_Internal.Count >= 4)
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        if (GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject != null)
                        {
                            Mod.scavRaidReturnItems[10 + i] = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.gameObject;
                            FVRPhysicalObject pocketItemPhysObj = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject;
                            pocketItemPhysObj.SetQuickBeltSlot(null);
                        }
                    }
                }
            }

            // Secure sceneSettings
            GameObject sceneSettings = GameObject.Find("[SceneSettings_ModBlank_Simple]");
            Mod.securedObjects.Add(sceneSettings);
            GameObject.DontDestroyOnLoad(sceneSettings);

            // Secure Pooled sources
            FVRPooledAudioSource[] pooledAudioSources = FindObjectsOfTypeIncludingDisabled<FVRPooledAudioSource>();
            foreach (FVRPooledAudioSource pooledAudioSource in pooledAudioSources)
            {
                Mod.securedObjects.Add(pooledAudioSource.gameObject);
                GameObject.DontDestroyOnLoad(pooledAudioSource.gameObject);
            }

            // Secure grabbity spheres
            FVRViveHand rightViveHand = cameraRig.transform.GetChild(0).gameObject.GetComponent<FVRViveHand>();
            FVRViveHand leftViveHand = cameraRig.transform.GetChild(1).gameObject.GetComponent<FVRViveHand>();
            Mod.securedObjects.Add(rightViveHand.Grabbity_HoverSphere.gameObject);
            Mod.securedObjects.Add(rightViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(rightViveHand.Grabbity_GrabSphere.gameObject);
            Mod.securedObjects.Add(leftViveHand.Grabbity_HoverSphere.gameObject);
            Mod.securedObjects.Add(leftViveHand.Grabbity_GrabSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_HoverSphere.gameObject);
            GameObject.DontDestroyOnLoad(leftViveHand.Grabbity_GrabSphere.gameObject);

            // Secure MovementManager objects
            Mod.securedObjects.Add(GM.CurrentMovementManager.MovementRig.gameObject);
            GameObject.DontDestroyOnLoad(GM.CurrentMovementManager.MovementRig.gameObject);
            // Movement arrows could be attached to movement manager if they are activated when we start loading
            // So only add them to the list if their parent is null
            GameObject touchPadArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_touchpadArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (touchPadArrows.transform.parent == null)
            {
                Mod.securedObjects.Add(touchPadArrows);
                GameObject.DontDestroyOnLoad(touchPadArrows);
            }
            GameObject joystickTPArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_joystickTPArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (joystickTPArrows.transform.parent == null)
            {
                Mod.securedObjects.Add(joystickTPArrows);
                GameObject.DontDestroyOnLoad(joystickTPArrows);
            }
            GameObject twinStickArrowsLeft = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsLeft", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (twinStickArrowsLeft.transform.parent == null)
            {
                Mod.securedObjects.Add(twinStickArrowsLeft);
                GameObject.DontDestroyOnLoad(twinStickArrowsLeft);
            }
            GameObject twinStickArrowsRight = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsRight", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            if (twinStickArrowsRight.transform.parent == null)
            {
                Mod.securedObjects.Add(twinStickArrowsRight);
                GameObject.DontDestroyOnLoad(twinStickArrowsRight);
            }
            GameObject floorHelper = (GameObject)(typeof(FVRMovementManager).GetField("m_floorHelper", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(floorHelper);
            GameObject.DontDestroyOnLoad(floorHelper);

            if (Mod.doorLeftPrefab == null)
            {
                // Secure doors
                Mod.doorLeftPrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Left_Cherry"));
                Mod.doorLeftPrefab.SetActive(false);
                Mod.doorLeftPrefab.name = "Door_KnobBolt_Left_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorLeftPrefab);
                Mod.doorRightPrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Right_Cherry"));
                Mod.doorRightPrefab.SetActive(false);
                Mod.doorRightPrefab.name = "Door_KnobBolt_Right_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorRightPrefab);
                Mod.doorDoublePrefab = GameObject.Instantiate(GameObject.Find("Door_KnobBolt_Double_Cherry"));
                Mod.doorDoublePrefab.SetActive(false);
                Mod.doorDoublePrefab.name = "Door_KnobBolt_Double_Cherry";
                GameObject.DontDestroyOnLoad(Mod.doorDoublePrefab);
            }
        }

        static T[] FindObjectsOfTypeIncludingDisabled<T>()
        {
            var ActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var RootObjects = ActiveScene.GetRootGameObjects();
            var MatchObjects = new List<T>();

            foreach (var ro in RootObjects)
            {
                var Matches = ro.GetComponentsInChildren<T>(true);
                MatchObjects.AddRange(Matches);
            }

            return MatchObjects.ToArray();
        }
    }

    // Patches FVRInteractiveObject.EndInteraction so we know when we drop an item so we can set its parent to the items transform so it can be saved properly later
    class EndInteractionPatch
    {
        public static bool ignoreEndInteraction; // Flag to be set to ignore endinteraction call because it is being handled elsewhere (see FireArmLoadMagPatch)

        static void Postfix(FVRViveHand hand, ref FVRInteractiveObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (ignoreEndInteraction)
            {
                ignoreEndInteraction = false;
                return;
            }

            // Just return if we declared the item as destroyed already, it would mean that we already managed the weight and item lists so we dont want this patch to do it
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            if(CIW != null)
            {
                if (CIW.destroyed)
                {
                    return;
                }
            }
            else if(VID != null)
            {
                if (VID.destroyed)
                {
                    return;
                }
            }
            else
            {
                return; // No need to process Item that has neither
            }

            // Stop here if dropping in a quick belt slot or if this is a door
            if ((__instance is FVRPhysicalObject && (__instance as FVRPhysicalObject).QuickbeltSlot != null))
            {
                // If the item is harnessed to the quickbelt slot, must scale it appropriately
                if ((__instance as FVRPhysicalObject).m_isHardnessed)
                {
                    if ((__instance as FVRPhysicalObject).QuickbeltSlot is EFM_EquipmentSlot)
                    {
                        // Make equipment the size of its QBPoseOverride because by default the game only sets rotation
                        if (__instance.QBPoseOverride != null)
                        {
                            __instance.transform.localScale = __instance.QBPoseOverride.localScale;
                        }
                    }
                }

                // Check if area slot
                if ((__instance as FVRPhysicalObject).QuickbeltSlot is EFM_AreaSlot)
                {
                    EFM_CustomItemWrapper heldCustomItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor heldVanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                    BeginInteractionPatch.SetItemLocationIndex(3, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                    // Was on player
                    Mod.RemoveFromPlayerInventory(__instance.transform, true);
                    return;
                }

                // This is only relevant in the case where the QB slot is on a loose rig in base or raid
                FVRPhysicalObject physObj =  __instance as FVRPhysicalObject;
                FVRQuickBeltSlot qbs = physObj.QuickbeltSlot;
                if (qbs.transform.parent != null && qbs.transform.parent.parent != null && qbs.transform.parent.parent.parent != null)
                {
                    EFM_CustomItemWrapper customItemWrapper = qbs.transform.parent.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                    if (customItemWrapper != null)
                    {
                        EFM_CustomItemWrapper heldCustomItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
                        EFM_VanillaItemDescriptor heldVanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                        if (heldCustomItemWrapper != null)
                        {
                            BeginInteractionPatch.SetItemLocationIndex(customItemWrapper.locationIndex, heldCustomItemWrapper, null);

                            // Update lists
                            if (customItemWrapper.locationIndex == 1)
                            {
                                // Was on player
                                Mod.RemoveFromPlayerInventory(heldCustomItemWrapper.transform, false);

                                // Now in hideout
                                Mod.currentBaseManager.AddToBaseInventory(heldCustomItemWrapper.transform, false);
                            }
                            else if (customItemWrapper.locationIndex == 2)
                            {
                                // Was on player
                                Mod.RemoveFromPlayerInventory(heldCustomItemWrapper.transform, true);
                            }
                        }
                        else if (heldVanillaItemDescriptor)
                        {
                            BeginInteractionPatch.SetItemLocationIndex(customItemWrapper.locationIndex, null, heldVanillaItemDescriptor);

                            // Update lists
                            if (customItemWrapper.locationIndex == 1)
                            {
                                // Was on player
                                Mod.RemoveFromPlayerInventory(heldVanillaItemDescriptor.transform, false);

                                // Now in hideout
                                Mod.currentBaseManager.AddToBaseInventory(heldVanillaItemDescriptor.transform, false);
                            }
                            else if (customItemWrapper.locationIndex == 2)
                            {
                                // Was on player
                                Mod.RemoveFromPlayerInventory(heldVanillaItemDescriptor.transform, true);
                            }
                        }
                        return;
                    }
                }

                // Play drop sound if custom
                if (CIW != null && hand.CanMakeGrabReleaseSound && CIW.itemSounds != null && CIW.itemSounds[0] != null)
                {
                    AudioEvent audioEvent = new AudioEvent();
                    audioEvent.Clips.Add(CIW.itemSounds[0]);
                    SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.Input.Pos);
                    hand.HandMadeGrabReleaseSound();
                }
                return;
            }
            else if(CheckIfDoorUpwards(__instance.gameObject, 3))
            {
                return;
            }

            if (__instance is FVRAlternateGrip)
            {
                FVRPhysicalObject primary = (__instance as FVRAlternateGrip).PrimaryObject;
                if (primary.transform.parent == null)
                {
                    DropItem(hand, primary);
                }
            }
            else if(__instance is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asAttachment = (FVRFireArmAttachment)__instance;
                if (asAttachment.Sensor.CurHoveredMount != null)
                {
                    EFM_CustomItemWrapper attachmentParentCIW = asAttachment.Sensor.CurHoveredMount.MyObject.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor attachmentParentVID = asAttachment.Sensor.CurHoveredMount.MyObject.GetComponent<EFM_VanillaItemDescriptor>();

                    int newLocationIndex = -1;
                    if(attachmentParentCIW != null)
                    {
                        newLocationIndex = attachmentParentCIW.locationIndex;
                    }
                    else if(attachmentParentVID != null)
                    {
                        newLocationIndex = attachmentParentVID.locationIndex;
                    }

                    BeginInteractionPatch.SetItemLocationIndex(newLocationIndex, CIW, VID, true);

                    if(newLocationIndex != 0)
                    {
                        Mod.RemoveFromPlayerInventory(__instance.transform, false);

                        if(newLocationIndex == 1)
                        {
                            Mod.currentBaseManager.AddToBaseInventory(__instance.transform, false);
                        }
                    }
                }
                else
                {
                    DropItem(hand, asAttachment);
                }
            }
            else if (__instance is FVRPhysicalObject)
            {
                FVRPhysicalObject primary = (__instance as FVRPhysicalObject);
                if (primary.AltGrip == null)
                {
                    DropItem(hand, primary);
                }
            }
        }

        private static bool CheckIfDoorUpwards(GameObject go, int steps)
        {
            if(go.GetComponent<SideHingedDestructibleDoor>() != null ||
               go.GetComponent<SideHingedDestructibleDoorHandle>() != null ||
               go.GetComponent<SideHingedDestructibleDoorDeadBolt>() != null)
            {
                return true;
            }
            else if(steps == 0 || go.transform.parent == null)
            {
                return false;
            }
            else
            {
                return CheckIfDoorUpwards(go.transform.parent.gameObject, steps - 1);
            }
        }

        private static void DropItem(FVRViveHand hand, FVRPhysicalObject primary)
        {
            Mod.instance.LogInfo("Dropped Item " + primary.name+":\n"+Environment.StackTrace);
            EFM_CustomItemWrapper collidingContainerWrapper = null;
            EFM_TradeVolume collidingTradeVolume = null;
            if (Mod.rightHand != null)
            {
                if (hand.IsThisTheRightHand)
                {
                    collidingContainerWrapper = Mod.rightHand.collidingContainerWrapper;
                    collidingTradeVolume = Mod.rightHand.collidingTradeVolume;
                    Mod.instance.LogInfo("\tfrom right hand, container null? " + (collidingContainerWrapper == null));
                }
                else // Left hand
                {
                    collidingContainerWrapper = Mod.leftHand.collidingContainerWrapper;
                    collidingTradeVolume = Mod.leftHand.collidingTradeVolume;
                    Mod.instance.LogInfo("\tfrom left hand, container null? " + (collidingContainerWrapper == null));
                }
            }

            EFM_CustomItemWrapper heldCustomItemWrapper = primary.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor heldVanillaItemDescriptor = primary.GetComponent<EFM_VanillaItemDescriptor>();

            // Remove from All if necessary
            if (heldCustomItemWrapper != null)
            {
                Mod.RemoveFromAll(primary, heldCustomItemWrapper, null);
            }
            else if(heldVanillaItemDescriptor != null && primary is FVRFireArm)
            {
                Mod.RemoveFromAll(primary, null, heldVanillaItemDescriptor);
            }

            if (collidingContainerWrapper != null && (hand.IsThisTheRightHand ? Mod.rightHand.hoverValid : Mod.leftHand.hoverValid) && (heldCustomItemWrapper == null || !heldCustomItemWrapper.Equals(collidingContainerWrapper)))
            {
                Mod.instance.LogInfo("\tChecking if item fits in container");
                // Check if item fits in container
                if (collidingContainerWrapper.AddItemToContainer(primary))
                {
                    if(collidingContainerWrapper.locationIndex == 1)
                    {
                        BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                        if(heldCustomItemWrapper != null)
                        {
                            // Was on player
                            Mod.RemoveFromPlayerInventory(heldCustomItemWrapper.transform, false);

                            // Now in hideout
                            Mod.currentBaseManager.AddToBaseInventory(heldCustomItemWrapper.transform, false);
                        }
                        else
                        {
                            // Was on player
                            Mod.RemoveFromPlayerInventory(heldVanillaItemDescriptor.transform, false);

                            // Now in hideout
                            Mod.currentBaseManager.AddToBaseInventory(heldVanillaItemDescriptor.transform, false);
                        }
                    }
                    else if(collidingContainerWrapper.locationIndex == 2)
                    {
                        BeginInteractionPatch.SetItemLocationIndex(2, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                        if (heldCustomItemWrapper != null)
                        {
                            // Was on player
                            Mod.RemoveFromPlayerInventory(heldCustomItemWrapper.transform, true);
                        }
                        else
                        {
                            // Was on player
                            Mod.RemoveFromPlayerInventory(heldVanillaItemDescriptor.transform, true);
                        }
                    }
                }
                else
                {
                    DropItemInWorld(primary, heldCustomItemWrapper, heldVanillaItemDescriptor);
                }
            }
            else if (collidingTradeVolume != null)
            {
                Mod.instance.LogInfo("\tcolliding trade volume not null, adding to trade volume");
                collidingTradeVolume.AddItem(primary);

                collidingTradeVolume.market.UpdateBasedOnItem(true, heldCustomItemWrapper, heldVanillaItemDescriptor);

                BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                // Was on player
                Mod.RemoveFromPlayerInventory(primary.transform, false);

                // Now in hideout
                Mod.currentBaseManager.AddToBaseInventory(primary.transform, false);
            }
            else
            {
                DropItemInWorld(primary, heldCustomItemWrapper, heldVanillaItemDescriptor);
            }
        }

        private static void DropItemInWorld(FVRPhysicalObject primary, EFM_CustomItemWrapper heldCustomItemWrapper, EFM_VanillaItemDescriptor heldVanillaItemDescriptor)
        {
            // Drop item in world
            GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
            EFM_Base_Manager baseManager = sceneRoot.GetComponent<EFM_Base_Manager>();
            EFM_Raid_Manager raidManager = sceneRoot.GetComponent<EFM_Raid_Manager>();
            if (baseManager != null)
            {
                BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                // Was on player
                Mod.RemoveFromPlayerInventory(primary.transform, false);

                // Now in hideout
                Mod.currentBaseManager.AddToBaseInventory(primary.transform, false);

                primary.SetParentage(sceneRoot.transform.GetChild(2));
            }
            else if (raidManager != null)
            {
                BeginInteractionPatch.SetItemLocationIndex(2, heldCustomItemWrapper, heldVanillaItemDescriptor, true);

                // Update lists
                // Was on player
                Mod.RemoveFromPlayerInventory(primary.transform, true);
                
                primary.SetParentage(sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2));
            }
        }
    }

    // Patches FVRPlayerBody.ConfigureQuickbelt so we can call it with index -1 to clear the quickbelt, or to save which is the latest quick belt index we configured
    // This completely replaces the original
    class ConfigureQuickbeltPatch
    {
        static bool Prefix(int index, ref FVRPlayerBody __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (index < 0)
            {
                // Clear the belt above the pockets
                if (__instance.QBSlots_Internal.Count > 4)
                {
                    for (int i = __instance.QBSlots_Internal.Count - 1; i >= 4; --i)
                    {
                        if (__instance.QBSlots_Internal[i] == null)
                        {
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                        else if (__instance.QBSlots_Internal[i].IsPlayer)
                        {
                            // Index -2 or -3 will destroy objects associated to the slots
                            if ((index == -2 || index == -3) && __instance.QBSlots_Internal[i].CurObject != null)
                            {
                                GameObject.Destroy(__instance.QBSlots_Internal[i].CurObject.gameObject);
                                __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QBSlots_Internal[i].gameObject);
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                    }
                }

                // If -3 also destroy objects in pockets, but dont get rid of the slots themselves
                if(index == -3)
                {
                    for (int i = 3; i >= 0; --i)
                    {
                        if (__instance.QBSlots_Internal[i].IsPlayer && __instance.QBSlots_Internal[i].CurObject != null)
                        {
                            GameObject.Destroy(__instance.QBSlots_Internal[i].CurObject.gameObject);
                            __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                        }
                    }
                }

                Mod.currentQuickBeltConfiguration = Mod.pocketsConfigIndex;
            }
            else if(index > Mod.pocketsConfigIndex) // If index is higher than the pockets configuration index, we must keep the pocket slots intact
            {
                // Only check for slots other than pockets
                if (__instance.QBSlots_Internal.Count > /* 0 */ 4)
                {
                    for (int i = __instance.QBSlots_Internal.Count - 1; i >= /* 0 */ 4; i--)
                    {
                        if (__instance.QBSlots_Internal[i] == null)
                        {
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                        else if (__instance.QBSlots_Internal[i].IsPlayer)
                        {
                            if (__instance.QBSlots_Internal[i].CurObject != null)
                            {
                                __instance.QBSlots_Internal[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QBSlots_Internal[i].gameObject);
                            __instance.QBSlots_Internal.RemoveAt(i);
                        }
                    }
                }
                int num = Mathf.Clamp(index, 0, ManagerSingleton<GM>.Instance.QuickbeltConfigurations.Length - 1);
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(ManagerSingleton<GM>.Instance.QuickbeltConfigurations[num], __instance.Torso.position, __instance.Torso.rotation);
                gameObject.transform.SetParent(__instance.Torso.transform);
                gameObject.transform.localPosition = Vector3.zero;
                IEnumerator enumerator = gameObject.transform.GetEnumerator();
                try
                {
                    while (enumerator.MoveNext())
                    {
                        object obj = enumerator.Current;
                        Transform transform = (Transform)obj;
                        if (transform.gameObject.tag == "QuickbeltSlot")
                        {
                            FVRQuickBeltSlot component = transform.GetComponent<FVRQuickBeltSlot>();
                            if (GM.Options.QuickbeltOptions.QuickbeltHandedness > 0)
                            {
                                Vector3 vector = component.PoseOverride.forward;
                                Vector3 vector2 = component.PoseOverride.up;
                                vector = Vector3.Reflect(vector, component.transform.right);
                                vector2 = Vector3.Reflect(vector2, component.transform.right);
                                component.PoseOverride.rotation = Quaternion.LookRotation(vector, vector2);
                            }
                            __instance.QBSlots_Internal.Add(component);
                        }
                    }
                }
                finally
                {
                    IDisposable disposable;
                    if ((disposable = (enumerator as IDisposable)) != null)
                    {
                        disposable.Dispose();
                    }
                }
                for (int j = 0; j < __instance.QBSlots_Internal.Count; j++)
                {
                    if (__instance.QBSlots_Internal[j].IsPlayer)
                    {
                        __instance.QBSlots_Internal[j].transform.SetParent(__instance.Torso);
                        __instance.QBSlots_Internal[j].QuickbeltRoot = null;
                        if (GM.Options.QuickbeltOptions.QuickbeltHandedness > 0)
                        {
                            __instance.QBSlots_Internal[j].transform.localPosition = new Vector3(-__instance.QBSlots_Internal[j].transform.localPosition.x, __instance.QBSlots_Internal[j].transform.localPosition.y, __instance.QBSlots_Internal[j].transform.localPosition.z);
                        }
                    }
                }
                PlayerBackPack[] array = UnityEngine.Object.FindObjectsOfType<PlayerBackPack>();
                for (int k = 0; k < array.Length; k++)
                {
                    array[k].RegisterQuickbeltSlots();
                }
                UnityEngine.Object.Destroy(gameObject);

                // Set custom quick belt config index
                Mod.currentQuickBeltConfiguration = index;
            }
            else // Equal to pockets or another vanilla config
            {
                return true;
            }

            return false;
        }

        static void Postfix(int index, ref FVRPlayerBody __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // Refresh the pocket quickbelt slots if necessary, they should always be the first 4 in the list of quickbelt slots
            if (index >= Mod.pocketsConfigIndex)
            {
                for(int i=0; i < 4; ++i)
                {
                    Mod.pocketSlots[i] = __instance.QBSlots_Internal[i];
                }
            }
        }
    }

    // Patches FVRViveHand.TestQuickBeltDistances so we also check custom slots and check for equipment incompatibility
    // This completely replaces the original
    class TestQuickbeltPatch
    {
        static bool Prefix(ref FVRViveHand __instance, ref FVRViveHand.HandState ___m_state, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }


            if (__instance.CurrentHoveredQuickbeltSlot != null && !__instance.CurrentHoveredQuickbeltSlot.IsSelectable)
            {
                __instance.CurrentHoveredQuickbeltSlot = null;
            }
            if (__instance.CurrentHoveredQuickbeltSlotDirty != null && !__instance.CurrentHoveredQuickbeltSlotDirty.IsSelectable)
            {
                __instance.CurrentHoveredQuickbeltSlotDirty = null;
            }
            if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.CurObject != null && !__instance.CurrentHoveredQuickbeltSlot.CurObject.IsInteractable())
            {
                __instance.CurrentHoveredQuickbeltSlot = null;
            }
            FVRQuickBeltSlot fvrquickBeltSlot = null;
            Vector3 position = __instance.PoseOverride.position;
            if (__instance.CurrentInteractable != null)
            {
                if (__instance.CurrentInteractable.PoseOverride != null)
                {
                    position = __instance.CurrentInteractable.PoseOverride.position;
                }
                else
                {
                    position = __instance.CurrentInteractable.transform.position;
                }
            }
            for (int i = 0; i < GM.CurrentPlayerBody.QBSlots_Internal.Count; i++)
            {
                if (GM.CurrentPlayerBody.QBSlots_Internal[i].IsPointInsideMe(position))
                {
                    fvrquickBeltSlot = GM.CurrentPlayerBody.QBSlots_Internal[i];
                    break;
                }
            }

            // Check equip slots if status UI is active
            int equipmentSlotIndex = -1;
            if (Mod.playerStatusManager != null && Mod.playerStatusManager.displayed && fvrquickBeltSlot == null && Mod.equipmentSlots != null)
            {
                for (int i = 0; i < Mod.equipmentSlots.Count; ++i) 
                {
                    if (Mod.equipmentSlots[i].IsPointInsideMe(position))
                    {
                        fvrquickBeltSlot = Mod.equipmentSlots[i];
                        equipmentSlotIndex = i;
                        break;
                    }
                }
            }

            // Check other active slots if it is not equip slot
            if (equipmentSlotIndex == -1 && Mod.otherActiveSlots != null)
            {
                for (int setIndex = 0; setIndex < Mod.otherActiveSlots.Count; ++setIndex)
                {
                    for (int slotIndex = 0; slotIndex < Mod.otherActiveSlots[setIndex].Count; ++slotIndex)
                    {
                        if (Mod.otherActiveSlots[setIndex][slotIndex].IsPointInsideMe(position))
                        {
                            fvrquickBeltSlot = Mod.otherActiveSlots[setIndex][slotIndex];
                            break;
                        }
                    }
                }
            }

            // Check shoulder slots
            int shoulderIndex = -1;
            if(fvrquickBeltSlot == null)
            {
                if (Mod.leftShoulderSlot != null && Mod.leftShoulderSlot.IsPointInsideMe(position))
                {
                    fvrquickBeltSlot = Mod.leftShoulderSlot;
                    shoulderIndex = 0;
                }
                else if (Mod.rightShoulderSlot != null && Mod.rightShoulderSlot.IsPointInsideMe(position))
                {
                    fvrquickBeltSlot = Mod.rightShoulderSlot;
                    shoulderIndex = 1;
                }
            }

            // Check area slots
            if(fvrquickBeltSlot == null && Mod.areaSlots != null)
            {
                foreach(FVRQuickBeltSlot slot in Mod.areaSlots)
                {
                    if(slot != null && slot.transform.parent.gameObject.activeSelf && slot.IsPointInsideMe(position))
                    {
                        fvrquickBeltSlot = slot;
                        break;
                    }
                }
            }

            if (fvrquickBeltSlot == null)
            {
                if (__instance.CurrentHoveredQuickbeltSlot != null)
                {
                    __instance.CurrentHoveredQuickbeltSlot = null;
                }
                __instance.CurrentHoveredQuickbeltSlotDirty = null;
            }
            else
            {
                __instance.CurrentHoveredQuickbeltSlotDirty = fvrquickBeltSlot;
                if (___m_state == FVRViveHand.HandState.Empty)
                {
                    if (fvrquickBeltSlot.CurObject != null && !fvrquickBeltSlot.CurObject.IsHeld && fvrquickBeltSlot.CurObject.IsInteractable())
                    {
                        __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                    }
                    else if (shoulderIndex == 0 && Mod.equipmentSlots[0].CurObject != null && !Mod.equipmentSlots[0].CurObject.IsHeld && Mod.equipmentSlots[0].CurObject.IsInteractable())
                    {
                        // Set hovered QB slot to backpack equip slot if it is left shoulder and backpack slot is not empty
                        __instance.CurrentHoveredQuickbeltSlot = Mod.equipmentSlots[0];
                    }
                }
                else if (___m_state == FVRViveHand.HandState.GripInteracting && ___m_currentInteractable != null && ___m_currentInteractable is FVRPhysicalObject)
                {
                    FVRPhysicalObject fvrphysicalObject = (FVRPhysicalObject)___m_currentInteractable;
                    if (fvrquickBeltSlot.CurObject == null && fvrquickBeltSlot.SizeLimit >= fvrphysicalObject.Size && fvrphysicalObject.QBSlotType == fvrquickBeltSlot.Type)
                    {
                        // Check for equipment compatibility if slot is an equipment slot
                        EFM_CustomItemWrapper customItemWrapper = fvrphysicalObject.GetComponent<EFM_CustomItemWrapper>();
                        if (equipmentSlotIndex > -1)
                        {
                            if(customItemWrapper != null)
                            {
                                bool typeCompatible = Mod.equipmentSlots[equipmentSlotIndex].equipmentType == customItemWrapper.itemType;
                                bool otherCompatible = true;
                                switch (customItemWrapper.itemType)
                                {
                                    case Mod.ItemType.ArmoredRig:
                                        typeCompatible = Mod.equipmentSlots[equipmentSlotIndex].equipmentType == Mod.ItemType.BodyArmor;
                                        otherCompatible = !EFM_EquipmentSlot.wearingBodyArmor && !EFM_EquipmentSlot.wearingRig;
                                        break;
                                    case Mod.ItemType.BodyArmor:
                                        otherCompatible = !EFM_EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case Mod.ItemType.Helmet:
                                        typeCompatible = Mod.equipmentSlots[equipmentSlotIndex].equipmentType == Mod.ItemType.Headwear;
                                        otherCompatible = (!EFM_EquipmentSlot.wearingEarpiece || !EFM_EquipmentSlot.currentEarpiece.blocksHeadwear) &&
                                                          (!EFM_EquipmentSlot.wearingFaceCover || !EFM_EquipmentSlot.currentFaceCover.blocksHeadwear) &&
                                                          (!EFM_EquipmentSlot.wearingEyewear || !EFM_EquipmentSlot.currentEyewear.blocksHeadwear);
                                        break;
                                    case Mod.ItemType.Earpiece:
                                        otherCompatible = (!EFM_EquipmentSlot.wearingHeadwear || !EFM_EquipmentSlot.currentHeadwear.blocksEarpiece) &&
                                                          (!EFM_EquipmentSlot.wearingFaceCover || !EFM_EquipmentSlot.currentFaceCover.blocksEarpiece) &&
                                                          (!EFM_EquipmentSlot.wearingEyewear || !EFM_EquipmentSlot.currentEyewear.blocksEarpiece);
                                        break;
                                    case Mod.ItemType.FaceCover:
                                        otherCompatible = (!EFM_EquipmentSlot.wearingHeadwear || !EFM_EquipmentSlot.currentHeadwear.blocksFaceCover) &&
                                                          (!EFM_EquipmentSlot.wearingEarpiece || !EFM_EquipmentSlot.currentEarpiece.blocksFaceCover) &&
                                                          (!EFM_EquipmentSlot.wearingEyewear || !EFM_EquipmentSlot.currentEyewear.blocksFaceCover);
                                        break;
                                    case Mod.ItemType.Eyewear:
                                        otherCompatible = (!EFM_EquipmentSlot.wearingHeadwear || !EFM_EquipmentSlot.currentHeadwear.blocksEyewear) &&
                                                          (!EFM_EquipmentSlot.wearingEarpiece || !EFM_EquipmentSlot.currentEarpiece.blocksEyewear) &&
                                                          (!EFM_EquipmentSlot.wearingFaceCover || !EFM_EquipmentSlot.currentFaceCover.blocksEyewear);
                                        break;
                                    case Mod.ItemType.Rig:
                                        otherCompatible = !EFM_EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case Mod.ItemType.Headwear:
                                        otherCompatible = (!EFM_EquipmentSlot.wearingEarpiece || !EFM_EquipmentSlot.currentEarpiece.blocksHeadwear) &&
                                                          (!EFM_EquipmentSlot.wearingFaceCover || !EFM_EquipmentSlot.currentFaceCover.blocksHeadwear) &&
                                                          (!EFM_EquipmentSlot.wearingEyewear || !EFM_EquipmentSlot.currentEyewear.blocksHeadwear);
                                        break;
                                    default:
                                        break;
                                }
                                if (typeCompatible && otherCompatible)
                                {
                                    __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                                }
                            }
                        }
                        else if(shoulderIndex > -1)
                        {
                            // If left shoulder, make sure item is backpack, and player not already wearing a backpack
                            // If right shoulder, make sure item is a firearm
                            if (shoulderIndex == 0 && customItemWrapper != null && customItemWrapper.itemType == Mod.ItemType.Backpack && EFM_EquipmentSlot.currentBackpack == null)
                            {
                                __instance.CurrentHoveredQuickbeltSlot = Mod.equipmentSlots[0];
                            }
                            else if(shoulderIndex == 1 && fvrphysicalObject is FVRFireArm)
                            {
                                __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                            }
                        }
                        else if(fvrquickBeltSlot is EFM_AreaSlot)
                        {
                            EFM_AreaSlot asAreaSlot = fvrquickBeltSlot as EFM_AreaSlot;
                            string IDToUse;
                            if(customItemWrapper != null)
                            {
                                IDToUse = customItemWrapper.ID;
                            }
                            else
                            {
                                IDToUse = fvrphysicalObject.GetComponent<EFM_VanillaItemDescriptor>().H3ID;
                            }
                            if (asAreaSlot.filter.Contains(IDToUse))
                            {
                                __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                            }
                        }
                        else
                        {
                            __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                        }
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRPhysicalObject.SetQuickBeltSlot so we can keep track of what goes in and out of rigs
    class SetQuickBeltSlotPatch
    {
        private static bool skipPatch;
        public static bool dontProcessRigWeight;

        static void Prefix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }
            Mod.instance.LogInfo("SetQuickBeltSlotPatch called");

            // This may be the case, and in this case we don't want to redo everything we do in the patch, so just skip
            if(slot == __instance.QuickbeltSlot)
            {
                Mod.instance.LogInfo("Item " + __instance.name + " is already in slot: " + (slot == null ? "null":slot.name)+ ", skipipng SetQuickBeltSlotPatch patch");
                skipPatch = true;

                // Even if skipping patch, need to make sure taht in the case of the backpack shoulder slot, we still set it as equip slot
                // This is in case the backpack is harnessed to the slot, it will already have this slot assigned but we can't skip this
                if(slot != null)
                {
                    // Need to make sure that a backpack being put into left shoulder slot gets put into backpack equipment slot instead
                    if (slot is EFM_ShoulderStorage)
                    {
                        if (!(slot as EFM_ShoulderStorage).right)
                        {
                            slot = Mod.equipmentSlots[0]; // Set the slot as the backpack equip slot
                        }
                    }
                }

                return;
            }

            if (slot == null)
            {
                if(__instance.QuickbeltSlot == null)
                {
                    return;
                }

                // Set the size of the object to normal because it may have been scaled to fit the slot
                __instance.transform.localScale = Vector3.one;

                // Prefix will be called before the object's current slot is set to null, so we can check if it was taken from an equipment slot or a rig slot
                EFM_CustomItemWrapper customItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
                if (__instance.QuickbeltSlot is EFM_EquipmentSlot)
                {
                    // Have to remove equipment
                    if (customItemWrapper != null)
                    {
                        EFM_EquipmentSlot.TakeOffEquipment(customItemWrapper);
                    }

                    // Also set left shoulder object to null if this is backpack slot
                    if (__instance.QuickbeltSlot.Equals(Mod.equipmentSlots[0]))
                    {
                        Mod.leftShoulderObject = null;
                    }
                }
                else if(__instance.QuickbeltSlot is EFM_ShoulderStorage)
                {
                    EFM_ShoulderStorage asShoulderSlot = __instance.QuickbeltSlot as EFM_ShoulderStorage;
                    if (asShoulderSlot.right)
                    {
                        Mod.rightShoulderObject = null;
                        __instance.gameObject.SetActive(true);
                    }
                    //else
                    //{
                    //    Mod.leftShoulderObject = null;
                    //    EFM_EquipmentSlot.TakeOffEquipment(customItemWrapper);
                    //}
                }
                else if(__instance.QuickbeltSlot is EFM_AreaSlot)
                {
                    EFM_AreaSlot asAreaSlot = __instance.QuickbeltSlot as EFM_AreaSlot;
                    Mod.currentBaseManager.baseAreaManagers[asAreaSlot.areaIndex].slotItems[asAreaSlot.slotIndex] = null;
                    
                    if (Mod.areaSlotShouldUpdate)
                    {
                        EFM_BaseAreaManager areaManager = __instance.QuickbeltSlot.transform.parent.parent.parent.GetComponent<EFM_BaseAreaManager>();

                        areaManager.slotItems[asAreaSlot.slotIndex] = null;

                        areaManager.UpdateBasedOnSlots();
                    }
                    else
                    {
                        Mod.areaSlotShouldUpdate = true;
                    }
                }
                else 
                {
                    // Check if in pockets
                    for (int i = 0; i < 4; ++i)
                    {
                        if (Mod.itemsInPocketSlots[i] != null && Mod.itemsInPocketSlots[i].Equals(__instance.gameObject))
                        {
                            Mod.itemsInPocketSlots[i] = null;
                            return;
                        }
                    }

                    // Check if slot in a loose rig
                    Transform slotRootParent = __instance.QuickbeltSlot.transform.parent.parent;
                    if (slotRootParent != null)
                    {
                        Transform rootOwner = slotRootParent.parent;
                        if (rootOwner != null)
                        {
                            EFM_CustomItemWrapper rigItemWrapper = rootOwner.GetComponent<EFM_CustomItemWrapper>();
                            if (rigItemWrapper != null && (rigItemWrapper.itemType == Mod.ItemType.Rig || rigItemWrapper.itemType == Mod.ItemType.ArmoredRig))
                            {
                                // This slot is owned by a rig, need to update that rig's content
                                for (int slotIndex = 0; slotIndex < rigItemWrapper.rigSlots.Count; ++slotIndex)
                                {
                                    if (rigItemWrapper.rigSlots[slotIndex].Equals(__instance.QuickbeltSlot))
                                    {
                                        rigItemWrapper.itemsInSlots[slotIndex] = null;
                                        rigItemWrapper.currentWeight -= customItemWrapper != null ? customItemWrapper.currentWeight : __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight;
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    if (EFM_EquipmentSlot.currentRig != null) // Is slot of current rig
                    {
                        // If we are setting this object's slot to null, it may be because we have begun interacting with
                        // the rig equipment and therefore should not remove it from the rig's itemsInSlots
                        if (!Mod.beginInteractingEquipRig)
                        {
                            // Find item in rig's itemsInSlots and remove it
                            for (int i = 0; i < EFM_EquipmentSlot.currentRig.itemsInSlots.Length; ++i)
                            {
                                if (EFM_EquipmentSlot.currentRig.itemsInSlots[i] == __instance.gameObject)
                                {
                                    EFM_EquipmentSlot.currentRig.itemsInSlots[i] = null;
                                    EFM_EquipmentSlot.currentRig.currentWeight -= customItemWrapper != null ? customItemWrapper.currentWeight : __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight;
                                    EFM_EquipmentSlot.currentRig.UpdateRigMode();
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Need to make sure that a backpack being put into left shoulder slot gets put into backpack equipment slot instead
                if(slot is EFM_ShoulderStorage)
                {
                    if(!(slot as EFM_ShoulderStorage).right)
                    {
                        slot = Mod.equipmentSlots[0]; // Set the slot as the backpack equip slot
                    }
                }

                // Add to All if necessary
                EFM_CustomItemWrapper customItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
                EFM_VanillaItemDescriptor vanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                if(customItemWrapper != null)
                {
                    Mod.AddToAll(__instance, customItemWrapper, null);
                }
                else if(vanillaItemDescriptor != null && (__instance is FVRFireArm || (__instance is FVRMeleeWeapon && !vanillaItemDescriptor.inAll)))
                {
                    Mod.AddToAll(__instance, null, vanillaItemDescriptor);
                }
            }
        }

        static void Postfix(FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (skipPatch)
            {
                skipPatch = false;

                // Even if skipping, we still want to make sure that if the slot is not null, we set the item's parent to null
                // Also make sure that if it is an equip slot, set active depending on player status display if the item is not being held
                if (slot != null)
                {
                    __instance.SetParentage(null);

                    // This is for in case we harnessed backpack to the shoulder slot
                    if (slot is EFM_EquipmentSlot && __instance.m_hand == null)
                    {
                        __instance.gameObject.SetActive(Mod.playerStatusManager.displayed);
                    }
                }

                return;
            }

            if (slot == null)
            {
                return;
            }

            // Ensure the item's parent is null
            // TODO: EndInteractionInInventorySlot is supposed to attach the item to quick belt root but this was only happening to equipment in equip slots and not on rig slots
            // So should we attach them to the slots or no? because it causes extreme lag and is clearly unnecessary
            __instance.SetParentage(null);

            // Check if pocket slot
            for (int i=0; i < 4; ++i)
            {
                if (Mod.pocketSlots[i].Equals(slot))
                {
                    Mod.itemsInPocketSlots[i] = __instance.gameObject;
                    return;
                }
            }

            // Check if shoulder slot
            if(slot is EFM_ShoulderStorage)
            {
                EFM_ShoulderStorage asShoulderSlot = slot as EFM_ShoulderStorage;
                if (asShoulderSlot.right)
                {
                    Mod.rightShoulderObject = __instance.gameObject;
                    __instance.gameObject.SetActive(__instance.IsAltHeld || __instance.IsHeld); // Dont want to set inactive if held, and could be held if harnessed
                }
                //else
                //{
                //    Mod.leftShoulderObject = __instance.gameObject;
                //    EFM_EquipmentSlot.WearEquipment(__instance.GetComponent<EFM_CustomItemWrapper>());
                //}

                return;
            }
            
            if (slot is EFM_AreaSlot)
            {
                EFM_AreaSlot asAreaSlot = __instance.QuickbeltSlot as EFM_AreaSlot;
                Mod.currentBaseManager.baseAreaManagers[asAreaSlot.areaIndex].slotItems[asAreaSlot.slotIndex] = __instance.gameObject;

                if (Mod.areaSlotShouldUpdate)
                {
                    EFM_BaseAreaManager areaManager = __instance.QuickbeltSlot.transform.parent.parent.parent.GetComponent<EFM_BaseAreaManager>();

                    areaManager.slotItems[asAreaSlot.slotIndex] = __instance.gameObject;

                    areaManager.UpdateBasedOnSlots();

                    areaManager.PlaySlotInputSound();
                }
                else
                {
                    Mod.areaSlotShouldUpdate = true;
                }
            }

            if (slot is EFM_EquipmentSlot)
            {
                // Make equipment the size of its QBPoseOverride because by default the game only sets rotation
                if (__instance.QBPoseOverride != null)
                {
                    __instance.transform.localScale = __instance.QBPoseOverride.localScale;

                    // Also set the slot's poseoverride to the QBPoseOverride of the item so it get positionned properly
                    // Multiply poseoverride position by 10 because our pose override is set in cm not relativ to scale of QBTransform but H3 sets position relative to it
                    slot.PoseOverride.localPosition = __instance.QBPoseOverride.localPosition * 10;
                }

                // If this is backpack slot, also set left shoulder to the object
                if (slot.Equals(Mod.equipmentSlots[0]))
                {
                    Mod.leftShoulderObject = __instance.gameObject;
                }

                EFM_EquipmentSlot.WearEquipment(__instance.GetComponent<EFM_CustomItemWrapper>());

                __instance.gameObject.SetActive(Mod.playerStatusManager.displayed);
            }
            else if (EFM_EquipmentSlot.wearingArmoredRig || EFM_EquipmentSlot.wearingRig) // We are wearing custom quick belt, check if slot is in there, update if it is
            {
                // Find slot index in config
                bool foundSlot = false;
                for (int slotIndex=4; slotIndex < GM.CurrentPlayerBody.QBSlots_Internal.Count; ++slotIndex)
                {
                    if (GM.CurrentPlayerBody.QBSlots_Internal[slotIndex].Equals(slot))
                    {
                        EFM_CustomItemWrapper equipmentItemWrapper = EFM_EquipmentSlot.currentRig;
                        equipmentItemWrapper.itemsInSlots[slotIndex - 4] = __instance.gameObject;
                        equipmentItemWrapper.currentWeight += __instance.GetComponent<EFM_CustomItemWrapper>() != null ? __instance.GetComponent<EFM_CustomItemWrapper>().currentWeight : __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight;
                        equipmentItemWrapper.UpdateRigMode();

                        foundSlot = true;
                        break;
                    }
                }

                if (!foundSlot)
                {
                    PlaceInOtherRig(slot, __instance);
                }
            }
            else // Check if the slot is owned by a rig, if it is need to update 
            {
                PlaceInOtherRig(slot, __instance);
            }
        }

        private static void PlaceInOtherRig(FVRQuickBeltSlot slot, FVRPhysicalObject __instance)
        {
            Transform slotRootParent = slot.transform.parent.parent;
            if (slotRootParent != null)
            {
                Transform rootOwner = slotRootParent.parent;
                if (rootOwner != null)
                {
                    EFM_CustomItemWrapper customItemWrapper = rootOwner.GetComponent<EFM_CustomItemWrapper>();
                    if (customItemWrapper != null && (customItemWrapper.itemType == Mod.ItemType.Rig || customItemWrapper.itemType == Mod.ItemType.ArmoredRig))
                    {
                        // This slot is owned by a rig, need to update that rig's content
                        // Upadte rig content
                        for (int slotIndex = 0; slotIndex < customItemWrapper.rigSlots.Count; ++slotIndex)
                        {
                            if (customItemWrapper.rigSlots[slotIndex].Equals(slot))
                            {
                                customItemWrapper.itemsInSlots[slotIndex] = __instance.gameObject;

                                Mod.instance.LogInfo("Added item to rig: " + customItemWrapper.name + ", dont precess weight: "+dontProcessRigWeight);
                                // Update rig weight
                                if (dontProcessRigWeight)
                                {
                                    dontProcessRigWeight = false;
                                }
                                else
                                {
                                    customItemWrapper.currentWeight += __instance.GetComponent<EFM_CustomItemWrapper>() != null ? __instance.GetComponent<EFM_CustomItemWrapper>().currentWeight : __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight;
                                }
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    // Patches FVRPhysicalObject.BeginInteraction() in order to know if we have begun interacting with a rig from an equipment slot
    // Also to give player the loot experience in case they haven't picked it up before
    class BeginInteractionPatch
    {
        static void Prefix(FVRViveHand hand, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // Ensure the object is active
            // This is more specifically for the case of using the left shoulder slot to access the backpack slot
            // while the equipment slots arent displayed, so the backpack is inactive, we must activate it
            Mod.instance.LogInfo("Began interacting with " + __instance.name +", qbs null?: "+(__instance.QuickbeltSlot == null)+", harnessed?: "+ __instance.m_isHardnessed);
            __instance.gameObject.SetActive(true);

            // If the item is harnessed to a quickbelt slot, must scale it to 1 while interacting with it in case it was scaled down in the slot
            if(__instance.QuickbeltSlot != null && __instance.m_isHardnessed)
            {
                __instance.transform.localScale = Vector3.one;

                // TODO: Will need to review if this is necessary because an item grabbed with both and and then let go with one will shift the item to the hand that is still holding it
                //if (__instance.QuickbeltSlot == Mod.rightShoulderSlot) 
                //{
                //    // Set the item's position to approx hand to ensure it doesn't have to collide with anything to get there
                //    __instance.transform.position = hand.transform.position;
                //}
            }

            EFM_VanillaItemDescriptor vanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper customItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                // Add to All if necessary
                Mod.AddToAll(__instance, customItemWrapper, null);

                if (customItemWrapper.hideoutSpawned)
                {
                    customItemWrapper.hideoutSpawned = false;
                    foreach (Collider col in customItemWrapper.colliders)
                    {
                        col.enabled = true;
                    }
                    // TODO: Check if backpack or rig, we dont want to change the cols with colliding smoke layer or wtv?
                }

                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig) 
                {
                    // Update whether we are picking the rig up from an equip slot
                    Mod.beginInteractingEquipRig = __instance.QuickbeltSlot != null && __instance.QuickbeltSlot is EFM_EquipmentSlot;

                    // Check which PoseOverride to use depending on hand side
                    __instance.PoseOverride = hand.IsThisTheRightHand ? customItemWrapper.rightHandPoseOverride : customItemWrapper.leftHandPoseOverride;
                }
                else if(customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    // Check which PoseOverride to use depending on hand side
                    __instance.PoseOverride = hand.IsThisTheRightHand ? customItemWrapper.rightHandPoseOverride : customItemWrapper.leftHandPoseOverride;

                    // If taken out of shoulderStorage, align with hand. Only do this if the item is not currently being held
                    // because if the backpack is harnessed, held, and then switched between hands, it will align with the new hand
                    // This is prefix so m_hand should still be null if newly grabbed
                    if (Mod.leftShoulderObject != null && Mod.leftShoulderObject.Equals(customItemWrapper.gameObject) && __instance.m_hand == null)
                    {
                        FVRViveHand.AlignChild(__instance.transform, __instance.PoseOverride, hand.transform);
                    }
                }

                // Check if this item is a container, if so need to check if we were colliding with its container volume and make sure we are not
                if (customItemWrapper.itemType == Mod.ItemType.Pouch ||
                    customItemWrapper.itemType == Mod.ItemType.Backpack ||
                    customItemWrapper.itemType == Mod.ItemType.Container)
                {
                    if (hand.IsThisTheRightHand && Mod.rightHand.collidingContainerWrapper != null && Mod.rightHand.collidingContainerWrapper.Equals(customItemWrapper))
                    {
                        Mod.rightHand.hoverValid = false;
                        Mod.rightHand.collidingContainerWrapper.SetContainerHovered(false);
                        Mod.rightHand.collidingContainerWrapper = null;
                    }
                    else if(!hand.IsThisTheRightHand && Mod.leftHand.collidingContainerWrapper != null && Mod.leftHand.collidingContainerWrapper.Equals(customItemWrapper))
                    {
                        Mod.leftHand.hoverValid = false;
                        Mod.leftHand.collidingContainerWrapper.SetContainerHovered(false);
                        Mod.leftHand.collidingContainerWrapper = null;
                    }
                }

                // Check if this item is a togglable, if so need to check if we were colliding with it and make sure we are not
                if (customItemWrapper.itemType == Mod.ItemType.Pouch ||
                    customItemWrapper.itemType == Mod.ItemType.Backpack ||
                    customItemWrapper.itemType == Mod.ItemType.Container ||
                    customItemWrapper.itemType == Mod.ItemType.ArmoredRig ||
                    customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    if (hand.IsThisTheRightHand && Mod.rightHand.collidingTogglableWrapper != null && Mod.rightHand.collidingTogglableWrapper == customItemWrapper)
                    {
                        Mod.rightHand.collidingTogglableWrapper = null;
                    }
                    else if(!hand.IsThisTheRightHand && Mod.leftHand.collidingTogglableWrapper != null && Mod.leftHand.collidingTogglableWrapper == customItemWrapper)
                    {
                        Mod.leftHand.collidingTogglableWrapper = null;
                    }
                }

                if (!customItemWrapper.looted)
                {
                    customItemWrapper.looted = true;
                    if (Mod.currentLocationIndex == 2)
                    {
                        if (customItemWrapper.lootExperience > 0)
                        {
                            Mod.AddExperience(customItemWrapper.lootExperience, 1);
                        }

                        if (customItemWrapper.foundInRaid && Mod.taskFindItemConditionsByItemID.ContainsKey(customItemWrapper.ID))
                        {
                            foreach(TraderTaskCondition condition in Mod.taskFindItemConditionsByItemID[customItemWrapper.ID])
                            {
                                if(condition.failCondition && condition.task.taskState != TraderTask.TaskState.Active)
                                {
                                    continue;
                                }

                                if (!condition.fulfilled)
                                {
                                    ++condition.itemCount;
                                    EFM_TraderStatus.UpdateConditionFulfillment(condition);
                                }
                            }
                        }
                    }
                    Mod.AddSkillExp(EFM_Skill.uniqueLoot, 7);
                }

                // Update lists
                if(customItemWrapper.locationIndex == 1)
                {
                    // Was in hideout
                    Mod.currentBaseManager.RemoveFromBaseInventory(customItemWrapper.transform, false);

                    // Now on player
                    Mod.AddToPlayerInventory(customItemWrapper.transform, false);

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null, true);
                }
                else if(customItemWrapper.locationIndex == 2)
                {
                    // Now on player
                    Mod.AddToPlayerInventory(customItemWrapper.transform, true);

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null, true);
                }
                else if(customItemWrapper.locationIndex == 3)
                {
                    // Now on player
                    Mod.AddToPlayerInventory(customItemWrapper.transform, true);

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null, true);

                    //foreach (EFM_BaseAreaManager baseAreaManager in Mod.currentBaseManager.baseAreaManagers)
                    //{
                    //    baseAreaManager.UpdateBasedOnItem(customItemWrapper.ID);
                    //}
                }
            }
            else if (vanillaItemDescriptor != null)
            {
                if (vanillaItemDescriptor.hideoutSpawned)
                {
                    vanillaItemDescriptor.hideoutSpawned = false;
                    vanillaItemDescriptor.physObj.SetAllCollidersToLayer(false, "Default");
                }

                if (!vanillaItemDescriptor.looted)
                {
                    vanillaItemDescriptor.looted = true;
                    if (Mod.currentLocationIndex == 2)
                    {
                        if (vanillaItemDescriptor.lootExperience > 0)
                        {
                            Mod.AddExperience(vanillaItemDescriptor.lootExperience, 1);
                        }

                        if (vanillaItemDescriptor.foundInRaid && Mod.taskFindItemConditionsByItemID.ContainsKey(vanillaItemDescriptor.H3ID))
                        {
                            foreach (TraderTaskCondition condition in Mod.taskFindItemConditionsByItemID[vanillaItemDescriptor.H3ID])
                            {
                                if (!condition.fulfilled)
                                {
                                    ++condition.itemCount;
                                    EFM_TraderStatus.UpdateConditionFulfillment(condition);
                                }
                            }
                        }
                    }
                    Mod.AddSkillExp(EFM_Skill.uniqueLoot, 7);
                }

                if(__instance is FVRFireArm || (__instance is FVRMeleeWeapon && !vanillaItemDescriptor.inAll))
                {
                    // Add to All if necessary
                    Mod.AddToAll(__instance, null, vanillaItemDescriptor);
                }

                // Update lists
                if(vanillaItemDescriptor.locationIndex == 1)
                {
                    Mod.instance.LogInfo("\tWas in hideout");
                    // Was in hideout
                    if (Mod.currentBaseManager != null)
                    {
                        Mod.currentBaseManager.RemoveFromBaseInventory(vanillaItemDescriptor.transform, false);
                    }
                    else
                    {
                        Mod.instance.LogError("Began interacting with " + __instance.name + " but VID's location index indicated that it was in hideout but current base manger is null");
                    }

                    // Now on player
                    Mod.AddToPlayerInventory(vanillaItemDescriptor.transform, false);

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor, true);
                }
                else if(vanillaItemDescriptor.locationIndex == 2)
                {
                    Mod.instance.LogInfo("\tWas in raid");
                    // Now on player
                    Mod.AddToPlayerInventory(vanillaItemDescriptor.transform, true);

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor, true);
                }
                else if (vanillaItemDescriptor.locationIndex == 3)
                {
                    Mod.instance.LogInfo("\tWas in hideout area slot");
                    // Now on player
                    Mod.AddToPlayerInventory(vanillaItemDescriptor.transform, true);

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor, true);

                    //foreach (EFM_BaseAreaManager baseAreaManager in Mod.currentBaseManager.baseAreaManagers)
                    //{
                    //    baseAreaManager.UpdateBasedOnItem(vanillaItemDescriptor.H3ID);
                    //}
                }
            }

            // Check if in trade volume or container
            EFM_TradeVolume tradeVolume = null;
            if (__instance.transform.parent != null && __instance.transform.parent.parent != null)
            {
                tradeVolume = __instance.transform.parent.parent.GetComponent<EFM_TradeVolume>();
            }
            if (tradeVolume != null)
            {
                // Reset cols of item so that they are non trigger again and can collide with the world
                for (int i = tradeVolume.resetColPairs.Count - 1; i >= 0; --i)
                {
                    if (tradeVolume.resetColPairs[i].physObj.Equals(__instance))
                    {
                        foreach (Collider col in tradeVolume.resetColPairs[i].colliders)
                        {
                            col.isTrigger = false;
                        }
                        tradeVolume.resetColPairs.RemoveAt(i);
                        break;
                    }
                }

                tradeVolume.market.UpdateBasedOnItem(false, __instance.GetComponent<EFM_CustomItemWrapper>(), __instance.GetComponent<EFM_VanillaItemDescriptor>());
            }
            else if (__instance.transform.parent != null && __instance.transform.parent.parent != null)
            {
                EFM_CustomItemWrapper containerItemWrapper = __instance.transform.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                if(containerItemWrapper != null && (containerItemWrapper.itemType == Mod.ItemType.Backpack || 
                                                    containerItemWrapper.itemType == Mod.ItemType.Container || 
                                                    containerItemWrapper.itemType == Mod.ItemType.Pouch))
                {
                    containerItemWrapper.currentWeight -= customItemWrapper != null ? customItemWrapper.currentWeight : vanillaItemDescriptor.currentWeight;

                    Mod.instance.LogInfo("Grabbed item from container: " + containerItemWrapper.name + ", prevol: " + containerItemWrapper.containingVolume);
                    containerItemWrapper.containingVolume -= customItemWrapper != null ? customItemWrapper.volumes[customItemWrapper.mode] : vanillaItemDescriptor.volume;
                    Mod.instance.LogInfo("Postvol: " + containerItemWrapper.containingVolume);

                    // Reset cols of item so that they are non trigger again and can collide with the world and the container
                    for (int i = containerItemWrapper.resetColPairs.Count - 1; i >= 0; --i)
                    {
                        if (containerItemWrapper.resetColPairs[i].physObj.Equals(__instance))
                        {
                            foreach(Collider col in containerItemWrapper.resetColPairs[i].colliders)
                            {
                                col.isTrigger = false;
                            }
                            containerItemWrapper.resetColPairs.RemoveAt(i);
                            break;
                        }
                    }

                    __instance.RecoverRigidbody();
                }
            }
        }

        public static void SetItemLocationIndex(int locationIndex, EFM_CustomItemWrapper customItemWrapper, EFM_VanillaItemDescriptor vanillaItemDescriptor, bool updateWeight = true)
        {
            if (customItemWrapper != null)
            {
                if (updateWeight)
                {
                    if (customItemWrapper.locationIndex == 0 && locationIndex != 0)
                    {
                        // Taken out of player inventory
                        Mod.weight -= customItemWrapper.currentWeight;
                    }
                    else if (customItemWrapper.locationIndex != 0 && locationIndex == 0)
                    {
                        // Added to player inventory
                        Mod.weight += customItemWrapper.currentWeight;
                    }
                }

                customItemWrapper.locationIndex = locationIndex;

                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            SetItemLocationIndex(locationIndex, innerItem.GetComponent<EFM_CustomItemWrapper>(), innerItem.GetComponent<EFM_VanillaItemDescriptor>(), false);
                        }
                    }
                }
                else if (customItemWrapper.itemType == Mod.ItemType.Container || customItemWrapper.itemType == Mod.ItemType.Pouch || customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    foreach (Transform innerItem in customItemWrapper.containerItemRoot)
                    {
                        SetItemLocationIndex(locationIndex, innerItem.GetComponent<EFM_CustomItemWrapper>(), innerItem.GetComponent<EFM_VanillaItemDescriptor>(), false);
                    }
                }
            }
            else if(vanillaItemDescriptor != null)
            {
                if (updateWeight)
                {
                    if (vanillaItemDescriptor.locationIndex == 0 && locationIndex != 0)
                    {
                        // Taken out of player inventory
                        Mod.weight -= vanillaItemDescriptor.currentWeight;
                    }
                    else if (vanillaItemDescriptor.locationIndex != 0 && locationIndex == 0)
                    {
                        // Added to player inventory
                        Mod.weight += vanillaItemDescriptor.currentWeight;
                    }
                }

                vanillaItemDescriptor.locationIndex = locationIndex;

                FVRPhysicalObject physObj = vanillaItemDescriptor.GetComponent<FVRPhysicalObject>();
                if(physObj != null)
                {
                    if (physObj is FVRFireArm)
                    {
                        FVRFireArm asFireArm = (FVRFireArm)physObj;

                        // Ammo container
                        if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                        {
                            EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Magazine.GetComponent<EFM_VanillaItemDescriptor>();
                            if(ammoContainerVID != null)
                            {
                                ammoContainerVID.locationIndex = locationIndex;
                            }
                            //else Mag could be internal, not interactable, and so, no VID
                        }
                        else if(asFireArm.UsesClips && asFireArm.Clip != null)
                        {
                            EFM_VanillaItemDescriptor ammoContainerVID = asFireArm.Clip.GetComponent<EFM_VanillaItemDescriptor>();
                            if (ammoContainerVID != null)
                            {
                                ammoContainerVID.locationIndex = locationIndex;
                            }
                            //else Clip could be internal, not interactable, and so, no VID
                        }

                        // Attachments
                        if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                        {
                            foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                            {
                                // TODO: Review if we ever update the weight of the firearm dynamically as we attach attachments to it, we will need to pass
                                // false here instead of updateWeight because the attachment's weight was already included in the firearm's weight
                                SetItemLocationIndex(locationIndex, null, attachment.GetComponent<EFM_VanillaItemDescriptor>(), updateWeight);
                            }
                        }
                    }
                    else if(physObj is FVRFireArmAttachment)
                    {
                        FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)physObj;

                        if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
                        {
                            foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
                            {
                                // TODO: Review if we ever update the weight of the attachment dynamically as we attach attachments to it, we will need to pass
                                // false here instead of updateWeight because the attachment's weight was already included in the attachment's weight
                                SetItemLocationIndex(locationIndex, null, attachment.GetComponent<EFM_VanillaItemDescriptor>(), updateWeight);
                            }
                        }
                    }
                }
            }
            else
            {
                Mod.instance.LogError("Attempted to set location index on item without providing CIW or VID");
            }
        }

        static void Postfix(ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (Mod.beginInteractingEquipRig)
            {
                Mod.beginInteractingEquipRig = false;
            }

            // Ensure the item is parented to null
            // This is because typically the items are parented to player body, when we move with two axis mode the position of held item is then moved along with the body
            // This move is done using body.transform.position = x, moving items this way with many children which each have many colliders is very demanding to the physics engine
            // This causes extreme lag and is not necessary since the item's position will be set to the hand's through the item's rigidbody instead which is much less performance heavy
            // This was mainly a problem with backpacks with many items in them begin held while moving
            __instance.transform.parent = null;
        }
    }

    // Patches FVRPlayerHitbox.Damage(Damage) in order to implement our own armor's damage resistance
    class DamagePatch
    {
        static bool Prefix(Damage d, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            if (d.Dam_Blinding > 0f && __instance.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
            {
                float num = Vector3.Angle(d.strikeDir, GM.CurrentPlayerBody.Head.forward);
                if (num > 90f)
                {
                    GM.CurrentPlayerBody.BlindPlayer(d.Dam_Blinding);
                }
            }
            if (GM.CurrentPlayerBody.IsBlort)
            {
                d.Dam_TotalEnergetic = 0f;
            }
            else if (GM.CurrentPlayerBody.IsDlort)
            {
                d.Dam_TotalEnergetic *= 3f;
            }
            float damage = d.Dam_TotalKinetic + d.Dam_TotalEnergetic;

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.instance.LogInfo("Player took " + damage + " damage (Damage)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (/*__instance.Body.RegisterPlayerHit(damage, false)*/ RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f); 
                }
            }

            return false;
        }

        public static object[] Damage(float amount, FVRPlayerHitbox hitbox = null, int partIndex = -1)
        {
            if(hitbox == null & partIndex == -1)
            {
                Mod.instance.LogError("Damage() called without hitbox nor partindex specified");
                return null;
            }

            // TODO: Apply drug multipliers

            int actualPartIndex = partIndex;
            float actualAmount = amount;
            float vitalityLevel = Mod.skills[2].currentProgress / 100;
            float bleedingChanceModifier = 0.012f * vitalityLevel;
            float healthLevel = Mod.skills[3].currentProgress / 100;
            float fractureChanceModifier = 0.012f * healthLevel;
            if (hitbox != null)
            {
                // Apply damage resist/multiplier based on equipment and body part
                if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
                {
                    Mod.instance.LogInfo("\tTo Head");
                    if (Mod.health[0] <= 0)
                    {
                        Mod.instance.LogInfo("\t\tHealth 0, killing player");
                        Mod.currentRaidManager.KillPlayer();
                    }

                    actualPartIndex = 0;

                    // Add a headshot damage multiplier
                    //damage *= Mod.headshotDamageMultiplier;

                    // We will actually be applying normal damage to the head, considering if health <= 0 is instant death and it only has 35 HP

                    // Process damage resist from EFM_EquipmentSlot.CurrentHelmet
                    float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                    float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                    if (EFM_EquipmentSlot.currentHeadwear != null && EFM_EquipmentSlot.currentHeadwear.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentHeadwear.coverage)
                    {
                        heavyBleedChance /= 3;
                        lightBleedChance /= 3;
                        EFM_EquipmentSlot.currentHeadwear.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentHeadwear.damageResist;
                        actualAmount *= EFM_EquipmentSlot.currentHeadwear.damageResist;
                    }

                    // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                    // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                    float chance = UnityEngine.Random.value;
                    if (chance <= heavyBleedChance)
                    {
                        Mod.instance.LogInfo("\t\tCaused heavy bleed");
                        EFM_Effect heavyBleedEffect = new EFM_Effect();
                        heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                        heavyBleedEffect.partIndex = 0;
                        if (vitalityLevel >= 51) 
                        {
                            heavyBleedEffect.hasTimer = true;
                            heavyBleedEffect.timer = 30;
                        }
                        EFM_Effect.effects.Add(heavyBleedEffect);
                    }
                    else if (chance <= lightBleedChance)
                    {
                        Mod.instance.LogInfo("\t\tCaused light bleed");
                        EFM_Effect lightBleedEffect = new EFM_Effect();
                        lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                        lightBleedEffect.partIndex = 0;
                        if (vitalityLevel >= 51)
                        {
                            lightBleedEffect.hasTimer = true;
                            lightBleedEffect.timer = 20;
                        }
                        EFM_Effect.effects.Add(lightBleedEffect);
                    }
                }
                else if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Torso)
                {
                    // These numbers are cumulative
                    float thoraxChance = 0.3f;
                    float stomachChance = 0.55f;
                    float rightArmChance = 0.625f;
                    float leftArmChance = 0.7f;
                    float rightLegChance = 0.85f;
                    float leftLegChance = 1f;

                    float partChance = UnityEngine.Random.value;
                    if (partChance >= 0 && partChance <= thoraxChance)
                    {
                        Mod.instance.LogInfo("\tTo thorax");
                        if (Mod.health[1] <= 0)
                        {
                            Mod.instance.LogInfo("\t\tHealth 0, killing player");
                            Mod.currentRaidManager.KillPlayer();
                        }

                        actualPartIndex = 1;

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance /= 3;
                            lightBleedChance /= 3;
                            EFM_EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EFM_EquipmentSlot.currentArmor.damageResist;
                        }

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }
                    }
                    else if (partChance > thoraxChance && partChance <= stomachChance)
                    {
                        Mod.instance.LogInfo("\tTo stomach");
                        actualPartIndex = 2;

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance = 0.1f - 0.1f * bleedingChanceModifier;
                        float lightBleedChance = 0.25f - 0.25f * bleedingChanceModifier;
                        if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance /= 3;
                            lightBleedChance /= 3;
                            EFM_EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EFM_EquipmentSlot.currentArmor.damageResist;
                        }

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }
                    }
                    else if (partChance > stomachChance && partChance <= rightArmChance)
                    {
                        Mod.instance.LogInfo("\tTo right arm");
                        actualPartIndex = 4;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused fracture");
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                        }
                    }
                    else if (partChance > rightArmChance && partChance <= leftArmChance)
                    {
                        Mod.instance.LogInfo("\tTo left arm");
                        actualPartIndex = 3;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused fracture");
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                        }
                    }
                    else if (partChance > leftArmChance && partChance <= rightLegChance)
                    {
                        Mod.instance.LogInfo("\tTo right leg");
                        actualPartIndex = 6;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused fracture");
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                        }
                    }
                    else if (partChance > rightLegChance && partChance <= leftLegChance)
                    {
                        Mod.instance.LogInfo("\tTo left leg");
                        actualPartIndex = 5;

                        float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                        // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                        // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                        float bleedValue = UnityEngine.Random.value;
                        if (bleedValue <= heavyBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused heavy bleed");
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue <= lightBleedChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused light bleed");
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChance)
                        {
                            Mod.instance.LogInfo("\t\tCaused fracture");
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                        }
                    }
                }
                else if (hitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Hand)
                {
                    actualPartIndex = hitbox.Hand.IsThisTheRightHand ? 3 : 4;
                    Mod.instance.LogInfo("\tTo hand, actual part: " + actualPartIndex);

                    // Add a damage resist because should do less damage when hit to hand than when hit to torso
                    //damage *= Mod.handDamageResist;

                    float heavyBleedChance = 0.05f - 0.05f * bleedingChanceModifier;
                    float lightBleedChance = 0.15f - 0.15f * bleedingChanceModifier;
                    float fractureChance = 0.02f - 0.02f * fractureChanceModifier;

                    // TODO: Maybe we can check which ammo the sosig is using, and when we create the Damage object we pass here,
                    // we could set Cutting/Piercing to define bleed chance. Until then, every shot has chance of bleed.
                    float bleedValue = UnityEngine.Random.value;
                    if (bleedValue <= heavyBleedChance)
                    {
                        Mod.instance.LogInfo("\t\tCaused heavy bleed");
                        EFM_Effect heavyBleedEffect = new EFM_Effect();
                        heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                        heavyBleedEffect.partIndex = actualPartIndex;
                        if (vitalityLevel >= 51)
                        {
                            heavyBleedEffect.hasTimer = true;
                            heavyBleedEffect.timer = 30;
                        }
                        EFM_Effect.effects.Add(heavyBleedEffect);
                    }
                    else if (bleedValue <= lightBleedChance)
                    {
                        Mod.instance.LogInfo("\t\tCaused light bleed");
                        EFM_Effect lightBleedEffect = new EFM_Effect();
                        lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                        lightBleedEffect.partIndex = actualPartIndex;
                        if (vitalityLevel >= 51)
                        {
                            lightBleedEffect.hasTimer = true;
                            lightBleedEffect.timer = 20;
                        }
                        EFM_Effect.effects.Add(lightBleedEffect);
                    }

                    if (UnityEngine.Random.value < fractureChance)
                    {
                        Mod.instance.LogInfo("\t\tCaused fracture");
                        EFM_Effect fractureEffect = new EFM_Effect();
                        fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                        fractureEffect.partIndex = actualPartIndex;
                        EFM_Effect.effects.Add(fractureEffect);
                    }
                }
            }
            else
            {
                switch (actualPartIndex)
                {
                    case 0: // Head
                        if (Mod.health[0] <= 0)
                        {
                            Mod.currentRaidManager.KillPlayer();
                        }

                        // Process damage resist from EFM_EquipmentSlot.CurrentHelmet
                        float heavyBleedChance0 = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance0 = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EFM_EquipmentSlot.currentHeadwear != null && EFM_EquipmentSlot.currentHeadwear.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentHeadwear.coverage)
                        {
                            heavyBleedChance0 /= 3;
                            lightBleedChance0 /= 3;
                            EFM_EquipmentSlot.currentHeadwear.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentHeadwear.damageResist;
                            actualAmount *= EFM_EquipmentSlot.currentHeadwear.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue0 = UnityEngine.Random.value;
                        if (bleedValue0 <= heavyBleedChance0)
                        {
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = 0;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue0 <= lightBleedChance0)
                        {
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = 0;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }
                        break;
                    case 1: // Thorax
                        if (Mod.health[1] <= 0)
                        {
                            Mod.currentRaidManager.KillPlayer();
                        }

                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance1 = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChance1 = 0.15f - 0.15f * bleedingChanceModifier;
                        if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance1 /= 3;
                            lightBleedChance1 /= 3;
                            EFM_EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EFM_EquipmentSlot.currentArmor.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue1 = UnityEngine.Random.value;
                        if (bleedValue1 <= heavyBleedChance1)
                        {
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue1 <= lightBleedChance1)
                        {
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }
                        break;
                    case 2: // Stomach
                        // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                        float heavyBleedChance2 = 0.1f - 0.1f * bleedingChanceModifier;
                        float lightBleedChance2 = 0.25f - 0.25f * bleedingChanceModifier;
                        if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentArmor.coverage)
                        {
                            heavyBleedChance2 /= 3;
                            lightBleedChance2 /= 3;
                            EFM_EquipmentSlot.currentArmor.armor -= actualAmount - actualAmount * EFM_EquipmentSlot.currentArmor.damageResist;
                            actualAmount *= EFM_EquipmentSlot.currentArmor.damageResist;
                        }

                        // Apply possible effects
                        float bleedValue2 = UnityEngine.Random.value;
                        if (bleedValue2 <= heavyBleedChance2)
                        {
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue2 <= lightBleedChance2)
                        {
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }
                        break;
                    case 3: // Left arm
                    case 4: // Right arm
                        float heavyBleedChanceArm = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChanceArm = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChanceArm = 0.02f - 0.02f * fractureChanceModifier;

                        // Apply possible effects
                        float bleedValue3 = UnityEngine.Random.value;
                        if (bleedValue3 <= heavyBleedChanceArm)
                        {
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValue3 <= lightBleedChanceArm)
                        {
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChanceArm)
                        {
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                            // TODO: Player fracture sound
                        }
                        break;
                    case 5:
                    case 6:
                        float heavyBleedChanceLeg = 0.05f - 0.05f * bleedingChanceModifier;
                        float lightBleedChanceLeg = 0.15f - 0.15f * bleedingChanceModifier;
                        float fractureChanceLeg = 0.02f - 0.02f * fractureChanceModifier;

                        // Apply possible effects
                        float bleedValueLeg = UnityEngine.Random.value;
                        if (bleedValueLeg <= heavyBleedChanceLeg)
                        {
                            EFM_Effect heavyBleedEffect = new EFM_Effect();
                            heavyBleedEffect.effectType = EFM_Effect.EffectType.HeavyBleeding;
                            heavyBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                heavyBleedEffect.hasTimer = true;
                                heavyBleedEffect.timer = 30;
                            }
                            EFM_Effect.effects.Add(heavyBleedEffect);
                        }
                        else if (bleedValueLeg <= lightBleedChanceLeg)
                        {
                            EFM_Effect lightBleedEffect = new EFM_Effect();
                            lightBleedEffect.effectType = EFM_Effect.EffectType.LightBleeding;
                            lightBleedEffect.partIndex = actualPartIndex;
                            if (vitalityLevel >= 51)
                            {
                                lightBleedEffect.hasTimer = true;
                                lightBleedEffect.timer = 20;
                            }
                            EFM_Effect.effects.Add(lightBleedEffect);
                        }

                        if (UnityEngine.Random.value < fractureChanceLeg)
                        {
                            EFM_Effect fractureEffect = new EFM_Effect();
                            fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                            fractureEffect.partIndex = actualPartIndex;
                            EFM_Effect.effects.Add(fractureEffect);
                            // TODO: Play fracture sound
                        }
                        break;
                }
            }

            return new object[] { actualPartIndex, actualAmount };
        }

        public static bool RegisterPlayerHit(int partIndex, float totalDamage, bool FromSelf)
        {
            if (GM.CurrentSceneSettings.DoesDamageGetRegistered && GM.CurrentSceneSettings.DeathResetPoint != null && !GM.IsDead())
            {
                Mod.AddSkillExp(EFM_Skill.damageTakenAction * totalDamage, 2);

                GM.CurrentPlayerBody.Health -= totalDamage;

                GM.CurrentPlayerBody.HitEffect();
                if (GM.CurrentPlayerBody.Health <= 0f)
                {
                    Mod.currentRaidManager.KillPlayer();
                    return true;
                }

                // Parts other than head and thorax at zero distribute damage over all other parts
                float[] destroyedMultiplier = new float[] { 0, 0, 1.5f, 0.7f, 0.7f, 1, 1};
                float actualTotalDamage = 0;
                if (partIndex >= 2)
                {
                    if (Mod.health[partIndex] <= 0)
                    {
                        for (int i = 0; i < Mod.health.Length; ++i)
                        {
                            if (i != partIndex)
                            {
                                float actualDamage = Mathf.Min(totalDamage / 6 * destroyedMultiplier[partIndex], Mod.health[i]);
                                Mod.health[i] -= actualDamage;
                                actualTotalDamage += actualDamage;

                                if (i == 0 || i == 1)
                                {
                                    if (Mod.health[0] <= 0 || Mod.health[1] <= 0)
                                    {
                                        Mod.currentRaidManager.KillPlayer();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        actualTotalDamage = totalDamage;
                        Mod.health[partIndex] = Mathf.Clamp(Mod.health[partIndex] - totalDamage, 0, Mod.currentMaxHealth[partIndex]);
                    }
                }
                else if (Mod.health[partIndex] <= 0) // Part is head or thorax, destroyed
                {
                    Mod.currentRaidManager.KillPlayer();
                    return true;
                }
                else // Part is head or thorax, not yet destroyed
                {
                    actualTotalDamage = totalDamage;
                    Mod.health[partIndex] = Mathf.Clamp(Mod.health[partIndex] - totalDamage, 0, Mod.currentMaxHealth[partIndex]);
                }
                GM.CurrentSceneSettings.OnPlayerTookDamage(actualTotalDamage / 440f);
            }
            return false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(float) in order to implement our own armor's damage resistance
    class DamageFloatPatch
    {
        static bool Prefix(float i, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            float damage = Mathf.Clamp(i * __instance.DamageMultiplier - __instance.DamageResist, 0f, 10000f);

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.instance.LogInfo("Player took " + damage + " damage (float)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = DamagePatch.Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (/*__instance.Body.RegisterPlayerHit(damage, false)*/ DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f); 
                }
            }

            return false;
        }
    }

    // Patches FVRPlayerHitbox.Damage(DamageDealt) in order to implement our own armor's damage resistance
    class DamageDealtPatch
    {
        static bool Prefix(DamageDealt dam, ref FVRPlayerHitbox __instance, ref AudioSource ___m_aud)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (!GM.CurrentSceneSettings.DoesDamageGetRegistered)
            {
                return false;
            }
            float damage = Mathf.Clamp(dam.PointsDamage * __instance.DamageMultiplier - __instance.DamageResist, 0f, 10000f);

            // Apply damage resist
            damage *= 0.05f; // For 500 to become 25

            Mod.instance.LogInfo("Player took " + damage + " damage (Dealt)");

            // Apply damage resist/multiplier based on equipment and body part
            object[] damageData = DamagePatch.Damage(damage, __instance);

            float actualDamage = (float)damageData[1];
            if (actualDamage > 0.1f && __instance.IsActivated)
            {
                if (/*__instance.Body.RegisterPlayerHit(damage, false)*/ DamagePatch.RegisterPlayerHit((int)damageData[0], actualDamage, false))
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Reset, 0.4f);
                }
                else if (!GM.IsDead())
                {
                    ___m_aud.PlayOneShot(__instance.AudClip_Hit, 0.4f); 
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.TestCollider() in order to be able to have interactable colliders on other gameobjects than the root
    // This completely replaces the original 
    class HandTestColliderPatch
    {
        static bool Prefix(Collider collider, bool isEnter, bool isPalm, ref FVRViveHand.HandState ___m_state, ref bool ___m_isClosestInteractableInPalm, ref FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            FVRInteractiveObject interactiveObject = collider.gameObject.GetComponent<FVRInteractiveObject>();
            EFM_OtherInteractable otherInteractable = collider.gameObject.GetComponent<EFM_OtherInteractable>();

            FVRInteractiveObject interactiveObjectToUse = otherInteractable != null ? otherInteractable.interactiveObject : interactiveObject;

            // Could be an interactable layer object without FVRInteractiveObject attached, if so we skip
            // For example, backpacks' MainContainer
            if(interactiveObjectToUse == null)
            {
                return false;
            }

            if (isEnter)
            {
                FVRInteractiveObject component = interactiveObjectToUse;
                component.Poke(__instance);
                return false;
            }

            if (___m_state == FVRViveHand.HandState.Empty && interactiveObjectToUse != null)
            {
                FVRInteractiveObject component2 = interactiveObjectToUse;
                if (component2 != null && component2.IsInteractable() && !component2.IsSelectionRestricted())
                {
                    float num = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere.transform.position);
                    float num2 = Vector3.Distance(interactiveObjectToUse.transform.position, __instance.Display_InteractionSphere_Palm.transform.position);
                    if (__instance.ClosestPossibleInteractable == null)
                    {
                        __instance.ClosestPossibleInteractable = component2;
                        if (num < num2)
                        {
                            ___m_isClosestInteractableInPalm = false;
                        }
                        else
                        {
                            ___m_isClosestInteractableInPalm = true;
                        }
                    }
                    else if (__instance.ClosestPossibleInteractable != component2)
                    {
                        float num3 = Vector3.Distance(__instance.ClosestPossibleInteractable.transform.position, __instance.Display_InteractionSphere.transform.position);
                        float num4 = Vector3.Distance(__instance.ClosestPossibleInteractable.transform.position, __instance.Display_InteractionSphere_Palm.transform.position);
                        bool flag = true;
                        if (num < num2)
                        {
                            flag = false;
                        }
                        if (flag && num2 < num4 && ___m_isClosestInteractableInPalm)
                        {
                            ___m_isClosestInteractableInPalm = true;
                            __instance.ClosestPossibleInteractable = component2;
                        }
                        else if (!flag && num < num3)
                        {
                            ___m_isClosestInteractableInPalm = false;
                            __instance.ClosestPossibleInteractable = component2;
                        }
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.HandTriggerExit() in order to be able to have interactable colliders on other gameobjects than the root
    // This completely replaces the original 
    class HandTriggerExitPatch
    {
        static bool Prefix(Collider collider, ref FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            FVRInteractiveObject interactiveObject = collider.gameObject.GetComponent<FVRInteractiveObject>();
            EFM_OtherInteractable otherInteractable = collider.gameObject.GetComponent<EFM_OtherInteractable>();
            FVRInteractiveObject interactiveObjectToUse = otherInteractable != null ? otherInteractable.interactiveObject : interactiveObject;

            FieldInfo isClosestInteractableInPalmField = typeof(FVRViveHand).GetField("m_isClosestInteractableInPalm", BindingFlags.NonPublic | BindingFlags.Instance);

            if (interactiveObjectToUse != null)
            {
                FVRInteractiveObject component = interactiveObjectToUse;
                if (__instance.ClosestPossibleInteractable == component)
                {
                    __instance.ClosestPossibleInteractable = null;
                    isClosestInteractableInPalmField.SetValue(__instance, false);
                }
            }

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBoltKey.KeyForwardBack to bypass H3VR key type functionality and to make sure it uses correct key prefab
    // This completely replaces the original
    class KeyForwardBackPatch
    {
        static bool Prefix(float ___distBetween, ref float ___m_keyLerp, ref SideHingedDestructibleDoorDeadBoltKey __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.parent.GetComponent<EFM_DoorWrapper>();

            Vector3 pos = __instance.m_hand.Input.Pos;
            Vector3 vector = pos - __instance.KeyIn.position;
            Vector3 vector2 = Vector3.ProjectOnPlane(vector, __instance.DeadBolt.Mount.up);
            vector2 = Vector3.ProjectOnPlane(vector2, __instance.DeadBolt.Mount.right);
            Vector3 a = __instance.KeyIn.position + vector2;
            float num = Vector3.Distance(a, pos);
            float num2 = Vector3.Distance(a, __instance.KeyIn.position);
            float num3 = Vector3.Distance(a, __instance.KeyOut.position);
            if (num3 <= ___distBetween && num2 <= ___distBetween)
            {
                float num4 = (___distBetween - num3) / ___distBetween;
                // Use object ID instead of type
                if (/*__instance.m_insertedType != __instance.KeyType*/ !doorWrapper.keyID.Equals(__instance.KeyFO))
                {
                    num4 = Mathf.Clamp(num4, 0.7f, 1f);
                    if (num4 <= 0.71f && (double)___m_keyLerp > 0.711)
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyStop, __instance.transform.position);
                    }
                }
                if (num4 < 0.3f)
                {
                    if (___m_keyLerp >= 0.3f)
                    {
                        SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyInsert, __instance.transform.position);
                    }
                    num4 = 0f;
                }
                __instance.transform.position = Vector3.Lerp(__instance.KeyIn.position, __instance.KeyOut.position, num4);
                ___m_keyLerp = num4;
            }
            else if (num2 > ___distBetween && num2 > num3 && __instance.DeadBolt.m_timeSinceKeyInOut > 1f)
            {
                __instance.DeadBolt.m_timeSinceKeyInOut = 0f;
                FVRViveHand hand = __instance.m_hand;
                // Use correct key item prefab
                GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(int.TryParse(__instance.KeyFO, out int result) ? Mod.itemPrefabs[result] : IM.OD[__instance.KeyFO].GetGameObject(), __instance.transform.position, __instance.transform.rotation);
                LockKey component = gameObject.GetComponent<LockKey>();
                ___m_keyLerp = 1f;
                __instance.ForceBreakInteraction();
                component.BeginInteraction(hand);
                hand.CurrentInteractable = component;
                SM.PlayCoreSound(FVRPooledAudioType.Generic, __instance.AudEvent_KeyExtract, __instance.transform.position);
                __instance.DeadBolt.SetKeyState(false);
            }
            else if (num > ___distBetween)
            {
                __instance.ForceBreakInteraction();
            }

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBoltKey.UpdateDisplayBasedOnType to make sure it uses correct key prefab
    // This completely replaces the original
    class UpdateDisplayBasedOnTypePatch
    {
        static bool Prefix(ref SideHingedDestructibleDoorDeadBoltKey __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.parent.GetComponent<EFM_DoorWrapper>();

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(int.TryParse(__instance.KeyFO, out int result) ? Mod.itemPrefabs[result] : IM.OD[__instance.KeyFO].GetGameObject(), __instance.transform.position, __instance.transform.rotation);
            LockKey component = gameObject.GetComponent<LockKey>();
            __instance.KeyMesh.mesh = component.KeyMesh.mesh;
            __instance.TagMesh.mesh = component.TagMesh.mesh;

            return false;
        }
    }

    // Patches SideHingedDestructibleDoor.Init to prevent door initialization when using grillhouse ones
    class DoorInitPatch
    {
        static bool Prefix(ref SideHingedDestructibleDoor __instance)
        {
            // If grillhouseSecure, it means we are currently loading into grillhouse but it is not a meatov scene, so need to check also because
            // although this is not a meatov scene, we still dont want to init doors if initDoors == false
            if (!Mod.inMeatovScene && !Mod.grillHouseSecure)
            {
                return true;
            }

            if (!Mod.initDoors)
            {
                return false;
            }

            return true;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.TurnBolt to make sure we need to rotate the hand the right way if flipped
    // This completely replaces the original
    class DeadBoltPatch
    {
        static bool Prefix(Vector3 upVec, ref Vector3 ___lastHandForward, ref float ___m_curRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // First check if lock is flipped
            Vector3 dirVecToUse = __instance.Mount.forward;
            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.GetComponent<EFM_DoorWrapper>();
            if(doorWrapper != null && doorWrapper.flipLock)
            {
                dirVecToUse *= -1; // Negate forward vector
            }

            Vector3 lhs = Vector3.ProjectOnPlane(upVec, dirVecToUse);
            Vector3 rhs = Vector3.ProjectOnPlane(___lastHandForward, dirVecToUse);
            float num = Mathf.Atan2(Vector3.Dot(dirVecToUse, Vector3.Cross(lhs, rhs)), Vector3.Dot(lhs, rhs)) * 57.29578f;
            ___m_curRot -= num;
            ___m_curRot = Mathf.Clamp(___m_curRot, __instance.MinRot, __instance.MaxRot);
            ___lastHandForward = lhs;

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.SetStartingLastHandForward to make sure we need to rotate the hand the right way if flipped
    // This completely replaces the original
    class DeadBoltLastHandPatch
    {
        static bool Prefix(Vector3 upVec, ref Vector3 ___lastHandForward, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // First check if lock is flipped
            Vector3 dirVecToUse = __instance.Mount.forward;
            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.GetComponent<EFM_DoorWrapper>();
            if(doorWrapper != null && doorWrapper.flipLock)
            {
                dirVecToUse *= -1; // Negate forward vector
            }

            ___lastHandForward = Vector3.ProjectOnPlane(upVec, dirVecToUse);

            return false;
        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.Awake to set correct vizRot if lock is flipped
    class DeadBoltAwakePatch
    {
        static void Postfix(ref float ___m_vizRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // First check if lock is flipped
            float yAngleToUse = 0;
            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.GetComponent<EFM_DoorWrapper>();
            if (doorWrapper != null && doorWrapper.flipLock)
            {
                yAngleToUse = 180;
            }

            __instance.transform.localEulerAngles = new Vector3(0f, yAngleToUse, ___m_vizRot);

        }
    }

    // Patches SideHingedDestructibleDoorDeadBolt.FVRFixedUpdate to set correct vizRot if lock is flipped
    class DeadBoltFVRFixedUpdatePatch
    {
        static void Postfix(ref float ___m_vizRot, ref SideHingedDestructibleDoorDeadBolt __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // First check if lock is flipped
            float yAngleToUse = 0;
            EFM_DoorWrapper doorWrapper = __instance.transform.parent.parent.parent.parent.GetComponent<EFM_DoorWrapper>();
            if(doorWrapper != null && doorWrapper.flipLock)
            {
                yAngleToUse = 180;
            }

            __instance.transform.localEulerAngles = new Vector3(0f, yAngleToUse, ___m_vizRot);
        }
    }

    // Patches FVRInteractiveObject.SetAllCollidersToLayer to make sure it doesn't set the layer of GOs with layer already set to NonBlockingSmoke
    // because layer is used by open backpacks and rigs in order to prevent items from colliding with them so its easier to put items in the container
    // This completely replaces the original
    class InteractiveSetAllLayersPatch
    {
        static bool Prefix(bool triggersToo, string layerName, ref Collider[] ___m_colliders, ref FVRInteractiveObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (triggersToo)
            {
                foreach (Collider collider in ___m_colliders)
                {
                    if (collider != null)
                    {
                        collider.gameObject.layer = LayerMask.NameToLayer(layerName);
                    }
                }
            }
            else
            {
                int nonBlockingSmokeLayer = LayerMask.NameToLayer("NonBlockingSmoke");
                foreach (Collider collider2 in ___m_colliders)
                {
                    // Also check current layer so we dont set it to default if NonBlockingSmoke
                    if (collider2 != null && !collider2.isTrigger && collider2.gameObject.layer != nonBlockingSmokeLayer)
                    {
                        collider2.gameObject.layer = LayerMask.NameToLayer(layerName);
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRViveHand.Update to change the grab laser input
    // This completely replaces the original
    class HandUpdatePatch
    {
        public static bool fullDescDirectionDown = false; // The direction in which to slide finger on touchpad to open full description for index and oculus

        static bool leftTouchWithinDescRange;
        static bool rightTouchWithinDescRange;
        static float rightPreviousFrameTPAxisY;
        static float leftPreviousFrameTPAxisY;

        //flag2 = __instance.Input.TouchpadTouched && __instance.Input.TouchpadAxes.magnitude < 0.2f;
        static bool Prefix(ref FVRViveHand.HandInitializationState ___m_initState, ref FVRPhysicalObject ___m_selectedObj,
                           ref float ___m_reset, ref bool ___m_isObjectInTransit, ref bool ___m_hasOverrider, ref InputOverrider ___m_overrider,
                           ref bool ___m_touchSphereMatInteractable, ref bool ___m_touchSphereMatInteractablePalm,
                           ref bool ___m_isClosestInteractableInPalm, ref FVRViveHand.HandState ___m_state, ref RaycastHit ___m_pointingHit,
                           ref bool ___m_isWristMenuActive,ref RaycastHit ___m_grabHit, ref Collider[] ___m_rawGrabCols,
                           ref FVRPhysicalObject ___m_grabityHoveredObject, ref float ___m_timeSinceLastGripButtonDown, ref float ___m_timeGripButtonHasBeenHeld,
                           ref bool ___m_canMadeGrabReleaseSoundThisFrame, ref FVRViveHand __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___m_initState == FVRViveHand.HandInitializationState.Uninitialized)
            {
                return false;
            }

            if (___m_selectedObj != null && ___m_selectedObj.IsHeld)
            {
                ___m_selectedObj = null;
                ___m_reset = 0f;
                ___m_isObjectInTransit = false;
            }
            if (___m_reset >= 0f && ___m_isObjectInTransit)
            {
                if (___m_selectedObj != null && Vector3.Distance(___m_selectedObj.transform.position, __instance.transform.position) < 0.4f)
                {
                    Vector3 b = __instance.transform.position - ___m_selectedObj.transform.position;
                    Vector3 vector = Vector3.Lerp(___m_selectedObj.RootRigidbody.velocity, b, Time.deltaTime * 2f);
                    ___m_selectedObj.RootRigidbody.velocity = Vector3.ClampMagnitude(vector, ___m_selectedObj.RootRigidbody.velocity.magnitude);
                    ___m_selectedObj.RootRigidbody.velocity = vector;
                    ___m_selectedObj.RootRigidbody.drag = 1f;
                    ___m_selectedObj.RootRigidbody.angularDrag = 8f;
                    ___m_reset -= Time.deltaTime * 0.4f;
                }
                else
                {
                    ___m_reset -= Time.deltaTime;
                }
                if (___m_reset <= 0f)
                {
                    ___m_isObjectInTransit = false;
                    if (___m_selectedObj != null)
                    {
                        ___m_selectedObj.RecoverDrag();
                        ___m_selectedObj = null;
                    }
                }
            }

            typeof(FVRViveHand).GetMethod("HapticBuzzUpdate", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            typeof(FVRViveHand).GetMethod("TestQuickBeltDistances", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            __instance.PollInput();

            // If started touching this frame
            if (__instance.Input.TouchpadTouchDown)
            {
                // Store whether we are in range for description so that we can only activate description if we STARTED touching within the range
                if (__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
                {
                    if (__instance.IsThisTheRightHand)
                    {
                        rightTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                    }
                    else
                    {
                        leftTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                    }
                }
                else if(__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus)
                {
                    if (__instance.IsThisTheRightHand)
                    {
                        rightTouchWithinDescRange = fullDescDirectionDown ? __instance.Input.TouchpadAxes.y >= 0 : __instance.Input.TouchpadAxes.y <= 0;
                    }
                    else
                    {
                        leftTouchWithinDescRange = fullDescDirectionDown ? __instance.Input.TouchpadAxes.y >= 0 : __instance.Input.TouchpadAxes.y <= 0;
                    }
                }
            }
            else if (__instance.Input.TouchpadTouchUp ||
                    ((__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR) && __instance.Input.TouchpadAxes.magnitude >= 0.3f) || 
                    ((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && fullDescDirectionDown ? __instance.Input.TouchpadAxes.y < 0 : __instance.Input.TouchpadAxes.y > 0))
            {
                if (__instance.IsThisTheRightHand)
                {
                    rightTouchWithinDescRange = false;
                }
                else
                {
                    leftTouchWithinDescRange = false;
                }
            }

            if (___m_hasOverrider && ___m_overrider != null)
            {
                ___m_overrider.Process(ref __instance.Input);
            }
            else
            {
                ___m_hasOverrider = false;
            }
            //if (!(__instance.m_currentInteractable != null) || __instance.Input.TriggerPressed)
            //{
            //}
            if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsInteractable())
            {
                __instance.ClosestPossibleInteractable = null;
            }
            if (__instance.ClosestPossibleInteractable == null)
            {
                if (___m_touchSphereMatInteractable)
                {
                    ___m_touchSphereMatInteractable = false;
                    __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
                }
                if (___m_touchSphereMatInteractablePalm)
                {
                    ___m_touchSphereMatInteractablePalm = false;
                    __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
                }
            }
            else if (!___m_touchSphereMatInteractable && !___m_isClosestInteractableInPalm)
            {
                ___m_touchSphereMatInteractable = true;
                __instance.TouchSphere.material = __instance.TouchSpheteMat_Interactable;
                ___m_touchSphereMatInteractablePalm = false;
                __instance.TouchSphere_Palm.material = __instance.TouchSphereMat_NoInteractable;
            }
            else if (!___m_touchSphereMatInteractablePalm && ___m_isClosestInteractableInPalm)
            {
                ___m_touchSphereMatInteractablePalm = true;
                __instance.TouchSphere_Palm.material = __instance.TouchSpheteMat_Interactable;
                ___m_touchSphereMatInteractable = false;
                __instance.TouchSphere.material = __instance.TouchSphereMat_NoInteractable;
            }
            float d = 1f / GM.CurrentPlayerBody.transform.localScale.x;
            if (___m_state == FVRViveHand.HandState.Empty && !__instance.Input.BYButtonPressed && !__instance.Input.TouchpadPressed && __instance.ClosestPossibleInteractable == null && __instance.CurrentHoveredQuickbeltSlot == null && __instance.CurrentInteractable == null && !___m_isWristMenuActive)
            {
                if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out ___m_pointingHit, GM.CurrentSceneSettings.MaxPointingDistance, __instance.PointingLayerMask, QueryTriggerInteraction.Collide) && ___m_pointingHit.collider.gameObject.GetComponent<FVRPointable>())
                {
                    FVRPointable component = ___m_pointingHit.collider.gameObject.GetComponent<FVRPointable>();
                    if (___m_pointingHit.distance <= component.MaxPointingRange)
                    {
                        __instance.CurrentPointable = component;
                        __instance.PointingLaser.position = __instance.Input.OneEuroPointingPos;
                        __instance.PointingLaser.rotation = __instance.Input.OneEuroPointRotation;
                        __instance.PointingLaser.localScale = new Vector3(0.002f, 0.002f, ___m_pointingHit.distance) * d;
                    }
                    else
                    {
                        __instance.CurrentPointable = null;
                    }
                }
                else
                {
                    __instance.CurrentPointable = null;
                }
            }
            else
            {
                __instance.CurrentPointable = null;
            }

            // Might have to cancel movement if touching TP for description
            // Should only be applicable with Vive since movement and description share the touchpad
            if (__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
            {
                if (!(__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange))
                {
                    __instance.MovementManager.UpdateMovementWithHand(__instance);
                }
                else // Started touching within desc range, want to stop movement, sprinting, and smooth turning
                {
                    typeof(FVRMovementManager).GetField("m_isTwinStickSmoothTurningClockwise", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_isTwinStickSmoothTurningCounterClockwise", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_sprintingEngaged", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, false);
                    typeof(FVRMovementManager).GetField("m_twoAxisVelocity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(GM.CurrentMovementManager, Vector3.zero);
                }
            }
            else // Any other controller, or any that has enough buttons to have description AND movement at the same time, we should update movement
            {
                __instance.MovementManager.UpdateMovementWithHand(__instance);
            }

            // Keep a reference to touchpad touch inputs so we can still use descriptions after touchpad input has been flushed
            bool touchpadTouched = __instance.Input.TouchpadTouched;
            float touchpadAxisMagnitude = __instance.Input.TouchpadAxes.magnitude;
            float touchpadAxisY = __instance.Input.TouchpadAxes.y;
            bool touchpadDown = __instance.Input.TouchpadDown;

            if (__instance.MovementManager.ShouldFlushTouchpad(__instance))
            {
                typeof(FVRViveHand).GetMethod("FlushTouchpadData", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            }
            bool flag = false;
            bool flag2 = false;
            bool fullDescInput = false;
            if (__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) // Usually we would check if streamlined here, but in meatov, it will always be streamlined if index
            {
                flag = __instance.Input.BYButtonDown;

                // Want grab laser if the default BYButtonPressed (vanilla) (TODO: On main hand, right for now) OR if the description is touched
                flag2 = __instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange;

                // Check if we move touch from bottom (touch range) to top of touch pad this frame
                fullDescInput = touchpadTouched && (fullDescDirectionDown ? touchpadAxisY < 0 : touchpadAxisY > 0) && (__instance.IsThisTheRightHand ? (fullDescDirectionDown ? rightPreviousFrameTPAxisY >= 0 : rightPreviousFrameTPAxisY <= 0) : (fullDescDirectionDown ? leftPreviousFrameTPAxisY >= 0 : leftPreviousFrameTPAxisY <= 0));
                if (__instance.IsThisTheRightHand)
                {
                    rightPreviousFrameTPAxisY = touchpadAxisY;
                }
                else
                {
                    leftPreviousFrameTPAxisY = touchpadAxisY;
                }
            }
            else if(__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR)
            {
                flag = touchpadDown;

                // Here check if only touched and within center of touchpad for grab laser input
                flag2 = touchpadTouched && touchpadAxisMagnitude < 0.3f;
                //Mod.instance.LogInfo("Flag2: " + flag2 + " from touched: " + __instance.Input.TouchpadTouched + " and magnitude: " + __instance.Input.TouchpadAxes.magnitude);

                // Check if we started pressing the center of touchpad this frame
                fullDescInput = touchpadDown && touchpadAxisMagnitude < 0.3f;
            }

            if (flag2)
            {
                if (___m_state == FVRViveHand.HandState.GripInteracting)
                {
                    // Only display description if started touching within desc range, and also check if descriptions have been init yet
                    // Because this will also be checked in meatov menu, the patch will run, but they havent been init yet at that point
                    if ((__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange) && Mod.rightDescriptionManager != null)
                    {
                        EFM_Describable describable = __instance.CurrentInteractable.GetComponent<EFM_Describable>();
                        if (describable != null)
                        {
                            // Get the description currently on this hand
                            EFM_DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            EFM_Describable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                if (manager.descriptionPack.isPhysical)
                                {
                                    if (manager.descriptionPack.isCustom)
                                    {
                                        describableToUse = manager.descriptionPack.customItem;
                                    }
                                    else
                                    {
                                        describableToUse = manager.descriptionPack.vanillaItem;
                                    }
                                }
                                else
                                {
                                    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                }

                                // If not already displayed
                                if (!describable.Equals(describableToUse))
                                {
                                    // Update the display to the description of the new item we are pointing at
                                    manager.SetDescriptionPack(describable.GetDescriptionPack());
                                }
                            }
                            else
                            {
                                // Set description pack
                                manager.SetDescriptionPack(describable.GetDescriptionPack());
                            }

                            if (manager.descriptionPack.itemType == Mod.ItemType.LootContainer)
                            {
                                manager.gameObject.SetActive(false);
                            }
                            else
                            {
                                manager.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
            else if(!((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && fullDescInput)) // Dont want to disable if we arent in desc range but we have full desc input
            {
                if (Mod.rightDescriptionManager != null)
                {
                    // Get the description currently on this hand
                    EFM_DescriptionManager manager = null;
                    if (__instance.IsThisTheRightHand)
                    {
                        manager = Mod.rightDescriptionManager;
                    }
                    else
                    {
                        manager = Mod.leftDescriptionManager;
                    }

                    // Make sure it is not displayed
                    if (manager.gameObject.activeSelf)
                    {
                        manager.gameObject.SetActive(false);
                    }
                }
            }
            if (fullDescInput && Mod.rightDescriptionManager != null)
            {
                // Get the description currently on this hand
                EFM_DescriptionManager manager = null;
                if (__instance.IsThisTheRightHand)
                {
                    manager = Mod.rightDescriptionManager;
                }
                else
                {
                    manager = Mod.leftDescriptionManager;
                }

                // If displayed, open fully and replace this hand's with new description
                if (manager.gameObject.activeSelf)
                {
                    manager.OpenFull();

                    if (__instance.IsThisTheRightHand)
                    {
                        Mod.rightDescriptionUI = GameObject.Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.RightHand);
                        Mod.rightDescriptionManager = Mod.rightDescriptionUI.AddComponent<EFM_DescriptionManager>();
                        Mod.rightDescriptionManager.Init();
                    }
                    else
                    {
                        Mod.leftDescriptionUI = GameObject.Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.LeftHand);
                        Mod.leftDescriptionManager = Mod.leftDescriptionUI.AddComponent<EFM_DescriptionManager>();
                        Mod.leftDescriptionManager.Init();
                    }
                }
            }
            if (___m_state == FVRViveHand.HandState.Empty && __instance.CurrentHoveredQuickbeltSlot == null)
            {
                // Dont have the grab laser if we didnt start touching the touchpad within desc range
                if (((__instance.CMode == ControlMode.Index || __instance.CMode == ControlMode.Oculus) && flag2) ||
                    ((__instance.CMode == ControlMode.Vive || __instance.CMode == ControlMode.WMR) && flag2 && (__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange)))
                {
                    if (!__instance.GrabLaser.gameObject.activeSelf)
                    {
                        __instance.GrabLaser.gameObject.SetActive(true);
                    }
                    bool flag3 = false;
                    bool pointNonGrabbableDescribable = false;
                    FVRPhysicalObject fvrphysicalObject = null;
                    EFM_Describable nonGrabbableDescribable = null;
                    if (Physics.Raycast(__instance.Input.OneEuroPointingPos, __instance.Input.OneEuroPointRotation * Vector3.forward, out ___m_grabHit, 3f, __instance.GrabLaserMask, QueryTriggerInteraction.Collide))
                    {
                        if (___m_grabHit.collider.attachedRigidbody != null && ___m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>())
                        {
                            fvrphysicalObject = ___m_grabHit.collider.attachedRigidbody.gameObject.GetComponent<FVRPhysicalObject>();
                            if (fvrphysicalObject != null && !fvrphysicalObject.IsHeld && fvrphysicalObject.IsDistantGrabbable())
                            {
                                flag3 = true;
                            }
                        }
                        else if (___m_grabHit.collider.GetComponent<EFM_Describable>() != null)
                        {
                            nonGrabbableDescribable = ___m_grabHit.collider.GetComponent<EFM_Describable>();
                            pointNonGrabbableDescribable = true;
                        }
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, ___m_grabHit.distance) * d;
                    }
                    else
                    {
                        __instance.GrabLaser.localScale = new Vector3(0.004f, 0.004f, 3f) * d;
                    }
                    __instance.GrabLaser.position = __instance.Input.OneEuroPointingPos;
                    __instance.GrabLaser.rotation = __instance.Input.OneEuroPointRotation;
                    if (flag3)
                    {
                        // Display summary description of object if describable and if not already displayed
                        EFM_Describable describable = fvrphysicalObject.GetComponent<EFM_Describable>();
                        if(describable != null && Mod.rightDescriptionManager != null)
                        {
                            // Get the description currently on this hand
                            EFM_DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            EFM_Describable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                if (manager.descriptionPack.isPhysical)
                                {
                                    if (manager.descriptionPack.isCustom)
                                    {
                                        describableToUse = manager.descriptionPack.customItem;
                                    }
                                    else
                                    {
                                        describableToUse = manager.descriptionPack.vanillaItem;
                                    }
                                }
                                else
                                {
                                    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                }

                                // If not already displayed
                                if (!describable.Equals(describableToUse))
                                {
                                    // Update the display to the description of the new item we are pointing at
                                    manager.SetDescriptionPack(describable.GetDescriptionPack());
                                }
                            }
                            else
                            {
                                // Set description pack
                                manager.SetDescriptionPack(describable.GetDescriptionPack());
                            }

                            if (manager.descriptionPack.itemType == Mod.ItemType.LootContainer)
                            {
                                manager.gameObject.SetActive(false);
                            }
                            else
                            {
                                manager.gameObject.SetActive(true);
                            }
                        }

                        if (!__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(true);
                        }
                        if (__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(false);
                        }
                        if (__instance.Input.IsGrabDown && fvrphysicalObject != null)
                        {
                            __instance.RetrieveObject(fvrphysicalObject);
                            if (__instance.GrabLaser.gameObject.activeSelf)
                            {
                                __instance.GrabLaser.gameObject.SetActive(false);
                            }
                        }
                    }
                    else if (pointNonGrabbableDescribable)
                    {
                        // Display summary description of object if describable and if not already displayed
                        if (nonGrabbableDescribable != null && Mod.rightDescriptionManager != null)
                        {
                            // Get the description currently on this hand
                            EFM_DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }

                            // Get item and check if the one we are pointing at is already being described
                            EFM_Describable describableToUse = null;
                            if (manager.descriptionPack != null)
                            {
                                if (manager.descriptionPack.isPhysical)
                                {
                                    if (manager.descriptionPack.isCustom)
                                    {
                                        describableToUse = manager.descriptionPack.customItem;
                                    }
                                    else
                                    {
                                        describableToUse = manager.descriptionPack.vanillaItem;
                                    }
                                }
                                else
                                {
                                    describableToUse = manager.descriptionPack.nonPhysDescribable;
                                }

                                // If not already displayed
                                if (!nonGrabbableDescribable.Equals(describableToUse))
                                {
                                    // Update the display to the description of the new item we are pointing at
                                    manager.SetDescriptionPack(nonGrabbableDescribable.GetDescriptionPack());
                                }
                            }
                            else
                            {
                                // Set description pack
                                manager.SetDescriptionPack(nonGrabbableDescribable.GetDescriptionPack());
                            }

                            if (manager.descriptionPack.itemType == Mod.ItemType.LootContainer)
                            {
                                manager.gameObject.SetActive(false);
                            }
                            else
                            {
                                manager.gameObject.SetActive(true);
                            }
                        }

                        if (!__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(true);
                        }
                        if (__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(false);
                        }
                    }
                    else
                    {
                        if (Mod.rightDescriptionManager != null)
                        {
                            // Hide summary description of object
                            EFM_DescriptionManager manager = null;
                            if (__instance.IsThisTheRightHand)
                            {
                                manager = Mod.rightDescriptionManager;
                            }
                            else
                            {
                                manager = Mod.leftDescriptionManager;
                            }
                            if (manager.gameObject.activeSelf)
                            {
                                manager.gameObject.SetActive(false);
                            }
                        }

                        if (__instance.BlueLaser.activeSelf)
                        {
                            __instance.BlueLaser.SetActive(false);
                        }
                        if (!__instance.RedLaser.activeSelf)
                        {
                            __instance.RedLaser.SetActive(true);
                        }
                    }
                }
                else if (__instance.GrabLaser.gameObject.activeSelf)
                {
                    __instance.GrabLaser.gameObject.SetActive(false);
                }
            }
            else if (__instance.GrabLaser.gameObject.activeSelf)
            {
                __instance.GrabLaser.gameObject.SetActive(false);
            }
            if (__instance.Mode == FVRViveHand.HandMode.Neutral && ___m_state == FVRViveHand.HandState.Empty && flag)
            {
                bool isSpawnLockingEnabled = GM.CurrentSceneSettings.IsSpawnLockingEnabled;
                if (__instance.ClosestPossibleInteractable != null && __instance.ClosestPossibleInteractable is FVRPhysicalObject)
                {
                    FVRPhysicalObject fvrphysicalObject2 = __instance.ClosestPossibleInteractable as FVRPhysicalObject;
                    if (((fvrphysicalObject2.SpawnLockable && isSpawnLockingEnabled) || fvrphysicalObject2.Harnessable) && fvrphysicalObject2.QuickbeltSlot != null)
                    {
                        fvrphysicalObject2.ToggleQuickbeltState();
                    }
                }
                else if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.HeldObject != null)
                {
                    FVRPhysicalObject fvrphysicalObject3 = __instance.CurrentHoveredQuickbeltSlot.HeldObject as FVRPhysicalObject;
                    if ((fvrphysicalObject3.SpawnLockable && isSpawnLockingEnabled) || fvrphysicalObject3.Harnessable)
                    {
                        fvrphysicalObject3.ToggleQuickbeltState();
                    }
                }
            }
            typeof(FVRViveHand).GetMethod("UpdateGrabityDisplay", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            if (__instance.Mode == FVRViveHand.HandMode.Neutral)
            {
                if (___m_state == FVRViveHand.HandState.Empty)
                {
                    bool flag4 = false;
                    if (__instance.Input.IsGrabDown)
                    {
                        if (__instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.CurObject != null)
                        {
                            __instance.CurrentInteractable = __instance.CurrentHoveredQuickbeltSlot.CurObject;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            flag4 = true;
                        }
                        else if (__instance.CurrentHoveredQuickbeltSlot != null && 
                                 __instance.CurrentHoveredQuickbeltSlot is EFM_ShoulderStorage &&
                                 !(__instance.CurrentHoveredQuickbeltSlot as EFM_ShoulderStorage).right &&
                                 Mod.equipmentSlots[0].CurObject != null)
                        {
                            // If we are hovering over left shoulder slot and backpack slot is not empty we want to grab backpack
                            __instance.CurrentInteractable = Mod.equipmentSlots[0].CurObject;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            Mod.leftShoulderObject = null;
                            flag4 = true;
                        }
                        else if (__instance.ClosestPossibleInteractable != null && !__instance.ClosestPossibleInteractable.IsSimpleInteract)
                        {
                            __instance.CurrentInteractable = __instance.ClosestPossibleInteractable;
                            ___m_state = FVRViveHand.HandState.GripInteracting;
                            __instance.CurrentInteractable.BeginInteraction(__instance);
                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                            flag4 = true;
                        }
                    }
                    bool flag5 = false;
                    if (!flag4 && __instance.Input.TriggerDown)
                    {
                        if (!(__instance.CurrentHoveredQuickbeltSlot != null) || !(__instance.CurrentHoveredQuickbeltSlot.CurObject != null))
                        {
                            if (__instance.ClosestPossibleInteractable != null && __instance.ClosestPossibleInteractable.IsSimpleInteract)
                            {
                                __instance.ClosestPossibleInteractable.SimpleInteraction(__instance);
                                flag5 = true;
                            }
                        }
                    }
                    bool flag6 = false;
                    if (!flag4 && !flag5 && __instance.Input.IsGrabDown)
                    {
                        ___m_rawGrabCols = Physics.OverlapSphere(__instance.transform.position, 0.01f, __instance.LM_RawGrab, QueryTriggerInteraction.Ignore);
                        if (___m_rawGrabCols.Length > 0)
                        {
                            for (int i = 0; i < ___m_rawGrabCols.Length; i++)
                            {
                                if (!(___m_rawGrabCols[i].attachedRigidbody == null))
                                {
                                    if (___m_rawGrabCols[i].attachedRigidbody.gameObject.CompareTag("RawGrab"))
                                    {
                                        FVRInteractiveObject component2 = ___m_rawGrabCols[i].attachedRigidbody.gameObject.GetComponent<FVRInteractiveObject>();
                                        if (component2 != null && component2.IsInteractable())
                                        {
                                            flag6 = true;
                                            __instance.CurrentInteractable = component2;
                                            ___m_state = FVRViveHand.HandState.GripInteracting;
                                            __instance.CurrentInteractable.BeginInteraction(__instance);
                                            __instance.Buzz(__instance.Buzzer.Buzz_BeginInteraction);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    if (GM.Options.ControlOptions.WIPGrabbityState == ControlOptions.WIPGrabbity.Enabled && !flag4 && !flag5 && !flag6)
                    {
                        if (___m_selectedObj == null)
                        {
                            typeof(FVRViveHand).GetMethod("CastToFindHover", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                        }
                        else
                        {
                            typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                        }
                        bool flag7;
                        bool flag8;
                        if (GM.Options.ControlOptions.WIPGrabbityButtonState == ControlOptions.WIPGrabbityButton.Grab)
                        {
                            flag7 = __instance.Input.GripDown;
                            flag8 = __instance.Input.GripUp;
                        }
                        else
                        {
                            flag7 = __instance.Input.TriggerDown;
                            flag8 = __instance.Input.TriggerUp;
                        }
                        if (flag7 && ___m_grabityHoveredObject != null && ___m_selectedObj == null)
                        {
                            typeof(FVRViveHand).GetMethod("CastToGrab", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
                        }
                        if (flag8 && !___m_isObjectInTransit)
                        {
                            ___m_selectedObj = null;
                        }
                        if (___m_selectedObj != null && !___m_isObjectInTransit)
                        {
                            float num = 3.5f;
                            if (Mathf.Abs(__instance.Input.VelAngularLocal.x) > num || Mathf.Abs(__instance.Input.VelAngularLocal.y) > num)
                            {
                                typeof(FVRViveHand).GetMethod("BeginFlick", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_selectedObj });
                            }
                        }
                    }
                    else
                    {
                        typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                    }
                    if (GM.Options.ControlOptions.WIPGrabbityState == ControlOptions.WIPGrabbity.Enabled && !flag4 && !flag5 && __instance.Input.IsGrabDown && ___m_isObjectInTransit && ___m_selectedObj != null)
                    {
                        float num2 = Vector3.Distance(__instance.transform.position, ___m_selectedObj.transform.position);
                        if (num2 < 0.5f)
                        {
                            if (___m_selectedObj.UseGripRotInterp)
                            {
                                __instance.CurrentInteractable = ___m_selectedObj;
                                __instance.CurrentInteractable.BeginInteraction(__instance);
                                ___m_state = FVRViveHand.HandState.GripInteracting;
                            }
                            else
                            {
                                __instance.RetrieveObject(___m_selectedObj);
                            }
                            ___m_selectedObj = null;
                            ___m_isObjectInTransit = false;
                            typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                        }
                    }
                }
                else if (___m_state == FVRViveHand.HandState.GripInteracting)
                {
                    typeof(FVRViveHand).GetMethod("SetGrabbityHovered", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { null });
                    bool flag9 = false;
                    if (__instance.CurrentInteractable != null)
                    {
                        ControlMode controlMode = __instance.CMode;
                        if (GM.Options.ControlOptions.GripButtonToHoldOverride == ControlOptions.GripButtonToHoldOverrideMode.OculusOverride)
                        {
                            controlMode = ControlMode.Oculus;
                        }
                        else if (GM.Options.ControlOptions.GripButtonToHoldOverride == ControlOptions.GripButtonToHoldOverrideMode.ViveOverride)
                        {
                            controlMode = ControlMode.Vive;
                        }
                        if (controlMode == ControlMode.Vive || controlMode == ControlMode.WMR)
                        {
                            if (__instance.CurrentInteractable.ControlType == FVRInteractionControlType.GrabHold)
                            {
                                if (__instance.Input.TriggerUp)
                                {
                                    flag9 = true;
                                }
                            }
                            else if (__instance.CurrentInteractable.ControlType == FVRInteractionControlType.GrabToggle)
                            {
                                ControlOptions.ButtonControlStyle gripButtonDropStyle = GM.Options.ControlOptions.GripButtonDropStyle;
                                if (gripButtonDropStyle != ControlOptions.ButtonControlStyle.Instant)
                                {
                                    if (gripButtonDropStyle != ControlOptions.ButtonControlStyle.Hold1Second)
                                    {
                                        if (gripButtonDropStyle == ControlOptions.ButtonControlStyle.DoubleClick)
                                        {
                                            if (!__instance.Input.TriggerPressed && __instance.Input.GripDown && ___m_timeSinceLastGripButtonDown > 0.05f && ___m_timeSinceLastGripButtonDown < 0.4f)
                                            {
                                                flag9 = true;
                                            }
                                        }
                                    }
                                    else if (!__instance.Input.TriggerPressed && ___m_timeGripButtonHasBeenHeld > 1f)
                                    {
                                        flag9 = true;
                                    }
                                }
                                else if (!__instance.Input.TriggerPressed && __instance.Input.GripDown)
                                {
                                    flag9 = true;
                                }
                            }
                        }
                        else if (__instance.Input.IsGrabUp)
                        {
                            flag9 = true;
                        }
                        if (flag9)
                        {
                            if (__instance.CurrentInteractable is FVRPhysicalObject && ((FVRPhysicalObject)__instance.CurrentInteractable).QuickbeltSlot == null && !((FVRPhysicalObject)__instance.CurrentInteractable).IsPivotLocked && __instance.CurrentHoveredQuickbeltSlot != null && __instance.CurrentHoveredQuickbeltSlot.HeldObject == null && ((FVRPhysicalObject)__instance.CurrentInteractable).QBSlotType == __instance.CurrentHoveredQuickbeltSlot.Type && __instance.CurrentHoveredQuickbeltSlot.SizeLimit >= ((FVRPhysicalObject)__instance.CurrentInteractable).Size)
                            {
                                // Note: This will call set quick belt slot twice, this is by vanilla design and is not a bug
                                ((FVRPhysicalObject)__instance.CurrentInteractable).EndInteractionIntoInventorySlot(__instance, __instance.CurrentHoveredQuickbeltSlot);
                            }
                            else
                            {
                                __instance.CurrentInteractable.EndInteraction(__instance);
                            }
                            __instance.CurrentInteractable = null;
                            ___m_state = FVRViveHand.HandState.Empty;
                        }
                        else
                        {
                            __instance.CurrentInteractable.UpdateInteraction(__instance);
                        }
                    }
                    else
                    {
                        ___m_state = FVRViveHand.HandState.Empty;
                    }
                }
            }
            if (__instance.Input.GripPressed)
            {
                ___m_timeSinceLastGripButtonDown = 0f;
                ___m_timeGripButtonHasBeenHeld += Time.deltaTime;
            }
            else
            {
                ___m_timeGripButtonHasBeenHeld = 0f;
            }
            ___m_canMadeGrabReleaseSoundThisFrame = true;

            return false;
        }
    }
    
    // Patches FVRFireArmMagazine to get the created round item when ejected from the magazine so we can set its location index and update the lists accordingly
    class MagazineUpdateInteractionPatch
    {
        static GameObject latestEjectedRound;
        static int latestEjectedRoundLocation; // IGNORE WARNING, Will be written by transpiler

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Equals("UnityEngine.GameObject RemoveRound(Boolean)") &&
                    instructionList[i+1].opcode == OpCodes.Stloc_S)
                {
                    if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (13)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in hand
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if(instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (18)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in slot, could be in raid or base so can just take the one in mod
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if(instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (23)"))
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in slot, could be in raid or base so can just take the one in mod
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                }
            }
            return instructionList;
        }

        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (latestEjectedRound != null)
            {
                EFM_VanillaItemDescriptor vanillaItemDescriptor = latestEjectedRound.GetComponent<EFM_VanillaItemDescriptor>();

                // Set a default location of 0, we assume the round was ejected from a mag in one of the hands, so should begin with a loc index of 0
                BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor, false);

                if (latestEjectedRoundLocation == 0)
                {
                    BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    // Now on player
                    Mod.AddToPlayerInventory(vanillaItemDescriptor.transform, true);
                }
                else // Could be in raid or base
                {
                    BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, vanillaItemDescriptor);
                    GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                    if (Mod.currentLocationIndex == 1)
                    {
                        // Now in hideout
                        Mod.currentBaseManager.AddToBaseInventory(vanillaItemDescriptor.transform, true);

                        latestEjectedRound.transform.parent = sceneRoot.transform.GetChild(2);
                    }
                    else if (Mod.currentLocationIndex == 2)
                    {
                        latestEjectedRound.transform.parent = sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2);
                    }
                }

                latestEjectedRound = null;
            }
        }
    }
    
    // Patches FVRFireArmClip to get the created round item when ejected from the magazine so we can set its location index and update the lists accordingly
    class ClipUpdateInteractionPatch
    {
        static GameObject latestEjectedRound;
        static int latestEjectedRoundLocation; // IGNORE WARNING, Will be written by transpiler

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            List<CodeInstruction> instructionList = new List<CodeInstruction>(instructions);
            List<CodeInstruction> toInsert = new List<CodeInstruction>();
            toInsert.Add(new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));
            toInsert.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRound")));

            for (int i = 0; i < instructionList.Count; ++i)
            {
                CodeInstruction instruction = instructionList[i];
                if (instruction.opcode == OpCodes.Call && instruction.operand.ToString().Equals("UnityEngine.GameObject RemoveRound(Boolean)"))
                {
                    if(instructionList[i + 1].opcode == OpCodes.Stloc_1)
                    {
                        instructionList.InsertRange(i + 1, toInsert);

                        // Now in hand
                        instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_0));
                        instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                    }
                    else if (instructionList[i + 1].opcode == OpCodes.Stloc_S)
                    {
                        if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (6)"))
                        {
                            instructionList.InsertRange(i + 1, toInsert);

                            // Now in slot, could be in raid or base so can just take the one in mod
                            instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                            instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                        }
                        else if (instructionList[i + 1].operand.ToString().Equals("UnityEngine.GameObject (11)"))
                        {
                            instructionList.InsertRange(i + 1, toInsert);

                            // Now in slot, could be in raid or base so can just take the one in mod
                            instructionList.Insert(i + 5, new CodeInstruction(OpCodes.Ldc_I4_1));
                            instructionList.Insert(i + 6, new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(MagazineUpdateInteractionPatch), "latestEjectedRoundLocation")));
                        }
                    }
                }
            }
            return instructionList;
        }

        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (latestEjectedRound != null)
            {
                EFM_VanillaItemDescriptor vanillaItemDescriptor = latestEjectedRound.GetComponent<EFM_VanillaItemDescriptor>();

                // Set a default location of 0, we assume the round was ejected from a clip in one of the hands, so should begin with a loc index of 0
                BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor, false);

                if (latestEjectedRoundLocation == 0) 
                {
                    BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    // Now on player
                    Mod.AddToPlayerInventory(vanillaItemDescriptor.transform, true);
                }
                else // Could be in raid or base
                {
                    BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, vanillaItemDescriptor);

                    GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                    if (Mod.currentLocationIndex == 1)
                    {
                        // Now in hideout
                        Mod.currentBaseManager.AddToBaseInventory(vanillaItemDescriptor.transform, true);

                        latestEjectedRound.transform.parent = sceneRoot.transform.GetChild(2);
                    }
                    else if (Mod.currentLocationIndex == 2)
                    {
                        latestEjectedRound.transform.parent = sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2);
                    }
                }

                latestEjectedRound = null;
            }
        }
    }
    
    // Patches FVRMovementManager.Jump to make it use stamina or to prevent it altogether if not enough stamina
    // This completely replaces the original
    class MovementManagerJumpPatch
    {
        static bool Prefix(ref bool ___m_armSwingerGrounded, ref bool ___m_twoAxisGrounded, ref Vector3 ___m_armSwingerVelocity,
                           ref Vector3 ___m_twoAxisVelocity, ref FVRMovementManager __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            // Return if not enough stamina
            if (Mod.stamina < Mod.jumpStaminaDrain)
            {
                return false;
            }

            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger && !___m_armSwingerGrounded)
            {
                return false;
            }
            if ((__instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis || __instance.Mode == FVRMovementManager.MovementMode.TwinStick) && !___m_twoAxisGrounded)
            {
                return false;
            }
            __instance.DelayGround(0.1f);
            float num = 0f;
            switch (GM.Options.SimulationOptions.PlayerGravityMode)
            {
                case SimulationOptions.GravityMode.Realistic:
                    num = 7.1f;
                    break;
                case SimulationOptions.GravityMode.Playful:
                    num = 5f;
                    break;
                case SimulationOptions.GravityMode.OnTheMoon:
                    num = 3f;
                    break;
                case SimulationOptions.GravityMode.None:
                    num = 0.001f;
                    break;
            }
            num *= 0.65f;
            num += num * (0.004f * (Mod.skills[1].currentProgress / 100));
            if (__instance.Mode == FVRMovementManager.MovementMode.Armswinger)
            {
                __instance.DelayGround(0.25f);
                ___m_armSwingerVelocity.y = Mathf.Clamp(___m_armSwingerVelocity.y, 0f, ___m_armSwingerVelocity.y);
                ___m_armSwingerVelocity.y = num;
                ___m_armSwingerGrounded = false;
            }
            else if (__instance.Mode == FVRMovementManager.MovementMode.SingleTwoAxis || __instance.Mode == FVRMovementManager.MovementMode.TwinStick)
            {
                __instance.DelayGround(0.25f);
                ___m_twoAxisVelocity.y = Mathf.Clamp(___m_twoAxisVelocity.y, 0f, ___m_twoAxisVelocity.y);
                ___m_twoAxisVelocity.y = num;
                ___m_twoAxisGrounded = false;
            }

            // Use stamina
            Mod.stamina = Mathf.Max(Mod.stamina - (Mod.jumpStaminaDrain - Mod.jumpStaminaDrain * (0.006f * (Mod.skills[0].progress / 100))), 0);
            Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

            Mod.AddSkillExp(UnityEngine.Random.Range(EFM_Skill.pushUpMin, EFM_Skill.pushUpMax), 1);

            // Reset stamina timer
            Mod.staminaTimer = 2;

            return false;
        }
    }

    // Patches FVRMovementManager.HandUpdateTwinstick to prevent sprinting in case of lack of stamina
    class MovementManagerUpdatePatch
    {
        private static bool wasGrounded = true;
        private static Vector3 previousVelocity;
        public static float damagePerMeter = 9;
        public static float safeHeight = 3;

        static bool Prefix(FVRViveHand hand, ref bool ___m_isRightHandActive, ref bool ___m_isLeftHandActive, ref GameObject ___m_twinStickArrowsRight,
                           ref bool ___m_isTwinStickSmoothTurningCounterClockwise, ref bool ___m_isTwinStickSmoothTurningClockwise, ref GameObject ___m_twinStickArrowsLeft,
                           ref float ___m_timeSinceSprintDownClick, ref float ___m_timeSinceSnapTurn, ref bool ___m_sprintingEngaged, ref bool ___m_twoAxisGrounded,
                           ref Vector3 ___m_twoAxisVelocity, ref FVRMovementManager __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            bool flag = hand.IsThisTheRightHand;
            if (GM.Options.MovementOptions.TwinStickLeftRightState == MovementOptions.TwinStickLeftRightSetup.RightStickMove)
            {
                flag = !flag;
            }
            if (!hand.IsInStreamlinedMode && (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus))
            {
                // In meatov, the secondary hand should always be in movement mode if constols not streamlined
                if (!___m_isLeftHandActive)
                {
                    ___m_isLeftHandActive = true;
                }

                if (hand.Input.BYButtonDown)
                {
                    if (flag)
                    {
                        ___m_isRightHandActive = !___m_isRightHandActive;
                    }
                    //if (!flag)
                    //{
                    //    ___m_isLeftHandActive = !___m_isLeftHandActive;
                    //}
                }
            }
            else
            {
                ___m_isLeftHandActive = true;
                ___m_isRightHandActive = true;
            }
            if (flag && !___m_isRightHandActive)
            {
                if (___m_twinStickArrowsRight.activeSelf)
                {
                    ___m_twinStickArrowsRight.SetActive(false);
                }
                ___m_isTwinStickSmoothTurningCounterClockwise = false;
                ___m_isTwinStickSmoothTurningClockwise = false;
                return false;
            }
            if (!flag && !___m_isLeftHandActive)
            {
                if (___m_twinStickArrowsLeft.activeSelf)
                {
                    ___m_twinStickArrowsLeft.SetActive(false);
                }
                return false;
            }
            if (!hand.IsInStreamlinedMode && (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus))
            {
                if (flag)
                {
                    if (!___m_twinStickArrowsRight.activeSelf)
                    {
                        ___m_twinStickArrowsRight.SetActive(true);
                    }
                    if (___m_twinStickArrowsRight.transform.parent != hand.TouchpadArrowTarget)
                    {
                        ___m_twinStickArrowsRight.transform.SetParent(hand.TouchpadArrowTarget);
                        ___m_twinStickArrowsRight.transform.localPosition = Vector3.zero;
                        ___m_twinStickArrowsRight.transform.localRotation = Quaternion.identity;
                    }
                }
                else
                {
                    if (!___m_twinStickArrowsLeft.activeSelf)
                    {
                        ___m_twinStickArrowsLeft.SetActive(true);
                    }
                    if (___m_twinStickArrowsLeft.transform.parent != hand.TouchpadArrowTarget)
                    {
                        ___m_twinStickArrowsLeft.transform.SetParent(hand.TouchpadArrowTarget);
                        ___m_twinStickArrowsLeft.transform.localPosition = Vector3.zero;
                        ___m_twinStickArrowsLeft.transform.localRotation = Quaternion.identity;
                    }
                }
            }
            if (___m_timeSinceSprintDownClick < 2f)
            {
                ___m_timeSinceSprintDownClick += Time.deltaTime;
            }
            if (___m_timeSinceSnapTurn < 2f)
            {
                ___m_timeSinceSnapTurn += Time.deltaTime;
            }
            bool flag3;
            bool flag4;
            Vector2 vector;
            bool flag5;
            bool flag6;
            if (hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.Oculus)
            {
                bool flag2 = hand.Input.TouchpadUp;
                flag3 = hand.Input.TouchpadDown;
                flag4 = hand.Input.TouchpadPressed;
                vector = hand.Input.TouchpadAxes;
                flag5 = hand.Input.TouchpadNorthDown;
                flag6 = hand.Input.TouchpadNorthPressed;
            }
            else
            {
                bool flag2 = hand.Input.Secondary2AxisInputUp;
                flag3 = hand.Input.Secondary2AxisInputDown;
                flag4 = hand.Input.Secondary2AxisInputPressed;
                vector = hand.Input.Secondary2AxisInputAxes;
                flag5 = hand.Input.Secondary2AxisNorthDown;
                flag6 = hand.Input.Secondary2AxisNorthPressed;
            }
            if (flag)
            {
                ___m_isTwinStickSmoothTurningCounterClockwise = false;
                ___m_isTwinStickSmoothTurningClockwise = false;
                if (GM.Options.MovementOptions.TwinStickSnapturnState == MovementOptions.TwinStickSnapturnMode.Enabled)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadWestDown)
                        {
                            __instance.TurnCounterClockWise();
                        }
                        else if (hand.Input.TouchpadEastDown)
                        {
                            __instance.TurnClockWise();
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadDown)
                            {
                                if (hand.Input.TouchpadWestPressed)
                                {
                                    __instance.TurnCounterClockWise();
                                }
                                else if (hand.Input.TouchpadEastPressed)
                                {
                                    __instance.TurnClockWise();
                                }
                            }
                        }
                        else if (hand.Input.TouchpadWestDown)
                        {
                            __instance.TurnCounterClockWise();
                        }
                        else if (hand.Input.TouchpadEastDown)
                        {
                            __instance.TurnClockWise();
                        }
                    }
                    else if (hand.Input.Secondary2AxisWestDown)
                    {
                        __instance.TurnCounterClockWise();
                    }
                    else if (hand.Input.Secondary2AxisEastDown)
                    {
                        __instance.TurnClockWise();
                    }
                }
                else if (GM.Options.MovementOptions.TwinStickSnapturnState == MovementOptions.TwinStickSnapturnMode.Smooth)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadWestPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                        else if (hand.Input.TouchpadEastPressed)
                        {
                            ___m_isTwinStickSmoothTurningClockwise = true;
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadPressed)
                            {
                                if (hand.Input.TouchpadWestPressed)
                                {
                                    ___m_isTwinStickSmoothTurningCounterClockwise = true;
                                }
                                else if (hand.Input.TouchpadEastPressed)
                                {
                                    ___m_isTwinStickSmoothTurningClockwise = true;
                                }
                            }
                        }
                        else if (hand.Input.TouchpadWestPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                        else if (hand.Input.TouchpadEastPressed)
                        {
                            ___m_isTwinStickSmoothTurningClockwise = true;
                        }
                    }
                    else if (hand.Input.Secondary2AxisWestPressed)
                    {
                        ___m_isTwinStickSmoothTurningCounterClockwise = true;
                    }
                    else if (hand.Input.Secondary2AxisEastPressed)
                    {
                        ___m_isTwinStickSmoothTurningClockwise = true;
                    }
                }
                MethodInfo jumpMethod = typeof(FVRMovementManager).GetMethod("Jump", BindingFlags.NonPublic | BindingFlags.Instance);
                if (GM.Options.MovementOptions.TwinStickJumpState == MovementOptions.TwinStickJumpMode.Enabled)
                {
                    if (hand.CMode == ControlMode.Oculus)
                    {
                        if (hand.Input.TouchpadSouthDown)
                        {
                            jumpMethod.Invoke(__instance, null);
                        }
                    }
                    else if (hand.CMode == ControlMode.Vive)
                    {
                        if (GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                        {
                            if (hand.Input.TouchpadDown && hand.Input.TouchpadSouthPressed)
                            {
                                jumpMethod.Invoke(__instance, null);
                            }
                        }
                        else if (hand.Input.TouchpadSouthDown)
                        {
                            jumpMethod.Invoke(__instance, null);
                        }
                    }
                    else if (hand.Input.Secondary2AxisSouthDown)
                    {
                        jumpMethod.Invoke(__instance, null);
                    }
                }
                if (GM.Options.MovementOptions.TwinStickSprintState == MovementOptions.TwinStickSprintMode.RightStickForward)
                {
                    if (GM.Options.MovementOptions.TwinStickSprintToggleState == MovementOptions.TwinStickSprintToggleMode.Disabled)
                    {
                        // Also check stamina for sprinting
                        if (flag6 && Mod.stamina > 0)
                        {
                            ___m_sprintingEngaged = true;
                        }
                        else
                        {
                            ___m_sprintingEngaged = false;
                        }
                    }
                    else if (flag5)
                    {
                        ___m_sprintingEngaged = !___m_sprintingEngaged;
                    }
                }
            }
            else
            {
                if (GM.Options.MovementOptions.TwinStickSprintState == MovementOptions.TwinStickSprintMode.LeftStickClick)
                {
                    if (GM.Options.MovementOptions.TwinStickSprintToggleState == MovementOptions.TwinStickSprintToggleMode.Disabled)
                    {
                        // Also check stamina for sprinting
                        if (flag4 && Mod.stamina > 0)
                        {
                            ___m_sprintingEngaged = true;
                        }
                        else
                        {
                            ___m_sprintingEngaged = false;
                        }
                    }
                    else if (flag3)
                    {
                        ___m_sprintingEngaged = !___m_sprintingEngaged;
                    }
                }
                Vector3 a = Vector3.zero;
                float y = vector.y;
                float x = vector.x;
                switch (GM.Options.MovementOptions.Touchpad_MovementMode)
                {
                    case FVRMovementManager.TwoAxisMovementMode.Standard:
                        a = y * hand.PointingTransform.forward + x * hand.PointingTransform.right * 0.75f;
                        a.y = 0f;
                        break;
                    case FVRMovementManager.TwoAxisMovementMode.Onward:
                        a = y * hand.Input.Forward + x * hand.Input.Right * 0.75f;
                        break;
                    case FVRMovementManager.TwoAxisMovementMode.LeveledHand:
                        {
                            Vector3 forward = hand.Input.Forward;
                            forward.y = 0f;
                            forward.Normalize();
                            Vector3 right = hand.Input.Right;
                            right.y = 0f;
                            right.Normalize();
                            a = y * forward + x * right * 0.75f;
                            break;
                        }
                    case FVRMovementManager.TwoAxisMovementMode.LeveledHead:
                        {
                            Vector3 forward2 = GM.CurrentPlayerBody.Head.forward;
                            forward2.y = 0f;
                            forward2.Normalize();
                            Vector3 right2 = GM.CurrentPlayerBody.Head.right;
                            right2.y = 0f;
                            right2.Normalize();
                            a = y * forward2 + x * right2 * 0.75f;
                            break;
                        }
                }
                Vector3 normalized = a.normalized;
                a *= GM.Options.MovementOptions.TPLocoSpeeds[GM.Options.MovementOptions.TPLocoSpeedIndex];
                if (hand.CMode == ControlMode.Vive && GM.Options.MovementOptions.Touchpad_Confirm == FVRMovementManager.TwoAxisMovementConfirm.OnClick)
                {
                    if (!flag4)
                    {
                        a = Vector3.zero;
                    }
                    else if (___m_sprintingEngaged && GM.Options.MovementOptions.TPLocoSpeedIndex < 5)
                    {
                        a += normalized * 2f;
                    }
                }
                else if (___m_sprintingEngaged && GM.Options.MovementOptions.TPLocoSpeedIndex < 5)
                {
                    a += normalized * 2f;
                }
                if (Mod.skills != null)
                {
                    a += a * (0.004f * (Mod.skills[1].currentProgress / 100));
                }
                if (___m_twoAxisGrounded)
                {
                    ___m_twoAxisVelocity.x = a.x;
                    ___m_twoAxisVelocity.z = a.z;
                    if (GM.CurrentSceneSettings.UsesMaxSpeedClamp)
                    {
                        Vector2 vector2 = new Vector2(___m_twoAxisVelocity.x, ___m_twoAxisVelocity.z);
                        if (vector2.magnitude > GM.CurrentSceneSettings.MaxSpeedClamp)
                        {
                            vector2 = vector2.normalized * GM.CurrentSceneSettings.MaxSpeedClamp;
                            ___m_twoAxisVelocity.x = vector2.x;
                            ___m_twoAxisVelocity.z = vector2.y;
                        }
                    }
                }
                else if (GM.CurrentSceneSettings.DoesAllowAirControl)
                {
                    Vector3 vector3 = new Vector3(___m_twoAxisVelocity.x, 0f, ___m_twoAxisVelocity.z);
                    ___m_twoAxisVelocity.x = ___m_twoAxisVelocity.x + a.x * Time.deltaTime;
                    ___m_twoAxisVelocity.z = ___m_twoAxisVelocity.z + a.z * Time.deltaTime;
                    Vector3 vector4 = new Vector3(___m_twoAxisVelocity.x, 0f, ___m_twoAxisVelocity.z);
                    float maxLength = Mathf.Max(1f, vector3.magnitude);
                    vector4 = Vector3.ClampMagnitude(vector4, maxLength);
                    ___m_twoAxisVelocity.x = vector4.x;
                    ___m_twoAxisVelocity.z = vector4.z;
                }
                else
                {
                    Vector3 vector5 = new Vector3(___m_twoAxisVelocity.x, 0f, ___m_twoAxisVelocity.z);
                    ___m_twoAxisVelocity.x = ___m_twoAxisVelocity.x + a.x * Time.deltaTime * 0.3f;
                    ___m_twoAxisVelocity.z = ___m_twoAxisVelocity.z + a.z * Time.deltaTime * 0.3f;
                    Vector3 vector6 = new Vector3(___m_twoAxisVelocity.x, 0f, ___m_twoAxisVelocity.z);
                    float maxLength2 = Mathf.Max(1f, vector5.magnitude);
                    vector6 = Vector3.ClampMagnitude(vector6, maxLength2);
                    ___m_twoAxisVelocity.x = vector6.x;
                    ___m_twoAxisVelocity.z = vector6.z;
                }
                if (flag3)
                {
                    ___m_timeSinceSprintDownClick = 0f;
                }
            }

            // Update fall damage depending on grounded and previous velocity
            if (Mod.currentLocationIndex == 2)
            {
                UpdateFallDamage(___m_twoAxisGrounded);
            }

            if (Mod.skills != null)
            {
                UpdateMovementAction(___m_twoAxisVelocity, ___m_sprintingEngaged);
            }

            wasGrounded = ___m_twoAxisGrounded;
            previousVelocity = ___m_twoAxisVelocity;

            return false;
        }

        private static void UpdateMovementAction(Vector3 velocity, bool sprinting)
        {
            Vector3 sideVelocity = velocity;
            sideVelocity.y = 0;

            if (sprinting)
            {
                Mod.AddSkillExp(sideVelocity.magnitude * EFM_Skill.sprintAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(sideVelocity.magnitude * UnityEngine.Random.Range(EFM_Skill.sprintActionMin, EFM_Skill.sprintActionMax), 1);
                }
            }
            else
            {
                Mod.AddSkillExp(sideVelocity.magnitude * EFM_Skill.movementAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(sideVelocity.magnitude * UnityEngine.Random.Range(EFM_Skill.movementActionMin, EFM_Skill.movementActionMax), 1);
                }
            }
        }

        private static void UpdateFallDamage(bool grounded)
        {
            if (grounded && !wasGrounded)
            {
                // Considering realistic 1g of acceleration, t = (Vf-Vi)/a, and s = Vi * t + 0.5 * a * t ^ 2, s being distance fallen
                float t = previousVelocity.y / -9.806f; // Note that here, velocity and a are negative, giving a positive time
                float s = 4.903f /* 0.5f * 9.806f */ * t * t; // Here a is positive to have a positive distance fallen
                if (s > safeHeight)
                {
                    float damage = s * damagePerMeter;
                    float distribution = UnityEngine.Random.value;
                    if (UnityEngine.Random.value < 0.125 * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        EFM_Effect fractureEffect = new EFM_Effect();
                        fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                        fractureEffect.partIndex = 5;
                        EFM_Effect.effects.Add(fractureEffect);
                        // TODO: Play fracture sound
                    }
                    if (UnityEngine.Random.value < 0.125 * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        EFM_Effect fractureEffect = new EFM_Effect();
                        fractureEffect.effectType = EFM_Effect.EffectType.Fracture;
                        fractureEffect.partIndex = 6;
                        EFM_Effect.effects.Add(fractureEffect);
                        // TODO: Play fracture sound
                    }

                    DamagePatch.RegisterPlayerHit(5, distribution * damage, true);
                    DamagePatch.RegisterPlayerHit(6, (1 - distribution) * damage, true);
                }
            }
        }
    }

    // Patches FVRFireArmChamber.SetRound to keep track of weight in chamber
    class ChamberSetRoundPatch
    {
        static void Prefix(ref FVRFireArmRound round, ref FVRFireArmChamber __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.IsFull && round == null)
            {
                EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
                VID.currentWeight -= 15;
                if(VID.locationIndex == 0)
                {
                    Mod.weight -= 15;
                }
            }
            else
            {
                EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
                VID.currentWeight += 15;
                if (VID.locationIndex == 0)
                {
                    Mod.weight += 15;
                }
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound() to keep track of weight of ammo in mag
    class MagRemoveRoundPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if(__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 15 * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else if(CIW != null)
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.ForceBreakInteraction();
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
            }
            else
            {
                Mod.instance.LogError("MagRemoveRoundPatch postfix: Mag " + __instance.name + " has no VID nor CIW");
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound(bool) to keep track of weight of ammo in mag
    // TODO: See if this could be used to do what we do in MagazineUpdateInteractionPatch instead
    class MagRemoveRoundBoolPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 15 * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else if(CIW != null)
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.ForceBreakInteraction();
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
            }
            else
            {
                Mod.instance.LogError("MagRemoveRoundBoolPatch postfix: Mag " + __instance.name + " has no VID nor CIW");
            }
        }
    }

    // Patches FVRFireArmMagazine.RemoveRound(int) to keep track of weight of ammo in mag
    class MagRemoveRoundIntPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 15 * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else if(CIW != null)
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.ForceBreakInteraction();
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 15 * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
            }
            else
            {
                Mod.instance.LogError("MagRemoveRoundIntPatch postfix: Mag " + __instance.name + " has no VID nor CIW");
            }
        }
    }

    // Patches FVRFireArmClip.RemoveRound() to keep track of weight of ammo in clip
    class ClipRemoveRoundPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
            //TODO: Set weight of firearm this is attached to if it is
        }
    }

    // Patches FVRFireArmClip.RemoveRound(bool) to keep track of weight of ammo in clip
    // TODO: See if this could be used to do what we do in ClipUpdateInteractionPatch instead
    class ClipRemoveRoundBoolPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
            //TODO: Set weight of firearm this is attached to if it is
        }
    }

    // Patches FVRFireArmClip.RemoveRoundReturnClass to keep track of weight of ammo in clip
    class ClipRemoveRoundClassPatch
    {
        static int preNumRounds = 0;

        static void Prefix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            preNumRounds = ___m_numRounds;
        }

        static void Postfix(int ___m_numRounds, ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            int postNumRounds = ___m_numRounds;

            if (__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.raidUnloadedAmmoAction, 31);
            }

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 15 * (preNumRounds - postNumRounds);
            //TODO: Set weight of firearm this is attached to if it is
        }
    }

    // Patches FVRFireArm.LoadMag to keep track of weight of mag on firearm and its location index
    class FireArmLoadMagPatch
    {
        public static bool ignoreLoadMag;

        static void Prefix(FVRFireArmMagazine mag, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (ignoreLoadMag)
            {
                ignoreLoadMag = false;
                return;
            }

            if (mag.m_hand != null)
            {
                // TODO: Might have to do this for ammo when putting it into a mag?
                EndInteractionPatch.ignoreEndInteraction = true; // To prevent EndInteraction from handling us dropping the mag when inserting in into a firearm

                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                switch (fireArmVID.weaponClass)
                {
                    case Mod.WeaponClass.Pistol:
                        Mod.AddSkillExp(EFM_Skill.pistolWeaponReloadAction, 12);
                        break;
                    case Mod.WeaponClass.Revolver:
                        Mod.AddSkillExp(EFM_Skill.revolverWeaponReloadAction, 13);
                        break;
                    case Mod.WeaponClass.SMG:
                        Mod.AddSkillExp(EFM_Skill.SMGWeaponReloadAction, 14);
                        break;
                    case Mod.WeaponClass.Assault:
                        Mod.AddSkillExp(EFM_Skill.assaultWeaponReloadAction, 15);
                        break;
                    case Mod.WeaponClass.Shotgun:
                        Mod.AddSkillExp(EFM_Skill.shotgunWeaponReloadAction, 16);
                        break;
                    case Mod.WeaponClass.Sniper:
                        Mod.AddSkillExp(EFM_Skill.sniperWeaponReloadAction, 17);
                        break;
                    case Mod.WeaponClass.LMG:
                        Mod.AddSkillExp(EFM_Skill.LMGWeaponReloadAction, 18);
                        break;
                    case Mod.WeaponClass.HMG:
                        Mod.AddSkillExp(EFM_Skill.HMGWeaponReloadAction, 19);
                        break;
                    case Mod.WeaponClass.Launcher:
                        Mod.AddSkillExp(EFM_Skill.launcherWeaponReloadAction, 20);
                        break;
                    case Mod.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(EFM_Skill.attachedLauncherWeaponReloadAction, 21);
                        break;
                    case Mod.WeaponClass.DMR:
                        Mod.AddSkillExp(EFM_Skill.DMRWeaponReloadAction, 24);
                        break;
                }
            }

            if (__instance.Magazine == null && mag != null)
            {
                EFM_VanillaItemDescriptor magVID = mag.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight += magVID.currentWeight;

                if(magVID.locationIndex == 0) // Player
                {
                    // Went from player to firearm location index
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Even if transfered from player to player, we don't want to consider it in inventory anymore
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.RemoveFromPlayerInventory(magVID.transform, true);
                        }

                        // No difference to weight
                    }
                    else // Hideout/Raid
                    {
                        // Transfered from player to hideout or raid but we dont want to consider it in baseinventory because it is inside a firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.RemoveFromPlayerInventory(magVID.transform, true);
                        }

                        // Update player weight
                        Mod.weight -= magVID.currentWeight;
                    }
                }
                else if(magVID.locationIndex == 1) // Hideout
                {
                    // Went from hideout to firearm locationIndex
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Transfered from hideout to player, dont want to consider it in player inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.currentBaseManager.RemoveFromBaseInventory(magVID.transform, true);
                        }

                        // Update player weight
                        Mod.weight += magVID.currentWeight;
                    }
                    else if(fireArmVID.locationIndex == 1) // Hideout
                    {
                        // Transfered from hideout to hideout, dont want to consider it in base inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.currentBaseManager.RemoveFromBaseInventory(magVID.transform, true);
                        }

                        // No change to player weight
                    }
                    else // Raid
                    {
                        Mod.instance.LogError("Fire arm load mag patch impossible case: Mag loaded from hideout to raid, meaning mag had wrong location index while on player");
                    }
                }
                else // Raid
                {
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Transfered from raid to player, dont want to add to inventory because it is in firearm

                        // Update player weight
                        Mod.weight += magVID.currentWeight;
                    }
                }

                BeginInteractionPatch.SetItemLocationIndex(fireArmVID.locationIndex, null, magVID);
            }
        }
    }

    // Patches FVRFireArm.EjectMag to keep track of weight of mag on firearm and its location index
    class FireArmEjectMagPatch
    {
        static int preLocationIndex;
        static EFM_VanillaItemDescriptor preMagVID;

        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Magazine != null)
            {
                EFM_VanillaItemDescriptor magVID = __instance.Magazine.GetComponent<EFM_VanillaItemDescriptor>();

                preLocationIndex = magVID.locationIndex;
                preMagVID = magVID;
            }
        }

        static void Postfix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            fireArmVID.currentWeight -= preMagVID.currentWeight;

            int currentLocationIndex = 0;
            if(preMagVID.physObj.m_hand == null) // Not in a hand
            {
                currentLocationIndex = Mod.currentLocationIndex;
            }

            if(preLocationIndex == 0)
            {
                if(currentLocationIndex == 0)
                {
                    // Transfered from player to player, from firearm to hand
                    Mod.AddToPlayerInventory(preMagVID.transform, true);

                    // No change to player weight
                }
                else if(currentLocationIndex == 1)
                {
                    // Transfered from player to hideout
                    Mod.currentBaseManager.AddToBaseInventory(preMagVID.transform, true);

                    // Update player weight
                    Mod.weight -= preMagVID.currentWeight;
                }
                else
                {
                    // Transfered from player to raid, no list to update

                    // Update player weight
                    Mod.weight -= preMagVID.currentWeight;
                }
            }
            else if(preLocationIndex == 1)
            {
                if (currentLocationIndex == 0)
                {
                    // Transfered from hideout to player, from firearm to hand
                    Mod.AddToPlayerInventory(preMagVID.transform, true);

                    // Update player weight
                    Mod.weight += preMagVID.currentWeight;
                }
                else if (currentLocationIndex == 1)
                {
                    // Transfered from hideout to hideout
                    Mod.currentBaseManager.AddToBaseInventory(preMagVID.transform, true);

                    // No change to player weight
                }
                else
                {
                    // Transfered from hideout to raid
                    Mod.instance.LogError("Fire arm eject mag patch impossible case: Mag ejected from hideout to raid, meaning mag had wrong location index while on player or in raid");
                }
            }
            else
            {
                if (currentLocationIndex == 0)
                {
                    // Transfered from raid to player, from firearm to hand
                    Mod.AddToPlayerInventory(preMagVID.transform, true);

                    // Update player weight
                    Mod.weight += preMagVID.currentWeight;
                }
                else if (currentLocationIndex == 1)
                {
                    // Transfered from raid to hideout
                    Mod.instance.LogError("Fire arm eject mag patch impossible case: Mag ejected from raid to hideout, meaning mag had wrong location index while on player or in hideout");
                }
                else
                {
                    // Transfered from raid to raid, nothing to update
                }
            }

            BeginInteractionPatch.SetItemLocationIndex(currentLocationIndex, null, preMagVID);
        }
    }

    // Patches FVRFireArm.LoadClip to keep track of weight of clip on firearm and its location index
    class FireArmLoadClipPatch
    {
        public static bool ignoreLoadClip;

        static void Prefix(FVRFireArmClip clip, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (ignoreLoadClip)
            {
                ignoreLoadClip = false;
                return;
            }

            if (clip.m_hand != null)
            {
                EndInteractionPatch.ignoreEndInteraction = true; // To prevent EndInteraction from handling us dropping the clip

                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                switch (fireArmVID.weaponClass)
                {
                    case Mod.WeaponClass.Pistol:
                        Mod.AddSkillExp(EFM_Skill.pistolWeaponReloadAction, 12);
                        break;
                    case Mod.WeaponClass.Revolver:
                        Mod.AddSkillExp(EFM_Skill.revolverWeaponReloadAction, 13);
                        break;
                    case Mod.WeaponClass.SMG:
                        Mod.AddSkillExp(EFM_Skill.SMGWeaponReloadAction, 14);
                        break;
                    case Mod.WeaponClass.Assault:
                        Mod.AddSkillExp(EFM_Skill.assaultWeaponReloadAction, 15);
                        break;
                    case Mod.WeaponClass.Shotgun:
                        Mod.AddSkillExp(EFM_Skill.shotgunWeaponReloadAction, 16);
                        break;
                    case Mod.WeaponClass.Sniper:
                        Mod.AddSkillExp(EFM_Skill.sniperWeaponReloadAction, 17);
                        break;
                    case Mod.WeaponClass.LMG:
                        Mod.AddSkillExp(EFM_Skill.LMGWeaponReloadAction, 18);
                        break;
                    case Mod.WeaponClass.HMG:
                        Mod.AddSkillExp(EFM_Skill.HMGWeaponReloadAction, 19);
                        break;
                    case Mod.WeaponClass.Launcher:
                        Mod.AddSkillExp(EFM_Skill.launcherWeaponReloadAction, 20);
                        break;
                    case Mod.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(EFM_Skill.attachedLauncherWeaponReloadAction, 21);
                        break;
                    case Mod.WeaponClass.DMR:
                        Mod.AddSkillExp(EFM_Skill.DMRWeaponReloadAction, 24);
                        break;
                }
            }

            if (__instance.Clip == null && clip != null)
            {
                EFM_VanillaItemDescriptor clipVID = clip.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight += clipVID.currentWeight;

                if (clipVID.locationIndex == 0) // Player
                {
                    // Went from player to firearm location index
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Even if transfered from player to player, we don't want to consider it in inventory anymore
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.RemoveFromPlayerInventory(clipVID.transform, true);
                        }

                        // No difference to weight
                    }
                    else // Hideout/Raid
                    {
                        // Transfered from player to hideout or raid but we dont want to consider it in baseinventory because it is inside a firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.RemoveFromPlayerInventory(clipVID.transform, true);
                        }

                        // Update player weight
                        Mod.weight -= clipVID.currentWeight;
                    }
                }
                else if (clipVID.locationIndex == 1) // Hideout
                {
                    // Went from hideout to firearm locationIndex
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Transfered from hideout to player, dont want to consider it in player inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.currentBaseManager.RemoveFromBaseInventory(clipVID.transform, true);
                        }

                        // Update player weight
                        Mod.weight += clipVID.currentWeight;
                    }
                    else if (fireArmVID.locationIndex == 1) // Hideout
                    {
                        // Transfered from hideout to hideout, dont want to consider it in base inventory because it is in firearm
                        if (!Mod.preventLoadMagUpdateLists)
                        {
                            Mod.currentBaseManager.RemoveFromBaseInventory(clipVID.transform, true);
                        }

                        // No change to player weight
                    }
                    else // Raid
                    {
                        Mod.instance.LogError("Fire arm load clip patch impossible case: Mag loaded from hideout to raid, meaning mag had wrong location index while on player");
                    }
                }
                else // Raid
                {
                    if (fireArmVID.locationIndex == 0) // Player
                    {
                        // Transfered from raid to player, dont want to add to inventory because it is in firearm

                        // Update player weight
                        Mod.weight += clipVID.currentWeight;
                    }
                }

                BeginInteractionPatch.SetItemLocationIndex(fireArmVID.locationIndex, null, clipVID);
            }
        }
    }

    // Patches FVRFireArm.EjectClip to keep track of weight of clip on firearm and its location index
    class FireArmEjectClipPatch
    {
        static int preLocationIndex;
        static EFM_VanillaItemDescriptor preClipVID;

        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Clip != null)
            {
                EFM_VanillaItemDescriptor clipVID = __instance.Clip.GetComponent<EFM_VanillaItemDescriptor>();

                preLocationIndex = clipVID.locationIndex;
                preClipVID = clipVID;
            }
        }

        static void Postfix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            fireArmVID.currentWeight -= preClipVID.currentWeight;

            int currentLocationIndex = 0;
            if (preClipVID.physObj.m_hand == null) // Not in a hand
            {
                currentLocationIndex = Mod.currentLocationIndex;
            }

            if (preLocationIndex == 0)
            {
                if (currentLocationIndex == 0)
                {
                    // Transfered from player to player, from firearm to hand
                    Mod.AddToPlayerInventory(preClipVID.transform, true);

                    // No change to player weight
                }
                else if (currentLocationIndex == 1)
                {
                    // Transfered from player to hideout
                    Mod.currentBaseManager.AddToBaseInventory(preClipVID.transform, true);

                    // Update player weight
                    Mod.weight -= preClipVID.currentWeight;
                }
                else
                {
                    // Transfered from player to raid, no list to update

                    // Update player weight
                    Mod.weight -= preClipVID.currentWeight;
                }
            }
            else if (preLocationIndex == 1)
            {
                if (currentLocationIndex == 0)
                {
                    // Transfered from hideout to player, from firearm to hand
                    Mod.AddToPlayerInventory(preClipVID.transform, true);

                    // Update player weight
                    Mod.weight += preClipVID.currentWeight;
                }
                else if (currentLocationIndex == 1)
                {
                    // Transfered from hideout to hideout
                    Mod.currentBaseManager.AddToBaseInventory(preClipVID.transform, true);

                    // No change to player weight
                }
                else
                {
                    // Transfered from hideout to raid
                    Mod.instance.LogError("Fire arm eject clip patch impossible case: Clip ejected from hideout to raid, meaning mag had wrong location index while on player or in raid");
                }
            }
            else
            {
                if (currentLocationIndex == 0)
                {
                    // Transfered from raid to player, from firearm to hand
                    Mod.AddToPlayerInventory(preClipVID.transform, true);

                    // Update player weight
                    Mod.weight += preClipVID.currentWeight;
                }
                else if (currentLocationIndex == 1)
                {
                    // Transfered from raid to hideout
                    Mod.instance.LogError("Fire arm eject clip patch impossible case: Clip ejected from raid to hideout, meaning mag had wrong location index while on player or in hideout");
                }
                else
                {
                    // Transfered from raid to raid, nothing to update
                }
            }

            BeginInteractionPatch.SetItemLocationIndex(currentLocationIndex, null, preClipVID);
        }
    }

    // Patches FVRFirearmMagazine.AddRound(Round) to keep track of weight and amount in ammoboxes
    class MagAddRoundPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            Mod.AddSkillExp(EFM_Skill.raidLoadedAmmoAction, 31);

            if (addedRound)
            {
                addedRound = false;
                EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();

                if (VID != null)
                {
                    VID.currentWeight += 15;
                }
                else if(CIW != null)
                {
                    CIW.currentWeight += 15;
                    CIW.amount += 1;
                }
            }
        }
    }

    // Patches FVRFirearmMagazine.AddRound(Class) to keep track of weight and amount in ammoboxes
    class MagAddRoundClassPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmMagazine __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (addedRound)
            {
                addedRound = false;
                EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();

                if (VID != null)
                {
                    VID.currentWeight += 15;
                }
                else if (CIW != null)
                {
                    CIW.currentWeight += 15;
                    CIW.amount += 1;
                }
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Round) to keep track of weight
    class ClipAddRoundPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            Mod.AddSkillExp(EFM_Skill.raidLoadedAmmoAction, 31);

            if (addedRound)
            {
                addedRound = false;
                __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 15;
            }
        }
    }

    // Patches FVRFirearmClip.AddRound(Class) to keep track of weight
    class ClipAddRoundClassPatch
    {
        static bool addedRound = false;

        static void Prefix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.m_numRounds < __instance.m_capacity)
            {
                addedRound = true;
            }
        }

        static void Postfix(ref FVRFireArmClip __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (addedRound)
            {
                addedRound = false;
                __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 15;
            }
        }
    }

    // Patches FVRFireArmAttachmentMount.RegisterAttachment to keep track of weight
    // This completely replaces the original
    class AttachmentMountRegisterPatch
    {
        static bool Prefix(FVRFireArmAttachment attachment, ref HashSet<FVRFireArmAttachment> ___AttachmentsHash, ref FVRFireArmAttachmentMount __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___AttachmentsHash.Add(attachment))
            {
                __instance.AttachmentsList.Add(attachment);
                if (__instance.HasHoverDisablePiece && __instance.DisableOnHover.activeSelf)
                {
                    __instance.DisableOnHover.SetActive(false);
                }

                // Add weight to parent
                EFM_VanillaItemDescriptor parentVID = __instance.Parent.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor attachmentVID = __instance.Parent.GetComponent<EFM_VanillaItemDescriptor>();
                parentVID.currentWeight += attachmentVID.currentWeight;

                BeginInteractionPatch.SetItemLocationIndex(parentVID.locationIndex, null, attachmentVID);
            }

            return false;
        }
    }

    // Patches FVRFireArmAttachmentMount.DeRegisterAttachment to keep track of weight
    // This completely replaces the original
    class AttachmentMountDeRegisterPatch
    {
        static bool Prefix(FVRFireArmAttachment attachment, ref HashSet<FVRFireArmAttachment> ___AttachmentsHash, ref FVRFireArmAttachmentMount __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (___AttachmentsHash.Remove(attachment))
            {
                __instance.AttachmentsList.Remove(attachment);

                // Add weight to parent
                EFM_VanillaItemDescriptor parentVID = __instance.Parent.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor attachmentVID = __instance.Parent.GetComponent<EFM_VanillaItemDescriptor>();
                parentVID.currentWeight -= attachmentVID.currentWeight;

                BeginInteractionPatch.SetItemLocationIndex(0, null, attachmentVID);
            }

            return false;
        }
    }

    // Patches AIManager.EntityCheck to use our own entity lists instead of OverlapSphere to check other entities
    // This completely replaces the original
    class EntityCheckPatch
    {
        static bool Prefix(AIEntity e)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            e.ResetTick();
            if (e.ReceivesEvent_Visual)
            {
                Vector3 pos = e.GetPos();
                Vector3 forward = e.SensoryFrame.forward;
                if (EFM_Raid_Manager.entities.Count > 0)
                {
                    for (int i = 0; i < EFM_Raid_Manager.entities.Count; i++)
                    {
                        AIEntity component = EFM_Raid_Manager.entities[i];
                        if (!(component == null))
                        {
                            if (!(component == e))
                            {
                                if (component.IFFCode >= -1)
                                {
                                    if (!component.IsPassiveEntity || e.PerceivesPassiveEntities)
                                    {
                                        Vector3 pos2 = component.GetPos();
                                        Vector3 to = pos2 - pos;
                                        float num = to.magnitude;
                                        float dist = num;
                                        float num2 = e.MaximumSightRange;
                                        if (num <= component.MaxDistanceVisibleFrom)
                                        {
                                            if (component.VisibilityMultiplier <= 2f)
                                            {
                                                if (component.VisibilityMultiplier > 1f)
                                                {
                                                    num = Mathf.Lerp(num, num2, component.VisibilityMultiplier - 1f);
                                                }
                                                else
                                                {
                                                    num = Mathf.Lerp(0f, num, component.VisibilityMultiplier);
                                                }
                                                if (!e.IsVisualCheckOmni)
                                                {
                                                    float num3 = Vector3.Angle(forward, to);
                                                    num2 = e.MaximumSightRange * e.SightDistanceByFOVMultiplier.Evaluate(num3 / e.MaximumSightFOV);
                                                }
                                                if (num <= num2)
                                                {
                                                    if (!Physics.Linecast(pos, pos2, e.LM_VisualOcclusionCheck, QueryTriggerInteraction.Collide))
                                                    {
                                                        float v = num / e.MaximumSightRange * component.DangerMultiplier;
                                                        AIEvent e2 = new AIEvent(component, AIEvent.AIEType.Visual, v, dist);
                                                        e.OnAIEventReceive(e2);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRFireArmChamber.EjectRound to keep track of the ejected round
    class ChamberEjectRoundPatch
    {
        static void Postfix(ref FVRFireArmRound __result)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__result != null && !__result.IsSpent)
            {
                EFM_VanillaItemDescriptor vanillaItemDescriptor = __result.GetComponent<EFM_VanillaItemDescriptor>();

                BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, vanillaItemDescriptor, true);

                GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                if (Mod.currentLocationIndex == 1)
                {
                    // Now in hideout
                    Mod.currentBaseManager.AddToBaseInventory(vanillaItemDescriptor.transform, true);

                    __result.transform.parent = sceneRoot.transform.GetChild(2);
                }
                else if (Mod.currentLocationIndex == 2)
                {
                    __result.transform.parent = sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2);
                }
            }
        }
    }

    // Patches FVRInteractiveObject.GlobalFixedUpdate to fix positioning of attachments after hideout load
    class GlobalFixedUpdatePatch
    {
        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (Mod.attachmentCheckNeeded >= 0) 
            {
                --Mod.attachmentCheckNeeded;

                if (Mod.attachmentCheckNeeded == 0)
                {
                    foreach (KeyValuePair<GameObject, object> attachCheck in Mod.attachmentLocalTransform)
                    {
                        if(attachCheck.Value is Transform)
                        {
                            attachCheck.Key.transform.localPosition = (attachCheck.Value as Transform).localPosition;
                            attachCheck.Key.transform.localRotation = (attachCheck.Value as Transform).localRotation;
                        }
                        else
                        {
                            attachCheck.Key.transform.localPosition = (attachCheck.Value as Vector3[])[0];
                            attachCheck.Key.transform.localEulerAngles = (attachCheck.Value as Vector3[])[1];
                        }
                    }
                }
            }

            if (Mod.physObjColResetNeeded >= 0) 
            {
                --Mod.physObjColResetNeeded;

                if (Mod.physObjColResetNeeded == 0)
                {
                    foreach (FVRPhysicalObject physObj in Mod.physObjColResetList)
                    {
                        physObj.SetAllCollidersToLayer(false, "Default");
                    }
                }
            }
        }
    }

    // Patches FVRInteractiveObject.PlayGrabSound to use custom item sounds 
    // This completely replaces the original
    class PlayGrabSoundPatch
    {
        static bool Prefix(ref FVRInteractiveObject __instance, bool isHard, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (hand.CanMakeGrabReleaseSound)
            {
                if (__instance.HandlingGrabSound != HandlingGrabType.None)
                {
                    SM.PlayHandlingGrabSound(__instance.HandlingGrabSound, hand.Input.Pos, isHard);
                    hand.HandMadeGrabReleaseSound();
                }
                else
                {
                    EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
                    //string[] soundCategories = new string[] { "drop", "pickup", "offline_use", "open", "use", "use_loop" };
                    if (CIW != null && CIW.itemSounds != null && CIW.itemSounds[1] != null) 
                    {
                        AudioEvent audioEvent = new AudioEvent();
                        audioEvent.Clips.Add(CIW.itemSounds[1]);
                        SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.Input.Pos);
                        hand.HandMadeGrabReleaseSound();
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRInteractiveObject.PlayReleaseSound to use custom item sounds 
    // This completely replaces the original
    class PlayReleaseSoundPatch
    {
        static bool Prefix(ref FVRInteractiveObject __instance, FVRViveHand hand)
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            if (hand.CanMakeGrabReleaseSound)
            {
                if (__instance.HandlingReleaseSound != HandlingReleaseType.None)
                {
                    SM.PlayHandlingReleaseSound(__instance.HandlingReleaseSound, hand.Input.Pos);
                    hand.HandMadeGrabReleaseSound();
                }
                else
                {
                    EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
                    //string[] soundCategories = new string[] { "drop", "pickup", "offline_use", "open", "use", "use_loop" };
                    if (CIW != null && CIW.itemSounds != null && CIW.itemSounds[0] != null) 
                    {
                        AudioEvent audioEvent = new AudioEvent();
                        audioEvent.Clips.Add(CIW.itemSounds[0]);
                        SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.Input.Pos);
                        hand.HandMadeGrabReleaseSound();
                    }
                }
            }

            return false;
        }
    }

    // Patches FVRFirearm.Fire to know when a weapon is fired
    class FireArmFirePatch
    {
        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if(__instance.m_hand != null)
            {
                Mod.AddSkillExp(EFM_Skill.recoilAction, 25);
                Mod.AddSkillExp(EFM_Skill.weaponShotAction, 26);

                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                switch (fireArmVID.weaponClass)
                {
                    case Mod.WeaponClass.Pistol:
                        Mod.AddSkillExp(EFM_Skill.pistolWeaponShotAction, 12);
                        break;
                    case Mod.WeaponClass.Revolver:
                        Mod.AddSkillExp(EFM_Skill.revolverWeaponShotAction, 13);
                        break;
                    case Mod.WeaponClass.SMG:
                        Mod.AddSkillExp(EFM_Skill.SMGWeaponShotAction, 14);
                        break;
                    case Mod.WeaponClass.Assault:
                        Mod.AddSkillExp(EFM_Skill.assaultWeaponShotAction, 15);
                        break;
                    case Mod.WeaponClass.Shotgun:
                        Mod.AddSkillExp(EFM_Skill.shotgunWeaponShotAction, 16);
                        break;
                    case Mod.WeaponClass.Sniper:
                        Mod.AddSkillExp(EFM_Skill.sniperWeaponShotAction, 17);
                        break;
                    case Mod.WeaponClass.LMG:
                        Mod.AddSkillExp(EFM_Skill.LMGWeaponShotAction, 18);
                        break;
                    case Mod.WeaponClass.HMG:
                        Mod.AddSkillExp(EFM_Skill.HMGWeaponShotAction, 19);
                        break;
                    case Mod.WeaponClass.Launcher:
                        Mod.AddSkillExp(EFM_Skill.launcherWeaponShotAction, 20);
                        break;
                    case Mod.WeaponClass.AttachedLauncher:
                        Mod.AddSkillExp(EFM_Skill.attachedLauncherWeaponShotAction, 21);
                        break;
                    case Mod.WeaponClass.DMR:
                        Mod.AddSkillExp(EFM_Skill.DMRWeaponShotAction, 24);
                        break;
                }
            }

            FireArmRecoilPatch.fromFire = true;
        }
    }

    // Patches FVRFirearm.Recoil to control recoil strengh
    class FireArmRecoilPatch
    {
        public static bool fromFire = false;

        static void Prefix(ref FVRFireArm __instance, ref float VerticalRecoilMult)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            // If this recoil is caused by firing firearm (Could be cause by tremors)
            if (fromFire)
            {
                if (__instance.m_hand != null)
                {
                    float originalRecoilMult = VerticalRecoilMult;
                    VerticalRecoilMult -= originalRecoilMult * (EFM_Skill.recoilBonusPerLevel * (Mod.skills[25].currentProgress / 100));

                    EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                    switch (fireArmVID.weaponClass)
                    {
                        case Mod.WeaponClass.Pistol:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[12].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.Revolver:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[13].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.SMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[14].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.Assault:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[15].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.Shotgun:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[16].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.Sniper:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[17].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.LMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[18].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.HMG:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[19].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.Launcher:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[20].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.AttachedLauncher:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[21].currentProgress / 100));
                            break;
                        case Mod.WeaponClass.DMR:
                            VerticalRecoilMult -= originalRecoilMult * (0.003f * (Mod.skills[24].currentProgress / 100));
                            break;
                    }

                    VerticalRecoilMult -= originalRecoilMult * 0.005f * (Mod.skills[26].currentProgress / 100);
                }

                fromFire = false;
            }
        }
    }

    // Patches FVRViveHand.CurrentInteractable.set to keep track of item held
    class HandCurrentInteractableSetPatch
    {
        static void Postfix(ref FVRViveHand __instance, ref FVRInteractiveObject ___m_currentInteractable)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            EFM_Hand handToUse = __instance.GetComponent<EFM_Hand>();
            if (___m_currentInteractable != null)
            {
                handToUse.CIW = ___m_currentInteractable.GetComponent<EFM_CustomItemWrapper>();
                handToUse.VID = ___m_currentInteractable.GetComponent<EFM_VanillaItemDescriptor>();
                handToUse.custom = handToUse.CIW != null;
                handToUse.hasScript = handToUse.custom || handToUse.VID != null;
            }
            else
            {
                handToUse.CIW = null;
                handToUse.VID = null;
                handToUse.hasScript = false;
            }
        }
    }

    // Patches SosigLink.Damage to keep track of player shots on AI
    class SosigLinkDamagePatch
    {
        static void Prefix(ref SosigLink __instance, Damage d)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if(d.Source_IFF == 0)
            {
                EFM_AI AIScript = __instance.S.GetComponent<EFM_AI>();
                AISpawn.AISpawnType AIType = AIScript.type;
                bool AIUsec = AIScript.USEC;
                switch (__instance.BodyPart)
                {
                    case SosigLink.SosigBodyPart.Head:
                        UpdateShotsCounterConditions(TraderTaskCounterCondition.CounterConditionTargetBodyPart.Head, d.point, AIType, AIUsec);
                        break;
                    case SosigLink.SosigBodyPart.Torso:
                        float thoraxChance = 0.5f; // 50%
                        float leftArmChance = 0.65f; // 15%
                        float rightArmChance = 0.8f; // 15%
                        // float stomachChance = 1f; // 20%
                        TraderTaskCounterCondition.CounterConditionTargetBodyPart chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                        float rand = UnityEngine.Random.value;
                        if(rand <= thoraxChance)
                        {
                            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                        }
                        else if(rand <= leftArmChance)
                        {
                            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftArm;
                        }
                        else if(rand <= rightArmChance)
                        {
                            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightArm;
                        }
                        else
                        {
                            chosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                        }
                        UpdateShotsCounterConditions(chosenBodyPart, d.point, AIType, AIUsec);
                        break;
                    case SosigLink.SosigBodyPart.UpperLink:
                        float stomachChance = 0.5f; // 50%
                        float upperLeftLegChance = 0.65f; // 15%
                        float upperRightLegChance = 0.8f; // 15%
                        // float thoraxChance = 1f; // 20%
                        TraderTaskCounterCondition.CounterConditionTargetBodyPart upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                        float upperRand = UnityEngine.Random.value;
                        if (upperRand <= stomachChance)
                        {
                            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                        }
                        else if (upperRand <= upperLeftLegChance)
                        {
                            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftLeg;
                        }
                        else if (upperRand <= upperRightLegChance)
                        {
                            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightLeg;
                        }
                        else
                        {
                            upperChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                        }
                        UpdateShotsCounterConditions(upperChosenBodyPart, d.point, AIType, AIUsec);
                        break;
                    case SosigLink.SosigBodyPart.LowerLink:
                        float lowerStomachChance = 0.20f; // 20%
                        float leftLegChance = 0.6f; // 40%
                        // float rightLegChance = 1f; // 40%
                        TraderTaskCounterCondition.CounterConditionTargetBodyPart lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax;
                        float lowerRand = UnityEngine.Random.value;
                        if (lowerRand <= lowerStomachChance)
                        {
                            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach;
                        }
                        else if (lowerRand <= leftLegChance)
                        {
                            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftLeg;
                        }
                        else
                        {
                            lowerChosenBodyPart = TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightLeg;
                        }
                        UpdateShotsCounterConditions(lowerChosenBodyPart, d.point, AIType, AIUsec);
                        break;
                }
            }
        }

        static void UpdateShotsCounterConditions(TraderTaskCounterCondition.CounterConditionTargetBodyPart bodyPart, Vector3 hitPoint, AISpawn.AISpawnType AIType, bool USEC)
        {
            foreach(TraderTaskCounterCondition counterCondition in Mod.currentShotsCounterConditionsByBodyPart[bodyPart])
            {
                // Check condition state validity
                if (!counterCondition.parentCondition.visible)
                {
                    continue;
                }

                // Check enemy type
                if (!((counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Any) ||
                      (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav && AIType == AISpawn.AISpawnType.Scav) ||
                      (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec && AIType == AISpawn.AISpawnType.PMC && USEC) ||
                      (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear && AIType == AISpawn.AISpawnType.PMC && !USEC) ||
                      (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC && AIType == AISpawn.AISpawnType.PMC)))
                {
                    continue;
                }

                // Check weapon
                if (counterCondition.allowedWeaponIDs != null && counterCondition.allowedWeaponIDs.Count > 0)
                {
                    bool isHoldingAllowedWeapon = false;
                    FVRInteractiveObject rightInteractable = Mod.rightHand.fvrHand.CurrentInteractable;
                    if (rightInteractable != null)
                    {
                        EFM_VanillaItemDescriptor VID = rightInteractable.GetComponent<EFM_VanillaItemDescriptor>();
                        if (VID != null)
                        {
                            foreach (string parent in VID.parents)
                            {
                                if (counterCondition.allowedWeaponIDs.Contains(parent))
                                {
                                    isHoldingAllowedWeapon = true;
                                    break;
                                }
                            }
                        }
                        if (!isHoldingAllowedWeapon)
                        {
                            EFM_CustomItemWrapper CIW = rightInteractable.GetComponent<EFM_CustomItemWrapper>();
                            if (CIW != null)
                            {
                                foreach (string parent in CIW.parents)
                                {
                                    if (counterCondition.allowedWeaponIDs.Contains(parent))
                                    {
                                        isHoldingAllowedWeapon = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (!isHoldingAllowedWeapon)
                    {
                        FVRInteractiveObject leftInteractable = Mod.leftHand.fvrHand.CurrentInteractable;
                        if (leftInteractable != null)
                        {
                            EFM_VanillaItemDescriptor VID = leftInteractable.GetComponent<EFM_VanillaItemDescriptor>();
                            if (VID != null)
                            {
                                foreach (string parent in VID.parents)
                                {
                                    if (counterCondition.allowedWeaponIDs.Contains(parent))
                                    {
                                        isHoldingAllowedWeapon = true;
                                        break;
                                    }
                                }
                            }
                            if (!isHoldingAllowedWeapon)
                            {
                                EFM_CustomItemWrapper CIW = leftInteractable.GetComponent<EFM_CustomItemWrapper>();
                                if (CIW != null)
                                {
                                    foreach (string parent in CIW.parents)
                                    {
                                        if (counterCondition.allowedWeaponIDs.Contains(parent))
                                        {
                                            isHoldingAllowedWeapon = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!isHoldingAllowedWeapon)
                    {
                        continue;
                    }
                }

                // Check distance
                if (counterCondition.distance != -1)
                {
                    if (counterCondition.distanceCompareMode == 0)
                    {
                        if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, hitPoint) < counterCondition.distance)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, hitPoint) > counterCondition.distance)
                        {
                            continue;
                        }
                    }
                }

                // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                bool constrained = false;
                foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                {
                    if (!EFM_TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                    {
                        constrained = true;
                        break;
                    }
                }
                if (constrained)
                {
                    continue;
                }

                // Successful shot, increment count and update fulfillment 
                ++counterCondition.shotCount;
                EFM_TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
            }
        }
    }

    // Patches FVRPlayerBody.HealPercent to keep track of player's healing from H3 sources like dings and other powerups
    class PlayerBodyHealPercentPatch
    {
        static void Prefix(ref FVRPlayerBody __instance, float f, ref float ___m_startingHealth)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            float amountHealed = Mathf.Max(___m_startingHealth * f, ___m_startingHealth - __instance.Health);
            for(int i=0; i<7;++i)
            {
                if(Mod.health[i] != 0)
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + amountHealed / 7, Mod.health[i], Mod.currentMaxHealth[i]);
                }
            }
        }
    }
    #endregion

    #region DebugPatches
    class EventSystemUpdateDebugPatch
    {
        static void Prefix(ref EventSystem __instance)
        {
            Mod.instance.LogInfo("Update called on event sys in scene " + __instance.gameObject.scene.name + "  physically at: ");
            Transform parent = __instance.transform.parent;
            while(parent != null)
            {
                Mod.instance.LogInfo(parent.name);
                parent = parent.parent;
            }
            
            BaseInputModule sim = __instance.GetComponent<BaseInputModule>();
            if(sim != null)
            {
                sim.enabled = false;
                GameObject.Destroy(sim);
            }
            GameObject.Destroy(__instance);
        }
    }

    class InteractiveGlobalUpdateDebugPatch
    {
        static bool Prefix()
        {
            if (!Mod.inMeatovScene)
            {
                return true;
            }

            return false;
        }
    }

    class inputModuleProcessDebugPatch
    {
        static void Prefix(ref StandaloneInputModule __instance)
        {
            Mod.instance.LogInfo("Process called on standalone input module in scene "+__instance.gameObject.scene.name+" physically at: ");
            Transform parent = __instance.transform.parent;
            while(parent != null)
            {
                Mod.instance.LogInfo(parent.name);
                parent = parent.parent;
            }

            GameObject.Destroy(__instance);
        }
    }

    class DequeueAndPlayDebugPatch
    {
        //AudioSourcePool private instance
        /*private FVRPooledAudioSource DequeueAndPlay(AudioEvent clipSet, Vector3 pos, Vector2 pitch, Vector2 volume, AudioMixerGroup mixerOverride = null)
			{
				FVRPooledAudioSource fvrpooledAudioSource = this.SourceQueue_Disabled.Dequeue();
				fvrpooledAudioSource.gameObject.SetActive(true);
				fvrpooledAudioSource.Play(clipSet, pos, pitch, volume, mixerOverride);
				this.ActiveSources.Add(fvrpooledAudioSource);
				return fvrpooledAudioSource;
			}*/

        static bool Prefix(AudioEvent clipSet, Vector3 pos, Vector2 pitch, Vector2 volume, AudioMixerGroup mixerOverride, ref AudioSourcePool __instance, ref FVRPooledAudioSource __result)
        {
            try
            {
                FVRPooledAudioSource fvrpooledAudioSource = __instance.SourceQueue_Disabled.Dequeue();
                fvrpooledAudioSource.gameObject.SetActive(true);
                fvrpooledAudioSource.Play(clipSet, pos, pitch, volume, mixerOverride);
                __instance.ActiveSources.Add(fvrpooledAudioSource);
                __result = fvrpooledAudioSource;
            }
            catch(NullReferenceException e)
            {
                Mod.instance.LogError("DequeueAndPlayDebugPatch called but threw null exception, __instance.SourceQueue_Disabled null?: " + (__instance.SourceQueue_Disabled == null)+ ", __instance.ActiveSources null?: " + (__instance.ActiveSources == null)+":\n"+e.StackTrace);
            }
            return false;
        }
    }

    class UpdateModeTwoAxisPatch
    {
        static bool Prefix(ref FVRMovementManager __instance, bool IsTwinstick, ref Vector3 ___CurNeckPos, ref Vector3 ___LastNeckPos, ref Vector3 ___correctionDir,
                           ref bool ___m_isLeftHandActive, ref bool ___m_twoAxisGrounded, ref Vector3 ___m_twoAxisVelocity, ref FVRViveHand ___m_authoratativeHand,
                           ref float ___m_armSwingerStepHeight, ref float ___m_delayGroundCheck, ref RaycastHit ___m_hit_ray, ref Vector3 ___m_groundPoint,
                           ref bool ___m_isTwinStickSmoothTurningClockwise, ref bool ___m_isTwinStickSmoothTurningCounterClockwise, ref bool ___IsGrabHolding)
        {
            ___CurNeckPos = GM.CurrentPlayerBody.NeckJointTransform.position;
            Vector3 vector = ___LastNeckPos - ___CurNeckPos;
            Vector3 lastNeckPos = ___LastNeckPos;
            Vector3 a = ___CurNeckPos - ___LastNeckPos;
            RaycastHit raycastHit;
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            if (Physics.SphereCast(___LastNeckPos, 0.15f, a.normalized, out raycastHit, a.magnitude, __instance.LM_TeleCast))
            {
                ___correctionDir = -a * 1f;
            }
            if (IsTwinstick)
            {
                if (!___m_isLeftHandActive && ___m_twoAxisGrounded)
                {
                    ___m_twoAxisVelocity.x = 0f;
                    ___m_twoAxisVelocity.z = 0f;
                }
            }
            else if (___m_authoratativeHand == null && ___m_twoAxisGrounded)
            {
                ___m_twoAxisVelocity.x = 0f;
                ___m_twoAxisVelocity.z = 0f;
            }
            Vector3 vector2 = lastNeckPos;
            Vector3 b = vector2;
            vector2.y = Mathf.Max(vector2.y, __instance.transform.position.y + ___m_armSwingerStepHeight);
            b.y = __instance.transform.position.y;
            float num = Vector3.Distance(vector2, b);
            if (___m_delayGroundCheck > 0f)
            {
                num *= 0.5f;
            }
            bool flag = false;
            Vector3 planeNormal = Vector3.up;
            bool flag2 = false;
            Vector3 vector3 = Vector3.up;
            Vector3 groundPoint = vector2 + -Vector3.up * num;
            Vector3 groundPoint2 = vector2 + -Vector3.up * num;
            float num2 = 90f;
            float a2 = -1000f;
            if (Physics.SphereCast(vector2, 0.2f, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                vector3 = ___m_hit_ray.normal;
                groundPoint = ___m_hit_ray.point;
                groundPoint2 = ___m_hit_ray.point;
                num2 = Vector3.Angle(Vector3.up, ___m_hit_ray.normal);
                a2 = groundPoint.y;
                flag2 = true;
            }
            if (Physics.Raycast(vector2, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                vector3 = ___m_hit_ray.normal;
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                flag2 = true;
            }
            Vector3 vector4 = __instance.Head.forward;
            vector4.y = 0f;
            vector4.Normalize();
            vector4 = Vector3.ClampMagnitude(vector4, 0.1f);
            Vector3 vector5 = __instance.Head.right;
            vector5.y = 0f;
            vector5.Normalize();
            vector5 = Vector3.ClampMagnitude(vector5, 0.1f);
            Vector3 b2 = -vector4;
            Vector3 b3 = -vector5;
            if (Physics.Raycast(vector2 + vector4, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + vector5, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + b2, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (Physics.Raycast(vector2 + b3, -Vector3.up, out ___m_hit_ray, num, __instance.LM_TeleCast))
            {
                num2 = Mathf.Min(num2, Vector3.Angle(Vector3.up, ___m_hit_ray.normal));
                groundPoint.y = Mathf.Max(groundPoint.y, ___m_hit_ray.point.y);
                groundPoint2.y = Mathf.Min(groundPoint.y, ___m_hit_ray.point.y);
                a2 = Mathf.Max(a2, ___m_hit_ray.point.y);
                if (!flag2)
                {
                    vector3 = ___m_hit_ray.normal;
                    flag2 = true;
                }
            }
            if (flag2)
            {
                if (num2 > 70f)
                {
                    flag = true;
                    ___m_twoAxisGrounded = false;
                    planeNormal = vector3;
                    ___m_groundPoint = groundPoint2;
                }
                else
                {
                    ___m_twoAxisGrounded = true;
                    ___m_groundPoint = groundPoint;
                }
            }
            else
            {
                ___m_twoAxisGrounded = false;
                ___m_groundPoint = vector2 - Vector3.up * num;
            }
            Vector3 vector6 = lastNeckPos;
            Vector3 b4 = vector6;
            b4.y = __instance.transform.position.y + 2.15f * GM.CurrentPlayerBody.transform.localScale.y;
            float maxDistance = Vector3.Distance(vector6, b4);
            float num3 = vector6.y + 0.15f;
            if (Physics.SphereCast(vector6, 0.15f, Vector3.up, out ___m_hit_ray, maxDistance, __instance.LM_TeleCast))
            {
                Vector3 point = ___m_hit_ray.point;
                float num4 = Vector3.Distance(vector6, new Vector3(vector6.x, point.y, vector6.z));
                num3 = ___m_hit_ray.point.y - 0.15f;
                float num5 = Mathf.Clamp(GM.CurrentPlayerBody.Head.localPosition.y, 0.3f, 2.5f);
                float y = ___m_groundPoint.y;
                float min = y - (num5 - 0.2f);
                float y2 = Mathf.Clamp(num3 - num5 - 0.15f, min, y);
                ___m_groundPoint.y = y2;
            }
            if (___m_twoAxisGrounded)
            {
                ___m_twoAxisVelocity.y = 0f;
            }
            else
            {
                float num6 = 5f;
                switch (GM.Options.SimulationOptions.PlayerGravityMode)
                {
                    case SimulationOptions.GravityMode.Realistic:
                        num6 = 9.81f;
                        break;
                    case SimulationOptions.GravityMode.Playful:
                        num6 = 5f;
                        break;
                    case SimulationOptions.GravityMode.OnTheMoon:
                        num6 = 1.62f;
                        break;
                    case SimulationOptions.GravityMode.None:
                        num6 = 0.001f;
                        break;
                }
                if (!flag)
                {
                    ___m_twoAxisVelocity.y = ___m_twoAxisVelocity.y - num6 * Time.deltaTime;
                }
                else
                {
                    Vector3 a3 = Vector3.ProjectOnPlane(-Vector3.up * num6, planeNormal);
                    ___m_twoAxisVelocity += a3 * Time.deltaTime;
                    ___m_twoAxisVelocity = Vector3.ProjectOnPlane(___m_twoAxisVelocity, planeNormal);
                }
            }
            float num7 = Mathf.Abs(lastNeckPos.y - GM.CurrentPlayerBody.transform.position.y);
            Vector3 point2 = lastNeckPos;
            Vector3 point3 = lastNeckPos;
            point2.y = Mathf.Min(point2.y, num3 - 0.01f);
            point3.y = Mathf.Max(__instance.transform.position.y, ___m_groundPoint.y) + (___m_armSwingerStepHeight + 0.2f);
            point2.y = Mathf.Max(point2.y, point3.y);
            Vector3 vector7 = ___m_twoAxisVelocity;
            float maxLength = ___m_twoAxisVelocity.magnitude * Time.deltaTime;
            if (Physics.CapsuleCast(point2, point3, 0.15f, ___m_twoAxisVelocity, out ___m_hit_ray, ___m_twoAxisVelocity.magnitude * Time.deltaTime + 0.1f, __instance.LM_TeleCast))
            {
                vector7 = Vector3.ProjectOnPlane(___m_twoAxisVelocity, ___m_hit_ray.normal);
                maxLength = ___m_hit_ray.distance * 0.5f;
                if (___m_twoAxisGrounded)
                {
                    vector7.y = 0f;
                }
                RaycastHit raycastHit2;
                if (Physics.CapsuleCast(point2, point3, 0.15f, vector7, out raycastHit2, vector7.magnitude * Time.deltaTime + 0.1f, __instance.LM_TeleCast))
                {
                    maxLength = raycastHit2.distance * 0.5f;
                }
            }
            ___m_twoAxisVelocity = vector7;
            if (___m_twoAxisGrounded)
            {
                ___m_twoAxisVelocity.y = 0f;
            }
            Vector3 a4 = __instance.transform.position;
            Vector3 vector8 = ___m_twoAxisVelocity * Time.deltaTime;
            vector8 = Vector3.ClampMagnitude(vector8, maxLength);
            a4 = __instance.transform.position + vector8;
            if (___m_twoAxisGrounded)
            {
                a4.y = Mathf.MoveTowards(a4.y, ___m_groundPoint.y, 8f * Time.deltaTime * Mathf.Abs(__instance.transform.position.y - ___m_groundPoint.y));
            }
            Vector3 a5 = ___CurNeckPos + vector8;
            a = a5 - ___LastNeckPos;
            if (Physics.SphereCast(___LastNeckPos, 0.15f, a.normalized, out raycastHit, a.magnitude, __instance.LM_TeleCast))
            {
                ___correctionDir = -a * 1f;
            }
            if (GM.Options.MovementOptions.AXButtonSnapTurnState == MovementOptions.AXButtonSnapTurnMode.Smoothturn)
            {
                for (int i = 0; i < __instance.Hands.Length; i++)
                {
                    if (!__instance.Hands[i].IsInStreamlinedMode)
                    {
                        if (__instance.Hands[i].IsThisTheRightHand)
                        {
                            if (__instance.Hands[i].Input.AXButtonPressed)
                            {
                                ___m_isTwinStickSmoothTurningClockwise = true;
                            }
                        }
                        else if (__instance.Hands[i].Input.AXButtonPressed)
                        {
                            ___m_isTwinStickSmoothTurningCounterClockwise = true;
                        }
                    }
                }
            }
            if (!___m_isTwinStickSmoothTurningClockwise && !___m_isTwinStickSmoothTurningCounterClockwise)
            {
                __instance.transform.position = a4 + ___correctionDir;
            }
            else
            {
                Vector3 vector9 = a4 + ___correctionDir;
                Vector3 vector10 = GM.CurrentPlayerBody.transform.forward;
                float num8 = GM.Options.MovementOptions.SmoothTurnMagnitudes[GM.Options.MovementOptions.SmoothTurnMagnitudeIndex] * Time.deltaTime;
                if (___m_isTwinStickSmoothTurningCounterClockwise)
                {
                    num8 = -num8;
                }
                vector9 = __instance.RotatePointAroundPivotWithEuler(vector9, ___CurNeckPos, new Vector3(0f, num8, 0f));
                vector10 = Quaternion.AngleAxis(num8, Vector3.up) * vector10;
                __instance.transform.SetPositionAndRotation(vector9, Quaternion.LookRotation(vector10, Vector3.up));
            }
            typeof(FVRMovementManager).GetMethod("SetTopSpeedLastSecond", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_twoAxisVelocity });
            //__instance.SetTopSpeedLastSecond(___m_twoAxisVelocity);
            if (!___IsGrabHolding)
            {
                typeof(FVRMovementManager).GetMethod("SetFrameSpeed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { ___m_twoAxisVelocity });
                //__instance.SetFrameSpeed(___m_twoAxisVelocity);
            }
            ___LastNeckPos = GM.CurrentPlayerBody.NeckJointTransform.position;
            return false;
        }
    }

    class IsPointInsideSphereGeoPatch
    {
        static bool Prefix(ref FVRQuickBeltSlot __instance, ref bool __result, Vector3 p)
        {
            try
            {
                __result = __instance.HoverGeo.transform.InverseTransformPoint(p).magnitude < 0.5f;
            }
            catch(Exception e)
            {
                Mod.instance.LogInfo("Exception in IsPointInsideSphereGeo called on " + __instance.name+":\n"+e.StackTrace);
                __result = false;
            }
            return false;
        }
    }

    class SetParentagePatch
    {
        static void Prefix(ref FVRPhysicalObject __instance, Transform t)
        {
            Mod.instance.LogInfo("SetParentage called on " + __instance.name+", setting parent to "+(t == null?"null":t.name));
        }
    }

    class SetActivePatch
    {
        static void Prefix(ref GameObject __instance, bool value)
        {
            Mod.instance.LogInfo("SetActive called on " + __instance.name+", with bool: "+value+":\n"+Environment.StackTrace);
        }
    }

    class DestroyPatch
    {
        static void Prefix(UnityEngine.Object obj)
        {
            if (obj != null)
            {
                Mod.instance.LogInfo("Destroy called on " + obj.name+", stack:\n "+Environment.StackTrace);
            }
        }
    }

    class PlayClipDebugPatch
    {
        //AudioSourcePool public instance
        /*public FVRPooledAudioSource PlayClip(AudioEvent clipSet, Vector3 pos, AudioMixerGroup mixerOverride = null)
			{
				if (clipSet.Clips.Count <= 0)
				{
					return null;
				}
				if (this.SourceQueue_Disabled.Count > 0)
				{
					return this.DequeueAndPlay(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				}
				if (this.m_curSize < this.m_maxSize)
				{
					GameObject prefabForType = SM.GetPrefabForType(this.Type);
					this.InstantiateAndEnqueue(prefabForType, true);
					return this.DequeueAndPlay(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				}
				FVRPooledAudioSource fvrpooledAudioSource = this.ActiveSources[0];
				this.ActiveSources.RemoveAt(0);
				if (!fvrpooledAudioSource.gameObject.activeSelf)
				{
					fvrpooledAudioSource.gameObject.SetActive(true);
				}
				fvrpooledAudioSource.Play(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
				this.ActiveSources.Add(fvrpooledAudioSource);
				return fvrpooledAudioSource;
			}*/
        static bool Prefix(AudioEvent clipSet, Vector3 pos, AudioMixerGroup mixerOverride, ref AudioSourcePool __instance, ref FVRPooledAudioSource __result)
        {
            Mod.instance.LogInfo("PlayClip debug prefix called on AudioSourcePool with type: "+ __instance .Type+ " with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
            MethodInfo dequeueAndPlayMethod = typeof(AudioSourcePool).GetMethod("DequeueAndPlay", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo curSizeField = typeof(AudioSourcePool).GetField("m_curSize", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo maxSizeField = typeof(AudioSourcePool).GetField("m_maxSize", BindingFlags.NonPublic | BindingFlags.Instance);
            if (clipSet.Clips.Count <= 0)
            {
                __result = null;
                return false;
            }
            if (__instance.SourceQueue_Disabled.Count > 0)
            {
                Mod.instance.LogInfo("Calling dequeue and play with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
                __result = (FVRPooledAudioSource)dequeueAndPlayMethod.Invoke(__instance, new object[] { clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride });
                return false;
            }
            if ((int)curSizeField.GetValue(__instance) < (int)maxSizeField.GetValue(__instance))
            {
                GameObject prefabForType = SM.GetPrefabForType(__instance.Type);
                __instance.InstantiateAndEnqueue(prefabForType, true);
                Mod.instance.LogInfo("Calling dequeue and play after with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
                __result = (FVRPooledAudioSource)dequeueAndPlayMethod.Invoke(__instance, new object[] { clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride });
                return false;
            }
            FVRPooledAudioSource fvrpooledAudioSource = __instance.ActiveSources[0];
            __instance.ActiveSources.RemoveAt(0);
            if (!fvrpooledAudioSource.gameObject.activeSelf)
            {
                fvrpooledAudioSource.gameObject.SetActive(true);
            }
            fvrpooledAudioSource.Play(clipSet, pos, clipSet.PitchRange, clipSet.VolumeRange, mixerOverride);
            __instance.ActiveSources.Add(fvrpooledAudioSource);
            __result = fvrpooledAudioSource;
            return false;
        }
    }

    class InstantiateAndEnqueueDebugPatch
    {
        /*public void InstantiateAndEnqueue(GameObject prefab, bool active)
			{
				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
				FVRPooledAudioSource component = gameObject.GetComponent<FVRPooledAudioSource>();
				if (!active)
				{
					gameObject.SetActive(false);
				}
				this.SourceQueue_Disabled.Enqueue(component);
				this.m_curSize++;
			}*/

        static bool Prefix(GameObject prefab, bool active, ref AudioSourcePool __instance)
        {
            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(prefab);
            FVRPooledAudioSource component = gameObject.GetComponent<FVRPooledAudioSource>();
            if (!active)
            {
                gameObject.SetActive(false);
            }
            __instance.SourceQueue_Disabled.Enqueue(component);
            FieldInfo curSizeField = typeof(AudioSourcePool).GetField("m_curSize", BindingFlags.NonPublic | BindingFlags.Instance);
            int curSizeVal = (int)curSizeField.GetValue(__instance);
            curSizeField.SetValue(__instance, curSizeVal + 1);
            return false;
        }
    }

    class ChamberFireDebugPatch
    {
        /*public bool Fire()
		    {
			    if (this.IsFull && this.m_round != null && !this.IsSpent)
			    {
				    this.IsSpent = true;
				    this.UpdateProxyDisplay();
				    return true;
			    }
			    return false;
		    }*/

        static bool Prefix(ref bool __result, ref FVRFireArmChamber __instance)
        {
            Mod.instance.LogInfo("Chamber fire prefix called");
            FieldInfo m_roundField = typeof(FVRFireArmChamber).GetField("m_round", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.IsFull && m_roundField.GetValue(__instance) != null && !__instance.IsSpent)
            {
                Mod.instance.LogInfo("\tFire successful");
                __instance.IsSpent = true;
                __instance.UpdateProxyDisplay();
                __result = true;
                return false;
            }
            Mod.instance.LogInfo("\tFire unsuccessful");
            __result = false;
            return false;
        }
    }

    class DropHammerDebugPatch
    {
        /*public void DropHammer()
		{
			if (this.m_isHammerCocked)
			{
				this.m_isHammerCocked = false;
				base.PlayAudioEvent(FirearmAudioEventType.HammerHit, 1f);
				this.Fire();
			}
		}*/

        static bool Prefix(ref bool __result, ref FVRFireArmChamber __instance)
        {
            Mod.instance.LogInfo("Chamber fire prefix called");
            FieldInfo m_roundField = typeof(FVRFireArmChamber).GetField("m_round", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.IsFull && m_roundField.GetValue(__instance) != null && !__instance.IsSpent)
            {
                Mod.instance.LogInfo("\tFire successful");
                __instance.IsSpent = true;
                __instance.UpdateProxyDisplay();
                __result = true;
                return false;
            }
            Mod.instance.LogInfo("\tFire unsuccessful");
            __result = false;
            return false;
        }
    }
    #endregion
}
