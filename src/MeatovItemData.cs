﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    // This should contain:
    // - everything we will need to instantiate an item (Really just an item ID)
    // - everything a description would need (Because we might not have a physical item as a describable)
    // - all static tarkov data for a vanilla item (Not live data like if it found in raid or something like that)
    public class MeatovItemData
    {
        // Identifying data
        public string tarkovID; // Original tarkov ID
        public string H3ID; // ID of the item in IM.OD or the index in string form in case of a custom EFM item
        public string H3SpawnerID; // ID of the item in IM.SpawnerIDDic (Vanilla/Non-EFM Modded item only)
        public int index = -1; // Custom EFM item index
        public int defaultItemDataIndex; // Live, index of this data in the list of Mod.defaultItemDataByH3ID
        public int vanillaItemDataIndex; // Live, index of this data in the list of Mod.vanillaItemDataByH3ID

        public MeatovItem.ItemType itemType;
        public MeatovItem.ItemRarity rarity;
        public string[] parents;
        public int weight;
        public int[] volumes;
        public int lootExperience;
        public string name;
        public string description;
        public bool canSellOnRagfair;
        public int value; // Roubles
        // Weapon/Ammo container
        public int compatibilityValue;  // 0: Does not need mag or round, 1: Needs mag, 2: Needs round, 3: Needs both, used by description to display list of compatibilities present in hideout
        public bool usesMags; // Could be clip
        public bool usesAmmoContainers; // Could be internal mag or revolver
        public FireArmMagazineType magType;
        public FireArmClipType clipType;
        public FireArmRoundType roundType; // Caliber
        public MeatovItem.WeaponClass weaponclass;
        public int recoilVertical;
        public int recoilHorizontal;
        public int sightingRange; // Max
        // Mod
        public string modGroup;
        public string modPart;
        public int modDictIndex; // Live, index in list of Mod.modItemsByPartByGroup
        public int ergonomicsModifier; // Addition
        public int recoilModifier; // Percentage
        public Vector3 dimensions;
        public Color color;
        // Head equipment
        public bool blocksEarpiece;
        public bool blocksEyewear;
        public bool blocksFaceCover;
        public bool blocksHeadwear;
        // Armor
        public float coverage;
        public float damageResist;
        public int maxArmor;
        // Rigs
        public int smallSlotCount;
        public int mediumSlotCount;
        // Containers
        public int maxVolume;
        public List<string> whiteList;
        public List<string> blackList;
        // Ammo box
        public string cartridge; // The item ID
        public FireArmRoundClass roundClass;
        // Stack
        public int maxStack;
        // Amount
        public int maxAmount;
        // Effects
        public float useTime = 0; // Amount of time it takes to use amountRate
        public int amountRate = -1; // Maximum amount that can be used from the consumable in single use ex.: grizzly can only be used to heal up to 175 hp per use. 0 means a single unit of multiple, -1 means no limit up to maxAmount
        public List<BuffEffect> effects; // Ongoing effects after consumption
        public List<ConsumableEffect> consumeEffects; // Built from item effects_damage and effects_health, these are emmediate effects upon consuming the item
        // Dogtag
        public int dogtagLevel = 1;
        public string dogtagName;

        // Checkmark data
        public bool[] neededFor; // Task, Area upgrade, wishlist, barters, production
        private bool _onWishlist;
        public bool onWishlist
        {
            get { return _onWishlist; }
            set
            {
                bool preValue = _onWishlist;
                _onWishlist = value;
                if(neededFor != null)
                {
                    neededFor[2] = _onWishlist;
                }
                if(preValue != _onWishlist)
                {
                    if (preValue) // Was on wishlist, must remove
                    {
                        Mod.wishList.Remove(this);
                    }
                    else // Was not on wishlist, must add
                    {
                        Mod.wishList.Add(this);
                    }
                    OnNeededForChangedInvoke(2);
                    if (_onWishlist)
                    {
                        OnAddedToWishlistInvoke(this);
                    }
                }
            }
        }
        public Dictionary<int, Dictionary<int, int>> neededForLevelByArea; // Levels(key) and count(value) in which Areas this item is needed
        public Dictionary<int, Dictionary<int, int>> neededForLevelByAreaCurrent; // Levels in which Areas this item is CURRENTLY needed  (Depends on Mod.checkmarkFutureAreas)
        // If Mod.checkmarkAreaFulfillledMinimum
        // Then we will want to display fulfilled area upgrade checkmark (Instead of needed), if 
        // we have enough of this item for AT LEAST one upgrade
        // Otherwise, only if we have enough for all upgrades requiring this item
        // For this, we need to keep track of the area uprade item requirement
        // requiring the least amount of this item
        private int _minimumUpgradeAmount = int.MaxValue;
        public int minimumUpgradeAmount
        {
            set
            {
                int preValue = _minimumUpgradeAmount;
                _minimumUpgradeAmount = value;
                if (preValue != _minimumUpgradeAmount)
                {
                    OnMinimumUpgradeAmountChangedInvoke();
                }
            }
            get { return _minimumUpgradeAmount; }
        }
        public Dictionary<int, Dictionary<int, Dictionary<Production, int>>> neededForProductionByLevelByArea; // Productions for which Level in which Areas this item is needed
        public Dictionary<int, Dictionary<int, Dictionary<Production, int>>> neededForProductionByLevelByAreaCurrent; // Productions for which Level in which Areas this item is CURRENTLY needed (Depends on Mod.checkmarkFutureProductions)
        public Dictionary<int, Dictionary<int, Dictionary<Barter, int>>> neededForBarterByLevelByTrader; // Barters for which Level for which Trader this item is needed
        public Dictionary<int, Dictionary<int, Dictionary<Barter, int>>> neededForBarterByLevelByTraderCurrent; // Barters for which Level for which Trader this item is CURRENTLY needed (Depends on Mod.checkmarkFutureBarters)
        public Dictionary<Task, KeyValuePair<int, bool>> neededForTasks; // Tasks for which this item is needed, amount (Key) and FIR only (value)
        public Dictionary<Task, KeyValuePair<int, bool>> neededForTasksCurrent; // Tasks for which this item is CURRENTLY needed (Depends on Mod.checkmarkFutureQuests)
        private int _neededForAreaTotal;
        public int neededForAreaTotal
        {
            set 
            {
                int preValue = _neededForAreaTotal;
                _neededForAreaTotal = value;
                if(preValue != _neededForAreaTotal)
                {
                    OnNeededForAreaTotalChangedInvoke();
                }
            }
            get { return _neededForAreaTotal; }
        }
        private int _neededForTaskTotal;
        public int neededForTaskTotal
        {
            set 
            {
                int preValue = _neededForTaskTotal;
                _neededForTaskTotal = value;
                if(preValue != _neededForTaskTotal)
                {
                    OnNeededForTaskTotalChangedInvoke();
                }
            }
            get { return _neededForTaskTotal; }
        }
        public RagFairWishlistItemView ragFairWishlistItemView;

        // Sosig equivalent
        public List<FVRObject> sosigItems;
        public List<FVRObject> sosigEarpiece;
        public List<FVRObject> sosigHeadwear;
        public List<FVRObject> sosigFacewear;
        public List<FVRObject> sosigTorso;
        public List<FVRObject> sosigBackpack;
        public bool sosigAbdoMatchTorso; // Item in abdo should only be used with the item in torso with the same index
        public Dictionary<FVRObject, bool> sosigAbdo; // False value means no leg
        public bool sosigLegMatchAbdo; // Item in leg should only be used with the item in abdo with the same index
        public List<FVRObject> sosigLeg;

        // Events
        public delegate void OnItemFoundDelegate();
        public event OnItemFoundDelegate OnItemFound;
        public delegate void OnItemLeftDelegate(string locationID);
        public event OnItemLeftDelegate OnItemLeft;
        public delegate void OnItemUsedDelegate();
        public event OnItemUsedDelegate OnItemUsed;
        public delegate void OnNeededForChangedDelegate(int index);
        public event OnNeededForChangedDelegate OnNeededForChanged;
        public delegate void OnAddedToWishlistDelegate(MeatovItemData itemData);
        public static event OnAddedToWishlistDelegate OnAddedToWishlist;
        public delegate void OnMinimumUpgradeAmountChangedDelegate();
        public event OnMinimumUpgradeAmountChangedDelegate OnMinimumUpgradeAmountChanged;
        public delegate void OnNeededForAreaTotalChangedDelegate();
        public event OnNeededForAreaTotalChangedDelegate OnNeededForAreaTotalChanged;
        public delegate void OnNeededForTaskTotalChangedDelegate();
        public event OnNeededForTaskTotalChangedDelegate OnNeededForTaskTotalChanged;
        public delegate void OnHideoutItemInventoryChangedDelegate(int difference);
        public event OnHideoutItemInventoryChangedDelegate OnHideoutItemInventoryChanged;
        public delegate void OnPlayerItemInventoryChangedDelegate(int difference);
        public event OnPlayerItemInventoryChangedDelegate OnPlayerItemInventoryChanged;

        public MeatovItemData(JToken data)
        {
            if(data["tarkovID"] == null)
            {
                H3ID = null;
                return;
            }

            tarkovID = data["tarkovID"].ToString();
            H3ID = Regex.Unescape(data["H3ID"].ToString());
            H3SpawnerID = data["H3SpawnerID"] == null ? null : data["H3SpawnerID"].ToString();
            index = data["index"] == null ? -1 : (int)data["index"];

            itemType = (MeatovItem.ItemType)Enum.Parse(typeof(MeatovItem.ItemType), data["itemType"].ToString());
            rarity = (MeatovItem.ItemRarity)Enum.Parse(typeof(MeatovItem.ItemRarity), data["rarity"].ToString());
            if (Mod.itemsByRarity.TryGetValue(rarity, out List<MeatovItemData> rarityList))
            {
                rarityList.Add(this);
            }
            else
            {
                Mod.itemsByRarity.Add(rarity, new List<MeatovItemData>() { this });
            }
            parents = data["parents"].ToObject<string[]>();
            // Note that a particular item will appear in the list corresponding to all of its ancestors, not only its direct parent
            for (int i=0; i < parents.Length; ++i)
            {
                if (Mod.itemsByParents.TryGetValue(parents[i], out List<MeatovItemData> parentList))
                {
                    parentList.Add(this);
                }
                else
                {
                    Mod.itemsByParents.Add(parents[i], new List<MeatovItemData>() { this });
                }
            }
            weight = (int)data["weight"];
            volumes = data["volumes"].ToObject<int[]>();
            lootExperience = (int)data["lootExperience"];
            name = Regex.Unescape(data["name"].ToString());
            description = Regex.Unescape(data["description"].ToString());
            canSellOnRagfair = (bool)data["canSellOnRagfair"];
            value = (int)data["value"];

            compatibilityValue = (int)data["compatibilityValue"];
            usesMags = (bool)data["usesMags"];
            usesAmmoContainers = (bool)data["usesAmmoContainers"];
            magType = (FireArmMagazineType)Enum.Parse(typeof(FireArmMagazineType), data["magType"].ToString());
            clipType = (FireArmClipType)Enum.Parse(typeof(FireArmClipType), data["clipType"].ToString());
            roundType = (FireArmRoundType)Enum.Parse(typeof(FireArmRoundType), data["roundType"].ToString());

            weaponclass = (MeatovItem.WeaponClass)Enum.Parse(typeof(MeatovItem.WeaponClass), data["weaponclass"].ToString());
            if(data["recoilVertical"] != null)
            {
                recoilVertical = (int)data["recoilVertical"];
            }
            if (data["ergonomicsModifier"] != null)
            {
                ergonomicsModifier = (int)data["ergonomicsModifier"];
            }
            if (data["sightingRange"] != null)
            {
                sightingRange = (int)data["sightingRange"];
            }
            if (data["recoilHorizontal"] != null)
            {
                recoilHorizontal = (int)data["recoilHorizontal"];
            }
            if (data["recoilModifier"] != null)
            {
                recoilModifier = (int)data["recoilModifier"];
            }
            if (data["dimensions"] != null)
            {
                dimensions = new Vector3(Mathf.Max(0.15f, (float)data["dimensions"][0]), Mathf.Max(0.05f, (float)data["dimensions"][1]), Mathf.Max(0.15f, (float)data["dimensions"][2]));
            }
            else
            {
                dimensions = new Vector3(0.1f, 0.05f, 0.1f);
            }
            if (data["color"] != null)
            {
                color = new Color((float)data["color"][0], (float)data["color"][1], (float)data["color"][2], 1);
            }
            else
            {
                color = new Color(0.12f, 0.12f, 0.12f, 1);
            }


            blocksEarpiece = (bool)data["blocksEarpiece"];
            blocksEyewear = (bool)data["blocksEyewear"];
            blocksFaceCover = (bool)data["blocksFaceCover"];
            blocksHeadwear = (bool)data["blocksHeadwear"];

            coverage = (float)data["coverage"];
            damageResist = (float)data["damageResist"];
            maxArmor = (int)data["maxArmor"];

            smallSlotCount = (int)data["smallSlotCount"];
            mediumSlotCount = (int)data["mediumSlotCount"];

            maxVolume = (int)data["maxVolume"];
            if (data["whiteList"] != null)
            {
                whiteList = data["whiteList"].ToObject<List<string>>();
            }
            if (data["blackList"] != null)
            {
                blackList = data["blackList"].ToObject<List<string>>();
            }

            cartridge = data["cartridge"].ToString();
            roundClass = (FireArmRoundClass)Enum.Parse(typeof(FireArmRoundClass), data["roundClass"].ToString());

            maxStack = (int)data["maxStack"];

            maxAmount = (int)data["maxAmount"];

            useTime = (float)data["useTime"];
            amountRate = (int)data["amountRate"];

            JArray consumeEffectData = data["consumeEffects"] as JArray;
            consumeEffects = new List<ConsumableEffect>();
            for (int i=0;i< consumeEffectData.Count;++i)
            {
                ConsumableEffect consumableEffect = new ConsumableEffect();
                consumeEffects.Add(consumableEffect);
                consumableEffect.value = (float)consumeEffectData[i]["value"];
                consumableEffect.delay = (float)consumeEffectData[i]["delay"];
                consumableEffect.duration = (float)consumeEffectData[i]["duration"];
                consumableEffect.cost = (int)consumeEffectData[i]["cost"];
                switch (consumeEffectData[i]["effectType"].ToString())
                {
                    case "RadExposure":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.RadExposure;
                        break;
                    case "Pain":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Pain;
                        if (consumeEffectData[i]["fadeOut"] != null)
                        {
                            consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        }
                        break;
                    case "Contusion":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Contusion;
                        consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        break;
                    case "Intoxication":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Intoxication;
                        consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        break;
                    case "LightBleeding":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.LightBleeding;
                        consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        break;
                    case "Fracture":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Fracture;
                        consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        break;
                    case "DestroyedPart":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.DestroyedPart;
                        consumableEffect.healthPenaltyMax = (float)consumeEffectData[i]["healthPenaltyMax"] / 100;
                        consumableEffect.healthPenaltyMin = (float)consumeEffectData[i]["healthPenaltyMin"] / 100;
                        break;
                    case "HeavyBleeding":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.HeavyBleeding;
                        consumableEffect.fadeOut = (float)consumeEffectData[i]["fadeOut"];
                        break;
                    case "Hydration":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Hydration;
                        break;
                    case "Energy":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Energy;
                        break;
                }
            }

            JArray buffEffectData = data["buffEffects"] as JArray;
            effects = new List<BuffEffect>();
            for (int i=0; i< buffEffectData.Count;++i)
            {
                BuffEffect buffEffect = new BuffEffect();
                effects.Add(buffEffect);
                buffEffect.effectType = (Effect.EffectType)Enum.Parse(typeof(Effect.EffectType), buffEffectData[i]["effectType"].ToString());
                buffEffect.value = (float)buffEffectData[i]["value"];
                buffEffect.chance = (float)buffEffectData[i]["chance"];
                buffEffect.delay = (float)buffEffectData[i]["delay"];
                buffEffect.duration = (float)buffEffectData[i]["duration"];
                buffEffect.absolute = (bool)buffEffectData[i]["absolute"];
                buffEffect.skillIndex = (int)buffEffectData[i]["skillIndex"];
            }

            if (data["modulGroup"] != null && data["modulGroup"].Type != JTokenType.Null)
            {
                modGroup = data["modulGroup"].ToString();
                modPart = data["modulPart"].ToString();
            }

            if (data["sosigItems"] != null)
            {
                sosigItems = new List<FVRObject>();
                JArray sosigItemData = data["sosigItems"] as JArray;
                for(int i=0; i < sosigItemData.Count; ++i)
                {
                    if(IM.OD.TryGetValue(sosigItemData[i].ToString(), out FVRObject sosigItem))
                    {
                        sosigItems.Add(sosigItem);
                    }
                    else
                    {
                        Mod.LogError("Could not find sosig item: " + sosigItemData[i].ToString()+" for item "+tarkovID);
                    }
                }
            }
            if (data["sosigWearables"] != null)
            {
                if (data["sosigWearables"]["earpiece"] != null)
                {
                    sosigEarpiece = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["earpiece"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigEarpiece.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig earpiece: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["headwear"] != null)
                {
                    sosigHeadwear = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["headwear"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigHeadwear.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig headwear: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["facewear"] != null)
                {
                    sosigFacewear = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["facewear"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigFacewear.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig facewear: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["torso"] != null)
                {
                    sosigTorso = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["torso"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigTorso.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig torso: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["backpack"] != null)
                {
                    sosigBackpack = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["backpack"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigBackpack.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig backpack: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["abdo"] != null)
                {
                    sosigAbdoMatchTorso = (bool)data["sosigWearables"]["abdoMatchTorso"];
                    sosigAbdo = new Dictionary<FVRObject, bool>();
                    Dictionary<string, bool> sosigWearables = data["sosigWearables"]["abdo"].ToObject<Dictionary<string, bool>>();
                    foreach (KeyValuePair<string, bool> abdoEntry in sosigWearables)
                    {
                        if (IM.OD.TryGetValue(abdoEntry.Key, out FVRObject sosigWearable))
                        {
                            sosigAbdo.Add(sosigWearable, abdoEntry.Value);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig abdo: " + abdoEntry.Key + " for item " + tarkovID);
                        }
                    }
                }
                if (data["sosigWearables"]["leg"] != null)
                {
                    sosigLegMatchAbdo = (bool)data["sosigWearables"]["legMatchAbdo"];
                    sosigLeg = new List<FVRObject>();
                    JArray sosigWearables = data["sosigWearables"]["leg"] as JArray;
                    for (int i = 0; i < sosigWearables.Count; ++i)
                    {
                        if (IM.OD.TryGetValue(sosigWearables[i].ToString(), out FVRObject sosigWearable))
                        {
                            sosigLeg.Add(sosigWearable);
                        }
                        else
                        {
                            Mod.LogError("Could not find sosig leg: " + sosigWearables[i].ToString() + " for item " + tarkovID);
                        }
                    }
                }
            }
        }

        public void InitCheckmarkData()
        {
            if(tarkovID == null)
            {
                return;
            }

            if (HideoutController.instance == null)
            {
                Mod.LogError("MeatovItemData.UpdateCheckmarkData called but missing hideout instance!");
                return;
            }

            bool currentOnly = false;
            if (neededForLevelByArea == null)
            {
                neededFor = new bool[5];
                neededForLevelByArea = new Dictionary<int, Dictionary<int, int>>();
                neededForLevelByAreaCurrent = new Dictionary<int, Dictionary<int, int>>();
                neededForProductionByLevelByArea = new Dictionary<int, Dictionary<int, Dictionary<Production, int>>>();
                neededForProductionByLevelByAreaCurrent = new Dictionary<int, Dictionary<int, Dictionary<Production, int>>>();
                neededForBarterByLevelByTrader = new Dictionary<int, Dictionary<int, Dictionary<Barter, int>>>();
                neededForBarterByLevelByTraderCurrent = new Dictionary<int, Dictionary<int, Dictionary<Barter, int>>>();
                neededForTasks = new Dictionary<Task, KeyValuePair<int, bool>>();
                neededForTasksCurrent = new Dictionary<Task, KeyValuePair<int, bool>>();
            }
            else
            {
                // Must only update current neededFor for newly loaded data
                neededFor = new bool[5];
                neededForLevelByAreaCurrent.Clear();
                neededForProductionByLevelByAreaCurrent.Clear();
                neededForBarterByLevelByTraderCurrent.Clear();
                neededForTasksCurrent.Clear();
                neededForAreaTotal = 0;
                neededForTaskTotal = 0;
                currentOnly = true;
            }

            onWishlist = Mod.wishList.Contains(this);
            neededFor[2] = onWishlist;

            // Get Area specific data (Upgrades, Productions)
            for (int i = 0; i < HideoutController.instance.areaController.areas.Length; ++i)
            {
                Area area = HideoutController.instance.areaController.areas[i];
                if (area != null)
                {
                    bool subscribed = false;

                    // Area upgrades
                    for (int j = 0; j < area.areaData.requirementsByTypePerLevel.Length; ++j)
                    {
                        Dictionary<Requirement.RequirementType, List<Requirement>> requirementsByType = area.areaData.requirementsByTypePerLevel[j];
                        if (requirementsByType.ContainsKey(Requirement.RequirementType.Item) && requirementsByType[Requirement.RequirementType.Item] != null)
                        {
                            List<Requirement> itemRequirements = area.areaData.requirementsByTypePerLevel[j][Requirement.RequirementType.Item];
                            for (int k = 0; k < itemRequirements.Count; ++k)
                            {
                                Requirement requirement = itemRequirements[k];
                                if (requirement.item == this)
                                {
                                    int newCount = requirement.itemCount;
                                    if (!currentOnly)
                                    {
                                        if (neededForLevelByArea.TryGetValue(i, out Dictionary<int, int> levels))
                                        {
                                            int currentCount = 0;
                                            if (levels.TryGetValue(j, out currentCount))
                                            {
                                                levels[j] = currentCount + newCount;
                                            }
                                            else
                                            {
                                                levels.Add(j, newCount);
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<int, int> newLevelsDict = new Dictionary<int, int>();
                                            newLevelsDict.Add(j, newCount);
                                            neededForLevelByArea.Add(i, newLevelsDict);
                                        }

                                        if (!subscribed)
                                        {
                                            area.areaData.OnAreaLevelChanged += OnAreaLevelChanged;
                                            subscribed = true;
                                        }
                                    }

                                    // Set initial needed for state
                                    if (area.currentLevel < area.levels.Length - 1)
                                    {
                                        // Needed for area upgrade if this requirement's level is a future one and (we want future upgrades or this requirement's level is next)
                                        bool currentNeededFor = j > area.currentLevel && (Mod.checkmarkFutureAreas || j == (area.currentLevel + 1));
                                        neededFor[1] |= currentNeededFor;
                                        if (currentNeededFor)
                                        {
                                            if (neededForLevelByAreaCurrent.TryGetValue(i, out Dictionary<int, int> neededForLevels))
                                            {
                                                int currentCount = 0;
                                                if (neededForLevels.TryGetValue(j, out currentCount))
                                                {
                                                    neededForLevels[j] = currentCount + newCount;
                                                }
                                                else
                                                {
                                                    neededForLevels.Add(j, newCount);
                                                }
                                            }
                                            else
                                            {
                                                Dictionary<int, int> newLevelsDict = new Dictionary<int, int>();
                                                newLevelsDict.Add(j, newCount);
                                                neededForLevelByAreaCurrent.Add(i, newLevelsDict);

                                                if (newCount < minimumUpgradeAmount)
                                                {
                                                    minimumUpgradeAmount = newCount;
                                                }
                                            }
                                            neededForAreaTotal += newCount;
                                        }
                                    }
                                }
                            }
                        }
                        if (requirementsByType.ContainsKey(Requirement.RequirementType.Tool) && requirementsByType[Requirement.RequirementType.Tool] != null)
                        {
                            List<Requirement> itemRequirements = area.areaData.requirementsByTypePerLevel[j][Requirement.RequirementType.Tool];
                            for (int k = 0; k < itemRequirements.Count; ++k)
                            {
                                Requirement requirement = itemRequirements[k];
                                if (requirement.item == this)
                                {
                                    if (!currentOnly)
                                    {
                                        if (neededForLevelByArea.TryGetValue(i, out Dictionary<int, int> levels))
                                        {
                                            int currentCount = 0;
                                            if (levels.TryGetValue(j, out currentCount))
                                            {
                                                levels[j] = currentCount + 1;
                                            }
                                            else
                                            {
                                                levels.Add(j, 1);
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<int, int> newLevelsDict = new Dictionary<int, int>();
                                            newLevelsDict.Add(j, 1);
                                            neededForLevelByArea.Add(i, newLevelsDict);
                                        }

                                        if (!subscribed)
                                        {
                                            area.areaData.OnAreaLevelChanged += OnAreaLevelChanged;
                                            subscribed = true;
                                        }
                                    }

                                    // Set initial needed for state
                                    if (area.currentLevel < area.levels.Length - 1)
                                    {
                                        // Needed for area upgrade if this requirement's level is a future one and (we want future upgrades or this requirement's level is next)
                                        bool currentNeededFor = j > area.currentLevel && (Mod.checkmarkFutureAreas || j == (area.currentLevel + 1));
                                        neededFor[1] |= currentNeededFor;
                                        if (currentNeededFor)
                                        {
                                            if (neededForLevelByAreaCurrent.TryGetValue(i, out Dictionary<int, int> neededForLevels))
                                            {
                                                int currentCount = 0;
                                                if (neededForLevels.TryGetValue(j, out currentCount))
                                                {
                                                    neededForLevels[j] = currentCount + 1;
                                                }
                                                else
                                                {
                                                    neededForLevels.Add(j, 1);
                                                }
                                            }
                                            else
                                            {
                                                Dictionary<int, int> newLevelsDict = new Dictionary<int, int>();
                                                newLevelsDict.Add(j, 1);
                                                neededForLevelByAreaCurrent.Add(i, newLevelsDict);

                                                if (1 < minimumUpgradeAmount)
                                                {
                                                    minimumUpgradeAmount = 1;
                                                }
                                            }
                                            ++neededForAreaTotal;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Productions
                    for (int j = 0; j < area.areaData.productionsPerLevel.Count; ++j)
                    {
                        List<Production> productions = area.areaData.productionsPerLevel[j];
                        for (int k = 0; k < productions.Count; ++k)
                        {
                            Production production = productions[k];
                            for (int l = 0; l < production.requirements.Count; ++l)
                            {
                                Requirement requirement = production.requirements[l];
                                if (requirement.item == this)
                                {
                                    int newCount = requirement.itemCount;
                                    if (!currentOnly)
                                    {
                                        if (neededForProductionByLevelByArea.TryGetValue(i, out Dictionary<int, Dictionary<Production, int>> levels))
                                        {
                                            if (levels.TryGetValue(j, out Dictionary<Production, int> productionsDict))
                                            {
                                                int currentCount = 0;
                                                if (productionsDict.TryGetValue(production, out currentCount))
                                                {
                                                    productionsDict[production] = currentCount + newCount;
                                                }
                                                else
                                                {
                                                    productionsDict.Add(production, newCount);
                                                }
                                            }
                                            else
                                            {
                                                Dictionary<Production, int> newProductionDict = new Dictionary<Production, int>();
                                                newProductionDict.Add(production, newCount);
                                                levels.Add(j, newProductionDict);
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<int, Dictionary<Production, int>> newLevelsDict = new Dictionary<int, Dictionary<Production, int>>();
                                            Dictionary<Production, int> newProductionDict = new Dictionary<Production, int>();
                                            newProductionDict.Add(production, newCount);
                                            newLevelsDict.Add(j, newProductionDict);
                                            neededForProductionByLevelByArea.Add(i, newLevelsDict);
                                        }

                                        if (!subscribed)
                                        {
                                            area.areaData.OnAreaLevelChanged += OnAreaLevelChanged;
                                            subscribed = true;
                                        }
                                    }

                                    // Set initial needed for state
                                    if (area.currentLevel < area.levels.Length - 1)
                                    {
                                        // Needed for production if want future productions, or this requirement's level is a previous or current one
                                        bool currentNeededFor = Mod.checkmarkFutureProductions || j <= area.currentLevel;
                                        neededFor[4] |= currentNeededFor;
                                        if (currentNeededFor)
                                        {
                                            if (neededForProductionByLevelByAreaCurrent.TryGetValue(i, out Dictionary<int, Dictionary<Production, int>> currentLevels))
                                            {
                                                if (currentLevels.TryGetValue(j, out Dictionary<Production, int> productionsDict))
                                                {
                                                    int currentCount = 0;
                                                    if (productionsDict.TryGetValue(production, out currentCount))
                                                    {
                                                        productionsDict[production] = currentCount + newCount;
                                                    }
                                                    else
                                                    {
                                                        productionsDict.Add(production, newCount);
                                                    }
                                                }
                                                else
                                                {
                                                    Dictionary<Production, int> newProductionDict = new Dictionary<Production, int>();
                                                    newProductionDict.Add(production, newCount);
                                                    currentLevels.Add(j, newProductionDict);
                                                }
                                            }
                                            else
                                            {
                                                Dictionary<int, Dictionary<Production, int>> newLevelsDict = new Dictionary<int, Dictionary<Production, int>>();
                                                Dictionary<Production, int> newProductionDict = new Dictionary<Production, int>();
                                                newProductionDict.Add(production, newCount);
                                                newLevelsDict.Add(j, newProductionDict);
                                                neededForProductionByLevelByAreaCurrent.Add(i, newLevelsDict);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Barters
            for (int i=0; i < Mod.traders.Length; ++i)
            {
                Trader trader = Mod.traders[i];
                bool subscribed = false;
                foreach(KeyValuePair<int, List<Barter>> levelBarters in trader.bartersByLevel)
                {
                    List<Barter> barters = levelBarters.Value;
                    for (int k = 0; k < barters.Count; ++k)
                    {
                        Barter barter = barters[k];
                        for (int l = 0; l < barter.prices.Length; ++l)
                        {
                            BarterPrice price = barter.prices[l];
                            if (price.itemData == this)
                            {
                                int newCount = price.count;
                                if (!currentOnly)
                                {
                                    if (neededForBarterByLevelByTrader.TryGetValue(i, out Dictionary<int, Dictionary<Barter, int>> levels))
                                    {
                                        if (levels.TryGetValue(levelBarters.Key, out Dictionary<Barter, int> bartersDict))
                                        {
                                            int currentCount = 0;
                                            if (bartersDict.TryGetValue(barter, out currentCount))
                                            {
                                                bartersDict[barter] = currentCount + newCount;
                                            }
                                            else
                                            {
                                                bartersDict.Add(barter, newCount);
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                                            newBarterDict.Add(barter, newCount);
                                            levels.Add(levelBarters.Key, newBarterDict);
                                        }
                                    }
                                    else
                                    {
                                        Dictionary<int, Dictionary<Barter, int>> newLevelsDict = new Dictionary<int, Dictionary<Barter, int>>();
                                        Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                                        newBarterDict.Add(barter, newCount);
                                        newLevelsDict.Add(levelBarters.Key, newBarterDict);
                                        neededForBarterByLevelByTrader.Add(i, newLevelsDict);
                                    }

                                    if (!subscribed)
                                    {
                                        trader.OnTraderLevelChanged += OnTraderLevelChanged;
                                        subscribed = true;
                                    }
                                }

                                // Needed for barter if we want future barters (Implying we want all of them) or this barter's trader's level is <= trader's current level
                                bool currentNeededFor = Mod.checkmarkFutureBarters || levelBarters.Key <= trader.level;
                                neededFor[3] |= currentNeededFor;
                                if (currentNeededFor)
                                {
                                    if (neededForBarterByLevelByTraderCurrent.TryGetValue(i, out Dictionary<int, Dictionary<Barter, int>> currentLevels))
                                    {
                                        if (currentLevels.TryGetValue(levelBarters.Key, out Dictionary<Barter, int> bartersDict))
                                        {
                                            int currentCount = 0;
                                            if (bartersDict.TryGetValue(barter, out currentCount))
                                            {
                                                bartersDict[barter] = currentCount + newCount;
                                            }
                                            else
                                            {
                                                bartersDict.Add(barter, newCount);
                                            }
                                        }
                                        else
                                        {
                                            Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                                            newBarterDict.Add(barter, newCount);
                                            currentLevels.Add(levelBarters.Key, newBarterDict);
                                        }
                                    }
                                    else
                                    {
                                        Dictionary<int, Dictionary<Barter, int>> newLevelsDict = new Dictionary<int, Dictionary<Barter, int>>();
                                        Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                                        newBarterDict.Add(barter, newCount);
                                        newLevelsDict.Add(levelBarters.Key, newBarterDict);
                                        neededForBarterByLevelByTraderCurrent.Add(i, newLevelsDict);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Tasks
            foreach (KeyValuePair<string, Task> taskEntry in Task.allTasks)
            {
                bool subscribed = false;
                for (int i=0; i < taskEntry.Value.finishConditions.Count; ++i)
                {
                    Condition condition = taskEntry.Value.finishConditions[i];
                    if (condition.conditionType == Condition.ConditionType.HandoverItem
                        || condition.conditionType == Condition.ConditionType.LeaveItemAtLocation
                        || condition.conditionType == Condition.ConditionType.PlaceBeacon
                        || condition.conditionType == Condition.ConditionType.WeaponAssembly)
                    {
                        for (int j=0; j < condition.targetItems.Count; ++j)
                        {
                            if (condition.targetItems[j] == this)
                            {
                                KeyValuePair<int, bool> currentCount;
                                if (!currentOnly)
                                {
                                    if (neededForTasks.TryGetValue(taskEntry.Value, out currentCount))
                                    {
                                        neededForTasks[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                    }
                                    else
                                    {
                                        neededForTasks.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                    }

                                    if (!subscribed)
                                    {
                                        taskEntry.Value.OnTaskStateChanged += OnTaskStateChanged;
                                        subscribed = true;
                                    }
                                }

                                // Set initial needed for state
                                if (taskEntry.Value.taskState != Task.TaskState.Complete && taskEntry.Value.taskState != Task.TaskState.Fail)
                                {
                                    // Needed for quest if we want future quests (Active or not) or if currently active
                                    bool currentNeededFor = Mod.checkmarkFutureQuests || taskEntry.Value.taskState == Task.TaskState.Active;
                                    neededFor[0] |= currentNeededFor;
                                    if (currentNeededFor)
                                    {
                                        if (neededForTasksCurrent.TryGetValue(taskEntry.Value, out currentCount))
                                        {
                                            neededForTasksCurrent[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                        }
                                        else
                                        {
                                            neededForTasksCurrent.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                        }
                                        neededForTaskTotal += condition.value;
                                    }
                                }

                                break;
                            }
                        }
                    }
                    else if(condition.conditionType == Condition.ConditionType.CounterCreator)
                    {
                        for (int j = 0; j < condition.counters.Count; ++j)
                        {
                            if (condition.counters[j].counterCreatorConditionType == ConditionCounter.CounterCreatorConditionType.UseItem)
                            {
                                for (int k = 0; k< condition.counters[j].useItemTargets.Count; ++k)
                                {
                                    if (condition.counters[j].useItemTargets[k] == this)
                                    {
                                        KeyValuePair<int, bool> currentCount;
                                        if (!currentOnly)
                                        {
                                            if (neededForTasks.TryGetValue(taskEntry.Value, out currentCount))
                                            {
                                                neededForTasks[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                            }
                                            else
                                            {
                                                neededForTasks.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                            }

                                            if (!subscribed)
                                            {
                                                taskEntry.Value.OnTaskStateChanged += OnTaskStateChanged;
                                                subscribed = true;
                                            }
                                        }

                                        // Set initial needed for state
                                        if (taskEntry.Value.taskState != Task.TaskState.Complete && taskEntry.Value.taskState != Task.TaskState.Fail)
                                        {
                                            // Needed for quest if we want future quests (Active or not) or if currently active
                                            bool currentNeededFor = Mod.checkmarkFutureQuests || taskEntry.Value.taskState == Task.TaskState.Active;
                                            neededFor[0] |= currentNeededFor;
                                            if (currentNeededFor)
                                            {
                                                if (neededForTasksCurrent.TryGetValue(taskEntry.Value, out currentCount))
                                                {
                                                    neededForTasksCurrent[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                                }
                                                else
                                                {
                                                    neededForTasksCurrent.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                                }
                                                neededForTaskTotal += condition.value;
                                            }
                                        }

                                        break;
                                    }
                                }
                            }
                            else if(condition.counters[j].counterCreatorConditionType == ConditionCounter.CounterCreatorConditionType.Equipment)
                            {
                                if (condition.counters[j].equipmentWhitelists != null)
                                {
                                    for (int k = 0; k < condition.counters[j].equipmentWhitelists.Count; ++k)
                                    {
                                        if (condition.counters[j].equipmentWhitelists[k].Contains(tarkovID))
                                        {
                                            KeyValuePair<int, bool> currentCount;
                                            if (!currentOnly)
                                            {
                                                if (neededForTasks.TryGetValue(taskEntry.Value, out currentCount))
                                                {
                                                    neededForTasks[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + 1, currentCount.Value);
                                                }
                                                else
                                                {
                                                    neededForTasks.Add(taskEntry.Value, new KeyValuePair<int, bool>(1, currentCount.Value));
                                                }

                                                if (!subscribed)
                                                {
                                                    taskEntry.Value.OnTaskStateChanged += OnTaskStateChanged;
                                                    subscribed = true;
                                                }
                                            }

                                            // Set initial needed for state
                                            if (taskEntry.Value.taskState != Task.TaskState.Complete && taskEntry.Value.taskState != Task.TaskState.Fail)
                                            {
                                                // Needed for quest if we want future quests (Active or not) or if currently active
                                                bool currentNeededFor = Mod.checkmarkFutureQuests || taskEntry.Value.taskState == Task.TaskState.Active;
                                                neededFor[0] |= currentNeededFor;
                                                if (currentNeededFor)
                                                {
                                                    if (neededForTasksCurrent.TryGetValue(taskEntry.Value, out currentCount))
                                                    {
                                                        neededForTasksCurrent[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                                    }
                                                    else
                                                    {
                                                        neededForTasksCurrent.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                                    }
                                                    neededForTaskTotal += condition.value;
                                                }
                                            }

                                            break;
                                        }
                                        bool foundInWhitelists = false;
                                        for (int l = 0; l < parents.Length; ++l)
                                        {
                                            if (condition.counters[j].equipmentWhitelists[k].Contains(parents[l]))
                                            {
                                                KeyValuePair<int, bool> currentCount;
                                                if (!currentOnly)
                                                {
                                                    if (neededForTasks.TryGetValue(taskEntry.Value, out currentCount))
                                                    {
                                                        neededForTasks[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + 1, currentCount.Value);
                                                    }
                                                    else
                                                    {
                                                        neededForTasks.Add(taskEntry.Value, new KeyValuePair<int, bool>(1, currentCount.Value));
                                                    }

                                                    if (!subscribed)
                                                    {
                                                        taskEntry.Value.OnTaskStateChanged += OnTaskStateChanged;
                                                        subscribed = true;
                                                    }
                                                }

                                                // Set initial needed for state
                                                if (taskEntry.Value.taskState != Task.TaskState.Complete && taskEntry.Value.taskState != Task.TaskState.Fail)
                                                {
                                                    // Needed for quest if we want future quests (Active or not) or if currently active
                                                    bool currentNeededFor = Mod.checkmarkFutureQuests || taskEntry.Value.taskState == Task.TaskState.Active;
                                                    neededFor[0] |= currentNeededFor;
                                                    if (currentNeededFor)
                                                    {
                                                        if (neededForTasksCurrent.TryGetValue(taskEntry.Value, out currentCount))
                                                        {
                                                            neededForTasksCurrent[taskEntry.Value] = new KeyValuePair<int, bool>(currentCount.Key + condition.value, currentCount.Value);
                                                        }
                                                        else
                                                        {
                                                            neededForTasksCurrent.Add(taskEntry.Value, new KeyValuePair<int, bool>(condition.value, condition.onlyFoundInRaid));
                                                        }
                                                        neededForTaskTotal += condition.value;
                                                    }
                                                }

                                                foundInWhitelists = true;
                                                break;
                                            }
                                        }
                                        if (foundInWhitelists)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnTaskStateChanged(Task task)
        {
            bool preNeeded = neededFor[0];

            if(task.taskState == Task.TaskState.Complete || task.taskState == Task.TaskState.Fail)
            {
                KeyValuePair<int, bool> currentNeededCount;
                if (neededForTasksCurrent.TryGetValue(task, out currentNeededCount))
                {
                    neededForTasksCurrent.Remove(task);
                    neededForTaskTotal -= currentNeededCount.Key;

                    // This item was currently needed for this task, must update needed for state
                    neededFor[0] = neededForTasksCurrent.Count > 0;
                }
            }
            else if(task.taskState != Task.TaskState.Active)
            {
                neededFor[0] = Mod.checkmarkFutureQuests;
                if (!neededFor[0])
                {
                    KeyValuePair<int, bool> currentNeededCount;
                    if (neededForTasksCurrent.TryGetValue(task, out currentNeededCount))
                    {
                        neededForTasksCurrent.Remove(task);
                        neededForTaskTotal -= currentNeededCount.Key;
                    }
                }
            }
            else // Changed to Active
            {
                neededFor[0] = true;
                if (!neededForTasksCurrent.ContainsKey(task))
                {
                    neededForTasksCurrent.Add(task, neededForTasks[task]);
                    neededForTaskTotal += neededForTasks[task].Key;
                }
            }

            if(preNeeded != neededFor[0])
            {
                OnNeededForChangedInvoke(0);
            }
        }

        public void OnAreaLevelChanged(Area area)
        {
            Mod.LogInfo("OnAreaLevelChanged on item "+ tarkovID + ":" + H3ID);
            bool preNeededForArea = neededFor[1];
            bool preNeededForProduction = neededFor[4];

            // Remove up to current level from current needed for area level
            if (neededForLevelByAreaCurrent.TryGetValue(area.index, out Dictionary<int, int> currentAreaDict))
            {
                for (int i = 0; i <= area.currentLevel; ++i)
                {
                    if (currentAreaDict.TryGetValue(i, out int currentValue))
                    {
                        neededForAreaTotal -= currentValue;
                        currentAreaDict.Remove(i);

                        if(currentAreaDict.Count == 0)
                        {
                            neededForLevelByAreaCurrent.Remove(area.index);
                        }
                    }
                }
            }

            // Ensure that next level requirement(s) (or future if Mod.checkmarkFutureAreas) is/are currently needed
            if (neededForLevelByArea.TryGetValue(area.index, out Dictionary<int, int> neededForLevels))
            {
                for (int i = area.currentLevel + 1; i < area.levels.Length; ++i)
                {
                    if(!Mod.checkmarkFutureAreas && i >= area.currentLevel + 2)
                    {
                        break;
                    }

                    if (neededForLevels.TryGetValue(i, out int neededForValue))
                    {
                        if (neededForLevelByAreaCurrent.TryGetValue(area.index, out Dictionary<int, int> neededForLevelsCurrent))
                        {
                            if (!neededForLevelsCurrent.ContainsKey(i))
                            {
                                neededForLevelsCurrent.Add(i, neededForValue);
                                neededForAreaTotal += neededForValue;
                            }
                        }
                        else
                        {
                            neededForLevelByAreaCurrent.Add(area.index, new Dictionary<int, int>() { { i, neededForValue } });
                            neededForAreaTotal += neededForValue;
                        }
                    }
                }
            }

            // Ensure that production requirements up to current level (and future if Mod.checkmarkFutureProductions) are currently needed
            if(neededForProductionByLevelByArea.TryGetValue(area.index, out Dictionary<int, Dictionary<Production, int>> neededForLevelProductions))
            {
                for (int i = 0; i <= area.levels.Length; ++i)
                {
                    if (!Mod.checkmarkFutureProductions && i >= area.currentLevel + 1)
                    {
                        break;
                    }

                    if (neededForLevelProductions.TryGetValue(i, out Dictionary<Production, int> neededForProductions))
                    {
                        if (neededForProductionByLevelByAreaCurrent.TryGetValue(area.index, out Dictionary<int, Dictionary<Production, int>> neededForLevelsCurrent))
                        {
                            if (!neededForLevelsCurrent.ContainsKey(i))
                            {
                                neededForLevelsCurrent.Add(i, neededForProductions);
                            }
                        }
                        else
                        {
                            neededForProductionByLevelByAreaCurrent.Add(area.index, new Dictionary<int, Dictionary<Production, int>>() { { i, neededForProductions } });
                        }
                    }
                }
            }

            // Remove future level productions if we don't want them
            if (!Mod.checkmarkFutureProductions)
            {
                for (int i = area.currentLevel + 1; i < area.levels.Length; ++i)
                {
                    if (neededForProductionByLevelByAreaCurrent.TryGetValue(area.index, out Dictionary<int, Dictionary<Production, int>> neededForLevelsCurrent))
                    {
                        if (neededForLevelsCurrent.ContainsKey(i))
                        {
                            neededForLevelsCurrent.Remove(i);

                            if(neededForLevelsCurrent.Count == 0)
                            {
                                neededForProductionByLevelByAreaCurrent.Remove(area.index);
                            }
                        }
                    }
                }
            }

            // Update neededFor
            neededFor[1] = neededForLevelByAreaCurrent.Count > 0;
            neededFor[4] = neededForProductionByLevelByAreaCurrent.Count > 0;

            // If still needed, must find minimum again as it might have changed
            if (neededFor[1])
            {
                int minimum = int.MaxValue;
                bool found = false;
                foreach (KeyValuePair<int, Dictionary<int, int>> neededAreaEntry in neededForLevelByAreaCurrent)
                {
                    foreach (KeyValuePair<int, int> neededLevelEntry in neededAreaEntry.Value)
                    {
                        if (neededLevelEntry.Value < minimum)
                        {
                            minimum = neededLevelEntry.Value;
                            found = true;
                        }
                    }
                }
                if (found)
                {
                    minimumUpgradeAmount = minimum;
                }
                else
                {
                    minimumUpgradeAmount = 0;
                }
            }
            
            if (preNeededForArea != neededFor[1])
            {
                OnNeededForChangedInvoke(1);
            }
            if (preNeededForProduction != neededFor[4])
            {
                OnNeededForChangedInvoke(4);
            }
        }

        public void OnTraderLevelChanged(Trader trader)
        {
            bool preNeeded = neededFor[3];

            // Ensure all barters for trader levels <= current (and > current if Mod.checkmarkFutureBarters) are in current
            if (neededForBarterByLevelByTrader.TryGetValue(trader.index, out Dictionary<int, Dictionary<Barter, int>> tradeLevels))
            {
                foreach (KeyValuePair<int, Dictionary<Barter, int>> tradeLevelEntry in tradeLevels)
                {
                    if (Mod.checkmarkFutureBarters || tradeLevelEntry.Key <= trader.level)
                    {
                        if (neededForBarterByLevelByTraderCurrent.TryGetValue(trader.index, out Dictionary<int, Dictionary<Barter, int>> levelDict))
                        {
                            if (!levelDict.ContainsKey(tradeLevelEntry.Key))
                            {
                                Dictionary<int, Dictionary<Barter, int>> newLevelsDict = new Dictionary<int, Dictionary<Barter, int>>();
                                Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                                foreach (KeyValuePair<Barter, int> toAddBarterEntry in tradeLevelEntry.Value)
                                {
                                    newBarterDict.Add(toAddBarterEntry.Key, toAddBarterEntry.Value);
                                }
                                levelDict.Add(tradeLevelEntry.Key, newBarterDict);
                            }
                        }
                        else
                        {
                            Dictionary<int, Dictionary<Barter, int>> newLevelsDict = new Dictionary<int, Dictionary<Barter, int>>();
                            Dictionary<Barter, int> newBarterDict = new Dictionary<Barter, int>();
                            foreach (KeyValuePair<Barter, int> toAddBarterEntry in tradeLevelEntry.Value)
                            {
                                newBarterDict.Add(toAddBarterEntry.Key, toAddBarterEntry.Value);
                            }
                            newLevelsDict.Add(tradeLevelEntry.Key, newBarterDict);
                            neededForBarterByLevelByTraderCurrent.Add(trader.index, newLevelsDict);
                        }
                    }
                }
            }


            if (!Mod.checkmarkFutureBarters)
            {
                // Ensure all barters for trader levels > current are not in current
                if (neededForBarterByLevelByTraderCurrent.TryGetValue(trader.index, out Dictionary<int, Dictionary<Barter, int>> levels))
                {
                    // Note that we get and iterate overt the dict from neededForBarterByLevelByTrader and not current
                    // this is because we want to modify current, which we can't do while iterating over it
                    if (neededForBarterByLevelByTrader.TryGetValue(trader.index, out Dictionary<int, Dictionary<Barter, int>> toCheckLevels))
                    {
                        foreach (KeyValuePair<int, Dictionary<Barter, int>> toCheckLevelEntry in toCheckLevels)
                        {
                            if(toCheckLevelEntry.Key > trader.level && levels.ContainsKey(toCheckLevelEntry.Key))
                            {
                                // Barters of this level NOT currently needed but are in current, must remove
                                levels.Remove(toCheckLevelEntry.Key);
                                if(levels.Count == 0)
                                {
                                    neededForBarterByLevelByTraderCurrent.Remove(trader.index);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            neededFor[3] = neededForBarterByLevelByTraderCurrent.Count > 0;

            if (preNeeded != neededFor[3])
            {
                OnNeededForChangedInvoke(3);
            }
        }

        public bool GetCheckmark(out Color color)
        {
            // Find needed with highest priority
            int currentNeeded = -1;
            int currentHighest = -1;
            for (int i = 0; i < 5; ++i)
            {
                if (neededFor[i] && Mod.neededForPriorities[i] > currentHighest)
                {
                    currentNeeded = i;
                    currentHighest = Mod.neededForPriorities[i];
                }
            }

            // Set checkmark if necessary
            if (currentNeeded > -1)
            {
                // Handle special case of areas
                if (currentNeeded == 1)
                {
                    long itemCount = Mod.GetItemCountInInventories(tarkovID);
                    if (Mod.checkmarkAreaFulfillledMinimum)
                    {
                        if (itemCount >= minimumUpgradeAmount)
                        {
                            color = Mod.neededForAreaFulfilledColor;
                        }
                        else
                        {
                            color = Mod.neededForColors[1];
                        }
                    }
                    else
                    {
                        if (itemCount >= neededForAreaTotal)
                        {
                            color = Mod.neededForAreaFulfilledColor;
                        }
                        else
                        {
                            color = Mod.neededForColors[1];
                        }
                    }
                }
                else // Not area, return corresponding color
                {
                    color = Mod.neededForColors[currentNeeded];
                }

                return true;
            }

            color = Color.clear;
            return false;
        }

        public long GetCurrentNeededForTotal()
        {
            return neededForAreaTotal + neededForTaskTotal;
        }

        public void OnItemFoundInvoke()
        {
            if(OnItemFound != null)
            {
                OnItemFound();
            }
        }

        public void OnItemLeftInvoke(string locationID)
        {
            if(OnItemLeft != null)
            {
                OnItemLeft(locationID);
            }
        }

        public void OnItemUsedInvoke()
        {
            if(OnItemUsed != null)
            {
                OnItemUsed();
            }
        }

        public void OnNeededForChangedInvoke(int index)
        {
            if(OnNeededForChanged != null)
            {
                OnNeededForChanged(index);
            }
        }

        public void OnMinimumUpgradeAmountChangedInvoke()
        {
            if(OnMinimumUpgradeAmountChanged != null)
            {
                OnMinimumUpgradeAmountChanged();
            }
        }

        public void OnNeededForAreaTotalChangedInvoke()
        {
            if(OnNeededForAreaTotalChanged != null)
            {
                OnNeededForAreaTotalChanged();
            }
        }

        public void OnNeededForTaskTotalChangedInvoke()
        {
            if(OnNeededForTaskTotalChanged != null)
            {
                OnNeededForTaskTotalChanged();
            }
        }

        public static void OnAddedToWishlistInvoke(MeatovItemData itemData)
        {
            if (OnAddedToWishlist != null)
            {
                OnAddedToWishlist(itemData);
            }
        }

        public void OnHideoutItemInventoryChangedInvoke(int difference)
        {
            if (OnHideoutItemInventoryChanged != null)
            {
                OnHideoutItemInventoryChanged(difference);
            }
        }

        public void OnPlayerItemInventoryChangedInvoke(int difference)
        {
            if (OnPlayerItemInventoryChanged != null)
            {
                OnPlayerItemInventoryChanged(difference);
            }
        }
    }
}
