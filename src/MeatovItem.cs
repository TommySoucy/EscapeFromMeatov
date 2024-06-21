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

        // Hierarchy
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
        public int weight; // Weight of a single instance of this item
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
		private int _currentWeight; // Includes stack and children items
		public int currentWeight
		{
			get { return _currentWeight; }
			set
			{
                int preValue = _currentWeight;
				_currentWeight = value;
                if(preValue != _currentWeight)
                {
                    if(parent != null)
                    {
                        parent.currentWeight += _currentWeight - preValue;
                    }
                    OnCurrentWeightChangedInvoke(this, preValue);
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
        public ItemView marketInsureItemView;
        [NonSerialized]
        public RagFairSellItemView ragFairSellItemView;
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
                if(preValue != _stack)
                {
                    currentWeight += (_stack - preValue) * weight;
                    UpdateStackModel();
                    UpdateInventoryStacks(preValue);
                    OnStackChangedInvoke();
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
		public int amountRate = -1; // Maximum amount that can be used from the consumable in single use ex.: grizzly can only be used to heal up to 175 hp per use. 0 a single unit of multiple, -1 means no limit up to maxAmount
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
        public delegate void OnCurrentWeightChangedDelegate(MeatovItem item, int preValue);
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
            Mod.LogInfo("Meatov item " + H3ID + " awake");
            if(physObj == null)
            {
                physObj = gameObject.GetComponent<FVRPhysicalObject>();
            }

            // Set data based on default data
            if (index == -1) // Vanilla, index will not be set
            {
                int parsedIndex = -1;
                if(int.TryParse(H3ID, out parsedIndex))
                {
                    Mod.LogError("DEV: Custom item "+ H3ID+" does not have index set in asset!");
                    index = parsedIndex;
                    itemData = Mod.customItemData[index];
                    SetData(itemData);
                }
                else
                {
                    SetData(Mod.vanillaItemData[physObj.ObjectWrapper.ItemID]);
                }
            }
            else // Custom, index will already have been set in asset
            {
                // Data already set, just need to set reference to data object
                itemData = Mod.customItemData[index];

                // Note that despite comment above, we will for now SetData
                // I have realized that having the data set in asset is not optimal since
                // whenever I want to modify something, I need to rebuild an entire bundle
                SetData(itemData);
            }
            Mod.LogInfo("\t0");

            Mod.meatovItemByInteractive.Add(physObj, this);

            if (itemType != ItemType.LootContainer)
			{
				_mode = volumes.Length - 1; // Set default mode to the last index of volumes, closed empty for containers and rigs
			}
			modeInitialized = true;
            Mod.LogInfo("\t0");

            if (itemType == ItemType.Rig || itemType == ItemType.ArmoredRig)
            {
                if(!Mod.quickbeltConfigurationIndices.TryGetValue(index, out configurationIndex))
                {
                    configurationIndex = GM.Instance.QuickbeltConfigurations.Length;
                    GM.Instance.QuickbeltConfigurations = GM.Instance.QuickbeltConfigurations.AddToArray(Mod.playerBundle.LoadAsset<GameObject>("Item"+index+"Configuration"));
                }
            }
            Mod.LogInfo("\t0");

            // Quantities/Contents gets set to max on awake and will be overriden as necessary
            if (maxAmount > 0)
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
            Mod.LogInfo("\t0");

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

				Mod.stackSplitUI.arrow.localPosition = new Vector3(distanceFromCenter * 100, -2.14f, 0);
				Mod.stackSplitUI.amountText.text = splitAmount.ToString() + "/" + stack;
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

			// If usage button started being pressed this frame
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
                            Mod.stackSplitUI.gameObject.SetActive(true);
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

            // If usage button is being pressed this frame
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
                            Mod.consumeUI.text.text = string.Format("{0:0.#}/{1:0.#}", amountRate <= 0 ? (use * amount) : (use * amountRate), amountRate <= 0 ? amount : amountRate);
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
                            Mod.consumeUI.gameObject.SetActive(true);
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
					EFMHand.consuming = false;
					Mod.consumeUI.gameObject.SetActive(false);
					Mod.LogInfo("Valid consume released");

					if (amountRate == -1) // This consumable is an amount that can be consumed partially
                    {
						Mod.LogInfo("\tAmount rate -1");
						if (maxAmount > 0)
						{
							Mod.LogInfo("\t\tConsuming, amountRate == -1, maxAmount > 0, consumableTimer: " + consumableTimer + ", useTime: " + useTime);
							// Consume for the fraction timer/useTime of remaining amount. If timer >= useTime, we consume the whole thing
							int amountToConsume = consumableTimer >= useTime ? amount : (int)(amount * (consumableTimer / useTime));
							Mod.LogInfo("\t\tAmount to consume: " + amountToConsume);
                            // Attempt to apply effects at effectiveness depending on how much we've consumed on possible total
                            // Note that ApplyEffects may be unsuccessful if all effects have a cost and there was not enough consumed to cover at least one of them
                            ApplyEffects(amountToConsume / maxAmount, amountToConsume);

                            if (amount - amountToConsume <= 0)
                            {
                                amount = 0;
                            }
                            else
                            {
                                amount -= amountToConsume;

                            }

                            itemData.OnItemUsedInvoke();
						}
					}
					else if (amountRate == 0) // This consumable is discrete units and can only use one at a time, so consume one unit of it if timer >= useTime
                    {
						Mod.LogInfo("\tAmount rate 0");
						if (consumableTimer >= useTime)
						{
							Mod.LogInfo("\t\tConsumable timer >= useTime");
                            ApplyEffects(1, 1);

							amount -= 1;

                            itemData.OnItemUsedInvoke();
						}
					}
					else // A maximum use amount per usage is specified
					{
						Mod.LogInfo("\tAmount rate else");
						// Consume timer/useTime of to amountRate
						int amountToConsume = consumableTimer >= useTime ? amountRate : (int)(amountRate * (consumableTimer / useTime));
						Mod.LogInfo("\tAmount to consume: "+ amountToConsume);
                        // Apply effects at full effectiveness
                        // NOTE: In the case we have an amount rate, here, we only remove the used amount if no other effects have been applied
                        // so only if ApplyEffects returns false.
                        int actualAmountConsumed = ApplyEffects(amountToConsume / amountRate, amountToConsume);
                        int additionalAmountConsumed = 0;
                        if (actualAmountConsumed != amountToConsume)
						{
							Mod.LogInfo("\t\t\tActual amount consumed: "+ actualAmountConsumed + ", using leftover to heal");
                            int leftOver = amountToConsume - actualAmountConsumed;
                            // Here we also have to apply amount consumed as health to relevant parts
                            // NOTE: This assumes that only items that can heal health have an amountRate != 0 || -1 defined
                            // If this ever changes, we will need to have an additional flag for healing items
							int partIndex = -1;
							if (targettedPart != -1)
							{
								partIndex = targettedPart;
								Mod.LogInfo("\t\t\t\tPart "+partIndex+" targetted");
							}
							else // No part targetted, prioritize least health
							{
								Mod.LogInfo("\t\t\t\tNo part targetted, finding best...");
								int leastIndex = -1;
								float leastAmount = float.MaxValue;
								for (int i = 0; i < Mod.GetHealthCount(); ++i)
								{
                                    float health = Mod.GetHealth(i);

                                    if (health < Mod.GetCurrentMaxHealth(i) && health < leastAmount)
									{
										leastIndex = i;
										leastAmount = Mod.GetHealth(i);
									}
								}
								if (leastIndex >= 0)
								{
									partIndex = leastIndex;
								}
								Mod.LogInfo("\t\t\t\tBest part to apply health to: "+partIndex);
							}

							if (partIndex != -1)
                            {
                                float health = Mod.GetHealth(partIndex);
                                Mod.LogInfo("\t\t\t\tApplying "+ leftOver + " to "+partIndex+", which has "+ health + " health");
                                additionalAmountConsumed = Mathf.Min(leftOver, Mathf.CeilToInt(Mod.GetCurrentMaxHealth(partIndex) - health));
                                Mod.SetHealth(partIndex, Mathf.Min(Mod.GetCurrentMaxHealth(partIndex), health + additionalAmountConsumed));
								Mod.LogInfo("\t\t\t\tAfter healing: "+ health);
							}
							// else, no target part and all parts are at max health
                        }

                        int totalAmountConsumed = actualAmountConsumed + additionalAmountConsumed;
                        if (totalAmountConsumed > 0)
                        {
                            amount -= totalAmountConsumed;

                            Mod.AddExperience(additionalAmountConsumed, 2, "Treatment experience - Healing ({0})");

                            itemData.OnItemUsedInvoke();
                        }
					}

					consumableTimer = 0;

					if (amount == 0)
					{
						Destroy(gameObject);
					}
				}
			}
		}

		public void CancelSplit()
        {
			Mod.stackSplitUI.gameObject.SetActive(false);
			splittingStack = false;
			Mod.amountChoiceUIUp = false;
			Mod.splittingItem = null;
		}

        /// <summary>
        /// Apply the various effects of this item
        /// </summary>
        /// <param name="effectiveness">How much we decided to consume on total</param>
        /// <param name="amountToConsume">Max amount we can consume</param>
        /// <returns></returns>
		public int ApplyEffects(float effectiveness, int amountToConsume)
        {
            int amountLeft = amountToConsume;

			Mod.LogInfo("Apply effects called, effectiveness: " + effectiveness + ", amount to consume: " + amountToConsume);
			if(consumeEffects == null)
            {
				consumeEffects = new List<ConsumableEffect>();
				effects = new List<BuffEffect>();
            }

            TODO: // Set targetted part if hovering over a part

            bool singleEffect = consumeEffects.Count + effects.Count == 1;

            // Apply consume effects
            foreach (ConsumableEffect consumeEffect in consumeEffects)
			{
				switch (consumeEffect.effectType)
                {
					// Health
					case ConsumableEffect.ConsumableEffectType.Hydration:
						float hydrationAmount = consumeEffect.value * effectiveness;
						Mod.hydration = Mathf.Min(Mod.hydration + hydrationAmount, Mod.currentMaxHydration);
						Mod.AddSkillExp(hydrationAmount, 5);
						Mod.LogInfo("\tApplied hydration");
						break;
					case ConsumableEffect.ConsumableEffectType.Energy:
						float energyAmount = consumeEffect.value * effectiveness;
                        Mod.energy = Mathf.Min(Mod.energy + energyAmount, Mod.currentMaxEnergy);
                        Mod.AddSkillExp(energyAmount, 5);
						Mod.LogInfo("\tApplied energy");
						break;

                    // Damage
                    case ConsumableEffect.ConsumableEffectType.HeavyBleeding:
                        Mod.LogInfo("\tHeavy bleed");
                        if (heavyBleedExperience == -1)
                        {
                            heavyBleedExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["HeavyBleeding"]["HealExperience"];
                        }
                        // Remove priority HeavyBleeding effect, or one found on targetted part
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            List<Effect> effectList = null;
                            if (Effect.effectsByType.TryGetValue(Effect.EffectType.HeavyBleeding, out effectList))
                            {
                                if (targettedPart == -1)
                                {
                                    Mod.LogInfo("\t\tNo targetted part, cost: " + consumeEffect.cost + ", amount: " + amount + ", to consume: " + amountToConsume);
                                    // Prioritize lowest partIndex, the lower the partIndex the more important the part, 0: Head, 1: Chest, 2: Stomach, arms, legs
                                    int highest = -1;
                                    int lowestPartIndex = 7;
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex < lowestPartIndex)
                                        {
                                            Mod.LogInfo("\t\t\tFound valid HeavyBleeding");
                                            highest = i;
                                        }
                                    }
                                    if (highest != -1)
                                    {
                                        Mod.LogInfo("\t\tRemoving valid HeavyBleeding");
                                        Effect.RemoveEffect(effectList[highest], effectList);
                                        amountLeft -= consumeEffect.cost;
                                        Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
                                    }
                                }
                                else
                                {
                                    // Find HeavyBleeding on targettedpart
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex == targettedPart)
                                        {
                                            Effect.RemoveEffect(effectList[i], effectList);
                                            amountLeft -= consumeEffect.cost;
                                            Mod.AddExperience(heavyBleedExperience, 2, "Treatment experience - Heavy Bleeding ({0})");
                                        }
                                    }
                                }
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
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            List<Effect> effectList = null;
                            if (Effect.effectsByType.TryGetValue(Effect.EffectType.LightBleeding, out effectList))
                            {
                                if (targettedPart == -1)
                                {
                                    Mod.LogInfo("\t\tNo targetted part, cost: " + consumeEffect.cost + ", amount: " + amount + ", to consume: " + amountToConsume);
                                    // Prioritize lowest partIndex, the lower the partIndex the more important the part, 0: Head, 1: Chest, 2: Stomach, arms, legs
                                    int highest = -1;
                                    int lowestPartIndex = 7;
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex < lowestPartIndex)
                                        {
                                            Mod.LogInfo("\t\t\tFound valid light bleeding");
                                            highest = i;
                                        }
                                    }
                                    if (highest != -1)
                                    {
                                        Mod.LogInfo("\t\tRemoving valid light bleed");
                                        Effect.RemoveEffect(effectList[highest], effectList);
                                        amountLeft -= consumeEffect.cost;
                                        Mod.AddExperience(lightBleedExperience, 2, "Treatment experience - Light Bleeding ({0})");
                                    }
                                }
                                else
                                {
                                    // Find lightbleeding on targettedpart
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex == targettedPart)
                                        {
                                            Effect.RemoveEffect(effectList[i], effectList);
                                            amountLeft -= consumeEffect.cost;
                                            Mod.AddExperience(lightBleedExperience, 2, "Treatment experience - Light Bleeding ({0})");
                                        }
                                    }
                                }
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
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            List<Effect> effectList = null;
                            if (Effect.effectsByType.TryGetValue(Effect.EffectType.DestroyedPart, out effectList))
                            {
                                if (targettedPart == -1)
                                {
                                    Mod.LogInfo("\t\tNo targetted part, cost: " + consumeEffect.cost + ", amount: " + amount + ", to consume: " + amountToConsume);
                                    // Prioritize lowest partIndex, the lower the partIndex the more important the part, 0: Head, 1: Chest, 2: Stomach, arms, legs
                                    int highest = -1;
                                    int lowestPartIndex = 7;
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        // Note that we only consider part indices > 1, so not head and chest
                                        // Can't heal those from being destroyed while in raid
                                        if (effectList[i].partIndex > 1 && effectList[i].partIndex < lowestPartIndex)
                                        {
                                            Mod.LogInfo("\t\t\tFound valid destroyed part");
                                            highest = i;
                                        }
                                    }
                                    if (highest != -1)
                                    {
                                        Mod.LogInfo("\t\tRemoving valid destroyed part");
                                        Effect.RemoveEffect(effectList[highest], effectList);
                                        amountLeft -= consumeEffect.cost;
                                        Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");

                                        // In addition to removing the effect, the part is heald to 1hp and the max health is lowered
                                        Mod.SetHealth(targettedPart, 1);
                                        if (Mod.currentLocationIndex == 2)
                                        {
                                            Mod.SetCurrentMaxHealth(targettedPart, Mod.GetCurrentMaxHealth(targettedPart) * UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax));
                                        }
                                    }
                                }
                                else
                                {
                                    // Find DestroyedPart on targettedpart
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex == targettedPart)
                                        {
                                            Effect.RemoveEffect(effectList[i], effectList);
                                            amountLeft -= consumeEffect.cost;
                                            Mod.AddExperience(breakPartExperience, 2, "Treatment experience - Destroyed Part ({0})");

                                            // In addition to removing the effect, the part is heald to 1hp and the max health is lowered
                                            Mod.SetHealth(targettedPart, 1);
                                            if (Mod.currentLocationIndex == 2)
                                            {
                                                Mod.SetCurrentMaxHealth(targettedPart, Mod.GetCurrentMaxHealth(targettedPart) * UnityEngine.Random.Range(consumeEffect.healthPenaltyMin, consumeEffect.healthPenaltyMax));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case ConsumableEffect.ConsumableEffectType.Fracture:
                        Mod.LogInfo("\tFracture");
                        if (fractureExperience == -1)
                        {
                            fractureExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Fracture"]["HealExperience"];
                        }
                        // Remove priority fracture effect, or one found on targetted part
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            List<Effect> effectList = null;
                            if (Effect.effectsByType.TryGetValue(Effect.EffectType.Fracture, out effectList))
                            {
                                if (targettedPart == -1)
                                {
                                    Mod.LogInfo("\t\tNo targetted part, cost: " + consumeEffect.cost + ", amount: " + amount + ", to consume: " + amountToConsume);
                                    // Prioritize lowest partIndex, the lower the partIndex the more important the part, 0: Head, 1: Chest, 2: Stomach, arms, legs
                                    int highest = -1;
                                    int lowestPartIndex = 7;
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex < lowestPartIndex)
                                        {
                                            Mod.LogInfo("\t\t\tFound valid fracture");
                                            highest = i;
                                        }
                                    }
                                    if (highest != -1)
                                    {
                                        Mod.LogInfo("\t\tRemoving valid fracture");
                                        Effect.RemoveEffect(effectList[highest], effectList);
                                        amountLeft -= consumeEffect.cost;
                                        Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
                                    }
                                }
                                else
                                {
                                    // Find fracture on targettedpart
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        if (effectList[i].partIndex == targettedPart)
                                        {
                                            Effect.RemoveEffect(effectList[i], effectList);
                                            amountLeft -= consumeEffect.cost;
                                            Mod.AddExperience(fractureExperience, 2, "Treatment experience - Fracture ({0})");
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case ConsumableEffect.ConsumableEffectType.RadExposure:
						Mod.LogInfo("\trad exposure");
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            if (consumeEffect.duration == 0)
                            {
                                Mod.LogInfo("\t\tNo duration, curing rad exposure if present");
                                // Remove all rad exposure effects
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.RadExposure, out List<Effect> effectList))
                                {
                                    Mod.LogInfo("\t\t\tFound valid rad exposure");
                                    amountLeft -= consumeEffect.cost;
                                    Effect.RemoveEffects(Effect.EffectType.RadExposure);
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\tNo valid rad exposure");
                                }
                            }
                            else
                            {
                                if (Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.RadExposure))
                                {
                                    Effect.inactiveTimersByType[Effect.EffectType.RadExposure] = Mathf.Max(consumeEffect.duration * effectiveness, Effect.inactiveTimersByType[Effect.EffectType.RadExposure]);
                                }
                                else
                                {
                                    Effect.AddInactiveTimer(Effect.EffectType.RadExposure, consumeEffect.duration * effectiveness);
                                }

                                // Make all rad exposure effects inactive for the duration
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.RadExposure, out List<Effect> effectList))
                                {
                                    amountLeft -= consumeEffect.cost;
                                    Mod.LogInfo("RadExposure effect found, disabling for " + consumeEffect.duration * effectiveness);
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        effectList[i].Deactivate();
                                        effectList[i].inactiveTimer = Mathf.Max(consumeEffect.duration * effectiveness, effectList[i].inactiveTimer);
                                    }
                                }
                            }
                        }
						break;
					case ConsumableEffect.ConsumableEffectType.Pain:
						Mod.LogInfo("\tPain");
						if (painExperience == -1)
						{
							painExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Pain"]["HealExperience"];
                        }
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            if (consumeEffect.duration == 0)
                            {
                                Mod.LogInfo("\t\tNo duration, curing pain if present");
                                // Remove all pain effects
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Pain, out List<Effect> effectList))
                                {
                                    Mod.LogInfo("\t\t\tFound valid pain");
                                    amountLeft -= consumeEffect.cost;
                                    Effect.RemoveEffects(Effect.EffectType.Pain);
                                    Mod.AddExperience((int)(painExperience * effectiveness), 2, "Treatment experience - Pain ({0})");
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\tNo valid pain");
                                }
                            }
                            else
                            {
                                if (Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.Pain))
                                {
                                    Effect.inactiveTimersByType[Effect.EffectType.Pain] = Mathf.Max(consumeEffect.duration * effectiveness, Effect.inactiveTimersByType[Effect.EffectType.Pain]);
                                }
                                else
                                {
                                    Effect.AddInactiveTimer(Effect.EffectType.Pain, consumeEffect.duration * effectiveness);
                                }

                                // Make all pain effects inactive for the duration
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Pain, out List<Effect> effectList))
                                {
                                    amountLeft -= consumeEffect.cost;
                                    Mod.LogInfo("Pain effect found, disabling for " + consumeEffect.duration * effectiveness);
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        effectList[i].Deactivate();
                                        effectList[i].inactiveTimer = Mathf.Max(consumeEffect.duration * effectiveness, effectList[i].inactiveTimer);
                                    }
                                    Mod.AddExperience((int)(painExperience * effectiveness), 2, "Treatment experience - Pain ({0})");
                                }
                            }
                        }
						break;
					case ConsumableEffect.ConsumableEffectType.Contusion:
						Mod.LogInfo("\tContusion");
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            if (consumeEffect.duration == 0)
                            {
                                Mod.LogInfo("\t\tNo duration, curing contusion if present");
                                // Remove all Contusion effects
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Contusion, out List<Effect> effectList))
                                {
                                    Mod.LogInfo("\t\t\tFound valid Contusion");
                                    amountLeft -= consumeEffect.cost;
                                    Effect.RemoveEffects(Effect.EffectType.Contusion);
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\tNo valid Contusion");
                                }
                            }
                            else
                            {
                                if (Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.Contusion))
                                {
                                    Effect.inactiveTimersByType[Effect.EffectType.Contusion] = Mathf.Max(consumeEffect.duration * effectiveness, Effect.inactiveTimersByType[Effect.EffectType.Contusion]);
                                }
                                else
                                {
                                    Effect.AddInactiveTimer(Effect.EffectType.Contusion, consumeEffect.duration * effectiveness);
                                }

                                // Make all Contusion effects inactive for the duration
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Contusion, out List<Effect> effectList))
                                {
                                    amountLeft -= consumeEffect.cost;
                                    Mod.LogInfo("Contusion effect found, disabling for " + consumeEffect.duration * effectiveness);
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        effectList[i].Deactivate();
                                        effectList[i].inactiveTimer = Mathf.Max(consumeEffect.duration * effectiveness, effectList[i].inactiveTimer);
                                    }
                                }
                            }
                        }
                        break;
					case ConsumableEffect.ConsumableEffectType.Intoxication:
						Mod.LogInfo("\tIntoxication");
						if (intoxicationExperience == -1)
						{
							intoxicationExperience = (int)Mod.globalDB["config"]["Health"]["Effects"]["Intoxication"]["HealExperience"];
                        }
                        if (consumeEffect.cost == 0 || consumeEffect.cost <= amountLeft)
                        {
                            if (consumeEffect.duration == 0)
                            {
                                Mod.LogInfo("\t\tNo duration, curing Intoxication if present");
                                // Remove all Intoxication effects
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Intoxication, out List<Effect> effectList))
                                {
                                    Mod.LogInfo("\t\t\tFound valid Intoxication");
                                    amountLeft -= consumeEffect.cost;
                                    Effect.RemoveEffects(Effect.EffectType.Intoxication);
                                    Mod.AddExperience(intoxicationExperience, 2, "Treatment experience - Intoxication ({0})");
                                }
                                else
                                {
                                    Mod.LogInfo("\t\t\tNo valid Intoxication");
                                }
                            }
                            else
                            {
                                if (Effect.inactiveTimersByType.ContainsKey(Effect.EffectType.Intoxication))
                                {
                                    Effect.inactiveTimersByType[Effect.EffectType.Intoxication] = Mathf.Max(consumeEffect.duration * effectiveness, Effect.inactiveTimersByType[Effect.EffectType.Intoxication]);
                                }
                                else
                                {
                                    Effect.AddInactiveTimer(Effect.EffectType.Intoxication, consumeEffect.duration * effectiveness);
                                }

                                // Make all Intoxication effects inactive for the duration
                                if (Effect.effectsByType.TryGetValue(Effect.EffectType.Intoxication, out List<Effect> effectList))
                                {
                                    amountLeft -= consumeEffect.cost;
                                    Mod.LogInfo("Intoxication effect found, disabling for " + consumeEffect.duration * effectiveness);
                                    for (int i = effectList.Count - 1; i >= 0; --i)
                                    {
                                        effectList[i].Deactivate();
                                        effectList[i].inactiveTimer = Mathf.Max(consumeEffect.duration * effectiveness, effectList[i].inactiveTimer);
                                    }
                                    Mod.AddExperience(intoxicationExperience, 2, "Treatment experience - Intoxication ({0})");
                                }
                            }
                        }
                        break;
				}
            }

            int actualAmountUsed = amountToConsume - amountLeft;

            // Apply buffs
            // Note that we apply all of these regardless of amount consumed
            foreach (BuffEffect buff in effects)
            {
                if (buff.chance < 1 && UnityEngine.Random.value > buff.chance)
                {
                    continue;
                }

                // Add effect
                Effect effect = new Effect(buff, true, targettedPart);
			}

			// Return actual amount used
			Mod.LogInfo("\tApplied amount "+ actualAmountUsed + "/" + amountToConsume);
			return actualAmountUsed; 
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

        public void OnCurrentWeightChangedInvoke(MeatovItem item, int preValue)
        {
            if(OnCurrentWeightChanged != null)
            {
                OnCurrentWeightChanged(item, preValue);
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

        public void DetachChildren()
        {
            if (parentVolume == null)
            {
                // Must be reverse order since as children parent changes, they will remove themselves from the children list
                for (int i = children.Count - 1; i >= 0; --i)
                {
                    if (children[i] != null)
                    {
                        children[i].transform.parent = null;
                    }
                }
            }
            else // We have parent volume, add children to it instead of just setting their parent to null
            {
                for (int i = children.Count - 1; i >= 0; --i)
                {
                    if (children[i] != null)
                    {
                        MeatovItem childItem = children[i];
                        parentVolume.AddItem(children[i]);
                        childItem.transform.localPosition = transform.localPosition;
                    }
                }
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
                newParent = transform.GetComponentInParents<MeatovItem>(false);
            }

            if (newParent != parent)
            {
                // Remove from previous parent if necessary
                if (parent != null)
                {
                    parent.currentWeight -= currentWeight;
                    parent.children[childIndex] = parent.children[parent.children.Count - 1];
                    parent.children[childIndex].childIndex = childIndex;
                    parent.children.RemoveAt(parent.children.Count - 1);
                    parent = null;
                    childIndex = -1;
                }
                
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

                    if(newParent == null || ownerItem != parent)
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
                    parent.currentWeight += currentWeight;
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
                parent.currentWeight -= currentWeight;
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
