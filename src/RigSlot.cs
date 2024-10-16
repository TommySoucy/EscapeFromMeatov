﻿using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class RigSlot : FVRQuickBeltSlot
    {
        public MeatovItem ownerItem;

        public void SpawnItem(MeatovItemData itemData, int amount, bool foundInRaid = false)
        {
            int amountToSpawn = amount;
            if (itemData.index == -1)
            {
                // Spawn vanilla item will handle the updating of proper elements
                AnvilManager.Run(SpawnVanillaItem(itemData, amountToSpawn, foundInRaid));
            }
            else
            {
                GameObject itemPrefab = Mod.GetItemPrefab(itemData.index);
                List<GameObject> objectsList = new List<GameObject>();
                while (amountToSpawn > 0)
                {
                    GameObject spawnedItem = Instantiate(itemPrefab);
                    MeatovItem meatovItem = spawnedItem.GetComponent<MeatovItem>();
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;
                    objectsList.Add(spawnedItem);

                    // Set stack and remove amount to spawn
                    if (meatovItem.maxStack > 1)
                    {
                        if (amountToSpawn > meatovItem.maxStack)
                        {
                            meatovItem.stack = meatovItem.maxStack;
                            amountToSpawn -= meatovItem.maxStack;
                        }
                        else // amountToSpawn <= itemCIW.maxStack
                        {
                            meatovItem.stack = amountToSpawn;
                            amountToSpawn = 0;
                        }
                    }
                    else
                    {
                        --amountToSpawn;
                    }

                    // Remove current slot item if there already is one
                    // This probably shouldn't happen, we should assume that if a slot is output of an area,
                    // there is only one production and the production is limited to only one ready item at once
                    if (CurObject != null)
                    {
                        CurObject.SetQuickBeltSlot(null);
                    }

                    // Add item to QBS
                    meatovItem.physObj.SetQuickBeltSlot(this);
                    meatovItem.UpdateInventories();
                }
            }
        }

        public IEnumerator SpawnVanillaItem(MeatovItemData itemData, int count, bool foundInRaid = false)
        {
            yield return IM.OD[itemData.H3ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[itemData.H3ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.LogError("Failed to get vanilla prefab for " + itemData.tarkovID + ":" + itemData.H3ID + " to spawn in slot " + name);
                yield break;
            }
            FVRPhysicalObject physObj = itemPrefab.GetComponent<FVRPhysicalObject>();
            GameObject itemObject = null;
            if (physObj is FVRFireArmRound && count > 1) // Multiple rounds, must spawn an ammobox
            {
                int countLeft = count;
                float boxCountLeft = count / 120.0f;
                while (boxCountLeft > 0)
                {
                    int amount = 0;
                    if (countLeft > 30)
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(716));

                        if (countLeft <= 120)
                        {
                            amount = countLeft;
                            countLeft = 0;
                        }
                        else
                        {
                            amount = 120;
                            countLeft -= 120;
                        }
                    }
                    else
                    {
                        itemObject = GameObject.Instantiate(Mod.GetItemPrefab(715));

                        amount = countLeft;
                        countLeft = 0;
                    }

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();
                    meatovItem.foundInRaid = foundInRaid;
                    meatovItem.SetData(itemData);
                    FVRFireArmMagazine asMagazine = meatovItem.physObj as FVRFireArmMagazine;
                    FVRFireArmRound round = physObj as FVRFireArmRound;
                    asMagazine.RoundType = round.RoundType;
                    meatovItem.roundClass = round.RoundClass;
                    for (int j = 0; j < amount; ++j)
                    {
                        asMagazine.AddRound(meatovItem.roundClass, false, false);
                    }

                    // Remove current slot item if there already is one
                    // This probably shouldn't happen, we should assume that if a slot is output of an area,
                    // there is only one production and the production is limited to only one ready item at once
                    if (CurObject != null)
                    {
                        CurObject.SetQuickBeltSlot(null);
                    }

                    // Add item to QBS
                    meatovItem.physObj.SetQuickBeltSlot(this);
                    meatovItem.UpdateInventories();

                    boxCountLeft = countLeft / 120.0f;
                }
            }
            else // Not a round, or just 1, spawn as normal
            {
                for (int i = 0; i < count; ++i)
                {
                    itemObject = GameObject.Instantiate(itemPrefab);

                    MeatovItem meatovItem = itemObject.GetComponent<MeatovItem>();
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;

                    // Remove current slot item if there already is one
                    // This probably shouldn't happen, we should assume that if a slot is output of an area,
                    // there is only one production and the production is limited to only one ready item at once
                    if (CurObject != null)
                    {
                        CurObject.SetQuickBeltSlot(null);
                    }

                    // Add item to QBS
                    meatovItem.physObj.SetQuickBeltSlot(this);
                    meatovItem.UpdateInventories();
                }
            }

            yield break;
        }
    }
}
