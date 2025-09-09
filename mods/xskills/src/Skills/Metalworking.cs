using HarmonyLib;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using XLib.XLeveling;

namespace XSkills
{
    public class Metalworking : XSkill
    {
        //ability ids
        public int SmelterId { get; private set; }
        public int MetalRecoveryId { get; private set; }
        public int HeatingHitsId { get; private set; }
        public int HammerExpertId { get; private set; }
        public int HeavyHitsId { get; private set; }
        public int BlacksmithId { get; private set; }
        public int FinishingTouchId { get; private set; }
        public int DuplicatorId { get; private set; }
        public int SalvagerId { get; private set; }
        public int MasterSmithId { get; private set; }
        public int SenseOfTime { get; private set; }
        public int MachineLearningId { get; private set; }
        public int BloomeryExpertId { get; private set; }
        public int AutomatedSmithingId { get; private set; }

        private List<SmithingRecipe> duplicatable;

        public Metalworking(ICoreAPI api) : base("metalworking", "xskills:skill-metalworking", "xskills:group-processing")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);
            this.Config = new MetalworkingConfig();

            // less metal to fill a mold
            // 0: value
            SmelterId = this.AddAbility(new Ability(
                "smelter",
                "xskills:ability-smelter",
                "xskills:abilitydesc-smelter",
                1, 3, new int[] { 10, 20, 25 }));

            MetalRecoveryId = -1;
            if (!api.ModLoader.IsModEnabled("metalrecovery"))
            {
                // receive metal bit from splits
                // 0: splits needed to receive a metal bit
                MetalRecoveryId = this.AddAbility(new Ability(
                    "metalrecovery",
                    "xskills:ability-metalrecovery",
                    "xskills:abilitydesc-metalrecovery",
                    1, 3, new int[] { 5, 4, 3 }));
            }

            // increase temperature on hits
            // 0: value
            HeatingHitsId = this.AddAbility(new Ability(
                "heatinghits",
                "xskills:ability-heatinghits",
                "xskills:abilitydesc-heatinghits",
                1, 2, new int[] { 1, 2}));

            // receive durability back
            // 0: base value
            // 1: value per level
            // 2: max value
            HammerExpertId = this.AddAbility(new Ability(
                "hammerexpert",
                "xskills:ability-hammerexpert",
                "xskills:abilitydesc-hammerexpert",
                1, 3, new int[] { 5, 1, 15, 5, 2, 25, 5, 2, 45 }));

            // heavy hits on slag works as a split
            HeavyHitsId = this.AddAbility(new Ability(
                "heavyhits",
                "xskills:ability-heavyhits",
                "xskills:abilitydesc-heavyhits",
                3, 1));

            // enables quality for forged items
            // 0: base value
            // 1: max value
            BlacksmithId = this.AddAbility(new Ability(
                "blacksmith",
                "xskills:ability-blacksmith",
                "xskills:abilitydesc-blacksmith",
                3, 2, new int[] { 1, 5, 2, 10 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "metalworker",
                "xskills:ability-metalworker",
                "xskills:abilitydesc-metalworker",
                5, 1, new int[] { 40 }));

            // chance to instantly finish a smithing work
            // 0: base value
            // 1: value per level
            // 2: max value
            FinishingTouchId = this.AddAbility(new Ability(
                "finishingtouch",
                "xskills:ability-finishingtouch",
                "xskills:abilitydesc-finishingtouch",
                5, 3, new int[] { 1, 1, 2, 2, 2, 4, 2, 2, 6 }));

            // chance to duplicate an item
            // 0: base value
            // 1: value per level
            // 2: max value
            DuplicatorId = this.AddAbility(new Ability(
                "duplicator",
                "xskills:ability-duplicator",
                "xskills:abilitydesc-duplicator",
                5, 3, new int[] { 5, 0, 5, 5, 1, 15, 5, 1, 25 }));

            // you can disassemblable locust
            // 0: base value
            // 1: value per level
            // 2: max value
            SalvagerId = this.AddAbility(new Ability(
                "salvager",
                "xskills:ability-salvager",
                "xskills:abilitydesc-salvager",
                5, 2, new int[] { 95, 1, 110, 160, 2, 200 }));

            // heavy hits move voxels to appropriate positions
            // 0: voxel count
            // 1: range
            MasterSmithId = this.AddAbility(new Ability(
                "mastersmith",
                "xskills:ability-mastersmith",
                "xskills:abilitydesc-mastersmith",
                7, 2, new int[] { 2, 3, 4, 4 }));

            // sends a message to the player when smelting finished
            SenseOfTime = this.AddAbility(new Ability(
                "senseoftime",
                "xskills:ability-senseoftime",
                "xskills:abilitydesc-senseoftime",
                8));

            // helve hammers can profit from some abilities
            MachineLearningId = this.AddAbility(new Ability(
                "machinelearning",
                "xskills:ability-machinelearning",
                "xskills:abilitydesc-machinelearning",
                8, 1, new int[] {}));

            // can take items out of bloomeries
            BloomeryExpertId = this.AddAbility(new Ability(
                "bloomeryexpert",
                "xskills:ability-bloomeryexpert",
                "xskills:abilitydesc-bloomeryexpert",
                8, 1, new int[] { }));

            // helve hammers can smith advanced recipes
            AutomatedSmithingId = this.AddAbility(new Ability(
                "automatedsmithing",
                "xskills:ability-automatedsmithing",
                "xskills:abilitydesc-automatedsmithing",
                10, 1, new int[] { 1 }));

            //behaviors
            api.RegisterEntityBehaviorClass("disassemblable", typeof(EntityBehaviorDisassemblable));
            api.RegisterBlockBehaviorClass("XSkillsBloomery", typeof(XSkillsBloomeryBehavior));

            ICoreServerAPI sapi = api as ICoreServerAPI;
            ICoreClientAPI capi = api as ICoreClientAPI;

            if (sapi != null) sapi.Event.PlayerJoin += (IServerPlayer byPlayer) => UpdateBits();

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 40;
            this.ExpMult = 10.0f;
            this.ExpEquationValue = 0.8f;
        }

