using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace AdvancedChests
{
    /// <summary>
    /// The coffin inventory.
    /// Can shrink in size. You can nerver put items into it.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventoryCoffin : InventoryGeneric
    {
        /// <summary>
        /// Returns always true to forbid putting items into any slot.
        /// </summary>
        public override bool PutLocked { get => true; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryCoffin"/> class.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="instanceID">The instance identifier.</param>
        /// <param name="api">The API.</param>
        /// <param name="onNewSlot">The on new slot.</param>
        public InventoryCoffin(int quantitySlots, string className, string instanceID, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceID, api, onNewSlot)
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="InventoryCoffin"/> class.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="invId">The inventory identifier.</param>
        /// <param name="api">The API.</param>
        /// <param name="onNewSlot">The on new slot.</param>
        public InventoryCoffin(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        {}

        /// <summary>
        /// Sets the slot count to the given quantity. Clears all slots.
        /// </summary>
        /// <param name="quantity">The quantity.</param>
        public void SetEmptySlots(int quantity)
        {
            slots = GenEmptySlots(quantity);
        }

        /// <summary>
        /// Loads the slot contents from given treeAttribute
        /// </summary>
        /// <param name="treeAttribute"></param>
        public override void FromTreeAttributes(ITreeAttribute treeAttribute)
        {
            int quantity = treeAttribute.GetInt("qslots", 1);
            SetEmptySlots(quantity);
            base.FromTreeAttributes(treeAttribute);
        }

        /// <summary>
        /// Tries to shrink the inventory.
        /// </summary>
        public void TryShrink()
        {
            if (openedByPlayerGUIds.Count > 0) return;
            int count = 0; 
            foreach (ItemSlot slot in this)
            {
                if (slot.Itemstack != null) count++;
            }

            if (count >= Count) return;

            ItemSlot[] newSlots = new ItemSlot[count];
            int free = 0;

            foreach (ItemSlot slot in this)
            {
                if (slot.Itemstack != null)
                {
                    newSlots[free] = slot;
                    free++;
                }
            }
            slots = newSlots;
        }
    }//!class InventoryCoffin
}//!namespace AdvancedChests
