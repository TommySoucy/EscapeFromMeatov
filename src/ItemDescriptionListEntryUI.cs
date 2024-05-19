using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class ItemDescriptionListEntryUI : MonoBehaviour
    {
        public ItemDescriptionUI owner;

        public GameObject fulfilledIcon;
        public GameObject unfulfilledIcon;
        public Text amount;
        public Text entryName;
        public Text entryInfo;

        public void OnFillClicked()
        {

        }

        public void SetTask(ItemDescriptionUI owner, Task task, long currentCount, int neededCount)
        {
            // Note that an entry like this one does not subscribe to events to update its amount in realtime
            // We consider item descriptions to be shortlived and as such, don't need to be updated
            // Up to date numbers will simply be displayed when we make a new description of the item
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = task.name;
            entryName.color = Mod.neededForColors[0];
            entryInfo.text = task.trader.name;
        }

        public void SetAreaLevel(ItemDescriptionUI owner, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
                entryName.color = Mod.neededForAreaFulfilledColor;
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
                entryName.color = Mod.neededForColors[1];
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = Area.IndexToName(areaIndex) + " lvl " + level;
            entryInfo.gameObject.SetActive(false);
        }

        public void SetBarter(ItemDescriptionUI owner, Barter barter, int traderIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            entryName.text = barter.itemData.name;
            entryName.color = Mod.neededForColors[3];
            entryInfo.text = Mod.traders[traderIndex].name + " lvl " + level;
        }

        public void SetProduction(ItemDescriptionUI owner, Production production, int areaIndex, int level, long currentCount, int neededCount)
        {
            this.owner = owner;
            if(currentCount >= neededCount)
            {
                fulfilledIcon.SetActive(true);
                unfulfilledIcon.SetActive(false);
            }
            else
            {
                fulfilledIcon.SetActive(false);
                unfulfilledIcon.SetActive(true);
            }
            amount.text = currentCount.ToString() + "/" + neededCount;
            if(Mod.GetItemData(production.endProduct, out MeatovItemData itemData))
            {
                entryName.text = itemData.name;
            }
            entryName.color = Mod.neededForColors[4];
            entryInfo.text = Area.IndexToName(areaIndex) + " lvl " + level;
        }
    }
}
