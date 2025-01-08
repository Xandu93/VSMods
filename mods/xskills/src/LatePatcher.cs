using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace XSkills
{
    /// <summary>
    /// A late initializer that does things after everything is loaded.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.ModSystem" />
    public class LatePatcher : ModSystem
    {
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
        public override double ExecuteOrder() => 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="LatePatcher"/> class.
        /// </summary>
        public LatePatcher() : base()
        {}

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

            // maybe need things for client?
            if (api.Side.IsClient()) return;

            foreach (EntityProperties entity in api.World.EntityTypes)
            {
                float damage = 0.0f;
                int damageTier = -1;
                float health = 0.0f;
                bool isHostile = false;
                bool isMultipliable = false;
                bool isXskillsEntity = false;
                bool isXskillsAnimal = false;
                float xp = 0.0f;

                isHostile = entity.Server.SpawnConditions?.Runtime?.Group == "hostile" ;

                JsonObject[] behaviors = entity.Server.BehaviorsAsJsonObj;
                foreach (JsonObject json in behaviors)
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
                        xp = json["xp"].AsFloat();
                        isXskillsEntity = true;
                    }
                    else if (code == "XSkillsAnimal")
                    {
                        isXskillsAnimal = true;
                    }
                }

                if (!isMultipliable && entity.Code.Path.Contains("male"))
                {
                    //is also an animal when the female version can multiply
                    AssetLocation assetLocation = new AssetLocation(entity.Code.Domain, entity.Code.Path.Replace("male", "female"));
                    EntityProperties female = api.World.GetEntityType(assetLocation);
                    if (female != null)
                    {
                        JsonObject[] behaviors2 = female.Server.BehaviorsAsJsonObj;
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

                if (health > 0.0f && ((damage > 1.0f && damageTier >= 0) || isMultipliable) && !isXskillsEntity)
                {
                    string str;
                    JsonObject animalBeh = null;
                    JsonObject entityBeh = null;
                    int behCount = 0;

                    float newXp = health * 0.025f + (damage - 1.0f) * 0.05f + damageTier * 0.25f + (isHostile ? 0.25f : 0.0f);
                    if (isMultipliable && !isXskillsAnimal)
                    {
                        newXp *= 0.5f;
                        behCount += 1;
                        str = "{\"code\": \"XSkillsAnimal\", \"xp\": " + 
                            newXp.ToString(new CultureInfo("en-US")) +
                            ", \"catchable\": \"false\"}";
                        animalBeh = JsonObject.FromJson(str);

                    }

                    behCount += 1;
                    str = "{\"code\": \"XSkillsEntity\", \"xp\": " + newXp.ToString(new CultureInfo("en-US")) + "}";
                    entityBeh = JsonObject.FromJson(str);

                    int index = 0;
                    JsonObject[] newEntityBeh = new JsonObject[behaviors.Length + behCount];
                    while (index < behaviors.Length)
                    {
                        newEntityBeh[index] = behaviors[index];
                        index++;
                    }
                    if (entityBeh != null)
                    {
                        newEntityBeh[index] = entityBeh;
                        index++;
                    }
                    if (animalBeh != null)
                    {
                        newEntityBeh[index] = animalBeh;
                    }

                    entity.Server.BehaviorsAsJsonObj = newEntityBeh;
                }
            }
        }
    }//class LatePatcher
}//!namespace XSkills