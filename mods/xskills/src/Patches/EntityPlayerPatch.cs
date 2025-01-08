using HarmonyLib;
using System;
using Vintagestory.API.Common;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(EntityPlayer))]
    public class EntityPlayerPatch
    {
        [HarmonyPatch("LightHsv", MethodType.Getter)]
        public static void Postfix(EntityPlayer __instance, ref byte[] __result)
        {
            //luminiferous
            int lightLevel = __instance.World.BlockAccessor.GetLightLevel(__instance.Pos?.AsBlockPos, EnumLightLevelType.MaxTimeOfDayLight);

            Survival survival = XLeveling.Instance(__instance.Api)?.GetSkill("survival") as Survival;
            if (survival == null) return;
            PlayerAbility ability = __instance.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.LuminiferousId];
            if (ability == null) return;

            byte[] abilityHSV = new byte[3];
            abilityHSV[0] = (byte) ability.Value(0);
            abilityHSV[1] = (byte) ability.Value(1);
            abilityHSV[2] = (byte)(ability.Value(2) * (1.0f - lightLevel / 32.0f));

            byte resultV = 0;

            if (__result == null)
            {
                __result = abilityHSV;
                //resultV = __result[2];
                return;
            }
            else
            {
                //ability = __instance.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.LeadingLightId];
                //if (ability?.Tier > 0)
                //{
                //    resultV = Math.Min((byte)(__result[2] * (1.0f + ability.FValue(0))), (byte)31);
                //}
                //else
                //{
                    resultV = __result[2];
                //}
            }

            float totalval = abilityHSV[2] + resultV;
            float t = resultV / totalval;

            abilityHSV[0] = (byte)(__result[0] * t + abilityHSV[0] * (1 - t));
            abilityHSV[1] = (byte)(__result[1] * t + abilityHSV[1] * (1 - t));
            abilityHSV[2] = Math.Max(resultV, abilityHSV[2]);
            __result = abilityHSV;
        }
    }//!class EntityPlayerPatch
}//!namespace XSkills
