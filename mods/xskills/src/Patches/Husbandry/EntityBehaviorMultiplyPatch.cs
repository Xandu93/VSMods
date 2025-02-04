using HarmonyLib;
using System;
using System.Reflection;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(EntityBehaviorMultiply))]
    public static class EntityBehaviorMultiplyPatch
    {
        public static float GetPregnancyDays(this EntityBehaviorMultiply multiply)
        {
            return multiply.entity.WatchedAttributes.GetTreeAttribute("multiply").GetFloat("pregnancyDays", 3.0f);
        }

        public static void SetPregnancyDays(this EntityBehaviorMultiply multiply, float days)
        {
            multiply.entity.WatchedAttributes.GetTreeAttribute("multiply").SetFloat("pregnancyDays", days);
        }


        [HarmonyPatch("SpawnQuantityMin", MethodType.Getter)]
        public static void Postfix1(EntityBehaviorMultiply __instance, ref float __result)
        {
            IPlayer player = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>()?.Feeder;
            if (player == null) return;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;
            PlayerAbility playerAbility = playerSkill[husbandry.MassHusbandryId];
            if (playerAbility == null) return;

            float multiplier = 1.0f +
                playerAbility.FValue(0) +
                playerAbility.FValue(1) * playerSkill.Level +
                playerAbility.FValue(2) * __instance.entity.WatchedAttributes.GetInt("generation");
            multiplier = Math.Min(multiplier, playerAbility.FValue(3));
            __result *= multiplier;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Initialize")]
        public static void InitializePostfix(EntityBehaviorMultiply __instance)
        {
            if (__instance.entity.Api.Side != EnumAppSide.Server) return;
            float pregnancyDays = (float)(typeof(EntityBehaviorMultiply).GetProperty("PregnancyDays", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance));
            __instance.SetPregnancyDays(pregnancyDays);
        }

        [HarmonyPatch("SpawnQuantityMax", MethodType.Getter)]
        public static void Postfix2(EntityBehaviorMultiply __instance, ref float __result)
        {
            IPlayer player = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>()?.Feeder;
            if (player == null) return;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;
            PlayerAbility playerAbility = playerSkill[husbandry.MassHusbandryId];
            if (playerAbility == null) return;

            float multiplier = 1.0f +
                playerAbility.FValue(0) +
                playerAbility.FValue(1) * playerSkill.Level +
                playerAbility.FValue(2) * __instance.entity.WatchedAttributes.GetInt("generation");
            multiplier = Math.Min(multiplier, playerAbility.FValue(3));
            __result *= multiplier;
        }

        [HarmonyPrefix]
        [HarmonyPatch("CheckMultiply")]
        public static void CheckMultiplyPrefix(EntityBehaviorMultiply __instance, out bool __state)
        {
            __state = __instance.IsPregnant;
        }

        [HarmonyPostfix]
        [HarmonyPatch("CheckMultiply")]
        public static void CheckMultiplyPostfix(EntityBehaviorMultiply __instance, bool __state)
        {
            if (__state || !__instance.IsPregnant) return;
            IPlayer player = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>()?.Feeder;
            if (player == null) return;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;
            PlayerAbility playerAbility = playerSkill[husbandry.BreederId];
            if (playerAbility == null) return;

            float multiplier =
                playerAbility.FValue(0) +
                playerAbility.FValue(1) * playerSkill.Level +
                playerAbility.FValue(2) * __instance.entity.WatchedAttributes.GetInt("generation");
            multiplier = Math.Min(multiplier, playerAbility.FValue(3));

            float pregnancyDays = __instance.GetPregnancyDays();
            __instance.TotalDaysPregnancyStart = __instance.TotalDaysPregnancyStart - pregnancyDays * multiplier;
        }

        //original: https://github.com/anegostudios/vsessentialsmod/blob/master/Entity/Behavior/BehaviorMultiply.cs
        [HarmonyPatch("GetInfoText")]
        public static bool Prefix(EntityBehaviorMultiply __instance, StringBuilder infotext)
        {
            IPlayer player = (__instance.entity?.World as IClientWorldAccessor)?.Player;
            if (player == null) return true;
            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return true;
            PlayerAbility playerAbility = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id][husbandry.BreederId];
            if (!(playerAbility?.Tier > 0)) return true;

            if (__instance.IsPregnant)
            {
                float pregnancyDays = __instance.GetPregnancyDays();
                double pregnantDays = __instance.entity.World.Calendar.TotalDays - __instance.TotalDaysPregnancyStart;
                infotext.AppendLine(Lang.Get("Is pregnant") + string.Format(" ({0:N1}/{1:N1})", pregnantDays, pregnancyDays));
            }
            else if (__instance.entity.Alive)
            {
                ITreeAttribute tree = __instance.entity.WatchedAttributes.GetTreeAttribute("hunger");
                if (tree != null)
                {
                    float saturation = tree.GetFloat("saturation", 0);
                    infotext.AppendLine(Lang.Get("Portions eaten: {0}", saturation));
                }

                double daysLeft = __instance.TotalDaysCooldownUntil - __instance.entity.World.Calendar.TotalDays;
                if (daysLeft <= 0) infotext.AppendLine(Lang.Get("Ready to mate"));
                else infotext.AppendLine(Lang.Get("xskills:ready-to-mate", daysLeft));
            }
            return false;
        }
    }//!class EntityBehaviorMultiplyPatch
}//!namespace XSkills
