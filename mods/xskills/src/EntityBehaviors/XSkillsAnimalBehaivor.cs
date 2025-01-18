using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsAnimalBehavior : EntityBehavior
    {
        protected Husbandry husbandry;
        protected float xp;

        public IPlayer Feeder { get; internal set; }
        public bool Catchable { get; set; }

        public override string PropertyName() => "XSkillsAnimal";

        public XSkillsAnimalBehavior(Entity entity) : base(entity)
        {
            this.husbandry = XLeveling.Instance(entity.Api)?.GetSkill("husbandry") as Husbandry;
            this.xp = 0.0f;
            this.Catchable = false;
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            this.xp = attributes["xp"].AsFloat(0.0f);
            this.Catchable = attributes["catchable"].AsBool(false);
            EntityBehaviorHealth behaviorHealth = (this.entity.GetBehavior("health") as EntityBehaviorHealth);
            if (behaviorHealth != null) behaviorHealth.onDamaged += OnDamage;
        }

        public float OnDamage(float damage, DamageSource dmgSource)
        {
            EntityPlayer byPlayer =
                dmgSource.SourceEntity as EntityPlayer ??
                dmgSource.CauseEntity as EntityPlayer ??
                (dmgSource.SourceEntity as EntityThrownStone)?.FiredBy as EntityPlayer;
            if (this.husbandry == null || byPlayer == null) return damage;

            PlayerAbility playerAbility = byPlayer.GetBehavior<PlayerSkillSet>()?[this.husbandry.Id]?[this.husbandry.HunterId];
            if (playerAbility == null) return damage;
            damage *= 1.0f + playerAbility.SkillDependentFValue();
            return damage;
        }

        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            if (damageSourceForDeath == null) return;
            EntityPlayer byPlayer =
                damageSourceForDeath.SourceEntity as EntityPlayer ??
                damageSourceForDeath.CauseEntity as EntityPlayer ??
                (damageSourceForDeath.SourceEntity as EntityThrownStone)?.FiredBy as EntityPlayer;
            if (this.husbandry == null || byPlayer == null) return;

            PlayerSkill playerSkill = byPlayer.GetBehavior<PlayerSkillSet>()?[this.husbandry.Id];
            if (playerSkill == null) return;

            //experience
            float generationBonus = 1.0f + (Math.Min(entity.WatchedAttributes.GetInt("generation", 0), 20) * 0.05f) ;
            playerSkill.AddExperience(generationBonus * this.xp);
        }
    }//!class XSkillsAnimalBehavior
}//!namespace XSkills
