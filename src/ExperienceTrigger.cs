using UnityEngine;

namespace EFM
{
    public class ExperienceTrigger : MonoBehaviour
    {
        bool triggered = false;
        public void OnTriggerEnter(Collider other)
        {
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && !Mod.charChoicePMC)
            {
                return;
            }

            Mod.LogInfo("Exploration trigger, on trigger enter: " + other.name);
            if (!triggered && (other.gameObject.layer == 15 || other.gameObject.layer == 9))
            {
                // TODO: Find amount for each zone, for now set to give 100 xp each
                Mod.AddExperience(100, 3);
                //Mod.triggeredExplorationTriggers[Mod.mapChoiceName][transform.GetSiblingIndex()] = true;
                triggered = true;
            }
        }
    }
}
