using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// The patch for the CookingRecipe class.
    /// </summary>
    internal class CookingRecipePatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony.</param>
        /// <param name="cookingRecipeType">Type of the cooking recipe.</param>
        public static void Apply(Harmony harmony, Type cookingRecipeType)
        {
            MethodInfo original = cookingRecipeType.GetMethod("Matches", new Type[] { typeof(ItemStack[]), typeof(int).MakeByRefType() });
            MethodInfo prefix = typeof(CookingRecipePatch).GetMethod("MatchesPrefix");

            HarmonyMethod harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
            harmony.Patch(original, harmonyPrefix, null);
        }

        /// <summary>
        /// Prefix for the Matches method.
        /// This should fix an issue with recipes the only consists out of fluids.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__result">The result of the method.</param>
        /// <param name="inputStacks">The input stacks.</param>
        /// <param name="quantityServings">The quantity servings.</param>
        /// <returns></returns>
        public static bool MatchesPrefix(CookingRecipe __instance, ref bool __result, ItemStack[] inputStacks, ref int quantityServings)
        {
            List<ItemStack> inputStacksList = new List<ItemStack>(inputStacks);
            List<CookingRecipeIngredient> ingredientList = new List<CookingRecipeIngredient>(__instance.Ingredients);

            __result = false;
            int totalOutputQuantity = 99999;

            int[] curQuantities = new int[ingredientList.Count];
            for (int i = 0; i < curQuantities.Length; i++) curQuantities[i] = 0;

            while (inputStacksList.Count > 0)
            {
                ItemStack inputStack = inputStacksList[0];
                inputStacksList.RemoveAt(0);
                if (inputStack == null) continue;

                bool found = false;
                for (int i = 0; i < ingredientList.Count; i++)
                {
                    CookingRecipeIngredient ingred = ingredientList[i];

                    if (ingred.Matches(inputStack))
                    {
                        if (curQuantities[i] >= ingred.MaxQuantity) continue;
                        int qportions = inputStack.StackSize;

                        if (inputStack.Collectible.Attributes["waterTightContainerProps"].Exists == true)
                        {
                            var props = BlockLiquidContainerBase.GetContainableProps(inputStack);
                            qportions = (int)(inputStack.StackSize / props.ItemsPerLitre / __instance.GetIngrendientFor(inputStack).PortionSizeLitres);
                        }

                        totalOutputQuantity = Math.Min(totalOutputQuantity, qportions);
                        curQuantities[i]++;
                        found = true;
                        break;
                    }
                }

                // This input stack does not fit in this cooking recipe
                if (!found) return false;
            }

            // Any required ingredients left?
            for (int i = 0; i < ingredientList.Count; i++)
            {
                if (curQuantities[i] < ingredientList[i].MinQuantity) return false;
            }

            quantityServings = totalOutputQuantity;
            if (quantityServings <= 0)
            {
                __result = false;
                return false;
            }

            // Too many ingredients?
            for (int i = 0; i < inputStacks.Length; i++)
            {
                var stack = inputStacks[i];
                if (stack == null) continue;

                int qportions = stack.StackSize;

                if (stack.Collectible.Attributes["waterTightContainerProps"].Exists == true)
                {
                    var props = BlockLiquidContainerBase.GetContainableProps(stack);
                    qportions = (int)(stack.StackSize / props.ItemsPerLitre / __instance.GetIngrendientFor(stack).PortionSizeLitres);
                }

                if (qportions != quantityServings) return false;
            }

            __result = true;
            return false;
        }
    }//!class CookingRecipePatch
}//!namespace XSkills
