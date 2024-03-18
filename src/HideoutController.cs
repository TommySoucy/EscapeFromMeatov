using FistVR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;
using static Valve.VR.SteamVR_TrackedObject;

namespace EFM
{
    public class HideoutController : UIController
    {
        public static HideoutController instance;

        public enum FinishRaidState
        {
            Survived,

            // These are fails
            RunThrough,
            MIA,
            KIA
        }

        // Objects
        public Transform spawn;
        public AreaController areaController;

        // UI
        public Button loadButton;
        public Button[] loadButtons;
        public GameObject[] pages;
        public int pageIndex;
        public GameObject scavBlock;
        public Text raidCountdownTitle;
        public Text raidCountdown;
        public Text timeChoice0;
        public Text timeChoice1;
        public Text confirmChosenCharacter;
        public Text confirmChosenMap;
        public Text confirmChosenTime;
        public Text loadingChosenCharacter;
        public Text loadingChosenMap;
        public Text loadingChosenTime;
        public AudioSource clickAudio;
        public Transform medicalScreen;
        public Transform medicalScreenPartImagesParent;
        public Transform[] medicalScreenPartImages;
        public Text[] medicalScreenPartHealthTexts;
        public Text medicalScreenTotalHealthText;
        public Text totalTreatmentPriceText;
        public Text scavTimerText;
        public Collider scavButtonCollider;
        public Text scavButtonText;
        public GameObject charChoicePanel;
        public GameObject[] saveConfirmTexts;
        public Transform[] optionPages;

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

        // Live data
        public static float[] defaultHealthRates;
        public static float defaultEnergyRate;
        public static float defaultHydrationRate;
        public static DateTime saveTime;
        public static double secondsSinceSave;
        public float time;
        private bool cancelRaidLoad;
        private bool loadingRaid;
        private bool countdownDeploy;
        private float deployTimer;
        private float deployTime = 0; // TODO: Should be 10 but set to 0 for faster debugging
        private int insuredSetIndex = 0;
        private float scavTimer;
        public Dictionary<string, int> inventory;
        public Dictionary<string, List<MeatovItem>> inventoryItems;
        private Dictionary<int, int[]> fullPartConditions;
        private Dictionary<int, GameObject> medicalPartElements;
        private int totalMedicalTreatmentPrice;
        public MarketManager marketManager;
        public static bool marketUI; // whether we are currently in market mode or in area UI mode
        public GCManager GCManager;

        public static float currentExperienceRate = 1;
        public static float currentQuestMoneyReward = 0;
        public static float currentFuelConsumption = 0;
        public static float currentDebuffEndDelay = 0;
        public static float currentScavCooldownTimer = 1;
        public static float currentInsuranceReturnTime = 1;
        public static float currentRagfairCommission = 1; // TODO: Implement, dont forget to use EFM_Skill.skillBoostPercent
        public static Dictionary<Skill.SkillType, float> currentSkillGroupLevelingBoosts;

        public delegate void OnHideoutInventoryChangedDelegate();
        public event OnHideoutInventoryChangedDelegate OnHideoutInventoryChanged;

        public override void Awake()
        {
            base.Awake();

            instance = this;

            Mod.currentLocationIndex = 1;

            // TP Player
            GM.CurrentMovementManager.TeleportToPoint(spawn.position, false, spawn.rotation.eulerAngles);

            Mod.dead = false;

            if (StatusUI.instance == null)
            {
                SetupPlayerRig();
            }

            // Set live data
            //float totalMaxHealth = 0;
            //for (int i = 0; i < 7; ++i)
            //{
            //    Mod.currentMaxHealth[i] = Mod.defaultMaxHealth[i];
            //    totalMaxHealth += Mod.currentMaxHealth[i];
            //    Mod.currentHealthRates[i] += Mod.hideoutHealthRates[i];
            //}
            //GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);
            //if (Mod.justFinishedRaid)
            //{
            //    Mod.currentEnergyRate -= Mod.raidEnergyRate;
            //    Mod.currentHydrationRate -= Mod.raidHydrationRate;
            //}
            //Mod.currentEnergyRate += Mod.hideoutEnergyRate;
            //Mod.currentHydrationRate += Mod.hideoutHydrationRate;
            //if (currentSkillGroupLevelingBoosts == null)
            //{
            //    currentSkillGroupLevelingBoosts = new Dictionary<Skill.SkillType, float>();
            //}

            //// Manage active descriptions dict
            //if (Mod.activeDescriptionsByItemID != null)
            //{
            //    Mod.activeDescriptionsByItemID.Clear();
            //}
            //else
            //{
            //    Mod.activeDescriptionsByItemID = new Dictionary<string, List<DescriptionManager>>();
            //}

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

            // Give any existing rewards to player now
            //if (Mod.rewardsToGive != null && Mod.rewardsToGive.Count > 0)
            //{
            //    foreach (List<TraderTaskReward> rewards in Mod.rewardsToGive)
            //    {
            //        marketManager.GivePlayerRewards(rewards);
            //    }
            //    Mod.rewardsToGive = null;
            //}

            Mod.justFinishedRaid = false;

            init = true;
        }

