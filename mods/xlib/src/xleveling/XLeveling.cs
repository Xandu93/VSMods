using System.IO;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using System.Collections.Generic;
using Vintagestory.API.Datastructures;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents the main interface for XLeveling and is used to initialize the mod.
    /// If you want to make a submod use the Instance method to receive an interface.
    /// </summary>
    /// <seealso cref="ModSystem" />
    public class XLeveling : ModSystem
    {
        /// <summary>
        /// Gets an instance of the <see cref="XLeveling" /> mod interface.
        /// Is the same as "api.ModLoader.GetModSystem("XLib.XLeveling.XLeveling") as XLeveling".
        /// </summary>
        /// <param name="api">The API.</param>
        /// <returns>
        /// an instance of the XLeveling mod interface, if it was found; otherwise, null
        /// </returns>
        public static XLeveling Instance(ICoreAPI api) => api.ModLoader.GetModSystem("XLib.XLeveling.XLeveling") as XLeveling;

        /// <summary>
        /// the asset category
        /// </summary>
        public AssetCategory SkillsAssetCategory { get; private set; }

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
        /// Gets the vintage story API.
        /// </summary>
        /// <value>
        /// The API.
        /// </value>
        public ICoreAPI Api { get; private set; }

        /// <summary>
        /// Gets a client and server specific interface.
        /// It takes care of synchronization between server and client and provides an common interface dependent on the side you are on at the moment.
        /// This value is only valid when this interface called the start method. Don't use it in the StartPre method.
        /// </summary>
        /// <value>
        /// The xleveling API.
        /// </value>
        public IXLevelingAPI IXLevelingAPI { get; private set; }

        /// <summary>
        /// Gets the skill set template.
        /// </summary>
        /// <value>
        /// The skill set template.
        /// </value>
        public SkillSetTemplate SkillSetTemplate { get; private set; }

        /// <summary>
        /// Gets the requirement types.
        /// </summary>
        /// <value>
        /// The requirement types.
        /// </value>
        public Dictionary<string, Type> RequirementTypes { get; private set;}

        /// <summary>
        /// Gets the limitations.
        /// </summary>
        /// <value>
        /// The limitations.
        /// </value>
        public Dictionary<string, LimitationRequirement> Limitations { get; private set; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Config Config { get => IXLevelingAPI.Config; }

        /// <summary>
        /// Gets the name of the group name
        /// </summary>
        /// <value>
        /// The name of the group name
        /// </value>
        public string XLibGroupName = "Xlib";

        /// <summary>
        /// Initializes a new instance of the <see cref="XLeveling"/> class.
        /// </summary>
        public XLeveling() : base()
        {
            this.SkillSetTemplate = new SkillSetTemplate();
            this.Api = null;
            this.IXLevelingAPI = null;
            
            this.RequirementTypes = new Dictionary<string, Type>();
            this.Limitations = new Dictionary<string, LimitationRequirement>();
            RegisterRequirement("and", typeof(AndRequirement));
            RegisterRequirement("or", typeof(OrRequirement));
            RegisterRequirement("not", typeof(NotRequirement));
            RegisterRequirement("ability", typeof(AbilityRequirement));
            RegisterRequirement("skill", typeof(SkillRequirement));
            RegisterRequirement("exclusiveAbility", typeof(ExclusiveAbilityRequirement));
            RegisterRequirement("class", typeof(ClassRequirement));
            RegisterRequirement("limitation", typeof(LimitationRequirement));
            RegisterRequirement("daytime", typeof(DaytimeRequirement));
            RegisterRequirement("knowledge", typeof(KnowledgeRequirement));
        }

        /// <summary>
        /// Called during initial mod loading, called before any mod receives the call to Start()
        /// </summary>
        /// <param name="api">The vintage story API.</param>
        public override void StartPre(ICoreAPI api)
        {
            this.Api = api;
            SkillsAssetCategory ??= new AssetCategory("skills", true, EnumAppSide.Universal);

            api.RegisterItemClass("ItemSkillBook", typeof(ItemSkillBook));
        }

        /// <summary>
        /// Initializes the XLeveling interface on the server side.
        /// </summary>
        /// <param name="api">The vintage story server API.</param>
        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            this.IXLevelingAPI = new XLevelingServer(this);
        }

        /// <summary>
        /// Initializes the XLeveling interface on the client side.
        /// </summary>
        /// <param name="api">The vintage story client API.</param>
        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.IXLevelingAPI = new XLevelingClient(this);
        }

        /// <summary>
        /// Side agnostic Start method, called after all mods received a call to StartPre().
        /// </summary>
        /// <param name="api">The vintage story core API.</param>
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
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
            this.Mod.Logger.Event("Initialize skills");
            LoadJsonSkills();

            int skillCount = 0;
            int skillsDisabled = 0;
            int abilityCount = 0;
            int abilitiesDisabled = 0;

            LimitationRequirement specialisations;
            this.Limitations.TryGetValue("specialisations", out specialisations);
            if (specialisations == null)
            {
                specialisations = new LimitationRequirement(1, Lang.GetUnformatted("xlib:specialisations"));
                this.Limitations.Add("specialisations", specialisations);
            }
            foreach (Skill skill in this.SkillSetTemplate.Skills)
            {
                //count skills and abilities
                skillCount++;
                if (!skill.Enabled) skillsDisabled++;

                foreach (Ability ability in skill.Abilities)
                {
                    abilityCount++;
                    if (!ability.Enabled) abilitiesDisabled++;
                }

                if (skill.SpecialisationID >= 0)
                {
                    specialisations.AddAbility(skill[skill.SpecialisationID]);
                }
            }
            this.Mod.Logger.Event("Registered {0} skills ({1} disabled) and {2} abilities ({3} disabled).", skillCount, skillsDisabled, abilityCount, abilitiesDisabled);
        }

        /// <summary>
        /// Registers the skill for the skill set template.
        /// You should register all skills with this method in the StartPre method of your mod.
        /// Also don't forget to add all abilities to the skill.
        /// The method fails if a skill with the name is already in the skill set template or skill is <c>null</c>.
        /// </summary>
        /// <param name="skill">The skill.</param>
        /// <returns>
        /// the id of the added skill if the method succeeds; otherwise, -1
        /// </returns>
        public int RegisterSkill(Skill skill)
        {
            int result = this.SkillSetTemplate.AddSkill(skill);
            skill.XLeveling = this;
            return result;
        }

        /// <summary>
        /// Gets a skill based on its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>
        /// the skill with the given id if it exists; otherwise, <c>null</c>
        /// </returns>
        public Skill GetSkill(int id)
        {
            return id > 0 && id <= this.SkillSetTemplate.Skills.Count ? SkillSetTemplate.Skills[id] : null;
        }

        /// <summary>
        /// Gets a skill by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="allowDisplayName">if set to <c>true</c> the method also looks for skills with this display name.</param>
        /// <returns>
        /// the skill with the given name if it exists; otherwise, <c>null</c>
        /// </returns>
        public Skill GetSkill(string name, bool allowDisplayName = false)
        {
            return this.SkillSetTemplate.FindSkill(name, allowDisplayName);
        }

        /// <summary>
        /// Registers the requirement.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="requirementType">Type of the requirement.</param>
        public void RegisterRequirement(string className, Type requirementType)
        {
            try
            {
                this.RequirementTypes.Add(className, requirementType);
            }
            catch (Exception exception) { this.Mod.Logger.Log(EnumLogType.Error, exception.Message); }
        }

        /// <summary>
        /// Loads the skills from json files.
        /// Primarily to load requirements.
        /// </summary>
        internal void LoadJsonSkills()
        {
            Dictionary<AssetLocation, JToken> tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "skills");
            if (tokens.Count == 0)
            {
                Api.Assets.Reload(SkillsAssetCategory);
                tokens = Api.Assets.GetMany<JToken>(this.Mod.Logger, "skills");
            }
            Dictionary<AssetLocation, JToken>.Enumerator enumerator = tokens.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.Key.RemoveEnding();
                Skill skill = this.SkillSetTemplate.FindSkill(enumerator.Current.Key.GetName());
                if (skill == null) continue;

                TreeAttribute attribute = new JsonObject(enumerator.Current.Value).ToAttribute() as TreeAttribute;
                IEnumerator<KeyValuePair<string, IAttribute>> enumerator2 = attribute.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    if (enumerator2.Current.Key == "classexpmult")
                    {
                        TreeAttribute enumerator3 = (enumerator2.Current.Value as TreeAttribute);
                        foreach (string classname in enumerator3.Keys)
                        {
                            float mult = (float)enumerator3.GetDecimal(classname);
                            skill.ClassExpMultipliers[classname] = mult;
                        }
                        continue;
                    }

                    Ability ability = skill.FindAbility(enumerator2.Current.Key);
                    ArrayAttribute<TreeAttribute> requirements = (enumerator2.Current.Value as TreeAttribute)?.GetAttribute("requirements") as ArrayAttribute<TreeAttribute>;
                    if (ability == null || requirements == null) continue;

                    foreach(TreeAttribute requirementAttributes in requirements.value)
                    {
                        Requirement requirement = this.ResolveRequirment(requirementAttributes);
                        if (requirement != null)
                        {
                            ability.AddRequirement(requirement);
                            //limitations
                            LimitationRequirement limitationRequirement = requirement as LimitationRequirement;
                            if (limitationRequirement != null) limitationRequirement.AddAbility(ability);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves a requirement.
        /// </summary>
        /// <param name="attribute">The attribute.</param>
        /// <returns>
        ///   a requirement if it was successfully resolved; otherwise, null
        /// </returns>
        public Requirement ResolveRequirment(TreeAttribute attribute)
        {
            Type type;
            if (!this.RequirementTypes.TryGetValue(attribute.GetString("code"), out type))
            {
                this.Mod.Logger.Log(EnumLogType.Error, "Can not find a mapping for requirement: {0}", attribute.GetString("code"));
                return null;
            }

            //limitations
            if (type.IsSubclassOf(typeof(LimitationRequirement)) || type == typeof(LimitationRequirement))
            {
                string name = attribute.GetString("name");
                if (name == null) return null;
                LimitationRequirement limitation;
                this.Limitations.TryGetValue(name, out limitation);
                if (limitation == null)
                {
                    limitation = type.GetConstructor(new Type[] { })?.Invoke(new object[] { }) as LimitationRequirement;
                    if (limitation == null)
                    {
                        this.Mod.Logger.Log(EnumLogType.Error, "Can not find a valid constructor for requirement: {0}", attribute.GetString("code"));
                        return null;
                    }
                }
                if (!limitation.FromTree(attribute, this)) return null;
                this.Limitations.Add(name, limitation);
                return limitation;
            }
            {
                Requirement requirement = type.GetConstructor(new Type[] { })?.Invoke(new object[] { }) as Requirement;
                if (requirement == null)
                {
                    this.Mod.Logger.Log(EnumLogType.Error, "Can not find a valid constructor for requirement: {0}", attribute.GetString("code"));
                    return null;
                }

                if (requirement.FromTree(attribute, this)) return requirement;
                return null;
            }
        }

        /// <summary>
        /// Removes requirements.
        /// Uses Config.disabledRequirements to filter requirements by default
        /// </summary>
        /// <param name="requirements">Names of the requirements that should be removed.</param>
        public void RemoveRequirements(List<string> requirements)
        {
            requirements ??= Config?.disabledRequirements;
            if (requirements == null) return;

            foreach (Skill skill in this.SkillSetTemplate.Skills)
            {
                foreach (Ability ability in skill.Abilities)
                {
                    RemoveRequirementsRecursive(ability.Requirements, requirements);
                }
            }
        }

        private void RemoveRequirementsRecursive(List<Requirement> requirements, List<string> remove)
        {
            if (requirements == null || remove == null) return;
            if (remove.Count == 0) return;
            requirements.RemoveAll(
                (Requirement requirement) =>
            {
                if (requirement is NotRequirement notRequirement)
                {
                    return remove.Contains(notRequirement.Requirement.Name);
                }
                return remove.Contains(requirement.Name);
            });

            foreach (Requirement requirement in requirements)
            {
                if (requirement is AndRequirement andRequirement)
                {
                    RemoveRequirementsRecursive(andRequirement.Requirements, remove);
                }
            }
            requirements.RemoveAll((Requirement requirement) => (requirement as AndRequirement)?.Requirements.Count == 0);
        }

        /// <summary>
        /// Creates the description file.
        /// The file will contain the description of all abilities.
        /// </summary>
        public void CreateDescriptionFile()
        {
            StreamWriter file = File.CreateText(GamePaths.Logs + "/xlevelingdescription.txt");
            foreach (Skill skill in this.SkillSetTemplate.Skills)
            {
                file.WriteLine("Skill: " + skill.DisplayName);
                foreach (Ability ability in skill.Abilities)
                {
                    file.WriteLine("Ability: " + ability.DisplayName);
                    file.WriteLine(ability.FormattedDescription(1));
                    file.WriteLine();
                }
                file.WriteLine();
            }
            file.Close();
#if DEBUG
            file = File.CreateText(GamePaths.Logs + "/xlevelingdescriptionmax.txt");
            foreach (Skill skill in this.SkillSetTemplate.Skills)
            {
                file.WriteLine("Skill: " + skill.DisplayName);
                foreach (Ability ability in skill.Abilities)
                {
                    file.WriteLine("Ability: " + ability.DisplayName);
                    file.WriteLine(ability.FormattedDescription(ability.MaxTier));
                    file.WriteLine();
                }
                file.WriteLine();
            }
            file.Close();
#endif
        }
    }//!class XLeveling

    /// <summary>
    /// An interface for common server and client methods.
    /// Takes care of synchronization between server and client.
    /// </summary>
    public interface IXLevelingAPI
    {
        /// <summary>
        /// Gets the player skill set for the given player.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns>
        ///   on client side the local player skill set if the given player is the local one; otherwise, null
        ///   on server side the player skill set of the given player if the player exists; otherwise null
        /// </returns>
        PlayerSkillSet GetPlayerSkillSet(IPlayer player);

        /// <summary>
        /// Adds experience to a player skill.
        /// Use this method to give a player experience.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="experience">The experience.</param>
        /// <param name="informClient">if set to <c>true</c> the server will inform the client. In most cases this should be <c>true</c>. 
        /// Unless you are sure that the method is called on server and client side.</param>
        void AddExperienceToPlayerSkill(IPlayer player, int skillId, float experience, bool informClient = true);

        /// <summary>
        /// Sets the player skill level.
        /// Use this to force set a player skill level.
        /// If you give a player experience the level will be set automatically.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <param name="skillId">The skill identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="informClient">if set to <c>true</c> the server will inform the client.</param>
        void SetPlayerSkillLevel(IPlayer player, int skillId, int level, bool informClient = true);

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
        void SetAbilityTier(IPlayer player, int skillId, int abilityId, int tier, bool informServer = true);

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
        void IncreaseAbilityTier(IPlayer player, int skillId, int abilityId, bool informServer = true);

        /// <summary>
        /// Returns the number of points you need to unlearn a skill
        /// </summary>
        /// <returns>
        ///   the number of points you need to unlearn a skill
        /// </returns>
        int GetPointsForUnlearn();

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        Config Config { get; }
    }//!interface IXLevelingAPI
}//!namespace XLeveling
