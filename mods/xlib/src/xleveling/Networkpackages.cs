using System.ComponentModel;
using ProtoBuf;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a player skill package that is send by the server to the player.
    /// This package will be send to every player for every skill when the player joins the server.
    /// </summary>
    [ProtoContract]
    public class PlayerSkillPackage
    {
        /// <summary>
        /// The skill identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(-1)]
        public int skillId;

        /// <summary>
        /// The current level of the skill.
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(1)]
        public int level;

        /// <summary>
        /// The current experience of the skill.
        /// </summary>
        [ProtoMember(3)]
        public float experience;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSkillPackage"/> class.
        /// </summary>
        public PlayerSkillPackage()
        {
            this.skillId = -1;
            this.experience = 0.0f;
            this.level = 1;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerSkillPackage" /> class from a skill.
        /// </summary>
        /// <param name="skill">The skill.</param>
        public PlayerSkillPackage(PlayerSkill skill)
        {
            this.skillId = skill.Skill.Id;
            this.level = skill.Level;
            this.experience = skill.Experience;
        }
    }//!class PlayerSkillPackage

    /// <summary>
    /// Represents a player skill experience package that is send by the server to the player when a player gets experience.
    /// </summary>
    [ProtoContract]
    public class ExperiencePackage
    {
        /// <summary>
        /// The skill identifier
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(-1)]
        public int skillId;

        /// <summary>
        /// The experience
        /// </summary>
        [ProtoMember(2)]
        public float experience;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperiencePackage"/> class.
        /// </summary>
        public ExperiencePackage()
        {
            this.skillId = -1;
            this.experience = 0.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperiencePackage"/> class.
        /// </summary>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="experience">The experience.</param>
        public ExperiencePackage(int skillId = -1, float experience = 0.0f)
        {
            this.skillId = skillId;
            this.experience = experience;
        }
    }//!class ExperiencePackage

    /// <summary>
    /// Represents a player ability package.
    /// A client sends this package when a player increases the tier of an ability
    /// A server sends this package to every player for every ability when the player joins the server.
    /// </summary>
    [ProtoContract]
    public class PlayerAbilityPackage
    {
        /// <summary>
        /// The ability identifier of the ability
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(-1)]
        public int abilityId;

        /// <summary>
        /// The skill identifier
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(-1)]
        public int skillId;

        /// <summary>
        /// The skilled tier
        /// </summary>
        [ProtoMember(3)]
        public int skilledTier;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAbilityPackage"/> class.
        /// </summary>
        public PlayerAbilityPackage()
        {
            this.abilityId = -1;
            this.skillId = -1;
            this.skilledTier = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAbilityPackage" /> class from an ability.
        /// </summary>
        /// <param name="ability">The ability.</param>
        /// <param name="skilledTier">The skilled tier.</param>
        public PlayerAbilityPackage(PlayerAbility ability, int skilledTier)
        {
            this.abilityId = ability.Ability.Id;
            this.skillId = ability.Ability.Skill.Id;
            this.skilledTier = skilledTier;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlayerAbilityPackage"/> class.
        /// </summary>
        /// <param name="ability">The ability.</param>
        public PlayerAbilityPackage(PlayerAbility ability) : this(ability, ability.Tier)
        {}
    }//!class PlayerAbilityPackage

    /// <summary>
    /// Represents a knowledge package.
    /// A server sends this package to players who receive knowledge.
    /// </summary>
    [ProtoContract]
    public class KnowledgePackage
    {
        /// <summary>
        /// The name of the knowledge
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(null)]
        public string name;

        /// <summary>
        /// The level of the knowledge
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(0)]
        public int level;

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgePackage" /> class.
        /// </summary>
        public KnowledgePackage()
        {
            this.name = null;
            this.level = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgePackage"/> class.
        /// </summary>
        /// <param name="name">The knowledge name.</param>
        /// <param name="level">The knowledge level.</param>
        public KnowledgePackage(string name, int level)
        {
            this.name = name;
            this.level = level;
        }
    }//!class KnowledgePackage

    /// <summary>
    /// A command for the CommandPackage
    /// </summary>
    public enum EnumXLevelingCommand : int
    {
        /// <summary>
        /// No command
        /// </summary>
        None = 0,

        /// <summary>
        /// Reset command
        /// </summary>
        Reset = 1,

        /// <summary>
        /// Unlearn points
        /// </summary>
        UnlearnPoints = 2,

        /// <summary>
        /// Unlearn ready time
        /// </summary>
        UnlearnReadyTime = 3,

        /// <summary>
        /// Sparring mode
        /// </summary>
        SparringMode = 4
    }//!enum EnumXLevelingCommand

    /// <summary>
    /// Represents a command.
    /// </summary>
    [ProtoContract]
    public class CommandPackage
    {
        /// <summary>
        /// The command
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(EnumXLevelingCommand.None)]
        public EnumXLevelingCommand command;

        /// <summary>
        /// A integer value for the command
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(0)]
        public int value;

        /// <summary>
        /// A double value for the command
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(0.0)]
        public double dValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPackage"/> class.
        /// </summary>
        public CommandPackage()
        {
            this.command = EnumXLevelingCommand.None;
            this.value = 0;
            this.dValue = 0.0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPackage"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="value">The value.</param>
        public CommandPackage(EnumXLevelingCommand command, int value)
        {
            this.command = command;
            this.value = value;
            this.dValue = 0.0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandPackage"/> class.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="value">The value.</param>
        public CommandPackage(EnumXLevelingCommand command, double value)
        {
            this.command = command;
            this.value = 0;
            this.dValue = value;
        }
    }//!class CommandPackage
}//!namespace XLib.XLeveling
