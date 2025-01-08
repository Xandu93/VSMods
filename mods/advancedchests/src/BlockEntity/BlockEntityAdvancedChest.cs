using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// A base class for some advanced chests.
    /// Allows to override the CreateInventory method to create a specific type of inventory.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockEntityLabeledChest" />
    public class BlockEntityAdvancedChest : BlockEntityLabeledChest
    {
        /// <summary>
        /// Creates the inventory.
        /// </summary>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <returns></returns>
        protected virtual InventoryGeneric CreateInventory(int quantitySlots)
        {
            InventoryGeneric inventory = new InventoryGeneric(quantitySlots, null, null, null);
            inventory.BaseWeight = 1f;
            inventory.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => (isMerge ? (inventory.BaseWeight + 3) : (inventory.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
            inventory.OnInventoryClosed += OnInvClosed;
            inventory.OnInventoryOpened += OnInvOpened;
            return inventory;
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

            InventoryGeneric inv = CreateInventory(quantitySlots);
            typeof(BlockEntityLabeledChest).GetField("inventory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(this, inv);
            if (inv == null) return;
            inv.PutLocked = retrieveOnly;

            if (Block?.Attributes != null)
            {
                if (Block.Attributes["spoilSpeedMulByFoodCat"][type].Exists == true)
                {
                    inv.PerishableFactorByFoodCategory = Block.Attributes["spoilSpeedMulByFoodCat"][type].AsObject<Dictionary<EnumFoodCategory, float>>();
                }

                if (Block.Attributes["transitionSpeedMulByType"][type].Exists == true)
                {
                    inv.TransitionableSpeedMulByType = Block.Attributes["transitionSpeedMulByType"][type].AsObject<Dictionary<EnumTransitionType, float>>();
                }
            }
        }

    }//!class BlockEntityAdvancedChest
}//!namespace AdvancedChests
