using UnityEngine.UI;

namespace EFM
{
    public class PriceItemView : RequirementItemView
    {
        public Text itemName;
        public BarterPrice price;

        public void ResetItemView()
        {
            itemName.text = "No Item";
            price = null;
            if(amount != null)
            {
                amount.text = "0";
            }
            if(fulfilledIcon != null)
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }

            itemView.ResetItemView();
        }
    }
}
