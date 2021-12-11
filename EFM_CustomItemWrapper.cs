using FistVR;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EFM
{
    public class EFM_CustomItemWrapper : MonoBehaviour
    {
		public FVRPhysicalObject physicalObject;

        public Mod.ItemType itemType;

		// Equipment
		// 0: Open (Model should be as orginal in tarkov), 1: ClosedFull (Closed but only folded to the point that any container/armor is not folded), 2: ClosedEmpty (Folded and flattened as much as is realistic)
		public int mode;
		public GameObject[] models; 
		public GameObject[] interactiveSets;
		public float[] volumes;

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

		// Backpacks
		public GameObject mainContainer;
        public Renderer[] mainContainerRenderers;
        public float maxVolume;
		public float containingVolume;
		public class ResetColPair
		{
			public FVRPhysicalObject physObj;
			public List<Collider> colliders;
		}
		public List<ResetColPair> resetColPairs;

		private void Update()
        {
			if (physicalObject.m_hand != null)
			{
				TakeInput();
			}
        }

		public void SetBackpackHovered(bool hovered)
        {
			Material matToUse = hovered ? Mod.quickSlotHoverMaterial : Mod.quickSlotConstantMaterial;
			foreach (Renderer r in mainContainerRenderers)
            {
				r.material = matToUse;
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
				}

				// If touchpad has been released this frame
				if (hand.Input.TouchpadUp)
				{
				}
			}
		}

		public void ToggleMode(bool inHand, bool isRightHand = false)
        {
			open = !open;
            if (open)
            {
				// Set active the open model and interactive set, and set all others inactive
				SetMode(0);

				if(itemType == Mod.ItemType.ArmoredRig || itemType == Mod.ItemType.Rig)
                {
					OpenRig(inHand, isRightHand);
				}
				else if (itemType == Mod.ItemType.Backpack)
                {
					SetBackpackOpen(true, inHand, isRightHand);
                }
            }
            else
            {
				if(itemType == Mod.ItemType.ArmoredRig || itemType == Mod.ItemType.Rig)
                {
					if(itemsInSlots != null)
                    {
						int modelIndex = 2; // Empty by default
						for(int i=0; i < itemsInSlots.Length; ++i)
                        {
							if(itemsInSlots != null)
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
				else if(itemType == Mod.ItemType.Backpack)
                {
					SetMode(containingVolume > 0 ? 1 : 2);
					SetBackpackOpen(false, inHand, isRightHand);
				}
				else if(itemType == Mod.ItemType.BodyArmor)
                {
					SetMode(1);
				}
            }
        }

		private void SetMode(int index)
        {
			mode = index;
			for (int i = 0; i < models.Length; ++i)
			{
				models[i].SetActive(i == index);
				interactiveSets[i].SetActive(i == index);
			}
		}

		private void OpenRig(bool processslots, bool isRightHand = false)
        {
			configurationRoot.gameObject.SetActive(true);

			if (processslots)
			{
				// Deactivate equip slots on that hand so they dont interfere with rig's
				for (int i = (isRightHand ? 0 : 3); i < (isRightHand ? 4 : 8); ++i)
				{
					Mod.equipmentSlots[i].gameObject.SetActive(false);
				}
			}

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

			if (processslots)
			{
				// Activate equip slots on that hand in case they were disabled when rig was open
				for (int i = (isRightHand ? 0 : 3); i < (isRightHand ? 4 : 8); ++i)
				{
					Mod.equipmentSlots[i].gameObject.SetActive(true);
				}
			}

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

		private void SetBackpackOpen(bool open, bool processslots, bool isRightHand = false)
		{
			if (processslots)
			{
				// Deactivate equip slots on that hand so they dont interfere with rig's
				for (int i = (isRightHand ? 0 : 3); i < (isRightHand ? 4 : 8); ++i)
				{
					Mod.equipmentSlots[i].gameObject.SetActive(!open);
				}
			}

			mainContainer.SetActive(open);
			itemObjectsRoot.gameObject.SetActive(open);
		}
    }
}
