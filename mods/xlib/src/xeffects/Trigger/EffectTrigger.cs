using System;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect trigger
    /// </summary>
    public class EffectTrigger
    {
        /// <summary>
        /// Returns the effect that this trigger should trigger.
        /// </summary>
        /// <value>
        /// To trigger.
        /// </value>
        public EffectType ToTrigger { get; protected set; }

        /// <summary>
        /// Gets or sets the base chance.
        /// </summary>
        /// <value>
        /// The chance.
        /// </value>
        public float Chance { get; set; }

        /// <summary>
        /// Gets or sets the base intensity.
        /// </summary>
        /// <value>
        /// The intensity.
        /// </value>
        public float MinIntensity { get; set; }

        /// <summary>
        /// Gets or sets the maximum intensity.
        /// </summary>
        /// <value>
        /// The intensity.
        /// </value>
        public float MaxIntensity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectTrigger" /> class.
        /// </summary>
        /// <param name="toTrigger">To trigger.</param>
        /// <exception cref="ArgumentNullException">Thrown if toTrigger is null.</exception>
        public EffectTrigger(EffectType toTrigger)
        {
            this.ToTrigger = toTrigger ?? throw new ArgumentNullException("A trigger effect must not be null!");
        }

        /// <summary>
        /// Creates an effect trigger from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public virtual void FromTree(ITreeAttribute tree)
        {
            Chance = (float)tree.GetDecimal("chance");
            double intensity = tree.GetDecimal("intensity", 0.0);
            MaxIntensity = (float)tree.GetDecimal("maxintensity", intensity);
            MinIntensity = (float)tree.GetDecimal("minintensity", intensity);
        }

        /// <summary>
        /// Scales the intensity between min and max intensity.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns name="scale">the Intensity scaled between min and max intensity</returns>
        public virtual float ScaledIntensity(float scale)
        {
            Math.Clamp(scale, 0.0f, 1.0f);
            return MinIntensity + ((MaxIntensity - MinIntensity) * scale);
        }
    }//!class EffectTrigger
}//!namespace XLib.XEffects
