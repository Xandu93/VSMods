using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the InventorySmelting class.
    /// </summary>
    [HarmonyPatch(typeof(InventorySmelting))]
    public class InventorySmeltingPatch
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
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return false;
            if (original == null) return true;

            return
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.DesalinateId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled;
        }

        /// <summary>
        /// Harmony postfix for the GetBestSuitedSlot method.
        /// Should set the owner when the player shifts click an
        /// item into the inventory.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        /// <param name="sourceSlot">The source slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetBestSuitedSlot")]
        public static void GetBestSuitedSlotPostfix(InventorySmelting __instance, WeightedSlot __result, ItemSlot sourceSlot)
        {
            if (__result == null) return;
            IPlayer player = (sourceSlot.Inventory as InventoryBasePlayer)?.Player;
            if (player == null) return;

            BlockEntity blockEntity = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = player;
        }

        /// <summary>
        /// Harmony prefix for NewSlot method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch("NewSlot")]
        public static bool NewSlotPrefix(InventorySmelting __instance, out ItemSlot __result, int i)
        {
            __result = null;
            if (i >= 3)
            {
                __result = new ItemSlotCooking(__instance);
                return false;
            }
            if (i == 1)
            {
                __result = new InputSlot(__instance);
                return false;
            }
            return true;
        }
    }//!class InventorySmeltingPatch

    [HarmonyPatch(typeof(InventoryOven))]
    public class InventoryOvenPatch
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
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return false;
            if (original == null) return true;

            return
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.DesalinateId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled;
        }

        /// <summary>
        /// Harmony postfix for the GetBestSuitedSlot method.
        /// Should set the owner when the player shifts click an
        /// item into the inventory.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        /// <param name="sourceSlot">The source slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetSuitability")]
        public static void GetSuitabilityPostfix(InventorySmelting __instance, float __result, ItemSlot sourceSlot)
        {
            if (__result == 0.0f) return;
            IPlayer player = (sourceSlot.Inventory as InventoryBasePlayer)?.Player;
            if (player == null) return;

            BlockEntity blockEntity = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = player;
        }

        /// <summary>
        /// Harmony prefix for NewSlot method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        /// <param name="i">The i.</param>
        /// <returns></returns>
        [HarmonyPrefix]
        [HarmonyPatch("NewSlot")]
        public static bool NewSlotPrefix(InventoryOven __instance, out ItemSlot __result)
        {
            __result = new ItemSlotOven(__instance);
            return true;
        }
    }//!class InventoryOvenPatch
}//!namespace XSkills