using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a skill associated to a player.
    /// Contains player specific data for a skill.
    /// </summary>
    public class PlayerSkill
    {
        /// <summary>
        /// Gets the skill that is associated to the player skill.
        /// </summary>
        /// <value>
        /// The skill.
        /// </value>
        public Skill Skill { get; private set; }

        /// <summary>
        /// Gets the player skill set this skill is associated to.
        /// </summary>
        /// <value>
        /// The player skill set.
        /// </value>
        public PlayerSkillSet PlayerSkillSet { get; private set; }

        /// <summary>
        /// The current level of this player skill.
        /// </summary>
        private int level;

        /// <summary>
        /// Gets or sets a value indicating if the skill is hidden for this player.
        /// </summary>
        public bool Hidden { get; set; }

        /// <summary>
        /// Gets the current level of this player skill.
        /// </summary>
        /// <value>
        /// The level.
        /// </value>
        public int Level
        {
            get { return this.level; }
            internal set
            {
                if (!this.Skill.Enabled) value = 1;
                value = GameMath.Clamp(value, this.Skill.MinLevel, this.Skill.MaxLevel);
                this.AbilityPoints += value - this.level;
                this.level = value;
                this.RequiredExperience = this.Skill.GetRequiredExperience(this.Level + 1);
            }
        }

        /// <summary>
        /// The current experience of this player skill.
        /// </summary>
        private float experience;

        /// <summary>
        /// Gets the current experience of this player skill.
        /// Use the AddExperience method to add experience to a skill.
        /// </summary>
        /// <value>
        /// The experience.
        /// </value>
        public float Experience
        {
            get { return this.experience; }
            set
            {
                if (this.Level >= this.Skill.MaxLevel || !this.Skill.Enabled)
                {
                    this.experience = 0.0f;
                    return;
                }
                this.experience = Math.Max(value, 0.0f);
                while (this.RequiredExperience <= this.experience && this.Level < this.Skill.MaxLevel)
                {
                    this.experience -= this.RequiredExperience;
                    this.Level++;
                    ICoreClientAPI client = this.Skill.XLeveling.Api as ICoreClientAPI;
                    client?.ShowChatMessage(Lang.Get("xlib:levelup", this.Level, this.Skill.DisplayName));
                }
            }
        }

        /// <summary>
        /// Gets the for the next level required experience.
        /// </summary>
        /// <value>
        /// The required experience.
        /// </value>
        public float RequiredExperience { get; private set; }

        /// <summary>
        /// Gets the player abilities associated with this skill.
        /// </summary>
        /// <value>
        /// The player abilities.
        /// </value>
        public List<PlayerAbility> PlayerAbilities { get; private set; }

        /// <summary>
        /// Gets the <see cref="PlayerAbility" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="PlayerAbility" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>the <see cref="PlayerAbility" /> at the specified index</returns>
        public PlayerAbility this[int index]
        {
            get => this.PlayerAbilities.Count > index && index >= 0 ? this.PlayerAbilities[index] : null;
            private set { if (this.PlayerAbilities.Count > index && index >= 0) this.PlayerAbilities[index] = value; }
        }

        /// <summary>
        /// Gets the number of additional abilities the player can learn.
        /// </summary>
        /// <value>
        /// The ability points.
        /// </value>
        public int AbilityPoints { get; internal set; }

        /// <summary>
        /// Gets or sets the experience multiplier.
        /// </summary>
        /// <value>
        /// The experience multiplier.
        /// </value>
        public float ExperienceMultiplier { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSkill" /> class.
        /// </summary>
        /// <param name="skill">The skill that is associated to the player skill.</param>
        /// <param name="playerSkillSet">The player skill set this skill is associated to.</param>
        /// <param name="level">The level.</param>
        /// <exception cref="ArgumentNullException">Is thrown if skill or playerSkillSet is <c>null</c>.</exception>
        public PlayerSkill(Skill skill, PlayerSkillSet playerSkillSet, int level = 1)
        {
            this.Skill = skill ?? throw new ArgumentNullException("The skill of a player skill must not be null.");
            this.PlayerSkillSet = playerSkillSet ?? throw new ArgumentNullException("The player skill set of a player skill must not be null.");
            this.Level = level;
            this.Experience = 0.0f;
            this.AbilityPoints = 0;
            this.PlayerAbilities = new List<PlayerAbility>();
            this.ExperienceMultiplier = 1.0f;
            this.Hidden = false;

            foreach (Ability ability in this.Skill.Abilities)
            {
                this.PlayerAbilities.Add(new PlayerAbility(ability, this));
            }
        }

        /// <summary>
        /// Gets the <see cref="PlayerAbility" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// the <see cref="PlayerAbility" /> at the specified index
        /// </returns>
        public PlayerAbility Ability(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Finds a player ability by its name.
        /// </summary>
        /// <param name="abilityName">Name of the ability.</param>
        /// <param name="allowDisplayName">if set to <c>true</c> the method also looks for matching display names.</param>
        /// <returns>
        /// The palyer ability if a ability with the given name is in this skill; otherwise, <c>null</c>
        /// </returns>
        public PlayerAbility FindAbility(string abilityName, bool allowDisplayName = false)
        {
            foreach (PlayerAbility playerAbility in this.PlayerAbilities)
            {
                if (playerAbility.Ability.Name == abilityName)
                {
                    return playerAbility;
                }
                else if(allowDisplayName && playerAbility.Ability.DisplayName == abilityName)
                {
                    return playerAbility;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds experience to this skill.
        /// Use this method to add experience to a skill.
        /// </summary>
        /// <param name="experience">The experience.</param>
        /// <param name="invokeModifiers">if set to <c>true</c> experience modifiers will be invoked.</param>
        public void AddExperience(float experience, bool invokeModifiers = true)
        {
            float mult = invokeModifiers ? 
                Skill.GetExperienceMultiplier(PlayerSkillSet) * ExperienceMultiplier : 
                1.0f;
            this.Skill.XLeveling.IXLevelingAPI.AddExperienceToPlayerSkill(this.PlayerSkillSet.Player, this.Skill.Id, experience * mult);
        }

        /// <summary>
        /// Fills this player skills from a saved player skill.
        /// </summary>
        /// <param name="saved">The saved.</param>
        virtual public void FromSavedSkill(SavedPlayerSkill saved)
        {
            this.Level = saved.Level;
            this.Experience = saved.Experience;

            //abilities
            foreach (PlayerAbility playerAbility in this.PlayerAbilities)
            {
                SavedPlayerAbility savedPlayerAbility;
                saved.Abilities.TryGetValue(playerAbility.Ability.Name, out savedPlayerAbility);
                if (savedPlayerAbility == null) continue;
                playerAbility.IgnoredRequirements = EnumRequirementType.WeakMediumRequirements;
                playerAbility.Tier = savedPlayerAbility.Tier;
                playerAbility.IgnoredRequirements = EnumRequirementType.None;
            }
        }

        /// <summary>
        /// Checks if the requirements for all abilities are fulfilled and tries to correct errors.
        /// </summary>
        /// <param name="ignored">Sets which requirements should be ignored.</param>
        /// <returns>
        ///   <c>true</c> if a ability tier was changed; otherwise, false
        /// </returns>
        public bool CheckRequirements(EnumRequirementType ignored)
        {
            bool changed = false;
            foreach (PlayerAbility playerAbility in this.PlayerAbilities)
            {
                playerAbility.IgnoredRequirements = ignored;
                if (playerAbility.CheckRequirements())
                {
                    changed = true;
                }
                playerAbility.IgnoredRequirements = EnumRequirementType.None;
            }
            return changed;
        }

        /// <summary>
        /// Sets the tiers of the abilities of this skill to 0.
        /// Also checks the requirements of all abilities.
        /// </summary>
        public void Reset()
        {
            foreach (PlayerAbility playerAbility in this.PlayerAbilities)
            {
                playerAbility.Tier = 0;
            }

            //check Requirments
            EnumRequirementType ignored = EnumRequirementType.WeakRequirement;
            this.PlayerSkillSet.CheckRequirements(ignored);
        }
    }//!class PlayerSkill

    /// <summary>
    /// Used to save player skills.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SavedPlayerSkill
    {
        /// <summary>
        /// The level of the skill.
        /// </summary>
        [JsonProperty]
        public int Level;

        /// <summary>
        /// The current experience of this player skill.
        /// </summary>
        [JsonProperty]
        public float Experience;

        /// <summary>
        /// he player abilities that are associated to the player skill.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, SavedPlayerAbility> Abilities = new Dictionary<string, SavedPlayerAbility>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerSkill"/> class.
        /// </summary>
        public SavedPlayerSkill() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerSkill"/> class.
        /// </summary>
        /// <param name="playerSkill">The player skill.</param>
        public SavedPlayerSkill(PlayerSkill playerSkill)
        {
            this.Level = playerSkill.Level;
            this.Experience = playerSkill.Experience;

            foreach(PlayerAbility playerAbility in playerSkill.PlayerAbilities)
            {
                if (playerAbility.Tier > 0)
                {
                    this.Abilities.Add(playerAbility.Ability.Name, new SavedPlayerAbility(playerAbility));
                }
            }
        }
    }//!class SavedPlayerSkill
}//!namespace XLib.XLeveling
