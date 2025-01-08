using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityFruitTreePart class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityFruitTreePart))]
    public class BlockEntityFruitTreePartPatch
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
            xSkills.Skills.TryGetValue("farming", out skill);
            Farming farming = skill as Farming;

            if (!(farming?.Enabled ?? false)) return false;
            if (original == null) return true;

            return farming[farming.OrchardistId]?.Enabled ?? false;
        }

        /// <summary>
        /// Prefix for the OnBlockInteractStop method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">if set to <c>true</c> the tree was ripe</param>
        [HarmonyPrefix]
        [HarmonyPatch("OnBlockInteractStop")]
        public static void OnBlockInteractStopPrefix(BlockEntityFruitTreePart __instance, out bool __state)
        {
            __state = (__instance.FoliageState == EnumFoliageState.Ripe);
        }

        /// <summary>
        /// Postfix for the OnBlockInteractStop method.
        /// Gives extra drops to the player.
        /// The tree must change its foliage state from ripe to something else.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">if set to <c>true</c> the tree was ripe</param>
        /// <param name="byPlayer">the player</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnBlockInteractStop")]
        public static void OnBlockInteractStopPostfix(BlockEntityFruitTreePart __instance, bool __state, IPlayer byPlayer)
        {
            if (__instance.FoliageState == EnumFoliageState.Ripe || !__state) return;

            Farming farming = __instance.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("farming") as Farming;
            if (farming == null) return;
            PlayerSkill playerSkill = byPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[farming.Id];
            if (playerSkill == null) return;

            float exp = (farming.Config as FarmingConfig)?.treeHarvestExp ?? 0.0f;
            if (exp > 0.0f)
            {
                playerSkill.AddExperience(exp);
            }

            PlayerAbility playerAbility = playerSkill[farming.OrchardistId];
            if (playerAbility?.Tier <= 0) return;
            float dropChance = playerAbility.SkillDependentFValue();

            var loc = AssetLocation.Create(__instance.Block.Attributes["branchBlock"].AsString(), __instance.Block.Code.Domain);
            var block = __instance.Api.World.GetBlock(loc) as BlockFruitTreeBranch;
            var drops = block.TypeProps[__instance.TreeType].FruitStacks;

            foreach (var drop in drops)
            {
                ItemStack stack = drop.GetNextItemStack(dropChance);
                if (stack == null) continue;

                if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
                {
                    __instance.Api.World.SpawnItemEntity(stack, byPlayer.Entity.Pos.XYZ.Add(0, 0.5, 0));
                }

                if (drop.LastDrop) break;
            }
        }
    }//!class BlockEntityFruitTreePartPatch
}//!namespace XSkills
