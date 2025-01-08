using Vintagestory.API.Common;

namespace AdvancedChests
{
    /// <summary>
    /// A chest that will destroy items. Can have buffer slots.
    /// </summary>
    /// <seealso cref="AdvancedChests.BlockEntityAdvancedChest" />
    public class BlockEntityVoidContainer : BlockEntityAdvancedChest
    {
        /// <summary>
        /// Creates the inventory.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <returns></returns>
        protected override InventoryGeneric CreateInventory(int quantitySlots)
        {
            InventoryGeneric inventory = new InventoryVoid(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            return inventory;
        }
    }//!class BlockEntityVoidChest
}//!namespace AdvancedChests
