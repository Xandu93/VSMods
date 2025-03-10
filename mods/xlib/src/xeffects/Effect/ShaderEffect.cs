using System;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect that can apply a shader program
    /// </summary>
    /// <seealso cref="Effect" />
    public class ShaderEffect : Effect
    {
        /// <summary>
        /// Gets or sets the name of the shader program that is applied by this effect.
        /// </summary>
        /// <value>
        /// The name of the shader program.
        /// </value>
        public string ShaderName { get; protected set; }

        /// <summary>
        /// Gets or sets the renderer that applies the shader.
        /// </summary>
        /// <value>
        /// The shader.
        /// </value>
        public EffectRenderer Renderer { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public ShaderEffect(EffectType effectType) : this(effectType, 1.0f, "")
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <param name="duration">The duration.</param>
        /// <param name="shader">Name of the shader.</param>
        /// <param name="maxStacks">The maximum stacks.</param>
        /// <param name="stacks">The stacks.</param>
        /// <param name="intensity">The intensity.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public ShaderEffect(EffectType effectType, float duration, string shader, int maxStacks = 1, int stacks = 1, float intensity = 0.0f) :
        base(effectType, duration, maxStacks, stacks, intensity)
        {
            this.ShaderName = shader;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);

            if (tree.HasAttribute("shader"))
            {
                this.ShaderName = tree.GetString("shader", this.ShaderName);
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
            if (this.ShaderName != null) result.SetString("shader", this.ShaderName);
            return result;
        }

        /// <summary>
        /// Called after an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            if (this.Entity.Api is ICoreClientAPI capi)
            {
                if (this.Entity != capi.World.Player.Entity) return;
                Renderer = new EffectRenderer(capi, ShaderName);
                Renderer.Register();
                Renderer.Intensity = ResultingIntensity();
            }
        }

        /// <summary>
        /// Called when an effect ends.
        /// </summary>
        public override void OnEnd()
        {
            base.OnEnd();
            Renderer?.Dispose();
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            if (Renderer != null) Renderer.Intensity = ResultingIntensity();
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
            if (Renderer != null) Renderer.Intensity = ResultingIntensity();
        }
    }
}
