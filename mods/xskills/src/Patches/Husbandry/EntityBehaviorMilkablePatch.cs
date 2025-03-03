using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(EntityBehaviorMilkable))]
    public class EntityBehaviorMilkablePatch
    {
        [HarmonyPatch("MilkingComplete")]
        public static bool Prefix(EntityBehaviorMilkable __instance, out bool __state, EntityAgent byEntity)
        {
            __state = true;
            if (__instance.entity.World.Side == EnumAppSide.Client) return __state;

            Husbandry husbandry = XLeveling.Instance(__instance.entity.Api).GetSkill("husbandry") as Husbandry;
            if (husbandry == null) return __state;
            PlayerSkill playerSkill = byEntity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
            if (playerSkill == null) return __state;
            PlayerAbility playerAbility = playerSkill[husbandry.CheesyCheeseId];
            if (playerAbility == null) return __state;

            //experience
            playerSkill.AddExperience(0.5f);

            float prob = (float)(
                playerAbility.FValue(0) +
                playerAbility.FValue(1) * (
                __instance.entity.World.Calendar.TotalHours -
                __instance.entity.WatchedAttributes.GetFloat("lastMilkedTotalHours")) /
                __instance.entity.World.Calendar.HoursPerDay);

            if (prob > __instance.entity.World.Rand.NextDouble())
            {
                typeof(EntityBehaviorMilkable).GetField("lastMilkedTotalHours", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(__instance, __instance.entity.World.Calendar.TotalHours);
                __instance.entity.WatchedAttributes.SetFloat("lastMilkedTotalHours", (float)__instance.entity.World.Calendar.TotalHours);
                ItemStack cheese;
                if (0.2f < __instance.entity.World.Rand.NextDouble())
                {

                    cheese = new ItemStack(byEntity.World.GetItem(new AssetLocation("game", "cheese-cheddar-4slice")));
                }
                else
                {
                    cheese = new ItemStack(byEntity.World.GetItem(new AssetLocation("game", "cheese-blue-4slice")));
                }
                if (!byEntity.TryGiveItemStack(cheese))
                {
                    byEntity.World.SpawnItemEntity(cheese, byEntity.Pos.XYZ.Add(0, 0.5, 0));
                }
                __state = false;
                return __state;
            }
            return __state;
        }

        [HarmonyPatch("MilkingComplete")]
        public static void Postfix(EntityBehaviorMilkable __instance, bool __state, ItemSlot slot, EntityAgent byEntity)
        {
            if (!__state) return;
            if (__instance.entity.World.Side == EnumAppSide.Server)
            {
                Husbandry husbandry = XLeveling.Instance(__instance.entity.Api).GetSkill("husbandry") as Husbandry;
                if (husbandry == null) return;
                PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[husbandry.Id]?[husbandry.RancherId];
                if (playerAbility == null) return;

                if (__instance.entity.World.Rand.NextDouble() < playerAbility.FValue(1))
                {
                    int generation = __instance.entity.WatchedAttributes.GetInt("generation") + 1;
                    if (generation < playerAbility.Value(2))
                    {
                        __instance.entity.WatchedAttributes.SetInt("generation", generation);
                        __instance.entity.WatchedAttributes.MarkPathDirty("generation");
                    }
                }

                ItemStack contentStack = new ItemStack(byEntity.World.GetItem(new AssetLocation("milkportion")));

                float stackSize = (1000 * playerAbility.FValue(0));
                contentStack.StackSize = (int)stackSize + ((stackSize - (int)stackSize) > byEntity.World.Rand.NextDouble() ? 1 : 0);
                if (contentStack.StackSize < 1) return;

                if (!TryFillLiquidContainer(slot, contentStack, byEntity))
                {
                    IInventory inventory = (byEntity as EntityPlayer)?.Player?.InventoryManager?.GetHotbarInventory();
                    if (inventory == null) return;
                    foreach (ItemSlot slot2 in inventory)
                    {
                        if (TryFillLiquidContainer(slot2, contentStack, byEntity)) return;
                    }
                    inventory = (byEntity as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory(GlobalConstants.backpackInvClassName);
                    if (inventory == null) return;
                    foreach (ItemSlot slot2 in inventory)
                    {
                        if (TryFillLiquidContainer(slot2, contentStack, byEntity)) return;
                    }
                }
            }
        }

        public static bool TryFillLiquidContainer(ItemSlot slot, ItemStack contentStack, EntityAgent byEntity)
        {
            BlockLiquidContainerBase lcblock = slot.Itemstack?.Collectible as BlockLiquidContainerBase;
            if (lcblock == null) return false;
            if (slot.Itemstack.StackSize == 1)
            {
                contentStack.StackSize -= lcblock.TryPutLiquid(slot.Itemstack, contentStack, 10);
                slot.MarkDirty();
                if (contentStack.StackSize <= 0) return true;
            }
            else
            {
                ItemStack containerStack = slot.TakeOut(1);
                slot.MarkDirty();
                contentStack.StackSize -= lcblock.TryPutLiquid(containerStack, contentStack, 10);

                if (!byEntity.TryGiveItemStack(containerStack))
                {
                    byEntity.World.SpawnItemEntity(containerStack, byEntity.Pos.XYZ.Add(0, 0.5, 0));
                }
                if (contentStack.StackSize <= 0) return true;
            }
            return false;
        }

    }//!class EntityBehaviorMilkablePatch
}