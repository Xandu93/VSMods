using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsItemPickaxe : Item
    {
        SkillItem[] toolModes;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api is ICoreClientAPI capi)
            {
                toolModes = ObjectCacheUtil.GetOrCreate(api, "pickaxeToolModes", () =>
                {
                    SkillItem[] modes = new SkillItem[3];

                    modes[0] = new SkillItem() { Code = new AssetLocation("1size"), Name = Lang.Get("1x1") }.WithIcon(capi, ItemClay.Drawcreate1_svg);
                    modes[1] = new SkillItem() { Code = new AssetLocation("3size"), Name = Lang.Get("3x3") }.WithIcon(capi, (new ItemClay()).Drawcreate9_svg);
                    modes[2] = new SkillItem() { Code = new AssetLocation("vein"), Name = Lang.Get("vein") }.WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/heatmap.svg"), 48, 48, 5, ColorUtil.WhiteArgb));

                    return modes;
                });
            }
        }
        public override void OnUnloaded(ICoreAPI api)
        {
            if (toolModes == null) return;
            for (int ii = 0; ii < toolModes.Length; ii++)
            {
                toolModes[ii]?.Dispose();
            }
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            Mining mining = XLeveling.Instance(api)?.GetSkill("mining") as Mining;
            if (mining == null) return null;
            PlayerSkill playerSkill = forPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[mining.Id];
            if (playerSkill == null) return null;
            PlayerAbility tunnelDigger = playerSkill[mining.TunnelDiggerId];
            PlayerAbility veinMiner = playerSkill[mining.VeinMinerId];

            int count = 1 + (tunnelDigger?.Tier > 0 ? 1 : 0) + (veinMiner?.Tier > 0 ? 1 : 0);
            if (count == 1) return null;
            SkillItem[] modes = new SkillItem[count];

            modes[0] = toolModes[0];
            count = 1;
            if (tunnelDigger.Tier > 0)
            {
                modes[count] = toolModes[1];
                count++;
            }
            if (veinMiner.Tier > 0) modes[count] = toolModes[2];

            return modes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            Mining mining = XLeveling.Instance(api)?.GetSkill("mining") as Mining;
            if (mining == null) return 0;
            PlayerSkill playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[mining.Id];
            if (playerSkill == null) return 0;
            PlayerAbility tunnelDigger = playerSkill[mining.TunnelDiggerId];
            PlayerAbility veinMiner = playerSkill[mining.VeinMinerId];

            int count = 1 + (tunnelDigger?.Tier > 0 ? 1 : 0) + (veinMiner?.Tier > 0 ? 1 : 0);

            return GameMath.Min(slot.Itemstack.Attributes.GetInt("toolMode"), count);
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            WorldInteraction[] temp = base.GetHeldInteractionHelp(inSlot);
            IPlayer byPlayer = (inSlot.Inventory as InventoryBasePlayer)?.Player;

            Mining mining = XLeveling.Instance(api)?.GetSkill("mining") as Mining;
            if (mining == null) return temp;
            PlayerSkill playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[mining.Id];
            if (playerSkill == null) return temp;
            PlayerAbility tunnelDigger = playerSkill[mining.TunnelDiggerId];
            PlayerAbility veinMiner = playerSkill[mining.VeinMinerId];

            if (tunnelDigger?.Tier == 0 && veinMiner?.Tier == 0) return temp;

            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-selecttoolmode",
                    HotKeyCode = "toolmodeselect",
                    MouseButton = EnumMouseButton.None
                }
            }.Append(temp);
        }
    }//!public class XSkillsItemPickaxe
}//!namespace XSkills
