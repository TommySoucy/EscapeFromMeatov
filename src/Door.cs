using FistVR;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Door : FVRInteractiveObject
    {
        public Lock lockScript;

        public Transform Root;
        public float minRot;
        public float maxRot;
        public List<Collider> blockingColliders; // Nav block colliders to disable once the door have been unlocked

        private bool forceOpen;

        public float rotAngle;

        public override void Awake()
        {
            base.Awake();

            EndInteractionIfDistant = false;

            if(lockScript != null )
            {
                lockScript.OnUnlock += OnUnlock;
            }
        }

        public void OnUnlock()
        {
            if(blockingColliders != null)
            {
                for(int i = 0; i < blockingColliders.Count; ++i)
                {
                    blockingColliders[i].enabled = false;
                }
            }
        }

        public void ForceOpen()
        {
            forceOpen = true;
        }

        public override bool IsInteractable()
        {
            return forceOpen || lockScript == null || !lockScript.locked;
        }

        public override void UpdateInteraction(FVRViveHand hand)
        {
            base.UpdateInteraction(hand);
            Vector3 vector = hand.Input.Pos - transform.position;
            vector = Vector3.ProjectOnPlane(vector, Root.right).normalized;
            Vector3 forward = Root.transform.forward;
            rotAngle = Mathf.Atan2(Vector3.Dot(Root.right, Vector3.Cross(forward, vector)), Vector3.Dot(forward, vector)) * 57.29578f;
            if (rotAngle > 0f)
            {
                rotAngle -= 360f;
            }
            if (Mathf.Abs(rotAngle - minRot) < 5f)
            {
                rotAngle = minRot;
            }
            if (Mathf.Abs(rotAngle - maxRot) < 5f)
            {
                rotAngle = maxRot;
            }
            if (rotAngle >= minRot && rotAngle <= maxRot)
            {
                transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);
            }
        }
    }
}
