﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_Effect
    {
        public static List<EFM_Effect> effects = new List<EFM_Effect>(); // Active effects

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
        public bool hideoutOnly = false;

        public static void RemoveEffectAt(int index)
        {
            Mod.instance.LogInfo("Effect remove at "+index+" called");
            EFM_Effect effectToRemove = effects[index];
            effects.RemoveAt(index);
            switch (effectToRemove.effectType)
            {
                case EffectType.LightBleeding:
                    Mod.instance.LogInfo("\t\tRemoving");
                    foreach (EFM_Effect causedEffect in effectToRemove.caused)
                    {
                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                        {
                            if (causedEffect.partIndex == -1)
                            {
                                if (causedEffect.nonLethal)
                                {
                                    for (int k = 0; k < 7; ++k)
                                    {
                                        Mod.currentNonLethalHealthRates[k] -= causedEffect.value;
                                    }
                                }
                                else
                                {
                                    for (int k = 0; k < 7; ++k)
                                    {
                                        Mod.currentHealthRates[k] -= causedEffect.value;
                                    }
                                }
                            }
                            else
                            {
                                if (causedEffect.nonLethal)
                                {
                                    Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                }
                                else
                                {
                                    Mod.currentHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                }
                            }
                        }
                        else // Energy rate
                        {
                            Mod.currentEnergyRate -= causedEffect.value;
                        }
                        EFM_Effect.effects.Remove(causedEffect);
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EffectType.HeavyBleeding:
                    Mod.instance.LogInfo("\t\tRemoving");
                    foreach (EFM_Effect causedEffect in effectToRemove.caused)
                    {
                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                        {
                            if (causedEffect.partIndex == -1)
                            {
                                if (causedEffect.nonLethal)
                                {
                                    for (int k = 0; k < 7; ++k)
                                    {
                                        Mod.currentNonLethalHealthRates[k] -= causedEffect.value;
                                    }
                                }
                                else
                                {
                                    for (int k = 0; k < 7; ++k)
                                    {
                                        Mod.currentHealthRates[k] -= causedEffect.value;
                                    }
                                }
                            }
                            else
                            {
                                if (causedEffect.nonLethal)
                                {
                                    Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                }
                                else
                                {
                                    Mod.currentHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                }
                            }
                        }
                        else // Energy rate
                        {
                            Mod.currentEnergyRate -= causedEffect.value;
                        }
                        EFM_Effect.effects.Remove(causedEffect);
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EffectType.Fracture:
                    Mod.instance.LogInfo("\t\tRemoving");
                    // Remove all effects caused by this fracture
                    foreach (EFM_Effect causedEffect in effectToRemove.caused)
                    {
                        // Could go two layers deep
                        foreach (EFM_Effect causedCausedEffect in effectToRemove.caused)
                        {
                            EFM_Effect.effects.Remove(causedCausedEffect);
                        }
                        EFM_Effect.effects.Remove(causedEffect);
                    }
                    bool hasFractureTremors = false;
                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                    {
                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                        {
                            hasFractureTremors = true;
                            break;
                        }
                    }
                    if (!hasFractureTremors)
                    {
                        // TODO: Disable tremors
                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                        {
                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                        }
                    }

                    if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EffectType.DestroyedPart:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.SkillRate:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.skills[effectToRemove.skillIndex].currentProgress -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.EnergyRate:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentEnergyRate -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.HydrationRate:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentHydrationRate -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.MaxStamina:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentMaxStamina -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.StaminaRate:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentStaminaEffect -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.HandsTremor:
                    Mod.instance.LogInfo("\t\tRemoving");
                    // TODO: Stop tremors if there are not other tremor effects
                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.QuantumTunnelling:
                    Mod.instance.LogInfo("\t\tRemoving");
                    // TODO: Stop QuantumTunnelling
                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.HealthRate:
                    Mod.instance.LogInfo("\t\tRemoving");
                    float[] arrayToUse = effectToRemove.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
                    if (effectToRemove.partIndex == -1)
                    {
                        for (int i = 0; i < 7; ++i)
                        {
                            arrayToUse[i] -= effectToRemove.value;
                        }
                    }
                    else
                    {
                        arrayToUse[effectToRemove.partIndex] -= effectToRemove.value;
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.RemoveAllBloodLosses:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.instance.LogInfo("\t\tRemoved");
                    // Reactivate all bleeding 
                    // Not necessary because when we disabled them we used the disable timer
                    break;
                case EFM_Effect.EffectType.Contusion:
                    Mod.instance.LogInfo("\t\tRemoving");
                    bool otherContusions = false;
                    foreach (EFM_Effect contusionEffectCheck in EFM_Effect.effects)
                    {
                        if (contusionEffectCheck.active && contusionEffectCheck.effectType == EFM_Effect.EffectType.Contusion)
                        {
                            otherContusions = true;
                            break;
                        }
                    }
                    if (!otherContusions)
                    {
                        // Enable haptic feedback
                        GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Enabled;
                        // TODO: also set volume to full
                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
                        {
                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
                        }
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.WeightLimit:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentWeightLimit -= (int)(effectToRemove.value * 1000);
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.DamageModifier:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.currentDamageModifier -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.Pain:
                    Mod.instance.LogInfo("\t\tRemoving");
                    // Remove all tremors caused by this pain and disable tremors if no other tremors active
                    foreach (EFM_Effect causedEffect in effectToRemove.caused)
                    {
                        EFM_Effect.effects.Remove(causedEffect);
                    }
                    bool hasPainTremors = false;
                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                    {
                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                        {
                            hasPainTremors = true;
                            break;
                        }
                    }
                    if (!hasPainTremors)
                    {
                        // TODO: Disable tremors
                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                        {
                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                        }
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effectToRemove.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.StomachBloodloss:
                    Mod.instance.LogInfo("\t\tRemoving");
                    --Mod.stomachBloodLossCount;
                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.UnknownToxin:
                    Mod.instance.LogInfo("\t\tRemoving");
                    // Remove all effects caused by this toxin
                    foreach (EFM_Effect causedEffect in effectToRemove.caused)
                    {
                        float[] currentArrayToUse = causedEffect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                        {
                            if (causedEffect.partIndex == -1)
                            {
                                for (int i = 0; i < 7; ++i)
                                {
                                    currentArrayToUse[i] -= causedEffect.value;
                                }
                            }
                            else
                            {
                                currentArrayToUse[causedEffect.partIndex] -= causedEffect.value;
                            }
                        }
                        // Could go two layers deep
                        foreach (EFM_Effect causedCausedEffect in effectToRemove.caused)
                        {
                            EFM_Effect.effects.Remove(causedCausedEffect);
                        }
                        EFM_Effect.effects.Remove(causedEffect);
                    }
                    bool hasToxinTremors = false;
                    foreach (EFM_Effect effectCheck in EFM_Effect.effects)
                    {
                        if (effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
                        {
                            hasToxinTremors = true;
                            break;
                        }
                    }
                    if (!hasToxinTremors)
                    {
                        // TODO: Disable tremors
                        if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                        {
                            Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                        }
                    }

                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
                    {
                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
                    }
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.BodyTemperature:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.temperatureOffset -= effectToRemove.value;
                    Mod.instance.LogInfo("\t\tRemoved");
                    break;
                case EFM_Effect.EffectType.Antidote:
                    Mod.instance.LogInfo("\t\tRemoving");
                    Mod.instance.LogInfo("\t\tRemoved");
                    // Will remove toxin on activation, does nothing after
                    break;
            }
        }

        public static void RemoveEffects(bool all = true, EffectType effectType = EffectType.LightBleeding, int partIndex = -1)
        {
            Mod.instance.LogInfo("Remove effects called with all: " + all + ", type: " + effectType + ", partindex: " + partIndex);
            for (int j = EFM_Effect.effects.Count - 1; j >= 0;)
            {
                if (all || ((partIndex == -1 || EFM_Effect.effects[j].partIndex == partIndex) && EFM_Effect.effects[j].effectType == effectType) && !EFM_Effect.effects[j].hideoutOnly)
                {
                    Mod.instance.LogInfo("\tFound matching effect: "+effectType+" on "+partIndex+" at effect index "+j+" while effects has "+ EFM_Effect.effects.Count+" elements");
                    RemoveEffectAt(j);
                    --j;
                    j = Mathf.Min(j, effects.Count - 1);
                }
            }
        }
    }

    [System.Serializable]
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

    [System.Serializable]
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
