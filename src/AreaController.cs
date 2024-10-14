using UnityEngine;

namespace EFM
{
    public class AreaController : MonoBehaviour
    {
        public Area[] areas;
        public Sprite[] areaIcons;

        public void TogglePower()
        {
            for (int i = 0; i < areas.Length; ++i)
            {
                if (areas[i] != null)
                {
                    areas[i].powered = !areas[i].powered;
                }
            }
        }
    }
}
