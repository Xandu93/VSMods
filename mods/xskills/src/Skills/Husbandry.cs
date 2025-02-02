using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class Husbandry : XSkill
    {
        public int HunterId { get; private set; }
        public int ButcherId { get; private set; }
        public int FurrierId { get; private set; }
        public int BoneBrakerId { get; private set; }
        public int RancherId { get; private set; }
        public int FeederId { get; private set; }
        public int LightFootedId { get; private set; }
        public int PreserverId { get; private set; }
        public int TannerId { get; private set; }
        public int CheesyCheeseId { get; private set; }
        public int CatcherId { get; private set; }
        public int BreederId { get; private set; }
        public int MassHusbandryId { get; private set; }

        public Husbandry(ICoreAPI api) : base("husbandry", "xskills:skill-husbandry", "xskills:group-collecting")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);

            // increases damage against wild animals
            // 0: base value
            // 1: value per level
            // 2: max value
            HunterId = this.AddAbility(new TraitAbility(
                "hunter", "bowyer",
                "xskills:ability-hunter",
                "xskills:abilitydesc-hunter", 
                1, 3, new int[] { 10, 0, 10, 10, 1, 20, 10, 1, 30 }));

            // more mob drops(flesh)
            // 0: base value
            // 1: value per level
            // 2: max value
            // 3: value per generation
            // 4: max generation
            ButcherId = this.AddAbility(new Ability(
                "butcher",
                "xskills:ability-butcher",
                "xskills:abilitydesc-butcher",
                1, 3, new int[] { 5, 1, 15, 1, 10, 5, 2, 25, 1, 15, 5, 2, 45, 1, 20}));

            // more mob drops(hides)
            // 0: base value
            // 1: value per level
            // 2: max value
            // 3: value per generation
            // 4: max generation
            FurrierId = this.AddAbility(new Ability(
                "furrier",
                "xskills:ability-furrier",
                "xskills:abilitydesc-furrier",
                1, 3, new int[] { 5, 1, 15, 1, 10, 5, 2, 25, 1, 15, 5, 2, 45, 1, 20 }));

            // can harvest more bones from carcasses
            // 0: base value
            // 1: value per level
            // 2: max value
            BoneBrakerId = this.AddAbility(new Ability(
                "bonebreaker",
                "xskills:ability-bonebreaker",
                "xskills:abilitydesc-bonebreaker",
                1, 3, new int[] { 10, 1, 20, 15, 2, 40, 20, 2, 60 }));

            // more eggs and milk
            // 0: base value
            // 1: milking generation boost chance
            // 2: max generation
            RancherId = this.AddAbility(new Ability(
                "rancher",
                "xskills:ability-rancher",
                "xskills:abilitydesc-rancher",
                3, 2, new int[] { 33, 2, 5, 50, 3, 10 }));

            // animals need less food + generation increase from food
            // 0: chance base value
            // 1: chance value per level
            // 2: max value
            // 3: generation boost chance
            // 4: max generation
            FeederId = this.AddAbility(new Ability(
                "feeder",
                "xskills:ability-feeder",
                "xskills:abilitydesc-feeder",
                3, 2, new int[] { 10, 1, 20, 1, 4, 20, 1, 40, 1, 8}));

            // reduced animal seaking range
            // 0: base value
            LightFootedId = this.AddAbility(new StatAbility(
                "lightfooted", "animalSeekingRange",
                "xskills:ability-lightfooted",
                "xskills:abilitydesc-lightfooted",
                3, 2, new int[] { -20, -40 }));

            // profession
            // 0: ep bonus
            SpecialisationID = this.AddAbility(new Ability(
                "shepherd",
                "xskills:ability-shepherd",
                "xskills:abilitydesc-shepherd",
                5, 1, new int[] { 40 }));

            // makes meat preserve longer
            // 0: base value
            // 1: value per level
            // 2: max value
            PreserverId = this.AddAbility(new Ability(
                "preserver",
                "xskills:ability-preserver",
                "xskills:abilitydesc-preserver",
                5, 1, new int[] { 10, 1, 30 }));

            // can tan hide with less recources
            // 0: base value
            // 1: value per level
            // 2: max value
            TannerId = this.AddAbility(new Ability(
                "tanner",
                "xskills:ability-tanner",
                "xskills:abilitydesc-tanner",
                5, 3, new int[] { 10, 1, 20, 10, 2, 30, 10, 2, 50 }));

            // chance to get cheese from milking
            // 0: base value
            // 1: chance per day
            CheesyCheeseId = this.AddAbility(new Ability(
                "cheesycheese",
                "xskills:ability-cheesycheese",
                "xskills:abilitydesc-cheesycheese",
                5, 2, new int[] { 5, 1, 5, 2 }));

            // can catch small animals
            CatcherId = this.AddAbility(new TraitAbility(
                "catcher", "ability-catcher",
                "xskills:ability-catcher",
                "xskills:abilitydesc-catcher",
                6, 1));

            // reduces the pregnancy time of animals
            // 0: base value
            // 1: value per skill level
            // 2: value per generation
            // 3: max value
            BreederId = this.AddAbility(new Ability(
                "breeder",
                "xskills:ability-breeder",
                "xskills:abilitydesc-breeder",
                8, 1, new int[] { 10, 2, 1, 60 }));

            // increased offstrings from animals
            // 0: base value
            // 1: value per skill level
            // 2: value per generation
            // 3: max value
            MassHusbandryId = this.AddAbility(new Ability(
                "masshusbandry",
                "xskills:ability-masshusbandry",
                "xskills:abilitydesc-masshusbandry",
                10, 1, new int[] { 0, 1, 1, 30 }));

            //behaviors
            api.RegisterEntityBehaviorClass("XSkillsAnimal", typeof(XSkillsAnimalBehavior));
            api.RegisterBlockBehaviorClass("XSkillsCarcass", typeof(XSkillsCarcassBehavior));

            api.RegisterBlockClass("XSkillsCage", typeof(BlockCage));
            api.RegisterBlockEntityClass("XSkillsBECage", typeof(BlockEntityCage));

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 100;
            this.ExpMult = 50.0f;
            this.ExpEquationValue = 4.0f;
        }
    }//!class Husbandry

    [HarmonyPatch(typeof(BehaviorCollectFrom))]
    public class BehaviorCollectFromPatch
    {
        [HarmonyPatch("OnBlockInteractStart")]
        public static void Prefix(BehaviorCollectFrom __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return;
            if (__instance.block.Code.Path.Contains("empty")) return;

            if (__instance.block.Drops != null && __instance.block.Drops.Length > 1)
            {
                Husbandry husbandry = XLeveling.Instance(world.Api)?.GetSkill("husbandry") as Husbandry;
                if (husbandry == null) return;
                PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
                PlayerAbility playerAbility = playerSkill?[husbandry.RancherId];
                if (playerAbility == null) return;
                BlockDropItemStack drop = __instance.block.Drops[0];

                //experience
                playerSkill.AddExperience(0.1f * __instance.block.Drops[0].Quantity.avg);

                if (playerAbility.Tier < 1) return;
                ItemStack stack = drop.GetNextItemStack(playerAbility.FValue(0));
                if ((stack?.StackSize ?? 0) < 1) return;

                if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    world.SpawnItemEntity(drop.GetNextItemStack(), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }
    }//!class BehaviorCollectFromPatch

    [HarmonyPatch(typeof(EntityBehaviorMilkable))]
    public class EntityBehaviorMilkablePatch
    {
        [HarmonyPatch("MilkingComplete")]
        public static bool Prefix(EntityBehaviorMilkable __instance, out bool __state, EntityAgent byEntity)
        {
            __state = true;
            if (__instance.entity.World.Side == EnumAppSide.Client) return __state;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return __state;
            PlayerSkill playerSkill = byEntity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return __state;
            PlayerAbility playerAbility = playerSkill[husbandry.CheesyCheeseId];
            if (playerAbility == null) return __state;

            //experience
            playerSkill.AddExperience(0.5f);

            float prob = (float)(
                playerAbility.FValue(0) + 
                playerAbility.FValue(1) * (
                __instance.entity.World.Calendar.TotalHours -
                __instance.entity.WatchedAttributes.GetFloat("lastMilkedTotalHours")) / 
                __instance.entity.World.Calendar.HoursPerDay);

            if (prob > __instance.entity.World.Rand.NextDouble())
            {
                typeof(EntityBehaviorMilkable).GetField( "lastMilkedTotalHours", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(__instance, __instance.entity.World.Calendar.TotalHours);
                __instance.entity.WatchedAttributes.SetFloat("lastMilkedTotalHours", (float)__instance.entity.World.Calendar.TotalHours);
                ItemStack cheese;
                if (0.2f < __instance.entity.World.Rand.NextDouble())
                {

                    cheese = new ItemStack(byEntity.World.GetItem(new AssetLocation("game", "cheese-cheddar-4slice")));
                }
                else
                {
                    cheese = new ItemStack(byEntity.World.GetItem(new AssetLocation("game", "cheese-blue-4slice")));
                }
                if (!byEntity.TryGiveItemStack(cheese))
                {
                    byEntity.World.SpawnItemEntity(cheese, byEntity.Pos.XYZ.Add(0, 0.5, 0));
                }
                __state = false;
                return __state;
            }
            return __state;
        }

        [HarmonyPatch("MilkingComplete")]
        public static void Postfix(EntityBehaviorMilkable __instance, bool __state, ItemSlot slot, EntityAgent byEntity)
        {
            if (!__state) return;
            if (__instance.entity.World.Side == EnumAppSide.Server)
            {
                Husbandry husbandry = XLeveling.Instance(__instance.entity.Api).GetSkill("husbandry") as Husbandry;
                if (husbandry == null) return;
                PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[husbandry.Id]?[husbandry.RancherId];
                if (playerAbility == null) return;

                if (__instance.entity.World.Rand.NextDouble() < playerAbility.FValue(1))
                {
                    int generation = __instance.entity.WatchedAttributes.GetInt("generation") + 1;
                    if (generation < playerAbility.Value(2))
                    {
                        __instance.entity.WatchedAttributes.SetInt("generation", generation);
                        __instance.entity.WatchedAttributes.MarkPathDirty("generation");
                    }
                }

                ItemStack contentStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("milkportion")));

                float stackSize = (1000 * playerAbility.FValue(0));
                contentStack.StackSize = (int)stackSize + ((stackSize - (int)stackSize) > byEntity.World.Rand.NextDouble() ? 1 : 0);
                if (contentStack.StackSize < 1) return;

                if(!TryFillLiquidContainer(slot, contentStack, byEntity))
                {
                    IInventory inventory = (byEntity as EntityPlayer)?.Player?.InventoryManager?.GetHotbarInventory();
                    if (inventory == null) return;
                    foreach(ItemSlot slot2 in inventory)
                    {
                        if (TryFillLiquidContainer(slot2, contentStack, byEntity)) return;
                    }
                    inventory = (byEntity as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory(GlobalConstants.backpackInvClassName);
                    if (inventory == null) return;
                    foreach (ItemSlot slot2 in inventory)
                    {
                        if (TryFillLiquidContainer(slot2, contentStack, byEntity)) return;
                    }
                }
            }
        }

        public static bool TryFillLiquidContainer(ItemSlot slot, ItemStack contentStack, EntityAgent byEntity)
        {
            BlockLiquidContainerBase lcblock = slot.Itemstack?.Collectible as BlockLiquidContainerBase;
            if (lcblock == null) return false;
            if (slot.Itemstack.StackSize == 1)
            {
                contentStack.StackSize -= lcblock.TryPutLiquid(slot.Itemstack, contentStack, 10);
                slot.MarkDirty();
                if (contentStack.StackSize <= 0) return true;
            }
            else
            {
                ItemStack containerStack = slot.TakeOut(1);
                slot.MarkDirty();
                contentStack.StackSize -= lcblock.TryPutLiquid(containerStack, contentStack, 10);

                if (!byEntity.TryGiveItemStack(containerStack))
                {
                    byEntity.World.SpawnItemEntity(containerStack, byEntity.Pos.XYZ.Add(0, 0.5, 0));
                }
                if (contentStack.StackSize <= 0) return true;
            }
            return false;
        }

    }//!class EntityBehaviorMilkablePatch

    public class XSkillsCarcassBehavior : BlockBehavior
    {
        private Husbandry husbandry;

        public XSkillsCarcassBehavior(Block block) : base(block)
        { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
        }

        public override void OnLoaded(ICoreAPI api)
        {
            this.husbandry = XLeveling.Instance(api)?.GetSkill("husbandry") as Husbandry;
        }
        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, ref float dropChanceMultiplier, ref EnumHandling handling)
        {
            if (this.husbandry == null) return null;

            PlayerAbility playerAbility = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id]?[husbandry.BoneBrakerId];
            if (playerAbility == null || playerAbility.Tier <= 0) return null;

            dropChanceMultiplier += playerAbility.SkillDependentFValue();

            return null;
        }
    }//!class XSkillsCarcassBehavior
}//!namespace XSkills
