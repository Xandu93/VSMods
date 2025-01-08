using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the InventoryMixingBowl class.
    /// </summary>
    /// <seealso cref="XSkills.ManualPatch" />
    public class BlockEntityButcherWorkstationPatch : ManualPatch
    {
        /// <summary>
        /// Applies harmony patches.
        /// </summary>
        /// <param name="harmony">The harmony lib.</param>
        /// <param name="type">The type.</param>
        /// <param name="xSkills">The xskills reference to check configurations.</param>
        public static void Apply(Harmony harmony, Type type, XSkills xSkills)
        {
            if (xSkills == null) return;
            Skill skill;
            xSkills.Skills.TryGetValue("husbandry", out skill);
            Husbandry husbandry = skill as Husbandry;

            if (!(husbandry?.Enabled ?? false)) return;
            Type patch = typeof(BlockEntityButcherWorkstationPatch);

            if (
                husbandry[husbandry.ButcherId].Enabled ||
                husbandry[husbandry.FurrierId].Enabled ||
                husbandry[husbandry.BoneBrakerId].Enabled ||
                husbandry[husbandry.PreserverId].Enabled)
            {
                PatchMethod(harmony, type, patch, "DropLoot");
            }
        }

        /// <summary>
        /// Harmony prefix for DropLoot method.
        /// </summary>
        /// <param name="__state">The state contains the original values.</param>
        /// <param name="byPlayer">The player.</param>
        /// <param name="drops">The drops.</param>
        /// <param name="_inventory">The inventory.</param>
        [HarmonyPrefix]
        [HarmonyPatch("DropLoot")]
        public static void DropLootPrefix(out List<float> __state, IPlayer byPlayer, BlockDropItemStack[] drops, InventoryBase ___inventory)
        {
            //the state contains the original values
            __state = new List<float>();
            Husbandry husbandry = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;
            int generation = ___inventory[0].Itemstack.Attributes.GetAsInt("generation", 0);

            foreach (BlockDropItemStack drop in drops)
            {
                __state.Add(drop.Quantity.avg); 
                PlayerAbility ability = null;
                if (drop.Code.FirstCodePart() == "hide")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }
                else if (drop.Code.Path.Contains("meat"))
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (drop.Code.FirstCodePart() == "fat")
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (drop.Code.FirstCodePart() == "feather")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }
                else if (drop.Code.Path.Contains("bone"))
                {
                    ability = playerSkill[husbandry.BoneBrakerId];
                }

                if (ability?.Tier > 0)
                {
                    float multiplier = 1.0f + ability.SkillDependentFValue() + (ability.FValue(3) * Math.Min(generation, ability.Value(4)));
                    drop.Quantity.avg *= multiplier;
                }
            }
        }

        /// <summary>
        /// Harmony postfix for DropLoot method.
        /// </summary>
        /// <param name="__state">The state contains the original values.</param>
        /// <param name="drops">The drops.</param>
        /// <returns></returns>
        [HarmonyPostfix]
        [HarmonyPatch("DropLoot")]
        public static void DropLootPostfix(List<float> __state, BlockDropItemStack[] drops)
        {
            int counter = 0;
            foreach (float value in __state)
            {
                drops[counter].Quantity.avg = value;
                counter++;
            }
        }

    }//!class BlockEntityButcherWorkstationPatch
}//!namespace XSkills
