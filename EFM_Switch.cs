using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_Switch : MonoBehaviour
    {
        public int mode; // 0: Toggle gameObjects, 1: Power

        // 0
        public List<GameObject> gameObjects;

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
                default:
                    break;
            }
        }
    }
}
