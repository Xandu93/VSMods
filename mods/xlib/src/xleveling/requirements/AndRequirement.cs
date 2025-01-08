using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents two or more requirements that must be met at the same time.
    /// Note that you don`t need this requirement in the top level requirements because they are already all needed.
    /// But you can use it as a sub requirement in an <see cref="OrRequirement" />.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class AndRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "and";

        /// <summary>
        /// Gets the requirements.
        /// </summary>
        /// <value>
        /// The requirements.
        /// </value>
        public List<Requirement> Requirements { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndRequirement"/> class.
        /// </summary>
        public AndRequirement() : base()
        {
            this.Requirements = new List<Requirement>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AndRequirement"/> class.
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        public AndRequirement(params Requirement[] requirements) : base()
        {
            this.Requirements = new List<Requirement>();
            if (requirements == null) return;
            for (int ii = 0; ii < requirements.Length; ii++)
            {
                this.Requirements.Add(requirements[ii]);
            }
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
            ArrayAttribute<TreeAttribute> requirments = tree.GetAttribute("requirements") as ArrayAttribute<TreeAttribute>;
            if (requirments == null) return false;

            foreach (TreeAttribute requirementAttributes in requirments.value)
            {
                Requirement requirement = toResolve.ResolveRequirment(requirementAttributes);
                if (requirement != null) this.AddRequirement(requirement);
            }
            return true;
        }

        /// <summary>
        /// Adds a requirement.
        /// </summary>
        /// <param name="requirement">The requirement.</param>
        public void AddRequirement(Requirement requirement)
        {
            this.Requirements.Add(requirement);
        }

        /// <summary>
        /// Removes a requirement.
        /// </summary>
        /// <param name="requirement">The requirement.</param>
        /// <returns>
        ///   <c>true</c> if the specified requirement was successfully removed; otherwise, <c>false</c>.
        /// </returns>
        public bool RemoveRequirement(Requirement requirement)
        {
            return this.Requirements.Remove(requirement);
        }

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
                if (!requirement.IsFulfilled(playerAbility, tier)) return false;
            }
            return true;
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
            bool result = true;
            foreach (Requirement requirement in this.Requirements)
            {
                if (!requirement.IsFulfilled(playerAbility))
                {
                    if (!requirement.ResolveConflict(playerAbility)) result = false;
                }
            }
            return result;
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
            string str = Lang.GetUnformatted("xlib:andrequirement");
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
            EnumRequirementType requirementType = EnumRequirementType.None;
            foreach (Requirement requirement in this.Requirements)
            {
                if (requirement.RequirementType() > requirementType)
                {
                    requirementType = requirement.RequirementType();
                }
            }
            return requirementType;
        }
    }//!class AndRequirement
}//!namespace XLib.XLeveling
