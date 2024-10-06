using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class Trader
    {
        // Static data
        public string ID;
        public int index;
        public string name;
        public int currency; // 0: RUB, 1: USD, 2: EUR
        public bool defaultUnlocked;
        public bool insuranceAvailable;
        public string[] insuranceExcluded;
        public float insuranceRate;
        public int insuranceMinReturnTime; // Hours
        public int insuranceMaxReturnTime; // Hours
        public int insuranceMaxStorageTime; // Hours
        public bool repairAvailable;
        public int repairCurrency;  // 0: RUB, 1: USD, 2: EUR
        public float repairCurrencyCoef;
        public string[] repairExcluded;
        public float repairPriceRate;
        public float repairQuality;
        public int defaultBalance;
        public string[] buyCategories;
        public string[] buyBlacklist;
        public LoyaltyLevel[] levels;
        public Dictionary<int, List<Barter>> bartersByLevel;
        public Dictionary<string, List<Barter>> bartersByItemID;
        public List<Task> tasks;
        public Dictionary<string, bool> rewardBarters; // Bool is whether barters for this item are unlocked

        // Live data
        private int _level;
        public int level
        {
            get { return _level; }
            set
            {
                int preLevel = _level;
                _level = value;
                if(preLevel != _level)
                {
                    OnTraderLevelChangedInvoke(this);
                }
            }
        }
        private float _standing;
        public float standing
        {
            get { return _standing; }
            set
            {
                float preStanding = _standing;
                _standing = value;
                if(preStanding != _standing)
                {
                    UpdateLevel();
                    OnTraderStandingChangedInvoke();
                }
            }
        }
        public float standingToRestore;
        private int _salesSum;
        public int salesSum
        {
            get { return _salesSum; }
            set
            {
                int preSalesSum = _salesSum;
                _salesSum = value;
                if(preSalesSum != _salesSum)
                {
                    UpdateLevel();
                    OnTraderSalesSumChangedInvoke();
                }
            }
        }
        public bool unlocked;
        public int balance;

        public delegate void OnTraderLevelChangedDelegate(Trader trader);
        public event OnTraderLevelChangedDelegate OnTraderLevelChanged;

        public delegate void OnTraderStandingChangedDelegate();
        public event OnTraderStandingChangedDelegate OnTraderStandingChanged;

        public delegate void OnTraderSalesSumChangedDelegate();
        public event OnTraderSalesSumChangedDelegate OnTraderSalesSumChanged;

        public Trader(int index, string ID)
        {
            this.index = index;
            this.ID = ID;

            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;

            tasks = new List<Task>();
            rewardBarters = new Dictionary<string, bool>();

            name = Mod.localeDB[ID + " Nickname"].ToString();
            string currencyString = Mod.traderBaseDB[index]["currency"].ToString();
            switch (currencyString)
            {
                case "RUB":
                    currency = 0;
                    break;
                case "USD":
                    currency = 1;
                    break;
                case "EUR":
                    currency= 2;
                    break;
                default:
                    Mod.LogError("DEV: Trader has unhandled currency string: " + currencyString);
                    currency = 0;
                    break;
            }
            defaultUnlocked = (bool)Mod.traderBaseDB[index]["unlockedByDefault"];
            insuranceAvailable = Mod.traderBaseDB[index]["insurance"] == null ? false : (bool)Mod.traderBaseDB[index]["insurance"]["availability"];
            if (insuranceAvailable)
            {
                JArray insuranceExcludedArray = Mod.traderBaseDB[index]["insurance"]["excluded_category"] as JArray;
                if (insuranceExcludedArray != null)
                {
                    insuranceExcluded = insuranceExcludedArray.ToObject<string[]>();
                }
                insuranceMinReturnTime = (int)Mod.traderBaseDB[index]["insurance"]["min_return_hour"];
                insuranceMaxReturnTime = (int)Mod.traderBaseDB[index]["insurance"]["max_return_hour"];
                insuranceMaxStorageTime = (int)Mod.traderBaseDB[index]["insurance"]["max_storage_time"];
                insuranceRate = (int)Mod.traderBaseDB[index]["insurance"]["min_payment"];
            }
            repairAvailable = Mod.traderBaseDB[index]["repair"] == null ? false : (bool)Mod.traderBaseDB[index]["repair"]["availability"];
            if (repairAvailable)
            {
                Mod.ItemIDToCurrencyIndex(Mod.traderBaseDB[index]["repair"]["currency"].ToString(), out repairCurrency);
                repairCurrencyCoef = (int)Mod.traderBaseDB[index]["repair"]["currency_coefficient"];
                List<string> tempRepairExcludedList = new List<string>();
                JArray repairExcludedArray = Mod.traderBaseDB[index]["repair"]["excluded_category"] as JArray;
                if (repairExcludedArray != null)
                {
                    for (int i = 0; i < repairExcludedArray.Count; ++i)
                    {
                        tempRepairExcludedList.Add(repairExcludedArray[i].ToString());
                    }
                }
                JArray repairExcludedIDsArray = Mod.traderBaseDB[index]["repair"]["excluded_category"] as JArray;
                if (repairExcludedIDsArray != null)
                {
                    for (int i = 0; i < repairExcludedIDsArray.Count; ++i)
                    {
                        tempRepairExcludedList.Add(repairExcludedIDsArray[i].ToString());
                    }
                }
                repairExcluded = tempRepairExcludedList.ToArray();
                repairPriceRate = (float)Mod.traderBaseDB[index]["repair"]["price_rate"];
                repairQuality = (float)Mod.traderBaseDB[index]["repair"]["quality"];
            }
            switch (currency)
            {
                case 0:
                    defaultBalance = (int)Mod.traderBaseDB[index]["balance_rub"];
                    break;
                case 1:
                    defaultBalance = (int)Mod.traderBaseDB[index]["balance_dol"];
                    break;
                case 2:
                    defaultBalance = (int)Mod.traderBaseDB[index]["balance_eur"];
                    break;
                default:
                    Mod.LogError("DEV: Trader "+index+" has currency index: "+currency);
                    defaultBalance = (int)Mod.traderBaseDB[index]["balance_rub"];
                    break;
            }
            List<string> tempBuyCategoriesList = new List<string>();
            JArray buyCategoriesArray = Mod.traderBaseDB[index]["items_buy"]["category"] as JArray;
            if (buyCategoriesArray != null)
            {
                for (int i = 0; i < buyCategoriesArray.Count; ++i)
                {
                    tempBuyCategoriesList.Add(buyCategoriesArray[i].ToString());
                }
            }
            JArray buyCategoriesIDsArray = Mod.traderBaseDB[index]["items_buy"]["id_list"] as JArray;
            if (buyCategoriesIDsArray != null)
            {
                for (int i = 0; i < buyCategoriesIDsArray.Count; ++i)
                {
                    tempBuyCategoriesList.Add(buyCategoriesIDsArray[i].ToString());
                }
            }
            buyCategories = tempBuyCategoriesList.ToArray();
            List<string> tempBuyBlacklist = new List<string>();
            JArray buyBlacklistArray = Mod.traderBaseDB[index]["items_buy_prohibited"]["category"] as JArray;
            if (buyBlacklistArray != null)
            {
                for (int i = 0; i < buyBlacklistArray.Count; ++i)
                {
                    tempBuyBlacklist.Add(buyBlacklistArray[i].ToString());
                }
            }
            JArray buyBlacklistIDsArray = Mod.traderBaseDB[index]["items_buy_prohibited"]["id_list"] as JArray;
            if (buyBlacklistIDsArray != null)
            {
                for (int i = 0; i < buyBlacklistIDsArray.Count; ++i)
                {
                    tempBuyBlacklist.Add(buyBlacklistIDsArray[i].ToString());
                }
            }
            buyBlacklist = tempBuyBlacklist.ToArray();

            List<LoyaltyLevel> tempLevels = new List<LoyaltyLevel>(); 
            JArray levelData = Mod.traderBaseDB[index]["loyaltyLevels"] as JArray;
            for(int i=0; i < levelData.Count; ++i)
            {
                LoyaltyLevel currentLevel = new LoyaltyLevel();
                currentLevel.buyPriceCoef = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["buy_price_coef"];
                currentLevel.exchangePriceCoef = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["exchange_price_coef"];
                currentLevel.healPriceCoef = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["heal_price_coef"];
                currentLevel.insurancePriceCoef = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["insurance_price_coef"];
                currentLevel.minLevel = (int)Mod.traderBaseDB[index]["loyaltyLevels"][i]["minLevel"];
                currentLevel.minSaleSum = (int)Mod.traderBaseDB[index]["loyaltyLevels"][i]["minSalesSum"];
                currentLevel.minStanding = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["minStanding"];
                currentLevel.repairPriceCoef = (float)Mod.traderBaseDB[index]["loyaltyLevels"][i]["repair_price_coef"];
                tempLevels.Add(currentLevel);
            }
            levels = tempLevels.ToArray();

            bartersByLevel = new Dictionary<int, List<Barter>>();
            bartersByItemID = new Dictionary<string, List<Barter>>();
            if (Mod.traderAssortDB[index] != null)
            {
                Dictionary<string, int> barterLevelPerID = Mod.traderAssortDB[index]["loyal_level_items"].ToObject<Dictionary<string, int>>();
                JArray itemsArray = Mod.traderAssortDB[index]["items"] as JArray;
                foreach (KeyValuePair<string, int> barterLevelEntry in barterLevelPerID)
                {
                    string barterItemID = null;
                    List<string> barterItemChildrenIDs = new List<string>();
                    for (int i=0; i< itemsArray.Count; ++i)
                    {
                        if (itemsArray[i]["_id"].ToString().Equals(barterLevelEntry.Key))
                        {
                            barterItemID = itemsArray[i]["_tpl"].ToString();
                            FindAssortChildren(barterItemID, barterItemChildrenIDs, itemsArray);
                            break;
                        }
                    }
                    if(barterItemID == null)
                    {
                        Mod.LogWarning("DEV: Trader "+index+": "+ID+": Couldn't get Item ID for barter with ID: "+ barterLevelEntry.Key);
                        continue;
                    }

                    JArray schemes = Mod.traderAssortDB[index]["barter_scheme"][barterLevelEntry.Key] as JArray;
                    for(int i=0; i < schemes.Count; ++i)
                    {
                        Barter currentBarter = new Barter();
                        currentBarter.level = barterLevelEntry.Value;
                        currentBarter.trader = this;
                        currentBarter.itemData = new List<MeatovItemData>();
                        Mod.defaultItemData.TryGetValue(barterItemID, out MeatovItemData parentItemData);
                        currentBarter.itemData.Add(parentItemData);
                        for(int j=0; j< barterItemChildrenIDs.Count; ++j)
                        {
                            Mod.defaultItemData.TryGetValue(barterItemChildrenIDs[j], out MeatovItemData childItemData);
                            currentBarter.itemData.Add(childItemData);
                        }

                        List<BarterPrice> tempBarterPrices = new List<BarterPrice>();
                        JArray currentScheme = schemes[i] as JArray;
                        for(int j=0; j < currentScheme.Count; ++j)
                        {
                            Mod.defaultItemData.TryGetValue(currentScheme[j]["_tpl"].ToString(), out MeatovItemData barterPriceItemData);
                            if(barterPriceItemData == null)
                            {
                                Mod.LogWarning("DEV: Trader " + index + ": " + ID + ": Couldn't get price "+j+" Item ID for barter with ID: " + barterLevelEntry.Key);

                                // Make null item price
                                BarterPrice currentBarterPrice = new BarterPrice();
                                currentBarterPrice.count = (int)currentScheme[j]["count"];
                                tempBarterPrices.Add(currentBarterPrice);
                            }
                            else
                            {
                                int dogtagLevel = -1;
                                ConditionCounter.EnemyTarget dogtagSide = ConditionCounter.EnemyTarget.Any;
                                if(barterPriceItemData.itemType == MeatovItem.ItemType.DogTag)
                                {
                                    dogtagLevel = (int)currentScheme[j]["level"];
                                    dogtagSide = (ConditionCounter.EnemyTarget)Enum.Parse(typeof(ConditionCounter.EnemyTarget), currentScheme[j]["side"].ToString());
                                }
                                BarterPrice foundPrice = null;
                                for (int k = 0; k < tempBarterPrices.Count; ++k)
                                {
                                    if (tempBarterPrices[k].itemData == barterPriceItemData
                                        && (barterPriceItemData.itemType != MeatovItem.ItemType.DogTag 
                                            || (dogtagLevel == tempBarterPrices[k].dogTagLevel 
                                                && dogtagSide == tempBarterPrices[k].side)))
                                    {
                                        foundPrice = tempBarterPrices[k];
                                        break;
                                    }
                                }

                                // If already have a barter price for this item, just add to the count
                                if (foundPrice == null)
                                {
                                    BarterPrice currentBarterPrice = new BarterPrice();
                                    currentBarterPrice.itemData = barterPriceItemData;
                                    if(barterPriceItemData.itemType == MeatovItem.ItemType.DogTag)
                                    {
                                        currentBarterPrice.dogTagLevel = dogtagLevel;
                                        currentBarterPrice.side = dogtagSide;
                                    }
                                    currentBarterPrice.count = (int)currentScheme[j]["count"];
                                    tempBarterPrices.Add(currentBarterPrice);
                                }
                                else
                                {
                                    foundPrice.count += (int)currentScheme[j]["count"];
                                }
                            }
                        }
                        currentBarter.prices = tempBarterPrices.ToArray();

                        if (bartersByLevel.TryGetValue(barterLevelEntry.Value, out List<Barter> barterList))
                        {
                            barterList.Add(currentBarter);
                        }
                        else
                        {
                            bartersByLevel.Add(barterLevelEntry.Value, new List<Barter>() { currentBarter });
                        }

                        if(bartersByItemID.TryGetValue(barterItemID, out List<Barter> currentBarters))
                        {
                            currentBarters.Add(currentBarter);
                        }
                        else
                        {
                            bartersByItemID.Add(barterItemID, new List<Barter> { currentBarter });
                        }
                    }
                }
            }
        }

        public void FindAssortChildren(string parent, List<string> children, JArray itemsArray)
        {
            for(int i=0; i < itemsArray.Count; ++i)
            {
                if (itemsArray[i]["parentId"].ToString().Equals(parent))
                {
                    children.Add(itemsArray[i]["_tpl"].ToString());
                    FindAssortChildren(itemsArray[i]["_tpl"].ToString(), children, itemsArray);
                }
            }
        }

        public void OnPlayerLevelChanged()
        {
            UpdateLevel();
        }

        public void Save(JToken data)
        {
            data["level"] = level;
            data["standing"] = standing;
            data["standingToRestore"] = standingToRestore;
            data["unlocked"] = unlocked;

            JObject tasks = new JObject();
            for (int i = 0; i < this.tasks.Count; ++i)
            {
                JObject task = new JObject();
                this.tasks[i].Save(task);
                tasks[this.tasks[i].ID] = task;
            }
            data["tasks"] = tasks;
        }

        public void LoadData(JToken data)
        {
            level = (int)data["level"];
            standing = (float)data["standing"];
            standingToRestore = (float)data["standingToRestore"];
            unlocked = (bool)data["unlocked"];
            if (!unlocked)
            {
                unlocked = defaultUnlocked;
            }
        }

        public static int IDToIndex(string ID)
        {
            switch (ID)
            {
                case "54cb50c76803fa8b248b4571":
                    return 0;
                case "54cb57776803fa99248b456e":
                    return 1;
                case "579dc571d53a0658a154fbec":
                    return 2;
                case "58330581ace78e27b8b10cee":
                    return 3;
                case "5935c25fb3acc3127c3d8cd9":
                    return 4;
                case "5a7c2eca46aef81a7ca2145d":
                    return 5;
                case "5ac3b934156ae10c4430e83c":
                    return 6;
                case "5c0647fdd443bc2504c2d371":
                    return 7;
                case "638f541a29ffd1183d187f57":
                    return 8;
                default:
                    return -1;
            }
        }

        public static string IndexToID(int index)
        {
            switch (index)
            {
                case 0:
                    return "54cb50c76803fa8b248b4571";
                case 1:
                    return "54cb57776803fa99248b456e";
                case 2:
                    return "579dc571d53a0658a154fbec";
                case 3:
                    return "58330581ace78e27b8b10cee";
                case 4:
                    return "5935c25fb3acc3127c3d8cd9";
                case 5:
                    return "5a7c2eca46aef81a7ca2145d";
                case 6:
                    return "5ac3b934156ae10c4430e83c";
                case 7:
                    return "5c0647fdd443bc2504c2d371";
                case 8:
                    return "638f541a29ffd1183d187f57";
                default:
                    return "";
            }
        }

        public static string IndexToName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Prapor";
                case 1:
                    return "Therapist";
                case 2:
                    return "Fence";
                case 3:
                    return "Skier";
                case 4:
                    return "Peacekeeper";
                case 5:
                    return "Mechanic";
                case 6:
                    return "Ragman";
                case 7:
                    return "Jaeger";
                case 8:
                    return "Lightkeeper";
                default:
                    return "";
            }
        }

        public static string LevelToRoman(int level)
        {
            // +1 because level is 0 based
            switch (level + 1)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                case 4:
                    return "IV";
                default:
                    Mod.LogError("DEV: Trader.LevelToRoman was given invalid level: "+level+":\n"+Environment.StackTrace);
                    return level.ToString();
            }
        }

        public void UpdateLevel()
        {
            int currentLevel = _level;
            for(int i=currentLevel + 1; i < levels.Length; ++i)
            {
                if (levels[i].minLevel <= Mod.level
                    && levels[i].minSaleSum <= _salesSum
                    && levels[i].minStanding <= _standing)
                {
                    currentLevel = i;
                }
            }
            if(currentLevel != _level)
            {
                level = currentLevel;
            }
        }

        public void OnTraderLevelChangedInvoke(Trader trader)
        {
            if(OnTraderLevelChanged != null)
            {
                OnTraderLevelChanged(trader);
            }
        }

        public void OnTraderStandingChangedInvoke()
        {
            if(OnTraderStandingChanged != null)
            {
                OnTraderStandingChanged();
            }
        }

        public void OnTraderSalesSumChangedInvoke()
        {
            if(OnTraderSalesSumChanged != null)
            {
                OnTraderSalesSumChanged();
            }
        }

        public bool ItemSellable(MeatovItemData itemData)
        {
            return Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), new List<string>(buyCategories), new List<string>(buyBlacklist));
        }

        public bool ItemInsureable(MeatovItemData itemData)
        {
            return insuranceAvailable && Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), new List<string>() { Mod.itemParentID }, new List<string>(insuranceExcluded));
        }
    }

    public class LoyaltyLevel
    {
        public float buyPriceCoef; // Percentage of item value to remove when selling to this trader at this level
        public float exchangePriceCoef;
        public float healPriceCoef;
        public float insurancePriceCoef; // This is written as string OR int in DB
        public int minLevel;
        public int minSaleSum;
        public float minStanding;
        public float repairPriceCoef;
    }

    public class Barter
    {
        // Static data
        public List<MeatovItemData> itemData;
        public int level;
        public Trader trader;
        public BarterPrice[] prices;
        public bool needUnlock;
    }

    public class BarterPrice
    {
        public MeatovItemData itemData;
        public int count;
        public PriceItemView priceItemView;
        public PriceItemView ragFairPriceItemView;

        // DogTag specific
        public int dogTagLevel;
        public ConditionCounter.EnemyTarget side;
    }
}
