using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockCookingContainer class.
    /// </summary>
    [HarmonyPatch(typeof(BlockWateringCan))]
    public class BlockWateringCanPatch
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
            xSkills.Skills.TryGetValue("farming", out skill);
            Farming farming = skill as Farming;

            if (!(farming?.Enabled ?? false)) return false;
            if (original == null) return true;

            switch (original.Name)
            {
                case "OnHeldInteractStep":
                    return
                        farming[farming.ExtensiveFarmingId].Enabled;
                default:
                    return
                        farming.Enabled;
            }
        }

        /// <summary>
        /// Postfix for the OnHeldInteractStep method.
        /// Applies watering to neighboring blocks.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("OnHeldInteractStep")]
        public static void Postfix(bool __result, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (!__result) return;

            IWorldAccessor world = byEntity.World;
            Block block = world.BlockAccessor.GetBlock(blockSel.Position);
            float prevsecondsused = slot.Itemstack.TempAttributes.GetFloat("secondsUsed");

            Farming farming = XLeveling.Instance(byEntity.Api)?.GetSkill("farming") as Farming;
            if (farming == null) return;

            PlayerAbility ability = byEntity.GetBehavior<PlayerSkillSet>()?[farming.Id]?[farming.ExtensiveFarmingId];
            int range = ability.Value(0);
            if (range == 0) return;

            int x = blockSel.Position.X;
            int y = blockSel.Position.Y;
            int z = blockSel.Position.Z;

            int directionX = 0;
            int directionZ = 0;

            if (range % 2 == 0)
            {
                if (x - byEntity.Pos.X >= 0.0f) directionX = 1;
                if (z - byEntity.Pos.Z >= 0.0f) directionZ = 1;
            }

            x = x - range / 2 + directionX;
            z = z - range / 2 + directionZ;

            for (int xx = x; xx < x + range; ++xx)
            {
                for (int zz = z; zz < z + range; ++zz)
                {
                    if (xx == blockSel.Position.X && zz == blockSel.Position.Z) continue;
                    BlockPos targetPos = new BlockPos(xx, y, zz, blockSel.Position.dimension);

                    if (block.CollisionBoxes == null || block.CollisionBoxes.Length == 0)
                    {
                        block = world.BlockAccessor.GetBlock(blockSel.Position, BlockLayersAccess.Fluid);
                        if ((block.CollisionBoxes == null || block.CollisionBoxes.Length == 0) && !block.IsLiquid())
                        {
                            targetPos = targetPos.DownCopy();
                        }
                    }

                    BlockEntityFarmland be = world.BlockAccessor.GetBlockEntity(targetPos) as BlockEntityFarmland;
                    if (be != null)
                    {
                        be.WaterFarmland(secondsUsed - prevsecondsused);
                    }
                }
            }
        }
    }//!BlockWateringCanPatch
}//!namespace XSkills
