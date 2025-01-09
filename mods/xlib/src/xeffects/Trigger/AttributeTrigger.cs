using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents a single attribute for an AttributeTrigger.
    /// </summary>
    public class AttributeTriggerAttribute
    {
        /// <summary>
        /// Gets or sets the minimum.
        /// </summary>
        /// <value>
        /// The minimum.
        /// </value>
        public float Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum.
        /// </summary>
        /// <value>
        /// The maximum.
        /// </value>
        public float Max { get; set; }

        /// <summary>
        /// Gets or sets the weight.
        /// </summary>
        /// <value>
        /// The weight.
        /// </value>
        public float Weight { get; set; }

        /// <summary>
        /// Gets or sets the stat.
        /// </summary>
        /// <value>
        /// The stat.
        /// </value>
        public string Stat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the calculated chance shold be inverted.
        /// </summary>
        /// <value>
        ///   <c>true</c> if invert; otherwise, <c>false</c>.
        /// </value>
        public bool Invert { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerAttribute(float min, float max, float weight, string stat, bool invert = false)
        {
            Min = min;
            Max = max;
            Weight = weight;
            Stat = stat;
            Invert = invert;
        }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public virtual double Chance(SyncedTreeAttribute watchedAttributes)
        {
            return ScaledValue(watchedAttributes.GetDecimal(Stat, 0.0f));
        }

        /// <summary>
        /// Scales a value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        protected virtual double ScaledValue(double value)
        {
            double result;
            if (value >= Max)
            {
                result = 1.0f;
            }
            else if (value <= Min)
            {
                result = 0.0f;
            }
            else
            {
                result = (value - Min) / (Max - Min);
            }

            if (Invert) return (1.0 - result) * Weight;
            return result * Weight;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerBodyTemp : AttributeTriggerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerBodyTemp(float min, float max, float weight, string stat, bool invert = false)
            : base(min, max, weight, stat, invert)
        { }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            return ScaledValue(watchedAttributes.GetTreeAttribute("bodyTemp")?.TryGetFloat("bodytemp") ?? 0.0);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerAttributeAttribute : AttributeTriggerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerAttributeAttribute(float min, float max, float weight, string stat, bool invert = false)
            : base(min, max, weight, stat, invert)
        { }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            return ScaledValue(watchedAttributes.GetTreeAttribute(Stat)?.TryGetFloat(Stat) ?? 0.0);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerHealth : AttributeTriggerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerHealth(float min, float max, float weight, string stat, bool invert = false)
            : base(min, max, weight, stat, invert)
        { }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            ITreeAttribute healthTree = watchedAttributes.GetTreeAttribute(Stat);
            if (healthTree == null) return 0.0;
            double value = healthTree.GetFloat("current" + Stat, 0.0f) / healthTree.GetFloat("max" + Stat, 1.0f);
            return ScaledValue(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerHunger : AttributeTriggerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerHunger(float min, float max, float weight, string stat, bool invert = false)
            : base(min, max, weight, stat, invert)
        { }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            ITreeAttribute hungerTree = watchedAttributes.GetTreeAttribute(Stat);
            if (hungerTree == null) return 0.0;
            double value = hungerTree.GetFloat("currentsaturation", 0.0f) / hungerTree.GetFloat("maxsaturation", 1.0f);
            return ScaledValue(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerNutrition : AttributeTriggerAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerNutrition(float min, float max, float weight, string stat, bool invert = false)
            : base(min, max, weight, stat, invert)
        { }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            ITreeAttribute hungerTree = watchedAttributes.GetTreeAttribute(Stat);
            if (hungerTree == null) return 0.0;
            float maxSaturation = hungerTree.GetFloat("maxsaturation", 1.0f);
            double value = (
                    hungerTree.GetFloat("fruitLevel") / maxSaturation +
                    hungerTree.GetFloat("vegetableLevel") / maxSaturation +
                    hungerTree.GetFloat("grainLevel") / maxSaturation +
                    hungerTree.GetFloat("proteinLevel") / maxSaturation +
                    hungerTree.GetFloat("dairyLevel") / maxSaturation) * 0.2;
            return ScaledValue(value);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="AttributeTriggerAttribute" />
    public class AttributeTriggerFoodLevel : AttributeTriggerAttribute
    {
        /// <summary>
        /// Gets or sets the food.
        /// </summary>
        /// <value>
        /// The food.
        /// </value>
        public string Food { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeTriggerAttribute" /> class.
        /// </summary>
        /// <param name="min">The minimum.</param>
        /// <param name="max">The maximum.</param>
        /// <param name="weight">The weight.</param>
        /// <param name="stat">The stat.</param>
        /// <param name="food">The food.</param>
        /// <param name="invert">if set to <c>true</c> [invert].</param>
        public AttributeTriggerFoodLevel(float min, float max, float weight, string stat, string food, bool invert = false)
            : base(min, max, weight, stat, invert)
        {
            Food = food;
        }

        /// <summary>
        /// Calculates the chance.
        /// </summary>
        /// <returns></returns>
        public override double Chance(SyncedTreeAttribute watchedAttributes)
        {
            ITreeAttribute hungerTree = watchedAttributes.GetTreeAttribute("hunger");
            if (hungerTree == null) return 0.0;
            double value = hungerTree.GetFloat(Food, 0.0f) / hungerTree.GetFloat("maxsaturation", 1500.0f);
            return ScaledValue(value);
        }
    }

    /// <summary>
    /// Represents an effect trigger that is triggered from attribute values
    /// </summary>
    /// <seealso cref="EffectTrigger" />
    public class AttributeTrigger : EffectTrigger
    {
        /// <summary>
        /// If the trigger chance exceeds this threshold 
        /// it will block the recovery of the effect.
        /// </summary>
        public float RecoveryThreshold { get; set; }

        /// <summary>
        /// The triggers
        /// </summary>
        protected List<AttributeTriggerAttribute> weights;

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageTrigger" /> class.
        /// </summary>
        /// <param name="toTrigger">To trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown if toTrigger is null.</exception>
        public AttributeTrigger(EffectType toTrigger) : base(toTrigger)
        {
            weights = new List<AttributeTriggerAttribute>();
        }

        /// <summary>
        /// Creates a damage trigger from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            RecoveryThreshold = (float)tree.GetDecimal("recoverythreshold");
            ArrayAttribute<TreeAttribute> weightsTree = (tree as TreeAttribute)?.GetAttribute("weights") as ArrayAttribute<TreeAttribute>;
            if (weightsTree == null) return;
            foreach(TreeAttribute attribute in weightsTree.value)
            {
                string attributeName = attribute.GetString("attribute");

                float min = (float)attribute.GetDecimal("min");
                float max = (float)attribute.GetDecimal("max");
                float weight = (float)attribute.GetDecimal("weight");
                bool inverted = attribute.GetBool("inverted");

                switch (attributeName)
                {
                    case "bodyTemp":
                        if (min < max)
                        {
                            this.weights.Add(new AttributeTriggerBodyTemp(min, max, weight, attributeName, true));
                            break;
                        }
                        else
                        {
                            this.weights.Add(new AttributeTriggerBodyTemp(max, min, weight, attributeName, false));
                            break;
                        }
                    case "tiredness": 
                        this.weights.Add(new AttributeTriggerAttributeAttribute(min, max, weight, attributeName, false)); 
                        break;
                    case "wetness":
                        this.weights.Add(new AttributeTriggerAttribute(min, max, weight, attributeName, false)); 
                        break;
                    case "temporalStability":
                        this.weights.Add(new AttributeTriggerAttribute(min, max, weight, attributeName, true)); 
                        break;
                    case "health":
                        this.weights.Add(new AttributeTriggerHealth(min, max, weight, attributeName, true));
                        break;
                    case "hunger":
                        this.weights.Add(new AttributeTriggerHunger(min, max, weight, attributeName, true));
                        break;
                    case "nutrition":
                        this.weights.Add(new AttributeTriggerNutrition(min, max, weight, attributeName, true));
                        break;
                    case "foodLevel":
                        this.weights.Add(new AttributeTriggerFoodLevel(min, max, weight, attributeName, attribute.GetString("food", ""), true));
                        break;
                    default:
                        this.weights.Add(new AttributeTriggerAttribute(min, max, weight, attributeName, inverted)); 
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the chance.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="chanceMultiplier">The chance multiplier.</param>
        /// <param name="power">The power used to calculate the chance.
        /// Can be used to simulate multiple chance calculations at once.</param>
        /// <returns></returns>
        public virtual double GetChance(Entity destination, float chanceMultiplier, float power = 1.0f)
        {
            if (destination == null) return 0.0f;

            double chance = 0.0;
            float totalWeights = 0.0f;
            foreach (AttributeTriggerAttribute weight in weights)
            {
                totalWeights += weight.Weight;
                chance += weight.Chance(destination.WatchedAttributes);
            }
            chance = (chance / totalWeights) * chanceMultiplier;
            if (chance >= 1.0f) return 1.0f;
            else if (chance <= 0.0f) return 0.0f;
            else if (power != 1.0f) return 1.0f - Math.Pow(1.0f - chance, power);
            return chance;
        }

        /// <summary>
        /// Checks whether this trigger should be triggered.
        /// </summary>
        /// <param name="destination">The destination.</param>
        /// <param name="chanceMultiplier">The chance multiplier.</param>
        /// <param name="power">The power used to calculate the chance.
        /// Can be used to simulate multiple chance calculations at once.</param>
        /// <returns></returns>
        public virtual bool ShouldTrigger(Entity destination, float chanceMultiplier = 1.0f, float power = 1.0f)
        {
            if (destination == null) return false;
            return GetChance(destination, chanceMultiplier) >= destination.World.Rand.NextDouble();
        }
    }//!class AttributeTrigger
}//!namespace XLib.XEffects
