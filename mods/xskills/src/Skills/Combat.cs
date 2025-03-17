using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using XLib.XLeveling;

namespace XSkills
{
    public class Combat : XSkill
    {
        //ability ids
        public int SwordsmanId { get; private set; }
        public int ArcherId { get; private set; }
        public int SpearmanId { get; private set; }
        //public int TankId { get; private set; }
        //public int DefenderId { get; private set; }
        //public int GuardianId { get; private set; }
        public int ToolMasteryId { get; private set; }
        public int IronFistId { get; private set; }
        public int MonkId { get; private set; }
        public int LooterId { get; private set; }
        public int SniperId { get; private set; }
        public int FreshFleshId { get; private set; }
        public int ShovelKnightId { get; private set; }
        //public int HeavyArmorExpertId { get; private set; }
        public int AdrenalineRushId { get; private set; }
        public int VampireId { get; private set; }
        public int DrunkenMasterId { get; private set; }
        public int BurningRageId { get; private set; }
        public int BloodlustId { get; private set; }
        public int MonsterExpertId { get; private set; }
        public Combat(ICoreAPI api) : base("combat", "xskills:skill-combat", "xskills:group-survival")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);

            // increases damage with swords
            // 0: base value
            // 1: value per level
            // 2: max value
            SwordsmanId = this.AddAbility(new Ability(
                "swordsman",
                "xskills:ability-swordsman",
                "xskills:abilitydesc-swordsman",
                1, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            // increases damage with bows
            // 0: base value
            // 1: value per level
            // 2: max value
            ArcherId = this.AddAbility(new Ability(
                "archer",
                "xskills:ability-archer",
                "xskills:abilitydesc-archer",
                1, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            // increases damage with spears
            // 0: base value
            // 1: value per level
            // 2: max value
            SpearmanId = this.AddAbility(new Ability(
                "spearman",
                "xskills:ability-spearman",
                "xskills:abilitydesc-spearman",
                1, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            //// increases damage absorbed by shields
            //// 0: base value
            //// 1: value per level
            //// 2: max value
            //TankId = this.AddAbility(new Ability(
            //    "tank",
            //    "xskills:ability-tank",
            //    "xskills:abilitydesc-tank",
            //    1, 2, new int[] { 10, 1, 20, 20, 1, 40 }));

            //// increases active chance for damage absorption by shields
            //// 0: base value
            //DefenderId = this.AddAbility(new Ability(
            //    "defender",
            //    "xskills:ability-defender",
            //    "xskills:abilitydesc-defender",
            //    1, 2, new int[] { 3, 5 }));

            //// increases passive chance for damage absorption by shields
            //// 0: base value
            //GuardianId = this.AddAbility(new Ability(
            //    "guardian",
            //    "xskills:ability-guardian",
            //    "xskills:abilitydesc-guardian",
            //    1, 2, new int[] { 5, 10 }));

            // increases damage with tools
            // 0: base value
            // 1: value per level
            // 2: max value
            ToolMasteryId = this.AddAbility(new Ability(
                "toolmastery",
                "xskills:ability-toolmastery",
                "xskills:abilitydesc-toolmastery",
                1, 3, new int[] { 5, 2, 25, 15, 3, 40, 15, 3, 75 }));

            // increases damage with bare hands with armor
            // 0: base value
            IronFistId = this.AddAbility(new Ability(
                "ironfist",
                "xskills:ability-ironfist",
                "xskills:abilitydesc-ironfist",
                1, 3, new int[] { 2, 3, 4 }));

            // increases damage with bare hands without armor
            // 0: base value
            MonkId = this.AddAbility(new Ability(
                "monk",
                "xskills:ability-monk",
                "xskills:abilitydesc-monk",
                1, 3, new int[] { 6, 9, 12 }));

            // more mob drops
            // 0: base value
            // 1: value per level
            // 2: max value
            LooterId = this.AddAbility(new Ability(
                "looter",
                "xskills:ability-looter",
                "xskills:abilitydesc-looter",
                1, 2, new int[] { 10, 1, 20, 20, 2, 40 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "warrior",
                "xskills:ability-warrior",
                "xskills:abilitydesc-warrior",
                5, 1, new int[] { 40 }));

            string stat = api.ModLoader.IsModEnabled("combatoverhaul") ? "steadyAim" : "rangedWeaponsAcc";
            //more accuracy with bows
            //0: value
            SniperId = this.AddAbility(new StatAbility(
                "sniper", stat,
                "xskills:ability-sniper",
                "xskills:abilitydesc-sniper",
                5, 2, new int[] { 15, 30 }));

            // gives saturation
            // 0: value
            FreshFleshId = this.AddAbility(new Ability(
                "freshflesh",
                "xskills:ability-freshflesh",
                "xskills:abilitydesc-freshflesh",
                 5, 3, new int[] { 10, 20, 30 }));

            // shovels have a chance to deal 30 times more damage
            // 0: chance
            // 1: damage multiplier
            ShovelKnightId = this.AddAbility(new Ability(
                "shovelknight",
                "xskills:ability-shovelknight",
                "xskills:abilitydesc-shovelknight",
                5, 2, new int[] { 1, 10, 2, 15 }));

            ////// increases and reduces values for some armor traits
            ////// 0: boni
            ////// 1: mali
            //HeavyArmorExpertId = this.AddAbility(new HeavyArmorExpertAbility(
            //    "heavyarmorexpert",
            //    "xskills:ability-heavyarmorexpert",
            //    "xskills:abilitydesc-heavyarmorexpert",
            //    6, 2, new int[] { 20, 40, 40, 60 }));

            //chance to trigger an adrenaline rush
            //0: threshold
            //1: speed boost
            //2: damage reduction
            //3: duration
            //4: exhaustion duration
            AdrenalineRushId = this.AddAbility(new Ability(
                "adrenalinerush",
                "xskills:ability-adrenalinerush",
                "xskills:abilitydesc-adrenalinerush",
                7, 2, new int[] {20, 20, 25, 10, 24, 20, 40, 50, 12, 20}));

            // steal the health of enemies, reduces life reg at daytime
            // 0: life steal
            // 1: regeneration 
            VampireId = this.AddAbility(new Ability(
                "vampire",
                "xskills:ability-vampire",
                "xskills:abilitydesc-vampire",
                7, 3, new int[] { 3, 80, 5, 65, 7, 50 }));

            // increases your damage with your bare hands when you are drunk
            // 0: max damage bonus
            // 1: sober penalty
            DrunkenMasterId = this.AddAbility(new Ability(
                "drunkenmaster",
                "xskills:ability-drunkenmaster",
                "xskills:abilitydesc-drunkenmaster",
                8, 1, new int[] { 50, 50 }));

            // chance to ignite an enemy
            // 0: chance
            BurningRageId = this.AddAbility(new Ability(
                "burningrage",
                "xskills:ability-burningrage",
                "xskills:abilitydesc-burningrage",
                10, 3, new int[] { 2, 4, 6 }));

            // increases damage done and damage taken
            // 0: damage increase
            // 1: taken damage increase
            // 2: duration
            // 3: max stacks
            BloodlustId = this.AddAbility(new Ability(
                "bloodlust",
                "xskills:ability-bloodlust",
                "xskills:abilitydesc-bloodlust",
                10, 1, new int[] { 2, 3, 16, 10 }));

            // grants some additional informations about enemies
            MonsterExpertId = this.AddAbility(new Ability(
                "monsterexpert",
                "xskills:ability-monsterexpert",
                "xskills:abilitydesc-monsterexpert",
                10, 1, new int[] {}));

            //behaviors
            api.RegisterEntityBehaviorClass("XSkillsEntity", typeof(XSkillsEntityBehavior));

            ICoreServerAPI sapi = api as ICoreServerAPI;
            if (sapi != null)
            {
                sapi.Event.PlayerJoin += OnPlayerJoin;
            }

            this.Config = new CombatSkillConfig();
            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 100;
            this.ExpMult = 50.0f;
            this.ExpEquationValue = 4.0f;
        }

        public void OnPlayerJoin(IPlayer byPlayer)
        {
            if ((this.Config as CombatSkillConfig)?.enableAbilitiesInPvP ?? false)
            {
                XSkillsEntityBehavior beh = new XSkillsEntityBehavior(byPlayer.Entity);
                byPlayer.Entity.AddBehavior(beh);
            }
            //IInventory inv = byPlayer.InventoryManager.GetOwnInventory(GlobalConstants.characterInvClassName);
            //if (inv != null) inv.SlotModified += (int slotID) => 
            //    (this[HeavyArmorExpertId] as HeavyArmorExpertAbility)?.ApplyAbility(byPlayer);
        }

        public override void OnConfigReceived()
        {
            base.OnConfigReceived();
            if ((this.Config as CombatSkillConfig)?.enableAbilitiesInPvP ?? false)
            {
                Entity entity = (this.XLeveling.Api as ICoreClientAPI)?.World.Player.Entity;
                entity?.AddBehavior(new XSkillsEntityBehavior(entity));
            }
        }

    }//!class Combat

    [ProtoContract]
    public class CombatSkillConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("enableAbilitiesInPvP", this.enableAbilitiesInPvP.ToString());
                return result;
            }
            set
            {
                string str;
                value.TryGetValue("enableAbilitiesInPvP", out str);
                if (str != null) bool.TryParse(str, out this.enableAbilitiesInPvP);
            }
        }

        [ProtoMember(1)]
        public bool enableAbilitiesInPvP = false;
    }//!class CombatSkillConfig

    public class HeavyArmorExpertAbility : Ability
    {
        // bonus traits have decreased values because all traits are negative
        public List<string> BonusTraits { get; private set; }
        public List<string> MalusTraits { get; private set; }
        public Dictionary<string, string> TraitTranslation { get; private set; }

        public HeavyArmorExpertAbility(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        {
            InitTraits();
        }

        public HeavyArmorExpertAbility(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        {
            InitTraits();
        }

        private void InitTraits()
        {
            BonusTraits = new List<string>()
            {
               "healingeffectivness", "hungerrate"
            };
            MalusTraits = new List<string>()
            {
                "rangedWeaponsAcc", "rangedWeaponsSpeed", "steadyAim"
            };
            TraitTranslation = new Dictionary<string, string>()
            {
                {"healingeffectivness", "game:Healing effectivness"},
                {"hungerrate", "game:Hunger rate"},
                {"rangedWeaponsAcc", "game:Ranged Accuracy"},
                {"rangedWeaponsSpeed", "game:Ranged Charge Speed"},
                {"steadyAim", "combatoverhaul:stat-steadyAim"},
            };
        }

        public override void OnTierChanged(PlayerAbility playerAbility, int oldTier)
        {
            base.OnTierChanged(playerAbility, oldTier);
        }

        public void ApplyAbility(IPlayer player)
        {
            PlayerSkill playerSkill = player.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return;
            PlayerAbility playerAbility = playerSkill[this.Id];
            if (playerAbility == null) return;

            EntityStats stats = player.Entity?.Stats;
            if (stats == null) return;

            foreach (string statName in BonusTraits)
            {
                EntityFloatStats stat = stats[statName];
                if (stat == null) continue;
                stat.ValuesByKey.TryGetValue("wearablemod", out EntityStat<float> temp);
                float value = temp?.Value ?? 0.0f;
                stat.ValuesByKey.TryGetValue("CombatOverhaul:Armor", out temp);
                value = temp?.Value ?? 0.0f;
                stat.Set("ability-armorexpert", -value * playerAbility.FValue(0));
            }

            foreach (string statName in MalusTraits)
            {
                EntityFloatStats stat = stats[statName];
                if (stat == null) continue;
                stat.ValuesByKey.TryGetValue("wearablemod", out EntityStat<float> temp);
                float value = temp?.Value ?? 0.0f;
                stat.ValuesByKey.TryGetValue("CombatOverhaul:Armor", out temp);
                value = temp?.Value ?? 0.0f;
                stat.Set("ability-armorexpert", value * playerAbility.FValue(1));
            }
        }

        public override string FormattedDescription(int tier)
        {
            tier = Math.Min(this.MaxTier, Math.Max(tier, 1));
            int begin = this.ValuesPerTier * (tier - 1);

            try
            {
                string bonus = "";
                foreach (string str in BonusTraits)
                {
                    bonus += Lang.Get(TraitTranslation[str]);
                    if (str != BonusTraits.Last()) bonus += ", ";
                }

                string malus = "";
                foreach (string str in MalusTraits)
                {
                    malus += Lang.Get(TraitTranslation[str]);
                    if (str != MalusTraits.Last()) malus += ", ";
                }

                return string.Format(this.Description, this.Values[begin], this.Values[begin + 1], bonus, malus);
            }
            catch (Exception error)
            {
                this.Skill.XLeveling.Api.Logger.Error(error.Message + "[" + "Ability: " + this.Name + "]");
                return this.Description;
            }
        }
    }
}//!namespace XSkills
