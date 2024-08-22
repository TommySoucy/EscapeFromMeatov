using H3MP.Networking;
using System.Collections.Generic;

namespace EFM
{
    public class Networking
    {
        public static RaidInstance potentialInstance;
        public static RaidInstance currentInstance;
        public static Dictionary<int, RaidInstance> raidInstances = new Dictionary<int, RaidInstance>();
        public static bool setLatestInstance;

        public static int addInstancePacketID; // AddEFMInstancePacketID
        public static int setPlayerReadynessPacketID; // SetEFMInstancePlayerReadyness
        public static int setInstanceWaitingPacketID; // SetEFMInstanceWaitingPacketID

        public static void OnConnection()
        {
            // Sub to events
            H3MP.GameManager.OnPlayerInstanceChanged += OnPlayerInstanceChanged;
            H3MP.Mod.OnGetBestPotentialObjectHost += OnGetBestPotentialObjectHost;

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
            }
        }

        public static void OnDisconnection()
        {
            // Unsub from events
            H3MP.GameManager.OnPlayerInstanceChanged -= OnPlayerInstanceChanged;
            H3MP.Mod.OnGetBestPotentialObjectHost -= OnGetBestPotentialObjectHost;
        }

        public static void OnPlayerInstanceChanged(int ID, int previousInstanceID, int newInstanceID)
        {
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

                    ServerSend.SendTCPDataToAll(newPacket, true);
                }
            }
        }

        public static void SetInstanceWaitingServerHandler(int clientID, Packet packet)
        {
            int instanceID = packet.ReadInt();
            bool waiting = packet.ReadBool();

            if(raidInstances.TryGetValue(instanceID, out RaidInstance raidInstance) && raidInstance.players[0] == clientID)
            {
                raidInstance.waiting = waiting;

                // Send to all clients
                using (Packet newPacket = new Packet(setInstanceWaitingPacketID))
                {
                    newPacket.Write(instanceID);
                    newPacket.Write(waiting);

                    ServerSend.SendTCPDataToAll(newPacket, true);
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
                raidInstance.waiting = waiting;
            }
        }
    }
}
