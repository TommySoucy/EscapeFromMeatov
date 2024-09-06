using FistVR;

namespace EFM
{
    public class DoorBreacher : FVRInteractiveObject
    {
        public Door door;
        public bool correctSide; // Whether this breacher can actually even be used to breach the door considering the door can only be breached from one side

        public override void Awake()
        {
            base.Awake();

            IsSimpleInteract = true;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            if (door != null)
            {
                door.AttemptBreach(correctSide);
            }
        }
    }
}
