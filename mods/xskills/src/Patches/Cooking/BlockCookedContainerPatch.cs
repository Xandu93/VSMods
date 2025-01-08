using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockCookedContainer class.
    /// </summary>
    [HarmonyPatch(typeof(BlockCookedContainer))]
    public class BlockCookedContainerPatch
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
            QualityUtil.AddQualityString(inSlot, dsc);
        }

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
    }//!class BlockCookedContainerPatch
}//!namespace XSkills
