using System;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents an ability that can't be learned simultaneously with another ability.
    /// You must add this to both abilities to achieve mutually exclusivity.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class ExclusiveAbilityRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "exclusiveAbility";

        /// <summary>
        /// Gets the exclusive ability.
        /// </summary>
        /// <value>
        /// The ability.
        /// </value>
        public Ability Ability { get; private set; }

        /// <summary>
        /// Gets the tier of the ability that is exclusive to this ability tier.
        /// </summary>
        /// <value>
        /// The minimum tier.
        /// </value>
        public int ExclusiveTier { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement"/> class.
        /// </summary>
        public ExclusiveAbilityRequirement() : base()
        {
            this.Ability = null;
            this.ExclusiveTier = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement" /> class.
        /// </summary>
        /// <param name="ability">The ability.</param>
        /// <param name="minimumTier">The minimum tier.</param>
        /// <param name="exclusiveTier">The exclusive tier.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability is <c>null</c>.</exception>
        public ExclusiveAbilityRequirement(Ability ability, int minimumTier = 1, int exclusiveTier = 1, bool hideAbilityUntilFulfilled = false) : base()
        {
            this.Ability = ability ?? throw new ArgumentNullException("The ability of a exclusive ability requirement must not be null.");
            this.MinimumTier = minimumTier;
            this.ExclusiveTier = exclusiveTier;
            this.HideAbilityUntilFulfilled = hideAbilityUntilFulfilled;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement"/> class and adds this as a requirement to it.
        /// Creates a secound instance of the <see cref="ExclusiveAbilityRequirement"/> class that is added as a requirement to the ability2.
        /// Creates two mutually excluding abilities.
        /// </summary>
        /// <param name="ability1">The first ability.</param>
        /// <param name="ability2">The second ability.</param>
        /// <param name="minimumTier1">The minimum tier1.</param>
        /// <param name="minimumTier2">The minimum tier2.</param>
        /// <param name="exclusiveTier1">The exclusive tier1.</param>
        /// <param name="exclusiveTier2">The exclusive tier2.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability1 or ability2 is <c>null</c>.</exception>
        public ExclusiveAbilityRequirement(Ability ability1, Ability ability2, int minimumTier1, int minimumTier2, int exclusiveTier1, int exclusiveTier2, bool hideAbilityUntilFulfilled = false)
        {
            if (ability1 == null || ability2 == null) throw new ArgumentNullException("The ability of a exclusive ability requirement must not be null.");

            ExclusiveAbilityRequirement other = new ExclusiveAbilityRequirement(ability2, minimumTier2, exclusiveTier2, hideAbilityUntilFulfilled);

            this.Ability = ability1;
            this.MinimumTier = minimumTier1;
            this.ExclusiveTier = exclusiveTier1;
            this.HideAbilityUntilFulfilled = hideAbilityUntilFulfilled;

            ability1.AddRequirement(other);
            ability2.AddRequirement(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement"/> class and adds this as a requirement to it.
        /// Creates a secound instance of the <see cref="ExclusiveAbilityRequirement"/> class that is added as a requirement to the ability2.
        /// Creates two mutually excluding abilities.
        /// </summary>
        /// <param name="ability1">The ability1.</param>
        /// <param name="ability2">The ability2.</param>
        /// <param name="minimumTier1">The minimum tier1.</param>
        /// <param name="minimumTier2">The minimum tier2.</param>
        /// <param name="exclusiveTier">The exclusive tier.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability1 or ability2 is <c>null</c>.</exception>
        public ExclusiveAbilityRequirement(Ability ability1, Ability ability2, int minimumTier1, int minimumTier2, int exclusiveTier, bool hideAbilityUntilFulfilled = false) :
            this(ability1, ability2, minimumTier1, minimumTier2, exclusiveTier, exclusiveTier, hideAbilityUntilFulfilled)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement"/> class and adds this as a requirement to it.
        /// Creates a secound instance of the <see cref="ExclusiveAbilityRequirement"/> class that is added as a requirement to the ability2.
        /// Creates two mutually excluding abilities.
        /// </summary>
        /// <param name="ability1">The ability1.</param>
        /// <param name="ability2">The ability2.</param>
        /// <param name="minimumTier">The minimum tier.</param>
        /// <param name="exclusiveTier">The exclusive tier.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability1 or ability2 is <c>null</c>.</exception>
        public ExclusiveAbilityRequirement(Ability ability1, Ability ability2, int minimumTier, int exclusiveTier, bool hideAbilityUntilFulfilled = false) :
           this(ability1, ability2, minimumTier, minimumTier, exclusiveTier, hideAbilityUntilFulfilled)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExclusiveAbilityRequirement"/> class and adds this as a requirement to it.
        /// Creates a secound instance of the <see cref="ExclusiveAbilityRequirement"/> class that is added as a requirement to the ability2.
        /// Creates two mutually excluding abilities.
        /// </summary>
        /// <param name="ability1">The ability1.</param>
        /// <param name="ability2">The ability2.</param>
        /// <param name="exclusiveMinimumTier">The minimum and exclusive tier.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability1 or ability2 is <c>null</c>.</exception>
        public ExclusiveAbilityRequirement(Ability ability1, Ability ability2, int exclusiveMinimumTier, bool hideAbilityUntilFulfilled = false) :
            this(ability1, ability2, exclusiveMinimumTier, exclusiveMinimumTier, hideAbilityUntilFulfilled)
        { }

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
            this.ExclusiveTier = (int)tree.GetLong("exclusiveTier", 0);

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
                return true;
            }
            if (tier < this.MinimumTier) return true;
            PlayerSkillSet playerSkillSet = playerAbility?.PlayerSkill?.PlayerSkillSet;
            if (playerSkillSet == null) return false;
            return !(playerSkillSet[this.Ability.Skill.Id]?[this.Ability.Id]?.Tier >= this.ExclusiveTier);
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
            PlayerAbility exclusive = playerAbility?.PlayerSkill.PlayerSkillSet[this.Ability.Skill.Id]?[this.Ability.Id];
            if (exclusive != null)
            {
                playerAbility.Tier = this.MinimumTier - 1;
                exclusive.Tier = this.ExclusiveTier - 1;
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
            string str = "";
            str += Lang.GetUnformatted("xlib:exclusivelywith") + this.Ability.DisplayName;

            if (this.ExclusiveTier > 1)
            {
                str += " " + Lang.GetUnformatted("xlib:tier") + " " + this.ExclusiveTier;
            }
            return str;
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
    }//!class ExclusiveAbilityRequirement
}//!namespace XLib.XLeveling
