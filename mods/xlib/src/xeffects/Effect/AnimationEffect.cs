using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect that can play player animations
    /// </summary>
    /// <seealso cref="Effect" />
    public class AnimationEffect : Effect
    {
        /// <summary>
        /// Gets or sets the name of the animation that is played by this effect.
        /// </summary>
        /// <value>
        /// The name of the animation.
        /// </value>
        public string Animation { get; protected set; }

        /// <summary>
        /// Gets or sets the sound that is played by this effect.
        /// </summary>
        /// <value>
        /// The sound.
        /// </value>
        public AssetLocation Sound { get; protected set; }

        /// <summary>
        /// Gets or sets the chance that the animation will be triggered.
        /// </summary>
        /// <value>
        /// The name of the animation.
        /// </value>
        public float Chance { get => this.Intensity; protected set => this.Intensity = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public AnimationEffect(EffectType effectType) : this(effectType, 1.0f, "")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AnimationEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="animation">Name of the animation.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public AnimationEffect(EffectType effectType, float duration, string animation, int maxStacks = 1, int stacks = 1, float intensity = 0.0f) :
        base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.Animation = animation;
            this.Sound = null;
            this.Chance = 1.0f;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.Chance = (float)tree.GetDecimal("chance", this.Chance);

            if (tree.HasAttribute("animation"))
            {
                this.Animation = tree.GetString("animation", this.Animation);
            }
            if (tree.HasAttribute("sound"))
            {
                AssetLocation asset = new AssetLocation(tree.GetString("sound"));
                this.Sound = asset;
            }
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
            if (this.Animation != null) result.SetString("animation", this.Animation);
            if (this.Sound != null) result.SetString("sound", this.Sound.Domain + ':' + this.Sound.Path);
            return result;
        }

        /// <summary>
        /// Tries to play the animation and sound.
        /// </summary>
        protected virtual void TryPlay()
        {
            if (Entity.Api.Side == EnumAppSide.Server) return;
            if (this.Chance > Entity.World.Rand.NextDouble())
            {
                if (this.Animation != null)
                    this.Entity.AnimManager.StartAnimation(this.Animation);

                if (this.Sound != null)
                    this.Entity.World.PlaySoundAt(this.Sound, this.Entity);
            }
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            TryPlay();
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
            this.Entity.AnimManager.StopAnimation(this.Animation);
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            TryPlay();
        }
    }
}
