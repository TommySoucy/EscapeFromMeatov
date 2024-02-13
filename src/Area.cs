using System;
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
        [NonSerialized]
        public int currentLevel;

        // Data
        public int[] constructionTimePerLevel; // In seconds
        public Requirement[][] requirementsPerLevel;
        public Bonus[][] bonusesPerLevel;

        // Power
        public bool requiresPower;
        [NonSerialized]
        public bool previousPowered;
        [NonSerialized]
        public bool powered;
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
            LoadData();

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

        public void LoadData()
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
    }

    public class Requirement
    {
        public Area area;
        public AreaRequirement areaRequirementUI;
        public RequirementItemView itemRequirementUI;
        public SkillRequirement skillRequirementUI;
        public TraderRequirement traderRequirementUI;

        public enum RequirementType
        {
            None,
            Item,
            Area,
            Skill,
            Trader
        }
        public RequirementType requirementType;

        // Item
        public string itemID;
        public int itemCount;

        // Area
        public int areaIndex;
        public int areaLevel;

        // Skill
        public int skillIndex;
        public int skillLevel;

        // Trader
        public string traderID;
        public int traderLevel;

        public Requirement(JToken requirementData)
        {
            requirementType = RequirementTypeFromName(requirementData["type"].ToString());

            switch (requirementType)
            {
                case RequirementType.Item:
                    if(Mod.itemMap.TryGetValue(requirementData["templateId"].ToString(), out ItemMapEntry entry))
                    {
                        itemID = entry.mode == 0 ? entry.ID : entry.moddedID;
                        itemCount = (int)requirementData["count"];
                    }
                    else
                    {
                        requirementType = RequirementType.None;
                    }
                    break;
                case RequirementType.Area:
                    areaIndex = (int)requirementData["areaType"];
                    areaLevel = (int)requirementData["requiredLevel"];
                    break;
                case RequirementType.Skill:
                    skillIndex = Mod.SkillNameToIndex(requirementData["skillName"].ToString());
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
                    traderID = requirementData["traderId"].ToString();
                    traderLevel = (int)requirementData["loyaltyLevel"];
                    break;
            }
        }

        public bool Fulfilled()
        {
            switch (requirementType)
            {
                case RequirementType.Item:
                    if(HideoutController.instance.inventory.TryGetValue(itemID, out int stashItemCount))
                    {
                        return stashItemCount >= itemCount;
                    }
                    else
                    {
                        return false;
                    }
                case RequirementType.Area:
                    return area.controller.areas[areaIndex].currentLevel >= areaLevel;
                case RequirementType.Skill:
                    return Mod.skills[skillIndex].progress / 100 >= skillLevel;
                case RequirementType.Trader:
                    return cont from here // Check based on loaded trader data
                default:
                    Mod.LogError("DEV: Tried to get Fulfilled on area requirement with None type");
                    return false;
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
                default:
                    Mod.LogError("DEV: Requirement.RequirementTypeFromName returning None for name: " + name);
                    return RequirementType.None;
            }
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
        todo
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
