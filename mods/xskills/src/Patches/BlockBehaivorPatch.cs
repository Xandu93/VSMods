using HarmonyLib;
using Vintagestory.API.Common;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockBehavior))]
    internal class BlockBehaivorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnBlockExploded")]
        public static bool OnBlockExplodedPrefix(BlockBehavior __instance)
        {
            return false;
        }
    }//!class BlockBehaivorPatch
}//!namespace XSkills
