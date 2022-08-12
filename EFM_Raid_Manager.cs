using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_Raid_Manager : EFM_Manager
    {
        public static readonly float extractionTime = 10;

        public static EFM_Raid_Manager currentManager;
        public static float extractionTimer;
        public static bool inRaid;
        public static JObject locationData;
        public static int maxBotPerZone;

        private List<Extraction> extractions;
        private List<Extraction> possibleExtractions;
        public ExtractionManager currentExtraction;
        private bool inExtractionLastFrame;
        public Transform spawnPoint;
        public Light sun;
        public float time;
        public EFM_GCManager GCManager;
        private float maxRaidTime;

        public float[] maxHealth = { 35, 85, 70, 60, 60, 65, 65 };
        public float[] currentHealthRates;
        private float[] currentNonLethalHealthRates;
        public float currentEnergyRate = -3.2f;
        public float currentHydrationRate = -2.6f;

        private List<AISpawn> AISpawns;
        private AISpawn nextSpawn;
        private Dictionary<string, Transform> AISquadSpawnTransforms;
        private List<int> availableIFFs;
        private int[] spawnedIFFs;
        private Dictionary<string, int> AISquadIFFs;
        private Dictionary<string, List<Sosig>> AISquads;
        private Dictionary<string, int> AISquadSizes;
        public static List<AIEntity> entities;
        public static List<EFM_AI> entityRelatedAI;

        private List<GameObject> extractionCards;
        private bool extracted;
        private bool spawning;

        // AI
        private float initSpawnTimer = 5; // Will only start spawning AI once this much time has elapsed at start of raid

        public override void Init()
        {
            Mod.currentRaidManager = this;
            Mod.lootingExp = 0;
            Mod.healingExp = 0;
            Mod.explorationExp = 0;
            Mod.raidTime = 0;

            // disable post processing
            GM.Options.PerformanceOptions.IsPostEnabled_AO = false;
            GM.Options.PerformanceOptions.IsPostEnabled_Bloom = false;
            GM.Options.PerformanceOptions.IsPostEnabled_CC = false;

            // Init player state
            currentHealthRates = new float[7];
            currentNonLethalHealthRates = new float[7];

            // Manager GC ourselves
            GCManager = gameObject.AddComponent<EFM_GCManager>();

            // Init effects that were already active before the raid to make sure their effects are applied
            InitEffects();

            Mod.instance.LogInfo("Raid init called");
            currentManager = this;

            LoadMapData();
            Mod.instance.LogInfo("Map data read");

            // Choose spawnpoints
            Transform spawnRoot = transform.GetChild(transform.childCount - 1).GetChild(0);
            spawnPoint = spawnRoot.GetChild(UnityEngine.Random.Range(0, spawnRoot.childCount));

            GM.CurrentSceneSettings.DeathResetPoint = spawnPoint;

            Mod.instance.LogInfo("Got spawn");

            // Find extractions
            possibleExtractions = new List<Extraction>();
            Extraction bestCandidate = null; // Must be an extraction without appearance times (always available) and no other requirements. This will be the minimum extraction
            float farthestDistance = float.MinValue;
            Mod.instance.LogInfo("Init raid with map index: " + Mod.chosenMapIndex + ", which has " + extractions.Count + " extractions");
            for (int extractionIndex = 0; extractionIndex < extractions.Count; ++extractionIndex)
            {
                Extraction currentExtraction = extractions[extractionIndex];
                currentExtraction.gameObject = gameObject.transform.GetChild(gameObject.transform.childCount - 2).GetChild(extractionIndex).gameObject;

                bool canBeMinimum = (currentExtraction.times == null || currentExtraction.times.Count == 0) &&
                                    (currentExtraction.itemRequirements == null || currentExtraction.itemRequirements.Count == 0) &&
                                    (currentExtraction.equipmentRequirements == null || currentExtraction.equipmentRequirements.Count == 0) &&
                                    (currentExtraction.role == Mod.chosenCharIndex || currentExtraction.role == 2) &&
                                    !currentExtraction.accessRequirement;
                float currentDistance = Vector3.Distance(spawnPoint.position, currentExtraction.gameObject.transform.position);
                Mod.instance.LogInfo("\tExtraction at index: " + extractionIndex + " has " + currentExtraction.times.Count + " times and " + currentExtraction.itemRequirements.Count + " items reqs. Its current distance from player is: " + currentDistance);
                if (UnityEngine.Random.value <= ExtractionChanceByDist(currentDistance))
                {
                    Mod.instance.LogInfo("\t\tAdding this extraction to list possible extractions");
                    possibleExtractions.Add(currentExtraction);

                    //Add an extraction manager
                    ExtractionManager extractionManager = currentExtraction.gameObject.AddComponent<ExtractionManager>();
                    extractionManager.extraction = currentExtraction;
                    extractionManager.raidManager = this;
                }

                // Best candidate will be farthest because if there is at least one extraction point, we don't want to always have the nearest
                if (canBeMinimum && currentDistance > farthestDistance)
                {
                    bestCandidate = currentExtraction;
                }
            }
            if (bestCandidate != null)
            {
                if (!possibleExtractions.Contains(bestCandidate))
                {
                    possibleExtractions.Add(bestCandidate);

                    //Add an extraction manager
                    ExtractionManager extractionManager = bestCandidate.gameObject.AddComponent<ExtractionManager>();
                    extractionManager.extraction = bestCandidate;
                    extractionManager.raidManager = this;
                }
            }
            else
            {
                Mod.instance.LogError("No minimum extraction found");
            }

            Mod.instance.LogInfo("Got extractions");

            // Init extraction cards
            Transform extractionParent = Mod.playerStatusUI.transform.GetChild(0).GetChild(9);
            extractionCards = new List<GameObject>();
            for (int i = 0; i < possibleExtractions.Count; ++i)
            {
                GameObject currentExtractionCard = Instantiate(Mod.extractionCardPrefab, extractionParent);
                extractionCards.Add(currentExtractionCard);

                currentExtractionCard.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = String.Format("EXFIL{0:00} {1}", i, possibleExtractions[i].name);
                if (possibleExtractions[i].times != null && possibleExtractions[i].times.Count > 0 ||
                   possibleExtractions[i].raidTimes != null && possibleExtractions[i].raidTimes.Count > 0)
                {
                    currentExtractionCard.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                }

                possibleExtractions[i].card = currentExtractionCard;
            }
            extractionParent.gameObject.SetActive(true);
            Mod.instance.LogInfo("Inited extract cards");

            // Initialize doors
            Mod.initDoors = true;
            Transform worldRoot = transform.GetChild(1);
            Transform modelRoot = worldRoot.GetChild(1);
            Transform doorRoot = modelRoot.GetChild(0);
            Transform logicRoot = modelRoot.GetChild(3);
            Transform playerBlocksRoot = logicRoot.GetChild(2);
            int doorIndex = 0;

            MatDef metalMatDef = new MatDef();
            metalMatDef.name = "Door_Metal";
            metalMatDef.BallisticType = MatBallisticType.MetalThick;
            metalMatDef.BulletHoleType = BulletHoleDecalType.Metal;
            metalMatDef.BulletImpactSound = BulletImpactSoundType.MetalThick;
            metalMatDef.ImpactEffectType = BallisticImpactEffectType.Sparks;
            metalMatDef.SoundType = MatSoundType.Metal;

            Mod.instance.LogInfo("Initializing doors");
            // TODO: Uncomment and delete what comes after once doors aren't as laggy. Will also have to put vanilla doors first in hierarchy in map asset
            /*
            foreach (JToken doorData in Mod.mapData["maps"][Mod.chosenMapIndex]["doors"])
            {
                GameObject doorObject = doorRoot.GetChild(doorIndex).gameObject;
                Mod.instance.LogInfo("\t" + doorObject.name);
                GameObject doorInstance = null;
                if (doorData["type"].ToString().Equals("left"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorLeftPrefab, doorRoot);
                }
                else if (doorData["type"].ToString().Equals("right"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorRightPrefab, doorObject.transform.position, doorObject.transform.rotation, doorRoot);
                }
                else if (doorData["type"].ToString().Equals("double"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorDoublePrefab, doorObject.transform.position, doorObject.transform.rotation, doorRoot);
                }
                doorInstance.transform.localPosition = doorObject.transform.localPosition;
                doorInstance.transform.localRotation = doorObject.transform.localRotation;
                doorInstance.transform.localScale = doorObject.transform.localScale;

                // Set door to active to awake and Init it
                doorInstance.SetActive(true);

                // Get relevant components
                SideHingedDestructibleDoorDeadBolt deadBolt = doorInstance.GetComponentInChildren<SideHingedDestructibleDoorDeadBolt>();
                SideHingedDestructibleDoor doorScript = doorInstance.transform.GetChild(0).GetComponent<SideHingedDestructibleDoor>();

                // Add a doorWrapper
                EFM_DoorWrapper doorWrapper = doorInstance.AddComponent<EFM_DoorWrapper>();

                // Transfer grill if it exists
                if (doorObject.transform.GetChild(0).childCount == 3)
                {
                    doorObject.transform.GetChild(0).GetChild(2).parent = doorInstance.transform.GetChild(0).GetChild(0);
                }

                // Remove frame if necessary
                if (!(bool)doorData["hasFrame"])
                {
                    Destroy(doorInstance.GetComponent<MeshFilter>());
                    Destroy(doorInstance.GetComponent<MeshRenderer>());
                }

                // Transfer material if necessary
                if ((bool)doorData["customMat"])
                {
                    if ((bool)doorData["hasFrame"])
                    {
                        doorInstance.GetComponent<MeshRenderer>().material = doorObject.GetComponent<MeshRenderer>().material;
                    }
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material = doorObject.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material;
                }

                // Set PMat if necessary
                if (doorData["mat"].ToString().Equals("metal"))
                {
                    doorInstance.GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                }

                // Flip lock if necessary
                if ((bool)doorData["flipLock"])
                {
                    doorWrapper.flipLock = true;
                    if (deadBolt != null)
                    {
                        deadBolt.transform.Rotate(0, 180, 0);
                    }
                }

                // Set destructable
                if (!(bool)doorData["destructable"])
                {
                    Destroy(doorInstance.transform.GetChild(0).GetComponent<UberShatterable>()); // TODO: Verify if this works
                }

                // Set door angle
                //bool hasAngle = (bool)doorData["angle"];
                //if (hasAngle)
                //{
                //    //Transform doorPlaceholder = doorObject.transform.GetChild(0);
                //    //Transform fakeHingePlaceholder = doorObject.transform.GetChild(1);
                //    //Transform door = doorInstance.transform.GetChild(0);
                //    //Transform fakeHinge = doorInstance.transform.GetChild(1);
                //    //door.gameObject.SetActive(false);
                //    //door.localPosition = doorPlaceholder.localPosition;
                //    //door.localRotation = doorPlaceholder.localRotation;
                //    //if (disabledDoors == null)
                //    //{
                //    //    disabledDoors = new List<GameObject>();
                //    //}
                //    //disabledDoors.Add(door.gameObject);

                //    //if (hingeJointsToDisableLimits == null)
                //    //{
                //    //    hingeJointsToDisableLimits = new List<HingeJoint>();
                //    //}
                //    //if (correspondingLimits == null)
                //    //{
                //    //    correspondingLimits = new List<JointLimits>();
                //    //}

                //    //float angleToUse = fakeHingePlaceholder.eulerAngles.y;
                //    ////if (doorData["type"].ToString().Equals("left"))
                //    ////{
                //    ////    angleToUse = -angleToUse;
                //    ////}
                //    //JointLimits jl = new JointLimits();
                //    //jl.min = angleToUse;
                //    //jl.max = angleToUse + 1;

                //    //correspondingLimits.Add(doorScript.HingeLower.limits);
                //    //correspondingLimits.Add(doorScript.HingeUpper.limits);

                //    //doorScript.HingeLower.limits = jl;
                //    //doorScript.HingeUpper.limits = jl;

                //    //hingeJointsToDisableLimits.Add(doorScript.HingeLower);
                //    //hingeJointsToDisableLimits.Add(doorScript.HingeUpper);
                //}

                // Set key
                int keyIndex = (int)doorData["keyIndex"];
                if (keyIndex > -1)
                {
                    doorWrapper.keyID = keyIndex.ToString();
                    if ((bool)doorData["locked"])
                    {
                        EFM_DoorLockChecker doorLockChecker = doorInstance.AddComponent<EFM_DoorLockChecker>();
                        doorLockChecker.deadBolt = deadBolt;
                        doorLockChecker.blocks = playerBlocksRoot.GetChild((int)doorData["blockIndex"]).gameObject;
                    }
                    else
                    {
                        // Make sure door is unlocked
                        doorScript.ForceUnlock();
                    }
                }
                else
                {
                    // Make sure door is unlocked
                    doorScript.ForceUnlock();
                }

                // Destroy placeholder door
                doorObject.SetActive(false);
                //Destroy(doorObject);

                ++doorIndex;
            }
            */

            foreach (JToken doorData in Mod.mapData["maps"][Mod.chosenMapIndex]["doors"])
            {
                GameObject doorObject = doorRoot.GetChild(doorIndex).GetChild(0).gameObject;

                // Add a doorWrapper
                EFM_DoorWrapper doorWrapper = doorObject.AddComponent<EFM_DoorWrapper>();

                // Set key
                int keyIndex = (int)doorData["keyIndex"];
                if (keyIndex > -1)
                {
                    doorWrapper.keyID = keyIndex.ToString();
                    doorWrapper.open = true;
                    doorWrapper.openAngleX = (float)doorData["openAngleX"];
                    doorWrapper.openAngleY = (float)doorData["openAngleY"];
                    doorWrapper.openAngleZ = (float)doorData["openAngleZ"];
                }

                ++doorIndex;
            }

            Mod.instance.LogInfo("Initialized doors, spawning loose loot");

            // Spawn loose loot
            JObject locationDB = Mod.locationsLootDB[GetLocationDataIndex(Mod.chosenMapIndex)];
            Transform itemsRoot = transform.GetChild(1).GetChild(1).GetChild(2);
            List<string> missingForced = new List<string>();
            List<string> missingDynamic = new List<string>();
            // Forced, always spawns TODO: Unless player has it already? Unless player doesnt have the quest yet?
            Mod.instance.LogInfo("Spawning forced loot");
            foreach (JToken forced in locationDB["forced"])
            {
                JArray items = forced["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItemCIWs = new Dictionary<string, EFM_CustomItemWrapper>();
                Dictionary<string, EFM_VanillaItemDescriptor> spawnedItemVIDs = new Dictionary<string, EFM_VanillaItemDescriptor>();
                List<string> unspawnedParents = new List<string>();

                for (int i = 0; i < items.Count; ++i)
                {
                    // Get item from item map
                    string originalID = items[i]["_tpl"].ToString();
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        itemID = Mod.itemMap[originalID];
                    }
                    else
                    {
                        missingForced.Add(originalID);

                        // Spawn random round instead
                        itemID = Mod.usedRoundIDs[UnityEngine.Random.Range(0, Mod.usedRoundIDs.Count - 1)];
                    }

                    // Get item prefab
                    GameObject itemPrefab = null;
                    if (int.TryParse(itemID, out int index))
                    {
                        itemPrefab = Mod.itemPrefabs[index];
                    }
                    else
                    {
                        itemPrefab = IM.OD[itemID].GetGameObject();
                    }

                    // Spawn item
                    SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItemCIWs, spawnedItemVIDs, unspawnedParents, forced, originalID, false);
                }
            }

            // Dynamic, has chance of spawning
            Mod.instance.LogInfo("Spawning dynamic loot");
            foreach (JToken dynamicSpawn in locationDB["dynamic"])
            {
                JArray items = dynamicSpawn["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItemCIWs = new Dictionary<string, EFM_CustomItemWrapper>();
                Dictionary<string, EFM_VanillaItemDescriptor> spawnedItemVIDs = new Dictionary<string, EFM_VanillaItemDescriptor>();
                List<string> unspawnedParents = new List<string>();

                for (int i = 0; i < items.Count; ++i)
                {
                    // Get item from item map
                    string originalID = items[i]["_tpl"].ToString();
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        itemID = Mod.itemMap[originalID];
                    }
                    else
                    {
                        missingForced.Add(originalID);

                        // Spawn random round instead
                        itemID = Mod.usedRoundIDs[UnityEngine.Random.Range(0, Mod.usedRoundIDs.Count - 1)];
                    }

                    // Get item prefab
                    GameObject itemPrefab = null;
                    if (int.TryParse(itemID, out int index))
                    {
                        itemPrefab = Mod.itemPrefabs[index];
                    }
                    else
                    {
                        itemPrefab = IM.OD[itemID].GetGameObject();
                    }

                    // Spawn item
                    SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItemCIWs, spawnedItemVIDs, unspawnedParents, dynamicSpawn, originalID, true);
                }
            }

            Mod.instance.LogInfo("Done spawning loose loot, initializing container");

            // Init containers
            Transform containersRoot = transform.GetChild(1).GetChild(1).GetChild(1);
            JArray mapContainerData = (JArray)Mod.mapData["maps"][Mod.chosenMapIndex]["containers"];
            for (int i = 0; i < containersRoot.childCount; ++i)
            {
                Transform container = containersRoot.GetChild(i);

                // Setup the container
                JObject containerData = Mod.lootContainersByName[container.name];
                Transform mainContainer = container.GetChild(container.childCount - 1);
                switch (container.name)
                {
                    case "Jacket":
                    case "scavDead":
                    case "MedBag":
                    case "SportBag":
                        // Static containers that can be toggled open closed by hovering hand overthem and pressing interact button
                        container.gameObject.SetActive(false); // Disable temporarily so CIW doesnt Awake before we set the itemType
                        EFM_CustomItemWrapper containerCIW = container.gameObject.AddComponent<EFM_CustomItemWrapper>();
                        containerCIW.itemType = Mod.ItemType.LootContainer;
                        container.gameObject.SetActive(true);
                        containerCIW.canInsertItems = false;
                        containerCIW.mainContainer = mainContainer.gameObject;
                        containerCIW.containerItemRoot = container.GetChild(container.childCount - 2);

                        EFM_LootContainer containerScript = container.gameObject.AddComponent<EFM_LootContainer>();
                        containerScript.mainContainerCollider = mainContainer.GetComponent<Collider>();
                        JToken gridProps = containerData["_props"]["Grids"][0]["_props"];
                        containerScript.Init(containerData["_props"]["SpawnFilter"].ToObject<List<string>>(), (int)gridProps["cellsH"] * (int)gridProps["cellsV"]);

                        mainContainer.GetComponent<MeshRenderer>().material = Mod.quickSlotConstantMaterial;
                        break;
                    case "Safe":
                    case "meds&other":
                    case "tools&other":
                    case "GrenadeBox":
                    case "terraWBoxLongBig":
                    case "terraWBoxLong":
                    case "WeaponCrate":
                    case "ToolBox":
                        // Containers that must be physically opened (Door, Cover, Cap, Lid...)
                        EFM_LootContainerCover cover = container.GetChild(0).gameObject.AddComponent<EFM_LootContainerCover>();
                        cover.keyID = mapContainerData[i]["keyID"].ToString();
                        cover.hasKey = !cover.keyID.Equals("");
                        cover.Root = container.GetChild(container.childCount - 3);
                        cover.MinRot = -90;
                        cover.MaxRot = 0;

                        EFM_LootContainer openableContainerScript = container.gameObject.AddComponent<EFM_LootContainer>();
                        openableContainerScript.interactable = cover;
                        openableContainerScript.mainContainerCollider = mainContainer.GetComponent<Collider>();
                        JToken openableContainergridProps = containerData["_props"]["Grids"][0]["_props"];
                        openableContainerScript.Init(containerData["_props"]["SpawnFilter"].ToObject<List<string>>(), (int)openableContainergridProps["cellsH"] * (int)openableContainergridProps["cellsV"]);

                        mainContainer.GetComponent<MeshRenderer>().material = Mod.quickSlotConstantMaterial;
                        break;
                    case "Drawer":
                        // Containers that must be slid open
                        for (int drawerIndex = 0; drawerIndex < 4; ++drawerIndex)
                        {
                            Transform drawerTransform = container.GetChild(drawerIndex);
                            EFM_LootContainerSlider slider = drawerTransform.gameObject.AddComponent<EFM_LootContainerSlider>();
                            slider.keyID = mapContainerData[i]["keyID"].ToString();
                            slider.hasKey = !slider.keyID.Equals("");
                            slider.Root = slider.transform;
                            slider.MinY = -0.3f;
                            slider.MaxY = 0.2f;
                            slider.posZ = container.GetChild(drawerIndex).localPosition.z;

                            EFM_LootContainer drawerScript = drawerTransform.gameObject.AddComponent<EFM_LootContainer>();
                            drawerScript.interactable = slider;
                            drawerScript.mainContainerCollider = drawerTransform.GetChild(drawerTransform.childCount - 1).GetComponent<Collider>();
                            JToken drawerGridProps = containerData["_props"]["Grids"][0]["_props"];
                            drawerScript.Init(containerData["_props"]["SpawnFilter"].ToObject<List<string>>(), (int)drawerGridProps["cellsH"] * (int)drawerGridProps["cellsV"]);

                            Transform drawerContainer = drawerTransform.GetChild(drawerTransform.childCount - 1);
                            drawerContainer.GetComponent<MeshRenderer>().material = Mod.quickSlotConstantMaterial;
                        }
                        break;
                    default:
                        break;
                }
            }

            // Init damage volumes
            foreach(Transform damageVolTransform in transform.GetChild(1).GetChild(1).GetChild(3).GetChild(3))
            {
                EFM_DamageVolume newDamageVolume = damageVolTransform.gameObject.AddComponent<EFM_DamageVolume>();
                newDamageVolume.Init();
            }

            // Init experience triggers
            Transform expTriggersParent = transform.GetChild(1).GetChild(1).GetChild(3).GetChild(5);
            for (int i=0; i < expTriggersParent.childCount; ++i)
            {
                if (!Mod.triggeredExplorationTriggers[Mod.chosenMapIndex][i])
                {
                    expTriggersParent.GetChild(i).gameObject.AddComponent<EFM_ExperienceTrigger>();
                }
            }

            //// Output missing items
            //if (Mod.instance.debug)
            //{
            //    string text = "Raid with map index = " + Mod.chosenMapIndex + " was missing FORCED loose loot:\n";
            //    foreach (string id in missingForced)
            //    {
            //        text += id + "\n";
            //    }
            //    text += "Raid with map index = " + Mod.chosenMapIndex + " was missing DYNAMIC loose loot:\n";
            //    foreach (string id in missingDynamic)
            //    {
            //        text += id + "\n";
            //    }
            //    File.WriteAllText("BepinEx/Plugins/EscapeFromMeatov/ErrorLog.txt", text);
            //}

            // Init time
            InitTime();

            // Init sun
            InitSun();

            // Init reverb system
            InitReverb();

            // Init AI
            InitAI();

            inRaid = true;

            init = true;
        }

        private void Update()
        {
            if (extracted)
            {
                return;
            }

            Mod.raidTime += Time.deltaTime;
            float timeLeft = maxRaidTime - Mod.raidTime;
            Mod.playerStatusManager.SetExtractionLimitTimer(timeLeft);
            if(timeLeft <= 600)
            {
                if (!Mod.extractionLimitUI.activeSelf)
                {
                    Mod.extractionLimitUI.SetActive(true);
                }
                Mod.extractionLimitUIText.text = Mod.FormatTimeString(timeLeft);

                // Position extraction limit UI
                Vector3 vector = GM.CurrentPlayerBody.Head.position + Vector3.up * 0.25f;
                Vector3 vector2 = GM.CurrentPlayerBody.Head.forward;
                vector2.y = 0f;
                vector2.Normalize();
                vector2 *= 0.35f;
                vector += vector2;
                Mod.extractionUI.transform.position = vector;
                Mod.extractionUI.transform.rotation = Quaternion.LookRotation(vector2, Vector3.up);
                Mod.extractionUI.transform.Rotate(Vector3.right, -25);

                if (Mod.playerStatusManager.extractionTimerText.color != Color.red)
                {
                    Mod.playerStatusManager.extractionTimerText.color = Color.red;
                }
            }

            if (Mod.instance.debug)
            {
                if (Input.GetKeyDown(KeyCode.U))
                {
                    Mod.justFinishedRaid = true;
                    Mod.raidState = EFM_Base_Manager.FinishRaidState.Survived;

                    // Disable extraction list and timer
                    Mod.playerStatusUI.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
                    Mod.playerStatusManager.extractionTimerText.color = Color.black;
                    Mod.extractionLimitUI.SetActive(false);
                    Mod.playerStatusManager.SetDisplayed(false);
                    Mod.extractionUI.SetActive(false);

                    EFM_Manager.LoadBase(5); // Load autosave, which is right before the start of raid

                    extracted = true;
                }
            }

            if(currentExtraction != null && currentExtraction.active)
            {
                string missingRequirement = currentExtraction.extraction.RequirementsMet();
                if (missingRequirement.Equals("none") || missingRequirement.Equals(""))
                {
                    // Make sure the requirement text is disabled if 
                    if(missingRequirement.Equals("none"))
                    {
                        currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
                    }

                    inExtractionLastFrame = true;

                    extractionTimer += UnityEngine.Time.deltaTime;

                    if (!Mod.extractionUI.activeSelf)
                    {
                        Mod.extractionUI.SetActive(true);
                    }
                    Mod.extractionUIText.text = string.Format("Extraction in {0:0.#}", Mathf.Max(0, extractionTime - extractionTimer));

                    // Position extraction UI
                    Vector3 vector = GM.CurrentPlayerBody.Head.position + Vector3.up * 0.3f;
                    Vector3 vector2 = GM.CurrentPlayerBody.Head.forward;
                    vector2.y = 0f;
                    vector2.Normalize();
                    vector2 *= 0.25f;
                    vector += vector2;
                    Mod.extractionUI.transform.position = vector;
                    Mod.extractionUI.transform.rotation = Quaternion.LookRotation(vector2, Vector3.up);
                    Mod.extractionUI.transform.Rotate(Vector3.right, -20);

                    if (extractionTimer >= extractionTime)
                    {
                        Mod.justFinishedRaid = true;
                        float totalExp = 0;
                        if (Mod.killList != null && Mod.killList.Count > 0) 
                        {
                            foreach (KeyValuePair<string, int> kill in Mod.killList)
                            {
                                totalExp += kill.Value;
                            }
                        }
                        totalExp += Mod.lootingExp;
                        totalExp += Mod.healingExp;
                        totalExp += Mod.explorationExp;
                        if (totalExp < 400 && Mod.raidTime < 420) // 420 seconds = 7 mins
                        {
                            Mod.raidState = EFM_Base_Manager.FinishRaidState.RunThrough;
                        }
                        else
                        {
                            Mod.raidState = EFM_Base_Manager.FinishRaidState.Survived;
                        }

                        // Disable extraction list
                        Mod.playerStatusUI.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
                        Mod.playerStatusManager.extractionTimerText.color = Color.black; 
                        Mod.extractionLimitUI.SetActive(false);
                        Mod.playerStatusManager.SetDisplayed(false);
                        Mod.extractionUI.SetActive(false);

                        EFM_Manager.LoadBase(5); // Load autosave, which is right before the start of raid

                        extracted = true;
                    }
                }
                else
                {
                    // Update extraction card requirement text with requirement of this extract
                    currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
                    Text requirementText = currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).GetComponent<Text>();
                    requirementText.color = Color.red;
                    requirementText.text = missingRequirement;
                }
            }
            else if (inExtractionLastFrame)
            {
                extractionTimer = 0;
                inExtractionLastFrame = false;

                Mod.extractionUI.SetActive(false);
            }

            // If hit escape time limit
            if(Mod.raidTime >= maxRaidTime)
            {
                KillPlayer(EFM_Base_Manager.FinishRaidState.MIA);
            }

            UpdateEffects();

            UpdateAI();

            // TODO: Reeneable these once we figure out efficient way to rotate sun with time for realistic day/night cycle
            //UpdateTime();

            //UpdateSun();
        }

        private void UpdateAI()
        {
            if(initSpawnTimer <= 0)
            {
                // Check if time is >= spawnTime on next AISpawn in list, if it is, spawn it at spawnpoint depending on AIType
                if(!spawning && nextSpawn != null && nextSpawn.spawnTime <= Mod.raidTime)
                {
                    AnvilManager.Run(SpawnAI(nextSpawn));
                    AISpawns.RemoveAt(AISpawns.Count - 1);
                    nextSpawn = AISpawns.Count > 0 ? AISpawns[AISpawns.Count - 1] : null;
                }
            }
            else
            {
                initSpawnTimer -= Time.deltaTime;
            }
        }

        private IEnumerator SpawnAI(AISpawn spawnData)
        {
            Mod.instance.LogInfo("SPAWNAI: SpawnAI called with name "+spawnData.name+" of type: "+spawnData.type);
            spawning = true;
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name+": \tGot sosig body prefab, null?: "+(sosigPrefab == null));

            // TODO: Make sure that wherever we spawn a bot, it is far enough from player, and there is no direct line of sight between player and potential spawnpoint
            Transform AISpawnPoint = null;
            switch (spawnData.type)
            {
                case AISpawn.AISpawnType.Scav:
                    List<Transform> mostAvailableScavBotZones = GetMostAvailableBotZones();
                    Transform currentScavZone = mostAvailableScavBotZones[UnityEngine.Random.Range(0, mostAvailableScavBotZones.Count)];
                    EFM_BotZone scavZoneScript = currentScavZone.GetComponent<EFM_BotZone>();
                    ++scavZoneScript.botCount;

                    AISpawnPoint = currentScavZone.GetChild(UnityEngine.Random.Range(0, currentScavZone.childCount));
                    break;
                case AISpawn.AISpawnType.PMC:
                    Transform PMCSpawnPointRoot = transform.GetChild(transform.childCount - 1).GetChild(0);

                    AISpawnPoint = PMCSpawnPointRoot.GetChild(UnityEngine.Random.Range(0, PMCSpawnPointRoot.childCount));
                    break;
                case AISpawn.AISpawnType.Raider:
                    if (AISquadSpawnTransforms.ContainsKey(spawnData.leaderName))
                    {
                        Transform raiderZone = AISquadSpawnTransforms[spawnData.leaderName];

                        AISpawnPoint = raiderZone.GetChild(UnityEngine.Random.Range(0, raiderZone.childCount));
                    }
                    else
                    {
                        List<Transform> mostAvailableBotZones = GetMostAvailableBotZones();
                        Transform randomRaiderZone = mostAvailableBotZones[UnityEngine.Random.Range(0, mostAvailableBotZones.Count)];
                        EFM_BotZone raiderBotZoneScript = randomRaiderZone.GetComponent<EFM_BotZone>();
                        ++raiderBotZoneScript.botCount;

                        AISpawnPoint = randomRaiderZone.GetChild(UnityEngine.Random.Range(0, randomRaiderZone.childCount));

                        AISquadSpawnTransforms.Add(spawnData.leaderName, randomRaiderZone);
                    }
                    break;
                case AISpawn.AISpawnType.Boss:
                case AISpawn.AISpawnType.Follower:
                    if (AISquadSpawnTransforms.ContainsKey(spawnData.leaderName))
                    {
                        Transform bossZone = AISquadSpawnTransforms[spawnData.leaderName];

                        AISpawnPoint = bossZone.GetChild(UnityEngine.Random.Range(0, bossZone.childCount));
                    }
                    else
                    {
                        Transform bossZoneRootRoot = transform.GetChild(transform.childCount - 1).GetChild(2);
                        Transform bossZoneRoot = bossZoneRootRoot.Find(spawnData.leaderName);
                        Transform randomBossZone = null;
                        if (bossZoneRoot != null)
                        {
                            randomBossZone = bossZoneRoot.GetChild(UnityEngine.Random.Range(0, bossZoneRoot.childCount));
                        }
                        else
                        {
                            List<Transform> mostAvailableBotZones = GetMostAvailableBotZones();
                            randomBossZone = mostAvailableBotZones[UnityEngine.Random.Range(0, mostAvailableBotZones.Count)];
                        }
                        EFM_BotZone bossBotZoneScript = randomBossZone.GetComponent<EFM_BotZone>();
                        ++bossBotZoneScript.botCount;

                        AISpawnPoint = randomBossZone.GetChild(UnityEngine.Random.Range(0, randomBossZone.childCount));

                        AISquadSpawnTransforms.Add(spawnData.leaderName, randomBossZone);
                    }
                    break;
                default:
                    break;
            }

            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tGot spawnpoint at " + AISpawnPoint.position);

            GameObject sosigObject = Instantiate(sosigPrefab, AISpawnPoint.position, AISpawnPoint.rotation);
            sosigObject.name = spawnData.type.ToString() + " AI ("+spawnData.name+")";
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tInstantiated sosig, position: " + sosigObject.transform.position);
            Sosig sosigScript = sosigObject.GetComponentInChildren<Sosig>();
            sosigScript.InitHands();
            sosigScript.Inventory.Slots.Add(new SosigInventory.Slot()); // Add a slot for weapon
            sosigScript.Inventory.Slots.Add(new SosigInventory.Slot()); // Add a slot for grenade
            sosigScript.Inventory.Init();

            EFM_AI AIScript = sosigObject.AddComponent<EFM_AI>();
            AIScript.experienceReward = spawnData.experienceReward;
            AIScript.entityIndex = entities.Count;
            AIScript.inventory = spawnData.inventory;
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tAdded EFM_AI script");

            entities.Add(sosigScript.E);
            entityRelatedAI.Add(AIScript);

            // Spawn outfit
            foreach (KeyValuePair<int, List<string>> outfitEntry in spawnData.outfitByLink)
            {
                SosigLink link = sosigScript.Links[outfitEntry.Key];
                foreach (string outfitItemID in outfitEntry.Value)
                {
                    yield return IM.OD[outfitItemID].GetGameObjectAsync();
                    GameObject outfitItemObject = Instantiate(IM.OD[outfitItemID].GetGameObject(), link.transform.position, link.transform.rotation, link.transform);
                    SosigWearable wearableScript = outfitItemObject.GetComponent<SosigWearable>();
                    wearableScript.RegisterWearable(link);
                }
            }
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawned outfit items");

            // Spawn sosig weapon and grenade
            if (spawnData.sosigWeapon != null)
            {
                yield return IM.OD[spawnData.sosigWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[spawnData.sosigWeapon].GetGameObject());
                SosigWeapon sosigWeapon = weaponObject.GetComponent<SosigWeapon>();
                Destroy(sosigWeapon.GetComponent<SosigWeaponPlayerInterface>());
                sosigWeapon.SetAutoDestroy(true);
                sosigScript.ForceEquip(sosigWeapon);
                if (sosigWeapon.Type == SosigWeapon.SosigWeaponType.Gun)
                {
                    sosigScript.Inventory.FillAmmoWithType(sosigWeapon.AmmoType);
                }
            }
            else
            {
                Mod.instance.LogError(spawnData.type.ToString() + " type AI with name: " + spawnData.name + ", has no weapon");
            }

            if (spawnData.sosigGrenade != null)
            {
                yield return IM.OD[spawnData.sosigGrenade].GetGameObjectAsync();
                GameObject grenadeObject = Instantiate(IM.OD[spawnData.sosigGrenade].GetGameObject());
                SosigWeapon sosigGrenade = grenadeObject.GetComponent<SosigWeapon>();
                Destroy(sosigGrenade.GetComponent<SosigWeaponPlayerInterface>());
                sosigGrenade.SetAutoDestroy(true);
                sosigScript.ForceEquip(sosigGrenade);
            }
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawned weapons");

            // Set sosig logic (IFF, path, TODO: difficulty vars, etc)
            int iff = 0;
            switch (spawnData.type)
            {
                case AISpawn.AISpawnType.Scav:
                    if(Mod.chosenCharIndex == 0)
                    {
                        iff = 1;
                    }
                    else // Player is scav
                    {
                        iff = 0;
                    }

                    sosigScript.CurrentOrder = Sosig.SosigOrder.Wander;
                    sosigScript.FallbackOrder = Sosig.SosigOrder.Wander;

                    sosigScript.SetCurrentOrder(sosigScript.FallbackOrder);
                    break;
                case AISpawn.AISpawnType.PMC:
                    if (availableIFFs.Count > 0)
                    {
                        iff = availableIFFs[availableIFFs.Count - 1];
                        availableIFFs.RemoveAt(availableIFFs.Count - 1);
                    }
                    else
                    {
                        iff = UnityEngine.Random.Range(2, 32);
                    }
                    ++spawnedIFFs[iff];

                    // Generate a path to random points in the map
                    Transform PMCPointsParentsRoot = transform.GetChild(transform.childCount - 3);
                    Transform PMCContainerPoints = PMCPointsParentsRoot.GetChild(0);
                    Transform PMCExtractionPoints = PMCPointsParentsRoot.GetChild(1);
                    Transform PMCPointsOfInterest = PMCPointsParentsRoot.GetChild(2);
                    List<Transform> PMCPoints = new List<Transform>();
                    //TODO: Decide length of path depending on sosig speed, should look how long it takes them to navigate 5 points or something and base ourselves off of that
                    //TODO: Make sure there are no duplicate points being used
                    // For now just use default of 15 containers and 25 PoIs
                    for (int i = 0; i < 15; ++i)
                    {
                        PMCPoints.Add(PMCContainerPoints.GetChild(UnityEngine.Random.Range(0, PMCContainerPoints.childCount)));
                    }
                    for (int i = 0; i < 25; ++i)
                    {
                        PMCPoints.Add(PMCPointsOfInterest.GetChild(UnityEngine.Random.Range(0, PMCPointsOfInterest.childCount)));
                    }
                    PMCPoints.Add(PMCExtractionPoints.GetChild(UnityEngine.Random.Range(0, PMCExtractionPoints.childCount)));

                    // Send the path to sosigs using Sosig.CommandPathTo
                    sosigScript.CommandPathTo(PMCPoints, 0.1f, new Vector2(2, 5), 0.5f, Sosig.SosigMoveSpeed.Walking, Sosig.PathLoopType.Once, null, 45, 1, true, 5);
                    break;
                case AISpawn.AISpawnType.Boss:
                case AISpawn.AISpawnType.Follower:
                case AISpawn.AISpawnType.Raider:
                    if (AISquadIFFs.ContainsKey(spawnData.leaderName))
                    {
                        iff = AISquadIFFs[spawnData.leaderName];
                    }
                    else
                    {
                        if (availableIFFs.Count > 0)
                        {
                            iff = availableIFFs[availableIFFs.Count - 1];
                            availableIFFs.RemoveAt(availableIFFs.Count - 1);
                        }
                        else
                        {
                            iff = UnityEngine.Random.Range(2, 32);
                        }
                        AISquadIFFs.Add(spawnData.leaderName, iff);
                    }
                    ++spawnedIFFs[iff];

                    if((AISquads.ContainsKey(spawnData.leaderName) && AISquadSizes[spawnData.leaderName] == AISquads[spawnData.leaderName].Count + 1) || AISquadSizes[spawnData.leaderName] == 1)
                    {
                        // Generate a path to random points in the map
                        Transform pointsParentsRoot = transform.GetChild(transform.childCount - 3);
                        Transform containerPoints = pointsParentsRoot.GetChild(0);
                        Transform extractionPoints = pointsParentsRoot.GetChild(1);
                        Transform pointsOfInterest = pointsParentsRoot.GetChild(2);
                        List<Transform> points = new List<Transform>();
                        //TODO: Decide length of path depending on sosig speed, should look how long it takes them to navigate 5 points or something and base ourselves off of that
                        //TODO: Make sure there are no duplicate points being used
                        // For now just use default of 15 containers and 25 PoIs
                        for(int i = 0; i < 15; ++i)
                        {
                            points.Add(containerPoints.GetChild(UnityEngine.Random.Range(0, containerPoints.childCount)));
                        }
                        for(int i = 0; i < 25; ++i)
                        {
                            points.Add(pointsOfInterest.GetChild(UnityEngine.Random.Range(0, pointsOfInterest.childCount)));
                        }
                        points.Add(extractionPoints.GetChild(UnityEngine.Random.Range(0, extractionPoints.childCount)));

                        // Send the path to sosigs using Sosig.CommandPathTo
                        sosigScript.CommandPathTo(points, 0.1f, new Vector2(2, 5), 0.5f, Sosig.SosigMoveSpeed.Walking, Sosig.PathLoopType.Once, AISquadSizes[spawnData.leaderName] == 1 ? null : AISquads[spawnData.leaderName], 45, 1, true, 5);
                    }
                    else
                    {
                        sosigScript.CurrentOrder = Sosig.SosigOrder.Wander;
                        sosigScript.FallbackOrder = Sosig.SosigOrder.Wander;

                        sosigScript.SetCurrentOrder(sosigScript.FallbackOrder);
                    }

                    // Add it to the list after so the pathTo pathWith doesnt include itself
                    if (AISquads.ContainsKey(spawnData.leaderName))
                    {
                        AISquads[spawnData.leaderName].Add(sosigScript);
                    }
                    else
                    {
                        AISquads.Add(spawnData.leaderName, new List<Sosig> { sosigScript });
                    }
                    break;

            }
            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawning with IFF: "+iff);
            sosigScript.SetIFF(iff);
            sosigScript.SetOriginalIFFTeam(iff);

            sosigScript.Priority.SetAllEnemy();
            sosigScript.Priority.MakeFriendly(iff);

            sosigScript.CanBeGrabbed = false;
            sosigScript.CanBeKnockedOut = false;

            Mod.instance.LogInfo("SPAWNAI " + spawnData.name + ": \tConfigured AI");

            spawning = false;
            yield break;
        }

        public void OnBotKill(Sosig sosig)
        {
            Mod.instance.LogInfo("Sosig " + sosig.name + " killed");

            // Here we use GetOriginalIFFTeam instead of GetIFF because the actual IFF gets set to -3 on sosig death, original doesn't change
            --spawnedIFFs[sosig.GetOriginalIFFTeam()];
            if(spawnedIFFs[sosig.GetOriginalIFFTeam()] == 0)
            {
                availableIFFs.Add(sosig.GetOriginalIFFTeam());
            }

            // If player killed this bot
            EFM_AI AIScript = sosig.GetComponent<EFM_AI>();
            if (sosig.GetDiedFromIFF() == 0)
            {
                Mod.AddExperience(AIScript.experienceReward);

                Mod.killList.Add(sosig.name, AIScript.experienceReward);
            }

            // Remove entity from list
            entities[AIScript.entityIndex] = entities[entities.Count - 1];
            entityRelatedAI[AIScript.entityIndex] = entityRelatedAI[entityRelatedAI.Count - 1];
            entityRelatedAI[AIScript.entityIndex].entityIndex = AIScript.entityIndex;
            entities.RemoveAt(entities.Count - 1);
            entityRelatedAI.RemoveAt(entityRelatedAI.Count - 1);

            sosig.TickDownToClear(5);

            // Spawn inventory
            AnvilManager.Run(SpawnAIInventory(AIScript.inventory, sosig.transform.position + Vector3.up));
        }

        private IEnumerator SpawnAIInventory(AIInventory inventory, Vector3 pos)
        {
            // Spawn generic items
            Mod.instance.LogInfo("\tSpawning generic items");
            foreach(string genericItem in inventory.generic)
            {
                bool custom = false;
                GameObject itemPrefab = null;
                int parsedID = -1;
                Mod.instance.LogInfo("\t\t"+genericItem);
                if (int.TryParse(genericItem, out parsedID))
                {
                    itemPrefab = Mod.itemPrefabs[parsedID];
                    custom = true;
                }
                else
                {
                    yield return IM.OD[genericItem].GetGameObjectAsync();
                    itemPrefab = IM.OD[genericItem].GetGameObject();
                }

                if (custom)
                {
                    GameObject itemObject = Instantiate(itemPrefab, pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, itemCIW, null);

                    // Get amount
                    if (itemCIW.itemType == Mod.ItemType.Money)
                    {
                        if(parsedID == 201) // USD
                        {
                            itemCIW.stack = UnityEngine.Random.Range(20, 200);
                        }
                        else if(parsedID == 202) // Euro
                        {
                            itemCIW.stack = UnityEngine.Random.Range(10, 120);
                        }
                        else if(parsedID == 203) // Rouble
                        {
                            itemCIW.stack = UnityEngine.Random.Range(150, 5000);
                        }
                    }
                    else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                        for (int j = 0; j < itemCIW.maxAmount; ++j)
                        {
                            asMagazine.AddRound(itemCIW.roundClass, false, false);
                        }
                    }
                    else if (itemCIW.maxAmount > 0)
                    {
                        itemCIW.amount = itemCIW.maxAmount;
                    }
                }
                else // vanilla
                {
                    if (Mod.usedRoundIDs.Contains(genericItem))
                    {
                        // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of rounds
                        int amount = UnityEngine.Random.Range(10, 121);
                        FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                        FireArmRoundType roundType = round.RoundType;
                        FireArmRoundClass roundClass = round.RoundClass;

                        if (Mod.ammoBoxByAmmoID.ContainsKey(genericItem))
                        {
                            GameObject itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[genericItem]], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                            FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            for (int j = 0; j < amount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }

                            // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                            Mod.RemoveFromAll(null, itemCIW, null);
                        }
                        else // Spawn in generic box
                        {
                            GameObject itemObject = null;
                            if (amount > 30)
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[716], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            }
                            else
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[715], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            }

                            EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                            FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            asMagazine.RoundType = roundType;
                            itemCIW.roundClass = roundClass;
                            for (int j = 0; j < amount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }

                            // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                            Mod.RemoveFromAll(null, itemCIW, null);
                        }
                    }
                    else // Not a round, spawn as normal
                    {
                        Instantiate(itemPrefab, pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                    }
                }
                yield return null;
            }

            // Spawn rig
            if (inventory.rig != null)
            {
                Mod.instance.LogInfo("\tSpawning rig: "+inventory.rig);
                GameObject rigObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.rig)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;
                
                EFM_CustomItemWrapper rigCIW = rigObject.GetComponent<EFM_CustomItemWrapper>();

                for (int slotIndex = 0; slotIndex < inventory.rigContents.Length; ++slotIndex)
                {
                    if(inventory.rigContents[slotIndex] == null)
                    {
                        continue;
                    }

                    string itemID = inventory.rigContents[slotIndex];
                    Mod.instance.LogInfo("\t\tSpawning rig content "+slotIndex+": " + itemID);
                    bool custom = false;
                    GameObject itemPrefab = null;
                    int parsedID = -1;
                    if (int.TryParse(itemID, out parsedID))
                    {
                        itemPrefab = Mod.itemPrefabs[parsedID];
                        custom = true;
                    }
                    else
                    {
                        yield return IM.OD[itemID].GetGameObjectAsync();
                        itemPrefab = IM.OD[itemID].GetGameObject();
                    }

                    GameObject itemObject = null;
                    if (custom)
                    {
                        itemObject = Instantiate(itemPrefab);
                        EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                        FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                        // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                        Mod.RemoveFromAll(null, itemCIW, null);

                        // Get amount
                        if (itemCIW.itemType == Mod.ItemType.Money)
                        {
                            if (parsedID == 201) // USD
                            {
                                itemCIW.stack = UnityEngine.Random.Range(20, 200);
                            }
                            else if (parsedID == 202) // Euro
                            {
                                itemCIW.stack = UnityEngine.Random.Range(10, 120);
                            }
                            else if (parsedID == 203) // Rouble
                            {
                                itemCIW.stack = UnityEngine.Random.Range(150, 5000);
                            }
                        }
                        else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
                        {
                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            for (int j = 0; j < itemCIW.maxAmount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }
                        }
                        else if (itemCIW.maxAmount > 0)
                        {
                            itemCIW.amount = itemCIW.maxAmount;
                        }
                    }
                    else // vanilla
                    {
                        if (Mod.usedRoundIDs.Contains(itemID))
                        {
                            // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of rounds
                            int amount = UnityEngine.Random.Range(10, 121);
                            FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                            FireArmRoundType roundType = round.RoundType;
                            FireArmRoundClass roundClass = round.RoundClass;

                            if (Mod.ammoBoxByAmmoID.ContainsKey(itemID))
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[itemID]]);
                                EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                                FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                for (int j = 0; j < amount; ++j)
                                {
                                    asMagazine.AddRound(itemCIW.roundClass, false, false);
                                }

                                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                Mod.RemoveFromAll(null, itemCIW, null);
                            }
                            else // Spawn in generic box
                            {
                                if (amount > 30)
                                {
                                    itemObject = Instantiate(Mod.itemPrefabs[716]);
                                }
                                else
                                {
                                    itemObject = Instantiate(Mod.itemPrefabs[715]);
                                }

                                EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                                FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                asMagazine.RoundType = roundType;
                                itemCIW.roundClass = roundClass;
                                for (int j = 0; j < amount; ++j)
                                {
                                    asMagazine.AddRound(itemCIW.roundClass, false, false);
                                }

                                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                Mod.RemoveFromAll(null, itemCIW, null);
                            }
                        }
                        else // Not a round, spawn as normal
                        {
                            itemObject = Instantiate(itemPrefab);
                        }
                    }

                    itemObject.SetActive(false);
                    rigCIW.itemsInSlots[slotIndex] = itemObject;

                    yield return null;
                }

                rigCIW.UpdateRigMode();
            }

            // Spawn dogtags
            if (inventory.dogtag != null)
            {
                Mod.instance.LogInfo("\tSpawning dogtag: " + inventory.dogtag);
                GameObject dogtagObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.dogtag)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                EFM_CustomItemWrapper dogtagCIW = dogtagObject.GetComponent<EFM_CustomItemWrapper>();
                dogtagCIW.dogtagLevel = inventory.dogtagLevel;
                dogtagCIW.dogtagName = inventory.dogtagName;

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(null, dogtagCIW, null);

                yield return null;
            }

            // Spawn backpack
            if (inventory.backpack != null)
            {
                Mod.instance.LogInfo("\tSpawning backpack: " + inventory.backpack);
                GameObject backpackObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.backpack)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                EFM_CustomItemWrapper backpackCIW = backpackObject.GetComponent<EFM_CustomItemWrapper>();

                foreach (string backpackItem in inventory.backpackContents)
                {
                    Mod.instance.LogInfo("\t\tSpawning backpack content " + backpackItem);
                    bool custom = false;
                    GameObject itemPrefab = null;
                    int parsedID = -1;
                    if (int.TryParse(backpackItem, out parsedID))
                    {
                        itemPrefab = Mod.itemPrefabs[parsedID];
                        custom = true;
                    }
                    else
                    {
                        yield return IM.OD[backpackItem].GetGameObjectAsync();
                        itemPrefab = IM.OD[backpackItem].GetGameObject();
                    }

                    GameObject itemObject = null;
                    FVRPhysicalObject itemPhysObj = null;
                    if (custom)
                    {
                        itemObject = Instantiate(itemPrefab);
                        EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                        itemPhysObj = itemObject.GetComponent<FVRPhysicalObject>();

                        // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                        Mod.RemoveFromAll(null, itemCIW, null);

                        // Get amount
                        if (itemCIW.itemType == Mod.ItemType.Money)
                        {
                            if (parsedID == 201) // USD
                            {
                                itemCIW.stack = UnityEngine.Random.Range(20, 200);
                            }
                            else if (parsedID == 202) // Euro
                            {
                                itemCIW.stack = UnityEngine.Random.Range(10, 120);
                            }
                            else if (parsedID == 203) // Rouble
                            {
                                itemCIW.stack = UnityEngine.Random.Range(150, 5000);
                            }
                        }
                        else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
                        {
                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            for (int j = 0; j < itemCIW.maxAmount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }
                        }
                        else if (itemCIW.maxAmount > 0)
                        {
                            itemCIW.amount = itemCIW.maxAmount;
                        }
                    }
                    else // vanilla
                    {
                        if (Mod.usedRoundIDs.Contains(backpackItem))
                        {
                            // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of rounds
                            int amount = UnityEngine.Random.Range(10, 121);
                            FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                            FireArmRoundType roundType = round.RoundType;
                            FireArmRoundClass roundClass = round.RoundClass;

                            if (Mod.ammoBoxByAmmoID.ContainsKey(backpackItem))
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[backpackItem]]);
                                EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                                FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                for (int j = 0; j < amount; ++j)
                                {
                                    asMagazine.AddRound(itemCIW.roundClass, false, false);
                                }

                                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                Mod.RemoveFromAll(null, itemCIW, null);
                            }
                            else // Spawn in generic box
                            {
                                if (amount > 30)
                                {
                                    itemObject = Instantiate(Mod.itemPrefabs[716]);
                                }
                                else
                                {
                                    itemObject = Instantiate(Mod.itemPrefabs[715]);
                                }

                                EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                                FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                asMagazine.RoundType = roundType;
                                itemCIW.roundClass = roundClass;
                                for (int j = 0; j < amount; ++j)
                                {
                                    asMagazine.AddRound(itemCIW.roundClass, false, false);
                                }

                                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                Mod.RemoveFromAll(null, itemCIW, null);
                            }
                        }
                        else // Not a round, spawn as normal
                        {
                            itemObject = Instantiate(itemPrefab);
                            itemPhysObj = itemObject.GetComponent<FVRPhysicalObject>();
                        }
                    }

                    itemObject.SetActive(false);
                    
                    // Set item in the container, at random pos and rot, if volume fits
                    bool boxMainContainer = backpackCIW.mainContainer.GetComponent<BoxCollider>() != null;
                    if (backpackCIW.AddItemToContainer(itemPhysObj))
                    {
                        itemObject.transform.parent = backpackCIW.containerItemRoot;

                        if (boxMainContainer)
                        {
                            BoxCollider boxCollider = backpackCIW.mainContainer.GetComponent<BoxCollider>();
                            itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
                                                                             UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
                                                                             UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
                        }
                        else
                        {
                            CapsuleCollider capsuleCollider = backpackCIW.mainContainer.GetComponent<CapsuleCollider>();
                            if (capsuleCollider != null)
                            {
                                itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-capsuleCollider.radius / 2, capsuleCollider.radius / 2),
                                                                                 UnityEngine.Random.Range(-(capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.height / 2 - capsuleCollider.radius),
                                                                                 0);
                            }
                            else
                            {
                                itemObject.transform.localPosition = Vector3.zero;
                            }
                        }
                        itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
                    }
                    else // Could not add item to container, set it next to parent
                    {
                        itemObject.transform.position = backpackCIW.transform.position + Vector3.up; // 1m above parent
                    }

                    yield return null;
                }
            }

            // Spawn primary weapon
            if (inventory.primaryWeapon != null)
            {
                Mod.instance.LogInfo("\tSpawning primaryWeapon: " + inventory.primaryWeapon);
                yield return IM.OD[inventory.primaryWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.primaryWeapon].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if(weaponFireArm == null)
                {
                    Mod.instance.LogWarning("Sosig primary weapon not a firearm");
                    yield break;
                }

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<EFM_VanillaItemDescriptor>());

                AIInventoryWeaponMod currentParent = inventory.primaryWeaponMods;
                Stack<FVRPhysicalObject> parentPhysObjs = new Stack<FVRPhysicalObject>();
                parentPhysObjs.Push(weaponFireArm);
                Stack<int> childIndices = new Stack<int>();
                childIndices.Push(0);
                while(currentParent != null)
                {
                    if (currentParent.children != null && childIndices.Peek() < currentParent.children.Count)
                    {
                        string modID = currentParent.children[childIndices.Peek()].ID;
                        Mod.instance.LogInfo("\t\tSpawning primaryWeapon mod: " + modID);
                        // Spawn child at index
                        yield return IM.OD[modID].GetGameObjectAsync();
                        GameObject attachmentPrefab = IM.OD[modID].GetGameObject();
                        FVRPhysicalObject attachmentPrefabPhysObj = attachmentPrefab.GetComponent<FVRPhysicalObject>();
                        GameObject attachmentObject = null;
                        FVRPhysicalObject attachmentPhysObj = null;

                        // If mag or clip, must be loaded accordingly
                        // If round, fill chambers and mag/clip if applicable
                        // If attachment, attach to first mount that fits on the parent
                        if (attachmentPrefabPhysObj is FVRFireArmMagazine)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor magVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            magVID.takeCurrentLocation = false;
                            FVRFireArmMagazine attachmentMagazine = attachmentObject.GetComponent<FVRFireArmMagazine>();
                            attachmentPhysObj = attachmentMagazine;
                            FireArmLoadMagPatch.ignoreLoadMag = true;
                            attachmentMagazine.UsesVizInterp = false;
                            attachmentMagazine.Load(weaponFireArm);
                        }
                        else if (attachmentPrefabPhysObj is FVRFireArmClip)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor clipVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            clipVID.takeCurrentLocation = false;
                            FVRFireArmClip attachmentClip = attachmentObject.GetComponent<FVRFireArmClip>();
                            attachmentPhysObj = attachmentClip;
                            FireArmLoadClipPatch.ignoreLoadClip = true;
                            attachmentClip.Load(weaponFireArm);
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmRound)
                        {
                            FVRFireArmRound asRound = attachmentPrefabPhysObj as FVRFireArmRound;

                            // Fill chamber(s) then fill mag/clip as necessary
                            int chamberCount = weaponFireArm.GetChambers().Count;
                            List<FireArmRoundClass> chamberRounds = new List<FireArmRoundClass>();
                            for(int i = 0; i < chamberCount; ++i)
                            {
                                chamberRounds.Add(asRound.RoundClass);
                            }
                            weaponFireArm.SetLoadedChambers(chamberRounds);

                            if (weaponFireArm.UsesMagazines)
                            {
                                if(weaponFireArm.Magazine != null)
                                {
                                    weaponFireArm.Magazine.ReloadMagWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else if (weaponFireArm.UsesClips)
                            {
                                if (weaponFireArm.Clip != null)
                                {
                                    weaponFireArm.Clip.ReloadClipWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else // No ammo container, meaning we haven't planned to spawn aditional ammo for this weapon in rest of inventory so spawn a box of it
                            {
                                int amount = UnityEngine.Random.Range(10, 61);

                                if (Mod.ammoBoxByAmmoID.ContainsKey(modID))
                                {
                                    GameObject itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[modID]], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                                else // Spawn in generic box
                                {
                                    GameObject itemObject = null;
                                    if (amount > 30)
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[716], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }
                                    else
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[715], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }

                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    asMagazine.RoundType = asRound.RoundType;
                                    itemCIW.roundClass = asRound.RoundClass;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                            }
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmAttachment)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            FVRFireArmAttachment asAttachment = attachmentObject.GetComponent<FVRFireArmAttachment>();
                            attachmentPhysObj = asAttachment;
                            bool mountFound = false;
                            foreach(FVRFireArmAttachmentMount mount in parentPhysObjs.Peek().AttachmentMounts)
                            {
                                if(mount.Type == asAttachment.Type)
                                {
                                    mountFound = true;
                                    asAttachment.AttachToMount(mount, false);
                                    break;
                                }
                            }
                            if (!mountFound)
                            {
                                Mod.instance.LogWarning("Could not find compatible mount for mod: "+ modID + " on: "+currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Unhandle weapon mod type for mod with ID: "+ modID);
                        }

                        // Make this child the new parent
                        currentParent = currentParent.children[childIndices.Peek()];
                        parentPhysObjs.Push(attachmentPhysObj);

                        // Increment top index
                        childIndices.Push(childIndices.Pop() + 1);

                        // Push 0 on stack
                        childIndices.Push(0);

                        yield return null;
                    }
                    else // Spawned all children, go up hierarchy
                    {
                        childIndices.Pop();
                        currentParent = currentParent.parent;
                        parentPhysObjs.Pop();
                    }
                }
            }

            // Spawn secondary weapon
            if (inventory.secondaryWeapon != null)
            {
                Mod.instance.LogInfo("\tSpawning secondaryWeapon: " + inventory.secondaryWeapon);
                yield return IM.OD[inventory.secondaryWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.secondaryWeapon].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if(weaponFireArm == null)
                {
                    Mod.instance.LogWarning("Sosig secondary weapon not a firearm");
                    yield break;
                }

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<EFM_VanillaItemDescriptor>());

                AIInventoryWeaponMod currentParent = inventory.secondaryWeaponMods;
                Stack<FVRPhysicalObject> parentPhysObjs = new Stack<FVRPhysicalObject>();
                parentPhysObjs.Push(weaponFireArm);
                Stack<int> childIndices = new Stack<int>();
                childIndices.Push(0);
                while(currentParent != null)
                {
                    if (currentParent.children != null && childIndices.Peek() < currentParent.children.Count)
                    {
                        string modID = currentParent.children[childIndices.Peek()].ID;
                        Mod.instance.LogInfo("\t\tSpawning secondaryWeapon mod: " + modID);
                        // Spawn child at index
                        yield return IM.OD[modID].GetGameObjectAsync();
                        GameObject attachmentPrefab = IM.OD[modID].GetGameObject();
                        FVRPhysicalObject attachmentPrefabPhysObj = attachmentPrefab.GetComponent<FVRPhysicalObject>();
                        GameObject attachmentObject = null;
                        FVRPhysicalObject attachmentPhysObj = null;

                        // If mag or clip, must be loaded accordingly
                        // If round, fill chambers and mag/clip if applicable
                        // If attachment, attach to first mount that fits on the parent
                        if (attachmentPrefabPhysObj is FVRFireArmMagazine)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor magVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            magVID.takeCurrentLocation = false;
                            FVRFireArmMagazine attachmentMagazine = attachmentObject.GetComponent<FVRFireArmMagazine>();
                            attachmentPhysObj = attachmentMagazine;
                            attachmentMagazine.UsesVizInterp = false;
                            FireArmLoadMagPatch.ignoreLoadMag = true;
                            attachmentMagazine.Load(weaponFireArm);
                        }
                        else if (attachmentPrefabPhysObj is FVRFireArmClip)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor clipVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            clipVID.takeCurrentLocation = false;
                            FVRFireArmClip attachmentClip = attachmentObject.GetComponent<FVRFireArmClip>();
                            attachmentPhysObj = attachmentClip;
                            FireArmLoadClipPatch.ignoreLoadClip = true;
                            attachmentClip.Load(weaponFireArm);
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmRound)
                        {
                            FVRFireArmRound asRound = attachmentPrefabPhysObj as FVRFireArmRound;

                            // Fill chamber(s) then fill mag/clip as necessary
                            int chamberCount = weaponFireArm.GetChambers().Count;
                            List<FireArmRoundClass> chamberRounds = new List<FireArmRoundClass>();
                            for(int i = 0; i < chamberCount; ++i)
                            {
                                chamberRounds.Add(asRound.RoundClass);
                            }
                            weaponFireArm.SetLoadedChambers(chamberRounds);

                            if (weaponFireArm.UsesMagazines)
                            {
                                if(weaponFireArm.Magazine != null)
                                {
                                    weaponFireArm.Magazine.ReloadMagWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else if (weaponFireArm.UsesClips)
                            {
                                if (weaponFireArm.Clip != null)
                                {
                                    weaponFireArm.Clip.ReloadClipWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else // No ammo container, meaning we haven't planned to spawn aditional ammo for this weapon in rest of inventory so spawn a box of it
                            {
                                int amount = UnityEngine.Random.Range(10, 61);

                                if (Mod.ammoBoxByAmmoID.ContainsKey(modID))
                                {
                                    GameObject itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[modID]], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                                else // Spawn in generic box
                                {
                                    GameObject itemObject = null;
                                    if (amount > 30)
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[716], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }
                                    else
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[715], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }

                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    asMagazine.RoundType = asRound.RoundType;
                                    itemCIW.roundClass = asRound.RoundClass;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                            }
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmAttachment)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            FVRFireArmAttachment asAttachment = attachmentObject.GetComponent<FVRFireArmAttachment>();
                            attachmentPhysObj = asAttachment;
                            bool mountFound = false;
                            foreach(FVRFireArmAttachmentMount mount in parentPhysObjs.Peek().AttachmentMounts)
                            {
                                if(mount.Type == asAttachment.Type)
                                {
                                    mountFound = true;
                                    asAttachment.AttachToMount(mount, false);
                                    break;
                                }
                            }
                            if (!mountFound)
                            {
                                Mod.instance.LogWarning("Could not find compatible mount for mod: " + modID + " on: " + currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Unhandle weapon mod type for mod with ID: "+ modID);
                        }

                        // Make this child the new parent
                        currentParent = currentParent.children[childIndices.Peek()];
                        parentPhysObjs.Push(attachmentPhysObj);

                        // Increment top index
                        childIndices.Push(childIndices.Pop() + 1);

                        // Push 0 on stack
                        childIndices.Push(0);

                        yield return null;
                    }
                    else // Spawned all children, go up hierarchy
                    {
                        childIndices.Pop();
                        currentParent = currentParent.parent;
                        parentPhysObjs.Pop();
                    }
                }
            }

            // Spawn holster weapon
            if (inventory.holster != null)
            {
                Mod.instance.LogInfo("\tSpawning holster: " + inventory.holster);
                yield return IM.OD[inventory.holster].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.holster].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if(weaponFireArm == null)
                {
                    Mod.instance.LogWarning("Sosig holster weapon not a firearm");
                    yield break;
                }

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<EFM_VanillaItemDescriptor>());
                
                AIInventoryWeaponMod currentParent = inventory.holsterMods;
                Stack<FVRPhysicalObject> parentPhysObjs = new Stack<FVRPhysicalObject>();
                parentPhysObjs.Push(weaponFireArm);
                Stack<int> childIndices = new Stack<int>();
                childIndices.Push(0);
                while(currentParent != null)
                {
                    if (currentParent.children != null && childIndices.Peek() < currentParent.children.Count)
                    {
                        string modID = currentParent.children[childIndices.Peek()].ID;
                        Mod.instance.LogInfo("\t\tSpawning holster mod: " + modID);
                        // Spawn child at index
                        yield return IM.OD[modID].GetGameObjectAsync();
                        GameObject attachmentPrefab = IM.OD[modID].GetGameObject();
                        FVRPhysicalObject attachmentPrefabPhysObj = attachmentPrefab.GetComponent<FVRPhysicalObject>();
                        GameObject attachmentObject = null;
                        FVRPhysicalObject attachmentPhysObj = null;

                        // If mag or clip, must be loaded accordingly
                        // If round, fill chambers and mag/clip if applicable
                        // If attachment, attach to first mount that fits on the parent
                        if (attachmentPrefabPhysObj is FVRFireArmMagazine)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor magVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            magVID.takeCurrentLocation = false;
                            FVRFireArmMagazine attachmentMagazine = attachmentObject.GetComponent<FVRFireArmMagazine>();
                            attachmentPhysObj = attachmentMagazine;
                            attachmentMagazine.UsesVizInterp = false;
                            FireArmLoadMagPatch.ignoreLoadMag = true;
                            attachmentMagazine.Load(weaponFireArm);
                        }
                        else if (attachmentPrefabPhysObj is FVRFireArmClip)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            EFM_VanillaItemDescriptor clipVID = attachmentObject.GetComponent<EFM_VanillaItemDescriptor>();
                            clipVID.takeCurrentLocation = false;
                            FVRFireArmClip attachmentClip = attachmentObject.GetComponent<FVRFireArmClip>();
                            attachmentPhysObj = attachmentClip;
                            FireArmLoadClipPatch.ignoreLoadClip = true;
                            attachmentClip.Load(weaponFireArm);
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmRound)
                        {
                            FVRFireArmRound asRound = attachmentPrefabPhysObj as FVRFireArmRound;

                            // Fill chamber(s) then fill mag/clip as necessary
                            int chamberCount = weaponFireArm.GetChambers().Count;
                            List<FireArmRoundClass> chamberRounds = new List<FireArmRoundClass>();
                            for(int i = 0; i < chamberCount; ++i)
                            {
                                chamberRounds.Add(asRound.RoundClass);
                            }
                            weaponFireArm.SetLoadedChambers(chamberRounds);

                            if (weaponFireArm.UsesMagazines)
                            {
                                if(weaponFireArm.Magazine != null)
                                {
                                    weaponFireArm.Magazine.ReloadMagWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else if (weaponFireArm.UsesClips)
                            {
                                if (weaponFireArm.Clip != null)
                                {
                                    weaponFireArm.Clip.ReloadClipWithType(asRound.RoundClass);
                                }
                                //else This means we have a round before we have an ammocontainer. This round would probably be used to fill the chamber,
                                //  but we just fill the chambers with wtv we have in the ammo container anyway so can just ignore this round
                            }
                            else // No ammo container, meaning we haven't planned to spawn aditional ammo for this weapon in rest of inventory so spawn a box of it
                            {
                                int amount = UnityEngine.Random.Range(10, 61);

                                if (Mod.ammoBoxByAmmoID.ContainsKey(currentParent.children[childIndices.Peek()].ID))
                                {
                                    GameObject itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[modID]], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                                else // Spawn in generic box
                                {
                                    GameObject itemObject = null;
                                    if (amount > 30)
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[716], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }
                                    else
                                    {
                                        itemObject = Instantiate(Mod.itemPrefabs[715], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                                    }

                                    EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                    attachmentPhysObj = itemPhysObj;

                                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                                    asMagazine.RoundType = asRound.RoundType;
                                    itemCIW.roundClass = asRound.RoundClass;
                                    for (int j = 0; j < amount; ++j)
                                    {
                                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                                    }

                                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                                    Mod.RemoveFromAll(null, itemCIW, null);
                                }
                            }
                        }
                        else if(attachmentPrefabPhysObj is FVRFireArmAttachment)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            FVRFireArmAttachment asAttachment = attachmentObject.GetComponent<FVRFireArmAttachment>();
                            attachmentPhysObj = asAttachment; ;
                            bool mountFound = false;
                            foreach(FVRFireArmAttachmentMount mount in parentPhysObjs.Peek().AttachmentMounts)
                            {
                                if(mount.Type == asAttachment.Type)
                                {
                                    mountFound = true;
                                    asAttachment.AttachToMount(mount, false);
                                    break;
                                }
                            }
                            if (!mountFound)
                            {
                                Mod.instance.LogWarning("Could not find compatible mount for mod: " + modID + " on: " + currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Unhandle weapon mod type for mod with ID: "+ modID);
                        }

                        // Make this child the new parent
                        currentParent = currentParent.children[childIndices.Peek()];
                        parentPhysObjs.Push(attachmentPhysObj);

                        // Increment top index
                        childIndices.Push(childIndices.Pop() + 1);

                        // Push 0 on stack
                        childIndices.Push(0);

                        yield return null;
                    }
                    else // Spawned all children, go up hierarchy
                    {
                        childIndices.Pop();
                        currentParent = currentParent.parent;
                        parentPhysObjs.Pop();
                    }
                }
            }

            yield break;
        }

        private List<Transform> GetMostAvailableBotZones()
        {
            // Compile list of least filled spawn points, take a random one from the list
            Transform raiderZoneRoot = transform.GetChild(transform.childCount - 1).GetChild(1);
            List<Transform> availableBotZones = new List<Transform>();
            int least = int.MaxValue;
            for (int i = 0; i < raiderZoneRoot.childCount; ++i)
            {
                EFM_BotZone botZoneScript = raiderZoneRoot.GetChild(i).GetComponent<EFM_BotZone>();
                if(botZoneScript.botCount < least)
                {
                    least = botZoneScript.botCount;
                    availableBotZones.Clear();
                    availableBotZones.Add(botZoneScript.transform);
                }
                else if(botZoneScript.botCount == least)
                {
                    availableBotZones.Add(botZoneScript.transform);
                }
            }
            return availableBotZones;
        }

        private void InitReverb()
        {
            GameObject reverbSystemObject = transform.GetChild(transform.childCount - 4).gameObject;
            reverbSystemObject.SetActive(false); // Set inactive to prevent reverb system from awaking yet
            FVRReverbSystem reverbSystem = reverbSystemObject.AddComponent<FVRReverbSystem>();
            reverbSystem.Environments = new List<FVRReverbEnvironment>();
            bool setDefault = false;
            foreach (Transform t in reverbSystem.transform)
            {
                FVRSoundEnvironment soundEnvType = (FVRSoundEnvironment)Enum.Parse(typeof(FVRSoundEnvironment), t.name);
                FVRReverbEnvironment newReverbEnv = t.gameObject.AddComponent<FVRReverbEnvironment>();
                newReverbEnv.Environment = soundEnvType;
                newReverbEnv.SetPriorityBasedOnType();
                reverbSystem.Environments.Add(newReverbEnv);

                if (!setDefault)
                {
                    reverbSystem.DefaultEnvironment = newReverbEnv;
                    setDefault = true;
                }
            }
            reverbSystemObject.SetActive(true);
        }

        private void InitAI()
        {
            //Cult priest - sectantpriest
            bool spawnCultPriest = false;
            int cultPriestFollowerCount = 0;
            //Glukhar - bossgluhar
            bool spawnGlukhar = false;
            //Killa - bosskilla
            bool spawnKilla = false;
            //Reshala - bossbully
            bool spawnReshala = false;
            //Sanitar - bosssanitar
            bool spawnSanitar = false;
            //Shturman - bosskojaniy
            bool spawnShturman = false;
            //Tagilla - bosstagilla
            bool spawnTagilla = false;

            switch (Mod.chosenMapIndex)
            {
                case 0: // Factory
                    if(time >= 21600 && time <= 64800 && UnityEngine.Random.value <= 0.02f)
                    {
                        spawnCultPriest = true;
                        cultPriestFollowerCount = UnityEngine.Random.Range(2, 5);
                    }
                    if(UnityEngine.Random.value <= 0.1f)
                    {
                        spawnTagilla = true;
                    }
                    break;

                // TODO: Add other maps and number of raider squads if applicable to the map

                default:
                    break;
            }

            // Prep scav spawn zones
            foreach(Transform zone in transform.GetChild(transform.childCount - 1).GetChild(1))
            {
                zone.gameObject.AddComponent<EFM_BotZone>();
            }

            // Subscribe to events
            GM.CurrentSceneSettings.SosigKillEvent += this.OnBotKill;

            // Get location's base data
            locationData = Mod.locationsBaseDB[GetLocationDataIndex(Mod.chosenMapIndex)];
            float averageLevel = (float)locationData["AveragePlayerLevel"];
            maxRaidTime = (float)locationData["escape_time_limit"] * 60;
            maxBotPerZone = (int)locationData["MaxBotPerZone"];

            // Add AIManager to map
            gameObject.AddComponent<AIManager>();
            GM.CurrentAIManager.SonicThresholdDecayCurve = AnimationCurve.Linear(0, 1, 1, 0);
            GM.CurrentAIManager.LoudnessFalloff = AnimationCurve.Linear(0, 1, 1, 0);
            GM.CurrentAIManager.LM_Entity = LayerMask.GetMask("AIEntity");

            AISpawns = new List<AISpawn>();
            AISquadSpawnTransforms = new Dictionary<string, Transform>();
            availableIFFs = new List<int> { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 ,22, 23, 24, 25, 26, 27, 28, 29, 30, 31 };
            spawnedIFFs = new int[32];
            AISquadIFFs = new Dictionary<string, int>();
            AISquads = new Dictionary<string, List<Sosig>>();
            AISquadSizes = new Dictionary<string, int>();
            Mod.killList = new Dictionary<string, int>();
            entities = new List<AIEntity>();
            entities.Add(GM.CurrentPlayerBody.Hitboxes[0].MyE); // Add player as first entity
            entityRelatedAI = new List<EFM_AI>();
            entityRelatedAI.Add(null); // Place holder for player entity

            // Init AI Cover points
            Transform coverPointsParent = transform.GetChild(1).GetChild(1).GetChild(3).GetChild(0);
            GM.CurrentAIManager.CPM = gameObject.AddComponent<AICoverPointManager>();
            GM.CurrentAIManager.CPM.MyCoverPoints = new List<AICoverPoint>();
            for (int i = 0; i < coverPointsParent.childCount; ++i)
            {
                AICoverPoint newCoverPoint = coverPointsParent.GetChild(i).gameObject.AddComponent<AICoverPoint>();
                GM.CurrentAIManager.CPM.MyCoverPoints.Add(newCoverPoint);
            }

            // Bosses
            if (spawnCultPriest)
            {
                BotData bossPriestData = GetBotData("sectantpriest");
                BotData bossPriestFollowerData = GetBotData("sectantwarrior");
                AISpawn newAISpawn = GenerateAISpawn(bossPriestData, AISpawn.AISpawnType.Boss, averageLevel, "CultPriest");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                for(int i=0; i< cultPriestFollowerCount; ++i)
                {
                    AISpawn newFollowerAISpawn = GenerateAISpawn(bossPriestFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "CultPriest");
                    newFollowerAISpawn.spawnTime = 0;

                    AISpawns.Add(newFollowerAISpawn);
                }

                AISquadSizes.Add("CultPriest", cultPriestFollowerCount + 1);
            }
            if (spawnGlukhar)
            {
                BotData bossGlukharData = GetBotData("bossgluhar");
                BotData bossGlukharAssaultFollowerData = GetBotData("followergluharassault");
                BotData bossGlukharScoutFollowerData = GetBotData("followergluharscout");
                BotData bossGlukharSecurityFollowerData = GetBotData("followergluharsecurity");
                BotData bossGlukharSniperFollowerData = GetBotData("followergluharsnipe");
                AISpawn newAISpawn = GenerateAISpawn(bossGlukharData, AISpawn.AISpawnType.Boss, averageLevel, "Glukhar");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                // Spawn 3 assault and 1 of each other type
                for (int i = 0; i < 3; ++i)
                {
                    AISpawn newAssaultFollowerAISpawn = GenerateAISpawn(bossGlukharAssaultFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Glukhar");
                    newAssaultFollowerAISpawn.spawnTime = 0;

                    AISpawns.Add(newAssaultFollowerAISpawn);
                }
                AISpawn newScoutFollowerAISpawn = GenerateAISpawn(bossGlukharScoutFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Glukhar");
                newScoutFollowerAISpawn.spawnTime = 0;

                AISpawns.Add(newScoutFollowerAISpawn);

                AISpawn newSecurityFollowerAISpawn = GenerateAISpawn(bossGlukharSecurityFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Glukhar");
                newSecurityFollowerAISpawn.spawnTime = 0;

                AISpawns.Add(newSecurityFollowerAISpawn);

                AISpawn newSniperFollowerAISpawn = GenerateAISpawn(bossGlukharSniperFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Glukhar");
                newSniperFollowerAISpawn.spawnTime = 0;

                AISpawns.Add(newSniperFollowerAISpawn);

                AISquadSizes.Add("Glukhar", 7);
            }
            if (spawnKilla)
            {
                BotData bossKillaData = GetBotData("bosskilla");
                AISpawn newAISpawn = GenerateAISpawn(bossKillaData, AISpawn.AISpawnType.Boss, averageLevel, "Killa");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                AISquadSizes.Add("Killa", 1);
            }
            if (spawnReshala)
            {
                BotData bossReshalaData = GetBotData("bossbully");
                BotData bossReshalaFollowerData = GetBotData("followerbully");
                AISpawn newAISpawn = GenerateAISpawn(bossReshalaData, AISpawn.AISpawnType.Boss, averageLevel, "Reshala");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                for (int i = 0; i < 4; ++i)
                {
                    AISpawn newFollowerAISpawn = GenerateAISpawn(bossReshalaFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Reshala");
                    newFollowerAISpawn.spawnTime = 0;

                    AISpawns.Add(newFollowerAISpawn);
                }

                AISquadSizes.Add("Reshala", 5);
            }
            if (spawnSanitar)
            {
                BotData bossSanitarData = GetBotData("bosssanitar");
                BotData bossSanitarFollowerData = GetBotData("followersanitar");
                AISpawn newAISpawn = GenerateAISpawn(bossSanitarData, AISpawn.AISpawnType.Boss, averageLevel, "Sanitar");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                for (int i = 0; i < 2; ++i)
                {
                    AISpawn newFollowerAISpawn = GenerateAISpawn(bossSanitarFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Sanitar");
                    newFollowerAISpawn.spawnTime = 0;

                    AISpawns.Add(newFollowerAISpawn);
                }

                AISquadSizes.Add("Sanitar", 3);
            }
            if (spawnShturman)
            {
                BotData bossShturmanData = GetBotData("bosskojaniy");
                BotData bossShturmanFollowerData = GetBotData("followerkojaniy");
                AISpawn newAISpawn = GenerateAISpawn(bossShturmanData, AISpawn.AISpawnType.Boss, averageLevel, "Shturman");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                for (int i = 0; i < 2; ++i)
                {
                    AISpawn newFollowerAISpawn = GenerateAISpawn(bossShturmanFollowerData, AISpawn.AISpawnType.Follower, averageLevel, "Shturman");
                    newFollowerAISpawn.spawnTime = 0;

                    AISpawns.Add(newFollowerAISpawn);
                }

                AISquadSizes.Add("Shturman", 3);
            }
            if (spawnTagilla)
            {
                BotData bossTagillaData = GetBotData("bosstagilla");
                AISpawn newAISpawn = GenerateAISpawn(bossTagillaData, AISpawn.AISpawnType.Boss, averageLevel, "Tagilla");
                newAISpawn.spawnTime = 0;

                AISpawns.Add(newAISpawn);

                AISquadSizes.Add("Tagilla", 1);
            }

            // PMC
            int PMCSpawnCount = UnityEngine.Random.Range((int)locationData["MinPlayers"], (int)locationData["MaxPlayers"] + 1); // Amount of PMCs to spawn during raid
            if (PMCSpawnCount > 0)
            {
                for (int i = 0; i < PMCSpawnCount; ++i)
                {
                    AISpawn newAISpawn = GenerateAISpawn(null, AISpawn.AISpawnType.PMC, averageLevel);
                    newAISpawn.spawnTime = UnityEngine.Random.Range(0, maxRaidTime - 600);

                    AISpawns.Add(newAISpawn);
                }
            }

            // Scav
            BotData assaultBotData = GetBotData("assault");
            BotData marksmanBotData = GetBotData("marksman");
            foreach (JObject wave in (locationData["waves"] as JArray))
            {
                int scavSpawnCount = UnityEngine.Random.Range((int)wave["slots_min"], (int)wave["slots_max"] + 1);
                string spawnType = wave["WildSpawnType"].ToString();
                BotData botDataToUse = null;
                switch (spawnType)
                {
                    case "assault":
                        botDataToUse = assaultBotData;
                        break;
                    case "marksman":
                        botDataToUse = marksmanBotData;
                        break;
                    default:
                        Mod.instance.LogError("Unexpected scav spawn type: " + spawnType);
                        continue;
                }

                for (int i = 0; i < scavSpawnCount; ++i)
                {
                    AISpawn newAISpawn = GenerateAISpawn(botDataToUse, AISpawn.AISpawnType.Scav, averageLevel);
                    float waveTimeMin = (float)wave["time_min"];
                    float waveTimeMax = (float)wave["time_max"];
                    if (waveTimeMin == -1)
                    {
                        newAISpawn.spawnTime = 0;
                    }
                    else
                    {
                        newAISpawn.spawnTime = UnityEngine.Random.Range(waveTimeMin, waveTimeMax);
                    }

                    AISpawns.Add(newAISpawn);
                }
            }

            // Raiders
            int raiderSquadIndex = 0;
            BotData raiderBotData = GetBotData("pmcbot");
            foreach (JObject wave in (locationData["BossLocationSpawn"] as JArray))
            {
                if (!wave["BossName"].ToString().Equals("pmcBot") || UnityEngine.Random.value > ((float)wave["BossChance"]) / 100) 
                {
                    continue;
                }

                int escortSize = (int)wave["BossEscortAmount"][UnityEngine.Random.Range(0, (wave["BossEscortAmount"] as JArray).Count)] + 1;
                string raiderSquadName = "Raider" + raiderSquadIndex;


                AISpawn newAISpawn = GenerateAISpawn(raiderBotData, AISpawn.AISpawnType.Boss, averageLevel, raiderSquadName);
                float spawnTime = (float)wave["Time"];
                if (spawnTime == -1)
                {
                    newAISpawn.spawnTime = 0;
                }
                else
                {
                    newAISpawn.spawnTime = spawnTime;
                }

                AISpawns.Add(newAISpawn);

                for (int i = 0; i < escortSize; ++i)
                {
                    AISpawn newFollowerAISpawn = GenerateAISpawn(raiderBotData, AISpawn.AISpawnType.Follower, averageLevel, raiderSquadName);
                    newFollowerAISpawn.spawnTime = newAISpawn.spawnTime;

                    AISpawns.Add(newFollowerAISpawn);
                }

                AISquadSizes.Add(raiderSquadName, escortSize);
            }

            // Sort the spawns by spawn time decreasing
            SortAISpawns();

            // Set the first spawn
            nextSpawn = AISpawns.Count > 0 ? AISpawns[AISpawns.Count - 1] : null;
        }

        private void SortAISpawns()
        {
            bool sorted = false;
            while (!sorted)
            {
                bool moved = false;
                for (int i = 1; i < AISpawns.Count; ++i)
                {
                    if(AISpawns[i].spawnTime > AISpawns[i - 1].spawnTime)
                    {
                        AISpawn temp = AISpawns[i];
                        AISpawns[i] = AISpawns[i - 1];
                        AISpawns[i - 1] = temp;

                        moved = true;
                    }
                }
                sorted = !moved;
            }
        }

        private AISpawn GenerateAISpawn(BotData botData, AISpawn.AISpawnType AIType, float averageLevel, string leaderName = null)
        {

            bool USEC = UnityEngine.Random.value <= 0.5;
            BotData botDataToUse = botData;
            if (AIType == AISpawn.AISpawnType.PMC)
            {
                if (USEC)
                {
                    botDataToUse = GetBotData("usec");
                }
                else
                {
                    botDataToUse = GetBotData("bear");
                }
                Mod.instance.LogInfo("\tGot PMC bot data");
            }

            Mod.instance.LogInfo("GenerateAISpawn called, botData null?: " + (botDataToUse == null) + ", with type: " + AIType + " and name: " + (leaderName != null ? leaderName : "None"));
            AISpawn newAISpawn = new AISpawn();
            newAISpawn.type = AIType;
            newAISpawn.experienceReward = UnityEngine.Random.Range((int)botDataToUse.experience["reward"]["min"], (int)botDataToUse.experience["reward"]["max"]);
            newAISpawn.inventory = new AIInventory();
            newAISpawn.inventory.generic = new List<string>();
            newAISpawn.outfitByLink = new Dictionary<int, List<string>>();
            Mod.instance.LogInfo("\tInited newAISpawn");

            float level = Mathf.Min(80, ExpDistrRandOnAvg(averageLevel));

            newAISpawn.name = botDataToUse.names[UnityEngine.Random.Range(0, botDataToUse.names.Count)].ToString();
            Mod.instance.LogInfo("\tgot level: "+level+" and name: "+newAISpawn.name);

            if (AIType == AISpawn.AISpawnType.PMC)
            {
                if (USEC)
                {
                    newAISpawn.inventory.dogtag = "12";
                }
                else
                {
                    newAISpawn.inventory.dogtag = "11";
                }
                newAISpawn.inventory.dogtagLevel = (int)level;
                newAISpawn.inventory.dogtagName = newAISpawn.name;
                Mod.instance.LogInfo("\tSet dogtag");
            }
            else if(AIType == AISpawn.AISpawnType.Boss || AIType == AISpawn.AISpawnType.Follower || AIType == AISpawn.AISpawnType.Raider)
            {
                newAISpawn.leaderName = leaderName;
                Mod.instance.LogInfo("\tSet leader name");
            }

            // Get inventory data corresponding to level
            JObject inventoryDataToUse = null;
            for (int j = 0; j < botDataToUse.minInventoryLevels.Length; ++j)
            {
                if (level >= botDataToUse.minInventoryLevels[j])
                {
                    inventoryDataToUse = botDataToUse.inventoryDB[j];
                    break;
                }
            }
            Mod.instance.LogInfo("\tGot inventory data");

            // TODO: Set variables for difficulty

            // Set equipment
            Mod.instance.LogInfo("\tSetting head equipment");
            string[] headSlots = { "Headwear", "Earpiece", "FaceCover", "Eyewear" };
            bool[] headEquipImpossible = new bool[4];
            List<int> headOrder = new List<int> { 0, 1, 2, 3 };
            headOrder.Shuffle();
            for (int j = 0; j < 4; ++j)
            {
                int actualEquipIndex = headOrder[j];
                if (headEquipImpossible[actualEquipIndex])
                {
                    continue;
                }
                string actualEquipName = headSlots[actualEquipIndex];
                Mod.instance.LogInfo("\t\tSetting "+actualEquipName);

                if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"][actualEquipName]) / 100)
                {
                    JArray possibleHeadEquip = inventoryDataToUse["equipment"][actualEquipName] as JArray;
                    if (possibleHeadEquip.Count > 0)
                    {
                        string headEquipID = possibleHeadEquip[UnityEngine.Random.Range(0, possibleHeadEquip.Count)].ToString();
                        if (Mod.itemMap.ContainsKey(headEquipID))
                        {
                            string actualHeadEquipID = Mod.itemMap[headEquipID];
                            Mod.instance.LogInfo("\t\t\tChosen ID: " + actualHeadEquipID);
                            newAISpawn.inventory.generic.Add(actualHeadEquipID);

                            // Add sosig outfit item if applicable
                            if (Mod.globalDB["AIItemMap"][actualHeadEquipID] != null)
                            {
                                Mod.instance.LogInfo("\t\t\t\tGot in AIItemMap");
                                JObject outfitItemData = Mod.globalDB["AIItemMap"][actualHeadEquipID] as JObject;
                                int linkIndex = (int)outfitItemData["Link"];
                                List<string> equivalentIDs = outfitItemData["List"].ToObject<List<string>>();
                                if (newAISpawn.outfitByLink.ContainsKey(linkIndex))
                                {
                                    newAISpawn.outfitByLink[linkIndex].Add(equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)]);
                                }
                                else
                                {
                                    newAISpawn.outfitByLink.Add(linkIndex, new List<string> { equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)] });
                                }
                                Mod.instance.LogInfo("\t\t\t\tAdded to outfitByLink");
                            }

                            // Add any restrictions implied by this item
                            int parsedEquipID = int.Parse(actualHeadEquipID);
                            if (Mod.defaultItemsData["ItemDefaults"][parsedEquipID]["BlocksEarpiece"] != null)
                            {
                                for (int k = 0; k < 4; ++k)
                                {
                                    headEquipImpossible[k] = headEquipImpossible[k] || (bool)Mod.defaultItemsData["ItemDefaults"][parsedEquipID]["Blocks" + headSlots[k]];
                                }
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Missing item: " + headEquipID + " for PMC AI spawn " + actualEquipName);
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting tact vest");
            FVRPhysicalObject.FVRPhysicalObjectSize[] rigSlotSizes = null;
            List<string> rigWhitelist = new List<string>();
            List<string> rigBlacklist = new List<string>();
            bool rigArmored = false;
            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["TacticalVest"]) / 100)
            {
                JArray possibleRigs = inventoryDataToUse["equipment"]["TacticalVest"] as JArray;
                if (possibleRigs.Count > 0)
                {
                    string rigID = possibleRigs[UnityEngine.Random.Range(0, possibleRigs.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(rigID))
                    {
                        string actualRigID = Mod.itemMap[rigID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualRigID);
                        newAISpawn.inventory.rig = actualRigID;

                        // Add sosig outfit item if applicable
                        if (Mod.globalDB["AIItemMap"][actualRigID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIItemMap");
                            JObject outfitItemData = Mod.globalDB["AIItemMap"][actualRigID] as JObject;
                            int linkIndex = (int)outfitItemData["Link"];
                            List<string> equivalentIDs = outfitItemData["List"].ToObject<List<string>>();
                            if (newAISpawn.outfitByLink.ContainsKey(linkIndex))
                            {
                                newAISpawn.outfitByLink[linkIndex].Add(equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)]);
                            }
                            else
                            {
                                newAISpawn.outfitByLink.Add(linkIndex, new List<string> { equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)] });
                            }
                            Mod.instance.LogInfo("\t\t\tAdded to outfitByLink");
                        }

                        // Get prefab of the rig to get the sizes and number of slots
                        // Keep sizes in an array here to use to decide where to spawn what item when we set those
                        // Set number of slots by initializing aispawn.inventory.rigcontents array with the correct length
                        // also keep the whitelist and blacklist from default item data so we can decide if can actually spawn items in this rig later on
                        // also based on whether this is an itemtype 2 (rig) or 3 (armored rig), set a bool to tell whether we can spawn a equipment of type ArmorVest after this
                        int parsedEquipID = int.Parse(actualRigID);
                        GameObject rigPrefab = Mod.itemPrefabs[parsedEquipID];
                        EFM_CustomItemWrapper rigCIW = rigPrefab.GetComponent<EFM_CustomItemWrapper>();
                        rigSlotSizes = new FVRPhysicalObject.FVRPhysicalObjectSize[rigCIW.rigSlots.Count];
                        for (int j = 0; j < rigCIW.rigSlots.Count; ++j)
                        {
                            rigSlotSizes[j] = rigCIW.rigSlots[j].SizeLimit;
                        }
                        newAISpawn.inventory.rigContents = new string[rigCIW.rigSlots.Count];
                        rigWhitelist = rigCIW.whiteList;
                        rigBlacklist = rigCIW.blackList;
                        rigArmored = rigCIW.itemType == Mod.ItemType.ArmoredRig;
                        Mod.instance.LogInfo("\t\tProcessed data from rig CIW");
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + rigID + " for PMC AI spawn Rig");
                    }
                }
            }

            if (!rigArmored)
            {
                Mod.instance.LogInfo("\tSetting armor");
                if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["ArmorVest"]) / 100)
                {
                    JArray possibleArmors = inventoryDataToUse["equipment"]["ArmorVest"] as JArray;
                    if (possibleArmors.Count > 0)
                    {
                        string armorID = possibleArmors[UnityEngine.Random.Range(0, possibleArmors.Count)].ToString();
                        if (Mod.itemMap.ContainsKey(armorID))
                        {
                            string actualArmorID = Mod.itemMap[armorID];
                            Mod.instance.LogInfo("\t\tChosen ID: " + actualArmorID);
                            newAISpawn.inventory.generic.Add(actualArmorID);

                            // Add sosig outfit item if applicable
                            if (Mod.globalDB["AIItemMap"][actualArmorID] != null)
                            {
                                Mod.instance.LogInfo("\t\t\tGot in AIItemMap");
                                JObject outfitItemData = Mod.globalDB["AIItemMap"][actualArmorID] as JObject;
                                int linkIndex = (int)outfitItemData["Link"];
                                List<string> equivalentIDs = outfitItemData["List"].ToObject<List<string>>();
                                if (newAISpawn.outfitByLink.ContainsKey(linkIndex))
                                {
                                    newAISpawn.outfitByLink[linkIndex].Add(equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)]);
                                }
                                else
                                {
                                    newAISpawn.outfitByLink.Add(linkIndex, new List<string> { equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)] });
                                }
                                Mod.instance.LogInfo("\t\t\tAdded to outfitByLink");
                            }
                        }
                        else
                        {
                            Mod.instance.LogError("Missing item: " + armorID + " for PMC AI spawn Armor");
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting backpack");
            List<string> backpackWhitelist = new List<string>();
            List<string> backpackBlacklist = new List<string>();
            float maxBackpackVolume = -1;
            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["Backpack"]) / 100)
            {
                JArray possibleBackpacks = inventoryDataToUse["equipment"]["Backpack"] as JArray;
                if (possibleBackpacks.Count > 0)
                {
                    string backpackID = possibleBackpacks[UnityEngine.Random.Range(0, possibleBackpacks.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(backpackID))
                    {
                        string actualBackpackID = Mod.itemMap[backpackID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualBackpackID);
                        newAISpawn.inventory.backpack = actualBackpackID;

                        // Add sosig outfit item if applicable
                        if (Mod.globalDB["AIItemMap"][actualBackpackID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIItemMap");
                            JObject outfitItemData = Mod.globalDB["AIItemMap"][actualBackpackID] as JObject;
                            int linkIndex = (int)outfitItemData["Link"];
                            List<string> equivalentIDs = outfitItemData["List"].ToObject<List<string>>();
                            if (newAISpawn.outfitByLink.ContainsKey(linkIndex))
                            {
                                newAISpawn.outfitByLink[linkIndex].Add(equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)]);
                            }
                            else
                            {
                                newAISpawn.outfitByLink.Add(linkIndex, new List<string> { equivalentIDs[UnityEngine.Random.Range(0, equivalentIDs.Count)] });
                            }
                            Mod.instance.LogInfo("\t\t\tAdded to outfitByLink");
                        }

                        // Get backpack data
                        int parsedEquipID = int.Parse(actualBackpackID);
                        JObject defaultBackpackData = Mod.defaultItemsData["ItemDefaults"][parsedEquipID] as JObject;
                        backpackWhitelist = defaultBackpackData["ContainerProperties"]["WhiteList"].ToObject<List<string>>();
                        backpackBlacklist = defaultBackpackData["ContainerProperties"]["BlackList"].ToObject<List<string>>();
                        maxBackpackVolume = (float)defaultBackpackData["BackpackProperties"]["MaxVolume"];
                        newAISpawn.inventory.backpackContents = new List<string>();
                        Mod.instance.LogInfo("\tProcessed backpack data");
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + backpackID + " for PMC AI spawn Rig");
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting weapon");
            bool hasSosigWeapon = false;
            string[] ammoContainers = new string[3];
            string[] weaponCartridges = new string[3];
            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["FirstPrimaryWeapon"]) / 100)
            {
                JArray possiblePW = inventoryDataToUse["equipment"]["FirstPrimaryWeapon"] as JArray;
                if (possiblePW.Count > 0)
                {
                    string PWID = possiblePW[UnityEngine.Random.Range(0, possiblePW.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(PWID))
                    {
                        string actualPWID = Mod.itemMap[PWID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualPWID);
                        newAISpawn.inventory.primaryWeapon = actualPWID;

                        // Add sosig weapon
                        if (Mod.globalDB["AIWeaponMap"][actualPWID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIWeaponMap");
                            newAISpawn.sosigWeapon = Mod.globalDB["AIWeaponMap"][actualPWID].ToString();
                            hasSosigWeapon = true;
                        }

                        // Get firearm data
                        GameObject PWPrefab = IM.OD[actualPWID].GetGameObject();
                        FVRFireArm PWFirearm = PWPrefab.GetComponent<FVRFireArm>();
                        if(PWFirearm != null)
                        {
                            AIInventoryWeaponMod weaponModTree = new AIInventoryWeaponMod(null, actualPWID, null); ;
                            newAISpawn.inventory.primaryWeaponMods = weaponModTree;
                            BuildModTree(ref weaponModTree, botDataToUse, inventoryDataToUse, PWID, ref ammoContainers[0], ref weaponCartridges[0]);
                        }
                        else
                        {
                            Mod.instance.LogError("AI FirstPrimaryWeapon with ID: "+ actualPWID +" is not a firearm");
                        }
                        Mod.instance.LogInfo("\t\tProcessed weapon data and mods");
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + PWID + " for PMC AI spawn Primary weapon");
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting secondary weapon");
            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["SecondPrimaryWeapon"]) / 100)
            {
                JArray possibleSW = inventoryDataToUse["equipment"]["SecondPrimaryWeapon"] as JArray;
                if (possibleSW.Count > 0)
                {
                    string SWID = possibleSW[UnityEngine.Random.Range(0, possibleSW.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(SWID))
                    {
                        string actualSWID = Mod.itemMap[SWID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualSWID);
                        newAISpawn.inventory.secondaryWeapon = actualSWID;

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualSWID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIWeaponMap");
                            newAISpawn.sosigWeapon = Mod.globalDB["AIWeaponMap"][actualSWID].ToString();
                            hasSosigWeapon = true;
                        }

                        // Get firearm data
                        GameObject SWPrefab = IM.OD[actualSWID].GetGameObject();
                        FVRFireArm SWFirearm = SWPrefab.GetComponent<FVRFireArm>();
                        if (SWFirearm != null)
                        {
                            AIInventoryWeaponMod weaponModTree = new AIInventoryWeaponMod(null, actualSWID, null); ;
                            newAISpawn.inventory.secondaryWeaponMods = weaponModTree;
                            BuildModTree(ref weaponModTree, botDataToUse, inventoryDataToUse, SWID, ref ammoContainers[1], ref weaponCartridges[1]);
                        }
                        else
                        {
                            Mod.instance.LogError("AI SecondPrimaryWeapon with ID: " + actualSWID + " is not a firearm");
                        }
                        Mod.instance.LogInfo("\t\tProcessed weapon data and mods");
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + SWID + " for PMC AI spawn Secondary weapon");
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting holster");
            if (rigSlotSizes != null && UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["Holster"]) / 100)
            {
                JArray possibleHolster = inventoryDataToUse["equipment"]["Holster"] as JArray;
                if (possibleHolster.Count > 0)
                {
                    string holsterID = possibleHolster[UnityEngine.Random.Range(0, possibleHolster.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(holsterID))
                    {
                        string actualHolsterID = Mod.itemMap[holsterID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualHolsterID);
                        newAISpawn.inventory.holster = actualHolsterID;

                        // Set to holster slot in rig
                        for (int i = 0; i < rigSlotSizes.Length; ++i)
                        {
                            if (newAISpawn.inventory.rigContents[i] == null && rigSlotSizes[i] >= FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
                            {
                                newAISpawn.inventory.rigContents[i] = actualHolsterID;
                                break;
                            }
                        }

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualHolsterID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIWeaponMap");
                            newAISpawn.sosigWeapon = Mod.globalDB["AIWeaponMap"][actualHolsterID].ToString();
                            hasSosigWeapon = true;
                        }

                        // Get firearm data
                        GameObject holsterPrefab = IM.OD[actualHolsterID].GetGameObject();
                        FVRFireArm holsterFirearm = holsterPrefab.GetComponent<FVRFireArm>();
                        if (holsterFirearm != null)
                        {
                            AIInventoryWeaponMod weaponModTree = new AIInventoryWeaponMod(null, actualHolsterID, null); ;
                            newAISpawn.inventory.holsterMods = weaponModTree;
                            BuildModTree(ref weaponModTree, botDataToUse, inventoryDataToUse, holsterID, ref ammoContainers[2], ref weaponCartridges[2]);
                        }
                        else
                        {
                            Mod.instance.LogError("AI Holster with ID: " + actualHolsterID + " is not a firearm");
                        }
                        Mod.instance.LogInfo("\t\tProcessed weapon data and mods");
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + holsterID + " for PMC AI spawn Holster");
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting Scabbard");
            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["Scabbard"]) / 100)
            {
                JArray possibleScabbard = inventoryDataToUse["equipment"]["Scabbard"] as JArray;
                if (possibleScabbard.Count > 0)
                {
                    string scabbardID = possibleScabbard[UnityEngine.Random.Range(0, possibleScabbard.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(scabbardID))
                    {
                        string actualScabbardID = Mod.itemMap[scabbardID];
                        Mod.instance.LogInfo("\t\tChosen ID: " + actualScabbardID);
                        newAISpawn.inventory.generic.Add(actualScabbardID);

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualScabbardID] != null)
                        {
                            Mod.instance.LogInfo("\t\t\tGot in AIWeaponMap");
                            newAISpawn.sosigWeapon = Mod.globalDB["AIWeaponMap"][actualScabbardID].ToString();
                            hasSosigWeapon = true;
                        }
                    }
                    else
                    {
                        Mod.instance.LogError("Missing item: " + scabbardID + " for PMC AI spawn Scabbard");
                    }
                }
            }

            if (!hasSosigWeapon)
            {
                Mod.instance.LogError(AIType.ToString() + " typr AI with name: " + newAISpawn.name+", has no weapon");
            }

            // Set items depending on generation limits
            int pocketsUsed = 0;
            float currentBackpackVolume = 0;

            Mod.instance.LogInfo("\tSetting magazines");
            int ammoContainerItemMin = (int)botDataToUse.generation["items"]["magazines"]["min"];
            int ammoContainerItemMax = (int)botDataToUse.generation["items"]["magazines"]["max"];
            if (ammoContainerItemMax > 0)
            {
                for (int k = 0; k < 3; ++k)
                {
                    if(ammoContainers[k] == null)
                    {
                        continue;
                    }

                    Mod.instance.LogInfo("\t\tFor weapon index: "+ k);
                    for (int i = 0; i < ammoContainerItemMax; ++i)
                    {
                        string ammoContainerItemID = ammoContainers[k];
                        Mod.instance.LogInfo("\t\t\tGot ammoContainerItemID: "+ ammoContainerItemID);
                        object[] ammoContainerItemData = GetItemData(ammoContainerItemID);

                        if (i >= ammoContainerItemMin && UnityEngine.Random.value > (float.Parse(ammoContainerItemData[2] as string) / 100))
                        {
                            continue;
                        }

                        FVRPhysicalObject.FVRPhysicalObjectSize ammoContainerItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(ammoContainerItemData[0] as string);
                        float ammoContainerItemVolume = float.Parse(ammoContainerItemData[1] as string);

                        // First try to put in rig, then pockets, then backpack
                        bool success = false;
                        if (rigSlotSizes != null)
                        {
                            for (int j = 0; j < rigSlotSizes.Length; ++j)
                            {
                                if (newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)ammoContainerItemSize)
                                {
                                    newAISpawn.inventory.rigContents[j] = ammoContainerItemID;
                                    success = true;
                                    break;
                                }
                            }
                        }
                        if (!success && ammoContainerItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                        {
                            newAISpawn.inventory.generic.Add(ammoContainerItemID);
                            ++pocketsUsed;
                            success = true;
                        }
                        if (!success && maxBackpackVolume > 0 && currentBackpackVolume + ammoContainerItemVolume <= maxBackpackVolume)
                        {
                            newAISpawn.inventory.backpackContents.Add(ammoContainerItemID);
                            currentBackpackVolume += ammoContainerItemVolume;
                        }
                        Mod.instance.LogInfo("\t\t\tAdded to inventory");
                    }
                }
            }

            // Fill lists of possible types of items for specific parts
            Mod.instance.LogInfo("\tGetting part specific content");
            Dictionary<string, object[]> possibleHealingItems = new Dictionary<string, object[]>(); // 0 - List of part indices, 1 - size, 2 - volume, 3 - spawn chance
            Dictionary<string, object[]> possibleGrenades = new Dictionary<string, object[]>();
            Dictionary<string, object[]> possibleLooseLoot = new Dictionary<string, object[]>();
            string[] itemParts = new string[] { "TacticalVest", "Pockets", "Backpack" };
            for (int i = 0; i < 3; ++i)
            {
                Mod.instance.LogInfo("\t\tChecking items in: " + itemParts[i]);
                foreach (string itemID in inventoryDataToUse["items"][itemParts[i]])
                {
                    if (Mod.itemMap.ContainsKey(itemID))
                    {
                        string actualItemID = Mod.itemMap[itemID];
                        GameObject itemPrefab = null;
                        bool custom = false;
                        if (int.TryParse(actualItemID, out int parseResult))
                        {
                            itemPrefab = Mod.itemPrefabs[parseResult];
                            custom = true;
                        }
                        else
                        {
                            itemPrefab = IM.OD[actualItemID].GetGameObject();
                        }
                        List<string> itemParents = null;
                        int itemVolume = 0;
                        float itemSpawnChance = 0;
                        FVRPhysicalObject itemPhysObj = null;
                        Mod.instance.LogInfo("\t\t\t"+ actualItemID+" custom?: "+custom);
                        if (custom)
                        {
                            EFM_CustomItemWrapper itemCIW = itemPrefab.GetComponentInChildren<EFM_CustomItemWrapper>();
                            if(itemCIW == null)
                            {
                                Mod.instance.LogError("Could spawn Item "+itemID+" in "+ newAISpawn.name + "'s "+itemParts[i]+" but is custom and has no CIW");
                                continue;
                            }
                            itemParents = itemCIW.parents;
                            itemVolume = itemCIW.volumes[0];
                            itemSpawnChance = itemCIW.spawnChance;
                        }
                        else
                        {
                            EFM_VanillaItemDescriptor itemVID = itemPrefab.GetComponentInChildren<EFM_VanillaItemDescriptor>();
                            if (itemVID == null)
                            {
                                Mod.instance.LogError("Could spawn Item " + itemID + " in " + newAISpawn.name + "'s " + itemParts[i] + " but is not custom and has no VID");
                                continue;
                            }
                            itemParents = itemVID.parents;
                            itemVolume = Mod.itemVolumes[actualItemID];
                            itemSpawnChance = itemVID.spawnChance;
                        }
                        itemPhysObj = itemPrefab.GetComponentInChildren<FVRPhysicalObject>();

                        if (itemParents.Contains("5448f3ac4bdc2dce718b4569")) // Medical item
                        {
                            if (possibleHealingItems.ContainsKey(actualItemID))
                            {
                                (possibleHealingItems[actualItemID][0] as List<int>).Add(i);
                            }
                            else
                            {
                                possibleHealingItems.Add(actualItemID, new object[] { new List<int> { i }, ((int)itemPhysObj.Size).ToString(), itemVolume.ToString(), itemSpawnChance.ToString() });
                            }
                        }
                        else if (itemParents.Contains("543be6564bdc2df4348b4568")) // Grenade
                        {
                            if (possibleGrenades.ContainsKey(actualItemID))
                            {
                                (possibleGrenades[actualItemID][0] as List<int>).Add(i);
                            }
                            else
                            {
                                possibleGrenades.Add(actualItemID, new object[] { new List<int> { i }, ((int)itemPhysObj.Size).ToString(), itemVolume.ToString(), itemSpawnChance.ToString() });
                            }
                        }
                        else // Loose loot
                        {
                            if (possibleLooseLoot.ContainsKey(actualItemID))
                            {
                                (possibleLooseLoot[actualItemID][0] as List<int>).Add(i);
                            }
                            else
                            {
                                possibleLooseLoot.Add(actualItemID, new object[] { new List<int> { i }, ((int)itemPhysObj.Size).ToString(), itemVolume.ToString(), itemSpawnChance.ToString() });
                            }
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\t Got a list of " + possibleHealingItems.Count + " possible healing items");
            Mod.instance.LogInfo("\t Got a list of " + possibleGrenades.Count + " possible grenades");
            Mod.instance.LogInfo("\t Got a list of " + possibleLooseLoot.Count + " possible loose loot items");

            Mod.instance.LogInfo("\tSetting healing items");
            int healingItemMin = (int)botDataToUse.generation["items"]["healing"]["min"];
            int healingItemMax = (int)botDataToUse.generation["items"]["healing"]["max"];
            if (healingItemMax > 0 && possibleHealingItems.Count > 0)
            {
                List<string> healingItemKeyList = new List<string>(possibleHealingItems.Keys);
                for (int i=0; i < healingItemMax; ++i)
                {
                    string healingItemID = healingItemKeyList[UnityEngine.Random.Range(0, healingItemKeyList.Count)];
                    object[] healingItemData = possibleHealingItems[healingItemID];
                    List<int> possibleParts = possibleHealingItems[healingItemID][0] as List<int>;

                    if (i >= healingItemMin && UnityEngine.Random.value > (float.Parse(healingItemData[3] as string) / 100))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize healingItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(healingItemData[1] as string);
                    float healingItemVolume = float.Parse(healingItemData[2] as string);

                    // First try to put in pockets, then backpack, then rig
                    bool success = false;
                    if (possibleParts.Contains(1) && healingItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                    {
                        newAISpawn.inventory.generic.Add(healingItemID);
                        ++pocketsUsed;
                        success = true;
                    }
                    if(!success && possibleParts.Contains(2) && maxBackpackVolume > 0 && currentBackpackVolume + healingItemVolume <= maxBackpackVolume)
                    {
                        newAISpawn.inventory.backpackContents.Add(healingItemID);
                        currentBackpackVolume += healingItemVolume;
                        success = true;
                    }
                    if(!success && possibleParts.Contains(0) && rigSlotSizes != null)
                    {
                        for(int j=0; j< rigSlotSizes.Length; ++j)
                        {
                            if(newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)healingItemSize)
                            {
                                newAISpawn.inventory.rigContents[j] = healingItemID;
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting grenades");
            int grenadeItemMin = (int)botDataToUse.generation["items"]["grenades"]["min"];
            int grenadeItemMax = (int)botDataToUse.generation["items"]["grenades"]["max"];
            if (grenadeItemMax > 0 && possibleGrenades.Count > 0)
            {
                List<string> grenadeItemKeyList = new List<string>(possibleGrenades.Keys);
                for (int i=0; i < grenadeItemMax; ++i)
                {
                    string grenadeItemID = grenadeItemKeyList[UnityEngine.Random.Range(0, grenadeItemKeyList.Count)];
                    object[] grenadeItemData = possibleGrenades[grenadeItemID];
                    List<int> possibleParts = possibleGrenades[grenadeItemID][0] as List<int>;

                    if (i >= grenadeItemMin && UnityEngine.Random.value > (float.Parse(grenadeItemData[3] as string) / 100))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize grenadeItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(grenadeItemData[1] as string);
                    float grenadeItemVolume = float.Parse(grenadeItemData[2] as string);

                    // First try to put in pockets, then backpack, then rig
                    bool success = false;
                    if (possibleParts.Contains(0) && rigSlotSizes != null)
                    {
                        for (int j = 0; j < rigSlotSizes.Length; ++j)
                        {
                            if (newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)grenadeItemSize)
                            {
                                newAISpawn.inventory.rigContents[j] = grenadeItemID;
                                newAISpawn.sosigGrenade = Mod.globalDB["AIWeaponMap"][grenadeItemID].ToString();
                                success = true;
                                break;
                            }
                        }
                    }
                    if (!success && possibleParts.Contains(1) && grenadeItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                    {
                        newAISpawn.inventory.generic.Add(grenadeItemID);
                        newAISpawn.sosigGrenade = Mod.globalDB["AIWeaponMap"][grenadeItemID].ToString();
                        ++pocketsUsed;
                        success = true;
                    }
                    if(!success && possibleParts.Contains(2) && maxBackpackVolume > 0 && currentBackpackVolume + grenadeItemVolume <= maxBackpackVolume)
                    {
                        newAISpawn.inventory.backpackContents.Add(grenadeItemID);
                        newAISpawn.sosigGrenade = Mod.globalDB["AIWeaponMap"][grenadeItemID].ToString();
                        currentBackpackVolume += grenadeItemVolume;
                        success = true;
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting looseloot");
            int looseLootItemMin = (int)botDataToUse.generation["items"]["looseLoot"]["min"];
            int looseLootItemMax = (int)botDataToUse.generation["items"]["looseLoot"]["max"];
            if (looseLootItemMax > 0 && possibleLooseLoot.Count > 0)
            {
                List<string> looseLootItemKeyList = new List<string>(possibleLooseLoot.Keys);
                for (int i=0; i < looseLootItemMax; ++i)
                {
                    string looseLootItemID = looseLootItemKeyList[UnityEngine.Random.Range(0, looseLootItemKeyList.Count)];
                    object[] looseLootItemData = possibleLooseLoot[looseLootItemID];
                    List<int> possibleParts = possibleLooseLoot[looseLootItemID][0] as List<int>;

                    if (i >= looseLootItemMin && UnityEngine.Random.value > (float.Parse(looseLootItemData[3] as string) / 100))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize looseLootItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(looseLootItemData[1] as string);
                    float looseLootItemVolume = float.Parse(looseLootItemData[2] as string);

                    // First try to put in backpack, then pockets, then rig
                    bool success = false;
                    if (possibleParts.Contains(2) && maxBackpackVolume > 0 && currentBackpackVolume + looseLootItemVolume <= maxBackpackVolume)
                    {
                        newAISpawn.inventory.backpackContents.Add(looseLootItemID);
                        currentBackpackVolume += looseLootItemVolume;
                        success = true;
                    }
                    if (!success && possibleParts.Contains(1) && looseLootItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                    {
                        newAISpawn.inventory.generic.Add(looseLootItemID);
                        ++pocketsUsed;
                        success = true;
                    }
                    if (!success && possibleParts.Contains(0) && rigSlotSizes != null)
                    {
                        for(int j=0; j< rigSlotSizes.Length; ++j)
                        {
                            if(newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)looseLootItemSize)
                            {
                                newAISpawn.inventory.rigContents[j] = looseLootItemID;
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\tSetting special items");
            int specialItemMin = (int)botDataToUse.generation["items"]["specialItems"]["min"];
            int specialItemMax = (int)botDataToUse.generation["items"]["specialItems"]["max"];
            List<string> possibleSpecialItems = inventoryDataToUse["items"]["SpecialLoot"].ToObject<List<string>>();
            if (specialItemMax > 0 && possibleSpecialItems.Count > 0)
            {
                for (int i = 0; i < specialItemMax; ++i)
                {
                    string specialItemID = Mod.itemMap[possibleSpecialItems[UnityEngine.Random.Range(0, possibleSpecialItems.Count)]];
                    object[] specialItemData = GetItemData(specialItemID);

                    if (i >= specialItemMin && UnityEngine.Random.value > (float.Parse(specialItemData[2] as string) / 100))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize specialItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(specialItemData[0] as string);
                    float specialItemVolume = float.Parse(specialItemData[1] as string);

                    // First try to put in backpack, then pockets, then rig
                    bool success = false;
                    if (maxBackpackVolume > 0 && currentBackpackVolume + specialItemVolume <= maxBackpackVolume)
                    {
                        newAISpawn.inventory.backpackContents.Add(specialItemID);
                        currentBackpackVolume += specialItemVolume;
                        success = true;
                    }
                    if (!success && specialItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                    {
                        newAISpawn.inventory.generic.Add(specialItemID);
                        ++pocketsUsed;
                        success = true;
                    }
                    if (!success && rigSlotSizes != null)
                    {
                        for (int j = 0; j < rigSlotSizes.Length; ++j)
                        {
                            if (newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)specialItemSize)
                            {
                                newAISpawn.inventory.rigContents[j] = specialItemID;
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            Mod.instance.LogInfo("\tDone");
            return newAISpawn;
        }

        private object[] GetItemData(string ID)
        {
            GameObject itemPrefab = null;
            bool custom = false;
            if (int.TryParse(ID, out int parseResult))
            {
                itemPrefab = Mod.itemPrefabs[parseResult];
                custom = true;
            }
            else
            {
                itemPrefab = IM.OD[ID].GetGameObject();
            }
            List<string> itemParents = null;
            int itemVolume = 0;
            float itemSpawnChance = 0;
            FVRPhysicalObject itemPhysObj = null;
            if (custom)
            {
                EFM_CustomItemWrapper itemCIW = itemPrefab.GetComponentInChildren<EFM_CustomItemWrapper>();
                itemParents = itemCIW.parents;
                itemVolume = itemCIW.volumes[0];
                itemSpawnChance = itemCIW.spawnChance;
            }
            else
            {
                EFM_VanillaItemDescriptor itemVID = itemPrefab.GetComponentInChildren<EFM_VanillaItemDescriptor>();
                itemParents = itemVID.parents;
                itemVolume = Mod.itemVolumes[ID];
                itemSpawnChance = itemVID.spawnChance;
            }
            itemPhysObj = itemPrefab.GetComponentInChildren<FVRPhysicalObject>();

            return new object[] { ((int)itemPhysObj.Size).ToString(), itemVolume.ToString(), itemSpawnChance.ToString() };
        }

        private void BuildModTree(ref AIInventoryWeaponMod root, BotData botData, JObject inventoryDataToUse, string ID, ref string ammoContainerID, ref string cartridgeID)
        {
            if (inventoryDataToUse["mods"][ID] != null)
            {
                Dictionary<string, List<string>> mods = inventoryDataToUse["mods"][ID].ToObject<Dictionary<string, List<string>>>();

                foreach (KeyValuePair<string, List<string>> modEntry in mods)
                {
                    // Barrels arent implemented but the muzzles attached to them might
                    // So need to check if can get a muzzle mod instead
                    KeyValuePair<string, List<string>> modEntryToUse = modEntry;
                    if (modEntry.Key.Equals("mod_barrel"))
                    {
                        // Take a random barrel from the list
                        string modID = modEntry.Value[UnityEngine.Random.Range(0, modEntry.Value.Count)];
                        if (inventoryDataToUse["mods"][modID] != null)
                        {
                            // If it has mods and one of those is muzzle, take this muzzle entry instead of the barrel one
                            Dictionary<string, List<string>> barrelMods = inventoryDataToUse["mods"][modID].ToObject<Dictionary<string, List<string>>>();
                            if (barrelMods.ContainsKey("mod_muzzle"))
                            {
                                modEntryToUse = new KeyValuePair<string, List<string>>("mod_muzzle", barrelMods["mod_muzzle"]);
                            }
                        }
                    }

                    // Add this mod to root if necessary
                    if (botData.chances["mods"][modEntryToUse.Key] == null || UnityEngine.Random.value <= ((float)botData.chances["mods"][modEntryToUse.Key]) / 100)
                    {
                        string modID = modEntryToUse.Value[UnityEngine.Random.Range(0, modEntryToUse.Value.Count)];
                        if (Mod.itemMap.ContainsKey(modID))
                        {
                            string actualModID = Mod.itemMap[modID];
                            if (modEntryToUse.Key.Equals("mod_magazine"))
                            {
                                ammoContainerID = actualModID;
                            }
                            else if (modEntryToUse.Key.Equals("cartridges"))
                            {
                                cartridgeID = actualModID;
                            }

                            AIInventoryWeaponMod newChild = new AIInventoryWeaponMod(root, actualModID, modEntryToUse.Key);
                            root.children.Add(newChild);
                            BuildModTree(ref newChild, botData, inventoryDataToUse, modID, ref ammoContainerID, ref cartridgeID);
                        }
                    }
                }
            }
        }

        private BotData GetBotData(string name)
        {
            BotData botData = new BotData();

            // Get inventory data
            string[] botInventoryFiles = Directory.GetFiles("BepInEx/Plugins/EscapeFromMeatov/bots/"+ name + "/inventory/");
            botData.minInventoryLevels = new int[botInventoryFiles.Length];
            botData.inventoryDB = new JObject[botInventoryFiles.Length];
            for (int i = botInventoryFiles.Length - 1; i >= 0; --i)
            {
                string[] pathSplit = botInventoryFiles[i].Split('/', '\\');
                string[] split = pathSplit[pathSplit.Length - 1].Split('_');
                botData.minInventoryLevels[i] = int.Parse(split[0]);
                botData.inventoryDB[i] = JObject.Parse(File.ReadAllText(botInventoryFiles[i]));
            }

            // Get other data
            botData.chances = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/bots/" + name + "/chances.json"));
            botData.experience = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/bots/" + name + "/experience.json"));
            botData.generation = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/bots/" + name + "/generation.json"));
            botData.health = JObject.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/bots/" + name + "/health.json"));
            botData.names = JArray.Parse(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/bots/" + name + "/names.json"));

            return botData;
        }

        private float ExpDistrRandOnAvg(float average)
        {
            return -average * Mathf.Log(UnityEngine.Random.value);
        }

        private void UpdateEffects()
        {
            // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
            for (int i = EFM_Effect.effects.Count; i >= 0; --i)
            {
                if (EFM_Effect.effects.Count == 0)
                {
                    break;
                }
                else if (i >= EFM_Effect.effects.Count)
                {
                    continue;
                }

                EFM_Effect effect = EFM_Effect.effects[i];
                if (effect.active)
                {
                    if (effect.hasTimer || effect.hideoutOnly)
                    {
                        effect.timer -= Time.deltaTime;
                        if (effect.timer <= 0)
                        {
                            effect.active = false;

                            // Unapply effect
                            switch (effect.effectType)
                            {
                                case EFM_Effect.EffectType.SkillRate:
                                    Mod.skills[effect.skillIndex].currentProgress -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.EnergyRate:
                                    currentEnergyRate -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.HydrationRate:
                                    currentHydrationRate -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.MaxStamina:
                                    Mod.currentMaxStamina -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.StaminaRate:
                                    Mod.currentStaminaEffect -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.HandsTremor:
                                    // TODO: Stop tremors if there are no other tremor effects
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.QuantumTunnelling:
                                    // TODO: Stop QuantumTunnelling if there are no other tunnelling effects
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.HealthRate:
                                    float[] arrayToUse = effect.nonLethal ? currentNonLethalHealthRates : currentHealthRates;
                                    if (effect.partIndex == -1)
                                    {
                                        for (int j = 0; j < 7; ++j)
                                        {
                                            arrayToUse[j] -= effect.value / 7;
                                        }
                                    }
                                    else
                                    {
                                        arrayToUse[effect.partIndex] -= effect.value;
                                    }
                                    break;
                                case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                    // Reactivate all bleeding 
                                    // Not necessary because when we disabled them we used the disable timer
                                    break;
                                case EFM_Effect.EffectType.Contusion:
                                    bool otherContusions = false;
                                    foreach(EFM_Effect contusionEffectCheck in EFM_Effect.effects)
                                    {
                                        if(contusionEffectCheck.active && contusionEffectCheck.effectType == EFM_Effect.EffectType.Contusion)
                                        {
                                            otherContusions = true;
                                            break;
                                        }
                                    }
                                    if (!otherContusions)
                                    {
                                        // Enable haptic feedback
                                        GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Enabled;
                                        // TODO: also set volume to full
                                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
                                        {
                                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
                                        }
                                    }
                                    break;
                                case EFM_Effect.EffectType.WeightLimit:
                                    Mod.currentWeightLimit -= (int)(effect.value * 1000);
                                    break;
                                case EFM_Effect.EffectType.DamageModifier:
                                    Mod.currentDamageModifier -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.Pain:
                                    // Remove all tremors caused by this pain and disable tremors if no other tremors active
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        EFM_Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasPainTremors = false;
                                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                                    {
                                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                                        {
                                            hasPainTremors = true;
                                            break;
                                        }
                                    }
                                    if (!hasPainTremors)
                                    {
                                        // TODO: Disable tremors
                                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                        {
                                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                        }
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.StomachBloodloss:
                                    --Mod.stomachBloodLossCount;
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.UnknownToxin:
                                    // Remove all effects caused by this toxin
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                                        {
                                            for (int j = 0; j < 7; ++j)
                                            {
                                                currentHealthRates[j] -= causedEffect.value / 7;
                                            }
                                        }
                                        // Could go two layers deep
                                        foreach (EFM_Effect causedCausedEffect in effect.caused)
                                        {
                                            EFM_Effect.effects.Remove(causedCausedEffect);
                                        }
                                        EFM_Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasToxinTremors = false;
                                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                                    {
                                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                                        {
                                            hasToxinTremors = true;
                                            break;
                                        }
                                    }
                                    if (!hasToxinTremors)
                                    {
                                        // TODO: Disable tremors
                                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                        {
                                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                        }
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.BodyTemperature:
                                    Mod.temperatureOffset -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.Antidote:
                                    // Will remove toxin on activation, does nothing after
                                    break;
                                case EFM_Effect.EffectType.LightBleeding:
                                case EFM_Effect.EffectType.HeavyBleeding:
                                    // Remove all effects caused by this bleeding
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                                        {
                                            currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                        }
                                        else // Energy rate
                                        {
                                            currentEnergyRate -= causedEffect.value;
                                        }
                                        EFM_Effect.effects.Remove(causedEffect);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }
                                    break;
                                case EFM_Effect.EffectType.Fracture:
                                    // Remove all effects caused by this fracture
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        // Could go two layers deep
                                        foreach (EFM_Effect causedCausedEffect in effect.caused)
                                        {
                                            EFM_Effect.effects.Remove(causedCausedEffect);
                                        }
                                        EFM_Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasFractureTremors = false;
                                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                                    {
                                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                                        {
                                            hasFractureTremors = true;
                                            break;
                                        }
                                    }
                                    if (!hasFractureTremors)
                                    {
                                        // TODO: Disable tremors
                                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                        {
                                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                        }
                                    }

                                    if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                                    }
                                    break;
                            }

                            EFM_Effect.effects.RemoveAt(i);

                            continue;
                        }
                    }
                }
                else
                {
                    bool effectJustActivated = false;
                    if (effect.delay > 0)
                    {
                        effect.delay -= Time.deltaTime;
                    }
                    else if (effect.inactiveTimer <= 0)
                    {
                        effect.active = true;
                        effectJustActivated = true;
                    }
                    if (effect.inactiveTimer > 0)
                    {
                        effect.inactiveTimer -= Time.deltaTime;
                    }
                    else if (effect.delay <= 0)
                    {
                        effect.active = true;
                        effectJustActivated = true;
                    }

                    // Apply effect if it just started being active
                    if (effectJustActivated)
                    {
                        switch (effect.effectType)
                        {
                            case EFM_Effect.EffectType.SkillRate:
                                Mod.skills[effect.skillIndex].currentProgress += effect.value;
                                break;
                            case EFM_Effect.EffectType.EnergyRate:
                                currentEnergyRate += effect.value;
                                break;
                            case EFM_Effect.EffectType.HydrationRate:
                                currentHydrationRate += effect.value;
                                break;
                            case EFM_Effect.EffectType.MaxStamina:
                                Mod.currentMaxStamina += effect.value;
                                break;
                            case EFM_Effect.EffectType.StaminaRate:
                                Mod.currentStaminaEffect += effect.value;
                                break;
                            case EFM_Effect.EffectType.HandsTremor:
                                // TODO: Begin tremors if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.QuantumTunnelling:
                                // TODO: Begin quantumtunneling if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.HealthRate:
                                float[] arrayToUse = effect.nonLethal ? currentNonLethalHealthRates : currentHealthRates;
                                if (effect.partIndex == -1)
                                {
                                    for (int j = 0; j < 7; ++j)
                                    {
                                        arrayToUse[j] += effect.value / 7;
                                    }
                                }
                                else
                                {
                                    arrayToUse[effect.partIndex] += effect.value;
                                }
                                break;
                            case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                // Deactivate all bleeding using disable timer
                                foreach (EFM_Effect bleedEffect in EFM_Effect.effects)
                                {
                                    if (bleedEffect.effectType == EFM_Effect.EffectType.LightBleeding || bleedEffect.effectType == EFM_Effect.EffectType.HeavyBleeding)
                                    {
                                        bleedEffect.active = false;
                                        bleedEffect.inactiveTimer = effect.timer;

                                        // Unapply the healthrate caused by this bleed
                                        EFM_Effect causedHealthRate = bleedEffect.caused[0];
                                        if (causedHealthRate.nonLethal)
                                        {
                                            currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        else
                                        {
                                            currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        EFM_Effect causedEnergyRate = bleedEffect.caused[1];
                                        currentEnergyRate -= causedEnergyRate.value;
                                        bleedEffect.caused.Clear();
                                        EFM_Effect.effects.Remove(causedHealthRate);
                                        EFM_Effect.effects.Remove(causedEnergyRate);
                                    }
                                }
                                break;
                            case EFM_Effect.EffectType.Contusion:
                                // Disable haptic feedback
                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
                                // TODO: also set volume to 0.33 * volume
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.WeightLimit:
                                Mod.currentWeightLimit += (int)(effect.value * 1000);
                                break;
                            case EFM_Effect.EffectType.DamageModifier:
                                Mod.currentDamageModifier += effect.value;
                                break;
                            case EFM_Effect.EffectType.Pain:
                                // Add a tremor effect
                                EFM_Effect newTremor = new EFM_Effect();
                                newTremor.effectType = EFM_Effect.EffectType.HandsTremor;
                                newTremor.delay = 5;
                                newTremor.hasTimer = effect.hasTimer;
                                newTremor.timer = effect.timer;
                                EFM_Effect.effects.Add(newTremor);
                                effect.caused.Add(newTremor);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.StomachBloodloss:
                                ++Mod.stomachBloodLossCount;
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.UnknownToxin:
                                // Add a pain effect
                                EFM_Effect newToxinPain = new EFM_Effect();
                                newToxinPain.effectType = EFM_Effect.EffectType.Pain;
                                newToxinPain.delay = 5;
                                newToxinPain.hasTimer = effect.hasTimer;
                                newToxinPain.timer = effect.timer;
                                newToxinPain.partIndex = 0;
                                EFM_Effect.effects.Add(newToxinPain);
                                effect.caused.Add(newToxinPain);
                                // Add a health rate effect
                                EFM_Effect newToxinHealthRate = new EFM_Effect();
                                newToxinHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newToxinHealthRate.delay = 5;
                                newToxinHealthRate.value = -25;
                                newToxinHealthRate.hasTimer = effect.hasTimer;
                                newToxinHealthRate.timer = effect.timer;
                                EFM_Effect.effects.Add(newToxinHealthRate);
                                effect.caused.Add(newToxinHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.BodyTemperature:
                                Mod.temperatureOffset += effect.value;
                                break;
                            case EFM_Effect.EffectType.Antidote:
                                // Will remove toxin on ativation, does nothing after
                                for (int j = EFM_Effect.effects.Count; j >= 0; --j)
                                {
                                    if (EFM_Effect.effects[j].effectType == EFM_Effect.EffectType.UnknownToxin)
                                    {
                                        EFM_Effect.effects.RemoveAt(j);
                                        break;
                                    }
                                }
                                break;
                            case EFM_Effect.EffectType.LightBleeding:
                                // Add a health rate effect
                                EFM_Effect newLightBleedingHealthRate = new EFM_Effect();
                                newLightBleedingHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newLightBleedingHealthRate.delay = 5;
                                newLightBleedingHealthRate.value = -8;
                                newLightBleedingHealthRate.hasTimer = effect.hasTimer;
                                newLightBleedingHealthRate.timer = effect.timer;
                                newLightBleedingHealthRate.partIndex = effect.partIndex;
                                newLightBleedingHealthRate.nonLethal = true;
                                EFM_Effect.effects.Add(newLightBleedingHealthRate);
                                effect.caused.Add(newLightBleedingHealthRate);
                                // Add a energy rate effect
                                EFM_Effect newLightBleedingEnergyRate = new EFM_Effect();
                                newLightBleedingEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newLightBleedingEnergyRate.delay = 5;
                                newLightBleedingEnergyRate.value = -5;
                                newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newLightBleedingEnergyRate.timer = effect.timer;
                                newLightBleedingEnergyRate.partIndex = effect.partIndex;
                                EFM_Effect.effects.Add(newLightBleedingEnergyRate);
                                effect.caused.Add(newLightBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.HeavyBleeding:
                                // Add a health rate effect
                                EFM_Effect newHeavyBleedingHealthRate = new EFM_Effect();
                                newHeavyBleedingHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyBleedingHealthRate.delay = 5;
                                newHeavyBleedingHealthRate.value = -13.5f;
                                newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingHealthRate.timer = effect.timer;
                                newHeavyBleedingHealthRate.nonLethal = true;
                                EFM_Effect.effects.Add(newHeavyBleedingHealthRate);
                                effect.caused.Add(newHeavyBleedingHealthRate);
                                // Add a energy rate effect
                                EFM_Effect newHeavyBleedingEnergyRate = new EFM_Effect();
                                newHeavyBleedingEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newHeavyBleedingEnergyRate.delay = 5;
                                newHeavyBleedingEnergyRate.value = -6;
                                newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingEnergyRate.timer = effect.timer;
                                newHeavyBleedingEnergyRate.partIndex = effect.partIndex;
                                EFM_Effect.effects.Add(newHeavyBleedingEnergyRate);
                                effect.caused.Add(newHeavyBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.Fracture:
                                // Add a pain effect
                                EFM_Effect newFracturePain = new EFM_Effect();
                                newFracturePain.effectType = EFM_Effect.EffectType.Pain;
                                newFracturePain.delay = 5;
                                newFracturePain.hasTimer = effect.hasTimer;
                                newFracturePain.timer = effect.timer;
                                EFM_Effect.effects.Add(newFracturePain);
                                effect.caused.Add(newFracturePain);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.Dehydration:
                                // Add a HealthRate effect
                                EFM_Effect newDehydrationHealthRate = new EFM_Effect();
                                newDehydrationHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newDehydrationHealthRate.value = -60;
                                newDehydrationHealthRate.delay = 5;
                                newDehydrationHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newDehydrationHealthRate);
                                effect.caused.Add(newDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.HeavyDehydration:
                                // Add a HealthRate effect
                                EFM_Effect newHeavyDehydrationHealthRate = new EFM_Effect();
                                newHeavyDehydrationHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyDehydrationHealthRate.value = -350;
                                newHeavyDehydrationHealthRate.delay = 5;
                                newHeavyDehydrationHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newHeavyDehydrationHealthRate);
                                effect.caused.Add(newHeavyDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.Fatigue:
                                Mod.fatigue = true;

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.HeavyFatigue:
                                // Add a HealthRate effect
                                EFM_Effect newHeavyFatigueHealthRate = new EFM_Effect();
                                newHeavyFatigueHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyFatigueHealthRate.value = -30;
                                newHeavyFatigueHealthRate.delay = 5;
                                newHeavyFatigueHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newHeavyFatigueHealthRate);
                                effect.caused.Add(newHeavyFatigueHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case EFM_Effect.EffectType.OverweightFatigue:
                                // Add a EnergyRate effect
                                EFM_Effect newOverweightFatigueEnergyRate = new EFM_Effect();
                                newOverweightFatigueEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newOverweightFatigueEnergyRate.value = -4;
                                newOverweightFatigueEnergyRate.delay = 5;
                                newOverweightFatigueEnergyRate.hasTimer = false;
                                EFM_Effect.effects.Add(newOverweightFatigueEnergyRate);
                                effect.caused.Add(newOverweightFatigueEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
                                }
                                break;
                        }
                    }
                }
            }


            float healthDelta = 0;
            float health = 0;
            bool lethal = false;

            // Apply lethal health rates
            for (int i = 0; i < 7; ++i)
            {
                if (Mod.health[i] == 0 && !(i == 0 || i == 1)) 
                {
                    // Apply currentHealthRates[i] to other parts
                    for (int j = 0; j < 7; ++j)
                    {
                        if (j != i)
                        {
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + currentHealthRates[i] * (Time.deltaTime / 60) / 6, 1, maxHealth[j]);

                            if (Mod.health[j] == 0 && (j == 0 || j == 1))
                            {
                                // TODO: Kill player
                            }
                        }
                    }
                }
                else if(currentHealthRates[i] < 0 && Mod.health[i] == 0)
                {
                    // TODO: Kill player
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthRates[i] * (Time.deltaTime / 60), 1, maxHealth[i]);
                }

                if (!lethal)
                {
                    lethal = currentHealthRates[i] > 0;
                }
            }

            // Apply nonlethal health rates
            for (int i = 0; i < 7; ++i)
            {
                if(Mod.health[i] == 0)
                {
                    // Apply currentNonLethalHealthRates[i] to other parts
                    for (int j = 0; j < 7; ++j)
                    {
                        if (j != i)
                        {
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + currentNonLethalHealthRates[i] * (Time.deltaTime / 60) / 6, 1, maxHealth[j]);
                        }
                    }
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentNonLethalHealthRates[i] * (Time.deltaTime / 60), 1, maxHealth[i]);
                }
                Mod.playerStatusManager.partHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", maxHealth[i]);
                Mod.playerStatusManager.partHealthImages[i].color = Color.Lerp(Color.red, Color.white, Mod.health[i] / maxHealth[i]);

                healthDelta += currentNonLethalHealthRates[i];
                health += Mod.health[i];
            }

            // Set status elements
            if (healthDelta != 0)
            {
                if (!Mod.playerStatusManager.healthDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.healthDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.healthDeltaText.text = (healthDelta >= 0 ? "+ " : "") + healthDelta + "/min";
                if (lethal)
                {
                    Mod.playerStatusManager.healthDeltaText.color = Color.red;
                }
                else
                {
                    Mod.playerStatusManager.healthDeltaText.color = Color.white;
                }
            }
            else if (Mod.playerStatusManager.healthDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.healthDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.healthText.text = String.Format("{0:0}/440", health);

            Mod.hydration = Mathf.Clamp(Mod.hydration + currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);
            if (currentHydrationRate != 0)
            {
                if (!Mod.playerStatusManager.hydrationDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.hydrationDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.hydrationDeltaText.text = (currentHydrationRate >= 0 ? "+ " : "") + String.Format("{0:0.#}/min", currentHydrationRate);
            }
            else if (Mod.playerStatusManager.hydrationDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.hydrationDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.hydrationText.text = String.Format("{0:0}/{1:0}", Mod.hydration, Mod.maxHydration);
            if (Mod.hydration == 0)
            {
                if (Mod.dehydrationEffect == null)
                {
                    // Add a heavyDehydration effect
                    EFM_Effect newHeavyDehydration = new EFM_Effect();
                    newHeavyDehydration.effectType = EFM_Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.delay = 5;
                    newHeavyDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == EFM_Effect.EffectType.Dehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if(Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyDehydration effect
                    EFM_Effect newHeavyDehydration = new EFM_Effect();
                    newHeavyDehydration.effectType = EFM_Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
            }
            else if(Mod.hydration < 20)
            {
                if (Mod.dehydrationEffect == null)
                {
                    // Add a dehydration effect
                    EFM_Effect newDehydration = new EFM_Effect();
                    newDehydration.effectType = EFM_Effect.EffectType.Dehydration;
                    newDehydration.delay = 5;
                    newDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newDehydration);
                    Mod.dehydrationEffect = newDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == EFM_Effect.EffectType.HeavyDehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a dehydration effect
                    EFM_Effect newDehydration = new EFM_Effect();
                    newDehydration.effectType = EFM_Effect.EffectType.Dehydration;
                    newDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newDehydration);
                    Mod.dehydrationEffect = newDehydration;
                }
            }
            else // Hydrated
            {
                // Remove any dehydration effect
                if(Mod.dehydrationEffect != null)
                {
                    // Disable 
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);
                }
            }

            Mod.energy = Mathf.Clamp(Mod.energy + currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);

            if (currentEnergyRate != 0)
            {
                if (!Mod.playerStatusManager.energyDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.energyDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.energyDeltaText.text = (currentEnergyRate >= 0 ? "+ " : "") + String.Format("{0:0.#}/min", currentEnergyRate);
            }
            else if (Mod.playerStatusManager.energyDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.energyDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.energyText.text = String.Format("{0:0}/{1:0}", Mod.energy, Mod.maxEnergy);
            if (Mod.energy == 0)
            {
                if (Mod.fatigueEffect == null)
                {
                    // Add a heavyFatigue effect
                    EFM_Effect newHeavyFatigue = new EFM_Effect();
                    newHeavyFatigue.effectType = EFM_Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.delay = 5;
                    newHeavyFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyFatigue);
                    Mod.fatigueEffect = newHeavyFatigue;
                }
                else if (Mod.fatigueEffect.effectType == EFM_Effect.EffectType.Fatigue)
                {
                    // Disable the other fatigue before adding a new one
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyFatigue effect
                    EFM_Effect newHeavyFatigue = new EFM_Effect();
                    newHeavyFatigue.effectType = EFM_Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyFatigue);
                    Mod.dehydrationEffect = newHeavyFatigue;
                }
            }
            else if (Mod.energy < 20)
            {
                if (Mod.fatigueEffect == null)
                {
                    // Add a fatigue effect
                    EFM_Effect newFatigue = new EFM_Effect();
                    newFatigue.effectType = EFM_Effect.EffectType.Fatigue;
                    newFatigue.delay = 5;
                    newFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newFatigue);
                    Mod.fatigueEffect = newFatigue;
                }
                else if (Mod.fatigueEffect.effectType == EFM_Effect.EffectType.HeavyFatigue)
                {
                    // Disable the other fatigue before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.fatigueEffect);

                    // Add a fatigue effect
                    EFM_Effect newFatigue = new EFM_Effect();
                    newFatigue.effectType = EFM_Effect.EffectType.Fatigue;
                    newFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newFatigue);
                    Mod.dehydrationEffect = newFatigue;
                }
            }
            else // Energized
            {
                // Remove any fatigue effect
                if (Mod.fatigueEffect != null)
                {
                    // Disable 
                    if (Mod.fatigueEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.fatigueEffect);
                }
            }
        }

        private void FixedUpdate()
        {
            //if (disabledLimits)
            //{
            //    foreach (GameObject go in disabledDoors)
            //    {
            //        go.SetActive(true);
            //    }
            //}
            //if (!disabledLimits && doneJointLimitFrame)
            //{
            //    if (hingeJointsToDisableLimits != null && hingeJointsToDisableLimits.Count > 0)
            //    {
            //        for(int i =0; i<hingeJointsToDisableLimits.Count; ++i)
            //        {
            //            hingeJointsToDisableLimits[i].limits = correspondingLimits[i];
            //        }
            //    }
            //    disabledLimits = true;
            //}
            //doneJointLimitFrame = true;
        }

        public void UpdateSun()
        {
            // Considering that 21600 (0600) is sunrise and that 64800 (1800) is sunset
            UpdateSunAngle();

            UpdateSunIntensity();
        }

        private void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * EFM_Manager.meatovTimeMultiplier) % 86400);
            time = (scaledTime + Mod.chosenTimeIndex == 0 ? 0 : 43200) % 86400;
        }

        private void InitSun()
        {
            Destroy(transform.GetChild(1).GetChild(0).GetComponent<AlloyAreaLight>());
            Destroy(transform.GetChild(1).GetChild(0).GetComponent<Light>());
            //// Get sun
            //sun = transform.GetChild(1).GetChild(0).GetComponent<Light>();

            //// Check if should be active and set intensity
            //if (time >= 23400 && time <= 63000) // Day
            //{
            //    sun.intensity = 1;
            //}
            //else if (time > 63000 && time < 64800) // Setting
            //{
            //    sun.intensity = (64800 - time) / 1800;
            //}
            //else if ((time > 64800 && time <= 86400) || (time >= 0 && time <= 21600)) // Night
            //{
            //    return; // Intensity should be 0
            //}
            //else //(time > 21600 && time < 23400) // Rising
            //{
            //    sun.intensity = 1800 / (time - 21600);
            //}
            //sun.gameObject.SetActive(true);

            //// Set angle
            //float angle = 0.004166f * time + 21600;
            //sun.transform.rotation = Quaternion.Euler(angle, 45, 45);
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * EFM_Manager.meatovTimeMultiplier;

            time %= 86400;
        }

        public void UpdateSunAngle()
        {
            // Sun's X axis must rotate by 180 degrees in 43200 seconds
            // so 0.004166 degree in 1 second with an offset of 21600
            float angle = 0.004166f * time + 21600;
            sun.transform.rotation = Quaternion.Euler(angle, 45, 45);
        }

        public void UpdateSunIntensity()
        {
            if(time >= 23400 && time <= 63000) // Day
            {
                sun.intensity = 1;
            }
            else if(time > 63000 && time < 64800) // Setting
            {
                sun.intensity = (64800 - time) / 1800;
            }
            else if((time > 64800 && time <= 86400) || (time >= 0 && time <= 21600)) // Night
            {
                sun.intensity = 0;
            }
            else //(time > 21600 && time < 23400) // Rising
            {
                sun.intensity = 1800 / (time - 21600);
            }
        }

        public void InitEffects()
        {
            foreach(EFM_Effect effect in EFM_Effect.effects)
            {
                if (effect.active)
                {
                    switch (effect.effectType)
                    {
                        case EFM_Effect.EffectType.EnergyRate:
                            currentEnergyRate += effect.value;
                            break;
                        case EFM_Effect.EffectType.HydrationRate:
                            currentHydrationRate += effect.value;
                            break;
                        case EFM_Effect.EffectType.HealthRate:
                            for (int j = 0; j < 7; ++j)
                            {
                                currentHealthRates[j] += effect.value / 7;
                            }
                            break;
                    }
                }
            }
        }

        private int GetLocationDataIndex(int chosenMapIndex)
        {
            switch (chosenMapIndex)
            {
                case 0: // Factory
                    return 1;
                default:
                    return 1;
            }
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        private GameObject SpawnLootItem(GameObject itemPrefab, Transform itemsRoot, string itemID, JToken itemData,
                                         Dictionary<string, EFM_CustomItemWrapper> spawnedItemCIWs, Dictionary<string, EFM_VanillaItemDescriptor> spawnedItemVIDs,
                                         List<string> unspawnedParents, JToken spawnData, string originalID, bool useChance)
        {
            Mod.instance.LogInfo("Spawn loot item called with ID: " + itemID);
            GameObject itemObject = null;
            if (itemPrefab == null)
            {
                return null;
            }

            // Instantiate in a random slot that fits the item if there is one
            EFM_CustomItemWrapper prefabCIW = itemPrefab.GetComponent<EFM_CustomItemWrapper>();
            EFM_VanillaItemDescriptor prefabVID = itemPrefab.GetComponent<EFM_VanillaItemDescriptor>();

            FVRPhysicalObject itemPhysObj = null;
            EFM_CustomItemWrapper itemCIW = null;
            EFM_VanillaItemDescriptor itemVID = null;
            FireArmRoundType roundType = FireArmRoundType.a106_25mmR;
            FireArmRoundClass roundClass = FireArmRoundClass.a20AP;
            int amount = 0;
            if (prefabCIW != null)
            {
                itemObject = GameObject.Instantiate(itemPrefab);
                itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(null, itemCIW, null);

                // Get amount
                amount = (itemData["upd"] != null && itemData["upd"]["StackObjectsCount"] != null) ? (int)itemData["upd"]["StackObjectsCount"] : 1;
                if (itemCIW.itemType == Mod.ItemType.Money)
                {
                    itemCIW.stack = amount;
                }
                else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
                {
                    // TODO: Ammo is specified as separate item with the ammobox as its parent, so the ammobox will be filled up separately? Need to confirm
                    /*
                    FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                    for (int j = 0; j < itemCIW.maxAmount; ++j)
                    {
                        asMagazine.AddRound(itemCIW.roundClass, false, false);
                    }
                    */
                }
                else if (itemCIW.maxAmount > 0)
                {
                    itemCIW.amount = itemCIW.maxAmount;
                }
            }
            else
            {
                if (Mod.usedRoundIDs.Contains(prefabVID.H3ID))
                {
                    Mod.instance.LogInfo("\tSpawning round with ID: " + prefabVID.H3ID);
                    // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of rounds
                    amount = (itemData["upd"] != null && itemData["upd"]["StackObjectsCount"] != null) ? (int)itemData["upd"]["StackObjectsCount"] : 1;
                    FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                    roundType = round.RoundType;
                    roundClass = round.RoundClass;
                    if (amount > 1)
                    {
                        Mod.instance.LogInfo("\t\tStack > 1");
                        if (Mod.ammoBoxByAmmoID.ContainsKey(prefabVID.H3ID))
                        {
                            Mod.instance.LogInfo("\t\t\tSpecific box");
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[prefabVID.H3ID]]);
                            itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                            itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            for (int j = 0; j < amount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }

                            // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                            Mod.RemoveFromAll(null, itemCIW, null);
                        }
                        else // Spawn in generic box
                        {
                            Mod.instance.LogInfo("\t\t\tGeneric box");
                            if (amount > 30)
                            {
                                itemObject = GameObject.Instantiate(Mod.itemPrefabs[716]);
                            }
                            else
                            {
                                itemObject = GameObject.Instantiate(Mod.itemPrefabs[715]);
                            }

                            itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                            itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>(); 

                            FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                            asMagazine.RoundType = roundType;
                            itemCIW.roundClass = roundClass;
                            for (int j = 0; j < amount; ++j)
                            {
                                asMagazine.AddRound(itemCIW.roundClass, false, false);
                            }

                            // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                            Mod.RemoveFromAll(null, itemCIW, null);
                        }
                    }
                    else // Single round, spawn as normal
                    {
                        Mod.instance.LogInfo("\t\tSingle round");
                        itemObject = GameObject.Instantiate(itemPrefab);
                        itemVID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
                        itemPhysObj = itemVID.GetComponent<FVRPhysicalObject>();
                    }
                }
                else // Not a round, spawn as normal
                {
                    itemObject = GameObject.Instantiate(itemPrefab);
                    itemVID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
                    itemPhysObj = itemVID.GetComponent<FVRPhysicalObject>();

                    if(itemPhysObj is FVRFireArm)
                    {
                        // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                        Mod.RemoveFromAll(itemPhysObj, null, itemVID);
                    }
                }
            }

            // Position and rotate item
            if (itemData["parentId"] != null)
            {
                Mod.instance.LogInfo("\t\tHas parent ID");
                string parentID = itemData["parentId"].ToString();
                if (unspawnedParents.Contains(parentID))
                {
                    Destroy(itemObject);
                    unspawnedParents.Add(itemData["_id"].ToString());
                    return null;
                }

                // Item has a parent which should be a previously spawned item
                // This parent should be a container of some sort so we need to add this item to the container
                if (spawnedItemCIWs.ContainsKey(parentID))
                {
                    Mod.instance.LogInfo("\t\tParent exists");
                    EFM_CustomItemWrapper parent = spawnedItemCIWs[parentID];

                    // Check which type of item the parent is, because how we instantiate it depends on that
                    if(parent.itemType == Mod.ItemType.Rig || parent.itemType == Mod.ItemType.ArmoredRig)
                    {
                        // Set item in a random slot that fits it if there is one

                        List<int> fittingSlotIndices = new List<int>();
                        for (int i = 0; i < parent.rigSlots.Count; ++i)
                        {
                            if ((int)parent.rigSlots[i].SizeLimit >= (int)itemPhysObj.Size)
                            {
                                fittingSlotIndices.Add(i);
                            }
                        }

                        if(fittingSlotIndices.Count > 0)
                        {
                            int randomIndex = fittingSlotIndices[UnityEngine.Random.Range(0, fittingSlotIndices.Count - 1)];

                            parent.itemsInSlots[randomIndex] = itemObject;
                        }
                        else // No fitting slots, just spawn next to parent
                        {
                            itemObject.transform.position = parent.transform.position + Vector3.up; // 1m above parent
                        }
                    }
                    else if(parent.itemType == Mod.ItemType.Backpack || parent.itemType == Mod.ItemType.Container || parent.itemType == Mod.ItemType.Pouch)
                    {
                        // Set item in the container, at random pos and rot, if volume fits
                        bool boxMainContainer = parent.mainContainer.GetComponent<BoxCollider>() != null;
                        if (parent.AddItemToContainer(itemObject.GetComponent<EFM_CustomItemWrapper>().physObj))
                        {
                            itemObject.transform.parent = parent.containerItemRoot;

                            if (boxMainContainer)
                            {
                                BoxCollider boxCollider = parent.mainContainer.GetComponent<BoxCollider>();
                                itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
                                                                                 UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
                                                                                 UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
                            }
                            else
                            {
                                CapsuleCollider capsuleCollider = parent.mainContainer.GetComponent<CapsuleCollider>();
                                if (capsuleCollider != null)
                                {
                                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-capsuleCollider.radius / 2, capsuleCollider.radius / 2),
                                                                                     UnityEngine.Random.Range(-(capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.height / 2 - capsuleCollider.radius),
                                                                                     0);
                                }
                                else
                                {
                                    itemObject.transform.localPosition = Vector3.zero;
                                }
                            }
                            itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
                        }
                        else // Could not add item to container, set it next to parent
                        {
                            itemObject.transform.position = parent.transform.position + Vector3.up; // 1m above parent
                        }
                    }
                    else if(parent.itemType == Mod.ItemType.AmmoBox)
                    {
                        Mod.instance.LogInfo("\t\tParent is ammo box, itemObject null?: "+(itemObject == null));
                        // Destroy itemObject, Set the ammo box's magazine script's ammo to the one specified and of specified count
                        Destroy(itemObject);
                        itemCIW = null;
                        itemVID = null;
                        itemObject = null;

                        FVRFireArmMagazine parentAsMagazine = parent.GetComponent<FVRFireArmMagazine>();

                        for (int i = 0; i < amount; ++i)
                        {
                            parentAsMagazine.AddRound(roundClass, false, false);
                        }
                    }
                }
                else
                {
                    Mod.instance.LogError("Attempted to spawn loose loot " + itemID + " with original ID " + originalID + " but parentID " + itemData["parentId"].ToString() + " does not exist in spawned items list");
                }
            }
            else
            {
                if (useChance && UnityEngine.Random.value > (prefabCIW != null ? prefabCIW.spawnChance : prefabVID.spawnChance) / 100)
                {
                    Destroy(itemObject);
                    unspawnedParents.Add(itemData["_id"].ToString());
                    return null;
                }

                if (spawnData["Position"].Type == JTokenType.Array)
                {
                    Vector3 position = new Vector3((float)spawnData["Position"][0], (float)spawnData["Position"][1], (float)spawnData["Position"][2]);
                    itemObject.transform.position = position;
                }
                if (spawnData["Rotation"].Type == JTokenType.Array)
                {
                    Vector3 rotation = new Vector3((float)spawnData["Rotation"][0], (float)spawnData["Rotation"][1], (float)spawnData["Rotation"][2]);
                    itemObject.transform.rotation = Quaternion.Euler(rotation);
                }
                itemObject.transform.parent = itemsRoot;
            }

            if (itemObject != null)
            {
                Mod.instance.LogInfo("Spawned loose loot: " + itemObject.name);
            }

            // Add item wrapper or descriptor to spawned items dict with _id as key
            if (itemCIW != null)
            {
                spawnedItemCIWs.Add(itemData["_id"].ToString(), itemCIW);
            }
            else if(itemVID != null)
            {
                spawnedItemVIDs.Add(itemData["_id"].ToString(), itemVID);
            }

            return itemObject;
        }

        public override void InitUI()
        {
        }

        public void LoadMapData()
        {
            extractions = new List<Extraction>();
            Transform extractionRoot = transform.GetChild(transform.childCount - 2);

            int extractionIndex = 0;
            foreach(JToken extractionData in Mod.mapData["maps"][Mod.chosenMapIndex]["extractions"])
            {
                Extraction extraction = new Extraction();
                extraction.gameObject = extractionRoot.GetChild(extractionIndex).gameObject;
                extraction.name = extraction.gameObject.name;

                extraction.raidTimes = new List<TimeInterval>();
                foreach(JToken raidTimeData in extractionData["raidTimes"])
                {
                    TimeInterval raidTime = new TimeInterval();
                    raidTime.start = (int)raidTimeData["start"];
                    raidTime.end = (int)raidTimeData["end"];
                    extraction.raidTimes.Add(raidTime);
                }
                extraction.times = new List<TimeInterval>();
                foreach(JToken timeData in extractionData["times"])
                {
                    TimeInterval time = new TimeInterval();
                    time.start = (int)timeData["start"];
                    time.end = (int)timeData["end"];
                    extraction.times.Add(time);
                }
                extraction.itemRequirements = new Dictionary<string, int>();
                foreach (JToken itemRequirement in extractionData["itemRequirements"])
                {
                    extraction.itemRequirements.Add(itemRequirement["ID"].ToString(), (int)itemRequirement["amount"]);
                }
                extraction.equipmentRequirements = new List<string>();
                foreach (JToken equipmentRequirement in extractionData["equipmentRequirements"])
                {
                    extraction.equipmentRequirements.Add(equipmentRequirement.ToString());
                }
                extraction.itemBlacklist = new List<string>();
                foreach (JToken blacklistItem in extractionData["itemBlacklist"])
                {
                    extraction.itemBlacklist.Add(blacklistItem.ToString());
                }
                extraction.equipmentBlacklist = new List<string>();
                foreach (JToken blacklistEquipment in extractionData["equipmentBlacklist"])
                {
                    extraction.equipmentBlacklist.Add(blacklistEquipment.ToString());
                }
                extraction.role = (int)extractionData["role"];
                extraction.accessRequirement = (bool)extractionData["accessRequirement"];

                extractions.Add(extraction);

                ++extractionIndex;
            }
        }

        private float ExtractionChanceByDist(float distance)
        {
            if(distance < 150)
            {
                return 0.001f * distance; // 15% chance at 150 meters
            }
            else if(distance < 500)
            {
                return 0.0008f * distance + 0.15f; // 55% chance at 500 meters
            }
            else // > 500
            {
                return 0.00045f * distance + 0.55f; // 100% at 1000 meters
            }
        }

        public void KillPlayer(EFM_Base_Manager.FinishRaidState raidState = EFM_Base_Manager.FinishRaidState.KIA)
        {
            if (Mod.dead)
            {
                return;
            }
            Mod.dead = true;

            // Register insured items that are currently on player
            if(Mod.insuredItems == null)
            {
                Mod.insuredItems = new List<InsuredSet>();
            }
            InsuredSet insuredSet = new InsuredSet();
            insuredSet.returnTime = GetTimeSeconds() + 86400; // Current time + 24 hours, TODO: Should be dependent on who insured it
            insuredSet.items = new Dictionary<string, int>();
            foreach(KeyValuePair<string, List<GameObject>> itemObjectList in Mod.playerInventoryObjects)
            {
                foreach(GameObject itemObject in itemObjectList.Value)
                {
                    if(itemObject == null)
                    {
                        Mod.instance.LogError("ItemObject in player inventory with ID: "+itemObjectList.Key+" is null while building insured set, removing from list");
                        itemObjectList.Value.Remove(itemObject);
                        break;
                    }
                    EFM_CustomItemWrapper CIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor VID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
                    if (CIW != null && CIW.insured && UnityEngine.Random.value <= 0.5) // TODO: Should have a chance depending on who insured the item
                    {
                        if (insuredSet.items.ContainsKey(CIW.ID))
                        {
                            insuredSet.items[CIW.ID] += 1;
                        }
                        else
                        {
                            insuredSet.items.Add(CIW.ID, 1);
                        }
                    }
                    else if(VID != null && VID.insured && UnityEngine.Random.value <= 0.5)
                    {
                        if (insuredSet.items.ContainsKey(VID.H3ID))
                        {
                            insuredSet.items[VID.H3ID] += 1;
                        }
                        else
                        {
                            insuredSet.items.Add(VID.H3ID, 1);
                        }
                    }
                }
            }
            if(insuredSet.items.Count > 0)
            {
                Mod.insuredItems.Add(insuredSet);
            }

            // Drop items in hand
            if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject))
            {
                GM.CurrentMovementManager.Hands[0].CurrentInteractable.ForceBreakInteraction();
            }
            if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[1].CurrentInteractable is FVRPhysicalObject))
            {
                GM.CurrentMovementManager.Hands[1].CurrentInteractable.ForceBreakInteraction();
            }

            // Unequip and destroy all equipment apart from pouch
            if (EFM_EquipmentSlot.wearingBackpack)
            {
                EFM_CustomItemWrapper backpackCIW = EFM_EquipmentSlot.currentBackpack;
                FVRPhysicalObject backpackPhysObj = backpackCIW.GetComponent<FVRPhysicalObject>();
                backpackPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= backpackCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(backpackCIW);
                backpackCIW.destroyed = true;
                Destroy(backpackCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingBodyArmor)
            {
                EFM_CustomItemWrapper bodyArmorCIW = EFM_EquipmentSlot.currentArmor;
                FVRPhysicalObject bodyArmorPhysObj = bodyArmorCIW.GetComponent<FVRPhysicalObject>();
                bodyArmorPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= bodyArmorCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(bodyArmorCIW);
                bodyArmorCIW.destroyed = true;
                Destroy(bodyArmorCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingEarpiece)
            {
                EFM_CustomItemWrapper earPieceCIW = EFM_EquipmentSlot.currentEarpiece;
                FVRPhysicalObject earPiecePhysObj = earPieceCIW.GetComponent<FVRPhysicalObject>();
                earPiecePhysObj.SetQuickBeltSlot(null);
                Mod.weight -= earPieceCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(earPieceCIW);
                earPieceCIW.destroyed = true;
                Destroy(earPieceCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingHeadwear)
            {
                EFM_CustomItemWrapper headWearCIW = EFM_EquipmentSlot.currentHeadwear;
                FVRPhysicalObject headWearPhysObj = headWearCIW.GetComponent<FVRPhysicalObject>();
                headWearPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= headWearCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(headWearCIW);
                headWearCIW.destroyed = true;
                Destroy(headWearCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingFaceCover)
            {
                EFM_CustomItemWrapper faceCoverCIW = EFM_EquipmentSlot.currentFaceCover;
                FVRPhysicalObject faceCoverPhysObj = faceCoverCIW.GetComponent<FVRPhysicalObject>();
                faceCoverPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= faceCoverCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(faceCoverCIW);
                faceCoverCIW.destroyed = true;
                Destroy(faceCoverCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingEyewear)
            {
                EFM_CustomItemWrapper eyeWearCIW = EFM_EquipmentSlot.currentEyewear;
                FVRPhysicalObject eyeWearPhysObj = eyeWearCIW.GetComponent<FVRPhysicalObject>();
                eyeWearPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= eyeWearCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(eyeWearCIW);
                eyeWearCIW.destroyed = true;
                Destroy(eyeWearCIW.gameObject);
            }
            if (EFM_EquipmentSlot.wearingRig)
            {
                EFM_CustomItemWrapper rigCIW = EFM_EquipmentSlot.currentRig;
                FVRPhysicalObject rigPhysObj = rigCIW.GetComponent<FVRPhysicalObject>();
                rigPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= rigCIW.currentWeight;
                EFM_EquipmentSlot.TakeOffEquipment(rigCIW);
                rigCIW.destroyed = true;
                Destroy(rigCIW.gameObject);
            }

            // Destroy right shoulder object
            if(Mod.rightShoulderObject != null)
            {
                EFM_VanillaItemDescriptor rightShoulderVID = Mod.rightShoulderObject.GetComponent<EFM_VanillaItemDescriptor>();
                FVRPhysicalObject rightShoulderPhysObj = rightShoulderVID.GetComponent<FVRPhysicalObject>();
                rightShoulderPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= rightShoulderVID.currentWeight;
                rightShoulderVID.destroyed = true;
                Mod.rightShoulderObject = null;
                Destroy(Mod.rightShoulderObject);
            }

            // Destroy pockets' contents, note that the quick belt config went back to pockets only when we unequipped rig
            for(int i=0; i < 4; ++i)
            {
                if(GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject != null)
                {
                    EFM_VanillaItemDescriptor pocketItemVID = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.GetComponent<EFM_VanillaItemDescriptor>();
                    EFM_CustomItemWrapper pocketItemCIW = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                    if(pocketItemCIW != null)
                    {
                        Mod.weight -= pocketItemCIW.currentWeight;
                        pocketItemCIW.destroyed = true;
                    }
                    else if(pocketItemVID != null)
                    {
                        Mod.weight -= pocketItemVID.currentWeight;
                        pocketItemVID.destroyed = true;
                    }
                    FVRPhysicalObject pocketItemPhysObj = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject;
                    pocketItemPhysObj.SetQuickBeltSlot(null);
                    Destroy(pocketItemPhysObj.gameObject);
                }
            }

            // Set raid state
            Mod.justFinishedRaid = true;
            Mod.raidState = raidState;

            // Disable extraction list and timer
            Mod.playerStatusUI.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
            Mod.playerStatusManager.extractionTimerText.color = Color.black;
            Mod.extractionLimitUI.SetActive(false);
            Mod.extractionUI.SetActive(false);

            EFM_Manager.LoadBase(5); // Load autosave, which is right before the start of raid

            extracted = true;
        }
    }

    public class InsuredSet
    {
        public long returnTime;
        public Dictionary<string, int> items;
    }

    public class ExtractionManager : MonoBehaviour
    {
        public EFM_Raid_Manager raidManager;

        public Extraction extraction;

        private List<Collider> playerColliders;
        public bool active;

        private void Start()
        {
            Mod.instance.LogInfo("Extraction manager created for extraction: "+extraction.name);
            playerColliders = new List<Collider>();
        }

        private void Update()
        {
            active = false;
            if(extraction.times != null && extraction.times.Count > 0)
            {
                foreach(TimeInterval timeInterval in extraction.times)
                {
                    if(raidManager.time >= timeInterval.start && raidManager.time <= timeInterval.end)
                    {
                        active = true;
                        break;
                    }
                }
            }
            else
            {
                active = true;
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            EFM_Raid_Manager.currentManager.currentExtraction = this;
        }

        private void OnTriggerExit(Collider collider)
        {
            EFM_Raid_Manager.currentManager.currentExtraction = null;
        }
    }

    public class Extraction
    {
        public string name;
        public GameObject gameObject;

        public List<TimeInterval> raidTimes;
        public List<TimeInterval> times;
        public Dictionary<string, int> itemRequirements;
        public List<string> equipmentRequirements;
        public List<string> itemBlacklist;
        public List<string> equipmentBlacklist;
        public int role;
        public bool accessRequirement;
        public GameObject card;

        public string RequirementsMet()
        {
            if((itemRequirements == null || itemRequirements.Count == 0) &&
               (itemBlacklist == null || itemBlacklist.Count == 0) &&
               (equipmentRequirements == null || equipmentRequirements.Count == 0) &&
               (equipmentBlacklist == null || equipmentBlacklist.Count == 0))
            {
                return "";
            }

            // Item requirements
            foreach(KeyValuePair<string, int> entry in itemRequirements)
            {
                string id = entry.Key;
                if (!Mod.playerInventory.ContainsKey(id) || Mod.playerInventory[id] < entry.Value)
                {
                    if(int.TryParse(id, out int result))
                    {
                        if (entry.Value > 1)
                        {
                            return "Need " + entry.Value + Mod.itemPrefabs[result].name;
                        }
                        else
                        {
                            return "Need " + Mod.itemPrefabs[result].name;
                        }
                    }
                    else
                    {
                        if (entry.Value > 1)
                        {
                            return "Need " + entry.Value + IM.OD[id].name;
                        }
                        else
                        {
                            return "Need " + IM.OD[id].name;
                        }
                    }
                }
            }

            // Item blacklist
            foreach(string id in itemBlacklist)
            {
                if (Mod.playerInventory.ContainsKey(id))
                {
                    if(int.TryParse(id, out int result))
                    {
                        return "Can't have " + Mod.itemPrefabs[result].name;
                    }
                    else
                    {
                        return "Can't have " + IM.OD[id].name;
                    }
                }
            }

            // Equipment requirements
            foreach(string id in equipmentRequirements)
            {
                bool found = false;
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i].CurObject.ObjectWrapper.ItemID.Equals(id))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (int.TryParse(id, out int result))
                    {
                        return "Need " + Mod.itemPrefabs[result].name + " equipped";
                    }
                    else
                    {
                        return "Need " + IM.OD[id].name + " equipped";
                    }
                }
            }

            // Equipment blacklist
            foreach(string id in equipmentBlacklist)
            {
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i].CurObject.ObjectWrapper.ItemID.Equals(id))
                    {
                        if (int.TryParse(id, out int result))
                        {
                            return "Can't have " + Mod.itemPrefabs[result].name + " equipped";
                        }
                        else
                        {
                            return "Can't have " + IM.OD[id].name + " equipped";
                        }
                    }
                }
                if (id.Equals("5448e53e4bdc2d60728b4567")) // Backpack
                {
                    return "Can't have a backpack equipped";
                }
                else if (id.Equals("5448e54d4bdc2dcc718b4568")) // Armor
                {
                    return "Can't have body armor equipped";
                }
                //else if (id.Equals("57bef4c42459772e8d35a53b")) // Armored equip
                //{
                //    return "Can't have armored rig equipped";
                //}
            }

            return "";
        }
    }

    public class TimeInterval
    {
        public int start;
        public int end;
    }

    public class DoorRotationSettings
    {
        public Transform placeholder;
        public Transform actual;
    }

    public class AISpawn
    {
        public enum AISpawnType
        {
            Scav,
            PMC,
            Raider,
            Boss,
            Follower
        }
        public AISpawnType type;
        public string name;
        public string leaderName;
        public int experienceReward;

        public AIInventory inventory;

        public Dictionary<int, List<string>> outfitByLink;
        public string sosigWeapon;
        public string sosigGrenade;

        public float spawnTime;

        // Raider
        public int squadSize;
    }

    public class AIInventory
    {
        public List<string> generic; // All items that don't require specific logic, just spawn those directly on death of AI

        public string rig;
        public string[] rigContents;

        public string dogtag;
        public int dogtagLevel;
        public string dogtagName;

        public string backpack;
        public List<string> backpackContents;

        public string primaryWeapon;
        public AIInventoryWeaponMod primaryWeaponMods;

        public string secondaryWeapon;
        public AIInventoryWeaponMod secondaryWeaponMods;

        public string holster;
        public AIInventoryWeaponMod holsterMods;
    }

    public class AIInventoryWeaponMod
    {
        public AIInventoryWeaponMod parent;
        public List<AIInventoryWeaponMod> children;

        public string ID;
        public string type;

        public AIInventoryWeaponMod(AIInventoryWeaponMod parent, string ID, string type)
        {
            this.parent = parent;
            children = new List<AIInventoryWeaponMod>();

            this.ID = ID;
            this.type = type;
        }

        public AIInventoryWeaponMod FindChild(string ID)
        {
            foreach (AIInventoryWeaponMod child in children)
            {
                if (child.ID.Equals(ID))
                {
                    return child;
                }
            }
            return null;
        }
    }

    public class BotData
    {
        public int[] minInventoryLevels;
        public JObject[] inventoryDB;
        public JObject chances;
        public JObject experience;
        public JArray names;
        public JObject health;
        public JObject generation;
    }
}
