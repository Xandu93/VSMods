using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockPie class.
    /// </summary>
    [HarmonyPatch(typeof(BlockPie))]
    public class BlockPiePatch
    {
        /// <summary>
        /// Postfix for the GetHeldItemInfo method.
        /// </summary>
        /// <param name="inSlot">The in slot.</param>
        /// <param name="dsc">The DSC.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo")]
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc)
        {
            float quality = inSlot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            QualityUtil.AddQualityString(quality, dsc);
        }

        /// <summary>
        /// Postfix for the GetPlacedBlockInfo method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetPlacedBlockInfo")]
        public static void Postfix(ref string __result, IWorldAccessor world, BlockPos pos)
        {
            BlockEntityPie bep = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPie;
            float quality = bep?.Inventory[0]?.Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
            if (quality <= 0.0f) return;
            __result += QualityUtil.QualityString(quality);
        }
    }//!class BlockPiePatch
}//!namespace XSkills
