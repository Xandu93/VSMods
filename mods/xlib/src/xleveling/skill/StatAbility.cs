using System;
using Vintagestory.API.Common;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents an ability that is associated to a player stat.
    /// </summary>
    /// <seealso cref="Ability" />
    public class StatAbility : Ability
    {
        /// <summary>
        /// Gets or sets the stat that this ability modifies.
        /// </summary>
        /// <value>
        /// The stat.
        /// </value>
        public string Stat { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatAbility" /> class.
        /// All integers of the values array are set to zero.
        /// </summary>
        /// <param name="name">The internal name of this ability. Used to save, load and identify this ability.
        /// This should always be the same and must be unique in its skill.</param>
        /// <param name="stat">The stat that this ability modifies.</param>
        /// <param name="displayName">The name that is displayed in the game.
        /// Can be localized.</param>
        /// <param name="description">The description of the ability. This can contain placeholders for string interpolation.
        /// The string interpolation will use the values from the values array.</param>
        /// <param name="minLevel">The minimum skill level that is required to learn this ability.</param>
        /// <param name="maxTier">The maximum tier of this ability.</param>
        /// <param name="valuesPerTier">The number of values per tier that exists.</param>
        /// <param name="hideUntilLearnable">if set to <c>true</c> this ability is hidden until all requirements are fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if name is <c>null</c>.</exception>
        public StatAbility(string name, string stat, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false) :
            base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        {
            Stat = stat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Ability" /> class.
        /// </summary>
        /// <param name="name">The internal name of this ability. Used to save, load and identify this ability.
        /// This should always be the same and must be unique in its skill.</param>
        /// <param name="stat">The stat that this ability modifies.</param>
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
        /// <exception cref="ArgumentNullException">Is thrown if name is <c>null</c>.</exception>
        /// Values are copied.
        public StatAbility(string name, string stat, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false) : 
            base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        {
            Stat = stat;
        }

        /// <summary>
        /// Called when a associated player ability changed its tier.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <param name="oldTier">The old tier.</param>
        public override void OnTierChanged(PlayerAbility playerAbility, int oldTier)
        {
            base.OnTierChanged(playerAbility, oldTier);
            EntityPlayer player = playerAbility?.PlayerSkill?.PlayerSkillSet?.Player?.Entity;
            if (player == null) return;
            float value = ValuesPerTier < 3 ? playerAbility.FValue(0) : playerAbility.SkillDependentFValue(); 
            player.Stats.Set(Stat, "ability-" + Name, value, false);
        }

    }//!class StatAbility
}//!namespace XLib.XLeveling
