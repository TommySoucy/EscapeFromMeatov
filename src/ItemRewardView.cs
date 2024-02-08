using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemRewardView : MonoBehaviour, IDescribable
    {
        public MeatovItem MIW;

        public Image icon;
        public Text count;
        public Text itemName;
        public GameObject unlockIcon;

        public void SetItem(MeatovItem MIW)
        {
            this.MIW = MIW;
        }

        public DescriptionPack GetDescriptionPack()
        {
            return MIW.GetDescriptionPack();
        }

        public void SetDescriptionManager(DescriptionManager descriptionManager)
        {
            // TODO
        }
    }
}
