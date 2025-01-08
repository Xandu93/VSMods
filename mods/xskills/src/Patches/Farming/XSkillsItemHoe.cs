using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace XSkills
{
    public class XSkillsItemHoe : ItemHoe
    {
        SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            HoeUtil.OnLoaded(api, ref toolModes);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            base.OnUnloaded(api);
            HoeUtil.OnUnloaded(toolModes);
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return HoeUtil.GetToolModes(api, toolModes, forPlayer);
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return HoeUtil.GetToolMode(api, slot, byPlayer);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (HoeUtil.DoTill(api, slot, byEntity, blockSel))
                base.DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return HoeUtil.GetHeldInteractionHelp(api, inSlot).Append(base.GetHeldInteractionHelp(inSlot));
        }
    }//!public class XSkillsItemHoe
}//!namespace XSkills
