using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace XLib.XEffects
{
    /// <summary>
    /// The main interface for the effect system.
    /// </summary>
    /// <seealso cref="ModSystem" />
    public class XEffectsSystem : ModSystem
    {
        /// <summary>
        /// the asset category
        /// </summary>
        public AssetCategory EffectsAssetCategory { get; private set; }

        /// <summary>
        /// the asset category
        /// </summary>
        public AssetCategory EffectTriggerAssetCategory { get; private set; }

        /// <summary>
        /// If you need mods to be executed in a certain order, adjust this methods return value.
        /// The server will call each Mods Start() method the ascending order of each mods execute order value. And thus, as long as every mod registers it's event handlers in the Start() method, all event handlers will be called in the same execution order.
        /// Default execute order of some survival mod parts
        /// Worldgen:
        /// - GenTerra: 0
        /// - RockStrata: 0.1
        /// - Deposits: 0.2
        /// - Caves: 0.3
        /// - Blocklayers: 0.4
        /// Asset Loading
        /// - Json Overrides loader: 0.05
        /// - Load hardcoded mantle block: 0.1
        /// - Block and Item Loader: 0.2
        /// - Recipes (Smithing, Knapping, Clayforming, Grid recipes, Alloys) Loader: 1
        /// </summary>
        /// <returns></returns>
        public override double ExecuteOrder() => 0.15;

        /// <summary>
        /// The configuration.
        /// </summary>
        public XEffectsConfig Config { get; private set; }

        /// <summary>
        /// The harmony api
        /// </summary>
        internal static Harmony Harmony { get; private set; }

        /// <summary>
        /// The vs core API
        /// </summary>
        public ICoreAPI Api { get; private set; }

        /// <summary>
        /// Gets the effect types.
        /// </summary>
        /// <value>
        /// The effect types.
        /// </value>
        public Dictionary<string, EffectType> EffectTypes { get; private set; }

        /// <summary>
        /// Gets the trigger.
        /// </summary>
        /// <value>
        /// The trigger.
        /// </value>
        public Dictionary<string, List<EffectTrigger>> Trigger { get; private set; }

        /// <summary>
        /// Gets the entity trigger.
        /// </summary>
        /// <value>
        /// The trigger.
        /// </value>
        public Dictionary<string, List<EntityTrigger>> EntityTrigger { get; private set; }

        /// <summary>
        /// The effect frame
        /// </summary>
        EffectFrame effectFrame;

        /// <summary>
        /// Applies the harmony patches.
        /// </summary>
        private static void DoHarmonyPatch(ICoreAPI api)
        {
            if (Harmony == null)
            {
                try
                {
                    Harmony = new Harmony("XEffectsPatch");
                    Harmony.PatchAll(Assembly.GetExecutingAssembly());
                }
                catch(Exception e)
                {
                    api.Logger.Error(e);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XEffectsSystem"/> class.
        /// </summary>
        public XEffectsSystem() : base()
        {
            this.EffectTypes = new Dictionary<string, EffectType>();
        }

        /// <summary>
        /// Called during initial mod loading, called before any mod receives the call to Start()
        /// </summary>
        /// <param name="api"></param>
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            this.Api = api;
            EffectsAssetCategory ??= new AssetCategory("effects", true, EnumAppSide.Universal);
            EffectTriggerAssetCategory ??= new AssetCategory("effecttrigger", true, EnumAppSide.Server);

            this.LoadConfig();
            this.Api.RegisterEntityBehaviorClass("Affected", typeof(AffectedEntityBehavior));
            this.Api.RegisterEntityBehaviorClass("Infectious", typeof(InfectiousEntityBehavior));
            this.Api.RegisterCollectibleBehaviorClass("XLibConsumable", typeof(ConsumableBehavior));
            this.RegisterDefaults();
        }

        /// <summary>
        /// Side agnostic Start method, called after all mods received a call to StartPre().
        /// </summary>
        /// <param name="api"></param>
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            DoHarmonyPatch(api);

            this.EntityTrigger = new Dictionary<string, List<EntityTrigger>>();
            this.Trigger = new Dictionary<string, List<EffectTrigger>>();
            this.Trigger["damage"] = new List<EffectTrigger>();
            this.Trigger["attribute"] = new List<EffectTrigger>();
        }

        /// <summary>
        /// Minor convenience method to save yourself the check for/cast to ICoreServerAPI in Start()
        /// </summary>
        /// <param name="api"></param>
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            api.Event.GameWorldSave += OnGameWorldSave;
            api.Event.PlayerNowPlaying += OnPlayerJoined;

            //register chat commands
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;
            api.ChatCommands.Create()
                .WithName("effect")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Modifies effects of players.")
                .BeginSubCommand("clear")
                .WithDescription("Clears all effects of a player.")
                .HandleWith(OnClearEffectCommand)
                .WithArgs(new ICommandArgumentParser[] {
                    parsers.OptionalWord("player"),
                })
                .EndSubCommand()

                .BeginSubCommand("add")
                .WithDescription("Adds an effect to a player.")
                .HandleWith (OnAddEffectCommand)
                .WithArgs(new ICommandArgumentParser[]{
                    parsers.OnlinePlayer("player"),
                    parsers.Word("effect"),
                    parsers.Float("intensity"),
                    parsers.Float("duration")
                })
                .EndSubCommand()

                .BeginSubCommand("remove")
                .WithDescription("Removes an effect from a player.")
                .HandleWith(OnRemoveEffectCommand)
                .WithArgs(new ICommandArgumentParser[]{
                    parsers.OnlinePlayer("player"),
                    parsers.Word("effect")
                })
                .EndSubCommand();
        }

        /// <summary>
        /// Loads the configuration from a file.
        /// </summary>
        public void LoadConfig()
        {
            string path = "xeffects.json";

            try
            {
                Config = Api.LoadModConfig<XEffectsConfig>(path);
            }
            catch (Exception)
            {
                Config = null;
            }
            Config ??= new XEffectsConfig();
            Api.StoreModConfig(Config, path);
        }

        /// <summary>
        /// Unpatches harmony patches.
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            Harmony?.UnpatchAll("XEffectsPatch");
            Harmony = null;
        }

        /// <summary>
        /// Called on the server or the client; implementing code may need to check which side it is.
        /// <br />On a server, called only after all mods have called Start(), and after asset JSONs have been read from disk and patched, but before runphase ModsAndConfigReady.
        /// <br />Asset files are now available to load using api.Assets.TryGet() calls or similar.  It is not guaranteed that the actual in-game assets (including blocks and items) are yet registered!
        /// <br />If called from a modsystem, what has been registered at this stage depends on the ExecuteOrder().  After 0.2, blocks and items have been registered.  After 0.6, recipes have been registered.
        /// <br />If implementing this, and if your code requires that blocks, items and entities have been registered first, make sure your ModSystem has set an appropriate ExecuteOrder()!!
        /// </summary>
        /// <param name="api"></param>
        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);
            this.Mod.Logger.Event("Initialize effects");
            this.LoadJsonEffects();

            int triggerCount = 0;
            foreach (List<EffectTrigger> triggers in Trigger.Values) triggerCount += triggers.Count;
            this.Mod.Logger.Event("Registered {0} effects and {1} effect triggers.", EffectTypes.Count, triggerCount);

            foreach (EffectType effect in this.EffectTypes.Values)
            {
                effect.DisplayName = Lang.GetUnformatted(effect.DisplayName);
                effect.Description = Lang.GetUnformatted(effect.Description);
            }
        }

        /// <summary>
        /// Called when a player joined.
        /// </summary>
        /// <param name="byPlayer">The by player.</param>
        private void OnPlayerJoined(IServerPlayer byPlayer)
        {
            if (byPlayer?.Entity == null) return;

            //clean up stats
            IEnumerator<KeyValuePair<string, EntityFloatStats>> stats = byPlayer.Entity.Stats.GetEnumerator();
            while (stats.MoveNext())
            {
                List<string> removeList = new List<string>();
                foreach (string statCode in stats.Current.Value.ValuesByKey.Keys)
                {
                    if (statCode.StartsWith("effect-")) removeList.Add(statCode);
                }
                foreach (string toRemove in removeList)
                {
                    stats.Current.Value.Remove(toRemove);
                }
            }

            //load effects
            AffectedEntityBehavior beh = byPlayer.Entity.GetBehavior<AffectedEntityBehavior>();
            if (beh == null) return;
            beh.CreateEffectsFromTree();
            beh.MarkDirty();
        }

        /// <summary>
        /// Minor convenience method to save yourself the check for/cast to ICoreClientAPI in Start()
        /// </summary>
        /// <param name="api"></param>
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            effectFrame = new EffectFrame(api);
            api.Event.RegisterGameTickListener((float tick) => this.effectFrame.Update(), 100);

            api.Input.RegisterHotKey("effectframehotkey", "Show/Hide Effect Hud", GlKeys.L, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("effectframehotkey", this.OnHotKeyEffectFrame);
        }

        /// <summary>
        /// Registers a effect type.
        /// </summary>
        /// <param name="effectType">The effect type.</param>
        public void RegisterEffectType(EffectType effectType)
        {
            if (effectType == null) return;
            this.EffectTypes.Add(effectType.Name, effectType);
            effectType.EffectsSystem = this;
        }

        /// <summary>
        /// Gets an effect type by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>a effects type by its name</returns>
        public EffectType EffectType(string name)
        {
            if (name == null) return null;
            EffectType effectType;
            this.EffectTypes.TryGetValue(name, out effectType);
            return effectType;
        }

        /// <summary>
        /// Creates an effect.
        /// </summary>
        /// <param name="name">The name of the effect type.</param>
        /// <returns></returns>
        public Effect CreateEffect(string name)
        {
            EffectType type = this.EffectType(name);
            if (type == null) return null;
            return type.CreateEffect();
        }

        /// <summary>
        /// Registers the default effect types.
        /// </summary>
        private void RegisterDefaults()
        {
            this.RegisterEffectType(new EffectType("stat", typeof(StatEffect)));
            this.RegisterEffectType(new EffectType("condition", typeof(Condition)));
            this.RegisterEffectType(new EffectType("momentum", typeof(MomentumEffect)));
            this.RegisterEffectType(new EffectType("dot", typeof(DotEffect)));
            this.RegisterEffectType(new EffectType("hot", typeof(HotEffect)));
            this.RegisterEffectType(new EffectType("receivedDamageMultiplier", typeof(ReceivedDamageMultiplierEffect)));
            this.RegisterEffectType(new EffectType("trigger", typeof(TriggerEffect)));
            this.RegisterEffectType(new EffectType("disease", typeof(DiseaseEffect)));
            this.RegisterEffectType(new EffectType("heated", typeof(HeatedEffect)));
            this.RegisterEffectType(new EffectType("nutrition", typeof(NutritionEffect)));
            this.RegisterEffectType(new EffectType("attribute", typeof(AttributeEffect)));
            this.RegisterEffectType(new EffectType("animation", typeof(AnimationEffect)));
            this.RegisterEffectType(new EffectType("shader", typeof(ShaderEffect)));
        }

        /// <summary>
        /// Finds a trigger that triggers a specific effect type.
        /// </summary>
        /// <param name="effectType">The effect type.</param>
        /// <param name="type">The name of the type.</param>
        /// <returns></returns>
        public EffectTrigger FindTrigger(EffectType effectType, string type)
        {
            List<EffectTrigger> triggers = Trigger[type];
            if (triggers == null) return null;
            foreach (EffectTrigger trigger in triggers)
            {
                if (trigger.ToTrigger == effectType) return trigger;
            }
            return null;
        }

        /// <summary>
        /// Loads the json effects.
        /// </summary>
        private void LoadJsonEffects()
        {
            //effects
            Dictionary<AssetLocation, JToken> tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "effects");
            if (tokens.Count == 0)
            {
                Api.Assets.Reload(EffectsAssetCategory);
                tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "effects");
            }
            Dictionary<AssetLocation, JToken>.Enumerator enumerator = tokens.GetEnumerator();

            while (enumerator.MoveNext())
            {
                TreeAttribute tree = new JsonObject(enumerator.Current.Value).ToAttribute() as TreeAttribute;
                IEnumerator<KeyValuePair<string, IAttribute>> enumerator2 = tree.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    TreeAttribute attributes = enumerator2.Current.Value as TreeAttribute;
                    if (attributes == null) continue;
                    EffectType baseType = this.EffectType(attributes.GetString("code"));
                    if (baseType == null) continue;

                    IEnumerator<KeyValuePair<string, IAttribute>> enumerator3 = baseType.Defaults.GetEnumerator();
                    TreeAttribute defaultTree = attributes.GetTreeAttribute("defaults") as TreeAttribute ?? new TreeAttribute();
                    while (enumerator3.MoveNext())
                    {
                        if (!defaultTree.HasAttribute(enumerator3.Current.Key)) defaultTree.SetAttribute(enumerator3.Current.Key, enumerator3.Current.Value);
                    }

                    EffectType effectType = new EffectType(
                        enumerator2.Current.Key, baseType.Type,
                        defaultTree, null,
                        enumerator.Current.Key.Domain,
                        attributes.GetString("icon"),
                        attributes.GetString("group"),
                        attributes.GetString("category"));
                    this.RegisterEffectType(effectType);
                }
            }

            //trigger
            tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "effecttrigger");
            if (tokens.Count == 0)
            {
                Api.Assets.Reload(EffectTriggerAssetCategory);
                tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "effecttrigger");
            }
            enumerator = tokens.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.Key.RemoveEnding();
                TreeArrayAttribute trees = new JsonObject(enumerator.Current.Value).ToAttribute() as TreeArrayAttribute;
                if (trees == null) continue;
                foreach (TreeAttribute triggerTree in trees.value)
                {
                    string type = triggerTree.GetString("type");
                    if (type == null) continue;
                    List<EffectTrigger> triggerList;
                    this.Trigger.TryGetValue(type, out triggerList);
                    if (triggerList == null) continue;

                    EffectType effectType = this.EffectType(triggerTree.GetString("effect"));
                    ITreeAttribute attributes = triggerTree.GetTreeAttribute("attributes");
                    if (effectType == null || attributes == null) continue;
                    EffectTrigger trigger = null;

                    if (type == "damage") { trigger = new DamageTrigger(effectType); }
                    if (type == "attribute") { trigger = new AttributeTrigger(effectType); }

                    if (trigger != null)
                    {
                        trigger.FromTree(attributes);
                        triggerList.Add(trigger);
                    }
                }
                continue;
            }
        }

        /// <summary>
        /// Called when the game world was saved.
        /// </summary>
        private void OnGameWorldSave()
        {
            foreach(IPlayer player in Api.World.AllOnlinePlayers)
            {
                AffectedEntityBehavior affected = player?.Entity?.GetBehavior<AffectedEntityBehavior>();
                if (affected == null) continue;
                affected.UpdateTree();
            }
        }

        /// <summary>
        /// Called when effect frame hot key was pressed.
        /// </summary>
        /// <param name="comb">The comb.</param>
        /// <returns></returns>
        private bool OnHotKeyEffectFrame(KeyCombination comb)
        {
            if (this.effectFrame.IsOpened())
            {
                this.effectFrame.ForcedState = Math.Clamp(this.effectFrame.ForcedState -1, -1, 1);
            }
            else
            {
                this.effectFrame.ForcedState = Math.Clamp(this.effectFrame.ForcedState + 1, -1, 1);
            }
            ICoreClientAPI capi = this.Api as ICoreClientAPI;
            switch(this.effectFrame.ForcedState)
            {
                case -1: capi.ShowChatMessage("effect hud mode: never"); break;
                case  0: capi.ShowChatMessage("effect hud mode: dynamic"); break;
                case  1: capi.ShowChatMessage("effect hud mode: always"); break;
                default: capi.ShowChatMessage("effect hud mode: undefined"); break;
            }
            return true;
        }

        /// <summary>
        /// Called when the clear effect command was called.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TextCommandResult OnClearEffectCommand(TextCommandCallingArgs arguments)
        {
            string playerName = arguments[0] as string;
            IPlayer player = arguments.Caller.Player;
            if (playerName != null)
            {
                player = null;
                Array.Find(Api.World.AllPlayers, (IPlayer player2) => {
                    if (player2?.PlayerName == playerName)
                    {
                        player = player2;
                        return true;
                    }
                    return false;
                });
            }
            if (player == null)
            {
                string msg = "Can't find the player " + playerName + ".";
                return TextCommandResult.Error(msg, msg);
            }

            AffectedEntityBehavior affected = player.Entity.GetBehavior<AffectedEntityBehavior>();
            if (affected == null) return null;
            affected.Clear();
            return TextCommandResult.Success("Cleared all effects of the player " + player.PlayerName + ".");
        }

        /// <summary>
        /// Called when the add effect command was called.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TextCommandResult OnAddEffectCommand(TextCommandCallingArgs arguments)
        {
            IPlayer player = arguments[0] as IPlayer;
            string effectName = arguments[1] as string;
            float intensity = (float)arguments[2];
            float duration = (float)arguments[3];
            AffectedEntityBehavior affected = player.Entity.GetBehavior<AffectedEntityBehavior>();
            if (affected == null)
            {
                string msg = player.PlayerName + " has no affected behavior.";
                return TextCommandResult.Error(msg, msg);
            }

            Effect effect = CreateEffect(effectName);
            if (effect == null)
            {
                string msg = string.Format("Effect {0} could not be created. Maybe the name is wrong.", effectName);
                return TextCommandResult.Error(msg, msg);
            }
            else
            {
                effect.Update(intensity);
                effect.Duration = duration;
                if (affected.AddEffect(effect))
                {
                    string msg = string.Format("Added {0} effect to player {1}.", effectName, player.PlayerName);
                    return TextCommandResult.Success(msg);
                }
                else
                {
                    string msg = string.Format("Effect {0} could not be added. The player may be immune or dead.", effectName);
                    return TextCommandResult.Error(msg, msg);
                }
            }
        }

        /// <summary>
        /// Called when the add effect command was called.
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private TextCommandResult OnRemoveEffectCommand(TextCommandCallingArgs arguments)
        {
            IPlayer player = arguments[0] as IPlayer;
            string effect = arguments[1] as string;
            AffectedEntityBehavior affected = player.Entity.GetBehavior<AffectedEntityBehavior>();
            if (affected == null)
            {
                string msg = player.PlayerName + " has no affected behavior.";
                return TextCommandResult.Error(msg, msg);
            }

            if(affected.RemoveEffect(effect, true))
            {
                string msg = string.Format("Removed {0} effect from player {1}.", effect, player.PlayerName);
                return TextCommandResult.Success(msg);
            }
            else
            {
                string msg = string.Format("Player {0} has no {1} effect.", player.PlayerName, effect);
                return TextCommandResult.Error(msg, msg);
            }

        }
    }//!class XEffectsSystem
}//!XLib.XEffects
