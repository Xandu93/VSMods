using System;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// The attribute effect Modifikation defines how a attribute effect modifies an attribute.
    /// By default the attribute will be set once.
    /// </summary>
    [Flags]
    public enum EnumAttributeEffectMod : int
    {
        /// <summary>
        /// Sets the attribute at each interval.
        /// </summary>
        Repeat = 0x00001,

        /// <summary>
        /// Adds the intensity to the attribute value.
        /// </summary>
        Add = 0x00002,

        /// <summary>
        /// When the effect expires the attribute will be set to 0.
        /// </summary>
        RemoveWhenExpires = 0x00004,
    }

    /// <summary>
    /// Represents an effect that affects an entities attribute
    /// </summary>
    /// <seealso cref="Effect" />
    public class AttributeEffect : Effect
    {
        /// <summary>
        /// Gets or sets the name of the attribute that is affected by this effect.
        /// </summary>
        /// <value>
        /// The name of the attribute.
        /// </value>
        public string AttributeName { get; protected set; }

        /// <summary>
        /// Gets or sets how an attribute effect modifies an attribute.
        /// </summary>
        /// <value>
        /// Defines how an attribute effect modifies an attribute.
        /// </value>
        public EnumAttributeEffectMod Modifikation { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum of the attribute that is affected by this effect.
        /// </summary>
        /// <value>
        /// The minimum of the attribute.
        /// </value>
        public float AttributeMinimum { get; protected set; }

        /// <summary>
        /// Gets or sets the maximum of the attribute that is affected by this effect.
        /// </summary>
        /// <value>
        /// The maximum of the attribute.
        /// </value>
        public float AttributeMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the effect repeats at every interval.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the effect repeats at every interval; otherwise, <c>false</c>.
        /// </value>
        public bool Repeats
        {
            get => (Modifikation & EnumAttributeEffectMod.Repeat) > 0;
            set => Modifikation = value ? Modifikation | EnumAttributeEffectMod.Repeat : Modifikation & ~EnumAttributeEffectMod.Repeat;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the effect will set the attribute to 0 when the effect expires.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the effect will set the attribute to 0 when the effect expires; otherwise, <c>false</c>.
        /// </value>
        public bool RemoveWhenExpires
        {
            get => (Modifikation & EnumAttributeEffectMod.RemoveWhenExpires) > 0;
            set => Modifikation = value ? Modifikation | EnumAttributeEffectMod.RemoveWhenExpires : Modifikation & ~EnumAttributeEffectMod.RemoveWhenExpires;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the effect will add a value to an existing attribute.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the effect will add a value to an existing attribute; otherwise, <c>false</c>.
        /// </value>
        public bool Adds
        {
            get => (Modifikation & EnumAttributeEffectMod.Add) > 0;
            set => Modifikation = value ? Modifikation | EnumAttributeEffectMod.Add : Modifikation & ~EnumAttributeEffectMod.Add;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AttributeEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public AttributeEffect(EffectType effectType) : this(effectType, 1.0f, "")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public AttributeEffect(EffectType effectType, float duration, string attributeName, int maxStacks = 1, int stacks = 1, float intensity = 0.0f) :
        base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.AttributeName = attributeName ?? "";
            this.Modifikation = 0;
            this.AttributeMinimum = 0.0f;
            this.AttributeMaximum = 1.0f;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            string attribute = tree.GetString("attributename", this.AttributeName);
            bool restart = false;
            if (Running && attribute != this.AttributeName)
            {
                OnEnd();
                restart = true;
            }
            base.FromTree(tree);
            this.AttributeName = attribute;
            this.Modifikation = (EnumAttributeEffectMod)tree.GetInt("modifikation", (int)this.Modifikation);
            this.Repeats = tree.GetBool("repeats", this.Repeats);
            this.RemoveWhenExpires = tree.GetBool("removewhenexpires", this.Repeats);
            this.AttributeMinimum = (float)tree.GetDecimal("attributemin", this.AttributeMinimum);
            this.AttributeMaximum = (float)tree.GetDecimal("attributemax", this.AttributeMaximum);
            if (restart) OnStart();
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
            result.SetString("attributename", this.AttributeName);
            result.SetInt("modifikation", (int)this.Modifikation);
            result.SetFloat("attributemin", this.AttributeMinimum);
            result.SetFloat("attributemax", this.AttributeMaximum);
            return result;
        }

        /// <summary>
        /// Calculates a resulting value.
        /// Takes min and max values into account if the add modifier is set.
        /// </summary>
        /// <param name="value">The original value.</param>
        /// <param name="mult">Just to sign the resulting intensity.</param>
        /// <returns>the resulting value</returns>
        protected virtual float ResultingValue(float? value, float mult)
        {
            if (!this.Adds) return ResultingIntensity();

            float result;
            if (value == null) result = ResultingIntensity();
            else result = value.Value + ResultingIntensity() * mult;
            result = Math.Clamp(result, this.AttributeMinimum, this.AttributeMaximum);
            return result;
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            SyncedTreeAttribute attributes = Entity.WatchedAttributes;
            if (attributes == null) return;
            float? value = attributes.TryGetFloat(this.AttributeName);
            attributes.SetFloat(this.AttributeName, ResultingValue(value, 1.0f));
            attributes.MarkPathDirty(this.AttributeName);
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            if (!this.RemoveWhenExpires) return;
            SyncedTreeAttribute attributes = Entity.WatchedAttributes;
            if (attributes == null) return;
            float? value = attributes.TryGetFloat(this.AttributeName);
            attributes.SetFloat(this.AttributeName, ResultingValue(value, -1.0f));
            attributes.MarkPathDirty(this.AttributeName);
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            if (!this.Repeats) return;
            SyncedTreeAttribute attributes = Entity.WatchedAttributes;
            if (attributes == null) return;
            float? value = attributes.TryGetFloat(this.AttributeName);
            attributes.SetFloat(this.AttributeName, ResultingValue(value, 1.0f));
            attributes.MarkPathDirty(this.AttributeName);
        }

        /// <summary>
        /// Updates the values of the effect.
        /// Some effects require a special handling when these values change.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        public override void Update(float intensity, int stacks = 0)
        {
            float value0 = 0.0f;
            if (this.RemoveWhenExpires)
            {
                value0 -= this.ResultingIntensity();
            }
            base.Update(intensity, stacks);

            SyncedTreeAttribute attributes = Entity?.WatchedAttributes;
            if (attributes == null) return;
            float? value = attributes.TryGetFloat(this.AttributeName);
            value0 += ResultingIntensity();
            if (value != null && this.Adds) value0 += value.Value;
            Math.Clamp(value0, this.AttributeMinimum, this.AttributeMaximum);
            attributes.SetFloat(this.AttributeName, value0);
            attributes.MarkPathDirty(this.AttributeName);
        }
    }
}
