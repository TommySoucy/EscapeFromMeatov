using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;
using Valve.VR.InteractionSystem;

namespace EFM
{
    public class Raid_Manager : Manager
    {
        public static readonly float extractionTime = 10;

        public static Raid_Manager currentManager;
        public static float extractionTimer;
        public static bool inRaid;
        public static JObject locationData;

        private List<Extraction> extractions;
        private List<Extraction> possibleExtractions;
        public ExtractionManager currentExtraction;
        private bool inExtractionLastFrame;
        public Transform spawnPoint;
        public Light sun;
        public float time;
        public GCManager GCManager;
        private float maxRaidTime;

        private List<GameObject> extractionCards;
        public bool extracted;
        private bool spawning;

        // AI
        private float initSpawnTimer = 5; // Will only start spawning AI once this much time has elapsed at start of raid
        public static int maxBotPerZone;
        private Dictionary<string, int> AISquadIFFs;
        private Dictionary<string, List<Sosig>> AISquads;
        private Dictionary<string, int> AISquadSizes;
        private List<Sosig> friendlyAI;
        public static List<AIEntity> entities;
        public static List<AI> entityRelatedAI;
        private List<AISpawn> AISpawns;
        private AISpawn nextSpawn;
        private Dictionary<string, Transform> AISquadSpawnTransforms;
        private List<int> availableIFFs;
        private int[] spawnedIFFs;

