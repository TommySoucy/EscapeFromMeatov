using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EFM
{
    public class EFM_AreaBonus
    {
        public enum BonusType
        {
            ExperienceRate,
            QuestMoneyReward,
            MaximumEnergyReserve,
            FuelConsumption,
            DebuffEndDelay,
            ScavCooldownTimer,
            InsuranceReturnTime,
            RagfairCommission,
            AdditionalSlots,
            SkillGroupLevelingBoost,
            StashSize,

            EnergyRegeneration, // EnergyRate, Value/hr
            HealthRegeneration, // HealthRate, (440/100 * Value)/hr
            HydrationRegeneration, // HydrationRate, Value/hr
        }
        public BonusType bonusType;
        public float value;
        public EFM_Skill.SkillType skillType;

        public EFM_Effect effectCaused;
        public bool active;

        public void SetActive(bool active)
        {
            if(active == this.active)
            {
                return;
            }

            if (active)
            {
                switch (bonusType) 
                {
                    case BonusType.ExperienceRate:
                        EFM_Base_Manager.currentExperienceRate += value / 100;
                        break;
                    case BonusType.QuestMoneyReward:
                        EFM_Base_Manager.currentQuestMoneyReward += value / 100;
                        break;
                    case BonusType.MaximumEnergyReserve:
                        Mod.maxEnergy += value;
                        break;
                    case BonusType.FuelConsumption:
                        EFM_Base_Manager.currentFuelConsumption += value / 100;
                        break;
                    case BonusType.DebuffEndDelay:
                        EFM_Base_Manager.currentDebuffEndDelay += value / 100;
                        break;
                    case BonusType.ScavCooldownTimer:
                        EFM_Base_Manager.currentScavCooldownTimer += value / 100;
                        break;
                    case BonusType.InsuranceReturnTime:
                        EFM_Base_Manager.currentInsuranceReturnTime += value / 100;
                        break;
                    case BonusType.RagfairCommission:
                        EFM_Base_Manager.currentRagfairCommission += value / 100;
                        break;
                    case BonusType.AdditionalSlots:
                        // This is handled directly in areasDB, the number of slots of the areas at each level has been set in consequence of these bonuses
                        break;
                    case BonusType.SkillGroupLevelingBoost:
                        if(EFM_Base_Manager.currentSkillGroupLevelingBoosts == null)
                        {
                            EFM_Base_Manager.currentSkillGroupLevelingBoosts = new Dictionary<EFM_Skill.SkillType, float>();
                        }
                        if (EFM_Base_Manager.currentSkillGroupLevelingBoosts.ContainsKey(skillType))
                        {
                            EFM_Base_Manager.currentSkillGroupLevelingBoosts[skillType] += value / 100;
                        }
                        else
                        {
                            EFM_Base_Manager.currentSkillGroupLevelingBoosts.Add(skillType, 1 + value / 100);
                        }
                        break;
                    case BonusType.StashSize:
                        // Does not apply considering the stash is a physical thing
                        break;

                    case BonusType.EnergyRegeneration:
                        effectCaused = new EFM_Effect();
                        effectCaused.effectType = EFM_Effect.EffectType.EnergyRate;
                        effectCaused.value = value / 60; // from /hr to /min
                        effectCaused.hasTimer = false;
                        effectCaused.hideoutOnly = true;
                        EFM_Effect.effects.Add(effectCaused);
                        break;
                    case BonusType.HealthRegeneration:
                        effectCaused = new EFM_Effect();
                        effectCaused.effectType = EFM_Effect.EffectType.HealthRate;
                        effectCaused.value = (4.4f * value) / 60; // from /hr to /min
                        effectCaused.hasTimer = false;
                        effectCaused.partIndex = -1; // Affects everypart
                        effectCaused.hideoutOnly = true;
                        EFM_Effect.effects.Add(effectCaused);
                        break;
                    case BonusType.HydrationRegeneration:
                        effectCaused = new EFM_Effect();
                        effectCaused.effectType = EFM_Effect.EffectType.HydrationRate;
                        effectCaused.value = value / 60; // from /hr to /min
                        effectCaused.hasTimer = false;
                        effectCaused.hideoutOnly = true;
                        EFM_Effect.effects.Add(effectCaused);
                        break;

                    default:
                        break;
                }

                if(Mod.activeBonuses == null)
                {
                    Mod.activeBonuses = new List<EFM_AreaBonus>();
                }

                Mod.activeBonuses.Add(this);
            }
            else
            {
                switch (bonusType)
                {
                    case BonusType.ExperienceRate:
                        EFM_Base_Manager.currentExperienceRate -= value / 100;
                        break;
                    case BonusType.QuestMoneyReward:
                        EFM_Base_Manager.currentQuestMoneyReward -= value / 100;
                        break;
                    case BonusType.MaximumEnergyReserve:
                        Mod.maxEnergy -= value;
                        break;
                    case BonusType.FuelConsumption:
                        EFM_Base_Manager.currentFuelConsumption -= value / 100;
                        break;
                    case BonusType.DebuffEndDelay:
                        EFM_Base_Manager.currentDebuffEndDelay -= value / 100;
                        break;
                    case BonusType.ScavCooldownTimer:
                        EFM_Base_Manager.currentScavCooldownTimer -= value / 100;
                        break;
                    case BonusType.InsuranceReturnTime:
                        EFM_Base_Manager.currentInsuranceReturnTime -= value / 100;
                        break;
                    case BonusType.RagfairCommission:
                        EFM_Base_Manager.currentRagfairCommission -= value / 100;
                        break;
                    case BonusType.AdditionalSlots:
                        // This is handled directly in areasDB, the number of slots of the areas at each level has been set in consequence of these bonuses
                        break;
                    case BonusType.SkillGroupLevelingBoost:
                        EFM_Base_Manager.currentSkillGroupLevelingBoosts[skillType] -= value / 100;
                        break;
                    case BonusType.StashSize:
                        // Does not apply considering the stash is a physical thing
                        break;

                    case BonusType.EnergyRegeneration:
                        if(effectCaused != null && effectCaused.active)
                        {
                            // If in hideout
                            if (Mod.currentLocationIndex == 1)
                            {
                                Mod.currentEnergyRate -= effectCaused.value / 60;
                            }
                            //else In raid map, no need to do anything, raid manager will disable effects itself
                            EFM_Effect.effects.Remove(effectCaused);
                        }
                        break;
                    case BonusType.HealthRegeneration:
                        if (effectCaused != null && effectCaused.active)
                        {
                            // If in hideout
                            if (Mod.currentLocationIndex == 1)
                            {
                                for (int i = 0; i < 7; ++i)
                                {
                                    Mod.currentHealthRates[i] -= (4.4f * effectCaused.value) / 60 / 7;
                                }
                            }
                            //else In raid map, no need to do anything, raid manager will disable effects itself
                            EFM_Effect.effects.Remove(effectCaused);
                        }
                        break;
                    case BonusType.HydrationRegeneration:
                        if (effectCaused != null && effectCaused.active)
                        {
                            // If in hideout
                            if (Mod.currentLocationIndex == 1)
                            {
                                Mod.currentHydrationRate -= effectCaused.value / 60;
                            }
                            //else In raid map, no need to do anything, raid manager will disable effects itself
                            EFM_Effect.effects.Remove(effectCaused);
                        }
                        break;

                    default:
                        break;
                }

                if (Mod.activeBonuses == null)
                {
                    Mod.activeBonuses = new List<EFM_AreaBonus>();
                }

                Mod.activeBonuses.Add(this);
            }

            this.active = active;
        }
    }
}
