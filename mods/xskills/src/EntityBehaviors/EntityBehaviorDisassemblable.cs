using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class EntityBehaviorDisassemblable : EntityBehaviorHarvestable
    {
        WorldInteraction[] interactions = null;

        public override string PropertyName() => "disassemblable";
        public string Animation { get; set; }

        public EntityBehaviorDisassemblable(Entity entity) : base(entity)
        {
            Animation = "hammerhit";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
        }

        public override WorldInteraction[] GetInteractionHelp(IClientWorldAccessor world, EntitySelection es, IClientPlayer player, ref EnumHandling handled)
        {
            interactions = ObjectCacheUtil.GetOrCreate(world.Api, "disassemblableEntityInteractions", () =>
            {
                List<ItemStack> hammerStacklist = new List<ItemStack>();

                foreach (Item item in world.Items)
                {
                    if (item.Code == null) continue;

                    if (item.Tool == EnumTool.Hammer /*|| item.Tool == EnumTool.Wrench || item.Tool == EnumTool.Pickaxe*/)
                    {
                        hammerStacklist.Add(new ItemStack(item));
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-creature-harvest",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "sneak",
                        Itemstacks = hammerStacklist.ToArray()
                    }
                };
            });

            if (player == null) return null;
            PlayerSkill playerSkill = player.Entity.GetBehavior<PlayerSkillSet>()?.FindSkill("metalworking");
            if (playerSkill == null) return null;
            Metalworking metalworking = playerSkill.Skill as Metalworking;
            if (metalworking == null) return null;
            if (playerSkill[metalworking.SalvagerId].Tier <= 0) return null;

            return !entity.Alive && !IsHarvested ? interactions : null;
        }
    }//!class EntityBehaviorDisassemblable
}//!namespace XSkills
