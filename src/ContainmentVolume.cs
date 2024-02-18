using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class ContainmentVolume : MonoBehaviour
    {
        public bool hasMaxVolume;
        public float maxVolume;
        public List<string> whitelist;
        public List<string> blacklist;
        public GameObject staticVolume;
        public GameObject activeVolume;
        public GameObject itemRoot;
    }
}
