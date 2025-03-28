using HarmonyLib;
using System;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityFruitPress class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityFruitPress))]
    public class BlockEntityFruitPressPatch
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
            return cooking[cooking.JuicerId].Enabled;
        }

        /// <summary>
        /// Postfix for the OnBlockInteractStart method.
        /// Sets the owner.
        /// </summary>
        /// <param name="byPlayer">The player</param>
        /// <param name="__instance">The instance</param>
        /// <param name="__result">The result</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnBlockInteractStart")]
        public static void OnBlockInteractStartPostfix(BlockEntityFruitPress __instance, bool __result, IPlayer byPlayer)
        {
            if (__result == false) return;
            BlockEntityBehaviorOwnable ownable = __instance?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = byPlayer;
        }

        /// <summary>
        /// Gets the litres in the bucket if a bucket exists.
        /// </summary>
        /// <param name="press"></param>
        /// <returns>the current litres.</returns>
        protected static float CurrentLitres(BlockEntityFruitPress press)
        {
            BlockLiquidContainerBase container = press.BucketSlot?.Itemstack?.Collectible as BlockLiquidContainerBase;
            if (container == null) return 0.0f;
            return container.GetCurrentLitres(press.BucketSlot.Itemstack);
        }

        /// <summary>
        /// Prefix for the onTick100msServer method.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("onTick100msServer")]
        public static void onTick100msServerPrefix(BlockEntityFruitPress __instance, ref float __state)
        {
            __state = CurrentLitres(__instance);
        }

        /// <summary>
        /// Postfix for the onTick100msServer method.
        /// For experience gain.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("onTick100msServer")]
        public static void onTick100msServerPostfix(BlockEntityFruitPress __instance, float __state)
        {
            float litres = CurrentLitres(__instance);
            if (litres <= __state) return;

            BlockEntityBehaviorOwnable ownable = __instance?.GetBehavior<BlockEntityBehaviorOwnable>();
            IPlayer player = ownable?.Owner;
            if (player == null) return;
            Cooking cooking = XLeveling.Instance(__instance.Api)?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            PlayerSkill skill = player.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id];
            if (skill == null) return;
            float exp = (cooking.Config as CookingSkillConfig).fruitPressExpPerLitre;

            float diff = litres - __state;
            skill.AddExperience(diff * exp);
        }

        /// <summary>
        /// Postfix for the getJuiceableProps method.
        /// Manipulates the amount of liters you get from one fruit item.
        /// </summary>
        /// <param name="__instance">The instance</param>
        /// <param name="__result">The result</param>
        [HarmonyPostfix]
        [HarmonyPatch("getJuiceableProps")]
        public static void getJuiceablePropsPostfix(BlockEntityFruitPress __instance, JuiceableProperties __result, ItemStack stack)
        {
            if (__result?.LitresPerItem == null) return;
            BlockEntityBehaviorOwnable ownable = __instance?.GetBehavior<BlockEntityBehaviorOwnable>();
            IPlayer player = ownable?.Owner;
            if (player == null) return;
            if (stack?.Collectible is ItemPressedMash) return;
            if (__instance.MashSlot.Itemstack == stack) return;

            Cooking cooking = XLeveling.Instance(__instance.Api)?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;

            PlayerAbility ability = player.Entity?.GetBehavior<PlayerSkillSet>()?[cooking.Id]?[cooking.JuicerId];
            if (ability == null) return;
            int value = ability.Value(0);
            if (value == 33) __result.LitresPerItem *= 1.0f + 1.0f / 3.0f;
            else if (value == 66) __result.LitresPerItem *= 2.0f - 1.0f / 3.0f;
            else __result.LitresPerItem *= 1.0f + value * 0.01f;

            ItemStack handStack = player.InventoryManager?.ActiveHotbarSlot?.Itemstack;
            if (handStack == null) return;
            int desiredTransferAmount = Math.Min(handStack.StackSize, player.Entity.Controls.ShiftKey ? 1 : player.Entity.Controls.CtrlKey ? handStack.Item.MaxStackSize : 4);

            double mashlitres = (__instance.MashSlot.Itemstack?.Attributes.GetDecimal("juiceableLitresLeft") ?? 0.0);
            if (mashlitres < 10.0 && mashlitres + __result.LitresPerItem * (desiredTransferAmount + 1) > 10.0)
            {
                while (mashlitres + __result.LitresPerItem * desiredTransferAmount > 10.0) desiredTransferAmount--;
                if (desiredTransferAmount == 0) desiredTransferAmount = 1;
                if (desiredTransferAmount > 0)
                {
                    __result.LitresPerItem = (float)((10.0 - mashlitres) / desiredTransferAmount);
                }
            }
        }
    }
}