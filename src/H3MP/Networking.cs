using FFmpeg.AutoGen;
using H3MP;
using H3MP.Networking;
using H3MP.Tracking;
using ModularWorkshop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static RootMotion.FinalIK.IKSolver;

namespace EFM
{
    public class Networking
    {
        // Instance handling:
        // We can only ever join a raid instance through HideoutController.SetInstance so we handle joining there
        // Leaving a raid instance is handled through the OnInstanceLeft event by HideoutController.OnInstanceLeft
        // Other players joining/leaving is handled through the OnPlayerInstanceChanged event by OnPlayerInstanceChanged
        // A RaidInstance should be left when we leave a scene that has a RaidManager (Leaving raid) or when we go to MeatovMainMenu
        public static RaidInstance potentialInstance;
        public static RaidInstance currentInstance;
        public static Dictionary<int, RaidInstance> raidInstances = new Dictionary<int, RaidInstance>();
        public static bool setLatestInstance;
        public static List<SpawnRequest> spawnRequests = new List<SpawnRequest>();
        public static bool spawnRequested;

        public static int addInstancePacketID; // AddEFMInstancePacketID
        public static int setPlayerReadynessPacketID; // SetEFMInstancePlayerReadyness
        public static int setInstanceWaitingPacketID; // SetEFMInstanceWaitingPacketID
        public static int setRaidEnemyIFFPacketID; // SetEFMInstanceEnemyIFFPacketID
        public static int setRaidActiveScavCountPacketID; // SetEFMInstanceActiveScavCountPacketID
        public static int setInstanceAIPMCSpawnedPacketID; // SetEFMInstanceAIPMCSpawnedPacketID
        public static int requestSpawnPacketID; // RequestEFMRaidSpawnPacketID
        public static int spawnReturnPacketID; // EFMRaidSpawnReturnPacketID
        public static int consumeSpawnPacketID; // ConsumeEFMInstanceSpawnPacketID
        public static int breachDoorPacketID; // EFMBreachPacketID
        public static int unlockDoorPacketID; // EFMUnlockDoorPacketID
        public static int unlockLootContainerPacketID; // EFMUnlockLootContainerPacketID
        public static int setLootContainerContentSpawnedPacketID; // SetEFMLootContainerContentSpawnedPacketID
        public static int setModulPartDataPacketID; // SetEFMModulPartDataPacketID

        public static void OnConnection()
        {
            if(HideoutController.instance != null)
            {
                if(HideoutController.instance.loadingRaid || HideoutController.instance.countdownDeploy || HideoutController.instance.waitForDeploy)
                {
                    // Already starting a game, just disconnect
                    if (ThreadManager.host)
                    {
                        Server.Close();
                    }
                    else
                    {
                        Client.singleton.Disconnect(true, 0);
                    }
                    return;
                }
                else if(HideoutController.instance.pageIndex > 2 && HideoutController.instance.pageIndex < 8)
                {
                    // Was already going through the singleplayer process to start a game, set back to page 0
                    HideoutController.instance.SetPage(0);
                }
            }

            // Sub to events
            H3MP.GameManager.OnPlayerInstanceChanged += OnPlayerInstanceChanged;
            H3MP.Mod.OnGetBestPotentialObjectHost += OnGetBestPotentialObjectHost;
            H3MP.GameManager.OnInstanceLeft += HideoutController.OnInstanceLeft;

            // Setup custom packets
            if (ThreadManager.host)
            {
                // Add instance
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("AddEFMInstancePacketID", out addInstancePacketID))
                {
                    addInstancePacketID = Server.RegisterCustomPacketType("AddEFMInstancePacketID");
                }
                H3MP.Mod.customPacketHandlers[addInstancePacketID] = AddInstanceServerHandler;

                // Set player readyness
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstancePlayerReadyness", out setPlayerReadynessPacketID))
                {
                    setPlayerReadynessPacketID = Server.RegisterCustomPacketType("SetEFMInstancePlayerReadyness");
                }
                H3MP.Mod.customPacketHandlers[setPlayerReadynessPacketID] = SetPlayerReadynessServerHandler;

                // Set instance waiting
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceWaitingPacketID", out setInstanceWaitingPacketID))
                {
                    setInstanceWaitingPacketID = Server.RegisterCustomPacketType("SetEFMInstanceWaitingPacketID");
                }
                H3MP.Mod.customPacketHandlers[setInstanceWaitingPacketID] = SetInstanceWaitingServerHandler;

                // Set enemy IFF
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceEnemyIFFPacketID", out setRaidEnemyIFFPacketID))
                {
                    setRaidEnemyIFFPacketID = Server.RegisterCustomPacketType("SetEFMInstanceEnemyIFFPacketID");
                }
                H3MP.Mod.customPacketHandlers[setRaidEnemyIFFPacketID] = SetRaidEnemyIFFServerHandler;

                // Set active scav count
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceActiveScavCountPacketID", out setRaidActiveScavCountPacketID))
                {
                    setRaidActiveScavCountPacketID = Server.RegisterCustomPacketType("SetEFMInstanceActiveScavCountPacketID");
                }
                H3MP.Mod.customPacketHandlers[setRaidActiveScavCountPacketID] = SetRaidActiveScavCountServerHandler;

                // Set AI PMC Spawned
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceAIPMCSpawnedPacketID", out setInstanceAIPMCSpawnedPacketID))
                {
                    setInstanceAIPMCSpawnedPacketID = Server.RegisterCustomPacketType("SetEFMInstanceAIPMCSpawnedPacketID");
                }
                H3MP.Mod.customPacketHandlers[setInstanceAIPMCSpawnedPacketID] = SetInstanceAIPMCSpawnedServerHandler;

                // Request spawn
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("RequestEFMRaidSpawnPacketID", out requestSpawnPacketID))
                {
                    requestSpawnPacketID = Server.RegisterCustomPacketType("RequestEFMRaidSpawnPacketID");
                }
                H3MP.Mod.customPacketHandlers[requestSpawnPacketID] = RequestSpawnServerHandler;

