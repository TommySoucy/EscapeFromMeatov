using UnityEngine;

namespace EFM
{
    public class ItemIcon : MonoBehaviour, IDescribable
    {
		DescriptionManager descriptionManager;

		public string itemID;
        public string itemName;
        public string description;
        public int weight;
        public int volume;

		public bool isPhysical;
		public bool isCustom;
		public MeatovItem MI;

        public DescriptionPack GetDescriptionPack()
		{
			if (!isPhysical)
			{
				DescriptionPack descriptionPack = new DescriptionPack();
				descriptionPack.ID = itemID;
				descriptionPack.nonPhysDescribable = this;
				descriptionPack.isPhysical = false;
				descriptionPack.name = itemName;
				descriptionPack.description = description;
				// descriptionPack.icon = Mod.itemIcons[ID]; TODO: Since in the case of a non physical describable we dont know if it has an itemIcon entry, need to handle this wehn we set the icon sprite in the description
				descriptionPack.amountRequiredPerArea = new int[22];
				//descriptionPack.amount = (HideoutController.instance.inventory.ContainsKey(itemID) ? HideoutController.instance.inventory[itemID] : 0) + (Mod.playerInventory.ContainsKey(itemID) ? Mod.playerInventory[itemID] : 0);
				Mod.LogInfo("Item " + itemID + " description amount = " + descriptionPack.amount);
				descriptionPack.amountRequired = 0;
				for (int i = 0; i < 22; ++i)
				{
					if (Mod.requiredPerArea[i] != null && Mod.requiredPerArea[i].ContainsKey(itemID))
					{
						descriptionPack.amountRequired += Mod.requiredPerArea[i][itemID];
						descriptionPack.amountRequiredPerArea[i] = Mod.requiredPerArea[i][itemID];
					}
					else
					{
						descriptionPack.amountRequiredPerArea[i] = 0;
					}
				}
				descriptionPack.onWishlist = Mod.wishList.Contains(itemID);
				descriptionPack.insured = false;
				descriptionPack.weight = weight;
				descriptionPack.volume = volume;
				descriptionPack.amountRequiredQuest = Mod.requiredForQuest.ContainsKey(itemID) ? Mod.requiredForQuest[itemID] : 0;

				return descriptionPack;
            }
            else
            {
				return MI.GetDescriptionPack();
            }
		}

		public void SetDescriptionManager(DescriptionManager descriptionManager)
		{
			this.descriptionManager = descriptionManager;
		}
	}
}
