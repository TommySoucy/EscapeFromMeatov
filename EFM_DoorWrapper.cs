﻿using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_DoorWrapper : MonoBehaviour
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
                    EFM_CustomItemWrapper CIW = collider.transform.parent.parent.parent.GetComponent<EFM_CustomItemWrapper>();
                    if (CIW != null && CIW.ID.Equals(keyID))
                    {
                        transform.localRotation = Quaternion.Euler(openAngleX, openAngleY, openAngleZ);
                        open = true;
                    }
                }
            }
        }
    }
}
