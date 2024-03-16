using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class KillData
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
