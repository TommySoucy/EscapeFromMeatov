using FistVR;
using H3MP.Networking;
using H3MP.Tracking;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class RaidManager : MonoBehaviour
    {
        public enum RaidStatus
        {
            Success,
            RunThrough,
            KIA,
            MIA
        }

        public static RaidManager instance { get; private set; }
        public static float defaultEnergyRate;
        public static float defaultHydrationRate;

        public float raidTime; // Amount of time to complete the raid, in seconds
        public int maxPMCCount;
        public List<Spawn> PMCSpawns;
        public int maxScavCount; // Max amount of scavs that can be active at once
        public float scavSpawnInterval;
        public List<ScavSpawn> scavSpawns; 
        public List<ScavBossSpawn> scavBossSpawns; 
        public List<Extraction> PMCExtractions; 
        public List<Extraction> scavExtractions; 
        public List<Transform> PMCNavPoints;
        public bool loadMapDataFromDB; // Whether we want to load the map's data from DB (If the data is from vanilla tarkov for example, and we already have the data)
        public string pathToData; // Only relevant if loadMapDataFromDB
        public List<LooseLootPoint> looseLootPoints;

        [NonSerialized]
        public bool control; // Whether we control the raid (Only relevant if MP)
        [NonSerialized]
        public Spawn spawn; // Spawn used
        [NonSerialized]
        public bool AIPMCSpawned;
        [NonSerialized]
        public int enemyIFF = 2; // Players are 0, Scavs are 1, PMCs and scav bosses get incremental IFF
        [NonSerialized]
        public float scavSpawnTimer;
        [NonSerialized]
        public int activeScavCount;
        [NonSerialized]
        public List<Extraction> activeExtractions;
        [NonSerialized]
        public float raidTimeLeft;
        [NonSerialized]
        public float time;

        public delegate void SpawnItemReturnDelegate(List<MeatovItem> itemsSpawned);

        public void Awake()
        {
            instance = this;

            Mod.currentLocationIndex = 2;

            // Remove hideout rates
            for (int i = 0; i < Mod.GetHealthCount(); ++i)
            {
                Mod.SetBasePositiveHealthRate(i, Mod.GetBasePositiveHealthRate(i) - HideoutController.defaultHealthRates[i]);
            }
            Mod.baseEnergyRate -= HideoutController.defaultEnergyRate;
            Mod.baseHydrationRate -= HideoutController.defaultHydrationRate;

            // Add raid rates
            Mod.baseEnergyRate -= defaultEnergyRate;
            Mod.baseHydrationRate -= defaultHydrationRate;
        }

        public void Start()
        {
            raidTimeLeft = raidTime;
            Mod.raidTime = 0;
            scavSpawnTimer = scavSpawnInterval;
            Mod.raidKills = new List<KillData>();

            Mod.UnsecureInventory();

            InitTime();

            UpdateControl();

            if (control)
            {
                Init();

                GM.CurrentSceneSettings.SosigKillEvent += OnSosigKill;
            }
            else // We don't control
            {
                enemyIFF = Networking.currentInstance.enemyIFF;
                activeScavCount = Networking.currentInstance.activeScavCount;

                AIPMCSpawned = Networking.currentInstance.AIPMCSpawned;

                // Request spawn from controller if necessary
                using (Packet packet = new Packet(Networking.requestSpawnPacketID))
                {
                    packet.Write(Networking.currentInstance.ID);
                    packet.Write(H3MP.GameManager.ID);
                    packet.Write(Mod.charChoicePMC);

                    if (ThreadManager.host)
                    {
                        ServerSend.SendTCPData(Networking.currentInstance.players[0], packet, true);
                    }
                    else
                    {
                        ClientSend.SendTCPData(packet, true);
                    }
                }

                // Store that we requested spawn so we can do it again if host changes before we receive it
                Networking.spawnRequested = true;
            }
        }

        public void OnSosigKill(Sosig sosig)
        {
            if (control)
            {
                AI ai = sosig.GetComponent<AI>();
                if(ai != null)
                {
                    // Update instance active scav count
                    if (ai.scav)
                    {
                        --activeScavCount;

                        if (Networking.currentInstance != null)
                        {
                            Networking.currentInstance.activeScavCount = activeScavCount;

                            using (Packet packet = new Packet(Networking.setRaidActiveScavCountPacketID))
                            {
                                packet.Write(Networking.currentInstance.ID);
                                packet.Write(activeScavCount);

                                if (ThreadManager.host)
                                {
                                    ServerSend.SendTCPDataToAll(packet, true);
                                }
                                else
                                {
                                    ClientSend.SendTCPData(packet, true);
                                }
                            }
                        }
                    }

                    // Process kill
                    if(ai.latestDamageSourceKillData != null)
                    {
                        Mod.OnKillInvoke(ai.latestDamageSourceKillData);
                        Mod.raidKills.Add(ai.latestDamageSourceKillData);
                        Mod.AddExperience(ai.latestDamageSourceKillData.baseExperienceReward + (ai.latestDamageSourceKillData.bodyPart == ConditionCounter.TargetBodyPart.Head ? 200 : 0));
                        ai.latestDamageSourceKillData = null;
                    }

                    // Spawn loot
                    List<MeatovItem> spawnedItems = SpawnItem(Mod.defaultItemData["000000000000000000000004"], 1, sosig.Links[0].transform.position + Vector3.up * 0.5f, UnityEngine.Random.rotation);
                    MeatovItem lootBox = spawnedItems[0];
                    ai.botInventory.Spawn(lootBox, ai.PMC);
                }
            }
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        public void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * Mod.meatovTimeMultiplier) % 86400);
            time = scaledTime;
        }

        public void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * Mod.meatovTimeMultiplier;
            time %= 86400;

            Mod.raidTime += Time.deltaTime;

            raidTimeLeft -= Time.deltaTime;
            if (raidTimeLeft <= 0)
            {
                EndRaid(RaidStatus.MIA);
            }
            else
            {
                if(raidTimeLeft <= 600)
                {
                    if (!Mod.extractionLimitUI.activeSelf)
                    {
                        Mod.extractionLimitUI.SetActive(true);
                    }
                    Mod.extractionLimitUIText.text = Mod.FormatTimeString(raidTimeLeft);
                }
                else
                {
                    if (Mod.extractionLimitUI.activeSelf)
                    {
                        Mod.extractionLimitUI.SetActive(false);
                    }
                }

                StatusUI.instance.extractionTimer.text = Mod.FormatTimeString(raidTimeLeft);
                if (raidTimeLeft < 600 && StatusUI.instance.extractionTimer.color == Color.black)
                {
                    StatusUI.instance.extractionTimer.color = Color.red;
                }
            }
        }

        public void Init()
        {
            if(Networking.currentInstance == null) // SP
            {
                // Spawn
                spawn = Mod.charChoicePMC ? PMCSpawns[UnityEngine.Random.Range(0, PMCSpawns.Count)] : scavSpawns[UnityEngine.Random.Range(0, scavSpawns.Count)];
                Spawn(spawn);
            }
            else // MP
            {
                // Choose spawn
                if (Mod.charChoicePMC)
                {
                    int spawnIndex = Networking.currentInstance.PMCSpawnIndices[UnityEngine.Random.Range(0, Networking.currentInstance.PMCSpawnIndices.Count)];
                    Networking.currentInstance.ConsumeSpawn(spawnIndex, true, PMCSpawns.Count, scavSpawns.Count, true);
                    spawn = PMCSpawns[spawnIndex];
                    Spawn(spawn);
                }
                else
                {
                    int spawnIndex = Networking.currentInstance.ScavSpawnIndices[UnityEngine.Random.Range(0, Networking.currentInstance.ScavSpawnIndices.Count)];
                    Networking.currentInstance.ConsumeSpawn(spawnIndex, false, PMCSpawns.Count, scavSpawns.Count, true);
                    spawn = scavSpawns[spawnIndex];
                    Spawn(spawn);
                }

                // Process already existing spawn requests
                for(int i=0; i < Networking.spawnRequests.Count; ++i)
                {
                    int spawnIndex = -1;
                    if (Networking.spawnRequests[i].PMC)
                    {
                        spawnIndex = Networking.currentInstance.PMCSpawnIndices[UnityEngine.Random.Range(0, Networking.currentInstance.PMCSpawnIndices.Count)];
                    }
                    else
                    {
                        spawnIndex = Networking.currentInstance.ScavSpawnIndices[UnityEngine.Random.Range(0, Networking.currentInstance.ScavSpawnIndices.Count)];
                    }
                    Networking.currentInstance.ConsumeSpawn(spawnIndex, Networking.spawnRequests[i].PMC, RaidManager.instance.PMCSpawns.Count, RaidManager.instance.scavSpawns.Count, true);

                    using (Packet newPacket = new Packet(Networking.spawnReturnPacketID))
                    {
                        newPacket.Write(Networking.spawnRequests[i].instanceID);
                        newPacket.Write(Networking.spawnRequests[i].playerID);
                        newPacket.Write(spawnIndex);
                        newPacket.Write(Networking.spawnRequests[i].PMC);

                        ServerSend.SendTCPData(Networking.spawnRequests[i].playerID, newPacket, true);
                    }
                }
                Networking.spawnRequests.Clear();
            }

            SpawnLooseLoot();

            SpawnScavBosses();
        }

        public void SpawnLooseLoot()
        {
            if (loadMapDataFromDB)
            {
                looseLootPoints = new List<LooseLootPoint>();

                JObject looseLootData = JObject.Parse(File.ReadAllText(pathToData));

                TODO: // spawnpointsForced should only be spawned if at least one of the items is needed for an active quest
                JArray forced = looseLootData["spawnpointsForced"] as JArray;
                for(int i=0; i < forced.Count; ++i)
                {
                    AddLooseLootPoint(forced[i], "LooseLootPointForced", i);
                }
                JArray normal = looseLootData["spawnpoints"] as JArray;
                for(int i=0; i < forced.Count; ++i)
                {
                    AddLooseLootPoint(forced[i], "LooseLootPoint", i);
                }
            }

            if(looseLootPoints.Count > 0)
            {
                for(int i=0; i < looseLootPoints.Count; ++i)
                {
                    switch (looseLootPoints[i].mode)
                    {
                        case LooseLootPoint.Mode.SpecificItems:
                            int totalWeight = looseLootPoints[i].totalProbabilityWeight;
                            if (totalWeight == -1)
                            {
                                totalWeight = 0;
                                for (int j=0; j<looseLootPoints[i].itemProbabilityWeight.Count;++j)
                                {
                                    totalWeight += looseLootPoints[i].itemProbabilityWeight[j];
                                }
                            }

                            int randSelection = UnityEngine.Random.Range(0, totalWeight);
                            int selectedIndex = -1;
                            for (int j = 0; j < looseLootPoints[i].itemProbabilityWeight.Count; ++j)
                            {
                                totalWeight += looseLootPoints[i].itemProbabilityWeight[j];

                                if (randSelection < totalWeight)
                                {
                                    selectedIndex = j;
                                    break;
                                }
                            }

                            string tarkovID = looseLootPoints[i].items[selectedIndex];
                            if (Mod.defaultItemData.TryGetValue(tarkovID, out MeatovItemData specificItemData))
                            {
                                Vector3 position = looseLootPoints[i].transform.position;
                                Quaternion rotation = looseLootPoints[i].transform.rotation;
                                if(looseLootPoints[i].transformData != null && looseLootPoints[i].transformData.Count > 0)
                                {
                                    KeyValuePair<Vector3, Vector3> data = looseLootPoints[i].transformData[UnityEngine.Random.Range(0, looseLootPoints[i].transformData.Count)];
                                    position = data.Key;
                                    rotation = Quaternion.Euler(data.Value);
                                }
                                if (looseLootPoints[i].randomRotation)
                                {
                                    rotation = UnityEngine.Random.rotation;
                                }
                                SpawnItem(specificItemData, looseLootPoints[i].itemStack[selectedIndex], position, rotation, true);
                            }
                            else
                            {
                                if(!Mod.oldItemMap.TryGetValue(tarkovID, out JToken itemMapValue))
                                {
                                    Mod.LogError("Raid tried to spawn specific item loose loot "+ tarkovID + " but item is missing");
                                }
                            }
                            break;
                        case LooseLootPoint.Mode.Category:
                            if(Mod.itemsByParents.TryGetValue(looseLootPoints[i].category, out List<MeatovItemData> categoryItemDatas))
                            {
                                MeatovItemData categoryItem = categoryItemDatas[UnityEngine.Random.Range(0, categoryItemDatas.Count)];
                                Vector3 position = looseLootPoints[i].transform.position;
                                Quaternion rotation = looseLootPoints[i].transform.rotation;
                                if (looseLootPoints[i].transformData != null && looseLootPoints[i].transformData.Count > 0)
                                {
                                    KeyValuePair<Vector3, Vector3> data = looseLootPoints[i].transformData[UnityEngine.Random.Range(0, looseLootPoints[i].transformData.Count)];
                                    position = data.Key;
                                    rotation = Quaternion.Euler(data.Value);
                                }
                                if (looseLootPoints[i].randomRotation)
                                {
                                    rotation = UnityEngine.Random.rotation;
                                }
                                SpawnItem(categoryItem, 1, position, rotation, true);
                            }
                            else
                            {
                                Mod.LogError("Could not find itemsByParents entry for " + looseLootPoints[i].category+" for loose loot point");
                            }
                            break;
                        case LooseLootPoint.Mode.Rarity:
                            if (Mod.itemsByRarity.TryGetValue(looseLootPoints[i].rarity, out List<MeatovItemData> rarityItemDatas))
                            {
                                MeatovItemData rarityItem = rarityItemDatas[UnityEngine.Random.Range(0, rarityItemDatas.Count)];
                                Vector3 position = looseLootPoints[i].transform.position;
                                Quaternion rotation = looseLootPoints[i].transform.rotation;
                                if (looseLootPoints[i].transformData != null && looseLootPoints[i].transformData.Count > 0)
                                {
                                    KeyValuePair<Vector3, Vector3> data = looseLootPoints[i].transformData[UnityEngine.Random.Range(0, looseLootPoints[i].transformData.Count)];
                                    position = data.Key;
                                    rotation = Quaternion.Euler(data.Value);
                                }
                                if (looseLootPoints[i].randomRotation)
                                {
                                    rotation = UnityEngine.Random.rotation;
                                }
                                SpawnItem(rarityItem, 1, position, rotation, true);
                            }
                            else
                            {
                                Mod.LogError("Could not find itemsByRarity entry for " + looseLootPoints[i].rarity + " for loose loot point");
                            }
                            break;
                    }
                }
            }
            else
            {
                Mod.LogError("No loose loot points, no loose loot will be spawned");
            }
        }

        public List<MeatovItem> SpawnItem(MeatovItemData itemData, int amount, Vector3 position, Quaternion rotation, bool foundInRaid = false, SpawnItemReturnDelegate del = null)
        {
            int amountToSpawn = amount;
            if (itemData.index == -1)
            {
                // Spawn vanilla item will handle the updating of proper elements
                AnvilManager.Run(SpawnVanillaItem(itemData, amountToSpawn, position, rotation, foundInRaid, del));

                return null;
            }
            else
            {
                GameObject itemPrefab = Mod.GetItemPrefab(itemData.index);
                List<GameObject> objectsList = new List<GameObject>();
                List<MeatovItem> itemsSpawned = new List<MeatovItem>();
                while (amountToSpawn > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPrefab);
                    MeatovItem meatovItem = spawnedItem.GetComponent<MeatovItem>();
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;
                    objectsList.Add(spawnedItem);

                    // Set stack and remove amount to spawn
                    if (meatovItem.maxStack > 1)
                    {
                        if (amountToSpawn > meatovItem.maxStack)
                        {
                            meatovItem.stack = meatovItem.maxStack;
                            amountToSpawn -= meatovItem.maxStack;
                        }
                        else // amountToSpawn <= itemCIW.maxStack
                        {
                            meatovItem.stack = amountToSpawn;
                            amountToSpawn = 0;
                        }
                    }
                    else
                    {
                        --amountToSpawn;
                    }

                    spawnedItem.transform.position = position;
                    spawnedItem.transform.rotation = rotation;

                    itemsSpawned.Add(meatovItem);
                }

                if (del != null)
                {
                    del(itemsSpawned);
                }

                return itemsSpawned;
            }
        }

        public IEnumerator SpawnVanillaItem(MeatovItemData itemData, int count, Vector3 position, Quaternion rotation, bool foundInRaid = false, SpawnItemReturnDelegate del = null)
        {
            yield return IM.OD[itemData.H3ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[itemData.H3ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.LogError("Failed to get vanilla prefab for " + itemData.tarkovID + ":" + itemData.H3ID + " to spawn in containment volume " + name);
                yield break;
            }
            FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
            GameObject itemObject = null;
            List<MeatovItem> itemsSpawned = new List<MeatovItem>();
            if (physObj is FVRFireArmRound && count > 1) // Multiple rounds, must spawn an ammobox
            {
                int countLeft = count;
                float boxCountLeft = count / 120.0f;
                while (boxCountLeft > 0)
                {
                    int amount = 0;
                    if (countLeft > 30)
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(716));

                        if (countLeft <= 120)
                        {
                            amount = countLeft;
                            countLeft = 0;
                        }
                        else
                        {
                            amount = 120;
                            countLeft -= 120;
                        }
                    }
                    else
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(715));

                        amount = countLeft;
                        countLeft = 0;
                    }

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;
                    FVRFireArmMagazine asMagazine = meatovItem.physObj as FVRFireArmMagazine;
                    FVRFireArmRound round = physObj as FVRFireArmRound;
                    asMagazine.RoundType = round.RoundType;
                    meatovItem.roundClass = round.RoundClass;
                    for (int j = 0; j < amount; ++j)
                    {
                        asMagazine.AddRound(meatovItem.roundClass, false, false);
                    }

                    itemObject.transform.position = position;
                    itemObject.transform.rotation = rotation;

                    boxCountLeft = countLeft / 120.0f;

                    itemsSpawned.Add(meatovItem);
                }
            }
            else // Not a round, or just 1, spawn as normal
            {
                for (int i = 0; i < count; ++i)
                {
                    itemObject = GameObject.Instantiate(itemPrefab);

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;

                    itemObject.transform.position = position + Vector3.up * 0.2f;
                    itemObject.transform.rotation = rotation;

                    itemsSpawned.Add(meatovItem);
                }
            }

            if (del != null)
            {
                del(itemsSpawned);
            }

            yield break;
        }

        public void AddLooseLootPoint(JToken pointData, string objectName, int index)
        {
            JToken templateData = pointData["template"];
            GameObject looseLootPointObject = new GameObject(objectName + index);
            LooseLootPoint looseLootPoint = looseLootPointObject.AddComponent<LooseLootPoint>();
            looseLootPoint.probability = (float)pointData["probability"];
            looseLootPoint.isKinematic = !(bool)templateData["useGravity"];
            JToken positionData = templateData["Position"];
            looseLootPointObject.transform.position = new Vector3((float)positionData["x"], (float)positionData["y"], (float)positionData["z"]);
            looseLootPoint.randomRotation = (bool)templateData["randomRotation"];
            if (!looseLootPoint.randomRotation)
            {
                JToken rotationData = templateData["Rotation"];
                looseLootPointObject.transform.rotation = Quaternion.Euler((float)rotationData["x"], (float)rotationData["y"], (float)rotationData["z"]);
            }
            if ((bool)templateData["IsGroupPosition"])
            {
                looseLootPoint.transformData = new List<KeyValuePair<Vector3, Vector3>>();

                JArray groupPositions = templateData["GroupPositions"] as JArray;
                for (int i = 0; i < groupPositions.Count; ++i)
                {
                    JToken groupPositionData = groupPositions[i]["Position"];
                    Vector3 position = new Vector3((float)groupPositionData["x"], (float)groupPositionData["y"], (float)groupPositionData["z"]);
                    Vector3 rotation = Vector3.zero;
                    if (!looseLootPoint.randomRotation)
                    {
                        JToken groupRotationData = groupPositions[i]["Rotation"];
                        rotation = new Vector3((float)groupRotationData["x"], (float)groupRotationData["y"], (float)groupRotationData["z"]);
                    }

                    looseLootPoint.transformData.Add(new KeyValuePair<Vector3, Vector3>(position, rotation));
                }
            }
            looseLootPoint.mode = LooseLootPoint.Mode.SpecificItems;
            looseLootPoint.items = new List<string>();
            looseLootPoint.itemProbabilityWeight = new List<int>();
            looseLootPoint.itemStack = new List<int>();
            JArray items = templateData["items"] as JArray;
            JArray distribution = null;
            if(pointData["itemDistribution"] != null)
            {
                distribution = pointData["itemDistribution"] as JArray;
            }
            int totalWeight = 0;
            Dictionary<string, int> parentEntries = new Dictionary<string, int>();
            Dictionary<string, int> parentEntrieStacks = new Dictionary<string, int>();
            for(int i = 0; i < items.Count; ++i)
            {
                if (items[i]["parentId"] != null && items[i]["upd"] != null && items[i]["upd"]["StackObjectsCount"] != null)
                {
                    string parentID = items[i]["parentId"].ToString();
                    int parentIndex = -1;
                    if (parentEntries.TryGetValue(parentID, out parentIndex))
                    {
                        looseLootPoint.itemStack[parentIndex] = (int)items[i]["upd"]["StackObjectsCount"];
                    }
                    else
                    {
                        parentEntrieStacks.Add(parentID, (int)items[i]["upd"]["StackObjectsCount"]);
                    }
                    continue;
                }
                string tarkovID = items[i]["_tpl"].ToString();
                string lookupID = items[i]["_id"].ToString();
                parentEntries.Add(tarkovID, looseLootPoint.items.Count);
                looseLootPoint.items.Add(tarkovID);
                int parentStack = 1;
                if(items[i]["upd"] != null && items[i]["upd"]["StackObjectsCount"] != null)
                {
                    looseLootPoint.itemStack.Add((int)items[i]["upd"]["StackObjectsCount"]);
                }
                else if(parentEntrieStacks.TryGetValue(lookupID, out parentStack))
                {
                    looseLootPoint.itemStack.Add(parentStack);
                }
                else
                {
                    looseLootPoint.itemStack.Add(1);
                }
                if (distribution == null)
                {
                    looseLootPoint.itemProbabilityWeight.Add(1);
                    ++totalWeight;
                }
                else
                {

                    bool found = false;
                    for(int j=0; j < distribution.Count; ++j)
                    {
                        if (distribution[j]["composedKey"]["key"].ToString().Equals(lookupID))
                        {
                            int relativeProbability = (int)distribution[j]["relativeProbability"];
                            looseLootPoint.itemProbabilityWeight.Add(relativeProbability);
                            totalWeight += relativeProbability;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        looseLootPoint.itemProbabilityWeight.Add(1);
                        ++totalWeight;
                    }
                }
            }
            looseLootPoint.totalProbabilityWeight = totalWeight;
        }

        public void Spawn(Spawn spawn)
        {
            Vector3 spawnPosition = GetSpawnPosition(spawn);
            GM.CurrentSceneSettings.DeathResetPoint = spawn.transform;
            GM.CurrentMovementManager.TeleportToPoint(spawnPosition, false);

            // Choose extractions
            Mod.LogInfo("Choosing extractions");
            List<Extraction> extractionsToUse = Mod.charChoicePMC ? PMCExtractions : scavExtractions;
            List<Extraction> noRestrictionExtractions = new List<Extraction>();
            List<Extraction> restrictionExtractions = new List<Extraction>();
            for(int i=0; i < extractionsToUse.Count; ++i)
            {
                Extraction extraction = extractionsToUse[i];
                Mod.LogInfo("\tChecking extraction: "+ extraction.extractionName);
                extraction.distToSpawn = Vector3.Distance(spawnPosition, extraction.transform.position);

                // Find which list to add to depending if the extraction has any restrictions
                List<Extraction> extractionListToAdd = null;
                if ((extraction.activeTimes != null && extraction.activeTimes.Count > 0)
                    || (extraction.itemRequirements != null && extraction.itemRequirements.Count > 0)
                    || (extraction.itemWhitelist != null && extraction.itemWhitelist.Count > 0)
                    || (extraction.itemBlacklist != null && extraction.itemBlacklist.Count > 0))
                {
                    Mod.LogInfo("\t\tGot restrictions");
                    extractionListToAdd = restrictionExtractions;
                }
                else
                {
                    Mod.LogInfo("\t\tNO restrictions");
                    extractionListToAdd = noRestrictionExtractions;
                }

                // Add to the list in order of distance to spawn
                if (extractionListToAdd.Count == 0)
                {
                    extractionListToAdd.Add(extraction);
                }
                else
                {
                    for (int j = 0; j < extractionListToAdd.Count; ++j)
                    {
                        if (extraction.distToSpawn < extractionListToAdd[j].distToSpawn)
                        {
                            extractionListToAdd.Insert(j, extraction);
                            break;
                        }
                    }
                }
            }

            Mod.LogInfo("\tFinal no restrict extraction list:"); 
            for (int i = 0; i < noRestrictionExtractions.Count; ++i)
            {
                Mod.LogInfo("\t\t"+ noRestrictionExtractions[i].name);
            }
            Mod.LogInfo("\tFinal restrict extraction list:"); 
            for (int i = 0; i < restrictionExtractions.Count; ++i)
            {
                Mod.LogInfo("\t\t"+ restrictionExtractions[i].name);
            }

            // Add a third (min 1) of the farthest no restriction extractions we found
            activeExtractions = new List<Extraction>();
            if (noRestrictionExtractions.Count == 0)
            {
                Mod.LogError("There are no extraction without restrictions, possibility of extraction from this raid is not guaranteed");
            }
            else
            {
                int count = Mathf.Max(1, noRestrictionExtractions.Count / 3);
                for (int i = noRestrictionExtractions.Count - 1, j = 0; i >= 0 && j < count; --i, ++j)
                {
                    noRestrictionExtractions[i].active = true;
                    noRestrictionExtractions[i].extractionsIndex = activeExtractions.Count;
                    activeExtractions.Add(noRestrictionExtractions[i]);
                }
            }

            // Add half (min 1) of the restriction extractions, chosen randomly
            if (restrictionExtractions.Count > 0)
            {
                int count = Mathf.Max(1, restrictionExtractions.Count / 2);
                for (int j = 0; j < count; ++j)
                {
                    int rand = UnityEngine.Random.Range(0, restrictionExtractions.Count);
                    if (!restrictionExtractions[rand].active)
                    {
                        restrictionExtractions[rand].active = true;
                        restrictionExtractions[rand].extractionsIndex = activeExtractions.Count;
                        activeExtractions.Add(restrictionExtractions[rand]);
                    }
                }
            }

            // Set extractions in StatusUI
            StatusUI.instance.extractionsParent.SetActive(true);
            for (int i = 0; i < activeExtractions.Count; ++i) 
            {
                GameObject extractionCard = Instantiate(StatusUI.instance.extractionCardPrefab, StatusUI.instance.extractionsParent.transform);
                extractionCard.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "EXFIL" + (i < 10 ? "0" + i : i.ToString()) + " " + activeExtractions[i].extractionName;
                activeExtractions[i].cardRequirementText = extractionCard.transform.GetChild(0).GetChild(1).GetComponent<Text>();
                if (activeExtractions[i].activeTimes != null && activeExtractions[i].activeTimes.Count > 0)
                {
                    extractionCard.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                }
                extractionCard.SetActive(true);
            }
        }

        public Vector3 GetSpawnPosition(Spawn spawn)
        {
            if (spawn.square)
            {
                return new Vector3(spawn.transform.position.x + UnityEngine.Random.Range(-spawn.range, spawn.range), spawn.transform.position.y, spawn.transform.position.z + UnityEngine.Random.Range(-spawn.range, spawn.range));
            }
            else
            {
                float angle = UnityEngine.Random.Range(-Mathf.PI, Mathf.PI);
                float range = UnityEngine.Random.Range(0, spawn.range);
                return new Vector3(spawn.transform.position.x + Mathf.Cos(angle) * range, spawn.transform.position.y, spawn.transform.position.z + Mathf.Sin(angle) * range);
            }
        }

        public void UpdateRates()
        {
            for (int i = 0; i < Mod.GetHealthCount(); ++i)
            {
                Mod.SetHealth(i, Mathf.Max(0, Mathf.Min(Mod.GetCurrentMaxHealth(i), Mod.GetHealth(i) + Mod.GetCurrentHealthRate(i) + Mod.GetCurrentNonLethalHealthRate(i) * Time.deltaTime)));
            }
            Mod.hydration = Mathf.Max(0, Mathf.Min(Mod.currentMaxHydration, Mod.hydration + Mod.currentHydrationRate * Time.deltaTime));
            Mod.energy = Mathf.Max(0, Mathf.Min(Mod.currentMaxEnergy, Mod.energy + Mod.currentEnergyRate * Time.deltaTime));
        }

        public void Update()
        {
            UpdateControl();
            if (control)
            {
                if(activeScavCount < maxScavCount)
                {
                    scavSpawnTimer -= Time.deltaTime;
                    if(scavSpawnTimer < 0)
                    {
                        SpawnScav();

                        scavSpawnTimer = scavSpawnInterval;
                    }
                }

                // Check if can spawn initial AI PMC
                if (!AIPMCSpawned)
                {
                    if (Networking.currentInstance == null)
                    {
                        int spawnCount = Mod.charChoicePMC ? maxPMCCount - 1 : maxPMCCount;
                        for (int i = 0; i < spawnCount; ++i)
                        {
                            SpawnPMC();
                        }
                        AIPMCSpawned = true;
                    }
                    else if (Networking.currentInstance.consumedSpawnCount >= Networking.currentInstance.players.Count)
                    {
                        int spawnCount = maxPMCCount - (PMCSpawns.Count - Networking.currentInstance.PMCSpawnIndices.Count);
                        for (int i = 0; i < spawnCount; ++i)
                        {
                            SpawnPMC();
                        }
                        AIPMCSpawned = true;
                        Networking.currentInstance.AIPMCSpawned = true;
                        using (Packet packet = new Packet(Networking.setInstanceAIPMCSpawnedPacketID))
                        {
                            packet.Write(Networking.currentInstance.ID);

                            if (ThreadManager.host)
                            {
                                ServerSend.SendTCPDataToAll(packet, true);
                            }
                            else
                            {
                                ClientSend.SendTCPData(packet, true);
                            }
                        }
                    }
                }
            }

            UpdateTime();

            UpdateRates();
        }

        public void UpdateControl()
        {
            control = Networking.currentInstance == null || Networking.currentInstance.players[0] == H3MP.GameManager.ID;
        }

        public void SpawnPMC()
        {
            // Find spawn
            Spawn spawn = null;
            List<Spawn> spawns = new List<Spawn>(PMCSpawns);
            if (Networking.currentInstance == null)
            {
                spawns.Remove(this.spawn);
                spawn = spawns[UnityEngine.Random.Range(0, spawns.Count)];
            }
            else
            {
                if(Networking.currentInstance.PMCSpawnIndices.Count > 0)
                {
                    int index = Networking.currentInstance.PMCSpawnIndices[UnityEngine.Random.Range(0, Networking.currentInstance.PMCSpawnIndices.Count)];
                    Networking.currentInstance.ConsumeSpawn(index, true, PMCSpawns.Count, scavSpawns.Count, true);
                    spawn = PMCSpawns[index];
                }
                else
                {
                    Mod.LogError("Not enough PMC spawns for AI PMC, will spawn at random spawn");
                    spawn = PMCSpawns[UnityEngine.Random.Range(0, PMCSpawns.Count)];
                }
            }

            // Decide side
            bool USEC = UnityEngine.Random.value < 0.5f;

            if(Mod.botData.TryGetValue(USEC ? "usec" : "bear", out JObject botData))
            {
                // Generate sosig template
                // ADS time will be faster due to SosigHand.Hold patch
                // Supression will decrease faster due to SuppresionUpdate patch
                SosigConfigTemplate sosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                sosigTemplate.EntityRecognitionSpeedMultiplier = 20;
                sosigTemplate.AggroSensitivityMultiplier = 20;
                sosigTemplate.CombatTargetIdentificationSpeedMultiplier = 20;
                sosigTemplate.RegistersPassiveThreats = true;
                sosigTemplate.DoesDropWeaponsOnBallistic = false;
                sosigTemplate.ShudderThreshold = 1000; // High number, sosig should probably just never shudder
                sosigTemplate.ConfusionThreshold = 1000; // High number, sosig should probably just never be consfused
                sosigTemplate.ConfusionMultiplier = 0;
                sosigTemplate.ConfusionTimeMax = 0;
                // Note that stun settings are unmodified
                sosigTemplate.CanBeKnockedOut = false;
                sosigTemplate.MaxUnconsciousTime = 0;
                sosigTemplate.CanBeGrabbed = false;
                sosigTemplate.SuppressionMult = 0.05f; // Minimum 20 supression events to fully suppress
                sosigTemplate.LinkDamageMultipliers = new List<float>
                {
                    3, // Head
                    2, // Torso
                    2, // Upper
                    1  // Lower
                };
                sosigTemplate.LinkStaggerMultipliers = new List<float>();
                sosigTemplate.StartingLinkIntegrity = new List<Vector2>();
                sosigTemplate.StartingChanceBrokenJoint = new List<float>();
                for (int i= 0;i< 4; ++i)
                {
                    sosigTemplate.LinkStaggerMultipliers.Add(1);
                    sosigTemplate.StartingLinkIntegrity.Add(new Vector2(100,100));
                    sosigTemplate.StartingChanceBrokenJoint.Add(0);
                }

                // Generate inventory
                BotInventory botInventory = new BotInventory(botData);

                // Generate outfit from inventory
                List<FVRObject>[] botOutfit = botInventory.GetOutfit(true);

                AnvilManager.Run(SpawnSosig(spawn, botInventory, sosigTemplate, botOutfit, enemyIFF++, 300, true, USEC, false, USEC ? "usec" : "bear"));

                // Loop IFF once we've reached max
                if(enemyIFF > 31)
                {
                    enemyIFF = 2;
                }

                if(Networking.currentInstance != null)
                {
                    Networking.currentInstance.enemyIFF = enemyIFF;

                    using (Packet packet = new Packet(Networking.setRaidEnemyIFFPacketID))
                    {
                        packet.Write(Networking.currentInstance.ID);
                        packet.Write(enemyIFF);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
                }
            }
            else
            {
                Mod.LogError("Could not get botdata to spawn PMC");
            }
        }

        public void SpawnScav()
        {
            // Find spawn
            ScavSpawn spawn = scavSpawns[UnityEngine.Random.Range(0, scavSpawns.Count)];
            string botDataName = null;
            if (spawn.random)
            {
                if(UnityEngine.Random.value < 0.5f)
                {
                    botDataName = "assault";
                }
                else
                {
                    botDataName = "marksman";
                }
            }
            else
            {
                if (spawn.marksman)
                {
                    botDataName = "marksman";
                }
                else
                {
                    botDataName = "assault";
                }
            }

            if(Mod.botData.TryGetValue(botDataName, out JObject botData))
            {
                // Generate sosig template
                // ADS time will be faster due to SosigHand.Hold patch
                // Supression will decrease faster due to SuppresionUpdate patch
                SosigConfigTemplate sosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                sosigTemplate.EntityRecognitionSpeedMultiplier = 20;
                sosigTemplate.AggroSensitivityMultiplier = 20;
                sosigTemplate.CombatTargetIdentificationSpeedMultiplier = 20;
                sosigTemplate.RegistersPassiveThreats = true;
                sosigTemplate.DoesDropWeaponsOnBallistic = false;
                sosigTemplate.ShudderThreshold = 1000; // High number, sosig should probably just never shudder
                sosigTemplate.ConfusionThreshold = 1000; // High number, sosig should probably just never be consfused
                sosigTemplate.ConfusionMultiplier = 0;
                sosigTemplate.ConfusionTimeMax = 0;
                // Note that stun settings are unmodified
                sosigTemplate.CanBeKnockedOut = false;
                sosigTemplate.MaxUnconsciousTime = 0;
                sosigTemplate.CanBeGrabbed = false;
                sosigTemplate.SuppressionMult = 0.05f; // Minimum 20 supression events to fully suppress
                sosigTemplate.LinkDamageMultipliers = new List<float>
                {
                    3, // Head
                    2, // Torso
                    2, // Upper
                    1  // Lower
                };
                sosigTemplate.LinkStaggerMultipliers = new List<float>();
                sosigTemplate.StartingLinkIntegrity = new List<Vector2>();
                sosigTemplate.StartingChanceBrokenJoint = new List<float>();
                for (int i = 0; i < 4; ++i)
                {
                    sosigTemplate.LinkStaggerMultipliers.Add(1);
                    sosigTemplate.StartingLinkIntegrity.Add(new Vector2(100, 100));
                    sosigTemplate.StartingChanceBrokenJoint.Add(0);
                }

                // Generate inventory
                BotInventory botInventory = new BotInventory(botData);

                // Generate outfit from inventory
                List<FVRObject>[] botOutfit = botInventory.GetOutfit(true);

                AnvilManager.Run(SpawnSosig(spawn, botInventory, sosigTemplate, botOutfit, 1, 100, false, false, true, botDataName));

                ++activeScavCount;

                if (Networking.currentInstance != null)
                {
                    Networking.currentInstance.activeScavCount = activeScavCount;

                    using (Packet packet = new Packet(Networking.setRaidActiveScavCountPacketID))
                    {
                        packet.Write(Networking.currentInstance.ID);
                        packet.Write(activeScavCount);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
                }
            }
            else
            {
                Mod.LogError("Could not get botdata to spawn scav "+botDataName);
            }
        }

        public void SpawnScavBosses()
        {
            Dictionary<ScavBossSpawn, byte> consumedSpawns = new System.Collections.Generic.Dictionary<ScavBossSpawn, byte>();
            for(int i=0; i < scavBossSpawns.Count; ++i)
            {
                if (consumedSpawns.ContainsKey(scavBossSpawns[i]))
                {
                    continue;
                }

                List<ScavBossSpawn> spawnGroup = scavBossSpawns[i].spawnGroup == null ? new List<ScavBossSpawn>() : scavBossSpawns[i].spawnGroup;
                if (!spawnGroup.Contains(scavBossSpawns[i]))
                {
                    spawnGroup.Add(scavBossSpawns[i]);
                }
                for (int j = 0; j < spawnGroup.Count; ++j)
                {
                    consumedSpawns.Add(spawnGroup[j], 0);
                }

                int totalWeight = scavBossSpawns[i].totalGroupWeight;
                int randSelection = UnityEngine.Random.Range(0, totalWeight);
                int currentWeight = 0;
                ScavBossSpawn selectedSpawn = null;
                for(int j=0; j < spawnGroup.Count; ++j)
                {
                    currentWeight += spawnGroup[j].groupWeight;
                    if (randSelection < currentWeight)
                    {
                        selectedSpawn = spawnGroup[j];
                    }
                }

                if (selectedSpawn != null)
                {
                    if(UnityEngine.Random.value > selectedSpawn.probability)
                    {
                        continue;
                    }

                    if (Mod.botData.TryGetValue(selectedSpawn.bossID, out JObject botData))
                    {
                        // Generate sosig template
                        // ADS time will be faster due to SosigHand.Hold patch
                        // Supression will decrease faster due to SuppresionUpdate patch
                        SosigConfigTemplate sosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                        sosigTemplate.EntityRecognitionSpeedMultiplier = 20;
                        sosigTemplate.AggroSensitivityMultiplier = 20;
                        sosigTemplate.CombatTargetIdentificationSpeedMultiplier = 20;
                        sosigTemplate.RegistersPassiveThreats = true;
                        sosigTemplate.DoesDropWeaponsOnBallistic = false;
                        sosigTemplate.ShudderThreshold = 1000; // High number, sosig should probably just never shudder
                        sosigTemplate.ConfusionThreshold = 1000; // High number, sosig should probably just never be consfused
                        sosigTemplate.ConfusionMultiplier = 0;
                        sosigTemplate.ConfusionTimeMax = 0;
                        // Note that stun settings are unmodified
                        sosigTemplate.CanBeKnockedOut = false;
                        sosigTemplate.MaxUnconsciousTime = 0;
                        sosigTemplate.CanBeGrabbed = false;
                        sosigTemplate.SuppressionMult = 0.05f; // Minimum 20 supression events to fully suppress
                        sosigTemplate.LinkDamageMultipliers = new List<float>
                        {
                            3, // Head
                            2, // Torso
                            2, // Upper
                            1  // Lower
                        };
                        sosigTemplate.LinkStaggerMultipliers = new List<float>();
                        sosigTemplate.StartingLinkIntegrity = new List<Vector2>();
                        sosigTemplate.StartingChanceBrokenJoint = new List<float>();
                        for (int j = 0; j < 4; ++j)
                        {
                            sosigTemplate.LinkStaggerMultipliers.Add(1);
                            sosigTemplate.StartingLinkIntegrity.Add(new Vector2(100, 100));
                            sosigTemplate.StartingChanceBrokenJoint.Add(0);
                        }

                        // Generate inventory
                        BotInventory botInventory = new BotInventory(botData);

                        // Generate outfit from inventory
                        List<FVRObject>[] botOutfit = botInventory.GetOutfit(true);

                        AnvilManager.Run(SpawnSosig(selectedSpawn, botInventory, sosigTemplate, botOutfit, enemyIFF, 300, false, false, false, selectedSpawn.bossID));

                        // Spawn squadmembers if necessary
                        if(selectedSpawn.squadMembers != null && selectedSpawn.squadMembers.Count > 0)
                        {
                            if (selectedSpawn.spawnAllSquadMembers)
                            {
                                for (int j = 0; j < selectedSpawn.squadMembers.Count; ++j) 
                                {
                                    if (Mod.botData.TryGetValue(selectedSpawn.squadMembers[j], out JObject squadMemberBotData))
                                    {
                                        // Generate sosig template
                                        // ADS time will be faster due to SosigHand.Hold patch
                                        // Supression will decrease faster due to SuppresionUpdate patch
                                        SosigConfigTemplate squadMemberSosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                                        sosigTemplate.EntityRecognitionSpeedMultiplier = 20;
                                        sosigTemplate.AggroSensitivityMultiplier = 20;
                                        sosigTemplate.CombatTargetIdentificationSpeedMultiplier = 20;
                                        squadMemberSosigTemplate.RegistersPassiveThreats = true;
                                        squadMemberSosigTemplate.DoesDropWeaponsOnBallistic = false;
                                        squadMemberSosigTemplate.ShudderThreshold = 1000; // High number, sosig should probably just never shudder
                                        squadMemberSosigTemplate.ConfusionThreshold = 1000; // High number, sosig should probably just never be consfused
                                        squadMemberSosigTemplate.ConfusionMultiplier = 0;
                                        squadMemberSosigTemplate.ConfusionTimeMax = 0;
                                        // Note that stun settings are unmodified
                                        squadMemberSosigTemplate.CanBeKnockedOut = false;
                                        squadMemberSosigTemplate.MaxUnconsciousTime = 0;
                                        squadMemberSosigTemplate.CanBeGrabbed = false;
                                        squadMemberSosigTemplate.SuppressionMult = 0.05f; // Minimum 20 supression events to fully suppress
                                        squadMemberSosigTemplate.LinkDamageMultipliers = new List<float>
                                        {
                                            3, // Head
                                            2, // Torso
                                            2, // Upper
                                            1  // Lower
                                        };
                                        squadMemberSosigTemplate.LinkStaggerMultipliers = new List<float>();
                                        squadMemberSosigTemplate.StartingLinkIntegrity = new List<Vector2>();
                                        squadMemberSosigTemplate.StartingChanceBrokenJoint = new List<float>();
                                        for (int k = 0; k < 4; ++k)
                                        {
                                            squadMemberSosigTemplate.LinkStaggerMultipliers.Add(1);
                                            squadMemberSosigTemplate.StartingLinkIntegrity.Add(new Vector2(100, 100));
                                            squadMemberSosigTemplate.StartingChanceBrokenJoint.Add(0);
                                        }

                                        // Generate inventory
                                        BotInventory squadMemberBotInventory = new BotInventory(squadMemberBotData);

                                        // Generate outfit from inventory
                                        List<FVRObject>[] squadMemberBotOutfit = squadMemberBotInventory.GetOutfit(true);

                                        AnvilManager.Run(SpawnSosig(selectedSpawn, squadMemberBotInventory, squadMemberSosigTemplate, squadMemberBotOutfit, enemyIFF, 200, false, false, false, selectedSpawn.squadMembers[j]));
                                    }
                                    else
                                    {
                                        Mod.LogError("Could not get botdata to spawn scav boss: " + selectedSpawn.bossID+" squad member: "+ selectedSpawn.squadMembers[j]);
                                    }
                                }
                            }
                            else
                            {
                                for(int j=0; j < selectedSpawn.squadMembersSpawnAttempts; ++j)
                                {
                                    if (UnityEngine.Random.value > selectedSpawn.squadMembersSpawnProbability)
                                    {
                                        continue;
                                    }

                                    string randomMember = selectedSpawn.squadMembers[UnityEngine.Random.Range(0, selectedSpawn.squadMembers.Count)];
                                    if (Mod.botData.TryGetValue(randomMember, out JObject squadMemberBotData))
                                    {
                                        // Generate sosig template
                                        // ADS time will be faster due to SosigHand.Hold patch
                                        // Supression will decrease faster due to SuppresionUpdate patch
                                        SosigConfigTemplate squadMemberSosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
                                        sosigTemplate.EntityRecognitionSpeedMultiplier = 20;
                                        sosigTemplate.AggroSensitivityMultiplier = 20;
                                        sosigTemplate.CombatTargetIdentificationSpeedMultiplier = 20;
                                        squadMemberSosigTemplate.RegistersPassiveThreats = true;
                                        squadMemberSosigTemplate.DoesDropWeaponsOnBallistic = false;
                                        squadMemberSosigTemplate.ShudderThreshold = 1000; // High number, sosig should probably just never shudder
                                        squadMemberSosigTemplate.ConfusionThreshold = 1000; // High number, sosig should probably just never be consfused
                                        squadMemberSosigTemplate.ConfusionMultiplier = 0;
                                        squadMemberSosigTemplate.ConfusionTimeMax = 0;
                                        // Note that stun settings are unmodified
                                        squadMemberSosigTemplate.CanBeKnockedOut = false;
                                        squadMemberSosigTemplate.MaxUnconsciousTime = 0;
                                        squadMemberSosigTemplate.CanBeGrabbed = false;
                                        squadMemberSosigTemplate.SuppressionMult = 0.05f; // Minimum 20 supression events to fully suppress
                                        squadMemberSosigTemplate.LinkDamageMultipliers = new List<float>
                                        {
                                            3, // Head
                                            2, // Torso
                                            2, // Upper
                                            1  // Lower
                                        };
                                        squadMemberSosigTemplate.LinkStaggerMultipliers = new List<float>();
                                        squadMemberSosigTemplate.StartingLinkIntegrity = new List<Vector2>();
                                        squadMemberSosigTemplate.StartingChanceBrokenJoint = new List<float>();
                                        for (int k = 0; k < 4; ++k)
                                        {
                                            squadMemberSosigTemplate.LinkStaggerMultipliers.Add(1);
                                            squadMemberSosigTemplate.StartingLinkIntegrity.Add(new Vector2(100, 100));
                                            squadMemberSosigTemplate.StartingChanceBrokenJoint.Add(0);
                                        }

                                        // Generate inventory
                                        BotInventory squadMemberBotInventory = new BotInventory(squadMemberBotData);

                                        // Generate outfit from inventory
                                        List<FVRObject>[] squadMemberBotOutfit = squadMemberBotInventory.GetOutfit(true);

                                        AnvilManager.Run(SpawnSosig(selectedSpawn, squadMemberBotInventory, squadMemberSosigTemplate, squadMemberBotOutfit, enemyIFF, 200, false, false, false, randomMember));
                                    }
                                    else
                                    {
                                        Mod.LogError("Could not get botdata to spawn scav boss: " + selectedSpawn.bossID + " squad member: " + randomMember);
                                    }
                                }
                            }
                        }

                        // Loop IFF once we've reached max
                        ++enemyIFF;
                        if (enemyIFF > 31)
                        {
                            enemyIFF = 2;
                        }

                        if (Networking.currentInstance != null)
                        {
                            Networking.currentInstance.enemyIFF = enemyIFF;

                            using (Packet packet = new Packet(Networking.setRaidEnemyIFFPacketID))
                            {
                                packet.Write(Networking.currentInstance.ID);
                                packet.Write(enemyIFF);

                                if (ThreadManager.host)
                                {
                                    ServerSend.SendTCPDataToAll(packet, true);
                                }
                                else
                                {
                                    ClientSend.SendTCPData(packet, true);
                                }
                            }
                        }
                    }
                    else
                    {
                        Mod.LogError("Could not get botdata to spawn scav boss: "+ selectedSpawn.bossID);
                    }
                }
            }
        }

        public IEnumerator SpawnSosig(Spawn spawn, BotInventory botInventory, SosigConfigTemplate template, List<FVRObject>[] outfit, int IFF, int experienceReward, bool PMC, bool USEC, bool scav, string dataName)
        {
            yield return IM.OD["SosigBody_Default"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody_Default"].GetGameObject();
            if (sosigPrefab == null)
            {
                Mod.LogError("Failed to get sosig prefab");
                yield break;
            }

            Vector3 spawnPos = GetSpawnPosition(spawn);
            if(Networking.currentInstance != null)
            {
                TrackedSosigData.OnCollectAdditionalData += Networking.OnSosigCollectData;

                Networking.botInventory = botInventory;
                Networking.experienceReward = experienceReward;
                Networking.PMC = PMC;
                Networking.scav = scav;
                Networking.USEC = USEC;
                Networking.dataName = dataName;
            }
            GameObject sosigObject = Instantiate(sosigPrefab, spawnPos, Quaternion.identity);
            Sosig sosig = sosigObject.GetComponentInChildren<Sosig>();
            sosig.Configure(template);
            sosig.SetIFF(IFF);
            if (Networking.currentInstance == null)
            {
                AI AIScript = sosig.gameObject.AddComponent<AI>();
                AIScript.botInventory = botInventory;
                AIScript.experienceReward = experienceReward;
                AIScript.PMC = PMC;
                AIScript.scav = scav;
                AIScript.USEC = USEC;
                AIScript.dataName = dataName;
            }
            else
            {
                TrackedSosigData.OnCollectAdditionalData -= Networking.OnSosigCollectData;
            }

            // Equip sosig items
            sosig.InitHands();
            sosig.Inventory.Init();
            sosig.Inventory.FillAllAmmo();
            TODO: // Put the repetitive weapon code in a method
            if (botInventory.equipment.TryGetValue("FirstPrimaryWeapon", out MeatovItemData firstPrimaryWeaponData) && firstPrimaryWeaponData.sosigItems != null && firstPrimaryWeaponData.sosigItems.Count > 0)
            {
                FVRObject firstPrimaryWeaponFVROObject = firstPrimaryWeaponData.sosigItems[UnityEngine.Random.Range(0, firstPrimaryWeaponData.sosigItems.Count)];
                yield return firstPrimaryWeaponFVROObject.GetGameObjectAsync();
                GameObject firstPrimaryWeaponPrefab = firstPrimaryWeaponFVROObject.GetGameObject();
                if (firstPrimaryWeaponPrefab == null)
                {
                    Mod.LogError("Failed to get FirstPrimaryWeapon prefab for " + firstPrimaryWeaponData.tarkovID);
                }
                else
                {
                    SosigWeapon component = UnityEngine.Object.Instantiate<GameObject>(firstPrimaryWeaponPrefab, spawnPos + Vector3.up * 0.1f, Quaternion.identity).GetComponent<SosigWeapon>();
                    component.SetAutoDestroy(true);
                    component.O.SpawnLockable = false;
                    component.O.IsPickUpLocked = true;
                    component.SetAmmoClamping(true);
                    component.IsShakeReloadable = false;
                    sosig.ForceEquip(component);
                }
            }

            if (sosig.Inventory.IsThereAFreeSlot() && botInventory.equipment.TryGetValue("SecondPrimaryWeapon", out MeatovItemData secondPrimaryWeaponData) && secondPrimaryWeaponData.sosigItems != null && secondPrimaryWeaponData.sosigItems.Count > 0)
            {
                FVRObject secondPrimaryWeaponFVROObject = secondPrimaryWeaponData.sosigItems[UnityEngine.Random.Range(0, secondPrimaryWeaponData.sosigItems.Count)];
                yield return secondPrimaryWeaponFVROObject.GetGameObjectAsync();
                GameObject secondPrimaryWeaponPrefab = secondPrimaryWeaponFVROObject.GetGameObject();
                if (secondPrimaryWeaponPrefab == null)
                {
                    Mod.LogError("Failed to get SecondPrimaryWeapon prefab for " + secondPrimaryWeaponData.tarkovID);
                }
                else
                {
                    SosigWeapon component2 = UnityEngine.Object.Instantiate<GameObject>(secondPrimaryWeaponPrefab, spawnPos + Vector3.up * 0.1f, Quaternion.identity).GetComponent<SosigWeapon>();
                    component2.SetAutoDestroy(true);
                    component2.O.SpawnLockable = false;
                    component2.O.IsPickUpLocked = true;
                    component2.SetAmmoClamping(true);
                    component2.IsShakeReloadable = false;
                    sosig.ForceEquip(component2);
                }
            }

            if (sosig.Inventory.IsThereAFreeSlot() && botInventory.equipment.TryGetValue("Holster", out MeatovItemData holsterWeaponData) && holsterWeaponData.sosigItems != null && holsterWeaponData.sosigItems.Count > 0)
            {
                FVRObject holsterWeaponFVROObject = holsterWeaponData.sosigItems[UnityEngine.Random.Range(0, holsterWeaponData.sosigItems.Count)];
                yield return holsterWeaponFVROObject.GetGameObjectAsync();
                GameObject holsterWeaponPrefab = holsterWeaponFVROObject.GetGameObject();
                if (holsterWeaponPrefab == null)
                {
                    Mod.LogError("Failed to get Holster prefab for " + holsterWeaponData.tarkovID);
                }
                else
                {
                    SosigWeapon component2 = UnityEngine.Object.Instantiate<GameObject>(holsterWeaponPrefab, spawnPos + Vector3.up * 0.1f, Quaternion.identity).GetComponent<SosigWeapon>();
                    component2.SetAutoDestroy(true);
                    component2.O.SpawnLockable = false;
                    component2.O.IsPickUpLocked = true;
                    component2.SetAmmoClamping(true);
                    component2.IsShakeReloadable = false;
                    sosig.ForceEquip(component2);
                }
            }

            if (sosig.Inventory.IsThereAFreeSlot() && botInventory.equipment.TryGetValue("Scabbard", out MeatovItemData scabbardWeaponData) && scabbardWeaponData.sosigItems != null && scabbardWeaponData.sosigItems.Count > 0)
            {
                FVRObject scabbardWeaponFVROObject = scabbardWeaponData.sosigItems[UnityEngine.Random.Range(0, scabbardWeaponData.sosigItems.Count)];
                yield return scabbardWeaponFVROObject.GetGameObjectAsync();
                GameObject scabbardWeaponPrefab = scabbardWeaponFVROObject.GetGameObject();
                if (scabbardWeaponPrefab == null)
                {
                    Mod.LogError("Failed to get Scabbard prefab for " + scabbardWeaponData.tarkovID);
                }
                else
                {
                    SosigWeapon component2 = UnityEngine.Object.Instantiate<GameObject>(scabbardWeaponPrefab, spawnPos + Vector3.up * 0.1f, Quaternion.identity).GetComponent<SosigWeapon>();
                    component2.SetAutoDestroy(true);
                    component2.O.SpawnLockable = false;
                    component2.O.IsPickUpLocked = true;
                    component2.SetAmmoClamping(true);
                    component2.IsShakeReloadable = false;
                    sosig.ForceEquip(component2);
                }
            }

            // Spawn outfit
            for (int i=0; i < outfit.Length; ++i)
            {
                if(outfit[i] != null)
                {
                    for(int j = 0; j < outfit[i].Count; ++j)
                    {
                        yield return outfit[i][j].GetGameObjectAsync();
                        GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(outfit[i][j].GetGameObject(), sosig.Links[i].transform.position, sosig.Links[i].transform.rotation);
                        gameObject.transform.SetParent(sosig.Links[i].transform);
                        SosigWearable component = gameObject.GetComponent<SosigWearable>();
                        component.RegisterWearable(sosig.Links[i]);
                    }
                }
            }

            // Activate behavior
            TODO0: // Make patch to make sosigs go to a random valid extraction once they finish their path or there is only enough time to reach extraction
            if (PMC)
            {
                PMCNavPoints.Shuffle();
                sosig.CommandPathTo(PMCNavPoints, 0.5f, new Vector2(10, 60), 1, Sosig.SosigMoveSpeed.Walking, Sosig.PathLoopType.Loop, null, 1, 1, true, 10);
            }
            else
            {
                sosig.CurrentOrder = Sosig.SosigOrder.Wander;
                sosig.FallbackOrder = Sosig.SosigOrder.Wander;
                sosig.CommandGuardPoint(spawnPos, true);
                sosig.SetDominantGuardDirection(UnityEngine.Random.onUnitSphere);
                sosig.SetGuardInvestigateDistanceThreshold(25f);
            }
        }

        public void EndRaid(RaidStatus status, string usedExtraction = null)
        {
            Mod.justFinishedRaid = true;
            Mod.raidStatus = status;
            Mod.usedExtraction = usedExtraction;

            if(status == RaidStatus.Success || status == RaidStatus.RunThrough)
            {
                // Secure all items player has on them
                Mod.SecureInventory();
            }
            else // MIA, KIA
            {
                // Secure only the pouch
                Mod.SecureInventory(true);
            }

            // Clear extraction UI
            StatusUI.instance.extractionsParent.SetActive(false);
            while(StatusUI.instance.extractionsParent.transform.childCount > 1)
            {
                Transform child = StatusUI.instance.extractionsParent.transform.GetChild(1);
                child.parent = null;
                Destroy(child.gameObject);
            }

            // Main scene components will get secured on load start and unsecured upon arrival
            Mod.unloadRaid = true;
            Mod.LoadHideout(-1, true);
        }

        public void OnDestroy()
        {
            if(Networking.spawnRequests.Count > 0)
            {
                Networking.spawnRequests.Clear();
            }
        }
    }
}
