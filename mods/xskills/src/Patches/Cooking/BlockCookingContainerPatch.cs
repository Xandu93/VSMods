using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockCookingContainer class.
    /// Most methods here manipulate the MaxServingSize field.
    /// Since the GetMatchingCookingRecipe method can not be 
    /// patched due to the lack of access to the player all methods 
    /// that use GetMatchingCookingRecipe must be patched.
    /// </summary>
    [HarmonyPatch(typeof(BlockCookingContainer))]
    public class BlockCookingContainerPatch
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

            switch (original.Name)  
            {
                case "CanSmelt":
                    return 
                        cooking[cooking.DesalinateId].Enabled ||
                        cooking[cooking.CanteenCookId].Enabled;
                default:
                    return 
                        cooking[cooking.CanteenCookId].Enabled;
            }
        }

        /// <summary>
        /// Prefix for the OnHeldInteractStop method.
        /// Replaces the vanilla method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">if set to <c>true</c> the item can be smelted.</param>
        /// <param name="world">The world.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <returns>
        ///   <c>true</c> if the vanilla method should run; otherwise, <c>false</c>.
        /// </returns>
        [HarmonyPrefix]
        [HarmonyPatch("CanSmelt")]
        public static bool CanSmeltPrefix(BlockCookingContainer __instance, out bool __result, IWorldAccessor world, ISlotProvider cookingSlotsProvider)
        {
            __result = false;
            BlockEntityBehaviorOwnable ownable = CookingUtil.GetOwnableFromInventory(cookingSlotsProvider as InventoryBase);
            if (ownable == null) return true;
            IPlayer player = ownable.Owner;
            if (player == null) return true;

            Cooking cooking = world.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            PlayerSkill playerSkill;
            if (cooking == null) return true;

            try
            {
                //for some reason GetBehavior can throw an exception here
                playerSkill = player.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id];
            }
            catch (NullReferenceException)
            {
                ownable.OwnerString = null;
                ownable.Owner = null;
                return true;
            }

            if (playerSkill == null) return true;

            //canteen cook
            int maxServingSize = __instance.MaxServingSize;
            PlayerAbility ability = playerSkill[cooking.CanteenCookId];
            if (ability != null) __instance.MaxServingSize = (int)(maxServingSize * ( 1.0f + ability.FValue(0)));

            ItemStack[] stacks = __instance.GetCookingStacks(cookingSlotsProvider, false);
            CookingRecipe recipe = __instance.GetMatchingCookingRecipe(world, stacks, out _);
            __instance.MaxServingSize = maxServingSize;

            //desalinate
            if (recipe != null)
            {
                if (recipe.Code == "salt" || recipe.Code == "lime")
                {
                    PlayerAbility playerAbility = playerSkill[cooking.DesalinateId];
                    if (playerAbility == null || playerAbility.Tier <= 0) return false;
                }
                __result = true;
            }
            return false;
        }

        /// <summary>
        /// Prefix for the DoSmelt method.
        /// Saves the old value of the MaxServingSize value 
        /// to reset it later. 
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        [HarmonyPrefix]
        [HarmonyPatch("DoSmelt")]
        public static void DoSmeltPrefix(BlockCookingContainer __instance, out int __state, ISlotProvider cookingSlotsProvider)
        {
            __state = CookingUtil.SetMaxServingSize(__instance, cookingSlotsProvider);
        }

        /// <summary>
        /// Postfix for the DoSmelt method.
        /// Applies cooking abilities to the cooked item.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <param name="outputSlot">The output slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("DoSmelt")]
        public static void DoSmeltPostfix(BlockCookingContainer __instance, int __state, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            __instance.MaxServingSize = __state; 
            IPlayer player = CookingUtil.GetOwnerFromInventory(cookingSlotsProvider as InventoryBase);
            if (player?.Entity == null) return;

            Cooking cooking = player.Entity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            if (outputSlot?.Itemstack != null) 
                cooking.ApplyAbilities(outputSlot, player, 0.0f);
            else if (cookingSlotsProvider?.Slots?[0].Itemstack != null) 
                cooking.ApplyAbilities(cookingSlotsProvider.Slots[0], player, 0.0f, cookingSlotsProvider.Slots[0]?.StackSize ?? 1.0f);
        }

        /// <summary>
        /// Prefix for the GetOutputText method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        [HarmonyPrefix]
        [HarmonyPatch("GetOutputText")]
        public static void GetOutputTextPrefix(BlockCookingContainer __instance, out int __state, ISlotProvider cookingSlotsProvider)
        {
            __state = CookingUtil.SetMaxServingSize(__instance, cookingSlotsProvider);
        }

        /// <summary>
        /// Postfix for the GetOutputText method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetOutputText")]
        public static void GetOutputTextPostfix(BlockCookingContainer __instance, int __state)
        {
            __instance.MaxServingSize = __state;
        }
    }//!BlockCookingContainerPatch
}//!namespace XSkills
