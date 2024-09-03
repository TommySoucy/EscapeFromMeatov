using FistVR;
using H3MP.Scripts;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Extraction : MonoBehaviour
    {
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

        public string name;
        public float extractionTime = 10;
        public List<Vector2> activeTimes; // Time ranges in seconds from 0 to 86400 (24h) during which this extraction can actually be used
        public List<string> itemRequirements; // Items consumed upon using the extraction
        public List<string> itemRequirementCounts; // The amount of each item consumed upon using the extraction
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

        public void OnTriggerEnter(Collider other)
        {
            if(playerHead == null)
            {
                FVRPlayerHitbox playerHitbox = other.GetComponent<FVRPlayerHitbox>();
                if (playerHitbox != null && playerHitbox.Type == FVRPlayerHitbox.PlayerHitBoxType.Head)
                {
                    playerHead = playerHitbox;

                    extractionTimer = extractionTime;
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
            Mod.usedExtraction = 
            TODO: // Implement raid ending, conditional success in this case
        }
    }
}