        public override void Update()
        {
            base.Update();

            if (init)
            {
                UpdateTime();
            }

            //UpdateScavTimer();

            //UpdateEffects();

            //UpdateInsuredSets();

            // Handle raid loading process
            if (cancelRaidLoad)
            {
                loadingRaid = false;
                countdownDeploy = false;

                // Wait until the raid map is done loading before unloading it
                if (Mod.currentRaidBundleRequest.isDone)
                {
                    if (Mod.currentRaidBundleRequest.assetBundle != null)
                    {
                        Mod.currentRaidBundleRequest.assetBundle.Unload(true);
                        cancelRaidLoad = false;
                    }
                    else
                    {
                        cancelRaidLoad = false;
                    }
                    SetPage(0);
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
                    if (Mod.chosenCharIndex == 1)
                    {
                        // TODO: Maybe make these somewhat random? The scav may not have max energy and hydration for example
                        Effect.RemoveEffects();
                        for (int i = 0; i < Mod.health.Length; ++i)
                        {
                            Mod.health[i] = Mod.defaultMaxHealth[i];
                        }
                        Mod.energy = Mod.defaultMaxEnergy;
                        Mod.hydration = Mod.defaultMaxHydration;
                        Mod.stamina = Mod.maxStamina;
                        Mod.weight = 0;
                    }

                    SteamVR_LoadLevel.Begin("Meatov" + confirmChosenMap.text, false, 0.5f, 0f, 0f, 0f, 1f);
                    countdownDeploy = false;
                }
            }
            else if (loadingRaid)
            {
                if (Mod.currentRaidBundleRequest.isDone)
                {
                    if (Mod.currentRaidBundleRequest.assetBundle != null)
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

        public void SetPage(int index)
        {
            pages[pageIndex].SetActive(false);
            pages[index].SetActive(true);
            pageIndex = index;
        }

        private void UpdateScavTimer()
        {
            scavTimer -= Time.deltaTime;
            if (scavTimer <= 0)
            {
                // Enable Scav button, disable scav timer text
                scavButtonCollider.enabled = true;
                scavButtonText.color = Color.white;
                scavTimerText.gameObject.SetActive(false);
            }
            else if (charChoicePanel.activeSelf)
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

        //private void UpdateInsuredSets()
        //{
        //    if (Mod.insuredItems != null && Mod.insuredItems.Count > 0)
        //    {
        //        if (insuredSetIndex >= Mod.insuredItems.Count)
        //        {
        //            insuredSetIndex = 0;
        //        }

        //        if (Mod.insuredItems[insuredSetIndex].returnTime <= GetTimeSeconds())
        //        {
        //            foreach (KeyValuePair<string, int> insuredToSpawn in Mod.insuredItems[insuredSetIndex].items)
        //            {
        //                marketManager.SpawnItem(insuredToSpawn.Key, insuredToSpawn.Value);
        //            }

        //            Mod.insuredItems[insuredSetIndex] = Mod.insuredItems[Mod.insuredItems.Count - 1];
        //            Mod.insuredItems.RemoveAt(insuredSetIndex);
        //        }
        //        else
        //        {
        //            ++insuredSetIndex;
        //        }
        //    }
        //}

        //private void UpdateEffects()
        //{
        //    // Count down timer on all effects, only apply rates, if part is bleeding we dont want to heal it so set to false
        //    bool[] heal = new bool[7];
        //    for (int i = 0; i < 7; ++i)
        //    {
        //        heal[i] = true;
        //    }
        //    for (int i = Effect.effects.Count; i >= 0; --i)
        //    {
        //        if (Effect.effects.Count == 0)
        //        {
        //            break;
        //        }
        //        else if (i >= Effect.effects.Count)
        //        {
        //            continue;
        //        }

        //        Effect effect = Effect.effects[i];
        //        if (effect.active)
        //        {
        //            if (effect.hasTimer)
        //            {
        //                effect.timer -= Time.deltaTime;
        //                if (effect.timer <= 0)
        //                {
        //                    effect.active = false;

        //                    // Unapply effect
        //                    switch (effect.effectType)
        //                    {
        //                        case Effect.EffectType.SkillRate:
        //                            Mod.skills[effect.skillIndex].currentProgress -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.EnergyRate:
        //                            Mod.currentEnergyRate -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.HydrationRate:
        //                            Mod.currentHydrationRate -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.MaxStamina:
        //                            Mod.currentMaxStamina -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.StaminaRate:
        //                            Mod.currentStaminaEffect -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.HandsTremor:
        //                            // TODO: Stop tremors if there are not other tremor effects
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.QuantumTunnelling:
        //                            // TODO: Stop QuantumTunnelling
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(false);
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.HealthRate:
        //                            float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
        //                            if (effect.partIndex == -1)
        //                            {
        //                                for (int j = 0; j < 7; ++j)
        //                                {
        //                                    arrayToUse[j] -= effect.value / 7;
        //                                }
        //                            }
        //                            else
        //                            {
        //                                arrayToUse[effect.partIndex] -= effect.value;
        //                            }
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.RemoveAllBloodLosses:
        //                            // Reactivate all bleeding 
        //                            // Not necessary because when we disabled them we used the disable timer
        //                            break;
        //                        case Effect.EffectType.Contusion:
        //                            bool otherContusions = false;
        //                            foreach (Effect contusionEffectCheck in Effect.effects)
        //                            {
        //                                if (contusionEffectCheck.active && contusionEffectCheck.effectType == Effect.EffectType.Contusion)
        //                                {
        //                                    otherContusions = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!otherContusions)
        //                            {
        //                                // Enable haptic feedback
        //                                GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Enabled;
        //                                // TODO: also set volume to full
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                                }
        //                            }
        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.WeightLimit:
        //                            Mod.effectWeightLimitBonus -= effect.value * 1000;
        //                            Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.DamageModifier:
        //                            Mod.currentDamageModifier -= effect.value;
        //                            if (effect.value < 0)
        //                            {
        //                                Mod.AddSkillExp(5, 6);
        //                            }
        //                            break;
        //                        case Effect.EffectType.Pain:
        //                            // Remove all tremors caused by this pain and disable tremors if no other tremors active
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasPainTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasPainTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasPainTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.StomachBloodloss:
        //                            --Mod.stomachBloodLossCount;
        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.UnknownToxin:
        //                            // Remove all effects caused by this toxin
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                if (causedEffect.effectType == Effect.EffectType.HealthRate)
        //                                {
        //                                    for (int j = 0; j < 7; ++j)
        //                                    {
        //                                        Mod.currentHealthRates[j] -= causedEffect.value / 7;
        //                                    }
        //                                }
        //                                // Could go two layers deep
        //                                foreach (Effect causedCausedEffect in effect.caused)
        //                                {
        //                                    Effect.effects.Remove(causedCausedEffect);
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasToxinTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasToxinTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasToxinTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.BodyTemperature:
        //                            Mod.temperatureOffset -= effect.value;
        //                            break;
        //                        case Effect.EffectType.Antidote:
        //                            // Will remove toxin on ativation, does nothing after
        //                            break;
        //                        case Effect.EffectType.LightBleeding:
        //                        case Effect.EffectType.HeavyBleeding:
        //                            // Remove all effects caused by this bleeding
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                if (causedEffect.effectType == Effect.EffectType.HealthRate)
        //                                {
        //                                    if (causedEffect.partIndex == -1)
        //                                    {
        //                                        for (int j = 0; j < 7; ++j)
        //                                        {
        //                                            Mod.currentNonLethalHealthRates[j] -= causedEffect.value;
        //                                        }
        //                                    }
        //                                    else
        //                                    {
        //                                        Mod.currentNonLethalHealthRates[causedEffect.partIndex] -= causedEffect.value;
        //                                    }
        //                                }
        //                                else // Energy rate
        //                                {
        //                                    Mod.currentEnergyRate -= causedEffect.value;
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(false);
        //                            }

        //                            if (StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(false);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                        case Effect.EffectType.Fracture:
        //                            // Remove all effects caused by this fracture
        //                            foreach (Effect causedEffect in effect.caused)
        //                            {
        //                                // Could go two layers deep
        //                                foreach (Effect causedCausedEffect in effect.caused)
        //                                {
        //                                    Effect.effects.Remove(causedCausedEffect);
        //                                }
        //                                Effect.effects.Remove(causedEffect);
        //                            }
        //                            bool hasFractureTremors = false;
        //                            foreach (Effect effectCheck in Effect.effects)
        //                            {
        //                                if (effectCheck.effectType == Effect.EffectType.HandsTremor && effectCheck.active)
        //                                {
        //                                    hasFractureTremors = true;
        //                                    break;
        //                                }
        //                            }
        //                            if (!hasFractureTremors)
        //                            {
        //                                // TODO: Disable tremors
        //                                if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                                {
        //                                    StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(false);
        //                                }
        //                            }

        //                            if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
        //                            {
        //                                StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
        //                            }

        //                            Mod.AddSkillExp(5, 6);
        //                            break;
        //                    }

        //                    Effect.effects.RemoveAt(i);

        //                    continue;
        //                }
        //            }

        //            if (effect.active && effect.partIndex != -1 && (effect.effectType == Effect.EffectType.LightBleeding ||
        //                                                           effect.effectType == Effect.EffectType.HeavyBleeding ||
        //                                                           (effect.effectType == Effect.EffectType.HealthRate && effect.value < 0)))
        //            {
        //                heal[effect.partIndex] = false;
        //            }
        //        }
        //        else
        //        {
        //            bool effectJustActivated = false;
        //            if (effect.delay > 0)
        //            {
        //                effect.delay -= Time.deltaTime;
        //            }
        //            else if (effect.inactiveTimer <= 0)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }
        //            if (effect.inactiveTimer > 0)
        //            {
        //                effect.inactiveTimer -= Time.deltaTime;
        //            }
        //            else if (effect.delay <= 0)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }
        //            if (effect.hideoutOnly)
        //            {
        //                effect.active = true;
        //                effectJustActivated = true;
        //            }

        //            // Apply effect if it just started being active
        //            if (effectJustActivated)
        //            {
        //                switch (effect.effectType)
        //                {
        //                    case Effect.EffectType.SkillRate:
        //                        Mod.skills[effect.skillIndex].currentProgress += effect.value;
        //                        break;
        //                    case Effect.EffectType.EnergyRate:
        //                        Mod.currentEnergyRate += effect.value;
        //                        break;
        //                    case Effect.EffectType.HydrationRate:
        //                        Mod.currentHydrationRate += effect.value;
        //                        break;
        //                    case Effect.EffectType.MaxStamina:
        //                        Mod.currentMaxStamina += effect.value;
        //                        break;
        //                    case Effect.EffectType.StaminaRate:
        //                        Mod.currentStaminaEffect += effect.value;
        //                        break;
        //                    case Effect.EffectType.HandsTremor:
        //                        // TODO: Begin tremors if there isnt already another active one
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(3).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.QuantumTunnelling:
        //                        // TODO: Begin quantumtunneling if there isnt already another active one
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(4).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HealthRate:
        //                        float[] arrayToUse = effect.nonLethal ? Mod.currentNonLethalHealthRates : Mod.currentHealthRates;
        //                        if (effect.partIndex == -1)
        //                        {
        //                            for (int j = 0; j < 7; ++j)
        //                            {
        //                                arrayToUse[j] += effect.value / 7;
        //                            }
        //                        }
        //                        else
        //                        {
        //                            arrayToUse[effect.partIndex] += effect.value;
        //                        }
        //                        break;
        //                    case Effect.EffectType.RemoveAllBloodLosses:
        //                        // Deactivate all bleeding using disable timer
        //                        foreach (Effect bleedEffect in Effect.effects)
        //                        {
        //                            if (bleedEffect.effectType == Effect.EffectType.LightBleeding || bleedEffect.effectType == Effect.EffectType.HeavyBleeding)
        //                            {
        //                                bleedEffect.active = false;
        //                                bleedEffect.inactiveTimer = effect.timer;

        //                                // Unapply the healthrate caused by this bleed
        //                                Effect causedHealthRate = bleedEffect.caused[0];
        //                                if (causedHealthRate.nonLethal)
        //                                {
        //                                    Mod.currentNonLethalHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
        //                                }
        //                                else
        //                                {
        //                                    Mod.currentHealthRates[causedHealthRate.partIndex] -= causedHealthRate.value;
        //                                }
        //                                Effect causedEnergyRate = bleedEffect.caused[1];
        //                                Mod.currentEnergyRate -= causedEnergyRate.value;
        //                                bleedEffect.caused.Clear();
        //                                Effect.effects.Remove(causedHealthRate);
        //                                Effect.effects.Remove(causedEnergyRate);
        //                            }
        //                        }
        //                        break;
        //                    case Effect.EffectType.Contusion:
        //                        // Disable haptic feedback
        //                        GM.Options.ControlOptions.HapticsState = ControlOptions.HapticsMode.Disabled;
        //                        // TODO: also set volume to 0.33 * volume
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(0).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.WeightLimit:
        //                        Mod.effectWeightLimitBonus += effect.value * 1000;
        //                        Mod.currentWeightLimit = (int)(Mod.baseWeightLimit + Mod.effectWeightLimitBonus + Mod.skillWeightLimitBonus);
        //                        break;
        //                    case Effect.EffectType.DamageModifier:
        //                        Mod.currentDamageModifier += effect.value;
        //                        break;
        //                    case Effect.EffectType.Pain:
        //                        if (UnityEngine.Random.value < 1 - (Mod.skills[4].currentProgress / 10000))
        //                        {
        //                            // Add a tremor effect
        //                            Effect newTremor = new Effect();
        //                            newTremor.effectType = Effect.EffectType.HandsTremor;
        //                            newTremor.delay = 5;
        //                            newTremor.hasTimer = effect.hasTimer;
        //                            newTremor.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                            - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                            Effect.effects.Add(newTremor);
        //                            effect.caused.Add(newTremor);
        //                        }

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(2).gameObject.SetActive(true);
        //                        }

        //                        Mod.AddSkillExp(Skill.stressResistanceHealthNegativeEffect, 4);
        //                        break;
        //                    case Effect.EffectType.StomachBloodloss:
        //                        ++Mod.stomachBloodLossCount;
        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(2).GetChild(3).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.UnknownToxin:
        //                        // Add a pain effect
        //                        Effect newToxinPain = new Effect();
        //                        newToxinPain.effectType = Effect.EffectType.Pain;
        //                        newToxinPain.delay = 5;
        //                        newToxinPain.hasTimer = effect.hasTimer;
        //                        newToxinPain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                           - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newToxinPain.partIndex = 0;
        //                        Effect.effects.Add(newToxinPain);
        //                        effect.caused.Add(newToxinPain);
        //                        // Add a health rate effect
        //                        Effect newToxinHealthRate = new Effect();
        //                        newToxinHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newToxinHealthRate.delay = 5;
        //                        newToxinHealthRate.value = -25 + 25 * (Skill.immunityPoisonBuff * (Mod.skills[6].currentProgress / 100) / 100);
        //                        newToxinHealthRate.hasTimer = effect.hasTimer;
        //                        newToxinHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                 - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        Effect.effects.Add(newToxinHealthRate);
        //                        effect.caused.Add(newToxinHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.BodyTemperature:
        //                        Mod.temperatureOffset += effect.value;
        //                        break;
        //                    case Effect.EffectType.Antidote:
        //                        // Will remove toxin on ativation, does nothing after
        //                        for (int j = Effect.effects.Count; j >= 0; --j)
        //                        {
        //                            if (Effect.effects[j].effectType == Effect.EffectType.UnknownToxin)
        //                            {
        //                                Effect.effects.RemoveAt(j);
        //                                break;
        //                            }
        //                        }
        //                        break;
        //                    case Effect.EffectType.LightBleeding:
        //                        // Add a health rate effect
        //                        Effect newLightBleedingHealthRate = new Effect();
        //                        newLightBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newLightBleedingHealthRate.delay = 5;
        //                        newLightBleedingHealthRate.value = -8;
        //                        newLightBleedingHealthRate.hasTimer = effect.hasTimer;
        //                        newLightBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newLightBleedingHealthRate.partIndex = effect.partIndex;
        //                        newLightBleedingHealthRate.nonLethal = true;
        //                        Effect.effects.Add(newLightBleedingHealthRate);
        //                        effect.caused.Add(newLightBleedingHealthRate);
        //                        // Add a energy rate effect
        //                        Effect newLightBleedingEnergyRate = new Effect();
        //                        newLightBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newLightBleedingEnergyRate.delay = 5;
        //                        newLightBleedingEnergyRate.value = -5;
        //                        newLightBleedingEnergyRate.hasTimer = effect.hasTimer;
        //                        newLightBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newLightBleedingEnergyRate.partIndex = effect.partIndex;
        //                        Effect.effects.Add(newLightBleedingEnergyRate);
        //                        effect.caused.Add(newLightBleedingEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(0).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyBleeding:
        //                        // Add a health rate effect
        //                        Effect newHeavyBleedingHealthRate = new Effect();
        //                        newHeavyBleedingHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyBleedingHealthRate.delay = 5;
        //                        newHeavyBleedingHealthRate.value = -13.5f;
        //                        newHeavyBleedingHealthRate.hasTimer = effect.hasTimer;
        //                        newHeavyBleedingHealthRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newHeavyBleedingHealthRate.nonLethal = true;
        //                        Effect.effects.Add(newHeavyBleedingHealthRate);
        //                        effect.caused.Add(newHeavyBleedingHealthRate);
        //                        // Add a energy rate effect
        //                        Effect newHeavyBleedingEnergyRate = new Effect();
        //                        newHeavyBleedingEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newHeavyBleedingEnergyRate.delay = 5;
        //                        newHeavyBleedingEnergyRate.value = -6;
        //                        newHeavyBleedingEnergyRate.hasTimer = effect.hasTimer;
        //                        newHeavyBleedingEnergyRate.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                                         - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        newHeavyBleedingEnergyRate.partIndex = effect.partIndex;
        //                        Effect.effects.Add(newHeavyBleedingEnergyRate);
        //                        effect.caused.Add(newHeavyBleedingEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Fracture:
        //                        // Add a pain effect
        //                        Effect newFracturePain = new Effect();
        //                        newFracturePain.effectType = Effect.EffectType.Pain;
        //                        newFracturePain.delay = 5;
        //                        newFracturePain.hasTimer = effect.hasTimer;
        //                        newFracturePain.timer = effect.timer + effect.timer * (currentDebuffEndDelay - currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100))
        //                                              - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
        //                        Effect.effects.Add(newFracturePain);
        //                        effect.caused.Add(newFracturePain);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(7).GetChild(effect.partIndex).GetChild(3).GetChild(4).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Dehydration:
        //                        // Add a HealthRate effect
        //                        Effect newDehydrationHealthRate = new Effect();
        //                        newDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newDehydrationHealthRate.value = -60;
        //                        newDehydrationHealthRate.delay = 5;
        //                        newDehydrationHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newDehydrationHealthRate);
        //                        effect.caused.Add(newDehydrationHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyDehydration:
        //                        // Add a HealthRate effect
        //                        Effect newHeavyDehydrationHealthRate = new Effect();
        //                        newHeavyDehydrationHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyDehydrationHealthRate.value = -350;
        //                        newHeavyDehydrationHealthRate.delay = 5;
        //                        newHeavyDehydrationHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newHeavyDehydrationHealthRate);
        //                        effect.caused.Add(newHeavyDehydrationHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.Fatigue:
        //                        Mod.fatigue = true;

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.HeavyFatigue:
        //                        // Add a HealthRate effect
        //                        Effect newHeavyFatigueHealthRate = new Effect();
        //                        newHeavyFatigueHealthRate.effectType = Effect.EffectType.HealthRate;
        //                        newHeavyFatigueHealthRate.value = -30;
        //                        newHeavyFatigueHealthRate.delay = 5;
        //                        newHeavyFatigueHealthRate.hasTimer = false;
        //                        Effect.effects.Add(newHeavyFatigueHealthRate);
        //                        effect.caused.Add(newHeavyFatigueHealthRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                    case Effect.EffectType.OverweightFatigue:
        //                        // Add a EnergyRate effect
        //                        Effect newOverweightFatigueEnergyRate = new Effect();
        //                        newOverweightFatigueEnergyRate.effectType = Effect.EffectType.EnergyRate;
        //                        newOverweightFatigueEnergyRate.value = -4;
        //                        newOverweightFatigueEnergyRate.delay = 5;
        //                        newOverweightFatigueEnergyRate.hasTimer = false;
        //                        Effect.effects.Add(newOverweightFatigueEnergyRate);
        //                        effect.caused.Add(newOverweightFatigueEnergyRate);

        //                        if (!StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.activeSelf)
        //                        {
        //                            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
        //                        }
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    // Apply health, energy, and hydration rates
        //    float healthDelta = 0;
        //    float health = 0;
        //    float maxHealthTotal = 0;
        //    for (int i = 0; i < 7; ++i)
        //    {
        //        maxHealthTotal += Mod.currentMaxHealth[i];
        //        float currentHealthDelta = Mod.currentHealthRates[i] + Mod.currentNonLethalHealthRates[i];
        //        if (heal[i] && currentHealthDelta > 0)
        //        {
        //            Mod.health[i] = Mathf.Clamp(Mod.health[i] + currentHealthDelta * (Time.deltaTime / 60), 1, Mod.currentMaxHealth[i]);

        //            healthDelta += currentHealthDelta;
        //            health += Mod.health[i];
        //        }
        //        StatusUI.instance.partHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
        //        StatusUI.instance.partHealthImages[i].color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);

        //        if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
        //        {
        //            medicalScreenPartHealthTexts[i].text = String.Format("{0:0}", Mod.health[i]) + "/" + String.Format("{0:0}", Mod.currentMaxHealth[i]);
        //            medicalScreenPartImages[i].GetComponent<Image>().color = Color.Lerp(Color.red, Color.white, Mod.health[i] / Mod.currentMaxHealth[i]);
        //        }
        //    }
        //    if (healthDelta != 0)
        //    {
        //        if (!StatusUI.instance.healthDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.healthDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.healthDelta.text = (healthDelta > 0 ? "+" : "") + String.Format("{0:0.#}/min", healthDelta);
        //    }
        //    else if (StatusUI.instance.healthDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.healthDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.healthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal);
        //    if (medicalScreen != null && medicalScreen.gameObject.activeSelf)
        //    {
        //        medicalScreenTotalHealthText.text = String.Format("{0:0}/{1}", health, maxHealthTotal); ;
        //    }
        //    GM.CurrentPlayerBody.SetHealthThreshold(maxHealthTotal);
        //    GM.CurrentPlayerBody.Health = health; // This must be done after setting health threshold because setting health threshold also sets health

        //    if (Mod.currentHydrationRate > 0)
        //    {
        //        Mod.hydration = Mathf.Clamp(Mod.hydration + Mod.currentHydrationRate * (Time.deltaTime / 60), 0, Mod.maxHydration);

        //        if (!StatusUI.instance.hydrationDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.hydrationDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.hydrationDelta.text = (Mod.currentHydrationRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentHydrationRate);
        //    }
        //    else if (StatusUI.instance.hydrationDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.hydrationDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.hydrationText.text = String.Format("{0:0}/{1}", Mod.hydration, Mod.maxHydration);
        //    if (Mod.hydration > 20)
        //    {
        //        // Remove any dehydration effect
        //        if (Mod.dehydrationEffect != null)
        //        {
        //            // Disable 
        //            if (Mod.dehydrationEffect.caused.Count > 0)
        //            {
        //                for (int j = 0; j < 7; ++j)
        //                {
        //                    Mod.currentHealthRates[j] -= Mod.dehydrationEffect.caused[0].value / 7;
        //                }
        //                Effect.effects.Remove(Mod.dehydrationEffect.caused[0]);
        //            }
        //            Effect.effects.Remove(Mod.dehydrationEffect);
        //        }

        //        if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.activeSelf)
        //        {
        //            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(5).gameObject.SetActive(false);
        //        }
        //    }

        //    if (Mod.currentEnergyRate > 0)
        //    {
        //        Mod.energy = Mathf.Clamp(Mod.energy + Mod.currentEnergyRate * (Time.deltaTime / 60), 0, Mod.maxEnergy);

        //        if (!StatusUI.instance.energyDelta.gameObject.activeSelf)
        //        {
        //            StatusUI.instance.energyDelta.gameObject.SetActive(true);
        //        }
        //        StatusUI.instance.energyDelta.text = (Mod.currentEnergyRate > 0 ? "+" : "") + String.Format("{0:0}/min", Mod.currentEnergyRate);
        //    }
        //    else if (StatusUI.instance.energyDelta.gameObject.activeSelf)
        //    {
        //        StatusUI.instance.energyDelta.gameObject.SetActive(false);
        //    }
        //    StatusUI.instance.energyText.text = String.Format("{0:0}/{1}", Mod.energy, Mod.maxEnergy);
        //    if (Mod.energy > 20)
        //    {
        //        // Remove any fatigue effect
        //        if (Mod.fatigueEffect != null)
        //        {
        //            // Disable 
        //            if (Mod.fatigueEffect.caused.Count > 0)
        //            {
        //                for (int j = 0; j < 7; ++j)
        //                {
        //                    Mod.currentHealthRates[j] -= Mod.fatigueEffect.caused[0].value / 7;
        //                }
        //                Effect.effects.Remove(Mod.fatigueEffect.caused[0]);
        //            }
        //            Effect.effects.Remove(Mod.fatigueEffect);
        //        }

        //        if (StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.activeSelf)
        //        {
        //            StatusUI.instance.transform.GetChild(0).GetChild(2).GetChild(8).gameObject.SetActive(false);
        //        }
        //    }
        //}

        private void UpdateTime()
        {
            time += UnityEngine.Time.deltaTime * UIController.meatovTimeMultiplier;

            time %= 86400;

            // Update time texts
            string formattedTime0 = Mod.FormatTimeString(time);
            timeChoice0.text = formattedTime0;

            float offsetTime = (time + 43200) % 86400; // Offset time by 12 hours
            string formattedTime1 = Mod.FormatTimeString(offsetTime);
            timeChoice1.text = formattedTime1;

            confirmChosenTime.text = Mod.chosenTimeIndex == 0 ? formattedTime0 : formattedTime1;
            loadingChosenTime.text = confirmChosenTime.text;
        }

        private void SetupPlayerRig()
        {
            Mod.LogInfo("Setting up player rig, current player body null?: " +(GM.CurrentPlayerBody == null)+ ", current player root null?: " + (GM.CurrentPlayerBody == null));
            // Set player's max health
            //float totalMaxHealth = 0;
            //foreach (float bodyPartMaxHealth in Mod.currentMaxHealth)
            //{
            //    totalMaxHealth += bodyPartMaxHealth;
            //}

            // Player status
            Instantiate(Mod.playerStatusUIPrefab, GM.CurrentPlayerRoot);
            // Consumable indicator
            //Mod.consumeUI = Instantiate(Mod.consumeUIPrefab, GM.CurrentPlayerRoot);
            //Mod.consumeUIText = Mod.consumeUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            //Mod.consumeUI.SetActive(false);
            //// Stack split UI
            //Mod.stackSplitUI = Instantiate(Mod.stackSplitUIPrefab, GM.CurrentPlayerRoot);
            //Mod.stackSplitUIText = Mod.stackSplitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            //Mod.stackSplitUICursor = Mod.stackSplitUI.transform.GetChild(0).GetChild(0).GetChild(1).GetChild(6);
            //Mod.stackSplitUI.SetActive(false);
            //// Extraction UI
            //Mod.extractionUI = Instantiate(Mod.extractionUIPrefab, GM.CurrentPlayerRoot);
            //Mod.extractionUIText = Mod.extractionUI.transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
            //Mod.extractionUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            //Mod.extractionUI.SetActive(false);
            //// Extraction limit UI
            //Mod.extractionLimitUI = Instantiate(Mod.extractionLimitUIPrefab, GM.CurrentPlayerRoot);
            //Mod.extractionLimitUIText = Mod.extractionLimitUI.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<Text>();
            //Mod.extractionLimitUI.transform.rotation = Quaternion.Euler(-25, 0, 0);
            //Mod.extractionLimitUI.SetActive(false);
            //// ItemDescription UIs
            //Mod.leftDescriptionUI = Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.LeftHand);
            //Mod.leftDescriptionManager = Mod.leftDescriptionUI.AddComponent<DescriptionManager>();
            //Mod.leftDescriptionManager.Init();
            //Mod.rightDescriptionUI = Instantiate(Mod.itemDescriptionUIPrefab, GM.CurrentPlayerBody.RightHand);
            //Mod.rightDescriptionManager = Mod.rightDescriptionUI.AddComponent<DescriptionManager>();
            //Mod.rightDescriptionManager.Init();
            //// Stamina bar
            Mod.staminaBarUI = Instantiate(Mod.staminaBarPrefab, GM.CurrentPlayerBody.Head);
            Mod.staminaBarUI.transform.localRotation = Quaternion.Euler(-25, 0, 0);
            Mod.staminaBarUI.transform.localPosition = new Vector3(0, -0.4f, 0.6f);
            Mod.staminaBarUI.transform.localScale = Vector3.one * 0.0015f;

            // Add our own hand component to each hand
            Mod.rightHand = GM.CurrentPlayerBody.RightHand.gameObject.AddComponent<Hand>();
            Mod.leftHand = GM.CurrentPlayerBody.LeftHand.gameObject.AddComponent<Hand>();
            Mod.rightHand.otherHand = Mod.leftHand;
            Mod.leftHand.otherHand = Mod.rightHand;

            // Set movement control
            GM.CurrentMovementManager.Mode = FVRMovementManager.MovementMode.TwinStick;
            GM.Options.MovementOptions.Touchpad_Confirm = FVRMovementManager.TwoAxisMovementConfirm.OnTouch;
            GM.Options.ControlOptions.CCM = ControlOptions.CoreControlMode.Streamlined;

            // Disable wrist menus
            //Mod.rightHand.fvrHand.DisableWristMenu();
            //Mod.leftHand.fvrHand.DisableWristMenu();

            // Set QB config to pockets
            Mod.pocketSlots = new FVRQuickBeltSlot[4];
            Mod.itemsInPocketSlots = new MeatovItem[4];
            GM.CurrentPlayerBody.ConfigureQuickbelt(Mod.pocketsConfigIndex);
        }

        public void ProcessData()
        {
            // Clear other active slots since we shouldn't have any on load
            Mod.looseRigSlots.Clear();

            // Load the save time
            saveTime = new DateTime((long)HideoutController.loadedData["time"]);
            secondsSinceSave = (float)DateTime.UtcNow.Subtract(saveTime).TotalSeconds;
            float minutesSinceSave = (float)secondsSinceSave / 60.0f;

            // Load player data only if we don't want to use the data from the PMC raid we just finished
            if (!Mod.justFinishedRaid || Mod.chosenCharIndex == 1)
            {
                Mod.level = (int)loadedData["level"];
                Mod.experience = (int)loadedData["experience"];
                Mod.health = loadedData["health"].ToObject<float[]>();
                for (int i = 0; i < Mod.health.Length; ++i)
                {
                    Mod.health[i] += Mathf.Min(Mod.currentHealthRates[i] * minutesSinceSave, Mod.currentMaxHealth[i]);
                }
                Mod.currentHealthRates = loadedData["healthRates"].ToObject<float[]>();
                Mod.currentNonLethalHealthRates = loadedData["nonLethalHealthRates"].ToObject<float[]>();
                Mod.currentMaxHealth = loadedData["maxHealth"].ToObject<float[]>();
                Mod.currentMaxHydration = (float)loadedData["maxHydration"];
                Mod.hydration = Mathf.Min((float)loadedData["hydration"] + Mod.currentHydrationRate * minutesSinceSave, Mod.defaultMaxHydration);
                Mod.currentMaxEnergy = (float)loadedData["maxEnergy"];
                Mod.energy = Mathf.Min((float)loadedData["energy"] + Mod.currentEnergyRate * minutesSinceSave, Mod.defaultMaxEnergy);
                Mod.maxStamina = (float)loadedData["maxStamina"];
                Mod.stamina = Mod.maxStamina;
                Mod.weight = (int)loadedData["weight"];
                Mod.totalRaidCount = (int)loadedData["totalRaidCount"];
                Mod.runThroughRaidCount = (int)loadedData["runThroughRaidCount"];
                Mod.survivedRaidCount = (int)loadedData["survivedRaidCount"];
                Mod.MIARaidCount = (int)loadedData["MIARaidCount"];
                Mod.KIARaidCount = (int)loadedData["KIARaidCount"];
                Mod.failedRaidCount = (int)loadedData["failedRaidCount"];
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].progress = (float)loadedData["skills"][i]["progress"];
                    Mod.skills[i].currentProgress = Mod.skills[i].progress;
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }

                // Set bonuses depending on skills
                float enduranceLevel = Mod.skills[0].progress / 100;
                Mod.maxStamina += Mod.maxStamina / 100 * enduranceLevel;
                if (enduranceLevel >= 51)
                {
                    Mod.maxStamina += 20;
                }

                // Player items
                if (Mod.playerInventory == null)
                {
                    Mod.playerInventory = new Dictionary<string, int>();
                    Mod.playerInventoryItems = new Dictionary<string, List<MeatovItem>>();
                }
                TODO: // Load player Items
            }
            else if (Mod.justFinishedRaid)
            {
                for (int i = 0; i < 64; ++i)
                {
                    Mod.skills[i].increasing = false;
                    Mod.skills[i].dimishingReturns = false;
                    Mod.skills[i].raidProgress = 0;
                }
            }

            // Load hideout items
            if (inventoryItems == null)
            {
                inventory = new Dictionary<string, int>();
                inventoryItems = new Dictionary<string, List<MeatovItem>>();
            }

            TODO: // Load hideout items

            // Load trader data
            JArray traderDataArray = loadedData["hideout"]["traders"] as JArray;
            for(int i=0; i < Mod.traders.Length; ++i)
            {
                Mod.traders[i].LoadData(traderDataArray[i]);
            }
            // Load task data only after loading all trader data because some task conditions are dependent on trader live data
            for (int i = 0; i < Mod.traders.Length; ++i)
            {
                for (int j = 0; j < Mod.traders[i].tasks.Count; ++j)
                {
                    Mod.traders[i].tasks[j].LoadData(traderDataArray[i]["tasks"][Mod.traders[i].tasks[j].ID]);
                }
            }

            //TraderStatus.fenceRestockTimer = (float)loadedData["fenceRestockTimer"] - secondsSinceSave;

            

            scavTimer = (float)((long)loadedData["scavTimer"] - secondsSinceSave);

            // Instantiate items
            //Transform itemsRoot = transform.GetChild(2);
            //JArray loadedItems = (JArray)loadedData["items"];
            //for (int i = 0; i < loadedItems.Count; ++i)
            //{
            //    JToken item = loadedItems[i];

            //    // If just finished raid as PMC, skip any items that are on player since we want to keep what player found in raid
            //    if (Mod.justFinishedRaid && Mod.chosenCharIndex == 0 && ((item["PhysicalObject"]["equipSlot"] != null && (int)item["PhysicalObject"]["equipSlot"] != -1) || (int)item["PhysicalObject"]["heldMode"] != 0 || (int)item["PhysicalObject"]["m_quickBeltSlot"] != -1 || item["pocketSlotIndex"] != null || item["isRightShoulder"] != null))
            //    {
            //        continue;
            //    }

            //    LoadSavedItem(itemsRoot, item);
            //}

            //// Load scav return items
            //if (loadedData["scavReturnItems"] != null)
            //{
            //    Transform scavReturnNodeParent = transform.GetChild(1).GetChild(25);
            //    JArray loadedScavReturnItems = (JArray)loadedData["scavReturnItems"];
            //    for (int i = 0; i < loadedScavReturnItems.Count; ++i)
            //    {
            //        if (loadedScavReturnItems[i] == null || loadedScavReturnItems[i].Type == JTokenType.Null)
            //        {
            //            continue;
            //        }

            //        LoadSavedItem(scavReturnNodeParent.GetChild(i), loadedScavReturnItems[i]);
            //    }
            //}

            // Check for insuredSets
            //if (Mod.insuredItems == null)
            //{
            //    Mod.insuredItems = new List<InsuredSet>();
            //}
            //if (loadedData["insuredSets"] != null)
            //{
            //    JArray loadedInsuredSets = (JArray)loadedData["insuredSets"];

            //    for (int i = 0; i < loadedInsuredSets.Count; ++i)
            //    {
            //        InsuredSet newInsuredSet = new InsuredSet();
            //        newInsuredSet.returnTime = (long)loadedInsuredSets[i]["returnTime"];
            //        newInsuredSet.items = loadedInsuredSets[i]["items"].ToObject<Dictionary<string, int>>();
            //        Mod.insuredItems.Add(newInsuredSet);
            //    }
            //}

            Mod.preventLoadMagUpdateLists = false;

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
                if (loadedData["triggeredExplorationTriggers"] != null)
                {
                    for (int i = 0; i < 12; ++i)
                    {
                        Mod.triggeredExplorationTriggers.Add(loadedData["triggeredExplorationTriggers"][i].ToObject<List<bool>>());
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
            for (int i = 0; i < controlsParent.childCount; ++i)
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
                if (i == controlsParent.childCount - 1)
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
            for (int i = 5; i < 18; ++i)
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
                Transform toShow = tutorialTransform.GetChild(i + 1);
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

        public void AddToInventory(MeatovItem item, bool stackOnly = false, int stackDifference = 0)
        {
            if (stackOnly)
            {
                if (inventory.ContainsKey(item.H3ID))
                {
                    inventory[item.H3ID] += stackDifference;

                    if (inventory[item.H3ID] <= 0)
                    {
                        Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        inventory.Remove(item.H3ID);
                        inventoryItems.Remove(item.H3ID);
                    }
                }
                else
                {
                    Mod.LogError("DEV: AddToPlayerInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in playerInventory:\n" + Environment.StackTrace);
                }
            }
            else
            {
                if (inventory.ContainsKey(item.H3ID))
                {
                    inventory[item.H3ID] += item.stack;
                    inventoryItems[item.H3ID].Add(item);
                }
                else
                {
                    inventory.Add(item.H3ID, item.stack);
                    inventoryItems.Add(item.H3ID, new List<MeatovItem> { item });
                }

                if (item.itemType == MeatovItem.ItemType.AmmoBox)
                {
                    FVRFireArmMagazine boxMagazine = item.physObj as FVRFireArmMagazine;
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
                else if (item.physObj is FVRFireArmMagazine)
                {
                    FVRFireArmMagazine asMagazine = item.physObj as FVRFireArmMagazine;
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
                else if (item.physObj is FVRFireArmClip)
                {
                    Mod.LogInfo("3");
                    FVRFireArmClip asClip = item.physObj as FVRFireArmClip;
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
                else if (item.physObj is FVRFireArmRound)
                {
                    FVRFireArmRound asRound = item.physObj as FVRFireArmRound;
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

            // Check for more items that may be contained inside this one
            // NOTE: Now handled by MeatovItem.UpdateInventories() and parent tracking system
            //if (item != null)
            //{
            //    if (item.itemType == MeatovItem.ItemType.Backpack || item.itemType == MeatovItem.ItemType.Container || item.itemType == MeatovItem.ItemType.Pouch)
            //    {
            //        foreach (Transform innerItem in item.containerItemRoot)
            //        {
            //            AddToInventory(innerItem, updateTypeLists);
            //        }
            //    }
            //    else if (item.itemType == MeatovItem.ItemType.Rig || item.itemType == MeatovItem.ItemType.ArmoredRig)
            //    {
            //        foreach (GameObject innerItem in item.itemsInSlots)
            //        {
            //            if (innerItem != null)
            //            {
            //                AddToInventory(innerItem.transform, updateTypeLists);
            //            }
            //        }
            //    }
            //}

            if(OnHideoutInventoryChanged != null)
            {
                OnHideoutInventoryChanged();
            }
        }

        public static bool RemoveFromContainer(Transform item, MeatovItem MI)
        {
            if (item.transform.parent != null && item.transform.parent.parent != null)
            {
                MeatovItem containerItemWrapper = item.transform.parent.parent.GetComponent<MeatovItem>();
                if (containerItemWrapper != null && (containerItemWrapper.itemType == MeatovItem.ItemType.Backpack ||
                                                    containerItemWrapper.itemType == MeatovItem.ItemType.Container ||
                                                    containerItemWrapper.itemType == MeatovItem.ItemType.Pouch))
                {
                    containerItemWrapper.currentWeight -= MI.currentWeight;

                    containerItemWrapper.containingVolume -= MI.volumes[MI.mode];

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

        public void RemoveFromInventory(MeatovItem item)
        {
            if (inventory.ContainsKey(item.H3ID))
            {
                inventory[item.H3ID] -= item.stack;
                inventoryItems[item.H3ID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove " + item.H3ID + " from hideout inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (inventory[item.H3ID] == 0)
            {
                inventory.Remove(item.H3ID);
                inventoryItems.Remove(item.H3ID);
            }

            if (item.itemType == MeatovItem.ItemType.AmmoBox)
            {
                FVRFireArmMagazine boxMagazine = item.physObj as FVRFireArmMagazine;
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
                            Mod.LogError("Attempting to remove " + item.H3ID + "  which is ammo box that contains ammo: " + roundName + " from hideout inventory but ammo name was not found in roundsByType:\n" + Environment.StackTrace);
                        }
                    }
                    else
                    {
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is ammo box that contains ammo: " + roundName + " from hideout inventory but ammo type was not found in roundsByType:\n" + Environment.StackTrace);
                    }
                }
            }
            else if (item.physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asMagazine = item.physObj as FVRFireArmMagazine;
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
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is mag from hideout inventory but its name was not found in magazinesByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is mag from hideout inventory but its type was not found in magazinesByType:\n" + Environment.StackTrace);
                }
            }
            else if (item.physObj is FVRFireArmClip)
            {
                FVRFireArmClip asClip = item.physObj as FVRFireArmClip;
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
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is clip from hideout inventory but its name was not found in clipsByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is clip from hideout inventory but its type was not found in clipsByType:\n" + Environment.StackTrace);
                }
            }
            else if (item.physObj is FVRFireArmRound)
            {
                FVRFireArmRound asRound = item.physObj as FVRFireArmRound;
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
                        Mod.LogError("Attempting to remove " + item.H3ID + "  which is round from hideout inventory but its name was not found in roundsByType:\n" + Environment.StackTrace);
                    }
                }
                else
                {
                    Mod.LogError("Attempting to remove " + item.H3ID + "  which is round from hideout inventory but its type was not found in roundsByType:\n" + Environment.StackTrace);
                }
            }

            // Check for more items that may be contained inside this one
            // NOTE: Now handled by MeatovItem.UpdateInventories() and parent tracking system
            //if (item != null)
            //{
            //    if (item.itemType == MeatovItem.ItemType.Backpack || item.itemType == MeatovItem.ItemType.Container || item.itemType == MeatovItem.ItemType.Pouch)
            //    {
            //        foreach (Transform innerItem in item.containerItemRoot)
            //        {
            //            RemoveFromInventory(innerItem, updateTypeLists);
            //        }
            //    }
            //    else if (item.itemType == MeatovItem.ItemType.Rig || item.itemType == MeatovItem.ItemType.ArmoredRig)
            //    {
            //        foreach (GameObject innerItem in item.itemsInSlots)
            //        {
            //            if (innerItem != null)
            //            {
            //                RemoveFromInventory(innerItem.transform, updateTypeLists);
            //            }
            //        }
            //    }
            //}

            if (OnHideoutInventoryChanged != null)
            {
                OnHideoutInventoryChanged();
            }
        }

        private GameObject LoadSavedItem(Transform parent, JToken item, int locationIndex = -1, bool inAll = false)
        {
            Mod.LogInfo("Loading item " + item["PhysicalObject"]["ObjectWrapper"]["ItemID"]);
            int parsedID = -1;
            GameObject prefabToUse = null;
            if (int.TryParse(item["PhysicalObject"]["ObjectWrapper"]["ItemID"].ToString(), out parsedID))
            {
                // Custom item, fetch from our own assets
                prefabToUse = Mod.GetItemPrefab(parsedID);
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
            MeatovItem MI = itemObject.GetComponent<MeatovItem>();

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
                MI.takeCurrentLocation = false;
                MI.locationIndex = 0;
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
                        containerPrefabToUse = Mod.GetItemPrefab(parsedContainerID);
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
                        MeatovItem clipMI = clipPhysicalObject.GetComponent<MeatovItem>();
                        if (locationIndex != -1)
                        {
                            clipMI.takeCurrentLocation = false;
                            clipMI.locationIndex = locationIndex;
                        }

                        clipPhysicalObject.Load(firearmPhysicalObject);
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
                            MeatovItem magMI = magPhysicalObject.GetComponent<MeatovItem>();
                            if (locationIndex != -1)
                            {
                                magMI.takeCurrentLocation = false;
                                magMI.locationIndex = locationIndex;
                            }

                            magPhysicalObject.Load(firearmPhysicalObject);
                        }
                    }
                }

                // Set to right shoulder if this was saved in it
                if (item["isRightShoulder"] != null)
                {
                    itemPhysicalObject.SetQuickBeltSlot(Mod.rightShoulderSlot);

                    MI.UpdateInventories();

                    Mod.rightShoulderObject = itemObject;
                    itemObject.SetActive(false);
                }

                if (firearmPhysicalObject is ClosedBoltWeapon)
                {
                    (firearmPhysicalObject as ClosedBoltWeapon).CockHammer();
                }
                else if (firearmPhysicalObject is BoltActionRifle)
                {
                    (firearmPhysicalObject as BoltActionRifle).CockHammer();
                }
                else if (firearmPhysicalObject is TubeFedShotgun)
                {
                    (firearmPhysicalObject as TubeFedShotgun).CockHammer();
                }
                else if (firearmPhysicalObject is BreakActionWeapon)
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
            if (MI != null)
            {
                if (inAll)
                {
                    MI.inAll = true;
                }
                else
                {
                    // When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
                    Mod.RemoveFromAll(itemPhysicalObject, MI);
                }

                MI.itemType = (MeatovItem.ItemType)(int)item["itemType"];
                MI.amount = (int)item["amount"];
                MI.looted = (bool)item["looted"];
                MI.insured = (bool)item["insured"];
                if (locationIndex != -1)
                {
                    MI.takeCurrentLocation = false;
                    MI.locationIndex = locationIndex;
                }

                // Armor
                if (MI.itemType == MeatovItem.ItemType.ArmoredRig || MI.itemType == MeatovItem.ItemType.BodyArmor)
                {
                    Mod.LogInfo("is armor");
                    MI.armor = (float)item["PhysicalObject"]["armor"];
                    MI.maxArmor = (float)item["PhysicalObject"]["maxArmor"];

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }
                }

                // Rig
                if (MI.itemType == MeatovItem.ItemType.ArmoredRig || MI.itemType == MeatovItem.ItemType.Rig)
                {
                    Mod.LogInfo("is rig");
                    bool equipped = (int)item["PhysicalObject"]["equipSlot"] != -1;
                    if (equipped)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["quickBeltSlotContents"] != null)
                    {
                        JArray loadedQBContents = (JArray)item["PhysicalObject"]["quickBeltSlotContents"];
                        for (int j = 0; j < loadedQBContents.Count; ++j)
                        {
                            if (loadedQBContents[j] == null || loadedQBContents[j].Type == JTokenType.Null)
                            {
                                MI.itemsInSlots[j] = null;
                            }
                            else
                            {
                                //MI.itemsInSlots[j] = LoadSavedItem(null, loadedQBContents[j], MI.locationIndex, equipped);
                                //MI.itemsInSlots[j].SetActive(false); // Inactive by default // TODO: If we ever save the mode of the rig, and therefore could load an open rig, then we should check this mode before setting active or inactive
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
                        MeatovItem.SetCurrentWeight(MI);

                        MI.UpdateClosedMode();
                    }
                }

                // Backpack
                if (MI.itemType == MeatovItem.ItemType.Backpack)
                {
                    Mod.LogInfo("is backpack");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["backpackContents"] != null)
                    {
                        JArray loadedBPContents = (JArray)item["PhysicalObject"]["backpackContents"];
                        for (int j = 0; j < loadedBPContents.Count; ++j)
                        {
                            LoadSavedItem(MI.containerItemRoot, loadedBPContents[j], MI.locationIndex, false);
                        }
                    }

                    MI.UpdateClosedMode();
                }

                // Container
                if (MI.itemType == MeatovItem.ItemType.Container)
                {
                    Mod.LogInfo("is container");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedContainerContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedContainerContents.Count; ++j)
                        {
                            LoadSavedItem(MI.containerItemRoot, loadedContainerContents[j], MI.locationIndex, false);
                        }
                    }
                }

                // Pouch
                if (MI.itemType == MeatovItem.ItemType.Pouch)
                {
                    Mod.LogInfo("is Pouch");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }

                    if (item["PhysicalObject"]["containerContents"] != null)
                    {
                        JArray loadedPouchContents = (JArray)item["PhysicalObject"]["containerContents"];
                        for (int j = 0; j < loadedPouchContents.Count; ++j)
                        {
                            LoadSavedItem(MI.containerItemRoot, loadedPouchContents[j], MI.locationIndex, false);
                        }
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == MeatovItem.ItemType.AmmoBox)
                //{
                //    Mod.LogInfo("is ammo box");
                //}

                // Money
                if (MI.itemType == MeatovItem.ItemType.Money)
                {
                    Mod.LogInfo("is money");

                    MI.stack = (int)item["stack"];
                    MI.UpdateStackModel();
                }

                // Consumable
                //if (customItemWrapper.itemType == MeatovItem.ItemType.Consumable)
                //{
                //    Mod.LogInfo("is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == MeatovItem.ItemType.Key)
                //{
                //    Mod.LogInfo("is Key");
                //}

                // Earpiece
                if (MI.itemType == MeatovItem.ItemType.Earpiece)
                {
                    Mod.LogInfo("is Earpiece");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }
                }

                // Face Cover
                if (MI.itemType == MeatovItem.ItemType.FaceCover)
                {
                    Mod.LogInfo("is Face Cover");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }
                }

                // Eyewear
                if (MI.itemType == MeatovItem.ItemType.Eyewear)
                {
                    Mod.LogInfo("is Eyewear");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }
                }

                // Headwear
                if (MI.itemType == MeatovItem.ItemType.Headwear)
                {
                    Mod.LogInfo("is Headwear");

                    if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                    {
                        MI.takeCurrentLocation = false;
                        MI.locationIndex = 0;
                    }
                }

                // Dogtag
                if (MI.itemType == MeatovItem.ItemType.DogTag)
                {
                    MI.dogtagName = item["dogtagName"].ToString();
                    MI.dogtagLevel = (int)item["dogtagLevel"];
                }

                // Equip the item if it has an equip slot
                if ((int)item["PhysicalObject"]["equipSlot"] != -1)
                {
                    MI.takeCurrentLocation = false;

                    int equipSlotIndex = (int)item["PhysicalObject"]["equipSlot"];
                    Mod.LogInfo("Item has equip slot: " + equipSlotIndex);
                    FVRQuickBeltSlot equipSlot = StatusUI.instance.equipmentSlots[equipSlotIndex];
                    itemPhysicalObject.SetQuickBeltSlot(equipSlot);
                    itemPhysicalObject.SetParentage(null);

                    if (equipSlotIndex == 0)
                    {
                        Mod.leftShoulderObject = itemPhysicalObject.gameObject;
                    }

                    //EFM_EquipmentSlot.WearEquipment(customItemWrapper);

                    itemPhysicalObject.gameObject.SetActive(StatusUI.instance.IsOpen());
                }

                // Put item in pocket if it has pocket index
                if (item["pocketSlotIndex"] != null)
                {
                    Mod.LogInfo("Loaded item has pocket index: " + ((int)item["pocketSlotIndex"]));
                    MI.takeCurrentLocation = false;
                    MI.locationIndex = 0;

                    FVRQuickBeltSlot pocketSlot = Mod.pocketSlots[(int)item["pocketSlotIndex"]];
                    itemPhysicalObject.SetQuickBeltSlot(pocketSlot);
                    itemPhysicalObject.SetParentage(null);
                }
            }