        public void RegisterAnvil()
        {
            ClassRegistry registry = (this.XLeveling.Api as ServerCoreAPI)?.ClassRegistryNative ?? (this.XLeveling.Api as ClientCoreAPI)?.ClassRegistryNative;
            if (registry != null)
            {
                registry.ItemClassToTypeMapping["ItemHammer"] = typeof(ItemHammerPatch);
            }
        }

        public override void OnConfigReceived()
        {
            base.OnConfigReceived();
            UpdateBits();
        }

        public void UpdateBits()
        {
            if (this.duplicatable != null) return;
            foreach (Item item in this.XLeveling.Api.World.Items)
            {
                if (item.FirstCodePart(0) == "metalbit")
                {
                    if (item.Code?.Domain == "xskills")
                    {
                        if (this.XLeveling.Api is ICoreServerAPI && (Config as MetalworkingConfig).useVanillaBits)
                        {
                            GridRecipe recipe = new GridRecipe();
                            recipe.IngredientPattern = "B";
                            recipe.Height = 1;
                            recipe.Width = 1;
                            recipe.Shapeless = true;

                            CraftingRecipeIngredient ingredient = new CraftingRecipeIngredient();
                            ingredient.Code = new AssetLocation("xskills", item.Code.Path);
                            ingredient.Type = EnumItemClass.Item;
                            ingredient.Quantity = 1;

                            CraftingRecipeIngredient output = new CraftingRecipeIngredient();
                            output.Code = new AssetLocation("game", item.Code.Path);
                            output.Type = EnumItemClass.Item;
                            output.Quantity = 1;
                            Item resolved = XLeveling.Api.World.GetItem(output.Code);
                            if (resolved == null || resolved.IsMissing) { continue; }

                            recipe.Ingredients = new Dictionary<string, CraftingRecipeIngredient>();
                            recipe.Ingredients.Add("B", ingredient);
                            recipe.RecipeGroup = 6;
                            recipe.Output = output;
                            recipe.Name = new AssetLocation("game", "recipes/grid/metalbit.json");

                            if (recipe.ResolveIngredients(this.XLeveling.Api.World))
                            {
                                (this.XLeveling.Api as ICoreServerAPI)?.RegisterCraftingRecipe(recipe);
                            }
                        }

                        Item bit = this.XLeveling.Api.World.GetItem(new AssetLocation("game", item.Code.Path));
                        if (bit != null)
                        {
                            item.MaterialDensity = bit.MaterialDensity;
                            if (bit.CombustibleProps != null)
                            {
                                item.CombustibleProps = bit.CombustibleProps.Clone();
                                item.CombustibleProps.SmeltedRatio = (this.Config as MetalworkingConfig).bitsForIngot;
                            }
                        }
                    }
                }
            }

            float recipeRatio = (Config as MetalworkingConfig)?.chiselRecipesRatio ?? 1.0f;
            if (recipeRatio != 1.0f)
            {
                foreach (GridRecipe recipe in this.XLeveling.Api.World.GridRecipes)
                {
                    if (recipe.Name.Path.Contains("metalbit") || 
                        recipe.Name.Path.Contains("ingot") ||
                        recipe.Name.Path.Contains("recycling"))
                    {
                        if (recipe.resolvedIngredients.Length != 2) continue;
                        CraftingRecipeIngredient ingredient = null;
                        bool foundTool = false;

                        foreach (GridRecipeIngredient tempIngredient in recipe.resolvedIngredients)
                        {
                            if (tempIngredient == null) continue;
                            if (tempIngredient.IsTool) foundTool = true;
                            else ingredient = tempIngredient;
                        }

                        if ((!(ingredient?.Code?.Path.Contains("ingot") ?? true)) && foundTool)
                        {
                            recipe.Output.ResolvedItemstack.StackSize = (int)(recipe.Output.ResolvedItemstack.StackSize * recipeRatio);
                            recipe.Output.Quantity = recipe.Output.ResolvedItemstack.StackSize;
                            if (recipe.Output.ResolvedItemstack.StackSize == 0)
                                recipe.Enabled = false;
                        }
                    }
                }

                //foreach (string asset in (Config as MetalworkingConfig)?.reduceSmeltingResult)
                //{
                //    CollectibleObject collectible = this.XLeveling.Api.World.GetBlock(asset);
                //    collectible ??= this.XLeveling.Api.World.GetItem(asset);
                //    if (collectible == null) continue;
                //    collectible.CombustibleProps.SmeltedRatio = (int)(collectible.CombustibleProps.SmeltedRatio * recipeRatio);
                //}
            }

            this.duplicatable = new List<SmithingRecipe>();
            foreach (SmithingRecipe recipe in this.XLeveling.Api.ModLoader.GetModSystem<RecipeRegistrySystem>().SmithingRecipes)
            {
                CollectibleObject collectible = recipe.Output?.ResolvedItemstack?.Collectible;
                if (collectible == null) continue;

                int neededVoxels = 0;
                if (collectible.CombustibleProps?.SmeltedStack?.ResolvedItemstack  != null)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        for (int y = 0; y < recipe.QuantityLayers; y++)
                        {
                            for (int z = 0; z < 16; z++)
                            {
                                if (recipe.Voxels[x, y, z])
                                {
                                    neededVoxels++;
                                }
                            }
                        }
                    }
                    Ability ability = this[this.DuplicatorId];
                    float proportion = 1.0f + ability.Values[ability.Values.Length - 1] * 0.01f;

                    if (recipe.Output.ResolvedItemstack.StackSize == 0) continue;
                    int voxelsPerItem = neededVoxels / recipe.Output.ResolvedItemstack.StackSize;
                    int returnedIngots = voxelsPerItem / 42;

                    if (returnedIngots == 0)
                    {
                        if (voxelsPerItem == 0) continue;
                        returnedIngots = 1;
                        int neededItems = (int)(42 / voxelsPerItem * proportion + 1.0f);
                        recipe.Output.ResolvedItemstack.Collectible.CombustibleProps.SmeltedRatio = 
                            Math.Max(neededItems, recipe.Output.ResolvedItemstack.Collectible.CombustibleProps.SmeltedRatio);
                        neededVoxels = voxelsPerItem * neededItems;
                    }

                    recipe.Output.ResolvedItemstack.Collectible.CombustibleProps.SmeltedStack.ResolvedItemstack.StackSize = returnedIngots;
                    recipe.Output.ResolvedItemstack.Collectible.CombustibleProps.SmeltedStack.StackSize = returnedIngots;
                    if ((float)neededVoxels / (returnedIngots * 42) > proportion)
                        this.duplicatable.Add(recipe);
                }
            }
        }

        internal bool IsDuplicatable(SmithingRecipe recipe)
        {
            return this.duplicatable.Contains(recipe);
        }

    }//!class Smithing

    [ProtoContract]
    public  class MetalworkingConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                CultureInfo provider = new CultureInfo("en-US");

                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("expBase", expBase.ToString(provider));
                result.Add("expPerHit", expPerHit.ToString(provider));
                result.Add("helveHammerPenalty", helveHammerPenalty.ToString(provider));
                result.Add("useVanillaBits", useVanillaBits.ToString(provider));
                result.Add("bitsForIngot", bitsForIngot.ToString(provider));
                result.Add("chiselRecipesRatio", chiselRecipesRatio.ToString(provider));
                result.Add("allowFinishingTouchExploit", allowFinishingTouchExploit.ToString(provider));
                result.Add("qualitySteps", qualitySteps.ToString(provider));
                return result;
            }
            set
            {
                string str;
                NumberStyles styles = NumberStyles.Any;
                CultureInfo provider = new CultureInfo("en-US");

                value.TryGetValue("expBase", out str);
                if (str != null) float.TryParse(str, styles, provider, out expBase);
                value.TryGetValue("expPerHit", out str);
                if (str != null) float.TryParse(str, styles, provider, out expPerHit);
                value.TryGetValue("helveHammerPenalty", out str);
                if (str != null) float.TryParse(str, styles, provider, out helveHammerPenalty);
                value.TryGetValue("useVanillaBits", out str);
                if (str != null) bool.TryParse(str, out useVanillaBits);
                value.TryGetValue("bitsForIngot", out str);
                if (str != null) int.TryParse(str, styles, provider, out bitsForIngot);
                value.TryGetValue("chiselRecipesRatio", out str);
                if (str != null) float.TryParse(str, styles, provider, out chiselRecipesRatio);
                value.TryGetValue("allowFinishingTouchExploit", out str);
                if (str != null) bool.TryParse(str, out allowFinishingTouchExploit);
                value.TryGetValue("qualitySteps", out str);
                if (str != null) float.TryParse(str, out qualitySteps);

                if (expPerHit > 1.0f) expPerHit *= 0.01f;
                if (helveHammerPenalty > 1.0f) helveHammerPenalty *= 0.01f;
                if (chiselRecipesRatio > 1.0f) chiselRecipesRatio *= 0.01f;
            }
        }

        [ProtoMember(1)]
        [DefaultValue(1.0f)]
        public float expBase = 1.0f;

        [ProtoMember(2)]
        [DefaultValue(0.02f)]
        public float expPerHit = 0.02f;

        [ProtoMember(3)]
        [DefaultValue(0.75f)]
        public float helveHammerPenalty = 0.75f;

        [ProtoMember(4)]
        [DefaultValue(true)]
        public bool useVanillaBits = true;

        [ProtoMember(5)]
        [DefaultValue(21)]
        public int bitsForIngot = 21;

        [ProtoMember(6)]
        [DefaultValue(0.5f)]
        public float chiselRecipesRatio = 0.5f;

        [ProtoMember(7)]
        [DefaultValue(0.5f)]
        public bool adjustRecipes = false;

        [ProtoMember(8)]
        [DefaultValue(false)]
        public bool allowFinishingTouchExploit = false;

        [ProtoMember(9)]
        [DefaultValue(0.0f)]
        public float qualitySteps = 0.0f;
    }//!class MetalworkingConfig

    [HarmonyPatch(typeof(BlockSmeltedContainer))]
    class BlockSmeltedContainerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStep")]
        public static void OnHeldInteractStepPrefix(EntityAgent byEntity, BlockSelection blockSel, ref int __state)
        {
            if (byEntity == null || blockSel == null) return;

            BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            BlockEntityToolMold beTool = blockEntity as BlockEntityToolMold;
            BlockEntityIngotMold beIngot = blockEntity as BlockEntityIngotMold;

            if (beTool != null) __state = beTool.FillLevel;
            else if (beIngot != null) __state = beIngot.FillLevelLeft + beIngot.FillLevelRight;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnHeldInteractStep")]
        public static void OnHeldInteractStepPostfix(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, int __state)
        {
            if (slot == null || byEntity == null || blockSel == null) return;
            BlockEntity blockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position);
            BlockEntityToolMold beTool = blockEntity as BlockEntityToolMold;
            BlockEntityIngotMold beIngot = blockEntity as BlockEntityIngotMold;

            int filled = 0;
            if (beTool != null) filled = beTool.FillLevel - __state;
            else if (beIngot != null) filled = beIngot.FillLevelLeft + beIngot.FillLevelRight - __state;

            Metalworking metalworking = XLeveling.Instance(byEntity.Api)?.GetSkill("metalworking") as Metalworking;
            PlayerSkill playerSkill = byEntity.GetBehavior<PlayerSkillSet>()?.PlayerSkills[metalworking.Id];
            if (metalworking != null && playerSkill != null && filled > 0)
            {
                //experience
                if (beIngot != null)
                {
                    playerSkill.AddExperience(filled * 0.0075f * 0.25f);
                    return;
                }
                playerSkill.AddExperience(filled * 0.0075f);

                //smelter
                PlayerAbility playerAbility = playerSkill.PlayerAbilities[metalworking.SmelterId];
                int aValue = playerAbility.Value(0);

                //return if one product is meltable
                ItemStack[] moldedStacks = beTool.GetMoldedStacks(beTool.MetalContent);
                foreach(ItemStack itemStack in moldedStacks)
                {
                    if (itemStack.Collectible.CombustibleProps != null) return;
                }
                if (aValue <= 0) return;

                int bonusFill = 100 / aValue;
                if (bonusFill > 1)
                {
                    int bonus = filled / (bonusFill - 1);
                    beTool.FillLevel += bonus;
                    __state += bonus * bonusFill;

                    if ((beTool.FillLevel + 1) / bonusFill > (__state + 1) / bonusFill)
                    {
                        beTool.FillLevel++;
                    }
                    if (beTool.IsFull)
                    {
                        int requiredUnits = beTool.Block.Attributes["requiredUnits"].AsInt(100);
                        slot.Itemstack.Attributes.SetInt("units", slot.Itemstack.Attributes.GetInt("units") + beTool.FillLevel % requiredUnits);
                        beTool.FillLevel -= beTool.FillLevel % requiredUnits;
                    }
                    slot.MarkDirty();
                    beTool.MarkDirty();
                }
            }
        }
    }//!class BlockSmeltedContainerPatch

    [HarmonyPatch(typeof(BlockBloomery))]
    public class BlockBloomeryPatch
    {
        [HarmonyPatch("OnBlockInteractStart")]
        public static bool Prefix(BlockSkep __instance, ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EnumHandling handled = EnumHandling.PassThrough;
            bool preventDefault = false;

            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                if (behavior.OnBlockInteractStart(world, byPlayer, blockSel, ref handled)) __result = true;
                if (handled != EnumHandling.PassThrough) preventDefault = true;
                if (handled == EnumHandling.PreventSubsequent) break;
            }
            return !preventDefault;
        }
    }//!class BlockBloomeryPatch

    public class XSkillsBloomeryBehavior : BlockBehavior
    {
        public XSkillsBloomeryBehavior(Block block) : base(block)
        { }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            if (world.Api.Side != EnumAppSide.Client) return new WorldInteraction[] { };
            BlockEntityBloomery beb = world.BlockAccessor.GetBlockEntity(selection.Position) as BlockEntityBloomery;
            if (beb == null || beb.IsBurning) return new WorldInteraction[] { };
            ItemSlot slot = typeof(BlockEntityBloomery).GetProperty("OutSlot", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(beb) as ItemSlot;
            if (slot == null || slot.Itemstack == null) return new WorldInteraction[] { };

            Metalworking metalworking = XLeveling.Instance(world.Api)?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return new WorldInteraction[] { };
            PlayerAbility playerAbility = forPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[metalworking.Id]?[metalworking.BloomeryExpertId];
            if (playerAbility.Tier < 1) return new WorldInteraction[] { };

            return new WorldInteraction[] { new WorldInteraction()
            {
                ActionLangCode = Lang.Get("xskills:blockhelp-bloomery-takeresult", slot.Itemstack.GetName()),
                HotKeyCode = null,
                MouseButton = EnumMouseButton.Right,
                Itemstacks = null,
            }};
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            BlockEntityBloomery beb = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityBloomery;
            if (beb == null || !beb.IsBurning) return "";

            Metalworking metalworking = XLeveling.Instance(world.Api)?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return "";
            PlayerAbility playerAbility = forPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[metalworking.Id]?[metalworking.SpecialisationID];
            if (playerAbility.Tier < 1) return "";

            double start = typeof(BlockEntityBloomery).GetField("burningUntilTotalDays", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(beb) as double? ?? 0.0;
            double end = typeof(BlockEntityBloomery).GetField("burningStartTotalDays", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(beb) as double? ?? 0.0;
            double now = world.Calendar.TotalDays;

            float result = (float) Math.Min(1.0 - (now - start) / (end - start), 1.0);
            return Lang.Get("xskills:progress", result);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            if (blockSel == null || byPlayer == null) return false;
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return false;

            Metalworking metalworking = XLeveling.Instance(world.Api)?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return false;
            PlayerAbility playerAbility = byPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[metalworking.Id]?[metalworking.BloomeryExpertId];
            if (playerAbility.Tier < 1) return false;

            BlockEntityBloomery beb = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBloomery;
            if (beb == null || beb.IsBurning) return false;
            ItemSlot slot = typeof(BlockEntityBloomery).GetProperty("OutSlot", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(beb) as ItemSlot;
            if (slot == null || slot.Itemstack == null) return false;

            if (!byPlayer.InventoryManager.TryGiveItemstack(slot.Itemstack))
            {
                world.SpawnItemEntity(slot.Itemstack, blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }
            handling = EnumHandling.PreventDefault;
            slot.Itemstack = null;
            return true;
        }
    }//!class XSkillsBloomeryBehavior
}//!namespace XSkills
