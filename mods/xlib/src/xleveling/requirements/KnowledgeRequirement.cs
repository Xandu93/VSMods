using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a specific knowledge level that must be acquired to learn an ability.
    /// </summary>
    /// <seealso cref="XLib.XLeveling.Requirement" />
    public class KnowledgeRequirement : Requirement
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "knowledge";

        /// <summary>
        /// Gets or sets the name of the knowledge that is required.
        /// </summary>
        /// <value>
        /// The name of the knowledge.
        /// </value>
        public string KnowledgeName { get; protected set; }

        /// <summary>
        /// Gets or sets the knowledge level that is required.
        /// </summary>
        /// <value>
        /// The knowledge level.
        /// </value>
        public int KnowledgeLevel { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeRequirement"/> class.
        /// </summary>
        public KnowledgeRequirement() : base()
        {
            this.KnowledgeName = "";
            this.KnowledgeLevel = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KnowledgeRequirement"/> class.
        /// </summary>
        /// <param name="knowledgeName">Name of the knowledge.</param>
        /// <param name="knowledgeLevel">The required knowledge level.</param>
        public KnowledgeRequirement(string knowledgeName, int knowledgeLevel) : base()
        {
            this.KnowledgeName = knowledgeName ?? "";
            this.KnowledgeLevel = knowledgeLevel;
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
            this.KnowledgeName = tree.GetString("name", "");
            this.KnowledgeLevel = tree.GetInt("level", 1);
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
        public override bool IsFulfilled(PlayerAbility playerAbility, int tier = 0)
        {
            if (tier < this.MinimumTier) return true;
            playerAbility.PlayerSkill.PlayerSkillSet.Knowledge.TryGetValue(this.KnowledgeName, out int requiredLevel);
            if (requiredLevel >= this.KnowledgeLevel) return true;
            return false;
        }

        /// <summary>
        /// This function is called when the requirement is not fulfilled after all skills are loaded and should resolve this conflict.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        /// <returns>
        /// Should return false if this conflict should be ignored and true if the conflict has been resolved.
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
            playerAbility.PlayerSkill.PlayerSkillSet.Knowledge.TryGetValue(this.KnowledgeName, out int level);
            return Lang.Get(KnowledgeName) + ": " + level + "/" + KnowledgeLevel;
        }

        /// <summary>
        /// The type of the requirement.
        /// </summary>
        /// <returns>
        /// the type of the requirement.
        /// </returns>
        public override EnumRequirementType RequirementType()
        {
            return EnumRequirementType.MediumRequirement;
        }
    }//!class KnowledgeRequirement
}//!namespace XLib.XLeveling
