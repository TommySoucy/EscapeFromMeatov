using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Valve.Newtonsoft.Json.Linq;

namespace EFM
{
    public class EFM_TraderStatus
    {
        public EFM_Base_Manager baseManager;

        public string id;
        public int index;
        public float salesSum;
        public float standing;
        public bool unlocked;

        public EFM_TraderStatus(EFM_Base_Manager baseManager, int index, float salesSum, float standing, bool unlocked)
        {
            this.baseManager = baseManager;
            this.id = IndexToID(index);
            this.index = index;
            this.salesSum = salesSum;
            this.standing = standing;
            this.unlocked = unlocked;
        }

        public int GetLoyaltyLevel()
        {
            Mod.instance.LogInfo("traderbaseDB length: " + (Mod.traderBaseDB.Length));
            JObject traderBase = Mod.traderBaseDB[index];
            Mod.instance.LogInfo("0");
            for (int i=0; i < traderBase["loyaltyLevels"].Count(); ++i)
            {
                Mod.instance.LogInfo(""+i);
                Mod.instance.LogInfo("loyaltylevels null?: "+(traderBase["loyaltyLevels"] == null));
                Mod.instance.LogInfo("loyaltylevels[i] null?: "+(traderBase["loyaltyLevels"][i] == null));
                Mod.instance.LogInfo("minlevel null?: "+(traderBase["loyaltyLevels"][i]["minLevel"] == null));
                Mod.instance.LogInfo("minSalesSum null?: " + (traderBase["loyaltyLevels"][i]["minSalesSum"] == null));
                Mod.instance.LogInfo("minStanding null?: " + (traderBase["loyaltyLevels"][i]["minStanding"] == null));
                int minLevel = ((int)traderBase["loyaltyLevels"][i]["minLevel"]);
                float minSalesSum = ((int)traderBase["loyaltyLevels"][i]["minSalesSum"]);
                float minStanding = ((int)traderBase["loyaltyLevels"][i]["minStanding"]);

                if((int)baseManager.data["level"] < minLevel || salesSum < minSalesSum || standing < minStanding)
                {
                    return i + 1;
                }
            }

            return -1;
        }

        public static int IDToIndex(string ID)
        {
            Mod.instance.LogInfo("getting index of trader with id: "+ID);
            switch (ID)
            {
                case "54cb50c76803fa8b248b4571":
                    return 0;
                case "54cb57776803fa99248b456e":
                    return 1;
                case "579dc571d53a0658a154fbec":
                    return 2;
                case "58330581ace78e27b8b10cee":
                    return 3;
                case "5935c25fb3acc3127c3d8cd9":
                    return 4;
                case "5a7c2eca46aef81a7ca2145d":
                    return 5;
                case "5ac3b934156ae10c4430e83c":
                    return 6;
                case "5c0647fdd443bc2504c2d371":
                    return 7;
                default:
                    return -1;
            }
        }

        public static string IndexToID(int index)
        {
            switch (index)
            {
                case 0:
                    return "54cb50c76803fa8b248b4571";
                case 1:
                    return "54cb57776803fa99248b456e";
                case 2:
                    return "579dc571d53a0658a154fbec";
                case 3:
                    return "58330581ace78e27b8b10cee";
                case 4:
                    return "5935c25fb3acc3127c3d8cd9";
                case 5:
                    return "5a7c2eca46aef81a7ca2145d";
                case 6:
                    return "5ac3b934156ae10c4430e83c";
                case 7:
                    return "5c0647fdd443bc2504c2d371";
                default:
                    return "";
            }
        }

        public static string LoyaltyLevelToRoman(int level)
        {
            switch (level)
            {
                case 1:
                    return "I";
                case 2:
                    return "II";
                case 3:
                    return "III";
                default:
                    return "";
            }
        }
    }
}
