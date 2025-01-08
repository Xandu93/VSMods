using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using Vintagestory.GameContent;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsEntityBehavior : EntityBehavior
    {
        protected Combat combat;
        protected Farming farming;
        protected Husbandry husbandry;
        protected Mining mining;
        protected Digging digging;
        protected Forestry forestry;
        protected Metalworking metalworking;
        protected Cooking cooking;
        protected TemporalAdaptation temporalAdaptation;
        protected float xp;

        public override string PropertyName() => "XSkillsEntity";

        public XSkillsEntityBehavior(Entity entity) : base(entity)
        {
            this.combat = XLeveling.Instance(entity.Api)?.GetSkill("combat") as Combat;
            this.farming = XLeveling.Instance(entity.Api)?.GetSkill("farming") as Farming;
            this.husbandry = XLeveling.Instance(entity.Api)?.GetSkill("husbandry") as Husbandry;
            this.mining = XLeveling.Instance(entity.Api)?.GetSkill("mining") as Mining;
            this.digging = XLeveling.Instance(entity.Api)?.GetSkill("digging") as Digging;
            this.forestry = XLeveling.Instance(entity.Api)?.GetSkill("forestry") as Forestry;
            this.metalworking = XLeveling.Instance(entity.Api)?.GetSkill("metalworking") as Metalworking;
            this.cooking = XLeveling.Instance(entity.Api)?.GetSkill("cooking") as Cooking;
            this.temporalAdaptation = XLeveling.Instance(entity.Api)?.GetSkill("temporaladaptation") as TemporalAdaptation;
            EntityBehaviorHealth behaviorHealth = (this.entity.GetBehavior("health") as EntityBehaviorHealth);
            if (behaviorHealth != null) behaviorHealth.onDamaged += OnDamage;
            this.xp = 0.0f;
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            this.xp = attributes["xp"].AsFloat(0.0f);
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (damageSourceForDeath == null) return;
            EntityPlayer byPlayer =
                damageSourceForDeath.SourceEntity as EntityPlayer ??
                damageSourceForDeath.CauseEntity as EntityPlayer ??
                (damageSourceForDeath.SourceEntity as EntityThrownStone)?.FiredBy as EntityPlayer;
            if (this.combat == null || byPlayer == null) return;

            PlayerSkill playerSkill = byPlayer.GetBehavior<PlayerSkillSet>()?[this.combat.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(this.xp);

            //fresh flesh
            PlayerAbility playerAbility = playerSkill[combat.FreshFleshId];
            if (playerAbility.Tier > 0 && !this.entity.Attributes.GetBool("isMechanical", false))
            {
                byPlayer.ReceiveSaturation(playerAbility.Value(0), EnumFoodCategory.Protein);
            }

            //bloodlust
            playerAbility = playerSkill[this.combat.BloodlustId];
            if (playerAbility.Tier > 0)
            {
                AffectedEntityBehavior affected = byPlayer.GetBehavior<AffectedEntityBehavior>();
                if (affected == null ||
                    affected.IsAffectedBy("adrenalineRush") ||
                    affected.IsAffectedBy("exhaustion")) return;

                XEffectsSystem effectSystem = combat.XLeveling.Api.ModLoader.GetModSystem<XEffectsSystem>();
                Condition effect = effectSystem?.CreateEffect("bloodlust") as Condition;
                if (effect != null)
                {
                    effect.Duration = playerAbility.Value(2);
                    effect.MaxStacks = playerAbility.Value(3);
                    effect.Stacks = 1;
                    effect.SetIntensity("meleeWeaponsDamage", playerAbility.FValue(0));
                    effect.SetIntensity("receivedDamageMultiplier", playerAbility.FValue(1));
                    affected.AddEffect(effect);
                    affected.MarkDirty();
                }
            }
        }

        public float OnDamage(float damage, DamageSource dmgSource)
        {
            EntityPlayer byPlayer = 
                dmgSource.SourceEntity as EntityPlayer ??
                dmgSource.CauseEntity as EntityPlayer ??
                (dmgSource.SourceEntity as EntityThrownStone)?.FiredBy as EntityPlayer;
            if (this.combat == null || byPlayer == null) return damage;

            PlayerSkillSet playerSkillSet = byPlayer.GetBehavior<PlayerSkillSet>();
            PlayerSkill playerSkill = playerSkillSet?[this.combat.Id];
            if (playerSkill == null) return damage;

            PlayerAbility playerAbility = null;
            int skillLevel = 0;

            //swordsman, spearman, archer, shovel knight, tool mastery
            switch (byPlayer.Player.InventoryManager.ActiveTool)
            {
                case EnumTool.Sword:
                case EnumTool.Club:
                case EnumTool.Mace:
                case EnumTool.Warhammer:
                    playerAbility = playerSkill[combat.SwordsmanId];
                    break;
                case EnumTool.Spear:
                case EnumTool.Javelin:
                case EnumTool.Pike:
                case EnumTool.Poleaxe:
                case EnumTool.Halberd:
                case EnumTool.Polearm:
                case EnumTool.Staff:
                    playerAbility = playerSkill[combat.SpearmanId];
                    break;
                case EnumTool.Bow:
                case EnumTool.Sling:
                case EnumTool.Crossbow:
                case EnumTool.Firearm:
                    playerAbility = playerSkill[combat.ArcherId];
                    break;
                case EnumTool.Knife:
                    skillLevel = playerSkillSet[this.husbandry.Id]?.Level ?? 0;
                    break;
                case EnumTool.Pickaxe:
                    skillLevel = playerSkillSet[this.mining.Id]?.Level ?? 0;
                    break;
                case EnumTool.Axe:
                    skillLevel = playerSkillSet[this.forestry.Id]?.Level ?? 0;
                    break;
                case EnumTool.Hammer:
                case EnumTool.Saw:
                    skillLevel = playerSkillSet[this.metalworking.Id]?.Level ?? 0;
                    break;
                case EnumTool.Hoe:
                case EnumTool.Sickle:
                case EnumTool.Scythe:
                    skillLevel = playerSkillSet[this.farming.Id]?.Level ?? 0;
                    break;
                case EnumTool.Shovel:
                    playerAbility = playerSkill[combat.ShovelKnightId];
                    if (playerAbility.FValue(0) > byPlayer.World.Rand.NextDouble()) damage *= playerAbility.Value(1);
                    playerAbility = null;

                    skillLevel = playerSkillSet[this.digging.Id]?.Level ?? 0;
                    break;
                case null:
                    //probably a thrown spear
                    if (dmgSource.Type == EnumDamageType.PiercingAttack)
                    {
                        playerAbility = playerSkill[combat.SpearmanId];
                        break;
                    }
                    if (byPlayer.Player.InventoryManager.ActiveHotbarSlot?.Itemstack?.Item?.FirstCodePart() == "rollingpin")
                    {
                        skillLevel = playerSkillSet[this.cooking.Id]?.Level ?? 0;
                    }
                    break;
                default:
                    break;
            }
            if (playerAbility != null) damage *= 1.0f + playerAbility.SkillDependentFValue();
            else if (skillLevel != 0)
            {
                playerAbility = playerSkill[combat.ToolMasteryId];
                if (playerAbility != null) damage *= 1.0f + playerAbility.SkillDependentFValue(skillLevel);
            }
            //iron fist, monk
            if (byPlayer.Player.InventoryManager.ActiveHotbarSlot?.Itemstack == null)
            {
                InventoryCharacter inv = byPlayer.Player.InventoryManager.GetOwnInventory("character") as InventoryCharacter;
                if (inv != null && inv.Count >= 15)
                {
                    PlayerAbility monkAbility = playerSkill[combat.MonkId];
                    playerAbility = playerSkill[combat.IronFistId];

                    if (playerAbility.Tier > 0 || monkAbility.Tier > 0)
                    {
                        float protectionTier =
                            ((inv[12].Itemstack?.Item as ItemWearable)?.ProtectionModifiers.ProtectionTier ?? 0.0f) +
                            ((inv[13].Itemstack?.Item as ItemWearable)?.ProtectionModifiers.ProtectionTier ?? 0.0f) +
                            ((inv[14].Itemstack?.Item as ItemWearable)?.ProtectionModifiers.ProtectionTier ?? 0.0f);
                        protectionTier /= 3;

                        if (playerAbility.Tier > 0) damage *= protectionTier * playerAbility.Value(0);
                        else damage *= Math.Max((monkAbility.Value(0) - ((protectionTier / 3.0f) * monkAbility.Value(0))) , 1);
                    }
                    playerAbility = playerSkill[combat.MonkId];
                }
            }

            //stable warrior and temporal unstable
            if (this.temporalAdaptation != null)
            {
                PlayerSkill temporalSkill = byPlayer.GetBehavior<PlayerSkillSet>()?[this.temporalAdaptation.Id];
                SystemTemporalStability system = this.combat.XLeveling.Api.ModLoader.GetModSystem<SystemTemporalStability>();

                if (temporalSkill != null && system != null)
                {
                    playerAbility = temporalSkill[temporalAdaptation.StableWarriorId];
                    PlayerAbility playerAbility2 = temporalSkill[temporalAdaptation.TemporalUnstableId];
                    float stability = Math.Clamp(system.GetTemporalStability(byPlayer.Pos.XYZ), 0.0f, 1.0f);

                    if (playerAbility2.Tier > 0)
                    {
                        damage *= 1.0f + 0.0001f * playerAbility.Value(0) * (100 + playerAbility2.Value(0)) * (1.0f - stability);
                    }
                    else
                    {
                        damage *= 1.0f + 0.01f * playerAbility.Value(0) * stability;
                    }
                }
            }

            //burning rage
            playerAbility = playerSkill[combat.BurningRageId];
            if (playerAbility.Value(0) * 0.01f > byPlayer.World.Rand.NextDouble()) this.entity.Ignite();
            return damage;
        }

        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            base.OnEntityReceiveDamage(damageSource, ref damage);
            if (this.entity.Api.Side == EnumAppSide.Server)
            {
                EntityPlayer byPlayer = damageSource.SourceEntity as EntityPlayer;
                if (this.combat == null || byPlayer == null) return;

                PlayerSkill playerSkill = byPlayer.GetBehavior<PlayerSkillSet>()?[this.combat.Id];
                if (playerSkill == null) return;

                PlayerAbility playerAbility = playerSkill[combat.VampireId];

                //vampire
                if (playerAbility.Tier > 0)
                {
                    float health = damage * playerAbility.Value(0) * 0.01f;

                    EntityBehaviorHealth playerHealth = (byPlayer.GetBehavior("health") as EntityBehaviorHealth);
                    if (playerHealth == null) return;
                    if (playerHealth != null) playerHealth.Health = Math.Min(playerHealth.Health + health, playerHealth.MaxHealth);
                }

            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (byPlayer == null) return null;

            Combat combat = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("combat") as Combat;
            if (combat == null) return null;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[combat.Id];
            if (playerSkill == null) return null;
            PlayerAbility ability = playerSkill[combat.LooterId];
            if (ability.Tier <= 0) return null;

            float dropQuantityMultiplier = ability.SkillDependentFValue();
            handling = EnumHandling.PreventSubsequent;

            //this is nearly a copy of the base game method
            if (entity.Properties.Drops == null) return null;
            List<ItemStack> todrop = new List<ItemStack>();

            float dropMul = 1 + dropQuantityMultiplier;

            if (entity.Properties.Attributes?["isMechanical"].AsBool() != true && byPlayer?.Entity != null)
            {
                dropMul += byPlayer.Entity.Stats.GetBlended("animalLootDropRate");
            }

            for (int i = 0; i < this.entity.Properties.Drops.Length; i++)
            {
                BlockDropItemStack bdStack = this.entity.Properties.Drops[i];

                float extraMul = 1f;
                if (bdStack.DropModbyStat != null && byPlayer?.Entity != null)
                {
                    // If the stat does not exist, then GetBlended returns 1 \o/
                    extraMul = byPlayer.Entity.Stats.GetBlended(bdStack.DropModbyStat);
                }

                ItemStack stack = bdStack.GetNextItemStack(dropMul * extraMul);
                if (stack == null) continue;

                if (stack.Collectible is IResolvableCollectible irc)
                {
                    var slot = new DummySlot(stack);
                    irc.Resolve(slot, world);
                    stack = slot.Itemstack;
                }

                todrop.Add(stack);
                if (bdStack.LastDrop) break;
            }

            return todrop.ToArray();
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            IPlayer player = (entity.Api as ICoreClientAPI)?.World.Player as IPlayer;
            if (player == null) return;

            Combat combat = XLeveling.Instance(player.Entity.Api)?.GetSkill("combat") as Combat;
            if (combat == null) return;
            PlayerSkill playerSkill = player.Entity.GetBehavior<PlayerSkillSet>()?[combat.Id];
            if (playerSkill == null) return;
            PlayerAbility ability = playerSkill?[combat.MonsterExpertId];
            if (ability.Tier <= 0) return;

            ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
            if (healthTree == null) return;
            infotext.AppendLine(Lang.Get("Health: {0:0.##}/{1:0.##}", healthTree.GetFloat("currenthealth"), healthTree.GetFloat("maxhealth")));
        }
    }//!class XSkillsEntityBehavior
}//!namespace XSkills