                // Spawn return
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMRaidSpawnReturnPacketID", out spawnReturnPacketID))
                {
                    spawnReturnPacketID = Server.RegisterCustomPacketType("EFMRaidSpawnReturnPacketID");
                }
                H3MP.Mod.customPacketHandlers[spawnReturnPacketID] = SpawnReturnServerHandler;

                // Consume spawn
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("ConsumeEFMInstanceSpawnPacketID", out consumeSpawnPacketID))
                {
                    consumeSpawnPacketID = Server.RegisterCustomPacketType("ConsumeEFMInstanceSpawnPacketID");
                }
                H3MP.Mod.customPacketHandlers[consumeSpawnPacketID] = ConsumeSpawnServerHandler;

                // Attempt door breach
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMBreachPacketID", out breachDoorPacketID))
                {
                    breachDoorPacketID = Server.RegisterCustomPacketType("EFMBreachPacketID");
                }
                H3MP.Mod.customPacketHandlers[breachDoorPacketID] = BreachDoorServerHandler;

                // Unlock door
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMUnlockDoorPacketID", out unlockDoorPacketID))
                {
                    unlockDoorPacketID = Server.RegisterCustomPacketType("EFMUnlockDoorPacketID");
                }
                H3MP.Mod.customPacketHandlers[unlockDoorPacketID] = UnlockDoorServerHandler;

                // Set LootContainer content spawned
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMLootContainerContentSpawnedPacketID", out setLootContainerContentSpawnedPacketID))
                {
                    setLootContainerContentSpawnedPacketID = Server.RegisterCustomPacketType("SetEFMLootContainerContentSpawnedPacketID");
                }
                H3MP.Mod.customPacketHandlers[setLootContainerContentSpawnedPacketID] = SetLootContainerContentSpawnedServerHandler;

                // Unlock loot container
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMUnlockLootContainerPacketID", out unlockLootContainerPacketID))
                {
                    unlockLootContainerPacketID = Server.RegisterCustomPacketType("EFMUnlockLootContainerPacketID");
                }
                H3MP.Mod.customPacketHandlers[unlockLootContainerPacketID] = UnlockLootContainerServerHandler;

