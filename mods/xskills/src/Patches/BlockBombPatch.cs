using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockBomb))]
    public class BlockBombPatch
    {
        [HarmonyPatch("OnBlockExploded")]
        public static void Postfix(BlockBomb __instance, IWorldAccessor world, BlockPos pos, BlockPos explosionCenter, EnumBlastType blastType)
        {
            EnumHandling handled = EnumHandling.PassThrough;

            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                behavior.OnBlockExploded(world, pos, explosionCenter, blastType, ref handled);
                if (handled == EnumHandling.PreventSubsequent) break;
            }
        }
    }//!class BlockBombPatch
}//!namespace XSkills
