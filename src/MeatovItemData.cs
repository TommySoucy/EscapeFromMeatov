using FistVR;
using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    // This should contain:
    // - everything we will need to instantiate an item (Really just an item ID)
    // - everything a description would need (Because we might not have a physical item as a describable)
    // - all static tarkov data for a vanilla item (Not live data like if it is currently isured or something like that)
    public class MeatovItemData
    {
        // Identifying data
        public string tarkovID; // Original tarkov ID
        public string H3ID; // ID of the item in IM.OD or the index in string form in case of a custom EFM item
        public string H3SpawnerID; // ID of the item in IM.SpawnerIDDic (Vanilla/Non-EFM Modded item only)
        public int index = -1; // Custom EFM item index

        public MeatovItem.ItemType itemType;
        public MeatovItem.ItemRarity rarity;
        public string[] parents;
        public int weight;
        public int[] volumes;
        public int lootExperience;
        public string name;
        public string description;
        public bool canSellOnRagfair;
        // Weapon/Ammo container
        public int compatibilityValue;  // 0: Does not need mag or round, 1: Needs mag, 2: Needs round, 3: Needs both, used by description to display list of compatibilities present in hideout
        public bool usesMags; // Could be clip
        public bool usesAmmoContainers; // Could be internal mag or revolver
        public FireArmMagazineType magType;
        public FireArmClipType clipType;
        public FireArmRoundType roundType; // Caliber
        public MeatovItem.WeaponClass weaponclass;
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
        // Ammo box
        public string cartridge; // The item ID
        public FireArmRoundClass roundClass;
        // Stack
        public int maxStack;
        // Amount
        public int maxAmount;
        // Effects
        public float useTime = 0; // Amount of time it takes to use amountRate
        public float amountRate = -1; // Maximum amount that can be used from the consumable in single use ex.: grizzly can only be used to heal up to 175 hp per use. 0 means a single unit of multiple, -1 means no limit up to maxAmount
        public List<BuffEffect> effects; // Gives new effects
        public List<ConsumableEffect> consumeEffects; // Immediate effects or effects that modify/override/give new effects 
        // Dogtag
        public int dogtagLevel = 1;
        public string dogtagName;

        public delegate void OnItemFoundDelegate();
        public event OnItemFoundDelegate OnItemFound;

        public MeatovItemData(JToken data)
        {
            if(data["tarkovID"] == null)
            {
                return;
            }
            
            tarkovID = data["tarkovID"].ToString();
            H3ID = data["H3ID"].ToString();
            H3SpawnerID = data["H3SpawnerID"] == null ? null : data["H3SpawnerID"].ToString();
            index = data["index"] == null ? -1 : (int)data["index"];

            itemType = (MeatovItem.ItemType)Enum.Parse(typeof(MeatovItem.ItemType), data["itemType"].ToString());
            rarity = (MeatovItem.ItemRarity)Enum.Parse(typeof(MeatovItem.ItemRarity), data["rarity"].ToString());
            parents = data["parents"].ToObject<string[]>();
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
            name = data["name"].ToString();
            description = data["description"].ToString();
            canSellOnRagfair = (bool)data["canSellOnRagfair"];

            compatibilityValue = (int)data["compatibilityValue"];
            usesMags = (bool)data["usesMags"];
            usesAmmoContainers = (bool)data["usesAmmoContainers"];
            magType = (FireArmMagazineType)Enum.Parse(typeof(FireArmMagazineType), data["magType"].ToString());
            clipType = (FireArmClipType)Enum.Parse(typeof(FireArmClipType), data["clipType"].ToString());
            roundType = (FireArmRoundType)Enum.Parse(typeof(FireArmRoundType), data["roundType"].ToString());
            weaponclass = (MeatovItem.WeaponClass)Enum.Parse(typeof(MeatovItem.WeaponClass), data["weaponclass"].ToString());

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

            cartridge = data["cartridge"].ToString();
            roundClass = (FireArmRoundClass)Enum.Parse(typeof(FireArmRoundClass), data["roundClass"].ToString());

            maxStack = (int)data["maxStack"];

            maxAmount = (int)data["maxAmount"];

            useTime = (float)data["useTime"];
            amountRate = (float)data["amountRate"];

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
        }

        public void OnItemFoundInvoke()
        {
            if(OnItemFound != null)
            {
                OnItemFound();
            }
        }
    }
}
