using FistVR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class DoorLockChecker : MonoBehaviour
    {
        public SideHingedDestructibleDoorDeadBolt deadBolt;
        public GameObject blocks;

        private bool disabledBlocks;

        private void FixedUpdate()
        {

            if (!disabledBlocks && (deadBolt.State == SideHingedDestructibleDoorDeadBolt.DeadBoltState.Open || deadBolt.State == SideHingedDestructibleDoorDeadBolt.DeadBoltState.Broken))
            {
                blocks.SetActive(false);

                // TODO: Use a unit of the key used if applicable. Right now when we put a key in a door, it destroys the key and makes a copy in the lock
                // When we pull it out it makes another copy from the item ID. We will need to make sure this is kept track of so that the key actually gets used up

                disabledBlocks = true;
            }
        }
    }
}
