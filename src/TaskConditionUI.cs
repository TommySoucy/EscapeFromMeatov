using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class TaskConditionUI : MonoBehaviour
    {
        public Text text;
        public GameObject progressBar;
        public RectTransform barFill;
        public Text counter;
        public GameObject doneIcon;
        public GameObject turnInButton;

        public void OnTurnInClicked()
        {

        }

        public void SetCondition(Condition condition)
        {
            text.text = condition.description;
            // Progress counter, only necessary if value > 1 and for specific condition types
            if (condition.value > 1)
            {
                switch (condition.conditionType)
                {
                    case Condition.ConditionType.CounterCreator:
                        foreach (ConditionCounter counter in condition.counters)
                        {
                            if (counter.counterCreatorConditionType == ConditionCounter.CounterCreatorConditionType.Kills)
                            {
                                progressBar.SetActive(true);
                                this.counter.gameObject.SetActive(true);
                                this.counter.text = "0/" + condition.value; // Activate progress counter
                                break;
                            }
                        }
                        break;
                    case Condition.ConditionType.HandoverItem:
                    case Condition.ConditionType.FindItem:
                    case Condition.ConditionType.LeaveItemAtLocation:
                        progressBar.SetActive(true);
                        counter.gameObject.SetActive(true);
                        counter.text = "0/" + condition.value; // Activate progress counter
                        break;
                    default:
                        break;
                }
            }

            // Setup handover button if necessary
            bool needHandOverButton = false;
            if (condition.conditionType == Condition.ConditionType.HandoverItem)
            {
                foreach (MeatovItemData item in condition.targetItems)
                {
                    if ((condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.FIRInventory.ContainsKey(item.H3ID))
                        || (!condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.inventory.ContainsKey(item.H3ID)))
                    {
                        needHandOverButton = true;
                        break;
                    }
                }
            }
            else if (condition.conditionType == Condition.ConditionType.WeaponAssembly)
            {
                foreach (MeatovItemData item in condition.targetItems)
                {
                    List<MeatovItem> itemList;
                    if ((condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.FIRInventoryItems.TryGetValue(item.H3ID, out itemList))
                        || (!condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.inventoryItems.TryGetValue(item.H3ID, out itemList)))
                    {
                        for(int i=0; i < itemList.Count; ++i)
                        {
                            if (condition.WeaponAssemblyItemMatches(itemList[i]))
                            {
                                needHandOverButton = true;
                                break;
                            }
                        }
                    }
                }
            }

            turnInButton.SetActive(needHandOverButton);

            // Disable condition gameObject if visibility conditions not met
            if (condition.visibilityConditions != null && condition.visibilityConditions.Count > 0)
            {
                foreach (VisibilityCondition visibilityCondition in condition.visibilityConditions)
                {
                    if (!visibilityCondition.fulfilled)
                    {
                        gameObject.SetActive(false);
                        break;
                    }
                }
            }
        }

        public void OnConditionHandOverClick(Condition condition, GameObject handOverButton, bool FIR)
        {
            int totalLeft = condition.value;
            foreach (string item in condition.items)
            {
                int actualAmount = Mathf.Min(totalLeft - condition.itemCount, tradeVolumeFIRInventory[item]);
                RemoveItemFromTrade(item, actualAmount, condition.dogtagLevel, FIR);
                condition.itemCount += actualAmount;
                totalLeft -= actualAmount;

                if (totalLeft == 0)
                {
                    break;
                }
            }

            Trader.UpdateConditionFulfillment(condition);

            handOverButton.SetActive(false);
        }
    }
}
