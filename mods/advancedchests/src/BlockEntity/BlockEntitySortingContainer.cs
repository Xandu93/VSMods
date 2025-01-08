using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// A container that can sort items and push them into specified neighboring blocks.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockEntityLabeledChest" />
    public class BlockEntitySortingContainer : BlockEntityLabeledChest
    {
        /// <summary>
        /// time past since the last update
        /// </summary>
        protected float dt = 0;

        /// <summary>
        /// True if the game rick listener should listen; false otherwise.
        /// Should be true when items are in the buffer slots.
        /// </summary>
        protected bool listen = false;

        /// <summary>
        /// The orientation index.
        /// See <seealso cref="Vintagestory.API.MathTools.BlockFacing" /> to get the ids.
        /// </summary>
        int orientationIndex = 0;

        /// <summary>
        /// Initializes the entity.
        /// </summary>
        /// <param name="api">The API.</param>
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            switch (Block.Code.Path.Substring(Block.Code.Path.LastIndexOf('-') + 1))
            {
                case "north":
                    orientationIndex = BlockFacing.indexNORTH;
                    break;
                case "east":
                    orientationIndex = BlockFacing.indexEAST;
                    break;
                case "south":
                    orientationIndex = BlockFacing.indexSOUTH;
                    break;
                case "west":
                    orientationIndex = BlockFacing.indexWEST;
                    break;
            }

            if (api.Side == EnumAppSide.Server)
            {
                InventorySorting Inventory = this.Inventory as InventorySorting;
                if (Inventory == null || listen) return;
                for (int i = Inventory.FirstBuffer; i < Inventory.Count; i++)
                {
                    if (!Inventory[i].Empty)
                    {
                        listen = true;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the inventory.
        /// </summary>
        /// <param name="block">The block.</param>
        protected override void InitInventory(Block block)
        {
            if (block?.Attributes != null)
            {
                collisionSelectionBoxes = block.Attributes["collisionSelectionBoxes"]?[type]?.AsObject<Cuboidf[]>();

                inventoryClassName = block.Attributes["inventoryClassName"].AsString(inventoryClassName);

                dialogTitleLangCode = block.Attributes["dialogTitleLangCode"][type].AsString(dialogTitleLangCode);
                quantitySlots = block.Attributes["quantitySlots"][type].AsInt(quantitySlots);
                quantityColumns = block.Attributes["quantityColumns"][type].AsInt(6);

                retrieveOnly = block.Attributes["retrieveOnly"][type].AsBool(false);

                if (block.Attributes["typedOpenSound"][type].Exists)
                {
                    OpenSound = AssetLocation.Create(block.Attributes["typedOpenSound"][type].AsString(OpenSound.ToShortString()), block.Code.Domain);
                }
                if (block.Attributes["typedCloseSound"][type].Exists)
                {
                    CloseSound = AssetLocation.Create(block.Attributes["typedCloseSound"][type].AsString(CloseSound.ToShortString()), block.Code.Domain);
                }
            }

            InventorySorting inventory = new InventorySorting(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;

            if (Block?.Attributes != null)
            {
                if (Block.Attributes["spoilSpeedMulByFoodCat"][type].Exists == true)
                {
                    inventory.PerishableFactorByFoodCategory = Block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
                }

                if (Block.Attributes["transitionSpeedMulByType"][type].Exists == true)
                {
                    inventory.TransitionableSpeedMulByType = Block.Attributes["transitionSpeedMulByType"][type].AsObject<Dictionary<EnumTransitionType, float>>();
                }
            }

            inventory.PutLocked = retrieveOnly;
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            inventory.SlotModified += OnSlotModified;
            typeof(BlockEntityLabeledChest).GetField("inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, inventory);
        }

        /// <summary>
        /// Called every few ticks.
        /// </summary>
        /// <param name="dt">Time past since the last tick.</param>
        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            this.dt += dt;
            if (this.dt < 20) return;
            this.dt = 0.0f;
            if (!listen) return;

            OnGameTickListener();
        }

        /// <summary>
        /// Called every few seconds if items are in the buffer slots.
        /// Tries to push items stacks into neighboring inventories.
        /// </summary>
        protected void OnGameTickListener()
        {
            if (Api.Side != EnumAppSide.Server) return;
            IBulkBlockAccessor blockAccessor = Api?.World?.BulkBlockAccessor;
            InventorySorting inv = this.Inventory as InventorySorting;
            if (blockAccessor == null || inv == null) return;
            bool empty = true;

            for (int index = 0; index < BlockFacing.NumberOfFaces; index++)
            {
                ItemSlot slot = inv[index + inv.FirstBuffer];
                if (slot.Itemstack == null) continue;
                empty = false;

                InventoryBase target = null;
                BlockFacing sourceFacing = null;
                int face = index;

                //get the proper rotation
                if (orientationIndex > 0 && orientationIndex < 4 && face < 4)
                {
                    face = (face + orientationIndex) % 4;
                }

                switch (face)
                {
                    case BlockFacing.indexNORTH:
                        target = (blockAccessor.GetBlockEntity(Pos.NorthCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.SOUTH;
                        break;
                    case BlockFacing.indexEAST:
                        target = (blockAccessor.GetBlockEntity(Pos.EastCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.EAST;
                        break;
                    case BlockFacing.indexSOUTH:
                        target = (blockAccessor.GetBlockEntity(Pos.SouthCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.SOUTH;
                        break;
                    case BlockFacing.indexWEST:
                        target = (blockAccessor.GetBlockEntity(Pos.WestCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.WEST;
                        break;
                    case BlockFacing.indexUP:
                        target = (blockAccessor.GetBlockEntity(Pos.UpCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.UP;
                        break;
                    case BlockFacing.indexDOWN:
                        target = (blockAccessor.GetBlockEntity(Pos.DownCopy()) as IBlockEntityContainer)?.Inventory as InventoryBase;
                        sourceFacing = BlockFacing.DOWN;
                        break;
                }
                if (target == null) continue;

                ItemSlot targetSlot = target.GetAutoPushIntoSlot(sourceFacing, slot);
                if (targetSlot == null) continue;
                slot.TryPutInto(Api.World, targetSlot, slot.StackSize);
            }

            listen = !empty;
        }

        /// <summary>
        /// Called when a slot was modified.
        /// </summary>
        /// <param name="slotID">Slot identifier of the modified slot.</param>
        protected void OnSlotModified(int slotID)
        {
            InventorySorting Inventory = this.Inventory as InventorySorting;
            if (Inventory == null || listen) return;
            if (slotID < Inventory.FirstBuffer || slotID >= Inventory.Count) return;
            if (Inventory[slotID].Empty) return;

            listen = true;
        }
    }//!class BlockEntitySortingContainer
}//!namespace AdvancedChests