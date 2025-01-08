using System;
using System.Collections.Generic;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    ///  Represents a specific character class that a player must have to learn an ability.
    /// </summary>
    /// <seealso cref="Requirement" />
    public class ClassRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "class";

        /// <summary>
        /// Gets or sets the classes.
        /// </summary>
        /// <value>
        /// The classes.
        /// </value>
        public List<string> Classes { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassRequirement"/> class.
        /// </summary>
        public ClassRequirement() : base()
        {
            this.Classes = new List<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClassRequirement" /> class.
        /// </summary>
        /// <param name="classes">The classes.</param>
        /// <param name="minimumTier">The minimum tier this requirement is required for.</param>
        /// <param name="hideAbilityUntilFulfilled">if set to <c>true</c> the ability is hidden until this requirement is fulfilled.</param>
        /// <exception cref="ArgumentNullException">Is thrown if classes is <c>null</c>.</exception>
        public ClassRequirement(string[] classes, int minimumTier = 1, bool hideAbilityUntilFulfilled = false) : base()
        {
            if(classes == null) throw new ArgumentNullException("The classes of a class requirement must not be null.");
            this.Classes = new List<string>();

            foreach (string str in classes)
            {
                if(str != null) this.Classes.Add(str);
            }

            this.HideAbilityUntilFulfilled = hideAbilityUntilFulfilled;
            this.MinimumTier = minimumTier;
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
            string[] classes = tree.GetStringArray("classes");
            if (classes != null)
            {
                foreach (string str in classes)
                {
                    if (str != null) this.Classes.Add(str);
                }
            }
            else
            {
                string characterClass = tree.GetString("class");
                if (characterClass == null) return false;
                this.Classes.Add(characterClass);
            }
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
            if (tier < this.MinimumTier) return true;
            string playerClass = playerAbility?.PlayerSkill.PlayerSkillSet.Player.Entity.WatchedAttributes.GetString("characterClass");
            if (playerClass == null) return false;
            foreach(string str in this.Classes)
            {
                if (playerClass == str) return true;
            }
            return false;
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
        /// Describes the requirement for the given player ability.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        /// a Description that describes the requirement for the given player ability.
        /// </returns>
        public override string ShortDescription(PlayerAbility playerAbility)
        {
            PlayerSkillSet playerSkillSet = playerAbility?.PlayerSkill?.PlayerSkillSet;
            if (playerSkillSet == null) return "";
            return "class: " + string.Join(", ", this.Classes);
        }

        /// <summary>
        /// The Type of the requirement.
        /// </summary>
        /// <returns>
        ///   the Type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            return EnumRequirementType.MediumRequirement;
        }
    }//!class ClassRequirement
}//!namespace XLib.XLeveling
