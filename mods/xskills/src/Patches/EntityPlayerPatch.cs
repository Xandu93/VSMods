using HarmonyLib;
using System;
using Vintagestory.API.Common;

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
            int luminiferous = __instance.WatchedAttributes.GetInt("ability-luminiferous");

            byte[] abilityHSV = new byte[3];
            abilityHSV[0] = (byte)(luminiferous >> 16 & 0xff);
            abilityHSV[1] = (byte)(luminiferous >>  8 & 0xff);
            abilityHSV[2] = (byte)((luminiferous & 0xff) * (1.0f - lightLevel / 32.0f));
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
