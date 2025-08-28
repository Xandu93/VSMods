using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;
using ProtoBuf;
using Vintagestory.ServerMods;
using System.ComponentModel;
using System.Globalization;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Util;

namespace XSkills
{
    public class Mining : CollectingSkill
    {
        protected Dictionary<BlockPos, IPlayer> BombPosToPlayerMap;
        protected Dictionary<string, float> OreRarities;

        //ability ids
        public int StoneBreakerId { get; private set; }
        public int StoneCutterId { get; private set; }
        public int OreMinerId { get; private set; }
        public int GemstoneMinerId { get; private set; }
        public int CrystalSeekerId { get; private set; }
        public int BombermanId { get; private set; }
        public int GeologistId { get; private set; }
        public int VeinMinerId { get; private set; }
        public int TunnelDiggerId { get; private set; }
        public int BlasterId { get; private set; }

        public Mining(ICoreAPI api) : base("mining")
        {
            XLeveling.Instance(api).RegisterSkill(this);
            this.Tool = EnumTool.Pickaxe;

            this.BombPosToPlayerMap = new Dictionary<BlockPos, IPlayer>();
            this.Config = new MiningSkillConfig();
            if(!(this.Config as MiningSkillConfig).geologistBlacklist.Contains("quartz"))
                (this.Config as MiningSkillConfig).geologistBlacklist.Add("quartz");

            // more stone drops
            // 0: base value
            // 1: value per level
            // 2: max value
            StoneBreakerId = this.AddAbility(new Ability(
                "stonebreaker",
                "xskills:ability-stonebreaker",
                "xskills:abilitydesc-stonebreaker",
                1, 3, new int[] { 10, 2, 30, 20, 4, 60, 20, 4, 100 }));

            // chance to get a complete stone instead of broken ones
            // 0: base value
            // 1: value per level
            // 2: max value
            StoneCutterId = this.AddAbility(new Ability(
                "stonecutter",
                "xskills:ability-stonecutter",
                "xskills:abilitydesc-stonecutter",
                1, 3, new int[] { 2, 1, 12, 4, 2, 24, 4, 2, 44 }));

            // more ore drops
            // 0: base value
            // 1: value per level
            // 2: max value
            OreMinerId = this.AddAbility(new Ability(
                "oreminer",
                "xskills:ability-oreminer",
                "xskills:abilitydesc-oreminer",
                 1, 3, new int[] { 5, 1, 15, 10, 2, 30, 10, 2, 50 }));

            // more gemstone drops
            // 0: base value
            // 1: value per level
            // 2: max value
            GemstoneMinerId = this.AddAbility(new Ability(
                "gemstoneminer",
                "xskills:ability-gemstoneminer",
                "xskills:abilitydesc-gemstoneminer",
                5, 3, new int[] { 5, 1, 15, 10, 2, 30, 10, 2, 50 }));

            // momentum
            // 0: base value
            // 1: value per level
            // 2: max effect value
            // 3: max stacks
            // 4: duration
            MiningSpeedId = this.AddAbility(new Ability(
                "pickaxeexpert",
                "xskills:ability-pickaxeexpert",
                "xskills:abilitydesc-pickaxeexpert",
                1, 3, new int[] { 1, 1, 2, 10,  4,
                                  2, 2, 4, 10,  6,
                                  2, 2, 6, 10,  8 }));

            // less durability usage
            // 0: base value
            // 1: value per level
            // 2: max effect value
            DurabilityId = this.AddAbility(new Ability(
                "carefulminer",
                "xskills:ability-carefulminer",
                "xskills:abilitydesc-carefulminer",
                1, 3, new int[] { 5, 1, 15, 5, 2, 25, 5, 2, 45 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "miner",
                "xskills:ability-miner",
                "xskills:abilitydesc-miner",
                5, 1, new int[] { 40 }));

            // more crystal drops
            // 0: value
            CrystalSeekerId = this.AddAbility(new Ability(
                "crystalseeker",
                "xskills:ability-crystalseeker",
                "xskills:abilitydesc-crystalseeker",
                5, 1, new int[] { 100 }));

            // less drop penalties for bombs
            // 0: value
            BombermanId = this.AddAbility(new Ability(
                "bomberman",
                "xskills:ability-bomberman",
                "xskills:abilitydesc-bomberman",
                7, 3, new int[] { 33, 66, 100 }));

            //notification for nearby ore blocks
            // 0: range
            GeologistId = this.AddAbility(new Ability(
                "geologist",
                "xskills:ability-geologist",
                "xskills:abilitydesc-geologist",
                10, 3, new int[] { 1, 2, 3 }));

            //vein mining
            // 0: base value
            // 1: levels for increased value
            // 2: max
            // 3: durability cost in percentage
            // 4: saturation cost
            VeinMinerId = this.AddAbility(new Ability(
                "veinminer",
                "xskills:ability-veinminer",
                "xskills:abilitydesc-veinminer",
                12, 1, new int[] { 2, 3, 10, 100, 10}));

            // you can mine a 3 by 3 area
            // 0: durability cost in percentage
            // 1: saturation cost
            TunnelDiggerId = this.AddAbility(new Ability(
                "tunneldigger",
                "xskills:ability-tunneldigger",
                "xskills:abilitydesc-tunneldigger",
                12, 1, new int[] { 50, 20 }));

            //blocks are affected by abilities when they exploded
            BlasterId = this.AddAbility(new Ability(
                "blaster",
                "xskills:ability-blaster",
                "xskills:abilitydesc-blaster",
                12, 1));

            //behaviors
            api.RegisterBlockBehaviorClass("XSkillsStone", typeof(XSkillsStoneBehavior));
            api.RegisterBlockBehaviorClass("XSkillsOres", typeof(XSkillsOreBehavior));
            api.RegisterBlockBehaviorClass("XSkillsGems", typeof(XSkillsGemBehavior));
            api.RegisterBlockBehaviorClass("XSkillsBomb", typeof(XSkillsBombBehavior));
            api.RegisterCollectibleBehaviorClass("XSkillsPickaxeBehavior", typeof(PickaxeBehavior));
        }

        internal void RegisterExplosion(BlockPos pos, IPlayer player)
        {
            if (pos == null || player == null) return;
            this.BombPosToPlayerMap.Add(pos, player);
            Action<float> action = new Action<float>((float f) => this.UnregisterExplosion(pos));
            this.XLeveling.Api.World.RegisterCallback(action, 1000);
        }

        internal void UnregisterExplosion(BlockPos pos)
        {
            if (pos == null) return;
            this.BombPosToPlayerMap.Remove(pos);
            return;
        }

        internal IPlayer GetPlayerCausingExplosion(BlockPos pos)
        {
            if (pos == null) return null;
            this.BombPosToPlayerMap.TryGetValue(pos, out IPlayer result);
            return result;
        }

        public float GetOreRarity(string ore)
        {
            if (ore == null) return 0.0f;
            if (OreRarities == null)
            {
                IWorldAccessor world = XLeveling.Api.World;
                OreRarities = new Dictionary<string, float>();
                float mostRare = 0.0f;
                float mostCommon = 1.0f;
                DepositVariant[] deposits = XLeveling.Api.ModLoader.GetModSystem<GenDeposits>()?.Deposits;
                if (deposits == null) return 0.0f;

                foreach (DepositVariant deposit in deposits)
                {
                    DiscDepositGenerator generator = deposit.GeneratorInst as DiscDepositGenerator;
                    if (generator == null)
                        continue;

                    AssetLocation oreLoc = generator.PlaceBlock.Code.Clone();
                    oreLoc.Path = oreLoc.Path.Replace("{rock}", "*");
                    Block[] blocks = world.SearchBlocks(oreLoc);
                    if (blocks.Length <= 0 || blocks[0].BlockMaterial != EnumBlockMaterial.Ore)
                    {
                        continue;
                    }
                    if (generator.SurfaceBlockChance <= 0.1f)
                    {
                        float quantity = (float)generator.absAvgQuantity;
                        if (!OreRarities.TryGetValue(ore, out float oldValue)) oldValue = 1.0f;
                        quantity /= world.BlockAccessor.MapSizeY * GlobalConstants.ChunkSize * GlobalConstants.ChunkSize;
                        float rarity = oldValue - quantity;
                        mostRare = Math.Max(rarity, mostRare);
                        mostCommon = Math.Min(rarity, mostCommon);
                        OreRarities[deposit.Code] = rarity;
                    }
                    else OreRarities[deposit.Code] = 0.0f;
                }
                List<string> keys = new List<string>(OreRarities.Keys);
                mostRare -= mostCommon;
                foreach (string key in keys)
                {
                    OreRarities[key] = Math.Max((OreRarities[key] - mostCommon) / mostRare, 0.0f);
                }
            }
            float value;
            if (OreRarities.TryGetValue(ore, out value)) return value;
            return 0.0f;
        }

        //method for geologist
        public void CheckBlock(IWorldAccessor world, IClientPlayer byPlayer, int x, int y, int z, int dimension, int range)
        {
            ICoreClientAPI capi = world.Api as ICoreClientAPI;
            if (capi == null) return;

            BlockOre block;
            BlockOre nearestBlock = null;
            Vec3i nearPos = new Vec3i();
            int nearest = (range + 1) * 3;

            for (int ix = x - range; ix <= x + range; ix++)
            {
                for (int iy = y - range; iy <= y + range; iy++)
                {
                    for (int iz = z - range; iz <= z + range; iz++)
                    {
                        block = world.BlockAccessor.GetBlock(new BlockPos(ix, iy, iz, dimension)) as BlockOre;
                        if (block != null)
                        {
                            int distance = Math.Abs(ix - x) + Math.Abs(iy - y) + Math.Abs(iz - z);
                            if (distance < nearest && !(Config as MiningSkillConfig).geologistBlacklist.Contains(block.OreName))
                            {
                                nearest = distance;
                                nearestBlock = block;
                                nearPos.Set(ix, iy, iz);
                            }
                        }
                    }
                }
            }
            if (nearestBlock != null && nearest > 1)
            {
                ClientEventManager eventManager = (capi.World as ClientMain)?.eventManager;
                if (eventManager == null) return;
                Vec3i rel = new Vec3i(nearPos.X - x, nearPos.Y - y, nearPos.Z - z);

                string msg = "[" + rel.X.ToString() + ", " + rel.Y.ToString() + ", " + rel.Z.ToString() + "]";
                msg += Lang.Get("game:ore-" + (nearestBlock.Variant?["type"] ?? "")) + Lang.GetUnformatted("xskills:isnearby");
                eventManager.TriggerNewServerChatLine(GlobalConstants.InfoLogChatGroup, msg, EnumChatType.Notification, null);
            }
        }
    }//!class Mining

