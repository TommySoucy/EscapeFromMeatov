using FistVR;
using H3MP;
using H3MP.Networking;
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
        public bool breachable; // Door could be breachable despite being locked
        public AudioSource audioSource;
        public AudioClip[] breachAudioClips; // 0: Success, 1: Fail
        public ParticleSystem particleSystem;

        private bool forceOpen;

        public float rotAngle;

        public TrackedDoorData trackedDoorData;

        public override void Awake()
        {
            base.Awake();

            EndInteractionIfDistant = false;

            if(lockScript != null )
            {
                lockScript.OnUnlock += OnUnlock;
            }
        }

        public void OnUnlock(bool toSend)
        {
            if(blockingColliders != null)
            {
                for(int i = 0; i < blockingColliders.Count; ++i)
                {
                    blockingColliders[i].enabled = false;
                }
            }

            if (toSend && trackedDoorData != null)
            {
                // Take control
                if (trackedDoorData.controller != GameManager.ID)
                {
                    trackedDoorData.TakeControlRecursive();
                }


                // Send unlock to others
                if (Networking.currentInstance != null)
                {
                    using (Packet packet = new Packet(Networking.unlockDoorPacketID))
                    {
                        packet.Write(trackedDoorData.trackedID);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
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

        public void AttemptBreach(bool correctSide, bool toSend = true)
        {
            if (correctSide && (breachable || lockScript==null || !lockScript.locked))
            {
                audioSource.PlayOneShot(breachAudioClips[0]);
                rotAngle = maxRot;
                transform.localEulerAngles = new Vector3(rotAngle, 0f, 0f);

            }
            else
            {
                audioSource.PlayOneShot(breachAudioClips[1]);
            }

            particleSystem.Play();

            if (toSend && trackedDoorData != null)
            {
                // Take control
                if (trackedDoorData.controller != GameManager.ID)
                {
                    trackedDoorData.TakeControlRecursive();
                }

                // Send breach to others
                if (Networking.currentInstance != null)
                {
                    using (Packet packet = new Packet(Networking.breachDoorPacketID))
                    {
                        packet.Write(trackedDoorData.trackedID);
                        packet.Write(correctSide);

                        if (ThreadManager.host)
                        {
                            ServerSend.SendTCPDataToAll(packet, true);
                        }
                        else
                        {
                            ClientSend.SendTCPData(packet, true);
                        }
                    }
                }
            }
        }
    }
}
