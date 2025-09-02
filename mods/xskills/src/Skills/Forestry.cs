using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class Forestry : CollectingSkill
    {
        //ability ids
        public int LumberjackId { get; private set; }
        public int AfforestationId { get; private set; }
        public int MoreLaddersId { get; private set; }
        public int ResinFarmerId { get; private set; }
        public int TreeNurseryId { get; private set; }
        public int CharcoalBurnerId { get; private set; }
        public int StokerId { get; private set; }
        public int GrafterId { get; private set; }
        public int ResinExtractor { get; private set; }

        public Forestry(ICoreAPI api) : base("forestry")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);
            this.Tool = EnumTool.Axe;

            // more log drops
            // 0: base value
            // 1: value per level
            // 2: max value
            LumberjackId = this.AddAbility(new Ability(
                "lumberjack",
                "xskills:ability-lumberjack",
                "xskills:abilitydesc-lumberjack",
                1, 3, new int[] { 10, 1, 20, 20, 2, 40, 20, 2, 60 }));

            // more sapling drops
            // 0: base value 
            AfforestationId = this.AddAbility(new Ability(
                "afforestation",
                "xskills:ability-afforestation",
                "xskills:abilitydesc-afforestation",
                1, 2, new int[] { 10, 20 }));

            // more stick drops
            // 0: base value
            // 1: value per level
            // 2: max value
            MoreLaddersId = this.AddAbility(new Ability(
                "moreladders",
                "xskills:ability-moreladders",
                "xskills:abilitydesc-moreladders",
                1, 2, new int[] { 10, 2, 30, 20, 4, 60 }));

            //resin from pine
            // 0: base value
            ResinFarmerId = this.AddAbility(new Ability(
                "resinfarmer",
                "xskills:ability-resinfarmer",
                "xskills:abilitydesc-resinfarmer",
                1, 1, new int[] { 2 }));

            // faster tree growth
            // 0: tree growth duration multiplier
            TreeNurseryId = this.AddAbility(new Ability(
                "treenursery",
                "xskills:ability-treenursery",
                "xskills:abilitydesc-treenursery",
                3, 3, new int[] { 87, 74, 60 }));

            // momentum
            // 0: base value
            // 1: value per level
            // 2: max effect value
            // 3: max stacks
            // 4: duration
            MiningSpeedId = this.AddAbility(new Ability(
                "axeexpert",
                "xskills:ability-axeexpert",
                "xskills:abilitydesc-axeexpert",
                1, 3, new int[] { 1, 1, 2, 10, 30,
                                  2, 2, 4, 10, 40,
                                  2, 2, 6, 10, 45 }));

            // less durability usage
            // 0: base value
            // 1: value per level
            // 2: max effect value
            DurabilityId = this.AddAbility(new Ability(
                "carefullumberjack",
                "xskills:ability-carefullumberjack",
                "xskills:abilitydesc-carefullumberjack",
                1, 3, new int[] { 5, 1, 15, 5, 2, 25, 5, 2, 45 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "forester",
                "xskills:ability-forester",
                "xskills:abilitydesc-forester",
                5, 1, new int[] { 40 }));

            // more charcoal drops
            // 0: drop bonus
            CharcoalBurnerId = this.AddAbility(new Ability(
                "charcoalburner",
                "xskills:ability-charcoalburner",
                "xskills:abilitydesc-charcoalburner",
                5, 3, new int[] { 13, 26, 40 }));

            // faster charcoal shoveling
            // 0: chance to break an additional layer
            // note: can break multiple layers at once
            StokerId = this.AddAbility(new Ability(
                "stoker",
                "xskills:ability-stoker",
                "xskills:abilitydesc-stoker",
                5, 2, new int[] { 25, 50 }));

            //higher grafting and rooting success rate for fruit trees
            // 0: base value
            GrafterId = this.AddAbility(new Ability(
                "grafter",
                "xskills:ability-grafter",
                "xskills:abilitydesc-grafter",
                5, 2, new int[] { 50, 100 }));

            // resin from all trees
            // 0: percentage of the ResinFarmer value
            ResinExtractor = this.AddAbility(new Ability(
                "resinextractor",
                "xskills:ability-resinextractor",
                "xskills:abilitydesc-resinextractor",
                7, 1, new int[] { 50 }));

            //behaviors
            api.RegisterBlockBehaviorClass("XSkillsSapling", typeof(XSkillsSaplingBehavior));
            api.RegisterBlockBehaviorClass("XSkillsWood", typeof(XSkillsWoodBehavior));
            api.RegisterBlockBehaviorClass("XSkillsLeaves", typeof(XSkillsLeavesBehavior));
            api.RegisterBlockBehaviorClass("XSkillsCharcoal", typeof(XSkillsCharcoalBehavior));
        }
    }//!class Forestry

    public class XSkillsBlockEntitySapling : BlockEntitySapling
    {
        public float GrowthTimeMultiplier { set; get; }

        public XSkillsBlockEntitySapling() : base()
        {
            GrowthTimeMultiplier = 1.0f;
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("growthTimeMultiplier", GrowthTimeMultiplier);
        }
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            GrowthTimeMultiplier = tree.GetFloat("growthTimeMultiplier", 1.0f);
        }
    }//!class XSkillsBlockEntitySapling


    [HarmonyPatch(typeof(BlockEntitySapling))]
    public class BlockEntitySaplingPatch
    {
        [HarmonyPatch("CheckGrow")]
        public static void Prefix(BlockEntitySapling __instance, out EnumTreeGrowthStage __state, EnumTreeGrowthStage ___stage)
        {
            __state = ___stage;
        }

        [HarmonyPatch("CheckGrow")]
        public static void Postfix(BlockEntitySapling __instance, EnumTreeGrowthStage __state, EnumTreeGrowthStage ___stage, ref double ___totalHoursTillGrowth)
        {
            XSkillsBlockEntitySapling instance = __instance as XSkillsBlockEntitySapling;
            if (instance == null || __state == ___stage) return;

            double start = instance.Api.World.Calendar.TotalHours;
            double hours = (___totalHoursTillGrowth - start) * instance.GrowthTimeMultiplier;
            ___totalHoursTillGrowth = start + hours;
        }
    }//!class BlockEntitySaplingPatch

    public class XSkillsSaplingBehavior : BlockBehavior
    {
        protected Forestry forestry;

        public XSkillsSaplingBehavior(Block block) : base(block)
        { }

        public override void OnLoaded(ICoreAPI api)
        {
            this.forestry = XLeveling.Instance(api)?.GetSkill("forestry") as Forestry;
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            if (this.forestry == null || byPlayer == null || world.Api.Side == EnumAppSide.Client)
            {
                return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
            }
            Action<IWorldAccessor, BlockPos, float> action =
                new Action<IWorldAccessor, BlockPos, float>((worldAccessor, blockPos, f) => TreePlantedCallback(byPlayer, worldAccessor, blockPos));

            //Tree Nursery
            world.Api.Event.RegisterCallback(action, blockSel.Position, 40);
            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack, ref handling);
        }

        public void TreePlantedCallback(IPlayer player, IWorldAccessor world, BlockPos blockPos)
        {
            PlayerAbility playerAbility = player?.Entity?.GetBehavior<PlayerSkillSet>()?[this.forestry.Id]?[forestry.TreeNurseryId];
            if (playerAbility == null || playerAbility.Tier <= 0) return;

            //Tree Nursery
            XSkillsBlockEntitySapling sapling = world.BlockAccessor.GetBlockEntity(blockPos) as XSkillsBlockEntitySapling;
            if (sapling != null)
            {
                TreeAttribute attributes = new TreeAttribute();
                sapling.ToTreeAttributes(attributes);

                double start = world.Calendar.TotalHours;
                double hours = attributes.GetDouble("totalHoursTillGrowth", 1.0) - start;
                sapling.GrowthTimeMultiplier = playerAbility.FValue(0, 1.0f);
                attributes.SetFloat("growthTimeMultiplier", sapling.GrowthTimeMultiplier);
                hours *= sapling.GrowthTimeMultiplier;

                attributes.SetDouble("totalHoursTillGrowth", start + hours);
                sapling.FromTreeAttributes(attributes, world);
                sapling.MarkDirty();
            }
        }
    }//!class XSkillsSaplingBehavior

    public class XSkillsWoodBehavior : CollectingBehavior
    {
        protected Forestry forestry;

        public override CollectingSkill Skill => forestry;
        public override EnumTool? Tool => EnumTool.Axe;
        public override PlayerAbility DropBonusAbility(PlayerSkill skill) => skill[forestry.LumberjackId];

        public XSkillsWoodBehavior(Block block) : base(block)
        { }

        public override void OnLoaded(ICoreAPI api)
        {
            this.forestry = XLeveling.Instance(api)?.GetSkill("forestry") as Forestry;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            //try to to get forestry here again since the OnLoaded method is not called by bamboo 
            if (this.forestry == null)
                this.forestry = XLeveling.Instance(world.Api)?.GetSkill("forestry") as Forestry;
            base.OnBlockBroken(world, pos, byPlayer, ref handling);
        }

        public override List<ItemStack> GetDropsList(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = base.GetDropsList(world, pos, byPlayer, dropChanceMultiplier, ref handling);
            if (drops.Count == 0) return drops;

            //resin farmer and resin extractor
            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()[this.Skill.Id];
            PlayerAbility playerAbility = playerSkill[forestry.ResinFarmerId];
            PlayerAbility playerAbility2 = playerSkill[forestry.ResinExtractor];
            if (playerAbility == null || playerAbility2 == null) return drops;

            if ((this.block.FirstCodePart(2) == "pine" && playerAbility.Value(0) * 0.01 > world.Rand.NextDouble()) ||
                (playerAbility.Value(0) * 0.01 * playerAbility2.Value(0) * 0.01 > world.Rand.NextDouble()))
            {
                ItemStack drop = new ItemStack(world.GetItem(new AssetLocation("game", "resin")), 1);
                if (drop != null) drops.Add(drop);
            }
            return drops;
        }
    }//!class XSkillsWoodBehavior

    public class XSkillsLeavesBehavior : BlockBehavior
    {
        protected Forestry forestry;
        protected float xp;

        public XSkillsLeavesBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.xp = properties["xp"].AsFloat(0.0f);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.forestry = XLeveling.Instance(api)?.GetSkill("forestry") as Forestry;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (this.forestry == null || byPlayer == null) return;

            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[this.forestry.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(this.xp);
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.forestry == null) return drops.ToArray();
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.forestry.Id];
            if (playerSkill == null) return drops.ToArray();

            handling = EnumHandling.PreventDefault;
            if (block.Drops.Length == 0) return drops.ToArray();

            PlayerAbility afforestation = playerSkill[forestry.AfforestationId];
            PlayerAbility moreLadders = playerSkill.PlayerAbilities[forestry.MoreLaddersId];

            foreach (BlockDropItemStack drop in block.Drops)
            {
                float dropMultipier = dropChanceMultiplier;
                if (drop.ResolvedItemstack == null) continue;
                if (drop.ResolvedItemstack.Collectible is BlockSapling || drop.ResolvedItemstack.Collectible is ItemTreeSeed)
                {
                    if (afforestation == null) continue;
                    dropMultipier += afforestation.FValue(0);
                }
                else if (drop.ResolvedItemstack.Collectible.Code.Path == "stick")
                {
                    if (moreLadders == null) continue;
                    dropMultipier += moreLadders.SkillDependentFValue(0);
                }
                ItemStack dropStack = drop.GetNextItemStack(dropMultipier);
                if (dropStack != null) drops.Add(dropStack);
            }
            return drops.ToArray();
        }
    }//!class XSkillsLeavesBehavior

    public class XSkillsCharcoalBehavior : DropBonusBehavior
    {
        public Forestry Forestry { get; set; }
        public override CollectingSkill Skill => Forestry;
        public override EnumTool? Tool => EnumTool.Shovel;
        public override PlayerAbility DropBonusAbility(PlayerSkill skill) => skill[Forestry.CharcoalBurnerId];

        public XSkillsCharcoalBehavior(Block block) : base(block)
        {}

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            this.Forestry = XLeveling.Instance(api)?.GetSkill("forestry") as Forestry;
        }

        public override float GetXP(IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            //don't give experience when block was broken
            return 0.0f;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            if (this.Skill == null || byPlayer == null) return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
            PlayerSkill playerSkill = byPlayer.Entity?.GetBehavior<PlayerSkillSet>()?.PlayerSkills?[this.Skill.Id];
            playerSkill?.AddExperience(this.xp);
            return base.GetDrops(world, pos, byPlayer, ref dropChanceMultiplier, ref handling);
        }
    }//!class XSkillsCharcoalBehavior

    //fixes OnLoaded method to also call OnLoaded methods from behaviors 
    //see: https://github.com/anegostudios/vssurvivalmod/blob/master/Block/BlockLayeredSlowDig.cs
    [HarmonyPatch(typeof(BlockLayeredSlowDig))]
    public class BlockLayeredSlowDigPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnLoaded")]
        public static void OnLoadedPostfix(BlockLayeredSlowDig __instance, ICoreAPI api)
        {
            foreach (CollectibleBehavior behavior in __instance.CollectibleBehaviors)
            {
                behavior.OnLoaded(api);
            }
        }
    }//!class BlockLayeredSlowDigPatch
}//!namespace XSkills