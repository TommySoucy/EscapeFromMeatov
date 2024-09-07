using FistVR;
using UnityEngine;

namespace EFM
{
    public class DoorBreacher : FVRInteractiveObject
    {
        public Door door;
        public bool correctSide; // Whether this breacher can actually even be used to breach the door considering the door can only be breached from one side
        public Transform directionCheckTransform;

        public override void Awake()
        {
            base.Awake();

            IsSimpleInteract = true;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            if (door != null)
            {
                Vector3 playerVector = GM.CurrentPlayerBody.Torso.position - directionCheckTransform.position;
                if(Vector3.Angle(directionCheckTransform.forward, playerVector) < 90)
                {
                    door.AttemptBreach(correctSide);
                }
            }
        }
    }
}
