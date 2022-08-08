using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_ExperienceTrigger : MonoBehaviour
    {
        bool triggered = false;
        public void OnTriggerEnter(Collider other)
        {
            Mod.instance.LogInfo("Exploration trigger, on trigger enter: " + other.name);
            if (!triggered && (other.gameObject.layer == 15 || other.gameObject.layer == 9))
            {
                // TODO: Find amount for each zone, for now set to give 100 xp each
                Mod.AddExperience(100, 3);
                Mod.triggeredExplorationTriggers[Mod.chosenMapIndex][transform.GetSiblingIndex()] = true;
                triggered = true;
            }
        }
    }
}
