using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(EntityBehaviorHarvestable))]
    public static class EntityBehaviorHarvestablePatch
    {
        private static bool TrySetHarvestedAnimal(EntityBehaviorHarvestable harvestable, IPlayer byPlayer, InventoryGeneric inv)
        {
            XSkillsAnimalBehavior animalBehavior = harvestable.entity?.GetBehavior<XSkillsAnimalBehavior>();
            if (animalBehavior == null) return false;

            Husbandry husbandry = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return false;
            PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return false;
            int generation = harvestable.entity.WatchedAttributes.GetInt("generation", 0);

            for (int ii = 0; ii < inv.Count; ii++)
            {
                PlayerAbility ability = null;

                if (inv[ii].Itemstack?.Collectible.FirstCodePart() == "hide")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }
                else if (inv[ii].Itemstack?.Collectible.FirstCodePart(1) == "raw")
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (inv[ii].Itemstack?.Collectible.FirstCodePart() == "fat")
                {
                    ability = playerSkill[husbandry.ButcherId];
                }
                else if (inv[ii].Itemstack?.Collectible.FirstCodePart() == "feather")
                {
                    ability = playerSkill[husbandry.FurrierId];
                }

                if (ability?.Tier > 0)
                {
                    float multiplier = 1.0f + ability.SkillDependentFValue() + (ability.FValue(3) * Math.Min(generation, ability.Value(4)));
                    float stackSize = multiplier * inv[ii].Itemstack.StackSize;
                    int quantity = (int)stackSize;
                    quantity += (stackSize - quantity) > harvestable.entity.World.Rand.NextDouble() ? 1 : 0;
                    inv[ii].Itemstack.StackSize = quantity;
                }

                //preserver
                if (inv[ii].Itemstack != null)
                {
                    inv[ii].Itemstack.Collectible.UpdateAndGetTransitionState(harvestable.entity.World, inv[ii], EnumTransitionType.Perish);
                    ITreeAttribute attr = (inv[ii].Itemstack.Attributes as TreeAttribute)?.GetTreeAttribute("transitionstate");
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
            inv.ToTreeAttributes(tree);
            harvestable.entity.WatchedAttributes["harvestableInv"] = tree;
            harvestable.entity.WatchedAttributes.MarkPathDirty("harvestableInv");
            harvestable.entity.WatchedAttributes.MarkPathDirty("harvested");
            return true;
        }

        //private static bool TrySetHarvestedEnemy(EntityBehaviorHarvestable harvestable, IPlayer byPlayer, InventoryGeneric inv)
        //{
        //    Combat combat = XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("combat") as Combat;
        //    if (combat == null) return false;
        //    PlayerSkill playerSkill = byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[combat.Id];
        //    if (playerSkill == null) return false;
        //    float multiplier = playerSkill[combat.LooterId].SkillDependentFValue();

        //    if (multiplier > 0.0f)
        //    {
        //        for (int ii = 0; ii < inv.Count; ii++)
        //        {
        //            float stackSize = multiplier * inv[ii].Itemstack.StackSize;
        //            int quantity = (int)stackSize;
        //            quantity += (stackSize - quantity) > harvestable.entity.World.Rand.NextDouble() ? 1 : 0;
        //            inv[ii].Itemstack.StackSize = quantity;
        //        }
        //    }
        //    return true;
        //}

        [HarmonyPrefix]
        [HarmonyPatch("SetHarvested")]
        public static void SetHarvestedPrefix(EntityBehaviorHarvestable __instance, IPlayer byPlayer, ref float dropQuantityMultiplier)
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

        [HarmonyPostfix]
        [HarmonyPatch("SetHarvested")]
        public static void SetHarvestedPostfix(EntityBehaviorHarvestable __instance, IPlayer byPlayer, InventoryGeneric ___inv)
        {
            if (__instance.entity.World.Side == EnumAppSide.Client || byPlayer?.Entity == null) return;
            if (___inv.Empty) return;

            if (TrySetHarvestedAnimal(__instance, byPlayer, ___inv)) return;
            //if (TrySetHarvestedEnemy(__instance, byPlayer, ___inv)) return;
        }
    }//!class EntityBehaviorHarvestablePatch
}//!namespace XSkills