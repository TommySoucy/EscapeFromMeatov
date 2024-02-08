using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class Bonus : MonoBehaviour
    {
        public Sprite[] bonusIcons; // Exp, bitcoin, createItemGeneric, createItemMed, fuel slots, scav item, shooting range, unlocked, GPUslots
        public Image bonusIcon;
        public Text description;
        public Text effect;
    }
}
