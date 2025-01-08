using System;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents an ability tier from an another ability that must be reached to learn an ability.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class AbilityRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "ability";

        /// <summary>
        /// Gets the required ability.
        /// </summary>
        /// <value>
        /// The ability.
        /// </value>
        public Ability Ability { get; protected set; }

        /// <summary>
        /// Gets the required tier.
        /// </summary>
        /// <value>
        /// The required tier.
        /// </value>
        public int RequiredTier { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityRequirement"/> class.
        /// </summary>
        public AbilityRequirement() : base()
        {
            this.Ability = null;
            this.RequiredTier = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityRequirement" /> class.
        /// </summary>
        /// <param name="ability">The ability.</param>
        /// <param name="requiredTier">The required tier.</param>
        /// <param name="minimumTier">The minimum tier this requirement is required for.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability is <c>null</c>.</exception>
        public AbilityRequirement(Ability ability, int requiredTier, int minimumTier = 1, bool hideAbilityUntilFulfilled = false) : base()
        {
            this.Ability = ability ?? throw new ArgumentNullException("The ability of an ability requirement must not be null.");
            this.RequiredTier = requiredTier;
            this.MinimumTier = minimumTier;
            this.HideAbilityUntilFulfilled = hideAbilityUntilFulfilled;
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
            this.RequiredTier = (int)tree.GetLong("requiredTier", 0);

            this.Ability = toResolve.GetSkill(tree.GetString("skill"))?.FindAbility(tree.GetString("ability"));
            if (this.Ability == null) return false;
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
        public override bool IsFulfilled(PlayerAbility playerAbility, int tier)
        {
            if (this.Ability.Id == -1 || this.Ability.Skill.Id == -1)
            {
                return false;
            }
            if (tier < this.MinimumTier) return true;
            PlayerSkillSet playerSkillSet = playerAbility.PlayerSkill.PlayerSkillSet;
            if (playerSkillSet == null) return false;
            return playerSkillSet[this.Ability.Skill.Id]?[this.Ability.Id]?.Tier >= this.RequiredTier;
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
                playerAbility.Tier = this.MinimumTier - 1;
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
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            PlayerSkillSet playerSkillSet = playerAbility?.PlayerSkill?.PlayerSkillSet;
            if (this.Ability.Id == -1 || playerSkillSet == null || this.Ability.Skill.Id == -1)
            {
                return "";
            }
            PlayerAbility required = playerSkillSet[this.Ability.Skill.Id]?[this.Ability.Id];
            if (required == null) return "";
            return this.Ability.DisplayName + " " + required.Tier + "/" + this.RequiredTier;
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        ///   the Type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            return EnumRequirementType.MediumRequirement;
        }
    }//!class AbilityRequirement
}//!namespace XLib.XLeveling
