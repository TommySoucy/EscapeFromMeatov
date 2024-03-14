using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class AreaSlot : FVRQuickBeltSlot
    {
        public Area area;
        public List<string> filter;
        public AreaSlot next;
        public PoseOverridePair[] poseOverridePerItem;
        public GameObject staticVolume;
        public GameObject activeVolume;

        [NonSerialized]
        public MeatovItem item;

        [Serializable]
        public class PoseOverridePair
        {
            public string item;
            public Transform poseOverride;
        }

        public void UpdatePose()
        {
            if(item != null && poseOverridePerItem != null)
            {
                for(int i=0; i < poseOverridePerItem.Length; ++i)
                {
                    if (poseOverridePerItem[i].item.Equals(item.H3ID))
                    {
                        PoseOverride = poseOverridePerItem[i].poseOverride;
                        break;
                    }
                }
            }
        }
    }
}
