using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class MarketManager : MonoBehaviour
    {
        // Objects
        public HideoutController controller;
        public ContainmentVolume tradeVolume;
        public AudioSource clickAudio;

        public Sprite[] traderIcons;
        public Image traderDetailsIcon;
        public GameObject traderDetailsLevelElite;
        public Text traderDetailsLevelText;
        public Text traderDetailsStanding;
        public Sprite[] currencyIcons; // RUB, USD, EUR
        public Image traderDetailsCurrencyIcon;
        public Text traderDetailsMoney;
        public Sprite[] playerRankIcons;
        public Image traderDetailsPlayerRankIcon;
        public Text traderDetailsPlayerRankText;
        public GameObject traderDetailsCurrentLevelElite;
        public Text traderDetailsCurrentLevelText;
        public Image traderDetailsCurrentPlayerRankIcon;
        public Text traderDetailsCurrentPlayerRankText;
        public Text traderDetailsCurrentStanding;
        public Image traderDetailsCurrentSaleSumCurrencyIcon;
        public Text traderDetailsCurrentSaleSum;
        public GameObject traderDetailsNextLevel;
        public GameObject traderDetailsNextLevelElite;
        public Text traderDetailsNextLevelText;
        public Image traderDetailsNextPlayerRankIcon;
        public Text traderDetailsNextPlayerRankText;
        public Text traderDetailsNextStanding;
        public Image traderDetailsNextSaleSumCurrencyIcon;
        public Text traderDetailsNextSaleSum;
        public Text traderDetailsPlayerRouble;
        public Text traderDetailsPlayerEuro;
        public Text traderDetailsPlayerDollar;

        public GameObject[] pages; // Buy, Sell, Tasks, Insure

        public Transform buyShowcaseContent;
        public GameObject buyShowcaseRowPrefab;
        public GameObject buyShowcaseItemViewPrefab;
        public Text buyItemName;
        public PriceItemView buyItemView;
        public Dictionary<string, PriceItemView> buyItemPriceViewsByH3ID;
        public Text buyItemCount;
        public Transform buyPricesContent;
        public GameObject buyPricePrefab;
        public GameObject buyDealButton;
        public GameObject buyCancelButton;

        public Transform sellShowcaseContent;
        public GameObject sellShowcaseRowPrefab;
        public GameObject sellShowcaseItemViewPrefab;
        public Text sellItemName;
        public PriceItemView sellItemView;
        public GameObject sellDealButton;

        public Transform tasksContent;
        public GameObject taskPrefab;

        public Transform insureShowcaseContent;
        public GameObject insureShowcaseRowPrefab;
        public GameObject insureShowcaseItemViewPrefab;
        public Text insureItemName;
        public PriceItemView insureItemView;
        public GameObject insureDealButton;

        // Live data
        public float fenceRestockTimer;

        public Dictionary<string, int> inventory;
        public Dictionary<string, List<MeatovItem>> inventoryItems;
        public Dictionary<string, int> FIRInventory;
        public Dictionary<string, List<MeatovItem>> FIRInventoryItems;

        public List<Task> referencedTasks;
        public List<Condition> referencedConditions;
        public TraderTab[] traderTabs;
        public TraderTab[] ragFairTabs;
        public Dictionary<string, GameObject> wishListItemViewsByID;
        public Dictionary<string, List<GameObject>> ragFairItemBuyViewsByID;

        public int currentTraderIndex;
        public MeatovItemData cartItem;
        public int cartItemCount;
        public List<BarterPrice> buyPrices;
        public string ragfairCartItem;
        public int ragfairCartItemCount;
        public List<BarterPrice> ragfairPrices;
        public List<GameObject> ragfairBuyPriceElements;
        public int currentTotalSellingPrice = 0;
        public int currentTotalInsurePrice = 0;

        private bool initButtonsSet;
        public bool choosingBuyAmount;
        public bool choosingRagfairBuyAmount;
        public bool startedChoosingThisFrame;
        private Vector3 amountChoiceStartPosition;
        private Vector3 amountChoiceRightVector;
        private int chosenAmount;
        private int maxBuyAmount;

        private GameObject currentActiveCategory;
        private GameObject currentActiveItemSelector;

        // Events
        public delegate void OnItemAddedToTradeInventoryDelegate(MeatovItem item);
        public static event OnItemAddedToTradeInventoryDelegate OnItemAddedToTradeInventory;
        public delegate void OnItemRemovedFromTradeInventoryDelegate(MeatovItem item);
        public static event OnItemRemovedFromTradeInventoryDelegate OnItemRemovedFromTradeInventory;
        public delegate void OnTradeInventoryItemStackChangedDelegate(MeatovItem item, int stackDifference);
        public static event OnTradeInventoryItemStackChangedDelegate OnTradeInventoryItemStackChanged;

        public void Start()
        {
            // Process items loaded in trade volume
            foreach(KeyValuePair<string, List<MeatovItem>> itemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < itemEntry.Value.Count; ++i)
                {
                    OnItemAdded(itemEntry.Value[i]);
                }
            }

            // Subscribe to events
            tradeVolume.OnItemAdded += OnItemAdded;
            tradeVolume.OnItemRemoved += OnItemRemoved;
            
            // Set default trader
            SetTrader(0);
        }

        private void Update()
        {
            TakeInput();

            // Update based on splitting stack
            if (choosingBuyAmount || choosingRagfairBuyAmount)
            {
                Vector3 handVector = Mod.rightHand.transform.localPosition - amountChoiceStartPosition;
                float angle = Vector3.Angle(amountChoiceRightVector, handVector);
                float distanceFromCenter = Mathf.Clamp(handVector.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad), -0.19f, 0.19f);

                // Scale is from -0.19 (1) to 0.19 (max)
                if (distanceFromCenter <= -0.19f)
                {
                    chosenAmount = 1;
                }
                else if (distanceFromCenter >= 0.19f)
                {
                    chosenAmount = maxBuyAmount;
                }
                else
                {
                    chosenAmount = Mathf.Max(1, (int)(Mathf.InverseLerp(-0.19f, 0.19f, distanceFromCenter) * maxBuyAmount));
                }

                Mod.stackSplitUICursor.transform.localPosition = new Vector3(distanceFromCenter * 100, -2.14f, 0);
                Mod.stackSplitUIText.text = chosenAmount.ToString() + "/" + maxBuyAmount;
            }

            if (fenceRestockTimer > 0)
            {
                fenceRestockTimer -= Time.deltaTime;
            }
            else
            {
                fenceRestockTimer = Convert.ToSingle((DateTime.Today.ToUniversalTime().AddHours(24) - DateTime.UtcNow).TotalSeconds);
                if (currentTraderIndex == 2)
                {
                    //SetTrader(2);
                }
            }
        }

        public void OnItemAdded(MeatovItem item)
        {
            AddToInventory(item);

            // Update children
            for (int i = 0; i < item.children.Count; ++i)
            {
                AddToInventory(item.children[i]);
            }

            UpdateBuyPriceForItem(item);
        }

        public void UpdateBuyPriceForItem(MeatovItem item)
        {
            if (buyItemPriceViewsByH3ID.TryGetValue(item.H3ID, out PriceItemView itemView))
            {
                bool prefulfilled = itemView.fulfilledIcon.activeSelf;
                int count = 0;
                tradeVolume.inventory.TryGetValue(item.H3ID, out count);
                itemView.amount.text = Mathf.Min(itemView.price.count, count).ToString() + "/" + itemView.price.count.ToString();
                if(count >= itemView.price.count)
                {
                    itemView.fulfilledIcon.SetActive(true);
                    itemView.unfulfilledIcon.SetActive(false);
                    if (!prefulfilled)
                    {
                        // Newly fulfilled, we might now be able to buy, check if all prices fulfilled
                        bool allFulfilled = true;
                        for(int i = 0; i < buyPrices.Count; ++i)
                        {
                            int currentCount = 0;
                            allFulfilled |= tradeVolume.inventory.TryGetValue(buyPrices[i].itemData.H3ID, out currentCount) && currentCount >= buyPrices[i].count;
                        }
                        buyDealButton.SetActive(allFulfilled);
                    }
                }
                else
                {
                    itemView.fulfilledIcon.SetActive(false);
                    itemView.unfulfilledIcon.SetActive(true);
                    buyDealButton.SetActive(false);
                }
            }
        }

        public void OnItemRemoved(MeatovItem item)
        {
            RemoveFromInventory(item);

            // Update children
            for (int i = 0; i < item.children.Count; ++i)
            {
                RemoveFromInventory(item.children[i]);
            }

            UpdateBuyPriceForItem(item);
        }

        public void AddToInventory(MeatovItem item, bool stackOnly = false, int stackDifference = 0)
        {
            // StackOnly should be true if not item location was changed, but the stack count has
            if (stackOnly)
            {
                if (inventory.ContainsKey(item.H3ID))
                {
                    inventory[item.H3ID] += stackDifference;

                    if (inventory[item.H3ID] <= 0)
                    {
                        Mod.LogError("DEV: Market AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        inventory.Remove(item.H3ID);
                        inventoryItems.Remove(item.H3ID);
                    }
                }
                else
                {
                    Mod.LogError("DEV: Market AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " did not find ID in inventory:\n" + Environment.StackTrace);
                }

                OnTradeInventoryItemStackChangedInvoke(item, stackDifference);
            }
            else
            {
                if (inventory.ContainsKey(item.H3ID))
                {
                    inventory[item.H3ID] += item.stack;
                    inventoryItems[item.H3ID].Add(item);
                }
                else
                {
                    inventory.Add(item.H3ID, item.stack);
                    inventoryItems.Add(item.H3ID, new List<MeatovItem> { item });
                }

                OnItemAddedToTradeInventoryInvoke(item);
            }
        }

        public void OnItemAddedToTradeInventoryInvoke(MeatovItem item)
        {
            if (OnItemAddedToTradeInventory != null)
            {
                OnItemAddedToTradeInventory(item);
            }
        }

        public void OnItemRemovedFromTradeInventoryInvoke(MeatovItem item)
        {
            if (OnItemRemovedFromTradeInventory != null)
            {
                OnItemRemovedFromTradeInventory(item);
            }
        }

        public void OnTradeInventoryItemStackChangedInvoke(MeatovItem item, int stackDifference)
        {
            if (OnTradeInventoryItemStackChanged != null)
            {
                OnTradeInventoryItemStackChanged(item, stackDifference);
            }
        }

        public void RemoveFromInventory(MeatovItem item)
        {
            if (inventory.ContainsKey(item.H3ID))
            {
                inventory[item.H3ID] -= item.stack;
                inventoryItems[item.H3ID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove " + item.H3ID + " from market inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (inventory[item.H3ID] == 0)
            {
                inventory.Remove(item.H3ID);
                inventoryItems.Remove(item.H3ID);
            }

            OnItemRemovedFromTradeInventoryInvoke(item);
        }

        private void TakeInput()
        {
            if (choosingBuyAmount || choosingRagfairBuyAmount)
            {
                FVRViveHand hand = Mod.rightHand.fvrHand;
                Transform cartShowcase;
                string countString;
                if (startedChoosingThisFrame)
                {
                    // Skip just this frame because if we started this frame, for sure the trigger is down
                    startedChoosingThisFrame = false;
                }
                else if (hand.Input.TriggerDown)
                {
                    List<BarterPrice> pricesToUse = null;
                    if (choosingBuyAmount)
                    {
                        cartItemCount = chosenAmount;
                        cartShowcase = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1);
                        countString = cartItemCount.ToString();
                        pricesToUse = buyPrices;
                    }
                    else
                    {
                        ragfairCartItemCount = chosenAmount;
                        cartShowcase = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1);
                        countString = ragfairCartItemCount.ToString();
                        pricesToUse = ragfairPrices;
                    }
                    Mod.stackSplitUI.SetActive(false);

                    // Change amount and price on UI
                    cartShowcase.GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = countString;

                    Transform pricesParent = cartShowcase.GetChild(3).GetChild(0).GetChild(0);
                    int priceElementIndex = 1;
                    bool canDeal = true;
                    foreach (BarterPrice price in pricesToUse)
                    {
                        Transform priceElement = pricesParent.GetChild(priceElementIndex++).transform;
                        priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = (price.count * cartItemCount).ToString();

                        if (inventory.ContainsKey(price.itemData.H3ID) && inventory[price.itemData.H3ID] >= price.count)
                        {
                            priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                            priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                        }
                        else
                        {
                            priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
                            priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
                            canDeal = false;
                        }
                    }

                    Transform dealButton = cartShowcase.parent.GetChild(2).GetChild(0).GetChild(0);
                    if (canDeal)
                    {
                        dealButton.GetComponent<Collider>().enabled = true;
                        dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
                    }
                    else
                    {
                        dealButton.GetComponent<Collider>().enabled = false;
                        dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
                    }

                    // Reenable buy amount buttons
                    transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = true;
                    transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = true;

                    choosingBuyAmount = false;
                    choosingRagfairBuyAmount = false;
                }
            }
        }

        public void AddRagFairCategories(List<CategoryTreeNode> children, Transform parent, GameObject template, int level)
        {
            foreach (CategoryTreeNode child in children)
            {
                GameObject category = Instantiate(template, parent);
                category.SetActive(true);
                category.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(level * 10, 0, 0, 0);
                category.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = child.name;
                category.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = "(" + ((child.children.Count == 0 && Mod.itemsByParents.ContainsKey(child.ID)) ? Mod.itemsByParents[child.ID].Count : child.children.Count) + ")";

                // Setup buttons
                //category.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairCategoryMainClick(category, child.ID); });
                //category.transform.GetChild(0).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairCategoryToggleClick(category); });

                // Setup actual item entries if this is a leaf category
                if (child.children.Count == 0)
                {
                    if (Mod.itemsByParents.ContainsKey(child.ID))
                    {
                        List<MeatovItemData> itemIDs = Mod.itemsByParents[child.ID];
                        foreach (MeatovItemData itemID in itemIDs)
                        {
                            GameObject item = Instantiate(template, category.transform.GetChild(1));
                            item.SetActive(true);
                            item.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((level + 1) * 10, 0, 0, 0);
                            item.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);

                            item.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = "itemname";
                            //item.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = "(" + GetTotalItemSell(itemID.H3ID) + ")";

                            item.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

                            //item.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairItemMainClick(item, itemID.H3ID); });
                        }
                    }
                }
                else
                {
                    AddRagFairCategories(child.children, category.transform.GetChild(1), template, level + 1);
                }
            }
        }

        public void SetTrader(int index, string defaultItemID = null)
        {
            Mod.LogInfo("set trader called with index: " + index);
            currentTraderIndex = index;
            Trader trader = Mod.traders[index];

            Mod.LogInfo("0");
            // Top
            traderDetailsIcon.sprite = traderIcons[index];
            traderDetailsPlayerRankIcon.sprite = playerRankIcons[Mod.level / 5];
            traderDetailsPlayerRankText.text = Mod.level.ToString();
            traderDetailsStanding.text = trader.standing.ToString();
            traderDetailsCurrencyIcon.sprite = currencyIcons[trader.currency];
            traderDetailsMoney.text = Mod.FormatMoneyString(trader.balance);

            LoyaltyLevel currentLevel = trader.levels[trader.level];
            if (trader.level == trader.levels.Length - 1)
            {
                traderDetailsLevelElite.SetActive(true);
                traderDetailsLevelText.gameObject.SetActive(false);

                traderDetailsCurrentLevelElite.SetActive(true);
                traderDetailsCurrentLevelText.gameObject.SetActive(false);

                traderDetailsNextLevel.SetActive(false);
            }
            else
            {
                traderDetailsLevelElite.SetActive(false);
                traderDetailsLevelText.gameObject.SetActive(true);
                traderDetailsLevelText.text = Trader.LevelToRoman(trader.level);

                traderDetailsCurrentLevelElite.SetActive(false);
                traderDetailsCurrentLevelText.gameObject.SetActive(true);
                traderDetailsCurrentLevelText.text = Trader.LevelToRoman(trader.level);

                LoyaltyLevel nextLevel = trader.levels[trader.level + 1];
                traderDetailsNextLevel.SetActive(true);
                if(nextLevel == trader.levels[trader.levels.Length - 1])
                {
                    traderDetailsNextLevelElite.SetActive(true);
                    traderDetailsNextLevelText.gameObject.SetActive(false);
                }
                else
                {
                    traderDetailsNextLevelElite.SetActive(false);
                    traderDetailsNextLevelText.gameObject.SetActive(true);
                    traderDetailsCurrentLevelText.text = Trader.LevelToRoman(trader.level + 1);
                }
                traderDetailsNextPlayerRankIcon.sprite = playerRankIcons[nextLevel.minLevel / 5];
                traderDetailsNextPlayerRankText.text = nextLevel.minLevel.ToString();
                if(Mod.level < nextLevel.minLevel)
                {
                    traderDetailsNextPlayerRankText.color = Color.red;
                }
                else
                {
                    traderDetailsNextPlayerRankText.color = Color.cyan;
                }
                traderDetailsNextStanding.text = nextLevel.minStanding.ToString("0.00");
                if (trader.standing < nextLevel.minStanding)
                {
                    traderDetailsNextStanding.color = Color.red;
                }
                else
                {
                    traderDetailsNextStanding.color = Color.cyan;
                }
                traderDetailsNextSaleSumCurrencyIcon.sprite = currencyIcons[trader.currency];
                traderDetailsNextSaleSum.text = Mod.FormatMoneyString(nextLevel.minSaleSum);
                if (trader.salesSum < nextLevel.minSaleSum)
                {
                    traderDetailsNextSaleSum.color = Color.red;
                }
                else
                {
                    traderDetailsNextSaleSum.color = Color.cyan;
                }
            }
            traderDetailsCurrentPlayerRankIcon.sprite = playerRankIcons[currentLevel.minLevel / 5];
            traderDetailsCurrentPlayerRankText.text = currentLevel.minLevel.ToString();
            traderDetailsCurrentStanding.text = currentLevel.minStanding.ToString("0.00");
            traderDetailsCurrentSaleSumCurrencyIcon.sprite = currencyIcons[trader.currency];
            traderDetailsCurrentSaleSum.text = Mod.FormatMoneyString(currentLevel.minSaleSum);

            traderDetailsPlayerRouble.text = Mod.FormatCompleteMoneyString(Mod.GetItemCountInInventories("203"));
            traderDetailsPlayerDollar.text = Mod.FormatCompleteMoneyString(Mod.GetItemCountInInventories("201"));
            traderDetailsPlayerEuro.text = Mod.FormatCompleteMoneyString(Mod.GetItemCountInInventories("202"));
            
            Mod.LogInfo("0");
            // Main
            // Buy
            Mod.LogInfo("0");
            while (buyShowcaseContent.childCount > 1)
            {
                Transform currentFirstChild = buyShowcaseContent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("0");
            // Only fill up showcase if standing with this trader is >= 0
            if (trader.standing >= 0)
            {
                // Fence special case, no assort, want to build a random one
                if (index == 2)
                {
                    TODO: // Generate fence barters

                    // We want everyone to use the same seed for generating random fence barters
                    UnityEngine.Random.InitState(Convert.ToInt32((DateTime.UtcNow - DateTime.Today.ToUniversalTime()).TotalHours));
                }
                else
                {
                    for (int i = 0; i <= trader.level; ++i)
                    {
                        List<Barter> barters = trader.bartersByLevel[i];

                        for (int j = 0; j < barters.Count; ++j)
                        {
                            Barter currentBarter = barters[j];

                            // Skip if this barter is locked
                            if (currentBarter.needUnlock && !trader.rewardBarters[currentBarter.itemData.H3ID])
                            {
                                continue;
                            }

                            // Add new row if necessary
                            Transform currentRow = buyShowcaseContent.GetChild(buyShowcaseContent.childCount - 1);
                            if (buyShowcaseContent.childCount == 1 || currentRow.childCount == 7) 
                            {
                                currentRow = GameObject.Instantiate(buyShowcaseRowPrefab, buyShowcaseContent).transform;
                                currentRow.gameObject.SetActive(true);
                            }

                            GameObject currentItemView = GameObject.Instantiate(buyShowcaseItemViewPrefab, currentRow);
                            currentItemView.SetActive(true);

                            // Setup ItemView
                            ItemView itemView = currentItemView.GetComponent<ItemView>();
                            int valueToUse = 0;
                            int currencyToUse = 0;
                            for (int k = 0; k < currentBarter.prices.Length; ++k)
                            {
                                valueToUse += currentBarter.prices[k].count;
                            }
                            if (currentBarter.prices.Length > 1)
                            {
                                currencyToUse = 3; // Item trade icon
                            }
                            else
                            {
                                currencyToUse = Mod.ItemIDToCurrencyIndex(currentBarter.prices[0].itemData.H3ID);
                            }
                            itemView.SetItemData(currentBarter.itemData, false, false, false, null, true, currencyToUse, valueToUse, false, false);

                            // Setup button
                            PointableButton pointableButton = currentItemView.GetComponent<PointableButton>();
                            pointableButton.Button.onClick.AddListener(() => { OnBuyItemClick(currentBarter.itemData, currentBarter.prices); });
                        }
                    }
                }
            }

            OnBuyItemClick(null, null);

            Mod.LogInfo("0");
            // Sell
            while (sellShowcaseContent.childCount > 1)
            {
                Transform currentFirstChild = sellShowcaseContent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("Adding all item involume to sell showcase");
            // Add all items in trade volume that are sellable at this trader to showcase
            int totalSellingPrice = 0;
            sellDealButton.SetActive(false);

            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for(int i=0; i< volumeItemEntry.Value.Count; ++i)
                {
                    Mod.LogInfo("\tAdding item from volume: " + volumeItemEntry.Value[i].name);
                    MeatovItem meatovItem = volumeItemEntry.Value[i];

                    // Check if this item can be sold to this trader
                    if (!trader.ItemSellable(meatovItem.itemData))
                    {
                        continue;
                    }

                    // Manage rows
                    Transform currentRow = sellShowcaseContent.GetChild(sellShowcaseContent.childCount - 1);
                    if (sellShowcaseContent.childCount == 1 || currentRow.childCount == 7) // If dont even have a single horizontal yet, add it
                    {
                        currentRow = GameObject.Instantiate(sellShowcaseRowPrefab, sellShowcaseContent).transform;
                        currentRow.gameObject.SetActive(true);
                    }

                    GameObject currentItemView = GameObject.Instantiate(sellShowcaseItemViewPrefab, currentRow);
                    currentItemView.SetActive(true);

                    // Setup ItemView
                    ItemView itemView = currentItemView.GetComponent<ItemView>();
                    int actualValue = meatovItem.itemData.value;

                    // Apply exchange rate if necessary
                    if(trader.currency == 1)
                    {
                        actualValue = (int)Mathf.Max(actualValue / 120.0f, 1);
                    }
                    else if(trader.currency == 2)
                    {
                        actualValue = (int)Mathf.Max(actualValue / 135.0f, 1);
                    }

                    // Apply trader buy coefficient
                    actualValue -= (int)(actualValue * (trader.levels[trader.level].buyPriceCoef / 100.0f));
                    actualValue = Mathf.Max(actualValue, 1);

                    totalSellingPrice += actualValue;

                    itemView.SetItem(meatovItem, true, trader.currency, actualValue);

                    // Set the itemView for that item
                    meatovItem.marketSellItemView = itemView;
                }
            }

            // Activate deal button if necessary
            if(totalSellingPrice > 0)
            {
                sellDealButton.SetActive(true);
            }

            Mod.LogInfo("0");
            // Setup selling price display
            MeatovItemData currencyItemData;
            if (trader.currency == 0)
            {
                Mod.GetItemData("203", out currencyItemData);
            }
            else if (trader.currency == 1)
            {
                Mod.GetItemData("201", out currencyItemData);
            }
            else // 2
            {
                Mod.GetItemData("202", out currencyItemData);
            }
            sellItemName.text = currencyItemData.name;
            sellItemView.itemView.SetItemData(currencyItemData);
            sellItemView.amount.text = totalSellingPrice.ToString();

            Mod.LogInfo("0");
            currentTotalSellingPrice = totalSellingPrice;

            // Tasks
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            float taskListHeight = 3; // Top padding
                                      // Clear previous tasks
            Mod.LogInfo("0");
            while (tasksParent.childCount > 1)
            {
                Transform currentFirstChild = tasksParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("0");
            if (referencedTasks != null)
            {
                foreach (Task referencedTask in referencedTasks)
                {
                    referencedTask.marketUI = null;
                }
                foreach (Condition referencedCondition in referencedConditions)
                {
                    referencedCondition.marketUI = null;
                }
            }
            else
            {
                referencedTasks = new List<Task>();
                referencedConditions = new List<Condition>();
            }
            Mod.LogInfo("0");
            // Add all of that trader's available and active tasks to the list
            foreach (Task task in trader.tasks)
            {
                Mod.LogInfo("Check if can add task " + task.name + " to task list, its state is: " + task.taskState);
                if (task.taskState == Task.TaskState.Available)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    currentTaskElement.SetActive(true);
                    task.marketUI = currentTaskElement;
                    taskListHeight += 29; // Task + Spacing

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
                    foreach (Condition currentCondition in task.completionConditions)
                    {
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
                        currentCondition.marketUI = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case Condition.ConditionType.CounterCreator:
                                    foreach (TaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if (counter.counterConditionType == TaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
                                            break;
                                        }
                                    }
                                    break;
                                case Condition.ConditionType.HandoverItem:
                                case Condition.ConditionType.FindItem:
                                case Condition.ConditionType.LeaveItemAtLocation:
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Setup handover button if necessary
                        if (currentCondition.conditionType == Condition.ConditionType.HandoverItem || currentCondition.conditionType == Condition.ConditionType.WeaponAssembly)
                        {
                            bool FIRInventoryContains = false;
                            int total = 0;
                            foreach (string item in currentCondition.items)
                            {
                                if (tradeVolumeFIRInventory.ContainsKey(item))
                                {
                                    FIRInventoryContains = true;
                                    total += tradeVolumeFIRInventory[item];
                                }
                            }
                            objectiveInfo.GetChild(5).gameObject.SetActive(FIRInventoryContains && total > 0);

                            PointableButton pointableHandOverButton = objectiveInfo.GetChild(5).gameObject.AddComponent<PointableButton>();
                            pointableHandOverButton.SetButton();
                            pointableHandOverButton.Button.onClick.AddListener(() => { OnConditionHandOverClick(currentCondition, objectiveInfo.GetChild(5).gameObject, currentCondition.conditionType == Condition.ConditionType.HandoverItem); });
                            pointableHandOverButton.MaxPointingRange = 10;
                            pointableHandOverButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                        }

                        // Disable condition gameObject if visibility conditions not met
                        if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                        {
                            foreach (Condition visibilityCondition in currentCondition.visibilityConditions)
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
                        foreach (TaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                            }
                            switch (reward.taskRewardType)
                            {
                                case TaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];

                                    // Setup ItemIcon
                                    ItemIcon itemIconScript = currentInitEquipItemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                                    itemIconScript.itemID = reward.itemIDs[0];
                                    itemIconScript.itemName = Mod.itemNames[reward.itemIDs[0]];
                                    itemIconScript.description = Mod.itemDescriptions[reward.itemIDs[0]];
                                    itemIconScript.weight = Mod.itemWeights[reward.itemIDs[0]];
                                    itemIconScript.volume = Mod.itemVolumes[reward.itemIDs[0]];
                                    break;
                                case TaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                    break;
                                case TaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TaskReward.TaskRewardType.AssortmentUnlock:
                                    foreach (string item in reward.itemIDs)
                                    {
                                        if (currentInitEquipHorizontal.childCount == 6)
                                        {
                                            currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                        }
                                        GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                        if (Mod.itemIcons.ContainsKey(item))
                                        {
                                            currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                        }
                                        else
                                        {
                                            AnvilManager.Run(Mod.SetVanillaIcon(item, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                        }
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                        currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

                                        // Setup ItemIcon
                                        ItemIcon assortIconScript = currentInitEquipAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                                        assortIconScript.itemID = item;
                                        assortIconScript.itemName = Mod.itemNames[item];
                                        assortIconScript.description = Mod.itemDescriptions[item];
                                        assortIconScript.weight = Mod.itemWeights[item];
                                        assortIconScript.volume = Mod.itemVolumes[item];
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Rewards
                    Transform rewardParent = description.GetChild(3);
                    rewardParent.gameObject.SetActive(true);
                    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
                    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    currentRewardHorizontal.gameObject.SetActive(true);
                    foreach (TaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];

                                // Setup ItemIcon
                                ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                                itemIconScript.itemID = reward.itemIDs[0];
                                itemIconScript.itemName = Mod.itemNames[reward.itemIDs[0]];
                                itemIconScript.description = Mod.itemDescriptions[reward.itemIDs[0]];
                                itemIconScript.weight = Mod.itemWeights[reward.itemIDs[0]];
                                itemIconScript.volume = Mod.itemVolumes[reward.itemIDs[0]];
                                break;
                            case TaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                break;
                            case TaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.experience > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TaskReward.TaskRewardType.AssortmentUnlock:
                                foreach (string item in reward.itemIDs)
                                {
                                    if (currentRewardHorizontal.childCount == 6)
                                    {
                                        currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                                        currentRewardHorizontal.gameObject.SetActive(true);
                                    }
                                    GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                    currentRewardAssortElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(item))
                                    {
                                        currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(item, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

                                    // Setup ItemIcon
                                    ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
                                    assortIconScript.itemID = item;
                                    assortIconScript.itemName = Mod.itemNames[item];
                                    assortIconScript.description = Mod.itemDescriptions[item];
                                    assortIconScript.weight = Mod.itemWeights[item];
                                    assortIconScript.volume = Mod.itemVolumes[item];
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    // TODO: Maybe have fail conditions and fail rewards sections

                    // Setup buttons
                    // ShortInfo
                    PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Start
                    PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<PointableButton>();
                    pointableTaskStartButton.SetButton();
                    pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
                    pointableTaskStartButton.MaxPointingRange = 20;
                    pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
                else if (task.taskState == Task.TaskState.Active)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    currentTaskElement.SetActive(true);
                    task.marketUI = currentTaskElement;
                    taskListHeight += 29; // Task + Spacing

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
                    foreach (Condition currentCondition in task.completionConditions)
                    {
                        if (currentCondition.fulfilled)
                        {
                            ++completedCount;
                        }
                        ++totalCount;
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
                        currentCondition.marketUI = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case Condition.ConditionType.CounterCreator:
                                    foreach (TaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if (counter.counterConditionType == TaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)counter.killCount) / currentCondition.value * 60, 6);
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = counter.killCount.ToString() + "/" + currentCondition.value;
                                            break;
                                        }
                                    }
                                    break;
                                case Condition.ConditionType.HandoverItem:
                                case Condition.ConditionType.FindItem:
                                case Condition.ConditionType.LeaveItemAtLocation:
                                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                    objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(((float)currentCondition.itemCount) / currentCondition.value * 60, 6);
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.itemCount.ToString() + "/" + currentCondition.value;
                                    break;
                                default:
                                    break;
                            }
                        }

                        // Setup handover button if necessary
                        if (currentCondition.conditionType == Condition.ConditionType.HandoverItem || currentCondition.conditionType == Condition.ConditionType.WeaponAssembly)
                        {
                            bool FIRInventoryContains = false;
                            int total = 0;
                            foreach (string item in currentCondition.items)
                            {
                                if (tradeVolumeFIRInventory.ContainsKey(item))
                                {
                                    FIRInventoryContains = true;
                                    total += tradeVolumeFIRInventory[item];
                                }
                            }
                            objectiveInfo.GetChild(5).gameObject.SetActive(FIRInventoryContains && total > 0);

                            PointableButton pointableHandOverButton = objectiveInfo.GetChild(5).gameObject.AddComponent<PointableButton>();
                            pointableHandOverButton.SetButton();
                            pointableHandOverButton.Button.onClick.AddListener(() => { OnConditionHandOverClick(currentCondition, objectiveInfo.GetChild(5).gameObject, currentCondition.conditionType == Condition.ConditionType.HandoverItem); });
                            pointableHandOverButton.MaxPointingRange = 10;
                            pointableHandOverButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                        }

                        // Disable condition gameObject if visibility conditions not met
                        if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
                        {
                            foreach (Condition visibilityCondition in currentCondition.visibilityConditions)
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
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                        foreach (TaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                currentInitEquipHorizontal.gameObject.SetActive(true);
                            }
                            switch (reward.taskRewardType)
                            {
                                case TaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];
                                    break;
                                case TaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.SetActive(true);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                    break;
                                case TaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.SetActive(true);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TaskReward.TaskRewardType.AssortmentUnlock:
                                    foreach (string item in reward.itemIDs)
                                    {
                                        if (currentInitEquipHorizontal.childCount == 6)
                                        {
                                            currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                            currentInitEquipHorizontal.gameObject.SetActive(true);
                                        }
                                        GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                        currentInitEquipAssortElement.SetActive(true);
                                        if (Mod.itemIcons.ContainsKey(item))
                                        {
                                            currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                        }
                                        else
                                        {
                                            AnvilManager.Run(Mod.SetVanillaIcon(item, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                        }
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                        currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    // Rewards
                    Transform rewardParent = description.GetChild(3);
                    rewardParent.gameObject.SetActive(true);
                    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
                    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    currentRewardHorizontal.gameObject.SetActive(true);
                    foreach (TaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];
                                break;
                            case TaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                break;
                            case TaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TaskReward.TaskRewardType.AssortmentUnlock:
                                foreach (string item in reward.itemIDs)
                                {
                                    if (currentRewardHorizontal.childCount == 6)
                                    {
                                        currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                                        currentRewardHorizontal.gameObject.SetActive(true);
                                    }
                                    GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                    currentRewardAssortElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(item))
                                    {
                                        currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(item, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];
                                }
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
                    PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
                else if (task.taskState == Task.TaskState.Complete)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    currentTaskElement.SetActive(true);
                    task.marketUI = currentTaskElement;
                    taskListHeight += 29; // Task + Spacing

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
                    foreach (Condition currentCondition in task.completionConditions)
                    {
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
                        currentCondition.marketUI = currentObjectiveElement;

                        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
                        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
                        // Progress counter, only necessary if value > 1 and for specific condition types
                        if (currentCondition.value > 1)
                        {
                            switch (currentCondition.conditionType)
                            {
                                case Condition.ConditionType.CounterCreator:
                                    foreach (TaskCounterCondition counter in currentCondition.counters)
                                    {
                                        if (counter.counterConditionType == TaskCounterCondition.CounterConditionType.Kills)
                                        {
                                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
                                            objectiveInfo.GetChild(2).GetChild(1).GetComponent<RectTransform>().sizeDelta = new Vector2(60, 6);
                                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                            objectiveInfo.GetChild(3).GetComponent<Text>().text = currentCondition.value.ToString() + "/" + currentCondition.value;
                                            break;
                                        }
                                    }
                                    break;
                                case Condition.ConditionType.HandoverItem:
                                case Condition.ConditionType.FindItem:
                                case Condition.ConditionType.LeaveItemAtLocation:
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
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                        foreach (TaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                currentInitEquipHorizontal.gameObject.SetActive(true);
                            }
                            switch (reward.taskRewardType)
                            {
                                case TaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];
                                    break;
                                case TaskReward.TaskRewardType.TraderUnlock:
                                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipTraderUnlockElement.SetActive(true);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                    break;
                                case TaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.SetActive(true);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TaskReward.TaskRewardType.AssortmentUnlock:
                                    foreach (string item in reward.itemIDs)
                                    {
                                        if (currentInitEquipHorizontal.childCount == 6)
                                        {
                                            currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                            currentInitEquipHorizontal.gameObject.SetActive(true);
                                        }
                                        GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                        currentInitEquipAssortElement.SetActive(true);
                                        if (Mod.itemIcons.ContainsKey(item))
                                        {
                                            currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                        }
                                        else
                                        {
                                            AnvilManager.Run(Mod.SetVanillaIcon(item, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                        }
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                        currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];
                                    }
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
                    currentRewardHorizontal.gameObject.SetActive(true);
                    foreach (TaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];
                                break;
                            case TaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
                                break;
                            case TaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TaskReward.TaskRewardType.AssortmentUnlock:
                                foreach (string item in reward.itemIDs)
                                {
                                    if (currentRewardHorizontal.childCount == 6)
                                    {
                                        currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                                        currentRewardHorizontal.gameObject.SetActive(true);
                                    }
                                    GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                    currentRewardAssortElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(item))
                                    {
                                        currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(item, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    // TODO: Maybe have fail conditions and fail rewards sections

                    // Setup buttons
                    // ShortInfo
                    PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<PointableButton>();
                    pointableTaskShortInfoButton.SetButton();
                    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
                    pointableTaskShortInfoButton.MaxPointingRange = 20;
                    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    // Finish
                    PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<PointableButton>();
                    pointableTaskFinishButton.SetButton();
                    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
                    pointableTaskFinishButton.MaxPointingRange = 20;
                    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                }
                else
                {
                    task.marketUI = null;
                }
            }
            Mod.LogInfo("0");
            // Setup hoverscrolls
            if (!initButtonsSet)
            {
                HoverScroll newTaskDownHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject.AddComponent<HoverScroll>();
                HoverScroll newTaskUpHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).gameObject.AddComponent<HoverScroll>();
                newTaskDownHoverScroll.MaxPointingRange = 30;
                newTaskDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                newTaskDownHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                newTaskDownHoverScroll.other = newTaskUpHoverScroll;
                newTaskDownHoverScroll.up = false;
                newTaskUpHoverScroll.MaxPointingRange = 30;
                newTaskUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                newTaskUpHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                newTaskUpHoverScroll.other = newTaskDownHoverScroll;
                newTaskUpHoverScroll.up = true;
            }
            HoverScroll downTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<HoverScroll>();
            HoverScroll upTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<HoverScroll>();
            if (taskListHeight > 145)
            {
                downTaskHoverScroll.rate = 145 / (taskListHeight - 145);
                upTaskHoverScroll.rate = 145 / (taskListHeight - 145);
                downTaskHoverScroll.gameObject.SetActive(true);
                upTaskHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downTaskHoverScroll.gameObject.SetActive(false);
                upTaskHoverScroll.gameObject.SetActive(false);
            }
            Mod.LogInfo("0");

            // Insure
            Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject insureHorizontalCopy = insureHorizontalsParent.GetChild(0).gameObject;
            float insureShowCaseHeight = 3; // Top padding
            Mod.LogInfo("0");
            // Clear previous horizontals
            while (insureHorizontalsParent.childCount > 1)
            {
                Transform currentFirstChild = insureHorizontalsParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("0");
            // Deactivate deal button by default
            traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
            traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
            if ((bool)Mod.traderBaseDB[trader.index]["insurance"]["availability"])
            {
                Mod.LogInfo("insurance available");
                // Add all items in trade volume that are insureable at this trader to showcase
                int totalInsurePrice = 0;
                if (insureItemShowcaseElements == null)
                {
                    insureItemShowcaseElements = new Dictionary<string, GameObject>();
                }
                else
                {
                    insureItemShowcaseElements.Clear();
                }
                foreach (Transform itemTransform in this.tradeVolume.itemsRoot)
                {
                    Mod.LogInfo("processing item " + itemTransform.name);
                    MeatovItem CIW = itemTransform.GetComponent<MeatovItem>();
                    List<MarketItemView> itemViewListToUse = null;
                    string itemID;
                    int itemInsureValue;
                    bool custom = false;
                    itemViewListToUse = CIW.marketItemViews;

                    itemID = CIW.H3ID;
                    custom = true;

                    itemInsureValue = (int)Mathf.Max(CIW.GetInsuranceValue() * trader.insuranceRate, 1);

                    if (CIW.insured || !trader.ItemInsureable(itemID, CIW.parents))
                    {
                        continue;
                    }


                    Mod.LogInfo("1");
                    if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
                    {
                        Mod.LogInfo("2");
                        Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
                        Mod.LogInfo("2");
                        MarketItemView marketItemView = currentItemIcon.GetComponent<MarketItemView>();
                        Mod.LogInfo("2");
                        int actualInsureValue = Mod.traders[currentTraderIndex].currency == 0 ? itemInsureValue : (int)Mathf.Max(itemInsureValue * 0.008f, 1);
                        Mod.LogInfo("2");
                        marketItemView.insureValue = marketItemView.insureValue + actualInsureValue;
                        Mod.LogInfo("2");
                        if (marketItemView.custom)
                        {
                            Mod.LogInfo("3");
                            marketItemView.MI.Add(CIW);
                            Mod.LogInfo("3");
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.MI.Count.ToString();
                            Mod.LogInfo("3");
                        }
                        Mod.LogInfo("2");
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
                        Mod.LogInfo("2");
                        currentTotalInsurePrice += actualInsureValue;

                        // Setup itemIcon
                        ItemIcon currentItemIconScript = currentItemIcon.GetComponent<ItemIcon>();
                        currentItemIconScript.isPhysical = false;
                        currentItemIconScript.itemID = itemID;
                        currentItemIconScript.itemName = Mod.itemNames[itemID];
                        currentItemIconScript.description = Mod.itemDescriptions[itemID];
                        currentItemIconScript.weight = Mod.itemWeights[itemID];
                        currentItemIconScript.volume = Mod.itemVolumes[itemID];
                    }
                    else
                    {
                        Mod.LogInfo("5");
                        Transform currentHorizontal = insureHorizontalsParent.GetChild(insureHorizontalsParent.childCount - 1);
                        Mod.LogInfo("5");
                        if (insureHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
                        {
                            currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                        }
                        else if (currentHorizontal.childCount == 7)
                        {
                            currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                            insureShowCaseHeight += 24; // horizontal
                        }
                        Mod.LogInfo("5");

                        Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

                        // Setup itemIcon
                        ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.MI = CIW;

                        currentItemIcon.gameObject.SetActive(true);
                        Mod.LogInfo("5");
                        insureItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                        if (Mod.itemIcons.ContainsKey(itemID))
                        {
                            currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
                        }
                        MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<MarketItemView>();
                        marketItemView.custom = custom;
                        marketItemView.MI = new List<MeatovItem>() { CIW };

                        Mod.LogInfo("5");
                        // Write price to item icon and set correct currency icon
                        Sprite currencySprite = null;
                        if (trader.currency == 0)
                        {
                            currencySprite = HideoutController.roubleCurrencySprite;
                        }
                        else if (trader.currency == 1)
                        {
                            currencySprite = HideoutController.dollarCurrencySprite;
                            itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
                        }
                        Mod.LogInfo("5");
                        marketItemView.insureValue = itemInsureValue;
                        totalInsurePrice += itemInsureValue;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemInsureValue.ToString();

                        Mod.LogInfo("5");
                        //// Setup button
                        //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                        //pointableButton.SetButton();
                        //pointableButton.Button.onClick.AddListener(() => { OnInsureItemClick(currentItemIcon, itemValue, currencyItemID); });
                        //pointableButton.MaxPointingRange = 20;
                        //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                        // Add the icon object to the list for that item
                        CIW.marketItemViews.Add(marketItemView);
                        Mod.LogInfo("5");
                    }
                }
                string currencyItemID = "";
                if (Mod.traders[currentTraderIndex].currency == 0)
                {
                    currencyItemID = "203";
                }
                else if (Mod.traders[currentTraderIndex].currency == 1)
                {
                    currencyItemID = "201";
                }
                // Activate deal button
                if (tradeVolumeInventory.ContainsKey(currencyItemID) && tradeVolumeInventory[currencyItemID] >= currentTotalInsurePrice)
                {
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
                }
                else
                {
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
                }
                // Setup insure price display
                Mod.LogInfo("1");
                string insurePriceItemID = "203";
                string insurePriceItemName = "Rouble";
                if (trader.currency == 0)
                {
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["203"];
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["203"];
                }
                else if (trader.currency == 1)
                {
                    insurePriceItemID = "201";
                    insurePriceItemName = Mod.itemNames["201"];
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["201"];
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["201"];
                }
                ItemIcon traderInsurePriceItemIcon = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<ItemIcon>();
                if (traderInsurePriceItemIcon == null)
                {
                    traderInsurePriceItemIcon = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).gameObject.AddComponent<ItemIcon>();
                    traderInsurePriceItemIcon.itemID = insurePriceItemID;
                    traderInsurePriceItemIcon.itemName = insurePriceItemName;
                    traderInsurePriceItemIcon.description = Mod.itemDescriptions[insurePriceItemID];
                    traderInsurePriceItemIcon.weight = Mod.itemWeights[insurePriceItemID];
                    traderInsurePriceItemIcon.volume = Mod.itemVolumes[insurePriceItemID];
                }
                Mod.LogInfo("1");
                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = totalInsurePrice.ToString();
                currentTotalInsurePrice = totalInsurePrice;
                Mod.LogInfo("1");
                // Setup button
                if (!initButtonsSet)
                {
                    PointableButton pointableInsureDealButton = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<PointableButton>();
                    pointableInsureDealButton.SetButton();
                    pointableInsureDealButton.Button.onClick.AddListener(() => { OnInsureDealClick(); });
                    pointableInsureDealButton.MaxPointingRange = 20;
                    pointableInsureDealButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                    // Setup hoverscrolls
                    HoverScroll newInsureDownHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<HoverScroll>();
                    HoverScroll newInsureUpHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<HoverScroll>();
                    newInsureDownHoverScroll.MaxPointingRange = 30;
                    newInsureDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newInsureDownHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                    newInsureDownHoverScroll.other = newInsureUpHoverScroll;
                    newInsureDownHoverScroll.up = false;
                    newInsureUpHoverScroll.MaxPointingRange = 30;
                    newInsureUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newInsureUpHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                    newInsureUpHoverScroll.other = newInsureDownHoverScroll;
                    newInsureUpHoverScroll.up = true;
                }
                Mod.LogInfo("1");
                HoverScroll downInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
                HoverScroll upInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
                if (insureShowCaseHeight > 150)
                {
                    downInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
                    upInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
                    downInsureHoverScroll.gameObject.SetActive(true);
                    upInsureHoverScroll.gameObject.SetActive(false);
                }
                else
                {
                    downInsureHoverScroll.gameObject.SetActive(false);
                    upInsureHoverScroll.gameObject.SetActive(false);
                }
                Mod.LogInfo("1");
            }
            else
            {
                insureItemShowcaseElements = null;
            }

            Mod.LogInfo("0");
            initButtonsSet = true;
        }



        //public void UpdateTaskListHeight()
        //{
        //    Mod.LogInfo("UpdateTaskListHeight called");
        //    Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
        //    Mod.LogInfo("got trader display");
        //    HoverScroll downTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<HoverScroll>();
        //    HoverScroll upTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<HoverScroll>();
        //    Mod.LogInfo("got hoverscrolls");
        //    Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //    Mod.LogInfo("got task parents");
        //    float taskListHeight = 3 + 29 * (tasksParent.childCount - 1);

        //    // Also need to add height of open descriptions
        //    for (int i = 1; i < tasksParent.childCount; ++i)
        //    {
        //        Mod.LogInfo("\tGetting task " + i);
        //        GameObject description = tasksParent.GetChild(i).GetChild(1).gameObject;
        //        Mod.LogInfo("\tgot description");
        //        if (description.activeSelf)
        //        {
        //            // This here is why we update a frame after we need to update
        //            // Because sizeDelta is only updated the frame after any change to the UI
        //            // Since this may be the first time we activated the task's desciption,
        //            // we must wait to the frame after to get the size
        //            taskListHeight += description.GetComponent<RectTransform>().sizeDelta.y;
        //        }
        //    }
        //    Mod.LogInfo("done adding desc heights, total: " + taskListHeight);

        //    if (taskListHeight > 145)
        //    {
        //        downTaskHoverScroll.rate = 145 / (taskListHeight - 145);
        //        upTaskHoverScroll.rate = 145 / (taskListHeight - 145);
        //        downTaskHoverScroll.gameObject.SetActive(true);
        //        upTaskHoverScroll.gameObject.SetActive(false);
        //    }
        //    else
        //    {
        //        downTaskHoverScroll.gameObject.SetActive(false);
        //        upTaskHoverScroll.gameObject.SetActive(false);
        //    }
        //}

        //public void UpdateBasedOnItem(bool added, MeatovItem CIW)
        //{
        //    string itemID = CIW.H3ID;
        //    int itemValue = CIW.GetValue();
        //    int itemInsureValue = (int)Mathf.Max(CIW.GetInsuranceValue() * Mod.traders[currentTraderIndex].insuranceRate, 1);

        //    Mod.LogInfo("UpdateBasedOnItem called");
        //    if (added)
        //    {
        //        Mod.LogInfo("Added");
        //        // Add to trade volume inventory
        //        Mod.LogInfo("custom");
        //        if (tradeVolumeInventory == null)
        //        {
        //            tradeVolumeInventory = new Dictionary<string, int>();
        //            tradeVolumeInventoryObjects = new Dictionary<string, List<GameObject>>();
        //            tradeVolumeFIRInventory = new Dictionary<string, int>();
        //            tradeVolumeFIRInventoryObjects = new Dictionary<string, List<GameObject>>();
        //        }
        //        if (tradeVolumeInventory.ContainsKey(CIW.H3ID))
        //        {
        //            tradeVolumeInventory[CIW.H3ID] += CIW.maxStack > 1 ? CIW.stack : 1;
        //            tradeVolumeInventoryObjects[CIW.H3ID].Add(CIW.gameObject);
        //        }
        //        else
        //        {
        //            tradeVolumeInventory.Add(CIW.H3ID, CIW.maxStack > 1 ? CIW.stack : 1);
        //            tradeVolumeInventoryObjects.Add(CIW.H3ID, new List<GameObject>() { CIW.gameObject });
        //        }
        //        if (CIW.foundInRaid)
        //        {
        //            if (tradeVolumeFIRInventory.ContainsKey(CIW.H3ID))
        //            {
        //                tradeVolumeFIRInventory[CIW.H3ID] += CIW.maxStack > 1 ? CIW.stack : 1;
        //                tradeVolumeFIRInventoryObjects[CIW.H3ID].Add(CIW.gameObject);
        //            }
        //            else
        //            {
        //                tradeVolumeFIRInventory.Add(CIW.H3ID, CIW.maxStack > 1 ? CIW.stack : 1);
        //                tradeVolumeFIRInventoryObjects.Add(CIW.H3ID, new List<GameObject>() { CIW.gameObject });
        //            }
        //        }

        //        // IN BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
        //        Mod.LogInfo("trader buy");
        //        if (prices != null)
        //        {
        //            bool foundID = false;
        //            AssortmentPriceData foundPriceData = null;
        //            foreach (AssortmentPriceData otherAssortPriceData in prices)
        //            {
        //                if (otherAssortPriceData.ID.Equals(itemID))
        //                {
        //                    foundPriceData = otherAssortPriceData;
        //                    foundID = true;
        //                    break;
        //                }
        //            }
        //            bool matchesType = false;
        //            if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //            {
        //                matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
        //            }
        //            else
        //            {
        //                matchesType = true;
        //            }
        //            if (foundID && matchesType)
        //            {

        //                Mod.LogInfo("Updating prices");
        //                // Go through each price because need to check if all are fulfilled anyway
        //                bool canDeal = true;
        //                for (int i = 0; i < prices.Count; ++i)
        //                {
        //                    AssortmentPriceData priceData = prices[i];
        //                    if (tradeVolumeInventory.ContainsKey(priceData.ID))
        //                    {
        //                        // Find how many we have in trade inventory
        //                        // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
        //                        int matchingCountInInventory = 0;
        //                        if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //                        {
        //                            foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
        //                            {
        //                                MeatovItem priceCIW = priceObject.GetComponent<MeatovItem>();
        //                                if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
        //                                {
        //                                    ++matchingCountInInventory;
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            matchingCountInInventory = priceData.count;
        //                        }

        //                        // If this is the item we are adding, make sure the requirement fulfilled icon is active
        //                        if (matchingCountInInventory >= (priceData.count * cartItemCount))
        //                        {
        //                            if (priceData.ID.Equals(itemID))
        //                            {
        //                                Transform priceElement = buyPriceElements[i].transform;
        //                                priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
        //                                priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            canDeal = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // If this is the item we are adding, no need to make sure the unfulfilled icon is active we cause we are adding it to the inventory
        //                        // So for sure if not fulfilled now, the icon is already set to unfulfilled
        //                        canDeal = false;
        //                    }
        //                }
        //                Transform dealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0);
        //                if (canDeal)
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = true;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = false;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //                }
        //            }
        //        }

        //        Mod.LogInfo("rag buy");
        //        // IN RAGFAIR BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
        //        if (ragfairPrices != null)
        //        {
        //            bool foundID = false;
        //            AssortmentPriceData foundPriceData = null;
        //            foreach (AssortmentPriceData otherAssortPriceData in ragfairPrices)
        //            {
        //                if (otherAssortPriceData.ID.Equals(itemID))
        //                {
        //                    foundPriceData = otherAssortPriceData;
        //                    foundID = true;
        //                    break;
        //                }
        //            }
        //            bool matchesType = false;
        //            if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //            {
        //                matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
        //            }
        //            else
        //            {
        //                matchesType = true;
        //            }
        //            if (foundID && matchesType)
        //            {

        //                Mod.LogInfo("updating rag buy prices");
        //                // Go through each price because need to check if all are fulfilled anyway
        //                bool canDeal = true;
        //                for (int i = 0; i < ragfairPrices.Count; ++i)
        //                {
        //                    AssortmentPriceData priceData = ragfairPrices[i];
        //                    if (tradeVolumeInventory.ContainsKey(priceData.ID))
        //                    {
        //                        // Find how many we have in trade inventory
        //                        // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
        //                        int matchingCountInInventory = 0;
        //                        if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //                        {
        //                            foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
        //                            {
        //                                MeatovItem priceCIW = priceObject.GetComponent<MeatovItem>();
        //                                if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
        //                                {
        //                                    ++matchingCountInInventory;
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            matchingCountInInventory = priceData.count;
        //                        }

        //                        // If this is the item we are adding, make sure the requirement fulfilled icon is active
        //                        if (matchingCountInInventory >= (priceData.count * ragfairCartItemCount))
        //                        {
        //                            if (priceData.ID.Equals(itemID))
        //                            {
        //                                Transform priceElement = ragfairBuyPriceElements[i].transform;
        //                                priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
        //                                priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            canDeal = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // If this is the item we are adding, no need to make sure the unfulfilled icon is active we cause we are adding it to the inventory
        //                        // So for sure if not fulfilled now, the icon is already set to unfulfilled
        //                        canDeal = false;
        //                    }
        //                }
        //                Transform dealButton = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(2).GetChild(0).GetChild(0);
        //                if (canDeal)
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = true;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = false;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //                }
        //            }
        //        }

        //        Mod.LogInfo("trader sell");
        //        // IN SELL, check if item is already in showcase, if it is, increment count, if not, add a new entry, update price under FOR, make sure deal! button is activated if item is a sellable item
        //        if (Mod.traders[currentTraderIndex].ItemSellable(itemID, Mod.itemAncestors[itemID]))
        //        {
        //            Mod.LogInfo("updating sell showcase");
        //            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
        //            if (sellItemShowcaseElements != null && sellItemShowcaseElements.ContainsKey(itemID))
        //            {
        //                Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
        //                MarketItemView marketItemView = currentItemIcon.GetComponent<MarketItemView>();
        //                int actualValue;
        //                if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
        //                {
        //                    actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
        //                }
        //                else
        //                {
        //                    // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
        //                    actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
        //                }
        //                actualValue = Mod.traders[currentTraderIndex].currency == 0 ? actualValue : (int)Mathf.Max(actualValue * 0.008f, 1);
        //                marketItemView.value = marketItemView.value + actualValue;
        //                if (marketItemView.custom)
        //                {
        //                    marketItemView.MI.Add(CIW);
        //                    currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.MI.Count.ToString();
        //                }
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.value.ToString();
        //                currentTotalSellingPrice += actualValue;

        //                // Setup itemIcon
        //                ItemIcon currentItemIconScript = currentItemIcon.GetComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = false;
        //                currentItemIconScript.itemID = itemID;
        //                currentItemIconScript.itemName = Mod.itemNames[itemID];
        //                currentItemIconScript.description = Mod.itemDescriptions[itemID];
        //                currentItemIconScript.weight = Mod.itemWeights[itemID];
        //                currentItemIconScript.volume = Mod.itemVolumes[itemID];
        //            }
        //            else
        //            {
        //                Mod.LogInfo("adding new sell entry");
        //                // Add a new sell item entry
        //                Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //                GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;

        //                Mod.LogInfo("0");
        //                float sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
        //                Transform currentHorizontal = sellHorizontalsParent.GetChild(sellHorizontalsParent.childCount - 1);
        //                if (sellHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
        //                {
        //                    currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
        //                    currentHorizontal.gameObject.SetActive(true);
        //                }
        //                else if (currentHorizontal.childCount == 7) // If last horizontal has max number of entries already, create a new one
        //                {
        //                    currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
        //                    currentHorizontal.gameObject.SetActive(true);
        //                    sellShowCaseHeight += 24; // horizontal
        //                }
        //                Mod.LogInfo("0");

        //                Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

        //                // Setup itemIcon
        //                ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = true;
        //                currentItemIconScript.MI = CIW;

        //                currentItemIcon.gameObject.SetActive(true);
        //                Mod.LogInfo("0");
        //                sellItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
        //                if (Mod.itemIcons.ContainsKey(itemID))
        //                {
        //                    currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
        //                }
        //                else
        //                {
        //                    AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
        //                }
        //                Mod.LogInfo("0");
        //                MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<MarketItemView>();
        //                marketItemView.MI = new List<MeatovItem>() { CIW };

        //                Mod.LogInfo("0");
        //                // Write price to item icon and set correct currency icon
        //                Sprite currencySprite = null;
        //                string currencyItemID = "";
        //                int actualValue;
        //                if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
        //                {
        //                    actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
        //                }
        //                else
        //                {
        //                    // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
        //                    actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
        //                }

        //                if (currentTraderIndex == 2)
        //                {
        //                    actualValue = (int)(actualValue * 0.9f);
        //                }

        //                if (Mod.traders[currentTraderIndex].currency == 0)
        //                {
        //                    currencySprite = HideoutController.roubleCurrencySprite;
        //                    currencyItemID = "203";
        //                }
        //                else if (Mod.traders[currentTraderIndex].currency == 1)
        //                {
        //                    currencySprite = HideoutController.dollarCurrencySprite;
        //                    actualValue = (int)Mathf.Max(actualValue * 0.008f, 1); // Adjust item value
        //                    currencyItemID = "201";
        //                }
        //                Mod.LogInfo("0");
        //                marketItemView.value = actualValue;
        //                currentTotalSellingPrice += actualValue;
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = actualValue.ToString();

        //                Mod.LogInfo("0");
        //                // Set count text
        //                currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "1";

        //                //// Setup button
        //                //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
        //                //pointableButton.SetButton();
        //                //pointableButton.Button.onClick.AddListener(() => { OnSellItemClick(currentItemIcon, itemValue, currencyItemID); });
        //                //pointableButton.MaxPointingRange = 20;
        //                //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

        //                Mod.LogInfo("0");
        //                // Add the icon object to the list for that item
        //                CIW.marketItemViews.Add(marketItemView);

        //                Mod.LogInfo("0");
        //                // Update total selling price
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames[currencyItemID];
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currencyItemID];

        //                // Activate deal button
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;

        //                Mod.LogInfo("0");
        //                // Update hoverscrolls
        //                HoverScroll downSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //                HoverScroll upSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //                if (sellShowCaseHeight > 150)
        //                {
        //                    downSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
        //                    upSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
        //                    downSellHoverScroll.gameObject.SetActive(true);
        //                    upSellHoverScroll.gameObject.SetActive(false);
        //                }
        //                else
        //                {
        //                    downSellHoverScroll.gameObject.SetActive(false);
        //                    upSellHoverScroll.gameObject.SetActive(false);
        //                }
        //                Mod.LogInfo("0");
        //            }
        //            traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalSellingPrice.ToString();
        //        }

        //        Mod.LogInfo("trader tasks");
        //        // IN TASKS, for each item requirement of each task, activate TURN IN buttons accordingly
        //        if (Trader.conditionsByItem.ContainsKey(itemID))
        //        {
        //            Mod.LogInfo("updating task conditions with this item");
        //            foreach (Condition condition in Trader.conditionsByItem[itemID])
        //            {
        //                if (!condition.fulfilled && condition.marketUI != null)
        //                {
        //                    if ((CIW.foundInRaid) && condition.conditionType == Condition.ConditionType.HandoverItem)
        //                    {
        //                        condition.marketUI.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(true);
        //                    }
        //                    else if (condition.conditionType == Condition.ConditionType.WeaponAssembly)
        //                    {
        //                        // Make sure all requirements are fulfilled, set them as fulfilled by default if they werent a requirement to begin with
        //                        bool[] typesFulfilled = condition.targetAttachmentTypes.Count == 0 ? new bool[] { true } : new bool[condition.targetAttachmentTypes.Count];
        //                        bool[] attachmentsFulfilled = condition.targetAttachments.Count == 0 ? new bool[] { true } : new bool[condition.targetAttachments.Count];
        //                        bool supressedFulfilled = !condition.suppressed || (CIW.physObj as FVRFireArm).IsSuppressed();
        //                        bool brakedFulfilled = !condition.braked || (CIW.physObj as FVRFireArm).IsBraked();

        //                        // Only care to check everything if braked and supressed are both fulfilled
        //                        if (supressedFulfilled && brakedFulfilled)
        //                        {
        //                            // WeaponAssembly items must be vanilla
        //                            FVRPhysicalObject[] physObjs = CIW.GetComponentsInChildren<FVRPhysicalObject>();
        //                            foreach (FVRPhysicalObject physObj in physObjs)
        //                            {
        //                                string attachmentID = physObj.ObjectWrapper.ItemID;

        //                                // Check types
        //                                for (int i = 0; i < condition.targetAttachmentTypes.Count; ++i)
        //                                {
        //                                    if (!typesFulfilled[i])
        //                                    {
        //                                        // Format list
        //                                        List<string> actualAttachments = new List<string>();
        //                                        foreach (string attachmentTypeID in condition.targetAttachmentTypes[i])
        //                                        {
        //                                            if (Mod.itemsByParents.TryGetValue(attachmentTypeID, out List<string> items))
        //                                            {
        //                                                actualAttachments.AddRange(items);
        //                                            }
        //                                            else
        //                                            {
        //                                                actualAttachments.Add(attachmentTypeID);
        //                                            }
        //                                        }

        //                                        // Check list
        //                                        if (actualAttachments.Contains(attachmentID))
        //                                        {
        //                                            typesFulfilled[i] = true;
        //                                        }
        //                                    }
        //                                }

        //                                // Check specific attachments
        //                                for (int i = 0; i < condition.targetAttachments.Count; ++i)
        //                                {
        //                                    if (!attachmentsFulfilled[i])
        //                                    {
        //                                        if (condition.targetAttachments[i].Equals(attachmentID))
        //                                        {
        //                                            attachmentsFulfilled[i] = true;
        //                                        }
        //                                    }
        //                                }
        //                            }

        //                            if (!typesFulfilled.Contains(false) && !attachmentsFulfilled.Contains(false) && supressedFulfilled && brakedFulfilled)
        //                            {
        //                                condition.marketUI.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(true);
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        Mod.LogInfo("trader insure");
        //        // IN INSURE, check if item already in showcase, if it is, increment count, if not, add a new entry, update price, make sure deal! button is activated, check if item is price, update accordingly
        //        bool itemInsureable = Mod.traders[currentTraderIndex].ItemInsureable(itemID, Mod.itemAncestors[itemID]);

        //        if ((!CIW.insured) && itemInsureable)
        //        {
        //            Mod.LogInfo("update insure showcase");
        //            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
        //            if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
        //            {
        //                Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
        //                MarketItemView marketItemView = currentItemIcon.GetComponent<MarketItemView>();
        //                int actualInsureValue = itemInsureValue;
        //                string currencyItemID = "";
        //                if (Mod.traders[currentTraderIndex].currency == 0)
        //                {
        //                    currencyItemID = "203";
        //                }
        //                else if (Mod.traders[currentTraderIndex].currency == 1)
        //                {
        //                    itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
        //                    currencyItemID = "201";
        //                }
        //                marketItemView.insureValue = marketItemView.insureValue + actualInsureValue;
        //                if (marketItemView.custom)
        //                {
        //                    marketItemView.MI.Add(CIW);
        //                    currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.MI.Count.ToString();
        //                }
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
        //                currentTotalInsurePrice += actualInsureValue;

        //                // Activate deal button
        //                if (tradeVolumeInventory.ContainsKey(currencyItemID) && tradeVolumeInventory[currencyItemID] >= currentTotalInsurePrice)
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }

        //                // Setup itemIcon
        //                ItemIcon currentItemIconScript = currentItemIcon.GetComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = false;
        //                currentItemIconScript.itemID = itemID;
        //                currentItemIconScript.itemName = Mod.itemNames[itemID];
        //                currentItemIconScript.description = Mod.itemDescriptions[itemID];
        //                currentItemIconScript.weight = Mod.itemWeights[itemID];
        //                currentItemIconScript.volume = Mod.itemVolumes[itemID];
        //            }
        //            else
        //            {
        //                Mod.LogInfo("adding new insure entry");
        //                // Add a new insure item entry
        //                Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //                GameObject insureHorizontalCopy = insureHorizontalsParent.GetChild(0).gameObject;

        //                Mod.LogInfo("0");
        //                float insureShowCaseHeight = 3 + 24 * insureHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
        //                Transform currentHorizontal = insureHorizontalsParent.GetChild(insureHorizontalsParent.childCount - 1);
        //                if (insureHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
        //                {
        //                    currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
        //                    currentHorizontal.gameObject.SetActive(true);
        //                }
        //                else if (currentHorizontal.childCount == 7) // If last horizontal is full
        //                {
        //                    currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
        //                    currentHorizontal.gameObject.SetActive(true);
        //                    insureShowCaseHeight += 24; // horizontal
        //                }
        //                Mod.LogInfo("0");

        //                Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

        //                // Setup itemIcon
        //                ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = true;
        //                currentItemIconScript.MI = CIW;

        //                currentItemIcon.gameObject.SetActive(true);
        //                Mod.LogInfo("0");
        //                insureItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
        //                if (Mod.itemIcons.ContainsKey(itemID))
        //                {
        //                    currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
        //                }
        //                else
        //                {
        //                    AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
        //                }
        //                Mod.LogInfo("0");
        //                MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<MarketItemView>();
        //                marketItemView.MI = new List<MeatovItem>() { CIW };

        //                Mod.LogInfo("0");
        //                // Write price to item icon and set correct currency icon
        //                Sprite currencySprite = null;
        //                string currencyItemID = "";
        //                if (Mod.traders[currentTraderIndex].currency == 0)
        //                {
        //                    currencySprite = HideoutController.roubleCurrencySprite;
        //                    currencyItemID = "203";
        //                }
        //                else if (Mod.traders[currentTraderIndex].currency == 1)
        //                {
        //                    currencySprite = HideoutController.dollarCurrencySprite;
        //                    itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
        //                    currencyItemID = "201";
        //                }
        //                Mod.LogInfo("0");
        //                marketItemView.insureValue = itemInsureValue;
        //                currentTotalInsurePrice += itemInsureValue;
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
        //                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemInsureValue.ToString();

        //                // Set count text
        //                currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "1";

        //                Mod.LogInfo("0");
        //                //// Setup button
        //                //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
        //                //pointableButton.SetButton();
        //                //pointableButton.Button.onClick.AddListener(() => { OnInsureItemClick(currentItemIcon, itemValue, currencyItemID); });
        //                //pointableButton.MaxPointingRange = 20;
        //                //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

        //                // Add the icon object to the list for that item
        //                CIW.marketItemViews.Add(marketItemView);
        //                Mod.LogInfo("0");

        //                // Update total insure price
        //                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames[currencyItemID];
        //                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currencyItemID];

        //                // Activate deal button
        //                if (tradeVolumeInventory.ContainsKey(currencyItemID) && tradeVolumeInventory[currencyItemID] >= currentTotalInsurePrice)
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }

        //                Mod.LogInfo("0");
        //                // Update hoverscrolls
        //                HoverScroll downInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //                HoverScroll upInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //                if (insureShowCaseHeight > 150)
        //                {
        //                    downInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
        //                    upInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
        //                    downInsureHoverScroll.gameObject.SetActive(true);
        //                    upInsureHoverScroll.gameObject.SetActive(false);
        //                }
        //                else
        //                {
        //                    downInsureHoverScroll.gameObject.SetActive(false);
        //                    upInsureHoverScroll.gameObject.SetActive(false);
        //                }
        //                Mod.LogInfo("0");
        //            }
        //            traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalInsurePrice.ToString();
        //        }
        //        Transform insurePriceFulfilledIcons = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(2);
        //        Transform insureDealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0);

        //        if (itemInsureable || itemID.Equals("201") || itemID.Equals("203"))
        //        {
        //            if (Mod.traders[currentTraderIndex].currency == 0)
        //            {
        //                if (tradeVolumeInventory.ContainsKey("203") && tradeVolumeInventory["203"] >= currentTotalInsurePrice)
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

        //                    insureDealButton.GetComponent<Collider>().enabled = true;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

        //                    insureDealButton.GetComponent<Collider>().enabled = false;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }
        //            }
        //            else if (Mod.traders[currentTraderIndex].currency == 1)
        //            {
        //                if (tradeVolumeInventory.ContainsKey("201") && tradeVolumeInventory["201"] >= currentTotalInsurePrice)
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

        //                    insureDealButton.GetComponent<Collider>().enabled = true;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

        //                    insureDealButton.GetComponent<Collider>().enabled = false;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Mod.LogInfo("removed");
        //        // Remove from trade volume inventory
        //        Mod.LogInfo("custom, pre amount: " + tradeVolumeInventory[CIW.H3ID]);
        //        tradeVolumeInventory[CIW.H3ID] -= (CIW.maxStack > 1 ? CIW.stack : 1);
        //        tradeVolumeInventoryObjects[CIW.H3ID].Remove(CIW.gameObject);
        //        Mod.LogInfo("post amount: " + tradeVolumeInventory[CIW.H3ID]);
        //        if (tradeVolumeInventory[CIW.H3ID] == 0)
        //        {
        //            tradeVolumeInventory.Remove(CIW.H3ID);
        //            tradeVolumeInventoryObjects.Remove(CIW.H3ID);
        //        }
        //        if (CIW.foundInRaid)
        //        {
        //            tradeVolumeFIRInventory[CIW.H3ID] -= (CIW.maxStack > 1 ? CIW.stack : 1);
        //            tradeVolumeFIRInventoryObjects[CIW.H3ID].Remove(CIW.gameObject);
        //            if (tradeVolumeFIRInventory[CIW.H3ID] == 0)
        //            {
        //                tradeVolumeFIRInventory.Remove(CIW.H3ID);
        //                tradeVolumeFIRInventoryObjects.Remove(CIW.H3ID);
        //            }
        //        }

        //        Mod.LogInfo("updating trader buy");
        //        // IN BUY, check if item corresponds to price, update fulfilled icon and deactivate deal! button if necessary
        //        if (prices != null)
        //        {
        //            bool foundID = false;
        //            AssortmentPriceData foundPriceData = null;
        //            foreach (AssortmentPriceData otherAssortPriceData in prices)
        //            {
        //                if (otherAssortPriceData.ID.Equals(itemID))
        //                {
        //                    foundPriceData = otherAssortPriceData;
        //                    foundID = true;
        //                    break;
        //                }
        //            }
        //            bool matchesType = false;
        //            if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //            {
        //                matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
        //            }
        //            else
        //            {
        //                matchesType = true;
        //            }
        //            if (foundID && matchesType)
        //            {
        //                // Go through each price because need to check if all are fulfilled anyway
        //                bool canDeal = true;
        //                for (int i = 0; i < prices.Count; ++i)
        //                {
        //                    AssortmentPriceData priceData = prices[i];
        //                    if (tradeVolumeInventory.ContainsKey(priceData.ID))
        //                    {
        //                        // Find how many we have in trade inventory
        //                        // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
        //                        int matchingCountInInventory = 0;
        //                        if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //                        {
        //                            foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
        //                            {
        //                                MeatovItem priceCIW = priceObject.GetComponent<MeatovItem>();
        //                                if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
        //                                {
        //                                    ++matchingCountInInventory;
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            matchingCountInInventory = priceData.count;
        //                        }

        //                        // If this is the item we are removing, make sure the requirement fulfilled icon is active
        //                        if (matchingCountInInventory < (priceData.count * cartItemCount))
        //                        {
        //                            // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
        //                            if (priceData.ID.Equals(itemID))
        //                            {
        //                                Transform priceElement = buyPriceElements[i].transform;
        //                                priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                                priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                            }
        //                            canDeal = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
        //                        if (priceData.ID.Equals(itemID))
        //                        {
        //                            Transform priceElement = buyPriceElements[i].transform;
        //                            priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                            priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        canDeal = false;
        //                    }
        //                }
        //                Transform dealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0);
        //                if (canDeal)
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = true;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = false;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //                }
        //            }
        //        }

        //        Mod.LogInfo("0");
        //        // IN RAGFAIR BUY, check if item corresponds to price, update fulfilled icon and deactivate deal! button if necessary
        //        if (ragfairPrices != null)
        //        {
        //            bool foundID = false;
        //            AssortmentPriceData foundPriceData = null;
        //            foreach (AssortmentPriceData otherAssortPriceData in ragfairPrices)
        //            {
        //                if (otherAssortPriceData.ID.Equals(itemID))
        //                {
        //                    foundPriceData = otherAssortPriceData;
        //                    foundID = true;
        //                    break;
        //                }
        //            }
        //            bool matchesType = false;
        //            if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //            {
        //                matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
        //            }
        //            else
        //            {
        //                matchesType = true;
        //            }
        //            if (foundID && matchesType)
        //            {
        //                // Go through each price because need to check if all are fulfilled anyway
        //                bool canDeal = true;
        //                for (int i = 0; i < ragfairPrices.Count; ++i)
        //                {
        //                    AssortmentPriceData priceData = ragfairPrices[i];
        //                    if (tradeVolumeInventory.ContainsKey(priceData.ID))
        //                    {
        //                        // Find how many we have in trade inventory
        //                        // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
        //                        int matchingCountInInventory = 0;
        //                        if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //                        {
        //                            foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
        //                            {
        //                                MeatovItem priceCIW = priceObject.GetComponent<MeatovItem>();
        //                                if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
        //                                {
        //                                    ++matchingCountInInventory;
        //                                }
        //                            }
        //                        }
        //                        else
        //                        {
        //                            matchingCountInInventory = priceData.count;
        //                        }

        //                        // If this is the item we are removing, make sure the requirement fulfilled icon is active
        //                        if (matchingCountInInventory < (priceData.count * ragfairCartItemCount))
        //                        {
        //                            // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
        //                            if (priceData.ID.Equals(itemID))
        //                            {
        //                                Transform priceElement = ragfairBuyPriceElements[i].transform;
        //                                priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                                priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                            }
        //                            canDeal = false;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
        //                        if (priceData.ID.Equals(itemID))
        //                        {
        //                            Transform priceElement = ragfairBuyPriceElements[i].transform;
        //                            priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
        //                            priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
        //                        }
        //                        canDeal = false;
        //                    }
        //                }
        //                Transform dealButton = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(2).GetChild(0).GetChild(0);
        //                if (canDeal)
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = true;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    dealButton.GetComponent<Collider>().enabled = false;
        //                    dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //                }
        //            }
        //        }

        //        Mod.LogInfo("0");
        //        // IN SELL, find item in showcase, if there are more its stack, decrement count, if not, remove entry, update price under FOR, make sure deal! button is deactivated if no sellable item in volume (only need to check this if this item was sellable)
        //        Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
        //        if (sellItemShowcaseElements != null && sellItemShowcaseElements.ContainsKey(itemID))
        //        {
        //            Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //            float sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
        //            Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
        //            MarketItemView marketItemView = currentItemIcon.GetComponent<MarketItemView>();
        //            int actualValue;
        //            if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
        //            {
        //                actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
        //            }
        //            else
        //            {
        //                // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
        //                actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
        //            }
        //            if (currentTraderIndex == 2)
        //            {
        //                actualValue = (int)(actualValue * 0.9f);
        //            }
        //            actualValue = Mod.traders[currentTraderIndex].currency == 0 ? actualValue : (int)Mathf.Max(actualValue * 0.008f, 1);
        //            marketItemView.value = marketItemView.value -= actualValue;
        //            bool shouldRemove = false;
        //            bool lastOne = false;
        //            marketItemView.MI.Remove(CIW);
        //            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.MI.Count.ToString();
        //            shouldRemove = marketItemView.MI.Count == 0;
        //            lastOne = marketItemView.MI.Count == 1;
        //            currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.value.ToString();
        //            currentTotalSellingPrice -= actualValue;

        //            // Setup itemIcon
        //            if (lastOne)
        //            {
        //                ItemIcon currentItemIconScript = currentItemIcon.GetComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = true;
        //                currentItemIconScript.MI = CIW;
        //            }

        //            // Remove item if necessary and move all other item icons that come after
        //            if (shouldRemove)
        //            {
        //                currentItemIcon.SetParent(null);
        //                Destroy(currentItemIcon.gameObject);
        //                sellItemShowcaseElements.Remove(itemID);

        //                for (int i = 1; i < sellHorizontalsParent.childCount - 1; ++i)
        //                {
        //                    Transform currentHorizontal = sellHorizontalsParent.GetChild(i);
        //                    // If item icon missing from this horizontal but not thi is not the last horizontal
        //                    if (currentHorizontal.childCount == 6 && i < sellHorizontalsParent.childCount - 2)
        //                    {
        //                        // Take item icon from next horizontal and put it on current
        //                        sellHorizontalsParent.GetChild(i + 1).GetChild(0).SetParent(currentHorizontal);
        //                    }
        //                    else if (currentHorizontal.childCount == 0)
        //                    {
        //                        currentHorizontal.SetParent(null);
        //                        Destroy(currentHorizontal.gameObject);

        //                        sellShowCaseHeight -= 24;
        //                        break; // we can break here because if there are 0 it means there wasnt another horizontal after it
        //                    }
        //                }
        //            }

        //            // Deactivate deal button if no sellable items
        //            if (sellHorizontalsParent.childCount == 1)
        //            {
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
        //                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //            }

        //            // Update hoverscrolls
        //            HoverScroll downSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //            HoverScroll upSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //            if (sellShowCaseHeight > 150)
        //            {
        //                downSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
        //                upSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
        //                downSellHoverScroll.gameObject.SetActive(true);
        //                upSellHoverScroll.gameObject.SetActive(false);
        //            }
        //            else
        //            {
        //                downSellHoverScroll.gameObject.SetActive(false);
        //                upSellHoverScroll.gameObject.SetActive(false);
        //            }
        //        }
        //        // else, if we are removing an item and it is not a sellable item (not in sellItemShowcaseElements) we dont need to do anything
        //        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalSellingPrice.ToString();

        //        Mod.LogInfo("0");
        //        // IN TASKS, for each item requirement of each task, deactivate TURN IN buttons accordingly
        //        if (Trader.conditionsByItem.ContainsKey(itemID))
        //        {
        //            foreach (Condition condition in Trader.conditionsByItem[itemID])
        //            {
        //                if ((condition.conditionType == Condition.ConditionType.HandoverItem || condition.conditionType == Condition.ConditionType.WeaponAssembly) &&
        //                    !condition.fulfilled && condition.marketUI != null)
        //                {
        //                    if (!tradeVolumeFIRInventory.ContainsKey(itemID) || tradeVolumeFIRInventory[itemID] == 0)
        //                    {
        //                        condition.marketUI.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(false);
        //                    }
        //                }
        //            }
        //        }

        //        Mod.LogInfo("0");
        //        // IN INSURE, find item in showcase, if there are more its stack, decrement count, if not, remove entry, update price under FOR, make sure deal! button is deactivated if no insureable item in volume (only need to check this if this item was insureable)
        //        bool insureable = false;
        //        if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
        //        {
        //            insureable = true;
        //            Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //            float insureShowCaseHeight = 3 + 24 * insureHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
        //            Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
        //            MarketItemView marketItemView = currentItemIcon.GetComponent<MarketItemView>();
        //            int actualInsureValue = Mod.traders[currentTraderIndex].currency == 0 ? itemInsureValue : (int)Mathf.Max(itemInsureValue * 0.008f, 1);
        //            marketItemView.insureValue = marketItemView.insureValue -= actualInsureValue;
        //            bool shouldRemove = false;
        //            bool lastOne = false;
        //            marketItemView.MI.Remove(CIW);
        //            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.MI.Count.ToString();
        //            shouldRemove = marketItemView.MI.Count == 0;
        //            lastOne = marketItemView.MI.Count == 1;
        //            currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
        //            currentTotalInsurePrice -= actualInsureValue;

        //            // Setup itemIcon
        //            if (lastOne)
        //            {
        //                ItemIcon currentItemIconScript = currentItemIcon.GetComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = true;
        //                currentItemIconScript.MI = CIW;
        //            }

        //            // Remove item if necessary and move all other item icons that come after
        //            if (shouldRemove)
        //            {
        //                currentItemIcon.SetParent(null);
        //                Destroy(currentItemIcon.gameObject);
        //                insureItemShowcaseElements.Remove(itemID);

        //                for (int i = 1; i < insureHorizontalsParent.childCount - 1; ++i)
        //                {
        //                    Transform currentHorizontal = insureHorizontalsParent.GetChild(i);
        //                    // If item icon missing from this horizontal but not thi is not the last horizontal
        //                    if (currentHorizontal.childCount == 6 && i < insureHorizontalsParent.childCount - 2)
        //                    {
        //                        // Take item icon from next horizontal and put it on current
        //                        insureHorizontalsParent.GetChild(i + 1).GetChild(0).SetParent(currentHorizontal);
        //                    }
        //                    else if (currentHorizontal.childCount == 0)
        //                    {
        //                        currentHorizontal.SetParent(null);
        //                        Destroy(currentHorizontal.gameObject);

        //                        insureShowCaseHeight -= 24;
        //                        break; // we can break here because if there are 0 it means there wasnt another horizontal after it
        //                    }
        //                }
        //            }

        //            // Deactivate deal button if no insureable items
        //            if (insureHorizontalsParent.childCount == 1)
        //            {
        //                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
        //                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //            }
        //            else
        //            {
        //                string currencyItemID = "";
        //                if (Mod.traders[currentTraderIndex].currency == 0)
        //                {
        //                    currencyItemID = "203";
        //                }
        //                else if (Mod.traders[currentTraderIndex].currency == 1)
        //                {
        //                    currencyItemID = "201";
        //                }
        //                // Activate deal button
        //                if (tradeVolumeInventory.ContainsKey(currencyItemID) && tradeVolumeInventory[currencyItemID] >= currentTotalInsurePrice)
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
        //                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }
        //            }

        //            // Update hoverscrolls
        //            HoverScroll downInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //            HoverScroll upInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //            if (insureShowCaseHeight > 150)
        //            {
        //                downInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
        //                upInsureHoverScroll.rate = 150 / (insureShowCaseHeight - 150);
        //                downInsureHoverScroll.gameObject.SetActive(true);
        //                upInsureHoverScroll.gameObject.SetActive(false);
        //            }
        //            else
        //            {
        //                downInsureHoverScroll.gameObject.SetActive(false);
        //                upInsureHoverScroll.gameObject.SetActive(false);
        //            }
        //        }
        //        // else, if we are removing an item and it is not an insureable item (not in insureItemShowcaseElements) we dont need to do anything
        //        traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalInsurePrice.ToString();
        //        Transform insurePriceFulfilledIcons = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(2);
        //        Transform insureDealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0);

        //        Mod.LogInfo("0");
        //        if (insureable || itemID.Equals("201") || itemID.Equals("203"))
        //        {
        //            if (Mod.traders[currentTraderIndex].currency == 0)
        //            {
        //                if (tradeVolumeInventory.ContainsKey("203") && tradeVolumeInventory["203"] >= currentTotalInsurePrice)
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

        //                    insureDealButton.GetComponent<Collider>().enabled = true;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

        //                    insureDealButton.GetComponent<Collider>().enabled = false;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }
        //            }
        //            else if (Mod.traders[currentTraderIndex].currency == 1)
        //            {
        //                if (tradeVolumeInventory.ContainsKey("201") && tradeVolumeInventory["201"] >= currentTotalInsurePrice)
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

        //                    insureDealButton.GetComponent<Collider>().enabled = true;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //                }
        //                else
        //                {
        //                    insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
        //                    insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

        //                    insureDealButton.GetComponent<Collider>().enabled = false;
        //                    insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
        //                }
        //            }
        //        }
        //    }
        //}

        //public void UpdateBasedOnPlayerLevel()
        //{
        //    SetTrader(currentTraderIndex);

        //    // TODO: Will also need to check if level 15 then we also want to add player items to flea market
        //}

        public void OnBuyItemClick(MeatovItemData item, BarterPrice[] priceList)
        {
            cartItem = item;

            if (buyItemPriceViewsByH3ID == null)
            {
                buyItemPriceViewsByH3ID = new Dictionary<string, PriceItemView>();
            }
            else
            {
                buyItemPriceViewsByH3ID.Clear();
            }

            if (item == null)
            {
                cartItemCount = -1;
                buyPrices = null;

                buyItemView.itemView.SetItemData(null);
                buyItemView.itemName.text = "";
                buyItemCount.gameObject.SetActive(false);

                while (buyPricesContent.childCount > 1)
                {
                    Transform currentFirstChild = buyPricesContent.GetChild(1);
                    currentFirstChild.SetParent(null);
                    Destroy(currentFirstChild.gameObject);
                }

                buyDealButton.SetActive(false);
            }
            else
            {
                cartItemCount = 1;
                buyPrices = new List<BarterPrice>(priceList);
                Mod.LogInfo("on buy item click called, with ID: " + item.H3ID);
                Mod.LogInfo("Got item name: " + item.name);

                buyItemView.itemView.SetItemData(item);
                buyItemView.itemName.text = item.name;
                buyItemCount.gameObject.SetActive(true);
                buyItemCount.text = "1";

                while (buyPricesContent.childCount > 1)
                {
                    Transform currentFirstChild = buyPricesContent.GetChild(1);
                    currentFirstChild.SetParent(null);
                    Destroy(currentFirstChild.gameObject);
                }

                bool canDeal = true;
                foreach (BarterPrice price in priceList)
                {
                    Mod.LogInfo("\tSetting price: " + price.itemData.H3ID);
                    Transform priceElement = Instantiate(buyPricePrefab, buyPricesContent).transform;
                    priceElement.gameObject.SetActive(true);
                    PriceItemView currentPriceView = priceElement.GetComponent<PriceItemView>();
                    currentPriceView.price = price;

                    currentPriceView.amount.text = price.count.ToString();
                    currentPriceView.itemName.text = price.itemData.name.ToString();
                    if (price.itemData.itemType == MeatovItem.ItemType.DogTag)
                    {
                        currentPriceView.itemView.SetItemData(price.itemData, false, false, true, ">= lvl " + price.dogTagLevel);
                    }
                    else
                    {
                        currentPriceView.itemView.SetItemData(price.itemData);
                    }

                    int count = 0;
                    tradeVolume.inventory.TryGetValue(price.itemData.H3ID, out count);
                    currentPriceView.amount.text = Mathf.Min(price.count, count).ToString() + "/" + price.count.ToString();

                    if (count >= price.count)
                    {
                        currentPriceView.fulfilledIcon.SetActive(true);
                        currentPriceView.unfulfilledIcon.SetActive(false);
                    }
                    else
                    {
                        currentPriceView.fulfilledIcon.SetActive(false);
                        currentPriceView.unfulfilledIcon.SetActive(true);
                        canDeal = false;
                    }

                    buyItemPriceViewsByH3ID.Add(price.itemData.H3ID, currentPriceView);
                }

                buyDealButton.SetActive(canDeal);
            }
        }

        public void OnBuyDealClick()
        {
            // Remove price from trade volume
            foreach (BarterPrice price in buyPrices)
            {
                RemoveItemFromTrade(price.itemData, price.count * cartItemCount, price.dogTagLevel);
            }

            // Add bought amount of item to trade volume
            SpawnItem(cartItem, cartItemCount);

            // Update amount of item in trader's assort
            //Mod.traders[currentTraderIndex].assortmentByLevel[Mod.traders[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack -= cartItemCount;
        }

        public IEnumerator SpawnVanillaItem(MeatovItemData itemData, int count)
        {
            yield return IM.OD[itemData.H3ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[itemData.H3ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.LogError("Failed to get vanilla prefab for " + itemData.H3ID+" to spawn in market");
                yield break;
            }
            FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
            GameObject itemObject = null;
            if (physObj is FVRFireArmRound && count > 1) // Multiple rounds, must spawn an ammobox
            {
                int countLeft = count;
                float boxCountLeft = count / 120.0f;
                while (boxCountLeft > 0)
                {
                    int amount = 0;
                    if (countLeft > 30)
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(716));

                        if (countLeft <= 120)
                        {
                            amount = countLeft;
                            countLeft = 0;
                        }
                        else
                        {
                            amount = 120;
                            countLeft -= 120;
                        }
                    }
                    else
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(715));

                        amount = countLeft;
                        countLeft = 0;
                    }

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();
                    FVRFireArmMagazine asMagazine = meatovItem.physObj as FVRFireArmMagazine;
                    FVRFireArmRound round = physObj as FVRFireArmRound;
                    asMagazine.RoundType = round.RoundType;
                    meatovItem.roundClass = round.RoundClass;
                    for (int j = 0; j < amount; ++j)
                    {
                        asMagazine.AddRound(meatovItem.roundClass, false, false);
                    }

                    // Add item to tradevolume
                    tradeVolume.AddItem(meatovItem);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.x / 2, tradeVolume.activeVolume.transform.localScale.x / 2),
                                                                        UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.y / 2, tradeVolume.activeVolume.transform.localScale.y / 2),
                                                                        UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.z / 2, tradeVolume.activeVolume.transform.localScale.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

                    boxCountLeft = countLeft / 120.0f;
                }
            }
            else // Not a round, or just 1, spawn as normal
            {
                for (int i = 0; i < count; ++i)
                {
                    itemObject = GameObject.Instantiate(itemPrefab);

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();

                    // Add item to tradevolume
                    tradeVolume.AddItem(meatovItem);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.x / 2, tradeVolume.activeVolume.transform.localScale.x / 2),
                                                                     UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.y / 2, tradeVolume.activeVolume.transform.localScale.y / 2),
                                                                     UnityEngine.Random.Range(-tradeVolume.activeVolume.transform.localScale.z / 2, tradeVolume.activeVolume.transform.localScale.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;
                }
            }

            yield break;
        }

        public void OnBuyAmountClick()
        {
            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                Mod.splittingItem.CancelSplit();
            }

            // Disable buy amount buttons until done choosing amount
            buyDealButton.SetActive(false);

            // Start choosing amount
            Mod.stackSplitUI.SetActive(true);
            Mod.stackSplitUI.transform.localPosition = Mod.rightHand.transform.localPosition + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.localRotation = Quaternion.Euler(0, Mod.rightHand.transform.localRotation.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.localPosition;
            amountChoiceRightVector = Mod.rightHand.transform.right;
            amountChoiceRightVector.y = 0;

            choosingBuyAmount = true;
            startedChoosingThisFrame = true;

            // Set max buy amount, limit it to 360 otherwise scale is not large enough and its hard to specify an exact value
            maxBuyAmount = 360;
        }

        public void OnSellDealClick()
        {
            // Remove all sellable items from trade volume
            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < volumeItemEntry.Value.Count; ++i)
                {
                    MeatovItem meatovItem = volumeItemEntry.Value[i];

                    if (Mod.traders[currentTraderIndex].ItemSellable(meatovItem.itemData))
                    {
                        Destroy(meatovItem.gameObject);
                    }
                }
            }

            // Add sold for item to trade volume
            Trader trader = Mod.traders[currentTraderIndex];
            string currencyID = trader.currency == 0 ? "203" : (trader.currency == 1 ? "201" : "202");
            MeatovItemData currencyItemData;
            Mod.GetItemData(currencyID, out currencyItemData);
            SpawnItem(currencyItemData, currentTotalSellingPrice);

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

        ////public void OnInsureItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        ////{
        ////    // TODO: MAYBE DONT EVEN NEED THIS, WE JUST INSURE EVERYTHING IN THE INSURE SHOWCASE
        ////    // TODO: Set cart UI to this item
        ////    // de/activate deal! button depending on whether trade volume has enough money
        ////}

        //public void OnInsureDealClick()
        //{
        //    // Set all insureable items in trade volume as insured
        //    foreach (KeyValuePair<string, int> item in tradeVolumeInventory)
        //    {
        //        if (Mod.traders[currentTraderIndex].ItemInsureable(item.Key, Mod.itemAncestors[item.Key]))
        //        {
        //            foreach (GameObject itemObject in tradeVolumeInventoryObjects[item.Key])
        //            {
        //                MeatovItem CIW = itemObject.GetComponent<MeatovItem>();
        //                CIW.insured = true;
        //            }
        //        }
        //    }

        //    // Remove price from trade volume
        //    int amountToRemove = currentTotalInsurePrice;
        //    string currencyID = Mod.traders[currentTraderIndex].currency == 0 ? "203" : "201"; // Roubles, else USD
        //    RemoveItemFromTrade(currencyID, amountToRemove);

        //    // Update trader UI
        //    SetTrader(currentTraderIndex);
        //}

        //public void OnTaskShortInfoClick(GameObject description)
        //{
        //    // Toggle task description
        //    description.SetActive(!description.activeSelf);
        //    clickAudio.Play();

        //    if (description.activeSelf)
        //    {
        //        // Set to call UpdateTaskListHeight on next frame because we will need description height but that will only be accessible next frame
        //        mustUpdateTaskListHeight = 1;
        //    }
        //    else
        //    {
        //        // If deactivated we can update height right away
        //        UpdateTaskListHeight();
        //    }
        //}

        //public void AddTask(Task task)
        //{
        //    Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
        //    Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //    GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
        //    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
        //    currentTaskElement.SetActive(true);
        //    task.marketUI = currentTaskElement;

        //    // Short info
        //    Transform shortInfo = currentTaskElement.transform.GetChild(0);
        //    shortInfo.GetChild(0).GetChild(0).GetComponent<Text>().text = task.name;
        //    shortInfo.GetChild(1).GetChild(0).GetComponent<Text>().text = task.location;

        //    // Description
        //    Transform description = currentTaskElement.transform.GetChild(1);
        //    description.GetChild(0).GetComponent<Text>().text = task.description;
        //    // Objectives (conditions)
        //    Transform objectivesParent = description.GetChild(1).GetChild(1);
        //    GameObject objectiveTemplate = objectivesParent.GetChild(0).gameObject;
        //    foreach (Condition currentCondition in task.completionConditions)
        //    {
        //        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
        //        currentObjectiveElement.SetActive(true);
        //        currentCondition.marketUI = currentObjectiveElement;

        //        Transform objectiveInfo = currentObjectiveElement.transform.GetChild(0).GetChild(0);
        //        objectiveInfo.GetChild(1).GetComponent<Text>().text = currentCondition.text;
        //        // Progress counter, only necessary if value > 1 and for specific condition types
        //        if (currentCondition.value > 1)
        //        {
        //            switch (currentCondition.conditionType)
        //            {
        //                case Condition.ConditionType.CounterCreator:
        //                    foreach (TaskCounterCondition counter in currentCondition.counters)
        //                    {
        //                        if (counter.counterConditionType == TaskCounterCondition.CounterConditionType.Kills)
        //                        {
        //                            objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
        //                            objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
        //                            objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
        //                            break;
        //                        }
        //                    }
        //                    break;
        //                case Condition.ConditionType.HandoverItem:
        //                case Condition.ConditionType.FindItem:
        //                case Condition.ConditionType.LeaveItemAtLocation:
        //                    objectiveInfo.GetChild(2).gameObject.SetActive(true); // Activate progress bar
        //                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
        //                    objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }

        //        // Disable condition gameObject if visibility conditions not met
        //        if (currentCondition.visibilityConditions != null && currentCondition.visibilityConditions.Count > 0)
        //        {
        //            foreach (Condition visibilityCondition in currentCondition.visibilityConditions)
        //            {
        //                if (!visibilityCondition.fulfilled)
        //                {
        //                    currentObjectiveElement.SetActive(false);
        //                    break;
        //                }
        //            }
        //        }
        //    }
        //    // Initial equipment
        //    if (task.startingEquipment != null && task.startingEquipment.Count > 0)
        //    {
        //        Transform initEquipParent = description.GetChild(2);
        //        initEquipParent.gameObject.SetActive(true);
        //        GameObject currentInitEquipHorizontalTemplate = initEquipParent.GetChild(1).gameObject;
        //        Transform currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
        //        currentInitEquipHorizontal.gameObject.SetActive(true);
        //        foreach (TaskReward reward in task.startingEquipment)
        //        {
        //            // Add new horizontal if necessary
        //            if (currentInitEquipHorizontal.childCount == 6)
        //            {
        //                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
        //                currentInitEquipHorizontal.gameObject.SetActive(true);
        //            }
        //            switch (reward.taskRewardType)
        //            {
        //                case TaskReward.TaskRewardType.Item:
        //                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
        //                    currentInitEquipItemElement.SetActive(true);
        //                    if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
        //                    {
        //                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
        //                    }
        //                    else
        //                    {
        //                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //                    }
        //                    if (reward.amount > 1)
        //                    {
        //                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
        //                    }
        //                    else
        //                    {
        //                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        //                    }
        //                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];

        //                    // Setup ItemIcon
        //                    ItemIcon itemIconScript = currentInitEquipItemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                    itemIconScript.itemID = reward.itemIDs[0];
        //                    itemIconScript.itemName = Mod.itemNames[reward.itemIDs[0]];
        //                    itemIconScript.description = Mod.itemDescriptions[reward.itemIDs[0]];
        //                    itemIconScript.weight = Mod.itemWeights[reward.itemIDs[0]];
        //                    itemIconScript.volume = Mod.itemVolumes[reward.itemIDs[0]];
        //                    break;
        //                case TaskReward.TaskRewardType.TraderUnlock:
        //                    GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
        //                    currentInitEquipTraderUnlockElement.SetActive(true);
        //                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
        //                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
        //                    break;
        //                case TaskReward.TaskRewardType.TraderStanding:
        //                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
        //                    currentInitEquipStandingElement.SetActive(true);
        //                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
        //                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
        //                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
        //                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
        //                    break;
        //                case TaskReward.TaskRewardType.Experience:
        //                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
        //                    currentInitEquipExperienceElement.SetActive(true);
        //                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
        //                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
        //                    break;
        //                case TaskReward.TaskRewardType.AssortmentUnlock:
        //                    foreach (string item in reward.itemIDs)
        //                    {
        //                        if (currentInitEquipHorizontal.childCount == 6)
        //                        {
        //                            currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
        //                            currentInitEquipHorizontal.gameObject.SetActive(true);
        //                        }
        //                        GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
        //                        currentInitEquipAssortElement.SetActive(true);
        //                        if (Mod.itemIcons.ContainsKey(item))
        //                        {
        //                            currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
        //                        }
        //                        else
        //                        {
        //                            AnvilManager.Run(Mod.SetVanillaIcon(item, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //                        }
        //                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        //                        currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

        //                        // Setup ItemIcon
        //                        ItemIcon assortIconScript = currentInitEquipAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                        assortIconScript.itemID = item;
        //                        assortIconScript.itemName = Mod.itemNames[item];
        //                        assortIconScript.description = Mod.itemDescriptions[item];
        //                        assortIconScript.weight = Mod.itemWeights[item];
        //                        assortIconScript.volume = Mod.itemVolumes[item];
        //                    }
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //    // Rewards
        //    Transform rewardParent = description.GetChild(2);
        //    rewardParent.gameObject.SetActive(true);
        //    GameObject currentRewardHorizontalTemplate = rewardParent.GetChild(1).gameObject;
        //    Transform currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
        //    currentRewardHorizontal.gameObject.SetActive(true);
        //    foreach (TaskReward reward in task.successRewards)
        //    {
        //        // Add new horizontal if necessary
        //        if (currentRewardHorizontal.childCount == 6)
        //        {
        //            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
        //            currentRewardHorizontal.gameObject.SetActive(true);
        //        }
        //        switch (reward.taskRewardType)
        //        {
        //            case TaskReward.TaskRewardType.Item:
        //                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
        //                currentRewardItemElement.SetActive(true);
        //                if (Mod.itemIcons.ContainsKey(reward.itemIDs[0]))
        //                {
        //                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemIDs[0]];
        //                }
        //                else
        //                {
        //                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemIDs[0], currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //                }
        //                if (reward.amount > 1)
        //                {
        //                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
        //                }
        //                else
        //                {
        //                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        //                }
        //                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemIDs[0]];

        //                // Setup ItemIcon
        //                ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                itemIconScript.itemID = reward.itemIDs[0];
        //                itemIconScript.itemName = Mod.itemNames[reward.itemIDs[0]];
        //                itemIconScript.description = Mod.itemDescriptions[reward.itemIDs[0]];
        //                itemIconScript.weight = Mod.itemWeights[reward.itemIDs[0]];
        //                itemIconScript.volume = Mod.itemVolumes[reward.itemIDs[0]];
        //                break;
        //            case TaskReward.TaskRewardType.TraderUnlock:
        //                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
        //                currentRewardTraderUnlockElement.SetActive(true);
        //                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = HideoutController.traderAvatars[reward.traderIndex];
        //                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traders[reward.traderIndex].name;
        //                break;
        //            case TaskReward.TaskRewardType.TraderStanding:
        //                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
        //                currentRewardStandingElement.SetActive(true);
        //                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.standingSprite;
        //                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
        //                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traders[reward.traderIndex].name;
        //                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
        //                break;
        //            case TaskReward.TaskRewardType.Experience:
        //                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
        //                currentRewardExperienceElement.SetActive(true);
        //                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = HideoutController.experienceSprite;
        //                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
        //                break;
        //            case TaskReward.TaskRewardType.AssortmentUnlock:
        //                foreach (string item in reward.itemIDs)
        //                {
        //                    if (currentRewardHorizontal.childCount == 6)
        //                    {
        //                        currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
        //                        currentRewardHorizontal.gameObject.SetActive(true);
        //                    }
        //                    GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
        //                    currentRewardAssortElement.SetActive(true);
        //                    if (Mod.itemIcons.ContainsKey(item))
        //                    {
        //                        currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[item];
        //                    }
        //                    else
        //                    {
        //                        AnvilManager.Run(Mod.SetVanillaIcon(item, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //                    }
        //                    currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
        //                    currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[item];

        //                    // Setup ItemIcon
        //                    ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                    assortIconScript.itemID = item;
        //                    assortIconScript.itemName = Mod.itemNames[item];
        //                    assortIconScript.description = Mod.itemDescriptions[item];
        //                    assortIconScript.weight = Mod.itemWeights[item];
        //                    assortIconScript.volume = Mod.itemVolumes[item];
        //                }
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //    // TODO: Maybe have fail conditions and fail rewards sections

        //    // Setup buttons
        //    // ShortInfo
        //    PointableButton pointableTaskShortInfoButton = shortInfo.gameObject.AddComponent<PointableButton>();
        //    pointableTaskShortInfoButton.SetButton();
        //    pointableTaskShortInfoButton.Button.onClick.AddListener(() => { OnTaskShortInfoClick(description.gameObject); });
        //    pointableTaskShortInfoButton.MaxPointingRange = 20;
        //    pointableTaskShortInfoButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
        //    // Start
        //    PointableButton pointableTaskStartButton = shortInfo.GetChild(6).gameObject.AddComponent<PointableButton>();
        //    pointableTaskStartButton.SetButton();
        //    pointableTaskStartButton.Button.onClick.AddListener(() => { OnTaskStartClick(task); });
        //    pointableTaskStartButton.MaxPointingRange = 20;
        //    pointableTaskStartButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
        //    // Finish
        //    PointableButton pointableTaskFinishButton = shortInfo.GetChild(7).gameObject.AddComponent<PointableButton>();
        //    pointableTaskFinishButton.SetButton();
        //    pointableTaskFinishButton.Button.onClick.AddListener(() => { OnTaskFinishClick(task); });
        //    pointableTaskFinishButton.MaxPointingRange = 20;
        //    pointableTaskFinishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

        //    UpdateTaskListHeight();
        //}

        //public void OnTaskStartClick(Task task)
        //{
        //    // Set state of task to active
        //    task.taskState = Task.TaskState.Active;

        //    // Add task to active task list of player status
        //    StatusUI.instance.AddTask(task);

        //    // Update market task list by making the shortinfo of the referenced task UI element in Task to show that it is active
        //    task.marketUI.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
        //    task.marketUI.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);
        //    task.marketUI.transform.GetChild(0).GetChild(5).gameObject.SetActive(true);
        //    task.marketUI.transform.GetChild(0).GetChild(6).gameObject.SetActive(false);

        //    // Update conditions that are dependent on this task being started, then update everything depending on those conditions
        //    if (Trader.questConditionsByTask.ContainsKey(task.ID))
        //    {
        //        foreach (Condition taskCondition in Trader.questConditionsByTask[task.ID])
        //        {
        //            // If the condition requires this task to be started
        //            if (taskCondition.value == 2)
        //            {
        //                Trader.FulfillCondition(taskCondition);
        //            }
        //        }
        //    }

        //    // Add completion conditions to list if necessary
        //    foreach (Condition condition in task.completionConditions)
        //    {
        //        if (condition.visible)
        //        {
        //            if (condition.conditionType == Condition.ConditionType.CounterCreator)
        //            {
        //                foreach (TaskCounterCondition counterCondition in condition.counters)
        //                {
        //                    if (Mod.taskCompletionCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
        //                    {
        //                        Mod.taskCompletionCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
        //                    }
        //                    else
        //                    {
        //                        List<TaskCounterCondition> newList = new List<TaskCounterCondition>();
        //                        Mod.taskCompletionCounterConditionsByType.Add(counterCondition.counterConditionType, newList);
        //                        newList.Add(counterCondition);
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                if (Mod.taskCompletionConditionsByType.ContainsKey(condition.conditionType))
        //                {
        //                    Mod.taskCompletionConditionsByType[condition.conditionType].Add(condition);
        //                }
        //                else
        //                {
        //                    List<Condition> newList = new List<Condition>();
        //                    Mod.taskCompletionConditionsByType.Add(condition.conditionType, newList);
        //                    newList.Add(condition);
        //                }
        //            }
        //        }
        //    }

        //    // Spawn intial equipment 
        //    if (task.startingEquipment != null)
        //    {
        //        GivePlayerRewards(task.startingEquipment);
        //    }
        //}

        public void RemoveItemFromTrade(MeatovItemData itemData, int amount, int dogtaglevel = -1, bool foundInRaid = false)
        {
            // If we destroy items or remove from their stack, the item will update player, hideout, and parent volume inventories accordingly
            // So in here we need to go through all items of given itemData and consume lowest stack first
            int amountToRemove = amount;
            while (amountToRemove > 0)
            {
                // Find next best item to consume
                MeatovItem item = null;
                Dictionary<string, List<MeatovItem>> inventoryItemsToUse = null;
                if (foundInRaid)
                {
                    inventoryItemsToUse = tradeVolume.FIRInventoryItems;
                }
                else
                {
                    inventoryItemsToUse = tradeVolume.inventoryItems;
                }
                if (inventoryItemsToUse.TryGetValue(itemData.H3ID, out List<MeatovItem> items))
                {
                    int lowestStack = -1;
                    for (int i = 0; i < items.Count; ++i)
                    {
                        if (items[i].stack < lowestStack && (dogtaglevel == -1 || items[i].dogtagLevel >= dogtaglevel))
                        {
                            lowestStack = items[i].stack;
                            item = items[i];
                        }
                    }
                }
                if(item == null)
                {
                    Mod.LogError("DEV: Market RemoveItemFromTrade did not find suitable FIR item for " + itemData.H3ID + " with " + amountToRemove + " amount left to remove");
                    break;
                }

                // Consume
                if(item.stack > amountToRemove)
                {
                    item.stack -= amountToRemove;
                    break;
                }
                else if(item.stack < amountToRemove)
                {
                    amountToRemove -= item.stack;
                    Destroy(item.gameObject);
                }
                else // item.stack == amountToRemove
                {
                    Destroy(item.gameObject);
                    break;
                }
            }
        }

        //public void OnConditionHandOverClick(Condition condition, GameObject handOverButton, bool FIR)
        //{
        //    int totalLeft = condition.value;
        //    foreach (string item in condition.items)
        //    {
        //        int actualAmount = Mathf.Min(totalLeft - condition.itemCount, tradeVolumeFIRInventory[item]);
        //        RemoveItemFromTrade(item, actualAmount, condition.dogtagLevel, FIR);
        //        condition.itemCount += actualAmount;
        //        totalLeft -= actualAmount;

        //        if (totalLeft == 0)
        //        {
        //            break;
        //        }
        //    }

        //    Trader.UpdateConditionFulfillment(condition);

        //    handOverButton.SetActive(false);
        //}

        //public void GivePlayerRewards(List<TaskReward> rewards, string taskName = null)
        //{
        //    bool resetTrader = false;
        //    foreach (TaskReward reward in rewards)
        //    {
        //        switch (reward.taskRewardType)
        //        {
        //            case TaskReward.TaskRewardType.AssortmentUnlock:
        //                foreach (string item in reward.itemIDs)
        //                {
        //                    Mod.traders[currentTraderIndex].itemsToWaitForUnlock.Remove(item);
        //                }
        //                resetTrader = true;
        //                break;
        //            case TaskReward.TaskRewardType.TraderUnlock:
        //                Mod.traders[reward.traderIndex].unlocked = true;
        //                Transform traderImageTransform = transform.GetChild(1).GetChild(24).GetChild(0).GetChild(0).GetChild(0).GetChild(reward.traderIndex);
        //                traderImageTransform.GetComponent<Collider>().enabled = true;
        //                traderImageTransform.GetChild(2).gameObject.SetActive(false);
        //                break;
        //            case TaskReward.TaskRewardType.TraderStanding:
        //                Mod.traders[reward.traderIndex].standing += reward.standing;
        //                if (Mod.taskStartConditionsByType.ContainsKey(Condition.ConditionType.TraderLoyalty))
        //                {
        //                    foreach (Condition condition in Mod.taskStartConditionsByType[Condition.ConditionType.TraderLoyalty])
        //                    {
        //                        Trader.UpdateConditionFulfillment(condition);
        //                    }
        //                }
        //                if (Mod.taskCompletionConditionsByType.ContainsKey(Condition.ConditionType.TraderLoyalty))
        //                {
        //                    foreach (Condition condition in Mod.taskCompletionConditionsByType[Condition.ConditionType.TraderLoyalty])
        //                    {
        //                        Trader.UpdateConditionFulfillment(condition);
        //                    }
        //                }
        //                if (Mod.taskFailConditionsByType.ContainsKey(Condition.ConditionType.TraderLoyalty))
        //                {
        //                    foreach (Condition condition in Mod.taskFailConditionsByType[Condition.ConditionType.TraderLoyalty])
        //                    {
        //                        Trader.UpdateConditionFulfillment(condition);
        //                    }
        //                }
        //                resetTrader = true;
        //                break;
        //            case TaskReward.TaskRewardType.Item:
        //                string randomItemRewardID = reward.itemIDs[UnityEngine.Random.Range(0, reward.itemIDs.Length)];
        //                int actualAmount = reward.amount;
        //                if (randomItemRewardID.Equals("201") || randomItemRewardID.Equals("202") || randomItemRewardID.Equals("203"))
        //                {
        //                    actualAmount += (int)(actualAmount * HideoutController.currentQuestMoneyReward);
        //                }
        //                SpawnItem(randomItemRewardID, actualAmount);
        //                break;
        //            case TaskReward.TaskRewardType.Experience:
        //                Mod.AddExperience(reward.experience, 3, taskName == null ? "Gained {0} exp. (Task completion)" : "Task \"" + taskName + "\" completed! Gained {0} exp.");
        //                break;
        //        }
        //    }
        //    if (resetTrader)
        //    {
        //        SetTrader(currentTraderIndex);
        //    }
        //}

        public void SpawnItem(MeatovItemData itemData, int amount)
        {
            int amountToSpawn = amount;
            float xSize = tradeVolume.activeVolume.transform.localScale.x;
            float ySize = tradeVolume.activeVolume.transform.localScale.y;
            float zSize = tradeVolume.activeVolume.transform.localScale.z;
            if (itemData.index == -1)
            {
                // Spawn vanilla item will handle the updating of proper elements
                AnvilManager.Run(SpawnVanillaItem(itemData, amountToSpawn));
            }
            else
            {
                GameObject itemPrefab = Mod.GetItemPrefab(itemData.index);
                List<GameObject> objectsList = new List<GameObject>();
                while (amountToSpawn > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPrefab);
                    MeatovItem meatovItem = spawnedItem.GetComponent<MeatovItem>();
                    objectsList.Add(spawnedItem);
                    spawnedItem.transform.localPosition = new Vector3(UnityEngine.Random.Range(-xSize / 2, xSize / 2),
                                                                      UnityEngine.Random.Range(-ySize / 2, ySize / 2),
                                                                      UnityEngine.Random.Range(-zSize / 2, zSize / 2));
                    spawnedItem.transform.localRotation = UnityEngine.Random.rotation;

                    // Set stack and remove amount to spawn
                    if (meatovItem.maxStack > 1)
                    {
                        if (amountToSpawn > meatovItem.maxStack)
                        {
                            meatovItem.stack = meatovItem.maxStack;
                            amountToSpawn -= meatovItem.maxStack;
                        }
                        else // amountToSpawn <= itemCIW.maxStack
                        {
                            meatovItem.stack = amountToSpawn;
                            amountToSpawn = 0;
                        }
                    }
                    else
                    {
                        --amountToSpawn;
                    }

                    // Add item to tradevolume
                    tradeVolume.AddItem(meatovItem);
                }
            }
        }

        //public void OnTaskFinishClick(Task task)
        //{
        //    // Set state of task to success
        //    task.taskState = Task.TaskState.Success;

        //    // Remove task from active task list of player status
        //    if (task.statusListElement != null)
        //    {
        //        foreach (Condition condition in task.startConditions)
        //        {
        //            condition.statusListElement = null;
        //        }
        //        foreach (Condition condition in task.completionConditions)
        //        {
        //            condition.statusListElement = null;
        //        }
        //        foreach (Condition condition in task.failConditions)
        //        {
        //            condition.statusListElement = null;
        //        }

        //        Destroy(task.statusListElement);
        //        task.statusListElement = null;

        //        StatusUI.instance.UpdateTaskListHeight();
        //    }

        //    // Remove from trader task list if exists
        //    if (task.marketUI != null)
        //    {
        //        foreach (Condition condition in task.startConditions)
        //        {
        //            condition.marketUI = null;
        //        }
        //        foreach (Condition condition in task.completionConditions)
        //        {
        //            condition.marketUI = null;
        //        }
        //        foreach (Condition condition in task.failConditions)
        //        {
        //            condition.marketUI = null;
        //        }

        //        Destroy(task.marketUI);
        //        task.marketUI = null;

        //        UpdateTaskListHeight();
        //    }

        //    // Update conditions that are dependent on this task being successfully completed, then update everything depending on those conditions
        //    if (Trader.questConditionsByTask.ContainsKey(task.ID))
        //    {
        //        foreach (Condition taskCondition in Trader.questConditionsByTask[task.ID])
        //        {
        //            // If the condition requires this task to be successfully completed
        //            if (taskCondition.value == 4)
        //            {
        //                Trader.FulfillCondition(taskCondition);
        //            }
        //        }
        //    }

        //    // Spawn completion rewards
        //    if (task.successRewards != null)
        //    {
        //        GivePlayerRewards(task.successRewards);
        //    }
        //}

        //public void OnRagFairCategoryMainClick(GameObject category, string ID)
        //{
        //    // Visually deactivate any other previously active category and activate new one. Or just return if this is already the active category
        //    if (currentActiveCategory != null)
        //    {
        //        if (category.Equals(currentActiveCategory))
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.28125f, 0.28125f, 0.28125f);
        //        }
        //    }
        //    if (currentActiveItemSelector != null)
        //    {
        //        currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        //        currentActiveItemSelector = null;
        //    }
        //    currentActiveCategory = category;
        //    currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.8203125f, 0.8203125f, 0.8203125f);

        //    // Reset item list
        //    ResetRagFairItemList();

        //    // Add all items of that category to the list
        //    Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
        //    Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);
        //    GameObject itemTemplate = listParent.GetChild(0).gameObject;
        //    if (Mod.itemsByParents.ContainsKey(ID))
        //    {
        //        foreach (string itemID in Mod.itemsByParents[ID])
        //        {
        //            AssortmentItem[] assortItems = GetTraderItemSell(itemID);

        //            for (int i = 0; i < assortItems.Length; ++i)
        //            {
        //                int traderIndex = i;
        //                if (assortItems[traderIndex] != null)
        //                {
        //                    // Make an entry for each price of this assort item
        //                    foreach (List<AssortmentPriceData> priceList in assortItems[traderIndex].prices)
        //                    {
        //                        GameObject itemElement = Instantiate(itemTemplate, listParent);
        //                        itemElement.SetActive(true);
        //                        Sprite itemIcon = null;
        //                        string itemName = Mod.itemNames[itemID];
        //                        Image imageElement = itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>();
        //                        if (Mod.itemIcons.ContainsKey(itemID))
        //                        {
        //                            itemIcon = Mod.itemIcons[itemID];
        //                            imageElement.sprite = itemIcon;
        //                        }
        //                        else
        //                        {
        //                            AnvilManager.Run(Mod.SetVanillaIcon(itemID, imageElement));
        //                        }
        //                        itemElement.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemBuyClick(traderIndex, assortItems[traderIndex], priceList, itemIcon); });
        //                        itemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = assortItems[traderIndex].stack.ToString();
        //                        itemElement.transform.GetChild(1).GetComponent<Text>().text = itemName;
        //                        itemElement.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemWishClick(itemID); });

        //                        // Set price icon and label
        //                        int currencyIndex = -1; // Rouble, Dollar, Euro, Barter
        //                        Sprite priceLabelSprite = HideoutController.roubleCurrencySprite;
        //                        int totalPriceCount = 0;
        //                        foreach (AssortmentPriceData price in priceList)
        //                        {
        //                            totalPriceCount += price.count;
        //                            switch (price.ID)
        //                            {
        //                                case "201":
        //                                    if (currencyIndex == -1)
        //                                    {
        //                                        currencyIndex = 1;
        //                                        priceLabelSprite = HideoutController.dollarCurrencySprite;
        //                                    }
        //                                    else if (currencyIndex != 1)
        //                                    {
        //                                        currencyIndex = 3;
        //                                        priceLabelSprite = HideoutController.barterSprite;
        //                                    }
        //                                    break;
        //                                case "202":
        //                                    if (currencyIndex == -1)
        //                                    {
        //                                        currencyIndex = 2;
        //                                        priceLabelSprite = HideoutController.euroCurrencySprite;
        //                                    }
        //                                    else if (currencyIndex != 2)
        //                                    {
        //                                        currencyIndex = 3;
        //                                        priceLabelSprite = HideoutController.barterSprite;
        //                                    }
        //                                    break;
        //                                case "203":
        //                                    if (currencyIndex == -1)
        //                                    {
        //                                        currencyIndex = 0;
        //                                        priceLabelSprite = HideoutController.roubleCurrencySprite;
        //                                    }
        //                                    else if (currencyIndex != 0)
        //                                    {
        //                                        currencyIndex = 3;
        //                                        priceLabelSprite = HideoutController.barterSprite;
        //                                    }
        //                                    break;
        //                                default:
        //                                    break;
        //                            }
        //                        }
        //                        itemElement.transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<Image>().sprite = priceLabelSprite;
        //                        itemElement.transform.GetChild(0).GetChild(2).GetChild(1).GetComponent<Text>().text = totalPriceCount.ToString();

        //                        // Setup itemIcon
        //                        ItemIcon currentItemIconScript = itemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                        currentItemIconScript.isPhysical = false;
        //                        currentItemIconScript.itemID = itemID;
        //                        currentItemIconScript.itemName = itemName;
        //                        currentItemIconScript.description = Mod.itemDescriptions[itemID];
        //                        currentItemIconScript.weight = Mod.itemWeights[itemID];
        //                        currentItemIconScript.volume = Mod.itemVolumes[itemID];

        //                        if (ragFairItemBuyViewsByID.ContainsKey(itemID))
        //                        {
        //                            ragFairItemBuyViewsByID[itemID].Add(itemElement);
        //                        }
        //                        else
        //                        {
        //                            ragFairItemBuyViewsByID.Add(itemID, new List<GameObject>() { itemElement });
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    else
        //    {
        //        Mod.LogError("category does not have children, does not exist in Mod.itemsByParents keys");
        //    }

        //    // Open category (set active sub container)
        //    category.transform.GetChild(1).gameObject.SetActive(true);

        //    // Set toggle button icon to open
        //    Transform toggle = category.transform.GetChild(0).GetChild(0);
        //    toggle.GetChild(0).gameObject.SetActive(false);
        //    toggle.GetChild(1).gameObject.SetActive(true);

        //    // Update category and item lists hoverscrolls
        //    UpdateRagfairBuyCategoriesHoverscrolls();
        //    UpdateRagfairBuyItemsHoverscrolls();
        //}

        //private void UpdateRagfairBuyCategoriesHoverscrolls()
        //{
        //    Transform listParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //    float categoriesHeight = 3; // Top padding
        //    for (int i = 1; i < listParent.childCount - 1; ++i)
        //    {
        //        categoriesHeight += (3 + 12 * CountCategories(listParent.GetChild(i)));
        //    }
        //    HoverScroll newBuyCategoriesDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //    HoverScroll newBuyCategoriesUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //    if (categoriesHeight > 186)
        //    {
        //        newBuyCategoriesUpHoverScroll.rate = 186 / (categoriesHeight - 186);
        //        newBuyCategoriesDownHoverScroll.rate = 186 / (categoriesHeight - 186);
        //        newBuyCategoriesDownHoverScroll.gameObject.SetActive(true);
        //        newBuyCategoriesUpHoverScroll.gameObject.SetActive(false);
        //    }
        //    else
        //    {
        //        newBuyCategoriesDownHoverScroll.gameObject.SetActive(false);
        //        newBuyCategoriesUpHoverScroll.gameObject.SetActive(false);
        //    }
        //}

        //private void UpdateRagfairBuyItemsHoverscrolls()
        //{
        //    Transform listParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //    float itemsHeight = 3 + 34 * (listParent.childCount - 1);
        //    HoverScroll buyItemsDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //    HoverScroll buyItemsUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //    if (itemsHeight > 186)
        //    {
        //        buyItemsUpHoverScroll.rate = 186 / (itemsHeight - 186);
        //        buyItemsDownHoverScroll.rate = 186 / (itemsHeight - 186);
        //        buyItemsDownHoverScroll.gameObject.SetActive(true);
        //        buyItemsUpHoverScroll.gameObject.SetActive(false);
        //    }
        //    else
        //    {
        //        buyItemsDownHoverScroll.gameObject.SetActive(false);
        //        buyItemsUpHoverScroll.gameObject.SetActive(false);
        //    }
        //}

        //private int CountCategories(Transform categoryTransform)
        //{
        //    int count = 1;
        //    if (categoryTransform.GetChild(1).gameObject.activeSelf)
        //    {
        //        foreach (Transform sub in categoryTransform.GetChild(1))
        //        {
        //            count += CountCategories(sub);
        //        }
        //    }
        //    return count;
        //}

        //private void ResetRagFairItemList()
        //{
        //    Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
        //    Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);

        //    // Clear list
        //    while (listParent.childCount > 1)
        //    {
        //        Transform currentFirstChild = listParent.GetChild(1);
        //        currentFirstChild.SetParent(null);
        //        Destroy(currentFirstChild.gameObject);
        //    }

        //    // Deactivate hover scrolls
        //    listTransform.GetChild(2).gameObject.SetActive(false);
        //    listTransform.GetChild(3).gameObject.SetActive(false);
        //}

        //public void OnRagFairItemMainClick(GameObject selector, string ID)
        //{
        //    if (currentActiveItemSelector != null)
        //    {
        //        if (selector.Equals(currentActiveItemSelector))
        //        {
        //            return;
        //        }
        //        else
        //        {
        //            currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        //        }
        //    }
        //    if (currentActiveCategory != null)
        //    {
        //        currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.28125f, 0.28125f, 0.28125f);
        //        currentActiveCategory = null;
        //    }
        //    currentActiveItemSelector = selector;
        //    currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.8203125f, 0.8203125f, 0.8203125f);

        //    // Reset item list
        //    ResetRagFairItemList();

        //    // Add all items of that category to the list
        //    Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
        //    Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);
        //    GameObject itemTemplate = listParent.GetChild(0).gameObject;
        //    AssortmentItem[] assortItems = GetTraderItemSell(ID);

        //    for (int i = 0; i < assortItems.Length; ++i)
        //    {
        //        int traderIndex = i;
        //        if (assortItems[i] != null)
        //        {
        //            // Make an entry for each price of this assort item
        //            foreach (List<AssortmentPriceData> priceList in assortItems[i].prices)
        //            {
        //                GameObject itemElement = Instantiate(itemTemplate, listParent);
        //                itemElement.SetActive(true);
        //                Sprite itemIcon = null;
        //                string itemName = Mod.itemNames[ID];
        //                if (Mod.itemIcons.ContainsKey(ID))
        //                {
        //                    itemIcon = Mod.itemIcons[ID];
        //                    itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = itemIcon;
        //                }
        //                else
        //                {
        //                    AnvilManager.Run(Mod.SetVanillaIcon(ID, itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //                }
        //                itemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = assortItems[i].stack.ToString();
        //                itemElement.transform.GetChild(1).GetComponent<Text>().text = itemName;
        //                itemElement.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemBuyClick(traderIndex, assortItems[traderIndex], priceList, itemIcon); });
        //                itemElement.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemWishClick(ID); });

        //                if (ragFairItemBuyViewsByID.ContainsKey(ID))
        //                {
        //                    ragFairItemBuyViewsByID[ID].Add(itemElement);
        //                }
        //                else
        //                {
        //                    ragFairItemBuyViewsByID.Add(ID, new List<GameObject>() { itemElement });
        //                }

        //                // Set price icon and label
        //                int currencyIndex = -1; // Rouble, Dollar, Euro, Barter
        //                Sprite priceLabelSprite = HideoutController.roubleCurrencySprite;
        //                int totalPriceCount = 0;
        //                foreach (AssortmentPriceData price in priceList)
        //                {
        //                    totalPriceCount += price.count;
        //                    switch (price.ID)
        //                    {
        //                        case "201":
        //                            if (currencyIndex == -1)
        //                            {
        //                                currencyIndex = 1;
        //                                priceLabelSprite = HideoutController.dollarCurrencySprite;
        //                            }
        //                            else if (currencyIndex != 1)
        //                            {
        //                                currencyIndex = 3;
        //                                priceLabelSprite = HideoutController.barterSprite;
        //                            }
        //                            break;
        //                        case "202":
        //                            if (currencyIndex == -1)
        //                            {
        //                                currencyIndex = 2;
        //                                priceLabelSprite = HideoutController.euroCurrencySprite;
        //                            }
        //                            else if (currencyIndex != 2)
        //                            {
        //                                currencyIndex = 3;
        //                                priceLabelSprite = HideoutController.barterSprite;
        //                            }
        //                            break;
        //                        case "203":
        //                            if (currencyIndex == -1)
        //                            {
        //                                currencyIndex = 0;
        //                                priceLabelSprite = HideoutController.roubleCurrencySprite;
        //                            }
        //                            else if (currencyIndex != 0)
        //                            {
        //                                currencyIndex = 3;
        //                                priceLabelSprite = HideoutController.barterSprite;
        //                            }
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                }
        //                itemElement.transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<Image>().sprite = priceLabelSprite;
        //                itemElement.transform.GetChild(0).GetChild(2).GetChild(1).GetComponent<Text>().text = totalPriceCount.ToString();

        //                // Setup itemIcon
        //                ItemIcon currentItemIconScript = itemElement.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //                currentItemIconScript.isPhysical = false;
        //                currentItemIconScript.itemID = ID;
        //                currentItemIconScript.itemName = itemName;
        //                currentItemIconScript.description = Mod.itemDescriptions[ID];
        //                currentItemIconScript.weight = Mod.itemWeights[ID];
        //                currentItemIconScript.volume = Mod.itemVolumes[ID];
        //            }
        //        }
        //    }

        //    // Update hoverscrolls
        //    UpdateRagfairBuyItemsHoverscrolls();
        //}

        //public void OnRagFairCategoryToggleClick(GameObject category)
        //{
        //    Transform toggle = category.transform.GetChild(0).GetChild(0);
        //    toggle.GetChild(0).gameObject.SetActive(!toggle.GetChild(0).gameObject.activeSelf);
        //    toggle.GetChild(1).gameObject.SetActive(!toggle.GetChild(1).gameObject.activeSelf);
        //    category.transform.GetChild(1).gameObject.SetActive(toggle.GetChild(1).gameObject.activeSelf);

        //    UpdateRagfairBuyCategoriesHoverscrolls();
        //}

        //public int GetTotalItemSell(string ID)
        //{
        //    // TODO: Once rag fair player simulation is implemented, add up the number of player selling entries 
        //    int count = 0;

        //    foreach (Trader trader in Mod.traders)
        //    {
        //        int level = trader.GetLoyaltyLevel();
        //        TraderAssortment currentAssort = trader.assortmentByLevel[level];
        //        if (currentAssort.itemsByID.ContainsKey(ID))
        //        {
        //            ++count;
        //        }
        //    }

        //    return count;
        //}

        //public AssortmentItem[] GetTraderItemSell(string ID)
        //{
        //    // TODO: Once rag fair player simulation is implemented, add up the number of player selling entries 
        //    AssortmentItem[] itemAssortments = new AssortmentItem[8];

        //    foreach (Trader trader in Mod.traders)
        //    {
        //        int level = trader.GetLoyaltyLevel();
        //        TraderAssortment currentAssort = trader.assortmentByLevel[level];
        //        if (currentAssort.itemsByID.ContainsKey(ID))
        //        {
        //            itemAssortments[trader.index] = currentAssort.itemsByID[ID];
        //        }
        //    }

        //    return itemAssortments;
        //}

        //public void OnRagFairWishlistItemWishClick(GameObject UIElement, string ID)
        //{
        //    // Destroy wishlist element
        //    UIElement.transform.SetParent(null);
        //    Destroy(UIElement);

        //    // Update wishlist hover scrolls
        //    UpdateRagFairWishlistHoverscrolls();

        //    // Disable star of buy item view
        //    if (ragFairItemBuyViewsByID.ContainsKey(ID))
        //    {
        //        foreach (GameObject buyItemView in ragFairItemBuyViewsByID[ID])
        //        {
        //            buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = Color.black;
        //        }
        //    }

        //    // Remove from wishlist logic
        //    wishListItemViewsByID.Remove(ID);
        //    Mod.wishList.Remove(ID);
        //}

        //public void OnRagFairBuyItemBuyClick(int traderIndex, AssortmentItem item, List<AssortmentPriceData> priceList, Sprite itemIcon)
        //{
        //    Mod.LogInfo("OnRagFairBuyItemBuyClick called on item: " + item.ID);
        //    // Set rag fair cart item, icon, amount, name
        //    ragfairCartItem = item.ID;
        //    ragfairCartItemCount = 1;
        //    ragfairPrices = priceList;

        //    Mod.LogInfo("0");
        //    Transform ragfairCartTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2);
        //    string itemName = Mod.itemNames[item.ID];
        //    ragfairCartTransform.GetChild(1).GetChild(0).GetComponent<Text>().text = itemName;
        //    Mod.LogInfo("0");
        //    if (itemIcon == null)
        //    {
        //        AnvilManager.Run(Mod.SetVanillaIcon(item.ID, ragfairCartTransform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>()));
        //    }
        //    else
        //    {
        //        ragfairCartTransform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = itemIcon;
        //    }
        //    Mod.LogInfo("0");
        //    ragfairCartTransform.GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "1";

        //    // Setup selected item ItemIcon
        //    ItemIcon ragfairCartItemIconScript = ragfairCartTransform.GetChild(1).GetChild(1).GetComponent<ItemIcon>();
        //    if (ragfairCartItemIconScript == null)
        //    {
        //        ragfairCartItemIconScript = ragfairCartTransform.GetChild(1).GetChild(1).gameObject.AddComponent<ItemIcon>();
        //    }
        //    ragfairCartItemIconScript.itemID = item.ID;
        //    ragfairCartItemIconScript.itemName = itemName;
        //    ragfairCartItemIconScript.description = Mod.itemDescriptions[item.ID];
        //    ragfairCartItemIconScript.weight = Mod.itemWeights[item.ID];
        //    ragfairCartItemIconScript.volume = Mod.itemVolumes[item.ID];

        //    Mod.LogInfo("0");
        //    Transform cartShowcase = ragfairCartTransform.GetChild(1);
        //    Transform pricesParent = cartShowcase.GetChild(3).GetChild(0).GetChild(0);
        //    GameObject priceTemplate = pricesParent.GetChild(0).gameObject;
        //    float priceHeight = 0;
        //    Mod.LogInfo("0");
        //    while (pricesParent.childCount > 1)
        //    {
        //        Transform currentFirstChild = pricesParent.GetChild(1);
        //        currentFirstChild.SetParent(null);
        //        Destroy(currentFirstChild.gameObject);
        //    }
        //    bool canDeal = true;
        //    Mod.LogInfo("0");
        //    if (ragfairBuyPriceElements == null)
        //    {
        //        ragfairBuyPriceElements = new List<GameObject>();
        //    }
        //    else
        //    {
        //        ragfairBuyPriceElements.Clear();
        //    }
        //    foreach (AssortmentPriceData price in priceList)
        //    {
        //        Mod.LogInfo("\t0");
        //        priceHeight += 50;
        //        Transform priceElement = Instantiate(priceTemplate, pricesParent).transform;
        //        priceElement.gameObject.SetActive(true);

        //        Mod.LogInfo("\t0");
        //        if (Mod.itemIcons.ContainsKey(price.ID))
        //        {
        //            priceElement.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[price.ID];
        //        }
        //        else
        //        {
        //            AnvilManager.Run(Mod.SetVanillaIcon(price.ID, priceElement.GetChild(0).GetChild(2).GetComponent<Image>()));
        //        }
        //        Mod.LogInfo("\t0");
        //        priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = price.count.ToString();
        //        string priceItemName = Mod.itemNames[price.ID];
        //        priceElement.GetChild(3).GetChild(0).GetComponent<Text>().text = priceItemName;
        //        Mod.LogInfo("\t0");
        //        if (price.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
        //        {
        //            priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).gameObject.SetActive(true);
        //            priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = ">= lvl " + price.dogtagLevel;
        //        }
        //        ragfairBuyPriceElements.Add(priceElement.gameObject);

        //        Mod.LogInfo("\t0");
        //        if (tradeVolumeInventory.ContainsKey(price.ID) && tradeVolumeInventory[price.ID] >= price.count)
        //        {
        //            priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
        //            priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
        //        }
        //        else
        //        {
        //            canDeal = false;
        //        }

        //        // Setup price ItemIcon
        //        ItemIcon ragfairCartPriceItemIconScript = priceElement.GetChild(2).gameObject.AddComponent<ItemIcon>();
        //        ragfairCartPriceItemIconScript.itemID = price.ID;
        //        ragfairCartPriceItemIconScript.itemName = priceItemName;
        //        ragfairCartPriceItemIconScript.description = Mod.itemDescriptions[price.ID];
        //        ragfairCartPriceItemIconScript.weight = Mod.itemWeights[price.ID];
        //        ragfairCartPriceItemIconScript.volume = Mod.itemVolumes[price.ID];
        //    }
        //    Mod.LogInfo("0");
        //    HoverScroll downHoverScroll = cartShowcase.GetChild(3).GetChild(3).GetComponent<HoverScroll>();
        //    HoverScroll upHoverScroll = cartShowcase.GetChild(3).GetChild(2).GetComponent<HoverScroll>();
        //    Mod.LogInfo("0");
        //    if (priceHeight > 100)
        //    {
        //        downHoverScroll.rate = 100 / (priceHeight - 100);
        //        upHoverScroll.rate = 100 / (priceHeight - 100);
        //        downHoverScroll.gameObject.SetActive(true);
        //        upHoverScroll.gameObject.SetActive(false);
        //    }
        //    else
        //    {
        //        downHoverScroll.gameObject.SetActive(false);
        //        upHoverScroll.gameObject.SetActive(false);
        //    }
        //    Mod.LogInfo("0");

        //    Transform dealButton = cartShowcase.parent.GetChild(2).GetChild(0).GetChild(0);
        //    if (canDeal)
        //    {
        //        dealButton.GetComponent<Collider>().enabled = true;
        //        dealButton.GetChild(1).GetComponent<Text>().color = Color.white;
        //    }
        //    else
        //    {
        //        dealButton.GetComponent<Collider>().enabled = false;
        //        dealButton.GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
        //    }
        //    Mod.LogInfo("0");

        //    // Set ragfair buy deal button 
        //    PointableButton ragfairCartDealAmountButton = ragfairCartTransform.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<PointableButton>();
        //    ragfairCartDealAmountButton.Button.onClick.AddListener(() => { OnRagfairBuyDealClick(traderIndex); });

        //    Mod.LogInfo("0");
        //    // Deactivate ragfair buy categories and item list, enable cart
        //    ragfairCartTransform.gameObject.SetActive(true);
        //    ragfairCartTransform.parent.GetChild(0).gameObject.SetActive(false);
        //    ragfairCartTransform.parent.GetChild(1).gameObject.SetActive(false);
        //}

        //public void OnRagFairBuyItemWishClick(string ID)
        //{
        //    if (Mod.wishList.Contains(ID))
        //    {
        //        // Destroy wishlist element
        //        wishListItemViewsByID[ID].transform.SetParent(null);
        //        Destroy(wishListItemViewsByID[ID]);

        //        // Update wishlist hover scrolls
        //        UpdateRagFairWishlistHoverscrolls();

        //        // Disable star of buy item views
        //        if (ragFairItemBuyViewsByID.ContainsKey(ID))
        //        {
        //            foreach (GameObject buyItemView in ragFairItemBuyViewsByID[ID])
        //            {
        //                buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = Color.black;
        //            }
        //        }

        //        // Remove from wishlist logic
        //        wishListItemViewsByID.Remove(ID);
        //        Mod.wishList.Remove(ID);
        //    }
        //    else
        //    {
        //        // Add wishlist UI entry, also updates hoverscrolls
        //        AddItemToWishlist(ID);

        //        // Enable star of buy item views
        //        if (ragFairItemBuyViewsByID.ContainsKey(ID))
        //        {
        //            foreach (GameObject buyItemView in ragFairItemBuyViewsByID[ID])
        //            {
        //                buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = new Color(1, 0.84706f, 0); ;
        //            }
        //        }

        //        // Add to wishlist logic
        //        Mod.wishList.Add(ID);
        //    }
        //}

        //public void AddItemToWishlist(string ID)
        //{
        //    Transform wishlistParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
        //    GameObject wishlistItemViewTemplate = wishlistParent.GetChild(0).gameObject;
        //    GameObject wishlistItemView = Instantiate(wishlistItemViewTemplate, wishlistParent);
        //    wishlistItemView.SetActive(true);

        //    // Update wishlist hover scrolls
        //    UpdateRagFairWishlistHoverscrolls();

        //    string itemName = Mod.itemNames[ID];
        //    if (Mod.itemIcons.ContainsKey(ID))
        //    {
        //        wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[ID];
        //    }
        //    else
        //    {
        //        AnvilManager.Run(Mod.SetVanillaIcon(ID, wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
        //    }
        //    wishlistItemView.transform.GetChild(1).GetComponent<Text>().text = itemName;

        //    wishlistItemView.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairWishlistItemWishClick(wishlistItemView, ID); });

        //    // Setup itemIcon
        //    ItemIcon currentItemIconScript = wishlistItemView.transform.GetChild(0).gameObject.AddComponent<ItemIcon>();
        //    currentItemIconScript.isPhysical = false;
        //    currentItemIconScript.itemID = ID;
        //    currentItemIconScript.itemName = itemName;
        //    currentItemIconScript.description = Mod.itemDescriptions[ID];
        //    currentItemIconScript.weight = Mod.itemWeights[ID];
        //    currentItemIconScript.volume = Mod.itemVolumes[ID];

        //    wishListItemViewsByID.Add(ID, wishlistItemView);

        //    if (ragFairItemBuyViewsByID.ContainsKey(ID))
        //    {
        //        List<GameObject> itemViewsList = ragFairItemBuyViewsByID[ID];
        //        foreach (GameObject itemView in itemViewsList)
        //        {
        //            itemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = new Color(1, 0.84706f, 0);
        //        }
        //    }

        //    if (Mod.activeDescriptionsByItemID.ContainsKey(ID))
        //    {
        //        foreach (DescriptionManager descriptionManager in Mod.activeDescriptionsByItemID[ID])
        //        {
        //            descriptionManager.wishlistButtonImage.color = new Color(1, 0.84706f, 0);
        //            descriptionManager.fullWishlist.SetActive(true);
        //            descriptionManager.fullNeededIcons[3].SetActive(true);
        //        }
        //    }
        //}

        //private void UpdateRagFairWishlistHoverscrolls()
        //{
        //    HoverScroll newWishlistDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<HoverScroll>();
        //    HoverScroll newWishlistUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<HoverScroll>();
        //    float wishlistHeight = 3 + 34 * Mod.wishList.Count;
        //    if (wishlistHeight > 190)
        //    {
        //        newWishlistUpHoverScroll.rate = 190 / (wishlistHeight - 190);
        //        newWishlistDownHoverScroll.rate = 190 / (wishlistHeight - 190);
        //        newWishlistDownHoverScroll.gameObject.SetActive(true);
        //    }
        //    else
        //    {
        //        newWishlistDownHoverScroll.gameObject.SetActive(false);
        //        newWishlistUpHoverScroll.gameObject.SetActive(false);
        //    }
        //}

        //public void OnRagfairBuyAmountClick()
        //{
        //    // Cancel stack splitting if in progress
        //    if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
        //    {
        //        Mod.splittingItem.CancelSplit();
        //    }

        //    // Disable buy amount buttons until done choosing amount
        //    transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;
        //    transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;

        //    // Start splitting
        //    Mod.stackSplitUI.SetActive(true);
        //    Mod.stackSplitUI.transform.localPosition = Mod.rightHand.transform.localPosition + Mod.rightHand.transform.forward * 0.2f;
        //    Mod.stackSplitUI.transform.localRotation = Quaternion.Euler(0, Mod.rightHand.transform.localRotation.eulerAngles.y, 0);
        //    amountChoiceStartPosition = Mod.rightHand.transform.localPosition;
        //    amountChoiceRightVector = Mod.rightHand.transform.right;
        //    amountChoiceRightVector.y = 0;

        //    choosingRagfairBuyAmount = true;
        //    startedChoosingThisFrame = true;

        //    // Set max buy amount, limit it to 360 otherwise scale is too small and it is hard to specify a exact value
        //    maxBuyAmount = Mathf.Min(360, Mod.traders[currentTraderIndex].assortmentByLevel[Mod.traders[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack);
        //}

        //public void OnRagfairBuyDealClick(int traderIndex)
        //{
        //    // Remove price from trade volume
        //    foreach (AssortmentPriceData price in ragfairPrices)
        //    {
        //        int amountToRemove = price.count * ragfairCartItemCount;
        //        RemoveItemFromTrade(price.ID, amountToRemove);
        //    }

        //    // Add bought amount of item to trade volume at random pos and rot within it
        //    SpawnItem(ragfairCartItem, ragfairCartItemCount);

        //    // Update amount of item in trader's assort
        //    Mod.traders[traderIndex].assortmentByLevel[Mod.traders[traderIndex].GetLoyaltyLevel()].itemsByID[ragfairCartItem].stack -= ragfairCartItemCount;
        //}

        //public void OnRagfairBuyCancelClick()
        //{
        //    // Deactivate ragfair buy categories and item list, enable cart
        //    Transform ragfairCartTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2);
        //    ragfairCartTransform.gameObject.SetActive(false);
        //    ragfairCartTransform.parent.GetChild(0).gameObject.SetActive(true);
        //    ragfairCartTransform.parent.GetChild(1).gameObject.SetActive(true);
        //}

        public void OnDestroy()
        {
            // Unsubscribe to events
            tradeVolume.OnItemAdded -= OnItemAdded;
            tradeVolume.OnItemRemoved -= OnItemRemoved;
        }
    }
}
