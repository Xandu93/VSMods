using HarmonyLib;
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
    }
}
