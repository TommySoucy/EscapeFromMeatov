using FistVR;
using H3MP.Networking;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class RaidManager : MonoBehaviour
    {
        public static RaidManager instance { get; private set; }

        public int maxPMCCount;
        public List<Spawn> PMCSpawns; 
        public List<Spawn> scavSpawns; 
        public List<Extraction> PMCExtractions; 
        public List<Extraction> scavExtractions; 
        public List<Transform> PMCNavPoints; 

        public bool control; // Whether we control the raid (Only relevant if MP)

        public Spawn spawn; // Spawn used
        public bool AIPMCSpawned;
        public int enemyIFF = 2; // Players are 0, Scavs are 1, PMCs and scav bosses get incremental IFF
        public List<Extraction> activeExtractions;

        public void Awake()
        {
            instance = this;

            if(PMCSpawns.Count < 1)
            {
                Mod.LogError("Raid does not have at least 1 PMC spawns, expect errors");
            }
        }

        public void Start()
        {
            UpdateControl();
            if (control)
            {
                Init();
            }
            else // We don't control
            {
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

            TODO e: // Spawn loose loot
        }

        public void Spawn(Spawn spawn)
        {
            Vector3 spawnPosition = GetSpawnPosition(spawn);
            GM.CurrentSceneSettings.DeathResetPoint = spawn.transform;
            GM.CurrentMovementManager.TeleportToPoint(spawnPosition, true);

            // Choose extractions
            List<Extraction> extractionsToUse = Mod.charChoicePMC ? PMCExtractions : scavExtractions;
            List<Extraction> noRestrictionExtractions = new List<Extraction>();
            List<Extraction> restrictionExtractions = new List<Extraction>();
            for(int i=0; i < extractionsToUse.Count; ++i)
            {
                Extraction extraction = extractionsToUse[i];
                extraction.distToSpawn = Vector3.Distance(spawnPosition, extraction.transform.position);

                // Find which list to add to depending if the extraction has any restrictions
                List<Extraction> extractionListToAdd = null;
                if ((extraction.activeTimes != null && extraction.activeTimes.Count > 0)
                    || (extraction.itemRequirements != null && extraction.itemRequirements.Count > 0)
                    || (extraction.itemWhitelist != null && extraction.itemWhitelist.Count > 0)
                    || (extraction.itemBlacklist != null && extraction.itemBlacklist.Count > 0))
                {
                    extractionListToAdd = restrictionExtractions;
                }
                else
                {
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

            // Add half (min 1) the restriction extractions, chosen randomly
            if (noRestrictionExtractions.Count > 0)
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
                return new Vector3(Mathf.Cos(angle) * range, spawn.transform.position.y, Mathf.Sin(angle) * range);
            }
        }

        public void Update()
        {
            UpdateControl();
            if (control)
            {
                // Check if can spawn initial AI PMC
                if (!AIPMCSpawned)
                {
                    if(Networking.currentInstance == null)
                    {
                        int spawnCount = Mod.charChoicePMC ? maxPMCCount - 1 : maxPMCCount;
                        for(int i=0; i < spawnCount; ++i)
                        {
                            SpawnPMC();
                        }
                        AIPMCSpawned = true;
                    }
                    else if(Networking.currentInstance.consumedSpawnCount >= Networking.currentInstance.players.Count)
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
                // Time until skirmish when recognizing an enemy entity will increase faster due to StateBailCheck_ShouldISkirmish patch
                // ADS time will be faster due to SosigHand.Hold patch
                // Supression will decrease faster due to SuppresionUpdate patch
                SosigConfigTemplate sosigTemplate = ScriptableObject.CreateInstance<SosigConfigTemplate>();
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

                // Generate inventory
                BotInventory botInventory = new BotInventory(botData);

                // Generate outfit from inventory
                List<FVRObject>[] botOutfit = botInventory.GetOutfit(true);

                AnvilManager.Run(SpawnSosig(spawn, botInventory, new SosigConfigTemplate(), botOutfit, enemyIFF++, true, USEC));

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

        public IEnumerator SpawnSosig(Spawn spawn, BotInventory botInventory, SosigConfigTemplate template, List<FVRObject>[] outfit, int IFF, bool PMC, bool USEC)
        {
            yield return IM.OD["SosigBody"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody"].GetGameObject();
            if (sosigPrefab == null)
            {
                Mod.LogError("Failed to get sosig prefab");
                yield break;
            }

            Vector3 spawnPos = GetSpawnPosition(spawn);
            GameObject sosigObject = Instantiate(sosigPrefab, spawnPos, Quaternion.identity);
            Sosig sosig = sosigObject.GetComponentInChildren<Sosig>();
            sosig.Configure(template);
            sosig.SetIFF(IFF);
            AI AIScript = sosigObject.AddComponent<AI>();
            AIScript.botInventory = botInventory;
            AIScript.PMC = PMC;
            AIScript.USEC = USEC;

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
            for(int i=0; i < outfit.Length; ++i)
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
            TODO e: // Use Sosig.CommandPathTo to make them go through points
            TODO e: // Make patch to make sosigs go to a random valid extraction once they finish their path or there is only enough time to reach extraction
            sosig.CurrentOrder = Sosig.SosigOrder.Wander;
            sosig.FallbackOrder = Sosig.SosigOrder.Wander;
            sosig.CommandGuardPoint(spawnPos, true);
            sosig.SetDominantGuardDirection(UnityEngine.Random.onUnitSphere);
            sosig.SetGuardInvestigateDistanceThreshold(25f);
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
