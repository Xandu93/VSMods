using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents one symptom of an disease
    /// </summary>
    public class Symptom
    {
        /// <summary>
        /// Gets or sets the threshold.
        /// </summary>
        /// <value>
        /// The trashold.
        /// </value>
        public float Threshold { get; set; }

        /// <summary>
        /// Gets or sets the name of the effect of the symptom
        /// </summary>
        /// <value>
        /// The name of the effect of the symptom.
        /// </value>
        public string TypeName { get; set; }

        /// <summary>
        /// Gets or sets the effect associated with the symptom.
        /// </summary>
        /// <value>
        /// The effect.
        /// </value>
        public Effect Effect { get; set; }

        /// <summary>
        /// Gets or sets the maximum intensity.
        /// </summary>
        /// <value>
        /// The maximum intensity.
        /// </value>
        public float MaxIntensity { get; set; }

        /// <summary>
        /// Gets or sets default values for symptoms.
        /// </summary>
        /// <value>
        /// The default values.
        /// </value>
        public TreeAttribute Defaults { get; set; }
    }

    /// <summary>
    /// Represents a disease
    /// </summary>
    /// <seealso cref="Condition" />
    public class DiseaseEffect : Condition
    {
        /// <summary>
        /// Gets or sets the spread range.
        /// </summary>
        /// <value>
        /// The spread range.
        /// </value>
        public float SpreadRange { get; set; }

        /// <summary>
        /// Gets or sets the spread chance.
        /// </summary>
        /// <value>
        /// The spread chance.
        /// </value>
        public float SpreadChance { get; set; }

        /// <summary>
        /// Gets or sets the last spread trigger.
        /// </summary>
        /// <value>
        /// The last spread trigger.
        /// </value>
        public float LastSpreadTrigger { get; protected set; }

        /// <summary>
        /// Gets or sets the last healing trigger.
        /// </summary>
        /// <value>
        /// The last healing trigger.
        /// </value>
        public float LastHealingTrigger { get; protected set; }

        /// <summary>
        /// Gets or sets the trigger.
        /// </summary>
        /// <value>
        /// The trigger.
        /// </value>
        public AttributeTrigger Trigger { get; internal set; }

        /// <summary>
        /// Gets the symptoms.
        /// </summary>
        /// <value>
        /// The symptoms.
        /// </value>
        public List<Symptom> Symptoms { get; private set; }

        /// <summary>
        /// The healing rate
        /// </summary>
        private float _healingRate;

        /// <summary>
        /// Gets or sets the healing rate.
        /// </summary>
        /// <value>
        /// The healing rate.
        /// </value>
        public float HealingRate 
        { 
            get => _healingRate; 
            set => _healingRate = Math.Clamp(value, MinHealingRate, MaxHealingRate); 
        }

        /// <summary>
        /// Gets or sets the minimum healing rate.
        /// </summary>
        /// <value>
        /// The minimum healing rate.
        /// </value>
        public float MinHealingRate { get; set; }

        /// <summary>
        /// Gets or sets the maximum healing rate.
        /// </summary>
        /// <value>
        /// The maximum healing rate.
        /// </value>
        public float MaxHealingRate { get; set; }

        /// <summary>
        /// Gets or sets the healing growth.
        /// </summary>
        /// <value>
        /// The healing growth.
        /// </value>
        public float HealingGrowth { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DiseaseEffect"/> class.
        /// </summary>
        /// <param name="effectType">Type of the effect.</param>
        /// <exception cref="ArgumentNullException">Is thrown if the effectType is <c>null</c>.</exception>
        public DiseaseEffect(EffectType effectType) : base(effectType)
        {
            SpreadRange = 0.0f;
            SpreadChance = 0.0f;
            LastSpreadTrigger = 0.0f;
            LastHealingTrigger = 0.0f;
            HealingRate = 0.0f;
            MinHealingRate = -1000.0f;
            MaxHealingRate = 1000.0f;
            HealingGrowth = 0.0f;
            Trigger = null;
        }

        /// <summary>
        /// Creates an effect from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void FromTree(ITreeAttribute tree)
        {
            base.FromTree(tree);
            this.SpreadRange = (float)tree.GetDecimal("spreadrange", this.SpreadRange);
            this.SpreadChance = (float)tree.GetDecimal("spreadchance", this.SpreadChance);
            this.LastSpreadTrigger = (float)tree.GetDecimal("lastspreadtrigger", this.LastSpreadTrigger);
            this.LastHealingTrigger = ((float)tree.GetDecimal("lasthealingtrigger", this.LastHealingTrigger));
            this.HealingRate = (float)tree.GetDecimal("healingrate", this.HealingRate);
            this.MinHealingRate = (float)tree.GetDecimal("minhealingrate", this.MinHealingRate);
            this.MaxHealingRate = (float)tree.GetDecimal("maxhealingrate", this.MaxHealingRate);
            this.HealingGrowth = (float)tree.GetDecimal("healinggrowth", this.HealingGrowth);

            List<Symptom> symptoms = new List<Symptom>();
            if (tree.GetTreeAttribute("symptoms") is TreeAttribute symptomsTree)
            {
                foreach (string name in symptomsTree.Keys)
                {
                    EffectType effectType = this.EffectType.EffectsSystem.EffectType(name);
                    ITreeAttribute subTree = symptomsTree.GetTreeAttribute(name);
                    if (effectType == null || subTree == null) continue;

                    string name0 = name;
                    Symptom symptom = new Symptom
                    {
                        TypeName = name0,
                        MaxIntensity = (float)subTree.GetDecimal("maxintensity", 1.0),
                        Threshold = (float)subTree.GetDecimal("threshold", 0.0),
                        Defaults = subTree.GetTreeAttribute("defaults") as TreeAttribute,
                    };
                    symptoms.Add(symptom);
                }
            }
            foreach (Symptom symptom1 in symptoms)
            {
                symptom1.Effect = this.Effect(symptom1.TypeName);
            }
            this.Symptoms = symptoms;
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
            result.SetFloat("spreadrange", this.SpreadRange);
            result.SetFloat("spreadchance", this.SpreadChance);
            result.SetFloat("lastspreadtrigger", this.LastSpreadTrigger);
            result.SetFloat("lasthealingtrigger", this.LastHealingTrigger);
            result.SetFloat("healingrate", this.HealingRate);
            result.SetFloat("minhealingrate", this.MinHealingRate);
            result.SetFloat("maxhealingrate", this.MaxHealingRate);
            result.SetFloat("healinggrowth", this.HealingGrowth);

            TreeAttribute symptomTrees = new TreeAttribute();
            foreach (Symptom symptom in this.Symptoms)
            {
                TreeAttribute symptomTree = new TreeAttribute();
                symptomTree.SetFloat("maxintensity", symptom.MaxIntensity);
                symptomTree.SetFloat("threshold", symptom.Threshold);
                symptomTrees.SetAttribute(symptom.TypeName, symptomTree);
            }
            result.SetAttribute("symptoms", symptomTrees);
            return result;
        }

        /// <summary>
        /// Called when an effect is cured by an item.
        /// </summary>
        /// <param name="cure">The cure props.</param>
        /// <param name="multiplier">The multiplier.</param>
        /// <returns>
        /// Whether the cure was used.
        /// </returns>
        public override bool OnCured(CureProps cure, float multiplier)
        {
            bool used = base.OnCured(cure, multiplier);
            float healinggrowth = cure.healinggrowth * multiplier;
            float healingrate = cure.healingrate * multiplier;
            if (healinggrowth > 0.0f && this.HealingGrowth < cure.maxhealinggrowth)
            {
                used = true;
                this.HealingGrowth = Math.Min(cure.maxhealinggrowth, this.HealingGrowth + healinggrowth);
            }
            if (healingrate > 0.0f && this.HealingRate < cure.maxhealingrate && this.HealingRate < this.MaxHealingRate)
            {
                used = true;
                this.HealingRate = Math.Min(
                    Math.Min(this.MaxHealingRate, cure.maxhealingrate), 
                    this.HealingRate + cure.healingrate);
            }
            return used;
        }

        /// <summary>
        /// Check the symptoms. Adds effects when threshold is reached.
        /// Removes effects when threshold is undercut.
        /// </summary>
        public virtual void CheckSymptoms()
        {
            if (Behavior == null) return;
            foreach (Symptom symptom in Symptoms)
            {
                if (symptom.Threshold > Intensity && symptom.Effect != null)
                {
                    this.RemoveEffect(symptom.Effect.EffectType.Name);
                    symptom.Effect = null;
                }
                else if (symptom.Threshold <= Intensity && symptom.Effect == null)
                {
                    symptom.Effect = this.EffectType.EffectsSystem.CreateEffect(symptom.TypeName);
                    if (symptom.Defaults != null) symptom.Effect.FromTree(symptom.Defaults);
                    this.AddEffect(symptom.Effect, true);
                }
                if (symptom.Effect != null)
                {
                    float symptonIntensity;
                    if (symptom.Threshold >= 1.0) symptonIntensity = 1.0f;
                    else symptonIntensity = (Intensity - symptom.Threshold) / (1.0f - symptom.Threshold);
                    symptom.Effect.Update(symptonIntensity * symptom.MaxIntensity, Stacks);
                }
            }
        }

        /// <summary>
        /// Called when an effect was created.
        /// </summary>
        public override void OnStart()
        {
            base.OnStart();
            CheckSymptoms();
            if (this.Trigger == null && this.Entity != null)
            {
                Trigger = this.Entity.Api.ModLoader.GetModSystem<XEffectsSystem>().FindTrigger(this.EffectType, "attribute") as AttributeTrigger;
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
            this.Intensity = intensity;
            if (stacks != 0) this.Stacks = stacks;
            CheckSymptoms();
        }

        /// <summary>
        /// Called when an interval ticks over.
        /// </summary>
        public override void OnInterval()
        {
            base.OnInterval();
            IWorldAccessor world = this.Entity?.World;
            CheckHealing();
            CheckSpread(world);
        }

        /// <summary>
        /// Checks if the disease should heal.
        /// </summary>
        public virtual void CheckHealing()
        {
            float healingInterval = 1.0f;
            if (this.LastHealingTrigger + healingInterval < this.Runtime)
            {
                this.LastHealingTrigger += healingInterval;

                //change intensity
                float newIntensity = Math.Clamp(Intensity - HealingRate * healingInterval, 0.0f, 1.0f);
                if (newIntensity != Intensity) Update(newIntensity);

                if (Trigger != null)
                {
                    float chance = (float)Trigger.GetChance(this.Entity, 1.0f);
                    if (chance > Trigger.RecoveryThreshold) return;
                }
                HealingRate += HealingGrowth * healingInterval;
            }
        }

        /// <summary>
        /// Checks if the disease should spread.
        /// </summary>
        public virtual void CheckSpread(IWorldAccessor world)
        {
            float spreadInterval = 30.0f;
            if (this.LastSpreadTrigger + spreadInterval < this.Runtime)
            {
                this.LastSpreadTrigger += spreadInterval;

                //check spread
                if (SpreadChance > 0.0f && SpreadRange > 0.0f)
                {
                    IPlayer[] players = world.GetPlayersAround(Entity.Pos.XYZ, SpreadRange, SpreadRange * 0.5f);
                    double squareSpread = SpreadRange * SpreadRange;

                    foreach (IPlayer player in players)
                    {
                        if (player.Entity == Entity) continue;
                        double squareDistance = player.Entity.Pos.SquareDistanceTo(Entity.Pos.XYZ);

                        float chance = (float)(squareSpread * spreadInterval * (1.0f - squareDistance / squareSpread)) * Intensity * SpreadChance;
                        if (Trigger != null && Trigger.ShouldTrigger(player.Entity, chance))
                        {
                            AffectedEntityBehavior affected = player.Entity.GetBehavior<AffectedEntityBehavior>();
                            if (affected == null) continue;
                            Effect effect = Trigger.ToTrigger.CreateEffect();
                            DiseaseEffect disease = (effect as DiseaseEffect);
                            if (disease != null) disease.Trigger = Trigger;
                            effect.Update(Trigger.ScaledIntensity((float)Entity.World.Rand.NextDouble()));
                            affected.AddEffect(effect);
                            affected.MarkDirty();
                        }
                    }
                }
            }
        }
    }//!class DiseaseEffect
}//!namespace XLib.XEffects
