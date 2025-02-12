using HarmonyLib;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public static class BlockEntityTroughExtension
    {
        public static IPlayer GetOwner(this BlockEntityTrough trough)
        {
            string owner = trough.Inventory[0].Itemstack?.Attributes.GetString("owner");
            if (owner == null) return null;
            return trough.Api.World.PlayerByUid(owner);
        }

        public static void SetOwner(this BlockEntityTrough trough, IPlayer owner)
        {
            trough.Inventory[0].Itemstack?.Attributes.SetString("owner", owner.PlayerUID);
        }
    }//!class BlockEntityTroughExtension

    [HarmonyPatch(typeof(BlockEntityTrough))]
    public class BlockEntityTroughPatch
    {
        [HarmonyPatch("OnInteract")]
        public static void Prefix(BlockEntityTrough __instance, out int __state)
        {
            __state = __instance.Inventory[0].Itemstack?.StackSize ?? 0;
        }

        [HarmonyPatch("OnInteract")]
        public static void Postfix(BlockEntityTrough __instance, int __state, IPlayer byPlayer)
        {
            if (__state >= (__instance.Inventory[0].Itemstack?.StackSize ?? 0)) return;
            __instance.SetOwner(byPlayer);
        }

        [HarmonyPatch("ConsumeOnePortion")]
        public static bool Prefix(BlockEntityTrough __instance, out float __result)
        {
            __result = 0.0f;
            IPlayer player = __instance.GetOwner();
            if (player == null) return true;

            Husbandry husbandry = XLeveling.Instance(__instance.Api)?.GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return true;
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return true;

            //feeder
            PlayerAbility playerAbility = playerSkill[husbandry.FeederId];
            if (playerAbility == null) return true;

            if (__instance.Api.World.Rand.NextDouble() < playerAbility.SkillDependentFValue())
            {
                __result = 1.0f;
                return false;
            }

            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(BlockEntityTrough __instance, StringBuilder dsc)
        {
            IPlayer player = __instance.GetOwner();
            if (player == null) return;
            dsc.AppendLine(Lang.Get("xskills:owner-desc", player.PlayerName));
        }
    }//!class BlockEntityTroughPatch
}//!namespace XSkills
