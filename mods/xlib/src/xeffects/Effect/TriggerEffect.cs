using System;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect that can trigger another effect when itself expires
    /// </summary>
    /// <seealso cref="Effect" />
    public class TriggerEffect : Effect
    {
        /// <summary>
        /// Gets or sets the name of the effect that is triggered by this effect.
        /// </summary>
        /// <value>
        /// The name of the effect.
        /// </value>
        public string EffectName { get; set; }

        /// <summary>
        /// Gets or sets the duration of the effect that is triggered by this effect.
        /// </summary>
        /// <value>
        /// The duration of the effect.
        /// </value>
        public float EffectDuration { get; set; }

        /// <summary>
        /// Gets or sets the intensity of the effect that is triggered by this effect.
        /// </summary>
        /// <value>
        /// The intensity of the effect.
        /// </value>
        public float EffectIntensity { get; set; }

        /// <summary>
        /// Gets or sets the chance to trigger the effect.
        /// </summary>
        /// <value>
        /// The chance to trigger the effect.
        /// </value>
        public float Chance { get => this.Intensity; set => this.Intensity = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriggerEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public TriggerEffect(EffectType effectType) : this(effectType, 1.0f, "")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="effectName">Name of the stat.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public TriggerEffect(EffectType effectType, float duration, string effectName, int maxStacks = 1, int stacks = 1, float intensity = 0.0f) :
        base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.EffectName = effectName ?? "";
            this.Chance = 1.0f;
            this.EffectIntensity = 1.0f;
            this.EffectDuration = 0.0f;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.EffectName = tree.GetString("effectname", this.EffectName);
            this.EffectIntensity = (float)tree.GetDecimal("effectintensity", this.EffectIntensity);
            this.EffectDuration = (float)tree.GetDecimal("effectduration", this.EffectDuration);
            this.Chance = (float)tree.GetDecimal("chance", this.Chance);
        }

        /// <summary>
        /// Converts to tree.
        /// </summary>
        /// <returns>
        /// The tree.
        /// </returns>
        public override ITreeAttribute ToTree()
        {
            TreeAttribute result = base.ToTree() as TreeAttribute;
            result.SetString("effectname", this.EffectName);
            result.SetFloat("effectintensity", this.EffectIntensity);
            result.SetFloat("effectduration", this.EffectDuration);
            result.SetFloat("chance", this.Chance);
            return result;
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            CheckTrigger();
        }

        /// <summary>
        /// Called when an effect expires.
        /// </summary>
        public override void OnExpires()
        {
            base.OnExpires();
            CheckTrigger();
        }

        /// <summary>
        /// Checks whether the effect should be triggered.
        /// </summary>
        public virtual void CheckTrigger()
        {
            if (this.Behavior == null) return;
            if (this.Entity.World.Rand.NextDouble() < this.Chance) return;

            Effect effect = this.EffectType.EffectsSystem.CreateEffect(this.EffectName);
            if (effect == null) return;

            effect.Duration = this.EffectDuration;
            effect.Update(this.EffectIntensity);

            //adding effects while the effect main loop is running is bad
            //so it will be delayed to add the effect outside of the loop
            this.Entity.Api.World.RegisterCallback((float dt) =>
            {
                this.Behavior.AddEffect(effect);
                this.Behavior.MarkDirty();
            }, 0);
        }

        /// <summary>
        /// Gets the description.
        /// Provides some default values for string interpolation.
        /// {0}: intensity
        /// {1}: interval
        /// {2}: interval
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            try
            {
                EffectType effect = this.EffectType.EffectsSystem.EffectType(this.EffectName);
                if (effect != null) 
                    return string.Format(this.EffectType.Description ?? "", this.ResultingIntensity(), effect.DisplayName, this.EffectDuration);
                else
                    return string.Format(this.EffectType.Description ?? "", this.ResultingIntensity(), this.EffectName, this.EffectDuration);
            }
            catch (Exception)
            {
                return this.EffectType.Description;
            }
        }
    }//!class TriggerEffect
}//!namespace XLib.XEffects