using HarmonyLib;
using System;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityOven class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class BlockEntityOvenPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony lib.</param>
        /// <param name="ovenType">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type ovenType, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("cooking", out skill);
            Cooking cooking = skill as Cooking;

            if (!(cooking?.Enabled ?? false)) return;
            Type patch = typeof(BlockEntityOvenPatch);

            PatchMethod(harmony, ovenType, patch, "GetBlockInfo");

            if (
                cooking[cooking.CanteenCookId].Enabled ||
                cooking[cooking.FastFoodId].Enabled ||
                cooking[cooking.WellDoneId].Enabled ||
                cooking[cooking.DilutionId].Enabled ||
                cooking[cooking.GourmetId].Enabled ||
                cooking[cooking.HappyMealId].Enabled)
            {
                PatchMethod(harmony, ovenType, patch, "OnInteract");
                PatchMethod(harmony, ovenType, patch, "IncrementallyBake");
            }
        }

        /// <summary>
        /// Postfix for the OnInteract method.
        /// Sets the owner of the oven.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="byPlayer">The player.</param>
        public static void OnInteractPostfix(BlockEntity __instance, IPlayer byPlayer)
        {
            BlockEntityBehaviorOwnable ownable = __instance.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            ownable.Owner = byPlayer;
        }

        /// <summary>
        /// Prefix for the IncrementallyBake method.
        /// Saves the old item stack.
        /// </summary>
        /// <param name="___ovenInv">The oven inventory.</param>
        /// <param name="__state">The state.</param>
        /// <param name="slotIndex">Index of the slot.</param>
        public static void IncrementallyBakePrefix(InventoryOven ___ovenInv, ref CookingState __state, int slotIndex)
        {
            __state = new CookingState();
            __state.quality = 0.0f;
            __state.stacks = new ItemStack[] { ___ovenInv[slotIndex].Itemstack };
        }

        /// <summary>
        /// Postfix for the IncrementallyBake method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="___ovenInv">The oven inventory.</param>
        /// <param name="slotIndex">Index of the slot.</param>
        public static void IncrementallyBakePostfix(BlockEntity __instance, ref CookingState __state, InventoryOven ___ovenInv, int slotIndex)
        {
            IPlayer byPlayer = __instance.GetBehavior<BlockEntityBehaviorOwnable>()?.Owner;
            if (byPlayer == null) return;
            if (__state.stacks[0] == ___ovenInv[slotIndex].Itemstack) return;
            if (___ovenInv[slotIndex].Itemstack.StackSize <  __state.stacks[0].StackSize)
                ___ovenInv[slotIndex].Itemstack.StackSize = __state.stacks[0].StackSize;

            Cooking cooking = byPlayer.Entity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            cooking.ApplyAbilities(___ovenInv[slotIndex], byPlayer, __state.quality, 1.0f, __state.stacks);
        }

        /// <summary>
        /// Postfix for the GetBlockInfo method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="___ovenInv">The oven inventory.</param>
        /// <param name="___bakingData">The baking data.</param>
        /// <param name="___fuelBurnTime">The fuel burn time.</param>
        /// <param name="forPlayer">For player.</param>
        /// <param name="sb">The sb.</param>
        public static void GetBlockInfoPostfix(BlockEntity __instance, InventoryOven ___ovenInv,  OvenItemData[] ___bakingData, float ___fuelBurnTime, IPlayer forPlayer, StringBuilder sb)
        {
            Cooking cooking = XLeveling.Instance(__instance.Api)?.GetSkill("cooking") as Cooking;
            if (cooking == null) return;
            PlayerAbility ability = forPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[cooking.Id][cooking.SpecialisationID];
            if (ability == null || ability.Tier < 1) return;

            if (___fuelBurnTime > 0.0f) sb.AppendLine(string.Format("Burning: {0:N2} sec", ___fuelBurnTime));
            for (int ii = 0; ii < ___bakingData.Length; ++ii)
            {
                if (___ovenInv[ii]?.Itemstack == null) continue;
                OvenItemData ovenData = ___bakingData[ii];
                BakingProperties props = BakingProperties.ReadFrom(___ovenInv[ii].Itemstack);
                if (props == null || ovenData == null) continue;
                float result = Math.Min((ovenData.BakedLevel - props.LevelFrom) / (props.LevelTo - props.LevelFrom), 1.0f);
                if (___ovenInv[ii]?.Itemstack != null) sb.AppendLine(Lang.Get("xskills:progress", result));
            }
        }
    }//!class BlockEntityOvenPatch
}//!namespace XSkills
