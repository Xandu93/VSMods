using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    public class InventoryOP : InventoryGeneric
    {
        InventoryOP sourceInv;

        public InventoryOP(int quantitySlots, string invId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, invId, api, onNewSlot)
        { }
        public InventoryOP(int quantitySlots, string className, string instanceId, ICoreAPI api, NewSlotDelegate onNewSlot = null) :
            base(quantitySlots, className, instanceId, api, onNewSlot)
        { }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            if (op.ActingPlayer != null) sourceInv = null;
            return base.ActivateSlot(slotId, sourceSlot, ref op);
        }

        public override ItemSlot GetAutoPushIntoSlot(BlockFacing atBlockFace, ItemSlot fromSlot)
        {
            InventoryOP originalSource = fromSlot.Inventory as InventoryOP;
            InventoryOP source = originalSource;
            while (source != null)
            {
                if (source == this) return null;
                source = source.sourceInv;
            }
            sourceInv = originalSource;

            return base.GetAutoPushIntoSlot(atBlockFace, fromSlot);
        }
    }

    public class BlockEntityOPContainer : BlockEntityLabeledChest
    {
        Dictionary<int, BlockPos> sortedContainers;
        public int Range { get; set; }

        //see https://github.com/anegostudios/vssurvivalmod/blob/master/BlockEntity/BEGenericTypedContainer.cs
        protected override void InitInventory(Block block)
        {
            if (block?.Attributes != null)
            {
                collisionSelectionBoxes = block.Attributes["collisionSelectionBoxes"]?[type]?.AsObject<Cuboidf[]>();

                inventoryClassName = block.Attributes["inventoryClassName"].AsString(inventoryClassName);

                dialogTitleLangCode = block.Attributes["dialogTitleLangCode"][type].AsString(dialogTitleLangCode);
                quantitySlots = block.Attributes["quantitySlots"][type].AsInt(quantitySlots);
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

            Range = 6;
            InventoryGeneric inventory = new InventoryOP(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;


            if (block?.Attributes != null)
            {
                if (block.Attributes["spoilSpeedMulByFoodCat"][type].Exists == true)
                {
                    inventory.PerishableFactorByFoodCategory = block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
                }

                if (block.Attributes["transitionSpeedMulByType"][type].Exists == true)
                {
                    inventory.TransitionableSpeedMulByType = block.Attributes["transitionSpeedMulByType"][type].AsObject<Dictionary<EnumTransitionType, float>>();
                }
            }

            inventory.PutLocked = retrieveOnly;
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            inventory.SlotModified += SlotModified;

            typeof(BlockEntityGenericTypedContainer).GetField("inventory", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(this as BlockEntityGenericTypedContainer, inventory);
            sortedContainers = new Dictionary<int, BlockPos>();
        }

        //see https://github.com/anegostudios/vssurvivalmod/blob/master/BlockEntity/BEGenericTypedContainer.cs
        private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (atBlockFace == BlockFacing.DOWN)
            {
                return Inventory.FirstOrDefault(slot => !slot.Empty);
            }

            return null;
        }

        protected virtual void SlotModified(int slotId)
        {
            ItemSlot slot = Inventory[slotId];
            if (slot?.Itemstack == null) return;
            BlockEntityLabeledChest container = null;

            BlockPos pos;
            sortedContainers.TryGetValue(slot.Itemstack.Id, out pos);

            if (pos != null) container = Api.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityLabeledChest;

            //check whether the label has changes and is now invalid
            if (container != null)
            {
                if (!CheckMatch(slot.Itemstack.Collectible, container.DialogTitle)) container = null;
            }

            if (container == null)
            {
                container = FindMatchingContainer(slot.Itemstack.Collectible);
                if (container == null) return;
                sortedContainers[slot.Itemstack.Id] = container.Pos;
            }

            ItemSlot slot2 = container.Inventory.GetAutoPushIntoSlot(BlockFacing.UP, slot);
            if (slot2 != null) slot?.TryPutInto(Api.World, slot2, slot.StackSize);
        }

        protected BlockEntityLabeledChest FindMatchingContainer(CollectibleObject collectible)
        {
            BlockEntityLabeledChest container = null;
            List<BlockEntityLabeledChest> containers = new List<BlockEntityLabeledChest>();

            for (int xx = Pos.X - 1; xx > Pos.X - Range; --xx)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(xx, Pos.Y, Pos.Z, Pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
                AddChestsUpDown(ref containers, container.Pos);
            }
            for (int xx = Pos.X + 1; xx < Pos.X + Range; ++xx)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(xx, Pos.Y, Pos.Z, Pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
                AddChestsUpDown(ref containers, container.Pos);
            }
            for (int zz = Pos.Z - 1; zz > Pos.Z - Range; --zz)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(Pos.X, Pos.Y, zz, Pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
                AddChestsUpDown(ref containers, container.Pos);
            }
            for (int zz = Pos.Z + 1; zz < Pos.Z + Range; ++zz)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(Pos.X, Pos.Y, zz, Pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
                AddChestsUpDown(ref containers, container.Pos);
            }
            AddChestsUpDown(ref containers, Pos);

            foreach (BlockEntityLabeledChest container2 in containers)
            {
                if (CheckMatch(collectible, container2.DialogTitle)) return container2;
            }
            return null;
        }

        protected string GetTag()
        {
            string text = DialogTitle;
            int begin = text.IndexOf('{') + 1;
            int end = text.IndexOf('}', begin);
            if (begin == 0 || end == -1) return null;
            return text.Substring(begin, end - begin);
        }

        protected void AddChestsUpDown(ref List<BlockEntityLabeledChest> containers, BlockPos pos)
        {
            BlockEntityLabeledChest container = null;
            for (int yy = pos.Y - 1; yy > pos.Y - Range; --yy)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(pos.X, yy, pos.Z, pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
            }
            for (int yy = pos.Y + 1; yy < pos.Y + Range; ++yy)
            {
                container = Api.World.BlockAccessor.GetBlockEntity(new BlockPos(pos.X, yy, pos.Z, pos.dimension)) as BlockEntityLabeledChest;
                if (container == null) break;
                containers.Add(container);
            }
        }

        protected bool CheckMatch(CollectibleObject collectible, string tag)
        {
            if (tag == null) return false;
            string[] tagParts = tag.Split(',', (char)StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in tagParts)
            {
                if (WildcardUtil.Match(collectible.Code.GetName(), part)) return true;
            }
            return false;
        }
    }//!class SortingInventory
}//!namespace AdvancedChests
