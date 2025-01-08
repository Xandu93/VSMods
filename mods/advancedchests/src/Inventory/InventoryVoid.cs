using Vintagestory.API.Common;

namespace AdvancedChests
{
    /// <summary>
    /// An inventory that will destroy items. Can have buffer slots.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventoryVoid : InventoryGeneric
    {
        /// <summary>
        /// Prevents a stack overflow error when moving arround item stacks.
        /// </summary>
        bool preventLoop = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryVoid"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="invId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryVoid(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : 
            base(quantitySlots, invId, api, onNewSlot)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryVoid"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="className"></param>
        /// <param name="instanceId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryVoid(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null) : 
            base(quantitySlots, className, instanceId, api, onNewSlot)
        {}

        /// <summary>
        /// Called when one of the containing slots has been modified
        /// </summary>
        /// <param name="slot"></param>
        public override void OnItemSlotModified(ItemSlot slot)
        {
            base.OnItemSlotModified(slot);
            if (Api.Side == EnumAppSide.Client) return;
            if (preventLoop) return;

            preventLoop = true;
            if (slot.Itemstack != null)
            {
                for (int ii = Count - 1; ii > 0; --ii)
                {
                    if (slots[ii - 1].Itemstack == null) continue;
                    slots[ii].Itemstack = slots[ii - 1].Itemstack;
                    slots[ii - 1].Itemstack = null;
                    slots[ii].MarkDirty();
                    slots[ii - 1].MarkDirty();
                    if (slots[ii - 1] == slot) break;
                }
            }
            preventLoop = false;
        }
    }//!class InventoryVoid
}//!namespace AdvancedChests
