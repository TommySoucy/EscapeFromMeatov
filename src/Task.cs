using Mono.Cecil.Mdb;
using System;
using System.Collections.Generic;
using System.Diagnostics.PerformanceData;
using System.Linq;
using Valve.Newtonsoft.Json.Linq;
using static UnityEngine.EventSystems.EventTrigger;

namespace EFM
{
    public class Task
    {
        public static Dictionary<string, Task> allTasks = new Dictionary<string, Task>();
        public static Dictionary<string, List<Condition>> waitingTaskConditions = new Dictionary<string, List<Condition>>();

        // Static data
        public string ID;
        public string name;
        public string description;
        public string location;
        public bool restartable;
        public bool PMC; // Pmc in DB, anything else should set this false
        public Trader trader;
        public string taskType;
        public List<Condition> startConditions;
        public List<Condition> finishConditions;
        public List<Condition> failConditions;
        public List<Reward> startRewards;
        public List<Reward> finishRewards;
        public List<Reward> failRewards;

        // Live data
        public enum TaskState
        {
            Locked,
            Available = 2,
            Active,
            Complete = 4,
            Success,
            Fail = 5
        }
        private TaskState _taskState = TaskState.Locked;
        public TaskState taskState
        {
            get { return _taskState; }
            set
            {
                _taskState = value;
                if (_taskState == TaskState.Complete)
                {
                    OnTaskStateChangedInvoke();
                }
            }
        }

        // Objects
        public TaskUI marketUI;
        public TaskUI playerUI;

        public delegate void OnTaskStateChangedDelegate();
        public event OnTaskStateChangedDelegate OnTaskStateChanged;

        public Task(KeyValuePair<string, JToken> questData)
        {
            TODO e: // Load saved data, and once we do, call UpdateEventSubscription to subscribe to necessary events
            ID = questData.Key;
            allTasks.Add(ID, this);
            name = Mod.localeDB[questData.Value["name"].ToString()].ToString();
            description = Mod.localeDB[questData.Value["description"].ToString()].ToString();
            location = questData.Value["location"].ToString();
            restartable = (bool)questData.Value["restartable"];
            PMC = questData.Value["side"].ToString().Equals("Pmc");
            trader = Mod.traders[Trader.IDToIndex(questData.Value["traderId"].ToString())];
            taskType = questData.Value["type"].ToString();

            // Manage waiting task conditions
            if(waitingTaskConditions.TryGetValue(ID, out List<Condition> conditions))
            {
                for(int i=0; i < conditions.Count; ++i)
                {
                    conditions[i].questTargetTask = this;
                    conditions[i].description = "Complete task: " + name;
                }
                waitingTaskConditions.Remove(ID);
            }

            TODO e: // Subscribe to condition fulfillment changed
            startConditions = new List<Condition>();
            SetupConditions(startConditions, questData.Value["conditions"]["AvailableForStart"] as JArray);
            finishConditions = new List<Condition>();
            SetupConditions(finishConditions, questData.Value["conditions"]["AvailableForFinish"] as JArray);
            failConditions = new List<Condition>();
            SetupConditions(failConditions, questData.Value["conditions"]["Fail"] as JArray);

            startRewards = new List<Reward>();
            SetupRewards(startRewards, questData.Value["rewards"]["Started"] as JArray);
            finishRewards = new List<Reward>();
            SetupRewards(finishRewards, questData.Value["rewards"]["Success"] as JArray);
            failRewards = new List<Reward>();
            SetupRewards(failRewards, questData.Value["rewards"]["Fail"] as JArray);
        }

        public void UpdateEventSubscription(TaskState from, TaskState to, bool onlyTo = false)
        {
            // We want to unsibscribe from events we needed to be subscribed to in the previous state
            // Then subscribe to the events we need to be subscribed to in the new state
            if (!onlyTo)
            {
                switch (from)
                {
                    case TaskState.Locked:
                    case TaskState.Available:
                        for (int i = 0; i < startConditions.Count; ++i)
                        {
                            startConditions[i].UnsubscribeFromEvents();
                        }
                        for (int i = 0; i < failConditions.Count; ++i)
                        {
                            failConditions[i].UnsubscribeFromEvents();
                        }
                        break;
                    case TaskState.Active:
                    case TaskState.Success:
                        for (int i = 0; i < finishConditions.Count; ++i)
                        {
                            finishConditions[i].UnsubscribeFromEvents();
                        }
                        for (int i = 0; i < failConditions.Count; ++i)
                        {
                            failConditions[i].UnsubscribeFromEvents();
                        }
                        break;
                }
            }

            switch (to)
            {
                case TaskState.Locked:
                case TaskState.Available:
                    for (int i = 0; i < startConditions.Count; ++i)
                    {
                        startConditions[i].SubscribeToEvents();
                    }
                    for (int i = 0; i < failConditions.Count; ++i)
                    {
                        failConditions[i].SubscribeToEvents();
                    }
                    break;
                case TaskState.Active:
                case TaskState.Success:
                    for (int i = 0; i < finishConditions.Count; ++i)
                    {
                        finishConditions[i].SubscribeToEvents();
                    }
                    for (int i = 0; i < failConditions.Count; ++i)
                    {
                        failConditions[i].SubscribeToEvents();
                    }
                    break;
            }
        }

        public void SetupConditions(List<Condition> conditions, JArray conditionData)
        {
            if (conditionData == null || conditionData.Count == 0)
            {
                return;
            }

            for (int i = 0; i < conditionData.Count; ++i)
            {
                conditions.Add(new Condition(this, conditionData[i]));
            }
        }

        public void SetupRewards(List<Reward> rewards, JArray rewardData)
        {
            if (rewardData == null || rewardData.Count == 0)
            {
                return;
            }

            for (int i = 0; i < rewardData.Count; ++i)
            {
                rewards.Add(new Reward(this, rewardData[i]));
            }
        }

        public void OnTaskStateChangedInvoke()
        {
            if(OnTaskStateChanged != null)
            {
                OnTaskStateChanged();
            }
        }
    }

    public class Condition
    {
        private static Dictionary<string, Condition> allConditions = new Dictionary<string, Condition>();
        private static Dictionary<string, List<Condition>> waitingConditions = new Dictionary<string, List<Condition>>(); 

