using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EFM
{
    public class EFM_PlayerStatusManager : MonoBehaviour
    {
        private bool init;
        private Vector3 setPosition;
        private Quaternion setRotation;

        public Text[] partHealthTexts;
        public Image[] partHealthImages;
        public Text healthText;
        public Text healthDeltaText;
        public Text hydrationText;
        public Text hydrationDeltaText;
        public Text energyText;
        public Text energyDeltaText;
        private Text weightText;
        public Text extractionTimerText;
        private Transform notificationsParent;
        private GameObject notificationPrefab;
        private AudioSource notificationSound;

        private AudioSource buttonClickAudio;
        private List<TraderTask> taskList;
        private List<GameObject> taskUIList;
        private Dictionary<string, TraderTask> taskByID;
        private Dictionary<string, GameObject> taskUIByID;

        public bool displayed;
        private int mustUpdateTaskListHeight;

        public void Init()
        {
            // Set part health elements
            partHealthTexts = new Text[7];
            partHealthImages = new Image[7];
            Transform partHealthParent = transform.GetChild(0).GetChild(7);
            Transform partImageParent = transform.GetChild(0).GetChild(1);
            for (int i = 0; i < 7; ++i)
            {
                partHealthTexts[i] = partHealthParent.GetChild(i).GetChild(1).GetComponent<Text>();
                partHealthImages[i] = partImageParent.GetChild(i).GetComponent<Image>();
            }
            // Set main stats texts
            healthText = transform.GetChild(0).GetChild(3).GetChild(1).GetComponent<Text>();
            healthDeltaText = transform.GetChild(0).GetChild(3).GetChild(3).GetComponent<Text>();
            hydrationText = transform.GetChild(0).GetChild(4).GetChild(1).GetComponent<Text>();
            hydrationDeltaText = transform.GetChild(0).GetChild(4).GetChild(3).GetComponent<Text>();
            energyText = transform.GetChild(0).GetChild(5).GetChild(1).GetComponent<Text>();
            energyDeltaText = transform.GetChild(0).GetChild(5).GetChild(3).GetComponent<Text>();
            weightText = transform.GetChild(0).GetChild(6).GetChild(1).GetComponent<Text>();
            // Set equipment slots
            Transform equipSlotParent = transform.GetChild(2);
            Mod.equipmentSlots = new List<EFM_EquipmentSlot>();
            for (int i = 0; i < 8; ++i)
            {
                GameObject slotObject = equipSlotParent.GetChild(i).GetChild(0).gameObject;
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
                switch (i)
                {
                    case 0:
                        slotComponent.equipmentType = Mod.ItemType.Backpack;
                        break;
                    case 1:
                        slotComponent.equipmentType = Mod.ItemType.BodyArmor;
                        break;
                    case 2:
                        slotComponent.equipmentType = Mod.ItemType.Earpiece;
                        break;
                    case 3:
                        slotComponent.equipmentType = Mod.ItemType.Headwear;
                        break;
                    case 4:
                        slotComponent.equipmentType = Mod.ItemType.FaceCover;
                        break;
                    case 5:
                        slotComponent.equipmentType = Mod.ItemType.Eyewear;
                        break;
                    case 6:
                        slotComponent.equipmentType = Mod.ItemType.Rig;
                        break;
                    case 7:
                        slotComponent.equipmentType = Mod.ItemType.Pouch;
                        break;
                }

                // Set slot sphere materials
                slotObject.transform.GetChild(0).GetChild(0).GetComponent<Renderer>().material = Mod.quickSlotHoverMaterial;
                slotObject.transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material = Mod.quickSlotConstantMaterial;

                // Reactivate slot
                slotObject.SetActive(true);
            }
            // Set exit button
            EFM_PointableButton exitButton = transform.GetChild(0).GetChild(10).gameObject.AddComponent<EFM_PointableButton>();
            exitButton.SetButton();
            exitButton.MaxPointingRange = 30;
            exitButton.hoverSound = transform.GetChild(4).GetComponent<AudioSource>();
            buttonClickAudio = transform.GetChild(5).GetComponent<AudioSource>();
            exitButton.Button.onClick.AddListener(() => { OnExitClick(); });
            // Set extraction timer text
            extractionTimerText = transform.GetChild(0).GetChild(9).GetChild(0).GetChild(1).GetChild(0).GetComponent<Text>();

            // Init task list, fill it with trader tasks currently active and complete, set buttons and hoverscrolls
            EFM_HoverScroll downHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
            EFM_HoverScroll upHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
            downHoverScroll.MaxPointingRange = 30;
            downHoverScroll.hoverSound = transform.GetChild(4).GetComponent<AudioSource>();
            downHoverScroll.scrollbar = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            downHoverScroll.other = upHoverScroll;
            downHoverScroll.up = false;
            upHoverScroll.MaxPointingRange = 30;
            upHoverScroll.hoverSound = transform.GetChild(4).GetComponent<AudioSource>();
            upHoverScroll.scrollbar = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            upHoverScroll.other = downHoverScroll;
            upHoverScroll.up = true;
            // Set task list toggle button
            EFM_PointableButton taskListButton = transform.GetChild(1).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            taskListButton.SetButton();
            taskListButton.MaxPointingRange = 30;
            taskListButton.hoverSound = transform.GetChild(4).GetComponent<AudioSource>();
            taskListButton.Button.onClick.AddListener(() => { OnToggleTaskListClick(); });

            // Init notificaiton stuff
            notificationsParent = transform.GetChild(0).GetChild(12);
            notificationPrefab = notificationsParent.GetChild(1).gameObject;
            notificationSound = notificationsParent.GetChild(0).GetComponent<AudioSource>();

            // Set background pointable
            FVRPointable backgroundPointable = transform.GetChild(0).gameObject.AddComponent<FVRPointable>();
            backgroundPointable.MaxPointingRange = 30;

            // Set as not active by default
            transform.GetChild(0).gameObject.SetActive(false);
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(2).gameObject.SetActive(false);

            init = true;
        }

        private void Update()
        {
            if (!init)
            {
                return;
            }

            if (displayed && Vector3.Distance(setPosition, GM.CurrentPlayerRoot.position) > 10)
            {
                SetDisplayed(false);
            }

            // Left (TODO: Non main hand) menu button
            if (Mod.leftHand.fvrHand.Input.BYButtonDown)
            {
                SetDisplayed(!displayed);
            }
            if (Mod.leftHand.fvrHand.Input.BYButtonPressed && displayed)
            {
                setPosition = Mod.leftHand.transform.position + Mod.leftHand.transform.forward * 0.6f + Mod.leftHand.transform.right * 0.3f;
                setRotation = Quaternion.Euler(0, Mod.leftHand.transform.rotation.eulerAngles.y, 0);
            }
            if (displayed)
            {
                transform.position = setPosition;
                transform.rotation = setRotation;
            }

            UpdateStamina();

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

        public void SetExtractionLimitTimer(float raidTimeLeft)
        {
            extractionTimerText.text = Mod.FormatTimeString(raidTimeLeft);
        }

        public void AddNotification(string text)
        {
            Mod.instance.LogInfo("Add notification called");
            notificationSound.Play();
            GameObject notification = Instantiate(notificationPrefab, notificationsParent);
            notification.SetActive(true);
            notification.transform.GetChild(0).GetChild(0).GetComponent<Text>().text = text;
            notification.AddComponent<EFM_DestroyTimer>().timer = 5;
        }

        public void SetDisplayed(bool displayed)
        {
            this.displayed = displayed;

            transform.GetChild(0).gameObject.SetActive(this.displayed);
            transform.GetChild(1).gameObject.SetActive(this.displayed);
            transform.GetChild(2).gameObject.SetActive(this.displayed);

            if (this.displayed)
            {
                // TODO: Player inventory closing sound
            }
            else
            {
                // TODO: Play inventory opening sound
            }

            foreach (EFM_EquipmentSlot equipSlot in Mod.equipmentSlots)
            {
                if (equipSlot.CurObject != null)
                {
                    equipSlot.CurObject.gameObject.SetActive(this.displayed);
                }
            }
        }

        private void UpdateStamina()
        {
            Vector3 movementVector = (Vector3)typeof(FVRMovementManager).GetField("m_twoAxisVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GM.CurrentMovementManager);
            bool sprintEngaged = (bool)typeof(FVRMovementManager).GetField("m_sprintingEngaged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(GM.CurrentMovementManager);

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

                Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);

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

                Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
            }
            else if(Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100.0f * 20)
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

                    Mod.staminaBarUI.transform.GetChild(0).GetChild(1).GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mod.stamina);
                }
            }

            // If reach 0 stamina due to being overweight, activate overweight fatigue effect
            if (Mod.stamina == 0 && Mod.weight > Mod.currentWeightLimit)
            {
                // TODO: maybe keep whether we have overweight fatigue as a bool in effects so we dont have to check the whole list every frame
                bool found = false;
                foreach (EFM_Effect effect in EFM_Effect.effects)
                {
                    if (effect.effectType == EFM_Effect.EffectType.OverweightFatigue)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    EFM_Effect newEffect = new EFM_Effect();
                    newEffect.effectType = EFM_Effect.EffectType.OverweightFatigue;
                    EFM_Effect.effects.Add(newEffect);

                    // Activate overweight fatigue icon
                    transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(true);
                }
            }
            else
            {
                for (int i = 0; i < EFM_Effect.effects.Count; ++i)
                {
                    if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.OverweightFatigue)
                    {
                        // The overweight fatigue could also have caused an energy rate effect, need to remove that too
                        if (EFM_Effect.effects[i].caused.Count > 0)
                        {
                            EFM_Effect.effects.Remove(EFM_Effect.effects[i].caused[0]);
                        }
                        EFM_Effect.effects.RemoveAt(i);

                        // Deactivate overweight fatigue icon
                        transform.GetChild(0).GetChild(2).GetChild(9).gameObject.SetActive(false);

                        break;
                    }
                }
            }
        }

        private void OnExitClick()
        {
            buttonClickAudio.Play();

            SetDisplayed(false);
        }

        private void OnToggleTaskListClick()
        {
            bool listNowActive = !transform.GetChild(1).GetChild(0).GetChild(1).gameObject.activeSelf;
            transform.GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(listNowActive);
            transform.GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(!listNowActive);
            transform.GetChild(1).GetChild(0).GetChild(0).GetChild(2).gameObject.SetActive(listNowActive);
        }

        public void UpdateWeight()
        {
            weightText.text = String.Format("{0:0.#}", Mod.weight / 1000.0f) + "/ " + String.Format("{0:0.#}", Mod.currentWeightLimit / 1000.0f);
            if(Mod.weight > Mod.currentWeightLimit + Mod.currentWeightLimit / 100.0f * 20) // Current weight limit + 20%
            {
                weightText.color = Color.red;

                // Enable hard overweight icon, disable overweight icon
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(true);
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(false);
            }
            else if(Mod.weight > Mod.currentWeightLimit)
            {
                weightText.color = Color.yellow;

                // Enable overweight icon, disable hard overweight icon
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(true);
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(false);
            }
            else
            {
                weightText.color = Color.white;

                // Disable overweight icons
                transform.GetChild(0).GetChild(2).GetChild(6).gameObject.SetActive(false);
                transform.GetChild(0).GetChild(2).GetChild(7).gameObject.SetActive(false);
            }
        }

        public void UpdatePlayerLevel()
        {
            transform.GetChild(0).GetChild(11).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
            transform.GetChild(0).GetChild(11).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
        }

        public void AddTask(TraderTask task)
        {
            // Add to logic lists
            if(taskList == null)
            {
                taskList = new List<TraderTask>();
            }
            taskList.Add(task);
            if(taskByID == null)
            {
                taskByID = new Dictionary<string, TraderTask>();
            }
            taskByID.Add(task.ID, task);

            // Make new task UI element
            Transform tasksParent = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            float taskListHeight = 29 * (tasksParent.childCount - 1) + 29;

            GameObject taskUI = Instantiate(taskTemplate, tasksParent);
            taskUI.SetActive(true);
            task.statusListElement = taskUI;

            // Short info
            Transform shortInfo = taskUI.transform.GetChild(0);
            shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
            shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;
            if(task.taskState == TraderTask.TaskState.Complete)
            {
                shortInfo.GetChild(2).gameObject.SetActive(true);
                shortInfo.GetChild(3).gameObject.SetActive(false);
            }

            // Description
            Transform description = taskUI.transform.GetChild(1);
            description.GetChild(0).GetComponent<Text>().text = task.description;
            // Objectives (conditions)
            Transform objectivesParent = description.GetChild(1).GetChild(1);
            GameObject objectiveTemplate = objectivesParent.GetChild(0).gameObject;
            int completedCount = 0;
            int totalCount = 0;
            foreach (TraderTaskCondition currentCondition in task.completionConditions)
            {
                if (currentCondition.fulfilled)
                {
                    ++completedCount;
                }
                ++totalCount;
                GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                currentObjectiveElement.SetActive(true);
                currentCondition.marketListElement = currentObjectiveElement;

                Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                // Progress counter, only necessary if value > 1 and for specific condition types
                if (currentCondition.value > 1)
                {
                    switch (currentCondition.conditionType)
                    {
                        case TraderTaskCondition.ConditionType.CounterCreator:
                            foreach (TraderTaskCounterCondition counter in currentCondition.counters)
                            {
                                if (counter.counterConditionType == TraderTaskCounterCondition.CounterConditionType.Kills)
                                {
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)counter.killCount) / currentCondition.value * 60, 6);
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = counter.killCount.ToString() + "/" + currentCondition.value;
                                    break;
                                }
                            }
                            break;
                        case TraderTaskCondition.ConditionType.HandoverItem:
                        case TraderTaskCondition.ConditionType.FindItem:
                        case TraderTaskCondition.ConditionType.LeaveItemAtLocation:
                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                            objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)currentCondition.itemCount) / currentCondition.value * 60, 6);
                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                            objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.itemCount.ToString() + "/" + currentCondition.value;
                            break;
                        default:
                            break;
                    }
                }

                // Disable condition gameObject if visibility conditions not met
                if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                {
                    foreach (TraderTaskCondition visibilityCondition in currentCondition.visibilityConditions)
                    {
                        if (!visibilityCondition.fulfilled)
                        {
                            currentObjectiveElement.SetActive(false);
                            break;
                        }
                    }
                }
            }
            // Rewards
            Transform rewardParent = description.GetChild(2);
            rewardParent.gameObject.SetActive(true);
            GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
            Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
            currentRewardHorizontal.gameObject.SetActive(true);
            foreach (TraderTaskReward reward in task.successRewards)
            {
                // Add new horizontal if necessary
                if (currentRewardHorizontal.childCount == 6)
                {
                    currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    currentRewardHorizontal.gameObject.SetActive(true);
                }
                switch (reward.taskRewardType)
                {
                    case TraderTaskReward.TaskRewardType.Item:
                        GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                        currentRewardItemElement.SetActive(true);
                        if (Mod.itemIcons.ContainsKey(reward.itemID))
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        if (reward.amount > 1)
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                        }
                        else
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        }
                        string itemRewardName = Mod.itemNames[reward.itemID];
                        currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = itemRewardName;

                        // Setup ItemIcon
                        EFM_ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                        itemIconScript.itemID = reward.itemID;
                        itemIconScript.itemName = itemRewardName;
                        itemIconScript.description = Mod.itemDescriptions[reward.itemID];
                        itemIconScript.weight = Mod.itemWeights[reward.itemID];
                        itemIconScript.volume = Mod.itemVolumes[reward.itemID];
                        break;
                    case TraderTaskReward.TaskRewardType.TraderUnlock:
                        GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                        currentRewardTraderUnlockElement.SetActive(true);
                        currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                        currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                        break;
                    case TraderTaskReward.TaskRewardType.TraderStanding:
                        GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardStandingElement.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                        currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                        currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                        break;
                    case TraderTaskReward.TaskRewardType.Experience:
                        GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardExperienceElement.SetActive(true);
                        currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                        currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.experience > 0 ? "+" : "-") + reward.experience;
                        break;
                    case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                        GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                        currentRewardAssortElement.SetActive(true);
                        if (Mod.itemIcons.ContainsKey(reward.itemID))
                        {
                            currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        string assortRewardName = Mod.itemNames[reward.itemID];
                        currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = assortRewardName;

                        // Setup ItemIcon
                        EFM_ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                        assortIconScript.itemID = reward.itemID;
                        assortIconScript.itemName = assortRewardName;
                        assortIconScript.description = Mod.itemDescriptions[reward.itemID];
                        assortIconScript.weight = Mod.itemWeights[reward.itemID];
                        assortIconScript.volume = Mod.itemVolumes[reward.itemID];
                        break;
                    default:
                        break;
                }
            }
            // TODO: Maybe have fail conditions and fail rewards sections

            // Set total progress depending on conditions
            float fractionCompletion = ((float)completedCount) / totalCount;
            shortInfo.GetChild(4).GetChild(0).GetComponent<Text>().text = String.Format("{0:0}%", fractionCompletion * 100);
            shortInfo.GetChild(4).GetChild(1).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(fractionCompletion * 60, 6);

            // Setup buttons
            // ShortInfo
            EFM_PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<EFM_PointableButton>();
            pointableTaskShortInfoButton.SetButton();
            pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
            pointableTaskShortInfoButton.MaxPointingRange = 20;
            pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

            // Add to UI lists
            if (taskUIList == null)
            {
                taskUIList = new List<GameObject>();
            }
            taskUIList.Add(taskUI);
            if (taskUIByID == null)
            {
                taskUIByID = new Dictionary<string, GameObject>();
            }
            taskUIByID.Add(task.ID, taskUI);

            // Update Hover scrolls based on new height
            EFM_HoverScroll downHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            if (taskListHeight > 245)
            {
                downHoverScroll.rate = 245 / (taskListHeight - 245);
                upHoverScroll.rate = 245 / (taskListHeight - 245);
                downHoverScroll.gameObject.SetActive(true);
                upHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downHoverScroll.gameObject.SetActive(false);
                upHoverScroll.gameObject.SetActive(false);
            }
        }
        
        public void OnTaskShortInfoClick(GameObject description)
        {
            // Toggle task description
            description.SetActive(!description.activeSelf);
            buttonClickAudio.Play();

            mustUpdateTaskListHeight = 1;
        }

        public void UpdateTaskListHeight()
        {
            EFM_HoverScroll downHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upHoverScroll = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            Transform tasksParent = transform.GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            float taskListHeight = 29 * (tasksParent.childCount - 1);
            if (taskListHeight > 245)
            {
                downHoverScroll.rate = 245 / (taskListHeight - 245);
                upHoverScroll.rate = 245 / (taskListHeight - 245);
                downHoverScroll.gameObject.SetActive(true);
                upHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downHoverScroll.gameObject.SetActive(false);
                upHoverScroll.gameObject.SetActive(false);
            }
        }
    }
}
