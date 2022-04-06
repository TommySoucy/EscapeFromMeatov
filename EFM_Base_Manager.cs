using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_Base_Manager : EFM_Manager
    {
        public enum FinishRaidState
        {
            Survived,

            // These are fails
            RunThrough,
            MIA,
            KIA
        }

        // UI
        private Button[][] buttons;
        private Transform canvas;
        private Text raidCountdownTitle;
        private Text raidCountdown;
        private Text timeChoice0;
        private Text timeChoice1;
        private Text chosenCharacter;
        private Text chosenMap;
        private Text chosenTime;
        private AudioSource clickAudio;

        // Assets
        public static GameObject areaCanvasPrefab; // AreaCanvas
        public static GameObject areaCanvasBottomButtonPrefab; // AreaCanvasBottomButton
        public static GameObject areaRequirementPrefab; // AreaRequirement
        public static GameObject itemRequirementPrefab; // ItemRequirement
        public static GameObject skillRequirementPrefab; // SkillRequirement
        public static GameObject traderRequirementPrefab; // TraderRequirement
        public static GameObject areaRequirementsPrefab; // AreaRequirements
        public static GameObject itemRequirementsPrefab; // ItemRequirements
        public static GameObject skillRequirementsPrefab; // SkillRequirements
        public static GameObject traderRequirementsPrefab; // TraderRequirements
        public static GameObject bonusPrefab; // Bonus
        public static Sprite areaBackgroundNormalSprite; // area_icon_default_back
        public static Sprite areaBackgroundLockedSprite; // area_icon_locked_back
        public static Sprite areaBackgroundAvailableSprite; // area_icon_default_back_green
        public static Sprite areaBackgroundEliteSprite; // area_icon_elite_back
        public static Sprite areaStatusIconUpgrading; // icon_status_upgrading
        public static Sprite areaStatusIconConstructing; // icon_status_constructing
        public static Sprite areaStatusIconLocked; // icon_lock
        public static Sprite areaStatusIconUnlocked; // icon_status_unlocked
        public static Sprite areaStatusIconReadyUpgrade; // icon_status_ready_to_upgrade
        public static Sprite areaStatusIconProducing; // icon_status_producing
        public static Sprite areaStatusIconOutOfFuel; // icon_out_of_fuel
        public static Sprite requirementFulfilled; // icon_requirement_fulfilled
        public static Sprite requirementLocked; // icon_requirement_locked
        public static Sprite[] traderAvatars; // avatar_russian_small, avatar_therapist_small, avatar_fence_small, avatar_ah_small, avatar_peacekeeper_small, avatar_tech_small, avatar_ragman_small, avatar_jaeger_small
        public static Sprite[] areaIcons; // icon_vents, icon_security, icon_watercloset, icon_stash, icon_generators, icon_heating, icon_rain_collector, icon_medstation, icon_kitchen, icon_restplace, icon_workbench, icon_intelligence_center, icon_shooting_range, icon_library, icon_scav_case, icon_illumination, icon_placeoffame, icon_afu, icon_solarpower, icon_boozegen, icon_bitcionfarm, icon_christmas_illumination
        public static Dictionary<string, Sprite> bonusIcons;
        public static Sprite[] skillIcons;

        public JToken data;

        public int chosenCharIndex = -1;
        public int chosenMapIndex = -1;
        public int chosenTimeIndex = -1;

        public float time;
        private bool cancelRaidLoad;
        private bool loadingRaid;
        private bool countdownDeploy;
        private float deployTimer;
        private float deployTime = 0; // TODO: Should be 10 but set to 0 for faster debugging
        AssetBundleCreateRequest currentRaidBundleRequest;
        public List<EFM_BaseAreaManager> baseAreaManagers;
        public Dictionary<string, int> baseInventory;
        public Dictionary<string, List<GameObject>> baseInventoryObjects;
        public EFM_TraderStatus[] traderStatuses;
        public float[] maxHealth = { 35, 85, 70, 60, 60, 65, 65 };
        public static float[] healthRates = { 0.6125f, 1.4f, 1.225f, 1.05f, 1.05f, 1.1375f, 1.1375f };
        public static float[] currentHealthRates = { 0.6125f, 1.4f, 1.225f, 1.05f, 1.05f, 1.1375f, 1.1375f };
        public float energyRate = 1;
        public float currentEnergyRate = 1;
        public float hydrationRate = 1;
        public float currentHydrationRate = 1;

        private void Update()
        {
            if (init)
            {
                UpdateTime();
            }

            UpdateEffects();

            // Handle raid loading process
            if (cancelRaidLoad)
            {
                loadingRaid = false;
                countdownDeploy = false;

                // Wait until the raid map is done loading before unloading it
                if (currentRaidBundleRequest.isDone)
                {
                    if(currentRaidBundleRequest.assetBundle != null)
                    {
                        currentRaidBundleRequest.assetBundle.Unload(true);
                        cancelRaidLoad = false;
                    }
                    else
                    {
                        cancelRaidLoad = false;
                    }
                    canvas.GetChild(7).gameObject.SetActive(false);
                    canvas.GetChild(0).gameObject.SetActive(true);
                }
            }
            else if (countdownDeploy)
            {
                deployTimer -= Time.deltaTime;

                TimeSpan timeSpan = TimeSpan.FromSeconds(deployTimer);
                raidCountdown.text = string.Format(@"{0:ss\.ff}", timeSpan);

                if (deployTimer <= 0)
                {
                    SteamVR_LoadLevel.Begin("Meatov"+ chosenMap.text+ "Scene", false, 0.5f, 0f, 0f, 0f, 1f);
                    countdownDeploy = false;
                }
            }
            else if (loadingRaid)
            {
                if(currentRaidBundleRequest.isDone)
                {
                    if(currentRaidBundleRequest.assetBundle != null)
                    {
                        // Load the asset of the map
                        // currentRaidMapRequest = currentRaidBundleRequest.assetBundle.LoadAllAssetsAsync<GameObject>();
                        deployTimer = deployTime;

                        loadingRaid = false;
                        countdownDeploy = true;
                    }
                    else
                    {
                        Mod.instance.LogError("Could not load raid map bundle, cancelling");
                        cancelRaidLoad = true;
                    }
                }
                else
                {
                    raidCountdownTitle.text = "Loading map:";
                    raidCountdown.text = (currentRaidBundleRequest.progress * 100).ToString() + "%";
                }
            }
        }

        private void UpdateEffects()
        {
            // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
            bool[] heal = new bool[7];
            for(int i=0; i < 7; ++i)
            {
                heal[i] = true;
            }
            for(int i = EFM_Effect.effects.Count; i >= 0; --i)
            {
                if(EFM_Effect.effects.Count == 0)
                {
                    break;
                }
                else if(i >= EFM_Effect.effects.Count)
                {
                    continue;
                }

                EFM_Effect effect = EFM_Effect.effects[i];
                if (effect.active)
                {
                    if (effect.hasTimer)
                    {
                        effect.timer -= Time.deltaTime;
                        if(effect.timer <= 0)
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
                                    // TODO: Stop tremors if there are not other tremor effects
                                    break;
                                case EFM_Effect.EffectType.QuantumTunnelling:
                                    // TODO: Stop QuantumTunnelling
                                    break;
                                case EFM_Effect.EffectType.HealthRate:
                                    if (effect.partIndex == -1)
                                    {
                                        for (int j = 0; j < 7; ++j)
                                        {
                                            currentHealthRates[j] -= effect.value / 7;
                                        }
                                    }
                                    else
                                    {
                                        currentHealthRates[effect.partIndex] -= effect.value;
                                    }
                                    break;
                                case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                    // Reactivate all bleeding 
                                    // Not necessary because when we disabled them we used the disable timer
                                    break;
                                case EFM_Effect.EffectType.Contusion:
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
                                    foreach(EFM_Effect effectCheck in EFM_Effect.effects)
                                    {
                                        if(effectCheck.effectType == EFM_Effect.EffectType.HandsTremor && effectCheck.active)
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
                                    // Will remove toxin on ativation, does nothing after
                                    break;
                                case EFM_Effect.EffectType.LightBleeding:
                                case EFM_Effect.EffectType.HeavyBleeding:
                                    // Remove all effects caused by this bleeding
                                    foreach (EFM_Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == EFM_Effect.EffectType.HealthRate)
                                        {
                                            currentHealthRates[causedEffect.partIndex] -= causedEffect.value;
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

                    if(effect.active && effect.partIndex != -1 && (effect.effectType == EFM_Effect.EffectType.LightBleeding ||
                                                                   effect.effectType == EFM_Effect.EffectType.HeavyBleeding ||
                                                                   (effect.effectType == EFM_Effect.EffectType.HealthRate && effect.value < 0)))
                    {
                        heal[effect.partIndex] = false;
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
                                if (effect.partIndex == -1)
                                {
                                    for (int j = 0; j < 7; ++j)
                                    {
                                        currentHealthRates[j] += effect.value / 7;
                                    }
                                }
                                else
                                {
                                    currentHealthRates[effect.partIndex] += effect.value;
                                }
                                break;
                            case EFM_Effect.EffectType.RemoveAllBloodLosses:
                                // Deactivate all bleeding using disable timer
                                foreach(EFM_Effect bleedEffect in EFM_Effect.effects)
                                {
                                    if(bleedEffect.effectType == EFM_Effect.EffectType.LightBleeding || bleedEffect.effectType == EFM_Effect.EffectType.HeavyBleeding)
                                    {
                                        bleedEffect.active = false;
                                        bleedEffect.inactiveTimer = effect.timer;
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

            // Apply health, energy, and hydration rates
            for (int i = 0; i < 7; ++i)
            {
                if (heal[i] && currentHealthRates[i] > 0)
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthRates[i] * (Time.deltaTime / 60), 1, maxHealth[i]);
                }
            }

            if (currentHydrationRate > 0)
            {
                Mod.hydration = Mathf.Clamp(Mod.hydration + currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);
            }

            if (currentEnergyRate > 0)
            {
                Mod.energy = Mathf.Clamp(Mod.energy + currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);
            }
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * EFM_Manager.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            TimeSpan timeSpan = TimeSpan.FromSeconds(time);
            string formattedTime0 = string.Format(@"{0:hh\:mm}", timeSpan);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            TimeSpan offsetTimeSpan = TimeSpan.FromSeconds(offsetTime);
            string formattedTime1 = string.Format(@"{0:hh\:mm}", offsetTimeSpan);
            timeChoice1.text = formattedTime1;

            chosenTime.text = chosenTimeIndex == 0 ? formattedTime0 : formattedTime1;
        }

        public override void Init()
        {
            // Don't want to setup player rig if just got out of raid
            if (!Mod.justFinishedRaid)
            {
                SetupPlayerRig();
            }

            ProcessData();

            if (Mod.justFinishedRaid)
            {
                FinishRaid(Mod.raidState); // This will save on autosave

                // Set any parts health to 1 if they are at 0
                for(int i=0; i < 7; ++i)
                {
                    if (Mod.health[i] == 0)
                    {
                        Mod.health[i] = 1;
                    }
                }
            }

            InitUI();

            InitTime();

            Mod.justFinishedRaid = false;

            init = true;
        }

        private void SetupPlayerRig()
        {
            Mod.instance.LogInfo("Setup player rig called");
            // Add equipment slots
            // Clear any previously existing ones
            if (Mod.equipmentSlots != null)
            {
                for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                {
                    if (Mod.equipmentSlots[i] != null)
                    {
                        Destroy(Mod.equipmentSlots[i].gameObject);
                    }
                }
                Mod.equipmentSlots.Clear();
            }
            else
            {
                Mod.equipmentSlots = new List<EFM_EquipmentSlot>();
            }
            List<GameObject> slotObjects = new List<GameObject>();
            for (int side = 0; side < 2; ++side)
            {
                for (int x = 0; x < 2; ++x)
                {
                    for (int y = 0; y < 2; ++y)
                    {
                        GameObject slotObject = Instantiate(Mod.quickBeltSlotPrefab, side == 0 ? GM.CurrentPlayerBody.RightHand : GM.CurrentPlayerBody.LeftHand);
                        slotObject.tag = "QuickbeltSlot";
                        slotObject.transform.localPosition = new Vector3((x - 0.5f) * 0.1f, 0, (y - 2.5f) * 0.1f);
                        slotObject.transform.localRotation = Quaternion.identity;
                        slotObject.name = "EquipmentSlot" + (side == 0 ? "Right" : "Left");
                        slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields
                        slotObjects.Add(slotObject);

                        EFM_EquipmentSlot slotComponent = slotObject.AddComponent<EFM_EquipmentSlot>();
                        Mod.equipmentSlots.Add(slotComponent);
                        Mod.instance.LogInfo("created equip slot x:"+x+",y:"+y);
                        slotComponent.QuickbeltRoot = slotObject.transform;
                        slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                        slotComponent.HoverGeo.SetActive(false);
                        slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                        slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
                        slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                        slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;

                        slotComponent.HoverGeo.transform.localScale = Vector3.one * 0.1f;
                        slotObject.transform.GetChild(0).GetChild(1).localScale = Vector3.one * 0.1f;
                        slotComponent.PoseOverride.transform.localScale = Vector3.one * 0.1f;
                        slotComponent.PoseOverride.transform.localRotation = Quaternion.Euler(270, -90, -90);

                        // Set slot sphere materials
                        slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                        slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;
                    }
                }
            }
            foreach (GameObject slotObject in slotObjects)
            {
                slotObject.SetActive(true);
            }

            // Add our own hand component to each hand
            Mod.rightHand = GM.CurrentPlayerBody.RightHand.gameObject.AddComponent<EFM_Hand>();
            Mod.leftHand = GM.CurrentPlayerBody.LeftHand.gameObject.AddComponent<EFM_Hand>();
            Mod.rightHand.otherHand = Mod.leftHand;
            Mod.leftHand.otherHand = Mod.rightHand;
        }

        public void ProcessData()
        {
            // Check if we have loaded data
            if (data == null)
            {
                // TODO: This is a new game, so we need to spawn starting equipement in the base and story/tutorial UI
                data = new JObject();
                Mod.level = 1;
                Mod.skills = new EFM_Skill[64];
                Mod.health = new float[7];
                Mod.hydration = 100;
                Mod.energy = 100;

                // Spawn standard edition starting items
                GameObject.Instantiate(Mod.itemPrefabs[25], new Vector3(0,0,-1.2f), Quaternion.identity, transform.GetChild(transform.childCount - 2));
                GameObject.Instantiate(Mod.itemPrefabs[407], transform.GetChild(transform.childCount - 2));

                // Instantiate areas
                baseAreaManagers = new List<EFM_BaseAreaManager>();
                for (int i = 0; i < 22; ++i)
                {
                    EFM_BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<EFM_BaseAreaManager>();
                    currentBaseAreaManager.baseManager = this;
                    currentBaseAreaManager.areaIndex = i;
                    currentBaseAreaManager.level = i == 3 ? 1 : 0; // Stash starts at level 1
                    currentBaseAreaManager.constructing = false;
                    currentBaseAreaManager.constructTime = 0;

                    baseAreaManagers.Add(currentBaseAreaManager);
                }

                // Instantiate other
                baseInventory = new Dictionary<string, int>();
                baseInventoryObjects = new Dictionary<string, List<GameObject>>();
                traderStatuses = new EFM_TraderStatus[8];
                for (int i = 0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, 0, 0, i == 7 ? false : true);
                }

                UpdateBaseInventory();

                return;
            }

            // Load player status
            Mod.level = (float)data["level"];
            Mod.health = data["health"].ToObject<float[]>();
            Mod.hydration = (float)data["hydration"];
            Mod.maxHydration = (float)data["maxHydration"];
            Mod.energy = (float)data["energy"];
            Mod.maxEnergy = (float)data["maxEnergy"];
            Mod.stamina = (float)data["stamina"];
            Mod.maxStamina = (float)data["maxStamina"];
            Mod.skills = new EFM_Skill[64];
            for(int i=0; i<64; ++i)
            {
                Mod.skills[i].progress = (float)data["skills"][i]["progress"];
                Mod.skills[i].currentProgress = (float)data["skills"][i]["currentProgress"];
            }

            // Instantiate items
            Transform itemsRoot = transform.GetChild(2);
            JArray loadedItems = (JArray)data["items"];
            for (int i = 0; i < loadedItems.Count; ++i)
            {
                JToken item = loadedItems[i];

                // If just finished raid, skip any items that are on player since we want to keep what player found in raid
                if (Mod.justFinishedRaid && ((int)item["PhysicalObject"]["equipSlot"] != -1 || (int)item["PhysicalObject"]["heldMode"] != 0 || (int)item["PhysicalObject"]["m_quickBeltSlot"] != -1))
                {
                    continue;
                }

                LoadSavedItem(itemsRoot, item);
            }
            // Count each type of item we have
            UpdateBaseInventory();

            // Instantiate areas
            baseAreaManagers = new List<EFM_BaseAreaManager>();
            for (int i = 0; i < 22; ++i)
            {
                EFM_BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<EFM_BaseAreaManager>();
                currentBaseAreaManager.baseManager = this;
                currentBaseAreaManager.areaIndex = i;
                if (data["areas"] != null)
                {
                    JArray loadedAreas = (JArray)data["areas"];
                    currentBaseAreaManager.level = (int)loadedAreas[i]["level"];
                    currentBaseAreaManager.constructing = (bool)loadedAreas[i]["constructing"];
                    currentBaseAreaManager.constructTime = (float)loadedAreas[i]["constructTime"];
                    if (loadedAreas[i]["slots"] != null)
                    {
                        currentBaseAreaManager.slotItems = new List<GameObject>();
                        JArray loadedAreaSlot = (JArray)loadedAreas[i]["slots"];
                        foreach (JToken item in loadedAreaSlot)
                        {
                            if (item == null)
                            {
                                currentBaseAreaManager.slotItems.Add(null);
                            }
                            else
                            {
                                currentBaseAreaManager.slotItems.Add(LoadSavedItem(currentBaseAreaManager.transform.GetChild(currentBaseAreaManager.transform.childCount - 1), item));
                            }
                        }
                    }
                }
                currentBaseAreaManager.level = 0;
                currentBaseAreaManager.constructing = false;
                currentBaseAreaManager.constructTime = 0;

                baseAreaManagers.Add(currentBaseAreaManager);
            }

            // Load trader statuses
            traderStatuses = new EFM_TraderStatus[8];
            if(data["traderStatuses"] == null)
            {
                for(int i=0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, 0, 0, i == 7 ? false : true);
                }
            }
            else
            {
                JArray loadedTraderStatuses = (JArray)data["traderStatuses"];
                for (int i = 0; i < 8; i++)
                {
                    traderStatuses[i] = new EFM_TraderStatus(this, i, (float)loadedTraderStatuses[i]["salesSum"], (float)loadedTraderStatuses[i]["standing"], (bool)loadedTraderStatuses[i]["unlocked"]);
                }
            }
        }

        public void UpdateBaseInventory()
        {
            if(baseInventory == null)
            {
                baseInventory = new Dictionary<string, int>();
                baseInventoryObjects = new Dictionary<string, List<GameObject>>();
            }

            baseInventory.Clear();
            baseInventoryObjects.Clear();

            Transform itemsRoot = transform.GetChild(2);

            foreach(Transform item in itemsRoot)
            {
                AddToBaseInventory(item);
            }
        }

        private void AddToBaseInventory(Transform item)
        {
            // TODO: Check for stack in stackable items
            string itemID = item.GetComponent<FVRPhysicalObject>().ObjectWrapper.ItemID;
            if (baseInventory.ContainsKey(itemID))
            {
                baseInventory[itemID] += 1;
                baseInventoryObjects[itemID].Add(item.gameObject);
            }
            else
            {
                baseInventory.Add(itemID, 1);
                baseInventoryObjects.Add(itemID, new List<GameObject> { item.gameObject });
            }

            // Check for more items that may be contained inside this one
            EFM_CustomItemWrapper customItemWrapper = item.GetComponent<EFM_CustomItemWrapper>();
            if(customItemWrapper != null && customItemWrapper.itemObjectsRoot != null)
            {
                foreach (Transform innerItem in customItemWrapper.itemObjectsRoot)
                {
                    AddToBaseInventory(innerItem);
                }
            }
        }

        private GameObject LoadSavedItem(Transform parent, JToken item)
        {
            Mod.instance.LogInfo("Loading item "+item["PhysicalObject"]["ObjectWrapper"]["ItemID"] +", on parent "+parent.name);
            int parsedID = -1;
            GameObject prefabToUse = null;
            if (int.TryParse(item["PhysicalObject"]["ObjectWrapper"]["ItemID"].ToString(), out parsedID))
            {
                // Custom item, fetch from our own assets
                prefabToUse = Mod.itemPrefabs[parsedID];
            }
            else
            {
                // Vanilla item, fetch from game assets
                prefabToUse = IM.OD[item["PhysicalObject"]["ObjectWrapper"]["ItemID"].ToString()].GetGameObject();
            }

            GameObject itemObject = Instantiate<GameObject>(prefabToUse, parent);

            FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;
            Mod.instance.LogInfo("physical object null?: "+(itemPhysicalObject == null)+", itemObjectWrapper null?: "+(itemObjectWrapper == null));

            // Fill data

            // PhysicalObject
            itemPhysicalObject.m_isSpawnLock = (bool)item["PhysicalObject"]["m_isSpawnLock"];
            itemPhysicalObject.m_isHardnessed = (bool)item["PhysicalObject"]["m_isHarnessed"];
            itemPhysicalObject.IsKinematicLocked = (bool)item["PhysicalObject"]["IsKinematicLocked"];
            itemPhysicalObject.IsInWater = (bool)item["PhysicalObject"]["IsInWater"];
            AddAttachments(itemPhysicalObject, item["PhysicalObject"]);
            if ((int)item["PhysicalObject"]["heldMode"] != 0)
            {
                FVRViveHand hand = ((int)item["PhysicalObject"]["heldMode"] == 1 ? GM.CurrentPlayerBody.RightHand : GM.CurrentPlayerBody.LeftHand).GetComponentInChildren<FVRViveHand>();
                hand.CurrentInteractable = itemPhysicalObject;
                FieldInfo handStateField = typeof(FVRViveHand).GetField("m_state", BindingFlags.NonPublic | BindingFlags.Instance);
                handStateField.SetValue(hand, FVRViveHand.HandState.GripInteracting);
                hand.CurrentInteractable.BeginInteraction(hand);
            }
            Mod.instance.LogInfo("Filled physical object data");

            // ObjectWrapper
            itemObjectWrapper.ItemID = item["PhysicalObject"]["ObjectWrapper"]["ItemID"].ToString();

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                FVRFireArm firearmPhysicalObject = itemPhysicalObject as FVRFireArm;
                Mod.instance.LogInfo("loading firearm " + firearmPhysicalObject.name);

                // Build and load flagDict from saved lists
                if (item["PhysicalObject"]["flagDictKeys"] != null)
                {
                    JObject loadedFlagDict = (JObject)item["PhysicalObject"]["flagDictKeys"];
                    Dictionary<string, string> flagDict = new Dictionary<string, string>();
                    flagDict = loadedFlagDict.ToObject<Dictionary<string, string>>();
                    firearmPhysicalObject.ConfigureFromFlagDic(flagDict);
                }

                // Chambers
                List<FireArmRoundClass> newLoadedRoundsInChambers = new List<FireArmRoundClass>();
                if (item["PhysicalObject"]["loadedRoundsInChambers"] != null && ((JArray)item["PhysicalObject"]["loadedRoundsInChambers"]).Count > 0)
                {
                    JArray loadedLRIC = ((JArray)item["PhysicalObject"]["loadedRoundsInChambers"]);
                    foreach (int round in loadedLRIC)
                    {
                        newLoadedRoundsInChambers.Add((FireArmRoundClass)round);
                    }
                    firearmPhysicalObject.SetLoadedChambers(newLoadedRoundsInChambers);
                }

                // Magazine/Clip
                if (item["PhysicalObject"]["ammoContainer"] != null)
                {
                    int parsedContainerID = -1;
                    GameObject containerPrefabToUse = null;
                    if (int.TryParse(item["PhysicalObject"]["ammoContainer"]["itemID"].ToString(), out parsedContainerID))
                    {
                        // Custom mag, fetch from our own assets
                        containerPrefabToUse = Mod.itemPrefabs[parsedContainerID];
                    }
                    else
                    {
                        // Vanilla mag, fetch from game assets
                        containerPrefabToUse = IM.OD[item["PhysicalObject"]["ammoContainer"]["itemID"].ToString()].GetGameObject();
                    }

                    GameObject containerObject = Instantiate<GameObject>(containerPrefabToUse);
                    FVRPhysicalObject containerPhysicalObject = containerObject.GetComponentInChildren<FVRPhysicalObject>();

                    if (firearmPhysicalObject.UsesClips && containerPhysicalObject is FVRFireArmClip)
                    {
                        Transform gunClipTransform = firearmPhysicalObject.ClipMountPos;

                        containerObject.transform.position = gunClipTransform.position;
                        containerObject.transform.rotation = gunClipTransform.rotation;

                        FVRFireArmClip clipPhysicalObject = containerPhysicalObject as FVRFireArmClip;

                        if (item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"] != null)
                        {
                            List<FireArmRoundClass> newLoadedRoundsInClip = new List<FireArmRoundClass>();
                            foreach (int round in item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"])
                            {
                                newLoadedRoundsInClip.Add((FireArmRoundClass)round);
                            }
                            clipPhysicalObject.ReloadClipWithList(newLoadedRoundsInClip);
                        }
                        else
                        {
                            while (clipPhysicalObject.m_numRounds > 0)
                            {
                                clipPhysicalObject.RemoveRound();
                            }
                        }

                        clipPhysicalObject.Load(firearmPhysicalObject);
                        clipPhysicalObject.IsInfinite = false;
                    }
                    else if (containerPhysicalObject is FVRFireArmMagazine)
                    {
                        Mod.instance.LogInfo("\tFirearm has mag");
                        FVRFireArmMagazine magPhysicalObject = containerPhysicalObject as FVRFireArmMagazine;
                        Mod.instance.LogInfo("\tis mag null?: " + (magPhysicalObject == null));
                        Transform gunMagTransform = firearmPhysicalObject.GetMagMountPos(magPhysicalObject.IsBeltBox);
                        Mod.instance.LogInfo("\tgunMagTransform null?: " + (gunMagTransform == null));

                        containerObject.transform.position = gunMagTransform.position;
                        containerObject.transform.rotation = gunMagTransform.rotation;

                        if (item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"] != null)
                        {
                            Mod.instance.LogInfo("\t\tmag has rounds list");
                            List<FireArmRoundClass> newLoadedRoundsInMag = new List<FireArmRoundClass>();
                            foreach (int round in item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"])
                            {
                                newLoadedRoundsInMag.Add((FireArmRoundClass)round);
                            }
                            magPhysicalObject.ReloadMagWithList(newLoadedRoundsInMag);
                        }
                        else
                        {
                            Mod.instance.LogInfo("\t\tmag has no rounds list, removing all default rounds");
                            while (magPhysicalObject.m_numRounds > 0)
                            {
                                magPhysicalObject.RemoveRound();
                            }
                        }

                        magPhysicalObject.Load(firearmPhysicalObject);
                        magPhysicalObject.IsInfinite = false;
                    }
                }
            }
            else if (itemPhysicalObject is FVRFireArmMagazine)
            {
                FVRFireArmMagazine magPhysicalObject = (itemPhysicalObject as FVRFireArmMagazine);

                if (item["PhysicalObject"]["loadedRoundsInContainer"] != null)
                {
                    List<FireArmRoundClass> newLoadedRoundsInMag = new List<FireArmRoundClass>();
                    foreach (int round in item["PhysicalObject"]["loadedRoundsInContainer"])
                    {
                        newLoadedRoundsInMag.Add((FireArmRoundClass)round);
                    }
                    magPhysicalObject.ReloadMagWithList(newLoadedRoundsInMag);
                }
                else
                {
                    while (magPhysicalObject.m_numRounds > 0)
                    {
                        magPhysicalObject.RemoveRound();
                    }
                }
            }
            else if (itemPhysicalObject is FVRFireArmClip)
            {
                FVRFireArmClip clipPhysicalObject = (itemPhysicalObject as FVRFireArmClip);

                if (item["PhysicalObject"]["loadedRoundsInContainer"] != null)
                {
                    List<FireArmRoundClass> newLoadedRoundsInClip = new List<FireArmRoundClass>();
                    foreach (int round in item["PhysicalObject"]["loadedRoundsInContainer"])
                    {
                        newLoadedRoundsInClip.Add((FireArmRoundClass)round);
                    }
                    clipPhysicalObject.ReloadClipWithList(newLoadedRoundsInClip);
                }
                else
                {
                    while (clipPhysicalObject.m_numRounds > 0)
                    {
                        clipPhysicalObject.RemoveRound();
                    }
                }
            }
            else if (itemPhysicalObject is Speedloader)
            {
                Speedloader SLPhysicalObject = (itemPhysicalObject as Speedloader);

                if (item["PhysicalObject"]["loadedRoundsInContainer"] != null)
                {
                    JArray loadedRIC = (JArray)item["PhysicalObject"]["loadedRoundsInContainer"];
                    for (int j = 0; j < loadedRIC.Count; ++j)
                    {
                        int currentRound = (int)loadedRIC[j];
                        SpeedloaderChamber currentChamber = SLPhysicalObject.Chambers[j];

                        if (currentRound > 0)
                        {
                            currentChamber.Load((FireArmRoundClass)currentRound);
                        }
                        else if (currentRound == -1)
                        {
                            currentChamber.Unload();
                        }
                        else // Loaded spent
                        {
                            currentChamber.LoadEmpty((FireArmRoundClass)(currentRound * -1 - 2));
                        }
                    }
                }
                else
                {
                    foreach (SpeedloaderChamber chamber in SLPhysicalObject.Chambers)
                    {
                        chamber.Unload();
                    }
                }
            }

            Mod.instance.LogInfo("Processed firearm");

            // Custom item
            EFM_CustomItemWrapper customItemWrapper = itemObject.GetComponent<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                customItemWrapper.itemType = (Mod.ItemType)(int)item["itemType"];
                customItemWrapper.amount = (int)item["amount"];
                customItemWrapper.looted = (bool)item["looted"];
                Mod.instance.LogInfo("Has custom item wrapper with type: "+((Mod.ItemType)(int)item["itemType"]));

                // Armor
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    Mod.instance.LogInfo("is armor");
                    customItemWrapper.armor = (float)item["PhysicalObject"]["armor"];
                    customItemWrapper.maxArmor = (float)item["PhysicalObject"]["maxArmor"];
                }

                // Rig
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    Mod.instance.LogInfo("is rig");
                    if (item["PhysicalObject"]["quickBeltSlotContents"] != null)
                    {
                        JArray loadedQBContents = (JArray)item["PhysicalObject"]["quickBeltSlotContents"];
                        for (int j = 0; j < loadedQBContents.Count; ++j)
                        {
                            if (loadedQBContents[j] == null)
                            {
                                customItemWrapper.itemsInSlots[j] = null;
                            }
                            else
                            {
                                customItemWrapper.itemsInSlots[j] = LoadSavedItem(customItemWrapper.itemObjectsRoot, loadedQBContents[j]);
                            }
                        }
                    }
                }

                // Backpack
                if (customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.instance.LogInfo("is backpack");
                    if (item["PhysicalObject"]["backpackContents"] != null)
                    {
                        JArray loadedBPContents = (JArray)item["PhysicalObject"]["backpackContents"];
                        for (int j = 0; j < loadedBPContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.itemObjectsRoot, loadedBPContents[j]);
                        }
                    }
                }

                // Container
                if (customItemWrapper.itemType == Mod.ItemType.Container)
                {
                    Mod.instance.LogInfo("is container");
                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedContainerContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedContainerContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.itemObjectsRoot, loadedContainerContents[j]);
                        }
                    }
                }

                // Pouch
                if (customItemWrapper.itemType == Mod.ItemType.Pouch)
                {
                    Mod.instance.LogInfo("is Pouch");
                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedPouchContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedPouchContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.itemObjectsRoot, loadedPouchContents[j]);
                        }
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                //{
                //    Mod.instance.LogInfo("is ammo box");
                //}

                // Money
                if (customItemWrapper.itemType == Mod.ItemType.Money)
                {
                    Mod.instance.LogInfo("is money");

                    customItemWrapper.stack = (int)item["stack"];
                    customItemWrapper.UpdateStackModel();
                }

                // Consumable
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.instance.LogInfo("is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.instance.LogInfo("is Consumable");
                //}

                // Equip the item if it has an equip slot
                if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                {
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[(int)item["PhysicalObject"]["equipSlot"]];
                    itemPhysicalObject.SetQuickBeltSlot(equipSlot);
                    itemPhysicalObject.SetParentage(equipSlot.QuickbeltRoot);

                    for(int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if (customItemWrapper.itemsInSlots[i] != null)
                        {
                            FVRPhysicalObject currentItemPhysObj = customItemWrapper.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                            if (currentItemPhysObj != null)
                            {
                                // Attach item to quick slot
                                FVRQuickBeltSlot quickBeltSlot = GM.CurrentPlayerBody.QuickbeltSlots[i];
                                currentItemPhysObj.SetQuickBeltSlot(quickBeltSlot);
                                currentItemPhysObj.SetParentage(quickBeltSlot.QuickbeltRoot);
                            }
                        }
                    }
                }
            }

            // Vanilla item
            //EFM_VanillaItemDescriptor vanillaItemDescriptor = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
            //if(vanillaItemDescriptor != null)
            //{
            //    vanillaItemDescriptor.looted = (bool)item["looted"];
            //}

            // GameObject
            itemObject.transform.localPosition = new Vector3((float)item["PhysicalObject"]["positionX"], (float)item["PhysicalObject"]["positionY"], (float)item["PhysicalObject"]["positionZ"]);
            itemObject.transform.localRotation = Quaternion.Euler(new Vector3((float)item["PhysicalObject"]["rotationX"], (float)item["PhysicalObject"]["rotationY"], (float)item["PhysicalObject"]["rotationZ"]));

            return itemObject;
        }

        private void AddAttachments(FVRPhysicalObject physicalObject, JToken loadedPhysicalObject)
        {
            if(loadedPhysicalObject["AttachmentsList"] == null)
            {
                return;
            }

            Transform root = physicalObject.transform;
            JArray loadedAttachmentsList = ((JArray)loadedPhysicalObject["AttachmentsList"]);
            for (int i = 0; i < loadedAttachmentsList.Count; ++i)
            {
                JToken currentPhysicalObject = loadedAttachmentsList[i];
                int parsedID = -1;
                GameObject prefabToUse = null;
                if (int.TryParse(currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString(), out parsedID))
                {
                    // Custom item, fetch from our own assets
                    prefabToUse = Mod.itemPrefabs[parsedID];
                }
                else
                {
                    // Vanilla item, fetch from game assets
                    prefabToUse = IM.OD[currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString()].GetGameObject();
                }

                GameObject itemObject = Instantiate<GameObject>(prefabToUse, root);

                FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
                FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;

                // Fill data
                // GameObject
                itemObject.transform.localPosition = new Vector3((float)currentPhysicalObject["positionX"], (float)currentPhysicalObject["positionY"], (float)currentPhysicalObject["positionZ"]);
                itemObject.transform.localRotation = Quaternion.Euler(new Vector3((float)currentPhysicalObject["rotationX"], (float)currentPhysicalObject["rotationY"], (float)currentPhysicalObject["rotationZ"]));

                // PhysicalObject
                itemPhysicalObject.m_isSpawnLock = (bool)currentPhysicalObject["m_isSpawnLock"];
                itemPhysicalObject.m_isHardnessed = (bool)currentPhysicalObject["m_isHarnessed"];
                itemPhysicalObject.IsKinematicLocked = (bool)currentPhysicalObject["IsKinematicLocked"];
                itemPhysicalObject.IsInWater = (bool)currentPhysicalObject["IsInWater"];
                AddAttachments(itemPhysicalObject, currentPhysicalObject);
                FVRFireArmAttachment itemAttachment = itemPhysicalObject as FVRFireArmAttachment;
                itemAttachment.AttachToMount(physicalObject.AttachmentMounts[(int)currentPhysicalObject["mountIndex"]], false);
                if(itemAttachment is Suppressor)
                {
                    (itemAttachment as Suppressor).AutoMountWell();
                }
                
                // ObjectWrapper
                itemObjectWrapper.ItemID = currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString();
            }
        }

        public override void InitUI()
        {
            // Main hideout menu
            buttons = new Button[8][];
            buttons[0] = new Button[4];
            buttons[1] = new Button[7];
            buttons[2] = new Button[6];
            buttons[3] = new Button[3];
            buttons[4] = new Button[2];
            buttons[5] = new Button[3];
            buttons[6] = new Button[2];
            buttons[7] = new Button[1];

            // Fetch buttons
            canvas = transform.GetChild(0).GetChild(0);
            buttons[0][0] = canvas.GetChild(0).GetChild(1).GetComponent<Button>(); // Raid
            buttons[0][1] = canvas.GetChild(0).GetChild(2).GetComponent<Button>(); // Save
            buttons[0][2] = canvas.GetChild(0).GetChild(3).GetComponent<Button>(); // Load
            buttons[0][3] = canvas.GetChild(0).GetChild(4).GetComponent<Button>(); // Base Back

            buttons[1][0] = canvas.GetChild(1).GetChild(1).GetComponent<Button>(); // Load Slot 0
            buttons[1][1] = canvas.GetChild(1).GetChild(2).GetComponent<Button>(); // Load Slot 1
            buttons[1][2] = canvas.GetChild(1).GetChild(3).GetComponent<Button>(); // Load Slot 2
            buttons[1][3] = canvas.GetChild(1).GetChild(4).GetComponent<Button>(); // Load Slot 3
            buttons[1][4] = canvas.GetChild(1).GetChild(5).GetComponent<Button>(); // Load Slot 4
            buttons[1][5] = canvas.GetChild(1).GetChild(6).GetComponent<Button>(); // Load Auto save
            buttons[1][6] = canvas.GetChild(1).GetChild(7).GetComponent<Button>(); // Load Back

            buttons[2][0] = canvas.GetChild(2).GetChild(1).GetComponent<Button>(); // Save Slot 0
            buttons[2][1] = canvas.GetChild(2).GetChild(2).GetComponent<Button>(); // Save Slot 1
            buttons[2][2] = canvas.GetChild(2).GetChild(3).GetComponent<Button>(); // Save Slot 2
            buttons[2][3] = canvas.GetChild(2).GetChild(4).GetComponent<Button>(); // Save Slot 3
            buttons[2][4] = canvas.GetChild(2).GetChild(5).GetComponent<Button>(); // Save Slot 4
            buttons[2][5] = canvas.GetChild(2).GetChild(6).GetComponent<Button>(); // Save Back

            buttons[3][0] = canvas.GetChild(3).GetChild(1).GetComponent<Button>(); // Char Main
            buttons[3][1] = canvas.GetChild(3).GetChild(2).GetComponent<Button>(); // Char Raider
            buttons[3][2] = canvas.GetChild(3).GetChild(3).GetComponent<Button>(); // Char Back

            buttons[4][0] = canvas.GetChild(4).GetChild(1).GetComponent<Button>(); // Map Mansion
            buttons[4][1] = canvas.GetChild(4).GetChild(2).GetComponent<Button>(); // Map Back

            buttons[5][0] = canvas.GetChild(5).GetChild(1).GetComponent<Button>(); // Time 0
            buttons[5][1] = canvas.GetChild(5).GetChild(2).GetComponent<Button>(); // Time 1
            buttons[5][2] = canvas.GetChild(5).GetChild(3).GetComponent<Button>(); // Time Back

            buttons[6][0] = canvas.GetChild(6).GetChild(1).GetComponent<Button>(); // Confirm
            buttons[6][1] = canvas.GetChild(6).GetChild(2).GetComponent<Button>(); // Confirm Back

            buttons[7][0] = canvas.GetChild(7).GetChild(1).GetComponent<Button>(); // Loading Cancel

            // Fetch audio sources
            AudioSource hoverAudio = canvas.transform.GetChild(10).GetComponent<AudioSource>();
            clickAudio = canvas.transform.GetChild(11).GetComponent<AudioSource>();

            // Create an FVRPointableButton for each button
            for (int i = 0; i < buttons.Length; ++i)
            {
                for (int j = 0; j < buttons[i].Length; ++j)
                {
                    EFM_PointableButton pointableButton = buttons[i][j].gameObject.AddComponent<EFM_PointableButton>();

                    pointableButton.SetButton();
                    pointableButton.MaxPointingRange = 5;
                    pointableButton.hoverGraphics = new GameObject[2];
                    pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                    pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                    pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                    pointableButton.toggleTextColor = true;
                    pointableButton.hoverSound = hoverAudio;
                }
            }

            // Set OnClick for each button
            buttons[0][0].onClick.AddListener(OnRaidClicked);
            buttons[0][1].onClick.AddListener(OnSaveClicked);
            buttons[0][2].onClick.AddListener(OnLoadClicked);
            buttons[0][3].onClick.AddListener(() => { OnBackClicked(0); });

            buttons[1][0].onClick.AddListener(() => { OnLoadSlotClicked(0); });
            buttons[1][1].onClick.AddListener(() => { OnLoadSlotClicked(1); });
            buttons[1][2].onClick.AddListener(() => { OnLoadSlotClicked(2); });
            buttons[1][3].onClick.AddListener(() => { OnLoadSlotClicked(3); });
            buttons[1][4].onClick.AddListener(() => { OnLoadSlotClicked(4); });
            buttons[1][5].onClick.AddListener(() => { OnLoadSlotClicked(5); });
            buttons[1][6].onClick.AddListener(() => { OnBackClicked(1); });

            buttons[2][0].onClick.AddListener(() => { OnSaveSlotClicked(0); });
            buttons[2][1].onClick.AddListener(() => { OnSaveSlotClicked(1); });
            buttons[2][2].onClick.AddListener(() => { OnSaveSlotClicked(2); });
            buttons[2][3].onClick.AddListener(() => { OnSaveSlotClicked(3); });
            buttons[2][4].onClick.AddListener(() => { OnSaveSlotClicked(4); });
            buttons[2][5].onClick.AddListener(() => { OnBackClicked(2); });

            buttons[3][0].onClick.AddListener(() => { OnCharClicked(0); });
            buttons[3][1].onClick.AddListener(() => { OnCharClicked(1); });
            buttons[3][2].onClick.AddListener(() => { OnBackClicked(3); });

            buttons[4][0].onClick.AddListener(() => { OnMapClicked(0); });
            buttons[4][1].onClick.AddListener(() => { OnBackClicked(4); });

            buttons[5][0].onClick.AddListener(() => { OnTimeClicked(0); });
            buttons[5][1].onClick.AddListener(() => { OnTimeClicked(1); });
            buttons[5][2].onClick.AddListener(() => { OnBackClicked(5); });

            buttons[6][0].onClick.AddListener(OnConfirmRaidClicked);
            buttons[6][1].onClick.AddListener(() => { OnBackClicked(6); });

            buttons[7][0].onClick.AddListener(OnCancelRaidLoadClicked);

            // Set buttons activated depending on presence of save files
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 5; ++i)
                {
                    buttons[1][i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
            }
            else
            {
                buttons[0][2].gameObject.SetActive(false);
            }

            // Keep references we need
            raidCountdownTitle = canvas.GetChild(7).GetChild(5).GetComponent<Text>();
            raidCountdown = canvas.GetChild(7).GetChild(6).GetComponent<Text>();
            timeChoice0 = canvas.GetChild(5).GetChild(1).GetComponentInChildren<Text>();
            timeChoice1 = canvas.GetChild(5).GetChild(2).GetComponentInChildren<Text>();
            chosenCharacter = canvas.GetChild(6).GetChild(3).GetComponentInChildren<Text>();
            chosenMap = canvas.GetChild(6).GetChild(4).GetComponentInChildren<Text>();
            chosenTime = canvas.GetChild(6).GetChild(5).GetComponentInChildren<Text>();

            // Areas
            if (areaCanvasPrefab == null)
            {
                Mod.instance.LogInfo("Area canvas not initialized, initializing all area UI and prepping...");

                // Load prefabs and assets
                areaCanvasPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaCanvas");
                areaCanvasBottomButtonPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaCanvasBottomButton");
                areaRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaRequirement");
                itemRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("ItemRequirement");
                skillRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("SkillRequirement");
                traderRequirementPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("TraderRequirement");
                areaRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("AreaRequirements");
                itemRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("ItemRequirements");
                skillRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("SkillRequirements");
                traderRequirementsPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("TraderRequirements");
                bonusPrefab = Mod.baseAssetsBundle.LoadAsset<GameObject>("Bonus");
                areaBackgroundNormalSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_default_back");
                areaBackgroundLockedSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_locked_back");
                areaBackgroundAvailableSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_default_back_green");
                areaBackgroundEliteSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("area_icon_elite_back");
                areaStatusIconUpgrading = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_upgrading");
                areaStatusIconConstructing = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_constructing");
                areaStatusIconLocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_lock");
                areaStatusIconUnlocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_unlocked");
                areaStatusIconReadyUpgrade = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_ready_to_upgrade");
                areaStatusIconProducing = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_status_producing");
                areaStatusIconOutOfFuel = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_out_of_fuel");
                requirementFulfilled = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_requirement_fulfilled");
                Mod.instance.LogInfo("requirementFulfilled null?: "+(requirementFulfilled == null));
                requirementLocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_requirement_locked");
                Mod.instance.LogInfo("requirementLocked null?: " + (requirementLocked == null));
                Mod.instance.LogInfo("All asset names:");

                string[] allNames = Mod.baseAssetsBundle.GetAllAssetNames();
                foreach(string name in allNames)
                {
                    Mod.instance.LogInfo(name);
                    if (name.Contains("locked"))
                    {
                        Mod.instance.LogInfo("\tCONTAINS LOCKED");
                    }
                }
                traderAvatars = new Sprite[8];
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_russian_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_therapist_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_fence_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ah_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_peacekeeper_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_tech_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ragman_small");
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_jaeger_small");
                areaIcons = new Sprite[22];
                areaIcons[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_vents");
                areaIcons[1] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_security");
                areaIcons[2] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_watercloset");
                areaIcons[3] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_stash");
                areaIcons[4] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_generators");
                areaIcons[5] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_heating");
                areaIcons[6] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_rain_collector");
                areaIcons[7] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_medstation");
                areaIcons[8] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_kitchen");
                areaIcons[9] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_restplace");
                areaIcons[10] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_workbench");
                areaIcons[11] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_intelligence_center");
                areaIcons[12] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_shooting_range");
                areaIcons[13] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_library");
                areaIcons[14] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_scav_case");
                areaIcons[15] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_illumination");
                areaIcons[16] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_placeoffame");
                areaIcons[17] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_afu");
                areaIcons[18] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_solarpower");
                areaIcons[19] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_boozegen");
                areaIcons[20] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_bitcoinfarm");
                areaIcons[21] = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_christmas_illumination");
                bonusIcons = new Dictionary<string, Sprite>();
                bonusIcons.Add("ExperienceRate", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_exp_small"));
                bonusIcons.Add("HealthRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_medical"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_meds.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_meds"));
                bonusIcons.Add("/files/Hideout/icon_hideout_videocardslots.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_videocardslots"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_bitcoin.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_bitcoin"));
                bonusIcons.Add("/files/Hideout/icon_hideout_unlocked.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_unlocked"));
                bonusIcons.Add("FuelConsumption", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_fuelconsumption"));
                bonusIcons.Add("/files/Hideout/icon_hideout_fuelslots.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_fuelslots"));
                bonusIcons.Add("EnergyRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_energy"));
                bonusIcons.Add("HydrationRegeneration", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_hydration"));
                bonusIcons.Add("/files/Hideout/icon_hideout_shootingrangeunlock.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_shootingrangeunlock"));
                bonusIcons.Add("DebuffEndDelay", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
                bonusIcons.Add("UnlockWeaponModification", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_weaponmodunlock"));
                bonusIcons.Add("SkillGroupLevelingBoost", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_skillboost"));
                bonusIcons.Add("AdditionalSlots", Mod.baseAssetsBundle.LoadAsset<Sprite>("skills_grid_icon"));
                bonusIcons.Add("StashSize", Mod.baseAssetsBundle.LoadAsset<Sprite>("skills_grid_icon"));
                bonusIcons.Add("/files/Hideout/icon_hideout_scavitem.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_scavitem"));
                bonusIcons.Add("ScavCooldownTimer", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_time"));
                bonusIcons.Add("InsuranceReturnTime", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_time"));
                bonusIcons.Add("QuestMoneyReward", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money"));
                bonusIcons.Add("RagfairCommission", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money"));
                bonusIcons.Add("/files/Hideout/icon_hideout_createitem_generic.png", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_createitem_generic"));
                bonusIcons.Add("MaximumEnergyReserve", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_hideout_batterycharge"));
                skillIcons = new Sprite[64];
                skillIcons[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_endurance");
                skillIcons[1] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_strength");
                skillIcons[2] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_vitality");
                skillIcons[3] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_health");
                skillIcons[4] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_stressresistance");
                skillIcons[5] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_metabolism");
                skillIcons[6] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_physical_immunity");
                skillIcons[7] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_perception");
                skillIcons[8] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_intellect");
                skillIcons[9] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_attention");
                skillIcons[10] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_charisma");
                skillIcons[11] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_mental_memory");
                skillIcons[12] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_pistols");
                skillIcons[13] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_revolvers");
                skillIcons[14] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_smgs");
                skillIcons[15] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_assaultrifles");
                skillIcons[16] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_shotguns");
                skillIcons[17] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_sniperrifles");
                skillIcons[18] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_lmgs");
                skillIcons[19] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_hmgs");
                skillIcons[20] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_launchers");
                skillIcons[21] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_ugls");
                skillIcons[22] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_grenades");
                skillIcons[23] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_melee");
                skillIcons[24] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_dmrs");
                skillIcons[25] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_recoilcontrol");
                skillIcons[26] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_weapondrawing");
                skillIcons[27] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_troubleshooting");
                skillIcons[28] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_surgery");
                skillIcons[29] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_covertmovement");
                skillIcons[30] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_search");
                skillIcons[31] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_magdrills");
                skillIcons[32] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_sniping");
                skillIcons[33] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_pronemovement");
                skillIcons[34] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_fieldmedical");
                skillIcons[35] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_basicmedical");
                skillIcons[36] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_lightarmor");
                skillIcons[37] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_heavyarmor");
                skillIcons[38] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_basicweaponmodding");
                skillIcons[39] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_advancedweaponmodding");
                skillIcons[40] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_nightoperations");
                skillIcons[41] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_silentoperations");
                skillIcons[42] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_lockpicking");
                skillIcons[43] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_weapontreatment");
                skillIcons[44] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_freetrading");
                skillIcons[45] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_auctions");
                skillIcons[46] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_cleanoperations");
                skillIcons[47] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_barter");
                skillIcons[48] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_shadowconnections");
                skillIcons[49] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_taskperformance");
                skillIcons[50] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_crafting");
                skillIcons[51] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_hideoutmanagement");
                skillIcons[52] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_combat_weaponswitch");
                skillIcons[53] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_practical_equipmentmanagement");
                skillIcons[54] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_bear_aksystems");
                skillIcons[55] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_bear_assaultoperations");
                skillIcons[56] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_bear_authority");
                skillIcons[57] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_bear_heavycaliber");
                skillIcons[58] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_bear_rawpower");
                skillIcons[59] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_usec_arsystems");
                skillIcons[60] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_usec_deepweaponmodding");
                skillIcons[61] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_usec_longrangeoptics");
                skillIcons[62] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_usec_negotiations");
                skillIcons[63] = Mod.baseAssetsBundle.LoadAsset<Sprite>("skill_special_usec_tactics");

                // Prep prefabs
                Mod.instance.LogInfo("All area UI loaded, prepping...");

                // AreaCanvasPrefab
                GameObject summaryButtonObject = areaCanvasPrefab.transform.GetChild(0).GetChild(2).gameObject;
                EFM_PointableButton summaryPointableButton = summaryButtonObject.AddComponent<EFM_PointableButton>();
                summaryPointableButton.SetButton();
                summaryPointableButton.MaxPointingRange = 30;
                summaryPointableButton.hoverGraphics = new GameObject[2];
                summaryPointableButton.hoverGraphics[0] = summaryButtonObject.transform.GetChild(0).gameObject;
                summaryPointableButton.hoverGraphics[1] = areaCanvasPrefab.transform.GetChild(0).GetChild(1).gameObject;
                summaryPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                GameObject fullCloseButtonObject = areaCanvasPrefab.transform.GetChild(1).GetChild(0).GetChild(1).gameObject;
                EFM_PointableButton fullClosePointableButton = fullCloseButtonObject.AddComponent<EFM_PointableButton>();
                fullClosePointableButton.SetButton();
                fullClosePointableButton.MaxPointingRange = 30;
                fullClosePointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                GameObject middleHoverScrollUpObject = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject;
                GameObject middleHoverScrollDownObject = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).gameObject;
                EFM_HoverScroll middleHoverScrollUp = middleHoverScrollUpObject.AddComponent<EFM_HoverScroll>();
                EFM_HoverScroll middleHoverScrollDown = middleHoverScrollDownObject.AddComponent<EFM_HoverScroll>();
                middleHoverScrollUp.MaxPointingRange = 30;
                middleHoverScrollUp.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                middleHoverScrollUp.scrollbar = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
                middleHoverScrollUp.other = middleHoverScrollDown;
                middleHoverScrollUp.up = true;
                middleHoverScrollUp.rate = 0.5f;
                middleHoverScrollDown.MaxPointingRange = 30;
                middleHoverScrollDown.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                middleHoverScrollDown.scrollbar = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
                middleHoverScrollDown.other = middleHoverScrollUp;
                middleHoverScrollDown.rate = 0.5f;

                GameObject middle2HoverScrollUpObject = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject;
                GameObject middle2HoverScrollDownObject = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(3).gameObject;
                EFM_HoverScroll middle2HoverScrollUp = middle2HoverScrollUpObject.AddComponent<EFM_HoverScroll>();
                EFM_HoverScroll middle2HoverScrollDown = middle2HoverScrollDownObject.AddComponent<EFM_HoverScroll>();
                middle2HoverScrollUp.MaxPointingRange = 30;
                middle2HoverScrollUp.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                middle2HoverScrollUp.scrollbar = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
                middle2HoverScrollUp.other = middle2HoverScrollDown;
                middle2HoverScrollUp.up = true;
                middle2HoverScrollDown.MaxPointingRange = 30;
                middle2HoverScrollDown.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                middle2HoverScrollDown.scrollbar = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetComponent<Scrollbar>();
                middle2HoverScrollDown.other = middle2HoverScrollUp;

                // AreaCanvasBottomButtonPrefab
                EFM_PointableButton bottomPointableButton = areaCanvasBottomButtonPrefab.AddComponent<EFM_PointableButton>();
                bottomPointableButton.SetButton();
                bottomPointableButton.MaxPointingRange = 30;
                bottomPointableButton.hoverGraphics = new GameObject[1];
                bottomPointableButton.hoverGraphics[0] = areaCanvasBottomButtonPrefab.transform.GetChild(0).gameObject;
                bottomPointableButton.buttonText = areaCanvasBottomButtonPrefab.transform.GetChild(1).GetComponent<Text>();
                bottomPointableButton.toggleTextColor = true;

                Mod.instance.LogInfo("Area UI prepped");
            }

            // Unload base assets bundle without unloading loaded assets, if necessary
            if (Mod.baseAssetsBundle != null)
            {
                Mod.baseAssetsBundle.Unload(false);
            }

            Mod.instance.LogInfo("Initializing area managers");
            // Init all area managers after UI is prepped
            for (int i = 0; i < 22; ++i)
            {
                baseAreaManagers[i].Init();
            }

            if (Mod.justFinishedRaid)
            {
                // TODO: Display raid results on UI
            }
        }

        public long GetTimeSeconds()
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - epoch).TotalSeconds);
        }

        private void InitTime()
        {
            long longTime = GetTimeSeconds();
            long clampedTime = longTime % 86400; // Clamp to 24 hours because thats the relevant range
            int scaledTime = (int)((clampedTime * EFM_Manager.meatovTimeMultiplier) % 86400);
            time = scaledTime;
        }

        public void OnSaveClicked()
        {
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(2).gameObject.SetActive(true);
            clickAudio.Play();
        }

        public void OnSaveSlotClicked(int slotIndex)
        {
            clickAudio.Play();
            Mod.saveSlotIndex = slotIndex;
            SaveBase();
        }

        public void OnLoadClicked()
        {
            clickAudio.Play();
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(1).gameObject.SetActive(true);
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            clickAudio.Play();
            ResetPlayerRig();
            LoadBase(slotIndex);
        }

        public void OnRaidClicked()
        {
            clickAudio.Play();
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(3).gameObject.SetActive(true);
        }

        public void OnBackClicked(int backIndex)
        {
            clickAudio.Play();
            switch (backIndex)
            {
                case 0:
                    SteamVR_LoadLevel.Begin("MeatovMenuScene", false, 0.5f, 0f, 0f, 0f, 1f);
                    break;
                case 1:
                case 2:
                case 3:
                    canvas.GetChild(backIndex).gameObject.SetActive(false);
                    canvas.GetChild(0).gameObject.SetActive(true);
                    break;
                case 4:
                case 5:
                case 6:
                    canvas.GetChild(backIndex).gameObject.SetActive(false);
                    canvas.GetChild(backIndex - 1).gameObject.SetActive(true);
                    break;
                default:
                    break;
            }
        }

        public void OnCharClicked(int charIndex)
        {
            clickAudio.Play();

            chosenCharIndex = charIndex;

            // Update chosen char text
            chosenCharacter.text = charIndex == 0 ? "Raider" : "Scavenger";

            canvas.GetChild(3).gameObject.SetActive(false);
            canvas.GetChild(4).gameObject.SetActive(true);
        }

        public void OnMapClicked(int mapIndex)
        {
            clickAudio.Play();

            chosenMapIndex = mapIndex;

            // Update chosen map text
            switch (chosenMapIndex)
            {
                case 0:
                    chosenMap.text = "Factory";
                    break;
                default:
                    break;
            }

            canvas.GetChild(4).gameObject.SetActive(false);
            canvas.GetChild(5).gameObject.SetActive(true);
        }

        public void OnTimeClicked(int timeIndex)
        {
            clickAudio.Play();

            chosenTimeIndex = timeIndex;
            Mod.chosenTimeIndex = chosenTimeIndex;
            canvas.GetChild(5).gameObject.SetActive(false);
            canvas.GetChild(6).gameObject.SetActive(true);
        }

        public void OnConfirmRaidClicked()
        {
            clickAudio.Play();

            canvas.GetChild(6).gameObject.SetActive(false);
            canvas.GetChild(7).gameObject.SetActive(true);

            // Begin loading raid map
            switch (chosenMapIndex)
            {
                case 0:
                    loadingRaid = true;
                    currentRaidBundleRequest = AssetBundle.LoadFromFileAsync("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovFactory.ab");
                    break;
                default:
                    loadingRaid = true;
                    chosenMapIndex = 0;
                    chosenCharIndex = 0;
                    chosenTimeIndex = 0;
                    chosenMap.text = "Factory";
                    currentRaidBundleRequest = AssetBundle.LoadFromFileAsync("BepinEx/Plugins/EscapeFromMeatov/EscapeFromMeatovFactory.ab");
                    break;
            }
        }

        public void OnCancelRaidLoadClicked()
        {
            clickAudio.Play();

            cancelRaidLoad = true;
        }

        private void ResetPlayerRig()
        {
            // Destroy and reset rig and equipment slots
            EFM_EquipmentSlot.Clear();
            for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
            {
                if (Mod.equipmentSlots[i] != null)
                {
                    // Unlike normal rig slots, items in equip slots are parented to their slots, so the destruction of slot will destroy the item
                    Destroy(Mod.equipmentSlots[i].gameObject);
                }
            }
            Mod.equipmentSlots.Clear();
            GM.CurrentPlayerBody.ConfigureQuickbelt(-2); // -2 in order to detroy the objects on belt as well
        }

        private void SaveBase()
        {
            JToken saveObject = data;

            // Write time
            saveObject["time"] = GetTimeSeconds();

            // Write player status
            saveObject["health"] = JArray.FromObject(Mod.health);
            saveObject["hydration"] = Mod.hydration;
            saveObject["maxHydration"] = Mod.maxHydration;
            saveObject["energy"] = Mod.energy;
            saveObject["maxEnergy"] = Mod.maxEnergy;
            saveObject["stamina"] = Mod.stamina;
            saveObject["maxStamina"] = Mod.maxStamina;
            saveObject["level"] = Mod.level;

            // Write skills
            saveObject["skills"] = new JArray();
            for(int i=0; i < 64; ++i)
            {
                ((JArray)saveObject["skills"]).Add(new JObject());
                saveObject["skills"][i]["progress"] = Mod.skills[i].progress;
                saveObject["skills"][i]["currentProgress"] = Mod.skills[i].currentProgress;
            }

            // Write areas
            JArray savedAreas = new JArray();
            saveObject["areas"] = savedAreas;
            for (int i = 0; i < baseAreaManagers.Count; ++i)
            {

                JToken currentSavedArea = new JObject();
                currentSavedArea["level"] = baseAreaManagers[i].level;
                currentSavedArea["constructing"] = baseAreaManagers[i].constructing;
                currentSavedArea["constructTime"] = baseAreaManagers[i].constructTime;
                if(baseAreaManagers[i].slotItems != null)
                {
                    JArray slots = new JArray();
                    currentSavedArea["slots"] = slots;
                    foreach(GameObject slotItem in baseAreaManagers[i].slotItems)
                    {
                        if(slotItem == null)
                        {
                            slots.Add(null);
                        }
                        else
                        {
                            SaveItem(slots, slotItem.transform);
                        }
                    }
                }
                savedAreas.Add(currentSavedArea);
            }

            // Save trader statuses
            JArray savedTraderStatuses = new JArray();
            saveObject["traderStatuses"] = savedTraderStatuses;
            for(int i=0; i<8; ++i)
            {
                JToken currentSavedTraderStatus = new JObject();
                currentSavedTraderStatus["id"] = traderStatuses[i].id;
                currentSavedTraderStatus["salesSum"] = traderStatuses[i].salesSum;
                currentSavedTraderStatus["standing"] = traderStatuses[i].standing;
                currentSavedTraderStatus["unlocked"] = traderStatuses[i].unlocked;

                savedTraderStatuses.Add(currentSavedTraderStatus);
            }

            // Reset save data item list
            JArray saveItems = new JArray();
            saveObject["items"] = saveItems;

            // Save loose items
            Transform itemsRoot = transform.GetChild(2);

            // Create a SavedItem for each item and fill it with minimum data needed to spawn it on load
            Mod.instance.LogInfo("Saving items on items root");
            for (int i = 0; i < itemsRoot.childCount; ++i)
            {
                SaveItem(saveItems, itemsRoot.GetChild(i));
            }

            // Save items in hands
            FVRViveHand rightHand = GM.CurrentPlayerBody.RightHand.GetComponentInChildren<FVRViveHand>();
            FVRViveHand leftHand = GM.CurrentPlayerBody.LeftHand.GetComponentInChildren<FVRViveHand>();
            if (rightHand.CurrentInteractable != null)
            {
                SaveItem(saveItems, rightHand.CurrentInteractable.transform, rightHand);
            }
            if (leftHand.CurrentInteractable != null)
            {
                SaveItem(saveItems, leftHand.CurrentInteractable.transform, leftHand);
            }

            // Save equipment
            Mod.instance.LogInfo("Saving equipment");
            if (EFM_EquipmentSlot.currentRig != null)
            {
                SaveItem(saveItems, EFM_EquipmentSlot.currentRig.transform);
            }
            if (EFM_EquipmentSlot.currentHelmet != null)
            {
                SaveItem(saveItems, EFM_EquipmentSlot.currentHelmet.transform);
            }
            if (EFM_EquipmentSlot.currentArmor != null && (EFM_EquipmentSlot.currentRig == null || EFM_EquipmentSlot.currentRig.itemType != Mod.ItemType.ArmoredRig))
            {
                SaveItem(saveItems, EFM_EquipmentSlot.currentArmor.transform);
            }
            if (EFM_EquipmentSlot.currentBackpack != null)
            {
                SaveItem(saveItems, EFM_EquipmentSlot.currentBackpack.transform);
            }

            // Replace data
            data = saveObject;

            SaveDataToFile();
        }

        private void SaveItem(JArray listToAddTo, Transform item, FVRViveHand hand = null, int quickBeltSlotIndex = -1)
        {
            if(item == null)
            {
                return;
            }
            Mod.instance.LogInfo("Saving item " + item.name);

            JToken savedItem = new JObject();
            savedItem["PhysicalObject"] = new JObject();
            savedItem["PhysicalObject"]["ObjectWrapper"] = new JObject();

            // Get correct item is held and set heldMode
            FVRPhysicalObject itemPhysicalObject = null;
            if (hand != null)
            {
                Mod.instance.LogInfo("saving hand item: "+item.name);
                if (hand.CurrentInteractable is FVRAlternateGrip)
                {
                    Mod.instance.LogInfo("\tItem is alt grip");
                    // Make sure this item isn't the same as held in right hand because we dont want an item saved twice, and will prioritize right hand
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        Mod.instance.LogInfo("\t\tItem in right hand or different from other hand's item");
                        itemPhysicalObject = (hand.CurrentInteractable as FVRAlternateGrip).PrimaryObject;
                        savedItem["PhysicalObject"]["heldMode"] = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\tItem not right hand or same as other hand's item");
                        itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                        savedItem["PhysicalObject"]["heldMode"] = 0;
                    }
                }
                else
                {
                    Mod.instance.LogInfo("\tItem is not alt grip");
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        Mod.instance.LogInfo("\t\tItem in right hand or different from other hand's item");
                        itemPhysicalObject = hand.CurrentInteractable as FVRPhysicalObject;
                        savedItem["PhysicalObject"]["heldMode"] = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
                        Mod.instance.LogInfo("\t\tItem not right hand or same as other hand's item");
                        itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                        savedItem["PhysicalObject"]["heldMode"] = 0;
                    }
                }
            }
            else
            {
                itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                savedItem["PhysicalObject"]["heldMode"] = 0;
            }
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;

            // Fill PhysicalObject
            savedItem["PhysicalObject"]["positionX"] = item.localPosition.x;
            savedItem["PhysicalObject"]["positionY"] = item.localPosition.y;
            savedItem["PhysicalObject"]["positionZ"] = item.localPosition.z;
            savedItem["PhysicalObject"]["rotationX"] = item.localRotation.eulerAngles.x;
            savedItem["PhysicalObject"]["rotationY"] = item.localRotation.eulerAngles.y;
            savedItem["PhysicalObject"]["rotationZ"] = item.localRotation.eulerAngles.z;
            savedItem["PhysicalObject"]["m_isSpawnLock"] = itemPhysicalObject.m_isSpawnLock;
            savedItem["PhysicalObject"]["m_isHarnessed"] = itemPhysicalObject.m_isHardnessed;
            savedItem["PhysicalObject"]["IsKinematicLocked"] = itemPhysicalObject.IsKinematicLocked;
            savedItem["PhysicalObject"]["IsInWater"] = itemPhysicalObject.IsInWater;
            SaveAttachments(itemPhysicalObject, savedItem["PhysicalObject"]);
            savedItem["PhysicalObject"]["m_quickBeltSlot"] = quickBeltSlotIndex;

            // Fill ObjectWrapper
            savedItem["PhysicalObject"]["ObjectWrapper"]["ItemID"] = itemObjectWrapper.ItemID;

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                FVRFireArm firearmPhysicalObject = itemPhysicalObject as FVRFireArm;

                // Save flagDict by converting it into two lists of string, one for keys and one for values
                Dictionary<string, string> flagDict = firearmPhysicalObject.GetFlagDic();
                if (flagDict != null && flagDict.Count > 0)
                {
                    JToken saveFlagDict = new JObject();
                    savedItem["PhysicalObject"]["flagDict"] = saveFlagDict;
                    foreach (KeyValuePair<string, string> flagDictEntry in flagDict)
                    {
                        saveFlagDict[flagDictEntry.Key] = flagDictEntry.Value;
                    }
                }

                // Chambers
                if (firearmPhysicalObject.GetChamberRoundList() != null && firearmPhysicalObject.GetChamberRoundList().Count > 0)
                {
                    JArray saveLoadedRounds = new JArray();
                    savedItem["PhysicalObject"]["loadedRoundsInChambers"] = saveLoadedRounds;
                    foreach (FireArmRoundClass round in firearmPhysicalObject.GetChamberRoundList())
                    {
                        saveLoadedRounds.Add((int)round);
                    }
                }

                // Magazine/Clip
                if (firearmPhysicalObject.UsesClips)
                {
                    if (firearmPhysicalObject.Clip != null)
                    {
                        savedItem["PhysicalObject"]["ammoContainer"] = new JObject();
                        savedItem["PhysicalObject"]["ammoContainer"]["itemID"] = firearmPhysicalObject.Clip.ObjectWrapper.ItemID;

                        if (firearmPhysicalObject.Clip.HasARound())
                        {
                            JArray newLoadedRoundsInClip = new JArray();
                            foreach (FVRFireArmClip.FVRLoadedRound round in firearmPhysicalObject.Clip.LoadedRounds)
                            {
                                if (round == null)
                                {
                                    break;
                                }
                                else
                                {
                                    newLoadedRoundsInClip.Add((int)round.LR_Class);
                                }
                            }
                            savedItem["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"] = newLoadedRoundsInClip;
                        }
                    }
                }
                else if (firearmPhysicalObject.Magazine != null)
                {
                    savedItem["PhysicalObject"]["ammoContainer"] = new JObject();
                    savedItem["PhysicalObject"]["ammoContainer"]["itemID"] = firearmPhysicalObject.Magazine.ObjectWrapper.ItemID;

                    if (firearmPhysicalObject.Magazine.HasARound() && firearmPhysicalObject.Magazine.LoadedRounds != null)
                    {
                        JArray newLoadedRoundsInMag = new JArray();
                        foreach (FVRLoadedRound round in firearmPhysicalObject.Magazine.LoadedRounds)
                        {
                            if (round == null)
                            {
                                break;
                            }
                            else
                            {
                                newLoadedRoundsInMag.Add((int)round.LR_Class);
                            }
                        }
                        savedItem["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"] = newLoadedRoundsInMag;
                    }
                }
            }
            else if (itemPhysicalObject is FVRFireArmMagazine)
            {
                FVRFireArmMagazine magPhysicalObject = (itemPhysicalObject as FVRFireArmMagazine);
                if (magPhysicalObject.HasARound())
                {
                    JArray newLoadedRoundsInMag = new JArray();
                    foreach (FVRLoadedRound round in magPhysicalObject.LoadedRounds)
                    {
                        if (round == null)
                        {
                            break;
                        }
                        else
                        {
                            newLoadedRoundsInMag.Add((int)round.LR_Class);
                        }
                    }
                    savedItem["PhysicalObject"]["loadedRoundsInContainer"] = newLoadedRoundsInMag;
                }
            }
            else if (itemPhysicalObject is FVRFireArmClip)
            {
                FVRFireArmClip clipPhysicalObject = (itemPhysicalObject as FVRFireArmClip);
                if (clipPhysicalObject.HasARound())
                {
                    JArray newLoadedRoundsInClip = new JArray();
                    foreach (FVRFireArmClip.FVRLoadedRound round in clipPhysicalObject.LoadedRounds)
                    {
                        if (round == null)
                        {
                            break;
                        }
                        else
                        {
                            newLoadedRoundsInClip.Add((int)round.LR_Class);
                        }
                    }
                    savedItem["PhysicalObject"]["loadedRoundsInContainer"] = newLoadedRoundsInClip;
                }
            }
            else if (itemPhysicalObject is Speedloader)
            {
                Speedloader SLPhysicalObject = (itemPhysicalObject as Speedloader);
                if (SLPhysicalObject.Chambers != null)
                {
                    JArray newLoadedRoundsInSL = new JArray();
                    foreach (SpeedloaderChamber chamber in SLPhysicalObject.Chambers)
                    {
                        if (chamber.IsLoaded)
                        {
                            if (chamber.IsSpent)
                            {
                                newLoadedRoundsInSL.Add((int)chamber.LoadedClass * -1 - 2); // negative means spent of class: value * -1 - 2
                            }
                            else
                            {
                                newLoadedRoundsInSL.Add((int)chamber.LoadedClass);
                            }
                        }
                        else
                        {
                            newLoadedRoundsInSL.Add(-1); // -1 means not loaded
                        }
                    }
                    savedItem["PhysicalObject"]["loadedRoundsInContainer"] = newLoadedRoundsInSL;
                }
            }

            // Custom items
            EFM_CustomItemWrapper customItemWrapper = itemPhysicalObject.gameObject.GetComponentInChildren<EFM_CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                savedItem["itemType"] = (int)customItemWrapper.itemType;
                savedItem["PhysicalObject"]["equipSlot"] = -1;
                savedItem["amount"] = customItemWrapper.amount;

                // Armor
                if(customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    Mod.instance.LogInfo("Item is armor");

                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentArmor != null && EFM_EquipmentSlot.currentArmor.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        for(int i=0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    savedItem["PhysicalObject"]["equipSlot"] = i;
                                    break;
                                }
                            }
                        }
                    }
                    savedItem["PhysicalObject"]["armor"] = customItemWrapper.armor;
                    savedItem["PhysicalObject"]["maxArmor"] = customItemWrapper.maxArmor;
                }

                // Rig
                if(customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentRig != null && EFM_EquipmentSlot.currentRig.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    savedItem["PhysicalObject"]["equipSlot"] = i;
                                    break;
                                }
                            }
                        }
                    }
                    if(savedItem["PhysicalObject"]["quickBeltSlotContents"] == null)
                    {
                        savedItem["PhysicalObject"]["quickBeltSlotContents"] = new JArray();
                    }
                    JArray saveQBContents = (JArray)savedItem["PhysicalObject"]["quickBeltSlotContents"];
                    for (int i=0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if(customItemWrapper.itemsInSlots[i] == null)
                        {
                            saveQBContents.Add(null);
                        }
                        else
                        {
                            SaveItem(saveQBContents, customItemWrapper.itemsInSlots[i].transform, null, i);
                        }
                    }
                }

                // Backpack
                if(customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.instance.LogInfo("Item is backpack");

                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentBackpack != null && EFM_EquipmentSlot.currentBackpack.Equals(customItemWrapper))
                    {
                        Mod.instance.LogInfo("is equipped");
                        // Find its equip slot index
                        //TODO: for all wore items we could keep equip slot in customitemwrapper since all equppable items are custom
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    Mod.instance.LogInfo("in slot " + i);
                                    savedItem["PhysicalObject"]["equipSlot"] = i;
                                    break;
                                }
                            }
                        }
                    }
                    if(savedItem["PhysicalObject"]["backpackContents"] == null)
                    {
                        savedItem["PhysicalObject"]["backpackContents"] = new JArray();
                    }
                    JArray saveBPContents = (JArray)savedItem["PhysicalObject"]["backpackContents"];
                    for (int i=0; i < customItemWrapper.itemObjectsRoot.childCount; ++i)
                    {
                        Mod.instance.LogInfo("Item in backpack " + i + ": "+ customItemWrapper.itemObjectsRoot.GetChild(i).name);
                        SaveItem(saveBPContents, customItemWrapper.itemObjectsRoot.GetChild(i));
                    }
                }

                // Container
                if (customItemWrapper.itemType == Mod.ItemType.Container)
                {
                    Mod.instance.LogInfo("Item is container");

                    if (savedItem["PhysicalObject"]["containerContents"] == null)
                    {
                        savedItem["PhysicalObject"]["containerContents"] = new JArray();
                    }
                    JArray saveContainerContents = (JArray)savedItem["PhysicalObject"]["containerContents"];
                    for (int i = 0; i < customItemWrapper.itemObjectsRoot.childCount; ++i)
                    {
                        Mod.instance.LogInfo("Item in container " + i + ": " + customItemWrapper.itemObjectsRoot.GetChild(i).name);
                        SaveItem(saveContainerContents, customItemWrapper.itemObjectsRoot.GetChild(i));
                    }
                }

                // Pouch
                if (customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.instance.LogInfo("Item is pouch");

                    // If this is an equipment piece we are currently wearing
                    if (EFM_EquipmentSlot.currentPouch != null && EFM_EquipmentSlot.currentPouch.Equals(customItemWrapper))
                    {
                        Mod.instance.LogInfo("is equipped");
                        // Find its equip slot index
                        //TODO: for all wore items we could keep equip slot in customitemwrapper since all equppable items are custom
                        for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
                        {
                            if (Mod.equipmentSlots[i].CurObject != null)
                            {
                                EFM_CustomItemWrapper equipCustomItemWrapper = Mod.equipmentSlots[i].CurObject.GetComponent<EFM_CustomItemWrapper>();
                                if (equipCustomItemWrapper != null && equipCustomItemWrapper.Equals(customItemWrapper))
                                {
                                    Mod.instance.LogInfo("in slot " + i);
                                    savedItem["PhysicalObject"]["equipSlot"] = i;
                                    break;
                                }
                            }
                        }
                    }
                    if (savedItem["PhysicalObject"]["containerContents"] == null)
                    {
                        savedItem["PhysicalObject"]["containerContents"] = new JArray();
                    }
                    JArray savePouchContents = (JArray)savedItem["PhysicalObject"]["containerContents"];
                    for (int i = 0; i < customItemWrapper.itemObjectsRoot.childCount; ++i)
                    {
                        Mod.instance.LogInfo("Item in pouch " + i + ": " + customItemWrapper.itemObjectsRoot.GetChild(i).name);
                        SaveItem(savePouchContents, customItemWrapper.itemObjectsRoot.GetChild(i));
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                //{
                //    Mod.instance.LogInfo("Item is ammo box");
                //}

                // Money
                if(customItemWrapper.itemType == Mod.ItemType.Money)
                {
                    Mod.instance.LogInfo("Item is money");

                    savedItem["stack"] = customItemWrapper.stack;
                }

                // Consumable
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.instance.LogInfo("Item is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.instance.LogInfo("is Consumable");
                //}
            }

            // Vanilla items
            EFM_VanillaItemDescriptor vanillaItemDescriptor = itemPhysicalObject.gameObject.GetComponentInChildren<EFM_VanillaItemDescriptor>();
            if (vanillaItemDescriptor != null)
            {
                savedItem["looted"] = vanillaItemDescriptor.looted;
            }

            listToAddTo.Add(savedItem);
        }

        private void SaveAttachments(FVRPhysicalObject physicalObject, JToken itemPhysicalObject)
        {
            // We want to save attachments curently physically present on physicalObject into the save data itemPhysicalObject
            for (int i = 0; i < physicalObject.Attachments.Count; ++i)
            {
                JToken newPhysicalObject = new JObject();
                if(itemPhysicalObject["AttachmentsList"] == null)
                {
                    itemPhysicalObject["AttachmentsList"] = new JArray();
                }
                ((JArray)itemPhysicalObject["AttachmentsList"]).Add(newPhysicalObject);

                FVRPhysicalObject currentPhysicalObject = physicalObject.Attachments[i];
                Transform currentTransform = currentPhysicalObject.transform;

                newPhysicalObject["ObjectWrapper"] = new JObject();

                // Fill PhysicalObject
                newPhysicalObject["positionX"] = currentTransform.localPosition.x;
                newPhysicalObject["positionY"] = currentTransform.localPosition.y;
                newPhysicalObject["positionZ"] = currentTransform.localPosition.z;
                newPhysicalObject["rotationX"] = currentTransform.localRotation.eulerAngles.x;
                newPhysicalObject["rotationY"] = currentTransform.localRotation.eulerAngles.y;
                newPhysicalObject["rotationZ"] = currentTransform.localRotation.eulerAngles.z;
                newPhysicalObject["m_isSpawnLock"] = currentPhysicalObject.m_isSpawnLock;
                newPhysicalObject["m_isHarnessed"] = currentPhysicalObject.m_isHardnessed;
                newPhysicalObject["IsKinematicLocked"] = currentPhysicalObject.IsKinematicLocked;
                newPhysicalObject["IsInWater"] = currentPhysicalObject.IsInWater;
                SaveAttachments(currentPhysicalObject, newPhysicalObject);
                newPhysicalObject["mountIndex"] = physicalObject.AttachmentMounts.IndexOf((currentPhysicalObject as FVRFireArmAttachment).curMount);

                // Fill ObjectWrapper
                newPhysicalObject["ObjectWrapper"]["ItemID"] = currentPhysicalObject.ObjectWrapper.ItemID;
            }
        }

        public void FinishRaid(FinishRaidState state)
        {
            // Increment raid counters
            data["totalRaidCount"] = (int)data["totalRaidCount"] + 1;
            switch (state)
            {
                case FinishRaidState.RunThrough:
                    data["runThroughRaidCount"] = (int)data["runThroughRaidCount"] + 1;
                    data["survivedRaidCount"] = (int)data["survivedRaidCount"] + 1;
                    break;
                case FinishRaidState.Survived:
                    data["survivedRaidCount"] = (int)data["survivedRaidCount"] + 1;
                    break;
                case FinishRaidState.MIA:
                    data["MIARaidCount"] = (int)data["MIARaidCount"] + 1;
                    data["failedRaidCount"] = (int)data["failedRaidCount"] + 1;
                    break;
                case FinishRaidState.KIA:
                    data["KIARaidCount"] = (int)data["KIARaidCount"] + 1;
                    data["failedRaidCount"] = (int)data["failedRaidCount"] + 1;
                    break;
                default:
                    break;
            }

            // Save the base
            Mod.saveSlotIndex = 5;
            SaveBase();
        }

        private void SaveDataToFile()
        {
            File.WriteAllText("BepInEx/Plugins/EscapeFromMeatov/" + (Mod.saveSlotIndex == 5 ? "AutoSave" : "Slot" + Mod.saveSlotIndex) + ".sav", data.ToString());
        }
    }
}
