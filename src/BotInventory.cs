using FistVR;
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

        public Dictionary<string, int> inventory; // All item counts by tarkovID

        public BotInventory(Dictionary<string, MeatovItemData> equipment, Dictionary<string, Dictionary<MeatovItemData, int>> loot)
        {
            this.equipment = equipment;
            this.loot = loot;

            inventory = new Dictionary<string, int>();
            foreach (KeyValuePair<string, MeatovItemData> equipmentEntry in equipment)
            {
                if (inventory.ContainsKey(equipmentEntry.Key))
                {
                    ++inventory[equipmentEntry.Key];
                }
                else
                {
                    inventory.Add(equipmentEntry.Key, 1);
                }
            }
            foreach (KeyValuePair<string, Dictionary<MeatovItemData, int>> lootEntry in loot)
            {
                foreach (KeyValuePair<MeatovItemData, int> innerEntry in lootEntry.Value)
                {
                    if (inventory.ContainsKey(innerEntry.Key.tarkovID))
                    {
                        inventory[innerEntry.Key.tarkovID] += innerEntry.Value;
                    }
                    else
                    {
                        inventory.Add(innerEntry.Key.tarkovID, innerEntry.Value);
                    }
                }
            }
        }

        public BotInventory(JObject botData)
        {
            inventory = new Dictionary<string, int>();
            TODO: // Take into account blocksEarpiece, blocksEyewear, etc
            equipment = new Dictionary<string, MeatovItemData>();
            Dictionary<string, int> equipmentChances = botData["chances"]["equipment"].ToObject<Dictionary<string, int>>();
            foreach (KeyValuePair<string, int> equipmentChance in equipmentChances)
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

                                if (inventory.ContainsKey(itemData.tarkovID))
                                {
                                    ++inventory[itemData.tarkovID];
                                }
                                else
                                {
                                    inventory.Add(itemData.tarkovID, 1);
                                }

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

                            if(itemList.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                    if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
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
                        }
                        else if (itemCountWeight.Key.Equals("pocketLoot"))
                        {
                            JArray itemList = botData["inventory"]["items"]["Pockets"] as JArray;

                            if (itemList.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                    if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
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
                        } 
                        else if (itemCountWeight.Key.Equals("vestLoot"))
                        {
                            JArray itemList = botData["inventory"]["items"]["TacticalVest"] as JArray;

                            if (itemList.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                    if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
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
                        } 
                        else if (itemCountWeight.Key.Equals("specialItems"))
                        {
                            JArray itemList = botData["inventory"]["items"]["SpecialLoot"] as JArray;

                            if (itemList.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    string itemID = itemList[UnityEngine.Random.Range(0, itemList.Count)].ToString();
                                    if (Mod.defaultItemData.TryGetValue(itemID, out MeatovItemData itemData))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
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
                        }
                        else if(itemCountWeight.Key.Equals("drugs")) // 5448f3a14bdc2d27728b4569
                        {
                            if (Mod.itemsByParents.TryGetValue("5448f3a14bdc2d27728b4569", out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("grenades")) // 543be6564bdc2df4348b4568
                        {
                            if (Mod.itemsByParents.TryGetValue("543be6564bdc2df4348b4568", out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("healing")) // 5448f39d4bdc2d0a728b4568
                        {
                            if (Mod.itemsByParents.TryGetValue("5448f39d4bdc2d0a728b4568", out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
                                        }
                                    }
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("magazines")) // 5448bc234bdc2d3c308b4569
                        {
                            if (equipment.TryGetValue("FirstPrimaryWeapon", out MeatovItemData firstPrimaryWeaponData) && firstPrimaryWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if (Mod.magDefaultItemDataByMagType.TryGetValue(firstPrimaryWeaponData.magType, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);

                                    if (inventory.ContainsKey(itemData.tarkovID))
                                    {
                                        inventory[itemData.tarkovID] += maxCount;
                                    }
                                    else
                                    {
                                        inventory.Add(itemData.tarkovID, maxCount);
                                    }
                                }
                                else
                                {
                                    Mod.LogWarning("No mag item data for mag type " + firstPrimaryWeaponData.magType + " needed for bot inventory first primary weapon " + firstPrimaryWeaponData.tarkovID);
                                }
                            }
                            if(equipment.TryGetValue("SecondPrimaryWeapon", out MeatovItemData secondPrimaryWeaponData) && secondPrimaryWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if (Mod.magDefaultItemDataByMagType.TryGetValue(secondPrimaryWeaponData.magType, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);

                                    if (inventory.ContainsKey(itemData.tarkovID))
                                    {
                                        inventory[itemData.tarkovID] += maxCount;
                                    }
                                    else
                                    {
                                        inventory.Add(itemData.tarkovID, maxCount);
                                    }
                                }
                                else
                                {
                                    Mod.LogWarning("No mag item data for mag type " + secondPrimaryWeaponData.magType + " needed for bot inventory second primary weapon " + secondPrimaryWeaponData.tarkovID);
                                }
                            }
                            if(equipment.TryGetValue("Holster", out MeatovItemData holsterWeaponData) && holsterWeaponData.magType != FistVR.FireArmMagazineType.mNone)
                            {
                                if (Mod.magDefaultItemDataByMagType.TryGetValue(holsterWeaponData.magType, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    itemDict.Add(itemData, maxCount);

                                    if (inventory.ContainsKey(itemData.tarkovID))
                                    {
                                        inventory[itemData.tarkovID] += maxCount;
                                    }
                                    else
                                    {
                                        inventory.Add(itemData.tarkovID, maxCount);
                                    }
                                }
                                else
                                {
                                    Mod.LogWarning("No mag item data for mag type " + holsterWeaponData.magType + " needed for bot inventory holster weapon " + holsterWeaponData.tarkovID);
                                }
                            }
                        }
                        else if(itemCountWeight.Key.Equals("stims")) // 5448f3a64bdc2d60728b456a
                        {
                            if (Mod.itemsByParents.TryGetValue("5448f3a64bdc2d60728b456a", out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                            {
                                for (int i = 0; i < maxCount; ++i)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        if (itemDict.ContainsKey(itemData))
                                        {
                                            itemDict[itemData] += stack;
                                        }
                                        else
                                        {
                                            itemDict.Add(itemData, stack);
                                        }

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
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

                                if (Mod.itemsByRarity.TryGetValue(rarity, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                                {
                                    MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                                    if (Mod.IDDescribedInList(itemData.tarkovID, new List<string>(itemData.parents), itemCountWeight.Value["whitelist"].ToObject<List<string>>(), null))
                                    {
                                        int stack = UnityEngine.Random.Range(1, itemData.maxStack + 1);
                                        itemDict.Add(itemData, stack);

                                        if (inventory.ContainsKey(itemData.tarkovID))
                                        {
                                            inventory[itemData.tarkovID] += stack;
                                        }
                                        else
                                        {
                                            inventory.Add(itemData.tarkovID, stack);
                                        }
                                    }
                                }
                            }
                        }

                        break;
                    }
                }
            }
        }

        public List<FVRObject>[] GetOutfit(bool PMC)
        {
            List<FVRObject>[] linkOutfit = new List<FVRObject>[4];

            List<MeatovItemData> possibleHeadwear = new List<MeatovItemData>();
            List<MeatovItemData> possibleFacewear = new List<MeatovItemData>();
            List<MeatovItemData> possibleEarpiece = new List<MeatovItemData>();

            List<MeatovItemData> possibleTorso = new List<MeatovItemData>();
            List<MeatovItemData> possibleBackpack = new List<MeatovItemData>();

            List<MeatovItemData> possibleAbdo = new List<MeatovItemData>();

            List<MeatovItemData> possibleLeg = new List<MeatovItemData>();

            // Equipment
            foreach(KeyValuePair<string, MeatovItemData> equipmentEntry in equipment)
            {
                if (equipmentEntry.Value.sosigHeadwear != null && equipmentEntry.Value.sosigHeadwear.Count > 0)
                {
                    possibleHeadwear.Add(equipmentEntry.Value);
                }
                if(equipmentEntry.Value.sosigFacewear != null && equipmentEntry.Value.sosigFacewear.Count > 0)
                {
                    possibleFacewear.Add(equipmentEntry.Value);
                }
                if(equipmentEntry.Value.sosigEarpiece != null && equipmentEntry.Value.sosigEarpiece.Count > 0)
                {
                    possibleEarpiece.Add(equipmentEntry.Value);
                }

                if(equipmentEntry.Value.sosigTorso != null && equipmentEntry.Value.sosigTorso.Count > 0)
                {
                    possibleTorso.Add(equipmentEntry.Value);
                }
                if(equipmentEntry.Value.sosigBackpack != null && equipmentEntry.Value.sosigBackpack.Count > 0)
                {
                    possibleBackpack.Add(equipmentEntry.Value);
                }

                if(equipmentEntry.Value.sosigAbdo != null && equipmentEntry.Value.sosigAbdo.Count > 0)
                {
                    possibleAbdo.Add(equipmentEntry.Value);
                }

                if(equipmentEntry.Value.sosigLeg != null && equipmentEntry.Value.sosigLeg.Count > 0)
                {
                    possibleLeg.Add(equipmentEntry.Value);
                }
            }

            linkOutfit[0] = new List<FVRObject>();
            if(possibleHeadwear.Count > 0)
            {
                MeatovItemData chosenHeadwearItem = possibleHeadwear[UnityEngine.Random.Range(0, possibleHeadwear.Count)];
                linkOutfit[0].Add(chosenHeadwearItem.sosigHeadwear[UnityEngine.Random.Range(0, chosenHeadwearItem.sosigHeadwear.Count)]);
            }
            if(possibleFacewear.Count > 0)
            {
                MeatovItemData chosenFacewearItem = possibleFacewear[UnityEngine.Random.Range(0, possibleFacewear.Count)];
                linkOutfit[0].Add(chosenFacewearItem.sosigFacewear[UnityEngine.Random.Range(0, chosenFacewearItem.sosigFacewear.Count)]);
            }
            if(possibleEarpiece.Count > 0)
            {
                MeatovItemData chosenEarpieceItem = possibleEarpiece[UnityEngine.Random.Range(0, possibleEarpiece.Count)];
                linkOutfit[0].Add(chosenEarpieceItem.sosigEarpiece[UnityEngine.Random.Range(0, chosenEarpieceItem.sosigEarpiece.Count)]);
            }

            linkOutfit[1] = new List<FVRObject>();
            MeatovItemData chosenTorsoItem = null;
            int torsoIndex = 0;
            if (possibleTorso.Count > 0)
            {
                chosenTorsoItem = possibleTorso[UnityEngine.Random.Range(0, possibleTorso.Count)];
                torsoIndex = UnityEngine.Random.Range(0, chosenTorsoItem.sosigTorso.Count);
                linkOutfit[1].Add(chosenTorsoItem.sosigTorso[torsoIndex]);
            }
            MeatovItemData chosenAbdoItem = null;
            int abdoIndex = 0;
            bool addLeg = false;
            linkOutfit[2] = new List<FVRObject>();
            if (chosenTorsoItem != null && chosenTorsoItem.sosigAbdoMatchTorso)
            {
                int currentAbdoIndex = 0;
                foreach(KeyValuePair<FVRObject, bool> abdoEntry in chosenTorsoItem.sosigAbdo)
                {
                    if(currentAbdoIndex == torsoIndex)
                    {
                        chosenAbdoItem = chosenTorsoItem;
                        linkOutfit[2].Add(abdoEntry.Key);
                        addLeg = abdoEntry.Value;
                        abdoIndex = torsoIndex;
                        break;
                    }
                    ++currentAbdoIndex;
                }
            }
            if (chosenAbdoItem == null && possibleAbdo.Count > 0)
            {
                chosenAbdoItem = possibleAbdo[UnityEngine.Random.Range(0, possibleAbdo.Count)];
                abdoIndex = UnityEngine.Random.Range(0, chosenAbdoItem.sosigAbdo.Count);
                int currentAbdoIndex = 0;
                foreach (KeyValuePair<FVRObject, bool> abdoEntry in chosenAbdoItem.sosigAbdo)
                {
                    if (currentAbdoIndex == abdoIndex)
                    {
                        linkOutfit[2].Add(abdoEntry.Key);
                        addLeg = abdoEntry.Value;
                        break;
                    }
                    ++currentAbdoIndex;
                }
            }
            MeatovItemData chosenLegItem = null;
            linkOutfit[3] = new List<FVRObject>();
            if (chosenAbdoItem != null && chosenAbdoItem.sosigLegMatchAbdo)
            {
                chosenLegItem = chosenAbdoItem;
                linkOutfit[3].Add(chosenLegItem.sosigLeg[abdoIndex]);
            }
            if (chosenLegItem == null && addLeg && possibleLeg.Count > 0)
            {
                chosenLegItem = possibleLeg[UnityEngine.Random.Range(0, possibleLeg.Count)];
                linkOutfit[3].Add(chosenLegItem.sosigLeg[UnityEngine.Random.Range(0, chosenLegItem.sosigLeg.Count)]);
            }

            // Clothes
            if (PMC)
            {
                TODO: // These should not be hardcoded here, they should be defined in a json file in DB. Even better, they should use the appearance in botData
                if (linkOutfit[1].Count == 0)
                {
                    string[] torsoID = {
                        "SosigAccessory_MountainMeat_Jacket_Black",
                        "SosigAccessory_MountainMeat_Jacket_Brown",
                        "SosigAccessory_MountainMeat_Jacket_Green",
                        "SosigCasualUndershirtCamoDesert",
                        "SosigCasualUndershirtCamoForest",
                        "SosigCasualUndershirtCamoNight",
                        "SosigCasualUndershirtCamoUrban",
                        "SosigAccessory_MountainMeat_Undershirt_Green",
                        "SosigCasualHoodieHoodDownBlack",
                        "SosigCasualHoodieHoodDownBlue",
                        "SosigCasualHoodieHoodDownForest",
                        "SosigCasualHoodieHoodDownGreen",
                        "SosigCasualHoodieHoodDownOlive",
                    };
                    linkOutfit[1].Add(IM.OD[torsoID[UnityEngine.Random.Range(0, torsoID.Length)]]);
                }

                if (linkOutfit[2].Count == 0)
                {
                    string[] pantsID = {
                        "SosigAccessory_MountainMeat_Pants_Black",
                        "SosigAccessory_MountainMeat_Pants_Brown",
                        "SosigAccessory_MountainMeat_Pants_Green",
                        "SosigAccessory_MountainMeat_Pants_Grey",
                        "SosigAccessory_MountainMeat_Pants_LightGrey",
                        "SosigAccessory_MountainMeat_Pants_Olive",
                        "SosigAccessory_MountainMeat_Pants_Tan",
                        "SosigAccessory_MountainMeat_Pants_Umber",
                        "SosigCasualShortsBlack",
                        "SosigCasualShortsBrown",
                        "SosigCasualShortsGreen",
                        "SosigCasualShortsGrey",
                        "SosigCasualShortsLightGrey",
                        "SosigCasualShortsOlive",
                        "SosigCasualShortsTan",
                        "SosigCasualShortsUmber",
                    };
                    linkOutfit[2].Add(IM.OD[pantsID[UnityEngine.Random.Range(0, pantsID.Length)]]);
                }
            }
            else
            {
                int clothTorsoIndex = -1;
                if (linkOutfit[1].Count == 0)
                {
                    string[] torsoID = {
                        "SosigCasualTrackSuitTorsoBlack",
                        "SosigCasualTrackSuitTorsoBlue",
                        "SosigCasualTrackSuitTorsoGreen",
                        "SosigCasualTrackSuitTorsoGrey",
                        "SosigCasualTrackSuitTorsoOrange",
                        "SosigCasualTrackSuitTorsoPink",
                        "SosigCasualTrackSuitTorsoPurple",
                        "SosigCasualTrackSuitTorsoRed",
                        "SosigCasualTrackSuitTorsoTurquoise",
                        "SosigCasualTrackSuitTorsoYellow",
                        "SosigCasualTrackSuitTopColorSet1",
                        "SosigCasualTrackSuitTopColorSet2",
                        "SosigCasualTrackSuitTopColorSet3",
                        "SosigCasualTrackSuitTopColorSet4",
                        "SosigCasualTrackSuitTopColorSet5",
                        "SosigCasualTrackSuitTopColorSet6",
                        "SosigCasualTrackSuitTopColorSet7",
                        "SosigCasualTrackSuitTopColorSet8",
                        "SosigCasualTrackSuitTopStripedColorSet1",
                        "SosigCasualTrackSuitTopStripedColorSet2",
                        "SosigCasualTrackSuitTopStripedColorSet3",
                        "SosigCasualTrackSuitTopStripedColorSet4",
                        "SosigCasualTrackSuitTopStripedColorSet5",
                        "SosigCasualTrackSuitTopStripedColorSet6",
                        "SosigCasualTrackSuitTopStripedColorSet7",
                        "SosigCasualTrackSuitTopStripedColorSet8",
                        "SosigAccessory_MountainMeat_Jacket_Black",
                        "SosigAccessory_MountainMeat_Jacket_Brown",
                        "SosigAccessory_MountainMeat_Jacket_Flannel",
                        "SosigAccessory_MountainMeat_Jacket_Green",
                        "SosigAccessory_MountainMeat_Jacket_Jean",
                        "SosigAccessory_MountainMeat_Jacket_Orange",
                        "SosigAccessory_MountainMeat_Undershirt_Blue",
                        "SosigAccessory_MountainMeat_Undershirt_Brown",
                        "SosigAccessory_MountainMeat_Undershirt_Green",
                        "SosigAccessory_MountainMeat_Undershirt_Tan",
                        "SosigAccessory_MountainMeat_Undershirt_White",
                        "SosigCasualUndershirtCamoDesert",
                        "SosigCasualUndershirtCamoForest",
                        "SosigCasualUndershirtCamoNight",
                        "SosigCasualUndershirtCamoPink",
                        "SosigCasualUndershirtCamoSky",
                        "SosigCasualUndershirtCamoUrban",
                        "SosigCasualHoodieHoodDownBlack",
                        "SosigCasualHoodieHoodDownBlue",
                        "SosigCasualHoodieHoodDownCream",
                        "SosigCasualHoodieHoodDownForest",
                        "SosigCasualHoodieHoodDownGreen",
                        "SosigCasualHoodieHoodDownLavendar",
                        "SosigCasualHoodieHoodDownMustard",
                        "SosigCasualHoodieHoodDownOlive",
                        "SosigCasualHoodieHoodDownPeach",
                        "SosigCasualHoodieHoodDownRed",
                        "SosigCasualHoodieHoodDownWhite",
                        "SosigCasualHoodieHoodDownYellow",
                    };
                    clothTorsoIndex = UnityEngine.Random.Range(0, torsoID.Length);
                    linkOutfit[1].Add(IM.OD[torsoID[clothTorsoIndex]]);
                }

                int clothPantsIndex = -1;
                if (linkOutfit[2].Count == 0)
                {
                    string[] pantsID = {
                        "SosigCasualTrackSuitAbdoBlack",
                        "SosigCasualTrackSuitAbdoBlue",
                        "SosigCasualTrackSuitAbdoGreen",
                        "SosigCasualTrackSuitAbdoGrey",
                        "SosigCasualTrackSuitAbdoOrange",
                        "SosigCasualTrackSuitAbdoPink",
                        "SosigCasualTrackSuitAbdoPurple",
                        "SosigCasualTrackSuitAbdoRed",
                        "SosigCasualTrackSuitAbdoTurquoise",
                        "SosigCasualTrackSuitAbdoYellow",
                        "SosigAccessory_MountainMeat_Pants_Black",
                        "SosigAccessory_MountainMeat_Pants_Brown",
                        "SosigAccessory_MountainMeat_Pants_Green",
                        "SosigAccessory_MountainMeat_Pants_Grey",
                        "SosigAccessory_MountainMeat_Pants_JeanDark",
                        "SosigAccessory_MountainMeat_Pants_JeanNew",
                        "SosigAccessory_MountainMeat_Pants_JeanWorn",
                        "SosigAccessory_MountainMeat_Pants_LightGrey",
                        "SosigAccessory_MountainMeat_Pants_Olive",
                        "SosigAccessory_MountainMeat_Pants_Tan",
                        "SosigAccessory_MountainMeat_Pants_Umber",
                        "SosigCasualShortsBlack",
                        "SosigCasualShortsBrown",
                        "SosigCasualShortsGreen",
                        "SosigCasualShortsGrey",
                        "SosigCasualShortsJeanDark",
                        "SosigCasualShortsJeanNew",
                        "SosigCasualShortsJeanWorn",
                        "SosigCasualShortsLightGrey",
                        "SosigCasualShortsOlive",
                        "SosigCasualShortsTan",
                        "SosigCasualShortsUmber",
                    };

                    if (clothTorsoIndex <= 9)
                    {
                        clothPantsIndex = clothTorsoIndex;
                    }
                    else
                    {
                        clothPantsIndex = UnityEngine.Random.Range(0, pantsID.Length);
                    }
                    linkOutfit[2].Add(IM.OD[pantsID[clothPantsIndex]]);
                }

                if (linkOutfit[3].Count == 0)
                {
                    string[] pantsID = {
                        "SosigCasualTrackSuitLegBlack",
                        "SosigCasualTrackSuitLegBlue",
                        "SosigCasualTrackSuitLegGreen",
                        "SosigCasualTrackSuitLegGrey",
                        "SosigCasualTrackSuitLegOrange",
                        "SosigCasualTrackSuitLegPink",
                        "SosigCasualTrackSuitLegPurple",
                        "SosigCasualTrackSuitLegRed",
                        "SosigCasualTrackSuitLegTurquoise",
                        "SosigCasualTrackSuitLegYellow",
                    };

                    if (clothPantsIndex <= 9)
                    {
                        linkOutfit[3].Add(IM.OD[pantsID[clothPantsIndex]]);
                    }
                    else
                    {
                        linkOutfit[3].Add(IM.OD[pantsID[UnityEngine.Random.Range(0, pantsID.Length)]]);
                    }
                }
            }

            return linkOutfit;
        }

        public void Spawn(MeatovItem lootBox, bool PMC)
        {
            MeatovItem backpack = null;
            MeatovItem vest = null;
            foreach(KeyValuePair<string, MeatovItemData> equipmentEntry in equipment)
            {
                bool isBackpack = equipmentEntry.Key.Equals("Backpack");
                bool isVest = equipmentEntry.Key.Equals("TacticalVest") || equipmentEntry.Key.Equals("ArmorVest");
                ContainmentVolume.SpawnItemReturnDelegate del = objs =>
                {
                    for(int i=0; i < objs.Count; ++i)
                    {
                        MeatovItem meatovItem = objs[i];
                        if (isBackpack)
                        {
                            backpack = meatovItem;
                        }
                        else if (isVest)
                        {
                            vest = meatovItem;
                        }
                    }
                };
                lootBox.containerVolume.SpawnItem(equipmentEntry.Value, 1, !PMC, del);
            }
            foreach(KeyValuePair<string, Dictionary<MeatovItemData, int>> lootGroupEntry in loot)
            {
                foreach(KeyValuePair<MeatovItemData, int> lootEntry in lootGroupEntry.Value)
                {
                    if (lootGroupEntry.Key.Equals("backpackLoot") && backpack != null)
                    {
                        backpack.containerVolume.SpawnItem(lootEntry.Key, lootEntry.Value, !PMC);
                    }
                    else if (lootGroupEntry.Key.Equals("vestLoot") && vest != null && lootEntry.Value == 1)
                    {
                        FVRPhysicalObject.FVRPhysicalObjectSize size = FVRPhysicalObject.FVRPhysicalObjectSize.CantCarryBig;
                        for (int i = 0; i < Mod.sizeVolumes.Length; ++i)
                        {
                            if (lootEntry.Key.volumes[0] <= Mod.sizeVolumes[i])
                            {
                                size = (FVRPhysicalObject.FVRPhysicalObjectSize)i;
                                break;
                            }
                        }

                        bool spawned = false;
                        for (int i=0; i < vest.rigSlots.Count; ++i)
                        {
                            if (vest.rigSlots[i] != null && vest.rigSlots[i].CurObject == null && (int)size <= (int)vest.rigSlots[i].SizeLimit)
                            {
                                vest.rigSlots[i].SpawnItem(lootEntry.Key, 1, !PMC);
                                spawned = true;
                                break;
                            }
                        }

                        if (!spawned)
                        {
                            lootBox.containerVolume.SpawnItem(lootEntry.Key, lootEntry.Value, !PMC);
                        }
                    }
                    else
                    {
                        lootBox.containerVolume.SpawnItem(lootEntry.Key, lootEntry.Value, !PMC);
                    }
                }
            }
        }

        public static string GetEquipmentNameByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "ArmBand";
                case 1:
                    return "ArmorVest";
                case 2:
                    return "Backpack";
                case 3:
                    return "Earpiece";
                case 4:
                    return "Eyewear";
                case 5:
                    return "FaceCover";
                case 6:
                    return "FirstPrimaryWeapon";
                case 7:
                    return "Headwear";
                case 8:
                    return "Holster";
                case 9:
                    return "Scabbard";
                case 10:
                    return "SecondPrimaryWeapon";
                case 11:
                    return "TacticalVest";
            }

            return null;
        }

        public static int GetIndexByEquipmentName(string equipmentName) 
        {
            switch (equipmentName)
            {
                case "ArmBand":
                    return 0;
                case "ArmorVest":
                    return 1;
                case "Backpack":
                    return 2;
                case "Earpiece":
                    return 3;
                case "Eyewear":
                    return 4;
                case "FaceCover":
                    return 5;
                case "FirstPrimaryWeapon":
                    return 6;
                case "Headwear":
                    return 7;
                case "Holster":
                    return 8;
                case "Scabbard":
                    return 9;
                case "SecondPrimaryWeapon":
                    return 10;
                case "TacticalVest":
                    return 11;
            }

            return -1;
        }

        public static string GetLootNameByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "backpackLoot";
                case 1:
                    return "drugs";
                case 2:
                    return "grenades";
                case 3:
                    return "healing";
                case 4:
                    return "magazines";
                case 5:
                    return "pocketLoot";
                case 6:
                    return "specialItems";
                case 7:
                    return "stims";
                case 8:
                    return "vestLoot";
            }

            return null;
        }

        public static int GetIndexByLootName(string lootName) 
        {
            switch (lootName)
            {
                case "backpackLoot":
                    return 0;
                case "drugs":
                    return 1;
                case "grenades":
                    return 2;
                case "healing":
                    return 3;
                case "magazines":
                    return 4;
                case "pocketLoot":
                    return 5;
                case "specialItems":
                    return 6;
                case "stims":
                    return 7;
                case "vestLoot":
                    return 8;
            }

            return -1;
        }
    }
}
