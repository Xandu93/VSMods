using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;
using XSkills;

namespace XSkills
{
    [HarmonyPatch(typeof(EntityBehaviorHarvestable))]
    public class EntityBehaviorHarvestablePatch
    {
        [HarmonyPatch("SetHarvested")]
        public static void Prefix(EntityBehaviorHarvestable __instance, IPlayer byPlayer, ref float dropQuantityMultiplier)
        {
            if (__instance.entity.World.Side == EnumAppSide.Client || byPlayer?.Entity == null) return;
            XSkillsAnimalBehavior animalBehavior = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (animalBehavior == null)
            {
                Combat combat = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("combat") as Combat;
                if (combat == null) return;
                PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[combat.Id];
                if (playerSkill == null) return;
                dropQuantityMultiplier += playerSkill[combat.LooterId].SkillDependentFValue();
            }
        }
        [HarmonyPatch("SetHarvested")]
        public static void Postfix(EntityBehaviorHarvestable __instance, IPlayer byPlayer, InventoryGeneric ___inv)
        {
            if (__instance.entity.World.Side == EnumAppSide.Client || byPlayer?.Entity == null) return;
            if (___inv.Empty) return;
            XSkillsAnimalBehavior animalBehavior = __instance.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (animalBehavior == null) return;

            Husbandry husbandry = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return;
            int generation = __instance.entity.WatchedAttributes.GetInt("generation", 0);

            for (int ii = 0; ii < ___inv.Count; ii++)
            {
                PlayerAbility ability = null;

                if (___inv[ii].Itemstack?.Collectible.FirstCodePart() == "hide")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }
                else if (___inv[ii].Itemstack?.Collectible.FirstCodePart(1) == "raw")
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (___inv[ii].Itemstack?.Collectible.FirstCodePart() == "fat")
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (___inv[ii].Itemstack?.Collectible.FirstCodePart() == "feather")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }

                if (ability?.Tier > 0)
                {
                    float multiplier = 1.0f + ability.SkillDependentFValue() + (ability.FValue(3) * Math.Min(generation, ability.Value(4)));
                    float stackSize = multiplier * ___inv[ii].Itemstack.StackSize;
                    int quantity = (int)stackSize;
                    quantity += (stackSize - quantity) > __instance.entity.World.Rand.NextDouble() ? 1 : 0;
                    ___inv[ii].Itemstack.StackSize = quantity;
                }

                //preserver
                if (___inv[ii].Itemstack != null)
                {
                    ___inv[ii].Itemstack.Collectible.UpdateAndGetTransitionState(__instance.entity.World, ___inv[ii], EnumTransitionType.Perish);
                    ITreeAttribute attr = (___inv[ii].Itemstack.Attributes as TreeAttribute)?.GetTreeAttribute("transitionstate");
                    ability = playerSkill[husbandry.PreserverId];
                    if (attr != null && ability?.Tier > 0)
                    {
                        FloatArrayAttribute freshHoursAttribute = attr["freshHours"] as FloatArrayAttribute;
                        FloatArrayAttribute transitionedHoursAttribute = attr["transitionedHours"] as FloatArrayAttribute;
                        if (transitionedHoursAttribute.value.Length >= 1)
                        {
                            float value = freshHoursAttribute.value[0] * ability.SkillDependentFValue();
                            transitionedHoursAttribute.value[0] -= value;
                        }
                    }
                }
            }

            TreeAttribute tree = new TreeAttribute();
            ___inv.ToTreeAttributes(tree);
            __instance.entity.WatchedAttributes["harvestableInv"] = tree;
            __instance.entity.WatchedAttributes.MarkPathDirty("harvestableInv");
            __instance.entity.WatchedAttributes.MarkPathDirty("harvested");
        }

    }//!class EntityBehaviorHarvestablePatch
}//!namespace XSkills