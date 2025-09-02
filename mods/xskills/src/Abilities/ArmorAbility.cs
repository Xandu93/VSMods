using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Config;
using XLib.XLeveling;

namespace XSkills
{
    public class ArmorAbility : Ability
    {
        // bonus traits have decreased values because all traits are negative
        public List<string> BonusTraits { get; protected set; }
        public List<string> MalusTraits { get; protected set; }
        public Dictionary<string, string> TraitTranslations { get; protected set; }

        public ArmorAbility(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        {
            InitTraits();
        }

        public ArmorAbility(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        {
            InitTraits();
        }

        protected virtual void InitTraits()
        {
            BonusTraits = new List<string>();
            MalusTraits = new List<string>();
            TraitTranslations = new Dictionary<string, string>();
        }

        public override void OnTierChanged(PlayerAbility playerAbility, int oldTier)
        {
            base.OnTierChanged(playerAbility, oldTier);
            (Skill as Combat)?.ApplyArmorAbilities(playerAbility.PlayerSkill.PlayerSkillSet.Player);
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
                    bonus += Lang.Get(TraitTranslations[str]);
                    if (str != BonusTraits.Last()) bonus += ", ";
                }

                string malus = "";
                foreach (string str in MalusTraits)
                {
                    malus += Lang.Get(TraitTranslations[str]);
                    if (str != MalusTraits.Last()) malus += ", ";
                }

                if (MalusTraits.Count > 0 && BonusTraits.Count > 0)
                {
                    return string.Format(this.Description, this.Values[begin], this.Values[begin + 1], bonus, malus);
                }
                else if (MalusTraits.Count > 0)
                {
                    return string.Format(this.Description, this.Values[begin], malus);
                }
                else if (BonusTraits.Count > 0)
                {
                    return string.Format(this.Description, this.Values[begin], bonus);
                }
                else
                {
                    return base.FormattedDescription(tier);
                }
            }
            catch (Exception error)
            {
                this.Skill.XLeveling.Api.Logger.Error(error.Message + "[" + "Ability: " + this.Name + "]");
                return this.Description;
            }
        }
    }

    public class HeavyArmorExpertAbility : ArmorAbility
    {
        public HeavyArmorExpertAbility(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        {}

        public HeavyArmorExpertAbility(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        {}

        protected override void InitTraits()
        {
            BonusTraits = new List<string>()
            {
               "healingeffectivness", "hungerrate"
            };
            MalusTraits = new List<string>()
            {
                "rangedWeaponsAcc", "rangedWeaponsSpeed"
            };

            if (XSkills.Instance.Api.ModLoader.IsModEnabled("combatoverhaul"))
            {
                MalusTraits.Add("steadyAim");
            }

            TraitTranslations = new Dictionary<string, string>()
            {
                {"healingeffectivness", "game:Healing effectivness"},
                {"hungerrate", "game:Hunger rate"},
                {"rangedWeaponsAcc", "game:Ranged Accuracy"},
                {"rangedWeaponsSpeed", "game:Ranged Charge Speed"},
                {"steadyAim", "combatoverhaul:stat-steadyAim"},
            };
        }
    }

    public class ArmoredAgilityAbility : ArmorAbility
    {
        public ArmoredAgilityAbility(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        { }

        public ArmoredAgilityAbility(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        { }

        protected override void InitTraits()
        {
            BonusTraits = new List<string>()
            {
               "walkspeed"
            };
            MalusTraits = new List<string>();
            TraitTranslations = new Dictionary<string, string>()
            {
                {"walkspeed", "game:Walkspeed"},
            };
        }
    }

    public class LightArmorExpertAbility : ArmorAbility
    {
        public LightArmorExpertAbility(string name, string displayName = null, string description = null, int minLevel = 1, int maxTier = 1, int valuesPerTier = 0, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, valuesPerTier, hideUntilLearnable)
        { }

        public LightArmorExpertAbility(string name, string displayName, string description, int minLevel, int maxTier, int[] values, bool hideUntilLearnable = false)
            : base(name, displayName, description, minLevel, maxTier, values, hideUntilLearnable)
        { }

        protected override void InitTraits()
        {
            BonusTraits = new List<string>()
            {
               "healingeffectivness", "hungerrate", "rangedWeaponsAcc", "rangedWeaponsSpeed"
            };
            MalusTraits = new List<string>();
            if (XSkills.Instance.Api.ModLoader.IsModEnabled("combatoverhaul"))
            {
                BonusTraits.Add("steadyAim");
            }

            TraitTranslations = new Dictionary<string, string>()
            {
                {"healingeffectivness", "game:Healing effectivness"},
                {"hungerrate", "game:Hunger rate"},
                {"rangedWeaponsAcc", "game:Ranged Accuracy"},
                {"rangedWeaponsSpeed", "game:Ranged Charge Speed"},
                {"steadyAim", "combatoverhaul:stat-steadyAim"},
            };
        }
    }
}//!namespace XSkills
