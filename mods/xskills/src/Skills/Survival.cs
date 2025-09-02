using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;
using Vintagestory.Server;
using XLib.XLeveling;

namespace XSkills
{
    public class Survival : XSkill
    {
        public int LongLifeId { get; private set; }
        public int HugeStomachId { get; private set; }
        public int WellRestedId { get; private set; }
        public int NudistId { get; private set; }
        public int MeatShieldId { get; private set; }
        public int DiverId { get; private set; }
        public int FeatherFallId { get; private set; }
        public int AllRounderId { get; protected set; }
        public int PhotosynthesisId { get; private set; }
        public int StrongBackId { get; private set; }
        public int OnTheRoadId { get; private set; }
        public int ScoutId { get; private set; }
        public int HealerId { get; private set; }
        //public int LeadingLightId { get; private set; }
        public int SteeplechaserId { get; private set; }
        public int SprinterId { get; private set; }
        public int AbundanceAdaptationId { get; private set; }
        //public int LongArmsId { get; private set; }
        public int SoulboundBagId { get; private set; }
        public int LuminiferousId { get; private set; }
        public int CatEyesId { get; private set; }
        public int MeteorologistId { get; private set; }
        public int LastStandId { get; private set; }

        private ICoreClientAPI capi;
        private NightVisionRenderer nightVisionRenderer;
        private IShaderProgram nightVisionShaderProg;

