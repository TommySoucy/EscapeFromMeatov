using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class AreaUpgradeCheckProcessor : MonoBehaviour
    {
        public static readonly int maxUpCheck = 4;

        public AreaUI areaUI;
        [NonSerialized]
        public List<MeatovItem> items = new List<MeatovItem>();
        public bool block;
        public AreaUpgradeCheckProcessor counterPart;

        public void OnDisable()
        {
            for (int i = items.Count - 1; i >= 0; i--) 
            {
                if(items[i].upgradeCheckBlockedIndex != -1 || items[i].upgradeCheckWarnedIndex != -1)
                {
                    items[i].upgradeCheckBlockedIndex = -1;
                    items[i].upgradeCheckWarnedIndex = -1;
                    items[i].RemoveHighlight();
                }
            }
            items.Clear();
        }

        public void OnTriggerEnter(Collider other)
        {
            Mod.LogInfo("Area upgrade processor, block?: " + block + " on trigger enter called");
            Transform currentTransform = other.transform;
            for (int i = 0; i < maxUpCheck; ++i) 
            {
                if(currentTransform != null)
                {
                    MeatovItem MIW = currentTransform.GetComponent<MeatovItem>();
                    if(MIW != null)
                    {
                        if (block)
                        {
                            if (MIW.upgradeCheckBlockedIndex == -1)
                            {
                                if (MIW.upgradeCheckWarnedIndex == -1)
                                {
                                    MIW.upgradeCheckBlockedIndex = items.Count;
                                    items.Add(MIW);
                                }
                                else
                                {
                                    MIW.upgradeCheckBlockedIndex = MIW.upgradeCheckWarnedIndex;
                                }
                                MIW.Highlight(Color.red);

                                areaUI.blockDialog.SetActive(true);
                                areaUI.warningDialog.SetActive(false);
                                areaUI.upgradeConfirmDialog.SetActive(false);
                            }
                        }
                        else
                        {
                            if (MIW.upgradeCheckWarnedIndex == -1)
                            {
                                if (MIW.upgradeCheckBlockedIndex == -1)
                                {
                                    MIW.upgradeCheckWarnedIndex = items.Count;
                                    items.Add(MIW);
                                    MIW.Highlight(Color.yellow);
                                }
                                else
                                {
                                    MIW.upgradeCheckWarnedIndex = MIW.upgradeCheckBlockedIndex;
                                }

                                areaUI.warningDialog.SetActive(!areaUI.blockDialog.activeSelf);
                                areaUI.upgradeConfirmDialog.SetActive(false);
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

        public void OnTriggerExit(Collider other)
        {
            Transform currentTransform = other.transform;
            for (int i = 0; i < maxUpCheck; ++i)
            {
                if (currentTransform != null)
                {
                    MeatovItem MIW = currentTransform.GetComponent<MeatovItem>();
                    if (MIW != null)
                    {
                        if (block)
                        {
                            if (MIW.upgradeCheckBlockedIndex != -1)
                            {
                                if(MIW.upgradeCheckWarnedIndex == -1)
                                {
                                    items[items.Count - 1].upgradeCheckBlockedIndex = items[items.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : MIW.upgradeCheckBlockedIndex;
                                    items[items.Count - 1].upgradeCheckWarnedIndex = items[items.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : MIW.upgradeCheckBlockedIndex;
                                    items[MIW.upgradeCheckBlockedIndex] = items[items.Count - 1];
                                    items.RemoveAt(items.Count - 1);
                                    MIW.RemoveHighlight();
                                }
                                else
                                {
                                    MIW.Highlight(Color.yellow);
                                }
                                MIW.upgradeCheckBlockedIndex = -1;

                                RemovedLastBlock();
                            }
                        }
                        else
                        {
                            if (MIW.upgradeCheckWarnedIndex != -1)
                            {
                                if (MIW.upgradeCheckBlockedIndex == -1)
                                {
                                    items[items.Count - 1].upgradeCheckBlockedIndex = items[items.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : MIW.upgradeCheckWarnedIndex;
                                    items[items.Count - 1].upgradeCheckWarnedIndex = items[items.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : MIW.upgradeCheckWarnedIndex;
                                    items[MIW.upgradeCheckWarnedIndex] = items[items.Count - 1];
                                    items.RemoveAt(items.Count - 1);
                                    MIW.RemoveHighlight();
                                }
                                MIW.upgradeCheckWarnedIndex = -1;

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
            if (items.Count == 0)
            {
                areaUI.blockDialog.SetActive(false);

                if (counterPart.items.Count > 0)
                {
                    areaUI.warningDialog.SetActive(true);
                }
                else
                {
                    areaUI.upgradeConfirmDialog.SetActive(true);
                }
            }
        }

        private void RemovedLastWarn()
        {
            if (items.Count == 0)
            {
                areaUI.warningDialog.SetActive(false);
                areaUI.upgradeConfirmDialog.SetActive(true);
            }
        }
    }
}
