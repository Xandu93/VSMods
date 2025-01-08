using System;
using Newtonsoft.Json;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents an ability associated to a player.
    /// </summary>
    public class PlayerAbility
    {
        /// <summary>
        /// Gets the ability this player ability is associated to.
        /// </summary>
        /// <value>
        /// The ability.
        /// </value>
        public Ability Ability { get; private set; }

        /// <summary>
        /// Gets the player skill this player ability is associated to.
        /// </summary>
        /// <value>
        /// The player skill.
        /// </value>
        public PlayerSkill PlayerSkill { get; private set; }

        /// <summary>
        /// Decides which requirements should be ignored when increasing tiers.
        /// </summary>
        public EnumRequirementType IgnoredRequirements { get; set; }

        /// <summary>
        /// The current tier of the player ability.
        /// </summary>
        private int tier;

        /// <summary>
        /// Gets the current tier of the player ability.
        /// </summary>
        /// <value>
        /// The tier.
        /// </value>
        public int Tier
        {
            get { return this.tier; }
            internal set
            {
                if (!this.Ability.Enabled || !this.Ability.Skill.Enabled) value = 0;
                if ((this.IgnoredRequirements & EnumRequirementType.LevelRequirement) == 0)
                {
                    value = Math.Min(value, this.PlayerSkill.Level - (this.Ability.MinLevel - 1));
                }

                //shrinks the tier in its limits
                value = Math.Max(Math.Min(Math.Min(value, this.Ability.MaxTier), this.PlayerSkill.AbilityPoints + this.tier), 0);
                if (value == this.tier) return;

                //check requirements
                if(value > this.tier)
                {
                    foreach (Requirement requirement in this.Ability.Requirements)
                    {
                        if ((requirement.RequirementType() & this.IgnoredRequirements) == 0 && !requirement.IsFulfilled(this, value))
                        {
                            return;
                        }
                    }
                }
                //set new tier
                this.PlayerSkill.AbilityPoints -= value - this.tier;
                int oldTier = this.tier;
                this.tier = value;
                this.Ability.OnTierChanged(this, oldTier);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAbility" /> class.
        /// </summary>
        /// <param name="ability">The ability this player ability is associated to.</param>
        /// <param name="playerSkill">The player skill this player ability is associated to.</param>
        /// <param name="tier">The current tier of the player ability.</param>
        /// <exception cref="ArgumentNullException">Is thrown if ability or playerSkill is <c>null</c>.</exception>
        public PlayerAbility(Ability ability, PlayerSkill playerSkill, int tier = 0)
        {
            this.Ability = ability ?? throw new ArgumentNullException("The ability of a player ability must not be null.");
            this.PlayerSkill = playerSkill ?? throw new ArgumentNullException("The player skill of a player ability must not be null."); ;
            this.Tier = tier;
            this.IgnoredRequirements = EnumRequirementType.None;
        }

        /// <summary>
        /// Returns the value with the index ([tier] - 1) * [valuesPerTier] + [id]
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// the value with the index ([tier] - 1) * [valuesPerTier] + [id] if the index is valid; the default value if the tier is 0; otherwise, 0
        /// </returns>
        public int Value(int id, int defaultValue = 0)
        {
            if(this.Tier <= 0) return defaultValue;
            return this.Ability.Value(this.Tier, id);
        }

        /// <summary>
        /// Returns the value with the index ([tier] - 1) * [valuesPerTier] + [id] multiplied by 0.01
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>
        /// the value with the index ([tier] - 1) * [valuesPerTier] + [id] multiplied by 0.01 if the index is valid; the default value if the tier is 0; otherwise, 0
        /// </returns>
        public float FValue(int id, float defaultValue = 0.0f)
        {
            if (this.Tier <= 0) return defaultValue;
            return this.Ability.Value(this.Tier, id) * 0.01f;
        }

        /// <summary>
        /// Returns a skill dependent value.
        /// With following equation:
        /// Min(
        /// Value(([tier] - 1) * [valuesPerTier]) +
        /// Value(([tier] - 1) * [valuesPerTier] + 1) * [Skill Level],
        /// Value(([tier] - 1) * [valuesPerTier] + 2);
        /// The ability must have 3 or more values per tier for this method to work.
        /// </summary>
        /// <param name="skillLevel">The skill level. If set to 0 the method will use the skill level of the skill this ability belongs to.</param>
        /// <returns>
        /// a skill dependent value if the player ability can calculate one; otherwise, 0.
        /// </returns>
        public int SkillDependentValue(int skillLevel = 0)
        {
            if(this.Tier <= 0 || this.Ability.ValuesPerTier < 3) return 0;
            
            if(skillLevel == 0)
                return Math.Min(
                    this.Ability.Value(this.Tier, 0) + 
                    this.Ability.Value(this.Tier, 1) * PlayerSkill.Level, 
                    this.Ability.Value(this.Tier, 2));
            else
                return Math.Min(
                    this.Ability.Value(this.Tier, 0) + 
                    this.Ability.Value(this.Tier, 1) * skillLevel, 
                    this.Ability.Value(this.Tier, 2));
        }

        /// <summary>
        /// Returns a skill dependent value.
        /// With following equation:
        /// Min(
        /// Value(([tier] - 1) * [valuesPerTier]) +
        /// Value(([tier] - 1) * [valuesPerTier] + 1) * [Skill Level],
        /// Value(([tier] - 1) * [valuesPerTier] + 2) * 0.01;
        /// The ability must have 3 or more values per tier for this method to work.
        /// </summary>
        /// <param name="skillLevel">The skill level. If set to 0 the method will use the skill level of the skill this ability belongs to.</param>
        /// <returns>
        /// a skill dependent value if the player ability can calculate one; otherwise, 0.
        /// </returns>

        public float SkillDependentFValue(int skillLevel = 0)
        {
            return this.SkillDependentValue(skillLevel) * 0.01f;
        }

        /// <summary>
        /// Sets the tier of this ability.
        /// Use this method to set a tier.
        /// </summary>
        /// <param name="tier">The tier.</param>
        virtual public void SetTier(int tier)
        {
            if (tier < 0) return;
            this.PlayerSkill.Skill.XLeveling.IXLevelingAPI.SetAbilityTier(
                this.PlayerSkill.PlayerSkillSet.Player, this.PlayerSkill.Skill.Id,
                this.Ability.Id, tier, true);
        }

        /// <summary>
        /// Is called after all skills and abilities are loaded to ensure that all requirements except weak requirements are fulfilled.
        /// This functions is also called after a ability tier is decreased to check whether other requirements are no longer fulfilled.
        /// </summary>
        /// <returns>
        ///   whether this function changed the tier.
        /// </returns>
        public bool CheckRequirements()
        {
            bool result = false;
            if (this.Tier == 0) return result;
            foreach (Requirement requirement in this.Ability.Requirements)
            {
                if ((requirement.RequirementType() & this.IgnoredRequirements) != 0) continue;
                if (!requirement.IsFulfilled(this, this.Tier))
                {
                    if(requirement.ResolveConflict(this)) result = true;
                    if (this.Tier == 0) return result;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks whether all requirements for a specific tier are fulfilled.
        /// </summary>
        /// <param name="tier">The tier to check.</param>
        /// <returns>
        ///   whether all requirements are fulfilled.
        /// </returns>
        public bool RequirementsFulfilled(int tier)
        {
            if (this.PlayerSkill.AbilityPoints == 0) return false;
            if (this.Ability.MaxTier < tier) return false;
            if ((this.IgnoredRequirements & EnumRequirementType.LevelRequirement) == 0)
            {
                if (this.PlayerSkill.Level < (this.Ability.MinLevel + tier - 1))
                {
                    return false;
                }
            }

            //check requirements
            foreach (Requirement requirement in this.Ability.Requirements)
            {
                if ((requirement.RequirementType() & this.IgnoredRequirements) == 0 && !requirement.IsFulfilled(this, tier))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines whether this player ability is visible.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this player ability is visible; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsVisible()
        {
            return this.Ability.IsVisible(this);
        }
    }//!class PlayerAbility

    /// <summary>
    /// Used to save player abilities.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SavedPlayerAbility
    {
        /// <summary>
        /// The tier of the ability.
        /// </summary>
        [JsonProperty]
        public int Tier;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerAbility"/> class.
        /// </summary>
        public SavedPlayerAbility() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerAbility"/> class.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        public SavedPlayerAbility(PlayerAbility playerAbility)
        {
            this.Tier = playerAbility.Tier;
        }
    }//!class SavedPlayerAbility
}//!namespace XLib.XLeveling
