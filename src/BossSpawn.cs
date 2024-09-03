using System.Collections.Generic;

namespace EFM
{
    public class BossSpawn : Spawn
    {
        public string boss; // Name of data file in DB
        public List<string> squadMembers; // Name of data files in DB
        public int probabilityWeight; // Weight of spawn probability compared to other boss spawns in the scene
        public float spawnProbability; // Actual spawn probability
    }
}
