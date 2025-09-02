using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockPan))]
    public class BlockPanPatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            XSkills xSkills = XSkills.Instance;
            if (xSkills == null) return false;
            xSkills.Skills.TryGetValue("combat", out Skill skill);
            Digging digging = skill as Digging;

            if (!(digging?.Enabled ?? false)) return false;
            if (original == null) return digging[digging.QuickPanId].Enabled || digging[digging.GoldDiggerId].Enabled;

            switch (original.Name)
            {
                case "OnHeldInteractStep":
                case "OnHeldInteractStop":
                    return digging[digging.QuickPanId].Enabled;
                case "CreateDrop":
                    return digging[digging.GoldDiggerId].Enabled;
                default:
                    break;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStep")]
        public static void OnHeldInteractStepPrefix(BlockPan __instance, ref float secondsUsed, EntityAgent byEntity)
        {
            Digging digging = XLeveling.Instance(byEntity.Api).GetSkill("digging") as Digging;
            if (digging == null) return;
            PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[digging.Id]?[digging.QuickPanId];
            if (playerAbility == null) return;
            secondsUsed *= 1.0f + playerAbility.FValue(0);
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop")]
        public static void OnHeldInteractStopPrefix(BlockPan __instance, ref float secondsUsed, EntityAgent byEntity)
        {
            Digging digging = XLeveling.Instance(byEntity.Api).GetSkill("digging") as Digging;
            if (digging == null) return;
            PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[digging.Id]?[digging.QuickPanId];
            if (playerAbility == null) return;
            secondsUsed *= 1.0f + playerAbility.FValue(0);
        }

        [HarmonyPrefix]
        [HarmonyPatch("CreateDrop")]
        public static bool CreateDropPrefix(BlockPan __instance, EntityAgent byEntity, string fromBlockCode)
        {
            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return true;
            Digging digging = XLeveling.Instance(byEntity.Api).GetSkill("digging") as Digging;
            if (digging == null) return true;
            PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[digging.Id]?[digging.GoldDiggerId];
            if (playerAbility == null) return true;
            playerAbility.PlayerSkill.AddExperience(0.25f);

            ItemStack[] drops = digging.GeneratePanDrops(byEntity, fromBlockCode, 1.0f + playerAbility.SkillDependentFValue(), 1);

            foreach (ItemStack drop in drops)
            {
                if (!player.InventoryManager.TryGiveItemstack(drop, true))
                {
                    byEntity.Api.World.SpawnItemEntity(drop, byEntity.ServerPos.XYZ);
                }
            }
            return false;
        }
    }
}