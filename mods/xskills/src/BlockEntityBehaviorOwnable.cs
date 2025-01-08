using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XSkills
{
    /// <summary>
    /// The entity can be owned by a player.
    /// The owner can be swapped.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.BlockEntityBehavior" />
    public class BlockEntityBehaviorOwnable : BlockEntityBehavior
    {
        /// <summary>
        /// Gets or sets the owner represented by a string.
        /// </summary>
        /// <value>
        /// The owner string.
        /// </value>
        public string OwnerString { get; set; }

        /// <summary>
        /// The owner.
        /// </summary>
        protected IPlayer owner;

        /// <summary>
        /// Gets or sets the owner.
        /// </summary>
        /// <value>
        /// The owner.
        /// </value>
        public IPlayer Owner 
        { 
            get
            {
                if (owner == null) ResolveOwner();
                return owner;
            }
            set
            {
                owner = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockEntityBehaviorOwnable"/> class.
        /// </summary>
        /// <param name="blockentity">The blockentity.</param>
        public BlockEntityBehaviorOwnable(BlockEntity blockentity) : base(blockentity)
        {
            OwnerString = null;
            owner = null;
        }

        /// <summary>
        /// Returns whether the owner should be resolved.
        /// </summary>
        /// <returns>whether the owner should be resolved</returns>
        public bool ShouldResolveOwner()
        {
            if (OwnerString != null && owner == null) return true;
            return false;
        }

        /// <summary>
        /// Resolves the owner.
        /// </summary>
        /// <returns></returns>
        public bool ResolveOwner()
        {
            if (OwnerString == null)
            {
                owner = null;
                return false;
            }
            owner = Api?.World.PlayerByUid(OwnerString);
            if (owner != null) return true;
            return false;
        }

        /// <summary>
        /// Gets the block information.
        /// Adds the owner information.
        /// </summary>
        /// <param name="forPlayer">For player.</param>
        /// <param name="dsc">The dsc.</param>
        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (owner == null) return;
            dsc.AppendLine(Lang.Get("xskills:owner-desc", owner.PlayerName));
        }

        /// <summary>
        /// Froms the tree attributes.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="worldAccessForResolve">The world access for resolve.</param>
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            OwnerString = tree.GetString("owner");
            ResolveOwner();
        }

        /// <summary>
        /// Converts to treeattributes.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (owner != null) tree.SetString("owner", owner.PlayerUID);
            else if(OwnerString != null) tree.SetString("owner", OwnerString);
        }

    }//!class BlockEntityBehaviorOwnable
}//!namespace XSkills
