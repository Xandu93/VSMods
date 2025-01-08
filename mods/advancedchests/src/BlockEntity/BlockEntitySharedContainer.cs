using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// A chest that shares its inventories with other chests.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockEntityLabeledChest" />
    public class BlockEntitySharedContainer : BlockEntityLabeledChest
    {
        /// <summary>
        /// Returns the inventory index of this chest.
        /// </summary>
        /// <returns>the inventory index of this chest</returns>
        protected int SharedInventoryIndex(ICoreAPI api)
        {
            AdvancedChestsSystem mod = api.ModLoader.GetModSystem<AdvancedChestsSystem>();
            int maxId = Math.Max(mod?.Config.maxSharedInventoryCount ?? 100, 1);

            string text = DialogTitle;
            int begin = text.IndexOf('[') + 1;
            int end = text.IndexOf(']', begin);
            if (begin == 0 || end == -1) return 0;
            int.TryParse(text.Substring(begin, end - begin), out int index);
            return index % maxId;
        }

        /// <summary>
        /// Creates an inventory from a attribute tree.
        /// Makes sure that the text attribute is set beforehand.
        /// It is required to get the inventory.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="worldForResolving">The world for resolving.</param>
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            bool hadInventory = Inventory != null;
            int old = SharedInventoryIndex(worldForResolving.Api);
            typeof(BlockEntityLabeledChest).GetField("text", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, tree.GetString("text"));
            base.FromTreeAttributes(tree, worldForResolving);
            int current = SharedInventoryIndex(worldForResolving.Api);

            //update the inventory on the client side after a label update
            if (worldForResolving is IClientWorldAccessor && hadInventory && current != old)
            {
                InitInventory(current);
                LateInitInventory();
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
                quantityColumns = block.Attributes["quantityColumns"][type].AsInt(4);

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

            ICoreAPI api = Api ?? typeof(Block).GetField("api", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Block) as ICoreAPI;
            InitInventory(SharedInventoryIndex(api));
            //quantityColumns = GameMath.Clamp((int)GameMath.Sqrt(Inventory?.Count ?? quantityColumns), 1, 12);
        }

        /// <summary>
        /// Initializes the inventory.
        /// </summary>
        /// <param name="index">The index.</param>
        protected virtual void InitInventory(int index)
        {
            //make sure to get an api since it may not been set yet
            ICoreAPI api = Api ?? typeof(Block).GetField("api", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(Block) as ICoreAPI;
            AdvancedChestsSystem mod = api.ModLoader.GetModSystem<AdvancedChestsSystem>();
            InventoryGeneric inventory = mod.GetOrCreateSharedInventory(index.ToString());
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            inventory.OnGetAutoPullFromSlot = GetAutoPullFromSlot;

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

            if (Inventory != null)
            {
                Inventory.OnInventoryClosed -= OnInvClosed;
                Inventory.OnInventoryOpened -= OnInvOpened;
            }

            inventory.PutLocked = retrieveOnly;
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            typeof(BlockEntityLabeledChest).GetField("inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, inventory);
        }

        /// <summary>
        /// Automatic pull from slot method.
        /// </summary>
        /// <param name="atBlockFace">The block face.</param>
        /// <returns>the item slot; null, if empty</returns>
        private ItemSlot GetAutoPullFromSlot(BlockFacing atBlockFace)
        {
            if (atBlockFace == BlockFacing.DOWN)
            {
                return Inventory.FirstOrDefault(slot => !slot.Empty);
            }

            return null;
        }

        /// <summary>
        /// Called when the server received a client packet.
        /// Updates the inventory when the label was changed.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="packetid">The packetid.</param>
        /// <param name="data">The data.</param>
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid != (int)EnumSignPacketId.SaveText)
            {
                base.OnReceivedClientPacket(player, packetid, data);
                return;
            }

            int old = SharedInventoryIndex(Api);
            base.OnReceivedClientPacket(player, packetid, data);
            int current = SharedInventoryIndex(Api);

            if (old != current)
            {
                InitInventory(current);
                MarkDirty();
            }
        }

        /// <summary>
        /// Called when the client received a server packet.
        /// Updates the inventory when the label was changed.
        /// </summary>
        /// <param name="packetid">The packetid.</param>
        /// <param name="data">The data.</param>
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid != (int)EnumSignPacketId.NowText)
            {
                base.OnReceivedServerPacket(packetid, data);
                return;
            }

            int old = SharedInventoryIndex(Api);
            base.OnReceivedServerPacket(packetid, data);
            int current = SharedInventoryIndex(Api);

            if (old != current)
            {
                InitInventory(current);
                MarkDirty();
            }
        }

        /// <summary>
        /// Called when player right clicks the chest.
        /// Prevents renaming when open.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        /// <param name="blockSel">The block selection.</param>
        /// <returns></returns>
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if ((byPlayer?.Entity?.Controls?.ShiftKey ?? false) &&
                Inventory.openedByPlayerGUIds.Count > 0)
            {
                return true;
            }
            return base.OnPlayerRightClick(byPlayer, blockSel);
        }
    }//!class BlockEntitySharedContainer
}//!namespace AdvancedChests
