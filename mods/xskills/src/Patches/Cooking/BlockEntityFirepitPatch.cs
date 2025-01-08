using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityFirepitPatch class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityFirepit))]
    public class BlockEntityFirepitPatch
    {
        /// <summary>
        /// Determines whether the specified firepit contains food.
        /// </summary>
        /// <param name="firepit">The firepit.</param>
        /// <returns>
        ///   <c>true</c> if the specified firepit contains food; otherwise, <c>false</c>.
        /// </returns>
        public static bool ContainsFood(BlockEntityFirepit firepit)
        {
            CollectibleObject input = firepit.inputSlot?.Itemstack?.Collectible;
            if (input == null) return false;
            bool isFood = input is BlockCookingContainer || input is BlockBucket;

            if (!isFood)
            {
                CombustibleProperties combProps = input.CombustibleProps;
                isFood = combProps?.SmeltedStack?.ResolvedItemstack?.Collectible?.NutritionProps != null;
            }
            return isFood;
        }

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
            return
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled;
        }

        /// <summary>
        /// Postfix for the maxCookingTime method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result.</param>
        [HarmonyPostfix]
        [HarmonyPatch("maxCookingTime")]
        public static void maxCookingTimePostfix(BlockEntityFirepit __instance, ref float __result)
        {
            if (ContainsFood(__instance))
            {
                __result *= CookingUtil.GetCookingTimeMultiplier(__instance);
            }
        }

        /// <summary>
        /// Postfix for the SetDialogValues method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="dialogTree">The dialog tree.</param>
        [HarmonyPostfix]
        [HarmonyPatch("SetDialogValues")]
        public static void SetDialogValuesPostfix(BlockEntityFirepit __instance, ITreeAttribute dialogTree)
        {
            if (!ContainsFood(__instance)) return;
            float? meltingDuration = dialogTree.TryGetFloat("maxOreCookingTime");
            if (meltingDuration != null)
            {
                dialogTree.SetFloat("maxOreCookingTime", (float)meltingDuration * CookingUtil.GetCookingTimeMultiplier(__instance));
            }
        }
    }//!class BlockEntityFirepitPatch
}//!namespace XSkills
