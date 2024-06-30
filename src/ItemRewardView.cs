using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemRewardView : MonoBehaviour
    {
        public MeatovItemData item;

        public ItemView itemView;
        public Text count;
        public Text itemName;
        public GameObject unlockIcon;

        public void SetItem(MeatovItemData item, bool foundInRaid)
        {
            this.item = item;

            itemView.SetItemData(item, hasFIROverride: foundInRaid, isFIROverride: foundInRaid);
        }
    }
}
