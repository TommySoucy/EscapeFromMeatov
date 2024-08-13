using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairBuyItemView : MonoBehaviour
    {
        public Barter barter;

        public ItemView itemView;
        public Text itemName;
        public Image wishlistStar;

        public void SetBarter(Barter barter)
        {
            if(this.barter != null)
            {
                this.barter.itemData[0].OnNeededForChanged -= OnNeededForChanged;
            }

            this.barter = barter;
            barter.itemData[0].OnNeededForChanged += OnNeededForChanged;

            if (barter.itemData == null)
            {
                Mod.LogWarning("Ragfair was adding a barter with missing item data");
                Destroy(gameObject);
                return;
            }

            int priceCount = 0;
            int firstValidPrice = -1;
            for (int k = 0; k < barter.prices.Length; ++k)
            {
                if (barter.prices[k].itemData != null)
                {
                    ++priceCount;
                    if (firstValidPrice == -1)
                    {
                        firstValidPrice = k;
                    }
                }
            }
            if (priceCount == 0)
            {
                Mod.LogWarning("Ragfair was adding a barter for item "+barter.itemData[0].name+" with all prices missing item data");
                Destroy(gameObject);
                return;
            }

            int valueToUse = 0;
            int currencyToUse = 0;
            for (int k = 0; k < barter.prices.Length; ++k)
            {
                valueToUse += barter.prices[k].count;
            }
            if (barter.prices.Length > 1)
            {
                currencyToUse = 3; // Item trade icon
            }
            else
            {
                if (!Mod.ItemIDToCurrencyIndex(barter.prices[firstValidPrice].itemData.tarkovID, out currencyToUse))
                {
                    currencyToUse = 3;
                }
            }
            itemView.SetItemData(barter.itemData[0], false, false, false, null, true, currencyToUse, valueToUse, false, false);
            itemName.text = barter.itemData[0].name;
            wishlistStar.color = barter.itemData[0].onWishlist ? Color.yellow : Color.black;
        }

        public void OnWishlistClicked()
        {
            // Note that we don't set star color here, it will be changed through OnNeededForChanged
            barter.itemData[0].onWishlist = !barter.itemData[0].onWishlist;
        }

        public void OnBuyClicked()
        {
            if(barter != null)
            {
                HideoutController.instance.marketManager.SetRagFairBuy(barter);
            }
        }

        public void OnNeededForChanged(int index)
        {
            if(index == 2)
            {
                wishlistStar.color = barter.itemData[0].onWishlist ? Color.yellow : Color.black;
            }
        }

        public void OnDestroy()
        {
            if (barter != null)
            {
                barter.itemData[0].OnNeededForChanged -= OnNeededForChanged;
            }
        }
    }
}
