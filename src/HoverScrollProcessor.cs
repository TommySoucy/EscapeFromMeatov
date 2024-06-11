using System;
using UnityEngine;

namespace EFM
{
    public class HoverScrollProcessor : MonoBehaviour
    {
        public RectTransform controlledTransform;
        public float maxHeight;
        public float targetRate = 1; // Max height per second
        public HoverScroll downHoverScroll; 
        public HoverScroll upHoverScroll;
        [NonSerialized]
        public int mustUpdateMiddleHeight = 0;
        [NonSerialized]
        public int mustUpdateHoverScrolls = 0;
        [NonSerialized]
        public bool setToTop;

        public void Update()
        {
            // We use this system to delay the update to a frame later
            // This is because the sizeDelta.y of a rectTransform will be updated a frame after the height change
            if (mustUpdateMiddleHeight == 0)
            {
                UpdateMiddleHeight();
                --mustUpdateMiddleHeight;
            }
            else if (mustUpdateMiddleHeight > 0)
            {
                --mustUpdateMiddleHeight;
            }

            // The height of the transform gets set a frame after content changes
            // The value of the scroll bar changes a frame after that
            if (mustUpdateHoverScrolls == 0)
            {
                UpdateHoverScrolls();
                --mustUpdateHoverScrolls;
            }
            else if (mustUpdateHoverScrolls > 0)
            {
                --mustUpdateHoverScrolls;
            }
        }

        public void OnEnable()
        {
            setToTop = true;
            mustUpdateMiddleHeight = 1;
        }

        public void UpdateMiddleHeight()
        {
            // Rate calculation:
            /*
                We see 2, total size is 10.
                We want to scroll 2 per second.
                (10-2)/2=4, 1/4=0.25.
                We want to scroll 0.25 of scroll bar value per second.
                It will take 4 seconds to scroll down entire view.
                Taking the difference between maxHeight (What we see) and total height (instead of just using total height)
                is necessary because our view already spans maxHeight (What we see), we won't have to 
                scroll through that, we will only have to scroll through the difference, so that is what we want to calculate our rate
                from.
             */
            if (controlledTransform.sizeDelta.y > maxHeight)
            {
                downHoverScroll.rate = 1 / ((controlledTransform.sizeDelta.y - maxHeight) / (targetRate * maxHeight));
                upHoverScroll.rate = downHoverScroll.rate;
                if (setToTop)
                {
                    setToTop = false;
                    downHoverScroll.scrollbar.value = 1; // Put it back to the top
                    downHoverScroll.gameObject.SetActive(true);
                    upHoverScroll.gameObject.SetActive(false);
                }
                else
                {
                    mustUpdateHoverScrolls = 1;
                }
            }
            else
            {
                downHoverScroll.gameObject.SetActive(false);
                upHoverScroll.gameObject.SetActive(false);
            }
        }

        public void UpdateHoverScrolls()
        {
            downHoverScroll.gameObject.SetActive(downHoverScroll.scrollbar.value > 0);
            upHoverScroll.gameObject.SetActive(upHoverScroll.scrollbar.value < 1);
        }
    }
}
