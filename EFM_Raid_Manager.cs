using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    class EFM_Raid_Manager : EFM_Manager
    {
        public static readonly float extractionTime = 10; 

        public static EFM_Raid_Manager currentManager;
        public static float extractionTimer;
        public static bool inRaid;

        private List<Extraction> extractions;
        private List<Extraction> possibleExtractions;
        public ExtractionManager currentExtraction;
        private bool inExtractionLastFrame;
        public Transform spawnPoint;
        public Light sun;
        public float time;

        private float energyLoop = 0;
        private float hydrationLoop = 0;

        private void Update()
        {
            if(currentExtraction != null && currentExtraction.active)
            {
                inExtractionLastFrame = true;

                extractionTimer += UnityEngine.Time.deltaTime;

                // TODO: update extraction timer display (also enable it if necessary)

                if(extractionTimer >= extractionTime)
                {
                    Mod.justFinishedRaid = true;
                    Mod.raidState = EFM_Base_Manager.FinishRaidState.Survived; // TODO: Will have to call with runthrough if exp is less than threshold

                    EFM_Manager.LoadBase(5); // Load autosave, which is right before the start of raid
                }
            }
            else if (inExtractionLastFrame)
            {
                extractionTimer = 0;
                inExtractionLastFrame = false;

                // TODO: update extraction timer display (disable it)
            }

            UpdateSun();
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

        public void UpdateSunAngle()
        {
            // Sun's X axis must rotate by 180 degrees in 43200 seconds
            // so 0.004166 degree in 1 second with an offset of 21600
            sun.transform.rotation = Quaternion.Euler(0.004166f * time + 21600, sun.transform.rotation.eulerAngles.y, sun.transform.rotation.eulerAngles.z);
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

        public override void Init()
        {
            Mod.instance.LogInfo("Raid init called");
            currentManager = this;

            LoadMapData();
            Mod.instance.LogInfo("Map data read");

            // Choose spawnpoints
            Transform spawnRoot = transform.GetChild(transform.childCount - 1).GetChild(Mod.chosenCharIndex);
            spawnPoint = spawnRoot.GetChild(UnityEngine.Random.Range(0, spawnRoot.childCount));

            Mod.instance.LogInfo("Got spawn");

            // Find extractions
            possibleExtractions = new List<Extraction>();
            Extraction bestCandidate = null; // Must be an extraction without appearance times (always available) and no other requirements. This will be the minimum extraction
            float farthestDistance = float.MinValue;
            Mod.instance.LogInfo("Init raid with map index: "+Mod.chosenMapIndex+", which has "+extractions.Count+" extractions");
            for(int extractionIndex = 0; extractionIndex < extractions.Count; ++extractionIndex)
            {
                Extraction currentExtraction = extractions[extractionIndex];
                currentExtraction.gameObject = gameObject.transform.GetChild(gameObject.transform.childCount - 2).GetChild(extractionIndex).gameObject;

                bool canBeMinimum = (currentExtraction.times == null || currentExtraction.times.Count == 0) &&
                                    (currentExtraction.itemRequirements == null || currentExtraction.itemRequirements.Count == 0) && 
                                    (currentExtraction.equipmentRequirements == null || currentExtraction.equipmentRequirements.Count == 0) &&
                                    (currentExtraction.role == Mod.chosenCharIndex || currentExtraction.role == 2) &&
                                    !currentExtraction.accessRequirement;
                float currentDistance = Vector3.Distance(spawnPoint.position, currentExtraction.gameObject.transform.position);
                Mod.instance.LogInfo("\tExtraction at index: "+extractionIndex+" has "+ currentExtraction.times.Count + " times and "+ currentExtraction.itemRequirements.Count+" items reqs. Its current distance from player is: "+ currentDistance);
                if (UnityEngine.Random.value <= ExtractionChanceByDist(currentDistance))
                {
                    Mod.instance.LogInfo("\t\tAdding this extraction to list possible extractions");
                    possibleExtractions.Add(currentExtraction);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in currentExtraction.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = currentExtraction;
                        extractionManager.raidManager = this;
                    }
                }
                   
                // Best candidate will be farthest because if there is at least one extraction point, we don't want to always have the nearest
                if(canBeMinimum && currentDistance > farthestDistance)
                {
                    bestCandidate = currentExtraction;
                }
            }
            if(bestCandidate != null)
            {
                if(!possibleExtractions.Contains(bestCandidate))
                {
                    Mod.instance.LogInfo("\t\tAdding candidate to list possible extractions");
                    possibleExtractions.Add(bestCandidate);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in bestCandidate.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = bestCandidate;
                        extractionManager.raidManager = this;
                    }
                }
            }
            else
            {
                Mod.instance.LogError("No minimum extraction found");
            }

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

            foreach (JToken doorData in Mod.mapData["maps"][Mod.chosenMapIndex]["doors"])
            {
                GameObject doorObject = doorRoot.GetChild(doorIndex).gameObject;
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
                        Mod.instance.LogInfo("\tTransfered mat to frame");
                    }
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material = doorObject.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material;
                    Mod.instance.LogInfo("\tTransfered mat to door");
                }

                // Set PMat if necessary
                if (doorData["mat"].ToString().Equals("metal"))
                {
                    doorInstance.GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                    Mod.instance.LogInfo("\tSet PMat to metal");
                }

                // Flip lock if necessary
                if ((bool)doorData["flipLock"])
                {
                    doorWrapper.flipLock = true;
                    if(deadBolt != null)
                    {
                        deadBolt.transform.Rotate(0, 180, 0);
                    }
                }

                // Set destructable
                if (!(bool)doorData["destructable"])
                {
                    Destroy(doorInstance.transform.GetChild(0).GetComponent<UberShatterable>()); // TODO: Verify if this works
                }

                // Set door to active
                doorInstance.SetActive(true);

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
                Mod.instance.LogInfo("\tSet door angle");

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

            // Spawn loose loot
            JObject locationDB = Mod.locationsDB[GetLocationDataIndex(Mod.chosenMapIndex)];
            Transform itemsRoot = transform.GetChild(1).GetChild(1).GetChild(2);
            List<string> missingForced = new List<string>();
            List<string> missingDynamic = new List<string>();
            // Forced, always spawns TODO: Unless player has it already? Unless player doesnt have the quest yet?
            foreach(JToken forced in locationDB["forced"])
            {
                JArray items = forced["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItems = new Dictionary<string, EFM_CustomItemWrapper>();
                for (int i=0; i < items.Count; ++i)
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
                        continue;
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
                    EFM_CustomItemWrapper itemWrapper = itemPrefab.GetComponent<EFM_CustomItemWrapper>();

                    // Check for stack
                    int amountToSpawn = items[i]["upd"] == null ? 1 : items[i]["upd"]["StackObjectCount"] == null ? 1 : (int)items[i]["upd"]["StackObjectCount"];
                    bool stackable = itemWrapper != null && itemWrapper.stackable;

                    // Spawn item(s)
                    if (!stackable)
                    {
                        // If not stackable, spawn individual instances of this item and place them under the same parent
                        for (int j = 0; j < amountToSpawn; ++j)
                        {
                            SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, forced, originalID);
                        }
                    }
                    else // Unstackable
                    {
                        GameObject itemObject = SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, forced, originalID);

                        // TODO: Set the item's stack
                        // TODO: itemObject.GetComponent<EFM_CustomItemWrapper>().stack = amountToSpawn;
                    }
                }
            }
            // TODO: Figure out how to spawn static loot
            // Dynamic, has chance of spawning based on rarity TODO: Which should be written to default item data
            foreach (JToken dynamic in locationDB["dynamic"])
            {
                JArray items = dynamic["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItems = new Dictionary<string, EFM_CustomItemWrapper>();
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
                        missingDynamic.Add(originalID);
                        continue;
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
                    EFM_CustomItemWrapper itemWrapper = itemPrefab.GetComponent<EFM_CustomItemWrapper>();

                    // Check for stack
                    int amountToSpawn = items[i]["upd"] == null ? 1 : items[i]["upd"]["StackObjectCount"] == null ? 1 : (int)items[i]["upd"]["StackObjectCount"];
                    bool stackable = itemWrapper != null && itemWrapper.stackable;

                    // Spawn item(s)
                    if (!stackable)
                    {
                        // If not stackable, spawn individual instances of this item and place them under the same parent
                        for (int j = 0; j < amountToSpawn; ++j)
                        {
                            SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, dynamic, originalID);
                        }
                    }
                    else // Unstackable
                    {
                        GameObject itemObject = SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, dynamic, originalID);

                        // TODO: Set the item's stack
                        // TODO: itemObject.GetComponent<EFM_CustomItemWrapper>().stack = amountToSpawn;
                    }
                }
            }

            // Spawn container loot
            // TODO: When we get containers implemented 

            // Output missing items
            if (Mod.instance.debug)
            {
                string text = "Raid with map index = " + Mod.chosenMapIndex + " was missing FORCED loose loot:\n";
                foreach(string id in missingForced)
                {
                    text += id + "\n";
                }
                text += "Raid with map index = " + Mod.chosenMapIndex + " was missing DYNAMIC loose loot:\n";
                foreach(string id in missingDynamic)
                {
                    text += id + "\n";
                }
                File.AppendAllText("BepinEx/Plugins/EscapeFromMeatov/ErrorLog.txt", text);
            }

            // Init time
            InitTime();

            // Get sun
            sun = transform.GetChild(1).GetChild(0).GetComponent<Light>();

            inRaid = true;

            init = true;
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

        private void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * EFM_Manager.meatovTimeMultiplier) % 86400);
            time = (scaledTime + Mod.chosenTimeIndex == 0 ? 0 : 43200) % 86400;
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * EFM_Manager.meatovTimeMultiplier;

            time %= 86400;
        }

        private GameObject SpawnLootItem(GameObject itemPrefab, Transform itemsRoot, string itemID, JToken itemData, Dictionary<string, EFM_CustomItemWrapper> spawnedItems, JToken spawnData, string originalID)
        {
            GameObject itemObject = GameObject.Instantiate(itemPrefab, itemsRoot);
            if (itemObject == null)
            {
                Mod.instance.LogError("Could not instantiate item prefab: " + itemID);
                return null;
            }

            // Position and rotate item
            if (itemData["parentId"] != null)
            {
                // Item has a parent which should be a previously spawned item
                // This parent should be a container of some sort so we need to add this item to the container
                if (spawnedItems.ContainsKey(itemData["parentId"].ToString()))
                {
                    EFM_CustomItemWrapper parent = spawnedItems[itemData["parentId"].ToString()];
                    bool boxMainContainer = parent.mainContainer.GetComponent<BoxCollider>() != null;
                    parent.AddItemToContainer(itemObject.GetComponent<EFM_CustomItemWrapper>().physicalObject);
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
                else
                {
                    Mod.instance.LogError("Attempted to spawn loose loot " + itemID + " with original ID " + originalID + " but parentID " + itemData["parentId"].ToString() + " does not exist in spawned items list");
                }
            }
            else
            {
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
            }

            // Add custom item wrapper to spawned items dict with _id as key
            if (spawnedItems.ContainsKey(itemData["_id"].ToString()))
            {
                spawnedItems.Add(itemData["_id"].ToString(), itemObject.GetComponent<EFM_CustomItemWrapper>());
            }

            return itemObject;
        }

        public override void InitUI()
        {
        }

        public void LoadMapData()
        {
            extractions = new List<Extraction>();
            Transform extractionRoot = transform.GetChild(3);

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
                extraction.itemRequirements = new List<string>();
                foreach (JToken itemRequirement in extractionData["itemRequirements"])
                {
                    extraction.itemRequirements.Add(itemRequirement.ToString());
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
    }

    class ExtractionManager : MonoBehaviour
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
            Mod.instance.LogInfo("Trigger enter called on "+gameObject.name+": "+collider.name);
            if (EFM_Raid_Manager.currentManager.currentExtraction == null)
            {
                // TODO: Check if player, if it is, set EFM_Raid_Manager.currentManager.currentExtraction = this if we dont already have a player collider and add this player collider to the list
                if (collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Hitbox_Neck") ||
                   collider.gameObject.name.Equals("Hitbox_Head") ||
                   collider.gameObject.name.Equals("Hitbox_Torso"))
                {
                    Mod.instance.LogInfo("Collider is player");
                    if (playerColliders.Count == 0)
                    {
                        EFM_Raid_Manager.currentManager.currentExtraction = this;
                    }

                    playerColliders.Add(collider);
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (playerColliders.Count > 0)
            {
                playerColliders.Remove(collider);

                if (playerColliders.Count == 0 && EFM_Raid_Manager.currentManager.currentExtraction == this)
                {
                    EFM_Raid_Manager.currentManager.currentExtraction = null;
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
        public List<string> itemRequirements;
        public List<string> equipmentRequirements;
        public List<string> itemBlacklist;
        public List<string> equipmentBlacklist;
        public int role;
        public bool accessRequirement;
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
}
