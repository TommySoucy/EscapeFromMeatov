﻿using UnityEngine;
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
                this.barter.itemData.OnNeededForChanged -= OnNeededForChanged;
            }

            this.barter = barter;
            barter.itemData.OnNeededForChanged += OnNeededForChanged;

            int valueToUse = barter.prices[0].count;
            int currencyToUse = 0;
            if (!Mod.ItemIDToCurrencyIndex(barter.prices[0].itemData.H3ID, out currencyToUse))
            {
                currencyToUse = 3;
            }
            itemView.SetItemData(barter.itemData, false, false, false, null, true, currencyToUse, valueToUse, false, false);
            itemName.text = barter.itemData.name;
            wishlistStar.color = barter.itemData.onWishlist ? Color.yellow : Color.black;
        }

        public void OnWishlistClicked()
        {
            // Note that we don't set star color here, it will be changed through OnNeededForChanged
            barter.itemData.onWishlist = !barter.itemData.onWishlist;
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
                wishlistStar.color = barter.itemData.onWishlist ? Color.yellow : Color.black;
            }
        }

        public void OnDestroy()
        {
            if (barter != null)
            {
                barter.itemData.OnNeededForChanged -= OnNeededForChanged;
            }
        }
    }
}
