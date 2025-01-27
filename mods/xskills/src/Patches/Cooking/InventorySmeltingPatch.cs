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
    public static class InventorySmeltingPatch
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

        [HarmonyPostfix]
        [HarmonyPatch("LateInitialize")]
        public static void LateInitializePostfix(InventorySmelting __instance)
        {
            __instance.OnInventoryOpened += __instance.OnInvOpened;
            //__instance.SlotModified += __instance.OnSlotModified;
        }

        //private static void SetSlotStackSize(InventorySmelting inventory, IPlayer player, ICoreAPI api)
        //{
        //    ////canteen cook
        //    if (!inventory.HaveCookingContainer) return;
        //    Cooking cooking = api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
        //    if (cooking == null) return;
        //    PlayerAbility ability = player.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id]?[cooking.CanteenCookId];
        //    if (ability == null) return;
        //    int size = inventory[1]?.Itemstack.ItemAttributes["maxContainerSlotStackSize"].AsInt(0) ?? 0;
        //    if (size == 0) return;
        //    size *= (int)(1.0f + ability.FValue(0));
        //    foreach (ItemSlot slot in inventory.CookingSlots)
        //    {
        //        slot.MaxSlotStackSize = size;
        //        if (slot is ItemSlotWatertight watertight) watertight.capacityLitres = size;
        //    }
        //}

        //public static void OnSlotModified(this InventorySmelting inventory, int id)
        //{
        //    if (id != 1) return;

        //    BlockEntity blockEntity = inventory.Api.World.BlockAccessor.GetBlockEntity(inventory.Pos);
        //    BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
        //    if (ownable.Owner != null) SetSlotStackSize(inventory, ownable.Owner, inventory.Api);
        //}

        public static void OnInvOpened(this InventorySmelting inventory, IPlayer player)
        {
            if (player == null) return;
            ICoreAPI api = inventory?.Api;
            if (api == null) return;

            BlockEntity blockEntity = inventory.Api.World.BlockAccessor.GetBlockEntity(inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = player;
            //SetSlotStackSize(inventory, player, api);
        }

        /// <summary>
        /// Harmony postfix for the GetBestSuitedSlot method.
        /// Should set the owner when the player shifts click an
        /// item into the inventory.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        /// <param name="sourceSlot">The source slot.</param>
        //[HarmonyPostfix]
        //[HarmonyPatch("GetBestSuitedSlot")]
        //public static void GetBestSuitedSlotPostfix(InventorySmelting __instance, WeightedSlot __result, ItemSlot sourceSlot)
        //{
        //    if (__result == null) return;
        //    IPlayer player = (sourceSlot.Inventory as InventoryBasePlayer)?.Player;
        //    if (player == null) return;

        //    BlockEntity blockEntity = __instance.Api.World.BlockAccessor.GetBlockEntity(__instance.Pos);
        //    BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
        //    if (ownable == null) return;
        //    ownable.Owner = player;
        //}

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