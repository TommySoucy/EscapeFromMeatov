using System.Collections.Generic;

namespace EFM
{
    public class CategoryTreeNode
    {
        public CategoryTreeNode parent;
        public List<CategoryTreeNode> children;
        public List<Barter> barters;

        public RagFairCategory UI;
        public bool uncollapsed;

        public string ID;
        public string name;

        public CategoryTreeNode(CategoryTreeNode parent, string ID, string name)
        {
            this.parent = parent;
            children = new List<CategoryTreeNode>();
            if(parent != null)
            {
                parent.children.Add(this);
            }

            this.ID = ID;
            this.name = name;

            barters = new List<Barter>();
            Mod.GetItemData("203", out MeatovItemData roubleData);
            if (Mod.itemsByParents.TryGetValue(ID, out List<MeatovItemData> items))
            {
                for (int i = 0; i < items.Count; ++i)
                {
                    bool added = false;

                    // Find trader barters for this item
                    for (int j = 0; j < Mod.traders.Length; ++j)
                    {
                        if (Mod.traders[j].bartersByItemID.TryGetValue(items[i].H3ID, out List<Barter> traderBarters))
                        {
                            for (int k = 0; k < traderBarters.Count; ++k)
                            {
                                barters.Add(traderBarters[k]);
                                added = true;
                            }
                        }
                    }
                    // Only add a barter if there aren't any trader barters and if the item canSellOnRagFair
                    if (!added && items[i].canSellOnRagfair)
                    {
                        Barter barter = new Barter();
                        barter.itemData = items[i];
                        barter.prices = new BarterPrice[1];
                        barter.prices[0] = new BarterPrice();
                        barter.prices[0].itemData = roubleData;
                        barter.prices[0].count = (int)(items[i].value * MarketManager.RAGFAIR_PRICE_MULT);
                        barters.Add(barter);
                    }
                }
            }
            else
            {
                Mod.LogError("CategoryTreeNode " + ID + " was not found as parent in items by parent");
            }
        }

        public CategoryTreeNode FindChild(string ID)
        {
            if (ID.Equals(this.ID))
            {
                return this;
            }
            else
            {
                for(int i=0; i < children.Count; ++i)
                {
                    CategoryTreeNode child = children[i].FindChild(ID);
                    if(child != null)
                    {
                        return child;
                    }
                }
            }

            return null;
        }
    }
}
