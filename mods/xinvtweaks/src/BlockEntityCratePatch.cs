using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    internal class BlockEntityCratePatch
    {
        static void OnBlockInteractStartPrefix(BlockEntityCrate __instance, IPlayer byPlayer)
        {
            CollectibleObject collectible = __instance.Inventory.FirstNonEmptySlot?.Itemstack?.Collectible;
            if (collectible == null) return;
            ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            if (hotbarSlot == null || !hotbarSlot.Empty) return;
            if (!byPlayer.Entity.Controls.ShiftKey) return;

            ItemSlot bestSlot = null;
            byPlayer.InventoryManager.Find((ItemSlot slot) =>
            {
                if (slot.Itemstack?.Collectible == collectible)
                {
                    bestSlot = slot;
                    return true;
                }
                return false;
            });
            if (bestSlot == null) return;

            int slotID = -1;
            for (int ii = 0; ii <= bestSlot.Inventory.Count; ++ii)
            {
                if (bestSlot.Inventory[ii] == bestSlot)
                {
                    slotID = ii;
                    break;
                }
            }

            object packet = bestSlot.Inventory.TryFlipItems(slotID, byPlayer.InventoryManager.ActiveHotbarSlot);
            (byPlayer.Entity.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
        }
    }//!class BlockEntityCratePatch
}//!namespace XInvTweaks
