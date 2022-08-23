using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_AreaUpgradeCheckProcessor : MonoBehaviour
    {
        public static readonly int maxUpCheck = 4;

        public EFM_BaseAreaManager manager;
        public List<EFM_CustomItemWrapper> customItems = new List<EFM_CustomItemWrapper>();
        public List<EFM_VanillaItemDescriptor> vanillaItems = new List<EFM_VanillaItemDescriptor>();
        public bool block;

        public void OnEnable()
        {
            if(manager.baseManager.activeCheckProcessors[0] != null)
            {
                manager.baseManager.activeCheckProcessors[0].gameObject.SetActive(false);
                manager.baseManager.activeCheckProcessors[1].gameObject.SetActive(false);
            }
            manager.baseManager.activeCheckProcessors[block ? 0 : 1] = this;
        }

        public void OnDisable()
        {
            for (int i = 0; i < manager.upgradeDialogs.Length; ++i)
            {
                if (!manager.upgradeDialogs[i].activeSelf)
                {
                    manager.upgradeDialogs[i].SetActive(false);
                }
            }
            for(int i=customItems.Count-1; i>=0; i--)
            {
                if(customItems[i].upgradeCheckBlockedIndex != -1 || customItems[i].upgradeCheckWarnedIndex != -1)
                {
                    customItems[i].upgradeCheckBlockedIndex = -1;
                    customItems[i].upgradeCheckWarnedIndex = -1;
                    // TODO: Remove highlight on item
                }
            }
            for(int i=vanillaItems.Count-1; i>=0; i--)
            {
                if(vanillaItems[i].upgradeCheckBlockedIndex != -1 || vanillaItems[i].upgradeCheckWarnedIndex != -1)
                {
                    vanillaItems[i].upgradeCheckBlockedIndex = -1;
                    vanillaItems[i].upgradeCheckWarnedIndex = -1;
                    // TODO: Remove highlight on item
                }
            }
            customItems.Clear();
            vanillaItems.Clear();
            Mod.currentBaseManager.activeCheckProcessors[block ? 0 : 1] = null;
        }

        public void OnTriggerEnter(Collider other)
        {
            Transform currentTransform = other.transform;
            for(int i=0; i< maxUpCheck; ++i)
            {
                if(currentTransform != null)
                {
                    EFM_CustomItemWrapper CIW = currentTransform.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor VID = currentTransform.GetComponent<EFM_VanillaItemDescriptor>();
                    if(CIW != null)
                    {
                        if (block)
                        {
                            if (CIW.upgradeCheckBlockedIndex == -1)
                            {
                                if (CIW.upgradeCheckWarnedIndex == -1)
                                {
                                    CIW.upgradeCheckBlockedIndex = customItems.Count;
                                    customItems.Add(CIW);
                                }
                                else
                                {
                                    CIW.upgradeCheckBlockedIndex = CIW.upgradeCheckWarnedIndex;
                                }
                                // TODO: Set red highlight on item

                                manager.upgradeDialogs[0].SetActive(true);
                                manager.upgradeDialogs[1].SetActive(false);
                                manager.upgradeDialogs[2].SetActive(false);
                            }
                        }
                        else
                        {
                            if (CIW.upgradeCheckWarnedIndex == -1)
                            {
                                if (CIW.upgradeCheckBlockedIndex == -1)
                                {
                                    CIW.upgradeCheckWarnedIndex = customItems.Count;
                                    customItems.Add(CIW);
                                    // TODO: Set yellow highlight
                                }
                                else
                                {
                                    CIW.upgradeCheckWarnedIndex = CIW.upgradeCheckBlockedIndex;
                                }

                                manager.upgradeDialogs[1].SetActive(!manager.upgradeDialogs[0].activeSelf);
                                manager.upgradeDialogs[2].SetActive(false);
                            }
                        }
                        break;
                    }
                    else if(VID != null)
                    {
                        if (block)
                        {
                            if (VID.upgradeCheckBlockedIndex == -1)
                            {
                                if (VID.upgradeCheckWarnedIndex == -1)
                                {
                                    VID.upgradeCheckBlockedIndex = vanillaItems.Count;
                                    vanillaItems.Add(VID);
                                }
                                else
                                {
                                    VID.upgradeCheckBlockedIndex = VID.upgradeCheckWarnedIndex;
                                }
                                // TODO: Set red highlight on item

                                manager.upgradeDialogs[0].SetActive(true);
                                manager.upgradeDialogs[1].SetActive(false);
                                manager.upgradeDialogs[2].SetActive(false);
                            }
                        }
                        else
                        {
                            if (VID.upgradeCheckWarnedIndex == -1)
                            {
                                if (VID.upgradeCheckBlockedIndex == -1)
                                {
                                    VID.upgradeCheckWarnedIndex = vanillaItems.Count;
                                    vanillaItems.Add(VID);
                                    // TODO: Set yellow highlight
                                }
                                else
                                {
                                    VID.upgradeCheckWarnedIndex = VID.upgradeCheckBlockedIndex;
                                }

                                manager.upgradeDialogs[1].SetActive(!manager.upgradeDialogs[0].activeSelf);
                                manager.upgradeDialogs[2].SetActive(false);
                            }
                        }
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            Transform currentTransform = other.transform;
            for (int i = 0; i < maxUpCheck; ++i)
            {
                if (currentTransform != null)
                {
                    EFM_CustomItemWrapper CIW = currentTransform.GetComponent<EFM_CustomItemWrapper>();
                    EFM_VanillaItemDescriptor VID = currentTransform.GetComponent<EFM_VanillaItemDescriptor>();
                    if (CIW != null)
                    {
                        if (block)
                        {
                            if (CIW.upgradeCheckBlockedIndex != -1)
                            {
                                if(CIW.upgradeCheckWarnedIndex == -1)
                                {
                                    customItems[customItems.Count - 1].upgradeCheckBlockedIndex = customItems[customItems.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : CIW.upgradeCheckBlockedIndex;
                                    customItems[customItems.Count - 1].upgradeCheckWarnedIndex = customItems[customItems.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : CIW.upgradeCheckBlockedIndex;
                                    customItems[CIW.upgradeCheckBlockedIndex] = customItems[customItems.Count - 1];
                                    customItems.RemoveAt(customItems.Count - 1);
                                    // TODO: Remove red highlight on item
                                }
                                else
                                {
                                    // TODO: Set yellow highlight on item
                                }
                                CIW.upgradeCheckBlockedIndex = -1;

                                RemovedLastBlock();
                            }
                        }
                        else
                        {
                            if (CIW.upgradeCheckWarnedIndex != -1)
                            {
                                if (CIW.upgradeCheckBlockedIndex == -1)
                                {
                                    customItems[customItems.Count - 1].upgradeCheckBlockedIndex = customItems[customItems.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : CIW.upgradeCheckWarnedIndex;
                                    customItems[customItems.Count - 1].upgradeCheckWarnedIndex = customItems[customItems.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : CIW.upgradeCheckWarnedIndex;
                                    customItems[CIW.upgradeCheckBlockedIndex] = customItems[customItems.Count - 1];
                                    customItems.RemoveAt(customItems.Count - 1);
                                    // TODO: Remove yellow highlight on item
                                }
                                CIW.upgradeCheckWarnedIndex = -1;

                                RemovedLastWarn();
                            }
                        }
                        break;
                    }
                    else if (VID != null)
                    {
                        if (block)
                        {
                            if (VID.upgradeCheckBlockedIndex != -1)
                            {
                                if (VID.upgradeCheckWarnedIndex == -1)
                                {
                                    vanillaItems[vanillaItems.Count - 1].upgradeCheckBlockedIndex = vanillaItems[vanillaItems.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : VID.upgradeCheckBlockedIndex;
                                    vanillaItems[vanillaItems.Count - 1].upgradeCheckWarnedIndex = vanillaItems[vanillaItems.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : VID.upgradeCheckBlockedIndex;
                                    vanillaItems[VID.upgradeCheckBlockedIndex] = vanillaItems[vanillaItems.Count - 1];
                                    vanillaItems.RemoveAt(vanillaItems.Count - 1);
                                    // TODO: Remove red highlight on item
                                }
                                else
                                {
                                    // TODO: Set yellow highlight on item
                                }
                                VID.upgradeCheckBlockedIndex = -1;

                                RemovedLastBlock();
                            }
                        }
                        else
                        {
                            if (VID.upgradeCheckWarnedIndex != -1)
                            {
                                if (VID.upgradeCheckBlockedIndex == -1)
                                {
                                    vanillaItems[vanillaItems.Count - 1].upgradeCheckBlockedIndex = vanillaItems[vanillaItems.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : VID.upgradeCheckWarnedIndex;
                                    vanillaItems[vanillaItems.Count - 1].upgradeCheckWarnedIndex = vanillaItems[vanillaItems.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : VID.upgradeCheckWarnedIndex;
                                    vanillaItems[VID.upgradeCheckBlockedIndex] = vanillaItems[vanillaItems.Count - 1];
                                    vanillaItems.RemoveAt(vanillaItems.Count - 1);
                                    // TODO: Remove yellow highlight on item
                                }
                                VID.upgradeCheckWarnedIndex = -1;

                                RemovedLastWarn();
                            }
                        }
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        private void RemovedLastBlock()
        {
            if (customItems.Count == 0 && vanillaItems.Count == 0)
            {
                manager.upgradeDialogs[0].SetActive(false);

                if (manager.baseManager.activeCheckProcessors[1].customItems.Count > 0 || manager.baseManager.activeCheckProcessors[1].vanillaItems.Count > 0)
                {
                    manager.upgradeDialogs[1].SetActive(true);
                }
                else
                {
                    manager.upgradeDialogs[2].SetActive(true);
                }
            }
        }

        private void RemovedLastWarn()
        {
            if (customItems.Count == 0 && vanillaItems.Count == 0)
            {
                manager.upgradeDialogs[1].SetActive(false);
                manager.upgradeDialogs[2].SetActive(true);
            }
        }
    }
}
