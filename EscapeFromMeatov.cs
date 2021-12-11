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
        public static readonly float[] sizeVolumes = { 1, 2, 4, 0, 8}; // 0: Small, 1: Medium, 2: Large, 3: Massive, 4: None, 5: CantCarryBig

        // Live data
        public static Mod instance;
        public static AssetBundle assetsBundle;
        public static AssetBundle sceneBundle;
        public static MainMenuSceneDef sceneDef;
        public static List<GameObject> securedObjects;
        public static int saveSlotIndex = -1;
        public static DefaultItemData defaultItemsData;
        public static int currentQuickBeltConfiguration = -1;
        public static int firstCustomConfigIndex = -1;
        public static List<EFM_EquipmentSlot> equipmentSlots;
        public static bool beginInteractingEquipRig;
        public static EFM_Hand rightHand;
        public static EFM_Hand leftHand;
        public static List<List<FVRQuickBeltSlot>> otherActiveSlots;

        // Assets
        public static Sprite sceneDefImage;
        public static GameObject scenePrefab_Menu;
        public static GameObject scenePrefab_Base;
        public static GameObject mainMenuPointable;
        public static GameObject quickBeltSlotPrefab;
        public static Material quickSlotHoverMaterial;
        public static Material quickSlotConstantMaterial;
        public static List<GameObject> itemPrefabs;

        // Config settings

        // DEBUG
        private bool debug;

        public enum ItemType
        {
            Generic = 0,
            BodyArmor = 1,
            Rig = 2,
            ArmoredRig = 3,
            Helmet = 4,
            Backpack = 5
        }

        public void Awake()
        {
        }

        public void Start()
        {
            instance = this;

            LoadConfig();

            LoadAssets();

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
                    SteamVR_LoadLevel.Begin("EscapeFromMeatovScene", false, 0.5f, 0f, 0f, 0f, 1f);
                }

                if (Input.GetKeyDown(KeyCode.U))
                {
                    GameObject armoredRig = itemPrefabs[1];
                    GameObject armoredRigObject = GameObject.Instantiate(armoredRig, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.5f, Quaternion.identity);
                    FVRObject armoredRigObjectWrapper = armoredRigObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    armoredRigObjectWrapper.ItemID = "1";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        armoredRigObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    }

                    GameObject cube = itemPrefabs[0];
                    GameObject cubeObject = GameObject.Instantiate(cube, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.55f, Quaternion.identity);
                    FVRObject cubeObjectWrapper = cubeObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    cubeObjectWrapper.ItemID = "0";
                    if (GameObject.Find("MeatovBase") != null)
                    {
                        cubeObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
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

                    //GameObject pumpkin = IM.OD["BodyPillow"].GetGameObject();
                    //GameObject pumpkinObject = GameObject.Instantiate(pumpkin, GM.CurrentPlayerBody.Head.position + GM.CurrentPlayerBody.Head.forward * 0.2f, Quaternion.identity);
                    //FVRObject pumpkinObjectWrapper = pumpkinObject.GetComponent<FVRPhysicalObject>().ObjectWrapper;
                    //pumpkinObjectWrapper.ItemID = "BodyPillow";
                    //if (GameObject.Find("MeatovBase") != null)
                    //{
                    //    pumpkinObject.transform.parent = GameObject.Find("MeatovBase").transform.GetChild(2);
                    //}
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    // Loads new game
                    EFM_Manager.LoadBase(GameObject.Find("MeatovMenu"));
                }

                if (Input.GetKeyDown(KeyCode.N))
                {
                    GM.CurrentPlayerBody.ConfigureQuickbelt(11);
                }

                if (Input.GetKeyDown(KeyCode.H))
                {
                    FieldInfo genericPoolField = typeof(SM).GetField("m_pool_generic", BindingFlags.NonPublic | BindingFlags.Instance);
                    Logger.LogInfo("Generic pool null?: "+(genericPoolField.GetValue(ManagerSingleton<SM>.Instance) == null));

                }

                if (Input.GetKeyDown(KeyCode.O))
                {
                    GameObject.Find("MeatovBase").GetComponent<EFM_Base_Manager>().OnConfirmRaidClicked();
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
                    EFM_Manager.LoadBase(GameObject.Find("MeatovMenu"), 0);
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
                }

                Logger.LogInfo("Configs loaded");
            }
            catch (FileNotFoundException ex) { Logger.LogInfo("Couldn't find EscapeFromMeatovConfig.txt, using default settings instead. Error: " + ex.Message); }
            catch (Exception ex) { Logger.LogInfo("Couldn't read EscapeFromMeatovConfig.txt, using default settings instead. Error: " + ex.Message); }
        }

        private void LoadAssets()
        {
            // Load mod's AssetBundle
            assetsBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovAssets.ab");
            sceneBundle = AssetBundle.LoadFromFile("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovScene.ab");

            // Load assets
            scenePrefab_Menu = assetsBundle.LoadAsset<GameObject>("MeatovMenu");
            scenePrefab_Base = assetsBundle.LoadAsset<GameObject>("MeatovBase");
            sceneDefImage = assetsBundle.LoadAsset<Sprite>("Tumbnail");
            mainMenuPointable = assetsBundle.LoadAsset<GameObject>("MeatovPointable");
            quickBeltSlotPrefab = assetsBundle.LoadAsset<GameObject>("QuickBeltSlot");

            // Load custom item prefabs
            otherActiveSlots = new List<List<FVRQuickBeltSlot>>();
            itemPrefabs = new List<GameObject>();
            defaultItemsData = JsonConvert.DeserializeObject<DefaultItemData>(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/DefaultItemData.txt"));
            List<GameObject> rigConfigurations = new List<GameObject>();
            quickSlotHoverMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Renderer>().material;
            quickSlotConstantMaterial = ManagerSingleton<GM>.Instance.QuickbeltConfigurations[0].transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material;
            for (int i = 0, rigIndex = 0; i < defaultItemsData.ItemDefaults.Count; ++i)
            {
                GameObject itemPrefab = assetsBundle.LoadAsset<GameObject>("Item"+i);
                itemPrefab.name = defaultItemsData.ItemDefaults[i].DefaultPhysicalObject.DefaultObjectWrapper.DisplayName;

                itemPrefabs.Add(itemPrefab);

                // Create an FVRPhysicalObject and FVRObject to fill with the item's default data
                FVRPhysicalObject itemPhysicalObject = itemPrefab.AddComponent<FVRPhysicalObject>();
                FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper = ScriptableObject.CreateInstance<FVRObject>();

                DefaultPhysicalObject defaultPhysicalObject = defaultItemsData.ItemDefaults[i].DefaultPhysicalObject;
                itemPhysicalObject.SpawnLockable = defaultPhysicalObject.SpawnLockable;
                itemPhysicalObject.Harnessable = defaultPhysicalObject.Harnessable;
                itemPhysicalObject.HandlingReleaseIntoSlotSound = (HandlingReleaseIntoSlotType)defaultPhysicalObject.HandlingReleaseIntoSlotSound;
                itemPhysicalObject.Size = (FVRPhysicalObject.FVRPhysicalObjectSize)defaultPhysicalObject.Size;
                itemPhysicalObject.QBSlotType = (FVRQuickBeltSlot.QuickbeltSlotType)defaultPhysicalObject.QBSlotType;
                itemPhysicalObject.DoesReleaseOverrideVelocity = defaultPhysicalObject.DoesReleaseOverrideVelocity;
                itemPhysicalObject.DoesReleaseAddVelocity = defaultPhysicalObject.DoesReleaseAddVelocity;
                itemPhysicalObject.ThrowVelMultiplier = defaultPhysicalObject.ThrowVelMultiplier;
                itemPhysicalObject.ThrowAngMultiplier = defaultPhysicalObject.ThrowAngMultiplier;
                itemPhysicalObject.MoveIntensity = defaultPhysicalObject.MoveIntensity;
                itemPhysicalObject.RotIntensity = defaultPhysicalObject.RotIntensity;
                itemPhysicalObject.UsesGravity = defaultPhysicalObject.UsesGravity;
                itemPhysicalObject.DistantGrabbable = defaultPhysicalObject.DistantGrabbable;
                itemPhysicalObject.IsDebug = defaultPhysicalObject.IsDebug;
                itemPhysicalObject.IsAltHeld = defaultPhysicalObject.IsAltHeld;
                itemPhysicalObject.IsKinematicLocked = defaultPhysicalObject.IsKinematicLocked;
                itemPhysicalObject.DoesQuickbeltSlotFollowHead = defaultPhysicalObject.DoesQuickbeltSlotFollowHead;
                itemPhysicalObject.IsPickUpLocked = defaultPhysicalObject.IsPickUpLocked;
                itemPhysicalObject.OverridesObjectToHand = (FVRPhysicalObject.ObjectToHandOverrideMode)defaultPhysicalObject.OverridesObjectToHand;
                itemPhysicalObject.PoseOverride = itemPrefab.transform.GetChild(1);
                itemPhysicalObject.QBPoseOverride = itemPrefab.transform.GetChild(2);

                DefaultObjectWrapper defaultObjectWrapper = defaultPhysicalObject.DefaultObjectWrapper;
                itemObjectWrapper.ItemID = i.ToString();
                itemObjectWrapper.DisplayName = defaultObjectWrapper.DisplayName;
                itemObjectWrapper.Category = (FVRObject.ObjectCategory)defaultObjectWrapper.Category;
                itemObjectWrapper.Mass = defaultObjectWrapper.Mass;
                itemObjectWrapper.MagazineCapacity = defaultObjectWrapper.MagazineCapacity;
                itemObjectWrapper.RequiresPicatinnySight = defaultObjectWrapper.RequiresPicatinnySight;
                itemObjectWrapper.TagEra = (FVRObject.OTagEra)defaultObjectWrapper.TagEra;
                itemObjectWrapper.TagSet = (FVRObject.OTagSet)defaultObjectWrapper.TagSet;
                itemObjectWrapper.TagFirearmSize = (FVRObject.OTagFirearmSize)defaultObjectWrapper.TagFirearmSize;
                itemObjectWrapper.TagFirearmAction = (FVRObject.OTagFirearmAction)defaultObjectWrapper.TagFirearmAction;
                itemObjectWrapper.TagFirearmRoundPower = (FVRObject.OTagFirearmRoundPower)defaultObjectWrapper.TagFirearmRoundPower;
                itemObjectWrapper.TagFirearmCountryOfOrigin = (FVRObject.OTagFirearmCountryOfOrigin)defaultObjectWrapper.TagFirearmCountryOfOrigin;
                if (defaultObjectWrapper.TagFirearmFiringModes != null && defaultObjectWrapper.TagFirearmFiringModes.Count > 0)
                {
                    List<FVRObject.OTagFirearmFiringMode> newTagFirearmFiringModes = new List<FVRObject.OTagFirearmFiringMode>();
                    foreach (int e in defaultObjectWrapper.TagFirearmFiringModes)
                    {
                        newTagFirearmFiringModes.Add((FVRObject.OTagFirearmFiringMode)e);
                    }
                    itemObjectWrapper.TagFirearmFiringModes = newTagFirearmFiringModes;
                }
                else
                {
                    itemObjectWrapper.TagFirearmFiringModes = null;
                }
                if (defaultObjectWrapper.TagFirearmFeedOption != null && defaultObjectWrapper.TagFirearmFeedOption.Count > 0)
                {
                    List<FVRObject.OTagFirearmFeedOption> newTagFirearmFeedOption = new List<FVRObject.OTagFirearmFeedOption>();
                    foreach (int e in defaultObjectWrapper.TagFirearmFeedOption)
                    {
                        newTagFirearmFeedOption.Add((FVRObject.OTagFirearmFeedOption)e);
                    }
                    itemObjectWrapper.TagFirearmFeedOption = newTagFirearmFeedOption;
                }
                else
                {
                    itemObjectWrapper.TagFirearmFeedOption = null;
                }
                if (defaultObjectWrapper.TagFirearmMounts != null && defaultObjectWrapper.TagFirearmMounts.Count > 0)
                {
                    List<FVRObject.OTagFirearmMount> newTagFirearmMounts = new List<FVRObject.OTagFirearmMount>();
                    foreach (int e in defaultObjectWrapper.TagFirearmMounts)
                    {
                        newTagFirearmMounts.Add((FVRObject.OTagFirearmMount)e);
                    }
                    itemObjectWrapper.TagFirearmMounts = newTagFirearmMounts;
                }
                else
                {
                    itemObjectWrapper.TagFirearmMounts = null;
                }
                itemObjectWrapper.TagAttachmentMount = (FVRObject.OTagFirearmMount)defaultObjectWrapper.TagAttachmentMount;
                itemObjectWrapper.TagAttachmentFeature = (FVRObject.OTagAttachmentFeature)defaultObjectWrapper.TagAttachmentFeature;
                itemObjectWrapper.TagMeleeStyle = (FVRObject.OTagMeleeStyle)defaultObjectWrapper.TagMeleeStyle;
                itemObjectWrapper.TagMeleeHandedness = (FVRObject.OTagMeleeHandedness)defaultObjectWrapper.TagMeleeHandedness;
                itemObjectWrapper.TagPowerupType = (FVRObject.OTagPowerupType)defaultObjectWrapper.TagPowerupType;
                itemObjectWrapper.TagThrownType = (FVRObject.OTagThrownType)defaultObjectWrapper.TagThrownType;
                itemObjectWrapper.MagazineType = (FireArmMagazineType)defaultObjectWrapper.MagazineType;
                itemObjectWrapper.CreditCost = defaultObjectWrapper.CreditCost;
                itemObjectWrapper.OSple = defaultObjectWrapper.OSple;

                // Add custom item wrapper
                EFM_CustomItemWrapper customItemWrapper = itemPrefab.AddComponent<EFM_CustomItemWrapper>();
                customItemWrapper.physicalObject = itemPhysicalObject;
                customItemWrapper.itemType = (ItemType)defaultItemsData.ItemDefaults[i].ItemType;
                customItemWrapper.volumes = defaultItemsData.ItemDefaults[i].Volumes.ToArray();

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
                if (defaultItemsData.ItemDefaults[i].ItemType == 1 || defaultItemsData.ItemDefaults[i].ItemType == 3)
                {
                    customItemWrapper.coverage = defaultItemsData.ItemDefaults[i].ArmorAndRigProperties.Coverage;
                    customItemWrapper.damageResist = defaultItemsData.ItemDefaults[i].ArmorAndRigProperties.DamageResist;
                    customItemWrapper.maxArmor = defaultItemsData.ItemDefaults[i].ArmorAndRigProperties.Armor;
                    customItemWrapper.armor = customItemWrapper.maxArmor;
                }

                // Rig
                if (defaultItemsData.ItemDefaults[i].ItemType == 2 || defaultItemsData.ItemDefaults[i].ItemType == 3)
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

                        FVRQuickBeltSlot slotComponent = slotObject.AddComponent<FVRQuickBeltSlot>();
                        slotComponent.QuickbeltRoot = slotObject.transform;
                        slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                        slotComponent.HoverGeo.SetActive(false);
                        slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                        slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;

                        FVRQuickBeltSlot rigSlotComponent = rigSlotObject.AddComponent<FVRQuickBeltSlot>();
                        customItemWrapper.rigSlots.Add(rigSlotComponent);
                        rigSlotComponent.QuickbeltRoot = rigSlotObject.transform;
                        rigSlotComponent.HoverGeo = rigSlotObject.transform.GetChild(0).GetChild(0).gameObject;
                        rigSlotComponent.HoverGeo.SetActive(false);
                        rigSlotComponent.PoseOverride = rigSlotObject.transform.GetChild(0).GetChild(2);
                        rigSlotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
                        switch (slotObject.name.Split('_')[0])
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

                        // Set slot sphere materials
                        slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = quickSlotHoverMaterial;
                        slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = quickSlotConstantMaterial;

                        // Set slot sphere materials
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
                if (defaultItemsData.ItemDefaults[i].ItemType == 5)
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

                    customItemWrapper.itemObjectsRoot = itemPrefab.transform.GetChild(itemPrefab.transform.childCount - 1);
                    customItemWrapper.maxVolume = defaultItemsData.ItemDefaults[i].BackpackProperties.MaxVolume;

                    customItemWrapper.rightHandPoseOverride = itemPhysicalObject.PoseOverride;
                    customItemWrapper.leftHandPoseOverride = itemPhysicalObject.PoseOverride.GetChild(0);
                }
            }

            // Add custom quick belt configs we made to GM's array of quick belt configurations
            List<GameObject> customQuickBeltConfigurations = new List<GameObject>();
            customQuickBeltConfigurations.AddRange(ManagerSingleton<GM>.Instance.QuickbeltConfigurations);
            firstCustomConfigIndex = customQuickBeltConfigurations.Count;
            customQuickBeltConfigurations.AddRange(rigConfigurations);
            ManagerSingleton<GM>.Instance.QuickbeltConfigurations = customQuickBeltConfigurations.ToArray();

            // TODO: Had to initialize something here i think, but forgot what
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
            // Subscribe to events
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Create scene def
            sceneDef = new MainMenuSceneDef();
            sceneDef.name = "MeatovSceneScreen";
            sceneDef.SceneName = "EscapeFromMeatovScene";
            sceneDef.Name = "Escape from Meatov";
            sceneDef.Desciption = "Enter Meatov, loot, attempt escape. Upgrade your base, complete quests, trade, and go again. Good luck.";
            sceneDef.Image = sceneDefImage;
            sceneDef.Type = "Escape";
        }

        public void OnSceneLoaded(Scene loadedScene, LoadSceneMode loadedSceneMode)
        {
            Logger.LogInfo("OnSceneLoaded called with scene: "+loadedScene.name);
            if(loadedScene.name.Equals("MainMenu3"))
            {
                LoadMainMenu();
            }
            else if (loadedScene.name.Equals("EscapeFromMeatovScene"))
            {
                UnsecureObjects();
                LoadMeatov();
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
            pointableInstance.MaxPointingRange = 16;
            currentPointable.transform.position = new Vector3(-12.14f, 9.5f, 4.88f);
            currentPointable.transform.rotation = Quaternion.Euler(0, 300, 0);
        }

        private void LoadMeatov()
        {
            // Instantiate scene prefab
            GameObject prefabInstance = Instantiate<GameObject>(scenePrefab_Menu);
            prefabInstance.name = scenePrefab_Menu.name;

            // TP Player
            Transform spawnPoint = prefabInstance.transform.GetChild(prefabInstance.transform.childCount - 1).GetChild(0);
            GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);

            // Init menu
            EFM_Menu_Manager menuManager = prefabInstance.AddComponent<EFM_Menu_Manager>();
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
    }

    #region GamePatches
    // Patches SteamVR_LoadLevel.Begin() So we can keep certain objects from main menu since we don't have them in the mod scene by default
    class LoadLevelBeginPatch
    {
        static void Prefix(string levelName)
        {
            Mod.instance.LogInfo("load level prefix called with levelname: "+levelName);
            if (levelName.Equals("EscapeFromMeatovScene"))
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
                GameObject sceneSettings = GameObject.Find("[SceneSettings]");
                Mod.securedObjects.Add(sceneSettings);
                GameObject.DontDestroyOnLoad(sceneSettings);

                Mod.instance.LogInfo("\tSecuring scenesettings");
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
            // Whenever we drop an item, we want to make sure equip slots on this hand are active
            for (int i = (hand.IsThisTheRightHand ? 0 : 3); i < (hand.IsThisTheRightHand ? 4 : 8); ++i)
            {
                Mod.equipmentSlots[i].gameObject.SetActive(true);
            }

            if (__instance is FVRPhysicalObject && (__instance as FVRPhysicalObject).QuickbeltSlot != null)
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

        private static void DropItem(FVRViveHand hand, FVRPhysicalObject primary)
        {
            EFM_CustomItemWrapper collidingBackpackWrapper = null;
            if (hand.IsThisTheRightHand)
            {
                collidingBackpackWrapper = Mod.rightHand.collidingBackpackWrapper;
            }
            else // Left hand
            {
                collidingBackpackWrapper = Mod.leftHand.collidingBackpackWrapper;
            }

            EFM_CustomItemWrapper heldCustomItemWrapper = primary.GetComponent<EFM_CustomItemWrapper>();
            if (collidingBackpackWrapper != null && (heldCustomItemWrapper == null || !heldCustomItemWrapper.Equals(collidingBackpackWrapper)))
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

                // Check if volume fits in bag
                if (collidingBackpackWrapper.containingVolume + volumeToUse <= collidingBackpackWrapper.maxVolume)
                {
                    // Attach item to backpack
                    // Set all non trigger colliders that are on default layer to trigger so they dont collide with bag
                    Collider[] cols = primary.gameObject.GetComponentsInChildren<Collider>(true);
                    if(collidingBackpackWrapper.resetColPairs == null)
                    {
                        collidingBackpackWrapper.resetColPairs = new List<EFM_CustomItemWrapper.ResetColPair>();
                    }
                    EFM_CustomItemWrapper.ResetColPair resetColPair = null;
                    foreach (Collider col in cols)
                    {
                        if (col.gameObject.layer == 0)
                        {
                            col.isTrigger = true;

                            // Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
                            if (resetColPair == null)
                            {
                                resetColPair = new EFM_CustomItemWrapper.ResetColPair();
                                resetColPair.physObj = primary;
                                resetColPair.colliders = new List<Collider>();
                            }
                            resetColPair.colliders.Add(col);
                        }
                    }
                    if(resetColPair != null)
                    {
                        collidingBackpackWrapper.resetColPairs.Add(resetColPair);
                    }
                    primary.SetParentage(collidingBackpackWrapper.itemObjectsRoot);
                    primary.RootRigidbody.isKinematic = true;

                    // Add volume to backpack
                    EFM_CustomItemWrapper primaryWrapper = primary.gameObject.GetComponent<EFM_CustomItemWrapper>();
                    if (primaryWrapper != null)
                    {
                        collidingBackpackWrapper.containingVolume += primaryWrapper.volumes[primaryWrapper.mode];
                    }
                    else
                    {
                        collidingBackpackWrapper.containingVolume += Mod.sizeVolumes[(int)primary.Size];
                    }
                }
                else
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
                GameObject meatovBase = GameObject.Find("MeatovBase");
                if (meatovBase != null)
                {
                    primary.SetParentage(meatovBase.transform.GetChild(2));
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
            if (equipmentSlotIndex == -1)
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

            // Check if in backpack
            if (__instance.transform.parent != null && __instance.transform.parent.parent != null)
            {
                EFM_CustomItemWrapper backpackItemWrapper = __instance.transform.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                if(backpackItemWrapper != null && backpackItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    backpackItemWrapper.containingVolume -= customItemWrapper != null ? customItemWrapper.volumes[customItemWrapper.mode]: Mod.sizeVolumes[(int)__instance.Size];

                    // Reset cols of item so that they are non trigger again and can collide with the world and the bag
                    for(int i = backpackItemWrapper.resetColPairs.Count - 1; i >= 0; --i)
                    {
                        if (backpackItemWrapper.resetColPairs[i].physObj.Equals(__instance))
                        {
                            foreach(Collider col in backpackItemWrapper.resetColPairs[i].colliders)
                            {
                                col.isTrigger = false;
                            }
                            backpackItemWrapper.resetColPairs.RemoveAt(i);
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
        static bool Prefix(Collider collider, bool isEnter, bool isPalm, ref FVRViveHand __instance)
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

            FieldInfo stateField = typeof(FVRViveHand).GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);

            if ((FVRViveHand.HandState)stateField.GetValue(__instance) == FVRViveHand.HandState.Empty && interactiveObjectToUse != null)
            {
                FieldInfo isClosestInteractableInPalmField = typeof(FVRViveHand).GetField("m_isClosestInteractableInPalm", BindingFlags.NonPublic | BindingFlags.Instance);

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
                            isClosestInteractableInPalmField.SetValue(__instance, false);
                        }
                        else
                        {
                            isClosestInteractableInPalmField.SetValue(__instance, true);
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
                        if (flag && num2 < num4 && (bool)isClosestInteractableInPalmField.GetValue(__instance))
                        {
                            isClosestInteractableInPalmField.SetValue(__instance, true);
                            __instance.ClosestPossibleInteractable = component2;
                        }
                        else if (!flag && num < num3)
                        {
                            isClosestInteractableInPalmField.SetValue(__instance, false);
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
