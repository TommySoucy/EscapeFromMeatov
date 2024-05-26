using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairListingUI : MonoBehaviour
    {
        public RagFairListing listing;

        public ItemView itemView;
        public Text itemName;
        public Text amount;
        public Text timeLeftText;

        public void Update()
        {
            timeLeftText.text = "Time Left: " + Mod.FormatTimeString(listing.timeLeft);
        }

        public void SetListing(RagFairListing listing)
        {
            this.listing = listing;

            itemView.SetItemData(listing.itemData, false, false, listing.amount > 1, listing.amount.ToString());
            itemName.text = listing.itemData.name;
            amount.text = listing.price.ToString();
            timeLeftText.text = "Time Left: " + Mod.FormatTimeString(listing.timeLeft);
        }

        public void OnCancelClicked()
        {
            TODO: // Remove listing
            Mod.LogInfo("");
        }
    }
}
