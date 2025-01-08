using System;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace XLib.XEffects
{
    /// <summary>
    /// Increases the temperature on every interval by a specific amount up to a specific temperature.
    /// </summary>
    /// <seealso cref="Effect"/>
    public class HeatedEffect : Effect
    {
        /// <summary>
        /// Gets or sets the heat gain.
        /// </summary>
        /// <value>
        /// The heat gain.
        /// </value>
        public float HeatGain { get => this.Intensity; set => this.Intensity = value; }

        /// <summary>
        /// Gets or sets the destination temperature.
        /// </summary>
        /// <value>
        /// The destination temperature.
        /// </value>
        public float DestinationTemperature { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatedEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public HeatedEffect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="HeatedEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="heatGain">The heat gain.</param>
        /// <param name="destTemp">The destination temperature.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public HeatedEffect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float heatGain = 0.0f, float destTemp = 37.0f) :
            base(effectType, duration, maxStacks, stacks, heatGain)
        {
           DestinationTemperature = destTemp;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            DestinationTemperature = (float)tree.GetDecimal("destTemp");
        }

        /// <summary>
        /// Converts to tree.
        /// </summary>
        /// <returns>
        /// The tree.
        /// </returns>
        public override ITreeAttribute ToTree()
        {
            ITreeAttribute result = base.ToTree();
            result.SetFloat("destTemp", DestinationTemperature);
            return result;
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            EntityBehaviorBodyTemperature tempBeh = Entity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (tempBeh == null) return;
            float temp = Math.Min(DestinationTemperature, tempBeh.CurBodyTemperature + HeatGain);
            if (temp > tempBeh.CurBodyTemperature)
            {
                tempBeh.CurBodyTemperature = Math.Min(DestinationTemperature, tempBeh.CurBodyTemperature + HeatGain);
            }
        }

        /// <summary>
        /// Gets the description.
        /// Provides some default values for string interpolation.
        /// {0}: intensity
        /// {1}: interval
        /// {2}: destination temperature
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            try
            {
                return string.Format(this.EffectType.Description ?? "", this.Intensity, this.Interval, this.DestinationTemperature);
            }
            catch (Exception)
            {
                return this.EffectType.Description;
            }
        }
    }//!class HeatedEffect
}//!namespace XLib.XEffects
