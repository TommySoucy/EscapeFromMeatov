﻿using FistVR;
using System.Collections.Generic;
using UnityEngine;
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

                        // Some types of generations specify an item list
                        if (itemCountWeight.Key.Equals("backpackLoot"))
                        {
                            JArray itemList = botData["inventory"]["items"]["Backpack"] as JArray;

                            for(int i=0; i < maxCount; ++i)
                            {
                                string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                {
                                    if (itemDict.ContainsKey(itemData))
                                    {
                                        itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                    }
                                    else
                                    {
                                        itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                    }
                                }
                                else
                                {
                                    if (!Mod.oldItemMap.ContainsKey(itemID))
                                    {
                                        Mod.LogError("Could not get item data for " + itemID + " in bot inventory backpack items");
                                    }
                                }
                            }
                        }
                        else if (itemCountWeight.Key.Equals("pocketLoot"))
                        {
                            JArray itemList = botData["inventory"]["items"]["Pockets"] as JArray;

                            for (int i = 0; i < maxCount; ++i)
                            {
                                string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                {
                                    if (itemDict.ContainsKey(itemData))
                                    {
                                        itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                    }
                                    else
                                    {
                                        itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                    }
                                }
                                else
                                {
                                    if (!Mod.oldItemMap.ContainsKey(itemID))
                                    {
                                        Mod.LogError("Could not get item data for " + itemID + " in bot inventory pocket items");
                                    }
                                }
                            }
                        } 
                        else if (itemCountWeight.Key.Equals("vestLoot"))
                        {
                            JArray itemList = botData["inventory"]["items"]["TacticalVest"] as JArray;

                            for (int i = 0; i < maxCount; ++i)
                            {
                                string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                {
                                    if (itemDict.ContainsKey(itemData))
                                    {
                                        itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                    }
                                    else
                                    {
                                        itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                    }
                                }
                                else
                                {
                                    if (!Mod.oldItemMap.ContainsKey(itemID))
                                    {
                                        Mod.LogError("Could not get item data for " + itemID + " in bot inventory vest items");
                                    }
                                }
                            }
                        } 
                        else if (itemCountWeight.Key.Equals("specialItems"))
                        {
                            JArray itemList = botData["inventory"]["items"]["SpecialLoot"] as JArray;

                            for (int i = 0; i < maxCount; ++i)
                            {
                                string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                {
                                    if (itemDict.ContainsKey(itemData))
                                    {
                                        itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                    }
                                    else
                                    {
                                        itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                    }
                                }
                                else
                                {
                                    if (!Mod.oldItemMap.ContainsKey(itemID))
                                    {
                                        Mod.LogError("Could not get item data for " + itemID + " in bot inventory special items");
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("drugs")) // 5448f3a14bdc2d27728b4569
                        {
                            if(Mod.itemsByParents.TryGetValue("5448f3a14bdc2d27728b4569", out List<MeatovItemData> itemDatas))
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("grenades")) // 543be6564bdc2df4348b4568
                        {
                            if (Mod.itemsByParents.TryGetValue("543be6564bdc2df4348b4568", out List<MeatovItemData> itemDatas))
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("healing")) // 5448f39d4bdc2d0a728b4568
                        {
                            if (Mod.itemsByParents.TryGetValue("5448f39d4bdc2d0a728b4568", out List<MeatovItemData> itemDatas))
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("magazines")) // 5448bc234bdc2d3c308b4569
                        {
                            if(equipment.TryGetValue("FirstPrimaryWeapon", out MeatovItemData firstPrimaryWeaponData) && firstPrimaryWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if(Mod.magDefaultItemDataByMagType.TryGetValue(firstPrimaryWeaponData.magType, out List<MeatovItemData> itemDatas))
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);
                                }
                                else
                                {
                                    Mod.LogError("No mag item data for mag type " + firstPrimaryWeaponData.magType + " needed for bot inventory first primary weapon " + firstPrimaryWeaponData.tarkovID);
                                }
                            }
                            if(equipment.TryGetValue("SecondPrimaryWeapon", out MeatovItemData secondPrimaryWeaponData) && secondPrimaryWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if(Mod.magDefaultItemDataByMagType.TryGetValue(secondPrimaryWeaponData.magType, out List<MeatovItemData> itemDatas))
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);
                                }
                                else
                                {
                                    Mod.LogError("No mag item data for mag type " + secondPrimaryWeaponData.magType + " needed for bot inventory second primary weapon " + secondPrimaryWeaponData.tarkovID);
                                }
                            }
                            if(equipment.TryGetValue("Holster", out MeatovItemData holsterWeaponData) && holsterWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if(Mod.magDefaultItemDataByMagType.TryGetValue(holsterWeaponData.magType, out List<MeatovItemData> itemDatas))
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);
                                }
                                else
                                {
                                    Mod.LogError("No mag item data for mag type " + holsterWeaponData.magType + " needed for bot inventory holster weapon " + holsterWeaponData.tarkovID);
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("stims")) // 5448f3a64bdc2d60728b456a
                        {
                            if (Mod.itemsByParents.TryGetValue("5448f3a64bdc2d60728b456a", out List<MeatovItemData> itemDatas))
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                        }
                                    }
                                }
                            }
                        }
                        else // Rarity
                        {
                            for (int i = 0; i < maxCount; ++i)
                            {
                                MeatovItem.ItemRarity rarity = MeatovItem.ItemRarity.Common;
                                int rarityRand = UnityEngine.Random.Range(0, 100);
                                int rarityTotalWeight = 0;
                                for (int j = 0; j < Mod.rarityWeights.Length; ++j)
                                {
                                    rarityTotalWeight += Mod.rarityWeights[j];
                                    if (rarityRand < rarityTotalWeight)
                                    {
                                        rarity = (MeatovItem.ItemRarity)j;
                                    }
                                }

                                if (Mod.itemsByRarity.TryGetValue(rarity, out List<MeatovItemData> itemDatas))
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        itemDict.Add(itemData, UnityEngine.Random.Range(1, itemData.maxStack + 1));
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        public SosigOutfitConfig GetOutfitConfig()
        {
            SosigOutfitConfig outfitConfig = ScriptableObject.CreateInstance<SosigOutfitConfig>();
            cont from here // Set in defaulti tem data what sosig wearable/item an item corresponds to, so we can use it here to generate outfit from inventory
        }
    }
}