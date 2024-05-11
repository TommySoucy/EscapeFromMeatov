using FistVR;
using UnityEngine;

namespace EFM
{
    public class TraderTab : FVRPointableButton
    {
		public GameObject hover;
		public AudioSource hoverSound;
		public AudioSource clickSound;
		public TraderTab[] tabs;
		public GameObject page;

		public bool active;

		public override void BeginHoverDisplay()
		{
			base.BeginHoverDisplay();

			hover.SetActive(true);

			hoverSound.Play();
		}

		public override void EndHoverDisplay()
		{
			base.EndHoverDisplay();

			hover.SetActive(false);
		}

		public void OnClick(int index)
        {
            TODO: // Set in asset
			if (!active)
			{
                TODO0: // Set selected in asset instead of using getchild
				transform.GetChild(1).gameObject.SetActive(true);
				page.SetActive(true);

				// Reorder the tabs and deactivate any other active ones
				for(int i = 0; i < tabs.Length; ++i)
                {
					if(i != index)
                    {
						tabs[i].transform.SetSiblingIndex(i);
                        if (tabs[i].active)
                        {
							tabs[i].active = false;
							tabs[i].transform.GetChild(1).gameObject.SetActive(false);
							tabs[i].page.SetActive(false);
                        }
                    }
                }

				// Set this tab as last sibling so it appears on top
				transform.SetAsLastSibling();

				active = true;
				clickSound.Play();
			}
        }
	}
}
