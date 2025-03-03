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
