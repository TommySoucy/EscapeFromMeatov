using System.Collections.Generic;

namespace EFM
{
    public class KillData
    {
        TODO: // Make sure we generate these in raid, check if we can use what we do in H3MP to track which shot was ours, and fill Mod.raidKills
        public string name;
        public int baseExperienceReward;
        public float distance;
        public ConditionCounter.EnemyTarget enemyTarget;
        public string savageRole;
        public MeatovItemData weaponData;
        public List<MeatovItemData> weaponChildrenData;
        public List<HealthEffectEntry> enemyHealthEffects;
        public ConditionCounter.TargetBodyPart bodyPart;
        public float killTime;
        public int level;
    }
}
