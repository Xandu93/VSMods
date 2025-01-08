using System;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect trigger that is triggered from entity damage
    /// </summary>
    /// <seealso cref="DamageTrigger" />
    public class EntityTrigger : DamageTrigger
    {
        /// <summary>
        /// Gets or sets whether projectiles can trigger the effect.
        /// </summary>
        /// <value>
        /// Whether projectiles can trigger the effect.
        /// </value>
        bool AllowProjectiles { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityTrigger" /> class.
        /// </summary>
        /// <param name="toTrigger">To trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown if toTrigger is null.</exception>
        public EntityTrigger(EffectType toTrigger) : base(toTrigger)
        {
            AllowProjectiles = false;
        }

        /// <summary>
        /// Creates an entity trigger from an attribute tree.
        /// </summary>
        /// <param name="tree"></param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            AllowProjectiles = tree.GetBool("allowprojectiles", false);
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
        public override bool ShouldTrigger(DamageSource damageSource, Entity destination, float damage)
        {
            if (damageSource.CauseEntity != null && !AllowProjectiles) return false;
            return base.ShouldTrigger(damageSource, destination, damage);
        }

    }//!class EntityTrigger
}//!namespace XLib.XEffects
