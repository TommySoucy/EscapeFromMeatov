using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairWishlistItemView : MonoBehaviour
    {
        public MeatovItemData itemData;

        public ItemView itemView;
        public Text itemName;

        public void SetItemData(MeatovItemData itemData)
        {
            if (this.itemData != null)
            {
                this.itemData.OnNeededForChanged -= OnNeededForChanged;
            }

            this.itemData = itemData;
            itemData.OnNeededForChanged += OnNeededForChanged;

            itemView.SetItemData(itemData);
            itemName.text = itemData.name;
        }

        public void OnWishlistClicked()
        {
            // Note that we don't destroy this element when removed from wishlist, it will be done through OnNeededForChanged
            itemData.onWishlist = !itemData.onWishlist;
        }

        public void OnNeededForChanged(int index)
        {
            if (index == 2 && !itemData.onWishlist)
            {
                Destroy(gameObject);
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
