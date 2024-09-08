using FistVR;
using H3MP;
using H3MP.Tracking;

namespace EFM
{
    public class TrackedLootContainer : TrackedObject
    {
        public TrackedLootContainerData lootContainerData;
        public LootContainer physicalLootContainer;

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
            TrackedLootContainerData.trackedLootContainerByLootContainer.Remove(physicalLootContainer);

            base.OnDestroy();
        }
    }
}
