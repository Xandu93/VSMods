using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    internal class HoeUtil
    {
        public static void RegisterItemHoePrimitive(ClassRegistry registry)
        {
            registry.ItemClassToTypeMapping["ItemHoeExtended"] = typeof(XSkillsItemHoePrimitive);
        }
        public static void RegisterItemHoe(ClassRegistry registry)
        {
            registry.ItemClassToTypeMapping["ItemHoe"] = typeof(XSkillsItemHoe);
        }

        public static void OnLoaded(ICoreAPI api, ref SkillItem[] toolModes)
        {
            if (api is ICoreClientAPI capi)
            {
                toolModes = ObjectCacheUtil.GetOrCreate(api, "hoeToolModes", () =>
                {
                    SkillItem[] modes = new SkillItem[3];

                    modes[0] = new SkillItem() { Code = new AssetLocation("1size"), Name = Lang.Get("1x1") }.WithIcon(capi, ItemClay.Drawcreate1_svg);
                    modes[1] = new SkillItem() { Code = new AssetLocation("2size"), Name = Lang.Get("2x2") }.WithIcon(capi, ItemClay.Drawcreate4_svg);
                    modes[2] = new SkillItem() { Code = new AssetLocation("3size"), Name = Lang.Get("3x3") }.WithIcon(capi, (new ItemClay()).Drawcreate9_svg);

                    return modes;
                });
            }
        }

        public static void OnUnloaded(SkillItem[] toolModes)
        {
            if (toolModes == null) return;
            for (int ii = 0; ii < toolModes.Length; ii++)
            {
                toolModes[ii]?.Dispose();
            }
        }

        public static int AbilityTier(ICoreAPI api, IPlayer player)
        {
            Farming farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
            if (farming == null) return 0;
            PlayerAbility playerAbility = player?.Entity?.GetBehavior<PlayerSkillSet>()?[farming.Id][farming.ExtensiveFarmingId];
            return playerAbility?.Tier ?? 0;
        }

        public static int AbilityTier(ICoreAPI api, IPlayer player, ref int value)
        {
            Farming farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
            if (farming == null) return 0;
            PlayerAbility playerAbility = player?.Entity?.GetBehavior<PlayerSkillSet>()?[farming.Id][farming.ExtensiveFarmingId];
            value = playerAbility.Value(0);
            return playerAbility?.Tier ?? 0;
        }

        public static SkillItem[] GetToolModes(ICoreAPI api, SkillItem[] toolModes, IClientPlayer forPlayer)
        {
            int tier = AbilityTier(api, forPlayer);
            return tier > 0 ? toolModes.Copy(0, tier + 1) : null;
        }

        public static int GetToolMode(ICoreAPI api, ItemSlot slot, IPlayer byPlayer)
        {
            int tier = AbilityTier(api, byPlayer);
            return GameMath.Min(slot.Itemstack.Attributes.GetInt("toolMode"), tier);
        }

        public static bool DoTill(ICoreAPI api, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel)
        {
            if (blockSel == null) return false;
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;

            int range = 0;
            int tier = AbilityTier(api, byPlayer, ref range);
            if (tier <= 0) return true;

            int toolMode = GetToolMode(api, slot, byPlayer);
            if (toolMode <= 0) return true;

            int used = 0;
            int x = blockSel.Position.X;
            int y = blockSel.Position.Y;
            int z = blockSel.Position.Z;

            int directionX = 0;
            int directionZ = 0;

            if (range % 2 == 0)
            {
                if (x - byEntity.Pos.X >= 0.0f) directionX = 1;
                if (z - byEntity.Pos.Z >= 0.0f) directionZ = 1;
            }

            x = x - range / 2 + directionX;
            z = z - range / 2 + directionZ;

            AssetLocation sound = null;

            for (int xx = x; xx < x + range; ++xx)
            {
                for (int zz = z; zz < z + range; ++zz)
                {
                    Block block = api.World.BlockAccessor.GetBlock(new BlockPos(xx, y + 1, zz, blockSel.Position.dimension));
                    if (!(block?.Id == 0)) continue;

                    block = api.World.BlockAccessor.GetBlock(new BlockPos(xx, y, zz, blockSel.Position.dimension));
                    if (!block.Code.Path.StartsWith("soil")) continue;

                    string fertility = block.LastCodePart(1);
                    Block farmland = byEntity.World.GetBlock(new AssetLocation("farmland-dry-" + fertility));
                    if (farmland == null) continue;

                    BlockPos pos = new BlockPos(xx, y, zz, blockSel.Position.dimension);
                    api.World.BlockAccessor.SetBlock(farmland.BlockId, pos);
                    used++;

                    BlockEntity be = byEntity.World.BlockAccessor.GetBlockEntity(pos);
                    if (be is BlockEntityFarmland)
                    {
                        ((BlockEntityFarmland)be).OnCreatedFromSoil(block);
                    }
                    api.World.BlockAccessor.MarkBlockDirty(pos);

                    if (byPlayer != null && block.Sounds != null && sound == null) sound = block.Sounds.Place;
                }
            }

            used = (int)(used * 0.5f + 0.6f);
            if (used > 0) slot.Itemstack.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot, used);
            if (slot.Empty)
            {
                byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.Y, byEntity.Pos.Z);
            }

            if (sound != null) byEntity.World.PlaySoundAt(sound, blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, null);
            return false;
        }

        public static WorldInteraction[] GetHeldInteractionHelp(ICoreAPI api, ItemSlot inSlot)
        {
            IPlayer byPlayer = (inSlot.Inventory as InventoryBasePlayer)?.Player;
            if (byPlayer == null) return System.Array.Empty<WorldInteraction>();
            int tier = AbilityTier(api, byPlayer);
            if (tier <= 0) return System.Array.Empty<WorldInteraction>();

            return new WorldInteraction[] {
                new WorldInteraction()
                {
                    ActionLangCode = "blockhelp-selecttoolmode",
                    HotKeyCode = "toolmodeselect",
                    MouseButton = EnumMouseButton.None
                }
            };
        }
    }//!class HoeUtil
}//!namespace XSkills
