using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockMeal class.
    /// </summary>
    [HarmonyPatch(typeof(BlockMeal))]
    public class BlockMealPatch
    {
        /// <summary>
        /// Postfix for the GetHeldItemInfo method.
        /// </summary>
        /// <param name="inSlot">The in slot.</param>
        /// <param name="dsc">The string builder.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetHeldItemInfo")]
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc)
        {
            float quality = inSlot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            QualityUtil.AddQualityString(quality, dsc);
        }

        /// <summary>
        /// Postfix for the OnPickBlock method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnPickBlock")]
        public static void OnPickBlockPostfix(ItemStack __result, IWorldAccessor world, BlockPos pos)
        {
            BlockEntityMeal bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityMeal;
            if (__result == null || bec == null) return;

            float quality = bec.Inventory[0]?.Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
            if (quality < 0.0f) return;
            __result.Attributes.SetFloat("quality", quality);
        }

        /// <summary>
        /// Prefix for the tryFinishEatMeal method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="byEntity">The by entity.</param>
        [HarmonyPrefix]
        [HarmonyPatch("tryFinishEatMeal")]
        internal static void tryFinishEatMealPrefix(BlockMeal __instance, out TryFinishEatMealState __state, ItemSlot slot, EntityAgent byEntity)
        {
            __state = new TryFinishEatMealState();
            __state.quality = slot.Itemstack?.Attributes?.GetFloat("quality") ?? 0.0f;
            __state.quantity = __instance.GetQuantityServings(byEntity.World, slot.Itemstack);
            __state.temperature = slot?.Itemstack?.Collectible?.GetTemperature(byEntity.World, slot.Itemstack) ?? 0.0f;
            __state.food0 = EnumFoodCategory.Unknown;
            __state.food1 = EnumFoodCategory.Unknown;
            FoodNutritionProperties[] nutritionProps = __instance.GetContentNutritionProperties(byEntity.World, slot, byEntity);
            float[] saturations = new float[(int)EnumFoodCategory.Unknown];
            float satFood0 = 0.0f;
            float satFood1 = 0.0f;

            for (int ii = 0; ii < nutritionProps.Length; ++ii)
            {
                if (nutritionProps[ii].FoodCategory >= EnumFoodCategory.Unknown) continue;
                if (nutritionProps[ii].FoodCategory <= EnumFoodCategory.NoNutrition) continue;
                saturations[(int)nutritionProps[ii].FoodCategory] += nutritionProps[ii].Satiety;
            }
            for (int ii = 0; ii < saturations.Length; ++ii)
            {
                if (saturations[ii] > satFood0)
                {
                    satFood1 = satFood0;
                    __state.food1 = __state.food0;
                    satFood0 = saturations[ii];
                    __state.food0 = (EnumFoodCategory)ii;
                }
                else if (saturations[ii] > satFood1)
                {
                    satFood1 = saturations[ii];
                    __state.food1 = (EnumFoodCategory)ii;
                }
            }
        }

        /// <summary>
        /// Postfix for the tryFinishEatMeal method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="byEntity">The by entity.</param>
        [HarmonyPostfix]
        [HarmonyPatch("tryFinishEatMeal")]
        internal static void tryFinishEatMealPostfix(BlockMeal __instance, TryFinishEatMealState __state, ItemSlot slot, EntityAgent byEntity)
        {
            float servings = slot.Itemstack != null ? __instance.GetQuantityServings(byEntity.World, slot.Itemstack) : 0.0f;
            float eaten = __state.quantity - servings;
            Cooking.ApplyQuality(__state.quality, eaten, __state.temperature, __state.food0, __state.food1, byEntity);
        }

        /// <summary>
        /// Postfix for the GetContainedInfo method.
        /// </summary>
        /// <param name="__result">The result.</param>
        /// <param name="inSlot">The in slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("GetContainedInfo")]
        public static void GetContainedInfoPostfix(ref string __result, ItemSlot inSlot)
        {
            float quality = QualityUtil.GetQuality(inSlot);
            if (quality <= 0.0f) return;
            __result += ", " + QualityUtil.QualityString(quality, false);
        }
    }//!class BlockMealPatch

    /// <summary>
    /// State for the tryFinishEatMeal method.
    /// </summary>
    internal class TryFinishEatMealState
    {
        public float temperature;
        public float quality;
        public float quantity;
        public EnumFoodCategory food0;
        public EnumFoodCategory food1;
    }//!class TryFinishEatMealState
}//!namespace XSkills
