using HarmonyLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class Farming : XSkill
    {
        //ability ids
        public int GreenThumbId { get; private set; }
        public int DemetersBlessId { get; private set; }
        public int GathererId { get; private set; }
        public int OrchardistId { get; private set; }
        public int RepottingId { get; private set; }
        public int RecyclerId { get; private set; }
        public int CarefulHandsId { get; private set; }
        public int BrightHarvestsId { get; private set; }
        public int CultivatedSeedsId { get; private set; }
        public int BeekeeperId { get; private set; }
        public int ExtensiveFarmingId { get; private set; }
        public int CompostingId { get; private set; }
        public int CrossBreedingId { get; private set; }
        public int BeemasterId { get; private set; }

        public static float MultiBreakMultiplier { get; set; }

        private List<Item> seeds;

        public List<Item> Seeds
        {
            get
            {
                if (seeds == null)
                {
                    seeds = new List<Item>();
                    foreach(Item item in XLeveling.Api.World.Items)
                    {
                        if (item is ItemPlantableSeed seed) seeds.Add(seed);
                        else if (item.Attributes?["isCrop"]?.AsBool() ?? false) seeds.Add(item);
                    }
                }
                return seeds;
            }
        }

        public Farming(ICoreAPI api) : base("farming", "xskills:skill-farming", "xskills:group-collecting")
        {
            XLeveling.Instance(api).RegisterSkill(this);
            this.Config = new FarmingConfig();
            MultiBreakMultiplier = 1.0f;
            seeds = null;

            // more crops
            // 0: base value
            // 1: value per level
            // 2: max value
            GreenThumbId = this.AddAbility(new Ability(
                "greenthumb",
                "xskills:ability-greenthumb",
                "xskills:abilitydesc-greenthumb",
                1, 3, new int[] { 10, 2, 30, 20, 4, 60, 20, 4, 100 }));

            // more seeds
            // 0: base value
            // 1: value per level
            // 2: max value
            DemetersBlessId = this.AddAbility(new Ability(
                "demetersbless",
                "xskills:ability-demetersbless",
                "xskills:abilitydesc-demetersbless",
                1, 3, new int[] { 5, 1, 15, 10, 2, 30, 10, 2, 50 }));

            // more berries and mushrooms and 
            // 0: base value
            // 1: value per level
            // 2: max value
            GathererId = this.AddAbility(new Ability(
                "gatherer",
                "xskills:ability-gatherer",
                "xskills:abilitydesc-gatherer",
                1, 2, new int[] { 10, 2, 30, 20, 4, 60 }));

            // more berries and mushrooms and 
            // 0: base value
            // 1: value per level
            // 2: max value
            OrchardistId = this.AddAbility(new Ability(
                "orchardist",
                "xskills:ability-orchardist",
                "xskills:abilitydesc-orchardist",
                3, 2, new int[] { 10, 2, 30, 20, 4, 60 }));

            //harvesting non-matured crops drops always the seeds
            RepottingId = this.AddAbility(new Ability(
                "repotting",
                "xskills:ability-repotting",
                "xskills:abilitydesc-repotting", 3));

            if (!(api.ModLoader.IsModEnabled("farmlanddropssoil") ||
                 (api.ModLoader.IsModEnabled("xfarmlanddrops"))))
            {
                // breaking farmland drops soil
                // 0: max base fertility improvement
                RecyclerId = this.AddAbility(new Ability(
                    "recycler",
                    "xskills:ability-recycler",
                    "xskills:abilitydesc-recycler", 
                    3, 2, new int[] { 0, 65 }));
            }

            //harvest mushrooms and reeds with your bare hands
            CarefulHandsId = this.AddAbility(new Ability(
                "carefulhands",
                "xskills:ability-carefulhands",
                "xskills:abilitydesc-carefulhands", 3));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "farmer",
                "xskills:ability-farmer",
                "xskills:abilitydesc-farmer",
                5, 1, new int[] { 40 }));

            // increases MultiBreakQuantity of shears and Scythes
            // 0: value
            BrightHarvestsId = this.AddAbility(new Ability(
                "brightharvest",
                "xskills:ability-brightharvest",
                "xskills:abilitydesc-brightharvest",
                5, 2, new int[] { 40, 80 }));

            // chance to skip a growth stage, skips always in greenhouse
            // 0: base value
            // 1: value per level
            // 2: max value
            CultivatedSeedsId = this.AddAbility(new Ability(
                "cultivatedseeds",
                "xskills:ability-cultivatedseeds",
                "xskills:abilitydesc-cultivatedseeds",
                5, 2, new int[] { 10, 1, 30, 10, 2, 50 }));

            // more drops from skeps
            // 0: value
            BeekeeperId = this.AddAbility(new Ability(
                "beekeeper",
                "xskills:ability-beekeeper",
                "xskills:abilitydesc-beekeeper",
                5, 3, new int[] { 1, 2, 3 }));

            // can till land, irrigate and plant seeds in a greater area
            // 0: range
            ExtensiveFarmingId = this.AddAbility(new Ability(
                "extensivefarming",
                "xskills:ability-extensivefarming",
                "xskills:abilitydesc-extensivefarming",
                6, 2, new int[] { 2, 3 }));

            // farmland will receive some nutraitions back
            // 0: value for main nutraitions
            // 1: value for other nutraitions
            CompostingId = this.AddAbility(new Ability(
                "composting",
                "xskills:ability-composting",
                "xskills:abilitydesc-composting",
                7, 2, new int[] { 10, 4, 20, 8 }));

            // chance to get a random seed
            // 0: base chance
            CrossBreedingId = this.AddAbility(new Ability(
                "crossbreeding",
                "xskills:ability-crossbreeding",
                "xskills:abilitydesc-crossbreeding",
                8, 1, new int[] { 1 }));

            //breaking harvestable skeps will give you the skep back
            BeemasterId = this.AddAbility(new Ability(
                "beemaster",
                "xskills:ability-beemaster",
                "xskills:abilitydesc-beemaster", 10, 1));

            //behaviors
            api.RegisterBlockBehaviorClass("XSkillsGrass", typeof(XSkillsGrassBehavior));
            api.RegisterBlockBehaviorClass("XSkillsCrop", typeof(XSkillsCropBehavior));
            api.RegisterBlockBehaviorClass("XSkillsFarmland", typeof(XSkillsFarmlandBehavior));
            api.RegisterBlockBehaviorClass("XSkillsMushroom", typeof(XSkillsMushroomBehavior));
            api.RegisterBlockBehaviorClass("XSkillsSkep", typeof(XSkillsSkepBehavior));
            api.RegisterBlockBehaviorClass("XSkillsBerryBush", typeof(XSkillsBerryBushBehavior));

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 200;
            this.ExpMult = 100.0f;
            this.ExpEquationValue = 8.0f;
        }
    }//!class Farming

    [ProtoContract]
    public class FarmingConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                CultureInfo provider = new CultureInfo("en-US");

                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("treeHarvestExp", treeHarvestExp.ToString(provider));
                return result;
            }
            set
            {
                string str;
                NumberStyles styles = NumberStyles.Any;
                CultureInfo provider = new CultureInfo("en-US");

                value.TryGetValue("treeHarvestExp", out str);
                if (str != null) float.TryParse(str, styles, provider, out treeHarvestExp);
            }
        }

        [ProtoMember(1)]
        [DefaultValue(0.5f)]
        public float treeHarvestExp = 0.5f;
    }//!class MetalworkingConfig

    public class XSkillsGrassBehavior : BlockBehavior
    {
        private Farming farming;
        private float xp;

        public XSkillsGrassBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.xp = properties["xp"].AsFloat(0.0f);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            ItemStack[] result = new ItemStack[0];
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[farming.Id];
            if (playerSkill == null) return result;

            //experience
            playerSkill.AddExperience(this.xp);

            //gatherer
            PlayerAbility playerAbility = playerSkill[farming.GathererId];
            if (playerAbility != null) dropChanceMultiplier += playerAbility.SkillDependentFValue();
            return result;
        }
    }

    public class XSkillsBerryBushBehavior : BlockBehaviorHarvestable
    {
        private Farming farming;
        private float xp;

        public XSkillsBerryBushBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            this.xp = properties["xp"].AsFloat(0.0f);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            return true;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handled)
        {
            BlockBehaviorHarvestable harvestable = this.block.GetBehavior<BlockBehaviorHarvestable>();
            //float harvestTime = harvestable.properties["harvestTime"].AsFloat(0.6f);
            Type type = typeof(BlockBehaviorHarvestable);
            FieldInfo info = type.GetField("harvestTime", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            float harvestTime = info?.GetValue(harvestable) as float? ?? 1.0f;

            if (this.farming == null || secondsUsed < harvestTime - 0.05f || byPlayer == null) return;

            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[this.farming.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(this.xp);

            //gatherer
            if (world.Api.Side != EnumAppSide.Server) return;
            PlayerAbility playerAbility = playerSkill.PlayerAbilities[farming.GathererId];

            if (playerAbility.Tier > 0)
            {
                float dropMultipier = 0.01f * playerAbility.SkillDependentValue();
                BlockDropItemStack[] drops = harvestable.harvestedStacks;
                if (drops == null || drops.Length == 0)
                {
                    ItemStack stack = harvestable.harvestedStack?.GetNextItemStack(dropMultipier);
                    if (stack == null) return;

                    if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                    {
                        world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                }
                else 
                {
                    foreach (BlockDropItemStack drop in drops)
                    {
                        if (drop == null) continue;
                        ItemStack stack = drop.GetNextItemStack(dropMultipier);
                        if (stack == null) continue;

                        if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                        {
                            world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                        }
                    }
                }
            }
        }
    }//!class XSkillsBerryBushBehavior

    public class XSkillsCropBehavior : BlockBehavior
    {
        private Farming farming;
        private float xp;

        public XSkillsCropBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            if (properties.KeyExists("xp"))
                this.xp = properties["xp"].AsFloat(0.0f);
            else
                this.xp = -1.0f;
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming ??= XLeveling.Instance(api)?.GetSkill("farming") as Farming;
            if (this.xp < 0.0f)
            {
                int currentCropStage = (block as BlockCrop)?.CurrentCropStage ?? -1;
                if (this.block.Drops.Length > 1 && block.CropProps != null && currentCropStage >= 0)
                {
                    float monthsPerStep = block.CropProps.TotalGrowthMonths / block.CropProps.GrowthStages;
                    if (block.CropProps.HarvestGrowthStageLoss > 0)
                        this.xp = block.CropProps.HarvestGrowthStageLoss * monthsPerStep * 0.5f;
                    else 
                        this.xp = block.CropProps.TotalGrowthMonths * 0.5f;
                    float penalty = (float)Math.Pow(0.5f, block.CropProps.GrowthStages - currentCropStage);
                    this.xp = Math.Clamp(this.xp * penalty, 0.0f, 3.0f);
                }
                else this.xp = 0.0f;
            }
        }

        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack, ref EnumHandling handling)
        {
            return true;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.farming == null) drops.ToArray();
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if(playerSkill == null) return drops.ToArray();

            //experience
            playerSkill.AddExperience(this.xp);

            PlayerAbility abilitySeeds = playerSkill[farming.DemetersBlessId];
            PlayerAbility abilityDrops = playerSkill[farming.GreenThumbId];

            for (int index = 0; index < block.Drops.Length; index++)
            {
                //demeters bless
                if (this.xp > 0.0f && block.Drops[index].ResolvedItemstack.GetName().Contains("seeds"))
                {
                    if (abilitySeeds != null)
                    {
                        float dropMultiplier = abilitySeeds.SkillDependentFValue() /* + dropChanceMultiplier*/;
                        ItemStack drop = block.Drops[index].GetNextItemStack(dropMultiplier);
                        if (drop != null) drops.Add(drop);
                    }
                }
                //green thumb
                else if (abilityDrops != null)
                {
                    float dropMultiplier = abilityDrops.SkillDependentFValue() /* + dropChanceMultiplier*/;
                    ItemStack drop = block.Drops[index].GetNextItemStack(dropMultiplier);
                    if (drop != null) drops.Add(drop);
                }
            }

            if (this.xp == 0.0f && drops.Count == 0)
            {
                //repotting
                if (playerSkill[farming.RepottingId].Tier > 0)
                {
                    int first = block.Code.Path.IndexOf("-");
                    int last = block.Code.Path.LastIndexOf("-");
                    if (last != first)
                    {
                        AssetLocation assetLocation =
                            new AssetLocation(block.Code.Domain, "seeds-" +
                            block.Code.Path.Substring(first + 1, last - first - 1));
                        Item seed = world.GetItem(assetLocation);
                        if (seed != null)
                        {
                            handling = EnumHandling.PreventDefault;
                            drops.Add(new ItemStack(seed, 1));
                        }
                    }
                }
            }
            else
            {
                //composting
                BlockEntityFarmland farmland = world.BlockAccessor.GetBlockEntity(new BlockPos(pos.X, pos.Y - 1, pos.Z, pos.dimension)) as BlockEntityFarmland;
                PlayerAbility playerAbility = playerSkill[farming.CompostingId];
                BlockCropProperties cropProps = this.block.CropProps;
                if (farmland != null && playerAbility.Tier > 0 && cropProps != null)
                {

                    for (EnumSoilNutrient nutrient = EnumSoilNutrient.N; nutrient <= EnumSoilNutrient.K; nutrient++)
                    {
                        float value;
                        if (cropProps.RequiredNutrient == nutrient)
                        {
                            value = Math.Min(
                                playerAbility.Value(0) * 0.01f * cropProps.NutrientConsumption,
                                playerAbility.Value(0) * 0.01f * farmland.OriginalFertility[(int)nutrient]);
                        }
                        else
                        {
                            value = (int)Math.Min(
                                playerAbility.Value(1) * 0.01f * cropProps.NutrientConsumption,
                                playerAbility.Value(1) * 0.01f * farmland.OriginalFertility[(int)nutrient]);
                        }
                        farmland.Nutrients[(int)nutrient] =
                            Math.Max(
                            Math.Min(farmland.Nutrients[(int)nutrient] + value,
                                     farmland.OriginalFertility[(int)nutrient] * 1.1f),
                                     farmland.Nutrients[(int)nutrient]);
                    }
                    farmland.MarkDirty();
                }

                //cross breeding
                playerAbility = playerSkill[farming.CrossBreedingId];
                BlockCrop crop = this.block as BlockCrop;
                if (playerAbility?.Tier > 0 && crop?.CurrentCropStage >= crop?.CropProps.GrowthStages)
                {
                    float chance = playerAbility.FValue(0);
                    int otherCount = 0;
                    BlockCrop other;
                    other = world.BlockAccessor.GetBlock(new BlockPos(pos.X + 1, pos.Y, pos.Z, pos.dimension)) as BlockCrop;
                    if (other != null && other != this.block && other.CurrentCropStage >= other.CropProps.GrowthStages) { chance *= 1.20f; ++otherCount; }
                    other = world.BlockAccessor.GetBlock(new BlockPos(pos.X - 1, pos.Y, pos.Z, pos.dimension)) as BlockCrop;
                    if (other != null && other != this.block && other.CurrentCropStage >= other.CropProps.GrowthStages) { chance *= 1.20f; ++otherCount; }
                    other = world.BlockAccessor.GetBlock(new BlockPos(pos.X, pos.Y, pos.Z + 1, pos.dimension)) as BlockCrop;
                    if (other != null && other != this.block && other.CurrentCropStage >= other.CropProps.GrowthStages) { chance *= 1.20f; ++otherCount; }
                    other = world.BlockAccessor.GetBlock(new BlockPos(pos.X, pos.Y, pos.Z - 1, pos.dimension)) as BlockCrop;
                    if (other != null && other != this.block && other.CurrentCropStage >= other.CropProps.GrowthStages) { chance *= 1.20f; ++otherCount; }

                    if (otherCount > 0 && chance > world.Rand.NextDouble())
                    {
                        Item seed = farming.Seeds[world.Rand.Next(farming.Seeds.Count - 1)];
                        ItemStack drop = new ItemStack(seed, 1);
                        drops.Add(drop);
                    }
                }
            }
            return drops.ToArray();
        }
    }//!class XSkillsCropBehavior

    public class XSkillsFarmlandBehavior : BlockBehavior
    {
        private Farming farming;

        public XSkillsFarmlandBehavior(Block block) : base(block)
        { }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            //recycler
            if (this.farming == null) return new ItemStack[] { };
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            PlayerAbility playerAbility = playerSkill?[farming.RecyclerId];
            if (playerAbility == null) return new ItemStack[] { };
            if (playerAbility.Tier <= 0) return new ItemStack[] { };

            IFarmlandBlockEntity farmlandBlock = world.BlockAccessor.GetBlockEntity(pos) as IFarmlandBlockEntity;
            if (farmlandBlock == null) return new ItemStack[] { };

            float maxNutrients = Math.Max(playerAbility.Value(0), farmlandBlock.OriginalFertility.Min());
            float minNutrients = farmlandBlock.Nutrients.Min();
            minNutrients = Math.Min(minNutrients, maxNutrients);

            AssetLocation assetLocation = null;
            handling = EnumHandling.PreventDefault;

            IEnumerator<KeyValuePair<string, float>> pairs = BlockEntityFarmland.Fertilities.GetEnumerator();
            float blockNutrient = 0.0f;
            string blockName = null;
            while (pairs.MoveNext())
            {
                if (minNutrients >= pairs.Current.Value - 0.1f && blockNutrient < pairs.Current.Value)
                {
                    blockNutrient = pairs.Current.Value;
                    blockName = pairs.Current.Key;
                }
            }

            if (blockName != null) assetLocation = new AssetLocation("game", "soil-" + blockName + "-none");
            else assetLocation = new AssetLocation("game", "soil-verylow-none");

            Block block = world.GetBlock(assetLocation);
            if (block == null) return new ItemStack[] { };
            return new ItemStack[] { new ItemStack(block, 1) };
        }
    }//!class XSkillsFarmlandBehavior

    public class XSkillsMushroomBehavior : BlockBehavior
    {
        private Farming farming;
        private float xp;

        public XSkillsMushroomBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            this.xp = properties["xp"].AsFloat(0.0f);
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (this.farming == null)
            {
                //try to to get farming here again since the OnLoaded method is not called by mushrooms 
                this.farming = XLeveling.Instance(world.Api)?.GetSkill("farming") as Farming;
                if (this.farming == null) return;
            }

            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(this.xp);

            PlayerAbility playerAbility = playerSkill[farming.CarefulHandsId];
            BlockPlant plant = this.block as BlockPlant;
            if (plant == null || playerAbility == null) return;

            //careful hands
            if (plant.Code.Path.Contains("normal") && playerAbility.Tier > 0 && byPlayer.InventoryManager.ActiveTool != EnumTool.Knife)
            {
                handling = EnumHandling.PreventDefault;
                AssetLocation newBlockCode = plant.Code.CopyWithPath(plant.Code.Path.Replace("normal", "harvested"));
                Block harvestedBlock = world.GetBlock(newBlockCode);
                if (harvestedBlock != null) world.BlockAccessor.SetBlock(harvestedBlock.BlockId, pos);
                else world.BlockAccessor.SetBlock(0, pos);

                //gatherer
                playerAbility = playerSkill[farming.GathererId];
                if (playerAbility == null) return;
                float dropMultipier = 1.0f + 0.01f * playerAbility.SkillDependentValue();
                for (int index = 0; index < block.Drops.Length; index++)
                {
                    world.SpawnItemEntity(block.Drops[index].GetNextItemStack(dropMultipier), pos.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            List<ItemStack> drops = new List<ItemStack>();
            if (this.farming == null) return drops.ToArray();
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if (playerSkill == null) return drops.ToArray();

            if (block.Drops.Length == 0) return drops.ToArray();

            //gatherer
            PlayerAbility playerAbility = playerSkill[farming.GathererId];
            if (playerAbility == null) return drops.ToArray();
            handling = EnumHandling.PreventDefault;
            float dropMultipier = dropChanceMultiplier + playerAbility.SkillDependentFValue();

            for (int index = 0; index < block.Drops.Length; index++)
            {
                ItemStack drop = block.Drops[index].GetNextItemStack(dropMultipier);
                if (drop != null) drops.Add(drop);
            }
            return drops.ToArray();
        }
    }//!class XSkillsMushroomBehavior

    [HarmonyPatch(typeof(BlockSkep))]
    public class BlockSkepPatch
    {
        [HarmonyPatch("OnBlockInteractStart")]
        public static bool Prefix(BlockSkep __instance, ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            bool preventDefault = false;
            __result = false;

            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                if (behavior.OnBlockInteractStart(world, byPlayer, blockSel, ref handled)) __result = true;
                if (handled != EnumHandling.PassThrough) preventDefault = true;
                if (handled == EnumHandling.PreventSubsequent) break;
            }
            return !preventDefault;
        }
    }//!class BlockSkepPatch

    public class XSkillsSkepBehavior : BlockBehavior
    {
        private Farming farming;

        public float xp { get; protected set; }
        public float HarvestTime { get; set; }
        public BlockDropItemStack HarvestedStack { get; set; }

        public XSkillsSkepBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            this.xp = properties["xp"].AsFloat(0.0f);
            HarvestTime = properties["harvestTime"].AsFloat(1.0f);
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.farming = XLeveling.Instance(api)?.GetSkill("farming") as Farming;
            if (block.Drops.Length > 1) HarvestedStack = block.Drops[1];
            else HarvestedStack = null;
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            if (world.Api.Side != EnumAppSide.Client) return new WorldInteraction[] { };
            BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityBeehive;
            if (beh == null || !beh.Harvestable || farming == null) return new WorldInteraction[] { };

            PlayerAbility playerAbility = forPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[farming.Id]?[farming.BeemasterId];
            if (playerAbility.Tier < 1) return new WorldInteraction[] { };

            return new WorldInteraction[] { new WorldInteraction()
            {
                ActionLangCode = Lang.Get("xskills:beehive-harvest"),
                HotKeyCode = null,
                MouseButton = EnumMouseButton.Right,
                Itemstacks = null,
            }};
        }
        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBeehive;
            if (beh == null || beh.Harvestable) return "";

            if (farming == null) return "";
            PlayerAbility playerAbility = forPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[farming.Id]?[farming.SpecialisationID];
            if (playerAbility.Tier < 1) return "";

            double end = typeof(BlockEntityBeehive).GetField("harvestableAtTotalHours", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(beh) as double? ?? 0.0;
            end /= world.Calendar.HoursPerDay;
            double now = world.Calendar.TotalDays;

            float result = (float)(end - now);
            if (result < 0.0f) return "";
            return Lang.Get("xskills:harvestable-in-days", result);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (blockSel == null || byPlayer == null) return false;
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return false;

            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if (playerSkill == null) return false;

            PlayerAbility playerAbility = playerSkill[farming.BeemasterId];
            if (playerAbility == null || playerAbility.Tier <= 0) return false;
            
            BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBeehive;
            if (beh == null || !beh.Harvestable) return false;

            handling = EnumHandling.PreventDefault;
            world.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
            return true;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (blockSel == null)  return false;
            BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBeehive;
            if (beh == null || !beh.Harvestable) return false;

            handling = EnumHandling.PreventDefault;

            (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemAttack);
            if (world.Rand.NextDouble() < 0.1)
            {
                world.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
            }
            return world.Side == EnumAppSide.Client || secondsUsed < HarvestTime;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (blockSel == null || byPlayer == null) return;
            if (secondsUsed < HarvestTime - 0.05f || HarvestedStack == null || world.Side == EnumAppSide.Client) return;

            PlayerSkill playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if (playerSkill == null) return;

            PlayerAbility playerAbility = playerSkill[farming.BeemasterId];
            if (playerAbility == null || playerAbility.Tier <= 0) return;

            PlayerAbility playerAbility2 = playerSkill[farming.BeekeeperId];
            if (playerAbility2 == null) return;

            BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBeehive;
            if (beh == null || !beh.Harvestable) return;

            handling = EnumHandling.PreventDefault;
            ItemStack stack = HarvestedStack.GetNextItemStack(1.0f);
            stack.StackSize += playerAbility2.Value(0);

            //experience
            playerSkill.AddExperience(this.xp);

            if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                world.SpawnItemEntity(stack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            ITreeAttribute tree = new TreeAttribute();
            beh.ToTreeAttributes(tree);
            tree.SetInt("harvestable", 0);
            tree.SetDouble("harvestableAtTotalHours", world.Calendar.TotalHours + 24 / 2 * (3 + world.Rand.NextDouble() * 8));
            tree.SetInt("hiveHealth", (int)EnumHivePopSize.Poor);
            beh.FromTreeAttributes(tree, world);
            beh.MarkDirty();
            world.PlaySoundAt(new AssetLocation("game:sounds/block/plant"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer);
        }

        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref EnumHandling handling)
        {
            if (this.farming == null) return;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[this.farming.Id];
            if (playerSkill == null) return;

            BlockSkep skep = this.block as BlockSkep;
            if (skep == null) return;

            if (!skep.IsEmpty())
            {
                BlockEntityBeehive beh = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBeehive;
                if (beh == null || !beh.Harvestable) return;

                BlockReinforcement reinforcment = world.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(true).GetReinforcment(pos);
                if (reinforcment?.Strength > 0) return;

                //experience
                playerSkill.AddExperience(this.xp);

                //beekeeper
                PlayerAbility playerAbility = playerSkill[farming.BeekeeperId];
                if (playerAbility != null)
                {
                    for (int value = playerAbility.Value(0); value > 0; value--)
                    {
                        world.SpawnItemEntity(new ItemStack(this.block.Drops[0].ResolvedItemstack.Item), pos.ToVec3d().Add(0.5, 0.5, 0.5));
                    }
                }
            }
        }
    }//!class XSkillsSkepBehavior
}//!namespace XSkills
