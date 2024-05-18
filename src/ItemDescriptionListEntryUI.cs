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
            entryName.text = owner.descriptionPack.itemData.name;
            entryInfo.text = task.trader.name;
        }

        public void SetAreaLevel(ItemDescriptionUI owner, int areaIndex, int level, long currentCount, int neededCount)
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
            entryName.text = owner.descriptionPack.itemData.name;
            entryInfo.text = Area.IndexToName(areaIndex)+" lvl "+ level;
        }
    }
}
