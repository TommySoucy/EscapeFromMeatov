using FistVR;
using H3MP;
using H3MP.Tracking;

namespace EFM
{
    public class TrackedLCSlider : TrackedObject
    {
        public TrackedLCSliderData sliderData;
        public LootContainerSlider physicalLCSlider;

        public override void EnsureUncontrolled()
        {
            if (physicalLCSlider.m_hand != null)
            {
                physicalLCSlider.ForceBreakInteraction();
            }
        }

        public override void BeginInteraction(FVRViveHand hand)
        {
            if (data.controller != GameManager.ID)
            {
                // Take control

                // Send to all clients
                data.TakeControlRecursive();
            }
        }

        protected override void OnDestroy()
        {
            // A skip of the entire destruction process may be used if H3MP has become irrelevant, like in the case of disconnection
            if (skipFullDestroy)
            {
                return;
            }

            // Remove from tracked lists, which has to be done no matter what OnDestroy because we will not have the physicalObject anymore
            TrackedLCSliderData.trackedLCSliderByLCSlider.Remove(physicalLCSlider);
            GameManager.trackedObjectByInteractive.Remove(physicalLCSlider);

            base.OnDestroy();
        }
    }
}
