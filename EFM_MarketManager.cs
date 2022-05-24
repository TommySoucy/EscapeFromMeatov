using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class EFM_MarketManager : MonoBehaviour
    {
        public bool init;

        public EFM_Base_Manager baseManager;

        public EFM_TradeVolume tradeVolume;
        public AudioSource clickAudio;

        public List<TraderTask> referencedTasks;
        public List<TraderTaskCondition> referencedTaskConditions;

        public void Init(EFM_Base_Manager baseManager)
        {
            this.baseManager = baseManager;

            // Setup the trade volume
            tradeVolume = transform.GetChild(1).gameObject.AddComponent<EFM_TradeVolume>();
            tradeVolume.mainContainerRenderer = tradeVolume.GetComponent<Renderer>();
            tradeVolume.mainContainerRenderer.material = Mod.quickSlotConstantMaterial;
            tradeVolume.market = this;

            InitUI();

            init = true;
        }

        public void InitUI()
        {
            // Setup buttons
            clickAudio = transform.GetChild(3).GetComponent<AudioSource>();
            Transform traderButtonsParent = transform.GetChild(0).GetChild(0).GetChild(0);
            for (int i = 0; i < traderButtonsParent.childCount; ++i) 
            {
                EFM_PointableButton pointableButton = traderButtonsParent.GetChild(i).gameObject.AddComponent<EFM_PointableButton>();

                pointableButton.SetButton();
                pointableButton.Button.onClick.AddListener(() => { SetTrader(i); });
                pointableButton.MaxPointingRange = 20;
                pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            }

            // Set default trader
            SetTrader(0);
        }

        public void SetTrader(int index)
        {
            EFM_TraderStatus trader = Mod.traderStatuses[index];
            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
            EFM_TraderStatus.TraderLoyaltyDetails loyaltyDetails = trader.GetLoyaltyDetails();
            Transform tradeVolume = transform.GetChild(1);

            // Top
            traderDisplay.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[index];
            if(loyaltyDetails.currentLevel < 4)
            {
                // Trader details
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.LoyaltyLevelToRoman(loyaltyDetails.currentLevel);

                // Current Loyalty
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(false);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.LoyaltyLevelToRoman(loyaltyDetails.currentLevel);
            }
            else
            {
                // Trader details
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);

                // Current Loyalty
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(1).gameObject.SetActive(false);
            }
            traderDisplay.GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = string.Format("{0:0.00}", trader.standing);
            // TODO: Set total amount of money the trader has, here we just disable the number for now because we dont use it
            traderDisplay.GetChild(0).GetChild(1).GetChild(2).GetChild(1).gameObject.SetActive(false);

            // Player level
            traderDisplay.GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
            traderDisplay.GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();

            // Other loyalty details
            // Current loyalty
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(2).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetComponent<Text>().text = trader.standing.ToString();
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(4).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.GetMoneyString(trader.salesSum);

            // Next loyalty
            if (loyaltyDetails.currentLevel == loyaltyDetails.nextLevel)
            {
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).gameObject.SetActive(false);
            }
            else
            {
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).gameObject.SetActive(true);

                if (loyaltyDetails.nextLevel < 4)
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(false);
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(true);
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.LoyaltyLevelToRoman(loyaltyDetails.nextLevel);
                }
                else
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(false);
                }
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().text = loyaltyDetails.nextMinLevel.ToString();
                if(Mod.level >= loyaltyDetails.nextMinLevel)
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().color = new Color(86, 193, 221);
                }
                else
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).GetChild(1).GetComponent<Text>().color = new Color(176, 0, 0);
                }
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().text = loyaltyDetails.nextMinStanding.ToString();
                if (trader.standing >= loyaltyDetails.nextMinStanding)
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().color = new Color(86, 193, 221);
                }
                else
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).GetChild(1).GetComponent<Text>().color = new Color(176, 0, 0);
                }
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.GetMoneyString(loyaltyDetails.nextMinSalesSum);
                if (trader.salesSum >= loyaltyDetails.nextMinSalesSum)
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().color = new Color(86, 193, 221);
                }
                else
                {
                    traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(4).GetChild(1).GetComponent<Text>().color = new Color(176, 0, 0);
                }
            }

            // Main
            // Buy
            bool setDefaultBuy = false;
            List<Transform> currentBuyHorizontals = new List<Transform>();
            Transform buyHorizontalsParent = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject buyHorizontalCopy = buyHorizontalsParent.GetChild(0).gameObject;
            // Clear previous horizontals
            while (buyHorizontalsParent.childCount > 1)
            {
                Destroy(buyHorizontalsParent.GetChild(1));
            }
            if (trader.standing >= 0)
            {
                // Add all assort items to showcase
                for (int i = 1; i <= loyaltyDetails.currentLevel; ++i)
                {
                    TraderAssortment assort = trader.assortmentByLevel[i];

                    foreach (KeyValuePair<string, AssortmentItem> item in assort.itemsByID)
                    {
                        // Skip if this item must be unlocked
                        if (trader.itemsToWaitForUnlock.Contains(item.Key))
                        {
                            continue;
                        }

                        if (item.Value.currentShowcaseElements == null)
                        {
                            item.Value.currentShowcaseElements = new List<GameObject>();
                        }
                        else
                        {
                            item.Value.currentShowcaseElements.Clear();
                        }
                        // Add a new item entry, in a new horizontal if necessary
                        foreach (Dictionary<string, int> priceList in item.Value.prices)
                        {
                            Transform currentHorizontal = currentBuyHorizontals[currentBuyHorizontals.Count - 1];
                            if (currentBuyHorizontals[currentBuyHorizontals.Count - 1].childCount == 7)
                            {
                                currentHorizontal = GameObject.Instantiate(buyHorizontalCopy, buyHorizontalsParent).transform;
                            }

                            Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                            currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[item.Key];
                            if (item.Value.stack >= 50000)
                            {
                                currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "A LOT";
                            }
                            else
                            {
                                currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = item.Value.stack.ToString();
                            }

                            // Write price to item icon and set correct currency icon
                            int totalPriceCount = 0;
                            Sprite currencySprite = null;
                            bool barterSprite = false;
                            foreach (KeyValuePair<string, int> currentPrice in priceList)
                            {
                                totalPriceCount += currentPrice.Value;
                                if (!barterSprite)
                                {
                                    if (currentPrice.Key.Equals("201"))
                                    {
                                        currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                                    }
                                    else if (currentPrice.Key.Equals("202"))
                                    {
                                        currencySprite = EFM_Base_Manager.euroCurrencySprite;
                                    }
                                    else if (currentPrice.Key.Equals("203"))
                                    {
                                        currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                                    }
                                    else
                                    {
                                        currencySprite = EFM_Base_Manager.barterSprite;
                                        barterSprite = true;
                                    }
                                }
                            }
                            currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                            currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = totalPriceCount.ToString();

                            // Setup button
                            EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();

                            pointableButton.SetButton();
                            pointableButton.Button.onClick.AddListener(() => { OnBuyItemClick(item.Value, priceList); });
                            pointableButton.MaxPointingRange = 20;
                            pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                            // Add the icon object to the list for that item
                            item.Value.currentShowcaseElements.Add(currentItemIcon.gameObject);

                            if (!setDefaultBuy)
                            {
                                OnBuyItemClick(item.Value, priceList);
                            }
                        }
                    }
                }
                // Setup buttons
                EFM_PointableButton pointableBuyDealButton = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
                pointableBuyDealButton.SetButton();
                pointableBuyDealButton.Button.onClick.AddListener(() => { OnBuyDealClick(); });
                pointableBuyDealButton.MaxPointingRange = 20;
                pointableBuyDealButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                EFM_PointableButton pointableBuyAmountButton = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
                pointableBuyAmountButton.SetButton();
                pointableBuyAmountButton.Button.onClick.AddListener(() => { OnBuyAmountClick(); });
                pointableBuyAmountButton.MaxPointingRange = 20;
                pointableBuyAmountButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            }

            // Sell
            List<Transform> currentSellHorizontals = new List<Transform>();
            Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;
            // Clear previous horizontals
            while (sellHorizontalsParent.childCount > 1)
            {
                Destroy(sellHorizontalsParent.GetChild(1));
            }
            // Add all items in trade volume that are sellable at this trader to showcase
            foreach (Transform itemTransform in tradeVolume)
            {
                EFM_CustomItemWrapper CIW = itemTransform.GetComponent<EFM_CustomItemWrapper>();
                EFM_VanillaItemDescriptor VID = itemTransform.GetComponent<EFM_VanillaItemDescriptor>();
                List<EFM_MarketItemView> itemViewListToUse = null;
                string itemID;
                int itemValue;
                bool custom = false;
                if(CIW != null)
                {
                    if(CIW.marketItemViews == null)
                    {
                        CIW.marketItemViews = new List<EFM_MarketItemView>();
                    }
                    CIW.marketItemViews.Clear();
                    itemViewListToUse = CIW.marketItemViews;

                    itemID = CIW.ID;
                    custom = true;

                    itemValue = CIW.GetValue();

                    if (!trader.ItemSellable(itemID, CIW.parents))
                    {
                        continue;
                    }
                }
                else
                {
                    if (VID.marketItemViews == null)
                    {
                        VID.marketItemViews = new List<EFM_MarketItemView>();
                    }
                    VID.marketItemViews.Clear();
                    itemViewListToUse = VID.marketItemViews;

                    itemID = VID.H3ID;

                    itemValue = VID.GetValue();

                    if (!trader.ItemSellable(itemID, VID.parents))
                    {
                        continue;
                    }
                }

                Transform currentHorizontal = currentSellHorizontals[currentSellHorizontals.Count - 1];
                if (currentSellHorizontals[currentSellHorizontals.Count - 1].childCount == 7)
                {
                    currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                }

                Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                marketItemView.custom = custom;
                marketItemView.CIW = CIW;
                marketItemView.VID = VID;

                // Write price to item icon and set correct currency icon
                Sprite currencySprite = null;
                string currencyItemID = "";
                if (trader.currency == 0)
                {
                    currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                    currencyItemID = "203";
                }
                else if (trader.currency == 1)
                {
                    currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                    itemValue = (int)Mathf.Max(itemValue * 0.008f, 1); // Adjust item value
                    currencyItemID = "201";
                }
                currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemValue.ToString();

                // Setup button
                EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                pointableButton.SetButton();
                pointableButton.Button.onClick.AddListener(() => { OnSellItemClick(currentItemIcon, itemValue, currencyItemID); });
                pointableButton.MaxPointingRange = 20;
                pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                // Add the icon object to the list for that item
                if(CIW != null)
                {
                    CIW.marketItemViews.Add(marketItemView);
                }
                else
                {
                    VID.marketItemViews.Add(marketItemView);
                }
            }
            // Setup button
            EFM_PointableButton pointableSellDealButton = traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            pointableSellDealButton.SetButton();
            pointableSellDealButton.Button.onClick.AddListener(() => { OnSellDealClick(); });
            pointableSellDealButton.MaxPointingRange = 20;
            pointableSellDealButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

            // Tasks
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            // Clear previous tasks
            while (tasksParent.childCount > 1)
            {
                Destroy(tasksParent.GetChild(1));
            }
            if (referencedTasks != null)
            {
                foreach (TraderTask referencedTask in referencedTasks)
                {
                    referencedTask.marketListElement = null;
                }
                foreach (TraderTaskCondition referencedTaskCondition in referencedTaskConditions)
                {
                    referencedTaskCondition.marketListElement = null;
                }
            }
            else
            {
                referencedTasks = new List<TraderTask>();
                referencedTaskConditions = new List<TraderTaskCondition>();
            }
            // Add all of that trader's available and active tasks to the list
            foreach (TraderTask task in trader.tasks)
            {
                if(task.taskState == TraderTask.TaskState.Available)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    task.marketListElement = currentTaskElement;

                    // Short info
                    Transform shortInfo = currentTaskElement.transform.GetChild(0);
                    shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
                    shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;

                    // Description
                    Transform description = currentTaskElement.transform.GetChild(1);
                    description.GetChild(0).GetComponent<Text>().text = task.description;
                    // Objectives (conditions)
                    Transform objectivesParent = description.GetChild(1).GetChild(1);
                    GameObject objectiveTemplate = objectivesParent.GetChild(0).gameObject;
                    foreach(KeyValuePair<string, TraderTaskCondition> condition in task.completionConditions)
                    {
                        TraderTaskCondition currentCondition = condition.Value;
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentCondition.marketListElement = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case TraderTaskCondition.ConditionType.CounterCreator:
                                    foreach(TraderTaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if(counter.counterConditionType == TraderTaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/"+currentCondition.value; // Activate progress counter
                                            break;
                                        }
                                    }
                                    break;
                                case TraderTaskCondition.ConditionType.HandoverItem:
                                case TraderTaskCondition.ConditionType.FindItem:
                                case TraderTaskCondition.ConditionType.LeaveItemAtLocation:
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Disable condition gameObject if visibility conditions not met
                        if(currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                        {
                            foreach(TraderTaskCondition visibilityCondition in currentCondition.visibilityConditions)
                            {
                                if(!visibilityCondition.fulfilled)
                                {
                                    currentObjectiveElement.SetActive(false);
                                    break;
                                }
                            }
                        }
                    }
                    // Initial equipment
                    if (task.startingEquipment != null && task.startingEquipment.Count > 0)
                    {
                        Transform initEquipParent = description.GetChild(2);
                        initEquipParent.gameObject.SetActive(true);
                        GameObject currentInitEquipHorizontalTemplate = initEquipParent.GetChild(1).gameObject;
                        Transform currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                        foreach (TraderTaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                            }
                            switch (reward.taskRewardType)
                            {
                                case TraderTaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TraderTaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                    GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Rewards
                    Transform rewardParent = description.GetChild(2);
                    rewardParent.gameObject.SetActive(true);
                    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
                    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            case TraderTaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            default:
                                break;
                        }
                    }
                    // TODO: Maybe have fail conditions and fail rewards sections

                    // Setup buttons
                    // ShortInfo
                    EFM_PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Start
                    EFM_PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskStartButton.SetButton();
                    pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
                    pointableTaskStartButton.MaxPointingRange = 20;
                    pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    EFM_PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
                else if(task.taskState == TraderTask.TaskState.Active)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    task.marketListElement = currentTaskElement;

                    // Short info
                    Transform shortInfo = currentTaskElement.transform.GetChild(0);
                    shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
                    shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;
                    shortInfo.GetChild(2).gameObject.SetActive(true);
                    shortInfo.GetChild(3).gameObject.SetActive(false);
                    shortInfo.GetChild(5).gameObject.SetActive(true);
                    shortInfo.GetChild(6).gameObject.SetActive(false);

                    // Description
                    Transform description = currentTaskElement.transform.GetChild(1);
                    description.GetChild(0).GetComponent<Text>().text = task.description;
                    // Objectives (conditions)
                    Transform objectivesParent = description.GetChild(1).GetChild(1);
                    GameObject objectiveTemplate = objectivesParent.GetChild(0).gameObject;
                    int completedCount = 0;
                    int totalCount = 0;
                    foreach (KeyValuePair<string, TraderTaskCondition> condition in task.completionConditions)
                    {
                        TraderTaskCondition currentCondition = condition.Value;
                        if (currentCondition.fulfilled)
                        {
                            ++completedCount;
                        }
                        ++totalCount;
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentCondition.marketListElement = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case TraderTaskCondition.ConditionType.CounterCreator:
                                    foreach (TraderTaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if (counter.counterConditionType == TraderTaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)counter.killCount) / currentCondition.value * 60, 6);
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = counter.killCount.ToString() + "/" + currentCondition.value;
                                            break;
                                        }
                                    }
                                    break;
                                case TraderTaskCondition.ConditionType.HandoverItem:
                                case TraderTaskCondition.ConditionType.FindItem:
                                case TraderTaskCondition.ConditionType.LeaveItemAtLocation:
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)currentCondition.itemCount) / currentCondition.value * 60, 6);
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.itemCount.ToString() + "/" + currentCondition.value;
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Disable condition gameObject if visibility conditions not met
                        if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                        {
                            foreach (TraderTaskCondition visibilityCondition in currentCondition.visibilityConditions)
                            {
                                if (!visibilityCondition.fulfilled)
                                {
                                    currentObjectiveElement.SetActive(false);
                                    break;
                                }
                            }
                        }
                    }
                    // Initial equipment
                    if (task.startingEquipment != null && task.startingEquipment.Count > 0)
                    {
                        Transform initEquipParent = description.GetChild(2);
                        initEquipParent.gameObject.SetActive(true);
                        GameObject currentInitEquipHorizontalTemplate = initEquipParent.GetChild(1).gameObject;
                        Transform currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                        foreach (TraderTaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                            }
                            switch (reward.taskRewardType)
                            {
                                case TraderTaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TraderTaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                    GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Rewards
                    Transform rewardParent = description.GetChild(2);
                    rewardParent.gameObject.SetActive(true);
                    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
                    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            case TraderTaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            default:
                                break;
                        }
                    }
                    // TODO: Maybe have fail conditions and fail rewards sections

                    // Set total progress depending on conditions
                    float fractionCompletion = ((float)completedCount) / totalCount;
                    shortInfo.GetChild(5).GetChild(0).GetComponent<Text>().text = String.Format("{0:0}%", fractionCompletion * 100);
                    shortInfo.GetChild(5).GetChild(1).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(fractionCompletion * 60, 6);

                    // Setup buttons
                    // ShortInfo
                    EFM_PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Start
                    EFM_PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskStartButton.SetButton();
                    pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
                    pointableTaskStartButton.MaxPointingRange = 20;
                    pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    EFM_PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
                else if(task.taskState == TraderTask.TaskState.Complete)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    task.marketListElement = currentTaskElement;

                    // Short info
                    Transform shortInfo = currentTaskElement.transform.GetChild(0);
                    shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
                    shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;
                    shortInfo.GetChild(3).gameObject.SetActive(false);
                    shortInfo.GetChild(4).gameObject.SetActive(true);
                    shortInfo.GetChild(6).gameObject.SetActive(false);
                    shortInfo.GetChild(7).gameObject.SetActive(true);

                    // Description
                    Transform description = currentTaskElement.transform.GetChild(1);
                    description.GetChild(0).GetComponent<Text>().text = task.description;
                    // Objectives (conditions)
                    Transform objectivesParent = description.GetChild(1).GetChild(1);
                    GameObject objectiveTemplate = objectivesParent.GetChild(0).gameObject;
                    foreach (KeyValuePair<string, TraderTaskCondition> condition in task.completionConditions)
                    {
                        TraderTaskCondition currentCondition = condition.Value;
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentCondition.marketListElement = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case TraderTaskCondition.ConditionType.CounterCreator:
                                    foreach (TraderTaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if (counter.counterConditionType == TraderTaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 6);
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.value.ToString() + "/" + currentCondition.value;
                                            break;
                                        }
                                    }
                                    break;
                                case TraderTaskCondition.ConditionType.HandoverItem:
                                case TraderTaskCondition.ConditionType.FindItem:
                                case TraderTaskCondition.ConditionType.LeaveItemAtLocation:
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 6);
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.value.ToString() + "/" + currentCondition.value;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Initial equipment
                    if (task.startingEquipment != null && task.startingEquipment.Count > 0)
                    {
                        Transform initEquipParent = description.GetChild(2);
                        initEquipParent.gameObject.SetActive(true);
                        GameObject currentInitEquipHorizontalTemplate = initEquipParent.GetChild(1).gameObject;
                        Transform currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                        foreach (TraderTaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                            }
                            switch (reward.taskRewardType)
                            {
                                case TraderTaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TraderTaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                    GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Rewards
                    Transform rewardParent = description.GetChild(2);
                    rewardParent.gameObject.SetActive(true);
                    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
                    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            case TraderTaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
                                break;
                            default:
                                break;
                        }
                    }
                    // TODO: Maybe have fail conditions and fail rewards sections

                    // Setup buttons
                    // ShortInfo
                    EFM_PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Start
                    EFM_PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskStartButton.SetButton();
                    pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
                    pointableTaskStartButton.MaxPointingRange = 20;
                    pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    EFM_PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<EFM_PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
            }

            // Insure
            // TODO

            // TODO: Add all necessary hover scrolls
            // TODO: Setup tabs with functionality to make sure the corerct one overlaps 
        }

        public void UpdateBasedOnItem(bool added, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            if (added)
            {
                // TODO: IN BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
                // TODO: IN SELL, check if item is already in showcase, if it is, increment count, if not, add a new entry, update price under FOR, make sure deal! button is activated if item is a sellable item
                // TODO: IN TASKS, for each item requirement of each task, activate TURN IN buttons accordingly
                // TODO: IN INSURE, check if item already in showcase, if it is, increment count, if not, add a new entry, update price, make sure deal! button is activated
            }
            else
            {
                // TODO: IN BUY, check if item corresponds to price, update fulfilled icon and deactivate deal! button if necessary
                // TODO: IN SELL, find item in showcase, if there are more its stack, decrement count, if not, remove entry, update price under FOR, make sure deal! button is deactivated if no sellable item in volume (only need to check this if this item was sellable)
                // TODO: IN TASKS, for each item requirement of each task, deactivate TURN IN buttons accordingly
                // TODO: IN INSURE, check if item already in showcase, if it is, increment count, if not, add a new entry, update price, make sure deal! button is activated
            }
        }

        public void UpdateBasedOnPlayerLevel()
        {
            // TODO: Update UI based on level
            // Gonna have to GetLoyaltyLevel() of currently displayed traderstatus and see if need to display the correct next attitude and level data on the trader UI
            // Will also need to refresh the items being sold by the trader and the prices because prices get lower as they level up and we can sell for more
            // will also need to check ifl evel 15 then we also want to add player items to flea market
        }

        public void OnBuyItemClick(AssortmentItem item, Dictionary<string, int> priceList)
        {
            // TODO: Set item in cart and store item in a var taht can be accessed when we click Deal! button
            // Set prices in cart and count items in tradevolume to check which prices are fulfilled, change fulfilled icons accordngingly
        }

        public void OnBuyDealClick()
        {
            // TODO: Remove price from trde volume
            // add chosen amount of item to trade volume at random pos and rot within it
            // update amount of item in assort
            // update amount of item in showcase
            // remove item from showcase if no more of it
        }

        public void OnBuyAmountClick()
        {
            // TODO: Set into amount choosing mode
            // if already in stack split mode, cancel it
            // display stack split UI to choose buy amount
            // MAYBE DO ALL THIS IN EFM_Hand SO THAT WE CAN COORDINATE IT WITH STACK SPLITTING PROPERLY
            // Set amount when we confirm amount in cart amount text and logically keep track of count so we buy the right amount
        }

        public void OnSellItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        {
            // TODO: Set cart UI to this item
            // de/activate deal! button depending on whether trader has enough money
        }

        public void OnSellDealClick()
        {
            // TODO: Remove all sellable items from trade volume
            // Add FOR to trade volume
            // Clear Sell showcase completely
            // Deactivate deal button
        }

        public void OnTaskShortInfoClick(GameObject description)
        {
            // Toggle task description
            description.SetActive(!description.activeSelf);
            clickAudio.Play();
        }

        public void OnTaskStartClick(TraderTask task)
        {
            // TODO: Set state of task to active
            // TODO: Add task to active task list of player status
            // TODO: Update market task list by making the shortinfo of the referenced task UI element in TraderTask to show that it is active
            // TODO: Update visibility conditions that are dependent on this task being started, then update everything depending on those visibility conditions
        }

        public void OnTaskFinishClick(TraderTask task)
        {
            // TODO: Set state of task to Success
            // TODO: Remove task from active task list of player status
            // TODO: Update market task list by removing the referenced task UI element in TraderTask from the market task list
            // TODO: Update visibility conditions that are dependent on this task being started, then update everything depending on those visibility conditions
            // TODO: Update all quest conditions dependent on success of this quest, then update everything depending on those conditions
        }
    }
}
