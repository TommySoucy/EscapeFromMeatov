using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class EFM_Switch : MonoBehaviour
    {
        public int mode; // 0: Toggle gameObjects

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
                default:
                    break;
            }
        }
    }
}