                // Set modul part data
                if (!H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMModulPartDataPacketID", out setModulPartDataPacketID))
                {
                    setModulPartDataPacketID = Server.RegisterCustomPacketType("SetEFMModulPartDataPacketID");
                }
                H3MP.Mod.customPacketHandlers[setModulPartDataPacketID] = SetModulPartDataServerHandler;
            }
            else
            {
                // Add instance
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("AddEFMInstancePacketID", out int customAddInstancePacketID))
                {
                    addInstancePacketID = customAddInstancePacketID;
                    H3MP.Mod.customPacketHandlers[addInstancePacketID] = AddInstanceClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("AddEFMInstancePacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += AddEFMInstancePacketIDReceived;
                }

                // Set player readyness
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstancePlayerReadyness", out int customSetPlayerReadynessPacketID))
                {
                    setPlayerReadynessPacketID = customSetPlayerReadynessPacketID;
                    H3MP.Mod.customPacketHandlers[setPlayerReadynessPacketID] = SetPlayerReadynessClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMInstancePlayerReadyness");
                    H3MP.Mod.CustomPacketHandlerReceived += SetEFMInstancePlayerReadynessPacketIDReceived;
                }

                // Set instance waiting
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceWaitingPacketID", out int customSetInstanceWaitingPacketID))
                {
                    setInstanceWaitingPacketID = customSetInstanceWaitingPacketID;
                    H3MP.Mod.customPacketHandlers[setInstanceWaitingPacketID] = SetInstanceWaitingClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMInstanceWaitingPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetEFMInstanceWaitingPacketIDReceived;
                }

                // Set enemy IFF
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceEnemyIFFPacketID", out int customSetRaidEnemyIFFPacketID))
                {
                    setRaidEnemyIFFPacketID = customSetRaidEnemyIFFPacketID;
                    H3MP.Mod.customPacketHandlers[setRaidEnemyIFFPacketID] = SetEnemyIFFClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMInstanceEnemyIFFPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetEnemyIFFPacketIDReceived;
                }

                // Set active scav count
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceActiveScavCountPacketID", out int customSetActiveScavCountPacketID))
                {
                    setRaidActiveScavCountPacketID = customSetActiveScavCountPacketID;
                    H3MP.Mod.customPacketHandlers[setRaidActiveScavCountPacketID] = SetActiveScavCountClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMInstanceActiveScavCountPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetActiveScavCountPacketIDReceived;
                }

                // Set AI PMC Spawned
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMInstanceAIPMCSpawnedPacketID", out int customSetInstanceAIPMCSpawnedPacketID))
                {
                    setInstanceAIPMCSpawnedPacketID = customSetInstanceAIPMCSpawnedPacketID;
                    H3MP.Mod.customPacketHandlers[setInstanceAIPMCSpawnedPacketID] = SetInstanceAIPMCSpawnedClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMInstanceAIPMCSpawnedPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetInstanceAIPMCSpawnedPacketIDReceived;
                }

                // Request spawn
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("RequestEFMRaidSpawnPacketID", out int customRequestSpawnPacketID))
                {
                    requestSpawnPacketID = customRequestSpawnPacketID;
                    H3MP.Mod.customPacketHandlers[requestSpawnPacketID] = RequestSpawnClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("RequestEFMRaidSpawnPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += RequestSpawnPacketIDReceived;
                }

                // Spawn return
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMRaidSpawnReturnPacketID", out int customSpawnReturnPacketID))
                {
                    spawnReturnPacketID = customSpawnReturnPacketID;
                    H3MP.Mod.customPacketHandlers[spawnReturnPacketID] = SpawnReturnClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("EFMRaidSpawnReturnPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SpawnReturnPacketIDReceived;
                }

                // Consume spawn
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("ConsumeEFMInstanceSpawnPacketID", out int customConsumeSpawnPacketID))
                {
                    consumeSpawnPacketID = customConsumeSpawnPacketID;
                    H3MP.Mod.customPacketHandlers[consumeSpawnPacketID] = ConsumeSpawnClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("ConsumeEFMInstanceSpawnPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += ConsumeSpawnPacketIDReceived;
                }

                // Breach door
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMBreachPacketID", out int customBreachDoorPacketID))
                {
                    breachDoorPacketID = customBreachDoorPacketID;
                    H3MP.Mod.customPacketHandlers[breachDoorPacketID] = BreachDoorClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("EFMBreachPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += BreachDoorPacketIDReceived;
                }

                // Unlock door
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMUnlockDoorPacketID", out int customUnlockDoorPacketID))
                {
                    unlockDoorPacketID = customUnlockDoorPacketID;
                    H3MP.Mod.customPacketHandlers[unlockDoorPacketID] = UnlockDoorClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("EFMUnlockDoorPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += UnlockDoorPacketIDReceived;
                }

                // Set LootContainer content spawned
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMLootContainerContentSpawnedPacketID", out int customSetLootContainerContentSpawnedPacketID))
                {
                    setLootContainerContentSpawnedPacketID = customSetLootContainerContentSpawnedPacketID;
                    H3MP.Mod.customPacketHandlers[setLootContainerContentSpawnedPacketID] = SetLootContainerContentSpawnedClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMLootContainerContentSpawnedPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetLootContainerContentSpawnedPacketIDReceived;
                }

                // Set LootContainer content spawned
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("EFMUnlockLootContainerPacketID", out int customUnlockLootContainerPacketID))
                {
                    unlockLootContainerPacketID = customUnlockLootContainerPacketID;
                    H3MP.Mod.customPacketHandlers[unlockLootContainerPacketID] = UnlockLootContainerClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("EFMUnlockLootContainerPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += UnlockLootcontainerPacketIDReceived;
                }

                // Set modul part data
                if (H3MP.Mod.registeredCustomPacketIDs.TryGetValue("SetEFMModulPartDataPacketID", out int customSetModulPartDataPacketID))
                {
                    setModulPartDataPacketID = customSetModulPartDataPacketID;
                    H3MP.Mod.customPacketHandlers[setModulPartDataPacketID] = SetModulPartDataClientHandler;
                }
                else
                {
                    ClientSend.RegisterCustomPacketType("SetEFMModulPartDataPacketID");
                    H3MP.Mod.CustomPacketHandlerReceived += SetModulPartDataPacketIDReceived;
                }
            }
        }

        public static void OnDisconnection()
        {
            // Unsub from events
            H3MP.GameManager.OnPlayerInstanceChanged -= OnPlayerInstanceChanged;
            H3MP.Mod.OnGetBestPotentialObjectHost -= OnGetBestPotentialObjectHost;
            H3MP.GameManager.OnInstanceLeft -= HideoutController.OnInstanceLeft;
        }

        public static void OnPlayerInstanceChanged(int ID, int previousInstanceID, int newInstanceID)
        {
            // Ignore if this is us
            if(ID == GameManager.ID)
            {
                return;
            }

            // Remove from previous instance
            if (raidInstances.TryGetValue(previousInstanceID, out RaidInstance previousInstance))
            {
                int preHost = previousInstance.players[0];
                previousInstance.players.Remove(ID);

                if (previousInstance.players.Count == 0) // No more players in instance, remove instance
                {
                    raidInstances.Remove(previousInstanceID);

                    if(HideoutController.instance != null)
                    {
                        HideoutController.instance.PopulateInstancesList();
                    }
                }
                else // Still have other players
                {
                    // If this is our instance, update list in UI
                    if (currentInstance != null && currentInstance.ID == previousInstanceID && HideoutController.instance != null)
                    {
                        HideoutController.instance.PopulatePlayerList();
                    }
                }

                // If host has changed
                if(preHost != previousInstance.players[0] && spawnRequested)
                {
                    if(GameManager.ID == previousInstance.players[0])
                    {
                        spawnRequested = false;
                    }
                    else
                    {
                        // Rerequest to new host
                        using (Packet packet = new Packet(Networking.requestSpawnPacketID))
                        {
                            packet.Write(currentInstance.ID);
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
            }

            // Add to new instance
            if (raidInstances.TryGetValue(newInstanceID, out RaidInstance newInstance))
            {
                newInstance.players.Add(ID);

                // If this is our instance, update list in UI
                if (currentInstance != null && currentInstance.ID == previousInstanceID && HideoutController.instance != null)
                {
                    HideoutController.instance.PopulatePlayerList();
                }
            }
        }

        public static void OnGetBestPotentialObjectHost(int currentController, bool forUs, bool hasWhiteList, List<int> whiteList, string sceneOverride, int instanceOverride, ref int bestPotentialObjectHost)
        {
            if(bestPotentialObjectHost == -1 && forUs && currentInstance != null)
            {
                for (int i = 0; i<currentInstance.players.Count; ++i)
                {
                    if (currentInstance.players[i] != currentController && (!hasWhiteList || whiteList.Contains(currentInstance.players[i])))
                    {
                        bestPotentialObjectHost = currentInstance.players[i];
                        return;
                    }
                }
            }
        }

        public static void AddModulPartData(List<byte> buffer, string groupID, string selectedPart, IDictionary pointDict, TrackedItem trackedItem)
        {
            ModularWeaponPartsAttachmentPoint point = pointDict[groupID] as ModularWeaponPartsAttachmentPoint;
            if (point != null)
            {
                MeatovItem partMeatovItem = point.ModularPartPoint.GetComponent<MeatovItem>();

                buffer.AddRange(new byte[] { 5, 7, 18 }); // EFM, to identify EFM relevant data amongst custom data
                if (partMeatovItem == null)
                {
                    // None part will be indicated by 0 length tarkov ID
                    buffer.Add(0);
                }
                else
                {
                    buffer.Add((byte)partMeatovItem.tarkovID.Length);
                    buffer.AddRange(Encoding.ASCII.GetBytes(partMeatovItem.tarkovID));
                    int data = 0;
                    // Note that whether an item (physical or modul part like in this case) is insured, never changes in raid
                    // So we don't need to sync the change of that state
                    data |= (partMeatovItem.insured ? 1 : 0);
                    data <<= 1;
                    TODO: // Check if even want to write this, because whether an item is found in raid
                    // should be independent from player to player. To another player, a player's item should not
                    // be found in raid, like in tarkov
                    data |= (partMeatovItem.foundInRaid ? 1 : 0);
                    buffer.Add((byte)data);
                }
            }
        }

        public static void PreConfigureModulPartData(byte[] additionalData, int offset, string groupID, IDictionary pointDict, TrackedItem trackedItem)
        {
            // Just set override to none so no default item data gets set on the newly configure part when it gets enabled
            // This obviously makes the assumption that whenever this is called, we are guaranteed that a part will be enabled before ReadModulPartData
            ModularWeaponPartPatch.overrideItemNone = true;
        }

        public static void ReadModulPartData(byte[] additionalData, int offset, string groupID, IDictionary pointDict, TrackedItem trackedItem)
        {
            ModularWeaponPartsAttachmentPoint point = pointDict[groupID] as ModularWeaponPartsAttachmentPoint;
            if (point != null)
            {
                MeatovItem partMeatovItem = point.ModularPartPoint.GetComponent<MeatovItem>();
                if (partMeatovItem == null)
                {
                    for (int i = offset; i < additionalData.Length - 2; ++i) 
                    {
                        // Find EFM data
                        if (additionalData[i] == 5 && additionalData[i+1] == 7 && additionalData[i+2] == 18)
                        {
                            i += 3;

                            byte tarkovIDLength = additionalData[i++];
                            // 0 tarkovIDLength indicates none part, so we don't want to add a meatovItem
                            if (tarkovIDLength > 0)
                            {
                                partMeatovItem = point.ModularPartPoint.gameObject.AddComponent<MeatovItem>();
                                string tarkovID = Encoding.ASCII.GetString(additionalData, i, tarkovIDLength);
                                i += tarkovIDLength;
                                partMeatovItem.SetData(Mod.defaultItemData[tarkovID]);
                                byte data = additionalData[i++];
                                partMeatovItem.foundInRaid = (data & 1) == 1;
                                partMeatovItem.insured = ((data >> 1) & 1) == 1;
                            }

                            break;
                        }
                    }
                }
            }
        }

        public static void AddEFMInstancePacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("AddEFMInstancePacketID"))
            {
                addInstancePacketID = ID;
                H3MP.Mod.customPacketHandlers[addInstancePacketID] = AddInstanceClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= AddEFMInstancePacketIDReceived;
            }
        }

        public static void SetEFMInstancePlayerReadynessPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMInstancePlayerReadyness"))
            {
                setPlayerReadynessPacketID = ID;
                H3MP.Mod.customPacketHandlers[setPlayerReadynessPacketID] = SetPlayerReadynessClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetEFMInstancePlayerReadynessPacketIDReceived;
            }
        }

        public static void SetEFMInstanceWaitingPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMInstanceWaitingPacketID"))
            {
                setInstanceWaitingPacketID = ID;
                H3MP.Mod.customPacketHandlers[setInstanceWaitingPacketID] = SetInstanceWaitingClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetEFMInstanceWaitingPacketIDReceived;
            }
        }

        public static void SetEnemyIFFPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMInstanceEnemyIFFPacketID"))
            {
                setRaidEnemyIFFPacketID = ID;
                H3MP.Mod.customPacketHandlers[setRaidEnemyIFFPacketID] = SetEnemyIFFClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetEnemyIFFPacketIDReceived;
            }
        }

