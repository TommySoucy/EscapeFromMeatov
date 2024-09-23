using System;
using System.Collections.Generic;

namespace EFM
{
    public class ShotData
    {
        // BallisticProjectile.MoveBullet calls Damage
        // Explosion.Explode calls Damage
        // GrenadeExplosion.Explode calls Damage
        // We care about shots damaging SosigLink and H3MP.PlayerHitbox
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