        // Static data
        public Task task;
        public string ID;
        public enum CompareMethod
        {
            GreaterEqual, // >=
            SmallerEqual, // <=
            Greater, // >
            Smaller // <
        }
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
            WeaponAssembly,
            PlaceBeacon,
            TraderStanding
        }
        public ConditionType conditionType;
        public string description = null;
        public List<VisibilityCondition> visibilityConditions;
        public float minDurability = 0;
        public float maxDurability = float.MaxValue;
        public int value;
        public bool onlyFoundInRaid = false;
        public int dogtagLevel;
        public List<MeatovItemData> targetItems;
        public CompareMethod compareMethod;
        public string zoneID;
        // CounterCreator
        public List<ConditionCounter> counters;
        public bool counterOneSessionOnly;
        // FindItem
        public bool findItemCountInRaid;
        // Quest
        public Task questTargetTask;
        public List<Task.TaskState> questTaskStates;
        public int questAvailableAfter = 0; // How many seconds after target task has reached status is this condition fulfilled
        public int questDispersion; // How moany seconds on top of AvailableAfter condition may be fulfilled, so this condition can be fulfilled questAvailableAfter+Random.Range(0,dispersion)
        // LeaveItemAtLocation, PlaceBeacon
        public float plantTime; // Seconds
        // TraderLoyalty, TraderStanding
        public Trader targetTrader;
        // TraderStanding
        public float standingValue;
        // Skill
        public Skill targetSkill;
        // WeaponAssembly
        public List<MeatovItemData> containsItems;
        public List<string> hasItemFromCategories;
        public float weaponAccuracy;
        public CompareMethod weaponAccuracyCompareMethod;
        public float weaponDurability;
        public CompareMethod weaponDurabilityCompareMethod;
        public float weaponEffectiveDistance;
        public CompareMethod weaponEffectiveDistanceCompareMethod;
        public int weaponEmptyTacticalSlot;
        public CompareMethod weaponEmptyTacticalSlotCompareMethod;
        public int weaponErgonomics;
        public CompareMethod weaponErgonomicsCompareMethod;
        public int weaponMagazineCapacity;
        public CompareMethod weaponMagazineCapacityCompareMethod;
        public float weaponMuzzleVelocity;
        public CompareMethod weaponMuzzleVelocityCompareMethod;
        public float weaponRecoil;
        public CompareMethod weaponRecoilCompareMethod;
        public float weaponWeight;
        public CompareMethod weaponWeightCompareMethod;

        // Live data
        private bool _fulfilled;
        public bool fulfilled
        {
            get { return _fulfilled; }
            set 
            {
                bool preValue = _fulfilled;
                _fulfilled = value;
                if (preValue != _fulfilled)
                {
                    OnConditionFulfillmentChangedInvoke(this);
                }
            }
        }
        public int count;
        public bool subscribedToEvents;

        // Objects
        public TaskObjectiveUI marketUI;
        public TaskObjectiveUI playerUI;

        // Events
        public delegate void OnConditionFulfillmentChangedDelegate(Condition condition);
        public event OnConditionFulfillmentChangedDelegate OnConditionFulfillmentChanged;

