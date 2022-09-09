using FistVR;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
	public class EFM_CustomItemWrapper : MonoBehaviour, EFM_Describable
	{
		public FVRPhysicalObject physObj;
		public bool destroyed;

		public Mod.ItemType itemType;
		public Collider[] colliders;
		public EFM_CustomItemWrapper prefabCIW;
		public List<string> parents;
		public string ID;
		public bool looted;
		public bool foundInRaid;
		public bool hideoutSpawned;
		public int lootExperience;
		public float spawnChance;
		public Mod.ItemRarity rarity;
		public string itemName;
		public string description;
		private DescriptionPack descriptionPack;
		public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
		public int locationIndex; // 0: Player inventory, 1: Base, 2: Raid, 3: Area slot. This is to keep track of where an item is in general
		public EFM_DescriptionManager descriptionManager; // The current description manager displaying this item's description
		public List<EFM_MarketItemView> marketItemViews;
		public LeaveItemProcessor leaveItemProcessor;
		public AudioClip[] itemSounds;
		public int upgradeCheckBlockedIndex = -1;
		public int upgradeCheckWarnedIndex = -1;
		public bool inAll;
		private bool _insured;
		public bool insured 
		{
			get { return _insured; }
            set { 
				_insured = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
			}
		}
		private int _currentWeight; // Includes attachments and ammo containers attached to this item
		public int currentWeight
		{
			get { return _currentWeight; }
			set
			{
				_currentWeight = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
			}
		}

		// Equipment
		// 0: Open (Model should be as orginal in tarkov), 1: ClosedFull (Closed but only folded to the point that any container/armor is not folded), 2: ClosedEmpty (Folded and flattened as much as is realistic)
		public bool modeInitialized;
		private int _mode = 2;
		public int mode
		{
			set
			{
				_mode = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
			} 
			get
            {
				return _mode;
            }
		}
		public GameObject[] models;
		public GameObject[] interactiveSets;
		public int[] volumes;

		// Helmet
		public bool blocksEarpiece;
		public bool blocksEyewear;
		public bool blocksFaceCover;
		public bool blocksHeadwear;

		// Armor
		public bool broken;
		public float coverage;
		public float damageResist;
		public float maxArmor;
		public float armor;

		// Rig and Backpacks
		public bool open;
		public Transform rightHandPoseOverride;
		public Transform leftHandPoseOverride;

		// Rig
		public int configurationIndex;
		public GameObject[] itemsInSlots;
		public Transform configurationRoot;
		public List<FVRQuickBeltSlot> rigSlots;
		private int activeSlotsSetIndex;

		// Backpacks, Containers, Pouches
		public Transform containerItemRoot;
		public GameObject mainContainer;
		public Renderer[] mainContainerRenderers;
		public bool canInsertItems = true;
		public Text volumeIndicatorText;
		public GameObject volumeIndicator;
		public int maxVolume;
		private int _containingVolume;
		public int containingVolume 
		{ 
			get { return _containingVolume; }
			set {
				_containingVolume = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
				volumeIndicatorText.text = (_containingVolume / Mod.volumePrecisionMultiplier).ToString() +"/"+(maxVolume / Mod.volumePrecisionMultiplier);
			}
		}
		public class ResetColPair
		{
			public FVRPhysicalObject physObj;
			public List<Collider> colliders;
		}
		public List<ResetColPair> resetColPairs;
		public List<string> whiteList;
		public List<string> blackList;

		// AmmoBox
		public string cartridge;
		public FireArmRoundClass roundClass;

		// Stack
		private int _stack = 1;
		public int stack
		{
			get { return _stack; }
			set { 
				_stack = value;
				UpdateStackModel();
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
			}
		}
		public int maxStack;
		public GameObject[] stackTriggers;
		private bool splittingStack;
		private int splitAmount;
		private Vector3 stackSplitStartPosition;
		private Vector3 stackSplitRightVector;

		// Amount
		public int _amount;
		public int amount 
		{ 
			get { return _amount; } 
			set
			{
				_amount = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
                }
			} 
		}
		public int maxAmount;

		// Consumable
		public float useTime = 0; // Amount of time it takes to use amountRate
		public float amountRate = -1; // Maximum amount that can be used from the consumable in single use ex.: grizzly can only be used to heal up to 175 hp per use. 0 a single unit of multiple, -1 means no limit up to maxAmount
		public List<EFM_Effect_Buff> effects; // Gives new effects
		public List<EFM_Effect_Consumable> consumeEffects; // Immediate effects or effects that modify/override/give new effects 
		private float consumableTimer;
		private bool validConsumePress;
		private bool validLeaveItemPress;
		private float leaveItemTime = -1;
		private float leavingTimer;
        public int targettedPart = -1; // TODO: Implement
		private static int breakPartExperience = -1;
		private static int painExperience = -1;
		private static int intoxicationExperience = -1;
		private static int lightBleedExperience = -1;
		private static int fractureExperience = -1;
		private static int heavyBleedExperience = -1;

		// DogTag
		public bool USEC;
		public int dogtagLevel = 1;
		public string dogtagName;

		private void Awake()
		{
			physObj = gameObject.GetComponent<FVRPhysicalObject>();
			if (itemType != Mod.ItemType.LootContainer)
			{
				_mode = volumes.Length - 1; // Set default mode to the last index of volumes, closed empty for containers and rigs
			}
			modeInitialized = true;

			descriptionPack = new DescriptionPack();
			if (itemType == Mod.ItemType.LootContainer)
			{
				descriptionPack.itemType = Mod.ItemType.LootContainer;
			}
			else
			{
				if (takeCurrentLocation)
				{
					locationIndex = Mod.currentLocationIndex;
				}

				prefabCIW = Mod.itemPrefabs[int.Parse(ID)].GetComponent<EFM_CustomItemWrapper>();

				descriptionPack.isCustom = true;
				descriptionPack.isPhysical = true;
				descriptionPack.customItem = this;
				descriptionPack.name = itemName;
				descriptionPack.description = description;
				descriptionPack.icon = Mod.itemIcons[ID];
				descriptionPack.amountRequiredPerArea = new int[22];

				SetCurrentWeight(this);
			}
		}

		public static int SetCurrentWeight(EFM_CustomItemWrapper item)
		{
			if (item == null)
			{
				return 0;
			}

			item.currentWeight = (int)(item.GetComponent<Rigidbody>().mass * 1000);

			if (item.itemType == Mod.ItemType.Rig || item.itemType == Mod.ItemType.ArmoredRig)
			{
				foreach (GameObject containedItem in item.itemsInSlots) 
				{
					if(containedItem != null)
                    {
						EFM_CustomItemWrapper containedItemCIW = containedItem.GetComponent<EFM_CustomItemWrapper>();
						EFM_VanillaItemDescriptor containedItemVID = containedItem.GetComponent<EFM_VanillaItemDescriptor>();
						if(containedItemCIW != null)
                        {
							item.currentWeight += SetCurrentWeight(containedItemCIW);
                        }
                        else if(containedItemVID != null)
                        {
							item.currentWeight += EFM_VanillaItemDescriptor.SetCurrentWeight(containedItemVID);
                        }
                    }
				}
			}
			else if(item.itemType == Mod.ItemType.Backpack || item.itemType == Mod.ItemType.Container || item.itemType == Mod.ItemType.Pouch)
			{
				foreach (Transform containedItem in item.containerItemRoot)
				{
					if (containedItem != null)
					{
						EFM_CustomItemWrapper containedItemCIW = containedItem.GetComponent<EFM_CustomItemWrapper>();
						EFM_VanillaItemDescriptor containedItemVID = containedItem.GetComponent<EFM_VanillaItemDescriptor>();
						if (containedItemCIW != null)
						{
							item.currentWeight += SetCurrentWeight(containedItemCIW);
						}
						else if (containedItemVID != null)
						{
							item.currentWeight += EFM_VanillaItemDescriptor.SetCurrentWeight(containedItemVID);
						}
					}
				}
			}
			else if(item.itemType == Mod.ItemType.AmmoBox)
            {
				FVRFireArmMagazine magazine = item.GetComponent<FVRFireArmMagazine>();
				if (magazine != null)
				{
					item.currentWeight += 15 * magazine.m_numRounds;
				}
            }

			return item.currentWeight;
		}

		private void Update()
		{
			// Update based on splitting stack
			if (splittingStack)
			{
				if(physObj.m_hand == null)
				{
					CancelSplit();
				}

				Vector3 handVector = physObj.m_hand.transform.position - stackSplitStartPosition;
				float angle = Vector3.Angle(stackSplitRightVector, handVector);
				float distanceFromCenter = Mathf.Clamp(handVector.magnitude * Mathf.Cos(angle * Mathf.Deg2Rad), -0.19f, 0.19f);

				// Scale is from -0.19 (0) to 0.19 (stack)
				if (distanceFromCenter <= -0.19f)
				{
					splitAmount = 0;
				}
				else if (distanceFromCenter >= 0.19f)
				{
					splitAmount = stack;
				}
				else
				{
					splitAmount = Mathf.Max(1, (int)(Mathf.InverseLerp(-0.19f, 0.19f, distanceFromCenter) * stack));
				}

				Mod.stackSplitUICursor.transform.localPosition = new Vector3(distanceFromCenter * 100, -2.14f, 0);
				Mod.stackSplitUIText.text = splitAmount.ToString() + "/" + stack;
			}
		}

		public void SetContainerHovered(bool hovered)
		{
			Material matToUse = hovered ? Mod.quickSlotHoverMaterial : Mod.quickSlotConstantMaterial;
			foreach (Renderer r in mainContainerRenderers)
			{
				r.material = matToUse;
			}
		}

		public bool AddItemToContainer(FVRPhysicalObject item)
		{
			if (itemType != Mod.ItemType.Backpack &&
			   itemType != Mod.ItemType.Container &&
			   itemType != Mod.ItemType.Pouch)
			{
				return false;
			}

			// Get item volume
			int volumeToUse = 0;
			int weightToUse = 0;
			string IDToUse = "";
			List<string> parentsToUse = null;
			EFM_CustomItemWrapper wrapper = item.GetComponent<EFM_CustomItemWrapper>();
			EFM_VanillaItemDescriptor VID = item.GetComponent<EFM_VanillaItemDescriptor>();
			if (wrapper != null)
			{
                if (!wrapper.modeInitialized)
                {
					wrapper.mode = wrapper.volumes.Length - 1;
					wrapper.modeInitialized = true;

				}
				volumeToUse = wrapper.volumes[wrapper.mode];
				weightToUse = wrapper.currentWeight;
				IDToUse = wrapper.ID;
				parentsToUse = wrapper.parents;
			}
			else
			{
				volumeToUse = VID.volume;
				weightToUse = VID.weight;
				IDToUse = VID.H3ID;
				parentsToUse = VID.parents;
			}

			if (containingVolume + volumeToUse <= maxVolume && Mod.IDDescribedInList(IDToUse, parentsToUse, whiteList, blackList))
			{
				// Attach item to container
				// Set all non trigger colliders that are on default layer to trigger so they dont collide with anything
				Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
				if (resetColPairs == null)
				{
					resetColPairs = new List<EFM_CustomItemWrapper.ResetColPair>();
				}
				ResetColPair resetColPair = null;
				foreach (Collider col in cols)
				{
					if (col.gameObject.layer == 0 || col.gameObject.layer == 14)
					{
						col.isTrigger = true;

						// Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
						if (resetColPair == null)
						{
							resetColPair = new ResetColPair();
							resetColPair.physObj = item;
							resetColPair.colliders = new List<Collider>();
						}
						resetColPair.colliders.Add(col);
					}
				}
				if (resetColPair != null)
				{
					resetColPairs.Add(resetColPair);
				}
				item.transform.parent = containerItemRoot;
				item.StoreAndDestroyRigidbody();

				// If put a melee weapon in a container, we must remove it from All, otherwise it will be FixedUpdated like a loose melee weapon
				if(item is FVRMeleeWeapon)
                {
					Mod.RemoveFromAll(item, null, VID);
                }

				// Add volume to container
				containingVolume += volumeToUse;

				// Add item's weight to container
				currentWeight += weightToUse;

				return true;
			}
			else
			{
				return false;
			}
		}

		public void TakeInput(FVRViveHand hand, EFM_Hand EFMHand)
		{
			bool usageButtonDown = false;
			bool usageButtonPressed = false;
			bool usageButtonUp = false;
			if (hand.CMode == ControlMode.Index || hand.CMode == ControlMode.Oculus)
            {
				usageButtonDown = hand.Input.AXButtonDown;
				usageButtonPressed = hand.Input.AXButtonPressed;
				usageButtonUp = hand.Input.AXButtonUp;
			}
			else if(hand.CMode == ControlMode.Vive || hand.CMode == ControlMode.WMR)
			{
				Vector2 touchpadAxes = hand.Input.TouchpadAxes;
				if (touchpadAxes.magnitude > 0.3f && Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
				{
					usageButtonDown = hand.Input.TouchpadDown;
					usageButtonPressed = hand.Input.TouchpadPressed;
					usageButtonUp = hand.Input.TouchpadUp;
				}
			}

			// If A has started being pressed this frame
			if (usageButtonDown)
			{
				if (leaveItemProcessor == null)
				{
					switch (itemType)
					{
						case Mod.ItemType.ArmoredRig:
						case Mod.ItemType.Rig:
						case Mod.ItemType.Backpack:
						case Mod.ItemType.BodyArmor:
						case Mod.ItemType.Container:
						case Mod.ItemType.Pouch:
							ToggleMode(true, hand.IsThisTheRightHand);
							break;
						case Mod.ItemType.Money:
							if (splittingStack)
							{
								// End splitting
								if (splitAmount != stack || splitAmount == 0)
								{
									stack -= splitAmount;

									GameObject itemObject = Instantiate(Mod.itemPrefabs[int.Parse(ID)], hand.transform.position + hand.transform.forward * 0.2f, Quaternion.identity);
									if (Mod.currentLocationIndex == 1) // In hideout
									{
										itemObject.transform.parent = Mod.currentBaseManager.transform.GetChild(Mod.currentBaseManager.transform.childCount - 2);
										EFM_CustomItemWrapper CIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
										CIW.stack = splitAmount;
										Mod.currentBaseManager.baseInventoryObjects[ID].Add(itemObject);
									}
									else // In raid
									{
										itemObject.transform.parent = Mod.currentRaidManager.transform.GetChild(1).GetChild(1).GetChild(2);
										EFM_CustomItemWrapper CIW = itemObject.GetComponent<EFM_CustomItemWrapper>();
										CIW.stack = splitAmount;
									}
								}
								// else the chosen amount is 0 or max, meaning cancel the split
								CancelSplit();
							}
							else
							{
								// Start splitting
								Mod.stackSplitUI.SetActive(true);
								Mod.stackSplitUI.transform.position = hand.transform.position + hand.transform.forward * 0.2f;
								Mod.stackSplitUI.transform.rotation = Quaternion.Euler(0, hand.transform.eulerAngles.y, 0);
								stackSplitStartPosition = hand.transform.position;
								stackSplitRightVector = hand.transform.right;
								stackSplitRightVector.y = 0;

								splittingStack = true;
								Mod.amountChoiceUIUp = true;
								Mod.splittingItem = this;
							}
							break;
						default:
							break;
					}
                }
			}

			// If A is being pressed this frame
			if (usageButtonPressed)
			{
				if (leaveItemProcessor == null || validConsumePress)
				{
					leavingTimer = 0;
					switch (itemType)
					{
						case Mod.ItemType.Consumable:
							bool otherHandConsuming = false;
							if (!validConsumePress)
							{
								otherHandConsuming = EFMHand.otherHand.consuming;
							}
							if (!otherHandConsuming)
							{
								EFMHand.consuming = true;

								// Increment timer
								consumableTimer += Time.deltaTime;

								float use = Mathf.Clamp01(consumableTimer / useTime);
								Mod.consumeUIText.text = string.Format("{0:0.#}/{1:0.#}", amountRate <= 0 ? (use * amount) : (use * amountRate), amountRate <= 0 ? amount : amountRate);
								if (amountRate == 0)
								{
									// This consumable is discrete units and can only use one at a time, so set text to red until we have reached useTime, then set it to green
									if (consumableTimer >= useTime)
									{
										Mod.consumeUIText.color = Color.green;
									}
									else
									{
										Mod.consumeUIText.color = Color.red;
									}
								}
								else
								{
									Mod.consumeUIText.color = Color.white;
								}
								Mod.consumeUI.transform.parent = hand.transform;
								Mod.consumeUI.transform.localPosition = new Vector3(hand.IsThisTheRightHand ? -0.15f : 0.15f, 0, 0);
								Mod.consumeUI.transform.localRotation = Quaternion.Euler(25, 0, 0);
								Mod.consumeUI.SetActive(true);
								validConsumePress = true;
							}
							break;
						default:
							break;
					}
                }
                else if(leaveItemProcessor != null)
                {
					bool otherHandLeaving = false;
					if (!validLeaveItemPress)
					{
						otherHandLeaving = EFMHand.otherHand.leaving;
					}
					if (!otherHandLeaving)
					{
						EFMHand.leaving = true;

						// Increment timer
						leavingTimer += Time.deltaTime;

						if(leaveItemTime == -1)
						{
							List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[ID];
							leaveItemTime = conditions[conditions.Count - 1].plantTime;
						}

						Mod.consumeUIText.text = string.Format("{0:0.#}/{1:0.#}", leavingTimer, leaveItemTime);
						Mod.consumeUIText.color = Color.white;
						Mod.consumeUI.transform.parent = hand.transform;
						Mod.consumeUI.transform.localPosition = new Vector3(hand.IsThisTheRightHand ? -0.15f : 0.15f, 0, 0);
						Mod.consumeUI.transform.localRotation = Quaternion.Euler(25, 0, 0);
						Mod.consumeUI.SetActive(true);
						validLeaveItemPress = true;

						if (leavingTimer >= leaveItemTime)
						{
							EFMHand.leaving = false;
							validLeaveItemPress = false;
							leavingTimer = 0;
							leaveItemTime = -1;

							List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[ID];
							if(conditions.Count > 0)
							{
								TraderTaskCondition condition = conditions[conditions.Count - 1];
								++condition.itemCount;
								EFM_TraderStatus.UpdateConditionFulfillment(condition);
								if (condition.fulfilled)
								{
									conditions.RemoveAt(conditions.Count - 1);
								}
							}
							if (conditions.Count == 0)
							{
								leaveItemProcessor.conditionsByItemID.Remove(ID);
								leaveItemProcessor.itemIDs.Remove(ID);
							}

							// Update player inventory and weight
							Mod.RemoveFromPlayerInventory(transform, true);
							Mod.weight -= currentWeight;
							destroyed = true;
							physObj.ForceBreakInteraction();
							Destroy(gameObject);
						}
					}
				}
			}

			// If A has been released this frame
			if (usageButtonUp)
			{
				// If last frame the consume button was being pressed
				if (validConsumePress)
				{
					validConsumePress = false;
					hand.GetComponent<EFM_Hand>().consuming = false;
					Mod.consumeUI.SetActive(false);
					Mod.instance.LogInfo("Valid consume released");

					if (amountRate == -1)
					{
						Mod.instance.LogInfo("\tAmount rate -1");
						if (maxAmount > 0)
						{
							Mod.instance.LogInfo("\t\tConsuming, amountRate == -1, maxAmount > 0, consumableTimer: " + consumableTimer + ", useTime: " + useTime);
							// Consume for the fraction timer/useTime of remaining amount. If timer >= useTime, we consume the whole thing
							int amountToConsume = consumableTimer >= useTime ? amount : (int)(amount * (consumableTimer / useTime));
							Mod.instance.LogInfo("\t\tAmount to consume: " + amountToConsume);
							if (amount - amountToConsume <= 0)
							{
								// Attempt to apply effects at full effectiveness
								// Consume if succesful
								if (ApplyEffects(1, amountToConsume))
								{
									amount = 0;

									UpdateUseItemCounterConditions();
								}
							}
							else
							{
								// Apply effects at partial effectiveness
								if (ApplyEffects(amountToConsume / maxAmount, amountToConsume))
								{
									amount -= amountToConsume;

									UpdateUseItemCounterConditions();
								}
							}
						}
						//else // This should never happen anymore because single unit consumable have amountRate = 0 and maxAmount = 1
						//{
						//	// Consume the whole thing if timer >= useTime because this is a single unit consumable
						//	if(consumableTimer >= useTime)
						//  {
						//		// Attempt to apply effects at full effectiveness
						//		// Consume if succesful
						//		if (ApplyEffects(1, 0))
						//		{
						//			amount = 0;
						//			Destroy(gameObject);
						//		}
						//	}
						//}
					}
					else if (amountRate == 0)
					{
						Mod.instance.LogInfo("\tAmount rate 0");
						// This consumable is discrete units and can only use one at a time, so consume one unit of it if timer >= useTime
						if (consumableTimer >= useTime)
						{
							Mod.instance.LogInfo("\t\tConsumable timer >= useTime");
							if (ApplyEffects(1, 1))
							{
								amount -= 1;

								UpdateUseItemCounterConditions();
							}
							// Apply effects at full effectiveness
						}
					}
					else
					{
						Mod.instance.LogInfo("\tAmount rate else");
						// Consume timer/useTime of to amountRate
						int amountToConsume = consumableTimer >= useTime ? (int)amountRate : (int)(amountRate * (consumableTimer / useTime));
						Mod.instance.LogInfo("\tAmount to consume: "+ amountToConsume);
						if (consumableTimer >= useTime)
						{
							Mod.instance.LogInfo("\t\tConsumable timer >= useTime");
							// Apply effects at full effectiveness
							// NOTE: In the case we have an amount rate, here, we only remove the used amount if no other effects  have been applied
							// so only if ApplyEffects returns false.
							if (!ApplyEffects(1, amountToConsume))
							{
								Mod.instance.LogInfo("\t\t\tNo effects applied, using consumed amount to heal");
								// Here we also have to apply amount consumed as health to relevant parts
								// NOTE: This assumes that only items that can heal health have an amountRate != 0 || -1 defined
								// If this ever changes, we will need to have an additional flag for healing items
								int actualAmountConsumed = 0;
								int partIndex = -1;
								if (targettedPart != -1)
								{
									partIndex = targettedPart;
									Mod.instance.LogInfo("\t\t\t\tPart "+partIndex+" targetted");
								}
								else // No part targetted, prioritize least health TODO: Make a setting to prioritize by part first instead, and as we go through more important parts first, those will be prioritized if health is equal
								{
									Mod.instance.LogInfo("\t\t\t\tNo part targetted, finding best...");
									int leastIndex = -1;
									float leastAmount = 1000;
									for (int i = 0; i < Mod.health.Length; ++i)
									{
										if (Mod.health[i] < leastAmount)
										{
											leastIndex = i;
											leastAmount = Mod.health[i];
										}
									}
									if (leastIndex >= 0)
									{
										partIndex = leastIndex;
									}
									Mod.instance.LogInfo("\t\t\t\tBest part to apply helth to: "+partIndex);
								}

								if (partIndex != -1)
								{
									Mod.instance.LogInfo("\t\t\t\tApplying "+actualAmountConsumed+" to "+partIndex+", which has "+ Mod.health[partIndex]+" health");
									actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]));
									Mod.health[partIndex] = Mathf.Min(Mod.currentMaxHealth[partIndex], Mod.health[partIndex] + actualAmountConsumed);
									Mod.instance.LogInfo("\t\t\t\tAfter healing: "+ Mod.health[partIndex]);
								}
								// else, no target part and all parts are at max health

								amount -= actualAmountConsumed;
								if (actualAmountConsumed > 0)
								{
									Mod.AddExperience(actualAmountConsumed, 2, "Treatment experience - Healing ({0})");

									UpdateUseItemCounterConditions();
								}
                            }
                            else
							{
								UpdateUseItemCounterConditions();
							}
						}
						else
						{
							Mod.instance.LogInfo("\t\tConsumable timer < useTime");

							// Apply effects at effectiveness * amountToConsume / amountRate
							if (!ApplyEffects(amountToConsume / amountRate, amountToConsume))
							{
								// Here we also have to apply amount consumed as health to relevant parts
								// NOTE: This assumes that only items that can heal health have an amountRate != 0 || -1 defined
								// If this ever changes, we will need to have an additional flag for healing items
								int actualAmountConsumed = 0;
								int partIndex = -1;
								if (targettedPart != -1)
								{
									partIndex = targettedPart;
								}
								else // No part targetted, prioritize least health, and as we go through more important parts first, those will be prioritized if health is equal
								{
									int leastIndex = -1;
									float leastAmount = 1000;
									for (int i = 0; i < Mod.health.Length; ++i)
									{
										if (Mod.health[i] < leastAmount)
										{
											leastIndex = i;
											leastAmount = Mod.health[i];
										}
									}
									if (leastIndex >= 0)
									{
										partIndex = leastIndex;
									}
								}

								if (partIndex != -1)
								{
									if (Mod.currentLocationIndex == 1) // In hideout, take base max health
									{
										actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]));
										Mod.health[partIndex] = Mathf.Min(Mod.currentMaxHealth[partIndex], Mod.health[partIndex] + actualAmountConsumed);
									}
									else // In raid, take raid max health
									{
										actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]));
										Mod.health[partIndex] = Mathf.Min(Mod.currentMaxHealth[partIndex], Mod.health[partIndex] + actualAmountConsumed);
									}
								}
								// else, no target part and all parts are at max health

								amount -= actualAmountConsumed;
								if (actualAmountConsumed > 0)
								{
									Mod.AddExperience(actualAmountConsumed, 2, "Treatment experience - Healing ({0})");

									UpdateUseItemCounterConditions();
								}
                            }
                            else
							{
								UpdateUseItemCounterConditions();
							}
						}
					}

					consumableTimer = 0;

					if (amount == 0)
					{
						// Update player inventory and weight
						Mod.RemoveFromPlayerInventory(transform, true);
						Mod.weight -= currentWeight;
						destroyed = true;
						physObj.ForceBreakInteraction();
						Destroy(gameObject);

						if (Mod.currentLocationIndex == 1)
						{
							foreach (EFM_BaseAreaManager areaManager in Mod.currentBaseManager.baseAreaManagers)
							{
								areaManager.UpdateBasedOnItem(ID);
							}
						}
					}
				}
			}
		}

		private void UpdateUseItemCounterConditions()
        {
            if (Mod.currentUseItemCounterConditionsByItemID.ContainsKey(ID))
            {
				List<TraderTaskCounterCondition> useItemCounterConditions = Mod.currentUseItemCounterConditionsByItemID[ID];
				foreach (TraderTaskCounterCondition counterCondition in useItemCounterConditions)
				{
					// Check task and condition state validity
					if (!counterCondition.parentCondition.visible)
					{
						continue;
					}

					// Check constraint counters (Location, Equipment, HealthEffect, InZone)
					bool constrained = false;
					foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
					{
						if (!EFM_TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
						{
							constrained = true;
							break;
						}
					}
					if (constrained)
					{
						continue;
					}

					// Successful use, increment count and update fulfillment 
					++counterCondition.useCount;
					EFM_TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
				}
			}
        }

		public void CancelSplit()
        {
			Mod.stackSplitUI.SetActive(false);
			splittingStack = false;
			Mod.amountChoiceUIUp = false;
			Mod.splittingItem = null;
		}

		private bool ApplyEffects(float effectiveness, int amountToConsume)
        {
			Mod.instance.LogInfo("Apply effects called, effectiveness: " + effectiveness + ", amount to consume: " + amountToConsume);
			if(consumeEffects == null)
            {
				consumeEffects = prefabCIW.consumeEffects;
				effects = prefabCIW.effects;
            }

			// TODO: Set targetted part if hovering over a part

			int appliedEffectCount = 0;
			bool singleEffect = consumeEffects.Count + effects.Count == 1;
			int unusedEffectCount = 0;

			// Apply consume effects
			foreach (EFM_Effect_Consumable consumeEffect in consumeEffects)
			{
				switch (consumeEffect.effectType)
                {
					// Health
					case EFM_Effect_Consumable.EffectConsumable.Hydration:
						float hydrationAmount = consumeEffect.value * effectiveness;
						Mod.hydration += hydrationAmount;
						Mod.AddSkillExp(hydrationAmount * (EFM_Skill.hydrationRecoveryRate / 100), 5);
						++appliedEffectCount;
						Mod.instance.LogInfo("\tApplied hydration");
						break;
					case EFM_Effect_Consumable.EffectConsumable.Energy:
						float energyAmount = consumeEffect.value * effectiveness;
						Mod.energy += energyAmount;
						Mod.AddSkillExp(energyAmount * (EFM_Skill.energyRecoveryRate / 100), 5);
						++appliedEffectCount;
						Mod.instance.LogInfo("\tApplied energy");
						break;

					// Damage
					case EFM_Effect_Consumable.EffectConsumable.RadExposure:
						Mod.instance.LogInfo("\trad exposure");
						bool radExposureApplied = false;
						if (consumeEffect.duration == 0)
						{
							Mod.instance.LogInfo("\t\tNo duration, curing rad exposure if present");
							// Remove all rad exposure effects
							for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
                            {
								if (consumeEffect.cost == 0 || consumeEffect.cost <= amount - amountToConsume)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.RadExposure)
									{
										EFM_Effect.effects.RemoveAt(i);
										radExposureApplied = true;
										++appliedEffectCount;
										amount -= consumeEffect.cost;
										Mod.instance.LogInfo("\t\t\tFound valid rad exposure at effect index: "+i);
										return true;
									}
                                }
                                else
                                {
									break;
                                }
                            }
                            if (!radExposureApplied)
							{
								Mod.instance.LogInfo("\t\t\tNo valid rad exposure");
								if (singleEffect)
                                {
									return false;
                                }
                                else
                                {
									++unusedEffectCount;
                                }
                            }
                        }
                        else
						{
							// Make all rad exposure effects inactive for the duration
							for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
							{
								if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.RadExposure && EFM_Effect.effects[i].active)
								{
									EFM_Effect.effects[i].active = false;
									EFM_Effect.effects[i].inactiveTimer = consumeEffect.duration * effectiveness;
									radExposureApplied = true;
									++appliedEffectCount;
									Mod.instance.LogInfo("RadExposure effect found, disabling for "+ EFM_Effect.effects[i].inactiveTimer);
								}
							}
                            if (radExposureApplied)
							{
								return true;
                            }
                            else
                            {
								++unusedEffectCount;
                            }
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Pain:
						Mod.instance.LogInfo("\tPain");
						if (painExperience == -1)
						{
							painExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Pain"]["HealExperience"];
						}
						// Make all pain effects inactive for the duration
						bool painApplied = false;
						for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
						{
							if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Pain && EFM_Effect.effects[i].active)
							{
								EFM_Effect.effects[i].active = false;
								float timer = consumeEffect.duration * effectiveness;
								EFM_Effect.effects[i].inactiveTimer = timer + timer * (EFM_Skill.immunityPainKiller * (Mod.skills[6].currentProgress / 100) / 100);
								Mod.AddExperience((int)(painExperience * effectiveness), 2, "Treatment experience - Pain ({0})");
								painApplied = true;
								++appliedEffectCount;
								Mod.instance.LogInfo("\t\tFound pain effect at effect index "+i);
							}
						}
						if (painApplied)
						{
							return true;
						}
						else
						{
							++unusedEffectCount;
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Contusion:
						Mod.instance.LogInfo("\tContusion");
						// Remove all contusion effects
						bool contusionApplied = false;
						for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
						{
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Contusion)
								{
									EFM_Effect.effects.RemoveAt(i);
									contusionApplied = true;
									++appliedEffectCount;
									amount -= consumeEffect.cost;
									Mod.instance.LogInfo("\t\tFound contusion effect at effect index " + i);
									return true;
								}
                            }
                            else
                            {
								break;
                            }
						}
                        if (!contusionApplied)
						{
							Mod.instance.LogInfo("\t\tNo valid contusion found");
							if (singleEffect)
							{
								return false;
							}
							else
							{
								++unusedEffectCount;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Intoxication:
						Mod.instance.LogInfo("\tIntoxication");
						if (intoxicationExperience == -1)
						{
							intoxicationExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Intoxication"]["HealExperience"];
						}
						// Remove all Intoxication effects
						bool intoxicationApplied = false;
						for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
						{
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Intoxication)
								{
									EFM_Effect.effects.RemoveAt(i);
									amount -= consumeEffect.cost;
									intoxicationApplied = true;
									 ++appliedEffectCount;
									Mod.AddExperience(intoxicationExperience, 2, "Treatment experience - Intoxication ({0})");
									Mod.instance.LogInfo("\t\tFound contusion effect at effect index " + i);
									return true;
								}
                            }
                            else
                            {
								break;
                            }
						}
						if (!intoxicationApplied)
						{
							Mod.instance.LogInfo("\t\tNo valid intoxication found");
							if (singleEffect)
							{
								return false;
							}
							else
							{
								++unusedEffectCount;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.LightBleeding:
						if (lightBleedExperience == -1)
						{
							lightBleedExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["HealExperience"];
						}
						Mod.instance.LogInfo("\tHas damage light bleeding effect");
						// Remove priority LightBleeding effect, or one found on targetted part
						if (targettedPart == -1)
						{
							Mod.instance.LogInfo("\t\tNo targetted part, cost: "+consumeEffect.cost+", amount: "+amount+", to consume: "+amountToConsume);
							// Prioritize lowest partIndex
							int highest = -1;
							int lowestPartIndex = 7;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.LightBleeding && EFM_Effect.effects[i].partIndex < lowestPartIndex)
									{
										Mod.instance.LogInfo("\t\t\tFound valid light bleeding");
										highest = i;
									}
								}
							}
							if(highest == -1) // We did not find light bleeding
							{
								Mod.instance.LogInfo("\t\tNo valid light bleeding");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no bleeding to stop but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
                            }
                            else
							{
								Mod.instance.LogInfo("\t\tRemoving valid light bleed");
								EFM_Effect.RemoveEffectAt(highest);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(lightBleedExperience, 2, "Treatment experience - Light Bleeding ({0})");
								return true;
							}
                        }
                        else
                        {
							// Find lightbleeding on targettedpart
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].partIndex == targettedPart && EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.LightBleeding)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find light bleeding on the part
							{
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no bleeding to stop but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(lightBleedExperience, 2, "Treatment experience - Light Bleeding ({0})");
								return true;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Fracture:
						Mod.instance.LogInfo("\tFracture");
						if (fractureExperience == -1)
						{
							fractureExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Fracture"]["HealExperience"];
						}
						// Remove first Fracture effect, or one found on targetted part
						if (targettedPart == -1)
						{
							// Find first fracture
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Fracture)
									{
										index = i;
										break;
									}
								}
							}
							if(index == -1) // We did not find Fracture
							{
								Mod.instance.LogInfo("\t\tNo fracture found");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no fracture to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
                            {
								EFM_Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
								Mod.instance.LogInfo("\t\tFound Fracture effect at effect index " + index);
								return true;
							}
                        }
                        else
                        {
							// Find Fracture on targettedpart
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].partIndex == targettedPart && EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Fracture)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find Fracture on the part
							{
								Mod.instance.LogInfo("\t\tNo fracture found on target part");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no fracture to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
								Mod.instance.LogInfo("\t\tFound Fracture effect at effect index " + index);
								return true;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.DestroyedPart:
						Mod.instance.LogInfo("\tDestroyed part");
						if (breakPartExperience == -1)
                        {
							breakPartExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["BreakPart"]["HealExperience"];
						}
						// Remove priority DestroyedPart effect, or one found on targetted part
						if (targettedPart == -1)
						{
							// Prioritize lowest partIndex
							int highest = -1;
							int lowestPartIndex = 7;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.DestroyedPart && EFM_Effect.effects[i].partIndex < lowestPartIndex)
									{
										highest = i;
									}
								}
							}
							if (highest == -1) // We did not find DestroyedPart
							{
								Mod.instance.LogInfo("\t\tNo destroyed part found");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no DestroyedPart to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(highest);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.health[lowestPartIndex] = 1;
								if(Mod.currentLocationIndex == 2)
                                {
									Mod.currentMaxHealth[lowestPartIndex] *= UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax);
                                }
								Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");
								Mod.AddSkillExp(EFM_Skill.surgerySkillProgress/EFM_Skill.surgeryAction, 28);
								Mod.instance.LogInfo("\t\tFound destroyed part effect at effect index " + highest);
								return true;
							}
						}
						else
						{
							// Find DestroyedPart on targettedpart
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].partIndex == targettedPart && EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.DestroyedPart)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find DestroyedPart on the part
							{
								Mod.instance.LogInfo("\t\tNo destroyed part found on target part");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no DestroyedPart to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.health[targettedPart] = 1;
								if (Mod.currentLocationIndex == 2)
								{
									Mod.currentMaxHealth[targettedPart] *= UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax);
								}
								Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");
								Mod.instance.LogInfo("\t\tFound destroyed part effect at effect index " + index);
								return true;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.HeavyBleeding:
						Mod.instance.LogInfo("\tHeavy bleed");
						if (heavyBleedExperience == -1)
						{
							heavyBleedExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["HealExperience"];
						}
						// Remove priority DestroyedPart effect, or one found on targetted part
						if (targettedPart == -1)
						{
							// Prioritize lowest partIndex
							int highest = -1;
							int lowestPartIndex = 7;

							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.HeavyBleeding && EFM_Effect.effects[i].partIndex < lowestPartIndex)
									{
										highest = i;
									}
								}
							}
							if (highest == -1) // We did not find HeavyBleeding
							{
								Mod.instance.LogInfo("\t\tNo heavy bleed found");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no DestroyedPart to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(highest);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
								Mod.instance.LogInfo("\t\tFound heavy bleed effect at effect index " + highest);
								return true;
							}
						}
						else
						{
							// Find heavy bleed on targettedpart
							int index = -1;

							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].partIndex == targettedPart && EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.HeavyBleeding)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find HeavyBleeding on the part
							{
								Mod.instance.LogInfo("\t\tNo heavy bleed found on targetted part");
								if (singleEffect) // It is the only effect we want to apply, so fail it because there is nothing to do
								{
									return false;
								}
								else // no HeavyBleeding to fix but this consumable has other effects we want to apply
								{
									++unusedEffectCount;
								}
							}
							else
							{
								EFM_Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
								Mod.instance.LogInfo("\t\tFound heavy bleed effect at effect index " + index);
								return true;
							}
						}
						break;
				}
            }

			// Apply buffs
			foreach (EFM_Effect_Buff buff in effects)
			{
				if (buff.chance != 1 && Random.value > buff.chance)
				{
					continue;
				}

				// Set effect
				EFM_Effect effect = new EFM_Effect();
				effect.effectType = buff.effectType;

				effect.value = buff.value;
				effect.timer = buff.duration;
				effect.hasTimer = buff.duration > 0;
				effect.delay = buff.delay;
				effect.fromStimulator = true;

				EFM_Effect.effects.Add(effect);

				switch (buff.effectType)
                {
					case EFM_Effect.EffectType.SkillRate:
						// Set effect
						effect.skillIndex = buff.skillIndex;
						effect.value = Mod.skills[buff.skillIndex].progress + buff.value * 100;
						break;
					case EFM_Effect.EffectType.MaxStamina:
						effect.value = Mod.maxStamina + buff.value;
						break;
					case EFM_Effect.EffectType.StaminaRate:
						effect.value = Mod.currentStaminaEffect + buff.value;
						break;
					case EFM_Effect.EffectType.WeightLimit:
					case EFM_Effect.EffectType.DamageModifier:
						effect.value = 1 + buff.value;
						break;
				}

				if (effect.value < 0)
				{
					if (Mod.skills[6].currentProgress / 100 >= 51 && UnityEngine.Random.value < (EFM_Skill.stimulatorNegativeBuff * 51 / 100))
                    {
						EFM_Effect.effects.RemoveAt(EFM_Effect.effects.Count - 1);
                    }

					effect.timer += effect.timer * (EFM_Base_Manager.currentDebuffEndDelay - EFM_Base_Manager.currentDebuffEndDelay * (EFM_Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100)) 
								  - effect.timer * (EFM_Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);

					effect.value += effect.value * (EFM_Skill.immunityMiscEffects * (Mod.skills[6].currentProgress / 100) / 100);
                }
                else
                {
					effect.timer += effect.timer * (EFM_Skill.increasePositiveEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                }
			}

			// Return true if at least one effect was used
			Mod.instance.LogInfo("\tApplied effect count: "+appliedEffectCount+" returning: "+(appliedEffectCount > 0));
			return appliedEffectCount > 0; 
        }

		public void ToggleMode(bool inHand, bool isRightHand = false)
		{
			open = !open;
			if (open)
			{
				if (itemType == Mod.ItemType.ArmoredRig || itemType == Mod.ItemType.Rig)
				{
					// Set active the open model and interactive set, and set all others inactive
					SetMode(0);
					OpenRig(inHand, isRightHand);
				}
				else if (itemType == Mod.ItemType.Backpack)
				{
					SetMode(0);
					SetContainerOpen(true, isRightHand);
					volumeIndicator.SetActive(true);
					volumeIndicatorText.text = (containingVolume / Mod.volumePrecisionMultiplier).ToString() + "/" + (maxVolume / Mod.volumePrecisionMultiplier);
				}
				else if (itemType == Mod.ItemType.Container || itemType == Mod.ItemType.Pouch)
				{
					SetContainerOpen(true, isRightHand);
					volumeIndicator.SetActive(true);
					volumeIndicatorText.text = (containingVolume / Mod.volumePrecisionMultiplier).ToString() + "/" + (maxVolume / Mod.volumePrecisionMultiplier);
				}
				else if(itemType == Mod.ItemType.LootContainer)
				{
					SetContainerOpen(true, isRightHand);
					gameObject.GetComponent<EFM_LootContainer>().shouldSpawnItems = true;

					Mod.AddSkillExp(EFM_Skill.searchAction, 30);
				}
			}
			else
			{
				if (itemType == Mod.ItemType.ArmoredRig || itemType == Mod.ItemType.Rig)
				{
					if (itemsInSlots != null)
					{
						int modelIndex = 2; // Empty by default
						for (int i = 0; i < itemsInSlots.Length; ++i)
						{
							if (itemsInSlots[i] != null)
							{
								modelIndex = 1; // Full if item is found
								break;
							}
						}

						SetMode(modelIndex);

						CloseRig(inHand, isRightHand);
					}
					// else should never happen in case we have a rig
				}
				else if (itemType == Mod.ItemType.Backpack)
				{
					SetMode(containingVolume > 0 ? 1 : 2);
					SetContainerOpen(false, isRightHand);
					volumeIndicator.SetActive(false);
				}
				else if (itemType == Mod.ItemType.BodyArmor)
				{
					SetMode(1);
				}
				else if (itemType == Mod.ItemType.Container || itemType == Mod.ItemType.Pouch)
				{
					SetContainerOpen(false, isRightHand);
					volumeIndicator.SetActive(false);
				}
			}
		}

		private void SetMode(int index)
		{
			mode = index;
			if (itemType == Mod.ItemType.Money)
			{
				for (int i = 0; i < models.Length; ++i)
				{
					models[i].SetActive(i == index);
					interactiveSets[i].SetActive(i == index);
					stackTriggers[i].SetActive(i == index);
				}
			}
			else
			{
				for (int i = 0; i < models.Length; ++i)
				{
					models[i].SetActive(i == index);
					interactiveSets[i].SetActive(i == index);
				}
			}

			if (descriptionManager != null)
			{
				descriptionManager.SetDescriptionPack();
			}
		}

		private void OpenRig(bool processslots, bool isRightHand = false)
		{
			configurationRoot.gameObject.SetActive(true);

			// Set active slots
			Mod.otherActiveSlots.Add(rigSlots);
			activeSlotsSetIndex = Mod.otherActiveSlots.Count - 1;

			// Load items into their slots
			for (int i = 0; i < itemsInSlots.Length; ++i)
			{
				if (itemsInSlots[i] != null)
				{
					FVRPhysicalObject physicalObject = itemsInSlots[i].GetComponent<FVRPhysicalObject>();
					EFM_CustomItemWrapper CIW = itemsInSlots[i].GetComponent<EFM_CustomItemWrapper>();
					if(CIW != null && !CIW.inAll)
                    {
						FVRInteractiveObject.All.Add(physicalObject);
						typeof(FVRInteractiveObject).GetField("m_index", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(FVRInteractiveObject.All[FVRInteractiveObject.All.Count - 1], FVRInteractiveObject.All.Count - 1);

						CIW.inAll = true;
					}
					SetQuickBeltSlotPatch.dontProcessRigWeight = true; // Dont want to add the weight of this item to the rig as we set its slot, the item is already in the rig
					physicalObject.SetQuickBeltSlot(rigSlots[i]);
					physicalObject.SetParentage(null);
					physicalObject.transform.localScale = Vector3.one;
					physicalObject.transform.position = rigSlots[i].transform.position;
					rigSlots[i].transform.localPosition = Vector3.zero;
					rigSlots[i].transform.localRotation = Quaternion.identity;
					FieldInfo grabPointTransformField = typeof(FVRPhysicalObject).GetField("m_grabPointTransform", BindingFlags.NonPublic | BindingFlags.Instance);
					Transform m_grabPointTransform = (Transform)grabPointTransformField.GetValue(physicalObject);
					if (m_grabPointTransform != null)
					{
						if (physicalObject.QBPoseOverride != null)
						{
							m_grabPointTransform.position = physicalObject.QBPoseOverride.position;
							m_grabPointTransform.rotation = physicalObject.QBPoseOverride.rotation;
						}
						else if (physicalObject.PoseOverride != null)
						{
							m_grabPointTransform.position = physicalObject.PoseOverride.position;
							m_grabPointTransform.rotation = physicalObject.PoseOverride.rotation;
						}
					}
					itemsInSlots[i].SetActive(true);
				}
			}
		}

		public void UpdateRigMode()
		{
			// Return right away if not a rig or if open
			if (!(itemType == Mod.ItemType.Rig || itemType == Mod.ItemType.ArmoredRig) || mode == 0)
			{
				return;
            }

			for(int i=0; i < itemsInSlots.Length; ++i)
            {
				if(itemsInSlots[i] != null)
				{
					SetMode(1);
					return;
                }
            }

			// If we get this far it is because no items in slots, so set to closed empty
			SetMode(2);
        }

		public void UpdateBackpackMode()
		{
			// Return right away if not a backpack or if open
			if (itemType != Mod.ItemType.Backpack || mode == 0)
			{
				return;
            }

			if(containerItemRoot.childCount > 0)
			{
				SetMode(1);
            }
            else
			{
				SetMode(2);
			}
        }

		private void CloseRig(bool processslots, bool isRightHand = false)
		{
			configurationRoot.gameObject.SetActive(false);

			// Remove from other active slots
			Mod.otherActiveSlots.RemoveAt(activeSlotsSetIndex);

			// Take all items out of their slots
			for (int i = 0; i < itemsInSlots.Length; ++i)
			{
				if (itemsInSlots[i] != null)
				{
					itemsInSlots[i].SetActive(false); 
				}
			}

			// TODO: Review if this is necessary, why should we be clearing the slots above pockets of their contents when we close a rig?
			//GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
		}

		private void SetContainerOpen(bool open, bool isRightHand = false)
		{
			mainContainer.SetActive(open);
			containerItemRoot.gameObject.SetActive(open);
		}

		public void UpdateStackModel()
        {
			float stackFraction = (float)_stack / maxStack;
			if(stackFraction <= 0.33f)
            {
				SetMode(0);
            }
			else if(stackFraction <= 0.66f)
			{
				SetMode(1);
			}
            else
			{
				SetMode(2);
			}
        }
	
		public DescriptionPack GetDescriptionPack()
        {
			if(itemType == Mod.ItemType.LootContainer)
            {
				return descriptionPack;
            }

			descriptionPack.amount = (Mod.baseInventory.ContainsKey(ID) ? Mod.baseInventory[ID] : 0) + (Mod.playerInventory.ContainsKey(ID) ? Mod.playerInventory[ID] : 0);
			descriptionPack.amountRequired = 0;
			for (int i=0; i < 22; ++i)
			{
				if (Mod.requiredPerArea[i] != null && Mod.requiredPerArea[i].ContainsKey(ID))
				{
					descriptionPack.amountRequired += Mod.requiredPerArea[i][ID];
					descriptionPack.amountRequiredPerArea[i] = Mod.requiredPerArea[i][ID];
                }
                else
				{
					descriptionPack.amountRequiredPerArea[i] = 0;
				}
			}
			descriptionPack.onWishlist = Mod.wishList.Contains(ID);
			descriptionPack.insured = insured;
			if (itemType == Mod.ItemType.Money)
			{
				descriptionPack.stack = stack;
			}
			else if(itemType == Mod.ItemType.Consumable)
			{
				if (maxAmount > 0)
				{
					descriptionPack.stack = amount;
					descriptionPack.maxStack = maxAmount;
                }
			}
			else if(itemType == Mod.ItemType.Backpack || itemType == Mod.ItemType.Container || itemType == Mod.ItemType.Pouch)
			{
				descriptionPack.containingVolume = containingVolume;
				descriptionPack.maxVolume = maxVolume;
			}
			else if(itemType == Mod.ItemType.AmmoBox)
			{
				FVRFireArmMagazine asMagazine = physObj as FVRFireArmMagazine;
				descriptionPack.stack = asMagazine.m_numRounds;
				descriptionPack.maxStack = asMagazine.m_capacity;
				descriptionPack.containedAmmoClassesByType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, int>>();
				foreach(FVRLoadedRound loadedRound in asMagazine.LoadedRounds)
				{
					if (loadedRound != null)
					{
						if (descriptionPack.containedAmmoClassesByType.ContainsKey(asMagazine.RoundType))
						{
							if (descriptionPack.containedAmmoClassesByType[asMagazine.RoundType].ContainsKey(loadedRound.LR_Class))
							{
								descriptionPack.containedAmmoClassesByType[asMagazine.RoundType][loadedRound.LR_Class] += 1;
							}
							else
							{
								descriptionPack.containedAmmoClassesByType[asMagazine.RoundType].Add(loadedRound.LR_Class, 1);
							}
						}
						else
						{
							Dictionary<FireArmRoundClass, int> newDict = new Dictionary<FireArmRoundClass, int>();
							newDict.Add(loadedRound.LR_Class, 1);
							descriptionPack.containedAmmoClassesByType.Add(asMagazine.RoundType, newDict);
						}
					}
				}
            }
			else if(itemType == Mod.ItemType.DogTag)
			{
				descriptionPack.stack = dogtagLevel;
				descriptionPack.name = itemName + " ("+dogtagName+")";
			}
			descriptionPack.weight = currentWeight;
			descriptionPack.volume = volumes[mode];
			descriptionPack.amountRequiredQuest = Mod.requiredForQuest.ContainsKey(ID) ? Mod.requiredForQuest[ID] : 0;
			descriptionPack.foundInRaid = foundInRaid;

			return descriptionPack;
        }

		public int GetValue()
        {
			return physObj.ObjectWrapper.CreditCost;

			// TODO: Maybe add all values of sub items attached to this one too but will have to adapt  market for it, for example, when we insure, we only ensure the root item not sub items
        }

		public int GetInsuranceValue()
        {
			// Thsi inureability of the current item will be checked by the calling method
			return physObj.ObjectWrapper.CreditCost;

			// TODO: WE DONT CHECK FOR INSUREABILITY HERE FOR THIS OBJECT, BUT WE MUST FOR SUB OBJECTS WHEN WE IMPLEMENT THAT
			// TODO: Maybe add all values of sub items attached to this one too but will have to adapt  market for it, for example, when we insure, we only ensure the root item not sub items
		}

        public void SetDescriptionManager(EFM_DescriptionManager descriptionManager)
        {
			this.descriptionManager = descriptionManager;
		}

		public void Highlight(Color color)
		{
			MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer mr in mrs)
			{
				mr.material.EnableKeyword("_RIM_ON");
				mr.material.SetColor("_RimColor", color);
			}
		}

		public void RemoveHighlight()
		{
			MeshRenderer[] mrs = gameObject.GetComponentsInChildren<MeshRenderer>();
			foreach (MeshRenderer mr in mrs)
			{
				mr.material.DisableKeyword("_RIM_ON");
			}
		}
	}

	public class EFM_MainContainer : MonoBehaviour
    {
		public EFM_CustomItemWrapper parentCIW;
    }
}
