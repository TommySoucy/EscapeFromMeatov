using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_LootContainerCover : FVRInteractiveObject
	{
		public string keyID;
		public bool hasKey;
		public Transform Root;
		private float rotAngle;
		public float MinRot;
		public float MaxRot;
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
			base.transform.localEulerAngles = Vector3.zero;
		}

		public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			Vector3 vector = hand.Input.Pos - transform.position;
			vector = Vector3.ProjectOnPlane(vector, Root.right).normalized;
			Vector3 forward = Root.transform.forward;
			rotAngle = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(forward, vector)), Vector3.Dot(forward, vector)) * 57.29578f;
			if (rotAngle > 0f)
			{
				rotAngle -= 360f;
			}
			if (Mathf.Abs(rotAngle - MinRot) < 5f)
			{
				rotAngle = MinRot;
			}
			if (Mathf.Abs(rotAngle - MaxRot) < 5f)
			{
				rotAngle = MaxRot;
			}
			if (rotAngle >= MinRot && rotAngle <= MaxRot)
			{
				transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);
			}
		}
	}
}
