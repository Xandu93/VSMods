using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.Server;
using Vintagestory.ServerMods;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsPlayerBehavior : EntityBehavior
    {
        private readonly Survival survival;
        private readonly Combat combat;
        private readonly Husbandry husbandry;
        private readonly TemporalAdaptation adaptation;

        private double oldStability;
        private float oldHealth;
        private float oldOxygen;

        private float timeSinceUpdate;
        private uint lastWeatherForecast;

        internal float HoursSlept { get; set; }

        internal Action<int> NudistSlotNotified;

        public override string PropertyName() => "XSkillsPlayer";
        protected EntityBehaviorTemporalStabilityAffected TemporalAffected => this.entity.GetBehavior<EntityBehaviorTemporalStabilityAffected>();
        protected EntityBehaviorHealth Health => this.entity.GetBehavior<EntityBehaviorHealth>();

        public XSkillsPlayerBehavior(Entity entity) : base(entity)
        {
            this.survival = XLeveling.Instance(entity.Api)?.GetSkill("survival") as Survival;
            this.combat = XLeveling.Instance(entity.Api)?.GetSkill("combat") as Combat;
            this.husbandry = XLeveling.Instance(entity.Api)?.GetSkill("husbandry") as Husbandry;
            this.adaptation = XLeveling.Instance(entity.Api)?.GetSkill("temporaladaptation") as TemporalAdaptation;
            ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
            ITreeAttribute oxygenTree = entity.WatchedAttributes.GetTreeAttribute("oxygen");

            this.oldStability = TemporalAffected?.OwnStability ?? 1.0;
            this.oldHealth = healthTree?.GetFloat("currenthealth") ?? 0.0f;
            this.oldOxygen = oxygenTree?.GetFloat("currentoxygen") ?? 0.0f;
            this.HoursSlept = 0.0f;
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            EntityBehaviorHealth behaviorHealth = (this.entity.GetBehavior("health") as EntityBehaviorHealth);
            if (behaviorHealth != null) behaviorHealth.onDamaged += OnDamage;
        }

        protected float OnDamageInternal(float damage, DamageSource dmgSource)
        {
            if (dmgSource.Source == EnumDamageSource.Player && !((combat?.Config as CombatSkillConfig)?.enableAbilitiesInPvP ?? false)) return damage;

            if (dmgSource.Source != EnumDamageSource.Player &&
                dmgSource.Source != EnumDamageSource.Fall &&
                dmgSource.Source != EnumDamageSource.Entity &&
                dmgSource.Source != EnumDamageSource.Explosion &&
                dmgSource.Source != EnumDamageSource.Machine) return damage;

            Entity sourceEntity = dmgSource.SourceEntity;
            PlayerSkillSet playerSkillSet = this.entity.GetBehavior<PlayerSkillSet>();
            if (playerSkillSet == null) return damage;

            //last stand
            if (Health != null && dmgSource.Type != EnumDamageType.Heal)
            {
                PlayerAbility playerAbility = playerSkillSet[survival.Id]?[survival.LastStandId];
                if (damage > Health.Health && Health.MaxHealth > 0.0f && playerAbility?.Tier > 0)
                {
                    float ratio = Health.Health / Health.MaxHealth;
                    if (ratio * playerAbility.FValue(0) >= this.entity.World.Rand.NextDouble())
                    {
                        damage = Health.Health - 0.1f;
                    }
                }
            }

            //feather fall
            if (dmgSource.Source == EnumDamageSource.Fall && dmgSource.Type == EnumDamageType.Gravity)
            {
                PlayerAbility playerAbility = playerSkillSet[survival.Id]?[survival.FeatherFallId];
                damage = Math.Max(damage - playerAbility.Value(0), 0);
                damage *= 1.0f - playerAbility.FValue(1);
            }

            //timeless
            if (this.adaptation != null && dmgSource.Source == EnumDamageSource.Machine && dmgSource.SourceEntity == null && dmgSource.Type == EnumDamageType.Poison)
            {
                PlayerAbility playerAbility = playerSkillSet[this.adaptation.Id]?[this.adaptation.TimelessId];
                if (playerAbility.Tier > 0) damage = 0.0f;
            }

            //hunter
            if (this.husbandry != null && sourceEntity?.GetBehavior<XSkillsAnimalBehavior>() != null)
            {
                PlayerAbility playerAbility = playerSkillSet[this.husbandry.Id]?[this.husbandry.HunterId];
                if (playerAbility != null) damage *= 1.0f - playerAbility.SkillDependentFValue();
            }

            //shifter
            if (this.adaptation != null && sourceEntity != null)
            {
                PlayerAbility playerAbility = playerSkillSet[this.adaptation.Id]?[this.adaptation.ShifterId];
                if (playerAbility != null)
                {
                    float chance = playerAbility.FValue(0) * (1.0f - this.entity.WatchedAttributes.GetFloat("temporalStability"));
                    if (chance > this.entity.World.Rand.NextDouble()) damage = 0.0f;
                }

            }

            //meat shield
            if (this.survival != null)
            {
                ITreeAttribute hungerTree = this.entity.WatchedAttributes.GetTreeAttribute("hunger");
                PlayerAbility playerAbility = playerSkillSet[this.survival.Id]?[this.survival.MeatShieldId];
                if (playerAbility != null && hungerTree != null)
                {
                    float saturation = hungerTree.GetFloat("currentsaturation");
                    float saturationLoss = damage * playerAbility.Value(1);
                    if (saturation > saturationLoss)
                    {
                        hungerTree.SetFloat("currentsaturation", saturation - saturationLoss);
                        this.entity.WatchedAttributes.MarkPathDirty("hunger");
                        damage *= 1.0f - playerAbility.FValue(0);
                    }
                }
            }
            return damage;
        }

        public float OnDamage(float damage, DamageSource dmgSource)
        {
            damage = OnDamageInternal(damage, dmgSource);
            if (dmgSource.Type != EnumDamageType.Heal && damage >= entity.WatchedAttributes.GetTreeAttribute("health")?.GetDecimal("currenthealth"))
            {
                BeforeDeath();
            }
            return damage;
        }

        protected void ApplyAbilitiesOxygen()
        {
            if (entity.WatchedAttributes == null || this.survival == null) return;
            PlayerAbility playerAbility = this.entity.GetBehavior<PlayerSkillSet>()?[this.survival.Id]?[survival.DiverId];
            if ((playerAbility?.Tier ?? 0) < 1) return; 

            TreeAttribute oxygen = entity.WatchedAttributes.GetTreeAttribute("oxygen") as TreeAttribute;
            float currentOxygen = oxygen.GetFloat("currentoxygen");
            float maxOxygen = oxygen.GetFloat("maxoxygen");

            float deltaOxygen = oldOxygen - currentOxygen;
            if (deltaOxygen > 0.0f)
            {
                deltaOxygen *= playerAbility.FValue(0);
                currentOxygen = Math.Min(currentOxygen + deltaOxygen, maxOxygen);
                oxygen.SetFloat("currentoxygen", currentOxygen);
            }
            oldOxygen = currentOxygen;
        }

        protected void ApplyAbilitiesHealth()
        {
            EntityStats stats = entity.Stats;
            IGameCalendar calender = this.entity.World.Calendar;
            if (stats == null || entity.World == null) return;

            int sunLight = -1;
            float maxLight = entity.World.SunBrightness;
            float healingEffectiveness = 0.0f;
            string statCode = "abilities-light";

            //survival
            if (this.survival != null)
            {
                PlayerSkill playerSurvival = this.entity.GetBehavior<PlayerSkillSet>()?[this.survival.Id];
                if (playerSurvival != null)
                {
                    //experience
                    float surivalTime = calender.SpeedOfTime * timeSinceUpdate / (calender.HoursPerDay * 60 * 60);
                    playerSurvival.AddExperience(surivalTime);

                    //photosynthesis
                    PlayerAbility playerAbility = playerSurvival[this.survival.PhotosynthesisId];
                    if (playerAbility != null && playerAbility.Tier > 0)
                    {
                        int fullLight = this.entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.MaxTimeOfDayLight);
                        if (sunLight == -1) sunLight = this.entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight);

                        if (sunLight > maxLight * 0.25)
                        {
                            healingEffectiveness += playerAbility.FValue(0) * (sunLight / maxLight);
                        }
                        else if (fullLight < maxLight * 0.25)
                        {
                            healingEffectiveness -= playerAbility.FValue(1) * (1.0f - fullLight / (maxLight * 0.25f));
                        }
                    }
                }
            }
            
            //combat
            if (this.combat != null)
            {
                PlayerSkill playerCombat = this.entity.GetBehavior<PlayerSkillSet>()?[this.combat.Id];
                if (playerCombat != null)
                {
                    //vampire
                    PlayerAbility playerAbility = playerCombat[this.combat.VampireId];
                    if (playerAbility != null && playerAbility.Tier > 0)
                    {
                        if (sunLight == -1) sunLight = this.entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.TimeOfDaySunLight);
                        if (sunLight > maxLight * 0.25)
                        {
                            healingEffectiveness -= playerAbility.FValue(1) * (sunLight / maxLight);
                        }
                    }
                }
            }
            if (healingEffectiveness == 0.0f) stats.Remove("healingeffectivness", statCode);
            else stats.Set("healingeffectivness", statCode, healingEffectiveness, false);
        }

        protected void ApplyAbilitiesStability()
        {
            //temporal adaption
            if (this.adaptation == null) return;
            if (entity?.Stats == null) return;

            EntityBehaviorTemporalStabilityAffected temporalAffected = TemporalAffected;
            PlayerSkill playeAdaptation = this.entity.GetBehavior<PlayerSkillSet>()?[this.adaptation.Id];
            if (temporalAffected == null || playeAdaptation == null) return;

            double stability = temporalAffected.OwnStability;
            double change = oldStability - stability;

            //any change
            if (change != 0.0)
            {
                //fast forward
                PlayerAbility playerAbility = playeAdaptation[this.adaptation.FastForwardId];
                float value = (float)(playerAbility.FValue(0) * (1.0f - temporalAffected.OwnStability));
                entity.Stats.Set("hungerrate", "ability-ff", value, false);
                entity.Stats.Set("miningSpeedMul", "ability-ff", value, false);
            }

            if (change > 0.0)
            {
                float exp = (float)change * 50.0f;
                float lostMult = 1.0f;

                playeAdaptation.AddExperience(exp);

                //temporal stable
                PlayerAbility playerAbility = playeAdaptation[this.adaptation.TemporalStableId];
                lostMult *= 1.0f - playerAbility.SkillDependentFValue();

                //caveman
                playerAbility = playeAdaptation[this.adaptation.CavemanId];
                lostMult *= 1.0f - playerAbility.SkillDependentFValue() * Math.Max((1.0f - (int)entity.Pos.Y / TerraGenConfig.seaLevel), 0.0f);

                //temporal adapted
                playerAbility = playeAdaptation[this.adaptation.TemporalAdaptedId];
                lostMult *= 1.0f - playerAbility.SkillDependentFValue() * (1.0f - (float)stability);

                temporalAffected.TempStabChangeVelocity *= temporalAffected.TempStabChangeVelocity * lostMult;
                temporalAffected.OwnStability += change * (1.0f - lostMult);
            }
            else if (change < 0.0)
            {
                //temporal recovery
                PlayerAbility playerAbility = playeAdaptation.PlayerAbilities[this.adaptation.TemporalRecoveryId];
                change *= playerAbility.FValue(0);

                temporalAffected.OwnStability -= change;
            }
            oldStability = stability;
        }

        public void ApplyMovementAbilities()
        {
            if (this.survival == null) return;
            if (this.entity.World == null) return;

            PlayerAbility ability = entity.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.OnTheRoadId];
            if (ability == null) return;

            if (ability.Tier > 0)
            {
                EntityPos pos = entity.SidedPos;
                if (pos == null) return;

                int y1 = (int)(pos.Y - 0.05f);
                int y2 = (int)(pos.Y + 0.01f);
                Block belowBlock = this.entity.World.BlockAccessor.GetBlock(new BlockPos((int)pos.X, y1, (int)pos.Z, pos.Dimension));
                Block insideBlock = this.entity.World.BlockAccessor.GetBlock(new BlockPos((int)pos.X, y2, (int)pos.Z, pos.Dimension));

                float multiplier = belowBlock.WalkSpeedMultiplier * (y1 == y2 ? 1.0f : insideBlock.WalkSpeedMultiplier);
                if (multiplier <= 1.0f)
                {
                    entity.Stats.Set("walkspeed", "ability-ontheroads", 0.0f, false);
                }
                else
                {
                    entity.Stats.Set("walkspeed", "ability-ontheroads", ability.FValue(0), false);
                }
            }
            else
            {
                entity.Stats.Remove("walkspeed", "ability-ontheroads");
            }
        }

        public override void OnGameTick(float deltaTime)
        {
            if (this.entity == null) return;

            timeSinceUpdate += deltaTime;

            if(timeSinceUpdate >= 1.0f)
            {
                ApplyAbilitiesHealth();
                ApplyAbilitiesStability();
                ApplyAbilitiesOxygen();
                ApplyMovementAbilities();
                timeSinceUpdate = 0.0f;
            }

            if (entity.Api.Side == EnumAppSide.Client)
            {
                //MaxSaturationFix();
                if (lastWeatherForecast < (uint)this.entity.World.Calendar.TotalDays)
                {
                    PlayerAbility ability = entity.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.MeteorologistId];
                    if (ability?.Tier > 0)
                    {
                        Survival.GenerateWeatherForecast(entity.Api, entity.Pos, ability.Value(0), ability.FValue(1));
                    }
                    lastWeatherForecast = (uint)this.entity.World.Calendar.TotalDays;
                }
            }
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
            PlayerSkill playerSkill = entity.GetBehavior<PlayerSkillSet>()?[this.combat.Id];
            
            //link inventories
            XSkillsPlayerInventory inv = (this.entity as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv != null) inv.Linked = true;

            if (playerSkill == null || healthTree == null) return;

            float newHealth = healthTree.GetFloat("currenthealth");
            if (damage < 0.0f) return;
            oldHealth -= damage;

            PlayerAbility playerAbility;

            //Healer
            //uses TicksPerDuration to identify the hot from healing items
            if (damageSource.Source == EnumDamageSource.Internal && damageSource.Type == EnumDamageType.Heal && damageSource.TicksPerDuration > 1)
            {
                AffectedEntityBehavior affected = this.entity.GetBehavior<AffectedEntityBehavior>();
                XEffectsSystem effectSystem = this.entity.Api.ModLoader.GetModSystem<XEffectsSystem>();

                if (affected != null && effectSystem != null)
                {
                    playerAbility = this.entity.GetBehavior<PlayerSkillSet>()?[this.survival.Id]?[this.survival.HealerId];
                    if (playerAbility?.Tier > 0)
                    {
                        HotEffect effect = effectSystem?.CreateEffect("hot") as HotEffect;
                        if (effect != null)
                        {
                            effect.Duration = playerAbility.Value(1);
                            effect.Heal = damage * playerAbility.FValue(0) / effect.Duration * effect.Interval;

                            affected.AddEffect(effect);
                            affected.MarkDirty();
                        }
                    }
                }
            }

            //adrenaline rush
            playerAbility = playerSkill[this.combat.AdrenalineRushId];
            if (newHealth > 0 && newHealth / healthTree.GetFloat("maxhealth") <= playerAbility.FValue(0))
            {
                AffectedEntityBehavior affected = entity.GetBehavior<AffectedEntityBehavior>();
                if (affected == null ||
                    affected.IsAffectedBy("adrenalinerush") ||
                    affected.IsAffectedBy("exhaustion") ||
                    affected.IsAffectedBy("bloodlust")) return;

                XEffectsSystem effectSystem = combat.XLeveling.Api.ModLoader.GetModSystem<XEffectsSystem>();
                Condition effect = effectSystem?.CreateEffect("adrenalinerush") as Condition;
                if (effect != null)
                {
                    effect.Duration = playerAbility.Value(3);
                    effect.MaxStacks = 1;
                    effect.Stacks = 1;
                    effect.SetIntensity("walkspeed", playerAbility.FValue(1));
                    effect.SetIntensity("receivedDamageMultiplier", 1.0f - playerAbility.FValue(2));
                    TriggerEffect trigger = effect.Effect("trigger") as TriggerEffect;
                    if (trigger != null)
                    {
                        trigger.EffectDuration = playerAbility.Value(4);
                        trigger.EffectIntensity = -0.2f;
                    }
                    affected.AddEffect(effect);
                    affected.MarkDirty();
                }
            }
        }

        protected virtual void BeforeDeath()
        {
            //inventory unlink
            PlayerSkillSet playerSkillSet = this.entity.GetBehavior<PlayerSkillSet>();
            if (playerSkillSet == null) return;
            PlayerSkill playerSurvival = playerSkillSet[this.survival.Id];
            if (playerSurvival == null) return;

            PlayerAbility playerAbility = playerSurvival[survival.SoulboundBagId];
            if (playerAbility?.Tier > 0)
            {
                XSkillsPlayerInventory inv = (this.entity as EntityPlayer)?.Player.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
                if (inv != null) inv.Linked = false;
            }
        }

        //public override void OnEntityRevive()
        //{
        //    base.OnEntityRevive();

        //    XSkillsPlayerInventory inv = (this.entity as EntityPlayer)?.Player.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
        //    inv.Linked = true;
        //}

        //the game sometimes randomly resets the max saturation to 1500
        //but this 'fix' seems to make the issue worse
        //public void MaxSaturationFix()
        //{
        //    ITreeAttribute hungerTree = entity.WatchedAttributes.GetTreeAttribute("hunger");
        //    float maxSaturation = hungerTree.GetFloat("maxsaturation");
        //    if (maxSaturation != 1500.0f || this.survival == null) return;

        //    PlayerSkill playerSurvival = this.entity.GetBehavior<PlayerSkillSet>()?[this.survival.Id];
        //    if (playerSurvival == null) return;
        //    PlayerAbility playerAbility = playerSurvival[this.survival.HugeStomachId];
        //    if (playerAbility == null) return;
        //    if (playerAbility.Tier <= 0) return;
        //    maxSaturation = (1500 + playerAbility.Value(0));
        //    hungerTree.SetFloat("maxSaturation", maxSaturation);
        //}
    }//! XSkillsPlayerBehavior
}//!namespace XSkills
