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
        public GameObject activeStatus;
        public GameObject completeStatus;
        public Text percentage;
        public RectTransform barFill;
        public GameObject full;
        public Text description;
        public RectTransform objectivesParent;
        public GameObject objectivePrefab;
        public RectTransform rewardsParent;
        public GameObject rewardsHorizontalParent;
        public GameObject itemRewardPrefab;
        public GameObject statRewardPrefab;
        public GameObject unknownRewardPrefab;
        public GameObject traderRewardPrefab;

        public void SetTask(Task task)
        {
            this.task = task;


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
