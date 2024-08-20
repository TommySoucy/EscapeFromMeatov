using System;
using UnityEngine;

namespace EFM
{
    public class AmbientLightChanger : MonoBehaviour
    {
        public Color color;
        [NonSerialized]
        public Color previousColor = new Color(0.1176471f, 0.1176471f, 0.1176471f);

        public void OnEnable()
        {
            previousColor = RenderSettings.ambientLight;
            RenderSettings.ambientLight = color;
        }

        public void OnDisable()
        {
            RenderSettings.ambientLight = previousColor;
        }
    }
}
