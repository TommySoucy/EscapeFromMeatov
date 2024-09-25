using System.Collections.Generic;

namespace EFM
{
    public class KillData
    {
        // Kill tracking process:
        // The two damagers we care about are BallisticProjectile, Explosion, and GrenadeExplosion
        // When these cause damage we store the latest damage data as a kill data somewhere on the damaged entity
        // We only care about damage done to SosigLinks for which we store the KillData in the Sosig's AI component
        // For SosigLink, when SceneSettings.OnSosigKill gets invoked from Sosig.SosigDies, we check if we have kill data
        // which would mean we are the last one to have damaged that Sosig, awarding us the kill
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
