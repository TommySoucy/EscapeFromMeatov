using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_StackTrigger : MonoBehaviour
    {
        public EFM_CustomItemWrapper stackableWrapper;
        public bool stacked;
        public void OnTriggerEnter(Collider collider)
        {
            if (stacked)
            {
                stacked = false;
                return;
            }

            EFM_StackTrigger otherStackTrigger = collider.GetComponent<EFM_StackTrigger>();
            if (otherStackTrigger != null)
            {
                EFM_CustomItemWrapper otherItemWrapper = otherStackTrigger.stackableWrapper;

                // Make sure the items have same ID
                if (stackableWrapper.ID == otherItemWrapper.ID)
                {
                    // Check that both are held
                    if((Mod.leftHand.fvrHand.CurrentInteractable.gameObject.Equals(stackableWrapper.gameObject) &&
                       Mod.rightHand.fvrHand.CurrentInteractable.gameObject.Equals(otherItemWrapper.gameObject)) || 
                       (Mod.leftHand.fvrHand.CurrentInteractable.gameObject.Equals(otherItemWrapper.gameObject) &&
                       Mod.rightHand.fvrHand.CurrentInteractable.gameObject.Equals(stackableWrapper.gameObject)))
                    {
                        // Decide which direction to stack, we want to stack on greatest amount, if amount is same, stack in lowest Y, else stack in this one
                        if(stackableWrapper.stack > otherItemWrapper.stack)
                        {
                            // Stack on this one
                            StackOnThis(otherStackTrigger);
                        }
                        else if(stackableWrapper.stack < otherItemWrapper.stack)
                        {
                            // Stack on other
                            StackOnOther(otherStackTrigger);
                        }
                        else if(transform.position.y < otherStackTrigger.transform.position.y)
                        {
                            // Stack on this one
                            StackOnThis(otherStackTrigger);
                        }
                        else if(transform.position.y > otherStackTrigger.transform.position.y)
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

        private void StackOnThis(EFM_StackTrigger otherStackTrigger)
        {
            EFM_CustomItemWrapper otherItemWrapper = otherStackTrigger.stackableWrapper;
            int newStack = stackableWrapper.stack + otherItemWrapper.stack;
            if (newStack <= stackableWrapper.maxStack)
            {
                stackableWrapper.stack = newStack;
                otherStackTrigger.stacked = true;
                Destroy(otherItemWrapper.gameObject);
            }
            else
            {
                stackableWrapper.stack = stackableWrapper.maxStack;
                otherItemWrapper.stack = newStack - stackableWrapper.maxStack;
                otherStackTrigger.stacked = true;
            }
        }

        private void StackOnOther(EFM_StackTrigger otherStackTrigger)
        {
            EFM_CustomItemWrapper otherItemWrapper = otherStackTrigger.stackableWrapper;
            int newStack = stackableWrapper.stack + otherItemWrapper.stack;
            if (newStack <= otherItemWrapper.maxStack)
            {
                otherItemWrapper.stack = newStack;
                otherStackTrigger.stacked = true;
                Destroy(gameObject);
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
