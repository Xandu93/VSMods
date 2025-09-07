using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsToolBehavior : XLibToolBehavior
    {
        public XSkillsToolBehavior(CollectibleObject collObj) : base(collObj)
        {}

        public override int OnGetMaxDurability(ItemStack itemstack, ref EnumHandling bhHandling)
        {
            if (itemstack.Collectible.Durability <= 1) return 0;
            bhHandling = EnumHandling.Handled;
            float quality = itemstack.Attributes.GetFloat("quality", 0.0f);
            return (int)(itemstack.Collectible.Durability * quality * 0.05f);
        }

        public override float OnGetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref EnumHandling bhHandling)
        {
            float result = base.OnGetMiningSpeed(itemstack, blockSel, block, forPlayer, ref bhHandling);
            float quality = itemstack.Attributes.GetFloat("quality", 0.0f);
            return result * quality * 0.02f;
        }

        public override void OnDamageItem(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, ref int amount, ref EnumHandling bhHandling)
        {
            string sskill = null;
            ItemStack itemstack = itemslot.Itemstack;
            if (itemstack == null || byEntity == null) return;
            switch (itemstack.Collectible.Tool)
            {
                case EnumTool.Pickaxe:
                    sskill = "mining";
                    break;
                case EnumTool.Axe:
                    sskill = "forestry";
                    break;
                case EnumTool.Shovel:
                    sskill = "digging";
                    break;
                default:
                    sskill = null;
                    break;
            }
            if (sskill == null) return;

            PlayerSkillSet skillSet = byEntity.GetBehavior<PlayerSkillSet>();
            if (skillSet == null) return;

            CollectingSkill skill = skillSet.XLeveling.GetSkill(sskill) as CollectingSkill;
            if (skill == null) return;

            PlayerAbility ability = skillSet[skill.Id][skill.DurabilityId];
            if (ability != null)
            {
                float bonus = amount * ability.SkillDependentFValue();
                amount -= (int)bonus;
                bonus -= (int)bonus;
                if (bonus >= world.Rand.NextDouble()) amount--;
            }
        }
    }
}
