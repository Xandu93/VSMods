using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XLib.XEffects
{
    /// <summary>
    /// Represents the curative properties of an item.
    /// </summary>
    public class CureProps
    {
        /// <summary>
        /// The domain name of this cure.
        /// </summary>
        public string domain;

        /// <summary>
        /// The effect name that is cured by the item.
        /// Can also be an effect category
        /// </summary>
        public string effect;

        /// <summary>
        /// The effect that is cured by the item.
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// The duration the effect is reduced of when the item is consumed.
        /// </summary>
        public float duration;

        /// <summary>
        /// The minimum duration the effect can be reduced to from this item.
        /// </summary>
        public float minduration;

        /// <summary>
        /// The amount of the intensity change when the item is consumed.
        /// </summary>
        public float intensity;

        /// <summary>
        /// The minimum intensity the effect can be reduced to from this item.
        /// </summary>
        public float minintensity;

        /// <summary>
        /// The amount of immunity growth this item will provide.
        /// </summary>
        public float healinggrowth;

        /// <summary>
        /// The maximum immunity growth you can get from this item.
        /// </summary>
        public float maxhealinggrowth;

        /// <summary>
        /// The healingrate you can get from this item.
        /// </summary>
        public float healingrate;

        /// <summary>
        /// The maximum healingrate you can get from this item.
        /// </summary>
        public float maxhealingrate;

        /// <summary>
        /// Ingredients can not be eaten directly.
        /// </summary>
        public bool ingredient;
    }

    /// <summary>
    /// Represents the trigger properties of an item.
    /// </summary>
    public class TriggerProps
    {
        /// <summary>
        /// The domain name of this cure.
        /// </summary>
        public string domain;

        /// <summary>
        /// The name of the effect that can be triggered.
        /// </summary>
        public string effect;

        /// <summary>
        /// The effect that is triggered by the item.
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// The chance to trigger the effect.
        /// </summary>
        public float chance;

        /// <summary>
        /// Influences the chance by the perish state of the item.
        /// </summary>
        public float perishweight;

        /// <summary>
        /// The intensity of the triggered effect.
        /// </summary>
        public float intensity;

        /// <summary>
        /// The maximal random intensity value that will be added to the default intensity
        /// when the effect is triggered.
        /// </summary>
        public float randomIntensity;

        /// <summary>
        /// The maximal intensity value that will be added to the default intensity
        /// when the effect is triggered depending on the transition state of the 
        /// consumed item.
        /// </summary>
        public float perishIntensity;

        /// <summary>
        /// The duration of the triggered effect.
        /// </summary>
        public float duration;

        /// <summary>
        /// The maximal random duration value that will be added to the default duration
        /// when the effect is triggered.
        /// </summary>
        public float randomDuration;

        /// <summary>
        /// The maximal duration value that will be added to the default duration
        /// when the effect is triggered depending on the transition state of the 
        /// consumed item.
        /// </summary>
        public float perishDuration;
    }

    /// <summary>
    /// Allows to consume items and blocks to get effects or cure effects.
    /// </summary>
    public class ConsumableBehavior : CollectibleBehavior
    {
        /// <summary>
        /// The effects that are cured by this consumable.
        /// </summary>
        private List<CureProps> cures;

        /// <summary>
        /// The effects that are triggered by this consumable.
        /// </summary>
        private List<TriggerProps> triggers;

        /// <summary>
        /// The number of unlearn points you will receive from this consumable.
        /// </summary>
        public float UnlearnPoints;

        /// <summary>
        /// The item must be used the given time in seconds to be triggered.
        /// </summary>
        public float UseTime;

        /// <summary>
        /// The portion that is usually consumed.
        /// </summary>
        public float Consumes;

        /// <summary>
        /// Should this item be consumed?
        /// </summary>
        public bool ShouldConsume;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumableBehavior"/> class.
        /// </summary>
        /// <param name="collObj"></param>
        public ConsumableBehavior(CollectibleObject collObj) : base(collObj)
        {
            cures = new List<CureProps>();
            triggers = new List<TriggerProps>();
        }

        /// <summary>
        /// Tries to patch the type.
        /// </summary>
        /// <param name="type">The type that should be patched.</param>
        private static bool TryPatch(Type type)
        {
            bool success = true;
            BindingFlags flags = BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;
            Harmony harmony = XEffectsSystem.Harmony;
            success &= TryPatchMethod(type, "OnHeldInteractStart", harmony, flags);
            success &= TryPatchMethod(type, "OnHeldInteractStep", harmony, flags);
            success &= TryPatchMethod(type, "OnHeldInteractStop", harmony, flags);
            return success;
        }

        /// <summary>
        /// Tries to patch a method.
        /// </summary>
        /// <param name="type">The type that should be patched.</param>
        /// <param name="methodName">The name of the method that should be patched.</param>
        /// <param name="harmony">The harmony api.</param>
        /// <param name="flags">The binding flags of the method.</param>
        private static bool TryPatchMethod(Type type, string methodName, Harmony harmony, BindingFlags flags)
        {
            MethodInfo original;
            Type baseType = type;
            do
            {
                original = baseType.GetMethod(methodName, flags);
                baseType = baseType.BaseType;
            } while (baseType != null && original == null);
            if (original == null) return false;

            //make sure to not patch the collectible multiple times
            if (XEffectsSystem.Harmony.GetPatchedMethods().FirstOrDefault((MethodBase method) =>
            { return method == original; }) != null) return false;

            Type patch = typeof(ConsumablePatch);
            MethodInfo patchMethod = patch.GetMethod(methodName + "Prefix", flags);
            try
            {
                harmony.Patch(original, new HarmonyMethod(patchMethod));
            }
            catch
            {
                try
                {
                    patchMethod = patch.GetMethod(methodName + "Prefix2", flags);
                    harmony.Patch(original, new HarmonyMethod(patchMethod));
                }
                catch { return false; }
            }
            return true;
        }

        /// <summary>
        /// Called right after the block behavior was created, must call base method
        /// </summary>
        /// <param name="properties"></param>
        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);

            JsonObject[] cures = properties["cures"].AsArray();
            JsonObject[] triggers = properties["triggers"].AsArray();
            UnlearnPoints = properties["unlearnPoints"].AsFloat(0.0f);
            UseTime = properties["useTime"].AsFloat(0.95f);
            Consumes = properties["consumes"].AsFloat(1.0f);
            ShouldConsume = properties["shouldConsume"].AsBool(false);
            if (properties["usePatch"].AsBool(false)) TryPatch(collObj.GetType());

            if (cures != null)
            {
                foreach (JsonObject cure in cures)
                {
                    try
                    {
                        CureProps cureProps = new();
                        cureProps.effect = cure["effect"].AsString();
                        cureProps.duration = cure["duration"]?.AsFloat() ?? 0.0f;
                        cureProps.minduration = cure["minduration"]?.AsFloat() ?? 0.0f;
                        cureProps.intensity = cure["intensity"]?.AsFloat() ?? 0.0f;
                        cureProps.minintensity = cure["minintensity"]?.AsFloat() ?? 0.0f;
                        cureProps.healinggrowth = cure["healinggrowth"]?.AsFloat() ?? 0.0f;
                        cureProps.maxhealinggrowth = cure["maxhealinggrowth"]?.AsFloat() ?? 0.0f;
                        cureProps.healingrate = cure["healingrate"]?.AsFloat() ?? 0.0f;
                        cureProps.maxhealingrate = cure["maxhealingrate"]?.AsFloat() ?? 0.0f;
                        cureProps.ingredient = cure["ingredient"]?.AsBool(false) ?? false;

                        string[] strings = cureProps.effect.Split(':');
                        if(strings.Length == 2)
                        {
                            cureProps.effect = strings[1];
                            cureProps.domain = strings[0];
                        }
                        this.cures.Add(cureProps);
                    }
                    catch { }
                }
            }
            if (triggers != null)
            {
                foreach (JsonObject trigger in triggers)
                {
                    try
                    {
                        TriggerProps triggerProps = new();
                        triggerProps.effect = trigger["effect"].AsString();
                        triggerProps.chance = trigger["chance"]?.AsFloat() ?? 0.0f;
                        triggerProps.perishweight = trigger["perishweight"]?.AsFloat() ?? 0.0f;
                        triggerProps.intensity = trigger["intensity"]?.AsFloat() ?? 0.0f;
                        triggerProps.randomIntensity = trigger["randomintensity"]?.AsFloat() ?? 0.0f;
                        triggerProps.perishIntensity = trigger["perishintensity"]?.AsFloat() ?? 0.0f;
                        triggerProps.duration = trigger["duration"]?.AsFloat() ?? 0.0f;
                        triggerProps.randomDuration = trigger["randomduration"]?.AsFloat() ?? 0.0f;
                        triggerProps.perishDuration = trigger["perishduration"]?.AsFloat() ?? 0.0f;

                        string[] strings = triggerProps.effect.Split(':');
                        if (strings.Length == 2)
                        {
                            triggerProps.effect = strings[1];
                            triggerProps.domain = strings[0];
                        }
                        this.triggers.Add(triggerProps);
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Calculates the total unlearn points. Also considers contents of containers.
        /// </summary>
        /// <returns></returns>
        /// <param name="world">The world.</param>
        /// <param name="stack">The stack.</param>
        /// <returns></returns>
        public float GetUnlearnPoints(IWorldAccessor world, ItemStack stack)
        {
            float unlearn = UnlearnPoints;
            if (world == null || stack == null) return unlearn;
            if (collObj is BlockContainer container)
            {
                ItemStack[] contentStacks = container.GetContents(world, stack);
                if (contentStacks != null)
                {
                    foreach (ItemStack content in contentStacks)
                    {
                        if (content == null) continue;
                        ConsumableBehavior beh = content.Collectible.GetCollectibleBehavior<ConsumableBehavior>(false);
                        if (beh != null) unlearn += beh.UnlearnPoints;
                    }
                }
            }
            return unlearn;
        }

        /// <summary>
        /// Server Side: Called once the collectible has been registered 
        /// Client Side: Called once the collectible has been loaded from server packet
        /// </summary>
        /// <param name="api"></param>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            XEffectsSystem system = api.ModLoader.GetModSystem<XEffectsSystem>();
            foreach (CureProps cure in cures)
            {
                cure.effectType = system.EffectType(cure.effect);
            }
            foreach (TriggerProps trigger in triggers)
            {
                trigger.effectType = system.EffectType(trigger.effect);
            }
        }

        /// <summary>
        /// Called when the player right clicks while holding this block/item in his hands.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent"></param>
        /// <param name="handHandling"></param>
        /// <param name="handling"></param>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            //maybe add sound and animation
            if (handHandling == EnumHandHandling.NotHandled)
                handHandling = EnumHandHandling.Handled;
        }

        /// <summary>
        /// Called every frame while the player is using this collectible. Return false to stop the interaction.
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="handling"></param>
        /// <returns></returns>
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client)
                return secondsUsed <= this.UseTime + 0.05f;
            return false;
        }

        /// <summary>
        /// Called when the player successfully completed the using action
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="handling"></param>
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            if (byEntity.Api.Side == EnumAppSide.Client) return;
            if (secondsUsed < UseTime) return;

            float servings;
            if (collObj is BlockLiquidContainerBase container)
            {
                float litresEach = container.GetCurrentLitres(slot.Itemstack);
                float litresTotal = litresEach * slot.StackSize;
                float drinkCapLitres = Consumes;

                servings = Math.Min(drinkCapLitres, litresTotal);
            }
            else servings = (slot.Itemstack as IBlockMealContainer)?.GetQuantityServings(byEntity.Api.World, slot.Itemstack) ?? 1.0f;

            //unlearn
            float unlearn = GetUnlearnPoints(byEntity.World, slot.Itemstack);
            if (unlearn > 0.0f)
            {
                unlearn *= servings;
                PlayerSkillSet playerSkillSet = byEntity.GetBehavior<PlayerSkillSet>();
                if (playerSkillSet != null)
                {
                    playerSkillSet.UnlearnPoints = Math.Min(playerSkillSet.UnlearnPoints + unlearn, 10.0f);
                    CommandPackage package = new CommandPackage(EnumXLevelingCommand.UnlearnPoints, playerSkillSet.UnlearnPoints);
                    (byEntity.Api.Network.GetChannel("XLeveling") as IServerNetworkChannel)?.SendPacket(package, playerSkillSet.Player as IServerPlayer);
                }
            }

            XEffectsSystem system = byEntity.Api.ModLoader.GetModSystem<XEffectsSystem>();
            AffectedEntityBehavior affected = byEntity.GetBehavior<AffectedEntityBehavior>();
            if (system == null || affected == null) return;

            //effect trigger
            foreach (TriggerProps trigger in triggers)
            {
                float chance = GetTriggerChance(trigger, slot, byEntity.World, out float perish);
                if (chance >= byEntity.World.Rand.NextDouble())
                {
                    Effect effect = system.CreateEffect(trigger.effect);
                    if (effect == null) continue;

                    float intensity = 
                        trigger.intensity + 
                        trigger.randomIntensity * (float)byEntity.World.Rand.NextDouble() +
                        trigger.perishIntensity * perish;
                    float duration =
                        trigger.duration +
                        trigger.randomDuration * (float)byEntity.World.Rand.NextDouble() +
                        trigger.perishDuration * perish;

                    if (intensity != 0.0f) effect.Update(intensity);
                    if (duration > 0.0f) effect.Duration = duration;
                    affected.AddEffect(effect);
                    break;
                }
            }

            //cures
            foreach (CureProps cure in GetCureProps(slot.Itemstack, byEntity.Api))
            {
                if (cure.ingredient) continue;
                bool used = false;
                Effect effect = affected.Effect(cure.effect);
                if (effect != null) used = effect.OnCured(cure, servings);

                if (!used)
                {
                    foreach (Effect effect2 in affected.Effects.Values)
                    {
                        if (effect2.EffectType.EffectCategory == cure.effect)
                        {
                            used = effect2.OnCured(cure, servings);
                            if (used) break;
                        }
                    }
                }
                if (used) affected.MarkDirty();
            }

            if (ShouldConsume)
            {
                int consumed = (int)Consumes;
                slot.TakeOut(consumed);
                slot.MarkDirty();
                (byEntity as EntityPlayer)?.Player.InventoryManager.BroadcastHotbarSlot();
            }
        }

        /// <summary>
        /// Adds cure description.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="dsc"></param>
        /// <param name="world"></param>
        /// <param name="withDebugInfo"></param>
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            //unlearn points
            float unlearn = GetUnlearnPoints(world, inSlot.Itemstack);
            if (unlearn > 0.00f) dsc.Append("\n" + Lang.Get("xlib:you receive") + " " + unlearn.ToString("N2") + " " + Lang.Get("xlib:unlearnpoints") + "\n");
            List<CureProps> cures = GetCureProps(inSlot.Itemstack, world.Api);

            //cures
            if (cures.Count > 0)
            {
                bool ingredient = true;
                dsc.Append("\n<font color=\"green\">" + Lang.Get("xeffects:cures") + "</font>: ");
                foreach (CureProps cure in cures)
                {
                    dsc.Append("\n\t");
                    ingredient &= cure.ingredient;
                    if (cure.effectType != null) dsc.Append(Lang.Get(cure.effectType.Domain + ':' + cure.effectType.Name + "-effect"));
                    else if (cure.domain != null) dsc.Append(Lang.Get(cure.domain + ':' + cure.effect));
                    else dsc.Append(Lang.Get(cure.effect));
                    if (ingredient) dsc.Append(string.Format(" ({0})", Lang.Get("xeffects:ingredient")));

                    if (cure.duration != 0.0f || cure.minduration != 0.0f)
                    {
                        dsc.Append(string.Format("\n\t\t{0}: {1:0.##} » {2:0.##}", Lang.Get("xeffects:duration"), cure.duration, cure.minduration));
                    }
                    if (cure.intensity != 0.0f || cure.minintensity != 0.0f )
                    {
                        dsc.Append(string.Format("\n\t\t{0}: {1:0.###} » {2:0.###}", Lang.Get("xeffects:intensity"), cure.intensity, cure.minintensity));
                    }
                    if (cure.healingrate != 0.0f || cure.maxhealingrate != 0.0f)
                    {
                        dsc.Append(string.Format("\n\t\t{0}: {1:0.#####} » {2:0.#####}", Lang.Get("xeffects:healingrate"), cure.healingrate * 60.0f, cure.maxhealingrate * 60.0f));
                    }
                    if (cure.healinggrowth != 0.0f || cure.maxhealinggrowth != 0.0f)
                    {
                        dsc.Append(string.Format("\n\t\t{0}: {1:0.######} » {2:0.######}", Lang.Get("xeffects:healinggrowth"), cure.healinggrowth * 60.0f, cure.maxhealinggrowth * 60.0f));
                    }
                }
                dsc.Append('\n');
            }

            IPlayer player = (world as IClientWorldAccessor)?.Player;
            if (player == null) return;
            if (player.WorldData.CurrentGameMode != EnumGameMode.Creative) return;
            if (triggers.Count > 0)
            {
                dsc.Append("\n<font color=\"red\"> triggers</font>:");
                foreach (TriggerProps trigger in triggers)
                {
                    float chance = GetTriggerChance(trigger, inSlot, world, out float perish);
                    dsc.Append("\n\t");
                    if (trigger.effectType != null) dsc.Append(Lang.Get(trigger.effectType.Domain + ':' + trigger.effectType.Name + "-effect"));
                    else if (trigger.domain != null) dsc.Append(Lang.Get(trigger.domain + ':' + trigger.effect));
                    else dsc.Append(Lang.Get(trigger.effect));

                    if (trigger.duration + trigger.randomDuration + trigger.perishDuration != 0.0f)
                    {
                        dsc.Append(string.Format(
                            "\n\t\t{0}: {1:0.##} + rand({2:0.##}) + {3:0.##} * {4:0.##}", 
                            Lang.Get("xeffects:duration"), trigger.duration, 
                            trigger.randomDuration, trigger.perishDuration, perish));
                    }
                    if (trigger.intensity + trigger.randomIntensity + trigger.perishIntensity != 0.0f)
                    {
                        dsc.Append(string.Format(
                            "\n\t\t{0}: {1:0.##} + rand({2:0.##}) + {3:0.##} * {4:0.##}", 
                            Lang.Get("xeffects:intensity"), trigger.intensity,
                            trigger.randomIntensity, trigger.perishIntensity, perish));
                    }
                    if (trigger.chance + trigger.perishweight != 0.0f)
                    {
                        dsc.Append(string.Format(
                            "\n\t\t{0}: {1:0.##} + {2:0.##} * {3:0.##}", 
                            Lang.Get("xeffects:chance"), trigger.chance, 
                            trigger.perishweight, perish));
                    }
                }
                dsc.Append('\n');
            }
        }

        /// <summary>
        /// Gets the cure props.
        /// Also considers ingredients and contents.
        /// </summary>
        /// <param name="itemStack">The item stack.</param>
        /// <param name="api">The api.</param>
        /// <returns>the cure props</returns>
        protected List<CureProps> GetCureProps(ItemStack itemStack, ICoreAPI api)
        {
            if (itemStack == null) return this.cures;
            StringArrayAttribute ingredients = itemStack.Attributes["madeWith"] as StringArrayAttribute;
            TreeAttribute contents = itemStack.Attributes["contents"] as TreeAttribute;
            if (ingredients == null && contents == null) return this.cures;
            List<CureProps> cures = new List<CureProps>(this.cures);

            if (ingredients != null)
            {
                Dictionary<string, List<CureProps>> curesDic = new Dictionary<string, List<CureProps>>();
                foreach (string code in ingredients.value)
                {
                    CollectibleObject collectible = api.World.GetItem(new AssetLocation(code));
                    collectible ??= api.World.GetBlock(new AssetLocation(code));
                    List<CureProps> temp = collectible?.GetCollectibleBehavior<ConsumableBehavior>(false)?.GetCureProps(null, api);
                    if (temp == null) continue;

                    foreach (CureProps cureProp in temp)
                    {
                        if (!curesDic.TryGetValue(cureProp.effect, out List<CureProps> value))
                        {
                            value = new List<CureProps>();
                            curesDic.Add(cureProp.effect, value);
                        }
                        value.Add(cureProp);
                    }
                }

                foreach (List<CureProps> value in curesDic.Values)
                {
                    if (value.Count < 2) continue;
                    CureProps result = new CureProps();
                    foreach (CureProps props in value)
                    {
                        result.ingredient = false;
                        result.domain = props.domain;
                        result.effect = props.effect;
                        result.effectType ??= props.effectType;
                        result.duration = Math.Max(result.duration, props.duration);
                        result.minduration =
                            result.minduration == 0 ? props.minduration :
                            props.minduration == 0 ? result.minduration :
                            Math.Min(result.minduration, props.minduration);
                        result.intensity += props.intensity;
                        result.minintensity =
                            result.minintensity == 0 ? props.minintensity :
                            props.minintensity == 0 ? result.minintensity :
                            Math.Min(result.minintensity, props.minintensity);
                        result.healinggrowth += props.healinggrowth;
                        result.maxhealinggrowth =
                            result.maxhealinggrowth == 0 ? props.maxhealinggrowth :
                            props.maxhealinggrowth == 0 ? result.maxhealinggrowth :
                            Math.Max(result.maxhealinggrowth, props.maxhealinggrowth);
                        result.healingrate += props.healingrate;
                        result.maxhealingrate =
                            result.maxhealingrate == 0 ? props.maxhealingrate :
                            props.maxhealingrate == 0 ? result.maxhealingrate :
                            Math.Max(result.maxhealingrate, props.maxhealingrate);
                    }
                    cures.Add(result);
                }
            }
            if (contents != null)
            {
                foreach (IAttribute content in contents.Values)
                {
                    ItemStack stack = (content as ItemstackAttribute)?.value;
                    List<CureProps> temp = stack?.Collectible.GetCollectibleBehavior<ConsumableBehavior>(false)?.GetCureProps(stack, api);
                    if (temp == null) continue;
                    cures.AddRange(temp);
                }
            }
            return cures;
        }

        /// <summary>
        /// Gets the trigger chance.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="world">The world.</param>
        /// <param name="perish">The perish.</param>
        /// <returns>the trigger chance</returns>
        protected float GetTriggerChance(TriggerProps trigger, ItemSlot slot, IWorldAccessor world, out float perish)
        {
            float chance = trigger.chance;
            perish = 0.0f;
            if (trigger.perishweight > 0.0f)
            {
                TransitionState state = this.collObj.UpdateAndGetTransitionState(world, slot, EnumTransitionType.Perish);
                if (state == null)
                {
                    ItemStack[] stacks = (this.collObj as BlockContainer)?.GetContents(world, slot.Itemstack);
                    if (stacks != null) 
                    {
                        for (int i = 0; i < stacks.Length; i++)
                        {
                            if (stacks[i] != null)
                            {
                                state = stacks[i].Collectible.UpdateAndGetTransitionState(world, new DummySlot(stacks[i]), EnumTransitionType.Perish);
                                if (state != null)
                                {
                                    perish = Math.Max(perish, state.TransitionLevel);
                                }
                            }
                        }
                    }
                }
                else
                {
                    perish = state.TransitionLevel;
                }
                chance += trigger.perishweight * perish;
            }
            return chance;
        }

    }//!class EffectDisease

    /// <summary>
    /// Patches collectibles that don't have collectibile behaviors implemented.
    /// </summary>
    internal class ConsumablePatch
    {
        /// <summary>
        /// Prefix for the OnHeldInteractStart method.
        /// </summary>
        public static bool OnHeldInteractStartPrefix(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            EnumHandHandling bhHandHandling = EnumHandHandling.NotHandled;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in __instance.CollectibleBehaviors)
            {
                EnumHandling bhHandling = EnumHandling.PassThrough;

                behavior.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref bhHandHandling, ref bhHandling);
                if (bhHandling != EnumHandling.PassThrough)
                {
                    handling = bhHandHandling;
                    preventDefault = true;
                }
                if (bhHandling == EnumHandling.PreventSubsequent) return !preventDefault;
            }
            return !preventDefault;
        }

        /// <summary>
        /// Same as OnHeldInteractStartPrefix but it's named handHandling instead of handling. 
        /// </summary>
        public static bool OnHeldInteractStartPrefix2(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
        {
            return OnHeldInteractStartPrefix(__instance, slot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
        }

        /// <summary>
        /// Prefix for the OnHeldInteractStep method.
        /// </summary>
        public static bool OnHeldInteractStepPrefix(CollectibleObject __instance, ref bool __result, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            __result = true;
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in __instance.CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                bool behaviorResult = behavior.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
                if (handled != EnumHandling.PassThrough)
                {
                    __result &= behaviorResult;
                    preventDefault = true;
                }

                if (handled == EnumHandling.PreventSubsequent) return !preventDefault;
            }
            return !preventDefault;
        }

        /// <summary>
        /// Prefix for the OnHeldInteractStop method.
        /// </summary>
        public static bool OnHeldInteractStopPrefix(CollectibleObject __instance, float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            bool preventDefault = false;

            foreach (CollectibleBehavior behavior in __instance.CollectibleBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handled);
                if (handled != EnumHandling.PassThrough) preventDefault = true;

                if (handled == EnumHandling.PreventSubsequent) return !preventDefault;
            }

            return !preventDefault;
        }
    }//!class ConsumablePatch
}//!namespace XLib.XEffects