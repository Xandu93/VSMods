using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect that consists of multiple effects
    /// </summary>
    /// <seealso cref="Effect" />
    public class Condition : Effect
    {
        /// <summary>
        /// Sets the maximum stacks.
        /// </summary>
        /// <value>
        /// The maximum stacks.
        /// </value>
        public override int MaxStacks 
        {
            set
            {
                base.MaxStacks = value;
                if (SynchronizedMaxStackSize)
                {
                    foreach (Effect effect in this.Effetcs.Values)
                    {
                        effect.MaxStacks = MaxStacks;
                    }
                }
            } 
        }

        /// <summary>
        /// Gets or sets a value indicating whether the maximum stack size is synchronized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the maximum stack sizes of all effects in this condition are the same; otherwise, <c>false</c>.
        /// </value>
        public bool SynchronizedMaxStackSize { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether the interval is synchronized.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the interval of all effects in this condition are the same; otherwise, <c>false</c>.
        /// </value>
        public bool SynchronizedInterval { get; protected set; }

        /// <summary>
        /// Gets the behavior.
        /// </summary>
        /// <value>
        /// The behavior.
        /// </value>
        public override AffectedEntityBehavior Behavior
        {
            internal set
            {
                base.Behavior = value;
                foreach (Effect effect in this.Effetcs.Values)
                { 
                    effect.Behavior = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets the effetcs.
        /// </summary>
        /// <value>
        /// The effetcs.
        /// </value>
        protected Dictionary<string, Effect> Effetcs { get; set; } = new Dictionary<string, Effect>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public Condition(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Condition" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public Condition(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float intensity = 1.0f) :
            base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.SynchronizedMaxStackSize = true;
            this.SynchronizedInterval = true;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            TreeAttribute attributes = tree as TreeAttribute;
            this.SynchronizedMaxStackSize = tree.GetBool("synchronizedmaxstack", this.SynchronizedMaxStackSize);
            this.SynchronizedInterval = tree.GetBool("synchronizedinterval", this.SynchronizedInterval);
            TreeAttribute effectsTree = attributes.GetTreeAttribute("effects") as TreeAttribute;
            if (effectsTree != null)
            {
                foreach (string name in effectsTree.Keys)
                {
                    ITreeAttribute subTree = effectsTree.GetTreeAttribute(name);
                    if (this.Effetcs.ContainsKey(name))
                    {
                        this.Effetcs[name].FromTree(subTree);
                    }
                    else
                    {
                        Effect effect = this.EffectType.EffectsSystem.CreateEffect(name);
                        if (effect == null) continue;
                        effect.FromTree(subTree);
                        this.AddEffect(effect, false);
                    }
                }
            }
            //don't update children again. already did that above
            bool running = this.Running;
            this.Running = false;
            base.FromTree(tree);
            this.Running = running;
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
            result.SetBool("synchronizedmaxstack", this.SynchronizedMaxStackSize);
            result.SetBool("synchronizedinterval", this.SynchronizedInterval);
            TreeAttribute effectsTree = new TreeAttribute();
            foreach (Effect effect in this.Effetcs.Values)
            {
                ITreeAttribute effectTree = effect.ToTree();
                effectsTree.SetAttribute(effect.EffectType.Name, effectTree);
            }
            result.SetAttribute("effects", effectsTree);
            return result;
        }

        /// <summary>
        /// Adds the effect.
        /// </summary>
        /// <param name="effect">The effect.</param>
        /// <param name="shouldStart">Should be true if you use this after this effect has been started.</param>
        public virtual void AddEffect(Effect effect, bool shouldStart)
        {
            if (effect == null) return;
            if (this.Effetcs.ContainsKey(effect.EffectType.Name)) return;
            this.Effetcs.Add(effect.EffectType.Name, effect);
            effect.Behavior = this.Behavior;
            if (this.SynchronizedMaxStackSize)
            {
                effect.MaxStacks = this.MaxStacks;
                effect.Stacks = this.Stacks;
            }
            if (this.SynchronizedInterval)
            {
                effect.Interval = this.Interval;
            }
            if (shouldStart) effect.OnStart();
        }

        /// <summary>
        /// Removes the effect.
        /// </summary>
        /// <param name="effectName">The effect.</param>
        public virtual void RemoveEffect(string effectName)
        {
            if (effectName == null) return;
            Effect effect; 
            if(!this.Effetcs.TryGetValue(effectName, out effect)) return;
            effect.OnRemoved();
            this.Effetcs.Remove(effectName);
        }

        /// <summary>
        /// Called when an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            foreach (Effect effect in this.Effetcs.Values)
            {
                effect.Behavior = this.Behavior;
                effect.OnStart();
            }
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            if (!this.SynchronizedInterval) return;
            foreach (Effect effect in this.Effetcs.Values)
            {
                effect.Runtime = this.Runtime;
                effect.OnInterval();
            }
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
        }

        /// <summary>
        /// Called when an effect expires.
        /// </summary>
        public override void OnExpires()
        {
            foreach (Effect effect in this.Effetcs.Values) 
            { 
                effect.OnExpires(); 
            }
            base.OnExpires();
        }

        /// <summary>
        /// Called when a affected entity dies.
        /// </summary>
        public override void OnDeath()
        {
            foreach (Effect effect in this.Effetcs.Values) 
            {
                effect.ExpiresAtDeath = this.ExpiresAtDeath;
                effect.OnDeath(); 
            }
            base.OnDeath();
        }

        /// <summary>
        /// Called when an effect was removed from other sources.
        /// </summary>
        public override void OnRemoved()
        {
            foreach (Effect effect in this.Effetcs.Values) 
            { 
                effect.OnRemoved(); 
            }
            base.OnRemoved();
        }

        /// <summary>
        /// Called when the effect should be renewed.
        /// </summary>
        /// <param name="other">The other.</param>
        public override void OnRenewed(Effect other)
        {
            base.OnRenewed(other);
            foreach (Effect otherEffect in (other as Condition)?.Effetcs.Values)
            {
                this.Effetcs.TryGetValue(otherEffect.EffectType.Name, out Effect effect);
                effect.OnRenewed(otherEffect);
            }
        }

        /// <summary>
        /// Updates the values of the effect.
        /// Some effects require a special handling when these values change.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        public override void Update(float intensity, int stacks = 0)
        {
            UpdateChildren(intensity, stacks);
            base.Update(intensity, stacks);
        }

        /// <summary>
        /// Updates the childrens of the effect.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        protected virtual void UpdateChildren(float intensity, int stacks = 0)
        {
            foreach (Effect effect in this.Effetcs.Values)
            {
                effect.Update(effect.Intensity * (intensity / Intensity), stacks);
            }
        }

        /// <summary>
        /// Fired when a game ticks over.
        /// </summary>
        /// <param name="dt">Past time since the last tick.</param>
        /// <returns>true if the interval ticked over; otherwise, false</returns>
        public override bool OnTick(float dt)
        {
            this.Runtime += dt;

            if (!this.SynchronizedInterval)
            {
                foreach (Effect effect in this.Effetcs.Values)
                {
                    effect.OnTick(dt);
                }
            }

            if (this.Interval > 0.0f && this.LastTriggered + this.Interval < this.Runtime)
            {
                this.OnInterval();
                this.LastTriggered += this.Interval;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the intensity of a specific effect.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public virtual float GetIntensity(string name)
        {
            this.Effetcs.TryGetValue(name, out Effect effect);
            if (effect != null) return effect.Intensity;
            return 0.0f;
        }

        /// <summary>
        /// Sets the intensity of a specific effect.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="intensity">The intensity.</param>
        public virtual void SetIntensity(string name, float intensity)
        {
            this.Effetcs.TryGetValue(name, out Effect effect);
            if (effect != null) effect.Update(intensity);
        }

        /// <summary>
        /// Gets an effect with the specified name.
        /// Returns null if no effect with the name exists in this condition.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Effect Effect(string name)
        {
            this.Effetcs.TryGetValue(name, out Effect effect);
            return effect;
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(Lang.Get("xeffects:effects") + ":\n");
            bool empty = string.IsNullOrEmpty(this.EffectType.Description);

            if (!empty)
            {
                builder.Append('\n');
                try
                {
                    builder.Append(string.Format(this.EffectType.Description ?? "", this.ResultingIntensity(), this.Interval));
                }
                catch (Exception)
                {
                    builder.Append(this.EffectType.Description);
                }
                builder.Append('\n');
            }

            bool first = true;
            foreach (Effect effect in Effetcs.Values)
            {
                if (first) 
                    builder.Append('\n');
                else
                    builder.Append("\n\n");
                first = false;
                builder.Append(effect.EffectType.DisplayName);
                builder.Append('\n');
                builder.Append(effect.GetDescription());
            }

            return builder.ToString();
        }
    }//!class Condition
}//!namespace XLib.XEffects