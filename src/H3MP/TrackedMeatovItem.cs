using FistVR;
using H3MP;
using H3MP.Tracking;
using UnityEngine.SceneManagement;

namespace EFM
{
    public class TrackedMeatovItem : TrackedItem
    {
        public TrackedMeatovItemData meatovItemData;
        public MeatovItem physicalMeatovItem;

        public override void Unsecure()
        {
            if(securedCode >= 515 && securedCode <= 521)
            {
                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());

                physicalItem.SetQuickBeltSlot(StatusUI.instance.equipmentSlots[securedCode - 515]);

                securedCode = -1;
            }
            else
            {
                base.Unsecure();
            }
        }

        protected override void OnDestroy()
        {
            TrackedMeatovItemData.trackedMeatovItemByMeatovItem.Remove(physicalMeatovItem);
            GameManager.trackedObjectByObject.Remove(physicalMeatovItem);

            base.OnDestroy();
        }
    }
}
