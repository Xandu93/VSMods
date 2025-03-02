using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    internal class BlockEntityItemPilePatch
    {
        static public void OnPlayerInteractPrefix(BlockEntityItemPile __instance, IPlayer byPlayer)
        {
            if (__instance.Api.Side == EnumAppSide.Server) return;
            InventoryUtil.FindPushableCollectible(__instance.inventory, byPlayer);
        }
    }
}
