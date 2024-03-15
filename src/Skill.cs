using static EFM.Requirement;

namespace EFM
{
    public class Skill
    {
        /* GLOBAL SKILL SETTINGS
        "SkillEnduranceWeightThreshold": 0.65,
        "SkillExpPerLevel": 100,
        "SkillFatiguePerPoint": 0.6,
        "SkillFatigueReset": 200,
        "SkillFreshEffectiveness": 1.3,
        "SkillFreshPoints": 1,
        "SkillMinEffectiveness": 0.0001,
        "SkillPointsBeforeFatigue": 1,
        */

        // Global skill settings
        public static float skillProgressRate;
        public static float weaponSkillProgressRate;

        // Endurance
        public static float movementAction;
        public static float sprintAction;
        public static float gainPerFatigueStack;

        // Strength
        public static float sprintActionMin;
        public static float sprintActionMax;
        public static float movementActionMin;
        public static float movementActionMax;
        public static float pushUpMin;
        public static float pushUpMax;
        public static float fistfightAction;
        public static float throwAction; // TODO: Check how fast hand is moving in a general throw, if end interaction with hand at >= this velocity, add skill exp
        // TODO: Implement bonus to throw velocity 0.4% per level

        // HideoutManagement
        // TODO: implement additional area slots if elite
        public static float skillPointsPerAreaUpgrade;
        public static float skillPointsPerCraft;
        public static float generatorPointsPerResourceSpent;
        public static float AFUPointsPerResourceSpent;
        public static float waterCollectorPointsPerResourceSpent;
        public static float solarPowerPointsPerResourceSpent;
        public static float consumptionReductionPerLevel;
        public static float skillBoostPercent;

        // Crafting
        public static float pointsPerHourCrafting;
        public static float craftTimeReductionPerLevel;
        public static float productionTimeReductionPerLevel;
        public static float eliteExtraProductions; // TODO: Make it limited to a single one by default

        // Metabolism
        public static float hydrationRecoveryRate;
        public static float energyRecoveryRate;
        public static float increasePositiveEffectDurationRate;
        public static float decreaseNegativeEffectDurationRate;
        public static float decreasePoisonDurationRate;

        // Immunity
        public static float immunityMiscEffects;
        public static float immunityPoisonBuff;
        public static float immunityPainKiller;
        public static float healthNegativeEffect; // TODO Implement when add source of unknown toxin
        public static float stimulatorNegativeBuff;

        // Vitality
        public static float damageTakenAction;
        public static float vitalityHealthNegativeEffect;

        // Health
        public static float skillProgress;

        // StressResistance
        public static float stressResistanceHealthNegativeEffect;
        public static float lowHPDuration;

        // Throwing
        public static float throwingThrowAction; // TODO: Check how fast hand is moving in a general throw, if end interaction with hand at >= this velocity, add skill exp
        // TODO: Implement bonus to throw velocity 0.5% per level

        // RecoilControl
        public static float recoilAction;
        public static float recoilBonusPerLevel;

        // Pistol
        public static float pistolWeaponReloadAction;
        public static float pistolWeaponShotAction;
        public static float pistolWeaponChamberAction; // TODO: See if can get an efficient way of knowing when it was the player who manually triggered a chamber action

        // Revolver
        public static float revolverWeaponReloadAction;
        public static float revolverWeaponShotAction;
        public static float revolverWeaponChamberAction;

        // SMG
        public static float SMGWeaponReloadAction;
        public static float SMGWeaponShotAction;
        public static float SMGWeaponChamberAction;

        // Assault
        public static float assaultWeaponReloadAction;
        public static float assaultWeaponShotAction;
        public static float assaultWeaponChamberAction;

        // Shotgun
        public static float shotgunWeaponReloadAction;
        public static float shotgunWeaponShotAction;
        public static float shotgunWeaponChamberAction;

        // Sniper
        public static float sniperWeaponReloadAction;
        public static float sniperWeaponShotAction;
        public static float sniperWeaponChamberAction;

        // LMG
        public static float LMGWeaponReloadAction;
        public static float LMGWeaponShotAction;
        public static float LMGWeaponChamberAction;

        // HMG
        public static float HMGWeaponReloadAction;
        public static float HMGWeaponShotAction;
        public static float HMGWeaponChamberAction;

        // Launcher
        public static float launcherWeaponReloadAction;
        public static float launcherWeaponShotAction;
        public static float launcherWeaponChamberAction;

        // AttachedLauncher
        public static float attachedLauncherWeaponReloadAction;
        public static float attachedLauncherWeaponShotAction;
        public static float attachedLauncherWeaponChamberAction;

