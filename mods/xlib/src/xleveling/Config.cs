using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;

namespace XLib.XLeveling
{
    /// <summary>
    /// The xleveling config.
    /// </summary>
    [ProtoContract]
    public class Config
    {
        /// <summary>
        /// The unlearn cooldown
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(120.0f)]
        public float unlearnCooldown;

        /// <summary>
        /// The number of unlearn points you need to unleran an ability
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(1)]
        public int pointsForUnlearn;

        /// <summary>
        /// The cooldown for the penalties you receive for dying.
        /// Usually that means you will lose experience for some skills.
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(0.0f)]
        public float deathPenaltyCooldown;

        /// <summary>
        /// The maximal number of specialisations one player can choose
        /// </summary>
        [ProtoMember(4)]
        [DefaultValue(1)]
        public int specialisationLimit;

        /// <summary>
        /// The experience multiplier
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue(1.0f)]
        public float expMult;

        /// <summary>
        /// A value indicating if items with different qualities should be merged
        /// Quality is a feature of xskills. But I put it here so I don't have a
        /// seperate config file for it.
        /// </summary>
        [ProtoMember(6)]
        [DefaultValue(true)]
        public bool mergeQualities;

        /// <summary>
        /// The multiplier for the skill book drop chance.
        /// </summary>
        [ProtoMember(7)]
        [DefaultValue(1.0f)]
        public float skillBookChanceMult;

        /// <summary>
        /// The multiplier for the skill book experience gain.
        /// </summary>
        [ProtoMember(8)]
        [DefaultValue(0.5f)]
        public float skillBookExpMult;

        /// <summary>
        /// Can be used to disable specific requirements.
        /// Primarily used to disable class requirements.
        /// </summary>
        [ProtoMember(9)]
        [DefaultValue(default(List<string>))]
        public List<string> disabledRequirements;

        /// <summary>
        /// A value indicating if a chat message should be send if the player got experience
        /// </summary>
        public bool trackExpGain;

        /// <summary>
        /// Initializes a new instance of the <see cref="Config"/> class.
        /// </summary>
        public Config()
        {
            this.unlearnCooldown = 120.0f;
            this.pointsForUnlearn = 1;
            this.deathPenaltyCooldown = 0.0f;
            this.specialisationLimit = 1;
            this.expMult = 1.0f;
            this.mergeQualities = true;
            this.skillBookChanceMult = 1.0f;
            this.skillBookExpMult = 0.5f;
            this.disabledRequirements = new List<string>();
            this.trackExpGain = false;
        }
    }//!class Config
}
