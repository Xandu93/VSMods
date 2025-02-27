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
        /// The experience loss on death
        /// Numbers smaller than or equal to 1.0 means a percentage of the earned experience.
        /// Numbers greater than 1.0 means a percentage of total needed experience for the next level up.
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(0.0f)]
        public float expLossOnDeath;

        /// <summary>
        /// The exp loss cooldown on death 
        /// </summary>
        [ProtoMember(4)]
        [DefaultValue(0.0f)]
        public float expLossCooldown;

        /// <summary>
        /// The maximal number of specialisations one player can choose
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue(1)]
        public int specialisationLimit;

        /// <summary>
        /// The experience multiplier
        /// </summary>
        [ProtoMember(6)]
        [DefaultValue(1.0f)]
        public float expMult;

        /// <summary>
        /// A value indicating if items with different qualities should be merged
        /// Quality is a feature of xskills. But I put it here so I don't have a
        /// seperate config file for it.
        /// </summary>
        [ProtoMember(7)]
        [DefaultValue(true)]
        public bool mergeQualities;

        /// <summary>
        /// Can be used to disable specific requirements.
        /// Primarily used to disable class requirements.
        /// </summary>
        [ProtoMember(8)]
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
            this.expLossOnDeath = 0.0f;
            this.expLossCooldown = 0.0f;
            this.specialisationLimit = 1;
            this.expMult = 1.0f;
            this.mergeQualities = true;
            this.disabledRequirements = new List<string>();
            this.trackExpGain = false;
        }
    }//!class Config
}
