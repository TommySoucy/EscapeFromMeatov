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

        // Player
        public static float[] health; // 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
        public static float hydration = 100;
        public static float maxHydration = 100;
        public static float energy = 100;
        public static float maxEnergy = 100;
        public static float stamina = 100;
        public static float maxStamina = 100;
        public static float currentMaxStamina = 100;
        public static EFM_Skill[] skills;
        public static float level = 1;
        public static float sprintStaminaDrain = 4.1f;
        public static float overweightStaminaDrain = 4f;
        public static float staminaRestoration = 4.4f;
        public static float jumpStaminaDrain = 16;
        public static float currentStaminaEffect = 0;
        public static float weightLimit = 55;
        public static float currentWeightLimit = 55;

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
        public static GameObject doorLeftPrefab;
        public static GameObject doorRightPrefab;
        public static GameObject doorDoublePrefab;
        public static bool initDoors = true;
        public static Dictionary<string, Sprite> itemIcons;

        // DB
        public static JObject areasDB;
        public static JObject localDB;
        public static Dictionary<string, string> itemMap;
        public static JObject[] traderBaseDB;
        public static JObject globalDB;
        public static JObject mapData;
        public static JObject[] locationsDB;
        public static JObject defaultItemsData;
        public static Dictionary<string, EFM_VanillaItemDescriptor> vanillaItems;

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
            Key = 11 // TODO Implement, need to make all keys like h3vr keys so they can be used in doors
        }

        public enum ItemRarity
        {
            Common,
            Rare,
            Superrare,
            Not_exist
        }

        public void Awake()
        {
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

                if (Input.GetKeyDown(KeyCode.M))
                {
                    GameObject m1911 = IM.OD["M4A1Classic"].GetGameObject();
                    GameObject m1911Object = GameObject.Instantiate(m1911, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.3f, Quaternion.identity);
                    FVRObject m1911ObjectWrapper = m1911Object.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    m1911ObjectWrapper.ItemID = "M4A1Classic";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        m1911Object.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }
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
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        GameObject.Find("MeatovBase").GetComponent<EFM_Base_Manager>().OnSaveSlotClicked(0);
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

            LogInfo("Loading item prefabs...");
            // Load custom item prefabs
            otherActiveSlots = new List<List<FVRQuickBeltSlot>>();
            itemPrefabs = new List<GameObject>();
            itemIcons = new Dictionary<string, Sprite>();
            defaultItemsData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DefaultItemData.txt"));
            List<GameObject> rigConfigurations = new List<GameObject>();
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            for (int i = 0, rigIndex = 0; i < ((JArray)defaultItemsData["ItemDefaults"]).Count; ++i)
            {
                LogInfo("\tLoading Item"+i);
                GameObject itemPrefab = assetsBundle.LoadAsset<GameObject>("Item"+i);
                itemPrefab.name = defaultItemsData["ItemDefaults"][i]["DefaultPhysicalObject"]["DefaultObjectWrapper"]["DisplayName"].ToString();

                itemPrefabs.Add(itemPrefab);

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
                if (defaultObjectWrapper["TagFirearmFiringModes"] != null && ((JArray)defaultObjectWrapper["TagFirearmFiringModes"]).Count > 0)
                {
                    List<FVRObject.OTagFirearmFiringMode> newTagFirearmFiringModes = new List<FVRObject.OTagFirearmFiringMode>();
                    foreach (int e in defaultObjectWrapper["TagFirearmFiringModes"])
                    {
                        newTagFirearmFiringModes.Add((FVRObject.OTagFirearmFiringMode)e);
                    }
                    itemObjectWrapper.TagFirearmFiringModes = newTagFirearmFiringModes;
                }
                else
                {
                    itemObjectWrapper.TagFirearmFiringModes = null;
                }
                if (defaultObjectWrapper["TagFirearmFeedOption"] != null && ((JArray)defaultObjectWrapper["TagFirearmFeedOption"]).Count > 0)
                {
                    List<FVRObject.OTagFirearmFeedOption> newTagFirearmFeedOption = new List<FVRObject.OTagFirearmFeedOption>();
                    foreach (int e in defaultObjectWrapper["TagFirearmFeedOption"])
                    {
                        newTagFirearmFeedOption.Add((FVRObject.OTagFirearmFeedOption)e);
                    }
                    itemObjectWrapper.TagFirearmFeedOption = newTagFirearmFeedOption;
                }
                else
                {
                    itemObjectWrapper.TagFirearmFeedOption = null;
                }
                if (defaultObjectWrapper["TagFirearmMounts"] != null && ((JArray)defaultObjectWrapper["TagFirearmMounts"]).Count > 0)
                {
                    List<FVRObject.OTagFirearmMount> newTagFirearmMounts = new List<FVRObject.OTagFirearmMount>();
                    foreach (int e in defaultObjectWrapper["TagFirearmMounts"])
                    {
                        newTagFirearmMounts.Add((FVRObject.OTagFirearmMount)e);
                    }
                    itemObjectWrapper.TagFirearmMounts = newTagFirearmMounts;
                }
                else
                {
                    itemObjectWrapper.TagFirearmMounts = null;
                }
                itemObjectWrapper.TagAttachmentMount = (FVRObject.OTagFirearmMount)((int)defaultObjectWrapper["TagAttachmentMount"]);
                itemObjectWrapper.TagAttachmentFeature = (FVRObject.OTagAttachmentFeature)((int)defaultObjectWrapper["TagAttachmentFeature"]);
                itemObjectWrapper.TagMeleeStyle = (FVRObject.OTagMeleeStyle)((int)defaultObjectWrapper["TagMeleeStyle"]);
                itemObjectWrapper.TagMeleeHandedness = (FVRObject.OTagMeleeHandedness)((int)defaultObjectWrapper["TagMeleeHandedness"]);
                itemObjectWrapper.TagPowerupType = (FVRObject.OTagPowerupType)((int)defaultObjectWrapper["TagPowerupType"]);
                itemObjectWrapper.TagThrownType = (FVRObject.OTagThrownType)((int)defaultObjectWrapper["TagThrownType"]);
                itemObjectWrapper.MagazineType = (FireArmMagazineType)((int)defaultObjectWrapper["MagazineType"]);
                itemObjectWrapper.CreditCost = (int)defaultObjectWrapper["CreditCost"]; // TODO: Make this dependent on tarkov data, otherwise take defaultObjectWrapper.CreditCost
                itemObjectWrapper.OSple = (bool)defaultObjectWrapper["OSple"];

                // Add custom item wrapper
                EFM_CustomItemWrapper customItemWrapper = itemPrefab.AddComponent<EFM_CustomItemWrapper>();
                customItemWrapper.ID = i.ToString();
                customItemWrapper.physicalObject = itemPhysicalObject;
                customItemWrapper.itemType = (ItemType)(int)defaultItemsData["ItemDefaults"][i]["ItemType"];
                customItemWrapper.volumes = defaultItemsData["ItemDefaults"][i]["Volumes"].ToObject<float[]>();
                customItemWrapper.parent = defaultItemsData["ItemDefaults"][i]["parent"].ToString();
                if(defaultItemsData["ItemDefaults"][i]["MaxAmount"] != null)
                {
                    customItemWrapper.maxAmount = (int)defaultItemsData["ItemDefaults"][i]["MaxAmount"];
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
                    LogInfo("\t\tIs rig");
                    // Setup the slots for the player rig config and the rig config
                    int slotCount = 0;
                    GameObject quickBeltConfiguration = assetsBundle.LoadAsset<GameObject>("Item"+i+"Configuration");
                    customItemWrapper.rigSlots = new List<FVRQuickBeltSlot>();
                    for (int j = 0; j < quickBeltConfiguration.transform.childCount; ++j)
                    {
                        LogInfo("\t\tLoading rig slot "+j);
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
                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = (float)defaultItemsData["ItemDefaults"][i]["ContainerProperties"]["MaxVolume"];

                    // Set filter lists
                    SetFilterListsFor(customItemWrapper, i);
                }

                // Ammobox
                if(itemType == 8)
                {
                    FVRFireArmMagazine fireArmMagazine = (FVRFireArmMagazine)itemPhysicalObject;
                    fireArmMagazine.MagazineType = FireArmMagazineType.mNone;
                    fireArmMagazine.RoundEjectionPos = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 2);
                    fireArmMagazine.RoundType = (FireArmRoundType)Enum.Parse(typeof(FireArmRoundType), defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["roundType"].ToString());
                    fireArmMagazine.m_capacity = (int)defaultItemsData["ItemDefaults"][i]["AmmoBoxProperties"]["maxStack"];
                    fireArmMagazine.m_numRounds = 0;

                    FVRFireArmMagazineReloadTrigger reloadTrigger = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1).gameObject.AddComponent<FVRFireArmMagazineReloadTrigger>();
                    reloadTrigger.Magazine = fireArmMagazine;
                }

                // Money
                if (itemType == 9)
                {
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
                    customItemWrapper.stack = 1;
                }

                // Consumable
                if (itemType == 10)
                {
                    // Set use time
                    customItemWrapper.useTime = (float)defaultItemsData["ItemDefaults"][i]["useTime"];

                    // Set amount rate
                    if(defaultItemsData["ItemDefaults"][i]["hpResourceRate"] != null)
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
                            currentBuff.absolute = (bool)buff["Absolute"];
                            if (currentBuff.effectType == EFM_Effect.EffectType.SkillRate)
                            {
                                currentBuff.skillIndex = Mod.SkillNameToIndex(buff["SkillName"].ToString());
                            }
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

        private void LoadDB()
        {
            areasDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/areas.json"));
            localDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/local.json"));
            ParseItemMap();
            traderBaseDB = new JObject[8];
            for(int i=0; i < 8; ++i)
            {
                string traderID = EFM_TraderStatus.IndexToID(i);
                traderBaseDB[i] = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/traders/"+traderID+"/base.json"));
            }
            globalDB = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/globals.json"));
            mapData = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/EscapeFromMeatovMapData.txt"));
            locationsDB = new JObject[9];
            string[] locationFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov/Locations/");
            for (int i = 0; i < locationFiles.Length; ++i)
            {
                locationsDB[i] = JObject.Parse(File.ReadAllText(locationFiles[i]));
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
                descriptor.lootExperience = (float)vanillaItemRaw["LootExperience"];
                descriptor.rarity = ItemRarityStringToEnum(vanillaItemRaw["Rarity"].ToString());
                descriptor.spawnChance = (float)vanillaItemRaw["SpawnChance"];
                descriptor.creditCost = (int)vanillaItemRaw["CreditCost"];
                descriptor.parent = vanillaItemRaw["parent"].ToString();
                vanillaItems.Add(descriptor.H3ID, descriptor);
            }

            // Get all item spawner object defs
            UnityEngine.Object[] array = Resources.LoadAll("ItemSpawnerDefinitions", typeof(ItemSpawnerObjectDefinition));
            foreach(ItemSpawnerObjectDefinition def in array)
            {
                // Check if our vanilla items contain the ItemID of this object
                FVRPhysicalObject physObj = def.Prefab.GetComponentInChildren<FVRPhysicalObject>();
                if (physObj != null && vanillaItems.ContainsKey(physObj.ObjectWrapper.ItemID) && !itemIcons.ContainsKey(physObj.ObjectWrapper.ItemID))
                {
                    // Add the sprite
                    itemIcons.Add(physObj.ObjectWrapper.ItemID, def.Sprite);
                }
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

            harmony.Patch(configureQuickbeltPatchOriginal, new HarmonyMethod(configureQuickbeltPatchPrefix));

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
                LoadMainMenu();
            }
            else if (loadedScene.name.Equals("MeatovMenuScene"))
            {
                UnsecureObjects();
                LoadMeatov();
            }
            else if (loadedScene.name.Equals("Grillhouse_2Story"))
            {
                if (grillHouseSecure)
                {
                    isGrillhouse = true;
                }
            }
            else if (loadedScene.name.Equals("MeatovHideoutScene"))
            {
                UnsecureObjects();

                GameObject baseRoot = GameObject.Find("Hideout");

                EFM_Base_Manager baseManager = baseRoot.AddComponent<EFM_Base_Manager>();
                baseManager.data = EFM_Manager.loadedData;
                baseManager.Init();

                Transform spawnPoint = baseRoot.transform.GetChild(baseRoot.transform.childCount - 1).GetChild(0);
                GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);
            }
            else if (loadedScene.name.Equals("MeatovFactoryScene") /*|| other raid scenes*/)
            {
                UnsecureObjects();

                GameObject raidRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];

                EFM_Raid_Manager raidManager = raidRoot.AddComponent<EFM_Raid_Manager>();
                raidManager.Init();

                GM.CurrentMovementManager.TeleportToPoint(raidManager.spawnPoint.position, true, raidManager.spawnPoint.rotation.eulerAngles);
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
    // Patches SteamVR_LoadLevel.Begin() So we can keep certain objects from main menu since we don't have them in the mod scene by default
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
                     levelName.Equals("MeatovFactoryScene") /*|| other raid scenes*/)
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

            Mod.instance.LogInfo("\tSecuring cam rig");
            // Secure the cameraRig
            GameObject cameraRig = GameObject.Find("[CameraRig]Fixed");
            Mod.securedObjects.Add(cameraRig);
            GameObject.DontDestroyOnLoad(cameraRig);

            Mod.instance.LogInfo("\tSecuring scenesettings");
            // Secure sceneSettings
            GameObject sceneSettings = GameObject.Find("[SceneSettings_ModBlank_Simple]");
            Mod.securedObjects.Add(sceneSettings);
            GameObject.DontDestroyOnLoad(sceneSettings);

            Mod.instance.LogInfo("\tSecuring audio pools");
            // Secure Pooled sources
            FVRPooledAudioSource[] pooledAudioSources = FindObjectsOfTypeIncludingDisabled<FVRPooledAudioSource>();
            foreach (FVRPooledAudioSource pooledAudioSource in pooledAudioSources)
            {
                Mod.securedObjects.Add(pooledAudioSource.gameObject);
                GameObject.DontDestroyOnLoad(pooledAudioSource.gameObject);
            }

            Mod.instance.LogInfo("\tSecuring grabbity sphere");
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

            Mod.instance.LogInfo("\tSecuring movementmanager");
            // Secure MovementManager objects
            Mod.securedObjects.Add(GM.CurrentMovementManager.MovementRig.gameObject);
            GameObject.DontDestroyOnLoad(GM.CurrentMovementManager.MovementRig.gameObject);
            GameObject touchPadArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_touchpadArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(touchPadArrows);
            GameObject.DontDestroyOnLoad(touchPadArrows);
            GameObject joystickTPArrows = (GameObject)(typeof(FVRMovementManager).GetField("m_joystickTPArrows", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(joystickTPArrows);
            GameObject.DontDestroyOnLoad(joystickTPArrows);
            GameObject twinStickArrowsLeft = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsLeft", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(twinStickArrowsLeft);
            GameObject.DontDestroyOnLoad(twinStickArrowsLeft);
            GameObject twinStickArrowsRight = (GameObject)(typeof(FVRMovementManager).GetField("m_twinStickArrowsRight", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(twinStickArrowsRight);
            GameObject.DontDestroyOnLoad(twinStickArrowsRight);
            GameObject floorHelper = (GameObject)(typeof(FVRMovementManager).GetField("m_floorHelper", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(GM.CurrentMovementManager));
            Mod.securedObjects.Add(floorHelper);
            GameObject.DontDestroyOnLoad(floorHelper);

            Mod.instance.LogInfo("Securing copies of door instances if necessary");
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
            Mod.instance.LogInfo("endInteract postfix called, hand null?: "+(hand == null));
            // Whenever we drop an item, we want to make sure equip slots on this hand are active if they exist
            if (Mod.equipmentSlots != null)
            {
                for (int i = (hand.IsThisTheRightHand ? 0 : 3); i < (hand.IsThisTheRightHand ? 4 : 8); ++i)
                {
                    Mod.equipmentSlots[i].gameObject.SetActive(true);
                }
            }
            Mod.instance.LogInfo("0");

            // Stop here if dropping in a quick belt slot or if this is a door
            if ((__instance is FVRPhysicalObject && (__instance as FVRPhysicalObject).QuickbeltSlot != null) ||
                CheckIfDoorUpwards(__instance.gameObject, 3))
            {
                Mod.instance.LogInfo("Dropping item in qs or is door part");
                return;
            }
            Mod.instance.LogInfo("1");

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
            EFM_CustomItemWrapper collidingContainerWrapper = null;
            if (Mod.rightHand != null)
            {
                if (hand.IsThisTheRightHand)
                {
                    collidingContainerWrapper = Mod.rightHand.collidingContainerWrapper;
                }
                else // Left hand
                {
                    collidingContainerWrapper = Mod.leftHand.collidingContainerWrapper;
                }
            }

            EFM_CustomItemWrapper heldCustomItemWrapper = primary.GetComponent<EFM_CustomItemWrapper>();
            if (collidingContainerWrapper != null && (heldCustomItemWrapper == null || !heldCustomItemWrapper.Equals(collidingContainerWrapper)))
            {
                // Get item volume
                float volumeToUse = 0;
                if (heldCustomItemWrapper != null)
                {
                    volumeToUse = heldCustomItemWrapper.volumes[heldCustomItemWrapper.mode];
                }
                else
                {
                    volumeToUse = Mod.sizeVolumes[(int)primary.Size];
                }

                // Check if volume fits in container
                if (!collidingContainerWrapper.AddItemToContainer(primary))
                {
                    // Drop item in world
                    GameObject meatovBase = GameObject.Find("MeatovBase");
                    if (meatovBase != null)
                    {
                        primary.SetParentage(meatovBase.transform.GetChild(2));
                    }
                }
            }
            else
            {
                // Drop item in world
                GameObject sceneRoot = SceneManager.GetActiveScene().GetRootGameObjects()[0];
                EFM_Base_Manager baseManager = sceneRoot.GetComponent<EFM_Base_Manager>();
                EFM_Raid_Manager raidManager = sceneRoot.GetComponent<EFM_Raid_Manager>();
                if(baseManager != null)
                {
                    primary.SetParentage(sceneRoot.transform.GetChild(2));
                }
                else if(raidManager != null)
                {
                    primary.SetParentage(sceneRoot.transform.GetChild(1).GetChild(1).GetChild(2));
                }
            }
        }
    }

    // Patches FVRPlayerBody.ConfigureQuickbelt so we can call it with index -1 to clear the quickbelt, or to save which is the latest quick belt index we configured
    class ConfigureQuickbeltPatch
    {
        static bool Prefix(int index, ref FVRPlayerBody __instance)
        {
            if(index < 0)
            {
                // Clear the belt
                if (__instance.QuickbeltSlots.Count > 0)
                {
                    for (int i = __instance.QuickbeltSlots.Count - 1; i >= 0; i--)
                    {
                        if (__instance.QuickbeltSlots[i] == null)
                        {
                            __instance.QuickbeltSlots.RemoveAt(i);
                        }
                        else if (__instance.QuickbeltSlots[i].IsPlayer)
                        {
                            // Index -2 will destroy objects associated to the slots
                            if (index == -2 && __instance.QuickbeltSlots[i].CurObject != null)
                            {
                                GameObject.Destroy(__instance.QuickbeltSlots[i].CurObject.gameObject);
                                __instance.QuickbeltSlots[i].CurObject.ClearQuickbeltState();
                            }
                            UnityEngine.Object.Destroy(__instance.QuickbeltSlots[i].gameObject);
                            __instance.QuickbeltSlots.RemoveAt(i);
                        }
                    }
                }

                Mod.currentQuickBeltConfiguration = -1;

                return false;
            }

            Mod.currentQuickBeltConfiguration = index;

            return true;
        }
    }

    // Patches FVRViveHand.TestQuickBeltDistances so we also check custom slots and check for equipment incompatibility
    class TestQuickbeltPatch
    {
        static bool Prefix(ref FVRViveHand __instance, ref FVRViveHand.HandState ___m_state, ref FVRInteractiveObject ___m_currentInteractable)
        {
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
            if(fvrquickBeltSlot == null && Mod.equipmentSlots != null)
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
                                bool compatible = false;
                                switch (customItemWrapper.itemType)
                                {
                                    case Mod.ItemType.ArmoredRig:
                                        compatible = !EFM_EquipmentSlot.wearingArmoredRig && !EFM_EquipmentSlot.wearingBodyArmor && !EFM_EquipmentSlot.wearingRig;
                                        break;
                                    case Mod.ItemType.Backpack:
                                        compatible = !EFM_EquipmentSlot.wearingBackpack;
                                        break;
                                    case Mod.ItemType.BodyArmor:
                                        compatible = !EFM_EquipmentSlot.wearingBodyArmor && !EFM_EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case Mod.ItemType.Helmet:
                                        compatible = !EFM_EquipmentSlot.wearingHelmet;
                                        break;
                                    case Mod.ItemType.Rig:
                                        compatible = !EFM_EquipmentSlot.wearingRig && !EFM_EquipmentSlot.wearingArmoredRig;
                                        break;
                                    case Mod.ItemType.Pouch:
                                        compatible = !EFM_EquipmentSlot.wearingPouch;
                                        break;
                                    default:
                                        break;
                                }
                                if (compatible)
                                {
                                    __instance.CurrentHoveredQuickbeltSlot = fvrquickBeltSlot;
                                }
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
        static void Prefix(FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if(slot == null)
            {
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
                }
                else 
                {
                    // Check if slot in a loose rig
                    if (__instance.QuickbeltSlot)
                    {
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
                                    // Upadte rig content
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
        }

        static void Postfix(FVRQuickBeltSlot slot, ref FVRPhysicalObject __instance)
        {
            if (slot == null)
            {
                return;
            }

            if (slot is EFM_EquipmentSlot)
            {
                // Make equipment the size of its QBPoseOverride because by default the game only sets position and rotation
                if (__instance.QBPoseOverride != null)
                {
                    __instance.transform.localScale = __instance.QBPoseOverride.localScale;
                    Mod.instance.LogInfo("Object was put into equipment slot and scale set to: "+ __instance.transform.localScale.x);
                }

                EFM_EquipmentSlot.WearEquipment(__instance.GetComponent<EFM_CustomItemWrapper>());
            }
            else if (EFM_EquipmentSlot.wearingArmoredRig || EFM_EquipmentSlot.wearingRig) // We are wearing custom quick belt, check if slot is in there, update if it is
            {
                // Find slot index in config
                bool foundSlot = false;
                for (int slotIndex=0; slotIndex < GM.CurrentPlayerBody.QuickbeltSlots.Count; ++slotIndex)
                {
                    if (GM.CurrentPlayerBody.QuickbeltSlots[slotIndex].Equals(slot))
                    {
                        // Find rig in equipment slots
                        foundSlot = true;
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null) 
                            {
                                EFM_CustomItemWrapper equipmentItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipmentItemWrapper != null && (equipmentItemWrapper.itemType == Mod.ItemType.ArmoredRig || equipmentItemWrapper.itemType == Mod.ItemType.Rig))
                                {
                                    equipmentItemWrapper.itemsInSlots[slotIndex] = __instance.gameObject;
                                    return;
                                }
                            }
                        }
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
            EFM_CustomItemWrapper customItemWrapper = __instance.GetComponent<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig) 
                {
                    // Update whether we are picking the rig up from an equip slot
                    Mod.beginInteractingEquipRig = __instance.QuickbeltSlot != null && __instance.QuickbeltSlot is EFM_EquipmentSlot;

                    // Set hand's equip slot inactive depending on whether the rig is open
                    if (customItemWrapper.open)
                    {
                        for (int i = (hand.IsThisTheRightHand ? 0 : 3); i < (hand.IsThisTheRightHand ? 4 : 8); ++i)
                        {
                            Mod.equipmentSlots[i].gameObject.SetActive(false);
                        }
                    }

                    // Check which PoseOverride to use depending on hand side
                    __instance.PoseOverride = hand.IsThisTheRightHand ? customItemWrapper.rightHandPoseOverride : customItemWrapper.leftHandPoseOverride;
                }
                else if(customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    // Check which PoseOverride to use depending on hand side
                    __instance.PoseOverride = hand.IsThisTheRightHand ? customItemWrapper.rightHandPoseOverride : customItemWrapper.leftHandPoseOverride;
                }
            }

            // Check if in container
            if (__instance.transform.parent != null && __instance.transform.parent.parent != null)
            {
                EFM_CustomItemWrapper containerItemWrapper = __instance.transform.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                if(containerItemWrapper != null && (containerItemWrapper.itemType == Mod.ItemType.Backpack || 
                                                    containerItemWrapper.itemType == Mod.ItemType.Container || 
                                                    containerItemWrapper.itemType == Mod.ItemType.Pouch))
                {
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

        static void Postfix()
        {
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
                if (EFM_EquipmentSlot.currentHelmet != null && EFM_EquipmentSlot.currentHelmet.armor > 0 && UnityEngine.Random.value <= EFM_EquipmentSlot.currentHelmet.coverage)
                {
                    EFM_EquipmentSlot.currentHelmet.armor -= damage - damage * EFM_EquipmentSlot.currentHelmet.damageResist;
                    damage *= EFM_EquipmentSlot.currentHelmet.damageResist;
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
            Mod.instance.LogInfo("Dequeue and play debug prefix called on AudioSourcePool with type: " + __instance.Type + " with SourceQueue_Disabled count: " + __instance.SourceQueue_Disabled.Count);
            FVRPooledAudioSource fvrpooledAudioSource = __instance.SourceQueue_Disabled.Dequeue();
            Mod.instance.LogInfo("After dequeue count: "+ __instance.SourceQueue_Disabled.Count + ", Dequeued is null?: "+ (fvrpooledAudioSource == null));
            Mod.instance.LogInfo("Dequeued gameobject is null?: "+ (fvrpooledAudioSource.gameObject == null));
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
            Mod.instance.LogInfo("Instnatite and enqueue prefix called, component null?: "+(component == null));
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
