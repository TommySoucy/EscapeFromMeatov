using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class TaskConditionUI : MonoBehaviour
    {
        public Condition condition;
        public Text text;
        public GameObject progressBar;
        public RectTransform barFill;
        public Text counter;
        public GameObject doneIcon;
        public GameObject turnInButton;

        private bool conditionSet;

        public void OnTurnInClicked()
        {
            int totalLeft = condition.value - condition.count;

            if(totalLeft <= 0)
            {
                Mod.LogError("DEV: Clicked on Turn In button for task " + condition.task.ID + " condition " + condition.ID + " but total left to turn in = " + totalLeft);
                turnInButton.SetActive(false);
                return;
            }

            // Go through each target item in order and consume the first valid instances in the trade volume
            if (condition.conditionType == Condition.ConditionType.HandoverItem)
            {
                foreach (MeatovItemData item in condition.targetItems)
                {
                    if(totalLeft <= 0)
                    {
                        break;
                    }

                    List<MeatovItem> itemList;
                    if ((condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.FIRInventoryItems.TryGetValue(item.H3ID, out itemList))
                        ||(!condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.inventoryItems.TryGetValue(item.H3ID, out itemList)))
                    {
                        // Consume items in the list
                        for(int i = 0; i < itemList.Count; ++i)
                        {
                            if (condition.conditionType == Condition.ConditionType.HandoverItem 
                                || (condition.conditionType == Condition.ConditionType.WeaponAssembly && condition.WeaponAssemblyItemMatches(itemList[i])))
                            {
                                // Consume
                                if (itemList[i].stack > totalLeft)
                                {
                                    totalLeft = 0;
                                    itemList[i].stack -= totalLeft;
                                    condition.count += totalLeft;
                                    break;
                                }
                                else if (itemList[i].stack < totalLeft)
                                {
                                    totalLeft -= itemList[i].stack;
                                    condition.count += itemList[i].stack;
                                    Destroy(itemList[i].gameObject);
                                }
                                else // item.stack == amountToRemove
                                {
                                    totalLeft = 0;
                                    condition.count += totalLeft;
                                    Destroy(itemList[i].gameObject);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            turnInButton.SetActive(false);

            condition.UpdateFulfillment();
        }

        public void SetCondition(Condition condition)
        {
            if (this.condition != null)
            {
                this.condition.OnConditionFulfillmentChanged -= OnConditionFulfillmentChanged;
                this.condition.OnConditionProgressChanged -= OnConditionProgressChanged;
            }

            this.condition = condition;

            this.condition.OnConditionFulfillmentChanged += OnConditionFulfillmentChanged;
            this.condition.OnConditionProgressChanged += OnConditionProgressChanged;

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
            UpdateTurnInButton();

            // Subscribe to necessary events
            HideoutController.instance.marketManager.tradeVolume.OnItemAdded += OnTradeVolumeItemsChanged;
            HideoutController.instance.marketManager.tradeVolume.OnItemRemoved += OnTradeVolumeItemsChanged;

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

            conditionSet = true;

            // Init state UI
            OnConditionFulfillmentChanged(this.condition);
            OnConditionProgressChanged(this.condition);
        }

        public void OnTradeVolumeItemsChanged(MeatovItem item)
        {
            if(Mod.IDDescribedInList(item.H3ID, item.parents, condition.targetItemIDs, null))
            {
                UpdateTurnInButton();
            }
        }

        public void UpdateTurnInButton()
        {
            bool needHandOverButton = false;
            int totalLeft = condition.value - condition.count;
            if (condition.conditionType == Condition.ConditionType.HandoverItem && totalLeft > 0)
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
            else if (condition.conditionType == Condition.ConditionType.WeaponAssembly && totalLeft > 0)
            {
                foreach (MeatovItemData item in condition.targetItems)
                {
                    List<MeatovItem> itemList;
                    if ((condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.FIRInventoryItems.TryGetValue(item.H3ID, out itemList))
                        || (!condition.onlyFoundInRaid && HideoutController.instance.marketManager.tradeVolume.inventoryItems.TryGetValue(item.H3ID, out itemList)))
                    {
                        for (int i = 0; i < itemList.Count; ++i)
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
        }

        public void OnConditionFulfillmentChanged(Condition condition)
        {
            doneIcon.SetActive(condition.fulfilled);
        }

        public void OnConditionProgressChanged(Condition condition)
        {
            if (condition.fulfilled || condition.value <= 1)
            {
                progressBar.SetActive(false);
                counter.gameObject.SetActive(false);
            }
            else // !condition.fulfilled && condition.value > 1
            {
                progressBar.SetActive(true);
                barFill.sizeDelta = new Vector2(60.0f * ((float)condition.count / (float)condition.value), 6);
                counter.gameObject.SetActive(true);
                counter.text = condition.count.ToString() + "/" + condition.value;
            }
        }

        public void OnDestroy()
        {
            if (condition != null)
            {
                condition.OnConditionFulfillmentChanged -= OnConditionFulfillmentChanged;
                condition.OnConditionProgressChanged -= OnConditionProgressChanged;
            }

            if (conditionSet && HideoutController.instance != null)
            {
                HideoutController.instance.marketManager.tradeVolume.OnItemAdded -= OnTradeVolumeItemsChanged;
                HideoutController.instance.marketManager.tradeVolume.OnItemRemoved -= OnTradeVolumeItemsChanged;
            }
        }
    }
}