        public Survival(ICoreAPI api) : base("survival", "xskills:skill-survival", "xskills:group-survival")
        {
            (XLeveling.Instance(api))?.RegisterSkill(this);
            this.Config = new SurvivalSkillConfig();

            // more life
            // 0: base value
            // 1: value per level
            // 2: max value
            LongLifeId = this.AddAbility(new Ability(
                "longlife",
                "xskills:ability-longlife",
                "xskills:abilitydesc-longlife",
                1, 3, new int[] { 5, 1, 15, 10, 2, 30, 10, 2, 50 }));

            // more maximum saturation
            // 0: value
            HugeStomachId = this.AddAbility(new Ability(
                "hugestomach",
                "xskills:ability-hugestomach",
                "xskills:abilitydesc-hugestomach",
                1, 3, new int[] { 500, 1000, 1500 }));

            // more experience
            // 0: value
            // 1: duration
            WellRestedId = this.AddAbility(new Ability(
                "wellrested",
                "xskills:ability-wellrested",
                "xskills:abilitydesc-wellrested",
                1, 2, new int[] { 6, 480, 12, 600}));

            // boni for not wearing clothes
            // 0: walkspeed boni, 1: walkspeed mali
            // 2: health boni, 3: health mali
            // 4: hungerrate boni, 5: hungerrate mali
            // 6: heat resistance boni, 7:  heat resistance mali
            NudistId = this.AddAbility(new Ability(
                "nudist",
                "xskills:ability-nudist",
                "xskills:abilitydesc-nudist",
                3, 2, new int[] {
                    6, 2, 3, 1, 10, 3, 4, 1,
                    10, 2, 5, 1, 20, 4, 8, 2}));

            // lose saturation instead of live
            // 0: absorbed damage
            // 1: saturation cost
            MeatShieldId = this.AddAbility(new Ability(
                "meatshield",
                "xskills:ability-meatshield",
                "xskills:abilitydesc-meatshield",
                3, 3, new int[] { 10, 20, 20, 15, 30, 10 }));

            // lesser oxygen consumption
            // 0: oxygen consumption reduction
            DiverId = this.AddAbility(new Ability(
                "diver",
                "xskills:ability-diver",
                "xskills:abilitydesc-diver",
                3, 2, new int[] { 50, 75 }));

            //// lesser fall damage
            //// 0: flat base value
            //// 1: percentage value
            FeatherFallId = this.AddAbility(new Ability(
                "featherfall",
                "xskills:ability-featherfall",
                "xskills:abilitydesc-featherfall",
                3, 2, new int[] { 1, 10, 2, 20 }));

            // additional specializations
            // 0: value
            AllRounderId = this.AddAbility(new Ability(
                "allrounder",
                "xskills:ability-allrounder",
                "xskills:abilitydesc-allrounder",
                5, 1, new int[] { 1 }));

            // increased health generation in the sunlight
            // 0: value
            // 1: darkness penalty
            PhotosynthesisId = this.AddAbility(new Ability(
                "photosynthesis",
                "xskills:ability-photosynthesis",
                "xskills:abilitydesc-photosynthesis",
                5, 3, new int[] { 15, 25, 40, 50, 50, 50 }));

            // adds a second hotbar
            // 0: value
            StrongBackId = this.AddAbility(new Ability(
                "strongback",
                "xskills:ability-strongback",
                "xskills:abilitydesc-strongback",
                5, 2, new int[] { 3, 6 }));

            // increases movement speed
            // 0: value
            OnTheRoadId = this.AddAbility(new Ability(
                "ontheroad",
                "xskills:ability-ontheroad",
                "xskills:abilitydesc-ontheroad",
                5, 1, new int[] { 10 }));

            // increases movement speed on paths
            // 0: value
            ScoutId = this.AddAbility(new StatAbility(
                "scout", "walkspeed",
                "xskills:ability-scout",
                "xskills:abilitydesc-scout",
                5, 1, new int[] { 5 }));

            // heal over time on healing item use
            // 0: total heal percentage
            // 1: duration
            HealerId = this.AddAbility(new Ability(
                "healer",
                "xskills:ability-healer",
                "xskills:abilitydesc-healer",
                5, 2, new int[] { 25, 30, 50, 30 }));

            //// increases intensity of light sources
            //// 0: percentage bonus
            //LeadingLightId = this.AddAbility(new Ability(
            //    "leadinglight",
            //    "xskills:ability-leadinglight",
            //    "xskills:abilitydesc-leadinglight",
            //    5, 2, new int[] { 50, 90 }));

            // increases step height
            // 0: value
            SteeplechaserId = this.AddAbility(new Ability(
                "steeplechaser",
                "xskills:ability-steeplechaser",
                "xskills:abilitydesc-steeplechaser",
                6, 2, new int[] { 100, 250 }));

            // increased health generation in the sunlight
            // 0: walkspeed increase
            // 1: hungerrate increase
            SprinterId = this.AddAbility(new StatsAbility(
                "sprinter",
                new string[] { "walkspeed", "hungerrate" },
                "xskills:ability-sprinter",
                "xskills:abilitydesc-sprinter",
                6, 2, new int[] { 5, 10, 10, 20 }));

            // increased health generation in the sunlight
            // 0: walkspeed increase
            // 1: healing increase
            AbundanceAdaptationId = this.AddAbility(new StatsAbility(
                "abundanceadaptation",
                new string[] { "healingeffectivness", "hungerrate" },
                "xskills:ability-abundanceadaptation",
                "xskills:abilitydesc-abundanceadaptation",
                6, 2, new int[] { 5, 10, 10, 20 }));

            //increase block selection range
            //0: value
            //LongArmsId = this.AddAbility(new Ability(
            //    "longarms",
            //    "xskills:ability-longarms",
            //    "xskills:abilitydesc-longarms",
            //    6, 1, new int[] { 1 }));

            // items in the strong back inventory are not dropped on death
            SoulboundBagId = -1;
            if (api.World.Config.GetString("deathPunishment", "drop") != "keep")
            {
                SoulboundBagId = this.AddAbility(new Ability(
                    "soulboundbag",
                    "xskills:ability-soulboundbag",
                    "xskills:abilitydesc-soulboundbag",
                    7, 1, new int[] { }));
            }

            // emit light in dark areas
            // 0: color
            // 1: saturation
            // 2: brightness
            LuminiferousId = this.AddAbility(new Ability(
                "luminiferous",
                "xskills:ability-luminiferous",
                "xskills:abilitydesc-luminiferous",
                8, 3, new int[] { 4, 2, 10, 4, 2, 15, 4, 2, 20 }));

            // you can see better in dark areas
            // 0: brightness multiplier
            // 1: adaptation time
            CatEyesId = this.AddAbility(new Ability(
                "cateyes",
                "xskills:ability-cateyes",
                "xskills:abilitydesc-cateyes",
                8, 2, new int[] { 6, 2000, 8, 2000 }));

            // you will receive a weather forecast every day
            // 0: Number of days in the future for which you receive a forecast
            // 1: inaccuracy per day
            MeteorologistId = this.AddAbility(new Ability(
                "meteorologist",
                "xskills:ability-meteorologist",
                "xskills:abilitydesc-meteorologist",
                8, 1, new int[] { 3, 50 }));

            // guarantees to survive if health ratio larger than a random value times value
            // 0: value
            LastStandId = this.AddAbility(new Ability(
                "laststand",
                "xskills:ability-laststand",
                "xskills:abilitydesc-laststand",
                10, 1, new int[] { 200 }));

            this[LongLifeId].OnPlayerAbilityTierChanged += OnLongLife;
            this[HugeStomachId].OnPlayerAbilityTierChanged += OnHugeStomach;
            this[NudistId].OnPlayerAbilityTierChanged += OnNudist;
            this[StrongBackId].OnPlayerAbilityTierChanged += OnStrongBack;
            this[SteeplechaserId].OnPlayerAbilityTierChanged += OnSteeplechaser;
            //this[LongArmsId].OnPlayerAbilityTierChanged += OnLongArms;
            this[LuminiferousId].OnPlayerAbilityTierChanged += OnLuminiferous;

            ClassRegistry registry = (api as ServerCoreAPI)?.ClassRegistryNative ?? (api as ClientCoreAPI)?.ClassRegistryNative;
            if (registry != null)
            {
                registry.RegisterInventoryClass("xskillshotbar", typeof(XSkillsPlayerInventory));
            }
            //GlobalConstants.FoodSpoilSatLossMulHandler += (float spoilState, ItemStack stack, EntityAgent byEntity) => 1.0f;

            this.ExperienceEquation = QuadraticEquation;
            this.ExpBase = 10;
            this.ExpMult = 5.0f;
            this.ExpEquationValue = 0.4f;
            this.ExpLossOnDeath = 0.5f;
            this.MaxExpLossOnDeath = 10.0f;
        }