        // DMR
        public static float DMRWeaponReloadAction;
        public static float DMRWeaponShotAction;
        public static float DMRWeaponChamberAction;

        // CovertMovement
        public static float covertMovementAction; // TODO: Check how fast is full speed, then give this much xp to CovertMovement every meter moved at 25% the full speed

        // Search
        public static float searchAction;
        public static float findAction;

        // MagDrills
        public static float raidLoadedAmmoAction;
        public static float raidUnloadedAmmoAction;
        public static float magazineCheckAction;
        // TODO: Implement mag/clip ammo count check precision 

        // Perception
        public static float onlineAction;
        public static float uniqueLoot;
        // TODO: Make grabitty spheres increasingly visible, exponentially, completely visible at elite
        // TODO: Implement examination?

        // Intellect
        public static float examineAction;
        public static float intellectSkillProgress;
        // TODO: Implement weapon damage
        // TODO: Implement examination?

        // Attention
        public static float examineWithInstruction;
        public static float findActionFalse;
        public static float findActionTrue;
        // TODO: Implement examination?

        // Charisma
        public static float skillProgressInt;
        public static float skillProgressAtn;
        public static float skillProgressPer;
        // TODO: Make amount of loyalty gained from quest dependent on charisma level

        // Memory
        public static float anySkillUp;
        public static float memorySkillProgress;
        // TODO: Implement losing skill progress

        // Surgery
        public static float surgeryAction;
        public static float surgerySkillProgress;

        // AimDrills
        public static float weaponShotAction;

        public enum SkillType
        {
            // Used in SkillGroupLevelingBoost type bonuses
            Special,
            Physical,
            Practical,
            Combat,
            Mental,

            NotSpecified
        }
        public SkillType skillType;

        private float _progress; // Actual, 1 lvl ea. 100, so current level is progress/100
        public float progress
        {
            get { return _progress; }
            set
            {
                int preLevel = (int)(_progress / 100);
                _progress = value;
                if (preLevel != (int)(_progress / 100))
                {
                    OnSkillLevelChangedInvoke();
                }
            }
        }
        public float currentProgress; // Affected by effects, this is the one we should check while in raid

        public bool increasing;
        public bool dimishingReturns;
        public float raidProgress;

        public delegate void OnSkillLevelChangedDelegate();
        public event OnSkillLevelChangedDelegate OnSkillLevelChanged;

        public int GetLevel()
        {
            return (int)(progress / 100);
        }

        public static SkillType SkillTypeFromName(string name)
        {
            switch (name)
            {
                case "Practical":
                    return SkillType.Practical;
                case "Physical":
                    return SkillType.Physical;
                case "Special":
                    return SkillType.Special;
                case "Combat":
                    return SkillType.Combat;
                case "Mental":
                    return SkillType.Mental;
                default:
                    Mod.LogError("DEV: Skill.SkillTypeFromName returning NotSpecified for name: " + name);
                    return SkillType.NotSpecified;
            }
        }

        public static int SkillNameToIndex(string name)
        {
            switch (name)
            {
                case "Endurance":
                    return 0;
                case "Strength":
                    return 1;
                case "Vitality":
                    return 2;
                case "Health":
                    return 3;
                case "StressResistance":
                    return 4;
                case "Metabolism":
                    return 5;
                case "Immunity":
                    return 6;
                case "Perception":
                    return 7;
                case "Intellect":
                    return 8;
                case "Attention":
                    return 9;
                case "Charisma":
                    return 10;
                case "Memory":
                    return 11;
                case "Pistol":
                    return 12;
                case "Revolver":
                    return 13;
                case "SMG":
                    return 14;
                case "Assault":
                    return 15;
                case "Shotgun":
                    return 16;
                case "Sniper":
                    return 17;
                case "LMG":
                    return 18;
                case "HMG":
                    return 19;
                case "Launcher":
                    return 20;
                case "AttachedLauncher":
                    return 21;
                case "Throwing":
                    return 22;
                case "Melee":
                    return 23;
                case "DMR":
                    return 24;
                case "RecoilControl":
                    return 25;
                case "AimDrills":
                    return 26;
                case "Troubleshooting":
                    return 27;
                case "Surgery":
                    return 28;
                case "CovertMovement":
                    return 29;
                case "Search":
                    return 30;
                case "MagDrills":
                    return 31;
                case "Sniping":
                    return 32;
                case "ProneMovement":
                    return 33;
                case "FieldMedicine":
                    return 34;
                case "FirstAid":
                    return 35;
                case "LightVests":
                    return 36;
                case "HeavyVests":
                    return 37;
                case "WeaponModding":
                    return 38;
                case "AdvancedModding":
                    return 39;
                case "NightOps":
                    return 40;
                case "SilentOps":
                    return 41;
                case "Lockpicking":
                    return 42;
                case "WeaponTreatment":
                    return 43;
                case "FreeTrading":
                    return 44;
                case "Auctions":
                    return 45;
                case "Cleanoperations":
                    return 46;
                case "Barter":
                    return 47;
                case "Shadowconnections":
                    return 48;
                case "Taskperformance":
                    return 49;
                case "Crafting":
                    return 50;
                case "HideoutManagement":
                    return 51;
                case "WeaponSwitch":
                    return 52;
                case "EquipmentManagement":
                    return 53;
                case "AKSystems":
                    return 54;
                case "AssaultOperations":
                    return 55;
                case "Authority":
                    return 56;
                case "HeavyCaliber":
                    return 57;
                case "RawPower":
                    return 58;
                case "ARSystems":
                    return 59;
                case "DeepWeaponModding":
                    return 60;
                case "LongRangeOptics":
                    return 61;
                case "Negotiations":
                    return 62;
                case "Tactics":
                    return 63;
                default:
                    Mod.LogError("DEV: SkillNameToIndex received name: " + name);
                    return -1;
            }
        }

