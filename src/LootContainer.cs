﻿using H3MP.Networking;
using H3MP;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class LootContainer : MonoBehaviour
    {
        public static readonly bool SPAWN_ALL_LOOT = true;

        public enum Mode
        {
            StaticLootData,
            SpecificItems,
            Rarity,
            Category
        }

        public ContainmentVolume volume;
        public Lock lockScript;
        public GameObject volumeRoot; // Only used if this is a togglable container

        public float presenceProbability; // Probability of this loot container to even exist
        public bool togglable; // Should be true if no slider or cover
        public int spawnAttemptCount; // Number of attempts to make of spawning an item in this loot container, overridden by StaticLootData mode
        public int maxItemCount = -1; // -1 is unlimited
        public int maxVolume = -1; // -1 is unlimited, in ml

        public float emptyProbability; // Overridden by StaticLootData mode
        public Mode mode;

        // StaticLootData
        public string staticLootID; // ID of this loot container in database/loot/staticLoot.json

        // SpecificItems
        public int totalProbabilityWeight; // Total of all probability weight
        public List<string> items;
        public List<int> itemProbabilityWeight; // Spawn probability weight
        public List<int> itemStack; // Max stack

        // Rarity
        public MeatovItem.ItemRarity rarity;

        // Category
        public string category;

        [NonSerialized]
        public bool contentsSpawned;
        [NonSerialized]
        public bool spawnContents;
        [NonSerialized]
        public Queue<KeyValuePair<MeatovItemData, int>> itemsToSpawn;

        public TrackedLootContainerData trackedLootContainerData;

        public void Awake()
        {
            if (lockScript != null)
            {
                lockScript.OnUnlock += OnUnlock;
            }
        }

        public void OnUnlock(bool toSend)
        {
            if (toSend && trackedLootContainerData != null)
            {
                // Take control
                if (trackedLootContainerData.controller != GameManager.ID)
                {
                    trackedLootContainerData.TakeControlRecursive();
                }

                // Send unlock to others
                if (Networking.currentInstance != null)
                {
                    using (Packet packet = new Packet(Networking.unlockLootContainerPacketID))
                    {
                        packet.Write(trackedLootContainerData.trackedID);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
                }
            }
        }

        public void Start()
        {
            if(UnityEngine.Random.value > presenceProbability)
            {
                Destroy(gameObject);
            }
        }

        public void Update()
        {
            if (spawnContents)
            {
                if (SPAWN_ALL_LOOT)
                {
                    while(itemsToSpawn.Count > 0)
                    {
                        KeyValuePair<MeatovItemData, int> entry = itemsToSpawn.Dequeue();

                        volume.SpawnItem(entry.Key, entry.Value, true);

                        spawnContents = false;
                    }
                }
                else
                {
                    KeyValuePair<MeatovItemData, int> entry = itemsToSpawn.Dequeue();

                    volume.SpawnItem(entry.Key, entry.Value, true);

                    spawnContents = itemsToSpawn.Count > 0;
                }
            }
        }

        public void ToggleMode()
        {
            volumeRoot.SetActive(!volumeRoot.activeSelf);

            if (volumeRoot.activeSelf)
            {
                SpawnContents();
            }
        }

        public void SpawnContents()
        {
            if (contentsSpawned)
            {
                return;
            }

            contentsSpawned = true;

            // Check if empty
            if (mode != Mode.StaticLootData && UnityEngine.Random.value < emptyProbability)
            {
                return;
            }

            int totalVolume = 0;
            itemsToSpawn = new Queue<KeyValuePair<MeatovItemData, int>>();
            switch (mode)
            {
                case Mode.StaticLootData:
                    JObject staticLoot = JObject.Parse(File.ReadAllText(Mod.path + "/database/loot/staticLoot.json"));
                    JToken lootData = staticLoot[staticLootID];
                    long itemCountWeightTotal = 0;
                    JArray itemCountArray = lootData["itemcountDistribution"] as JArray;
                    for(int i=0; i < itemCountArray.Count; ++i)
                    {
                        itemCountWeightTotal += (int)itemCountArray[i]["relativeProbability"];
                    }
                    long randomItemCountSelection = LongRandom(0, itemCountWeightTotal, new System.Random());
                    long currentWeight = 0;
                    int itemCount = -1;
                    for (int i = 0; i < itemCountArray.Count; ++i)
                    {
                        currentWeight += (int)itemCountArray[i]["relativeProbability"];
                        if (randomItemCountSelection < currentWeight)
                        {
                            itemCount = Mathf.Min(maxItemCount, (int)itemCountArray[i]["count"]);
                            break;
                        }
                    }
                    long itemWeightTotal = 0;
                    JArray itemArray = lootData["itemDistribution"] as JArray;
                    for (int i = 0; i < itemArray.Count; ++i)
                    {
                        itemWeightTotal += (int)itemArray[i]["relativeProbability"];
                    }
                    for (int i=0; i < itemCount; ++i)
                    {
                        long randomItemSelection = LongRandom(0, itemCountWeightTotal, new System.Random());
                        long currentItemWeight = 0;
                        string item = null;
                        for (int j = 0; j < itemArray.Count; ++j)
                        {
                            currentItemWeight += (int)itemArray[j]["relativeProbability"];
                            if (randomItemSelection < currentItemWeight)
                            {
                                item = itemArray[j]["tpl"].ToString();
                                break;
                            }
                        }

                        if(Mod.defaultItemData.TryGetValue(item, out MeatovItemData itemData))
                        {
                            totalVolume += itemData.volumes[0];
                            int actualMaxStack = itemData.maxStack;
                            if (itemData.itemType == MeatovItem.ItemType.Round)
                            {
                                actualMaxStack = 30;
                            }
                            int stack = UnityEngine.Random.Range(1, actualMaxStack + 1);
                            itemsToSpawn.Enqueue(new KeyValuePair<MeatovItemData, int>(itemData, stack));
                        }

                        if(maxVolume != -1 && totalVolume >= maxVolume)
                        {
                            break;
                        }
                    }
                    break;
                case Mode.SpecificItems:
                    for (int i = 0; i < spawnAttemptCount; ++i)
                    {
                        long randomItemSelection = LongRandom(0, totalProbabilityWeight, new System.Random());
                        long currentItemWeight = 0;
                        string item = null;
                        int maxStack = 1;
                        for (int j = 0; j < items.Count; ++j)
                        {
                            currentItemWeight += itemProbabilityWeight[j];
                            if (randomItemSelection < currentItemWeight)
                            {
                                item = items[j];
                                maxStack = itemStack[j];
                                break;
                            }
                        }

                        if (Mod.defaultItemData.TryGetValue(item, out MeatovItemData itemData))
                        {
                            totalVolume += itemData.volumes[0];
                            itemsToSpawn.Enqueue(new KeyValuePair<MeatovItemData, int>(itemData, UnityEngine.Random.Range(1, maxStack + 1)));
                        }

                        if (maxVolume != -1 && totalVolume >= maxVolume)
                        {
                            break;
                        }
                    }
                    break;
                case Mode.Rarity:
                    for (int i = 0; i < spawnAttemptCount; ++i)
                    {
                        if (Mod.itemsByRarity.TryGetValue(rarity, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                        {
                            MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                            totalVolume += itemData.volumes[0];
                            int actualMaxStack = itemData.maxStack;
                            if (itemData.itemType == MeatovItem.ItemType.Round)
                            {
                                actualMaxStack = 30;
                            }
                            int stack = UnityEngine.Random.Range(1, actualMaxStack + 1);
                            itemsToSpawn.Enqueue(new KeyValuePair<MeatovItemData, int>(itemData, stack));
                        }

                        if (maxVolume != -1 && totalVolume >= maxVolume)
                        {
                            break;
                        }
                    }
                    break;
                case Mode.Category:
                    for (int i = 0; i < spawnAttemptCount; ++i)
                    {
                        if (Mod.itemsByParents.TryGetValue(category, out List<MeatovItemData> itemDatas) && itemDatas.Count > 0)
                        {
                            MeatovItemData itemData = itemDatas[UnityEngine.Random.Range(0, itemDatas.Count)];
                            totalVolume += itemData.volumes[0];
                            int actualMaxStack = itemData.maxStack;
                            if (itemData.itemType == MeatovItem.ItemType.Round)
                            {
                                actualMaxStack = 30;
                            }
                            int stack = UnityEngine.Random.Range(1, actualMaxStack + 1);
                            itemsToSpawn.Enqueue(new KeyValuePair<MeatovItemData, int>(itemData, stack));
                        }

                        if (maxVolume != -1 && totalVolume >= maxVolume)
                        {
                            break;
                        }
                    }
                    break;
            }

            spawnContents = itemsToSpawn.Count > 0;

            if(trackedLootContainerData != null)
            {
                // Take control
                if (trackedLootContainerData.controller != GameManager.ID)
                {
                    trackedLootContainerData.TakeControlRecursive();
                }

                // Send breach to others
                if (Networking.currentInstance != null)
                {
                    using (Packet packet = new Packet(Networking.setLootContainerContentSpawnedPacketID))
                    {
                        packet.Write(trackedLootContainerData.trackedID);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
                }
            }
        }

        public long LongRandom(long min, long max, System.Random rand)
        {
            byte[] buf = new byte[8];
            rand.NextBytes(buf);
            long longRand = BitConverter.ToInt64(buf, 0);

            return (Math.Abs(longRand % (max - min)) + min);
        }
    }
}
