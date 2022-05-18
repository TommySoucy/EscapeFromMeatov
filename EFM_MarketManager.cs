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
            // TODO
            EFM_TraderStatus trader = baseManager.traderStatuses[index];
            Transform traderDisplay = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(3);

            EFM_TraderStatus.TraderLoyaltyDetails loyaltyDetails = trader.GetLoyaltyDetails();

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
            List<Transform> currentHorizontals = new List<Transform>();
            Transform horizontalsParent = traderDisplay.GetChild(1).GetChild(3).GetChild(0).GetChild(1).GetChild(0).GetChild(0).GetChild(0);
            GameObject horizontalCopy = horizontalsParent.GetChild(0).gameObject;
            // Clear previous horizontals
            while (horizontalsParent.childCount > 1)
            {
               Destroy(horizontalsParent.GetChild(1));
            }
            // Add all assort items to showcase
            for(int i = 1; i <= loyaltyDetails.currentLevel; ++i)
            {
                TraderAssortment assort = trader.assortmentByLevel[i];

                foreach(KeyValuePair<string, AssortmentItem> item in assort.itemsByID)
                {
                    if(item.Value.currentShowcaseElements == null)
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
                        Transform currentHorizontal = currentHorizontals[currentHorizontals.Count - 1];
                        if (currentHorizontals[currentHorizontals.Count - 1].childCount == 7)
                        {
                            currentHorizontal = GameObject.Instantiate(horizontalCopy, horizontalsParent).transform;
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
                        foreach(KeyValuePair<string, int> currentPrice in priceList)
                        {
                            totalPriceCount += currentPrice.Value;
                            if (!barterSprite)
                            {
                                if(currentPrice.Key.Equals("201"))
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
                        currentItemIcon.GetChild(3).GetChild(5).GetChild(1).GetComponent<Text>().text = "totalPriceCount";

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

            // Sell
            // TODO
            // Tasks
            // TODO
            // Insure
            // TODO

            // TODO: Add all necessary hover scrolls
            // TODO: Setup tabs with functionality to make sure the corerct one overlaps 
        }

        public void UpdateBasedOnItem(bool added, EFM_CustomItemWrapper CIW, EFM_VanillaItemDescriptor VID)
        {
            if (added)
            {
                // TODO: IN BUY, check if item corresponds to price, update fulfilled icon and activate deal! button if necessary
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
    }
}