        public static void SetActiveScavCountPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMInstanceActiveScavCountPacketID"))
            {
                setRaidActiveScavCountPacketID = ID;
                H3MP.Mod.customPacketHandlers[setRaidActiveScavCountPacketID] = SetActiveScavCountClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetActiveScavCountPacketIDReceived;
            }
        }

        public static void SetInstanceAIPMCSpawnedPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMInstanceAIPMCSpawnedPacketID"))
            {
                setInstanceAIPMCSpawnedPacketID = ID;
                H3MP.Mod.customPacketHandlers[setInstanceAIPMCSpawnedPacketID] = SetInstanceAIPMCSpawnedClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetInstanceAIPMCSpawnedPacketIDReceived;
            }
        }

        public static void RequestSpawnPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("RequestEFMRaidSpawnPacketID"))
            {
                requestSpawnPacketID = ID;
                H3MP.Mod.customPacketHandlers[requestSpawnPacketID] = RequestSpawnClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= RequestSpawnPacketIDReceived;
            }
        }

        public static void SpawnReturnPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("EFMRaidSpawnReturnPacketID"))
            {
                spawnReturnPacketID = ID;
                H3MP.Mod.customPacketHandlers[spawnReturnPacketID] = SpawnReturnClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SpawnReturnPacketIDReceived;
            }
        }

        public static void ConsumeSpawnPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("ConsumeEFMInstanceSpawnPacketID"))
            {
                consumeSpawnPacketID = ID;
                H3MP.Mod.customPacketHandlers[consumeSpawnPacketID] = ConsumeSpawnClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= ConsumeSpawnPacketIDReceived;
            }
        }

        public static void BreachDoorPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("EFMBreachPacketID"))
            {
                breachDoorPacketID = ID;
                H3MP.Mod.customPacketHandlers[breachDoorPacketID] = BreachDoorClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= BreachDoorPacketIDReceived;
            }
        }

        public static void UnlockDoorPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("EFMUnlockDoorPacketID"))
            {
                unlockDoorPacketID = ID;
                H3MP.Mod.customPacketHandlers[unlockDoorPacketID] = UnlockDoorClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= UnlockDoorPacketIDReceived;
            }
        }

        public static void SetLootContainerContentSpawnedPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMLootContainerContentSpawnedPacketID"))
            {
                setLootContainerContentSpawnedPacketID = ID;
                H3MP.Mod.customPacketHandlers[setLootContainerContentSpawnedPacketID] = SetLootContainerContentSpawnedClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetLootContainerContentSpawnedPacketIDReceived;
            }
        }

        public static void UnlockLootcontainerPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("EFMUnlockLootContainerPacketID"))
            {
                unlockLootContainerPacketID = ID;
                H3MP.Mod.customPacketHandlers[unlockLootContainerPacketID] = UnlockLootContainerClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= UnlockLootcontainerPacketIDReceived;
            }
        }

        public static void SetModulPartDataPacketIDReceived(string identifier, int ID)
        {
            if (identifier.Equals("SetEFMModulPartDataPacketID"))
            {
                setModulPartDataPacketID = ID;
                H3MP.Mod.customPacketHandlers[setModulPartDataPacketID] = SetModulPartDataClientHandler;
                H3MP.Mod.CustomPacketHandlerReceived -= SetModulPartDataPacketIDReceived;
            }
        }

        public static RaidInstance AddInstance(string map, bool timeIs0, bool spawnTogether)
        {
            if (ThreadManager.host)
            {
                int freeInstance = 1; // Start at 1 because 0 is the default instance
                while (H3MP.GameManager.activeInstances.ContainsKey(freeInstance))
                {
                    ++freeInstance;
                }

                RaidInstance raidInstance = new RaidInstance(freeInstance, map, timeIs0, spawnTogether);

                raidInstances.Add(freeInstance, raidInstance);

                H3MP.GameManager.activeInstances.Add(freeInstance, 0);

                OnInstanceReceived(raidInstance);

                using (Packet packet = new Packet(addInstancePacketID))
                {
                    packet.Write(freeInstance);
                    packet.Write(map);
                    packet.Write(timeIs0);
                    packet.Write(spawnTogether);
                    packet.Write(true);
                    packet.Write(1);
                    packet.Write(0);
                    packet.Write(1);
                    packet.Write(0);

                    ServerSend.SendTCPDataToAll(packet, true);
                }

                return raidInstance;
            }
            else
            {
                using (Packet packet = new Packet(addInstancePacketID))
                {
                    packet.Write(map);
                    packet.Write(timeIs0);
                    packet.Write(spawnTogether);

                    ClientSend.SendTCPData(packet, true);
                }

                return null;
            }
        }

        public static void OnInstanceReceived(RaidInstance raidInstance)
        {
            if (setLatestInstance)
            {
                setLatestInstance = false;

                if(HideoutController.instance != null)
                {
                    HideoutController.instance.SetInstance(raidInstance);
                }
            }
        }

        ////////////// SERVER HANDLERS

        public static void AddInstanceServerHandler(int clientID, Packet packet)
        {
            string map = packet.ReadString();
            bool timeIs0 = packet.ReadBool();
            bool spawnTogether = packet.ReadBool();

            RaidInstance newRaidInstance = AddInstance(map, timeIs0, spawnTogether);

            // Send to all clients
            using (Packet newPacket = new Packet(addInstancePacketID))
            {
                newPacket.Write(newRaidInstance.ID);
                newPacket.Write(map);
                newPacket.Write(timeIs0);
                newPacket.Write(spawnTogether);

                ServerSend.SendTCPDataToAll(newPacket, true);
            }
        }

        public static void SetPlayerReadynessServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool ready = packet.ReadBool();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                if (ready)
                {
                    if (raidInstance.players.Contains(clientID) && !raidInstance.readyPlayers.Contains(clientID))
                    {
                        raidInstance.readyPlayers.Add(clientID);
                    }
                }
                else
                {
                    raidInstance.readyPlayers.Remove(clientID);
                }

                // Send to all clients
                using (Packet newPacket = new Packet(setPlayerReadynessPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(clientID);
                    newPacket.Write(ready);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void SetInstanceWaitingServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool waiting = packet.ReadBool();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                bool preValue = raidInstance.waiting;
                raidInstance.waiting = waiting;

                // Start loading if necessary
                if(preValue != waiting && !waiting && Networking.currentInstance == raidInstance && HideoutController.instance != null && (HideoutController.instance.waitingInstancePage.activeSelf || HideoutController.instance.waitingInstancePlayerListPage.activeSelf))
                {
                    HideoutController.instance.OnWaitingInstanceStartClicked();
                }

                // Send to all clients
                using (Packet newPacket = new Packet(setInstanceWaitingPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(waiting);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void SetRaidEnemyIFFServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int enemyIFF = packet.ReadInt();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                raidInstance.enemyIFF = enemyIFF;

                // Send to all clients
                using (Packet newPacket = new Packet(setRaidEnemyIFFPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(enemyIFF);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void SetRaidActiveScavCountServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int activeScavCount = packet.ReadInt();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                raidInstance.activeScavCount = activeScavCount;

                // Send to all clients
                using (Packet newPacket = new Packet(setRaidActiveScavCountPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(activeScavCount);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void SetInstanceAIPMCSpawnedServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool spawned = packet.ReadBool();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                raidInstance.AIPMCSpawned = spawned;

                // Send to all clients
                using (Packet newPacket = new Packet(setInstanceAIPMCSpawnedPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(spawned);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void RequestSpawnServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int playerID = packet.ReadInt();
            bool PMC = packet.ReadBool();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                // We are instance host
                if(raidInstance.players[0] == GameManager.ID)
                {
                    if(RaidManager.instance == null)
                    {
                        spawnRequests.Add(new SpawnRequest(instanceID, playerID, PMC));
                    }
                    else
                    {
                        int spawnIndex = -1;
                        bool consume = false;
                        if (PMC)
                        {
                            if (currentInstance.PMCSpawnIndices.Count > 0)
                            {
                                spawnIndex = currentInstance.PMCSpawnIndices[UnityEngine.Random.Range(0, currentInstance.PMCSpawnIndices.Count)];
                                consume = true;
                            }
                            else // No more unused indices, will have to spawn with another PMC
                            {
                                spawnIndex = UnityEngine.Random.Range(0, RaidManager.instance.PMCSpawns.Count);
                            }
                        }
                        else
                        {
                            if (currentInstance.ScavSpawnIndices.Count > 0)
                            {
                                spawnIndex = currentInstance.ScavSpawnIndices[UnityEngine.Random.Range(0, currentInstance.ScavSpawnIndices.Count)];
                                consume = true;
                            }
                            else // No more unused indices, will have to reuse a scav spawn
                            {
                                spawnIndex = UnityEngine.Random.Range(0, RaidManager.instance.scavSpawns.Count);
                            }
                        }
                        if (consume)
                        {
                            currentInstance.ConsumeSpawn(spawnIndex, PMC, RaidManager.instance.PMCSpawns.Count, RaidManager.instance.scavSpawns.Count, true);
                        }

                        using (Packet newPacket = new Packet(spawnReturnPacketID))
                        {
                            newPacket.Write(instanceID);
                            newPacket.Write(playerID);
                            newPacket.Write(spawnIndex);
                            newPacket.Write(PMC);

                            ServerSend.SendTCPData(playerID, newPacket, true);
                        }
                    }
                }
                else // Instance host is someone else, relay to them
                {
                    using (Packet newPacket = new Packet(requestSpawnPacketID))
                    {
                        newPacket.Write(instanceID);
                        newPacket.Write(playerID);
                        newPacket.Write(PMC);

                        ServerSend.SendTCPData(raidInstance.players[0], newPacket, true);
                    }
                }
            }
        }

        public static void SpawnReturnServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int playerID = packet.ReadInt();
            int spawnIndex = packet.ReadInt();
            bool PMC = packet.ReadBool();

            if(playerID == GameManager.ID)
            {
                if(currentInstance != null && spawnRequested)
                {
                    spawnRequested = false;

                    if(RaidManager.instance != null)
                    {
                        RaidManager.instance.spawn = PMC ? RaidManager.instance.PMCSpawns[spawnIndex] : RaidManager.instance.scavSpawns[spawnIndex];
                        RaidManager.instance.Spawn(RaidManager.instance.spawn);
                    }
                }
            }
            else
            {
                using (Packet newPacket = new Packet(requestSpawnPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(playerID);
                    newPacket.Write(spawnIndex);
                    newPacket.Write(PMC);

                    ServerSend.SendTCPData(playerID, newPacket, true);
                }
            }
        }

        public static void ConsumeSpawnServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int spawnIndex = packet.ReadInt();
            bool PMC = packet.ReadBool();
            int PMCSpawnCount = packet.ReadInt();
            int scavSpawnCount = packet.ReadInt();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                raidInstance.ConsumeSpawn(spawnIndex, PMC, PMCSpawnCount, scavSpawnCount, false);

                // Send to all clients
                using (Packet newPacket = new Packet(consumeSpawnPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(spawnIndex);
                    newPacket.Write(PMC);
                    newPacket.Write(PMCSpawnCount);
                    newPacket.Write(scavSpawnCount);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        public static void BreachDoorServerHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool correctSide = packet.ReadBool();

            TrackedDoorData trackedDoor = Server.objects[trackedID] as TrackedDoorData;
            if (trackedDoor != null && trackedDoor.physical != null)
            {
                trackedDoor.physicalDoor.physicalDoor.AttemptBreach(correctSide, false);
            }

            using(Packet newPacket = new Packet(breachDoorPacketID))
            {
                newPacket.Write(trackedID);
                newPacket.Write(correctSide);

                ServerSend.SendTCPDataToAll(newPacket, true);
            }
        }

        public static void UnlockDoorServerHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedDoorData trackedDoor = Server.objects[trackedID] as TrackedDoorData;
            if (trackedDoor != null)
            {
                trackedDoor.locked = false;

                if (trackedDoor.physical != null)
                {
                    trackedDoor.physicalDoor.physicalDoor.lockScript.UnlockAction(false);
                }
            }

            using(Packet newPacket = new Packet(unlockDoorPacketID))
            {
                newPacket.Write(trackedID);

                ServerSend.SendTCPDataToAll(newPacket, true);
            }
        }

        public static void UnlockLootContainerServerHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedLootContainerData trackedLC = Server.objects[trackedID] as TrackedLootContainerData;
            if (trackedLC != null)
            {
                trackedLC.locked = false;

                if (trackedLC.physical != null)
                {
                    trackedLC.physicalLootContainer.physicalLootContainer.lockScript.UnlockAction(false);
                }
            }

            using(Packet newPacket = new Packet(unlockLootContainerPacketID))
            {
                newPacket.Write(trackedID);

                ServerSend.SendTCPDataToAll(newPacket, true);
            }
        }

        public static void SetLootContainerContentSpawnedServerHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedLootContainerData trackedLootContainer = Server.objects[trackedID] as TrackedLootContainerData;
            if (trackedLootContainer != null)
            {
                trackedLootContainer.spawnedContents = true;

                if (trackedLootContainer.physical != null)
                {
                    trackedLootContainer.physicalLootContainer.physicalLootContainer.contentsSpawned = true;
                }
            }

            using(Packet newPacket = new Packet(setLootContainerContentSpawnedPacketID))
            {
                newPacket.Write(trackedID);

                ServerSend.SendTCPDataToAll(newPacket, true);
            }
        }

        public static void SetModulPartDataServerHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Server.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                string givenGroupID = packet.ReadString();
                bool FIR = packet.ReadBool();
                bool insured = packet.ReadBool();

                int offset = 0;
                int partCount = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                offset += 4;
                for (int j = 0; j < partCount; ++j)
                {
                    int groupIDLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                    offset += 4;
                    string groupID = Encoding.ASCII.GetString(trackedItemData.additionalData, offset, groupIDLength);
                    offset += groupIDLength;
                    int partLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                    offset += 4;
                    offset += partLength;

                    offset += 4; // To skip the custom data length

                    if (givenGroupID.Equals(groupID))
                    {
                        for (int i = offset; i < trackedItemData.additionalData.Length - 2; ++i)
                        {
                            // Find EFM data
                            if (trackedItemData.additionalData[i] == 5 && trackedItemData.additionalData[i + 1] == 7 && trackedItemData.additionalData[i + 2] == 18)
                            {
                                i += 3;

                                byte tarkovIDLength = trackedItemData.additionalData[i++];
                                if (tarkovIDLength > 0)
                                {
                                    i += tarkovIDLength;
                                    trackedItemData.additionalData[i] = (byte)(((0 | (insured ? 1 : 0)) << 1) | (FIR ? 1 : 0));
                                }

                                break;
                            }
                        }
                    }
                    else // Not part we want to modify data of
                    {
                        int customDataLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                        offset += customDataLength;
                        offset += 4;
                    }
                }

                if (trackedItemData.physical != null)
                {
                    IModularWeapon modularWeapon = trackedItemData.physicalItem.dataObject as IModularWeapon;
                    Dictionary<string, ModularWeaponPartsAttachmentPoint> pointDict = modularWeapon.AllAttachmentPoints;
                    MeatovItem partMeatovItem = pointDict[givenGroupID].ModularPartPoint.GetComponent<MeatovItem>();
                    if(partMeatovItem != null)
                    {
                        if(partMeatovItem.foundInRaid != FIR)
                        {
                            partMeatovItem.dontProcessPartFIRChanged = true;
                            partMeatovItem.foundInRaid = FIR;
                        }
                        if(partMeatovItem.insured != insured)
                        {
                            partMeatovItem.dontProcessPartInsuredChanged = true;
                            partMeatovItem.insured = insured;
                        }
                    }
                }

                using (Packet newPacket = new Packet(setModulPartDataPacketID))
                {
                    newPacket.Write(trackedID);
                    newPacket.Write(givenGroupID);
                    newPacket.Write(FIR);
                    newPacket.Write(insured);

                    ServerSend.SendTCPDataToAll(clientID, newPacket, true);
                }
            }
        }

        ////////////// CLIENT HANDLERS

        public static void AddInstanceClientHandler(int clientID, Packet packet)
        {
            int ID = packet.ReadInt();
            string map = packet.ReadString();
            bool timeIs0 = packet.ReadBool();
            bool spawnTogether = packet.ReadBool();
            bool waiting = packet.ReadBool();
            int playerCount = packet.ReadInt();
            List<int> players = new List<int>();
            for(int i=0; i < playerCount; ++i)
            {
                players.Add(packet.ReadInt());
            }
            int readyPlayerCount = packet.ReadInt();
            List<int> readyPlayers = new List<int>();
            for(int i=0; i < readyPlayerCount; ++i)
            {
                readyPlayers.Add(packet.ReadInt());
            }

            if (!H3MP.GameManager.activeInstances.ContainsKey(ID))
            {
                H3MP.GameManager.activeInstances.Add(ID, playerCount);
            }
            RaidInstance newRaidInstance = new RaidInstance(ID, map, timeIs0, spawnTogether, waiting, players, readyPlayers);
            raidInstances.Add(ID, newRaidInstance);

            // Only want to set dontAddToInstance if we want to join the TNH instance upon receiving it
            H3MP.GameManager.dontAddToInstance = setLatestInstance;
            OnInstanceReceived(newRaidInstance);
        }

        public static void SetPlayerReadynessClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int playerID = packet.ReadInt();
            bool ready = packet.ReadBool();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                if (ready)
                {
                    if (raidInstance.players.Contains(playerID) && !raidInstance.readyPlayers.Contains(playerID))
                    {
                        raidInstance.readyPlayers.Add(playerID);
                    }
                }
                else
                {
                    raidInstance.readyPlayers.Remove(playerID);
                }
            }
        }

        public static void SetInstanceWaitingClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool waiting = packet.ReadBool();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                bool preValue = raidInstance.waiting;
                raidInstance.waiting = waiting;

                // Start loading if necessary
                if (preValue != waiting && !waiting && Networking.currentInstance == raidInstance && HideoutController.instance != null && (HideoutController.instance.waitingInstancePage.activeSelf || HideoutController.instance.waitingInstancePlayerListPage.activeSelf))
                {
                    HideoutController.instance.OnWaitingInstanceStartClicked();
                }
            }
        }

        public static void SetEnemyIFFClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int enemyIFF = packet.ReadInt();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                raidInstance.enemyIFF = enemyIFF;
            }
        }

        public static void SetActiveScavCountClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int activeScavCount = packet.ReadInt();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                raidInstance.activeScavCount = activeScavCount;
            }
        }

        public static void SetInstanceAIPMCSpawnedClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool spawned = packet.ReadBool();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                raidInstance.AIPMCSpawned = spawned;
            }
        }

        public static void RequestSpawnClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int playerID = packet.ReadInt();
            bool PMC = packet.ReadBool();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                // We are instance host
                if (raidInstance.players[0] == GameManager.ID)
                {
                    if (RaidManager.instance == null)
                    {
                        spawnRequests.Add(new SpawnRequest(instanceID, playerID, PMC));
                    }
                    else
                    {
                        int spawnIndex = -1;
                        bool consume = false;
                        if (PMC)
                        {
                            if(currentInstance.PMCSpawnIndices.Count > 0)
                            {
                                spawnIndex = currentInstance.PMCSpawnIndices[UnityEngine.Random.Range(0, currentInstance.PMCSpawnIndices.Count)];
                                consume = true;
                            }
                            else // No more unused indices, will have to spawn with another PMC
                            {
                                spawnIndex = UnityEngine.Random.Range(0, RaidManager.instance.PMCSpawns.Count);
                            }
                        }
                        else
                        {
                            if (currentInstance.ScavSpawnIndices.Count > 0)
                            {
                                spawnIndex = currentInstance.ScavSpawnIndices[UnityEngine.Random.Range(0, currentInstance.ScavSpawnIndices.Count)];
                                consume = true;
                            }
                            else // No more unused indices, will have to reuse a scav spawn
                            {
                                spawnIndex = UnityEngine.Random.Range(0, RaidManager.instance.scavSpawns.Count);
                            }
                        }
                        if (consume)
                        {
                            currentInstance.ConsumeSpawn(spawnIndex, PMC, RaidManager.instance.PMCSpawns.Count, RaidManager.instance.scavSpawns.Count, true);
                        }

                        using(Packet newPacket = new Packet(spawnReturnPacketID))
                        {
                            newPacket.Write(instanceID);
                            newPacket.Write(playerID);
                            newPacket.Write(spawnIndex);
                            newPacket.Write(PMC);

                            ClientSend.SendTCPData(newPacket, true);
                        }
                    }
                }
                // else // Instance host is someone else. Possible if host changed while request was being sent.
                // This case will be handle b the player who requested, who, upon seeing that the host changed, if they haven't received 
                // their requested spawn yet, will rerequest it
            }
        }

        public static void SpawnReturnClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int playerID = packet.ReadInt();
            int spawnIndex = packet.ReadInt();
            bool PMC = packet.ReadBool();

            if (playerID == GameManager.ID)
            {
                if (currentInstance != null && spawnRequested)
                {
                    spawnRequested = false;

                    if (RaidManager.instance != null)
                    {
                        RaidManager.instance.spawn = PMC ? RaidManager.instance.PMCSpawns[spawnIndex] : RaidManager.instance.scavSpawns[spawnIndex];
                        RaidManager.instance.Spawn(RaidManager.instance.spawn);
                    }
                }
            }
        }

        public static void ConsumeSpawnClientHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            int spawnIndex = packet.ReadInt();
            bool PMC = packet.ReadBool();
            int PMCSpawnCount = packet.ReadInt();
            int scavSpawnCount = packet.ReadInt();

            if (raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance))
            {
                raidInstance.ConsumeSpawn(spawnIndex, PMC, PMCSpawnCount, scavSpawnCount, false);
            }
        }

        public static void BreachDoorClientHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();
            bool correctSide = packet.ReadBool();

            TrackedDoorData trackedDoor = Client.objects[trackedID] as TrackedDoorData;
            if (trackedDoor != null && trackedDoor.physical != null)
            {
                trackedDoor.physicalDoor.physicalDoor.AttemptBreach(correctSide, false);
            }
        }

        public static void UnlockDoorClientHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedDoorData trackedDoor = Client.objects[trackedID] as TrackedDoorData;
            if (trackedDoor != null)
            {
                trackedDoor.locked = false;

                if (trackedDoor.physical != null)
                {
                    trackedDoor.physicalDoor.physicalDoor.lockScript.UnlockAction(false);
                }
            }
        }

        public static void UnlockLootContainerClientHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedLootContainerData trackedLC = Client.objects[trackedID] as TrackedLootContainerData;
            if (trackedLC != null)
            {
                trackedLC.locked = false;

                if (trackedLC.physical != null)
                {
                    trackedLC.physicalLootContainer.physicalLootContainer.lockScript.UnlockAction(false);
                }
            }
        }

        public static void SetLootContainerContentSpawnedClientHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedLootContainerData trackedLootContainer = Client.objects[trackedID] as TrackedLootContainerData;
            if (trackedLootContainer != null)
            {
                trackedLootContainer.spawnedContents = true;

                if (trackedLootContainer.physical != null)
                {
                    trackedLootContainer.physicalLootContainer.physicalLootContainer.contentsSpawned = true;
                }
            }
        }

        public static void SetModulPartDataClientHandler(int clientID, Packet packet)
        {
            int trackedID = packet.ReadInt();

            TrackedItemData trackedItemData = Client.objects[trackedID] as TrackedItemData;
            if (trackedItemData != null)
            {
                string givenGroupID = packet.ReadString();
                bool FIR = packet.ReadBool();
                bool insured = packet.ReadBool();

                int offset = 0;
                int partCount = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                offset += 4;
                for (int j = 0; j < partCount; ++j)
                {
                    int groupIDLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                    offset += 4;
                    string groupID = Encoding.ASCII.GetString(trackedItemData.additionalData, offset, groupIDLength);
                    offset += groupIDLength;
                    int partLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                    offset += 4;
                    offset += partLength;

                    offset += 4; // To skip the custom data length

                    if (givenGroupID.Equals(groupID))
                    {
                        for (int i = offset; i < trackedItemData.additionalData.Length - 2; ++i)
                        {
                            // Find EFM data
                            if (trackedItemData.additionalData[i] == 5 && trackedItemData.additionalData[i + 1] == 7 && trackedItemData.additionalData[i + 2] == 18)
                            {
                                i += 3;

                                byte tarkovIDLength = trackedItemData.additionalData[i++];
                                if (tarkovIDLength > 0)
                                {
                                    i += tarkovIDLength;
                                    trackedItemData.additionalData[i] = (byte)(((0 | (insured ? 1 : 0)) << 1) | (FIR ? 1 : 0));
                                }

                                break;
                            }
                        }
                    }
                    else // Not part we want to modify data of
                    {
                        int customDataLength = BitConverter.ToInt32(trackedItemData.additionalData, offset);
                        offset += customDataLength;
                        offset += 4;
                    }
                }

                if (trackedItemData.physical != null)
                {
                    IModularWeapon modularWeapon = trackedItemData.physicalItem.dataObject as IModularWeapon;
                    Dictionary<string, ModularWeaponPartsAttachmentPoint> pointDict = modularWeapon.AllAttachmentPoints;
                    MeatovItem partMeatovItem = pointDict[givenGroupID].ModularPartPoint.GetComponent<MeatovItem>();
                    if (partMeatovItem != null)
                    {
                        if (partMeatovItem.foundInRaid != FIR)
                        {
                            partMeatovItem.dontProcessPartFIRChanged = true;
                            partMeatovItem.foundInRaid = FIR;
                        }
                        if (partMeatovItem.insured != insured)
                        {
                            partMeatovItem.dontProcessPartInsuredChanged = true;
                            partMeatovItem.insured = insured;
                        }
                    }
                }
            }
        }
    }

    public class SpawnRequest
    {
        public int instanceID;
        public int playerID;
        public bool PMC;

        public SpawnRequest(int instanceID, int playerID, bool PMC)
        {
            this.instanceID = instanceID;
            this.playerID = playerID;
            this.PMC = PMC;
        }
    }
}
