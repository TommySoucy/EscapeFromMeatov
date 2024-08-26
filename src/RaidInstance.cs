using H3MP.Networking;
using System.Collections.Generic;

namespace EFM
{
    public class RaidInstance
    {
        public int ID;
        public string map;
        public bool timeIs0;
        public bool spawnTogether;

        // Live
        public bool waiting; // false means game is ongoing
        public List<int> players = new List<int>();
        public List<int> readyPlayers = new List<int>();

        // Raid
        public List<int> PMCSpawnIndices;
        public List<int> ScavSpawnIndices;
        public int consumedSpawnCount;
        public bool AIPMCSpawned;

        public RaidInstance(int ID, string map, bool timeIs0, bool spawnTogether)
        {
            this.ID = ID;
            this.map = map;
            this.timeIs0 = timeIs0;
            this.spawnTogether = spawnTogether;
        }

        public RaidInstance(int ID, string map, bool timeIs0, bool spawnTogether, bool waiting, List<int> players, List<int> readyPlayers)
        {
            this.ID = ID;
            this.map = map;
            this.timeIs0 = timeIs0;
            this.spawnTogether = spawnTogether;

            this.waiting = waiting;
            this.players = players;
            this.readyPlayers = readyPlayers;
        }

        public bool AllPlayersReady()
        {
            for(int i = 0; i < players.Count; ++i)
            {
                if (!readyPlayers.Contains(players[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public void InitSpawnIndices(int PMCSpawnCount, int scavSpawnCount)
        {
            if (PMCSpawnIndices == null)
            {
                PMCSpawnIndices = new List<int>();
                for (int i = 0; i < PMCSpawnCount; ++i)
                {
                    PMCSpawnIndices.Add(i);
                }
                ScavSpawnIndices = new List<int>();
                for (int i = 0; i < scavSpawnCount; ++i)
                {
                    ScavSpawnIndices.Add(i);
                }
            }
        }

        public void ConsumeSpawn(int index, bool PMC, int PMCSpawnCount, int scavSpawnCount, bool send = false)
        {
            ++consumedSpawnCount;

            InitSpawnIndices(PMCSpawnCount, scavSpawnCount);

            // Consume
            if (PMC)
            {
                PMCSpawnIndices.Remove(index);
            }
            else
            {
                ScavSpawnIndices.Remove(index);
            }

            // Tell others
            if (send)
            {
                using (Packet packet = new Packet(Networking.consumeSpawnPacketID))
                {
                    packet.Write(ID);
                    packet.Write(index);
                    packet.Write(PMC);
                    packet.Write(PMCSpawnCount);
                    packet.Write(scavSpawnCount);

                    if (ThreadManager.host)
                    {
                        ServerSend.SendTCPDataToAll(packet);
                    }
                    else
                    {
                        ClientSend.SendTCPData(packet);
                    }
                }
            }
        }
    }
}
