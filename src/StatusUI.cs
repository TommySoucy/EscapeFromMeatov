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
        public GameObject canvas;
        public GameObject slotsParent;
        public EquipmentSlot[] equipmentSlots; // Backpack, BodyArmor, EarPiece, HeadWear, FaceCover, EyeWear, Rig, Pouch
        public Sprite[] levelSprites;
        public Image levelIcon;
        public Text level;
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
        public HoverScroll tasksDownHoverScroll;
        public HoverScroll tasksUpHoverScroll;
        public GameObject infoClosedIcon;
        public GameObject infoOpenIcon;
        public GameObject infoView;
        public HoverScroll infoDownHoverScroll;
        public HoverScroll infoUpHoverScroll;
        public AudioSource clickAudio;
        public AudioSource notificationAudio;
        public AudioSource openAudio;
        public AudioSource closeAudio;
        public Text extractionTimer;
        public SkillUI[] skillUIs;

        public List<TaskUI> tasks = new List<TaskUI>();
        public Dictionary<string, TaskUI> taskByID = new Dictionary<string, TaskUI>();

        [NonSerialized]
        public int mustUpdateTaskListHeight;

        public void Awake()
        {
            // Set static instance
            instance = this;

            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;

            // Ensure disabled by default
            Close();
        }

        public void Update()
        {
            if (IsOpen() && Vector3.Distance(transform.position, GM.CurrentPlayerRoot.position) > 5)
            {
                OnCloseClicked();
            }

            UpdateStamina();

            TODO: // Make this work with hoverscroll processor
            if (mustUpdateTaskListHeight == 0)
            {
                UpdateTaskListHeight();
                --mustUpdateTaskListHeight;
            }
            else if (mustUpdateTaskListHeight > 0)
            {
                --mustUpdateTaskListHeight;
            }
        }

        public void AddTask(Task task)
        {
            // Make new task UI element
            float taskListHeight = 29 * (tasksParent.childCount - 1) + 29;

            GameObject taskUIObject = Instantiate(taskPrefab, tasksParent);
            taskUIObject.SetActive(true);
            TaskUI taskUI = taskUIObject.GetComponent<TaskUI>();
            task.playerUI = taskUI;

            // Short info
            Transform shortInfo = taskUI.transform.GetChild(0);
            shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
            shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;
            if (task.taskState == Task.TaskState.Complete)
            {
                shortInfo.GetChild(2).gameObject.SetActive(true);
                shortInfo.GetChild(3).gameObject.SetActive(false);
            }

            // Description
            taskUI.description.text = task.description;
            // Objectives (conditions)
            int completedCount = 0;
            int totalCount = 0;
            foreach (Condition currentCondition in task.finishConditions)
            {
                //if (currentCondition.fulfilled)
                //{
                //    ++completedCount;
                //}
                ++totalCount;
                GameObject currentObjectiveElement = Instantiate(taskUI.objectivePrefab, taskUI.objectivesParent);
                currentObjectiveElement.SetActive(true);
                TaskObjectiveUI currentObjective = currentObjectiveElement.GetComponent<TaskObjectiveUI>();
                //currentCondition.statusListElement = currentObjective;

                //currentObjective.text.text = currentCondition.text;
                // Progress counter, only necessary if value > 1 and for specific condition types
                if (currentCondition.value > 1)
                {
                    switch (currentCondition.conditionType)
                    {
                        case Condition.ConditionType.CounterCreator:
                            //foreach (TaskCounterCondition counter in currentCondition.counters)
                            //{
                            //    if (counter.counterConditionType == TaskCounterCondition.CounterConditionType.Kills)
                            //    {
                            //        currentObjective.progressBar.SetActive(true); // Activate progress bar
                            //        currentObjective.barFill.sizeDelta = new Vector2(((float)counter.killCount) / currentCondition.value * 60, 6);
                            //        currentObjective.counter.gameObject.SetActive(true); // Activate progress counter
                            //        currentObjective.counter.text = counter.killCount.ToString() + "/" + currentCondition.value;
                            //        break;
                            //    }
                            //}
                            break;
                        case Condition.ConditionType.HandoverItem:
                        case Condition.ConditionType.FindItem:
                        case Condition.ConditionType.LeaveItemAtLocation:
                            currentObjective.progressBar.SetActive(true); // Activate progress bar
                            //currentObjective.barFill.sizeDelta = new Vector2(((float)currentCondition.itemCount) / currentCondition.value * 60, 6);
                            //currentObjective.counter.gameObject.SetActive(true); // Activate progress counter
                            //currentObjective.counter.text = currentCondition.itemCount.ToString() + "/" + currentCondition.value;
                            break;
                        default:
                            break;
                    }
                }

                // Disable condition gameObject if visibility conditions not met
                if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                {
                    //foreach (Condition visibilityCondition in currentCondition.visibilityConditions)
                    //{
                    //    if (!visibilityCondition.fulfilled)
                    //    {
                    //        currentObjectiveElement.SetActive(false);
                    //        break;
                    //    }
                    //}
                }
            }
            // Rewards
            taskUI.rewardsParent.gameObject.SetActive(true);
            Transform currentRewardHorizontal = Instantiate(taskUI.rewardsHorizontalParent, taskUI.rewardsParent).transform;
            currentRewardHorizontal.gameObject.SetActive(true);
            //foreach (TaskReward reward in task.successRewards)
            //{
            //    // Add new horizontal if necessary
            //    if (currentRewardHorizontal.childCount == 2)
            //    {
            //        currentRewardHorizontal = Instantiate(taskUI.rewardsHorizontalParent, taskUI.rewardsParent).transform;
            //        currentRewardHorizontal.gameObject.SetActive(true);
            //    }
            //    switch (reward.taskRewardType)
            //    {
            //        case TaskReward.TaskRewardType.Item:
            //            GameObject currentRewardItemElement = Instantiate(taskUI.itemRewardPrefab, currentRewardHorizontal);
            //            currentRewardItemElement.SetActive(true);
            //            ItemRewardView itemRewardView = currentRewardItemElement.GetComponent<ItemRewardView>();
            //            string randomReward = reward.itemIDs[UnityEngine.Random.Range(0, reward.itemIDs.Length)];
            //            if (Mod.itemIcons.ContainsKey(randomReward))
            //            {
            //                itemRewardView.icon.sprite = Mod.itemIcons[randomReward];
            //            }
            //            else
            //            {
            //                AnvilManager.Run(Mod.SetVanillaIcon(randomReward, itemRewardView.icon));
            //            }
            //            if (reward.amount > 1)
            //            {
            //                itemRewardView.count.text = reward.amount.ToString();
            //            }
            //            else
            //            {
            //                itemRewardView.count.gameObject.SetActive(false);
            //            }
            //            string itemRewardName = Mod.itemNames[randomReward];
            //            itemRewardView.itemName.text = itemRewardName;
            //            break;
            //        case TaskReward.TaskRewardType.TraderUnlock:
            //            GameObject currentRewardTraderUnlockElement = Instantiate(taskUI.traderRewardPrefab, currentRewardHorizontal);
            //            currentRewardTraderUnlockElement.SetActive(true);
            //            TraderRewardView traderRewardView = currentRewardTraderUnlockElement.GetComponent<TraderRewardView>();
            //            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.traderIndex];
            //            traderRewardView.text.text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
            //            break;
            //        case TaskReward.TaskRewardType.TraderStanding:
            //            GameObject currentRewardStandingElement = Instantiate(taskUI.statRewardPrefab, currentRewardHorizontal);
            //            currentRewardStandingElement.SetActive(true);
            //            StatRewardView statRewardView = currentRewardStandingElement.GetComponent<StatRewardView>();
            //            statRewardView.icon.sprite = statRewardView.sprites[1];
            //            statRewardView.specificName.gameObject.SetActive(true);
            //            statRewardView.specificName.text = Mod.traderStatuses[reward.traderIndex].name;
            //            statRewardView.detailText.text = (reward.standing > 0 ? "+" : "-") + reward.standing;
            //            break;
            //        case TaskReward.TaskRewardType.Experience:
            //            GameObject currentRewardExperienceElement = Instantiate(taskUI.statRewardPrefab, currentRewardHorizontal);
            //            currentRewardExperienceElement.SetActive(true);
            //            StatRewardView expStatRewardView = currentRewardExperienceElement.GetComponent<StatRewardView>();
            //            expStatRewardView.icon.sprite = HideoutController.experienceSprite;
            //            expStatRewardView.detailText.text = (reward.experience > 0 ? "+" : "-") + reward.experience;
            //            break;
            //        case TaskReward.TaskRewardType.AssortmentUnlock:
            //            foreach (string unlockReward in reward.itemIDs)
            //            {
            //                if (currentRewardHorizontal.childCount == 2)
            //                {
            //                    currentRewardHorizontal = Instantiate(taskUI.rewardsHorizontalParent, taskUI.rewardsParent).transform;
            //                    currentRewardHorizontal.gameObject.SetActive(true);
            //                }
            //                GameObject currentRewardAssortElement = Instantiate(taskUI.itemRewardPrefab, currentRewardHorizontal);
            //                currentRewardAssortElement.SetActive(true);
            //                ItemRewardView unlockItemRewardView = currentRewardAssortElement.GetComponent<ItemRewardView>();
            //                if (Mod.itemIcons.ContainsKey(unlockReward))
            //                {
            //                    unlockItemRewardView.icon.sprite = Mod.itemIcons[unlockReward];
            //                }
            //                else
            //                {
            //                    AnvilManager.Run(Mod.SetVanillaIcon(unlockReward, unlockItemRewardView.icon));
            //                }
            //                unlockItemRewardView.count.gameObject.SetActive(false);
            //                string assortRewardName = Mod.itemNames[unlockReward];
            //                unlockItemRewardView.itemName.text = assortRewardName;
            //                unlockItemRewardView.unlockIcon.SetActive(true);
            //            }
            //            break;
            //        default:
            //            break;
            //    }
            //}
            // TODO: Maybe have fail conditions and fail rewards sections

            // Set total progress depending on conditions
            float fractionCompletion = ((float)completedCount) / totalCount;
            taskUI.percentage.text = String.Format("{0:0}%", fractionCompletion * 100);
            taskUI.barFill.sizeDelta = new Vector2(fractionCompletion * 60, 6);

            // Add to UI lists
            tasks.Add(taskUI);
            taskByID.Add(task.ID, taskUI);
        }

        public void UpdateTaskListHeight()
        {
            // We should always wait until 1 frame after setting the UI before calling this because
            // tasksParent.sizeDelta.y only get updated the frame after any changes
            // 250 is the height we see in the view
            if (tasksParent.sizeDelta.y > 250)
            {
                tasksDownHoverScroll.rate = 1 / (tasksParent.sizeDelta.y / 250);
                tasksUpHoverScroll.rate = tasksDownHoverScroll.rate;
                tasksDownHoverScroll.scrollbar.value = 1; // Put it back to the top
                tasksDownHoverScroll.gameObject.SetActive(true);
                tasksUpHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                tasksDownHoverScroll.gameObject.SetActive(false);
                tasksUpHoverScroll.gameObject.SetActive(false);
            }
        }

        public void OnPlayerLevelChanged()
        {
            levelIcon.sprite = levelSprites[Mod.level / 5];
            level.text = Mod.level.ToString();
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
                skillUI.progressing.SetActive(true);
                skillUI.diminishingReturns.SetActive(false);
            }
            else if (Mod.skills[skillIndex].dimishingReturns)
            {
                skillUI.progressing.SetActive(false);
                skillUI.diminishingReturns.SetActive(true);
            }
            else
            {
                skillUI.progressing.SetActive(false);
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

        public void UpdateWeight()
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
