using FistVR;
using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class HoverScroll : FVRPointableButton
    {
        public AudioSource hoverSound;
        public Scrollbar scrollbar;
        public HoverScroll other;

        public bool up;
        public float rate; // fraction of entire scrollview height/s
		private bool scrolling;

        public void Start()
        {
            if(Button != null)
            {
                Button.onClick.AddListener(OnClick);
            }
        }

        public void OnClick()
        {
            if (up)
            {
                scrollbar.value = 1;
                scrolling = false;
                gameObject.SetActive(false);
            }
            else
            {
                scrollbar.value = 0;
                scrolling = false;
                gameObject.SetActive(false);
            }
        }

		public override void Update()
        {
            base.Update();

            if (scrolling)
            {
                other.gameObject.SetActive(true);

                if (up)
                {
                    scrollbar.value += rate * Time.deltaTime;

                    if (scrollbar.value >= 1)
                    {
                        scrollbar.value = 1;
                        scrolling = false;
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    scrollbar.value -= rate * Time.deltaTime;

                    if (scrollbar.value <= 0)
                    {
                        scrollbar.value = 0;
                        scrolling = false;
                        gameObject.SetActive(false);
                    }
                }
            }
        }

		public override void BeginHoverDisplay()
		{
			base.BeginHoverDisplay();

            scrolling = true;

			hoverSound.Play();
		}

		public override void EndHoverDisplay()
		{
			base.EndHoverDisplay();

            scrolling = false;
		}
	}
}
