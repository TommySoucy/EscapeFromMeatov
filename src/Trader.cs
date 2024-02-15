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

        // Live data
        public int level;
        public float standing;
        public int salesSum;
        public bool unlocked;
        public int balance;

        // Objects
        public TraderUI UI;

        public Trader(int index, string ID)
        {
            this.index = index;
            this.ID = ID;

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
                    currency = 2; // Euro here because EUR is the only string im not sure of
                    break;
            }
            defaultUnlocked = (bool)Mod.traderBaseDB[index]["unlockedByDefault"];
            insuranceAvailable = (bool)Mod.traderBaseDB[index]["insurance"]["availability"];
            List<string> tempInsuranceExcluded = new List<string>();
            JArray insuranceExcludedArray = Mod.traderBaseDB[index]["insurance"]["excluded_category"] as JArray;
            if(insuranceExcludedArray != null)
            {
                for(int i=0; i < insuranceExcludedArray.Count; ++i)
                {
                    tempInsuranceExcluded.Add(Mod.TarkovIDtoH3ID(insuranceExcludedArray[i].ToString()));
                }
                insuranceExcluded = tempInsuranceExcluded.ToArray();
            }
            insuranceMinReturnTime = (int)Mod.traderBaseDB[index]["insurance"]["min_return_hour"];
            insuranceMaxReturnTime = (int)Mod.traderBaseDB[index]["insurance"]["max_return_hour"];
            insuranceMaxStorageTime = (int)Mod.traderBaseDB[index]["insurance"]["max_storage_time"];
            insuranceRate = (int)Mod.traderBaseDB[index]["insurance"]["min_payment"];
            repairAvailable = (bool)Mod.traderBaseDB[index]["repair"]["availability"];
            repairCurrency = Mod.ItemIDToCurrencyIndex(Mod.traderBaseDB[index]["repair"]["repairCurrency"].ToString());
            repairCurrencyCoef = (int)Mod.traderBaseDB[index]["repair"]["currency_coefficient"];
            List<string> tempRepairExcludedList = new List<string>();
            JArray repairExcludedArray = Mod.traderBaseDB[index]["repair"]["excluded_category"] as JArray;
            if (repairExcludedArray != null)
            {
                for (int i = 0; i < repairExcludedArray.Count; ++i)
                {
                    tempRepairExcludedList.Add(Mod.TarkovIDtoH3ID(repairExcludedArray[i].ToString()));
                }
            }
            JArray repairExcludedIDsArray = Mod.traderBaseDB[index]["repair"]["excluded_category"] as JArray;
            if (repairExcludedIDsArray != null)
            {
                for (int i = 0; i < repairExcludedIDsArray.Count; ++i)
                {
                    tempRepairExcludedList.Add(Mod.TarkovIDtoH3ID(repairExcludedIDsArray[i].ToString()));
                }
            }
            repairExcluded = tempRepairExcludedList.ToArray();
            repairPriceRate = (float)Mod.traderBaseDB[index]["repair"]["price_rate"];
            repairQuality = (float)Mod.traderBaseDB[index]["repair"]["quality"];
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
                    tempBuyCategoriesList.Add(Mod.TarkovIDtoH3ID(buyCategoriesArray[i].ToString()));
                }
            }
            JArray buyCategoriesIDsArray = Mod.traderBaseDB[index]["items_buy"]["id_list"] as JArray;
            if (buyCategoriesIDsArray != null)
            {
                for (int i = 0; i < buyCategoriesIDsArray.Count; ++i)
                {
                    tempBuyCategoriesList.Add(Mod.TarkovIDtoH3ID(buyCategoriesIDsArray[i].ToString()));
                }
            }
            buyCategories = tempBuyCategoriesList.ToArray();
            List<string> tempBuyBlacklist = new List<string>();
            JArray buyBlacklistArray = Mod.traderBaseDB[index]["items_buy_prohibited"]["category"] as JArray;
            if (buyBlacklistArray != null)
            {
                for (int i = 0; i < buyBlacklistArray.Count; ++i)
                {
                    tempBuyBlacklist.Add(Mod.TarkovIDtoH3ID(buyBlacklistArray[i].ToString()));
                }
            }
            JArray buyBlacklistIDsArray = Mod.traderBaseDB[index]["items_buy_prohibited"]["id_list"] as JArray;
            if (buyBlacklistIDsArray != null)
            {
                for (int i = 0; i < buyBlacklistIDsArray.Count; ++i)
                {
                    tempBuyBlacklist.Add(Mod.TarkovIDtoH3ID(buyBlacklistIDsArray[i].ToString()));
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

            if(Mod.traderAssortDB != null)
            {
                bartersByLevel = new Dictionary<int, List<Barter>>();
                bartersByItemID = new Dictionary<string, List<Barter>>();
                Dictionary<string, int> barterLevelPerID = Mod.traderAssortDB[index]["loyal_level_items"].ToObject<Dictionary<string, int>>();
                foreach(KeyValuePair<string, int> barterLevelEntry in barterLevelPerID)
                {
                    string barterItemID = Mod.TarkovIDtoH3ID(Mod.traderAssortDB[index]["items"][barterLevelEntry.Key].ToString());

                    JArray schemes = Mod.traderAssortDB[index]["barter_scheme"][barterLevelEntry.Key] as JArray;
                    for(int i=0; i < schemes.Count; ++i)
                    {
                        Barter currentBarter = new Barter();
                        currentBarter.level = barterLevelEntry.Value;
                        currentBarter.itemID = barterItemID;

                        List<BarterPrice> tempBarterPrices = new List<BarterPrice>();
                        JArray currentScheme = schemes[i] as JArray;
                        for(int j=0; j < currentScheme.Count; ++j)
                        {
                            BarterPrice currentBarterPrice = new BarterPrice();
                            currentBarterPrice.itemID = Mod.TarkovIDtoH3ID(currentScheme[j]["_tpl"].ToString());
                            currentBarterPrice.count = (int)currentScheme[j]["count"];

                            tempBarterPrices.Add(currentBarterPrice);
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
    }

    public class LoyaltyLevel
    {
        public float buyPriceCoef;
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
        public string itemID;
        public int level;
        public BarterPrice[] prices;
        public bool needUnlock;

        // Live data
        public bool locked;
    }

    public class BarterPrice
    {
        public string itemID;
        public int count;
    }
}
