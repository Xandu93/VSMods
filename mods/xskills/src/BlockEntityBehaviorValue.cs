using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace XSkills
{
    /// <summary>
    /// Just adds a float value to a block entity.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.BlockEntityBehavior" />
    public class BlockEntityBehaviorValue : BlockEntityBehavior
    {
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public float Value { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockEntityBehaviorOwnable"/> class.
        /// </summary>
        /// <param name="blockentity">The blockentity.</param>
        public BlockEntityBehaviorValue(BlockEntity blockentity) : base(blockentity)
        {
            Value = 0.0f;
        }

        /// <summary>
        /// Froms the tree attributes.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="worldAccessForResolve">The world access for resolve.</param>
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            Value = tree.GetFloat("value");
        }

        /// <summary>
        /// Converts to treeattributes.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("value", Value);
        }

    }//!class BlockEntityBehaviorValue
}//!namespace XSkills
