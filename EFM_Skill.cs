using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class EFM_Skill
    {
        public float progress; // Actual
        public float currentProgress; // Affected by effects, this is the one we should check while playing
    }
}
