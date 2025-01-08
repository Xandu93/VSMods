using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityCookedContainer class.
    /// Don't know if this is still required.
    /// Will be kept here for legacy reasons.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityCookedContainer))]
    public class BlockEntityCookedContainerPatch
    {
        /// <summary>
        /// Postfix for the constructor.
        /// Increases inventory size.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="___inventory">The inventory.</param>
        [HarmonyPatch(MethodType.Constructor)]
        public static void Postfix(ref InventoryGeneric ___inventory)
        {
            int size = ___inventory != null ? ___inventory.Count + 1 : 7;
            ___inventory = new InventoryGeneric(size, null, null);
        }

        /// <summary>
        /// Postfix for the OnBlockPlaced method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="byItemStack">The by item stack.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnBlockPlaced")]
        public static void OnBlockPlacedPostfix(BlockEntityCookedContainer __instance, ItemStack byItemStack)
        {
            BlockCookedContainer blockpot = byItemStack?.Block as BlockCookedContainer;
            if (blockpot == null) return;
            float quality = byItemStack.Attributes.GetFloat("quality", 0.0f);
            if (quality <= 0.0f) return;
            __instance.Inventory[0]?.Itemstack?.Attributes.SetFloat("quality", quality);
        }

        /// <summary>
        /// Postfix for the GetBlockInfo method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="dsc">The DSC.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(BlockEntityCookedContainer __instance, StringBuilder dsc)
        {
            QualityUtil.AddQualityString(__instance.Inventory?[0], dsc);
        }
    }//!class BlockEntityCookedContainerPatch
}//!namespace XSkills
