using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect trigger that is triggered from incoming damage
    /// </summary>
    /// <seealso cref="EffectTrigger" />
    public class DamageTrigger : EffectTrigger
    {
        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        /// <value>
        /// The threshold.
        /// </value>
        public float Threshold { get; set; }

        /// <summary>
        /// Gets or sets the maximum threshold.
        /// </summary>
        /// <value>
        /// The maximum threshold.
        /// </value>
        public float MaxThreshold { get; set; }

        /// <summary>
        /// Gets or sets the type of the damage source.
        /// </summary>
        /// <value>
        /// The source.
        /// </value>
        public EnumDamageSource Source { get; set; }

        /// <summary>
        /// Gets or sets the damage factor.
        /// </summary>
        /// <value>
        /// The damage factor.
        /// </value>
        public float DamageWeight { get; set; }

        /// <summary>
        /// Gets or sets the damage intensity weight.
        /// </summary>
        /// <value>
        /// The damage intensity weight.
        /// </value>
        public float DamageIntensity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DamageTrigger" /> class.
        /// </summary>
        /// <param name="toTrigger">To trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown if toTrigger is null.</exception>
        public DamageTrigger(EffectType toTrigger) : base(toTrigger)
        {
            this.Threshold = 0.0f;
            this.MaxThreshold = 999999.0f;
            this.Source = EnumDamageSource.Unknown;
            this.DamageWeight = 1.0f;
        }

        /// <summary>
        /// Creates a damage trigger from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.Threshold = (float)tree.GetDecimal("threshold", this.Threshold);
            this.MaxThreshold = (float)tree.GetDecimal("maxthreshold", this.MaxThreshold);
            this.DamageWeight = (float)tree.GetDecimal("damageweight", this.DamageWeight);
            this.DamageIntensity = (float)tree.GetDecimal("damageintensity", this.DamageIntensity);

            switch (tree.GetString("source"))
            {
                case "block": Source = EnumDamageSource.Block; break;
                case "player": Source = EnumDamageSource.Player; break;
                case "fall": Source = EnumDamageSource.Fall; break;
                case "drown": Source = EnumDamageSource.Drown; break;
                case "revive": Source = EnumDamageSource.Revive; break;
                case "void": Source = EnumDamageSource.Void; break;
                case "suicide": Source = EnumDamageSource.Suicide; break;
                case "internal": Source = EnumDamageSource.Internal; break;
                case "entity": Source = EnumDamageSource.Entity; break;
                case "explosion": Source = EnumDamageSource.Explosion; break;
                case "machine": Source = EnumDamageSource.Machine; break;
                case "unknown": Source = EnumDamageSource.Unknown; break;
                case "weather": Source = EnumDamageSource.Weather; break;
                default:
                    Source = EnumDamageSource.Unknown; break;
            }
        }

        /// <summary>
        /// Gets the chance.
        /// </summary>
        /// <param name="damageSource">The damage source.</param>
        /// <param name="destination">The destination.</param>
        /// <param name="damage">The damage.</param>
        /// <returns></returns>
        public virtual double GetChance(DamageSource damageSource, Entity destination, float damage)
        {
            if (damageSource.Source != Source || damage < Threshold || damage >= MaxThreshold || destination == null) return 0.0f;
            return (Chance + DamageWeight * (damage - Threshold));
        }

        /// <summary>
        /// Checks whether this trigger should be triggered.
        /// </summary>
        /// <param name="damageSource">The damage source.</param>
        /// <param name="destination">The destination entity.</param>
        /// <param name="damage">The damage.</param>
        /// <returns>
        /// true if the effect should trigger; otherwise, false
        /// </returns>
        public virtual bool ShouldTrigger(DamageSource damageSource, Entity destination, float damage)
        {
            if (destination == null) return false;
            bool result = GetChance(damageSource, destination, damage) >= destination.World.Rand.NextDouble();
            if (damageSource.Type != EnumDamageType.Heal && result)
            {
                EntityBehaviorHealth health = destination.GetBehavior<EntityBehaviorHealth>();
                if (health.Health <= damage) return false;
            }
            return result;
        }
    }//!class DamageTrigger
}//!namespace XLib.XEffects
