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
        public string name;
        public int salesSum;
        public float standing;
        public bool unlocked;
        public int currency; // 0: RUB, 1: USD
        public bool offersInsurance;
        public float insuranceRate;

        public Dictionary<int, TraderAssortment> assortmentByLevel;
        public List<string> categories;
        public List<TraderTask> tasks; // TODO: Implement save
        public List<string> itemsToWaitForUnlock;

        public JToken traderData;

        public static Dictionary<string, List<TraderTaskCondition>> waitingQuestConditions;
        public static Dictionary<string, List<TraderTaskCondition>> waitingVisibilityConditions;
        public static Dictionary<string, TraderTask> foundTasks;
        public static Dictionary<string, TraderTaskCondition> foundTaskConditions;
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

        public EFM_TraderStatus(JToken traderData, int index, string name, int salesSum, float standing, bool unlocked, string currency, JObject assortData, JArray categoriesData, JObject questAssortData)
        {
            if(waitingQuestConditions == null)
            {
                foundTasks = new Dictionary<string, TraderTask>();
                foundTaskConditions = new Dictionary<string, TraderTaskCondition>();
                waitingQuestConditions = new Dictionary<string, List<TraderTaskCondition>>();
                waitingVisibilityConditions = new Dictionary<string, List<TraderTaskCondition>>();
            }

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

            BuildTasks(questAssortData);

            BuildAssortments(assortData);

            categories = categoriesData.ToObject<List<string>>();
        }

        public void Init()
        {
            // Init conditions that we didnt have save data for because some are dependent on trader loyalty or standing which hasnt been loaded yet
            // when we first build the tasks
            foreach(TraderTaskCondition condition in conditionsToInit)
            {
                if (!condition.init)
                {
                    condition.Init();
                }
            }

            // Then init tasks
            foreach (TraderTask task in tasksToInit)
            {
                if (!task.init)
                {
                    task.Init();
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
            // TODO: based on traderData["previousRestockTime"] and current time, decide whether we should load amounts of items from save (If havent restocked yet since last save) or just set default (has restocked since)
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
                        foreach (JObject price in entry.Value["barter_scheme"][0])
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
                        foreach (JObject price in entry.Value["barter_scheme"][0])
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
                switch (itemID)
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

        private void BuildTasks(JObject tasksData)
        {
            tasks = new List<TraderTask>();
            conditionsByItem = new Dictionary<string, List<TraderTaskCondition>>();
            Dictionary<string, string> rawTasks = tasksData["success"].ToObject<Dictionary<string, string>>();
            Dictionary<string, JObject> questLocales = Mod.localDB["quest"].ToObject<Dictionary<string, JObject>>();

            foreach (KeyValuePair<string, string> rawTask in rawTasks)
            {
                if (foundTasks.ContainsKey(rawTask.Value))
                {
                    continue;
                }

                JObject taskSaveData = null;
                if(traderData != null && traderData[index] != null && traderData[index][rawTask.Value] != null)
                {
                    taskSaveData = (JObject)traderData[index]["tasks"][rawTask.Value];
                }

                // Find quest in questDB
                JObject questData = null;
                foreach(JObject quest in Mod.questDB)
                {
                    if (quest["_id"].ToString().Equals(rawTask.Value))
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
                newTask.ownerTraderIndex = index;
                newTask.name = questLocale["name"].ToString();
                newTask.description = Mod.localDB["mail"][questLocale["description"].ToString()].ToString();
                newTask.failMessage = Mod.localDB["mail"][questLocale["failMessageText"].ToString()].ToString();
                newTask.successMessage = Mod.localDB["mail"][questLocale["successMessageText"].ToString()].ToString();
                newTask.location = "Any";
                if(taskSaveData == null)
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
                newTask.startConditions = new Dictionary<string, TraderTaskCondition>();
                foreach(JObject startConditionData in questData["conditions"]["AvailableForStart"])
                {
                    TraderTaskCondition newCondition = new TraderTaskCondition();

                    if(!SetCondition(newCondition, startConditionData, questLocale, taskSaveData, newTask))
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

                    if(!SetCondition(newCondition, completionConditionData, questLocale, taskSaveData, newTask))
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

                    if(!SetCondition(newCondition, failConditionData, questLocale, taskSaveData, newTask))
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
                case "AssortmentUnlock":
                    string originalAssortUnlockItemID = rewardData["items"][0]["_tpl"].ToString();
                    if (Mod.itemMap.ContainsKey(originalAssortUnlockItemID))
                    {
                        reward.taskRewardType = TraderTaskReward.TaskRewardType.AssortmentUnlock;
                        reward.itemID = Mod.itemMap[originalAssortUnlockItemID];
                        reward.amount = (int)rewardData["value"];
                        listToFill.Add(reward);
                    }
                    else
                    {
                        Mod.instance.LogError("Quest " + task.name + " with ID " + task.ID + " has ite0m unlock reward with missing item: " + originalAssortUnlockItemID);
                    }
                    itemsToWaitForUnlock.Add(originalAssortUnlockItemID);
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

        private bool SetCondition(TraderTaskCondition condition, JObject conditionData, JObject taskLocale, JObject taskSaveData, TraderTask task)
        {
            condition.ID = conditionData["_props"]["id"].ToString();
            condition.task = task;
            condition.text = taskLocale["conditions"][condition.ID].ToString();

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
                                else
                                {
                                    newCounter.counterConditionTargetEnemy = TraderTaskCounterCondition.CounterConditionTargetEnemy.PMC;
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

                                if ( condition.fulfilled)
                                {
                                    newCounter.killCount = condition.value; // Condition is fulfilled so just set count to max 
                                }
                                else // Not fulfilled, need to check for counter save data
                                {
                                    if(conditionCounterSaveData != null)
                                    {
                                        newCounter.killCount = (int)conditionCounterSaveData["killCount"];
                                    }
                                    // else, no data but is not dependent on any other task or condition, so counter is at 0
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
                                    newCounter.effectType = (EFM_Effect.EffectType)Enum.Parse(typeof(EFM_Effect.EffectType), effectName);
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
                                }
                                // No need to load timer, timer will always be at 0 since we are loading the game
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
                                // This is a constraint condition, it does not have live data of its own, so no loading save data
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
                case "FindItem":
                    condition.conditionType = TraderTaskCondition.ConditionType.FindItem;
                    condition.value = (int)conditionData["_props"]["value"];
                    string originalItemID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalItemID))
                    {
                        condition.item = Mod.itemMap[originalItemID];
                        if (conditionsByItem.ContainsKey(originalItemID))
                        {
                            conditionsByItem[originalItemID].Add(condition);
                        }
                        else
                        {
                            conditionsByItem.Add(originalItemID, new List<TraderTaskCondition>() { condition });
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case "LeaveItemAtLocation":
                    condition.conditionType = TraderTaskCondition.ConditionType.LeaveItemAtLocation;
                    condition.value = (int)conditionData["_props"]["value"];
                    string originalLeaveItemID = conditionData["_props"]["target"][0].ToString();
                    if (Mod.itemMap.ContainsKey(originalLeaveItemID))
                    {
                        condition.item = Mod.itemMap[originalLeaveItemID];
                        if (conditionsByItem.ContainsKey(originalLeaveItemID))
                        {
                            conditionsByItem[originalLeaveItemID].Add(condition);
                        }
                        else
                        {
                            conditionsByItem.Add(originalLeaveItemID, new List<TraderTaskCondition>() { condition });
                        }
                    }
                    else
                    {
                        return false;
                    }
                    condition.locationID = conditionData["_props"]["zoneId"].ToString();
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

        public static void UpdateConditionVisibility(string conditionID)
        {
            if (EFM_TraderStatus.conditionsByVisCond.ContainsKey(conditionID))
            {
                foreach (TraderTaskCondition dependentCondition in EFM_TraderStatus.conditionsByVisCond[conditionID])
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
                        if (dependentCondition.marketListElement != null)
                        {
                            dependentCondition.marketListElement.SetActive(true);
                        }
                        if (dependentCondition.statusListElement != null)
                        {
                            dependentCondition.statusListElement.SetActive(true);
                        }
                    }
                }
            }
        }

        public static void UpdateTaskCompletion(TraderTask task)
        {
            // Check if failed first
            bool failed = true;
            foreach (KeyValuePair<string, TraderTaskCondition> condition in task.failConditions)
            {
                if (!condition.Value.fulfilled)
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
                        GameObject.Destroy(task.marketListElement);
                        task.marketListElement = null;

                        Mod.currentBaseManager.marketManager.UpdateTaskListHeight();
                    }
                }
                if(task.statusListElement != null)
                {
                    GameObject.Destroy(task.statusListElement);
                    task.statusListElement = null;

                    Mod.playerStatusManager.UpdateTaskListHeight();
                }
            }
            else
            {
                bool complete = true;
                foreach (KeyValuePair<string, TraderTaskCondition> condition in task.completionConditions)
                {
                    if (!condition.Value.fulfilled)
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
            foreach (KeyValuePair<string, TraderTaskCondition> condition in task.startConditions)
            {
                if (!condition.Value.fulfilled)
                {
                    available = false;
                    break;
                }
            }

            if (available)
            {
                task.taskState = TraderTask.TaskState.Available;

                // Add to UI
                if(Mod.currentLocationIndex == 1 && Mod.currentBaseManager.marketManager.currentTraderIndex == task.ownerTraderIndex)
                {
                    Mod.currentBaseManager.marketManager.AddTask(task);
                }
            }
        }

        public static void FulfillCondition(TraderTaskCondition condition)
        {
            condition.fulfilled = true;

            // Make visible any conditions that have a UI element and that have all their visibility conditions fulfilled
            EFM_TraderStatus.UpdateConditionVisibility(condition.ID);

            // Update tasks dependent on this condition's fulfillment
            if (condition.task.taskState == TraderTask.TaskState.Locked)
            {
                EFM_TraderStatus.UpdateTaskAvailability(condition.task);
            }
            else if (condition.task.taskState == TraderTask.TaskState.Active)
            {
                EFM_TraderStatus.UpdateTaskCompletion(condition.task);
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

        public List<Dictionary<string, int>> prices;

        public int stack = 1;
        public int buyRestrictionMax = -1;
        public int buyRestrictionCurrent;
    }

    public class TraderTask
    {
        public bool init;

        public GameObject marketListElement;
        public GameObject statusListElement;

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

        public Dictionary<string, TraderTaskCondition> startConditions;
        public Dictionary<string, TraderTaskCondition> completionConditions;
        public Dictionary<string, TraderTaskCondition> failConditions;
        public List<TraderTaskReward> successRewards;
        public List<TraderTaskReward> startingEquipment;
        public List<TraderTaskReward> failureRewards;

        public void Init()
        {
            bool allStartFulfilled = true;
            foreach(KeyValuePair<string, TraderTaskCondition> condition in startConditions)
            {
                if (!condition.Value.fulfilled)
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
        public GameObject statusListElement;

        public string text;

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
        // HandOverItem, FindItem: amount to hand in/find
        public int value;

        // Level, TraderLoyalty
        public int mode; // 0: >= (Min), 1: <= (Max) 

        // Quest
        public TraderTask target;

        // TraderLoyalty
        public int targetTraderIndex;

        // HandOverItem, FindItem, LeaveItemAtLocation
        public string item;

        public int itemCount; // Live data

        // LeaveItemAtLocation
        public string locationID;

        // Counter
        public List<TraderTaskCounterCondition> counters;

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
            Equipment
        }
        public CounterConditionType counterConditionType;
        public string ID;

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

        public int killCount; // Live data

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

        public float timer; // Live data
        public bool completed; // Live data

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
        public string itemID;
        public int amount;
    }
}
