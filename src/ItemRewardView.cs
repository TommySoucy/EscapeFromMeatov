using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemRewardView : MonoBehaviour, IDescribable
    {
        public MeatovItem item;

        public Image icon;
        public Text count;
        public Text itemName;
        public GameObject unlockIcon;

        public void SetItem(MeatovItem item)
        {
            this.item = item;
        }

        public DescriptionPack GetDescriptionPack()
        {
            return item.GetDescriptionPack();
        }

        public void SetDescriptionManager(DescriptionManager descriptionManager)
        {
            // TODO
        }
    }
}
