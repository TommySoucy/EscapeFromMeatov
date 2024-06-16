using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Effect
    {
        public static Dictionary<EffectType, List<Effect>> effectsByType = new Dictionary<EffectType, List<Effect>>();
        public static Dictionary<EffectType, float> inactiveTimersByType = new Dictionary<EffectType, float>();

        public static float damageModifier; 

        public enum EffectType
        {
            SkillRate, // In/decreases skill level
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
            DamageModifier, // Adds to damage taken: Damage + Damage * DamageModifier value
            Pain, // Causes tremors
            StomachBloodloss, // Energy/Hydration rates multiplied by 5
            UnknownToxin, // Causes pain and negative health rate
            BodyTemperature, // Adds/Subtract from body temp
            Antidote, // Removes toxin with 60s delay
            LightBleeding, // Causes negative healthrate in each bodypart
            HeavyBleeding, // Causes negative healthrate in each bodypart
            Fracture, // Causes pain
            Dehydration, // Causes damage, causes pain
            HeavyDehydration, // Causes more damage
            Fatigue, // Cant sprint
            HeavyFatigue, // Cant sprint, cause damage
            OverweightFatigue, // Causes EnergyRate

            RadExposure,
            Intoxication,
            DestroyedPart
        }
        public EffectType effectType;

        public bool previousActive = false;
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
        public Effect parentEffect; // Effect that caused this effect
        public List<Effect> caused = new List<Effect>(); // Effects caused by this effect
        public bool nonLethal = false;
        public bool hideoutOnly = false;
        public bool fromStimulator = false;

        public delegate void OnEffectAddedDelegate(Effect effect);
        public static event OnEffectAddedDelegate OnEffectAdded;
        public delegate void OnEffectActivatedDelegate(Effect effect);
        public static event OnEffectActivatedDelegate OnEffectActivated;
        public delegate void OnEffectDeactivatedDelegate(Effect effect);
        public static event OnEffectDeactivatedDelegate OnEffectDeactivated;
        public delegate void OnEffectRemovedDelegate(Effect effect);
        public static event OnEffectRemovedDelegate OnEffectRemoved;

        public Effect(BuffEffect buffEffect)
        {
            td
        }

        public static void UpdateStatic()
        {
            // Update global timers
            List<EffectType> toRemove = new List<EffectType>();
            foreach (KeyValuePair<EffectType, float> timer in inactiveTimersByType)
            { 
                float newValue = timer.Value - Time.deltaTime;
                if(newValue <= 0)
                {
                    toRemove.Add(timer.Key);
                }
                else
                {
                    inactiveTimersByType[timer.Key] = newValue;
                }
            }
            for(int i=0; i < toRemove.Count; ++i)
            {
                inactiveTimersByType.Remove(toRemove[i]);
            }

            // Update effect instances
            foreach (KeyValuePair<EffectType, List<Effect>> effectEntry in effectsByType)
            {
                for (int i = 0; i < effectEntry.Value.Count; ++i)
                {
                    effectEntry.Value[i].Update();
                }
            }
        }

        public void Update()
        {
            // Update timers
            if (active)
            {
                if (hasTimer)
                {
                    if(timer > 0)
                    {
                        timer -= Time.deltaTime;
                    }
                    else
                    {
                        active = false;
                        RemoveEffect(this, parentEffect == null ? null : parentEffect.caused);
                    }
                }
            }
            else
            {
                // Note that an effect can only become active once delay or timer have ran out
                // If inactive but there is no timer, it will remain inactive forever
                if(delay > 0)
                {
                    delay -= Time.deltaTime;
                    if(delay <= 0 && inactiveTimer <= 0 && (parentEffect == null || parentEffect.active))
                    {
                        Activate();
                    }
                }
                else if(inactiveTimer > 0)
                {
                    inactiveTimer -= Time.deltaTime;
                    if (delay <= 0 && inactiveTimer <= 0 && (parentEffect == null || parentEffect.active))
                    {
                        Activate();
                    }
                }
            }

            previousActive = active;
        }

        public void Activate()
        {
            if(parentEffect != null && !parentEffect.active)
            {
                return;
            }

            active = true;

            // Apply effect
            switch (effectType)
            {
                case EffectType.SkillRate:
                    Mod.skills[skillIndex].currentProgress += value;
                    break;
                case EffectType.EnergyRate:
                    Mod.currentEnergyRate += value;
                    break;
                case EffectType.HydrationRate:
                    Mod.currentHydrationRate += value;
                    break;
                case EffectType.MaxStamina:
                    Mod.currentMaxStamina += value;
                    break;
                case EffectType.StaminaRate:
                    Mod.currentStaminaRate += value;
                    break;
                case EffectType.HandsTremor:
                    TODO0: // Start tremors once implemented
                    Mod.LogWarning("Tremors not implemented yet, tried to start");
                    break;
                case EffectType.QuantumTunnelling:
                    TODO1: // Start tunnel vision once implemented
                    Mod.LogWarning("Tunnel vision not implemented yet, tried to start");
                    break;
                case EffectType.HealthRate:
                    if (value > 0)
                    {
                        Mod.SetBasePositiveHealthRate(partIndex, Mod.GetBasePositiveHealthRate(partIndex) - value);
                    }
                    else
                    {
                        if (nonLethal)
                        {
                            Mod.SetBaseNonLethalHealthRate(partIndex, Mod.GetBaseNonLethalHealthRate(partIndex) - value);
                        }
                        else
                        {
                            Mod.SetBaseNegativeHealthRate(partIndex, Mod.GetBaseNegativeHealthRate(partIndex) - value);
                        }
                    }
                    break;
                case EffectType.RemoveAllBloodLosses:
                    // Do nothing upon removing this effect
                    // The effect really just takes the form of an inactive timer on all blood losses
                    // Its deactivation/removal will be when the timer runs out
                    break;
                case EffectType.Contusion:
                    TODO2: // Start Contusion once implemented
                    Mod.LogWarning("Contusion not implemented yet, tried to start");
                    break;
                case EffectType.WeightLimit:
                    Mod.baseWeightLimit -= (int)value;
                    break;
                case EffectType.DamageModifier:
                    TODO5: // Implement with damage
                    damageModifier -= value;
                    break;
                case EffectType.Pain:
                    // Do nothing, really just there as a cause for other effects (Tremors)
                    break;
                case EffectType.StomachBloodloss:
                    // Do nothing, really just there as a cause for other effects (Energy and hydration rates)
                    break;
                case EffectType.UnknownToxin:
                    // Do nothing, really just there as a cause for other effects (Pain and healthrate)
                    break;
                case EffectType.BodyTemperature:
                    // Do nothing, really just there as a cause for other effects
                    break;
                case EffectType.Antidote:
                    // Do nothing, really just there as delayed removal for toxin
                    break;
                case EffectType.LightBleeding:
                    // Do nothing, really just there as a cause for other effects (Healthrate and Energyrate)
                    break;
                case EffectType.HeavyBleeding:
                    // Do nothing, really just there as a cause for other effects (Healthrate and Energyrate)
                    break;
                case EffectType.Fracture:
                    // Do nothing, really just there as a cause for other effects (Pain)
                    // Existence of this effect will also be used by other system for other behavior like bullet hit probability and random stagger 
                    // if walking on a fracture
                    break;
                case EffectType.Dehydration:
                    // Do nothing, really just there as a cause for other effects (Healthrate and pain)
                    break;
                case EffectType.HeavyDehydration:
                    // Do nothing, really just there as a cause for other effects (Healthrate and pain)
                    break;
                case EffectType.Fatigue:
                    // Do nothing, existence prevents sprint
                    break;
                case EffectType.HeavyFatigue:
                    // Do nothing, existence prevents sprint, also causes healthrate
                    break;
                case EffectType.OverweightFatigue:
                    // Do nothing, really just there as a cause for other effects (energyrate)
                    break;
            }

            // Activate caused effects after
            for (int i = 0; i < caused.Count; ++i)
            {
                caused[i].Activate();
            }

            OnEffectActivatedInvoke(this);
        }

        public void Deactivate()
        {
            active = false;

            // Deactivate caused effects first
            for(int i=0; i < caused.Count; ++i)
            {
                caused[i].Deactivate();
            }

            // Unapply effect
            switch (effectType)
            {
                case EffectType.SkillRate:
                    Mod.skills[skillIndex].currentProgress -= value;
                    break;
                case EffectType.EnergyRate:
                    Mod.currentEnergyRate -= value;
                    break;
                case EffectType.HydrationRate:
                    Mod.currentHydrationRate -= value;
                    break;
                case EffectType.MaxStamina:
                    Mod.currentMaxStamina -= value;
                    break;
                case EffectType.StaminaRate:
                    Mod.currentStaminaRate -= value;
                    break;
                case EffectType.HandsTremor:
                    if (effectsByType.TryGetValue(EffectType.HandsTremor, out List<Effect> otherTremors))
                    {
                        bool activeTremors = false;
                        for(int i=0; i < otherTremors.Count; ++i)
                        {
                            if (otherTremors[i].active)
                            {
                                activeTremors = true;
                                break;
                            }
                        }
                        if (!activeTremors)
                        {
                            TODO1: // Stop tremors once implemented
                            Mod.LogWarning("Tremors not implemented yet, tried to deactivate");
                        }
                    }
                    else
                    {
                    TODO1: // Stop tremors once implemented
                        Mod.LogWarning("Tremors not implemented yet, tried to deactivate");
                    }
                    break;
                case EffectType.QuantumTunnelling:
                    if (effectsByType.TryGetValue(EffectType.QuantumTunnelling, out List<Effect> otherTunnels))
                    {
                        bool activeTunnels = false;
                        for (int i = 0; i < otherTunnels.Count; ++i)
                        {
                            if (otherTunnels[i].active)
                            {
                                activeTunnels = true;
                                break;
                            }
                        }
                        if (!activeTunnels)
                        {
                            TODO1: // Stop tunnel vision once implemented
                            Mod.LogWarning("Tunnel vision not implemented yet, tried to deactivate");
                        }
                    }
                    else
                    {
                        TODO1: // Stop tunnel vision once implemented
                        Mod.LogWarning("Tunnel vision not implemented yet, tried to deactivate");
                    }
                    break;
                case EffectType.HealthRate:
                    if (value > 0)
                    {
                        Mod.SetBasePositiveHealthRate(partIndex, Mod.GetBasePositiveHealthRate(partIndex) - value);
                    }
                    else
                    {
                        if (nonLethal)
                        {
                            Mod.SetBaseNonLethalHealthRate(partIndex, Mod.GetBaseNonLethalHealthRate(partIndex) - value);
                        }
                        else
                        {
                            Mod.SetBaseNegativeHealthRate(partIndex, Mod.GetBaseNegativeHealthRate(partIndex) - value);
                        }
                    }
                    break;
                case EffectType.RemoveAllBloodLosses:
                    // Do nothing upon removing this effect
                    // The effect really just takes the form of an inactive timer on all blood losses
                    // Its deactivation/removal will be when the timer runs out
                    break;
                case EffectType.Contusion:
                    if (effectsByType.TryGetValue(EffectType.Contusion, out List<Effect> otherContusions))
                    {
                        bool activeContusions = false;
                        for (int i = 0; i < otherContusions.Count; ++i)
                        {
                            if (otherContusions[i].active)
                            {
                                activeContusions = true;
                                break;
                            }
                        }
                        if (!activeContusions)
                        {
                            TODO1: // Stop contusion once implemented
                            Mod.LogWarning("Contusion not implemented yet, tried to deactivate");
                        }
                    }
                    else
                    {
                        TODO1: // Stop contusion once implemented
                        Mod.LogWarning("Contusion not implemented yet, tried to deactivate");
                    }
                    break;
                case EffectType.WeightLimit:
                    Mod.baseWeightLimit -= (int)value;
                    break;
                case EffectType.DamageModifier:
                    TODO5: // Implement with damage
                    damageModifier -= value;
                    break;
                case EffectType.Pain:
                    // Do nothing, really just there as a cause for other effects (Tremors)
                    break;
                case EffectType.StomachBloodloss:
                    // Do nothing, really just there as a cause for other effects (Energy and hydration rates)
                    break;
                case EffectType.UnknownToxin:
                    // Do nothing, really just there as a cause for other effects (Pain and healthrate)
                    break;
                case EffectType.BodyTemperature:
                    // Do nothing, really just there as a cause for other effects
                    break;
                case EffectType.Antidote:
                    // Do nothing, really just there as delayed removal for toxin
                    break;
                case EffectType.LightBleeding:
                    // Do nothing, really just there as a cause for other effects (Healthrate and Energyrate)
                    break;
                case EffectType.HeavyBleeding:
                    // Do nothing, really just there as a cause for other effects (Healthrate and Energyrate)
                    break;
                case EffectType.Fracture:
                    // Do nothing, really just there as a cause for other effects (Pain)
                    // Existence of this effect will also be used by other system for other behavior like bullet hit probability and random stagger 
                    // if walking on a fracture
                    break;
                case EffectType.Dehydration:
                    // Do nothing, really just there as a cause for other effects (Healthrate and pain)
                    break;
                case EffectType.HeavyDehydration:
                    // Do nothing, really just there as a cause for other effects (Healthrate and pain)
                    break;
                case EffectType.Fatigue:
                    // Do nothing, existence prevents sprint
                    break;
                case EffectType.HeavyFatigue:
                    // Do nothing, existence prevents sprint, also causes healthrate
                    break;
                case EffectType.OverweightFatigue:
                    // Do nothing, really just there as a cause for other effects (energyrate)
                    break;
            }

            // Raise event
            OnEffectDeactivatedInvoke(this);
        }

        public static void RemoveEffect(Effect effect, List<Effect> listToRemoveFrom)
        {
            // Remove from specified list (could be an effect caused by another)
            if (listToRemoveFrom != null)
            {
                listToRemoveFrom.Remove(effect);
            }
            // Remove from global effect dict
            if(effectsByType.TryGetValue(effect.effectType, out List<Effect> otherEffects))
            {
                otherEffects.Remove(effect);
            }

            // Remove caused effects first
            for(int i=0; i < effect.caused.Count; ++i)
            {
                RemoveEffect(effect.caused[i], effect.caused);
            }

            // Deactivate this effect
            effect.Deactivate();

            // Raise event
            OnEffectRemovedInvoke(effect);
        }

        public static void RemoveEffects(EffectType effectType = EffectType.LightBleeding)
        {
            Mod.LogInfo("Remove effects called with type: " + effectType);
            if(effectsByType.TryGetValue(effectType, out List<Effect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; --i)
                {
                    RemoveEffect(effects[i], effects);
                }
                effectsByType.Remove(effectType);
            }
        }

        public static void OnEffectAddedInvoke(Effect effect)
        {
            if(OnEffectAdded != null)
            {
                OnEffectAdded(effect);
            }
        }

        public static void OnEffectActivatedInvoke(Effect effect)
        {
            if(OnEffectActivated != null)
            {
                OnEffectActivated(effect);
            }
        }

        public static void OnEffectDeactivatedInvoke(Effect effect)
        {
            if(OnEffectDeactivated != null)
            {
                OnEffectDeactivated(effect);
            }
        }

        public static void OnEffectRemovedInvoke(Effect effect)
        {
            if(OnEffectRemoved != null)
            {
                OnEffectRemoved(effect);
            }
        }
    }

    [System.Serializable]
    public class BuffEffect
    {
        public Effect.EffectType effectType;
        public float chance = 1;
        public float delay = 0;
        public float duration = 0;
        public float value = 0;
        public bool absolute = false;
        public int skillIndex = -1;
    }

    [System.Serializable]
    public class ConsumableEffect
    {
        public enum ConsumableEffectType
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
        public ConsumableEffectType effectType;

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
