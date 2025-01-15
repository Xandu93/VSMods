using HarmonyLib;
using System;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BEFrameRack class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class BEFrameRackPatch : ManualPatch
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
            Type patch = typeof(BEFrameRackPatch);

            if (
                farming[farming.BeekeeperId].Enabled)
            {
                PatchMethod(harmony, type, patch, "TryHarvest");
            }
        }

        /// <summary>
        /// Harmony prefix for TryHarvest method.
        /// </summary>
        /// <returns></returns>
        public static void TryHarvestPrefix(IWorldAccessor world, IPlayer player)
        {
            if (world.Api.Side != EnumAppSide.Server) return;
            Farming farming = XLeveling.Instance(world.Api)?.SkillSetTemplate.FindSkill("farming") as Farming;
            if (farming == null) return;

            PlayerSkill playerSkill = player?.Entity.GetBehavior<PlayerSkillSet>()?[farming.Id];
            if (playerSkill == null) return;

            XSkillsSkepBehavior beh = world.GetBlock(new AssetLocation("game", "skep-populated-east"))?.GetBehavior<XSkillsSkepBehavior>();
            if (beh != null) playerSkill.AddExperience(beh.xp * 0.20f);

            //beekeeper
            PlayerAbility playerAbility = playerSkill[farming.BeekeeperId];
            if (playerAbility == null) return;

            if (playerAbility.Tier > 0)
            {
                world.SpawnItemEntity(new ItemStack(world.GetItem(new AssetLocation("game", "honeycomb")), playerAbility.Value(0)), player.Entity.Pos.XYZ.AddCopy(0.5, 0.5, 0.5));
            }
        }
    }//!BEFrameRackPatch
}//!namespace XSkills
