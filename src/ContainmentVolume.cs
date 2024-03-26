using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class ContainmentVolume : MonoBehaviour
    {
        public bool hasMaxVolume;
        public int maxVolume;
        [NonSerialized]
        public int volume;
        public List<string> whitelist;
        public List<string> blacklist;
        public GameObject staticVolume;
        public GameObject activeVolume;
        public GameObject itemRoot;
        [NonSerialized]
        public List<MeatovItem> items = new List<MeatovItem>();

        public delegate void OnItemAddedDelegate(MeatovItem item);
        public event OnItemAddedDelegate OnItemAdded;
        public delegate void OnItemRemovedDelegate(MeatovItem item);
        public event OnItemRemovedDelegate OnItemRemoved;

        public bool AddItem(MeatovItem item)
        {
            staticVolume.SetActive(true);
            activeVolume.SetActive(false);

            bool fits = Mod.IDDescribedInList(item.H3ID, item.parents, whitelist, blacklist) && (!hasMaxVolume || item.volumes[item.mode] <= (maxVolume - volume));
            if (fits)
            {
                item.physObj.SetParentage(itemRoot.transform);
                if (item.physObj.IsAltHeld)
                {
                    item.physObj.IsAltHeld = false;
                }
                if (item.physObj.AltGrip != null)
                {
                    item.physObj.AltGrip.m_hand.EndInteractionIfHeld(item.physObj.AltGrip);
                    item.physObj.AltGrip.EndInteraction(item.physObj.AltGrip.m_hand);
                }

                volume += item.volumes[item.mode];
                items.Add(item);
                item.parentVolume = this;

                OnItemAddedInvoke(item);
            }
            return fits;
        }

        public void RemoveItem(MeatovItem item)
        {
            if (items.Remove(item))
            {
                volume -= item.volumes[item.mode];
                item.parentVolume = null;

                OnItemRemovedInvoke(item);
            }
        }

        public bool Offer(MeatovItem item)
        {
            bool fits = Mod.IDDescribedInList(item.H3ID, item.parents, whitelist, blacklist) && (!hasMaxVolume || item.volumes[item.mode] <= (maxVolume - volume));
            staticVolume.SetActive(!fits);
            activeVolume.SetActive(fits);
            return fits;
        }

        public void Unoffer()
        {
            staticVolume.SetActive(true);
            activeVolume.SetActive(false);
        }

        public void OnItemAddedInvoke(MeatovItem item)
        {
            if(OnItemAdded != null)
            {
                OnItemAdded(item);
            }
        }

        public void OnItemRemovedInvoke(MeatovItem item)
        {
            if(OnItemRemoved != null)
            {
                OnItemRemoved(item);
            }
        }
    }
}
