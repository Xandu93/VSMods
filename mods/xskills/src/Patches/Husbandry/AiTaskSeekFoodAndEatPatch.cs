using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(AiTaskSeekFoodAndEat))]
    public class AiTaskSeekFoodAndEatPatch
    {
        [HarmonyPatch("ContinueExecute")]
        public static void Prefix(AiTaskSeekFoodAndEat __instance, IAnimalFoodSource ___targetPoi)
        {
            BlockEntityTrough Trough = ___targetPoi as BlockEntityTrough;
            XSkillsAnimalBehavior beh = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (beh == null || Trough == null) return;
            beh.Feeder = Trough.GetOwner();
        }

        [HarmonyPatch("FinishExecute")]
        public static void Prefix(AiTaskSeekFoodAndEat __instance, float ___quantityEaten)
        {
            if (___quantityEaten < 1.0f) return;
            IPlayer player = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>()?.Feeder;
            if (player == null) return;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.World.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;

            //experience
            playerSkill.AddExperience(0.025f);

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
