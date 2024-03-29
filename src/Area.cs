﻿using FMOD;
using System;
using System.Collections.Generic;
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
        public static float GPUBoostRate;
        [NonSerialized]
        public int[] constructionTimePerLevel; // In seconds
        public Dictionary<Requirement.RequirementType, List<Requirement>>[] requirementsByTypePerLevel;
        public Bonus[][] bonusesPerLevel;
        public List<List<Production>> productionsPerLevel;
        public Dictionary<string, Production> productionsByID;
        public bool hasReadyProduction;

        // Live
        private int _currentLevel;
        public int currentLevel
        {
            get { return _currentLevel; }
            set 
            {
                int preLevel = _currentLevel;
                _currentLevel = value;
                if(preLevel != _currentLevel)
                {
                    OnAreaLevelChangedInvoke();
                }
            }
        }
        [NonSerialized]
        public bool upgrading;
        [NonSerialized]
        public float upgradeTimeLeft; // Amount of seconds currently ongoing upgrade still has to go
        [NonSerialized]
        public List<Production> activeProductions = new List<Production>();

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

        public delegate void OnSlotContentChangedDelegate();
        public event OnSlotContentChangedDelegate OnSlotContentChanged;

        public delegate void OnAreaLevelChangedDelegate();
        public event OnAreaLevelChangedDelegate OnAreaLevelChanged;

        public void Start()
        {
            // Special case for christmas tree (21)
            // Disable instead of init if not in season
            if(index == 21)
            {
                DateTime now = DateTime.UtcNow;
                if ((now.Month != 12 || now.Day < 15)
                    && (now.Month != 1 || now.Day > 15))
                {
                    gameObject.SetActive(false);
                    return;
                }
            }

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
                    break;
                }
            }

            constructionTimePerLevel = new int[levels.Length];
            requirementsByTypePerLevel = new Dictionary<Requirement.RequirementType, List<Requirement>>[levels.Length];
            bonusesPerLevel = new Bonus[levels.Length][];
            productionsPerLevel = new List<List<Production>>();
            productionsByID = new Dictionary<string, Production>();
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
                    requirementsByTypePerLevel[i] = null;
                }
                else
                {
                    requirementsByTypePerLevel[i] = new Dictionary<Requirement.RequirementType, List<Requirement>>();
                    for (int j = 0; j < levelRequirements.Count; ++j)
                    {
                        Requirement currentRequirement = new Requirement(levelRequirements[j], this);
                        if (requirementsByTypePerLevel[i].TryGetValue(currentRequirement.requirementType, out List<Requirement> currentRequirements))
                        {
                            currentRequirements.Add(currentRequirement);
                        }
                        else
                        {
                            requirementsByTypePerLevel[i].Add(currentRequirement.requirementType, new List<Requirement>() { currentRequirement });
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
                        Bonus currentBonus = new Bonus(levelBonuses[j], this);
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

            // Special case for Bitcoin Farm (20)
            // Has its bitcoin production but no requirements are listed
            // So no requirements to control whether it is in production or not
            // In this case, the production will handle this itself
            if(index == 20)
            {
                OnSlotContentChanged += productionsPerLevel[startLevel + 1][0].OnBitcoinFarmSlotContentChanged;
            }

            // Special case for Scav Case (14)
            // Productions stored in separate file (scavcase.json)
            if(index == 14)
            {
                for (int i = 0; i < Mod.scavCaseProductionsDB.Count; ++i)
                {
                    JToken productionData = Mod.scavCaseProductionsDB[i];
                    Production newProduction = new Production(this, productionData, true);
                    productionsPerLevel[newProduction.areaLevel].Add(newProduction);
                }
            }
        }

        public void LoadLiveData()
        {
            powered = requiresPower && (bool)HideoutController.loadedData["hideout"]["powered"];
            previousPowered = powered;
            currentLevel = (int)HideoutController.loadedData["hideout"]["areas"][index]["level"];
            if(currentLevel == 0)
            {
                currentLevel = startLevel;
            }
            upgrading = (bool)HideoutController.loadedData["hideout"]["areas"][index]["upgrading"];
            if (upgrading)
            {
                upgradeTimeLeft = (float)HideoutController.loadedData["hideout"]["areas"][index]["upgradeTimeLeft"] - (float)HideoutController.secondsSinceSave;
            }

            // Production live data has been loaded upon their instantiation
            // Here, we need to make sure that all requirements are up to date with live data
            // Note that certain requirements will also update themselves as we initialize other areas
            // as the corresponding events are raised
            if(currentLevel < levels.Length - 1)
            {
                Dictionary<Requirement.RequirementType, List<Requirement>> requirements = requirementsByTypePerLevel[currentLevel + 1];
                foreach(KeyValuePair<Requirement.RequirementType, List<Requirement>> requirementEntry in requirements)
                {
                    for (int i = 0; i < requirementEntry.Value.Count; ++i)
                    {
                        requirementEntry.Value[i].UpdateFulfilled();
                    }
                }
            }
            for(int i = 0; i < productionsPerLevel.Count; ++i)
            {
                for(int j = 0; j< productionsPerLevel[i].Count; ++j)
                {
                    for(int k=0; k< productionsPerLevel[i][j].requirements.Count; ++k)
                    {
                        productionsPerLevel[i][j].requirements[k].UpdateFulfilled();
                    }
                }
            }

            // Set area based on live data
            for(int i=0; i < levels.Length; ++i)
            {
                levels[i].SetActive(i == currentLevel);
            }

            // Special case for bitcoin farm
            productionsPerLevel[startLevel + 1][0].OnBitcoinFarmSlotContentChanged();
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

            for (int i = 0; i <= currentLevel; ++i)
            {
                for (int j = 0; j < productionsPerLevel[i].Count; ++j)
                {
                    productionsPerLevel[i][j].Update();
                }
            }

            if (upgrading)
            {
                upgradeTimeLeft -= Time.deltaTime;

                if(upgradeTimeLeft <= 0)
                {
                    upgrading = false;

                    ++currentLevel;

                    for (int i = 0; i < levels.Length; ++i)
                    {
                        levels[i].SetActive(i == currentLevel);
                    }

                    UI.Init();

                    UI.genericAudioSource.PlayOneShot(UI.genericAudioClips[2]);
                }

                UI.UpdateStatusTexts();
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

            Dictionary<Requirement.RequirementType, List<Requirement>> requirements = requirementsByTypePerLevel[currentLevel + 1];
            foreach (KeyValuePair<Requirement.RequirementType, List<Requirement>> requirementEntry in requirements)
            {
                for (int i = 0; i < requirementEntry.Value.Count; ++i)
                {
                    if (!requirementEntry.Value[i].fulfilled)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void OnSlotContentChangedInvoke()
        {
            // Raise event
            if (OnSlotContentChanged != null)
            {
                OnSlotContentChanged();
            }
        }

        public void OnAreaLevelChangedInvoke()
        {
            // Raise event
            if (OnAreaLevelChanged != null)
            {
                OnAreaLevelChanged();
            }
        }

        public void OnBeginProduction(Production production)
        {
            activeProductions.Add(production);
        }

        public void OnStopProduction(Production production)
        {
            activeProductions.Remove(production);

            if(production.readyCount > 0)
            {
                hasReadyProduction = true;
                if (!upgrading)
                {
                    UI.summaryIconProductionBackground.SetActive(true);
                    UI.fullIconProductionBackground.SetActive(true);
                }
            }
        }

        public List<Production> GetActiveProductions()
        {
            List<Production> productions = new List<Production>();
            for(int i=startLevel; i <= currentLevel; ++i)
            {
                if(productionsPerLevel[currentLevel] != null)
                {
                    for (int j = 0; j < productionsPerLevel[currentLevel].Count; ++j)
                    {
                        if (productionsPerLevel[currentLevel][j].inProduction)
                        {
                            productions.Add(productionsPerLevel[currentLevel][j]);
                        }
                    }
                }
            }
            return productions;
        }

        public void BeginUpgrade()
        {
            // Set into upgrade state
            upgrading = true;
            upgradeTimeLeft = constructionTimePerLevel[currentLevel + 1];

            // Consume requirements
            List<Requirement> itemRequirements = requirementsByTypePerLevel[currentLevel + 1][Requirement.RequirementType.Item];
            for(int i=0; i < itemRequirements.Count; ++i)
            {
                Requirement itemRequirement = itemRequirements[i];
                int countLeft  = itemRequirement.itemCount;
                while(countLeft > 0)
                {
                    MeatovItem item = GetClosestItem(itemRequirement.itemID);
                    if(item.stack > countLeft)
                    {
                        item.stack -= countLeft;
                        countLeft = 0;
                        break;
                    }
                    else
                    {
                        countLeft -= item.stack;
                        Destroy(item.gameObject);
                    }
                }
            }
        }

        public MeatovItem GetClosestItem(string H3ID)
        {
            if(HideoutController.instance.inventoryItems.TryGetValue(H3ID, out List<MeatovItem> items))
            {
                MeatovItem closest = null;
                float closestDistance = float.MaxValue;
                for(int i=0; i < items.Count; ++i)
                {
                    if (closest == null)
                    {
                        closestDistance = Vector3.Distance(items[i].transform.position, transform.position - Vector3.down);
                        closest = items[i];
                    }
                    else
                    {
                        float distance = Vector3.Distance(items[i].transform.position, transform.position - Vector3.down);
                        if(distance < closestDistance)
                        {
                            closestDistance = distance;
                            closest = items[i];
                        }
                    }
                }

                return closest;
            }
            else
            {
                return null;
            }
        }

        public void SetUpgradeCheckProcessors(bool active)
        {
            if(activeCheckProcessors != null)
            {
                if (activeCheckProcessors[0] != null)
                {
                    activeCheckProcessors[0].gameObject.SetActive(false);
                }
                if (activeCheckProcessors[1] != null)
                {
                    activeCheckProcessors[1].gameObject.SetActive(false);
                }
            }

            if(upgradeCheckProcessors != null && upgradeCheckProcessors[currentLevel] != null)
            {
                if (upgradeCheckProcessors[currentLevel][0] != null)
                {
                    upgradeCheckProcessors[currentLevel][0].gameObject.SetActive(true);
                }
                if (upgradeCheckProcessors[currentLevel][1] != null)
                {
                    upgradeCheckProcessors[currentLevel][1].gameObject.SetActive(true);
                }
            }
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
        public ResultItemView stashItemUI;
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
        public int itemCount = 1;

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

        public Requirement(JToken requirementData, Area area = null, Production production = null)
        {
            if(requirementData != null)
            {
                this.area = area;
                this.production = production;
                requirementType = RequirementTypeFromName(requirementData["type"].ToString());

                switch (requirementType)
                {
                    case RequirementType.Item:
                        itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        if(requirementData["count"] != null)
                        {
                            itemCount = (int)requirementData["count"];
                        }
                        HideoutController.instance.OnHideoutInventoryChanged += OnHideoutInventoryChanged;
                        area.OnSlotContentChanged += OnAreaSlotContentChanged;
                        break;
                    case RequirementType.Area:
                        areaIndex = (int)requirementData["areaType"];
                        areaLevel = (int)requirementData["requiredLevel"];
                        area.controller.areas[areaIndex].OnAreaLevelChanged += OnAreaLevelChanged;
                        break;
                    case RequirementType.Skill:
                        skillIndex = Skill.SkillNameToIndex(requirementData["skillName"].ToString());
                        if (skillIndex == -1)
                        {
                            requirementType = RequirementType.None;
                        }
                        else
                        {
                            skillLevel = (int)requirementData["skillLevel"];
                        }
                        Mod.skills[skillIndex].OnSkillLevelChanged += OnSkillLevelChanged;
                        break;
                    case RequirementType.Trader:
                        trader = Mod.traders[Trader.IDToIndex(requirementData["traderId"].ToString())];
                        traderLevel = (int)requirementData["loyaltyLevel"];
                        trader.OnTraderLevelChanged += OnTraderLevelChanged;
                        break;
                    case RequirementType.Tool:
                        itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        HideoutController.instance.OnHideoutInventoryChanged += OnHideoutInventoryChanged;
                        break;
                    case RequirementType.Resource:
                        itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        resourceCount = (int)requirementData["resource"];
                        HideoutController.instance.OnHideoutInventoryChanged += OnHideoutInventoryChanged;
                        area.OnSlotContentChanged += OnAreaSlotContentChanged;
                        break;
                    case RequirementType.QuestComplete:
                        task = Task.allTasks[requirementData["questId"].ToString()];
                        task.OnTaskStateChanged += OnTaskStateChanged;
                        break;
                }
            }
        }

        public Requirement(int areaIndex, int areaLevel)
        {
            requirementType = RequirementType.Area;
            this.areaIndex = areaIndex;
            this.areaLevel = areaLevel;
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

        public void UpdateFulfilled()
        {
            OnHideoutInventoryChanged();
            OnAreaSlotContentChanged();
            OnAreaLevelChanged();
            OnTraderLevelChanged();
            OnSkillLevelChanged();
            OnTaskStateChanged();
        }

        public void OnHideoutInventoryChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Item:
                    int count = 0;
                    if(HideoutController.inventory.TryGetValue(itemID, out count))
                    {
                        fulfilled = count >= itemCount;
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
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Mathf.Min(count, itemCount).ToString() + "/" + itemCount;
                    }
                    if(stashItemUI != null)
                    {
                        stashItemUI.amount.text = count.ToString() + "\n(STASH)";
                    }
                    break;
                case RequirementType.Tool:
                    int toolCount = 0;
                    if (HideoutController.inventory.TryGetValue(itemID, out int toolCurrentItemCount))
                    {
                        fulfilled = toolCurrentItemCount >= 1;
                        toolCount = toolCurrentItemCount;
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
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Mathf.Min(toolCount, 1).ToString() + "/1";
                    }
                    break;
                case RequirementType.Resource:
                    int totalAmount = 0;
                    if (HideoutController.inventory.TryGetValue(itemID, out totalAmount))
                    {
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
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Mathf.Min(totalAmount, resourceCount).ToString() + "/" + resourceCount;
                    }
                    if (stashItemUI != null)
                    {
                        stashItemUI.amount.text = totalAmount.ToString() + "\n(STASH)";
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnAreaSlotContentChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Resource:
                    int totalAmount = 0;
                    for(int i=0; i < area.areaSlotsPerLevel[area.currentLevel].Length; ++i)
                    {
                        if(area.areaSlotsPerLevel[area.currentLevel][i].item != null)
                        {
                            totalAmount += area.areaSlotsPerLevel[area.currentLevel][i].item.amount;
                        }
                    }
                    // Note that here we only check greater than 0
                    // Slot resource requirements are really only used for continuous productions
                    // These requirements specify an amount to COMPLETE the production
                    // but the production itself should be in production if we have more than 0 amount
                    fulfilled = totalAmount >= 0;
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Mathf.Min(totalAmount, resourceCount).ToString();
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Mathf.Min(totalAmount, resourceCount).ToString() + "\n(INSTALLED)";
                    }
                    break;
                case RequirementType.Item:
                    int itemCount = 0;
                    for(int i=0; i < area.areaSlotsPerLevel[area.currentLevel].Length; ++i)
                    {
                        if(area.areaSlotsPerLevel[area.currentLevel][i].item != null)
                        {
                            ++itemCount;
                        }
                    }
                    // Note that here we only check greater than 0
                    // Slot item requirements are really only used for continuous productions
                    // The production itself should be in production if we have more than 0 item installed
                    fulfilled = itemCount >= 0;
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = itemCount.ToString();
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = itemCount.ToString() + "\n(INSTALLED)";
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnAreaLevelChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Area:
                    fulfilled = area.controller.areas[areaIndex].currentLevel >= areaLevel;
                    if (areaRequirementUI != null)
                    {
                        areaRequirementUI.fulfilled.SetActive(fulfilled);
                        areaRequirementUI.unfulfilled.SetActive(!fulfilled);
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnTraderLevelChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Trader:
                    fulfilled = trader.level >= traderLevel;
                    if (traderRequirementUI)
                    {
                        traderRequirementUI.fulfilled.SetActive(fulfilled);
                        traderRequirementUI.unfulfilled.SetActive(!fulfilled);
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnSkillLevelChanged()
        {
            switch (requirementType)
            {
                case RequirementType.Skill:
                    fulfilled = Mod.skills[skillIndex].progress/100 >= traderLevel;
                    if (skillRequirementUI != null)
                    {
                        skillRequirementUI.fulfilled.SetActive(fulfilled);
                        skillRequirementUI.unfulfilled.SetActive(!fulfilled);
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnTaskStateChanged()
        {
            switch (requirementType)
            {
                case RequirementType.QuestComplete:
                    fulfilled = task.taskState == Task.TaskState.Complete;
                    break;
            }

            UpdateAreaUI();
        }

        public void UpdateAreaUI()
        {
            if (production == null) // Not a production requirement, must be area upgrade requirement
            {
                UpdateAreaUpgradeUI();
            }
            else
            {
                production.unlocked = production.AllUnlockRequirementsFulfilled();
                if (production.productionUI != null)
                {
                    production.productionUI.gameObject.SetActive(production.unlocked);
                    if (production.continuous)
                    {
                        if (production.unlocked)
                        {
                            production.inProduction = production.AllRequirementsFulfilled();
                        }
                    }
                    else if (!production.inProduction && production.limit > production.readyCount)
                    {
                        production.productionUI.startButton.SetActive(production.AllRequirementsFulfilled());
                    }
                }
            }
        }

        public void UpdateAreaUpgradeUI()
        {
            area.UI.UpdateBottomButtons();
            area.UI.UpdateStatusIcons();
            area.UI.UpdateStatusTexts();
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

        public Bonus(JToken bonusData, Area area)
        {
            this.area = area;
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
        public string ID;
        public ProductionView productionUI;
        public FarmingView farmingUI;
        public bool scavCase;
        public ScavCaseView scavCaseUI;
        public int areaLevel;
        public int time; // Seconds
        public bool needFuelForAllProductionTime;
        public string endProduct;
        public Vector2Int[] endProductRarities;
        public bool continuous;
        public int limit;
        public int count;
        public List<Requirement> requirements;

        // Live data
        public bool unlocked;
        public bool previousInProduction;
        private bool _inProduction;
        public bool inProduction
        {
            get{ return _inProduction; }
            set
            {
                if (value && !inProduction)
                {
                    OnBeginProductionInvoke();
                }
                else if (!value && inProduction)
                {
                    OnStopProductionInvoke();
                }
                _inProduction = value;
            }
        }
        public float timeLeft;
        public bool timeLeftSet;
        public float progressBaseTime; // The total time we need to base ourselves on when calculating progress, may be different than time, like in the case of bitcoin farm, total time depends on number of GPUs
        public float progress; // Percentage
        public int readyCount;

        public delegate void OnBeginProductionDelegate(Production production);
        public event OnBeginProductionDelegate OnBeginProduction;

        public delegate void OnStopProductionDelegate(Production production);
        public event OnStopProductionDelegate OnStopProduction;

        public Production(Area area, JToken data, bool scavCase = false)
        {
            this.area = area;
            OnBeginProduction += area.OnBeginProduction;
            OnStopProduction += area.OnStopProduction;

            ID = data["_id"].ToString();

            area.productionsByID.Add(ID, this);
            this.scavCase = scavCase;
            JArray requirementsArray = null;
            if (scavCase)
            {
                endProductRarities = new Vector2Int[3];
                if (data["EndProducts"]["Common"] != null)
                {
                    endProductRarities[0] = new Vector2Int((int)data["EndProducts"]["Common"]["min"], (int)data["EndProducts"]["Common"]["max"]);
                }
                else
                {
                    endProductRarities[0] = new Vector2Int(0, 0);
                }
                if (data["EndProducts"]["Rare"] != null)
                {
                    endProductRarities[1] = new Vector2Int((int)data["EndProducts"]["Rare"]["min"], (int)data["EndProducts"]["Rare"]["max"]);
                }
                else
                {
                    endProductRarities[1] = new Vector2Int(0, 0);
                }
                if (data["EndProducts"]["Superrare"] != null)
                {
                    endProductRarities[2] = new Vector2Int((int)data["EndProducts"]["Superrare"]["min"], (int)data["EndProducts"]["Superrare"]["max"]);
                }
                else
                {
                    endProductRarities[2] = new Vector2Int(0, 0);
                }
                time = (int)data["ProductionTime"];
                limit = 1;
                requirementsArray = data["Requirements"] as JArray;
            }
            else
            {
                endProduct = Mod.TarkovIDtoH3ID(data["endProduct"].ToString());
                needFuelForAllProductionTime = (bool)data["needFuelForAllProductionTime"];
                continuous = (bool)data["continuous"];
                count = (int)data["count"];
                limit = (int)data["productionLimitCount"];
                time = (int)data["productionTime"];
                requirementsArray = data["requirements"] as JArray;
            }
            progressBaseTime = time;

            requirements = new List<Requirement>();
            bool foundProductionAreaRequirement = false;
            for (int i = 0; i < requirementsArray.Count; ++i)
            {
                Requirement newRequirement = new Requirement(requirementsArray[i], area, this);

                if (newRequirement.requirementType == Requirement.RequirementType.Area && newRequirement.areaIndex == area.index)
                {
                    areaLevel = newRequirement.areaLevel;
                    foundProductionAreaRequirement = true;
                }
            }

            // Bitcoin farm special case
            // We want to make sure its production has a GPU resource requirement
            if (area.index == 20)
            {
                Requirement newRequirement = new Requirement(null, area, this);
                newRequirement.requirementType = Requirement.RequirementType.Item;
                newRequirement.itemID = "159";
                newRequirement.resourceCount = 0;
                area.OnSlotContentChanged += newRequirement.OnAreaSlotContentChanged;
            }

            if (!foundProductionAreaRequirement)
            {
                // This is to handle cases like bitcoin farm and scav case productions
                // If production does not specify the level this production 
                // should be listed in, we assume it should be listed since startLevel + 1
                areaLevel = area.startLevel + 1;
                Requirement newRequirement = new Requirement(area.index, areaLevel);
                newRequirement.production = this;
                newRequirement.area = area;
                area.OnAreaLevelChanged += newRequirement.OnAreaLevelChanged;
            }

            // Load live data
            if(HideoutController.loadedData["hideout"]["areas"][area.index]["productions"][ID] != null)
            {
                inProduction = false;
                progress = 0;
                readyCount = 0;
            }
            else
            {
                JToken productionData = HideoutController.loadedData["hideout"]["areas"][area.index]["productions"][ID];
                inProduction = (bool)productionData["inProduction"];
                progress = (float)productionData["progress"];
                readyCount = (int)productionData["readyCount"];
            }
            if (readyCount > 0)
            {
                area.hasReadyProduction = true;
            }
        }

        public void Update()
        {
            if (inProduction)
            {
                // Only need to start if progress == 0, otherwise we are resuming
                if (!previousInProduction && progress == 0)
                {
                    // When production is started, we might already have a time set for us
                    // Like for bitcoin farm for example
                    // At which point it is just like resuming, so only start of no time is set
                    if (!timeLeftSet)
                    {
                        timeLeft = time;
                    }
                }

                timeLeft -= Time.deltaTime;
                if(timeLeft <= 0)
                {
                    ++readyCount;
                    if (scavCase)
                    {
                        scavCaseUI.getButton.SetActive(true);
                    }
                    else
                    {
                        if (continuous)
                        {
                            farmingUI.getButton.SetActive(true);
                        }
                        else
                        {
                            productionUI.getButton.SetActive(true);
                        }
                    }

                    if (readyCount == limit)
                    {
                        inProduction = false;
                        if (scavCase)
                        {
                            scavCaseUI.productionStatus.SetActive(false);
                            scavCaseUI.timePanel.percentage.gameObject.SetActive(false);
                        }
                        else
                        {
                            if (continuous)
                            {
                                farmingUI.productionStatus.SetActive(false);
                                farmingUI.timePanel.percentage.gameObject.SetActive(false);
                            }
                            else
                            {
                                productionUI.productionStatus.SetActive(false);
                                productionUI.timePanel.percentage.gameObject.SetActive(false);
                            }
                        }
                    }
                    else
                    {
                        if (scavCase)
                        {
                            scavCaseUI.productionStatus.SetActive(false);
                            scavCaseUI.timePanel.percentage.gameObject.SetActive(false);
                            scavCaseUI.startButton.SetActive(AllRequirementsFulfilled());
                            inProduction = false;
                        }
                        else
                        {
                            if (continuous)
                            {
                                timeLeft = progressBaseTime;
                                farmingUI.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(timeLeft) + ")...";
                                progress = 0;
                                productionUI.timePanel.percentage.text = ((int)progress).ToString() + "%";
                            }
                            else
                            {
                                productionUI.productionStatus.SetActive(false);
                                productionUI.timePanel.percentage.gameObject.SetActive(false);
                                productionUI.startButton.SetActive(AllRequirementsFulfilled());
                                inProduction = false;
                            }
                        }
                    }
                }
                else
                {
                    progress = (1 - timeLeft / progressBaseTime) * 100;
                    if (continuous)
                    {
                        TODO: // Have a timer for each resource requirement and consume resource as needed
                        farmingUI.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(timeLeft) + ")...";
                        farmingUI.timePanel.percentage.text = ((int)progress).ToString() + "%";
                    }
                    else
                    {
                        productionUI.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(timeLeft) + ")...";
                        productionUI.timePanel.percentage.text = ((int)progress).ToString() + "%";
                    }
                }
            }

            previousInProduction = inProduction;
        }

        public bool AllRequirementsFulfilled()
        {
            for (int i = 0; i < requirements.Count; ++i)
            {
                if (!requirements[i].fulfilled)
                {
                    return false;
                }
            }

            return true;
        }

        public bool AllUnlockRequirementsFulfilled()
        {
            for (int i = 0; i < requirements.Count; ++i)
            {
                if (requirements[i].requirementType == Requirement.RequirementType.Area 
                    && requirements[i].requirementType == Requirement.RequirementType.QuestComplete
                    && !requirements[i].fulfilled)
                {
                    return false;
                }
            }

            return true;
        }

        public void OnBitcoinFarmSlotContentChanged()
        {
            // Time per bitcoin (s): productionsPerLevel[currentLevel][0].time/(1+(GC-1)*GPUBoostRate)
            // Time left if already in progress: (Time per bitcoin) - (Time per bitcoin) * (productionsPerLevel[currentLevel][0].progress / 100)
            int GPUCount = 0;
            for (int i = 0; i < area.areaSlotsPerLevel[area.currentLevel].Length; ++i)
            {
                if (area.areaSlotsPerLevel[area.currentLevel][i].item != null)
                {
                    ++GPUCount;
                }
            }
            timeLeft = time / (1 + (GPUCount - 1) * Area.GPUBoostRate);
            progressBaseTime = timeLeft;
            farmingUI.timePanel.requiredTime.text = Mod.FormatTimeString(progressBaseTime);
            timeLeft = timeLeft - timeLeft * (progress / 100);
            timeLeftSet = true;
            inProduction = GPUCount > 0;
        }

        public void OnBeginProductionInvoke()
        {
            if(OnBeginProduction != null)
            {
                OnBeginProduction(this);
            }
        }

        public void OnStopProductionInvoke()
        {
            if(OnStopProduction != null)
            {
                OnStopProduction(this);
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
