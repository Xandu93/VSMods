using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a skill in general.
    /// </summary>
    public class Skill
    {
        /// <summary>
        /// Gets the internal name of this skill. Used to save, load and identify this skill.
        /// This should always be the same and must be unique.
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
        /// Gets or sets the group.
        /// This is only used by the gui.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        public string Group { get; set; }

        /// <summary>
        /// The xleveling mod this skill belongs to.
        /// </summary>
        public XLeveling XLeveling { get; internal set; }

        /// <summary>
        /// Gets the internal identifier for this skill. This will be set by the XLeveling interface.
        /// Is used to quickly identify this skill and can also be used to identify the associated player skill in a player skill set.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; internal set; }

        /// <summary>
        /// Gets or sets the experience base.
        /// The base is used for the calculation of the required experience for the next level.
        /// The default calculation is [expBase] * pow([expMult], current level - 1).
        /// </summary>
        /// <value>
        /// The experience base.
        /// </value>
        public float ExpBase { get; set; }

        /// <summary>
        /// Gets or sets the experience multiplier.
        /// The multiplier is used for the calculation of the required experience for the next level.
        /// The default calculation is [expBase] * pow([expMult], current level - 1)
        /// </summary>
        /// <value>
        /// The experience multiplier.
        /// </value>
        public float ExpMult { get; set; }

        /// <summary>
        /// Gets or sets An additional value for the calculation of the required experience for the next level.
        /// This value is not used for the default calculation.
        /// </summary>
        /// <value>
        /// The exp calculate value.
        /// </value>
        public float ExpEquationValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum level that a player can reach in this skill.
        /// </summary>
        /// <value>
        /// The maximum level.
        /// </value>
        public int MaxLevel { get; set; }

        /// <summary>
        /// Gets or sets the minimum level of this skill.
        /// </summary>
        /// <value>
        /// The minimum level.
        /// </value>
        public int MinLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Skill"/> is enabled.
        /// Disabled skills are always level zero and are hidden in the user interface.
        /// Set this value before any player joins the world.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the experience loss on death.
        /// Numbers smaller than or equal to 1.0 means a percentage of the earned experience is lossed.
        /// Numbers greater than 1.0 means a percentage of total needed experience for the next level up is lossed.
        /// </summary>
        /// <value>
        /// The experience loss on death.
        /// </value>
        public float ExpLossOnDeath { get; set; }

        /// <summary>
        /// Gets or sets the maximum experience loss on death.
        /// Numbers smaller than or equal to 1.0 means a percentage of total needed experience for the next level up can be lossed.
        /// Numbers greater than 1.0 means a fixed maximum of experience can be lossed.
        /// 0.0 or smaller numbers means no maximum is set.
        /// </summary>
        /// <value>
        /// The maximum experience loss on death.
        /// </value>
        public float MaxExpLossOnDeath { get; set; }

        /// <summary>
        /// Gets the abilities that are associated to this skill.
        /// You can use the ability id to receive a specific ability.
        /// </summary>
        /// <value>
        /// The abilities.
        /// </value>
        public List<Ability> Abilities { get; private set; }

        /// <summary>
        /// Gets the <see cref="Ability"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Ability"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>the <see cref="Ability"/> at the specified index</returns>
        public Ability this[int index]
        {
            get => this.Abilities.Count > index && index >= 0 ? this.Abilities[index] : null;
            private set { if (this.Abilities.Count > index && index >= 0) this.Abilities[index] = value; }
        }

        /// <summary>
        /// Gets or sets the experience equation.
        /// Is only required if you don't want to use the default one.
        /// </summary>
        /// <value>
        /// The experience equation.
        /// </value>
        public ExperienceEquationDelegate ExperienceEquation { get; set; }

        /// <summary>
        /// The specialisation ability identifier.
        /// The ability with this id will be used for experience gain calculations.
        /// </summary>
        private int specialisationID;

        /// <summary>
        /// Gets or sets the specialisation ability identifier.
        /// The ability with this id will be used for experience gain calculations.
        /// The id must exist within the abilities of this skill.
        /// </summary>
        /// <value>
        /// The specialisation ability identifier.
        /// Must be a valid ability ID.
        /// </value>
        public int SpecialisationID
        {
            get { return this.specialisationID; }
            set
            {
                if (value < this.Abilities.Count)
                {
                    this.specialisationID = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public CustomSkillConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the class exp multipliers.
        /// </summary>
        /// <value>
        /// The class exp multipliers.
        /// </value>
        public Dictionary<string, float> ClassExpMultipliers { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Skill" /> class.
        /// </summary>
        /// <param name="name">The internal name of this skill. Used to save, load and identify this skill.
        /// This should always be the same and must be unique.</param>
        /// <param name="displayName">The name that is displayed in the game.
        /// Can be localized.</param>
        /// <param name="group">The name of the skill group.</param>
        /// <param name="expBase">The base for the calculation of the required experience for the next level.
        /// The default calculation is [expBase] * pow([expMult], current level - 1)</param>
        /// <param name="expMult">The multiplier for the calculation of the required experience for the next level.
        /// The default calculation is [expBase] * pow([expMult], current level - 1).</param>
        /// <param name="maxLevel">The maximum level that a player can reach in this skill.</param>
        /// <exception cref="ArgumentNullException">Is thrown if name is <c>null</c>.</exception>
        public Skill(string name, string displayName = null, string group = null, int expBase = 200, float expMult = 1.33f, int maxLevel = 25)
        {
            this.Name = name ?? throw new ArgumentNullException("A skill name must not be null."); ;
            this.DisplayName = displayName ?? this.Name;
            this.Group = group ?? "Survival";
            this.Id = -1;
            this.ExpBase = expBase;
            this.ExpMult = expMult;
            this.MaxLevel = maxLevel;
            this.MinLevel = 1;
            this.Abilities = new List<Ability>();
            this.SpecialisationID = -1;
            this.Enabled = true;
            this.ExpLossOnDeath = 0.0f;
            this.MaxExpLossOnDeath = 0.0f;
            this.ClassExpMultipliers = new Dictionary<string, float>();
        }

        /// <summary>
        /// Adds an ability to the skill. 
        /// Fails if the ability already belongs to a skill or an ability with the same name is exists already within this skill.
        /// Also sets the ability id. That can be used to identify the ability.
        /// </summary>
        /// <param name="ability">The ability.</param>
        /// <returns>
        ///   the id of the added ability if the method succeeds; otherwise, -1
        /// </returns>
        public int AddAbility(Ability ability)
        {
            if (ability.Id != -1) return -1;
            if (this.FindAbility(ability.Name) != null) return -1;

            this.Abilities.Add(ability);
            ability.Id = Abilities.Count - 1;
            ability.Skill = this;
            return ability.Id;
        }

        /// <summary>
        /// Replaces an ability of the skill. 
        /// Fails if the ability already belongs to a skill or an ability with the same name is exists already within this skill.
        /// Also fails if the ValuesPerTier value of the old ability is greater than the ValuesPerTier value of the new ability.
        /// Also sets the ability id. That can be used to identify the ability.
        /// </summary>
        /// <param name="index">The index of the ability that should be replaced.</param>
        /// <param name="ability">The new ability.</param>
        /// <returns>
        ///   the id of the added ability if the method succeeds; otherwise, -1
        /// </returns>
        public int ReplaceAbility(int index, Ability ability)
        {
            if (ability.Id != -1) return -1;
            if (index >= Abilities.Count) return -1;
            Ability other = this.FindAbility(ability.Name);
            //fails if another ability has already the name (the replaced ability can have the same name)
            if (other?.Id != index) return -1;
            if (this.Abilities[index]?.ValuesPerTier > ability.ValuesPerTier) return -1;

            this.Abilities[index] = ability;
            ability.Id = index;
            ability.Skill = this;
            return ability.Id;
        }

        /// <summary>
        /// Gets the <see cref="PlayerAbility" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// the <see cref="Ability" /> at the specified index
        /// </returns>
        public Ability Ability(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Finds an ability by its name.
        /// </summary>
        /// <param name="abilityName">Name of the ability.</param>
        /// <param name="allowDisplayName">if set to <c>true</c> the method also looks for matching display names.</param>
        /// <returns>
        ///   The ability if a ability with the given name is in this skill; otherwise, <c>null</c>
        /// </returns>
        public Ability FindAbility(string abilityName, bool allowDisplayName = false)
        {
            foreach (Ability ability in this.Abilities)
            {
                if (ability.Name == abilityName)
                {
                    return ability;
                }
                else if (allowDisplayName && ability.DisplayName == abilityName)
                {
                    return ability;
                }
            }
            return null;
        }

        /// <summary>
        /// Determines whether the skill contains an ability with the specified ability name.
        /// </summary>
        /// <param name="abilityName">Name of the ability.</param>
        /// <returns>
        ///   <c>true</c> if the skill contains an ability with the specified ability name; otherwise, <c>false</c>.
        /// </returns>
        public bool ContainsAbility(string abilityName)
        {
            return (this.FindAbility(abilityName) != null);
        }

        /// <summary>
        /// Gets the experience that is required to reach a specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="includePreviousLevels">if set to <c>true</c> the method also includes the experience for all previous levels.</param>
        /// <returns>
        ///   the experience that is required to reach a specified level.
        /// </returns>
        virtual public float GetRequiredExperience(int level, bool includePreviousLevels = false)
        {
            if (level <= MinLevel)
            {
                return 0;
            }

            if (includePreviousLevels)
            {
                return GetRequiredExperience(level) + this.GetRequiredExperience(level - 1, includePreviousLevels);
            }
            else
            {
                if (ExperienceEquation != null)
                {
                    return this.ExperienceEquation(this.ExpBase, this.ExpMult, this.ExpEquationValue, level);
                }
                return (int)(Math.Pow(this.ExpMult, level - (MinLevel + 1)) * this.ExpEquationValue + this.ExpBase);
            }
        }

        /// <summary>
        /// Gets the experience multiplier for a specific player.
        /// </summary>
        /// <param name="playerSkillSet">The player skill set of a player.</param>
        /// <param name="includeGlobalModifiers">Decides whether global modifiers should be included.</param>
        /// <returns>the experience multiplier for a specific player; returns 0.0f if playerSkillSet is null</returns>
        virtual public float GetExperienceMultiplier(PlayerSkillSet playerSkillSet, bool includeGlobalModifiers = true)
        {
            if (playerSkillSet == null) return 0.0f;
            float value = 1.0f;
            float mult = 0.0f;
            ClassExpMultipliers.TryGetValue(playerSkillSet.Player.Entity?.WatchedAttributes.GetString("characterClass"), out mult);

            if (includeGlobalModifiers) value *= XLeveling.Config.expMult;
            if (SpecialisationID >= 0)
            {
                mult += playerSkillSet[Id][SpecialisationID].FValue(0);
            }
            mult += playerSkillSet.Player.Entity.Stats.GetBlended("expMult");
            return value * mult;
        }

        /// <summary>
        /// This function is called after the skill configuration for this skill have been received from a server.
        /// </summary>
        virtual public void OnConfigReceived()
        {}

        /// <summary>
        /// Called when the player dies.
        /// Reduces players experience when enabled.
        /// </summary>
        /// <param name="playerSkillSet">The player skill set.</param>
        /// <param name="invokePenalty">if set to <c>true</c> invokes penalties. Otherwise no experience is lossed. 
        /// This parameter usually is <c>false</c> when the player already died recently to prevent too many penalties from chain dying.</param>
        virtual public void OnPlayerDeath(PlayerSkillSet playerSkillSet, bool invokePenalty)
        {
            if (ExpLossOnDeath <= 0.0f || ExpLossOnDeath > 100.0f) return;

            PlayerSkill playerSkill = playerSkillSet?[Id];
            if (playerSkill == null) return;

            float loss = ExpLossOnDeath <= 1.0f ? 
                playerSkill.Experience * ExpLossOnDeath :
                Math.Min(playerSkill.Experience, playerSkill.RequiredExperience * ExpLossOnDeath * 0.01f);

            float maxLoss =
                MaxExpLossOnDeath <= 0.0f ? float.MaxValue :
                MaxExpLossOnDeath > 1.0f ? MaxExpLossOnDeath :
                MaxExpLossOnDeath * playerSkill.RequiredExperience;

            playerSkill.AddExperience(-Math.Min(loss, maxLoss), false);
            (playerSkillSet.Player as IServerPlayer)?.SendLocalisedMessage(GlobalConstants.GeneralChatGroup, "xlib:explossondeath", loss, playerSkill.Skill.DisplayName);
        }

        /// <summary>
        /// Fills this skills values from a skill configuration.
        /// </summary>
        /// <param name="config">The skill configuration.</param>
        /// <exception cref="ArgumentNullException">Is thrown if config is <c>null</c>.</exception>
        virtual public void FromConfig(SkillConfig config)
        {
            if (config == null) throw new ArgumentNullException("The skill config must not be null.");
            if (config.name != this.Name) throw new ArgumentException("Skill configuration mismatch. Server and client could run on different versions of the mod.");

            this.MaxLevel = Math.Max(config.maxLevel, 1);
            this.MinLevel = Math.Max(config.minLevel, 1);
            this.ExpBase = Math.Max(config.expBase, 0.0f);
            this.ExpMult = Math.Max(config.expMult, 0.0f);
            this.ExpEquationValue = Math.Max(config.expValue, 0.0f);
            this.Enabled = config.enabled;
            this.ExpLossOnDeath = config.expLossOnDeath;
            this.MaxExpLossOnDeath = config.maxExpLossOnDeath;

            if (config.expEquation == "logarithmic") this.ExperienceEquation = LogarithmicEquation;
            else if (config.expEquation == "quadratic") this.ExperienceEquation = QuadraticEquation;
            else if (config.expEquation == "exponential") this.ExperienceEquation = null;

            if (this.Config != null && config.Attributes != null)
            {
                this.Config.Attributes = config.Attributes;
            }
            if (config.abilities == null) return;

            foreach (AbilityConfig abilityConfig in config.abilities)
            {
                Ability ability = this.FindAbility(abilityConfig.name);
                if (ability == null) continue;
                ability.FromConfig(abilityConfig);
            }

            if (config.ClassExpMultipliers != null)
            {
                Dictionary<string, float>.Enumerator pairs = config.ClassExpMultipliers.GetEnumerator();
                while(pairs.MoveNext())
                {
                    this.ClassExpMultipliers[pairs.Current.Key] = pairs.Current.Value;
                }
            }
        }

        /// <summary>
        /// A logarithmic experience equation.
        /// ln(level - 1) * expMult + expBase
        /// </summary>
        /// <param name="expBase">The exp base.</param>
        /// <param name="expMult">The exp mult.</param>
        /// <param name="expEquationValue">The exp equation value.</param>
        /// <param name="level">The level.</param>
        /// <returns>
        ///   the experience that is required to reach a specified level.
        /// </returns>
        public int LogarithmicEquation(float expBase, float expMult, float expEquationValue, int level)
        {
            return (int)(Math.Log(level - MinLevel) * expMult + expBase);
        }

        /// <summary>
        /// A quadratic experience equation.
        /// expEquationValue * (level-2)^2 + (level-2) * expMult + expBase
        /// </summary>
        /// <param name="expBase">The exp base.</param>
        /// <param name="expMult">The exp mult.</param>
        /// <param name="expEquationValue">The exp equation value.</param>
        /// <param name="level">The level.</param>
        /// <returns>
        ///   the experience that is required to reach a specified level.
        /// </returns>
        public int QuadraticEquation(float expBase, float expMult, float expEquationValue, int level)
        {
            level-= (MinLevel + 1);
            return (int)(expEquationValue * level * level + level * expMult + expBase);
        }
    }//!class Skill

    /// <summary>
    /// Calculates the required experience for a specific level.
    /// </summary>
    /// <param name="expBase">The exp base.</param>
    /// <param name="expMult">The exp mult.</param>
    /// <param name="expEquationValue">The exp calculate value.</param>
    /// <param name="level">The level the experience should be calculated for.</param>
    /// <returns>
    ///   the experience that is required to reach a specified level.
    /// </returns>
    public delegate int ExperienceEquationDelegate(float expBase, float expMult, float expEquationValue, int level);
}//!namespace XLib.XLeveling
