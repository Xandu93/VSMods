using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XEffects;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockEntityBed))]
    public class BlockEntityBedPatch
    {
        [HarmonyPatch("RestPlayer")]
        public static void Prefix(BlockEntityBed __instance, double ___hoursTotal/*, float ___sleepEfficiency*/)
        {
            double hoursPassed = __instance.Api.World.Calendar.TotalHours - ___hoursTotal;

            if (hoursPassed > 0)
            {
                XSkillsPlayerBehavior pbh = __instance.MountedBy?.GetBehavior("XSkillsPlayer") as XSkillsPlayerBehavior;
                if (pbh == null) return;

                //float sleepEff = ___sleepEfficiency - 1f / 12;
                pbh.HoursSlept += (float)hoursPassed;
            }
        }

        [HarmonyPatch("DidUnmount")]
        public static void Postfix(BlockEntityBed __instance, EntityAgent entityAgent)
        {
            if (entityAgent == null) return;

            EntityBehaviorTiredness ebt = entityAgent.GetBehavior("tiredness") as EntityBehaviorTiredness;
            XSkillsPlayerBehavior pbh = entityAgent.GetBehavior("XSkillsPlayer") as XSkillsPlayerBehavior;
            if (ebt == null || pbh == null) return;

            float rest = 1.0f - ebt.Tiredness / __instance.Api.World.Calendar.HoursPerDay;
            if (rest < 0.0f) return;

            Survival survival = XLeveling.Instance(__instance.Api).GetSkill("survival") as Survival;
            if (survival == null) return;
            PlayerAbility playerAbility = entityAgent.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.WellRestedId];
            if (playerAbility == null) return;
            if (playerAbility.Tier < 1) return;

            XEffectsSystem effectSystem = __instance.Api.ModLoader.GetModSystem<XEffectsSystem>();
            Effect effect = effectSystem?.CreateEffect("rested");
            if (effect == null || pbh.HoursSlept < 1.0f) return;
            effect.Update(rest * playerAbility.FValue(0));
            effect.Duration = playerAbility.Value(1) * Math.Min(pbh.HoursSlept / 8.0f, 1.0f);
            pbh.HoursSlept = 0.0f;

            entityAgent.AddEffect(effect);
        }
    }//!class BlockEntityBedPatch
}//!namespace XSkills
