using UnityEngine;

namespace EFM
{
    public class StaminaUI : MonoBehaviour
    {
        public static StaminaUI instance;

        public RectTransform barFill;

        public void Awake()
        {
            instance = this;
        }
    }
}
