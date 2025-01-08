using Vintagestory.API.Common;

namespace AdvancedChests
{
    /// <summary>
    /// A chests that tries to keep one item in a slot.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockEntityLabeledChest" />
    public class BlockEntityFilterContainer : BlockEntityAdvancedChest
    {
        /// <summary>
        /// Creates the inventory.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <returns></returns>
        protected override InventoryGeneric CreateInventory(int quantitySlots)
        {
            InventoryGeneric inventory = new InventoryFilter(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            return inventory;
        }

    }//!class BlockEntityFilterContainer
}//!namespace AdvancedChests
