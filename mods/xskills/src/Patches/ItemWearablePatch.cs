using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    [HarmonyPatch(typeof(ItemWearable))]
    public static class ItemWearablePatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetWarmth")]
        public static void GetWarmthPostfix(ref float __result, ItemSlot inslot)
        {
            float quality = inslot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            if (quality > 0.0f) __result = (__result * (1.0f + quality * 0.05f));
        }

        //[HarmonyPostfix]
        //[HarmonyPatch("CalculateRepairValue")]
        //public static void CalculateRepairValuePostfix(ItemSlot[] allInputslots, ItemSlot outputSlot, ref float repairValue, ref int matCostPerMatType)
        //{
        //    IPlayer player = (outputSlot.Inventory as InventoryBasePlayer)?.Player;
        //    if (player == null) return;

        //    float repairBonus = 0.0f;

        //    repairValue = repairValue * (1.0f + repairBonus);
        //    float value = 1.0f / matCostPerMatType;
        //    value *= (1.0f + repairBonus);
        //    matCostPerMatType = Math.Min(1, matCostPerMatType - (int)(1.0f / value));
        //}
    }
}
