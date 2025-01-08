using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Decides how strong a requirement is. Used especially when some requirements are ignored.
    /// </summary>
    public enum EnumRequirementType
    {
        /// <summary>
        /// None specified requirement
        /// </summary>
        None = 0x00,

        /// <summary>
        /// This is for the default skill level requirement every ability has by default.
        /// </summary>
        LevelRequirement = 0x01,

        /// <summary>
        /// Weak requirements. Weak requirements can be violated when loading the game.
        /// </summary>
        WeakRequirement = 0x02,

        /// <summary>
        /// Medium requirements are checked while abilities are loaded.
        /// </summary>
        MediumRequirement = 0x04,

        /// <summary>
        /// Strong requirements can never be violated and are never ignored by default.
        /// </summary>
        StrongRequirement = 0x08,

        /// <summary>
        /// WeakRequirement | MediumRequirement
        /// </summary>
        WeakMediumRequirements = WeakRequirement | MediumRequirement,

        /// <summary>
        /// StrongRequirement | MediumRequirement
        /// </summary>
        StrongMediumRequirements = StrongRequirement | MediumRequirement,

        /// <summary>
        /// 0xff
        /// </summary>
        AllRequirements = 0xff,
    }//!enum EnumRequirementType

    /// <summary>
    /// Represents a requirement for an ability that must be fulfilled to increases its tier.
    /// </summary>
    public abstract class Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the ability is hidden until this requirement is fulfilled.
        /// </summary>
        public bool HideAbilityUntilFulfilled { get; set; }

        /// <summary>
        /// Gets the minimum tier this requirement is required for.
        /// </summary>
        /// <value>
        /// The minimum tier.
        /// </value>
        public int MinimumTier { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Requirement"/> class.
        /// </summary>
        public Requirement()
        {
            this.HideAbilityUntilFulfilled = false;
            this.MinimumTier = 0;
        }

        /// <summary>
        /// Creates a requirement from a tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="toResolve">XLeveling object for resolving.</param>
        /// <returns>
        ///   <c>true</c> if the resolving was successful, the requirement is only added to an ability if this method was successful; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool FromTree(TreeAttribute tree, XLeveling toResolve)
        {
            this.HideAbilityUntilFulfilled = tree.GetBool("hideUntilFulfilled", false);
            this.MinimumTier = (int)tree.GetLong("minimumTier", 0);
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
        public abstract bool IsFulfilled(PlayerAbility playerAbility, int tier = 0);

        /// <summary>
        /// This function is called when the requirement is not fulfilled after all skills are loaded and should resolve this conflict.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   Should return false if this conflict should be ignored and true if the conflict has been resolved.
        /// </returns>
        public abstract bool ResolveConflict(PlayerAbility playerAbility);

        /// <summary>
        /// Describes the requirement for the given player ability.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   a Description that describes the requirement for the given player ability.
        /// </returns>
        public virtual string Description(PlayerAbility playerAbility)
        {
            if (this.MinimumTier > 1) return Lang.GetUnformatted("xlib:tier") + " " + MinimumTier + ": " + ShortDescription(playerAbility);
            else return ShortDescription(playerAbility);
        }

        /// <summary>
        /// Describes the requirement for the given player ability.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        ///   a Description that describes the requirement for the given player ability.
        /// </returns>
        public abstract string ShortDescription(PlayerAbility playerAbility);

        /// <summary>
        /// The type of the requirement.
        /// </summary>
        /// <returns>
        ///   the type of the requirement.
        /// </returns>
        public abstract EnumRequirementType RequirementType();
    }//!class Requirement
}//!namespace XLib.XLeveling
