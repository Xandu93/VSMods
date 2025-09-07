using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.Server;
using XLib.XLeveling;
using Vintagestory.API.Datastructures;
using CombatOverhaul.Implementations;

namespace XSkills
{
    public class XSkills : ModSystem
    {
        private static Harmony harmony;

        /// <summary>
        /// Gets an instance of this class.
        /// This is only used to get an instance for harmony prepare methods.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        public static XSkills Instance { get; private set; }

        public Dictionary<string, Skill> Skills { get; set; }

        public ICoreAPI Api { get; private set; }
        public XLeveling XLeveling { get; private set; }

        internal static void DoHarmonyPatch(ICoreAPI api)
        {
            if (harmony == null)
            {
                XSkills xskills = api.ModLoader.GetModSystem<XSkills>();

                harmony = new Harmony("XSkillsPatch");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                Type type;

                BlockEntityAnvilPatch.Apply(harmony, api.ClassRegistry.GetBlockEntity("Anvil"));
                BlockEntityOvenPatch.Apply(harmony, api.ClassRegistry.GetBlockEntity("Oven"), xskills);

                //type = api.ClassRegistry.GetBlockEntity("ExpandedOven");
                //if (type != null) BlockEntityOvenPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockEntity("OvenBakingTop");
                if (type != null) BlockEntityOvenPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockEntity("MixingBowl");
                if (type != null) BlockEntityMixingBowlPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetItemClass("ExpandedRawFood");
                if (type != null) ItemExpandedRawFoodPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockClass("BlockSaucepan");
                if (type != null) BlockSaucepanPatch.Apply(harmony, type, xskills);

                type = type?.Assembly.GetType("ACulinaryArtillery.InventoryMixingBowl");
                if (type != null) InventoryMixingBowlPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockEntity("ButcherTable");
                if (type != null) BlockEntityButcherWorkstationPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockEntity("beframerack");
                if (type != null) BEFrameRackPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockClass("hivetop");
                if (type != null) ClayHiveTopPatch.Apply(harmony, type, xskills);

                type = api.ClassRegistry.GetBlockEntity("BlockNestbox");
                if (type != null) BlockEntityNestBoxPatch.Apply(harmony, type, xskills);

                if (api.ModLoader.IsModSystemEnabled("overhaullib"))
                {
                    ApplyCOPatch(xskills);
                }
            }
        }

        /// <summary>
        /// Apply patches for Combat Overhaul.
        /// </summary>
        internal static void ApplyCOPatch(XSkills xskills)
        {
            ItemStackMeleeWeaponStatsPatch.Apply(harmony, typeof(ItemStackMeleeWeaponStats), xskills);
            ItemStackRangedStatsPatch.Apply(harmony, typeof(ItemStackRangedStats), xskills);
        }

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
        public override double ExecuteOrder() => 0.25;

        public XSkills() : base()
        {
            if (Instance == null) Instance = this;
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony?.UnpatchAll("XSkillsPatch");
            harmony = null;
        }

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            this.Api = api;
            this.XLeveling = XLeveling.Instance(this.Api);
            this.Skills = new Dictionary<string, Skill>();

            //skills
            Survival survival = new Survival(api);
            this.Skills.Add(survival.Name, survival);
            Farming farming = new Farming(api);
            this.Skills.Add(farming.Name, farming);
            Digging digging = new Digging(api);
            this.Skills.Add(digging.Name, digging);
            Forestry forestry = new Forestry(api);
            this.Skills.Add(forestry.Name, forestry);
            Mining mining = new Mining(api);
            this.Skills.Add(mining.Name, mining);
            Husbandry husbandry = new Husbandry(api);
            this.Skills.Add(husbandry.Name, husbandry);
            Combat combat = new Combat(api);
            this.Skills.Add(combat.Name, combat);
            Metalworking metalworking = new Metalworking(api);
            this.Skills.Add(metalworking.Name, metalworking);
            Pottery pottery = new Pottery(api);
            this.Skills.Add(pottery.Name, pottery);
            Cooking cooking = new Cooking(api);
            this.Skills.Add(cooking.Name, cooking);

