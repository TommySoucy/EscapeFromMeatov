﻿using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class ContainmentVolume : MonoBehaviour
    {
        public bool hasMaxVolume;
        public int maxVolume;
        [NonSerialized]
        private int _volume;
        public int volume
        {
            get { return _volume; }
            set
            {
                int preValue = _volume;
                _volume = value;
                if(preValue != _volume)
                {
                    OnVolumeChangedInvoke();
                }
            }
        }
        public List<string> whitelist;
        public List<string> blacklist;
        public GameObject staticVolume;
        public GameObject activeVolume;
        public GameObject itemRoot;
        [NonSerialized]
        public Dictionary<string, int> inventory = new Dictionary<string, int>();
        [NonSerialized]
        public Dictionary<string, List<MeatovItem>> inventoryItems = new Dictionary<string, List<MeatovItem>>();
        [NonSerialized]
        public Dictionary<string, int> FIRInventory = new Dictionary<string, int>();
        [NonSerialized]
        public Dictionary<string, List<MeatovItem>> FIRInventoryItems = new Dictionary<string, List<MeatovItem>>();

        public delegate void OnItemAddedDelegate(MeatovItem item);
        public event OnItemAddedDelegate OnItemAdded;
        public delegate void OnItemRemovedDelegate(MeatovItem item);
        public event OnItemRemovedDelegate OnItemRemoved;
        public delegate void OnItemStackChangedDelegate(MeatovItem item, int difference);
        public event OnItemStackChangedDelegate OnItemStackChanged;
        public delegate void OnVolumeChangedDelegate();
        public event OnVolumeChangedDelegate OnVolumeChanged;

        public bool AddItem(MeatovItem item)
        {
            staticVolume.SetActive(true);
            activeVolume.SetActive(false);

            bool fits = Mod.IDDescribedInList(item.tarkovID, item.parents, whitelist, blacklist) && (!hasMaxVolume || item.volumes[item.mode] <= (maxVolume - volume));
            if (fits)
            {
                item.physObj.SetAllCollidersToLayer(false, "NoCol");
                item.physObj.StoreAndDestroyRigidbody();
                item.physObj.SetParentage(itemRoot.transform);
                if (item.physObj.IsAltHeld)
                {
                    item.physObj.IsAltHeld = false;
                }
                if (item.physObj.AltGrip != null)
                {
                    item.physObj.AltGrip.m_hand.EndInteractionIfHeld(item.physObj.AltGrip);
                    item.physObj.AltGrip.EndInteraction(item.physObj.AltGrip.m_hand);
                }

                volume += item.volumes[item.mode];

                int count = 0;
                if(inventory.TryGetValue(item.tarkovID, out count))
                {
                    inventory[item.tarkovID] = count + item.stack;
                }
                else
                {
                    inventory.Add(item.tarkovID, item.stack);
                }
                if(inventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> items))
                {
                    items.Add(item);
                }
                else
                {
                    inventoryItems.Add(item.tarkovID, new List<MeatovItem>() { item });
                }

                if (item.foundInRaid)
                {
                    if (FIRInventory.TryGetValue(item.tarkovID, out count))
                    {
                        FIRInventory[item.tarkovID] = count + item.stack;
                    }
                    else
                    {
                        FIRInventory.Add(item.tarkovID, item.stack);
                    }
                    if (FIRInventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> FIRItems))
                    {
                        FIRItems.Add(item);
                    }
                    else
                    {
                        FIRInventoryItems.Add(item.tarkovID, new List<MeatovItem>() { item });
                    }
                }

                item.parentVolume = this;

                OnItemAddedInvoke(item);
            }
            return fits;
        }

        public void RemoveItem(MeatovItem item)
        {
            if (inventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> items) && items.Remove(item))
            {
                if(inventoryItems.Count == 0)
                {
                    inventoryItems.Remove(item.tarkovID);
                }
                inventory[item.tarkovID] -= item.stack;
                if(inventory[item.tarkovID] <= 0)
                {
                    inventory.Remove(item.tarkovID);
                }

                if (item.foundInRaid && FIRInventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> FIRItems) && FIRItems.Remove(item))
                {
                    if (FIRInventoryItems.Count == 0)
                    {
                        FIRInventoryItems.Remove(item.tarkovID);
                    }
                    FIRInventory[item.tarkovID] -= item.stack;
                    if (FIRInventory[item.tarkovID] <= 0)
                    {
                        FIRInventory.Remove(item.tarkovID);
                    }
                }

                volume -= item.volumes[item.mode];
                item.parentVolume = null;
                item.physObj.RecoverRigidbody();
                item.physObj.SetAllCollidersToLayer(false, "Default");

                OnItemRemovedInvoke(item);
            }
        }

        public void AdjustStack(MeatovItem item, int difference)
        {
            if(inventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> items) && items.Contains(item))
            {
                inventory[item.tarkovID] += difference;

                if(item.foundInRaid && FIRInventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> FIRItems) && FIRItems.Contains(item))
                {
                    FIRInventory[item.tarkovID] += difference;
                }

                OnItemStackChangedInvoke(item, difference);
            }
        }

        public void AddItemFIR(MeatovItem item)
        {
            if (item.foundInRaid)
            {
                int count = 0;
                if (FIRInventory.TryGetValue(item.tarkovID, out count))
                {
                    FIRInventory[item.tarkovID] = count + item.stack;
                }
                else
                {
                    FIRInventory.Add(item.tarkovID, item.stack);
                }
                if (FIRInventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> FIRItems))
                {
                    FIRItems.Add(item);
                }
                else
                {
                    FIRInventoryItems.Add(item.tarkovID, new List<MeatovItem>() { item });
                }
            }
        }

        public void RemoveItemFIR(MeatovItem item)
        {
            if (item.foundInRaid && FIRInventoryItems.TryGetValue(item.tarkovID, out List<MeatovItem> FIRItems) && FIRItems.Remove(item))
            {
                if (FIRInventoryItems.Count == 0)
                {
                    FIRInventoryItems.Remove(item.tarkovID);
                }
                FIRInventory[item.tarkovID] -= item.stack;
                if (FIRInventory[item.tarkovID] <= 0)
                {
                    FIRInventory.Remove(item.tarkovID);
                }
            }
        }

        public void SpawnItem(MeatovItemData itemData, int amount, bool foundInRaid = false)
        {
            int amountToSpawn = amount;
            float xSize = transform.localScale.x;
            float ySize = transform.localScale.y;
            float zSize = transform.localScale.z;
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

                    // Add item to volume
                    AddItem(meatovItem);

                    spawnedItem.transform.localPosition = new Vector3(UnityEngine.Random.Range(-xSize / 2, xSize / 2),
                                                                      UnityEngine.Random.Range(-ySize / 2, ySize / 2),
                                                                      UnityEngine.Random.Range(-zSize / 2, zSize / 2));
                    spawnedItem.transform.localRotation = UnityEngine.Random.rotation;
                }
            }
        }

        public IEnumerator SpawnVanillaItem(MeatovItemData itemData, int count, bool foundInRaid = false)
        {
            yield return IM.OD[itemData.H3ID].GetGameObjectAsync();
            GameObject itemPrefab = IM.OD[itemData.H3ID].GetGameObject();
            if (itemPrefab == null)
            {
                Mod.LogError("Failed to get vanilla prefab for "+itemData.tarkovID+":" + itemData.H3ID + " to spawn in containment volume "+name);
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
                    meatovItem.SetData(itemData);
                    meatovItem.foundInRaid = foundInRaid;
                    FVRFireArmMagazine asMagazine = meatovItem.physObj as FVRFireArmMagazine;
                    FVRFireArmRound round = physObj as FVRFireArmRound;
                    asMagazine.RoundType = round.RoundType;
                    meatovItem.roundClass = round.RoundClass;
                    for (int j = 0; j < amount; ++j)
                    {
                        asMagazine.AddRound(meatovItem.roundClass, false, false);
                    }

                    // Add item to volume
                    AddItem(meatovItem);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-activeVolume.transform.localScale.x / 2, activeVolume.transform.localScale.x / 2),
                                                                        UnityEngine.Random.Range(-activeVolume.transform.localScale.y / 2, activeVolume.transform.localScale.y / 2),
                                                                        UnityEngine.Random.Range(-activeVolume.transform.localScale.z / 2, activeVolume.transform.localScale.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;

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

                    // Add item to volume
                    AddItem(meatovItem);

                    itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-activeVolume.transform.localScale.x / 2, activeVolume.transform.localScale.x / 2),
                                                                     UnityEngine.Random.Range(-activeVolume.transform.localScale.y / 2, activeVolume.transform.localScale.y / 2),
                                                                     UnityEngine.Random.Range(-activeVolume.transform.localScale.z / 2, activeVolume.transform.localScale.z / 2));
                    itemObject.transform.localRotation = UnityEngine.Random.rotation;
                }
            }

            yield break;
        }

        public bool Offer(MeatovItem item)
        {
            bool fits = Mod.IDDescribedInList(item.tarkovID, item.parents, whitelist, blacklist) && (!hasMaxVolume || item.volumes[item.mode] <= (maxVolume - volume));
            Mod.LogInfo("\t\tItem " + item.itemName + " offered to volume " + name+", fits: "+ fits);
            staticVolume.SetActive(!fits);
            activeVolume.SetActive(fits);
            return fits;
        }

        public void Unoffer()
        {
            staticVolume.SetActive(true);
            activeVolume.SetActive(false);
        }

        public void OnItemAddedInvoke(MeatovItem item)
        {
            if(OnItemAdded != null)
            {
                OnItemAdded(item);
            }
        }

        public void OnItemRemovedInvoke(MeatovItem item)
        {
            if(OnItemRemoved != null)
            {
                OnItemRemoved(item);
            }
        }

        public void OnItemStackChangedInvoke(MeatovItem item, int difference)
        {
            if(OnItemStackChanged != null)
            {
                OnItemStackChanged(item, difference);
            }
        }

        public void OnVolumeChangedInvoke()
        {
            if(OnVolumeChanged != null)
            {
                OnVolumeChanged();
            }
        }
    }
}
