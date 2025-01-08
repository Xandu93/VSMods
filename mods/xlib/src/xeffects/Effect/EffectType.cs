using System;
using System.Reflection;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents an effect in general.
    /// </summary>
    public class EffectType
    {
        /// <summary>
        /// Gets the effects system.
        /// Is only valid if the effect type is registered.
        /// </summary>
        /// <value>
        /// The effects system.
        /// </value>
        public XEffectsSystem EffectsSystem { get; internal set; }

        /// <summary>
        /// Gets the domain.
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public string Domain { get; internal set; }

        /// <summary>
        /// Gets the defaults.
        /// </summary>
        /// <value>
        /// The defaults.
        /// </value>
        public ITreeAttribute Defaults { get; internal set; }

        /// <summary>
        /// Gets the type of the effect.
        /// </summary>
        /// <value>
        /// The type of the effect.
        /// </value>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the internal name of this effect type. Used to save, load and identify this effect.
        /// This should always be the same and must be unique.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; private set; }

        /// <summary>
        /// Gets or sets the name that is displayed in the game.
        /// Can be localized.
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the name of the texture.
        /// </summary>
        /// <value>
        /// The name of the texture.
        /// </value>
        public string IconName { get; set; }

        /// <summary>
        /// Gets or sets the effect group.
        /// Every entity can only have one effect from a group.
        /// </summary>
        /// <value>
        /// The effect group.
        /// </value>
        public string EffectGroup { get; set; }

        /// <summary>
        /// Gets or sets the effect category.
        /// Various items can influence specific effect categories.
        /// </summary>
        /// <value>
        /// The effect category.
        /// </value>
        public string EffectCategory { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectType" /> class.
        /// </summary>
        /// <param name="name">The internal name of this effect type. Used to save, load and identify this effect type.
        /// This should always be the same and must be unique.</param>
        /// <param name="type">The type of the effect.</param>
        /// <param name="defaults">The defaults.</param>
        /// <param name="displayName">The name that is displayed in the game.</param>
        /// <param name="domain">The name of the domain of this effect. Is used to auto generate description string keys.</param>
        /// <param name="iconName">Name of the texture.</param>
        /// <param name="effectGroup">Group of this effect.</param>
        /// <param name="effectCategory">Category of this effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if name or type is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">>Is thrown if the type is not a type of an effect.</exception>
        public EffectType(string name, Type type, ITreeAttribute defaults = null, string displayName = null, string domain = null, string iconName = null, string effectGroup = null, string effectCategory = null)
        {
            this.Name = name ?? throw new ArgumentNullException("An effect name must not be null.");
            this.Type = type ?? throw new ArgumentNullException("An effect type must not be null.");
            this.Domain = domain ?? "xeffects";
            this.Defaults = defaults ?? new TreeAttribute();
            this.DisplayName = displayName ?? (this.Domain + ":" + name + "-effect");
            this.Description = this.Domain + ":" + name + "-effectdesc";
            this.IconName = iconName;
            this.EffectGroup = effectGroup;
            this.EffectCategory = effectCategory;

            if (!(this.Type == typeof(Effect) || this.Type.IsSubclassOf(typeof(Effect)))) throw new ArgumentException("The type of an EffectType must be an Effect.");
        }

        /// <summary>
        /// Creates an effect.
        /// </summary>
        /// <returns></returns>
        public virtual Effect CreateEffect()
        {
            ConstructorInfo constructor = this.Type.GetConstructor(new Type[] { typeof(EffectType) });
            Effect effect = constructor?.Invoke(new object[] { this }) as Effect;
            effect?.FromTree(Defaults);
            return effect;
        }
    }//!class EffectType
}//!namespace XLib.XEffects
