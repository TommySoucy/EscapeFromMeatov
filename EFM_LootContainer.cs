using FistVR;
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
		private List<FireArmRoundClass> roundClasses;
		private List<FireArmRoundType> roundTypes;
		private bool spawnCustomItems;
		public FVRInteractiveObject interactable;
		public Collider mainContainerCollider;
		public Transform itemObjectsRoot;
		public bool shouldSpawnItems;

		public void Init(List<string> spawnFilter, int maxSuccessfulAttempts)
		{
			Mod.instance.LogInfo("\tInitializing loot container: " + name);
			// Apply 10% chance of empty container
			if(UnityEngine.Random.value <= 0.1 || spawnFilter == null || spawnFilter.Count == 0)
			{
				Mod.instance.LogInfo("\t\tLoot container empty");
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
			roundClasses = new List<FireArmRoundClass>();
			roundTypes = new List<FireArmRoundType>();
			itemObjectsRoot = transform.GetChild(transform.childCount - 2);

			// Set vanillaIDs and customIDs lists with ID of items to spawn when opened
			int successfulAttempts = 0;
			Mod.instance.LogInfo("\tSpawning attempts...");
			for (int i=0; i < 20 + (UnityEngine.Random.value <= Mod.skills[30].currentProgress / 10000 ? 2 : 0) + (UnityEngine.Random.value <= Mod.skills[9].currentProgress / 10000 ? 2 : 0); ++i) // 20 spawn attempts + chance of 2 more if high enough search skill and also for attention skill 
            {
				string randomFilterID = spawnFilter[UnityEngine.Random.Range(0, spawnFilter.Count - 1)];
				Mod.instance.LogInfo("\t\trandomFilterID: "+ randomFilterID);
				string itemID = "";
                if (Mod.itemsByParents.TryGetValue(randomFilterID, out List<string> possibleItems))
                {
					itemID = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count - 1)];
                }
                else if(Mod.itemMap.ContainsKey(randomFilterID))
                {
					ItemMapEntry entry = Mod.itemMap[randomFilterID];
					switch (entry.mode)
					{
						case 0:
							itemID = entry.ID;
							break;
						case 1:
							itemID = entry.modulIDs[UnityEngine.Random.Range(0, entry.modulIDs.Length)];
							break;
						case 2:
							itemID = entry.otherModID;
							break;
					}
                }
                else
                {
					// TODO: If we have no item of a particular ancestor implemented, we will come here. 
					// Maybe in this case we want to spawn a single random round instead
					Mod.instance.LogError("\t\tLoot container has spawn filter ID: " + randomFilterID + " not present in both itemMap and parents dict.");
					continue;
				}
				Mod.instance.LogInfo("\t\titemID: " + itemID);

				if (int.TryParse(itemID, out int result))
                {
					EFM_CustomItemWrapper prefabCIW = Mod.itemPrefabs[result].GetComponent<EFM_CustomItemWrapper>();

					if(UnityEngine.Random.value <= prefabCIW.spawnChance / 100)
                    {
						++successfulAttempts;

						customIDs.Add(result);

						Mod.instance.LogInfo("\t\t"+ result);

						if (prefabCIW.itemType == Mod.ItemType.AmmoBox)
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

							Mod.instance.LogInfo("\t\t" + actualItemID);

							stackSizes.Add(stackSize);
							FVRFireArmRound roundScript = IM.OD[itemID].GetGameObject().GetComponent<FVRFireArmRound>();
							roundClasses.Add(roundScript.RoundClass);
							roundTypes.Add(roundScript.RoundType);
						}
					}
                    else
					{
						if (UnityEngine.Random.value <= prefabVID.spawnChance / 100)
						{
							++successfulAttempts;

							vanillaIDs.Add(itemID);

							Mod.instance.LogInfo("\t\t" + itemID);
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
			if (m_containsItems && !m_hasSpawnedContents && ((interactable != null && interactable.IsHeld) || shouldSpawnItems))
			{
				m_hasSpawnedContents = true;
				spawnCustomItems = true;
				AnvilManager.Run(SpawnVanillaItems());

				mainContainerCollider.gameObject.SetActive(true);
				itemObjectsRoot.gameObject.SetActive(true);
			}

            if (spawnCustomItems)
            {
				if(customIDs.Count > 0)
                {
					Mod.AddSkillExp(EFM_Skill.findAction, 30);
					Mod.AddSkillExp(EFM_Skill.findActionTrue, 9);

					int itemID = customIDs[customIDs.Count - 1];
					customIDs.RemoveAt(customIDs.Count - 1);
					GameObject itemObject = Instantiate(Mod.itemPrefabs[itemID], itemObjectsRoot);

					itemObject.GetComponent<Rigidbody>().isKinematic = true;
					if (mainContainerCollider is BoxCollider)
					{
						BoxCollider boxCollider = mainContainerCollider as BoxCollider;
						itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x * mainContainerCollider.transform.localScale.x / 2, boxCollider.size.x * mainContainerCollider.transform.localScale.x / 2),
																		 UnityEngine.Random.Range(-boxCollider.size.y * mainContainerCollider.transform.localScale.y / 2, boxCollider.size.y * mainContainerCollider.transform.localScale.y / 2),
																		 UnityEngine.Random.Range(-boxCollider.size.z * mainContainerCollider.transform.localScale.z / 2, boxCollider.size.z * mainContainerCollider.transform.localScale.z / 2));
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
					itemCIW.foundInRaid = true;

					// When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
					Mod.RemoveFromAll(null, itemCIW, null);

					if (itemCIW.itemType == Mod.ItemType.Money)
					{
						itemCIW.stack = UnityEngine.Random.Range(120, 2500);
					}
					else if (itemCIW.itemType == Mod.ItemType.AmmoBox)
					{
						int stackSize = stackSizes[stackSizes.Count - 1];
						Mod.instance.LogInfo("Spawning ammo box with stack size: " + stackSize);
						stackSizes.RemoveAt(stackSizes.Count - 1);
						int actualStackSize = stackSize == -1 ? itemCIW.maxAmount : stackSize;
						Mod.instance.LogInfo("actualStackSize: " + actualStackSize);
						FVRFireArmMagazine asMagazine = itemCIW.physObj as FVRFireArmMagazine;
						if(itemID == 715 || itemID == 716)
                        {
							asMagazine.RoundType = roundTypes[roundTypes.Count - 1];
							itemCIW.roundClass = roundClasses[roundClasses.Count - 1];
							roundTypes.RemoveAt(roundTypes.Count - 1);
							roundClasses.RemoveAt(roundClasses.Count - 1);
						}
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
				Mod.AddSkillExp(EFM_Skill.findAction, 30);
				Mod.AddSkillExp(EFM_Skill.findActionTrue, 9);
				yield return IM.OD[vanillaID].GetGameObjectAsync();
				GameObject itemPrefab = IM.OD[vanillaID].GetGameObject();
				if(itemPrefab == null)
				{
					Mod.instance.LogWarning("Attempted to get vanilla prefab for " + vanillaID + ", but the prefab had been destroyed, refreshing cache...");

					IM.OD[vanillaID].RefreshCache();
					do
					{
						Mod.instance.LogInfo("Waiting for cache refresh...");
						itemPrefab = IM.OD[vanillaID].GetGameObject();
					} while (itemPrefab == null);
				}
				GameObject itemObject = Instantiate(itemPrefab, itemObjectsRoot);
				itemObject.GetComponent<Rigidbody>().isKinematic = true;
				if (mainContainerCollider is BoxCollider)
				{
					BoxCollider boxCollider = mainContainerCollider as BoxCollider;
					itemObject.transform.localPosition = new Vector3(UnityEngine.Random.Range(-boxCollider.size.x * mainContainerCollider.transform.localScale.x / 2, boxCollider.size.x * mainContainerCollider.transform.localScale.x / 2),
																	 UnityEngine.Random.Range(-boxCollider.size.y * mainContainerCollider.transform.localScale.y / 2, boxCollider.size.y * mainContainerCollider.transform.localScale.y / 2),
																	 UnityEngine.Random.Range(-boxCollider.size.z * mainContainerCollider.transform.localScale.z / 2, boxCollider.size.z * mainContainerCollider.transform.localScale.z / 2));
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

				EFM_VanillaItemDescriptor VID = itemObject.GetComponent<EFM_VanillaItemDescriptor>();
				VID.foundInRaid = true;
				if(itemObject.GetComponent<FVRInteractiveObject>() is FVRFireArm)
				{
					// When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
					Mod.RemoveFromAll(null, null, VID);
				}
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
