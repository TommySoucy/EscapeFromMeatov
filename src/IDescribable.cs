namespace EFM
{
    public interface IDescribable
    {
        DescriptionPack GetDescriptionPack();

        void SetDescriptionManager(DescriptionManager descriptionManager);
    }

    public class DescriptionPack
    {
        public MeatovItem item;
        public MeatovItemData itemData;

        // Overrides
        public bool hasInsuredOverride = false;
        public bool insuredOverride = false;
        public bool hasCountOverride = false;
        public string countOverride = null;
        public bool hasValueOverride = false;
        public int currencyIconIndexOverride = 0;
        public int valueOverride = 0;
        public bool hasToolOveride = false;
        public bool isToolOverride = false;
    }
}
