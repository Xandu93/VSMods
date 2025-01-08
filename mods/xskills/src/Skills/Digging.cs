using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class Digging : CollectingSkill
    {
        internal List<Item> ClayItems = new List<Item>();

        //ability ids
        public int ClayDiggerId { get; private set; }
        public int PeatCutterId { get; private set; }
        public int SaltpeterDiggerId { get; private set; }
        public int MixedClayId { get; private set; }
        public int ScrapDetectorId { get; private set; }
        public int ScrapSpecialistId { get; private set; }

        private Dictionary<string, BlockDropItemStack[]> panningDrops;
        internal List<PanningDrop> StoneDrops { get; private set; }

        public Dictionary<string, BlockDropItemStack[]> PanningDrops
        {
            get
            {
                if (panningDrops == null) this.LoadPanningDrops();
                return panningDrops;
            }
            private set => panningDrops = value;
        }

        public Digging(ICoreAPI api) : base("digging")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);
            this.Tool = EnumTool.Shovel;

            // more clay drops
            // 0: base value
            // 1: value per level
            // 2: max value
            ClayDiggerId = this.AddAbility(new Ability(
                "claydigger",
                "xskills:ability-claydigger",
                "xskills:abilitydesc-claydigger",
                1, 3, new int[] { 10, 2, 30, 20, 4, 60, 20, 4, 100 }));

            // more peat drops
            // 0: base value
            // 1: value per level
            // 2: max value
            PeatCutterId = this.AddAbility(new Ability(
                "peatcutter",
                "xskills:ability-peatcutter",
                "xskills:abilitydesc-peatcutter",
                1, 3, new int[] { 10, 2, 30, 20, 4, 60, 20, 4, 100 }));

            // more saltpeter drops
            // 0: base value
            // 1: value per level
            // 2: max value
            SaltpeterDiggerId = this.AddAbility(new Ability(
                "saltpeterdigger",
                "xskills:ability-saltpeterdigger",
                "xskills:abilitydesc-saltpeterdigger",
                1, 3, new int[] { 10, 2, 30, 20, 4, 60, 20, 4, 100 }));

            // momentum
            // 0: base value
            // 1: value per level
            // 2: max effect value
            // 3: max stacks
            // 4: duration
            MiningSpeedId = this.AddAbility(new Ability(
                "shovelexpert",
                "xskills:ability-shovelexpert",
                "xskills:abilitydesc-shovelexpert",
                1, 3, new int[] { 1, 1, 2, 10,  4,
                                  2, 2, 4, 10,  6,
                                  2, 2, 6, 10,  8 }));

            // less durability usage
            // 0: base value
            // 1: value per level
            // 2: max effect value
            DurabilityId = this.AddAbility(new Ability(
                "carefuldigger",
                "xskills:ability-carefuldigger",
                "xskills:abilitydesc-carefuldigger",
                1, 3, new int[] { 5, 1, 15, 5, 2, 25, 5, 2, 45 }));

            // different clay type from clay blocks
            // 0: chance
            MixedClayId = this.AddAbility(new Ability(
                "mixedclay",
                "xskills:ability-mixedclay",
                "xskills:abilitydesc-mixedclay",
                3, 2, new int[] { 50, 100 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "digger",
                "xskills:ability-digger",
                "xskills:abilitydesc-digger",
                5, 1, new int[] { 40 }));

            // sand and gravel blocks can be sieved
            // 0: chance
            ScrapDetectorId = this.AddAbility(new Ability(
                "scrapdetector",
                "xskills:ability-scrapdetector",
                "xskills:abilitydesc-scrapdetector",
                5, 2, new int[] { 2, 4 }));

            // increased chance to sieve rare items
            // 0: chance
            // 1: bonus loot
            ScrapSpecialistId = this.AddAbility(new Ability(
                "scrapspecialist",
                "xskills:ability-scrapspecialist",
                "xskills:abilitydesc-scrapspecialist",
                5, 2, new int[] { 1, 50, 2, 100 }));

            //behaviors
            api.RegisterBlockBehaviorClass("XSkillsSoil", typeof(XSkillsSoilBehavior));
            api.RegisterBlockBehaviorClass("XSkillsPeat", typeof(XSkillsPeatBehavior));
            api.RegisterBlockBehaviorClass("XSkillsClay", typeof(XSkillsClayBehavior));
            api.RegisterBlockBehaviorClass("XSkillsSalpeter", typeof(XSkillsSalpeterBehavior));
            api.RegisterBlockBehaviorClass("XSkillsSand", typeof(XSkillsSandBehavior));

            ClassExpMultipliers["commoner"] = 0.1f;
            ClassExpMultipliers["miner"] = 0.2f;
            ClassExpMultipliers["archer"] = -0.2f;
            ClassExpMultipliers["hunter"] = -0.15f;
            ClassExpMultipliers["malefactor"] = 0.1f;
            ClassExpMultipliers["clockmaker"] = -0.15f;
            ClassExpMultipliers["blackguard"] = 0.1f;
            ClassExpMultipliers["forager"] = 0.15f;
            ClassExpMultipliers["vanguard"] = -0.05f;
            ClassExpMultipliers["gatherer"] = 0.15f;
            ClassExpMultipliers["mercenary"] = -0.05f;
        }

        protected void LoadPanningDrops()
        {
            PanningDrops = new Dictionary<string, BlockDropItemStack[]>();
            this.StoneDrops = new List<PanningDrop>();

            ICoreAPI api = this.XLeveling?.Api;
            BlockPan pan = api?.World.GetBlock(new AssetLocation("game", "pan-wooden")) as BlockPan;
            if (pan == null) return;
            Dictionary<string, PanningDrop[]> dropDic = pan.Attributes["panningDrops"].AsObject<Dictionary<string, PanningDrop[]>>();
            if (dropDic == null) return;

            foreach (string key in dropDic.Keys)
            {
                List<BlockDropItemStack> itemStacks = new List<BlockDropItemStack>();
                PanningDrop[] panningDrops = dropDic[key];

                foreach (PanningDrop drop in panningDrops)
                {
                    if (drop.Code.GetName().Contains("{rocktype}"))
                    {
                        this.StoneDrops.Add(drop);
                        continue;
                    }
                    CollectibleObject collectible;
                    if (drop.Type == EnumItemClass.Block) collectible = api.World.GetBlock(new AssetLocation(drop.Code.Path));
                    else collectible = api.World.GetItem(new AssetLocation(drop.Code.Path));
                    if (collectible != null)
                    {
                        BlockDropItemStack temp = new BlockDropItemStack(new ItemStack(collectible));
                        temp.Quantity = drop.Chance;
                        itemStacks.Add(temp);
                    }
                }

                if (itemStacks.Count == 0)
                {
                    this.XLeveling.Mod.Logger.Warning("Failed to resolve any panning drops for key: " + key);
                }
                PanningDrops[key] = itemStacks.ToArray();
            }
        }
    }//!class Digging

    public class XSkillsSoilBehavior : CollectingBehavior
    {
        protected Digging digging;

        public override CollectingSkill Skill => digging;
        public override EnumTool? Tool => EnumTool.Shovel;
        public override PlayerAbility DropBonusAbility(PlayerSkill playerSkill) => null;

        public XSkillsSoilBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.digging = XLeveling.Instance(api)?.GetSkill("digging") as Digging;
        }
    }//!class XSkillsSoilBehavior

    public class XSkillsPeatBehavior : XSkillsSoilBehavior
    {
        public XSkillsPeatBehavior(Block block) : base(block)
        { }

        public override PlayerAbility DropBonusAbility(PlayerSkill skill)
        {
            return skill[digging.PeatCutterId];
        }
    }

    public class XSkillsClayBehavior : XSkillsSoilBehavior
    {
        public XSkillsClayBehavior(Block block) : base(block)
        { }

        public override PlayerAbility DropBonusAbility(PlayerSkill skill)
        {
            return skill[digging.ClayDiggerId];
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            Digging digging = Skill as Digging;
            if (digging == null) return;
            foreach (BlockDropItemStack blockDrop in this.block.Drops)
            {
                Item drop = blockDrop.ResolvedItemstack?.Item;
                if (drop == null) continue;
                if (drop.Code.Path.Contains("clay") && !digging.ClayItems.Contains(drop))
                {
                    digging.ClayItems.Add(drop);
                }
            }
        }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = base.GetDropsList(world, pos, byPlayer, dropChanceMultiplier, ref handling);
            if (drops.Count == 0) return drops;

            //mixed clay
            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()[this.digging.Id];
            PlayerAbility playerAbility = playerSkill.PlayerAbilities[this.digging.MixedClayId];
            if (playerAbility.FValue(0) >= world.Rand.NextDouble())
            {
                Digging digging = Skill as Digging;
                int tries = 0;
                Item clay = null;
                while (clay == null && tries < 10)
                {
                    clay = digging.ClayItems[world.Rand.Next(digging.ClayItems.Count)];
                    foreach (ItemStack itemStack in drops)
                    {
                        if (itemStack.Item == clay)
                        {
                            clay = null; 
                            break;
                        }
                    }
                }
                if (clay != null)
                {
                    drops.Add(new ItemStack(clay));
                }
            }
            return drops;
        }
    }//!class XSkillsClayBehavior

    public class XSkillsSalpeterBehavior : XSkillsSoilBehavior
    {
        public override PlayerAbility DropBonusAbility(PlayerSkill skill)
        {
            return skill[this.digging.SaltpeterDiggerId];
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            ItemStack[] drops = new ItemStack[] { };
            if (this.Skill == null) return drops;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return drops;

            PlayerAbility playerAbility = this.DropBonusAbility(playerSkill);
            if (playerAbility == null) return drops;
            dropChanceMultiplier += playerAbility.Ability.ValuesPerTier >= 3 ? 0.01f * playerAbility.SkillDependentValue() : 0.01f * playerAbility.Value(0);
            return drops;
        }

        public XSkillsSalpeterBehavior(Block block) : base(block)
        { }
    }//!class XSkillsClayBehavior

    public class XSkillsSandBehavior : XSkillsSoilBehavior
    {
        public XSkillsSandBehavior(Block block) : base(block)
        { }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.Skill == null) return drops;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.Skill.Id];
            if (playerSkill == null) return drops;

            if (block.Drops.Length == 0) return drops;

            //scrap detector
            PlayerAbility playerAbility = playerSkill[this.digging.ScrapDetectorId];
            if (playerAbility == null) return drops;

            if (playerAbility.Value(0) * 0.01f > world.Rand.NextDouble())
            {
                ItemStack drop;
                BlockDropItemStack[] panningDrops;
                if (!digging.PanningDrops.TryGetValue("@(sand|gravel)-.*", out panningDrops)) return drops;
                handling = EnumHandling.PreventDefault;
                float dropMultipier = dropChanceMultiplier * 8.0f;

                //scrap specialist
                playerAbility = playerSkill.PlayerAbilities[this.digging.ScrapSpecialistId];

                for (int ii = 0; ii < panningDrops.Length; ii++)
                {
                    if (panningDrops[ii].Quantity.avg <= playerAbility.Value(0) * 0.005f)
                    {
                        drop = panningDrops[ii].GetNextItemStack(dropMultipier * ( 1.0f + playerAbility.Value(1) * 0.01f));
                    }
                    else
                    {
                        drop = panningDrops[ii].GetNextItemStack(dropMultipier);
                    }
                    if (drop != null) drops.Add(drop);
                }
            }
            return drops;
        }
    }//!class XSkillsSandBehavior
}//!namespace XSkills
