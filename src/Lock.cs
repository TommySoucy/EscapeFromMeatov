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

        public delegate void OnUnlockDelegate(bool toSend);
        public event OnUnlockDelegate OnUnlock;

        public void OnTriggerEnter(Collider other)
        {
            Mod.LogInfo("Lock trigger enter");
            if (locked)
            {
                MeatovItem item = other.GetComponentInParent<MeatovItem>();
                Mod.LogInfo("\tLocked, got item?: "+(item != null)+", ID: "+(item == null ? "null" : item.tarkovID)+" and we want "+ keyID);
                if (item != null && item.tarkovID.Equals(keyID))
                {
                    Mod.LogInfo("\t\tUnlocking");
                    UnlockAction();
                }
            }
        }

        public void UnlockAction(bool toSend = true)
        {
            locked = false;
            if (unlockClip != null && unlockAudioSource != null)
            {
                unlockAudioSource.PlayOneShot(unlockClip);
            }

            OnUnlockInvoke(toSend);
        }

        public void LockAction()
        {
            locked = true;
            if (unlockClip != null && unlockAudioSource != null)
            {
                unlockAudioSource.PlayOneShot(unlockClip);
            }
        }

        public void OnUnlockInvoke(bool toSend)
        {
            if(OnUnlock != null)
            {
                OnUnlock(toSend);
            }
        }
    }
}
