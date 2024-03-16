using System;
using System.Collections.Generic;

namespace EFM
{
    public class ShotData
    {
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
