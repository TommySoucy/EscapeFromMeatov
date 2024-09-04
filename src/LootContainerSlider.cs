using FistVR;
using UnityEngine;

namespace EFM
{
    public class LootContainerSlider : FVRInteractiveObject
    {
        public LootContainer lootContainer;

		public Vector3 slidingAxis; // Axis we should slide on
		public float handleAxisOffset; // The offset of this script's transform on the axis
		public float minAxisMagnitude; // Minimum position along the axis
		public float maxAxisMagnitude; // Maximum position along the axis
		public Vector3 defaultPos; // Default local position of this script's transform

		private bool forceOpen;

		public float newAxisMagnitude;

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

            // vector from slider to hand = hand.transform.position - transform.parent.position
            // that vector projected on vector in opening direction of slider = Vector3.Project(..., slidingAxis)
            // how far the hand is from the surface of slider = ....magnitude
            // if the drawer was moved to have its handle at the hand's position = ... - handleAxisOffset
            newAxisMagnitude = Mathf.Clamp(Vector3.Project(hand.transform.position - transform.parent.position, transform.parent.up).magnitude - handleAxisOffset, minAxisMagnitude, maxAxisMagnitude);

			transform.localPosition = defaultPos + slidingAxis * newAxisMagnitude;
		}
	}
}
