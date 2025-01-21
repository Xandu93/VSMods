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
    public class DotEffect : Effect
    {
        /// <summary>
        /// Gets or sets the damage.
        /// </summary>
        /// <value>
        /// The damage.
        /// </value>
        public float Damage { get => this.Intensity; set => this.Intensity = value; }

        /// <summary>
        /// Gets the damage source.
        /// </summary>
        /// <value>
        /// The damage source.
        /// </value>
        public DamageSource DamageSource { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public DotEffect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DotEffect" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="damage">The damage.</param>
        /// <param name="damageSource">The damage source.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public DotEffect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float damage = 0.0f, DamageSource damageSource = null) :
            base(effectType, duration, maxStacks, stacks, damage)
        {
            this.DamageSource = damageSource ?? new DamageSource();
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.DamageSource.Source = (EnumDamageSource)tree.GetInt("DamageSource", (int)EnumDamageSource.Internal);
            this.DamageSource.DamageTier = tree.GetInt("DamageTier", 0);
            this.DamageSource.Type = (EnumDamageType)tree.GetInt("DamageType", (int)EnumDamageType.Injury);
        }

        /// <summary>
        /// Converts to an attribute tree.
        /// </summary>
        /// <returns>The tree.</returns>
        public override ITreeAttribute ToTree()
        {
            ITreeAttribute result = base.ToTree();
            result.SetInt("DamageSource", (int)this.DamageSource.Source);
            result.SetInt("DamageTier", this.DamageSource.DamageTier);
            result.SetInt("DamageType", (int)this.DamageSource.Type);
            return result;
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            EntityBehaviorHealth health = this.Entity?.GetBehavior<EntityBehaviorHealth>();
            if (health == null) return;
            float damage = this.Damage * this.Stacks;
            health.OnEntityReceiveDamage(DamageSource, ref damage);
        }
    }//!class EffectDot
}//!namespace XLib.XEffects
