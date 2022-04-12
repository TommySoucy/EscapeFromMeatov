using FistVR;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EFM
{
	public class EFM_CustomItemWrapper : MonoBehaviour, EFM_Describable
	{
		public FVRPhysicalObject physicalObject;

		public Mod.ItemType itemType;
		public Collider[] colliders;
		public string parent;
		public string ID;
		public bool looted;
		public int lootExperience;
		public string itemName;
		public string description;
		private DescriptionPack descriptionPack;
		public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
		public int locationIndex; // 0: Player inventory, 1: Base, 2: Raid. This is to keep track of where an item is in general
		public EFM_DescriptionManager descriptionManager; // The current description manager displaying this item's description
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

		// Equipment
		// 0: Open (Model should be as orginal in tarkov), 1: ClosedFull (Closed but only folded to the point that any container/armor is not folded), 2: ClosedEmpty (Folded and flattened as much as is realistic)
		public int mode;
		public GameObject[] models;
		public GameObject[] interactiveSets;
		public float[] volumes;

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
		public Transform itemObjectsRoot;
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
		public GameObject mainContainer;
		public Renderer[] mainContainerRenderers;
		public float maxVolume;
		private float _containingVolume;
		public float containingVolume 
		{ 
			get { return _containingVolume; }
			set {
				_containingVolume = value;
				if (descriptionManager != null)
				{
					descriptionManager.SetDescriptionPack();
				}
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

		// Stack
		private int _stack;
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

		// Amount
		private int _amount;
		public int amount 
		{ 
			get { return _amount; } 
			set {
				_amount = value;
				if(descriptionManager != null)
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
        public int targettedPart = -1;

		private void Start()
		{
			if (takeCurrentLocation)
			{
				locationIndex = Mod.currentLocationIndex;
			}

			descriptionPack = new DescriptionPack();
			descriptionPack.isCustom = true;
			descriptionPack.customItem = this;
			descriptionPack.name = itemName;
			descriptionPack.description = description;
			descriptionPack.icon = Mod.itemIcons[ID];
		}

		private void Update()
		{
			if (physicalObject.m_hand != null)
			{
				TakeInput();
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
			if (itemType != Mod.ItemType.Backpack ||
			   itemType != Mod.ItemType.Container ||
			   itemType != Mod.ItemType.Pouch)
			{
				return false;
			}

			// Get item volume
			float volumeToUse = 0;
			EFM_CustomItemWrapper wrapper = item.GetComponent<EFM_CustomItemWrapper>();
			if (wrapper != null)
			{
				volumeToUse = wrapper.volumes[wrapper.mode];
			}
			else
			{
				volumeToUse = Mod.sizeVolumes[(int)item.Size];
			}

			if (containingVolume + volumeToUse <= maxVolume)
			{
				// Attach item to backpack
				// Set all non trigger colliders that are on default layer to trigger so they dont collide with anything
				Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
				if (resetColPairs == null)
				{
					resetColPairs = new List<EFM_CustomItemWrapper.ResetColPair>();
				}
				EFM_CustomItemWrapper.ResetColPair resetColPair = null;
				foreach (Collider col in cols)
				{
					if (col.gameObject.layer == 0 || col.gameObject.layer == 14)
					{
						col.isTrigger = true;

						// Create new resetColPair for each collider so we can reset those specific ones to non-triggers when taken out of the backpack
						if (resetColPair == null)
						{
							resetColPair = new EFM_CustomItemWrapper.ResetColPair();
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
				item.SetParentage(itemObjectsRoot);
				item.RootRigidbody.isKinematic = true;

				// Add volume to backpack
				EFM_CustomItemWrapper primaryWrapper = item.gameObject.GetComponent<EFM_CustomItemWrapper>();
				if (primaryWrapper != null)
				{
					containingVolume += primaryWrapper.volumes[primaryWrapper.mode];
				}
				else
				{
					containingVolume += Mod.sizeVolumes[(int)item.Size];
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		private void TakeInput()
		{
			FVRViveHand hand = physicalObject.m_hand;
			if (hand.IsInStreamlinedMode)
			{
				if (hand.Input.AXButtonPressed)
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
						default:
							break;
					}
				}
			}
			else
			{
				Vector2 touchpadAxes = hand.Input.TouchpadAxes;

				// If touchpad has started being pressed this frame
				if (hand.Input.TouchpadDown)
				{
					Vector2 TouchpadClickInitiation = touchpadAxes;
					if (touchpadAxes.magnitude > 0.2f)
					{
						if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
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
								default:
									break;
							}
						}
					}
				}

				// If the touchpadd is being pressed
				if (hand.Input.TouchpadPressed)
				{
					if (touchpadAxes.magnitude > 0.2f)
					{
						if (Vector2.Angle(touchpadAxes, Vector2.down) <= 45f)
						{
							switch (itemType)
							{
								case Mod.ItemType.Consumable:
									bool otherHandConsuming = false;
                                    if (!validConsumePress)
                                    {
										otherHandConsuming = hand.OtherHand.GetComponent<EFM_Hand>().consuming;
                                    }
									if (!otherHandConsuming)
									{
										hand.GetComponent<EFM_Hand>().consuming = true;

										// Increment timer
										consumableTimer += Time.deltaTime;

										float use = consumableTimer / useTime;
										Mod.consumeUIText.text = ((int)(use * amountRate)).ToString() + "/" + amountRate;
										Mod.consumeUI.transform.position = hand.transform.position;
										Mod.consumeUI.transform.rotation = hand.transform.rotation;
										Mod.consumeUI.SetActive(true);
										validConsumePress = true;
									}
									break;
								default:
									break;
							}
                        }
                        else
                        {
							// Cancel consumable timer
							consumableTimer = 0;
							validConsumePress = false;
							hand.GetComponent<EFM_Hand>().consuming = false;
							Mod.consumeUI.SetActive(false);
						}
					}
				}

				// If touchpad has been released this frame
				if (hand.Input.TouchpadUp)
				{
                    // If last frame the consume button was being pressed
                    if (validConsumePress)
                    {
						validConsumePress = false;
						hand.GetComponent<EFM_Hand>().consuming = false;
						Mod.consumeUI.SetActive(false);

						if (amountRate == -1)
                        {
							if(maxAmount > 0)
							{
								// Consume for the fraction timer/useTime of remaining amount. If timer >= useTime, we consume the whole thing
								int amountToConsume = consumableTimer >= useTime ? maxAmount : (int)(amount * (consumableTimer / useTime));
								if(amount - amountToConsume == 0)
                                {
									// Attempt to apply effects at full effectiveness
									// Consume if succesful
									if (ApplyEffects(1, amountToConsume))
                                    {
										amount = 0;
										Destroy(gameObject);
									}
                                }
                                else
								{
									// Apply effects at partial effectiveness
									if (ApplyEffects(amountToConsume / maxAmount, amountToConsume))
									{
										amount -= amountToConsume;
									}
								}
							}
							else
                            {
								// Consume the whole thing if timer >= useTime because this is a single unit consumable
								if(consumableTimer >= useTime)
                                {
									// Attempt to apply effects at full effectiveness
									// Consume if succesful
									if (ApplyEffects(1, 0))
									{
										amount = 0;
										Destroy(gameObject);
									}
								}
                            }
                        }
						else if(amountRate == 0)
                        {
							// This consumable is multiple units but can only use one at a time, so consume one unit of it if timer >= useTime
							if (consumableTimer >= useTime)
							{
								if (ApplyEffects(1, 1))
								{
									amount -= 1;
								}
								// Apply effects at full effectiveness
							}
						}
                        else
                        {
							// Consume timer/useTime of to amountRate
							int amountToConsume = consumableTimer >= useTime ? (int)amountRate : (int)(amountRate * (consumableTimer / useTime));
							if (consumableTimer >= useTime)
							{
								// Apply effects at full effectiveness
								if (ApplyEffects(1, amountToConsume))
								{
									amount -= amountToConsume;
								}
							}
							else
							{
								// Apply effects at effectiveness * amountToConsume / amountRate
								if (ApplyEffects(amountToConsume / amountRate, amountToConsume))
								{
									amount -= amountToConsume;
								}
							}
						}

						if (amount == 0)
						{
							Destroy(gameObject);
						}
					}
				}
			}
		}

		private bool ApplyEffects(float effectiveness, int amountToConsume)
        {
			// TODO: Set targetted part if hovering over a part


			bool singleEffect = consumeEffects.Count + effects.Count == 1;
			int unusedEffectCount = 0;

			// Apply consume effects
			foreach (EFM_Effect_Consumable consumeEffect in consumeEffects)
            {
                switch (consumeEffect.effectType)
                {
					// Health
					case EFM_Effect_Consumable.EffectConsumable.Hydration:
						Mod.hydration += consumeEffect.value;
						break;
					case EFM_Effect_Consumable.EffectConsumable.Energy:
						Mod.energy += consumeEffect.value;
						break;

					// Damage
					case EFM_Effect_Consumable.EffectConsumable.RadExposure:
						if(consumeEffect.duration == 0)
                        {
							// Remove all rad exposure effects
							bool radExposureApplied = false;
							for(int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
                            {
								if (consumeEffect.cost <= amount - amountToConsume)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.RadExposure)
									{
										EFM_Effect.effects.RemoveAt(i);
										radExposureApplied = true;
										amount -= consumeEffect.cost;
										if(amount - amountToConsume == 0)
                                        {
											return true;
                                        }
									}
                                }
                                else
                                {
									break;
                                }
                            }
                            if (!radExposureApplied)
                            {
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
								if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.RadExposure)
								{
									EFM_Effect.effects[i].active = false;
									EFM_Effect.effects[i].inactiveTimer = consumeEffect.duration * effectiveness;
								}
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Pain:
						// Make all pain effects inactive for the duration
						for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
						{
							if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.Pain)
							{
								EFM_Effect.effects[i].active = false;
								EFM_Effect.effects[i].inactiveTimer = consumeEffect.duration * effectiveness;
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Contusion:
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
									amount -= consumeEffect.cost;
									if (amount - amountToConsume == 0)
									{
										return true;
									}
								}
                            }
                            else
                            {
								break;
                            }
						}
                        if (!contusionApplied)
						{
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
									if (amount - amountToConsume == 0)
									{
										return true;
									}
								}
                            }
                            else
                            {
								break;
                            }
						}
						if (!intoxicationApplied)
						{
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
						// Remove priority LightBleeding effect, or one found on targetted part
						if (targettedPart == -1)
						{
							// Prioritize lowest partIndex
							int highest = -1;
							int lowestPartIndex = 7;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = EFM_Effect.effects.Count - 1; i >= 0; --i)
								{
									if (EFM_Effect.effects[i].effectType == EFM_Effect.EffectType.LightBleeding && EFM_Effect.effects[i].partIndex < lowestPartIndex)
									{
										highest = i;
									}
								}
							}
							if(highest == -1) // We did not find light bleeding
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
								EFM_Effect.effects.RemoveAt(highest);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
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
								EFM_Effect.effects.RemoveAt(index);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.Fracture:
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
								EFM_Effect.effects.RemoveAt(index);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
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
								EFM_Effect.effects.RemoveAt(index);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.DestroyedPart:
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
								EFM_Effect.effects.RemoveAt(highest);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
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
								EFM_Effect.effects.RemoveAt(index);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
							}
						}
						break;
					case EFM_Effect_Consumable.EffectConsumable.HeavyBleeding:
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
								EFM_Effect.effects.RemoveAt(highest);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
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
								EFM_Effect.effects.RemoveAt(index);
								amount -= consumeEffect.cost;
								if (amount - amountToConsume == 0)
								{
									return true;
								}
							}
						}
						break;
				}
            }

			// Apply buffs
			foreach(EFM_Effect_Buff buff in effects)
            {
				if (buff.chance != 1 && Random.value > buff.chance)
				{
					continue;
				}

				// Set effect
				EFM_Effect effect = new EFM_Effect();
				effect.effectType = EFM_Effect.EffectType.SkillRate;

				effect.value = buff.value;
				effect.timer = buff.duration;
				effect.hasTimer = buff.duration > 0;
				effect.delay = buff.delay;

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
            }

			// Return true if at least one effect was used
			return unusedEffectCount != consumeEffects.Count + effects.Count; 
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
				}
				else if (itemType == Mod.ItemType.Container || itemType == Mod.ItemType.Pouch)
				{
					SetContainerOpen(true, isRightHand);
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
							if (itemsInSlots != null)
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
				}
				else if (itemType == Mod.ItemType.BodyArmor)
				{
					SetMode(1);
				}
				else if (itemType == Mod.ItemType.Container || itemType == Mod.ItemType.Pouch)
				{
					SetContainerOpen(false, isRightHand);
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
					physicalObject.SetQuickBeltSlot(rigSlots[i]);
					physicalObject.SetParentage(rigSlots[i].gameObject.transform);
					physicalObject.transform.localScale = Vector3.one;
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
					itemsInSlots[i].transform.parent = itemObjectsRoot;
				}
			}
			GM.CurrentPlayerBody.ConfigureQuickbelt(-1);
		}

		private void SetContainerOpen(bool open, bool isRightHand = false)
		{
			mainContainer.SetActive(open);
			itemObjectsRoot.gameObject.SetActive(open);
		}

		public void UpdateStackModel()
        {
			float stackFraction = maxStack / (float)_stack;
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
			descriptionPack.amount = Mod.baseInventory[ID] + Mod.playerInventory[ID];
			for(int i=0; i < 22; ++i)
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
			if (maxStack > 1)
			{
				descriptionPack.stack = stack;
				descriptionPack.maxStack = maxStack;
			}
			else if(maxAmount > 0)
			{
				descriptionPack.stack = amount;
				descriptionPack.maxStack = maxAmount;
			}
			descriptionPack.amountRequiredQuest = Mod.requiredForQuest[ID];

			return descriptionPack;
        }
	}
}
