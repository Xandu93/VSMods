using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a server side interface for XLeveling
    /// </summary>
    /// <seealso cref="XLeveling.IXLevelingAPI" />
    public class XLevelingServer : IXLevelingAPI
    {
        /// <summary>
        /// Gets the xLeveling mod system.
        /// </summary>
        /// <value>
        /// The xLeveling mod system.
        /// </value>
        public XLeveling XLeveling { get; private set; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Config Config { get; private set; }

        /// <summary>
        /// The channel for network communication with the clients.
        /// </summary>
        private IServerNetworkChannel channel;

        /// <summary>
        /// Gets a list of all online players skill sets
        /// </summary>
        /// <value>
        /// The player skill sets.
        /// </value>
        public Dictionary<IPlayer, PlayerSkillSet> PlayerSkillSets { get; private set; }

        /// <summary>
        /// Gets a list of all disconnected players skill sets
        /// </summary>
        /// <value>
        /// The player skill sets.
        /// </value>
        public Dictionary<string, SavedPlayerSkillSet> DiscPlayerSkillSets { get; private set; }

        /// <summary>
        /// Gets the player group
        /// </summary>
        /// <value>
        /// The player group
        /// </value>
        internal PlayerGroup PlayerGroup;

        /// <summary>
        /// Gets the name of the save directory.
        /// </summary>
        /// <value>
        /// The name of the save directory.
        /// </value>
        public string SaveFileDirectory => Path.Combine(GamePaths.Saves, "XLeveling");

        /// <summary>
        /// Gets or sets the backup file directory.
        /// </summary>
        /// <value>
        /// The backup file directory.
        /// </value>
        public string BackupFileDirectory => Path.Combine(GamePaths.Backups, "XLeveling");

        /// <summary>
        /// Gets or sets the name of the file.
        /// </summary>
        /// <value>
        /// The name of the file.
        /// </value>
        private string FileName
        {
            get
            {
                string invalidChars = new string(Path.GetInvalidFileNameChars());
                Regex regex = new Regex(string.Format("[{0}]", Regex.Escape(invalidChars)));
                string str = this.XLeveling.Api.World.Config.GetString("XLevelingSkillsFile") ?? (this.XLeveling.Api as ICoreServerAPI).WorldManager.SaveGame.WorldName;
                str = regex.Replace(str, "");
                return str + ".json";
            }
        }

        /// <summary>
        /// Gets the name of the save file.
        /// </summary>
        /// <value>
        /// The name of the save file.
        /// </value>
        public string SaveFileName
        {
            get 
            {
                return Path.Combine(SaveFileDirectory, FileName);
            }
        }

        /// <summary>
        /// Gets or sets the name of the backup save file.
        /// </summary>
        /// <value>
        /// The name of the backup save file.
        /// </value>
        public string BackupSaveFileName
        {
            get
            {
                return Path.Combine(BackupFileDirectory, FileName);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="XLevelingServer" /> class.
        /// </summary>
        /// <param name="xLeveling">The x leveling.</param>
        /// <exception cref="ArgumentNullException">Is thrown if xLeveling is <c>null</c>.</exception>
        /// <exception cref="Exception">Is thrown if this function was called on the wrong side.</exception>
        internal XLevelingServer(XLeveling xLeveling)
        {
            this.XLeveling = xLeveling ?? throw new ArgumentNullException("The XLeveling system of a XLeveling server interface must not be null.");
            ICoreServerAPI api = this.XLeveling.Api as ICoreServerAPI ?? throw new Exception("Tried to create a server interface on the wrong side.");

            api.Event.PlayerNowPlaying += OnPlayerNowPlaying;
            api.Event.PlayerDisconnect += OnPlayerDisconnect;
            api.Event.PlayerCreate += OnPlayerCreate;
            api.Event.GameWorldSave += OnWorldSave;
            api.Event.PlayerDeath += OnPlayerDeath;
            this.Config = new Config();
            this.PlayerSkillSets = new Dictionary<IPlayer, PlayerSkillSet>();

            //set paths and load data
            string savePath = SaveFileDirectory;
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            //check if save file exists
            string saveFile = this.SaveFileName;
            if (!File.Exists(saveFile))
            {
                api.World.Config.SetString("XLevelingSkillsFile", api.WorldManager.SaveGame.WorldName);
            }

            this.LoadConfiguration();
            this.LoadData();

            //create chat channel
            PlayerGroup = api.Groups.GetPlayerGroupByName(XLeveling.XLibGroupName);
            if (PlayerGroup == null)
            {
                PlayerGroup = new PlayerGroup();
                PlayerGroup.Name = XLeveling.XLibGroupName;
                PlayerGroup.OwnerUID = null;
                api.Groups.AddPlayerGroup(PlayerGroup);
                PlayerGroup.Md5Identifier = GameMath.Md5Hash(PlayerGroup.Uid + "null");
            }
            PlayerGroup.JoinPolicy = "everyone";

            //create network channel and register handlers
            this.channel = api.Network.RegisterChannel("XLeveling");
            this.channel.RegisterMessageType(typeof(PlayerSkillPackage));
            this.channel.RegisterMessageType(typeof(ExperiencePackage));
            this.channel.RegisterMessageType(typeof(PlayerAbilityPackage));
            this.channel.SetMessageHandler<PlayerAbilityPackage>(this.OnPlayerAbilityPackage);
            this.channel.RegisterMessageType(typeof(SkillConfig));
            this.channel.RegisterMessageType(typeof(CommandPackage));
            this.channel.SetMessageHandler<CommandPackage>(this.OnPlayerMessage);
            this.channel.RegisterMessageType(typeof(Config));
            this.channel.RegisterMessageType(typeof(KnowledgePackage));
            CommandArgumentParsers parsers = api.ChatCommands.Parsers;

            //register chat commands
            api.ChatCommands.Create()
                .WithName("level")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Sets the level of a player's skill. Can also be used to set a skill level for all players or all skills. You can also use this command to reset configurations.")
                .HandleWith(OnSkillLevelCommand)
                .WithRootAlias("skill")
                .WithArgs(new ICommandArgumentParser[] { 
                    parsers.WordRange("cmd", new string[] { "add", "set", "get", "reset"}), 
                    parsers.Word("player"),
                    parsers.Word("skill"),
                    parsers.OptionalInt("level")});

            api.ChatCommands.Create()
                .WithName("exp")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Sets the experience of a player's skill.")
                .HandleWith(OnSkillExpCommand)
                .WithArgs(new ICommandArgumentParser[] {
                    parsers.WordRange("cmd", new string[] { "add", "set", "get"}),
                    parsers.Word("player"),
                    parsers.Word("skill"),
                    parsers.OptionalFloat("experience")});

            api.ChatCommands.Create()
                .WithName("tier")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Sets the tier of a player's ability.")
                .HandleWith(OnAbilityTierCommand)
                .WithArgs(new ICommandArgumentParser[] {
                    parsers.WordRange("cmd", new string[] { "add", "set", "get"}),
                    parsers.Word("player"),
                    parsers.Word("skill"),
                    parsers.Word("ability"),
                    parsers.Int("tier")});

            api.ChatCommands.Create()
                .WithName("skillset")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Saves, loads or deletes a specific skill set.")
                .HandleWith(OnSkillSetCommand)
                .WithArgs(new ICommandArgumentParser[] {
                    parsers.WordRange("cmd", new string[] { "save", "load", "delete", "default"}),
                    parsers.Word("SkillSetName")});

            api.ChatCommands.Create()
                .WithName("skillbook")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithDescription("Creates a skill book that will grant experience for a specific skill.")
                .HandleWith(OnSkillBookCommand)
                .WithArgs(new ICommandArgumentParser[] {
                    parsers.Word("skill"),
                    parsers.Float("experience"),
                    parsers.OptionalWord("color"),
                    parsers.OptionalWord("knowledge"),
                    parsers.OptionalInt("quantity"),
                });
        }

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        private void LoadConfiguration()
        {
            //load general configuration
            string path = Path.Combine("XLeveling", "xleveling.json");
            ICoreServerAPI api = XLeveling.Api as ICoreServerAPI;
            try
            {
                this.XLeveling.Mod.Logger.Debug("Load: " + path);
                this.Config = api.LoadModConfig<Config>(path) ?? this.Config;
            }
            catch (Exception error)
            {
                api.Server.LogError("[XLeveling] Error while loading: " + path);
                api.Server.LogError(error.Message);
            }

            LimitationRequirement specialisations;
            this.XLeveling.Limitations.TryGetValue("specialisations", out specialisations);
            if (specialisations != null) specialisations.Limit = this.Config.specialisationLimit;

            this.XLeveling.Mod.Logger.Debug("Save: " + path);
            api.StoreModConfig(this.Config, path);

            //load skill configuration
            foreach (Skill skill in this.XLeveling.SkillSetTemplate.Skills)
            {
                SkillConfig skillConfig;
                path = Path.Combine("XLeveling", skill.Name + ".json");
                try
                {
                    this.XLeveling.Mod.Logger.Debug("Load: " + path);
                    skillConfig = api.LoadModConfig<SkillConfig>(path);
                    if (skillConfig != null) skill.FromConfig(skillConfig);
                }
                catch (Exception error)
                {
                    api.Server.LogError("[XLeveling] Error while loading: " + path);
                    api.Server.LogError(error.Message);
                }

                this.XLeveling.Mod.Logger.Debug("Save: " + path);
                skillConfig = new SkillConfig(skill);
                api.StoreModConfig(skillConfig, path);
            }

            //remove requirements
            XLeveling.RemoveRequirements(Config.disabledRequirements);
        }

        /// <summary>
        /// Sends the configuration to players.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        private void SendConfig(IServerPlayer byPlayer)
        {
            this.channel.SendPacket(this.Config, byPlayer);
            foreach (Skill skill in this.XLeveling.SkillSetTemplate.Skills)
            {
                SkillConfig skillConfig = new SkillConfig(skill);
                this.channel.SendPacket(skillConfig, byPlayer);
            }
        }

        /// <summary>
        /// Loads save data from a specific file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns>
        ///  1, if the method succeeded<br></br>
        ///  0, if the file does not exist<br></br>
        /// -1, if the method failed
        /// </returns>
        private int LoadFromFile(string fileName)
        {
            if (fileName == null) return 0;
            if (!File.Exists(fileName)) return 0;
            try
            {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Error = (object sender, Newtonsoft.Json.Serialization.ErrorEventArgs err) => 
                {
                    XLeveling.Api.Logger.Log(EnumLogType.Error, "[XLeveling] Error while loading: " + fileName + ": \n" + err.ErrorContext.Error.Message);
                    err.ErrorContext.Handled = true;
                };

                this.DiscPlayerSkillSets = JsonConvert.DeserializeObject<Dictionary<string, SavedPlayerSkillSet>>(File.ReadAllText(fileName), settings);
                if (DiscPlayerSkillSets == null)
                {
                    this.DiscPlayerSkillSets = new Dictionary<string, SavedPlayerSkillSet>();
                    XLeveling.Api.Logger.Log(EnumLogType.Error, "[XLeveling] Error while loading: " + fileName + "\nThe file seems to be damaged.");
                    return -1;
                }
                foreach (PlayerSkillSet playerSkillSet in this.PlayerSkillSets.Values)
                {
                    this.LoadPlayerSkillSet(playerSkillSet.Player as IServerPlayer);
                }
            }
            catch (Exception error)
            {
                XLeveling.Api.Logger.Log(EnumLogType.Error, "[XLeveling] Error while loading: " + fileName + ": \n" + error.Message);
                return -1;
            }
            return 1;
        }

        /// <summary>
        /// Loads the skill data of all players form a json file.
        /// </summary>
        private void LoadData()
        {
            int result = LoadFromFile(this.SaveFileName);
            if (result > 0) return;
            if (result < 0) 
            {
                XLeveling.Api.Logger.Warning("[XLeveling] Failed to load save file. Try to load backup.");
                result = LoadFromFile(this.BackupSaveFileName);
            }
            if (result > 0) return;
            this.DiscPlayerSkillSets = new Dictionary<string, SavedPlayerSkillSet>();
        }

        /// <summary>
        /// Saves the skill data of all players to a json file.
        /// </summary>
        private void SaveData()
        {
            string saveFileName = this.SaveFileName;
            Dictionary<string, SavedPlayerSkillSet> toStore = new Dictionary<string, SavedPlayerSkillSet>();

            try
            {
                string backupName = this.BackupSaveFileName;
                string path = Path.GetDirectoryName(backupName);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                File.Move(saveFileName, backupName, true);
            }
            catch (Exception)
            { }

            JsonSerializer serializer = new JsonSerializer();
            serializer.NullValueHandling = NullValueHandling.Ignore;
            serializer.Formatting = Formatting.Indented;
            StreamWriter streamWriter = new StreamWriter(saveFileName);
            JsonWriter jsonWriter = new JsonTextWriter(streamWriter);

            foreach (IPlayer player in this.PlayerSkillSets.Keys)
            {
                toStore.Add(player.PlayerUID, new SavedPlayerSkillSet(this.PlayerSkillSets[player]));
            }
            foreach (string key in this.DiscPlayerSkillSets.Keys)
            {
                try
                {
                    toStore.Add(key, this.DiscPlayerSkillSets[key]);
                }
                catch(Exception exp)
                {
                    this.XLeveling.Api.Logger.Warning("Exception thrown during XLeveling data save but save will continue.");
                    this.XLeveling.Api.Logger.Warning(exp);
                }
            }

            serializer.Serialize(jsonWriter, toStore);
            jsonWriter.Close();
            streamWriter.Dispose();
        }

        /// <summary>
        /// Called when the world was saved. Saves the mods player data.
        /// </summary>
        private void OnWorldSave()
        {
            this.SaveData();
        }

        /// <summary>
        /// Called when a player disconnects from the server.
        /// Updates the data table for this player.
        /// </summary>
        /// <param name="byPlayer">The by player.</param>
        private void OnPlayerDisconnect(IServerPlayer byPlayer)
        {
            if (byPlayer == null) return;
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(byPlayer);
            UpdatePlayerDataset(playerSkillSet);
            this.PlayerSkillSets.Remove(byPlayer);
        }

        /// <summary>
        /// Updates the dataset for a specific player skill set.
        /// </summary>
        /// <param name="playerSkillSet">The player skill set.</param>
        private void UpdatePlayerDataset(PlayerSkillSet playerSkillSet)
        {
            if (playerSkillSet?.Player == null) return;
            SavedPlayerSkillSet savedPlayerSkillSet = new SavedPlayerSkillSet(playerSkillSet);
            this.DiscPlayerSkillSets[playerSkillSet.Player.PlayerUID] = savedPlayerSkillSet;
        }

        /// <summary>
        /// Called when a player joins the game for the first time.
        /// Sets up the player group.
        /// </summary>
        /// <param name="byPlayer"></param>
        private void OnPlayerCreate(IServerPlayer byPlayer)
        {
            if (PlayerGroup == null) return;
            if (byPlayer.GetGroup(PlayerGroup.Uid) == null)
            {
                PlayerGroupMembership membership = new PlayerGroupMembership();
                membership.GroupName = PlayerGroup.Name;
                membership.GroupUid = PlayerGroup.Uid;
                membership.Level = EnumPlayerGroupMemberShip.Member;

                byPlayer.ServerData.PlayerGroupMemberships.Add(PlayerGroup.Uid, membership);
                PlayerGroup.OnlinePlayers.Add(byPlayer);
            }
        }

        /// <summary>
        /// Called when a player is ready to play.
        /// Creates a skill entry for a player, sends the configuration to it and loads player data.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        private void OnPlayerNowPlaying(IServerPlayer byPlayer)
        {
            if (byPlayer == null) return;
            PlayerSkillSet skillSet = new PlayerSkillSet(byPlayer, this.XLeveling.SkillSetTemplate, XLeveling);
            this.PlayerSkillSets.Add(byPlayer, skillSet);
            this.SendConfig(byPlayer);
            this.LoadPlayerSkillSet(byPlayer);
        }

        /// <summary>
        /// Loads data for a specific player.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        private void LoadPlayerSkillSet(IServerPlayer byPlayer)
        {
            PlayerSkillSet skillSet;
            this.PlayerSkillSets.TryGetValue(byPlayer, out skillSet);

            SavedPlayerSkillSet savedPlayerSkillSet;
            this.DiscPlayerSkillSets.TryGetValue(byPlayer.PlayerUID, out savedPlayerSkillSet);
            if (this.DiscPlayerSkillSets != null)
            {
                skillSet.FromSavedSkillSet(savedPlayerSkillSet);
                this.DiscPlayerSkillSets.Remove(byPlayer.PlayerUID);
            }

            //check Requirments
            EnumRequirementType ignored = EnumRequirementType.WeakRequirement;
            skillSet.CheckRequirements(ignored);

            //send data to player
            this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.UnlearnPoints, skillSet.UnlearnPoints), byPlayer);
            this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.UnlearnReadyTime, skillSet.UnlearnCooldown), byPlayer);
            this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.SparringMode, skillSet.Sparring ? 1 : 0), byPlayer);
            //skills
            foreach (PlayerSkill playerSkill in skillSet.PlayerSkills)
            {
                PlayerSkillPackage playerSkillPackage = new PlayerSkillPackage(playerSkill);
                this.channel.SendPacket(playerSkillPackage, byPlayer);

                //abilities
                foreach (PlayerAbility playerAbility in playerSkill.PlayerAbilities)
                {
                    if (playerAbility.Tier == 0)
                    {
                        playerAbility.Ability.OnTierChanged(playerAbility, 0);
                    }
                    PlayerAbilityPackage playerAbilityPackage = new PlayerAbilityPackage(playerAbility);
                    this.channel.SendPacket(playerAbilityPackage, byPlayer);
                }
            }

            foreach (string key in skillSet.Knowledge.Keys)
            {
                this.channel.SendPacket(new KnowledgePackage(key, skillSet.Knowledge[key]), byPlayer);
            }
        }

        /// <summary>
        /// Called when a player died.
        /// Reduces the player experience for all skills.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        /// <param name="damageSource">The damage source.</param>
        private void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            PlayerSkillSet playerSkillSet = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>();
            if (playerSkillSet == null) return;

            PlayerSkillSet killerSkillSet = (damageSource.GetCauseEntity()?.GetBehavior<PlayerSkillSet>());
            bool shouldLose = !(playerSkillSet.Sparring && (killerSkillSet?.Sparring ?? false));

            float cooldown =
                this.Config.deathPenaltyCooldown *
                this.XLeveling.Api.World.Calendar.SpeedOfTime / 3600.0f;
            if (cooldown + playerSkillSet.LastDeath <= this.XLeveling.Api.World.Calendar.TotalHours)
            {
                playerSkillSet.LastDeath = this.XLeveling.Api.World.Calendar.TotalHours;
            }
            else shouldLose = false;

            foreach (PlayerSkill playerSkill in playerSkillSet.PlayerSkills)
            {
                playerSkill.Skill?.OnPlayerDeath(playerSkillSet, shouldLose);
            }
        }

        /// <summary>
        /// Handles the PlayerSkillPackage from the client.
        /// </summary>
        /// <param name="fromPlayer">From player.</param>
        /// <param name="abilitySkilledPackage">The PlayerAbilityPackage.</param>
        private void OnPlayerAbilityPackage(IServerPlayer fromPlayer, PlayerAbilityPackage abilitySkilledPackage)
        {
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(fromPlayer);
            PlayerAbility playerAbility = playerSkillSet?[abilitySkilledPackage.skillId]?[abilitySkilledPackage.abilityId];
            if (playerAbility == null) return;

            int reversedTierChange = playerAbility.Tier - abilitySkilledPackage.skilledTier;
            bool reduced = reversedTierChange > 0;
            if (!reduced || playerSkillSet.CanUnlearn(reversedTierChange)) playerAbility.Tier = abilitySkilledPackage.skilledTier;

            if (reduced && playerSkillSet.CanUnlearn(reversedTierChange))
            {
                //check Requirments
                EnumRequirementType ignored = EnumRequirementType.WeakRequirement;
                playerSkillSet.CheckRequirements(ignored);

                playerSkillSet.UnlearnPoints -= 
                    this.GetPointsForUnlearn() * ((reversedTierChange > 1) ? reversedTierChange * 1.5f : 1);
                playerSkillSet.UnlearnCooldown = this.Config.unlearnCooldown * 60.0f;
            }

            //corrects data on client side if setting the tier failed
            if (abilitySkilledPackage.skilledTier != playerAbility.Tier)
            {
                abilitySkilledPackage.skilledTier = playerAbility.Tier;
                this.channel.SendPacket(abilitySkilledPackage, fromPlayer);
            }
            this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.UnlearnReadyTime, playerSkillSet.UnlearnCooldown), fromPlayer);
            this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.UnlearnPoints, playerSkillSet.UnlearnPoints), fromPlayer);
        }

        /// <summary>
        /// Handles the PlayerSkillPackage from the client.
        /// </summary>
        /// <param name="fromPlayer">From player.</param>
        /// <param name="package">The command package.</param>
        private void OnPlayerMessage(IServerPlayer fromPlayer, CommandPackage package)
        {
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(fromPlayer);
            if (playerSkillSet == null) return;

            switch (package.command)
            {
                case EnumXLevelingCommand.SparringMode:
                    playerSkillSet.Sparring = package.value > 0;
                    break;
                default: return;
            }
        }

        /// <summary>
        /// Gets the player skill set for the given player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///   on client side the local player skill set if the given player is the local one; otherwise, null
        ///   on server side the player skill set of the given player if the player exists; otherwise null
        /// </returns>
        public PlayerSkillSet GetPlayerSkillSet(IPlayer player)
        {
            if (player == null) return null;
            PlayerSkillSet playerSkillSet;
            this.PlayerSkillSets.TryGetValue(player, out playerSkillSet);
            return playerSkillSet;
        }

        /// <summary>
        /// Gets the skill set of the player with the given name.
        /// </summary>
        /// <param name="playerName">Name of the player.</param>
        /// <returns>
        ///   the skill set of the player with the given name if a player with this name exists; null, otherwise
        /// </returns>
        public PlayerSkillSet GetPlayerSkillSet(string playerName)
        {
            foreach (PlayerSkillSet playerSkillSet in this.PlayerSkillSets.Values)
            {
                if (playerSkillSet.Player.PlayerName == playerName)
                {
                    return playerSkillSet;
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
            PlayerSkillSet skillSet = GetPlayerSkillSet(player);
            if (skillSet == null) return;

            skillSet.PlayerSkills[skillId].Experience += experience;
            if (informClient)
            {
                this.channel.SendPacket(new ExperiencePackage(skillId, experience), skillSet.Player as IServerPlayer);
            }
        }

        /// <summary>
        /// Sets the player skill level.
        /// Use this to force set a player skill level.
        /// If you give a player experience the level will be set automatically.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="informClient">if set to <c>true</c> the server will inform the client.</param>
        public void SetPlayerSkillLevel(IPlayer player, int skillId, int level, bool informClient = true)
        {
            PlayerSkillSet skillSet = GetPlayerSkillSet(player);
            if (skillSet == null) return;

            PlayerSkill skill = skillSet[skillId];
            if (skill == null) return;

            skill.Level = level;
            if (informClient)
            {
                this.channel.SendPacket(new PlayerSkillPackage(skill), skillSet.Player as IServerPlayer);
            }
        }

        /// <summary>
        /// Sets the ability tier.
        /// Use this method to set the tier of a player ability.
        /// This method is used by the default gui to set a ability tier.
        /// In general you don't need to call this method.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="abilityId">The ability identifier.</param>
        /// <param name="tier">The tier.</param>
        /// <param name="informServer">if set to <c>true</c> the client informs the server that the player has chosen a tier.</param>
        public void SetAbilityTier(IPlayer player, int skillId, int abilityId, int tier, bool informServer = true)
        {
            if (!informServer)
            {
                PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(player);
                PlayerAbility ability = playerSkillSet[skillId]?[abilityId];
                if (ability == null) return;
                ability.Tier = tier;
                this.channel.SendPacket(new PlayerAbilityPackage(ability, tier), playerSkillSet.Player as IServerPlayer);
            }
        }

        /// <summary>
        /// Increases the ability tier.
        /// Use this method to increases the tier of a player ability.
        /// This method is used by the default gui to set a ability tier.
        /// In general you don't need to call this method.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="abilityId">The ability identifier.</param>
        /// <param name="informServer">if set to <c>true</c> the client informs the server that the player has chosen a tier.</param>
        public void IncreaseAbilityTier(IPlayer player, int skillId, int abilityId, bool informServer = true)
        {
            if (!informServer)
            {
                PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(player);
                PlayerAbility ability = playerSkillSet[skillId]?[abilityId];
                if (ability == null) return;
                ability.Tier += 1;
                this.channel.SendPacket(new PlayerAbilityPackage(ability, ability.Tier), playerSkillSet.Player as IServerPlayer);
            }
        }

        /// <summary>
        /// Sets the player knowledge.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="name">The knowledge name.</param>
        /// <param name="level">The knowledge level.</param>
        public void SetPlayerKnowledge(IPlayer player, string name, int level)
        {
            PlayerSkillSet skillSet = this.GetPlayerSkillSet(player);
            if (name == null) return;
            if (level == 0) 
                skillSet.Knowledge.Remove(name);
            else
                skillSet.Knowledge[name] = level;
            this.channel.SendPacket(new KnowledgePackage(name, level), player as IServerPlayer);
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

        /// <summary>
        /// Converts an error message to a TextCommandResult
        /// </summary>
        /// <param name="msg">The message</param>
        /// <returns>A command error result</returns>
        TextCommandResult CommandErrorResult(string msg)
        {
            return TextCommandResult.Error(msg, msg);
        }

        /// <summary>
        /// Called when the level command was called.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        public TextCommandResult OnSkillLevelCommand(TextCommandCallingArgs arguments)
        {
            TextCommandResult result = new TextCommandResult();
            result.StatusMessage = "";
            result.Status = EnumCommandStatus.Success;
            IServerPlayer player = arguments.Caller.Player as IServerPlayer;
            string cmd = arguments[0] as string;
            string splayer = arguments[1] as string;
            string sskill = arguments[2] as string;
            int level = arguments.ArgCount >= 4 ? (int)arguments[3] : 0;

            //list of players affected by this command
            List<PlayerSkillSet> playerSkillSets = new List<PlayerSkillSet>();
            //list of players affected by this command that are currently offline
            List<SavedPlayerSkillSet> savedPlayerSkillSets = new List<SavedPlayerSkillSet>();
            //list of skills affected by this command
            List<Skill> skills = new List<Skill>();

            bool setLevel = false;
            bool resetConfig = false;

            if (arguments.ArgCount != 4 && !(arguments.ArgCount == 3 && cmd == "reset"))
            {
                return CommandErrorResult("The level/skill command requires 4 parameter or reset as the first and 2 additional parameters.");
            }

            if(level > 0)
            {
                setLevel = true;
            }

            bool notify = true;
            if (splayer == "all" || splayer == "All" || sskill == "all" || sskill == "All") notify = false;

            if (splayer == "all" || splayer == "All")
            {
                foreach (PlayerSkillSet playerSkillSet in this.PlayerSkillSets.Values)
                {
                    playerSkillSets.Add(playerSkillSet);
                }
                foreach (SavedPlayerSkillSet playerSkillSet in this.DiscPlayerSkillSets.Values)
                {
                    savedPlayerSkillSets.Add(playerSkillSet);
                }
            }
            else if ((splayer == "config" || splayer == "Config") && cmd == "reset")
            {
                resetConfig = true;
            }
            else
            {
                PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(splayer);
                if (playerSkillSet == null)
                {
                    return CommandErrorResult("Can't find the player " + splayer + ".");
                }
                playerSkillSets.Add(playerSkillSet);
            }

            if (sskill == "all" || sskill == "All")
            {
                foreach (Skill skill in this.XLeveling.SkillSetTemplate.Skills)
                {
                    skills.Add(skill);
                }
            }
            else
            {
                Skill skill = this.XLeveling.SkillSetTemplate.FindSkill(sskill, true);
                if (skill == null)
                {
                    return CommandErrorResult("Can't find the skill " + sskill + ".");
                }
                skills.Add(skill);
            }

            if (resetConfig)
            {
                string path = Path.Combine(GamePaths.ModConfig, "XLeveling");
                foreach (Skill skill in skills)
                {
                    string fullPath = Path.Combine(path, skill.Name + ".json");
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        result.StatusMessage += "File " + fullPath + " has been removed.\n";
                    }
                }
                result.Status = EnumCommandStatus.Success;
                return result;
            }

            foreach (PlayerSkillSet playerSkillSet in playerSkillSets)
            {
                foreach (Skill skill in skills)
                {
                    PlayerSkill playerSkill = playerSkillSet[skill.Id];
                    if (playerSkill == null) continue;

                    if (cmd == "set") playerSkill.Level = level;
                    else if (cmd == "add") playerSkill.Level += level;
                    else if (cmd == "get")
                    {
                        result.StatusMessage +=
                            playerSkillSet.Player.PlayerName + "'s " +
                            skill.DisplayName + " skill is at level " + 
                            playerSkill.Level + ".";
                        continue;
                    }
                    else if (cmd == "reset")
                    {
                        if (setLevel) playerSkill.Level = level;
                        this.channel.SendPacket(new CommandPackage(EnumXLevelingCommand.Reset, playerSkill.Skill.Id), playerSkillSet.Player as IServerPlayer);
                        playerSkill.Reset();
                        if (notify)
                        {
                            result.StatusMessage += 
                                "Player " + playerSkillSet.Player.PlayerName + "'s " + 
                                skill.DisplayName + " skill has been reset.";
                        }
                        (playerSkillSet.Player as IServerPlayer)?.SendMessage(0, "Your " + skill.DisplayName + " skill has been reset.", EnumChatType.CommandSuccess);
                    }
                    else
                    {
                        return CommandErrorResult(cmd + " is not a valid operation.");
                    }

                    if (setLevel)
                    {
                        playerSkill.AddExperience(-playerSkill.Experience, false);
                        if (notify)
                        {
                            result.StatusMessage += 
                                "Sets player " + playerSkillSet.Player.PlayerName + "'s " + 
                                skill.DisplayName + " skill to level " + 
                                playerSkill.Level + ".";
                        }
                        (playerSkillSet.Player as IServerPlayer)?.SendMessage(0, "Your level of the " + skill.DisplayName + " skill was set to " + playerSkill.Level + ".", EnumChatType.CommandSuccess);

                        PlayerSkillPackage package = new PlayerSkillPackage(playerSkill);
                        this.channel.SendPacket(package, (playerSkillSet.Player as IServerPlayer));
                    }
                }
            }
            foreach (SavedPlayerSkillSet playerSkillSet in savedPlayerSkillSets)
            {
                foreach (Skill skill in skills)
                {
                    SavedPlayerSkill playerSkill;
                    if (!playerSkillSet.Skills.TryGetValue(skill.Name, out playerSkill)) continue;

                    if (cmd == "set") playerSkill.Level = level;
                    else if (cmd == "add") playerSkill.Level += level;
                    else if (cmd == "reset")
                    {
                        if (setLevel) playerSkill.Level = level;
                        foreach (SavedPlayerAbility playerAbility in playerSkill.Abilities.Values)
                        {
                            playerAbility.Tier = 0;
                        }
                    }
                    if (setLevel) playerSkill.Experience = 0;
                }
            }
            return result;
        }

        /// <summary>
        /// Called when the exp command was called.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        private TextCommandResult OnSkillExpCommand(TextCommandCallingArgs arguments)
        {
            TextCommandResult result = new TextCommandResult();
            result.StatusMessage = "";
            result.Status = EnumCommandStatus.Success;

            if (arguments.ArgCount != 4)
            {
                return CommandErrorResult("The exp command requires 4 parameter.");
            }

            string playerName = arguments[1] as string;
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(playerName);
            if (playerSkillSet == null)
            {
                return CommandErrorResult("Can't find the player " + playerName + ".");
            }

            string skillName = arguments[2] as string;
            PlayerSkill playerSkill = playerSkillSet.FindSkill(skillName, true);
            if (playerSkill == null)
            {
                return CommandErrorResult("Can't find the skill " + skillName + ".");
            }

            float exp = (float)arguments[3];
            string cmd = arguments[0] as string;

            if (cmd == "set")
            {
                playerSkill.Experience = exp;
            }
            else if (cmd == "add")
            {
                playerSkill.Experience += exp;
            }
            else if (cmd == "get")
            {
                result.StatusMessage = 
                    playerName + "'s " + 
                    playerSkill.Skill.Name + 
                    " skill's experience is " + 
                    playerSkill.Experience + "/" + 
                    playerSkill.RequiredExperience + ".";
                return result;
            }
            else
            {
                return CommandErrorResult(cmd + " is not a valid operation.");
            }
            
            result.StatusMessage = 
                exp.ToString("0.00") + " experience was given to " + 
                playerName + "'s " + 
                playerSkill.Skill.Name + " skill.";

            (playerSkillSet.Player as IServerPlayer)?.SendMessage(0, "You have gained  " + exp.ToString("0.00") + " experience in the " + playerSkill.Skill.Name + " skill.", EnumChatType.CommandSuccess);

            PlayerSkillPackage package = new PlayerSkillPackage(playerSkill);
            this.channel.SendPacket(package, (playerSkillSet.Player as IServerPlayer));
            return result;
        }

        /// <summary>
        /// Called when the tier command was called.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        private TextCommandResult OnAbilityTierCommand(TextCommandCallingArgs arguments)
        {
            TextCommandResult result = new TextCommandResult();
            result.StatusMessage = "";
            result.Status = EnumCommandStatus.Success;

            if (arguments.ArgCount != 5)
            {
                return CommandErrorResult("The exp command requires 5 parameter.");
            }

            string playerName = arguments[1] as string;
            PlayerSkillSet playerSkillSet = this.GetPlayerSkillSet(playerName);
            if (playerSkillSet == null)
            {
                return CommandErrorResult("Can't find the player " + playerName + ".");
            }

            string skillName = arguments[2] as string;
            PlayerSkill playerSkill = playerSkillSet.FindSkill(skillName, true);
            if (playerSkill == null)
            {
                return CommandErrorResult("Can't find the skill " + skillName + ".");
            }

            string abilityName = arguments[3] as string;
            PlayerAbility playerAbility = playerSkill.FindAbility(abilityName, true);
            if (playerAbility == null)
            {
                return CommandErrorResult("Can't find the ability " + abilityName + ".");
            }

            int tier = (int)arguments[4];
            string cmd = arguments[0] as string;

            if (cmd == "set")
            {
                playerAbility.Tier = tier;
            }
            else if (cmd == "add")
            {
                playerAbility.Tier += tier;
            }
            else if (cmd == "get")
            {
                result.StatusMessage =
                    playerName + "'s " + 
                    playerAbility.Ability.Name + 
                    " ability tier is " + 
                    playerAbility.Tier + ".";
                return result;
            }
            else
            {
                return CommandErrorResult(cmd + " is not a valid operation.");
            }

            result.StatusMessage =
                "Set player " + playerName + "'s " + 
                playerAbility.Ability.Name + " " +
                "ability tier to " + 
                playerAbility.Tier + ".";

            (playerSkillSet.Player as IServerPlayer)?.SendMessage(0, "Your tier of the " + playerAbility.Ability.Name + " ability was set to " + playerAbility.Tier + ".", EnumChatType.CommandSuccess);

            PlayerAbilityPackage package = new PlayerAbilityPackage(playerAbility);
            this.channel.SendPacket(package, (playerSkillSet.Player as IServerPlayer));
            return result;
        }

        /// <summary>
        /// Called when the skillset command was called.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        private TextCommandResult OnSkillSetCommand(TextCommandCallingArgs arguments)
        {
            TextCommandResult result = new TextCommandResult();
            result.StatusMessage = "";
            result.Status = EnumCommandStatus.Success;

            string cmd = arguments[0] as string;

            if (cmd == "default")
            {
                if (arguments.ArgCount < 1)
                {
                    return CommandErrorResult("The skillset command requires 2 parameter or default as the first one.");
                }
            }
            else
            {
                if (arguments.ArgCount < 2)
                {
                    return CommandErrorResult("The skillset command requires 2 parameter or default as the first one.");
                }
            }

            string fileName = arguments[1] as string;
            string newFile = Path.Combine(GamePaths.Saves, "XLeveling", fileName + ".json");

            if (cmd == "save")
            {
                Regex regex = new Regex("[" + Regex.Escape(@":/?*" + "\"") + "]");
                if (regex.IsMatch(fileName))
                {
                    return CommandErrorResult(fileName + " is not a valid name.");
                }

                if (File.Exists(newFile))
                {
                    return CommandErrorResult(fileName + " file already exists! Please delete it with the xleveling delete command to override it.");
                }

                this.SaveData();
                this.XLeveling.Api.World.Config.SetString("XLevelingSkillsFile", fileName);
                this.SaveData();
                result.StatusMessage = "Saved skillset as " + fileName;
            }
            else if (cmd == "load")
            {
                if (!File.Exists(newFile))
                {
                    return CommandErrorResult("Can´t find file: " + fileName + ".");
                }

                this.SaveData();
                this.XLeveling.Api.World.Config.SetString("XLevelingSkillsFile", fileName);
                this.LoadData();
                result.StatusMessage = "Loaded skillset: " + fileName + "\nPlease restart the game to make sure all changes take effect.";
            }
            else if (cmd == "delete")
            {
                if (!File.Exists(newFile))
                {
                    return CommandErrorResult("Can´t find file: " + fileName + ".");
                }
                File.Delete(newFile);
                result.StatusMessage = "Deleted skillset: " + fileName;
            }
            else if (cmd == "default")
            {
                ICoreServerAPI api = this.XLeveling.Api as ICoreServerAPI;
                //newFile = Path.Combine(GamePaths.Saves, "XLeveling", api.WorldManager.SaveGame.WorldName + ".json");

                this.SaveData();
                api.World.Config.SetString("XLevelingSkillsFile", api.WorldManager.SaveGame.WorldName);
                this.LoadData();
                result.StatusMessage = "Loaded skillset: " + api.WorldManager.SaveGame.WorldName;
            }
            else
            {
                return CommandErrorResult(cmd + " is not a valid operation.");
            }
            return result;
        }

        /// <summary>
        /// Called when the skillbook command was called.
        /// </summary>
        /// <param name="arguments">The arguments.</param>
        private TextCommandResult OnSkillBookCommand(TextCommandCallingArgs arguments)
        {
            TextCommandResult result = new TextCommandResult();
            result.StatusMessage = "";
            result.Status = EnumCommandStatus.Success;

            string skillName = arguments[0] as string;
            float experience = arguments[1] as float? ?? 0.0f;
            string color = arguments[2] as string ?? "aged-gray";
            string knowledge = arguments[3] as string ?? null;
            int count = arguments[4] as int? ?? 1;
            Skill skill = this.XLeveling.GetSkill(skillName, true);

            if (skill == null)
            {
                return CommandErrorResult("Failed to resolve skill: " + skillName);
            }

            Entity entity = arguments.Caller.Entity;
            if (entity == null)
            {
                return CommandErrorResult("Failed to resolve position to spawn the skill book.");
            }

            IWorldAccessor world = this.XLeveling.Api.World;
            AssetLocation asset = new AssetLocation("xlib", "skillbook-" + color);
            Item book = world.GetItem(asset);
            if (book == null)
            {
                return CommandErrorResult("Could not find item: \"" + asset.Path + "\". Maybe the book color does not exist?");
            }

            ItemStack stack = new ItemStack(book, Math.Max(count, 1));
            stack.Attributes.SetString("skill", skill.Name);
            if (experience != 0.0f) stack.Attributes.SetFloat("experience", experience);
            if (knowledge != null) stack.Attributes.SetString("knowledge", knowledge);

            if (!(arguments.Caller.Player.InventoryManager?.TryGiveItemstack(stack) ?? false))
            {
                world.SpawnItemEntity(stack, entity.Pos.XYZ);
            }

            return result;
        }
    }//!class XLevelingServer
}//!namespace XLeveling
