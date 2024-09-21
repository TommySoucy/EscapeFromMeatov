using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class ScavBossSpawn : Spawn
    {
        TODO: // set in scene
        // All spawns that should be considered the same as this one
        // These spawns will not be used to spawn a boss if this one was chosen
        public List<ScavBossSpawn> spawnGroup;
        public int totalGroupWeight; // The total probability weight of the group
        public int groupWeight; // Probability weight of this spawn in the group
        public float probability; // Even if chosen as boss spawn, probability that a boss will be spawned
        public string bossID; // Name of bot type file
        public List<string> squadMembers; // Names of bot ype files for this boss's squadmembers
        public bool spawnAllSquadMembers; // Whether we want all of given squadmembers to be spawned
        public int squadMembersSpawnAttempts;
        public float squadMembersSpawnProbability;
    }
}
