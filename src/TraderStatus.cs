using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class TraderStatus
    {
        public static float fenceRestockTimer = 0;

        public string id;
        public int index;
        public string name;
        public int salesSum;
        public float standing;
        public bool unlocked;
        public int currency; // 0: RUB, 1: USD
        public bool offersInsurance;
        public float insuranceRate;

        public Dictionary<int, TraderAssortment> assortmentByLevel;
        public List<string> categories; // Categories defined in DB correspond only to a locale string and have no relation to actual items or their ancestors, so this list will be filled based on what categories this trader sells instead of the categoriesData
        public List<TraderTask> tasks; // TODO: Implement save
        public List<string> itemsToWaitForUnlock;

        public JToken traderData;

        public static Dictionary<string, List<TraderTaskCondition>> waitingQuestConditions;
        public static Dictionary<string, List<TraderTaskCondition>> waitingVisibilityConditions;
        public static Dictionary<string, TraderTask> foundTasks;
        public static Dictionary<string, List<TraderTaskCondition>> foundTaskConditions;
        public static Dictionary<string, List<TraderTaskCondition>> conditionsByItem;
        public static Dictionary<string, List<TraderTaskCondition>> questConditionsByTask; // List of quest conditions dependent on the specific task 
        public static Dictionary<string, List<TraderTaskCondition>> conditionsByVisCond; // List of conditions whos visibility depends on the specific condition

        public List<TraderTask> tasksToInit;
        public List<TraderTaskCondition> conditionsToInit;

        public struct TraderLoyaltyDetails
        {
            public int currentLevel;
            public int nextLevel;
            public int currentMinLevel;
            public int nextMinLevel;
            public int currentMinSalesSum;
            public int nextMinSalesSum;
            public float currentMinStanding;
            public float nextMinStanding;
        }

        public TraderStatus(JToken traderData, int index, string name, int salesSum, float standing, bool unlocked, string currency, JObject assortData, JArray categoriesData)
        {
            this.traderData = traderData;
            this.id = IndexToID(index);
            this.index = index;
            this.name = name;
            this.salesSum = salesSum;
            this.standing = standing;
            this.unlocked = unlocked;
            if (currency.Equals("USD"))
            {
                this.currency = 1;
            }
            offersInsurance = (bool)Mod.traderBaseDB[index]["insurance"]["availability"];
            if (offersInsurance)
            {
                // TODO: Find out where this value is kept, for now prapor will use 0.25 * original value of item, while therapist and any other will be 0.35
                insuranceRate = index == 0 ? 0.25f : 0.35f;
            }

            BuildTasks();

            //categories = categoriesData.ToObject<List<string>>();
            categories = new List<string>();

            BuildAssortments(assortData);
        }

        public void Init()
        {
            // Init conditions that we didnt have save data for because some are dependent on trader loyalty or standing which hasnt been loaded yet
            // when we first build the tasks
            if (conditionsToInit != null)
            {
                foreach (TraderTaskCondition condition in conditionsToInit)
                {
                    if (!condition.init)
                    {
                        condition.Init();
                    }
                }
            }

            // Then init tasks
            if (tasksToInit != null)
            {
                foreach (TraderTask task in tasksToInit)
                {
                    if (!task.init)
                    {
                        task.Init();
                    }
                }
            }
        }

        public int GetLoyaltyLevel()
        {
            JObject traderBase = Mod.traderBaseDB[index];
            for (int i=1; i < traderBase["loyaltyLevels"].Count(); ++i)
            {
                int minLevel = ((int)traderBase["loyaltyLevels"][i]["minLevel"]);
                float minSalesSum = ((int)traderBase["loyaltyLevels"][i]["minSalesSum"]);
                float minStanding = ((int)traderBase["loyaltyLevels"][i]["minStanding"]);

                if(Mod.level < minLevel || salesSum < minSalesSum || standing < minStanding)
                {
                    return i;
                }
            }

            return -1;
        }

        public TraderLoyaltyDetails GetLoyaltyDetails()
        {
            JObject traderBase = Mod.traderBaseDB[index];
            for (int i = 1; i < traderBase["loyaltyLevels"].Count(); ++i)
            {
                int minLevel = ((int)traderBase["loyaltyLevels"][i]["minLevel"]);
                int minSalesSum = ((int)traderBase["loyaltyLevels"][i]["minSalesSum"]);
                float minStanding = ((float)traderBase["loyaltyLevels"][i]["minStanding"]);

                if (Mod.level < minLevel || salesSum < minSalesSum || standing < minStanding)
                {
                    TraderLoyaltyDetails lowerTLD = new TraderLoyaltyDetails();
                    // Current level will be i, but i is the index, not the level, and it is 1 smaller than the level we are currently checking
                    // So the first one where level<minLevel etc will be the next level, i is then current level, and i + 1 is next level
                    // and if we never hit that, it means that the current level is the last one
                    lowerTLD.currentLevel = i;
                    lowerTLD.nextLevel = i + 1;
                    lowerTLD.currentMinLevel = ((int)traderBase["loyaltyLevels"][i - 1]["minLevel"]);
                    lowerTLD.nextMinLevel = minLevel;
                    lowerTLD.currentMinStanding = ((int)traderBase["loyaltyLevels"][i - 1]["minStanding"]);
                    lowerTLD.nextMinStanding = minStanding;
                    lowerTLD.currentMinSalesSum = ((int)traderBase["loyaltyLevels"][i - 1]["minSalesSum"]);
                    lowerTLD.nextMinSalesSum = minSalesSum;

                    return lowerTLD;
                }
            }

            // All stats are greater than all loyalty levels requirements, meaning we are at max level
            // Set tld's current and next level equal to mean that we reach maximum
            TraderLoyaltyDetails tld = new TraderLoyaltyDetails();
            tld.currentLevel = traderBase["loyaltyLevels"].Count();
            tld.nextLevel = tld.currentLevel;
            return tld;
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

        public static string LoyaltyLevelToRoman(int level)
        {
            switch (level)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                default:
                    return "";
            }
        }

        public static string GetMoneyString(long money)
        {
            if (money < 1000L)
            {
                return money.ToString();
            }
            if (money < 1000000L)
            {
                long num = money / 1000L;
                long num2 = money / 100L % 10L;
                return num + ((num2 != 0L) ? ("." + num2) : "") + "k";
            }
            long num3 = money / 1000000L;
            long num4 = money / 100000L % 10L;
            return num3 + ((num4 != 0L) ? ("." + num4) : "") + "M";
        }

        private void BuildAssortments(JObject assortData)
        {
            // TODO: based on traderData["previousRestockTime"] and current time, decide whether we should load amounts of items from save (If havent restocked yet since last save) or just set default (has restocked since)
            Mod.LogInfo("BuildAssorts called on trader: " + index);
            assortmentByLevel = new Dictionary<int, TraderAssortment>();
            foreach (JToken entry in assortData["items"])
            {
                if (entry["parentId"].ToString().Equals("hideout"))
                {
                    // Only add item if we have an ID for it
                    string entryID = entry["_id"].ToString(); 
                    string itemID = entry["_tpl"].ToString();
                    int loyaltyLevel = (int)assortData["loyal_level_items"][entryID];
                    JArray barterSchemes = assortData["barter_scheme"][entryID][0] as JArray;
                    string[] actualItemIDs = null;
                    if (Mod.itemMap.ContainsKey(itemID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[itemID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                actualItemIDs = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                actualItemIDs = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                actualItemIDs = new string[] { itemMapEntry.otherModID };
                                break;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    foreach (string actualParentItemID in actualItemIDs)
                    {
                        // Add new assort for this level or get the one already there
                        TraderAssortment currentAssort = null;
                        if (!assortmentByLevel.ContainsKey(loyaltyLevel))
                        {
                            currentAssort = new TraderAssortment();
                            currentAssort.level = loyaltyLevel;
                            currentAssort.itemsByID = new Dictionary<string, AssortmentItem>();
                            assortmentByLevel.Add(loyaltyLevel, currentAssort);
                        }
                        else
                        {
                            currentAssort = assortmentByLevel[loyaltyLevel];
                        }

                        // Add all the items in the assort
                        if (currentAssort.itemsByID.ContainsKey(actualParentItemID))
                        {
                            if (entry["upd"] != null && entry["upd"]["StackObjectsCount"] != null)
                            {
                                currentAssort.itemsByID[actualParentItemID].stack += (int)entry["upd"]["StackObjectsCount"];
                            }
                            else
                            {
                                currentAssort.itemsByID[actualParentItemID].stack += 100; // TODO: Review trader 579dc571d53a0658a154fbec, their assort does not specify stack
                            }

                            // Build entry's pricelist
                            List<AssortmentPriceData> currentPrices = new List<AssortmentPriceData>();
                            bool onlyCurrency = true;
                            int totalRoubles = 0;
                            bool useFallback = false;
                            bool missingFallback = false;
                            for (int i = 0; i < 2; ++i)
                            {
                                foreach (JObject price in barterSchemes)
                                {
                                    string priceID = price["_tpl"].ToString();
                                    if (Mod.itemMap.ContainsKey(priceID))
                                    {
                                        string[] priceIDs = null;
                                        ItemMapEntry itemMapEntry = Mod.itemMap[priceID];
                                        if (!useFallback && itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.ToString().Equals("")))
                                        {
                                            if ((assortData["barter_scheme"][entryID] as JArray).Count <= 1)
                                            {
                                                Mod.LogError("Trader: " + index + " has an assort entry: " + entryID + " with price: " + priceID + " which has not item map ID, but is also missing a fallback entirely");
                                                missingFallback = true;
                                                break;
                                            }
                                            else
                                            {
                                                useFallback = true;
                                                currentPrices.Clear();
                                                barterSchemes = assortData["barter_scheme"][entryID][1] as JArray;
                                                break;
                                            }
                                        }
                                        switch (itemMapEntry.mode)
                                        {
                                            case 0:
                                                priceIDs = new string[] { itemMapEntry.ID };
                                                break;
                                            case 1:
                                                priceIDs = itemMapEntry.modulIDs;
                                                break;
                                            case 2:
                                                priceIDs = new string[] { itemMapEntry.otherModID };
                                                break;
                                        }
                                        foreach (string newPriceID in priceIDs)
                                        {
                                            AssortmentPriceData priceData = new AssortmentPriceData();
                                            priceData.ID = newPriceID;
                                            priceData.count = (int)price["count"];

                                            if (priceData.ID.Equals("11") || priceData.ID.Equals("12"))
                                            {
                                                priceData.priceItemType = AssortmentPriceData.PriceItemType.Dogtag;
                                                priceData.USEC = price["side"].ToString().Equals("usec");
                                                priceData.dogtagLevel = (int)price["level"];
                                            }
                                            else
                                            {
                                                priceData.priceItemType = AssortmentPriceData.PriceItemType.Other;
                                            }

                                            bool alreadyContainsID = false;
                                            AssortmentPriceData otherPriceData = null;
                                            foreach (AssortmentPriceData otherAssortPriceData in currentPrices)
                                            {
                                                if (otherAssortPriceData.ID.Equals(priceData.ID))
                                                {
                                                    otherPriceData = otherAssortPriceData;
                                                    alreadyContainsID = true;
                                                    break;
                                                }
                                            }
                                            if (alreadyContainsID)
                                            {
                                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                                {
                                                    if (otherPriceData.dogtagLevel == priceData.dogtagLevel && otherPriceData.USEC == priceData.USEC)
                                                    {
                                                        otherPriceData.count += priceData.count;
                                                    }
                                                    else
                                                    {
                                                        currentPrices.Add(priceData);
                                                    }
                                                }
                                                else if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Other)
                                                {
                                                    // This should just be a price that maps to the same ID as another, so just increment the count
                                                    otherPriceData.count += priceData.count;
                                                }
                                            }
                                            else
                                            {
                                                currentPrices.Add(priceData);
                                            }

                                            if (priceData.ID.Equals("203"))
                                            {
                                                totalRoubles += priceData.count;
                                            }
                                            else if (priceData.ID.Equals("201"))
                                            {
                                                totalRoubles += priceData.count * 125;
                                            }
                                            else
                                            {
                                                onlyCurrency = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (useFallback)
                                        {
                                            Mod.LogError("Trader " + index + " assort entry " + entryID + " fallback ID ("+ priceID + ") missing from item map");
                                        }
                                        else
                                        {
                                            if ((assortData["barter_scheme"][entryID] as JArray).Count <= 1)
                                            {
                                                Mod.LogError("Trader: " + index + " has an assort entry: " + entryID + " with price: " + priceID + " which has no item map ID, but is also missing a fallback entirely");
                                                missingFallback = true;
                                            }
                                            else
                                            {
                                                useFallback = true;
                                                currentPrices.Clear();
                                                barterSchemes = assortData["barter_scheme"][entryID][1] as JArray;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!useFallback || missingFallback)
                                {
                                    break;
                                }
                            }

                            // Skip the rest if this price is an error anyway
                            if (missingFallback)
                            {
                                continue;
                            }

                            // Ensure that the price isn't exactly the same as the assort item itself
                            if(currentPrices.Count == 1 && currentPrices[0].ID.Equals(actualParentItemID))
                            {
                                continue;
                            }

                            // Ensure that this exact pricelist doesn't already exist, only add the pricelist if it isnt there yet
                            bool priceListFound = false;
                            foreach (List<AssortmentPriceData> existingPriceList in currentAssort.itemsByID[actualParentItemID].prices)
                            {
                                bool allFound = true;
                                foreach (AssortmentPriceData price in currentPrices)
                                {
                                    bool foundID = false;
                                    AssortmentPriceData otherPriceData = null;
                                    foreach (AssortmentPriceData otherAssortPriceData in existingPriceList)
                                    {
                                        if (otherAssortPriceData.ID.Equals(price.ID))
                                        {
                                            otherPriceData = otherAssortPriceData;
                                            foundID = true;
                                            break;
                                        }
                                    }

                                    if (price.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                    {
                                        if (!foundID || otherPriceData.dogtagLevel != price.dogtagLevel || otherPriceData.USEC != price.USEC)
                                        {
                                            allFound = false;
                                            break;
                                        }
                                    }
                                    else if (price.priceItemType == AssortmentPriceData.PriceItemType.Other)
                                    {
                                        if (!foundID)
                                        {
                                            allFound = false;
                                            break;
                                        }
                                    }
                                }
                                if (allFound)
                                {
                                    priceListFound = true;
                                    break;
                                }
                            }
                            if (!priceListFound && !missingFallback)
                            {
                                currentAssort.itemsByID[actualParentItemID].prices.Add(currentPrices);

                                if (onlyCurrency)
                                {
                                    if (Mod.lowestBuyValueByItem == null)
                                    {
                                        Mod.lowestBuyValueByItem = new Dictionary<string, int>();
                                    }
                                    if (Mod.lowestBuyValueByItem.ContainsKey(actualParentItemID))
                                    {
                                        if (Mod.lowestBuyValueByItem[actualParentItemID] > totalRoubles)
                                        {
                                            Mod.lowestBuyValueByItem[actualParentItemID] = totalRoubles;
                                        }
                                    }
                                    else
                                    {
                                        Mod.lowestBuyValueByItem.Add(actualParentItemID, totalRoubles);
                                    }
                                }
                            }
                        }
                        else
                        {
                            AssortmentItem item = new AssortmentItem();
                            item.ID = actualParentItemID;
                            item.prices = new List<List<AssortmentPriceData>>();
                            List<AssortmentPriceData> currentPrices = new List<AssortmentPriceData>();
                            item.prices.Add(currentPrices);
                            bool onlyCurrency = true;
                            int totalRoubles = 0;
                            bool useFallback = false;
                            bool missingFallback = false;
                            for (int i = 0; i < 2; ++i)
                            {
                                foreach (JObject price in barterSchemes)
                                {
                                    string priceID = price["_tpl"].ToString();
                                    if (Mod.itemMap.ContainsKey(priceID))
                                    {
                                        string[] priceIDs = null;
                                        ItemMapEntry itemMapEntry = Mod.itemMap[priceID];
                                        if (!useFallback && itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.ToString().Equals("")))
                                        {
                                            if ((assortData["barter_scheme"][entryID] as JArray).Count <= 1)
                                            {
                                                Mod.LogError("Trader: " + index + " has an assort entry: " + entryID + " with price: " + priceID + " which has not item map ID, but is also missing a fallback entirely");
                                                missingFallback = true;
                                                break;
                                            }
                                            else
                                            {
                                                useFallback = true;
                                                currentPrices.Clear();
                                                barterSchemes = assortData["barter_scheme"][entryID][1] as JArray;
                                                break;
                                            }
                                        }
                                        switch (itemMapEntry.mode)
                                        {
                                            case 0:
                                                priceIDs = new string[] { itemMapEntry.ID };
                                                break;
                                            case 1:
                                                priceIDs = itemMapEntry.modulIDs;
                                                break;
                                            case 2:
                                                priceIDs = new string[] { itemMapEntry.otherModID };
                                                break;
                                        }

                                        foreach (string newPriceID in priceIDs)
                                        {
                                            AssortmentPriceData priceData = new AssortmentPriceData();
                                            priceData.ID = newPriceID;
                                            priceData.count = (int)price["count"];

                                            if (priceData.ID.Equals("11") || priceData.ID.Equals("12"))
                                            {
                                                priceData.priceItemType = AssortmentPriceData.PriceItemType.Dogtag;
                                                priceData.USEC = price["side"].ToString().Equals("usec");
                                                priceData.dogtagLevel = (int)price["level"];
                                            }
                                            else
                                            {
                                                priceData.priceItemType = AssortmentPriceData.PriceItemType.Other;
                                            }

                                            bool alreadyContainsID = false;
                                            AssortmentPriceData otherPriceData = null;
                                            foreach (AssortmentPriceData otherAssortPriceData in currentPrices)
                                            {
                                                if (otherAssortPriceData.ID.Equals(priceData.ID))
                                                {
                                                    otherPriceData = otherAssortPriceData;
                                                    alreadyContainsID = true;
                                                    break;
                                                }
                                            }
                                            if (alreadyContainsID)
                                            {
                                                if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Dogtag)
                                                {
                                                    if (otherPriceData.dogtagLevel == priceData.dogtagLevel && otherPriceData.USEC == priceData.USEC)
                                                    {
                                                        otherPriceData.count += priceData.count;
                                                    }
                                                    else
                                                    {
                                                        currentPrices.Add(priceData);
                                                    }
                                                }
                                                else if (priceData.priceItemType == AssortmentPriceData.PriceItemType.Other)
                                                {
                                                    // This should just be a price that maps to the same ID as another, so just increment the count
                                                    otherPriceData.count += priceData.count;
                                                }
                                            }
                                            else
                                            {
                                                currentPrices.Add(priceData);
                                            }

                                            if (priceData.ID.Equals("203"))
                                            {
                                                totalRoubles += priceData.count;
                                            }
                                            else if (priceData.ID.Equals("201"))
                                            {
                                                totalRoubles += priceData.count * 125;
                                            }
                                            else
                                            {
                                                onlyCurrency = false;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (useFallback)
                                        {
                                            Mod.LogError("Trader " + index + " assort entry " + entryID + " fallback ID missing from item map");
                                        }
                                        else
                                        {
                                            if ((assortData["barter_scheme"][entryID] as JArray).Count <= 1)
                                            {
                                                Mod.LogError("Trader: " + index + " has an assort entry: " + entryID + " with price: " + priceID + " which has not item map ID, but is also missing a fallback entirely");
                                                missingFallback = true;
                                            }
                                            else
                                            {
                                                useFallback = true;
                                                currentPrices.Clear();
                                                barterSchemes = assortData["barter_scheme"][entryID][1] as JArray;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (!useFallback || missingFallback)
                                {
                                    break;
                                }
                            }

                            // Skip the rest if this price is an error anyway
                            if (missingFallback)
                            {
                                continue;
                            }

                            // Ensure that the price isn't exactly the same as the assort item itself
                            if (currentPrices.Count == 1 && currentPrices[0].ID.Equals(actualParentItemID))
                            {
                                continue;
                            }

                            if (onlyCurrency)
                            {
                                if (Mod.lowestBuyValueByItem == null)
                                {
                                    Mod.lowestBuyValueByItem = new Dictionary<string, int>();
                                }
                                if (Mod.lowestBuyValueByItem.ContainsKey(actualParentItemID))
                                {
                                    if (Mod.lowestBuyValueByItem[actualParentItemID] > totalRoubles)
                                    {
                                        Mod.lowestBuyValueByItem[actualParentItemID] = totalRoubles;
                                    }
                                }
                                else
                                {
                                    Mod.lowestBuyValueByItem.Add(actualParentItemID, totalRoubles);
                                }
                            }

                            if (entry["upd"] != null && entry["upd"]["StackObjectsCount"] != null)
                            {
                                item.stack = (int)entry["upd"]["StackObjectsCount"];
                            }
                            else
                            {
                                item.stack = 100; // TODO: Review trader 579dc571d53a0658a154fbec, their assort does not specify stack
                            }

                            if (entry["upd"] != null && entry["upd"]["BuyRestrictionMax"] != null)
                            {
                                item.buyRestrictionMax = (int)entry["upd"]["BuyRestrictionMax"];
                            }

                            if (!missingFallback)
                            {
                                currentAssort.itemsByID.Add(item.ID, item);
                            }

                            // Add the first of this item's ancestor to the list of sell categories if not already in there
                            if (Mod.itemAncestors.ContainsKey(item.ID))
                            {
                                string firstAncestor = Mod.itemAncestors[item.ID][0];
                                if (!categories.Contains(firstAncestor))
                                {
                                    categories.Add(firstAncestor);
                                }
                            }
                            else
                            {
                                Mod.LogError("Item ancestors does not contain a list for item: " + item.ID);
                            }
                        }
                    }
                }
            }

            // If Fence, should be able to sell all items
            if (index == 2)
            {
                categories.Clear();
                categories.Add("54009119af1c881c07000029"); // All items
            }
        }

        public bool ItemSellable(string itemID, List<string> ancestors)
        {
            // TODO: Also check every sub item for validity
            if (categories.Contains(itemID))
            {
                return true;
            }

            foreach(string ancestor in ancestors)
            {
                if (categories.Contains(ancestor))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ItemInsureable(string itemID, List<string> ancestors)
        {
            if (!(bool)Mod.traderBaseDB[index]["insurance"]["availability"])
            {
                return false;
            }

            // 5448bf274bdc2dfc2f8b456a secure container
            // 5f4f9eb969cdc30ff33f09db compass
            // 5447e1d04bdc2dff2f8b4567 knife (all melee really, it also includes axes)
            // 543be6674bdc2df1348b4569 food and drink
            // 543be5dd4bdc2deb348b4569 money
            // 543be5664bdc2dd4348b4569 meds
            // 543be5cb4bdc2deb348b4568 ammo container (doesnt include mags/clips, so just ammo boxes)
            // 5485a8684bdc2da71d8b4567 ammo

            // Check if compass, this is the only specific ID
            if (itemID.Equals("5f4f9eb969cdc30ff33f09db"))
            {
                return false;
            }

            foreach(string ancestor in ancestors)
            {
                switch (ancestor)
                {
                    case "5448bf274bdc2dfc2f8b456a": // secure container
                    case "5447e1d04bdc2dff2f8b4567": // melee
                    case "543be6674bdc2df1348b4569": // food/drink
                    case "543be5dd4bdc2deb348b4569": // money
                    case "543be5664bdc2dd4348b4569": // meds
                    case "543be5cb4bdc2deb348b4568": // ammoboxes
                    case "5485a8684bdc2da71d8b4567": // ammo
                        return false;
                    default:
                        break;
                }
            }

            return true;
        }

        private void BuildTasks()
        {
            tasks = new List<TraderTask>();
            if (conditionsByItem == null)
            {
                conditionsByItem = new Dictionary<string, List<TraderTaskCondition>>();
            }

            // Get raw tasks
            Dictionary<string, JObject> tasksData = Mod.questDB.ToObject<Dictionary<string, JObject>>();
            Dictionary<string, JObject> rawTasks = new Dictionary<string, JObject>();
            foreach (KeyValuePair<string, JObject> rawTask in tasksData)
            {
                if (rawTask.Value["traderId"].ToString().Equals(id))
                {
                    rawTasks.Add(rawTask.Key, rawTask.Value);
                }
            }

            // Get quest locales
            Dictionary<string, JObject> questLocales = Mod.localeDB["quest"].ToObject<Dictionary<string, JObject>>();

            foreach (KeyValuePair<string, JObject> rawTask in rawTasks)
            {
                JObject taskSaveData = null;
                if(traderData != null && traderData["tasks"] != null && traderData["tasks"][rawTask.Key] != null)
                {
                    taskSaveData = (JObject)traderData["tasks"][rawTask.Key];
                }

                JObject questData = rawTask.Value;

                // Find quest locale
                JObject questLocale = null;
                foreach (KeyValuePair<string, JObject> quest in questLocales)
                {
                    if (quest.Key.Equals(rawTask.Key))
                    {
                        questLocale = quest.Value;
                        break;
                    }
                }
                if(questLocale == null)
                {
                    Mod.LogError("Could not find quest with ID: "+rawTask.Key + " in locale");
                    continue;
                }

                TraderTask newTask = new TraderTask();
                tasks.Add(newTask);

                newTask.ID = rawTask.Key;
                newTask.ownerTraderIndex = index;
                newTask.name = questLocale["name"].ToString();
                newTask.description = Mod.localeDB["mail"][questLocale["description"].ToString()].ToString();
                if (questLocale["failMessageText"] != null && Mod.localeDB["mail"][questLocale["failMessageText"].ToString()] != null) // Will be null if quest has no fail conditions
                {
                    newTask.failMessage = Mod.localeDB["mail"][questLocale["failMessageText"].ToString()].ToString();
                }
                if (questLocale["successMessageText"] != null && Mod.localeDB["mail"][questLocale["successMessageText"].ToString()] != null) // Can be null?
                {
                    newTask.successMessage = Mod.localeDB["mail"][questLocale["successMessageText"].ToString()].ToString(); // Unused anyway
                }
                newTask.location = "Any";
                if (taskSaveData == null)
                {
                    if(tasksToInit == null)
                    {
                        tasksToInit = new List<TraderTask>();
                    }
                    tasksToInit.Add(newTask);
                }
                else
                {
                    newTask.taskState = (TraderTask.TaskState)Enum.Parse(typeof(TraderTask.TaskState), taskSaveData["state"].ToString());
                    newTask.init = true;
                }

                // Fill start conditions
                newTask.startConditions = new List<TraderTaskCondition>();
                JArray startConditionsData = questData["conditions"]["AvailableForStart"] as JArray;
                if (startConditionsData != null && startConditionsData.Count > 0)
                {
                    foreach (JObject startConditionData in questData["conditions"]["AvailableForStart"])
                    {
                        TraderTaskCondition newCondition = new TraderTaskCondition();

                        if (!SetCondition(newCondition, startConditionData, questLocale, taskSaveData, newTask, true, true, false))
                        {
                            continue;
                        }
                        else
                        {
                            newTask.startConditions.Add(newCondition);
                        }
                    }
                }
                else if(newTask.taskState == TraderTask.TaskState.Locked) // Some starting tasks may have no starting conditions so set to available if currently locked
                {
                    newTask.taskState = TraderTask.TaskState.Available;
                }

                // Fill completion conditions
                newTask.completionConditions = new List<TraderTaskCondition>();
                foreach (JObject completionConditionData in questData["conditions"]["AvailableForFinish"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, completionConditionData, questLocale, taskSaveData, newTask, false, false, false))
                    {
                        continue;
                    }
                    else
                    {
                        newTask.completionConditions.Add(newCondition);
                    }
                }

                // Fill fail conditions
                newTask.failConditions = new List<TraderTaskCondition>();
                foreach (JObject failConditionData in questData["conditions"]["Fail"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, failConditionData, questLocale, taskSaveData, newTask, true, false, true))
                    {
                        continue;
                    }
                    else
                    {
                        newTask.failConditions.Add(newCondition);
                    }
                }

                // Fill success rewards
                newTask.successRewards = new List<TraderTaskReward>();
                itemsToWaitForUnlock = new List<string>();
                foreach (JObject rewardData in questData["rewards"]["Success"])
                {
                    TraderTaskReward newReward = new TraderTaskReward();
                    SetReward(newReward, rewardData, newTask, newTask.successRewards);
                }

                // Fill fail rewards
                newTask.failureRewards = new List<TraderTaskReward>();
                foreach (JObject rewardData in questData["rewards"]["Fail"])
                {
                    TraderTaskReward newReward = new TraderTaskReward();
                    SetReward(newReward, rewardData, newTask, newTask.failureRewards);
                }

                // Fill start rewards (initial equipment)
                newTask.startingEquipment = new List<TraderTaskReward>();
                foreach (JObject rewardData in questData["rewards"]["Started"])
                {
                    TraderTaskReward newReward = new TraderTaskReward();
                    SetReward(newReward, rewardData, newTask, newTask.startingEquipment);
                }

                // Add task to found tasks and update condition waiting for it if any
                foundTasks.Add(rawTask.Key, newTask);
                if (waitingQuestConditions.ContainsKey(rawTask.Key))
                {
                    foreach(TraderTaskCondition condition in waitingQuestConditions[rawTask.Key])
                    {
                        condition.target = newTask;
                    }
                    waitingQuestConditions.Remove(rawTask.Key);
                }
            }
        }

        private void SetReward(TraderTaskReward reward, JObject rewardData, TraderTask task, List<TraderTaskReward> listToFill)
        {
            bool useFallback = false;
            switch (rewardData["type"].ToString())
            {
                case "Experience":
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.Experience;
                    reward.experience = int.Parse(rewardData["value"].ToString());
                    listToFill.Add(reward);
                    break;
                case "TraderStanding":
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.TraderStanding;
                    reward.traderIndex = IDToIndex(rewardData["target"].ToString());
                    reward.standing = float.Parse(rewardData["value"].ToString());
                    listToFill.Add(reward);
                    break;
                case "Item":
                    string originalItemID = rewardData["items"][0]["_tpl"].ToString();
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.Item;
                    if (Mod.itemMap.ContainsKey(originalItemID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalItemID];
                        if(itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.Equals("")))
                        {
                            useFallback = true;
                        }
                        if (!useFallback)
                        {
                            switch (itemMapEntry.mode)
                            {
                                case 0:
                                    reward.itemIDs = new string[] { itemMapEntry.ID };
                                    break;
                                case 1:
                                    reward.itemIDs = itemMapEntry.modulIDs;
                                    break;
                                case 2:
                                    reward.itemIDs = new string[] { itemMapEntry.otherModID };
                                    break;
                            }

                            reward.amount = int.Parse(rewardData["value"].ToString());
                            listToFill.Add(reward);
                        }
                    }
                    else
                    {
                        useFallback = true;
                    }

                    if (useFallback)
                    {
                        if ((rewardData["items"] as JArray).Count > 1)
                        {
                            originalItemID = rewardData["items"][1]["_tpl"].ToString();
                            if (originalItemID.Equals(""))
                            {
                                // This is an empty fallback, so there will be no reward, unless the mod for this item is implemented
                                return;
                            }
                            if (Mod.itemMap.ContainsKey(originalItemID))
                            {
                                ItemMapEntry itemMapEntry = Mod.itemMap[originalItemID];
                                if (itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.Equals("")))
                                {
                                    Mod.LogError("Item reward for task: " + task.ID + " had missing main entry, but fallback " + originalItemID + " is on mode 0 but is also missing H3ID");
                                    return;
                                }
                                switch (itemMapEntry.mode)
                                {
                                    case 0:
                                        reward.itemIDs = new string[] { itemMapEntry.ID };
                                        break;
                                    case 1:
                                        reward.itemIDs = itemMapEntry.modulIDs;
                                        break;
                                    case 2:
                                        reward.itemIDs = new string[] { itemMapEntry.otherModID };
                                        break;
                                }

                                reward.amount = int.Parse(rewardData["value"].ToString());
                                listToFill.Add(reward);
                            }
                            else
                            {
                                Mod.LogError("Item reward for task: " + task.ID + " had missing main entry, but fallback " + originalItemID + " is also missing");
                            }
                        }
                        else
                        {
                            Mod.LogError("Item reward for task: " + task.ID + " had missing main entry, but fallback data was missing entirely");
                        }
                    }
                    break;
                case "AssortmentUnlock":
                    string originalAssortUnlockItemID = rewardData["items"][0]["_tpl"].ToString();
                    if (Mod.itemMap.ContainsKey(originalAssortUnlockItemID))
                    {
                        reward.taskRewardType = TraderTaskReward.TaskRewardType.AssortmentUnlock;

                        ItemMapEntry itemMapEntry = Mod.itemMap[originalAssortUnlockItemID];
                        if (itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.Equals("")))
                        {
                            useFallback = true;
                        }
                        if (!useFallback)
                        {
                            switch (itemMapEntry.mode)
                            {
                                case 0:
                                    reward.itemIDs = new string[] { itemMapEntry.ID };
                                    break;
                                case 1:
                                    reward.itemIDs = itemMapEntry.modulIDs;
                                    break;
                                case 2:
                                    reward.itemIDs = new string[] { itemMapEntry.otherModID };
                                    break;
                            }

                            listToFill.Add(reward);
                            itemsToWaitForUnlock.Add(originalAssortUnlockItemID);
                        }
                    }
                    else
                    {
                        useFallback = true;
                    }

                    if (useFallback)
                    {
                        if ((rewardData["items"] as JArray).Count > 1)
                        {
                            originalAssortUnlockItemID = rewardData["items"][1]["_tpl"].ToString();
                            if (!originalAssortUnlockItemID.Equals(""))
                            {
                                if (Mod.itemMap.ContainsKey(originalAssortUnlockItemID))
                                {
                                    ItemMapEntry itemMapEntry = Mod.itemMap[originalAssortUnlockItemID];
                                    switch (itemMapEntry.mode)
                                    {
                                        case 0:
                                            reward.itemIDs = new string[] { itemMapEntry.ID };
                                            break;
                                        case 1:
                                            reward.itemIDs = itemMapEntry.modulIDs;
                                            break;
                                        case 2:
                                            reward.itemIDs = new string[] { itemMapEntry.otherModID };
                                            break;
                                    }

                                    listToFill.Add(reward);
                                    itemsToWaitForUnlock.Add(originalAssortUnlockItemID);
                                }
                                else
                                {
                                    Mod.LogError("AssortmentUnlock reward for task: " + task.ID + " had missing main entry, but fallback ID "+ originalAssortUnlockItemID + " was missing from item map");
                                }
                            }
                        }
                        else
                        {
                            Mod.LogError("AssortmentUnlock reward for task: " + task.ID + " had missing main entry, but fallback data was missing entirely");
                        }
                    }
                    break;
                case "TraderUnlock":
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.TraderUnlock;
                    reward.traderIndex = IDToIndex(rewardData["target"].ToString());
                    listToFill.Add(reward);
                    break;
                default:
                    break;
            }
        }

        private bool SetCondition(TraderTaskCondition condition, JObject conditionData, JObject taskLocale, JObject taskSaveData, TraderTask task, bool addToList, bool startCondition, bool failCondition)
        {
            condition.ID = conditionData["_props"]["id"].ToString();
            condition.failCondition = failCondition;
            condition.task = task;
            if (taskLocale["conditions"][condition.ID] != null) // This will be null for start/fail conditions
            {
                condition.text = taskLocale["conditions"][condition.ID].ToString();
            }

            JObject conditionSaveData = null;
            if (taskSaveData != null && taskSaveData["conditions"] != null && taskSaveData["conditions"][condition.ID] != null)
            {
                conditionSaveData = (JObject)taskSaveData["conditions"][condition.ID];
            }
            if (task.taskState == TraderTask.TaskState.Success || task.taskState == TraderTask.TaskState.Complete)
            {
                condition.fulfilled = true;
                condition.init = true;
            }
            else if (task.taskState != TraderTask.TaskState.Active)
            {
                if (conditionSaveData != null)
                {
                    condition.fulfilled = (bool)conditionSaveData["fulfilled"];
                    condition.init = true;
                }
                else
                {
                    if (conditionSaveData == null)
                    {
                        conditionsToInit = new List<TraderTaskCondition>();
                    }
                    conditionsToInit.Add(condition);
                }
            }
            else
            {
                condition.fulfilled = false;
                condition.init = true;
            }

            // Only add cond to tracking list if not yet fulfilled, if we want, and if it is not a start condition or (if it is) the task is locked
            // Because if it is a start condition and the quest is available or more, we dont care about start conditions anymore
            bool notStartOrLocked = !startCondition || task.taskState == TraderTask.TaskState.Locked;
            if (!condition.fulfilled && addToList && notStartOrLocked)
            {
                AddConditionToList(startCondition, false, condition);
            }

            switch (conditionData["_parent"].ToString())
            {
                case "CounterCreator":
                    condition.conditionType = TraderTaskCondition.ConditionType.CounterCreator;
                    condition.counters = new List<TraderTaskCounterCondition>();
                    condition.value = (int)conditionData["_props"]["value"];
                    foreach (JObject counter in conditionData["_props"]["counter"]["conditions"])
                    {
                        TraderTaskCounterCondition newCounter = new TraderTaskCounterCondition();
                        newCounter.ID = counter["_props"]["id"].ToString();
                        newCounter.parentCondition = condition;
                        JObject conditionCounterSaveData = null;
                        if (conditionSaveData != null && conditionSaveData["counters"] != null && conditionSaveData["counters"][newCounter.ID] != null)
                        {
                            conditionCounterSaveData = (JObject)conditionSaveData["counters"][newCounter.ID];
                        }
                        switch (counter["_parent"].ToString())
                        {
                            case "Kills":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.Kills;
                                if (counter["_props"]["target"].ToString().Equals("Savage"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav;
                                }
                                else if(counter["_props"]["target"].ToString().Equals("AnyPmc"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC;
                                }
                                else if(counter["_props"]["target"].ToString().Equals("Usec"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec;
                                }
                                else if(counter["_props"]["target"].ToString().Equals("Bear"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear;
                                }
                                else if(counter["_props"]["target"].ToString().Equals("Any"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Any;
                                }
                                else
                                {
                                    Mod.LogError("Task: " + task.ID + " has condition: " + condition.ID + " with kill counter with unhandled target type: " + counter["_props"]["target"].ToString());
                                }
                                if (counter["_props"]["weapon"] != null)
                                {
                                    newCounter.allowedWeaponIDs = new List<string>();
                                    List<string> originalIDs = counter["_props"]["weapon"].ToObject<List<string>>();
                                    for (int i = 0; i < originalIDs.Count; ++i)
                                    {
                                        if (Mod.itemMap.ContainsKey(originalIDs[i]))
                                        {
                                            ItemMapEntry itemMapEntry = Mod.itemMap[originalIDs[i]];
                                            switch (itemMapEntry.mode)
                                            {
                                                case 0:
                                                    newCounter.allowedWeaponIDs.Add(itemMapEntry.ID);
                                                    break;
                                                case 1:
                                                    newCounter.allowedWeaponIDs.AddRange(itemMapEntry.modulIDs);
                                                    break;
                                                case 2:
                                                    newCounter.allowedWeaponIDs.Add(itemMapEntry.otherModID);
                                                    break;
                                            }
                                        }
                                        //else Item is either missing or this is a category of item
                                    }
                                }
                                if (counter["_props"]["weaponModsInclusive"] != null) 
                                {
                                    newCounter.weaponModsInclusive = new List<string>();
                                    foreach(JArray weaponMod in counter["_props"]["weaponModsInclusive"])
                                    {
                                        // take the last element of the inner array because that seems to be the correct one
                                        string lastElement = weaponMod[weaponMod.Count - 1].ToString();
                                        if (Mod.itemMap.ContainsKey(lastElement))
                                        {
                                            ItemMapEntry itemMapEntry = Mod.itemMap[lastElement];
                                            if(itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.ToString().Equals("")))
                                            {
                                                continue;
                                            }
                                            switch (itemMapEntry.mode)
                                            {
                                                case 0:
                                                    newCounter.weaponModsInclusive.Add(itemMapEntry.ID);
                                                    break;
                                                case 1:
                                                    newCounter.weaponModsInclusive.AddRange(itemMapEntry.modulIDs);
                                                    break;
                                                case 2:
                                                    newCounter.weaponModsInclusive.Add(itemMapEntry.otherModID);
                                                    break;
                                            }
                                        }
                                        //else Item is either missing or this is a category of item
                                    }
                                }
                                if (counter["_props"]["distance"] != null) 
                                {
                                    newCounter.distance = (float)counter["_props"]["distance"]["value"];
                                    if (counter["_props"]["distance"]["compareMethod"].ToString().Equals("<="))
                                    {
                                        newCounter.distanceCompareMode = 1;
                                    }
                                }

                                if (condition.fulfilled)
                                {
                                    newCounter.killCount = condition.value; // Condition is fulfilled so just set count to max 
                                    newCounter.completed = true;
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if(conditionCounterSaveData != null)
                                    {
                                        newCounter.killCount = (int)conditionCounterSaveData["killCount"];
                                        newCounter.completed = newCounter.killCount >= condition.value;
                                    }
                                    // else, no data but is not dependent on any other task or condition, so counter is at 0

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }
                                }
                                break;
                            case "Shots":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.Shots;
                                List<string> bodyParts = counter["_props"]["bodyPart"].ToObject<List<string>>();
                                newCounter.counterConditionTargetBodyParts = new List<TraderTaskCounterCondition.CounterConditionTargetBodyPart>();
                                foreach (string bodyPartString in bodyParts)
                                {
                                    switch (bodyPartString)
                                    {
                                        case "Head":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.Head);
                                            break;
                                        case "Thorax":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.Thorax);
                                            break;
                                        case "Stomach":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.Stomach);
                                            break;
                                        case "LeftArm":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftArm);
                                            break;
                                        case "RightArm":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightArm);
                                            break;
                                        case "LeftLeg":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.LeftLeg);
                                            break;
                                        case "RightLeg":
                                            newCounter.counterConditionTargetBodyParts.Add(TraderTaskCounterCondition.CounterConditionTargetBodyPart.RightLeg);
                                            break;
                                        default:
                                            Mod.LogError("Task: " + task.ID + " has condition: " + condition.ID + " with shots counter with unhandled target type: " + counter["_props"]["target"].ToString());
                                            break;
                                    }
                                }
                                if (counter["_props"]["target"].ToString().Equals("Savage"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Scav;
                                }
                                else if (counter["_props"]["target"].ToString().Equals("AnyPmc"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC;
                                }
                                else if (counter["_props"]["target"].ToString().Equals("Usec"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Usec;
                                }
                                else if (counter["_props"]["target"].ToString().Equals("Bear"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Bear;
                                }
                                else if (counter["_props"]["target"].ToString().Equals("Any"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.Any;
                                }
                                else
                                {
                                    Mod.LogError("Task: " + task.ID + " has condition: " + condition.ID + " with kill counter with unhandled target type: " + counter["_props"]["target"].ToString());
                                }
                                if (counter["_props"]["weapon"] != null)
                                {
                                    newCounter.allowedWeaponIDs = new List<string>();
                                    List<string> originalIDs = counter["_props"]["weapon"].ToObject<List<string>>();
                                    for (int i = 0; i < originalIDs.Count; ++i)
                                    {
                                        if (Mod.itemMap.ContainsKey(originalIDs[i]))
                                        {
                                            ItemMapEntry itemMapEntry = Mod.itemMap[originalIDs[i]];
                                            switch (itemMapEntry.mode)
                                            {
                                                case 0:
                                                    newCounter.allowedWeaponIDs.Add(itemMapEntry.ID);
                                                    break;
                                                case 1:
                                                    newCounter.allowedWeaponIDs.AddRange(itemMapEntry.modulIDs);
                                                    break;
                                                case 2:
                                                    newCounter.allowedWeaponIDs.Add(itemMapEntry.otherModID);
                                                    break;
                                            }
                                        }
                                        //else Item is either missing or this is a category of item
                                    }
                                }
                                if (counter["_props"]["distance"] != null) 
                                {
                                    newCounter.distance = (float)counter["_props"]["distance"]["value"];
                                    if (counter["_props"]["distance"]["compareMethod"].ToString().Equals("<="))
                                    {
                                        newCounter.distanceCompareMode = 1;
                                    }
                                }

                                if (condition.fulfilled)
                                {
                                    newCounter.shotCount = condition.value; // Condition is fulfilled so just set count to max 
                                    newCounter.completed = true;
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if(conditionCounterSaveData != null && conditionSaveData["shotCount"] != null)
                                    {
                                        newCounter.shotCount = (int)conditionCounterSaveData["shotCount"];
                                        newCounter.completed = newCounter.shotCount > condition.value;
                                    }
                                    // else, no data but is not dependent on any other task or condition, so counter is at 0

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }

                                    foreach (TraderTaskCounterCondition.CounterConditionTargetBodyPart bodyPart in newCounter.counterConditionTargetBodyParts)
                                    {
                                        if (Mod.taskShotsCounterConditionsByBodyPart.ContainsKey(bodyPart))
                                        {
                                            Mod.taskShotsCounterConditionsByBodyPart[bodyPart].Add(newCounter);
                                        }
                                        else
                                        {
                                            Mod.taskShotsCounterConditionsByBodyPart.Add(bodyPart, new List<TraderTaskCounterCondition>() { newCounter });
                                        }
                                    }
                                }
                                break;
                            case "ExitStatus":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.ExitStatus;
                                newCounter.counterConditionTargetExitStatuses = new List<TraderTaskCounterCondition.CounterConditionTargetExitStatus>();
                                foreach(string raidState in counter["_props"]["status"])
                                {
                                    newCounter.counterConditionTargetExitStatuses.Add((TraderTaskCounterCondition.CounterConditionTargetExitStatus)Enum.Parse(typeof(TraderTaskCounterCondition.CounterConditionTargetExitStatus), raidState));
                                }

                                if (condition.fulfilled)
                                {
                                    newCounter.completed = true; // Condition is fulfilled so just set as completed
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if (conditionCounterSaveData != null)
                                    {
                                        newCounter.completed = (bool)conditionCounterSaveData["completed"];
                                    }
                                    // else, no data but is not dependent on any other task or condition, so not completed

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }
                                }
                                break;
                            case "VisitPlace":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.VisitPlace;
                                newCounter.targetPlaceName = counter["_props"]["target"].ToString();

                                if (condition.fulfilled)
                                {
                                    newCounter.completed = true; // Condition is fulfilled so just set as completed
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if (conditionCounterSaveData != null)
                                    {
                                        newCounter.completed = (bool)conditionCounterSaveData["completed"];
                                    }
                                    // else, no data but is not dependent on any other task or condition, so not completed

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }
                                }

                                if (Mod.taskVisitPlaceCounterConditionsByZone.ContainsKey(newCounter.targetPlaceName))
                                {
                                    Mod.taskVisitPlaceCounterConditionsByZone[newCounter.targetPlaceName].Add(newCounter);
                                }
                                else
                                {
                                    Mod.taskVisitPlaceCounterConditionsByZone.Add(newCounter.targetPlaceName, new List<TraderTaskCounterCondition>() { newCounter });
                                }
                                break;
                            case "Location":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.Location;
                                newCounter.counterConditionLocations = new List<TraderTaskCounterCondition.CounterConditionLocation>();
                                foreach (string targetLocation in counter["_props"]["target"])
                                {
                                    newCounter.counterConditionLocations.Add((TraderTaskCounterCondition.CounterConditionLocation)Enum.Parse(typeof(TraderTaskCounterCondition.CounterConditionLocation), targetLocation));

                                    if (task.location.Equals("Any"))
                                    {
                                        switch(newCounter.counterConditionLocations[newCounter.counterConditionLocations.Count - 1])
                                        {
                                            case TraderTaskCounterCondition.CounterConditionLocation.factory4_day:
                                            case TraderTaskCounterCondition.CounterConditionLocation.factory4_night:
                                                task.location = "Factory";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.laboratory:
                                                task.location = "Laboratory";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.RezervBase:
                                                task.location = "Reserve";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Lighthouse:
                                                task.location = "Lighthouse";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Shoreline:
                                                task.location = "Shoreline";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.bigmap:
                                                task.location = "Customs";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Interchange:
                                                task.location = "Interchange";
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Woods:
                                                task.location = "Woods";
                                                break;
                                            default:
                                                task.location = "Special";
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        switch (newCounter.counterConditionLocations[newCounter.counterConditionLocations.Count - 1])
                                        {
                                            case TraderTaskCounterCondition.CounterConditionLocation.factory4_day:
                                            case TraderTaskCounterCondition.CounterConditionLocation.factory4_night:
                                                if (!task.location.Equals("Factory"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.laboratory:
                                                if (!task.location.Equals("Laboratory"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.RezervBase:
                                                if (!task.location.Equals("Reserve"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Lighthouse:
                                                if (!task.location.Equals("Lighthouse"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Shoreline:
                                                if (!task.location.Equals("Shoreline"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.bigmap:
                                                if (!task.location.Equals("Customs"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Interchange:
                                                if (!task.location.Equals("Interchange"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            case TraderTaskCounterCondition.CounterConditionLocation.Woods:
                                                if (!task.location.Equals("Woods"))
                                                {
                                                    task.location = "Multiple";
                                                }
                                                break;
                                            default:
                                                task.location = "Multiple";
                                                break;
                                        }
                                    }
                                }
                                // This is a constraint condition, it does not have live data of its own, so no loading save data

                                if (addToList)
                                {
                                    AddConditionToList(startCondition, true, null, newCounter);
                                }
                                break;
                            case "HealthEffect":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.HealthEffect;
                                string effectName = counter["_props"]["bodyPartsWithEffects"][0]["effects"][0].ToString();
                                if (effectName.Equals("Stimulator"))
                                {
                                    newCounter.stimulatorEffect = true;
                                }
                                else
                                {
                                    newCounter.effectType = (Effect.EffectType)Enum.Parse(typeof(Effect.EffectType), effectName);
                                }

                                if (condition.fulfilled)
                                {
                                    newCounter.completed = true; // Condition is fulfilled so just set as completed
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if (conditionCounterSaveData != null)
                                    {
                                        newCounter.completed = (bool)conditionCounterSaveData["completed"];
                                    }
                                    // else, no data but is not dependent on any other task or condition, so not completed

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }
                                }
                                // No need to load timer, timer will always be at 0 since we are loading the game

                                if (Mod.taskHealthEffectCounterConditionsByEffectType.ContainsKey(newCounter.effectType))
                                {
                                    Mod.taskHealthEffectCounterConditionsByEffectType[newCounter.effectType].Add(newCounter);
                                }
                                else
                                {
                                    Mod.taskHealthEffectCounterConditionsByEffectType.Add(newCounter.effectType, new List<TraderTaskCounterCondition>() { newCounter });
                                }
                                break;
                            case "Equipment":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.Equipment;
                                if(counter["_props"]["equipmentExclusive"] != null)
                                {
                                    newCounter.equipmentExclusive = new List<string>();
                                    foreach(JArray equipArray in counter["_props"]["equipmentExclusive"])
                                    {
                                        string equipID = equipArray[equipArray.Count - 1].ToString();
                                        if (Mod.itemMap.ContainsKey(equipID))
                                        {
                                            ItemMapEntry itemMapEntry = Mod.itemMap[equipID];
                                            if (itemMapEntry.mode == 0 && (itemMapEntry.ID == null || itemMapEntry.ID.ToString().Equals("")))
                                            {
                                                continue;
                                            }
                                            switch (itemMapEntry.mode)
                                            {
                                                case 0:
                                                    newCounter.equipmentExclusive.Add(itemMapEntry.ID);
                                                    break;
                                                case 1:
                                                    newCounter.equipmentExclusive.AddRange(itemMapEntry.modulIDs);
                                                    break;
                                                case 2:
                                                    newCounter.equipmentExclusive.Add(itemMapEntry.otherModID);
                                                    break;
                                            }
                                        }
                                        // else there is some equipment that isnt implemented (yet?)
                                    }
                                }
                                if(counter["_props"]["equipmentInclusive"] != null)
                                {
                                    newCounter.equipmentInclusive = new List<string>();
                                    foreach(JArray equipArray in counter["_props"]["equipmentInclusive"])
                                    {
                                        string equipID = equipArray[equipArray.Count - 1].ToString();
                                        if (Mod.itemMap.ContainsKey(equipID))
                                        {
                                            ItemMapEntry itemMapEntry = Mod.itemMap[equipID];
                                            switch (itemMapEntry.mode)
                                            {
                                                case 0:
                                                    newCounter.equipmentInclusive.Add(itemMapEntry.ID);
                                                    break;
                                                case 1:
                                                    newCounter.equipmentInclusive.AddRange(itemMapEntry.modulIDs);
                                                    break;
                                                case 2:
                                                    newCounter.equipmentInclusive.Add(itemMapEntry.otherModID);
                                                    break;
                                            }
                                        }
                                        // else there is some equipment that isnt implemented (yet?)
                                    }
                                }
                                // This is a constraint condition, it does not have live data of its own, so no loading save data

                                // Only add to list of not already fulfilled
                                if (addToList)
                                {
                                    AddConditionToList(startCondition, true, null, newCounter);
                                }
                                break;
                            case "InZone":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.InZone;
                                newCounter.zoneIDs = counter["_props"]["zoneIds"].ToObject<List<string>>();
                                // This is a constraint condition, it does not have live data of its own, so no loading save data

                                // Only add to list of not already fulfilled
                                if (addToList)
                                {
                                    AddConditionToList(startCondition, true, null, newCounter);
                                }
                                break;
                            case "UseItem":
                                newCounter.counterConditionType = TraderTaskCounterCondition.CounterConditionType.UseItem;
                                newCounter.itemIDs = new List<string>();
                                List<string> useItemOriginalIDs = counter["_props"]["target"].ToObject<List<string>>();
                                newCounter.useCountCompareMode = counter["_props"]["compareMethod"].ToString().Equals("<=") ? 1 : 0;
                                for (int i=0; i < newCounter.itemIDs.Count; ++i)
                                {
                                    if (Mod.itemMap.ContainsKey(useItemOriginalIDs[i]))
                                    {
                                        ItemMapEntry itemMapEntry = Mod.itemMap[useItemOriginalIDs[i]];
                                        switch (itemMapEntry.mode)
                                        {
                                            case 0:
                                                newCounter.itemIDs.Add(itemMapEntry.ID);
                                                break;
                                            case 1:
                                                newCounter.itemIDs.AddRange(itemMapEntry.modulIDs);
                                                break;
                                            case 2:
                                                newCounter.itemIDs.Add(itemMapEntry.otherModID);
                                                break;
                                        }
                                    }
                                    // else the item is either missing or is a category
                                }

                                if (condition.fulfilled)
                                {
                                    newCounter.useCount = condition.value;
                                    newCounter.completed = true; // Condition is fulfilled so just set as completed
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if (conditionCounterSaveData != null)
                                    {
                                        newCounter.useCount = (int)conditionCounterSaveData["useCount"];
                                        newCounter.completed = newCounter.useCountCompareMode == 0 ? (newCounter.useCount >= condition.value):(newCounter.useCount <= condition.value);
                                    }
                                    // else, no data but is not dependent on any other task or condition, so not completed

                                    // Only add to list of not already fulfilled
                                    if (addToList)
                                    {
                                        AddConditionToList(startCondition, true, null, newCounter);
                                    }
                                }

                                // Only add to list of not already fulfilled
                                if (addToList)
                                {
                                    AddConditionToList(startCondition, true, null, newCounter);
                                }
                                break;
                            default:
                                Mod.LogError("Trader " + index + " buildtask: " + task.ID + ", condition: " + condition.ID + ", unhandled counter type: " + counter["_parent"].ToString());
                                return false;
                        }
                        condition.counters.Add(newCounter);
                    }
                    break;
                case "Level":
                    condition.conditionType = TraderTaskCondition.ConditionType.Level;
                    condition.value = (int)conditionData["_props"]["value"];
                    if (conditionData["_props"]["compareMethod"].ToString().Equals("<="))
                    {
                        condition.mode = 1;
                    }
                    break;
                case "Quest":
                    condition.conditionType = TraderTaskCondition.ConditionType.Quest;
                    condition.value = (int)conditionData["_props"]["status"][0];
                    string targetTaskID = conditionData["_props"]["target"].ToString();
                    if (foundTasks.ContainsKey(targetTaskID))
                    {
                        condition.target = foundTasks[targetTaskID];
                    }
                    else
                    {
                        if (waitingQuestConditions.ContainsKey(targetTaskID))
                        {
                            waitingQuestConditions[targetTaskID].Add(condition);
                        }
                        else
                        {
                            waitingQuestConditions.Add(targetTaskID, new List<TraderTaskCondition> { condition });
                        }
                    }

                    // Add to lists
                    if (questConditionsByTask == null)
                    {
                        questConditionsByTask = new Dictionary<string, List<TraderTaskCondition>>();
                    }
                    if (questConditionsByTask.ContainsKey(targetTaskID))
                    {
                        questConditionsByTask[targetTaskID].Add(condition);
                    }
                    else
                    {
                        questConditionsByTask.Add(targetTaskID, new List<TraderTaskCondition>() { condition });
                    }
                    break;
                case "TraderLoyalty":
                    condition.conditionType = TraderTaskCondition.ConditionType.TraderLoyalty;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.targetTraderIndex = IDToIndex(conditionData["_props"]["target"].ToString());
                    break;
                case "HandoverItem":
                    condition.conditionType = TraderTaskCondition.ConditionType.HandoverItem;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.dogtagLevel = conditionData["_props"]["dogtagLevel"] != null ? (int)conditionData["_props"]["dogtagLevel"] : -1;
                    string originalItemID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalItemID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalItemID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                condition.items = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                condition.items = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                condition.items = new string[] { itemMapEntry.otherModID };
                                break;
                        }
                        foreach (string item in condition.items)
                        {
                            if (conditionsByItem.ContainsKey(item))
                            {
                                conditionsByItem[item].Add(condition);
                            }
                            else
                            {
                                conditionsByItem.Add(item, new List<TraderTaskCondition>() { condition });
                            }
                            if (Mod.itemNames.ContainsKey(item))
                            {
                                condition.text = "Hand over " + condition.value + " " + Mod.itemNames[item];
                            }
                        }
                    }
                    else
                    {
                        Mod.LogError("Quest " + task.name + " with ID " + task.ID + " has has missing condition item: " + originalItemID);
                        return false;
                    }
                    break;
                case "FindItem":
                    condition.conditionType = TraderTaskCondition.ConditionType.FindItem;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.dogtagLevel = conditionData["_props"]["dogtagLevel"] != null ? (int)conditionData["_props"]["dogtagLevel"] : -1;
                    string originalFindItemID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalFindItemID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalFindItemID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                condition.items = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                condition.items = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                condition.items = new string[] { itemMapEntry.otherModID };
                                break;
                        }
                        foreach (string item in condition.items)
                        {
                            if (conditionsByItem.ContainsKey(item))
                            {
                                conditionsByItem[item].Add(condition);
                            }
                            else
                            {
                                conditionsByItem.Add(item, new List<TraderTaskCondition>() { condition });
                            }
                            if (Mod.itemNames.ContainsKey(item))
                            {
                                condition.text = "Find in raid " + condition.value + " " + Mod.itemNames[item];
                            }

                            if (!condition.fulfilled)
                            {
                                if (Mod.taskFindItemConditionsByItemID.ContainsKey(item))
                                {
                                    Mod.taskFindItemConditionsByItemID[item].Add(condition);
                                }
                                else
                                {
                                    Mod.taskFindItemConditionsByItemID.Add(item, new List<TraderTaskCondition>() { condition });
                                }
                            }
                        }
                    }
                    else
                    {
                        Mod.LogError("Quest " + task.name + " with ID " + task.ID + " has has missing condition item: " + originalFindItemID);
                        return false;
                    }
                    break;
                case "PlaceBeacon":
                case "LeaveItemAtLocation":
                    condition.conditionType = TraderTaskCondition.ConditionType.LeaveItemAtLocation;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.plantTime = conditionData["_props"]["plantTime"] != null ? (float)conditionData["_props"]["plantTime"] : 0;
                    condition.locationID = conditionData["_props"]["zoneId"].ToString();
                    string originalLeaveItemID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalLeaveItemID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalLeaveItemID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                condition.items = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                condition.items = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                condition.items = new string[] { itemMapEntry.otherModID };
                                break;
                        }

                        foreach (string item in condition.items)
                        {
                            if (conditionsByItem.ContainsKey(item))
                            {
                                conditionsByItem[item].Add(condition);
                            }
                            else
                            {
                                conditionsByItem.Add(item, new List<TraderTaskCondition>() { condition });
                            }

                            if (Mod.taskLeaveItemConditionsByItemIDByZone.ContainsKey(condition.locationID))
                            {
                                if (Mod.taskLeaveItemConditionsByItemIDByZone[condition.locationID].ContainsKey(item))
                                {
                                    Mod.taskLeaveItemConditionsByItemIDByZone[condition.locationID][item].Add(condition);
                                }
                                else
                                {
                                    Mod.taskLeaveItemConditionsByItemIDByZone[condition.locationID].Add(item, new List<TraderTaskCondition>() { condition });
                                }
                            }
                            else
                            {
                                Dictionary<string, List<TraderTaskCondition>> conditionsByItemID = new Dictionary<string, List<TraderTaskCondition>>();
                                conditionsByItemID.Add(item, new List<TraderTaskCondition>() { condition });
                                Mod.taskLeaveItemConditionsByItemIDByZone.Add(condition.locationID, conditionsByItemID);
                            }
                        }
                    }
                    else
                    {
                        Mod.LogError("Quest " + task.name + " with ID " + task.ID + " has missing leave condition item: " + originalLeaveItemID);
                        return false;
                    }
                    string zoneLocation = condition.locationID.Split('_')[0];
                    if (task.location.Equals("Any"))
                    {
                        task.location = zoneLocation;
                    }
                    else if(!task.location.Equals(zoneLocation))
                    {
                        task.location = "Multiple";
                    }
                    break;
                case "Skill":
                    condition.conditionType = TraderTaskCondition.ConditionType.Skill;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.skillIndex = Mod.SkillNameToIndex(conditionData["_props"]["target"].ToString());

                    if (!condition.fulfilled)
                    {
                        if (Mod.taskSkillConditionsBySkillIndex.ContainsKey(condition.skillIndex))
                        {
                            Mod.taskSkillConditionsBySkillIndex[condition.skillIndex].Add(condition);
                        }
                        else
                        {
                            Mod.taskSkillConditionsBySkillIndex.Add(condition.skillIndex, new List<TraderTaskCondition>() { condition });
                        }
                    }
                    break;
                case "VisitPlace":
                    condition.conditionType = TraderTaskCondition.ConditionType.VisitPlace;
                    condition.targetPlaceName = conditionData["_props"]["target"].ToString(); 

                    if (Mod.taskVisitPlaceConditionsByZone.ContainsKey(condition.targetPlaceName))
                    {
                        Mod.taskVisitPlaceConditionsByZone[condition.targetPlaceName].Add(condition);
                    }
                    else
                    {
                        Mod.taskVisitPlaceConditionsByZone.Add(condition.targetPlaceName, new List<TraderTaskCondition>() { condition });
                    }
                    break;
                case "WeaponAssembly":
                    condition.conditionType = TraderTaskCondition.ConditionType.WeaponAssembly;
                    condition.targetAttachmentTypes = conditionData["_props"]["targetAttachmentTypes"].ToObject<List<List<string>>>();
                    condition.targetAttachments = conditionData["_props"]["targetAttachments"].ToObject<List<string>>();
                    condition.suppressed = (bool)conditionData["_props"]["suppressed"];
                    condition.braked = (bool)conditionData["_props"]["braked"];
                    string originalTargetWeaponID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalTargetWeaponID))
                    {
                        ItemMapEntry itemMapEntry = Mod.itemMap[originalTargetWeaponID];
                        switch (itemMapEntry.mode)
                        {
                            case 0:
                                condition.targetWeapons = new string[] { itemMapEntry.ID };
                                break;
                            case 1:
                                condition.targetWeapons = itemMapEntry.modulIDs;
                                break;
                            case 2:
                                condition.targetWeapons = new string[] { itemMapEntry.otherModID };
                                break;
                        }

                        foreach (string weapon in condition.targetWeapons)
                        {
                            if (conditionsByItem.ContainsKey(weapon))
                            {
                                conditionsByItem[weapon].Add(condition);
                            }
                            else
                            {
                                conditionsByItem.Add(weapon, new List<TraderTaskCondition>() { condition });
                            }
                        }
                    }
                    else
                    {
                        Mod.LogError("Quest " + task.name + " with ID " + task.ID + " has has missing condition item: " + originalTargetWeaponID);
                        return false;
                    }
                    break;
                default:
                    Mod.LogError("Quest " + task.name + " with ID " + task.ID + " has unexpected condition type: " + conditionData["_parent"].ToString());
                    return false;
            }

            if (foundTaskConditions.ContainsKey(condition.ID))
            {
                foundTaskConditions[condition.ID].Add(condition);
            }
            else
            {
                foundTaskConditions.Add(condition.ID, new List<TraderTaskCondition>() { condition });
            }

            // Fill visibility conditions
            if (conditionData["_props"]["visibilityConditions"] != null && ((JArray)conditionData["_props"]["visibilityConditions"]).Count > 0)
            {
                condition.visibilityConditions = new List<TraderTaskCondition>();
                foreach(JObject visibilityConditionData in conditionData["_props"]["visibilityConditions"])
                {
                    string target = visibilityConditionData["_props"]["target"].ToString();
                    if (foundTasks.ContainsKey(target))
                    {
                        condition.visibilityConditions.AddRange(foundTaskConditions[target]);
                    }
                    else
                    {
                        if (waitingVisibilityConditions.ContainsKey(target))
                        {
                            waitingVisibilityConditions[target].Add(condition);
                        }
                        else
                        {
                            waitingVisibilityConditions.Add(target, new List<TraderTaskCondition>() { condition });
                        }
                    }

                    // Add to lists
                    if (conditionsByVisCond == null)
                    {
                        conditionsByVisCond = new Dictionary<string, List<TraderTaskCondition>>();
                    }
                    if (conditionsByVisCond.ContainsKey(target))
                    {
                        conditionsByVisCond[target].Add(condition);
                    }
                    else
                    {
                        conditionsByVisCond.Add(target, new List<TraderTaskCondition>() { condition });
                    }
                }
            }

            if (waitingVisibilityConditions.ContainsKey(condition.ID))
            {
                foreach (TraderTaskCondition visibilityCondition in waitingVisibilityConditions[condition.ID])
                {
                    if(visibilityCondition.visibilityConditions == null)
                    {
                        visibilityCondition.visibilityConditions = new List<TraderTaskCondition>();
                    }
                    visibilityCondition.visibilityConditions.Add(condition);
                }
                waitingQuestConditions.Remove(condition.ID);
            }
            
            return true;
        }

        public static void AddConditionToList(bool start, bool counter, TraderTaskCondition condition, TraderTaskCounterCondition counterCondition = null)
        {
            if (start)
            {
                if (counter)
                {
                    if (Mod.taskStartCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
                    {
                        Mod.taskStartCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
                    }
                    else
                    {
                        List<TraderTaskCounterCondition> newList = new List<TraderTaskCounterCondition>();
                        Mod.taskStartCounterConditionsByType.Add(counterCondition.counterConditionType, newList);
                        newList.Add(counterCondition);
                    }
                }
                else
                {
                    if (Mod.taskStartConditionsByType.ContainsKey(condition.conditionType))
                    {
                        Mod.taskStartConditionsByType[condition.conditionType].Add(condition);
                    }
                    else
                    {
                        List<TraderTaskCondition> newList = new List<TraderTaskCondition>();
                        Mod.taskStartConditionsByType.Add(condition.conditionType, newList);
                        newList.Add(condition);
                    }
                }
            }
            else // Fail condition
            {
                if (counter)
                {
                    if (Mod.taskFailCounterConditionsByType.ContainsKey(counterCondition.counterConditionType))
                    {
                        Mod.taskFailCounterConditionsByType[counterCondition.counterConditionType].Add(counterCondition);
                    }
                    else
                    {
                        List<TraderTaskCounterCondition> newList = new List<TraderTaskCounterCondition>();
                        Mod.taskFailCounterConditionsByType.Add(counterCondition.counterConditionType, newList);
                        newList.Add(counterCondition);
                    }
                }
                else
                {
                    if (Mod.taskFailConditionsByType.ContainsKey(condition.conditionType))
                    {
                        Mod.taskFailConditionsByType[condition.conditionType].Add(condition);
                    }
                    else
                    {
                        List<TraderTaskCondition> newList = new List<TraderTaskCondition>();
                        Mod.taskFailConditionsByType.Add(condition.conditionType, newList);
                        newList.Add(condition);
                    }
                }
            }
        }

        public static void UpdateConditionVisibility(string conditionID)
        {
            if (TraderStatus.conditionsByVisCond.ContainsKey(conditionID))
            {
                foreach (TraderTaskCondition dependentCondition in TraderStatus.conditionsByVisCond[conditionID])
                {
                    bool fulfilled = true;
                    foreach (TraderTaskCondition visibilityCondition in dependentCondition.visibilityConditions)
                    {
                        if (!visibilityCondition.fulfilled)
                        {
                            fulfilled = false;
                            break;
                        }
                    }
                    if (fulfilled)
                    {
                        dependentCondition.visible = true;
                        if (dependentCondition.marketListElement != null)
                        {
                            dependentCondition.marketListElement.SetActive(true);
                        }
                        if (dependentCondition.statusListElement != null)
                        {
                            dependentCondition.statusListElement.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        public static void UpdateTaskCompletion(TraderTask task)
        {
            // Check if failed first
            bool failed = true;
            foreach (TraderTaskCondition condition in task.failConditions)
            {
                if (!condition.fulfilled)
                {
                    failed = false;
                    break;
                }
            }

            if (failed)
            {
                task.taskState = TraderTask.TaskState.Fail;
                if (Mod.currentLocationIndex == 1) 
                {
                    if (task.marketListElement != null)
                    {
                        foreach (TraderTaskCondition condition in task.startConditions)
                        {
                            condition.marketListElement = null;
                        }
                        foreach (TraderTaskCondition condition in task.completionConditions)
                        {
                            condition.marketListElement = null;
                        }
                        foreach (TraderTaskCondition condition in task.failConditions)
                        {
                            condition.marketListElement = null;
                        }

                        GameObject.Destroy(task.marketListElement);
                        task.marketListElement = null;

                        HideoutController.instance.marketManager.UpdateTaskListHeight();
                    }
                    if (task.statusListElement != null)
                    {
                        foreach (TraderTaskCondition condition in task.startConditions)
                        {
                            condition.statusListElement = null;
                        }
                        foreach (TraderTaskCondition condition in task.completionConditions)
                        {
                            condition.statusListElement = null;
                        }
                        foreach (TraderTaskCondition condition in task.failConditions)
                        {
                            condition.statusListElement = null;
                        }

                        GameObject.Destroy(task.statusListElement);
                        task.statusListElement = null;

                        StatusUI.instance.UpdateTaskListHeight();
                    }

                    if(task.failureRewards != null)
                    {
                        HideoutController.instance.marketManager.GivePlayerRewards(task.failureRewards);
                    }
                }
                else // In raid
                {
                    if (task.failureRewards != null)
                    {
                        // Ensure player gets failure rewards when they get back from raid
                        if (Mod.rewardsToGive == null)
                        {
                            Mod.rewardsToGive = new List<List<TraderTaskReward>>();
                        }

                        Mod.rewardsToGive.Add(task.failureRewards);
                    }
                }
                if(task.statusListElement != null)
                {
                    GameObject.Destroy(task.statusListElement);
                    task.statusListElement = null;

                    StatusUI.instance.UpdateTaskListHeight();
                }
            }
            else
            {
                bool complete = true;
                foreach (TraderTaskCondition condition in task.completionConditions)
                {
                    if (!condition.fulfilled)
                    {
                        complete = false;
                        break;
                    }
                }

                if (complete)
                {
                    task.taskState = TraderTask.TaskState.Complete;
                    if (Mod.currentLocationIndex == 1) // If in base, we want to update market list element if it exists
                    {
                        if (task.marketListElement != null)
                        {
                            task.marketListElement.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                            task.marketListElement.transform.GetChild(0).GetChild(4).gameObject.SetActive(true);
                            task.marketListElement.transform.GetChild(0).GetChild(5).gameObject.SetActive(false);
                            task.marketListElement.transform.GetChild(0).GetChild(7).gameObject.SetActive(true);
                        }
                    }
                    if (task.statusListElement != null)
                    {
                        task.marketListElement.transform.GetChild(0).GetChild(2).gameObject.SetActive(false);
                        task.marketListElement.transform.GetChild(0).GetChild(3).gameObject.SetActive(true);
                    }
                }
            }
        }

        public static void UpdateTaskAvailability(TraderTask task)
        {
            bool available = true;
            foreach (TraderTaskCondition condition in task.startConditions)
            {
                if (!condition.fulfilled)
                {
                    available = false;
                    break;
                }
            }

            if (available)
            {
                task.taskState = TraderTask.TaskState.Available;

                // Add to UI
                if(Mod.currentLocationIndex == 1 && HideoutController.instance.marketManager.currentTraderIndex == task.ownerTraderIndex)
                {
                    HideoutController.instance.marketManager.AddTask(task);
                }
            }
        }

        public static void FulfillCondition(TraderTaskCondition condition)
        {
            condition.fulfilled = true;

            // Make visible any conditions that have a UI element and that have all their visibility conditions fulfilled
            TraderStatus.UpdateConditionVisibility(condition.ID);

            // Update tasks dependent on this condition's fulfillment
            if (condition.task.taskState == TraderTask.TaskState.Locked)
            {
                TraderStatus.UpdateTaskAvailability(condition.task);
            }
            //else if (condition.task.taskState == TraderTask.TaskState.Active)
            //{
                // TODO: Review, some quests we want to be able to complete before they are unlocked (TraderLoyalty/Skill/Level quests for example)
                // Some we also want to be able to fail before they are unlocked or active (Being able to take some quests can be dependent on whether we did another particular quest first)
                // Not being able to fail certain types of quests before they are unlocked or started is handled by the conditions
                // The relevant ones will simply not work towards fulfillment if they are not a type that should
                TraderStatus.UpdateTaskCompletion(condition.task);
            //}
        }

        public static bool CheckCounterConditionConstraint(TraderTaskCounterCondition counterCondition)
        {
            switch (counterCondition.counterConditionType)
            {
                case TraderTaskCounterCondition.CounterConditionType.Location:
                    return counterCondition.counterConditionLocations == null ||
                            (Mod.currentLocationIndex == 2 && counterCondition.counterConditionLocations.Contains((TraderTaskCounterCondition.CounterConditionLocation)Mod.chosenMapIndex));
                case TraderTaskCounterCondition.CounterConditionType.InZone:
                    return counterCondition.zoneIDs == null || counterCondition.zoneIDs.Count == 0 || (Mod.playerStatusManager.currentZone != null && counterCondition.zoneIDs.Contains(Mod.playerStatusManager.currentZone));
                case TraderTaskCounterCondition.CounterConditionType.HealthEffect:
                    if(Mod.currentLocationIndex == 2)
                    {
                        foreach(Effect effect in Effect.effects)
                        {
                            if(effect.effectType == counterCondition.effectType && effect.fromStimulator == counterCondition.stimulatorEffect)
                            {
                                return true;
                            }
                        }
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.Equipment:
                    if ((!EquipmentSlot.wearingBackpack || !Mod.IDDescribedInList(EquipmentSlot.currentBackpack.ID, EquipmentSlot.currentBackpack.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingBodyArmor || !Mod.IDDescribedInList(EquipmentSlot.currentArmor.ID, EquipmentSlot.currentArmor.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingEarpiece || !Mod.IDDescribedInList(EquipmentSlot.currentEarpiece.ID, EquipmentSlot.currentEarpiece.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingHeadwear || !Mod.IDDescribedInList(EquipmentSlot.currentHeadwear.ID, EquipmentSlot.currentHeadwear.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingFaceCover || !Mod.IDDescribedInList(EquipmentSlot.currentFaceCover.ID, EquipmentSlot.currentFaceCover.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingEyewear || !Mod.IDDescribedInList(EquipmentSlot.currentEyewear.ID, EquipmentSlot.currentEyewear.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingRig || !Mod.IDDescribedInList(EquipmentSlot.currentRig.ID, EquipmentSlot.currentRig.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive))&&
                        (!EquipmentSlot.wearingPouch || !Mod.IDDescribedInList(EquipmentSlot.currentPouch.ID, EquipmentSlot.currentPouch.parents, counterCondition.equipmentInclusive, counterCondition.equipmentExclusive)))
                    {
                        return false;
                    }
                    break;
            }

            return false;
        }

        public static void UpdateCounterConditionFulfillment(TraderTaskCounterCondition counterCondition)
        {
            // Some counter conditinos are not dependent on some other variable than being complete or not
            // So this may be called right after counterCondition.completed was set to true
            // So for some types we need to check that and can't assume we dont have to do anything if already true
            switch (counterCondition.counterConditionType)
            {
                case TraderTaskCounterCondition.CounterConditionType.Kills:
                    if (counterCondition.completed)
                    {
                        return;
                    }
                    if (counterCondition.killCount >= counterCondition.parentCondition.value)
                    {
                        counterCondition.completed = true;
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.Shots:
                    if (counterCondition.completed)
                    {
                        return;
                    }
                    if (counterCondition.shotCount >= counterCondition.parentCondition.value)
                    {
                        counterCondition.completed = true;
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.UseItem:
                    if (counterCondition.completed)
                    {
                        return;
                    }
                    counterCondition.completed = counterCondition.useCountCompareMode == 0 ? (counterCondition.useCount >= counterCondition.parentCondition.value) : (counterCondition.useCount <= counterCondition.parentCondition.value);
                    if (counterCondition.completed)
                    {
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.ExitStatus:
                    // See first comment of method
                    if (counterCondition.completed)
                    {
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.VisitPlace:
                    // See first comment of method
                    if (counterCondition.completed)
                    {
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.HealthEffect:
                    // TODO: Will need to check how compare mode is used in this case
                    if(counterCondition.timer >= counterCondition.time)
                    {
                        counterCondition.completed = true;
                        UpdateConditionFulfillment(counterCondition.parentCondition);
                    }
                    break;
                case TraderTaskCounterCondition.CounterConditionType.Location:
                case TraderTaskCounterCondition.CounterConditionType.Equipment:
                    // This is a constraint condition, it does not have live data of its own, so no updating it
                    break;
            }
        }

        public static void UpdateConditionFulfillment(TraderTaskCondition condition)
        {
            // TODO: Might need to implement unfulfilling conditions by checking if the conditions are met and if not calling UnfulfillCondition or something similar
            // in order to properly update everything
            switch (condition.conditionType)
            {
                case TraderTaskCondition.ConditionType.CounterCreator:
                    bool fulfilled = true;
                    foreach (TraderTaskCounterCondition counterCondition in condition.counters)
                    {
                        if (!counterCondition.completed)
                        {
                            fulfilled = false;
                            break;
                        }
                    }
                    if (fulfilled)
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.Level:
                    if(condition.mode == 0 && Mod.level >= condition.value) // >=
                    {
                        FulfillCondition(condition);
                    }
                    else if (condition.mode == 1 && Mod.level <= condition.value) // <=
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.Quest:
                    if(condition.target.taskState == TraderTask.TaskState.Success)
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.TraderLoyalty:
                    if (condition.mode == 0 && Mod.traderStatuses[condition.targetTraderIndex].GetLoyaltyLevel() >= condition.value) // >=
                    {
                        FulfillCondition(condition);
                    }
                    else if (condition.mode == 1 && Mod.traderStatuses[condition.targetTraderIndex].GetLoyaltyLevel() <= condition.value) // <=
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.HandoverItem:
                case TraderTaskCondition.ConditionType.FindItem:
                case TraderTaskCondition.ConditionType.LeaveItemAtLocation:
                    if (condition.itemCount == condition.value)
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.Skill:
                    if(condition.value >= Mod.skills[condition.skillIndex].progress / 100)
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.VisitPlace:
                    // VisitPlace conditions are not dependent on some other variable, so to update its fulfillment, we assume that fulfilled var would be set prior to 
                    // calling this method on it, so this is all we have to check
                    if(condition.fulfilled)
                    {
                        FulfillCondition(condition);
                    }
                    break;
                case TraderTaskCondition.ConditionType.WeaponAssembly:
                    // WeaponAssembly conditions are dependent on other variables but these will be checked when
                    // the player adds a weapon to the trade volume, the weapon will be checked against all weaponAssembly type
                    // conditions and if the weapon matches one, they will be given the option to hand it in
                    // Once handed in, fulfilled will be set to true on the condition and this method called on it.
                    if (condition.fulfilled)
                    {
                        FulfillCondition(condition);
                    }
                    break;
            }
        }
    }

    public class TraderAssortment
    {
        public int level;
        public Dictionary<string, AssortmentItem> itemsByID;
    }

    public class AssortmentItem
    {
        public List<GameObject> currentShowcaseElements;

        public string ID;

        public List<List<AssortmentPriceData>> prices;

        public int stack = 1;
        public int buyRestrictionMax = -1;
        public int buyRestrictionCurrent;
    }

    public class AssortmentPriceData
    {
        public enum PriceItemType
        {
            Other,
            Dogtag
        }
        public PriceItemType priceItemType;

        public string ID;
        public int count;

        // Dogtag
        public bool USEC;
        public int dogtagLevel = -1;
    }

    public class TraderTask
    {
        public bool init;

        public GameObject marketListElement;
        public TaskUI statusListElement;

        public string ID;
        public int ownerTraderIndex;
        public string name;
        public string description;
        public string failMessage;
        public string successMessage;
        public string location;
        public enum TaskState
        {
            Locked,
            Available,
            Active,
            Complete,
            Success,
            Fail
        }
        public TaskState taskState;

        public List<TraderTaskCondition> startConditions;
        public List<TraderTaskCondition> completionConditions;
        public List<TraderTaskCondition> failConditions;
        public List<TraderTaskReward> successRewards;
        public List<TraderTaskReward> startingEquipment;
        public List<TraderTaskReward> failureRewards;

        public void Init()
        {
            bool allStartFulfilled = true;
            foreach(TraderTaskCondition condition in startConditions)
            {
                if (!condition.fulfilled)
                {
                    allStartFulfilled = false;
                    break;
                }
            }
            if (allStartFulfilled)
            {
                taskState = TaskState.Available;
            }

            init = true;
        }
    }

    public class TraderTaskCondition
    {
        public bool init;
        public TraderTask task;

        public GameObject marketListElement;
        public TaskObjectiveUI statusListElement;

        public string text;

        public enum ConditionType
        {
            CounterCreator,
            HandoverItem,
            Level,
            FindItem,
            Quest,
            LeaveItemAtLocation,
            TraderLoyalty,
            Skill,
            VisitPlace,
            WeaponAssembly
        }
        public ConditionType conditionType;
        public bool fulfilled;
        public string ID;
        public List<TraderTaskCondition> visibilityConditions;
        public bool visible = true;
        public bool failCondition;

        // Level: The level to compare with
        // Skill: The skill to compare with
        // Quest: The status the quest should be at, 2: started, 4: completed
        // TraderLoyalty: The loyalty level of that trader
        // Counter: depends on counter conditions
        // HandOverItem, FindItem, LeaveItemAtLocation: amount to hand in/find/leave at location
        public int value;

        // Level, TraderLoyalty, Skill
        public int mode; // 0: >= (Min), 1: <= (Max) 

        // Quest
        public TraderTask target;

        // TraderLoyalty
        public int targetTraderIndex;

        // HandOverItem, FindItem, LeaveItemAtLocation
        public string[] items;
        public int dogtagLevel;

        // LeaveItemAtLocation
        public string locationID;
        public float plantTime;

        public int itemCount; // Live data, count of items handed in/found/left at location

        // Counter
        public List<TraderTaskCounterCondition> counters;

        // Skill
        public int skillIndex;

        // VisitPlace
        public string targetPlaceName;

        // WeaponAssembly
        public string[] targetWeapons;
        public List<List<string>> targetAttachmentTypes; // Attachment categories that the weapon must have one of each
        public List<string> targetAttachments; // Specific attachments the weapon must have
        public bool suppressed;
        public bool braked;

        public void Init()
        {
            // Only care for quest and trader loyalty because those are the ones that are dependent on the init of other traders
            switch (conditionType)
            {
                case ConditionType.Quest:
                    if(value == 2)
                    {
                        fulfilled = target.taskState == TraderTask.TaskState.Active;
                    }
                    else if(value == 4)
                    {
                        fulfilled = target.taskState == TraderTask.TaskState.Success;
                    }
                    break;
                case ConditionType.TraderLoyalty:
                    if(mode == 0) // >=
                    {
                        fulfilled = Mod.traderStatuses[targetTraderIndex].GetLoyaltyLevel() >= value;
                    }
                    else // <=
                    {
                        fulfilled = Mod.traderStatuses[targetTraderIndex].GetLoyaltyLevel() <= value;
                    }
                    break;
                default:
                    break;
            }

            init = true;
        }
    }

    public class TraderTaskCounterCondition
    {
        public enum CounterConditionType
        {
            Kills,
            ExitStatus,
            VisitPlace,
            Location,
            HealthEffect,
            Equipment,
            InZone,
            Shots,
            UseItem
        }
        public CounterConditionType counterConditionType;
        public string ID;
        public TraderTaskCondition parentCondition;

        // Kills
        public enum CounterConditionTargetEnemy
        {
            Scav, // Savage
            PMC, // AnyPmc
            Usec,
            Bear,
            Any
        }
        public CounterConditionTargetEnemy counterConditionTargetEnemy;
        public List<string> allowedWeaponIDs;
        public List<string> weaponModsInclusive;
        public float distance = -1;
        public int distanceCompareMode; //0: >=, 1: <=

        public int killCount; // Live data

        // Shots
        public enum CounterConditionTargetBodyPart
        {
            Head,
            Thorax,
            Stomach,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg
        }
        public List<CounterConditionTargetBodyPart> counterConditionTargetBodyParts;

        public int shotCount; // Live data

        // ExitStatus
        public enum CounterConditionTargetExitStatus
        {
            Survived,
            Runner,
            Killed,
            Left,
            MissingInAction
        }
        public List<CounterConditionTargetExitStatus> counterConditionTargetExitStatuses;

        // VisitPlace
        public string targetPlaceName;

        // Location
        public enum CounterConditionLocation
        {
            factory4_day = 0,
            factory4_night = 1,
            bigmap = 2, // Customs
            Interchange = 3,
            Shoreline = 4,
            RezervBase = 5,
            Woods = 6,
            laboratory = 7,
            Lighthouse = 8,
            develop = 9
        }
        public List<CounterConditionLocation> counterConditionLocations;

        // HealthEffect
        public bool stimulatorEffect;
        public Effect.EffectType effectType;
        public float time;
        public int timeCompareMode; //0: >=, 1: <=

        public float timer; // Live data
        public bool completed; // Live data

        // Equipment
        public List<string> equipmentExclusive;
        public List<string> equipmentInclusive;

        // InZone
        public List<string> zoneIDs;

        // UseItem
        public List<string> itemIDs;
        public int useCountCompareMode;

        public int useCount; // Live data
    }

    public class TraderTaskReward
    {
        public enum TaskRewardType
        {
            Experience,
            TraderStanding,
            Item,
            TraderUnlock,
            AssortmentUnlock
        }
        public TaskRewardType taskRewardType;

        // Experience
        public int experience;

        // Standing and TraderUnlock
        public int traderIndex;

        // Standing
        public float standing;

        // Item
        public string[] itemIDs;
        public int amount;
    }
}
