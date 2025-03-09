using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsAnimalBehavior : XSkillsEntityBehavior
    {
        public IPlayer Feeder 
        { 
            get 
            {
                string uid = entity.WatchedAttributes.GetString("owner");
                if (uid == null) return null;
                return entity.Api.World.PlayerByUid(uid); 
            }
            set 
            {
                if (value != null) entity.WatchedAttributes.SetString("owner", value.PlayerUID);
                else entity.WatchedAttributes.RemoveAttribute("owner");
            }
        }
        public bool Catchable { get; set; }

        public override string PropertyName() => "XSkillsAnimal";

        public XSkillsAnimalBehavior(Entity entity) : base(entity)
        {
            this.Catchable = false;
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            this.Catchable = attributes["catchable"].AsBool(false);
        }

        protected override Skill GetPlayerSkill()
        {
            return this.husbandry;
        }

        public override float OnDamage(float damage, DamageSource dmgSource)
        {
            EntityPlayer byPlayer =
                dmgSource.SourceEntity as EntityPlayer ??
                dmgSource.CauseEntity as EntityPlayer ??
                (dmgSource.SourceEntity as EntityThrownStone)?.FiredBy as EntityPlayer;
            if (this.husbandry == null || byPlayer == null) return damage;
            damage = base.OnDamage(damage, dmgSource);

            PlayerAbility playerAbility = byPlayer.GetBehavior<PlayerSkillSet>()?[this.husbandry.Id]?[this.husbandry.HunterId];
            if (playerAbility == null) return damage;
            damage *= 1.0f + playerAbility.SkillDependentFValue();
            return damage;
        }

        public override void GetInfoText(StringBuilder infotext)
        {
            base.GetInfoText(infotext);
            if (Catchable) infotext.AppendLine(Lang.Get("xskills:catchable"));
            IPlayer player = Feeder;
            if (player == null) return;
            infotext.AppendLine(Lang.Get("xskills:owner-desc", player.PlayerName));
        }
    }//!class XSkillsAnimalBehavior
}//!namespace XSkills
