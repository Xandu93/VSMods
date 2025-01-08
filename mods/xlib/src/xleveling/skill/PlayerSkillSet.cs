using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a set of player skills.
    /// Will be created for every player by the XLeveling interface.
    /// </summary>
    public class PlayerSkillSet : EntityBehavior
    {
        /// <summary>
        /// Gets the player interface.
        /// </summary>
        /// <value>
        /// The player.
        /// </value>
        public IPlayer Player { get; internal set; }

        /// <summary>
        /// Gets the player skills.
        /// </summary>
        /// <value>
        /// The player skills.
        /// </value>
        public List<PlayerSkill> PlayerSkills { get; private set; }

        /// <summary>
        /// Gets the XLeveling interface.
        /// </summary>
        /// <value>
        /// The XLeveling interface.
        /// </value>
        public XLeveling XLeveling { get; private set; }

        /// <summary>
        /// Gets the knowledge.
        /// This is a dictionary with a string key[knowledge name].
        /// And a specific value for the knowledge in this field.
        /// </summary>
        /// <value>
        /// The knowledge.
        /// </value>
        public Dictionary<string, int> Knowledge { get; private set; }

        /// <summary>
        /// Gets the <see cref="PlayerSkill"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="PlayerSkill"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>the <see cref="PlayerSkill"/> at the specified index</returns>
        public PlayerSkill this[int index]
        {
            get => this.PlayerSkills.Count > index && index >= 0 ? this.PlayerSkills[index] : null;
            private set { if (this.PlayerSkills.Count > index && index >= 0) this.PlayerSkills[index] = value; }
        }

        /// <summary>
        /// The unused player skills.
        /// Contains player skills that are saved but not used currently.
        /// </summary>
        internal Dictionary<string, SavedPlayerSkill> UnusedPlayerSkills { get; private set; }

        /// <summary>
        /// Gets or sets the unlearn points.
        /// </summary>
        /// <value>
        /// The unlearn points.
        /// </value>
        public float UnlearnPoints { get; set; }

        /// <summary>
        /// Gets or sets the timestamp at which the next ability can be forgotten.
        /// </summary>
        /// <value>
        /// The timestamp at which the next ability can be forgotten.
        /// </value>
        public float UnlearnCooldown { get; set; }

        /// <summary>
        /// Gets or sets whether sparring is enabled. 
        /// If a player with sparring enabled was killed by a player 
        /// who has also sparring enabled he will not loose any experience.
        /// </summary>
        /// <value>
        /// The value that determines whether sparring is enabled.
        /// </value>
        public bool Sparring { get; set; }

        /// <summary>
        /// Gets or sets the last death in total world hours.
        /// </summary>
        /// <value>
        /// The last death.
        /// </value>
        public double LastDeath
        {
            get { return entity.WatchedAttributes.GetDouble("lastdeath", 0.0); }
            set { entity.WatchedAttributes.SetDouble("lastdeath", value); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSkillSet" /> class.
        /// </summary>
        /// <param name="player">The player interface.</param>
        /// <param name="skillSetTemplate">The skill set template.</param>
        /// <param name="xLeveling">The XLeveling interface.</param>
        /// <exception cref="ArgumentNullException">Is thrown if player or skillSetTemplate is null.</exception>
        public PlayerSkillSet(IPlayer player, SkillSetTemplate skillSetTemplate, XLeveling xLeveling) : base(player.Entity)
        {
            if(skillSetTemplate == null) throw new ArgumentNullException("The skillSetTemplate of a player skill set must not be null.");
            this.Player = player ?? throw new ArgumentNullException("The player of a player skill set must not be null.");
            this.Player.Entity.Stats.Set("expMult", "base", 1.0f, false);
            this.entity.AddBehavior(this);
            this.UnlearnPoints = 0.0f;
            this.UnlearnCooldown = 0.0f;
            this.UnusedPlayerSkills = new Dictionary<string, SavedPlayerSkill>();
            this.XLeveling = xLeveling;

            this.PlayerSkills = new List<PlayerSkill>();
            this.PlayerSkills.Capacity = skillSetTemplate.Skills.Count;
            foreach (Skill skill in skillSetTemplate.Skills)
            {
                this.PlayerSkills.Add(new PlayerSkill(skill, this));
            }
            this.Knowledge = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets the <see cref="PlayerSkill" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// the <see cref="PlayerSkill" /> at the specified index
        /// </returns>
        public PlayerSkill Skill(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the <see cref="PlayerAbility" /> at the specified indices.
        /// </summary>
        /// <param name="skillIndex">Index of the skill.</param>
        /// <param name="abilityIndex">Index of the ability.</param>
        /// <returns>
        /// the <see cref="PlayerAbility" /> at the specified index
        /// </returns>
        public PlayerAbility Ability(int skillIndex, int abilityIndex)
        {
            return this[skillIndex]?[abilityIndex];
        }

        /// <summary>
        /// Finds a player skill by its name.
        /// </summary>
        /// <param name="skillName">Name of the skill.</param>
        /// <param name="allowDisplayName">if set to <c>true</c> the method also looks for matching display names.</param>
        /// <returns>
        /// The palyer skill if a skill with the given name exists; otherwise, <c>null</c>
        /// </returns>
        public PlayerSkill FindSkill(string skillName, bool allowDisplayName = false)
        {
            foreach (PlayerSkill playerSkill in this.PlayerSkills)
            {
                if (playerSkill.Skill.Name == skillName)
                {
                    return playerSkill;
                }
                else if (allowDisplayName && playerSkill.Skill.DisplayName == skillName)
                {
                    return playerSkill;
                }
            }
            return null;
        }

        /// <summary>
        /// Fills this player skills set from a saved player skill set.
        /// </summary>
        /// <param name="saved">The saved.</param>
        virtual public void FromSavedSkillSet(SavedPlayerSkillSet saved)
        {
            if (saved == null) return;
            this.UnlearnPoints = saved.UnlearnPoints;
            this.UnlearnCooldown = saved.UnlearnCooldown;
            this.Sparring = saved.Sparring;

            foreach (PlayerSkill playerSkill in this.PlayerSkills)
            {
                SavedPlayerSkill savedPlayerSkill;
                saved.Skills.TryGetValue(playerSkill.Skill.Name, out savedPlayerSkill);
                if (savedPlayerSkill == null) continue;
                saved.Skills.Remove(playerSkill.Skill.Name);
                playerSkill.FromSavedSkill(savedPlayerSkill);
            }
            foreach (string key in saved.Skills.Keys)
            {
                UnusedPlayerSkills.Add(key, saved.Skills[key]);
            }
            this.Knowledge = saved.Knowledge ?? new Dictionary<string, int>();
        }

        /// <summary>
        /// Checks if the requirements for all abilities are fulfilled and tries to correct errors.
        /// </summary>
        /// <param name="ignored">Sets which requirements should be ignored.</param>
        public void CheckRequirements(EnumRequirementType ignored)
        {
            bool changed = false;
            int loops = 0;
            int maxLoops = 10;
            do
            {
                changed = false;
                foreach (PlayerSkill playerSkill in this.PlayerSkills)
                {
                    if (playerSkill.CheckRequirements(ignored))
                    {
                        changed = true;
                    }
                }
                loops++;
            } while (changed && loops < maxLoops);
        }

        /// <summary>
        /// Determines whether the player can unlearn an ability.
        /// </summary>
        /// <param name="tiers">The number of tiers that shoud be removed.</param>
        /// <returns>
        ///   <c>true</c> if the player can unlearn an ability; otherwise, <c>false</c>.
        /// </returns>
        public bool CanUnlearn(int tiers)
        {
            if (this.PlayerSkills.Count == 0) return false;
            if (this.UnlearnPoints < this.PlayerSkills[0].Skill.XLeveling.IXLevelingAPI.GetPointsForUnlearn() * (tiers > 1 ? tiers * 1.5f : 1)) return false;
            if (this.UnlearnCooldown > 0.0f) return false;

            return true;
        }

        /// <summary>
        /// The name of the property tied to this entity behavior.
        /// </summary>
        /// <returns></returns>
        public override string PropertyName()
        {
            return "SkillSet";
        }

        /// <summary>
        /// The event fired when a game ticks over.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void OnGameTick(float deltaTime)
        {
            base.OnGameTick(deltaTime);
            this.UnlearnCooldown = Math.Max(this.UnlearnCooldown - deltaTime, 0.0f);
        }

    }//!class PlayerSkillSet

    /// <summary>
    /// Used to save player skill sets.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SavedPlayerSkillSet
    {
        /// <summary>
        /// The unlearn points
        /// </summary>
        [JsonProperty]
        public float UnlearnPoints = 0.0f;

        /// <summary>
        /// The timestamp at which the next ability can be forgotten.
        /// </summary>
        [JsonProperty]
        public float UnlearnCooldown = 0.0f;

        /// <summary>
        /// The value that determines whether sparring is enabled.
        /// </summary>
        [JsonProperty]
        public bool Sparring = false;

        /// <summary>
        /// The player skills that belongs to the player.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, SavedPlayerSkill> Skills;

        /// <summary>
        /// The knowledge of the player.
        /// </summary>
        [JsonProperty]
        public Dictionary<string, int> Knowledge;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerSkillSet"/> class.
        /// </summary>
        public SavedPlayerSkillSet() {}

        /// <summary>
        /// Initializes a new instance of the <see cref="SavedPlayerSkillSet"/> class.
        /// </summary>
        /// <param name="playerSkillSet">The player skill set.</param>
        public SavedPlayerSkillSet(PlayerSkillSet playerSkillSet)
        {
            this.UnlearnPoints = playerSkillSet.UnlearnPoints;
            this.UnlearnCooldown = playerSkillSet.UnlearnCooldown;
            this.Sparring = playerSkillSet.Sparring;
            this.Skills = new Dictionary<string, SavedPlayerSkill>();
            this.Knowledge = new Dictionary<string, int>();

            foreach (PlayerSkill playerSkill in playerSkillSet.PlayerSkills)
            {
                if(playerSkill.Experience > 0.0f || playerSkill.Level > 1)
                {
                    this.Skills.Add(playerSkill.Skill.Name, new SavedPlayerSkill(playerSkill));
                }
            }
            foreach(string key in playerSkillSet.UnusedPlayerSkills.Keys)
            {
                this.Skills.Add(key, playerSkillSet.UnusedPlayerSkills[key]);
            }
            foreach (string key in playerSkillSet.Knowledge.Keys)
            {
                this.Knowledge.Add(key, playerSkillSet.Knowledge[key]);
            }
        }
    }//!class SavedPlayerSkillSet
}//!namespace XLib.XLeveling