            if (api.World.Config.GetBool("temporalStability"))
            {
                TemporalAdaptation adaptation = new TemporalAdaptation(api);
                this.Skills.Add(adaptation.Name, adaptation);
            }

            api.RegisterEntityBehaviorClass("XSkillsPlayer", typeof(XSkillsPlayerBehavior));
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            (this.Skills["metalworking"] as Metalworking).RegisterAnvil();

            //register 'quality' and 'owner' to be ignored
            string[] temp = new string[GlobalConstants.IgnoredStackAttributes.Length + 2];
            int count = 0;
            for (; count < GlobalConstants.IgnoredStackAttributes.Length; ++count)
            {
                temp[count] = GlobalConstants.IgnoredStackAttributes[count];
            }
            temp[count] = "quality";
            count++;
            temp[count] = "owner";
            GlobalConstants.IgnoredStackAttributes = temp;

            api.RegisterBlockEntityBehaviorClass("XskillsOwnable", typeof(BlockEntityBehaviorOwnable));

            ClassRegistry registry = (api as ServerCoreAPI)?.ClassRegistryNative ?? (api as ClientCoreAPI)?.ClassRegistryNative;
            if (registry != null)
            {
                registry.blockEntityClassnameToTypeMapping["Sapling"] = typeof(XSkillsBlockEntitySapling);
                registry.blockEntityTypeToClassnameMapping[typeof(XSkillsBlockEntitySapling)] = "Sapling";

                if (Api.ModLoader.IsModEnabled("primitivesurvival"))
                    HoeUtil.RegisterItemHoePrimitive(registry);
                HoeUtil.RegisterItemHoe(registry);

                registry.ItemClassToTypeMapping["ItemPlantableSeed"] = typeof(XSkillsItemPlantableSeed);

                //registry.entityBehaviorClassNameToTypeMapping["commandable"] = typeof(XSkillsEntityBehaviorCommandable);
                //registry.entityBehaviorTypeToClassNameMapping[typeof(XSkillsEntityBehaviorCommandable)] = "commandable";
            }
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.XLeveling.CreateDescriptionFile();

