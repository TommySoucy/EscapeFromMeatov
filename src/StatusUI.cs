using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class StatusUI : MonoBehaviour
    {
        public static StatusUI instance;

        public static float damagePerMeter = 9;
        public static float safeHeight = 3;
        public Vector3 previousVelocity;
        public bool wasGrounded;

        public GameObject canvas;
        public GameObject slotsParent;
        public EquipmentSlot[] equipmentSlots; // Backpack, BodyArmor, EarPiece, HeadWear, FaceCover, EyeWear, Rig, Pouch
        public Sprite[] levelSprites;
        public Image levelIcon;
        public Text level;
        public RectTransform expBarFill;
        public Text experienceText;
        public Text[] partLabels;
        public Image[] partImages; // Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg
        public GameObject[] effectIcons; // Contusion, Toxin, Painkiller, Tremor, TunnelVision, Dehydration, Encumbered, Overencumbered, Fatigue, OverweightFatigue
        public Text[] partHealth;  // Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg
        public PartUI[] partEffects;
        public Text health;
        public Text healthDelta;
        public Text hydration;
        public Text hydrationDelta;
        public Text energy;
        public Text energyDelta;
        public Text weight;
        public GameObject extractionsParent;
        public GameObject extractionCardPrefab;
        public GameObject notificationParent;
        public GameObject notificationPrefab;
        public GameObject tasksClosedIcon;
        public GameObject tasksOpenIcon;
        public GameObject tasksView;
        public RectTransform tasksParent;
        public GameObject taskPrefab;
        public GameObject infoClosedIcon;
        public GameObject infoOpenIcon;
        public GameObject infoView;
        public RectTransform skillsParent;
        public GameObject skillPairPrefab;
        public GameObject skillPrefab;
        public Text infoKills;
        public Text infoDeaths;
        public Text infoRaids;
        public Text infoSurvived;
        public Text infoRunthrough;
        public Text infoMIA;
        public Text infoKIA;
        public HoverScrollProcessor tasksHoverScrollProcessor;
        public HoverScroll infoDownHoverScroll;
        public HoverScroll infoUpHoverScroll;
        public AudioSource clickAudio;
        public AudioSource notificationAudio;
        public AudioSource openAudio;
        public AudioSource closeAudio;
        public Text extractionTimer;

        public void Awake()
        {
            // Set static instance
            instance = this;

            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;
            Mod.OnPartHealthChanged += OnPartHealthChanged;
            Mod.OnPartCurrentMaxHealthChanged += OnPartHealthChanged;
            Mod.OnPlayerWeightChanged += OnPlayerWeightChanged;
            Mod.OnPlayerExperienceChanged += OnPlayerExperienceChanged;
            Mod.OnHydrationChanged += OnHydrationChanged;
            Mod.OnEnergyChanged += OnEnergyChanged;
            Mod.OnHydrationRateChanged += OnHydrationRateChanged;
            Mod.OnEnergyRateChanged += OnEnergyRateChanged;
            Mod.OnCurrentHealthRateChanged += OnCurrentHealthRateChanged;
            Mod.OnKillCountChanged += OnKillCountChanged;
            Mod.OnDeathCountChanged += OnDeathCountChanged;
            Mod.OnRaidCountChanged += OnRaidCountChanged;
            Mod.OnSurvivedRaidCountChanged += OnSurvivedRaidCountChanged;
            Mod.OnRunthroughRaidCountChanged += OnRunthroughRaidCountChanged;
            Mod.OnMIARaidCountChanged += OnMIARaidCountChanged;
            Mod.OnKIARaidCountChanged += OnKIARaidCountChanged;
            Effect.OnEffectActivated += OnEffectActivated;
            Effect.OnEffectDeactivated += OnEffectDeactivated;
            Effect.OnInactiveTimerAdded += OnInactiveTimerAdded;
            Effect.OnInactiveTimerRemoved += OnInactiveTimerRemoved;

            // Initialize UI
            Init();

            // Ensure disabled by default
            Close();
        }

        public void Init()
        {
            OnPlayerLevelChanged();
            OnPlayerExperienceChanged();
            OnPartHealthChanged(-1);
            OnPlayerWeightChanged();
            OnHydrationChanged();
            OnEnergyChanged();
            OnHydrationRateChanged();
            OnEnergyRateChanged();
            for (int i = 0; i < Mod.GetHealthCount(); ++i) 
            {
                OnCurrentHealthRateChanged(i);
            }
            OnKillCountChanged();
            OnDeathCountChanged();
            OnRaidCountChanged();
            OnSurvivedRaidCountChanged();
            OnRunthroughRaidCountChanged();
            OnMIARaidCountChanged();
            OnKIARaidCountChanged();

            foreach (KeyValuePair<int, List<Task>> traderTasksEntry in Mod.tasksByTraderIndex)
            {
                for(int i=0; i < traderTasksEntry.Value.Count; ++i)
                {
                    Task task = traderTasksEntry.Value[i];
                    if (task.playerUI == null
                        && (task.taskState == Task.TaskState.Available
                        || task.taskState == Task.TaskState.Active
                        || task.taskState == Task.TaskState.Complete))
                    {
                        AddTask(task);
                    }
                }
            }

            Transform currentPair = null;
            for(int i =0; i < Mod.skills.Length; ++i)
            {
                if (currentPair == null || currentPair.childCount == 3)
                {
                    currentPair = Instantiate(skillPairPrefab, skillsParent).transform;
                    currentPair.gameObject.SetActive(true);
                }

                SkillUI skillUI = Instantiate(skillPrefab, currentPair).GetComponent<SkillUI>();
                skillUI.SetSkill(Mod.skills[i]);
                skillUI.gameObject.SetActive(true);
            }

            // Set based on effects
            foreach(KeyValuePair<Effect.EffectType, List<Effect>> effectTypeEntry in Effect.effectsByType)
            {
                for(int i=0; i < effectTypeEntry.Value.Count; ++i)
                {
                    if (effectTypeEntry.Value[i].active)
                    {
                        OnEffectActivated(effectTypeEntry.Value[i]);
                    }
                }
            }
            effectIcons[2].SetActive(Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.Pain));
            if(Mod.weight > Mod.currentWeightLimit)
            {
                effectIcons[6].SetActive(false);
                effectIcons[7].SetActive(true);
            }
            else if(Mod.weight > Mod.currentWeightLimit / 2)
            {
                effectIcons[6].SetActive(true);
                effectIcons[7].SetActive(false);
            }
            else
            {
                effectIcons[6].SetActive(false);
                effectIcons[7].SetActive(false);
            }
        }

        public void Update()
        {
            if (IsOpen() && Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 5)
            {
                OnCloseClicked();
            }

            UpdateStamina();
            UpdateFall();
            UpdateMovement();

            previousVelocity = GM.CurrentMovementManager.m_smoothLocoVelocity;
            wasGrounded = GM.CurrentMovementManager.m_isGrounded;
        }

        public void AddTask(Task task)
        {
            // Instantiate task element
            GameObject currentTaskElement = Instantiate(taskPrefab, tasksParent);
            currentTaskElement.SetActive(true);
            task.playerUI = currentTaskElement.GetComponent<TaskUI>();

            // Set task UI
            task.playerUI.SetTask(task, false);
        }

        public void OnPlayerLevelChanged()
        {
            levelIcon.sprite = levelSprites[Mod.level / 5];
            level.text = Mod.level.ToString();
        }

        public void OnPlayerExperienceChanged()
        {
            experienceText.text = Mod.experience.ToString() + "/" + (Mod.level >= Mod.XPPerLevel.Length ? "INFINITY" : Mod.XPPerLevel[Mod.level].ToString());
            expBarFill.sizeDelta = new Vector2(Mod.level >= Mod.XPPerLevel.Length ? 0 : Mod.level / (float)Mod.XPPerLevel[Mod.level] * 100f, 4.73f);
        }

        public void OnHydrationChanged()
        {
            hydration.text = ((int)Mod.hydration).ToString() +"/"+(int)Mod.currentMaxHydration;
            if(Mod.hydration == Mod.currentMaxHydration)
            {
                if (hydrationDelta.gameObject.activeSelf)
                {
                    hydrationDelta.gameObject.SetActive(false);
                }
            }
            else if(Mod.currentHydrationRate != 0)
            {
                if (!hydrationDelta.gameObject.activeSelf)
                {
                    hydrationDelta.gameObject.SetActive(true);
                }
            }
        }

        public void OnEnergyChanged()
        {
            energy.text = ((int)Mod.energy).ToString() +"/"+(int)Mod.currentMaxEnergy;
            if (Mod.energy == Mod.currentMaxEnergy)
            {
                if (energyDelta.gameObject.activeSelf)
                {
                    energyDelta.gameObject.SetActive(false);
                }
            }
            else
            {
                if (!energyDelta.gameObject.activeSelf)
                {
                    energyDelta.gameObject.SetActive(true);
                }
            }
        }

        public void OnHydrationRateChanged()
        {
            if(Mod.currentHydrationRate == 0 || Mod.hydration == Mod.currentMaxHydration)
            {
                if (hydrationDelta.gameObject.activeSelf)
                {
                    hydrationDelta.gameObject.SetActive(false);
                }
            }
            else
            {
                hydrationDelta.gameObject.SetActive(true);
                hydrationDelta.text = String.Format((Mod.currentHydrationRate < 0 ? "" : "+") + "{0:0.##}/min", Mod.currentHydrationRate * 60);
            }
        }

        public void OnEnergyRateChanged()
        {
            if (Mod.currentEnergyRate == 0 || Mod.energy == Mod.currentMaxEnergy)
            {
                if (energyDelta.gameObject.activeSelf)
                {
                    energyDelta.gameObject.SetActive(false);
                }
            }
            else
            {
                energyDelta.gameObject.SetActive(true);
                energyDelta.text = String.Format((Mod.currentEnergyRate < 0 ? "" : "+") + "{0:0.##}/min", Mod.currentEnergyRate * 60);
            }
        }

        public void OnCurrentHealthRateChanged(int index)
        {
            float total = 0;
            float totalHealth = 0;
            float totalMax = 0;
            for(int i=0; i < Mod.GetHealthCount(); ++i)
            {
                total += Mod.GetCurrentHealthRate(i);
                total += Mod.GetCurrentNonLethalHealthRate(i);

                totalHealth += Mod.GetHealth(i);
                totalMax += Mod.GetCurrentMaxHealth(i);
            }

            if(total == 0 || totalHealth == totalMax)
            {
                healthDelta.gameObject.SetActive(false);
            }
            else
            {
                healthDelta.gameObject.SetActive(true);
                healthDelta.text = String.Format((total < 0 ? "" : "+") + "{0:0.##}/min", total * 60);
            }
        }

        public void OnPartHealthChanged(int index)
        {
            if(Mod.GetCurrentMaxHealthArray() == null || Mod.GetHealthArray() == null)
            {
                return;
            }

            if (index == -1)
            {
                for(int i=0; i < Mod.GetHealthCount(); ++i)
                {
                    partImages[i].color = Color.Lerp(Color.red, Color.white, Mod.GetHealth(i) / Mod.GetCurrentMaxHealth(i));
                    partHealth[i].text = ((int)Mod.GetHealth(i)).ToString() + "/" + ((int)Mod.GetCurrentMaxHealth(i));
                }
            }
            else
            {
                partImages[index].color = Color.Lerp(Color.red, Color.white, Mod.GetHealth(index) / Mod.GetCurrentMaxHealth(index));
                partHealth[index].text = ((int)Mod.GetHealth(index)).ToString() + "/" + ((int)Mod.GetCurrentMaxHealth(index));
            }

            float total = 0;
            float totalMax = 0;
            for(int i=0; i < Mod.GetHealthCount(); ++i)
            {
                total += Mod.GetHealth(i);
                totalMax += Mod.GetCurrentMaxHealth(i);
            }
            health.text = ((int)total).ToString() + "/" + ((int)totalMax);

            if(total == totalMax)
            {
                healthDelta.gameObject.SetActive(false);
            }
        }

        public void OnPlayerWeightChanged()
        {
            weight.text = String.Format("{0:0.#}", Mod.weight / 1000.0f) + "/ " + String.Format("{0:0.#}", Mod.currentWeightLimit / 1000.0f);
            if (Mod.weight > Mod.currentWeightLimit)
            {
                weight.color = Color.red;

                // Enable hard overweight icon, disable overweight icon
                effectIcons[6].SetActive(false);
                effectIcons[7].SetActive(true);
            }
            else if (Mod.weight > Mod.currentWeightLimit / 2)
            {
                weight.color = Color.yellow;

                // Enable overweight icon, disable hard overweight icon
                effectIcons[6].SetActive(true);
                effectIcons[7].SetActive(false);
            }
            else
            {
                weight.color = Color.white;

                // Disable overweight icons
                effectIcons[6].SetActive(false);
                effectIcons[7].SetActive(false);
            }
        }

        public void OnKillCountChanged()
        {
            infoKills.text = "Kills: " + Mod.totalKillCount;
        }

        public void OnDeathCountChanged()
        {
            infoDeaths.text = "Deaths: " + Mod.totalDeathCount;
        }

        public void OnRaidCountChanged()
        {
            infoRaids.text = "Raids: " + Mod.totalRaidCount;
        }

        public void OnSurvivedRaidCountChanged()
        {
            infoSurvived.text = "Survived: " + Mod.survivedRaidCount;
        }

        public void OnRunthroughRaidCountChanged()
        {
            infoRunthrough.text = "Runthrough: " + Mod.runthroughRaidCount;
        }

        public void OnMIARaidCountChanged()
        {
            infoMIA.text = "MIA: " + Mod.MIARaidCount;
        }

        public void OnKIARaidCountChanged()
        {
            infoKIA.text = "KIA: " + Mod.KIARaidCount;
        }

        public void OnEffectActivated(Effect effect)
        {
            switch (effect.effectType)
            {
                case Effect.EffectType.Pain:
                    if(effect.partIndex == -1)
                    {
                        partEffects[0].AddPain();
                    }
                    else
                    {
                        partEffects[effect.partIndex].AddPain();
                    }
                    break;
                case Effect.EffectType.LightBleeding:
                    if(effect.partIndex == -1)
                    {
                        partEffects[0].AddLightBleed();
                    }
                    else
                    {
                        partEffects[effect.partIndex].AddLightBleed();
                    }
                    break;
                case Effect.EffectType.HeavyBleeding:
                    if(effect.partIndex == -1)
                    {
                        partEffects[0].AddHeavyBleed();
                    }
                    else
                    {
                        partEffects[effect.partIndex].AddHeavyBleed();
                    }
                    break;
                case Effect.EffectType.Fracture:
                    partEffects[effect.partIndex].AddFracture();
                    break;
                case Effect.EffectType.Contusion:
                    effectIcons[0].SetActive(true);
                    break;
                case Effect.EffectType.UnknownToxin:
                    effectIcons[1].SetActive(true);
                    break;
                case Effect.EffectType.HandsTremor:
                    effectIcons[3].SetActive(true);
                    break;
                case Effect.EffectType.QuantumTunnelling:
                    effectIcons[4].SetActive(true);
                    break;
                case Effect.EffectType.Dehydration:
                    effectIcons[5].SetActive(true);
                    break;
                case Effect.EffectType.Fatigue:
                case Effect.EffectType.HeavyFatigue:
                    effectIcons[8].SetActive(true);
                    break;
                case Effect.EffectType.OverweightFatigue:
                    effectIcons[9].SetActive(true);
                    break;
            }
        }

        public void OnEffectDeactivated(Effect effect)
        {
            switch (effect.effectType)
            {
                case Effect.EffectType.Pain:
                    if (effect.partIndex == -1)
                    {
                        partEffects[0].RemovePain();
                    }
                    else
                    {
                        partEffects[effect.partIndex].RemovePain();
                    }
                    break;
                case Effect.EffectType.LightBleeding:
                    if (effect.partIndex == -1)
                    {
                        partEffects[0].RemoveLightBleed();
                    }
                    else
                    {
                        partEffects[effect.partIndex].RemoveLightBleed();
                    }
                    break;
                case Effect.EffectType.HeavyBleeding:
                    if (effect.partIndex == -1)
                    {
                        partEffects[0].RemoveHeavyBleed();
                    }
                    else
                    {
                        partEffects[effect.partIndex].RemoveHeavyBleed();
                    }
                    break;
                case Effect.EffectType.Fracture:
                    partEffects[effect.partIndex].RemoveFracture();
                    break;
                case Effect.EffectType.Contusion:
                    bool foundContusion = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherContusions))
                    {
                        for(int i=0; i < otherContusions.Count; ++i)
                        {
                            if (otherContusions[i].active)
                            {
                                foundContusion = true;
                                break;
                            }
                        }
                    }
                    if (!foundContusion)
                    {
                        effectIcons[0].SetActive(false);
                    }
                    break;
                case Effect.EffectType.UnknownToxin:
                    bool foundToxin = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherToxins))
                    {
                        for (int i = 0; i < otherToxins.Count; ++i)
                        {
                            if (otherToxins[i].active)
                            {
                                foundToxin = true;
                                break;
                            }
                        }
                    }
                    if (!foundToxin)
                    {
                        effectIcons[1].SetActive(false);
                    }
                    break;
                case Effect.EffectType.HandsTremor:
                    bool foundTremors = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherTremors))
                    {
                        for (int i = 0; i < otherTremors.Count; ++i)
                        {
                            if (otherTremors[i].active)
                            {
                                foundTremors = true;
                                break;
                            }
                        }
                    }
                    if (!foundTremors)
                    {
                        effectIcons[3].SetActive(false);
                    }
                    break;
                case Effect.EffectType.QuantumTunnelling:
                    bool foundTunnel = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherTunnels))
                    {
                        for (int i = 0; i < otherTunnels.Count; ++i)
                        {
                            if (otherTunnels[i].active)
                            {
                                foundTunnel = true;
                                break;
                            }
                        }
                    }
                    if (!foundTunnel)
                    {
                        effectIcons[4].SetActive(false);
                    }
                    break;
                case Effect.EffectType.Dehydration:
                    bool foundDehydration = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherDehydration))
                    {
                        for (int i = 0; i < otherDehydration.Count; ++i)
                        {
                            if (otherDehydration[i].active)
                            {
                                foundDehydration = true;
                                break;
                            }
                        }
                    }
                    if (!foundDehydration)
                    {
                        effectIcons[5].SetActive(false);
                    }
                    break;
                case Effect.EffectType.Fatigue:
                case Effect.EffectType.HeavyFatigue:
                    bool foundFatigue = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherFatigues))
                    {
                        for (int i = 0; i < otherFatigues.Count; ++i)
                        {
                            if (otherFatigues[i].active)
                            {
                                foundFatigue = true;
                                break;
                            }
                        }
                    }
                    if (!foundFatigue)
                    {
                        effectIcons[8].SetActive(false);
                    }
                    break;
                case Effect.EffectType.OverweightFatigue:
                    bool foundOverweight = false;
                    if (Effect.effectsByType.TryGetValue(effect.effectType, out List<Effect> otherOverweights))
                    {
                        for (int i = 0; i < otherOverweights.Count; ++i)
                        {
                            if (otherOverweights[i].active)
                            {
                                foundOverweight = true;
                                break;
                            }
                        }
                    }
                    if (!foundOverweight)
                    {
                        effectIcons[9].SetActive(false);
                    }
                    break;
            }
        }

        public void OnInactiveTimerAdded(Effect.EffectType effectType, float time)
        {
            switch (effectType)
            {
                case Effect.EffectType.Pain:
                    effectIcons[2].SetActive(true);
                    break;
            }
        }

        public void OnInactiveTimerRemoved(Effect.EffectType effectType)
        {
            switch (effectType)
            {
                case Effect.EffectType.Pain:
                    effectIcons[2].SetActive(Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.Pain));
                    break;
            }
        }

        public void SetExtractionLimitTimer(float raidTimeLeft)
        {
            extractionTimer.text = Mod.FormatTimeString(raidTimeLeft);
        }

        public void UpdateMovement()
        {
            Vector3 sideMovement = GM.CurrentMovementManager.m_smoothLocoVelocity * Time.deltaTime;
            sideMovement.y = 0;

            if (GM.CurrentMovementManager.m_sprintingEngaged)
            {
                Mod.distanceTravelledSprinting += sideMovement.magnitude;
            }
            else if (sideMovement.magnitude > 0)
            {
                Mod.distanceTravelledWalking += sideMovement.magnitude;
            }
        }

        public void UpdateFall()
        {
            if (Mod.currentLocationIndex == 2 && GM.CurrentMovementManager.m_isGrounded && !wasGrounded)
            {
                // Considering realistic 1g of acceleration, t = (Vf-Vi)/a, and s = Vi * t + 0.5 * a * t ^ 2, s being distance fallen
                float t = previousVelocity.y / -9.806f; // Note that here, velocity and a are negative, giving a positive time
                float s = 4.903f * t * t; // Here a is positive to have a positive distance fallen
                if (s > safeHeight)
                {
                    float damage = s * damagePerMeter;
                    float distribution = UnityEngine.Random.value;
                    if (UnityEngine.Random.value < 0.125f * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, 5, -1, true);
                    }
                    if (UnityEngine.Random.value < 0.125f * (s - safeHeight)) // 100% chance of fracture 8+ meters fall above safe height
                    {
                        new Effect(Effect.EffectType.Fracture, 0, 0, 0, null, false, 6, -1, true);
                    }

                    DamagePatch.RegisterPlayerHit(5, distribution * damage, true);
                    DamagePatch.RegisterPlayerHit(6, (1 - distribution) * damage, true);
                }
            }
        }

        public void UpdateStamina()
        {
            Vector3 movementVector = GM.CurrentMovementManager.m_smoothLocoVelocity;
            Vector2 movementVector2 = new Vector2(movementVector.x, movementVector.z);

            float weightFraction = ((float)Mod.weight) / Mod.currentWeightLimit;
            if(weightFraction < 0.5)
            {
                // No regen while sprinting

                if ((GM.CurrentMovementManager.m_sprintingEngaged || movementVector2.magnitude > 3.6f) && GM.CurrentMovementManager.m_isGrounded) // Sprinting
                {
                    // Reset stamina timer to prevent stamina regen
                    Mod.staminaTimer = 1.5f;

                    // Drain stamina
                    float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime;
                    Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }

                if (Mod.staminaTimer > 0)
                {
                    Mod.staminaTimer -= Time.deltaTime;
                }
                else if (Mod.stamina <= Mod.currentMaxStamina)
                {
                    Mod.stamina = Mathf.Min(Mod.stamina + Mod.currentStaminaRate * Time.deltaTime, Mod.currentMaxStamina);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }
            }
            else if (weightFraction < 0.75)
            {
                // Encumbered, 50% stamina regen, no regen while moving, double stamina drain while sprinting

                if ((GM.CurrentMovementManager.m_sprintingEngaged || movementVector2.magnitude > 3.6f) && GM.CurrentMovementManager.m_isGrounded) // Sprinting
                {
                    // Reset stamina timer to prevent stamina regen
                    Mod.staminaTimer = 1.5f;

                    // Drain stamina
                    float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime * 2;
                    Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }
                else if (movementVector2.magnitude > 0 && GM.CurrentMovementManager.m_isGrounded) // Moving
                {
                    // Reset stamina timer to prevent stamina regen
                    Mod.staminaTimer = 1.5f;
                }
                
                // If reach 0 stamina due to being overweight, activate overweight fatigue effect
                if (Mod.stamina == 0)
                {
                    if (Effect.overweightFatigue == null)
                    {
                        new Effect(Effect.EffectType.OverweightFatigue, 0, 300, 0);
                    }
                    else
                    {
                        Effect.overweightFatigue.timer = 300;
                    }
                }

                if (Mod.staminaTimer > 0)
                {
                    Mod.staminaTimer -= Time.deltaTime;
                }
                else if (Mod.stamina <= Mod.currentMaxStamina)
                {
                    Mod.stamina = Mathf.Min(Mod.stamina + Mod.currentStaminaRate * Time.deltaTime * 0.5f, Mod.currentMaxStamina);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }
            }
            else // Weight >=75% max weight. If >=100%, movement will be prevented by patch
            {
                // No stamina regen, stamina drain while walking

                // Reset stamina timer to prevent stamina regen
                Mod.staminaTimer = 1.5f;

                if ((GM.CurrentMovementManager.m_sprintingEngaged || movementVector2.magnitude > 3.6f) && GM.CurrentMovementManager.m_isGrounded) // Sprinting
                {
                    // Drain stamina
                    float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime * 2;
                    Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }
                else if (movementVector2.magnitude > 0 && GM.CurrentMovementManager.m_isGrounded) // Moving
                {
                    // Drain stamina
                    float currentStaminaDrain = Mod.overweightStaminaDrain * Time.deltaTime;
                    Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina / Mod.currentMaxStamina * 100);
                }
            }
        }

        public void AddNotification(string text)
        {
            notificationAudio.Play();
            GameObject notification = Instantiate(notificationPrefab, notificationParent.transform);
            notification.SetActive(true);
            notification.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = text;
            notification.AddComponent<DestroyTimer>().timer = 5;
            if (notificationParent.transform.childCount > 10)
            {
                GameObject firstChild = notificationParent.transform.GetChild(1).gameObject;
                firstChild.SetActive(false);
                Destroy(firstChild);
            }
        }

        public void Toggle()
        {
            if (IsOpen())
            {
                OnCloseClicked();
            }
            else
            {
                openAudio.Play();
                Open();
            }
        }

        public bool IsOpen()
        {
            return canvas.activeSelf;
        }

        public void Close()
        {
            // Equipment items do not necessarily get parented to the slot, so we need to toggle them manually
            // Equipment items will get parented to the slot when putting them in manually, but not when loaded
            for(int i=0; i < equipmentSlots.Length; ++i)
            {
                if (equipmentSlots[i].CurObject != null)
                {
                    equipmentSlots[i].CurObject.gameObject.SetActive(false);
                }
            }
            canvas.SetActive(false);
            slotsParent.SetActive(false);
        }

        public void Open()
        {
            // Equipment items do not necessarily get parented to the slot, so we need to toggle them manually
            // Equipment items will get parented to the slot when putting them in manually, but not when loaded
            for (int i = 0; i < equipmentSlots.Length; ++i)
            {
                if (equipmentSlots[i].CurObject != null)
                {
                    equipmentSlots[i].CurObject.gameObject.SetActive(true);
                }
            }
            canvas.SetActive(true);
            slotsParent.SetActive(true);
        }

        public void OnCloseClicked()
        {
            closeAudio.Play();
            Close();
        }

        public void OnTaskClicked()
        {
            tasksView.SetActive(!tasksView.activeSelf);
            tasksClosedIcon.SetActive(!tasksView.activeSelf);
            tasksOpenIcon.SetActive(tasksView.activeSelf);
        }

        public void OnInfoClicked()
        {
            infoView.SetActive(!infoView.activeSelf);
            infoClosedIcon.SetActive(!infoView.activeSelf);
            infoOpenIcon.SetActive(infoView.activeSelf);
        }

        public void OnDestroy()
        {
            Mod.OnPlayerLevelChanged -= OnPlayerLevelChanged;
            Mod.OnPartHealthChanged -= OnPartHealthChanged;
            Mod.OnPartCurrentMaxHealthChanged -= OnPartHealthChanged;
            Mod.OnPlayerWeightChanged -= OnPlayerWeightChanged;
            Mod.OnPlayerExperienceChanged -= OnPlayerExperienceChanged;
            Mod.OnHydrationChanged -= OnHydrationChanged;
            Mod.OnEnergyChanged -= OnEnergyChanged;
            Mod.OnHydrationRateChanged -= OnHydrationRateChanged;
            Mod.OnEnergyRateChanged -= OnEnergyRateChanged;
            Mod.OnCurrentHealthRateChanged -= OnCurrentHealthRateChanged;
            Mod.OnKillCountChanged -= OnKillCountChanged;
            Mod.OnDeathCountChanged -= OnDeathCountChanged;
            Mod.OnRaidCountChanged -= OnRaidCountChanged;
            Mod.OnSurvivedRaidCountChanged -= OnSurvivedRaidCountChanged;
            Mod.OnRunthroughRaidCountChanged -= OnRunthroughRaidCountChanged;
            Mod.OnMIARaidCountChanged -= OnMIARaidCountChanged;
            Mod.OnKIARaidCountChanged -= OnKIARaidCountChanged;
            Effect.OnEffectActivated -= OnEffectActivated;
            Effect.OnEffectDeactivated -= OnEffectDeactivated;
            Effect.OnInactiveTimerAdded -= OnInactiveTimerAdded;
            Effect.OnInactiveTimerRemoved -= OnInactiveTimerRemoved;
        }
    }
}
