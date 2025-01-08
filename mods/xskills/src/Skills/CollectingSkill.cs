using System;
using System.Collections.Generic;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    public class CollectingSkill : XSkill
    {
        //ability ids
        public int MiningSpeedId { get; protected set; }
        public int DurabilityId { get; protected set; }

        // tool used for some skills
        protected internal EnumTool Tool { get; set; }

        public CollectingSkill(string name) : base(name, "xskills:skill-" + name, "xskills:group-collecting")
        {
            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 200;
            this.ExpMult = 100.0f;
            this.ExpEquationValue = 8.0f;
        }
    }//!class CollectingSkill

    public abstract class DropBonusBehavior : BlockBehavior
    {
        protected float xp;
        public abstract CollectingSkill Skill { get; }
        public abstract EnumTool? Tool { get; }
        public abstract PlayerAbility DropBonusAbility(PlayerSkill playerSkill);

        public virtual float GetXP(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            return xp;
        }

        public DropBonusBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.xp = properties["xp"].AsFloat(0.0f);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (this.Skill == null || byPlayer == null) return;
            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[this.Skill.Id];
            if (playerSkill == null) return;

            BlockReinforcement reinforcment = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(true).GetReinforcment(pos);
            if (reinforcment?.Strength > 0) return;

            //experience
            playerSkill.AddExperience(this.GetXP(world, pos, byPlayer));
        }

        public virtual List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.Skill == null) return drops;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return drops;

            if (block.Drops.Length == 0) return drops;

            //associated ability
            float dropMultipier = dropChanceMultiplier;
            PlayerAbility playerAbility = this.DropBonusAbility(playerSkill);
            if (playerAbility == null) return drops;
            handling = EnumHandling.PreventDefault;
            dropMultipier += playerAbility.Ability.ValuesPerTier >= 3 ? 0.01f * playerAbility.SkillDependentValue() : 0.01f * playerAbility.Value(0);

            for (int index = 0; index < block.Drops.Length; index++)
            {
                ItemStack drop = block.Drops[index].GetNextItemStack(dropMultipier);
                if (drop != null) drops.Add(drop);
            }
            return drops;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            return this.GetDropsList(world, pos, byPlayer, dropChanceMultiplier, ref handling).ToArray();
        }
    }//!class DropBonusBehavior

    public abstract class CollectingBehavior : DropBonusBehavior
    {
        public CollectingBehavior(Block block) : base(block)
        { }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (this.Skill == null || byPlayer == null) return;
            base.OnBlockBroken(world, pos, byPlayer, ref handling);

            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return;

            //momentum
            AffectedEntityBehavior affected = byPlayer.Entity.GetBehavior<AffectedEntityBehavior>();
            Item tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Item;
            if (affected == null || tool == null) return;

            PlayerAbility playerAbility = playerSkill[this.Skill.MiningSpeedId];
            if (playerAbility == null || playerAbility.Tier <= 0) return;

            EnumTool toolType;
            if (Skill.Tool == tool.Tool)
            {
                toolType = Skill.Tool;
            }
            else if (tool.Code.Path.Contains("paxel"))
            {
                toolType = EnumTool.Shovel;
            }
            else return;

            float modifier = Math.Min(
                playerAbility.Value(0) * 0.01f +
                playerAbility.Value(1) * playerSkill.Level * 0.001f,
                playerAbility.Value(2) * 0.01f);

            XEffectsSystem effectSystem = this.Skill.XLeveling.Api.ModLoader.GetModSystem<XEffectsSystem>();
            MomentumEffect effect = effectSystem?.CreateEffect("momentum") as MomentumEffect;
            if (effect == null) return;

            effect.Duration = playerAbility.Value(4);
            effect.MaxStacks = playerAbility.Value(3);
            effect.Speed = modifier;
            effect.Tool = toolType;
            affected.AddEffect(effect);
        }
    }
}//!namespace XSkills
