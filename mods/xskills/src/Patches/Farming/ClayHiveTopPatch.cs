using HarmonyLib;
using System;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the ClayHiveTop class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class ClayHiveTopPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony lib.</param>
        /// <param name="type">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type type, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("farming", out skill);
            Farming farming = skill as Farming;

            if (!(farming?.Enabled ?? false)) return;
            Type patch = typeof(ClayHiveTopPatch);

            if (
                farming[farming.BeekeeperId].Enabled)
            {
                PatchMethod(harmony, type, patch, "OnBlockInteractStart");
            }
        }

        /// <summary>
        /// Harmony prefix for OnBlockInteractStart method.
        /// </summary>
        /// <returns></returns>
        public static void OnBlockInteractStartPrefix(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (world.Api.Side != EnumAppSide.Server) return;

            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            if (block.Variant["type"] != "harvestable") return;

            EnumTool? tool = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Item?.Tool;
            if (tool == null) return;
            if (tool.Value != EnumTool.Knife) return;

            Farming farming = XLeveling.Instance(world.Api)?.SkillSetTemplate.FindSkill("farming") as Farming;
            if (farming == null) return;

            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[farming.Id];
            if (playerSkill == null) return;

            XSkillsSkepBehavior beh = world.GetBlock(new AssetLocation("game", "skep-populated-east"))?.GetBehavior<XSkillsSkepBehavior>();
            if (beh != null) playerSkill.AddExperience(beh.xp * 0.20f);

            //beekeeper
            PlayerAbility playerAbility = playerSkill[farming.BeekeeperId];
            if (playerAbility == null) return;

            if (playerAbility.Tier > 0)
            {
                world.SpawnItemEntity(new ItemStack(world.GetItem(new AssetLocation("game", "honeycomb")), playerAbility.Value(0)), byPlayer.Entity.Pos.XYZ.AddCopy(0.5, 0.5, 0.5));
            }
        }
    }//!ClayHiveTopPatch
}//!namespace XSkills
