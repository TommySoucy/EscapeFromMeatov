using FistVR;

namespace EFM
{
    public class DoorBreacher : FVRInteractiveObject
    {
        public Door door;

        public override void Awake()
        {
            base.Awake();

            IsSimpleInteract = true;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            if (door != null)
            {
                door.AttemptBreach();
            }
        }
    }
}
