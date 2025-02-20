using System;
using System.Collections.Generic;
using ACulinaryArtillery;
using HarmonyLib;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityMixingBowl class.
    /// Compatibility  for the ACulinaryArtillery mod.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class BlockEntityMixingBowlPatch : ManualPatch
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
            Type patch = typeof(BlockEntityMixingBowlPatch);

            if(cooking[cooking.CanteenCookId].Enabled)
            {
                PatchMethod(harmony, type, patch, "GetMatchingMixingRecipe");
            }
            if (
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled)
            {
                PatchMethod(harmony, type, patch, "mixInput");
            }

            InventoryMixingBowlPatch.Apply(harmony, typeof(InventoryMixingBowl), xSkills);
            ItemSlotMixingBowlPatch.Apply(harmony, typeof(ItemSlotMixingBowl), xSkills);
        }

        /// <summary>
        /// Prefix for the GetMatchingMixingRecipe method.
        /// Saves the old value of the MaxServingSize value 
        /// to reset it later and adjust it by the canteen cook ability.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        public static void GetMatchingMixingRecipePrefix(BlockEntityMixingBowl __instance, out int __state)
        {
            __state = __instance.Pot?.MaxServingSize ?? 0;
            IPlayer player = __instance.GetBehavior<BlockEntityBehaviorOwnable>()?.Owner;
            if (player?.Entity == null || __state == 0) return;

            Cooking cooking = __instance.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;

            //canteen cook
            PlayerSkill skill = player.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id];
            PlayerAbility ability = skill?[cooking.CanteenCookId];
            if (ability != null) __instance.Pot.MaxServingSize += (int)(__instance.Pot.MaxServingSize * ability.FValue(0));
        }

        /// <summary>
        /// Postfix for the GetMatchingMixingRecipe method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        public static void GetMatchingMixingRecipePostfix(BlockEntityMixingBowl __instance, int __state)
        {
            if(__instance.Pot != null) __instance.Pot.MaxServingSize = __state;
        }

        /// <summary>
        /// Prefix for the mixInpu method.
        /// </summary>
        /// <param name="__state">The input stacks.</param>
        /// <param name="__instance">The instance.</param>
        public static void mixInputPrefix(out CookingState __state, BlockEntityMixingBowl __instance)
        {
            InventoryBase inv = __instance.Inventory;
            List<ItemStack> stacks = new List<ItemStack>();
            __state = new CookingState();
            if (inv == null) return;

            __state.quality = inv[1].Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
            __state.outputStackSize = __instance.OutputStack?.StackSize ?? 0;

            for (int ii = 2; ii <= 7; ++ii)
            {
                if (!inv[ii].Empty) stacks.Add(inv[ii].Itemstack);
            }
            if (stacks.Count > 0) __state.stacks = stacks.ToArray();
        }

        /// <summary>
        /// Postfix for the mixInputPostfix method.
        /// </summary>
        /// <param name="__state">The input stacks.</param>
        /// <param name="__instance">The instance.</param>
        public static void mixInputPostfix(CookingState __state, BlockEntityMixingBowl __instance)
        {
            IPlayer byPlayer = __instance.GetBehavior<BlockEntityBehaviorOwnable>()?.Owner;
            if (byPlayer == null) return;

            Cooking cooking = byPlayer.Entity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            int cooked = (__instance.OutputStack?.StackSize ?? 0) - __state.outputStackSize;
            if (cooked <= 0) return;
            cooking.ApplyAbilities(__instance.OutputSlot, byPlayer, __state.quality, cooked, __state.stacks);
        }
    }//!class BlockEntityMixingBowlPatch
}//!namespace XSkills