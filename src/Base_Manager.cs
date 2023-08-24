using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using Valve.Newtonsoft.Json;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class Base_Manager : Manager
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
        private Transform medicalScreen;
        private Transform medicalScreenPartImagesParent;
        private Transform[] medicalScreenPartImages;
        private Text[] medicalScreenPartHealthTexts;
        private Text medicalScreenTotalHealthText;
        private Text totalTreatmentPriceText;
        private Text scavTimerText;
        private Collider scavButtonCollider;
        private Text scavButtonText;
        private GameObject charChoicePanel;
        private GameObject[] saveConfirmTexts;
        private Transform optionsPageParent;
        private Transform[] optionPages;

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
        public static Sprite emptyItemSlotIcon;
        public static Sprite dollarCurrencySprite;
        public static Sprite euroCurrencySprite;
        public static Sprite roubleCurrencySprite;
        public static Sprite barterSprite;
        public static Sprite experienceSprite;
        public static Sprite standingSprite;
        public static AudioClip[,] areaProductionSounds;
        public static AudioClip[] areaSlotSounds;
        public static AudioClip generatorLevel1And2Audio;
        public static AudioClip generatorLevel3Audio;
        public static AudioClip medStationLevel3Audio;
        public static AudioClip kitchenPotAudio;
        public static AudioClip kitchenFridgeAudio;
        public static AudioClip[] restSpaceTracks;
        public static AudioClip restSpacePSAudio;
        public static AudioClip intelCenterPCAudio;
        public static AudioClip intelCenterHDDAudio;
        public static AudioClip AFUAudio;
        public static AudioClip boozeGenAudio;
        public static AudioClip bitcoinFarmAudio;

        public JToken data;

        public float time;
        private bool cancelRaidLoad;
        private bool loadingRaid;
        private bool countdownDeploy;
        private float deployTimer;
        private float deployTime = 0; // TODO: Should be 10 but set to 0 for faster debugging
        private int insuredSetIndex = 0;
        private float scavTimer;
        public List<BaseAreaManager> baseAreaManagers;
        public Dictionary<string, List<GameObject>> baseInventoryObjects;
        private Dictionary<int, int[]> fullPartConditions;
        private Dictionary<int, GameObject> medicalPartElements;
        private int totalMedicalTreatmentPrice;
        public MarketManager marketManager;
        public static bool marketUI; // whether we are currently in market mode or in area UI mode
        public GCManager GCManager;
        public AreaUpgradeCheckProcessor[] activeCheckProcessors = new AreaUpgradeCheckProcessor[2];

        public static float currentExperienceRate = 1;
        public static float currentQuestMoneyReward = 0;
        public static float currentFuelConsumption = 0; 
        public static float currentDebuffEndDelay = 0; 
        public static float currentScavCooldownTimer = 1;
        public static float currentInsuranceReturnTime = 1;
        public static float currentRagfairCommission = 1; // TODO: Implement, dont forget to use EFM_Skill.skillBoostPercent
        public static Dictionary<Skill.SkillType, float> currentSkillGroupLevelingBoosts;

        private void Update()
        {
            if (init)
            {
                UpdateTime();
            }

            UpdateScavTimer();

            UpdateEffects();

            UpdateInsuredSets();

            // Handle raid loading process
            if (cancelRaidLoad)
            {
                loadingRaid = false;
                countdownDeploy = false;

                // Wait until the raid map is done loading before unloading it
                if (Mod.currentRaidBundleRequest.isDone)
                {
                    if(Mod.currentRaidBundleRequest.assetBundle != null)
                    {
                        Mod.currentRaidBundleRequest.assetBundle.Unload(true);
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

                raidCountdown.text = Mod.FormatTimeString(deployTimer);

                if (deployTimer <= 0)
                {
                    // Autosave before starting the raid
                    Mod.saveSlotIndex = 5;
                    SaveBase();

                    // Reset player stats if scav raid
                    if(Mod.chosenCharIndex == 1)
                    {
                        // TODO: Maybe make these somewhat random? The scav may not have max energy and hydration for example
                        Effect.RemoveEffects();
                        for(int i=0; i < Mod.health.Length; ++i)
                        {
                            Mod.health[i] = Mod.defaultMaxHealth[i];
                        }
                        Mod.energy = Mod.maxEnergy;
                        Mod.hydration = Mod.maxHydration;
                        Mod.stamina = Mod.maxStamina;
                        Mod.weight = 0;
                    }

                    SteamVR_LoadLevel.Begin("Meatov"+ chosenMap.text+ "Scene", false, 0.5f, 0f, 0f, 0f, 1f);
                    countdownDeploy = false;
                }
            }
            else if (loadingRaid)
            {
                if(Mod.currentRaidBundleRequest.isDone)
                {
                    if(Mod.currentRaidBundleRequest.assetBundle != null)
                    {
                        // Load the asset of the map
                        // currentRaidMapRequest = currentRaidBundleRequest.assetBundle.LoadAllAssetsAsync<GameObject>();
                        deployTimer = deployTime;

                        loadingRaid = false;
                        countdownDeploy = true;
                    }
                    else
                    {
                        Mod.LogError("Could not load raid map bundle, cancelling");
                        cancelRaidLoad = true;
                    }
                }
                else
                {
                    raidCountdownTitle.text = "Loading map:";
                    raidCountdown.text = (Mod.currentRaidBundleRequest.progress * 100).ToString() + "%";
                }
            }
        }

        private void UpdateScavTimer()
        {
            scavTimer -= Time.deltaTime;
            if(scavTimer <= 0)
            {
                // Enable Scav button, disable scav timer text
                scavButtonCollider.enabled = true;
                scavButtonText.color = Color.white;
                scavTimerText.gameObject.SetActive(false);
            }
            else if(charChoicePanel.activeSelf)
            {
                // Update scav timer text
                scavTimerText.text = Mod.FormatTimeString(scavTimer);

                if (!scavTimerText.gameObject.activeSelf)
                {
                    // Disable Scav button, Enable scav timer text
                    scavButtonCollider.enabled = false;
                    scavButtonText.color = Color.grey;
                    scavTimerText.gameObject.SetActive(true);
                }
            }
        }

        private void UpdateInsuredSets()
        {
            if(Mod.insuredItems != null && Mod.insuredItems.Count > 0)
            {
                if(insuredSetIndex >= Mod.insuredItems.Count)
                {
                    insuredSetIndex = 0;
                }

                if(Mod.insuredItems[insuredSetIndex].returnTime <= GetTimeSeconds())
                {
                    foreach(KeyValuePair<string, int> insuredToSpawn in Mod.insuredItems[insuredSetIndex].items)
                    {
                        marketManager.SpawnItem(insuredToSpawn.Key, insuredToSpawn.Value);
                    }

                    Mod.insuredItems[insuredSetIndex] = Mod.insuredItems[Mod.insuredItems.Count - 1];
                    Mod.insuredItems.RemoveAt(insuredSetIndex);
                }
                else
                {
                    ++insuredSetIndex;
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
            for(int i = Effect.effects.Count; i >= 0; --i)
            {
                if(Effect.effects.Count == 0)
                {
                    break;
                }
                else if(i >= Effect.effects.Count)
                {
                    continue;
                }

                Effect effect = Effect.effects[i];
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
                                case Effect.EffectType.SkillRate:
                                    Mod.skills[effect.skillIndex].currentProgress -= effect.value;
                                    if(effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.EnergyRate:
                                    Mod.currentEnergyRate -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.HydrationRate:
                                    Mod.currentHydrationRate -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.MaxStamina:
                                    Mod.currentMaxStamina -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.StaminaRate:
                                    Mod.currentStaminaEffect -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.HandsTremor:
                                    // TODO: Stop tremors if there are not other tremor effects
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.QuantumTunnelling:
                                    // TODO: Stop QuantumTunnelling
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
                                    }
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.HealthRate:
                                    float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
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
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.RemoveAllBloodLosses:
                                    // Reactivate all bleeding 
                                    // Not necessary because when we disabled them we used the disable timer
                                    break;
                                case Effect.EffectType.Contusion:
                                    bool otherContusions = false;
                                    foreach (Effect contusionEffectCheck in Effect.effects)
                                    {
                                        if (contusionEffectCheck.active && contusionEffectCheck.effectType == Effect.EffectType.Contusion)
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
                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.WeightLimit:
                                    Mod.effectWeightLimitBonus -= effect.value * 1000;
                                    Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.DamageModifier:
                                    Mod.currentDamageModifier -= effect.value;
                                    if (effect.value < 0)
                                    {
                                        Mod.AddSkillExp(5, 6);
                                    }
                                    break;
                                case Effect.EffectType.Pain:
                                    // Remove all tremors caused by this pain and disable tremors if no other tremors active
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasPainTremors = false;
                                    foreach(Effect effectCheck in Effect.effects)
                                    {
                                        if(effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(false);
                                    }

                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.StomachBloodloss:
                                    --Mod.stomachBloodLossCount;
                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }

                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.UnknownToxin:
                                    // Remove all effects caused by this toxin
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == Effect.EffectType.HealthRate)
                                        {
                                            for (int j = 0; j < 7; ++j)
                                            {
                                                Mod.currentHealthRates[j] -= causedEffect.value / 7;
                                            }
                                        }
                                        // Could go two layers deep
                                        foreach (Effect causedCausedEffect in effect.caused)
                                        {
                                            Effect.effects.Remove(causedCausedEffect);
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasToxinTremors = false;
                                    foreach (Effect effectCheck in Effect.effects)
                                    {
                                        if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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

                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.BodyTemperature:
                                    Mod.temperatureOffset -= effect.value;
                                    break;
                                case Effect.EffectType.Antidote:
                                    // Will remove toxin on ativation, does nothing after
                                    break;
                                case Effect.EffectType.LightBleeding:
                                case Effect.EffectType.HeavyBleeding:
                                    // Remove all effects caused by this bleeding
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        if (causedEffect.effectType == Effect.EffectType.HealthRate)
                                        {
                                            if (causedEffect.partIndex == -1)
                                            {
                                                for (int j = 0; j < 7; ++j)
                                                {
                                                    Mod.currentNonLethalHealthRates[j] -= causedEffect.value;
                                                }
                                            }
                                            else
                                            {
                                                Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
                                            }
                                        }
                                        else // Energy rate
                                        {
                                            Mod.currentEnergyRate -= causedEffect.value;
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
                                    }

                                    if (Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
                                    }

                                    Mod.AddSkillExp(5, 6);
                                    break;
                                case Effect.EffectType.Fracture:
                                    // Remove all effects caused by this fracture
                                    foreach (Effect causedEffect in effect.caused)
                                    {
                                        // Could go two layers deep
                                        foreach (Effect causedCausedEffect in effect.caused)
                                        {
                                            Effect.effects.Remove(causedCausedEffect);
                                        }
                                        Effect.effects.Remove(causedEffect);
                                    }
                                    bool hasFractureTremors = false;
                                    foreach (Effect effectCheck in Effect.effects)
                                    {
                                        if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
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

                                    if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                                    {
                                        Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                                    }

                                    Mod.AddSkillExp(5, 6);
                                    break;
                            }

                            Effect.effects.RemoveAt(i);

                            continue;
                        }
                    }

                    if(effect.active && effect.partIndex != -1 && (effect.effectType == Effect.EffectType.LightBleeding ||
                                                                   effect.effectType == Effect.EffectType.HeavyBleeding ||
                                                                   (effect.effectType == Effect.EffectType.HealthRate && effect.value < 0)))
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
                    if (effect.hideoutOnly)
                    {
                        effect.active = true;
                        effectJustActivated = true;
                    }

                    // Apply effect if it just started being active
                    if (effectJustActivated)
                    {
                        switch (effect.effectType)
                        {
                            case Effect.EffectType.SkillRate:
                                Mod.skills[effect.skillIndex].currentProgress += effect.value;
                                break;
                            case Effect.EffectType.EnergyRate:
                                Mod.currentEnergyRate += effect.value;
                                break;
                            case Effect.EffectType.HydrationRate:
                                Mod.currentHydrationRate += effect.value;
                                break;
                            case Effect.EffectType.MaxStamina:
                                Mod.currentMaxStamina += effect.value;
                                break;
                            case Effect.EffectType.StaminaRate:
                                Mod.currentStaminaEffect += effect.value;
                                break;
                            case Effect.EffectType.HandsTremor:
                                // TODO: Begin tremors if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.QuantumTunnelling:
                                // TODO: Begin quantumtunneling if there isnt already another active one
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HealthRate:
                                float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
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
                            case Effect.EffectType.RemoveAllBloodLosses:
                                // Deactivate all bleeding using disable timer
                                foreach(Effect bleedEffect in Effect.effects)
                                {
                                    if(bleedEffect.effectType == Effect.EffectType.LightBleeding || bleedEffect.effectType == Effect.EffectType.HeavyBleeding)
                                    {
                                        bleedEffect.active = false;
                                        bleedEffect.inactiveTimer = effect.timer;

                                        // Unapply the healthrate caused by this bleed
                                        Effect causedHealthRate = bleedEffect.caused[0];
                                        if (causedHealthRate.nonLethal)
                                        {
                                            Mod.currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        else
                                        {
                                            Mod.currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
                                        }
                                        Effect causedEnergyRate = bleedEffect.caused[1];
                                        Mod.currentEnergyRate -= causedEnergyRate.value;
                                        bleedEffect.caused.Clear();
                                        Effect.effects.Remove(causedHealthRate);
                                        Effect.effects.Remove(causedEnergyRate);
                                    }
                                }
                                break;
                            case Effect.EffectType.Contusion:
                                // Disable haptic feedback
                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
                                // TODO: also set volume to 0.33 * volume
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.WeightLimit:
                                Mod.effectWeightLimitBonus += effect.value * 1000;
                                Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
                                break;
                            case Effect.EffectType.DamageModifier:
                                Mod.currentDamageModifier += effect.value;
                                break;
                            case Effect.EffectType.Pain:
                                if (UnityEngine.Random.value < 1 - (Mod.skills[4].currentProgress / 10000))
                                {
                                    // Add a tremor effect
                                    Effect newTremor = new Effect();
                                    newTremor.effectType = Effect.EffectType.HandsTremor;
                                    newTremor.delay = 5;
                                    newTremor.hasTimer = effect.hasTimer;
                                    newTremor.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                    - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                    Effect.effects.Add(newTremor);
                                    effect.caused.Add(newTremor);
                                }

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(true);
                                }

                                Mod.AddSkillExp(Skill.stressResistanceHealthNegativeEffect, 4);
                                break;
                            case Effect.EffectType.StomachBloodloss:
                                ++Mod.stomachBloodLossCount;
                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.UnknownToxin:
                                // Add a pain effect
                                Effect newToxinPain = new Effect();
                                newToxinPain.effectType = Effect.EffectType.Pain;
                                newToxinPain.delay = 5;
                                newToxinPain.hasTimer = effect.hasTimer;
                                newToxinPain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                   - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                newToxinPain.partIndex = 0;
                                Effect.effects.Add(newToxinPain);
                                effect.caused.Add(newToxinPain);
                                // Add a health rate effect
                                Effect newToxinHealthRate = new Effect();
                                newToxinHealthRate.effectType = Effect.EffectType.HealthRate;
                                newToxinHealthRate.delay = 5;
                                newToxinHealthRate.value = -25 + 25 * (Skill.immunityPoisonBuff * (Mod.skills[6].currentProgress / 100) / 100);
                                newToxinHealthRate.hasTimer = effect.hasTimer;
                                newToxinHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                Effect.effects.Add(newToxinHealthRate);
                                effect.caused.Add(newToxinHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.BodyTemperature:
                                Mod.temperatureOffset += effect.value;
                                break;
                            case Effect.EffectType.Antidote:
                                // Will remove toxin on ativation, does nothing after
                                for (int j = Effect.effects.Count; j >= 0; --j)
                                {
                                    if (Effect.effects[j].effectType == Effect.EffectType.UnknownToxin)
                                    {
                                        Effect.effects.RemoveAt(j);
                                        break;
                                    }
                                }
                                break;
                            case Effect.EffectType.LightBleeding:
                                // Add a health rate effect
                                Effect newLightBleedingHealthRate = new Effect();
                                newLightBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
                                newLightBleedingHealthRate.delay = 5;
                                newLightBleedingHealthRate.value = -8;
                                newLightBleedingHealthRate.hasTimer = effect.hasTimer;
                                newLightBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                newLightBleedingHealthRate.partIndex = effect.partIndex;
                                newLightBleedingHealthRate.nonLethal = true;
                                Effect.effects.Add(newLightBleedingHealthRate);
                                effect.caused.Add(newLightBleedingHealthRate);
                                // Add a energy rate effect
                                Effect newLightBleedingEnergyRate = new Effect();
                                newLightBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newLightBleedingEnergyRate.delay = 5;
                                newLightBleedingEnergyRate.value = -5;
                                newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newLightBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                newLightBleedingEnergyRate.partIndex = effect.partIndex;
                                Effect.effects.Add(newLightBleedingEnergyRate);
                                effect.caused.Add(newLightBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyBleeding:
                                // Add a health rate effect
                                Effect newHeavyBleedingHealthRate = new Effect();
                                newHeavyBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyBleedingHealthRate.delay = 5;
                                newHeavyBleedingHealthRate.value = -13.5f;
                                newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                newHeavyBleedingHealthRate.nonLethal = true;
                                Effect.effects.Add(newHeavyBleedingHealthRate);
                                effect.caused.Add(newHeavyBleedingHealthRate);
                                // Add a energy rate effect
                                Effect newHeavyBleedingEnergyRate = new Effect();
                                newHeavyBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newHeavyBleedingEnergyRate.delay = 5;
                                newHeavyBleedingEnergyRate.value = -6;
                                newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
                                newHeavyBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                newHeavyBleedingEnergyRate.partIndex = effect.partIndex;
                                Effect.effects.Add(newHeavyBleedingEnergyRate);
                                effect.caused.Add(newHeavyBleedingEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Fracture:
                                // Add a pain effect
                                Effect newFracturePain = new Effect();
                                newFracturePain.effectType = Effect.EffectType.Pain;
                                newFracturePain.delay = 5;
                                newFracturePain.hasTimer = effect.hasTimer;
                                newFracturePain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
                                                      - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                                Effect.effects.Add(newFracturePain);
                                effect.caused.Add(newFracturePain);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Dehydration:
                                // Add a HealthRate effect
                                Effect newDehydrationHealthRate = new Effect();
                                newDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
                                newDehydrationHealthRate.value = -60;
                                newDehydrationHealthRate.delay = 5;
                                newDehydrationHealthRate.hasTimer = false;
                                Effect.effects.Add(newDehydrationHealthRate);
                                effect.caused.Add(newDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyDehydration:
                                // Add a HealthRate effect
                                Effect newHeavyDehydrationHealthRate = new Effect();
                                newHeavyDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyDehydrationHealthRate.value = -350;
                                newHeavyDehydrationHealthRate.delay = 5;
                                newHeavyDehydrationHealthRate.hasTimer = false;
                                Effect.effects.Add(newHeavyDehydrationHealthRate);
                                effect.caused.Add(newHeavyDehydrationHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.Fatigue:
                                Mod.fatigue = true;

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.HeavyFatigue:
                                // Add a HealthRate effect
                                Effect newHeavyFatigueHealthRate = new Effect();
                                newHeavyFatigueHealthRate.effectType = Effect.EffectType.HealthRate;
                                newHeavyFatigueHealthRate.value = -30;
                                newHeavyFatigueHealthRate.delay = 5;
                                newHeavyFatigueHealthRate.hasTimer = false;
                                Effect.effects.Add(newHeavyFatigueHealthRate);
                                effect.caused.Add(newHeavyFatigueHealthRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
                                }
                                break;
                            case Effect.EffectType.OverweightFatigue:
                                // Add a EnergyRate effect
                                Effect newOverweightFatigueEnergyRate = new Effect();
                                newOverweightFatigueEnergyRate.effectType = Effect.EffectType.EnergyRate;
                                newOverweightFatigueEnergyRate.value = -4;
                                newOverweightFatigueEnergyRate.delay = 5;
                                newOverweightFatigueEnergyRate.hasTimer = false;
                                Effect.effects.Add(newOverweightFatigueEnergyRate);
                                effect.caused.Add(newOverweightFatigueEnergyRate);

                                if (!Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.activeSelf)
                                {
                                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
                                }
                                break;
                        }
                    }
                }
            }

            // Apply health, energy, and hydration rates
            float healthDelta = 0;
            float health = 0;
            float maxHealthTotal = 0;
            for (int i = 0; i < 7; ++i)
            {
                maxHealthTotal += Mod.currentMaxHealth[i];
                float currentHealthDelta = Mod.currentHealthRates[i] + Mod.currentNonLethalHealthRates[i];
                if (heal[i] && currentHealthDelta > 0)
                {
                    Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthDelta * (Time.deltaTime / 60), 1, Mod.currentMaxHealth[i]);

                    healthDelta += currentHealthDelta;
                    health += Mod.health[i];
                }
                Mod.playerStatusManager.partHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
                Mod.playerStatusManager.partHealthImages[i].color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);

                if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
                {
                    medicalScreenPartHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
                    medicalScreenPartImages[i].GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);
                }
            }
            if (healthDelta != 0)
            {
                if (!Mod.playerStatusManager.healthDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.healthDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.healthDeltaText.text = (healthDelta > 0 ? "+":"") + String.Format("{0:0.#}/min", healthDelta);
            }
            else if(Mod.playerStatusManager.healthDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.healthDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.healthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal);
            if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
            {
                medicalScreenTotalHealthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal); ;
            }
            GM.CurrentPlayerBody.SetHealthThreshold(maxHealthTotal);
            GM.CurrentPlayerBody.Health = health; // This must be done after setting health threshold because setting health threshold also sets health

            if (Mod.currentHydrationRate > 0)
            {
                Mod.hydration = Mathf.Clamp(Mod.hydration + Mod.currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);

                if (!Mod.playerStatusManager.hydrationDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.hydrationDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.hydrationDeltaText.text = (Mod.currentHydrationRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentHydrationRate);
            }
            else if (Mod.playerStatusManager.hydrationDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.hydrationDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.hydrationText.text = String.Format("{0:0}/{1}", Mod.hydration, Mod.maxHydration);
            if(Mod.hydration > 20)
            {
                // Remove any dehydration effect
                if (Mod.dehydrationEffect != null)
                {
                    // Disable 
                    if (Mod.dehydrationEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.dehydrationEffect);
                }

                if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
                {
                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(false);
                }
            }

            if (Mod.currentEnergyRate > 0)
            {
                Mod.energy = Mathf.Clamp(Mod.energy + Mod.currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);

                if (!Mod.playerStatusManager.energyDeltaText.gameObject.activeSelf)
                {
                    Mod.playerStatusManager.energyDeltaText.gameObject.SetActive(true);
                }
                Mod.playerStatusManager.energyDeltaText.text = (Mod.currentEnergyRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentEnergyRate);
            }
            else if (Mod.playerStatusManager.energyDeltaText.gameObject.activeSelf)
            {
                Mod.playerStatusManager.energyDeltaText.gameObject.SetActive(false);
            }
            Mod.playerStatusManager.energyText.text = String.Format("{0:0}/{1}", Mod.energy, Mod.maxEnergy);
            if(Mod.energy > 20)
            {
                // Remove any fatigue effect
                if (Mod.fatigueEffect != null)
                {
                    // Disable 
                    if (Mod.fatigueEffect.caused.Count > 0)
                    {
                        for (int j = 0; j < 7; ++j)
                        {
                            Mod.currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
                        }
                        Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
                    }
                    Effect.effects.Remove(Mod.fatigueEffect);
                }

                if (Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
                {
                    Mod.playerStatusManager.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(false);
                }
            }
        }

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * Manager.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            string formattedTime0 = Mod.FormatTimeString(time);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            string formattedTime1 = Mod.FormatTimeString(offsetTime);
            timeChoice1.text = formattedTime1;

            chosenTime.text = Mod.chosenTimeIndex == 0 ? formattedTime0 : formattedTime1;
        }

        public override void Init()
        {
            Mod.currentBaseManager = this;
            GM.CurrentSceneSettings.MaxPointingDistance = 30;
            GM.CurrentSceneSettings.IsSpawnLockingEnabled = false;
            GM.CurrentSceneSettings.AreQuickbeltSlotsEnabled = false; // To prevent them from resetting when switching scene, we can still manually set config

            Mod.dead = false;

            // Don't want to setup player rig if just got out of raid
            if (!Mod.justFinishedRaid)
            {
                // Only setup player rig if not already setup
                if (Mod.playerStatusUI == null)
                {
                    SetupPlayerRig();
                }
                else
                {
                    // Sometimes when loading stamina bar gets offset, so ensure everything we have attached to player is positionned properly
                    Mod.staminaBarUI.transform.localRotation = Quaternion.Euler(-25, 0, 0);
                    Mod.staminaBarUI.transform.localPosition = new Vector3(0, -0.4f, 0.6f);
                    Mod.staminaBarUI.transform.localScale = Vector3.one * 0.0015f;

                    Mod.rightShoulderSlot.transform.GetChild(0).localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    Mod.rightShoulderSlot.transform.GetChild(0).localPosition = new Vector3(0.15f, 0, -0.05f);
                    Mod.leftShoulderSlot.transform.GetChild(0).localScale = new Vector3(0.3f, 0.3f, 0.3f);
                    Mod.leftShoulderSlot.transform.GetChild(0).localPosition = new Vector3(-0.15f, 0, -0.05f);
                }

                // Set pockets configuration as default
                GM.CurrentPlayerBody.ConfigureQuickbelt(Mod.pocketsConfigIndex);
            }

            // Set live data
            float totalMaxHealth = 0;
            for (int i = 0; i < 7; ++i)
            {
                Mod.currentMaxHealth[i] = Mod.defaultMaxHealth[i];
                totalMaxHealth += Mod.currentMaxHealth[i];
                Mod.currentHealthRates[i] += Mod.hideoutHealthRates[i];
            }
            GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);
            if (Mod.justFinishedRaid)
            {
                Mod.currentEnergyRate -= Mod.raidEnergyRate;
                Mod.currentHydrationRate -= Mod.raidHydrationRate;
            }
            Mod.currentEnergyRate += Mod.hideoutEnergyRate;
            Mod.currentHydrationRate += Mod.hideoutHydrationRate;
            if (currentSkillGroupLevelingBoosts == null)
            {
                currentSkillGroupLevelingBoosts = new Dictionary<Skill.SkillType, float>();
            }

            // Manage active descriptions dict
            if(Mod.activeDescriptionsByItemID != null)
            {
                Mod.activeDescriptionsByItemID.Clear();
            }
            else
            {
                Mod.activeDescriptionsByItemID = new Dictionary<string, List<DescriptionManager>>();
            }

            ProcessData();

            InitUI();

            InitTime();

            if (Mod.justFinishedRaid && Mod.chosenCharIndex == 0)
            {
                FinishRaid(Mod.raidState); // This will save on autosave

                // Set any parts health to 1 if they are at 0
                for (int i = 0; i < 7; ++i)
                {
                    if (Mod.health[i] == 0)
                    {
                        Mod.health[i] = 1;
                    }
                }

                // Add movement skill exp
                Mod.AddSkillExp(Mod.distanceTravelledSprinting * Skill.sprintAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(Mod.distanceTravelledSprinting * UnityEngine.Random.Range(Skill.sprintActionMin, Skill.sprintActionMax), 1);
                }
                Mod.AddSkillExp(Mod.distanceTravelledWalking * Skill.movementAction, 0);
                if (Mod.weight <= Mod.currentWeightLimit)
                {
                    Mod.AddSkillExp(Mod.distanceTravelledWalking * UnityEngine.Random.Range(Skill.movementActionMin, Skill.movementActionMax), 1);
                }
            }

            // Manager GC ourselves
            GCManager = gameObject.AddComponent<GCManager>();

            // Give any existing rewards to player now
            if(Mod.rewardsToGive != null && Mod.rewardsToGive.Count > 0)
            {
                foreach(List<TraderTaskReward> rewards in Mod.rewardsToGive)
                {
                    marketManager.GivePlayerRewards(rewards);
                }
                Mod.rewardsToGive = null;
            }

            Mod.justFinishedRaid = false;

            // Also set respawn to spawn point
            GM.CurrentSceneSettings.DeathResetPoint = transform.GetChild(transform.childCount - 1).GetChild(0);

            init = true;
        }

        private void SetupPlayerRig()
        {
            // Remove postprocess layer from rig because unnecessary? TODO: Review this once weve fixed cause of high CPU usage in raid, maybe post process doesnt actually affect it that much
            PostProcessLayer PPLayer = GM.CurrentPlayerBody.GetComponentInChildren<PostProcessLayer>();
            if(PPLayer != null)
            {
                Destroy(PPLayer);
            }

            // Set player's max health
            float totalMaxHealth = 0;
            foreach(float bodyPartMaxHealth in Mod.currentMaxHealth)
            {
                totalMaxHealth += bodyPartMaxHealth;
            }
            GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);

            // Player status
            Mod.playerStatusUI = Instantiate(Mod.playerStatusUIPrefab, GM.CurrentPlayerRoot);
            Mod.playerStatusManager = Mod.playerStatusUI.AddComponent<PlayerStatusManager>();
            Mod.playerStatusManager.Init();
            // Consumable indicator
            Mod.consumeUI = Instantiate(Mod.consumeUIPrefab, GM.CurrentPlayerRoot);
            Mod.consumeUIText = Mod.consumeUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            Mod.consumeUI.SetActive(false);
            // Stack split UI
            Mod.stackSplitUI = Instantiate(Mod.stackSplitUIPrefab, GM.CurrentPlayerRoot);
            Mod.stackSplitUIText = Mod.stackSplitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            Mod.stackSplitUICursor = Mod.stackSplitUI.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(6);
            Mod.stackSplitUI.SetActive(false);
            // Extraction UI
            Mod.extractionUI = Instantiate(Mod.extractionUIPrefab, GM.CurrentPlayerRoot);
            Mod.extractionUIText = Mod.extractionUI.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
            Mod.extractionUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            Mod.extractionUI.SetActive(false);
            // Extraction limit UI
            Mod.extractionLimitUI = Instantiate(Mod.extractionLimitUIPrefab, GM.CurrentPlayerRoot);
            Mod.extractionLimitUIText = Mod.extractionLimitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            Mod.extractionLimitUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            Mod.extractionLimitUI.SetActive(false);
            // ItemDescription UIs
            Mod.leftDescriptionUI = Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.LeftHand);
            Mod.leftDescriptionManager = Mod.leftDescriptionUI.AddComponent<DescriptionManager>();
            Mod.leftDescriptionManager.Init();
            Mod.rightDescriptionUI = Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.RightHand);
            Mod.rightDescriptionManager = Mod.rightDescriptionUI.AddComponent<DescriptionManager>();
            Mod.rightDescriptionManager.Init();
            // Stamina bar
            Mod.staminaBarUI = Instantiate(Mod.staminaBarPrefab, GM.CurrentPlayerBody.Head);
            Mod.staminaBarUI.transform.localRotation = Quaternion.Euler(-25, 0, 0);
            Mod.staminaBarUI.transform.localPosition = new Vector3(0, -0.4f, 0.6f);
            Mod.staminaBarUI.transform.localScale = Vector3.one * 0.0015f;

            // Add our own hand component to each hand
            Mod.rightHand = GM.CurrentPlayerBody.RightHand.gameObject.AddComponent<Hand>();
            Mod.leftHand = GM.CurrentPlayerBody.LeftHand.gameObject.AddComponent<Hand>();
            Mod.rightHand.otherHand = Mod.leftHand;
            Mod.leftHand.otherHand = Mod.rightHand;

            /*
             * GameObject slotObject = equipSlotParent.GetChild(i).GetChild(0).gameObject;
                slotObject.tag = "QuickbeltSlot";
                slotObject.SetActive(false); // Just so Awake() isn't called until we've set slot components fields

                EFM_EquipmentSlot slotComponent = slotObject.AddComponent<EFM_EquipmentSlot>();
                Mod.equipmentSlots.Add(slotComponent);
                slotComponent.QuickbeltRoot = slotObject.transform;
                slotComponent.HoverGeo = slotObject.transform.GetChild(0).GetChild(0).gameObject;
                slotComponent.HoverGeo.SetActive(false);
                slotComponent.PoseOverride = slotObject.transform.GetChild(0).GetChild(2);
                slotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Sphere;
                slotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                slotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;

                // Set slot sphere materials
                slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;
             * */

            // Shoulder storage
            GameObject rightShoulderSlot = GameObject.Instantiate(Mod.rectQuickBeltSlotPrefab, GM.CurrentPlayerBody.Head);
            rightShoulderSlot.name = "RightShoulderSlot";
            rightShoulderSlot.tag = "QuickbeltSlot";
            rightShoulderSlot.SetActive(false);
            rightShoulderSlot.transform.GetChild(0).localScale = new Vector3(0.3f, 0.3f, 0.3f);
            rightShoulderSlot.transform.GetChild(0).localPosition = new Vector3(0.15f, 0, -0.05f);

            ShoulderStorage rightSlotComponent = rightShoulderSlot.AddComponent<ShoulderStorage>();
            rightSlotComponent.right = true;
            Mod.rightShoulderSlot = rightSlotComponent;
            rightSlotComponent.QuickbeltRoot = rightShoulderSlot.transform;
            rightSlotComponent.HoverGeo = rightShoulderSlot.transform.GetChild(0).GetChild(0).gameObject;
            rightSlotComponent.HoverGeo.SetActive(false);
            rightSlotComponent.PoseOverride = rightShoulderSlot.transform.GetChild(0).GetChild(2);
            rightSlotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear;
            rightSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
            rightSlotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
            rightSlotComponent.RectBounds = rightShoulderSlot.transform.GetChild(0);

            GameObject leftShoulderSlot = GameObject.Instantiate(Mod.rectQuickBeltSlotPrefab, GM.CurrentPlayerBody.Head);
            leftShoulderSlot.name = "LeftShoulderSlot";
            leftShoulderSlot.tag = "QuickbeltSlot";
            leftShoulderSlot.SetActive(false);
            leftShoulderSlot.transform.GetChild(0).localScale = new Vector3(0.3f, 0.3f, 0.3f);
            leftShoulderSlot.transform.GetChild(0).localPosition = new Vector3(-0.15f, 0, -0.05f);

            ShoulderStorage leftSlotComponent = leftShoulderSlot.AddComponent<ShoulderStorage>();
            leftSlotComponent.right = true;
            Mod.leftShoulderSlot = leftSlotComponent;
            leftSlotComponent.QuickbeltRoot = leftShoulderSlot.transform;
            leftSlotComponent.HoverGeo = leftShoulderSlot.transform.GetChild(0).GetChild(0).gameObject;
            leftSlotComponent.HoverGeo.SetActive(false);
            leftSlotComponent.PoseOverride = leftShoulderSlot.transform.GetChild(0).GetChild(2);
            leftSlotComponent.Shape = FVRQuickBeltSlot.QuickbeltSlotShape.Rectalinear;
            leftSlotComponent.SizeLimit = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
            leftSlotComponent.Type = FVRQuickBeltSlot.QuickbeltSlotType.Standard;
            leftSlotComponent.RectBounds = leftShoulderSlot.transform.GetChild(0);

            // Set shoulders invisible
            rightShoulderSlot.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().enabled = false;
            rightShoulderSlot.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().enabled = false;
            leftShoulderSlot.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().enabled = false;
            leftShoulderSlot.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().enabled = false;

            rightShoulderSlot.SetActive(true);
            leftShoulderSlot.SetActive(true);

            // Set movement control
            GM.CurrentMovementManager.Mode = FVRMovementManager.MovementMode.TwinStick;
            GM.Options.MovementOptions.Touchpad_Confirm = FVRMovementManager.TwoAxisMovementConfirm.OnTouch;
            GM.Options.ControlOptions.CCM = ControlOptions.CoreControlMode.Streamlined;

            // Disable wrist menus
            Mod.rightHand.fvrHand.DisableWristMenu();
            Mod.leftHand.fvrHand.DisableWristMenu();
        }

        public void ProcessData()
        {
            Mod.preventLoadMagUpdateLists = true;
            Mod.attachmentLocalTransform = new List<KeyValuePair<GameObject, object>>();

            // Check if we have loaded data
            if (data == null)
            {
                data = new JObject();
                Mod.level = 1;
                Mod.skills = new Skill[64];
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i] = new Skill();
                    // 0-6 unless 4 physical
                    // 28-53 unless 52 practical
                    // 54-63 special
                    if (i >= 0 && i <= 6 && i != 4) 
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Physical;
                    }
                    else if(i >= 28 && i <= 53 && i != 52)
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Practical;
                    }
                    else if (i >= 54 && i <= 63)
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Special;
                    }
                }

                Mod.health = new float[7];
                for (int i = 0; i < 7; ++i)
                {
                    Mod.health[i] = Mod.currentMaxHealth[i];
                }
                Mod.hydration = 100;
                Mod.energy = 100;
                Mod.weight = 0;

                // Spawn standard edition starting items
                Transform itemRoot = transform.GetChild(transform.childCount - 2);
                GameObject.Instantiate(Mod.itemPrefabs[199], new Vector3(0.782999f, 0.6760001f, 6.609f), Quaternion.Euler(0f, 37.55229f, 0f), itemRoot);
                GameObject.Instantiate(IM.OD["UtilityFlashlight"].GetGameObject(), new Vector3(0.782999f, 0.8260001f, 6.609f), Quaternion.Euler(0f, 37.55229f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[396], new Vector3(16.685f, 0.405f, -2.755f), Quaternion.Euler(328.4395f, 270.6471f, 2.003955E-06f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[396], new Vector3(8.198049f, 0.4181025f, -6.029191f), Quaternion.Euler(346.8106f, 0f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[396], new Vector3(-4.905951f, 0.4161026f, 23.27681f), Quaternion.Euler(348.9087f, 0f, 0f), itemRoot);
                GameObject ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.007049f, 0.5902026f, 3.70981f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                CustomItemWrapper customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                FVRFireArmMagazine asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.007049f, 0.5902026f, 3.64581f), Quaternion.Euler(0f, 5.83668f, 0f), itemRoot); customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(0.9946489f, 0.5902026f, 3.578609f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.004449f, 0.5902026f, 3.49771f), Quaternion.Euler(0f, 323.5824f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.084949f, 0.5902026f, 3.58231f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.145749f, 0.6102026f, 3.68561f), Quaternion.Euler(0f, 0f, 86.27259f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.117249f, 0.5902026f, 3.39831f), Quaternion.Euler(0f, 350.4561f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.117249f, 1.496603f, 3.39831f), Quaternion.Euler(0f, 350.4561f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(1.049649f, 1.496603f, 3.63981f), Quaternion.Euler(0f, 221.015f, 180f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(0.9520493f, 0.2060025f, 4.07481f), Quaternion.Euler(0f, 54.43977f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(-4.89295f, 0.02490258f, -7.48019f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(-4.825951f, 0.02490258f, -7.42929f), Quaternion.Euler(0f, 56.71285f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(16.25405f, 0.02390254f, -1.25619f), Quaternion.Euler(0f, 51.82084f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(-3.582951f, 0.07820261f, 1.22981f), Quaternion.Euler(0f, 51.82084f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(-3.651051f, 0.07820261f, 1.23771f), Quaternion.Euler(0f, 117.0799f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[678], new Vector3(-3.61725f, 0.1157026f, 1.23901f), Quaternion.Euler(0f, 106.7499f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                GameObject.Instantiate(IM.OD["PinnedGrenadeXM84"].GetGameObject(), new Vector3(-0.04095078f, 0.4530027f, 4.52161f), Quaternion.Euler(0f, 0f, 271.3958f), itemRoot);
                GameObject.Instantiate(IM.OD["PinnedGrenadeXM84"].GetGameObject(), new Vector3(-0.2619514f, 0.4481025f, 4.35781f), Quaternion.Euler(359.111f, 39.55607f, 271.0761f), itemRoot);
                GameObject consumableObject = GameObject.Instantiate(Mod.itemPrefabs[608], new Vector3(-3.854952f, 0.1344025f, 0.6271096f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                CustomItemWrapper consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[608], new Vector3(13.58705f, 0.08240259f, -2.68019f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[597], new Vector3(13.41505f, 0.1511025f, -0.4421903f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[597], new Vector3(13.46805f, 0.1511025f, -0.2891903f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[597], new Vector3(16.04205f, 0.05110264f, -1.50819f), Quaternion.Euler(45f, 27.66409f, 270.0001f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[597], new Vector3(-3.742851f, 0.1030025f, 0.8398097f), Quaternion.Euler(45f, 27.66409f, 270.0001f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[623], new Vector3(11.52605f, 0.1997025f, -2.04419f), Quaternion.Euler(0f, 10.17791f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[623], new Vector3(8.865049f, 0.5168025f, -4.50319f), Quaternion.Euler(0f, 10.17791f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[623], new Vector3(8.865049f, 0.5331025f, -4.43109f), Quaternion.Euler(14.04671f, 10.49514f, 2.574447f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[595], new Vector3(11.78305f, 0.1993027f, -2.02819f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[595], new Vector3(-4.93795f, 0.07120264f, 1.75981f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[595], new Vector3(-4.906952f, 0.07120264f, 1.86681f), Quaternion.Euler(0f, 21.25007f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[596], new Vector3(11.44805f, 0.1927025f, -1.02219f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[596], new Vector3(11.40375f, 0.1927025f, -1.12049f), Quaternion.Euler(0f, 7.272392f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[596], new Vector3(12.21005f, 0.1927025f, -1.70619f), Quaternion.Euler(0f, 29.66055f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[636], new Vector3(16.37205f, 0.04950261f, -1.18019f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[636], new Vector3(11.35405f, 0.04950261f, -2.26519f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[636], new Vector3(-4.762951f, 0.1073025f, 1.95981f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[636], new Vector3(-4.830952f, 0.1073025f, 2.08881f), Quaternion.Euler(0f, 0f, 270.0742f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[628], new Vector3(-4.569649f, 0.0717026f, 1.841403f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[628], new Vector3(-4.73295f, 0.8293025f, 10.27161f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[593], new Vector3(0.3083f, 0.1314001f, 32.8823f), Quaternion.Euler(0f, 333.2294f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[593], new Vector3(-4.58f, 0.1506f, 1.13f), Quaternion.Euler(0f, 7.700641f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[593], new Vector3(11.6359f, 0.2268f, -1.4793f), Quaternion.Euler(0f, 7.700641f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[656], new Vector3(-0.6802502f, 0.3907025f, 3.97801f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[63], new Vector3(2.551049f, 0.2303026f, -5.279191f), Quaternion.Euler(0f, 302.7106f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                GameObject.Instantiate(Mod.itemPrefabs[513], new Vector3(14.11435f, 0.3618026f, -0.3831904f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[594], new Vector3(13.90705f, 0.01460254f, -1.54019f), Quaternion.Euler(0f, 324.3596f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                GameObject.Instantiate(Mod.itemPrefabs[93], new Vector3(0.9440489f, 0.00280261f, 15.13881f), Quaternion.Euler(0f, 327.9549f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[92], new Vector3(0.9536486f, 0.03980255f, 15.15481f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                GameObject moneyObject = GameObject.Instantiate(Mod.itemPrefabs[203], new Vector3(-4.80595f, 0.01010263f, -7.312191f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                customItemWrapper = moneyObject.GetComponent<CustomItemWrapper>();
                customItemWrapper.stack = 80000;
                moneyObject = GameObject.Instantiate(Mod.itemPrefabs[203], new Vector3(-0.6729507f, 0.3903027f, 4.17981f), Quaternion.Euler(0f, 314.1891f, 0f), itemRoot);
                customItemWrapper = moneyObject.GetComponent<CustomItemWrapper>();
                customItemWrapper.stack = 175000;
                moneyObject = GameObject.Instantiate(Mod.itemPrefabs[203], new Vector3(11.68705f, 0.1943026f, -1.87019f), Quaternion.Euler(0f, 68.00121f, 0f), itemRoot);
                customItemWrapper = moneyObject.GetComponent<CustomItemWrapper>();
                customItemWrapper.stack = 200000;
                moneyObject = GameObject.Instantiate(Mod.itemPrefabs[203], new Vector3(11.74695f, 0.1985025f, -1.77169f), Quaternion.Euler(0f, 30.20087f, 0f), itemRoot);
                customItemWrapper = moneyObject.GetComponent<CustomItemWrapper>();
                customItemWrapper.stack = 45000;
                GameObject.Instantiate(IM.OD["CombatKnife"].GetGameObject(), new Vector3(-3.296951f, 0.03210258f, 0.4808097f), Quaternion.Euler(358.7422f, 318.3018f, 270.2902f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[568], new Vector3(16.10405f, 0.08310258f, -2.71819f), Quaternion.Euler(0f, 0f, 291.3354f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[567], new Vector3(0.6410007f, 0.03299999f, 15.506f), Quaternion.Euler(0f, 0f, 90f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[586], new Vector3(14.22805f, 0.03710258f, -0.2551903f), Quaternion.Euler(0f, 292.561f, 270f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[436], new Vector3(-4.393351f, 0.1706026f, 0.6231097f), Quaternion.Euler(270f, 271.9656f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[436], new Vector3(0.8820486f, 1.820103f, 4.05281f), Quaternion.Euler(272.9363f, 269.8858f, 89.99997f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[413], new Vector3(1.112049f, 0.1021026f, -1.00119f), Quaternion.Euler(82.1389f, 122.4497f, 8.344072f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[510], new Vector3(16.28405f, -0.04089737f, -2.97019f), Quaternion.Euler(270f, 261.68f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[511], new Vector3(12.28205f, -0.02909744f, -0.5291904f), Quaternion.Euler(270f, 299.4444f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[480], new Vector3(-6.411951f, 0.3561025f, -5.55619f), Quaternion.Euler(272.2496f, 4.349851E-05f, 160.9452f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[518], new Vector3(11.73005f, 0.06210256f, -4.120191f), Quaternion.Euler(0f, 49.91647f, 0f), itemRoot);
                GameObject.Instantiate(Mod.itemPrefabs[514], new Vector3(-4.182951f, 0.1709025f, 11.94181f), Quaternion.Euler(319.921f, 331.6011f, 3.973541f), itemRoot);
                GameObject.Instantiate(IM.OD["MP5A4"].GetGameObject(), new Vector3(0.7950497f, 0.6081026f, 7.395809f), Quaternion.Euler(0f, 26.78772f, 270f), itemRoot);
                GameObject.Instantiate(IM.OD["PP19Vityaz"].GetGameObject(), new Vector3(-0.3019505f, 0.1272025f, 3.753809f), Quaternion.Euler(0.1332195f, 79.69434f, 89.81905f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineMp530rndStraight"].GetGameObject(), new Vector3(0.2940483f, 0.4071026f, 4.71781f), Quaternion.Euler(0.1332366f, 104.3259f, 89.81904f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineMp530rndStraight"].GetGameObject(), new Vector3(0.3930492f, 0.4071026f, 4.71281f), Quaternion.Euler(0.08480537f, 90.13348f, 89.79189f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineMp530rndStraight"].GetGameObject(), new Vector3(0.5190487f, 0.4071026f, 4.64981f), Quaternion.Euler(0.1998774f, 130.7681f, 89.8973f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazinePP19Vityaz30rnd"].GetGameObject(), new Vector3(0.4270496f, 0.1021026f, 4.14481f), Quaternion.Euler(0.1820061f, 122.0492f, 89.86818f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazinePP19Vityaz30rnd"].GetGameObject(), new Vector3(0.3400497f, 0.1021026f, 4.13681f), Quaternion.Euler(0.1820061f, 122.0492f, 89.86818f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazinePP19Vityaz30rnd"].GetGameObject(), new Vector3(0.2160492f, 0.1021026f, 4.06881f), Quaternion.Euler(0.08534154f, 90.28258f, 89.79212f), itemRoot);
                GameObject.Instantiate(IM.OD["M9A3"].GetGameObject(), new Vector3(11.14705f, 0.2051027f, -1.68719f), Quaternion.Euler(0.08534154f, 90.28258f, 89.79212f), itemRoot);
                GameObject.Instantiate(IM.OD["CZ75Shadow"].GetGameObject(), new Vector3(15.75105f, 0.03310263f, -1.46119f), Quaternion.Euler(0.2009627f, 184.5467f, 90.10056f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineM9A1"].GetGameObject(), new Vector3(11.16405f, 0.2041025f, -1.83819f), Quaternion.Euler(0.02504972f, 74.36313f, 89.77668f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineM9A1"].GetGameObject(), new Vector3(11.22505f, 0.2041025f, -1.89619f), Quaternion.Euler(0.002482774f, 68.59618f, 89.7753f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineM9A1"].GetGameObject(), new Vector3(10.98705f, 0.2051027f, -1.68019f), Quaternion.Euler(359.7862f, 355.9008f, 89.93082f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineM9A1"].GetGameObject(), new Vector3(11.21405f, 0.2051027f, -1.50319f), Quaternion.Euler(359.776f, 333.3978f, 90.0179f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineM9A1"].GetGameObject(), new Vector3(11.30105f, 0.2051027f, -1.38819f), Quaternion.Euler(359.794f, 314.4457f, 90.08968f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineCZ75Shadow"].GetGameObject(), new Vector3(15.89105f, 0.03310263f, -1.35319f), Quaternion.Euler(0.1624631f, 201.6643f, 90.15526f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineCZ75Shadow"].GetGameObject(), new Vector3(15.87605f, 0.03310263f, -1.22519f), Quaternion.Euler(0.1624631f, 201.6643f, 90.15526f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineCZ75Shadow"].GetGameObject(), new Vector3(16.01305f, 0.03210258f, -1.44519f), Quaternion.Euler(0.126526f, 213.6976f, 90.18571f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineCZ75Shadow"].GetGameObject(), new Vector3(16.03705f, 0.03210258f, -1.57219f), Quaternion.Euler(0.1118922f, 218.1005f, 90.19489f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineCZ75Shadow"].GetGameObject(), new Vector3(16.33705f, 0.03110254f, -1.62219f), Quaternion.Euler(0.2137769f, 140.0049f, 89.93072f), itemRoot);
                GameObject.Instantiate(IM.OD["M4A1v2Rightie"].GetGameObject(), new Vector3(8.036049f, 0.4011025f, -4.61919f), Quaternion.Euler(0.06372909f, 231.488f, 90.21549f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineStanag2"].GetGameObject(), new Vector3(7.859049f, 0.4021025f, -4.53319f), Quaternion.Euler(0.05014384f, 235.0685f, 90.21905f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineStanag2"].GetGameObject(), new Vector3(7.761049f, 0.4021025f, -4.608191f), Quaternion.Euler(0.01802146f, 243.3631f, 90.22398f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineStanag2"].GetGameObject(), new Vector3(8.064049f, 0.4021025f, -4.38819f), Quaternion.Euler(359.7971f, 312.504f, 90.0966f), itemRoot);
                GameObject.Instantiate(IM.OD["AK74N"].GetGameObject(), new Vector3(7.153049f, 0.4011025f, -5.07319f), Quaternion.Euler(359.9211f, 268.5323f, 90.21038f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineAK74N"].GetGameObject(), new Vector3(7.389049f, 0.4011025f, -4.85919f), Quaternion.Euler(359.9642f, 257.1226f, 90.22184f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineAK74N"].GetGameObject(), new Vector3(7.376049f, 0.4011025f, -4.76219f), Quaternion.Euler(0.009509331f, 245.5376f, 90.2245f), itemRoot);
                GameObject.Instantiate(IM.OD["MagazineAK74N"].GetGameObject(), new Vector3(7.210049f, 0.4011025f, -4.81319f), Quaternion.Euler(359.8211f, 300.7395f, 90.13593f), itemRoot);
                GameObject.Instantiate(IM.OD["PinnedGrenadeM67"].GetGameObject(), new Vector3(-0.3319511f, 0.4461026f, 4.372809f), Quaternion.Euler(359.4122f, 24.90105f, 271.266f), itemRoot);
                GameObject.Instantiate(IM.OD["PinnedGrenadeF1Russia"].GetGameObject(), new Vector3(-0.3249512f, 0.4461026f, 4.47281f), Quaternion.Euler(359.9848f, 0.6241663f, 271.3957f), itemRoot);
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[711], new Vector3(-3.788952f, 0.03410256f, 1.97721f), Quaternion.Euler(0f, 348.3584f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[706], new Vector3(10.97705f, 0.04020262f, -1.23219f), Quaternion.Euler(0f, 97.96564f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[684], new Vector3(7.163049f, 0.03710258f, -4.63719f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[684], new Vector3(4.257049f, 0.3941026f, -5.22419f), Quaternion.Euler(0f, 70.93663f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                ammoBoxObject = GameObject.Instantiate(Mod.itemPrefabs[684], new Vector3(4.222049f, 0.3941026f, -5.41819f), Quaternion.Euler(90f, 102f, 0f), itemRoot);
                customItemWrapper = ammoBoxObject.GetComponent<CustomItemWrapper>();
                asMagazine = customItemWrapper.physObj as FVRFireArmMagazine;
                for (int i = 0; i < customItemWrapper.maxAmount; ++i)
                {
                    asMagazine.AddRound(customItemWrapper.roundClass, false, false);
                }
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[619], new Vector3(0.5500488f, 0.03410256f, 33.48181f), Quaternion.Euler(0f, 343.748f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[619], new Vector3(0.8410492f, 0.1321025f, 33.00081f), Quaternion.Euler(0f, 93.00641f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[633], new Vector3(-0.3458996f, 0.09630001f, 32.2702f), Quaternion.Euler(332.3804f, 73.96642f, 1.927157E-06f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[633], new Vector3(-0.2215977f, 0.03030002f, 32.30628f), Quaternion.Euler(0f, 345.1047f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[182], new Vector3(14.78605f, 0.002902627f, -0.4561903f), Quaternion.Euler(0f, 0f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;
                consumableObject = GameObject.Instantiate(Mod.itemPrefabs[180], new Vector3(-8.410952f, 0.01550257f, -7.51019f), Quaternion.Euler(0f, 56.62873f, 0f), itemRoot);
                consumableCIW = consumableObject.GetComponent<CustomItemWrapper>();
                consumableCIW.amount = consumableCIW.maxAmount;

                // Instantiate areas
                baseAreaManagers = new List<BaseAreaManager>();
                for (int i = 0; i < 22; ++i)
                {
                    BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<BaseAreaManager>();
                    currentBaseAreaManager.baseManager = this;
                    currentBaseAreaManager.areaIndex = i;
                    currentBaseAreaManager.level = i == 3 ? 1 : 0; // Stash starts at level 1
                    currentBaseAreaManager.constructing = false;
                    currentBaseAreaManager.slotItems = new GameObject[0];

                    baseAreaManagers.Add(currentBaseAreaManager);
                }

                // Instantiate other
                if (TraderStatus.waitingQuestConditions == null)
                {
                    TraderStatus.foundTasks = new Dictionary<string, TraderTask>();
                    TraderStatus.foundTaskConditions = new Dictionary<string, List<TraderTaskCondition>>();
                    TraderStatus.waitingQuestConditions = new Dictionary<string, List<TraderTaskCondition>>();
                    TraderStatus.waitingVisibilityConditions = new Dictionary<string, List<TraderTaskCondition>>();
                }
                else
                {
                    TraderStatus.foundTasks.Clear();
                    TraderStatus.foundTaskConditions.Clear();
                    TraderStatus.waitingQuestConditions.Clear();
                    TraderStatus.waitingVisibilityConditions.Clear();
                }
                Mod.traderStatuses = new TraderStatus[8];
                for (int i = 0; i < 8; i++)
                {
                    Mod.traderStatuses[i] = new TraderStatus(null, i, Mod.traderBaseDB[i]["nickname"].ToString(), 0, 0, i == 7 ? false : true, Mod.traderBaseDB[i]["currency"].ToString(), Mod.traderAssortDB[i], Mod.traderCategoriesDB[i]);
                }
                for (int i = 0; i < 8; i++)
                {
                    Mod.traderStatuses[i].Init();
                }

                // Add active tasks to player status list
                // Also check each condition for visibility
                foreach (KeyValuePair<string, TraderTask> task in TraderStatus.foundTasks)
                {
                    if (task.Value.taskState == TraderTask.TaskState.Active || task.Value.taskState == TraderTask.TaskState.Complete)
                    {
                        Mod.playerStatusManager.AddTask(task.Value);
                    }
                    else
                    {
                        task.Value.statusListElement = null;
                    }

                    foreach(TraderTaskCondition condition in task.Value.startConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach(TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null) 
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if(condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }
                    }

                    foreach(TraderTaskCondition condition in task.Value.completionConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach(TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null) 
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if(condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }

                        // Add condition to completion list if necessary
                        if (condition.visible)
                        {
                            if(condition.conditionType == TraderTaskCondition.ConditionType.CounterCreator)
                            {
                                foreach (TraderTaskCounterCondition counterCondition in condition.counters)
                                {
                                    if (Mod.taskCompletionCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
                                    {
                                        Mod.taskCompletionCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
                                    }
                                    else
                                    {
                                        List<TraderTaskCounterCondition> newList = new List<TraderTaskCounterCondition>();
                                        Mod.taskCompletionCounterConditionsByType.Add(counterCondition.counterConditionType, newList);
                                        newList.Add(counterCondition);
                                    }
                                }
                            }
                            else
                            {
                                if (Mod.taskCompletionConditionsByType.ContainsKey(condition.conditionType))
                                {
                                    Mod.taskCompletionConditionsByType[condition.conditionType].Add(condition);
                                }
                                else
                                {
                                    List<TraderTaskCondition> newList = new List<TraderTaskCondition>();
                                    Mod.taskCompletionConditionsByType.Add(condition.conditionType, newList);
                                    newList.Add(condition);
                                }
                            }
                        }
                    }

                    foreach(TraderTaskCondition condition in task.Value.failConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach(TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null) 
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if(condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }
                    }
                }

                // Init exploration trigger bools
                if (Mod.triggeredExplorationTriggers == null)
                {
                    Mod.triggeredExplorationTriggers = new List<List<bool>>();
                }
                else
                {
                    Mod.triggeredExplorationTriggers.Clear();
                }
                for(int i=0; i < 12; ++i)
                {
                    Mod.triggeredExplorationTriggers.Add(new List<bool>());
                }

                // Init lists
                UpdateBaseInventory();
                if (Mod.playerInventory == null)
                {
                    Mod.playerInventory = new Dictionary<string, int>();
                    Mod.playerInventoryObjects = new Dictionary<string, List<GameObject>>();
                }

                // Setup tutorial
                SetupTutorial();

                scavTimer = 0;

                Mod.playerInventory.Clear();
                Mod.playerInventoryObjects.Clear();
                Mod.insuredItems = new List<InsuredSet>();

                Mod.preventLoadMagUpdateLists = false;

                return;
            }

            // Clear other active slots since we shouldn't have any on load
            Mod.otherActiveSlots.Clear();

            // Load player status if not loading in from a raid
            long secondsSinceSave = GetTimeSeconds() - (long)data["time"];
            float minutesSinceSave = secondsSinceSave / 60.0f;
            if (!Mod.justFinishedRaid || Mod.chosenCharIndex == 1)
            {
                Mod.level = (int)data["level"];
                Mod.playerStatusManager.UpdatePlayerLevel();
                Mod.experience = (int)data["experience"];
                Mod.health = data["health"].ToObject<float[]>();
                for(int i=0; i < Mod.health.Length; ++i)
                {
                    Mod.health[i] += Mathf.Min(Mod.currentHealthRates[i] * minutesSinceSave, Mod.currentMaxHealth[i]);
                }
                Mod.maxHydration = (float)data["maxHydration"];
                Mod.hydration = Mathf.Min((float)data["hydration"] + Mod.currentHydrationRate * minutesSinceSave, Mod.maxHydration);
                Mod.maxEnergy = (float)data["maxEnergy"];
                Mod.energy = Mathf.Min((float)data["energy"] + Mod.currentEnergyRate * minutesSinceSave, Mod.maxEnergy);
                Mod.stamina = (float)data["stamina"];
                Mod.maxStamina = (float)data["maxStamina"];
                Mod.weight = (int)data["weight"];
                Mod.totalRaidCount = (int)data["totalRaidCount"];
                Mod.runThroughRaidCount = (int)data["runThroughRaidCount"];
                Mod.survivedRaidCount = (int)data["survivedRaidCount"];
                Mod.MIARaidCount = (int)data["MIARaidCount"];
                Mod.KIARaidCount = (int)data["KIARaidCount"];
                Mod.failedRaidCount = (int)data["failedRaidCount"];
                Mod.skills = new Skill[64];
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i] = new Skill();
                    Mod.skills[i].progress = (float)data["skills"][i]["progress"];
                    Mod.skills[i].currentProgress = Mod.skills[i].progress;

                    if(i == 0)
                    {
                        float enduranceLevel = Mod.skills[0].progress / 100;
                        Mod.maxStamina += (float)data["maxStamina"] / 100 * enduranceLevel;

                        if (enduranceLevel >= 51)
                        {
                            Mod.maxStamina += 20;
                        }
                    }

                    // 0-6 unless 4 physical
                    // 28-53 unless 52 practical
                    // 54-63 special
                    if (i >= 0 && i <= 6 && i != 4)
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Physical;
                    }
                    else if (i >= 28 && i <= 53 && i != 52)
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Practical;
                    }
                    else if (i >= 54 && i <= 63)
                    {
                        Mod.skills[i].skillType = Skill.SkillType.Special;
                    }
                }
            }
            TraderStatus.fenceRestockTimer = (float)data["fenceRestockTimer"] - secondsSinceSave;

            if (Mod.justFinishedRaid)
            {
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }
            }

            scavTimer = (long)data["scavTimer"] - secondsSinceSave;

            // Instantiate items
            Transform itemsRoot = transform.GetChild(2);
            JArray loadedItems = (JArray)data["items"];
            for (int i = 0; i < loadedItems.Count; ++i)
            {
                JToken item = loadedItems[i];

                // If just finished raid as PMC, skip any items that are on player since we want to keep what player found in raid
                if (Mod.justFinishedRaid && Mod.chosenCharIndex == 0 && ((item["PhysicalObject"]["equipSlot"] != null && (int)item["PhysicalObject"]["equipSlot"] != -1) || (int)item["PhysicalObject"]["heldMode"] != 0 || (int)item["PhysicalObject"]["m_quickBeltSlot"] != -1 || item["pocketSlotIndex"] != null || item["isRightShoulder"] != null))
                {
                    continue;
                }

                LoadSavedItem(itemsRoot, item);
            }

            // Load scav return items
            if (data["scavReturnItems"] != null)
            {
                Transform scavReturnNodeParent = transform.GetChild(1).GetChild(25);
                JArray loadedScavReturnItems = (JArray)data["scavReturnItems"];
                for (int i = 0; i < loadedScavReturnItems.Count; ++i)
                {
                    if (loadedScavReturnItems[i] == null || loadedScavReturnItems[i].Type == JTokenType.Null)
                    {
                        continue;
                    }

                    LoadSavedItem(scavReturnNodeParent.GetChild(i), loadedScavReturnItems[i]);
                }
            }

            // Check for insuredSets
            if (Mod.insuredItems == null) 
            {
                Mod.insuredItems = new List<InsuredSet>();
            }
            if (data["insuredSets"] != null)
            {
                JArray loadedInsuredSets = (JArray)data["insuredSets"];

                for (int i = 0; i < loadedInsuredSets.Count; ++i)
                {
                    InsuredSet newInsuredSet = new InsuredSet();
                    newInsuredSet.returnTime = (long)loadedInsuredSets[i]["returnTime"];
                    newInsuredSet.items = loadedInsuredSets[i]["items"].ToObject<Dictionary<string, int>>();
                    Mod.insuredItems.Add(newInsuredSet);
                }
            }

            // Count each type of item we have
            UpdateBaseInventory();
            Mod.UpdatePlayerInventory();

            Mod.preventLoadMagUpdateLists = false;

            // Instantiate areas
            baseAreaManagers = new List<BaseAreaManager>();
            for (int i = 0; i < 22; ++i)
            {
                BaseAreaManager currentBaseAreaManager = transform.GetChild(1).GetChild(i).gameObject.AddComponent<BaseAreaManager>();
                Transform slotsParent = currentBaseAreaManager.transform.GetChild(currentBaseAreaManager.transform.childCount - 1);
                currentBaseAreaManager.baseManager = this;
                currentBaseAreaManager.areaIndex = i;
                if (data["areas"] != null)
                {
                    JArray loadedAreas = (JArray)data["areas"];
                    currentBaseAreaManager.level = (int)loadedAreas[i]["level"];
                    currentBaseAreaManager.constructing = (bool)loadedAreas[i]["constructing"];
                    currentBaseAreaManager.constructionTimer = (float)loadedAreas[i]["constructTimer"] - secondsSinceSave;
                    if (slotsParent.childCount > 0)
                    {
                        currentBaseAreaManager.slotItems = new GameObject[slotsParent.GetChild(currentBaseAreaManager.level).childCount];
                    }
                    else
                    {
                        currentBaseAreaManager.slotItems = new GameObject[0];
                    }
                    if (loadedAreas[i]["slots"] != null)
                    {
                        JArray loadedAreaSlot = (JArray)loadedAreas[i]["slots"];
                        int slotIndex = 0;
                        foreach (JToken item in loadedAreaSlot)
                        {
                            if (item == null || item.Type == JTokenType.Null)
                            {
                                currentBaseAreaManager.slotItems[slotIndex] = null;
                            }
                            else
                            {
                                currentBaseAreaManager.slotItems[slotIndex] = LoadSavedItem(null, item);
                            }
                            ++slotIndex;
                        }
                    }
                }
                else
                {
                    currentBaseAreaManager.level = 0;
                    currentBaseAreaManager.constructing = false;
                    currentBaseAreaManager.slotItems = new GameObject[0];
                }

                baseAreaManagers.Add(currentBaseAreaManager);
            }

            // Load trader statuses if not loading in from a raid
            if (!Mod.justFinishedRaid)
            {
                if (TraderStatus.waitingQuestConditions == null)
                {
                    TraderStatus.foundTasks = new Dictionary<string, TraderTask>();
                    TraderStatus.foundTaskConditions = new Dictionary<string, List<TraderTaskCondition>>();
                    TraderStatus.waitingQuestConditions = new Dictionary<string, List<TraderTaskCondition>>();
                    TraderStatus.waitingVisibilityConditions = new Dictionary<string, List<TraderTaskCondition>>();
                }
                else
                {
                    TraderStatus.foundTasks.Clear();
                    TraderStatus.foundTaskConditions.Clear();
                    TraderStatus.waitingQuestConditions.Clear();
                    TraderStatus.waitingVisibilityConditions.Clear();
                }
                Mod.traderStatuses = new TraderStatus[8];
                if (data["traderStatuses"] == null)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        Mod.traderStatuses[i] = new TraderStatus(null, i, Mod.traderBaseDB[i]["nickname"].ToString(), 0, 0, i == 7 ? false : true, Mod.traderBaseDB[i]["currency"].ToString(), Mod.traderAssortDB[i], Mod.traderCategoriesDB[i]);
                    }
                }
                else
                {
                    JArray loadedTraderStatuses = (JArray)data["traderStatuses"];
                    for (int i = 0; i < 8; i++)
                    {
                        Mod.traderStatuses[i] = new TraderStatus(data["traderStatuses"][i], i, Mod.traderBaseDB[i]["nickname"].ToString(), (int)loadedTraderStatuses[i]["salesSum"], (float)loadedTraderStatuses[i]["standing"], (bool)loadedTraderStatuses[i]["unlocked"], Mod.traderBaseDB[i]["currency"].ToString(), Mod.traderAssortDB[i], Mod.traderCategoriesDB[i]);
                    }
                }
                for (int i = 0; i < 8; i++)
                {
                    Mod.traderStatuses[i].Init();
                    if (data["traderStatuses"][i]["itemsToWaitForUnlock"] != null)
                    {
                        Mod.traderStatuses[i].itemsToWaitForUnlock = data["traderStatuses"][i]["itemsToWaitForUnlock"].ToObject<List<string>>();
                    }
                }

                // Add active tasks to player status list
                foreach (KeyValuePair<string, TraderTask> task in TraderStatus.foundTasks)
                {
                    if (task.Value.taskState == TraderTask.TaskState.Active || task.Value.taskState == TraderTask.TaskState.Complete)
                    {
                        Mod.playerStatusManager.AddTask(task.Value);
                    }
                    else
                    {
                        task.Value.statusListElement = null;
                    }

                    // Check for condition visibility
                    foreach (TraderTaskCondition condition in task.Value.startConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach (TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null)
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if (condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }
                    }

                    foreach (TraderTaskCondition condition in task.Value.completionConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach (TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null)
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if (condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }

                        // Add condition to completion list if necessary
                        if (condition.visible)
                        {
                            if (condition.conditionType == TraderTaskCondition.ConditionType.CounterCreator)
                            {
                                foreach (TraderTaskCounterCondition counterCondition in condition.counters)
                                {
                                    if (Mod.taskCompletionCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
                                    {
                                        Mod.taskCompletionCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
                                    }
                                    else
                                    {
                                        List<TraderTaskCounterCondition> newList = new List<TraderTaskCounterCondition>();
                                        Mod.taskCompletionCounterConditionsByType.Add(counterCondition.counterConditionType, newList);
                                        newList.Add(counterCondition);
                                    }
                                }
                            }
                            else
                            {
                                if (Mod.taskCompletionConditionsByType.ContainsKey(condition.conditionType))
                                {
                                    Mod.taskCompletionConditionsByType[condition.conditionType].Add(condition);
                                }
                                else
                                {
                                    List<TraderTaskCondition> newList = new List<TraderTaskCondition>();
                                    Mod.taskCompletionConditionsByType.Add(condition.conditionType, newList);
                                    newList.Add(condition);
                                }
                            }
                        }
                    }

                    foreach (TraderTaskCondition condition in task.Value.failConditions)
                    {
                        bool visiblityFulfilled = true;
                        if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
                        {
                            foreach (TraderTaskCondition visCond in condition.visibilityConditions)
                            {
                                if (!visCond.fulfilled)
                                {
                                    visiblityFulfilled = false;
                                    break;
                                }
                            }
                        }
                        condition.visible = visiblityFulfilled;
                        if (condition.marketListElement != null)
                        {
                            condition.marketListElement.SetActive(visiblityFulfilled);
                        }
                        if (condition.statusListElement != null)
                        {
                            condition.statusListElement.SetActive(visiblityFulfilled);
                        }
                    }
                }
            }

            // Load triggered exploration triggers if not loading in from raid
            if (!Mod.justFinishedRaid)
            {
                if (Mod.triggeredExplorationTriggers == null)
                {
                    Mod.triggeredExplorationTriggers = new List<List<bool>>();
                }
                else
                {
                    Mod.triggeredExplorationTriggers.Clear();
                }
                if (data["triggeredExplorationTriggers"] != null)
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        Mod.triggeredExplorationTriggers.Add(data["triggeredExplorationTriggers"][i].ToObject<List<bool>>());
                    }
                }
                else
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        Mod.triggeredExplorationTriggers.Add(new List<bool>());
                    }
                }
            }
        }

        private void SetupTutorial()
        {
            Transform tutorialTransform = transform.GetChild(0).GetChild(1);
            tutorialTransform.gameObject.SetActive(true);
            AudioSource hoverAudio = tutorialTransform.GetChild(21).GetComponent<AudioSource>();
            FVRPointable backgroundPointable = tutorialTransform.gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;
            Transform controlsParent = null;
            switch (Mod.leftHand.fvrHand.CMode)
            {
                case ControlMode.Index:
                    controlsParent = tutorialTransform.GetChild(1);
                    break;
                case ControlMode.Oculus:
                    controlsParent = tutorialTransform.GetChild(2);
                    break;
                case ControlMode.Vive:
                    controlsParent = tutorialTransform.GetChild(3);
                    break;
                case ControlMode.WMR:
                    controlsParent = tutorialTransform.GetChild(4);
                    break;
            }

            // Setup welcome
            // Skip
            PointableButton pointableButton = tutorialTransform.GetChild(0).GetChild(1).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
            // Next
            pointableButton = tutorialTransform.GetChild(0).GetChild(2).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(tutorialTransform.GetChild(0), controlsParent); });

            // Setup controls buttons
            for(int i=0; i<controlsParent.childCount;++i)
            {
                // Skip
                pointableButton = controlsParent.GetChild(i).GetChild(1).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
                // Next
                pointableButton = controlsParent.GetChild(i).GetChild(2).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                Transform toHide = null;
                Transform toShow = null;
                if(i == controlsParent.childCount - 1)
                {
                    toHide = controlsParent;
                    toShow = tutorialTransform.GetChild(5);

                }
                else
                {
                    toHide = controlsParent.GetChild(i);
                    toShow = controlsParent.GetChild(i + 1);
                }
                pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(toHide, toShow); });
            }

            // Setup remaining tutorial screens
            for (int i=5; i < 18; ++i)
            {
                // Skip
                pointableButton = tutorialTransform.GetChild(i).GetChild(1).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
                // Next
                pointableButton = tutorialTransform.GetChild(i).GetChild(2).gameObject.AddComponent<PointableButton>();
                pointableButton.SetButton();
                pointableButton.MaxPointingRange = 5;
                pointableButton.hoverGraphics = new GameObject[2];
                pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
                pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
                pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
                pointableButton.toggleTextColor = true;
                pointableButton.hoverSound = hoverAudio;
                Transform toHide = tutorialTransform.GetChild(i);
                Transform toShow = tutorialTransform.GetChild(i+1);
                pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(toHide, toShow); });
            }

            // Setup end
            // Skip
            pointableButton = tutorialTransform.GetChild(18).GetChild(1).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(OnTutorialSkipClick);
            // Next
            pointableButton = tutorialTransform.GetChild(18).GetChild(2).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnTutorialNextClick(tutorialTransform, null); });
            // Donate
            pointableButton = tutorialTransform.GetChild(18).GetChild(3).gameObject.AddComponent<PointableButton>();
            pointableButton.SetButton();
            pointableButton.MaxPointingRange = 5;
            pointableButton.hoverGraphics = new GameObject[2];
            pointableButton.hoverGraphics[0] = pointableButton.transform.GetChild(0).gameObject; // Background
            pointableButton.hoverGraphics[1] = pointableButton.transform.GetChild(1).gameObject; // Hover
            pointableButton.buttonText = pointableButton.transform.GetChild(2).GetComponent<Text>();
            pointableButton.toggleTextColor = true;
            pointableButton.hoverSound = hoverAudio;
            pointableButton.Button.onClick.AddListener(() => { OnDonateClick(tutorialTransform.GetChild(18).GetChild(5).gameObject); });
        }

        public void UpdateBaseInventory()
        {
            if(Mod.baseInventory == null)
            {
                Mod.baseInventory = new Dictionary<string, int>();
            }
            if(baseInventoryObjects == null)
            {
                baseInventoryObjects = new Dictionary<string, List<GameObject>>();
            }

            Mod.baseInventory.Clear();
            baseInventoryObjects.Clear();

            Transform itemsRoot = transform.GetChild(2);

            foreach (Transform item in itemsRoot)
            {
                AddToBaseInventory(item, true);
            }

            // Also add items in trade volume
            foreach(Transform item in transform.GetChild(1).GetChild(24).GetChild(1).GetChild(1))
            {
                AddToBaseInventory(item, true);
            }

            // Also add items from scav raid return nodes
            foreach(Transform node in transform.GetChild(1).GetChild(25))
            {
                if (node.childCount > 0) 
                {
                    AddToBaseInventory(node.GetChild(0), true);
                }
            }
        }

        public void AddToBaseInventory(Transform item, bool updateTypeLists)
        {
            CustomItemWrapper customItemWrapper = item.GetComponent<CustomItemWrapper>();
            VanillaItemDescriptor vanillaItemDescriptor = item.GetComponent<VanillaItemDescriptor>();
            FVRPhysicalObject physObj = item.GetComponent<FVRPhysicalObject>();
            if (physObj == null || physObj.ObjectWrapper == null)
            {
                return; // Grenade pin for example, has no wrapper
            }
            string itemID = physObj.ObjectWrapper.ItemID;
            if (Mod.baseInventory.ContainsKey(itemID))
            {
                Mod.baseInventory[itemID] += customItemWrapper != null ? (customItemWrapper.stack > 0 ? customItemWrapper.stack : 1) : 1;
                baseInventoryObjects[itemID].Add(item.gameObject);
            }
            else
            {
                Mod.baseInventory.Add(itemID, customItemWrapper != null ? (customItemWrapper.stack > 0 ? customItemWrapper.stack : 1) : 1);
                baseInventoryObjects.Add(itemID, new List<GameObject> { item.gameObject });
            }

            if (updateTypeLists)
            {
                if (customItemWrapper != null)
                {
                    if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine boxMagazine = customItemWrapper.GetComponent<FVRFireArmMagazine>();
                        foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                        {
                            if (loadedRound == null)
                            {
                                break;
                            }

                            string roundName = AM.GetFullRoundName(boxMagazine.RoundType, loadedRound.LR_Class);

                            if (Mod.roundsByType.ContainsKey(boxMagazine.RoundType))
                            {
                                if (Mod.roundsByType[boxMagazine.RoundType] == null)
                                {
                                    Mod.roundsByType[boxMagazine.RoundType] = new Dictionary<string, int>();
                                    Mod.roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                                }
                                else
                                {
                                    if (Mod.roundsByType[boxMagazine.RoundType].ContainsKey(roundName))
                                    {
                                        Mod.roundsByType[boxMagazine.RoundType][roundName] += 1;
                                    }
                                    else
                                    {
                                        Mod.roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                                    }
                                }
                            }
                            else
                            {
                                Mod.roundsByType.Add(boxMagazine.RoundType, new Dictionary<string, int>());
                                Mod.roundsByType[boxMagazine.RoundType].Add(roundName, 1);
                            }
                        }
                    }
                }

                if (vanillaItemDescriptor != null)
                {
                    if (vanillaItemDescriptor.physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = vanillaItemDescriptor.physObj as FVRFireArmMagazine;
                        if (Mod.magazinesByType.ContainsKey(asMagazine.MagazineType))
                        {
                            if (Mod.magazinesByType[asMagazine.MagazineType] == null)
                            {
                                Mod.magazinesByType[asMagazine.MagazineType] = new Dictionary<string, int>();
                                Mod.magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                            }
                            else
                            {
                                if (Mod.magazinesByType[asMagazine.MagazineType].ContainsKey(asMagazine.ObjectWrapper.DisplayName))
                                {
                                    Mod.magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] += 1;
                                }
                                else
                                {
                                    Mod.magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                                }
                            }
                        }
                        else
                        {
                            Mod.magazinesByType.Add(asMagazine.MagazineType, new Dictionary<string, int>());
                            Mod.magazinesByType[asMagazine.MagazineType].Add(asMagazine.ObjectWrapper.DisplayName, 1);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmClip)
                    {
                        Mod.LogInfo("3");
                        FVRFireArmClip asClip = vanillaItemDescriptor.physObj as FVRFireArmClip;
                        if (Mod.clipsByType.ContainsKey(asClip.ClipType))
                        {
                            if (Mod.clipsByType[asClip.ClipType] == null)
                            {
                                Mod.clipsByType[asClip.ClipType] = new Dictionary<string, int>();
                                Mod.clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                            }
                            else
                            {
                                if (Mod.clipsByType[asClip.ClipType].ContainsKey(asClip.ObjectWrapper.DisplayName))
                                {
                                    Mod.clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] += 1;
                                }
                                else
                                {
                                    Mod.clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                                }
                            }
                        }
                        else
                        {
                            Mod.clipsByType.Add(asClip.ClipType, new Dictionary<string, int>());
                            Mod.clipsByType[asClip.ClipType].Add(asClip.ObjectWrapper.DisplayName, 1);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = vanillaItemDescriptor.physObj as FVRFireArmRound;
                        if (Mod.roundsByType.ContainsKey(asRound.RoundType))
                        {
                            if (Mod.roundsByType[asRound.RoundType] == null)
                            {
                                Mod.roundsByType[asRound.RoundType] = new Dictionary<string, int>();
                                Mod.roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                            }
                            else
                            {
                                if (Mod.roundsByType[asRound.RoundType].ContainsKey(asRound.ObjectWrapper.DisplayName))
                                {
                                    Mod.roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] += 1;
                                }
                                else
                                {
                                    Mod.roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                                }
                            }
                        }
                        else
                        {
                            Mod.roundsByType.Add(asRound.RoundType, new Dictionary<string, int>());
                            Mod.roundsByType[asRound.RoundType].Add(asRound.ObjectWrapper.DisplayName, 1);
                        }
                    }
                }
            }

            // Check for more items that may be contained inside this one
            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == Mod.ItemType.Backpack || customItemWrapper.itemType == Mod.ItemType.Container || customItemWrapper.itemType == Mod.ItemType.Pouch)
                {
                    foreach (Transform innerItem in customItemWrapper.containerItemRoot)
                    {
                        AddToBaseInventory(innerItem, updateTypeLists);
                    }
                }
                else if (customItemWrapper.itemType == Mod.ItemType.Rig || customItemWrapper.itemType == Mod.ItemType.ArmoredRig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            AddToBaseInventory(innerItem.transform, updateTypeLists);
                        }
                    }
                }
            }
        }

        public static bool RemoveFromContainer(Transform item, CustomItemWrapper CIW, VanillaItemDescriptor VID)
        {
            if (item.transform.parent != null && item.transform.parent.parent != null)
            {
                CustomItemWrapper containerItemWrapper = item.transform.parent.parent.GetComponent<CustomItemWrapper>();
                if (containerItemWrapper != null && (containerItemWrapper.itemType == Mod.ItemType.Backpack ||
                                                    containerItemWrapper.itemType == Mod.ItemType.Container ||
                                                    containerItemWrapper.itemType == Mod.ItemType.Pouch))
                {
                    containerItemWrapper.currentWeight -= CIW != null ? CIW.currentWeight : VID.currentWeight;

                    containerItemWrapper.containingVolume -= CIW != null ? CIW.volumes[CIW.mode] : VID.volume;

                    // Reset cols of item so that they are non trigger again and can collide with the world and the container
                    FVRPhysicalObject physObj = item.GetComponent<FVRPhysicalObject>();
                    for (int i = containerItemWrapper.resetColPairs.Count - 1; i >= 0; --i)
                    {
                        if (containerItemWrapper.resetColPairs[i].physObj.Equals(physObj))
                        {
                            foreach (Collider col in containerItemWrapper.resetColPairs[i].colliders)
                            {
                                col.isTrigger = false;
                            }
                            containerItemWrapper.resetColPairs.RemoveAt(i);
                            break;
                        }
                    }

                    physObj.RecoverRigidbody();

                    return true;
                }
            }

            return false;
        }

        public void RemoveFromBaseInventory(Transform item, bool updateTypeLists)
        {
            CustomItemWrapper customItemWrapper = item.GetComponent<CustomItemWrapper>();
            VanillaItemDescriptor vanillaItemDescriptor = item.GetComponent<VanillaItemDescriptor>();
            FVRPhysicalObject physObj = item.GetComponent<FVRPhysicalObject>();
            if (physObj == null || physObj.ObjectWrapper == null)
            {
                return; // Grenade pin for example, has no wrapper
            }
            string itemID = physObj.ObjectWrapper.ItemID;
            if (Mod.baseInventory.ContainsKey(itemID))
            {
                Mod.baseInventory[itemID] -= customItemWrapper != null ? customItemWrapper.stack : 1;
                baseInventoryObjects[itemID].Remove(item.gameObject);
            }
            else
            {
                Mod.LogError("Attempting to remove " + itemID + " from base inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (Mod.baseInventory[itemID] == 0)
            {
                Mod.baseInventory.Remove(itemID);
                baseInventoryObjects.Remove(itemID);
            }

            if (updateTypeLists)
            {
                if (customItemWrapper != null)
                {
                    if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine boxMagazine = customItemWrapper.GetComponent<FVRFireArmMagazine>();
                        foreach (FVRLoadedRound loadedRound in boxMagazine.LoadedRounds)
                        {
                            if (loadedRound == null)
                            {
                                break;
                            }

                            string roundName = loadedRound.LR_ObjectWrapper.DisplayName;

                            if (Mod.roundsByType.ContainsKey(boxMagazine.RoundType))
                            {
                                if (Mod.roundsByType[boxMagazine.RoundType].ContainsKey(roundName))
                                {
                                    Mod.roundsByType[boxMagazine.RoundType][roundName] -= 1;
                                    if (Mod.roundsByType[boxMagazine.RoundType][roundName] == 0)
                                    {
                                        Mod.roundsByType[boxMagazine.RoundType].Remove(roundName);
                                    }
                                    if (Mod.roundsByType[boxMagazine.RoundType].Count == 0)
                                    {
                                        Mod.roundsByType.Remove(boxMagazine.RoundType);
                                    }
                                }
                                else
                                {
                                    Mod.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from base inventory but ammo name was not found in roundsByType:\n" + Environment.StackTrace);
                                }
                            }
                            else
                            {
                                Mod.LogError("Attempting to remove " + itemID + "  which is ammo box that contains ammo: " + roundName + " from base inventory but ammo type was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                    }
                }

                if (vanillaItemDescriptor != null)
                {
                    if (vanillaItemDescriptor.physObj is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine asMagazine = vanillaItemDescriptor.physObj as FVRFireArmMagazine;
                        if (Mod.magazinesByType.ContainsKey(asMagazine.MagazineType))
                        {
                            if (Mod.magazinesByType[asMagazine.MagazineType].ContainsKey(asMagazine.ObjectWrapper.DisplayName))
                            {
                                Mod.magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.magazinesByType[asMagazine.MagazineType][asMagazine.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.magazinesByType[asMagazine.MagazineType].Remove(asMagazine.ObjectWrapper.DisplayName);
                                }
                                if (Mod.magazinesByType[asMagazine.MagazineType].Count == 0)
                                {
                                    Mod.magazinesByType.Remove(asMagazine.MagazineType);
                                }
                            }
                            else
                            {
                                Mod.LogError("Attempting to remove " + itemID + "  which is mag from base inventory but its name was not found in magazinesByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is mag from base inventory but its type was not found in magazinesByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmClip)
                    {
                        FVRFireArmClip asClip = vanillaItemDescriptor.physObj as FVRFireArmClip;
                        if (Mod.clipsByType.ContainsKey(asClip.ClipType))
                        {
                            if (Mod.clipsByType[asClip.ClipType].ContainsKey(asClip.ObjectWrapper.DisplayName))
                            {
                                Mod.clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.clipsByType[asClip.ClipType][asClip.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.clipsByType[asClip.ClipType].Remove(asClip.ObjectWrapper.DisplayName);
                                }
                                if (Mod.clipsByType[asClip.ClipType].Count == 0)
                                {
                                    Mod.clipsByType.Remove(asClip.ClipType);
                                }
                            }
                            else
                            {
                                Mod.LogError("Attempting to remove " + itemID + "  which is clip from base inventory but its name was not found in clipsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is clip from base inventory but its type was not found in clipsByType:\n" + Environment.StackTrace);
                        }
                    }
                    else if (vanillaItemDescriptor.physObj is FVRFireArmRound)
                    {
                        FVRFireArmRound asRound = vanillaItemDescriptor.physObj as FVRFireArmRound;
                        if (Mod.roundsByType.ContainsKey(asRound.RoundType))
                        {
                            if (Mod.roundsByType[asRound.RoundType].ContainsKey(asRound.ObjectWrapper.DisplayName))
                            {
                                Mod.roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] -= 1;
                                if (Mod.roundsByType[asRound.RoundType][asRound.ObjectWrapper.DisplayName] == 0)
                                {
                                    Mod.roundsByType[asRound.RoundType].Remove(asRound.ObjectWrapper.DisplayName);
                                }
                                if (Mod.roundsByType[asRound.RoundType].Count == 0)
                                {
                                    Mod.roundsByType.Remove(asRound.RoundType);
                                }
                            }
                            else
                            {
                                Mod.LogError("Attempting to remove " + itemID + "  which is round from base inventory but its name was not found in roundsByType:\n" + Environment.StackTrace);
                            }
                        }
                        else
                        {
                            Mod.LogError("Attempting to remove " + itemID + "  which is round from base inventory but its type was not found in roundsByType:\n" + Environment.StackTrace);
                        }
                    }
                }
            }

            // Check for more items that may be contained inside this one
            if (customItemWrapper != null)
            {
                if (customItemWrapper.itemType == Mod.ItemType.Backpack || customItemWrapper.itemType == Mod.ItemType.Container || customItemWrapper.itemType == Mod.ItemType.Pouch)
                {
                    foreach (Transform innerItem in customItemWrapper.containerItemRoot)
                    {
                        RemoveFromBaseInventory(innerItem, updateTypeLists);
                    }
                }
                else if (customItemWrapper.itemType == Mod.ItemType.Rig || customItemWrapper.itemType == Mod.ItemType.ArmoredRig)
                {
                    foreach (GameObject innerItem in customItemWrapper.itemsInSlots)
                    {
                        if (innerItem != null)
                        {
                            RemoveFromBaseInventory(innerItem.transform, updateTypeLists);
                        }
                    }
                }
            }
        }

        private GameObject LoadSavedItem(Transform parent, JToken item, int locationIndex = -1, bool inAll = false)
        {
            Mod.LogInfo("Loading item "+item["PhysicalObject"]["ObjectWrapper"]["ItemID"]);
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

            GameObject itemObject = Instantiate<GameObject>(prefabToUse);

            itemObject.transform.parent = parent; // Set parent after so it can awake before doing anything, in case parent is inactive

            FVRPhysicalObject itemPhysicalObject = itemObject.GetComponentInChildren<FVRPhysicalObject>();
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;
            CustomItemWrapper customItemWrapper = itemObject.GetComponent<CustomItemWrapper>();
            VanillaItemDescriptor vanillaItemDescriptor = itemObject.GetComponent<VanillaItemDescriptor>();

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
                // Must set location index before beginning interaction because begin interactionpatch will consider this to be in hideout and will try to remove it from it
                // but it isnt in there yet
                if(customItemWrapper != null)
                {
                    customItemWrapper.takeCurrentLocation = false;
                    customItemWrapper.locationIndex = 0;
                }
                else
                {
                    vanillaItemDescriptor.takeCurrentLocation = false;
                    vanillaItemDescriptor.locationIndex = 0;
                }
                locationIndex = 0;
                hand.CurrentInteractable.BeginInteraction(hand);
            }

            // ObjectWrapper
            itemObjectWrapper.ItemID = item["PhysicalObject"]["ObjectWrapper"]["ItemID"].ToString();

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                FVRFireArm firearmPhysicalObject = itemPhysicalObject as FVRFireArm;

                // Build and load flagDict from saved lists
                if (item["PhysicalObject"]["flagDict"] != null)
                {
                    JObject loadedFlagDict = (JObject)item["PhysicalObject"]["flagDict"];
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
                    string rawContainerID = item["PhysicalObject"]["ammoContainer"]["itemID"].ToString();
                    bool internalMag = false;
                    int parsedContainerID = -1;
                    GameObject containerPrefabToUse = null;
                    if (int.TryParse(rawContainerID, out parsedContainerID))
                    {
                        // Custom mag, fetch from our own assets
                        containerPrefabToUse = Mod.itemPrefabs[parsedContainerID];
                    }
                    else if (rawContainerID.Equals("InternalMag"))
                    {
                        internalMag = true;
                    }
                    else
                    {
                        // Vanilla mag, fetch from game assets
                        containerPrefabToUse = IM.OD[rawContainerID].GetGameObject();
                    }

                    GameObject containerObject = internalMag ? firearmPhysicalObject.Magazine.gameObject : Instantiate<GameObject>(containerPrefabToUse);
                    FVRPhysicalObject containerPhysicalObject = containerObject.GetComponentInChildren<FVRPhysicalObject>();

                    if (firearmPhysicalObject.UsesClips && containerPhysicalObject is FVRFireArmClip)
                    {
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

                        // Make sure the clip doesnt take the current location index once awake
                        VanillaItemDescriptor clipVID = clipPhysicalObject.GetComponent<VanillaItemDescriptor>();
                        if (locationIndex != -1)
                        {
                            clipVID.takeCurrentLocation = false;
                            clipVID.locationIndex = locationIndex;
                        }

                        clipPhysicalObject.Load(firearmPhysicalObject);

                        // Store the clip's supposed local position so we can ensure it is correct later
                        Mod.attachmentLocalTransform.Add(new KeyValuePair<GameObject, object>(containerObject, firearmPhysicalObject.ClipMountPos));
                        Mod.attachmentCheckNeeded = 5;
                    }
                    else if (firearmPhysicalObject.UsesMagazines && containerPhysicalObject is FVRFireArmMagazine)
                    {
                        FVRFireArmMagazine magPhysicalObject = containerPhysicalObject as FVRFireArmMagazine;
                        magPhysicalObject.UsesVizInterp = false;

                        if (item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"] != null)
                        {
                            List<FireArmRoundClass> newLoadedRoundsInMag = new List<FireArmRoundClass>();
                            foreach (int round in item["PhysicalObject"]["ammoContainer"]["loadedRoundsInContainer"])
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

                        if (!internalMag)
                        {
                            // Make sure the mag doesnt take the current location index once awake
                            VanillaItemDescriptor magVID = magPhysicalObject.GetComponent<VanillaItemDescriptor>();
                            if (locationIndex != -1)
                            {
                                magVID.takeCurrentLocation = false;
                                magVID.locationIndex = locationIndex;
                            }

                            magPhysicalObject.Load(firearmPhysicalObject);

                            // Store the mag's supposed local position so we can ensure it is correct later
                            Mod.attachmentLocalTransform.Add(new KeyValuePair<GameObject, object>(containerObject, firearmPhysicalObject.MagazineMountPos));
                            Mod.attachmentCheckNeeded = 5;
                        }
                    }
                }

                // Set to right shoulder if this was saved in it
                if (item["isRightShoulder"] != null)
                {
                    itemPhysicalObject.SetQuickBeltSlot(Mod.rightShoulderSlot);

                    BeginInteractionPatch.SetItemLocationIndex(0, null, vanillaItemDescriptor);

                    Mod.rightShoulderObject = itemObject;
                    itemObject.SetActive(false);
                }

                if(firearmPhysicalObject is ClosedBoltWeapon)
                {
                    (firearmPhysicalObject as ClosedBoltWeapon).CockHammer();
                }
                else if(firearmPhysicalObject is BoltActionRifle)
                {
                    (firearmPhysicalObject as BoltActionRifle).CockHammer();
                }
                else if(firearmPhysicalObject is TubeFedShotgun)
                {
                    (firearmPhysicalObject as TubeFedShotgun).CockHammer();
                }
                else if(firearmPhysicalObject is BreakActionWeapon)
                {
                    (firearmPhysicalObject as BreakActionWeapon).CockHammer();
                }
                // TODO: Might also have to set private fields in OpenBolt, LeverAction, etc
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

            // Custom item
            if (customItemWrapper != null)
            {
                if (inAll)
                {
                    customItemWrapper.inAll = true;
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(itemPhysicalObject, customItemWrapper, null);
                }

                customItemWrapper.itemType = (Mod.ItemType)(int)item["itemType"];
                customItemWrapper.amount = (int)item["amount"];
                customItemWrapper.looted = (bool)item["looted"];
                customItemWrapper.insured = (bool)item["insured"];
                if(locationIndex != -1)
                {
                    customItemWrapper.takeCurrentLocation = false;
                    customItemWrapper.locationIndex = locationIndex;
                }

                // Armor
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    Mod.LogInfo("is armor");
                    customItemWrapper.armor = (float)item["PhysicalObject"]["armor"];
                    customItemWrapper.maxArmor = (float)item["PhysicalObject"]["maxArmor"];

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }
                }

                // Rig
                if (customItemWrapper.itemType == Mod.ItemType.ArmoredRig || customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    Mod.LogInfo("is rig");
                    bool equipped = (int)item["PhysicalObject"]["equipSlot"] != -1;
                    if (equipped)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["quickBeltSlotContents"] != null)
                    {
                        JArray loadedQBContents = (JArray)item["PhysicalObject"]["quickBeltSlotContents"];
                        for (int j = 0; j < loadedQBContents.Count; ++j)
                        {
                            if (loadedQBContents[j] == null || loadedQBContents[j].Type == JTokenType.Null)
                            {
                                customItemWrapper.itemsInSlots[j] = null;
                            }
                            else
                            {
                                customItemWrapper.itemsInSlots[j] = LoadSavedItem(null, loadedQBContents[j], customItemWrapper.locationIndex, equipped);
                                customItemWrapper.itemsInSlots[j].SetActive(false); // Inactive by default // TODO: If we ever save the mode of the rig, and therefore could load an open rig, then we should check this mode before setting active or inactive
                            }
                        }

                        //if (equipped)
                        //{
                        //    // Put inner items in their slots
                        //    for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
                        //    {
                        //        if (customItemWrapper.itemsInSlots[i] != null)
                        //        {
                        //            FVRPhysicalObject currentItemPhysObj = customItemWrapper.itemsInSlots[i].GetComponent<FVRPhysicalObject>();
                        //            if (currentItemPhysObj != null)
                        //            {
                        //                // Attach item to quick slot
                        //                FVRQuickBeltSlot quickBeltSlot = customItemWrapper.rigSlots[i];
                        //                currentItemPhysObj.SetQuickBeltSlot(quickBeltSlot);
                        //                currentItemPhysObj.SetParentage(null);
                        //            }
                        //        }
                        //    }
                        //}

                        // Update the current weight of the rig
                        CustomItemWrapper.SetCurrentWeight(customItemWrapper);

                        customItemWrapper.UpdateRigMode();
                    }
                }

                // Backpack
                if (customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    Mod.LogInfo("is backpack");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["backpackContents"] != null)
                    {
                        JArray loadedBPContents = (JArray)item["PhysicalObject"]["backpackContents"];
                        for (int j = 0; j < loadedBPContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.containerItemRoot, loadedBPContents[j], customItemWrapper.locationIndex, false);
                        }
                    }

                    customItemWrapper.UpdateBackpackMode();
                }

                // Container
                if (customItemWrapper.itemType == Mod.ItemType.Container)
                {
                    Mod.LogInfo("is container");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedContainerContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedContainerContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.containerItemRoot, loadedContainerContents[j], customItemWrapper.locationIndex, false);
                        }
                    }
                }

                // Pouch
                if (customItemWrapper.itemType == Mod.ItemType.Pouch)
                {
                    Mod.LogInfo("is Pouch");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedPouchContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedPouchContents.Count; ++j)
                        {
                            LoadSavedItem(customItemWrapper.containerItemRoot, loadedPouchContents[j], customItemWrapper.locationIndex, false);
                        }
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                //{
                //    Mod.LogInfo("is ammo box");
                //}

                // Money
                if (customItemWrapper.itemType == Mod.ItemType.Money)
                {
                    Mod.LogInfo("is money");

                    customItemWrapper.stack = (int)item["stack"];
                    customItemWrapper.UpdateStackModel();
                }

                // Consumable
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.LogInfo("is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == Mod.ItemType.Key)
                //{
                //    Mod.LogInfo("is Key");
                //}

                // Earpiece
                if (customItemWrapper.itemType == Mod.ItemType.Earpiece)
                {
                    Mod.LogInfo("is Earpiece");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }
                }

                // Face Cover
                if (customItemWrapper.itemType == Mod.ItemType.FaceCover)
                {
                    Mod.LogInfo("is Face Cover");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }
                }

                // Eyewear
                if (customItemWrapper.itemType == Mod.ItemType.Eyewear)
                {
                    Mod.LogInfo("is Eyewear");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }
                }

                // Headwear
                if (customItemWrapper.itemType == Mod.ItemType.Headwear)
                {
                    Mod.LogInfo("is Headwear");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        customItemWrapper.takeCurrentLocation = false;
                        customItemWrapper.locationIndex = 0;
                    }
                }

                // Dogtag
                if (customItemWrapper.itemType == Mod.ItemType.DogTag)
                {
                    customItemWrapper.dogtagName = item["dogtagName"].ToString();
                    customItemWrapper.dogtagLevel = (int)item["dogtagLevel"];
                }

                // Equip the item if it has an equip slot
                if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                {
                    customItemWrapper.takeCurrentLocation = false;

                    int equipSlotIndex = (int)item["PhysicalObject"]["equipSlot"];
                    Mod.LogInfo("Item has equip slot: "+ equipSlotIndex);
                    FVRQuickBeltSlot equipSlot = Mod.equipmentSlots[equipSlotIndex];
                    itemPhysicalObject.SetQuickBeltSlot(equipSlot);
                    itemPhysicalObject.SetParentage(null);

                    if(equipSlotIndex == 0)
                    {
                        Mod.leftShoulderObject = itemPhysicalObject.gameObject;
                    }

                    //EFM_EquipmentSlot.WearEquipment(customItemWrapper);

                    itemPhysicalObject.gameObject.SetActive(Mod.playerStatusManager.displayed);
                }

                // Put item in pocket if it has pocket index
                if (item["pocketSlotIndex"] != null)
                {
                    Mod.LogInfo("Loaded item has pocket index: " + ((int)item["pocketSlotIndex"]));
                    customItemWrapper.takeCurrentLocation = false;
                    customItemWrapper.locationIndex = 0;

                    FVRQuickBeltSlot pocketSlot = Mod.pocketSlots[(int)item["pocketSlotIndex"]];
                    itemPhysicalObject.SetQuickBeltSlot(pocketSlot);
                    itemPhysicalObject.SetParentage(null);
                }
            }

            if (vanillaItemDescriptor != null)
            {
                if (inAll)
                {
                    vanillaItemDescriptor.inAll = true;
                }
                else if(itemPhysicalObject is FVRFireArm)
                {
                    Mod.RemoveFromAll(itemPhysicalObject, null, vanillaItemDescriptor);
                }

                if (locationIndex != -1)
                {
                    vanillaItemDescriptor.takeCurrentLocation = false;
                    vanillaItemDescriptor.locationIndex = locationIndex;
                }
                vanillaItemDescriptor.looted = (bool)item["looted"];
                vanillaItemDescriptor.insured = (bool)item["insured"];

                // Put item in pocket if it has pocket index
                if (item["pocketSlotIndex"] != null)
                {
                    Mod.LogInfo("Loaded item has pocket index: " + ((int)item["pocketSlotIndex"]));
                    vanillaItemDescriptor.takeCurrentLocation = false;
                    vanillaItemDescriptor.locationIndex = 0;

                    FVRQuickBeltSlot pocketSlot = Mod.pocketSlots[(int)item["pocketSlotIndex"]];
                    itemPhysicalObject.SetQuickBeltSlot(pocketSlot);
                }
            }

            // Place in tradeVolume
            if (item["inTradeVolume"] != null)
            {
                itemObject.transform.parent = transform.GetChild(1).GetChild(24).GetChild(1);
            }
            else if(parent != null && parent.parent != null) // Add to container in case parent is one
            {
                CustomItemWrapper parentCIW = parent.parent.GetComponent<CustomItemWrapper>();
                if(parentCIW != null)
                {
                    parentCIW.AddItemToContainer(itemPhysicalObject);
                }
            }

            // GameObject
            itemObject.transform.localPosition = new Vector3((float)item["PhysicalObject"]["positionX"], (float)item["PhysicalObject"]["positionY"], (float)item["PhysicalObject"]["positionZ"]);
            itemObject.transform.localRotation = Quaternion.Euler(new Vector3((float)item["PhysicalObject"]["rotationX"], (float)item["PhysicalObject"]["rotationY"], (float)item["PhysicalObject"]["rotationZ"]));

            // Ensure item and its contents are all in the correct location index
            if (customItemWrapper != null)
            {
                BeginInteractionPatch.SetItemLocationIndex(customItemWrapper.locationIndex, customItemWrapper, null);
            }
            else if(vanillaItemDescriptor != null)
            {
                BeginInteractionPatch.SetItemLocationIndex(vanillaItemDescriptor.locationIndex, null, vanillaItemDescriptor);
            }

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
                itemObject.transform.parent = null;

                // PhysicalObject
                itemPhysicalObject.m_isSpawnLock = (bool)currentPhysicalObject["m_isSpawnLock"];
                itemPhysicalObject.m_isHardnessed = (bool)currentPhysicalObject["m_isHarnessed"];
                itemPhysicalObject.IsKinematicLocked = (bool)currentPhysicalObject["IsKinematicLocked"];
                itemPhysicalObject.IsInWater = (bool)currentPhysicalObject["IsInWater"];
                FVRFireArmAttachment itemAttachment = itemPhysicalObject as FVRFireArmAttachment;
                itemAttachment.AttachToMount(physicalObject.AttachmentMounts[(int)currentPhysicalObject["mountIndex"]], false);
                if (itemAttachment is Suppressor)
                {
                    (itemAttachment as Suppressor).AutoMountWell();
                }
                AddAttachments(itemPhysicalObject, currentPhysicalObject);
                
                // ObjectWrapper
                itemObjectWrapper.ItemID = currentPhysicalObject["ObjectWrapper"]["ItemID"].ToString();

                // Store the attachment's supposed local position so we can ensure it is correct later
                Mod.attachmentLocalTransform.Add(new KeyValuePair<GameObject, object>(itemObject, new Vector3[] { itemObject.transform.localPosition, itemObject.transform.localEulerAngles }));
                Mod.attachmentCheckNeeded = 5;
            }
        }

        public override void InitUI()
        {
            // Main hideout menu
            buttons = new Button[13][];
            buttons[0] = new Button[7];
            buttons[1] = new Button[7];
            buttons[2] = new Button[6];
            buttons[3] = new Button[4];
            buttons[4] = new Button[2];
            buttons[5] = new Button[3];
            buttons[6] = new Button[2];
            buttons[7] = new Button[1];
            buttons[8] = new Button[1];
            buttons[9] = new Button[2];
            buttons[10] = new Button[1];
            buttons[11] = new Button[2];
            buttons[12] = new Button[3];

            // Fetch buttons
            canvas = transform.GetChild(0).GetChild(0);
            buttons[0][0] = canvas.GetChild(0).GetChild(1).GetComponent<Button>(); // Raid
            buttons[0][1] = canvas.GetChild(0).GetChild(2).GetComponent<Button>(); // Save
            buttons[0][2] = canvas.GetChild(0).GetChild(3).GetComponent<Button>(); // Load
            buttons[0][3] = canvas.GetChild(0).GetChild(4).GetComponent<Button>(); // Base Back
            buttons[0][4] = canvas.GetChild(0).GetChild(5).GetComponent<Button>(); // Credits
            buttons[0][5] = canvas.GetChild(0).GetChild(6).GetComponent<Button>(); // Donate
            buttons[0][6] = canvas.GetChild(0).GetChild(7).GetComponent<Button>(); // Options

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

            buttons[3][0] = canvas.GetChild(3).GetChild(1).GetComponent<Button>(); // Char PMC
            buttons[3][1] = canvas.GetChild(3).GetChild(2).GetComponent<Button>(); // Char Scav
            buttons[3][2] = canvas.GetChild(3).GetChild(3).GetComponent<Button>(); // Char Back
            buttons[3][3] = canvas.GetChild(3).GetChild(5).GetChild(0).GetComponent<Button>(); // Scav block dialog OK

            buttons[4][0] = canvas.GetChild(4).GetChild(1).GetComponent<Button>(); // Map Mansion
            buttons[4][1] = canvas.GetChild(4).GetChild(2).GetComponent<Button>(); // Map Back

            buttons[5][0] = canvas.GetChild(5).GetChild(1).GetComponent<Button>(); // Time 0
            buttons[5][1] = canvas.GetChild(5).GetChild(2).GetComponent<Button>(); // Time 1
            buttons[5][2] = canvas.GetChild(5).GetChild(3).GetComponent<Button>(); // Time Back

            buttons[6][0] = canvas.GetChild(6).GetChild(1).GetComponent<Button>(); // Confirm
            buttons[6][1] = canvas.GetChild(6).GetChild(2).GetComponent<Button>(); // Confirm Back

            buttons[7][0] = canvas.GetChild(7).GetChild(1).GetComponent<Button>(); // Loading Cancel

            buttons[8][0] = canvas.GetChild(12).GetChild(1).GetComponent<Button>(); // Raid report Next

            buttons[9][0] = canvas.GetChild(13).GetChild(1).GetComponent<Button>(); // After raid treatment Next
            buttons[9][1] = canvas.GetChild(13).GetChild(2).GetComponent<Button>(); // After raid treatment Apply

            buttons[10][0] = canvas.GetChild(14).GetChild(1).GetComponent<Button>(); // Credits back

            buttons[11][0] = canvas.GetChild(15).GetChild(1).GetComponent<Button>(); // Donate donate
            buttons[11][1] = canvas.GetChild(15).GetChild(2).GetComponent<Button>(); // Donate back

            buttons[12][0] = canvas.GetChild(16).GetChild(1).GetComponent<Button>(); // Options back
            buttons[12][1] = canvas.GetChild(16).GetChild(2).GetComponent<Button>(); // Options Next
            buttons[12][2] = canvas.GetChild(16).GetChild(3).GetComponent<Button>(); // Options Previous

            // Fetch audio sources
            AudioSource hoverAudio = canvas.transform.GetChild(10).GetComponent<AudioSource>();
            clickAudio = canvas.transform.GetChild(11).GetComponent<AudioSource>();

            // Create an FVRPointableButton for each button
            for (int i = 0; i < buttons.Length; ++i)
            {
                for (int j = 0; j < buttons[i].Length; ++j)
                {
                    PointableButton pointableButton = buttons[i][j].gameObject.AddComponent<PointableButton>();

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
            buttons[0][4].onClick.AddListener(OnCreditsClicked);
            buttons[0][5].onClick.AddListener(OnDonatePanelClicked);
            buttons[0][6].onClick.AddListener(OnOptionsClicked);

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
            buttons[3][3].onClick.AddListener(OnScavBlockOKClicked);

            buttons[4][0].onClick.AddListener(() => { OnMapClicked(0); });
            buttons[4][1].onClick.AddListener(() => { OnBackClicked(4); });

            buttons[5][0].onClick.AddListener(() => { OnTimeClicked(0); });
            buttons[5][1].onClick.AddListener(() => { OnTimeClicked(1); });
            buttons[5][2].onClick.AddListener(() => { OnBackClicked(5); });

            buttons[6][0].onClick.AddListener(OnConfirmRaidClicked);
            buttons[6][1].onClick.AddListener(() => { OnBackClicked(6); });

            buttons[7][0].onClick.AddListener(OnCancelRaidLoadClicked);

            buttons[8][0].onClick.AddListener(OnRaidReportNextClicked);

            buttons[9][0].onClick.AddListener(OnMedicalNextClicked);
            buttons[9][1].onClick.AddListener(OnMedicalApplyClicked);

            buttons[10][0].onClick.AddListener(() => { OnBackClicked(14); });

            buttons[11][0].onClick.AddListener(() => { OnDonateClick(canvas.GetChild(15).GetChild(4).gameObject); });
            buttons[11][1].onClick.AddListener(() => { OnBackClicked(15); });

            buttons[12][0].onClick.AddListener(() => { OnBackClicked(16); });
            buttons[12][1].onClick.AddListener(OnOptionsNextClicked);
            buttons[12][2].onClick.AddListener(OnOptionsPreviousClicked);

            // Add background pointable
            FVRPointable backgroundPointable = canvas.gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 5;

            // Set options next active depending on number of pages
            optionsPageParent = canvas.GetChild(16).GetChild(4);
            if (optionsPageParent.childCount > 1)
            {
                buttons[12][1].gameObject.SetActive(true);
            }
            optionPages = new Transform[optionsPageParent.childCount];
            for(int i = 0; i < optionsPageParent.childCount; ++i)
            {
                optionPages[i] = optionsPageParent.GetChild(i);
            }

            // Set save buttons activated depending on presence of save files
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
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
            scavTimerText = canvas.GetChild(3).GetChild(4).GetComponent<Text>();
            scavButtonCollider = canvas.GetChild(3).GetChild(2).GetComponent<Collider>();
            scavButtonText = canvas.GetChild(3).GetChild(2).GetChild(2).GetComponent<Text>();
            charChoicePanel = canvas.GetChild(3).gameObject;
            saveConfirmTexts = new GameObject[5];
            for(int i=0; i < 5; ++i)
            {
                saveConfirmTexts[i] = canvas.GetChild(2).GetChild(i + 7).gameObject;
            }

            // Areas
            if (areaCanvasPrefab == null)
            {
                Mod.LogInfo("Area canvas not initialized, initializing all area UI and prepping...");

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
                requirementLocked = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_requirement_locked");
                emptyItemSlotIcon = Mod.baseAssetsBundle.LoadAsset<Sprite>("slot_empty_fill");
                dollarCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_dollars");
                euroCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_euros");
                roubleCurrencySprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_info_money_roubles");
                barterSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_currency_barter");
                experienceSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_experience_big");
                standingSprite = Mod.baseAssetsBundle.LoadAsset<Sprite>("standing_icon");
                traderAvatars = new Sprite[8];
                traderAvatars[0] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_russian_small");
                traderAvatars[1] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_therapist_small");
                traderAvatars[2] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_fence_small");
                traderAvatars[3] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ah_small");
                traderAvatars[4] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_peacekeeper_small");
                traderAvatars[5] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_tech_small");
                traderAvatars[6] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_ragman_small");
                traderAvatars[7] = Mod.baseAssetsBundle.LoadAsset<Sprite>("avatar_jaeger_small");
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
                bonusIcons.Add("UnlockArmorRepair", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_itemtype_gear_armor"));
                bonusIcons.Add("UnlockWeaponRepair", Mod.baseAssetsBundle.LoadAsset<Sprite>("icon_itemtype_weapon"));

                // Area specific sounds
                areaProductionSounds = new AudioClip[22,2];
                areaSlotSounds = new AudioClip[22];
                areaProductionSounds[2,0] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("waterprocessing_item_started");
                areaProductionSounds[2,1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("waterprocessing_item_ready");
                areaSlotSounds[4] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("generator_item_plugin_fuel");
                generatorLevel1And2Audio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("generator_level1_on + generator_level1_working + generator_level1_off + generator_level1_notworking");
                generatorLevel3Audio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("generator_level3_on + generator_level3_working + generator_level3_off + generator_level3_notworking");
                areaProductionSounds[6,0] = areaProductionSounds[2, 0];
                areaProductionSounds[6,1] = areaProductionSounds[2, 1];
                areaSlotSounds[6] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("waterprocessing_item_install");
                areaProductionSounds[7, 0] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("medstation_item_started");
                areaProductionSounds[7, 1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("medstation_item_ready");
                medStationLevel3Audio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("medstation_level3_fridge_on + medstation_level3_fridge_working + medstation_level3_fridge_off");
                areaProductionSounds[8, 0] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("boozegen_item_started");
                areaProductionSounds[8, 1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("boozegen_item_ready");
                kitchenPotAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("kitchen_level1_pot_on + kitchen_level1_pot_working + kitchen_level1_pot_off");
                kitchenFridgeAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("kitchen_level2_fridge_on + kitchen_level2_fridge_working + kitchen_level2_fridge_off");
                restSpaceTracks = new AudioClip[3];
                restSpaceTracks[0] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("restplace_level3_track1");
                restSpaceTracks[1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("restplace_level3_track2");
                restSpaceTracks[2] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("restplace_level3_track3");
                restSpacePSAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("restplace_level3_ps_on + restplace_level3_ps_working + restplace_level3_ps_off");
                areaProductionSounds[10, 0] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("workbench_item_started");
                areaProductionSounds[10, 1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("workbench_item_ready");
                areaProductionSounds[11, 0] = areaProductionSounds[10, 0];
                areaProductionSounds[11, 1] = areaProductionSounds[10, 1];
                intelCenterPCAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("icenter_level2_pc_on + icenter_level2_pc_working + icenter_level2_pc_off + icenter_level2_pc_notworking");
                intelCenterHDDAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("icenter_level3_hdd_on + icenter_level3_hdd_working + icenter_level3_hdd_off");
                AFUAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("afu_generic_working + afu_generic_notworking");
                areaSlotSounds[17] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("afu_item_plugin_filter");
                boozeGenAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("boozegen_generic_on + boozegen_generic_working + boozegen_generic_off + boozegen_generic_notworking");
                areaProductionSounds[19, 0] = areaProductionSounds[8, 0];
                areaProductionSounds[19, 1] = areaProductionSounds[8, 1];
                areaProductionSounds[20, 1] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("bitcoinfarm_item_ready"); ;
                areaSlotSounds[20] = Mod.baseAssetsBundle.LoadAsset<AudioClip>("bitcoinfarm_item_install_videocard");
                bitcoinFarmAudio = Mod.baseAssetsBundle.LoadAsset<AudioClip>("bitcoinfarm_generic_on + bitcoinfarm_generic_working + bitcoinfarm_generic_off");

                // Prep prefabs
                Mod.LogInfo("All area UI loaded, prepping...");

                // AreaCanvasPrefab
                GameObject summaryButtonObject = areaCanvasPrefab.transform.GetChild(0).GetChild(2).gameObject;
                PointableButton summaryPointableButton = summaryButtonObject.AddComponent<PointableButton>();
                summaryPointableButton.SetButton();
                summaryPointableButton.MaxPointingRange = 30;
                summaryPointableButton.hoverGraphics = new GameObject[2];
                summaryPointableButton.hoverGraphics[0] = summaryButtonObject.transform.GetChild(0).gameObject;
                summaryPointableButton.hoverGraphics[1] = areaCanvasPrefab.transform.GetChild(0).GetChild(1).gameObject;
                summaryPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                GameObject fullCloseButtonObject = areaCanvasPrefab.transform.GetChild(1).GetChild(0).GetChild(1).gameObject;
                PointableButton fullClosePointableButton = fullCloseButtonObject.AddComponent<PointableButton>();
                fullClosePointableButton.SetButton();
                fullClosePointableButton.MaxPointingRange = 30;
                fullClosePointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                GameObject middleHoverScrollUpObject = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(3).gameObject;
                GameObject middleHoverScrollDownObject = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject;
                HoverScroll middleHoverScrollUp = middleHoverScrollUpObject.AddComponent<HoverScroll>();
                HoverScroll middleHoverScrollDown = middleHoverScrollDownObject.AddComponent<HoverScroll>();
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

                GameObject middle2HoverScrollUpObject = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(3).gameObject;
                GameObject middle2HoverScrollDownObject = areaCanvasPrefab.transform.GetChild(1).GetChild(2).GetChild(0).GetChild(2).gameObject;
                HoverScroll middle2HoverScrollUp = middle2HoverScrollUpObject.AddComponent<HoverScroll>();
                HoverScroll middle2HoverScrollDown = middle2HoverScrollDownObject.AddComponent<HoverScroll>();
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
                PointableButton bottomPointableButton = areaCanvasBottomButtonPrefab.AddComponent<PointableButton>();
                bottomPointableButton.SetButton();
                bottomPointableButton.MaxPointingRange = 30;
                bottomPointableButton.hoverGraphics = new GameObject[1];
                bottomPointableButton.hoverGraphics[0] = areaCanvasBottomButtonPrefab.transform.GetChild(0).gameObject;
                bottomPointableButton.buttonText = areaCanvasBottomButtonPrefab.transform.GetChild(1).GetComponent<Text>();
                bottomPointableButton.toggleTextColor = true;

                // Production produce view
                Transform produceView = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(0);
                GameObject produceViewStartButtonObject = produceView.GetChild(4).GetChild(0).gameObject;
                PointableButton produceViewStartPointableButton = produceViewStartButtonObject.AddComponent<PointableButton>();
                produceViewStartPointableButton.SetButton();
                produceViewStartPointableButton.MaxPointingRange = 30;
                produceViewStartPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                GameObject produceViewGetItemsButtonObject = produceView.GetChild(4).GetChild(1).gameObject;
                PointableButton produceViewGetItemsPointableButton = produceViewGetItemsButtonObject.AddComponent<PointableButton>();
                produceViewGetItemsPointableButton.SetButton();
                produceViewGetItemsPointableButton.MaxPointingRange = 30;
                produceViewGetItemsPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                // Production farming view
                Transform farmingView = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(1);
                GameObject farmingViewSetAllButtonObject = farmingView.GetChild(1).GetChild(1).GetChild(0).gameObject;
                PointableButton farmingViewSetAllPointableButton = farmingViewSetAllButtonObject.AddComponent<PointableButton>();
                farmingViewSetAllPointableButton.SetButton();
                farmingViewSetAllPointableButton.MaxPointingRange = 30;
                farmingViewSetAllPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                GameObject farmingViewSetOneButtonObject = farmingView.GetChild(1).GetChild(1).GetChild(1).gameObject;
                PointableButton farmingViewSetOnePointableButton = farmingViewSetOneButtonObject.AddComponent<PointableButton>();
                farmingViewSetOnePointableButton.SetButton();
                farmingViewSetOnePointableButton.MaxPointingRange = 30;
                farmingViewSetOnePointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                GameObject farmingViewRemoveOneButtonObject = farmingView.GetChild(1).GetChild(1).GetChild(2).gameObject;
                PointableButton farmingViewRemoveOnePointableButton = farmingViewRemoveOneButtonObject.AddComponent<PointableButton>();
                farmingViewRemoveOnePointableButton.SetButton();
                farmingViewRemoveOnePointableButton.MaxPointingRange = 30;
                farmingViewRemoveOnePointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();
                GameObject farmingViewGetItemsButtonObject = farmingView.GetChild(1).GetChild(5).GetChild(0).gameObject;
                PointableButton farmingViewGetItemsPointableButton = farmingViewGetItemsButtonObject.AddComponent<PointableButton>();
                farmingViewGetItemsPointableButton.SetButton();
                farmingViewGetItemsPointableButton.MaxPointingRange = 30;
                farmingViewGetItemsPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                // Production scav case view
                Transform scavCaseView = areaCanvasPrefab.transform.GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetChild(2);
                GameObject scavCaseViewStartButtonObject = scavCaseView.GetChild(4).GetChild(0).gameObject;
                PointableButton scavCaseViewStartPointableButton = scavCaseViewStartButtonObject.AddComponent<PointableButton>();
                scavCaseViewStartPointableButton.SetButton();
                scavCaseViewStartPointableButton.MaxPointingRange = 30;
                scavCaseViewStartPointableButton.hoverSound = areaCanvasPrefab.transform.GetChild(2).GetComponent<AudioSource>();

                Mod.LogInfo("Area UI prepped");
            }

            Mod.LogInfo("Initializing area managers");
            // Init all area managers after UI is prepped
            for (int i = 0; i < 22; ++i)
            {
                baseAreaManagers[i].Init();
            }

            // Unload base assets bundle without unloading loaded assets, if necessary
            // TODO: will have to review the asset loading process, because if already in hideout, if we load a save, this unload causes assets to be missing because the hideout assets only get loaded once, on the first hideout load
            //if (Mod.baseAssetsBundle != null)
            //{
            //    Mod.baseAssetsBundle.Unload(false);
            //}

            // Init Market
            marketManager = transform.GetChild(1).GetChild(24).gameObject.AddComponent<MarketManager>();
            marketManager.Init(this);

            // Add switches
            // LightSwitch
            Switch lightSwitch = transform.GetChild(1).GetChild(23).GetChild(0).gameObject.AddComponent<Switch>();
            lightSwitch.level2AudioSource = transform.GetChild(1).GetChild(23).GetChild(0).GetChild(0).GetComponent<AudioSource>();
            lightSwitch.level3AudioSource = transform.GetChild(1).GetChild(23).GetChild(0).GetChild(1).GetComponent<AudioSource>();
            lightSwitch.gameObjects = new List<GameObject>
            {
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(0).GetChild(1).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(1).GetChild(1).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(2).GetChild(1).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(3).GetChild(1).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(4).GetChild(1).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(2).GetChild(1).GetChild(5).GetChild(1).gameObject,

                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(0).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(1).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(2).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(3).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(4).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(5).GetChild(5).gameObject,
                transform.GetChild(1).GetChild(15).GetChild(3).GetChild(0).GetChild(6).GetChild(5).gameObject
            };
            // UISwitch
            Switch UISwitch = transform.GetChild(1).GetChild(23).GetChild(1).gameObject.AddComponent<Switch>();
            UISwitch.mode = 2;
            UISwitch.audioSource = transform.GetChild(1).GetChild(23).GetChild(0).GetChild(0).GetComponent<AudioSource>();
            UISwitch.gameObjects = new List<GameObject>();
            for(int i=0; i < 22; ++i)
            {
                // Each area canvas
                UISwitch.gameObjects.Add(transform.GetChild(1).GetChild(i).GetChild(transform.GetChild(1).GetChild(i).childCount - 2).gameObject);
            }
            UISwitch.gameObjects.Add(transform.GetChild(1).GetChild(24).gameObject); // Market
            // MarketSwitch
            Switch MarketSwitch = transform.GetChild(1).GetChild(23).GetChild(2).gameObject.AddComponent<Switch>();
            MarketSwitch.mode = 3;
            MarketSwitch.audioSource = transform.GetChild(1).GetChild(23).GetChild(0).GetChild(0).GetComponent<AudioSource>();
            MarketSwitch.gameObjects = new List<GameObject>();
            for(int i=0; i < 22; ++i)
            {
                // Each area canvas
                MarketSwitch.gameObjects.Add(transform.GetChild(1).GetChild(i).GetChild(transform.GetChild(1).GetChild(i).childCount - 2).gameObject);
            }
            MarketSwitch.gameObjects.Add(transform.GetChild(1).GetChild(24).gameObject); // Market

            if (Mod.justFinishedRaid)
            {
                if (Mod.chosenCharIndex == 0)
                {
                    // Spawn with less health and energy/hydration if killed or MIA
                    int raidResultExp = 0; // TODO: Add bonus for how long we survived
                    if (Mod.raidState == FinishRaidState.KIA || Mod.raidState == FinishRaidState.MIA)
                    {
                        for (int i = 0; i < Mod.health.Length; ++i)
                        {
                            Mod.health[i] = 0.3f * Mod.currentMaxHealth[i];
                        }

                        Mod.hydration = 0.3f * Mod.maxHydration;
                        Mod.energy = 0.3f * Mod.maxEnergy;

                        // Remove all effects
                        Effect.RemoveEffects();
                    }
                    else if (Mod.raidState == FinishRaidState.Survived)
                    {
                        raidResultExp = 600 + (int)(Mod.raidTime / 60 * 10); // Survive exp + bonus of 10 exp / min
                        Mod.AddExperience(600);
                    }

                    Mod.LogInfo("Base init: Just finished raid");
                    Transform raidReportScreen = transform.GetChild(0).GetChild(0).GetChild(12);
                    medicalScreen = transform.GetChild(0).GetChild(0).GetChild(13);
                    float raidReportListHeight = 141; // Default height not including None kill
                    float medicalListHeight = 26;

                    // Activate raid report, deactivate hideout menu
                    transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    transform.GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(true);

                    // Set raid result in title
                    string raidResultString = Mod.raidState == FinishRaidState.RunThrough ? "Run Through" : Mod.raidState.ToString();
                    raidReportScreen.GetChild(0).GetComponent<Text>().text = "Raid Report: " + raidResultString;

                    // Set experience elements
                    raidReportScreen.GetChild(2).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
                    raidReportScreen.GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
                    int expForNextLevel = 0;
                    for (int i = 0; i < Mod.level; ++i)
                    {
                        expForNextLevel += (int)Mod.XPPerLevel[Mod.level]["exp"];
                    }
                    raidReportScreen.GetChild(2).GetChild(1).GetChild(1).GetComponent<Image>().rectTransform.sizeDelta = new Vector2(450 * (Mod.experience / (float)expForNextLevel), 12.8f);
                    raidReportScreen.GetChild(2).GetChild(1).GetChild(2).GetComponent<Text>().text = Mod.experience.ToString() + "/" + expForNextLevel;

                    Transform listContent = raidReportScreen.GetChild(3).GetChild(0).GetChild(0);
                    int expTotal = 0;

                    Mod.LogInfo("\tSet raid report xp");
                    // Fill kill list
                    int killCount = 0;
                    if (Mod.killList != null && Mod.killList.Count > 0)
                    {
                        Mod.LogInfo("\tHave kills, adding to list");
                        // Disable none
                        listContent.GetChild(0).GetChild(1).gameObject.SetActive(false);

                        // Add each kill
                        foreach (KeyValuePair<string, int> kill in Mod.killList)
                        {
                            GameObject killElement = Instantiate(listContent.GetChild(0).GetChild(2).gameObject, listContent.GetChild(0));
                            killElement.SetActive(true);
                            killElement.transform.GetChild(0).GetComponent<Text>().text = kill.Key;
                            killElement.transform.GetChild(1).GetComponent<Text>().text = kill.Value.ToString() + " exp";

                            expTotal += kill.Value;

                            raidReportListHeight += 21;
                            ++killCount;
                        }
                    }
                    else
                    {
                        Mod.LogInfo("\tNo kills");
                        raidReportListHeight += 21; // Add none kill
                    }

                    // Set other
                    listContent.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = Mod.lootingExp.ToString() + " exp";
                    expTotal += Mod.lootingExp;
                    listContent.GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().text = Mod.healingExp.ToString() + " exp";
                    expTotal += Mod.healingExp;
                    listContent.GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().text = Mod.explorationExp.ToString() + " exp";
                    expTotal += Mod.explorationExp;
                    listContent.GetChild(1).GetChild(4).GetChild(0).GetComponent<Text>().text = "Raid Result (" + raidResultString + ")";
                    listContent.GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().text = raidResultExp.ToString() + " exp";
                    expTotal += raidResultExp;

                    Mod.LogInfo("\tSet other xp");
                    // Set total
                    listContent.GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = expTotal.ToString() + " exp";

                    Mod.LogInfo("\tSetting hoverscrolls, killcount: " + killCount);
                    // Set hover scrolls
                    if (killCount >= 8)
                    {
                        Mod.LogInfo("\t\tkillcount >= 8, activating kill list hover scrolls");
                        HoverScroll raidReportListDownHoverScroll = raidReportScreen.GetChild(3).GetChild(2).gameObject.AddComponent<HoverScroll>();
                        HoverScroll raidReportListUpHoverScroll = raidReportScreen.GetChild(3).GetChild(3).gameObject.AddComponent<HoverScroll>();
                        raidReportListDownHoverScroll.MaxPointingRange = 30;
                        raidReportListDownHoverScroll.hoverSound = hoverAudio;
                        raidReportListDownHoverScroll.scrollbar = raidReportScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        raidReportListDownHoverScroll.other = raidReportListUpHoverScroll;
                        raidReportListDownHoverScroll.up = false;
                        raidReportListUpHoverScroll.MaxPointingRange = 30;
                        raidReportListUpHoverScroll.hoverSound = hoverAudio;
                        raidReportListUpHoverScroll.scrollbar = raidReportScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        raidReportListUpHoverScroll.other = raidReportListDownHoverScroll;
                        raidReportListUpHoverScroll.up = true;

                        raidReportListUpHoverScroll.rate = 309 / (raidReportListHeight - 309);
                        raidReportListDownHoverScroll.rate = 309 / (raidReportListHeight - 309);
                        raidReportListDownHoverScroll.gameObject.SetActive(true);
                    }
                    Mod.LogInfo("\tSet hoverscrolls, setting medical");


                    // Set medical body
                    Dictionary<int, bool[]> partConditions = new Dictionary<int, bool[]>();
                    foreach (Effect effect in Effect.effects)
                    {
                        if (effect.partIndex > -1)
                        {
                            if (effect.effectType == Effect.EffectType.LightBleeding)
                            {
                                if (partConditions.ContainsKey(effect.partIndex))
                                {
                                    partConditions[effect.partIndex][0] = true;
                                }
                                else
                                {
                                    partConditions.Add(effect.partIndex, new bool[] { true, false, false });
                                }
                            }
                            else if (effect.effectType == Effect.EffectType.HeavyBleeding)
                            {
                                if (partConditions.ContainsKey(effect.partIndex))
                                {
                                    partConditions[effect.partIndex][1] = true;
                                }
                                else
                                {
                                    partConditions.Add(effect.partIndex, new bool[] { false, true, false });
                                }
                            }
                            else if (effect.effectType == Effect.EffectType.Fracture)
                            {
                                if (partConditions.ContainsKey(effect.partIndex))
                                {
                                    partConditions[effect.partIndex][2] = true;
                                }
                                else
                                {
                                    partConditions.Add(effect.partIndex, new bool[] { false, false, true });
                                }
                            }
                        }
                    }
                    Mod.LogInfo("\tSet body");

                    // Process parts
                    medicalScreenPartImagesParent = medicalScreen.GetChild(4).GetChild(0);
                    Transform partInfoParent = medicalScreen.GetChild(4).GetChild(1);
                    Transform medicalListContent = medicalScreen.GetChild(3).GetChild(0).GetChild(0);
                    medicalScreenPartImages = new Transform[7];
                    medicalScreenPartHealthTexts = new Text[7];
                    totalMedicalTreatmentPrice = 0;
                    fullPartConditions = new Dictionary<int, int[]>();
                    medicalPartElements = new Dictionary<int, GameObject>();
                    int breakPartPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["BreakPart"]["RemovePrice"];
                    int fracturePrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["Fracture"]["RemovePrice"];
                    int heavyBleedingPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["RemovePrice"];
                    int lightBleedingPrice = (int)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["RemovePrice"];
                    int healthPrice = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["HealthPointPrice"];
                    int trialLevels = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["TrialLevels"];
                    int trialRaids = (int)Mod.globalDB["config"]["Health"]["HealPrice"]["TrialRaids"];
                    int[] otherConditionCosts = new int[] { lightBleedingPrice, heavyBleedingPrice, fracturePrice };
                    Mod.LogInfo("\tInit part process");
                    for (int partIndex = 0; partIndex < 7; ++partIndex)
                    {
                        // Set part color
                        medicalScreenPartImages[partIndex] = medicalScreenPartImagesParent.GetChild(partIndex);
                        if (Mod.health[partIndex] == 0)
                        {
                            medicalScreenPartImages[partIndex].GetComponent<Image>().color = Color.black;
                        }
                        else
                        {
                            medicalScreenPartImages[partIndex].GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mod.health[partIndex] / Mod.currentMaxHealth[partIndex]);
                        }

                        // Set part info
                        medicalScreenPartHealthTexts[partIndex] = partInfoParent.GetChild(partIndex).GetChild(1).GetComponent<Text>();
                        medicalScreenPartHealthTexts[partIndex].text = String.Format("{0:0}", Mod.health[partIndex]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[partIndex]);
                        if (partConditions.ContainsKey(partIndex))
                        {
                            for (int i = 0; i < partConditions[partIndex].Length; ++i)
                            {
                                partInfoParent.GetChild(partIndex).GetChild(3).GetChild(i).gameObject.SetActive(partConditions[partIndex][i]);
                            }
                        }

                        if (partConditions.ContainsKey(partIndex) || Mod.health[partIndex] < Mod.currentMaxHealth[partIndex])
                        {
                            fullPartConditions.Add(partIndex, new int[5]);

                            // Add to list
                            GameObject partElement = Instantiate(medicalListContent.GetChild(0).gameObject, medicalListContent);
                            partElement.transform.GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.GetBodyPartName(partIndex);
                            partElement.SetActive(true);

                            // Setup logic
                            PointableButton pointableButton = partElement.transform.GetChild(0).gameObject.AddComponent<PointableButton>();
                            pointableButton.SetButton();
                            int currentPartIndex = partIndex;
                            pointableButton.Button.onClick.AddListener(() => { ToggleMedicalPart(currentPartIndex); });
                            pointableButton.MaxPointingRange = 20;
                            pointableButton.hoverSound = hoverAudio;

                            medicalPartElements.Add(partIndex, partElement);

                            medicalListHeight += 27;

                            // Process health and destroyed
                            int partTotalCost = 0;
                            if (Mod.health[partIndex] < Mod.currentMaxHealth[partIndex])
                            {
                                if (Mod.health[partIndex] <= 0)
                                {
                                    int cost = (int)(breakPartPrice + healthPrice * Mod.currentMaxHealth[partIndex]);
                                    fullPartConditions[partIndex][4] = cost;
                                    totalMedicalTreatmentPrice += cost;
                                    partTotalCost += cost;

                                    partElement.transform.GetChild(5).gameObject.SetActive(true);
                                    partElement.transform.GetChild(5).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                                    PointableButton destroyedPartButton = partElement.transform.GetChild(5).gameObject.AddComponent<PointableButton>();
                                    destroyedPartButton.SetButton();
                                    destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 4); });
                                    destroyedPartButton.MaxPointingRange = 20;
                                    destroyedPartButton.hoverSound = hoverAudio;
                                }
                                else // Not destroyed but damaged
                                {
                                    int hpToHeal = (int)(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]);
                                    int cost = healthPrice * hpToHeal;
                                    fullPartConditions[partIndex][0] = cost;
                                    totalMedicalTreatmentPrice += cost;
                                    partTotalCost += cost;

                                    partElement.transform.GetChild(1).gameObject.SetActive(true);
                                    partElement.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);
                                    partElement.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "Health (" + hpToHeal + ")";

                                    PointableButton destroyedPartButton = partElement.transform.GetChild(1).gameObject.AddComponent<PointableButton>();
                                    destroyedPartButton.SetButton();
                                    destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 0); });
                                    destroyedPartButton.MaxPointingRange = 20;
                                    destroyedPartButton.hoverSound = hoverAudio;
                                }

                                medicalListHeight += 22;
                            }

                            // Process other conditions
                            if (partConditions.ContainsKey(partIndex))
                            {
                                for (int i = 0; i < 3; ++i)
                                {
                                    if (partConditions[partIndex][i])
                                    {
                                        int cost = otherConditionCosts[i];
                                        fullPartConditions[partIndex][i + 1] = cost;
                                        totalMedicalTreatmentPrice += cost;
                                        partTotalCost += cost;

                                        partElement.transform.GetChild(i + 2).gameObject.SetActive(true);
                                        partElement.transform.GetChild(i + 2).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                                        PointableButton destroyedPartButton = partElement.transform.GetChild(i + 2).gameObject.AddComponent<PointableButton>();
                                        destroyedPartButton.SetButton();
                                        destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, i + 1); });
                                        destroyedPartButton.MaxPointingRange = 20;
                                        destroyedPartButton.hoverSound = hoverAudio;

                                        medicalListHeight += 22;
                                    }
                                }
                            }

                            // Set part total cost
                            partElement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(partTotalCost);
                        }
                    }
                    Mod.LogInfo("\tProcessed parts");

                    // Setup total and put as last sibling
                    totalTreatmentPriceText = medicalListContent.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
                    totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                    medicalListContent.GetChild(1).SetAsLastSibling();

                    // Set total health
                    float totalHealth = 0;
                    foreach (float partHealth in Mod.health)
                    {
                        totalHealth += partHealth;
                    }
                    medicalScreenTotalHealthText = medicalScreen.GetChild(4).GetChild(2).GetChild(1).GetComponent<Text>();
                    medicalScreenTotalHealthText.text = totalHealth.ToString() + "/440";

                    medicalScreen.GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((Mod.baseInventory.ContainsKey("203") ? Mod.baseInventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));

                    Mod.LogInfo("\tSet total health");
                    // Set hover scrolls
                    if (medicalListHeight >= 377)
                    {
                        HoverScroll medicalListDownHoverScroll = medicalScreen.GetChild(3).GetChild(2).gameObject.AddComponent<HoverScroll>();
                        HoverScroll merdicalListUpHoverScroll = medicalScreen.GetChild(3).GetChild(3).gameObject.AddComponent<HoverScroll>();
                        medicalListDownHoverScroll.MaxPointingRange = 30;
                        medicalListDownHoverScroll.hoverSound = hoverAudio;
                        medicalListDownHoverScroll.scrollbar = medicalScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        medicalListDownHoverScroll.other = merdicalListUpHoverScroll;
                        medicalListDownHoverScroll.up = false;
                        merdicalListUpHoverScroll.MaxPointingRange = 30;
                        merdicalListUpHoverScroll.hoverSound = hoverAudio;
                        merdicalListUpHoverScroll.scrollbar = medicalScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        merdicalListUpHoverScroll.other = medicalListDownHoverScroll;
                        merdicalListUpHoverScroll.up = true;

                        merdicalListUpHoverScroll.rate = 377 / (medicalListHeight - 377);
                        medicalListDownHoverScroll.rate = 377 / (medicalListHeight - 377);
                        medicalListDownHoverScroll.gameObject.SetActive(true);
                    }
                    Mod.LogInfo("\tSet hoverscrolls");

                    UpdateTreatmentApply();

                    // Enable after raid collider to keep player in security room until they're done with after raid report
                    transform.GetChild(1).GetChild(23).GetChild(3).gameObject.SetActive(true);
                }
                else // Finished scav raid
                {
                    scavTimer = 600 * (currentScavCooldownTimer - currentScavCooldownTimer * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100));
                }
            }

            // Init status skills
            Transform skillList = Mod.playerStatusManager.transform.GetChild(9).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject skillPairPrefab = skillList.GetChild(0).gameObject;
            GameObject skillPrefab = skillPairPrefab.transform.GetChild(0).gameObject;
            Transform currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
            currentSkillPair.gameObject.SetActive(true);
            Mod.playerStatusManager.skills = new SkillUI[Mod.skills.Length];
            for (int i = 0; i < Mod.skills.Length; ++i)
            {
                if (currentSkillPair.childCount == 3)
                {
                    currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
                    currentSkillPair.gameObject.SetActive(true);
                }

                SkillUI skillUI = new SkillUI();
                GameObject currentSkill = Instantiate(skillPrefab, currentSkillPair);
                currentSkill.SetActive(true);
                currentSkill.transform.GetChild(0).GetComponent<Image>().sprite = Mod.skillIcons[i];
                skillUI.text = currentSkill.transform.GetChild(1).GetChild(0).GetComponent<Text>();
                skillUI.text.text = String.Format("{0} lvl. {1:0} ({2:0}/100)", Mod.SkillIndexToName(i), (int)(Mod.skills[i].currentProgress / 100), Mod.skills[i].currentProgress % 100);
                skillUI.progressBarRectTransform = currentSkill.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<RectTransform>();
                skillUI.progressBarRectTransform.sizeDelta = new Vector2(Mod.skills[i].currentProgress % 100, 4.73f);
                skillUI.diminishingReturns = currentSkill.transform.GetChild(1).GetChild(2).gameObject;
                skillUI.increasing = currentSkill.transform.GetChild(1).GetChild(3).gameObject;
                Mod.playerStatusManager.skills[i] = skillUI;
            }
        }

        private void UpdateTreatmentApply()
        {
            int playerRoubleCount = (Mod.baseInventory.ContainsKey("203") ? Mod.baseInventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0);
            if(playerRoubleCount < totalMedicalTreatmentPrice)
            {
                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = false;
                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.gray;
            }
            else
            {
                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = true;
                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.white;
            }
        }

        public void ToggleMedicalPart(int partIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price
                medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                for (int i=0; i < 5; ++i)
                {
                    if (medicalPartElements[partIndex].transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                    {
                        totalMedicalTreatmentPrice -= fullPartConditions[partIndex][i];
                    }
                }
                totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
            }
            else
            {
                // Activate checkmark and add to price
                medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
                for (int i = 0; i < 5; ++i)
                {
                    if (medicalPartElements[partIndex].transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                    {
                        totalMedicalTreatmentPrice += fullPartConditions[partIndex][i];
                    }
                }
                totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
            }

            UpdateTreatmentApply();
        }

        public void ToggleMedicalPartCondition(int partIndex, int conditionIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(false);
                if(medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice -= fullPartConditions[partIndex][conditionIndex];
                    totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }
            else
            {
                // Activate checkmark and add to price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice += fullPartConditions[partIndex][conditionIndex];
                    totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }

            UpdateTreatmentApply();
        }

        private void UpdateSaveButtonList()
        {
            // Set buttons activated depending on presence of save files
            FetchAvailableSaveFiles();
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    buttons[1][i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
            }
            else
            {
                buttons[0][2].gameObject.SetActive(false);
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
            int scaledTime = (int)((clampedTime * Manager.meatovTimeMultiplier) % 86400);
            time = scaledTime;
        }

        public void OnTutorialSkipClick()
        {
            clickAudio.Play();
            transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        }

        public void OnTutorialNextClick(Transform current, Transform next)
        {
            clickAudio.Play();
            current.gameObject.SetActive(false);
            if (next != null)
            {
                next.gameObject.SetActive(true);
            }
        }

        public void OnDonateClick(GameObject text)
        {
            clickAudio.Play(); 
            Application.OpenURL("https://ko-fi.com/tommysoucy");
            text.SetActive(true);
            text.AddComponent<TimedDisabler>();
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
            saveConfirmTexts[slotIndex].SetActive(true);
            saveConfirmTexts[slotIndex].AddComponent<TimedDisabler>();
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

        public void OnCreditsClicked()
        {
            clickAudio.Play();
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(14).gameObject.SetActive(true);
        }

        public void OnDonatePanelClicked()
        {
            clickAudio.Play();
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(15).gameObject.SetActive(true);
        }

        public void OnOptionsClicked()
        {
            clickAudio.Play();
            canvas.GetChild(0).gameObject.SetActive(false);
            canvas.GetChild(16).gameObject.SetActive(true);
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
                case 14:
                case 15:
                case 16:
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

        public void OnOptionsNextClicked()
        {
            clickAudio.Play();
            for (int i = 0; i < optionPages.Length; ++i)
            {
                if (optionPages[i].gameObject.activeSelf)
                {
                    optionPages[i].gameObject.SetActive(false);
                    optionPages[i + 1].gameObject.SetActive(true);

                    if(i + 1 == optionPages.Length - 1)
                    {
                        buttons[12][1].gameObject.SetActive(false);
                    }

                    buttons[12][2].gameObject.SetActive(true);

                    break;
                }
            }
        }

        public void OnOptionsPreviousClicked()
        {
            clickAudio.Play();
            for (int i = 0; i < optionPages.Length; ++i)
            {
                if (optionPages[i].gameObject.activeSelf)
                {
                    optionPages[i].gameObject.SetActive(false);
                    optionPages[i - 1].gameObject.SetActive(true);

                    if(i - 1 == 0)
                    {
                        buttons[12][2].gameObject.SetActive(false);
                    }

                    buttons[12][1].gameObject.SetActive(true);

                    break;
                }
            }
        }

        public void OnScavBlockOKClicked()
        {
            clickAudio.Play();

            buttons[3][0].GetComponent<Collider>().enabled = true;
            buttons[3][1].GetComponent<Collider>().enabled = true;
        }

        public void OnCharClicked(int charIndex)
        {
            clickAudio.Play();

            Mod.chosenCharIndex = charIndex;

            // Update chosen char text
            chosenCharacter.text = charIndex == 0 ? "PMC" : "Scav";

            bool previousScavItemFound = false;
            Transform scavRaidItemNodeParent = transform.GetChild(1).GetChild(25);
            foreach(Transform child in scavRaidItemNodeParent)
            {
                if(child.childCount > 0)
                {
                    previousScavItemFound = true;
                    break;
                }
            }

            if (previousScavItemFound)
            {
                buttons[3][0].GetComponent<Collider>().enabled = false;
                buttons[3][1].GetComponent<Collider>().enabled = false;

                canvas.GetChild(3).GetChild(5).gameObject.SetActive(true);
            }
            else
            {
                canvas.GetChild(3).gameObject.SetActive(false);
                canvas.GetChild(4).gameObject.SetActive(true);
            }
        }

        public void OnMapClicked(int mapIndex)
        {
            clickAudio.Play();

            Mod.chosenMapIndex = mapIndex;

            // Update chosen map text
            switch (Mod.chosenMapIndex)
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

            Mod.chosenTimeIndex = timeIndex;
            canvas.GetChild(5).gameObject.SetActive(false);
            canvas.GetChild(6).gameObject.SetActive(true);
        }

        public void OnConfirmRaidClicked()
        {
            clickAudio.Play();

            canvas.GetChild(6).gameObject.SetActive(false);
            canvas.GetChild(7).gameObject.SetActive(true);

            // Begin loading raid map
            switch (Mod.chosenMapIndex)
            {
                case 0:
                    loadingRaid = true;
                    Mod.chosenMapName = "Factory";
                    Mod.currentRaidBundleRequest = AssetBundle.LoadFromFileAsync("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovFactory.ab");
                    break;
                default:
                    loadingRaid = true;
                    Mod.chosenMapIndex = 0;
                    Mod.chosenCharIndex = 0;
                    Mod.chosenTimeIndex = 0;
                    Mod.chosenMapName = "Factory";
                    chosenMap.text = "Factory";
                    Mod.currentRaidBundleRequest = AssetBundle.LoadFromFileAsync("BepinEx/Plugins/EscapeFromMeatov/Assets/EscapeFromMeatovFactory.ab");
                    break;
            }
        }

        public void OnCancelRaidLoadClicked()
        {
            clickAudio.Play();

            cancelRaidLoad = true;
        }

        public void OnRaidReportNextClicked()
        {
            clickAudio.Play();

            transform.GetChild(0).GetChild(0).GetChild(12).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetChild(13).gameObject.SetActive(true);
        }

        public void OnMedicalNextClicked()
        {
            clickAudio.Play();

            transform.GetChild(0).GetChild(0).GetChild(13).gameObject.SetActive(false);
            transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);

            // Disable after raid collider
            transform.GetChild(1).GetChild(23).GetChild(3).gameObject.SetActive(false);
        }

        public void OnMedicalApplyClicked()
        {
            clickAudio.Play();

            // Check which parts are active, which conditions are active for each of those, fix them
            foreach(KeyValuePair<int, GameObject> partElement in medicalPartElements)
            {
                if (partElement.Value.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    for(int i=0; i < 5; ++i)
                    {
                        if (partElement.Value.transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                        {
                            if(i == 0) // Health
                            {
                                Mod.health[partElement.Key] = Mod.currentMaxHealth[partElement.Key];

                                // Update display
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(0).GetChild(partElement.Key).GetComponent<Image>().color = Color.white;
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(1).GetComponent<Text>().text = Mod.currentMaxHealth[partElement.Key].ToString()+"/"+ Mod.currentMaxHealth[partElement.Key];
                            }
                            else if(i == 1) // LightBleeding
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.LightBleeding, partElement.Key);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(0).gameObject.SetActive(false);
                            }
                            else if(i == 2) // HeavyBleeding
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.HeavyBleeding, partElement.Key);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(1).gameObject.SetActive(false);
                            }
                            else if(i == 3) // Fracture
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.Fracture, partElement.Key);

                                // Update fracture icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(2).gameObject.SetActive(false);
                            }
                            else if(i == 4) // DestroyedPart
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.DestroyedPart, partElement.Key);

                                Mod.health[partElement.Key] = Mod.currentMaxHealth[partElement.Key];

                                // Update display
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(0).GetChild(partElement.Key).GetComponent<Image>().color = Color.white;
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(1).GetComponent<Text>().text = Mod.currentMaxHealth[partElement.Key].ToString() + "/" + Mod.currentMaxHealth[partElement.Key];
                            }
                        }
                    }

                    Destroy(partElement.Value);
                }
            }

            // Remove money from player
            int amountToRemoveFromBase = 0;
            int amountToRemoveFromPlayer = 0;
            if (Mod.baseInventory.ContainsKey("203"))
            {
                if (Mod.baseInventory["203"] >= totalMedicalTreatmentPrice)
                {
                    amountToRemoveFromBase = totalMedicalTreatmentPrice;
                }
                else
                {
                    amountToRemoveFromBase = Mod.baseInventory["203"];
                    amountToRemoveFromPlayer = totalMedicalTreatmentPrice - Mod.baseInventory["203"];
                }
            }
            else
            {
                amountToRemoveFromPlayer = totalMedicalTreatmentPrice;
            }
            if (amountToRemoveFromBase > 0)
            {
                Mod.baseInventory["203"] = Mod.baseInventory["203"] - amountToRemoveFromBase;
                List<GameObject> objectList = baseInventoryObjects["203"];
                for (int i = objectList.Count - 1, j = amountToRemoveFromBase; i >= 0 && j > 0; --i)
                {
                    GameObject toCheck = objectList[objectList.Count - 1];
                    CustomItemWrapper CIW = toCheck.GetComponent<CustomItemWrapper>();
                    if (CIW.stack > amountToRemoveFromBase)
                    {
                        CIW.stack = CIW.stack - amountToRemoveFromBase;
                        j = 0;
                    }
                    else // CIW.stack <= amountToRemoveFromBase
                    {
                        j -= CIW.stack;
                        objectList.RemoveAt(objectList.Count - 1);
                        CIW.physObj.SetQuickBeltSlot(null);
                        CIW.destroyed = true;
                        Destroy(toCheck);
                    }
                }
            }
            if (amountToRemoveFromPlayer > 0)
            {
                Mod.playerInventory["203"] = Mod.playerInventory["203"] - amountToRemoveFromPlayer;
                List<GameObject> objectList = Mod.playerInventoryObjects["203"];
                for (int i = objectList.Count - 1, j = amountToRemoveFromPlayer; i >= 0 && j > 0; --i)
                {
                    GameObject toCheck = objectList[objectList.Count - 1];
                    CustomItemWrapper CIW = toCheck.GetComponent<CustomItemWrapper>();
                    if (CIW.stack > amountToRemoveFromPlayer)
                    {
                        CIW.stack = CIW.stack - amountToRemoveFromPlayer;
                        j = 0;
                    }
                    else // CIW.stack <= amountToRemoveFromBase
                    {
                        j -= CIW.stack;
                        objectList.RemoveAt(objectList.Count - 1);
                        CIW.physObj.SetQuickBeltSlot(null);
                        CIW.physObj.ForceBreakInteraction();
                        CIW.destroyed = true;
                        Destroy(toCheck);
                        Mod.weight -= CIW.currentWeight;
                    }
                }
            }

            foreach (BaseAreaManager baseAreaManager in baseAreaManagers)
            {
                baseAreaManager.UpdateBasedOnItem("203");
            }

            transform.GetChild(0).GetChild(0).GetChild(13).GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((Mod.baseInventory.ContainsKey("203") ? Mod.baseInventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));
        }

        private void ResetPlayerRig()
        {
            // Destroy and reset rig and equipment slots
            EquipmentSlot.Clear();
            for (int i = 0; i < Mod.equipmentSlots.Count; ++i)
            {
                if (Mod.equipmentSlots[i] != null && Mod.equipmentSlots[i].CurObject != null)
                {
                    Destroy(Mod.equipmentSlots[i].CurObject.gameObject);
                }
            }
            GM.CurrentPlayerBody.ConfigureQuickbelt(-2); // -2 in order to destroy the objects on belt as well
        }

        private void SaveBase()
        {
            Mod.LogInfo("Saving base");
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
            saveObject["weight"] = Mod.weight;
            saveObject["level"] = Mod.level;
            saveObject["experience"] = Mod.experience;
            saveObject["totalRaidCount"] = Mod.totalRaidCount;
            saveObject["runThroughRaidCount"] = Mod.runThroughRaidCount;
            saveObject["survivedRaidCount"] = Mod.survivedRaidCount;
            saveObject["MIARaidCount"] = Mod.MIARaidCount;
            saveObject["KIARaidCount"] = Mod.KIARaidCount;
            saveObject["failedRaidCount"] = Mod.failedRaidCount;
            saveObject["fenceRestockTimer"] = TraderStatus.fenceRestockTimer;
            saveObject["scavTimer"] = scavTimer;

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
                currentSavedArea["constructTimer"] = baseAreaManagers[i].constructionTimer;
                if (baseAreaManagers[i].slotItems != null)
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
                if (baseAreaManagers[i].activeProductions != null)
                {
                    currentSavedArea["productions"] = new JObject();

                    foreach(KeyValuePair<string, AreaProduction> production in baseAreaManagers[i].activeProductions)
                    {
                        JObject currentProduction = new JObject();
                        currentProduction["timeLeft"] = production.Value.timeLeft;
                        currentProduction["productionCount"] = production.Value.productionCount;
                        currentProduction["count"] = production.Value.count;

                        currentSavedArea["productions"][production.Value.ID] = currentProduction;
                    }
                }
                else if(baseAreaManagers[i].activeScavCaseProductions != null)
                {
                    currentSavedArea["productions"] = new JArray();

                    foreach(EFM_ScavCaseProduction production in baseAreaManagers[i].activeScavCaseProductions)
                    {
                        JObject currentProduction = new JObject();
                        currentProduction["timeLeft"] = production.timeLeft;
                        currentProduction["products"] = new JObject();
                        if (production.products.ContainsKey(Mod.ItemRarity.Common))
                        {
                            currentProduction["products"]["common"] = new JObject();
                            currentProduction["products"]["common"]["min"] = production.products[Mod.ItemRarity.Common].x;
                            currentProduction["products"]["common"]["max"] = production.products[Mod.ItemRarity.Common].y;
                        }
                        if (production.products.ContainsKey(Mod.ItemRarity.Rare))
                        {
                            currentProduction["products"]["rare"] = new JObject();
                            currentProduction["products"]["rare"]["min"] = production.products[Mod.ItemRarity.Rare].x;
                            currentProduction["products"]["rare"]["max"] = production.products[Mod.ItemRarity.Rare].y;
                        }
                        if (production.products.ContainsKey(Mod.ItemRarity.Superrare))
                        {
                            currentProduction["products"]["superrare"] = new JObject();
                            currentProduction["products"]["superrare"]["min"] = production.products[Mod.ItemRarity.Superrare].x;
                            currentProduction["products"]["superrare"]["max"] = production.products[Mod.ItemRarity.Superrare].y;
                        }

                        (currentSavedArea["productions"] as JArray).Add(currentProduction);
                    }
                }
                savedAreas.Add(currentSavedArea);
            }

            // Save trader statuses
            JArray savedTraderStatuses = new JArray();
            saveObject["traderStatuses"] = savedTraderStatuses;
            for (int i=0; i<8; ++i)
            {
                JToken currentSavedTraderStatus = new JObject();
                currentSavedTraderStatus["id"] = Mod.traderStatuses[i].id;
                currentSavedTraderStatus["salesSum"] = Mod.traderStatuses[i].salesSum;
                currentSavedTraderStatus["standing"] = Mod.traderStatuses[i].standing;
                currentSavedTraderStatus["unlocked"] = Mod.traderStatuses[i].unlocked;

                // Save tasks
                // TODO: This saves literally all saveable data for tasks their conditions, and the conditions counters
                // We could ommit tasks that dont have any data to save, like the ones that are still locked
                // This makes trader init slower but would save on space. Check if necessary
                currentSavedTraderStatus["tasks"] = new JObject();
                foreach(TraderTask traderTask in Mod.traderStatuses[i].tasks)
                {
                    JObject taskSaveData = new JObject();
                    currentSavedTraderStatus["tasks"][traderTask.ID] = taskSaveData;
                    taskSaveData["state"] = traderTask.taskState.ToString();
                    taskSaveData["conditions"] = new JObject();
                    foreach (TraderTaskCondition traderTaskCondition in traderTask.completionConditions)
                    {
                        JObject conditionSaveData = new JObject();
                        taskSaveData["conditions"][traderTaskCondition.ID] = conditionSaveData;
                        conditionSaveData["fulfilled"] = traderTaskCondition.fulfilled;
                        conditionSaveData["itemCount"] = traderTaskCondition.itemCount;
                        if (traderTaskCondition.counters != null)
                        {
                            conditionSaveData["counters"] = new JObject();
                            foreach (TraderTaskCounterCondition traderTaskCounterCondition in traderTaskCondition.counters)
                            {
                                JObject counterConditionSaveData = new JObject();
                                conditionSaveData["counters"][traderTaskCounterCondition.ID] = counterConditionSaveData;
                                counterConditionSaveData["killCount"] = traderTaskCounterCondition.killCount;
                                counterConditionSaveData["shotCount"] = traderTaskCounterCondition.shotCount;
                                counterConditionSaveData["completed"] = traderTaskCounterCondition.completed;
                            }
                        }
                    }
                }

                currentSavedTraderStatus["itemsToWaitForUnlock"] = JArray.FromObject(Mod.traderStatuses[i].itemsToWaitForUnlock);

                savedTraderStatuses.Add(currentSavedTraderStatus);
            }

            // Reset save data item list
            JArray saveItems = new JArray();
            saveObject["items"] = saveItems;

            // Reset save data item list
            JArray scavSaveItems = new JArray();
            saveObject["scavReturnItems"] = scavSaveItems;

            // Save loose items
            Transform itemsRoot = transform.GetChild(2);
            for (int i = 0; i < itemsRoot.childCount; ++i)
            {
                SaveItem(saveItems, itemsRoot.GetChild(i));
            }

            // Save trade volume items
            for (int i = 0; i < marketManager.tradeVolume.itemsRoot.childCount; ++i)
            {
                SaveItem(saveItems, marketManager.tradeVolume.itemsRoot.GetChild(i));
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
            foreach (EquipmentSlot equipSlot in Mod.equipmentSlots)
            {
                if (equipSlot.CurObject != null)
                {
                    SaveItem(saveItems, equipSlot.CurObject.transform);
                }
            }

            // Save pockets
            foreach (FVRQuickBeltSlot pocketSlot in Mod.pocketSlots)
            {
                if (pocketSlot.CurObject != null)
                {
                    SaveItem(saveItems, pocketSlot.CurObject.transform);
                }
            }

            // Save right shoulder
            if (Mod.rightShoulderObject != null)
            {
                SaveItem(saveItems, Mod.rightShoulderObject.transform);
            }

            // Save scav raid return nodes
            Transform scavReturnNodeParent = transform.GetChild(1).GetChild(25);
            for (int i=0; i < 15; ++i)
            {
                if(scavReturnNodeParent.GetChild(i).childCount > 0)
                {
                    SaveItem(scavSaveItems, scavReturnNodeParent.GetChild(i).GetChild(0));
                }
                else
                {
                    scavSaveItems.Add(null);
                }
            }

            // Save insuredSets
            Mod.insuredItems = new List<InsuredSet>();
            if (Mod.insuredItems != null)
            {
                JArray savedInsuredSets = new JArray();
                saveObject["insuredSets"] = savedInsuredSets;

                for (int i = 0; i < Mod.insuredItems.Count; ++i)
                {
                    JObject newSavedInsuredSet = new JObject();
                    newSavedInsuredSet["returnTime"] = Mod.insuredItems[i].returnTime;
                    newSavedInsuredSet["items"] = JObject.FromObject(Mod.insuredItems[i].items);
                    savedInsuredSets.Add(newSavedInsuredSet);
                }
            }

            // Save triggered exploration triggers
            JArray savedExperiencetriggers = new JArray();
            data["triggeredExplorationTriggers"] = savedExperiencetriggers;
            for (int i = 0; i < 12; ++i)
            {
                savedExperiencetriggers.Add(JArray.FromObject(Mod.triggeredExplorationTriggers[i]));
            }

            // Replace data
            data = saveObject;

            SaveDataToFile();
            Mod.LogInfo("Saved base");
            UpdateSaveButtonList();
        }

        private void SaveItem(JArray listToAddTo, Transform item, FVRViveHand hand = null, int quickBeltSlotIndex = -1)
        {
            if(item == null)
            {
                return;
            }

            JToken savedItem = new JObject();
            savedItem["PhysicalObject"] = new JObject();
            savedItem["PhysicalObject"]["ObjectWrapper"] = new JObject();

            // Get correct item is held and set heldMode
            FVRPhysicalObject itemPhysicalObject = null;
            if (hand != null)
            {
                if (hand.CurrentInteractable is FVRAlternateGrip)
                {
                    // Make sure this item isn't the same as held in right hand because we dont want an item saved twice, and will prioritize right hand
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        itemPhysicalObject = (hand.CurrentInteractable as FVRAlternateGrip).PrimaryObject;
                        savedItem["PhysicalObject"]["heldMode"] = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
                        itemPhysicalObject = item.GetComponentInChildren<FVRPhysicalObject>();
                        savedItem["PhysicalObject"]["heldMode"] = 0;
                    }
                }
                else
                {
                    if (hand.IsThisTheRightHand || hand.CurrentInteractable != hand.OtherHand.CurrentInteractable)
                    {
                        itemPhysicalObject = hand.CurrentInteractable as FVRPhysicalObject;
                        savedItem["PhysicalObject"]["heldMode"] = hand.IsThisTheRightHand ? 1 : 2;
                    }
                    else
                    {
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
            FVRObject itemObjectWrapper = itemPhysicalObject.ObjectWrapper;
            savedItem["PhysicalObject"]["ObjectWrapper"]["ItemID"] = itemObjectWrapper.ItemID;

            // Check if in pocket
            for (int i=0; i< 4; ++i)
            {
                if (Mod.itemsInPocketSlots[i] != null && Mod.itemsInPocketSlots[i].Equals(item.gameObject))
                {
                    savedItem["pocketSlotIndex"] = i;
                    break;
                }
            }

            // Check if in tradeVolume
            if (item.parent != null && item.parent.parent != null && item.parent.parent.GetComponent<TradeVolume>() != null)
            {
                savedItem["inTradeVolume"] = true;
            }

            // Firearm
            if (itemPhysicalObject is FVRFireArm)
            {
                Mod.LogInfo("Saving firearm: " + itemPhysicalObject.name);
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
                if (firearmPhysicalObject.UsesClips && firearmPhysicalObject.Clip != null)
                {
                    savedItem["PhysicalObject"]["ammoContainer"] = new JObject();
                    savedItem["PhysicalObject"]["ammoContainer"]["itemID"] = firearmPhysicalObject.Clip.ObjectWrapper.ItemID;

                    if (firearmPhysicalObject.Clip.HasARound())
                    {
                        JArray newLoadedRoundsInClip = new JArray();
                        foreach (FVRFireArmClip.FVRLoadedRound round in firearmPhysicalObject.Clip.LoadedRounds)
                        {
                            if (round == null || round.LR_ObjectWrapper == null)
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
                else if (firearmPhysicalObject.UsesMagazines && firearmPhysicalObject.Magazine != null)
                {
                    savedItem["PhysicalObject"]["ammoContainer"] = new JObject();
                    savedItem["PhysicalObject"]["ammoContainer"]["itemID"] = firearmPhysicalObject.Magazine.ObjectWrapper == null ? "InternalMag" : firearmPhysicalObject.Magazine.ObjectWrapper.ItemID;

                    if (firearmPhysicalObject.Magazine.HasARound() && firearmPhysicalObject.Magazine.LoadedRounds != null)
                    {
                        JArray newLoadedRoundsInMag = new JArray();
                        foreach (FVRLoadedRound round in firearmPhysicalObject.Magazine.LoadedRounds)
                        {
                            if (round == null || round.LR_ObjectWrapper == null)
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

                // Save to shoulder if necessary
                if (Mod.rightShoulderObject != null && Mod.rightShoulderObject.Equals(item.gameObject))
                {
                    savedItem["isRightShoulder"] = true;
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
                        if (round == null || round.LR_ObjectWrapper == null)
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
                        if (round == null || round.LR_ObjectWrapper == null)
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
            CustomItemWrapper customItemWrapper = itemPhysicalObject.gameObject.GetComponentInChildren<CustomItemWrapper>();
            if (customItemWrapper != null)
            {
                savedItem["itemType"] = (int)customItemWrapper.itemType;
                savedItem["PhysicalObject"]["equipSlot"] = -1;
                savedItem["amount"] = customItemWrapper.amount;
                savedItem["looted"] = customItemWrapper.looted;
                savedItem["insured"] = customItemWrapper.insured;
                savedItem["foundInRaid"] = customItemWrapper.foundInRaid;

                // Armor
                if (customItemWrapper.itemType == Mod.ItemType.BodyArmor)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 1;
                    }
                    savedItem["PhysicalObject"]["armor"] = customItemWrapper.armor;
                    savedItem["PhysicalObject"]["maxArmor"] = customItemWrapper.maxArmor;
                }

                // Rig
                if(customItemWrapper.itemType == Mod.ItemType.Rig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentRig != null && EquipmentSlot.currentRig.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 6;
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

                // ArmoredRig
                if(customItemWrapper.itemType == Mod.ItemType.ArmoredRig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 1;
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
                    savedItem["PhysicalObject"]["armor"] = customItemWrapper.armor;
                    savedItem["PhysicalObject"]["maxArmor"] = customItemWrapper.maxArmor;
                }

                // Backpack
                if(customItemWrapper.itemType == Mod.ItemType.Backpack)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentBackpack != null && EquipmentSlot.currentBackpack.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 0;
                    }
                    if(savedItem["PhysicalObject"]["backpackContents"] == null)
                    {
                        savedItem["PhysicalObject"]["backpackContents"] = new JArray();
                    }
                    JArray saveBPContents = (JArray)savedItem["PhysicalObject"]["backpackContents"];
                    for (int i=0; i < customItemWrapper.containerItemRoot.childCount; ++i)
                    {
                        Mod.LogInfo("Item in backpack " + i + ": "+ customItemWrapper.containerItemRoot.GetChild(i).name);
                        SaveItem(saveBPContents, customItemWrapper.containerItemRoot.GetChild(i));
                    }
                }

                // Container
                if (customItemWrapper.itemType == Mod.ItemType.Container)
                {
                    if (savedItem["PhysicalObject"]["containerContents"] == null)
                    {
                        savedItem["PhysicalObject"]["containerContents"] = new JArray();
                    }
                    JArray saveContainerContents = (JArray)savedItem["PhysicalObject"]["containerContents"];
                    for (int i = 0; i < customItemWrapper.containerItemRoot.childCount; ++i)
                    {
                        Mod.LogInfo("Item in container " + i + ": " + customItemWrapper.containerItemRoot.GetChild(i).name);
                        SaveItem(saveContainerContents, customItemWrapper.containerItemRoot.GetChild(i));
                    }
                }

                // Pouch
                if (customItemWrapper.itemType == Mod.ItemType.Pouch)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentPouch != null && EquipmentSlot.currentPouch.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 7;
                    }
                    if (savedItem["PhysicalObject"]["containerContents"] == null)
                    {
                        savedItem["PhysicalObject"]["containerContents"] = new JArray();
                    }
                    JArray savePouchContents = (JArray)savedItem["PhysicalObject"]["containerContents"];
                    for (int i = 0; i < customItemWrapper.containerItemRoot.childCount; ++i)
                    {
                        Mod.LogInfo("Item in pouch " + i + ": " + customItemWrapper.containerItemRoot.GetChild(i).name);
                        SaveItem(savePouchContents, customItemWrapper.containerItemRoot.GetChild(i));
                    }
                }

                // Helmet
                if (customItemWrapper.itemType == Mod.ItemType.Helmet)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentHeadwear != null && EquipmentSlot.currentHeadwear.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 3;
                    }
                    savedItem["PhysicalObject"]["armor"] = customItemWrapper.armor;
                    savedItem["PhysicalObject"]["maxArmor"] = customItemWrapper.maxArmor;
                }

                // Earpiece
                if (customItemWrapper.itemType == Mod.ItemType.Earpiece)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentEarpiece != null && EquipmentSlot.currentEarpiece.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 2;
                    }
                }

                // FaceCover
                if (customItemWrapper.itemType == Mod.ItemType.FaceCover)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentFaceCover != null && EquipmentSlot.currentFaceCover.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 4;
                    }
                }

                // Eyewear
                if (customItemWrapper.itemType == Mod.ItemType.Eyewear)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentEyewear != null && EquipmentSlot.currentEyewear.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 5;
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == Mod.ItemType.AmmoBox)
                //{
                //    Mod.LogInfo("Item is ammo box");
                //}

                // Money
                if (customItemWrapper.itemType == Mod.ItemType.Money)
                {
                    savedItem["stack"] = customItemWrapper.stack;
                }

                // Consumable
                //if (customItemWrapper.itemType == Mod.ItemType.Consumable)
                //{
                //    Mod.LogInfo("Item is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == Mod.ItemType.Key)
                //{
                //    Mod.LogInfo("is Key");
                //}

                // Dogtag
                if (customItemWrapper.itemType == Mod.ItemType.DogTag)
                {
                    savedItem["dogtagName"] = customItemWrapper.dogtagName;
                    savedItem["dogtagLevel"] = customItemWrapper.dogtagLevel;
                }
            }

            // Vanilla items
            VanillaItemDescriptor vanillaItemDescriptor = itemPhysicalObject.gameObject.GetComponentInChildren<VanillaItemDescriptor>();
            if (vanillaItemDescriptor != null)
            {
                savedItem["looted"] = vanillaItemDescriptor.looted;
                savedItem["insured"] = vanillaItemDescriptor.insured;
                savedItem["foundInRaid"] = vanillaItemDescriptor.foundInRaid;
            }

            listToAddTo.Add(savedItem);
        }

        private void SaveAttachments(FVRPhysicalObject physicalObject, JToken itemPhysicalObject)
        {
            // We want to save attachments curently physically present on physicalObject into the save data itemPhysicalObject
            for (int i = 0; i < physicalObject.AttachmentMounts.Count; ++i)
            {
                for (int j = 0; j < physicalObject.AttachmentMounts[i].AttachmentsList.Count; ++j) 
                {
                    JToken newPhysicalObject = new JObject();
                    if (itemPhysicalObject["AttachmentsList"] == null)
                    {
                        itemPhysicalObject["AttachmentsList"] = new JArray();
                    }
                    ((JArray)itemPhysicalObject["AttachmentsList"]).Add(newPhysicalObject);

                    FVRPhysicalObject currentPhysicalObject = physicalObject.AttachmentMounts[i].AttachmentsList[j];
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
                    newPhysicalObject["mountIndex"] = i;

                    // Fill ObjectWrapper
                    newPhysicalObject["ObjectWrapper"]["ItemID"] = currentPhysicalObject.ObjectWrapper.ItemID;
                }
            }
        }

        public void FinishRaid(FinishRaidState state)
        {
            // Increment raid counters
            ++Mod.totalRaidCount;
            switch (state)
            {
                case FinishRaidState.RunThrough:
                    ++Mod.runThroughRaidCount;
                    ++Mod.survivedRaidCount;
                    break;
                case FinishRaidState.Survived:
                    ++Mod.survivedRaidCount;
                    break;
                case FinishRaidState.MIA:
                    ++Mod.MIARaidCount;
                    ++Mod.failedRaidCount;
                    break;
                case FinishRaidState.KIA:
                    ++Mod.KIARaidCount;
                    ++Mod.failedRaidCount;
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

        public void UpdateBasedOnPlayerLevel()
        {
            marketManager.UpdateBasedOnPlayerLevel();
        }
    }

    public class TimedDisabler : MonoBehaviour
    {
        public float time = 1;
        public float timer;

        public void Start()
        {
            timer = time;
        }

        public void Update()
        {
            timer -= Time.deltaTime;
            if(timer <= 0)
            {
                gameObject.SetActive(false);
                Destroy(this);
            }
        }
    }
}
