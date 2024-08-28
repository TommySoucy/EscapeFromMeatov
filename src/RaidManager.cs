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
        public List<Transform> PMCNavPoints; 

        public bool control; // Whether we control the raid (Only relevant if MP)

        public Spawn spawn; // Spawn used
        public bool AIPMCSpawned;
        public int enemyIFF = 2; // Players are 0, Scavs are 1, PMCs and scav bosses get incremental IFF

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
            }
        }

        public void Spawn(Spawn spawn)
        {
            GM.CurrentSceneSettings.DeathResetPoint = spawn.transform;
            GM.CurrentMovementManager.TeleportToPoint(GetSpawnPosition(spawn), true);
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

                AnvilManager.Run(SpawnSosig(spawn, botInventory, new SosigConfigTemplate(), botOutfit, enemyIFF++));

                // Loop IFF once we've reached max
                if(enemyIFF > 31)
                {
                    enemyIFF = 2;
                }

                if(Networking.currentInstance != null)
                {
                    using (Packet packet = new Packet(setRaidenemyIFFPacketID))
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

        public IEnumerator SpawnSosig(Spawn spawn, BotInventory botInventory, SosigConfigTemplate template, List<FVRObject>[] outfit, int IFF)
        {
            yield return IM.OD["SosigBody"].GetGameObjectAsync();
            GameObject sosigPrefab = IM.OD["SosigBody"].GetGameObject();
            if (sosigPrefab == null)
            {
                Mod.LogError("Failed to get sosig prefab");
                yield break;
            }

            GameObject sosigObject = Instantiate(sosigPrefab, GetSpawnPosition(spawn), Quaternion.identity);
            Sosig sosig = sosigObject.GetComponentInChildren<Sosig>();
            sosig.Configure(template);
            sosig.SetIFF(IFF);
        }
    }
}
