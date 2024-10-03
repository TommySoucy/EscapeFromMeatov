using FistVR;
using H3MP.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class Extraction : MonoBehaviour
    {
        [NonSerialized]
        public Text cardRequirementText;
        [NonSerialized]
        public float distToSpawn;
        private bool _active
        {
            get { return _active; }
            set
            {
                bool preValue = _active;
                _active = value;
                if(_active != preValue)
                {
                    UpdateDisplay();
                }
            }
        }
        [NonSerialized]
        public bool active; // Whether the extraction can be used by the player this raid
        private bool _timeActive
        {
            get { return _timeActive; }
            set
            {
                bool preValue = _timeActive;
                _timeActive = value;
                if(preValue != _timeActive)
                {
                    UpdateDisplay();
                }
            }
        }
        [NonSerialized]
        public bool timeActive = true; // Whether the extraction is actually active considering activeTimes
        [NonSerialized]
        public float timeToCheck = -1; // How long until the extraction needs to toggle its timeActive considering activeTimes
        [NonSerialized]
        public int extractionsIndex; // Current index in RaidManager extractions list
        [NonSerialized]
        public FVRPlayerHitbox playerHead; // Currently colliding player head hitbox
        [NonSerialized]
        public float extractionTimer = -1;

        public string extractionName;
        public float extractionTime = 10;
        public List<Vector2> activeTimes; // Time ranges in seconds from 0 to 86400 (24h) during which this extraction can actually be used
        public List<string> itemRequirements; // Items consumed upon using the extraction
        public List<int> itemRequirementCounts; // The amount of each item consumed upon using the extraction
        public List<string> itemWhitelist; // Items that must be in player inventory upon extraction, not consumed
        public List<string> itemBlacklist; // Items that must NOT be in player inventory upon extraction
        public GameObject activeObject;

        public void Start()
        {
            if(activeTimes == null || activeTimes.Count == 0)
            {
                timeActive = true;
            }
            else
            {
                CalculateTimetoCheck();
            }
        }

        public void Update()
        {
            if(timeToCheck > 0)
            {
                timeToCheck -= Time.deltaTime;
                if(timeToCheck <= 0)
                {
                    CalculateTimetoCheck();
                }
            }

            if(extractionTimer > 0)
            {
                extractionTimer -= Time.deltaTime;
                if(extractionTimer <= 0)
                {
                    Extract();
                }
            }
        }

        public void CalculateTimetoCheck()
        {
            timeActive = false;
            for(int i=0; i < activeTimes.Count; ++i)
            {
                if(RaidManager.instance.time >= activeTimes[i].x && RaidManager.instance.time <= activeTimes[i].y)
                {
                    timeActive = true;
                    timeToCheck = activeTimes[i].y - RaidManager.instance.time;
                    break;
                }
            }
            if (!timeActive)
            {
                bool found = false;
                for (int i = activeTimes.Count - 1; i >= 0; --i)
                {
                    if (RaidManager.instance.time < activeTimes[i].x) 
                    {
                        found = true;
                        timeToCheck = activeTimes[i].x - RaidManager.instance.time;
                    }
                }
                if (!found)
                {
                    timeToCheck = activeTimes[0].x - (RaidManager.instance.time - 86400);
                }
            }
        }

        /// <summary>
        /// Can be used to update the extraction based on whether it is time/active or not
        /// </summary>
        public virtual void UpdateDisplay()
        {
            if(activeObject != null)
            {
                activeObject.SetActive(timeActive && active);
            }
        }

        public bool UpdateRequirementFulfillment()
        {
            if (itemRequirements != null && itemRequirementCounts != null && itemRequirementCounts.Count == itemRequirements.Count)
            {
                for (int i = 0; i < itemRequirements.Count; ++i)
                {
                    int inventoryCount = 0;
                    if (!Mod.playerInventory.TryGetValue(itemRequirements[i], out inventoryCount) || inventoryCount < itemRequirementCounts[i])
                    {
                        cardRequirementText.gameObject.SetActive(true);
                        cardRequirementText.text = "Requires x" + itemRequirementCounts[i] + " " + (Mod.defaultItemData.TryGetValue(itemRequirements[i], out MeatovItemData itemData) ? itemData.name : "null");
                        return false;
                    }
                }
            }
            if (itemWhitelist != null)
            {
                for (int i = 0; i < itemWhitelist.Count; ++i)
                {
                    if (Mod.itemsByParents.TryGetValue(itemWhitelist[i], out List<MeatovItemData> itemDatas))
                    {
                        for (int j = 0; j < itemDatas.Count; ++j)
                        {
                            int inventoryCount = 0;
                            if (!Mod.playerInventory.TryGetValue(itemDatas[j].tarkovID, out inventoryCount) || inventoryCount <= 0)
                            {
                                cardRequirementText.gameObject.SetActive(true);
                                string categoryName = null;
                                if (Mod.localeDB[itemWhitelist[i] + " Name"] != null)
                                {
                                    categoryName = Mod.localeDB[itemWhitelist[i] + " Name"].ToString();
                                }
                                else
                                {
                                    categoryName = Mod.GetCorrectCategoryName(Mod.itemDB[itemWhitelist[i]]["_name"].ToString());
                                }
                                cardRequirementText.text = "Requires " + categoryName;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        int inventoryCount = 0;
                        if (!Mod.playerInventory.TryGetValue(itemWhitelist[i], out inventoryCount) || inventoryCount <= 0)
                        {
                            cardRequirementText.gameObject.SetActive(true);
                            cardRequirementText.text = "Requires " + (Mod.defaultItemData.TryGetValue(itemWhitelist[i], out MeatovItemData itemData) ? itemData.name : "null");
                            return false;
                        }
                    }
                }
            }
            if (itemBlacklist != null)
            {
                for (int i = 0; i < itemBlacklist.Count; ++i)
                {
                    if (Mod.itemsByParents.TryGetValue(itemBlacklist[i], out List<MeatovItemData> itemDatas))
                    {
                        for (int j = 0; j < itemDatas.Count; ++j)
                        {
                            int inventoryCount = 0;
                            if (Mod.playerInventory.TryGetValue(itemDatas[j].tarkovID, out inventoryCount) && inventoryCount > 0)
                            {
                                cardRequirementText.gameObject.SetActive(true);
                                string categoryName = null;
                                if (Mod.localeDB[itemBlacklist[i] + " Name"] != null)
                                {
                                    categoryName = Mod.localeDB[itemBlacklist[i] + " Name"].ToString();
                                }
                                else
                                {
                                    categoryName = Mod.GetCorrectCategoryName(Mod.itemDB[itemBlacklist[i]]["_name"].ToString());
                                }
                                cardRequirementText.text = "Cannot have " + categoryName;
                                return false;
                            }
                        }
                    }
                    else
                    {
                        int inventoryCount = 0;
                        if (Mod.playerInventory.TryGetValue(itemBlacklist[i], out inventoryCount) && inventoryCount > 0)
                        {
                            cardRequirementText.gameObject.SetActive(true);
                            cardRequirementText.text = "Cannot have " + (Mod.defaultItemData.TryGetValue(itemBlacklist[i], out MeatovItemData itemData) ? itemData.name : "null");
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        public void OnTriggerEnter(Collider other)
        {
            if (timeActive && active && playerHead == null)
            {
                FVRPlayerHitbox playerHitbox = other.GetComponent<FVRPlayerHitbox>();
                if (playerHitbox != null && playerHitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
                {
                    if (UpdateRequirementFulfillment())
                    {
                        playerHead = playerHitbox;

                        extractionTimer = extractionTime;
                    }
                }
            }
        }

        public void OnTriggerExit(Collider other)
        {
            if (playerHead != null)
            {
                FVRPlayerHitbox playerHitbox = other.GetComponent<FVRPlayerHitbox>();
                if (playerHitbox != null && playerHitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
                {
                    playerHead = null;

                    extractionTimer = -1;
                }
            }
        }

        public void Extract()
        {
            if (UpdateRequirementFulfillment())
            {
                // Consume requirements
                for(int i=0; i<itemRequirements.Count;++i)
                {
                    if (Mod.playerInventoryItems.TryGetValue(itemRequirements[i], out List<MeatovItem> potentialItems))
                    {
                        int countLeft = itemRequirementCounts[i];
                        while (countLeft > 0)
                        {
                            MeatovItem item = potentialItems[potentialItems.Count - 1];
                            if (item.stack > countLeft)
                            {
                                item.stack -= countLeft;
                                countLeft = 0;
                                break;
                            }
                            else
                            {
                                countLeft -= item.stack;
                                if (item.DetachChildren())
                                {
                                    item.Destroy();
                                }
                            }
                        }
                    }
                }

                // Runthrough if made less than 250xp or stayed in raid less than 5 minutes
                if (Mod.charChoicePMC && (Mod.raidExp < 250 || Mod.raidTime < 300))
                {
                    RaidManager.instance.EndRaid(RaidManager.RaidStatus.RunThrough, extractionName);
                }
                else
                {
                    RaidManager.instance.EndRaid(RaidManager.RaidStatus.Success, extractionName);
                }
            }
        }
    }
}
