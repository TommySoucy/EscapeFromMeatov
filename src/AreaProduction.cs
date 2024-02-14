using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace EFM
{
    public class AreaProduction : MonoBehaviour
    {
        public BaseAreaManager area;

        public string ID;
        public List<AreaProductionRequirement> requirements;
        public float productionTime;
        public string endProduct;
        public bool continuous; // Bitcoin farm for example, continuously farms bitcoins as long as requirements are fulfilled
        public int count; // How many the production will produce, if continuous should be set to 1
        public int productionLimitCount; // Used by continuous productions to say what is the maximum amount of products that can be produced before they need to be harvested

        // Active fields
        public bool active;
        public float timeLeft;
        public int productionCount; // How many are currently retrievable
        public int installedCount;
    }

    public class AreaProductionRequirement
    {
        public bool resource;

        public string[] IDs;
        public int count; // A "Resource" type requirement will have a "resource" property instead of count
        public bool isFunctional; // This means the item could have contents inside? or is composed of multiple items? firearms for example
    }

    public class Vector2Int
    {
        public Vector2Int(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
        public int x;
        public int y;
    }

    public class EFM_ScavCaseProduction
    {
        public float timeLeft;
        public Dictionary<Mod.ItemRarity, Vector2Int> products;
    }
}
