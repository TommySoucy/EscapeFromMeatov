using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public RectTransform rewardsParent;
        public GameObject rewardsHorizontalParent;
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
            td
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
