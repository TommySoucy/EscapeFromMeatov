using System;
using UnityEngine;

namespace EFM
{
    public class RagFairSellItemView : MonoBehaviour
    {
        [NonSerialized]
        public MeatovItem item;

        public ItemView itemView;

        public void SetItemData(MeatovItem item)
        {
            this.item = item;

            itemView.SetItem(item);
        }

        public void OnClicked()
        {
            TODO: // Set selected itemview, set For itemview, set sell chance
            Mod.LogInfo("");
        }
    }
}
