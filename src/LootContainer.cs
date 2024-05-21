using FistVR;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class LootContainer : MonoBehaviour
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
			Mod.LogInfo("\tInitializing loot container: " + name);
			// Apply 10% chance of empty container
			if(UnityEngine.Random.value <= 0.1 || spawnFilter == null || spawnFilter.Count == 0)
			{
				Mod.LogInfo("\t\tLoot container empty");
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
			for (int i=0; i < 20 + (UnityEngine.Random.value <= Mod.skills[30].currentProgress / 10000 ? 2 : 0) + (UnityEngine.Random.value <= Mod.skills[9].currentProgress / 10000 ? 2 : 0); ++i) // 20 spawn attempts + chance of 2 more if high enough search skill and also for attention skill 
            {
				string randomFilterID = spawnFilter[UnityEngine.Random.Range(0, spawnFilter.Count)];
				string itemID = "";
    //            if (Mod.itemsByParents.TryGetValue(randomFilterID, out List<string> possibleItems))
    //            {
				//	itemID = possibleItems[UnityEngine.Random.Range(0, possibleItems.Count)];
    //            }
    //            else if(Mod.itemMap.ContainsKey(randomFilterID))
    //            {
    //                itemID = Mod.TarkovIDtoH3ID(randomFilterID);
    //            }
    //            else
    //            {
    //                // Spawn random round instead
    //                itemID = Mod.usedRoundIDs[UnityEngine.Random.Range(0, Mod.usedRoundIDs.Count - 1)];
    //                continue;
				//}

				if (int.TryParse(itemID, out int result))
                {
					MeatovItem prefabCIW = Mod.GetItemPrefab(result).GetComponent<MeatovItem>();

					if(UnityEngine.Random.value <= Mod.GetRaritySpawnChanceMultiplier(prefabCIW.rarity))
                    {
						++successfulAttempts;

						customIDs.Add(result);

						if (prefabCIW.itemType == MeatovItem.ItemType.AmmoBox)
                        {
							stackSizes.Add(-1); // -1 indicates maxAmount, actual ammo boxes should always spawn with max amount in them
                        }
                    }
                }
                else
				{
					// If loose round stack, spawn generic ammo box instead
					//if (Mod.usedRoundIDs.Contains(itemID))
					//{
					//	int stackSize = UnityEngine.Random.Range(15, 120);
					//	int actualItemID;
					//	if(stackSize <= 30)
					//	{
					//		actualItemID = 715;
     //                   }
     //                   else
					//	{
					//		actualItemID = 716;
					//	}

					//	//if (UnityEngine.Random.value <= Mod.GetRaritySpawnChanceMultiplier(prefabVID.rarity))
					//	//{
					//	//	++successfulAttempts;

					//	//	customIDs.Add(actualItemID);

					//	//	stackSizes.Add(stackSize);
					//	//	FVRFireArmRound roundScript = IM.OD[itemID].GetGameObject().GetComponent<FVRFireArmRound>();
					//	//	roundClasses.Add(roundScript.RoundClass);
					//	//	roundTypes.Add(roundScript.RoundType);
					//	//}
					//}
     //               else
					//{
					//	//if (UnityEngine.Random.value <= Mod.GetRaritySpawnChanceMultiplier(prefabVID.rarity) / 100)
					//	//{
					//	//	++successfulAttempts;

					//	//	vanillaIDs.Add(itemID);
					//	//}
					//}
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
					Mod.AddSkillExp(Skill.findAction, 30);
					Mod.AddSkillExp(Skill.findActionTrue, 9);

					int itemID = customIDs[customIDs.Count - 1];
					customIDs.RemoveAt(customIDs.Count - 1);
					GameObject itemObject = Instantiate(Mod.GetItemPrefab(itemID), itemObjectsRoot);

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
					itemObject.transform.localRotation = UnityEngine.Random.rotation;
					MeatovItem itemCIW = itemObject.GetComponent<MeatovItem>();
					itemCIW.foundInRaid = true;

					// When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
					Mod.RemoveFromAll(null, itemCIW);

					if (itemCIW.itemType == MeatovItem.ItemType.Money)
					{
						itemCIW.stack = UnityEngine.Random.Range(120, 2500);
					}
					else if (itemCIW.itemType == MeatovItem.ItemType.AmmoBox)
					{
						int stackSize = stackSizes[stackSizes.Count - 1];
						stackSizes.RemoveAt(stackSizes.Count - 1);
						int actualStackSize = stackSize == -1 ? itemCIW.maxAmount : stackSize;
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
				Mod.AddSkillExp(Skill.findAction, 30);
				Mod.AddSkillExp(Skill.findActionTrue, 9);
				yield return IM.OD[vanillaID].GetGameObjectAsync();
				GameObject itemPrefab = IM.OD[vanillaID].GetGameObject();
				if(itemPrefab == null)
				{
					Mod.LogWarning("Attempted to get vanilla prefab for " + vanillaID + ", but the prefab had been destroyed, refreshing cache...");

					IM.OD[vanillaID].RefreshCache();
					itemPrefab = IM.OD[vanillaID].GetGameObject();
				}
				if (itemPrefab == null)
				{
					Mod.LogError("Attempted to get vanilla prefab for " + vanillaID + ", but the prefab had been destroyed, refreshing cache did nothing");
					continue;
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

				//VanillaItemDescriptor VID = itemObject.GetComponent<VanillaItemDescriptor>();
				//VID.foundInRaid = true;
				//if(itemObject.GetComponent<FVRInteractiveObject>() is FVRFireArm)
				//{
				//	// When instantiated, the interactive object awoke and got added to All, we need to remove it because we want to handle that ourselves
				//	Mod.RemoveFromAll(null, null, VID);
				//}
			}
			yield break;
		}
	}
}
