using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedChests
{
    /// <summary>
    /// An item slot for the filter inventory.
    /// Tries to keep one item in a slot.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.ItemSlot" />
    public class ItemSlotFilter : ItemSlot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSlotFilter"/> class.
        /// </summary>
        /// <param name="inventory"></param>
        public ItemSlotFilter(InventoryBase inventory) : base(inventory)
        { }

        /// <summary>
        /// Gets some of the contents of the stack.
        /// If it contains more than one item it returns a stack with a stacksize with a maximum of stacksize - 1 items.
        /// </summary>
        /// <param name="quantity">The amount to get from the stack.</param>
        /// <returns>
        /// The stack with the quantity take out (or as much as was available)
        /// </returns>
        public override ItemStack TakeOut(int quantity)
        {
            if (itemstack == null) return null;
            if (itemstack.StackSize == 1) return base.TakeOut(quantity);
            if (itemstack.StackSize <= quantity) return base.TakeOut(itemstack.StackSize - 1);

            return base.TakeOut(quantity);
        }
    }//!class ItemSlotFilter

    /// <summary>
    /// The filter inventory. It uses filter slots.
    /// Prevents auto pull from slots with only one item left.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventoryFilter : InventoryGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryFilter"/> class.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="instanceID">The instance identifier.</param>
        /// <param name="api">The API.</param>
        /// <param name="onNewSlot">The on new slot.</param>
        public InventoryFilter(int quantitySlots, string className, string instanceID, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceID, api, onNewSlot)
        {
            slots = GenItemSlots(quantitySlots);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryFilter"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="invId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryFilter(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        {
            slots = GenItemSlots(quantitySlots);
        }

        /// <summary>
        /// Generates the item slots.
        /// </summary>
        /// <param name="quantity">The quantity.</param>
        /// <returns></returns>
        public ItemSlotFilter[] GenItemSlots(int quantity)
        {
            ItemSlotFilter[] slots = new ItemSlotFilter[quantity];
            for (int i = 0; i < quantity; i++)
            {
                slots[i] = new ItemSlotFilter(this);
            }
            return slots;
        }

        /// <summary>
        /// Returns the slot where a chute may pull items from. Returns null if it is not allowed to pull any items from this inventory.
        /// </summary>
        /// <param name="atBlockFace">At block face.</param>
        /// <returns>the slot where a chute may pull items from; null, if it is not allowed to pull any items</returns>
        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (OnGetAutoPullFromSlot != null)
            {
                return OnGetAutoPullFromSlot(atBlockFace);
            }

            foreach (ItemSlot slot in this)
            {
                if (slot.Itemstack == null) continue;
                if (slot.Itemstack.StackSize > 1) return slot;
            }

            return null;
        }
    }//!class InventoryFilter
}//!namespace AdvancedChests
