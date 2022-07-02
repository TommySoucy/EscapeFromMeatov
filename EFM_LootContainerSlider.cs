using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_LootContainerSlider : FVRInteractiveObject
	{
		public string keyID;
		public bool hasKey;
		public Transform Root;
		public float MinY;
		public float MaxY;
		public float posZ;
		private bool m_forceOpen;

		protected override void Awake()
		{
			base.Awake();
			EndInteractionIfDistant = false;
		}

		public void ForceOpen()
		{
			m_forceOpen = true;
		}

		public override bool IsInteractable()
		{
			return m_forceOpen || !hasKey || Mod.playerInventory.ContainsKey(keyID);
		}

		public void Reset()
		{
			transform.localPosition = new Vector3(0, -0.3f, posZ);
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			float handY = hand.Input.Pos.y - 0.3f;
			transform.localPosition = new Vector3(0, Mathf.Clamp(handY, MinY, MaxY), posZ);
		}
	}
}
