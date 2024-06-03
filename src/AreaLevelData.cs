using UnityEngine;

namespace EFM
{
    public class AreaLevelData : MonoBehaviour
    {
        public AreaUpgradeCheckProcessor[] areaUpgradeCheckProcessors; // 0: Block, 1: Warning
        public AreaVolume[] areaVolumes; // If this area has output and the output is a volume, element 0 must be the output volume
        public AreaSlot[] areaSlots; // If this area has output and the output is a slot, element 0 must be the output slot
        public GameObject[] objectsToToggle;
        public Vector2[] workingRanges;
        public AudioClip[] mainAudioClips;
        public AudioSource[] mainAudioSources;
    }
}
