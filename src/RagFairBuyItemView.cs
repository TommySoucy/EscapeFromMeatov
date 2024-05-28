using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairBuyItemView : MonoBehaviour
    {
        public MeatovItemData itemData;

        public ItemView itemView;
        public Text itemName;
        public Image wishlistStar;

        public void SetItemData(MeatovItemData itemData)
        {
            if(this.itemData != null)
            {
                this.itemData.OnNeededForChanged -= OnNeededForChanged;
            }

            this.itemData = itemData;
            itemData.OnNeededForChanged += OnNeededForChanged;

            itemView.SetItemData(itemData);
            itemName.text = itemData.name;
            wishlistStar.color = itemData.onWishlist ? Color.yellow : Color.black;
        }

        public void OnWishlistClicked()
        {
            // Note that we don't set star color here, it will be changed through OnNeededForChanged
            itemData.onWishlist = !itemData.onWishlist;
        }

        public void OnBuyClicked()
        {
            cont from here // go through entre buy process and implement stuff as we go like we have them for buying from trader
            TODO: // Open cart, set ui, and set prices in market
            Mod.LogInfo("");
        }

        public void OnNeededForChanged(int index)
        {
            if(index == 2)
            {
                wishlistStar.color = itemData.onWishlist ? Color.yellow : Color.black;
            }
        }

        public void OnDestroy()
        {
            if (itemData != null)
            {
                itemData.OnNeededForChanged -= OnNeededForChanged;
            }
        }
    }
}
