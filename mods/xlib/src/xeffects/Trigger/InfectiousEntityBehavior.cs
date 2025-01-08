using System;
using System.Collections.Generic;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// The entity can cause effects when attacking other entities.
    /// </summary>
    /// <seealso cref="EntityBehavior" />
    public class InfectiousEntityBehavior : EntityBehavior
    {
        /// <summary>
        /// The name of the property tied to this entity behavior.
        /// </summary>
        /// <returns></returns>
        public override string PropertyName() => "Infectious";

        /// <summary>
        /// The effects system
        /// </summary>
        protected XEffectsSystem system;

        /// <summary>
        /// The triggers
        /// </summary>
        public List<EntityTrigger> Triggers { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InfectiousEntityBehavior"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public InfectiousEntityBehavior(Entity entity) : base(entity)
        {
            system = entity.Api.ModLoader.GetModSystem<XEffectsSystem>();
            if (system == null) throw new ApplicationException("Could not find effect system!");
        }

        /// <summary>
        /// Initializes the entity behavior.
        /// </summary>
        /// <param name="properties">The properties of this entity.</param>
        /// <param name="attributes">The attributes of this entity.</param>
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);
            system.EntityTrigger.TryGetValue(properties.Code.Path, out List<EntityTrigger> triggerList);

            if (triggerList != null)
            {
                Triggers = triggerList;
                return;
            }

            triggerList = new List<EntityTrigger>();
            Triggers = triggerList;
            system.EntityTrigger[properties.Code.Path] = triggerList;

            TreeAttribute tree = attributes.ToAttribute() as TreeAttribute;
            ArrayAttribute<TreeAttribute> effectsTree = tree?.GetAttribute("effects") as ArrayAttribute<TreeAttribute>;
            if (effectsTree == null) return;

            foreach (TreeAttribute attribute in effectsTree.value)
            {
                string effectName = attribute.GetString("effect");
                ITreeAttribute attributesTree = attribute.GetTreeAttribute("attributes");
                if (effectName == null || attributesTree == null) continue;
                EffectType effect = system.EffectType(effectName);
                if (effect == null) continue;

                EntityTrigger trigger = new EntityTrigger(effect);
                trigger.FromTree(attributesTree);
                Triggers.Add(trigger);
            }
        }
    }//!class InfectiousEntityBehavior
}//!namespace XLib.XEffects
