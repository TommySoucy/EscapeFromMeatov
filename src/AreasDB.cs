using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class AreasDB
    {
        public List<AreaDefault> areaDefaults { get; set; }
    }

    public class Requirement
    {
        public int areaType { get; set; }
        public int requiredLevel { get; set; }
        public string type { get; set; }
        public string templateId { get; set; }
        public int? count { get; set; }
        public bool? isFunctional { get; set; }
        public string traderId { get; set; }
        public int? loyaltyLevel { get; set; }
        public string skillName { get; set; }
        public int? skillIndex { get; set; }
        public int? skillLevel { get; set; }
    }

    public class Bonus
    {
        public int value { get; set; }
        public bool passive { get; set; }
        public bool production { get; set; }
        public bool visible { get; set; }
        public List<string> filter { get; set; }
        public string icon { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public string skillType { get; set; }
        public string templateId { get; set; }
    }

    public class Stage
    {
        public List<Requirement> requirements { get; set; }
        public List<Bonus> bonuses { get; set; }
        public int slots { get; set; }
        public int constructionTime { get; set; }
        public string description { get; set; }
    }

    public class AreaDefault
    {
        public string ID { get; set; }
        public int type { get; set; }
        public bool enabled { get; set; }
        public bool needsFuel { get; set; }
        public bool takeFromSlotLocked { get; set; }
        public bool craftGivesExp { get; set; }
        public List<Stage> stages { get; set; }
    }
}
