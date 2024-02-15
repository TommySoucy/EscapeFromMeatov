using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class AreaSlot : MonoBehaviour
    {
        public Area area;
        public List<string> filter;
        public AreaSlot next;
        public Transform poseOverride;
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
    }
}
