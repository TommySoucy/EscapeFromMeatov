using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Effect
    {
        public static Dictionary<EffectType, List<Effect>> effectsByType = new Dictionary<EffectType, List<Effect>>();
        public static Dictionary<EffectType, float> inactiveTimersByType = new Dictionary<EffectType, float>();

        public static float damageModifier;
        public static bool overweightFatigue;

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
            Pain, // Causes tremors and tunnel vision
            StomachBloodloss, // Energy/Hydration rates multiplied by 5
            UnknownToxin, // Causes pain and negative health rate
            BodyTemperature, // Adds/Subtract from body temp
            Antidote, // Removes toxin with delay
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
        public delegate void OnInactiveTimerAddedDelegate(EffectType effectType, float time);
        public static event OnInactiveTimerAddedDelegate OnInactiveTimerAdded;
        public delegate void OnInactiveTimerRemovedDelegate(EffectType effectType);
        public static event OnInactiveTimerRemovedDelegate OnInactiveTimerRemoved;

        public Effect(BuffEffect buffEffect, bool fromStimulator = true, int partIndex = -1, bool nonLethal = false, bool hideoutOnly = false)
        {
            effectType = buffEffect.effectType;

            value = buffEffect.value;
            timer = buffEffect.duration;
            hasTimer = buffEffect.duration > 0;
            delay = buffEffect.delay;
            this.fromStimulator = fromStimulator;
            this.partIndex = partIndex;
            skillIndex = buffEffect.skillIndex;
            this.nonLethal = nonLethal;
            this.hideoutOnly = hideoutOnly;

            if(effectsByType.TryGetValue(effectType, out List<Effect> typeEffects))
            {
                typeEffects.Add(this);
            }
            else
            {
                effectsByType.Add(effectType, new List<Effect>() { this });
            }

            if(inactiveTimersByType.TryGetValue(effectType, out float typeInactiveTimer))
            {
                inactiveTimer = typeInactiveTimer;
            }

            if (hasTimer && !IsBuff())
            {
                timer = timer + timer / 100 * Bonus.debuffEndDelay;
            }

            if(inactiveTimer <= 0 && delay <= 0)
            {
                Activate();
            }

            OnEffectAddedInvoke(this);
        }

        public Effect(EffectType effectType, float value, float timer, float delay, Effect parentEffect = null, bool fromStimulator = true,
                      int partIndex = -1, int skillIndex = -1, bool nonLethal = false, bool hideoutOnly = false)
        {
            this.effectType = effectType;

            this.value = value;
            this.timer = timer;
            hasTimer = timer > 0;
            this.delay = delay;
            this.fromStimulator = fromStimulator;
            this.partIndex = partIndex;
            this.skillIndex = skillIndex;
            this.nonLethal = nonLethal;
            this.hideoutOnly = hideoutOnly;

            this.parentEffect = parentEffect;
            parentEffect.caused.Add(this);

            if (effectsByType.TryGetValue(effectType, out List<Effect> typeEffects))
            {
                typeEffects.Add(this);
            }
            else
            {
                effectsByType.Add(effectType, new List<Effect>() { this });
            }

            if(inactiveTimersByType.TryGetValue(effectType, out float typeInactiveTimer))
            {
                inactiveTimer = typeInactiveTimer;
            }

            if(inactiveTimer <= 0 && delay <= 0)
            {
                Activate();
            }

            OnEffectAddedInvoke(this);
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
                RemoveInactiveTimer(toRemove[i]);
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
                // Don't want to process timers if parent isn't active
                if(parentEffect == null || parentEffect.active)
                {
                    // Note that an effect can only become active once delay or timer have ran out
                    // If inactive but there is no timer, it will remain inactive forever
                    if (delay > 0)
                    {
                        delay -= Time.deltaTime;
                        if (delay <= 0 && inactiveTimer <= 0 && (parentEffect == null || parentEffect.active))
                        {
                            Activate();
                        }
                    }
                    else if (inactiveTimer > 0)
                    {
                        inactiveTimer -= Time.deltaTime;
                        if (delay <= 0 && inactiveTimer <= 0 && (parentEffect == null || parentEffect.active))
                        {
                            Activate();
                        }
                    }
                }
            }

            previousActive = active;
        }

        public static void AddInactiveTimer(EffectType effectType, float time)
        {
            inactiveTimersByType.Add(effectType, time);
            OnInactiveTimerAddedInvoke(effectType, time);
        }

        public static void RemoveInactiveTimer(EffectType effectType)
        {
            inactiveTimersByType.Remove(effectType);
            OnInactiveTimerRemovedInvoke(effectType);
        }

        public bool IsBuff()
        {
            return (effectType == EffectType.SkillRate && value > 0)
                || (effectType == EffectType.EnergyRate && value > 0)
                || (effectType == EffectType.MaxStamina && value > 0)
                || (effectType == EffectType.StaminaRate && value > 0)
                || (effectType == EffectType.HydrationRate && value > 0)
                || (effectType == EffectType.HealthRate && value > 0)
                || (effectType == EffectType.WeightLimit && value > 0)
                || (effectType == EffectType.DamageModifier && value < 0)
                || effectType == EffectType.RemoveAllBloodLosses
                || effectType == EffectType.Antidote;
        }

        public void Activate()
        {
            // Don't want to activate if parent isn't active,
            // Also don't want to activate if have delay or inactive timer
            if((parentEffect != null && !parentEffect.active) || delay > 0 || inactiveTimer > 0)
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
                        Mod.SetBasePositiveHealthRate(partIndex, Mod.GetBasePositiveHealthRate(partIndex) + value);
                    }
                    else
                    {
                        if (nonLethal)
                        {
                            Mod.SetBaseNonLethalHealthRate(partIndex, Mod.GetBaseNonLethalHealthRate(partIndex) + value);
                        }
                        else
                        {
                            Mod.SetBaseNegativeHealthRate(partIndex, Mod.GetBaseNegativeHealthRate(partIndex) + value);
                        }
                    }
                    break;
                case EffectType.RemoveAllBloodLosses:
                    if (inactiveTimersByType.ContainsKey(EffectType.LightBleeding))
                    {
                        inactiveTimersByType[EffectType.LightBleeding] = Mathf.Max(timer, inactiveTimersByType[EffectType.LightBleeding]);
                    }
                    else
                    {
                        AddInactiveTimer(EffectType.LightBleeding, timer);
                    }
                    if (effectsByType.TryGetValue(EffectType.LightBleeding, out List<Effect> lightBleedEffectList))
                    {
                        for (int i = lightBleedEffectList.Count - 1; i >= 0; --i)
                        {
                            lightBleedEffectList[i].Deactivate();
                            lightBleedEffectList[i].inactiveTimer = Mathf.Max(timer, lightBleedEffectList[i].inactiveTimer);
                        }
                    }
                    if (inactiveTimersByType.ContainsKey(EffectType.HeavyBleeding))
                    {
                        inactiveTimersByType[EffectType.HeavyBleeding] = Mathf.Max(timer, inactiveTimersByType[EffectType.HeavyBleeding]);
                    }
                    else
                    {
                        AddInactiveTimer(EffectType.HeavyBleeding, timer);
                    }
                    if (effectsByType.TryGetValue(EffectType.HeavyBleeding, out List<Effect> heavyBleedEffectList))
                    {
                        for (int i = heavyBleedEffectList.Count - 1; i >= 0; --i)
                        {
                            heavyBleedEffectList[i].Deactivate();
                            heavyBleedEffectList[i].inactiveTimer = Mathf.Max(timer, heavyBleedEffectList[i].inactiveTimer);
                        }
                    }
                    break;
                case EffectType.Contusion:
                    TODO2: // Start Contusion once implemented
                    Mod.LogWarning("Contusion not implemented yet, tried to start");
                    break;
                case EffectType.WeightLimit:
                    Mod.baseWeightLimit += (int)value;
                    break;
                case EffectType.DamageModifier:
                    TODO5: // Implement with damage
                    damageModifier += value;
                    break;
                case EffectType.Pain:
                    if(caused == null)
                    {
                        caused = new List<Effect>();
                        float delay = timer > 30 ? 30 : timer / 2;
                        new Effect(EffectType.HandsTremor, 0, timer - delay, delay, this, false);
                        if(partIndex == -1 || partIndex == 0)
                        {
                            new Effect(EffectType.QuantumTunnelling, 0, timer - delay, delay, this, false);
                        }
                    }
                    break;
                case EffectType.StomachBloodloss:
                    if(caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.EnergyRate, Mod.raidEnergyRate * 5, timer, 0, this, false);
                        new Effect(EffectType.HydrationRate, Mod.raidHydrationRate * 5, timer, 0, this, false);
                    }
                    break;
                case EffectType.UnknownToxin:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        float delay = timer > 5 ? 5 : timer / 2;
                        new Effect(EffectType.Pain, 0, timer - delay, delay, this, false, partIndex);
                        new Effect(EffectType.HealthRate, -2.33f, timer - delay, delay, this, false, partIndex);
                    }
                    break;
                case EffectType.BodyTemperature:
                    TODO6: // Apply body temp once implemented
                    Mod.LogWarning("BodyTemperature not implemented yet, tried to change");
                    break;
                case EffectType.Antidote:
                    // Antidote should have a delayed start so once we start, remove all toxins
                    RemoveEffects(EffectType.UnknownToxin);
                    break;
                case EffectType.LightBleeding:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        float healthRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["DamageHealth"];
                        float healthTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["HealthLoopTime"];
                        new Effect(EffectType.HealthRate, -healthRate / healthTime, 0, 0, this, false, partIndex, -1, true);
                        float energyRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["DamageEnergy"];
                        float energyTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["EnergyLoopTime"];
                        new Effect(EffectType.EnergyRate, -energyRate / energyTime, 0, 0, this, false, partIndex, -1, true);
                    }
                    break;
                case EffectType.HeavyBleeding:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        float healthRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["DamageHealth"];
                        float healthTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["HealthLoopTime"];
                        new Effect(EffectType.HealthRate, -healthRate / healthTime, 0, 0, this, false, partIndex, -1, true);
                        float energyRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["DamageEnergy"];
                        float energyTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["EnergyLoopTime"];
                        new Effect(EffectType.EnergyRate, -energyRate / energyTime, 0, 0, this, false, partIndex, -1, true);
                    }
                    break;
                case EffectType.Fracture:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.Pain, 0, 0, 0, this, false, partIndex);
                    }
                    break;
                case EffectType.Dehydration:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.Pain, 0, 0, 0, this, false, partIndex);
                        for(int i=0; i < Mod.GetHealthCount(); ++i)
                        {
                            new Effect(EffectType.HealthRate, -0.0666f, 0, 0, this, false, i);
                        }
                    }
                    break;
                case EffectType.HeavyDehydration:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.Pain, 0, 0, 0, this, false, partIndex);
                        for (int i = 0; i < Mod.GetHealthCount(); ++i)
                        {
                            new Effect(EffectType.HealthRate, -1.0666f, 0, 0, this, false, i);
                        }
                    }
                    break;
                case EffectType.Fatigue:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.Pain, 0, 0, 0, this, false, partIndex);
                    }
                    break;
                case EffectType.HeavyFatigue:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.Pain, 0, 0, 0, this, false, partIndex);
                        for (int i = 0; i < Mod.GetHealthCount(); ++i)
                        {
                            new Effect(EffectType.HealthRate, -0.0714f, 0, 0, this, false, i);
                        }
                    }
                    break;
                case EffectType.OverweightFatigue:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        new Effect(EffectType.EnergyRate, -0.033f, 0, 0, this, false);
                    }
                    overweightFatigue = true;
                    break;
                case EffectType.RadExposure:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        float healthRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["RadExposure"]["Damage"];
                        float healthTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["RadExposure"]["DamageLoopTime"];
                        new Effect(EffectType.HealthRate, -healthRate / healthTime, 0, 0, this, false);
                    }
                    break;
                case EffectType.Intoxication:
                    if (caused == null)
                    {
                        caused = new List<Effect>();
                        float healthRate = (float)Mod.globalDB["config"]["Health"]["Effects"]["Intoxication"]["DamageHealth"];
                        float healthTime = (float)Mod.globalDB["config"]["Health"]["Effects"]["Intoxication"]["HealthLoopTime"];
                        new Effect(EffectType.HealthRate, -healthRate / healthTime, 0, 0, this, false);
                    }
                    break;
                case EffectType.DestroyedPart:
                    // Do nothing on activation
                    // Existence has effects on other systems
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
                    overweightFatigue = false;
                    break;
                case EffectType.RadExposure:
                    // Do nothing, really just there as a cause for other effects (Healthrate)
                    break;
                case EffectType.Intoxication:
                    // Do nothing, really just there as a cause for other effects (Healthrate)
                    break;
                case EffectType.DestroyedPart:
                    // Do nothing, Existence has effects on other systems
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

        public static void RemoveEffects(EffectType effectType)
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

        public static void RemoveAllEffects()
        {
            List<EffectType> toRemove = new List<EffectType>();
            foreach(KeyValuePair<EffectType, List<Effect>> entry in effectsByType)
            {
                toRemove.Add(entry.Key);
            }
            for(int i=0; i < toRemove.Count; ++i)
            {
                effectsByType.Remove(toRemove[i]);
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

        public static void OnInactiveTimerAddedInvoke(EffectType effectType, float time)
        {
            if(OnInactiveTimerAdded != null)
            {
                OnInactiveTimerAdded(effectType, time);
            }
        }

        public static void OnInactiveTimerRemovedInvoke(EffectType effectType)
        {
            if(OnInactiveTimerRemoved != null)
            {
                OnInactiveTimerRemoved(effectType);
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
