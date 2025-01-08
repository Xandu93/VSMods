using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityPie class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityPie))]
    public class BlockEntityPiePatch
    {
        /// <summary>
        /// Postfix for the TakeSlice method.
        /// Carries over quality.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        [HarmonyPostfix]
        [HarmonyPatch("TakeSlice")]
        public static void TakeSlicePostfix(BlockEntityPie __instance, ItemStack __result)
        {
            float quality = __instance.Inventory[0]?.Itemstack?.Attributes?.GetFloat("quality") ?? 0;
            if (quality <= 0.0f) return;
            __result?.Attributes?.SetFloat("quality", quality);
        }
    }//!class BlockEntityPiePatch
}//!namespace XSkills
