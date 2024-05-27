using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
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

            DogTag = 17,

            Firearm = 18,
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

        // Hiderarchy
        [NonSerialized]
        public MeatovItem parent;
        [NonSerialized]
        public ContainmentVolume parentVolume;
        [NonSerialized]
        public int childIndex; // Our index in our parent's children list
        [NonSerialized]
        public List<MeatovItem> children = new List<MeatovItem>();

        [Header("General")]
        [NonSerialized]
        public MeatovItemData itemData;
        public string H3ID;
        public string tarkovID;
        public string H3SpawnerID;
        public int index = -1;
        [NonSerialized]
        public bool vanilla;
        public ItemType itemType;
		public List<string> parents;
        public int weight;
		public int lootExperience;
        public bool canSellOnRagfair;
		public ItemRarity rarity;
		public string itemName;
		public string description;
		public AudioClip[] itemSounds;
        [NonSerialized]
		public int upgradeCheckBlockedIndex = -1;
        [NonSerialized]
        public int upgradeCheckWarnedIndex = -1;
        [NonSerialized]
		public int upgradeBlockCount = 0;
        [NonSerialized]
        public int upgradeWarnCount = 0;
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
                bool preValue = _insured;
                _insured = value;
                if(preValue != _insured)
                {
                    OnInsuredChangedInvoke();
                }
			}
		}
		private int _currentWeight; // Includes attachments and ammo containers attached to this item
		public int currentWeight
		{
			get { return _currentWeight; }
			set
			{
                int preValue = _currentWeight;
				_currentWeight = value;
                if(preValue != _currentWeight)
                {
                    OnCurrentWeightChangedInvoke();
                }
			}
        }
        [NonSerialized]
        public bool destroyed;
        [NonSerialized]
        public bool looted;
        private bool _foundInRaid;
        public bool foundInRaid
        {
            set
            {
                bool preValue = _foundInRaid;
                _foundInRaid = value;
                if(preValue != _foundInRaid)
                {
                    UpdateFIRStatus(preValue);
                    OnFIRStatusChangedInvoke();
                }
            }

            get
            {
                return _foundInRaid;
            }
        }
        [NonSerialized]
        public bool takeCurrentLocation = true; // This dictates whether this item should take the current global location index or if it should wait to be set manually
        [NonSerialized]
        public int locationIndex = -1; // 0: Player inventory, 1: Hideout, 2: Raid
        [NonSerialized]
        public ItemView marketSellItemView;
        [NonSerialized]
        public RagFairSellItemView ragFairSellItemView;
        [NonSerialized]
        public ItemView marketInsureItemView;
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
                int preValue = _mode;
				_mode = value;
                if(preValue != _mode)
                {
                    OnModeChangedInvoke();
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
		public MeatovItem[] itemsInSlots;
		public Transform configurationRoot;
		public List<RigSlot> rigSlots;
        [NonSerialized]
		private int activeSlotsSetIndex; // The index of the rigSlots in Mod.looseRigSlots when the rig is open

        // Backpacks, Containers, Pouches
        [Header("Containers")]
        public ContainerVolume containerVolume;
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
                int preValue = _containingVolume;
				_containingVolume = value;
                if(preValue != _containingVolume)
                {
                    volumeIndicatorText.text = (_containingVolume / 1000f).ToString("0.00") + "/" + (maxVolume / 1000f).ToString("0.00");
                    OnContainingVolumeChangedInvoke();
                }
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
                int preValue = _stack;
				_stack = value;
				UpdateStackModel();
                UpdateInventoryStacks(preValue);
                OnStackChangedInvoke();
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
                int preValue = _amount;
				_amount = value;
                if(preValue != _amount)
                {
                    OnAmountChangedInvoke();
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

        // Events
        public delegate void OnInsuredChangedDelegate();
        public event OnInsuredChangedDelegate OnInsuredChanged;
        public delegate void OnCurrentWeightChangedDelegate();
        public event OnCurrentWeightChangedDelegate OnCurrentWeightChanged;
        public delegate void OnFIRStatusChangedDelegate();
        public event OnFIRStatusChangedDelegate OnFIRStatusChanged;
        public delegate void OnModeChangedDelegate();
        public event OnModeChangedDelegate OnModeChanged;
        public delegate void OnContainingVolumeChangedDelegate();
        public event OnContainingVolumeChangedDelegate OnContainingVolumeChanged;
        public delegate void OnStackChangedDelegate();
        public event OnStackChangedDelegate OnStackChanged;
        public delegate void OnAmountChangedDelegate();
        public event OnAmountChangedDelegate OnAmountChanged;

        public static void Setup(FVRPhysicalObject physicalObject)
        {
            // Just need to add component, data actually gets set in Awake()
            physicalObject.gameObject.AddComponent<MeatovItem>();
        }

        public void SetData(MeatovItemData data)
        {
            itemData = data;

            TODO: // Maybe instead of having default data in MeatovItem,
            //       we could leave it all in MeatovItemData, and this class would instead only be used
            //       for live data
            //       When we need access to default data from an object instance, we can just access it through item.itemData
            tarkovID = data.tarkovID;
            H3ID = data.H3ID;
            H3SpawnerID = data.H3SpawnerID;

            itemType = data.itemType;
            rarity = data.rarity;
            parents = new List<string>(data.parents);
            weight = data.weight;
            volumes = data.volumes;
            lootExperience = data.lootExperience;
            itemName = data.name;
            description = data.description;
            canSellOnRagfair = data.canSellOnRagfair;

            compatibilityValue = data.compatibilityValue;
            usesMags = data.usesMags;
            usesAmmoContainers = data.usesAmmoContainers;
            magType = data.magType;
            clipType = data.clipType;
            roundType = data.roundType;
            weaponClass = data.weaponclass;

            blocksEarpiece = data.blocksEarpiece;
            blocksEyewear = data.blocksEyewear;
            blocksFaceCover = data.blocksFaceCover;
            blocksHeadwear = data.blocksHeadwear;

            coverage = data.coverage;
            damageResist = data.damageResist;
            maxArmor = data.maxArmor;

            maxVolume = data.maxVolume;

            cartridge = data.cartridge;
            roundClass = data.roundClass;

            maxStack = data.maxStack;

            maxAmount = data.maxAmount;

            useTime = data.useTime;
            amountRate = data.amountRate;
            effects = data.effects;
            consumeEffects = data.consumeEffects;

            dogtagLevel = data.dogtagLevel;
            dogtagName = data.dogtagName;
        }

        private void Awake()
		{
            if(physObj == null)
            {
                physObj = gameObject.GetComponent<FVRPhysicalObject>();
            }

            // Set data based on default data
            if (index == -1) // Vanilla, index will not be set
            {
                SetData(Mod.vanillaItemData[physObj.ObjectWrapper.ItemID]);
            }
            else // Custom, index will already have been set in asset
            {
                // Data already set, just need to set reference to data object
                itemData = Mod.customItemData[index];
            }

            Mod.meatovItemByInteractive.Add(physObj, this);

            if (itemType != ItemType.LootContainer)
			{
				_mode = volumes.Length - 1; // Set default mode to the last index of volumes, closed empty for containers and rigs
			}
			modeInitialized = true;

            if(itemType == ItemType.Rig || itemType == ItemType.ArmoredRig)
            {
                if(!Mod.quickbeltConfigurationIndices.TryGetValue(index, out configurationIndex))
                {
                    configurationIndex = GM.Instance.QuickbeltConfigurations.Length;
                    GM.Instance.QuickbeltConfigurations = GM.Instance.QuickbeltConfigurations.AddToArray(Mod.playerBundle.LoadAsset<GameObject>("Item"+index+"Configuration"));
                }
            }

            // Quantities/Contents gets set to max on awake and will be overriden as necessary
            if(maxAmount > 0)
            {
                amount = maxAmount;
            }
            if(maxArmor > 0)
            {
                armor = maxArmor;
            }
            if(physObj is FVRFireArmMagazine)
            {
                FVRFireArmMagazine asMagazine = physObj as FVRFireArmMagazine;
                asMagazine.ReloadMagWithType(roundClass);
            }
            if(physObj is FVRFireArmClip)
            {
                FVRFireArmClip asClip = physObj as FVRFireArmClip;
                asClip.ReloadClipWithType(roundClass);
            }

            UpdateInventories();
		}

        public void UpdateInventories()
        {
            // Find new location
            int newLocation = -1;
            if (parent != null)
            {
                newLocation = parent.locationIndex;
            }
            else
            {
                if (physObj.m_hand != null
                    || (physObj.QuickbeltSlot != null && physObj.QuickbeltSlot.IsPlayer))
                {
                    newLocation = 0;
                }
                else
                {
                    newLocation = Mod.currentLocationIndex;
                }
            }

            // Update inventories only if location has changed
            if(locationIndex != newLocation)
            {
                if(locationIndex == 0)
                {
                    Mod.RemoveFromPlayerInventory(this);
                }
                else if(locationIndex == 1)
                {
                    HideoutController.instance.RemoveFromInventory(this);
                }

                if (newLocation == 0)
                {
                    Mod.AddToPlayerInventory(this);
                }
                else if (newLocation == 1)
                {
                    HideoutController.instance.AddToInventory(this);
                }
            }

            locationIndex = newLocation;

            // Update children
            for (int i=0; i < children.Count; ++i)
            {
                children[i].UpdateInventories();
            }
        }

        public void UpdateInventoryStacks(int preStack)
        {
            int difference = stack - preStack;

            // Add stack difference to inventory
            if (locationIndex == 0)
            {
                Mod.AddToPlayerInventory(this, true, difference);
            }
            else if (locationIndex == 1)
            {
                HideoutController.instance.AddToInventory(this, true, difference);
            }

            // Update current parent volume inventory if we have one
            if(parentVolume != null)
            {
                parentVolume.AdjustStack(this, difference);
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
				foreach (MeatovItem containedItem in item.itemsInSlots) 
				{
					if(containedItem != null)
                    {
						item.currentWeight += SetCurrentWeight(containedItem);
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
                switch (itemType)
                {
                    case ItemType.ArmoredRig:
                    case ItemType.Rig:
                    case ItemType.Backpack:
                    case ItemType.BodyArmor:
                    case ItemType.Container:
                    case ItemType.Pouch:
                        ToggleMode(true, hand.IsThisTheRightHand);
                        break;
                    case ItemType.Money:
                        if (splittingStack)
                        {
                            // End splitting
                            if (splitAmount != stack && splitAmount != 0)
                            {
                                stack -= splitAmount;

                                MeatovItem splitItem = Instantiate(Mod.GetItemPrefab(index), hand.transform.position + hand.transform.forward * 0.2f, Quaternion.identity).GetComponent<MeatovItem>();
                                splitItem.stack = splitAmount;
                                splitItem.UpdateInventories();
                            }
                            // else the chosen amount is 0 or max, meaning cancel the split
                            CancelSplit();
                        }
                        else
                        {
                            // Cancel market amount choosing if active
                            if(HideoutController.instance != null)
                            {
                                HideoutController.instance.marketManager.choosingBuyAmount = false;
                                HideoutController.instance.marketManager.choosingRagfairBuyAmount = false;
                                HideoutController.instance.marketManager.startedChoosingThisFrame = false;
                            }

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

			// If A is being pressed this frame
			if (usageButtonPressed)
			{
                switch (itemType)
                {
                    case ItemType.Consumable:
                        if (!EFMHand.otherHand.consuming)
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

                                    itemData.OnItemUsedInvoke();
								}
							}
							else
							{
								// Apply effects at partial effectiveness
								if (ApplyEffects(amountToConsume / maxAmount, amountToConsume))
								{
									amount -= amountToConsume;

                                    itemData.OnItemUsedInvoke();
                                }
							}
						}
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

                                itemData.OnItemUsedInvoke();
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
									for (int i = 0; i < Mod.GetHealthCount(); ++i)
									{
                                        float health = Mod.GetHealth(i);

                                        if (health < Mod.currentMaxHealth[i] && health < leastAmount)
										{
											leastIndex = i;
											leastAmount = Mod.GetHealth(i);
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
                                    float health = Mod.GetHealth(partIndex);
                                    Mod.LogInfo("\t\t\t\tApplying "+actualAmountConsumed+" to "+partIndex+", which has "+ health + " health");
									actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - health));
                                    Mod.SetHealth(partIndex, Mathf.Min(Mod.currentMaxHealth[partIndex], health + actualAmountConsumed));
									Mod.LogInfo("\t\t\t\tAfter healing: "+ health);
								}
								// else, no target part and all parts are at max health

								amount -= actualAmountConsumed;
								if (actualAmountConsumed > 0)
								{
									Mod.AddExperience(actualAmountConsumed, 2, "Treatment experience - Healing ({0})");

                                    itemData.OnItemUsedInvoke();
                                }
                            }
                            else
                            {
                                itemData.OnItemUsedInvoke();
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
                                    for (int i = 0; i < Mod.GetHealthCount(); ++i)
                                    {
                                        float health = Mod.GetHealth(i);
                                        if (health < Mod.currentMaxHealth[i] && health < leastAmount)
										{
											leastIndex = i;
											leastAmount = health;
										}
									}
									if (leastIndex >= 0)
									{
										partIndex = leastIndex;
									}
								}

								if (partIndex != -1)
                                {
                                    float health = Mod.GetHealth(partIndex);
                                    if (Mod.currentLocationIndex == 1) // In hideout, take base max health
									{
										actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - health));
                                        Mod.SetHealth(partIndex, Mathf.Min(Mod.currentMaxHealth[partIndex], health + actualAmountConsumed));
									}
									else // In raid, take raid max health
									{
										actualAmountConsumed = Mathf.Min(amountToConsume, Mathf.CeilToInt(Mod.currentMaxHealth[partIndex] - health));
                                        Mod.SetHealth(partIndex, Mathf.Min(Mod.currentMaxHealth[partIndex], health + actualAmountConsumed));
									}
								}
								// else, no target part and all parts are at max health

								amount -= actualAmountConsumed;
								if (actualAmountConsumed > 0)
								{
									Mod.AddExperience(actualAmountConsumed, 2, "Treatment experience - Healing ({0})");

                                    itemData.OnItemUsedInvoke();
                                }
                            }
                            else
                            {
                                itemData.OnItemUsedInvoke();
                            }
						}
					}

					consumableTimer = 0;

					if (amount == 0)
					{
						// Update player inventory and weight
						Mod.RemoveFromPlayerInventory(this);
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
                                Mod.SetHealth(lowestPartIndex, 1);
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
                                Mod.SetHealth(targettedPart, 1);
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
            // Can't open item that has a parent item (ex.: backpack in volume)
            if(!open && parent != null)
            {
                return;
            }

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
					volumeIndicatorText.text = (containingVolume / 1000f).ToString("0.00") + "/" + (maxVolume / 1000f).ToString("0.00");
				}
				else if (itemType == ItemType.Container || itemType == ItemType.Pouch)
				{
					SetContainerOpen(true, isRightHand);
					volumeIndicator.SetActive(true);
					volumeIndicatorText.text = (containingVolume / 1000f).ToString("0.00") + "/" + (maxVolume / 1000f).ToString("0.00");
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
			else if(models != null)
			{
				for (int i = 0; i < models.Length; ++i)
				{
					models[i].SetActive(i == index);
					interactiveSets[i].SetActive(i == index);
				}
			}
		}

        public void UpdateClosedMode()
        {
            if(mode == 0)
            {
                return;
            }

            switch (itemType)
            {
                case ItemType.Rig:
                case ItemType.ArmoredRig:
                    for (int i = 0; i < itemsInSlots.Length; ++i)
                    {
                        if (itemsInSlots[i] != null)
                        {
                            SetMode(1);
                            break;
                        }
                    }

                    // If we get this far it is because no items in slots, so set to closed empty
                    SetMode(2);
                    break;
                case ItemType.Backpack:
                case ItemType.Pouch:
                case ItemType.Container:
                    if (containerItemRoot.childCount > 0)
                    {
                        SetMode(1);
                    }
                    else
                    {
                        SetMode(2);
                    }
                    break;
            }
        }

        public void OpenRig(bool processslots, bool isRightHand = false)
        {
            configurationRoot.gameObject.SetActive(true);

            // Set active slots
            activeSlotsSetIndex = Mod.looseRigSlots.Count;
            Mod.looseRigSlots.Add(rigSlots);
        }

        public void CloseRig(bool processslots, bool isRightHand = false)
		{
			configurationRoot.gameObject.SetActive(false);

            // Remove from other active slots
            Mod.looseRigSlots[activeSlotsSetIndex] = Mod.looseRigSlots[Mod.looseRigSlots.Count - 1];
            MeatovItem replacementRig = Mod.looseRigSlots[activeSlotsSetIndex][0].ownerItem;
            replacementRig.activeSlotsSetIndex = activeSlotsSetIndex;
            Mod.looseRigSlots.RemoveAt(Mod.looseRigSlots.Count - 1);
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

        public void UpdateFIRStatus(bool preValue)
        {
            // Note that when an item is spawned, this type of live data will not have been set yet
            // This means that once we spawn an item, it will be added to necessary inventories
            // but not FIR inventories, until we set the item's FIR status, at which point this method will
            // be called and only then will we add it to FIR inventories
            if (preValue)
            {
                // FIR status removed, must remove from FIR inventories
                if (locationIndex == 0)
                {
                    Mod.RemoveFromPlayerFIRInventory(this);
                }
                else if (locationIndex == 1)
                {
                    HideoutController.instance.RemoveFromFIRInventory(this);
                }

                if(parentVolume != null)
                {
                    parentVolume.RemoveItemFIR(this);
                }
            }
            else
            {
                // FIR status added, must add to FIR inventories
                if (locationIndex == 0)
                {
                    Mod.AddToPlayerFIRInventory(this);
                }
                else if (locationIndex == 1)
                {
                    HideoutController.instance.AddToFIRInventory(this);
                }

                if (parentVolume != null)
                {
                    parentVolume.AddItemFIR(this);
                }
            }
        }


        public DescriptionPack GetDescriptionPack()
        {
            DescriptionPack newPack = new DescriptionPack();

            newPack.itemData = itemData;
            newPack.item = this;

			return newPack;
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
            Mod.LogInfo((hand.fvrHand.IsThisTheRightHand ? "Right" : "Left") + " hand began interacting with " + itemData.name);
            hand.heldItem = this;
            hand.hasScript = true;

            // Ensure the Grabbity_HoverSphere is displayed also when holding equipment item, so we know where to put the item in an equip slot
            // This is only necessary for equipment items because the slots are much smaller than the item itself so we need to know what specific point to put inside the slot
            if (hand.heldItem.itemType == ItemType.Backpack ||
			   hand.heldItem.itemType == ItemType.Container ||
			   hand.heldItem.itemType == ItemType.ArmoredRig ||
			   hand.heldItem.itemType == ItemType.Rig ||
			   hand.heldItem.itemType == ItemType.BodyArmor ||
		       hand.heldItem.itemType == ItemType.Eyewear ||
			   hand.heldItem.itemType == ItemType.Headwear ||
			   hand.heldItem.itemType == ItemType.Helmet ||
			   hand.heldItem.itemType == ItemType.Earpiece ||
			   hand.heldItem.itemType == ItemType.Pouch ||
			   hand.heldItem.itemType == ItemType.FaceCover)
            {
                if (!hand.fvrHand.Grabbity_HoverSphere.gameObject.activeSelf)
                {
                    hand.fvrHand.Grabbity_HoverSphere.gameObject.SetActive(true);
                }
                hand.updateInteractionSphere = true;
            }

            if (!looted)
            {
                looted = true;
                if (Mod.currentLocationIndex == 2)
                {
                    if (lootExperience > 0)
                    {
                        Mod.AddExperience(lootExperience, 1);
                    }
                }

                if (foundInRaid)
                {
                    itemData.OnItemFoundInvoke();
                }

                Mod.AddSkillExp(Skill.uniqueLoot, 7);
            }

            // Handle harnessed case
            if (physObj.m_isHardnessed)
            {
                // Scale item back to 1 if harnessed
                // When items are put in equipment slot they are scaled down to fit the slot
                // This scaling is done in SetQuickBeltSlotPatch
                // If item is harnessed, this patch doesn't get called because the item's slot doesn't actually change
                // So we need to handle scaling with harnessed items here
                transform.localScale = Vector3.one;
            }

            UpdateInventories();
        }

		public void EndInteraction(Hand hand)
        {
            Mod.LogInfo((hand.fvrHand.IsThisTheRightHand ? "Right" : "Left") + " hand stopped interacting with " + itemData.name);

            if (hand != null)
            {
                hand.heldItem = null;
                hand.hasScript = false;

                if (hand.fvrHand.Grabbity_HoverSphere.gameObject.activeSelf)
                {
                    hand.fvrHand.Grabbity_HoverSphere.gameObject.SetActive(false);
                }
                hand.updateInteractionSphere = false;
            }

            // Check hovered QBS because we want to prioritize that over EFM volumes
            if(hand.fvrHand.CurrentHoveredQuickbeltSlot == null && hand.collidingVolume != null)
            {
                // Remove from previous volume if it had one
                if(parentVolume != null && hand.collidingVolume != parentVolume)
                {
                    parentVolume.RemoveItem(this);
                }

                // Add to new volume
                hand.collidingVolume.AddItem(this);
                hand.collidingVolume.Unoffer();
                hand.volumeCollider = null;
            }

            // Handle harnessed case
            if (physObj.m_isHardnessed)
            {
                // Scale item back to correspond with QBS if harnessed
                // When items are put in equipment slot they are scaled down to fit the slot
                // This scaling is done in SetQuickBeltSlotPatch
                // If item is harnessed, this patch doesn't get called because the item's slot doesn't actually change
                // So we need to handle scaling with harnessed items here
                transform.localScale = physObj.QBPoseOverride.localScale;

                // For the same reason, we must make sure that a togglable item in a slot is closed
                if (open)
                {
                    ToggleMode(false);
                }
            }
            
            // Play drop sound
            // Vanilla will for sure not have play a release sound for a custom item because phys obj asset has release sound None
            if (hand.fvrHand.CanMakeGrabReleaseSound && itemSounds != null && itemSounds[0] != null)
            {
                AudioEvent audioEvent = new AudioEvent();
                audioEvent.Clips.Add(itemSounds[0]);
                SM.PlayCoreSound(FVRPooledAudioType.GenericClose, audioEvent, hand.fvrHand.Input.Pos);
                hand.fvrHand.HandMadeGrabReleaseSound();
            }

            UpdateInventories();
        }

        public bool ContainsItem(MeatovItemData itemData)
        {
            if (H3ID.Equals(itemData.H3ID))
            {
                return true;
            }

            for(int i=0; i < children.Count; ++i)
            {
                if (children[i].ContainsItem(itemData))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItems(List<MeatovItemData> itemDataList)
        {
            for(int i=0; i < itemDataList.Count; ++i)
            {
                if (!ContainsItem(itemDataList[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ContainsItemInCategory(string category)
        {
            if (parents.Contains(category))
            {
                return true;
            }

            for (int i = 0; i < children.Count; ++i)
            {
                if (children[i].ContainsItemInCategory(category))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ContainsItemInCategories(List<string> categoryList)
        {
            for(int i=0; i < categoryList.Count; ++i)
            {
                if (!ContainsItemInCategory(categoryList[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetEmptyMountCount()
        {
            int total = 0;
            if(physObj.AttachmentMounts != null)
            {
                for(int i=0; i < physObj.AttachmentMounts.Count; ++i)
                {
                    if (!physObj.AttachmentMounts[i].HasAttachmentsOnIt())
                    {
                        ++total;
                    }
                }
            }

            for(int i=0; i < children.Count; ++i)
            {
                total += children[i].GetEmptyMountCount();
            }

            return total;
        }

        public int GetCurrentWeight()
        {
            int total = weight;

            for (int i = 0; i < children.Count; ++i)
            {
                total += children[i].GetCurrentWeight();
            }

            return total;
        }

        public JObject Serialize()
        {
            JObject serialized = new JObject();

            // Store ID, from which we can get all static data about this item
            serialized["H3ID"] = H3ID;

            // Store live data
            serialized["insured"] = insured;
            serialized["looted"] = looted;
            serialized["foundInRaid"] = foundInRaid;
            serialized["mode"] = mode;
            serialized["broken"] = broken;
            serialized["armor"] = armor;
            serialized["open"] = open;
            serialized["containingVolume"] = containingVolume;
            serialized["cartridge"] = cartridge;
            serialized["roundClass"] = (int)roundClass;
            serialized["stack"] = stack;
            serialized["amount"] = amount;
            serialized["USEC"] = USEC;
            serialized["dogtagLevel"] = dogtagLevel;
            serialized["dogtagName"] = dogtagName;
            if(itemType == ItemType.AmmoBox)
            {
                JArray ammo = new JArray();
                FVRFireArmMagazine asMag = physObj as FVRFireArmMagazine;
                for(int i=0; i < asMag.m_numRounds; ++i)
                {
                    ammo.Add((int)asMag.LoadedRounds[i].LR_Class);
                }
                serialized["ammo"] = ammo;
            }

            // Store children
            // Custom types
            JArray serializedChildren = new JArray();
            switch (itemType)
            {
                case ItemType.Rig:
                case ItemType.ArmoredRig:
                    for(int i=0; i < itemsInSlots.Length; ++i)
                    {
                        if(itemsInSlots[i] != null)
                        {
                            JObject serializedChild = itemsInSlots[i].Serialize();
                            serializedChild["slotIndex"] = i;
                            serializedChildren.Add(serializedChild);
                        }
                    }
                    break;
                case ItemType.Backpack:
                case ItemType.Container:
                case ItemType.Pouch:
                    foreach (KeyValuePair<string, List<MeatovItem>> containerChild in containerVolume.inventoryItems)
                    {
                        for(int i=0; i < containerChild.Value.Count; ++i)
                        {
                            if (containerChild.Value[i] != null)
                            {
                                JObject serializedChild = containerChild.Value[i].Serialize();
                                serializedChild["posX"] = containerChild.Value[i].transform.localPosition.x;
                                serializedChild["posY"] = containerChild.Value[i].transform.localPosition.y;
                                serializedChild["posZ"] = containerChild.Value[i].transform.localPosition.z;
                                serializedChild["rotX"] = containerChild.Value[i].transform.localRotation.eulerAngles.x;
                                serializedChild["rotY"] = containerChild.Value[i].transform.localRotation.eulerAngles.y;
                                serializedChild["rotZ"] = containerChild.Value[i].transform.localRotation.eulerAngles.z;
                                serializedChildren.Add(serializedChild);
                            }
                        }
                    }
                    break;
            }
            // Vanilla types (Using vault system)
            if (vanilla)
            {
                VaultFile vaultFile = new VaultFile();
                VaultSystem.ScanObjectToVaultFile(vaultFile, physObj);
                serialized["vanillaData"] = JObject.FromObject(vaultFile);
                // Store all live meatov item data necessary for vanilla items in the entire hierarchy of this item, including this item's
                serialized["vanillaCustomData"] = GetAllSerializedVanillaCustomData(); 
            }
            serialized["children"] = serializedChildren;

            return serialized;
        }

        public JObject GetAllSerializedVanillaCustomData()
        {
            JObject serialized = GetSerializedVanillaCustomData();
            JArray children = new JArray();
            List<FVRPhysicalObject> objs = physObj.GetContainedObjectsRecursively();
            for(int i=0; i < objs.Count; ++i)
            {
                MeatovItem childItem = objs[i].GetComponent<MeatovItem>();
                if(childItem != null)
                {
                    children.Add(childItem.GetSerializedVanillaCustomData());
                }
            }
            serialized["children"] = children;

            return serialized;
        }

        public JObject GetSerializedVanillaCustomData()
        {
            JObject serialized = new JObject();

            serialized["insured"] = insured;
            serialized["looted"] = looted;
            serialized["foundInRaid"] = foundInRaid;

            return serialized;
        }

        public static MeatovItem Deserialize(JToken serialized, VaultSystem.ReturnObjectListDelegate del = null)
        {
            if(serialized["H3ID"] == null)
            {
                Mod.LogError("Attempted to deserialize item but data missing H3ID property");
                return null;
            }

            if (Mod.GetItemData(serialized["H3ID"].ToString(), out MeatovItemData itemData))
            {
                if(itemData.index == -1)
                {
                    VaultFile loadedVanillaData = serialized["vanillaData"].ToObject<VaultFile>();
                    VaultSystem.SpawnVaultFile(loadedVanillaData, null, false, false, false, out string error, Vector3.zero, del, false);

                    return null;
                }
                else
                {
                    GameObject itemPrefab = Mod.GetItemPrefab(itemData.index);
                    GameObject itemInstance = Instantiate(itemPrefab);
                    MeatovItem item = itemInstance.GetComponent<MeatovItem>();

                    item.insured = (bool)serialized["insured"];
                    item.looted = (bool)serialized["looted"];
                    item.foundInRaid = (bool)serialized["foundInRaid"];
                    item.mode = (int)serialized["mode"];
                    item.broken = (bool)serialized["broken"];
                    item.armor = (float)serialized["armor"];
                    item.open = (bool)serialized["open"];
                    item.containingVolume = (int)serialized["containingVolume"];
                    item.cartridge = serialized["cartridge"].ToString();
                    item.roundClass = (FireArmRoundClass)(int)serialized["roundClass"];
                    item.stack = (int)serialized["stack"];
                    item.amount = (int)serialized["amount"];
                    item.USEC = (bool)serialized["USEC"];
                    item.dogtagLevel = (int)serialized["dogtagLevel"];
                    item.dogtagName = serialized["dogtagName"].ToString();
                    if (itemData.itemType == ItemType.AmmoBox)
                    {
                        JArray ammo = serialized["ammo"] as JArray;
                        FVRFireArmMagazine asMag = item.physObj as FVRFireArmMagazine;
                        for (int i = 0; i < ammo.Count; ++i)
                        {
                            asMag.AddRound((FireArmRoundClass)(int)ammo[i], false, true);
                        }
                    }

                    // Process children
                    JArray children = serialized["children"] as JArray;
                    switch (itemData.itemType)
                    {
                        case ItemType.Rig:
                        case ItemType.ArmoredRig:
                            for (int i = 0; i < children.Count; ++i)
                            {
                                int slotIndex = (int)children[i]["slotIndex"];

                                if (children[i]["vanillaCustomData"] == null)
                                {
                                    MeatovItem childItem = Deserialize(children[i]);
                                    if (childItem != null)
                                    {
                                        childItem.physObj.SetQuickBeltSlot(item.rigSlots[slotIndex]);
                                        childItem.UpdateInventories();
                                    }
                                }
                                else
                                {
                                    // Make a delegate, in case the deserialized child item is vanilla, so we place it in the correct QBS once spawned
                                    JToken vanillaCustomData = children[i]["vanillaCustomData"];
                                    VaultSystem.ReturnObjectListDelegate delact = objs =>
                                    {
                                        // Here, assume objs[0] is the root item
                                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                                        if (meatovItem != null)
                                        {
                                            objs[0].SetQuickBeltSlot(item.rigSlots[slotIndex]);
                                            meatovItem.UpdateInventories();

                                            // Set live data
                                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];
                                            for(int j=1; j < objs.Count; ++j)
                                            {
                                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                                if(childMeatovItem != null)
                                                {
                                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][j-1]["insured"];
                                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][j - 1]["looted"];
                                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][j - 1]["foundInRaid"];
                                                }
                                            }
                                        }
                                    }; 
                                    
                                    Deserialize(children[i], delact);
                                }
                            }
                            break;
                        case ItemType.Backpack:
                        case ItemType.Container:
                        case ItemType.Pouch:
                            for (int i = 0; i < children.Count; ++i)
                            {
                                Vector3 localPos = new Vector3((float)children[i]["posX"], (float)children[i]["posY"], (float)children[i]["posX"]);
                                Quaternion localRot = Quaternion.Euler((float)children[i]["rotX"], (float)children[i]["rotY"], (float)children[i]["rotX"]);

                                if(children[i]["vanillaCustomData"] == null)
                                {
                                    MeatovItem childItem = Deserialize(children[i]);
                                    if (childItem != null)
                                    {
                                        item.containerVolume.AddItem(childItem);
                                        childItem.UpdateInventories();
                                        childItem.transform.localPosition = localPos;
                                        childItem.transform.localRotation = localRot;
                                    }
                                }
                                else
                                {
                                    // Make a delegate, in case the deserialized child item is vanilla, so we place it correctly in the container volume once spawned
                                    JToken vanillaCustomData = children[i]["vanillaCustomData"];
                                    VaultSystem.ReturnObjectListDelegate delact = objs =>
                                    {
                                        // Here, assume objs[0] is the root item
                                        MeatovItem meatovItem = objs[0].GetComponent<MeatovItem>();
                                        if (meatovItem != null)
                                        {
                                            item.containerVolume.AddItem(meatovItem);
                                            meatovItem.UpdateInventories();
                                            objs[0].transform.localPosition = localPos;
                                            objs[0].transform.localRotation = localRot;

                                            // Set live data
                                            meatovItem.insured = (bool)vanillaCustomData["insured"];
                                            meatovItem.looted = (bool)vanillaCustomData["looted"];
                                            meatovItem.foundInRaid = (bool)vanillaCustomData["foundInRaid"];
                                            for (int j = 1; j < objs.Count; ++j)
                                            {
                                                MeatovItem childMeatovItem = objs[j].GetComponent<MeatovItem>();
                                                if (childMeatovItem != null)
                                                {
                                                    childMeatovItem.insured = (bool)vanillaCustomData["children"][j - 1]["insured"];
                                                    childMeatovItem.looted = (bool)vanillaCustomData["children"][j - 1]["looted"];
                                                    childMeatovItem.foundInRaid = (bool)vanillaCustomData["children"][j - 1]["foundInRaid"];
                                                }
                                            }
                                        }
                                    };

                                    Deserialize(children[i], delact);
                                }
                            }
                            break;
                    }

                    return item;
                }
            }
            else
            {
                Mod.LogError("Attempted to deserialize item but could not get item data for H3ID: "+ serialized["H3ID"].ToString());
                return null;
            }
        }

        public void OnInsuredChangedInvoke()
        {
            if(OnInsuredChanged != null)
            {
                OnInsuredChanged();
            }
        }

        public void OnCurrentWeightChangedInvoke()
        {
            if(OnCurrentWeightChanged != null)
            {
                OnCurrentWeightChanged();
            }
        }

        public void OnFIRStatusChangedInvoke()
        {
            if(OnFIRStatusChanged != null)
            {
                OnFIRStatusChanged();
            }
        }

        public void OnModeChangedInvoke()
        {
            if(OnModeChanged != null)
            {
                OnModeChanged();
            }
        }

        public void OnContainingVolumeChangedInvoke()
        {
            if(OnContainingVolumeChanged != null)
            {
                OnContainingVolumeChanged();
            }
        }

        public void OnStackChangedInvoke()
        {
            if(OnStackChanged != null)
            {
                OnStackChanged();
            }
        }

        public void OnAmountChangedInvoke()
        {
            if(OnAmountChanged != null)
            {
                OnAmountChanged();
            }
        }

        public void OnTransformParentChanged()
        {
            MeatovItem newParent = null;

            if(physObj.QuickbeltSlot != null)
            {
                // If in QBS, we may not be attached directly to the QBS itself so go through it instead of transform parent
                newParent = physObj.QuickbeltSlot.GetComponentInParents<MeatovItem>();
            }
            else if(physObj is FVRFireArmAttachment && (physObj as FVRFireArmAttachment).curMount != null)
            {
                // If attachment, we may be parented to some root item instead of the item that owns the mount we are on
                newParent = (physObj as FVRFireArmAttachment).curMount.GetComponentInParents<MeatovItem>();
            }
            else
            {
                newParent = transform.GetComponentInParents<MeatovItem>();
            }

            if (newParent != parent)
            {
                // Remove from previous parent
                parent.children[childIndex] = parent.children[parent.children.Count - 1];
                parent.children[childIndex].childIndex = childIndex;
                parent.children.RemoveAt(parent.children.Count - 1);
                parent = null;
                childIndex = -1; 
                
                // If was in a volume, must check if still correct one
                // because of the order in which this parent changed event is called and when we call
                // EndInteraction on the item to add it to a volume,
                // if we call this after for any reason, we obviously don't want to remove from volume we just added this item to
                if (parentVolume != null)
                {
                    MeatovItem ownerItem = null;
                    if (parentVolume is ContainerVolume)
                    {
                        ownerItem = (parentVolume as ContainerVolume).ownerItem;
                    }

                    if(ownerItem != parent)
                    {
                        parentVolume.RemoveItem(this);
                    }
                }

                // Add to new parent
                if (newParent != null)
                {
                    parent = newParent;
                    childIndex = parent.children.Count;
                    parent.children.Add(this);
                }
            }
        }

        public void OnDestroy()
        {
            Mod.meatovItemByInteractive.Remove(physObj);

            if(locationIndex == 0)
            {
                Mod.RemoveFromPlayerInventory(this);
            }
            else if(locationIndex == 1)
            {
                HideoutController.instance.RemoveFromInventory(this);
            }

            // Remove from parent
            if (parent != null)
            {
                parent.children[childIndex] = parent.children[parent.children.Count - 1];
                parent.children[childIndex].childIndex = childIndex;
                parent.children.RemoveAt(parent.children.Count - 1);
                parent = null;
                childIndex = -1;
            }

            // Remove from parent volume
            if (parentVolume != null)
            {
                parentVolume.RemoveItem(this);
            }

            // Cancel split if necessary
            if(Mod.splittingItem == this)
            {
                CancelSplit();
            }
        }
	}
}