        public static string SkillIndexToName(int index)
        {
            switch (index)
            {
                case 0:
                    return "Endurance";
                case 1:
                    return "Strength";
                case 2:
                    return "Vitality";
                case 3:
                    return "Health";
                case 4:
                    return "StressResistance";
                case 5:
                    return "Metabolism";
                case 6:
                    return "Immunity";
                case 7:
                    return "Perception";
                case 8:
                    return "Intellect";
                case 9:
                    return "Attention";
                case 10:
                    return "Charisma";
                case 11:
                    return "Memory";
                case 12:
                    return "Pistols";
                case 13:
                    return "Revolvers";
                case 14:
                    return "SMG";
                case 15:
                    return "Assault";
                case 16:
                    return "Shotgun";
                case 17:
                    return "Sniper";
                case 18:
                    return "LMG";
                case 19:
                    return "HMG";
                case 20:
                    return "Launcher";
                case 21:
                    return "AttachedLauncher";
                case 22:
                    return "Throwing";
                case 23:
                    return "Melee";
                case 24:
                    return "DMR";
                case 25:
                    return "RecoilControl";
                case 26:
                    return "AimDrills";
                case 27:
                    return "Troubleshooting";
                case 28:
                    return "Surgery";
                case 29:
                    return "CovertMovement";
                case 30:
                    return "Search";
                case 31:
                    return "MagDrills";
                case 32:
                    return "Sniping";
                case 33:
                    return "ProneMovement";
                case 34:
                    return "FieldMedicine";
                case 35:
                    return "FirstAid";
                case 36:
                    return "LightVests";
                case 37:
                    return "HeavyVests";
                case 38:
                    return "WeaponModding";
                case 39:
                    return "AdvancedModding";
                case 40:
                    return "NightOps";
                case 41:
                    return "SilentOps";
                case 42:
                    return "Lockpicking";
                case 43:
                    return "WeaponTreatment";
                case 44:
                    return "FreeTrading";
                case 45:
                    return "Auctions";
                case 46:
                    return "Cleanoperations";
                case 47:
                    return "Barter";
                case 48:
                    return "Shadowconnections";
                case 49:
                    return "Taskperformance";
                case 50:
                    return "Crafting";
                case 51:
                    return "HideoutManagement";
                case 52:
                    return "WeaponSwitch";
                case 53:
                    return "EquipmentManagement";
                case 54:
                    return "AKSystems";
                case 55:
                    return "AssaultOperations";
                case 56:
                    return "Authority";
                case 57:
                    return "HeavyCaliber";
                case 58:
                    return "RawPower";
                case 59:
                    return "ARSystems";
                case 60:
                    return "DeepWeaponModding";
                case 61:
                    return "LongRangeOptics";
                case 62:
                    return "Negotiations";
                case 63:
                    return "Tactics";
                default:
                    Mod.LogError("SkillIndexToName received index: " + index);
                    return "";
            }
        }

        public void OnSkillLevelChangedInvoke()
        {
            if(OnSkillLevelChanged != null)
            {
                OnSkillLevelChanged();
            }
        }
    }
}
