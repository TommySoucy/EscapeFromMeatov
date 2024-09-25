using UnityEngine;

namespace EFM
{
    public class AI : MonoBehaviour
    {
        public int experienceReward;
        public bool PMC;
        public bool USEC;
        public bool scav;
        public string dataName;
        public BotInventory botInventory;

        public KillData latestDamageSourceKillData;
    }
}
