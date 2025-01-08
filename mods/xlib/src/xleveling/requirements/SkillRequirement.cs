using System;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a skill level that must be reached to learn an ability.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class SkillRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "skill";

        /// <summary>
        /// Gets the skill.
        /// </summary>
        /// <value>
        /// The skill.
        /// </value>
        public Skill Skill { get; private set; }

        /// <summary>
        /// Gets the required level.
        /// </summary>
        /// <value>
        /// The required level.
        /// </value>
        public int RequiredLevel { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillRequirement"/> class.
        /// </summary>
        public SkillRequirement() : base()
        {
            this.Skill = null;
            this.RequiredLevel = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillRequirement" /> class.
        /// </summary>
        /// <param name="skill">The skill.</param>
        /// <param name="requiredLevel">The required level.</param>
        /// <param name="minimumTier">The minimum tier this requirement is required for.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown when skill is <c>null</c>.</exception>
        public SkillRequirement(Skill skill, int requiredLevel = 5, int minimumTier = 1, bool hideAbilityUntilFulfilled = false) : base()
        {
            this.Skill = skill ?? throw new ArgumentNullException("The skill of a skill requirement must not be null.");
            this.RequiredLevel = requiredLevel;
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
            this.RequiredLevel = (int)tree.GetLong("requiredLevel", 0);

            this.Skill = toResolve.GetSkill(tree.GetString("skill"));
            if (this.Skill == null) return false;
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
            if (tier < this.MinimumTier) return true;
            if (this.Skill.Id == -1 || playerAbility == null)
            {
                return false;
            }
            PlayerSkillSet playerSkillSet = playerAbility.PlayerSkill.PlayerSkillSet;
            if (playerSkillSet == null || playerSkillSet.PlayerSkills.Count <= this.Skill.Id) return false;
            return playerSkillSet.PlayerSkills[this.Skill.Id].Level >= this.RequiredLevel;
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
        ///   a Description that describes the requirement for the given player ability.
        /// </returns>
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            if (playerAbility == null) return "";
            PlayerSkillSet playerSkillSet = playerAbility.PlayerSkill.PlayerSkillSet;
            if (this.Skill.Id == -1 || playerSkillSet.PlayerSkills.Count <= this.Skill.Id)
            {
                return "";
            }
            return this.Skill.DisplayName + " " + playerSkillSet.PlayerSkills[this.Skill.Id]?.Level + "/" + this.RequiredLevel;
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
    }//!class SkillRequirement
}//!namespace XLib.XLeveling
