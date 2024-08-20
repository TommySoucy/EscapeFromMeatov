using FistVR;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace EFM
{
    public class Switch : FVRInteractiveObject
    {
        public AreaController areaController;
        [NonSerialized]
        public bool init;
        public enum Mode
        {
            GameObjects,
            Power
        }
        public Mode mode;
        public AudioSource audioSource;
        public bool active;
        public List<GameObject> gameObjects;
        public List<GameObject> negativeGameObjects;

        public override void Start()
        {
            base.Start();

            SimpleInteraction(null);

            init = true;
        }

        public override void SimpleInteraction(FVRViveHand hand)
        {
            if(init && audioSource != null)
            {
                audioSource.Play();
            }

            switch (mode)
            {
                case Mode.GameObjects:
                    if (init)
                    {
                        active = !active;
                    }
                    foreach (GameObject go in gameObjects)
                    {
                        if(go != null)
                        {
                            go.SetActive(active);
                        }
                    }
                    foreach (GameObject go in negativeGameObjects)
                    {
                        if (go != null)
                        {
                            go.SetActive(!active);
                        }
                    }
                    break;
                case Mode.Power:
                    areaController.TogglePower();
                    break;
                default:
                    break;
            }
        }
    }
}
