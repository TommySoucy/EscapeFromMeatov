using H3MP.Networking;
using System.Collections.Generic;

namespace EFM
{
    public class RaidInstance
    {
        public int ID;
        public bool waiting; // false means game is ongoing
        public string map;
        public bool timeIs0;
        public bool spawnTogether;
        public List<int> players = new List<int>();

        public RaidInstance(int ID, string map, bool timeIs0, bool spawnTogether)
        {
            this.ID = ID;
            this.map = map;
            this.timeIs0 = timeIs0;
            this.spawnTogether = spawnTogether;
        }

        public RaidInstance(int ID, string map, bool timeIs0, bool spawnTogether, List<int> players)
        {
            this.ID = ID;
            this.map = map;
            this.timeIs0 = timeIs0;
            this.spawnTogether = spawnTogether;
            this.players = players;
        }
    }
}
