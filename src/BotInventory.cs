using FistVR;
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
            TODO: // Take into account blocksEarpiece, blocksEyewear, etc
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

        public SosigOutfitConfig GetOutfitConfig(bool PMC)
        {
            SosigOutfitConfig outfitConfig = ScriptableObject.CreateInstance<SosigOutfitConfig>();

            if (equipment.TryGetValue("Headwear", out MeatovItemData headwearItemData) && headwearItemData.sosigEquivalents != null && headwearItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Headwear = 1;
                outfitConfig.Headwear = headwearItemData.sosigEquivalents;
            }
            else
            {
                outfitConfig.Chance_Headwear = 0;
            }
            TODO e: // Make our own sosig otufit config that has a different list for earpieces
            if (equipment.TryGetValue("Earpiece", out MeatovItemData earpieceItemData) && earpieceItemData.sosigEquivalents != null && earpieceItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Headwear = 1;
                if(outfitConfig.Headwear == null)
                {
                    outfitConfig.Headwear = earpieceItemData.sosigEquivalents;
                }
                else
                {
                    outfitConfig.Headwear.AddRange(earpieceItemData.sosigEquivalents);
                }
            }
            else
            {
                if (outfitConfig.Headwear == null)
                {
                    outfitConfig.Chance_Headwear = 0;
                }
            }

            if (equipment.TryGetValue("Eyewear", out MeatovItemData eyewearItemData) && eyewearItemData.sosigEquivalents != null && eyewearItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Eyewear = 1;
                outfitConfig.Eyewear = eyewearItemData.sosigEquivalents;
            }
            else
            {
                outfitConfig.Chance_Eyewear = 0;
            }

            if (equipment.TryGetValue("FaceCover", out MeatovItemData facewearItemData) && facewearItemData.sosigEquivalents != null && facewearItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Facewear = 1;
                outfitConfig.Facewear = facewearItemData.sosigEquivalents;
            }
            else
            {
                outfitConfig.Chance_Facewear = 0;
            }

            if (equipment.TryGetValue("ArmorVest", out MeatovItemData armorvestItemData) && armorvestItemData.sosigEquivalents != null && armorvestItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Torsowear = 1;
                outfitConfig.Torsowear = armorvestItemData.sosigEquivalents;
            }
            else
            {
                outfitConfig.Chance_Torsowear = 0;
            }
            if (equipment.TryGetValue("TacticalVest", out MeatovItemData vestItemData) && vestItemData.sosigEquivalents != null && vestItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Torsowear = 1;
                if(outfitConfig.Torsowear == null)
                {
                    outfitConfig.Torsowear = vestItemData.sosigEquivalents;
                }
                else
                {
                    outfitConfig.Torsowear.AddRange(vestItemData.sosigEquivalents);
                }
            }
            else
            {
                if (outfitConfig.Torsowear == null)
                {
                    outfitConfig.Chance_Torsowear = 0;
                }
            }

            if (equipment.TryGetValue("Backpack", out MeatovItemData backpackItemData) && backpackItemData.sosigEquivalents != null && backpackItemData.sosigEquivalents.Count > 0)
            {
                outfitConfig.Chance_Backpacks = 1;
                outfitConfig.Backpacks = backpackItemData.sosigEquivalents;
            }
            else
            {
                outfitConfig.Chance_Backpacks = 0;
            }

            if (PMC)
            {
                /* Torso
                SosigAccessory_MountainMeat_Jacket_Black
                SosigAccessory_MountainMeat_Jacket_Brown
                SosigAccessory_MountainMeat_Jacket_Green
                SosigCasualUndershirtCamoDesert
                SosigCasualUndershirtCamoForest
                SosigCasualUndershirtCamoNight
                SosigCasualUndershirtCamoUrban
                SosigAccessory_MountainMeat_Undershirt_Green
                SosigCasualHoodieHoodDownBlack
                SosigCasualHoodieHoodDownBlue
                SosigCasualHoodieHoodDownForest
                SosigCasualHoodieHoodDownGreen
                SosigCasualHoodieHoodDownOlive
                */
                /* Pants
                SosigAccessory_MountainMeat_Pants_Black
                SosigAccessory_MountainMeat_Pants_Brown
                SosigAccessory_MountainMeat_Pants_Green
                SosigAccessory_MountainMeat_Pants_Grey
                SosigAccessory_MountainMeat_Pants_LightGrey
                SosigAccessory_MountainMeat_Pants_Olive
                SosigAccessory_MountainMeat_Pants_Tan
                SosigAccessory_MountainMeat_Pants_Umber
                SosigCasualShortsBlack
                SosigCasualShortsBrown
                SosigCasualShortsGreen
                SosigCasualShortsGrey
                SosigCasualShortsLightGrey
                SosigCasualShortsOlive
                SosigCasualShortsTan
                SosigCasualShortsUmber
                */
            }
            else
            {
                /* Torso
                SosigAccessory_MountainMeat_Jacket_Black
                SosigAccessory_MountainMeat_Jacket_Brown
                SosigAccessory_MountainMeat_Jacket_Flannel
                SosigAccessory_MountainMeat_Jacket_Green
                SosigAccessory_MountainMeat_Jacket_Jean
                SosigAccessory_MountainMeat_Jacket_Orange
                SosigCasualTrackSuitTorsoBlack
                SosigCasualTrackSuitTorsoBlue
                SosigCasualTrackSuitTorsoGreen
                SosigCasualTrackSuitTorsoGrey
                SosigCasualTrackSuitTorsoOrange
                SosigCasualTrackSuitTorsoPink
                SosigCasualTrackSuitTorsoPurple
                SosigCasualTrackSuitTorsoRed
                SosigCasualTrackSuitTorsoTurquoise
                SosigCasualTrackSuitTorsoYellow
                SosigCasualTrackSuitTopColorSet1
                SosigCasualTrackSuitTopColorSet2
                SosigCasualTrackSuitTopColorSet3
                SosigCasualTrackSuitTopColorSet4
                SosigCasualTrackSuitTopColorSet5
                SosigCasualTrackSuitTopColorSet6
                SosigCasualTrackSuitTopColorSet7
                SosigCasualTrackSuitTopColorSet8
                SosigCasualTrackSuitTopStripedColorSet1
                SosigCasualTrackSuitTopStripedColorSet2
                SosigCasualTrackSuitTopStripedColorSet3
                SosigCasualTrackSuitTopStripedColorSet4
                SosigCasualTrackSuitTopStripedColorSet5
                SosigCasualTrackSuitTopStripedColorSet6
                SosigCasualTrackSuitTopStripedColorSet7
                SosigCasualTrackSuitTopStripedColorSet8
                SosigAccessory_MountainMeat_Undershirt_Blue
                SosigAccessory_MountainMeat_Undershirt_Brown
                SosigAccessory_MountainMeat_Undershirt_Green
                SosigAccessory_MountainMeat_Undershirt_Tan
                SosigAccessory_MountainMeat_Undershirt_White
                SosigCasualUndershirtCamoDesert
                SosigCasualUndershirtCamoForest
                SosigCasualUndershirtCamoNight
                SosigCasualUndershirtCamoPink
                SosigCasualUndershirtCamoSky
                SosigCasualUndershirtCamoUrban
                SosigCasualHoodieHoodDownBlack
                SosigCasualHoodieHoodDownBlue
                SosigCasualHoodieHoodDownCream
                SosigCasualHoodieHoodDownForest
                SosigCasualHoodieHoodDownGreen
                SosigCasualHoodieHoodDownLavendar
                SosigCasualHoodieHoodDownMustard
                SosigCasualHoodieHoodDownOlive
                SosigCasualHoodieHoodDownPeach
                SosigCasualHoodieHoodDownRed
                SosigCasualHoodieHoodDownWhite
                SosigCasualHoodieHoodDownYellow
                */
                /* Pants
                SosigAccessory_MountainMeat_Pants_Black
                SosigAccessory_MountainMeat_Pants_Brown
                SosigAccessory_MountainMeat_Pants_Green
                SosigAccessory_MountainMeat_Pants_Grey
                SosigAccessory_MountainMeat_Pants_JeanDark
                SosigAccessory_MountainMeat_Pants_JeanNew
                SosigAccessory_MountainMeat_Pants_JeanWorn
                SosigAccessory_MountainMeat_Pants_LightGrey
                SosigAccessory_MountainMeat_Pants_Olive
                SosigAccessory_MountainMeat_Pants_Tan
                SosigAccessory_MountainMeat_Pants_Umber
                SosigCasualShortsBlack
                SosigCasualShortsBrown
                SosigCasualShortsGreen
                SosigCasualShortsGrey
                SosigCasualShortsJeanDark
                SosigCasualShortsJeanNew
                SosigCasualShortsJeanWorn
                SosigCasualShortsLightGrey
                SosigCasualShortsOlive
                SosigCasualShortsTan
                SosigCasualShortsUmber
                SosigCasualTrackSuitAbdoBlack
                SosigCasualTrackSuitAbdoBlue
                SosigCasualTrackSuitAbdoGreen
                SosigCasualTrackSuitAbdoGrey
                SosigCasualTrackSuitAbdoOrange
                SosigCasualTrackSuitAbdoPink
                SosigCasualTrackSuitAbdoPurple
                SosigCasualTrackSuitAbdoRed
                SosigCasualTrackSuitAbdoTurquoise
                SosigCasualTrackSuitAbdoYellow
                */
                /* PantsLower
                SosigCasualTrackSuitLegBlack
                SosigCasualTrackSuitLegBlue
                SosigCasualTrackSuitLegGreen
                SosigCasualTrackSuitLegGrey
                SosigCasualTrackSuitLegOrange
                SosigCasualTrackSuitLegPink
                SosigCasualTrackSuitLegPurple
                SosigCasualTrackSuitLegRed
                SosigCasualTrackSuitLegTurquoise
                SosigCasualTrackSuitLegYellow
                */
            }

            return outfitConfig;
        }
    }
}
