using FistVR;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
	public class MeatovItem : MonoBehaviour, IDescribable
	{
		public FVRPhysicalObject physObj;

        public enum ItemType
        {
            Generic = 0,
            BodyArmor = 1,
            Rig = 2,
            ArmoredRig = 3,
            Helmet = 4,
            Backpack = 5,
            Container = 6,
            Pouch = 7,
            AmmoBox = 8,
            Money = 9,
            Consumable = 10,
            Key = 11,
            Earpiece = 12,
            FaceCover = 13,
            Eyewear = 14,
            Headwear = 15,

            LootContainer = 16,

            DogTag = 17
        }

        public enum WeaponClass
        {
            Pistol = 0,
            Revolver = 1,
            SMG = 2,
            Assault = 3,
            Shotgun = 4,
            Sniper = 5,
            LMG = 6,
            HMG = 7,
            Launcher = 8,
            AttachedLauncher = 9,
            DMR = 10
        }

        public enum ItemRarity
        {
            Common,
            Rare,
            Superrare,
            Not_exist
        }

        [Header("General")]
        public string H3ID;
        public string tarkovID;
        [NonSerialized]
        public bool vanilla;
        public ItemType itemType;
		public List<string> parents;
        public int weight;
		public int lootExperience;
		public ItemRarity rarity;
		public string itemName;
		public string description;
		public AudioClip[] itemSounds;
        [NonSerialized]
		public int upgradeCheckBlockedIndex = -1;
        [NonSerialized]
        public int upgradeCheckWarnedIndex = -1;
        [NonSerialized]
        public int compatibilityValue; // 0: Does not need mag or round, 1: Needs mag, 2: Needs round, 3: Needs both
        [NonSerialized]
        public bool usesMags; // Could be clip
        [NonSerialized]
        public bool usesAmmoContainers; // Could be internal mag or revolver
        [NonSerialized]
        public FireArmMagazineType magType;
        [NonSerialized]
        public FireArmClipType clipType;
        [NonSerialized]
        public FireArmRoundType roundType;
        [NonSerialized]
        public bool inAll;
        [NonSerialized]
        public int creditCost; // Value of item in rubles
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
        [NonSerialized]
        public bool destroyed;
        [NonSerialized]
        public bool looted;
        [NonSerialized]
        public bool foundInRaid;
        [NonSerialized]
        public bool hideoutSpawned;
        private DescriptionPack descriptionPack;
        [NonSerialized]
        private long previousDescriptionTime;
        [NonSerialized]
        public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
        [NonSerialized]
        public int locationIndex; // 0: Player inventory, 1: Base, 2: Raid, 3: Area slot. This is to keep track of where an item is in general
        [NonSerialized]
        public DescriptionManager descriptionManager; // The current description manager displaying this item's description
        [NonSerialized]
        public List<MarketItemView> marketItemViews;
        //public LeaveItemProcessor leaveItemProcessor;

        // Equipment
        [Header("Equipment")]
        // 0: Open (Model should be as orginal in tarkov), 1: ClosedFull (Closed but only folded to the point that any container/armor is not folded), 2: ClosedEmpty (Folded and flattened as much as is realistic)
        [NonSerialized]
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
        [Header("Helmet")]
        public bool blocksEarpiece;
		public bool blocksEyewear;
		public bool blocksFaceCover;
		public bool blocksHeadwear;

        // Armor
        [Header("Armor")]
        [NonSerialized]
		public bool broken;
		public float coverage;
		public float damageResist;
		public float maxArmor;
        [NonSerialized]
		public float armor;

        // Rig and Backpacks
        [Header("Rig and Backpack")]
        [NonSerialized]
		public bool open;
		public Transform rightHandPoseOverride;
		public Transform leftHandPoseOverride;

        // Rig
        [Header("Rig")]
        [NonSerialized]
		public int configurationIndex;
        [NonSerialized]
		public GameObject[] itemsInSlots;
		public Transform configurationRoot;
		public List<FVRQuickBeltSlot> rigSlots;
        [NonSerialized]
		private int activeSlotsSetIndex;

        // Backpacks, Containers, Pouches
        [Header("Containers")]
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
        [Serializable]
		public class ResetColPair
		{
			public FVRPhysicalObject physObj;
			public List<Collider> colliders;
		}
		public List<ResetColPair> resetColPairs;
		public List<string> whiteList;
		public List<string> blackList;

        // AmmoBox
        [Header("Ammo box")]
        public string cartridge;
		public FireArmRoundClass roundClass;

        // Stack
        [Header("Stack data")]
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
        [NonSerialized]
		private bool splittingStack;
        [NonSerialized]
        private int splitAmount;
        [NonSerialized]
        private Vector3 stackSplitStartPosition;
        [NonSerialized]
        private Vector3 stackSplitRightVector;

        // Amount
        [Header("Amount data")]
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
        [Header("Consumable")]
        public float useTime = 0; // Amount of time it takes to use amountRate
		public float amountRate = -1; // Maximum amount that can be used from the consumable in single use ex.: grizzly can only be used to heal up to 175 hp per use. 0 a single unit of multiple, -1 means no limit up to maxAmount
		public List<BuffEffect> effects; // Gives new effects
		public List<ConsumableEffect> consumeEffects; // Immediate effects or effects that modify/override/give new effects 
        [NonSerialized]
        private float consumableTimer;
        [NonSerialized]
        private bool validConsumePress;
        [NonSerialized]
        private bool validLeaveItemPress;
        [NonSerialized]
        private float leaveItemTime = -1;
        [NonSerialized]
        private float leavingTimer;
        [NonSerialized]
        public int targettedPart = -1; // TODO: Implement
		private static int breakPartExperience = -1;
		private static int painExperience = -1;
		private static int intoxicationExperience = -1;
		private static int lightBleedExperience = -1;
		private static int fractureExperience = -1;
		private static int heavyBleedExperience = -1;

        // DogTag
        [Header("Dogtag")]
        public bool USEC;
        [NonSerialized]
        public int dogtagLevel = 1;
        [NonSerialized]
        public string dogtagName;

        // Weapon
        [NonSerialized]
        public WeaponClass weaponClass;

        public static void Setup(FVRPhysicalObject physicalObject)
        {
            MeatovItem meatovItem = physicalObject.gameObject.AddComponent<MeatovItem>();
            JObject defaultItemDataEntry = Mod.vanillaItemData["NewVanillaItems"][physicalObject.ObjectWrapper.ItemID] as JObject;
            meatovItem.tarkovID = defaultItemDataEntry["TarkovID"].ToString();
            meatovItem.description = defaultItemDataEntry["Description"].ToString();
            meatovItem.lootExperience = (int)defaultItemDataEntry["LootExperience"];
            meatovItem.creditCost = (int)defaultItemDataEntry["CreditCost"];
            meatovItem.parents = ((JArray)defaultItemDataEntry["parents"]).ToObject<List<string>>();
            meatovItem.volumes = new int[1];
            meatovItem.volumes[0] = (int)((float)defaultItemDataEntry["Volume"] * Mod.volumePrecisionMultiplier); 

            if (physicalObject is FVRFireArm)
            {
                FVRFireArm asFireArm = physicalObject as FVRFireArm;

                // Set chamber firearm vars
                // TODO: Check if this is necessary, shouldnt already be done by h3?
                if (asFireArm.GetChambers() != null && asFireArm.GetChambers().Count > 0)
                {
                    foreach (FVRFireArmChamber chamber in asFireArm.GetChambers())
                    {
                        chamber.Firearm = asFireArm;
                    }
                }

                meatovItem.compatibilityValue = 3;
                if (asFireArm.UsesMagazines)
                {
                    meatovItem.usesAmmoContainers = true;
                    meatovItem.usesMags = true;
                    meatovItem.magType = asFireArm.MagazineType;
                }
                else if (asFireArm.UsesClips)
                {
                    meatovItem.usesAmmoContainers = true;
                    meatovItem.usesMags = false;
                    meatovItem.clipType = asFireArm.ClipType;
                }
                meatovItem.roundType = asFireArm.RoundType;
            }
            else if (meatovItem.physObj is FVRFireArmMagazine)
            {
                meatovItem.compatibilityValue = 1;
                meatovItem.roundType = (physicalObject as FVRFireArmMagazine).RoundType;
            }
            else if (physicalObject is FVRFireArmClip)
            {
                meatovItem.compatibilityValue = 1;
                meatovItem.roundType = (physicalObject as FVRFireArmClip).RoundType;
            }
            else if (physicalObject is Speedloader)
            {
                meatovItem.compatibilityValue = 1;
                meatovItem.roundType = (physicalObject as Speedloader).Chambers[0].Type;
            }
            else if (physicalObject is FVRFireArmRound)
            {
                Mod.usedRoundIDs.Add(meatovItem.H3ID);

                // TODO: Figure out how to get a round's compatible mags efficiently so we can list them in the round's description
            }

            // Set weapon class
            foreach (string parent in meatovItem.parents)
            {
                if (parent.Equals("5447b5cf4bdc2d65278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.Pistol;
                    break;
                }
                else if (parent.Equals("617f1ef5e8b54b0998387733"))
                {
                    meatovItem.weaponClass = WeaponClass.Revolver;
                    break;
                }
                else if (parent.Equals("5447b5e04bdc2d62278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.SMG;
                    break;
                }
                else if (parent.Equals("5447b5f14bdc2d61278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.Assault;
                    break;
                }
                else if (parent.Equals("5447b6094bdc2dc3278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.Shotgun;
                    break;
                }
                else if (parent.Equals("5447b6254bdc2dc3278b4568"))
                {
                    meatovItem.weaponClass = WeaponClass.Sniper;
                    break;
                }
                else if (parent.Equals("5447b5fc4bdc2d87278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.LMG;
                    break;
                }
                else if (parent.Equals("5447bed64bdc2d97278b4568"))
                {
                    meatovItem.weaponClass = WeaponClass.HMG;
                    break;
                }
                else if (parent.Equals("5447bedf4bdc2d87278b4568"))
                {
                    meatovItem.weaponClass = WeaponClass.Launcher;
                    break;
                }
                else if (parent.Equals("5447bee84bdc2dc3278b4569"))
                {
                    meatovItem.weaponClass = WeaponClass.AttachedLauncher;
                    break;
                }
                else if (parent.Equals("5447b6194bdc2d67278b4567"))
                {
                    meatovItem.weaponClass = WeaponClass.DMR;
                    break;
                }
                continue;
            }
        }

		private void Awake()
		{
            if(physObj == null)
            {
                physObj = gameObject.GetComponent<FVRPhysicalObject>();
            }

            Mod.meatovItemByInteractive.Add(physObj, this);

            if (physObj.RootRigidbody == null)
            {
                // This has to be done because attachments and ammo container, when attached to a firearm, will have their rigidbodies detroyed
                weight = (int)(((FVRPhysicalObject.RigidbodyStoredParams)typeof(FVRPhysicalObject).GetField("StoredRBParams").GetValue(physObj)).Mass * 1000);
            }
            else
            {
                weight = (int)(physObj.RootRigidbody.mass * 1000);
            }

            if (itemType != ItemType.LootContainer)
			{
				_mode = volumes.Length - 1; // Set default mode to the last index of volumes, closed empty for containers and rigs
			}
			modeInitialized = true;

			descriptionPack = new DescriptionPack();
			if (itemType == ItemType.LootContainer)
			{
				descriptionPack.itemType = ItemType.LootContainer;
			}
			else
			{
				if (takeCurrentLocation)
				{
					locationIndex = Mod.currentLocationIndex;
				}

				descriptionPack.ID = H3ID;
				descriptionPack.isCustom = !vanilla;
				descriptionPack.isPhysical = true;
				descriptionPack.MI = this;
				descriptionPack.name = itemName;
				descriptionPack.description = description;
				descriptionPack.amountRequiredPerArea = new int[22];

                if (Mod.itemIcons.TryGetValue(H3ID, out Sprite icon))
                {
                    descriptionPack.icon = icon;
                }
                else
                {
                    descriptionPack.icon = physObj is FVRFireArmRound ? Mod.cartridgeIcon : IM.GetSpawnerID(physObj.ObjectWrapper.SpawnedFromId).Sprite;
                }

                SetCurrentWeight(this);
			}
		}

		public static int SetCurrentWeight(MeatovItem item)
		{
			if (item == null)
			{
				return 0;
			}

			item.currentWeight = item.weight;

			if (item.itemType == ItemType.Rig || item.itemType == ItemType.ArmoredRig)
			{
				foreach (GameObject containedItem in item.itemsInSlots) 
				{
					if(containedItem != null)
                    {
						item.currentWeight += SetCurrentWeight(containedItem.GetComponent<MeatovItem>());
                    }
				}
			}
			else if(item.itemType == ItemType.Backpack || item.itemType == ItemType.Container || item.itemType == ItemType.Pouch)
			{
				foreach (Transform containedItem in item.containerItemRoot)
				{
					if (containedItem != null)
					{
						item.currentWeight += SetCurrentWeight(containedItem.GetComponent<MeatovItem>());
					}
				}
			}
			else if(item.itemType == ItemType.AmmoBox)
			{
				// TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
				// locationIndex so we know when to add/remove weight from player also
				FVRFireArmMagazine magazine = item.GetComponent<FVRFireArmMagazine>();
				if (magazine != null)
				{
					//item.currentWeight += 15 * magazine.m_numRounds;
				}
            }
            else if (item.physObj is FVRFireArm)
            {
                FVRFireArm asFireArm = (FVRFireArm)item.physObj;

                // Considering 0.015g per round
                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * (asFireArm.GetChamberRoundList() == null ? 0 : asFireArm.GetChamberRoundList().Count);

                // Ammo container
                if (asFireArm.UsesMagazines && asFireArm.Magazine != null)
                {
                    MeatovItem magMI = asFireArm.Magazine.GetComponent<MeatovItem>();
                    if (magMI != null)
                    {
                        item.currentWeight += SetCurrentWeight(magMI);
                    }
                }
                else if (asFireArm.UsesClips && asFireArm.Clip != null)
                {
                    MeatovItem clipMI = asFireArm.Clip.GetComponent<MeatovItem>();
                    if (clipMI != null)
                    {
                        item.currentWeight += SetCurrentWeight(clipMI);
                    }
                }

                // Attachments
                if (asFireArm.Attachments != null && asFireArm.Attachments.Count > 0)
                {
                    foreach (FVRFireArmAttachment attachment in asFireArm.Attachments)
                    {
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<MeatovItem>());
                    }
                }
            }
            else if (item.physObj is FVRFireArmAttachment)
            {
                FVRFireArmAttachment asFireArmAttachment = (FVRFireArmAttachment)item.physObj;

                if (asFireArmAttachment.Attachments != null && asFireArmAttachment.Attachments.Count > 0)
                {
                    foreach (FVRFireArmAttachment attachment in asFireArmAttachment.Attachments)
                    {
                        item.currentWeight += SetCurrentWeight(attachment.GetComponent<MeatovItem>());
                    }
                }
            }
            else if (item.physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asFireArmMagazine = (FVRFireArmMagazine)item.physObj;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * asFireArmMagazine.m_numRounds;
            }
            else if (item.physObj is FVRFireArmClip)
            {
                FVRFireArmClip asFireArmClip = (FVRFireArmClip)item.physObj;

                // TODO: Ammo container weight management will have to be reviewed. If we want to manage it, we will need to also keep track of the round and container's
                // locationIndex so we know when to add/remove weight from player also
                //item.currentWeight += 15 * asFireArmClip.m_numRounds;
            }
            else if (item.GetComponentInChildren<M203>() != null)
            {
                M203 m203 = item.GetComponentInChildren<M203>();
                item.currentWeight += m203.Chamber.IsFull ? 100 : 0;
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
			if (itemType != ItemType.Backpack &&
			   itemType != ItemType.Container &&
			   itemType != ItemType.Pouch)
			{
				return false;
			}

			// Get item volume
			int volumeToUse = 0;
			int weightToUse = 0;
			string IDToUse = "";
			List<string> parentsToUse = null;
			MeatovItem wrapper = item.GetComponent<MeatovItem>();
			if (wrapper != null)
			{
                if (!wrapper.modeInitialized)
                {
					wrapper.mode = wrapper.volumes.Length - 1;
					wrapper.modeInitialized = true;

				}
				volumeToUse = wrapper.volumes[wrapper.mode];
				weightToUse = wrapper.currentWeight;
				IDToUse = wrapper.H3ID;
				parentsToUse = wrapper.parents;
			}

			if (containingVolume + volumeToUse <= maxVolume && Mod.IDDescribedInList(IDToUse, parentsToUse, whiteList, blackList))
			{
				// Attach item to container
				// Set all non trigger colliders that are on default layer to trigger so they dont collide with anything
				Collider[] cols = item.gameObject.GetComponentsInChildren<Collider>(true);
				if (resetColPairs == null)
				{
					resetColPairs = new List<MeatovItem.ResetColPair>();
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
					Mod.RemoveFromAll(item, wrapper);
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

		public void TakeInput(FVRViveHand hand, Hand EFMHand)
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
				//if (leaveItemProcessor == null)
				//{
				//	switch (itemType)
				//	{
				//		case ItemType.ArmoredRig:
				//		case ItemType.Rig:
				//		case ItemType.Backpack:
				//		case ItemType.BodyArmor:
				//		case ItemType.Container:
				//		case ItemType.Pouch:
				//			ToggleMode(true, hand.IsThisTheRightHand);
				//			break;
				//		case ItemType.Money:
				//			if (splittingStack)
				//			{
				//				// End splitting
				//				if (splitAmount != stack || splitAmount == 0)
				//				{
				//					stack -= splitAmount;

				//					GameObject itemObject = Instantiate(Mod.itemPrefabs[int.Parse(H3ID)], hand.transform.position + hand.transform.forward * 0.2f, Quaternion.identity);
				//					if (Mod.currentLocationIndex == 1) // In hideout
				//					{
				//						itemObject.transform.parent = HideoutController.instance.transform.GetChild(HideoutController.instance.transform.childCount - 2);
				//						MeatovItem CIW = itemObject.GetComponent<MeatovItem>();
				//						CIW.stack = splitAmount;
				//						HideoutController.instance.inventoryObjects[H3ID].Add(itemObject);
				//					}
				//					else // In raid
				//					{
				//						itemObject.transform.parent = Mod.currentRaidManager.transform.GetChild(1).GetChild(1).GetChild(2);
				//						MeatovItem CIW = itemObject.GetComponent<MeatovItem>();
				//						CIW.stack = splitAmount;
				//					}
				//				}
				//				// else the chosen amount is 0 or max, meaning cancel the split
				//				CancelSplit();
				//			}
				//			else
				//			{
				//				// Start splitting
				//				Mod.stackSplitUI.SetActive(true);
				//				Mod.stackSplitUI.transform.position = hand.transform.position + hand.transform.forward * 0.2f;
				//				Mod.stackSplitUI.transform.rotation = Quaternion.Euler(0, hand.transform.eulerAngles.y, 0);
				//				stackSplitStartPosition = hand.transform.position;
				//				stackSplitRightVector = hand.transform.right;
				//				stackSplitRightVector.y = 0;

				//				splittingStack = true;
				//				Mod.amountChoiceUIUp = true;
				//				Mod.splittingItem = this;
				//			}
				//			break;
				//		default:
				//			break;
				//	}
    //            }
			}

			// If A is being pressed this frame
			if (usageButtonPressed)
			{
				//if (leaveItemProcessor == null || validConsumePress)
				//{
				//	leavingTimer = 0;
				//	switch (itemType)
				//	{
				//		case ItemType.Consumable:
				//			bool otherHandConsuming = false;
				//			if (!validConsumePress)
				//			{
				//				otherHandConsuming = EFMHand.otherHand.consuming;
				//			}
				//			if (!otherHandConsuming)
				//			{
				//				EFMHand.consuming = true;

				//				// Increment timer
				//				consumableTimer += Time.deltaTime;

				//				float use = Mathf.Clamp01(consumableTimer / useTime);
				//				Mod.consumeUIText.text = string.Format("{0:0.#}/{1:0.#}", amountRate <= 0 ? (use * amount) : (use * amountRate), amountRate <= 0 ? amount : amountRate);
				//				if (amountRate == 0)
				//				{
				//					// This consumable is discrete units and can only use one at a time, so set text to red until we have reached useTime, then set it to green
				//					if (consumableTimer >= useTime)
				//					{
				//						Mod.consumeUIText.color = Color.green;
				//					}
				//					else
				//					{
				//						Mod.consumeUIText.color = Color.red;
				//					}
				//				}
				//				else
				//				{
				//					Mod.consumeUIText.color = Color.white;
				//				}
				//				Mod.consumeUI.transform.parent = hand.transform;
				//				Mod.consumeUI.transform.localPosition = new Vector3(hand.IsThisTheRightHand ? -0.15f : 0.15f, 0, 0);
				//				Mod.consumeUI.transform.localRotation = Quaternion.Euler(25, 0, 0);
				//				Mod.consumeUI.SetActive(true);
				//				validConsumePress = true;
				//			}
				//			break;
				//		default:
				//			break;
				//	}
    //            }
    //            else if(leaveItemProcessor != null)
    //            {
				//	bool otherHandLeaving = false;
				//	if (!validLeaveItemPress)
				//	{
				//		otherHandLeaving = EFMHand.otherHand.leaving;
				//	}
				//	if (!otherHandLeaving)
				//	{
				//		EFMHand.leaving = true;

				//		// Increment timer
				//		leavingTimer += Time.deltaTime;

				//		if(leaveItemTime == -1)
				//		{
				//			List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[H3ID];
				//			leaveItemTime = conditions[conditions.Count - 1].plantTime;
				//		}

				//		Mod.consumeUIText.text = string.Format("{0:0.#}/{1:0.#}", leavingTimer, leaveItemTime);
				//		Mod.consumeUIText.color = Color.white;
				//		Mod.consumeUI.transform.parent = hand.transform;
				//		Mod.consumeUI.transform.localPosition = new Vector3(hand.IsThisTheRightHand ? -0.15f : 0.15f, 0, 0);
				//		Mod.consumeUI.transform.localRotation = Quaternion.Euler(25, 0, 0);
				//		Mod.consumeUI.SetActive(true);
				//		validLeaveItemPress = true;

				//		if (leavingTimer >= leaveItemTime)
				//		{
				//			EFMHand.leaving = false;
				//			validLeaveItemPress = false;
				//			leavingTimer = 0;
				//			leaveItemTime = -1;

				//			List<TraderTaskCondition> conditions = leaveItemProcessor.conditionsByItemID[H3ID];
				//			if(conditions.Count > 0)
				//			{
				//				TraderTaskCondition condition = conditions[conditions.Count - 1];
				//				++condition.itemCount;
				//				TraderStatus.UpdateConditionFulfillment(condition);
				//				if (condition.fulfilled)
				//				{
				//					conditions.RemoveAt(conditions.Count - 1);
				//				}
				//			}
				//			if (conditions.Count == 0)
				//			{
				//				leaveItemProcessor.conditionsByItemID.Remove(H3ID);
				//				leaveItemProcessor.itemIDs.Remove(H3ID);
				//			}

				//			// Update player inventory and weight
				//			Mod.RemoveFromPlayerInventory(transform, true);
				//			Mod.weight -= currentWeight;
				//			destroyed = true;
				//			physObj.ForceBreakInteraction();
				//			Destroy(gameObject);
				//		}
				//	}
				//}
			}

			// If A has been released this frame
			if (usageButtonUp)
			{
				// If last frame the consume button was being pressed
				if (validConsumePress)
				{
					validConsumePress = false;
					hand.GetComponent<Hand>().consuming = false;
					Mod.consumeUI.SetActive(false);
					Mod.LogInfo("Valid consume released");

					if (amountRate == -1)
					{
						Mod.LogInfo("\tAmount rate -1");
						if (maxAmount > 0)
						{
							Mod.LogInfo("\t\tConsuming, amountRate == -1, maxAmount > 0, consumableTimer: " + consumableTimer + ", useTime: " + useTime);
							// Consume for the fraction timer/useTime of remaining amount. If timer >= useTime, we consume the whole thing
							int amountToConsume = consumableTimer >= useTime ? amount : (int)(amount * (consumableTimer / useTime));
							Mod.LogInfo("\t\tAmount to consume: " + amountToConsume);
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
						Mod.LogInfo("\tAmount rate 0");
						// This consumable is discrete units and can only use one at a time, so consume one unit of it if timer >= useTime
						if (consumableTimer >= useTime)
						{
							Mod.LogInfo("\t\tConsumable timer >= useTime");
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
						Mod.LogInfo("\tAmount rate else");
						// Consume timer/useTime of to amountRate
						int amountToConsume = consumableTimer >= useTime ? (int)amountRate : (int)(amountRate * (consumableTimer / useTime));
						Mod.LogInfo("\tAmount to consume: "+ amountToConsume);
						if (consumableTimer >= useTime)
						{
							Mod.LogInfo("\t\tConsumable timer >= useTime");
							// Apply effects at full effectiveness
							// NOTE: In the case we have an amount rate, here, we only remove the used amount if no other effects  have been applied
							// so only if ApplyEffects returns false.
							if (!ApplyEffects(1, amountToConsume))
							{
								Mod.LogInfo("\t\t\tNo effects applied, using consumed amount to heal");
								// Here we also have to apply amount consumed as health to relevant parts
								// NOTE: This assumes that only items that can heal health have an amountRate != 0 || -1 defined
								// If this ever changes, we will need to have an additional flag for healing items
								int actualAmountConsumed = 0;
								int partIndex = -1;
								if (targettedPart != -1)
								{
									partIndex = targettedPart;
									Mod.LogInfo("\t\t\t\tPart "+partIndex+" targetted");
								}
								else // No part targetted, prioritize least health TODO: Make a setting to prioritize by part first instead, and as we go through more important parts first, those will be prioritized if health is equal
								{
									Mod.LogInfo("\t\t\t\tNo part targetted, finding best...");
									int leastIndex = -1;
									float leastAmount = 1000;
									for (int i = 0; i < Mod.health.Length; ++i)
									{
										if (Mod.health[i] < Mod.currentMaxHealth[i] && Mod.health[i] < leastAmount)
										{
											leastIndex = i;
											leastAmount = Mod.health[i];
										}
									}
									if (leastIndex >= 0)
									{
										partIndex = leastIndex;
									}
									Mod.LogInfo("\t\t\t\tBest part to apply helth to: "+partIndex);
								}

								if (partIndex != -1)
								{
									Mod.LogInfo("\t\t\t\tApplying "+actualAmountConsumed+" to "+partIndex+", which has "+ Mod.health[partIndex]+" health");
									actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - Mod.health[partIndex]));
									Mod.health[partIndex] = Mathf.Min(Mod.currentMaxHealth[partIndex], Mod.health[partIndex] + actualAmountConsumed);
									Mod.LogInfo("\t\t\t\tAfter healing: "+ Mod.health[partIndex]);
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
							Mod.LogInfo("\t\tConsumable timer < useTime");

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
										if (Mod.health[i] < Mod.currentMaxHealth[i] && Mod.health[i] < leastAmount)
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
							//foreach (BaseAreaManager areaManager in HideoutController.instance.baseAreaManagers)
							//{
							//	areaManager.UpdateBasedOnItem(H3ID);
							//}
						}
					}
				}
			}
		}

		private void UpdateUseItemCounterConditions()
        {
   //         if (Mod.currentUseItemCounterConditionsByItemID.ContainsKey(H3ID))
   //         {
			//	List<TraderTaskCounterCondition> useItemCounterConditions = Mod.currentUseItemCounterConditionsByItemID[H3ID];
			//	foreach (TraderTaskCounterCondition counterCondition in useItemCounterConditions)
			//	{
			//		// Check task and condition state validity
			//		if (!counterCondition.parentCondition.visible)
			//		{
			//			continue;
			//		}

			//		// Check constraint counters (Location, Equipment, HealthEffect, InZone)
			//		bool constrained = false;
			//		foreach (TraderTaskCounterCondition otherCounterCondition in counterCondition.parentCondition.counters)
			//		{
			//			if (!TraderStatus.CheckCounterConditionConstraint(otherCounterCondition))
			//			{
			//				constrained = true;
			//				break;
			//			}
			//		}
			//		if (constrained)
			//		{
			//			continue;
			//		}

			//		// Successful use, increment count and update fulfillment 
			//		++counterCondition.useCount;
			//		TraderStatus.UpdateCounterConditionFulfillment(counterCondition);
			//	}
			//}
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
			Mod.LogInfo("Apply effects called, effectiveness: " + effectiveness + ", amount to consume: " + amountToConsume);
			if(consumeEffects == null)
            {
				consumeEffects = new List<ConsumableEffect>();
				effects = new List<BuffEffect>();
            }

			// TODO: Set targetted part if hovering over a part

			int appliedEffectCount = 0;
			bool singleEffect = consumeEffects.Count + effects.Count == 1;
			int unusedEffectCount = 0;

			// Apply consume effects
			foreach (ConsumableEffect consumeEffect in consumeEffects)
			{
				switch (consumeEffect.effectType)
                {
					// Health
					case ConsumableEffect.ConsumableEffectType.Hydration:
						float hydrationAmount = consumeEffect.value * effectiveness;
						Mod.hydration += hydrationAmount;
						Mod.AddSkillExp(hydrationAmount * (Skill.hydrationRecoveryRate / 100), 5);
						++appliedEffectCount;
						Mod.LogInfo("\tApplied hydration");
						break;
					case ConsumableEffect.ConsumableEffectType.Energy:
						float energyAmount = consumeEffect.value * effectiveness;
						Mod.energy += energyAmount;
						Mod.AddSkillExp(energyAmount * (Skill.energyRecoveryRate / 100), 5);
						++appliedEffectCount;
						Mod.LogInfo("\tApplied energy");
						break;

					// Damage
					case ConsumableEffect.ConsumableEffectType.RadExposure:
						Mod.LogInfo("\trad exposure");
						bool radExposureApplied = false;
						if (consumeEffect.duration == 0)
						{
							Mod.LogInfo("\t\tNo duration, curing rad exposure if present");
							// Remove all rad exposure effects
							for (int i = Effect.effects.Count - 1; i >= 0; --i)
                            {
								if (consumeEffect.cost == 0 || consumeEffect.cost <= amount - amountToConsume)
								{
									if (Effect.effects[i].effectType == Effect.EffectType.RadExposure)
									{
										Effect.effects.RemoveAt(i);
										radExposureApplied = true;
										++appliedEffectCount;
										amount -= consumeEffect.cost;
										Mod.LogInfo("\t\t\tFound valid rad exposure at effect index: "+i);
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
								Mod.LogInfo("\t\t\tNo valid rad exposure");
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
							for (int i = Effect.effects.Count - 1; i >= 0; --i)
							{
								if (Effect.effects[i].effectType == Effect.EffectType.RadExposure && Effect.effects[i].active)
								{
									Effect.effects[i].active = false;
									Effect.effects[i].inactiveTimer = consumeEffect.duration * effectiveness;
									radExposureApplied = true;
									++appliedEffectCount;
									Mod.LogInfo("RadExposure effect found, disabling for "+ Effect.effects[i].inactiveTimer);
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
					case ConsumableEffect.ConsumableEffectType.Pain:
						Mod.LogInfo("\tPain");
						if (painExperience == -1)
						{
							painExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Pain"]["HealExperience"];
						}
						// Make all pain effects inactive for the duration
						bool painApplied = false;
						for (int i = Effect.effects.Count - 1; i >= 0; --i)
						{
							if (Effect.effects[i].effectType == Effect.EffectType.Pain && Effect.effects[i].active)
							{
								Effect.effects[i].active = false;
								float timer = consumeEffect.duration * effectiveness;
								Effect.effects[i].inactiveTimer = timer + timer * (Skill.immunityPainKiller * (Mod.skills[6].currentProgress / 100) / 100);
								Mod.AddExperience((int)(painExperience * effectiveness), 2, "Treatment experience - Pain ({0})");
								painApplied = true;
								++appliedEffectCount;
								Mod.LogInfo("\t\tFound pain effect at effect index "+i);
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
					case ConsumableEffect.ConsumableEffectType.Contusion:
						Mod.LogInfo("\tContusion");
						// Remove all contusion effects
						bool contusionApplied = false;
						for (int i = Effect.effects.Count - 1; i >= 0; --i)
						{
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								if (Effect.effects[i].effectType == Effect.EffectType.Contusion)
								{
									Effect.effects.RemoveAt(i);
									contusionApplied = true;
									++appliedEffectCount;
									amount -= consumeEffect.cost;
									Mod.LogInfo("\t\tFound contusion effect at effect index " + i);
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
							Mod.LogInfo("\t\tNo valid contusion found");
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
					case ConsumableEffect.ConsumableEffectType.Intoxication:
						Mod.LogInfo("\tIntoxication");
						if (intoxicationExperience == -1)
						{
							intoxicationExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Intoxication"]["HealExperience"];
						}
						// Remove all Intoxication effects
						bool intoxicationApplied = false;
						for (int i = Effect.effects.Count - 1; i >= 0; --i)
						{
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								if (Effect.effects[i].effectType == Effect.EffectType.Intoxication)
								{
									Effect.effects.RemoveAt(i);
									amount -= consumeEffect.cost;
									intoxicationApplied = true;
									 ++appliedEffectCount;
									Mod.AddExperience(intoxicationExperience, 2, "Treatment experience - Intoxication ({0})");
									Mod.LogInfo("\t\tFound contusion effect at effect index " + i);
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
							Mod.LogInfo("\t\tNo valid intoxication found");
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
					case ConsumableEffect.ConsumableEffectType.LightBleeding:
						if (lightBleedExperience == -1)
						{
							lightBleedExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["LightBleeding"]["HealExperience"];
						}
						Mod.LogInfo("\tHas damage light bleeding effect");
						// Remove priority LightBleeding effect, or one found on targetted part
						if (targettedPart == -1)
						{
							Mod.LogInfo("\t\tNo targetted part, cost: "+consumeEffect.cost+", amount: "+amount+", to consume: "+amountToConsume);
							// Prioritize lowest partIndex
							int highest = -1;
							int lowestPartIndex = 7;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].effectType == Effect.EffectType.LightBleeding && Effect.effects[i].partIndex < lowestPartIndex)
									{
										Mod.LogInfo("\t\t\tFound valid light bleeding");
										highest = i;
									}
								}
							}
							if(highest == -1) // We did not find light bleeding
							{
								Mod.LogInfo("\t\tNo valid light bleeding");
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
								Mod.LogInfo("\t\tRemoving valid light bleed");
								Effect.RemoveEffectAt(highest);
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
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].partIndex == targettedPart && Effect.effects[i].effectType == Effect.EffectType.LightBleeding)
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
								Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(lightBleedExperience, 2, "Treatment experience - Light Bleeding ({0})");
								return true;
							}
						}
						break;
					case ConsumableEffect.ConsumableEffectType.Fracture:
						Mod.LogInfo("\tFracture");
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
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].effectType == Effect.EffectType.Fracture)
									{
										index = i;
										break;
									}
								}
							}
							if(index == -1) // We did not find Fracture
							{
								Mod.LogInfo("\t\tNo fracture found");
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
								Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
								Mod.LogInfo("\t\tFound Fracture effect at effect index " + index);
								return true;
							}
                        }
                        else
                        {
							// Find Fracture on targettedpart
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].partIndex == targettedPart && Effect.effects[i].effectType == Effect.EffectType.Fracture)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find Fracture on the part
							{
								Mod.LogInfo("\t\tNo fracture found on target part");
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
								Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
								Mod.LogInfo("\t\tFound Fracture effect at effect index " + index);
								return true;
							}
						}
						break;
					case ConsumableEffect.ConsumableEffectType.DestroyedPart:
						Mod.LogInfo("\tDestroyed part");
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
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].effectType == Effect.EffectType.DestroyedPart && Effect.effects[i].partIndex < lowestPartIndex)
									{
										highest = i;
									}
								}
							}
							if (highest == -1) // We did not find DestroyedPart
							{
								Mod.LogInfo("\t\tNo destroyed part found");
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
								Effect.RemoveEffectAt(highest);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.health[lowestPartIndex] = 1;
								if(Mod.currentLocationIndex == 2)
                                {
									Mod.currentMaxHealth[lowestPartIndex] *= UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax);
									float totalMaxHealth = 0;
									foreach (float bodyPartMaxHealth in Mod.currentMaxHealth)
									{
										totalMaxHealth += bodyPartMaxHealth;
									}
									GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);
								}
								Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");
								Mod.AddSkillExp(Skill.surgerySkillProgress/Skill.surgeryAction, 28);
								Mod.LogInfo("\t\tFound destroyed part effect at effect index " + highest);
								return true;
							}
						}
						else
						{
							// Find DestroyedPart on targettedpart
							int index = -1;
							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].partIndex == targettedPart && Effect.effects[i].effectType == Effect.EffectType.DestroyedPart)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find DestroyedPart on the part
							{
								Mod.LogInfo("\t\tNo destroyed part found on target part");
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
								Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.health[targettedPart] = 1;
								if (Mod.currentLocationIndex == 2)
								{
									Mod.currentMaxHealth[targettedPart] *= UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax);
									float totalMaxHealth = 0;
									foreach (float bodyPartMaxHealth in Mod.currentMaxHealth)
									{
										totalMaxHealth += bodyPartMaxHealth;
									}
									GM.CurrentPlayerBody.SetHealthThreshold(totalMaxHealth);
								}
								Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");
								Mod.LogInfo("\t\tFound destroyed part effect at effect index " + index);
								return true;
							}
						}
						break;
					case ConsumableEffect.ConsumableEffectType.HeavyBleeding:
						Mod.LogInfo("\tHeavy bleed");
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
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].effectType == Effect.EffectType.HeavyBleeding && Effect.effects[i].partIndex < lowestPartIndex)
									{
										highest = i;
									}
								}
							}
							if (highest == -1) // We did not find HeavyBleeding
							{
								Mod.LogInfo("\t\tNo heavy bleed found");
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
								Effect.RemoveEffectAt(highest);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
								Mod.LogInfo("\t\tFound heavy bleed effect at effect index " + highest);
								return true;
							}
						}
						else
						{
							// Find heavy bleed on targettedpart
							int index = -1;

							if (consumeEffect.cost <= amount - amountToConsume)
							{
								for (int i = Effect.effects.Count - 1; i >= 0; --i)
								{
									if (Effect.effects[i].partIndex == targettedPart && Effect.effects[i].effectType == Effect.EffectType.HeavyBleeding)
									{
										index = i;
										break;
									}
								}
							}
							if (index == -1) // We did not find HeavyBleeding on the part
							{
								Mod.LogInfo("\t\tNo heavy bleed found on targetted part");
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
								Effect.RemoveEffectAt(index);
								amount -= consumeEffect.cost;
								++appliedEffectCount;
								Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
								Mod.LogInfo("\t\tFound heavy bleed effect at effect index " + index);
								return true;
							}
						}
						break;
				}
            }

			// Apply buffs
			foreach (BuffEffect buff in effects)
			{
				if (buff.chance != 1 && UnityEngine.Random.value > buff.chance)
				{
					continue;
				}

				// Set effect
				Effect effect = new Effect();
				effect.effectType = buff.effectType;

				effect.value = buff.value;
				effect.timer = buff.duration;
				effect.hasTimer = buff.duration > 0;
				effect.delay = buff.delay;
				effect.fromStimulator = true;

				Effect.effects.Add(effect);

				switch (buff.effectType)
                {
					case Effect.EffectType.SkillRate:
						// Set effect
						effect.skillIndex = buff.skillIndex;
						effect.value = Mod.skills[buff.skillIndex].progress + buff.value * 100;
						break;
					case Effect.EffectType.MaxStamina:
						effect.value = Mod.maxStamina + buff.value;
						break;
					case Effect.EffectType.StaminaRate:
						effect.value = Mod.currentStaminaEffect + buff.value;
						break;
					case Effect.EffectType.WeightLimit:
					case Effect.EffectType.DamageModifier:
						effect.value = 1 + buff.value;
						break;
				}

				if (effect.value < 0)
				{
					if (Mod.skills[6].currentProgress / 100 >= 51 && UnityEngine.Random.value < (Skill.stimulatorNegativeBuff * 51 / 100))
                    {
						Effect.effects.RemoveAt(Effect.effects.Count - 1);
                    }

					effect.timer += effect.timer * (HideoutController.currentDebuffEndDelay - HideoutController.currentDebuffEndDelay * (Skill.skillBoostPercent * (Mod.skills[51].currentProgress / 100) / 100)) 
								  - effect.timer * (Skill.decreaseNegativeEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);

					effect.value += effect.value * (Skill.immunityMiscEffects * (Mod.skills[6].currentProgress / 100) / 100);
                }
                else
                {
					effect.timer += effect.timer * (Skill.increasePositiveEffectDurationRate * (Mod.skills[5].currentProgress / 100) / 100);
                }
			}

			// Return true if at least one effect was used
			Mod.LogInfo("\tApplied effect count: "+appliedEffectCount+" returning: "+(appliedEffectCount > 0));
			return appliedEffectCount > 0; 
        }

		public void ToggleMode(bool inHand, bool isRightHand = false)
		{
			open = !open;
			if (open)
			{
				if (itemType == ItemType.ArmoredRig || itemType == ItemType.Rig)
				{
					// Set active the open model and interactive set, and set all others inactive
					SetMode(0);
					OpenRig(inHand, isRightHand);
				}
				else if (itemType == ItemType.Backpack)
				{
					SetMode(0);
					SetContainerOpen(true, isRightHand);
					volumeIndicator.SetActive(true);
					volumeIndicatorText.text = (containingVolume / Mod.volumePrecisionMultiplier).ToString() + "/" + (maxVolume / Mod.volumePrecisionMultiplier);
				}
				else if (itemType == ItemType.Container || itemType == ItemType.Pouch)
				{
					SetContainerOpen(true, isRightHand);
					volumeIndicator.SetActive(true);
					volumeIndicatorText.text = (containingVolume / Mod.volumePrecisionMultiplier).ToString() + "/" + (maxVolume / Mod.volumePrecisionMultiplier);
				}
				else if(itemType == ItemType.LootContainer)
				{
					SetContainerOpen(true, isRightHand);
					gameObject.GetComponent<LootContainer>().shouldSpawnItems = true;

					Mod.AddSkillExp(Skill.searchAction, 30);
				}
			}
			else
			{
				if (itemType == ItemType.ArmoredRig || itemType == ItemType.Rig)
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
				else if (itemType == ItemType.Backpack)
				{
					SetMode(containingVolume > 0 ? 1 : 2);
					SetContainerOpen(false, isRightHand);
					volumeIndicator.SetActive(false);
				}
				else if (itemType == ItemType.BodyArmor)
				{
					SetMode(1);
				}
				else if (itemType == ItemType.Container || itemType == ItemType.Pouch)
				{
					SetContainerOpen(false, isRightHand);
					volumeIndicator.SetActive(false);
				}
			}
		}

		private void SetMode(int index)
		{
			mode = index;
			if (itemType == ItemType.Money)
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
					MeatovItem CIW = itemsInSlots[i].GetComponent<MeatovItem>();
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
			if (!(itemType == ItemType.Rig || itemType == ItemType.ArmoredRig) || mode == 0)
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
			if (itemType != ItemType.Backpack || mode == 0)
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
			if(itemType == ItemType.LootContainer)
            {
				return descriptionPack;
            }

            //descriptionPack.amount = (HideoutController.instance.inventory.ContainsKey(H3ID) ? HideoutController.instance.inventory[H3ID] : 0) + (Mod.playerInventory.ContainsKey(H3ID) ? Mod.playerInventory[H3ID] : 0);
			descriptionPack.amountRequired = 0;
			for (int i=0; i < 22; ++i)
			{
				if (Mod.requiredPerArea[i] != null && Mod.requiredPerArea[i].ContainsKey(H3ID))
				{
					descriptionPack.amountRequired += Mod.requiredPerArea[i][H3ID];
					descriptionPack.amountRequiredPerArea[i] = Mod.requiredPerArea[i][H3ID];
                }
                else
				{
					descriptionPack.amountRequiredPerArea[i] = 0;
				}
			}
			descriptionPack.onWishlist = Mod.wishList.Contains(H3ID);
			descriptionPack.insured = insured;

			if (itemType == ItemType.Money)
			{
				descriptionPack.stack = stack;
			}
			else if(itemType == ItemType.Consumable)
			{
				if (maxAmount > 0)
				{
					descriptionPack.stack = amount;
					descriptionPack.maxStack = maxAmount;
                }
			}
			else if(itemType == ItemType.Backpack || itemType == ItemType.Container || itemType == ItemType.Pouch)
			{
				descriptionPack.containingVolume = containingVolume;
				descriptionPack.maxVolume = maxVolume;
			}
			else if(itemType == ItemType.AmmoBox)
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
			else if(itemType == ItemType.DogTag)
			{
				descriptionPack.stack = dogtagLevel;
				descriptionPack.name = itemName + " ("+dogtagName+")";
			}
            else
            {
                if (compatibilityValue == 3 || compatibilityValue == 1)
                {
                    if (usesAmmoContainers)
                    {
                        if (usesMags)
                        {
                            if (Mod.magazinesByType.ContainsKey(magType))
                            {
                                descriptionPack.compatibleAmmoContainers = Mod.magazinesByType[magType];
                            }
                            else
                            {
                                descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                            }
                        }
                        else
                        {
                            if (Mod.clipsByType.ContainsKey(clipType))
                            {
                                descriptionPack.compatibleAmmoContainers = Mod.clipsByType[clipType];
                            }
                            else
                            {
                                descriptionPack.compatibleAmmoContainers = new Dictionary<string, int>();
                            }
                        }
                    }
                }
                if (compatibilityValue == 3 || compatibilityValue == 2)
                {
                    if (Mod.roundsByType.ContainsKey(roundType))
                    {
                        descriptionPack.compatibleAmmo = Mod.roundsByType[roundType];
                    }
                    else
                    {
                        descriptionPack.compatibleAmmo = new Dictionary<string, int>();
                    }
                }

                if (physObj is FVRFireArmMagazine)
                {
                    FVRFireArmMagazine asMagazine = physObj as FVRFireArmMagazine;
                    descriptionPack.containedAmmoClassesByType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, int>>();

                    // Checking amount of ammo in mag only counts once per minute for a unique mag
                    long currentTime = 0/*Mod.currentLocationIndex == 1 ? HideoutController.instance.GetTimeSeconds() : Mod.currentRaidManager.GetTimeSeconds();*/;
                    if (currentTime - previousDescriptionTime > 60)
                    {
                        Mod.AddSkillExp(Skill.magazineCheckAction, 31);
                    }
                    previousDescriptionTime = currentTime;

                    descriptionPack.stack = asMagazine.m_numRounds;
                    descriptionPack.maxStack = asMagazine.m_capacity;
                    foreach (FVRLoadedRound loadedRound in asMagazine.LoadedRounds)
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
                else if (physObj is FVRFireArmClip)
                {
                    FVRFireArmClip asClip = physObj as FVRFireArmClip;
                    descriptionPack.containedAmmoClassesByType = new Dictionary<FireArmRoundType, Dictionary<FireArmRoundClass, int>>();

                    descriptionPack.stack = asClip.m_numRounds;
                    descriptionPack.maxStack = asClip.m_capacity;
                    foreach (FVRFireArmClip.FVRLoadedRound loadedRound in asClip.LoadedRounds)
                    {
                        if (loadedRound != null)
                        {
                            if (descriptionPack.containedAmmoClassesByType.ContainsKey(asClip.RoundType))
                            {
                                if (descriptionPack.containedAmmoClassesByType[asClip.RoundType].ContainsKey(loadedRound.LR_Class))
                                {
                                    descriptionPack.containedAmmoClassesByType[asClip.RoundType][loadedRound.LR_Class] += 1;
                                }
                                else
                                {
                                    descriptionPack.containedAmmoClassesByType[asClip.RoundType].Add(loadedRound.LR_Class, 1);
                                }
                            }
                            else
                            {
                                Dictionary<FireArmRoundClass, int> newDict = new Dictionary<FireArmRoundClass, int>();
                                newDict.Add(loadedRound.LR_Class, 1);
                                descriptionPack.containedAmmoClassesByType.Add(asClip.RoundType, newDict);
                            }
                        }
                    }
                }
            }
			descriptionPack.weight = currentWeight;
			descriptionPack.volume = volumes[mode];
			descriptionPack.amountRequiredQuest = Mod.requiredForQuest.TryGetValue(H3ID, out int redForQuestValue) ? redForQuestValue : 0;
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

        public void SetDescriptionManager(DescriptionManager descriptionManager)
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

		public void BeginInteraction(Hand hand)
		{
            hand.MI = this;
            hand.hasScript = true;

            // Ensure the Display_InteractionSphere is displayed also when holding equipment item, so we know where to put the item in an equip slot
            // This is only necessary for equipment items because the slots are much smaller than the item itself so we need to know what specific point to put inside the slot
            if(hand.MI.itemType == ItemType.Backpack ||
			   hand.MI.itemType == ItemType.Container ||
			   hand.MI.itemType == ItemType.ArmoredRig ||
			   hand.MI.itemType == ItemType.Rig ||
			   hand.MI.itemType == ItemType.BodyArmor ||
		       hand.MI.itemType == ItemType.Eyewear ||
			   hand.MI.itemType == ItemType.Headwear ||
			   hand.MI.itemType == ItemType.Helmet ||
			   hand.MI.itemType == ItemType.Earpiece ||
			   hand.MI.itemType == ItemType.Pouch ||
			   hand.MI.itemType == ItemType.FaceCover)
            {
                if (!hand.fvrHand.Grabbity_HoverSphere.gameObject.activeSelf)
                {
                    hand.fvrHand.Grabbity_HoverSphere.gameObject.SetActive(true);
                }
                hand.updateInteractionSphere = true;
            }
        }

		public void EndInteraction(Hand hand)
        {

            if (hand != null)
            {
                hand.MI = null;
                hand.hasScript = false;

                if (hand.fvrHand.Grabbity_HoverSphere.gameObject.activeSelf)
                {
                    hand.fvrHand.Grabbity_HoverSphere.gameObject.SetActive(false);
                }
                hand.updateInteractionSphere = false;
            }
        }

        public void OnDestroy()
        {
            Mod.meatovItemByInteractive.Remove(physObj);
        }
	}
}
