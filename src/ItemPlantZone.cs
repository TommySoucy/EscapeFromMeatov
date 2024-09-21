using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class ItemPlantZone : MonoBehaviour
    {
        TODO: // Add to raid dev
        public string locationID;
        public string itemID;
        public float plantTime;

        public void OnTriggerEnter(Collider other)
        {
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && !Mod.charChoicePMC)
            {
                return;
            }

            MeatovItem meatovItem = other.GetComponentInParent<MeatovItem>();
            if (meatovItem != null && meatovItem.tarkovID.Equals(itemID))
            {
                meatovItem.currentPlantZone = this;
            }
        }

        public void OnTriggerExit(Collider other)
        {
            // Skip if in scav raid
            if (Mod.currentLocationIndex == 2 && !Mod.charChoicePMC)
            {
                return;
            }

            MeatovItem meatovItem = other.GetComponentInParent<MeatovItem>();
            if (meatovItem != null && meatovItem.tarkovID.Equals(itemID) && meatovItem.currentPlantZone == this)
            {
                meatovItem.currentPlantZone = null;
            }
        }
    }
}
