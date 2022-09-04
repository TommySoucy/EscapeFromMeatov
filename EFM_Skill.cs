using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class EFM_Skill
    {
        public enum SkillType
        {
            // Used in SkillGroupLevelingBoost type bonuses
            Special,
            Physical,
            Practical,

            NotSpecified
        }
        public SkillType skillType;

        public float progress; // Actual, 1 lvl ea. 100
        public float currentProgress; // Affected by effects, this is the one we should check while playing
    }
}
