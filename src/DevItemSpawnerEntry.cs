﻿using FistVR;
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
                spawnedItem.SetData(item);
            }
            else // Vanilla
            {
                if(IM.OD.TryGetValue(item.H3ID, out FVRObject v))
                {
                    spawnedItem = Instantiate(v.GetGameObject(), spawner.transform.position + spawner.transform.forward * -0.2f, Quaternion.identity).GetComponent<MeatovItem>();
                    spawnedItem.SetData(item);
                }
                else
                {
                    Mod.LogError("DEV: Could not get prefab for "+item.tarkovID + ":" + item.H3ID + " : " + item.name);
                }
            }

            if(spawnedItem != null && item.maxStack > 1)
            {
                spawnedItem.stack = item.maxStack;
            }
        }
    }
}
