using PrimitiveSurvival.ModSystem;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// I know this whole thing is a mess.
    /// </summary>
    public class XSkillsItemHoePrimitive : ItemHoe
    {
        SkillItem[] toolModes;
        ItemHoeExtended dummyHoe;

        public override void OnLoaded(ICoreAPI api)
        {
            dummyHoe ??= new ItemHoeExtended();
            dummyHoe.Attributes = this.Attributes;
            dummyHoe.Class = this.Class;
            dummyHoe.Code = this.Code;
            dummyHoe.Durability = this.Durability;
            typeof(ItemHoeExtended).GetField("api", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(dummyHoe, api);

            dummyHoe.OnLoaded(api);
            base.OnLoaded(api);
            HoeUtil.OnLoaded(api, ref toolModes);
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            dummyHoe?.OnUnloaded(api);
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
            if (!byEntity.Controls.Sneak)
            {
                if (HoeUtil.DoTill(api, slot, byEntity, blockSel))
                {
                    base.DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
                }
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return HoeUtil.GetHeldInteractionHelp(api, inSlot).Append(dummyHoe.GetHeldInteractionHelp(inSlot));
        }

        public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            dummyHoe.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            dummyHoe.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            if (secondsUsed > 0.6f && byEntity.Attributes.GetInt("didtill", 0) == 0 && 
                byEntity.World.Side == EnumAppSide.Server && !byEntity.Controls.ShiftKey)
            {
                DoTill(secondsUsed, slot, byEntity, blockSel, entitySel);
                byEntity.Attributes.SetInt("didtill", 1);
            }
            dummyHoe.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
            return secondsUsed < 1f;
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            return dummyHoe.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }

    }//!public class XSkillsItemHoe
}//!namespace XSkills
