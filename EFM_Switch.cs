using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_Switch : MonoBehaviour
    {
        public int mode; // 0: Toggle gameObjects, 1: Power, 2: UI, 3: Market toggle

        // 0
        public List<GameObject> gameObjects;

        // 2
        private bool active = true;

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
                    EFM_BaseAreaManager.ToggleGenerator();
                    break;
                case 2:
                    for(int i=0; i < gameObjects.Count; ++i)
                    {
                        if(i < gameObjects.Count - 1)
                        {
                            gameObjects[i].SetActive(active && !EFM_Base_Manager.marketUI);
                        }
                        else
                        {
                            gameObjects[i].SetActive(active && EFM_Base_Manager.marketUI);
                        }
                    }
                    active = !active;
                    break;
                case 3:
                    bool currentlyActive = gameObjects[0].activeSelf || gameObjects[gameObjects.Count - 1].activeSelf;
                    EFM_Base_Manager.marketUI = !EFM_Base_Manager.marketUI;
                    for (int i=0; i < gameObjects.Count; ++i)
                    {
                        if(i < gameObjects.Count - 1)
                        {
                            gameObjects[i].SetActive(currentlyActive && !EFM_Base_Manager.marketUI);
                        }
                        else
                        {
                            gameObjects[i].SetActive(currentlyActive && EFM_Base_Manager.marketUI);
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
