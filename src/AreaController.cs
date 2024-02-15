using UnityEngine;

namespace EFM
{
    public class AreaController : MonoBehaviour
    {
        public Area[] areas;

        public void TogglePower()
        {
            for (int i = 0; i < areas.Length; ++i)
            {
                areas[i].powered = !areas[i].powered;
            }
        }
    }
}
