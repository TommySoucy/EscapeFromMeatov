using H3MP;
using H3MP.Tracking;

namespace EFM
{
    public class TrackedMeatovItem : TrackedItem
    {
        public TrackedMeatovItemData meatovItemData;
        public MeatovItem physicalMeatovItem;

        protected override void OnDestroy()
        {
            TrackedMeatovItemData.trackedMeatovItemByMeatovItem.Remove(physicalMeatovItem);
            GameManager.trackedObjectByObject.Remove(physicalMeatovItem);

            base.OnDestroy();
        }
    }
}