        public override void Init()
        {
            Mod.currentRaidManager = this;
            Mod.lootingExp = 0;
            Mod.healingExp = 0;
            Mod.explorationExp = 0;
            Mod.raidTime = 0;
            Mod.distanceTravelledSprinting = 0;
            Mod.distanceTravelledWalking = 0;

            locationData = Mod.locationsBaseDB[GetLocationDataIndex(Mod.chosenMapIndex)];

            // Set live data
            for (int i = 0; i < 7; ++i)
            {
                Mod.currentHealthRates[i] -= Mod.hideoutHealthRates[i];
            }
            Mod.currentEnergyRate -= Mod.hideoutEnergyRate;
            Mod.currentHydrationRate -= Mod.hideoutHydrationRate;
            Mod.currentEnergyRate += Mod.raidEnergyRate;
            Mod.currentHydrationRate += Mod.raidHydrationRate;

            // Manage active descriptions dict
            if (Mod.activeDescriptionsByItemID != null)
            {
                Mod.activeDescriptionsByItemID.Clear();
            }
            else
            {
                Mod.activeDescriptionsByItemID = new Dictionary<string, List<DescriptionManager>>();
            }

            // Clear other active slots since we shouldn't have any on load
            Mod.otherActiveSlots.Clear();

            // Manager GC ourselves
            GCManager = gameObject.AddComponent<GCManager>();

            Mod.LogInfo("Raid init called");
            currentManager = this;

            LoadMapData();
            Mod.LogInfo("Map data read");

            // Choose spawnpoints
            Transform spawnRoot = transform.GetChild(transform.childCount - 1).GetChild(0);
            spawnPoint = spawnRoot.GetChild(UnityEngine.Random.Range(0, spawnRoot.childCount));

            GM.CurrentSceneSettings.DeathResetPoint = spawnPoint;

            Mod.LogInfo("Got spawn");

            // Find extractions
            Mod.currentTaskLeaveItemConditionsByItemIDByZone = new Dictionary<string, Dictionary<string, List<TraderTaskCondition>>>();
            Mod.currentTaskVisitPlaceConditionsByZone = new Dictionary<string, List<TraderTaskCondition>>();
            Mod.currentTaskVisitPlaceCounterConditionsByZone = new Dictionary<string, List<TraderTaskCounterCondition>>();
            possibleExtractions = new List<Extraction>();
            Extraction bestCandidate = null; // Must be an extraction without appearance times (always available) and no other requirements. This will be the minimum extraction
            float farthestDistance = float.MinValue;
            Mod.LogInfo("Init raid with map index: " + Mod.chosenMapIndex + ", which has " + extractions.Count + " extractions");
            for (int extractionIndex = 0; extractionIndex < extractions.Count; ++extractionIndex)
            {
                Extraction currentExtraction = extractions[extractionIndex];
                currentExtraction.gameObject = gameObject.transform.GetChild(gameObject.transform.childCount - 2).GetChild(extractionIndex).gameObject;
                currentExtraction.gameObject.AddComponent<ZoneManager>();
                if (Mod.taskVisitPlaceConditionsByZone.ContainsKey(currentExtraction.gameObject.name))
                {
                    List<TraderTaskCondition> conditions = Mod.taskVisitPlaceConditionsByZone[currentExtraction.gameObject.name];
                    for(int i=conditions.Count-1; i>=0; --i)
                    {
                        if(conditions[i].failCondition && conditions[i].task.taskState != TraderTask.TaskState.Active)
                        {
                            conditions.RemoveAt(i);
                        }
                    }
                    if (conditions.Count > 0)
                    {
                        Mod.currentTaskVisitPlaceConditionsByZone.Add(currentExtraction.gameObject.name, conditions);
                    }
                }
                if (Mod.taskVisitPlaceCounterConditionsByZone.ContainsKey(currentExtraction.gameObject.name))
                {
                    List<TraderTaskCounterCondition> counterConditions = Mod.taskVisitPlaceCounterConditionsByZone[currentExtraction.gameObject.name];
                    for (int i = counterConditions.Count - 1; i >= 0; --i)
                    {
                        if (counterConditions[i].parentCondition.failCondition && counterConditions[i].parentCondition.task.taskState != TraderTask.TaskState.Active)
                        {
                            counterConditions.RemoveAt(i);
                        }
                    }
                    if (counterConditions.Count > 0)
                    {
                        Mod.currentTaskVisitPlaceCounterConditionsByZone.Add(currentExtraction.gameObject.name, counterConditions);
                    }
                }

                bool canBeMinimum = (currentExtraction.times == null || currentExtraction.times.Count == 0) &&
                                    (currentExtraction.itemRequirements == null || currentExtraction.itemRequirements.Count == 0) &&
                                    (currentExtraction.equipmentRequirements == null || currentExtraction.equipmentRequirements.Count == 0) &&
                                    (currentExtraction.role == Mod.chosenCharIndex || currentExtraction.role == 2) &&
                                    !currentExtraction.accessRequirement;
                float currentDistance = Vector3.Distance(spawnPoint.position, currentExtraction.gameObject.transform.position);
                Mod.LogInfo("\tExtraction at index: " + extractionIndex + " has " + currentExtraction.times.Count + " times and " + currentExtraction.itemRequirements.Count + " items reqs. Its current distance from player is: " + currentDistance);
                if (UnityEngine.Random.value <= ExtractionChanceByDist(currentDistance))
                {
                    Mod.LogInfo("\t\tAdding this extraction to list possible extractions");
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
                Mod.LogError("No minimum extraction found");
            }

            Mod.LogInfo("Got extractions");

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
            Mod.LogInfo("Inited extract cards");

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

            Mod.LogInfo("Initializing doors");
            // TODO: Uncomment and delete what comes after once doors aren't as laggy. Will also have to put vanilla doors first in hierarchy in map asset and use the modified map meshes to accomodate doors
            /*
            foreach (JToken doorData in Mod.mapData["maps"][Mod.chosenMapIndex]["doors"])
            {
                GameObject doorObject = doorRoot.GetChild(doorIndex).gameObject;
                Mod.LogInfo("\t" + doorObject.name);
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
                DoorWrapper doorWrapper = doorObject.AddComponent<DoorWrapper>();

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

            Mod.LogInfo("Initialized doors, spawning loose loot");

            // Spawn loose loot
            if (Mod.spawnItems)
            {
                JObject locationDB = Mod.locationsLootDB[Mod.chosenMapIndex];
                Transform itemsRoot = transform.GetChild(1).GetChild(1).GetChild(2);
                // Forced, always spawns TODO: Unless player has it already? Unless player doesnt have the quest yet?
                Mod.LogInfo("Spawning forced loot, forced null?: " + (locationDB["forced"] == null));
                foreach (JToken forced in locationDB["forced"])
                {
                    Mod.LogInfo("Forced entry, items null?: " + (forced["Items"] == null));
                    Dictionary<string, CustomItemWrapper> spawnedItemCIWs = new Dictionary<string, CustomItemWrapper>();
                    Dictionary<string, VanillaItemDescriptor> spawnedItemVIDs = new Dictionary<string, VanillaItemDescriptor>();
                    List<string> unspawnedParents = new List<string>();

                    // Get item from item map
                    string originalID = forced["Items"][0].Type != JTokenType.String ? forced["Items"][0]["_tpl"].ToString() : forced["Items"][0].ToString();
                    Mod.LogInfo("Got ID");
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                itemID = itemMapEntry.ID;
                                break;
                            case 1:
                                itemID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                itemID = itemMapEntry.otherModID;
                                break;
                        }
                    }
                    else
                    {
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
                    SpawnLootItem(itemPrefab, itemsRoot, itemID, forced, originalID, false);
                }

                // Dynamic, has chance of spawning
                Mod.LogInfo("Spawning dynamic loot");
                foreach (JToken dynamicSpawn in locationDB["dynamic"])
                {
                    Dictionary<string, CustomItemWrapper> spawnedItemCIWs = new Dictionary<string, CustomItemWrapper>();
                    Dictionary<string, VanillaItemDescriptor> spawnedItemVIDs = new Dictionary<string, VanillaItemDescriptor>();
                    List<string> unspawnedParents = new List<string>();

                    // Get item from item map
                    string[] lootTableIDSplit = dynamicSpawn["id"].ToString().Split(' ');
                    string originalID = null;
                    if (lootTableIDSplit.Length == 1 || Mod.dynamicLootTable[Mod.LocationIndexToDataName(Mod.chosenMapIndex)] == null || Mod.dynamicLootTable[Mod.LocationIndexToDataName(Mod.chosenMapIndex)][lootTableIDSplit[0]] == null)
                    {
                        Mod.LogWarning("Failed to get loot table ID from: " + lootTableIDSplit[0] + ", spawning specified item instead");
                        JArray items = dynamicSpawn["Items"].Value<JArray>();
                        originalID = items[0].Type != JTokenType.String ? items[0]["_tpl"].ToString() : items[0].ToString();
                    }
                    else
                    {
                        JArray possibleItems = Mod.dynamicLootTable[Mod.LocationIndexToDataName(Mod.chosenMapIndex)][lootTableIDSplit[0]]["SpawnList"] as JArray;
                        originalID = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)].ToString();
                    }
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                itemID = itemMapEntry.ID;
                                break;
                            case 1:
                                itemID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                itemID = itemMapEntry.otherModID;
                                break;
                        }
                    }
                    else
                    {
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
                    SpawnLootItem(itemPrefab, itemsRoot, itemID, dynamicSpawn, originalID, true);
                }
            }

            Mod.LogInfo("Done spawning loose loot, initializing container");

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
                        CustomItemWrapper containerCIW = container.gameObject.AddComponent<CustomItemWrapper>();
                        containerCIW.itemType = Mod.ItemType.LootContainer;
                        container.gameObject.SetActive(true);
                        containerCIW.canInsertItems = false;
                        containerCIW.mainContainer = mainContainer.gameObject;
                        containerCIW.containerItemRoot = container.GetChild(container.childCount - 2);

                        LootContainer containerScript = container.gameObject.AddComponent<LootContainer>();
                        containerScript.mainContainerCollider = mainContainer.GetComponent<Collider>();
                        JToken gridProps = containerData["_props"]["Grids"][0]["_props"];
                        containerScript.Init(Mod.staticLootTable[containerData["_id"].ToString()]["SpawnList"].ToObject<List<string>>(), (int)gridProps["cellsH"] * (int)gridProps["cellsV"]);

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
                        LootContainerCover cover = container.GetChild(0).gameObject.AddComponent<LootContainerCover>();
                        cover.keyID = mapContainerData[i]["keyID"].ToString();
                        cover.hasKey = !cover.keyID.Equals("");
                        cover.Root = container.GetChild(container.childCount - 3);
                        cover.MinRot = -90;
                        cover.MaxRot = 0;

                        LootContainer openableContainerScript = container.gameObject.AddComponent<LootContainer>();
                        openableContainerScript.interactable = cover;
                        openableContainerScript.mainContainerCollider = mainContainer.GetComponent<Collider>();
                        JToken openableContainergridProps = containerData["_props"]["Grids"][0]["_props"];
                        openableContainerScript.Init(Mod.staticLootTable[containerData["_id"].ToString()]["SpawnList"].ToObject<List<string>>(), (int)openableContainergridProps["cellsH"] * (int)openableContainergridProps["cellsV"]);

                        mainContainer.GetComponent<MeshRenderer>().material = Mod.quickSlotConstantMaterial;
                        break;
                    case "Drawer":
                        // Containers that must be slid open
                        for (int drawerIndex = 0; drawerIndex < 4; ++drawerIndex)
                        {
                            Transform drawerTransform = container.GetChild(drawerIndex);
                            LootContainerSlider slider = drawerTransform.gameObject.AddComponent<LootContainerSlider>();
                            slider.keyID = mapContainerData[i]["keyID"].ToString();
                            slider.hasKey = !slider.keyID.Equals("");
                            slider.Root = slider.transform;
                            slider.MinY = -0.3f;
                            slider.MaxY = 0.2f;
                            slider.posZ = container.GetChild(drawerIndex).localPosition.z;

                            LootContainer drawerScript = drawerTransform.gameObject.AddComponent<LootContainer>();
                            drawerScript.interactable = slider;
                            drawerScript.mainContainerCollider = drawerTransform.GetChild(drawerTransform.childCount - 1).GetComponent<Collider>();
                            JToken drawerGridProps = containerData["_props"]["Grids"][0]["_props"];
                            drawerScript.Init(Mod.staticLootTable[containerData["_id"].ToString()]["SpawnList"].ToObject<List<string>>(), (int)drawerGridProps["cellsH"] * (int)drawerGridProps["cellsV"]);

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
                DamageVolume newDamageVolume = damageVolTransform.gameObject.AddComponent<DamageVolume>();
                newDamageVolume.Init();
            }

            // Init experience triggers
            Transform expTriggersParent = transform.GetChild(1).GetChild(1).GetChild(3).GetChild(5);
            if (Mod.triggeredExplorationTriggers[Mod.chosenMapIndex].Count > 0)
            {
                for (int i = 0; i < expTriggersParent.childCount; ++i)
                {
                    if (!Mod.triggeredExplorationTriggers[Mod.chosenMapIndex][i])
                    {
                        expTriggersParent.GetChild(i).gameObject.AddComponent<ExperienceTrigger>();
                    }
                }
            }
            else
            {
                for (int i = 0; i < expTriggersParent.childCount; ++i)
                {
                    Mod.triggeredExplorationTriggers[Mod.chosenMapIndex].Add(false);
                    expTriggersParent.GetChild(i).gameObject.AddComponent<ExperienceTrigger>();
                }
            }

            // Init zones
            Transform questParent = transform.GetChild(1).GetChild(1).GetChild(3).GetChild(6);
            foreach(Transform questChild in questParent)
            {
                questChild.gameObject.AddComponent<ZoneManager>();
                LeaveItemProcessor currentLeaveItemProcessor = questChild.gameObject.AddComponent<LeaveItemProcessor>();
                if (Mod.taskLeaveItemConditionsByItemIDByZone.ContainsKey(questChild.name))
                {
                    currentLeaveItemProcessor.conditionsByItemID = Mod.taskLeaveItemConditionsByItemIDByZone[questChild.name];
                    Mod.currentTaskLeaveItemConditionsByItemIDByZone.Add(questChild.name, currentLeaveItemProcessor.conditionsByItemID);
                    // Add itemID if there is at least one condition in the list that is not a fail condition or if hte task is active
                    foreach(KeyValuePair<string, List<TraderTaskCondition>> entry in currentLeaveItemProcessor.conditionsByItemID)
                    {
                        foreach(TraderTaskCondition condition in entry.Value)
                        {
                            if(!condition.failCondition || condition.task.taskState == TraderTask.TaskState.Active)
                            {
                                currentLeaveItemProcessor.itemIDs.Add(entry.Key);
                                break;
                            }
                        }
                    }
                }
                if (Mod.taskVisitPlaceConditionsByZone.ContainsKey(questChild.name))
                {
                    List<TraderTaskCondition> conditions = Mod.taskVisitPlaceConditionsByZone[questChild.name];
                    for (int i = conditions.Count - 1; i >= 0; --i)
                    {
                        if (conditions[i].failCondition && conditions[i].task.taskState != TraderTask.TaskState.Active)
                        {
                            conditions.RemoveAt(i);
                        }
                    }
                    if (conditions.Count > 0)
                    {
                        Mod.currentTaskVisitPlaceConditionsByZone.Add(questChild.name, conditions);
                    }
                }
                if (Mod.taskVisitPlaceCounterConditionsByZone.ContainsKey(questChild.name))
                {
                    List<TraderTaskCounterCondition> counterConditions = Mod.taskVisitPlaceCounterConditionsByZone[questChild.name];
                    for (int i = counterConditions.Count - 1; i >= 0; --i)
                    {
                        if (counterConditions[i].parentCondition.failCondition && counterConditions[i].parentCondition.task.taskState != TraderTask.TaskState.Active)
                        {
                            counterConditions.RemoveAt(i);
                        }
                    }
                    if (counterConditions.Count > 0)
                    {
                        Mod.currentTaskVisitPlaceCounterConditionsByZone.Add(questChild.name, counterConditions);
                    }
                }
            }

            // Init current health effect counter conditions
            Mod.currentHealthEffectCounterConditionsByEffectType = new Dictionary<Effect.EffectType, List<TraderTaskCounterCondition>>();
            foreach(KeyValuePair<Effect.EffectType, List<TraderTaskCounterCondition>> entry in Mod.taskHealthEffectCounterConditionsByEffectType)
            {
                foreach(TraderTaskCounterCondition condition in entry.Value)
                {
                    if(condition.parentCondition.task.taskState == TraderTask.TaskState.Active)
                    {
                        if (Mod.currentHealthEffectCounterConditionsByEffectType.ContainsKey(entry.Key))
                        {
                            Mod.currentHealthEffectCounterConditionsByEffectType[entry.Key].Add(condition);
                        }
                        else
                        {
                            Mod.currentHealthEffectCounterConditionsByEffectType.Add(entry.Key, new List<TraderTaskCounterCondition>() { condition });
                        }
                    }
                }
            }

            // Init current shots counter conditions
            Mod.currentShotsCounterConditionsByBodyPart = new Dictionary<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>>();
            foreach(KeyValuePair<TraderTaskCounterCondition.CounterConditionTargetBodyPart, List<TraderTaskCounterCondition>> entry in Mod.taskShotsCounterConditionsByBodyPart)
            {
                foreach(TraderTaskCounterCondition condition in entry.Value)
                {
                    if (condition.parentCondition.task.taskState == TraderTask.TaskState.Locked ||
                        condition.parentCondition.task.taskState == TraderTask.TaskState.Active)
                    {
                        if (Mod.currentShotsCounterConditionsByBodyPart.ContainsKey(entry.Key))
                        {
                            Mod.currentShotsCounterConditionsByBodyPart[entry.Key].Add(condition);
                        }
                        else
                        {
                            Mod.currentShotsCounterConditionsByBodyPart.Add(entry.Key, new List<TraderTaskCounterCondition>() { condition });
                        }
                    }
                }
            }

            // Init current use item counter conditions
            Mod.currentUseItemCounterConditionsByItemID = new Dictionary<string, List<TraderTaskCounterCondition>>();
            foreach(KeyValuePair<string, List<TraderTaskCounterCondition>> entry in Mod.taskUseItemCounterConditionsByItemID)
            {
                foreach(TraderTaskCounterCondition condition in entry.Value)
                {
                    if (condition.parentCondition.task.taskState == TraderTask.TaskState.Locked ||
                        condition.parentCondition.task.taskState == TraderTask.TaskState.Active)
                    {
                        if (Mod.currentUseItemCounterConditionsByItemID.ContainsKey(entry.Key))
                        {
                            Mod.currentUseItemCounterConditionsByItemID[entry.Key].Add(condition);
                        }
                        else
                        {
                            Mod.currentUseItemCounterConditionsByItemID.Add(entry.Key, new List<TraderTaskCounterCondition>() { condition });
                        }
                    }
                }
            }

            // Init player if scav
            if (Mod.chosenCharIndex == 1)
            {
                BotData playerScavData = GetBotData("playerscav");
                float averageLevel = (float)locationData["AveragePlayerLevel"];
                AISpawn newAISpawn = GenerateAISpawn(playerScavData, AISpawn.AISpawnType.Player, averageLevel, "Player");
                AnvilManager.Run(SpawnAIInventory(newAISpawn.inventory, spawnPoint.position, true));
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

            // Load night lightmaps if necessary
            if (time <= 21600 || time >= 64800)
            {
                AssetBundle lightmapAssetBundle = AssetBundle.LoadFromFile(Mod.path + "/EscapeFromMeatov" + Mod.chosenMapName + "Night.ab");
                if(lightmapAssetBundle != null)
                {
                    foreach(LightmapData lmd in LightmapSettings.lightmaps)
                    {
                        lmd.lightmapColor = lightmapAssetBundle.LoadAsset<Texture2D>(lmd.lightmapColor.name);
                    }
                    LightmapSettings.lightProbes = lightmapAssetBundle.LoadAsset<LightProbes>(LightmapSettings.lightProbes.name);
                    RenderSettings.skybox = lightmapAssetBundle.LoadAsset<Material>("SkyNight");
                }
                else
                {
                    Mod.LogError("Night time raid selected, but EscapeFromMeatov" + Mod.chosenMapName + "Night.ab could not be loaded");
                }
            }

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
                Mod.extractionLimitUI.transform.position = vector;
                Mod.extractionLimitUI.transform.rotation = Quaternion.LookRotation(vector2, Vector3.up);
                Mod.extractionLimitUI.transform.Rotate(Vector3.right, -25);

                if (Mod.playerStatusManager.extractionTimerText.color != Color.red)
                {
                    Mod.playerStatusManager.extractionTimerText.color = Color.red;
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
                    vector2 *= 0.33f;
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
                            Mod.raidState = Base_Manager.FinishRaidState.RunThrough;
                        }
                        else
                        {
                            Mod.raidState = Base_Manager.FinishRaidState.Survived;
                        }

                        // Disable extraction list
                        Mod.playerStatusUI.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
                        Mod.playerStatusManager.extractionTimerText.color = Color.black; 
                        Mod.extractionLimitUI.SetActive(false);
                        Mod.playerStatusManager.SetDisplayed(false);
                        Mod.extractionUI.SetActive(false);

                        UpdateExitStatusCounterConditions();
                        ResetHealthEffectCounterConditions();

                        Manager.LoadBase(5); // Load autosave, which is right before the start of raid

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
                KillPlayer(Base_Manager.FinishRaidState.MIA);
            }

            UpdateEffects();

            UpdateAI();

            // TODO: Reeneable these once we figure out efficient way to rotate sun with time for realistic day/night cycle
            //UpdateTime();

            //UpdateSun();
        }

        private void UpdateExitStatusCounterConditions()
        {
            if(Mod.chosenCharIndex == 1)
            {
                return;
            }

            if (Mod.taskStartCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.ExitStatus))
            {
                List<TraderTaskCounterCondition> startExitStatusCounterConditions = Mod.taskStartCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.ExitStatus];
                foreach (TraderTaskCounterCondition counterCondition in startExitStatusCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Locked)
                    {
                        continue;
                    }

                    // Check exit status
                    bool wrongStatus = false;
                    switch (Mod.raidState)
                    {
                        case Base_Manager.FinishRaidState.Survived:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Survived))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.RunThrough:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Runner))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.KIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Killed))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.MIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.MissingInAction))
                            {
                                wrongStatus = true;
                            }
                            break;
                    }
                    if (wrongStatus)
                    {
                        continue;
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    counterCondition.completed = true;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
            if (Mod.taskCompletionCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.ExitStatus))
            {
                List<TraderTaskCounterCondition> completionExitStatusCounterConditions = Mod.taskCompletionCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.ExitStatus];
                foreach (TraderTaskCounterCondition counterCondition in completionExitStatusCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Active)
                    {
                        continue;
                    }

                    // Check exit status
                    bool wrongStatus = false;
                    switch (Mod.raidState)
                    {
                        case Base_Manager.FinishRaidState.Survived:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Survived))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.RunThrough:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Runner))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.KIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Killed))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.MIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.MissingInAction))
                            {
                                wrongStatus = true;
                            }
                            break;
                    }
                    if (wrongStatus)
                    {
                        continue;
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    counterCondition.completed = true;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
            if (Mod.taskFailCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.ExitStatus))
            {
                List<TraderTaskCounterCondition> failExitStatusCounterConditions = Mod.taskFailCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.ExitStatus];
                foreach (TraderTaskCounterCondition counterCondition in failExitStatusCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Active)
                    {
                        continue;
                    }

                    // Check exit status
                    bool wrongStatus = false;
                    switch (Mod.raidState)
                    {
                        case Base_Manager.FinishRaidState.Survived:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Survived))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.RunThrough:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Runner))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.KIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.Killed))
                            {
                                wrongStatus = true;
                            }
                            break;
                        case Base_Manager.FinishRaidState.MIA:
                            if (!counterCondition.counterConditionTargetExitStatuses.Contains(TraderTaskCounterCondition.CounterConditionTargetExitStatus.MissingInAction))
                            {
                                wrongStatus = true;
                            }
                            break;
                    }
                    if (wrongStatus)
                    {
                        continue;
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    counterCondition.completed = true;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
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

        public void ForceSpawnNextAI()
        {
            if (!spawning && nextSpawn != null)
            {
                Mod.forceSpawnAI = true;
                AnvilManager.Run(SpawnAI(nextSpawn));
                AISpawns.RemoveAt(AISpawns.Count - 1);
                nextSpawn = AISpawns.Count > 0 ? AISpawns[AISpawns.Count - 1] : null;
            }
        }

        private IEnumerator SpawnAI(AISpawn spawnData)
        {
            if (!Mod.forceSpawnAI)
            {
                if (!Mod.spawnAI)
                {
                    yield break;
                }

                if (Mod.spawnOnlyFirstAI)
                {
                    if (Mod.spawnedFirstAI)
                    {
                        yield break;
                    }
                    else
                    {
                        Mod.spawnedFirstAI = true;
                    }
                }
            }
            else
            {
                Mod.forceSpawnAI = false;
            }

            Mod.LogInfo("SPAWNAI: SpawnAI called with name "+spawnData.name+" of type: "+spawnData.type);
            spawning = true;
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            Mod.LogInfo("SPAWNAI " + spawnData.name+": \tGot sosig body prefab, null?: "+(sosigPrefab == null));

            // TODO: Make sure that wherever we spawn a bot, it is far enough from player, and there is no direct line of sight between player and potential spawnpoint
            Transform AISpawnPoint = null;
            switch (spawnData.type)
            {
                case AISpawn.AISpawnType.Scav:
                    List<Transform> mostAvailableScavBotZones = GetMostAvailableBotZones();
                    Transform currentScavZone = mostAvailableScavBotZones[UnityEngine.Random.Range(0, mostAvailableScavBotZones.Count)];
                    BotZone scavZoneScript = currentScavZone.GetComponent<BotZone>();
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
                        BotZone raiderBotZoneScript = randomRaiderZone.GetComponent<BotZone>();
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
                        BotZone bossBotZoneScript = randomBossZone.GetComponent<BotZone>();
                        ++bossBotZoneScript.botCount;

                        AISpawnPoint = randomBossZone.GetChild(UnityEngine.Random.Range(0, randomBossZone.childCount));

                        AISquadSpawnTransforms.Add(spawnData.leaderName, randomBossZone);
                    }
                    break;
                default:
                    break;
            }

            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tGot spawnpoint at " + AISpawnPoint.position);

            GameObject sosigObject = Instantiate(sosigPrefab, AISpawnPoint.position, AISpawnPoint.rotation);
            sosigObject.name = spawnData.type.ToString() + " AI ("+spawnData.name+")";
            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tInstantiated sosig, position: " + sosigObject.transform.position);
            Sosig sosigScript = sosigObject.GetComponentInChildren<Sosig>();
            sosigScript.InitHands();
            // TODO: Here, if needed, is where we add new slots, but new slots need to be initialized, we need to set a target transform to each and there might be more, need to check
            sosigScript.Inventory.Init();

            // Remove all interactive objects on the sosig from All
            foreach(FVRInteractiveObject io in sosigObject.GetComponentsInChildren<FVRInteractiveObject>())
            {
                Mod.RemoveFromAll(io, null, null);
            }

            AI AIScript = sosigObject.AddComponent<AI>();
            AIScript.experienceReward = spawnData.experienceReward;
            AIScript.entityIndex = entities.Count;
            AIScript.inventory = spawnData.inventory;
            AIScript.USEC = spawnData.USEC;
            AIScript.type = spawnData.type;
            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tAdded EFM_AI script");

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
            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawned outfit items");

            // Spawn sosig weapon and grenade
            if (spawnData.sosigWeapon != null)
            {
                yield return IM.OD[spawnData.sosigWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[spawnData.sosigWeapon].GetGameObject());
                Mod.RemoveFromAll(null, null, null);
                SosigWeapon sosigWeapon = weaponObject.GetComponent<SosigWeapon>();
                sosigWeapon.SetAutoDestroy(true);
                typeof(SosigWeapon).GetField("m_autoDestroyTickDown", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(sosigWeapon, 0);
                sosigScript.ForceEquip(sosigWeapon);
                if (sosigWeapon.Type == SosigWeapon.SosigWeaponType.Gun)
                {
                    sosigScript.Inventory.FillAmmoWithType(sosigWeapon.AmmoType);
                }
            }
            else
            {
                Mod.LogError(spawnData.type.ToString() + " type AI with name: " + spawnData.name + ", has no weapon");
            }

            if (spawnData.sosigGrenade != null)
            {
                yield return IM.OD[spawnData.sosigGrenade].GetGameObjectAsync();
                GameObject grenadeObject = Instantiate(IM.OD[spawnData.sosigGrenade].GetGameObject());
                Mod.RemoveFromAll(null, null, null);
                SosigWeapon sosigGrenade = grenadeObject.GetComponent<SosigWeapon>();
                sosigGrenade.SetAutoDestroy(true);
                sosigScript.ForceEquip(sosigGrenade);
            }
            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawned weapons");

            // Configure
            sosigScript.Configure(spawnData.configTemplate);

            // Set sosig logic (IFF, path, TODO: difficulty vars, etc)
            int iff = 0;
            switch (spawnData.type)
            {
                case AISpawn.AISpawnType.Scav:
                    if(Mod.chosenCharIndex == 1)
                    { 
                        if(friendlyAI != null)
                        {
                            friendlyAI.Add(sosigScript);
                        }
                    }
                    iff = 1;

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
            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tSpawning with IFF: "+iff);
            sosigScript.SetIFF(iff);
            sosigScript.SetOriginalIFFTeam(iff);

            //sosigScript.Priority.SetAllEnemy();
            //sosigScript.Priority.MakeFriendly(iff);

            // If player scav, also make player iff friendly
            if (Mod.chosenCharIndex == 1)
            {
                sosigScript.Priority.MakeFriendly(0);
            }

            sosigScript.CanBeGrabbed = false;
            sosigScript.CanBeKnockedOut = false;

            Mod.LogInfo("SPAWNAI " + spawnData.name + ": \tConfigured AI");

            spawning = false;
            yield break;
        }

        public void OnBotKill(Sosig sosig)
        {
            Mod.LogInfo("Sosig " + sosig.name + " killed");

            // Here we use GetOriginalIFFTeam instead of GetIFF because the actual IFF gets set to -3 on sosig death, original doesn't change
            --spawnedIFFs[sosig.GetOriginalIFFTeam()];
            if(spawnedIFFs[sosig.GetOriginalIFFTeam()] == 0)
            {
                availableIFFs.Add(sosig.GetOriginalIFFTeam());
            }

            // If player killed this bot
            AI AIScript = sosig.GetComponent<AI>();
            if (sosig.GetDiedFromIFF() == 0)
            {
                Mod.AddExperience(AIScript.experienceReward);

                Mod.killList.Add(sosig.name, AIScript.experienceReward);

                // Have to make all scavs enemies if this was a friendly scav that was killed
                if(sosig.GetOriginalIFFTeam() == 1)
                {
                    foreach(Sosig friendly in friendlyAI)
                    {
                        friendly.Priority.MakeEnemy(0);
                    }
                }

                // Update kill counter conditions
                UpdateKillCounterConditions(sosig, AIScript);
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

        private void UpdateKillCounterConditions(Sosig sosig, AI AIScript)
        {
            if (Mod.chosenCharIndex == 1)
            {
                return;
            }

            // TODO: Put these in current dict at start of raid to prevent taht we look through them all like this every kill we get
            if (Mod.taskStartCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.Kills))
            {
                List<TraderTaskCounterCondition> startKillCounterConditions = Mod.taskStartCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.Kills];
                foreach (TraderTaskCounterCondition counterCondition in startKillCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Locked)
                    {
                        continue;
                    }

                    // Check kill type
                    if (!((counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Any) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav && AIScript.type == AISpawn.AISpawnType.Scav) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec && AIScript.type == AISpawn.AISpawnType.PMC && AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear && AIScript.type == AISpawn.AISpawnType.PMC && !AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC && AIScript.type == AISpawn.AISpawnType.PMC)))
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
                            VanillaItemDescriptor VID = rightInteractable.GetComponent<VanillaItemDescriptor>();
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
                                CustomItemWrapper CIW = rightInteractable.GetComponent<CustomItemWrapper>();
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
                                VanillaItemDescriptor VID = leftInteractable.GetComponent<VanillaItemDescriptor>();
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
                                    CustomItemWrapper CIW = leftInteractable.GetComponent<CustomItemWrapper>();
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

                    // Check mods inclusive (Weapon must have these mods)
                    if (counterCondition.weaponModsInclusive != null && counterCondition.weaponModsInclusive.Count > 0)
                    {
                        if (Mod.rightHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.rightHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                        if (Mod.leftHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.leftHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                    }

                    // Check distance
                    if (counterCondition.distance != -1)
                    {
                        if (counterCondition.distanceCompareMode == 0)
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) < counterCondition.distance)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) > counterCondition.distance)
                            {
                                continue;
                            }
                        }
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    ++counterCondition.killCount;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
            if (Mod.taskCompletionCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.Kills))
            {
                List<TraderTaskCounterCondition> completionKillCounterConditions = Mod.taskCompletionCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.Kills];
                foreach (TraderTaskCounterCondition counterCondition in completionKillCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Active)
                    {
                        continue;
                    }

                    // Check kill type
                    if (!((counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav && AIScript.type == AISpawn.AISpawnType.Scav) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec && AIScript.type == AISpawn.AISpawnType.PMC && AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear && AIScript.type == AISpawn.AISpawnType.PMC && !AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC && AIScript.type == AISpawn.AISpawnType.PMC)))
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
                            VanillaItemDescriptor VID = rightInteractable.GetComponent<VanillaItemDescriptor>();
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
                                CustomItemWrapper CIW = rightInteractable.GetComponent<CustomItemWrapper>();
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
                                VanillaItemDescriptor VID = leftInteractable.GetComponent<VanillaItemDescriptor>();
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
                                    CustomItemWrapper CIW = leftInteractable.GetComponent<CustomItemWrapper>();
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

                    // Check mods inclusive (Weapon must have these mods)
                    if (counterCondition.weaponModsInclusive != null && counterCondition.weaponModsInclusive.Count > 0)
                    {
                        if (Mod.rightHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.rightHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                        if (Mod.leftHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.leftHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                    }

                    // Check distance
                    if (counterCondition.distance != -1)
                    {
                        if (counterCondition.distanceCompareMode == 0)
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) < counterCondition.distance)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) > counterCondition.distance)
                            {
                                continue;
                            }
                        }
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    ++counterCondition.killCount;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
            if (Mod.taskFailCounterConditionsByType.ContainsKey(TraderTaskCounterCondition.CounterConditionType.Kills))
            {
                List<TraderTaskCounterCondition> failKillCounterConditions = Mod.taskFailCounterConditionsByType[TraderTaskCounterCondition.CounterConditionType.Kills];
                foreach (TraderTaskCounterCondition counterCondition in failKillCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible || counterCondition.parentCondition.task.taskState != TraderTask.TaskState.Active)
                    {
                        continue;
                    }

                    // Check kill type
                    if (!((counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav && AIScript.type == AISpawn.AISpawnType.Scav) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec && AIScript.type == AISpawn.AISpawnType.PMC && AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear && AIScript.type == AISpawn.AISpawnType.PMC && !AIScript.USEC) ||
                          (counterCondition.counterConditionTargetEnemy == TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC && AIScript.type == AISpawn.AISpawnType.PMC)))
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
                            VanillaItemDescriptor VID = rightInteractable.GetComponent<VanillaItemDescriptor>();
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
                                CustomItemWrapper CIW = rightInteractable.GetComponent<CustomItemWrapper>();
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
                                VanillaItemDescriptor VID = leftInteractable.GetComponent<VanillaItemDescriptor>();
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
                                    CustomItemWrapper CIW = leftInteractable.GetComponent<CustomItemWrapper>();
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

                    // Check mods inclusive (Weapon must have these mods)
                    if (counterCondition.weaponModsInclusive != null && counterCondition.weaponModsInclusive.Count > 0)
                    {
                        if (Mod.rightHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.rightHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                        if (Mod.leftHand.fvrHand.CurrentInteractable != null)
                        {
                            LinkedList<string> tempWeaponMod = new LinkedList<string>(counterCondition.weaponModsInclusive);
                            if (!WeaponHasMods(Mod.leftHand.fvrHand.CurrentInteractable, ref tempWeaponMod))
                            {
                                continue;
                            }
                        }
                    }

                    // Check distance
                    if (counterCondition.distance != -1)
                    {
                        if (counterCondition.distanceCompareMode == 0)
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) < counterCondition.distance)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            if (Vector3.Distance(GM.CurrentPlayerBody.transform.position, sosig.transform.position) > counterCondition.distance)
                            {
                                continue;
                            }
                        }
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful kill, increment count and update fulfillment 
                    ++counterCondition.killCount;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
        }

        public static bool WeaponHasMods(FVRInteractiveObject item, ref LinkedList<string> mods)
        {
            if(mods.Count == 0)
            {
                return true;
            }

            FVRFireArm fireArm = item.GetComponent<FVRFireArm>();
            FVRFireArmAttachment attachment = item.GetComponent<FVRFireArmAttachment>();
            List<FVRFireArmAttachment> subAttachments = null;
            if(fireArm != null)
            {
                subAttachments = fireArm.Attachments;
            }
            else if(attachment != null)
            {
                subAttachments = attachment.Attachments;
            }
            else
            {
                return false;
            }

            CustomItemWrapper CIW = item.GetComponent<CustomItemWrapper>();
            VanillaItemDescriptor VID = item.GetComponent<VanillaItemDescriptor>();
            List<string> parents = null;
            if(VID != null)
            {
                if (mods.Remove(VID.H3ID) && mods.Count == 0)
                {
                    return true;
                }
                parents = VID.parents;
            }
            else if (CIW != null)
            {
                if (mods.Remove(CIW.ID) && mods.Count == 0)
                {
                    return true;
                }
                parents = CIW.parents;
            }
            else
            {
                return false;
            }

            foreach (string parent in VID.parents)
            {
                if (mods.Remove(parent) && mods.Count == 0)
                {
                    return true;
                }
            }

            foreach (FVRFireArmAttachmentMount subAttachmentMount in fireArm.AttachmentMounts)
            {
                if (subAttachmentMount.AttachmentsList.Count > 0)
                {
                    foreach (FVRFireArmAttachment subAttachment in subAttachmentMount.AttachmentsList)
                    {
                        if(WeaponHasMods(subAttachment as FVRInteractiveObject, ref mods))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private IEnumerator SpawnAIInventory(AIInventory inventory, Vector3 pos, bool player = false)
        {
            // Spawn rig
            FVRPhysicalObject.FVRPhysicalObjectSize[] rigSlotSizes = null;
            CustomItemWrapper rigCIW = null;
            if (inventory.rig != null)
            {
                Mod.LogInfo("\tSpawning rig: "+inventory.rig);
                GameObject rigObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.rig)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;
                
                rigCIW = rigObject.GetComponent<CustomItemWrapper>();
                rigCIW.foundInRaid = true;

                if (!player)
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, rigCIW, null);
                }

                for (int slotIndex = 0; slotIndex < inventory.rigContents.Length; ++slotIndex)
                {
                    if(inventory.rigContents[slotIndex] == null)
                    {
                        continue;
                    }

                    string itemID = inventory.rigContents[slotIndex];
                    Mod.LogInfo("\t\tSpawning rig content "+slotIndex+": " + itemID);
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
                        CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                        FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                        itemCIW.foundInRaid = true;

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
                                CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                itemCIW.foundInRaid = true;

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

                                CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                itemCIW.foundInRaid = true;

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
                            itemObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
                        }
                    }

                    itemObject.SetActive(false);
                    rigCIW.itemsInSlots[slotIndex] = itemObject;

                    yield return null;
                }

                rigCIW.UpdateRigMode();

                rigSlotSizes = new FVRPhysicalObject.FVRPhysicalObjectSize[rigCIW.rigSlots.Count];
                for (int j = 0; j < rigCIW.rigSlots.Count; ++j)
                {
                    rigSlotSizes[j] = rigCIW.rigSlots[j].SizeLimit;
                }
            }

            // Spawn armor
            if (inventory.armor != null)
            {
                Mod.LogInfo("\tSpawning armor: " + inventory.armor);
                GameObject backpackObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.armor)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                CustomItemWrapper armorCIW = backpackObject.GetComponent<CustomItemWrapper>();
                armorCIW.foundInRaid = true;

                if (player)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[1];
                    FVRPhysicalObject armorPhysObj = backpackObject.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(backpackObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, armorCIW, null);
                    armorPhysObj.SetQuickBeltSlot(equipSlot);
                    armorPhysObj.SetParentage(null);

                    backpackObject.SetActive(Mod.playerStatusManager.displayed);
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, armorCIW, null);
                }
            }

            // Spawn head wear
            if (inventory.headWear != null)
            {
                Mod.LogInfo("\tSpawning headwear: " + inventory.headWear);
                GameObject headWearObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.headWear)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                CustomItemWrapper headWearCIW = headWearObject.GetComponent<CustomItemWrapper>();
                headWearCIW.foundInRaid = true;

                if (player)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[3];
                    FVRPhysicalObject headWearPhysObj = headWearObject.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(headWearObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, headWearCIW, null);
                    headWearPhysObj.SetQuickBeltSlot(equipSlot);
                    headWearPhysObj.SetParentage(null);

                    headWearObject.SetActive(Mod.playerStatusManager.displayed);
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, headWearCIW, null);
                }
            }

            // Spawn ear piece
            if (inventory.earPiece != null)
            {
                Mod.LogInfo("\tSpawning earPiece: " + inventory.earPiece);
                GameObject earPieceObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.earPiece)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                CustomItemWrapper earPieceCIW = earPieceObject.GetComponent<CustomItemWrapper>();
                earPieceCIW.foundInRaid = true;

                if (player)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[2];
                    FVRPhysicalObject earPiecePhysObj = earPieceObject.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(earPieceObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, earPieceCIW, null);
                    earPiecePhysObj.SetQuickBeltSlot(equipSlot);
                    earPiecePhysObj.SetParentage(null);

                    earPieceObject.SetActive(Mod.playerStatusManager.displayed);
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, earPieceCIW, null);
                }
            }

            // Spawn face cover
            if (inventory.faceCover != null)
            {
                Mod.LogInfo("\tSpawning faceCover: " + inventory.faceCover);
                GameObject faceCoverObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.faceCover)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                CustomItemWrapper faceCoverCIW = faceCoverObject.GetComponent<CustomItemWrapper>();
                faceCoverCIW.foundInRaid = true;

                if (player)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[4];
                    FVRPhysicalObject faceCoverPhysObj = faceCoverObject.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(faceCoverObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, faceCoverCIW, null);
                    faceCoverPhysObj.SetQuickBeltSlot(equipSlot);
                    faceCoverPhysObj.SetParentage(null);

                    faceCoverObject.SetActive(Mod.playerStatusManager.displayed);
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, faceCoverCIW, null);
                }
            }

            // Spawn eye wear
            if (inventory.eyeWear != null)
            {
                Mod.LogInfo("\tSpawning eyeWear: " + inventory.eyeWear);
                GameObject eyeWearObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.eyeWear)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                CustomItemWrapper eyeWearCIW = eyeWearObject.GetComponent<CustomItemWrapper>();
                eyeWearCIW.foundInRaid = true;

                if (player)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[5];
                    FVRPhysicalObject eyeWearPhysObj = eyeWearObject.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(eyeWearObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, eyeWearCIW, null);
                    eyeWearPhysObj.SetQuickBeltSlot(equipSlot);
                    eyeWearPhysObj.SetParentage(null);

                    eyeWearObject.SetActive(Mod.playerStatusManager.displayed);
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, eyeWearCIW, null);
                }
            }

            // Spawn dogtags
            if (inventory.dogtag != null)
            {
                Mod.LogInfo("\tSpawning dogtag: " + inventory.dogtag);
                GameObject dogtagObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.dogtag)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                CustomItemWrapper dogtagCIW = dogtagObject.GetComponent<CustomItemWrapper>();
                dogtagCIW.dogtagLevel = inventory.dogtagLevel;
                dogtagCIW.dogtagName = inventory.dogtagName;
                dogtagCIW.foundInRaid = true;

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(null, dogtagCIW, null);

                yield return null;
            }

            // Spawn backpack
            CustomItemWrapper backpackCIW = null;
            if (inventory.backpack != null)
            {
                Mod.LogInfo("\tSpawning backpack: " + inventory.backpack);
                GameObject backpackObject = Instantiate(Mod.itemPrefabs[int.Parse(inventory.backpack)], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                backpackCIW = backpackObject.GetComponent<CustomItemWrapper>();
                backpackCIW.foundInRaid = true;

                if (!player)
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, backpackCIW, null);
                }

                foreach (string backpackItem in inventory.backpackContents)
                {
                    Mod.LogInfo("\t\tSpawning backpack content " + backpackItem);
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
                        CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                        itemPhysObj = itemObject.GetComponent<FVRPhysicalObject>();
                        itemCIW.foundInRaid = true;

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
                                CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                itemCIW.foundInRaid = true;

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

                                CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                                itemCIW.foundInRaid = true;

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
                            itemObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
                        }
                    }

                    itemObject.SetActive(false);
                    
                    // Set item in the container, at random pos and rot, if volume fits
                    bool boxMainContainer = backpackCIW.mainContainer.GetComponent<BoxCollider>() != null;
                    if (backpackCIW.AddItemToContainer(itemPhysObj))
                    {
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

                backpackCIW.UpdateBackpackMode();
            }

            // Spawn primary weapon
            if (inventory.primaryWeapon != null)
            {
                Mod.LogInfo("\tSpawning primaryWeapon: " + inventory.primaryWeapon);
                yield return IM.OD[inventory.primaryWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.primaryWeapon].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                weaponObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if(weaponFireArm == null)
                {
                    Mod.LogWarning("Sosig primary weapon not a firearm");
                    yield break;
                }

                if (weaponFireArm is ClosedBoltWeapon)
                {
                    (weaponFireArm as ClosedBoltWeapon).CockHammer();
                }
                else if (weaponFireArm is BoltActionRifle)
                {
                    (weaponFireArm as BoltActionRifle).CockHammer();
                }
                else if (weaponFireArm is TubeFedShotgun)
                {
                    (weaponFireArm as TubeFedShotgun).CockHammer();
                }
                else if (weaponFireArm is BreakActionWeapon)
                {
                    (weaponFireArm as BreakActionWeapon).CockHammer();
                }
                // TODO: Might also have to set private fields in OpenBolt, LeverAction, etc

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<VanillaItemDescriptor>());

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
                        Mod.LogInfo("\t\tSpawning primaryWeapon mod: " + modID);
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
                            VanillaItemDescriptor magVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            magVID.foundInRaid = true;
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
                            VanillaItemDescriptor clipVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            clipVID.foundInRaid = true;
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
                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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

                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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
                            attachmentObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
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
                                Mod.LogWarning("Could not find compatible mount for mod: "+ modID + " on: "+currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.LogError("Unhandle weapon mod type for mod with ID: "+ modID);
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

                if (player)
                {
                    VanillaItemDescriptor VID = weaponObject.GetComponent<VanillaItemDescriptor>();

                    // TODO: Should use main hand here instead of right
                    FVRViveHand hand = Mod.rightHand.fvrHand;
                    FVRPhysicalObject weaponPhysObj = weaponObject.GetComponent<FVRPhysicalObject>();
                    hand.CurrentInteractable = weaponPhysObj;
                    FieldInfo handStateField = typeof(FVRViveHand).GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);
                    handStateField.SetValue(hand, FVRViveHand.HandState.GripInteracting);
                    // Must set location index before beginning interaction because begin interactionpatch will consider this to be in raid and will try to remove it from it
                    // but it isnt in there yet
                    VID.takeCurrentLocation = false;
                    VID.locationIndex = 0;
                    hand.CurrentInteractable.BeginInteraction(hand);
                }
            }

            // Spawn secondary weapon
            if (inventory.secondaryWeapon != null)
            {
                Mod.LogInfo("\tSpawning secondaryWeapon: " + inventory.secondaryWeapon);
                yield return IM.OD[inventory.secondaryWeapon].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.secondaryWeapon].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                weaponObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if(weaponFireArm == null)
                {
                    Mod.LogWarning("Sosig secondary weapon not a firearm");
                    yield break;
                }

                if (weaponFireArm is ClosedBoltWeapon)
                {
                    (weaponFireArm as ClosedBoltWeapon).CockHammer();
                }
                else if (weaponFireArm is BoltActionRifle)
                {
                    (weaponFireArm as BoltActionRifle).CockHammer();
                }
                else if (weaponFireArm is TubeFedShotgun)
                {
                    (weaponFireArm as TubeFedShotgun).CockHammer();
                }
                else if (weaponFireArm is BreakActionWeapon)
                {
                    (weaponFireArm as BreakActionWeapon).CockHammer();
                }
                // TODO: Might also have to set private fields in OpenBolt, LeverAction, etc

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<VanillaItemDescriptor>());

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
                        Mod.LogInfo("\t\tSpawning secondaryWeapon mod: " + modID);
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
                            VanillaItemDescriptor magVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            magVID.foundInRaid = true;
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
                            VanillaItemDescriptor clipVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            clipVID.foundInRaid = true;
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
                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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

                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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
                            attachmentObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
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
                                Mod.LogWarning("Could not find compatible mount for mod: " + modID + " on: " + currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.LogError("Unhandle weapon mod type for mod with ID: "+ modID);
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

                if (player)
                {
                    weaponFireArm.SetQuickBeltSlot(Mod.rightShoulderSlot);

                    VanillaItemDescriptor VID = weaponObject.GetComponent<VanillaItemDescriptor>();
                    Mod.AddToPlayerInventory(weaponObject.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, null, VID);

                    Mod.rightShoulderObject = weaponObject;
                    weaponObject.SetActive(false);
                }
            }

            // Spawn holster weapon
            if (inventory.holster != null)
            {
                Mod.LogInfo("\tSpawning holster: " + inventory.holster);
                yield return IM.OD[inventory.holster].GetGameObjectAsync();
                GameObject weaponObject = Instantiate(IM.OD[inventory.holster].GetGameObject(), pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));

                yield return null;

                weaponObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;

                // FireArmAttachment are attached to FVRFireArmAttachmentMount using attachment.AttachToMount
                FVRFireArm weaponFireArm = weaponObject.GetComponent<FVRFireArm>();
                if (weaponFireArm == null)
                {
                    Mod.LogWarning("Sosig holster weapon not a firearm");
                    yield break;
                }

                if (weaponFireArm is ClosedBoltWeapon)
                {
                    (weaponFireArm as ClosedBoltWeapon).CockHammer();
                }
                else if (weaponFireArm is BoltActionRifle)
                {
                    (weaponFireArm as BoltActionRifle).CockHammer();
                }
                else if (weaponFireArm is TubeFedShotgun)
                {
                    (weaponFireArm as TubeFedShotgun).CockHammer();
                }
                else if (weaponFireArm is BreakActionWeapon)
                {
                    (weaponFireArm as BreakActionWeapon).CockHammer();
                }
                // TODO: Might also have to set private fields in OpenBolt, LeverAction, etc

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(weaponFireArm, null, weaponObject.GetComponent<VanillaItemDescriptor>());

                AIInventoryWeaponMod currentParent = inventory.holsterMods;
                Stack<FVRPhysicalObject> parentPhysObjs = new Stack<FVRPhysicalObject>();
                parentPhysObjs.Push(weaponFireArm);
                Stack<int> childIndices = new Stack<int>();
                childIndices.Push(0);
                while (currentParent != null)
                {
                    if (currentParent.children != null && childIndices.Peek() < currentParent.children.Count)
                    {
                        string modID = currentParent.children[childIndices.Peek()].ID;
                        Mod.LogInfo("\t\tSpawning holster mod: " + modID);
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
                            VanillaItemDescriptor magVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            magVID.foundInRaid = true;
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
                            VanillaItemDescriptor clipVID = attachmentObject.GetComponent<VanillaItemDescriptor>();
                            clipVID.foundInRaid = true;
                            clipVID.takeCurrentLocation = false;
                            FVRFireArmClip attachmentClip = attachmentObject.GetComponent<FVRFireArmClip>();
                            attachmentPhysObj = attachmentClip;
                            FireArmLoadClipPatch.ignoreLoadClip = true;
                            attachmentClip.Load(weaponFireArm);
                        }
                        else if (attachmentPrefabPhysObj is FVRFireArmRound)
                        {
                            FVRFireArmRound asRound = attachmentPrefabPhysObj as FVRFireArmRound;

                            // Fill chamber(s) then fill mag/clip as necessary
                            int chamberCount = weaponFireArm.GetChambers().Count;
                            List<FireArmRoundClass> chamberRounds = new List<FireArmRoundClass>();
                            for (int i = 0; i < chamberCount; ++i)
                            {
                                chamberRounds.Add(asRound.RoundClass);
                            }
                            weaponFireArm.SetLoadedChambers(chamberRounds);

                            if (weaponFireArm.UsesMagazines)
                            {
                                if (weaponFireArm.Magazine != null)
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
                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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

                                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                                    itemCIW.foundInRaid = true;
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
                        else if (attachmentPrefabPhysObj is FVRFireArmAttachment)
                        {
                            attachmentObject = Instantiate(attachmentPrefab);
                            attachmentObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
                            FVRFireArmAttachment asAttachment = attachmentObject.GetComponent<FVRFireArmAttachment>();
                            attachmentPhysObj = asAttachment; ;
                            bool mountFound = false;
                            foreach (FVRFireArmAttachmentMount mount in parentPhysObjs.Peek().AttachmentMounts)
                            {
                                if (mount.Type == asAttachment.Type)
                                {
                                    mountFound = true;
                                    asAttachment.AttachToMount(mount, false);
                                    break;
                                }
                            }
                            if (!mountFound)
                            {
                                Mod.LogWarning("Could not find compatible mount for mod: " + modID + " on: " + currentParent.ID);
                                attachmentObject.transform.position = parentPhysObjs.Peek().transform.position + Vector3.up;
                            }
                        }
                        else
                        {
                            Mod.LogError("Unhandle weapon mod type for mod with ID: " + modID);
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

                if (inventory.rig != null)
                {
                    // Put in first medium slot
                    for (int i = 0; i < rigSlotSizes.Length; ++i)
                    {
                        if (rigSlotSizes[i] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
                        {
                            rigCIW.itemsInSlots[i] = weaponObject;
                            weaponObject.SetActive(false);

                            break;
                        }
                    }
                }
            }

            // Spawn generic items
            Mod.LogInfo("\tSpawning generic items");
            for(int i = 0; i<inventory.generic.Count;++i)
            {
                string genericItem = inventory.generic[i];
                bool custom = false;
                GameObject itemPrefab = null;
                int parsedID = -1;
                Mod.LogInfo("\t\t" + genericItem);
                if (int.TryParse(genericItem, out parsedID))
                {
                    Mod.LogInfo("\t\tparsed, is custom");
                    itemPrefab = Mod.itemPrefabs[parsedID];
                    custom = true;
                }
                else
                {
                    Mod.LogInfo("\t\tnot parsed, is vanilla");
                    yield return IM.OD[genericItem].GetGameObjectAsync();
                    itemPrefab = IM.OD[genericItem].GetGameObject();
                    if (itemPrefab == null)
                    {
                        Mod.LogWarning("Attempted to get vanilla prefab for " + genericItem + ", but the prefab had been destroyed, refreshing cache...");

                        IM.OD[genericItem].RefreshCache();
                        itemPrefab = IM.OD[genericItem].GetGameObject();
                    }
                    if (itemPrefab == null)
                    {
                        Mod.LogError("Attempted to get vanilla prefab for " + genericItem + ", but the prefab had been destroyed, refreshing cache did nothing");
                        continue;
                    }
                }

                GameObject itemObject = null;
                if (custom)
                {
                    Mod.LogInfo("\t\tis custom");
                    itemObject = Instantiate(itemPrefab, pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                    Mod.LogInfo("\t\tinstantiated");
                    CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                    itemCIW.foundInRaid = true;
                    Mod.LogInfo("\t\tgot CIW");
                    FVRPhysicalObject itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();
                    Mod.LogInfo("\t\tgot physobj");

                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(null, itemCIW, null);

                    // Get amount
                    if (itemCIW.itemType == Mod.ItemType.Money)
                    {
                        Mod.LogInfo("\t\t\tis money");
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
                        Mod.LogInfo("\t\t\tis ammobox");
                        FVRFireArmMagazine asMagazine = itemPhysObj as FVRFireArmMagazine;
                        for (int j = 0; j < itemCIW.maxAmount; ++j)
                        {
                            asMagazine.AddRound(itemCIW.roundClass, false, false);
                        }
                    }
                    else if (itemCIW.maxAmount > 0)
                    {
                        Mod.LogInfo("\t\t\thas amount");
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
                            itemObject = Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[genericItem]], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                            itemCIW.foundInRaid = true;
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
                            itemObject = null;
                            if (amount > 30)
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[716], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            }
                            else
                            {
                                itemObject = Instantiate(Mod.itemPrefabs[715], pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                            }

                            CustomItemWrapper itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                            itemCIW.foundInRaid = true;
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
                        itemObject = Instantiate(itemPrefab, pos + new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)), UnityEngine.Random.rotation, transform.GetChild(1).GetChild(1).GetChild(2));
                        itemObject.GetComponent<VanillaItemDescriptor>().foundInRaid = true;
                    }
                }

                bool success = false;
                int pocketsUsed = inventory.genericPockets.Count;
                FVRPhysicalObject finalItemPhysObj = itemObject.GetComponent<FVRPhysicalObject>();

                if (inventory.genericPockets.Contains(i))
                {
                    for(int j=0; j < 4; ++j)
                    {
                        if(GM.CurrentPlayerBody.QBSlots_Internal[j].CurObject == null)
                        {
                            Mod.AddToPlayerInventory(finalItemPhysObj.transform, false);
                            BeginInteractionPatch.SetItemLocationIndex(0, itemObject.GetComponent<CustomItemWrapper>(), itemObject.GetComponent<VanillaItemDescriptor>());
                            finalItemPhysObj.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[j]);
                            finalItemPhysObj.SetParentage(null);
                            success = true;
                        }
                    }
                }
                if (!success && finalItemPhysObj.Size == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                {
                    for (int j = 0; j < 4; ++j)
                    {
                        if (GM.CurrentPlayerBody.QBSlots_Internal[j].CurObject == null)
                        {
                            Mod.AddToPlayerInventory(finalItemPhysObj.transform, false);
                            BeginInteractionPatch.SetItemLocationIndex(0, itemObject.GetComponent<CustomItemWrapper>(), itemObject.GetComponent<VanillaItemDescriptor>());
                            finalItemPhysObj.SetQuickBeltSlot(GM.CurrentPlayerBody.QBSlots_Internal[j]);
                            finalItemPhysObj.SetParentage(null);
                            success = true;
                            ++pocketsUsed;
                        }
                    }
                }
                if (!success)
                {
                    if (backpackCIW.AddItemToContainer(finalItemPhysObj))
                    {
                        bool boxMainContainer = backpackCIW.mainContainer.GetComponent<BoxCollider>() != null;
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
                        Mod.AddToPlayerInventory(finalItemPhysObj.transform, false);
                        BeginInteractionPatch.SetItemLocationIndex(0, itemObject.GetComponent<CustomItemWrapper>(), itemObject.GetComponent<VanillaItemDescriptor>());
                        itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
                        success = true;
                    }
                }
                if (!success && rigCIW != null)
                {
                    for (int j = 0; j < rigCIW.itemsInSlots.Length; ++j)
                    {
                        if (rigCIW.itemsInSlots[j] == null && (int)rigSlotSizes[j] >= (int)finalItemPhysObj.Size)
                        {
                            rigCIW.itemsInSlots[j] = itemObject;
                            break;
                        }
                    }
                }

                yield return null;
            }

            // Equip rig/backpack after everything if player, since by then all items that need to be added to it have been
            if (player)
            {
                // Equip rig
                if (rigCIW != null)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[6];
                    FVRPhysicalObject rigPhysObj = rigCIW.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(rigCIW.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, rigCIW, null);
                    rigPhysObj.SetQuickBeltSlot(equipSlot);
                    rigPhysObj.SetParentage(null);

                    rigCIW.gameObject.SetActive(Mod.playerStatusManager.displayed);
                }

                // Equip backpack, this is done after we put items inside because if we put it on player and THEN spawn items inside,
                // the backpack will be scaled down because is it in equip slot, so when we take it out of there and open it
                // items will be giant because the whole things gets scaled back up
                if(backpackCIW != null)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[0];
                    FVRPhysicalObject backpackPhysObj = backpackCIW.GetComponent<FVRPhysicalObject>();
                    Mod.AddToPlayerInventory(backpackCIW.transform, false);
                    BeginInteractionPatch.SetItemLocationIndex(0, backpackCIW, null);
                    backpackPhysObj.SetQuickBeltSlot(equipSlot);
                    backpackPhysObj.SetParentage(null);

                    Mod.leftShoulderObject = backpackPhysObj.gameObject;

                    backpackCIW.gameObject.SetActive(Mod.playerStatusManager.displayed);
                }
            }

            yield break;
        }

        private List<Transform> GetMostAvailableBotZones()
        {
            // Compile list of least filled spawn points, take a random one from the list
            Transform raiderZoneRoot = transform.GetChild(transform.childCount - 1).GetChild(1);
            List<Transform> availableBotZones = new List<Transform>();
            List<Transform> notIdealBotZones = new List<Transform>();
            List<Transform> idealBotZones = new List<Transform>();
            int least = int.MaxValue;
            for (int i = 0; i < raiderZoneRoot.childCount; ++i)
            {
                BotZone botZoneScript = raiderZoneRoot.GetChild(i).GetComponent<BotZone>();
                if(botZoneScript.botCount < least)
                {
                    least = botZoneScript.botCount;
                    availableBotZones.Clear();
                    availableBotZones.Add(botZoneScript.transform);
                    Vector3 zonePosition = botZoneScript.transform.GetChild(0).position;
                    float distanceToPlayerHead = Vector3.Distance(zonePosition, GM.CurrentPlayerBody.headPositionFiltered);
                    Vector3 vectorToPlayerHead = (GM.CurrentPlayerBody.headPositionFiltered - zonePosition).normalized;
                    bool outOfRange = distanceToPlayerHead >= 20;
                    bool noLineOfSight = !Physics.Raycast(zonePosition + Vector3.up * 0.15f, vectorToPlayerHead, distanceToPlayerHead, 524288); // Check only if collide with environment
                    if(outOfRange && noLineOfSight)
                    {
                        idealBotZones.Add(botZoneScript.transform);
                    }
                    else if(outOfRange || noLineOfSight)
                    {
                        notIdealBotZones.Add(botZoneScript.transform);
                    }
                }
                else if(botZoneScript.botCount == least)
                {
                    availableBotZones.Add(botZoneScript.transform);
                    Vector3 zonePosition = botZoneScript.transform.GetChild(0).position;
                    float distanceToPlayerHead = Vector3.Distance(zonePosition, GM.CurrentPlayerBody.headPositionFiltered);
                    Vector3 vectorToPlayerHead = (GM.CurrentPlayerBody.headPositionFiltered - zonePosition).normalized;
                    bool outOfRange = distanceToPlayerHead >= 20;
                    bool noLineOfSight = !Physics.Raycast(zonePosition + Vector3.up * 0.15f, vectorToPlayerHead, distanceToPlayerHead, 524288);
                    if (outOfRange && noLineOfSight)
                    {
                        idealBotZones.Add(botZoneScript.transform);
                    }
                    else if (outOfRange || noLineOfSight)
                    {
                        notIdealBotZones.Add(botZoneScript.transform);
                    }
                }
            }
            if(idealBotZones.Count > 0)
            {
                return idealBotZones;
            }
            else if(notIdealBotZones.Count > 0)
            {
                return notIdealBotZones;
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
            Mod.spawnedFirstAI = false;

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
                zone.gameObject.AddComponent<BotZone>();
            }

            // Subscribe to events
            GM.CurrentSceneSettings.SosigKillEvent += this.OnBotKill;

            // Get location's base data
            float averageLevel = (float)locationData["AveragePlayerLevel"];
            maxRaidTime = (float)locationData["EscapeTimeLimit"] * 60;
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
            friendlyAI = new List<Sosig>();
            entities = new List<AIEntity>();
            entities.Add(GM.CurrentPlayerBody.Hitboxes[0].MyE); // Add player as first entity
            entityRelatedAI = new List<AI>();
            entityRelatedAI.Add(null); // Place holder for player entity

            // Init AI Cover points
            //Transform coverPointsParent = transform.GetChild(1).GetChild(1).GetChild(3).GetChild(0);
            //GM.CurrentAIManager.CPM = gameObject.AddComponent<AICoverPointManager>();
            //GM.CurrentAIManager.CPM.MyCoverPoints = new List<AICoverPoint>();
            //for (int i = 0; i < coverPointsParent.childCount; ++i)
            //{
            //    AICoverPoint newCoverPoint = coverPointsParent.GetChild(i).gameObject.AddComponent<AICoverPoint>();
            //    GM.CurrentAIManager.CPM.MyCoverPoints.Add(newCoverPoint);
            //}


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
                        Mod.LogError("Unexpected scav spawn type: " + spawnType);
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

                int escortSize = wave["BossEscortAmount"].Type == JTokenType.Array ? (int)wave["BossEscortAmount"][UnityEngine.Random.Range(0, (wave["BossEscortAmount"] as JArray).Count)] + 1 : int.Parse(wave["BossEscortAmount"].ToString());
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
            }

            AISpawn newAISpawn = new AISpawn();
            newAISpawn.type = AIType;
            newAISpawn.experienceReward = UnityEngine.Random.Range((int)botDataToUse.experience["reward"]["min"], (int)botDataToUse.experience["reward"]["max"]);
            newAISpawn.inventory = new AIInventory();
            newAISpawn.inventory.generic = new List<string>();
            newAISpawn.inventory.genericPockets = new List<int>();
            newAISpawn.outfitByLink = new Dictionary<int, List<string>>();

            float level = Mathf.Min(80, ExpDistrRandOnAvg(averageLevel));

            newAISpawn.name = botDataToUse.names[UnityEngine.Random.Range(0, botDataToUse.names.Count)].ToString();

            if (AIType == AISpawn.AISpawnType.PMC)
            {
                if (USEC)
                {
                    newAISpawn.inventory.dogtag = "12";
                    newAISpawn.USEC = true;
                }
                else
                {
                    newAISpawn.inventory.dogtag = "11";
                }
                newAISpawn.inventory.dogtagLevel = (int)level;
                newAISpawn.inventory.dogtagName = newAISpawn.name;
            }
            else if(AIType == AISpawn.AISpawnType.Boss || AIType == AISpawn.AISpawnType.Follower || AIType == AISpawn.AISpawnType.Raider)
            {
                newAISpawn.leaderName = leaderName;
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

            // Set equipment
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

                if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"][actualEquipName]) / 100)
                {
                    JArray possibleHeadEquip = inventoryDataToUse["equipment"][actualEquipName] as JArray;
                    if (possibleHeadEquip.Count > 0)
                    {
                        string headEquipID = possibleHeadEquip[UnityEngine.Random.Range(0, possibleHeadEquip.Count)].ToString();
                        if (Mod.itemMap.ContainsKey(headEquipID))
                        {
                            string actualHeadEquipID = null;
                            ItemMapEntry itemMapEntry = Mod.itemMap[headEquipID];
                            switch (itemMapEntry.mode)
                            {
                                case 0:
                                    actualHeadEquipID = itemMapEntry.ID;
                                    break;
                                case 1:
                                    actualHeadEquipID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                    break;
                                case 2:
                                    actualHeadEquipID = itemMapEntry.otherModID;
                                    break;
                            }

                            // Add sosig outfit item if applicable
                            if (Mod.globalDB["AIItemMap"][actualHeadEquipID] != null)
                            {
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

                            Mod.ItemType itemType = (Mod.ItemType)(int)Mod.defaultItemsData["ItemDefaults"][parsedEquipID]["ItemType"];
                            switch (itemType)
                            {
                                case Mod.ItemType.Helmet:
                                case Mod.ItemType.Headwear:
                                    newAISpawn.inventory.headWear = actualHeadEquipID;
                                    break;
                                case Mod.ItemType.Earpiece:
                                    newAISpawn.inventory.earPiece = actualHeadEquipID;
                                    break;
                                case Mod.ItemType.FaceCover:
                                    newAISpawn.inventory.faceCover = actualHeadEquipID;
                                    break;
                                case Mod.ItemType.Eyewear:
                                    newAISpawn.inventory.eyeWear = actualHeadEquipID;
                                    break;
                            }
                        }
                        else
                        {
                            Mod.LogError("Missing item: " + headEquipID + " for PMC AI spawn " + actualEquipName);
                        }
                    }
                }
            }

            Mod.LogInfo("\tSetting tact vest");
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
                        string actualRigID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[rigID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualRigID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualRigID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualRigID = itemMapEntry.otherModID;
                                break;
                        }
                        Mod.LogInfo("\t\tChosen ID: " + actualRigID);
                        newAISpawn.inventory.rig = actualRigID;

                        // Add sosig outfit item if applicable
                        if (Mod.globalDB["AIItemMap"][actualRigID] != null)
                        {
                            Mod.LogInfo("\t\t\tGot in AIItemMap");
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
                            Mod.LogInfo("\t\t\tAdded to outfitByLink");
                        }

                        // Get prefab of the rig to get the sizes and number of slots
                        // Keep sizes in an array here to use to decide where to spawn what item when we set those
                        // Set number of slots by initializing aispawn.inventory.rigcontents array with the correct length
                        // also keep the whitelist and blacklist from default item data so we can decide if can actually spawn items in this rig later on
                        // also based on whether this is an itemtype 2 (rig) or 3 (armored rig), set a bool to tell whether we can spawn a equipment of type ArmorVest after this
                        int parsedEquipID = int.Parse(actualRigID);
                        GameObject rigPrefab = Mod.itemPrefabs[parsedEquipID];
                        CustomItemWrapper rigCIW = rigPrefab.GetComponent<CustomItemWrapper>();
                        rigSlotSizes = new FVRPhysicalObject.FVRPhysicalObjectSize[rigCIW.rigSlots.Count];
                        for (int j = 0; j < rigCIW.rigSlots.Count; ++j)
                        {
                            rigSlotSizes[j] = rigCIW.rigSlots[j].SizeLimit;
                        }
                        newAISpawn.inventory.rigContents = new string[rigCIW.rigSlots.Count];
                        rigWhitelist = rigCIW.whiteList;
                        rigBlacklist = rigCIW.blackList;
                        rigArmored = rigCIW.itemType == Mod.ItemType.ArmoredRig;
                        Mod.LogInfo("\t\tProcessed data from rig CIW");
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + rigID + " for PMC AI spawn Rig");
                    }
                }
            }

            if (!rigArmored)
            {
                if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["ArmorVest"]) / 100)
                {
                    JArray possibleArmors = inventoryDataToUse["equipment"]["ArmorVest"] as JArray;
                    if (possibleArmors.Count > 0)
                    {
                        string armorID = possibleArmors[UnityEngine.Random.Range(0, possibleArmors.Count)].ToString();
                        if (Mod.itemMap.ContainsKey(armorID))
                        {
                            string actualArmorID = null;
                            ItemMapEntry itemMapEntry = Mod.itemMap[armorID];
                            switch (itemMapEntry.mode)
                            {
                                case 0:
                                    actualArmorID = itemMapEntry.ID;
                                    break;
                                case 1:
                                    actualArmorID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                    break;
                                case 2:
                                    actualArmorID = itemMapEntry.otherModID;
                                    break;
                            }
                            newAISpawn.inventory.armor = actualArmorID;

                            // Add sosig outfit item if applicable
                            if (Mod.globalDB["AIItemMap"][actualArmorID] != null)
                            {
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
                            }
                        }
                        else
                        {
                            Mod.LogError("Missing item: " + armorID + " for PMC AI spawn Armor");
                        }
                    }
                }
            }

            Mod.LogInfo("\tSetting backpack");
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
                        string actualBackpackID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[backpackID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualBackpackID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualBackpackID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualBackpackID = itemMapEntry.otherModID;
                                break;
                        }
                        newAISpawn.inventory.backpack = actualBackpackID;

                        // Add sosig outfit item if applicable
                        if (Mod.globalDB["AIItemMap"][actualBackpackID] != null)
                        {
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
                        }

                        // Get backpack data
                        int parsedEquipID = int.Parse(actualBackpackID);
                        JObject defaultBackpackData = Mod.defaultItemsData["ItemDefaults"][parsedEquipID] as JObject;
                        backpackWhitelist = defaultBackpackData["ContainerProperties"]["WhiteList"].ToObject<List<string>>();
                        backpackBlacklist = defaultBackpackData["ContainerProperties"]["BlackList"].ToObject<List<string>>();
                        maxBackpackVolume = (float)defaultBackpackData["BackpackProperties"]["MaxVolume"];
                        newAISpawn.inventory.backpackContents = new List<string>();
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + backpackID + " for PMC AI spawn Rig");
                    }
                }
            }

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
                        string actualPWID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[PWID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualPWID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualPWID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualPWID = itemMapEntry.otherModID;
                                break;
                        }
                        newAISpawn.inventory.primaryWeapon = actualPWID;

                        // Add sosig weapon
                        if (Mod.globalDB["AIWeaponMap"][actualPWID] != null)
                        {
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
                            Mod.LogError("AI FirstPrimaryWeapon with ID: "+ actualPWID +" is not a firearm");
                        }
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + PWID + " for PMC AI spawn Primary weapon");
                    }
                }
            }

            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["SecondPrimaryWeapon"]) / 100)
            {
                JArray possibleSW = inventoryDataToUse["equipment"]["SecondPrimaryWeapon"] as JArray;
                if (possibleSW.Count > 0)
                {
                    string SWID = possibleSW[UnityEngine.Random.Range(0, possibleSW.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(SWID))
                    {
                        string actualSWID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[SWID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualSWID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualSWID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualSWID = itemMapEntry.otherModID;
                                break;
                        }
                        newAISpawn.inventory.secondaryWeapon = actualSWID;

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualSWID] != null)
                        {
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
                            Mod.LogError("AI SecondPrimaryWeapon with ID: " + actualSWID + " is not a firearm");
                        }
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + SWID + " for PMC AI spawn Secondary weapon");
                    }
                }
            }

            if (rigSlotSizes != null && UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["Holster"]) / 100)
            {
                JArray possibleHolster = inventoryDataToUse["equipment"]["Holster"] as JArray;
                if (possibleHolster.Count > 0)
                {
                    string holsterID = possibleHolster[UnityEngine.Random.Range(0, possibleHolster.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(holsterID))
                    {
                        string actualHolsterID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[holsterID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualHolsterID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualHolsterID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualHolsterID = itemMapEntry.otherModID;
                                break;
                        }
                        newAISpawn.inventory.holster = actualHolsterID;

                        //// Set to holster slot in rig
                        //for (int i = 0; i < rigSlotSizes.Length; ++i)
                        //{
                        //    if (newAISpawn.inventory.rigContents[i] == null && rigSlotSizes[i] >= FVRPhysicalObject.FVRPhysicalObjectSize.Medium)
                        //    {
                        //        newAISpawn.inventory.rigContents[i] = actualHolsterID;
                        //        break;
                        //    }
                        //}

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualHolsterID] != null)
                        {
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
                            Mod.LogError("AI Holster with ID: " + actualHolsterID + " is not a firearm");
                        }
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + holsterID + " for PMC AI spawn Holster");
                    }
                }
            }

            if (UnityEngine.Random.value <= ((float)botDataToUse.chances["equipment"]["Scabbard"]) / 100)
            {
                JArray possibleScabbard = inventoryDataToUse["equipment"]["Scabbard"] as JArray;
                if (possibleScabbard.Count > 0)
                {
                    string scabbardID = possibleScabbard[UnityEngine.Random.Range(0, possibleScabbard.Count)].ToString();
                    if (Mod.itemMap.ContainsKey(scabbardID))
                    {
                        string actualScabbardID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[scabbardID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualScabbardID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualScabbardID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualScabbardID = itemMapEntry.otherModID;
                                break;
                        }
                        newAISpawn.inventory.generic.Add(actualScabbardID);

                        // Add sosig weapon if necessary
                        if (!hasSosigWeapon && Mod.globalDB["AIWeaponMap"][actualScabbardID] != null)
                        {
                            newAISpawn.sosigWeapon = Mod.globalDB["AIWeaponMap"][actualScabbardID].ToString();
                            hasSosigWeapon = true;
                        }
                    }
                    else
                    {
                        Mod.LogError("Missing item: " + scabbardID + " for PMC AI spawn Scabbard");
                    }
                }
            }

            if (!hasSosigWeapon)
            {
                Mod.LogError(AIType.ToString() + " typr AI with name: " + newAISpawn.name+", has no weapon");
            }

            // Set items depending on generation limits
            int pocketsUsed = 0;
            float currentBackpackVolume = 0;

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

                    for (int i = 0; i < ammoContainerItemMax; ++i)
                    {
                        string ammoContainerItemID = ammoContainers[k];
                        object[] ammoContainerItemData = GetItemData(ammoContainerItemID);

                        if (i >= ammoContainerItemMin && UnityEngine.Random.value > float.Parse(ammoContainerItemData[2] as string))
                        {
                            continue;
                        }

                        FVRPhysicalObject.FVRPhysicalObjectSize ammoContainerItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(ammoContainerItemData[0] as string);
                        float ammoContainerItemVolume = float.Parse(ammoContainerItemData[1] as string);

                        // First try to put in rig, then pockets, then backpack
                        bool success = false;
                        if (rigSlotSizes != null)
                        {
                            bool firstMediumSkipped = false;
                            for (int j = 0; j < rigSlotSizes.Length; ++j)
                            {
                                if (rigSlotSizes[j] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium && !firstMediumSkipped)
                                {
                                    firstMediumSkipped = true;
                                    continue;
                                }
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
                            newAISpawn.inventory.genericPockets.Add(newAISpawn.inventory.generic.Count);
                            newAISpawn.inventory.generic.Add(ammoContainerItemID);
                            ++pocketsUsed;
                            success = true;
                        }
                        if (!success && maxBackpackVolume > 0 && currentBackpackVolume + ammoContainerItemVolume <= maxBackpackVolume)
                        {
                            newAISpawn.inventory.backpackContents.Add(ammoContainerItemID);
                            currentBackpackVolume += ammoContainerItemVolume;
                        }
                    }
                }
            }

            // Fill lists of possible types of items for specific parts
            Dictionary<string, object[]> possibleHealingItems = new Dictionary<string, object[]>(); // 0 - List of part indices, 1 - size, 2 - volume, 3 - spawn chance
            Dictionary<string, object[]> possibleGrenades = new Dictionary<string, object[]>();
            Dictionary<string, object[]> possibleLooseLoot = new Dictionary<string, object[]>();
            string[] itemParts = new string[] { "TacticalVest", "Pockets", "Backpack" };
            for (int i = 0; i < 3; ++i)
            {
                foreach (string itemID in inventoryDataToUse["items"][itemParts[i]])
                {
                    if (Mod.itemMap.ContainsKey(itemID))
                    {
                        string actualItemID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[itemID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualItemID = itemMapEntry.ID;
                                break;
                            case 1:
                                actualItemID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                actualItemID = itemMapEntry.otherModID;
                                break;
                        }
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
                            if (itemPrefab == null)
                            {
                                Mod.LogWarning("Attempted to get vanilla prefab for " + actualItemID + ", but the prefab had been destroyed, refreshing cache...");

                                IM.OD[actualItemID].RefreshCache();
                                itemPrefab = IM.OD[actualItemID].GetGameObject();
                            }
                            if (itemPrefab == null)
                            {
                                Mod.LogError("Attempted to get vanilla prefab for " + actualItemID + ", but the prefab had been destroyed, refreshing cache did nothing");
                                continue;
                            }
                        }
                        List<string> itemParents = null;
                        int itemVolume = 0;
                        float itemSpawnChance = 0;
                        FVRPhysicalObject itemPhysObj = null;
                        if (custom)
                        {
                            CustomItemWrapper itemCIW = itemPrefab.GetComponent<CustomItemWrapper>();
                            if(itemCIW == null)
                            {
                                Mod.LogError("Could spawn Item "+itemID+" in "+ newAISpawn.name + "'s "+itemParts[i]+" but is custom and has no CIW");
                                continue;
                            }
                            itemParents = itemCIW.parents;
                            itemVolume = itemCIW.volumes[0];
                            itemSpawnChance = Mod.GetRaritySpawnChanceMultiplier(itemCIW.rarity);
                        }
                        else
                        {
                            VanillaItemDescriptor itemVID = itemPrefab.GetComponent<VanillaItemDescriptor>();
                            if (itemVID == null)
                            {
                                Mod.LogError("Could spawn Item " + itemID + " in " + newAISpawn.name + "'s " + itemParts[i] + " but is not custom and has no VID");
                                continue;
                            }
                            itemParents = itemVID.parents;
                            itemVolume = Mod.itemVolumes[actualItemID];
                            itemSpawnChance = Mod.GetRaritySpawnChanceMultiplier(itemVID.rarity);
                        }
                        itemPhysObj = itemPrefab.GetComponent<FVRPhysicalObject>();

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

                    if (i >= healingItemMin && UnityEngine.Random.value > float.Parse(healingItemData[3] as string))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize healingItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(healingItemData[1] as string);
                    float healingItemVolume = float.Parse(healingItemData[2] as string);

                    // First try to put in pockets, then backpack, then rig
                    bool success = false;
                    if (possibleParts.Contains(1) && healingItemSize == FVRPhysicalObject.FVRPhysicalObjectSize.Small && pocketsUsed < 4)
                    {
                        newAISpawn.inventory.genericPockets.Add(newAISpawn.inventory.generic.Count);
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
                        bool firstMediumSkipped = false; // Want to skip first medium because it is reserved for holster
                        for(int j=0; j< rigSlotSizes.Length; ++j)
                        {
                            if(rigSlotSizes[j] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium && !firstMediumSkipped)
                            {
                                firstMediumSkipped = true;
                                continue;
                            }
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

                    if (i >= grenadeItemMin && UnityEngine.Random.value > float.Parse(grenadeItemData[3] as string))
                    {
                        continue;
                    }

                    FVRPhysicalObject.FVRPhysicalObjectSize grenadeItemSize = (FVRPhysicalObject.FVRPhysicalObjectSize)int.Parse(grenadeItemData[1] as string);
                    float grenadeItemVolume = float.Parse(grenadeItemData[2] as string);

                    // First try to put in pockets, then backpack, then rig
                    bool success = false;
                    if (possibleParts.Contains(0) && rigSlotSizes != null)
                    {
                        bool firstMediumSkipped = false;
                        for (int j = 0; j < rigSlotSizes.Length; ++j)
                        {
                            if (rigSlotSizes[j] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium && !firstMediumSkipped)
                            {
                                firstMediumSkipped = true;
                                continue;
                            }
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
                        newAISpawn.inventory.genericPockets.Add(newAISpawn.inventory.generic.Count);
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

                    if (i >= looseLootItemMin && UnityEngine.Random.value > float.Parse(looseLootItemData[3] as string))
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
                        newAISpawn.inventory.genericPockets.Add(newAISpawn.inventory.generic.Count);
                        newAISpawn.inventory.generic.Add(looseLootItemID);
                        ++pocketsUsed;
                        success = true;
                    }
                    if (!success && possibleParts.Contains(0) && rigSlotSizes != null)
                    {
                        bool firstMediumSkipped = false;
                        for (int j=0; j< rigSlotSizes.Length; ++j)
                        {
                            if (rigSlotSizes[j] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium && !firstMediumSkipped)
                            {
                                firstMediumSkipped = true;
                                continue;
                            }
                            if (newAISpawn.inventory.rigContents[j] == null && (int)rigSlotSizes[j] >= (int)looseLootItemSize)
                            {
                                newAISpawn.inventory.rigContents[j] = looseLootItemID;
                                success = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (inventoryDataToUse["items"]["SpecialLoot"] != null)
            {
                int specialItemMin = (int)botDataToUse.generation["items"]["specialItems"]["min"];
                int specialItemMax = (int)botDataToUse.generation["items"]["specialItems"]["max"];
                List<string> possibleSpecialItems = inventoryDataToUse["items"]["SpecialLoot"].ToObject<List<string>>();
                if (specialItemMax > 0 && possibleSpecialItems.Count > 0)
                {
                    for (int i = 0; i < specialItemMax; ++i)
                    {
                        string specialItemID = null;
                        ItemMapEntry itemMapEntry = Mod.itemMap[possibleSpecialItems[UnityEngine.Random.Range(0, possibleSpecialItems.Count)]];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                specialItemID = itemMapEntry.ID;
                                break;
                            case 1:
                                specialItemID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                break;
                            case 2:
                                specialItemID = itemMapEntry.otherModID;
                                break;
                        }
                        object[] specialItemData = GetItemData(specialItemID);

                        if (i >= specialItemMin && UnityEngine.Random.value > float.Parse(specialItemData[2] as string))
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
                            newAISpawn.inventory.genericPockets.Add(newAISpawn.inventory.generic.Count);
                            newAISpawn.inventory.generic.Add(specialItemID);
                            ++pocketsUsed;
                            success = true;
                        }
                        if (!success && rigSlotSizes != null)
                        {
                            bool firstMediumSkipped = false;
                            for (int j = 0; j < rigSlotSizes.Length; ++j)
                            {
                                if (rigSlotSizes[j] == FVRPhysicalObject.FVRPhysicalObjectSize.Medium && !firstMediumSkipped)
                                {
                                    firstMediumSkipped = true;
                                    continue;
                                }
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
            }

            // Build config template
            newAISpawn.configTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
            newAISpawn.configTemplate.LinkDamageMultipliers = new List<float>() { 1, 1, 1, 1 };
            newAISpawn.configTemplate.LinkStaggerMultipliers = new List<float>() { 1, 1, 1, 1 };
            newAISpawn.configTemplate.StartingLinkIntegrity = new List<Vector2>() { new Vector2(100, 100), new Vector2(100, 100), new Vector2(100, 100), new Vector2(100, 100) };
            newAISpawn.configTemplate.StartingChanceBrokenJoint = new List<float>() { 0,0,0,0 };
            newAISpawn.configTemplate.DoesAggroOnFriendlyFire = true;
            Dictionary<string, JObject> health = botDataToUse.health["BodyParts"].ToObject<Dictionary<string, JObject>>();
            float totalHealth = 0;
            foreach(KeyValuePair<string, JObject> pair in health)
            {
                totalHealth += UnityEngine.Random.Range((float)pair.Value["min"], (float)pair.Value["max"]);
            }
            newAISpawn.configTemplate.TotalMustard += (totalHealth - 440);
            //newAISpawn.configTemplate.ConfusionMultiplier = 1;
            newAISpawn.configTemplate.StunThreshold = float.MaxValue;
            newAISpawn.configTemplate.StunMultiplier = 0;
            newAISpawn.configTemplate.StunTimeMax = 0;
            newAISpawn.configTemplate.ShudderThreshold = float.MaxValue;
            newAISpawn.configTemplate.CanBeKnockedOut = false;
            newAISpawn.configTemplate.CanBeGrabbed = false;

            Mod.LogInfo("\tDone");
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
                CustomItemWrapper itemCIW = itemPrefab.GetComponentInChildren<CustomItemWrapper>();
                itemParents = itemCIW.parents;
                itemVolume = itemCIW.volumes[0];
                itemSpawnChance = Mod.GetRaritySpawnChanceMultiplier(itemCIW.rarity);
            }
            else
            {
                VanillaItemDescriptor itemVID = itemPrefab.GetComponentInChildren<VanillaItemDescriptor>();
                itemParents = itemVID.parents;
                itemVolume = Mod.itemVolumes[ID];
                itemSpawnChance = Mod.GetRaritySpawnChanceMultiplier(itemVID.rarity);
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
                            string actualModID = null;
                            ItemMapEntry itemMapEntry = Mod.itemMap[modID];
                            switch (itemMapEntry.mode)
                            {
                                case 0:
                                    actualModID = itemMapEntry.ID;
                                    break;
                                case 1:
                                    actualModID = itemMapEntry.modulIDs[UnityEngine.Random.Range(0, itemMapEntry.modulIDs.Length)];
                                    break;
                                case 2:
                                    actualModID = itemMapEntry.otherModID;
                                    break;
                            }
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
            string[] botInventoryFiles = Directory.GetFiles(Mod.path + "/DB/Bots/" + name + "/inventory/");
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
            botData.chances = JObject.Parse(File.ReadAllText(Mod.path + "/DB/Bots/" + name + "/chances.json"));
            botData.experience = JObject.Parse(File.ReadAllText(Mod.path + "/DB/Bots/" + name + "/experience.json"));
            botData.generation = JObject.Parse(File.ReadAllText(Mod.path + "/DB/Bots/" + name + "/generation.json"));
            botData.health = JObject.Parse(File.ReadAllText(Mod.path + "/DB/Bots/" + name + "/health.json"));
            botData.names = JArray.Parse(File.ReadAllText(Mod.path + "/DB/Bots/" + name + "/names.json"));

            return botData;
        }

        private float ExpDistrRandOnAvg(float average)
        {
            return -average * Mathf.Log(UnityEngine.Random.value);
        }

        private void UpdateEffects()
        {
            // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
            for (int i = Effect.effects.Count; i >= 0; --i)
            {
                if (Effect.effects.Count == 0)
                {
                    break;
                }
                else if (i >= Effect.effects.Count)
                {
                    continue;
                }

                Effect effect = Effect.effects[i];
                if (effect.active)
                {
                    UpdateHealthEffectCounterConditions(effect.effectType);

                    if (effect.hasTimer || effect.hideoutOnly)
                    {
                        effect.timer -= Time.deltaTime;
                        if (effect.timer <= 0)
                        {
                            effect.active = false;

                            ResetHealthEffectCounterConditions(false, effect.effectType);

                            // Unapply effect
                            switch (effect.effectType)
                            {
                                case Effect.EffectType.SkillRate:
                                    Mod.skills[effect.skillIndex].currentProgress -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.EnergyRate:
                                    Mod.currentEnergyRate -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.HydrationRate:
                                    Mod.currentHydrationRate -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.MaxStamina:
                                    Mod.currentMaxStamina -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.StaminaRate:
                                    Mod.currentStaminaEffect -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.HandsTremor:
                                    // TODO: Stop tremors if there are no other tremor effects
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.QuantumTunnelling:
                                    // TODO: Stop QuantumTunnelling if there are no other tunnelling effects
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.HealthRate:
                                    float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
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
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.RemoveAllBloodLosses:
                                    // Reactivate all bleeding 
                                    // Not necessary because when we disabled them we used the disable timer
                                    break;
                                case Effect.EffectType.Contusion:
                                    bool otherContusions = false;
                                    foreach(Effect contusionEffectCheck in Effect.effects)
                                    {
                                        if(contusionEffectCheck.active && contusionEffectCheck.effectType == Effect.EffectType.Contusion)
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
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.WeightLimit:
                                    Mod.effectWeightLimitBonus -= effect.value * 1000;
                                    Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.DamageModifier:
                                    Mod.currentDamageModifier -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.Pain:
                                    // Remove all tremors caused by this pain and disable tremors if no other tremors active
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasPainTremors = false;
                                    foreach (Effect effectCheck in Effect.effects)
                                    {
                                        if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.StomachBloodloss:
                                    --Mod.stomachBloodLossCount;
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.UnknownToxin:
                                    // Remove all effects caused by this toxin
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == Effect.EffectType.HealthRate)
                                        {
                                            for (int j = 0; j < 7; ++j)
                                            {
                                                Mod.currentHealthRates[j] -= causedEffect.value / 7;
                                            }
                                        }
                                        // Could go two layers deep
                                        foreach (Effect causedCausedEffect in effect.caused)
                                        {
                                            Effect.effects.Remove(causedCausedEffect);
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasToxinTremors = false;
                                    foreach (Effect effectCheck in Effect.effects)
                                    {
                                        if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.BodyTemperature:
                                    Mod.temperatureOffset -= effect.value;
                                    break;
                                case Effect.EffectType.Antidote:
                                    // Will remove toxin on activation, does nothing after
                                    break;
                                case Effect.EffectType.LightBleeding:
                                case Effect.EffectType.HeavyBleeding:
                                    // Remove all effects caused by this bleeding
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == Effect.EffectType.HealthRate)
                                        {
                                            if (causedEffect.partIndex == -1)
                                            {
                                                for (int j = 0; j < 7; ++j)
                                                {
                                                    Mod.currentNonLethalHealthRates[j] -= causedEffect.value;
                                                }
                                            }
                                            else
                                            {
                                                Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                            }
                                        }
                                        else // Energy rate
                                        {
                                            Mod.currentEnergyRate -= causedEffect.value;
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.Fracture:
                                    // Remove all effects caused by this fracture
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        // Could go two layers deep
                                        foreach (Effect causedCausedEffect in effect.caused)
                                        {
                                            Effect.effects.Remove(causedCausedEffect);
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasFractureTremors = false;
                                    foreach (Effect effectCheck in Effect.effects)
                                    {
                                        if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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
                                    Mod.AddSkillExp(5, 6);
                                    break;
                            }

                            Effect.effects.RemoveAt(i);

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
                            case Effect.EffectType.SkillRate:
                                Mod.skills[effect.skillIndex].currentProgress += effect.value;
                                break;
                            case Effect.EffectType.EnergyRate:
                                Mod.currentEnergyRate += effect.value;
                                break;
                            case Effect.EffectType.HydrationRate:
                                Mod.currentHydrationRate += effect.value;
                                break;
                            case Effect.EffectType.MaxStamina:
                                Mod.currentMaxStamina += effect.value;
                                break;
                            case Effect.EffectType.StaminaRate:
                                Mod.currentStaminaEffect += effect.value;
                                break;
                            case Effect.EffectType.HandsTremor:
                                // TODO: Begin tremors if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.QuantumTunnelling:
                                // TODO: Begin quantumtunneling if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HealthRate:
                                float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
                                if (effect.partIndex == -1)
                                {
                                    for (int j = 0; j < 7; ++j)
                                    {
                                        arrayToUse[j] += effect.value; // Note: Need to keep in mind this will apply the ENTIRE value to EACH part
                                    }
                                }
                                else
                                {
                                    arrayToUse[effect.partIndex] += effect.value;
                                }
                                break;
                            case Effect.EffectType.RemoveAllBloodLosses:
                                // Deactivate all bleeding using disable timer
                                foreach (Effect bleedEffect in Effect.effects)
                                {
                                    if (bleedEffect.effectType == Effect.EffectType.LightBleeding || bleedEffect.effectType == Effect.EffectType.HeavyBleeding)
                                    {
                                        bleedEffect.active = false;
                                        bleedEffect.inactiveTimer = effect.timer;

                                        // Unapply the healthrate caused by this bleed
                                        Effect causedHealthRate = bleedEffect.caused[0];
                                        if (causedHealthRate.nonLethal)
                                        {
                                            Mod.currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        else
                                        {
                                            Mod.currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        Effect causedEnergyRate = bleedEffect.caused[1];
                                        Mod.currentEnergyRate -= causedEnergyRate.value;
                                        bleedEffect.caused.Clear();
                                        Effect.effects.Remove(causedHealthRate);
                                        Effect.effects.Remove(causedEnergyRate);
                                    }
                                }
                                break;
                            case Effect.EffectType.Contusion:
                                // Disable haptic feedback
                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
                                // TODO: also set volume to 0.33 * volume
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.WeightLimit:
                                Mod.effectWeightLimitBonus += effect.value * 1000;
                                Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
                                break;
                            case Effect.EffectType.DamageModifier:
                                Mod.currentDamageModifier += effect.value;
                                break;
                            case Effect.EffectType.Pain:
                                if (UnityEngine.Random.value < 1 - (Mod.skills[4].currentProgress / 10000))
                                {
                                    // Add a tremor effect
                                    Effect newTremor = new Effect();
                                    newTremor.effectType = Effect.EffectType.HandsTremor;
                                    newTremor.delay = 5;
                                    newTremor.hasTimer = effect.hasTimer;
                                    newTremor.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                    - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                    Effect.effects.Add(newTremor);
                                    effect.caused.Add(newTremor);
                                }

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(true);
                                }

                                Mod.AddSkillExp(Skill.stressResistanceHealthNegativeEffect, 4);
                                break;
                            case Effect.EffectType.StomachBloodloss:
                                ++Mod.stomachBloodLossCount;
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.UnknownToxin:
                                // Add a pain effect
                                Effect newToxinPain = new Effect();
                                newToxinPain.effectType = Effect.EffectType.Pain;
                                newToxinPain.delay = 5;
                                newToxinPain.hasTimer = effect.hasTimer;
                                newToxinPain.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                   - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                newToxinPain.partIndex = 0;
                                Effect.effects.Add(newToxinPain);
                                effect.caused.Add(newToxinPain);
                                // Add a health rate effect
                                Effect newToxinHealthRate = new Effect();
                                newToxinHealthRate.effectType = Effect.EffectType.HealthRate;
                                newToxinHealthRate.delay = 5;
                                newToxinHealthRate.value = -25 + 25 * (Skill.immunityPoisonBuff * (Mod.skills[6].currentProgress / 100) / 100);
                                newToxinHealthRate.hasTimer = effect.hasTimer;
                                newToxinHealthRate.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                Effect.effects.Add(newToxinHealthRate);
                                effect.caused.Add(newToxinHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.BodyTemperature:
                                Mod.temperatureOffset += effect.value;
                                break;
                            case Effect.EffectType.Antidote:
                                // Will remove toxin on ativation, does nothing after
                                for (int j = Effect.effects.Count; j >= 0; --j)
                                {
                                    if (Effect.effects[j].effectType == Effect.EffectType.UnknownToxin)
                                    {
                                        Effect.effects.RemoveAt(j);
                                        break;
                                    }
                                }
                                break;
                            case Effect.EffectType.LightBleeding:
                                // Add a health rate effect
                                Effect newLightBleedingHealthRate = new Effect();
                                newLightBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
                                newLightBleedingHealthRate.delay = 5;
                                newLightBleedingHealthRate.value = -8;
                                newLightBleedingHealthRate.hasTimer = effect.hasTimer;
                                newLightBleedingHealthRate.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                newLightBleedingHealthRate.nonLethal = true;
                                Effect.effects.Add(newLightBleedingHealthRate);
                                effect.caused.Add(newLightBleedingHealthRate);
                                // Add a energy rate effect
                                Effect newLightBleedingEnergyRate = new Effect();
                                newLightBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newLightBleedingEnergyRate.delay = 5;
                                newLightBleedingEnergyRate.value = -5;
                                newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newLightBleedingEnergyRate.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                Effect.effects.Add(newLightBleedingEnergyRate);
                                effect.caused.Add(newLightBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyBleeding:
                                // Add a health rate effect
                                Effect newHeavyBleedingHealthRate = new Effect();
                                newHeavyBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyBleedingHealthRate.delay = 5;
                                newHeavyBleedingHealthRate.value = -13.5f; // Note: This damage will be applied to each part every 60 seconds since no part index is specified
                                newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingHealthRate.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                newHeavyBleedingHealthRate.nonLethal = true;
                                Effect.effects.Add(newHeavyBleedingHealthRate);
                                effect.caused.Add(newHeavyBleedingHealthRate);
                                // Add a energy rate effect
                                Effect newHeavyBleedingEnergyRate = new Effect();
                                newHeavyBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newHeavyBleedingEnergyRate.delay = 5;
                                newHeavyBleedingEnergyRate.value = -6;
                                newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingEnergyRate.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                Effect.effects.Add(newHeavyBleedingEnergyRate);
                                effect.caused.Add(newHeavyBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Fracture:
                                // Add a pain effect
                                Effect newFracturePain = new Effect();
                                newFracturePain.effectType = Effect.EffectType.Pain;
                                newFracturePain.delay = 5;
                                newFracturePain.hasTimer = effect.hasTimer;
                                newFracturePain.timer = effect.timer + effect.timer * (Base_Manager.currentDebuffEndDelay - Base_Manager.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                      - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100); ;
                                Effect.effects.Add(newFracturePain);
                                effect.caused.Add(newFracturePain);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Dehydration:
                                // Add a HealthRate effect
                                Effect newDehydrationHealthRate = new Effect();
                                newDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
                                newDehydrationHealthRate.value = -60;
                                newDehydrationHealthRate.delay = 5;
                                newDehydrationHealthRate.hasTimer = false;
                                Effect.effects.Add(newDehydrationHealthRate);
                                effect.caused.Add(newDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyDehydration:
                                // Add a HealthRate effect
                                Effect newHeavyDehydrationHealthRate = new Effect();
                                newHeavyDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyDehydrationHealthRate.value = -350;
                                newHeavyDehydrationHealthRate.delay = 5;
                                newHeavyDehydrationHealthRate.hasTimer = false;
                                Effect.effects.Add(newHeavyDehydrationHealthRate);
                                effect.caused.Add(newHeavyDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Fatigue:
                                Mod.fatigue = true;

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyFatigue:
                                // Add a HealthRate effect
                                Effect newHeavyFatigueHealthRate = new Effect();
                                newHeavyFatigueHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyFatigueHealthRate.value = -30;
                                newHeavyFatigueHealthRate.delay = 5;
                                newHeavyFatigueHealthRate.hasTimer = false;
                                Effect.effects.Add(newHeavyFatigueHealthRate);
                                effect.caused.Add(newHeavyFatigueHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.OverweightFatigue:
                                // Add a EnergyRate effect
                                Effect newOverweightFatigueEnergyRate = new Effect();
                                newOverweightFatigueEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newOverweightFatigueEnergyRate.value = -4;
                                newOverweightFatigueEnergyRate.delay = 5;
                                newOverweightFatigueEnergyRate.hasTimer = false;
                                Effect.effects.Add(newOverweightFatigueEnergyRate);
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
            float maxHealthTotal = 0;

            // Apply lethal health rates
            for (int i = 0; i < 7; ++i)
            {
                maxHealthTotal += Mod.currentMaxHealth[i];
                if (Mod.health[i] <= 0 && !(i == 0 || i == 1)) 
                {
                    // Apply currentHealthRates[i] to other parts
                    for (int j = 0; j < 7; ++j)
                    {
                        if (j != i)
                        {
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + Mod.currentHealthRates[i] * (Time.deltaTime / 60) / 6, 0, Mod.currentMaxHealth[j]);

                            if (Mod.health[j] <= 0 && (j == 0 || j == 1))
                            {
                                KillPlayer();
                            }
                        }
                    }
                }
                else if(Mod.currentHealthRates[i] < 0 && Mod.health[i] <= 0)
                {
                    KillPlayer();
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + Mod.currentHealthRates[i] * (Time.deltaTime / 60), 0, Mod.currentMaxHealth[i]);
                }

                if (!lethal)
                {
                    lethal = Mod.currentHealthRates[i] > 0;
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
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + Mod.currentNonLethalHealthRates[i] * (Time.deltaTime / 60) / 6, 0, Mod.currentMaxHealth[j]);
                        }
                    }
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + Mod.currentNonLethalHealthRates[i] * (Time.deltaTime / 60), 0, Mod.currentMaxHealth[i]);
                }
                Mod.playerStatusManager.partHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
                Mod.playerStatusManager.partHealthImages[i].color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);

                healthDelta += Mod.currentNonLethalHealthRates[i];
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
            Mod.playerStatusManager.healthText.text = String.Format("{0:0}/{1:0}", health, maxHealthTotal);
            GM.CurrentPlayerBody.SetHealthThreshold(maxHealthTotal);
            GM.CurrentPlayerBody.Health = health; // This must be done after setting health threshold because setting health threshold also sets health
            if(health < maxHealthTotal / 100 * 20) // Add stress resistance xp if below 20% max health
            {
                Mod.AddSkillExp(Skill.lowHPDuration * Time.deltaTime, 4);
            }

            Mod.hydration = Mathf.Clamp(Mod.hydration + (Mod.currentHydrationRate - (Mod.currentHydrationRate * (0.006f * (Mod.skills[3].currentProgress / 100)))) * (Time.deltaTime / 60), 0, Mod.maxHydration);
            if (Mod.currentHydrationRate != 0)
            {
                if (!Mod.playerStatusManager.hydrationDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.hydrationDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.hydrationDeltaText.text = (Mod.currentHydrationRate >= 0 ? "+ " : "") + String.Format("{0:0.#}/min", Mod.currentHydrationRate);
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
                    Effect newHeavyDehydration = new Effect();
                    newHeavyDehydration.effectType = Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.delay = 5;
                    newHeavyDehydration.hasTimer = false;
                    Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == Effect.EffectType.Dehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if(Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyDehydration effect
                    Effect newHeavyDehydration = new Effect();
                    newHeavyDehydration.effectType = Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.hasTimer = false;
                    Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
            }
            else if(Mod.hydration < 20)
            {
                if (Mod.dehydrationEffect == null)
                {
                    // Add a dehydration effect
                    Effect newDehydration = new Effect();
                    newDehydration.effectType = Effect.EffectType.Dehydration;
                    newDehydration.delay = 5;
                    newDehydration.hasTimer = false;
                    Effect.effects.Add(newDehydration);
                    Mod.dehydrationEffect = newDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == Effect.EffectType.HeavyDehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a dehydration effect
                    Effect newDehydration = new Effect();
                    newDehydration.effectType = Effect.EffectType.Dehydration;
                    newDehydration.hasTimer = false;
                    Effect.effects.Add(newDehydration);
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
                            Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.dehydrationEffect);
                }
            }

            Mod.energy = Mathf.Clamp(Mod.energy + (Mod.currentEnergyRate - (Mod.currentEnergyRate * (0.006f * (Mod.skills[3].currentProgress / 100)))) * (Time.deltaTime / 60), 0, Mod.maxEnergy);

            if (Mod.currentEnergyRate != 0)
            {
                if (!Mod.playerStatusManager.energyDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.energyDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.energyDeltaText.text = (Mod.currentEnergyRate >= 0 ? "+ " : "") + String.Format("{0:0.#}/min", Mod.currentEnergyRate);
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
                    Effect newHeavyFatigue = new Effect();
                    newHeavyFatigue.effectType = Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.delay = 5;
                    newHeavyFatigue.hasTimer = false;
                    Effect.effects.Add(newHeavyFatigue);
                    Mod.fatigueEffect = newHeavyFatigue;
                }
                else if (Mod.fatigueEffect.effectType == Effect.EffectType.Fatigue)
                {
                    // Disable the other fatigue before adding a new one
                    Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyFatigue effect
                    Effect newHeavyFatigue = new Effect();
                    newHeavyFatigue.effectType = Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.hasTimer = false;
                    Effect.effects.Add(newHeavyFatigue);
                    Mod.dehydrationEffect = newHeavyFatigue;
                }
            }
            else if (Mod.energy < 20)
            {
                if (Mod.fatigueEffect == null)
                {
                    // Add a fatigue effect
                    Effect newFatigue = new Effect();
                    newFatigue.effectType = Effect.EffectType.Fatigue;
                    newFatigue.delay = 5;
                    newFatigue.hasTimer = false;
                    Effect.effects.Add(newFatigue);
                    Mod.fatigueEffect = newFatigue;
                }
                else if (Mod.fatigueEffect.effectType == Effect.EffectType.HeavyFatigue)
                {
                    // Disable the other fatigue before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Mod.currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.fatigueEffect);

                    // Add a fatigue effect
                    Effect newFatigue = new Effect();
                    newFatigue.effectType = Effect.EffectType.Fatigue;
                    newFatigue.hasTimer = false;
                    Effect.effects.Add(newFatigue);
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
                            Mod.currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.fatigueEffect);
                }
            }
        }

        public void UpdateHealthEffectCounterConditions(Effect.EffectType effectType)
        {
            if (Mod.chosenCharIndex == 1)
            {
                return;
            }

            if (Mod.currentHealthEffectCounterConditionsByEffectType != null && Mod.currentHealthEffectCounterConditionsByEffectType.ContainsKey(effectType))
            {
                List<TraderTaskCounterCondition> healthEffectCounterConditions = Mod.currentHealthEffectCounterConditionsByEffectType[effectType];
                foreach (TraderTaskCounterCondition counterCondition in healthEffectCounterConditions)
                {
                    counterCondition.timer += Time.deltaTime;

                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
        }

        public void ResetHealthEffectCounterConditions(bool all = true, Effect.EffectType effectType = Effect.EffectType.UnknownToxin)
        {
            if (all)
            {
                foreach (KeyValuePair<Effect.EffectType, List<TraderTaskCounterCondition>> entry in Mod.currentHealthEffectCounterConditionsByEffectType)
                {
                    foreach (TraderTaskCounterCondition counterCondition in entry.Value)
                    {
                        counterCondition.timer = 0;
                    }
                }

                Mod.currentHealthEffectCounterConditionsByEffectType = null;
            }
            else
            {
                if (Mod.currentHealthEffectCounterConditionsByEffectType.ContainsKey(effectType))
                {
                    List<TraderTaskCounterCondition> healthEffectCounterConditions = Mod.currentHealthEffectCounterConditionsByEffectType[effectType];
                    foreach (TraderTaskCounterCondition counterCondition in healthEffectCounterConditions)
                    {
                        counterCondition.timer = 0;
                    }
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
            int scaledTime = (int)((clampedTime * Manager.meatovTimeMultiplier) % 86400);
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
            time += UnityEngine.Time.deltaTime * Manager.meatovTimeMultiplier;

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
            foreach(Effect effect in Effect.effects)
            {
                if (effect.active)
                {
                    switch (effect.effectType)
                    {
                        case Effect.EffectType.EnergyRate:
                            Mod.currentEnergyRate += effect.value;
                            break;
                        case Effect.EffectType.HydrationRate:
                            Mod.currentHydrationRate += effect.value;
                            break;
                        case Effect.EffectType.HealthRate:
                            for (int j = 0; j < 7; ++j)
                            {
                                Mod.currentHealthRates[j] += effect.value / 7;
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

        private GameObject SpawnLootItem(GameObject itemPrefab, Transform itemsRoot, string itemID, JToken spawnData, string originalID, bool useChance)
        {
            Mod.LogInfo("Spawn loot item called with ID: " + itemID);
            GameObject itemObject = null;
            if (itemPrefab == null)
            {
                return null;
            }

            // Instantiate in a random slot that fits the item if there is one
            CustomItemWrapper prefabCIW = itemPrefab.GetComponent<CustomItemWrapper>();
            VanillaItemDescriptor prefabVID = itemPrefab.GetComponent<VanillaItemDescriptor>();

            FVRPhysicalObject itemPhysObj = null;
            CustomItemWrapper itemCIW = null;
            VanillaItemDescriptor itemVID = null;
            FireArmRoundType roundType = FireArmRoundType.a106_25mmR;
            FireArmRoundClass roundClass = FireArmRoundClass.a20AP;
            if (prefabCIW != null)
            {
                itemObject = GameObject.Instantiate(itemPrefab);
                itemCIW = itemObject.GetComponent<CustomItemWrapper>();
                itemPhysObj = itemCIW.GetComponent<FVRPhysicalObject>();

                itemCIW.foundInRaid = true;

                // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                Mod.RemoveFromAll(null, itemCIW, null);

                // Get amount
                if (itemCIW.itemType == Mod.ItemType.Money)
                {
                    if (itemCIW.ID.Equals("201"))
                    {
                        itemCIW.stack = UnityEngine.Random.Range(20, 121);
                    }
                    else if (itemCIW.ID.Equals("202"))
                    {
                        itemCIW.stack = UnityEngine.Random.Range(10, 101);
                    }
                    else
                    {
                        itemCIW.stack = UnityEngine.Random.Range(500, 5001);
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
            else
            {
                if (Mod.usedRoundIDs.Contains(prefabVID.H3ID))
                {
                    Mod.LogInfo("\tSpawning round with ID: " + prefabVID.H3ID);
                    // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of rounds
                    int amount = UnityEngine.Random.Range(1, 121);
                    FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                    roundType = round.RoundType;
                    roundClass = round.RoundClass;
                    if (amount > 1)
                    {
                        Mod.LogInfo("\t\tStack > 1");
                        if (Mod.ammoBoxByAmmoID.ContainsKey(prefabVID.H3ID))
                        {
                            Mod.LogInfo("\t\t\tSpecific box");
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[Mod.ammoBoxByAmmoID[prefabVID.H3ID]]);
                            itemCIW = itemObject.GetComponent<CustomItemWrapper>();
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
                            Mod.LogInfo("\t\t\tGeneric box");
                            if (amount > 30)
                            {
                                itemObject = GameObject.Instantiate(Mod.itemPrefabs[716]);
                            }
                            else
                            {
                                itemObject = GameObject.Instantiate(Mod.itemPrefabs[715]);
                            }

                            itemCIW = itemObject.GetComponent<CustomItemWrapper>();
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
                        itemCIW.foundInRaid = true;
                    }
                    else // Single round, spawn as normal
                    {
                        Mod.LogInfo("\t\tSingle round");
                        itemObject = GameObject.Instantiate(itemPrefab);
                        itemVID = itemObject.GetComponent<VanillaItemDescriptor>();
                        itemPhysObj = itemVID.GetComponent<FVRPhysicalObject>();
                        itemVID.foundInRaid = true;
                    }
                }
                else // Not a round, spawn as normal
                {
                    itemObject = GameObject.Instantiate(itemPrefab);
                    itemVID = itemObject.GetComponent<VanillaItemDescriptor>();
                    itemVID.foundInRaid = true;
                    itemPhysObj = itemVID.GetComponent<FVRPhysicalObject>();

                    if(itemPhysObj is FVRFireArm)
                    {
                        // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                        Mod.RemoveFromAll(itemPhysObj, null, itemVID);
                    }
                }
            }

            if (spawnData["Position"].Type == JTokenType.Object)
            {
                Vector3 position = new Vector3((float)spawnData["Position"]["x"], (float)spawnData["Position"]["y"], (float)spawnData["Position"]["z"]);
                itemObject.transform.position = position;
            }
            if (spawnData["Rotation"].Type == JTokenType.Object)
            {
                Vector3 rotation = new Vector3((float)spawnData["Rotation"]["x"], (float)spawnData["Rotation"]["y"], (float)spawnData["Rotation"]["z"]);
                itemObject.transform.rotation = Quaternion.Euler(rotation);
            }
            else if(spawnData["randomRotation"] != null && (bool)spawnData["randomRotation"])
            {
                itemObject.transform.rotation = UnityEngine.Random.rotation;
            }
            itemObject.transform.parent = itemsRoot;

            if (itemObject != null)
            {
                Mod.LogInfo("Spawned loose loot: " + itemObject.name);
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

        public void KillPlayer(Base_Manager.FinishRaidState raidState = Base_Manager.FinishRaidState.KIA)
        {
            if (Mod.dead)
            {
                return;
            }
            Mod.LogInfo("Kill player called:\n "+Environment.StackTrace);
            Mod.dead = true;

            // Register insured items that are currently on player
            if (Mod.chosenCharIndex == 0)
            {
                Mod.LogInfo("\tRegistering insured items");
                if (Mod.insuredItems == null)
                {
                    Mod.insuredItems = new List<InsuredSet>();
                }
                InsuredSet insuredSet = new InsuredSet();
                insuredSet.returnTime = GetTimeSeconds() + (long)(86400 * Base_Manager.currentInsuranceReturnTime - Base_Manager.currentInsuranceReturnTime * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100)); // Current time + 24 hours, TODO: Should be dependent on who insured it
                insuredSet.items = new Dictionary<string, int>();
                foreach (KeyValuePair<string, List<GameObject>> itemObjectList in Mod.playerInventoryObjects)
                {
                    foreach (GameObject itemObject in itemObjectList.Value)
                    {
                        if (itemObject == null)
                        {
                            Mod.LogError("ItemObject in player inventory with ID: " + itemObjectList.Key + " is null while building insured set, removing from list");
                            itemObjectList.Value.Remove(itemObject);
                            break;
                        }
                        CustomItemWrapper CIW = itemObject.GetComponent<CustomItemWrapper>();
                        VanillaItemDescriptor VID = itemObject.GetComponent<VanillaItemDescriptor>();
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
                        else if (VID != null && VID.insured && UnityEngine.Random.value <= 0.5)
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
                if (insuredSet.items.Count > 0)
                {
                    Mod.insuredItems.Add(insuredSet);
                }
            }

            Mod.LogInfo("\tDropping held items, preweight: "+Mod.weight);
            // Drop items in hand
            if (GM.CurrentMovementManager.Hands[0].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[0].CurrentInteractable is FVRPhysicalObject))
            {
                Mod.LogInfo("\t\tDropping left held item");
                GM.CurrentMovementManager.Hands[0].CurrentInteractable.ForceBreakInteraction();
            }
            if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null && !(GM.CurrentMovementManager.Hands[1].CurrentInteractable is FVRPhysicalObject))
            {
                Mod.LogInfo("\t\tDropping right held item");
                GM.CurrentMovementManager.Hands[1].CurrentInteractable.ForceBreakInteraction();
            }
            Mod.LogInfo("\tDropping held items, postweight: " + Mod.weight);

            Mod.LogInfo("\tDropping equipment, preweight: " + Mod.weight);
            // Unequip and destroy all equipment apart from pouch
            if (EquipmentSlot.wearingBackpack)
            {
                CustomItemWrapper backpackCIW = EquipmentSlot.currentBackpack;
                Mod.LogInfo("\t\tDropping backpack, weight: "+ backpackCIW.currentWeight);
                FVRPhysicalObject backpackPhysObj = backpackCIW.GetComponent<FVRPhysicalObject>();
                backpackPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= backpackCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(backpackCIW);
                backpackCIW.destroyed = true;
                Destroy(backpackCIW.gameObject);
            }
            if (EquipmentSlot.wearingBodyArmor)
            {
                CustomItemWrapper bodyArmorCIW = EquipmentSlot.currentArmor;
                Mod.LogInfo("\t\tDropping body armor, weight: " + bodyArmorCIW.currentWeight);
                FVRPhysicalObject bodyArmorPhysObj = bodyArmorCIW.GetComponent<FVRPhysicalObject>();
                bodyArmorPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= bodyArmorCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(bodyArmorCIW);
                bodyArmorCIW.destroyed = true;
                Destroy(bodyArmorCIW.gameObject);
            }
            if (EquipmentSlot.wearingEarpiece)
            {
                CustomItemWrapper earPieceCIW = EquipmentSlot.currentEarpiece;
                Mod.LogInfo("\t\tDropping earpiece, weight: " + earPieceCIW.currentWeight);
                FVRPhysicalObject earPiecePhysObj = earPieceCIW.GetComponent<FVRPhysicalObject>();
                earPiecePhysObj.SetQuickBeltSlot(null);
                Mod.weight -= earPieceCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(earPieceCIW);
                earPieceCIW.destroyed = true;
                Destroy(earPieceCIW.gameObject);
            }
            if (EquipmentSlot.wearingHeadwear)
            {
                CustomItemWrapper headWearCIW = EquipmentSlot.currentHeadwear;
                Mod.LogInfo("\t\tDropping headwear, weight: " + headWearCIW.currentWeight);
                FVRPhysicalObject headWearPhysObj = headWearCIW.GetComponent<FVRPhysicalObject>();
                headWearPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= headWearCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(headWearCIW);
                headWearCIW.destroyed = true;
                Destroy(headWearCIW.gameObject);
            }
            if (EquipmentSlot.wearingFaceCover)
            {
                CustomItemWrapper faceCoverCIW = EquipmentSlot.currentFaceCover;
                Mod.LogInfo("\t\tDropping face cover, weight: " + faceCoverCIW.currentWeight);
                FVRPhysicalObject faceCoverPhysObj = faceCoverCIW.GetComponent<FVRPhysicalObject>();
                faceCoverPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= faceCoverCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(faceCoverCIW);
                faceCoverCIW.destroyed = true;
                Destroy(faceCoverCIW.gameObject);
            }
            if (EquipmentSlot.wearingEyewear)
            {
                CustomItemWrapper eyeWearCIW = EquipmentSlot.currentEyewear;
                Mod.LogInfo("\t\tDropping eyewear, weight: " + eyeWearCIW.currentWeight);
                FVRPhysicalObject eyeWearPhysObj = eyeWearCIW.GetComponent<FVRPhysicalObject>();
                eyeWearPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= eyeWearCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(eyeWearCIW);
                eyeWearCIW.destroyed = true;
                Destroy(eyeWearCIW.gameObject);
            }
            if (EquipmentSlot.wearingRig)
            {
                CustomItemWrapper rigCIW = EquipmentSlot.currentRig;
                Mod.LogInfo("\t\tDropping rig, weight: " + rigCIW.currentWeight);
                FVRPhysicalObject rigPhysObj = rigCIW.GetComponent<FVRPhysicalObject>();
                rigPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= rigCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(rigCIW);
                rigCIW.destroyed = true;
                Destroy(rigCIW.gameObject);
            }
            if (Mod.chosenCharIndex == 1 && EquipmentSlot.wearingPouch)
            {
                CustomItemWrapper pouchCIW = EquipmentSlot.currentPouch;
                Mod.LogInfo("\t\tDropping pouch, weight: " + pouchCIW.currentWeight);
                FVRPhysicalObject pouchPhysObj = pouchCIW.GetComponent<FVRPhysicalObject>();
                pouchPhysObj.SetQuickBeltSlot(null);
                Mod.weight -= pouchCIW.currentWeight;
                EquipmentSlot.TakeOffEquipment(pouchCIW);
                pouchCIW.destroyed = true;
                Destroy(pouchCIW.gameObject);
            }
            Mod.LogInfo("\tDropping equipment, postweight: " + Mod.weight);

            Mod.LogInfo("\tDestroying right shoulder item, preweight: " + Mod.weight);
            // Destroy right shoulder object
            if (Mod.rightShoulderObject != null)
            {
                VanillaItemDescriptor rightShoulderVID = Mod.rightShoulderObject.GetComponent<VanillaItemDescriptor>();
                FVRPhysicalObject rightShoulderPhysObj = rightShoulderVID.GetComponent<FVRPhysicalObject>();
                rightShoulderPhysObj.SetQuickBeltSlot(null);
                Mod.LogInfo("\t\tDestroying right shoulder item, weight: " + rightShoulderVID.currentWeight);
                Mod.weight -= rightShoulderVID.currentWeight;
                rightShoulderVID.destroyed = true;
                Mod.rightShoulderObject = null;
                Destroy(Mod.rightShoulderObject);
            }
            Mod.LogInfo("\tDestroying right shoulder item, postweight: " + Mod.weight);

            Mod.LogInfo("\tDestroying pocket contents, preweight: " + Mod.weight);
            // Destroy pockets' contents, note that the quick belt config went back to pockets only when we unequipped rig
            for (int i=0; i < 4; ++i)
            {
                if(GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject != null)
                {
                    VanillaItemDescriptor pocketItemVID = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.GetComponent<VanillaItemDescriptor>();
                    CustomItemWrapper pocketItemCIW = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject.GetComponent<CustomItemWrapper>();
                    if(pocketItemCIW != null)
                    {
                        Mod.LogInfo("\t\tpocket item, weight: " + pocketItemCIW.currentWeight);
                        Mod.weight -= pocketItemCIW.currentWeight;
                        pocketItemCIW.destroyed = true;
                    }
                    else if(pocketItemVID != null)
                    {
                        Mod.LogInfo("\t\tpocket item, weight: " + pocketItemVID.currentWeight);
                        Mod.weight -= pocketItemVID.currentWeight;
                        pocketItemVID.destroyed = true;
                    }
                    FVRPhysicalObject pocketItemPhysObj = GM.CurrentPlayerBody.QBSlots_Internal[i].CurObject;
                    pocketItemPhysObj.SetQuickBeltSlot(null);
                    Destroy(pocketItemPhysObj.gameObject);
                }
            }
            Mod.LogInfo("\tKilled player final weight: " + Mod.weight);

            // Set raid state
            Mod.justFinishedRaid = true;
            Mod.raidState = raidState;

            // Disable extraction list and timer
            Mod.playerStatusUI.transform.GetChild(0).GetChild(9).gameObject.SetActive(false);
            Mod.playerStatusManager.extractionTimerText.color = Color.black;
            Mod.extractionLimitUI.SetActive(false);
            Mod.extractionUI.SetActive(false);

            UpdateExitStatusCounterConditions();
            ResetHealthEffectCounterConditions();

            Manager.LoadBase(5); // Load autosave, which is right before the start of raid

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
        public Raid_Manager raidManager;

        public Extraction extraction;

        private List<Collider> playerColliders;
        public bool active;

        private void Start()
        {
            Mod.LogInfo("Extraction manager created for extraction: "+extraction.name);
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
            playerColliders.Add(collider);
            Raid_Manager.currentManager.currentExtraction = this;
            Mod.playerStatusManager.currentZone = name;
        }

        private void OnTriggerExit(Collider collider)
        {
            if (playerColliders.Remove(collider) && playerColliders.Count == 0)
            {
                Raid_Manager.currentManager.currentExtraction = null;
                Mod.playerStatusManager.currentZone = null;
            }
        }
    }

    public class LeaveItemProcessor : MonoBehaviour
    {
        public static readonly int maxUpCheck = 4;
        public List<string> itemIDs = new List<string>(); // The itemIDs corresponding to the leave item conditions for this location
        public Dictionary<string, List<TraderTaskCondition>> conditionsByItemID; // The leaveitem conditions for this location

        public void OnTriggerEnter(Collider other)
        {
            Transform currentTransform = other.transform;
            for (int i = 0; i < maxUpCheck; ++i)
            {
                if (currentTransform != null)
                {
                    CustomItemWrapper CIW = currentTransform.GetComponent<CustomItemWrapper>();
                    VanillaItemDescriptor VID = currentTransform.GetComponent<VanillaItemDescriptor>();
                    if (CIW != null)
                    {
                        if (itemIDs.Contains(CIW.ID))
                        {
                            CIW.Highlight(Color.green);
                            CIW.leaveItemProcessor = this;
                        }
                        break;
                    }
                    else if (VID != null)
                    {
                        if (itemIDs.Contains(VID.H3ID))
                        {
                            VID.Highlight(Color.green);
                            VID.leaveItemProcessor = this;
                        }
                        break;
                    }
                    else
                    {
                        currentTransform = currentTransform.parent;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            Transform currentTransform = other.transform;
            for (int i = 0; i < maxUpCheck; ++i)
            {
                if (currentTransform != null)
                {
                    CustomItemWrapper CIW = currentTransform.GetComponent<CustomItemWrapper>();
                    VanillaItemDescriptor VID = currentTransform.GetComponent<VanillaItemDescriptor>();
                    if (CIW != null)
                    {
                        if (itemIDs.Contains(CIW.ID))
                        {
                            CIW.RemoveHighlight();
                            CIW.leaveItemProcessor = null;
                        }
                        break;
                    }
                    else if (VID != null)
                    {
                        if (itemIDs.Contains(VID.H3ID))
                        {
                            VID.RemoveHighlight();
                            VID.leaveItemProcessor = null;
                        }
                        break;
                    }
                    else
                    {
                        currentTransform = currentTransform.parent;
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    public class ZoneManager : MonoBehaviour
    {
        private List<Collider> playerColliders;

        private void Start()
        {
            playerColliders = new List<Collider>();
        }

        private void OnTriggerEnter(Collider collider)
        {
            playerColliders.Add(collider);
            if (Mod.playerStatusManager.currentZone == null)
            {
                Mod.playerStatusManager.currentZone = name;
                UpdateVisitPlaceConditions();
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (playerColliders.Remove(collider) && playerColliders.Count == 0)
            {
                Mod.playerStatusManager.currentZone = null;
            }
        }

        private void UpdateVisitPlaceConditions()
        {
            if (Mod.chosenCharIndex == 1)
            {
                return;
            }

            if (Mod.currentTaskVisitPlaceCounterConditionsByZone.ContainsKey(Mod.playerStatusManager.currentZone))
            {
                List<TraderTaskCounterCondition> visitPlaceCounterConditions = Mod.currentTaskVisitPlaceCounterConditionsByZone[Mod.playerStatusManager.currentZone];
                foreach (TraderTaskCounterCondition counterCondition in visitPlaceCounterConditions)
                {
                    // Check task and condition state validity
                    if (!counterCondition.parentCondition.visible)
                    {
                        continue;
                    }

                    // Check visited place
                    if (Mod.playerStatusManager.currentZone == null || !Mod.playerStatusManager.currentZone.Equals(counterCondition.targetPlaceName))
                    {
                        continue;
                    }

                    // Check constraint counters (Location, Equipment, HealthEffect, InZone)
                    bool constrained = false;
                    foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
                    {
                        if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
                        {
                            constrained = true;
                            break;
                        }
                    }
                    if (constrained)
                    {
                        continue;
                    }

                    // Successful visit, increment count and update fulfillment 
                    counterCondition.completed = true;
                    TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
                }
            }
            if (Mod.currentTaskVisitPlaceConditionsByZone.ContainsKey(Mod.playerStatusManager.currentZone))
            {
                List<TraderTaskCondition> visitPlaceConditions = Mod.currentTaskVisitPlaceConditionsByZone[Mod.playerStatusManager.currentZone];
                foreach (TraderTaskCondition condition in visitPlaceConditions)
                {
                    // Check task and condition state validity
                    if (!condition.visible)
                    {
                        continue;
                    }

                    // Check visited place
                    if (Mod.playerStatusManager.currentZone == null || !Mod.playerStatusManager.currentZone.Equals(condition.targetPlaceName))
                    {
                        continue;
                    }

                    // Successful visit, increment count and update fulfillment 
                    condition.fulfilled = true;
                    TraderStatus.UpdateConditionFulfillment(condition);
                }
            }
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
            Follower,
            Player
        }
        public AISpawnType type;
        public string name;
        public string leaderName;
        public int experienceReward;
        public SosigConfigTemplate configTemplate;
        public bool USEC;

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
        public List<int> genericPockets;

        public string rig;
        public string[] rigContents;

        public string dogtag;
        public int dogtagLevel;
        public string dogtagName;

        public string armor;
        public string earPiece;
        public string headWear;
        public string faceCover;
        public string eyeWear;

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
