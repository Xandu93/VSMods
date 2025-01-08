using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityCrock class.
    /// Don't know if this is still required.
    /// Will be kept here for legacy reasons.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityCrock))]
    public class BlockEntityCrockPatch
    {
        /// <summary>
        /// Postfix for the constructor.
        /// Increases inventory size.
        /// </summary>
        /// <param name="___inventory">The inventory.</param>
        [HarmonyPatch(MethodType.Constructor)]
        public static void Postfix(ref InventoryGeneric ___inv)
        {
            int size = ___inv != null ? ___inv.Count + 1 : 7;
            ___inv = new InventoryGeneric(size, null, null);
        }
    }//!class BlockEntityCrockPatch
}//!namespace XSkills
