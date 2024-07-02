using FistVR;
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
        public static float fuelConsumptionRate;
        public static float filterConsumptionRate;
        [NonSerialized]
        public int[] constructionTimePerLevel; // In seconds
        public Dictionary<Requirement.RequirementType, List<Requirement>>[] requirementsByTypePerLevel;
        public Bonus[][] bonusesPerLevel;
        public List<List<Production>> productionsPerLevel;
        public Dictionary<string, Production> productionsByID;
        public Dictionary<string, List<Production>> productionsByProductID;
        public int readyProdutionCount;

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
                    OnAreaLevelChangedInvoke(this);
                }
            }
        }
        [NonSerialized]
        public bool upgrading;
        [NonSerialized]
        public float upgradeTimeLeft; // Amount of seconds currently ongoing upgrade still has to go
        [NonSerialized]
        public List<Production> activeProductions = new List<Production>();
        [NonSerialized]
        public bool init;

        // Power
        public bool requiresPower;
        [NonSerialized]
        public bool previousPowered;
        [NonSerialized]
        public bool powered; // Live
        [NonSerialized]
        public AudioClip[][][] subClips;
        [NonSerialized]
        public bool poweringOn;
        public float fuelConsumptionTimer;
        public bool previouslyConsumingFilter;
        public float filterConsumptionTimer;

        // Objects
        public Transform UIRoot;
        public AreaUI UI;
        public AreaLevelData[] levels;
        public GameObject[] objectsToToggle;
        [NonSerialized]
        public AreaUpgradeCheckProcessor[] activeCheckProcessors;
        public bool craftOuputSlot; // False is Volume, output will always be first in slot/vol per level
        public AudioClip[] genericAudioClips; // AreaSelected, UpgradeBegin, UpgradeComplete, ItemInstalled, ItemStarted, ItemComplete

        public delegate void OnSlotContentChangedDelegate();
        public event OnSlotContentChangedDelegate OnSlotContentChanged;

        public delegate void OnAreaLevelChangedDelegate(Area area);
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

            UpdateObjectsPerLevel();

            subClips = new AudioClip[levels.Length][][];
            for(int i=0; i< levels.Length; ++i)
            {
                if(levels[i].mainAudioClips != null && levels[i].mainAudioClips.Length > 0)
                {
                    subClips[i] = new AudioClip[levels[i].mainAudioClips.Length][];
                    for (int j=0; j< levels[i].mainAudioClips.Length; ++j)
                    {
                        subClips[i][j] = new AudioClip[3];
                        subClips[i][j][0] = levels[i].workingRanges[j].x == 0 ? null : MakeSubclip(levels[i].mainAudioClips[j], 0, levels[i].workingRanges[j].x);
                        subClips[i][j][1] = MakeSubclip(levels[i].mainAudioClips[j], levels[i].workingRanges[j].x, levels[i].workingRanges[j].y);
                        subClips[i][j][2] = levels[i].workingRanges[j].y == levels[i].mainAudioClips[j].length ? null : MakeSubclip(levels[i].mainAudioClips[j], levels[i].workingRanges[j].y, levels[i].mainAudioClips[j].length);
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
                    for (int j = 0; j < levels[i].mainAudioSources.Length; ++j)
                    {
                        if (subClips[i][j][1] != null)
                        {
                            levels[i].mainAudioSources[j].loop = true;
                            levels[i].mainAudioSources[j].clip = subClips[i][j][1];
                            levels[i].mainAudioSources[j].Play();
                        }
                    }
                }
                previousPowered = true;
            }

            init = true;
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
            productionsByProductID = new Dictionary<string, List<Production>>();
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

        public void Save(JToken data)
        {
            HideoutController.loadedData["hideout"]["powered"] = powered;
            data["level"] = currentLevel;
            data["upgrading"] = upgrading;
            data["upgradeTimeLeft"] = upgradeTimeLeft;

            JObject productions = new JObject();
            for(int i=0; i < productionsPerLevel.Count; ++i)
            {
                for(int j=0; j < productionsPerLevel[i].Count; ++j)
                {
                    JObject production = new JObject();
                    productionsPerLevel[i][j].Save(production);
                    productions[productionsPerLevel[i][j].ID] = production;
                }
            }
            data["productions"] = productions;
        }

        public void LoadLiveData()
        {
            Mod.LogInfo("LoadLiveData on area " + index);
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
                levels[i].gameObject.SetActive(i == currentLevel);
            }

            // Special case for bitcoin farm
            if(index == 20)
            {
                productionsPerLevel[startLevel + 1][0].OnBitcoinFarmSlotContentChanged();
            }
        }

        public void Update()
        {
            if (requiresPower)
            {
                if (powered && !previousPowered) // Just powered on
                {
                    // Manage audio
                    poweringOn = true;
                    for(int i=0;i < levels.Length; ++i)
                    {
                        for (int j = 0; j < levels[i].mainAudioSources.Length; ++j)
                        {
                            if(subClips[i][j][0] != null)
                            {
                                levels[i].mainAudioSources[j].PlayOneShot(subClips[i][j][0]);
                            }
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(true);
                    }

                    // Resource consumption and specific bonuses
                    if(index == 4) // Generator, fuel consumption
                    {
                        if (fuelConsumptionTimer <= 0)
                        {
                            // Consume fuel
                            bool consumed = false;
                            for (int i = 0; i < levels[currentLevel].areaSlots.Length; ++i)
                            {
                                if (levels[currentLevel].areaSlots[i].item != null && levels[currentLevel].areaSlots[i].item.amount > 0)
                                {
                                    --levels[currentLevel].areaSlots[i].item.amount;
                                    break;
                                }
                            }

                            // Reset timer or turn off if no fuel
                            if (consumed)
                            {
                                fuelConsumptionTimer = 1 / fuelConsumptionRate;
                                fuelConsumptionTimer += fuelConsumptionTimer / 100 * Bonus.fuelConsumption;
                                for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                                {
                                    if (bonusesPerLevel[currentLevel][i].production)
                                    {
                                        bonusesPerLevel[currentLevel][i].Apply();
                                    }
                                }
                            }
                            else
                            {
                                controller.TogglePower();
                            }
                        }
                    }
                    else if (index == 17) // AFU, begin filter consumption if necessary
                    {
                        if (filterConsumptionTimer <= 0)
                        {
                            // Consume filter
                            bool consumed = false;
                            for (int i = 0; i < levels[currentLevel].areaSlots.Length; ++i)
                            {
                                if (levels[currentLevel].areaSlots[i].item != null && levels[currentLevel].areaSlots[i].item.amount > 0)
                                {
                                    --levels[currentLevel].areaSlots[i].item.amount;
                                    break;
                                }
                            }

                            // Reset timer and apply bonuses if consumed
                            if (consumed)
                            {
                                filterConsumptionTimer = 1 / filterConsumptionRate;
                                for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                                {
                                    if (bonusesPerLevel[currentLevel][i].production)
                                    {
                                        bonusesPerLevel[currentLevel][i].Apply();
                                    }
                                }
                            }
                        }
                    }

                    // Bonuses
                    for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                    {
                        // Note that we only apply bonuses that are non passive (require power) and not production (but require no resources)
                        if (!bonusesPerLevel[currentLevel][i].passive && !bonusesPerLevel[currentLevel][i].production)
                        {
                            bonusesPerLevel[currentLevel][i].Apply();
                        }
                    }
                }
                else if (!powered && previousPowered) // Just powered off
                {
                    // Manage audio
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < levels[i].mainAudioSources.Length; ++j)
                        {
                            levels[i].mainAudioSources[j].Stop();
                            if(subClips[i][j][2] != null)
                            {
                                levels[i].mainAudioSources[j].PlayOneShot(subClips[i][j][2]);
                            }
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(false);
                    }

                    // Bonuses
                    // Note that we don't manage resource consumption for generator and AFU here
                    // as it will be done on update, where if not powered, resources will not be consumed
                    for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                    {
                        // Note that unlike power on, here we unapply all bonuses that are non passive (require power) 
                        // regardless of whether it needs resources or not
                        if (!bonusesPerLevel[currentLevel][i].passive)
                        {
                            bonusesPerLevel[currentLevel][i].Unapply();
                        }
                    }
                }

                // Manage powering on audio
                if (poweringOn && !levels[currentLevel].mainAudioSources[0].isPlaying)
                {
                    poweringOn = false;
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < levels[i].mainAudioSources.Length; ++j)
                        {
                            if (subClips[i][j][1] != null)
                            {
                                levels[i].mainAudioSources[j].loop = true;
                                levels[i].mainAudioSources[j].clip = subClips[i][j][1];
                                levels[i].mainAudioSources[j].Play();
                            }
                        }
                    }
                }

                // Finally update previousPowered
                previousPowered = powered;
            }

            // Update productions
            for (int i = 0; i <= currentLevel; ++i)
            {
                for (int j = 0; j < productionsPerLevel[i].Count; ++j)
                {
                    productionsPerLevel[i][j].Update();
                }
            }

            // Process upgrade
            if (upgrading)
            {
                upgradeTimeLeft -= Time.deltaTime;

                if(upgradeTimeLeft <= 0)
                {
                    upgrading = false;

                    ++currentLevel;

                    for (int i = 0; i < levels.Length; ++i)
                    {
                        levels[i].gameObject.SetActive(i == currentLevel);
                    }

                    UI.Init();

                    UI.genericAudioSource.PlayOneShot(genericAudioClips[2]);

                    ReturnTools();

                    // Apply new bonuses
                    // Note that production bonuses are not applied, those are controlled through specific updates below
                    for(int i=0; i < bonusesPerLevel[currentLevel].Length; ++i)
                    {
                        if ((bonusesPerLevel[currentLevel][i].passive || powered) && !bonusesPerLevel[currentLevel][i].production)
                        {
                            bonusesPerLevel[currentLevel][i].Apply();
                        }
                    }
                }

                UI.UpdateStatusTexts();
            }

            // Specific updates
            if(index == 4) // Generator, consume fuel if power is on
            {
                if (powered && fuelConsumptionTimer > 0)
                {
                    fuelConsumptionTimer -= Time.deltaTime;
                    if(fuelConsumptionTimer <= 0)
                    {
                        // Consume fuel
                        bool consumed = false;
                        for (int i = 0; i < levels[currentLevel].areaSlots.Length; ++i)
                        {
                            if (levels[currentLevel].areaSlots[i].item != null && levels[currentLevel].areaSlots[i].item.amount > 0)
                            {
                                --levels[currentLevel].areaSlots[i].item.amount;
                                break;
                            }
                        }

                        // Reset timer or turn off if we ran out of fuel
                        if (consumed)
                        {
                            fuelConsumptionTimer = 1/fuelConsumptionRate;
                            fuelConsumptionTimer += fuelConsumptionTimer / 100 * Bonus.fuelConsumption;
                        }
                        else
                        {
                            controller.TogglePower();
                            for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                            {
                                if (bonusesPerLevel[currentLevel][i].production)
                                {
                                    bonusesPerLevel[currentLevel][i].Unapply();
                                }
                            }
                        }
                    }
                }
            }
            else if(index == 17) // AFU, consume filter resource is power is on
            {
                if (powered && filterConsumptionTimer > 0) 
                {
                    filterConsumptionTimer -= Time.deltaTime;
                    if (filterConsumptionTimer <= 0)
                    {
                        // Consume filter
                        bool consumed = false;
                        for (int i = 0; i < levels[currentLevel].areaSlots.Length; ++i)
                        {
                            if (levels[currentLevel].areaSlots[i].item != null && levels[currentLevel].areaSlots[i].item.amount > 0)
                            {
                                --levels[currentLevel].areaSlots[i].item.amount;
                                break;
                            }
                        }

                        // Reset timer or disable bonus if ran out of filter resource
                        if (consumed)
                        {
                            filterConsumptionTimer = 1 / filterConsumptionRate;
                        }
                        else
                        {
                            for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                            {
                                if (bonusesPerLevel[currentLevel][i].production)
                                {
                                    bonusesPerLevel[currentLevel][i].Unapply();
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ReturnTools()
        {
            if(requirementsByTypePerLevel[currentLevel].TryGetValue(Requirement.RequirementType.Tool, out List<Requirement> toolRequirements))
            {
                for (int i = 0; i < toolRequirements.Count; ++i)
                {
                    if (toolRequirements[i].requirementType == Requirement.RequirementType.Tool)
                    {
                        // In case item is vanilla, in which case we use the vault system to save it,
                        // we will only be getting the instantiated item later
                        // We must write a delegate in order to add it to the area volume later
                        VaultSystem.ReturnObjectListDelegate del = objs =>
                        {
                            // Here, assume objs[0] is the root item
                            MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                            if (meatovItem != null)
                            {
                                if (levels[currentLevel].areaVolumes != null && levels[currentLevel].areaVolumes.Length > 0)
                                {
                                    levels[currentLevel].areaVolumes[0].AddItem(meatovItem);
                                }
                                else // No output volume, spawn item in trade volume
                                {
                                    HideoutController.instance.marketManager.tradeVolume.AddItem(meatovItem);
                                }
                            }
                        };

                        // In case item is custom, it will be returned right away and we can handle it here
                        MeatovItem loadedItem = MeatovItem.Deserialize(toolRequirements[i].serializedTool, del);

                        if (loadedItem != null)
                        {
                            if (levels[currentLevel].areaVolumes != null && levels[currentLevel].areaVolumes.Length > 0)
                            {
                                levels[currentLevel].areaVolumes[0].AddItem(loadedItem);
                            }
                            else // No output volume, spawn item in trade volume
                            {
                                HideoutController.instance.marketManager.tradeVolume.AddItem(loadedItem);
                            }
                        }
                    }
                }
            }
        }

        public void UpdateObjectsPerLevel()
        {
            for (int i = 0; i < levels.Length; ++i)
            {
                // Only enable object for current level
                for (int j = 0; j < levels[i].objectsToToggle.Length; ++j)
                {
                    levels[i].objectsToToggle[j].SetActive(i == currentLevel);
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
            // Area specific
            if(index == 17) // AFU
            {
                if (powered && filterConsumptionTimer <= 0)
                {
                    // Consume filter
                    bool consumed = false;
                    for (int i = 0; i < levels[currentLevel].areaSlots.Length; ++i)
                    {
                        if (levels[currentLevel].areaSlots[i].item != null && levels[currentLevel].areaSlots[i].item.amount > 0)
                        {
                            --levels[currentLevel].areaSlots[i].item.amount;
                            break;
                        }
                    }

                    // Reset timer and apply bonuses if consumed
                    if (consumed)
                    {
                        filterConsumptionTimer = 1 / filterConsumptionRate;
                        for (int i = 0; i < bonusesPerLevel[currentLevel].Length; ++i)
                        {
                            if (bonusesPerLevel[currentLevel][i].production)
                            {
                                bonusesPerLevel[currentLevel][i].Apply();
                            }
                        }
                    }
                }
            }

            // Raise event
            if (OnSlotContentChanged != null)
            {
                OnSlotContentChanged();
            }
        }

        public void OnAreaLevelChangedInvoke(Area area)
        {
            // Raise event
            if (OnAreaLevelChanged != null)
            {
                OnAreaLevelChanged(area);
            }
        }

        public void OnBeginProduction(Production production)
        {
            activeProductions.Add(production);
        }

        public void OnStopProduction(Production production)
        {
            activeProductions.Remove(production);

            UI.UpdateStatusIcons();
            UI.UpdateStatusTexts();
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
            if(requirementsByTypePerLevel[currentLevel + 1].TryGetValue(Requirement.RequirementType.Item, out List<Requirement> itemRequirements))
            {
                for (int i = 0; i < itemRequirements.Count; ++i)
                {
                    Requirement itemRequirement = itemRequirements[i];
                    int countLeft = itemRequirement.itemCount;
                    while (countLeft > 0)
                    {
                        MeatovItem item = GetClosestItem(itemRequirement.item.H3ID);
                        if (item.stack > countLeft)
                        {
                            item.stack -= countLeft;
                            countLeft = 0;
                            break;
                        }
                        else
                        {
                            countLeft -= item.stack;
                            item.DetachChildren();
                            item.Destroy();
                        }
                    }
                }
            }
            if(requirementsByTypePerLevel[currentLevel + 1].TryGetValue(Requirement.RequirementType.Tool, out List<Requirement> toolRequirements))
            {
                for (int i = 0; i < toolRequirements.Count; ++i)
                {
                    Requirement toolRequirement = toolRequirements[i];
                    int countLeft = toolRequirement.itemCount;
                    while (countLeft > 0)
                    {
                        MeatovItem item = GetClosestItem(toolRequirement.item.H3ID);
                        if (item.stack > countLeft)
                        {
                            item.stack -= countLeft;
                            countLeft = 0;
                            break;
                        }
                        else
                        {
                            countLeft -= item.stack;
                            item.DetachChildren();
                            if (toolRequirement.requirementType == Requirement.RequirementType.Tool)
                            {
                                toolRequirement.serializedTool = item.Serialize();
                            }
                            item.Destroy();
                        }
                    }
                }
            }
            if(requirementsByTypePerLevel[currentLevel + 1].TryGetValue(Requirement.RequirementType.Resource, out List<Requirement> resourceRequirements))
            {
                for (int i = 0; i < resourceRequirements.Count; ++i)
                {
                    Requirement resourceRequirement = resourceRequirements[i];
                    int countLeft = resourceRequirement.resourceCount;
                    while (countLeft > 0)
                    {
                        MeatovItem item = GetClosestItem(resourceRequirement.item.H3ID);
                        if (item.amount > countLeft)
                        {
                            item.amount -= countLeft;
                            countLeft = 0;
                            break;
                        }
                        else
                        {
                            countLeft -= item.amount;
                            item.DetachChildren();
                            item.Destroy();
                        }
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
            else if (Mod.playerInventoryItems.TryGetValue(H3ID, out List<MeatovItem> playerItems))
            {
                MeatovItem closest = null;
                float closestDistance = float.MaxValue;
                for (int i = 0; i < playerItems.Count; ++i)
                {
                    if (closest == null)
                    {
                        closestDistance = Vector3.Distance(playerItems[i].transform.position, transform.position - Vector3.down);
                        closest = playerItems[i];
                    }
                    else
                    {
                        float distance = Vector3.Distance(playerItems[i].transform.position, transform.position - Vector3.down);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closest = playerItems[i];
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
            // Check for length because serialized array will not be null but will have no elements
            if(activeCheckProcessors != null && activeCheckProcessors.Length > 0)
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

            if(levels[currentLevel].areaUpgradeCheckProcessors != null && levels[currentLevel].areaUpgradeCheckProcessors.Length > 0)
            {
                if (levels[currentLevel].areaUpgradeCheckProcessors[0] != null)
                {
                    levels[currentLevel].areaUpgradeCheckProcessors[0].gameObject.SetActive(true);
                }
                if (levels[currentLevel].areaUpgradeCheckProcessors[1] != null)
                {
                    levels[currentLevel].areaUpgradeCheckProcessors[1].gameObject.SetActive(true);
                }
            }
        }

        public static string IndexToName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Vents";
                case 1:
                    return "Security";
                case 2:
                    return "Lavatory";
                case 3:
                    return "Stash";
                case 4:
                    return "Generator";
                case 5:
                    return "Heating";
                case 6:
                    return "Water Collector";
                case 7:
                    return "Med Station";
                case 8:
                    return "Kitchen";
                case 9:
                    return "Rest Space";
                case 10:
                    return "Workbench";
                case 11:
                    return "Intelligence Center";
                case 12:
                    return "Shooting Range";
                case 13:
                    return "Library";
                case 14:
                    return "Scav Case";
                case 15:
                    return "Illumination";
                case 16:
                    return "Place Of Fame";
                case 17:
                    return "Air Filtering Unit";
                case 18:
                    return "Solar Power";
                case 19:
                    return "Booze Generator";
                case 20:
                    return "Bitcoin Farm";
                case 21:
                    return "Christmas Tree";
                case 22:
                    return "Leaking Wall";
                case 23:
                    return "Gym";
                case 24:
                    return "Weapon Stand";
                case 25:
                    return "Secondary Weapon Stand";
                default:
                    Mod.LogError("Area IndexToName called with invalid index: " + index);
                    return "";
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
        public MeatovItemData item;

        // Item
        public int itemCount = 1;

        // Tool
        public JObject serializedTool;

        // Resource
        public int resourceCount;
        public float resourceConsumptionTime = -1;
        public float resourceConsumptionTimer;

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
                        string itemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        if(!Mod.GetItemData(itemID, out item))
                        {
                            Mod.LogError("DEV: " + (production == null ? "Area " + area.index : "Prodution " + production.ID) + " item requirement targets item " + itemID + " for which we do not have data");
                            fulfilled = true;
                            return;
                        }

                        if (requirementData["count"] != null)
                        {
                            itemCount = (int)requirementData["count"];
                        }
                        // Really just want to handle a change in slot content if we are continuous production req
                        if (production != null && production.continuous)
                        {
                            area.OnSlotContentChanged += OnAreaSlotContentChanged;
                        }
                        else // Either we are area upgrade req or we are production but not continuous so not slot dependent
                        {
                            item.OnHideoutItemInventoryChanged += OnInventoryChanged;
                            item.OnPlayerItemInventoryChanged += OnInventoryChanged;
                        }
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
                        string toolItemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        if (!Mod.GetItemData(toolItemID, out item))
                        {
                            Mod.LogError("DEV: " + (production == null ? "Area " + area.index : "Prodution " + production.ID) + " tool requirement targets item " + toolItemID + " for which we do not have data");
                            fulfilled = true;
                            return;
                        }

                        item.OnHideoutItemInventoryChanged += OnInventoryChanged;
                        item.OnPlayerItemInventoryChanged += OnInventoryChanged;
                        break;
                    case RequirementType.Resource:
                        string resourceItemID = Mod.TarkovIDtoH3ID(requirementData["templateId"].ToString());
                        if (!Mod.GetItemData(resourceItemID, out item))
                        {
                            Mod.LogError("DEV: " + (production == null ? "Area " + area.index : "Prodution " + production.ID) + " item requirement targets item " + resourceItemID + " for which we do not have data");
                            fulfilled = true;
                            return;
                        }

                        resourceCount = (int)requirementData["resource"];
                        // Really just want to handle a change in slot content if we are continuous production req
                        if (production != null && production.continuous)
                        {
                            area.OnSlotContentChanged += OnAreaSlotContentChanged;
                        }
                        else // Either we are area upgrade req or we are production but not continuous so not slot dependent
                        {
                            item.OnHideoutItemInventoryChanged += OnInventoryChanged;
                            item.OnPlayerItemInventoryChanged += OnInventoryChanged;
                        }
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
            OnInventoryChanged(-1);
            OnAreaSlotContentChanged();
            OnAreaLevelChanged(area);
            OnTraderLevelChanged(null);
            OnSkillLevelChanged();
            OnTaskStateChanged(null);
        }

        public void OnInventoryChanged(int difference)
        {
            if(item == null)
            {
                return;
            }

            switch (requirementType)
            {
                case RequirementType.Item:
                    long count = Mod.GetItemCountInInventories(item.H3ID);
                    fulfilled = count >= itemCount;
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Extensions.Min(count, itemCount).ToString() + "/" + itemCount;
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Extensions.Min(count, itemCount).ToString() + "/" + itemCount;
                    }
                    if(stashItemUI != null)
                    {
                        stashItemUI.amount.text = count.ToString() + "\n(INVENTORY)";
                    }
                    break;
                case RequirementType.Tool:
                    long toolCount = Mod.GetItemCountInInventories(item.H3ID);
                    fulfilled = toolCount >= 1;
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Extensions.Min(toolCount, 1).ToString() + "/1";
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Extensions.Min(toolCount, 1).ToString() + "/1";
                    }
                    break;
                case RequirementType.Resource:
                    long totalAmount = Mod.GetItemCountInInventories(item.H3ID);
                    fulfilled = totalAmount >= resourceCount;
                    if (itemRequirementUI != null)
                    {
                        itemRequirementUI.amount.text = Extensions.Min(totalAmount, resourceCount).ToString() + "/" + resourceCount;
                        itemRequirementUI.fulfilledIcon.SetActive(fulfilled);
                        itemRequirementUI.unfulfilledIcon.SetActive(!fulfilled);
                    }
                    else if (itemResultUI != null)
                    {
                        itemResultUI.amount.text = Extensions.Min(totalAmount, resourceCount).ToString() + "/" + resourceCount;
                    }
                    if (stashItemUI != null)
                    {
                        stashItemUI.amount.text = totalAmount.ToString() + "\n(INVENTORY)";
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
                    for(int i=0; i < area.levels[area.currentLevel].areaSlots.Length; ++i)
                    {
                        if(area.levels[area.currentLevel].areaSlots[i].item != null)
                        {
                            totalAmount += area.levels[area.currentLevel].areaSlots[i].item.amount;
                        }
                    }
                    // Note that here we only check greater than 0
                    // Slot resource requirements are really only used for continuous productions
                    // These requirements specify an amount to COMPLETE the production
                    // but the production itself should be in production if we have more than 0 amount
                    fulfilled = totalAmount > 0;
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
                    for(int i=0; i < area.levels[area.currentLevel].areaSlots.Length; ++i)
                    {
                        if(area.levels[area.currentLevel].areaSlots[i].item != null)
                        {
                            ++itemCount;
                        }
                    }

                    fulfilled = itemCount > this.itemCount;
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

        public void OnAreaLevelChanged(Area area)
        {
            switch (requirementType)
            {
                case RequirementType.Area:
                    fulfilled = area.controller.areas[areaIndex].currentLevel >= areaLevel;
                    if (areaRequirementUI != null)
                    {
                        areaRequirementUI.fulfilled.SetActive(fulfilled);
                        areaRequirementUI.unfulfilled.SetActive(!fulfilled);
                        areaRequirementUI.areaName.color = fulfilled ? Color.green : Color.red;
                        areaRequirementUI.requiredLevel.color = fulfilled ? Color.green : Color.red;
                    }
                    break;
            }

            UpdateAreaUI();
        }

        public void OnTraderLevelChanged(Trader trader)
        {
            switch (requirementType)
            {
                case RequirementType.Trader:
                    fulfilled = (trader == null ? this.trader : trader).level >= traderLevel;
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

        public void OnTaskStateChanged(Task task)
        {
            switch (requirementType)
            {
                case RequirementType.QuestComplete:
                    fulfilled = (task == null ? this.task : task).taskState == Task.TaskState.Complete;
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
                    else if (!production.inProduction && (production.limit == 0 || production.limit > production.readyCount))
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

    /// <summary>
    /// Bonus functionality:
    /// Class keeps static amounts for each bonus type
    /// When bonuses get un/applied, their value is removed/added from/to the coresponding variable
    /// This way, this class keeps the raw bonus amount per type
    /// 
    /// Some values are being updated per frame, like energy regen
    /// We don't want to have to calculate the base+bonus value every frame
    /// For those types of values we keep a "current" var in Mod, which gets calculated only once on bonus un/apply
    /// 
    /// So some game functionality will use values here directly, like ragfair commision bonus
    /// While others will have their dedicated value in Mod
    /// </summary>
    public class Bonus
    {
        public Area area;
        public BonusUI bonusUI;

        public static int energyRegeneration;
        public static int debuffEndDelay; // TODO: // Implement with effects
        public static int repairArmorBonus; // TODO: // Implement with armor repair
        public static int hydrationRegeneration;
        public static int maximumEnergyReserve;
        public static int healthRegeneration;
        public static int scavCooldownTimer;
        public static int questMoneyReward;
        public static int insuranceReturnTime; // TODO: // Implement with insurance
        public static int ragfairCommission;
        public static int experienceRate;
        public static Dictionary<Skill.SkillType, int> skillGroupLevelingBoost = new Dictionary<Skill.SkillType, int>(); // TODO: // Implement with skill progress
        public static int fuelConsumption;
        public static int repairWeaponBonus; // TODO: // Implement with weapon repair

        public enum BonusType
        {
            None,
            EnergyRegeneration, // Value %
            DebuffEndDelay, // Value %
            AdditionalSlots,
            UnlockArmorRepair,
            RepairArmorBonus, // Value %
            StashSize,
            HydrationRegeneration, // Value %
            HealthRegeneration, // Value %
            TextBonus,
            MaximumEnergyReserve, // Value +amount
            ScavCooldownTimer, // Value %
            QuestMoneyReward, // Value %
            InsuranceReturnTime, // Value %
            RagfairCommission, // Value %
            ExperienceRate, // Value %
            SkillGroupLevelingBoost, // Value %
            FuelConsumption, // Value %
            UnlockWeaponModification,
            UnlockWeaponRepair,
            RepairWeaponBonus, // Value %
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

        public void Apply()
        {
            switch (bonusType)
            {
                case BonusType.EnergyRegeneration:
                    energyRegeneration += value;
                    Mod.currentEnergyRate = Mod.baseEnergyRate + Mod.baseEnergyRate / 100 * energyRegeneration;
                    break;
                case BonusType.DebuffEndDelay:
                    debuffEndDelay += value;
                    break;
                case BonusType.RepairArmorBonus:
                    repairArmorBonus += value;
                    break;
                case BonusType.HydrationRegeneration:
                    hydrationRegeneration += value;
                    Mod.currentHydrationRate = Mod.baseHydrationRate + Mod.baseHydrationRate / 100 * hydrationRegeneration;
                    break;
                case BonusType.HealthRegeneration:
                    healthRegeneration += value;
                    for(int i=0; i < Mod.GetHealthCount(); ++i)
                    {
                        Mod.SetCurrentHealthRate(i, Mod.GetBasePositiveHealthRate(i) + (Mod.GetBasePositiveHealthRate(i) / 100 * healthRegeneration) + Mod.GetBaseNegativeHealthRate(i));
                    }
                    break;
                case BonusType.MaximumEnergyReserve:
                    maximumEnergyReserve += value;
                    Mod.baseMaxEnergy += value;
                    break;
                case BonusType.ScavCooldownTimer:
                    scavCooldownTimer += value;
                    break;
                case BonusType.QuestMoneyReward:
                    questMoneyReward += value;
                    break;
                case BonusType.InsuranceReturnTime:
                    insuranceReturnTime += value;
                    break;
                case BonusType.RagfairCommission:
                    ragfairCommission += value;
                    break;
                case BonusType.ExperienceRate:
                    experienceRate += value;
                    break;
                case BonusType.SkillGroupLevelingBoost:
                    if(skillGroupLevelingBoost.ContainsKey(skillType))
                    {
                        skillGroupLevelingBoost[skillType] += value;
                    }
                    else
                    {
                        skillGroupLevelingBoost.Add(skillType, value);
                    }
                    break;
                case BonusType.FuelConsumption:
                    fuelConsumption += value;
                    break;
                case BonusType.RepairWeaponBonus:
                    repairWeaponBonus += value;
                    break;
            }
        }

        public void Unapply()
        {
            switch (bonusType)
            {
                case BonusType.EnergyRegeneration:
                    energyRegeneration -= value;
                    Mod.currentEnergyRate = Mod.baseEnergyRate + Mod.baseEnergyRate / 100 * energyRegeneration;
                    break;
                case BonusType.DebuffEndDelay:
                    debuffEndDelay -= value;
                    break;
                case BonusType.RepairArmorBonus:
                    repairArmorBonus -= value;
                    break;
                case BonusType.HydrationRegeneration:
                    hydrationRegeneration -= value;
                    Mod.currentHydrationRate = Mod.baseHydrationRate + Mod.baseHydrationRate / 100 * hydrationRegeneration;
                    break;
                case BonusType.HealthRegeneration:
                    healthRegeneration -= value;
                    for (int i = 0; i < Mod.GetHealthCount(); ++i)
                    {
                        Mod.SetCurrentHealthRate(i, Mod.GetBasePositiveHealthRate(i) + (Mod.GetBasePositiveHealthRate(i) / 100 * healthRegeneration) + Mod.GetBaseNegativeHealthRate(i));
                    }
                    break;
                case BonusType.MaximumEnergyReserve:
                    maximumEnergyReserve -= value;
                    Mod.baseMaxEnergy -= value;
                    break;
                case BonusType.ScavCooldownTimer:
                    scavCooldownTimer -= value;
                    break;
                case BonusType.QuestMoneyReward:
                    questMoneyReward -= value;
                    break;
                case BonusType.InsuranceReturnTime:
                    insuranceReturnTime -= value;
                    break;
                case BonusType.RagfairCommission:
                    ragfairCommission -= value;
                    break;
                case BonusType.ExperienceRate:
                    experienceRate -= value;
                    break;
                case BonusType.SkillGroupLevelingBoost:
                    if (skillGroupLevelingBoost.ContainsKey(skillType))
                    {
                        skillGroupLevelingBoost[skillType] -= value;
                    }
                    else
                    {
                        Mod.LogError("Bonus skillGroupLevelingBoost did not contain "+skillType+" which we tried to unapply");
                    }
                    break;
                case BonusType.FuelConsumption:
                    fuelConsumption -= value;
                    break;
                case BonusType.RepairWeaponBonus:
                    repairWeaponBonus -= value;
                    break;
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
        public MeatovItemData endProduct;
        public Vector2Int[] endProductRarities; // 0: Common, 1: Rare, 2: Superrare
        public bool continuous;
        public int limit; // 0 means no limit
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
                if(!Mod.GetItemData(Mod.TarkovIDtoH3ID(data["endProduct"].ToString()), out endProduct))
                {
                    Mod.LogError("DEV: Production "+ID+" end product with ID "+ Mod.TarkovIDtoH3ID(data["endProduct"].ToString())+" is missing item data");
                }
                needFuelForAllProductionTime = (bool)data["needFuelForAllProductionTime"];
                continuous = (bool)data["continuous"];
                count = (int)data["count"];
                limit = (int)data["productionLimitCount"];
                time = (int)data["productionTime"];
                requirementsArray = data["requirements"] as JArray;

                if(endProduct != null)
                {
                    if (area.productionsByProductID.TryGetValue(endProduct.H3ID, out List<Production> productionList))
                    {
                        productionList.Add(this);
                    }
                    else
                    {
                        area.productionsByProductID.Add(endProduct.H3ID, new List<Production>() { this });
                    }
                }
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

                requirements.Add(newRequirement);
            }

            // Bitcoin farm special case
            // We want to make sure its production has a GPU resource requirement
            if (area.index == 20)
            {
                Requirement newRequirement = new Requirement(null, area, this);
                newRequirement.requirementType = Requirement.RequirementType.Item;
                newRequirement.item = Mod.customItemData[159];
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
            if (HideoutController.loadedData["hideout"]["areas"][area.index]["productions"][ID] == null)
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
                area.readyProdutionCount += readyCount;
            }
        }

        public void Save(JToken data)
        {
            data["inProduction"] = inProduction;
            data["progress"] = progress;
            data["readyCount"] = readyCount;
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
                    ++area.readyProdutionCount;
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

                    if (limit != 0 && readyCount == limit)
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
                        farmingUI.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(timeLeft) + ")...";
                        farmingUI.timePanel.percentage.text = ((int)progress).ToString() + "%";

                        if (!farmingUI.productionStatus.activeSelf)
                        {
                            farmingUI.productionStatus.SetActive(true);
                        }
                        if (!farmingUI.timePanel.percentage.gameObject.activeSelf)
                        {
                            farmingUI.timePanel.percentage.gameObject.SetActive(true);
                        }

                        UpdateContinuousResourceConsumption();
                    }
                    else
                    {
                        productionUI.productionStatusText.text = "Producing\n(" + Mod.FormatTimeString(timeLeft) + ")...";
                        productionUI.timePanel.percentage.text = ((int)progress).ToString() + "%";

                        if (!productionUI.productionStatus.activeSelf)
                        {
                            productionUI.productionStatus.SetActive(true);
                        }
                        if (!productionUI.timePanel.percentage.gameObject.activeSelf)
                        {
                            productionUI.timePanel.percentage.gameObject.SetActive(true);
                        }
                    }
                }
            }

            previousInProduction = inProduction;
        }

        public void UpdateContinuousResourceConsumption()
        {
            for (int i = 0; i < requirements.Count; ++i) 
            {
                if (requirements[i].requirementType == Requirement.RequirementType.Resource)
                {
                    if(requirements[i].resourceConsumptionTime == -1)
                    {
                        requirements[i].resourceConsumptionTime = time / requirements[i].resourceCount;
                    }

                    requirements[i].resourceConsumptionTimer -= Time.deltaTime;
                    if(requirements[i].resourceConsumptionTimer <= 0)
                    {
                        for(int j=0; j < area.levels[area.currentLevel].areaSlots.Length; ++j)
                        {
                            if (area.levels[area.currentLevel].areaSlots[j].item != null
                                && area.levels[area.currentLevel].areaSlots[j].item.itemData == requirements[i].item
                                && area.levels[area.currentLevel].areaSlots[j].item.amount > 0)
                            {
                                --area.levels[area.currentLevel].areaSlots[j].item.amount;

                                requirements[i].resourceConsumptionTimer = requirements[i].resourceConsumptionTime;
                                break;
                            }
                        }
                    }
                }
            }
        }

        public void ReturnTools()
        {
            for(int i=0; i < requirements.Count; ++i)
            {
                if (requirements[i].requirementType == Requirement.RequirementType.Tool)
                {
                    // In case item is vanilla, in which case we use the vault system to save it,
                    // we will only be getting the instantiated item later
                    // We must write a delegate in order to add it to the area volume later
                    VaultSystem.ReturnObjectListDelegate del = objs =>
                    {
                        // Here, assume objs[0] is the root item
                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                        if (meatovItem != null)
                        {
                            // Note that despite this being a production, output could be a slot so we might not have a volume
                            if (area.levels[area.currentLevel].areaVolumes != null && area.levels[area.currentLevel].areaVolumes.Length > 0)
                            {
                                area.levels[area.currentLevel].areaVolumes[0].AddItem(meatovItem);
                            }
                            else // No output volume, spawn item in trade volume
                            {
                                HideoutController.instance.marketManager.tradeVolume.AddItem(meatovItem);
                            }
                        }
                    };

                    // In case item is custom, it will be returned right away and we can handle it here
                    MeatovItem loadedItem = MeatovItem.Deserialize(requirements[i].serializedTool, del);

                    if (loadedItem != null)
                    {
                        if (area.levels[area.currentLevel].areaVolumes != null && area.levels[area.currentLevel].areaVolumes.Length > 0)
                        {
                            area.levels[area.currentLevel].areaVolumes[0].AddItem(loadedItem);
                        }
                        else // No output volume, spawn item in trade volume
                        {
                            HideoutController.instance.marketManager.tradeVolume.AddItem(loadedItem);
                        }
                    }
                }
            }
        }

        public void SpawnProduct()
        {
            if (scavCase)
            {
                if(Mod.itemsByRarity.TryGetValue(MeatovItem.ItemRarity.Common, out List<MeatovItemData> commonList))
                {
                    int commonCount = UnityEngine.Random.Range(endProductRarities[0].x, endProductRarities[0].y + 1);
                    for (int i = 0; i < commonCount; ++i)
                    {
                        MeatovItemData randomCommon = commonList[UnityEngine.Random.Range(0, commonList.Count)];
                        area.levels[area.currentLevel].areaVolumes[0].SpawnItem(randomCommon, 1, true);
                    }
                }
                if(Mod.itemsByRarity.TryGetValue(MeatovItem.ItemRarity.Rare, out List<MeatovItemData> rareList))
                {
                    int rareCount = UnityEngine.Random.Range(endProductRarities[1].x, endProductRarities[1].y + 1);
                    for (int i = 0; i < rareCount; ++i)
                    {
                        MeatovItemData randomRare = rareList[UnityEngine.Random.Range(0, rareList.Count)];
                        area.levels[area.currentLevel].areaVolumes[0].SpawnItem(randomRare, 1, true);
                    }
                }
                if(Mod.itemsByRarity.TryGetValue(MeatovItem.ItemRarity.Rare, out List<MeatovItemData> superRareList))
                {
                    int superRareCount = UnityEngine.Random.Range(endProductRarities[2].x, endProductRarities[2].y + 1);
                    for (int i = 0; i < superRareCount; ++i)
                    {
                        MeatovItemData randomSuperRare = superRareList[UnityEngine.Random.Range(0, superRareList.Count)];
                        area.levels[area.currentLevel].areaVolumes[0].SpawnItem(randomSuperRare, 1, true);
                    }
                }
            }
            else
            {
                if (area.craftOuputSlot)
                {
                    area.levels[area.currentLevel].areaSlots[0].SpawnItem(endProduct, count, true);
                }
                else
                {
                    area.levels[area.currentLevel].areaVolumes[0].SpawnItem(endProduct, count, true);
                }
            }

            --area.readyProdutionCount;
            --readyCount;
        }

        public void BeginProduction()
        {
            // Set into upgrade state
            inProduction = true;
            timeLeft = time;

            // Consume requirements
            for (int i = 0; i < requirements.Count; ++i)
            {
                Requirement itemRequirement = requirements[i];
                if(itemRequirement.requirementType == Requirement.RequirementType.Resource)
                {
                    int countLeft = itemRequirement.resourceCount;
                    while (countLeft > 0)
                    {
                        MeatovItem item = area.GetClosestItem(itemRequirement.item.H3ID);
                        if (item.amount > countLeft)
                        {
                            item.amount -= countLeft;
                            countLeft = 0;
                            break;
                        }
                        else
                        {
                            countLeft -= item.amount;
                            item.DetachChildren();
                            item.Destroy();
                        }
                    }
                }
                else if (itemRequirement.requirementType == Requirement.RequirementType.Item
                         || itemRequirement.requirementType == Requirement.RequirementType.Tool)
                {
                    int countLeft = itemRequirement.itemCount;
                    while (countLeft > 0)
                    {
                        MeatovItem item = area.GetClosestItem(itemRequirement.item.H3ID);
                        if (item.stack > countLeft)
                        {
                            item.stack -= countLeft;
                            countLeft = 0;
                            break;
                        }
                        else
                        {
                            countLeft -= item.stack;
                            item.DetachChildren();
                            if(itemRequirement.requirementType == Requirement.RequirementType.Tool)
                            {
                                itemRequirement.serializedTool = item.Serialize();
                            }
                            item.Destroy();
                        }
                    }
                }
            }
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
            for (int i = 0; i < area.levels[area.currentLevel].areaSlots.Length; ++i)
            {
                if (area.levels[area.currentLevel].areaSlots[i].item != null)
                {
                    ++GPUCount;
                }
            }
            timeLeft = time / (1 + (GPUCount - 1) * Area.GPUBoostRate);
            progressBaseTime = timeLeft;
            timeLeft = timeLeft - timeLeft * (progress / 100);
            timeLeftSet = true;
            inProduction = GPUCount > 0;

            // Check because we call this before farming view is set just to set data above
            if(farmingUI != null)
            {
                farmingUI.timePanel.requiredTime.text = Mod.FormatTimeString(progressBaseTime);
            }
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
}
