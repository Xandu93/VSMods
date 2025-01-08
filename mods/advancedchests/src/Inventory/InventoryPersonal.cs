using Vintagestory.API.Common;

namespace AdvancedChests
{
    /// <summary>
    /// A player unique inventory shared across multiple chests.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventoryPersonal : InventoryShared
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedInventory"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="className"></param>
        /// <param name="instanceId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryPersonal(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceId, api, onNewSlot)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedInventory"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="invId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryPersonal(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        { }
    }//!class InventoryPersonal
}//!namespace AdvancedChests
