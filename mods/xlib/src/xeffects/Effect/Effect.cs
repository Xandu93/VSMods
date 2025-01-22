using System;
using System.ComponentModel.Design;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// The expire state defines under which circumstances an effect expires.
    /// </summary>
    [Flags]
    public enum ExpireState : int
    {
        /// <summary>
        /// The effect does not expire.
        /// </summary>
        Endless = 0x0000,

        /// <summary>
        /// The effect expires when the entity dies.
        /// </summary>
        Death = 0x0001,

        /// <summary>
        /// The effect expires when their runtime becomes greater than their duration.
        /// </summary>
        Time = 0x0002,

        /// <summary>
        /// The effect expires when their intensity becomes 0.
        /// </summary>
        Intensity = 0x0004,

        /// <summary>
        /// The effect accumulates when merged with another effect of the same type
        /// instead of merging.
        /// </summary>
        Accumulates = 0x0008,
    }

    /// <summary>
    /// Represents an effect that affects an entity
    /// </summary>
    public class Effect
    {
        /// <summary>
        /// Gets the type of the effect.
        /// </summary>
        /// <value>
        /// The type of the effect.
        /// </value>
        public EffectType EffectType { get; private set; }

        /// <summary>
        /// Gets or sets the duration.
        /// </summary>
        /// <value>
        /// The duration.
        /// </value>
        public float Duration { get; set; }

        /// <summary>
        /// Gets or sets the runtime.
        /// </summary>
        /// <value>
        /// The runtime.
        /// </value>
        public float Runtime { get; set; }

        /// <summary>
        /// Gets or sets the trigger interval.
        /// </summary>
        /// <value>
        /// The interval.
        /// </value>
        public float Interval { get; set; }

        /// <summary>
        /// Gets or sets the last triggered.
        /// </summary>
        /// <value>
        /// The last triggered.
        /// </value>
        public float LastTriggered { get; protected set; }

        /// <summary>
        /// The maximum stacks
        /// </summary>
        private int maxStacks;

        /// <summary>
        /// Gets or sets the maximum stacks.
        /// </summary>
        /// <value>
        /// The maximum stacks.
        /// </value>
        public virtual int MaxStacks { get => maxStacks; set => maxStacks = Math.Max(value, 1); }

        /// <summary>
        /// The stacks
        /// </summary>
        private int stacks;

        /// <summary>
        /// Gets or sets the stacks.
        /// </summary>
        /// <value>
        /// The stacks.
        /// </value>
        public int Stacks { get => stacks; set => stacks = Math.Clamp(value, 1, MaxStacks); }

        /// <summary>
        /// Gets or sets the intensity.
        /// </summary>
        /// <value>
        /// The intensity.
        /// </value>
        public float Intensity { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Effect"/> expires.
        /// </summary>
        /// <value>
        /// The expire state.
        /// </value>
        public ExpireState ExpireState { get; set; }

        /// <summary>
        /// Gets or sets the immunity duration.
        /// </summary>
        /// <value>
        /// The immunity duration.
        /// </value>
        public float ImmunityDuration { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the effect has been started.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the effect has been started; otherwise, <c>false</c>.
        /// </value>
        public bool Running { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the effect expires at the death of the entity.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the effect expires at the death of the entity; otherwise, <c>false</c>.
        /// </value>
        public bool ExpiresAtDeath 
        { 
            get => (ExpireState & ExpireState.Death) > 0; 
            set => ExpireState = value ? ExpireState | ExpireState.Death : ExpireState & ~ExpireState.Death; 
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Effect"/> expires over time.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expires over time; otherwise, <c>false</c>.
        /// </value>
        public bool ExpiresOverTime 
        { 
            get => (this.ExpireState & ExpireState.Time) > 0;
            set => ExpireState = value ? ExpireState | ExpireState.Time : ExpireState & ~ExpireState.Time; 
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Effect"/> expires through intensity.
        /// </summary>
        /// <value>
        ///   <c>true</c> if expires through intensity; otherwise, <c>false</c>.
        /// </value>
        public bool ExpiresThroughIntensity
        {
            get => (this.ExpireState & ExpireState.Intensity) > 0;
            set => ExpireState = value ? ExpireState | ExpireState.Intensity : ExpireState & ~ExpireState.Intensity;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Effect"/> accumulates when merged with another effect.
        /// </summary>
        /// <value>
        ///   <c>true</c> if accumulates when merged with another effect; otherwise, <c>false</c>.
        /// </value>
        public bool Accumulates
        {
            get => (this.ExpireState & ExpireState.Accumulates) > 0;
            set => ExpireState = value ? ExpireState | ExpireState.Accumulates : ExpireState & ~ExpireState.Accumulates;
        }

        /// <summary>
        /// Gets the time left.
        /// </summary>
        /// <value>
        /// The time left.
        /// </value>
        public float TimeLeft { get => Duration - Runtime; }

        /// <summary>
        /// Gets the behavior.
        /// </summary>
        /// <value>
        /// The behavior.
        /// </value>
        public virtual AffectedEntityBehavior Behavior { get; internal set; }

        /// <summary>
        /// Gets the entity.
        /// </summary>
        /// <value>
        /// The entity.
        /// </value>
        public Entity Entity { get => this.Behavior?.entity; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public Effect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <param name="interval">The interval</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public Effect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float intensity = 1.0f, float interval = 0.0f)
        {
            this.EffectType = effectType ?? throw new ArgumentNullException("An effect type of an effect must not be null.");
            this.Duration = duration;
            this.Interval = interval;
            this.MaxStacks = maxStacks;
            this.Stacks = stacks;
            this.Intensity = intensity;
            this.Runtime = 0.0f;
            this.LastTriggered = 0.0f;
            this.ImmunityDuration = 0.0f;
            this.Running = false;

            this.ExpireState = ExpireState.Death | ExpireState.Intensity;
            if (duration > 0.0f)
            {
                this.ExpireState |= ExpireState.Time;
            }
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public virtual void FromTree(ITreeAttribute tree)
        {
            this.Duration = (float)tree.GetDecimal("duration", this.Duration);
            this.Interval = (float)tree.GetDecimal("interval", this.Duration);
            this.MaxStacks = tree.GetInt("maxstacks", this.MaxStacks);
            this.Runtime = (float)tree.GetDecimal("runtime", this.Runtime);
            this.LastTriggered = (float)tree.GetDecimal("lasttriggered", this.LastTriggered);
            this.ExpireState = (ExpireState)tree.GetInt("expires", (int)this.ExpireState);
            this.ExpiresAtDeath = tree.GetBool("expiresatdeath", this.ExpiresAtDeath);
            this.ExpiresOverTime = tree.GetBool("expiresovertime", this.ExpiresOverTime);
            this.ExpiresThroughIntensity = tree.GetBool("expiresthroughintensity", this.ExpiresThroughIntensity);
            this.Accumulates = tree.GetBool("accumulates", this.Accumulates);
            this.ImmunityDuration = (float)tree.GetDecimal("immunityduration", this.ImmunityDuration);

            if (Running)
            {
                Update((float)tree.GetDecimal("intensity", this.Intensity), tree.GetInt("stacks", this.Stacks));
            }
            else
            {            
                this.Stacks = tree.GetInt("stacks", this.Stacks);
                this.Intensity = (float)tree.GetDecimal("intensity", this.Intensity);
            }
        }

        /// <summary>
        /// Converts to an attribute tree.
        /// </summary>
        /// <returns>The tree.</returns>
        public virtual ITreeAttribute ToTree()
        {
            ITreeAttribute tree = new TreeAttribute();
            tree.SetFloat("duration", this.Duration);
            tree.SetFloat("interval", this.Interval);
            tree.SetInt("maxstacks", this.MaxStacks);
            tree.SetInt("stacks", this.Stacks);
            tree.SetFloat("intensity", this.Intensity);
            tree.SetFloat("runtime", this.Runtime);
            tree.SetFloat("lasttriggered", this.LastTriggered);
            tree.SetInt("expires", (int)this.ExpireState);
            tree.SetFloat("immunityduration", this.ImmunityDuration);
            return tree;
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public virtual void OnStart()
        {
            Running = true;
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public virtual void OnEnd()
        {
            Running = false;
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public virtual void OnInterval()
        { }

        /// <summary>
        /// Called when an effect expires.
        /// </summary>
        public virtual void OnExpires()
        {
            if (this.ImmunityDuration > 0.0f) Behavior.SetImmunity(this.EffectType.Name, this.ImmunityDuration);
            OnEnd();
        }

        /// <summary>
        /// Called when an effect is cured by an item.
        /// </summary>
        /// <param name="cure">The cure props.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <returns>
        /// Whether the cure was used.
        /// </returns>
        public virtual bool OnCured(CureProps cure, float multiplier)
        {
            bool used = false;
            float intensity = cure.intensity * multiplier;
            float duration = cure.duration * multiplier;
            if (intensity > 0.0f && cure.minintensity < Intensity)
            {
                used = true;
                intensity = Math.Max(Intensity - (intensity / this.stacks), cure.minintensity);
                Update(intensity);
            }
            if (duration > 0.0f && cure.minduration < Duration)
            {
                used = true;
                Duration = Math.Max(Duration - duration, cure.minduration);
            }
            return used;
        }

        /// <summary>
        /// Called when an affected entity dies.
        /// </summary>
        public virtual void OnDeath()
        {
            if (this.ExpiresAtDeath)
            {
                this.OnEnd();
            }
        }

        /// <summary>
        /// Called when an effect was removed from other sources.
        /// </summary>
        public virtual void OnRemoved()
        {
            OnEnd();
        }

        /// <summary>
        /// Called when the effect should be renewed.
        /// </summary>
        /// <param name="other">The other.</param>
        public virtual void OnRenewed(Effect other)
        {
            float intensity;
            float interval;
            int stacks = this.Stacks + other.Stacks;
            if (this.Duration == 0.0f)
            {
                if (this.Accumulates)
                    intensity = this.Intensity + other.Intensity;
                else
                    intensity = (this.Intensity * this.Stacks + other.Intensity * other.Stacks) / (this.Stacks + other.Stacks);
                interval = (this.Interval * this.Stacks + other.Interval * other.Stacks) / (this.Stacks + other.Stacks);
            }
            else
            {
                if (this.Accumulates)
                    intensity = this.Intensity + other.Intensity;
                else 
                    intensity = (this.Intensity * this.Stacks * this.Duration + other.Intensity * other.Stacks * other.Duration) / (this.Stacks * this.Duration + other.Stacks * other.Duration);
                interval = (this.Interval * this.Stacks * this.Duration + other.Interval * other.Stacks * other.Duration) / (this.Stacks * this.Duration + other.Stacks * other.Duration);
            }

            this.Interval = interval;
            this.Duration = Math.Max(this.Duration, other.Duration);
            this.MaxStacks = Math.Max(this.MaxStacks, other.MaxStacks);
            this.LastTriggered -= this.Runtime;
            this.Runtime = 0.0f;
            Update(intensity, stacks);
        }

        /// <summary>
        /// Updates the values of the effect.
        /// Some effects require a special handling when these values change.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        public virtual void Update(float intensity, int stacks = 0)
        {
            this.Intensity = intensity;
            if (stacks != 0) this.Stacks = stacks;
        }

        /// <summary>
        /// Returns the resulting intensity.
        /// This is usually intensity * stacks.
        /// </summary>
        /// <returns></returns>
        public virtual float ResultingIntensity()
        {
            return this.Intensity * this.Stacks;
        }

        /// <summary>
        /// Fired when a game ticks over.
        /// </summary>
        /// <param name="dt">Past time since the last tick.</param>
        /// <returns>true if the interval ticked over; otherwise, false</returns>
        public virtual bool OnTick(float dt)
        {
            this.Runtime += dt;
            if (this.Interval > 0.0f && this.LastTriggered + this.Interval < this.Runtime)
            {
                this.OnInterval();
                this.LastTriggered += this.Interval;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Decides whether the effect should expire now.
        /// Effects usually expire when their intensity becomes 0 or their runtime becomes greater than their duration. 
        /// </summary>
        /// <returns>true if the effect should expire now; otherwise, false</returns>
        public virtual bool ShouldExpire()
        {
            return 
                (ExpiresOverTime && this.Runtime >= this.Duration) ||
                (ExpiresThroughIntensity && this.Intensity == 0.0f);
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <returns></returns>
        public virtual string GetName()
        {
            if (ExpiresOverTime)
            {
                if (this.Stacks > 1) return this.EffectType.DisplayName + "(" + this.Stacks + ") " + TimeToString(this.TimeLeft);
                else return this.EffectType.DisplayName + " " + TimeToString(this.TimeLeft);
            }
            else
            {
                if (this.Stacks > 1) return this.EffectType.DisplayName + "(" + this.Stacks + ") " + this.Intensity.ToString("n2");
                else return this.EffectType.DisplayName + " " + this.Intensity.ToString("n2");
            }
        }

        /// <summary>
        /// Gets the description.
        /// Provides some default values for string interpolation.
        /// {0}: intensity
        /// {1}: interval
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            try 
            {
                return string.Format(this.EffectType.Description ?? "", this.ResultingIntensity(), this.Interval);
            }
            catch (Exception)
            {
                return this.EffectType.Description;
            }
        }

        /// <summary>
        /// Converts a time to a string.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public static string TimeToString(float time)
        {
            if (time <= 60.0f)
            {
                return time.ToString("N1") + "s";
            }
            else if (time <= 3600.0f)
            {
                return (time / 60.0f).ToString("N1") + "m";
            }
            else if (time <= 86400.0f)
            {
                return (time / 3600.0f).ToString("N1") + "h";
            }
            else
            {
                return (time / 86400.0f).ToString("N1") + "d";
            }
        }
    }//!class XEffects
}//!namespace XLib.XEffects