            // Place in tradeVolume
            if (item["inTradeVolume"] != null)
            {
                itemObject.transform.parent = transform.GetChild(1).GetChild(24).GetChild(1);
            }
            else if (parent != null && parent.parent != null) // Add to container in case parent is one
            {
                MeatovItem parentCIW = parent.parent.GetComponent<MeatovItem>();
                if (parentCIW != null)
                {
                    parentCIW.AddItemToContainer(itemPhysicalObject);
                }
            }

            // GameObject
            itemObject.transform.localPosition = new Vector3((float)item["PhysicalObject"]["positionX"], (float)item["PhysicalObject"]["positionY"], (float)item["PhysicalObject"]["positionZ"]);
            itemObject.transform.localRotation = Quaternion.Euler(new Vector3((float)item["PhysicalObject"]["rotationX"], (float)item["PhysicalObject"]["rotationY"], (float)item["PhysicalObject"]["rotationZ"]));

            // Ensure item and its contents are all in the correct location index
            MI.UpdateInventories();

            return itemObject;
        }

        private void AddAttachments(FVRPhysicalObject physicalObject, JToken loadedPhysicalObject)
        {
            if (loadedPhysicalObject["AttachmentsList"] == null)
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
                    prefabToUse = Mod.GetItemPrefab(parsedID);
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
            }
        }

        public override void InitUI()
        {
            UpdateLoadButtonList();

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

                        Mod.hydration = 0.3f * Mod.defaultMaxHydration;
                        Mod.energy = 0.3f * Mod.defaultMaxEnergy;

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
                        //raidReportListDownHoverScroll.hoverSound = hoverAudio;
                        raidReportListDownHoverScroll.scrollbar = raidReportScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        raidReportListDownHoverScroll.other = raidReportListUpHoverScroll;
                        raidReportListDownHoverScroll.up = false;
                        raidReportListUpHoverScroll.MaxPointingRange = 30;
                        //raidReportListUpHoverScroll.hoverSound = hoverAudio;
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
                            //pointableButton.hoverSound = hoverAudio;

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
                                    //partElement.transform.GetChild(5).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                                    PointableButton destroyedPartButton = partElement.transform.GetChild(5).gameObject.AddComponent<PointableButton>();
                                    destroyedPartButton.SetButton();
                                    destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 4); });
                                    destroyedPartButton.MaxPointingRange = 20;
                                    //destroyedPartButton.hoverSound = hoverAudio;
                                }
                                else // Not destroyed but damaged
                                {
                                    int hpToHeal = (int)(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]);
                                    int cost = healthPrice * hpToHeal;
                                    fullPartConditions[partIndex][0] = cost;
                                    totalMedicalTreatmentPrice += cost;
                                    partTotalCost += cost;

                                    partElement.transform.GetChild(1).gameObject.SetActive(true);
                                    //partElement.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);
                                    partElement.transform.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "Health (" + hpToHeal + ")";

                                    PointableButton destroyedPartButton = partElement.transform.GetChild(1).gameObject.AddComponent<PointableButton>();
                                    destroyedPartButton.SetButton();
                                    destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, 0); });
                                    destroyedPartButton.MaxPointingRange = 20;
                                    //destroyedPartButton.hoverSound = hoverAudio;
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
                                        //partElement.transform.GetChild(i + 2).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(cost);

                                        PointableButton destroyedPartButton = partElement.transform.GetChild(i + 2).gameObject.AddComponent<PointableButton>();
                                        destroyedPartButton.SetButton();
                                        destroyedPartButton.Button.onClick.AddListener(() => { ToggleMedicalPartCondition(currentPartIndex, i + 1); });
                                        destroyedPartButton.MaxPointingRange = 20;
                                        //destroyedPartButton.hoverSound = hoverAudio;

                                        medicalListHeight += 22;
                                    }
                                }
                            }

                            // Set part total cost
                            //partElement.transform.GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = MarketManager.FormatCompleteMoneyString(partTotalCost);
                        }
                    }
                    Mod.LogInfo("\tProcessed parts");

                    // Setup total and put as last sibling
                    totalTreatmentPriceText = medicalListContent.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>();
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                    medicalListContent.GetChild(1).SetAsLastSibling();

                    // Set total health
                    float totalHealth = 0;
                    foreach (float partHealth in Mod.health)
                    {
                        totalHealth += partHealth;
                    }
                    medicalScreenTotalHealthText = medicalScreen.GetChild(4).GetChild(2).GetChild(1).GetComponent<Text>();
                    medicalScreenTotalHealthText.text = totalHealth.ToString() + "/440";

                    //medicalScreen.GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));

                    Mod.LogInfo("\tSet total health");
                    // Set hover scrolls
                    if (medicalListHeight >= 377)
                    {
                        HoverScroll medicalListDownHoverScroll = medicalScreen.GetChild(3).GetChild(2).gameObject.AddComponent<HoverScroll>();
                        HoverScroll merdicalListUpHoverScroll = medicalScreen.GetChild(3).GetChild(3).gameObject.AddComponent<HoverScroll>();
                        medicalListDownHoverScroll.MaxPointingRange = 30;
                        //medicalListDownHoverScroll.hoverSound = hoverAudio;
                        medicalListDownHoverScroll.scrollbar = medicalScreen.GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                        medicalListDownHoverScroll.other = merdicalListUpHoverScroll;
                        medicalListDownHoverScroll.up = false;
                        merdicalListUpHoverScroll.MaxPointingRange = 30;
                        //merdicalListUpHoverScroll.hoverSound = hoverAudio;
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
            //Transform skillList = StatusUI.instance.transform.GetChild(9).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            //GameObject skillPairPrefab = skillList.GetChild(0).gameObject;
            //GameObject skillPrefab = skillPairPrefab.transform.GetChild(0).gameObject;
            //Transform currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
            //currentSkillPair.gameObject.SetActive(true);
            //StatusUI.instance.skills = new SkillUI[Mod.skills.Length];
            //for (int i = 0; i < Mod.skills.Length; ++i)
            //{
            //    if (currentSkillPair.childCount == 3)
            //    {
            //        currentSkillPair = Instantiate(skillPairPrefab, skillList).transform;
            //        currentSkillPair.gameObject.SetActive(true);
            //    }

            //    SkillUI skillUI = new SkillUI();
            //    GameObject currentSkill = Instantiate(skillPrefab, currentSkillPair);
            //    currentSkill.SetActive(true);
            //    currentSkill.transform.GetChild(0).GetComponent<Image>().sprite = Mod.skillIcons[i];
            //    skillUI.text = currentSkill.transform.GetChild(1).GetChild(0).GetComponent<Text>();
            //    skillUI.text.text = String.Format("{0} lvl. {1:0} ({2:0}/100)", Mod.SkillIndexToName(i), (int)(Mod.skills[i].currentProgress / 100), Mod.skills[i].currentProgress % 100);
            //    skillUI.progressBarRectTransform = currentSkill.transform.GetChild(1).GetChild(1).GetChild(1).GetComponent<RectTransform>();
            //    skillUI.progressBarRectTransform.sizeDelta = new Vector2(Mod.skills[i].currentProgress % 100, 4.73f);
            //    skillUI.diminishingReturns = currentSkill.transform.GetChild(1).GetChild(2).gameObject;
            //    skillUI.increasing = currentSkill.transform.GetChild(1).GetChild(3).gameObject;
            //    StatusUI.instance.skills[i] = skillUI;
            //}
        }

        private void UpdateTreatmentApply()
        {
            //int playerRoubleCount = (inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0);
            //if (playerRoubleCount < totalMedicalTreatmentPrice)
            //{
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = false;
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.gray;
            //}
            //else
            //{
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetComponent<Collider>().enabled = true;
            //    transform.GetChild(0).GetChild(0).GetChild(13).GetChild(2).GetChild(2).GetComponent<Text>().color = Color.white;
            //}
        }

        public void ToggleMedicalPart(int partIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price
                medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                for (int i = 0; i < 5; ++i)
                {
                    if (medicalPartElements[partIndex].transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                    {
                        totalMedicalTreatmentPrice -= fullPartConditions[partIndex][i];
                    }
                }
                //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
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
                //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
            }

            UpdateTreatmentApply();
        }

        public void ToggleMedicalPartCondition(int partIndex, int conditionIndex)
        {
            if (medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
            {
                // Deactivate checkmark and remove from price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(false);
                if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice -= fullPartConditions[partIndex][conditionIndex];
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }
            else
            {
                // Activate checkmark and add to price if necessary
                medicalPartElements[partIndex].transform.GetChild(conditionIndex + 1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                if (medicalPartElements[partIndex].transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    totalMedicalTreatmentPrice += fullPartConditions[partIndex][conditionIndex];
                    //totalTreatmentPriceText.text = MarketManager.FormatCompleteMoneyString(totalMedicalTreatmentPrice);
                }
            }

            UpdateTreatmentApply();
        }

        public void UpdateLoadButtonList()
        {
            // Call a fetch because new saves could have been made
            FetchAvailableSaveFiles();

            // Set buttons activated depending on presence of save files
            if (availableSaveFiles.Count > 0)
            {
                for (int i = 0; i < 6; ++i)
                {
                    loadButtons[i].gameObject.SetActive(availableSaveFiles.Contains(i));
                }
                loadButton.gameObject.SetActive(true);
            }
            else
            {
                loadButton.gameObject.SetActive(false);
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
            int scaledTime = (int)((clampedTime * UIController.meatovTimeMultiplier) % 86400);
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
            SetPage(2);
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
            SetPage(1);
        }

        public void OnLoadSlotClicked(int slotIndex)
        {
            clickAudio.Play();
            ResetPlayerRig();
            LoadHideout(slotIndex);
        }

        public void OnRaidClicked()
        {
            clickAudio.Play();
            SetPage(3);
        }

        public void OnCreditsClicked()
        {
            clickAudio.Play();
            SetPage(10);
        }

        public void OnDonatePanelClicked()
        {
            clickAudio.Play();
            SetPage(11);
        }

        public void OnOptionsClicked()
        {
            clickAudio.Play();
            SetPage(12);
        }

        public void OnBackClicked()
        {
            clickAudio.Play();
            switch (pageIndex)
            {
                case 0:
                    SteamVR_LoadLevel.Begin("MeatovMainMenu", false, 0.5f, 0f, 0f, 0f, 1f);
                    break;
                case 4:
                case 5:
                case 6:
                    SetPage(pageIndex - 1);
                    break;
                default:
                    SetPage(0);
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

                    if (i + 1 == optionPages.Length - 1)
                    {
                        //buttons[12][1].gameObject.SetActive(false);
                    }

                    //buttons[12][2].gameObject.SetActive(true);

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

                    if (i - 1 == 0)
                    {
                        //buttons[12][2].gameObject.SetActive(false);
                    }

                    //buttons[12][1].gameObject.SetActive(true);

                    break;
                }
            }
        }

        public void OnScavBlockOKClicked()
        {
            clickAudio.Play();

            //buttons[3][0].GetComponent<Collider>().enabled = true;
            //buttons[3][1].GetComponent<Collider>().enabled = true;
        }

        public void OnCharClicked(int charIndex)
        {
            clickAudio.Play();

            Mod.chosenCharIndex = charIndex;

            // Update chosen char text
            confirmChosenCharacter.text = charIndex == 0 ? "PMC" : "Scav";
            loadingChosenCharacter.text = confirmChosenCharacter.text;

            bool previousScavItemFound = false;
            Transform scavRaidItemNodeParent = transform.GetChild(1).GetChild(25);
            foreach (Transform child in scavRaidItemNodeParent)
            {
                if (child.childCount > 0)
                {
                    previousScavItemFound = true;
                    break;
                }
            }

            if (previousScavItemFound)
            {
                //buttons[3][0].GetComponent<Collider>().enabled = false;
                //buttons[3][1].GetComponent<Collider>().enabled = false;

                scavBlock.gameObject.SetActive(true);
            }
            else
            {
                SetPage(4);
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
                    confirmChosenMap.text = "Factory";
                    loadingChosenMap.text = "Factory";
                    break;
                default:
                    break;
            }

            SetPage(5);
        }

        public void OnTimeClicked(int timeIndex)
        {
            clickAudio.Play();

            Mod.chosenTimeIndex = timeIndex;
            SetPage(6);
        }

        public void OnConfirmRaidClicked()
        {
            clickAudio.Play();

            SetPage(7);

            // Begin loading raid map
            switch (Mod.chosenMapIndex)
            {
                case 0:
                    loadingRaid = true;
                    Mod.chosenMapName = "Factory";
                    Mod.currentRaidBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/EscapeFromMeatovFactory.ab");
                    break;
                default:
                    loadingRaid = true;
                    Mod.chosenMapIndex = 0;
                    Mod.chosenCharIndex = 0;
                    Mod.chosenTimeIndex = 0;
                    Mod.chosenMapName = "Factory";
                    confirmChosenMap.text = "Factory";
                    loadingChosenMap.text = "Factory";
                    Mod.currentRaidBundleRequest = AssetBundle.LoadFromFileAsync(Mod.path + "/EscapeFromMeatovFactory.ab");
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
            foreach (KeyValuePair<int, GameObject> partElement in medicalPartElements)
            {
                if (partElement.Value.transform.GetChild(0).GetChild(0).GetChild(0).gameObject.activeSelf)
                {
                    for (int i = 0; i < 5; ++i)
                    {
                        if (partElement.Value.transform.GetChild(i + 1).GetChild(0).GetChild(0).gameObject.activeSelf)
                        {
                            if (i == 0) // Health
                            {
                                Mod.health[partElement.Key] = Mod.currentMaxHealth[partElement.Key];

                                // Update display
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(0).GetChild(partElement.Key).GetComponent<Image>().color = Color.white;
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(1).GetComponent<Text>().text = Mod.currentMaxHealth[partElement.Key].ToString() + "/" + Mod.currentMaxHealth[partElement.Key];
                            }
                            else if (i == 1) // LightBleeding
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.LightBleeding, partElement.Key);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(0).gameObject.SetActive(false);
                            }
                            else if (i == 2) // HeavyBleeding
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.HeavyBleeding, partElement.Key);

                                // Update bleed icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(1).gameObject.SetActive(false);
                            }
                            else if (i == 3) // Fracture
                            {
                                Effect.RemoveEffects(false, Effect.EffectType.Fracture, partElement.Key);

                                // Update fracture icon
                                transform.GetChild(0).GetChild(0).GetChild(13).GetChild(4).GetChild(1).GetChild(partElement.Key).GetChild(3).GetChild(2).gameObject.SetActive(false);
                            }
                            else if (i == 4) // DestroyedPart
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
            if (inventoryItems.ContainsKey("203"))
            {
                if (inventoryItems["203"].Count >= totalMedicalTreatmentPrice)
                {
                    amountToRemoveFromBase = totalMedicalTreatmentPrice;
                }
                else
                {
                    amountToRemoveFromBase = inventoryItems["203"].Count;
                    amountToRemoveFromPlayer = totalMedicalTreatmentPrice - inventoryItems["203"].Count;
                }
            }
            else
            {
                amountToRemoveFromPlayer = totalMedicalTreatmentPrice;
            }
            if (amountToRemoveFromBase > 0)
            {
                //inventory["203"] = inventory["203"].Count - amountToRemoveFromBase;
                //List<GameObject> objectList = inventory["203"].Count;
                //for (int i = objectList.Count - 1, j = amountToRemoveFromBase; i >= 0 && j > 0; --i)
                //{
                //    GameObject toCheck = objectList[objectList.Count - 1];
                //    MeatovItem CIW = toCheck.GetComponent<MeatovItem>();
                //    if (CIW.stack > amountToRemoveFromBase)
                //    {
                //        CIW.stack = CIW.stack - amountToRemoveFromBase;
                //        j = 0;
                //    }
                //    else // CIW.stack <= amountToRemoveFromBase
                //    {
                //        j -= CIW.stack;
                //        objectList.RemoveAt(objectList.Count - 1);
                //        CIW.physObj.SetQuickBeltSlot(null);
                //        CIW.destroyed = true;
                //        Destroy(toCheck);
                //    }
                //}
            }
            if (amountToRemoveFromPlayer > 0)
            {
                Mod.playerInventory["203"] = Mod.playerInventory["203"] - amountToRemoveFromPlayer;
                List<MeatovItem> objectList = Mod.playerInventoryItems["203"];
                for (int i = objectList.Count - 1, j = amountToRemoveFromPlayer; i >= 0 && j > 0; --i)
                {
                    MeatovItem toCheck = objectList[objectList.Count - 1];
                    if (toCheck.stack > amountToRemoveFromPlayer)
                    {
                        toCheck.stack = toCheck.stack - amountToRemoveFromPlayer;
                        j = 0;
                    }
                    else // CIW.stack <= amountToRemoveFromBase
                    {
                        j -= toCheck.stack;
                        objectList.RemoveAt(objectList.Count - 1);
                        toCheck.physObj.SetQuickBeltSlot(null);
                        toCheck.physObj.ForceBreakInteraction();
                        toCheck.destroyed = true;
                        Destroy(toCheck);
                        Mod.weight -= toCheck.currentWeight;
                    }
                }
            }

            //foreach (BaseAreaManager baseAreaManager in baseAreaManagers)
            //{
            //    baseAreaManager.UpdateBasedOnItem("203");
            //}

            //transform.GetChild(0).GetChild(0).GetChild(13).GetChild(5).GetChild(0).GetComponent<Text>().text = "Stash: " + MarketManager.FormatCompleteMoneyString((inventory.ContainsKey("203") ? inventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));
        }

        private void ResetPlayerRig()
        {
            // Destroy and reset rig and equipment slots
            EquipmentSlot.Clear();
            for (int i = 0; i < StatusUI.instance.equipmentSlots.Length; ++i)
            {
                if (StatusUI.instance.equipmentSlots[i] != null && StatusUI.instance.equipmentSlots[i].CurObject != null)
                {
                    Destroy(StatusUI.instance.equipmentSlots[i].CurObject.gameObject);
                }
            }
            GM.CurrentPlayerBody.ConfigureQuickbelt(-2); // -2 in order to destroy the objects on belt as well
        }

        private void SaveBase()
        {
            Mod.LogInfo("Saving base");

            // Write time
            loadedData["time"] = GetTimeSeconds();

            // Write player status
            loadedData["health"] = JArray.FromObject(Mod.health);
            loadedData["hydration"] = Mod.hydration;
            loadedData["maxHydration"] = Mod.defaultMaxHydration;
            loadedData["energy"] = Mod.energy;
            loadedData["maxEnergy"] = Mod.defaultMaxEnergy;
            loadedData["stamina"] = Mod.stamina;
            loadedData["maxStamina"] = Mod.maxStamina;
            loadedData["weight"] = Mod.weight;
            loadedData["level"] = Mod.level;
            loadedData["experience"] = Mod.experience;
            loadedData["totalRaidCount"] = Mod.totalRaidCount;
            loadedData["runThroughRaidCount"] = Mod.runThroughRaidCount;
            loadedData["survivedRaidCount"] = Mod.survivedRaidCount;
            loadedData["MIARaidCount"] = Mod.MIARaidCount;
            loadedData["KIARaidCount"] = Mod.KIARaidCount;
            loadedData["failedRaidCount"] = Mod.failedRaidCount;
            //loadedData["fenceRestockTimer"] = TraderStatus.fenceRestockTimer;
            loadedData["scavTimer"] = scavTimer;

            // Write skills
            loadedData["skills"] = new JArray();
            for (int i = 0; i < 64; ++i)
            {
                ((JArray)loadedData["skills"]).Add(new JObject());
                loadedData["skills"][i]["progress"] = Mod.skills[i].progress;
                loadedData["skills"][i]["currentProgress"] = Mod.skills[i].currentProgress;
            }

            // Write areas
            JArray savedAreas = new JArray();
            loadedData["areas"] = savedAreas;
            //for (int i = 0; i < baseAreaManagers.Count; ++i)
            //{
            //    JToken currentSavedArea = new JObject();
            //    currentSavedArea["level"] = baseAreaManagers[i].level;
            //    currentSavedArea["constructing"] = baseAreaManagers[i].constructing;
            //    currentSavedArea["constructTimer"] = baseAreaManagers[i].constructionTimer;
            //    if (baseAreaManagers[i].slotItems != null)
            //    {
            //        JArray slots = new JArray();
            //        currentSavedArea["slots"] = slots;
            //        foreach (GameObject slotItem in baseAreaManagers[i].slotItems)
            //        {
            //            if (slotItem == null)
            //            {
            //                slots.Add(null);
            //            }
            //            else
            //            {
            //                SaveItem(slots, slotItem.transform);
            //            }
            //        }
            //    }
            //    if (baseAreaManagers[i].activeProductions != null)
            //    {
            //        currentSavedArea["productions"] = new JObject();

            //        foreach (KeyValuePair<string, AreaProduction> production in baseAreaManagers[i].activeProductions)
            //        {
            //            JObject currentProduction = new JObject();
            //            currentProduction["timeLeft"] = production.Value.timeLeft;
            //            currentProduction["productionCount"] = production.Value.productionCount;
            //            currentProduction["count"] = production.Value.count;

            //            currentSavedArea["productions"][production.Value.ID] = currentProduction;
            //        }
            //    }
            //    else if (baseAreaManagers[i].activeScavCaseProductions != null)
            //    {
            //        currentSavedArea["productions"] = new JArray();

            //        foreach (EFM_ScavCaseProduction production in baseAreaManagers[i].activeScavCaseProductions)
            //        {
            //            JObject currentProduction = new JObject();
            //            currentProduction["timeLeft"] = production.timeLeft;
            //            currentProduction["products"] = new JObject();
            //            if (production.products.ContainsKey(Mod.ItemRarity.Common))
            //            {
            //                currentProduction["products"]["common"] = new JObject();
            //                currentProduction["products"]["common"]["min"] = production.products[Mod.ItemRarity.Common].x;
            //                currentProduction["products"]["common"]["max"] = production.products[Mod.ItemRarity.Common].y;
            //            }
            //            if (production.products.ContainsKey(Mod.ItemRarity.Rare))
            //            {
            //                currentProduction["products"]["rare"] = new JObject();
            //                currentProduction["products"]["rare"]["min"] = production.products[Mod.ItemRarity.Rare].x;
            //                currentProduction["products"]["rare"]["max"] = production.products[Mod.ItemRarity.Rare].y;
            //            }
            //            if (production.products.ContainsKey(Mod.ItemRarity.Superrare))
            //            {
            //                currentProduction["products"]["superrare"] = new JObject();
            //                currentProduction["products"]["superrare"]["min"] = production.products[Mod.ItemRarity.Superrare].x;
            //                currentProduction["products"]["superrare"]["max"] = production.products[Mod.ItemRarity.Superrare].y;
            //            }

            //            (currentSavedArea["productions"] as JArray).Add(currentProduction);
            //        }
            //    }
            //    savedAreas.Add(currentSavedArea);
            //}

            // Save trader statuses
            JArray savedTraderStatuses = new JArray();
            loadedData["traderStatuses"] = savedTraderStatuses;
            for (int i = 0; i < 8; ++i)
            {
                JToken currentSavedTraderStatus = new JObject();
                //currentSavedTraderStatus["id"] = Mod.traderStatuses[i].id;
                //currentSavedTraderStatus["salesSum"] = Mod.traderStatuses[i].salesSum;
                //currentSavedTraderStatus["standing"] = Mod.traderStatuses[i].standing;
                //currentSavedTraderStatus["unlocked"] = Mod.traderStatuses[i].unlocked;

                // Save tasks
                // TODO: This saves literally all saveable data for tasks their conditions, and the conditions counters
                // We could ommit tasks that dont have any data to save, like the ones that are still locked
                // This makes trader init slower but would save on space. Check if necessary
                currentSavedTraderStatus["tasks"] = new JObject();
                //foreach (TraderTask traderTask in Mod.traderStatuses[i].tasks)
                //{
                //    JObject taskSaveData = new JObject();
                //    currentSavedTraderStatus["tasks"][traderTask.ID] = taskSaveData;
                //    taskSaveData["state"] = traderTask.taskState.ToString();
                //    taskSaveData["conditions"] = new JObject();
                //    foreach (TraderTaskCondition traderTaskCondition in traderTask.completionConditions)
                //    {
                //        JObject conditionSaveData = new JObject();
                //        taskSaveData["conditions"][traderTaskCondition.ID] = conditionSaveData;
                //        conditionSaveData["fulfilled"] = traderTaskCondition.fulfilled;
                //        conditionSaveData["itemCount"] = traderTaskCondition.itemCount;
                //        if (traderTaskCondition.counters != null)
                //        {
                //            conditionSaveData["counters"] = new JObject();
                //            foreach (TraderTaskCounterCondition traderTaskCounterCondition in traderTaskCondition.counters)
                //            {
                //                JObject counterConditionSaveData = new JObject();
                //                conditionSaveData["counters"][traderTaskCounterCondition.ID] = counterConditionSaveData;
                //                counterConditionSaveData["killCount"] = traderTaskCounterCondition.killCount;
                //                counterConditionSaveData["shotCount"] = traderTaskCounterCondition.shotCount;
                //                counterConditionSaveData["completed"] = traderTaskCounterCondition.completed;
                //            }
                //        }
                //    }
                //}

                //currentSavedTraderStatus["itemsToWaitForUnlock"] = JArray.FromObject(Mod.traderStatuses[i].itemsToWaitForUnlock);

                savedTraderStatuses.Add(currentSavedTraderStatus);
            }

            // Reset save data item list
            JArray saveItems = new JArray();
            loadedData["items"] = saveItems;

            // Reset save data item list
            JArray scavSaveItems = new JArray();
            loadedData["scavReturnItems"] = scavSaveItems;

            // Save loose items
            Transform itemsRoot = transform.GetChild(2);
            for (int i = 0; i < itemsRoot.childCount; ++i)
            {
                SaveItem(saveItems, itemsRoot.GetChild(i));
            }

            // Save trade volume items
            //for (int i = 0; i < marketManager.tradeVolume.itemsRoot.childCount; ++i)
            //{
            //    SaveItem(saveItems, marketManager.tradeVolume.itemsRoot.GetChild(i));
            //}

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
            foreach (EquipmentSlot equipSlot in StatusUI.instance.equipmentSlots)
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
            for (int i = 0; i < 15; ++i)
            {
                if (scavReturnNodeParent.GetChild(i).childCount > 0)
                {
                    SaveItem(scavSaveItems, scavReturnNodeParent.GetChild(i).GetChild(0));
                }
                else
                {
                    scavSaveItems.Add(null);
                }
            }

            // Save insuredSets
            //Mod.insuredItems = new List<InsuredSet>();
            //if (Mod.insuredItems != null)
            //{
            //    JArray savedInsuredSets = new JArray();
            //    loadedData["insuredSets"] = savedInsuredSets;

            //    for (int i = 0; i < Mod.insuredItems.Count; ++i)
            //    {
            //        JObject newSavedInsuredSet = new JObject();
            //        newSavedInsuredSet["returnTime"] = Mod.insuredItems[i].returnTime;
            //        newSavedInsuredSet["items"] = JObject.FromObject(Mod.insuredItems[i].items);
            //        savedInsuredSets.Add(newSavedInsuredSet);
            //    }
            //}

            // Save triggered exploration triggers
            JArray savedExperiencetriggers = new JArray();
            loadedData["triggeredExplorationTriggers"] = savedExperiencetriggers;
            for (int i = 0; i < 12; ++i)
            {
                savedExperiencetriggers.Add(JArray.FromObject(Mod.triggeredExplorationTriggers[i]));
            }

            SaveDataToFile();
            Mod.LogInfo("Saved base");
            UpdateLoadButtonList();
        }

        private void SaveItem(JArray listToAddTo, Transform item, FVRViveHand hand = null, int quickBeltSlotIndex = -1)
        {
            if (item == null)
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
            for (int i = 0; i < 4; ++i)
            {
                if (Mod.itemsInPocketSlots[i] != null && Mod.itemsInPocketSlots[i].gameObject.Equals(item.gameObject))
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
            MeatovItem customItemWrapper = itemPhysicalObject.gameObject.GetComponentInChildren<MeatovItem>();
            if (customItemWrapper != null)
            {
                savedItem["itemType"] = (int)customItemWrapper.itemType;
                savedItem["PhysicalObject"]["equipSlot"] = -1;
                savedItem["amount"] = customItemWrapper.amount;
                savedItem["looted"] = customItemWrapper.looted;
                savedItem["insured"] = customItemWrapper.insured;
                savedItem["foundInRaid"] = customItemWrapper.foundInRaid;

                // Armor
                if (customItemWrapper.itemType == MeatovItem.ItemType.BodyArmor)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.Rig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentRig != null && EquipmentSlot.currentRig.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 6;
                    }
                    if (savedItem["PhysicalObject"]["quickBeltSlotContents"] == null)
                    {
                        savedItem["PhysicalObject"]["quickBeltSlotContents"] = new JArray();
                    }
                    JArray saveQBContents = (JArray)savedItem["PhysicalObject"]["quickBeltSlotContents"];
                    for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if (customItemWrapper.itemsInSlots[i] == null)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.ArmoredRig)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentArmor != null && EquipmentSlot.currentArmor.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 1;
                    }
                    if (savedItem["PhysicalObject"]["quickBeltSlotContents"] == null)
                    {
                        savedItem["PhysicalObject"]["quickBeltSlotContents"] = new JArray();
                    }
                    JArray saveQBContents = (JArray)savedItem["PhysicalObject"]["quickBeltSlotContents"];
                    for (int i = 0; i < customItemWrapper.itemsInSlots.Length; ++i)
                    {
                        if (customItemWrapper.itemsInSlots[i] == null)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.Backpack)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentBackpack != null && EquipmentSlot.currentBackpack.Equals(customItemWrapper))
                    {
                        savedItem["PhysicalObject"]["equipSlot"] = 0;
                    }
                    if (savedItem["PhysicalObject"]["backpackContents"] == null)
                    {
                        savedItem["PhysicalObject"]["backpackContents"] = new JArray();
                    }
                    JArray saveBPContents = (JArray)savedItem["PhysicalObject"]["backpackContents"];
                    for (int i = 0; i < customItemWrapper.containerItemRoot.childCount; ++i)
                    {
                        Mod.LogInfo("Item in backpack " + i + ": " + customItemWrapper.containerItemRoot.GetChild(i).name);
                        SaveItem(saveBPContents, customItemWrapper.containerItemRoot.GetChild(i));
                    }
                }

                // Container
                if (customItemWrapper.itemType == MeatovItem.ItemType.Container)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.Pouch)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.Helmet)
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
                if (customItemWrapper.itemType == MeatovItem.ItemType.Earpiece)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentEarpiece != null && EquipmentSlot.currentEarpiece.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 2;
                    }
                }

                // FaceCover
                if (customItemWrapper.itemType == MeatovItem.ItemType.FaceCover)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentFaceCover != null && EquipmentSlot.currentFaceCover.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 4;
                    }
                }

                // Eyewear
                if (customItemWrapper.itemType == MeatovItem.ItemType.Eyewear)
                {
                    // If this is an equipment piece we are currently wearing
                    if (EquipmentSlot.currentEyewear != null && EquipmentSlot.currentEyewear.Equals(customItemWrapper))
                    {
                        // Find its equip slot index
                        savedItem["PhysicalObject"]["equipSlot"] = 5;
                    }
                }

                // AmmoBox
                //if (customItemWrapper.itemType == MeatovItem.ItemType.AmmoBox)
                //{
                //    Mod.LogInfo("Item is ammo box");
                //}

                // Money
                if (customItemWrapper.itemType == MeatovItem.ItemType.Money)
                {
                    savedItem["stack"] = customItemWrapper.stack;
                }

                // Consumable
                //if (customItemWrapper.itemType == MeatovItem.ItemType.Consumable)
                //{
                //    Mod.LogInfo("Item is Consumable");
                //}

                // Key
                //if (customItemWrapper.itemType == MeatovItem.ItemType.Key)
                //{
                //    Mod.LogInfo("is Key");
                //}

                // Dogtag
                if (customItemWrapper.itemType == MeatovItem.ItemType.DogTag)
                {
                    savedItem["dogtagName"] = customItemWrapper.dogtagName;
                    savedItem["dogtagLevel"] = customItemWrapper.dogtagLevel;
                }
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
            File.WriteAllText(Mod.path + "/EscapeFromMeatov/" + (Mod.saveSlotIndex == 5 ? "AutoSave" : "Slot" + Mod.saveSlotIndex) + ".sav", loadedData.ToString());
        }

        public void OnHideoutInventoryChangedInvoke()
        {
            // Raise event
            if (OnHideoutInventoryChanged != null)
            {
                OnHideoutInventoryChanged();
            }
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
            if (timer <= 0)
            {
                gameObject.SetActive(false);
                Destroy(this);
            }
        }
    }
}
