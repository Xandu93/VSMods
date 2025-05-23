﻿using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockCrock class.
    /// </summary>
    [HarmonyPatch(typeof(BlockCrock))]
    public class BlockCrockPatch
    {
        /// <summary>
        /// Postfix for the OnPickBlock method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnPickBlock")]
        public static void OnPickBlockPostfix(ItemStack __result, IWorldAccessor world, BlockPos pos)
        {
            QualityUtil.PickQuality(__result, world, pos);
        }

        /// <summary>
        /// Postfix for the GetPlacedBlockInfo method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetPlacedBlockInfo")]
        public static void GetPlacedBlockInfoPostfix(ref string __result, IWorldAccessor world, BlockPos pos)
        {
            float quality = QualityUtil.GetQuality(world, pos);
            if (quality <= 0.0f) return;
            __result += QualityUtil.QualityString(quality);
        }

        /// <summary>
        /// Postfix for the OnCreatedByCrafting method.
        /// Sealing crocks reduces quality by 20%.
        /// </summary>
        /// <param name="allInputslots">All inputslots.</param>
        /// <param name="outputSlot">The output slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnCreatedByCrafting")]
        public static void OnCreatedByCraftingPostfix(ItemSlot[] allInputslots, ItemSlot outputSlot)
        {
            for (int i = 0; i < allInputslots.Length; i++)
            {
                ItemSlot slot = allInputslots[i];
                if (slot.Itemstack?.Collectible is BlockCrock)
                {
                    float quality = QualityUtil.GetQuality(slot);
                    if (quality > 0.0f) outputSlot.Itemstack.Attributes.SetFloat("quality", quality * 0.8f);
                    return;
                }
            }
        }
    }//!class BlockCrockPatch
}//!namespace XSkills
