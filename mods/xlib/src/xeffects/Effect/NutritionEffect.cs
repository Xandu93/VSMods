using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents a damage over time effect.
    /// </summary>
    /// <seealso cref="Effect" />
    public class NutritionEffect : Effect
    {
        /// <summary>
        /// Gets the food category.
        /// </summary>
        /// <value>
        /// The food category.
        /// </value>
        public EnumFoodCategory FoodCategory { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public NutritionEffect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotEffect" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="FoodCategory">the food category</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public NutritionEffect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float damage = 0.0f, EnumFoodCategory FoodCategory = EnumFoodCategory.Unknown) :
            base(effectType, duration, maxStacks, stacks, damage)
        {
            this.FoodCategory = FoodCategory;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.FoodCategory = (EnumFoodCategory)tree.GetInt("foodcategory", (int)EnumFoodCategory.NoNutrition);
            if (this.FoodCategory == EnumFoodCategory.NoNutrition)
            {
                string str = tree.GetString("foodcategory");
                if (Enum.TryParse(str, out EnumFoodCategory FoodCategory))
                {
                    this.FoodCategory = FoodCategory;
                }
                else
                {
                    this.FoodCategory = EnumFoodCategory.Unknown;
                }
            }
        }

        /// <summary>
        /// Converts to an attribute tree.
        /// </summary>
        /// <returns>The tree.</returns>
        public override ITreeAttribute ToTree()
        {
            ITreeAttribute result = base.ToTree();
            result.SetInt("foodcategory", (int)this.FoodCategory);
            return result;
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            EntityBehaviorHunger hunger = this.Entity?.GetBehavior<EntityBehaviorHunger>();
            if (hunger == null) return;

            switch(this.FoodCategory)
            {
                case EnumFoodCategory.Fruit:
                    hunger.FruitLevel += Intensity * this.Stacks;
                    break;
                case EnumFoodCategory.Vegetable:
                    hunger.VegetableLevel += Intensity * this.Stacks;
                    break;
                case EnumFoodCategory.Protein:
                    hunger.ProteinLevel += Intensity * this.Stacks;
                    break;
                case EnumFoodCategory.Grain:
                    hunger.GrainLevel += Intensity * this.Stacks;
                    break;
                case EnumFoodCategory.Dairy:
                    hunger.DairyLevel += Intensity * this.Stacks;
                    break;
                default:
                    hunger.OnEntityReceiveSaturation(Intensity * this.Stacks);
                    break;
            }
        }
    }//!class EffectDot
}//!namespace XLib.XEffects
