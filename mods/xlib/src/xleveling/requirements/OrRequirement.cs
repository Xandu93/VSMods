using Vintagestory.API.Config;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents two or more requirements of which one must be fulfilled.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class OrRequirement : AndRequirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "or";

        /// <summary>
        /// Initializes a new instance of the <see cref="OrRequirement"/> class.
        /// </summary>
        public OrRequirement() : base()
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="OrRequirement"/> class.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        public OrRequirement(params Requirement[] requirements) : base(requirements)
        { }

        /// <summary>
        /// Determines whether the specified player ability fulfills the requirement.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <param name="tier">The tier this requirement is checked for.</param>
        /// <returns>
        ///   <c>true</c> if the specified player ability fulfills the requirement; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsFulfilled(PlayerAbility playerAbility, int tier)
        {
            if (tier == 0) tier = playerAbility.Tier;
            if (tier < this.MinimumTier) return true;
            foreach (Requirement requirement in this.Requirements)
            {
                if (requirement.IsFulfilled(playerAbility, tier)) return true;
            }
            return false;
        }

        /// <summary>
        /// This function is called when the requirement is not fulfilled after all skills are loaded and should resolve this conflict.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        /// Should return false if this conflict should be ignored and true if the conflict has been resolved.
        /// </returns>
        public override bool ResolveConflict(PlayerAbility playerAbility)
        {
            foreach (Requirement requirement in this.Requirements)
            {
                if (!requirement.IsFulfilled(playerAbility))
                {
                    if (requirement.ResolveConflict(playerAbility)) return true;
                }
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
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            string str = Lang.GetUnformatted("xlib:orrequirement");
            foreach (Requirement requirement in this.Requirements)
            {
                str += "\n\t\t" + requirement.ShortDescription(playerAbility);
            }
            return str;
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        /// the Type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            EnumRequirementType requirementType = EnumRequirementType.StrongRequirement;
            foreach (Requirement requirement in this.Requirements)
            {
                if (requirement.RequirementType() < requirementType)
                {
                    requirementType = requirement.RequirementType();
                }
            }
            return requirementType;
        }
    }//!class OrRequirement
}//!namespace XLib.XLeveling
