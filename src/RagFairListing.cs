using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class RagFairListing
    {
        public MeatovItemData itemData; // Data of root item
        public JObject serializedItem; // Serialized version of the item so it can be respawned if listing is canceled
        public int amount;
        public int price;
        public float timeLeft;

        public RagFairListingUI UI;
    }
}
