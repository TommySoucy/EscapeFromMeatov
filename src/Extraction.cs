using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Extraction : MonoBehaviour
    {
        public float distToSpawn;
        public bool active;
        public int extractionsIndex; // Current index in RaidManager extractions list

        public string name;
        public List<Vector2> activeTimes; // Time ranges in seconds from 0 to 86400 (24h) during which this extraction can actually be used
        public Dictionary<string, int> itemRequirements; // Items consumed upon using the extraction
        public List<string> itemWhitelist; // Items that must be in player inventory upon extraction, not consumed
        public List<string> itemBlacklist; // Items that must NOT be in player inventory upon extraction
    }
}
