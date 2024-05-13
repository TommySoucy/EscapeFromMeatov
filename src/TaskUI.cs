using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class TaskUI : MonoBehaviour
    {
        public Task task;

        public Text taskName;
        public Text location;
        public GameObject availableStatus;
        public GameObject activeStatus;
        public GameObject completeStatus;
        public Text percentage;
        public RectTransform barFill;
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

        public void SetTask(Task task)
        {
            this.task = task;

            // Short info
            task.marketUI.taskName.text = task.name;
            task.marketUI.location.text = task.location;

            // Description
            task.marketUI.description.text = task.description;

            // Completion conditions
            foreach (Condition currentCondition in task.finishConditions)
            {
                GameObject currentObjectiveElement = Instantiate(conditionPrefab, objectivesParent);
                currentObjectiveElement.SetActive(true);
                currentCondition.marketUI = currentObjectiveElement.GetComponent<TaskConditionUI>();

                currentCondition.marketUI.SetCondition(currentCondition);
            }

            // Initial equipment
            if (task.startRewards != null && task.startRewards.Count > 0)
            {
                initEquipParent.gameObject.SetActive(true);
                Transform currentInitEquipHorizontal = Instantiate(initEquipHorizontalPrefab, initEquipParent).transform;
                foreach (Reward reward in task.startRewards)
                {
                    // Add new horizontal if necessary
                    if (currentInitEquipHorizontal.childCount == 6)
                    {
                        currentInitEquipHorizontal = Instantiate(initEquipHorizontalPrefab, initEquipParent).transform;
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentInitEquipItemElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            ItemRewardView itemRewardView = currentInitEquipItemElement.GetComponent<ItemRewardView>();
                            if(reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0]);
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
                            TraderRewardView traderRewardView = currentInitEquipTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentInitEquipStandingElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            StatRewardView statStandingRewardView = currentInitEquipStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentInitEquipExpElement = Instantiate(initEquipStatRewardPrefab, currentInitEquipHorizontal);
                            StatRewardView statExpRewardView = currentInitEquipExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentInitEquipBarterElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            ItemRewardView barterRewardView = currentInitEquipBarterElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                barterRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    barterRewardView.count.gameObject.SetActive(true);
                                    barterRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    barterRewardView.count.gameObject.SetActive(false);
                                }
                                barterRewardView.itemName.text = reward.itemIDs[0].name;
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
                            StatRewardView statSkillRewardView = currentInitEquipSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentInitEquipProdElement = Instantiate(initEquipItemRewardPrefab, currentInitEquipHorizontal);
                            ItemRewardView prodRewardView = currentInitEquipProdElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                prodRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    prodRewardView.count.gameObject.SetActive(true);
                                    prodRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    prodRewardView.count.gameObject.SetActive(false);
                                }
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

            // Rewards
            if (task.finishRewards != null && task.finishRewards.Count > 0)
            {
                initEquipParent.gameObject.SetActive(true);
                Transform currentRewardHorizontal = Instantiate(rewardsHorizontalPrefab, rewardsParent).transform;
                foreach (Reward reward in task.finishRewards)
                {
                    // Add new horizontal if necessary
                    if (currentRewardHorizontal.childCount == 6)
                    {
                        currentRewardHorizontal = Instantiate(rewardsHorizontalPrefab, rewardsParent).transform;
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentRewardItemElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            ItemRewardView itemRewardView = currentRewardItemElement.GetComponent<ItemRewardView>();
                            if(reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0]);
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
                            TraderRewardView traderRewardView = currentRewardTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentRewardStandingElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            StatRewardView statStandingRewardView = currentRewardStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentRewardExpElement = Instantiate(statRewardPrefab, currentRewardHorizontal);
                            StatRewardView statExpRewardView = currentRewardExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentRewardBarterElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            ItemRewardView barterRewardView = currentRewardBarterElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                barterRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    barterRewardView.count.gameObject.SetActive(true);
                                    barterRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    barterRewardView.count.gameObject.SetActive(false);
                                }
                                barterRewardView.itemName.text = reward.itemIDs[0].name;
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
                            StatRewardView statSkillRewardView = currentRewardSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentRewardProdElement = Instantiate(itemRewardPrefab, currentRewardHorizontal);
                            ItemRewardView prodRewardView = currentRewardProdElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                prodRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    prodRewardView.count.gameObject.SetActive(true);
                                    prodRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    prodRewardView.count.gameObject.SetActive(false);
                                }
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

            TODO:// Maybe have fail conditions section

            // Fail Rewards
            if (task.failRewards != null && task.failRewards.Count > 0)
            {
                failParent.gameObject.SetActive(true);
                Transform currentFailHorizontal = Instantiate(failHorizontalPrefab, failParent).transform;
                foreach (Reward reward in task.failRewards)
                {
                    // Add new horizontal if necessary
                    if (currentFailHorizontal.childCount == 6)
                    {
                        currentFailHorizontal = Instantiate(failHorizontalPrefab, failParent).transform;
                    }
                    switch (reward.rewardType)
                    {
                        case Reward.RewardType.Item:
                            GameObject currentFailItemElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            ItemRewardView itemRewardView = currentFailItemElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                itemRewardView.SetItem(reward.itemIDs[0]);
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
                            TraderRewardView traderRewardView = currentFailTraderElement.GetComponent<TraderRewardView>();
                            traderRewardView.traderIcon.sprite = traderRewardView.traderIcons[reward.trader.index];
                            traderRewardView.text.text = "Unlock " + reward.trader.name;
                            break;
                        case Reward.RewardType.TraderStanding:
                            GameObject currentFailStandingElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            StatRewardView statStandingRewardView = currentFailStandingElement.GetComponent<StatRewardView>();
                            statStandingRewardView.icon.sprite = statStandingRewardView.sprites[1];
                            statStandingRewardView.specificName.gameObject.SetActive(true);
                            statStandingRewardView.specificName.text = reward.trader.name;
                            statStandingRewardView.detailText.text = (reward.standing > 0 ? "+" : "") + reward.standing;
                            break;
                        case Reward.RewardType.Experience:
                            GameObject currentFailExpElement = Instantiate(failStatRewardPrefab, currentFailHorizontal);
                            StatRewardView statExpRewardView = currentFailExpElement.GetComponent<StatRewardView>();
                            statExpRewardView.icon.sprite = statExpRewardView.sprites[0];
                            statExpRewardView.specificName.gameObject.SetActive(false);
                            statExpRewardView.detailText.text = (reward.experience > 0 ? "+" : "") + reward.experience;
                            break;
                        case Reward.RewardType.AssortmentUnlock:
                            GameObject currentFailBarterElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            ItemRewardView barterRewardView = currentFailBarterElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                barterRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    barterRewardView.count.gameObject.SetActive(true);
                                    barterRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    barterRewardView.count.gameObject.SetActive(false);
                                }
                                barterRewardView.itemName.text = reward.itemIDs[0].name;
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
                            StatRewardView statSkillRewardView = currentFailSkillElement.GetComponent<StatRewardView>();
                            statSkillRewardView.icon.sprite = statSkillRewardView.sprites[2];
                            statSkillRewardView.specificName.gameObject.SetActive(true);
                            statSkillRewardView.specificName.text = reward.skill.displayName;
                            statSkillRewardView.detailText.text = (reward.value > 0 ? "+" : "") + reward.value;
                            break;
                        case Reward.RewardType.ProductionScheme:
                            GameObject currentFailProdElement = Instantiate(failItemRewardPrefab, currentFailHorizontal);
                            ItemRewardView prodRewardView = currentFailProdElement.GetComponent<ItemRewardView>();
                            if (reward.itemIDs.Count > 0)
                            {
                                prodRewardView.SetItem(reward.itemIDs[0]);
                                if (reward.amount > 1)
                                {
                                    prodRewardView.count.gameObject.SetActive(true);
                                    prodRewardView.count.text = reward.amount.ToString();
                                }
                                else
                                {
                                    prodRewardView.count.gameObject.SetActive(false);
                                }
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

            // Start
            PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<PointableButton>();
            pointableTaskStartButton.SetButton();
            pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
            pointableTaskStartButton.MaxPointingRange = 20;
            pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            // Finish
            PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<PointableButton>();
            pointableTaskFinishButton.SetButton();
            pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
            pointableTaskFinishButton.MaxPointingRange = 20;
            pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
        }

        public void OnClicked()
        {
            // Toggle task description
            full.SetActive(!full.activeSelf);
            StatusUI.instance.clickAudio.Play();

            StatusUI.instance.mustUpdateTaskListHeight = 1;
        }

        public void OnStartClicked()
        {
            //Set state of task to active
            task.taskState = Task.TaskState.Active;

            // Update market task list by making the shortinfo of the referenced task UI element in Task to show that it is active
            task.marketUI.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
            task.marketUI.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);
            task.marketUI.transform.GetChild(0).GetChild(5).gameObject.SetActive(true);
            task.marketUI.transform.GetChild(0).GetChild(6).gameObject.SetActive(false);

            // Update conditions that are dependent on this task being started, then update everything depending on those conditions
            if (Trader.questConditionsByTask.ContainsKey(task.ID))
            {
                foreach (Condition taskCondition in Trader.questConditionsByTask[task.ID])
                {
                    // If the condition requires this task to be started
                    if (taskCondition.value == 2)
                    {
                        Trader.FulfillCondition(taskCondition);
                    }
                }
            }

            // Add completion conditions to list if necessary
            foreach (Condition condition in task.completionConditions)
            {
                if (condition.visible)
                {
                    if (condition.conditionType == Condition.ConditionType.CounterCreator)
                    {
                        foreach (TaskCounterCondition counterCondition in condition.counters)
                        {
                            if (Mod.taskCompletionCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
                            {
                                Mod.taskCompletionCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
                            }
                            else
                            {
                                List<TaskCounterCondition> newList = new List<TaskCounterCondition>();
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
                            List<Condition> newList = new List<Condition>();
                            Mod.taskCompletionConditionsByType.Add(condition.conditionType, newList);
                            newList.Add(condition);
                        }
                    }
                }
            }

            // Spawn intial equipment 
            if (task.startingEquipment != null)
            {
                GivePlayerRewards(task.startingEquipment);
            }
        }
    }
}
