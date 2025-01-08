﻿using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;

namespace XLib.XLeveling
{
    /// <summary>
    /// The configuration of a skill.
    /// </summary>
    [ProtoContract]
    public class SkillConfig
    {
        /// <summary>
        /// A note for the configuration file.
        /// </summary>
        public string note;

        /// <summary>
        /// The name of the skill.
        /// </summary>
        [ProtoMember(1)]
        public string name;

        /// <summary>
        /// The internal identifier for the skill.
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(-1)]
        public int id;

        /// <summary>
        /// The maximum level that a player can reach in this skill.
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(20)]
        public int maxLevel;

        /// <summary>
        /// The base for the calculation of the required experience for the next level.
        /// </summary>
        [ProtoMember(4)]
        [DefaultValue(200.0)]
        public float expBase;

        /// <summary>
        /// The multiplier for the calculation of the required experience for the next level.
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue(100.0f)]
        public float expMult;

        /// <summary>
        /// An additional value for the calculation of the required experience for the next level.
        /// </summary>
        [ProtoMember(6)]
        [DefaultValue(8.0f)]
        public float expValue;

        /// <summary>
        /// The name of the equation used to calculate the experience.
        /// </summary>
        [ProtoMember(7)]
        [DefaultValue(null)]
        public string expEquation;

        /// <summary>
        /// Decides whether this skill is enabled
        /// </summary>
        [ProtoMember(8)]
        [DefaultValue(true)]
        public bool enabled;

        /// <summary>
        /// The configuration of the associated abilities.
        /// </summary>
        [ProtoMember(9)]
        public AbilityConfig[] abilities;

        /// <summary>
        /// Gets or sets the attributes. This contains the values of the custom config.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        [ProtoMember(10)]
        public Dictionary<string, string> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the class exp multipliers.
        /// </summary>
        /// <value>
        /// The class exp multipliers.
        /// </value>
        [ProtoMember(11)]
        public Dictionary<string, float> ClassExpMultipliers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillConfig"/> class.
        /// </summary>
        public SkillConfig()
        {
            this.note = "";
            this.name = "";
            this.id = -1;
            this.maxLevel = 20;
            this.expBase = 200;
            this.expMult = 100.0f;
            this.expValue = 8.0f;
            this.expEquation = null;
            this.enabled = true;

            this.abilities = null;
            this.Attributes = null;
            this.ClassExpMultipliers = null;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillConfig"/> class.
        /// </summary>
        /// <param name="skill">The skill.</param>
        public SkillConfig(Skill skill)
        {
            this.note = "If you change the maximal tier of an ability you must also adjust the number of values! It is recommended to make backups from edited config files. If an error occurs your changes will be overwritten by default values. Values must be integers.";
            this.name = skill.Name;
            this.id = skill.Id;
            this.maxLevel = skill.MaxLevel;
            this.expBase = skill.ExpBase;
            this.expMult = skill.ExpMult;
            this.expValue = skill.ExpEquationValue;
            this.enabled = skill.Enabled;

            if(skill.ExperienceEquation == Skill.LogarithmicEquation) this.expEquation = "logarithmic";
            else if (skill.ExperienceEquation == Skill.QuadraticEquation) this.expEquation = "quadratic";
            else if (skill.ExperienceEquation == null) this.expEquation = "exponential";
            else this.expEquation = "unknown";

            this.abilities = new AbilityConfig[skill.Abilities.Count];
            for (int index = 0; index < skill.Abilities.Count; index++)
            {
                this.abilities[index] = new AbilityConfig(skill.Abilities[index]);
            }
            if (skill.Config != null)
            {
                this.Attributes = skill.Config.Attributes;
            }
            this.ClassExpMultipliers = skill.ClassExpMultipliers;
        }
    }//!class XLevelingSkillConfig

    /// <summary>
    /// A custom skill configuration.
    /// </summary>
    public class CustomSkillConfig
    {
        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>
        /// The attributes.
        /// </value>
        public virtual Dictionary<string, string> Attributes
        {
            get { return null; }
            set {}
        }
    }
}//!namespace XLib.XLeveling
