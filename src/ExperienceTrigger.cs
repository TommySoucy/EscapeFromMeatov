using System;
using UnityEngine;

namespace EFM
{
    public class ExperienceTrigger : MonoBehaviour
    {
        TODO: // Add to raid dev
        public string ID;
        public int experienceAward;
        public bool PMCOnly;
        public string quest; // Set only if only want trigger to be active while this quest is active

        [NonSerialized]
        public bool triggered = false;

        public void Awake()
        {
            if(quest != null && !quest.Equals("") && (!Task.allTasks.TryGetValue(quest, out Task task) || task.taskState != Task.TaskState.Active))
            {
                Destroy(gameObject);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && !Mod.charChoicePMC)
            {
                return;
            }

            Mod.LogInfo("Exploration trigger, on trigger enter: " + other.name);
            if (!Mod.triggeredExperienceTriggers.Contains(ID) && (other.gameObject.layer == 15 || other.gameObject.layer == 9) && (!PMCOnly || Mod.charChoicePMC))
            {
                Mod.AddExperience(experienceAward, 3);
                Mod.triggeredExperienceTriggers.Add(ID);
                Mod.OnPlaceVisitedInvoke(ID);
            }
        }
    }
}
