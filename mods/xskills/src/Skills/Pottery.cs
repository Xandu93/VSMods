using System.Collections.Generic;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The pottery skill.
    /// </summary>
    /// <seealso cref="XSkills.XSkill" />
    public class Pottery : XSkill
    {
        /// <summary>
        /// The collectibles that are affected by the inspiration skill.
        /// The key is the name of the affected collectible and the value is a list of collectibles it can be mapped to.
        /// Put in null as a value to auto generate the list the first time it is needed.
        /// </summary>
        /// <value>
        /// The collectibles that are affected by the inspiration skill.
        /// </value>
        public Dictionary<string, List<CollectibleObject>> InspirationCollectibles { get; protected set; }

        //ability ids
        public int ThriftId { get; private set; }
        public int LayerLayerId { get; private set; }
        public int PerfectFitId { get; private set; }
        public int PerfectionistId { get; private set; }
        public int FastPotterId { get; private set; }
        public int JackPotId { get; private set; }
        public int InfallibleId { get; private set; }
        public int InspirationId { get; private set; }
        public int PotteryTimerId { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Pottery"/> class.
        /// </summary>
        /// <param name="api">The API.</param>
        public Pottery(ICoreAPI api) : base("pottery", "xskills:skill-pottery", "xskills:group-processing")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);

            // more voxels for each clay
            // 0: value
            ThriftId = this.AddAbility(new Ability(
                "thrift",
                "xskills:ability-thrift",
                "xskills:abilitydesc-thrift",
                1, 3, new int[] { 3, 6, 10 }));

            // copying layers copies additional voxels
            // 0: value
            LayerLayerId = this.AddAbility(new Ability(
                "layerlayer",
                "xskills:ability-layerlayer",
                "xskills:abilitydesc-layerlayer",
                1, 3, new int[] { 1, 2, 4 }));

            //can't remove right voxels
            PerfectFitId = this.AddAbility(new Ability(
                "perfectfit",
                "xskills:ability-perfectfit",
                "xskills:abilitydesc-perfectfit",
                3, 1));

            //can't add a wrong voxel
            PerfectionistId = this.AddAbility(new Ability(
                "perfectionist",
                "xskills:ability-perfectionist",
                "xskills:abilitydesc-perfectionist",
                3, 1));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "potter",
                "xskills:ability-potter",
                "xskills:abilitydesc-potter",
                5, 1, new int[] { 40 }));

            // chance to instantly finish a pottering work
            // 0: base value
            // 1: value per level
            // 2: max value
            FastPotterId = this.AddAbility(new Ability(
                "fastpotter",
                "xskills:ability-fastpotter",
                "xskills:abilitydesc-fastpotter",
                5, 3, new int[] { 1, 1, 2, 2, 2, 4, 2, 2, 6 }));

            // chance to duplicate an item
            // 0: base value
            // 1: value per level
            // 2: max value
            JackPotId = this.AddAbility(new Ability(
                "jackpot",
                "xskills:ability-jackpot",
                "xskills:abilitydesc-jackpot",
                5, 3, new int[] { 5, 0, 5, 5, 1, 15, 5, 1, 25 }));

            // increases the radius of perfectfit and perfectionist abilities
            // 0: value
            InfallibleId = this.AddAbility(new Ability(
                "infallible",
                "xskills:ability-infallible",
                "xskills:abilitydesc-infallible",
                5, 2, new int[] { 1, 2 }));

            // chance to get a unique storage vessel
            // 0: chance
            InspirationId = this.AddAbility(new Ability(
                "inspiration",
                "xskills:ability-inspiration",
                "xskills:abilitydesc-inspiration",
                7, 2, new int[] { 10, 20 }));

            // chance to get a unique storage vessel
            // 0: chance
           PotteryTimerId = this.AddAbility(new Ability(
                "potterytimer",
                "xskills:ability-potterytimer",
                "xskills:abilitydesc-potterytimer",
                8));

            InspirationCollectibles = new Dictionary<string, List<CollectibleObject>>();
            InspirationCollectibles.Add("clayplanter", null);
            InspirationCollectibles.Add("flowerpot", null);
            InspirationCollectibles.Add("storagevessel", null);

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 40;
            this.ExpMult = 10.0f;
            this.ExpEquationValue = 0.8f;
        }
    }//! class Pottery
}//!namespace Xskills
