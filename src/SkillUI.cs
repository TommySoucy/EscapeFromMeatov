using UnityEngine;
using UnityEngine.UI;

namespace EFM
{
    public class SkillUI : MonoBehaviour
    {
        public Skill skill;

        public Sprite[] sprites;
        public Image icon;
        public Text text;
        public RectTransform barFill;
        public GameObject diminishingReturns;
        public GameObject increasing;

        public void SetSkill(Skill skill)
        {
            if(this.skill != null)
            {
                this.skill.OnSkillLevelChanged -= OnSkillLevelChanged;
                this.skill.OnProgressChanged -= OnProgressChanged;
                this.skill.OnDiminishingReturnsChanged -= OnDiminishingReturnsChanged;
                this.skill.OnIncreasingChanged -= OnIncreasingChanged;
            }

            this.skill = skill;

            skill.OnSkillLevelChanged += OnSkillLevelChanged;
            skill.OnProgressChanged += OnProgressChanged;
            skill.OnDiminishingReturnsChanged += OnDiminishingReturnsChanged;
            skill.OnIncreasingChanged += OnIncreasingChanged;

            icon.sprite = sprites[skill.index];
            text.text = skill.displayName.ToString()+" lvl "+skill.GetLevel();
            barFill.sizeDelta = new Vector2(skill.currentProgress % 100, 4.73f);
            diminishingReturns.SetActive(skill.dimishingReturns);
            increasing.SetActive(skill.increasing);
        }

        public void OnSkillLevelChanged()
        {
            text.text = skill.displayName.ToString() + " lvl " + skill.GetLevel();
        }

        public void OnProgressChanged()
        {
            barFill.sizeDelta = new Vector2(skill.currentProgress % 100, 4.73f);
        }

        public void OnDiminishingReturnsChanged()
        {
            diminishingReturns.SetActive(skill.dimishingReturns);
        }

        public void OnIncreasingChanged()
        {
            increasing.SetActive(skill.increasing);
        }

        public void OnDestroy()
        {
            if (skill != null)
            {
                skill.OnSkillLevelChanged -= OnSkillLevelChanged;
                skill.OnProgressChanged -= OnProgressChanged;
                skill.OnDiminishingReturnsChanged -= OnDiminishingReturnsChanged;
                skill.OnIncreasingChanged -= OnIncreasingChanged;
            }
        }
    }
}
