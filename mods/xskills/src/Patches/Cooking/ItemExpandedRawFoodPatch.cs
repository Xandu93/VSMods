using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the ItemExpandedRawFood class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    internal class ItemExpandedRawFoodPatch : ManualPatch
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
            Type patch = typeof(ItemExpandedRawFoodPatch);

            PatchMethod(harmony, type, patch, "GetHeldItemInfo");

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
        /// Postfix for the GetHeldItemInfo method.
        /// </summary>
        /// <param name="inSlot">The in slot.</param>
        /// <param name="dsc">The string builder.</param>
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc)
        {
            float quality = inSlot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            QualityUtil.AddQualityString(quality, dsc);
        }

        /// <summary>
        /// Prefix for the DoSmelt method.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="outputSlot">The output slot.</param>
        public static void DoSmeltPrefix(out DoSmeltState __state, ItemSlot outputSlot)
        {
            __state = new DoSmeltState();
            __state.stackSize = outputSlot.Itemstack?.StackSize ?? 0;
            __state.quality = outputSlot.Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
        }

        /// <summary>
        /// Postfix for the DoSmelt method.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="world">The world.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <param name="outputSlot">The output slot.</param>
        public static void DoSmeltPostfix(DoSmeltState __state, IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            InventoryBase inv = cookingSlotsProvider as InventoryBase;
            if (inv == null) return;
            BlockEntity blockEntity = world?.BlockAccessor.GetBlockEntity(inv.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();

            int cooked = (outputSlot.Itemstack?.StackSize ?? 0) - __state.stackSize;
            if (ownable?.Owner == null || cooked <= 0) return;
            CollectibleObjectPatch.DoSmeltCooking(ownable?.Owner, outputSlot, cooked, __state.quality);
        }
    }//!class ItemExpandedRawFoodPatch
}//!namespace XSkills