        //huge stomach
        public void OnHugeStomach(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player?.Entity?.Api.Side == EnumAppSide.Server)
            {
                EntityBehaviorHunger playerHunger = player.Entity.GetBehavior<EntityBehaviorHunger>();
                if (playerHunger != null)
                {
                    float saturationGrowth = (1500 + playerAbility.Value(0)) / playerHunger.MaxSaturation;
                    playerHunger.MaxSaturation = (1500 + playerAbility.Value(0));
                    playerHunger.FruitLevel *= saturationGrowth;
                    playerHunger.GrainLevel *= saturationGrowth;
                    playerHunger.VegetableLevel *= saturationGrowth;
                    playerHunger.DairyLevel *= saturationGrowth;
                    playerHunger.ProteinLevel *= saturationGrowth;
                    playerHunger.Saturation *= saturationGrowth;
                    playerHunger.UpdateNutrientHealthBoost();
                }
            }
        }

        //long life
        public void OnLongLife(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;

            if (player?.Entity?.Api.Side == EnumAppSide.Server)
            {
                EntityBehaviorHealth playerHealth = player.Entity.GetBehavior("health") as EntityBehaviorHealth;
                if (playerHealth != null)
                {
                    player.Entity.Stats.Set("maxhealthExtraPoints", "longlife", 0.01f * playerAbility.SkillDependentValue() * playerHealth.BaseMaxHealth, false);
                }
            }
        }

