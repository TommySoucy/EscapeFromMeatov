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
            category.UI = this;

            categoryName.text = category.name;

            layoutGroup.padding.left = 10 * step;
            mainCollider.size = new Vector3(mainCollider.size.x - 10 * step, mainCollider.size.y, mainCollider.size.z);

            int actualBarterCount = 0;
            for (int i = 0; i < category.barters.Count; ++i)
            {
                if (category.barters[i].trader == null
                    || (category.barters[i].level <= category.barters[i].trader.level
                        && (!category.barters[i].trader.rewardBarters.TryGetValue(category.barters[i].itemData[0].tarkovID, out bool unlocked)
                            || unlocked)))
                {
                    ++actualBarterCount;
                }
            }
            count.text = "(" + actualBarterCount + ")";
        }

        public void OnToggleClicked()
        {
            subList.SetActive(!subList.activeSelf);
            openArrow.SetActive(!subList.activeSelf);
            closeArrow.SetActive(subList.activeSelf);

            HideoutController.instance.marketManager.ragFairCategoriesHoverScrollProcessor.mustUpdateMiddleHeight = 1;

            category.uncollapsed = subList.activeSelf;
        }

        public void OnClicked()
        {
            HideoutController.instance.marketManager.SetRagFairBuyCategory(category);
        }
    }
}
