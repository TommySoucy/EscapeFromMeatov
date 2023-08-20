using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class AreaRequirement : MonoBehaviour
    {
        public enum RequirementType
        {
            Item,
            Area,
            Skill,
            Trader
        }
        public RequirementType requirementType;
        public string itemID;
        public int count;
        public int index; // Area, trader, or skill
        public int level; // Area, trader, or skill
    }
}
