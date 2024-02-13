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

        public float progress; // Actual, 1 lvl ea. 100, so current level is progress/100
        public float currentProgress; // Affected by effects, this is the one we should check while in raid

        public bool increasing;
        public bool dimishingReturns;
        public float raidProgress;

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
    }
}
