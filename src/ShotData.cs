using System;
using System.Collections.Generic;

namespace EFM
{
    public class ShotData
    {
        TODO: // Make sure we generate these in raid, check if we can use what we do in H3MP to track which shot was ours
        public float distance;
        public ConditionCounter.EnemyTarget enemyTarget;
        public string savageRole;
        public MeatovItemData weaponData;
        public List<MeatovItemData> weaponChildrenData;
        public List<HealthEffectEntry> enemyHealthEffects;
        public ConditionCounter.TargetBodyPart bodyPart;
        public DateTime killTime;
    }
}
