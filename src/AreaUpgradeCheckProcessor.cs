using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class AreaUpgradeCheckProcessor : MonoBehaviour
    {
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
                    items[i].upgradeBlockCount = 0;
                    items[i].upgradeWarnCount = 0;
                    items[i].RemoveHighlight();
                }
            }
            items.Clear();
        }

        public void OnTriggerEnter(Collider other)
        {
            MeatovItem item = other.GetComponentInParents<MeatovItem>();
            if(item != null)
            {
                if (block)
                {
                    ++item.upgradeBlockCount;

                    // If item is not already blocking
                    if (item.upgradeCheckBlockedIndex == -1)
                    {
                        // Set to block and add to items
                        item.upgradeCheckBlockedIndex = items.Count;
                        items.Add(item);
                        Color color = Color.red;
                        color.a = 0.2f;
                        item.Highlight(color);

                        areaUI.blockDialog.SetActive(true);
                        areaUI.warningDialog.SetActive(false);
                        areaUI.upgradeConfirmDialog.SetActive(false);
                    }
                }
                else
                {
                    ++item.upgradeWarnCount;

                    // If item not already warning
                    if (item.upgradeCheckWarnedIndex == -1)
                    {
                        // Set to warn and add to items
                        item.upgradeCheckWarnedIndex = items.Count;
                        items.Add(item);

                        if(item.upgradeBlockCount <= 0)
                        {
                            // Only highlight if not blocking because blocking gets priority
                            Color color = Color.yellow;
                            color.a = 0.2f;
                            item.Highlight(color);
                        }

                        // Only have warning dialog if not blocking because blocking gets priority
                        areaUI.warningDialog.SetActive(!areaUI.blockDialog.activeSelf);
                        areaUI.upgradeConfirmDialog.SetActive(false);
                    }
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            MeatovItem item = other.GetComponentInParents<MeatovItem>();
            if (item != null)
            {
                if (block)
                {
                    --item.upgradeBlockCount;

                    if (item.upgradeBlockCount <= 0)
                    {
                        items[items.Count - 1].upgradeCheckBlockedIndex = items[items.Count - 1].upgradeCheckBlockedIndex == -1 ? -1 : item.upgradeCheckBlockedIndex;
                        items[item.upgradeCheckBlockedIndex] = items[items.Count - 1];
                        items.RemoveAt(items.Count - 1);
                        item.upgradeCheckBlockedIndex = -1;

                        if (item.upgradeCheckWarnedIndex == -1)
                        {
                            item.RemoveHighlight();
                        }
                        else
                        {
                            Color color = Color.yellow;
                            color.a = 0.2f;
                            item.Highlight(color);
                        }

                        RemovedBlock();
                    }
                }
                else
                {
                    --item.upgradeWarnCount;

                    if (item.upgradeWarnCount <= 0)
                    {
                        items[items.Count - 1].upgradeCheckWarnedIndex = items[items.Count - 1].upgradeCheckWarnedIndex == -1 ? -1 : item.upgradeCheckWarnedIndex;
                        items[item.upgradeCheckWarnedIndex] = items[items.Count - 1];
                        items.RemoveAt(items.Count - 1);
                        item.upgradeCheckWarnedIndex = -1;

                        if (item.upgradeCheckBlockedIndex == -1)
                        {
                            item.RemoveHighlight();
                        }

                        RemovedWarn();
                    }
                }
            }
        }

        private void RemovedBlock()
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

        private void RemovedWarn()
        {
            if (items.Count == 0)
            {
                areaUI.warningDialog.SetActive(false);

                if (counterPart.items.Count <= 0)
                {
                    areaUI.upgradeConfirmDialog.SetActive(true);
                }
            }
        }
    }
}
