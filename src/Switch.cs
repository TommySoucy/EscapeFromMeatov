using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class Switch : MonoBehaviour
    {
        public int mode; // 0: Toggle gameObjects, 1: Power, 2: UI, 3: Market toggle, 4: Lights
        public AudioSource audioSource;

        // 0
        public List<GameObject> gameObjects;

        // 2
        private bool active = true;

        // 4
        public AudioSource level2AudioSource;
        public AudioSource level3AudioSource;

        public void Activate()
        {
            switch (mode)
            {
                case 0:
                    foreach(GameObject go in gameObjects)
                    {
                        go.SetActive(!go.activeSelf);
                    }
                    break;
                case 1:
                    BaseAreaManager.ToggleGenerator();
                    break;
                case 2:
                    active = !active;
                    for (int i=0; i < gameObjects.Count; ++i)
                    {
                        if(i < gameObjects.Count - 1)
                        {
                            gameObjects[i].SetActive(active && !Base_Manager.marketUI);
                        }
                        else
                        {
                            gameObjects[i].SetActive(active && Base_Manager.marketUI);
                        }
                    }
                    break;
                case 3:
                    bool currentlyActive = gameObjects[0].activeSelf || gameObjects[gameObjects.Count - 1].activeSelf;
                    Base_Manager.marketUI = !Base_Manager.marketUI;
                    for (int i=0; i < gameObjects.Count; ++i)
                    {
                        if(i < gameObjects.Count - 1)
                        {
                            gameObjects[i].SetActive(currentlyActive && !Base_Manager.marketUI);
                        }
                        else
                        {
                            gameObjects[i].SetActive(currentlyActive && Base_Manager.marketUI);
                        }
                    }
                    break;
                case 4:
                    foreach (GameObject go in gameObjects)
                    {
                        go.SetActive(!go.activeSelf);
                    }
                    if(Mod.currentBaseManager.baseAreaManagers[15].level == 2)
                    {
                        level2AudioSource.Play();
                    }
                    else if(Mod.currentBaseManager.baseAreaManagers[15].level == 3)
                    {
                        level3AudioSource.Play();
                    }
                    break;
                default:
                    break;
            }

            if(audioSource != null)
            {
                audioSource.Play();
            }
        }
    }
}
