﻿using UnityEngine;

namespace EFM
{
    public class StackTrigger : MonoBehaviour
    {
        public MeatovItem stackableWrapper;
        public bool stacked;
        public void OnTriggerEnter(Collider collider)
        {
            if (stacked)
            {
                stacked = false;
                return;
            }

            StackTrigger otherStackTrigger = collider.GetComponent<StackTrigger>();
            if (otherStackTrigger != null)
            {
                MeatovItem otherItemWrapper = otherStackTrigger.stackableWrapper;

                // Make sure the items have same ID
                if (stackableWrapper.tarkovID == otherItemWrapper.tarkovID)
                {
                    // Check that both are held
                    if (stackableWrapper.physObj.m_hand != null && otherItemWrapper.physObj.m_hand != null)
                    {
                        // Decide which direction to stack, we want to stack on greatest amount, if amount is same, stack in lowest Y, else stack in this one
                        if (stackableWrapper.stack > otherItemWrapper.stack)
                        {
                            // Stack on this one
                            StackOnThis(otherStackTrigger);
                        }
                        else if (stackableWrapper.stack < otherItemWrapper.stack)
                        {
                            // Stack on other
                            StackOnOther(otherStackTrigger);
                        }
                        else if (transform.position.y < otherStackTrigger.transform.position.y)
                        {
                            // Stack on this one
                            StackOnThis(otherStackTrigger);
                        }
                        else if (transform.position.y > otherStackTrigger.transform.position.y)
                        {
                            // Stack on other
                            StackOnOther(otherStackTrigger);
                        }
                        else // Same amount in both stacks and same height
                        {
                            // Stack on this one
                            StackOnThis(otherStackTrigger);
                        }
                    }
                }
            }
        }

        private void StackOnThis(StackTrigger otherStackTrigger)
        {
            MeatovItem otherItemWrapper = otherStackTrigger.stackableWrapper;
            int newStack = stackableWrapper.stack + otherItemWrapper.stack;
            if (newStack <= stackableWrapper.maxStack)
            {
                stackableWrapper.stack = newStack;

                // Must set other's stack to 0 before destruction
                // so total weight is properly managed
                otherStackTrigger.stackableWrapper.stack = 0;
                otherStackTrigger.stacked = true;

                otherItemWrapper.Destroy();
            }
            else
            {
                stackableWrapper.stack = stackableWrapper.maxStack;
                otherItemWrapper.stack = newStack - stackableWrapper.maxStack;
                otherStackTrigger.stacked = true;
            }
        }

        private void StackOnOther(StackTrigger otherStackTrigger)
        {
            MeatovItem otherItemWrapper = otherStackTrigger.stackableWrapper;
            int newStack = stackableWrapper.stack + otherItemWrapper.stack;
            if (newStack <= otherItemWrapper.maxStack)
            {
                otherItemWrapper.stack = newStack;

                // Must set stack to 0 before destruction
                // so total weight is properly managed
                stackableWrapper.stack = 0;
                stacked = true;

                stackableWrapper.Destroy();
            }
            else
            {
                otherItemWrapper.stack = otherItemWrapper.maxStack;
                stackableWrapper.stack = newStack - otherItemWrapper.maxStack;
                otherStackTrigger.stacked = true;
            }
        }
    }
}
