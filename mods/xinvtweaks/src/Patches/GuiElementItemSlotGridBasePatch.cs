using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace XInvTweaks
{
    internal class GuiElementItemSlotGridBasePatch
    {
        public static void OnMouseWheelPostfix(GuiElementItemSlotGridBase __instance, ElementBounds[] ___SlotBounds, OrderedDictionary<int, ItemSlot> ___renderedSlots, IInventory ___inventory, Action<object> ___SendPacketHandler, ICoreClientAPI api, MouseWheelEventArgs args)
        {
            bool ctrl = api.Input.KeyboardKeyState[(int)GlKeys.ShiftLeft] || api.Input.KeyboardKeyState[(int)GlKeys.ShiftRight];
            if (ctrl && __instance.KeyboardControlEnabled && __instance.IsPositionInside(api.Input.MouseX, api.Input.MouseY))
            {
                for (int i = 0; i < __instance.SlotBounds.Length; i++)
                {
                    if (i >= ___renderedSlots.Count) break;

                    if (__instance.SlotBounds[i].PointInside(api.Input.MouseX, api.Input.MouseY))
                    {
                        OnMouseWheel(api, ___inventory[___renderedSlots.GetKeyAtIndex(i)], args.delta, ___SendPacketHandler);
                        args.SetHandled(true);
                    }
                }
            }
        }

        internal static void OnMouseWheel(ICoreClientAPI api, ItemSlot source, int wheelDelta, Action<object> SendPacketHandler)
        {
            object packet = null;
            if (api.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
            if (wheelDelta > 0)
            {
                packet = PushItem(api, source);
            }
            else
            {
                packet = PullItem(api, source);
            }

            if (packet != null)
            {
                {
                    SendPacketHandler?.Invoke(packet);
                }
            }

        }
        internal static object PushItem(ICoreClientAPI api, ItemSlot source)
        {
            ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1);
            op.ActingPlayer = api.World.Player;
            ItemSlot target = api.World.Player.InventoryManager.GetBestSuitedSlot(source, false);
            return api.World.Player.InventoryManager.TryTransferTo(source, target, ref op);
        }

        internal static object PullItem(ICoreClientAPI api, ItemSlot target)
        {
            if (target?.Itemstack == null) return null;
            List<IInventory> inventories = api.World.Player.InventoryManager.OpenedInventories;
            ItemSlot bestSource = null;

            foreach (IInventory inventory in inventories)
            {
                if (inventory == target.Inventory && bestSource != null) continue;

                if (target.Inventory is InventoryBasePlayer && 
                    bestSource?.Inventory is InventoryBasePlayer) continue;

                foreach(ItemSlot slot in inventory)
                {
                    if (slot == target) continue;
                    if (slot.Itemstack?.Collectible == target.Itemstack.Collectible)
                    {
                        bestSource = slot;
                        break;
                    }
                }
                if ((bestSource?.Inventory is InventoryBasePlayer && bestSource?.Inventory != target.Inventory)) break;
            }

            if (bestSource == null) return null;
            ItemStackMoveOperation op = new ItemStackMoveOperation(api.World, EnumMouseButton.Wheel, 0, EnumMergePriority.AutoMerge, 1);
            op.ActingPlayer = api.World.Player;
            return api.World.Player.InventoryManager.TryTransferTo(bestSource, target, ref op);

        }

    }//!class GuiElementItemSlotGridBasePatch
}//!namespace XInvTweaks
