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
                        case TaskReward.TaskRewardType.TraderStanding:
                            GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                            currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                            currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                            currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                            currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                            break;
                        case TaskReward.TaskRewardType.Experience:
                            GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                            currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                            currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                            break;
                        case TaskReward.TaskRewardType.AssortmentUnlock:
                            foreach (string item in reward.itemIDs)
                            {
                                if (currentInitEquipHorizontal.childCount == 6)
                                {
                                    currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                }
                                GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                if (Mod.itemIcons.ContainsKey(item))
                                {
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(item, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

                                // Setup ItemIcon
                                ItemIcon assortIconScript = currentInitEquipAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                                assortIconScript.itemID = item;
                                assortIconScript.itemName = Mod.itemNames[item];
                                assortIconScript.description = Mod.itemDescriptions[item];
                                assortIconScript.weight = Mod.itemWeights[item];
                                assortIconScript.volume = Mod.itemVolumes[item];
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            // Rewards
            Transform rewardParent = description.GetChild(3);
            rewardParent.gameObject.SetActive(true);
            GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
            Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
            currentRewardHorizontal.gameObject.SetActive(true);
            foreach (TaskReward reward in task.successRewards)
            {
                // Add new horizontal if necessary
                if (currentRewardHorizontal.childCount == 6)
                {
                    currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    currentRewardHorizontal.gameObject.SetActive(true);
                }
                switch (reward.taskRewardType)
                {
                    case TaskReward.TaskRewardType.Item:
                        GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                        currentRewardItemElement.SetActive(true);
                        if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        if (reward.amount > 1)
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                        }
                        else
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        }
                        currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];

                        // Setup ItemIcon
                        ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                        itemIconScript.itemID = reward.itemIDs[0];
                        itemIconScript.itemName = Mod.itemNames[reward.itemIDs[0]];
                        itemIconScript.description = Mod.itemDescriptions[reward.itemIDs[0]];
                        itemIconScript.weight = Mod.itemWeights[reward.itemIDs[0]];
                        itemIconScript.volume = Mod.itemVolumes[reward.itemIDs[0]];
                        break;
                    case TaskReward.TaskRewardType.TraderUnlock:
                        GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                        currentRewardTraderUnlockElement.SetActive(true);
                        currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                        currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                        break;
                    case TaskReward.TaskRewardType.TraderStanding:
                        GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardStandingElement.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                        currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                        currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                        break;
                    case TaskReward.TaskRewardType.Experience:
                        GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardExperienceElement.SetActive(true);
                        currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                        currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.experience > 0 ? "+" : "-") + reward.experience;
                        break;
                    case TaskReward.TaskRewardType.AssortmentUnlock:
                        foreach (string item in reward.itemIDs)
                        {
                            if (currentRewardHorizontal.childCount == 6)
                            {
                                currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                                currentRewardHorizontal.gameObject.SetActive(true);
                            }
                            GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                            currentRewardAssortElement.SetActive(true);
                            if (Mod.itemIcons.ContainsKey(item))
                            {
                                currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                            }
                            else
                            {
                                AnvilManager.Run(Mod.SetVanillaIcon(item, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                            }
                            currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                            currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

                            // Setup ItemIcon
                            ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                            assortIconScript.itemID = item;
                            assortIconScript.itemName = Mod.itemNames[item];
                            assortIconScript.description = Mod.itemDescriptions[item];
                            assortIconScript.weight = Mod.itemWeights[item];
                            assortIconScript.volume = Mod.itemVolumes[item];
                        }
                        break;
                    default:
                        break;
                }
            }
            TODO:// Maybe have fail conditions and fail rewards sections

            // Setup buttons
            // ShortInfo
            PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<PointableButton>();
            pointableTaskShortInfoButton.SetButton();
            pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
            pointableTaskShortInfoButton.MaxPointingRange = 20;
            pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
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
    }
}
