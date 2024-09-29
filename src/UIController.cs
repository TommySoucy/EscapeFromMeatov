using FistVR;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public abstract class UIController : MonoBehaviour
    {
        [NonSerialized]
        public bool init;

        public virtual void Awake()
        {
            Mod.FetchAvailableSaveFiles();

            InitUI();

            init = true;
        }

        public abstract void InitUI();
    }
}
