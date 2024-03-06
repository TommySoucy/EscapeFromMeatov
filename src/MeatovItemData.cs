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
        public int index; // Custom EFM item index

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

        public MeatovItemData(JToken data)
        {
            tarkovID = data["tarkovID"].ToString();
            H3ID = data["H3ID"].ToString();
            H3SpawnerID = data["H3SpawnerID"].ToString();
            index = (int)data["index"];

            itemType = (MeatovItem.ItemType)Enum.Parse(typeof(MeatovItem.ItemType), data["itemType"].ToString());
            rarity = (MeatovItem.ItemRarity)Enum.Parse(typeof(MeatovItem.ItemRarity), data["rarity"].ToString());
            parents = data["parents"].ToObject<string[]>();
            for(int i=0; i < parents.Length; ++i)
            {
                if (Mod.itemsByParents.TryGetValue(parents[i], out List<MeatovItemData> parentList))
                {
                    parentList.Add(this);
                }
                else
                {
                    Mod.itemsByParents.Add(parents[i], parentList);
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

            Dictionary<string, JToken> consumeEffectData = data["consumeEffects"].ToObject<Dictionary<string, JToken>>();
            foreach (KeyValuePair<string, JToken> consumeEffectEntry in consumeEffectData)
            {
                ConsumableEffect consumableEffect = new ConsumableEffect();
                consumeEffects.Add(consumableEffect);
                consumableEffect.value = (float)consumeEffectEntry.Value["value"];
                consumableEffect.delay = (float)consumeEffectEntry.Value["delay"];
                consumableEffect.duration = (float)consumeEffectEntry.Value["duration"];
                consumableEffect.cost = (int)consumeEffectEntry.Value["cost"];
                switch (consumeEffectEntry.Key)
                {
                    case "RadExposure":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.RadExposure;
                        break;
                    case "Pain":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Pain;
                        if (consumeEffectEntry.Value["fadeOut"] != null)
                        {
                            consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        }
                        break;
                    case "Contusion":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Contusion;
                        consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        break;
                    case "Intoxication":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Intoxication;
                        consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        break;
                    case "LightBleeding":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.LightBleeding;
                        consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        break;
                    case "Fracture":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Fracture;
                        consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        break;
                    case "DestroyedPart":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.DestroyedPart;
                        consumableEffect.healthPenaltyMax = (float)consumeEffectEntry.Value["healthPenaltyMax"] / 100;
                        consumableEffect.healthPenaltyMin = (float)consumeEffectEntry.Value["healthPenaltyMin"] / 100;
                        break;
                    case "HeavyBleeding":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.HeavyBleeding;
                        consumableEffect.fadeOut = (float)consumeEffectEntry.Value["fadeOut"];
                        break;
                    case "Hydration":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Hydration;
                        break;
                    case "Energy":
                        consumableEffect.effectType = ConsumableEffect.ConsumableEffectType.Energy;
                        break;
                }
            }

            Dictionary<string, JToken> buffEffectData = data["buffEffects"].ToObject<Dictionary<string, JToken>>();
            foreach (KeyValuePair<string, JToken> buffEffectEntry in buffEffectData)
            {
                BuffEffect buffEffect = new BuffEffect();
                effects.Add(buffEffect);
                buffEffect.effectType = (Effect.EffectType)Enum.Parse(typeof(Effect.EffectType), buffEffectEntry.Value["effectType"].ToString());
                buffEffect.value = (float)buffEffectEntry.Value["value"];
                buffEffect.chance = (float)buffEffectEntry.Value["chance"];
                buffEffect.delay = (float)buffEffectEntry.Value["delay"];
                buffEffect.duration = (float)buffEffectEntry.Value["duration"];
                buffEffect.absolute = (bool)buffEffectEntry.Value["absolute"];
                buffEffect.skillIndex = (int)buffEffectEntry.Value["skillIndex"];
            }
        }
    }
}
