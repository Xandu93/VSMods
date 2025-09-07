using HarmonyLib;
using Vintagestory.API.Common;

namespace XLib.XEffects
{
    [HarmonyPatch(typeof(Block))]
    [HarmonyPatch("OnGettingBroken")]
    internal class BlockOnGettingBrokenPatch
    {
        public static void Prefix(IPlayer player, ItemSlot itemslot, ref float dt)
        {
            EnumTool? tool = itemslot?.Itemstack?.Item?.Tool;
            if (tool == null) return;
            AffectedEntityBehavior affected = player?.Entity?.GetBehavior<AffectedEntityBehavior>();
            if (affected == null) return;

            float mult = affected.GetMiningSpeedMultiplier(tool.Value);
            if (mult > 0.0f)
            {
                dt *= mult;
            }
        }
    }//!BlockOnGettingBrokenPatch
}//!namespace XLib.XEffects