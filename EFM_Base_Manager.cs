using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;

namespace EFM
{
    public class EFM_Base_Manager : EFM_Manager
    {
        public enum FinishRaidState
        {
            Survived,

            // These are fails
            RunThrough,
            MIA,
            KIA
        }

        // UI
        private Button[][] buttons;
        private Transform canvas;
        private Text raidCountdownTitle;
        private Text raidCountdown;
        private Text timeChoice0;
        private Text timeChoice1;
        private Text chosenCharacter;
        private Text chosenMap;
        private Text chosenTime;

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
        public static Sprite[] skillIcons;

        public SaveData data;

        public int chosenCharIndex = -1;
        public int chosenMapIndex = -1;
        public int chosenTimeIndex = -1;

        public static float time;
        private bool cancelRaidLoad;
        private bool loadingRaid;
        private bool loadingMap;
        private bool countdownDeploy;
        private float deployTimer;
        private float deployTime = 10;
        private Transform raidSpawnPoint;
        AssetBundleCreateRequest currentRaidBundleRequest;
        AssetBundleRequest currentRaidMapRequest;
        public List<EFM_BaseAreaManager> baseAreaManagers;
        public Dictionary<string, int> baseInventory;
        public Dictionary<string, List<GameObject>> baseInventoryObjects;
        public EFM_TraderStatus[] traderStatuses;

        private void Update()
        {
            if (init)
            {
                UpdateTime();
            }

            // Handle raid loading process
            if (cancelRaidLoad)
            {
                loadingRaid = false;
                loadingMap = false;

                // Wait until the raid map is done loading before unloading it
                if (currentRaidBundleRequest.isDone)
                {
                    if(currentRaidBundleRequest.assetBundle != null)
                    {
                        currentRaidBundleRequest.assetBundle.Unload(true);
                        cancelRaidLoad = false;
                    }
                    else
                    {
                        cancelRaidLoad = false;
                    }
                    canvas.GetChild(7).gameObject.SetActive(false);
                    canvas.GetChild(0).gameObject.SetActive(true);
                }
            }
            else if (countdownDeploy)
            {
                deployTimer -= Time.deltaTime;

                TimeSpan timeSpan = TimeSpan.FromSeconds(deployTimer);
                raidCountdown.text = string.Format(@"{0:ss\.ff}", timeSpan);

                if (deployTimer <= 0)
                {
                    GM.CurrentMovementManager.TeleportToPoint(raidSpawnPoint.position, true, raidSpawnPoint.rotation.eulerAngles);
                    countdownDeploy = false;
                }
            }
            else if (loadingMap)
            {
                if (currentRaidMapRequest.isDone)
                {
                    if (currentRaidMapRequest.asset != null)
                    {
                        // Instantiate map asset
                        GameObject map = Instantiate<GameObject>(currentRaidMapRequest.allAssets[0] as GameObject);
                        map.name = currentRaidMapRequest.asset.name;

                        raidCountdownTitle.text = "Initializing map 3/3";
                        raidCountdown.text = string.Empty;

                        EFM_Raid_Manager raidManager = map.AddComponent<EFM_Raid_Manager>();
                        raidManager.baseManager = this;
                        raidManager.Init();

                        raidSpawnPoint = raidManager.spawnPoint;
                        countdownDeploy = true;

                        deployTimer = deployTime;
                        raidCountdownTitle.text = "Deploying in:";

                        currentRaidBundleRequest.assetBundle.Unload(false);
                        loadingMap = false;
                    }
                    else
                    {
                        Mod.instance.LogError("Could not load raid map, cancelling");
                        cancelRaidLoad = true;
                    }
                }
                else
                {
                    raidCountdownTitle.text = "Loading map 2/3:";
                    raidCountdown.text = (currentRaidMapRequest.progress * 100).ToString() + "%";
                }
            }
            else if (loadingRaid)
            {
                if(currentRaidBundleRequest.isDone)
                {
                    if(currentRaidBundleRequest.assetBundle != null)
                    {
                        // Load the asset of the map
                        currentRaidMapRequest = currentRaidBundleRequest.assetBundle.LoadAllAssetsAsync<GameObject>();
                        loadingMap = true;
                        loadingRaid = false;
                    }
                    else
                    {
                        Mod.instance.LogError("Could not load raid map bundle, cancelling");
                        cancelRaidLoad = true;
                    }
                }
                else
                {
                    raidCountdownTitle.text = "Loading assetbundle 1/3:";
                    raidCountdown.text = (currentRaidBundleRequest.progress * 100).ToString() + "%";
                }
            }
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * EFM_Manager.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            string formattedTime0 = string.Format(@"{0:hh\:mm}", timeSpan);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            TimeSpan offsetTimeSpan = TimeSpan.FromSeconds(offsetTime);
            string formattedTime1 = string.Format(@"{0:hh\:mm}", offsetTimeSpan);
            timeChoice1.text = formattedTime1;

            chosenTime.text = chosenTimeIndex == 0 ? formattedTime0 : formattedTime1;
        }

        public override void Init()
        {
            SetupPlayerRig();

            ProcessData();

            InitUI();

            InitTime();

            init = true;
        }

