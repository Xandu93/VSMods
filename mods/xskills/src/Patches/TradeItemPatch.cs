using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(TradeItem))]
    internal class TradeItemPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("Resolve")]
        public static void ResolvePostfix(ResolvedTradeItem __result, IWorldAccessor world)
        {
            ItemStack stack = __result.Stack;
            if (stack == null) return;

            if (stack.Collectible is ItemSkillBook)
            {
                if (stack.Attributes == null) return;
                if (stack.Attributes.HasAttribute("experience")) return;
                XLeveling xLeveling = XLeveling.Instance(world.Api);
                if (xLeveling == null) return;
                string skillName = stack.Attributes.GetString("skill");
                if (skillName == null) return;
                Skill skill = xLeveling.GetSkill(skillName);
                if (skill == null) return;

                float minexperience = (float)stack.Attributes.GetDecimal("minexperience", skill.ExpBase * 0.1f);
                float maxexperience = (float)stack.Attributes.GetDecimal("maxexperience", skill.ExpBase * 1.5f);

                float mult = (float)world.Api.World.Rand.NextDouble();
                float experience = minexperience + (maxexperience - minexperience) * mult;

                float average = (minexperience + maxexperience) / 2.0f;
                float priceperexp = __result.Price / average;

                __result.Price = (int)(priceperexp * experience + 1.5f);
                stack.Attributes.SetFloat("experience", experience);
                stack.Attributes.RemoveAttribute("minexperience");
                stack.Attributes.RemoveAttribute("maxexperience");
            }
        }
    }
}