        //strong back
        public void OnStrongBack(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            if (player == null) return;

            PlayerInventoryManager invMan = player.InventoryManager as PlayerInventoryManager;
            XSkillsPlayerInventory inv = invMan.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv == null)
            {
                try
                {
                    inv = new XSkillsPlayerInventory("xskillshotbar", player.PlayerUID, player.Entity.Api);
                    invMan.Inventories.Add(inv.InventoryID, inv);
                }
                catch (Exception) { return; }
            }
            inv.SetSize(playerAbility.Value(0));
            inv.SwitchCD = (Config as SurvivalSkillConfig).invSwitchCD;
        }

        //nudist
        public void OnNudist(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet?.Player;
            if (player == null) return;

            InventoryCharacter inv = 
                player.InventoryManager?.
                GetOwnInventory(GlobalConstants.characterInvClassName) as InventoryCharacter;
            if (inv == null) return;
            OnNudist(inv);

            XSkillsPlayerBehavior behavior = player.Entity?.GetBehavior<XSkillsPlayerBehavior>();
            if (behavior == null) return;

            if (behavior.NudistSlotNotified == null && playerAbility.Tier > 0)
            {
                behavior.NudistSlotNotified = (int slot) => { OnNudist(inv); };
                inv.SlotModified += behavior.NudistSlotNotified;
            }
            else if (behavior.NudistSlotNotified != null && playerAbility.Tier <= 0)
            {
                inv.SlotModified -= behavior.NudistSlotNotified;
                behavior.NudistSlotNotified = null;
            }
        }

        public static void OnNudist(InventoryCharacter inv)
        {
            Entity entity = inv?.Player?.Entity;
            if (entity == null) return;

            float clothCounter = 0;

            if (inv.Count <= (int)EnumCharacterDressType.ArmorLegs) return;
            if (inv[(int)EnumCharacterDressType.Head].Itemstack != null) clothCounter += 0.5f; //head
            if (inv[(int)EnumCharacterDressType.Shoulder].Itemstack != null) clothCounter += 0.5f; //shoulder
            if (inv[(int)EnumCharacterDressType.UpperBody].Itemstack != null) clothCounter += 1.25f; //upperbody
            if (inv[(int)EnumCharacterDressType.LowerBody].Itemstack != null) clothCounter += 1.5f; //lowerbody
            if (inv[(int)EnumCharacterDressType.Foot].Itemstack != null) clothCounter += 0.5f; //foot
            if (inv[(int)EnumCharacterDressType.Hand].Itemstack != null) clothCounter += 0.5f; //hand
            if (inv[(int)EnumCharacterDressType.Face].Itemstack != null) clothCounter += 0.5f; //face
            if (inv[(int)EnumCharacterDressType.Waist].Itemstack != null) clothCounter += 0.5f; //waist
            if (inv[(int)EnumCharacterDressType.UpperBodyOver].Itemstack != null) clothCounter += 1.25f; //upperbodyover
            if (inv[(int)EnumCharacterDressType.ArmorHead].Itemstack != null) clothCounter += 1.5f; //armorhead
            if (inv[(int)EnumCharacterDressType.ArmorBody].Itemstack != null) clothCounter += 2.0f; //armorbody
            if (inv[(int)EnumCharacterDressType.ArmorLegs].Itemstack != null) clothCounter += 2.0f; //armorlegs

            for (int ii = (int)EnumCharacterDressType.ArmorLegs + 1; ii < inv.Count ; ++ii)
            {
                if (inv[ii].Itemstack != null) clothCounter += 1.5f;
            }
            clothCounter = Math.Clamp(clothCounter, 0.0f, 10.0f);

            Survival survival = XLeveling.Instance(inv.Api)?.GetSkill("survival") as Survival;
            if (survival == null) return;
            PlayerAbility playerAbility = entity.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.NudistId];
            if (playerAbility == null) return;

            entity.Stats.Set("walkspeed", "ability-nudist", playerAbility.FValue(0) - clothCounter * playerAbility.FValue(1), false);
            entity.Stats.Set("maxhealthExtraPoints", "ability-nudist", playerAbility.Value(2) - clothCounter * playerAbility.Value(3), false);
            entity.Stats.Set("hungerrate", "ability-nudist", -playerAbility.FValue(4) + clothCounter * playerAbility.FValue(5), false);

            EntityBehaviorBodyTemperature temp = entity.GetBehavior<EntityBehaviorBodyTemperature>();
            if (temp != null)
                typeof(EntityBehaviorBodyTemperature).GetField("bodyTemperatureResistance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).
                SetValue(temp, inv.Api.World.Config.GetString("bodyTemperatureResistance").ToFloat(0) + Math.Min(playerAbility.Value(7) * clothCounter - playerAbility.Value(6), 0.0f));
        }

        //steeplechaser
        public static void OnSteeplechaser(PlayerAbility playerAbility, int oldTier)
        {
            float mult = 1.0f;
            if (oldTier > 0)
                mult /= 1.0f + playerAbility.Ability.Value(oldTier, 0) * 0.01f;

            if (playerAbility.Tier > 0)
                mult *= 1.0f + playerAbility.Value(0) * 0.01f;

            EntityBehaviorControlledPhysics physics = playerAbility.PlayerSkill.PlayerSkillSet.Player.Entity.GetBehavior<EntityBehaviorControlledPhysics>();
            if (physics == null) return;
            physics.StepHeight *= mult;
        }

        public static void OnLongArms(PlayerAbility playerAbility, int oldTier)
        {
            playerAbility.PlayerSkill.PlayerSkillSet.Player.WorldData.PickingRange = 4.5f + playerAbility.Value(0);
        }

        public static void OnLuminiferous(PlayerAbility playerAbility, int oldTier)
        {
            IPlayer player = playerAbility.PlayerSkill.PlayerSkillSet.Player;
            int value = playerAbility.Value(0) << 16 | playerAbility.Value(1) << 8 | playerAbility.Value(2);
            if (value == 0) player.Entity.WatchedAttributes.RemoveAttribute("ability-luminiferous");
            else player.Entity.WatchedAttributes.SetInt("ability-luminiferous", value);
        }

        public static string GenerateWeatherForecast(ICoreAPI api, EntityPos pos, int days = 3, float inaccuracy = 0.5f)
        {
            WeatherSystemBase weather = api.ModLoader.GetModSystem<WeatherSystemBase>();
            IGameCalendar calendar = api.World.Calendar;
            double today = ((int)calendar.TotalDays) + 0.25;
            const double foo = Math.PI * 1.5f;

            float[] minTemperature = new float[days];
            float[] maxTemperature = new float[days];

            float[] minRainfall = new float[days];
            float[] maxRainfall = new float[days];

            double[] minCloudness = new double[days];
            double[] maxCloudness = new double[days];

            float[] sunrise = new float[days];
            float[] sunset = new float[days];

            EnumPrecipitationType[] precipitationTypes = new EnumPrecipitationType[days];

            for (int ii = 0; ii < days; ++ii)
            {
                float yearRel = calendar.YearRel + (float)ii / calendar.DaysPerYear;
                if (yearRel > 1.0f) { yearRel -= 1.0f; }

                minTemperature[ii] =  100.0f;
                maxTemperature[ii] = -100.0f;

                minRainfall[ii] =  1.0f;
                maxRainfall[ii] = -1.0f;

                minCloudness[ii] =  1.0f;
                maxCloudness[ii] = -1.0f;

                sunrise[ii] = 1.0f;
                sunset[ii]  = 0.0f;

                for (int jj = 0; jj < 4; ++jj)
                {
                    double time = today + ii + 0.25 * jj;

                    ClimateCondition conds = api.World.BlockAccessor.GetClimateAt(pos.AsBlockPos, EnumGetClimateMode.ForSuppliedDateValues, time);

                    float temperature = conds.Temperature;
                    float rainfall = conds.Rainfall;
                    double cloudness = weather.GetRainCloudness(conds, pos.X, pos.Z, time);
                    PrecipitationState precipitation = weather.GetPrecipitationState(pos.XYZ, time);
                    EnumPrecipitationType precipitationType = precipitation.Level > 0.0 ? precipitation.Type : EnumPrecipitationType.Rain;
                    if (precipitationTypes[ii] == EnumPrecipitationType.Rain)
                        precipitationTypes[ii] = precipitationType;

                    minTemperature[ii] = Math.Min(minTemperature[ii], temperature);
                    maxTemperature[ii] = Math.Max(maxTemperature[ii], temperature);

                    minRainfall[ii] = Math.Min(minRainfall[ii], rainfall);
                    maxRainfall[ii] = Math.Max(maxRainfall[ii], rainfall);

                    minCloudness[ii] = Math.Min(minCloudness[ii], cloudness);
                    maxCloudness[ii] = Math.Max(maxCloudness[ii], cloudness);
                }

                float ftime = 0.25f;
                float step = 0.05f;
                int steps = 0;
                int maxsteps = 50;
                while (ftime > 0.0f && ftime < 1.0f && steps < maxsteps)
                {
                    float zenith = calendar.OnGetSolarSphericalCoords(
                        pos.X, pos.Z,
                        yearRel, ftime).ZenithAngle;

                    if (zenith >= foo) ftime -= step;
                    else ftime += step;
                    if (sunrise[ii] == ftime)
                    {
                        if (step < 0.0001f) break;
                        sunrise[ii] += step;
                        step *= 0.1f;
                    }
                    else if (zenith >= foo) sunrise[ii] = Math.Min(sunrise[ii], ftime);
                    ++steps;
                }

                ftime = 0.75f;
                step = 0.05f;
                steps = 0;
                while (ftime > 0.0f && ftime < 1.0f && steps < maxsteps)
                {
                    float zenith = calendar.OnGetSolarSphericalCoords(
                        pos.X, pos.Z,
                        yearRel, ftime).ZenithAngle;

                    if (zenith <= foo) ftime -= step;
                    else ftime += step;
                    if (sunset[ii] == ftime)
                    {
                        if (step < 0.0001f) break;
                        sunset[ii] -= step;
                        step *= 0.1f;
                    }
                    if (zenith <= foo) sunset[ii] = Math.Max(sunset[ii], ftime);
                    ++steps;
                }

                //adding some inaccuracy. later days have more inaccuracy
                minTemperature[ii] += (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1);
                maxTemperature[ii] += (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1);
                if (minTemperature[ii] > maxTemperature[ii])
                    (minTemperature[ii], maxTemperature[ii]) = (maxTemperature[ii], minTemperature[ii]);

                minRainfall[ii] = Math.Clamp(minRainfall[ii] + (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1) * 0.1f, 0.0f, 1.0f);
                maxRainfall[ii] = Math.Clamp(maxRainfall[ii] + (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1) * 0.1f, 0.0f, 1.0f);
                if (minRainfall[ii] > maxRainfall[ii])
                    (minRainfall[ii], maxRainfall[ii]) = (maxRainfall[ii], minRainfall[ii]);

                minCloudness[ii] = Math.Clamp(minCloudness[ii] + (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1) * 0.1f, 0.0f, 1.0f);
                maxCloudness[ii] = Math.Clamp(maxCloudness[ii] + (api.World.Rand.NextSingle() - inaccuracy) * (ii + 1) * 0.1f, 0.0f, 1.0f);
                if (minCloudness[ii] > maxCloudness[ii])
                    (minCloudness[ii], maxCloudness[ii]) = (maxCloudness[ii], minCloudness[ii]);
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Lang.Get("xskills:weather-forecast-from", calendar.PrettyDate()));
            for (int ii = 0; ii < days; ++ii)
            {
                if (ii == 0) builder.AppendLine(Lang.Get("xskills:today"));
                else if (ii == 1) builder.AppendLine(Lang.Get("xskills:tomorrow"));
                else if (ii == 2) builder.AppendLine(Lang.Get("xskills:the-day-after-tomorrow"));
                else builder.AppendLine(Lang.Get("xskills:in-days", ii));
                builder.AppendLine(Lang.Get("xskills:temperature-range", minTemperature[ii], maxTemperature[ii]));
                builder.AppendLine(Lang.Get("xskills:probability-of-precipitation-range", minRainfall[ii], maxRainfall[ii]));
                builder.AppendLine(Lang.Get("xskills:cloudiness-range", minCloudness[ii], maxCloudness[ii]));

                int sunHour = (int)(sunrise[ii] * calendar.HoursPerDay);
                int sunMinute = (int)(sunrise[ii] * calendar.HoursPerDay * 60) % 60;
                builder.AppendLine(Lang.Get("xskills:sunrise-at", string.Format("{0}:{1:00}", sunHour, sunMinute)));

                sunHour = (int)(sunset[ii] * calendar.HoursPerDay);
                sunMinute = (int)(sunset[ii] * calendar.HoursPerDay * 60) % 60;
                builder.AppendLine(Lang.Get("xskills:sunset-at", string.Format("{0}:{1:00}", sunHour, sunMinute)));
            }

            SystemTemporalStability temporal = api.ModLoader.GetModSystem<SystemTemporalStability>();
            if (temporal != null)
            {
                if(temporal.StormData.nextStormTotalDays <= days)
                {
                    switch(temporal.StormData.nextStormStrength)
                    {
                        case EnumTempStormStrength.Light:
                            builder.AppendLine(Lang.Get("A light temporal storm is approaching"));
                            break;
                        case EnumTempStormStrength.Medium:
                            builder.AppendLine(Lang.Get("A medium temporal storm is approaching"));
                            break;
                        case EnumTempStormStrength.Heavy:
                            builder.AppendLine(Lang.Get("A heavy temporal storm is approaching"));
                            break;
                    }
                }
            }

            (api.World as ClientMain)?.eventManager.TriggerNewServerChatLine(GlobalConstants.InfoLogChatGroup, builder.ToString(), EnumChatType.Notification, null);
            return builder.ToString();
        }

        public override void FromConfig(SkillConfig config)
        {
            base.FromConfig(config);
            InitNightVision();
        }

        private void InitNightVision()
        {
            capi = this.XLeveling.Api as ICoreClientAPI;
            if (capi == null) return;

            capi.Event.ReloadShader += LoadShader;
            LoadShader();
            nightVisionRenderer = new NightVisionRenderer(capi, nightVisionShaderProg);
            capi.Event.RegisterRenderer(nightVisionRenderer, EnumRenderStage.AfterFinalComposition);

#if !DEBUG
            if (!(this.Config as SurvivalSkillConfig).allowCatEyesToggle) return;
#endif

            capi.Input.RegisterHotKey("cateyestoggle", "Cat eyes toggle", GlKeys.P, HotkeyType.CharacterControls);
            capi.Input.SetHotKeyHandler("cateyestoggle", (KeyCombination key) =>
            {
                if (capi.World.Player.Entity.Controls.Sneak)
                {
                    if ((nightVisionRenderer.Mode & EnumNightVisionMode.Compress) == 0)
                    {
                        nightVisionRenderer.Mode |= EnumNightVisionMode.Compress;
                        capi.ShowChatMessage("Compress: on");
                    }
                    else
                    {
                        nightVisionRenderer.Mode &= ~EnumNightVisionMode.Compress;
                        capi.ShowChatMessage("Compress: off");
                    }
                }
                else
                {
                    switch (nightVisionRenderer.Mode & EnumNightVisionMode.Filter)
                    {
                        case EnumNightVisionMode.FilterNone:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterGray;
                            capi.ShowChatMessage("Filter: Gray");
                            break;
                        case EnumNightVisionMode.FilterGray:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterSepia;
                            capi.ShowChatMessage("Filter: Sepia");
                            break;
                        case EnumNightVisionMode.FilterSepia:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterGreen;
                            capi.ShowChatMessage("Filter: Green");
                            break;
                        case EnumNightVisionMode.FilterGreen:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterBlue;
                            capi.ShowChatMessage("Filter: Blue");
                            break;
                        case EnumNightVisionMode.FilterBlue:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterRed;
                            capi.ShowChatMessage("Filter: Red");
                            break;
                        case EnumNightVisionMode.FilterRed:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.Deactivated;
                            capi.ShowChatMessage("Filter: Deactivated");
                            break;
                        case EnumNightVisionMode.Deactivated:
                            nightVisionRenderer.Mode = nightVisionRenderer.Mode & ~EnumNightVisionMode.Filter | EnumNightVisionMode.FilterNone;
                            capi.ShowChatMessage("Filter: None");
                            break;
                    }
                }
                LoadShader();
                return true;
            });
        }

        public bool LoadShader()
        {
            nightVisionShaderProg = capi.Shader.NewShaderProgram();
            nightVisionShaderProg.VertexShader = capi.Shader.NewShader(EnumShaderType.VertexShader);
            nightVisionShaderProg.FragmentShader = capi.Shader.NewShader(EnumShaderType.FragmentShader);

            nightVisionShaderProg.VertexShader.Code = GetVertexShaderCode();
            if (nightVisionRenderer != null) nightVisionShaderProg.FragmentShader.Code = GetFragmentShaderCode(nightVisionRenderer.Mode);
            else nightVisionShaderProg.FragmentShader.Code = GetFragmentShaderCode(EnumNightVisionMode.Default);

            capi.Shader.RegisterMemoryShaderProgram("nightvision", nightVisionShaderProg);
            nightVisionShaderProg.Compile();

            if (nightVisionRenderer != null) nightVisionRenderer.Shader = nightVisionShaderProg;
            return true;
        }

        #region Shader Code

        public static string GetVertexShaderCode()
        {
            return @"
                #version 330 core
                #extension GL_ARB_explicit_attrib_location: enable
                layout(location = 0) in vec3 vertex;

                out vec2 uv;

                void main(void)
                {
                    gl_Position = vec4(vertex.xy, 0, 1);
                    uv = (vertex.xy + 1.0) / 2.0;
                }";
        }

        public static string GetFragmentShaderCode(EnumNightVisionMode mode)
        {
            string str = "#version 330 core\r\n";
            if ((mode & EnumNightVisionMode.FilterGray) > 0) str += "#define GRAY 1\r\n";
            if ((mode & EnumNightVisionMode.FilterSepia) > 0) str += "#define SEPIA 1\r\n";
            if ((mode & EnumNightVisionMode.FilterGreen) > 0) str += "#define GREEN 1\r\n";
            if ((mode & EnumNightVisionMode.FilterRed) > 0) str += "#define RED 1\r\n";
            if ((mode & EnumNightVisionMode.FilterBlue) > 0) str += "#define BLUE 1\r\n";
            if ((mode & EnumNightVisionMode.Compress) > 0) str += "#define COMPRESS 1\r\n";
            str += @"
                uniform sampler2D primaryScene;
                uniform float intensity;
                uniform float brightness;
                in vec2 uv;
                out vec4 outColor;
                void main () {

                    vec4 color = texture(primaryScene, uv);
                    //default mix with sepia, optional gray
	                #if GRAY
                        vec3 mixColor = vec3(dot(color.rgb, vec3(0.2126, 0.7152, 0.0722)));
                    #elif SEPIA
	                    vec3 mixColor = vec3(
		                    (color.r * 0.393) + (color.g * 0.769) + (color.b * 0.189),
		                    (color.r * 0.349) + (color.g * 0.686) + (color.b * 0.168),
		                    (color.r * 0.272) + (color.g * 0.534) + (color.b * 0.131));
                    #elif GREEN
	                    vec3 mixColor = vec3(
		                    0.0f,
		                    (color.r * 0.2126) + (color.g * 0.7152) + (color.b * 0.0722),
		                    0.0f);
                    #elif RED
	                    vec3 mixColor = vec3(
		                    (color.r * 0.2126) + (color.g * 0.7152) + (color.b * 0.0722),
		                    0.0f,
		                    0.0f);
                    #elif BLUE
	                    vec3 mixColor = vec3(
		                    0.0f,
		                    0.0f,		                    
                            (color.r * 0.2126) + (color.g * 0.7152) + (color.b * 0.0722));
	                #else
                        vec3 mixColor = color.rgb;
                    #endif

                    float inten = intensity;
	                #if COMPRESS
                        float bright = ((color.r * 0.2126) + (color.g * 0.7152) + (color.b * 0.0722));
                        inten = inten * (1.0 - min(sqrt(bright), 1.0));
                        //inten = inten * (1.0 - min(bright * (1.0 + brightness) * 0.5, 1.0));
                    #endif
                    
                    float scale = 1.0 + brightness * inten;
                    outColor.r = min((color.r * (1.0 - inten) + mixColor.r * inten) * scale, 1.0);
                    outColor.g = min((color.g * (1.0 - inten) + mixColor.g * inten) * scale, 1.0);
                    outColor.b = min((color.b * (1.0 - inten) + mixColor.b * inten) * scale, 1.0);
                    outColor.a = color.a;
                }";
            return str;
        }

        #endregion

    }//!class Survival

    [Flags]
    public enum EnumNightVisionMode
    {
        FilterNone = 0x00,
        FilterSepia = 0x01,
        FilterGray = 0x02,
        FilterGreen = 0x04,
        FilterBlue = 0x08,
        FilterRed = 0x10,
        Deactivated = 0x20,
        Filter = 0x3f,
        Compress = 0x40,
        Default = 0x41
    }

    public class NightVisionRenderer : IRenderer
    {
        MeshRef quadRef;
        ICoreClientAPI capi;
        float nightVisionIntensity;

        internal EnumNightVisionMode Mode { get; set; }
        public float NightVisionBrightness { get; set; }

        public IShaderProgram Shader { get; internal set; }

        public double RenderOrder => 0.85;
        public int RenderRange => 1;

        PlayerAbility ability;

        public NightVisionRenderer(ICoreClientAPI capi, IShaderProgram shader)
        {
            this.capi = capi;
            this.Shader = shader;

            MeshData quadMesh = QuadMeshUtil.GetCustomQuadModelData(-1, -1, 0, 2, 2);
            quadMesh.Rgba = null;

            quadRef = capi.Render.UploadMesh(quadMesh);
            nightVisionIntensity = 0.0f;
            Mode = EnumNightVisionMode.Default;
            NightVisionBrightness = 0.0f;
            ability = null;
        }

        public void Dispose()
        {
            capi.Render.DeleteMesh(quadRef);
            Shader.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            EntityPlayer player = capi.World.Player.Entity;
            if (player == null || deltaTime <= 0.0f) return;

            if (ability == null)
            {
                Survival survival = XLeveling.Instance(player.Api).GetSkill("survival") as Survival;
                if (survival == null) return;
                ability = player.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.CatEyesId];
                if (ability == null) return;
            }
            if (ability.Tier <= 0) return;

            IShaderProgram curShader = capi.Render.CurrentActiveShader;
            curShader?.Stop();
            float adaptationTime = ability.Value(1) / 1000.0f;
            deltaTime = Math.Min(deltaTime, adaptationTime);
            int light = capi.World.BlockAccessor.GetLightLevel(player.Pos.AsBlockPos, EnumLightLevelType.MaxTimeOfDayLight);

            IPlayer[] players = capi.World.GetPlayersAround(player.Pos.XYZ, 32.0f, 32.0f);
            foreach (IPlayer player1 in players)
            {
                int playerBrightness = Math.Clamp(player1.Entity?.LightHsv?[2] ?? 0, (byte) 0, (byte) 32);
                if (playerBrightness == 0) continue;
                int distance = (int)player1.Entity.Pos.DistanceTo(player.Pos);

                light = Math.Max(light, playerBrightness - distance * 2);
            }

            float mult = light == 0 ? 1.6f : 1.0f;
            float destination = (1.0f - light / 16.0f) * mult;
            if (adaptationTime > 0.0f)
                nightVisionIntensity = (nightVisionIntensity * (adaptationTime - deltaTime) + destination * deltaTime) / adaptationTime;
            else
                nightVisionIntensity = destination;

            if (nightVisionIntensity > 0.0f && (Mode & EnumNightVisionMode.Deactivated) == 0)
            {
                Shader.Use();
                capi.Render.GlToggleBlend(true, EnumBlendMode.Overlay);
                capi.Render.GLDisableDepthTest();
                Shader.BindTexture2D("primaryScene", capi.Render.FrameBuffers[(int)EnumFrameBuffer.Primary].ColorTextureIds[0], 0);
                Shader.Uniform("intensity", nightVisionIntensity);
                Shader.Uniform("brightness", NightVisionBrightness + ability.Value(0));
                capi.Render.RenderMesh(quadRef);
                Shader.Stop();
            }
            curShader?.Use();
        }
    };

    [ProtoContract]
    public class SurvivalSkillConfig : CustomSkillConfig
    {
        public override Dictionary<string, string> Attributes
        {
            get
            {
                CultureInfo provider = new CultureInfo("en-US");

                Dictionary<string, string> result = new Dictionary<string, string>();
                result.Add("invSwitchCD", this.invSwitchCD.ToString(provider));
                result.Add("allowCatEyesToggle", this.allowCatEyesToggle.ToString(provider));
                return result;
            }
            set
            {
                string str;
                NumberStyles styles = NumberStyles.Any;
                CultureInfo provider = new CultureInfo("en-US");

                value.TryGetValue("invSwitchCD", out str);
                if (str != null) float.TryParse(str, styles, provider, out this.invSwitchCD);

                value.TryGetValue("allowCatEyesToggle", out str);
                if (str != null) bool.TryParse(str, out this.allowCatEyesToggle);
            }
        }

        [ProtoMember(1)]
        [DefaultValue(3.0f)]
        public float invSwitchCD = 3.0f;

        [ProtoMember(2)]
        [DefaultValue(false)]
        public bool allowCatEyesToggle = false;
    }//!class CombatSkillConfig
}//!namespace XSkills
