using FistVR;
using H3MP;
using H3MP.Tracking;

namespace EFM
{
    public class TrackedLCCover : TrackedObject
    {
        public TrackedLCCoverData coverData;
        public LootContainerCover physicalLCCover;

        public override void EnsureUncontrolled()
        {
            if (physicalLCCover.m_hand != null)
            {
                physicalLCCover.ForceBreakInteraction();
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
            TrackedLCCoverData.trackedLCCoverByLCCover.Remove(physicalLCCover);
            GameManager.trackedObjectByInteractive.Remove(physicalLCCover);

            base.OnDestroy();
        }
    }
}
