﻿using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class TaskUI : MonoBehaviour
    {
        public Task task;
        public bool market; // Whether this TaskUI is for market or StatusUI

        public Text taskName;
        public Text location;
        public GameObject availableStatus;
        public GameObject activeStatus;
        public GameObject completeStatus;
        public Text percentage;
        public GameObject progressBar;
        public RectTransform barFill;
        public GameObject startButton;
        public GameObject finishButton;
        public GameObject full;
        public Text description;
        public RectTransform objectivesParent;
        public GameObject conditionPrefab;
        public RectTransform initEquipParent;
        public GameObject initEquipHorizontalPrefab;
        public GameObject initEquipItemRewardPrefab;
        public GameObject initEquipStatRewardPrefab;
        public GameObject initEquipUnknownRewardPrefab;
        public GameObject initEquipTraderRewardPrefab;
        public RectTransform rewardsParent;
        public GameObject rewardsHorizontalPrefab;
        public GameObject itemRewardPrefab;
        public GameObject statRewardPrefab;
        public GameObject unknownRewardPrefab;
        public GameObject traderRewardPrefab;
        public RectTransform failParent;
        public GameObject failHorizontalPrefab;
        public GameObject failItemRewardPrefab;
        public GameObject failStatRewardPrefab;
        public GameObject failUnknownRewardPrefab;
        public GameObject failTraderRewardPrefab;

        [NonSerialized]
        public bool awakened;

        public void Awake()
        {
            awakened = true;

            if(task != null)
            {
                task.OnTaskStateChanged += OnTaskStateChanged;
                for (int i = 0; i < task.finishConditions.Count; ++i)
                {
                    task.finishConditions[i].OnConditionFulfillmentChanged += OnConditionFulfillmentChanged;
                }
            }
        }

        public void SetTask(Task task, bool market)
        {
            this.market = market;

            if(awakened && this.task != null)
            {
                this.task.OnTaskStateChanged -= OnTaskStateChanged;
                for(int i=0; i < this.task.finishConditions.Count; ++i)
                {
                    this.task.finishConditions[i].OnConditionFulfillmentChanged -= OnConditionFulfillmentChanged;
                }
            }

            this.task = task;

            if (awakened)
            {
                this.task.OnTaskStateChanged += OnTaskStateChanged;
                for (int i = 0; i < this.task.finishConditions.Count; ++i)
                {
                    this.task.finishConditions[i].OnConditionFulfillmentChanged += OnConditionFulfillmentChanged;
                }
            }

            // Short info
            taskName.text = task.name;
            location.text = task.location;

            // Description
            description.text = task.description;

            // Completion conditions
            foreach (Condition currentCondition in task.finishConditions)
            {
                GameObject currentObjectiveElement = Instantiate(conditionPrefab, objectivesParent);
                currentObjectiveElement.SetActive(true);
                currentCondition.UI = currentObjectiveElement.GetComponent<TaskConditionUI>();

                currentCondition.UI.SetCondition(currentCondition, market);
            }

            // Initial equipment
            if (task.startRewards != null && task.startRewards.Count > 0)
            {
                initEquipParent.gameObject.SetActive(true);
                Transform currentInitEquipHorizontal = Instantiate(initEquipHorizontalPrefab, initEquipParent).transform;
                currentInitEquipHorizontal.gameObject.SetActive(true);
                foreach (Reward reward in task.startRewards)
                {
                    // Add new horizontal if necessary
                    if (currentInitEquipHorizontal.childCount == 6)
                    {
                        currentInitEquipHorizontal = Instantiate(initEquipHorizontalPrefab, initEquipParent).transform;
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentInitEquipItemElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipItemElement.gameObject.SetActive(true);
                            ItemRewardView itemRewardView = currentInitEquipItemElement.GetComponent<ItemRewardView>();
                            if(reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0], false);
                                if (reward.amount > 1)
                                {
                                    itemRewardView.count.gameObject.SetActive(true);
                                    itemRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    itemRewardView.count.gameObject.SetActive(false);
                                }
                                itemRewardView.itemName.text = reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                itemRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                itemRewardView.itemName.text = "Missing data for reward "+ reward.ID;
                            }
                            break;
                        case Reward.RewardType.TraderUnlock:
                            GameObject currentInitEquipTraderElement = Instantiate(initEquipTraderRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipTraderElement.gameObject.SetActive(true);
                            TraderRewardView traderRewardView = currentInitEquipTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentInitEquipStandingElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipStandingElement.gameObject.SetActive(true);
                            StatRewardView statStandingRewardView = currentInitEquipStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentInitEquipExpElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipExpElement.gameObject.SetActive(true);
                            StatRewardView statExpRewardView = currentInitEquipExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentInitEquipBarterElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipBarterElement.gameObject.SetActive(true);
                            ItemRewardView barterRewardView = currentInitEquipBarterElement.GetComponent<ItemRewardView>();
                            if (reward.barters.Count > 0 && reward.barters[0].itemData.Count > 0)
                            {
                                barterRewardView.SetItem(reward.barters[0].itemData[0], false);
                                barterRewardView.count.gameObject.SetActive(false);
                                barterRewardView.itemName.text = reward.barters[0].itemData[0].name;
                            }
                            else // Missing item
                            {
                                barterRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                barterRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            barterRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.Skill:
                            GameObject currentInitEquipSkillElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipSkillElement.gameObject.SetActive(true);
                            StatRewardView statSkillRewardView = currentInitEquipSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentInitEquipProdElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipProdElement.gameObject.SetActive(true);
                            ItemRewardView prodRewardView = currentInitEquipProdElement.GetComponent<ItemRewardView>();
                            if (reward.productionProduct != null)
                            {
                                prodRewardView.SetItem(reward.productionProduct, true);
                                prodRewardView.count.gameObject.SetActive(false);
                                prodRewardView.itemName.text = " (Craft)" + reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                prodRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                prodRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            prodRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.TraderStandingRestore:
                            GameObject currentInitEquipStandingRestoreElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            currentInitEquipStandingRestoreElement.gameObject.SetActive(true);
                            StatRewardView statStandingRestoreRewardView = currentInitEquipStandingRestoreElement.GetComponent<StatRewardView>();
                            statStandingRestoreRewardView.icon.sprite = statStandingRestoreRewardView.sprites[1];
                            statStandingRestoreRewardView.specificName.gameObject.SetActive(true);
                            statStandingRestoreRewardView.specificName.text = reward.trader.name;
                            statStandingRestoreRewardView.detailText.text = (reward.trader.standingToRestore > 0 ? "+" : "") + reward.trader.standingToRestore;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                initEquipParent.gameObject.SetActive(false);
            }

            // Rewards
            if (task.finishRewards != null && task.finishRewards.Count > 0)
            {
                rewardsParent.gameObject.SetActive(true);
                Transform currentRewardHorizontal = Instantiate(rewardsHorizontalPrefab, rewardsParent).transform;
                currentRewardHorizontal.gameObject.SetActive(true);
                foreach (Reward reward in task.finishRewards)
                {
                    // Add new horizontal if necessary
                    if (currentRewardHorizontal.childCount == 6)
                    {
                        currentRewardHorizontal = Instantiate(rewardsHorizontalPrefab, rewardsParent).transform;
                        currentRewardHorizontal.gameObject.SetActive(true);
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentRewardItemElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            currentRewardItemElement.gameObject.SetActive(true);
                            ItemRewardView itemRewardView = currentRewardItemElement.GetComponent<ItemRewardView>();
                            if(reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0], true);
                                if (reward.amount > 1)
                                {
                                    itemRewardView.count.gameObject.SetActive(true);
                                    itemRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    itemRewardView.count.gameObject.SetActive(false);
                                }
                                itemRewardView.itemName.text = reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                itemRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                itemRewardView.itemName.text = "Missing data for reward "+ reward.ID;
                            }
                            break;
                        case Reward.RewardType.TraderUnlock:
                            GameObject currentRewardTraderElement = Instantiate(traderRewardPrefab, currentRewardHorizontal);
                            currentRewardTraderElement.gameObject.SetActive(true);
                            TraderRewardView traderRewardView = currentRewardTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentRewardStandingElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            currentRewardStandingElement.gameObject.SetActive(true);
                            StatRewardView statStandingRewardView = currentRewardStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentRewardExpElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            currentRewardExpElement.gameObject.SetActive(true);
                            StatRewardView statExpRewardView = currentRewardExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentRewardBarterElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            currentRewardBarterElement.gameObject.SetActive(true);
                            ItemRewardView barterRewardView = currentRewardBarterElement.GetComponent<ItemRewardView>();
                            if (reward.barters.Count > 0 && reward.barters[0].itemData.Count > 0)
                            {
                                barterRewardView.SetItem(reward.barters[0].itemData[0], false);
                                barterRewardView.count.gameObject.SetActive(false);
                                barterRewardView.itemName.text = reward.barters[0].itemData[0].name;
                            }
                            else // Missing item
                            {
                                barterRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                barterRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            barterRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.Skill:
                            GameObject currentRewardSkillElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            currentRewardSkillElement.gameObject.SetActive(true);
                            StatRewardView statSkillRewardView = currentRewardSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentRewardProdElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            currentRewardProdElement.gameObject.SetActive(true);
                            ItemRewardView prodRewardView = currentRewardProdElement.GetComponent<ItemRewardView>();
                            if (reward.productionProduct != null)
                            {
                                prodRewardView.SetItem(reward.productionProduct, true);
                                prodRewardView.count.gameObject.SetActive(false);
                                prodRewardView.itemName.text = " (Craft)" + reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                prodRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                prodRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            prodRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.TraderStandingRestore:
                            GameObject currentRewardStandingRestoreElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            currentRewardStandingRestoreElement.gameObject.SetActive(true);
                            StatRewardView statStandingRestoreRewardView = currentRewardStandingRestoreElement.GetComponent<StatRewardView>();
                            statStandingRestoreRewardView.icon.sprite = statStandingRestoreRewardView.sprites[1];
                            statStandingRestoreRewardView.specificName.gameObject.SetActive(true);
                            statStandingRestoreRewardView.specificName.text = reward.trader.name;
                            statStandingRestoreRewardView.detailText.text = (reward.trader.standingToRestore > 0 ? "+" : "") + reward.trader.standingToRestore;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                rewardsParent.gameObject.SetActive(false);
            }

            TODO:// Maybe have fail conditions section

            // Fail Rewards
            if (task.failRewards != null && task.failRewards.Count > 0)
            {
                failParent.gameObject.SetActive(true);
                Transform currentFailHorizontal = Instantiate(failHorizontalPrefab, failParent).transform;
                currentFailHorizontal.gameObject.SetActive(true);
                foreach (Reward reward in task.failRewards)
                {
                    // Add new horizontal if necessary
                    if (currentFailHorizontal.childCount == 6)
                    {
                        currentFailHorizontal = Instantiate(failHorizontalPrefab, failParent).transform;
                        currentFailHorizontal.gameObject.SetActive(true);
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentFailItemElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            currentFailItemElement.gameObject.SetActive(true);
                            ItemRewardView itemRewardView = currentFailItemElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0], true);
                                if (reward.amount > 1)
                                {
                                    itemRewardView.count.gameObject.SetActive(true);
                                    itemRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    itemRewardView.count.gameObject.SetActive(false);
                                }
                                itemRewardView.itemName.text = reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                itemRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                itemRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            break;
                        case Reward.RewardType.TraderUnlock:
                            GameObject currentFailTraderElement = Instantiate(failTraderRewardPrefab, currentFailHorizontal);
                            currentFailTraderElement.gameObject.SetActive(true);
                            TraderRewardView traderRewardView = currentFailTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentFailStandingElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            currentFailStandingElement.gameObject.SetActive(true);
                            StatRewardView statStandingRewardView = currentFailStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentFailExpElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            currentFailExpElement.gameObject.SetActive(true);
                            StatRewardView statExpRewardView = currentFailExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentFailBarterElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            currentFailBarterElement.gameObject.SetActive(true);
                            ItemRewardView barterRewardView = currentFailBarterElement.GetComponent<ItemRewardView>();
                            if (reward.barters.Count > 0 && reward.barters[0].itemData.Count > 0)
                            {
                                barterRewardView.SetItem(reward.barters[0].itemData[0], false);
                                barterRewardView.count.gameObject.SetActive(false);
                                barterRewardView.itemName.text = reward.barters[0].itemData[0].name;
                            }
                            else // Missing item
                            {
                                barterRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                barterRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            barterRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.Skill:
                            GameObject currentFailSkillElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            currentFailSkillElement.gameObject.SetActive(true);
                            StatRewardView statSkillRewardView = currentFailSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentFailProdElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            currentFailProdElement.gameObject.SetActive(true);
                            ItemRewardView prodRewardView = currentFailProdElement.GetComponent<ItemRewardView>();
                            if (reward.productionProduct != null)
                            {
                                prodRewardView.SetItem(reward.productionProduct, true);
                                prodRewardView.count.gameObject.SetActive(false);
                                prodRewardView.itemName.text = " (Craft)" + reward.itemIDs[0].name;
                            }
                            else // Missing item
                            {
                                prodRewardView.itemView.itemIcon.sprite = Mod.questionMarkIcon;
                                prodRewardView.itemName.text = "Missing data for reward " + reward.ID;
                            }
                            prodRewardView.unlockIcon.SetActive(true);
                            break;
                        case Reward.RewardType.TraderStandingRestore:
                            GameObject currentFailStandingRestoreElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            currentFailStandingRestoreElement.gameObject.SetActive(true);
                            StatRewardView statStandingRestoreRewardView = currentFailStandingRestoreElement.GetComponent<StatRewardView>();
                            statStandingRestoreRewardView.icon.sprite = statStandingRestoreRewardView.sprites[1];
                            statStandingRestoreRewardView.specificName.gameObject.SetActive(true);
                            statStandingRestoreRewardView.specificName.text = reward.trader.name;
                            statStandingRestoreRewardView.detailText.text = (reward.trader.standingToRestore > 0 ? "+" : "") + reward.trader.standingToRestore;
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                failParent.gameObject.SetActive(false);
            }

            // Set initial state specific UI
            OnTaskStateChanged(this.task);
            OnConditionFulfillmentChanged(null);
        }

        public void OnClicked()
        {
            // Toggle task description
            full.SetActive(!full.activeSelf);

            if (market)
            {
                HideoutController.instance.marketManager.clickAudio.Play();
                HideoutController.instance.marketManager.tasksHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            }
            else
            {
                StatusUI.instance.clickAudio.Play();
                StatusUI.instance.tasksHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            }
        }

        public void OnStartClicked()
        {
            // Set state of task to active
            // We do nothing else here, everything updates through OnTaskStateChanged event
            // Which is invoked by the task.taskState property setter
            task.taskState = Task.TaskState.Active;

            for (int i = 0; i < task.startRewards.Count; ++i)
            {
                if (task.startRewards[i].rewardType == Reward.RewardType.AssortmentUnlock)
                {
                    HideoutController.instance.marketManager.UpdateCategories();
                    break;
                }
            }
        }

        public void OnFinishClicked()
        {
            // Set state of task to Success
            // We do nothing else here, everything updates through OnTaskStateChanged event
            // Which is invoked by the task.taskState property setter
            task.taskState = Task.TaskState.Success;

            for(int i=0; i < task.finishRewards.Count; ++i)
            {
                if (task.finishRewards[i].rewardType == Reward.RewardType.AssortmentUnlock)
                {
                    HideoutController.instance.marketManager.UpdateCategories();
                    break;
                }
            }
        }

        public void OnTaskStateChanged(Task task)
        {
            switch (task.taskState)
            {
                case Task.TaskState.Locked:
                case Task.TaskState.Success:
                case Task.TaskState.Fail:
                    Destroy(gameObject);

                    for (int i = 0; i < task.failRewards.Count; ++i)
                    {
                        if (task.failRewards[i].rewardType == Reward.RewardType.AssortmentUnlock)
                        {
                            HideoutController.instance.marketManager.UpdateCategories();
                            break;
                        }
                    }
                    break;
                case Task.TaskState.Available:
                    if (!market)
                    {
                        Destroy(gameObject);
                        return;
                    }
                    availableStatus.SetActive(true);
                    activeStatus.SetActive(false);
                    completeStatus.SetActive(false);

                    startButton.SetActive(true);
                    progressBar.SetActive(false);
                    finishButton.SetActive(false);
                    break;
                case Task.TaskState.Active:
                    if (market)
                    {
                        availableStatus.SetActive(false);
                        startButton.SetActive(false);
                        finishButton.SetActive(false);
                    }
                    activeStatus.SetActive(true);
                    completeStatus.SetActive(false);
                    OnConditionFulfillmentChanged(null);
                    progressBar.SetActive(true);
                    break;
                case Task.TaskState.Complete:
                    if (market)
                    {
                        availableStatus.SetActive(false);
                        startButton.SetActive(false);
                        progressBar.SetActive(false);
                        finishButton.SetActive(true);
                    }
                    activeStatus.SetActive(false);
                    completeStatus.SetActive(true);
                    break;
            }
        }

        public void OnConditionFulfillmentChanged(Condition condition)
        {
            if(task.taskState == Task.TaskState.Active)
            {
                int fulfilledCount = 0;
                for (int i = 0; i < task.finishConditions.Count; ++i)
                {
                    if (task.finishConditions[i].fulfilled)
                    {
                        ++fulfilledCount;
                    }
                }
                float fraction = (float)fulfilledCount / (float)task.finishConditions.Count;
                barFill.sizeDelta = new Vector2(60 * fraction, 6);
                percentage.text = ((int)(100 * fraction)).ToString() + "%";
            }
        }

        public void OnDestroy()
        {
            if(awakened && task != null)
            {
                task.OnTaskStateChanged -= OnTaskStateChanged;
                for (int i = 0; i < task.finishConditions.Count; ++i)
                {
                    task.finishConditions[i].OnConditionFulfillmentChanged -= OnConditionFulfillmentChanged;
                }
            }
        }
    }
}
