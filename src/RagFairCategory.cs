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
            count.text = "("+ category.barters.Count+")";

            layoutGroup.padding.left = 10 * step;
            mainCollider.size = new Vector3(mainCollider.size.x - 10 * step, mainCollider.size.y, mainCollider.size.z);
        }

        public void OnToggleClicked()
        {
            subList.SetActive(!subList.activeSelf);
            openArrow.SetActive(!subList.activeSelf);
            closeArrow.SetActive(subList.activeSelf);

            HideoutController.instance.marketManager.ragFairCategoriesHoverScrollProcessor.mustUpdateMiddleHeight = 1;
        }

        public void OnClicked()
        {
            HideoutController.instance.marketManager.SetRagFairBuyCategory(category);
        }
    }
}
