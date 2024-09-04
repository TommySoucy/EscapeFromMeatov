using System;
using UnityEngine;

namespace EFM
{
    public class Lock : MonoBehaviour
    {
        public string keyID; // Tarkov ID of key item to this lock
        public AudioClip unlockClip;
        public AudioSource unlockAudioSource;

        [NonSerialized]
        public bool locked;

        public delegate void OnUnlockDelegate();
        public static event OnUnlockDelegate OnUnlock;

        public void OnTriggerEnter(Collider other)
        {
            if (locked)
            {
                MeatovItem item = other.GetComponentInParent<MeatovItem>();
                if (item != null && item.tarkovID.Equals("keyID"))
                {
                    locked = false;
                    if(unlockClip != null && unlockAudioSource != null)
                    {
                        unlockAudioSource.PlayOneShot(unlockClip);
                    }

                    OnUnlockInvoke();
                }
            }
        }

        public void OnUnlockInvoke()
        {
            if(OnUnlock != null)
            {
                OnUnlock();
            }
        }
    }
}
