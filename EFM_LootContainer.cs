﻿using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_LootContainer : MonoBehaviour
    {
		private bool m_containsItems;
		private bool m_hasSpawnedContents;
		private List<string> vanillaIDs;
		private List<int> customIDs;
		private List<int> stackSizes;
		private bool spawnCustomItems;
		public FVRInteractiveObject interactable;
		public Collider mainContainerCollider;

		public void Init(List<string> spawnFilter, int maxSuccessfulAttempts)
		{
			// Apply 10% chance of empty container
			if(UnityEngine.Random.value <= 0.1 || spawnFilter == null || spawnFilter.Count == 0)
            {
				m_containsItems = false;
				return;
            }
            else
            {
				m_containsItems = true;
            }

			vanillaIDs = new List<string>();
			customIDs = new List<int>();
			stackSizes = new List<int>();

			// Set vanillaIDs and customIDs lists with ID of items to spawn when opened
			int successfulAttempts = 0;
			for(int i=0; i < 20; ++i) // 20 spawn attempts
            {
				string randomFilterID = spawnFilter[UnityEngine.Random.Range(0, spawnFilter.Count - 1)];
				string itemID;
                if (Mod.itemsByParents.TryGetValue(randomFilterID, out List<string> possibleItems))
                {
					itemID = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count - 1)];
                }
                else if(Mod.itemMap.ContainsKey(randomFilterID))
                {
					itemID = Mod.itemMap[randomFilterID];
                }
                else
                {
					// TODO: If we have no item of a particular ancestor implemented, we will come here. 
					// Maybe in this case we want to spawn a single random round instead
					Mod.instance.LogError("Loot container has spawn filter ID: " + randomFilterID + " not present in both itemMap and parents dict.");
					continue;
                }

				if(int.TryParse(itemID, out int result))
                {
					EFM_CustomItemWrapper prefabCIW = Mod.itemPrefabs[result].GetComponent<EFM_CustomItemWrapper>();

					if(UnityEngine.Random.value <= prefabCIW.spawnChance / 100)
                    {
						++successfulAttempts;

						customIDs.Add(result);

						if(prefabCIW.itemType == Mod.ItemType.AmmoBox)
                        {
							stackSizes.Add(-1); // -1 indicates maxAmount, actual ammo boxes should always spawn with max amount in them
                        }
                    }
                }
                else
				{
					EFM_VanillaItemDescriptor prefabVID = Mod.vanillaItems[itemID];

					// If loose round stack, spawn generic ammo box instead
					if (Mod.usedRoundIDs.Contains(itemID))
					{
						int stackSize = UnityEngine.Random.Range(15, 120);
						int actualItemID;
						if(stackSize <= 30)
						{
							actualItemID = 715;
                        }
                        else
						{
							actualItemID = 716;
						}

						if (UnityEngine.Random.value <= prefabVID.spawnChance / 100)
						{
							++successfulAttempts;

							customIDs.Add(actualItemID);

							stackSizes.Add(stackSize);
						}
					}
                    else
					{
						if (UnityEngine.Random.value <= prefabVID.spawnChance / 100)
						{
							++successfulAttempts;

							vanillaIDs.Add(itemID);
						}
					}
				}

				// Limit successful item spawn count to number of cells in container
				if(successfulAttempts == maxSuccessfulAttempts)
                {
					break;
                }
            }
		}

		private void Update()
		{
			if (m_containsItems && interactable.IsHeld && !m_hasSpawnedContents)
			{
				m_hasSpawnedContents = true;
				spawnCustomItems = true;
				AnvilManager.Run(SpawnVanillaItems());
			}

            if (spawnCustomItems)
            {
				if(customIDs.Count > 0)
                {
					int itemID = customIDs[customIDs.Count - 1];
					customIDs.RemoveAt(customIDs.Count - 1);
					GameObject itemObject = Instantiate(Mod.itemPrefabs[itemID], mainContainerCollider.transform);
					if (mainContainerCollider is BoxCollider)
					{
						BoxCollider boxCollider = mainContainerCollider as BoxCollider;
						itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
																		 UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
																		 UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
					}
					else
					{
						CapsuleCollider capsuleCollider = mainContainerCollider as CapsuleCollider;
						if (capsuleCollider != null)
						{
							itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-capsuleCollider.radius / 2, capsuleCollider.radius / 2),
																			 UnityEngine.Random.Range(-(capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.height / 2 - capsuleCollider.radius),
																			 0);
						}
						else
						{
							itemObject.transform.localPosition = Vector3.zero;
						}
					}
					itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
					EFM_CustomItemWrapper itemCIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
					if (itemCIW.itemType == Mod.ItemType.Money)
					{
						itemCIW.stack = UnityEngine.Random.Range(120, 2500);
					}
					else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
					{
						int stackSize = stackSizes[stackSizes.Count - 1];
						int actualStackSize = stackSize == -1 ? itemCIW.maxAmount : stackSize;
						FVRFireArmMagazine asMagazine = itemCIW.physObj as FVRFireArmMagazine;
						for (int j = 0; j < actualStackSize; ++j)
						{
							asMagazine.AddRound(itemCIW.roundClass, false, false);
						}
					}
					else if (itemCIW.maxAmount > 0)
					{
						itemCIW.amount = itemCIW.maxAmount;
					}
				}
                else
                {
					spawnCustomItems = false;
				}
            }
		}

		private IEnumerator SpawnVanillaItems()
		{
			foreach(string vanillaID in vanillaIDs)
			{
				yield return IM.OD[vanillaID].GetGameObjectAsync();
				GameObject itemObject = Instantiate(IM.OD[vanillaID].GetGameObject(), mainContainerCollider.transform);
				if (mainContainerCollider is BoxCollider)
				{
					BoxCollider boxCollider = mainContainerCollider as BoxCollider;
					itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x / 2, boxCollider.size.x / 2),
																	 UnityEngine.Random.Range(-boxCollider.size.y / 2, boxCollider.size.y / 2),
																	 UnityEngine.Random.Range(-boxCollider.size.z / 2, boxCollider.size.z / 2));
				}
				else
				{
					CapsuleCollider capsuleCollider = mainContainerCollider as CapsuleCollider;
					if (capsuleCollider != null)
					{
						itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-capsuleCollider.radius / 2, capsuleCollider.radius / 2),
																		 UnityEngine.Random.Range(-(capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.height / 2 - capsuleCollider.radius),
																		 0);
					}
					else
					{
						itemObject.transform.localPosition = Vector3.zero;
					}
				}
				itemObject.transform.localEulerAngles = new Vector3(UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f), UnityEngine.Random.Range(0.0f, 180f));
			}
			yield break;
		}

		private static float GetRaritySpawnChanceMultiplier(Mod.ItemRarity rarity)
        {
            switch (rarity)
            {
				case Mod.ItemRarity.Common:
					return 1;
				case Mod.ItemRarity.Rare:
					return 0.7f;
				case Mod.ItemRarity.Superrare:
					return 0.4f;
				case Mod.ItemRarity.Not_exist:
					return 0;
				default:
					return 1;
            }
        }
	}
}
