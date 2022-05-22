using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_TraderStatus
    {
        public string id;
        public int index;
        public int salesSum;
        public float standing;
        public bool unlocked;
        public int currency; // 0: RUB, 1: USD

        public Dictionary<int, TraderAssortment> assortmentByLevel;
        public List<string> categories;
        public List<TraderTask> tasks;

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

        public EFM_TraderStatus(int index, int salesSum, float standing, bool unlocked, string currency, JObject assortData, JArray categoriesData, JObject questAssortData)
        {
            this.id = IndexToID(index);
            this.index = index;
            this.salesSum = salesSum;
            this.standing = standing;
            this.unlocked = unlocked;
            if (currency.Equals("USD"))
            {
                this.currency = 1;
            }

            BuildAssortments(assortData);

            categories = categoriesData.ToObject<List<string>>();

            BuildTasks(questAssortData);
        }

        public void Init(JObject data)
        {
            sdgr
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
            Mod.instance.LogInfo("getting index of trader with id: "+ID);
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
            Dictionary<string, JObject> data = assortData.ToObject<Dictionary<string, JObject>>();
            assortmentByLevel = new Dictionary<int, TraderAssortment>();
            foreach (KeyValuePair<string, JObject> entry in data)
            {
                if((entry.Value["items"] as JArray).Count > 0)
                {
                    // Only add item if we have an ID for it
                    JObject parentItem = entry.Value["items"][0] as JObject;
                    string parentItemID = parentItem["_tpl"].ToString();
                    string actualParentItemID = "";
                    if (Mod.itemMap.ContainsKey(parentItemID))
                    {
                        actualParentItemID = Mod.itemMap[parentItemID];
                    }
                    else
                    {
                        continue;
                    }

                    // Add new assort for this level or get the one already there
                    int loyaltyLevel = (int)entry.Value["loyalty"];
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
                        currentAssort.itemsByID[actualParentItemID].stack += (int)parentItem["upd"]["StackObjectsCount"];

                        Dictionary<string, int> currentPrices = new Dictionary<string, int>();
                        currentAssort.itemsByID[actualParentItemID].prices.Add(currentPrices);
                        foreach (JObject price in entry.Value["barter_scheme"])
                        {
                            currentPrices.Add(price["_tpl"].ToString(), (int)price["count"]);
                        }
                    }
                    else
                    {
                        AssortmentItem item = new AssortmentItem();
                        item.ID = actualParentItemID;
                        item.prices = new List<Dictionary<string, int>>();
                        Dictionary<string, int> currentPrices = new Dictionary<string, int>();
                        item.prices.Add(currentPrices);
                        foreach(JObject price in entry.Value["barter_scheme"])
                        {
                            currentPrices.Add(price["_tpl"].ToString(), (int)price["count"]);
                        }
                        currentAssort.itemsByID[actualParentItemID].stack = (int)parentItem["upd"]["StackObjectsCount"];
                        if(parentItem["upd"]["BuyRestrictionMax"] != null)
                        {
                            currentAssort.itemsByID[actualParentItemID].buyRestrictionMax = (int)parentItem["upd"]["BuyRestrictionMax"];
                        }
                    }
                }
            }
        }

        public bool ItemSellable(string itemID, List<string> ancestors)
        {
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

        private void BuildTasks(JObject tasksData)
        {
            tasks = new List<TraderTask>();
            Dictionary<string, string> rawTasks = tasksData["success"].ToObject<Dictionary<string, string>>();
            Dictionary<string, TraderTask> foundTasks = new Dictionary<string, TraderTask>();
            Dictionary<string, TraderTaskCondition> foundTaskConditions = new Dictionary<string, TraderTaskCondition>();
            Dictionary<string, JObject> questLocales = Mod.localDB["quest"].ToObject<Dictionary<string, JObject>>();
            Dictionary<string, List<TraderTaskCondition>> waitingQuestConditions = new Dictionary<string, List<TraderTaskCondition>>();
            Dictionary<string, List<TraderTaskCondition>> waitingVisibilityConditions = new Dictionary<string, List<TraderTaskCondition>>();

            foreach (KeyValuePair<string, string> rawTask in rawTasks)
            {
                if (foundTasks.ContainsKey(rawTask.Value))
                {
                    continue;
                }

                // Find quest in questDB
                JObject questData = null;
                foreach(JObject quest in Mod.questDB)
                {
                    if (quest["_id"].ToString().Equals(rawTask))
                    {
                        questData = quest;
                        break;
                    }
                }
                if(questData == null)
                {
                    Mod.instance.LogError("Could not find quest with ID: "+rawTask.Value+" in questDB");
                    continue;
                }

                // Find quest locale
                JObject questLocale = null;
                foreach (KeyValuePair<string, JObject> quest in questLocales)
                {
                    if (quest.Key.Equals(rawTask))
                    {
                        questLocale = quest.Value;
                        break;
                    }
                }
                if(questData == null)
                {
                    Mod.instance.LogError("Could not find quest with ID: "+rawTask.Value+" in locale");
                    continue;
                }

                TraderTask newTask = new TraderTask();
                tasks.Add(newTask);

                newTask.ID = rawTask.Value;
                newTask.name = questLocale["name"].ToString();
                newTask.description = Mod.localDB["mail"][questLocale["description"].ToString()].ToString();
                newTask.failMessage = Mod.localDB["mail"][questLocale["failMessageText"].ToString()].ToString();
                newTask.successMessage = Mod.localDB["mail"][questLocale["successMessageText"].ToString()].ToString();

                // Fill start conditions
                newTask.startConditions = new Dictionary<string, TraderTaskCondition>();
                foreach(JObject startConditionData in questData["conditions"]["AvailableForStart"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, startConditionData, foundTasks, foundTaskConditions, waitingQuestConditions, waitingVisibilityConditions))
                    {
                        Mod.instance.LogError("Quest " + newTask.name + " with ID " + newTask.ID + " has unexpected type: " + startConditionData["_parent"].ToString());
                        continue;
                    }
                    else
                    {
                        newTask.startConditions.Add(newCondition.ID, newCondition);
                    }
                }

                // Fill completion conditions
                newTask.completionConditions = new Dictionary<string, TraderTaskCondition>();
                foreach(JObject completionConditionData in questData["conditions"]["AvailableForFinish"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, completionConditionData, foundTasks, foundTaskConditions, waitingQuestConditions, waitingVisibilityConditions))
                    {
                        Mod.instance.LogError("Quest " + newTask.name + " with ID " + newTask.ID + " has unexpected type: " + completionConditionData["_parent"].ToString());
                        continue;
                    }
                    else
                    {
                        newTask.completionConditions.Add(newCondition.ID, newCondition);
                    }
                }

                // Fill fail conditions
                newTask.failConditions = new Dictionary<string, TraderTaskCondition>();
                foreach(JObject failConditionData in questData["conditions"]["Fail"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, failConditionData, foundTasks, foundTaskConditions, waitingQuestConditions, waitingVisibilityConditions))
                    {
                        Mod.instance.LogError("Quest " + newTask.name + " with ID " + newTask.ID + " has unexpected type: " + failConditionData["_parent"].ToString());
                        continue;
                    }
                    else
                    {
                        newTask.failConditions.Add(newCondition.ID, newCondition);
                    }
                }

                // Fill success rewards
                foreach(JObject rewardData in questData["rewards"]["Success"])
                {
                    TraderTaskReward newReward = new TraderTaskReward();
                    SetReward(newReward, rewardData, newTask, newTask.successRewards);
                }

                // Add task to found tasks and update condition waiting for it if any
                foundTasks.Add(rawTask.Value, newTask);
                if (waitingQuestConditions.ContainsKey(rawTask.Value))
                {
                    foreach(TraderTaskCondition condition in waitingQuestConditions[rawTask.Value])
                    {
                        condition.target = newTask;
                    }
                    waitingQuestConditions.Remove(rawTask.Value);
                }
            }
        }

        private void SetReward(TraderTaskReward reward, JObject rewardData, TraderTask task, List<TraderTaskReward> listToFill)
        {
            switch (rewardData["type"].ToString())
            {
                case "Experience":
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.Experience;
                    reward.experience = (int)rewardData["value"];
                    listToFill.Add(reward);
                    break;
                case "TraderStanding":
                    reward.taskRewardType = TraderTaskReward.TaskRewardType.TraderStanding;
                    reward.traderIndex = IDToIndex(rewardData["target"].ToString());
                    reward.standing = (float)rewardData["value"];
                    listToFill.Add(reward);
                    break;
                case "Item":
                    string originalItemID = rewardData["items"][0]["_tpl"].ToString();
                    if (Mod.itemMap.ContainsKey(originalItemID))
                    {
                        reward.taskRewardType = TraderTaskReward.TaskRewardType.Item;
                        reward.itemID = Mod.itemMap[originalItemID];
                        reward.amount = (int)rewardData["value"];
                        listToFill.Add(reward);
                    }
                    else
                    {
                        Mod.instance.LogError("Quest " + task.name + " with ID " + task.ID + " has item reward with missing item: " + originalItemID);
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

        private bool SetCondition(TraderTaskCondition condition, JObject conditionData, Dictionary<string, TraderTask> foundTasks, Dictionary<string, TraderTaskCondition> foundTaskConditions, Dictionary<string, List<TraderTaskCondition>> waitingQuestConditions, Dictionary<string, List<TraderTaskCondition>> waitingVisibilityConditions)
        {
            condition.ID = conditionData["_props"]["id"].ToString();

            switch (conditionData["_parent"].ToString())
            {
                case "CounterCreator":
                    condition.conditionType = TraderTaskCondition.ConditionType.CounterCreator;
                    condition.counters = new List<TraderTaskCountercondition>();
                    condition.value = (int)conditionData["_props"]["value"];
                    foreach (JObject counter in conditionData["_props"]["counter"]["conditions"])
                    {
                        TraderTaskCountercondition newCounter = new TraderTaskCountercondition();
                        switch (counter["_parent"].ToString())
                        {
                            case "Kills":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.Kills;
                                if (counter["_props"]["target"].ToString().Equals("Savage"))
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCountercondition.CounterConditionTargetEnemy.Scav;
                                }
                                else
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCountercondition.CounterConditionTargetEnemy.PMC;
                                }
                                if (counter["_props"]["weapon"] != null)
                                {
                                    newCounter.allowedWeaponIDs = counter["_props"]["weapon"].ToObject<List<string>>();
                                    for (int i = 0; i < newCounter.allowedWeaponIDs.Count; ++i)
                                    {
                                        if (!Mod.itemMap.ContainsKey(newCounter.allowedWeaponIDs[i]))
                                        {
                                            newCounter.allowedWeaponIDs[i] = Mod.itemMap[newCounter.allowedWeaponIDs[i]];
                                        }
                                        else
                                        {
                                            Mod.instance.LogError("Missing weapon " + newCounter.allowedWeaponIDs[i] + " for kills counter condition in quest condition: " + condition.ID);
                                        }
                                    }
                                }
                                if (counter["_props"]["weaponModsInclusive"] != null) 
                                {
                                    newCounter.weaponModsInclusive = new List<string>();
                                    foreach(JArray weaponMod in counter["_props"]["weaponModsInclusive"])
                                    {
                                        // take the last element of the inner array because that seems to be the correct one
                                        string lastElement = weaponMod[weaponMod.Count - 1].ToString();
                                        if (!Mod.itemMap.ContainsKey(lastElement))
                                        {
                                            newCounter.weaponModsInclusive.Add(Mod.itemMap[lastElement]);
                                        }
                                        else
                                        {
                                            Mod.instance.LogError("Missing weaponModsInclusive element "+ lastElement + " for kills counter condition in quest condition: " + condition.ID);
                                        }
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
                                break;
                            case "ExitStatus":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.ExitStatus;
                                newCounter.counterConditionTargetExitStatuses = new List<TraderTaskCountercondition.CounterConditionTargetExitStatus>();
                                foreach(string raidState in counter["_props"]["status"])
                                {
                                    newCounter.counterConditionTargetExitStatuses.Add((TraderTaskCountercondition.CounterConditionTargetExitStatus)Enum.Parse(typeof(TraderTaskCountercondition.CounterConditionTargetExitStatus), raidState));
                                }
                                break;
                            case "VisitPlace":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.VisitPlace;
                                newCounter.targetPlaceName = counter["_props"]["target"].ToString();
                                break;
                            case "Location":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.Location;
                                newCounter.counterConditionLocations = new List<TraderTaskCountercondition.CounterConditionLocation>();
                                foreach (string targetLocation in counter["_props"]["target"])
                                {
                                    newCounter.counterConditionLocations.Add((TraderTaskCountercondition.CounterConditionLocation)Enum.Parse(typeof(TraderTaskCountercondition.CounterConditionLocation), targetLocation));
                                }
                                break;
                            case "HealthEffect":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.HealthEffect;
                                string effectName = counter["_props"]["bodyPartsWithEffects"][0]["effects"][0].ToString();
                                if (effectName.Equals("Stimulator"))
                                {
                                    newCounter.stimulatorEffect = true;
                                }
                                else
                                {
                                    newCounter.effectType = (EFM_Effect.EffectType)Enum.Parse(typeof(EFM_Effect.EffectType), effectName);
                                }
                                break;
                            case "Equipment":
                                newCounter.counterConditionType = TraderTaskCountercondition.CounterConditionType.Equipment;
                                if(counter["_props"]["equipmentExclusive"] != null)
                                {
                                    newCounter.equipmentExclusive = new List<string>();
                                    foreach(JArray equipArray in counter["_props"]["equipmentExclusive"])
                                    {
                                        string equipID = equipArray[equipArray.Count - 1].ToString();
                                        if (Mod.itemMap.ContainsKey(equipID))
                                        {
                                            newCounter.equipmentExclusive.Add(Mod.itemMap[equipID]);
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
                                            newCounter.equipmentInclusive.Add(Mod.itemMap[equipID]);
                                        }
                                        // else there is some equipment that isnt implemented (yet?)
                                    }
                                }
                                break;
                        }
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
                    break;
                case "TraderLoyalty":
                    condition.conditionType = TraderTaskCondition.ConditionType.TraderLoyalty;
                    condition.value = (int)conditionData["_props"]["value"];
                    condition.targetTraderIndex = IDToIndex(conditionData["_props"]["target"].ToString());
                    break;
                default:
                    return false;
            }

            foundTaskConditions.Add(condition.ID, condition);

            // Fill visibility conditions
            if (((JArray)conditionData["_props"]["visibilityConditions"]).Count > 0)
            {
                condition.visibilityConditions = new List<TraderTaskCondition>();
                foreach(string target in conditionData["_props"]["visibilityConditions"])
                {
                    if (foundTasks.ContainsKey(target))
                    {
                        condition.visibilityConditions.Add(foundTaskConditions[target]);
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

        public List<Dictionary<string, int>> prices;

        public int stack = 1;
        public int buyRestrictionMax = -1;
        public int buyRestrictionCurrent;
    }

    public class TraderTask
    {
        public string ID;
        public string name;
        public string description;
        public string failMessage;
        public string successMessage;
        public enum TaskState
        {
            Locked,
            Available,
            Active,
            Success,
            Fail
        }
        public TaskState taskState;

        public Dictionary<string, TraderTaskCondition> startConditions;
        public Dictionary<string, TraderTaskCondition> completionConditions;
        public Dictionary<string, TraderTaskCondition> failConditions;
        public List<TraderTaskReward> successRewards;
        public List<TraderTaskReward> startingEquipment;
        public List<TraderTaskReward> failureRewards;
    }

    public class TraderTaskCondition
    {
        public enum ConditionType
        {
            CounterCreator,
            HandoverItem,
            Level,
            FindItem,
            Quest,
            LeaveItemAtLocation,
            TraderLoyalty
        }
        public ConditionType conditionType;
        public bool fulfilled;
        public string ID;
        public List<TraderTaskCondition> visibilityConditions;

        // Level: The level to compare with
        // Quest: The status the quest should be at, 2: started, 4: completed
        // TraderLoyalty: The loyalty level of that trader
        // Counter: depends on counter conditions
        public int value;

        // Level, TraderLoyalty
        public int mode; // 0: >= (Min), 1: <= (Max) 

        // Quest
        public TraderTask target;

        // TraderLoyalty
        public int targetTraderIndex;

        // Counter
        public List<TraderTaskCountercondition> counters;
    }

    public class TraderTaskCountercondition
    {
        public enum CounterConditionType
        {
            Kills,
            ExitStatus,
            VisitPlace,
            Location,
            HealthEffect,
            Equipment
        }
        public CounterConditionType counterConditionType;

        // Kills
        public enum CounterConditionTargetEnemy
        {
            Scav, // Savage
            PMC, // AnyPmc
            Usec,
            Bear
        }
        public CounterConditionTargetEnemy counterConditionTargetEnemy;
        public List<string> allowedWeaponIDs;
        public List<string> weaponModsInclusive;
        public float distance = -1;
        public int distanceCompareMode; //0: >=, 1: <=

        public int killCount;

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

        public bool completed;

        // Location
        public enum CounterConditionLocation
        {
            bigmap,
            Shoreline,
            RezervBase,
            Woods,
            develop,
            factory4_day,
            factory4_night,
            Interchange,
            laboratory,
            Lighthouse
        }
        public List<CounterConditionLocation> counterConditionLocations;

        // HealthEffect
        public bool stimulatorEffect;
        public EFM_Effect.EffectType effectType;
        public float time;
        public int timeCompareMode; //0: >=, 1: <=

        public float timer;

        // Equipment
        public List<string> equipmentExclusive;
        public List<string> equipmentInclusive;
    }

    public class TraderTaskReward
    {
        public enum TaskRewardType
        {
            Experience,
            TraderStanding,
            Item,
            TraderUnlock
        }
        public TaskRewardType taskRewardType;

        // Experience
        public int experience;

        // Standing and TraderUnlock
        public int traderIndex;

        // Standing
        public float standing;

        // Item
        public string itemID;
        public int amount;
    }
}
