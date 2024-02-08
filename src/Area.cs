using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class Area : MonoBehaviour
    {
        // Main
        public AreaController controller;
        public int startLevel;
        [NonSerialized]
        public int currentLevel;

        // Power
        public bool requiresPower;
        [NonSerialized]
        public bool previousPowered;
        [NonSerialized]
        public bool powered;
        public MainAudioSources[] mainAudioSources;
        public MainAudioClips[] mainAudioClips;
        public Vector2s[] workingRanges;
        [NonSerialized]
        public AudioClip[][][] subClips;
        [NonSerialized]
        public bool poweringOn;

        // Objects
        public AreaUI UI;
        public GameObject[] levels;
        public GameObject[] objectsToToggle;
        public GameObjects[] objectsToTogglePerLevel;
        public AreaUpgradeCheckProcessorPair[] upgradeCheckProcessors;
        [NonSerialized]
        public AreaUpgradeCheckProcessor[] activeCheckProcessors;
        public AreaSlots[] areaSlotsPerLevel;
        public AreaVolumes[] areaVolumesPerLevel;
        public bool craftOuputSlot; // False is Volume, output will always be first in slot/vol per level

        public void Start()
        {
            UpdateObjectsPerLevel();

            if (mainAudioClips != null)
            {
                subClips = new AudioClip[levels.Length][][];
                for(int i=0; i< levels.Length; ++i)
                {
                    if(mainAudioClips[i] != null && mainAudioClips[i].Length > 0)
                    {
                        subClips[i] = new AudioClip[mainAudioClips[i].Length][];
                        for (int j=0; j< mainAudioClips[i].Length; ++j)
                        {
                            subClips[i][j] = new AudioClip[3];
                            subClips[i][j][0] = MakeSubclip(mainAudioClips[i][j], 0, workingRanges[i][j].x);
                            subClips[i][j][1] = MakeSubclip(mainAudioClips[i][j], workingRanges[i][j].x, workingRanges[i][j].y);
                            subClips[i][j][2] = MakeSubclip(mainAudioClips[i][j], workingRanges[i][j].y, mainAudioClips[i][j].length);
                        }
                    }
                }
            }

            if(objectsToToggle == null)
            {
                objectsToToggle = new GameObject[0];
            }
        }

        public void Update()
        {
            if (requiresPower)
            {
                if (powered && !previousPowered)
                {
                    // Manage audio
                    poweringOn = true;
                    for(int i=0;i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].PlayOneShot(subClips[i][j][0]);
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(true);
                    }
                }
                else if (!powered && previousPowered)
                {
                    // Manage audio
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].Stop();
                            mainAudioSources[i][j].PlayOneShot(subClips[i][j][2]);
                        }
                    }

                    // Manage objects
                    for (int i = 0; i < objectsToToggle.Length; ++i)
                    {
                        objectsToToggle[i].SetActive(false);
                    }
                }

                if (poweringOn && !mainAudioSources[currentLevel][0].isPlaying)
                {
                    poweringOn = false;
                    for (int i = 0; i < levels.Length; ++i)
                    {
                        for (int j = 0; j < mainAudioSources[i].Length; ++j)
                        {
                            mainAudioSources[i][j].loop = true;
                            mainAudioSources[i][j].clip = subClips[i][j][1];
                            mainAudioSources[i][j].Play();
                        }
                    }
                }

                // Finally update previousPowered
                previousPowered = powered;
            }
        }

        public void UpdateObjectsPerLevel()
        {
            if (objectsToTogglePerLevel != null)
            {
                for (int i = 0; i < objectsToTogglePerLevel.Length; ++i)
                {
                    // Only enable object for current level
                    for (int j = 0; j < objectsToTogglePerLevel[i].Length; ++j)
                    {
                        objectsToTogglePerLevel[i][j].SetActive(i == currentLevel);
                    }
                }
            }
        }

        public static AudioClip MakeSubclip(AudioClip clip, float start, float stop)
        {
            int frequency = clip.frequency;
            float timeLength = stop - start;
            int samplesLength = (int)(frequency * timeLength);
            AudioClip newClip = AudioClip.Create(clip.name + "-sub", samplesLength, 1, frequency, false);

            float[] data = new float[samplesLength];
            clip.GetData(data, (int)(frequency * start));
            newClip.SetData(data, 0);

            return newClip;
        }
    }

    [Serializable]
    public class MainAudioSources
    {
        public AudioSource[] mainAudioSources; 
        
        public AudioSource this[int i]
        {
            get { return mainAudioSources[i]; }
            set { mainAudioSources[i] = value; }
        }

        public int Length
        {
            get { return mainAudioSources.Length; }
        }
    }

    [Serializable]
    public class MainAudioClips
    {
        public AudioClip[] mainAudioClips; 
        
        public AudioClip this[int i]
        {
            get { return mainAudioClips[i]; }
            set { mainAudioClips[i] = value; }
        }

        public int Length
        {
            get { return mainAudioClips.Length; }
        }
    }

    [Serializable]
    public class Vector2s
    {
        public Vector2[] workingRanges; 
        
        public Vector2 this[int i]
        {
            get { return workingRanges[i]; }
            set { workingRanges[i] = value; }
        }

        public int Length
        {
            get { return workingRanges.Length; }
        }
    }

    [Serializable]
    public class GameObjects
    {
        public GameObject[] objectsToTogglePerLevel; 
        
        public GameObject this[int i]
        {
            get { return objectsToTogglePerLevel[i]; }
            set { objectsToTogglePerLevel[i] = value; }
        }

        public int Length
        {
            get { return objectsToTogglePerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaSlots
    {
        public AreaSlot[] areaSlotsPerLevel; 
        
        public AreaSlot this[int i]
        {
            get { return areaSlotsPerLevel[i]; }
            set { areaSlotsPerLevel[i] = value; }
        }

        public int Length
        {
            get { return areaSlotsPerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaVolumes
    {
        public AreaVolume[] areaVolumesPerLevel; 
        
        public AreaVolume this[int i]
        {
            get { return areaVolumesPerLevel[i]; }
            set { areaVolumesPerLevel[i] = value; }
        }

        public int Length
        {
            get { return areaVolumesPerLevel.Length; }
        }
    }

    [Serializable]
    public class AreaUpgradeCheckProcessorPair
    {
        public AreaUpgradeCheckProcessor[] areaUpgradeCheckProcessors; 
        
        public AreaUpgradeCheckProcessor this[int i]
        {
            get { return areaUpgradeCheckProcessors[i]; }
            set { areaUpgradeCheckProcessors[i] = value; }
        }
    }
}
