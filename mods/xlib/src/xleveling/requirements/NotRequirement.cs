using System;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Inverts a requirement
    /// </summary>
    /// <seealso cref="Requirement" />
    public class NotRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "not";

        /// <summary>
        /// Gets or sets the inverted requirement.
        /// </summary>
        /// <value>
        /// The requirement.
        /// </value>
        public Requirement Requirement { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotRequirement"/> class.
        /// </summary>
        public NotRequirement() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="NotRequirement" /> class.
        /// </summary>
        /// <param name="requirement">The requirement.</param>
        /// <param name="minimumTier">The minimum tier.</param>
        /// <exception cref="ArgumentNullException">Is thrown if requirement is <c>null</c>.</exception>
        public NotRequirement(Requirement requirement, int minimumTier = 1) : base()
        {
            this.Requirement = requirement ?? throw new ArgumentNullException("The requirement of a not requirement must not be null.");
        }

        /// <summary>
        /// Creates a requirement from a tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="toResolve">XLeveling object for resolving.</param>
        /// <returns>
        ///   <c>true</c> if the resolving was successful, the requirement is only added to an ability if this method was successful; otherwise, <c>false</c>.
        /// </returns>
        public override bool FromTree(TreeAttribute tree, XLeveling toResolve)
        {
            base.FromTree(tree, toResolve);
            TreeAttribute requirmentAttribute = tree.GetAttribute("requirement") as TreeAttribute;
            if (requirmentAttribute == null) return false;

            Requirement requirement = toResolve.ResolveRequirment(requirmentAttribute);
            if (requirement == null) return false;
            this.Requirement = requirement;
            return true;
        }

        /// <summary>
        /// Determines whether the specified player ability fulfills the requirement.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <param name="tier">The tier this requirement is checked for.</param>
        /// <returns>
        ///   <c>true</c> if the specified player ability fulfills the requirement; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsFulfilled(PlayerAbility playerAbility, int tier = 0)
        {
            if (tier < this.MinimumTier) return true;
            return !this.Requirement.IsFulfilled(playerAbility, tier);
        }

        /// <summary>
        /// This function is called when the requirement is not fulfilled after all skills are loaded and should resolve this conflict.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   false, if this conflict has been ignored; true, if the conflict has been resolved.
        /// </returns>
        public override bool ResolveConflict(PlayerAbility playerAbility)
        {
            if (playerAbility != null)
            {
                playerAbility.Tier = 0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Describes the requirement for the given player ability.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        /// a Description that describes the requirement for the given player ability.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            return "NOT: " + this.Requirement.Description(playerAbility);
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        /// the Type of the requirement.
        /// </returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override EnumRequirementType RequirementType()
        {
            return this.Requirement.RequirementType();
        }
    }//!class NotRequirement
}//!namespace XLib.XLeveling
