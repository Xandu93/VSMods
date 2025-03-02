using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace XInvTweaks
{
    //used for block picking
    public class SystemMouseInWorldInteractionsPatch
    {
        public static void HandleMouseInteractionsBlockSelectedPostfix(SystemMouseInWorldInteractions __instance, ClientMain ___game)
        {
            IClientPlayer player = ___game.Player;
            if (player?.InventoryManager?.ActiveHotbarSlot == null) return;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Survival) return;
            if (!((___game.Api as ICoreClientAPI)?.Input.MouseButton.Middle ?? false)) return;

            Block block = player.CurrentBlockSelection?.Block;
            if (block == null) return;
            if (player.InventoryManager.ActiveHotbarSlot.Itemstack?.Collectible.Id == block.Id) return;
            if (player.InventoryManager.ActiveHotbarSlot is ItemSlotBackpack) return;

            ItemSlot bestSlot = null;
            InventoryUtil.FindBestSlot(block, player, ref bestSlot);
            int slotID = InventoryUtil.GetSlotID(bestSlot);
            if (slotID == -1 || bestSlot == null) return;
            object packet = bestSlot.Inventory.TryFlipItems(slotID, player.InventoryManager.ActiveHotbarSlot);
            (player.Entity.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
        }
    }//!class SystemMouseInWorldInteractionsPatch
}//!namespace XInvTweaks
