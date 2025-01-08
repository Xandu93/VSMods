using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace AdvancedChests
{
    /// <summary>
    /// Contains filter and buffer slots.
    /// Has one buffer slot for each of the six sides.
    /// The remaining slots are buffer slots.
    /// Each side has a number of filter slots assigned.
    /// You can only put one item in each filter slots.
    /// Only items that match with a filter slot can be put into a corresponding buffer slot.
    /// If all filters for one side are empty it means every item can go in there.
    /// The chest tries regularly to push the items from the buffer slots into neighbouring inventories.
    /// The push destination depends on the buffer slot.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.InventoryGeneric" />
    public class InventorySorting : InventoryGeneric
    {
        /// <summary>
        /// The background colors for the inventoy slots.
        /// See <seealso cref="Vintagestory.API.MathTools.BlockFacing" /> to get the ids for the corresponding sides.
        /// </summary>
        public static string[] BackgroundColors = { "#c16937", "#e7aa3b", "#7e7e7e", "#313730", "#d6d6ba", "#a27d5a" };

        /// <summary>
        /// index of the first buffer slot;
        /// filter slots start at 0
        /// </summary>
        public int FirstBuffer 
        {
            get => (Count - BlockFacing.NumberOfFaces);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySorting"/> class.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="instanceID">The instance identifier.</param>
        /// <param name="api">The API.</param>
        /// <param name="onNewSlot">The on new slot.</param>
        public InventorySorting(int quantitySlots, string className, string instanceID, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceID, api, onNewSlot)
        {
            slots = GenItemSlots(quantitySlots);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InventorySorting"/> class.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="invId">>The inventory identifier.</param>
        /// <param name="api">The API.</param>
        /// <param name="onNewSlot">The on new slot.</param>
        public InventorySorting(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        {
            slots = GenItemSlots(quantitySlots);
        }

        /// <summary>
        /// Generates empty item slots.
        /// </summary>
        /// <param name="quantity">The quantity.</param>
        /// <returns></returns>
        public ItemSlot[] GenItemSlots(int quantity)
        {
            ItemSlot[] slots = new ItemSlot[quantity];
            for (int i = 0; i < quantity; i++)
            {
                slots[i] = new ItemSlot(this);
                if (i < FirstBuffer)
                {
                    slots[i].HexBackgroundColor = BackgroundColors[i % 6];
                    slots[i].MaxSlotStackSize = 1;
                }
            }
            return slots;
        }

        /// <summary>
        /// Gets the best suited slot for the given item.
        /// </summary>
        /// <param name="sourceSlot">The source item slot.</param>
        /// <param name="skipSlots">The slots to skip.</param>
        /// <returns>
        /// A weighted slot set.
        /// </returns>
        public override WeightedSlot GetBestSuitedSlot(ItemSlot sourceSlot, ItemStackMoveOperation op = null, List<ItemSlot> skipSlots = null)
        {
            WeightedSlot bestWSlot = new WeightedSlot();

            // Useless to put the item into the same inventory
            if (PutLocked || sourceSlot.Inventory == this) return bestWSlot;
            bool hasFilter = false;
            bool useNoFilter = false;

            for (int face = 0; face < BlockFacing.NumberOfFaces; face++)
            {
                bool noFilter = true;

                for (int index = face; index < FirstBuffer; index += BlockFacing.NumberOfFaces)
                {
                    if (skipSlots != null && skipSlots.Contains(this[index])) continue;
                    if (this[index].Itemstack != null)
                    {
                        noFilter = false;
                        if (this[index].Itemstack.Collectible == sourceSlot.Itemstack?.Collectible)
                        {
                            hasFilter = true;
                            ItemSlot targetSlot = this[FirstBuffer + face];
                            if (targetSlot.CanTakeFrom(sourceSlot))
                            {
                                float curWeight = GetSuitability(sourceSlot, targetSlot, false) * 2.0f;
                                if (bestWSlot.slot == null || bestWSlot.weight < curWeight)
                                {
                                    useNoFilter = false;
                                    bestWSlot.slot = targetSlot;
                                    bestWSlot.weight = curWeight;
                                }
                            }
                        }
                    }
                }
                if (noFilter)
                {
                    ItemSlot targetSlot = this[FirstBuffer + face];
                    if (targetSlot.CanTakeFrom(sourceSlot))
                    {
                        float curWeight = GetSuitability(sourceSlot, targetSlot, false);
                        if (bestWSlot.slot == null || bestWSlot.weight < curWeight)
                        {
                            useNoFilter = true;
                            bestWSlot.slot = targetSlot;
                            bestWSlot.weight = curWeight;
                        }
                    }
                }
            }
            if (useNoFilter && hasFilter)
            {
                bestWSlot.slot = null;
                bestWSlot.weight = 0.0f;
            }
            return bestWSlot;
        }

        /// <summary>
        /// Gets the automatic pull from slot.
        /// </summary>
        /// <param name="atBlockFace">At block face.</param>
        /// <returns></returns>
        public override ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (OnGetAutoPullFromSlot != null)
            {
                return OnGetAutoPullFromSlot(atBlockFace);
            }

            int index = atBlockFace.Index + FirstBuffer;
            while (index < Count)
            {
                ItemSlot slot = this[index];
                if (slot.Itemstack == null) continue;
                if (slot.Itemstack.StackSize > 0) return slot;
                index += BlockFacing.NumberOfFaces;
            }
            return null;
        }
    }//!class InventoryFilter
}//!namespace AdvancedChests
