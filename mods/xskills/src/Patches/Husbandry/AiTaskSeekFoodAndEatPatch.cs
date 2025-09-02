using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(AiTaskSeekFoodAndEat))]
    public class AiTaskSeekFoodAndEatPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("ContinueExecute")]
        public static void ContinueExecutePrefix(AiTaskSeekFoodAndEat __instance, IAnimalFoodSource ___targetPoi)
        {
            BlockEntityTrough trough = ___targetPoi as BlockEntityTrough;
            XSkillsAnimalBehavior beh = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (beh == null || trough == null) return;
            beh.Feeder = trough.GetOwner();
        }

        [HarmonyPrefix]
        [HarmonyPatch("FinishExecute")]
        public static void FinishExecutePrefix(AiTaskSeekFoodAndEat __instance, float ___quantityEaten)
        {
            if (___quantityEaten < 1.0f) return;
            XSkillsAnimalBehavior animal = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (animal == null) return;
            IPlayer player = animal.Feeder;
            if (player == null) return;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(0.025f * animal.XP);

            //feeder
            PlayerAbility playerAbility = playerSkill[husbandry.FeederId];
            if (playerAbility == null) return;

            if (__instance.entity.World.Rand.NextDouble() < playerAbility.FValue(3))
            {
                int generation = __instance.entity.WatchedAttributes.GetInt("generation") + 1;
                if (generation < playerAbility.Value(4))
                {
                    __instance.entity.WatchedAttributes.SetInt("generation", generation);
                    __instance.entity.WatchedAttributes.MarkPathDirty("generation");
                }
            }
        }
    }//!class AiTaskSeekFoodAndEatPatch
}//!namespace XSkills
