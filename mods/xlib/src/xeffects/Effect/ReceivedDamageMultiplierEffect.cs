using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XLib.XEffects
{
    /// <summary>
    /// A damage multiplier effect
    /// </summary>
    /// <seealso cref="Effect" />
    public class ReceivedDamageMultiplierEffect : Effect
    {
        /// <summary>
        /// Gets or sets the multiplier.
        /// Same as Intensity.
        /// </summary>
        /// <value>
        /// The multiplier.
        /// </value>
        public float Multiplier { get => this.Intensity; set => this.Intensity = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedDamageMultiplierEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public ReceivedDamageMultiplierEffect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReceivedDamageMultiplierEffect" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public ReceivedDamageMultiplierEffect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float multiplier = 1.0f) :
            base(effectType, duration, maxStacks, stacks, multiplier)
        {}

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            EntityBehaviorHealth health = this.Entity?.GetBehavior<EntityBehaviorHealth>();
            if (health == null) return;
            health.onDamaged += OnDamaged;
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
            EntityBehaviorHealth health = this.Entity?.GetBehavior<EntityBehaviorHealth>();
            if (health == null) return;
            health.onDamaged -= OnDamaged;
        }

        /// <summary>
        /// Returns the resulting intensity.
        /// This is Pow(this.Multiplier, this.Stacks).
        /// </summary>
        /// <returns></returns>
        public override float ResultingIntensity()
        {
            if (this.Stacks > 1) 
                return (float)(Math.Pow(this.Multiplier, this.Stacks));
            else 
                return this.Multiplier * this.Stacks;
        }

        /// <summary>
        /// Called when an entity gets damaged.
        /// </summary>
        /// <param name="damage">The damage.</param>
        /// <param name="dmgSource">The DMG source.</param>
        /// <returns>the new damage</returns>
        public float OnDamaged(float damage, DamageSource dmgSource)
        {
            return damage * ResultingIntensity();
        }


    }//!class DamageMultiplierEffect
}//!namespace XLib.XEffects
