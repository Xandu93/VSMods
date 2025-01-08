using Vintagestory.API.Common;

namespace AdvancedChests
{
    /// <summary>
    /// Replaces the container behavior that is used usually for containers.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.BlockBehavior" />
    public class BlockBehaviorPersonalContainer : BlockBehavior
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BlockBehaviorPersonalContainer"/> class.
        /// </summary>
        /// <param name="block">The block.</param>
        public BlockBehaviorPersonalContainer(Block block) : base(block)
        { }

        /// <summary>
        /// When a player does a right click while targeting this placed block. Should return true if the event is handled, so that other events can occur, e.g. eating a held item if the block is not interactable with.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSel"></param>
        /// <param name="handling"></param>
        /// <returns></returns>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            handling = EnumHandling.Handled;
            BlockEntityPersonalContainer entity = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPersonalContainer;
            if (entity == null) return false;
            entity.OnPlayerRightClick(world, byPlayer);
            return true;
        }
    }//!class BlockBehaviorPersonalContainer
}//!namespace AdvancedChests
