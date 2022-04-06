using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_Effect
    {
        public static List<EFM_Effect> effects; // Active effects

        public enum EffectType
        {
            SkillRate, // Increases skill level
            EnergyRate, // Adds/Subtract energy rate
            HydrationRate, // Adds/Subtract hydration rate
            MaxStamina, // Adds/Subtract max stamina
            StaminaRate, // Adds/Subtract stamina rates
            HandsTremor, // Causes hands to shake
            QuantumTunnelling, // Activates vignettes
            HealthRate, // Adds/Subtract healing rate
            RemoveAllBloodLosses, // Removes and prevents blood loss
            Contusion, // No haptic feedbak, lowers volume by 0.33
            WeightLimit, // Adds/Subtract weightlimit above which cant sprint
            DamageModifier, // Multiplies amount of damage taken
            Pain, // Causes tremors
            StomachBloodloss, // Energy/Hydration rates multiplied by 5
            UnknownToxin, // Causes pain and negative health rate
            BodyTemperature, // Adds/Subtract from body temp
            Antidote, // Removes toxin with 60s delay
            LightBleeding, // Causes negative healthrate in each bodypart
            HeavyBleeding, // Causes negative healthrate in each bodypart
            Fracture, // Causes pain
            Dehydration, // Causes damage
            HeavyDehydration, // Causes more damage
            Fatigue, // Cant sprint
            HeavyFatigue, // Cant sprint, cause damage
            OverweightFatigue, // Causes EnergyRate

            RadExposure,
            Intoxication,
            DestroyedPart
        }
        public EffectType effectType;

        public bool active = false;
        public float timer = 0;
        public bool hasTimer = false;
        public float inactiveTimer = 0;
        public float delay = 0;
        public float healthLoopTime = 60;
        public float healthRate = 0;
        public float energyLoopTime = 60;
        public float energyRate = 0;
        public float hydrationLoopTime = 60;
        public float hydrationRate = 0;
        public float audioVolumeMultiplier = 1.0f;
        public float healExperience = 0;
        public int removePrice = 0;
        public int partIndex = -1; // 0 Head, 1 Chest, 2 Stomach, 3 LeftArm, 4 RightArm, 5 LeftLeg, 6 RightLeg
        public int skillIndex = -1;
        public float value;
        public List<EFM_Effect> caused = new List<EFM_Effect>();
        public bool nonLethal = false;
    }

    public class EFM_Effect_Buff
    {
        public EFM_Effect.EffectType effectType;
        public float chance = 1;
        public float delay = 0;
        public float duration = 0;
        public float value = 0;
        public bool absolute = false;
        public int skillIndex = -1;
    }

    public class EFM_Effect_Consumable
    {
        public enum EffectConsumable
        {
            // Health
            Hydration,
            Energy,

            // Damage
            RadExposure,
            Pain,
            Contusion,
            Intoxication,
            LightBleeding,
            Fracture,
            DestroyedPart,
            HeavyBleeding
        }
        public EffectConsumable effectType;

        // Damage
        public int cost;
        public float delay;
        public float duration;
        public float fadeOut;
        public float healthPenaltyMax;
        public float healthPenaltyMin;

        // Health
        public float value;
    }
}
