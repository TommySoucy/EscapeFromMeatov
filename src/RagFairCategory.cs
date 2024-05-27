using System;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class RagFairCategory : MonoBehaviour
    {
        [NonSerialized]
        public CategoryTreeNode category;

        public GameObject openArrow;
        public GameObject closeArrow;
        public GameObject subList;
        public Text categoryName;
        public Text count;
        public VerticalLayoutGroup layoutGroup;
        public BoxCollider mainCollider;
        public GameObject toggle;

        public void SetCategory(CategoryTreeNode category, int step)
        {
            this.category = category;

            categoryName.text = category.name;
            count.text = "("+Mod.itemsByParents[category.ID].Count+")";

            cont from ehre // apply step
        }

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
