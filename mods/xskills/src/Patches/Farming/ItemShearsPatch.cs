using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(ItemScythe))]
    public class ItemScythePatch
    {
        [HarmonyPatch("MultiBreakQuantity", MethodType.Getter)]
        public static void Postfix(ref int __result)
        {
            __result = (int)(__result * Farming.MultiBreakMultiplier);
        }
    }//!ItemScythePatch

    [HarmonyPatch(typeof(ItemShears))]
    public class ItemShearsPatch
    {
        [HarmonyPatch("MultiBreakQuantity", MethodType.Getter)]
        public static void Postfix(ref int __result)
        {
            __result = (int)(__result * Farming.MultiBreakMultiplier);
        }

        [HarmonyPatch("OnBlockBrokenWith")]
        public static void Prefix(Entity byEntity)
        {
            Farming farming = XLeveling.Instance(byEntity?.Api)?.GetSkill("farming") as Farming;
            if (farming == null) return;
            PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[farming.Id]?[farming.BrightHarvestsId];
            if (playerAbility == null) return;
            Farming.MultiBreakMultiplier = 1.0f + playerAbility.FValue(0);
        }

        [HarmonyPrefix]
        [HarmonyPatch("GetNearblyMultibreakables")]
        public static bool Prefix(ItemShears __instance, out OrderedDictionary<BlockPos, float> __result, IWorldAccessor world, BlockPos pos, Vec3d hitPos)
        {
            __result = new OrderedDictionary<BlockPos, float>();
            int range = __instance.MultiBreakQuantity > 9 ? 2 : 1;

            for (int dx = -range; dx <= range; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dz = -range; dz <= range; dz++)
                    {
                        if (dx == 0 && dy == 0 && dz == 0) continue;

                        BlockPos dpos = pos.AddCopy(dx, dy, dz);
                        if (__instance.CanMultiBreak(world.BlockAccessor.GetBlock(dpos)))
                        {
                            __result.Add(dpos, hitPos.SquareDistanceTo(dpos.X + 0.5, dpos.Y + 0.5, dpos.Z + 0.5));
                        }
                    }
                }
            }

            return false;
        }
    }//!class ItemShearsPatch
}//!namespace XSkills
