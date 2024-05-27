using System;
using UnityEngine;

namespace EFM
{
    public class RagFairSellItemView : MonoBehaviour
    {
        [NonSerialized]
        public MeatovItem item;

        public ItemView itemView;

        public void SetItem(MeatovItem item, int actualValue)
        {
            this.item = item;

            itemView.SetItem(item, true, 0, actualValue);
        }

        public void OnClicked()
        {
            TODO: // Set selected itemview, set For itemview, set sell chance
            Mod.LogInfo("");
        }
    }
}
