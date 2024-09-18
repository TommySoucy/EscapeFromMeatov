using FistVR;
using UnityEngine;

namespace EFM
{
    public class LootContainerCover : FVRInteractiveObject
	{
		public LootContainer lootContainer;

		public Transform Root;
		public float minRot;
		public float maxRot;

		private bool forceOpen;

        public float rotAngle;

		public TrackedLCCoverData trackedLCCoverData;

        public override void Awake()
        {
            base.Awake();

            EndInteractionIfDistant = false;
        }

		public void ForceOpen()
		{
			forceOpen = true;
		}

		public override bool IsInteractable()
		{
			return forceOpen || lootContainer.lockScript == null || !lootContainer.lockScript.locked;
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            base.BeginInteraction(hand);

            lootContainer.SpawnContents();
        }

        public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);
			Vector3 vector = hand.Input.Pos - transform.position;
			vector = Vector3.ProjectOnPlane(vector, Root.right).normalized;
			Vector3 forward = Root.forward;
			rotAngle = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(forward, vector)), Vector3.Dot(forward, vector)) * 57.29578f;
			if (rotAngle > 0f)
			{
				rotAngle -= 360f;
			}
			if (Mathf.Abs(rotAngle - minRot) < 5f)
			{
				rotAngle = minRot;
			}
			if (Mathf.Abs(rotAngle - maxRot) < 5f)
			{
				rotAngle = maxRot;
			}
			if (rotAngle >= minRot && rotAngle <= maxRot)
			{
				transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);
			}
		}
	}
}
