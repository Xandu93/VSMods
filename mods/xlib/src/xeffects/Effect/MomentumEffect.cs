using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// A mining speed effect
    /// </summary>
    /// <seealso cref="Effect" />
    public class MomentumEffect : Effect
    {
        /// <summary>
        /// The tool
        /// </summary>
        public EnumTool Tool { get; set; }

        /// <summary>
        /// Gets or sets the speed.
        /// Same as Intensity.
        /// </summary>
        /// <value>
        /// The speed.
        /// </value>
        public float Speed { get => this.Intensity; set => this.Intensity = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomentumEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public MomentumEffect(EffectType effectType) : this(effectType, 1.0f)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MomentumEffect" /> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="tool">The tool.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public MomentumEffect(EffectType effectType, float duration, int maxStacks = 1, int stacks = 1, float speed = 0.0f, EnumTool tool = EnumTool.Knife) :
            base(effectType, duration, maxStacks, stacks, speed)
        {
            this.Tool = tool;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            EnumTool tool = (EnumTool)tree.GetInt("tool", (int)this.Tool);
            bool restart = false;
            if (Running && tool != this.Tool)
            {
                OnEnd();
                restart = true;
            }
            base.FromTree(tree);
            this.Tool = tool;
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
            ITreeAttribute result = base.ToTree();
            result.SetInt("tool", (int)this.Tool);
            return result;
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            this.Behavior?.AddMiningSpeedMultiplier(this.Tool, 1.0f + ResultingIntensity());
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
            this.Behavior?.AddMiningSpeedMultiplier(this.Tool, 1.0f / (1.0f + ResultingIntensity()));
        }

        /// <summary>
        /// Called when the effect should be renewed.
        /// </summary>
        /// <param name="other">The other.</param>
        public override void OnRenewed(Effect other)
        {
            MomentumEffect momentum = other as MomentumEffect;
            if (momentum == null) return;

            if (momentum.Tool != this.Tool)
            {
                this.OnEnd();

                this.Duration = momentum.Duration;
                this.MaxStacks = momentum.MaxStacks;
                this.Stacks = momentum.Stacks;
                this.Speed = momentum.Speed;
                this.Tool = momentum.Tool;
                this.ExpireState = momentum.ExpireState;
                this.Runtime = 0.0f;
                this.LastTriggered = 0.0f;

                this.OnStart();
                return;
            }
            base.OnRenewed(other);
        }

        /// <summary>
        /// Updates the values of the effect.
        /// Some effects require a special handling when these values change.
        /// </summary>
        /// <param name="intensity">The new intensity.</param>
        /// <param name="stacks">The new stacks.</param>
        public override void Update(float intensity, int stacks = 0)
        {
            if (Running)
            {
                float oldModifier = 1.0f + ResultingIntensity();
                base.Update(intensity, stacks);
                float modifier = 1.0f + ResultingIntensity();
                this.Behavior?.AddMiningSpeedMultiplier(this.Tool, modifier / oldModifier);
            }
        }
    }//!class EffectMomentum
}//!namespace XLib.XEffects
