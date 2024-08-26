using System.Collections.Generic;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class BotInventory
    {
        /// <summary>
        /// ArmBand
        /// ArmorVest
        /// Backpack
        /// Earpiece
        /// Eyewear
        /// FaceCover
        /// FirstPrimaryWeapon
        /// Headwear
        /// Holster
        /// Scabbard
        /// SecondPrimaryWeapon
        /// TacticalVest
        /// </summary>
        public Dictionary<string, MeatovItemData> equipment;

        /// <summary>
        /// backpackLoot
        /// drugs
        /// grenades
        /// healing
        /// magazines
        /// pocketLoot
        /// specialItems
        /// stims
        /// vestLoot
        /// </summary>
        public Dictionary<string, Dictionary<MeatovItemData, int>> loot;

        public BotInventory(JObject botData)
        {
            equipment = new Dictionary<string, MeatovItemData>();
            Dictionary<string, int> equipmentChances = botData["chances"]["equipment"].ToObject<Dictionary<string, int>>();
            foreach(KeyValuePair<string, int> equipmentChance in equipmentChances)
            {
                if(UnityEngine.Random.value < equipmentChance.Value / 100.0f)
                {
                    Dictionary<string, int> weights = botData["inventory"]["equipment"][equipmentChance.Key].ToObject<Dictionary<string, int>>();
                    int totalWeight = 0;
                    foreach(KeyValuePair<string,int> weight in weights)
                    {
                        totalWeight += weight.Value;
                    }
                    int rand = UnityEngine.Random.Range(0, totalWeight);
                    totalWeight = 0;
                    foreach (KeyValuePair<string, int> weight in weights)
                    {
                        totalWeight += weight.Value;
                        if(rand < totalWeight)
                        {
                            if(Mod.defaultItemData.TryGetValue(weight.Key, out MeatovItemData itemData))
                            {
                                equipment.Add(equipmentChance.Key, itemData);
                                break;
                            }
                            else
                            {
                                if (!Mod.oldItemMap.ContainsKey(weight.Key))
                                {
                                    Mod.LogError("Could not get item data for "+weight.Key+" in bot inventory equipment generation");
                                }
                            }
                        }
                    }
                }
            }

            loot = new Dictionary<string, Dictionary<MeatovItemData, int>>();
            Dictionary<string, JToken> itemCountWeights = botData["generation"]["items"].ToObject<Dictionary<string, JToken>>();
            foreach(KeyValuePair<string, JToken> itemCountWeight in itemCountWeights)
            {
                Dictionary<string, int> weights = itemCountWeight.Value["weights"].ToObject<Dictionary<string, int>>();
                int totalWeight = 0;
                foreach (KeyValuePair<string, int> weight in weights)
                {
                    totalWeight += weight.Value;
                }
                int rand = UnityEngine.Random.Range(0, totalWeight);
                totalWeight = 0;
                foreach (KeyValuePair<string, int> weight in weights)
                {
                    totalWeight += weight.Value;
                    if (rand < totalWeight)
                    {
                        Dictionary<MeatovItemData, int> itemDict = new Dictionary<MeatovItemData, int>();
                        loot.Add(itemCountWeight.Key, itemDict);

                        int maxCount = int.Parse(weight.Key);
                        for(int i=0; i < maxCount; ++i)
                        {
                            td
                        }

                        break;
                    }
                }
            }
        }
    }
}