    [ProtoContract]
    public class MiningSkillConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                CultureInfo provider = new CultureInfo("en-US");

                Dictionary<string, string> result = new Dictionary<string, string>();
                string str = string.Join(", ", this.geologistBlacklist);
                result.Add("geologistBlacklist", str);
                result.Add("oreRarityExpMultiplier", this.oreRarityExpMultiplier.ToString(provider));
                result.Add("oreDepthExpMultiplier", this.oreDepthExpMultiplier.ToString(provider));
                return result;
            }
            set
            {
                string str;
                NumberStyles styles = NumberStyles.Any;
                CultureInfo provider = new CultureInfo("en-US");

                value.TryGetValue("geologistBlacklist", out str);
                if (str == null) return;
                string[] blacklist = str.Split(new char[] { ',' });
                foreach (string ss in blacklist)
                {
                    string str2 = ss.Trim();
                    if(!this.geologistBlacklist.Contains(str2)) this.geologistBlacklist.Add(str2);
                }

                value.TryGetValue("oreRarityExpMultiplier", out str);
                if (str != null) float.TryParse(str, styles, provider, out oreRarityExpMultiplier);
                value.TryGetValue("oreDepthExpMultiplier", out str);
                if (str != null) float.TryParse(str, styles, provider, out oreDepthExpMultiplier);

                if (oreRarityExpMultiplier > 1.0f) oreRarityExpMultiplier *= 0.01f;
                if (oreDepthExpMultiplier > 1.0f) oreDepthExpMultiplier *= 0.01f;
            }
        }

        [ProtoMember(1)]
        public List<string> geologistBlacklist;

        [ProtoMember(2)]
        [DefaultValue(0.5f)]
        public float oreRarityExpMultiplier;

        [ProtoMember(3)]
        [DefaultValue(0.5f)]
        public float oreDepthExpMultiplier;

        public MiningSkillConfig()
        {
            this.geologistBlacklist = new List<string>();
            this.oreRarityExpMultiplier = 0.5f;
            this.oreDepthExpMultiplier = 0.5f;
        }
    }//!class MiningSkillConfig

    public class XSkillsExplodableBehavior : CollectingBehavior
    {
        protected Mining mining;
        protected TemporalAdaptation temporalAdaptation;

        public override CollectingSkill Skill => mining;
        public override EnumTool? Tool => EnumTool.Pickaxe;
        public override PlayerAbility DropBonusAbility(PlayerSkill playerSkill) => null;

        public XSkillsExplodableBehavior(Block block) : base(block)
        {}

        public override void OnLoaded(ICoreAPI api)
        {
            this.mining = XLeveling.Instance(api)?.GetSkill("mining") as Mining;
            this.temporalAdaptation = XLeveling.Instance(api)?.GetSkill("temporaladaptation") as TemporalAdaptation;
        }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
        {
            if (this.mining == null) return;
            IPlayer byPlayer = this.mining.GetPlayerCausingExplosion(explosionCenter);
            if (byPlayer == null) return;

            PlayerSkill playerMining = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[this.mining.Id];
            if (playerMining == null) return;

            //experience
            //exploded blocks give only a tenth of the default experience
            float xp = (this as XSkillsOreBehavior)?.GetXP(world, pos, byPlayer) ?? this.xp;
            playerMining.AddExperience(xp * 0.1f);

            handling = EnumHandling.PreventDefault;
            world.BulkBlockAccessor.SetBlock(0, pos);
            double dropChance = this.block.ExplosionDropChance(world, pos, blastType);
            PlayerAbility playerAbility;

            //bomberman
            playerAbility = playerMining.PlayerAbilities[mining.BombermanId];
            dropChance += (1.0f - dropChance) * playerAbility.FValue(0);

            if (world.Rand.NextDouble() < dropChance)
            {
                //blaster
                ItemStack[] drops;
                playerAbility = playerMining.PlayerAbilities[mining.BlasterId];
                if (playerAbility.Tier > 0)
                {
                    drops = this.block.GetDrops(world, pos, byPlayer, 1.0f);
                }
                else
                {
                    drops = this.block.GetDrops(world, pos, null, 1.0f);
                }

                if (drops == null) return;

                for (int ii = 0; ii < drops.Length; ii++)
                {
                    if (this.block.SplitDropStacks)
                    {
                        for (int kk = 0; kk < drops[ii].StackSize; kk++)
                        {
                            ItemStack stack = drops[ii].Clone();
                            stack.StackSize = 1;
                            world.SpawnItemEntity(stack, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                        }
                    }
                    else
                    {
                        world.SpawnItemEntity(drops[ii], new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    }
                }
            }

            if (this.block.EntityClass != null)
            {
                BlockEntity entity = world.BlockAccessor.GetBlockEntity(pos);
                if (entity != null)
                {
                    entity.OnBlockBroken();
                }
            }
        }
    }//!class XSkillsExplodableBehavior

    public class XSkillsStoneBehavior : XSkillsExplodableBehavior
    {
        public override PlayerAbility DropBonusAbility(PlayerSkill playerSkill) => playerSkill[mining.StoneBreakerId];

        public XSkillsStoneBehavior(Block block) : base(block)
        { }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
            if (this.mining == null) return;

            PlayerSkill playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[this.mining.Id];
            if (playerSkill == null) return;

            PlayerAbility playerAbility;

            //geologist
            if (byPlayer is IClientPlayer clientPlayer)
            {
                playerAbility = playerSkill[this.mining.GeologistId];
                if (playerAbility?.Tier >= 1) this.mining.CheckBlock(world, clientPlayer, pos.X, pos.Y, pos.Z, pos.dimension, playerAbility.Value(0));
            }

            //tunnel digger
            ItemSlot toolSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
            playerAbility = playerSkill[this.mining.TunnelDiggerId];

            if (playerAbility == null || playerAbility.Tier <= 0) return;
            if (toolSlot?.Itemstack?.Item == null || byPlayer.CurrentBlockSelection == null) return;
            if (toolSlot.Itemstack.Item.Tool != EnumTool.Pickaxe) return;
            if (toolSlot.Itemstack.Item is ItemProspectingPick) return;
            if (byPlayer.CurrentBlockSelection.Position != pos) return;

            string toolMode = PickaxeBehavior.GetToolModeItem(toolSlot, byPlayer, byPlayer.CurrentBlockSelection)?.Code.Path;
            if (toolMode != "vein") return;

            PlayerAbility durabilityAbility = playerSkill[this.mining.DurabilityId];
            int toolDamage = 0;
            int brokenBlocks = 0;

            for (int ii = -1; ii <= 1; ++ii)
            {
                for (int jj = -1; jj <= 1; ++jj)
                {
                    if (ii == 0 && jj == 0) continue;
                    BlockPos otherPos;
                    switch(byPlayer.CurrentBlockSelection.Face.Axis)
                    {
                        case EnumAxis.X:
                            otherPos = new BlockPos(pos.X, pos.Y + ii, pos.Z + jj, pos.dimension);
                            break;
                        case EnumAxis.Y:
                            otherPos = new BlockPos(pos.X + ii, pos.Y, pos.Z + jj, pos.dimension);
                            break;
                        case EnumAxis.Z:
                            otherPos = new BlockPos(pos.X + ii, pos.Y + jj, pos.Z, pos.dimension);
                            break;
                        default:
                            continue;
                    }

                    Block otherBlock = world.BlockAccessor.GetBlock(otherPos);
                    if (otherBlock.Id == block.Id)
                    {
                        world.BlockAccessor.BreakBlock(otherPos, byPlayer, 1.0f);
                        brokenBlocks++;
                        if (!(durabilityAbility?.SkillDependentFValue() > world.Rand.NextDouble()))
                        {
                            toolDamage++;
                        }
                    }
                }
            }

            toolDamage = (int)(toolDamage * (1.0f + playerAbility.FValue(0)));
            toolDamage = Math.Min(toolDamage, toolSlot.Itemstack.Item.GetRemainingDurability(toolSlot.Itemstack) - 1);
            toolSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, toolSlot, toolDamage);
            byPlayer.Entity.GetBehavior<EntityBehaviorHunger>()?.ConsumeSaturation(brokenBlocks * playerAbility.Value(1));
        }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.Skill == null) return drops;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return drops;

            if (block.Drops.Length == 0) return drops;

            handling = EnumHandling.PreventDefault;

            //stone cutter
            PlayerAbility playerAbility = playerSkill.PlayerAbilities[mining.StoneCutterId];
            float dropMultipier = playerAbility != null ? playerAbility.SkillDependentFValue() : 0.0f;
            if (world.Rand.NextDouble() <= dropMultipier)
            {
                drops.Add(new ItemStack(this.block));
                return drops;
            }
            //test for cracked rocks
            //else if (world.Rand.NextDouble() <= dropMultipier && block.Code.Path.Contains("rock"))
            //{
            //    AssetLocation assetLocation = new AssetLocation(block.Code.Domain, block.Code.Path.Replace("rock", "crackedrock"));
            //    Block crackedBlock = world.GetBlock(assetLocation);
            //    if (crackedBlock != null)
            //    {
            //        drops.Add(new ItemStack(crackedBlock));
            //        return drops;
            //    }
            //}

            //stone breaker
            playerAbility = this.DropBonusAbility(playerSkill);
            dropMultipier = playerAbility != null ? dropChanceMultiplier + 0.01f * playerAbility.SkillDependentValue() : dropChanceMultiplier;
            for (int index = 0; index < block.Drops.Length; index++)
            {
                ItemStack drop = block.Drops[index].GetNextItemStack(dropMultipier);
                if (drop != null) drops.Add(drop);
            }
            return drops;
        }
    }//!class XSkillsStoneBehavior

    public class XSkillsOreBehavior : XSkillsExplodableBehavior
    {
        public override PlayerAbility DropBonusAbility(PlayerSkill playerSkill) => playerSkill[mining.OreMinerId];

        public override float GetXP(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            MiningSkillConfig config = mining.Config as MiningSkillConfig;
            float xp = this.xp * (
                1.0f +
                mining.GetOreRarity(block.FirstCodePart(2)) * config.oreRarityExpMultiplier +
                config.oreDepthExpMultiplier * (1.0f - Math.Min(pos.Y, world.SeaLevel) / world.SeaLevel));
            return xp;
        }

        public XSkillsOreBehavior(Block block) : base(block)
        { }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            ItemStack[] result = base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
            return result;
        }

        static protected bool veinMining = false;

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
            IClientPlayer cPlayer = byPlayer as IClientPlayer;
            if (this.mining == null) return;
            PlayerSkill playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[this.mining.Id];
            if (playerSkill == null) return;

            if (cPlayer != null && !veinMining)
            {
                BlockOre ore = this.block as BlockOre;

                //geologist only for quartz ore
                if (ore?.OreName == "quartz")
                {
                    PlayerAbility playerAbility = playerSkill[this.mining.GeologistId];
                    if (playerAbility?.Tier >= 1) this.mining.CheckBlock(world, cPlayer, pos.X, pos.Y, pos.Z, pos.dimension, playerAbility.Value(0));
                }
            }

            //vein miner
            if (!veinMining && world.Api.Side == EnumAppSide.Server)
            {
                ItemSlot toolSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                PlayerAbility playerAbility = playerSkill[this.mining.VeinMinerId];
                if (playerAbility?.Tier <= 0) return;

                Item tool = toolSlot?.Itemstack?.Item;
                if (tool == null) return;
                if (tool.Tool != EnumTool.Pickaxe) return;
                if (tool is ItemProspectingPick) return;

                string toolMode = PickaxeBehavior.GetToolModeItem(toolSlot, byPlayer, byPlayer.CurrentBlockSelection)?.Code.Path;
                if (toolMode != "vein") return;

                int max = Math.Min(playerAbility.Value(0) + playerSkill.Level / playerAbility.Value(1), playerAbility.Value(2));
                max = Math.Min(max, tool.GetRemainingDurability(toolSlot.Itemstack));

                EntityBehaviorHunger hunger = byPlayer.Entity.GetBehavior<EntityBehaviorHunger>();
                if (hunger?.Saturation < playerAbility.Value(4) * max) return;

                veinMining = true;
                List<BlockPos> toCheck = new List<BlockPos>();
                List<BlockPos> toMine = new List<BlockPos>();
                toCheck.Add(pos);
                toMine.Add(pos);

                while (toCheck.Count > 0 && toMine.Count < max)
                {
                    BlockPos blockPos = toCheck.PopOne();

                    ShouldVeinMine(block, blockPos.NorthCopy(), world, toCheck, toMine, max);
                    ShouldVeinMine(block, blockPos.SouthCopy(), world, toCheck, toMine, max);
                    ShouldVeinMine(block, blockPos.EastCopy(), world, toCheck, toMine, max);
                    ShouldVeinMine(block, blockPos.WestCopy(), world, toCheck, toMine, max);
                    ShouldVeinMine(block, blockPos.UpCopy(), world, toCheck, toMine, max);
                    ShouldVeinMine(block, blockPos.DownCopy(), world, toCheck, toMine, max);
                }

                int toolDamage = 0;
                PlayerAbility durabilityAbility = playerSkill[this.mining.DurabilityId];
                foreach (BlockPos blockPos in toMine)
                {
                    if (blockPos == pos) handling = EnumHandling.PreventSubsequent;
                    world.BlockAccessor.BreakBlock(blockPos, byPlayer, 1.0f);
                    if (!(durabilityAbility?.SkillDependentFValue() > world.Rand.NextDouble()))
                    {
                        toolDamage++;
                    }
                }
                veinMining = false;

                toolDamage = (int) (toolDamage * (1.0f + playerAbility.FValue(3)));
                toolDamage = Math.Min(toolDamage, toolSlot.Itemstack.Item.GetRemainingDurability(toolSlot.Itemstack) - 1);
                toolSlot.Itemstack.Item.DamageItem(world, byPlayer.Entity, toolSlot, toolDamage);
                hunger?.ConsumeSaturation(toMine.Count * playerAbility.Value(4));
            }
        }

        protected void ShouldVeinMine(Block block, BlockPos blockPos, IWorldAccessor world, List<BlockPos> toCheck, List<BlockPos> toMine, int max)
        {
            if (toMine.Count >= max) return;
            IBlockAccessor blockAccessor = world.BlockAccessor;
            Block other = blockAccessor.GetBlock(blockPos);
            if (other.Id == block.Id)
            {
                if (toMine.Contains(blockPos)) return;
                toMine.Add(blockPos);
                toCheck.Add(blockPos);
            }
        }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.Skill == null) return drops;
            PlayerSkill minerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (minerSkill == null) return drops;

            if (block.Drops.Length == 0) return drops;

            handling = EnumHandling.PreventDefault;

            //ore miner
            PlayerAbility playerAbility = DropBonusAbility(minerSkill);
            float dropMinerMultipier = playerAbility != null ? 0.01f * playerAbility.SkillDependentValue() : 0.0f;
            float dropStabilityMultipier = 0.0f;

            //stable miner and temporal unstable
            if (this.temporalAdaptation != null)
            {
                PlayerSkill temporalSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[this.temporalAdaptation.Id];
                SystemTemporalStability system = this.mining.XLeveling.Api.ModLoader.GetModSystem<SystemTemporalStability>();

                if (temporalSkill != null && system != null)
                {
                    playerAbility = temporalSkill[temporalAdaptation.StableMinerId];
                    PlayerAbility playerAbility2 = temporalSkill[temporalAdaptation.TemporalUnstableId];
                    float stability = Math.Clamp(system.GetTemporalStability(pos), 0.0f, 1.0f);

                    if (playerAbility != null && playerAbility2 != null)
                    {
                        if (playerAbility2.Tier > 0)
                            dropStabilityMultipier = (1.0f - stability) * playerAbility.FValue(0) * (1.0f + playerAbility2.FValue(0));
                        else
                            dropStabilityMultipier = stability * playerAbility.FValue(0);
                    }
                }
            }

            for (int index = 0; index < block.Drops.Length; index++)
            {
                float crystalizedMultipier = 1.0f;
                //crystal seeker
                if (block.Drops[index].ResolvedItemstack?.Item?.FirstCodePart() == "crystalizedore")
                {
                    playerAbility = minerSkill.PlayerAbilities[mining.CrystalSeekerId];
                    crystalizedMultipier = playerAbility != null ? 1.0f + playerAbility.Value(0) * 0.01f : 1.0f;
                }
                ItemStack drop = block.Drops[index].GetNextItemStack(dropChanceMultiplier + dropMinerMultipier * crystalizedMultipier + dropStabilityMultipier);
                if (drop != null) drops.Add(drop);
            }
            return drops;
        }
    }//!class XSkillsOreBehavior

    public class XSkillsGemBehavior : XSkillsOreBehavior
    {
        public override PlayerAbility DropBonusAbility(PlayerSkill playerSkill) => playerSkill[mining.GemstoneMinerId];
        public XSkillsGemBehavior(Block block) : base(block)
        { }
    }//!class XSkillsGemBehavior

    public class XSkillsBombBehavior : BlockBehavior
    {
        private Mining mining;

        public XSkillsBombBehavior(Block block) : base(block)
        { }

        public override void OnBlockExploded(IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType, ref EnumHandling handling)
        {
            if (this.mining == null)
            {
                this.mining = XLeveling.Instance(world.Api)?.GetSkill("mining") as Mining;
                if (this.mining == null) return;
            }
            if (pos == null) return;
            IPlayer player = this.mining.GetPlayerCausingExplosion(pos);
            if (player != null) this.mining.RegisterExplosion(pos, player);
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            if (this.mining == null)
            {
                this.mining = XLeveling.Instance(world.Api)?.GetSkill("mining") as Mining;
                if (this.mining == null) return;
            }

            BlockEntityBomb bomb = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBomb;
            if (bomb == null) return;

            TreeAttribute attributes = new TreeAttribute();
            bomb.ToTreeAttributes(attributes);
            IAttribute attribute = attributes.GetAttribute("ignitedByPlayerUid");
            if (attribute?.GetValue() == null) return;

            string UID = attribute.ToString();

            IPlayer byPlayer = world.PlayerByUid(UID);
            if (byPlayer != null && bomb.IsLit) this.mining.RegisterExplosion(pos, byPlayer);
        }
    }//!class XSkillsBombBehavior
}//!namespace XSkills
