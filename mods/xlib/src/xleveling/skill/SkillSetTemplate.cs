using System.Collections.Generic;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a set of skills.
    /// This is the template for the creation of a player skill sets.
    /// </summary>
    public class SkillSetTemplate
    {
        /// <summary>
        /// Gets the skills.
        /// </summary>
        /// <value>
        /// The skills.
        /// </value>
        public List<Skill> Skills { get; private set; }

        /// <summary>
        /// Gets the count of skills in this template.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        public int Count { get => Skills.Count; }

        /// <summary>
        /// Gets the <see cref="Skill"/> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Skill"/>.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>the <see cref="Skill"/> at the specified index</returns>
        public Skill this[int index]
        {
            get => this.Skills.Count > index && index >= 0 ? this.Skills[index] : null;
            private set { if (this.Skills.Count > index && index >= 0) this.Skills[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillSetTemplate"/> class.
        /// </summary>
        public SkillSetTemplate()
        {
            this.Skills = new List<Skill>();
        }

        /// <summary>
        /// Adds the skill to the skill set template. The method fails if a skill with the name is already in the skill set template or skill is <c>null</c>.
        /// </summary>
        /// <param name="skill">The skill.</param>
        /// <returns>
        ///   the id of the added skill if the method succeeds; otherwise, -1
        /// </returns>
        public int AddSkill(Skill skill)
        {
            int count = 0;
            if (skill == null) { return -1; }
            foreach (Skill skill2 in Skills)
            {
                if (skill2.Name == skill.Name)
                {
                    return -1;
                }
                count++;
            }
            skill.Id = count;
            this.Skills.Add(skill);
            return count;
        }

        /// <summary>
        /// Gets the <see cref="Skill" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>
        /// the <see cref="Skill" /> at the specified index
        /// </returns>
        public Skill Skill(int index)
        {
            return this[index];
        }

        /// <summary>
        /// Gets the <see cref="Ability" /> at the specified indices.
        /// </summary>
        /// <param name="skillIndex">Index of the skill.</param>
        /// <param name="abilityIndex">Index of the ability.</param>
        /// <returns>
        /// the <see cref="Ability" /> at the specified index
        /// </returns>
        public Ability Ability(int skillIndex, int abilityIndex)
        {
            return this[skillIndex]?[abilityIndex];
        }

        /// <summary>
        /// Gets the skill by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowDisplayName">if set to <c>true</c> the method also looks for skills with this display name.</param>
        /// <returns>
        /// the skill with the given name if it exists; otherwise, <c>null</c>
        /// </returns>
        public Skill FindSkill(string name, bool allowDisplayName = false)
        {
            foreach (Skill skill in this.Skills)
            {
                if (skill.Name == name)
                {
                    return skill;
                }
                if (allowDisplayName && skill.DisplayName == name)
                {
                    return skill;
                }
            }
            return null;
        }

    }//!class SkillSetTemplate
}//!namespace XLib.XLeveling
