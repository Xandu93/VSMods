using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Skills with this requirement are limited.
    /// Limits the amount of skills of a specific set.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class LimitationRequirement : Requirement
    {
        /// <summary>
        /// A list of all abilities that are affected by this requirement.
        /// </summary>
        private List<Ability> abilities;

        /// <summary>
        /// The name
        /// </summary>
        protected string name;

        /// <summary>
        /// Gets or sets the name of this limitation.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name { get => name; }

        /// <summary>
        /// Gets or sets the limit.
        /// </summary>
        /// <value>
        /// The limit.
        /// </value>
        public int Limit { get; set; }

        /// <summary>
        /// Gets or sets the an ability that can modify the limit.
        /// </summary>
        /// <value>
        /// The modifier ability.
        /// </value>
        public Ability ModifierAbility { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LimitationRequirement"/> class.
        /// </summary>
        public LimitationRequirement()
        {
            this.abilities = new List<Ability>();
            this.name = "Specialisations";
            this.Limit = 1;
            this.ModifierAbility = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LimitationRequirement" /> class.
        /// </summary>
        /// <param name="limit">The limit.</param>
        /// <param name="name">The name.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        public LimitationRequirement(int limit = 1, string name = null, bool hideAbilityUntilFulfilled = false) : base()
        {
            this.abilities = new List<Ability>();
            this.name = name ?? "Specialisations";
            this.Limit = limit;
            this.ModifierAbility = null;
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
            this.Limit = (int)tree.GetLong("limit", this.Limit);
            this.name = tree.GetString("name", this.name);

            TreeAttribute modifier = tree.GetTreeAttribute("modifierAbility") as TreeAttribute;
            if (modifier != null)
            {
                this.ModifierAbility = toResolve.GetSkill(modifier.GetString("skill"))?.FindAbility(modifier.GetString("ability"));
                if (this.ModifierAbility == null) return false;
            }
            return true;
        }

        /// <summary>
        /// Adds a ability to this requirement.
        /// Also adds this to the requirements of an ability.
        /// </summary>
        /// <param name="ability">a ability.</param>
        public void AddAbility(Ability ability)
        {
            this.abilities.Add(ability);
            ability.AddRequirement(this);
            return;
        }

        /// <summary>
        /// Determines whether the specified ability is contained in this requirement.
        /// </summary>
        /// <param name="ability">The ability.</param>
        /// <returns>
        ///   <c>true</c> if the specified ability is contained in this requirement; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsAbility(Ability ability)
        {
            return this.abilities.Contains(ability);
        }

        /// <summary>
        /// Counts the number of limited abilities the player has learned.
        /// </summary>
        /// <param name="skillSet">The player skill set.</param>
        /// <returns></returns>
        public virtual int CountAbilities(PlayerSkillSet skillSet)
        {
            if (skillSet == null) return -1;
            int counter = 0;

            foreach (Ability ability in this.abilities)
            {
                counter += skillSet[ability.Skill.Id]?[ability.Id]?.Tier ?? 0;
            }
            return counter;
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
            PlayerAbility modifier = this.ModifierAbility != null ? playerAbility?.PlayerSkill.PlayerSkillSet[this.ModifierAbility.Skill.Id]?[this.ModifierAbility.Id] : null;
            int limit = modifier != null ? this.Limit + modifier.Value(0) : this.Limit;
            return this.CountAbilities(playerAbility?.PlayerSkill.PlayerSkillSet) - playerAbility.Tier + tier <= limit;
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
            PlayerSkillSet skillSet = playerAbility?.PlayerSkill.PlayerSkillSet;
            if (skillSet == null) return false;

            foreach (Ability ability in this.abilities)
            {
                PlayerAbility playerAbility2 = skillSet[ability.Skill.Id]?[ability.Id];
                if (playerAbility2 != null) playerAbility2.Tier = 0;
            }
            return true;
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
            PlayerAbility modifier = this.ModifierAbility != null ? playerAbility?.PlayerSkill.PlayerSkillSet[this.ModifierAbility.Skill.Id]?[this.ModifierAbility.Id] : null;
            int limit = modifier != null ? this.Limit + modifier.Value(0) : this.Limit;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(Lang.GetUnformatted("xlib:limited") + " (" + CountAbilities(playerAbility?.PlayerSkill.PlayerSkillSet) + "/" + limit + ") " + Name + ": ");

            foreach (Ability ability in this.abilities)
            {
                stringBuilder.Append("\n\t");
                stringBuilder.Append(ability.DisplayName);
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        /// the Type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            return EnumRequirementType.MediumRequirement;
        }
    }//!class LimitationRequirement
}//!namespace XLib.XLeveling