        public Condition(Task task, JToken data)
        {
            this.task = task;

            JToken properties = data["_props"];
            ID = properties["id"].ToString();
            conditionType = (ConditionType)Enum.Parse(typeof(ConditionType), data["_parent"].ToString());
            if(Mod.localeDB[ID] == null)
            {
                switch (conditionType)
                {
                    case ConditionType.Level:
                    case ConditionType.Quest:
                    case ConditionType.TraderLoyalty:
                    case ConditionType.TraderStanding:
                        // Handled further down when we set other values
                        break;
                    default:
                        Mod.LogError("DEV: "+conditionType+" condition with ID: "+ ID+" of task: "+ task.ID+" does not have description in locale");
                        break;
                }
            }
            else
            {
                description = Mod.localeDB[ID].ToString();
            }

            SetupVisibilityConditions(data);

            // Set condition type specific data
            switch (conditionType)
            {
                case ConditionType.CounterCreator:
                    value = (int)properties["value"];
                    counterOneSessionOnly = (bool)properties["oneSessionOnly"];
                    SetupCounters(properties);
                    break;
                case ConditionType.HandoverItem:
                    if(properties["minDurability"] != null)
                    {
                        minDurability = (float)properties["minDurability"];
                    }
                    if(properties["maxDurability"] != null)
                    {
                        maxDurability = (float)properties["maxDurability"];
                    }
                    value = (int)properties["value"];
                    if (properties["onlyFoundInRaid"] != null)
                    {
                        onlyFoundInRaid = (bool)properties["onlyFoundInRaid"];
                    }
                    dogtagLevel = (int)properties["dogtagLevel"];
                    List<string> handoverItemTargetItemIDs = properties["target"].ToObject<List<string>>();
                    targetItems = new List<MeatovItemData>();
                    for (int i = 0; i < handoverItemTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(handoverItemTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            targetItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if(Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                targetItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " handoveritem condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    break;
                case ConditionType.Level:
                    value = (int)properties["value"];
                    compareMethod = CompareMethodFromString(properties["compareMethod"].ToString());
                    description = "Reach level "+ value;
                    break;
                case ConditionType.FindItem:
                    value = (int)properties["value"];
                    minDurability = (float)properties["minDurability"];
                    maxDurability = (float)properties["maxDurability"];
                    onlyFoundInRaid = (bool)properties["onlyFoundInRaid"];
                    dogtagLevel = (int)properties["dogtagLevel"];
                    List<string> findItemTargetItemIDs = properties["target"].ToObject<List<string>>();
                    targetItems = new List<MeatovItemData>();
                    for (int i = 0; i < findItemTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(findItemTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            targetItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                targetItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " find item condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    findItemCountInRaid = (bool)properties["countInRaid"];
                    break;
                case ConditionType.Quest:
                    string targetTaskID = properties["target"].ToString();
                    if (Task.allTasks.TryGetValue(targetTaskID, out Task targetTask))
                    {
                        questTargetTask = targetTask;
                        description = "Complete task: " + targetTask.name;
                    }
                    else
                    {
                        if (Task.waitingTaskConditions.TryGetValue(targetTaskID, out List<Condition> otherCurrentWaitingConditions))
                        {
                            otherCurrentWaitingConditions.Add(this);
                        }
                        else
                        {
                            Task.waitingTaskConditions.Add(targetTaskID, new List<Condition> { this });
                        }
                    }
                    JArray taskStateArray = properties["status"] as JArray;
                    questTaskStates = new List<Task.TaskState>();
                    for (int i=0; i < taskStateArray.Count; ++i)
                    {
                        questTaskStates.Add((Task.TaskState)(int)taskStateArray[i]);
                    }
                    if(properties["availableAfter"] != null)
                    {
                        questAvailableAfter = (int)properties["availableAfter"];
                    }
                    break;
                case ConditionType.LeaveItemAtLocation:
                    value = (int)properties["value"];
                    minDurability = (float)properties["minDurability"];
                    maxDurability = (float)properties["maxDurability"];
                    onlyFoundInRaid = (bool)properties["onlyFoundInRaid"];
                    dogtagLevel = (int)properties["dogtagLevel"];
                    List<string> leaveItemTargetItemIDs = properties["target"].ToObject<List<string>>();
                    targetItems = new List<MeatovItemData>();
                    for (int i = 0; i < leaveItemTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(leaveItemTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            targetItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                targetItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " leave item condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    plantTime = (int)properties["plantTime"];
                    zoneID = properties["zoneId"].ToString();
                    break;
                case ConditionType.PlaceBeacon:
                    value = (int)properties["value"];
                    List<string> leaveBeaconTargetItemIDs = properties["target"].ToObject<List<string>>();
                    targetItems = new List<MeatovItemData>();
                    for (int i = 0; i < leaveBeaconTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(leaveBeaconTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            targetItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                targetItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " leave beacon condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    plantTime = (int)properties["plantTime"];
                    zoneID = properties["zoneId"].ToString();
                    break;
                case ConditionType.TraderLoyalty:
                    value = (int)properties["value"];
                    compareMethod = CompareMethodFromString(properties["compareMethod"].ToString());
                    targetTrader = Mod.traders[Trader.IDToIndex(properties["target"].ToString())];
                    if(description == null)
                    {
                        description = "Reach level " + value + " with " + targetTrader.name;
                    }
                    break;
                case ConditionType.TraderStanding:
                    standingValue = (float)properties["value"];
                    if(properties["compareMethod"] == null)
                    {
                        if(standingValue < 0)
                        {
                            compareMethod = CompareMethod.SmallerEqual;
                        }
                    }
                    else
                    {
                        compareMethod = CompareMethodFromString(properties["compareMethod"].ToString());
                    }
                    targetTrader = Mod.traders[Trader.IDToIndex(properties["target"].ToString())];
                    if(description == null)
                    {
                        description = "Reach standing " + value + " with " + targetTrader.name;
                    }
                    break;
                case ConditionType.Skill:
                    value = (int)properties["value"];
                    compareMethod = CompareMethodFromString(properties["compareMethod"].ToString());
                    targetSkill = Mod.skills[Skill.SkillNameToIndex(properties["target"].ToString())];
                    break;
                case ConditionType.WeaponAssembly:
                    value = (int)properties["value"];
                    List<string> weaponAssemblyTargetItemIDs = properties["target"].ToObject<List<string>>();
                    targetItems = new List<MeatovItemData>();
                    for (int i = 0; i < weaponAssemblyTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(weaponAssemblyTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            targetItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                targetItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " weapon assembly condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    List<string> weaponAssemblyContainsTargetItemIDs = properties["containsItems"].ToObject<List<string>>();
                    containsItems = new List<MeatovItemData>();
                    for (int i = 0; i < weaponAssemblyContainsTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(weaponAssemblyContainsTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            containsItems.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                containsItems.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " + task.ID + " weapon assembly contains condition " + ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    hasItemFromCategories = properties["hasItemFromCategory"].ToObject<List<string>>();
                    for (int i = 0; i < hasItemFromCategories.Count; ++i)
                    {
                        hasItemFromCategories[i] = Mod.TarkovIDtoH3ID(hasItemFromCategories[i]);
                    }
                    weaponAccuracy = (float)properties["baseAccuracy"]["value"];
                    weaponAccuracyCompareMethod = CompareMethodFromString(properties["baseAccuracy"]["compareMethod"].ToString());
                    weaponDurability = (float)properties["durability"]["value"];
                    weaponDurabilityCompareMethod = CompareMethodFromString(properties["durability"]["compareMethod"].ToString());
                    weaponEffectiveDistance = (float)properties["effectiveDistance"]["value"];
                    weaponEffectiveDistanceCompareMethod = CompareMethodFromString(properties["effectiveDistance"]["compareMethod"].ToString());
                    weaponEmptyTacticalSlot = (int)properties["emptyTacticalSlot"]["value"];
                    weaponEmptyTacticalSlotCompareMethod = CompareMethodFromString(properties["emptyTacticalSlot"]["compareMethod"].ToString());
                    weaponErgonomics = (int)properties["ergonomics"]["value"];
                    weaponErgonomicsCompareMethod = CompareMethodFromString(properties["ergonomics"]["compareMethod"].ToString());
                    weaponMagazineCapacity = (int)properties["magazineCapacity"]["value"];
                    weaponMagazineCapacityCompareMethod = CompareMethodFromString(properties["magazineCapacity"]["compareMethod"].ToString());
                    weaponMuzzleVelocity = (float)properties["muzzleVelocity"]["value"];
                    weaponMuzzleVelocityCompareMethod = CompareMethodFromString(properties["muzzleVelocity"]["compareMethod"].ToString());
                    weaponRecoil = (float)properties["recoil"]["value"];
                    weaponRecoilCompareMethod = CompareMethodFromString(properties["recoil"]["compareMethod"].ToString());
                    weaponWeight = (float)properties["weight"]["value"];
                    weaponWeightCompareMethod = CompareMethodFromString(properties["weight"]["compareMethod"].ToString());
                    break;
            }
        }

        public void SubscribeToEvents()
        {
            if (!subscribedToEvents)
            {
                switch (conditionType)
                {
                    case ConditionType.CounterCreator:
                        if (counterOneSessionOnly)
                        {
                            Mod.OnRaidExit += OnRaidExit;
                        }
                        for(int i=0; i < counters.Count; ++i)
                        {
                            counters[i].SubscribeToEvents();
                        }
                        subscribedToEvents = true;
                        break;
                    case ConditionType.Level:
                        fulfilled = CompareInt(compareMethod, Mod.level, value);
                        if (!fulfilled)
                        {
                            Mod.OnPlayerLevelChanged += OnPlayerLevelChanged;
                            subscribedToEvents = true;
                        }
                        break;
                    case ConditionType.FindItem:
                        for (int i = 0; i < targetItems.Count; ++i)
                        {
                            targetItems[i].OnItemFound += OnItemFound;
                        }
                        subscribedToEvents = true;
                        break;
                    case ConditionType.Quest:
                        fulfilled = questTargetTask.taskState == Task.TaskState.Complete;
                        if (!fulfilled)
                        {
                            questTargetTask.OnTaskStateChanged += OnTaskStateChanged;
                            subscribedToEvents = true;
                        }
                        break;
                    case ConditionType.LeaveItemAtLocation:
                    case ConditionType.PlaceBeacon:
                        for (int i = 0; i < targetItems.Count; ++i)
                        {
                            targetItems[i].OnItemLeft += OnItemLeft;
                        }
                        subscribedToEvents = true;
                        break;
                    case ConditionType.TraderLoyalty:
                        fulfilled = CompareInt(compareMethod, targetTrader.level, value);
                        if (!fulfilled)
                        {
                            targetTrader.OnTraderLevelChanged += OnTraderLevelChanged;
                            subscribedToEvents = true;
                        }
                        break;
                    case ConditionType.TraderStanding:
                        fulfilled = CompareFloat(compareMethod, targetTrader.standing, value);
                        if (!fulfilled)
                        {
                            targetTrader.OnTraderStandingChanged += OnTraderStandingChanged;
                            subscribedToEvents = true;
                        }
                        break;
                    case ConditionType.Skill:
                        fulfilled = CompareInt(compareMethod, targetSkill.GetLevel(), value);
                        if (!fulfilled)
                        {
                            targetSkill.OnSkillLevelChanged += OnSkillLevelChanged;
                            subscribedToEvents = true;
                        }
                        break;
                }
            }
        }

        public void UnsubscribeFromEvents()
        {
            if (subscribedToEvents)
            {
                switch (conditionType)
                {
                    case ConditionType.CounterCreator:
                        if (counterOneSessionOnly)
                        {
                            Mod.OnRaidExit -= OnRaidExit;
                        }
                        for (int i=0; i < counters.Count; ++i)
                        {
                            counters[i].UnsubscribeFromEvents();
                        }
                        break;
                    case ConditionType.Level:
                        Mod.OnPlayerLevelChanged -= OnPlayerLevelChanged;
                        break;
                    case ConditionType.FindItem:
                        for (int i = 0; i < targetItems.Count; ++i)
                        {
                            targetItems[i].OnItemFound -= OnItemFound;
                        }
                        break;
                    case ConditionType.Quest:
                        questTargetTask.OnTaskStateChanged -= OnTaskStateChanged;
                        break;
                    case ConditionType.LeaveItemAtLocation:
                    case ConditionType.PlaceBeacon:
                        for (int i = 0; i < targetItems.Count; ++i)
                        {
                            targetItems[i].OnItemLeft -= OnItemLeft;
                        }
                        break;
                    case ConditionType.TraderLoyalty:
                        targetTrader.OnTraderLevelChanged -= OnTraderLevelChanged;
                        break;
                    case ConditionType.TraderStanding:
                        targetTrader.OnTraderStandingChanged -= OnTraderStandingChanged;
                        break;
                    case ConditionType.Skill:
                        targetSkill.OnSkillLevelChanged -= OnSkillLevelChanged;
                        break;
                }

                subscribedToEvents = false;
            }
        }

        public static CompareMethod CompareMethodFromString(string s)
        {
            switch (s)
            {
                case ">=":
                    return CompareMethod.GreaterEqual;
                case "<=":
                    return CompareMethod.SmallerEqual;
                case ">":
                    return CompareMethod.Greater;
                case "<":
                    return CompareMethod.Smaller;
                default:
                    Mod.LogError("DEV: CompareMethodFromString got string " + s);
                    return CompareMethod.GreaterEqual;
            }
        }

        public static bool CompareInt(CompareMethod method, int x, int y)
        {
            switch (method)
            {
                case CompareMethod.Smaller:
                    return x < y;
                case CompareMethod.SmallerEqual:
                    return x <= y;
                case CompareMethod.Greater:
                    return x > y;
                case CompareMethod.GreaterEqual:
                    return x >= y;
                default:
                    return false;
            }
        }

        public static bool CompareFloat(CompareMethod method, float x, float y)
        {
            switch (method)
            {
                case CompareMethod.Smaller:
                    return x < y;
                case CompareMethod.SmallerEqual:
                    return x <= y;
                case CompareMethod.Greater:
                    return x > y;
                case CompareMethod.GreaterEqual:
                    return x >= y;
                default:
                    return false;
            }
        }

        public void SetupVisibilityConditions(JToken data)
        {
            // Manage visibility conditions
            // Set other Conditions' visibilityConditions if their target is us
            if (waitingConditions.TryGetValue(ID, out List<Condition> currentWaitingConditions))
            {
                for (int i = 0; i < currentWaitingConditions.Count; ++i)
                {
                    currentWaitingConditions[i].visibilityConditions.Add(new VisibilityCondition(currentWaitingConditions[i], this));
                }
                waitingConditions.Remove(ID);
            }
            // Set our own visibilityConditions
            visibilityConditions = new List<VisibilityCondition>();
            JArray visibilityConditionDataArray = data["visibilityConditions"] as JArray;
            if (visibilityConditionDataArray != null && allConditions.Count > 0)
            {
                for (int i = 0; i < visibilityConditionDataArray.Count; ++i)
                {
                    string targetConditionID = visibilityConditionDataArray[i]["_props"]["target"].ToString();
                    if (allConditions.TryGetValue(targetConditionID, out Condition targetCondition))
                    {
                        visibilityConditions.Add(new VisibilityCondition(this, targetCondition));
                    }
                    else
                    {
                        if (waitingConditions.TryGetValue(targetConditionID, out List<Condition> otherCurrentWaitingConditions))
                        {
                            otherCurrentWaitingConditions.Add(this);
                        }
                        else
                        {
                            waitingConditions.Add(targetConditionID, new List<Condition> { this });
                        }
                    }
                }
            }
        }

        public void SetupCounters(JToken properties)
        {
            counters = new List<ConditionCounter>();
            JArray counterArray = properties["counter"]["conditions"] as JArray;
            for(int i=0; i < counterArray.Count; ++i)
            {
                counters.Add(new ConditionCounter(this, counterArray[i]));
            }
        }

        public void OnConditionFulfillmentChangedInvoke(Condition condition)
        {
            if (fulfilled)
            {
                UnsubscribeFromEvents();
            }
            else
            {
                SubscribeToEvents();
            }

            if(OnConditionFulfillmentChanged != null)
            {
                OnConditionFulfillmentChanged(condition);
            }
        }

        public void OnPlayerLevelChanged()
        {
            if(conditionType == ConditionType.Level)
            {
                fulfilled = CompareInt(compareMethod, Mod.level, value);
            }
        }

        public void OnItemFound()
        {
            if(conditionType == ConditionType.FindItem)
            {
                ++count;
                fulfilled = CompareInt(compareMethod, count, value);
            }
        }

        public void OnItemLeft(string locationID)
        {
            if(conditionType == ConditionType.LeaveItemAtLocation)
            {
                if (locationID.Equals(zoneID))
                {
                    ++count;
                    fulfilled = CompareInt(compareMethod, count, value);
                }
            }
        }

        public void OnTaskStateChanged()
        {
            if(conditionType == ConditionType.Quest)
            {
                fulfilled = questTaskStates.Contains(questTargetTask.taskState);
            }
        }

        public void OnTraderLevelChanged()
        {
            if(conditionType == ConditionType.TraderLoyalty)
            {
                fulfilled = CompareInt(compareMethod, targetTrader.level, value);
            }
        }

        public void OnTraderStandingChanged()
        {
            if(conditionType == ConditionType.TraderStanding)
            {
                fulfilled = CompareFloat(compareMethod, targetTrader.standing, standingValue);
            }
        }

        public void OnSkillLevelChanged()
        {
            if(conditionType == ConditionType.Skill)
            {
                fulfilled = CompareInt(compareMethod, targetSkill.GetLevel(), value);
            }
        }

        public void OnRaidExit(ConditionCounter.ExitStatus status, string exitID)
        {
            if(conditionType == ConditionType.CounterCreator && counterOneSessionOnly && !fulfilled)
            {
                count = 0;
            }
        }
    }

    public class ConditionCounter
    {
        // Static data
        public Condition condition;
        public enum CounterCreatorConditionType
        {
            Kills,
            ExitStatus,
            VisitPlace,
            Location,
            HealthEffect,
            Equipment,
            InZone,
            Shots,
            UseItem,
            LaunchFlare,
            ExitName,
            HealthBuff,
        }
        public CounterCreatorConditionType counterCreatorConditionType;
        // Kills
        public Condition.CompareMethod killCompareMethod = Condition.CompareMethod.GreaterEqual;
        public Condition.CompareMethod killDistanceCompareMethod; // Could be omitted from DB
        public int killDistance; // Could be omitted from DB
        public enum EnemyTarget
        {
            Savage, // Scav
            AnyPmc, // PMC
            Usec,
            Bear,
            Any
        }
        public EnemyTarget killTarget;
        public List<string> killSavageRoles; // Could be omitted from DB
        public int killValue = 1;
        public List<string> killWeaponWhitelist; // Could be omitted from DB
        public List<List<string>> killWeaponModWhitelists; // Could be omitted from DB
        public List<List<string>> killWeaponModBlacklists; // Could be omitted from DB
        public enum TargetBodyPart
        {
            Head,
            Chest,
            Stomach,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg
        }
        public List<TargetBodyPart> killBodyParts; // Could be omitted from DB
        public Vector2Int killTime; // Could be omitted from DB
        public List<HealthEffectEntry> killEnemyHealthEffects; // Could be omitted from DB
        // HealthEffect
        public List<HealthEffectEntry> healthEffects;
        public int healthEffectDuration = 0;
        public Condition.CompareMethod healthEffectDurationCompareMethod;
        // Location
        public List<string> locationTargets;
        // InZone
        public List<string> zoneIDs;
        // ExitStatus
        public enum ExitStatus
        {
            Survived,
            Runner,
            Killed,
            Left,
            MissingInAction
        }
        public List<ExitStatus> exitStatuses;
        // VisitPlace
        public string visitTarget;
        public int visitValue;
        // Equipment
        public List<List<string>> equipmentWhitelists;
        public List<List<string>> equipmentBlacklists;
        // Shots
        public Condition.CompareMethod shotCompareMethod;
        public Condition.CompareMethod shotDistanceCompareMethod;
        public int shotDistance;
        public EnemyTarget shotTarget;
        public int shotValue;
        public List<string> shotWeaponWhitelist; // Could be omitted from DB
        public List<List<string>> shotWeaponModWhitelists; // Could be omitted from DB
        public List<List<string>> shotWeaponModBlacklists; // Could be omitted from DB
        public List<TargetBodyPart> shotBodyParts;
        public List<string> shotSavageRoles; // Could be omitted from DB
        public List<HealthEffectEntry> shotEnemyHealthEffects; // Could be omitted from DB
        public Vector2Int shotTime; // Could be omitted from DB
        // UseItem
        public Condition.CompareMethod useItemCompareMethod;
        public int useItemValue;
        public List<MeatovItemData> useItemTargets;
        // ExitName
        public string exitName;
        // HealthBuff
        public List<string> buffTargets;

        // Live data
        public bool subscribedToEvents;

        public ConditionCounter(Condition condition, JToken data)
        {
            this.condition = condition;

            counterCreatorConditionType = (CounterCreatorConditionType)Enum.Parse(typeof(CounterCreatorConditionType), data["_parent"].ToString());
            JToken properties = data["_props"];
            switch (counterCreatorConditionType)
            {
                case CounterCreatorConditionType.Kills:
                    if (properties["compareMethod"] != null)
                    {
                        killCompareMethod = Condition.CompareMethodFromString(properties["compareMethod"].ToString());
                    }
                    if (properties["distance"] != null)
                    {
                        killDistanceCompareMethod = Condition.CompareMethodFromString(properties["distance"]["compareMethod"].ToString());
                        killDistance = (int)properties["distance"]["value"];
                    }
                    killTarget = (EnemyTarget)Enum.Parse(typeof(EnemyTarget), properties["target"].ToString(), true);
                    JArray savageRolesArray = properties["savageRole"] as JArray;
                    if (savageRolesArray != null)
                    {
                        killSavageRoles = savageRolesArray.ToObject<List<string>>();
                    }
                    if(properties["value"] != null)
                    {
                        killValue = (int)properties["value"];
                    }
                    JArray weaponArray = properties["weapon"] as JArray;
                    if(weaponArray != null)
                    {
                        killWeaponWhitelist = weaponArray.ToObject<List<string>>();
                        for(int i=0; i < killWeaponWhitelist.Count; ++i)
                        {
                            killWeaponWhitelist[i] = Mod.TarkovIDtoH3ID(killWeaponWhitelist[i]);
                        }
                    }
                    JArray weaponModArray = properties["weaponModsInclusive"] as JArray;
                    if(weaponModArray != null)
                    {
                        killWeaponModWhitelists = new List<List<string>>();
                        for(int i=0; i < weaponModArray.Count; ++i)
                        {
                            List<string> newSubList = weaponModArray[i].ToObject<List<string>>();
                            for(int j=0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            killWeaponModWhitelists.Add(newSubList);
                        }
                    }
                    weaponModArray = properties["weaponModsExclusive"] as JArray;
                    if(weaponModArray != null)
                    {
                        killWeaponModBlacklists = new List<List<string>>();
                        for(int i=0; i < weaponModArray.Count; ++i)
                        {
                            List<string> newSubList = weaponModArray[i].ToObject<List<string>>();
                            for(int j=0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            killWeaponModBlacklists.Add(newSubList);
                        }
                    }
                    JArray enemyHealthEffectArray = properties["enemyHealthEffects"] as JArray;
                    if(enemyHealthEffectArray != null)
                    {
                        killEnemyHealthEffects = new List<HealthEffectEntry>();
                        for (int i = 0; i < enemyHealthEffectArray.Count; ++i)
                        {
                            killEnemyHealthEffects.Add(new HealthEffectEntry(enemyHealthEffectArray[i]));
                        }
                    }
                    JArray killBodyPartsArray = properties["bodyPart"] as JArray;
                    if(killBodyPartsArray != null)
                    {
                        killBodyParts = new List<TargetBodyPart>();
                        for (int i = 0; i < killBodyPartsArray.Count; ++i)
                        {
                            killBodyParts.Add((TargetBodyPart)Enum.Parse(typeof(TargetBodyPart), killBodyPartsArray[i].ToString()));
                        }
                    }
                    if (properties["daytime"] != null)
                    {
                        killTime = new Vector2Int((int)properties["daytime"]["from"], (int)properties["daytime"]["to"]);
                    }
                    break;
                case CounterCreatorConditionType.ExitStatus:
                    JArray statusArray = properties["status"] as JArray;
                    exitStatuses = new List<ExitStatus>();
                    for(int i=0; i < statusArray.Count; ++i)
                    {
                        exitStatuses.Add((ExitStatus)Enum.Parse(typeof(ExitStatus), statusArray[i].ToString(), true));
                    }
                    break;
                case CounterCreatorConditionType.VisitPlace:
                    visitTarget = properties["target"].ToString();
                    visitValue = (int)properties["value"];
                    break;
                case CounterCreatorConditionType.Location:
                    locationTargets = properties["target"].ToObject<List<string>>(); 
                    break;
                case CounterCreatorConditionType.HealthEffect:
                    JArray healthEffectArray = properties["bodyPartsWithEffects"] as JArray;
                    healthEffects = new List<HealthEffectEntry>();
                    for (int i = 0; i < healthEffectArray.Count; ++i)
                    {
                        healthEffects.Add(new HealthEffectEntry(healthEffectArray[i]));
                    }
                    if(properties["time"] != null)
                    {
                        healthEffectDuration = (int)properties["time"]["value"];
                        healthEffectDurationCompareMethod = Condition.CompareMethodFromString(properties["time"]["compareMethod"].ToString());
                    }
                    break;
                case CounterCreatorConditionType.Equipment:
                    JArray equipmentArray = properties["equipmentInclusive"] as JArray;
                    if (equipmentArray != null)
                    {
                        equipmentWhitelists = new List<List<string>>();
                        for (int i = 0; i < equipmentArray.Count; ++i)
                        {
                            List<string> newSubList = equipmentArray[i].ToObject<List<string>>();
                            for (int j = 0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            equipmentWhitelists.Add(newSubList);
                        }
                    }
                    equipmentArray = properties["equipmentExclusive"] as JArray;
                    if (equipmentArray != null)
                    {
                        equipmentBlacklists = new List<List<string>>();
                        for (int i = 0; i < equipmentArray.Count; ++i)
                        {
                            List<string> newSubList = equipmentArray[i].ToObject<List<string>>();
                            for (int j = 0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            equipmentBlacklists.Add(newSubList);
                        }
                    }
                    break;
                case CounterCreatorConditionType.InZone:
                    JArray zoneIDArray = properties["zoneIds"] as JArray;
                    zoneIDs = zoneIDArray.ToObject<List<string>>();
                    break;
                case CounterCreatorConditionType.Shots:
                    shotCompareMethod = Condition.CompareMethodFromString(properties["compareMethod"].ToString());
                    if (properties["distance"] != null)
                    {
                        shotDistanceCompareMethod = Condition.CompareMethodFromString(properties["distance"]["compareMethod"].ToString());
                        shotDistance = (int)properties["distance"]["value"];
                    }
                    shotTarget = (EnemyTarget)Enum.Parse(typeof(EnemyTarget), properties["target"].ToString(), true);
                    JArray shotSavageRolesArray = properties["savageRole"] as JArray;
                    if (shotSavageRolesArray != null)
                    {
                        shotSavageRoles = shotSavageRolesArray.ToObject<List<string>>();
                    }
                    shotValue = (int)properties["value"];
                    JArray shotWeaponArray = properties["weapon"] as JArray;
                    if (shotWeaponArray != null)
                    {
                        shotWeaponWhitelist = shotWeaponArray.ToObject<List<string>>();
                        for (int i = 0; i < shotWeaponWhitelist.Count; ++i)
                        {
                            shotWeaponWhitelist[i] = Mod.TarkovIDtoH3ID(shotWeaponWhitelist[i]);
                        }
                    }
                    JArray shotWeaponModArray = properties["weaponModsInclusive"] as JArray;
                    if (shotWeaponModArray != null)
                    {
                        shotWeaponModWhitelists = new List<List<string>>();
                        for (int i = 0; i < shotWeaponModArray.Count; ++i)
                        {
                            List<string> newSubList = shotWeaponModArray[i].ToObject<List<string>>();
                            for (int j = 0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            shotWeaponModWhitelists.Add(newSubList);
                        }
                    }
                    shotWeaponModArray = properties["weaponModsExclusive"] as JArray;
                    if (shotWeaponModArray != null)
                    {
                        shotWeaponModBlacklists = new List<List<string>>();
                        for (int i = 0; i < shotWeaponModArray.Count; ++i)
                        {
                            List<string> newSubList = shotWeaponModArray[i].ToObject<List<string>>();
                            for (int j = 0; j < newSubList.Count; ++j)
                            {
                                newSubList[j] = Mod.TarkovIDtoH3ID(newSubList[j]);
                            }
                            shotWeaponModBlacklists.Add(newSubList);
                        }
                    }
                    JArray shotEnemyHealthEffectArray = properties["enemyHealthEffects"] as JArray;
                    if (shotEnemyHealthEffectArray != null)
                    {
                        shotEnemyHealthEffects = new List<HealthEffectEntry>();
                        for (int i = 0; i < shotEnemyHealthEffectArray.Count; ++i)
                        {
                            shotEnemyHealthEffects.Add(new HealthEffectEntry(shotEnemyHealthEffectArray[i]));
                        }
                    }
                    JArray shotBodyPartsArray = properties["bodyPart"] as JArray;
                    if (shotBodyPartsArray != null)
                    {
                        shotBodyParts = new List<TargetBodyPart>();
                        for (int i = 0; i < shotBodyPartsArray.Count; ++i)
                        {
                            shotBodyParts.Add((TargetBodyPart)Enum.Parse(typeof(TargetBodyPart), shotBodyPartsArray[i].ToString()));
                        }
                    }
                    if (properties["daytime"] != null)
                    {
                        shotTime = new Vector2Int((int)properties["daytime"]["from"], (int)properties["daytime"]["to"]);
                    }
                    break;
                case CounterCreatorConditionType.UseItem:
                    useItemCompareMethod = Condition.CompareMethodFromString(properties["compareMethod"].ToString());
                    List<string> useItemTargetItemIDs = properties["target"].ToObject<List<string>>();
                    useItemTargets = new List<MeatovItemData>();
                    for (int i = 0; i < useItemTargetItemIDs.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(useItemTargetItemIDs[i]);
                        int parsedIndex = -1;
                        if (int.TryParse(itemID, out parsedIndex))
                        {
                            useItemTargets.Add(Mod.customItemData[parsedIndex]);
                        }
                        else
                        {
                            if (Mod.vanillaItemData.TryGetValue(itemID, out MeatovItemData itemData))
                            {
                                useItemTargets.Add(Mod.vanillaItemData[itemID]);
                            }
                            else
                            {
                                Mod.LogError("DEV: Task " +condition.task.ID + " use item counter condition from condition " + condition.ID + " targets item " + itemID + " for which we do not have data");
                            }
                        }
                    }
                    useItemValue = (int)properties["value"];
                    break;
                case CounterCreatorConditionType.LaunchFlare:
                    zoneIDs = new List<string>();
                    zoneIDs.Add(properties["target"].ToString());
                    break;
                case CounterCreatorConditionType.ExitName:
                    exitName = properties["exitName"].ToString();
                    break;
                case CounterCreatorConditionType.HealthBuff:
                    buffTargets = properties["target"].ToObject<List<string>>();
                    break;
            }
        }

        public void SubscribeToEvents()
        {
            TODO: // These events should probably be put into RaidController once we create it
            if (!subscribedToEvents)
            {
                switch (counterCreatorConditionType)
                {
                    case CounterCreatorConditionType.Kills:
                        Mod.OnKill += OnKill;
                        break;
                    case CounterCreatorConditionType.ExitStatus:
                        Mod.OnRaidExit += OnRaidExit;
                        break;
                    case CounterCreatorConditionType.VisitPlace:
                        Mod.OnPlaceVisited += OnPlaceVisited;
                        break;
                    case CounterCreatorConditionType.Shots:
                        Mod.OnShot += OnShot;
                        break;
                    case CounterCreatorConditionType.UseItem:
                        for (int i = 0; i < useItemTargets.Count; ++i)
                        {
                            useItemTargets[i].OnItemUsed += OnItemUsed;
                        }
                        break;
                    case CounterCreatorConditionType.LaunchFlare:
                        Mod.OnFlareLaunched += OnFlareLaunched;
                        break;
                }

                subscribedToEvents = true;
            }
        }

        public void UnsubscribeFromEvents()
        {
            if (subscribedToEvents)
            {
                switch (counterCreatorConditionType)
                {
                    case CounterCreatorConditionType.Kills:
                        Mod.OnKill -= OnKill;
                        break;
                    case CounterCreatorConditionType.ExitStatus:
                        Mod.OnRaidExit -= OnRaidExit;
                        break;
                    case CounterCreatorConditionType.VisitPlace:
                        Mod.OnPlaceVisited -= OnPlaceVisited;
                        break;
                    case CounterCreatorConditionType.Shots:
                        Mod.OnShot -= OnShot;
                        break;
                    case CounterCreatorConditionType.UseItem:
                        for (int i = 0; i < useItemTargets.Count; ++i)
                        {
                            useItemTargets[i].OnItemUsed -= OnItemUsed;
                        }
                        break;
                    case CounterCreatorConditionType.LaunchFlare:
                        Mod.OnFlareLaunched -= OnFlareLaunched;
                        break;
                }

                subscribedToEvents = false;
            }
        }

        public void OnKill(KillData killData)
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.Kills)
            {
                if (Condition.CompareFloat(killDistanceCompareMethod, killData.distance, killDistance)
                    && DoesEnemyTargetMatch(killTarget, killData.enemyTarget, killSavageRoles, killData.savageRole)
                    && Mod.IDDescribedInList(killData.weaponData.H3ID, new List<string>(killData.weaponData.parents), killWeaponWhitelist, null)
                    && (killWeaponModWhitelists == null || killWeaponModWhitelists.Count == 0 || IDsDescribedInMultipleLists(killData.weaponChildrenData, killWeaponModWhitelists))
                    && (killWeaponModBlacklists == null || killWeaponModBlacklists.Count == 0 || !IDsDescribedInMultipleLists(killData.weaponChildrenData, killWeaponModBlacklists))
                    && MatchesHealthEffectList(killData.enemyHealthEffects, killEnemyHealthEffects)
                    && killBodyParts.Contains(killData.bodyPart))
                {
                    ++condition.count;
                    condition.fulfilled = condition.count >= condition.value;
                }
            }
        }

        public void OnShot(ShotData shotData)
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.Kills)
            {
                if (Condition.CompareFloat(killDistanceCompareMethod, shotData.distance, killDistance)
                    && DoesEnemyTargetMatch(killTarget, shotData.enemyTarget, killSavageRoles, shotData.savageRole)
                    && Mod.IDDescribedInList(shotData.weaponData.H3ID, new List<string>(shotData.weaponData.parents), killWeaponWhitelist, null)
                    && (killWeaponModWhitelists == null || killWeaponModWhitelists.Count == 0 || IDsDescribedInMultipleLists(shotData.weaponChildrenData, killWeaponModWhitelists))
                    && (killWeaponModBlacklists == null || killWeaponModBlacklists.Count == 0 || !IDsDescribedInMultipleLists(shotData.weaponChildrenData, killWeaponModBlacklists))
                    && MatchesHealthEffectList(shotData.enemyHealthEffects, killEnemyHealthEffects)
                    && killBodyParts.Contains(shotData.bodyPart))
                {
                    ++condition.count;
                    condition.fulfilled = condition.count >= condition.value;
                }
            }
        }

        public void OnRaidExit(ExitStatus status, string exitID)
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.ExitStatus)
            {
                if (exitStatuses.Contains(status))
                {
                    List<string> zoneIDs = null;
                    for(int i=0; i < condition.counters.Count; ++i)
                    {
                        if (condition.counters[i].counterCreatorConditionType == CounterCreatorConditionType.InZone)
                        {
                            zoneIDs = condition.counters[i].zoneIDs;
                            break;
                        }
                    }
                    if(zoneIDs != null && zoneIDs.Contains(exitID))
                    {
                        TODO: // Also check for Location counter once we know how well set this up
                        ++condition.count;
                        condition.fulfilled = condition.count >= condition.value;
                    }
                }
            }
        }

        public void OnPlaceVisited(string placeID)
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.VisitPlace)
            {
                if (visitTarget.Equals(placeID))
                {
                    ++condition.count;
                    condition.fulfilled = condition.count >= condition.value;
                }
            }
        }

        public void OnFlareLaunched(string placeID)
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.LaunchFlare)
            {
                if (zoneIDs.Contains(placeID))
                {
                    ++condition.count;
                    condition.fulfilled = condition.count >= condition.value;
                }
            }
        }

        public void OnItemUsed()
        {
            if(counterCreatorConditionType == CounterCreatorConditionType.UseItem)
            {
                TODO: // Also check for Location counter once we know how well set this up
                ++condition.count;
                condition.fulfilled = condition.count >= condition.value;
            }
        }

        public static bool MatchesHealthEffectList(List<HealthEffectEntry> gotEntries, List<HealthEffectEntry> list)
        {
            for(int i = 0; i < list.Count; i++)
            {
                for(int j = 0; j < gotEntries.Count; ++j)
                {
                    if (list[i].Matches(gotEntries[j]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IDsDescribedInMultipleLists(List<MeatovItemData> items, List<List<string>> list)
        {
            for(int i = 0; i < list.Count; ++i)
            {
                for(int j=0; j < items.Count; ++j)
                {
                    if (list[i].Contains(items[j].H3ID))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool DoesEnemyTargetMatch(EnemyTarget wanted, EnemyTarget got, List<string> savageRoles, string gotSavageRole)
        {
            // Got can only ever be something specific (Savage, BEAR, or USEC)
            if (got == EnemyTarget.Savage)
            {
                return (wanted == EnemyTarget.Any || wanted == got) && (savageRoles == null || (gotSavageRole != null && savageRoles.Contains(gotSavageRole)));
            }
            else // Must be BEAR or USEC
            {
                return wanted == EnemyTarget.Any || wanted == EnemyTarget.AnyPmc || wanted == got;
            }
        }
    }

    public class HealthEffectEntry
    {
        // Static data
        public List<ConditionCounter.TargetBodyPart> bodyParts;
        public List<string> effects;

        public HealthEffectEntry(JToken data)
        {
            bodyParts = new List<ConditionCounter.TargetBodyPart>();
            JArray bodyPartsArray = data["bodyParts"] as JArray;
            for(int i=0; i < bodyPartsArray.Count; ++i)
            {
                bodyParts.Add((ConditionCounter.TargetBodyPart)Enum.Parse(typeof(ConditionCounter.TargetBodyPart), bodyPartsArray[i].ToString()));
            }
            JArray effectsArray = data["effects"] as JArray;
            effects = effectsArray.ToObject<List<string>>();
        }

        public bool Matches(HealthEffectEntry other)
        {
            bool hasBodyPart = false;
            for(int i=0; i < other.bodyParts.Count; ++i)
            {
                if (bodyParts.Contains(other.bodyParts[i]))
                {
                    hasBodyPart = true;
                    break;
                }
            }
            bool hasEffect = false;
            for(int i=0; i < other.effects.Count; ++i)
            {
                if (effects.Contains(other.effects[i]))
                {
                    hasEffect = true;
                    break;
                }
            }
            return hasBodyPart && hasEffect;
        }
    }

    public class VisibilityCondition
    {
        // Static data
        public Condition condition;
        public enum VisibilityConditionType
        {
            CompleteCondition
        }
        public VisibilityConditionType visibilityConditionType;
        // CompleteCondition
        public Condition targetCondition;

        public VisibilityCondition(Condition condition, Condition targetCondition)
        {
            this.condition = condition;

            visibilityConditionType = VisibilityConditionType.CompleteCondition;
            this.targetCondition = targetCondition;
        }
    }

    public class Reward
    {
        public Task task;

        public enum RewardType
        {
            Experience,
            TraderStanding,
            Item,
            TraderUnlock,
            AssortmentUnlock,
            Skill,
            ProductionScheme,
            TraderStandingRestore,
        }
        public RewardType rewardType;

        // Experience
        public int experience;

        // Standing, TraderUnlock, and AssortmentUnlock
        public Trader trader;

        // Standing
        public float standing;

        // AssormentUnlock
        public List<Barter> barters;

        // Item
        public List<string> itemIDs;
        public int amount;
        public bool foundInRaid = false;

        // Skill
        public Skill skill;
        public int value;

        public Reward(Task task, JToken data)
        {
            this.task = task;

            rewardType = (RewardType)Enum.Parse(typeof(RewardType), data["type"].ToString());
            switch (rewardType)
            {
                case RewardType.Experience:
                    experience = (int)data["value"];
                    break;
                case RewardType.TraderStanding:
                case RewardType.TraderStandingRestore:
                    trader = Mod.traders[Trader.IDToIndex(data["target"].ToString())];
                    standing = (float)data["value"];
                    break;
                case RewardType.Item:
                    JArray itemsArray = data["items"] as JArray;
                    itemIDs = new List<string>();
                    for (int i=0; i < itemsArray.Count; ++i)
                    {
                        itemIDs.Add(Mod.TarkovIDtoH3ID(itemsArray[i]["_tpl"].ToString()));
                    }
                    amount = (int)data["value"];
                    if(data["findInRaid"] != null)
                    {
                        foundInRaid = (bool)data["findInRaid"];
                    }
                    break;
                case RewardType.TraderUnlock:
                    trader = Mod.traders[Trader.IDToIndex(data["target"].ToString())];
                    break;
                case RewardType.AssortmentUnlock:
                    trader = Mod.traders[Trader.IDToIndex(data["traderId"].ToString())];
                    barters = new List<Barter>();
                    JArray assortItemsArray = data["items"] as JArray;
                    for (int i = 0; i < assortItemsArray.Count; ++i)
                    {
                        string itemID = Mod.TarkovIDtoH3ID(assortItemsArray[i]["_tpl"].ToString());
                        if (trader.bartersByItemID.TryGetValue(itemID, out List<Barter> currentBarters))
                        {
                            barters.AddRange(currentBarters);
                            for(int j=0; j < currentBarters.Count; ++j)
                            {
                                currentBarters[j].needUnlock = true;
                            }
                        }
                    }
                    break;
                case RewardType.Skill:
                    skill = Mod.skills[Skill.SkillNameToIndex(data["target"].ToString())];
                    value = (int)data["value"];
                    break;
                case RewardType.ProductionScheme:
                    // Note: This type of reward will be handled by the relevant production's QuestComplete 
                    //       condition which will be handled as an unlock condition
                    break;
            }
        }
    }
}