        private void SetupPlayerRig()
        {
            Mod.instance.LogInfo("0");
            // Add equipment slots
            // Clear any previously existing ones
            if (Mod.equipmentSlots != null)
            {
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i] != null)
                    {
                        Destroy(Mod.equipmentSlots[i].gameObject);
                    }
                }
                Mod.equipmentSlots.Clear();
            }
            else
            {
                Mod.equipmentSlots = new List<EFM_EquipmentSlot>();
            }
            List<GameObject> slotObjects = new List<GameObject>();
            for (int side = 0; side < 2; ++side)
            {
                for (int x = 0; x < 2; ++x)
                {
                    for (int y = 0; y < 2; ++y)
                    {
                        GameObject slotObject = Instantiate(Mod.quickBeltSlotPrefab, side == 0 ? GM.CurrentPlayerBody.RightHand : GM.CurrentPlayerBody.LeftHand);
                        slotObject.tag = "QuickbeltSlot";
                        slotObject.transform.localPosition = new Vector3((x - 0.5f) * 0.1f, 0, (y - 2.5f) * 0.1f);
                        slotObject.transform.localRotation = Quaternion.identity;
                        slotObject.name = "EquipmentSlot" + (side == 0 ? "Right" : "Left");
                        slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields
                        slotObjects.Add(slotObject);

                        EFM_EquipmentSlot slotComponent = slotObject.AddComponent<EFM_EquipmentSlot>();
                        Mod.equipmentSlots.Add(slotComponent);
                        slotComponent.QuickbeltRoot = slotObject.transform;
                        slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                        slotComponent.HoverGeo.SetActive(false);
                        slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                        slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                        slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;

                        slotComponent.HoverGeo.transform.localScale = Vector3.one * 0.1f;
                        slotObject.transform.GetChild(0).GetChild(1).localScale = Vector3.one * 0.1f;
                        slotComponent.PoseOverride.transform.localScale = Vector3.one * 0.1f;
                        slotComponent.PoseOverride.transform.localRotation = Quaternion.Euler(270, -90, -90);

                        // Set slot sphere materials
                        slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                        slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;
                    }
                }
            }
            foreach (GameObject slotObject in slotObjects)
            {
                slotObject.SetActive(true);
            }

            // Add our own hand component to each hand
            Mod.rightHand = GM.CurrentPlayerBody.RightHand.gameObject.AddComponent<EFM_Hand>();
            Mod.leftHand = GM.CurrentPlayerBody.LeftHand.gameObject.AddComponent<EFM_Hand>();
            Mod.rightHand.otherHand = Mod.leftHand;
            Mod.leftHand.otherHand = Mod.rightHand;
        }

        public void ProcessData()
        {
            // Check if we have loaded data
            if (data == null)
            {
                // TODO: This is a new game, so we need to spawn starting equipement in the base and story/tutorial UI
                data = new SaveData();
                data.level = 1;
                data.skills = new List<SavedSkill>();
                for(int i=0; i < 52; ++i)
                {
                    data.skills.Add(new SavedSkill());
                }

                // Instantiate areas
                baseAreaManagers = new List<EFM_BaseAreaManager>();
                for (int i = 0; i < 22; ++i)
                {
                    EFM_BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<EFM_BaseAreaManager>();
                    currentBaseAreaManager.baseManager = this;
                    currentBaseAreaManager.areaIndex = i;
                    currentBaseAreaManager.level = 0;
                    currentBaseAreaManager.constructing = false;
                    currentBaseAreaManager.constructTime = 0;

                    baseAreaManagers.Add(currentBaseAreaManager);
                }

                // Instantiate other
                baseInventory = new Dictionary<string, int>();
                baseInventoryObjects = new Dictionary<string, List<GameObject>>();
                traderStatuses = new EFM_TraderStatus[8];
                for (int i = 0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, 0, 0, i == 7 ? false : true);
                }

                return;
            }

            // Instantiate items
            Transform itemsRoot = transform.GetChild(2);
            for (int i = 0; i < data.items.Count; ++i)
            {
                SavedItem item = data.items[i];
                LoadSavedItem(itemsRoot, item);
            }
            // Count each type of item we have
            UpdateBaseInventory();

            // Instantiate areas
            baseAreaManagers = new List<EFM_BaseAreaManager>();
            for (int i = 0; i < 22; ++i)
            {
                EFM_BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<EFM_BaseAreaManager>();
                currentBaseAreaManager.baseManager = this;
                currentBaseAreaManager.areaIndex = i;
                if (data.areas != null)
                {
                    currentBaseAreaManager.level = data.areas[i].level;
                    currentBaseAreaManager.constructing = data.areas[i].constructing;
                    currentBaseAreaManager.constructTime = data.areas[i].constructTime;
                    if (data.areas[i].slots != null)
                    {
                        currentBaseAreaManager.slotItems = new List<GameObject>();
                        foreach (SavedItem item in data.areas[i].slots)
                        {
                            if (item == null)
                            {
                                currentBaseAreaManager.slotItems.Add(null);
                            }
                            else
                            {
                                currentBaseAreaManager.slotItems.Add(LoadSavedItem(currentBaseAreaManager.transform.GetChild(currentBaseAreaManager.transform.childCount - 1), item));
                            }
                        }
                    }
                }
                currentBaseAreaManager.level = 0;
                currentBaseAreaManager.constructing = false;
                currentBaseAreaManager.constructTime = 0;

                baseAreaManagers.Add(currentBaseAreaManager);
            }

            // Load trader statuses
            traderStatuses = new EFM_TraderStatus[8];
            if(data.traderStatuses == null)
            {
                for(int i=0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, 0, 0, i == 7 ? false : true);
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, data.traderStatuses[i].salesSum, data.traderStatuses[i].standing, data.traderStatuses[i].unlocked);
                }
            }
        }

        public void UpdateBaseInventory()
        {
            if(baseInventory == null)
            {
                baseInventory = new Dictionary<string, int>();
                baseInventoryObjects = new Dictionary<string, List<GameObject>>();
            }

            baseInventory.Clear();
            baseInventoryObjects.Clear();

            Transform itemsRoot = transform.GetChild(2);

            foreach(Transform item in itemsRoot)
            {
                AddToBaseInventory(item);
            }
        }

        private void AddToBaseInventory(Transform item)
        {
            // TODO: Check for stack in stackable items
            string itemID = item.GetComponent<FVRPhysicalObject>().ObjectWrapper.ItemID;
            if (baseInventory.ContainsKey(itemID))
            {
                baseInventory[itemID] += 1;
                baseInventoryObjects[itemID].Add(item.gameObject);
            }
            else
            {
                baseInventory.Add(itemID, 1);
                baseInventoryObjects.Add(itemID, new List<GameObject> { item.gameObject });
            }

            // Check for more items that may be contained inside this one
            EFM_CustomItemWrapper customItemWrapper = item.GetComponent<EFM_CustomItemWrapper>();
            if(customItemWrapper != null && customItemWrapper.itemObjectsRoot != null)
            {
                foreach (Transform innerItem in customItemWrapper.itemObjectsRoot)
                {
                    AddToBaseInventory(innerItem);
                }
            }
        }

        private GameObject LoadSavedItem(Transform parent, SavedItem item)
        {
            Mod.instance.LogInfo("Loading item "+item.PhysicalObject.ObjectWrapper.ItemID+", on parent "+parent.name);
            int parsedID = -1;
            GameObject prefabToUse = null;
            if (int.TryParse(item.PhysicalObject.ObjectWrapper.ItemID, out parsedID))
            {
                // Custom item, fetch from our own assets
                prefabToUse = Mod.itemPrefabs[parsedID];
            }
            else
            {
                // Vanilla item, fetch from game assets
                prefabToUse = IM.OD[item.PhysicalObject.ObjectWrapper.ItemID].GetGameObject();
            }

            GameObject itemObject = Instantiate<GameObject>(prefabToUse, parent);

            FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;
            Mod.instance.LogInfo("physical object null?: "+(itemPhysicalObject == null)+", itemObjectWrapper null?: "+(itemObjectWrapper == null));

            // Fill data

            // PhysicalObject
            itemPhysicalObject.m_isSpawnLock = item.PhysicalObject.m_isSpawnLock;
            itemPhysicalObject.m_isHardnessed = item.PhysicalObject.m_isHarnessed;
            itemPhysicalObject.IsKinematicLocked = item.PhysicalObject.IsKinematicLocked;
            itemPhysicalObject.IsInWater = item.PhysicalObject.IsInWater;
            AddAttachments(itemPhysicalObject, item.PhysicalObject);
            if (item.PhysicalObject.heldMode != 0)
            {
                FVRViveHand hand = (item.PhysicalObject.heldMode == 1 ? GM.CurrentPlayerBody.RightHand : GM.CurrentPlayerBody.LeftHand).GetComponentInChildren<FVRViveHand>();
                hand.CurrentInteractable = itemPhysicalObject;
                FieldInfo handStateField = typeof(FVRViveHand).GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);
                handStateField.SetValue(hand, FVRViveHand.HandState.GripInteracting);
                hand.CurrentInteractable.BeginInteraction(hand);
            }
            Mod.instance.LogInfo("Filled physical object data");

            // ObjectWrapper
            itemObjectWrapper.ItemID = item.PhysicalObject.ObjectWrapper.ItemID;

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                FVRFireArm firearmPhysicalObject = itemPhysicalObject as FVRFireArm;
                Mod.instance.LogInfo("loading firearm " + firearmPhysicalObject.name);

                // Build and load flagDict from saved lists
                if (item.PhysicalObject.flagDictKeys != null && item.PhysicalObject.flagDictKeys.Count > 0)
                {
                    Dictionary<string, string> flagDict = new Dictionary<string, string>();
                    for (int j = 0; j < item.PhysicalObject.flagDictKeys.Count; ++j)
                    {
                        flagDict.Add(item.PhysicalObject.flagDictKeys[j], item.PhysicalObject.flagDictValues[j]);
                    }
                    firearmPhysicalObject.ConfigureFromFlagDic(flagDict);
                }

                // Chambers
                List<FireArmRoundClass> newLoadedRoundsInChambers = new List<FireArmRoundClass>();
                if (item.PhysicalObject.loadedRoundsInChambers != null && item.PhysicalObject.loadedRoundsInChambers.Count > 0)
                {
                    foreach (int round in item.PhysicalObject.loadedRoundsInChambers)
                    {
                        newLoadedRoundsInChambers.Add((FireArmRoundClass)round);
                    }
                    firearmPhysicalObject.SetLoadedChambers(newLoadedRoundsInChambers);
                }

                // Magazine/Clip
                if (item.PhysicalObject.ammoContainer != null)
                {
                    int parsedContainerID = -1;
                    GameObject containerPrefabToUse = null;
                    if (int.TryParse(item.PhysicalObject.ammoContainer.itemID, out parsedContainerID))
                    {
                        // Custom mag, fetch from our own assets
                        containerPrefabToUse = Mod.itemPrefabs[parsedContainerID];
                    }
                    else
                    {
                        // Vanilla mag, fetch from game assets
                        containerPrefabToUse = IM.OD[item.PhysicalObject.ammoContainer.itemID].GetGameObject();
                    }

                    GameObject containerObject = Instantiate<GameObject>(containerPrefabToUse);
                    FVRPhysicalObject containerPhysicalObject = containerObject.GetComponentInChildren<FVRPhysicalObject>();

                    if (firearmPhysicalObject.UsesClips && containerPhysicalObject is FVRFireArmClip)
                    {
                        Transform gunClipTransform = firearmPhysicalObject.ClipMountPos;

                        containerObject.transform.position = gunClipTransform.position;
                        containerObject.transform.rotation = gunClipTransform.rotation;

                        FVRFireArmClip clipPhysicalObject = containerPhysicalObject as FVRFireArmClip;

                        if (item.PhysicalObject.ammoContainer.loadedRoundsInContainer != null)
                        {
                            List<FireArmRoundClass> newLoadedRoundsInClip = new List<FireArmRoundClass>();
                            foreach (int round in item.PhysicalObject.ammoContainer.loadedRoundsInContainer)
                            {
                                newLoadedRoundsInClip.Add((FireArmRoundClass)round);
                            }
                            clipPhysicalObject.ReloadClipWithList(newLoadedRoundsInClip);
                        }
                        else
                        {
                            while (clipPhysicalObject.m_numRounds > 0)
                            {
                                clipPhysicalObject.RemoveRound();
                            }
                        }

                        clipPhysicalObject.Load(firearmPhysicalObject);
                        clipPhysicalObject.IsInfinite = false;
                    }
                    else if (containerPhysicalObject is FVRFireArmMagazine)
                    {
                        Mod.instance.LogInfo("\tFirearm has mag");
                        FVRFireArmMagazine magPhysicalObject = containerPhysicalObject as FVRFireArmMagazine;
                        Mod.instance.LogInfo("\tis mag null?: " + (magPhysicalObject == null));
                        Transform gunMagTransform = firearmPhysicalObject.GetMagMountPos(magPhysicalObject.IsBeltBox);
                        Mod.instance.LogInfo("\tgunMagTransform null?: " + (gunMagTransform == null));

                        containerObject.transform.position = gunMagTransform.position;
                        containerObject.transform.rotation = gunMagTransform.rotation;

                        if (item.PhysicalObject.ammoContainer.loadedRoundsInContainer != null)
                        {
                            Mod.instance.LogInfo("\t\tmag has rounds list");
                            List<FireArmRoundClass> newLoadedRoundsInMag = new List<FireArmRoundClass>();
                            foreach (int round in item.PhysicalObject.ammoContainer.loadedRoundsInContainer)
                            {
                                newLoadedRoundsInMag.Add((FireArmRoundClass)round);
                            }
                            magPhysicalObject.ReloadMagWithList(newLoadedRoundsInMag);
                        }
                        else
                        {
                            Mod.instance.LogInfo("\t\tmag has no rounds list, removing all default rounds");
                            while (magPhysicalObject.m_numRounds > 0)
                            {
                                magPhysicalObject.RemoveRound();
                            }
                        }

                        magPhysicalObject.Load(firearmPhysicalObject);
                        magPhysicalObject.IsInfinite = false;
                    }
                }
            }
            else if (itemPhysicalObject is FVRFireArmMagazine)
            {
                FVRFireArmMagazine magPhysicalObject = (itemPhysicalObject as FVRFireArmMagazine);

                if (item.PhysicalObject.loadedRoundsInContainer != null)
                {
                    List<FireArmRoundClass> newLoadedRoundsInMag = new List<FireArmRoundClass>();
                    foreach (int round in item.PhysicalObject.loadedRoundsInContainer)
                    {
                        newLoadedRoundsInMag.Add((FireArmRoundClass)round);
                    }
                    magPhysicalObject.ReloadMagWithList(newLoadedRoundsInMag);
                }
                else
                {
                    while (magPhysicalObject.m_numRounds > 0)
                    {
                        magPhysicalObject.RemoveRound();
                    }
                }
            }
            else if (itemPhysicalObject is FVRFireArmClip)
            {
                FVRFireArmClip clipPhysicalObject = (itemPhysicalObject as FVRFireArmClip);

                if (item.PhysicalObject.loadedRoundsInContainer != null)
                {
                    List<FireArmRoundClass> newLoadedRoundsInClip = new List<FireArmRoundClass>();
                    foreach (int round in item.PhysicalObject.loadedRoundsInContainer)
                    {
                        newLoadedRoundsInClip.Add((FireArmRoundClass)round);
                    }
                    clipPhysicalObject.ReloadClipWithList(newLoadedRoundsInClip);
                }
                else
                {
                    while (clipPhysicalObject.m_numRounds > 0)
                    {
                        clipPhysicalObject.RemoveRound();
                    }
                }
            }
            else if (itemPhysicalObject is Speedloader)
            {
                Speedloader SLPhysicalObject = (itemPhysicalObject as Speedloader);

                if (item.PhysicalObject.loadedRoundsInContainer != null)
                {
                    for (int j = 0; j < item.PhysicalObject.loadedRoundsInContainer.Count; ++j)
                    {
                        int currentRound = item.PhysicalObject.loadedRoundsInContainer[j];
                        SpeedloaderChamber currentChamber = SLPhysicalObject.Chambers[j];

                        if (currentRound > 0)
                        {
                            currentChamber.Load((FireArmRoundClass)currentRound);
                        }
                        else if (currentRound == -1)
                        {
                            currentChamber.Unload();
                        }
                        else // Loaded spent
                        {
                            currentChamber.LoadEmpty((FireArmRoundClass)(currentRound * -1 - 2));
                        }
                    }
                }
                else
                {
                    foreach (SpeedloaderChamber chamber in SLPhysicalObject.Chambers)
                    {
                        chamber.Unload();
                    }
                }
            }

            Mod.instance.LogInfo("Processed firearm");

            // Custom item
            EFM_CustomItemWrapper customItemWrapper = itemObject.GetComponent<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                customItemWrapper.itemType = (Mod.ItemType)item.itemType;
                Mod.instance.LogInfo("Has custom item wrapper with type: "+((Mod.ItemType)item.itemType));

                // Armor
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    Mod.instance.LogInfo("is armor");
                    customItemWrapper.armor = item.PhysicalObject.armor;
                    customItemWrapper.maxArmor = item.PhysicalObject.maxArmor;
                }

                // Rig
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    Mod.instance.LogInfo("is rig");
                    if (item.PhysicalObject.quickBeltSlotContents != null)
                    {
                        for (int j = 0; j < item.PhysicalObject.quickBeltSlotContents.Count; ++j)
                        {
                            if (item.PhysicalObject.quickBeltSlotContents[j] == null)
                            {
                                customItemWrapper.itemsInSlots[j] = null;
                            }
                            else
                            {
                                customItemWrapper.itemsInSlots[j] = LoadSavedItem(customItemWrapper.itemObjectsRoot, item.PhysicalObject.quickBeltSlotContents[j]);
                            }
                        }
                    }
                }

                // Backpack
                if (customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.instance.LogInfo("is backpack");
                    if (item.PhysicalObject.backpackContents != null)
                    {
                        for (int j = 0; j < item.PhysicalObject.backpackContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.itemObjectsRoot, item.PhysicalObject.backpackContents[j]);
                        }
                    }
                }

                // Equip the item if it has an equip slot
                if(item.PhysicalObject.equipSlot != -1)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[item.PhysicalObject.equipSlot];
                    itemPhysicalObject.SetQuickBeltSlot(equipSlot);
                    itemPhysicalObject.SetParentage(equipSlot.QuickbeltRoot);

                    for(int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if (customItemWrapper.itemsInSlots[i] != null)
                        {
                            FVRPhysicalObject currentItemPhysObj = customItemWrapper.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                            if (currentItemPhysObj != null)
                            {
                                // Attach item to quick slot
                                FVRQuickBeltSlot quickBeltSlot = GM.CurrentPlayerBody.QuickbeltSlots[i];
                                currentItemPhysObj.SetQuickBeltSlot(quickBeltSlot);
                                currentItemPhysObj.SetParentage(quickBeltSlot.QuickbeltRoot);
                            }
                        }
                    }
                }
            }

            // GameObject
            itemObject.transform.localPosition = new Vector3(item.PhysicalObject.positionX, item.PhysicalObject.positionY, item.PhysicalObject.positionZ);
            itemObject.transform.localRotation = Quaternion.Euler(new Vector3(item.PhysicalObject.rotationX, item.PhysicalObject.rotationY, item.PhysicalObject.rotationZ));

            return itemObject;
        }

        private void AddAttachments(FVRPhysicalObject physicalObject, PhysicalObject loadedPhysicalObject)
        {
            if(loadedPhysicalObject.AttachmentsList == null)
            {
                return;
            }

            Transform root = physicalObject.transform;
            for (int i = 0; i < loadedPhysicalObject.AttachmentsList.Count; ++i)
            {
                PhysicalObject currentPhysicalObject = loadedPhysicalObject.AttachmentsList[i];
                int parsedID = -1;
                GameObject prefabToUse = null;
                if (int.TryParse(currentPhysicalObject.ObjectWrapper.ItemID, out parsedID))
                {
                    // Custom item, fetch from our own assets
                    prefabToUse = Mod.itemPrefabs[parsedID];
                }
                else
                {
                    // Vanilla item, fetch from game assets
                    prefabToUse = IM.OD[currentPhysicalObject.ObjectWrapper.ItemID].GetGameObject();
                }

                GameObject itemObject = Instantiate<GameObject>(prefabToUse, root);

                FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
                FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;

                // Fill data
                // GameObject
                itemObject.transform.localPosition = new Vector3(currentPhysicalObject.positionX, currentPhysicalObject.positionY, currentPhysicalObject.positionZ);
                itemObject.transform.localRotation = Quaternion.Euler(new Vector3(currentPhysicalObject.rotationX, currentPhysicalObject.rotationY, currentPhysicalObject.rotationZ));

                // PhysicalObject
                itemPhysicalObject.m_isSpawnLock = currentPhysicalObject.m_isSpawnLock;
                itemPhysicalObject.m_isHardnessed = currentPhysicalObject.m_isHarnessed;
                itemPhysicalObject.IsKinematicLocked = currentPhysicalObject.IsKinematicLocked;
                itemPhysicalObject.IsInWater = currentPhysicalObject.IsInWater;
                AddAttachments(itemPhysicalObject, currentPhysicalObject);
                FVRFireArmAttachment itemAttachment = itemPhysicalObject as FVRFireArmAttachment;
                itemAttachment.AttachToMount(physicalObject.AttachmentMounts[currentPhysicalObject.mountIndex], false);
                if(itemAttachment is Suppressor)
                {
                    (itemAttachment as Suppressor).AutoMountWell();
                }
                
                // ObjectWrapper
                itemObjectWrapper.ItemID = currentPhysicalObject.ObjectWrapper.ItemID;
            }
        }

        public override void InitUI()
        {
            // Main hideout menu
            buttons = new Button[8][];
            buttons[0] = new Button[4];
            buttons[1] = new Button[7];
            buttons[2] = new Button[6];
            buttons[3] = new Button[3];
            buttons[4] = new Button[2];
            buttons[5] = new Button[3];
            buttons[6] = new Button[2];
            buttons[7] = new Button[1];

            // Fetch buttons
            canvas = transform.GetChild(0).GetChild(0);
            buttons[0][0] = canvas.GetChild(0).GetChild(1).GetComponent<Button>(); // Raid
            buttons[0][1] = canvas.GetChild(0).GetChild(2).GetComponent<Button>(); // Save
            buttons[0][2] = canvas.GetChild(0).GetChild(3).GetComponent<Button>(); // Load
            buttons[0][3] = canvas.GetChild(0).GetChild(4).GetComponent<Button>(); // Base Back

            buttons[1][0] = canvas.GetChild(1).GetChild(1).GetComponent<Button>(); // Load Slot 0
            buttons[1][1] = canvas.GetChild(1).GetChild(2).GetComponent<Button>(); // Load Slot 1
            buttons[1][2] = canvas.GetChild(1).GetChild(3).GetComponent<Button>(); // Load Slot 2
            buttons[1][3] = canvas.GetChild(1).GetChild(4).GetComponent<Button>(); // Load Slot 3
            buttons[1][4] = canvas.GetChild(1).GetChild(5).GetComponent<Button>(); // Load Slot 4
            buttons[1][5] = canvas.GetChild(1).GetChild(6).GetComponent<Button>(); // Load Auto save
            buttons[1][6] = canvas.GetChild(1).GetChild(7).GetComponent<Button>(); // Load Back

            buttons[2][0] = canvas.GetChild(2).GetChild(1).GetComponent<Button>(); // Save Slot 0
            buttons[2][1] = canvas.GetChild(2).GetChild(2).GetComponent<Button>(); // Save Slot 1
            buttons[2][2] = canvas.GetChild(2).GetChild(3).GetComponent<Button>(); // Save Slot 2
            buttons[2][3] = canvas.GetChild(2).GetChild(4).GetComponent<Button>(); // Save Slot 3
            buttons[2][4] = canvas.GetChild(2).GetChild(5).GetComponent<Button>(); // Save Slot 4
            buttons[2][5] = canvas.GetChild(2).GetChild(6).GetComponent<Button>(); // Save Back

            buttons[3][0] = canvas.GetChild(3).GetChild(1).GetComponent<Button>(); // Char Main
            buttons[3][1] = canvas.GetChild(3).GetChild(2).GetComponent<Button>(); // Char Raider
            buttons[3][2] = canvas.GetChild(3).GetChild(3).GetComponent<Button>(); // Char Back

            buttons[4][0] = canvas.GetChild(4).GetChild(1).GetComponent<Button>(); // Map Mansion
            buttons[4][1] = canvas.GetChild(4).GetChild(2).GetComponent<Button>(); // Map Back

            buttons[5][0] = canvas.GetChild(5).GetChild(1).GetComponent<Button>(); // Time 0
            buttons[5][1] = canvas.GetChild(5).GetChild(2).GetComponent<Button>(); // Time 1
            buttons[5][2] = canvas.GetChild(5).GetChild(3).GetComponent<Button>(); // Time Back

            buttons[6][0] = canvas.GetChild(6).GetChild(1).GetComponent<Button>(); // Confirm
            buttons[6][1] = canvas.GetChild(6).GetChild(2).GetComponent<Button>(); // Confirm Back

            buttons[7][0] = canvas.GetChild(7).GetChild(1).GetComponent<Button>(); // Loading Cancel

            // Create an FVRPointableButton for each button
            for (int i = 0; i < buttons.Length; ++i)
            {
                for (int j = 0; j < buttons[i].Length; ++j)
                {
                    FVRPointableButton pointableButton = buttons[i][j].gameObject.AddComponent<FVRPointableButton>();
                    pointableButton.SetButton();
                    pointableButton.SetText();
                    pointableButton.SetRenderer();
                    pointableButton.ColorSelected = Color.white;
                    pointableButton.ColorUnselected = Color.white;
                    pointableButton.MaxPointingRange = 10;
                }
            }

            // Set OnClick for each button
            buttons[0][0].onClick.AddListener(OnRaidClicked);
            buttons[0][1].onClick.AddListener(OnSaveClicked);
            buttons[0][2].onClick.AddListener(OnLoadClicked);
            buttons[0][3].onClick.AddListener(() => { OnBackClicked(0); });

            buttons[1][0].onClick.AddListener(() => { OnLoadSlotClicked(0); });
            buttons[1][1].onClick.AddListener(() => { OnLoadSlotClicked(1); });
            buttons[1][2].onClick.AddListener(() => { OnLoadSlotClicked(2); });
            buttons[1][3].onClick.AddListener(() => { OnLoadSlotClicked(3); });
            buttons[1][4].onClick.AddListener(() => { OnLoadSlotClicked(4); });
            buttons[1][5].onClick.AddListener(() => { OnLoadSlotClicked(5); });
            buttons[1][6].onClick.AddListener(() => { OnBackClicked(1); });

            buttons[2][0].onClick.AddListener(() => { OnSaveSlotClicked(0); });
            buttons[2][1].onClick.AddListener(() => { OnSaveSlotClicked(1); });
            buttons[2][2].onClick.AddListener(() => { OnSaveSlotClicked(2); });
            buttons[2][3].onClick.AddListener(() => { OnSaveSlotClicked(3); });
            buttons[2][4].onClick.AddListener(() => { OnSaveSlotClicked(4); });
            buttons[2][5].onClick.AddListener(() => { OnBackClicked(2); });

            buttons[3][0].onClick.AddListener(() => { OnCharClicked(0); });
            buttons[3][1].onClick.AddListener(() => { OnCharClicked(1); });
            buttons[3][2].onClick.AddListener(() => { OnBackClicked(3); });

            buttons[4][0].onClick.AddListener(() => { OnMapClicked(0); });
            buttons[4][1].onClick.AddListener(() => { OnBackClicked(4); });

            buttons[5][0].onClick.AddListener(() => { OnTimeClicked(0); });
            buttons[5][1].onClick.AddListener(() => { OnTimeClicked(1); });
            buttons[5][2].onClick.AddListener(() => { OnBackClicked(5); });

            buttons[6][0].onClick.AddListener(OnConfirmRaidClicked);
            buttons[6][1].onClick.AddListener(() => { OnBackClicked(6); });

            buttons[7][0].onClick.AddListener(OnCancelRaidLoadClicked);

            // Set buttons activated depending on presence of save files
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    buttons[1][i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
            }
            else
            {
                buttons[0][2].gameObject.SetActive(false);
            }

            // Keep references we need
            raidCountdownTitle = canvas.GetChild(7).GetChild(5).GetComponent<Text>();
            raidCountdown = canvas.GetChild(7).GetChild(6).GetComponent<Text>();
            timeChoice0 = canvas.GetChild(5).GetChild(1).GetComponentInChildren<Text>();
            timeChoice1 = canvas.GetChild(5).GetChild(2).GetComponentInChildren<Text>();
            chosenCharacter = canvas.GetChild(6).GetChild(3).GetComponentInChildren<Text>();
            chosenMap = canvas.GetChild(6).GetChild(4).GetComponentInChildren<Text>();
            chosenTime = canvas.GetChild(6).GetChild(5).GetComponentInChildren<Text>();

            // Areas
            if(areaCanvasPrefab == null)
            {
                Mod.instance.LogInfo("Area canvas not initialized, initializing all area UI and prepping...");

                // Load prefabs and assets
                areaCanvasPrefab = Mod.baseBundle.LoadAsset<GameObject>("AreaCanvas");
                areaCanvasBottomButtonPrefab = Mod.baseBundle.LoadAsset<GameObject>("AreaCanvasBottomButton");
                areaRequirementPrefab = Mod.baseBundle.LoadAsset<GameObject>("AreaRequirement");
                itemRequirementPrefab = Mod.baseBundle.LoadAsset<GameObject>("ItemRequirement");
                skillRequirementPrefab = Mod.baseBundle.LoadAsset<GameObject>("SkillRequirement");
                traderRequirementPrefab = Mod.baseBundle.LoadAsset<GameObject>("TraderRequirement");
                areaRequirementsPrefab = Mod.baseBundle.LoadAsset<GameObject>("AreaRequirements");
                itemRequirementsPrefab = Mod.baseBundle.LoadAsset<GameObject>("ItemRequirements");
                skillRequirementsPrefab = Mod.baseBundle.LoadAsset<GameObject>("SkillRequirements");
                traderRequirementsPrefab = Mod.baseBundle.LoadAsset<GameObject>("TraderRequirements");
                bonusPrefab = Mod.baseBundle.LoadAsset<GameObject>("Bonus");
                areaBackgroundNormalSprite = Mod.baseBundle.LoadAsset<Sprite>("area_icon_default_back");
                areaBackgroundLockedSprite = Mod.baseBundle.LoadAsset<Sprite>("area_icon_locked_back");
                areaBackgroundAvailableSprite = Mod.baseBundle.LoadAsset<Sprite>("area_icon_default_back_green");
                areaBackgroundEliteSprite = Mod.baseBundle.LoadAsset<Sprite>("area_icon_elite_back");
                areaStatusIconUpgrading = Mod.baseBundle.LoadAsset<Sprite>("icon_status_upgrading");
                areaStatusIconConstructing = Mod.baseBundle.LoadAsset<Sprite>("icon_status_constructing");
                areaStatusIconLocked = Mod.baseBundle.LoadAsset<Sprite>("icon_lock");
                areaStatusIconUnlocked = Mod.baseBundle.LoadAsset<Sprite>("icon_status_unlocked");
                areaStatusIconReadyUpgrade = Mod.baseBundle.LoadAsset<Sprite>("icon_status_ready_to_upgrade");
                areaStatusIconProducing = Mod.baseBundle.LoadAsset<Sprite>("icon_status_producing");
                areaStatusIconOutOfFuel = Mod.baseBundle.LoadAsset<Sprite>("icon_out_of_fuel");
                requirementFulfilled = Mod.baseBundle.LoadAsset<Sprite>("icon_requirement_fulfilled");
                requirementLocked = Mod.baseBundle.LoadAsset<Sprite>("icon_requirement_locked");
                traderAvatars = new Sprite[8];
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_russian_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_therapist_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_fence_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_ah_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_peacekeeper_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_tech_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_ragman_small");
                traderAvatars[0] = Mod.baseBundle.LoadAsset<Sprite>("avatar_jaeger_small");
                areaIcons = new Sprite[22];
                areaIcons[0] = Mod.baseBundle.LoadAsset<Sprite>("icon_vents");
                areaIcons[1] = Mod.baseBundle.LoadAsset<Sprite>("icon_security");
                areaIcons[2] = Mod.baseBundle.LoadAsset<Sprite>("icon_watercloset");
                areaIcons[3] = Mod.baseBundle.LoadAsset<Sprite>("icon_stash");
                areaIcons[4] = Mod.baseBundle.LoadAsset<Sprite>("icon_generators");
                areaIcons[5] = Mod.baseBundle.LoadAsset<Sprite>("icon_heating");
                areaIcons[6] = Mod.baseBundle.LoadAsset<Sprite>("icon_rain_collector");
                areaIcons[7] = Mod.baseBundle.LoadAsset<Sprite>("icon_medstation");
                areaIcons[8] = Mod.baseBundle.LoadAsset<Sprite>("icon_kitchen");
                areaIcons[9] = Mod.baseBundle.LoadAsset<Sprite>("icon_restplace");
                areaIcons[10] = Mod.baseBundle.LoadAsset<Sprite>("icon_workbench");
                areaIcons[11] = Mod.baseBundle.LoadAsset<Sprite>("icon_intelligence_center");
                areaIcons[12] = Mod.baseBundle.LoadAsset<Sprite>("icon_shooting_range");
                areaIcons[13] = Mod.baseBundle.LoadAsset<Sprite>("icon_library");
                areaIcons[14] = Mod.baseBundle.LoadAsset<Sprite>("icon_scav_case");
                areaIcons[15] = Mod.baseBundle.LoadAsset<Sprite>("icon_illumination");
                areaIcons[16] = Mod.baseBundle.LoadAsset<Sprite>("icon_placeoffame");
                areaIcons[17] = Mod.baseBundle.LoadAsset<Sprite>("icon_afu");
                areaIcons[18] = Mod.baseBundle.LoadAsset<Sprite>("icon_solarpower");
                areaIcons[19] = Mod.baseBundle.LoadAsset<Sprite>("icon_boozegen");
                areaIcons[20] = Mod.baseBundle.LoadAsset<Sprite>("icon_bitcionfarm");
                areaIcons[21] = Mod.baseBundle.LoadAsset<Sprite>("icon_christmas_illumination");
                bonusIcons = new Dictionary<string, Sprite>();
                bonusIcons.Add("ExperienceRate", Mod.baseBundle.LoadAsset<Sprite>("icon_exp_small"));
                bonusIcons.Add("HealthRegeneration", Mod.baseBundle.LoadAsset<Sprite>("icon_medical"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_meds.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_createitem_meds"));
                bonusIcons.Add("/files/Hideout/icon_hideout_videocardslots.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_videocardslots"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_bitcoin.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_createitem_bitcoin"));
                bonusIcons.Add("/files/Hideout/icon_hideout_unlocked.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_unlocked"));
                bonusIcons.Add("FuelConsumption", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_fuelconsumption"));
                bonusIcons.Add("/files/Hideout/icon_hideout_fuelslots.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_fuelslots"));
                bonusIcons.Add("EnergyRegeneration", Mod.baseBundle.LoadAsset<Sprite>("icon_info_energy"));
                bonusIcons.Add("HydrationRegeneration", Mod.baseBundle.LoadAsset<Sprite>("icon_info_hydration"));
                bonusIcons.Add("/files/Hideout/icon_hideout_shootingrangeunlock.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_shootingrangeunlock"));
                bonusIcons.Add("DebuffEndDelay", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
                bonusIcons.Add("UnlockWeaponModification", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_weaponmodunlock"));
                bonusIcons.Add("SkillGroupLevelingBoost", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
                bonusIcons.Add("AdditionalSlots", Mod.baseBundle.LoadAsset<Sprite>("skills_grid_icon"));
                bonusIcons.Add("StashSize", Mod.baseBundle.LoadAsset<Sprite>("skills_grid_icon"));
                bonusIcons.Add("/files/Hideout/icon_hideout_scavitem.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_scavitem"));
                bonusIcons.Add("ScavCooldownTimer", Mod.baseBundle.LoadAsset<Sprite>("icon_time"));
                bonusIcons.Add("InsuranceReturnTime", Mod.baseBundle.LoadAsset<Sprite>("icon_time"));
                bonusIcons.Add("QuestMoneyReward", Mod.baseBundle.LoadAsset<Sprite>("icon_info_money"));
                bonusIcons.Add("RagfairCommission", Mod.baseBundle.LoadAsset<Sprite>("icon_info_money"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_generic.png", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_createitem_generic"));
                bonusIcons.Add("MaximumEnergyReserve", Mod.baseBundle.LoadAsset<Sprite>("icon_hideout_batterycharge"));
                skillIcons = new Sprite[64];
                skillIcons[0] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_endurance");
                skillIcons[1] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_strength");
                skillIcons[2] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_vitality");
                skillIcons[3] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_health");
                skillIcons[4] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_stressresistance");
                skillIcons[5] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_metabolism");
                skillIcons[6] = Mod.baseBundle.LoadAsset<Sprite>("skill_physical_immunity");
                skillIcons[7] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_perception");
                skillIcons[8] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_intellect");
                skillIcons[9] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_attention");
                skillIcons[10] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_charisma");
                skillIcons[11] = Mod.baseBundle.LoadAsset<Sprite>("skill_mental_memory");
                skillIcons[12] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_pistols");
                skillIcons[13] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_revolvers");
                skillIcons[14] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_smgs");
                skillIcons[15] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_assaultrifles");
                skillIcons[16] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_shotguns");
                skillIcons[17] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_sniperrifles");
                skillIcons[18] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_lmgs");
                skillIcons[19] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_hmgs");
                skillIcons[20] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_launchers");
                skillIcons[21] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_ugls");
                skillIcons[22] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_grenades");
                skillIcons[23] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_melee");
                skillIcons[24] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_dmrs");
                skillIcons[25] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_recoilcontrol");
                skillIcons[26] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_weapondrawing");
                skillIcons[27] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_troubleshooting");
                skillIcons[28] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_surgery");
                skillIcons[29] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_covertmovement");
                skillIcons[30] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_search");
                skillIcons[31] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_magdrills");
                skillIcons[32] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_sniping");
                skillIcons[33] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_pronemovement");
                skillIcons[34] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_fieldmedical");
                skillIcons[35] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_basicmedical");
                skillIcons[36] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_lightarmor");
                skillIcons[37] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_heavyarmor");
                skillIcons[38] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_basicweaponmodding");
                skillIcons[39] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_advancedweaponmodding");
                skillIcons[40] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_nightoperations");
                skillIcons[41] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_silentoperations");
                skillIcons[42] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_lockpicking");
                skillIcons[43] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_weapontreatment");
                skillIcons[44] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_freetrading");
                skillIcons[45] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_auctions");
                skillIcons[46] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_cleanoperations");
                skillIcons[47] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_barter");
                skillIcons[48] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_shadowconnections");
                skillIcons[49] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_taskperformance");
                skillIcons[50] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_crafting");
                skillIcons[51] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_hideoutmanagement");
                skillIcons[52] = Mod.baseBundle.LoadAsset<Sprite>("skill_combat_weaponswitch");
                skillIcons[53] = Mod.baseBundle.LoadAsset<Sprite>("skill_practical_equipmentmanagement");
                skillIcons[54] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_bear_aksystems");
                skillIcons[55] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_bear_assaultoperations");
                skillIcons[56] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_bear_authority");
                skillIcons[57] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_bear_heavycaliber");
                skillIcons[58] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_bear_rawpower");
                skillIcons[59] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_usec_arsystems");
                skillIcons[60] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_usec_deepweaponmodding");
                skillIcons[61] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_usec_longrangeoptics");
                skillIcons[62] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_usec_negotiations");
                skillIcons[63] = Mod.baseBundle.LoadAsset<Sprite>("skill_special_usec_tactics");

                // Prep prefabs
                Mod.instance.LogInfo("All area UI loaded, prepping...");

                // AreaCanvasPrefab
                EFM_BaseAreaManager areaManager = areaCanvasPrefab.AddComponent<EFM_BaseAreaManager>();
                areaManager.buttonClickSound = areaCanvasPrefab.transform.GetChild(3).GetComponent<AudioSource>();

                GameObject summaryButtonObject = areaCanvasPrefab.transform.GetChild(0).GetChild(2).gameObject;
                EFM_PointableButton summaryPointableButton = summaryButtonObject.AddComponent<EFM_PointableButton>();
                summaryPointableButton.SetButton();
                summaryPointableButton.MaxPointingRange = 30;
                summaryPointableButton.hoverGraphics = new GameObject[2];
                summaryPointableButton.hoverGraphics[0] = summaryButtonObject.transform.GetChild(0).gameObject;
                summaryPointableButton.hoverGraphics[1] = areaCanvasPrefab.transform.GetChild(0).GetChild(1).gameObject;
                summaryPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                GameObject fullCloseButtonObject = areaCanvasPrefab.transform.GetChild(1).GetChild(0).GetChild(1).gameObject;
                EFM_PointableButton fullClosePointableButton = fullCloseButtonObject.AddComponent<EFM_PointableButton>();
                fullClosePointableButton.SetButton();
                fullClosePointableButton.MaxPointingRange = 30;
                fullClosePointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                // AreaCanvasBottomButtonPrefab
                EFM_PointableButton bottomPointableButton = areaCanvasBottomButtonPrefab.AddComponent<EFM_PointableButton>();
                bottomPointableButton.SetButton();
                bottomPointableButton.MaxPointingRange = 30;
                bottomPointableButton.hoverGraphics = new GameObject[1];
                bottomPointableButton.hoverGraphics[0] = areaCanvasBottomButtonPrefab.transform.GetChild(0).gameObject;
                bottomPointableButton.buttonText = areaCanvasBottomButtonPrefab.transform.GetChild(1).GetComponent<Text>();
                bottomPointableButton.toggleTextColor = true;

                Mod.instance.LogInfo("Area UI prepped");
            }

            Mod.instance.LogInfo("Initializing area managers");
            // Init all area managers after UI is prepped
            for (int i = 0; i < 22; ++i)
            {
                baseAreaManagers[i].Init();
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
            int scaledTime = (int)((clampedTime * EFM_Manager.meatovTimeMultiplier) % 86400);
            time = scaledTime;
        }

        public void OnSaveClicked()
        {
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(2).gameObject.SetActive(true);
        }

        public void OnSaveSlotClicked(int slotIndex)
        {
            Mod.saveSlotIndex = slotIndex;
            SaveBase();
        }

        public void OnLoadClicked()
        {
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(1).gameObject.SetActive(true);
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            ResetPlayerRig();
            LoadBase(gameObject, slotIndex);
        }

        public void OnRaidClicked()
        {
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(3).gameObject.SetActive(true);
        }

        public void OnBackClicked(int backIndex)
        {
            switch (backIndex)
            {
                case 0:
                    GameObject menuObject = Instantiate<GameObject>(Mod.scenePrefab_Menu);
                    menuObject.name = Mod.scenePrefab_Menu.name;

                    EFM_Menu_Manager menuManager = menuObject.AddComponent<EFM_Menu_Manager>();
                    menuManager.Init();

                    Transform spawnPoint = menuObject.transform.GetChild(menuObject.transform.childCount - 1).GetChild(0);
                    GM.CurrentMovementManager.TeleportToPoint(spawnPoint.position, true, spawnPoint.rotation.eulerAngles);

                    // Unload base
                    ResetPlayerRig();
                    Destroy(gameObject);
                    break;
                case 1:
                case 2:
                case 3:
                    canvas.GetChild(backIndex).gameObject.SetActive(false);
                    canvas.GetChild(0).gameObject.SetActive(true);
                    break;
                case 4:
                case 5:
                case 6:
                    canvas.GetChild(backIndex).gameObject.SetActive(false);
                    canvas.GetChild(backIndex - 1).gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        public void OnCharClicked(int charIndex)
        {
            chosenCharIndex = charIndex;

            // Update chosen char text
            chosenCharacter.text = charIndex == 0 ? "Raider" : "Scavenger";

            canvas.GetChild(3).gameObject.SetActive(false);
            canvas.GetChild(4).gameObject.SetActive(true);
        }

        public void OnMapClicked(int mapIndex)
        {
            chosenMapIndex = mapIndex;

            // Update chosen map text
            switch (chosenMapIndex)
            {
                case 0:
                    chosenMap.text = "Factory";
                    break;
                default:
                    break;
            }

            canvas.GetChild(4).gameObject.SetActive(false);
            canvas.GetChild(5).gameObject.SetActive(true);
        }

        public void OnTimeClicked(int timeIndex)
        {
            chosenTimeIndex = timeIndex;
            canvas.GetChild(5).gameObject.SetActive(false);
            canvas.GetChild(6).gameObject.SetActive(true);
        }

        public void OnConfirmRaidClicked()
        {
            canvas.GetChild(6).gameObject.SetActive(false);
            canvas.GetChild(7).gameObject.SetActive(true);

            // Begin loading raid map
            switch (chosenMapIndex)
            {
                case 0:
                    loadingRaid = true;
                    currentRaidBundleRequest = AssetBundle.LoadFromFileAsync("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovFactory.ab");
                    break;
                default:
                    break;
            }
        }

        public void OnCancelRaidLoadClicked()
        {
            cancelRaidLoad = true;
        }

        private void ResetPlayerRig()
        {
            // Destroy and reset rig and equipment slots
            EFM_EquipmentSlot.Clear();
            for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
            {
                if (Mod.equipmentSlots[i] != null)
                {
                    // Unlike normal rig slots, items in equip slots are parented to their slots, so the destruction of slot will destroy the item
                    Destroy(Mod.equipmentSlots[i].gameObject);
                }
            }
            Mod.equipmentSlots.Clear();
            GM.CurrentPlayerBody.ConfigureQuickbelt(-2); // -2 in order to detroy the objects on belt as well
        }

        private void SaveBase()
        {
            // Update time
            data.time = GetTimeSeconds();

            // Save areas
            if (data.areas == null)
            {
                data.areas = new List<SavedArea>();
            }
            else
            {
                data.areas.Clear();
            }
            for (int i = 0; i < baseAreaManagers.Count; ++i)
            {
                SavedArea currentSavedArea = new SavedArea();
                currentSavedArea.level = baseAreaManagers[i].level;
                currentSavedArea.constructing = baseAreaManagers[i].constructing;
                currentSavedArea.constructTime = baseAreaManagers[i].constructTime;
                if(baseAreaManagers[i].slotItems != null)
                {
                    currentSavedArea.slots = new List<SavedItem>();
                    foreach(GameObject slotItem in baseAreaManagers[i].slotItems)
                    {
                        if(slotItem == null)
                        {
                            currentSavedArea.slots.Add(null);
                        }
                        else
                        {
                            SaveItem(currentSavedArea.slots, slotItem.transform);
                        }
                    }
                }
                data.areas.Add(currentSavedArea);
            }

            // Save trader statuses
            if (data.traderStatuses == null)
            {
                data.traderStatuses = new List<SavedTraderStatus>();
            }
            else
            {
                data.traderStatuses.Clear();
            }
            for(int i=0; i<8; ++i)
            {
                SavedTraderStatus currentSavedTraderStatus = new SavedTraderStatus();
                currentSavedTraderStatus.id = traderStatuses[i].id;
                currentSavedTraderStatus.salesSum = traderStatuses[i].salesSum;
                currentSavedTraderStatus.standing = traderStatuses[i].standing;
                currentSavedTraderStatus.unlocked = traderStatuses[i].unlocked;

                data.traderStatuses.Add(currentSavedTraderStatus);
            }

            // Reset save data item list
            if (data.items == null)
            {
                data.items = new List<SavedItem>();
            }
            else
            {
                data.items.Clear();
            }

            // Save loose items
            Transform itemsRoot = transform.GetChild(2);

            // Create a SavedItem for each item and fill it with minimum data needed to spawn it on load
            Mod.instance.LogInfo("Saving items on items root");
            for (int i = 0; i < itemsRoot.childCount; ++i)
            {
                SaveItem(data.items, itemsRoot.GetChild(i));
            }

            // Save items in hands
            FVRViveHand rightHand = GM.CurrentPlayerBody.RightHand.GetComponentInChildren<FVRViveHand>();
            FVRViveHand leftHand = GM.CurrentPlayerBody.LeftHand.GetComponentInChildren<FVRViveHand>();
            if (rightHand.CurrentInteractable != null)
            {
                SaveItem(data.items, rightHand.CurrentInteractable.transform, rightHand);
            }
            if (leftHand.CurrentInteractable != null)
            {
                SaveItem(data.items, leftHand.CurrentInteractable.transform, leftHand);
            }

            // Save equipment
            Mod.instance.LogInfo("Saving equipment");
            if (EFM_EquipmentSlot.currentRig != null)
            {
                SaveItem(data.items, EFM_EquipmentSlot.currentRig.transform);
            }
            if (EFM_EquipmentSlot.currentHelmet != null)
            {
                SaveItem(data.items, EFM_EquipmentSlot.currentHelmet.transform);
            }
            if (EFM_EquipmentSlot.currentArmor != null && (EFM_EquipmentSlot.currentRig == null || EFM_EquipmentSlot.currentRig.itemType != Mod.ItemType.ArmoredRig))
            {
                SaveItem(data.items, EFM_EquipmentSlot.currentArmor.transform);
            }
            if (EFM_EquipmentSlot.currentBackpack != null)
            {
                SaveItem(data.items, EFM_EquipmentSlot.currentBackpack.transform);
            }

            // Save base areas
            Mod.instance.LogInfo("Saving equipment");


            SaveDataToFile();
        }

        private void SaveItem(List<SavedItem> listToAddTo, Transform item, FVRViveHand hand = null, int quickBeltSlotIndex = -1)
        {
            if(item == null)
            {
                return;
            }
            Mod.instance.LogInfo("Saving item " + item.name);

            SavedItem savedItem = new SavedItem();
            savedItem.PhysicalObject = new PhysicalObject();
            savedItem.PhysicalObject.ObjectWrapper = new ObjectWrapper();

            // Get correct item is held and set heldMode
            FVRPhysicalObject itemPhysicalObject = null;
            if (hand != null)
            {
                Mod.instance.LogInfo("saving hand item: "+item.name);
                if (hand.CurrentInteractable is FVRAlternateGrip)
                {
                    Mod.instance.LogInfo("\tItem is alt grip");
                    // Make sure this item isn't the same as held in right hand because we dont want an item saved twice, and will prioritize right hand
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        Mod.instance.LogInfo("\t\tItem in right hand or different from other hand's item");
                        itemPhysicalObject = (hand.CurrentInteractable as FVRAlternateGrip).PrimaryObject;
                        savedItem.PhysicalObject.heldMode = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\tItem not right hand or same as other hand's item");
                        itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                        savedItem.PhysicalObject.heldMode = 0;
                    }
                }
                else
                {
                    Mod.instance.LogInfo("\tItem is not alt grip");
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        Mod.instance.LogInfo("\t\tItem in right hand or different from other hand's item");
                        itemPhysicalObject = hand.CurrentInteractable as FVRPhysicalObject;
                        savedItem.PhysicalObject.heldMode = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\tItem not right hand or same as other hand's item");
                        itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                        savedItem.PhysicalObject.heldMode = 0;
                    }
                }
            }
            else
            {
                itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                savedItem.PhysicalObject.heldMode = 0;
            }
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;

            // Fill PhysicalObject
            savedItem.PhysicalObject.positionX = item.localPosition.x;
            savedItem.PhysicalObject.positionY = item.localPosition.y;
            savedItem.PhysicalObject.positionZ = item.localPosition.z;
            savedItem.PhysicalObject.rotationX = item.localRotation.eulerAngles.x;
            savedItem.PhysicalObject.rotationY = item.localRotation.eulerAngles.y;
            savedItem.PhysicalObject.rotationZ = item.localRotation.eulerAngles.z;
            savedItem.PhysicalObject.m_isSpawnLock = itemPhysicalObject.m_isSpawnLock;
            savedItem.PhysicalObject.m_isHarnessed = itemPhysicalObject.m_isHardnessed;
            savedItem.PhysicalObject.IsKinematicLocked = itemPhysicalObject.IsKinematicLocked;
            savedItem.PhysicalObject.IsInWater = itemPhysicalObject.IsInWater;
            SaveAttachments(itemPhysicalObject, savedItem.PhysicalObject);
            savedItem.PhysicalObject.m_quickBeltSlot = quickBeltSlotIndex;

            // Fill ObjectWrapper
            savedItem.PhysicalObject.ObjectWrapper.ItemID = itemObjectWrapper.ItemID;

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                FVRFireArm firearmPhysicalObject = itemPhysicalObject as FVRFireArm;

                // Save flagDict by converting it into two lists of string, one for keys and one for values
                Dictionary<string, string> flagDict = firearmPhysicalObject.GetFlagDic();
                if (flagDict != null && flagDict.Count > 0)
                {
                    List<string> flagDictKeys = new List<string>();
                    List<string> flagDictValues = new List<string>();
                    foreach (KeyValuePair<string, string> flagDictEntry in flagDict)
                    {
                        flagDictKeys.Add(flagDictEntry.Key);
                        flagDictValues.Add(flagDictEntry.Value);
                    }
                    savedItem.PhysicalObject.flagDictKeys = flagDictKeys;
                    savedItem.PhysicalObject.flagDictValues = flagDictValues;
                }
                else
                {
                    savedItem.PhysicalObject.flagDictKeys = null;
                    savedItem.PhysicalObject.flagDictValues = null;
                }

                // Chambers
                if (firearmPhysicalObject.GetChamberRoundList() != null && firearmPhysicalObject.GetChamberRoundList().Count > 0)
                {
                    List<int> newLoadedRoundsInChambers = new List<int>();
                    foreach (FireArmRoundClass round in firearmPhysicalObject.GetChamberRoundList())
                    {
                        newLoadedRoundsInChambers.Add((int)round);
                    }
                    savedItem.PhysicalObject.loadedRoundsInChambers = newLoadedRoundsInChambers;
                }
                else
                {
                    savedItem.PhysicalObject.loadedRoundsInChambers = null;
                }

                // Magazine/Clip
                if (firearmPhysicalObject.UsesClips)
                {
                    if (firearmPhysicalObject.Clip != null)
                    {
                        savedItem.PhysicalObject.ammoContainer = new AmmoContainer();
                        savedItem.PhysicalObject.ammoContainer.itemID = firearmPhysicalObject.Clip.ObjectWrapper.ItemID;

                        if (firearmPhysicalObject.Clip.HasARound())
                        {
                            List<int> newLoadedRoundsInClip = new List<int>();
                            foreach (FVRFireArmClip.FVRLoadedRound round in firearmPhysicalObject.Clip.LoadedRounds)
                            {
                                if (round == null)
                                {
                                    break;
                                }
                                else
                                {
                                    newLoadedRoundsInClip.Add((int)round.LR_Class);
                                }
                            }
                            savedItem.PhysicalObject.ammoContainer.loadedRoundsInContainer = newLoadedRoundsInClip;
                        }
                        else
                        {
                            savedItem.PhysicalObject.ammoContainer.loadedRoundsInContainer = null;
                        }
                    }
                    else
                    {
                        savedItem.PhysicalObject.ammoContainer = null;
                    }
                }
                else if (firearmPhysicalObject.Magazine != null)
                {
                    savedItem.PhysicalObject.ammoContainer = new AmmoContainer();
                    savedItem.PhysicalObject.ammoContainer.itemID = firearmPhysicalObject.Magazine.ObjectWrapper.ItemID;

                    if (firearmPhysicalObject.Magazine.HasARound() && firearmPhysicalObject.Magazine.LoadedRounds != null)
                    {
                        List<int> newLoadedRoundsInMag = new List<int>();
                        foreach (FVRLoadedRound round in firearmPhysicalObject.Magazine.LoadedRounds)
                        {
                            if (round == null)
                            {
                                break;
                            }
                            else
                            {
                                newLoadedRoundsInMag.Add((int)round.LR_Class);
                            }
                        }
                        savedItem.PhysicalObject.ammoContainer.loadedRoundsInContainer = newLoadedRoundsInMag;
                    }
                    else
                    {
                        savedItem.PhysicalObject.ammoContainer.loadedRoundsInContainer = null;
                    }
                }
                else
                {
                    savedItem.PhysicalObject.ammoContainer = null;
                }
            }
            else if (itemPhysicalObject is FVRFireArmMagazine)
            {
                FVRFireArmMagazine magPhysicalObject = (itemPhysicalObject as FVRFireArmMagazine);
                if (magPhysicalObject.HasARound())
                {
                    List<int> newLoadedRoundsInMag = new List<int>();
                    foreach (FVRLoadedRound round in magPhysicalObject.LoadedRounds)
                    {
                        if (round == null)
                        {
                            break;
                        }
                        else
                        {
                            newLoadedRoundsInMag.Add((int)round.LR_Class);
                        }
                    }
                    savedItem.PhysicalObject.loadedRoundsInContainer = newLoadedRoundsInMag;
                }
                else
                {
                    savedItem.PhysicalObject.loadedRoundsInContainer = null;
                }
            }
            else if (itemPhysicalObject is FVRFireArmClip)
            {
                FVRFireArmClip clipPhysicalObject = (itemPhysicalObject as FVRFireArmClip);
                if (clipPhysicalObject.HasARound())
                {
                    List<int> newLoadedRoundsInClip = new List<int>();
                    foreach (FVRFireArmClip.FVRLoadedRound round in clipPhysicalObject.LoadedRounds)
                    {
                        if (round == null)
                        {
                            break;
                        }
                        else
                        {
                            newLoadedRoundsInClip.Add((int)round.LR_Class);
                        }
                    }
                    savedItem.PhysicalObject.loadedRoundsInContainer = newLoadedRoundsInClip;
                }
                else
                {
                    savedItem.PhysicalObject.loadedRoundsInContainer = null;
                }

            }
            else if (itemPhysicalObject is Speedloader)
            {
                Speedloader SLPhysicalObject = (itemPhysicalObject as Speedloader);
                if (SLPhysicalObject.Chambers != null)
                {
                    List<int> newLoadedRoundsInSL = new List<int>();
                    foreach (SpeedloaderChamber chamber in SLPhysicalObject.Chambers)
                    {
                        if (chamber.IsLoaded)
                        {
                            if (chamber.IsSpent)
                            {
                                newLoadedRoundsInSL.Add((int)chamber.LoadedClass * -1 - 2); // negative means spent of class: value * -1 - 2
                            }
                            else
                            {
                                newLoadedRoundsInSL.Add((int)chamber.LoadedClass);
                            }
                        }
                        else
                        {
                            newLoadedRoundsInSL.Add(-1); // -1 means not loaded
                        }
                    }
                    savedItem.PhysicalObject.loadedRoundsInContainer = newLoadedRoundsInSL;
                }
                else
                {
                    savedItem.PhysicalObject.loadedRoundsInContainer = null;
                }
            }

            // Custom items
            EFM_CustomItemWrapper customItemWrapper = itemPhysicalObject.gameObject.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                savedItem.itemType = (int)customItemWrapper.itemType;
                savedItem.PhysicalObject.equipSlot = -1;

                // Armor
                if(customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    Mod.instance.LogInfo("Item is armor");

                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    savedItem.PhysicalObject.equipSlot = i;
                                    break;
                                }
                            }
                        }
                    }
                    savedItem.PhysicalObject.armor = customItemWrapper.armor;
                    savedItem.PhysicalObject.maxArmor = customItemWrapper.maxArmor;
                }

                // Rig
                if(customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentRig != null && EFM_EquipmentSlot.currentRig.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    savedItem.PhysicalObject.equipSlot = i;
                                    break;
                                }
                            }
                        }
                    }
                    if(savedItem.PhysicalObject.quickBeltSlotContents == null)
                    {
                        savedItem.PhysicalObject.quickBeltSlotContents = new List<SavedItem>();
                    }
                    for(int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if(customItemWrapper.itemsInSlots[i] == null)
                        {
                            savedItem.PhysicalObject.quickBeltSlotContents.Add(null);
                        }
                        else
                        {
                            SaveItem(savedItem.PhysicalObject.quickBeltSlotContents, customItemWrapper.itemsInSlots[i].transform, null, i);
                        }
                    }
                }

                // Backpack
                if(customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.instance.LogInfo("Item is backpack");

                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentBackpack != null && EFM_EquipmentSlot.currentBackpack.Equals(customItemWrapper))
                    {
                        Mod.instance.LogInfo("is equipped");
                        // Find its equip slot index
                        //TODO: for all wore items we could keep equip slot in customitemwrapper since all equppable items are custom
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    Mod.instance.LogInfo("in slot " + i);
                                    savedItem.PhysicalObject.equipSlot = i;
                                    break;
                                }
                            }
                        }
                    }
                    if(savedItem.PhysicalObject.quickBeltSlotContents == null)
                    {
                        savedItem.PhysicalObject.backpackContents = new List<SavedItem>();
                    }
                    for(int i=0; i < customItemWrapper.itemObjectsRoot.childCount; ++i)
                    {
                        Mod.instance.LogInfo("Item in backpack " + i + ": "+ customItemWrapper.itemObjectsRoot.GetChild(i).name);
                        SaveItem(savedItem.PhysicalObject.backpackContents, customItemWrapper.itemObjectsRoot.GetChild(i));
                    }
                }
            }

            listToAddTo.Add(savedItem);
        }

        private void SaveAttachments(FVRPhysicalObject physicalObject, PhysicalObject itemPhysicalObject)
        {
            // We want to save attachments curently physically present on physicalObject into the save data itemPhysicalObject
            for (int i = 0; i < physicalObject.Attachments.Count; ++i)
            {
                PhysicalObject newPhysicalObject = new PhysicalObject();
                if(itemPhysicalObject.AttachmentsList == null)
                {
                    itemPhysicalObject.AttachmentsList = new List<PhysicalObject>();
                }
                itemPhysicalObject.AttachmentsList.Add(newPhysicalObject);

                FVRPhysicalObject currentPhysicalObject = physicalObject.Attachments[i];
                Transform currentTransform = currentPhysicalObject.transform;

                newPhysicalObject.ObjectWrapper = new ObjectWrapper();

                // Fill PhysicalObject
                newPhysicalObject.positionX = currentTransform.localPosition.x;
                newPhysicalObject.positionY = currentTransform.localPosition.y;
                newPhysicalObject.positionZ = currentTransform.localPosition.z;
                newPhysicalObject.rotationX = currentTransform.localRotation.eulerAngles.x;
                newPhysicalObject.rotationY = currentTransform.localRotation.eulerAngles.y;
                newPhysicalObject.rotationZ = currentTransform.localRotation.eulerAngles.z;
                newPhysicalObject.m_isSpawnLock = currentPhysicalObject.m_isSpawnLock;
                newPhysicalObject.m_isHarnessed = currentPhysicalObject.m_isHardnessed;
                newPhysicalObject.IsKinematicLocked = currentPhysicalObject.IsKinematicLocked;
                newPhysicalObject.IsInWater = currentPhysicalObject.IsInWater;
                SaveAttachments(currentPhysicalObject, newPhysicalObject);
                newPhysicalObject.mountIndex = physicalObject.AttachmentMounts.IndexOf((currentPhysicalObject as FVRFireArmAttachment).curMount);

                // Fill ObjectWrapper
                newPhysicalObject.ObjectWrapper.ItemID = currentPhysicalObject.ObjectWrapper.ItemID;
            }
        }

        public void FinishRaid(FinishRaidState state)
        {
            // Increment raid counters
            ++data.totalRaidCount;
            switch (state)
            {
                case FinishRaidState.RunThrough:
                    ++data.runThroughRaidCount;
                    ++data.survivedRaidCount;
                    break;
                case FinishRaidState.Survived:
                    ++data.survivedRaidCount;
                    break;
                case FinishRaidState.MIA:
                    ++data.MIARaidCount;
                    ++data.failedRaidCount;
                    break;
                case FinishRaidState.KIA:
                    ++data.KIARaidCount;
                    ++data.failedRaidCount;
                    break;
                default:
                    break;
            }

            // Save the base
            Mod.saveSlotIndex = 5;
            SaveBase();
        }

        private void SaveDataToFile()
        {
            File.WriteAllText("BepInEx/Plugins/EscapeFromMeatov/" + (Mod.saveSlotIndex == 5 ? "AutoSave" : "Slot" + Mod.saveSlotIndex) + ".sav", JsonConvert.SerializeObject(data));
        }
    }

    // Save data class
    public class SaveData
    {
        public long time { get; set; }
        public int level { get; set; }
        public int totalRaidCount { get; set; }
        public int survivedRaidCount { get; set; }
        public int runThroughRaidCount { get; set; }
        public int failedRaidCount { get; set; }
        public int MIARaidCount { get; set; }
        public int KIARaidCount { get; set; }
        public List<SavedItem> items { get; set; }
        public List<SavedArea> areas { get; set; }
        public List<SavedTraderStatus> traderStatuses { get; set; }
        public List<SavedSkill> skills { get; set; }
    }

    public class ObjectWrapper
    {
        public string ItemID { get; set; }
    }

    public class PhysicalObject
    {
        public ObjectWrapper ObjectWrapper { get; set; }
        public float positionX { get; set; }
        public float positionY { get; set; }
        public float positionZ { get; set; }
        public float rotationX { get; set; }
        public float rotationY { get; set; }
        public float rotationZ { get; set; }
        public bool m_isSpawnLock { get; set; }
        public bool m_isHarnessed { get; set; }
        public bool IsKinematicLocked { get; set; }
        public bool IsInWater { get; set; }
        public List<PhysicalObject> AttachmentsList { get; set; }
        public int m_quickBeltSlot { get; set; }
        public int heldMode { get; set; } // 0: not held, 1: held in right, 2: held in left

        // Attachment specific
        public int mountIndex { get; set; }

        // Firearm specific
        public List<int> loadedRoundsInChambers { get; set; }
        public AmmoContainer ammoContainer {get; set;}
        public List<string> flagDictKeys { get; set; }
        public List<string> flagDictValues { get; set; }

        // Equipment specific
        public int equipSlot { get; set; }

        // Ammo container specific (Magazine, Clip, or Speedloader)
        public List<int> loadedRoundsInContainer { get; set; }

        // Bodyarmor and rig specific
        public List<SavedItem> quickBeltSlotContents { get; set; }
        public float maxArmor { get; set; }
        public float armor { get; set; }

        // Backpack specific
        public List<SavedItem> backpackContents { get; set; }
    }

    public class AmmoContainer
    {
        public string itemID { get; set; }
        public List<int> loadedRoundsInContainer { get; set; }
    }

    public class SavedItem
    {
        public int itemType { get; set; }
        public PhysicalObject PhysicalObject { get; set; }
    }

    public class SavedArea
    {
        public int level { get; set; }
        public bool constructing { get; set; }
        public float constructTime { get; set; } // What time the cunstruction has started so we can check how long it has been since then and decide if it is done
        public List<SavedItem> slots { get; set; } // Slots that could contain items, like generator that could have gas cans
    }

    public class SavedTraderStatus
    {
        public string id { get; set; }
        public float salesSum { get; set; }
        public float standing { get; set; }
        public bool unlocked { get; set; }
    }

    public class SavedSkill
    {
        public float progress { get; set; }
    }
}
