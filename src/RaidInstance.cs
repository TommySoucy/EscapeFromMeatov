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
    }
}
