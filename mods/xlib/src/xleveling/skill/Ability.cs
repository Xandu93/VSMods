using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Vintagestory.API.Common;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents an ability in general.
    /// </summary>
    public class Ability
    {
        /// <summary>
        /// Gets the internal name of this ability. Used to save, load and identify this ability.
        /// This should always be the same and must be unique in its skill.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the name that is displayed in the game.
        /// Can be localized.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description of the ability. This can contain placeholders for string interpolation.
        /// The string interpolation will use the values from the values array.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets the internal identifier for this ability. This will be set by the skill this ability belongs to.
        /// Is used to quickly identify this ability within a skill and is only unique within this ability. 
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets or sets the skill this ability  belongs to.
        /// </summary>
        /// <value>
        /// The skill.
        /// </value>
        public Skill Skill { get; internal set; }

        /// <summary>
        /// Gets or sets the minimum skill level that is required to learn this ability.
        /// This does not affect players who are ingame and have already learned this ability.
        /// Set this value before any player joins the world.
        /// </summary>
        /// <value>
        /// The minimum level.
        /// </value>
        public int MinLevel { get; set; }

        /// <summary>
        /// The maximum tier of this ability.
        /// </summary>
        private int maxTier;

        /// <summary>
        /// Gets or sets the maximum tier of this ability.
        /// If you change the tier of an ability you must also adjust the value array!
        /// It resizes the array. But you have to fill in additional values eventually.
        /// </summary>
        /// <value>
        /// The maximum tier.
        /// </value>
        public int MaxTier
        {
            get { return this.maxTier; }
            set
            {
                this.maxTier = Math.Max(value, 0);
                if (this.ValuesPerTier > 0)
                {
                    int[] newArray = new int[this.ValuesPerTier * this.MaxTier];
                    for (int index = 0; index < this.Values.Length && index < newArray.Length; index++)
                    {
                        newArray[index] = this.Values[index];
                    }
                    this.Values = newArray;
                }
            }
        }

        /// <summary>
        /// Gets how many values per tier exists.
        /// </summary>
        /// <value>
        /// The values per tier.
        /// </value>
        public int ValuesPerTier { get; private set; }

        /// <summary>
        /// Gets the values that are specific for this ability.
        /// Can be modified by mods and in the default generated config files and is used for dynamic description string generation.
        /// The array stores a set of integers for every ability tier.
        /// The index of a specific value is calculated by: ([tier] - 1) * [valuesPerTier] + [id]
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        public int[] Values { get; private set; }

        /// <summary>
        /// Gets the requirements of this ability.
        /// This can be one of the defined requirements or you implement your own requirement.
        /// You can use the AddRequirement method to add a new requirement.
        /// </summary>
        /// <value>
        /// The requirements.
        /// </value>
        public List<Requirement> Requirements { get; private set; }

        /// <summary>
        /// Gets or sets a method that is called every time a tier of a player ability changed.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public OnPlayerAbilityTierChangedDelegate OnPlayerAbilityTierChanged { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Ability"/> is enabled.
        /// Disabled abilities are always tier zero and are hidden in the user interface.
        /// Set this value before any player joins the world.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this ability is hidden until all requirements are fulfilled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this ability is hidden until all requirements are fulfilled; otherwise, <c>false</c>.
        /// </value>
        public bool HideUntilLearnable { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ability" /> class.
        /// All integers of the values array are set to zero.
        /// </summary>
        /// <param name="name">The internal name of this ability. Used to save, load and identify this ability.
        /// This should always be the same and must be unique in its skill.</param>
        /// <param name="displayName">The name that is displayed in the game.
        /// Can be localized.</param>
        /// <param name="description">The description of the ability. This can contain placeholders for string interpolation.
        /// The string interpolation will use the values from the values array.</param>
        /// <param name="minLevel">The minimum skill level that is required to learn this ability.</param>
        /// <param name="maxTier">The maximum tier of this ability.</param>
        /// <param name="valuesPerTier">The number of values per tier that exists.</param>
        /// <param name="hideUntilLearnable">if set to <c>true</c> this ability is hidden until all requirements are fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if name is <c>null</c>.</exception>
        public Ability(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
        {
            this.Name = name ?? throw new ArgumentNullException("An ability name must not be null.");
            this.DisplayName = displayName ?? this.Name;
            this.Description = description ?? this.DisplayName;
            this.MinLevel = Math.Max(minLevel, 0);
            this.Enabled = true;
            this.MaxTier = maxTier;
            this.Id = -1;
            this.ValuesPerTier = valuesPerTier;
            this.Requirements = new List<Requirement>();
            this.HideUntilLearnable = hideUntilLearnable;
            this.Values = new int[this.ValuesPerTier * this.MaxTier];

            for (int ii = 0; ii < this.Values.Length; ii++)
            {
                this.Values[ii] = 0;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ability" /> class.
        /// </summary>
        /// <param name="name">The internal name of this ability. Used to save, load and identify this ability.
        /// This should always be the same and must be unique in its skill.</param>
        /// <param name="displayName">The name that is displayed in the game.
        /// Can be localized.</param>
        /// <param name="description">The description of the ability. This can contain placeholders for string interpolation.
        /// The string interpolation will use the values from the values array.</param>
        /// <param name="minLevel">The minimum skill level that is required to learn this ability.</param>
        /// <param name="maxTier">The maximum tier of this ability.</param>
        /// <param name="values">The values that are specific for this ability.
        /// Can be modified by mods and in the default generated config files and is used for dynamic description string generation.
        /// The array stores a set of integers for every ability tier.
        /// The index of a specific value is calculated by: ([tier] - 1) * [valuesPerTier] + [id]</param>
        /// <param name="hideUntilLearnable">if set to <c>true</c> this ability is hidden until all requirements are fulfilled.</param>
        /// Values are copied.
        /// <exception cref="ArgumentNullException">Is thrown if name is <c>null</c>.</exception>
        public Ability(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
        {
            this.Name = name ?? throw new ArgumentNullException("An ability name must not be null.");
            this.DisplayName = displayName ?? this.Name;
            this.Description = description ?? this.DisplayName;
            this.MinLevel = Math.Max(minLevel, 0);
            this.Enabled = true;
            this.MaxTier = maxTier;
            this.Id = -1;
            this.ValuesPerTier = values.Length / maxTier;
            this.Requirements = new List<Requirement>();
            this.HideUntilLearnable = hideUntilLearnable;
            this.Values = new int[this.ValuesPerTier * this.MaxTier];

            for (int ii = 0; ii < this.Values.Length; ii++)
            {
                this.Values[ii] = values[ii];
            }
        }

        /// <summary>
        /// Returns the value with the index ([tier] - 1) * [valuesPerTier] + [id]
        /// </summary>
        /// <param name="tier">The tier.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>
        ///   the value with the index ([tier] - 1) * [valuesPerTier] + [id] if the index is valid; otherwise, 0
        /// </returns>
        public int Value(int tier, int id)
        {
            int index = (tier - 1) * ValuesPerTier + id;
            if (index >= this.Values.Length)
            {
                return 0;
            }
            return this.Values[index];
        }

        /// <summary>
        /// Sets the value with the index ([tier] - 1) * [valuesPerTier] + [id]
        /// </summary>
        /// <param name="tier">The tier.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the index is valid; otherwise, <c>false</c>.
        /// </returns>
        public bool SetValue(int tier, int id, int value)
        {
            int index = (tier - 1) * ValuesPerTier + id;
            if (index < this.Values.Length)
            {
                this.Values[index] = value;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a formatted description.
        /// Override this method to create a custom description.
        /// </summary>
        /// <param name="tier">The tier for which the description should be generated.</param>
        /// <returns>
        ///   a formatted description
        /// </returns>
        public virtual string FormattedDescription(int tier)
        {
            tier = Math.Min(this.MaxTier, Math.Max(tier, 1));
            int begin = this.ValuesPerTier * (tier - 1);
            try
            {
                switch (this.ValuesPerTier)
                {
                    case 1: return String.Format(this.Description, this.Values[begin]);
                    case 2: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1]);
                    case 3: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2]);
                    case 4: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2], this.Values[begin + 3]);
                    case 5: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2], this.Values[begin + 3], this.Values[begin + 4]);
                    case 6: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2], this.Values[begin + 3], this.Values[begin + 4], this.Values[begin + 5]);
                    case 7: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2], this.Values[begin + 3], this.Values[begin + 4], this.Values[begin + 5], this.Values[begin + 6]);
                    case 8: return String.Format(this.Description, this.Values[begin], this.Values[begin + 1], this.Values[begin + 2], this.Values[begin + 3], this.Values[begin + 4], this.Values[begin + 5], this.Values[begin + 6], this.Values[begin + 7]);
                    default: return this.Description;
                }
            }
            catch(Exception error)
            {
                this.Skill.XLeveling.Api.Logger.Error(error.Message + "[" + "Ability: " + this.Name +"]");
                return this.Description;
            }
        }

        /// <summary>
        /// Called when a associated player ability changed its tier.
        /// Default behavior just calls the OnPlayerAbilityTierChanged delegate if one exists.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <param name="oldTier">The old tier.</param>
        public virtual void OnTierChanged(PlayerAbility playerAbility, int oldTier)
        {
            this.OnPlayerAbilityTierChanged?.Invoke(playerAbility, oldTier);
        }

        /// <summary>
        /// Adds a requirement.
        /// This can be one of the defined requirements or you implement your own <see cref="Requirement" />.
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
        ///   <c>true</c> if the requirement has been successfully removed; otherwise, <c>false</c>.
        /// </returns>
        public bool RemoveRequirement(Requirement requirement)
        {
            return this.Requirements.Remove(requirement);
        }

        /// <summary>
        /// Determines whether the specified player ability is visible.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   <c>true</c> if the specified player ability is visible; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsVisible(PlayerAbility playerAbility)
        {
            if (playerAbility.Tier > 0) return true;
            if (!this.Enabled || !this.Skill.Enabled) return false;
            foreach(Requirement requirement in this.Requirements)
            {
                if (!requirement.IsFulfilled(playerAbility, 1))
                {
                    if (this.HideUntilLearnable || requirement.HideAbilityUntilFulfilled) return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Fills this abilities values from a ability configuration.
        /// </summary>
        /// <param name="config">The ability configuration.</param>
        /// <exception cref="ArgumentNullException">Is thrown if config is <c>null</c>.</exception>
        public virtual void FromConfig(AbilityConfig config)
        {
            if (config == null) throw new ArgumentNullException("The skill config must not be null.");
            if (config.name != this.Name) throw new ArgumentException("Ability configuration mismatch. Server and client could run on different versions of the mod.");

            if (config.maxTier * this.ValuesPerTier != config.values.Length)
            {
                this.Skill.XLeveling.Mod.Logger.Log(EnumLogType.Warning, "Error while loading ability configuration for: " + this.Name + ": bad number of values. Uses default values.");
                return;
            }

            this.MaxTier = config.maxTier;
            if (config.minLevel != 0) this.MinLevel = Math.Max(config.minLevel, 1);
            if (config.values != null)
            {
                for (int index = 0; index < config.values.Length && index < this.Values.Length; index++)
                {
                    this.Values[index] = config.values[index];
                }
            }
            this.Enabled = config.enabled;
        }

        /// <summary>
        /// Returns the required skill level for the given ability tier.
        /// This method also includes requirements into the determination.
        /// </summary>
        /// <param name="tier"></param>
        /// <returns>the required skill level for the given ability tier</returns>
        public virtual int RequiredLevel(int tier)
        {
            int level = MinLevel + Math.Min(tier, MaxTier) - 1;

            foreach(Requirement requirement in Requirements)
            {
                SkillRequirement skillRequirement = requirement as SkillRequirement;
                if (skillRequirement == null) continue;
                if (skillRequirement.Skill == Skill && skillRequirement.MinimumTier <= tier)
                {
                    level = Math.Max(level, skillRequirement.RequiredLevel);
                }
            }

            return level;
        }
    }//!class Ability

    /// <summary>
    /// Is called every time a tier of a player ability changed.
    /// </summary>
    /// <param name="ability">The ability.</param>
    /// <param name="oldTier">The old tier.</param>
    public delegate void OnPlayerAbilityTierChangedDelegate(PlayerAbility ability, int oldTier);

    /// <summary>
    /// The configuration of an ability.
    /// </summary>
    [ProtoContract]
    public class AbilityConfig
    {
        /// <summary>
        /// The internal name of the ability.
        /// </summary>
        [ProtoMember(1)]
        public string name;

        /// <summary>
        /// The internal identifier for the ability.
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(-1)]
        public int id;

        /// <summary>
        /// The maximum tier of the ability.
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(1)]
        public int maxTier;

        /// <summary>
        /// The minimum skill level you need to learn the ability.
        /// </summary>
        [ProtoMember(4)]
        [DefaultValue(0)]
        public int minLevel;

        /// <summary>
        /// <c>true</c> if the ability is enabled; otherwise, <c>false</c>.
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue(true)]
        public bool enabled;

        /// <summary>
        /// The values that are specific for this ability.
        /// </summary>
        [ProtoMember(6)]
        public int[] values;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityConfig"/> class.
        /// </summary>
        public AbilityConfig()
        {
            this.name = "";
            this.id = -1;
            this.maxTier = 1;
            this.minLevel = 1;
            this.enabled = true;
            this.values = new int[0];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityConfig"/> class.
        /// </summary>
        /// <param name="ability">The ability.</param>
        public AbilityConfig(Ability ability)
        {
            this.name = ability.Name;
            this.id = ability.Id;
            this.maxTier = ability.MaxTier;
            this.minLevel = ability.MinLevel;
            this.enabled = ability.Enabled;
            this.values = ability.Values;
        }
    }//!class AbilityConfig
}//!namespace XLib.XLeveling
