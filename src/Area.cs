using System;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class Area : MonoBehaviour
    {
        // Main
        public AreaController controller;
        public int index;
        public int startLevel;

        // Data
        [NonSerialized]
        public int[] constructionTimePerLevel; // In seconds
        public Requirement[][] requirementsPerLevel;
        public Bonus[][] bonusesPerLevel;
        public List<List<Production>> productionsPerLevel;

        // Live
        [NonSerialized]
        public int currentLevel;
        [NonSerialized]
        public bool upgrading;
        public DateTime upgradeStartTime;
        public Dictionary<string, List<MeatovItem>> inventory;

        // Power
        public bool requiresPower;
        [NonSerialized]
        public bool previousPowered;
        [NonSerialized]
        public bool powered; // Live
        public MainAudioSources[] mainAudioSources;
        public MainAudioClips[] mainAudioClips;
        public Vector2s[] workingRanges;
        [NonSerialized]
        public AudioClip[][][] subClips;
        [NonSerialized]
        public bool poweringOn;

        // Objects
        public AreaUI UI;
        public GameObject[] levels;
        public GameObject[] objectsToToggle;
        public GameObjects[] objectsToTogglePerLevel;
        public AreaUpgradeCheckProcessorPair[] upgradeCheckProcessors;
        [NonSerialized]
        public AreaUpgradeCheckProcessor[] activeCheckProcessors;
        public AreaSlots[] areaSlotsPerLevel;
        public AreaVolumes[] areaVolumesPerLevel;
        public bool craftOuputSlot; // False is Volume, output will always be first in slot/vol per level

        public void Start()
        {
            LoadStaticData();

            UpdateObjectsPerLevel();

            if (mainAudioClips != null)
            {
                subClips = new AudioClip[levels.Length][][];
                for(int i=0; i< levels.Length; ++i)
                {
                    if(mainAudioClips[i] != null && mainAudioClips[i].Length > 0)
                    {
                        subClips[i] = new AudioClip[mainAudioClips[i].Length][];
                        for (int j=0; j< mainAudioClips[i].Length; ++j)
                        {
                            subClips[i][j] = new AudioClip[3];
                            subClips[i][j][0] = MakeSubclip(mainAudioClips[i][j], 0, workingRanges[i][j].x);
                            subClips[i][j][1] = MakeSubclip(mainAudioClips[i][j], workingRanges[i][j].x, workingRanges[i][j].y);
                            subClips[i][j][2] = MakeSubclip(mainAudioClips[i][j], workingRanges[i][j].y, mainAudioClips[i][j].length);
                        }
                    }
                }
            }

            if(objectsToToggle == null)
            {
                objectsToToggle = new GameObject[0];
            }

            LoadLiveData();

            // Init UI based on data
            UI.Init();

            // If powered at start, make sure correct audio is playing
            if (powered)
            {
                for (int i = 0; i < levels.Length; ++i)
                {
                    for (int j = 0; j < mainAudioSources[i].Length; ++j)
                    {
                        mainAudioSources[i][j].loop = true;
                        mainAudioSources[i][j].clip = subClips[i][j][1];
                        mainAudioSources[i][j].Play();
                    }
                }
                previousPowered = true;
            }
        }

        public void LoadStaticData()
        {
            JToken areaData = null;
            for (int i = 0; i < Mod.areasDB.Count; ++i)
            {
                if ((int)Mod.areasDB[i]["type"] == index)
                {
                    areaData = Mod.areasDB[i];
                }
            }

            constructionTimePerLevel = new int[levels.Length];
            requirementsPerLevel = new Requirement[levels.Length][];
            bonusesPerLevel = new Bonus[levels.Length][];
            productionsPerLevel = new List<List<Production>>();
            for (int i=0; i < levels.Length; ++i)
            {
                productionsPerLevel.Add(new List<Production>());
            }
            for (int i = 0; i < levels.Length; ++i)
            {
                JToken levelData = areaData["stages"][i.ToString()];
                constructionTimePerLevel[i] = levelData["constructionTime"] == null ? 0 : (int)levelData["constructionTime"];

                JArray levelRequirements = levelData["requirements"] as JArray;
                if (levelRequirements == null)
                {
                    requirementsPerLevel[i] = null;
                }
                else
                {
                    requirementsPerLevel[i] = new Requirement[levelRequirements.Count];
                    for (int j = 0; j < levelRequirements.Count; ++j)
                    {
                        Requirement currentRequirement = new Requirement(levelRequirements[j]);
                        if (currentRequirement.requirementType == Requirement.RequirementType.None)
                        {
                            requirementsPerLevel[i][j] = null;
                        }
                        else
                        {
                            requirementsPerLevel[i][j] = currentRequirement;
                        }
                    }
                }

                JArray levelBonuses = levelData["bonuses"] as JArray;
                if (levelBonuses == null)
                {
                    bonusesPerLevel[i] = null;
                }
                else
                {
                    bonusesPerLevel[i] = new Bonus[levelBonuses.Count];
                    for (int j = 0; j < levelBonuses.Count; ++j)
                    {
                        Bonus currentBonus = new Bonus(levelBonuses[j]);
                        if (currentBonus.bonusType == Bonus.BonusType.None)
                        {
                            bonusesPerLevel[i][j] = null;
                        }
                        else
                        {
                            bonusesPerLevel[i][j] = currentBonus;
                        }
                    }
                }
            }

            // Setup productions
            for(int i=0; i < Mod.productionsDB.Count; ++i)
            {
                JToken productionData = Mod.productionsDB[i];
                if ((int)productionData["areaType"] == index)
                {
                    Production newProduction = new Production(this, productionData);
                    productionsPerLevel[newProduction.areaLevel].Add(newProduction);
                }
            }
        }

        public void LoadLiveData()
        {
            powered = requiresPower && (bool)HideoutController.loadedData["hideout"]["powered"];
            previousPowered = powered;
            currentLevel = (int)HideoutController.loadedData["hideout"]["areas"][index]["level"];
            upgrading = (bool)HideoutController.loadedData["hideout"]["areas"][index]["upgrading"];
            if (upgrading)
            {
                upgradeStartTime = new DateTime((long)HideoutController.loadedData["hideout"]["areas"][index]["upgradeStartTime"]);
            }

            inventory = new Dictionary<string, List<MeatovItem>>();

            cont from ehre // Need to not load live data for reqs and productions
        }

        public void Update()
        {
            if (requiresPower)
            {
                if (powered && !previousPowered)
                {
                    // Manage audio
                    poweringOn = true;
                    for(int i=0;i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].PlayOneShot(subClips[i][j][0]);
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(true);
                    }
                }
                else if (!powered && previousPowered)
                {
                    // Manage audio
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].Stop();
                            mainAudioSources[i][j].PlayOneShot(subClips[i][j][2]);
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(false);
                    }
                }

                if (poweringOn && !mainAudioSources[currentLevel][0].isPlaying)
                {
                    poweringOn = false;
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].loop = true;
                            mainAudioSources[i][j].clip = subClips[i][j][1];
                            mainAudioSources[i][j].Play();
                        }
                    }
                }

                // Finally update previousPowered
                previousPowered = powered;
            }
        }

        public void UpdateObjectsPerLevel()
        {
            if (objectsToTogglePerLevel != null)
            {
                for (int i = 0; i < objectsToTogglePerLevel.Length; ++i)
                {
                    // Only enable object for current level
                    for (int j = 0; j < objectsToTogglePerLevel[i].Length; ++j)
                    {
                        objectsToTogglePerLevel[i][j].SetActive(i == currentLevel);
                    }
                }
            }
        }

        public static AudioClip MakeSubclip(AudioClip clip, float start, float stop)
        {
            int frequency = clip.frequency;
            float timeLength = stop - start;
            int samplesLength = (int)(frequency * timeLength);
            AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, 1, frequency, false);

            float[] data = new float[samplesLength];
            clip.GetData(data, (int)(frequency * start));
            newClip.SetData(data, 0);

            return newClip;
        }

        public bool AllRequirementsFulfilled()
        {
            // Already at highest level
            if(currentLevel == levels.Length - 1)
            {
                return false;
            }


            for(int i=0; i < requirementsPerLevel[currentLevel + 1].Length; ++i)
            {
                if(!requirementsPerLevel[currentLevel + 1][i].fulfilled)
                {
                    return false;
                }
            }

            return true;
        }
    }

    public class Requirement
    {
        public Area area;
        public Production production;
        public AreaRequirement areaRequirementUI;
        public RequirementItemView itemRequirementUI;
        public SkillRequirement skillRequirementUI;
        public TraderRequirement traderRequirementUI;
        public ResultItemView itemResultUI;
        public bool fulfilled;

        public enum RequirementType
        {
            None,
            Item,
            Area,
            Skill,
            Trader,
            Tool,
            Resource,
            QuestComplete,
        }
        public RequirementType requirementType;

        // Item, Tool, and Resource
        public string itemID;

        // Item
        public int itemCount;

        // Resource
        public int resourceCount;

        // Area
        public int areaIndex;
        public int areaLevel;

        // Skill
        public int skillIndex;
        public int skillLevel;

        // Trader
        public Trader trader;
        public int traderLevel;

        // QuestComplete
        public Task task;

        public Requirement(JToken requirementData)
        {
            requirementType = RequirementTypeFromName(requirementData["type"].ToString());

            switch (requirementType)
            {
                case RequirementType.Item:
                    itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                    itemCount = (int)requirementData["count"];
                    break;
                case RequirementType.Area:
                    areaIndex = (int)requirementData["areaType"];
                    areaLevel = (int)requirementData["requiredLevel"];
                    break;
                case RequirementType.Skill:
                    skillIndex = Skill.SkillNameToIndex(requirementData["skillName"].ToString());
                    if(skillIndex == -1)
                    {
                        requirementType = RequirementType.None;
                    }
                    else
                    {
                        skillLevel = (int)requirementData["skillLevel"];
                    }
                    break;
                case RequirementType.Trader:
                    trader = Mod.traders[Trader.IDToIndex(requirementData["traderId"].ToString())];
                    traderLevel = (int)requirementData["loyaltyLevel"];
                    break;
                case RequirementType.Tool:
                    itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                    break;
                case RequirementType.Resource:
                    itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                    resourceCount = (int)requirementData["resource"];
                    break;
                case RequirementType.QuestComplete:
                    task = Task.allTasks[requirementData["questId"].ToString()];
                    break;
            }
        }

        public static RequirementType RequirementTypeFromName(string name)
        {
            switch (name)
            {
                case "Item":
                    return RequirementType.Item;
                case "Area":
                    return RequirementType.Area;
                case "Skill":
                    return RequirementType.Skill;
                case "TraderLoyalty":
                    return RequirementType.Trader;
                case "Tool":
                    return RequirementType.Tool;
                case "Resource":
                    return RequirementType.Resource;
                case "QuestComplete":
                    return RequirementType.QuestComplete;
                default:
                    Mod.LogError("DEV: Requirement.RequirementTypeFromName returning None for name: " + name);
                    return RequirementType.None;
            }
        }

        public void OnAreaInventoryChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Item:
                    int count = 0;
                    if(area.inventory.TryGetValue(itemID, out List<MeatovItem> currentItems))
                    {
                        fulfilled = currentItems.Count >= itemCount;
                        count = currentItems.Count;
                    }
                    else
                    {
                        fulfilled = false;
                    }
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Mathf.Min(count, itemCount).ToString() + "/" + itemCount;
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    if (production == null) // Not a production requirement, must be area upgrade requirement
                    {
                        if (area.AllRequirementsFulfilled())
                        {
                            if (area.currentLevel == area.startLevel) // Area still needs to be constructed, no next page, just enable construct button
                            {
                                area.UI.constructButton.SetActive(true);
                            }
                            else // Area already constructed, but only display upgrade button if on next page
                            {
                                if (area.UI.onNextPage)
                                {
                                    area.UI.upgradeButton.SetActive(true);
                                }
                            }
                        }
                        else
                        {
                            area.UI.constructButton.SetActive(false);
                            area.UI.upgradeButton.SetActive(false);
                        }
                    }
                    else if (production.UI != null && !production.inProduction && !production.continuous)
                    {
                        production.UI.startButton.SetActive(fulfilled);
                    }
                    break;
                case RequirementType.Tool:
                    int toolCount = 0;
                    if (area.inventory.TryGetValue(itemID, out List<MeatovItem> toolCurrentItems))
                    {
                        fulfilled = toolCurrentItems.Count >= 1;
                        toolCount = toolCurrentItems.Count;
                    }
                    else
                    {
                        fulfilled = false;
                    }
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Mathf.Min(toolCount, 1).ToString() + "/1";
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    if (production == null) // Not a production requirement, must be area upgrade requirement
                    {
                        if (area.AllRequirementsFulfilled())
                        {
                            if (area.currentLevel == area.startLevel) // Area still needs to be constructed, no next page, just enable construct button
                            {
                                area.UI.constructButton.SetActive(true);
                            }
                            else // Area already constructed, but only display upgrade button if on next page
                            {
                                if (area.UI.onNextPage)
                                {
                                    area.UI.upgradeButton.SetActive(true);
                                }
                            }
                        }
                        else
                        {
                            area.UI.constructButton.SetActive(false);
                            area.UI.upgradeButton.SetActive(false);
                        }
                    }
                    else if (production.UI != null && !production.inProduction && !production.continuous)
                    {
                        production.UI.startButton.SetActive(fulfilled);
                    }
                    break;
                case RequirementType.Resource:
                    int totalAmount = 0;
                    if (area.inventory.TryGetValue(itemID, out List<MeatovItem> resourceCurrentItems))
                    {
                        for(int i=0; i < resourceCurrentItems.Count; ++i)
                        {
                            totalAmount += resourceCurrentItems[i].amount;
                        }
                        fulfilled = totalAmount >= resourceCount;
                    }
                    else
                    {
                        fulfilled = false;
                    }
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Mathf.Min(totalAmount, resourceCount).ToString() + "/" + resourceCount;
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    if (production == null) // Not a production requirement, must be area upgrade requirement
                    {
                        if (area.AllRequirementsFulfilled())
                        {
                            if (area.currentLevel == area.startLevel) // Area still needs to be constructed, no next page, just enable construct button
                            {
                                area.UI.constructButton.SetActive(true);
                            }
                            else // Area already constructed, but only display upgrade button if on next page
                            {
                                if (area.UI.onNextPage)
                                {
                                    area.UI.upgradeButton.SetActive(true);
                                }
                            }
                        }
                        else
                        {
                            area.UI.constructButton.SetActive(false);
                            area.UI.upgradeButton.SetActive(false);
                        }
                    }
                    else if (production.UI != null && !production.inProduction && !production.continuous)
                    {
                        production.UI.startButton.SetActive(fulfilled);
                    }
                    break;
            }
        }

        public void OnAreaSlotContentChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Resource:
                    int totalAmount = 0;
                    cont from ehre // handle this case specifically for water collector production
                    if (area.inventory.TryGetValue(itemID, out List<MeatovItem> resourceCurrentItems))
                    {
                        for (int i = 0; i < resourceCurrentItems.Count; ++i)
                        {
                            totalAmount += resourceCurrentItems[i].amount;
                        }
                        fulfilled = totalAmount >= resourceCount;
                    }
                    else
                    {
                        fulfilled = false;
                    }
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Mathf.Min(totalAmount, resourceCount).ToString() + "/" + resourceCount;
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    if (production == null) // Not a production requirement, must be area upgrade requirement
                    {
                        if (area.AllRequirementsFulfilled())
                        {
                            if (area.currentLevel == area.startLevel) // Area still needs to be constructed, no next page, just enable construct button
                            {
                                area.UI.constructButton.SetActive(true);
                            }
                            else // Area already constructed, but only display upgrade button if on next page
                            {
                                if (area.UI.onNextPage)
                                {
                                    area.UI.upgradeButton.SetActive(true);
                                }
                            }
                        }
                        else
                        {
                            area.UI.constructButton.SetActive(false);
                            area.UI.upgradeButton.SetActive(false);
                        }
                    }
                    else if (production.UI != null && !production.inProduction && !production.continuous)
                    {
                        production.UI.startButton.SetActive(fulfilled);
                    }
                    break;
            }
        }

        public void OnAreaLevelChanged()
        {

        }

        public void OnTraderLevelChanged()
        {

        }
    }

    public class Bonus
    {
        public Area area;
        public BonusUI bonusUI;

        public enum BonusType
        {
            None,
            EnergyRegeneration,
            DebuffEndDelay,
            AdditionalSlots,
            UnlockArmorRepair,
            RepairArmorBonus,
            StashSize,
            HydrationRegeneration,
            HealthRegeneration,
            TextBonus,
            MaximumEnergyReserve,
            ScavCooldownTimer,
            QuestMoneyReward,
            InsuranceReturnTime,
            RagfairCommission,
            ExperienceRate,
            SkillGroupLevelingBoost,
            FuelConsumption,
            UnlockWeaponModification,
            UnlockWeaponRepair,
            RepairWeaponBonus,
        }
        public BonusType bonusType;

        public int value;
        public bool passive;
        public bool visible;
        public bool production;

        // AdditionalSlots, TextBonus
        public string iconPath;

        // TextBonus
        public string ID;

        // SkillGroupLevelingBoost
        public Skill.SkillType skillType;

        public Bonus(JToken bonusData)
        {
            bonusType = BonusTypeFromName(bonusData["type"].ToString());

            if(bonusData["value"] != null)
            {
                value = (int)bonusData["value"];
            }
            if (bonusData["passive"] != null)
            {
                passive = (bool)bonusData["passive"];
            }
            if (bonusData["production"] != null)
            {
                production = (bool)bonusData["production"];
            }
            if (bonusData["visible"] != null)
            {
                visible = (bool)bonusData["visible"];
            }
            if (bonusData["icon"] != null)
            {
                iconPath = bonusData["icon"].ToString();
            }
            if (bonusData["id"] != null)
            {
                ID = bonusData["id"].ToString();
            }
            if (bonusData["skillType"] != null)
            {
                skillType = Skill.SkillTypeFromName(bonusData["skillType"].ToString());
            }
        }

        public static BonusType BonusTypeFromName(string name)
        {
            switch (name)
            {
                case "EnergyRegeneration":
                    return BonusType.EnergyRegeneration;
                case "DebuffEndDelay":
                    return BonusType.DebuffEndDelay;
                case "AdditionalSlots":
                    return BonusType.AdditionalSlots;
                case "UnlockArmorRepair":
                    return BonusType.UnlockArmorRepair;
                case "RepairArmorBonus":
                    return BonusType.RepairArmorBonus;
                case "StashSize":
                    return BonusType.StashSize;
                case "HydrationRegeneration":
                    return BonusType.HydrationRegeneration;
                case "HealthRegeneration":
                    return BonusType.HealthRegeneration;
                case "TextBonus":
                    return BonusType.TextBonus;
                case "MaximumEnergyReserve":
                    return BonusType.MaximumEnergyReserve;
                case "ScavCooldownTimer":
                    return BonusType.ScavCooldownTimer;
                case "QuestMoneyReward":
                    return BonusType.QuestMoneyReward;
                case "InsuranceReturnTime":
                    return BonusType.InsuranceReturnTime;
                case "RagfairCommission":
                    return BonusType.RagfairCommission;
                case "ExperienceRate":
                    return BonusType.ExperienceRate;
                case "SkillGroupLevelingBoost":
                    return BonusType.SkillGroupLevelingBoost;
                case "FuelConsumption":
                    return BonusType.FuelConsumption;
                case "UnlockWeaponModification":
                    return BonusType.UnlockWeaponModification;
                case "UnlockWeaponRepair":
                    return BonusType.UnlockWeaponRepair;
                case "RepairWeaponBonus":
                    return BonusType.RepairWeaponBonus;
                default:
                    Mod.LogError("DEV: Bonus.BonusTypeFromName returning None for name: " + name);
                    return BonusType.None;
            }
        }
    }

    public class Production
    {
        // Static data
        public Area area;
        public ProductionView UI;
        public int areaLevel;
        public int time; // Seconds
        public bool needFuelForAllProductionTime;
        public string endProduct;
        public bool continuous;
        public int limit;
        public int count;
        public List<Requirement> requirements;

        // Live data
        public bool inProduction;
        public DateTime productionStartTime;

        public Production(Area area, JToken data)
        {
            this.area = area;

            time = (int)data["productionTime"];
            needFuelForAllProductionTime = (bool)data["needFuelForAllProductionTime"];
            endProduct = Mod.TarkovIDtoH3ID(data["endProduct"].ToString());
            continuous = (bool)data["continuous"];
            count = (int)data["count"];
            limit = (int)data["productionLimitCount"];

            requirements = new List<Requirement>();
            JArray requirementsArray = data["requirements"] as JArray;
            for(int i=0; i < requirementsArray.Count; ++i)
            {
                Requirement newRequirement = new Requirement(requirementsArray[i]);
                newRequirement.production = this;
                newRequirement.area = area;

                if(newRequirement.requirementType == Requirement.RequirementType.Area && newRequirement.areaIndex == area.index)
                {
                    areaLevel = newRequirement.areaLevel;
                }
            }
        }
    }

    [Serializable]
    public class MainAudioSources
    {
        public AudioSource[] mainAudioSources; 
        
        public AudioSource this[int i]
        {
            get { return mainAudioSources[i]; }
            set { mainAudioSources[i] = value; }
        }

        public int Length
        {
            get { return mainAudioSources.Length; }
        }
    }

    [Serializable]
    public class MainAudioClips
    {
        public AudioClip[] mainAudioClips; 
        
        public AudioClip this[int i]
        {
            get { return mainAudioClips[i]; }
            set { mainAudioClips[i] = value; }
        }

        public int Length
        {
            get { return mainAudioClips.Length; }
        }
    }

    [Serializable]
    public class Vector2s
    {
        public Vector2[] workingRanges; 
        
        public Vector2 this[int i]
        {
            get { return workingRanges[i]; }
            set { workingRanges[i] = value; }
        }

        public int Length
        {
            get { return workingRanges.Length; }
        }
    }

    [Serializable]
    public class GameObjects
    {
        public GameObject[] objectsToTogglePerLevel; 
        
        public GameObject this[int i]
        {
            get { return objectsToTogglePerLevel[i]; }
            set { objectsToTogglePerLevel[i] = value; }
        }

        public int Length
        {
            get { return objectsToTogglePerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaSlots
    {
        public AreaSlot[] areaSlotsPerLevel; 
        
        public AreaSlot this[int i]
        {
            get { return areaSlotsPerLevel[i]; }
            set { areaSlotsPerLevel[i] = value; }
        }

        public int Length
        {
            get { return areaSlotsPerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaVolumes
    {
        public AreaVolume[] areaVolumesPerLevel; 
        
        public AreaVolume this[int i]
        {
            get { return areaVolumesPerLevel[i]; }
            set { areaVolumesPerLevel[i] = value; }
        }

        public int Length
        {
            get { return areaVolumesPerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaUpgradeCheckProcessorPair
    {
        public AreaUpgradeCheckProcessor[] areaUpgradeCheckProcessors; 
        
        public AreaUpgradeCheckProcessor this[int i]
        {
            get { return areaUpgradeCheckProcessors[i]; }
            set { areaUpgradeCheckProcessors[i] = value; }
        }
    }
}
