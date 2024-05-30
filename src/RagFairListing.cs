using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class RagFairListing
    {
        public MeatovItemData itemData; // Data of root item
        public JObject serializedItem; // Serialized version of the item so it can be respawned if listing is canceled
        public int price;
        public int stack;
        public float timeLeft;

        // Sell chance params
        public float sellChance;
        public float timeToSellCheck;
        public bool sellChecked;

        public RagFairListingUI UI;

        public bool SellCheck()
        {
            sellChecked = true;
            return UnityEngine.Random.value < sellChance;
        }
    }
}
