using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using XLib.XLeveling;

namespace XSkills
{
    public class BlockEntityNestBoxPatch : ManualPatch
    {
        public static void Apply(Harmony harmony, Type nestType, XSkills xskills)
        {
            Type patch = typeof(BlockEntityNestBoxPatch);
            PatchMethod(harmony, nestType, patch, "GetDrops");
        }

        public static void GetDropsPostfix(ref ItemStack[] __result, IWorldAccessor world, BlockPos pos, IPlayer byPlayer)
        {
            if (byPlayer == null) return;
            Husbandry husbandry = XLeveling.Instance(world.Api)?.GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            PlayerAbility playerAbility = playerSkill?[husbandry.RancherId];
            if (playerAbility == null) return;
            if (__result.Length == 0) return;

            float experience = 0.0f;

            foreach (ItemStack stack in __result)
            {
                experience += stack.StackSize * 0.1f;

                if (playerAbility.Tier > 0)
                {
                    float quantity = stack.StackSize * (1.0f + playerAbility.FValue(0));
                    int size = (int)quantity + ((quantity - (int)quantity) > world.Rand.NextDouble() ? 1 : 0);
                    stack.StackSize = size;
                }
            }
            playerSkill.AddExperience(experience);
        }
    }//!class BlockEntityNestBoxPatch
}