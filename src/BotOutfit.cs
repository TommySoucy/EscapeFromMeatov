using FistVR;
using System.Collections.Generic;

namespace EFM
{
    public class BotOutfit
    {
        // Head, link 0
        public List<FVRObject> earpiece;
        public List<FVRObject> headwear;
        public List<FVRObject> facewear;

        // Torso, link 1
        public List<FVRObject> torso;
        public List<FVRObject> backpack;

        // Pants, link 2
        public Dictionary<FVRObject, bool> abdo; // False value means no leg

        // Pants_Lower, link 3
        public List<FVRObject> leg;
    }
}
