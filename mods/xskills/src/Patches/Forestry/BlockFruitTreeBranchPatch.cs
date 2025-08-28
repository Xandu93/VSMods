using HarmonyLib;
using System.Reflection;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockFruitTreeBranch class.
    /// </summary>
    [HarmonyPatch(typeof(BlockFruitTreeBranch))]
    public class BlockFruitTreeBranchPatch
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
            xSkills.Skills.TryGetValue("forestry", out skill);
            Forestry forestry = skill as Forestry;

            if (!(forestry?.Enabled ?? false)) return false;
            if (original == null) return true;

            return forestry[forestry.GrafterId]?.Enabled ?? false;
        }

        /// <summary>
        /// Postfix for the TryPlaceBlock method.
        /// Adds the BlockEntityBehaviorValue to the block entity if the player has the grafter ability.
        /// </summary>
        /// <param name="__result">Only runs the method when the base method succeeded.</param>
        /// <param name="world">The world.</param>
        /// <param name="byPlayer">The player.</param>
        /// <param name="blockSel">The block selection</param>
        [HarmonyPostfix]
        [HarmonyPatch("TryPlaceBlock")]
        public static void TryPlaceBlockPostfix(bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (!__result) return;
            BlockEntityFruitTreeBranch be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFruitTreeBranch;
            BlockEntityBehaviorValue beh = new BlockEntityBehaviorValue(be);

            Forestry forestry = world.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("forestry") as Forestry;
            if (forestry == null) return;
            PlayerAbility playerAbility = byPlayer.Entity?.GetBehavior<PlayerSkillSet>()?[forestry.Id]?[forestry.GrafterId];
            if (playerAbility?.Tier <= 0) return;

            beh.Value = 1.0f + playerAbility.FValue(0);
            be.Behaviors.Add(beh);
        }
    }//!class BlockFruitTreeBranchPatch

    /// <summary>
    /// The patch for the BlockEntityFruitTreeBranch class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityFruitTreeBranch))]
    public class BlockEntityFruitTreeBranchPatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            return BlockFruitTreeBranchPatch.Prepare(original);
        }

        /// <summary>
        /// Postfix for the FromTreeAttributes method.
        /// Adds the BlockEntityBehaviorValue to the block entity for loaded blocks.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="tree">The attribute tree.</param>
        [HarmonyPostfix]
        [HarmonyPatch("FromTreeAttributes")]
        public static void FromTreeAttributesPostfix(BlockEntityFruitTreeBranch __instance, ITreeAttribute tree)
        {
            if (__instance.GetBehavior<FruitTreeGrowingBranchBH>() == null) return;
            float value = tree.GetFloat("value", -1.0f);
            if (value <= 0.0f) return;

            BlockEntityBehaviorValue beh = __instance.Behaviors.Find(x => x is BlockEntityBehaviorValue) as BlockEntityBehaviorValue;
            if (beh == null)
            {
                beh = new BlockEntityBehaviorValue(__instance);
                beh.Api = __instance.Api;
                __instance.Behaviors.Add(beh);
            }
            beh.Value = value;
        }
    }//!class BlockEntityFruitTreeBranchPatch

    /// <summary>
    /// A helper struct that saves some tree stats for some method patches.
    /// </summary>
    public struct FruitTreeGrowingState
    {
        public float CuttingGraftChance;
        public float CuttingRootingChance;
        public FruitTreeTypeProperties props;

    }//!struct FruitTreeGrowingState

    /// <summary>
    /// The patch for the FruitTreeGrowingBranchBH class.
    /// </summary>
    [HarmonyPatch(typeof(FruitTreeGrowingBranchBH))]
    public class FruitTreeGrowingBranchBHPatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            return BlockFruitTreeBranchPatch.Prepare(original);
        }

        /// <summary>
        /// A prefix method that is used to save some tree properties and manipulate the default values.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <param name="state">The saved tree properties.</param>
        /// <param name="branchBlock">The branch block.</param>
        public static void CommonPrefix(FruitTreeGrowingBranchBH instance, out FruitTreeGrowingState state, BlockFruitTreeBranch branchBlock)
        {
            state = new FruitTreeGrowingState();
            BlockEntityBehaviorValue behaviorValue = instance.Blockentity.GetBehavior<BlockEntityBehaviorValue>();
            if (behaviorValue == null) return;

            BlockEntityFruitTreeBranch ownBe = instance.Blockentity as BlockEntityFruitTreeBranch;
            branchBlock.TypeProps.TryGetValue(ownBe.TreeType, out var typeprops);
            if (typeprops == null) return;

            state.props = typeprops;
            state.CuttingGraftChance = typeprops.CuttingGraftChance;
            state.CuttingRootingChance = typeprops.CuttingRootingChance;

            typeprops.CuttingGraftChance *= behaviorValue.Value;
            typeprops.CuttingRootingChance *= behaviorValue.Value;
        }

        /// <summary>
        /// A postfix method that is used to restore the old tree properties.
        /// </summary>
        /// <param name="state">The saved tree properties.</param>
        public static void CommonPostfix(FruitTreeGrowingState state)
        {
            if (state.props == null) return;
            state.props.CuttingGraftChance = state.CuttingGraftChance;
            state.props.CuttingRootingChance = state.CuttingRootingChance;
        }

        /// <summary>
        /// Prefix for the GetBlockInfo method.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPrefix(FruitTreeGrowingBranchBH __instance, out FruitTreeGrowingState __state, BlockFruitTreeBranch ___branchBlock)
        {
            CommonPrefix(__instance, out __state, ___branchBlock);
        }

        /// <summary>
        /// Postfix for the GetBlockInfo method.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(FruitTreeGrowingState __state)
        {
            CommonPostfix(__state);
        }

        /// <summary>
        /// Prefix for the TryGrow method.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("TryGrow")]
        public static void TryGrowPrefix(FruitTreeGrowingBranchBH __instance, out FruitTreeGrowingState __state, BlockFruitTreeBranch ___branchBlock)
        {
            CommonPrefix(__instance, out __state, ___branchBlock);
        }

        /// <summary>
        /// Postfix for the TryGrow method.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("TryGrow")]
        public static void TryGrowPostfix(FruitTreeGrowingBranchBH __instance, FruitTreeGrowingState __state)
        {
            CommonPostfix(__state);
        }
    }//!class FruitTreeGrowingBranchBHPatch
}//!namespace XSkills
