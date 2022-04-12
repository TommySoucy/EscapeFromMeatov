using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    class EFM_Raid_Manager : EFM_Manager
    {
        public static readonly float extractionTime = 10; 

        public static EFM_Raid_Manager currentManager;
        public static float extractionTimer;
        public static bool inRaid;

        private List<Extraction> extractions;
        private List<Extraction> possibleExtractions;
        public ExtractionManager currentExtraction;
        private bool inExtractionLastFrame;
        public Transform spawnPoint;
        public Light sun;
        public float time;

        private float[] maxHealth = { 35, 85, 70, 60, 60, 65, 65 };
        private float[] currentHealthRates;
        private float[] currentNonLethalHealthRates;
        private float currentEnergyRate = 0;
        private float currentHydrationRate = 0;

        private List<GameObject> extractionCards;

        private void Update()
        {
            if(currentExtraction != null && currentExtraction.active)
            {
                string missingRequirement = currentExtraction.extraction.RequirementsMet();
                if (missingRequirement.Equals("none") || missingRequirement.Equals(""))
                {
                    // Make sure the requirement text is disabled if 
                    if(missingRequirement.Equals("none"))
                    {
                        currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
                    }

                    inExtractionLastFrame = true;

                    extractionTimer += UnityEngine.Time.deltaTime;

                    if (!Mod.extractionUI.activeSelf)
                    {
                        Mod.extractionUI.SetActive(true);
                    }
                    Mod.extractionUI.transform.localPosition = GM.CurrentPlayerBody.Head.localPosition;
                    Mod.extractionUI.transform.GetChild(0).localPosition = Vector3.forward;
                    Mod.extractionUIText.text = "Extraction in " + Mathf.Min(0, extractionTime - extractionTimer);

                    if (extractionTimer >= extractionTime)
                    {
                        Mod.justFinishedRaid = true;
                        Mod.raidState = EFM_Base_Manager.FinishRaidState.Survived; // TODO: Will have to call with runthrough if exp is less than threshold
                        //TODO: Give experience depending on raid state

                        EFM_Manager.LoadBase(5); // Load autosave, which is right before the start of raid
                    }
                }
                else
                {
                    // Update extraction card requirement text with requirement of this extract
                    currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).gameObject.SetActive(true);
                    Text requirementText = currentExtraction.extraction.card.transform.GetChild(0).GetChild(1).GetComponent<Text>();
                    requirementText.color = Color.red;
                    requirementText.text = missingRequirement;
                }
            }
            else if (inExtractionLastFrame)
            {
                extractionTimer = 0;
                inExtractionLastFrame = false;

                Mod.extractionUI.SetActive(false);
            }

            UpdateEffects();

            UpdateSun();
        }

        private void UpdateEffects()
        {
            // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
            for (int i = EFM_Effect.effects.Count; i >= 0; --i)
            {
                if (EFM_Effect.effects.Count == 0)
                {
                    break;
                }
                else if (i >= EFM_Effect.effects.Count)
                {
                    continue;
                }

                EFM_Effect effect = EFM_Effect.effects[i];
                if (effect.active)
                {
                    if (effect.hasTimer)
                    {
                        effect.timer -= Time.deltaTime;
                        if (effect.timer <= 0)
                        {
                            effect.active = false;

                            // Unapply effect
                            switch (effect.effectType)
                            {
                                case EFM_Effect.EffectType.SkillRate:
                                    Mod.skills[effect.skillIndex].currentProgress -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.EnergyRate:
                                    currentEnergyRate -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.HydrationRate:
                                    currentHydrationRate -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.MaxStamina:
                                    Mod.currentMaxStamina -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.StaminaRate:
                                    Mod.currentStaminaEffect -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.HandsTremor:
                                    // TODO: Stop tremors if there are no other tremor effects
                                    break;
                                case EFM_Effect.EffectType.QuantumTunnelling:
                                    // TODO: Stop QuantumTunnelling if there are no other tunnelling effects
                                    break;
                                case EFM_Effect.EffectType.HealthRate:
                                    float[] arrayToUse = effect.nonLethal ? currentNonLethalHealthRates : currentHealthRates;
                                    if (effect.partIndex == -1)
                                    {
                                        for (int j = 0; j < 7; ++j)
                                        {
                                            arrayToUse[j] -= effect.value / 7;
                                        }
                                    }
                                    else
                                    {
                                        arrayToUse[effect.partIndex] -= effect.value;
                                    }
                                    break;
                                case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                    // Reactivate all bleeding 
                                    // Not necessary because when we disabled them we used the disable timer
                                    break;
                                case EFM_Effect.EffectType.Contusion:
                                    bool otherContusions = false;
                                    foreach(EFM_Effect contusionEffectCheck in EFM_Effect.effects)
                                    {
                                        if(contusionEffectCheck.active && contusionEffectCheck.effectType == EFM_Effect.EffectType.Contusion)
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
                                    }
                                    break;
                                case EFM_Effect.EffectType.WeightLimit:
                                    Mod.currentWeightLimit -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.DamageModifier:
                                    Mod.currentDamageModifier -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.Pain:
                                    // Remove all tremors caused by this pain and disable tremors if no other tremors active
                                    foreach (EFM_Effect causedEffect in effect.caused)
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
                                    }
                                    break;
                                case EFM_Effect.EffectType.StomachBloodloss:
                                    --Mod.stomachBloodLossCount;
                                    break;
                                case EFM_Effect.EffectType.UnknownToxin:
                                    // Remove all effects caused by this toxin
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                                        {
                                            for (int j = 0; j < 7; ++j)
                                            {
                                                currentHealthRates[j] -= causedEffect.value / 7;
                                            }
                                        }
                                        // Could go two layers deep
                                        foreach (EFM_Effect causedCausedEffect in effect.caused)
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
                                    }
                                    break;
                                case EFM_Effect.EffectType.BodyTemperature:
                                    Mod.temperatureOffset -= effect.value;
                                    break;
                                case EFM_Effect.EffectType.Antidote:
                                    // Will remove toxin on activation, does nothing after
                                    break;
                                case EFM_Effect.EffectType.LightBleeding:
                                case EFM_Effect.EffectType.HeavyBleeding:
                                    // Remove all effects caused by this bleeding
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                                        {
                                            currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                        }
                                        else // Energy rate
                                        {
                                            currentEnergyRate -= causedEffect.value;
                                        }
                                        EFM_Effect.effects.Remove(causedEffect);
                                    }
                                    break;
                                case EFM_Effect.EffectType.Fracture:
                                    // Remove all effects caused by this fracture
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        // Could go two layers deep
                                        foreach (EFM_Effect causedCausedEffect in effect.caused)
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
                                    }
                                    break;
                            }

                            EFM_Effect.effects.RemoveAt(i);

                            continue;
                        }
                    }
                }
                else
                {
                    bool effectJustActivated = false;
                    if (effect.delay > 0)
                    {
                        effect.delay -= Time.deltaTime;
                    }
                    else if (effect.inactiveTimer <= 0)
                    {
                        effect.active = true;
                        effectJustActivated = true;
                    }
                    if (effect.inactiveTimer > 0)
                    {
                        effect.inactiveTimer -= Time.deltaTime;
                    }
                    else if (effect.delay <= 0)
                    {
                        effect.active = true;
                        effectJustActivated = true;
                    }

                    // Apply effect if it just started being active
                    if (effectJustActivated)
                    {
                        switch (effect.effectType)
                        {
                            case EFM_Effect.EffectType.SkillRate:
                                Mod.skills[effect.skillIndex].currentProgress += effect.value;
                                break;
                            case EFM_Effect.EffectType.EnergyRate:
                                currentEnergyRate += effect.value;
                                break;
                            case EFM_Effect.EffectType.HydrationRate:
                                currentHydrationRate += effect.value;
                                break;
                            case EFM_Effect.EffectType.MaxStamina:
                                Mod.currentMaxStamina += effect.value;
                                break;
                            case EFM_Effect.EffectType.StaminaRate:
                                Mod.currentStaminaEffect += effect.value;
                                break;
                            case EFM_Effect.EffectType.HandsTremor:
                                // TODO: Begin tremors if there isnt already another active one
                                break;
                            case EFM_Effect.EffectType.QuantumTunnelling:
                                // TODO: Begin quantumtunneling if there isnt already another active one
                                break;
                            case EFM_Effect.EffectType.HealthRate:
                                float[] arrayToUse = effect.nonLethal ? currentNonLethalHealthRates : currentHealthRates;
                                if (effect.partIndex == -1)
                                {
                                    for (int j = 0; j < 7; ++j)
                                    {
                                        arrayToUse[j] += effect.value / 7;
                                    }
                                }
                                else
                                {
                                    arrayToUse[effect.partIndex] += effect.value;
                                }
                                break;
                            case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                // Deactivate all bleeding using disable timer
                                foreach (EFM_Effect bleedEffect in EFM_Effect.effects)
                                {
                                    if (bleedEffect.effectType == EFM_Effect.EffectType.LightBleeding || bleedEffect.effectType == EFM_Effect.EffectType.HeavyBleeding)
                                    {
                                        bleedEffect.active = false;
                                        bleedEffect.inactiveTimer = effect.timer;

                                        // Unapply the healthrate caused by this bleed
                                        EFM_Effect causedHealthRate = bleedEffect.caused[0];
                                        if (causedHealthRate.nonLethal)
                                        {
                                            currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        else
                                        {
                                            currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        EFM_Effect causedEnergyRate = bleedEffect.caused[1];
                                        currentEnergyRate -= causedEnergyRate.value;
                                        bleedEffect.caused.Clear();
                                        EFM_Effect.effects.Remove(causedHealthRate);
                                        EFM_Effect.effects.Remove(causedEnergyRate);
                                    }
                                }
                                break;
                            case EFM_Effect.EffectType.Contusion:
                                // Disable haptic feedback
                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
                                // TODO: also set volume to 0.33 * volume
                                break;
                            case EFM_Effect.EffectType.WeightLimit:
                                Mod.currentWeightLimit += effect.value;
                                break;
                            case EFM_Effect.EffectType.DamageModifier:
                                Mod.currentDamageModifier += effect.value;
                                break;
                            case EFM_Effect.EffectType.Pain:
                                // Add a tremor effect
                                EFM_Effect newTremor = new EFM_Effect();
                                newTremor.effectType = EFM_Effect.EffectType.HandsTremor;
                                newTremor.delay = 5;
                                newTremor.hasTimer = effect.hasTimer;
                                newTremor.timer = effect.timer;
                                EFM_Effect.effects.Add(newTremor);
                                effect.caused.Add(newTremor);
                                break;
                            case EFM_Effect.EffectType.StomachBloodloss:
                                ++Mod.stomachBloodLossCount;
                                break;
                            case EFM_Effect.EffectType.UnknownToxin:
                                // Add a pain effect
                                EFM_Effect newToxinPain = new EFM_Effect();
                                newToxinPain.effectType = EFM_Effect.EffectType.Pain;
                                newToxinPain.delay = 5;
                                newToxinPain.hasTimer = effect.hasTimer;
                                newToxinPain.timer = effect.timer;
                                newToxinPain.partIndex = 0;
                                EFM_Effect.effects.Add(newToxinPain);
                                effect.caused.Add(newToxinPain);
                                // Add a health rate effect
                                EFM_Effect newToxinHealthRate = new EFM_Effect();
                                newToxinHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newToxinHealthRate.delay = 5;
                                newToxinHealthRate.value = -25;
                                newToxinHealthRate.hasTimer = effect.hasTimer;
                                newToxinHealthRate.timer = effect.timer;
                                EFM_Effect.effects.Add(newToxinHealthRate);
                                effect.caused.Add(newToxinHealthRate);
                                break;
                            case EFM_Effect.EffectType.BodyTemperature:
                                Mod.temperatureOffset += effect.value;
                                break;
                            case EFM_Effect.EffectType.Antidote:
                                // Will remove toxin on ativation, does nothing after
                                for (int j = EFM_Effect.effects.Count; j >= 0; --j)
                                {
                                    if (EFM_Effect.effects[j].effectType == EFM_Effect.EffectType.UnknownToxin)
                                    {
                                        EFM_Effect.effects.RemoveAt(j);
                                        break;
                                    }
                                }
                                break;
                            case EFM_Effect.EffectType.LightBleeding:
                                // Add a health rate effect
                                EFM_Effect newLightBleedingHealthRate = new EFM_Effect();
                                newLightBleedingHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newLightBleedingHealthRate.delay = 5;
                                newLightBleedingHealthRate.value = -8;
                                newLightBleedingHealthRate.hasTimer = effect.hasTimer;
                                newLightBleedingHealthRate.timer = effect.timer;
                                newLightBleedingHealthRate.partIndex = effect.partIndex;
                                newLightBleedingHealthRate.nonLethal = true;
                                EFM_Effect.effects.Add(newLightBleedingHealthRate);
                                effect.caused.Add(newLightBleedingHealthRate);
                                // Add a energy rate effect
                                EFM_Effect newLightBleedingEnergyRate = new EFM_Effect();
                                newLightBleedingEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newLightBleedingEnergyRate.delay = 5;
                                newLightBleedingEnergyRate.value = -5;
                                newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newLightBleedingEnergyRate.timer = effect.timer;
                                newLightBleedingEnergyRate.partIndex = effect.partIndex;
                                EFM_Effect.effects.Add(newLightBleedingEnergyRate);
                                effect.caused.Add(newLightBleedingEnergyRate);
                                break;
                            case EFM_Effect.EffectType.HeavyBleeding:
                                // Add a health rate effect
                                EFM_Effect newHeavyBleedingHealthRate = new EFM_Effect();
                                newHeavyBleedingHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyBleedingHealthRate.delay = 5;
                                newHeavyBleedingHealthRate.value = -13.5f;
                                newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingHealthRate.timer = effect.timer;
                                newHeavyBleedingHealthRate.nonLethal = true;
                                EFM_Effect.effects.Add(newHeavyBleedingHealthRate);
                                effect.caused.Add(newHeavyBleedingHealthRate);
                                // Add a energy rate effect
                                EFM_Effect newHeavyBleedingEnergyRate = new EFM_Effect();
                                newHeavyBleedingEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newHeavyBleedingEnergyRate.delay = 5;
                                newHeavyBleedingEnergyRate.value = -6;
                                newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingEnergyRate.timer = effect.timer;
                                newHeavyBleedingEnergyRate.partIndex = effect.partIndex;
                                EFM_Effect.effects.Add(newHeavyBleedingEnergyRate);
                                effect.caused.Add(newHeavyBleedingEnergyRate);
                                break;
                            case EFM_Effect.EffectType.Fracture:
                                // Add a pain effect
                                EFM_Effect newFracturePain = new EFM_Effect();
                                newFracturePain.effectType = EFM_Effect.EffectType.Pain;
                                newFracturePain.delay = 5;
                                newFracturePain.hasTimer = effect.hasTimer;
                                newFracturePain.timer = effect.timer;
                                EFM_Effect.effects.Add(newFracturePain);
                                effect.caused.Add(newFracturePain);
                                break;
                            case EFM_Effect.EffectType.Dehydration:
                                // Add a HealthRate effect
                                EFM_Effect newDehydrationHealthRate = new EFM_Effect();
                                newDehydrationHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newDehydrationHealthRate.value = -60;
                                newDehydrationHealthRate.delay = 5;
                                newDehydrationHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newDehydrationHealthRate);
                                effect.caused.Add(newDehydrationHealthRate);
                                break;
                            case EFM_Effect.EffectType.HeavyDehydration:
                                // Add a HealthRate effect
                                EFM_Effect newHeavyDehydrationHealthRate = new EFM_Effect();
                                newHeavyDehydrationHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyDehydrationHealthRate.value = -350;
                                newHeavyDehydrationHealthRate.delay = 5;
                                newHeavyDehydrationHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newHeavyDehydrationHealthRate);
                                effect.caused.Add(newHeavyDehydrationHealthRate);
                                break;
                            case EFM_Effect.EffectType.Fatigue:
                                Mod.fatigue = true;
                                break;
                            case EFM_Effect.EffectType.HeavyFatigue:
                                // Add a HealthRate effect
                                EFM_Effect newHeavyFatigueHealthRate = new EFM_Effect();
                                newHeavyFatigueHealthRate.effectType = EFM_Effect.EffectType.HealthRate;
                                newHeavyFatigueHealthRate.value = -30;
                                newHeavyFatigueHealthRate.delay = 5;
                                newHeavyFatigueHealthRate.hasTimer = false;
                                EFM_Effect.effects.Add(newHeavyFatigueHealthRate);
                                effect.caused.Add(newHeavyFatigueHealthRate);
                                break;
                            case EFM_Effect.EffectType.OverweightFatigue:
                                // Add a EnergyRate effect
                                EFM_Effect newOverweightFatigueEnergyRate = new EFM_Effect();
                                newOverweightFatigueEnergyRate.effectType = EFM_Effect.EffectType.EnergyRate;
                                newOverweightFatigueEnergyRate.value = -4;
                                newOverweightFatigueEnergyRate.delay = 5;
                                newOverweightFatigueEnergyRate.hasTimer = false;
                                EFM_Effect.effects.Add(newOverweightFatigueEnergyRate);
                                effect.caused.Add(newOverweightFatigueEnergyRate);
                                break;
                        }
                    }
                }
            }

            // Apply lethal health rates
            for (int i = 0; i < 7; ++i)
            {
                if(Mod.health[i] == 0)
                {
                    // Apply currentHealthRates[i] to other parts
                    for (int j = 0; j < 7; ++j)
                    {
                        if (j != i)
                        {
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + currentHealthRates[i] * (Time.deltaTime / 60) / 6, 1, maxHealth[j]);
                        }
                    }
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthRates[i] * (Time.deltaTime / 60), 1, maxHealth[i]);

                    if (Mod.health[i] == 0 && (i == 0 || i == 1))
                    {
                        // TODO: Kill player
                    }
                }
            }

            // Apply nonlethal health rates
            for (int i = 0; i < 7; ++i)
            {
                if(Mod.health[i] == 0)
                {
                    // Apply currentNonLethalHealthRates[i] to other parts
                    for (int j = 0; j < 7; ++j)
                    {
                        if (j != i)
                        {
                            Mod.health[j] = Mathf.Clamp(Mod.health[j] + currentNonLethalHealthRates[i] * (Time.deltaTime / 60) / 6, 1, maxHealth[j]);
                        }
                    }
                }
                else
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentNonLethalHealthRates[i] * (Time.deltaTime / 60), 1, maxHealth[i]);
                }
            }

            Mod.hydration = Mathf.Clamp(Mod.hydration + currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);
            if(Mod.hydration == 0)
            {
                if (Mod.dehydrationEffect == null)
                {
                    // Add a heavyDehydration effect
                    EFM_Effect newHeavyDehydration = new EFM_Effect();
                    newHeavyDehydration.effectType = EFM_Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.delay = 5;
                    newHeavyDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == EFM_Effect.EffectType.Dehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if(Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyDehydration effect
                    EFM_Effect newHeavyDehydration = new EFM_Effect();
                    newHeavyDehydration.effectType = EFM_Effect.EffectType.HeavyDehydration;
                    newHeavyDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyDehydration);
                    Mod.dehydrationEffect = newHeavyDehydration;
                }
            }
            else if(Mod.hydration < 20)
            {
                if (Mod.dehydrationEffect == null)
                {
                    // Add a dehydration effect
                    EFM_Effect newDehydration = new EFM_Effect();
                    newDehydration.effectType = EFM_Effect.EffectType.Dehydration;
                    newDehydration.delay = 5;
                    newDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newDehydration);
                    Mod.dehydrationEffect = newDehydration;
                }
                else if(Mod.dehydrationEffect.effectType == EFM_Effect.EffectType.HeavyDehydration)
                {
                    // Disable the other dehydration before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a dehydration effect
                    EFM_Effect newDehydration = new EFM_Effect();
                    newDehydration.effectType = EFM_Effect.EffectType.Dehydration;
                    newDehydration.hasTimer = false;
                    EFM_Effect.effects.Add(newDehydration);
                    Mod.dehydrationEffect = newDehydration;
                }
            }
            else // Hydrated
            {
                // Remove any dehydration effect
                if(Mod.dehydrationEffect != null)
                {
                    // Disable 
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);
                }
            }

            Mod.energy = Mathf.Clamp(Mod.energy + currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);
            if (Mod.energy == 0)
            {
                if (Mod.fatigueEffect == null)
                {
                    // Add a heavyFatigue effect
                    EFM_Effect newHeavyFatigue = new EFM_Effect();
                    newHeavyFatigue.effectType = EFM_Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.delay = 5;
                    newHeavyFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyFatigue);
                    Mod.fatigueEffect = newHeavyFatigue;
                }
                else if (Mod.fatigueEffect.effectType == EFM_Effect.EffectType.Fatigue)
                {
                    // Disable the other fatigue before adding a new one
                    EFM_Effect.effects.Remove(Mod.dehydrationEffect);

                    // Add a heavyFatigue effect
                    EFM_Effect newHeavyFatigue = new EFM_Effect();
                    newHeavyFatigue.effectType = EFM_Effect.EffectType.HeavyFatigue;
                    newHeavyFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newHeavyFatigue);
                    Mod.dehydrationEffect = newHeavyFatigue;
                }
            }
            else if (Mod.energy < 20)
            {
                if (Mod.fatigueEffect == null)
                {
                    // Add a fatigue effect
                    EFM_Effect newFatigue = new EFM_Effect();
                    newFatigue.effectType = EFM_Effect.EffectType.Fatigue;
                    newFatigue.delay = 5;
                    newFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newFatigue);
                    Mod.fatigueEffect = newFatigue;
                }
                else if (Mod.fatigueEffect.effectType == EFM_Effect.EffectType.HeavyFatigue)
                {
                    // Disable the other fatigue before adding a new one
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.fatigueEffect);

                    // Add a fatigue effect
                    EFM_Effect newFatigue = new EFM_Effect();
                    newFatigue.effectType = EFM_Effect.EffectType.Fatigue;
                    newFatigue.hasTimer = false;
                    EFM_Effect.effects.Add(newFatigue);
                    Mod.dehydrationEffect = newFatigue;
                }
            }
            else // Energized
            {
                // Remove any fatigue effect
                if (Mod.fatigueEffect != null)
                {
                    // Disable 
                    if (Mod.fatigueEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        EFM_Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    EFM_Effect.effects.Remove(Mod.fatigueEffect);
                }
            }
        }

        private void FixedUpdate()
        {
            //if (disabledLimits)
            //{
            //    foreach (GameObject go in disabledDoors)
            //    {
            //        go.SetActive(true);
            //    }
            //}
            //if (!disabledLimits && doneJointLimitFrame)
            //{
            //    if (hingeJointsToDisableLimits != null && hingeJointsToDisableLimits.Count > 0)
            //    {
            //        for(int i =0; i<hingeJointsToDisableLimits.Count; ++i)
            //        {
            //            hingeJointsToDisableLimits[i].limits = correspondingLimits[i];
            //        }
            //    }
            //    disabledLimits = true;
            //}
            //doneJointLimitFrame = true;
        }

        public void UpdateSun()
        {
            // Considering that 21600 (0600) is sunrise and that 64800 (1800) is sunset
            UpdateSunAngle();

            UpdateSunIntensity();
        }

        public void UpdateSunAngle()
        {
            // Sun's X axis must rotate by 180 degrees in 43200 seconds
            // so 0.004166 degree in 1 second with an offset of 21600
            sun.transform.rotation = Quaternion.Euler(0.004166f * time + 21600, sun.transform.rotation.eulerAngles.y, sun.transform.rotation.eulerAngles.z);
        }

        public void UpdateSunIntensity()
        {
            if(time >= 23400 && time <= 63000) // Day
            {
                sun.intensity = 1;
            }
            else if(time > 63000 && time < 64800) // Setting
            {
                sun.intensity = (64800 - time) / 1800;
            }
            else if((time > 64800 && time <= 86400) || (time >= 0 && time <= 21600)) // Night
            {
                sun.intensity = 0;
            }
            else //(time > 21600 && time < 23400) // Rising
            {
                sun.intensity = 1800 / (time - 21600);
            }
        }

        public override void Init()
        {
            // Init player state
            currentHealthRates = new float[7];
            currentNonLethalHealthRates = new float[7];

            // Init effects that were already active before the raid to make sure their effects are applied
            InitEffects();

            Mod.instance.LogInfo("Raid init called");
            currentManager = this;

            LoadMapData();
            Mod.instance.LogInfo("Map data read");

            // Choose spawnpoints
            Transform spawnRoot = transform.GetChild(transform.childCount - 1).GetChild(Mod.chosenCharIndex);
            spawnPoint = spawnRoot.GetChild(UnityEngine.Random.Range(0, spawnRoot.childCount));

            Mod.instance.LogInfo("Got spawn");

            // Find extractions
            possibleExtractions = new List<Extraction>();
            Extraction bestCandidate = null; // Must be an extraction without appearance times (always available) and no other requirements. This will be the minimum extraction
            float farthestDistance = float.MinValue;
            Mod.instance.LogInfo("Init raid with map index: "+Mod.chosenMapIndex+", which has "+extractions.Count+" extractions");
            for(int extractionIndex = 0; extractionIndex < extractions.Count; ++extractionIndex)
            {
                Extraction currentExtraction = extractions[extractionIndex];
                currentExtraction.gameObject = gameObject.transform.GetChild(gameObject.transform.childCount - 2).GetChild(extractionIndex).gameObject;

                bool canBeMinimum = (currentExtraction.times == null || currentExtraction.times.Count == 0) &&
                                    (currentExtraction.itemRequirements == null || currentExtraction.itemRequirements.Count == 0) && 
                                    (currentExtraction.equipmentRequirements == null || currentExtraction.equipmentRequirements.Count == 0) &&
                                    (currentExtraction.role == Mod.chosenCharIndex || currentExtraction.role == 2) &&
                                    !currentExtraction.accessRequirement;
                float currentDistance = Vector3.Distance(spawnPoint.position, currentExtraction.gameObject.transform.position);
                Mod.instance.LogInfo("\tExtraction at index: "+extractionIndex+" has "+ currentExtraction.times.Count + " times and "+ currentExtraction.itemRequirements.Count+" items reqs. Its current distance from player is: "+ currentDistance);
                if (UnityEngine.Random.value <= ExtractionChanceByDist(currentDistance))
                {
                    Mod.instance.LogInfo("\t\tAdding this extraction to list possible extractions");
                    possibleExtractions.Add(currentExtraction);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in currentExtraction.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = currentExtraction;
                        extractionManager.raidManager = this;
                    }
                }
                   
                // Best candidate will be farthest because if there is at least one extraction point, we don't want to always have the nearest
                if(canBeMinimum && currentDistance > farthestDistance)
                {
                    bestCandidate = currentExtraction;
                }
            }
            if(bestCandidate != null)
            {
                if(!possibleExtractions.Contains(bestCandidate))
                {
                    Mod.instance.LogInfo("\t\tAdding candidate to list possible extractions");
                    possibleExtractions.Add(bestCandidate);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in bestCandidate.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = bestCandidate;
                        extractionManager.raidManager = this;
                    }
                }
            }
            else
            {
                Mod.instance.LogError("No minimum extraction found");
            }

            // Init extraction cards
            Transform extractionParent = Mod.playerStatusUI.transform.GetChild(0).GetChild(9);
            extractionCards = new List<GameObject>();
            for(int i=0; i < possibleExtractions.Count; ++i)
            {
                GameObject currentExtractionCard = Instantiate(Mod.extractionCardPrefab, extractionParent);
                extractionCards.Add(currentExtractionCard);

                currentExtractionCard.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = String.Format("EXFIL{0:00} {1}", i, possibleExtractions[i].name);
                if(possibleExtractions[i].times != null && possibleExtractions[i].times.Count > 0||
                   possibleExtractions[i].raidTimes != null && possibleExtractions[i].raidTimes.Count > 0)
                {
                    currentExtractionCard.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
                }

                possibleExtractions[i].card = currentExtractionCard;
            }

            // Initialize doors
            Mod.initDoors = true;
            Transform worldRoot = transform.GetChild(1);
            Transform modelRoot = worldRoot.GetChild(1);
            Transform doorRoot = modelRoot.GetChild(0);
            Transform logicRoot = modelRoot.GetChild(3);
            Transform playerBlocksRoot = logicRoot.GetChild(2);
            int doorIndex = 0;

            MatDef metalMatDef = new MatDef();
            metalMatDef.name = "Door_Metal";
            metalMatDef.BallisticType = MatBallisticType.MetalThick;
            metalMatDef.BulletHoleType = BulletHoleDecalType.Metal;
            metalMatDef.BulletImpactSound = BulletImpactSoundType.MetalThick;
            metalMatDef.ImpactEffectType = BallisticImpactEffectType.Sparks;
            metalMatDef.SoundType = MatSoundType.Metal;

            foreach (JToken doorData in Mod.mapData["maps"][Mod.chosenMapIndex]["doors"])
            {
                GameObject doorObject = doorRoot.GetChild(doorIndex).gameObject;
                GameObject doorInstance = null;
                if (doorData["type"].ToString().Equals("left"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorLeftPrefab, doorRoot);
                }
                else if (doorData["type"].ToString().Equals("right"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorRightPrefab, doorObject.transform.position, doorObject.transform.rotation, doorRoot);
                }
                else if (doorData["type"].ToString().Equals("double"))
                {
                    doorInstance = GameObject.Instantiate(Mod.doorDoublePrefab, doorObject.transform.position, doorObject.transform.rotation, doorRoot);
                }
                doorInstance.transform.localPosition = doorObject.transform.localPosition;
                doorInstance.transform.localRotation = doorObject.transform.localRotation;
                doorInstance.transform.localScale = doorObject.transform.localScale;

                // Get relevant components
                SideHingedDestructibleDoorDeadBolt deadBolt = doorInstance.GetComponentInChildren<SideHingedDestructibleDoorDeadBolt>();
                SideHingedDestructibleDoor doorScript = doorInstance.transform.GetChild(0).GetComponent<SideHingedDestructibleDoor>();

                // Add a doorWrapper
                EFM_DoorWrapper doorWrapper = doorInstance.AddComponent<EFM_DoorWrapper>();

                // Transfer grill if it exists
                if (doorObject.transform.GetChild(0).childCount == 3)
                {
                    doorObject.transform.GetChild(0).GetChild(2).parent = doorInstance.transform.GetChild(0).GetChild(0);
                }

                // Remove frame if necessary
                if (!(bool)doorData["hasFrame"])
                {
                    Destroy(doorInstance.GetComponent<MeshFilter>());
                    Destroy(doorInstance.GetComponent<MeshRenderer>());
                }

                // Transfer material if necessary
                if ((bool)doorData["customMat"])
                {
                    if ((bool)doorData["hasFrame"])
                    {
                        doorInstance.GetComponent<MeshRenderer>().material = doorObject.GetComponent<MeshRenderer>().material;
                        Mod.instance.LogInfo("\tTransfered mat to frame");
                    }
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material = doorObject.transform.GetChild(0).GetChild(0).GetComponent<MeshRenderer>().material;
                    Mod.instance.LogInfo("\tTransfered mat to door");
                }

                // Set PMat if necessary
                if (doorData["mat"].ToString().Equals("metal"))
                {
                    doorInstance.GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                    doorInstance.transform.GetChild(0).GetChild(0).GetComponent<PMat>().MatDef = metalMatDef;
                    Mod.instance.LogInfo("\tSet PMat to metal");
                }

                // Flip lock if necessary
                if ((bool)doorData["flipLock"])
                {
                    doorWrapper.flipLock = true;
                    if(deadBolt != null)
                    {
                        deadBolt.transform.Rotate(0, 180, 0);
                    }
                }

                // Set destructable
                if (!(bool)doorData["destructable"])
                {
                    Destroy(doorInstance.transform.GetChild(0).GetComponent<UberShatterable>()); // TODO: Verify if this works
                }

                // Set door to active
                doorInstance.SetActive(true);

                // Set door angle
                //bool hasAngle = (bool)doorData["angle"];
                //if (hasAngle)
                //{
                //    //Transform doorPlaceholder = doorObject.transform.GetChild(0);
                //    //Transform fakeHingePlaceholder = doorObject.transform.GetChild(1);
                //    //Transform door = doorInstance.transform.GetChild(0);
                //    //Transform fakeHinge = doorInstance.transform.GetChild(1);
                //    //door.gameObject.SetActive(false);
                //    //door.localPosition = doorPlaceholder.localPosition;
                //    //door.localRotation = doorPlaceholder.localRotation;
                //    //if (disabledDoors == null)
                //    //{
                //    //    disabledDoors = new List<GameObject>();
                //    //}
                //    //disabledDoors.Add(door.gameObject);

                //    //if (hingeJointsToDisableLimits == null)
                //    //{
                //    //    hingeJointsToDisableLimits = new List<HingeJoint>();
                //    //}
                //    //if (correspondingLimits == null)
                //    //{
                //    //    correspondingLimits = new List<JointLimits>();
                //    //}

                //    //float angleToUse = fakeHingePlaceholder.eulerAngles.y;
                //    ////if (doorData["type"].ToString().Equals("left"))
                //    ////{
                //    ////    angleToUse = -angleToUse;
                //    ////}
                //    //JointLimits jl = new JointLimits();
                //    //jl.min = angleToUse;
                //    //jl.max = angleToUse + 1;

                //    //correspondingLimits.Add(doorScript.HingeLower.limits);
                //    //correspondingLimits.Add(doorScript.HingeUpper.limits);

                //    //doorScript.HingeLower.limits = jl;
                //    //doorScript.HingeUpper.limits = jl;

                //    //hingeJointsToDisableLimits.Add(doorScript.HingeLower);
                //    //hingeJointsToDisableLimits.Add(doorScript.HingeUpper);
                //}
                Mod.instance.LogInfo("\tSet door angle");

                // Set key
                int keyIndex = (int)doorData["keyIndex"];
                if (keyIndex > -1)
                {
                    doorWrapper.keyID = keyIndex.ToString();
                    if ((bool)doorData["locked"])
                    {
                        EFM_DoorLockChecker doorLockChecker = doorInstance.AddComponent<EFM_DoorLockChecker>();
                        doorLockChecker.deadBolt = deadBolt;
                        doorLockChecker.blocks = playerBlocksRoot.GetChild((int)doorData["blockIndex"]).gameObject;
                    }
                    else
                    {
                        // Make sure door is unlocked
                        doorScript.ForceUnlock();
                    }
                }
                else
                {
                    // Make sure door is unlocked
                    doorScript.ForceUnlock();
                }

                // Destroy placeholder door
                doorObject.SetActive(false);
                //Destroy(doorObject);

                ++doorIndex;
            }

            // Spawn loose loot
            JObject locationDB = Mod.locationsDB[GetLocationDataIndex(Mod.chosenMapIndex)];
            Transform itemsRoot = transform.GetChild(1).GetChild(1).GetChild(2);
            List<string> missingForced = new List<string>();
            List<string> missingDynamic = new List<string>();
            // Forced, always spawns TODO: Unless player has it already? Unless player doesnt have the quest yet?
            foreach(JToken forced in locationDB["forced"])
            {
                JArray items = forced["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItems = new Dictionary<string, EFM_CustomItemWrapper>();
                for (int i=0; i < items.Count; ++i)
                {
                    // Get item from item map
                    string originalID = items[i]["_tpl"].ToString();
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        itemID = Mod.itemMap[originalID];
                    }
                    else
                    {
                        missingForced.Add(originalID);
                        continue;
                    }

                    // Get item prefab
                    GameObject itemPrefab = null;
                    if (int.TryParse(itemID, out int index))
                    {
                        itemPrefab = Mod.itemPrefabs[index];
                    }
                    else
                    {
                        itemPrefab = IM.OD[itemID].GetGameObject();
                    }
                    EFM_CustomItemWrapper itemWrapper = itemPrefab.GetComponent<EFM_CustomItemWrapper>();

                    // Check for stack
                    int amountToSpawn = items[i]["upd"] == null ? 1 : items[i]["upd"]["StackObjectCount"] == null ? 1 : (int)items[i]["upd"]["StackObjectCount"];
                    bool stackable = itemWrapper != null && itemWrapper.maxStack > 1;

                    // Spawn item(s)
                    if (!stackable)
                    {
                        // If not stackable, spawn individual instances of this item and place them under the same parent
                        for (int j = 0; j < amountToSpawn; ++j)
                        {
                            SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, forced, originalID);
                        }
                    }
                    else // Unstackable
                    {
                        GameObject itemObject = SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, forced, originalID);

                        // TODO: Set the item's stack
                        // TODO: itemObject.GetComponent<EFM_CustomItemWrapper>().stack = amountToSpawn;
                    }
                }
            }
            // TODO: Figure out how to spawn static loot
            // Dynamic, has chance of spawning based on rarity TODO: Which should be written to default item data
            foreach (JToken dynamic in locationDB["dynamic"])
            {
                JArray items = dynamic["Items"].Value<JArray>();
                Dictionary<string, EFM_CustomItemWrapper> spawnedItems = new Dictionary<string, EFM_CustomItemWrapper>();
                for (int i = 0; i < items.Count; ++i)
                {
                    // Get item from item map
                    string originalID = items[i]["_tpl"].ToString();
                    string itemID = null;
                    if (Mod.itemMap.ContainsKey(originalID))
                    {
                        itemID = Mod.itemMap[originalID];
                    }
                    else
                    {
                        missingDynamic.Add(originalID);
                        continue;
                    }

                    // Get item prefab
                    GameObject itemPrefab = null;
                    if (int.TryParse(itemID, out int index))
                    {
                        itemPrefab = Mod.itemPrefabs[index];
                    }
                    else
                    {
                        itemPrefab = IM.OD[itemID].GetGameObject();
                    }
                    EFM_CustomItemWrapper itemWrapper = itemPrefab.GetComponent<EFM_CustomItemWrapper>();

                    // Check for stack
                    int amountToSpawn = items[i]["upd"] == null ? 1 : items[i]["upd"]["StackObjectCount"] == null ? 1 : (int)items[i]["upd"]["StackObjectCount"];
                    bool stackable = itemWrapper != null && itemWrapper.maxStack > 1;

                    // Spawn item(s)
                    if (!stackable)
                    {
                        // If not stackable, spawn individual instances of this item and place them under the same parent
                        for (int j = 0; j < amountToSpawn; ++j)
                        {
                            SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, dynamic, originalID);
                        }
                    }
                    else // Unstackable
                    {
                        GameObject itemObject = SpawnLootItem(itemPrefab, itemsRoot, itemID, items[i], spawnedItems, dynamic, originalID);

                        // TODO: Set the item's stack
                        // TODO: itemObject.GetComponent<EFM_CustomItemWrapper>().stack = amountToSpawn;
                    }
                }
            }

            // Spawn container loot
            // TODO: When we get containers implemented 

            // Output missing items
            if (Mod.instance.debug)
            {
                string text = "Raid with map index = " + Mod.chosenMapIndex + " was missing FORCED loose loot:\n";
                foreach(string id in missingForced)
                {
                    text += id + "\n";
                }
                text += "Raid with map index = " + Mod.chosenMapIndex + " was missing DYNAMIC loose loot:\n";
                foreach(string id in missingDynamic)
                {
                    text += id + "\n";
                }
                File.AppendAllText("BepinEx/Plugins/EscapeFromMeatov/ErrorLog.txt", text);
            }

            // Init time
            InitTime();

            // Get sun
            sun = transform.GetChild(1).GetChild(0).GetComponent<Light>();

            inRaid = true;

            init = true;
        }

        public void InitEffects()
        {
            foreach(EFM_Effect effect in EFM_Effect.effects)
            {
                if (effect.active)
                {
                    switch (effect.effectType)
                    {
                        case EFM_Effect.EffectType.EnergyRate:
                            currentEnergyRate += effect.value;
                            break;
                        case EFM_Effect.EffectType.HydrationRate:
                            currentHydrationRate += effect.value;
                            break;
                        case EFM_Effect.EffectType.HealthRate:
                            for (int j = 0; j < 7; ++j)
                            {
                                currentHealthRates[j] += effect.value / 7;
                            }
                            break;
                    }
                }
            }
        }

        private int GetLocationDataIndex(int chosenMapIndex)
        {
            switch (chosenMapIndex)
            {
                case 0: // Factory
                    return 1;
                default:
                    return 1;
            }
        }

        private void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * EFM_Manager.meatovTimeMultiplier) % 86400);
            time = (scaledTime + Mod.chosenTimeIndex == 0 ? 0 : 43200) % 86400;
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * EFM_Manager.meatovTimeMultiplier;

            time %= 86400;
        }

        private GameObject SpawnLootItem(GameObject itemPrefab, Transform itemsRoot, string itemID, JToken itemData, Dictionary<string, EFM_CustomItemWrapper> spawnedItems, JToken spawnData, string originalID)
        {
            GameObject itemObject = GameObject.Instantiate(itemPrefab, itemsRoot);
            if (itemObject == null)
            {
                Mod.instance.LogError("Could not instantiate item prefab: " + itemID);
                return null;
            }

            // Position and rotate item
            if (itemData["parentId"] != null)
            {
                // Item has a parent which should be a previously spawned item
                // This parent should be a container of some sort so we need to add this item to the container
                if (spawnedItems.ContainsKey(itemData["parentId"].ToString()))
                {
                    EFM_CustomItemWrapper parent = spawnedItems[itemData["parentId"].ToString()];
                    bool boxMainContainer = parent.mainContainer.GetComponent<BoxCollider>() != null;
                    parent.AddItemToContainer(itemObject.GetComponent<EFM_CustomItemWrapper>().physicalObject);
                    if (boxMainContainer)
                    {
                        BoxCollider boxCollider = parent.mainContainer.GetComponent<BoxCollider>();
                        itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
                                                                         UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
                                                                         UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
                    }
                    else
                    {
                        CapsuleCollider capsuleCollider = parent.mainContainer.GetComponent<CapsuleCollider>();
                        if (capsuleCollider != null)
                        {
                            itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-capsuleCollider.radius / 2, capsuleCollider.radius / 2),
                                                                             UnityEngine.Random.Range(-(capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.height / 2 - capsuleCollider.radius),
                                                                             0);
                        }
                        else
                        {
                            itemObject.transform.localPosition = Vector3.zero;
                        }
                    }
                    itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
                }
                else
                {
                    Mod.instance.LogError("Attempted to spawn loose loot " + itemID + " with original ID " + originalID + " but parentID " + itemData["parentId"].ToString() + " does not exist in spawned items list");
                }
            }
            else
            {
                if (spawnData["Position"].Type == JTokenType.Array)
                {
                    Vector3 position = new Vector3((float)spawnData["Position"][0], (float)spawnData["Position"][1], (float)spawnData["Position"][2]);
                    itemObject.transform.position = position;
                }
                if (spawnData["Rotation"].Type == JTokenType.Array)
                {
                    Vector3 rotation = new Vector3((float)spawnData["Rotation"][0], (float)spawnData["Rotation"][1], (float)spawnData["Rotation"][2]);
                    itemObject.transform.rotation = Quaternion.Euler(rotation);
                }
            }

            // Add custom item wrapper to spawned items dict with _id as key
            if (spawnedItems.ContainsKey(itemData["_id"].ToString()))
            {
                spawnedItems.Add(itemData["_id"].ToString(), itemObject.GetComponent<EFM_CustomItemWrapper>());
            }

            return itemObject;
        }

        public override void InitUI()
        {
        }

        public void LoadMapData()
        {
            extractions = new List<Extraction>();
            Transform extractionRoot = transform.GetChild(3);

            int extractionIndex = 0;
            foreach(JToken extractionData in Mod.mapData["maps"][Mod.chosenMapIndex]["extractions"])
            {
                Extraction extraction = new Extraction();
                extraction.gameObject = extractionRoot.GetChild(extractionIndex).gameObject;
                extraction.name = extraction.gameObject.name;

                extraction.raidTimes = new List<TimeInterval>();
                foreach(JToken raidTimeData in extractionData["raidTimes"])
                {
                    TimeInterval raidTime = new TimeInterval();
                    raidTime.start = (int)raidTimeData["start"];
                    raidTime.end = (int)raidTimeData["end"];
                    extraction.raidTimes.Add(raidTime);
                }
                extraction.times = new List<TimeInterval>();
                foreach(JToken timeData in extractionData["times"])
                {
                    TimeInterval time = new TimeInterval();
                    time.start = (int)timeData["start"];
                    time.end = (int)timeData["end"];
                    extraction.times.Add(time);
                }
                extraction.itemRequirements = new List<string>();
                foreach (JToken itemRequirement in extractionData["itemRequirements"])
                {
                    extraction.itemRequirements.Add(itemRequirement.ToString());
                }
                extraction.equipmentRequirements = new List<string>();
                foreach (JToken equipmentRequirement in extractionData["equipmentRequirements"])
                {
                    extraction.equipmentRequirements.Add(equipmentRequirement.ToString());
                }
                extraction.itemBlacklist = new List<string>();
                foreach (JToken blacklistItem in extractionData["itemBlacklist"])
                {
                    extraction.itemBlacklist.Add(blacklistItem.ToString());
                }
                extraction.equipmentBlacklist = new List<string>();
                foreach (JToken blacklistEquipment in extractionData["equipmentBlacklist"])
                {
                    extraction.equipmentBlacklist.Add(blacklistEquipment.ToString());
                }
                extraction.role = (int)extractionData["role"];
                extraction.accessRequirement = (bool)extractionData["accessRequirement"];

                extractions.Add(extraction);

                ++extractionIndex;
            }
        }

        private float ExtractionChanceByDist(float distance)
        {
            if(distance < 150)
            {
                return 0.001f * distance; // 15% chance at 150 meters
            }
            else if(distance < 500)
            {
                return 0.0008f * distance + 0.15f; // 55% chance at 500 meters
            }
            else // > 500
            {
                return 0.00045f * distance + 0.55f; // 100% at 1000 meters
            }
        }
    }

    class ExtractionManager : MonoBehaviour
    {
        public EFM_Raid_Manager raidManager;

        public Extraction extraction;

        private List<Collider> playerColliders;
        public bool active;

        private void Start()
        {
            Mod.instance.LogInfo("Extraction manager created for extraction: "+extraction.name);
            playerColliders = new List<Collider>();
        }

        private void Update()
        {
            active = false;
            if(extraction.times != null && extraction.times.Count > 0)
            {
                foreach(TimeInterval timeInterval in extraction.times)
                {
                    if(raidManager.time >= timeInterval.start && raidManager.time <= timeInterval.end)
                    {
                        active = true;
                        break;
                    }
                }
            }
            else
            {
                active = true;
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            Mod.instance.LogInfo("Trigger enter called on "+gameObject.name+": "+collider.name);
            if (EFM_Raid_Manager.currentManager.currentExtraction == null)
            {
                if (collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Hitbox_Neck") ||
                   collider.gameObject.name.Equals("Hitbox_Head") ||
                   collider.gameObject.name.Equals("Hitbox_Torso"))
                {
                    Mod.instance.LogInfo("Collider is player");
                    if (playerColliders.Count == 0)
                    {
                        EFM_Raid_Manager.currentManager.currentExtraction = this;
                    }

                    playerColliders.Add(collider);
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (playerColliders.Count > 0)
            {
                playerColliders.Remove(collider);

                if (playerColliders.Count == 0 && EFM_Raid_Manager.currentManager.currentExtraction == this)
                {
                    EFM_Raid_Manager.currentManager.currentExtraction = null;
                }
            }
        }
    }

    public class Extraction
    {
        public string name;
        public GameObject gameObject;

        public List<TimeInterval> raidTimes;
        public List<TimeInterval> times;
        public List<string> itemRequirements;
        public List<string> equipmentRequirements;
        public List<string> itemBlacklist;
        public List<string> equipmentBlacklist;
        public int role;
        public bool accessRequirement;
        public GameObject card;

        public string RequirementsMet()
        {
            if((itemRequirements == null || itemRequirements.Count == 0) &&
               (itemBlacklist == null || itemBlacklist.Count == 0) &&
               (equipmentRequirements == null || equipmentRequirements.Count == 0) &&
               (equipmentBlacklist == null || equipmentBlacklist.Count == 0))
            {
                return "";
            }

            // Item requirements
            foreach(string id in itemRequirements)
            {
                if (!Mod.playerInventory.ContainsKey(id))
                {
                    if(int.TryParse(id, out int result))
                    {
                        return "Need " + Mod.itemPrefabs[result].name;
                    }
                    else
                    {
                        return "Need " + IM.OD[id].name;
                    }
                }
            }

            // Item blacklist
            foreach(string id in itemBlacklist)
            {
                if (Mod.playerInventory.ContainsKey(id))
                {
                    if(int.TryParse(id, out int result))
                    {
                        return "Can't have " + Mod.itemPrefabs[result].name;
                    }
                    else
                    {
                        return "Can't have " + IM.OD[id].name;
                    }
                }
            }

            // Equipment requirements
            foreach(string id in equipmentRequirements)
            {
                bool found = false;
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i].CurObject.ObjectWrapper.ItemID.Equals(id))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    if (int.TryParse(id, out int result))
                    {
                        return "Need " + Mod.itemPrefabs[result].name + " equipped";
                    }
                    else
                    {
                        return "Need " + IM.OD[id].name + " equipped";
                    }
                }
            }

            // Equipment blacklist
            foreach(string id in equipmentBlacklist)
            {
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i].CurObject.ObjectWrapper.ItemID.Equals(id))
                    {
                        if (int.TryParse(id, out int result))
                        {
                            return "Can't have " + Mod.itemPrefabs[result].name + " equipped";
                        }
                        else
                        {
                            return "Can't have " + IM.OD[id].name + " equipped";
                        }
                    }
                }
                if (id.Equals("5448e53e4bdc2d60728b4567")) // Backpack
                {
                    return "Can't have a backpack equipped";
                }
                else if (id.Equals("5448e54d4bdc2dcc718b4568")) // Armor
                {
                    return "Can't have body armor equipped";
                }
                //else if (id.Equals("57bef4c42459772e8d35a53b")) // Armored equip
                //{
                //    return "Can't have armored rig equipped";
                //}
            }

            return "";
        }
    }

    public class TimeInterval
    {
        public int start;
        public int end;
    }

    public class DoorRotationSettings
    {
        public Transform placeholder;
        public Transform actual;
    }
}
