using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace XSkills
{
    public class PickaxeBehavior : CollectibleBehavior
    {
        private SkillItem[] toolModes;
        public SkillItem[] ToolModes
        {
            get
            {
                //set this after all pickaxe behaviors are loaded to have a full array
                toolModes ??= (api != null ? ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes") : null);
                return toolModes;
            }
        }
        ICoreAPI api;
        int offset = -1;

        public PickaxeBehavior(CollectibleObject collObj) : base(collObj)
        { }

        public static SkillItem[] CreateToolModes(ICoreAPI api)
        {
            SkillItem[] modes = new SkillItem[2];
            modes[0] = new SkillItem() { Code = new AssetLocation("1size"), Name = Lang.Get("1x1") };
            modes[1] = new SkillItem() { Code = new AssetLocation("vein"), Name = Lang.Get("3x3") + " / " + Lang.Get("vein") };

            if (api is ICoreClientAPI capi)
            {
                modes[0].WithIcon(capi, ItemClay.Drawcreate1_svg);
                modes[1].WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation("textures/icons/heatmap.svg"), 48, 48, 5, ColorUtil.WhiteArgb));
                return modes;
            }
            return modes;
        }

        //don't set the tool modes here but wait until all mods have registered their modes
        //just add it to the ObjectCache
        public override void OnLoaded(ICoreAPI api)
        {
            this.api = api;
            SkillItem[] modes = ObjectCacheUtil.TryGet<SkillItem[]>(api, "pickaxeToolModes");
            if (modes == null)
            {
                modes = ObjectCacheUtil.GetOrCreate(api, "pickaxeToolModes", () =>
                {
                    return CreateToolModes(api);
                });
                offset = 0;
            }
            else
            {
                for (int ii = 0; ii < modes.Length; ++ii)
                {
                    if (modes[ii].Code.Path == "1size")
                    {
                        offset = ii;
                        break;
                    }
                }
                //expand tool modes if they are not registered already
                if (offset == -1)
                {
                    offset = modes.Length;
                    modes = modes.Append(CreateToolModes(api));
                    api.ObjectCache["pickaxeToolModes"] = modes;
                }
            }
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            this.api = api;
            if (ToolModes == null) return;
            for (int ii = offset; ii < offset + 2; ++ii)
            {
                ToolModes[ii]?.Dispose();
            }
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel, int toolMode)
        {
            slot.Itemstack.Attributes.SetInt("toolMode", toolMode);
        }

        //returns a tool mode instead of just an id
        public static SkillItem GetToolModeItem(ItemSlot slot, IPlayer forPlayer, BlockSelection blockSel)
        {
            if (slot.Itemstack == null) return null;
            SkillItem[] modes = GetToolModeItems(slot);
            if (modes == null) return null;
            int modeID = slot.Itemstack.Collectible.GetToolMode(slot, forPlayer, blockSel);
            if (modeID >= modes.Length || modeID < 0) return modes[0];
            return modes[modeID];
        }

        //works also on server not only client like GetToolModes
        public static SkillItem[] GetToolModeItems(ItemSlot slot)
        {
            PickaxeBehavior beh = slot.Itemstack.Collectible.GetCollectibleBehavior<PickaxeBehavior>(false);
            return beh?.ToolModes;
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
        {
            return ToolModes;
        }

        public override int GetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
        {
            return slot.Itemstack.Attributes.GetInt("toolMode");
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-selecttoolmode",
                    HotKeyCode = "toolmodeselect",
                    MouseButton = EnumMouseButton.None
                }
            };
        }
    }
}