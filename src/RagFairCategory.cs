using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairCategory : MonoBehaviour
    {
        [NonSerialized]
        public string category;

        public GameObject openArrow;
        public GameObject closeArrow;
        public GameObject subList;
        public Text categoryName;
        public Text count;
        public VerticalLayoutGroup layoutGroup;
        public BoxCollider mainCollider;
        public GameObject toggle;

        public void OnToggleClicked()
        {
            subList.SetActive(!subList.activeSelf);
            openArrow.SetActive(!subList.activeSelf);
            closeArrow.SetActive(subList.activeSelf);
        }

        public void OnClicked()
        {
            TODO: // List all items
            Mod.LogInfo("");
        }
    }
}
