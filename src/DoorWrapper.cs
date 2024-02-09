using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class DoorWrapper : MonoBehaviour
    {
        public string keyID;
        public bool flipLock;
        public float openAngleX;
        public float openAngleY;
        public float openAngleZ;
        public bool open;

        public void OnTriggerEnter(Collider collider)
        {
            if (!open)
            {
                if (collider.transform.parent != null && collider.transform.parent.parent != null && collider.transform.parent.parent.parent != null)
                {
                    MeatovItem MI = collider.transform.parent.parent.parent.GetComponent<MeatovItem>();
                    if (MI != null && MI.H3ID.Equals(keyID))
                    {
                        transform.localRotation = Quaternion.Euler(openAngleX, openAngleY, openAngleZ);
                        open = true;
                    }
                }
            }
        }
    }
}
