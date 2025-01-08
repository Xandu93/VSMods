using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedChests
{
    /// <summary>
    /// An inventory shared across multiple chests.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventoryShared : InventoryGeneric
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedInventory"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="className"></param>
        /// <param name="instanceId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryShared(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceId, api, onNewSlot)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SharedInventory"/> class.
        /// </summary>
        /// <param name="quantitySlots"></param>
        /// <param name="invId"></param>
        /// <param name="api"></param>
        /// <param name="onNewSlot"></param>
        public InventoryShared(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        { }

        /// <summary>
        /// Drops the contents of all the slots into the world.
        /// Overrides the method to prevent dropping of items.
        /// </summary>
        /// <param name="pos">Where to drop all this stuff.</param>
        /// <param name="maxStackSize">If non-zero, will split up the stacks into stacks of give max stack size</param>
        public override void DropAll(Vec3d pos, int maxStackSize = 0)
        { }

        /// <summary>
        /// You can initialize an InventoryBase with null as parameters and use LateInitialize to set these values later. This is sometimes required during chunk loading.
        /// Does not change id if it is already set.
        /// </summary>
        /// <param name="inventoryID"></param>
        /// <param name="api"></param>
        public override void LateInitialize(string inventoryID, ICoreAPI api)
        {
            this.Api = api;
            if (className == null || instanceID == null)
            {
                string[] elems = inventoryID.Split(new char[] { '-' }, 2);
                className = elems[0];
                instanceID = elems[1];
            }

            if (InvNetworkUtil == null)
            {
                InvNetworkUtil = api.ClassRegistry.CreateInvNetworkUtil(this, api);
            }
            else
            {
                InvNetworkUtil.Api = api;
            }

            AfterBlocksLoaded(api.World);
        }

        /// <summary>
        /// Tries to resize the inventory.
        /// </summary>
        /// <param name="size">The size.</param>
        public void TryResize(int size)
        {
            if (openedByPlayerGUIds.Count > 0) return;
            int count = 0;
            foreach (ItemSlot slot in this)
            {
                if (slot.Itemstack != null) count++;
            }

            size = Math.Max(size, count);
            if (size == Count) return;

            ItemSlot[] newSlots = new ItemSlot[size];
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
    }//!class SharedInventory
}//!namespace AdvancedChests
