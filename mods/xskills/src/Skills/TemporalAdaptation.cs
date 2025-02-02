using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    public class TemporalAdaptation : XSkill
    {
        public int TemporalStableId { get; private set; }
        public int CavemanId { get; private set; }
        public int TemporalAdaptedId { get; private set; }
        public int TemporalRecoveryId { get; private set; }
        public int ShifterId { get; private set; }
        public int FastForwardId { get; private set; }
        public int StableMinerId { get; private set; }
        public int StableWarriorId { get; private set; }
        public int TemporalUnstableId { get; private set; }
        public int TimelessId { get; private set; }

        public TemporalAdaptation(ICoreAPI api) : base("temporaladaptation", "xskills:skill-temporaladaptation", "xskills:group-survival")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);

            // reduces the loss of temporal stability
            // 0: base value
            // 1: value per level
            // 2: max value
            TemporalStableId = this.AddAbility(new Ability(
                "temporalstable",
                "xskills:ability-temporalstable",
                "xskills:abilitydesc-temporalstable",
                1, 3, new int[] { 5, 1, 15, 10, 2, 30, 10, 2, 50 }));

            // reduces the loss of temporal stability deep down
            // 0: base value
            // 1: value per level
            // 2: max value
            CavemanId = this.AddAbility(new Ability(
                "caveman",
                "xskills:ability-caveman",
                "xskills:abilitydesc-caveman",
                1, 3, new int[] { 10, 2, 30, 20, 3, 50, 30, 3, 90 }));

            // reduces the loss of temporal stability depending on your current stability
            // 0: base value
            // 1: value per level
            // 2: max value
            TemporalAdaptedId = this.AddAbility(new Ability(
                "temporaladapted",
                "xskills:ability-temporaladapted",
                "xskills:abilitydesc-temporaladapted",
                1, 3, new int[] { 10, 2, 30, 20, 3, 50, 30, 3, 90 }));

            // increases the recovery rate of your temporal stability
            // 0: value
            TemporalRecoveryId = this.AddAbility(new Ability(
                "temporalrecovery",
                "xskills:ability-temporalrecovery",
                "xskills:abilitydesc-temporalrecovery",
                1, 2, new int[] { 50, 100 }));

            // chance to avoid attacks when the player is temporal unstable
            // 0: value
            ShifterId = this.AddAbility(new Ability(
                "shifter",
                "xskills:ability-shifter",
                "xskills:abilitydesc-shifter",
                3, 3, new int[] { 11, 22, 33}));

            // increased mining speed and food consumption
            // 0: value
            FastForwardId = this.AddAbility(new Ability(
                "fastforward",
                "xskills:ability-fastforward",
                "xskills:abilitydesc-fastforward",
                3, 2, new int[] { 10, 20 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "timelord",
                "xskills:ability-timelord",
                "xskills:abilitydesc-timelord",
                5, 1, new int[] { 40 }));

            // increased yield from all ores in a temporal stable area
            // 0: value
            StableMinerId = this.AddAbility(new Ability(
                "stableminer",
                "xskills:ability-stableminer",
                "xskills:abilitydesc-stableminer",
                5, 2, new int[] { 10, 20 }));

            // increased damage in a temporal stable area
            // 0: value
            StableWarriorId = this.AddAbility(new Ability(
                "stablewarrior",
                "xskills:ability-stablewarrior",
                "xskills:abilitydesc-stablewarrior",
                5, 2, new int[] { 10, 20 }));

            // inverts stable miner and stable warrior and inceases values
            // 0: value
            TemporalUnstableId = this.AddAbility(new Ability(
                "temporalunstable",
                "xskills:ability-temporalunstable",
                "xskills:abilitydesc-temporalunstable",
                10, 3, new int[] { 33, 66, 100 }));

            // no damage from temporal stability
            TimelessId = this.AddAbility(new Ability(
                "timeless",
                "xskills:ability-timeless",
                "xskills:abilitydesc-timeless",
                10, 1, new int[] { }));

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 200;
            this.ExpMult = 100.0f;
            this.ExpEquationValue = 8.0f;
        }

    }//!class TemporalAdaptation
}//!namespace XSkills
