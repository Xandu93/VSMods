using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    internal class BlockEntityCratePatch
    {
        static public void OnBlockInteractStartPrefix(BlockEntityCrate __instance, IPlayer byPlayer)
        {
            if (__instance.Api.Side == EnumAppSide.Server) return;
            InventoryUtil.FindPushableCollectible(__instance.Inventory, byPlayer);
        }
    }
}
