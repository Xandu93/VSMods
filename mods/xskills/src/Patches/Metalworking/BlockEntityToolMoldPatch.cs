using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityToolMold class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityToolMold))]
    public class BlockEntityToolMoldPatch
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
            Skill skill;
            xSkills.Skills.TryGetValue("metalworking", out skill);
            Metalworking metalworking = skill as Metalworking;

            if (!(metalworking?.Enabled ?? false)) return false;
            if (original == null) return true;

            switch (original.Name)
            {
                case "GetBlockInfo":
                    return
                        metalworking[metalworking.SmelterId].Enabled;
                default:
                    return
                        true;
            }
        }

        /// <summary>
        /// Postfix for the GetBlockInfo method.
        /// Just adds a line to the block info.
        /// </summary>
        /// <param name="forPlayer"></param>
        /// <param name="dsc"></param>
        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(BlockEntityToolMold __instance, IPlayer forPlayer, StringBuilder dsc)
        {
            PlayerSkillSet skillSet = forPlayer.Entity.GetBehavior<PlayerSkillSet>();
            if (skillSet == null) return;
            Metalworking metalworking = skillSet.XLeveling?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return;

            PlayerAbility playerAbility = skillSet[metalworking.Id]?[metalworking.SmelterId];
            if (playerAbility == null) return;
            if (playerAbility.Tier <= 0) return;

            //return if one product is meltable
            if (__instance.MetalContent != null)
            {
                ItemStack[] moldedStacks = __instance.GetMoldedStacks(__instance.MetalContent);
                foreach (ItemStack itemStack in moldedStacks)
                {
                    if (itemStack.Collectible.CombustibleProps != null) return;
                }
            }
            else
            {
                ItemStack dummy = new ItemStack(__instance.Api.World.GetItem(new AssetLocation("game", "ingot-copper")));
                ItemStack[] moldedStacks = __instance.GetMoldedStacks(dummy);
                foreach (ItemStack itemStack in moldedStacks)
                {
                    if (itemStack.Collectible.CombustibleProps != null) return;
                }
            }

            dsc.Append(Lang.Get("xskills:resourcereduction", playerAbility.FValue(0)));
        }
    }
}
