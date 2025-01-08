using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect that affects an entities stats
    /// </summary>
    /// <seealso cref="Effect" />
    public class StatEffect : Effect
    {
        /// <summary>
        /// Gets or sets the name of the stat that is affected by this effect.
        /// </summary>
        /// <value>
        /// The name of the stat.
        /// </value>
        public string StatName { get; protected set; }

        /// <summary>
        /// Gets or sets the stat identifier.
        /// Used to differ the stat modifiers from different sources.
        /// </summary>
        /// <value>
        /// The stat identifier.
        /// </value>
        private string EffectStatId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public StatEffect(EffectType effectType) : this(effectType, 1.0f, "")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="StatEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="statName">Name of the stat.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public StatEffect(EffectType effectType, float duration, string statName, int maxStacks = 1, int stacks = 1, float intensity = 0.0f) :
        base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.StatName = statName ?? "";
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            string stat = tree.GetString("statname", this.StatName);
            bool restart = false;
            if (Running && stat != this.StatName)
            {
                OnEnd();
                restart = true;
            }
            base.FromTree(tree);
            this.StatName = stat;
            this.EffectStatId = tree.GetString("effectstatid", this.EffectStatId);
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
            result.SetString("statname", this.StatName);
            result.SetString("effectstatid", this.EffectStatId);
            return result;
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            EntityFloatStats stats = null;
            try
            {
                stats = Entity.Stats[this.StatName];
            }
            catch(KeyNotFoundException exception) 
            {  
                this.Entity.Api.Logger.Error(exception);
            }
            if (stats == null) return;
            int cc = 0;
            if (EffectStatId == null)
            {
                do
                {
                    EffectStatId = "effect-" + this.EffectType.Name + cc;
                    cc++;
                } while (stats.ValuesByKey.ContainsKey(EffectStatId));
            }
            Entity.Stats.Set(this.StatName, EffectStatId, ResultingIntensity(), false);
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
            if (EffectStatId == null) return;
            Entity.Stats.Remove(this.StatName, EffectStatId);
        }

        /// <summary>
        /// Updates the values of the effect.
        /// Some effects require a special handling when these values change.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        public override void Update(float intensity, int stacks = 0)
        {
            base.Update(intensity, stacks);
            if (EffectStatId == null) return;
            Entity.Stats.Set(this.StatName, EffectStatId, ResultingIntensity(), false);
        }
    }//!class StatEffect
}//! namespace XLib.XEffects
