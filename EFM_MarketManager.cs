﻿using System;
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
        public EFM_TraderTab[] traderTabs;
        public EFM_TraderTab[] ragFairTabs;
        public Dictionary<string, GameObject> wishListItemViewsByID;
        public Dictionary<string, List<GameObject>> ragFairItemBuyViewsByID;

        public int currentTraderIndex;
        public Dictionary<string, int> tradeVolumeInventory;
        public string cartItem;
        public int cartItemCount;
        public Dictionary<string, int> prices;
        public Dictionary<string, GameObject> buyPriceElements;
        public Dictionary<string, GameObject> sellItemShowcaseElements;
        public int currentTotalSellingPrice = 0;

        private bool initButtonsSet;

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

            // Setup trader tabs
            Transform tabsParent = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2);
            for (int i = 0; i < 4;++i)
            {
                Transform tab = tabsParent.GetChild(i);
                if (traderTabs == null)
                {
                    traderTabs = new EFM_TraderTab[4];
                }

                EFM_TraderTab tabScript = tab.gameObject.AddComponent<EFM_TraderTab>();
                traderTabs[i] = tabScript;
                tabScript.SetButton();
                tabScript.MaxPointingRange = 20;
                tabScript.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                tabScript.clickSound = clickAudio;
                tabScript.Button.onClick.AddListener(() => { tabScript.OnClick(i); });
                tabScript.page = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(i).gameObject;

                tabScript.tabs = traderTabs;
            }

            // Setup rag fair
            // Buy
            Transform categoriesParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject categoryTemplate = categoriesParent.GetChild(0).gameObject;
            EFM_PointableButton categoryTemplateMainButton = categoryTemplate.transform.GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            categoryTemplateMainButton.SetButton();
            categoryTemplateMainButton.MaxPointingRange = 20;
            categoryTemplateMainButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            EFM_PointableButton categoryTemplateToggleButton = categoryTemplate.transform.GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            categoryTemplateToggleButton.SetButton();
            categoryTemplateToggleButton.MaxPointingRange = 20;
            categoryTemplateToggleButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            AddRagFairCategories(Mod.itemCategories.children, categoriesParent, categoryTemplate, 1);
            ragFairItemBuyViewsByID = new Dictionary<string, List<GameObject>>();

            // Setup buy categories hoverscrolls
            EFM_HoverScroll newBuyCategoriesDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
            EFM_HoverScroll newBuyCategoriesUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
            newBuyCategoriesDownHoverScroll.MaxPointingRange = 30;
            newBuyCategoriesDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newBuyCategoriesDownHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newBuyCategoriesDownHoverScroll.other = newBuyCategoriesUpHoverScroll;
            newBuyCategoriesDownHoverScroll.up = false;
            newBuyCategoriesUpHoverScroll.MaxPointingRange = 30;
            newBuyCategoriesUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newBuyCategoriesUpHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newBuyCategoriesUpHoverScroll.other = newBuyCategoriesDownHoverScroll;
            newBuyCategoriesUpHoverScroll.up = true;
            float buyCategoriesHeight = 3 + 12 * Mod.itemCategories.children.Count;
            if (buyCategoriesHeight > 186)
            {
                newBuyCategoriesUpHoverScroll.rate = 186 / (buyCategoriesHeight - 186);
                newBuyCategoriesDownHoverScroll.rate = 186 / (buyCategoriesHeight - 186);
                newBuyCategoriesDownHoverScroll.gameObject.SetActive(true);
            }

            // Setup buy items hoverscrolls
            EFM_HoverScroll newBuyItemsDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
            EFM_HoverScroll newBuyItemsUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
            newBuyItemsDownHoverScroll.MaxPointingRange = 30;
            newBuyItemsDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newBuyItemsDownHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newBuyItemsDownHoverScroll.other = newBuyItemsUpHoverScroll;
            newBuyItemsDownHoverScroll.up = false;
            newBuyItemsUpHoverScroll.MaxPointingRange = 30;
            newBuyItemsUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newBuyItemsUpHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newBuyItemsUpHoverScroll.other = newBuyItemsDownHoverScroll;
            newBuyItemsUpHoverScroll.up = true;

            // Wishlist
            Transform wishlistParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject wishlistItemViewTemplate = wishlistParent.GetChild(0).gameObject;
            EFM_PointableButton wishlistItemViewTemplateWishButton = wishlistItemViewTemplate.transform.GetChild(2).gameObject.AddComponent<EFM_PointableButton>();
            categoryTemplateMainButton.SetButton();
            categoryTemplateMainButton.MaxPointingRange = 20;
            categoryTemplateMainButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            wishListItemViewsByID = new Dictionary<string, GameObject>();
            foreach (string wishlistItemID in Mod.wishList)
            {
                GameObject wishlistItemView = Instantiate(wishlistItemViewTemplate, wishlistParent);
                wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[wishlistItemID];
                wishlistItemView.transform.GetChild(1).GetComponent<Text>().text = Mod.itemNames[wishlistItemID];

                wishlistItemView.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairWishlistItemWishClick(wishlistItemID); });
                wishListItemViewsByID.Add(wishlistItemID, wishlistItemView);
            }

            // Setup buy categories hoverscrolls
            EFM_HoverScroll newWishlistDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
            EFM_HoverScroll newWishlistUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
            newWishlistDownHoverScroll.MaxPointingRange = 30;
            newWishlistDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newWishlistDownHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newWishlistDownHoverScroll.other = newWishlistUpHoverScroll;
            newWishlistDownHoverScroll.up = false;
            newWishlistUpHoverScroll.MaxPointingRange = 30;
            newWishlistUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newWishlistUpHoverScroll.scrollbar = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
            newWishlistUpHoverScroll.other = newWishlistDownHoverScroll;
            newWishlistUpHoverScroll.up = true;
            float wishlistHeight = 3 + 34 * Mod.wishList.Count;
            if (wishlistHeight > 190)
            {
                newWishlistUpHoverScroll.rate = 190 / (wishlistHeight - 190);
                newBuyCategoriesDownHoverScroll.rate = 190 / (wishlistHeight - 190);
                newBuyCategoriesDownHoverScroll.gameObject.SetActive(true);
            }

            // Setup rag fair tabs
            Transform ragFaireTabsParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(2);
            for (int i = 0; i < 3; ++i)
            {
                Transform tab = ragFaireTabsParent.GetChild(i);
                if (ragFairTabs == null)
                {
                    ragFairTabs = new EFM_TraderTab[3];
                }

                EFM_TraderTab tabScript = tab.gameObject.AddComponent<EFM_TraderTab>();
                ragFairTabs[i] = tabScript;
                tabScript.SetButton();
                tabScript.MaxPointingRange = 20;
                tabScript.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                tabScript.clickSound = clickAudio;
                tabScript.Button.onClick.AddListener(() => { tabScript.OnClick(i); });
                tabScript.page = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(i).gameObject;

                tabScript.tabs = ragFairTabs;
            }
        }

        private void AddRagFairCategories(List<EFM_CategoryTreeNode> children, Transform parent, GameObject template, int level)
        {
            foreach(EFM_CategoryTreeNode child in children)
            {
                GameObject category = Instantiate(template, parent);
                category.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().padding = new RectOffset(level * 10, 0, 0, 0);
                category.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = child.name;
                category.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = "(" + ((child.children.Count == 0 && Mod.itemsByParents.ContainsKey(child.ID)) ? Mod.itemsByParents[child.ID].Count : child.children.Count) + ")";

                // Setup buttons
                category.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairCategoryMainClick(category, child.ID); });
                category.transform.GetChild(0).GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairCategoryToggleClick(category); });

                // Setup actual item entries if this is a leaf category
                if(child.children.Count == 0)
                {
                    if (Mod.itemsByParents.ContainsKey(child.ID))
                    {
                        List<string> itemIDs = Mod.itemsByParents[child.ID];
                        foreach (string itemID in itemIDs)
                        {
                            GameObject item = Instantiate(template, category.transform.GetChild(1));
                            item.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((level + 1) * 10, 0, 0, 0);
                            item.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);

                            item.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = Mod.itemNames[itemID];
                            item.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = "(" + GetTotalItemSell(itemID) + ")";

                            category.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairItemMainClick(item, itemID); });
                        }
                    }
                }
                else
                {
                    AddRagFairCategories(child.children, category.transform.GetChild(1), template, level + 1);
                }
            }
        }

        public void SetTrader(int index)
        {
            currentTraderIndex = index;
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
            float buyShowCaseHeight = 27; // Top padding + horizontal
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
                                buyShowCaseHeight += 24; // horizontal
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
                                setDefaultBuy = true;
                            }
                        }
                    }
                }
                // Setup buttons
                if (!initButtonsSet)
                {
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

                    // Set hover scrolls
                    EFM_HoverScroll newDownHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
                    EFM_HoverScroll newUpHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                    newDownHoverScroll.MaxPointingRange = 30;
                    newDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newDownHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                    newDownHoverScroll.other = newUpHoverScroll;
                    newDownHoverScroll.up = false;
                    newUpHoverScroll.MaxPointingRange = 30;
                    newUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newUpHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                    newUpHoverScroll.other = newDownHoverScroll;
                    newUpHoverScroll.up = true;

                    // Set price hover scrolls
                    EFM_HoverScroll newPriceDownHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(3).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
                    EFM_HoverScroll newPriceUpHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(3).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                    newPriceDownHoverScroll.MaxPointingRange = 30;
                    newPriceDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newPriceDownHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                    newPriceDownHoverScroll.other = newPriceUpHoverScroll;
                    newPriceDownHoverScroll.up = false;
                    newPriceUpHoverScroll.MaxPointingRange = 30;
                    newPriceUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                    newPriceUpHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(3).GetChild(1).GetComponent<Scrollbar>();
                    newPriceUpHoverScroll.other = newPriceDownHoverScroll;
                    newPriceUpHoverScroll.up = true;
                }
                EFM_HoverScroll downHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
                EFM_HoverScroll upHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
                if (buyShowCaseHeight > 150)
                {
                    downHoverScroll.rate = 150 / (buyShowCaseHeight - 150);
                    upHoverScroll.rate = 150 / (buyShowCaseHeight - 150);
                    downHoverScroll.gameObject.SetActive(true);
                    upHoverScroll.gameObject.SetActive(false);
                }
                else
                {
                    downHoverScroll.gameObject.SetActive(false);
                    upHoverScroll.gameObject.SetActive(false);
                }
            }

            // Sell
            List<Transform> currentSellHorizontals = new List<Transform>();
            Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;
            float sellShowCaseHeight = 27; // Top padding + horizontal
            // Clear previous horizontals
            while (sellHorizontalsParent.childCount > 1)
            {
                Destroy(sellHorizontalsParent.GetChild(1));
            }
            // Add all items in trade volume that are sellable at this trader to showcase
            int totalSellingPrice = 0;
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
                    if(tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                    }
                    if (tradeVolumeInventory.ContainsKey(CIW.ID))
                    {
                        tradeVolumeInventory[CIW.ID] += CIW.maxStack > 1 ? CIW.stack : 1;
                    }
                    else
                    {
                        tradeVolumeInventory.Add(CIW.ID, CIW.maxStack > 1 ? CIW.stack : 1);
                    }
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
                    if (tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                    }
                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                    }
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

                TODO: Here need to check if an item entry exists already in sellItemShowcaseElements and if yes just edit that one instead of making a new one

                Transform currentHorizontal = currentSellHorizontals[currentSellHorizontals.Count - 1];
                if (currentSellHorizontals[currentSellHorizontals.Count - 1].childCount == 7)
                {
                    currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                    buyShowCaseHeight += 24; // horizontal
                }

                Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                if(sellItemShowcaseElements == null)
                {
                    sellItemShowcaseElements = new Dictionary<string, GameObject>();
                }
                sellItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                marketItemView.custom = custom;
                marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

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
                marketItemView.value = itemValue;
                totalSellingPrice += itemValue;
                currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemValue.ToString();

                //// Setup button
                //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                //pointableButton.SetButton();
                //pointableButton.Button.onClick.AddListener(() => { OnSellItemClick(currentItemIcon, itemValue, currencyItemID); });
                //pointableButton.MaxPointingRange = 20;
                //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

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
            // Setup selling price display
            if (trader.currency == 0)
            {
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["203"];
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["203"];
            }
            else if (trader.currency == 1)
            {
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["201"];
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["201"];
            }
            traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = totalSellingPrice.ToString();
            currentTotalSellingPrice = totalSellingPrice;
            // Setup button
            if (!initButtonsSet)
            {
                EFM_PointableButton pointableSellDealButton = traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
                pointableSellDealButton.SetButton();
                pointableSellDealButton.Button.onClick.AddListener(() => { OnSellDealClick(); });
                pointableSellDealButton.MaxPointingRange = 20;
                pointableSellDealButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                // Set hover scrolls
                EFM_HoverScroll newSellDownHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                EFM_HoverScroll newSellUpHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
                newSellDownHoverScroll.MaxPointingRange = 30;
                newSellDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                newSellDownHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                newSellDownHoverScroll.other = newSellUpHoverScroll;
                newSellDownHoverScroll.up = false;
                newSellUpHoverScroll.MaxPointingRange = 30;
                newSellUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                newSellUpHoverScroll.scrollbar = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(1).GetComponent<Scrollbar>();
                newSellUpHoverScroll.other = newSellDownHoverScroll;
                newSellUpHoverScroll.up = true;
            }
            EFM_HoverScroll downSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upSellHoverScroll = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            if (sellShowCaseHeight > 150)
            {
                downSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
                upSellHoverScroll.rate = 150 / (sellShowCaseHeight - 150);
                downSellHoverScroll.gameObject.SetActive(true);
                upSellHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downSellHoverScroll.gameObject.SetActive(false);
                upSellHoverScroll.gameObject.SetActive(false);
            }

            // Tasks
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            float taskListHeight = 3; // Top padding
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
            // Setup hoverscrolls
            if (!initButtonsSet)
            {
                EFM_HoverScroll newTaskDownHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                EFM_HoverScroll newTaskUpHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
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
            EFM_HoverScroll downTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
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

            // Insure
            List<Transform> currentInsureHorizontals = new List<Transform>();
            Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject insureHorizontalCopy = insureHorizontalsParent.GetChild(0).gameObject;
            float insureShowCaseHeight = 27; // Top padding + horizontal
            // Clear previous horizontals
            while (insureHorizontalsParent.childCount > 1)
            {
                Destroy(insureHorizontalsParent.GetChild(1));
            }
            if ((bool)Mod.traderBaseDB[trader.index]["insurance"]["availability"])
            {
                // Add all items in trade volume that are insureable at this trader to showcase
                foreach (Transform itemTransform in tradeVolume)
                {
                    EFM_CustomItemWrapper CIW = itemTransform.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor VID = itemTransform.GetComponent<EFM_VanillaItemDescriptor>();
                    List<EFM_MarketItemView> itemViewListToUse = null;
                    string itemID;
                    int itemInsureValue;
                    bool custom = false;
                    if (CIW != null)
                    {
                        if (CIW.marketItemViews == null)
                        {
                            CIW.marketItemViews = new List<EFM_MarketItemView>();
                        }
                        CIW.marketItemViews.Clear();
                        itemViewListToUse = CIW.marketItemViews;

                        itemID = CIW.ID;
                        custom = true;

                        itemInsureValue = CIW.GetValue() * trader.insuranceRate;

                        if (!trader.ItemInsureable(itemID, CIW.parents))
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

                        itemInsureValue = VID.GetValue() * trader.insuranceRate;

                        if (!trader.ItemInsureable(itemID, VID.parents))
                        {
                            continue;
                        }
                    }

                    TODO: Here need to check if an item entry exists already in sellItemShowcaseElements and if yes just edit that one instead of making a new one

                    Transform currentHorizontal = currentInsureHorizontals[currentInsureHorizontals.Count - 1];
                    if (currentInsureHorizontals[currentInsureHorizontals.Count - 1].childCount == 7)
                    {
                        currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
                        insureShowCaseHeight += 24; // horizontal
                    }

                    Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                    if (insureItemShowcaseElements == null)
                    {
                        insureItemShowcaseElements = new Dictionary<string, GameObject>();
                    }
                    insureItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                    currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                    EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                    marketItemView.custom = custom;
                    marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                    marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

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
                    marketItemView.insureValue = itemInsureValue;
                    totalInsurePrice += itemInsureValue;
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemInsureValue.ToString();

                    //// Setup button
                    //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                    //pointableButton.SetButton();
                    //pointableButton.Button.onClick.AddListener(() => { OnInsureItemClick(currentItemIcon, itemValue, currencyItemID); });
                    //pointableButton.MaxPointingRange = 20;
                    //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                    // Add the icon object to the list for that item
                    if (CIW != null)
                    {
                        CIW.marketItemViews.Add(marketItemView);
                    }
                    else
                    {
                        VID.marketItemViews.Add(marketItemView);
                    }
                }
                // Setup insure price display
                if (trader.currency == 0)
                {
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["203"];
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["203"];
                }
                else if (trader.currency == 1)
                {
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames["201"];
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["201"];
                }
                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = totalInsurePrice.ToString();
                currentTotalInsurePrice = totalInsurePrice;
                // Setup button
                if (!initButtonsSet)
                {
                    EFM_PointableButton pointableInsureDealButton = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
                    pointableInsureDealButton.SetButton();
                    pointableInsureDealButton.Button.onClick.AddListener(() => { OnInsureDealClick(); });
                    pointableInsureDealButton.MaxPointingRange = 20;
                    pointableInsureDealButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                    // Setup hoverscrolls
                    EFM_HoverScroll newInsureDownHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                    EFM_HoverScroll newInsureUpHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
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
                EFM_HoverScroll downInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
                EFM_HoverScroll upInsureHoverScroll = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
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
            }

            initButtonsSet = true;
        }

        public void UpdateBasedOnItem(bool added, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            bool custom = CIW != null;
            string itemID = custom ? CIW.ID : VID.H3ID;
            int itemValue = custom ? CIW.GetValue() : VID.GetValue();

            if (added)
            {
                // Add to trade volume inventory
                if (custom)
                {
                    if (tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                    }
                    if (tradeVolumeInventory.ContainsKey(CIW.ID))
                    {
                        tradeVolumeInventory[CIW.ID] += CIW.maxStack > 1 ? CIW.stack : 1;
                    }
                    else
                    {
                        tradeVolumeInventory.Add(CIW.ID, CIW.maxStack > 1 ? CIW.stack : 1);
                    }
                }
                else
                {
                    if (tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                    }
                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                    }
                }

                // IN BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
                if (prices.ContainsKey(itemID))
                {
                    // Go through each price because need to check if all are fulfilled anyway
                    bool canDeal = true;
                    foreach (KeyValuePair<string, int> price in prices)
                    {
                        if (tradeVolumeInventory.ContainsKey(price.Key) && tradeVolumeInventory[price.Key] >= price.Value)
                        {
                            // If this is the item we are adding, make sure the requirement fulfilled icon is active
                            if (price.Key.Equals(itemID))
                            {
                                Transform priceElement = buyPriceElements[price.Key].transform;
                                priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                                priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                            }
                        }
                        else
                        {
                            // If this is the item we are adding, no need to make sure the unfulfilled icon is active we cause we are adding it to the inventory
                            // So for sure if not fulfilled now, the icon is already set to unfulfilled
                            canDeal = false;
                        }
                    }
                    Transform dealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0);
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
                }

                // TODO: IN SELL, check if item is already in showcase, if it is, increment count, if not, add a new entry, update price under FOR, make sure deal! button is activated if item is a sellable item
                if(Mod.traderStatuses[currentTraderIndex].ItemSellable(itemID, Mod.itemAncestors[itemID]))
                {
                    Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
                    if (sellItemShowcaseElements.ContainsKey(itemID))
                    {
                        Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
                        EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                        int actualValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? itemValue : (int)Mathf.Max(itemValue * 0.008f, 1);
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = (marketItemView.value + actualValue).ToString();
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = ((custom ? marketItemView.CIW.Count : marketItemView.VID.Count) + 1).ToString();
                        currentTotalSellingPrice += actualValue;
                    }
                    else 
                    { 
                        // Add a new sell item entry
                        Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
                        GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;

                        float sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                        Transform currentHorizontal = sellHorizontalsParent.GetChild(sellHorizontalsParent.childCount - 1);
                        if (sellHorizontalsParent.childCount == 1) // If if dont even have a single horizontal yet, add it
                        {
                            currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                        }
                        else if (currentHorizontal.childCount == 7) // If last horizontal has max number of entries already, create a new one
                        {
                            currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                            sellShowCaseHeight += 24; // horizontal
                        }

                        Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                        if (sellItemShowcaseElements == null)
                        {
                            sellItemShowcaseElements = new Dictionary<string, GameObject>();
                        }
                        sellItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                        currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                        EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                        marketItemView.custom = custom;
                        marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                        marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

                        // Write price to item icon and set correct currency icon
                        Sprite currencySprite = null;
                        string currencyItemID = "";
                        if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                        {
                            currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                            currencyItemID = "203";
                        }
                        else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
                        {
                            currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                            itemValue = (int)Mathf.Max(itemValue * 0.008f, 1); // Adjust item value
                            currencyItemID = "201";
                        }
                        marketItemView.value = itemValue;
                        currentTotalSellingPrice += itemValue;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemValue.ToString();

                        // Set count text
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "1";

                        //// Setup button
                        //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                        //pointableButton.SetButton();
                        //pointableButton.Button.onClick.AddListener(() => { OnSellItemClick(currentItemIcon, itemValue, currencyItemID); });
                        //pointableButton.MaxPointingRange = 20;
                        //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                        // Add the icon object to the list for that item
                        if (CIW != null)
                        {
                            CIW.marketItemViews.Add(marketItemView);
                        }
                        else
                        {
                            VID.marketItemViews.Add(marketItemView);
                        }

                        // Update total selling price
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames[currencyItemID];
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currencyItemID];

                        // Activate deal button
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
                    }
                    traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalSellingPrice.ToString();
                }
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
            cartItem = item.ID;
            cartItemCount = 1;
            prices = priceList;

            Transform cartShowcase = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1);
            cartShowcase.GetChild(0).GetComponent<Text>().text = Mod.itemNames[item.ID];
            cartShowcase.GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[item.ID];
            cartShowcase.GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "1";

            Transform pricesParent = cartShowcase.GetChild(4).GetChild(0).GetChild(0);
            GameObject priceTemplate = pricesParent.GetChild(0).gameObject;
            float priceHeight = 0;
            if(pricesParent.childCount > 1)
            {
                Destroy(pricesParent.GetChild(1).gameObject);
            }
            bool canDeal = true;
            foreach(KeyValuePair<string, int> price in priceList)
            {
                priceHeight += 50;
                Transform priceElement = Instantiate(priceTemplate, pricesParent).transform;
                priceElement.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[price.Key];
                priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = price.Value.ToString();
                priceElement.GetChild(3).GetChild(0).GetComponent<Text>().text = Mod.itemNames[price.Key];
                if(buyPriceElements == null)
                {
                    buyPriceElements = new Dictionary<string, GameObject>();
                }
                buyPriceElements.Add(price.Key, priceElement.gameObject);

                if (tradeVolumeInventory.ContainsKey(price.Key) && tradeVolumeInventory[price.Key] >= price.Value)
                {
                    priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                    priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                }
                else
                {
                    canDeal = false;
                }
            }
            EFM_HoverScroll downHoverScroll = cartShowcase.GetChild(3).GetChild(3).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upHoverScroll = cartShowcase.GetChild(3).GetChild(3).GetComponent<EFM_HoverScroll>();
            if (priceHeight > 60)
            {
                downHoverScroll.rate = 60 / (priceHeight - 60);
                upHoverScroll.rate = 60 / (priceHeight - 60);
                downHoverScroll.gameObject.SetActive(true);
                upHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downHoverScroll.gameObject.SetActive(false);
                upHoverScroll.gameObject.SetActive(false);
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

        //public void OnSellItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        //{
        //    // TODO: MAYBE DONT EVEN NEED THIS, WE JUST SELL EVERYTHING IN THE SELL SHOWCASE
        //    // TODO: Set cart UI to this item
        //    // de/activate deal! button depending on whether trader has enough money
        //}

        public void OnSellDealClick()
        {
            // TODO: Remove all sellable items from trade volume
            // Add FOR to trade volume
            // Clear Sell showcase completely
            // Deactivate deal button
        }

        public void OnInsureItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        {
            // TODO: Set cart UI to this item
            // de/activate deal! button depending on whether trade volume has enough money
        }

        public void OnInsureDealClick()
        {
            // TODO: Set all insureable items in trade volume as insured
            // Remove FOR from trade volume
            // Clear insure showcase completely, considering all those items are now insured
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

        public void OnRagFairCategoryMainClick(GameObject category, string ID)
        {
            // TODO: reset item list
            // Add all items of that category to the list
            // Visually activate this category
            // Visually deactivate any other previously active category
            // TODO: Also update hoverscrolls here
        }

        public void OnRagFairItemMainClick(GameObject itemUIElement, string ID)
        {
            // TODO: reset item list
            // Add all sell enrties of that item to the list
            // Visually activate this item element
            // Visually deactivate any other previously active element
            // TODO: Also update hoverscrolls here
            // update ragFairItemBuyViewsByID
        }

        public void OnRagFairCategoryToggleClick(GameObject category)
        {
            Transform toggle = category.transform.GetChild(0).GetChild(0);
            toggle.GetChild(0).gameObject.SetActive(!toggle.GetChild(0).gameObject.activeSelf);
            toggle.GetChild(1).gameObject.SetActive(!toggle.GetChild(1).gameObject.activeSelf);
            category.transform.GetChild(1).gameObject.SetActive(toggle.GetChild(1).gameObject.activeSelf);

            // TODO: Ensure hoverscrolls are properly updated (active and rate) because the height of the lsit has changed since we toggled
        }

        public int GetTotalItemSell(string ID)
        {
            // TODO: Check in all trader assorts, for each taht has an entry of this item, +1
            // TODO: Once rag fair player simulation is implemented, add up the number of player selling entries 
            return 0;
        }

        public void OnRagFairWishlistItemWishClick(string ID)
        {
            // TODO: Keeps lists of all elements for this item that is relevant to wishlist (rag fair buy and wishlist item views (deactivate star on buy, destroy teh view in wishlist), all the CIWs and VIDs of this item that are in the hideout so we can set their wishlist var to false)
            // TODO: Remove ID from Mod.wishList
        }

        public void AddItemToWishlist(string ID)
        {
            Transform wishlistParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject wishlistItemViewTemplate = wishlistParent.GetChild(0).gameObject;
            GameObject wishlistItemView = Instantiate(wishlistItemViewTemplate, wishlistParent);
            wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[ID];
            wishlistItemView.transform.GetChild(1).GetComponent<Text>().text = Mod.itemNames[ID];

            wishlistItemView.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairWishlistItemWishClick(ID); });

            wishListItemViewsByID.Add(ID, wishlistItemView);

            if (ragFairItemBuyViewsByID.ContainsKey(ID))
            {
                List<GameObject> itemViewsList = ragFairItemBuyViewsByID[ID];
                foreach(GameObject itemView in itemViewsList)
                {
                    itemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = new Color(1, 0.84706f, 0);
                }
            }
        }
    }
}
