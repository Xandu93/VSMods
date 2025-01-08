using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;

namespace XSkills
{
    /// <summary>
    /// The patch for the ItemExpandedFood class.
    /// </summary>
    internal class ItemExpandedFoodPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony.</param>
        /// <param name="type">The type.</param>
        public static void Apply(Harmony harmony, Type type)
        {
            Type patch = typeof(ItemExpandedFoodPatch);
            PatchMethod(harmony, type, patch, "GetHeldItemInfo");
        }

        /// <summary>
        /// Postfix for the GetHeldItemInfo method.
        /// </summary>
        /// <param name="inSlot">The in slot.</param>
        /// <param name="dsc">The string builder.</param>
        public static void GetHeldItemInfoPostfix(ItemSlot inSlot, StringBuilder dsc)
        {
            float quality = inSlot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            QualityUtil.AddQualityString(quality, dsc);
        }
    }//!class ItemExpandedFoodPatch
}//!namespace XSkills
