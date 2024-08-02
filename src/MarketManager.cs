using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class MarketManager : MonoBehaviour
    {
        public static readonly float RAGFAIR_PRICE_MULT = 2f;
        public static readonly float FENCE_PRICE_MULT = 1.8f; // Should be lower than RAGFAIR_PRICE_MULT
        public static readonly float DOLLAR_EXCHANGE_RATE = 120.0f;
        public static readonly float EURO_EXCHANGE_RATE = 135.0f;

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
        public HoverScrollProcessor buyShowcaseHoverScrollProcessor;
        public GameObject buyShowcaseRowPrefab;
        public GameObject buyShowcaseItemViewPrefab;
        public Text buyItemName;
        public PriceItemView buyItemView;
        public Dictionary<string, List<PriceItemView>> buyItemPriceViewsByH3ID;
        public Text buyItemCount;
        public Transform buyPricesContent;
        public HoverScrollProcessor buyPricesHoverScrollProcessor;
        public GameObject buyPricePrefab;
        public GameObject buyDealButton;
        public Collider buyAmountButtonCollider;

        public Transform sellShowcaseContent;
        public HoverScrollProcessor sellShowcaseHoverScrollProcessor;
        public GameObject sellShowcaseRowPrefab;
        public GameObject sellShowcaseItemViewPrefab;
        public Text sellItemName;
        public PriceItemView sellItemView;
        public GameObject sellDealButton;

        public Transform tasksContent;
        public HoverScrollProcessor tasksHoverScrollProcessor;
        public GameObject taskPrefab;

        public Transform insureShowcaseContent;
        public HoverScrollProcessor insureShowcaseHoverScrollProcessor;
        public GameObject insureShowcaseRowPrefab;
        public GameObject insureShowcaseItemViewPrefab;
        public Text insureItemName;
        public PriceItemView insureItemView;
        public GameObject insureDealButton;
        public GameObject insurePriceFulfilled;
        public GameObject insurePriceUnfulfilled;

        public Transform ragFairBuyCategoriesParent;
        public HoverScrollProcessor ragFairCategoriesHoverScrollProcessor;
        public GameObject ragFairBuyCategoryPrefab;
        public Transform ragFairBuyItemParent;
        public HoverScrollProcessor ragFairBuyItemsHoverScrollProcessor;
        public GameObject ragFairBuyItemPrefab;
        public GameObject ragFairBuyCart;
        public PriceItemView ragFairBuyItemView;
        public Transform ragFairBuyPricesParent;
        public HoverScrollProcessor ragFairPricesHoverScrollProcessor;
        public GameObject ragFairBuyPricePrefab;
        public GameObject ragFairBuyDealButton;
        public Dictionary<string, List<PriceItemView>> ragFairBuyItemPriceViewsByH3ID;
        public Text ragFairBuyItemCount;
        public Collider ragFairBuyAmountButtonCollider;

        public Transform ragFairSellShowcaseParent;
        public HoverScrollProcessor ragFairSellShowcaseHoverScrollProcessor;
        public GameObject ragFairSellRowPrefab;
        public GameObject ragFairSellItemPrefab;
        public PriceItemView ragFairSellSelectedItemView;
        public PriceItemView ragFairSellForItemView;
        public Text ragFairSellChance;
        public Text ragFairSellTax;
        public GameObject ragFairSellListButton;
        public Collider ragFairSellAmountButtonCollider;

        public Transform ragFairListingsParent;
        public HoverScrollProcessor ragFairListingsHoverScrollProcessor;
        public GameObject ragFairListingPrefab;

        public Transform ragFairWishlistParent;
        public HoverScrollProcessor ragFairWishlistHoverScrollProcessor;
        public GameObject ragFairWishlistItemPrefab;

        // Live data
        public MeatovItemData roubleItemData;
        [NonSerialized]
        public float fenceRestockTimer;

        public Dictionary<string, int> inventory;
        public Dictionary<string, List<MeatovItem>> inventoryItems;
        public Dictionary<string, int> FIRInventory;
        public Dictionary<string, List<MeatovItem>> FIRInventoryItems;

        public List<RagFairListing> ragFairListings = new List<RagFairListing>();
        public Dictionary<string, GameObject> wishListItemViewsByID;
        public Dictionary<string, List<GameObject>> ragFairItemBuyViewsByID;

        [NonSerialized]
        public int currentTraderIndex;
        public MeatovItemData currencyItemData;
        public MeatovItemData cartItem;
        [NonSerialized]
        public int cartItemCount;
        public List<BarterPrice> buyPrices;
        public MeatovItemData ragFairCartItem;
        public Barter ragFairCartBarter;
        [NonSerialized]
        public int ragFairCartItemCount;
        public List<BarterPrice> ragFairBuyPrices;
        [NonSerialized]
        public int currentTotalSellingPrice = 0;
        [NonSerialized]
        public int currentTotalInsurePrice = 0;
        [NonSerialized]
        public MeatovItem currentRagFairSellItem;
        [NonSerialized]
        public float currentRagFairSellChance;
        [NonSerialized]
        public int currentRagFairSellPrice;
        [NonSerialized]
        public int currentRagFairSellTax;

        [NonSerialized]
        public bool choosingBuyAmount;
        [NonSerialized]
        public bool choosingRagfairBuyAmount;
        [NonSerialized]
        public bool choosingRagFairSellAmount;
        [NonSerialized]
        public bool startedChoosingThisFrame;
        private Vector3 amountChoiceStartPosition;
        private Vector3 amountChoiceRightVector;
        private int chosenAmount;
        private int maxBuyAmount;

        // Events
        public delegate void OnItemAddedToTradeInventoryDelegate(MeatovItem item);
        public static event OnItemAddedToTradeInventoryDelegate OnItemAddedToTradeInventory;
        public delegate void OnItemRemovedFromTradeInventoryDelegate(MeatovItem item);
        public static event OnItemRemovedFromTradeInventoryDelegate OnItemRemovedFromTradeInventory;
        public delegate void OnTradeInventoryItemStackChangedDelegate(MeatovItem item, int stackDifference);
        public static event OnTradeInventoryItemStackChangedDelegate OnTradeInventoryItemStackChanged;

        public void Start()
        {
            roubleItemData = Mod.customItemData[203];

            // Process items loaded in trade volume
            foreach (KeyValuePair<string, List<MeatovItem>> itemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < itemEntry.Value.Count; ++i)
                {
                    OnItemAdded(itemEntry.Value[i], false);
                }
            }

            // Subscribe to events
            tradeVolume.OnItemAdded += OnTradeVolumeItemAdded;
            tradeVolume.OnItemRemoved += OnItemRemoved;
            MeatovItemData.OnAddedToWishlist += OnItemAddedToWishlist;
            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;
            for(int i=0; i < Mod.traders.Length; ++i)
            {
                Mod.traders[i].OnTraderLevelChanged += OnTraderLevelChanged;
            }
            
            // Initialize everything
            SetTrader(0);
            InitRagFair();
        }

        private void Update()
        {
            TakeInput();

            // Update based on splitting stack
            if (choosingBuyAmount || choosingRagfairBuyAmount || choosingRagFairSellAmount)
            {
                Vector3 handVector = Mod.rightHand.transform.position - amountChoiceStartPosition;
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

                Mod.stackSplitUI.arrow.localPosition = new Vector3(distanceFromCenter * 100, -2.14f, 0);
                Mod.stackSplitUI.amountText.text = chosenAmount.ToString() + "/" + maxBuyAmount;
            }

            // Update ragfair listings
            for(int i=0; i < ragFairListings.Count; ++i)
            {
                ragFairListings[i].timeLeft -= Time.deltaTime;
                if(ragFairListings[i].timeToSellCheck <= 0)
                {
                    if (!ragFairListings[i].sellChecked)
                    {
                        if (ragFairListings[i].SellCheck())
                        {
                            tradeVolume.SpawnItem(roubleItemData, ragFairListings[i].price);
                            Destroy(ragFairListings[i].UI.gameObject);
                            ragFairListings[i] = ragFairListings[ragFairListings.Count - 1];
                            ragFairListings.RemoveAt(ragFairListings.Count - 1);
                            break;
                        }
                    }
                }
                else
                {
                    ragFairListings[i].timeToSellCheck -= Time.deltaTime;
                }
                if(ragFairListings[i].timeLeft <= 0)
                {
                    CancelRagFairListing(i);
                }
            }

            if (fenceRestockTimer > 0)
            {
                fenceRestockTimer -= Time.deltaTime;
            }
            else
            {
                // Reset trader if necessary
                if (currentTraderIndex == 2)
                {
                    SetTrader(2);
                }

                // Reset timer
                fenceRestockTimer = Convert.ToSingle((DateTime.Today.ToUniversalTime().AddHours(24) - DateTime.UtcNow).TotalSeconds);
            }
        }

        public void CancelRagFairListing(int index)
        {
            // Reload serialized item from listing and readd it to trade volume
            JToken tradeItemData = ragFairListings[index].serializedItem;
            JToken vanillaCustomData = tradeItemData["vanillaCustomData"];
            VaultSystem.ReturnObjectListDelegate del = objs =>
            {
                // Here, assume objs[0] is the root item
                MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                if (meatovItem != null)
                {
                    // Set live data
                    meatovItem.SetData(Mod.defaultItemData[vanillaCustomData["tarkovID"].ToString()]);
                    meatovItem.insured = (bool)vanillaCustomData["insured"];
                    meatovItem.looted = (bool)vanillaCustomData["looted"];
                    meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                    for (int m = 1; m < objs.Count; ++m)
                    {
                        MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                        if (childMeatovItem != null)
                        {
                            childMeatovItem.SetData(Mod.defaultItemData[vanillaCustomData["children"][m - 1]["tarkovID"].ToString()]);
                            childMeatovItem.insured = (bool)vanillaCustomData["children"][m - 1]["insured"];
                            childMeatovItem.looted = (bool)vanillaCustomData["children"][m - 1]["looted"];
                            childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][m - 1]["foundInRaid"];
                        }
                    }

                    tradeVolume.AddItem(meatovItem);
                    meatovItem.transform.localPosition = Vector3.zero;
                }
            };

            MeatovItem loadedItem = MeatovItem.Deserialize(tradeItemData, del);

            if (loadedItem != null)
            {
                tradeVolume.AddItem(loadedItem);
                loadedItem.transform.localPosition = Vector3.zero;
            }

            // Remove listing from list
            Destroy(ragFairListings[index].UI.gameObject);
            ragFairListings[index] = ragFairListings[ragFairListings.Count - 1];
            ragFairListings.RemoveAt(ragFairListings.Count - 1);
        }

        public void CancelRagFairListing(RagFairListing listing)
        {
            for(int i=0; i < ragFairListings.Count; ++i)
            {
                if(ragFairListings[i] == listing)
                {
                    CancelRagFairListing(i);
                    break;
                }
            }
        }

        public void OnTraderLevelChanged(Trader trader)
        {
            if(currentTraderIndex == trader.index)
            {
                SetTrader(currentTraderIndex);
            }

            UpdateCategories();
        }

        public void OnPlayerLevelChanged()
        {
            traderDetailsPlayerRankIcon.sprite = playerRankIcons[Mod.level / 5];
            traderDetailsPlayerRankText.text = Mod.level.ToString();
        }

        public void OnTradeVolumeItemAdded(MeatovItem item)
        {
            OnItemAdded(item);
        }

        public void OnItemAdded(MeatovItem item, bool processUI = true)
        {
            AddToInventory(item);

            // Update children
            for (int i = 0; i < item.children.Count; ++i)
            {
                AddToInventory(item.children[i]);
            }

            if (processUI)
            {
                UpdateBuyPriceForItem(item.itemData);
                AddSellItem(item);
                AddRagFairSellItem(item);
                AddInsureItem(item);
                UpdateInsurePriceForItem(item.itemData);
                UpdateRagFairBuyPriceForItem(item.itemData);
                UpdateRagFairSellTax();
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

            // Update UI
            RemoveSellItem(item);
            RemoveInsureItem(item);
            RemoveRagFairSellItem(item);
            UpdateBuyPriceForItem(item.itemData);
            UpdateInsurePriceForItem(item.itemData);
            UpdateRagFairBuyPriceForItem(item.itemData);
            UpdateRagFairSellTax();
        }

        public void AddInsureItem(MeatovItem item)
        {
            Trader trader = Mod.traders[currentTraderIndex];

            // Check if this item can be insured by this trader
            if (item.insured || !trader.ItemInsureable(item.itemData))
            {
                return;
            }

            // Manage rows
            Transform currentRow = insureShowcaseContent.GetChild(insureShowcaseContent.childCount - 1);
            if (insureShowcaseContent.childCount == 1 || currentRow.childCount == 7) // If dont even have a single horizontal yet, add it
            {
                currentRow = GameObject.Instantiate(insureShowcaseRowPrefab, insureShowcaseContent).transform;
                currentRow.gameObject.SetActive(true);

                insureShowcaseHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            }

            GameObject currentItemView = GameObject.Instantiate(insureShowcaseItemViewPrefab, currentRow);
            currentItemView.SetActive(true);

            // Setup ItemView
            ItemView itemView = currentItemView.GetComponent<ItemView>();
            int actualValue = item.itemData.value;

            // Apply exchange rate if necessary
            if (trader.currency == 1)
            {
                actualValue = (int)Mathf.Max(actualValue / DOLLAR_EXCHANGE_RATE, 1);
            }
            else if (trader.currency == 2)
            {
                actualValue = (int)Mathf.Max(actualValue / EURO_EXCHANGE_RATE, 1);
            }

            // Apply trader insure coefficient
            actualValue -= (int)(actualValue * (trader.levels[trader.level].insurancePriceCoef / 100.0f));
            actualValue = Mathf.Max(actualValue, 1);


            itemView.SetItem(item, true, trader.currency, actualValue);

            // Set the itemView for that item
            item.marketInsureItemView = itemView;

            // Update price
            currentTotalInsurePrice += actualValue;
            if (currentTotalInsurePrice > 0)
            {
                UpdateInsurePriceForItem(currencyItemData);
            }
            insureItemView.amount.text = currentTotalInsurePrice.ToString();
        }

        public void AddSellItem(MeatovItem item)
        {
            Trader trader = Mod.traders[currentTraderIndex];

            // Check if this item can be sold to this trader
            if (!trader.ItemSellable(item.itemData))
            {
                return;
            }

            // Manage rows
            Transform currentRow = sellShowcaseContent.GetChild(sellShowcaseContent.childCount - 1);
            if (sellShowcaseContent.childCount == 1 || currentRow.childCount == 7) // If dont even have a single horizontal yet, add it
            {
                currentRow = GameObject.Instantiate(sellShowcaseRowPrefab, sellShowcaseContent).transform;
                currentRow.gameObject.SetActive(true);
                sellShowcaseHoverScrollProcessor.mustUpdateMiddleHeight = 1;
            }

            GameObject currentItemView = GameObject.Instantiate(sellShowcaseItemViewPrefab, currentRow);
            currentItemView.SetActive(true);

            // Setup ItemView
            ItemView itemView = currentItemView.GetComponent<ItemView>();
            int actualValue = item.itemData.value;

            // Apply exchange rate if necessary
            if (trader.currency == 1)
            {
                actualValue = (int)Mathf.Max(actualValue / DOLLAR_EXCHANGE_RATE, 1);
            }
            else if (trader.currency == 2)
            {
                actualValue = (int)Mathf.Max(actualValue / EURO_EXCHANGE_RATE, 1);
            }

            // Apply trader buy coefficient
            actualValue -= (int)(actualValue * (trader.levels[trader.level].buyPriceCoef / 100.0f));
            actualValue = Mathf.Max(actualValue, 1);

            itemView.SetItem(item, true, trader.currency, actualValue);

            // Set the itemView for that item
            item.marketSellItemView = itemView;

            // Update price
            currentTotalSellingPrice += actualValue;
            if (currentTotalSellingPrice > 0)
            {
                sellDealButton.SetActive(true);
            }
            sellItemView.amount.text = currentTotalSellingPrice.ToString();
        }

        public void RemoveSellItem(MeatovItem item)
        {
            if(item.marketSellItemView == null)
            {
                return;
            }

            Transform row = item.marketSellItemView.transform.parent;
            item.marketSellItemView.transform.SetParent(null);
            Destroy(item.marketSellItemView.gameObject);
            item.marketSellItemView = null;

            // Get last row in display
            Transform lastRow = sellShowcaseContent.transform.GetChild(sellShowcaseContent.transform.childCount - 1);

            if(lastRow == row) // The item's row was last row, need to remove the row if there are no other items in it
            {
                if (lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }
            else // Different row, replace destroyed item view with one from last row
            {
                // Note that if a row exists, it must have at least 1 active item view child
                Transform otherItemView = lastRow.GetChild(lastRow.childCount - 1);
                otherItemView.SetParent(row);

                // Destroy last row if other item view was the only one on it
                if(lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }

            // Update price
            Trader trader = Mod.traders[currentTraderIndex];
            int actualValue = item.itemData.value;

            // Apply exchange rate if necessary
            if (trader.currency == 1)
            {
                actualValue = (int)Mathf.Max(actualValue / DOLLAR_EXCHANGE_RATE, 1);
            }
            else if (trader.currency == 2)
            {
                actualValue = (int)Mathf.Max(actualValue / EURO_EXCHANGE_RATE, 1);
            }

            // Apply trader buy coefficient
            actualValue -= (int)(actualValue * (trader.levels[trader.level].buyPriceCoef / 100.0f));
            actualValue = Mathf.Max(actualValue, 1);

            currentTotalSellingPrice -= actualValue;
            if (currentTotalSellingPrice > 0)
            {
                sellDealButton.SetActive(true);
            }
            sellItemView.amount.text = currentTotalSellingPrice.ToString();
        }

        public void RemoveRagFairSellItem(MeatovItem item)
        {
            if(item.ragFairSellItemView == null)
            {
                return;
            }

            Transform row = item.ragFairSellItemView.transform.parent;
            item.ragFairSellItemView.transform.SetParent(null);
            Destroy(item.ragFairSellItemView.gameObject);
            item.ragFairSellItemView = null;

            // Get last row in display
            Transform lastRow = ragFairSellShowcaseParent.transform.GetChild(ragFairSellShowcaseParent.transform.childCount - 1);

            if(lastRow == row) // The item's row was last row, need to remove the row if there are no other items in it
            {
                if (lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }
            else // Different row, replace destroyed item view with one from last row
            {
                // Note that if a row exists, it must have at least 1 active item view child
                Transform otherItemView = lastRow.GetChild(lastRow.childCount - 1);
                otherItemView.SetParent(row);

                // Destroy last row if other item view was the only one on it
                if(lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }

            // Make sure to unselect this item from ragfair sell if it was the one selected
            if(currentRagFairSellItem == item)
            {
                currentRagFairSellItem = null;
                ragFairSellSelectedItemView.ResetItemView();
                ragFairSellListButton.SetActive(false);
            }
        }

        public void RemoveInsureItem(MeatovItem item)
        {
            if(item.marketInsureItemView == null)
            {
                return;
            }

            Transform row = item.marketInsureItemView.transform.parent;
            item.marketInsureItemView.transform.SetParent(null);
            Destroy(item.marketInsureItemView.gameObject);
            item.marketInsureItemView = null;

            // Get last row in display
            Transform lastRow = insureShowcaseContent.transform.GetChild(insureShowcaseContent.transform.childCount - 1);

            if(lastRow == row) // The item's row was last row, need to remove the row if there are no other items in it
            {
                if (lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }
            else // Different row, replace destroyed item view with one from last row
            {
                // Note that if a row exists, it must have at least 1 active item view child
                Transform otherItemView = lastRow.GetChild(lastRow.childCount - 1);
                otherItemView.SetParent(row);

                // Destroy last row if other item view was the only one on it
                if(lastRow.childCount == 1)
                {
                    lastRow.SetParent(null);
                    Destroy(lastRow.gameObject);
                }
            }

            // Update price
            Trader trader = Mod.traders[currentTraderIndex];
            int actualValue = item.itemData.value;

            // Apply exchange rate if necessary
            if (trader.currency == 1)
            {
                actualValue = (int)Mathf.Max(actualValue / DOLLAR_EXCHANGE_RATE, 1);
            }
            else if (trader.currency == 2)
            {
                actualValue = (int)Mathf.Max(actualValue / EURO_EXCHANGE_RATE, 1);
            }

            // Apply trader insure coefficient
            actualValue -= (int)(actualValue * (trader.levels[trader.level].insurancePriceCoef / 100.0f));
            actualValue = Mathf.Max(actualValue, 1);

            // Update price
            currentTotalInsurePrice -= actualValue;
            if (currentTotalInsurePrice > 0)
            {
                UpdateInsurePriceForItem(currencyItemData);
            }
            insureItemView.amount.text = currentTotalInsurePrice.ToString();
        }

        public void UpdateBuyPriceForItem(MeatovItemData itemData)
        {
            if (buyItemPriceViewsByH3ID.TryGetValue(itemData.H3ID, out List<PriceItemView> itemViews))
            {
                for(int j=0; j< itemViews.Count; ++j)
                {
                    PriceItemView itemView = itemViews[j];
                    bool prefulfilled = itemView.fulfilledIcon.activeSelf;
                    int count = 0;
                    tradeVolume.inventory.TryGetValue(itemData.H3ID, out count);
                    itemView.amount.text = Mathf.Min(itemView.price.count, count).ToString() + "/" + (itemView.price.count * cartItemCount).ToString();
                    if (count >= itemView.price.count * cartItemCount)
                    {
                        itemView.fulfilledIcon.SetActive(true);
                        itemView.unfulfilledIcon.SetActive(false);
                        if (!prefulfilled)
                        {
                            // Newly fulfilled, we might now be able to buy, check if all prices fulfilled
                            bool allFulfilled = true;
                            for (int i = 0; i < buyPrices.Count; ++i)
                            {
                                int currentCount = 0;
                                allFulfilled |= tradeVolume.inventory.TryGetValue(buyPrices[i].itemData.H3ID, out currentCount) && currentCount >= buyPrices[i].count * cartItemCount;
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
        }

        public void UpdateInsurePriceForItem(MeatovItemData itemData)
        {
            if (currencyItemData == itemData)
            {
                int count = 0;
                tradeVolume.inventory.TryGetValue(itemData.H3ID, out count);
                insureItemView.amount.text = count.ToString() + "/" + currentTotalInsurePrice.ToString();
                if (count >= currentTotalInsurePrice)
                {
                    insurePriceFulfilled.SetActive(true);
                    insurePriceUnfulfilled.SetActive(false);
                    buyDealButton.SetActive(true);
                }
                else
                {
                    insurePriceFulfilled.SetActive(false);
                    insurePriceUnfulfilled.SetActive(true);
                    buyDealButton.SetActive(false);
                }
            }
        }

        public void OnItemAddedToWishlist(MeatovItemData itemData)
        {
            AddRagFairWishlistEntry(itemData);
        }

        public void AddToInventory(MeatovItem item, bool stackOnly = false, int stackDifference = 0)
        {
            // StackOnly should be true if not item location was changed, but the stack count has
            if (stackOnly)
            {
                if (inventory.ContainsKey(item.H3ID))
                {
                    inventory[item.H3ID] += stackDifference;
                    if (item.foundInRaid)
                    {
                        FIRInventory[item.H3ID] += stackDifference;
                    }

                    if (inventory[item.H3ID] <= 0)
                    {
                        Mod.LogError("DEV: Market AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        inventory.Remove(item.H3ID);
                        inventoryItems.Remove(item.H3ID);
                        FIRInventory.Remove(item.H3ID);
                        FIRInventoryItems.Remove(item.H3ID);
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

                if (item.foundInRaid)
                {
                    if (FIRInventory.ContainsKey(item.H3ID))
                    {
                        FIRInventory[item.H3ID] += item.stack;
                        FIRInventoryItems[item.H3ID].Add(item);
                    }
                    else
                    {
                        FIRInventory.Add(item.H3ID, item.stack);
                        FIRInventoryItems.Add(item.H3ID, new List<MeatovItem> { item });
                    }
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

            if (item.foundInRaid)
            {
                if (FIRInventory.ContainsKey(item.H3ID))
                {
                    FIRInventory[item.H3ID] -= item.stack;
                    FIRInventoryItems[item.H3ID].Remove(item);
                }
                if (FIRInventory[item.H3ID] == 0)
                {
                    FIRInventory.Remove(item.H3ID);
                    FIRInventoryItems.Remove(item.H3ID);
                }
            }

            OnItemRemovedFromTradeInventoryInvoke(item);
        }

        public void OnTraderClicked(int index)
        {
            SetTrader(index);
        }

        public void OnRagFairBuyCancelClicked()
        {
            ragFairBuyCart.SetActive(false);
        }

        public void SetRagFairBuy(Barter ragFairBarter)
        {
            ragFairCartItem = ragFairBarter.itemData;
            ragFairCartBarter = ragFairBarter;

            if (ragFairBuyItemPriceViewsByH3ID == null)
            {
                ragFairBuyItemPriceViewsByH3ID = new Dictionary<string, List<PriceItemView>>();
            }
            else
            { 
                ragFairBuyItemPriceViewsByH3ID.Clear();
            }

            ragFairBuyCart.SetActive(true);
            ragFairBuyItemView.itemView.SetItemData(ragFairBarter.itemData);
            ragFairBuyItemView.itemName.text = ragFairBarter.itemData.name;
            ragFairBuyItemView.amount.text = "1";
            ragFairBuyPrices = new List<BarterPrice>(ragFairBarter.prices);

            while (ragFairBuyPricesParent.childCount > 1)
            {
                Transform currentFirstChild = ragFairBuyPricesParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            bool canDeal = true;
            foreach (BarterPrice price in ragFairBarter.prices)
            {
                if (price.itemData == null)
                {
                    continue;
                }

                Transform priceElement = Instantiate(ragFairBuyPricePrefab, ragFairBuyPricesParent).transform;
                priceElement.gameObject.SetActive(true);
                PriceItemView currentPriceView = priceElement.GetComponentInChildren<PriceItemView>();
                currentPriceView.price = price;
                price.priceItemView = currentPriceView;

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

                // Note that there should only be a single price of a specific item
                // but items like dogtags will be counted as different if the price is of different level
                // So there may be multiple price views per item ID
                if(ragFairBuyItemPriceViewsByH3ID.TryGetValue(price.itemData.H3ID, out List<PriceItemView> priceItemViewList))
                {
                    priceItemViewList.Add(currentPriceView);
                }
                else
                {
                    ragFairBuyItemPriceViewsByH3ID.Add(price.itemData.H3ID, new List<PriceItemView> { currentPriceView });
                }
            }

            ragFairBuyDealButton.SetActive(canDeal);

            ragFairPricesHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void SetRagFairBuyCategory(CategoryTreeNode category)
        {
            while (ragFairBuyItemParent.childCount > 1)
            {
                Transform currentFirstChild = ragFairBuyItemParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            for (int i = 0; i < category.barters.Count; ++i) 
            {
                if (category.barters[i].trader == null 
                    || (category.barters[i].level <= category.barters[i].trader.level
                        && (!category.barters[i].trader.rewardBarters.TryGetValue(category.barters[i].itemData.H3ID, out bool unlocked)
                            || unlocked)))
                {
                    Transform buyItemElement = Instantiate(ragFairBuyItemPrefab, ragFairBuyItemParent).transform;
                    buyItemElement.gameObject.SetActive(true);
                    RagFairBuyItemView currentItemView = buyItemElement.GetComponent<RagFairBuyItemView>();
                    currentItemView.SetBarter(category.barters[i]);
                }
            }

            ragFairBuyItemsHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void OnRagFairBuyDealClicked()
        {
            // Remove price from trade volume
            foreach (BarterPrice price in ragFairBuyPrices)
            {
                RemoveItemFromTrade(price.itemData, price.count * ragFairCartItemCount, price.dogTagLevel);
            }

            // Add bought amount of item to trade volume
            tradeVolume.SpawnItem(ragFairCartItem, ragFairCartItemCount);
        }

        public void OnRagFairBuyAmountClicked()
        {
            if(ragFairCartItem == null)
            {
                return;
            }

            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                Mod.splittingItem.CancelSplit();
            }

            // Disable buy deal/amount buttons until done choosing amount
            buyAmountButtonCollider.enabled = false;
            ragFairBuyAmountButtonCollider.enabled = false;
            ragFairSellAmountButtonCollider.enabled = false;

            // Start choosing amount
            Mod.stackSplitUI.gameObject.SetActive(true);
            Mod.stackSplitUI.transform.position = Mod.rightHand.transform.position + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.rotation = Quaternion.Euler(0, Mod.rightHand.transform.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.position;
            amountChoiceRightVector = Mod.rightHand.transform.right;
            amountChoiceRightVector.y = 0;

            choosingRagfairBuyAmount = true;
            startedChoosingThisFrame = true;

            // Set max buy amount, limit it to 360 otherwise scale is not large enough and its hard to specify an exact value
            maxBuyAmount = 360;
        }

        public void UpdateRagFairBuyPriceForItem(MeatovItemData itemData)
        {
            if (ragFairBuyItemPriceViewsByH3ID.TryGetValue(itemData.H3ID, out List<PriceItemView> itemViews))
            {
                for(int j=0; j< itemViews.Count; ++j)
                {
                    PriceItemView itemView = itemViews[j];
                    bool prefulfilled = itemView.fulfilledIcon.activeSelf;
                    int count = 0;
                    tradeVolume.inventory.TryGetValue(itemData.H3ID, out count);
                    itemView.amount.text = Mathf.Min(itemView.price.count, count).ToString() + "/" + (itemView.price.count * ragFairCartItemCount).ToString();
                    if (count >= itemView.price.count * ragFairCartItemCount)
                    {
                        itemView.fulfilledIcon.SetActive(true);
                        itemView.unfulfilledIcon.SetActive(false);
                        if (!prefulfilled)
                        {
                            // Newly fulfilled, we might now be able to buy, check if all prices fulfilled
                            bool allFulfilled = true;
                            for (int i = 0; i < ragFairBuyPrices.Count; ++i)
                            {
                                int currentCount = 0;
                                allFulfilled |= tradeVolume.inventory.TryGetValue(ragFairBuyPrices[i].itemData.H3ID, out currentCount) && currentCount >= ragFairBuyPrices[i].count * ragFairCartItemCount;
                            }
                            ragFairBuyDealButton.SetActive(allFulfilled);
                        }
                    }
                    else
                    {
                        itemView.fulfilledIcon.SetActive(false);
                        itemView.unfulfilledIcon.SetActive(true);
                        ragFairBuyDealButton.SetActive(false);
                    }
                }
            }
        }

        public void AddRagFairSellItem(MeatovItem item)
        {
            // Check if this item can be sold on ragfair
            if (!item.canSellOnRagfair)
            {
                return;
            }

            // Manage rows
            Transform currentRow = ragFairSellShowcaseParent.GetChild(ragFairSellShowcaseParent.childCount - 1);
            if (ragFairSellShowcaseParent.childCount == 1 || currentRow.childCount == 6) // If dont even have a single horizontal yet, add it
            {
                currentRow = GameObject.Instantiate(ragFairSellRowPrefab, ragFairSellShowcaseParent).transform;
                currentRow.gameObject.SetActive(true);
            }

            GameObject currentItemView = GameObject.Instantiate(ragFairSellItemPrefab, currentRow);
            currentItemView.SetActive(true);

            // Setup ItemView
            RagFairSellItemView itemView = currentItemView.GetComponent<RagFairSellItemView>();
            int actualValue = item.itemData.value;

            itemView.SetItem(item, actualValue);

            // Set the itemView for that item
            item.ragFairSellItemView = itemView;
        }

        public float GetRagFairSellChance(MeatovItem item, int givenValue)
        {
            if (!item.canSellOnRagfair)
            {
                return 0;
            }

            // CASES
            // 1: Trader may be selling this item at arbitrary currency value
            // 2: Trader may also be selling this item as trade for other item, each of them having a value, and thus the barter having a total currency value
            // 3: Ragfair will otherwise be selling it at 1.5x item value if canSellOnRagfair

            // If given value > found value, chance is 0%.
            //    given value <= found value, should result in sigmoid increasing chance with 100% being at the value the item can be sold at a trader

            int foundValue = 0;
            for(int i=0; i < Mod.traders.Length; ++i)
            {
                if (Mod.traders[i].bartersByItemID.TryGetValue(item.H3ID, out List<Barter> barters))
                {
                    for(int j=0; j < barters.Count; ++j)
                    {
                        if (barters[j].prices.Length == 1)
                        {
                            if (barters[j].prices[0].itemData.H3ID.Equals("203"))
                            {
                                foundValue = barters[j].prices[0].count;
                            }
                            else if (barters[j].prices[0].itemData.H3ID.Equals("202"))
                            {
                                foundValue = (int)Mathf.Max(barters[j].prices[0].count / EURO_EXCHANGE_RATE, 1);
                            }
                            else if (barters[j].prices[0].itemData.H3ID.Equals("201"))
                            {
                                foundValue = (int)Mathf.Max(barters[j].prices[0].count / DOLLAR_EXCHANGE_RATE, 1);
                            }
                            else
                            {
                                for (int k = 0; k < barters[j].prices.Length; ++k) 
                                {
                                    foundValue += barters[j].prices[k].count * barters[j].prices[k].itemData.value;
                                }
                            }
                        }
                        else
                        {
                            for (int k = 0; k < barters[j].prices.Length; ++k)
                            {
                                foundValue += barters[j].prices[k].count * barters[j].prices[k].itemData.value;
                            }
                        }
                    }
                }
            }
            if(foundValue == 0)
            {
                foundValue = (int)(item.itemData.value * MarketManager.RAGFAIR_PRICE_MULT);
            }

            if(givenValue > foundValue)
            {
                return 0;
            }
            else
            {
                int soldValue = (int)(item.itemData.value * 0.5f);
                float actualX = Mathf.InverseLerp(soldValue, foundValue, givenValue) - 0.5f;
                return 1 / (1 + Mathf.Pow(43000, actualX));
            }
        }

        public void UpdateRagFairSellTax()
        {
            if(currentRagFairSellItem == null)
            {
                ragFairSellTax.text = "Tax (5%): " + 0;
                ragFairSellListButton.SetActive(false);

                return;
            }

            currentRagFairSellTax = (int)Mathf.Max(currentRagFairSellPrice / 100f * 5f, 1);
            currentRagFairSellTax = (int)Mathf.Max(currentRagFairSellTax - currentRagFairSellTax / 100f * Bonus.ragfairCommission, 1);
            ragFairSellTax.text = "Tax (5%): " + currentRagFairSellTax;

            int amount = 0;
            if(tradeVolume.inventory.TryGetValue(roubleItemData.H3ID, out amount))
            {
                ragFairSellListButton.SetActive(amount >= currentRagFairSellTax);
            }
            else
            {
                ragFairSellListButton.SetActive(false);
            }
        }

        public void SetRagFairSell(MeatovItem item)
        {
            currentRagFairSellItem = item;
            currentRagFairSellPrice = item.stack * item.itemData.value;
            currentRagFairSellChance = GetRagFairSellChance(item, currentRagFairSellPrice);

            ragFairSellSelectedItemView.itemView.SetItem(item);
            ragFairSellSelectedItemView.itemName.text = item.name;
            ragFairSellSelectedItemView.amount.text = item.stack.ToString();
            ragFairSellForItemView.amount.text = currentRagFairSellPrice.ToString();
            ragFairSellChance.text = "Sell Chance: " + (int)(currentRagFairSellChance * 100) + "%";

            UpdateRagFairSellTax();
        }

        public void OnRagFairSellListClicked()
        {
            RagFairListing listing = new RagFairListing();
            listing.price = currentRagFairSellPrice;
            listing.itemData = currentRagFairSellItem.itemData;
            listing.serializedItem = currentRagFairSellItem.Serialize();
            listing.stack = currentRagFairSellItem.stack;
            listing.timeLeft = 3600 * 24 * 3; // 3 days in seconds
            listing.sellChance = currentRagFairSellChance;
            listing.timeToSellCheck = UnityEngine.Random.Range(10, listing.timeLeft - 10);
            AddRagFairListing(listing);

            currentRagFairSellItem.DetachChildren();
            Destroy(currentRagFairSellItem.gameObject);

            RemoveItemFromTrade(roubleItemData, currentRagFairSellTax);
        }

        public void OnRagFairSellAmountClicked()
        {
            if (currentRagFairSellItem == null)
            {
                return;
            }

            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                Mod.splittingItem.CancelSplit();
            }

            // Disable buy deal/amount buttons until done choosing amount
            buyAmountButtonCollider.enabled = false;
            ragFairBuyAmountButtonCollider.enabled = false;
            ragFairSellAmountButtonCollider.enabled = false;

            // Start choosing amount
            Mod.stackSplitUI.gameObject.SetActive(true);
            Mod.stackSplitUI.transform.position = Mod.rightHand.transform.position + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.rotation = Quaternion.Euler(0, Mod.rightHand.transform.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.position;
            amountChoiceRightVector = Mod.rightHand.transform.right;
            amountChoiceRightVector.y = 0;

            choosingRagFairSellAmount = true;
            startedChoosingThisFrame = true;

            // Set max buy amount, limit it to 360 otherwise scale is not large enough and its hard to specify an exact value
            maxBuyAmount = 10000000;
        }

        private void TakeInput()
        {
            if (choosingBuyAmount || choosingRagfairBuyAmount || choosingRagFairSellAmount)
            {
                FVRViveHand hand = Mod.rightHand.fvrHand;
                string countString;
                if (startedChoosingThisFrame)
                {
                    // Skip just this frame because if we started this frame, for sure the trigger is down
                    startedChoosingThisFrame = false;
                }
                else if (hand.Input.TriggerDown)
                {
                    if (choosingBuyAmount)
                    {
                        cartItemCount = chosenAmount;
                        countString = cartItemCount.ToString();
                        List<BarterPrice> pricesToUse = buyPrices;
                        buyItemCount.text = countString;

                        foreach (BarterPrice price in pricesToUse)
                        {
                            price.priceItemView.amount.text = (price.count * cartItemCount).ToString();

                            UpdateBuyPriceForItem(price.itemData);
                        }
                    }
                    else if (choosingRagfairBuyAmount)
                    {
                        ragFairCartItemCount = chosenAmount;
                        countString = ragFairCartItemCount.ToString();
                        List<BarterPrice> pricesToUse = ragFairBuyPrices;
                        ragFairBuyItemCount.text = countString;

                        foreach (BarterPrice price in pricesToUse)
                        {
                            price.ragFairPriceItemView.amount.text = (price.count * ragFairCartItemCount).ToString();

                            UpdateRagFairBuyPriceForItem(price.itemData);
                        }
                    }
                    else // choosingRagFairSellAmount
                    {
                        currentRagFairSellPrice = chosenAmount;
                        ragFairSellForItemView.amount.text = currentRagFairSellPrice.ToString();
                        currentRagFairSellChance = GetRagFairSellChance(currentRagFairSellItem, currentRagFairSellPrice);
                        ragFairSellChance.text = "Sell Chance: " + (int)(currentRagFairSellChance * 100) + "%";
                    }
                    Mod.stackSplitUI.gameObject.SetActive(false);

                    // Reenable buy amount buttons
                    buyAmountButtonCollider.enabled = true;
                    ragFairBuyAmountButtonCollider.enabled = true;
                    ragFairSellAmountButtonCollider.enabled = true;

                    choosingBuyAmount = false;
                    choosingRagfairBuyAmount = false;
                }
            }
        }

        public void ClearRagFairCategories()
        {
            while (ragFairBuyCategoriesParent.childCount > 1)
            {
                Transform currentFirstChild = ragFairBuyCategoriesParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
        }

        public void AddRagFairCategories(CategoryTreeNode category, Transform currentParent, int step = 0)
        {
            RagFairCategory categoryUI = Instantiate(ragFairBuyCategoryPrefab, currentParent).GetComponent<RagFairCategory>();
            categoryUI.SetCategory(category, step);
            categoryUI.gameObject.SetActive(true);

            for(int i=0; i < category.children.Count; ++i)
            {
                AddRagFairCategories(category.children[i], categoryUI.subList.transform, step + 1);
            }

            categoryUI.toggle.SetActive(categoryUI.subList.transform.childCount > 0);
            if (categoryUI.toggle.activeSelf && category.uncollapsed && !categoryUI.subList.activeSelf)
            {
                categoryUI.OnToggleClicked();
            }
        }

        public void InitRagFair()
        {
            // Init vars
            ragFairListings = new List<RagFairListing>();

            // Buy
            UpdateCategories();
            ragFairCategoriesHoverScrollProcessor.mustUpdateMiddleHeight = 1;

            // Sell
            // Setup selling price display
            ragFairSellForItemView.itemName.text = roubleItemData.name;
            ragFairSellForItemView.itemView.SetItemData(roubleItemData);

            // Disable lsit button by default
            ragFairSellListButton.SetActive(false);

            // Add all items in trade volume that can be sold on rag fair
            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < volumeItemEntry.Value.Count; ++i)
                {
                    AddRagFairSellItem(volumeItemEntry.Value[i]);
                }
            }

            // Listings
            for(int i=0; i < ragFairListings.Count; ++i)
            {
                AddRagFairListing(ragFairListings[i]);
            }

            // Wishlist
            for (int i = 0; i < Mod.wishList.Count; ++i)
            {
                AddRagFairWishlistEntry(Mod.wishList[i]);
            }
        }

        public void AddRagFairListing(RagFairListing listing)
        {
            RagFairListingUI listingUI = Instantiate(ragFairListingPrefab, ragFairListingsParent).GetComponent<RagFairListingUI>();
            listingUI.SetListing(listing);
            listing.UI = listingUI;
        }

        public void AddRagFairWishlistEntry(MeatovItemData itemData)
        {
            RagFairWishlistItemView ragFairWishlistItemView = Instantiate(ragFairWishlistItemPrefab, ragFairWishlistParent).GetComponent<RagFairWishlistItemView>();
            ragFairWishlistItemView.SetItemData(itemData);
            itemData.ragFairWishlistItemView = ragFairWishlistItemView;
        }

        public void SetTrader(int index, string defaultItemID = null)
        {
            Mod.LogInfo("set trader called with index: " + index);
            currentTraderIndex = index;
            Trader trader = Mod.traders[index];
            if (trader.currency == 0)
            {
                currencyItemData = Mod.customItemData[203];
            }
            else if (trader.currency == 1)
            {
                currencyItemData = Mod.customItemData[201];
            }
            else // 2
            {
                currencyItemData = Mod.customItemData[202];
            }

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
                    GenerateFenceAssort();
                }

                for (int i = 0; i <= trader.level; ++i)
                {
                    List<Barter> barters = trader.bartersByLevel[i + 1];

                    for (int j = 0; j < barters.Count; ++j)
                    {
                        Barter currentBarter = barters[j];

                        // Skip if missing item data
                        if (currentBarter.itemData == null)
                        {
                            Mod.LogWarning("Trader " + trader.name + " with ID " + trader.ID + " has barter "+j+" at level "+i+" with missing itemdata");
                            continue;
                        }
                        int priceCount = 0;
                        int firstValidPrice = -1;
                        for (int k = 0; k < currentBarter.prices.Length; ++k)
                        {
                            if (currentBarter.prices[k].itemData != null)
                            {
                                ++priceCount;
                                if (firstValidPrice == -1)
                                {
                                    firstValidPrice = k;
                                }
                            }
                        }
                        if (priceCount == 0)
                        {
                            Mod.LogWarning("Trader " + trader.name + " with ID " + trader.ID + " has barter " + j + " at level " + i + " with no price itemdata");
                            continue;
                        }

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
                            if (!Mod.ItemIDToCurrencyIndex(currentBarter.prices[firstValidPrice].itemData.H3ID, out currencyToUse))
                            {
                                currencyToUse = 3;
                            }
                        }
                        itemView.SetItemData(currentBarter.itemData, false, false, false, null, true, currencyToUse, valueToUse, false, false);

                        // Setup button
                        PointableButton pointableButton = currentItemView.GetComponent<PointableButton>();
                        pointableButton.Button.onClick.AddListener(() => { OnBuyItemClick(currentBarter.itemData, currentBarter.prices); });
                    }
                }
            }

            OnBuyItemClick(null, null);

            buyShowcaseHoverScrollProcessor.mustUpdateMiddleHeight = 1;

            Mod.LogInfo("0");
            // Sell
            // Setup selling price display
            sellItemName.text = currencyItemData.name;
            sellItemView.itemView.SetItemData(currencyItemData);
            Mod.LogInfo("0");

            // Reset showcase
            while (sellShowcaseContent.childCount > 1)
            {
                Transform currentFirstChild = sellShowcaseContent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("Adding all sellable item in volume to sell showcase");

            // Add all items in trade volume that are sellable at this trader to showcase
            sellDealButton.SetActive(false);

            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for(int i=0; i< volumeItemEntry.Value.Count; ++i)
                {
                    Mod.LogInfo("\tAdding item from volume: " + volumeItemEntry.Value[i].name);
                    AddSellItem(volumeItemEntry.Value[i]);
                }
            }
            Mod.LogInfo("0");

            // Tasks
            while (tasksContent.childCount > 1)
            {
                Transform currentFirstChild = tasksContent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.LogInfo("0");
            // Add all of this trader's available, active, and complete tasks to the list
            foreach (Task task in trader.tasks)
            {
                Mod.LogInfo("Check if can add task " + task.name + " to task list, its state is: " + task.taskState);
                if (task.taskState == Task.TaskState.Available
                    || task.taskState == Task.TaskState.Active
                    || task.taskState == Task.TaskState.Complete)
                {
                    AddTask(task);
                }
                else
                {
                    task.marketUI = null;
                }
            }
            Mod.LogInfo("0");

            // Insure
            // Setup insure price display
            insureItemName.text = currencyItemData.name;
            insureItemView.itemView.SetItemData(currencyItemData);

            // Reset showcase
            while (insureShowcaseContent.childCount > 1)
            {
                Transform currentFirstChild = insureShowcaseContent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            Mod.LogInfo("Adding all item in volume to insure showcase");
            // Add all items in trade volume that are insureable at this trader to showcase
            insureDealButton.SetActive(false);

            if (trader.insuranceAvailable)
            {
                foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
                {
                    for (int i = 0; i < volumeItemEntry.Value.Count; ++i)
                    {
                        Mod.LogInfo("\tAdding item from volume: " + volumeItemEntry.Value[i].name);
                        AddInsureItem(volumeItemEntry.Value[i]);
                    }
                }
            }

            Mod.LogInfo("0");
        }

        public void GenerateFenceAssort()
        {
            Trader fence = Mod.traders[2];

            // Remove existing barters from categories
            if(fence.bartersByItemID.Count > 0)
            {
                foreach (KeyValuePair<string, List<Barter>> barters in fence.bartersByItemID)
                {
                    for (int i = 0; i < barters.Value.Count; ++i)
                    {
                        for (int j = 0; j < barters.Value[i].itemData.parents.Length; ++j)
                        {
                            CategoryTreeNode category = Mod.itemCategories.FindChild(barters.Value[i].itemData.parents[j]);
                            if (category != null)
                            {
                                category.barters.Remove(barters.Value[i]);
                            }
                        }

                        if(ragFairCartBarter == barters.Value[i])
                        {
                            ragFairCartBarter = null;
                            ragFairCartItem = null;

                            ragFairBuyDealButton.SetActive(false);
                        }
                    }
                }
            }

            // Clear existing barters
            fence.bartersByLevel.Clear();
            fence.bartersByItemID.Clear();

            // We want everyone to use the same seed for generating random fence barters
            UnityEngine.Random.InitState(Convert.ToInt32((DateTime.UtcNow - DateTime.Today.ToUniversalTime()).TotalHours));

            // Generate 25-50 random barters from fence's buyCategories and buyBlacklist
            int generateCount = UnityEngine.Random.Range(25, 51);
            for (int i=0; i < generateCount; ++i)
            {
                CategoryTreeNode randomCategory = Mod.itemCategories.FindChild(fence.buyCategories[UnityEngine.Random.Range(0, fence.buyCategories.Length)]);
                if (randomCategory != null)
                {
                    if(Mod.itemsByParents.TryGetValue(randomCategory.ID, out List<MeatovItemData> items))
                    {
                        MeatovItemData randomItem = items[UnityEngine.Random.Range(0, items.Count)];

                        // Note that fence should only ever have a single barter for a specific item since they would all have the same price anyway
                        if (!fence.bartersByItemID.ContainsKey(randomItem.H3ID))
                        {
                            Barter barter = new Barter();
                            barter.itemData = randomItem;
                            barter.prices = new BarterPrice[1];
                            barter.prices[0] = new BarterPrice();
                            barter.prices[0].itemData = roubleItemData;
                            // Barter price will be a little lower than ragfair if can sell on ragfair, or 1.5x higher if can't
                            barter.prices[0].count = randomItem.canSellOnRagfair ? (int)(randomItem.value * FENCE_PRICE_MULT) : (int)(randomItem.value * FENCE_PRICE_MULT * 1.5f);

                            randomCategory.barters.Add(barter);
                            if (fence.bartersByLevel.TryGetValue(1, out List<Barter> levelBarters))
                            {
                                levelBarters.Add(barter);
                            }
                            else
                            {
                                fence.bartersByLevel.Add(1, new List<Barter>() { barter });
                            }

                            fence.bartersByItemID.Add(randomItem.H3ID, new List<Barter>() { barter });
                        }
                    }
                }
            }

            UpdateCategories();
        }

        public void UpdateCategories()
        {
            ClearRagFairCategories();
            AddRagFairCategories(Mod.itemCategories, ragFairBuyCategoriesParent);
        }

        public void AddTask(Task task)
        {
            // Instantiate task element
            GameObject currentTaskElement = Instantiate(taskPrefab, tasksContent);
            currentTaskElement.SetActive(true);
            task.marketUI = currentTaskElement.GetComponent<TaskUI>();

            // Set task UI
            task.marketUI.SetTask(task, true);

            tasksHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void OnBuyItemClick(MeatovItemData item, BarterPrice[] priceList)
        {
            cartItem = item;

            if (buyItemPriceViewsByH3ID == null)
            {
                buyItemPriceViewsByH3ID = new Dictionary<string, List<PriceItemView>>();
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
                    if(price.itemData == null)
                    {
                        continue;
                    }

                    Mod.LogInfo("\tSetting price: " + price.itemData.H3ID);
                    Transform priceElement = Instantiate(buyPricePrefab, buyPricesContent).transform;
                    priceElement.gameObject.SetActive(true);
                    PriceItemView currentPriceView = priceElement.GetComponentInChildren<PriceItemView>();
                    currentPriceView.price = price;
                    price.priceItemView = currentPriceView;

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

                    if(buyItemPriceViewsByH3ID.TryGetValue(price.itemData.H3ID, out List<PriceItemView> itemViews))
                    {
                        itemViews.Add(currentPriceView);
                    }
                    else
                    {
                        buyItemPriceViewsByH3ID.Add(price.itemData.H3ID, new List<PriceItemView> { currentPriceView });
                    }
                }

                buyDealButton.SetActive(canDeal);
            }

            buyPricesHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void OnBuyDealClick()
        {
            // Remove price from trade volume
            foreach (BarterPrice price in buyPrices)
            {
                RemoveItemFromTrade(price.itemData, price.count * cartItemCount, price.dogTagLevel);
            }

            // Add bought amount of item to trade volume
            tradeVolume.SpawnItem(cartItem, cartItemCount);

            // Update amount of item in trader's assort
            //Mod.traders[currentTraderIndex].assortmentByLevel[Mod.traders[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack -= cartItemCount;
        }

        public void OnBuyAmountClick()
        {
            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                Mod.splittingItem.CancelSplit();
            }

            // Disable buy deal/amount buttons until done choosing amount
            buyAmountButtonCollider.enabled = false;
            ragFairBuyAmountButtonCollider.enabled = false;
            ragFairSellAmountButtonCollider.enabled = false;

            // Start choosing amount
            Mod.stackSplitUI.gameObject.SetActive(true);
            Mod.stackSplitUI.transform.position = Mod.rightHand.transform.position + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.rotation = Quaternion.Euler(0, Mod.rightHand.transform.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.position;
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
                        meatovItem.DetachChildren();
                        meatovItem.Destroy();
                    }
                }
            }

            // Add sold for item to trade volume
            Trader trader = Mod.traders[currentTraderIndex];
            int currencyIndex = trader.currency == 0 ? 203 : (trader.currency == 1 ? 201 : 202);
            MeatovItemData currencyItemData = Mod.customItemData[currencyIndex];

            tradeVolume.SpawnItem(currencyItemData, currentTotalSellingPrice);

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

        public void OnInsureDealClick()
        {
            // Insure all insureable items in trade volume
            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < volumeItemEntry.Value.Count; ++i)
                {
                    MeatovItem meatovItem = volumeItemEntry.Value[i];

                    if (Mod.traders[currentTraderIndex].ItemInsureable(meatovItem.itemData))
                    {
                        meatovItem.insured = true;
                    }
                }
            }

            // Remove insurance price from trade volume
            RemoveItemFromTrade(currencyItemData, currentTotalInsurePrice);

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

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
                    item.Destroy();
                }
                else // item.stack == amountToRemove
                {
                    item.Destroy();
                    break;
                }
            }
        }

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
            tradeVolume.OnItemAdded -= OnTradeVolumeItemAdded;
            tradeVolume.OnItemRemoved -= OnItemRemoved;
            MeatovItemData.OnAddedToWishlist -= OnItemAddedToWishlist;
            Mod.OnPlayerLevelChanged -= OnPlayerLevelChanged;
            for (int i = 0; i < Mod.traders.Length; ++i)
            {
                Mod.traders[i].OnTraderLevelChanged -= OnTraderLevelChanged;
            }
        }
    }
}