            api.Input.RegisterHotKey("xskillshotbarswitch", "Xskills hotbar switch", GlKeys.R);
            api.Input.SetHotKeyHandler("xskillshotbarswitch", OnHotbarSwitch);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);
            DoHarmonyPatch(api);
        }

        public override void AssetsLoaded(ICoreAPI api)
        {
            base.AssetsLoaded(api);
            PatchEntities();

            Survival survival = (this.Skills["survival"] as Survival);
            LimitationRequirement specialisations = this.XLeveling.Limitations["specialisations"];
            if (specialisations != null && survival != null) specialisations.ModifierAbility = survival[survival.AllRounderId];

            foreach (Skill skill in this.Skills.Values)
            {
                skill.DisplayName = Lang.GetUnformatted(skill.DisplayName);
                skill.Group = Lang.GetUnformatted(skill.Group);
                foreach (Ability ability in skill.Abilities)
                {
                    ability.DisplayName = Lang.GetUnformatted(ability.DisplayName);
                    ability.Description = Lang.GetUnformatted(ability.Description);
                }
            }
        }

        public bool OnHotbarSwitch(KeyCombination keys)
        {
            IPlayer player = (this.Api as ICoreClientAPI)?.World.Player;
            XSkillsPlayerInventory inv = player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv == null) return false;
            inv.SwitchInventories();
            return true;
        }

        /// <summary>
        /// Patches entities.
        /// Adds husbandry and combat related behaviors to entities that don't have explicit compatibility.
        /// </summary>
        public void PatchEntities()
        {
            if (Api.Side.IsClient()) return;

            foreach (EntityProperties entity in Api.World.EntityTypes)
            {
                float damage = 0.0f;
                int damageTier = -1;
                float health = 0.0f;
                bool isHostile = false;
                bool isMultipliable = false;
                bool isXskillsEntity = false;
                bool isXskillsAnimal = false;

                isHostile = entity.Server?.SpawnConditions?.Runtime?.Group == "hostile";

                JsonObject[] serverBehaviors = entity.Server?.BehaviorsAsJsonObj;
                JsonObject[] clientBehaviors = entity.Client?.BehaviorsAsJsonObj;

                foreach (JsonObject json in serverBehaviors)
                {
                    string code = json["code"].AsString();
                    if (code == "taskai")
                    {
                        JsonObject[] tasks = json["aitasks"].AsArray();
                        foreach (JsonObject aitask in tasks)
                        {
                            code = aitask["code"].AsString();
                            if (!(code == "meleeattack" || code == "melee")) continue;

                            damage = Math.Max(aitask["damage"].AsFloat(), damage);
                            damageTier = Math.Max(aitask["damageTier"].AsInt(), damageTier);
                        }
                    }
                    else if (code == "health")
                    {
                        health = json["maxhealth"].AsFloat();
                    }
                    else if (code == "multiply")
                    {
                        //is probably an animal
                        isMultipliable = true;
                    }
                    else if (code == "XSkillsEntity")
                    {
                        isXskillsEntity = true;
                        break;
                    }
                    else if (code == "XSkillsAnimal")
                    {
                        isXskillsAnimal = true;
                        break;
                    }
                }

                if (isXskillsEntity || isXskillsAnimal) continue;

                if (!isMultipliable && entity.Code.Path.Contains("male"))
                {
                    //is also an animal when the female version can multiply
                    AssetLocation assetLocation = new AssetLocation(entity.Code.Domain, entity.Code.Path.Replace("male", "female"));
                    EntityProperties female = Api.World.GetEntityType(assetLocation);
                    if (female != null)
                    {
                        JsonObject[] behaviors2 = female.Server?.BehaviorsAsJsonObj ?? female.Client?.BehaviorsAsJsonObj;
                        foreach (JsonObject json in behaviors2)
                        {
                            string code = json["code"].AsString();
                            if (code == "multiply")
                            {
                                //is probably an animal
                                isMultipliable = true;
                            }
                        }
                    }
                }

                if (health > 0.0f && ((damage > 1.0f && damageTier >= 0) || isMultipliable))
                {
                    string str;
                    List<JsonObject> newBehaviors = new List<JsonObject>();

                    float newXp = health * 0.025f + (damage - 1.0f) * 0.05f + damageTier * 0.25f + (isHostile ? 0.25f : 0.0f);
                    if (isMultipliable && !isXskillsAnimal)
                    {
                        newXp *= 0.5f;
                        str = "{\"code\": \"XSkillsAnimal\", \"xp\": " +
                            newXp.ToString(new CultureInfo("en-US")) +
                            ", \"catchable\": \"false\"}";
                        newBehaviors.Add(JsonObject.FromJson(str));
                    }
                    else
                    {
                        str = "{\"code\": \"XSkillsEntity\", \"xp\": " + newXp.ToString(new CultureInfo("en-US")) + "}";
                        newBehaviors.Add(JsonObject.FromJson(str));
                    }

                    JsonObject[] newBehaviorsArray = newBehaviors.ToArray();
                    JsonObject[] newServerBeh = serverBehaviors.AddRangeToArray(newBehaviorsArray);
                    JsonObject[] newClientBeh = clientBehaviors.AddRangeToArray(newBehaviorsArray);

                    if (entity.Server != null) entity.Server.BehaviorsAsJsonObj = newServerBeh;
                    if (entity.Client != null) entity.Client.BehaviorsAsJsonObj = newClientBeh;
                }
            }
        }

        /// <summary>
        /// Adds the tool behavior to all collectibles that are defined as a tool.
        /// </summary>
        //public void AddToolBehaviors()
        //{
        //    foreach(CollectibleObject collectible in Api.World.Collectibles)
        //    {
        //        if (collectible == null) continue;
        //        switch(collectible.Tool)
        //        {
        //            case EnumTool.Pickaxe:
        //            case EnumTool.Axe:
        //            case EnumTool.Shovel:
        //                collectible.HasBehavior(typeof(XSkillsToolBehavior), true);
        //                collectible.CollectibleBehaviors.AddItem(new XSkillsToolBehavior(collectible));
        //                break;
        //            default:
        //                continue;
        //        }
        //    }
        //}
    }//!class XSkills
}//!namespace XSkills