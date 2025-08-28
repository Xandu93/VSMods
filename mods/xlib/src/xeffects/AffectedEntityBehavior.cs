using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// The entity can be affected by effects.
    /// </summary>
    /// <seealso cref="EntityBehavior" />
    public class AffectedEntityBehavior : EntityBehavior
    {
        /// <summary>
        /// The name of the property tied to this entity behavior.
        /// </summary>
        /// <returns></returns>
        public override string PropertyName() => "Affected";

        /// <summary>
        /// Gets the effects.
        /// </summary>
        /// <value>
        /// The effects.
        /// </value>
        public Dictionary<string, Effect> Effects { get; private set; }

        /// <summary>
        /// Gets the mining speed modifiers.
        /// </summary>
        /// <value>
        /// The mining speed modifiers.
        /// </value>
        protected Dictionary<EnumTool, float> MiningSpeedModifiers { get; private set; }

        /// <summary>
        /// The timer for effects
        /// </summary>
        protected float effectTimer;

        /// <summary>
        /// The timer for effect triggers
        /// </summary>
        protected float triggerTimer;

        /// <summary>
        /// The effects system
        /// </summary>
        protected XEffectsSystem system;

        /// <summary>
        /// Marks whether the effect tree should be updated.
        /// </summary>
        protected bool dirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="AffectedEntityBehavior"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public AffectedEntityBehavior(Entity entity) : base(entity)
        {
            system = entity.Api.ModLoader.GetModSystem<XEffectsSystem>();
            if (system == null) throw new ApplicationException("Could not find effect system!");

            this.Effects = new Dictionary<string, Effect>();
            effectTimer = 0.0f;
            triggerTimer = 0.0f;
            dirty = false;

            //add mining speed multiplier only for players
            if (this.entity as EntityPlayer != null)
            {
                this.MiningSpeedModifiers = new Dictionary<EnumTool, float>();
                this.entity.WatchedAttributes.GetOrAddTreeAttribute("immunities");

                for (EnumTool tool = 0; tool <= EnumTool.Scythe; tool++)
                {
                    this.MiningSpeedModifiers.Add(tool, 1.0f);
                }
            }
        }

        /// <summary>
        /// Initializes the entity behavior.
        /// </summary>
        /// <param name="properties">The properties of this entity.</param>
        /// <param name="attributes">The attributes of this entity.</param>
        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            if (this.entity.Api.Side == EnumAppSide.Client)
            {
                entity.WatchedAttributes.RegisterModifiedListener("effects", this.CreateEffectsFromTree);
            }
        }

        /// <summary>
        /// Creates the effects from a attribute tree.
        /// </summary>
        public void CreateEffectsFromTree()
        {
            TreeAttribute effectTree = entity.WatchedAttributes.GetOrAddTreeAttribute("effects") as TreeAttribute;
            CreateEffectsFromTree(effectTree);
        }

        /// <summary>
        /// Creates the effects from a attribute tree.
        /// </summary>
        /// <param name="effectTree">The effect tree.</param>
        public void CreateEffectsFromTree(TreeAttribute effectTree)
        {
            List<string> toRemove = new List<string>();
            foreach (KeyValuePair<string, Effect> pair in this.Effects) 
            {
                ITreeAttribute tree = effectTree.GetTreeAttribute(pair.Key);
                effectTree.RemoveAttribute(pair.Key);
                if (tree == null)
                {
                    toRemove.Add(pair.Key);
                }
                else
                {
                    pair.Value.FromTree(tree);
                }
            }
            foreach(string key in toRemove)
            {
                this.RemoveEffect(key);
            }

            foreach (string key in effectTree.Keys)
            {
                ITreeAttribute tree = effectTree.GetTreeAttribute(key);
                Effect effect = system.CreateEffect(key);
                if (effect == null) continue;

                effect.FromTree(tree);
                this.AddEffect(effect);
            }
        }

        /// <summary>
        /// The event fired when the entity is despawned.
        /// </summary>
        /// <param name="despawn">The reason the entity despawned.</param>
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntityDespawn(despawn);
            if (despawn.Reason == EnumDespawnReason.Disconnect || despawn.Reason == EnumDespawnReason.Unload) UpdateTree();
        }

        /// <summary>
        /// Updates the tree.
        /// </summary>
        public void UpdateTree()
        {
            TreeAttribute effectTree = new TreeAttribute();
            foreach (string key in this.Effects.Keys)
            {
                Effect effect = this.Effects[key];
                effectTree.SetAttribute(key, effect.ToTree());
            }
            entity.WatchedAttributes.SetAttribute("effects", effectTree);
            entity.WatchedAttributes.MarkPathDirty("effects");
        }

        /// <summary>
        /// Marks this as dirty.
        /// </summary>
        public void MarkDirty()
        {
            this.dirty = true;
        }

        /// <summary>
        /// The event fired when a game ticks over.
        /// </summary>
        /// <param name="deltaTime"></param>
        public override void OnGameTick(float deltaTime)
        {
            List<Effect> toRemove = new List<Effect>();
            effectTimer += deltaTime;
            triggerTimer += deltaTime;

            if (effectTimer >= system.Config.effectInterval)
            {
                effectTimer = Math.Min(effectTimer, system.Config.effectInterval * 1.5f);
                foreach (Effect effect in this.Effects.Values)
                {
                    if (effect.OnTick(effectTimer))
                    {
                        MarkDirty();
                    }
                    if (effect.ShouldExpire())
                    {
                        MarkDirty();
                        toRemove.Add(effect);
                    }
                }
                foreach (Effect effect in toRemove)
                {
                    effect.OnExpires();
                    this.Effects.Remove(effect.EffectType.Name);
                }
                List<string> toRemove2 = new List<string>();
                ITreeAttribute immunities = entity.WatchedAttributes.GetTreeAttribute("immunities");
                if (immunities != null)
                {
                    foreach (KeyValuePair<string, IAttribute> pair in immunities)
                    {
                        float value = (float)pair.Value.GetValue() - effectTimer;
                        if (value < 0.0f)
                        {
                            toRemove2.Add(pair.Key);
                        }
                        else immunities.SetFloat(pair.Key, value);
                    }
                    foreach (string key in toRemove2)
                    {
                        immunities.RemoveAttribute(key);
                    }
                }

                effectTimer = 0;
                if (this.dirty && this.entity.Api.Side == EnumAppSide.Server)
                {
                    dirty = false;
                    this.UpdateTree();
                }
            }

            if (triggerTimer >= system.Config.tiggerInterval && system.Api.Side == EnumAppSide.Server && entity.Alive)
            {
                int light = entity.World.BlockAccessor.GetLightLevel(entity.Pos.AsBlockPos, EnumLightLevelType.OnlySunLight);
                float undergroundHours = entity.WatchedAttributes.GetFloat("undergroundHours");

                if (light < 4)
                {
                    undergroundHours += triggerTimer * entity.World.Calendar.SpeedOfTime / 3600.0f;
                }
                else
                {
                    undergroundHours -= 4 * triggerTimer * entity.World.Calendar.SpeedOfTime / 3600.0f;
                }
                undergroundHours = Math.Clamp(undergroundHours, 0.0f, 168.0f);
                entity.WatchedAttributes.SetFloat("undergroundHours", undergroundHours);

                List<EffectTrigger> triggers = system.Trigger["attribute"];
                if (triggers == null) return;
                foreach (EffectTrigger trigger in triggers)
                {
                    if (trigger is not AttributeTrigger attributeTrigger) continue;
                    if (this.Effects.ContainsKey(attributeTrigger.ToTrigger.Name)) continue;
                    if (attributeTrigger.ShouldTrigger(this.entity, 1.0f, system.Config.tiggerInterval))
                    {
                        if (trigger.ToTrigger == null) continue;
                        if (this.IsAffectedBy(trigger.ToTrigger.Name)) continue;
                        if (IsImmune(trigger.ToTrigger.Name)) continue;
                        Effect effect = trigger.ToTrigger.CreateEffect();
                        if (effect == null) continue;

                        if (effect is DiseaseEffect disease) disease.Trigger = attributeTrigger;
                        effect.Update(trigger.ScaledIntensity((float)entity.World.Rand.NextDouble()));
                        this.AddEffect(effect);
                        this.MarkDirty();
                        break;
                    }
                }
                triggerTimer = 0;
            }
        }

        /// <summary>
        /// The event fired when the entity dies.
        /// </summary>
        /// <param name="damageSourceForDeath">The source of damage for the entity.</param>
        public override void OnEntityDeath(DamageSource damageSourceForDeath)
        {
            List<Effect> ToRemove = new List<Effect>();
            foreach (Effect effect in this.Effects.Values)
            {
                if (effect.ExpiresAtDeath) ToRemove.Add(effect);
                effect.OnDeath();
            }
            foreach (Effect effect in ToRemove)
            {
                this.Effects.Remove(effect.EffectType.Name);
            }
            this.UpdateTree();
        }

        /// <summary>
        /// The event fired when the entity receives damage.
        /// </summary>
        /// <param name="damageSource">The source of the damage</param>
        /// <param name="damage">The amount of the damage.</param>
        public override void OnEntityReceiveDamage(DamageSource damageSource, ref float damage)
        {
            if (system.Api.Side == EnumAppSide.Client) return;

            //general damage trigger
            List<EffectTrigger> triggers = system.Trigger["damage"];
            if (triggers == null) return;
            foreach (EffectTrigger trigger in triggers)
            {
                if (trigger is not DamageTrigger damageTrigger) continue;
                if (damageTrigger.ShouldTrigger(damageSource, this.entity, damage))
                {
                    Effect effect = trigger.ToTrigger.CreateEffect();
                    effect.Update(trigger.ScaledIntensity(damageTrigger.DamageIntensity * damage));
                    this.AddEffect(effect);
                    this.MarkDirty();
                }
            }

            //entity trigger
            EntityAgent entity = damageSource.CauseEntity as EntityAgent ?? damageSource.SourceEntity as EntityAgent;
            InfectiousEntityBehavior infectious = entity?.GetBehavior<InfectiousEntityBehavior>();
            if (infectious == null) return;
            foreach (EntityTrigger trigger in infectious.Triggers)
            {
                if (trigger.ShouldTrigger(damageSource, this.entity, damage))
                {
                    Effect effect = trigger.ToTrigger.CreateEffect();
                    effect.Update(trigger.ScaledIntensity(trigger.DamageIntensity * damage));
                    this.AddEffect(effect);
                    this.MarkDirty();
                }
            }
        }

        /// <summary>
        /// Adds an effect.
        /// </summary>
        /// <param name="effect">The effect.</param>
        public bool AddEffect(Effect effect)
        {
            if (effect == null) return false;
            if (IsImmune(effect.EffectType.Name)) return false;
            if (!entity.Alive && effect.ExpiresAtDeath) return false;

            effect.Behavior = this;
            foreach (Effect other in Effects.Values)
            {
                if (other.EffectType.Name == effect.EffectType.Name)
                {
                    other.OnRenewed(effect);
                    return true;
                }

                if (other.EffectType.EffectGroup != null &&
                    effect.EffectType.EffectGroup != null &&
                    other.EffectType.EffectGroup == other.EffectType.EffectGroup)
                {
                    other.OnRemoved();
                    Effects.Remove(other.EffectType.Name);
                    break;
                }
            }

            this.Effects.Add(effect.EffectType.Name, effect);
            MarkDirty();
            effect.OnStart();
            return true;
        }

        /// <summary>
        /// Determines whether the entity is affected by an effect with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>
        ///   <c>true</c> if the entity is effected by an effect with the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool IsAffectedBy(string name)
        {
            return this.Effects.ContainsKey(name);
        }

        /// <summary>
        /// Removes an effect with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowDisplayNames">if set to <c>true</c> the method also looks for matching display names.</param>
        /// <returns></returns>
        public bool RemoveEffect(string name, bool allowDisplayNames = false)
        {
            if (this.Effects.TryGetValue(name, out Effect effect))
            {
                effect.OnRemoved();
                MarkDirty();
                return this.Effects.Remove(name);
            }
            if (allowDisplayNames)
            {
                foreach (Effect other in this.Effects.Values)
                {
                    if (other.EffectType.DisplayName.Equals(name))
                    {
                        other.OnRemoved();
                        MarkDirty();
                        return this.Effects.Remove(other.EffectType.Name);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Clears all effects.
        /// </summary>
        public void Clear()
        {
            foreach(Effect effect in this.Effects.Values)
            {
                effect.OnRemoved();
            }
            this.Effects.Clear();
            this.UpdateTree();
        }

        /// <summary>
        /// Gets an effect with the specified name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Effect Effect(string name)
        {
            this.Effects.TryGetValue(name, out Effect effect);
            return effect;
        }

        /// <summary>
        /// Adds a mining speed multiplier.
        /// Add 1/multiplier to remove the multiplier
        /// </summary>
        /// <param name="tool">The tool.</param>
        /// <param name="multiplier">The multiplier.</param>
        public void AddMiningSpeedMultiplier(EnumTool tool, float multiplier)
        {
            if (this.MiningSpeedModifiers.ContainsKey(tool))
            {
                this.MiningSpeedModifiers[tool] *= multiplier;
            }
        }

        /// <summary>
        /// Gets the mining speed multiplier for the specified tool.
        /// </summary>
        /// <param name="tool">The tool.</param>
        /// <returns></returns>
        public float GetMiningSpeedMultiplier(EnumTool tool)
        {
            this.MiningSpeedModifiers.TryGetValue(tool, out float result);
            return result;
        }

        /// <summary>
        /// Determines whether the entity is immune to an effect.
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <returns>
        ///   <c>true</c> if the entity is immune to an effect with the specified name; otherwise, <c>false</c>.
        /// </returns>
        public bool IsImmune(string name)
        {
            return entity.WatchedAttributes.GetTreeAttribute("immunities")?.HasAttribute(name) ?? false;
        }

        /// <summary>
        /// Sets whether the entity is immune to an effect.
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        /// <param name="duration">The immunity duration.</param>
        public void SetImmunity(string name, float duration)
        {
            ITreeAttribute immunities = entity.WatchedAttributes.GetTreeAttribute("immunities");
            if (immunities == null) return;
            float old = immunities.GetFloat(name);
            immunities.SetFloat(name, Math.Max(duration, old));
        }

        /// <summary>
        /// Gets how long the entity is immune to an effect.
        /// </summary>
        /// <param name="name">The name of the effect.</param>
        public float GetImmunity(string name)
        {
            float result = entity.WatchedAttributes.GetTreeAttribute("immunities")?.GetFloat(name) ?? 0.0f;
            return result;
        }
    }//!class EffectedEntityBehavior
}//!namespace XLib.XEffects
