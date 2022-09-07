using FistVR;
using System;
using System.Collections;
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
        public Dictionary<string, List<GameObject>> tradeVolumeInventoryObjects;
        public string cartItem;
        public int cartItemCount;
        public List<AssortmentPriceData> prices;
        public List<GameObject> buyPriceElements;
        public string ragfairCartItem;
        public int ragfairCartItemCount;
        public List<AssortmentPriceData> ragfairPrices;
        public List<GameObject> ragfairBuyPriceElements;
        public Dictionary<string, GameObject> sellItemShowcaseElements;
        public Dictionary<string, GameObject> insureItemShowcaseElements;
        public int currentTotalSellingPrice = 0;
        public int currentTotalInsurePrice = 0;

        private bool initButtonsSet;
        private bool choosingBuyAmount;
        private bool choosingRagfairBuyAmount;
        private bool startedChoosingThisFrame;
        private Vector3 amountChoiceStartPosition;
        private Vector3 amountChoiceRightVector;
        private int chosenAmount;
        private int maxBuyAmount;

        private GameObject currentActiveCategory;
        private GameObject currentActiveItemSelector;
        private int mustUpdateTaskListHeight;

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
                if(distanceFromCenter <= -0.19f)
                {
                    chosenAmount = 1;
                }
                else if(distanceFromCenter >= 0.19f)
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

            if (mustUpdateTaskListHeight == 0)
            {
                UpdateTaskListHeight();
                --mustUpdateTaskListHeight;
            }
            else if(mustUpdateTaskListHeight > 0)
            {
                --mustUpdateTaskListHeight;
            }

            if (EFM_TraderStatus.fenceRestockTimer > 0)
            {
                EFM_TraderStatus.fenceRestockTimer -= Time.deltaTime;

                if(EFM_TraderStatus.fenceRestockTimer <= 0 && currentTraderIndex == 2)
                {
                    SetTrader(2);
                }
            }
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
                    if (choosingBuyAmount)
                    {
                        cartItemCount = chosenAmount;
                        cartShowcase = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1);
                        countString = cartItemCount.ToString(); ;
                    }
                    else
                    {
                        ragfairCartItemCount = chosenAmount;
                        cartShowcase = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1);
                        countString = ragfairCartItemCount.ToString();
                    }
                    Mod.stackSplitUI.SetActive(false);

                    // Change amount and price on UI
                    cartShowcase.GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = countString;

                    Transform pricesParent = cartShowcase.GetChild(3).GetChild(0).GetChild(0);
                    int priceElementIndex = 1;
                    bool canDeal = true;
                    foreach (AssortmentPriceData price in prices)
                    {
                        Transform priceElement = pricesParent.GetChild(priceElementIndex++).transform;
                        priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = (price.count * cartItemCount).ToString();

                        if (tradeVolumeInventory.ContainsKey(price.ID) && tradeVolumeInventory[price.ID] >= price.count)
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

        public void Init(EFM_Base_Manager baseManager)
        {
            this.baseManager = baseManager;

            if (tradeVolumeInventory == null)
            {
                tradeVolumeInventory = new Dictionary<string, int>();
                tradeVolumeInventoryObjects = new Dictionary<string, List<GameObject>>();
            }

            // Setup the trade volume
            tradeVolume = transform.GetChild(1).gameObject.AddComponent<EFM_TradeVolume>();
            tradeVolume.itemsRoot = tradeVolume.transform.GetChild(1);
            tradeVolume.mainContainerRenderer = tradeVolume.GetComponentInChildren<Renderer>();
            tradeVolume.mainContainerRenderer.material = Mod.quickSlotConstantMaterial;
            tradeVolume.market = this;

            // Init trade volume inventory
            foreach (Transform itemTransform in tradeVolume.itemsRoot)
            {
                EFM_CustomItemWrapper CIW = itemTransform.GetComponent<EFM_CustomItemWrapper>();
                EFM_VanillaItemDescriptor VID = itemTransform.GetComponent<EFM_VanillaItemDescriptor>();
                if (CIW != null)
                {
                    if (tradeVolumeInventory.ContainsKey(CIW.ID))
                    {
                        tradeVolumeInventory[CIW.ID] += CIW.maxStack > 1 ? CIW.stack : 1;
                        tradeVolumeInventoryObjects[CIW.ID].Add(CIW.gameObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(CIW.ID, CIW.maxStack > 1 ? CIW.stack : 1);
                        tradeVolumeInventoryObjects.Add(CIW.ID, new List<GameObject>() { CIW.gameObject });
                    }
                }
                else
                {
                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                        tradeVolumeInventoryObjects[VID.H3ID].Add(VID.gameObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                        tradeVolumeInventoryObjects.Add(VID.H3ID, new List<GameObject>() { VID.gameObject });
                    }
                }
            }

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
                int traderIndex = i;
                EFM_PointableButton pointableButton = traderButtonsParent.GetChild(traderIndex).gameObject.AddComponent<EFM_PointableButton>();

                pointableButton.SetButton();
                pointableButton.Button.onClick.AddListener(() => { SetTrader(traderIndex); });
                pointableButton.MaxPointingRange = 20;
                pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            }

            // Set default trader
            SetTrader(0);
            Mod.instance.LogInfo("initUI post set trader");
            // Setup trader tabs
            Transform tabsParent = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2);
            for (int i = 0; i < 4;++i)
            {
                int tabIndex = i;
                Transform tab = tabsParent.GetChild(tabIndex);
                if (traderTabs == null)
                {
                    traderTabs = new EFM_TraderTab[4];
                }

                EFM_TraderTab tabScript = tab.gameObject.AddComponent<EFM_TraderTab>();
                traderTabs[tabIndex] = tabScript;
                tabScript.SetButton();
                tabScript.MaxPointingRange = 20;
                tabScript.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                tabScript.clickSound = clickAudio;
                tabScript.Button.onClick.AddListener(() => { tabScript.OnClick(tabIndex); });
                tabScript.hover = tab.transform.GetChild(0).GetChild(1).gameObject;
                tabScript.page = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(tabIndex).gameObject;

                tabScript.tabs = traderTabs;

                if(tabIndex == 3)
                {
                    tabScript.active = true;
                }
            }
            // Add background pointable
            FVRPointable traderBackgroundPointable = transform.GetChild(0).GetChild(1).gameObject.AddComponent<FVRPointable>();
            traderBackgroundPointable.MaxPointingRange = 30;

            // Setup rag fair
            // Buy
            Mod.instance.LogInfo("0");
            Transform categoriesParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject categoryTemplate = categoriesParent.GetChild(0).gameObject;
            EFM_PointableButton categoryTemplateMainButton = categoryTemplate.transform.GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            categoryTemplateMainButton.SetButton();
            categoryTemplateMainButton.MaxPointingRange = 20;
            categoryTemplateMainButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            EFM_PointableButton categoryTemplateToggleButton = categoryTemplate.transform.GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            categoryTemplateToggleButton.SetButton();
            categoryTemplateToggleButton.MaxPointingRange = 20;
            categoryTemplateToggleButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            Transform buyItemListTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
            Transform buyItemListParent = buyItemListTransform.GetChild(0).GetChild(0).GetChild(0);
            GameObject buyItemTemplate = buyItemListParent.GetChild(0).gameObject;
            EFM_PointableButton itemViewTemplateWishButton = buyItemTemplate.transform.GetChild(3).gameObject.AddComponent<EFM_PointableButton>();
            itemViewTemplateWishButton.SetButton();
            itemViewTemplateWishButton.MaxPointingRange = 20;
            itemViewTemplateWishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            EFM_PointableButton itemViewTemplateBuyButton = buyItemTemplate.transform.GetChild(2).gameObject.AddComponent<EFM_PointableButton>();
            itemViewTemplateBuyButton.SetButton();
            itemViewTemplateBuyButton.MaxPointingRange = 20;
            itemViewTemplateBuyButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            AddRagFairCategories(Mod.itemCategories.children, categoriesParent, categoryTemplate, 1);
            ragFairItemBuyViewsByID = new Dictionary<string, List<GameObject>>();

            // Setup buy categories hoverscrolls
            Mod.instance.LogInfo("0");
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
            Mod.instance.LogInfo("0");
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

            // Cart
            Mod.instance.LogInfo("0");
            Transform ragfairCartTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2);
            EFM_PointableButton ragfairCartAmountButton = ragfairCartTransform.transform.GetChild(1).GetChild(1).gameObject.AddComponent<EFM_PointableButton>();
            ragfairCartAmountButton.SetButton();
            ragfairCartAmountButton.MaxPointingRange = 20;
            ragfairCartAmountButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            ragfairCartAmountButton.Button.onClick.AddListener(() => { OnRagfairBuyAmountClick(); });
            EFM_PointableButton ragfairCartDealAmountButton = ragfairCartTransform.transform.GetChild(2).GetChild(0).GetChild(0).gameObject.AddComponent<EFM_PointableButton>();
            ragfairCartDealAmountButton.SetButton();
            ragfairCartDealAmountButton.MaxPointingRange = 20;
            ragfairCartDealAmountButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            EFM_PointableButton ragfairCartCancelAmountButton = ragfairCartTransform.transform.GetChild(2).GetChild(0).GetChild(1).gameObject.AddComponent<EFM_PointableButton>();
            ragfairCartCancelAmountButton.SetButton();
            ragfairCartCancelAmountButton.MaxPointingRange = 20;
            ragfairCartCancelAmountButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            ragfairCartCancelAmountButton.Button.onClick.AddListener(() => { OnRagfairBuyCancelClick(); });

            Mod.instance.LogInfo("0");
            EFM_HoverScroll newWishlistCartDownHoverScroll = ragfairCartTransform.GetChild(1).GetChild(3).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
            EFM_HoverScroll newWishlistCartUpHoverScroll = ragfairCartTransform.GetChild(1).GetChild(3).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
            newWishlistCartDownHoverScroll.MaxPointingRange = 30;
            newWishlistCartDownHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newWishlistCartDownHoverScroll.scrollbar = ragfairCartTransform.GetChild(1).GetChild(3).GetChild(1).GetComponent<Scrollbar>();
            newWishlistCartDownHoverScroll.other = newWishlistCartUpHoverScroll;
            newWishlistCartDownHoverScroll.up = false;
            newWishlistCartUpHoverScroll.MaxPointingRange = 30;
            newWishlistCartUpHoverScroll.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            newWishlistCartUpHoverScroll.scrollbar = ragfairCartTransform.GetChild(1).GetChild(3).GetChild(1).GetComponent<Scrollbar>();
            newWishlistCartUpHoverScroll.other = newWishlistCartDownHoverScroll;
            newWishlistCartUpHoverScroll.up = true;

            // Wishlist
            Mod.instance.LogInfo("0");
            Transform wishlistParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject wishlistItemViewTemplate = wishlistParent.GetChild(0).gameObject;
            EFM_PointableButton wishlistItemViewTemplateWishButton = wishlistItemViewTemplate.transform.GetChild(2).gameObject.AddComponent<EFM_PointableButton>();
            wishlistItemViewTemplateWishButton.SetButton();
            wishlistItemViewTemplateWishButton.MaxPointingRange = 20;
            wishlistItemViewTemplateWishButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
            wishListItemViewsByID = new Dictionary<string, GameObject>();
            foreach (string wishlistItemID in Mod.wishList)
            {
                GameObject wishlistItemView = Instantiate(wishlistItemViewTemplate, wishlistParent);
                wishlistItemView.SetActive(true);
                string wishlistItemName = Mod.itemNames[wishlistItemID];
                if (Mod.itemIcons.ContainsKey(wishlistItemID))
                {
                    wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[wishlistItemID];
                }
                else
                {
                    AnvilManager.Run(Mod.SetVanillaIcon(wishlistItemID, wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                }
                wishlistItemView.transform.GetChild(1).GetComponent<Text>().text = wishlistItemName;

                wishlistItemView.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairWishlistItemWishClick(wishlistItemView, wishlistItemID); });
                wishListItemViewsByID.Add(wishlistItemID, wishlistItemView);

                // Setup itemIcon
                EFM_ItemIcon currentItemIconScript = wishlistItemView.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                currentItemIconScript.isPhysical = false;
                currentItemIconScript.itemID = wishlistItemID;
                currentItemIconScript.itemName = wishlistItemName;
                currentItemIconScript.description = Mod.itemDescriptions[wishlistItemID];
                currentItemIconScript.weight = Mod.itemWeights[wishlistItemID];
                currentItemIconScript.volume = Mod.itemVolumes[wishlistItemID];
            }

            // Setup wishlist hoverscrolls
            Mod.instance.LogInfo("0");
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
                newWishlistDownHoverScroll.rate = 190 / (wishlistHeight - 190);
                newWishlistDownHoverScroll.gameObject.SetActive(true);
            }

            // Setup rag fair tabs
            Mod.instance.LogInfo("0");
            Transform ragFaireTabsParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(2);
            for (int i = 0; i < 3; ++i)
            {
                int tabIndex = i;
                Transform tab = ragFaireTabsParent.GetChild(tabIndex);
                if (ragFairTabs == null)
                {
                    ragFairTabs = new EFM_TraderTab[3];
                }

                EFM_TraderTab tabScript = tab.gameObject.AddComponent<EFM_TraderTab>();
                ragFairTabs[tabIndex] = tabScript;
                tabScript.SetButton();
                tabScript.MaxPointingRange = 20;
                tabScript.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();
                tabScript.clickSound = clickAudio;
                tabScript.Button.onClick.AddListener(() => { tabScript.OnClick(tabIndex); });
                tabScript.hover = tab.transform.GetChild(0).GetChild(1).gameObject;
                tabScript.page = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(tabIndex).gameObject;

                tabScript.tabs = ragFairTabs;

                if (tabIndex == 2)
                {
                    tabScript.active = true;
                }
            }

            Mod.instance.LogInfo("0");
            // Add background pointable
            FVRPointable ragfairBackgroundPointable = transform.GetChild(0).GetChild(2).gameObject.AddComponent<FVRPointable>();
            ragfairBackgroundPointable.MaxPointingRange = 30;
        }

        private void AddRagFairCategories(List<EFM_CategoryTreeNode> children, Transform parent, GameObject template, int level)
        {
            foreach(EFM_CategoryTreeNode child in children)
            {
                GameObject category = Instantiate(template, parent);
                category.SetActive(true);
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
                            item.SetActive(true);
                            item.transform.GetChild(0).GetComponent<HorizontalLayoutGroup>().padding = new RectOffset((level + 1) * 10, 0, 0, 0);
                            item.transform.GetChild(0).GetChild(0).gameObject.SetActive(false);

                            item.transform.GetChild(0).GetChild(2).GetComponent<Text>().text = Mod.itemNames[itemID];
                            item.transform.GetChild(0).GetChild(3).GetComponent<Text>().text = "(" + GetTotalItemSell(itemID) + ")";

                            item.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);

                            item.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => { OnRagFairItemMainClick(item, itemID); });
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
            Mod.instance.LogInfo("set trader called with index: " + index);
            currentTraderIndex = index;
            EFM_TraderStatus trader = Mod.traderStatuses[index];
            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
            EFM_TraderStatus.TraderLoyaltyDetails loyaltyDetails = trader.GetLoyaltyDetails();
            Transform tradeVolume = transform.GetChild(1);

            Mod.instance.LogInfo("0");
            // Top
            traderDisplay.GetChild(0).GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[index];
            if(loyaltyDetails.currentLevel < 4)
            {
                // Trader details
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(false);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.LoyaltyLevelToRoman(loyaltyDetails.currentLevel);

                // Current Loyalty
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(false);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.LoyaltyLevelToRoman(loyaltyDetails.currentLevel);
            }
            else
            {
                // Trader details
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(1).gameObject.SetActive(false);

                // Current Loyalty
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).gameObject.SetActive(true);
                traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(1).gameObject.SetActive(false);
            }
            traderDisplay.GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetComponent<Text>().text = string.Format("{0:0.00}", trader.standing);
            traderDisplay.GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetComponent<Image>().sprite = trader.currency == 0 ? EFM_Base_Manager.roubleCurrencySprite : EFM_Base_Manager.dollarCurrencySprite;
            // TODO: Set total amount of money the trader has, here we just disable the number for now because we dont use it
            traderDisplay.GetChild(0).GetChild(1).GetChild(2).GetChild(1).gameObject.SetActive(false);

            Mod.instance.LogInfo("0");
            // Player level
            traderDisplay.GetChild(0).GetChild(2).GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.playerLevelIcons[Mod.level / 5];
            traderDisplay.GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();

            Mod.instance.LogInfo("0");
            // Other loyalty details
            // Current loyalty
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(2).GetChild(1).GetComponent<Text>().text = Mod.level.ToString();
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetComponent<Text>().text = trader.standing.ToString();
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(0).GetChild(4).GetChild(1).GetComponent<Text>().text = EFM_TraderStatus.GetMoneyString(trader.salesSum);

            Mod.instance.LogInfo("0");
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

            Mod.instance.LogInfo("0");
            // Player money
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetComponent<Text>().text = FormatCompleteMoneyString((Mod.baseInventory.ContainsKey("203") ? Mod.baseInventory["203"] : 0) + (Mod.playerInventory.ContainsKey("203") ? Mod.playerInventory["203"] : 0));
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetComponent<Text>().text = FormatCompleteMoneyString((Mod.baseInventory.ContainsKey("202") ? Mod.baseInventory["202"] : 0) + (Mod.playerInventory.ContainsKey("202") ? Mod.playerInventory["202"] : 0));
            traderDisplay.GetChild(0).GetChild(2).GetChild(1).GetChild(2).GetChild(2).GetChild(1).GetComponent<Text>().text = FormatCompleteMoneyString((Mod.baseInventory.ContainsKey("201") ? Mod.baseInventory["201"] : 0) + (Mod.playerInventory.ContainsKey("201") ? Mod.playerInventory["201"] : 0));

            Mod.instance.LogInfo("0");
            // Main
            // Buy
            bool setDefaultBuy = false;
            Transform buyHorizontalsParent = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject buyHorizontalCopy = buyHorizontalsParent.GetChild(0).gameObject;
            float buyShowCaseHeight = 27; // Top padding + horizontal
            // Clear previous horizontals
            Mod.instance.LogInfo("0");
            while (buyHorizontalsParent.childCount > 1)
            {
                // Unparent so it is not a child anymore so the while loop isnt infinite because this will only actually be destroyed next frame
                Transform currentFirstChild = buyHorizontalsParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.instance.LogInfo("0");
            if (trader.standing >= 0)
            {
                // init price hoverscrolls first because they are needed by default prices
                if (!initButtonsSet)
                {
                    // Set price hover scrolls
                    EFM_HoverScroll newPriceDownHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(3).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
                    EFM_HoverScroll newPriceUpHoverScroll = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(3).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
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

                if (index == 2)
                {
                    UnityEngine.Random.InitState(Convert.ToInt32((DateTime.UtcNow - DateTime.Today).TotalHours));
                }
                // Add all assort items to showcase
                for (int i = 1; i <= loyaltyDetails.currentLevel; ++i)
                {
                    TraderAssortment assort = trader.assortmentByLevel[i];

                    AssortmentItem lastAssortItem = null;
                    List<AssortmentPriceData> lastPriceList = null;
                    Sprite lastSprite = null;
                    foreach (KeyValuePair<string, AssortmentItem> item in assort.itemsByID)
                    {
                        // Have 30% chance of adding any item if the trader is fence
                        if(index == 2 && UnityEngine.Random.value > 0.3f)
                        {
                            continue;
                        }

                        // Skip if this item must be unlocked
                        if (trader.itemsToWaitForUnlock != null && trader.itemsToWaitForUnlock.Contains(item.Key))
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
                        foreach (List<AssortmentPriceData> priceList in item.Value.prices)
                        {
                            Transform currentHorizontal = buyHorizontalsParent.GetChild(buyHorizontalsParent.childCount - 1);
                            if (buyHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
                            {
                                currentHorizontal = GameObject.Instantiate(buyHorizontalCopy, buyHorizontalsParent).transform;
                                currentHorizontal.gameObject.SetActive(true);
                            }
                            else if (currentHorizontal.childCount == 7)
                            {
                                currentHorizontal = GameObject.Instantiate(buyHorizontalCopy, buyHorizontalsParent).transform;
                                currentHorizontal.gameObject.SetActive(true);
                                buyShowCaseHeight += 24; // horizontal
                            }

                            Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;
                            currentItemIcon.gameObject.SetActive(true);

                            // Setup ItemIcon
                            Mod.instance.LogInfo("Adding item assort " + item.Key);
                            EFM_ItemIcon itemIconScript = currentItemIcon.gameObject.AddComponent<EFM_ItemIcon>();
                            itemIconScript.itemID = item.Key;
                            itemIconScript.itemName = Mod.itemNames[item.Key];
                            itemIconScript.description = Mod.itemDescriptions[item.Key];
                            itemIconScript.weight = Mod.itemWeights[item.Key];
                            itemIconScript.volume = Mod.itemVolumes[item.Key];

                            if (Mod.itemIcons.ContainsKey(item.Key))
                            {
                                currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[item.Key];
                            }
                            else
                            {
                                AnvilManager.Run(Mod.SetVanillaIcon(item.Key, currentItemIcon.GetChild(2).GetComponent<Image>()));
                            }
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
                            foreach (AssortmentPriceData currentPrice in priceList)
                            {
                                totalPriceCount += currentPrice.count;
                                if (!barterSprite)
                                {
                                    if (currentPrice.ID.Equals("201"))
                                    {
                                        currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                                    }
                                    else if (currentPrice.ID.Equals("202"))
                                    {
                                        currencySprite = EFM_Base_Manager.euroCurrencySprite;
                                    }
                                    else if (currentPrice.ID.Equals("203"))
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
                            totalPriceCount -= (int)(totalPriceCount * (Mod.skills[10].currentProgress / 10000) / 2); // -25% at elite
                            currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                            currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = totalPriceCount.ToString();

                            // Setup button
                            EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                            pointableButton.SetButton();
                            pointableButton.Button.onClick.AddListener(() => { OnBuyItemClick(item.Value, priceList, currentItemIcon.GetChild(2).GetComponent<Image>().sprite); });
                            pointableButton.MaxPointingRange = 20;
                            pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                            // Add the icon object to the list for that item
                            item.Value.currentShowcaseElements.Add(currentItemIcon.gameObject);

                            if (!setDefaultBuy)
                            {
                                if(defaultItemID == null || defaultItemID.Equals(item.Value.ID))
                                {
                                    OnBuyItemClick(item.Value, priceList, currentItemIcon.GetChild(2).GetComponent<Image>().sprite);
                                    setDefaultBuy = true;
                                }
                                else
                                {
                                    lastAssortItem = item.Value;
                                    lastPriceList = priceList;
                                    lastSprite = currentItemIcon.GetChild(2).GetComponent<Image>().sprite;
                                }
                            }
                        }
                    }

                    // This can happen if default item is specified but the item is not in the assort
                    if (!setDefaultBuy)
                    {
                        OnBuyItemClick(lastAssortItem, lastPriceList, lastSprite);
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
                    EFM_PointableButton pointableBuyAmountButton = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(1).gameObject.AddComponent<EFM_PointableButton>();
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
            // De/Activate buy deal button as necessary
            bool canDeal = trader.standing >= 0;
            for (int i = 0; i < prices.Count; ++i)
            {
                AssortmentPriceData priceData = prices[i];
                if (tradeVolumeInventory.ContainsKey(priceData.ID))
                {
                    // Find how many we have in trade inventory
                    // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
                    int matchingCountInInventory = 0;
                    if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                    {
                        foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
                        {
                            EFM_CustomItemWrapper priceCIW = priceObject.GetComponent<EFM_CustomItemWrapper>();
                            if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
                            {
                                ++matchingCountInInventory;
                            }
                        }
                    }
                    else
                    {
                        matchingCountInInventory = priceData.count;
                    }

                    // If this is the item we are adding, make sure the requirement fulfilled icon is active
                    if (matchingCountInInventory >= (priceData.count * cartItemCount))
                    {
                        Transform priceElement = buyPriceElements[i].transform;
                        priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                        priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                    }
                    else
                    {
                        canDeal = false;
                        break;
                    }
                }
                else
                {
                    canDeal = false;
                    break;
                }
            }
            Transform dealButton = traderDisplay.GetChild(1).GetChild(3).GetChild(1).GetChild(2).GetChild(0).GetChild(0);
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

            Mod.instance.LogInfo("0");
            // Sell
            Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;
            float sellShowCaseHeight = 3; // Top padding
            // Clear previous horizontals
            Mod.instance.LogInfo("0");
            while (sellHorizontalsParent.childCount > 1)
            {
                Transform currentFirstChild = sellHorizontalsParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.instance.LogInfo("Adding all item involume to sell showcase");
            // Add all items in trade volume that are sellable at this trader to showcase
            int totalSellingPrice = 0;
            if (sellItemShowcaseElements == null)
            {
                sellItemShowcaseElements = new Dictionary<string, GameObject>();
            }
            else
            {
                sellItemShowcaseElements.Clear();
            }
            // Deactivate deal button by default
            traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
            traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
            foreach (Transform itemTransform in this.tradeVolume.itemsRoot)
            {
                Mod.instance.LogInfo("\tAdding item from volume: "+itemTransform.name);
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

                if (sellItemShowcaseElements != null && sellItemShowcaseElements.ContainsKey(itemID))
                {
                    Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
                    EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                    int actualValue;
                    if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
                    {
                        actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
                    }
                    else
                    {
                        // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
                        actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
                    }
                    actualValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? actualValue : (int)Mathf.Max(actualValue * 0.008f, 1);
                    marketItemView.value = marketItemView.value + actualValue;
                    if (marketItemView.custom)
                    {
                        marketItemView.CIW.Add(CIW);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                    }
                    else
                    {
                        marketItemView.VID.Add(VID);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                    }
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.value.ToString();
                    currentTotalSellingPrice += actualValue;

                    // Setup itemIcon
                    EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                    currentItemIconScript.isPhysical = false;
                    currentItemIconScript.itemID = itemID;
                    currentItemIconScript.itemName = Mod.itemNames[itemID];
                    currentItemIconScript.description = Mod.itemDescriptions[itemID];
                    currentItemIconScript.weight = Mod.itemWeights[itemID];
                    currentItemIconScript.volume = Mod.itemVolumes[itemID];
                }
                else
                {
                    sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                    Transform currentHorizontal = sellHorizontalsParent.GetChild(sellHorizontalsParent.childCount - 1);
                    if (sellHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
                    {
                        currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                        currentHorizontal.gameObject.SetActive(true);
                    }
                    else if (currentHorizontal.childCount == 7)
                    {
                        currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                        currentHorizontal.gameObject.SetActive(true);
                        buyShowCaseHeight += 24; // horizontal
                    }

                    Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

                    // Setup itemIcon
                    EFM_ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<EFM_ItemIcon>();
                    currentItemIconScript.isPhysical = true;
                    currentItemIconScript.isCustom = custom;
                    currentItemIconScript.CIW = CIW;
                    currentItemIconScript.VID = VID;

                    currentItemIcon.gameObject.SetActive(true);
                    sellItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                    if (Mod.itemIcons.ContainsKey(itemID))
                    {
                        currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                    }
                    else
                    {
                        AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
                    }
                    EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                    marketItemView.custom = custom;
                    marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                    marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

                    int actualValue;
                    if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
                    {
                        actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
                    }
                    else
                    {
                        // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
                        actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
                    }

                    // Write price to item icon and set correct currency icon
                    Sprite currencySprite = null;
                    //string currencyItemID = "";
                    if (trader.currency == 0)
                    {
                        currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                        //currencyItemID = "203";
                    }
                    else if (trader.currency == 1)
                    {
                        currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                        actualValue = (int)Mathf.Max(actualValue * 0.008f, 1); // Adjust item value
                        //currencyItemID = "201";
                    }
                    marketItemView.value = actualValue;
                    totalSellingPrice += actualValue;
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = actualValue.ToString();

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
                }

                // Activate deal button
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;
            }
            Mod.instance.LogInfo("0");
            // Setup selling price display
            string sellPriceItemID = "203";
            string sellPriceItemName = "Rouble";
            if (trader.currency == 0)
            {
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = sellPriceItemName;
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["203"];
            }
            else if (trader.currency == 1)
            {
                sellPriceItemID = "201";
                sellPriceItemName = Mod.itemNames["201"];
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = sellPriceItemName;
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons["201"];
            }
            EFM_ItemIcon traderSellPriceItemIcon = traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<EFM_ItemIcon>();
            if (traderSellPriceItemIcon == null)
            {
                traderSellPriceItemIcon = traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                traderSellPriceItemIcon.itemID = sellPriceItemID;
                traderSellPriceItemIcon.itemName = sellPriceItemName;
                traderSellPriceItemIcon.description = Mod.itemDescriptions[sellPriceItemID];
                traderSellPriceItemIcon.weight = Mod.itemWeights[sellPriceItemID];
                traderSellPriceItemIcon.volume = Mod.itemVolumes[sellPriceItemID];
            }
            Mod.instance.LogInfo("0");
            traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = totalSellingPrice.ToString();
            currentTotalSellingPrice = totalSellingPrice;
            Mod.instance.LogInfo("0");
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
            Mod.instance.LogInfo("0");

            Mod.instance.LogInfo("0");
            // Tasks
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            float taskListHeight = 3; // Top padding
            // Clear previous tasks
            Mod.instance.LogInfo("0");
            while (tasksParent.childCount > 1)
            {
                Transform currentFirstChild = tasksParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.instance.LogInfo("0");
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
            Mod.instance.LogInfo("0");
            // Add all of that trader's available and active tasks to the list
            foreach (TraderTask task in trader.tasks)
            {
                Mod.instance.LogInfo("Check if can add task "+task.name+" to task list, its state is: "+task.taskState);
                if(task.taskState == TraderTask.TaskState.Available)
                {
                    GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
                    currentTaskElement.SetActive(true);
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
                    foreach(TraderTaskCondition currentCondition in task.completionConditions)
                    {
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
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
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    if (reward.amount > 1)
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                    }
                                    else
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    }
                                    currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                                    // Setup ItemIcon
                                    EFM_ItemIcon itemIconScript = currentInitEquipItemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                                    itemIconScript.itemID = reward.itemID;
                                    itemIconScript.itemName = Mod.itemNames[reward.itemID];
                                    itemIconScript.description = Mod.itemDescriptions[reward.itemID];
                                    itemIconScript.weight = Mod.itemWeights[reward.itemID];
                                    itemIconScript.volume = Mod.itemVolumes[reward.itemID];
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
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                                    // Setup ItemIcon
                                    EFM_ItemIcon assortIconScript = currentInitEquipAssortElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                                    assortIconScript.itemID = reward.itemID;
                                    assortIconScript.itemName = Mod.itemNames[reward.itemID];
                                    assortIconScript.description = Mod.itemDescriptions[reward.itemID];
                                    assortIconScript.weight = Mod.itemWeights[reward.itemID];
                                    assortIconScript.volume = Mod.itemVolumes[reward.itemID];
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
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                if (reward.amount > 1)
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                                }
                                else
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                }
                                currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                                // Setup ItemIcon
                                EFM_ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                                itemIconScript.itemID = reward.itemID;
                                itemIconScript.itemName = Mod.itemNames[reward.itemID];
                                itemIconScript.description = Mod.itemDescriptions[reward.itemID];
                                itemIconScript.weight = Mod.itemWeights[reward.itemID];
                                itemIconScript.volume = Mod.itemVolumes[reward.itemID];
                                break;
                            case TraderTaskReward.TaskRewardType.TraderUnlock:
                                GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.experience > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
                                currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                                // Setup ItemIcon
                                EFM_ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                                assortIconScript.itemID = reward.itemID;
                                assortIconScript.itemName = Mod.itemNames[reward.itemID];
                                assortIconScript.description = Mod.itemDescriptions[reward.itemID];
                                assortIconScript.weight = Mod.itemWeights[reward.itemID];
                                assortIconScript.volume = Mod.itemVolumes[reward.itemID];
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
                    currentTaskElement.SetActive(true);
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
                    foreach (TraderTaskCondition currentCondition in task.completionConditions)
                    {
                        if (currentCondition.fulfilled)
                        {
                            ++completedCount;
                        }
                        ++totalCount;
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
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
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                        foreach (TraderTaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                currentInitEquipHorizontal.gameObject.SetActive(true);
                            }
                            switch (reward.taskRewardType)
                            {
                                case TraderTaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
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
                                    currentInitEquipTraderUnlockElement.SetActive(true);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TraderTaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.SetActive(true);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                    GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipAssortElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
                                    currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                                    currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];
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
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
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
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
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
                    currentTaskElement.SetActive(true);
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
                    foreach (TraderTaskCondition currentCondition in task.completionConditions)
                    {
                        GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                        currentObjectiveElement.SetActive(true);
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
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                        foreach (TraderTaskReward reward in task.startingEquipment)
                        {
                            // Add new horizontal if necessary
                            if (currentInitEquipHorizontal.childCount == 6)
                            {
                                currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                                currentInitEquipHorizontal.gameObject.SetActive(true);
                            }
                            switch (reward.taskRewardType)
                            {
                                case TraderTaskReward.TaskRewardType.Item:
                                    GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipItemElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
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
                                    currentInitEquipTraderUnlockElement.SetActive(true);
                                    currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                    currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                    break;
                                case TraderTaskReward.TaskRewardType.TraderStanding:
                                    GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipStandingElement.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                    currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                    currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                    currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                    break;
                                case TraderTaskReward.TaskRewardType.Experience:
                                    GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipExperienceElement.SetActive(true);
                                    currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                    currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                    break;
                                case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                    GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                                    currentInitEquipAssortElement.SetActive(true);
                                    if (Mod.itemIcons.ContainsKey(reward.itemID))
                                    {
                                        currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                    }
                                    else
                                    {
                                        AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                    }
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
                    currentRewardHorizontal.gameObject.SetActive(true);
                    foreach (TraderTaskReward reward in task.successRewards)
                    {
                        // Add new horizontal if necessary
                        if (currentRewardHorizontal.childCount == 6)
                        {
                            currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                            currentRewardHorizontal.gameObject.SetActive(true);
                        }
                        switch (reward.taskRewardType)
                        {
                            case TraderTaskReward.TaskRewardType.Item:
                                GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardItemElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
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
                                currentRewardTraderUnlockElement.SetActive(true);
                                currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                                currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                                break;
                            case TraderTaskReward.TaskRewardType.TraderStanding:
                                GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardStandingElement.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                                currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                                currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                                currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                                break;
                            case TraderTaskReward.TaskRewardType.Experience:
                                GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                                currentRewardExperienceElement.SetActive(true);
                                currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                                currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                                break;
                            case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                                GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                                currentRewardAssortElement.SetActive(true);
                                if (Mod.itemIcons.ContainsKey(reward.itemID))
                                {
                                    currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                                }
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
                else
                {
                    task.marketListElement = null;
                }
            }
            Mod.instance.LogInfo("0");
            // Setup hoverscrolls
            if (!initButtonsSet)
            {
                EFM_HoverScroll newTaskDownHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).gameObject.AddComponent<EFM_HoverScroll>();
                EFM_HoverScroll newTaskUpHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).gameObject.AddComponent<EFM_HoverScroll>();
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
            EFM_HoverScroll downTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<EFM_HoverScroll>();
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
            Mod.instance.LogInfo("0");

            // Insure
            Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject insureHorizontalCopy = insureHorizontalsParent.GetChild(0).gameObject;
            float insureShowCaseHeight = 3; // Top padding
            Mod.instance.LogInfo("0");
            // Clear previous horizontals
            while (insureHorizontalsParent.childCount > 1)
            {
                Transform currentFirstChild = insureHorizontalsParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            Mod.instance.LogInfo("0");
            // Deactivate deal button by default
            traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
            traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.gray;
            if ((bool)Mod.traderBaseDB[trader.index]["insurance"]["availability"])
            {
                Mod.instance.LogInfo("insurance available");
                // Add all items in trade volume that are insureable at this trader to showcase
                int totalInsurePrice = 0;
                if(insureItemShowcaseElements == null)
                {
                    insureItemShowcaseElements = new Dictionary<string, GameObject>();
                }
                else
                {
                    insureItemShowcaseElements.Clear();
                }
                foreach (Transform itemTransform in this.tradeVolume.itemsRoot)
                {
                    Mod.instance.LogInfo("processing item "+itemTransform.name);
                    EFM_CustomItemWrapper CIW = itemTransform.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor VID = itemTransform.GetComponent<EFM_VanillaItemDescriptor>();
                    List<EFM_MarketItemView> itemViewListToUse = null;
                    string itemID;
                    int itemInsureValue;
                    bool custom = false;
                    if (CIW != null)
                    {
                        itemViewListToUse = CIW.marketItemViews;

                        itemID = CIW.ID;
                        custom = true;

                        itemInsureValue = (int)Mathf.Max(CIW.GetInsuranceValue() * trader.insuranceRate, 1);

                        if (CIW.insured || !trader.ItemInsureable(itemID, CIW.parents))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        itemViewListToUse = VID.marketItemViews;

                        itemID = VID.H3ID;

                        itemInsureValue = (int)Mathf.Max(VID.GetInsuranceValue() * trader.insuranceRate, 1);

                        if (VID.insured || !trader.ItemInsureable(itemID, VID.parents))
                        {
                            continue;
                        }
                    }


                    Mod.instance.LogInfo("1");
                    if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
                    {
                        Mod.instance.LogInfo("2");
                        Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
                        Mod.instance.LogInfo("2");
                        EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                        Mod.instance.LogInfo("2");
                        int actualInsureValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? itemInsureValue : (int)Mathf.Max(itemInsureValue * 0.008f, 1);
                        Mod.instance.LogInfo("2");
                        marketItemView.insureValue = marketItemView.insureValue + actualInsureValue;
                        Mod.instance.LogInfo("2");
                        if (marketItemView.custom)
                        {
                            Mod.instance.LogInfo("3");
                            marketItemView.CIW.Add(CIW);
                            Mod.instance.LogInfo("3");
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                            Mod.instance.LogInfo("3");
                        }
                        else
                        {
                            Mod.instance.LogInfo("4");
                            marketItemView.VID.Add(VID);
                            Mod.instance.LogInfo("4");
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                            Mod.instance.LogInfo("4");
                        }
                        Mod.instance.LogInfo("2");
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
                        Mod.instance.LogInfo("2");
                        currentTotalInsurePrice += actualInsureValue;

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = false;
                        currentItemIconScript.itemID = itemID;
                        currentItemIconScript.itemName = Mod.itemNames[itemID];
                        currentItemIconScript.description = Mod.itemDescriptions[itemID];
                        currentItemIconScript.weight = Mod.itemWeights[itemID];
                        currentItemIconScript.volume = Mod.itemVolumes[itemID];
                    }
                    else
                    {
                        Mod.instance.LogInfo("5");
                        Transform currentHorizontal = insureHorizontalsParent.GetChild(insureHorizontalsParent.childCount - 1);
                        Mod.instance.LogInfo("5");
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
                        Mod.instance.LogInfo("5");

                        Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.CIW = CIW;
                        currentItemIconScript.VID = VID;

                        currentItemIcon.gameObject.SetActive(true);
                        Mod.instance.LogInfo("5");
                        insureItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                        if (Mod.itemIcons.ContainsKey(itemID))
                        {
                            currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
                        }
                        EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                        marketItemView.custom = custom;
                        marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                        marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

                        Mod.instance.LogInfo("5");
                        // Write price to item icon and set correct currency icon
                        Sprite currencySprite = null;
                        if (trader.currency == 0)
                        {
                            currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                        }
                        else if (trader.currency == 1)
                        {
                            currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                            itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
                        }
                        Mod.instance.LogInfo("5");
                        marketItemView.insureValue = itemInsureValue;
                        totalInsurePrice += itemInsureValue;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemInsureValue.ToString();

                        Mod.instance.LogInfo("5");
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
                        Mod.instance.LogInfo("5");
                    }
                }
                string currencyItemID = "";
                if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                {
                    currencyItemID = "203";
                }
                else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
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
                Mod.instance.LogInfo("1");
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
                EFM_ItemIcon traderInsurePriceItemIcon = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<EFM_ItemIcon>();
                if (traderInsurePriceItemIcon == null)
                {
                    traderInsurePriceItemIcon = traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                    traderInsurePriceItemIcon.itemID = insurePriceItemID;
                    traderInsurePriceItemIcon.itemName = insurePriceItemName;
                    traderInsurePriceItemIcon.description = Mod.itemDescriptions[insurePriceItemID];
                    traderInsurePriceItemIcon.weight = Mod.itemWeights[insurePriceItemID];
                    traderInsurePriceItemIcon.volume = Mod.itemVolumes[insurePriceItemID];
                }
                Mod.instance.LogInfo("1");
                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = totalInsurePrice.ToString();
                currentTotalInsurePrice = totalInsurePrice;
                Mod.instance.LogInfo("1");
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
                Mod.instance.LogInfo("1");
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
                Mod.instance.LogInfo("1");
            }
            else
            {
                insureItemShowcaseElements = null;
            }

            Mod.instance.LogInfo("0");
            initButtonsSet = true;
        }

        public static string FormatCompleteMoneyString(int amount)
        {
            string s = amount.ToString();
            int charCount = 0;
            for(int i = s.Length-1; i >= 0; --i)
            {
                if(charCount != 0 && charCount % 3 == 0)
                {
                    s = s.Insert(i + 1, " ");
                }
                ++charCount;
            }
            return s;
        }

        public void UpdateTaskListHeight()
        {
            Mod.instance.LogInfo("UpdateTaskListHeight called");
            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
            Mod.instance.LogInfo("got trader display");
            EFM_HoverScroll downTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upTaskHoverScroll = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(3).GetComponent<EFM_HoverScroll>();
            Mod.instance.LogInfo("got hoverscrolls");
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            Mod.instance.LogInfo("got task parents");
            float taskListHeight = 3 + 29 * (tasksParent.childCount - 1);

            // Also need to add height of open descriptions
            for(int i = 1; i<tasksParent.childCount; ++i)
            {
                Mod.instance.LogInfo("\tGetting task " + i);
                GameObject description = tasksParent.GetChild(i).GetChild(1).gameObject;
                Mod.instance.LogInfo("\tgot description");
                if (description.activeSelf) 
                {
                    taskListHeight += description.GetComponent<RectTransform>().sizeDelta.y;
                }
            }
            Mod.instance.LogInfo("done adding desc heights, total: "+ taskListHeight);

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
        }

        public void UpdateBasedOnItem(bool added, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            bool custom = CIW != null;
            string itemID = custom ? CIW.ID : VID.H3ID;
            int itemValue = custom ? CIW.GetValue() : VID.GetValue();
            int itemInsureValue = custom ? (int)Mathf.Max(CIW.GetInsuranceValue() * Mod.traderStatuses[currentTraderIndex].insuranceRate, 1) : (int)Mathf.Max(VID.GetInsuranceValue() * Mod.traderStatuses[currentTraderIndex].insuranceRate, 1);

            Mod.instance.LogInfo("UpdateBasedOnItem called");
            if (added)
            {
                Mod.instance.LogInfo("Added");
                // Add to trade volume inventory
                if (custom)
                {
                    Mod.instance.LogInfo("custom");
                    if (tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                        tradeVolumeInventoryObjects = new Dictionary<string, List<GameObject>>();
                    }
                    if (tradeVolumeInventory.ContainsKey(CIW.ID))
                    {
                        tradeVolumeInventory[CIW.ID] += CIW.maxStack > 1 ? CIW.stack : 1;
                        tradeVolumeInventoryObjects[CIW.ID].Add(CIW.gameObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(CIW.ID, CIW.maxStack > 1 ? CIW.stack : 1);
                        tradeVolumeInventoryObjects.Add(CIW.ID, new List<GameObject>() { CIW.gameObject });
                    }
                }
                else
                {
                    Mod.instance.LogInfo("vanilla");
                    if (tradeVolumeInventory == null)
                    {
                        tradeVolumeInventory = new Dictionary<string, int>();
                        tradeVolumeInventoryObjects = new Dictionary<string, List<GameObject>>();
                    }
                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                        tradeVolumeInventoryObjects[VID.H3ID].Add(VID.gameObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                        tradeVolumeInventoryObjects.Add(VID.H3ID, new List<GameObject>() { VID.gameObject });
                    }
                }

                // IN BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
                Mod.instance.LogInfo("trader buy");
                if (prices != null)
                {
                    bool foundID = false;
                    AssortmentPriceData foundPriceData = null;
                    foreach (AssortmentPriceData otherAssortPriceData in prices)
                    {
                        if (otherAssortPriceData.ID.Equals(itemID))
                        {
                            foundPriceData = otherAssortPriceData;
                            foundID = true;
                            break;
                        }
                    }
                    bool matchesType = false;
                    if(foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                    {
                        matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
                    }
                    else
                    {
                        matchesType = true;
                    }
                    if (foundID && matchesType)
                    {

                        Mod.instance.LogInfo("Updating prices");
                        // Go through each price because need to check if all are fulfilled anyway
                        bool canDeal = true;
                        for(int i=0; i<prices.Count;++i)
                        {
                            AssortmentPriceData priceData = prices[i];
                            if (tradeVolumeInventory.ContainsKey(priceData.ID))
                            {
                                // Find how many we have in trade inventory
                                // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
                                int matchingCountInInventory = 0;
                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                {
                                    foreach(GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
                                    {
                                        EFM_CustomItemWrapper priceCIW = priceObject.GetComponent<EFM_CustomItemWrapper>();
                                        if(priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
                                        {
                                            ++matchingCountInInventory;
                                        }
                                    }
                                }
                                else
                                {
                                    matchingCountInInventory = priceData.count;
                                }

                                // If this is the item we are adding, make sure the requirement fulfilled icon is active
                                if (matchingCountInInventory >= (priceData.count * cartItemCount))
                                {
                                    if (priceData.ID.Equals(itemID))
                                    {
                                        Transform priceElement = buyPriceElements[i].transform;
                                        priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                                        priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                                    }
                                }
                                else
                                {
                                    canDeal = false;
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
                }

                Mod.instance.LogInfo("rag buy");
                // IN RAGFAIR BUY, check if item corresponds to price, update fulfilled icons and activate deal! button if necessary
                if (ragfairPrices != null)
                {
                    bool foundID = false;
                    AssortmentPriceData foundPriceData = null;
                    foreach (AssortmentPriceData otherAssortPriceData in ragfairPrices)
                    {
                        if (otherAssortPriceData.ID.Equals(itemID))
                        {
                            foundPriceData = otherAssortPriceData;
                            foundID = true;
                            break;
                        }
                    }
                    bool matchesType = false;
                    if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                    {
                        matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
                    }
                    else
                    {
                        matchesType = true;
                    }
                    if (foundID && matchesType)
                    {

                        Mod.instance.LogInfo("updating rag buy prices");
                        // Go through each price because need to check if all are fulfilled anyway
                        bool canDeal = true;
                        for (int i = 0; i < ragfairPrices.Count; ++i)
                        {
                            AssortmentPriceData priceData = ragfairPrices[i];
                            if (tradeVolumeInventory.ContainsKey(priceData.ID))
                            {
                                // Find how many we have in trade inventory
                                // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
                                int matchingCountInInventory = 0;
                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                {
                                    foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
                                    {
                                        EFM_CustomItemWrapper priceCIW = priceObject.GetComponent<EFM_CustomItemWrapper>();
                                        if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
                                        {
                                            ++matchingCountInInventory;
                                        }
                                    }
                                }
                                else
                                {
                                    matchingCountInInventory = priceData.count;
                                }

                                // If this is the item we are adding, make sure the requirement fulfilled icon is active
                                if (matchingCountInInventory >= (priceData.count * ragfairCartItemCount))
                                {
                                    if (priceData.ID.Equals(itemID))
                                    {
                                        Transform priceElement = ragfairBuyPriceElements[i].transform;
                                        priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                                        priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                                    }
                                }
                                else
                                {
                                    canDeal = false;
                                }
                            }
                            else
                            {
                                // If this is the item we are adding, no need to make sure the unfulfilled icon is active we cause we are adding it to the inventory
                                // So for sure if not fulfilled now, the icon is already set to unfulfilled
                                canDeal = false;
                            }
                        }
                        Transform dealButton = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(2).GetChild(0).GetChild(0);
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
                }

                Mod.instance.LogInfo("trader sell");
                // IN SELL, check if item is already in showcase, if it is, increment count, if not, add a new entry, update price under FOR, make sure deal! button is activated if item is a sellable item
                if (Mod.traderStatuses[currentTraderIndex].ItemSellable(itemID, Mod.itemAncestors[itemID]))
                {
                    Mod.instance.LogInfo("updating sell showcase");
                    Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
                    if (sellItemShowcaseElements != null && sellItemShowcaseElements.ContainsKey(itemID))
                    {
                        Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
                        EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                        int actualValue;
                        if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
                        {
                            actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
                        }
                        else
                        {
                            // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
                            actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
                        }
                        actualValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? actualValue : (int)Mathf.Max(actualValue * 0.008f, 1);
                        marketItemView.value = marketItemView.value + actualValue;
                        if (marketItemView.custom)
                        {
                            marketItemView.CIW.Add(CIW);
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                        }
                        else
                        {
                            marketItemView.VID.Add(VID);
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                        }
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.value.ToString();
                        currentTotalSellingPrice += actualValue;

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = false;
                        currentItemIconScript.itemID = itemID;
                        currentItemIconScript.itemName = Mod.itemNames[itemID];
                        currentItemIconScript.description = Mod.itemDescriptions[itemID];
                        currentItemIconScript.weight = Mod.itemWeights[itemID];
                        currentItemIconScript.volume = Mod.itemVolumes[itemID];
                    }
                    else
                    {
                        Mod.instance.LogInfo("adding new sell entry");
                        // Add a new sell item entry
                        Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
                        GameObject sellHorizontalCopy = sellHorizontalsParent.GetChild(0).gameObject;

                        Mod.instance.LogInfo("0");
                        float sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                        Transform currentHorizontal = sellHorizontalsParent.GetChild(sellHorizontalsParent.childCount - 1);
                        if (sellHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
                        {
                            currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                        }
                        else if (currentHorizontal.childCount == 7) // If last horizontal has max number of entries already, create a new one
                        {
                            currentHorizontal = GameObject.Instantiate(sellHorizontalCopy, sellHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                            sellShowCaseHeight += 24; // horizontal
                        }
                        Mod.instance.LogInfo("0");

                        Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.CIW = CIW;
                        currentItemIconScript.VID = VID;

                        currentItemIcon.gameObject.SetActive(true);
                        Mod.instance.LogInfo("0");
                        sellItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                        if (Mod.itemIcons.ContainsKey(itemID))
                        {
                            currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
                        }
                        Mod.instance.LogInfo("0");
                        EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                        marketItemView.custom = custom;
                        marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                        marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

                        Mod.instance.LogInfo("0");
                        // Write price to item icon and set correct currency icon
                        Sprite currencySprite = null;
                        string currencyItemID = "";
                        int actualValue;
                        if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
                        {
                            actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
                        }
                        else
                        {
                            // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
                            actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
                        }

                        if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                        {
                            currencySprite = EFM_Base_Manager.roubleCurrencySprite;
                            currencyItemID = "203";
                        }
                        else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
                        {
                            currencySprite = EFM_Base_Manager.dollarCurrencySprite;
                            actualValue = (int)Mathf.Max(actualValue * 0.008f, 1); // Adjust item value
                            currencyItemID = "201";
                        }
                        Mod.instance.LogInfo("0");
                        marketItemView.value = actualValue;
                        currentTotalSellingPrice += actualValue;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = actualValue.ToString();

                        Mod.instance.LogInfo("0");
                        // Set count text
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "1";

                        //// Setup button
                        //EFM_PointableButton pointableButton = currentItemIcon.gameObject.AddComponent<EFM_PointableButton>();
                        //pointableButton.SetButton();
                        //pointableButton.Button.onClick.AddListener(() => { OnSellItemClick(currentItemIcon, itemValue, currencyItemID); });
                        //pointableButton.MaxPointingRange = 20;
                        //pointableButton.hoverSound = transform.GetChild(2).GetComponent<AudioSource>();

                        Mod.instance.LogInfo("0");
                        // Add the icon object to the list for that item
                        if (CIW != null)
                        {
                            CIW.marketItemViews.Add(marketItemView);
                        }
                        else
                        {
                            VID.marketItemViews.Add(marketItemView);
                        }

                        Mod.instance.LogInfo("0");
                        // Update total selling price
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames[currencyItemID];
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currencyItemID];

                        // Activate deal button
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = true;
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = Color.white;

                        Mod.instance.LogInfo("0");
                        // Update hoverscrolls
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
                        Mod.instance.LogInfo("0");
                    }
                    traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalSellingPrice.ToString();
                }

                Mod.instance.LogInfo("trader tasks");
                // IN TASKS, for each item requirement of each task, activate TURN IN buttons accordingly
                if (EFM_TraderStatus.conditionsByItem.ContainsKey(itemID))
                {
                    Mod.instance.LogInfo("updating task conditions with this item");
                    foreach (TraderTaskCondition condition in EFM_TraderStatus.conditionsByItem[itemID])
                    {
                        if (!condition.fulfilled && condition.marketListElement != null)
                        {
                            if (condition.conditionType == TraderTaskCondition.ConditionType.HandoverItem)
                            {
                                condition.marketListElement.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(true);
                            }
                            else if(condition.conditionType == TraderTaskCondition.ConditionType.WeaponAssembly)
                            {
                                // Make sure all requirements are fulfilled, set them as fulfilled by default if they werent a requirement to begin with
                                bool[] typesFulfilled = condition.targetAttachmentTypes.Count == 0 ? new bool[] { true } : new bool[condition.targetAttachmentTypes.Count];
                                bool[] attachmentsFulfilled = condition.targetAttachments.Count == 0 ? new bool[] { true } : new bool[condition.targetAttachments.Count];
                                bool supressedFulfilled = !condition.suppressed || (VID.physObj as FVRFireArm).IsSuppressed();
                                bool brakedFulfilled = !condition.braked || (VID.physObj as FVRFireArm).IsBraked();

                                // Only care to check everything if braked and supressed are both fulfilled
                                if (supressedFulfilled && brakedFulfilled)
                                {
                                    // WeaponAssembly items must be vanilla
                                    FVRPhysicalObject[] physObjs = VID.GetComponentsInChildren<FVRPhysicalObject>();
                                    foreach (FVRPhysicalObject physObj in physObjs)
                                    {
                                        string attachmentID = physObj.ObjectWrapper.ItemID;

                                        // Check types
                                        for (int i = 0; i < condition.targetAttachmentTypes.Count; ++i)
                                        {
                                            if (!typesFulfilled[i])
                                            {
                                                // Format list
                                                List<string> actualAttachments = new List<string>();
                                                foreach (string attachmentTypeID in condition.targetAttachmentTypes[i])
                                                {
                                                    if (Mod.itemsByParents.TryGetValue(attachmentTypeID, out List<string> items))
                                                    {
                                                        actualAttachments.AddRange(items);
                                                    }
                                                    else
                                                    {
                                                        actualAttachments.Add(attachmentTypeID);
                                                    }
                                                }

                                                // Check list
                                                if (actualAttachments.Contains(attachmentID))
                                                {
                                                    typesFulfilled[i] = true;
                                                }
                                            }
                                        }

                                        // Check specific attachments
                                        for (int i = 0; i < condition.targetAttachments.Count; ++i)
                                        {
                                            if (!attachmentsFulfilled[i])
                                            {
                                                if (condition.targetAttachments[i].Equals(attachmentID))
                                                {
                                                    attachmentsFulfilled[i] = true;
                                                }
                                            }
                                        }
                                    }

                                    if (!typesFulfilled.Contains(false) && !attachmentsFulfilled.Contains(false) && supressedFulfilled && brakedFulfilled)
                                    {
                                        condition.marketListElement.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(true);
                                    }
                                }
                            }
                        }
                    }
                }

                Mod.instance.LogInfo("trader insure");
                // IN INSURE, check if item already in showcase, if it is, increment count, if not, add a new entry, update price, make sure deal! button is activated, check if item is price, update accordingly
                bool itemInsureable = Mod.traderStatuses[currentTraderIndex].ItemInsureable(itemID, Mod.itemAncestors[itemID]);

                if (((custom && !CIW.insured) || (!custom && !VID.insured)) && itemInsureable)
                {
                    Mod.instance.LogInfo("update insure showcase");
                    Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
                    if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
                    {
                        Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
                        EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                        int actualInsureValue = itemInsureValue;
                        string currencyItemID = "";
                        if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                        {
                            currencyItemID = "203";
                        }
                        else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
                        {
                            itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
                            currencyItemID = "201";
                        }
                        marketItemView.insureValue = marketItemView.insureValue + actualInsureValue;
                        if (marketItemView.custom)
                        {
                            marketItemView.CIW.Add(CIW);
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                        }
                        else
                        {
                            marketItemView.VID.Add(VID);
                            currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                        }
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
                        currentTotalInsurePrice += actualInsureValue;

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

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = false;
                        currentItemIconScript.itemID = itemID;
                        currentItemIconScript.itemName = Mod.itemNames[itemID];
                        currentItemIconScript.description = Mod.itemDescriptions[itemID];
                        currentItemIconScript.weight = Mod.itemWeights[itemID];
                        currentItemIconScript.volume = Mod.itemVolumes[itemID];
                    }
                    else
                    {
                        Mod.instance.LogInfo("adding new insure entry");
                        // Add a new insure item entry
                        Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
                        GameObject insureHorizontalCopy = insureHorizontalsParent.GetChild(0).gameObject;

                        Mod.instance.LogInfo("0");
                        float insureShowCaseHeight = 3 + 24 * insureHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                        Transform currentHorizontal = insureHorizontalsParent.GetChild(insureHorizontalsParent.childCount - 1);
                        if (insureHorizontalsParent.childCount == 1) // If dont even have a single horizontal yet, add it
                        {
                            currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                        }
                        else if (currentHorizontal.childCount == 7) // If last horizontal is full
                        {
                            currentHorizontal = GameObject.Instantiate(insureHorizontalCopy, insureHorizontalsParent).transform;
                            currentHorizontal.gameObject.SetActive(true);
                            insureShowCaseHeight += 24; // horizontal
                        }
                        Mod.instance.LogInfo("0");

                        Transform currentItemIcon = GameObject.Instantiate(currentHorizontal.transform.GetChild(0), currentHorizontal).transform;

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.gameObject.AddComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.CIW = CIW;
                        currentItemIconScript.VID = VID;

                        currentItemIcon.gameObject.SetActive(true);
                        Mod.instance.LogInfo("0");
                        insureItemShowcaseElements.Add(itemID, currentItemIcon.gameObject);
                        if (Mod.itemIcons.ContainsKey(itemID))
                        {
                            currentItemIcon.GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(itemID, currentItemIcon.GetChild(2).GetComponent<Image>()));
                        }
                        Mod.instance.LogInfo("0");
                        EFM_MarketItemView marketItemView = currentItemIcon.gameObject.AddComponent<EFM_MarketItemView>();
                        marketItemView.custom = custom;
                        marketItemView.CIW = new List<EFM_CustomItemWrapper>() { CIW };
                        marketItemView.VID = new List<EFM_VanillaItemDescriptor>() { VID };

                        Mod.instance.LogInfo("0");
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
                            itemInsureValue = (int)Mathf.Max(itemInsureValue * 0.008f, 1); // Adjust item value
                            currencyItemID = "201";
                        }
                        Mod.instance.LogInfo("0");
                        marketItemView.insureValue = itemInsureValue;
                        currentTotalInsurePrice += itemInsureValue;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(0).GetComponent<Image>().sprite = currencySprite;
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = itemInsureValue.ToString();

                        // Set count text
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = "1";

                        Mod.instance.LogInfo("0");
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
                        Mod.instance.LogInfo("0");

                        // Update total insure price
                        traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = Mod.itemNames[currencyItemID];
                        traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[currencyItemID];

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

                        Mod.instance.LogInfo("0");
                        // Update hoverscrolls
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
                        Mod.instance.LogInfo("0");
                    }
                    traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalInsurePrice.ToString();
                }
                Transform insurePriceFulfilledIcons = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(2);
                Transform insureDealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0);

                if (itemInsureable || itemID.Equals("201") || itemID.Equals("203"))
                {
                    if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                    {
                        if (tradeVolumeInventory.ContainsKey("203") && tradeVolumeInventory["203"] >= currentTotalInsurePrice)
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

                            insureDealButton.GetComponent<Collider>().enabled = true;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
                        }
                        else
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

                            insureDealButton.GetComponent<Collider>().enabled = false;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
                        }
                    }
                    else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
                    {
                        if (tradeVolumeInventory.ContainsKey("201") && tradeVolumeInventory["201"] >= currentTotalInsurePrice)
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

                            insureDealButton.GetComponent<Collider>().enabled = true;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
                        }
                        else
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

                            insureDealButton.GetComponent<Collider>().enabled = false;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
                        }
                    }
                }
            }
            else
            {
                Mod.instance.LogInfo("removed");
                // Remove from trade volume inventory
                if (custom)
                {
                    Mod.instance.LogInfo("custom, pre amount: "+ tradeVolumeInventory[CIW.ID]);
                    tradeVolumeInventory[CIW.ID] -= (CIW.maxStack > 1 ? CIW.stack : 1);
                    tradeVolumeInventoryObjects[CIW.ID].Remove(CIW.gameObject);
                    Mod.instance.LogInfo("post amount: " + tradeVolumeInventory[CIW.ID]);
                    if (tradeVolumeInventory[CIW.ID] == 0)
                    {
                        tradeVolumeInventory.Remove(CIW.ID);
                        tradeVolumeInventoryObjects.Remove(CIW.ID);
                    }
                }
                else
                {
                    Mod.instance.LogInfo("vanilla");
                    tradeVolumeInventory[VID.H3ID] -= 1;
                    tradeVolumeInventoryObjects[VID.H3ID].Remove(VID.gameObject);
                    if (tradeVolumeInventory[VID.H3ID] == 0)
                    {
                        tradeVolumeInventory.Remove(VID.H3ID);
                        tradeVolumeInventoryObjects.Remove(VID.H3ID);
                    }
                }

                Mod.instance.LogInfo("updating trader buy");
                // IN BUY, check if item corresponds to price, update fulfilled icon and deactivate deal! button if necessary
                if (prices != null)
                {
                    bool foundID = false;
                    AssortmentPriceData foundPriceData = null;
                    foreach (AssortmentPriceData otherAssortPriceData in prices)
                    {
                        if (otherAssortPriceData.ID.Equals(itemID))
                        {
                            foundPriceData = otherAssortPriceData;
                            foundID = true;
                            break;
                        }
                    }
                    bool matchesType = false;
                    if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                    {
                        matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
                    }
                    else
                    {
                        matchesType = true;
                    }
                    if (foundID && matchesType)
                    {
                        // Go through each price because need to check if all are fulfilled anyway
                        bool canDeal = true;
                        for (int i = 0; i < prices.Count; ++i)
                        {
                            AssortmentPriceData priceData = prices[i];
                            if (tradeVolumeInventory.ContainsKey(priceData.ID))
                            {
                                // Find how many we have in trade inventory
                                // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
                                int matchingCountInInventory = 0;
                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                {
                                    foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
                                    {
                                        EFM_CustomItemWrapper priceCIW = priceObject.GetComponent<EFM_CustomItemWrapper>();
                                        if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
                                        {
                                            ++matchingCountInInventory;
                                        }
                                    }
                                }
                                else
                                {
                                    matchingCountInInventory = priceData.count;
                                }

                                // If this is the item we are removing, make sure the requirement fulfilled icon is active
                                if (matchingCountInInventory < (priceData.count * cartItemCount))
                                {
                                    // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
                                    if (priceData.ID.Equals(itemID))
                                    {
                                        Transform priceElement = buyPriceElements[i].transform;
                                        priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
                                        priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
                                    }
                                    canDeal = false;
                                }
                            }
                            else
                            {
                                // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
                                if (priceData.ID.Equals(itemID))
                                {
                                    Transform priceElement = buyPriceElements[i].transform;
                                    priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
                                    priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
                                }
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
                }

                Mod.instance.LogInfo("0");
                // IN RAGFAIR BUY, check if item corresponds to price, update fulfilled icon and deactivate deal! button if necessary
                if (ragfairPrices != null)
                {
                    bool foundID = false;
                    AssortmentPriceData foundPriceData = null;
                    foreach (AssortmentPriceData otherAssortPriceData in ragfairPrices)
                    {
                        if (otherAssortPriceData.ID.Equals(itemID))
                        {
                            foundPriceData = otherAssortPriceData;
                            foundID = true;
                            break;
                        }
                    }
                    bool matchesType = false;
                    if (foundID && foundPriceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                    {
                        matchesType = foundPriceData.dogtagLevel <= CIW.dogtagLevel;  // No need to check USEC because true or false have different item IDs
                    }
                    else
                    {
                        matchesType = true;
                    }
                    if (foundID && matchesType)
                    {
                        // Go through each price because need to check if all are fulfilled anyway
                        bool canDeal = true;
                        for (int i = 0; i < ragfairPrices.Count; ++i)
                        {
                            AssortmentPriceData priceData = ragfairPrices[i];
                            if (tradeVolumeInventory.ContainsKey(priceData.ID))
                            {
                                // Find how many we have in trade inventory
                                // If the type has more data (ie. dogtags) we must check if that data matches also, not just the ID
                                int matchingCountInInventory = 0;
                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                {
                                    foreach (GameObject priceObject in tradeVolumeInventoryObjects[priceData.ID])
                                    {
                                        EFM_CustomItemWrapper priceCIW = priceObject.GetComponent<EFM_CustomItemWrapper>();
                                        if (priceCIW.dogtagLevel >= priceData.dogtagLevel) // No need to check USEC because true or false have different item IDs
                                        {
                                            ++matchingCountInInventory;
                                        }
                                    }
                                }
                                else
                                {
                                    matchingCountInInventory = priceData.count;
                                }

                                // If this is the item we are removing, make sure the requirement fulfilled icon is active
                                if (matchingCountInInventory < (priceData.count * ragfairCartItemCount))
                                {
                                    // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
                                    if (priceData.ID.Equals(itemID))
                                    {
                                        Transform priceElement = ragfairBuyPriceElements[i].transform;
                                        priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
                                        priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
                                    }
                                    canDeal = false;
                                }
                            }
                            else
                            {
                                // If this is the item we are removing, make sure the requirement fulfilled icon is inactive
                                if (priceData.ID.Equals(itemID))
                                {
                                    Transform priceElement = ragfairBuyPriceElements[i].transform;
                                    priceElement.GetChild(2).GetChild(0).gameObject.SetActive(false);
                                    priceElement.GetChild(2).GetChild(1).gameObject.SetActive(true);
                                }
                                canDeal = false;
                            }
                        }
                        Transform dealButton = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(2).GetChild(0).GetChild(0);
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
                }

                Mod.instance.LogInfo("0");
                // IN SELL, find item in showcase, if there are more its stack, decrement count, if not, remove entry, update price under FOR, make sure deal! button is deactivated if no sellable item in volume (only need to check this if this item was sellable)
                Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
                if (sellItemShowcaseElements != null && sellItemShowcaseElements.ContainsKey(itemID))
                {
                    Transform sellHorizontalsParent = traderDisplay.GetChild(1).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
                    float sellShowCaseHeight = 3 + 24 * sellHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                    Transform currentItemIcon = sellItemShowcaseElements[itemID].transform;
                    EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                    int actualValue;
                    if (Mod.lowestBuyValueByItem.ContainsKey(itemID))
                    {
                        actualValue = (int)Mathf.Max(Mod.lowestBuyValueByItem[itemID] * 0.9f, 1);
                    }
                    else
                    {
                        // If we do not have a buy value to compare with, just use half of the original value TODO: Will have to adjust this multiplier if it is still too high
                        actualValue = (int)Mathf.Max(itemValue * 0.5f, 1);
                    }
                    actualValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? actualValue : (int)Mathf.Max(actualValue * 0.008f, 1);
                    marketItemView.value = marketItemView.value -= actualValue;
                    bool shouldRemove = false;
                    bool lastOne = false;
                    if (marketItemView.custom)
                    {
                        marketItemView.CIW.Remove(CIW);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                        shouldRemove = marketItemView.CIW.Count == 0;
                        lastOne = marketItemView.CIW.Count == 1;
                    }
                    else
                    {
                        marketItemView.VID.Remove(VID);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                        shouldRemove = marketItemView.VID.Count == 0;
                        lastOne = marketItemView.VID.Count == 1;
                    }
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.value.ToString();
                    currentTotalSellingPrice -= actualValue;

                    // Setup itemIcon
                    if (lastOne)
                    {
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.CIW = CIW;
                        currentItemIconScript.VID = VID;
                    }

                    // Remove item if necessary and move all other item icons that come after
                    if (shouldRemove)
                    {
                        currentItemIcon.SetParent(null);
                        Destroy(currentItemIcon.gameObject);
                        sellItemShowcaseElements.Remove(itemID);

                        for (int i=1; i<sellHorizontalsParent.childCount - 1; ++i)
                        {
                            Transform currentHorizontal = sellHorizontalsParent.GetChild(i);
                            // If item icon missing from this horizontal but not thi is not the last horizontal
                            if(currentHorizontal.childCount == 6 && i < sellHorizontalsParent.childCount - 2)
                            {
                                // Take item icon from next horizontal and put it on current
                                sellHorizontalsParent.GetChild(i + 1).GetChild(0).SetParent(currentHorizontal);
                            }
                            else if(currentHorizontal.childCount == 0)
                            {
                                currentHorizontal.SetParent(null);
                                Destroy(currentHorizontal.gameObject);

                                sellShowCaseHeight -= 24;
                                break; // we can break here because if there are 0 it means there wasnt another horizontal after it
                            }
                        }
                    }

                    // Deactivate deal button if no sellable items
                    if (sellHorizontalsParent.childCount == 1)
                    {
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
                        traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
                    }

                    // Update hoverscrolls
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
                }
                // else, if we are removing an item and it is not a sellable item (not in sellItemShowcaseElements) we dont need to do anything
                traderDisplay.GetChild(1).GetChild(2).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalSellingPrice.ToString();

                Mod.instance.LogInfo("0");
                // IN TASKS, for each item requirement of each task, deactivate TURN IN buttons accordingly
                if (EFM_TraderStatus.conditionsByItem.ContainsKey(itemID))
                {
                    foreach (TraderTaskCondition condition in EFM_TraderStatus.conditionsByItem[itemID])
                    {
                        if ((condition.conditionType == TraderTaskCondition.ConditionType.HandoverItem || condition.conditionType == TraderTaskCondition.ConditionType.WeaponAssembly) &&
                            !condition.fulfilled && condition.marketListElement != null)
                        {
                            if (!tradeVolumeInventory.ContainsKey(itemID) || tradeVolumeInventory[itemID] == 0)
                            {
                                condition.marketListElement.transform.GetChild(0).GetChild(0).GetChild(5).gameObject.SetActive(false);
                            }
                        }
                    }
                }

                Mod.instance.LogInfo("0");
                // IN INSURE, find item in showcase, if there are more its stack, decrement count, if not, remove entry, update price under FOR, make sure deal! button is deactivated if no insureable item in volume (only need to check this if this item was insureable)
                bool insureable = false;
                if (insureItemShowcaseElements != null && insureItemShowcaseElements.ContainsKey(itemID))
                {
                    insureable = true;
                    Transform insureHorizontalsParent = traderDisplay.GetChild(1).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
                    float insureShowCaseHeight = 3 + 24 * insureHorizontalsParent.childCount - 1; // Top padding + horizontal * number of horizontals
                    Transform currentItemIcon = insureItemShowcaseElements[itemID].transform;
                    EFM_MarketItemView marketItemView = currentItemIcon.GetComponent<EFM_MarketItemView>();
                    int actualInsureValue = Mod.traderStatuses[currentTraderIndex].currency == 0 ? itemInsureValue : (int)Mathf.Max(itemInsureValue * 0.008f, 1);
                    marketItemView.insureValue = marketItemView.insureValue -= actualInsureValue;
                    bool shouldRemove = false;
                    bool lastOne = false;
                    if (marketItemView.custom)
                    {
                        marketItemView.CIW.Remove(CIW);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.CIW.Count.ToString();
                        shouldRemove = marketItemView.CIW.Count == 0;
                        lastOne = marketItemView.CIW.Count == 1;
                    }
                    else
                    {
                        marketItemView.VID.Remove(VID);
                        currentItemIcon.GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = marketItemView.VID.Count.ToString();
                        shouldRemove = marketItemView.VID.Count == 0;
                        lastOne = marketItemView.VID.Count == 1;
                    }
                    currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = marketItemView.insureValue.ToString();
                    currentTotalInsurePrice -= actualInsureValue;

                    // Setup itemIcon
                    if (lastOne)
                    {
                        EFM_ItemIcon currentItemIconScript = currentItemIcon.GetComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = true;
                        currentItemIconScript.isCustom = custom;
                        currentItemIconScript.CIW = CIW;
                        currentItemIconScript.VID = VID;
                    }

                    // Remove item if necessary and move all other item icons that come after
                    if (shouldRemove)
                    {
                        currentItemIcon.SetParent(null);
                        Destroy(currentItemIcon.gameObject);
                        insureItemShowcaseElements.Remove(itemID);

                        for (int i = 1; i < insureHorizontalsParent.childCount - 1; ++i)
                        {
                            Transform currentHorizontal = insureHorizontalsParent.GetChild(i);
                            // If item icon missing from this horizontal but not thi is not the last horizontal
                            if (currentHorizontal.childCount == 6 && i < insureHorizontalsParent.childCount - 2)
                            {
                                // Take item icon from next horizontal and put it on current
                                insureHorizontalsParent.GetChild(i + 1).GetChild(0).SetParent(currentHorizontal);
                            }
                            else if (currentHorizontal.childCount == 0)
                            {
                                currentHorizontal.SetParent(null);
                                Destroy(currentHorizontal.gameObject);

                                insureShowCaseHeight -= 24;
                                break; // we can break here because if there are 0 it means there wasnt another horizontal after it
                            }
                        }
                    }

                    // Deactivate deal button if no insureable items
                    if (insureHorizontalsParent.childCount == 1)
                    {
                        traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetComponent<Collider>().enabled = false;
                        traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0).GetChild(1).GetComponent<Text>().color = new Color(0.15f, 0.15f, 0.15f);
                    }
                    else
                    {
                        string currencyItemID = "";
                        if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                        {
                            currencyItemID = "203";
                        }
                        else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
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
                    }

                    // Update hoverscrolls
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
                // else, if we are removing an item and it is not an insureable item (not in insureItemShowcaseElements) we dont need to do anything
                traderDisplay.GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = currentTotalInsurePrice.ToString();
                Transform insurePriceFulfilledIcons = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(1).GetChild(1).GetChild(2);
                Transform insureDealButton = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(0).GetChild(1).GetChild(2).GetChild(0).GetChild(0);

                Mod.instance.LogInfo("0");
                if (insureable || itemID.Equals("201") || itemID.Equals("203"))
                {
                    if (Mod.traderStatuses[currentTraderIndex].currency == 0)
                    {
                        if (tradeVolumeInventory.ContainsKey("203") && tradeVolumeInventory["203"] >= currentTotalInsurePrice)
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

                            insureDealButton.GetComponent<Collider>().enabled = true;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
                        }
                        else
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

                            insureDealButton.GetComponent<Collider>().enabled = false;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
                        }
                    }
                    else if (Mod.traderStatuses[currentTraderIndex].currency == 1)
                    {
                        if (tradeVolumeInventory.ContainsKey("201") && tradeVolumeInventory["201"] >= currentTotalInsurePrice)
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(true);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(false);

                            insureDealButton.GetComponent<Collider>().enabled = true;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.white;
                        }
                        else
                        {
                            insurePriceFulfilledIcons.GetChild(0).gameObject.SetActive(false);
                            insurePriceFulfilledIcons.GetChild(1).gameObject.SetActive(true);

                            insureDealButton.GetComponent<Collider>().enabled = false;
                            insureDealButton.GetChild(1).GetComponent<Text>().color = Color.gray;
                        }
                    }
                }
            }
        }

        public void UpdateBasedOnPlayerLevel()
        {
            SetTrader(currentTraderIndex);

            // TODO: Will also need to check if level 15 then we also want to add player items to flea market
        }

        public void OnBuyItemClick(AssortmentItem item, List<AssortmentPriceData> priceList, Sprite itemIcon)
        {
            cartItem = item.ID;
            cartItemCount = 1;
            prices = priceList;
            Mod.instance.LogInfo("on buy item click called, with ID: " + item.ID);
            string itemName = Mod.itemNames[item.ID];
            Mod.instance.LogInfo("Got item name: " + itemName);

            Transform cartShowcase = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1);
            cartShowcase.GetChild(0).GetComponent<Text>().text = itemName;
            cartShowcase.GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = itemIcon;
            cartShowcase.GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "1";

            // Setup ItemIcon
            EFM_ItemIcon itemIconScript = cartShowcase.GetChild(1).gameObject.GetComponent<EFM_ItemIcon>();
            if (itemIconScript == null)
            {
                itemIconScript = cartShowcase.GetChild(1).gameObject.AddComponent<EFM_ItemIcon>();
            }
            itemIconScript.itemID = item.ID;
            itemIconScript.itemName = itemName;
            itemIconScript.description = Mod.itemDescriptions[item.ID];
            Mod.instance.LogInfo("Got item description");
            itemIconScript.weight = Mod.itemWeights[item.ID];
            itemIconScript.volume = Mod.itemVolumes[item.ID];

            Transform pricesParent = cartShowcase.GetChild(3).GetChild(0).GetChild(0);
            GameObject priceTemplate = pricesParent.GetChild(0).gameObject;
            float priceHeight = 0;
            while(pricesParent.childCount > 1)
            {
                Transform currentFirstChild = pricesParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            if (buyPriceElements == null)
            {
                buyPriceElements = new List<GameObject>();
            }
            else
            {
                buyPriceElements.Clear();
            }
            bool canDeal = true;
            foreach(AssortmentPriceData price in priceList)
            {
                Mod.instance.LogInfo("\tSetting price: "+price.ID);
                priceHeight += 50;
                Transform priceElement = Instantiate(priceTemplate, pricesParent).transform;
                priceElement.gameObject.SetActive(true);

                if (Mod.itemIcons.ContainsKey(price.ID))
                {
                    priceElement.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[price.ID];
                }
                else
                {
                    AnvilManager.Run(Mod.SetVanillaIcon(price.ID, priceElement.GetChild(0).GetChild(2).GetComponent<Image>()));
                }
                priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = price.count.ToString();
                string priceName = Mod.itemNames[price.ID];
                Mod.instance.LogInfo("\t\tGot name: " + priceName);
                priceElement.GetChild(3).GetChild(0).GetComponent<Text>().text = priceName;
                if(price.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                {
                    priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).gameObject.SetActive(true);
                    priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = ">= lvl "+price.dogtagLevel;
                }
                buyPriceElements.Add(priceElement.gameObject);

                // Setup ItemIcon
                EFM_ItemIcon priceIconScript = priceElement.gameObject.AddComponent<EFM_ItemIcon>();
                priceIconScript.itemID = price.ID;
                priceIconScript.itemName = priceName;
                priceIconScript.description = Mod.itemDescriptions[price.ID];
                Mod.instance.LogInfo("\t\tGot description");
                priceIconScript.weight = Mod.itemWeights[price.ID];
                priceIconScript.volume = Mod.itemVolumes[price.ID];

                if (tradeVolumeInventory.ContainsKey(price.ID) && tradeVolumeInventory[price.ID] >= price.count)
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
            EFM_HoverScroll upHoverScroll = cartShowcase.GetChild(3).GetChild(2).GetComponent<EFM_HoverScroll>();
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
            // Remove price from trade volume
            foreach(AssortmentPriceData price in prices)
            {
                // TODO: Make removing items from hideout as a common utility inside base manager, ebcause we also use this in base manager and we could probably use it elsewhere
                int amountToRemove = price.count;
                tradeVolumeInventory[price.ID] -= (price.count * cartItemCount);
                List<GameObject> objectList = tradeVolumeInventoryObjects[price.ID];
                while (amountToRemove > 0)
                {
                    GameObject currentItemObject = objectList[objectList.Count - 1];
                    EFM_CustomItemWrapper CIW = currentItemObject.GetComponent<EFM_CustomItemWrapper>();
                    if (CIW != null)
                    {
                        if (CIW.maxStack > 1)
                        {
                            int stack = CIW.stack;
                            if(stack - amountToRemove <= 0)
                            {
                                amountToRemove -= stack;

                                // Destroy item
                                objectList.RemoveAt(objectList.Count - 1);
                                Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                CIW.destroyed = true;
                                currentItemObject.transform.parent = null;
                                Destroy(currentItemObject);
                            }
                            else // stack - amountToRemove > 0
                            {
                                CIW.stack -= amountToRemove;
                                Mod.baseInventory[CIW.ID] -= amountToRemove;
                                Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                amountToRemove = 0;
                            }
                        }
                        else // Doesnt have stack, its a single item
                        {
                            // If dogtag must only delete the ones that match the required level
                            if(CIW.itemType == Mod.ItemType.DogTag)
                            {
                                if (CIW.dogtagLevel >= price.dogtagLevel)
                                {
                                    --amountToRemove;

                                    // Destroy item
                                    objectList.RemoveAt(objectList.Count - 1);
                                    Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                    CIW.destroyed = true;
                                    currentItemObject.transform.parent = null;
                                    Destroy(currentItemObject);
                                }
                            }
                            else
                            {
                                --amountToRemove;

                                // Destroy item
                                objectList.RemoveAt(objectList.Count - 1);
                                Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                CIW.destroyed = true;
                                currentItemObject.transform.parent = null;
                                Destroy(currentItemObject);
                            }
                        }
                    }
                    else // Vanilla item cannot have stack
                    {
                        --amountToRemove;

                        // Destroy item
                        objectList.RemoveAt(objectList.Count - 1);
                        Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                        currentItemObject.GetComponent<EFM_VanillaItemDescriptor>().destroyed = true;
                        currentItemObject.transform.parent = null;
                        Destroy(currentItemObject);
                    }
                }

                // Remove this item from lists if dont have anymore in inventory
                if (tradeVolumeInventory[price.ID] == 0)
                {
                    tradeVolumeInventory.Remove(price.ID);
                    tradeVolumeInventoryObjects.Remove(price.ID);
                    // TODO: In this case, we could just destroy all objects in tradeVolumeInventoryObjects[price.Key] right away without having to check stacks like in the while loop above
                }

                // Update area managers based on item we just removed
                foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
                {
                    areaManager.UpdateBasedOnItem(price.ID);
                }
            }

            // Add bought amount of item to trade volume at random pos and rot within it
            SpawnItem(cartItem, cartItemCount);

            // Update amount of item in trader's assort
            Mod.traderStatuses[currentTraderIndex].assortmentByLevel[Mod.traderStatuses[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack -= cartItemCount;
        }

        public IEnumerator SpawnVanillaItem(string ID, int count)
        {
            yield return IM.OD[ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.instance.LogWarning("Attempted to get vanilla prefab for " + ID + ", but the prefab had been destroyed, refreshing cache...");

                IM.OD[ID].RefreshCache();
                do
                {
                    Mod.instance.LogInfo("Waiting for cache refresh...");
                    itemPrefab = IM.OD[ID].GetGameObject();
                } while (itemPrefab == null);
            }
            EFM_VanillaItemDescriptor prefabVID = itemPrefab.GetComponent<EFM_VanillaItemDescriptor>();
            BoxCollider tradeVolumeCollider = tradeVolume.GetComponentInChildren<BoxCollider>();
            GameObject itemObject = null;
            bool spawnedSmallBox = false;
            bool spawnedBigBox = false;
            if (Mod.usedRoundIDs.Contains(prefabVID.H3ID))
            {
                // Round, so must spawn an ammobox with specified stack amount if more than 1 instead of the stack of round
                if (count > 1)
                {
                    int countLeft = count;
                    float boxCountLeft = count / 120.0f;
                    while (boxCountLeft > 0)
                    {
                        int amount = 0;
                        if (countLeft > 30)
                        {
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[716], tradeVolume.itemsRoot);
                            if (tradeVolumeInventory.ContainsKey("716"))
                            {
                                tradeVolumeInventory["716"] += 1;
                                tradeVolumeInventoryObjects["716"].Add(itemObject);
                            }
                            else
                            {
                                tradeVolumeInventory.Add("716", 1);
                                tradeVolumeInventoryObjects.Add("716", new List<GameObject>() { itemObject });
                            }
                            Mod.currentBaseManager.AddToBaseInventory(itemObject.transform, true);

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

                            spawnedBigBox = true;
                        }
                        else
                        {
                            itemObject = GameObject.Instantiate(Mod.itemPrefabs[715], tradeVolume.itemsRoot);

                            if (tradeVolumeInventory.ContainsKey("715"))
                            {
                                tradeVolumeInventory["715"] += 1;
                                tradeVolumeInventoryObjects["715"].Add(itemObject);
                            }
                            else
                            {
                                tradeVolumeInventory.Add("715", 1);
                                tradeVolumeInventoryObjects.Add("715", new List<GameObject>() { itemObject });
                            }
                            Mod.currentBaseManager.AddToBaseInventory(itemObject.transform, true);

                            amount = countLeft;
                            countLeft = 0;

                            spawnedSmallBox = true;
                        }

                        EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                        FVRFireArmMagazine asMagazine = itemCIW.physObj as FVRFireArmMagazine;
                        FVRFireArmRound round = itemPrefab.GetComponentInChildren<FVRFireArmRound>();
                        asMagazine.RoundType = round.RoundType;
                        itemCIW.roundClass = round.RoundClass;
                        for (int j = 0; j < amount; ++j)
                        {
                            asMagazine.AddRound(itemCIW.roundClass, false, false);
                        }

                        // Add item to tradevolume so it can set its reset cols and kinematic to true
                        tradeVolume.AddItem(itemCIW.physObj);

                        itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-tradeVolumeCollider.size.x / 2, tradeVolumeCollider.size.x / 2),
                                                                         UnityEngine.Random.Range(-tradeVolumeCollider.size.y / 2, tradeVolumeCollider.size.y / 2),
                                                                         UnityEngine.Random.Range(-tradeVolumeCollider.size.z / 2, tradeVolumeCollider.size.z / 2));
                        itemObject.transform.localRotation = UnityEngine.Random.rotation;

                        BeginInteractionPatch.SetItemLocationIndex(1, itemCIW, null, false);

                        boxCountLeft = countLeft / 120.0f;
                    }
                }
                else // Single round, spawn as normal
                {
                    itemObject = GameObject.Instantiate(itemPrefab, tradeVolume.itemsRoot);

                    EFM_VanillaItemDescriptor VID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();

                    // Add item to tradevolume so it can set its reset cols and kinematic to true
                    tradeVolume.AddItem(VID.physObj);

                    BeginInteractionPatch.SetItemLocationIndex(1, null, VID, false);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-tradeVolumeCollider.size.x / 2, tradeVolumeCollider.size.x / 2),
                                                                     UnityEngine.Random.Range(-tradeVolumeCollider.size.y / 2, tradeVolumeCollider.size.y / 2),
                                                                     UnityEngine.Random.Range(-tradeVolumeCollider.size.z / 2, tradeVolumeCollider.size.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                        tradeVolumeInventoryObjects[VID.H3ID].Add(itemObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                        tradeVolumeInventoryObjects.Add(VID.H3ID, new List<GameObject>() { itemObject });
                    }
                    Mod.currentBaseManager.AddToBaseInventory(itemObject.transform, true);
                }
            }
            else // Not a round, spawn as normal
            {
                for (int i = 0; i < count; ++i)
                {
                    itemObject = GameObject.Instantiate(itemPrefab, tradeVolume.itemsRoot);

                    EFM_VanillaItemDescriptor VID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();

                    // Add item to tradevolume so it can set its reset cols and kinematic to true
                    tradeVolume.AddItem(VID.physObj);
                    
                    BeginInteractionPatch.SetItemLocationIndex(1, null, VID, false);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-tradeVolumeCollider.size.x / 2, tradeVolumeCollider.size.x / 2),
                                                                     UnityEngine.Random.Range(-tradeVolumeCollider.size.y / 2, tradeVolumeCollider.size.y / 2),
                                                                     UnityEngine.Random.Range(-tradeVolumeCollider.size.z / 2, tradeVolumeCollider.size.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;


                    if (tradeVolumeInventory.ContainsKey(VID.H3ID))
                    {
                        tradeVolumeInventory[VID.H3ID] += 1;
                        tradeVolumeInventoryObjects[VID.H3ID].Add(itemObject);
                    }
                    else
                    {
                        tradeVolumeInventory.Add(VID.H3ID, 1);
                        tradeVolumeInventoryObjects.Add(VID.H3ID, new List<GameObject>() { itemObject });
                    }
                    Mod.currentBaseManager.AddToBaseInventory(itemObject.transform, true);
                }
            }

            // Update all areas based on the item
            foreach (EFM_BaseAreaManager baseAreaManager in baseManager.baseAreaManagers)
            {
                if (spawnedSmallBox || spawnedBigBox)
                {
                    if (spawnedSmallBox)
                    {
                        baseAreaManager.UpdateBasedOnItem("715");
                    }
                    if (spawnedBigBox)
                    {
                        baseAreaManager.UpdateBasedOnItem("716");
                    }
                }
                else
                {
                    baseAreaManager.UpdateBasedOnItem(ID);
                }
            }
            Mod.instance.LogInfo("callingsettrader");

            // Refresh trader when done spawning items
            SetTrader(currentTraderIndex, ID);

            yield break;
        }

        public void OnBuyAmountClick()
        {
            // TODO: Check coordination between stack split and buy amount choosing. For now buy amount will replace stack split if splitting stack, but havent checked other way around

            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                Mod.splittingItem.CancelSplit();
            }

            // Disable buy amount buttons until done choosing amount
            transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;
            transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;

            // Start splitting
            Mod.stackSplitUI.SetActive(true);
            Mod.stackSplitUI.transform.localPosition = Mod.rightHand.transform.localPosition + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.localRotation = Quaternion.Euler(0, Mod.rightHand.transform.localRotation.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.localPosition;
            amountChoiceRightVector = Mod.rightHand.transform.right;
            amountChoiceRightVector.y = 0;

            choosingBuyAmount = true;
            startedChoosingThisFrame = true;

            // Set max buy amount, limit it to 360 otherwise scale is not large enough and its hard to specify an exact value
            maxBuyAmount = Mathf.Min(360, Mod.traderStatuses[currentTraderIndex].assortmentByLevel[Mod.traderStatuses[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack);
        }

        //public void OnSellItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        //{
        //    // TODO: MAYBE DONT EVEN NEED THIS, WE JUST SELL EVERYTHING IN THE SELL SHOWCASE
        //    // TODO: Set cart UI to this item
        //    // de/activate deal! button depending on whether trader has enough money
        //}

        public void OnSellDealClick()
        {
            // Remove all sellable items from trade volume
            List<string> itemsToRemove = new List<string>();
            foreach (KeyValuePair<string, int> item in tradeVolumeInventory)
            {
                if(Mod.traderStatuses[currentTraderIndex].ItemSellable(item.Key, Mod.itemAncestors[item.Key]))
                {
                    foreach (GameObject itemObject in tradeVolumeInventoryObjects[item.Key])
                    {
                        // Destroy item
                        EFM_CustomItemWrapper CIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                        Mod.currentBaseManager.RemoveFromBaseInventory(itemObject.transform, true);
                        // Unparent object before destroying so it doesnt get processed by settrader
                        itemObject.transform.parent = null;
                        Destroy(itemObject);
                    }
                    itemsToRemove.Add(item.Key);
                }

                // Update area managers based on item we just removed
                foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
                {
                    areaManager.UpdateBasedOnItem(item.Key);
                }
            }
            foreach(string itemID in itemsToRemove)
            {
                tradeVolumeInventory.Remove(itemID);
                tradeVolumeInventoryObjects.Remove(itemID);
            }

            // Add sold for item to trade volume
            int amountToSpawn = currentTotalSellingPrice;
            int currencyID = Mod.traderStatuses[currentTraderIndex].currency == 0 ? 203 : 201; // Roubles, else USD
            GameObject itemPrefab = Mod.itemPrefabs[currencyID];
            EFM_CustomItemWrapper prefabCIW = itemPrefab.GetComponent<EFM_CustomItemWrapper>();
            BoxCollider tradeVolumeCollider = tradeVolume.GetComponentInChildren<BoxCollider>();
            List<GameObject> objectsList = new List<GameObject>();
            while (amountToSpawn > 0)
            {
                GameObject spawnedItem = Instantiate(itemPrefab, tradeVolume.itemsRoot);
                objectsList.Add(spawnedItem);
                float xSize = tradeVolumeCollider.size.x;
                float ySize = tradeVolumeCollider.size.y;
                float zSize = tradeVolumeCollider.size.z;
                spawnedItem.transform.localPosition = new Vector3(UnityEngine.Random.Range(-xSize / 2, xSize / 2),
                                                                  UnityEngine.Random.Range(-ySize / 2, ySize / 2),
                                                                  UnityEngine.Random.Range(-zSize / 2, zSize / 2));
                spawnedItem.transform.localRotation = UnityEngine.Random.rotation;

                EFM_CustomItemWrapper itemCIW = spawnedItem.GetComponent<EFM_CustomItemWrapper>();
                itemCIW.stack = Mathf.Min(amountToSpawn, prefabCIW.maxStack);
                amountToSpawn -= prefabCIW.maxStack;

                // Add item to tradevolume so it can set its reset cols and kinematic to true
                tradeVolume.AddItem(itemCIW.physObj);

                Mod.currentBaseManager.AddToBaseInventory(spawnedItem.transform, true);

                BeginInteractionPatch.SetItemLocationIndex(1, itemCIW, null, false);
            }
            string stringCurrencyID = currencyID.ToString();
            if (tradeVolumeInventory.ContainsKey(stringCurrencyID))
            {
                tradeVolumeInventory[stringCurrencyID] += currentTotalSellingPrice;
                tradeVolumeInventoryObjects[stringCurrencyID].AddRange(objectsList);
            }
            else
            {
                tradeVolumeInventory.Add(stringCurrencyID, currentTotalSellingPrice);
                tradeVolumeInventoryObjects.Add(stringCurrencyID, objectsList);
            }

            // Update area managers based on item we just added
            foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
            {
                areaManager.UpdateBasedOnItem(stringCurrencyID);
            }

            // Update the whole thing
            SetTrader(currentTraderIndex);
        }

        //public void OnInsureItemClick(Transform currentItemIcon, int itemValue, string currencyItemID)
        //{
        //    // TODO: MAYBE DONT EVEN NEED THIS, WE JUST INSURE EVERYTHING IN THE INSURE SHOWCASE
        //    // TODO: Set cart UI to this item
        //    // de/activate deal! button depending on whether trade volume has enough money
        //}

        public void OnInsureDealClick()
        {
            // Set all insureable items in trade volume as insured
            foreach (KeyValuePair<string, int> item in tradeVolumeInventory)
            {
                if (Mod.traderStatuses[currentTraderIndex].ItemInsureable(item.Key, Mod.itemAncestors[item.Key]))
                { 
                    foreach(GameObject itemObject in tradeVolumeInventoryObjects[item.Key])
                    {
                        EFM_CustomItemWrapper CIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
                        EFM_VanillaItemDescriptor VID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
                        if(CIW != null && !CIW.insured)
                        {
                            CIW.insured = true;
                        }
                        else if(!VID.insured)
                        {
                            VID.insured = true;
                        }
                    }
                }
            }

            // Remove price from trade volume
            int amountToRemove = currentTotalInsurePrice;
            string currencyID = Mod.traderStatuses[currentTraderIndex].currency == 0 ? "203" : "201"; // Roubles, else USD
            tradeVolumeInventory[currencyID] -= amountToRemove;
            List<GameObject> objectList = tradeVolumeInventoryObjects[currencyID];
            while (amountToRemove > 0)
            {
                GameObject currentItemObject = objectList[objectList.Count - 1];
                EFM_CustomItemWrapper CIW = currentItemObject.GetComponent<EFM_CustomItemWrapper>();
                int stack = CIW.stack;
                if (stack - amountToRemove <= 0)
                {
                    amountToRemove -= stack;

                    // Destroy item
                    objectList.RemoveAt(objectList.Count - 1);
                    Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                    CIW.destroyed = true;
                    currentItemObject.transform.parent = null;
                    Destroy(currentItemObject);
                }
                else // stack - amountToRemove > 0
                {
                    CIW.stack -= amountToRemove;
                    Mod.baseInventory[CIW.ID] -= amountToRemove;
                    amountToRemove = 0;
                }
            }

            // Remove this item from lists if dont have anymore in inventory
            if (tradeVolumeInventory[currencyID] == 0)
            {
                tradeVolumeInventory.Remove(currencyID);
                tradeVolumeInventoryObjects.Remove(currencyID);
                // TODO: In this case, we could just destroy all objects in tradeVolumeInventoryObjects[price.Key] right away without having to check stacks like in the while loop above
            }

            // Update area managers based on item we just removed
            foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
            {
                areaManager.UpdateBasedOnItem(currencyID);
            }

            // Update trader UI
            SetTrader(currentTraderIndex);
        }

        public void OnTaskShortInfoClick(GameObject description)
        {
            // Toggle task description
            description.SetActive(!description.activeSelf);
            clickAudio.Play();

            if (description.activeSelf)
            {
                // Set to call UpdateTaskListHeight on next frame because we will need description height but that will only be accessible next frame
                mustUpdateTaskListHeight = 1;
            }
            else
            {
                // If deactivated we can update height right away
                UpdateTaskListHeight();
            }
        }

        public void AddTask(TraderTask task)
        {
            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);
            Transform tasksParent = traderDisplay.GetChild(1).GetChild(1).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject taskTemplate = tasksParent.GetChild(0).gameObject;
            GameObject currentTaskElement = Instantiate(taskTemplate, tasksParent);
            currentTaskElement.SetActive(true);
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
            foreach (TraderTaskCondition currentCondition in task.completionConditions)
            {
                GameObject currentObjectiveElement = Instantiate(objectiveTemplate, objectivesParent);
                currentObjectiveElement.SetActive(true);
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
                                    objectiveInfo.GetChild(3).gameObject.SetActive(true); // Activate progress counter
                                    objectiveInfo.GetChild(3).GetComponent<Text>().text = "0/" + currentCondition.value; // Activate progress counter
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
                currentInitEquipHorizontal.gameObject.SetActive(true);
                foreach (TraderTaskReward reward in task.startingEquipment)
                {
                    // Add new horizontal if necessary
                    if (currentInitEquipHorizontal.childCount == 6)
                    {
                        currentInitEquipHorizontal = Instantiate(currentInitEquipHorizontalTemplate, initEquipParent).transform;
                        currentInitEquipHorizontal.gameObject.SetActive(true);
                    }
                    switch (reward.taskRewardType)
                    {
                        case TraderTaskReward.TaskRewardType.Item:
                            GameObject currentInitEquipItemElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                            currentInitEquipItemElement.SetActive(true);
                            if (Mod.itemIcons.ContainsKey(reward.itemID))
                            {
                                currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                            }
                            else
                            {
                                AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                            }
                            if (reward.amount > 1)
                            {
                                currentInitEquipItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                            }
                            else
                            {
                                currentInitEquipItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                            }
                            currentInitEquipItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                            // Setup ItemIcon
                            EFM_ItemIcon itemIconScript = currentInitEquipItemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                            itemIconScript.itemID = reward.itemID;
                            itemIconScript.itemName = Mod.itemNames[reward.itemID];
                            itemIconScript.description = Mod.itemDescriptions[reward.itemID];
                            itemIconScript.weight = Mod.itemWeights[reward.itemID];
                            itemIconScript.volume = Mod.itemVolumes[reward.itemID];
                            break;
                        case TraderTaskReward.TaskRewardType.TraderUnlock:
                            GameObject currentInitEquipTraderUnlockElement = Instantiate(currentInitEquipHorizontal.GetChild(3).gameObject, currentInitEquipHorizontal);
                            currentInitEquipTraderUnlockElement.SetActive(true);
                            currentInitEquipTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                            currentInitEquipTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                            break;
                        case TraderTaskReward.TaskRewardType.TraderStanding:
                            GameObject currentInitEquipStandingElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                            currentInitEquipStandingElement.SetActive(true);
                            currentInitEquipStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                            currentInitEquipStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                            currentInitEquipStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                            currentInitEquipStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                            break;
                        case TraderTaskReward.TaskRewardType.Experience:
                            GameObject currentInitEquipExperienceElement = Instantiate(currentInitEquipHorizontal.GetChild(1).gameObject, currentInitEquipHorizontal);
                            currentInitEquipExperienceElement.SetActive(true);
                            currentInitEquipExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                            currentInitEquipExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                            break;
                        case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                            GameObject currentInitEquipAssortElement = Instantiate(currentInitEquipHorizontal.GetChild(0).gameObject, currentInitEquipHorizontal);
                            currentInitEquipAssortElement.SetActive(true);
                            if (Mod.itemIcons.ContainsKey(reward.itemID))
                            {
                                currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                            }
                            else
                            {
                                AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentInitEquipAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                            }
                            currentInitEquipAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                            currentInitEquipAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                            // Setup ItemIcon
                            EFM_ItemIcon assortIconScript = currentInitEquipAssortElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                            assortIconScript.itemID = reward.itemID;
                            assortIconScript.itemName = Mod.itemNames[reward.itemID];
                            assortIconScript.description = Mod.itemDescriptions[reward.itemID];
                            assortIconScript.weight = Mod.itemWeights[reward.itemID];
                            assortIconScript.volume = Mod.itemVolumes[reward.itemID];
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
            foreach (TraderTaskReward reward in task.successRewards)
            {
                // Add new horizontal if necessary
                if (currentRewardHorizontal.childCount == 6)
                {
                    currentRewardHorizontal = Instantiate(currentRewardHorizontalTemplate, rewardParent).transform;
                    currentRewardHorizontal.gameObject.SetActive(true);
                }
                switch (reward.taskRewardType)
                {
                    case TraderTaskReward.TaskRewardType.Item:
                        GameObject currentRewardItemElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                        currentRewardItemElement.SetActive(true);
                        if (Mod.itemIcons.ContainsKey(reward.itemID))
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardItemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        if (reward.amount > 1)
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = reward.amount.ToString();
                        }
                        else
                        {
                            currentRewardItemElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        }
                        currentRewardItemElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                        // Setup ItemIcon
                        EFM_ItemIcon itemIconScript = currentRewardItemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                        itemIconScript.itemID = reward.itemID;
                        itemIconScript.itemName = Mod.itemNames[reward.itemID];
                        itemIconScript.description = Mod.itemDescriptions[reward.itemID];
                        itemIconScript.weight = Mod.itemWeights[reward.itemID];
                        itemIconScript.volume = Mod.itemVolumes[reward.itemID];
                        break;
                    case TraderTaskReward.TaskRewardType.TraderUnlock:
                        GameObject currentRewardTraderUnlockElement = Instantiate(currentRewardHorizontal.GetChild(3).gameObject, currentRewardHorizontal);
                        currentRewardTraderUnlockElement.SetActive(true);
                        currentRewardTraderUnlockElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.traderAvatars[reward.traderIndex];
                        currentRewardTraderUnlockElement.transform.GetChild(1).GetComponent<Text>().text = "Unlock " + Mod.traderStatuses[reward.traderIndex].name;
                        break;
                    case TraderTaskReward.TaskRewardType.TraderStanding:
                        GameObject currentRewardStandingElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardStandingElement.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.standingSprite;
                        currentRewardStandingElement.transform.GetChild(1).gameObject.SetActive(true);
                        currentRewardStandingElement.transform.GetChild(1).GetComponent<Text>().text = Mod.traderStatuses[reward.traderIndex].name;
                        currentRewardStandingElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.standing;
                        break;
                    case TraderTaskReward.TaskRewardType.Experience:
                        GameObject currentRewardExperienceElement = Instantiate(currentRewardHorizontal.GetChild(1).gameObject, currentRewardHorizontal);
                        currentRewardExperienceElement.SetActive(true);
                        currentRewardExperienceElement.transform.GetChild(0).GetComponent<Image>().sprite = EFM_Base_Manager.experienceSprite;
                        currentRewardExperienceElement.transform.GetChild(2).GetComponent<Text>().text = (reward.standing > 0 ? "+" : "-") + reward.experience;
                        break;
                    case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                        GameObject currentRewardAssortElement = Instantiate(currentRewardHorizontal.GetChild(0).gameObject, currentRewardHorizontal);
                        currentRewardAssortElement.SetActive(true);
                        if (Mod.itemIcons.ContainsKey(reward.itemID))
                        {
                            currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[reward.itemID];
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(reward.itemID, currentRewardAssortElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        currentRewardAssortElement.transform.GetChild(0).GetChild(1).gameObject.SetActive(false);
                        currentRewardAssortElement.transform.GetChild(2).GetComponent<Text>().text = Mod.itemNames[reward.itemID];

                        // Setup ItemIcon
                        EFM_ItemIcon assortIconScript = currentRewardAssortElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                        assortIconScript.itemID = reward.itemID;
                        assortIconScript.itemName = Mod.itemNames[reward.itemID];
                        assortIconScript.description = Mod.itemDescriptions[reward.itemID];
                        assortIconScript.weight = Mod.itemWeights[reward.itemID];
                        assortIconScript.volume = Mod.itemVolumes[reward.itemID];
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

            UpdateTaskListHeight();
        }

        public void OnTaskStartClick(TraderTask task)
        {
            // Set state of task to active
            task.taskState = TraderTask.TaskState.Active;

            // Add task to active task list of player status
            Mod.playerStatusManager.AddTask(task);

            // Update market task list by making the shortinfo of the referenced task UI element in TraderTask to show that it is active
            task.marketListElement.transform.GetChild(0).GetChild(2).gameObject.SetActive(true);
            task.marketListElement.transform.GetChild(0).GetChild(3).gameObject.SetActive(false);
            task.marketListElement.transform.GetChild(0).GetChild(5).gameObject.SetActive(true);
            task.marketListElement.transform.GetChild(0).GetChild(6).gameObject.SetActive(false);

            // Update conditions that are dependent on this task being started, then update everything depending on those conditions
            if (EFM_TraderStatus.questConditionsByTask.ContainsKey(task.ID))
            {
                foreach (TraderTaskCondition taskCondition in EFM_TraderStatus.questConditionsByTask[task.ID])
                {
                    // If the condition requires this task to be started
                    if(taskCondition.value == 2)
                    {
                        EFM_TraderStatus.FulfillCondition(taskCondition);
                    }
                }
            }

            // Spawn intial equipment 
            if(task.startingEquipment != null)
            {
                GivePlayerRewards(task.startingEquipment);
            }
        }

        public void GivePlayerRewards(List<TraderTaskReward> rewards, string taskName = null)
        {
            bool resetTrader = false;
            foreach(TraderTaskReward reward in rewards)
            {
                switch(reward.taskRewardType)
                {
                    case TraderTaskReward.TaskRewardType.AssortmentUnlock:
                        Mod.traderStatuses[currentTraderIndex].itemsToWaitForUnlock.Remove(reward.itemID);
                        resetTrader = true;
                        break;
                    case TraderTaskReward.TaskRewardType.TraderUnlock:
                        Mod.traderStatuses[reward.traderIndex].unlocked = true;
                        Transform traderImageTransform = transform.GetChild(1).GetChild(24).GetChild(0).GetChild(0).GetChild(0).GetChild(reward.traderIndex);
                        traderImageTransform.GetComponent<Collider>().enabled = true;
                        traderImageTransform.GetChild(2).gameObject.SetActive(false);
                        break;
                    case TraderTaskReward.TaskRewardType.TraderStanding:
                        Mod.traderStatuses[reward.traderIndex].standing += reward.standing;
                        resetTrader = true;
                        break;
                    case TraderTaskReward.TaskRewardType.Item:
                        SpawnItem(reward.itemID, reward.amount);
                        break;
                    case TraderTaskReward.TaskRewardType.Experience:
                        Mod.AddExperience(reward.experience, 3, taskName == null ? "Gained {0} exp. (Task completion)" : "Task \""+taskName+"\" completed! Gained {0} exp.");
                        break;
                }
            }
            if (resetTrader)
            {
                SetTrader(currentTraderIndex);
            }
        }

        public void SpawnItem(string itemID, int amount)
        {
            int amountToSpawn = amount;
            if (int.TryParse(cartItem, out int parseResult))
            {
                GameObject itemPrefab = Mod.itemPrefabs[parseResult];
                EFM_CustomItemWrapper prefabCIW = itemPrefab.GetComponent<EFM_CustomItemWrapper>();
                Transform tradeVolume = this.tradeVolume.transform.GetChild(0);
                List<GameObject> objectsList = new List<GameObject>();
                while (amountToSpawn > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPrefab, this.tradeVolume.itemsRoot);
                    objectsList.Add(spawnedItem);
                    float xSize = tradeVolume.localScale.x;
                    float ySize = tradeVolume.localScale.y;
                    float zSize = tradeVolume.localScale.z;
                    spawnedItem.transform.localPosition = new Vector3(UnityEngine.Random.Range(-xSize / 2, xSize / 2),
                                                                      UnityEngine.Random.Range(-ySize / 2, ySize / 2),
                                                                      UnityEngine.Random.Range(-zSize / 2, zSize / 2));
                    spawnedItem.transform.localRotation = UnityEngine.Random.rotation;

                    // Setup CIW
                    EFM_CustomItemWrapper itemCIW = spawnedItem.GetComponent<EFM_CustomItemWrapper>();
                    if (itemCIW.maxAmount > 0)
                    {
                        itemCIW.amount = itemCIW.maxAmount;
                    }

                    if (itemCIW.itemType == Mod.ItemType.AmmoBox)
                    {
                        FVRFireArmMagazine asMagazine = itemCIW.physObj as FVRFireArmMagazine;
                        for (int i = 0; i < itemCIW.maxAmount; ++i)
                        {
                            asMagazine.AddRound(itemCIW.roundClass, false, false);
                        }
                    }

                    // Set stack and remove amount to spawn
                    if (itemCIW.maxStack > 1)
                    {
                        if (amountToSpawn > itemCIW.maxStack)
                        {
                            itemCIW.stack = itemCIW.maxStack;
                            amountToSpawn -= itemCIW.maxStack;
                        }
                        else // amountToSpawn <= itemCIW.maxStack
                        {
                            itemCIW.stack = amountToSpawn;
                            amountToSpawn = 0;
                        }
                    }
                    else
                    {
                        --amountToSpawn;
                    }

                    // Add item to tradevolume so it can set its reset cols and kinematic to true
                    this.tradeVolume.AddItem(itemCIW.physObj);

                    Mod.currentBaseManager.AddToBaseInventory(spawnedItem.transform, true);

                    BeginInteractionPatch.SetItemLocationIndex(1, itemCIW, null, false);
                }
                if (tradeVolumeInventory.ContainsKey(cartItem))
                {
                    tradeVolumeInventory[cartItem] += cartItemCount;
                    tradeVolumeInventoryObjects[cartItem].AddRange(objectsList);
                }
                else
                {
                    tradeVolumeInventory.Add(cartItem, cartItemCount);
                    tradeVolumeInventoryObjects.Add(cartItem, objectsList);
                }

                // Set trader immediately because we spawned a custom item
                SetTrader(currentTraderIndex, cartItem);

                // Update area managers based on item we just added
                foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
                {
                    areaManager.UpdateBasedOnItem(cartItem);
                }
            }
            else
            {
                // Spawn vanilla item will handle the updating of proper elements
                AnvilManager.Run(SpawnVanillaItem(cartItem, amountToSpawn));
            }
        }

        public void OnTaskFinishClick(TraderTask task)
        {
            // Set state of task to success
            task.taskState = TraderTask.TaskState.Success;

            // Remove task from active task list of player status
            if(task.statusListElement != null)
            {
                Destroy(task.statusListElement);
                task.statusListElement = null;
            }

            // Remove from trader task list if exists
            if (task.marketListElement != null)
            {
                Destroy(task.marketListElement);
                task.marketListElement = null;
            }

            // Update conditions that are dependent on this task being successfully completed, then update everything depending on those conditions
            if (EFM_TraderStatus.questConditionsByTask.ContainsKey(task.ID))
            {
                foreach (TraderTaskCondition taskCondition in EFM_TraderStatus.questConditionsByTask[task.ID])
                {
                    // If the condition requires this task to be successfully completed
                    if (taskCondition.value == 4)
                    {
                        EFM_TraderStatus.FulfillCondition(taskCondition);
                    }
                }
            }

            // Spawn completion rewards
            if (task.successRewards != null)
            {
                GivePlayerRewards(task.successRewards);
            }
        }

        public void OnRagFairCategoryMainClick(GameObject category, string ID)
        {
            // Visually deactivate any other previously active category and activate new one. Or just return if this is already the active category
            if (currentActiveCategory != null)
            {
                if (category.Equals(currentActiveCategory))
                {
                    return;
                }
                else
                {
                    currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.28125f, 0.28125f, 0.28125f);
                }
            }
            if(currentActiveItemSelector != null)
            {
                currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                currentActiveItemSelector = null;
            }
            currentActiveCategory = category;
            currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.8203125f, 0.8203125f, 0.8203125f);

            // Reset item list
            ResetRagFairItemList();

            // Add all items of that category to the list
            Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
            Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);
            GameObject itemTemplate = listParent.GetChild(0).gameObject;
            if (Mod.itemsByParents.ContainsKey(ID))
            {
                foreach (string itemID in Mod.itemsByParents[ID])
                {
                    AssortmentItem[] assortItems = GetTraderItemSell(itemID);

                    for (int i = 0; i < assortItems.Length; ++i)
                    {
                        int traderIndex = i;
                        if (assortItems[traderIndex] != null)
                        {
                            // Make an entry for each price of this assort item
                            foreach (List<AssortmentPriceData> priceList in assortItems[traderIndex].prices)
                            {
                                GameObject itemElement = Instantiate(itemTemplate, listParent);
                                itemElement.SetActive(true);
                                Sprite itemIcon = null;
                                string itemName = Mod.itemNames[itemID];
                                Image imageElement = itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>();
                                if (Mod.itemIcons.ContainsKey(itemID))
                                {
                                    itemIcon = Mod.itemIcons[itemID];
                                    imageElement.sprite = itemIcon;
                                }
                                else
                                {
                                    AnvilManager.Run(Mod.SetVanillaIcon(itemID, imageElement));
                                }
                                itemElement.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemBuyClick(traderIndex, assortItems[traderIndex], priceList, itemIcon); });
                                itemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = assortItems[traderIndex].stack.ToString();
                                itemElement.transform.GetChild(1).GetComponent<Text>().text = itemName;
                                itemElement.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemWishClick(itemID); });

                                // Set price icon and label
                                int currencyIndex = -1; // Rouble, Dollar, Euro, Barter
                                Sprite priceLabelSprite = EFM_Base_Manager.roubleCurrencySprite;
                                int totalPriceCount = 0;
                                foreach(AssortmentPriceData price in priceList)
                                {
                                    totalPriceCount += price.count;
                                    switch (price.ID)
                                    {
                                        case "201":
                                            if (currencyIndex == -1)
                                            {
                                                currencyIndex = 1;
                                                priceLabelSprite = EFM_Base_Manager.dollarCurrencySprite;
                                            }
                                            else if (currencyIndex != 1)
                                            {
                                                currencyIndex = 3;
                                                priceLabelSprite = EFM_Base_Manager.barterSprite;
                                            }
                                            break;
                                        case "202":
                                            if (currencyIndex == -1)
                                            {
                                                currencyIndex = 2;
                                                priceLabelSprite = EFM_Base_Manager.euroCurrencySprite;
                                            }
                                            else if (currencyIndex != 2)
                                            {
                                                currencyIndex = 3;
                                                priceLabelSprite = EFM_Base_Manager.barterSprite;
                                            }
                                            break;
                                        case "203":
                                            if (currencyIndex == -1)
                                            {
                                                currencyIndex = 0;
                                                priceLabelSprite = EFM_Base_Manager.roubleCurrencySprite;
                                            }
                                            else if (currencyIndex != 0)
                                            {
                                                currencyIndex = 3;
                                                priceLabelSprite = EFM_Base_Manager.barterSprite;
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                itemElement.transform.GetChild(0).GetChild(2).GetChild(0).GetComponent<Image>().sprite = priceLabelSprite;
                                itemElement.transform.GetChild(0).GetChild(2).GetChild(1).GetComponent<Text>().text = totalPriceCount.ToString();

                                // Setup itemIcon
                                EFM_ItemIcon currentItemIconScript = itemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                                currentItemIconScript.isPhysical = false;
                                currentItemIconScript.itemID = itemID;
                                currentItemIconScript.itemName = itemName;
                                currentItemIconScript.description = Mod.itemDescriptions[itemID];
                                currentItemIconScript.weight = Mod.itemWeights[itemID];
                                currentItemIconScript.volume = Mod.itemVolumes[itemID];

                                if (ragFairItemBuyViewsByID.ContainsKey(itemID))
                                {
                                    ragFairItemBuyViewsByID[itemID].Add(itemElement);
                                }
                                else
                                {
                                    ragFairItemBuyViewsByID.Add(itemID, new List<GameObject>() { itemElement });
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                Mod.instance.LogError("category does not have children, does not exist in Mod.itemsByParents keys");
            }

            // Open category (set active sub container)
            category.transform.GetChild(1).gameObject.SetActive(true);

            // Set toggle button icon to open
            Transform toggle = category.transform.GetChild(0).GetChild(0);
            toggle.GetChild(0).gameObject.SetActive(false);
            toggle.GetChild(1).gameObject.SetActive(true);

            // Update category and item lists hoverscrolls
            UpdateRagfairBuyCategoriesHoverscrolls();
            UpdateRagfairBuyItemsHoverscrolls();
        }

        private void UpdateRagfairBuyCategoriesHoverscrolls()
        {
            Transform listParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            float categoriesHeight = 3; // Top padding
            for (int i = 1; i < listParent.childCount - 1; ++i)
            {
                categoriesHeight += (3 + 12 * CountCategories(listParent.GetChild(i)));
            }
            EFM_HoverScroll newBuyCategoriesDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll newBuyCategoriesUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            if (categoriesHeight > 186)
            {
                newBuyCategoriesUpHoverScroll.rate = 186 / (categoriesHeight - 186);
                newBuyCategoriesDownHoverScroll.rate = 186 / (categoriesHeight - 186);
                newBuyCategoriesDownHoverScroll.gameObject.SetActive(true);
                newBuyCategoriesUpHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                newBuyCategoriesDownHoverScroll.gameObject.SetActive(false);
                newBuyCategoriesUpHoverScroll.gameObject.SetActive(false);
            }
        }

        private void UpdateRagfairBuyItemsHoverscrolls()
        {
            Transform listParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            float itemsHeight = 3 + 34 * (listParent.childCount - 1);
            EFM_HoverScroll buyItemsDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll buyItemsUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            if (itemsHeight > 186)
            {
                buyItemsUpHoverScroll.rate = 186 / (itemsHeight - 186);
                buyItemsDownHoverScroll.rate = 186 / (itemsHeight - 186);
                buyItemsDownHoverScroll.gameObject.SetActive(true);
                buyItemsUpHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                buyItemsDownHoverScroll.gameObject.SetActive(false);
                buyItemsUpHoverScroll.gameObject.SetActive(false);
            }
        }

        private int CountCategories(Transform categoryTransform)
        {
            int count = 1;
            if (categoryTransform.GetChild(1).gameObject.activeSelf)
            {
                foreach (Transform sub in categoryTransform.GetChild(1))
                {
                    count += CountCategories(sub);
                }
            }
            return count;
        }

        private void ResetRagFairItemList()
        {
            Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
            Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);

            // Clear list
            while(listParent.childCount > 1)
            {
                Transform currentFirstChild = listParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }

            // Deactivate hover scrolls
            listTransform.GetChild(2).gameObject.SetActive(false);
            listTransform.GetChild(3).gameObject.SetActive(false);
        }

        public void OnRagFairItemMainClick(GameObject selector, string ID)
        {
            if (currentActiveItemSelector != null)
            {
                if (selector.Equals(currentActiveItemSelector))
                {
                    return;
                }
                else
                {
                    currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
                }
            }
            if (currentActiveCategory != null)
            {
                currentActiveCategory.transform.GetChild(0).GetComponent<Image>().color = new Color(0.28125f, 0.28125f, 0.28125f);
                currentActiveCategory = null;
            }
            currentActiveItemSelector = selector;
            currentActiveItemSelector.transform.GetChild(0).GetComponent<Image>().color = new Color(0.8203125f, 0.8203125f, 0.8203125f);

            // Reset item list
            ResetRagFairItemList();

            // Add all items of that category to the list
            Transform listTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(1).GetChild(1);
            Transform listParent = listTransform.GetChild(0).GetChild(0).GetChild(0);
            GameObject itemTemplate = listParent.GetChild(0).gameObject;
            AssortmentItem[] assortItems = GetTraderItemSell(ID);

            for (int i = 0; i < assortItems.Length; ++i)
            {
                int traderIndex = i;
                if (assortItems[i] != null)
                {
                    // Make an entry for each price of this assort item
                    foreach (List<AssortmentPriceData> priceList in assortItems[i].prices)
                    {
                        GameObject itemElement = Instantiate(itemTemplate, listParent);
                        itemElement.SetActive(true);
                        Sprite itemIcon = null;
                        string itemName = Mod.itemNames[ID];
                        if (Mod.itemIcons.ContainsKey(ID))
                        {
                            itemIcon = Mod.itemIcons[ID];
                            itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = itemIcon;
                        }
                        else
                        {
                            AnvilManager.Run(Mod.SetVanillaIcon(ID, itemElement.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
                        }
                        itemElement.transform.GetChild(0).GetChild(1).GetComponent<Text>().text = assortItems[i].stack.ToString();
                        itemElement.transform.GetChild(1).GetComponent<Text>().text = itemName;
                        itemElement.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemBuyClick(traderIndex, assortItems[traderIndex], priceList, itemIcon); });
                        itemElement.transform.GetChild(3).GetComponent<Button>().onClick.AddListener(() => { OnRagFairBuyItemWishClick(ID); });

                        if (ragFairItemBuyViewsByID.ContainsKey(ID))
                        {
                            ragFairItemBuyViewsByID[ID].Add(itemElement);
                        }
                        else
                        {
                            ragFairItemBuyViewsByID.Add(ID, new List<GameObject>() { itemElement });
                        }

                        // Setup itemIcon
                        EFM_ItemIcon currentItemIconScript = itemElement.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
                        currentItemIconScript.isPhysical = false;
                        currentItemIconScript.itemID = ID;
                        currentItemIconScript.itemName = itemName;
                        currentItemIconScript.description = Mod.itemDescriptions[ID];
                        currentItemIconScript.weight = Mod.itemWeights[ID];
                        currentItemIconScript.volume = Mod.itemVolumes[ID];
                    }
                }
            }

            // Update hoverscrolls
            UpdateRagfairBuyItemsHoverscrolls();
        }

        public void OnRagFairCategoryToggleClick(GameObject category)
        {
            Transform toggle = category.transform.GetChild(0).GetChild(0);
            toggle.GetChild(0).gameObject.SetActive(!toggle.GetChild(0).gameObject.activeSelf);
            toggle.GetChild(1).gameObject.SetActive(!toggle.GetChild(1).gameObject.activeSelf);
            category.transform.GetChild(1).gameObject.SetActive(toggle.GetChild(1).gameObject.activeSelf);

            UpdateRagfairBuyCategoriesHoverscrolls();
        }

        public int GetTotalItemSell(string ID)
        {
            // TODO: Once rag fair player simulation is implemented, add up the number of player selling entries 
            int count = 0;

            foreach (EFM_TraderStatus trader in Mod.traderStatuses)
            {
                int level = trader.GetLoyaltyLevel();
                TraderAssortment currentAssort = trader.assortmentByLevel[level];
                if (currentAssort.itemsByID.ContainsKey(ID))
                {
                    ++count;
                }
            }

            return count;
        }

        public AssortmentItem[] GetTraderItemSell(string ID)
        {
            // TODO: Once rag fair player simulation is implemented, add up the number of player selling entries 
            AssortmentItem[] itemAssortments = new AssortmentItem[8];

            foreach(EFM_TraderStatus trader in Mod.traderStatuses)
            {
                int level = trader.GetLoyaltyLevel();
                TraderAssortment currentAssort = trader.assortmentByLevel[level];
                if (currentAssort.itemsByID.ContainsKey(ID))
                {
                    itemAssortments[trader.index] = currentAssort.itemsByID[ID];
                }
            }

            return itemAssortments;
        }

        public void OnRagFairWishlistItemWishClick(GameObject UIElement, string ID)
        {
            // Destroy wishlist element
            UIElement.transform.SetParent(null);
            Destroy(UIElement);

            // Update wishlist hover scrolls
            UpdateRagFairWishlistHoverscrolls();

            // Disable star of buy item view
            if (ragFairItemBuyViewsByID.ContainsKey(ID))
            {
                foreach(GameObject buyItemView in ragFairItemBuyViewsByID[ID])
                {
                    buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = Color.black;
                }
            }

            // Remove from wishlist logic
            wishListItemViewsByID.Remove(ID);
            Mod.wishList.Remove(ID);
        }

        public void OnRagFairBuyItemBuyClick(int traderIndex, AssortmentItem item, List<AssortmentPriceData> priceList, Sprite itemIcon)
        {
            Mod.instance.LogInfo("OnRagFairBuyItemBuyClick called on item: "+item.ID);
            // Set rag fair cart item, icon, amount, name
            ragfairCartItem = item.ID;
            ragfairCartItemCount = 1;
            ragfairPrices = priceList;

            Mod.instance.LogInfo("0");
            Transform ragfairCartTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2);
            string itemName = Mod.itemNames[item.ID];
            ragfairCartTransform.GetChild(1).GetChild(0).GetComponent<Text>().text = itemName;
            Mod.instance.LogInfo("0");
            if (itemIcon == null)
            {
                AnvilManager.Run(Mod.SetVanillaIcon(item.ID, ragfairCartTransform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>()));
            }
            else
            {
                ragfairCartTransform.GetChild(1).GetChild(1).GetChild(0).GetChild(2).GetComponent<Image>().sprite = itemIcon;
            }
            Mod.instance.LogInfo("0");
            ragfairCartTransform.GetChild(1).GetChild(1).GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = "1";

            // Setup selected item ItemIcon
            EFM_ItemIcon ragfairCartItemIconScript = ragfairCartTransform.GetChild(1).GetChild(1).GetComponent<EFM_ItemIcon>();
            if(ragfairCartItemIconScript == null)
            { 
                ragfairCartItemIconScript = ragfairCartTransform.GetChild(1).GetChild(1).gameObject.AddComponent<EFM_ItemIcon>();
            }
            ragfairCartItemIconScript.itemID = item.ID;
            ragfairCartItemIconScript.itemName = itemName;
            ragfairCartItemIconScript.description = Mod.itemDescriptions[item.ID];
            ragfairCartItemIconScript.weight = Mod.itemWeights[item.ID];
            ragfairCartItemIconScript.volume = Mod.itemVolumes[item.ID];

            Mod.instance.LogInfo("0");
            Transform cartShowcase = ragfairCartTransform.GetChild(1);
            Transform pricesParent = cartShowcase.GetChild(3).GetChild(0).GetChild(0);
            GameObject priceTemplate = pricesParent.GetChild(0).gameObject;
            float priceHeight = 0;
            Mod.instance.LogInfo("0");
            while (pricesParent.childCount > 1)
            {
                Transform currentFirstChild = pricesParent.GetChild(1);
                currentFirstChild.SetParent(null);
                Destroy(currentFirstChild.gameObject);
            }
            bool canDeal = true;
            Mod.instance.LogInfo("0");
            if (ragfairBuyPriceElements == null)
            {
                ragfairBuyPriceElements = new List<GameObject>();
            }
            else
            {
                ragfairBuyPriceElements.Clear();
            }
            foreach (AssortmentPriceData price in priceList)
            {
                Mod.instance.LogInfo("\t0");
                priceHeight += 50;
                Transform priceElement = Instantiate(priceTemplate, pricesParent).transform;
                priceElement.gameObject.SetActive(true);

                Mod.instance.LogInfo("\t0");
                if (Mod.itemIcons.ContainsKey(price.ID))
                {
                    priceElement.GetChild(0).GetChild(2).GetComponent<Image>().sprite = Mod.itemIcons[price.ID];
                }
                else
                {
                    AnvilManager.Run(Mod.SetVanillaIcon(price.ID, priceElement.GetChild(0).GetChild(2).GetComponent<Image>()));
                }
                Mod.instance.LogInfo("\t0");
                priceElement.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = price.count.ToString();
                string priceItemName = Mod.itemNames[price.ID];
                priceElement.GetChild(3).GetChild(0).GetComponent<Text>().text = priceItemName;
                Mod.instance.LogInfo("\t0");
                if (price.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                {
                    priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).gameObject.SetActive(true);
                    priceElement.GetChild(0).GetChild(3).GetChild(7).GetChild(2).GetComponent<Text>().text = ">= lvl " + price.dogtagLevel;
                }
                ragfairBuyPriceElements.Add(priceElement.gameObject);

                Mod.instance.LogInfo("\t0");
                if (tradeVolumeInventory.ContainsKey(price.ID) && tradeVolumeInventory[price.ID] >= price.count)
                {
                    priceElement.GetChild(2).GetChild(0).gameObject.SetActive(true);
                    priceElement.GetChild(2).GetChild(1).gameObject.SetActive(false);
                }
                else
                {
                    canDeal = false;
                }

                // Setup price ItemIcon
                EFM_ItemIcon ragfairCartPriceItemIconScript = priceElement.GetChild(2).gameObject.AddComponent<EFM_ItemIcon>();
                ragfairCartPriceItemIconScript.itemID = price.ID;
                ragfairCartPriceItemIconScript.itemName = priceItemName;
                ragfairCartPriceItemIconScript.description = Mod.itemDescriptions[price.ID];
                ragfairCartPriceItemIconScript.weight = Mod.itemWeights[price.ID];
                ragfairCartPriceItemIconScript.volume = Mod.itemVolumes[price.ID];
            }
            Mod.instance.LogInfo("0");
            EFM_HoverScroll downHoverScroll = cartShowcase.GetChild(3).GetChild(3).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll upHoverScroll = cartShowcase.GetChild(3).GetChild(2).GetComponent<EFM_HoverScroll>();
            Mod.instance.LogInfo("0");
            if (priceHeight > 100)
            {
                downHoverScroll.rate = 100 / (priceHeight - 100);
                upHoverScroll.rate = 100 / (priceHeight - 100);
                downHoverScroll.gameObject.SetActive(true);
                upHoverScroll.gameObject.SetActive(false);
            }
            else
            {
                downHoverScroll.gameObject.SetActive(false);
                upHoverScroll.gameObject.SetActive(false);
            }
            Mod.instance.LogInfo("0");

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
            Mod.instance.LogInfo("0");

            // Set ragfair buy deal button 
            EFM_PointableButton ragfairCartDealAmountButton = ragfairCartTransform.transform.GetChild(2).GetChild(0).GetChild(0).GetComponent<EFM_PointableButton>();
            ragfairCartDealAmountButton.Button.onClick.AddListener(() => { OnRagfairBuyDealClick(traderIndex); });

            Mod.instance.LogInfo("0");
            // Deactivate ragfair buy categories and item list, enable cart
            ragfairCartTransform.gameObject.SetActive(true);
            ragfairCartTransform.parent.GetChild(0).gameObject.SetActive(false);
            ragfairCartTransform.parent.GetChild(1).gameObject.SetActive(false);
        }

        public void OnRagFairBuyItemWishClick(string ID)
        {
            if (Mod.wishList.Contains(ID))
            {
                // Destroy wishlist element
                wishListItemViewsByID[ID].transform.SetParent(null);
                Destroy(wishListItemViewsByID[ID]);

                // Update wishlist hover scrolls
                UpdateRagFairWishlistHoverscrolls();

                // Disable star of buy item views
                if (ragFairItemBuyViewsByID.ContainsKey(ID))
                {
                    foreach (GameObject buyItemView in ragFairItemBuyViewsByID[ID])
                    {
                        buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = Color.black;
                    }
                }

                // Remove from wishlist logic
                wishListItemViewsByID.Remove(ID);
                Mod.wishList.Remove(ID);
            }
            else
            {
                // Add wishlist UI entry, also updates hoverscrolls
                AddItemToWishlist(ID);

                // Enable star of buy item views
                if (ragFairItemBuyViewsByID.ContainsKey(ID))
                {
                    foreach (GameObject buyItemView in ragFairItemBuyViewsByID[ID])
                    {
                        buyItemView.transform.GetChild(3).GetChild(0).GetComponent<Image>().color = new Color(1, 0.84706f, 0); ;
                    }
                }

                // Add to wishlist logic
                Mod.wishList.Add(ID);
            }
        }

        public void AddItemToWishlist(string ID)
        {
            Transform wishlistParent = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(0).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject wishlistItemViewTemplate = wishlistParent.GetChild(0).gameObject;
            GameObject wishlistItemView = Instantiate(wishlistItemViewTemplate, wishlistParent);
            wishlistItemView.SetActive(true);

            // Update wishlist hover scrolls
            UpdateRagFairWishlistHoverscrolls();

            string itemName = Mod.itemNames[ID];
            if (Mod.itemIcons.ContainsKey(ID))
            {
                wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = Mod.itemIcons[ID];
            }
            else
            {
                AnvilManager.Run(Mod.SetVanillaIcon(ID, wishlistItemView.transform.GetChild(0).GetChild(0).GetComponent<Image>()));
            }
            wishlistItemView.transform.GetChild(1).GetComponent<Text>().text = itemName;

            wishlistItemView.transform.GetChild(2).GetComponent<Button>().onClick.AddListener(() => { OnRagFairWishlistItemWishClick(wishlistItemView, ID); });

            // Setup itemIcon
            EFM_ItemIcon currentItemIconScript = wishlistItemView.transform.GetChild(0).gameObject.AddComponent<EFM_ItemIcon>();
            currentItemIconScript.isPhysical = false;
            currentItemIconScript.itemID = ID;
            currentItemIconScript.itemName = itemName;
            currentItemIconScript.description = Mod.itemDescriptions[ID];
            currentItemIconScript.weight = Mod.itemWeights[ID];
            currentItemIconScript.volume = Mod.itemVolumes[ID];

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

        private void UpdateRagFairWishlistHoverscrolls()
        {
            EFM_HoverScroll newWishlistDownHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(2).GetComponent<EFM_HoverScroll>();
            EFM_HoverScroll newWishlistUpHoverScroll = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(0).GetChild(1).GetChild(3).GetComponent<EFM_HoverScroll>();
            float wishlistHeight = 3 + 34 * Mod.wishList.Count;
            if (wishlistHeight > 190)
            {
                newWishlistUpHoverScroll.rate = 190 / (wishlistHeight - 190);
                newWishlistDownHoverScroll.rate = 190 / (wishlistHeight - 190);
                newWishlistDownHoverScroll.gameObject.SetActive(true);
            }
            else
            {
                newWishlistDownHoverScroll.gameObject.SetActive(false);
                newWishlistUpHoverScroll.gameObject.SetActive(false);
            }
        }

        public void OnRagfairBuyAmountClick()
        {
            // Cancel stack splitting if in progress
            if (Mod.amountChoiceUIUp || Mod.splittingItem != null)
            {
                    Mod.splittingItem.CancelSplit();
            }

            // Disable buy amount buttons until done choosing amount
            transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3).GetChild(1).GetChild(3).GetChild(1).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;
            transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2).GetChild(1).GetChild(1).GetComponent<Collider>().enabled = false;

            // Start splitting
            Mod.stackSplitUI.SetActive(true);
            Mod.stackSplitUI.transform.localPosition = Mod.rightHand.transform.localPosition + Mod.rightHand.transform.forward * 0.2f;
            Mod.stackSplitUI.transform.localRotation = Quaternion.Euler(0, Mod.rightHand.transform.localRotation.eulerAngles.y, 0);
            amountChoiceStartPosition = Mod.rightHand.transform.localPosition;
            amountChoiceRightVector = Mod.rightHand.transform.right;
            amountChoiceRightVector.y = 0;

            choosingRagfairBuyAmount = true;
            startedChoosingThisFrame = true;

            // Set max buy amount, limit it to 360 otherwise scale is too small and it is hard to specify a exact value
            maxBuyAmount = Mathf.Min(360, Mod.traderStatuses[currentTraderIndex].assortmentByLevel[Mod.traderStatuses[currentTraderIndex].GetLoyaltyLevel()].itemsByID[cartItem].stack);
        }

        public void OnRagfairBuyDealClick(int traderIndex)
        {
            // Remove price from trade volume
            foreach (AssortmentPriceData price in ragfairPrices)
            {
                int amountToRemove = price.count;
                tradeVolumeInventory[price.ID] -= (price.count * ragfairCartItemCount);
                List<GameObject> objectList = tradeVolumeInventoryObjects[price.ID];
                while (amountToRemove > 0)
                {
                    GameObject currentItemObject = objectList[objectList.Count - 1];
                    EFM_CustomItemWrapper CIW = currentItemObject.GetComponent<EFM_CustomItemWrapper>();
                    if (CIW != null)
                    {
                        if (CIW.maxStack > 1)
                        {
                            int stack = CIW.stack;
                            if (stack - amountToRemove <= 0)
                            {
                                amountToRemove -= stack;

                                // Destroy item
                                objectList.RemoveAt(objectList.Count - 1);
                                Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                CIW.destroyed = true;
                                currentItemObject.transform.parent = null;
                                Destroy(currentItemObject);
                            }
                            else // stack - amountToRemove > 0
                            {
                                CIW.stack -= amountToRemove;
                                Mod.baseInventory[CIW.ID] -= amountToRemove;
                                amountToRemove = 0;
                            }
                        }
                        else // Doesnt have stack, its a single item
                        {
                            if (CIW.itemType == Mod.ItemType.DogTag)
                            {
                                if (CIW.dogtagLevel >= price.dogtagLevel)
                                {
                                    --amountToRemove;

                                    // Destroy item
                                    objectList.RemoveAt(objectList.Count - 1);
                                    Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                    CIW.destroyed = true;
                                    currentItemObject.transform.parent = null;
                                    Destroy(currentItemObject);
                                }
                            }
                            else
                            {
                                --amountToRemove;

                                // Destroy item
                                objectList.RemoveAt(objectList.Count - 1);
                                Mod.baseInventory[CIW.ID] -= 1;
                                Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                                CIW.destroyed = true;
                                currentItemObject.transform.parent = null;
                                Destroy(currentItemObject);
                            }
                        }
                    }
                    else // Vanilla item cannot have stack
                    {
                        --amountToRemove;

                        // Destroy item
                        objectList.RemoveAt(objectList.Count - 1);
                        Mod.currentBaseManager.RemoveFromBaseInventory(currentItemObject.transform, true);
                        currentItemObject.GetComponent<EFM_VanillaItemDescriptor>().destroyed = true;
                        currentItemObject.transform.parent = null;
                        Destroy(currentItemObject);
                    }
                }

                // Update area managers based on item we just removed
                foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
                {
                    areaManager.UpdateBasedOnItem(price.ID);
                }
            }

            // Add bought amount of item to trade volume at random pos and rot within it
            SpawnItem(ragfairCartItem, ragfairCartItemCount);

            // Update amount of item in trader's assort
            Mod.traderStatuses[traderIndex].assortmentByLevel[Mod.traderStatuses[traderIndex].GetLoyaltyLevel()].itemsByID[ragfairCartItem].stack -= ragfairCartItemCount;
        }

        public void OnRagfairBuyCancelClick()
        {
            // Deactivate ragfair buy categories and item list, enable cart
            Transform ragfairCartTransform = transform.GetChild(0).GetChild(2).GetChild(0).GetChild(3).GetChild(0).GetChild(2).GetChild(2);
            ragfairCartTransform.gameObject.SetActive(false);
            ragfairCartTransform.parent.GetChild(0).gameObject.SetActive(true);
            ragfairCartTransform.parent.GetChild(1).gameObject.SetActive(true);
        }
    }
}
