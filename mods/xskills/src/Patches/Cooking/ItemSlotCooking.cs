using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// Cooking slot for the cooking pot.
    /// Used by the firepit.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.ItemSlotWatertight" />
    public class ItemSlotCooking : ItemSlotWatertight
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSlotCooking"/> class.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        public ItemSlotCooking(InventoryBase inventory) : base(inventory)
        { }

        /// <summary>
        /// Gets the maximum size of the slot stack.
        /// </summary>
        /// <value>
        /// The maximum size of the slot stack.
        /// </value>
        public override int MaxSlotStackSize
        {
            get
            {
                ICoreAPI api = Inventory?.Api;
                if (api == null) return base.MaxSlotStackSize;
                BlockEntity blockEntity = api.World?.BlockAccessor?.GetBlockEntity(Inventory.Pos);
                if (blockEntity == null) return base.MaxSlotStackSize;
                BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
                EntityPlayer player = ownable?.Owner?.Entity;
                if (player == null) return base.MaxSlotStackSize;

                //canteen cook
                Cooking cooking = api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
                if (cooking == null) return base.MaxSlotStackSize;
                PlayerAbility ability = player.GetBehavior<PlayerSkillSet>()?[cooking.Id]?[cooking.CanteenCookId];
                if (ability == null) return base.MaxSlotStackSize;
                return (int)(base.MaxSlotStackSize * (1.0f + ability.FValue(0)));
            }
        }

        /// <summary>
        /// Called when a player has clicked on this slot.  The source slot is the mouse cursor slot.  This handles the logic of either taking, putting or exchanging items.
        /// </summary>
        /// <param name="sourceSlot"></param>
        /// <param name="op"></param>
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            BlockEntity blockEntity = Inventory?.Api.World.BlockAccessor.GetBlockEntity(inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (op.ActingPlayer != null && ownable != null) ownable.Owner = op.ActingPlayer;
            this.capacityLitres = MaxSlotStackSize;
            base.ActivateSlot(sourceSlot, ref op);
        }
    }//!class ItemSlotCooking

    /// <summary>
    /// Cooking slot for the oven.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.ItemSlotSurvival" />
    public class ItemSlotOven : ItemSlotSurvival
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemSlotOven"/> class.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        public ItemSlotOven(InventoryBase inventory) : base(inventory)
        { }

        /// <summary>
        /// Gets the maximum size of the slot stack.
        /// </summary>
        /// <value>
        /// The maximum size of the slot stack.
        /// </value>
        public override int MaxSlotStackSize
        {
            get
            {
                if (Inventory == null) return base.MaxSlotStackSize;
                BlockEntity blockEntity = Inventory.Api.World.BlockAccessor.GetBlockEntity(Inventory.Pos);
                if (blockEntity == null) return base.MaxSlotStackSize;
                BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
                EntityPlayer player = ownable?.Owner?.Entity;
                if (player == null) return base.MaxSlotStackSize;

                //canteen cook
                Cooking cooking = player.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
                if (cooking == null) return base.MaxSlotStackSize;
                PlayerAbility ability = player.GetBehavior<PlayerSkillSet>()[cooking.Id]?[cooking.CanteenCookId];
                if (ability == null) return base.MaxSlotStackSize;
                return (int)(base.MaxSlotStackSize * (1.0f + ability.FValue(0)));
            }
        }

        /// <summary>
        /// Called when a player has clicked on this slot.  The source slot is the mouse cursor slot.  This handles the logic of either taking, putting or exchanging items.
        /// </summary>
        /// <param name="sourceSlot"></param>
        /// <param name="op"></param>
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            BlockEntity blockEntity = Inventory?.Api.World.BlockAccessor.GetBlockEntity(Inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (op.ActingPlayer != null && ownable != null) ownable.Owner = op.ActingPlayer;

            base.ActivateSlot(sourceSlot, ref op);
        }
    }//!class ItemSlotOven

    /// <summary>
    /// Input slot pot.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.ItemSlotSurvival" />
    public class InputSlot : ItemSlot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputSlot"/> class.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        public InputSlot(InventoryBase inventory) : base(inventory)
        { }

        /// <summary>
        /// Called when a player has clicked on this slot.  The source slot is the mouse cursor slot.  This handles the logic of either taking, putting or exchanging items.
        /// </summary>
        /// <param name="sourceSlot"></param>
        /// <param name="op"></param>
        public override void ActivateSlot(ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            BlockEntity blockEntity = Inventory?.Api.World.BlockAccessor.GetBlockEntity(Inventory.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (op.ActingPlayer != null && ownable != null) ownable.Owner = op.ActingPlayer;

            base.ActivateSlot(sourceSlot, ref op);
        }
    }//!class ItemSlotOven
}//!namespace XSkills