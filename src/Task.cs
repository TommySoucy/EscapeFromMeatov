using System.Collections.Generic;

namespace EFM
{
    public class Task
    {
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
        public TaskState taskState;

        // Objects
        public TaskUI marketUI;
        public TaskUI playerUI;
    }

    public class Condition
    {
        // Static data
        public enum CompareMethod
        {
            GreaterEqual, // >=
            SmallerEqual // <=
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
            VisitPlace,
            WeaponAssembly
        }
        public ConditionType conditionType;
        public string description;
        public List<VisibilityCondition> visibilityConditions;
        public float minDurability;
        public float maxDurability;
        public int value;
        public bool onlyFoundInRaid;
        public int dogtagLevel;
        public List<string> targetItemIDs;
        public CompareMethod compareMethod;
        public string zoneID;
        // CounterCreator
        public List<ConditionCounter> counters;
        public bool counterOneSessionOnly;
        // FindItem
        public bool findItemCountInRaid;
        public List<string> findItemTargetItemIDs;
        // Quest
        public Task questTargetTask;
        public List<Task.TaskState> questTaskStates;
        public int questAvailableAfter;
        public int questDispersion;
        // LeaveItemAtLocation
        public float plantTime; // Seconds
        // TraderLoyalty
        public Trader targetTrader;
        // Skill
        public Skill targetSkill;
        // WeaponAssembly
        public List<string> weaponTargets;
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
        public List<string> weaponHasItemFromCategories;
        public int weaponMagazineCapacity;
        public CompareMethod weaponMagazineCapacityCompareMethod;
        public float weaponMuzzleVelocity;
        public CompareMethod weaponMuzzleVelocityCompareMethod;
        public float weaponRecoil;
        public CompareMethod weaponRecoilCompareMethod;
        public float weaponWeight;
        public CompareMethod weaponWeightCompareMethod;

        // Live data
        public int count;

        // Objects
        public TaskObjectiveUI marketUI;
        public TaskObjectiveUI playerUI;
    }

    public class ConditionCounter
    {
        // Static data
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
            UseItem
        }
        public CounterCreatorConditionType counterCreatorConditionType;
        // Kills
        public Condition.CompareMethod killCompareMethod;
        public Condition.CompareMethod killDistanceCompareMethod; // Could be omitted from DB
        public int killDistance; // Could be omitted from DB
        public enum EnemyTarget
        {
            Scav, // Savage
            PMC, // AnyPmc
            Usec,
            Bear,
            Any
        }
        public EnemyTarget killTarget;
        public List<string> killSavageRoles; // Could be omitted from DB
        public int killValue;
        public List<string> killWeaponWhitelist; // Could be omitted from DB
        public List<List<string>> killWeaponModWhitelists; // Could be omitted from DB
        public enum TargetBodyPart
        {
            Head,
            Thorax,
            Stomach,
            LeftArm,
            RightArm,
            LeftLeg,
            RightLeg
        }
        public List<TargetBodyPart> killBodyParts; // Could be omitted from DB
        public Vector2Int killTime; // Could be omitted from DB
        // HealthEffect
        public List<TargetBodyPart> healthEffectBodyParts;
        public List<string> healthEffects;
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
        // Shots
        public Condition.CompareMethod shotCompareMethod;
        public Condition.CompareMethod shotDistanceCompareMethod;
        public int shotDistance;
        public EnemyTarget shotTarget;
        public int shotValue;
        public List<string> shotWeaponWhitelist; // Could be omitted from DB
        public List<List<string>> shotWeaponModWhitelists; // Could be omitted from DB
        public List<TargetBodyPart> shotBodyParts;
        // UseItem
        public Condition.CompareMethod useItemCompareMethod;
        public int useItemValue;
        public List<string> useItemTargets;
    }

    public class VisibilityCondition
    {
        // Static data
        public enum VisibilityConditionType
        {
            CompleteCondition
        }
        public VisibilityConditionType visibilityConditionType;
        // CompleteCondition
        public string targetCondition;
    }

    public class Reward
    {
        public enum RewardType
        {
            Experience,
            TraderStanding,
            Item,
            TraderUnlock,
            AssortmentUnlock
        }
        public RewardType rewardType;

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
