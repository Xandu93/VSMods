using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// The player inventory can be completely transferred into this inventory.
    /// </summary>
    /// <seealso cref="AdvancedChests.BlockEntityAdvancedChest" />
    public class BlockEntityCoffin : BlockEntityAdvancedChest
    {
        /// <summary>
        /// Sets the label.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="color">The color.</param>
        public void SetLabel(string text, int color)
        {
            typeof(BlockEntityLabeledChest).GetField("text", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(this, text);
            typeof(BlockEntityLabeledChest).GetField("color", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(this, color);
            MarkDirty(true);
        }

        /// <summary>
        /// Creates the inventory.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <returns></returns>
        protected override InventoryGeneric CreateInventory(int quantitySlots)
        {
            InventoryGeneric inventory = new InventoryCoffin(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => 0.0f;
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            return inventory;
        }

        /// <summary>
        /// Called when the inventory was closed.
        /// </summary>
        /// <param name="player">The player.</param>
        protected override void OnInvClosed(IPlayer player)
        {
            base.OnInvClosed(player);
            if (Api.Side == EnumAppSide.Server) (Inventory as InventoryCoffin)?.TryShrink();
        }

        /// <summary>
        /// Transfers the player inventory into this one.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="inventoryNames">The inventory names of the player inventories.</param>
        public void TransferPlayerInventory(IServerPlayer player, HashSet<string> inventoryNames)
        {
            IPlayerInventoryManager manager = player?.InventoryManager;
            InventoryCoffin ownInv = Inventory as InventoryCoffin;
            IWorldAccessor world = player?.Entity?.World;

            if (ownInv == null || manager == null || world == null) return;

            int count = 0;
            foreach (string invName in inventoryNames)
            {
                IInventory inv = player.InventoryManager.GetOwnInventory(invName);
                if (inv == null) continue;
                count += CountStacks(inv);
            }

            ownInv.SetEmptySlots(count);
            int ownCounter = 0;

            foreach (string invName in inventoryNames)
            {
                IInventory inv = player.InventoryManager.GetOwnInventory(invName);
                if (inv == null) continue;
                InventoryPlayerBackPacks backpack = inv as InventoryPlayerBackPacks;
                if (backpack != null)
                {
                    //empty the backpacks before the backpacks are being taken out of the slots
                    for (int ii = 4; ii < backpack.Count; ++ii)
                    {
                        if (backpack[ii].Itemstack == null) continue;
                        ownInv[ownCounter].Itemstack = backpack[ii].Itemstack;
                        backpack[ii].Itemstack = null;
                        ownInv[ownCounter].MarkDirty();
                        backpack[ii].MarkDirty();
                        ownCounter++;
                    }

                    for (int ii = 0; ii < 4; ++ii)
                    {
                        if (backpack[ii].Itemstack == null) continue;
                        ownInv[ownCounter].Itemstack = backpack[ii].Itemstack;
                        backpack[ii].Itemstack = null;
                        ownInv[ownCounter].MarkDirty();
                        backpack[ii].MarkDirty();
                        ownCounter++;
                    }
                }
                else
                {
                    foreach (ItemSlot slot in inv)
                    {
                        if (slot.Itemstack == null) continue;
                        ownInv[ownCounter].Itemstack = slot.Itemstack;
                        slot.Itemstack = null;
                        ownInv[ownCounter].MarkDirty();
                        slot.MarkDirty();
                        ownCounter++;
                    }
                }
            }
        }

        /// <summary>
        /// Counts the number of non empty stacks in an inventory.
        /// </summary>
        /// <param name="inv">The inventory.</param>
        /// <returns></returns>
        protected int CountStacks(IInventory inv)
        {
            int count = 0;
            foreach (ItemSlot slot in inv)
            {
                if (slot.Itemstack != null) count++;
            }
            return count;
        }
    }
}
