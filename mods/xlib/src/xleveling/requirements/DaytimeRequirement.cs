using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a time restriction of learning the ability.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class DaytimeRequirement : Requirement
    {
        /// <summary>
        /// Names this instance.
        /// </summary>
        /// <returns></returns>
        public override string Name => "daytime";

        /// <summary>
        /// Gets or sets the earliest time.
        /// </summary>
        /// <value>
        /// The after.
        /// </value>
        public float After { get; set; }

        /// <summary>
        /// Gets or sets latest time.
        /// </summary>
        /// <value>
        /// The before.
        /// </value>
        public float Before { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DaytimeRequirement"/> class.
        /// </summary>
        public DaytimeRequirement() : base()
        {
            this.After = 0.0f;
            this.Before = 1.0f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DaytimeRequirement" /> class.
        /// </summary>
        /// <param name="after">The earliest time. Should be a value between 0.0 and 1.0.</param>
        /// <param name="before">The latest time. Should be a value between 0.0 and 1.0.</param>
        /// <param name="minimumTier">The minimum tier this requirement is required for.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        public DaytimeRequirement(float after, float before, int minimumTier = 1, bool hideAbilityUntilFulfilled = false) : base()
        {
            after = GameMath.Clamp(after, 0.0f, 1.0f);
            before = GameMath.Clamp(before, 0.0f, 1.0f);

            this.After = after;
            this.Before = before;
            this.MinimumTier = minimumTier;
            this.HideAbilityUntilFulfilled = hideAbilityUntilFulfilled;
        }

        /// <summary>
        /// Creates a requirement from a tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="toResolve">XLeveling object for resolving.</param>
        /// <returns>
        ///   <c>true</c> if the resolving was successful, the requirement is only added to an ability if this method was successful; otherwise, <c>false</c>.
        /// </returns>
        public override bool FromTree(TreeAttribute tree, XLeveling toResolve)
        {
            base.FromTree(tree, toResolve);
            this.After = (float)tree.GetDecimal("after", 0);
            this.Before = (float)tree.GetDecimal("before", 0);
            return true;
        }

        /// <summary>
        /// Determines whether the specified player ability fulfills the requirement.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <param name="tier">The tier this requirement is checked for.</param>
        /// <returns>
        ///   <c>true</c> if the specified player ability fulfills the requirement; otherwise, <c>false</c>.
        /// </returns>
        public override bool IsFulfilled(PlayerAbility playerAbility, int tier)
        {
            if (this.MinimumTier > tier) return true;
            IGameCalendar calendar = playerAbility?.PlayerSkill.PlayerSkillSet.Player.Entity.World.Calendar;
            float currentTime = calendar.HourOfDay / calendar.HoursPerDay;

            if (After < Before)
            {
                return currentTime > After && currentTime < Before;
            }
            else
            {
                return currentTime > After || currentTime < Before;
            }
        }

        /// <summary>
        /// This function is called when the requirement is not fulfilled after all skills are loaded and should resolve this conflict.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   false, if this conflict has been ignored; true, if the conflict has been resolved.
        /// </returns>
        public override bool ResolveConflict(PlayerAbility playerAbility)
        {
            if (playerAbility != null)
            {
                playerAbility.Tier = this.MinimumTier - 1;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Describes the requirement for the given player skill set.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        /// a Description that describes the requirement for the given player skill set.
        /// </returns>
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            return IsFulfilled(playerAbility, playerAbility.Tier + 1) ? Lang.GetUnformatted("xlib:righttime") : Lang.GetUnformatted("xlib:wrongtime");
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        ///   the Type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            return EnumRequirementType.WeakRequirement;
        }
    }//!class DaytimeRequirement
}//!namespace XLib.XLeveling
