using System;
using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a client side interface for some XLeveling methods
    /// </summary>
    /// <seealso cref="XLeveling.IXLevelingAPI" />
    public class XLevelingClient : IXLevelingAPI
    {
        /// <summary>
        /// Gets the xLeveling mod system.
        /// </summary>
        /// <value>
        /// The xLeveling mod system.
        /// </value>
        public XLeveling XLeveling { get; private set; }

        /// <summary>
        /// Gets the local player skill set.
        /// </summary>
        /// <value>
        /// The local player skill set.
        /// </value>
        public PlayerSkillSet LocalPlayerSkillSet { get; private set; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Config Config { get; private set; }

        /// <summary>
        /// The skill dialog.
        /// This is the user interface for this mod.
        /// It displays all player skills and allows the player to choose abilities.
        /// </summary>
        public GuiDialog skillDialog { get; set; }

        /// <summary>
        /// The channel for network communication with the server.
        /// </summary>
        private IClientNetworkChannel channel;

        /// <summary>
        /// Saves the experience accumulated over a specific time frame.
        /// </summary>
        private Dictionary<int, float> AccumulatedExperience = new Dictionary<int, float>();

        /// <summary>
        /// The last timestamp at which accumulated experience has been printed.
        /// </summary>
        private long AccumulatedTimeStamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="XLevelingClient" /> class.
        /// </summary>
        /// <param name="xLeveling">The x leveling.</param>
        /// <exception cref="ArgumentNullException">Is thrown if xLeveling is <c>null</c>.</exception>
        /// <exception cref="Exception">Is thrown if this function was called on the wrong side.</exception>
        internal XLevelingClient(XLeveling xLeveling)
        {
            this.XLeveling = xLeveling ?? throw new ArgumentNullException("The XLeveling system of a XLeveling client interface must not be null.");
            ICoreClientAPI api = this.XLeveling.Api as ICoreClientAPI ?? throw new Exception("Tried to create a client interface on the wrong side.");
            LoadConfiguration();

            //create network channel and register handlers
            this.channel = api.Network.RegisterChannel("XLeveling");
            this.channel.RegisterMessageType(typeof(PlayerSkillPackage));
            this.channel.SetMessageHandler<PlayerSkillPackage>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(ExperiencePackage));
            this.channel.SetMessageHandler<ExperiencePackage>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(PlayerAbilityPackage));
            this.channel.SetMessageHandler<PlayerAbilityPackage>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(SkillConfig));
            this.channel.SetMessageHandler<SkillConfig>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(CommandPackage));
            this.channel.SetMessageHandler<CommandPackage>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(Config));
            this.channel.SetMessageHandler<Config>(this.MessageHandler);
            this.channel.RegisterMessageType(typeof(KnowledgePackage));
            this.channel.SetMessageHandler<KnowledgePackage>(this.MessageHandler);

            api.Input.RegisterHotKey("skilldialoghotkey", "Show/Hide Skill Dialog", GlKeys.O, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("skilldialoghotkey", this.OnHotKeySkillDialog);
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        private void LoadConfiguration()
        {
            //load general configuration
            string path = Path.Combine("XLeveling", "xleveling.json");
            ICoreClientAPI api = XLeveling.Api as ICoreClientAPI;
            try
            {
                this.XLeveling.Mod.Logger.Debug("Load: " + path);
                this.Config = api.LoadModConfig<Config>(path) ?? this.Config;
            }
            catch (Exception error)
            {
                this.XLeveling.Mod.Logger.Error("Error while loading: " + path);
                this.XLeveling.Mod.Logger.Error(error);
            }
        }

        /// <summary>
        /// Called when hotkey to open the skill dialog was pressed.
        /// Opens and closes the skill dialog.
        /// </summary>
        /// <param name="comb">The key combination.</param>
        /// <returns>
        ///   <c>true</c>
        /// </returns>
        private bool OnHotKeySkillDialog(KeyCombination comb)
        {
            if (this.LocalPlayerSkillSet?.PlayerSkills == null) return true;
            if (this.skillDialog == null)
                this.skillDialog = new SkillDialog(this);

            if (this.skillDialog.IsOpened()) this.skillDialog.TryClose();
            else this.skillDialog.TryOpen();
            return true;
        }

        /// <summary>
        /// Handles the SkillConfig from the server.
        /// </summary>
        /// <param name="skillConfig">The skill configuration.</param>
        private void MessageHandler(SkillConfig skillConfig)
        {
            IPlayer player = (this.XLeveling.Api as ICoreClientAPI).World.Player;
            if (player?.Entity == null) return;
            if (this.LocalPlayerSkillSet == null)
                this.LocalPlayerSkillSet = new PlayerSkillSet(player, this.XLeveling.SkillSetTemplate, XLeveling);

            Skill skill = this.LocalPlayerSkillSet[skillConfig.id]?.Skill;
            if(skill == null || skill.Name != skillConfig.name)
            {
                XLeveling.Api.Logger.Error("XLeveling: " + "The configuration of the server is not compatible with your Version!");
                return;
            }
            skill.FromConfig(skillConfig);
            skill.OnConfigReceived();
        }

        /// <summary>
        /// Handles the PlayerSkillPackage from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(PlayerSkillPackage package)
        {
            if (package.skillId < this.LocalPlayerSkillSet?.PlayerSkills.Count && package.skillId >= 0)
            {
                this.LocalPlayerSkillSet.PlayerSkills[package.skillId].Level = package.level;
                this.LocalPlayerSkillSet.PlayerSkills[package.skillId].Experience = package.experience;
            }
        }

        /// <summary>
        /// Handles the ExperiencePackage from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(ExperiencePackage package)
        {
            if (package.skillId < LocalPlayerSkillSet?.PlayerSkills.Count && package.skillId >= 0)
            {
                PlayerSkill playerSkill = LocalPlayerSkillSet.PlayerSkills[package.skillId];
                playerSkill.Experience += package.experience;
                if (!this.XLeveling.Config.trackExpGain) return;

                if (!AccumulatedExperience.ContainsKey(package.skillId))
                {
                    AccumulatedExperience.Add(package.skillId, package.experience);
                }
                else
                {
                    AccumulatedExperience[package.skillId] += package.experience;
                }

                IWorldAccessor world = XLeveling.Api.World;
                long seconds = DateTime.Now.Ticks / TimeSpan.TicksPerSecond;
                if (seconds - AccumulatedTimeStamp > 10)
                {
                    List<int> remove = new List<int>();
                    AccumulatedTimeStamp = seconds;
                    PlayerGroupMembership membership = Array.Find(playerSkill.PlayerSkillSet.Player.Groups, (membership) => membership.GroupName == XLeveling.XLibGroupName);
                    if (membership == null) return;

                    foreach (KeyValuePair<int, float> pair in AccumulatedExperience)
                    {
                        if (pair.Value < 0.01) continue;
                        Skill skill = LocalPlayerSkillSet[pair.Key].Skill;
                        string msg = Lang.Get("You received {0:0.00} experience for the {1} skill.", pair.Value, skill.DisplayName);
                        (world as ClientMain).eventManager.TriggerNewServerChatLine(membership.GroupUid, msg, EnumChatType.Notification, null);
                        remove.Add(pair.Key);
                    }
                    foreach (int ii in remove)
                    {
                        AccumulatedExperience.Remove(ii);
                    }
                }
            }
        }

        /// <summary>
        /// Handles the PlayerAbilityPackage from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(PlayerAbilityPackage package)
        {
            PlayerAbility ability = this.LocalPlayerSkillSet?.Ability(package.skillId, package.abilityId);
            if(ability != null)
            {
                ability.IgnoredRequirements = EnumRequirementType.AllRequirements;
                ability.Tier = package.skilledTier;
                ability.IgnoredRequirements = EnumRequirementType.None;
                if (package.skilledTier == 0)
                {
                    ability.Ability.OnTierChanged(ability, 0);
                }
            }
        }

        /// <summary>
        /// Handles the CommandPackage from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(CommandPackage package)
        {
            IPlayer player = (this.XLeveling.Api as ICoreClientAPI).World.Player;
            if (player?.Entity == null) return;
            if (package == null) return;
            if (this.LocalPlayerSkillSet == null)
                this.LocalPlayerSkillSet = new PlayerSkillSet(player, this.XLeveling.SkillSetTemplate, XLeveling);

            switch (package.command)
            {
                case EnumXLevelingCommand.Reset:
                    this.LocalPlayerSkillSet[package.value]?.Reset();
                    break;
                case EnumXLevelingCommand.UnlearnPoints:
                    this.LocalPlayerSkillSet.UnlearnPoints = (float)package.dValue;
                    break;
                case EnumXLevelingCommand.UnlearnReadyTime:
                    this.LocalPlayerSkillSet.UnlearnCooldown = (float)package.dValue;
                    break;
                case EnumXLevelingCommand.SparringMode:
                    this.LocalPlayerSkillSet.Sparring = package.value > 0;
                    break;
                default: return;
            }
        }

        /// <summary>
        /// Handles the configuration from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(Config package)
        {
            bool track = this.Config?.trackExpGain ?? false;
            this.Config = package;
            this.Config.trackExpGain = track;
            LimitationRequirement specialisations;
            this.XLeveling.Limitations.TryGetValue("specialisations", out specialisations);
            if (specialisations != null) specialisations.Limit = this.Config.specialisationLimit;

            //remove requirements
            XLeveling.RemoveRequirements(Config.disabledRequirements);
        }

        /// <summary>
        /// Handles the KnowledgePackage from the server.
        /// </summary>
        /// <param name="package">The package.</param>
        private void MessageHandler(KnowledgePackage package)
        {
            PlayerSkillSet playerSkillSet = this.LocalPlayerSkillSet;
            if (playerSkillSet == null || package?.name == null) return;
            if (package.level == 0) playerSkillSet.Knowledge.Remove(package.name);
            playerSkillSet.Knowledge[package.name] = package.level;
        }

        /// <summary>
        /// Sends a packet to the server.
        /// </summary>
        /// <param name="package">The package</param>
        public void SendPackage(CommandPackage package)
        {
            this.channel.SendPacket(package);
        }

        /// <summary>
        /// Gets the player skill set for the given player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        /// on client side the local player skill set if the given player is the local one; otherwise, null
        /// on server side the player skill set of the given player if the player exists; otherwise null
        /// </returns>
        public PlayerSkillSet GetPlayerSkillSet(IPlayer player)
        {
            ICoreClientAPI api = (this.XLeveling.Api as ICoreClientAPI);
            if (this.LocalPlayerSkillSet.Player == player)
            {
                return this.LocalPlayerSkillSet;
            }
            else  if (api.World.Player != null)
            {
                this.LocalPlayerSkillSet.Player = api.World.Player;
                if (this.LocalPlayerSkillSet.Player == player)
                {
                    return this.LocalPlayerSkillSet;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds experience to a player skill.
        /// Use this method to give a player experience.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="informClient">if set to <c>true</c> the server will inform the client. In most cases this should be <c>true</c>.
        /// Unless you are sure that the method is called on server and client side.</param>
        public void AddExperienceToPlayerSkill(IPlayer player, int skillId, float experience, bool informClient = true)
        {
            if (!informClient)
            {
                PlayerSkillSet skillSet = this.GetPlayerSkillSet(player);
                skillSet.PlayerSkills[skillId].Experience += experience;
            }
        }

        /// <summary>
        /// Sets the players skill level.
        /// Use this to force set a player skill level.
        /// If you give a player experience the level will be set automatically.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="informClient">if set to <c>true</c> the server will inform the client.</param>
        public void SetPlayerSkillLevel(IPlayer player, int skillId, int level, bool informClient = true)
        {
            if(!informClient)
            {
                PlayerSkillSet skillSet = this.GetPlayerSkillSet(player);
                skillSet.PlayerSkills[skillId].Level += level;
            }
        }

        /// <summary>
        /// Sets the ability tier.
        /// Use this method to set the tier of a player ability.
        /// This method is used by the default gui to set a ability tier.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="abilityId">The ability identifier.</param>
        /// <param name="tier">The tier.</param>
        /// <param name="informServer">if set to <c>true</c> the client informs the server that the player has chosen a tier.</param>
        public void SetAbilityTier(IPlayer player, int skillId, int abilityId, int tier, bool informServer = true)
        {
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(player);
            PlayerAbility playerAbility = playerSkillSet?[skillId]?[abilityId];
            if(playerAbility == null) return;

            int reversedTierChange = playerAbility.Tier - tier;
            bool reduced = reversedTierChange > 0;
            if (reduced && !playerSkillSet.CanUnlearn(reversedTierChange)) return;
            playerAbility.Tier = tier;

            if (reduced)
            {
                //check Requirments
                EnumRequirementType ignored = EnumRequirementType.WeakRequirement;
                playerAbility.PlayerSkill.PlayerSkillSet.CheckRequirements(ignored);

                playerSkillSet.UnlearnPoints -=
                    this.GetPointsForUnlearn() * ((reversedTierChange > 1) ? reversedTierChange * 1.5f : 1);
                playerSkillSet.UnlearnCooldown = this.Config.unlearnCooldown * 60.0f;
            }

            //only informs the player if setting the new tier succeeded
            if (informServer && playerAbility.Tier == tier)
            {
                PlayerAbilityPackage package = new PlayerAbilityPackage(playerAbility, tier);
                this.channel.SendPacket(package);
            }
        }

        /// <summary>
        /// Increases the ability tier.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="abilityId">The ability identifier.</param>
        /// <param name="informServer">if set to <c>true</c> the client informs the server that the player has chosen a tier.</param>
        public void IncreaseAbilityTier(IPlayer player, int skillId, int abilityId, bool informServer = true)
        {
            this.SetAbilityTier(player, skillId, abilityId, this.LocalPlayerSkillSet[skillId]?[abilityId]?.Tier + 1 ?? 0, informServer);
        }

        /// <summary>
        /// Returns the number of points you need to unlearn a skill
        /// </summary>
        /// <returns>
        /// the number of points you need to unlearn a skill
        /// </returns>
        public int GetPointsForUnlearn()
        {
            return this.Config.pointsForUnlearn;
        }
    }//!class XLevelingClient
}//!namespace XLeveling
