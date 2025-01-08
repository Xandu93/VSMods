using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockSaucepan class.
    /// </summary>
    internal class BlockSaucepanPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony.</param>
        /// <param name="type">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type type, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return;
            Type patch = typeof(BlockSaucepanPatch);

            if (
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled)
            {
                PatchMethod(harmony, type, patch, "DoSmelt");
            }
        }

        /// <summary>
        /// Prefix for the DoSmelt method.
        /// </summary>
        /// <param name="__state">The input stacks.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        public static void DoSmeltPrefix(out DoSmeltState __state, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            InventoryBase inv = cookingSlotsProvider as InventoryBase;
            List<ItemStack> stacks = new List<ItemStack>();
            __state = new DoSmeltState();
            if (inv == null) return;

            __state.quality = inv[2].Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;

            for (int ii = 3; ii <= 6; ++ii)
            {
                if (!inv[ii].Empty) stacks.Add(inv[ii].Itemstack);
            }
            if (stacks.Count > 0) __state.stacks = stacks.ToArray();
            __state.stackSize = outputSlot.StackSize;
        }

        /// <summary>
        /// Postfix for the DoSmelt method.
        /// </summary>
        /// <param name="__state">The input stacks.</param>
        /// <param name="world">The world.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <param name="outputSlot">The output slot.</param>
        public static void DoSmeltPostfix(DoSmeltState __state, IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            InventoryBase inv = cookingSlotsProvider as InventoryBase;
            if (inv == null) return;
            BlockEntity blockEntity = world?.BlockAccessor.GetBlockEntity(inv.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();

            float exp = 1.0f;
            if (!(outputSlot?.Itemstack?.Collectible is BlockLiquidContainerBase))
            {
                exp = 0.25f;
            }

            Cooking cooking = blockEntity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            cooking.ApplyAbilities(outputSlot, ownable.Owner, __state.quality, outputSlot.StackSize - __state.stackSize, __state.stacks, exp);
        }
    }//!class BlockSaucepanPatch

    public class DoSmeltState : CookingState
    {
        public int stackSize;
    }//!class DoSmeltState
}//!namespace XSkills
