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
        public Dictionary<string, List<PriceItemView>> buyItemPriceViewsByID;
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
        public Dictionary<string, List<PriceItemView>> ragFairBuyItemPriceViewsByID;
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
        public List<MeatovItemData> cartItems;
        [NonSerialized]
        public int cartItemCount;
        public List<BarterPrice> buyPrices;
        public List<MeatovItemData> ragFairCartItems;
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
                    string currentTarkovID = vanillaCustomData["tarkovID"].ToString();
                    if (!meatovItem.itemDataSet || !meatovItem.tarkovID.Equals(currentTarkovID))
                    {
                        meatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                    }
                    meatovItem.insured = (bool)vanillaCustomData["insured"];
                    meatovItem.looted = (bool)vanillaCustomData["looted"];
                    meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];

                    for (int m = 1; m < objs.Count; ++m)
                    {
                        MeatovItem childMeatovItem = objs[m].GetComponent<MeatovItem>();
                        if (childMeatovItem != null)
                        {
                            currentTarkovID = vanillaCustomData["children"][m - 1]["tarkovID"].ToString();
                            if (!childMeatovItem.itemDataSet || !childMeatovItem.tarkovID.Equals(currentTarkovID))
                            {
                                childMeatovItem.SetData(Mod.defaultItemData[currentTarkovID], true);
                            }
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
            sellDealButton.SetActive(currentTotalSellingPrice > 0);
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
            sellDealButton.SetActive(currentTotalSellingPrice > 0);
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
            if (buyItemPriceViewsByID.TryGetValue(itemData.tarkovID, out List<PriceItemView> itemViews))
            {
                for(int j=0; j< itemViews.Count; ++j)
                {
                    PriceItemView itemView = itemViews[j];
                    bool prefulfilled = itemView.fulfilledIcon.activeSelf;
                    int count = 0;
                    tradeVolume.inventory.TryGetValue(itemData.tarkovID, out count);
                    itemView.amount.text = Mathf.Min(itemView.price.count * cartItemCount, count).ToString() + "/" + (itemView.price.count * cartItemCount).ToString();
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
                                allFulfilled |= tradeVolume.inventory.TryGetValue(buyPrices[i].itemData.tarkovID, out currentCount) && currentCount >= buyPrices[i].count * cartItemCount;
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
                tradeVolume.inventory.TryGetValue(itemData.tarkovID, out count);
                insureItemView.amount.text = count.ToString() + "/" + currentTotalInsurePrice.ToString();
                if (count >= currentTotalInsurePrice)
                {
                    insurePriceFulfilled.SetActive(true);
                    insurePriceUnfulfilled.SetActive(false);
                    insureDealButton.SetActive(true);
                }
                else
                {
                    insurePriceFulfilled.SetActive(false);
                    insurePriceUnfulfilled.SetActive(true);
                    insureDealButton.SetActive(false);
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
                if (inventory.ContainsKey(item.tarkovID))
                {
                    inventory[item.tarkovID] += stackDifference;
                    if (item.foundInRaid)
                    {
                        FIRInventory[item.tarkovID] += stackDifference;
                    }

                    if (inventory[item.tarkovID] <= 0)
                    {
                        Mod.LogError("DEV: Market AddToInventory stackonly with difference " + stackDifference + " for " + item.name + " reached 0 count:\n" + Environment.StackTrace);
                        inventory.Remove(item.tarkovID);
                        inventoryItems.Remove(item.tarkovID);
                        FIRInventory.Remove(item.tarkovID);
                        FIRInventoryItems.Remove(item.tarkovID);
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
                if (inventory.ContainsKey(item.tarkovID))
                {
                    inventory[item.tarkovID] += item.stack;
                    inventoryItems[item.tarkovID].Add(item);
                }
                else
                {
                    inventory.Add(item.tarkovID, item.stack);
                    inventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
                }

                if (item.foundInRaid)
                {
                    if (FIRInventory.ContainsKey(item.tarkovID))
                    {
                        FIRInventory[item.tarkovID] += item.stack;
                        FIRInventoryItems[item.tarkovID].Add(item);
                    }
                    else
                    {
                        FIRInventory.Add(item.tarkovID, item.stack);
                        FIRInventoryItems.Add(item.tarkovID, new List<MeatovItem> { item });
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
            if (inventory.ContainsKey(item.tarkovID))
            {
                inventory[item.tarkovID] -= item.stack;
                inventoryItems[item.tarkovID].Remove(item);
            }
            else
            {
                Mod.LogError("Attempting to remove "+item.tarkovID + ":" + item.H3ID + " from market inventory but key was not found in it:\n" + Environment.StackTrace);
                return;
            }
            if (inventory[item.tarkovID] == 0)
            {
                inventory.Remove(item.tarkovID);
                inventoryItems.Remove(item.tarkovID);
            }

            if (item.foundInRaid)
            {
                if (FIRInventory.ContainsKey(item.tarkovID))
                {
                    FIRInventory[item.tarkovID] -= item.stack;
                    FIRInventoryItems[item.tarkovID].Remove(item);
                }
                if (FIRInventory[item.tarkovID] == 0)
                {
                    FIRInventory.Remove(item.tarkovID);
                    FIRInventoryItems.Remove(item.tarkovID);
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
            ragFairCartItems = ragFairBarter.itemData;
            ragFairCartBarter = ragFairBarter;

            if (ragFairBuyItemPriceViewsByID == null)
            {
                ragFairBuyItemPriceViewsByID = new Dictionary<string, List<PriceItemView>>();
            }
            else
            { 
                ragFairBuyItemPriceViewsByID.Clear();
            }

            ragFairBuyCart.SetActive(true);
            ragFairBuyItemView.itemView.SetItemData(ragFairBarter.itemData[0]);
            ragFairBuyItemView.itemName.text = ragFairBarter.itemData[0].name;
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
                tradeVolume.inventory.TryGetValue(price.itemData.tarkovID, out count);
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
                if(ragFairBuyItemPriceViewsByID.TryGetValue(price.itemData.tarkovID, out List<PriceItemView> priceItemViewList))
                {
                    priceItemViewList.Add(currentPriceView);
                }
                else
                {
                    ragFairBuyItemPriceViewsByID.Add(price.itemData.tarkovID, new List<PriceItemView> { currentPriceView });
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
                        && (!category.barters[i].trader.rewardBarters.TryGetValue(category.barters[i].itemData[0].tarkovID, out bool unlocked)
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
                if(ragFairCartBarter.trader != null)
                {
                    if (price.itemData.index == 201 || price.itemData.index == 202 || price.itemData.index == 203)
                    {
                        ragFairCartBarter.trader.salesSum += price.count * ragFairCartItemCount;
                    }
                }

                RemoveItemFromTrade(price.itemData, price.count * ragFairCartItemCount, price.dogTagLevel);
            }

            // Add bought amount of item to trade volume
            for(int i = 0; i< ragFairCartItems.Count; ++i)
            {
                tradeVolume.SpawnItem(ragFairCartItems[i], ragFairCartItemCount);
            }
        }

        public void OnRagFairBuyAmountClicked()
        {
            if(ragFairCartItems == null)
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
            if (ragFairBuyItemPriceViewsByID != null && ragFairBuyItemPriceViewsByID.TryGetValue(itemData.tarkovID, out List<PriceItemView> itemViews))
            {
                for(int j=0; j< itemViews.Count; ++j)
                {
                    PriceItemView itemView = itemViews[j];
                    bool prefulfilled = itemView.fulfilledIcon.activeSelf;
                    int count = 0;
                    tradeVolume.inventory.TryGetValue(itemData.tarkovID, out count);
                    itemView.amount.text = Mathf.Min(itemView.price.count * ragFairCartItemCount, count).ToString() + "/" + (itemView.price.count * ragFairCartItemCount).ToString();
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
                                allFulfilled |= tradeVolume.inventory.TryGetValue(ragFairBuyPrices[i].itemData.tarkovID, out currentCount) && currentCount >= ragFairBuyPrices[i].count * ragFairCartItemCount;
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
            if (!item.canSellOnRagfair || item.itemData.value == 0)
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
            if (!item.canSellOnRagfair || item.itemData.value == 0)
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
                if (Mod.traders[i].bartersByItemID.TryGetValue(item.tarkovID, out List<Barter> barters))
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
            if(tradeVolume.inventory.TryGetValue(roubleItemData.tarkovID, out amount))
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
                    traderDetailsNextLevelText.text = Trader.LevelToRoman(trader.level + 1);
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
                        int nullCount = 0;
                        for (int k=0; k< currentBarter.itemData.Count; ++k)
                        {
                            if (currentBarter.itemData[k] == null)
                            {
                                ++nullCount;
                            }
                        }
                        if (nullCount == currentBarter.itemData.Count)
                        {
                            Mod.LogWarning("Trader " + trader.name + " with ID " + trader.ID + " has barter " + j + " at level " + i + " with missing itemdata items");
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
                        if (currentBarter.needUnlock && !trader.rewardBarters[currentBarter.itemData[0].tarkovID])
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
                            if (!Mod.ItemIDToCurrencyIndex(currentBarter.prices[firstValidPrice].itemData.tarkovID, out currencyToUse))
                            {
                                currencyToUse = 3;
                            }
                        }
                        itemView.SetItemData(currentBarter.itemData[0], false, false, false, null, true, currencyToUse, valueToUse, false, false);

                        // Setup button
                        PointableButton pointableButton = currentItemView.GetComponent<PointableButton>();
                        pointableButton.GetComponent<Button>().onClick.AddListener(() => { OnBuyItemClick(currentBarter.itemData, currentBarter.prices); });
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
                        for (int j = 0; j < barters.Value[i].itemData[0].parents.Length; ++j)
                        {
                            CategoryTreeNode category = Mod.itemCategories.FindChild(barters.Value[i].itemData[0].parents[j]);
                            if (category != null)
                            {
                                category.barters.Remove(barters.Value[i]);
                            }
                        }

                        if(ragFairCartBarter == barters.Value[i])
                        {
                            ragFairCartBarter = null;
                            ragFairCartItems = null;

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
                        if (!fence.bartersByItemID.ContainsKey(randomItem.tarkovID) && randomItem.value != 0)
                        {
                            Barter barter = new Barter();
                            barter.itemData = new List<MeatovItemData>();
                            barter.itemData.Add(randomItem);
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

                            fence.bartersByItemID.Add(randomItem.tarkovID, new List<Barter>() { barter });
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

        public void OnBuyItemClick(List<MeatovItemData> items, BarterPrice[] priceList)
        {
            cartItems = items;

            if (buyItemPriceViewsByID == null)
            {
                buyItemPriceViewsByID = new Dictionary<string, List<PriceItemView>>();
            }
            else
            {
                buyItemPriceViewsByID.Clear();
            }

            if (items == null || items[0] == null)
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
                Mod.LogInfo("on buy item click called, with ID: " + items[0].tarkovID+":"+ items[0].H3ID);
                Mod.LogInfo("Got item name: " + items[0].name);

                buyItemView.itemView.SetItemData(items[0]);
                buyItemView.itemName.text = items[0].name;
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

                    Mod.LogInfo("\tSetting price: "+price.itemData.tarkovID + ":" + price.itemData.H3ID);
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
                    tradeVolume.inventory.TryGetValue(price.itemData.tarkovID, out count);
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

                    if(buyItemPriceViewsByID.TryGetValue(price.itemData.tarkovID, out List<PriceItemView> itemViews))
                    {
                        itemViews.Add(currentPriceView);
                    }
                    else
                    {
                        buyItemPriceViewsByID.Add(price.itemData.tarkovID, new List<PriceItemView> { currentPriceView });
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
                if(price.itemData.index == 201 || price.itemData.index == 202 || price.itemData.index == 203)
                {
                    Mod.traders[currentTraderIndex].salesSum += price.count * cartItemCount;
                }

                RemoveItemFromTrade(price.itemData, price.count * cartItemCount, price.dogTagLevel);
            }

            // Add bought amount of item to trade volume
            for(int i = 0; i < cartItems.Count; ++i)
            {
                tradeVolume.SpawnItem(cartItems[i], cartItemCount);
            }

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
            Mod.traders[currentTraderIndex].salesSum += currentTotalSellingPrice;
            int totalSellingPrice = currentTotalSellingPrice;

            // Remove all sellable items from trade volume
            List<MeatovItem> itemsToSell = new List<MeatovItem>();
            foreach (KeyValuePair<string, List<MeatovItem>> volumeItemEntry in tradeVolume.inventoryItems)
            {
                for (int i = 0; i < volumeItemEntry.Value.Count; ++i)
                {
                    MeatovItem meatovItem = volumeItemEntry.Value[i];

                    if (Mod.traders[currentTraderIndex].ItemSellable(meatovItem.itemData))
                    {
                        itemsToSell.Add(meatovItem);
                    }
                }
            }
            for(int i=0; i < itemsToSell.Count; ++i)
            {
                itemsToSell[i].DetachChildren();
                itemsToSell[i].Destroy();
            }

            // Add sold for item to trade volume
            Trader trader = Mod.traders[currentTraderIndex];
            int currencyIndex = trader.currency == 0 ? 203 : (trader.currency == 1 ? 201 : 202);
            MeatovItemData currencyItemData = Mod.customItemData[currencyIndex];

            tradeVolume.SpawnItem(currencyItemData, totalSellingPrice);

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

        public void OnInsureDealClick()
        {
            Mod.traders[currentTraderIndex].salesSum += currentTotalInsurePrice;
            int totalInsurePrice = currentTotalInsurePrice;

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
            RemoveItemFromTrade(currencyItemData, totalInsurePrice);

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

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
                if (inventoryItemsToUse.TryGetValue(itemData.tarkovID, out List<MeatovItem> items))
                {
                    int lowestStack = int.MaxValue;
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
                    Mod.LogError("DEV: Market RemoveItemFromTrade did not find suitable item for "+itemData.tarkovID + ":" + itemData.H3ID + " with " + amountToRemove + " amount left to remove");
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
