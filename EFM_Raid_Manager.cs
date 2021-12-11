using FistVR;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace EFM
{
    class EFM_Raid_Manager : EFM_Manager
    {
        public static readonly float extractionTime = 10;

        public static EFM_Raid_Manager currentManager;
        public static float extractionTimer;
        public static bool inRaid;

        public EFM_Base_Manager baseManager;

        private Mapdata mapData;
        private List<Extraction> possibleExtractions;
        public ExtractionManager currentExtraction;
        private bool inExtractionLastFrame;
        public Transform spawnPoint;

        private void Update()
        {
            if(currentExtraction != null && currentExtraction.active)
            {
                inExtractionLastFrame = true;

                extractionTimer += UnityEngine.Time.deltaTime;

                // TODO: update extraction timer display (also enable it if necessary)

                if(extractionTimer >= extractionTime)
                {
                    GM.CurrentMovementManager.TeleportToPoint(Vector3.zero, true, Quaternion.identity.eulerAngles);
                    inRaid = false;
                    baseManager.FinishRaid(EFM_Base_Manager.FinishRaidState.Survived);// TODO: Will have to call with runthrough if exp is less than threshold

                    Destroy(gameObject);
                }
            }
            else if (inExtractionLastFrame)
            {
                extractionTimer = 0;
                inExtractionLastFrame = false;

                // TODO: update extraction timer display (disable it)
            }
        }

        public override void Init()
        {
            Mod.instance.LogInfo("Raid init called");
            currentManager = this;

            LoadMapData();
            Mod.instance.LogInfo("Map data read");

            // Choose spawnpoints
            Transform spawnRoot = transform.GetChild(transform.childCount - (baseManager.chosenCharIndex == 0 ? 1 : 3));
            spawnPoint = spawnRoot.GetChild(Random.Range(0, spawnRoot.childCount));

            Mod.instance.LogInfo("Got spawn");

            // Find extractions
            possibleExtractions = new List<Extraction>();
            Extraction bestCandidate = null; // Must be an extraction without appearance times (always available) and no other requirements. This will be the minimum extraction
            float farthestDistance = float.MinValue;
            Mod.instance.LogInfo("Init raid with map index: "+baseManager.chosenMapIndex+", which has "+mapData.maps[baseManager.chosenMapIndex].extractions.Count+" extractions");
            for(int extractionIndex = 0; extractionIndex < mapData.maps[baseManager.chosenMapIndex].extractions.Count; ++extractionIndex)
            {
                Extraction currentExtraction = mapData.maps[baseManager.chosenMapIndex].extractions[extractionIndex];
                currentExtraction.gameObject = gameObject.transform.GetChild(gameObject.transform.childCount - 2).GetChild(extractionIndex).gameObject;

                bool canBeMinimum = (currentExtraction.times == null || currentExtraction.times.Count == 0) &&
                                    (currentExtraction.itemRequirements == null || currentExtraction.itemRequirements.Count == 0);
                float currentDistance = Vector3.Distance(spawnPoint.position, currentExtraction.gameObject.transform.position);
                Mod.instance.LogInfo("\tExtraction at index: "+extractionIndex+" has "+ currentExtraction.times.Count + " times and "+ currentExtraction.itemRequirements.Count+" items reqs. Its current distance from player is: "+ currentDistance);
                if (UnityEngine.Random.value <= ExtractionChanceByDist(currentDistance))
                {
                    Mod.instance.LogInfo("\t\tAdding this extraction to list possible extractions");
                    possibleExtractions.Add(currentExtraction);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in currentExtraction.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = currentExtraction;
                    }
                }
                   
                // Best candidate will be farthest because if there is at least one extraction point, we don't want to always have the nearest
                if(canBeMinimum && currentDistance > farthestDistance)
                {
                    bestCandidate = currentExtraction;
                }
            }
            if(bestCandidate != null)
            {
                if(!possibleExtractions.Contains(bestCandidate))
                {
                    Mod.instance.LogInfo("\t\tAdding candidate to list possible extractions");
                    possibleExtractions.Add(bestCandidate);

                    //Add an extraction manager to each of the extraction's volumes
                    foreach (Transform volume in bestCandidate.gameObject.transform)
                    {
                        ExtractionManager extractionManager = volume.gameObject.AddComponent<ExtractionManager>();
                        extractionManager.extraction = bestCandidate;
                    }
                }
            }
            else
            {
                Mod.instance.LogError("No minimum extraction found");
            }

            inRaid = true;

            init = true;
        }

        public override void InitUI()
        {
        }

        private void LoadMapData()
        {
            // Load and Deserialize map data
            mapData = JsonConvert.DeserializeObject<Mapdata>(File.ReadAllText("BepInEx/Plugins/EscapeFromMeatov/EscapeFromMeatovMapData.txt"));
        }

        private float ExtractionChanceByDist(float distance)
        {
            if(distance < 150)
            {
                return 0.001f * distance; // 15% chance at 150 meters
            }
            else if(distance < 500)
            {
                return 0.0008f * distance + 0.15f; // 55% chance at 500 meters
            }
            else // > 500
            {
                return 0.00045f * distance + 0.55f; // 100% at 1000 meters
            }
        }
    }

    class ExtractionManager : MonoBehaviour
    {
        public Extraction extraction;

        private List<Collider> playerColliders;
        public bool active;

        private void Start()
        {
            Mod.instance.LogInfo("Extraction manager created for extraction: "+extraction.gameObject.name);
            playerColliders = new List<Collider>();
        }

        private void Update()
        {
            active = false;
            if(extraction.times != null && extraction.times.Count > 0)
            {
                foreach(TimeInterval timeInterval in extraction.times)
                {
                    if(EFM_Base_Manager.time >= timeInterval.start && EFM_Base_Manager.time <= timeInterval.end)
                    {
                        active = true;
                        break;
                    }
                }
            }
            else
            {
                active = true;
            }
        }

        private void OnTriggerEnter(Collider collider)
        {
            Mod.instance.LogInfo("Trigger enter called on "+gameObject.name+": "+collider.name);
            if (EFM_Raid_Manager.currentManager.currentExtraction == null)
            {
                // TODO: Check if player, if it is, set EFM_Raid_Manager.currentManager.currentExtraction = this if we dont already have a player collider and add this player collider to the list
                if (collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Controller (left)") ||
                   collider.gameObject.name.Equals("Hitbox_Neck") ||
                   collider.gameObject.name.Equals("Hitbox_Head") ||
                   collider.gameObject.name.Equals("Hitbox_Torso"))
                {
                    Mod.instance.LogInfo("Collider is player");
                    if (playerColliders.Count == 0)
                    {
                        EFM_Raid_Manager.currentManager.currentExtraction = this;
                    }

                    playerColliders.Add(collider);
                }
            }
        }

        private void OnTriggerExit(Collider collider)
        {
            if (playerColliders.Count > 0)
            {
                playerColliders.Remove(collider);

                if (playerColliders.Count == 0 && EFM_Raid_Manager.currentManager.currentExtraction == this)
                {
                    EFM_Raid_Manager.currentManager.currentExtraction = null;
                }
            }
        }
    }

    // Deserialized map data classes
    public class TimeInterval
    {
        public int start { get; set; }
        public int end { get; set; }
    }

    public class Extraction
    {
        public List<TimeInterval> times { get; set; }
        public List<int> itemRequirements { get; set; }

        public GameObject gameObject;
    }

    public class OtherDetail
    {
    }

    public class Map
    {
        public List<Extraction> extractions { get; set; }
        public List<OtherDetail> otherDetails { get; set; }
    }

    public class Mapdata
    {
        public List<Map> maps { get; set; }
    }
}
