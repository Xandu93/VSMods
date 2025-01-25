using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the ItemClay class.
    /// </summary>
    [HarmonyPatch(typeof(ItemClay))]
    public class ItemClayPatch
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
            xSkills.Skills.TryGetValue("pottery", out skill);
            Pottery pottery = skill as Pottery;

            if (!(pottery?.Enabled ?? false)) return false;
            if (original == null) return true;

            return pottery[pottery.ThriftId].Enabled;
        }

        /// <summary>
        /// Prefix for the OnHeldInteractStop method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="byEntity">The by entity.</param>
        /// <param name="blockSel">The block sel.</param>
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop")]
        public static void OnHeldInteractStopPrefix(ItemClay __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (blockSel == null) return;
            BlockEntityClayForm bea = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityClayForm;
            PotteryUtil.AddClay(bea, slot, byEntity);
        }
    }//!class ItemClayPatch
}//!namespace XSkills
