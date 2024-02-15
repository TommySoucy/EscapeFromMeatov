using System;
using System.Collections.Generic;

namespace EFM
{
    public class AreaVolume : ContainmentVolume
    {
        public Area area;
        public AreaVolume next;
        [NonSerialized]
        public List<MeatovItem> items = new List<MeatovItem>();
    }
}
