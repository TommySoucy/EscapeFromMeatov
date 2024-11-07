using FistVR;
using System;
using UnityEngine;

namespace EFM
{
    public class LootContainerSlider : FVRInteractiveObject
    {
        public LootContainer lootContainer;

		public Vector3 slidingAxis; // Local axis we should slide on
		public float handleAxisOffset; // The offset of this script's transform on the axis
		public float minAxisMagnitude; // Minimum position along the axis
		public float maxAxisMagnitude; // Maximum position along the axis

		private bool forceOpen;

		[NonSerialized]
		public float newAxisMagnitude;
        [NonSerialized]
        public Vector3 defaultPos;

        public TrackedLCSliderData trackedLCSliderData;

        public override void Awake()
		{
            base.Awake();

			defaultPos = transform.position;

            // Transform sliding axis to world frame
            slidingAxis = Transform.TransformVector(slidingAxis);

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
			Mod.LogInfo("Drawer being interaction, spawning contents");
            base.BeginInteraction(hand);

			lootContainer.SpawnContents();
        }

        public override void UpdateInteraction(FVRViveHand hand)
		{
			base.UpdateInteraction(hand);

			// vector from slider to hand = hand.transform.position - transform.parent.position
			Vector3 handVector = hand.transform.position - transform.parent.position;
			// that vector projected on vector in opening direction of slider = Vector3.Project(..., slidingAxis)
			Vector3 projection = Vector3.Project(handVector, slidingAxis);
            // how far the hand is from the surface of slider = ....magnitude
            // if the drawer was moved to have its handle at the hand's position = ... - handleAxisOffset
            newAxisMagnitude = Mathf.Clamp(projection.magnitude - handleAxisOffset, minAxisMagnitude, maxAxisMagnitude);

			transform.position = defaultPos + slidingAxis * newAxisMagnitude;
		}
	}
}
