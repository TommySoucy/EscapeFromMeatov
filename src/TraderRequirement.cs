using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class TraderRequirement : MonoBehaviour
    {
        public Sprite[] traderIcons; // Prapor, Therapist, Fence, Skier, Peacekeeper, Tech, Ragman, Jaeger, Lightkeeper
        public Image traderIcon;
        public GameObject elite;
        public Text rankText;
        public GameObject unfulfilled;
        public GameObject fulfilled;
    }
}
