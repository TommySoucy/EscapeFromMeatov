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
        public static readonly float[] sizeVolumes = { 1, 2, 5, 30, 0, 50}; // 0: Small, 1: Medium, 2: Large, 3: Massive, 4: None, 5: CantCarryBig

        // Live data
        public static Mod instance;
        public static int currentLocationIndex = -1; // This will be used by custom item wrapper and vanilla item descr. in their Start(). Shoud only ever be 1(base) or 2(raid). If want to spawn an item in player inventory, will have to set it manually
        public static AssetBundle assetsBundle;
        public static AssetBundle menuBundle;
        public static AssetBundle baseAssetsBundle;
        public static AssetBundle baseBundle;
        public static MainMenuSceneDef sceneDef;
        public static List<GameObject> securedObjects;
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
        public static Dictionary<FireArmRoundType, Dictionary<string, int>> roundsByType; // TODO: See if we tak into account ammo in boxes, we should
        public static Dictionary<ItemRarity, List<string>> itemsByRarity;
        public static Dictionary<string, List<string>> itemsByParents;
        public static List<string> usedRoundIDs;
        public static Dictionary<string, int> ammoBoxByAmmoID;
        public static Dictionary<string, int> requiredForQuest;
        public static List<EFM_DescriptionManager> activeDescriptions;
        public static List<EFM_AreaSlot> areaSlots;
        public static bool areaSlotShouldUpdate = true;
        public static List<EFM_AreaBonus> activeBonuses;
        public static EFM_TraderStatus[] traderStatuses;

        // Player
        public static GameObject playerStatusUI;
        public static EFM_PlayerStatusManager playerStatusManager;
        public static Dictionary<string, int> playerInventory;
        public static Dictionary<string, List<GameObject>> playerInventoryObjects;
        public static float[] health; // 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
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
        public static float weightLimit = 55;
        public static float currentDamageModifier = 1;
        public static int stomachBloodLossCount = 0; // If this is 0, in hideout we will regen health otherwise not, in raid we will multiply energy and hydration rate by 5
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
        public static GameObject leftDescriptionUI;
        public static EFM_DescriptionManager leftDescriptionManager;
        public static GameObject rightDescriptionUI;
        public static EFM_DescriptionManager rightDescriptionManager;
        public static GameObject staminaBarUI;
        private static float _weight = 0;
        public static float weight
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
        private static float _currentWeightLimit = 55;
        public static float currentWeightLimit
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
        public static GameObject doorLeftPrefab;
        public static GameObject doorRightPrefab;
        public static GameObject doorDoublePrefab;
        public static bool initDoors = true;
        public static Dictionary<string, Sprite> itemIcons;
        public static GameObject playerStatusUIPrefab;
        public static GameObject extractionUIPrefab;
        public static GameObject extractionCardPrefab;
        public static GameObject consumeUIPrefab;
        public static GameObject stackSplitUIPrefab;
        public static GameObject itemDescriptionUIPrefab;
        public static GameObject neededForPrefab;
        public static GameObject ammoContainsPrefab;
        public static GameObject staminaBarPrefab;
        public static Sprite cartridgeIcon;
        public static Sprite[] playerLevelIcons;

        // DB
        public static JObject areasDB;
        public static JObject localDB;
        public static Dictionary<string, string> itemMap;
        public static JObject[] traderBaseDB;
        public static JObject[] traderAssortDB;
        public static JObject[] traderTasksDB;
        public static JArray[] traderCategoriesDB;
        public static JObject globalDB;
        public static JArray questDB;
        public static JArray XPPerLevel;
        public static JObject mapData;
        public static JObject[] locationsDB;
        public static JArray lootContainerDB;
        public static JObject defaultItemsData;
        public static Dictionary<string, EFM_VanillaItemDescriptor> vanillaItems;
        public static Dictionary<string, JObject> lootContainersByName; 

        // Config settings

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

            LootContainer = 16
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

            DoPatching();

            Init();
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.KeypadPeriod))
            {
                debug = !debug;
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

                if (Input.GetKeyDown(KeyCode.U))
                {
                    GameObject armoredRig = itemPrefabs[375];
                    GameObject armoredRigObject = GameObject.Instantiate(armoredRig, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.5f, Quaternion.identity);
                    FVRObject armoredRigObjectWrapper = armoredRigObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    armoredRigObjectWrapper.ItemID = "375";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        armoredRigObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject basicArmoredRig = itemPrefabs[1];
                    GameObject basicArmoredRigObject = GameObject.Instantiate(basicArmoredRig, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.7f, Quaternion.identity);
                    FVRObject basicArmoredRigObjectWrapper = basicArmoredRigObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    basicArmoredRigObjectWrapper.ItemID = "1";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        basicArmoredRigObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject battery = itemPrefabs[5];
                    GameObject batteryObject = GameObject.Instantiate(battery, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward, Quaternion.identity);
                    FVRObject batteryObjectWrapper = batteryObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    batteryObjectWrapper.ItemID = "5";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        batteryObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject pack = itemPrefabs[2];
                    GameObject packObject = GameObject.Instantiate(pack, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.3f, Quaternion.identity);
                    FVRObject packObjectWrapper = packObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    packObjectWrapper.ItemID = "2";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        packObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject m1911Mag = IM.OD["MagazineStanag20rnd"].GetGameObject();
                    GameObject m1911MagObject = GameObject.Instantiate(m1911Mag, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.2f, Quaternion.identity);
                    FVRObject m1911MagObjectWrapper = m1911MagObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    m1911MagObjectWrapper.ItemID = "MagazineStanag20rnd";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        m1911MagObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }
                }

                if (Input.GetKeyDown(KeyCode.N))
                {
                    SteamVR_LoadLevel.Begin("Grillhouse_2Story", false, 0.5f, 0f, 0f, 0f, 1f);
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    FieldInfo genericPoolField = typeof(SM).GetField("m_pool_generic", BindingFlags.NonPublic | BindingFlags.Instance);
                    Logger.LogInfo("Generic pool null?: "+(genericPoolField.GetValue(ManagerSingleton<SM>.Instance) == null));
                }

                if (Input.GetKeyDown(KeyCode.O))
                {
                    GameObject.Find("Hideout").GetComponent<EFM_Base_Manager>().OnConfirmRaidClicked();
                }

                if (Input.GetKeyDown(KeyCode.J))
                {
                    GameObject m1911Mag = IM.OD["MagazineStanag20rnd"].GetGameObject();
                    GameObject m1911MagObject = GameObject.Instantiate(m1911Mag, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.2f, Quaternion.identity);
                    FVRObject m1911MagObjectWrapper = m1911MagObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    m1911MagObjectWrapper.ItemID = "MagazineStanag20rnd";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        m1911MagObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject round = IM.OD["45ACPCartridgeTracer"].GetGameObject();
                    GameObject roundObject = GameObject.Instantiate(round, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.25f, Quaternion.identity);
                    FVRObject roundObjectWrapper = roundObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    roundObjectWrapper.ItemID = "45ACPCartridgeTracer";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        roundObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject fl = IM.OD["TacticalFlashlight"].GetGameObject();
                    GameObject flObject = GameObject.Instantiate(fl, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.30f, Quaternion.identity);
                    FVRObject flObjectWrapper = flObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    flObjectWrapper.ItemID = "TacticalFlashlight";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        flObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject fg = IM.OD["ForegripAngledBlack"].GetGameObject();
                    GameObject fgObject = GameObject.Instantiate(fg, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.40f, Quaternion.identity);
                    FVRObject fgObjectWrapper = fgObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    fgObjectWrapper.ItemID = "ForegripAngledBlack";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        fgObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }
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
                    DumpLayers();
                }
            }
        }

        private void DumpObjectIDs()
        {
            foreach(string key in IM.OD.Keys)
            {
                Logger.LogInfo("key: "+key+": "+IM.OD[key].DisplayName);
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
                Logger.LogInfo(levelString + "File: "+f.FullName);
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
            try
            {
                string[] lines = System.IO.File.ReadAllLines("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovConfig.txt");

                foreach (string line in lines)
                {
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();
                    string[] tokens = trimmedLine.Split('=');

                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    // TODO
                }

                Logger.LogInfo("Configs loaded");
            }
            catch (FileNotFoundException ex) { Logger.LogInfo("Couldn't find EscapeFromMeatovConfig.txt, using default settings instead. Error: " + ex.Message); }
            catch (Exception ex) { Logger.LogInfo("Couldn't read EscapeFromMeatovConfig.txt, using default settings instead. Error: " + ex.Message); }
        }

        public void LoadAssets()
        {
            LogInfo("Loading assets and scene bundles");
            // Load mod's AssetBundle
            assetsBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovAssets.ab");
            menuBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovMenu.ab");

            LogInfo("Loading Main assets");
            // Load assets
            sceneDefImage = assetsBundle.LoadAsset<Sprite>("Tumbnail");
            mainMenuPointable = assetsBundle.LoadAsset<GameObject>("MeatovPointable");
            quickBeltSlotPrefab = assetsBundle.LoadAsset<GameObject>("QuickBeltSlot");
            rectQuickBeltSlotPrefab = assetsBundle.LoadAsset<GameObject>("RectQuickBeltSlot");
            playerStatusUIPrefab = assetsBundle.LoadAsset<GameObject>("StatusUI");
            consumeUIPrefab = assetsBundle.LoadAsset<GameObject>("ConsumeUI");
            stackSplitUIPrefab = assetsBundle.LoadAsset<GameObject>("StackSplitUI");
            extractionUIPrefab = assetsBundle.LoadAsset<GameObject>("ExtractionUI");
            extractionCardPrefab = assetsBundle.LoadAsset<GameObject>("ExtractionCard");
            itemDescriptionUIPrefab = assetsBundle.LoadAsset<GameObject>("ItemDescriptionUI");
            neededForPrefab = assetsBundle.LoadAsset<GameObject>("NeededForText");
            ammoContainsPrefab = assetsBundle.LoadAsset<GameObject>("ContainsText");
            staminaBarPrefab = assetsBundle.LoadAsset<GameObject>("StaminaBar");
            cartridgeIcon = assetsBundle.LoadAsset<Sprite>("ItemCartridge_Icon");
            playerLevelIcons = new Sprite[16];
            for(int i=1; i <= 16; ++i)
            {
                playerLevelIcons[i - 1] = assetsBundle.LoadAsset<Sprite>("rank"+(i*5));
            }

            // Load pockets configuration
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            List<GameObject> rigConfigurations = new List<GameObject>();
            pocketSlots = new FVRQuickBeltSlot[4];
            int rigIndex = 0;
            GameObject pocketsConfiguration = assetsBundle.LoadAsset<GameObject>("PocketsConfiguration");
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
            itemIcons = new Dictionary<string, Sprite>();
            defaultItemsData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DefaultItemData.txt"));
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            for (int i = 0; i < ((JArray)defaultItemsData["ItemDefaults"]).Count; ++i)
            {
                LogInfo("\tLoading Item"+i);
                GameObject itemPrefab = assetsBundle.LoadAsset<GameObject>("Item"+i);
                itemPrefab.name = defaultItemsData["ItemDefaults"][i]["DefaultPhysicalObject"]["DefaultObjectWrapper"]["DisplayName"].ToString();

                itemPrefabs.Add(itemPrefab);
                itemNames.Add(i.ToString(), itemPrefab.name);

                Sprite itemIcon = assetsBundle.LoadAsset<Sprite>("Item"+i+"_Icon");
                itemIcons.Add(i.ToString(), itemIcon);

                int itemType = ((int)defaultItemsData["ItemDefaults"][i]["ItemType"]);

                // Create an FVRPhysicalObject and FVRObject to fill with the item's default data
                FVRPhysicalObject itemPhysicalObject = null;
                if (itemType == 8) // Ammo box needs to be magazine
                {
                    itemPhysicalObject = itemPrefab.AddComponent<FVRFireArmMagazine>();
                }
                else if(itemType == 11)
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
                itemPhysicalObject.QBPoseOverride = itemPrefab.transform.GetChild(2);
                itemPhysicalObject.UseGrabPointChild = true; // Makes sure the item will be held where the player grabs it instead of at pose override

                JToken defaultObjectWrapper = defaultPhysicalObject["DefaultObjectWrapper"];
                itemObjectWrapper.ItemID = i.ToString();
                itemObjectWrapper.DisplayName = defaultObjectWrapper["DisplayName"].ToString();
                itemObjectWrapper.Category = (FVRObject.ObjectCategory)((int)defaultObjectWrapper["Category"]);
                itemObjectWrapper.Mass = itemPrefab.GetComponent<Rigidbody>().mass;
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
                customItemWrapper.volumes = defaultItemsData["ItemDefaults"][i]["Volumes"].ToObject<float[]>();
                customItemWrapper.parents = defaultItemsData["ItemDefaults"][i]["parents"] != null ? defaultItemsData["ItemDefaults"][i]["parents"].ToObject<List<String>>() : new List<string>();
                customItemWrapper.itemName = itemObjectWrapper.DisplayName;
                customItemWrapper.description = defaultItemsData["ItemDefaults"][i]["description"] != null ? defaultItemsData["ItemDefaults"][i]["description"].ToString() : "";
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
                }
                if(defaultItemsData["ItemDefaults"][i]["BlocksEarpiece"] != null)
                {
                    customItemWrapper.blocksEarpiece = (bool)defaultItemsData["ItemDefaults"][i]["BlocksEarpiece"];
                    customItemWrapper.blocksEyewear = (bool)defaultItemsData["ItemDefaults"][i]["BlocksEyewear"];
                    customItemWrapper.blocksFaceCover = (bool)defaultItemsData["ItemDefaults"][i]["BlocksFaceCover"];
                    customItemWrapper.blocksHeadwear = (bool)defaultItemsData["ItemDefaults"][i]["BlocksHeadwear"];
                }

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
                foreach(Transform collider in itemPrefab.transform.GetChild(0).GetChild(itemPrefab.transform.GetChild(0).childCount - 1))
                {
                    colliders.Add(collider.GetComponent<Collider>());
                }
                customItemWrapper.colliders = colliders.ToArray();

                // Add an EFM_OtherInteractable to each child under Interactives recursively and add them to interactiveSets
                List<GameObject> interactiveSets = new List<GameObject>();
                foreach(Transform interactive in itemPrefab.transform.GetChild(3))
                {
                    interactiveSets.Add(MakeItemInteractiveSet(interactive, itemPhysicalObject));
                }
                customItemWrapper.interactiveSets = interactiveSets.ToArray();

                // Fill customItemWrapper models
                List<GameObject> models = new List<GameObject>();
                foreach(Transform model in itemPrefab.transform.GetChild(0))
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
                    GameObject quickBeltConfiguration = assetsBundle.LoadAsset<GameObject>("Item"+i+"Configuration");
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
                        if(slotShape == FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear)
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
                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.configurationRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2);

                    customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride;
                    customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride.GetChild(0);
                }

                // Backpack
                if (itemType == 5)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
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
                    customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride;
                    if (customItemWrapper.rightHandPoseOverride != null)
                    {
                        customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride.GetChild(0);
                    }

                    // Set backpack settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (float)defaultItemsData["ItemDefaults"][i]["BackpackProperties"]["MaxVolume"];

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Container
                if(itemType == 6)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
                    Renderer[] mainContainerRenderer = new Renderer[1];
                    mainContainerRenderer[0] = mainContainer.GetComponent<Renderer>();
                    mainContainerRenderer[0].material = quickSlotConstantMaterial;
                    customItemWrapper.mainContainerRenderers = mainContainerRenderer;

                    // Set container settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (float)defaultItemsData["ItemDefaults"][i]["ContainerProperties"]["MaxVolume"];

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Pouch
                if(itemType == 7)
                {
                    // Set MainContainer renderers and their material
                    GameObject mainContainer = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2).gameObject;
                    customItemWrapper.mainContainer = mainContainer;
                    Renderer[] mainContainerRenderer = new Renderer[1];
                    mainContainerRenderer[0] = mainContainer.GetComponent<Renderer>();
                    mainContainerRenderer[0].material = quickSlotConstantMaterial;
                    customItemWrapper.mainContainerRenderers = mainContainerRenderer;

                    // Set pouch settings
                    customItemWrapper.volumeIndicatorText = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).GetComponentInChildren<Text>();
                    customItemWrapper.volumeIndicator = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 3).gameObject;
                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (float)defaultItemsData["ItemDefaults"][i]["ContainerProperties"]["MaxVolume"];

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Ammobox
                if(itemType == 8)
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
                        fireArmMagazine.PoseOverride_Touch = fireArmMagazine.PoseOverride;
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
                        fireArmMagazine.PoseOverride_Touch = fireArmMagazine.PoseOverride;
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
                    for(int j=0; j < 3; ++j)
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
                    if (defaultItemsData["ItemDefaults"][i]["effects_damage"] != null)
                    {
                        // Damage effects
                        Dictionary<string, JToken> damageEffects = defaultItemsData["ItemDefaults"][i]["effects_damage"].ToObject<Dictionary<string, JToken>>();
                        foreach (KeyValuePair<string, JToken> damageEntry in damageEffects)
                        {
                            EFM_Effect_Consumable consumableEffect = new EFM_Effect_Consumable();
                            customItemWrapper.consumeEffects.Add(consumableEffect);
                            consumableEffect.delay = (float)damageEntry.Value["delay"];
                            consumableEffect.duration = (float)damageEntry.Value["duration"];
                            if(damageEntry.Value["cost"] != null)
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
                                    consumableEffect.healthPenaltyMax = (float)damageEntry.Value["healthPenaltyMax"];
                                    consumableEffect.healthPenaltyMin = (float)damageEntry.Value["healthPenaltyMin"];
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
                        JArray buffs = (JArray)globalDB["config"]["Health"]["Stimulator"]["Buffs"][defaultItemsData["ItemDefaults"][i]["stimulatorBuffs"].ToString()];
                        foreach(JToken buff in buffs)
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
            }

            SetVanillaItems();

            // Add custom quick belt configs we made to GM's array of quick belt configurations
            List<GameObject> customQuickBeltConfigurations = new List<GameObject>();
            customQuickBeltConfigurations.AddRange(ManagerSingleton<GM>.Instance.QuickbeltConfigurations);
            firstCustomConfigIndex = customQuickBeltConfigurations.Count;
            customQuickBeltConfigurations.AddRange(rigConfigurations);
            ManagerSingleton<GM>.Instance.QuickbeltConfigurations = customQuickBeltConfigurations.ToArray();
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
                        customItemWrapper.whiteList.Add(itemMap[whiteListElement.ToString()]);
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
                        customItemWrapper.blackList.Add(itemMap[blackListElement.ToString()]);
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
            areasDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/areas.json"));
            localDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/local.json"));
            ParseItemMap();
            traderBaseDB = new JObject[8];
            traderAssortDB = new JObject[8];
            traderTasksDB = new JObject[8];
            traderCategoriesDB = new JArray[8];
            for (int i=0; i < 8; ++i)
            {
                string traderID = EFM_TraderStatus.IndexToID(i);
                traderBaseDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/traders/"+traderID+"/base.json"));
                traderAssortDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/traders/"+traderID+"/assort.json"));
                traderTasksDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/traders/"+traderID+"/questassort.json"));
                traderCategoriesDB[i] = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/traders/"+traderID+"/categories.json"));
            }
            globalDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/globals.json"));
            questDB = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/quests.json"));
            XPPerLevel = (JArray)globalDB["config"]["exp"]["level"]["exp_table"];
            mapData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/EscapeFromMeatovMapData.txt"));
            locationsDB = new JObject[9];
            string[] locationFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov/Locations/");
            for (int i = 0; i < locationFiles.Length; ++i)
            {
                locationsDB[i] = JObject.Parse(File.ReadAllText(locationFiles[i]));
            }
            lootContainerDB = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/LootContainer.json"));
            lootContainersByName = new Dictionary<string, JObject>();
            foreach(JToken container in lootContainerDB)
            {
                lootContainersByName.Add(container["_name"].ToString(), (JObject)container);
            }
        }

        private void SetVanillaItems()
        {
            vanillaItems = new Dictionary<string, EFM_VanillaItemDescriptor>();
            JArray vanillaItemsRaw = (JArray)defaultItemsData["VanillaItems"];

            foreach(JToken vanillaItemRaw in vanillaItemsRaw)
            {
                string H3ID = vanillaItemRaw["H3ID"].ToString();
                GameObject itemPrefab = IM.OD[H3ID].GetGameObject();
                EFM_VanillaItemDescriptor descriptor = itemPrefab.AddComponent<EFM_VanillaItemDescriptor>();
                descriptor.H3ID = vanillaItemRaw["H3ID"].ToString();
                descriptor.tarkovID = vanillaItemRaw["TarkovID"].ToString();
                descriptor.description = vanillaItemRaw["Description"].ToString();
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
                }

                FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
                descriptor.itemName = physObj.ObjectWrapper.DisplayName;
                itemNames.Add(H3ID, descriptor.itemName);
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
                else if(physObj is FVRFireArmMagazine)
                {
                    descriptor.compatibilityValue = 1;
                    descriptor.roundType = (physObj as FVRFireArmMagazine).RoundType;
                }
                else if(physObj is FVRFireArmClip)
                {
                    descriptor.compatibilityValue = 1;
                    descriptor.roundType = (physObj as FVRFireArmClip).RoundType;
                }
                else if(physObj is Speedloader)
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

            // Get all item spawner object defs
            //UnityEngine.Object[] array = Resources.LoadAll("ItemSpawnerDefinitions", typeof(ItemSpawnerObjectDefinition));
            //LogInfo("VANILLA SPRITES COUNT=" + array.Length);
            //foreach (ItemSpawnerObjectDefinition def in array)
            //{
            //    // Check if our vanilla items contain the ItemID of this object
            //    FVRPhysicalObject physObj = def.Prefab.GetComponentInChildren<FVRPhysicalObject>();
            //    if (physObj != null && vanillaItems.ContainsKey(physObj.ObjectWrapper.ItemID) && !itemIcons.ContainsKey(physObj.ObjectWrapper.ItemID))
            //    {
            //        // Add the sprite
            //        itemIcons.Add(physObj.ObjectWrapper.ItemID, def.Sprite);
            //        LogInfo("Added sprite for vanilla ID: " + physObj.ObjectWrapper.ItemID);
            //    }
            //}
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
            itemMap = new Dictionary<string, string>();

            try
            {
                string[] lines = System.IO.File.ReadAllLines("BepinEx/Plugins/EscapeFromMeatov/itemMap.txt");

                foreach (string line in lines)
                {
                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    string trimmedLine = line.Trim();
                    string[] tokens = trimmedLine.Split(':');

                    if (tokens.Length == 0)
                    {
                        continue;
                    }

                    if (itemMap.ContainsKey(tokens[0]))
                    {
                        Logger.LogError("Key: " + tokens[0] + ":"+tokens[1]+" already exists in itemMap!");
                    }
                    else
                    {
                        itemMap.Add(tokens[0], tokens[1]);
                    }
                }
            }
            catch (FileNotFoundException ex) { Logger.LogInfo("Couldn't find itemMap.txt. Error: " + ex.Message); }
            catch (Exception ex) { Logger.LogInfo("Couldn't read itemMap.txt. Error: " + ex.Message); }
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
            foreach(EFM_EquipmentSlot equipSlot in equipmentSlots)
            {
                if (equipSlot.CurObject != null)
                {
                    AddToPlayerInventory(equipSlot.CurObject.transform);
                }
            }

            // Check quickbelt slots
            foreach (FVRQuickBeltSlot beltSlot in GM.CurrentPlayerBody.QuickbeltSlots)
            {
                if (beltSlot.CurObject != null)
                {
                    AddToPlayerInventory(beltSlot.CurObject.transform);
                }
            }

            // Check hands
            EFM_Hand leftHand = GM.CurrentPlayerBody.LeftHand.GetComponent<EFM_Hand>();
            if (leftHand.currentHeldItem != null)
            {
                AddToPlayerInventory(leftHand.currentHeldItem.transform);
            }
            if (leftHand.otherHand.currentHeldItem != null)
            {
                AddToPlayerInventory(leftHand.otherHand.currentHeldItem.transform);
            }
        }

        public static void AddToPlayerInventory(Transform item)
        {
            EFM_CustomItemWrapper customItemWrapper = item.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor vanillaItemDescriptor = item.GetComponent<EFM_VanillaItemDescriptor>();
            string itemID = item.GetComponent<FVRPhysicalObject>().ObjectWrapper.ItemID;
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

            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == ItemType.AmmoBox)
                {
                    FVRFireArmMagazine boxMagazine = customItemWrapper.GetComponent<FVRFireArmMagazine>();
                    foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                    {
                        string roundName = AM.STypeDic[boxMagazine.RoundType][loadedRound.LR_Class].Name;

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
                            magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
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
                            clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
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

            // Check for more items that may be contained inside this one
            if (customItemWrapper != null && customItemWrapper.itemObjectsRoot != null)
            {
                foreach (Transform innerItem in customItemWrapper.itemObjectsRoot)
                {
                    AddToPlayerInventory(innerItem);
                }
            }
        }

        public static void AddExperience(int xp)
        {
            experience += xp;
            int XPForNextLevel = (int)XPPerLevel[level]["exp"]; // XP for current level would be at level - 1
            while (experience >= XPForNextLevel)
            {
                ++level;
                experience -= XPForNextLevel; 
                XPForNextLevel = (int)XPPerLevel[level]["exp"];
            }

            // Update UI
            playerStatusManager.UpdatePlayerLevel();
            if (currentLocationIndex == 1) // In hideout
            {
                currentBaseManager.UpdateBasedOnPlayerLevel();
            }
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
            MethodInfo magRemoveRoundPatchOriginal = typeof(FVRFireArmMagazine).GetMethod("RemoveRound", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Any, new Type[] {}, null);
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

            harmony.Patch(fireArmEjectMagPatchOriginal, new HarmonyMethod(fireArmEjectMagPatchPrefix));

            // FireArmLoadClipPatch
            MethodInfo fireArmLoadClipPatchOriginal = typeof(FVRFireArm).GetMethod("LoadClip", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmLoadClipPatchPrefix = typeof(FireArmLoadClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmLoadClipPatchOriginal, new HarmonyMethod(fireArmLoadClipPatchPrefix));

            // FireArmEjectClipPatch
            MethodInfo fireArmEjectClipPatchOriginal = typeof(FVRFireArm).GetMethod("EjectClip", BindingFlags.Public | BindingFlags.Instance);
            MethodInfo fireArmEjectClipPatchPrefix = typeof(FireArmEjectClipPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            harmony.Patch(fireArmEjectClipPatchOriginal, new HarmonyMethod(fireArmEjectClipPatchPrefix));

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

            //// DeadBoltPatch
            //MethodInfo deadBoltPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("TurnBolt", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltPatchPrefix = typeof(DeadBoltPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltPatchOriginal, new HarmonyMethod(deadBoltPatchPrefix));

            //// DeadBoltLastHandPatch
            //MethodInfo deadBoltLastHandPatchOriginal = typeof(SideHingedDestructibleDoorDeadBolt).GetMethod("SetStartingLastHandForward", BindingFlags.Public | BindingFlags.Instance);
            //MethodInfo deadBoltLastHandPatchPrefix = typeof(DeadBoltLastHandPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(deadBoltLastHandPatchOriginal, new HarmonyMethod(deadBoltLastHandPatchPrefix));

            //// DequeueAndPlayDebugPatch
            //MethodInfo dequeueAndPlayDebugPatchOriginal = typeof(AudioSourcePool).GetMethod("DequeueAndPlay", BindingFlags.NonPublic | BindingFlags.Instance);
            //MethodInfo dequeueAndPlayDebugPatchPrefix = typeof(DequeueAndPlayDebugPatch).GetMethod("Prefix", BindingFlags.NonPublic | BindingFlags.Static);

            //harmony.Patch(dequeueAndPlayDebugPatchOriginal, new HarmonyMethod(dequeueAndPlayDebugPatchPrefix));

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
            activeDescriptions = new List<EFM_DescriptionManager>();
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
            sceneDef = new MainMenuSceneDef();
            sceneDef.name = "MeatovSceneScreen";
            sceneDef.SceneName = "MeatovMenuScene";
            sceneDef.Name = "Escape from Meatov";
            sceneDef.Desciption = "Enter Meatov, loot, attempt escape. Upgrade your base, complete quests, trade, and go again. Good luck.";
            sceneDef.Image = sceneDefImage;
            sceneDef.Type = "Escape";
        }

        public void OnSceneLoadedVR(bool loading)
        {
            if (!loading && isGrillhouse && grillHouseSecure)
            {
                isGrillhouse = false;
                Mod.instance.LogInfo("proxying through grillhouse");
                LoadLevelBeginPatch.secureObjects();
                grillHouseSecure = false;
                SteamVR_LoadLevel.Begin("MeatovMenuScene", false, 0.5f, 0f, 0f, 0f, 1f);
            }
        }

        public void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            Logger.LogInfo("OnSceneLoaded called with scene: "+loadedScene.name);
            if(loadedScene.name.Equals("MainMenu3"))
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
            }
            else if (loadedScene.name.Equals("MeatovFactoryScene") /*|| other raid scenes*/)
            {
                inMeatovScene = true;
                Mod.currentBaseManager = null;
                UnsecureObjects();

                GameObject raidRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];

                EFM_Raid_Manager raidManager = raidRoot.AddComponent<EFM_Raid_Manager>();
                raidManager.Init();

                GM.CurrentMovementManager.TeleportToPoint(raidManager.spawnPoint.position, true, raidManager.spawnPoint.rotation.eulerAngles);
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
            GameObject currentPointable = Instantiate<GameObject>(mainMenuPointable); // TODO: This does not exist yet anymore because we made it so assets are all loaded when we load meatov menu. So we have to make a specific asset bundle just to have the H3VR main menu button assets taht we can load at game start
            currentPointable.name = mainMenuPointable.name;
            MainMenuScenePointable pointableInstance = currentPointable.AddComponent<MainMenuScenePointable>();
            pointableInstance.Def = sceneDef;
            pointableInstance.Screen = GameObject.Find("LevelLoadScreen").GetComponent<MainMenuScreen>();
            pointableInstance.MaxPointingRange = 16;
            currentPointable.transform.position = new Vector3(-12.14f, 9.5f, 4.88f);
            currentPointable.transform.rotation = Quaternion.Euler(0, 300, 0);

            // Set LOD bias to default
            QualitySettings.lodBias = 2;
        }

        private void LoadMeatov()
        {
            // Set LOD bias
            QualitySettings.lodBias = 5;

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
            if(securedObjects == null)
            {
                securedObjects = new List<GameObject>();
                return;
            }

            foreach (GameObject go in securedObjects)
            {
                SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            }
            securedObjects.Clear();
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
                    Mod.instance.LogError("SkillNameToIndex received name: "+name);
                    return 0;
            }
        }
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

                    secureObjects();
                }
            }
            else if (levelName.Equals("MeatovHideoutScene") ||
                     levelName.Equals("MeatovFactoryScene") /*|| TODO: other raid scenes*/)
            {
                secureObjects();
            }
        }

        public static void secureObjects()
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
        static void Postfix(FVRViveHand hand, ref FVRInteractiveObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
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

            hand.GetComponent<EFM_Hand>().currentHeldItem = null;

            // Stop here if dropping in a quick belt slot or if this is a door
            if ((__instance is FVRPhysicalObject && (__instance as FVRPhysicalObject).QuickbeltSlot != null))
            {
                Mod.instance.LogInfo("Dropping item in qs");

                // Check if area slot
                if ((__instance as FVRPhysicalObject).QuickbeltSlot is EFM_AreaSlot)
                {
                    EFM_CustomItemWrapper heldCustomItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor heldVanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                    BeginInteractionPatch.SetItemLocationIndex(3, null, heldVanillaItemDescriptor);
                    if (heldCustomItemWrapper != null)
                    {
                        // Was on player
                        Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                        if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                        {
                            Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                        }
                        Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                        if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                        {
                            Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                        }
                    }
                    else if(heldVanillaItemDescriptor != null)
                    {
                        // Was on player
                        Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                        if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                        {
                            Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                        }
                        Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                        if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                        {
                            Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                        }
                    }
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
                                Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                                if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                                {
                                    Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                                }
                                Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                                if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                                {
                                    Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                                }

                                // Now in hideout
                                if (Mod.baseInventory.ContainsKey(heldCustomItemWrapper.ID))
                                {
                                    Mod.baseInventory[heldCustomItemWrapper.ID] += heldCustomItemWrapper.stack;
                                }
                                else
                                {
                                    Mod.baseInventory.Add(heldCustomItemWrapper.ID, heldCustomItemWrapper.stack);
                                }
                                if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldCustomItemWrapper.ID))
                                {
                                    Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                                }
                                else
                                {
                                    Mod.currentBaseManager.baseInventoryObjects.Add(heldCustomItemWrapper.ID, new List<GameObject>());
                                    Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                                }
                            }
                            else if (customItemWrapper.locationIndex == 2)
                            {
                                // Was on player
                                Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                                if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                                {
                                    Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                                }
                                Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                                if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                                {
                                    Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                                }
                            }
                        }
                        else if (heldVanillaItemDescriptor)
                        {
                            BeginInteractionPatch.SetItemLocationIndex(customItemWrapper.locationIndex, null, heldVanillaItemDescriptor);

                            // Update lists
                            if (customItemWrapper.locationIndex == 1)
                            {
                                // Was on player
                                Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                                if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                                {
                                    Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                                }
                                Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                                if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                                {
                                    Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                                }

                                // Now in hideout
                                if (Mod.baseInventory.ContainsKey(heldVanillaItemDescriptor.H3ID))
                                {
                                    Mod.baseInventory[heldVanillaItemDescriptor.H3ID] += 1;
                                }
                                else
                                {
                                    Mod.baseInventory.Add(heldVanillaItemDescriptor.H3ID, 1);
                                }
                                if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldVanillaItemDescriptor.H3ID))
                                {
                                    Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                                }
                                else
                                {
                                    Mod.currentBaseManager.baseInventoryObjects.Add(heldVanillaItemDescriptor.H3ID, new List<GameObject>());
                                    Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                                }
                            }
                            else if (customItemWrapper.locationIndex == 2)
                            {
                                // Was on player
                                Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                                if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                                {
                                    Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                                }
                                Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                                if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                                {
                                    Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                                }

                                // MagazinesByType/ClipsbyType
                                if (__instance is FVRFireArmMagazine)
                                {
                                    FVRFireArmMagazine asMag = __instance as FVRFireArmMagazine;
                                    Mod.magazinesByType[asMag.MagazineType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] -= 1;
                                    if (Mod.magazinesByType[asMag.MagazineType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] == 0)
                                    {
                                        Mod.magazinesByType[asMag.MagazineType].Remove((__instance as FVRPhysicalObject).ObjectWrapper.DisplayName);

                                        if (Mod.magazinesByType[asMag.MagazineType].Count == 0)
                                        {
                                            Mod.magazinesByType.Remove(asMag.MagazineType);
                                        }
                                    }

                                    foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                    {
                                        descManager.SetDescriptionPack();
                                    }
                                }
                                else if (__instance is FVRFireArmClip)
                                {
                                    FVRFireArmClip asClip = __instance as FVRFireArmClip;
                                    Mod.clipsByType[asClip.ClipType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] -= 1;
                                    if (Mod.clipsByType[asClip.ClipType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] == 0)
                                    {
                                        Mod.clipsByType[asClip.ClipType].Remove((__instance as FVRPhysicalObject).ObjectWrapper.DisplayName);

                                        if (Mod.clipsByType[asClip.ClipType].Count == 0)
                                        {
                                            Mod.clipsByType.Remove(asClip.ClipType);
                                        }
                                    }

                                    foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                    {
                                        descManager.SetDescriptionPack();
                                    }
                                }
                                else if (__instance is FVRFireArmRound)
                                {
                                    FVRFireArmRound asRound = __instance as FVRFireArmRound;
                                    Mod.roundsByType[asRound.RoundType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] -= 1;
                                    if (Mod.roundsByType[asRound.RoundType][(__instance as FVRPhysicalObject).ObjectWrapper.DisplayName] == 0)
                                    {
                                        Mod.roundsByType[asRound.RoundType].Remove((__instance as FVRPhysicalObject).ObjectWrapper.DisplayName);

                                        if (Mod.roundsByType[asRound.RoundType].Count == 0)
                                        {
                                            Mod.roundsByType.Remove(asRound.RoundType);
                                        }
                                    }

                                    foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                    {
                                        descManager.SetDescriptionPack();
                                    }
                                }
                            }
                        }
                        return;
                    }
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
            Mod.instance.LogInfo("Dropped Item " + primary.name);
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
            if (collidingContainerWrapper != null && (heldCustomItemWrapper == null || !heldCustomItemWrapper.Equals(collidingContainerWrapper)))
            {
                Mod.instance.LogInfo("\tChecking if item fits in container");
                // Check if item fits in container
                if (collidingContainerWrapper.AddItemToContainer(primary))
                {
                    if(collidingContainerWrapper.locationIndex == 1)
                    {
                        BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor);

                        if(heldCustomItemWrapper != null)
                        {
                            collidingContainerWrapper.currentWeight += heldCustomItemWrapper.currentWeight;

                            // Was on player
                            Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                            if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                            {
                                Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                            }
                            Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                            if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                            {
                                Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                            }

                            // Now in hideout
                            if (Mod.baseInventory.ContainsKey(heldCustomItemWrapper.ID))
                            {
                                Mod.baseInventory[heldCustomItemWrapper.ID] += heldCustomItemWrapper.stack;
                            }
                            else
                            {
                                Mod.baseInventory.Add(heldCustomItemWrapper.ID, heldCustomItemWrapper.stack);
                            }
                            if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldCustomItemWrapper.ID))
                            {
                                Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                            }
                            else
                            {
                                Mod.currentBaseManager.baseInventoryObjects.Add(heldCustomItemWrapper.ID, new List<GameObject>());
                                Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                            }
                        }
                        else
                        {
                            collidingContainerWrapper.currentWeight += heldVanillaItemDescriptor.currentWeight;

                            // Was on player
                            Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                            if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                            {
                                Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                            }
                            Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                            if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                            {
                                Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                            }

                            // Now in hideout
                            if (Mod.baseInventory.ContainsKey(heldVanillaItemDescriptor.H3ID))
                            {
                                Mod.baseInventory[heldVanillaItemDescriptor.H3ID] += 1;
                            }
                            else
                            {
                                Mod.baseInventory.Add(heldVanillaItemDescriptor.H3ID, 1);
                            }
                            if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldVanillaItemDescriptor.H3ID))
                            {
                                Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                            }
                            else
                            {
                                Mod.currentBaseManager.baseInventoryObjects.Add(heldVanillaItemDescriptor.H3ID, new List<GameObject>());
                                Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                            }
                        }
                    }
                    else if(collidingContainerWrapper.locationIndex == 2)
                    {
                        BeginInteractionPatch.SetItemLocationIndex(2, heldCustomItemWrapper, heldVanillaItemDescriptor);

                        if (heldCustomItemWrapper != null)
                        {
                            collidingContainerWrapper.currentWeight += heldCustomItemWrapper.currentWeight;

                            // Was on player
                            Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                            if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                            {
                                Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                            }
                            Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                            if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                            {
                                Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                            }
                        }
                        else
                        {
                            collidingContainerWrapper.currentWeight += heldVanillaItemDescriptor.currentWeight;

                            // Was on player
                            Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                            if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                            {
                                Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                            }
                            Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                            if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                            {
                                Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                            }

                            // MagazinesByType/ClipsbyType
                            if (primary is FVRFireArmMagazine)
                            {
                                FVRFireArmMagazine asMag = primary as FVRFireArmMagazine;
                                Mod.magazinesByType[asMag.MagazineType][primary.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.magazinesByType[asMag.MagazineType][primary.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.magazinesByType[asMag.MagazineType].Remove(primary.ObjectWrapper.DisplayName);

                                    if (Mod.magazinesByType[asMag.MagazineType].Count == 0)
                                    {
                                        Mod.magazinesByType.Remove(asMag.MagazineType);
                                    }
                                }

                                foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                {
                                    descManager.SetDescriptionPack();
                                }
                            }
                            else if (primary is FVRFireArmClip)
                            {
                                FVRFireArmClip asClip = primary as FVRFireArmClip;
                                Mod.clipsByType[asClip.ClipType][primary.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.clipsByType[asClip.ClipType][primary.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.clipsByType[asClip.ClipType].Remove(primary.ObjectWrapper.DisplayName);

                                    if (Mod.clipsByType[asClip.ClipType].Count == 0)
                                    {
                                        Mod.clipsByType.Remove(asClip.ClipType);
                                    }
                                }

                                foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                {
                                    descManager.SetDescriptionPack();
                                }
                            }
                            else if (primary is FVRFireArmRound)
                            {
                                FVRFireArmRound asRound = primary as FVRFireArmRound;
                                Mod.roundsByType[asRound.RoundType][primary.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.roundsByType[asRound.RoundType][primary.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.roundsByType[asRound.RoundType].Remove(primary.ObjectWrapper.DisplayName);

                                    if (Mod.roundsByType[asRound.RoundType].Count == 0)
                                    {
                                        Mod.roundsByType.Remove(asRound.RoundType);
                                    }
                                }

                                foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                                {
                                    descManager.SetDescriptionPack();
                                }
                            }
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
                collidingTradeVolume.AddItem(primary);

                collidingTradeVolume.market.UpdateBasedOnItem(false, heldCustomItemWrapper, heldVanillaItemDescriptor);

                BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor);

                if (heldCustomItemWrapper != null)
                {
                    // Was on player
                    Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                    if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                    }
                    Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                    if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                    }

                    // Now in hideout
                    if (Mod.baseInventory.ContainsKey(heldCustomItemWrapper.ID))
                    {
                        Mod.baseInventory[heldCustomItemWrapper.ID] += heldCustomItemWrapper.stack;
                    }
                    else
                    {
                        Mod.baseInventory.Add(heldCustomItemWrapper.ID, heldCustomItemWrapper.stack);
                    }
                    if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldCustomItemWrapper.ID))
                    {
                        Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                    }
                    else
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Add(heldCustomItemWrapper.ID, new List<GameObject>());
                        Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                    }
                }
                else
                {
                    // Was on player
                    Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                    if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                    }
                    Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                    if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                    }

                    // Now in hideout
                    if (Mod.baseInventory.ContainsKey(heldVanillaItemDescriptor.H3ID))
                    {
                        Mod.baseInventory[heldVanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.baseInventory.Add(heldVanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldVanillaItemDescriptor.H3ID))
                    {
                        Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Add(heldVanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                    }
                }
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
                BeginInteractionPatch.SetItemLocationIndex(1, heldCustomItemWrapper, heldVanillaItemDescriptor);

                // Update lists
                if (heldCustomItemWrapper != null)
                {
                    // Was on player
                    Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                    if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                    }
                    Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                    if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                    }

                    // Now in hideout
                    if (Mod.baseInventory.ContainsKey(heldCustomItemWrapper.ID))
                    {
                        Mod.baseInventory[heldCustomItemWrapper.ID] += heldCustomItemWrapper.stack;
                    }
                    else
                    {
                        Mod.baseInventory.Add(heldCustomItemWrapper.ID, heldCustomItemWrapper.stack);
                    }
                    if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldCustomItemWrapper.ID))
                    {
                        Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                    }
                    else
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Add(heldCustomItemWrapper.ID, new List<GameObject>());
                        Mod.currentBaseManager.baseInventoryObjects[heldCustomItemWrapper.ID].Add(heldCustomItemWrapper.gameObject);
                    }
                }
                else
                {
                    // Was on player
                    Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                    if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                    }
                    Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                    if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                    }

                    // Now in hideout
                    if (Mod.baseInventory.ContainsKey(heldVanillaItemDescriptor.H3ID))
                    {
                        Mod.baseInventory[heldVanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.baseInventory.Add(heldVanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(heldVanillaItemDescriptor.H3ID))
                    {
                        Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Add(heldVanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.currentBaseManager.baseInventoryObjects[heldVanillaItemDescriptor.H3ID].Add(heldVanillaItemDescriptor.gameObject);
                    }
                }

                primary.SetParentage(sceneRoot.transform.GetChild(2));
            }
            else if (raidManager != null)
            {
                BeginInteractionPatch.SetItemLocationIndex(2, heldCustomItemWrapper, heldVanillaItemDescriptor);

                // Update lists
                if (heldCustomItemWrapper != null)
                {
                    // Was on player
                    Mod.playerInventory[heldCustomItemWrapper.ID] -= heldCustomItemWrapper.stack;
                    if (Mod.playerInventory[heldCustomItemWrapper.ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldCustomItemWrapper.ID);
                    }
                    Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Remove(heldCustomItemWrapper.gameObject);
                    if (Mod.playerInventoryObjects[heldCustomItemWrapper.ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldCustomItemWrapper.ID);
                    }
                }
                else
                {
                    // Was on player
                    Mod.playerInventory[heldVanillaItemDescriptor.H3ID] -= 1;
                    if (Mod.playerInventory[heldVanillaItemDescriptor.H3ID] == 0)
                    {
                        Mod.playerInventory.Remove(heldVanillaItemDescriptor.H3ID);
                    }
                    Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Remove(heldVanillaItemDescriptor.gameObject);
                    if (Mod.playerInventoryObjects[heldVanillaItemDescriptor.H3ID].Count == 0)
                    {
                        Mod.playerInventoryObjects.Remove(heldVanillaItemDescriptor.H3ID);
                    }

                    // MagazinesByType/ClipsbyType
                    if (primary is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMag = primary as FVRFireArmMagazine;
                        Mod.magazinesByType[asMag.MagazineType][primary.ObjectWrapper.DisplayName] -= 1;
                        if (Mod.magazinesByType[asMag.MagazineType][primary.ObjectWrapper.DisplayName] == 0)
                        {
                            Mod.magazinesByType[asMag.MagazineType].Remove(primary.ObjectWrapper.DisplayName);

                            if (Mod.magazinesByType[asMag.MagazineType].Count == 0)
                            {
                                Mod.magazinesByType.Remove(asMag.MagazineType);
                            }
                        }

                        foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }
                    else if (primary is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = primary as FVRFireArmClip;
                        Mod.clipsByType[asClip.ClipType][primary.ObjectWrapper.DisplayName] -= 1;
                        if (Mod.clipsByType[asClip.ClipType][primary.ObjectWrapper.DisplayName] == 0)
                        {
                            Mod.clipsByType[asClip.ClipType].Remove(primary.ObjectWrapper.DisplayName);

                            if (Mod.clipsByType[asClip.ClipType].Count == 0)
                            {
                                Mod.clipsByType.Remove(asClip.ClipType);
                            }
                        }

                        foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }
                    else if (primary is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = primary as FVRFireArmRound;
                        Mod.roundsByType[asRound.RoundType][primary.ObjectWrapper.DisplayName] -= 1;
                        if (Mod.roundsByType[asRound.RoundType][primary.ObjectWrapper.DisplayName] == 0)
                        {
                            Mod.roundsByType[asRound.RoundType].Remove(primary.ObjectWrapper.DisplayName);

                            if (Mod.roundsByType[asRound.RoundType].Count == 0)
                            {
                                Mod.roundsByType.Remove(asRound.RoundType);
                            }
                        }

                        foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }
                }
                
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
                if (__instance.QuickbeltSlots.Count > 4)
                {
                    for (int i = __instance.QuickbeltSlots.Count - 1; i >= 4; --i)
                    {
                        if (__instance.QuickbeltSlots[i] == null)
                        {
                            __instance.QuickbeltSlots.RemoveAt(i);
                        }
                        else if (__instance.QuickbeltSlots[i].IsPlayer)
                        {
                            // Index -2 or -3 will destroy objects associated to the slots
                            if ((index == -2 || index == -3) && __instance.QuickbeltSlots[i].CurObject != null)
                            {
                                GameObject.Destroy(__instance.QuickbeltSlots[i].CurObject.gameObject);
                                __instance.QuickbeltSlots[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QuickbeltSlots[i].gameObject);
                            __instance.QuickbeltSlots.RemoveAt(i);
                        }
                    }
                }

                // If -3 also destroy objects in pockets, but dont get rid of the slots themselves
                if(index == -3)
                {
                    for (int i = 3; i >= 0; --i)
                    {
                        if (__instance.QuickbeltSlots[i].IsPlayer && __instance.QuickbeltSlots[i].CurObject != null)
                        {
                            GameObject.Destroy(__instance.QuickbeltSlots[i].CurObject.gameObject);
                            __instance.QuickbeltSlots[i].CurObject.ClearQuickbeltState();
                        }
                    }
                }

                Mod.currentQuickBeltConfiguration = Mod.pocketsConfigIndex;
            }
            else if(index > Mod.pocketsConfigIndex) // If index is higher than the pockets configuration index, we must keep the pocket slots intact
            {
                // Only check for slots other than pockets
                if (__instance.QuickbeltSlots.Count > /* 0 */ 4)
                {
                    for (int i = __instance.QuickbeltSlots.Count - 1; i >= /* 0 */ 4; i--)
                    {
                        if (__instance.QuickbeltSlots[i] == null)
                        {
                            __instance.QuickbeltSlots.RemoveAt(i);
                        }
                        else if (__instance.QuickbeltSlots[i].IsPlayer)
                        {
                            if (__instance.QuickbeltSlots[i].CurObject != null)
                            {
                                __instance.QuickbeltSlots[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QuickbeltSlots[i].gameObject);
                            __instance.QuickbeltSlots.RemoveAt(i);
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
                            __instance.QuickbeltSlots.Add(component);
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
                for (int j = 0; j < __instance.QuickbeltSlots.Count; j++)
                {
                    if (__instance.QuickbeltSlots[j].IsPlayer)
                    {
                        __instance.QuickbeltSlots[j].transform.SetParent(__instance.Torso);
                        __instance.QuickbeltSlots[j].QuickbeltRoot = null;
                        if (GM.Options.QuickbeltOptions.QuickbeltHandedness > 0)
                        {
                            __instance.QuickbeltSlots[j].transform.localPosition = new Vector3(-__instance.QuickbeltSlots[j].transform.localPosition.x, __instance.QuickbeltSlots[j].transform.localPosition.y, __instance.QuickbeltSlots[j].transform.localPosition.z);
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
                    Mod.pocketSlots[i] = __instance.QuickbeltSlots[i];
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
            for (int i = 0; i < GM.CurrentPlayerBody.QuickbeltSlots.Count; i++)
            {
                if (GM.CurrentPlayerBody.QuickbeltSlots[i].IsPointInsideMe(position))
                {
                    fvrquickBeltSlot = GM.CurrentPlayerBody.QuickbeltSlots[i];
                    break;
                }
            }

            // Check equip slots
            int equipmentSlotIndex = -1;
            if (fvrquickBeltSlot == null && Mod.equipmentSlots != null)
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
        static void Prefix(ref FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
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
            }
        }

        static void Postfix(FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (slot == null)
            {
                return;
            }

            // Check if pocket slot
            for(int i=0; i < 4; ++i)
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
                    __instance.gameObject.SetActive(false);
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
            }
            else if (EFM_EquipmentSlot.wearingArmoredRig || EFM_EquipmentSlot.wearingRig) // We are wearing custom quick belt, check if slot is in there, update if it is
            {
                // Find slot index in config
                bool foundSlot = false;
                for (int slotIndex=4; slotIndex < GM.CurrentPlayerBody.QuickbeltSlots.Count; ++slotIndex)
                {
                    if (GM.CurrentPlayerBody.QuickbeltSlots[slotIndex].Equals(slot))
                    {
                        EFM_CustomItemWrapper equipmentItemWrapper = EFM_EquipmentSlot.currentRig;
                        equipmentItemWrapper.itemsInSlots[slotIndex] = __instance.gameObject;

                        // Update rig weight
                        equipmentItemWrapper.currentWeight += __instance.RootRigidbody.mass;

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

                                // Update rig weight
                                customItemWrapper.currentWeight += __instance.RootRigidbody.mass;

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

            hand.GetComponent<EFM_Hand>().currentHeldItem = __instance.gameObject;

            EFM_VanillaItemDescriptor vanillaItemDescriptor = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper customItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
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

                    // If taken out of shoulderStorage, align with hand
                    if (Mod.leftShoulderObject != null && Mod.leftShoulderObject.Equals(customItemWrapper.gameObject))
                    {
                        FVRViveHand.AlignChild(__instance.transform, __instance.PoseOverride, hand.transform);
                    }
                }

                if (!customItemWrapper.looted)
                {
                    customItemWrapper.looted = true;
                    Mod.AddExperience(customItemWrapper.lootExperience);
                }

                // Update lists
                if(customItemWrapper.locationIndex == 1)
                {
                    // Was in hideout
                    Mod.baseInventory[customItemWrapper.ID] -= customItemWrapper.stack;
                    if(Mod.baseInventory[customItemWrapper.ID] == 0)
                    {
                        Mod.baseInventory.Remove(customItemWrapper.ID);
                    }
                    Mod.currentBaseManager.baseInventoryObjects[customItemWrapper.ID].Remove(customItemWrapper.gameObject);
                    if(Mod.currentBaseManager.baseInventoryObjects[customItemWrapper.ID].Count == 0)
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Remove(customItemWrapper.ID);
                    }

                    // Now on player
                    if (Mod.playerInventory.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventory[customItemWrapper.ID] += customItemWrapper.stack;
                    }
                    else
                    {
                        Mod.playerInventory.Add(customItemWrapper.ID, customItemWrapper.stack);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(customItemWrapper.ID, new List<GameObject>());
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null);
                }
                else if(customItemWrapper.locationIndex == 2)
                {
                    // Now on player
                    if (Mod.playerInventory.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventory[customItemWrapper.ID] += customItemWrapper.stack;
                    }
                    else
                    {
                        Mod.playerInventory.Add(customItemWrapper.ID, customItemWrapper.stack);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(customItemWrapper.ID, new List<GameObject>());
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null);
                }
                else if(customItemWrapper.locationIndex == 3)
                {
                    // Now on player
                    if (Mod.playerInventory.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventory[customItemWrapper.ID] += customItemWrapper.stack;
                    }
                    else
                    {
                        Mod.playerInventory.Add(customItemWrapper.ID, customItemWrapper.stack);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(customItemWrapper.ID))
                    {
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(customItemWrapper.ID, new List<GameObject>());
                        Mod.playerInventoryObjects[customItemWrapper.ID].Add(customItemWrapper.gameObject);
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, customItemWrapper, null);

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
                    Mod.AddExperience(vanillaItemDescriptor.lootExperience);
                }

                // Update lists
                if(vanillaItemDescriptor.locationIndex == 1)
                {
                    // Was in hideout
                    Mod.baseInventory[vanillaItemDescriptor.H3ID] -= 1;
                    if (Mod.baseInventory[vanillaItemDescriptor.H3ID] == 0)
                    {
                        Mod.baseInventory.Remove(vanillaItemDescriptor.H3ID);
                    }
                    Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Remove(vanillaItemDescriptor.gameObject);
                    if (Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Count == 0)
                    {
                        Mod.currentBaseManager.baseInventoryObjects.Remove(vanillaItemDescriptor.H3ID);
                    }

                    // Now on player
                    if (Mod.playerInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventory[vanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.playerInventory.Add(vanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor);
                }
                else if(vanillaItemDescriptor.locationIndex == 2)
                {
                    // Now on player
                    if (Mod.playerInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventory[vanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.playerInventory.Add(vanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }

                    // MagazinesByType/ClipsbyType
                    if(__instance is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMag = __instance as FVRFireArmMagazine;
                        if (Mod.magazinesByType.ContainsKey(asMag.MagazineType))
                        {
                            if (Mod.magazinesByType[asMag.MagazineType].ContainsKey(__instance.ObjectWrapper.DisplayName))
                            {
                                Mod.magazinesByType[asMag.MagazineType][__instance.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                Mod.magazinesByType[asMag.MagazineType].Add(__instance.ObjectWrapper.DisplayName, 1);
                            }
                        }
                        else
                        {
                            Mod.magazinesByType.Add(asMag.MagazineType, new Dictionary<string, int>());
                            Mod.magazinesByType[asMag.MagazineType].Add(__instance.ObjectWrapper.DisplayName, 1);
                        }

                        foreach(EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }
                    else if (__instance is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = __instance as FVRFireArmClip;
                        if (Mod.clipsByType.ContainsKey(asClip.ClipType))
                        {
                            if (Mod.clipsByType[asClip.ClipType].ContainsKey(__instance.ObjectWrapper.DisplayName))
                            {
                                Mod.clipsByType[asClip.ClipType][__instance.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                Mod.clipsByType[asClip.ClipType].Add(__instance.ObjectWrapper.DisplayName, 1);
                            }
                        }
                        else
                        {
                            Mod.clipsByType.Add(asClip.ClipType, new Dictionary<string, int>());
                            Mod.clipsByType[asClip.ClipType].Add(__instance.ObjectWrapper.DisplayName, 1);
                        }

                        foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }
                    else if (__instance is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = __instance as FVRFireArmRound;
                        if (Mod.roundsByType.ContainsKey(asRound.RoundType))
                        {
                            if (Mod.roundsByType[asRound.RoundType].ContainsKey(__instance.ObjectWrapper.DisplayName))
                            {
                                Mod.roundsByType[asRound.RoundType][__instance.ObjectWrapper.DisplayName] += 1;
                            }
                            else
                            {
                                Mod.roundsByType[asRound.RoundType].Add(__instance.ObjectWrapper.DisplayName, 1);
                            }
                        }
                        else
                        {
                            Mod.roundsByType.Add(asRound.RoundType, new Dictionary<string, int>());
                            Mod.roundsByType[asRound.RoundType].Add(__instance.ObjectWrapper.DisplayName, 1);
                        }

                        foreach (EFM_DescriptionManager descManager in Mod.activeDescriptions)
                        {
                            descManager.SetDescriptionPack();
                        }
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor);
                }
                else if (vanillaItemDescriptor.locationIndex == 3)
                {
                    // Now on player
                    if (Mod.playerInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventory[vanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.playerInventory.Add(vanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }

                    // Update locationIndex
                    SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    //foreach (EFM_BaseAreaManager baseAreaManager in Mod.currentBaseManager.baseAreaManagers)
                    //{
                    //    baseAreaManager.UpdateBasedOnItem(vanillaItemDescriptor.H3ID);
                    //}
                }
            }

            // Check if in trade volume or container
            EFM_TradeVolume tradeVolume = __instance.transform.parent.GetComponent<EFM_TradeVolume>();
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

                    containerItemWrapper.containingVolume -= customItemWrapper != null ? customItemWrapper.volumes[customItemWrapper.mode]: Mod.sizeVolumes[(int)__instance.Size];

                    // Reset cols of item so that they are non trigger again and can collide with the world and the container
                    for(int i = containerItemWrapper.resetColPairs.Count - 1; i >= 0; --i)
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
                }
            }
        }

        public static void SetItemLocationIndex(int locationIndex, EFM_CustomItemWrapper customItemWrapper, EFM_VanillaItemDescriptor vanillaItemDescriptor)
        {
            if(customItemWrapper != null)
            {
                if(customItemWrapper.locationIndex == 0 && locationIndex != 0)
                {
                    // Taken out of player inventory
                    Mod.weight -= customItemWrapper.currentWeight;
                }
                else if(customItemWrapper.locationIndex != 0 && locationIndex == 0)
                {
                    // Added to player inventory
                    Mod.weight += customItemWrapper.currentWeight;
                }

                customItemWrapper.locationIndex = locationIndex;

                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            SetItemLocationIndex(locationIndex, innerItem.GetComponent<EFM_CustomItemWrapper>(), innerItem.GetComponent<EFM_VanillaItemDescriptor>());
                        }
                    }
                }
                else if (customItemWrapper.itemType == Mod.ItemType.Container || customItemWrapper.itemType == Mod.ItemType.Pouch || customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    foreach (Transform innerItem in customItemWrapper.itemObjectsRoot)
                    {
                        SetItemLocationIndex(locationIndex, innerItem.GetComponent<EFM_CustomItemWrapper>(), innerItem.GetComponent<EFM_VanillaItemDescriptor>());
                    }
                }
            }
            else
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

                vanillaItemDescriptor.locationIndex = locationIndex;

                FVRPhysicalObject physObj = vanillaItemDescriptor.GetComponent<FVRPhysicalObject>();
                if(physObj != null)
                {
                    if(physObj is FVRFireArm)
                    {
                        FVRFireArm asFireArm = (FVRFireArm)physObj;

                        // Ammo container
                        if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                        {
                            asFireArm.Magazine.GetComponent<EFM_VanillaItemDescriptor>().locationIndex = locationIndex;
                        }
                        else if(asFireArm.UsesClips && asFireArm.Clip != null)
                        {
                            asFireArm.Clip.GetComponent<EFM_VanillaItemDescriptor>().locationIndex = locationIndex;
                        }

                        // Attachments
                        if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                        {
                            foreach(FVRFireArmAttachment attachment in asFireArm.Attachments)
                            {
                                SetItemLocationIndex(locationIndex, null, attachment.GetComponent<EFM_VanillaItemDescriptor>());
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
                                SetItemLocationIndex(locationIndex, null, attachment.GetComponent<EFM_VanillaItemDescriptor>());
                            }
                        }
                    }
                }
            }
        }

        static void Postfix()
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (Mod.beginInteractingEquipRig)
            {
                Mod.beginInteractingEquipRig = false;
            }
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

            //if (GM.CurrentPlayerBody.IsDamResist || GM.CurrentPlayerBody.IsDamMult)
            //{
            //    float damageResist = GM.CurrentPlayerBody.GetDamageResist();
            //    if (damageResist <= 0.01f)
            //    {
            //        return false;
            //    }
            //    num2 *= damageResist;
            //}

            // Apply damage resist/multiplier based on equipment and body part
            if(__instance.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
            {
                // Add a headshot damage multiplier
                damage *= Mod.headshotDamageMultiplier;

                // Process damage resist from EFM_EquipmentSlot.CurrentHelmet
                if (EFM_EquipmentSlot.currentHeadwear != null && EFM_EquipmentSlot.currentHeadwear.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentHeadwear.coverage)
                {
                    EFM_EquipmentSlot.currentHeadwear.armor -= damage - damage * EFM_EquipmentSlot.currentHeadwear.damageResist;
                    damage *= EFM_EquipmentSlot.currentHeadwear.damageResist;
                }
            }
            else if(__instance.Type == FVRPlayerHitbox.PlayerHitBoxType.Torso)
            {
                // Process damage resist from EFM_EquipmentSlot.CurrentArmor
                if(EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentArmor.coverage)
                {
                    EFM_EquipmentSlot.currentArmor.armor -= damage - damage * EFM_EquipmentSlot.currentArmor.damageResist;
                    damage *= EFM_EquipmentSlot.currentArmor.damageResist;
                }
            }
            else
            {
                // Add a damage resist because should do less damage when hit to hand than when hit to torso
                damage *= Mod.handDamageResist;
            }

            // Apply damage res/mult base on status effects
            // TODO: implement status effects, like drugs or broken limb

            if (damage > 0.1f && __instance.IsActivated)
            {
                if (__instance.Body.RegisterPlayerHit(damage, false))
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
            if (!Mod.inMeatovScene)
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
        static bool leftTouchWithinDescRange;
        static bool rightTouchWithinDescRange;

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
                if (__instance.IsThisTheRightHand)
                {
                    rightTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                }
                else
                {
                    leftTouchWithinDescRange = __instance.Input.TouchpadAxes.magnitude < 0.3f;
                }
            }
            if (__instance.Input.TouchpadTouchUp || __instance.Input.TouchpadAxes.magnitude >= 0.3f)
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

            // Keep a reference to touchpad touch inputs so we can still use descriptions after touchpad input has been flushed
            bool touchpadTouched = __instance.Input.TouchpadTouched;
            float touchpadAxisMagnitude = __instance.Input.TouchpadAxes.magnitude;
            bool touchpadDown = __instance.Input.TouchpadDown;

            if (__instance.MovementManager.ShouldFlushTouchpad(__instance))
            {
                typeof(FVRViveHand).GetMethod("FlushTouchpadData", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, null);
            }
            bool flag;
            bool flag2;
            bool pressedCenter = false;
            if (__instance.IsInStreamlinedMode)
            {
                flag = __instance.Input.BYButtonDown;
                flag2 = __instance.Input.BYButtonPressed;
            }
            else
            {
                flag = __instance.Input.TouchpadDown;

                // Here check if only touched and within center of touchpad for grab laser input
                flag2 = touchpadTouched && touchpadAxisMagnitude < 0.3f;
                //Mod.instance.LogInfo("Flag2: " + flag2 + " from touched: " + __instance.Input.TouchpadTouched + " and magnitude: " + __instance.Input.TouchpadAxes.magnitude);

                // Check if we started pressing the center of touchpad this frame
                pressedCenter = touchpadDown && touchpadAxisMagnitude < 0.3f;
            }
            if (flag2)
            {
                if (___m_state == FVRViveHand.HandState.GripInteracting)
                {
                    // Only display description if started touching at magnitude < 0.2, and also check if descriptions have been init yet
                    // Because this will also be checked in meatov menu but they havent been init yet at that point
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
                                if (manager.descriptionPack.isCustom)
                                {
                                    describableToUse = manager.descriptionPack.customItem;
                                }
                                else
                                {
                                    describableToUse = manager.descriptionPack.vanillaItem;
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

                            manager.gameObject.SetActive(true);
                        }
                    }
                }
            }
            else
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
            if (pressedCenter && Mod.rightDescriptionManager != null)
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
                // Dont have the grab laser if we didnt start touching the touchpad within magnitude 0.2
                if (flag2 && (__instance.IsThisTheRightHand ? rightTouchWithinDescRange : leftTouchWithinDescRange))
                {
                    if (!__instance.GrabLaser.gameObject.activeSelf)
                    {
                        __instance.GrabLaser.gameObject.SetActive(true);
                    }
                    bool flag3 = false;
                    FVRPhysicalObject fvrphysicalObject = null;
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
                                if (manager.descriptionPack.isCustom)
                                {
                                    describableToUse = manager.descriptionPack.customItem;
                                }
                                else
                                {
                                    describableToUse = manager.descriptionPack.vanillaItem;
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

                            manager.gameObject.SetActive(true);
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
        static int callcount = 0;

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            Mod.instance.LogInfo("transpiler call count: "+ (callcount++));
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
                if (latestEjectedRoundLocation == 0) 
                {
                    BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    // Now on player
                    if (Mod.playerInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventory[vanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.playerInventory.Add(vanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                }
                else // Could be in raid or base
                {
                    BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, vanillaItemDescriptor);

                    GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                    if (Mod.currentLocationIndex == 1)
                    {
                        // Now in hideout
                        if (Mod.baseInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                        {
                            Mod.baseInventory[vanillaItemDescriptor.H3ID] += 1;
                        }
                        else
                        {
                            Mod.baseInventory.Add(vanillaItemDescriptor.H3ID, 1);
                        }
                        if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                        {
                            Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                        }
                        else
                        {
                            Mod.currentBaseManager.baseInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                            Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                        }

                        latestEjectedRound.GetComponent<FVRPhysicalObject>().SetParentage(sceneRoot.transform.GetChild(2));
                    }
                    else if (Mod.currentLocationIndex == 2)
                    {
                        latestEjectedRound.GetComponent<FVRPhysicalObject>().SetParentage(sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2));
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
                if (latestEjectedRoundLocation == 0) 
                {
                    BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    // Now on player
                    if (Mod.playerInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventory[vanillaItemDescriptor.H3ID] += 1;
                    }
                    else
                    {
                        Mod.playerInventory.Add(vanillaItemDescriptor.H3ID, 1);
                    }
                    if (Mod.playerInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                    {
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                    else
                    {
                        Mod.playerInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                        Mod.playerInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                    }
                }
                else // Could be in raid or base
                {
                    BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, vanillaItemDescriptor);

                    GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                    if (Mod.currentLocationIndex == 1)
                    {
                        // Now in hideout
                        if (Mod.baseInventory.ContainsKey(vanillaItemDescriptor.H3ID))
                        {
                            Mod.baseInventory[vanillaItemDescriptor.H3ID] += 1;
                        }
                        else
                        {
                            Mod.baseInventory.Add(vanillaItemDescriptor.H3ID, 1);
                        }
                        if (Mod.currentBaseManager.baseInventoryObjects.ContainsKey(vanillaItemDescriptor.H3ID))
                        {
                            Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                        }
                        else
                        {
                            Mod.currentBaseManager.baseInventoryObjects.Add(vanillaItemDescriptor.H3ID, new List<GameObject>());
                            Mod.currentBaseManager.baseInventoryObjects[vanillaItemDescriptor.H3ID].Add(vanillaItemDescriptor.gameObject);
                        }

                        latestEjectedRound.GetComponent<FVRPhysicalObject>().SetParentage(sceneRoot.transform.GetChild(2));
                    }
                    else if (Mod.currentLocationIndex == 2)
                    {
                        latestEjectedRound.GetComponent<FVRPhysicalObject>().SetParentage(sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2));
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
            Mod.stamina = Mathf.Max(Mod.stamina - Mod.jumpStaminaDrain, 0);
            Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

            return false;
        }
    }

    // Patches FVRMovementManager.HandUpdateTwinstick to prevent sprinting in case of lack of stamina
    class MovementManagerUpdatePatch
    {
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
                        if (flag4)
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

            return false;
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
                VID.currentWeight -= 0.015f;
                if(VID.locationIndex == 0)
                {
                    Mod.weight -= 0.015f;
                }
            }
            else
            {
                EFM_VanillaItemDescriptor VID = __instance.Firearm.GetComponent<EFM_VanillaItemDescriptor>();
                VID.currentWeight += 0.015f;
                if (VID.locationIndex == 0)
                {
                    Mod.weight += 0.015f;
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

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.EndInteraction(__instance.m_hand);
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
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

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.EndInteraction(__instance.m_hand);
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
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

            EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
            EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (VID != null)
            {
                VID.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                //TODO: Set weight of firearm this is attached to if it is
            }
            else
            {
                if (postNumRounds == 0)
                {
                    if (CIW.ID.Equals("715") || CIW.ID.Equals("716"))
                    {
                        __instance.EndInteraction(__instance.m_hand);
                        GameObject.Destroy(CIW.gameObject);
                    }
                    else
                    {
                        CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                        CIW.amount -= (preNumRounds - postNumRounds);
                    }
                }
                else
                {
                    CIW.currentWeight -= 0.015f * (preNumRounds - postNumRounds);
                    CIW.amount -= (preNumRounds - postNumRounds);
                }
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

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 0.015f * (preNumRounds - postNumRounds);
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

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 0.015f * (preNumRounds - postNumRounds);
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

            __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight -= 0.015f * (preNumRounds - postNumRounds);
            //TODO: Set weight of firearm this is attached to if it is
        }
    }

    // Patches FVRFireArm.LoadMag to keep track of weight of mag on firearm and its location index
    class FireArmLoadMagPatch
    {
        static void Prefix(FVRFireArmMagazine mag, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Magazine == null && mag != null)
            {
                EFM_VanillaItemDescriptor magVID = mag.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight += magVID.currentWeight;

                if(fireArmVID.locationIndex == 0)
                {
                    // If went from outside player inventory into player inventory
                    if (magVID.locationIndex != 0)
                    {
                        Mod.weight += magVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(0, null, magVID);
                    }
                }
                else // Fire arm not on player
                {
                    // If went from player inventory to outside player inventory
                    if (magVID.locationIndex == 0)
                    {
                        Mod.weight -= magVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(fireArmVID.locationIndex, null, magVID);
                    }
                }
            }
        }
    }

    // Patches FVRFireArm.EjectMag to keep track of weight of mag on firearm and its location index
    class FireArmEjectMagPatch
    {
        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Magazine != null)
            {
                EFM_VanillaItemDescriptor magVID = __instance.Magazine.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight -= magVID.currentWeight;

                if(fireArmVID.locationIndex == 0)
                {
                    // Went from player to out of player
                    if (__instance.Magazine.m_hand == null)
                    {
                        Mod.weight -= magVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, magVID);
                    }
                }
                else
                {
                    // Went from out of player to player
                    if (__instance.Magazine.m_hand != null)
                    {
                        Mod.weight += magVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(0, null, magVID);
                    }
                }
            }
        }
    }

    // Patches FVRFireArm.LoadClip to keep track of weight of clip on firearm and its location index
    class FireArmLoadClipPatch
    {
        static void Prefix(FVRFireArmClip clip, ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Clip == null && clip != null)
            {
                EFM_VanillaItemDescriptor clipVID = clip.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight += clipVID.currentWeight;

                if(fireArmVID.locationIndex == 0)
                {
                    // If went from outside player inventory into player inventory
                    if (clipVID.locationIndex != 0)
                    {
                        Mod.weight += clipVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(0, null, clipVID);
                    }
                }
                else // Fire arm not on player
                {
                    // If went from player inventory to outside player inventory
                    if (clipVID.locationIndex == 0)
                    {
                        Mod.weight -= clipVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(fireArmVID.locationIndex, null, clipVID);
                    }
                }
            }
        }
    }

    // Patches FVRFireArm.EjectClip to keep track of weight of clip on firearm and its location index
    class FireArmEjectClipPatch
    {
        static void Prefix(ref FVRFireArm __instance)
        {
            if (!Mod.inMeatovScene)
            {
                return;
            }

            if (__instance.Clip != null)
            {
                EFM_VanillaItemDescriptor clipVID = __instance.Clip.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_VanillaItemDescriptor fireArmVID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                fireArmVID.currentWeight -= clipVID.currentWeight;

                if(fireArmVID.locationIndex == 0)
                {
                    // Went from player to out of player
                    if (__instance.Clip.m_hand == null)
                    {
                        Mod.weight -= clipVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(Mod.currentLocationIndex, null, clipVID);
                    }
                }
                else
                {
                    // Went from out of player to player
                    if (__instance.Clip.m_hand != null)
                    {
                        Mod.weight += clipVID.currentWeight;

                        BeginInteractionPatch.SetItemLocationIndex(0, null, clipVID);
                    }
                }
            }
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

            if (addedRound)
            {
                addedRound = false;
                EFM_VanillaItemDescriptor VID = __instance.GetComponent<EFM_VanillaItemDescriptor>();
                EFM_CustomItemWrapper CIW = __instance.GetComponent<EFM_CustomItemWrapper>();

                if (VID != null)
                {
                    VID.currentWeight += 0.015f;
                }
                else if(CIW != null)
                {
                    CIW.currentWeight += 0.015f;
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
                    VID.currentWeight += 0.015f;
                }
                else if (CIW != null)
                {
                    CIW.currentWeight += 0.015f;
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

            if (addedRound)
            {
                addedRound = false;
                __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 0.015f;
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
                __instance.GetComponent<EFM_VanillaItemDescriptor>().currentWeight += 0.015f;
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
    #endregion

    #region DebugPatches
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
            FVRPooledAudioSource fvrpooledAudioSource = __instance.SourceQueue_Disabled.Dequeue();
            fvrpooledAudioSource.gameObject.SetActive(true);
            fvrpooledAudioSource.Play(clipSet, pos, pitch, volume, mixerOverride);
            __instance.ActiveSources.Add(fvrpooledAudioSource);
            __result = fvrpooledAudioSource;
            return false;
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
