using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    internal class BlockEntityGroundStorageState
    {
        public ItemStack stack;
        public int stackSize;
    }

    [HarmonyPatch(typeof(BlockEntityGroundStorage))]
    internal class BlockEntityGroundStoragePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("TryPutItem")]
        internal static void TryPutItemPrefix(BlockEntityGroundStorage __instance, out BlockEntityGroundStorageState __state, IPlayer player)
        {
            __state = new BlockEntityGroundStorageState();
            __state.stack = player?.InventoryManager.ActiveHotbarSlot.Itemstack;
            if (__instance.Inventory?.Count < 1) return;
            __state.stackSize = __instance.Inventory[0].Itemstack?.StackSize ?? 0;
        }

        [HarmonyPostfix]
        [HarmonyPatch("TryPutItem")]
        internal static void TryPutItemPostfix(BlockEntityGroundStorage __instance, bool __result, BlockEntityGroundStorageState __state)
        {
            if (__state?.stack == null || !__result || __state.stackSize == 0) return;
            int newStackSize = __instance.Inventory[0].Itemstack.StackSize;
            int transferred = newStackSize - __state.stackSize;

            if (transferred <= 0) return;

            float oldQuality = __instance.Inventory[0].Itemstack.Attributes.TryGetFloat("quality") ?? 0.0f;
            float quality = __state.stack.Attributes.TryGetFloat("quality") ?? 0.0f;

            if (quality <= 0.0f && oldQuality <= 0.0f) return;
            float newQuality = (__state.stackSize * oldQuality + transferred * quality) / newStackSize;
            __instance.Inventory[0].Itemstack.Attributes.SetFloat("quality", newQuality);
        }
    }//!class BlockEntityGroundStoragePatch
}//!namespace XSkills
