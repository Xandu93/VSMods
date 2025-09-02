using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockCharcoalPile class.
    /// </summary>
    [HarmonyPatch(typeof(BlockCharcoalPile))]
    public class BlockCharcoalPilePatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            XSkills xSkills = XSkills.Instance;
            if (xSkills == null) return false;
            Skill skill;
            xSkills.Skills.TryGetValue("forestry", out skill);
            Forestry forestry = skill as Forestry;

            if (!(forestry?.Enabled ?? false)) return false;
            if (original == null) return true;

            return forestry[forestry.StokerId]?.Enabled ?? false;
        }

        /// <summary>
        /// Postfix for the OnBlockBroken method.
        /// May break additional layers.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        /// <param name="byPlayer">The player.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnBlockBroken")]
        public static void OnBlockBrokenPostfix(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            Block block = world.BlockAccessor.GetBlock(pos);
            if (block.Id == 0) return;

            Forestry forestry = world.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("forestry") as Forestry;
            if (forestry == null) return;
            PlayerSkill playerSkill = byPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[forestry.Id];
            if (playerSkill == null) return;
            PlayerAbility ability = playerSkill[forestry.StokerId];
            if (ability == null) return;

            if(ability.FValue(0) >= world.Rand.NextDouble())
            {
                world.BlockAccessor.BreakBlock(pos, byPlayer, dropQuantityMultiplier);
            }
        }
    }
}
