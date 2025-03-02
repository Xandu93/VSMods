using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Common;

namespace XInvTweaks
{
    internal class InventoryBasePatch
    {
        static bool ActivateSlotPrefix(InventoryBase __instance, ref object __result, int slotId, ref ItemStackMoveOperation op)
        {
            if (op.ShiftDown || !op.CtrlDown) return true;
            if (__instance is InventoryPlayerCreative) return true;

            ICoreClientAPI capi = __instance.Api as ICoreClientAPI;
            ItemSlot slot = __instance[slotId];
            if (slot?.Itemstack == null || capi == null) return true;
            if (slot.Itemstack.Collectible.IsLiquid()) return true;
            __result = null;

            for (int ii = 0; ii <= slotId; ii++)
            {
                if (__instance[ii].Itemstack == null) continue;
                if (__instance[ii].Itemstack.Collectible != slot.Itemstack.Collectible) continue;
                ItemStackMoveOperation slotOp = new ItemStackMoveOperation(op.World, op.MouseButton, EnumModifierKey.SHIFT, EnumMergePriority.AutoMerge);
                slotOp.ActingPlayer = op.ActingPlayer;
                slotOp.RequestedQuantity = __instance[ii].Itemstack.StackSize;

                object[] objcs = slotOp.ActingPlayer.InventoryManager.TryTransferAway(__instance[ii], ref slotOp, false);
                if (objcs == null) continue;
                foreach (object obj in objcs)
                {
                    capi.Network.SendPacketClient(obj);
                }
            }
            return false;
        }
    }//!class InventoryBasePatch
}//!namespace XInvTweaks
