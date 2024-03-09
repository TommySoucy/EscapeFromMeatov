using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class DevItemSpawnerEntry : MonoBehaviour
    {
        public DevItemSpawner spawner;

        public Text text;
        public MeatovItemData item;

        public void Spawn()
        {
            MeatovItem spawnedItem = null;
            if(item.index != -1)
            {
                spawnedItem = Instantiate(Mod.GetItemPrefab(item.index), spawner.transform.position + spawner.transform.forward * -0.2f, Quaternion.identity).GetComponent<MeatovItem>();
            }
            else // Vanilla
            {
                if(IM.OD.TryGetValue(item.H3ID, out FVRObject v))
                {
                    spawnedItem = Instantiate(v.GetGameObject(), spawner.transform.position + spawner.transform.forward * -0.2f, Quaternion.identity).GetComponent<MeatovItem>();
                }
                else
                {
                    Mod.LogError("DEV: Could not get prefab for " + item.H3ID + " : " + item.name);
                }
            }

            if(spawnedItem != null && item.maxStack > 1)
            {
                spawnedItem.stack = item.maxStack;
            }
        }
    }
}
