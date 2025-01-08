using ACulinaryArtillery;
using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the InventoryMixingBowl class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class InventoryMixingBowlPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony lib.</param>
        /// <param name="type">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type type, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return;
            Type patch = typeof(InventoryMixingBowlPatch);

            if (
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.DesalinateId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled)
            {
                PatchMethod(harmony, type, patch, "NewSlot");
            }
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
            if (i == 1)
            {
                __result = new ItemSlotCooking(__instance);
                return false;
            }
            return true;
        }
    }//!class InventoryMixingBowlPatch

    /// <summary>
    /// The patch for the ItemSlotMixingBowl class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class ItemSlotMixingBowlPatch : ManualPatch
    {
        public static int ContainerMaxSlotStackSize = 6;

        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony lib.</param>
        /// <param name="type">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type type, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return;
            Type patch = typeof(ItemSlotMixingBowlPatch);

            if (
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.DesalinateId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled)
            {
                PatchMethod(harmony, type, patch, "CanTakeFrom");
                PatchMethod(harmony, type, patch, "ActivateSlotLeftClick", "ActivateSlotPrefix", null);
                PatchMethod(harmony, type, patch, "ActivateSlotRightClick", "ActivateSlotPrefix", null);
            }
        }

        /// <summary>
        /// Harmony postfix for ActivateSlot method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="sourceSlot">The source slot.</param>
        public static void ActivateSlotPrefix(ItemSlotMixingBowl __instance, ref ItemStackMoveOperation op)
        {
            BlockEntity blockEntity = __instance.Inventory?.Api.World.BlockAccessor.GetBlockEntity(__instance.Inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (op.ActingPlayer != null && ownable != null) ownable.Owner = op.ActingPlayer;

            //canteen cook
            Cooking cooking = op.ActingPlayer.Entity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;

            PlayerAbility ability = op.ActingPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills?[cooking.Id]?[cooking.CanteenCookId];
            if (ability == null) return;

            int stackSize = ContainerMaxSlotStackSize + (int)(ContainerMaxSlotStackSize * ability.FValue(0));
            __instance.MaxSlotStackSize = stackSize;
        }

        /// <summary>
        /// Harmony postfix for CanTakeFrom method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="sourceSlot">The source slot.</param>
        public static void CanTakeFromPostfix(ItemSlotMixingBowl __instance, bool __result, ItemSlot sourceSlot)
        {
            if (!__result) return;
            IPlayer player = (sourceSlot.Inventory as InventoryBasePlayer)?.Player;
            if (player == null) return;

            BlockEntity blockEntity = __instance.Inventory.Api.World.BlockAccessor.GetBlockEntity(__instance.Inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = player;

            //canteen cook
            Cooking cooking = player.Entity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;

            PlayerAbility ability = player.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills?[cooking.Id]?[cooking.CanteenCookId];
            if (ability == null) return;

            int stackSize = ContainerMaxSlotStackSize + (int)(ContainerMaxSlotStackSize * ability.FValue(0));
            __instance.MaxSlotStackSize = stackSize;
        }
    }//!class ItemSlotMixingBowlPatch
}//!namespace XSkills
