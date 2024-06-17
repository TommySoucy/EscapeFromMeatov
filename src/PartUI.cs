using UnityEngine;

namespace EFM
{
    public class PartUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject lightBleed;
        private int lightBleedCount;
        [SerializeField]
        private GameObject heavyBleed;
        private int heavyBleedCount;
        [SerializeField]
        private GameObject pain;
        private int painCount;
        [SerializeField]
        private GameObject fracture;
        private int fractureCount;

        public void AddLightBleed()
        {
            ++lightBleedCount;
            lightBleed.SetActive(true);
        }

        public void RemoveLightBleed()
        {
            --lightBleedCount;
            if(lightBleedCount == 0)
            {
                lightBleed.SetActive(false);
            }
        }

        public void AddHeavyBleed()
        {
            ++heavyBleedCount;
            heavyBleed.SetActive(true);
        }

        public void RemoveHeavyBleed()
        {
            --heavyBleedCount;
            if(heavyBleedCount == 0)
            {
                heavyBleed.SetActive(false);
            }
        }

        public void AddPain()
        {
            ++painCount;
            pain.SetActive(true);
        }

        public void RemovePain()
        {
            --painCount;
            if(painCount == 0)
            {
                pain.SetActive(false);
            }
        }

        public void AddFracture()
        {
            ++fractureCount;
            fracture.SetActive(true);
        }

        public void RemoveFracture()
        {
            --fractureCount;
            if(fractureCount == 0)
            {
                fracture.SetActive(false);
            }
        }
    }
}
