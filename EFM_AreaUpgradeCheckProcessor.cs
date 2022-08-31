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
        public ProcessorCollider[] colliders;

        public void OnEnable()
        {
            if(manager.baseManager.activeCheckProcessors[block ? 0 : 1] != null)
            {
                manager.baseManager.activeCheckProcessors[block ? 0 : 1].gameObject.SetActive(false);
            }
            manager.baseManager.activeCheckProcessors[block ? 0 : 1] = this;

            if(colliders == null)
            {
                colliders = new ProcessorCollider[transform.childCount];
                for(int i=0; i < colliders.Length; ++i)
                {
                    ProcessorCollider currentCollider = transform.GetChild(i).gameObject.AddComponent<ProcessorCollider>();
                    currentCollider.processor = this;
                }
            }
        }

        public void OnDisable()
        {
            for (int i = 0; i < manager.upgradeDialogs.Length; ++i)
            {
                if (manager.upgradeDialogs[i].activeSelf)
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
                    customItems[i].RemoveHighlight();
                }
            }
            for(int i=vanillaItems.Count-1; i>=0; i--)
            {
                if(vanillaItems[i].upgradeCheckBlockedIndex != -1 || vanillaItems[i].upgradeCheckWarnedIndex != -1)
                {
                    vanillaItems[i].upgradeCheckBlockedIndex = -1;
                    vanillaItems[i].upgradeCheckWarnedIndex = -1;
                    vanillaItems[i].RemoveHighlight();
                }
            }
            customItems.Clear();
            vanillaItems.Clear();
            Mod.currentBaseManager.activeCheckProcessors[block ? 0 : 1] = null;
        }

        public void TriggerEnter(Collider other)
        {
            Mod.instance.LogInfo("Area upgrade processor, block?: " + block + " on trigger enter called");
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
                                CIW.Highlight(Color.red);

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
                                    CIW.Highlight(Color.yellow);
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
                                VID.Highlight(Color.red);

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
                                    VID.Highlight(Color.yellow);
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
                    else
                    {
                        currentTransform = currentTransform.parent;
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void TriggerExit(Collider other)
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
                                    CIW.RemoveHighlight();
                                }
                                else
                                {
                                    CIW.Highlight(Color.yellow);
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
                                    CIW.RemoveHighlight();
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
                                    VID.RemoveHighlight();
                                }
                                else
                                {
                                    VID.Highlight(Color.yellow);
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
                                    VID.RemoveHighlight();
                                }
                                VID.upgradeCheckWarnedIndex = -1;

                                RemovedLastWarn();
                            }
                        }
                        break;
                    }
                    else
                    {
                        currentTransform = currentTransform.parent;
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

    public class ProcessorCollider : MonoBehaviour
    {
        public EFM_AreaUpgradeCheckProcessor processor;

        public void OnTriggerEnter(Collider other)
        {
            processor.TriggerEnter(other);
        }

        public void OnTriggerExit(Collider other)
        {
            processor.TriggerExit(other);
        }
    }
}
