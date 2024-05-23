﻿using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class StatusUI : MonoBehaviour
    {
        public static StatusUI instance;
        public GameObject canvas;
        public GameObject slotsParent;
        public EquipmentSlot[] equipmentSlots; // Backpack, BodyArmor, EarPiece, HeadWear, FaceCover, EyeWear, Rig, Pouch
        public Sprite[] levelSprites;
        public Image levelIcon;
        public Text level;
        public RectTransform expBarFill;
        public Text experienceText;
        public Image[] partImages; // Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg
        public GameObject[] effectIcons; // Contusion, Toxin, Painkiller, Tremor, TunnelVision, Dehydration, Encumbered, Overencumbered, Fatigue, OverweightFatigue
        public Text[] partHealth;  // Head, Thorax, Stomach, LeftArm, RightArm, LeftLeg, RightLeg
        public PartEffects[] partEffects;  // Light bleed, Heavy bleed, Pain, Fracture
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
        public HoverScroll infoDownHoverScroll;
        public HoverScroll infoUpHoverScroll;
        public AudioSource clickAudio;
        public AudioSource notificationAudio;
        public AudioSource openAudio;
        public AudioSource closeAudio;
        public Text extractionTimer;
        public SkillUI[] skillUIs;

        [NonSerialized]
        public int mustUpdateTaskListHeight;

        public void Awake()
        {
            // Set static instance
            instance = this;

            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;
            Mod.OnPartHealthChanged += OnPartHealthChanged;
            Mod.OnPlayerWeightChanged += OnPlayerWeightChanged;
            Mod.OnPlayerExperienceChanged += OnPlayerExperienceChanged;
            Mod.OnHydrationChanged += OnHydrationChanged;
            Mod.OnEnergyChanged += OnEnergyChanged;
            Mod.OnHydrationRateChanged += OnHydrationRateChanged;
            Mod.OnEnergyRateChanged += OnEnergyRateChanged;
            Mod.OnHealthRateChanged += OnHealthRateChanged;
            Mod.OnKillCountChanged += OnKillCountChanged;
            Mod.OnDeathCountChanged += OnDeathCountChanged;
            Mod.OnRaidCountChanged += OnRaidCountChanged;
            Mod.OnSurvivedRaidCountChanged += OnSurvivedRaidCountChanged;
            Mod.OnRunthroughRaidCountChanged += OnRunthroughRaidCountChanged;
            Mod.OnMIARaidCountChanged += OnMIARaidCountChanged;
            Mod.OnKIARaidCountChanged += OnKIARaidCountChanged;

            // Initialize UI
            Init();

            // Ensure disabled by default
            Close();
        }

        public void Init()
        {
            OnPlayerLevelChanged();
            OnPlayerExperienceChanged();
            for(int i = 0; i < partImages.Length; ++i)
            {
                OnPartHealthChanged(i);
            }
            OnPlayerWeightChanged();
            OnHydrationChanged();
            OnEnergyChanged();
            OnHydrationRateChanged();
            OnEnergyRateChanged();
            for (int i = 0; i < Mod.GetHealthCount(); ++i) 
            {
                OnHealthRateChanged(i);
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
                }

                SkillUI skillUI = Instantiate(skillPrefab, currentPair).GetComponent<SkillUI>();
                skillUI.SetSkill(Mod.skills[i]);
            }
        }

        public void Update()
        {
            if (IsOpen() && Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 5)
            {
                OnCloseClicked();
            }

            UpdateStamina();
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
        }

        public void OnEnergyChanged()
        {
            energy.text = ((int)Mod.energy).ToString() +"/"+(int)Mod.currentMaxEnergy;
        }

        public void OnHydrationRateChanged()
        {
            if(Mod.currentHydrationRate == 0)
            {
                hydrationDelta.gameObject.SetActive(false);
            }
            else
            {
                hydrationDelta.gameObject.SetActive(true);
                hydrationDelta.text = String.Format((Mod.currentHydrationRate < 0 ? "" : "+") + "{0:0.##}", Mod.currentHydrationRate);
            }
        }

        public void OnEnergyRateChanged()
        {
            if(Mod.currentEnergyRate == 0)
            {
                energyDelta.gameObject.SetActive(false);
            }
            else
            {
                energyDelta.gameObject.SetActive(true);
                energyDelta.text = String.Format((Mod.currentEnergyRate < 0 ? "" : "+") + "{0:0.##}", Mod.currentEnergyRate);
            }
        }

        public void OnHealthRateChanged(int index)
        {
            float total = 0;
            for(int i=0; i < Mod.GetHealthCount(); ++i)
            {
                total += Mod.GetHealthRate(i);
                total += Mod.GetNonLethalHealthRate(i);
            }

            if(total == 0)
            {
                healthDelta.gameObject.SetActive(false);
            }
            else
            {
                healthDelta.gameObject.SetActive(true);
                healthDelta.text = String.Format((total < 0 ? "" : "+") + "{0:0.##}", total);
            }
        }

        public void OnPartHealthChanged(int index)
        {
            if (index == -1)
            {
                for(int i=0; i < Mod.GetHealthCount(); ++i)
                {
                    partImages[i].color = Color.Lerp(Color.white, Color.red, Mod.GetHealth(i) / Mod.currentMaxHealth[i]);
                    partHealth[i].text = ((int)Mod.GetHealth(i)).ToString() + "/" + ((int)Mod.currentMaxHealth[i]);
                }
            }
            else
            {
                partImages[index].color = Color.Lerp(Color.white, Color.red, Mod.GetHealth(index) / Mod.currentMaxHealth[index]);
                partHealth[index].text = ((int)Mod.GetHealth(index)).ToString() + "/" + ((int)Mod.currentMaxHealth[index]);
            }
        }

        public void OnPlayerWeightChanged()
        {
            weight.text = String.Format("{0:0.#}", Mod.weight / 1000.0f) + "/ " + String.Format("{0:0.#}", Mod.currentWeightLimit / 1000.0f);
            if (Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100.0f * 20) // Current weight limit + 20%
            {
                weight.color = Color.red;

                // Enable hard overweight icon, disable overweight icon
                effectIcons[6].SetActive(false);
                effectIcons[7].SetActive(true);
            }
            else if (Mod.weight > Mod.currentWeightLimit)
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

        public void SetExtractionLimitTimer(float raidTimeLeft)
        {
            extractionTimer.text = Mod.FormatTimeString(raidTimeLeft);
        }

        public void UpdateSkillUI(int skillIndex)
        {
            SkillUI skillUI = skillUIs[skillIndex];
            float currentProgress = Mod.skills[skillIndex].currentProgress % 100;
            //skillUI.text.text = String.Format("{0} lvl. {1:0} ({2:0}/100)", Mod.SkillIndexToName(skillIndex), (int)(Mod.skills[skillIndex].currentProgress / 100), currentProgress);
            skillUI.barFill.sizeDelta = new Vector2(currentProgress, 4.73f);

            if (Mod.skills[skillIndex].increasing)
            {
                skillUI.increasing.SetActive(true);
                skillUI.diminishingReturns.SetActive(false);
            }
            else if (Mod.skills[skillIndex].dimishingReturns)
            {
                skillUI.increasing.SetActive(false);
                skillUI.diminishingReturns.SetActive(true);
            }
            else
            {
                skillUI.increasing.SetActive(false);
                skillUI.diminishingReturns.SetActive(false);
            }
        }

        private void UpdateStamina()
        {
            Vector3 movementVector = GM.CurrentMovementManager.m_smoothLocoVelocity;
            bool sprintEngaged = GM.CurrentMovementManager.m_sprintingEngaged;

            if (sprintEngaged)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.sprintStaminaDrain * Time.deltaTime;

                if (Mod.weight > Mod.currentWeightLimit)
                {
                    currentStaminaDrain += Mod.overweightStaminaDrain * Time.deltaTime;
                }

                Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

                //if(Mod.stamina == 0)
                //{
                // Dont need to do anything here, here movement manager, we will patch to make sure that sprint is disengaged when we reach 0 stamina
                //}
            }
            else if (movementVector.magnitude > 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // Reset stamina timer
                Mod.staminaTimer = 2;

                float currentStaminaDrain = Mod.overweightStaminaDrain * Time.deltaTime;

                Mod.stamina = Mathf.Max(Mod.stamina - currentStaminaDrain, 0);

                StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
            }
            else if (Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100.0f * 20)
            {
                // Reset stamina timer to prevent stamina regen even while not moving if we are 20% above max weight
                Mod.staminaTimer = 2;
            }
            else // Not using stamina
            {
                if (Mod.staminaTimer > 0)
                {
                    Mod.staminaTimer -= Time.deltaTime;
                }
                else
                {
                    Mod.stamina = Mathf.Min(Mod.stamina + Mod.staminaRestoration * Time.deltaTime, Mod.currentMaxStamina);

                    StaminaUI.instance.barFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
                }
            }

            // If reach 0 stamina due to being overweight, activate overweight fatigue effect
            if (Mod.stamina == 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // TODO: maybe keep whether we have overweight fatigue as a bool in effects so we dont have to check the whole list every frame
                bool found = false;
                foreach (Effect effect in Effect.effects)
                {
                    if (effect.effectType == Effect.EffectType.OverweightFatigue)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    Effect newEffect = new Effect();
                    newEffect.effectType = Effect.EffectType.OverweightFatigue;
                    Effect.effects.Add(newEffect);

                    // Activate overweight fatigue icon
                    effectIcons[9].SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < Effect.effects.Count; ++i)
                {
                    if (Effect.effects[i].effectType == Effect.EffectType.OverweightFatigue)
                    {
                        // The overweight fatigue could also have caused an energy rate effect, need to remove that too
                        if (Effect.effects[i].caused.Count > 0)
                        {
                            Effect.effects.Remove(Effect.effects[i].caused[0]);
                        }
                        Effect.effects.RemoveAt(i);

                        // Deactivate overweight fatigue icon
                        effectIcons[9].SetActive(true);

                        break;
                    }
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
            TODO: // Must review if this will actually make slotted equipment items hidden, because it won't if they don't get parented to the slot
            canvas.SetActive(false);
            slotsParent.SetActive(false);
        }

        public void Open()
        {
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
            Mod.OnPlayerWeightChanged -= OnPlayerWeightChanged;
            Mod.OnPlayerExperienceChanged -= OnPlayerExperienceChanged;
            Mod.OnHydrationChanged -= OnHydrationChanged;
            Mod.OnEnergyChanged -= OnEnergyChanged;
            Mod.OnHydrationRateChanged -= OnHydrationRateChanged;
            Mod.OnEnergyRateChanged -= OnEnergyRateChanged;
            Mod.OnHealthRateChanged -= OnHealthRateChanged;
            Mod.OnKillCountChanged -= OnKillCountChanged;
            Mod.OnDeathCountChanged -= OnDeathCountChanged;
            Mod.OnRaidCountChanged -= OnRaidCountChanged;
            Mod.OnSurvivedRaidCountChanged -= OnSurvivedRaidCountChanged;
            Mod.OnRunthroughRaidCountChanged -= OnRunthroughRaidCountChanged;
            Mod.OnMIARaidCountChanged -= OnMIARaidCountChanged;
            Mod.OnKIARaidCountChanged -= OnKIARaidCountChanged;
        }

        [Serializable]
        public class PartEffects
        {
            public GameObject[] partEffects;

            public GameObject this[int i]
            {
                get { return partEffects[i]; }
                set { partEffects[i] = value; }
            }

            public int Length
            {
                get { return partEffects.Length; }
            }
        }
    }
}
