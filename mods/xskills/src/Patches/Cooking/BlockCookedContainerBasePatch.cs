using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockContainer))]
    public class BlockContainerPatch
    {
        /// <summary>
        /// Prefix for the SetRecipeCode method.
        /// Removes the quality attribute for empty containers.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("SetContents")]
        public static void SetContentsPrefix(ItemStack containerStack, ItemStack[] stacks)
        {
            if (containerStack.Collectible is not BlockCookedContainerBase) return;
            if (stacks == null || stacks.Length == 0)
            {
                containerStack.Attributes.RemoveAttribute("quality");
            }
        }
    }

    /// <summary>
    /// The patch for the BlockCookedContainerBase class.
    /// Mainly to make sure that the quality value is transferred.
    /// </summary>
    [HarmonyPatch(typeof(BlockCookedContainerBase))]
    public class BlockCookedContainerBasePatch
    {
        /// <summary>
        /// Prefix for the ServeIntoBowl method.
        /// Saves the quality state.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="pos">The position.</param>
        /// <param name="potslot">The potslot.</param>
        /// <param name="world">The world.</param>
        [HarmonyPrefix]
        [HarmonyPatch("ServeIntoBowl")]
        public static void ServeIntoBowlPrefix(out QualityState __state, BlockPos pos, ItemSlot potslot, IWorldAccessor world)
        {
            __state = new QualityState();
            if (world.Side == EnumAppSide.Client) return;

            IBlockEntityMealContainer bemeal = world.BlockAccessor.GetBlockEntity(pos) as IBlockEntityMealContainer;
            if (bemeal == null) return;

            __state.quality = potslot.Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
            __state.oldQuality = bemeal.inventory[0].Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
            __state.oldQuantity = bemeal.QuantityServings;
        }

        /// <summary>
        /// Postfix for the ServeIntoBowl method.
        /// Transfers the quality properties.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="pos">The position.</param>
        /// <param name="world">The world.</param>
        [HarmonyPostfix]
        [HarmonyPatch("ServeIntoBowl")]
        public static void ServeIntoBowlPostfix(QualityState __state, BlockPos pos, IWorldAccessor world)
        {
            if (world.Side == EnumAppSide.Client) return;
            IBlockEntityMealContainer bemeal = world.BlockAccessor.GetBlockEntity(pos) as IBlockEntityMealContainer;
            if (bemeal == null) return;

            float transferred = bemeal.QuantityServings - __state.oldQuantity;
            if (transferred <= 0.0f) return;
            float newQuality = (__state.oldQuality * __state.oldQuantity + __state.quality * transferred) / (__state.oldQuantity + transferred);
            if (newQuality <= 0.0f) return;
            bemeal.inventory[0].Itemstack?.Attributes.SetFloat("quality", newQuality);
        }

        /// <summary>
        /// Prefix for the ServeIntoStack method.
        /// Saves the quality state.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="bowlSlot">The bowl slot.</param>
        /// <param name="potslot">The potslot.</param>
        /// <param name="world">The world.</param>
        [HarmonyPrefix]
        [HarmonyPatch("ServeIntoStack")]
        public static void ServeIntoStackPrefix(out QualityState __state, ItemSlot bowlSlot, ItemSlot potslot, IWorldAccessor world)
        {
            __state = new QualityState();
            if (world.Side == EnumAppSide.Client) return;
            if (bowlSlot?.Itemstack == null || potslot?.Itemstack == null) return;

            __state.quality = potslot.Itemstack.Attributes.GetFloat("quality");
            __state.oldQuality = bowlSlot.Itemstack.Attributes.GetFloat("quality");
            __state.oldQuantity = (bowlSlot.Itemstack.Collectible as IBlockMealContainer)?.GetQuantityServings(world, bowlSlot.Itemstack) ?? 0;
        }

        /// <summary>
        /// Postfix for the ServeIntoStack method.
        /// Transfers the quality properties.
        /// </summary>
        /// <param name="__state">The state.</param>
        /// <param name="bowlSlot">The bowl slot.</param>
        /// <param name="potslot">The potslot.</param>
        /// <param name="world">The world.</param>
        [HarmonyPostfix]
        [HarmonyPatch("ServeIntoStack")]
        public static void ServeIntoStackPostfix(QualityState __state, ItemSlot bowlSlot, ItemSlot potslot, IWorldAccessor world)
        {
            if (world.Side == EnumAppSide.Client) return;
            if (bowlSlot?.Itemstack == null || potslot?.Itemstack == null) return;

            IBlockMealContainer bemeal = bowlSlot.Itemstack.Collectible as IBlockMealContainer;
            if (bemeal == null) return;

            float transferred = bemeal.GetQuantityServings(world, bowlSlot.Itemstack) - __state.oldQuantity;
            if (transferred <= 0.0f) return;
            float newQuality = (__state.oldQuality * __state.oldQuantity + __state.quality * transferred) / (__state.oldQuantity + transferred);
            if (newQuality <= 0.0f) return;
            bowlSlot.Itemstack.Attributes.SetFloat("quality", newQuality);
        }

        /// <summary>
        /// Postfix for the GetContainedInfo method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="inSlot">The in slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetContainedInfo")]
        public static void GetContainedInfoPostfix(ref string __result, ItemSlot inSlot)
        {
            float quality = QualityUtil.GetQuality(inSlot);
            if (quality <= 0.0f) return;
            __result += ", " + QualityUtil.QualityString(quality, false);
        }
    }//!class BlockCookedContainerBasePatch
}//!namespace XSkills
