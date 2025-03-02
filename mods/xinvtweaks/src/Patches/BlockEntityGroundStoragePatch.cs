using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    internal class BlockEntityGroundStoragePatch
    {
        static public void OnPlayerInteractStartPrefix(BlockEntityGroundStorage __instance, IPlayer player)
        {
            if (__instance.Api.Side == EnumAppSide.Server) return;
            InventoryUtil.FindPushableCollectible(__instance.Inventory, player);
        }
    }
}
