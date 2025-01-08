using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(CollectibleObject))]
    public class CollectibleObjectPatch
    {
        //public static bool Prepare(MethodBase original)
        //{
        //    XSkills xSkills = XSkills.Instance;
        //    if (xSkills == null) return false;

        //    if (original.Name == "TryMergeStacks")
        //    {
        //        return !xSkills.XLeveling.Config.mergeQualities;
        //    }
        //    else return true;
        //}

        [HarmonyPatch("OnBlockBrokenWith")]
        public static void Prefix(ref Block __state, IWorldAccessor world, BlockSelection blockSel)
        {
            __state = world.BlockAccessor.GetBlock(blockSel.Position);
        }

        [HarmonyPatch("OnBlockBrokenWith")]
        public static void Postfix(CollectibleObject __instance, Block __state, IWorldAccessor world, Entity byEntity, ItemSlot itemslot)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = (byEntity as EntityPlayer)?.Player;
            ItemStack itemstack = itemslot.Itemstack;
            if (itemstack == null || byPlayer == null) return;
            if (__state == null) return;
            DropBonusBehavior beh = null;

            foreach (BlockBehavior beh2 in __state.BlockBehaviors)
            {
                beh = beh2 as DropBonusBehavior;
                if (beh != null) break;
            }

            Item tool = itemstack.Item;
            if (tool == null || beh == null) return;
            if ((beh.Tool != tool.Tool) && !tool.Code.Path.Contains("paxel")) return;

            //durability
            if (__instance.DamagedBy != null && __instance.DamagedBy.Contains(EnumItemDamageSource.BlockBreaking))
            {
                //for multiplayer server the clients sometimes don't set the forestry skill properly
                //and i don't know why. This should fix it. 
                if(beh.Skill == null)
                {
                    if (beh is XSkillsCharcoalBehavior charcoalBehavior)
                    {
                        charcoalBehavior.Forestry = XLeveling.Instance(world.Api)?.GetSkill("forestry") as Forestry;
                    }
                    if (beh.Skill == null) return;
                }

                PlayerAbility playerAbility = byEntity.GetBehavior<PlayerSkillSet>()?[beh.Skill.Id]?[beh.Skill.DurabilityId];

                if (playerAbility != null && (playerAbility.SkillDependentFValue() >= world.Rand.NextDouble()))
                {
                    int leftDurability = itemstack.Attributes.GetInt("durability", __instance.GetMaxDurability(itemstack));
                    leftDurability += 1;
                    itemstack.Attributes.SetInt("durability", leftDurability);
                    itemslot.MarkDirty();
                }
            }
        }

        [HarmonyPatch("GetHeldItemInfo")]
        public static void Postfix(ItemSlot inSlot, StringBuilder dsc)
        {
            float quality = inSlot?.Itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            QualityUtil.AddQualityString(quality, dsc);
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetMaxDurability")]
        public static void Postfix0(ref int __result, ItemStack itemstack)
        {
            float quality = itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            if (quality > 0.0f && __result > 1) __result = (int)(__result * (1.0f + quality * 0.05f));
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetAttackPower")]
        public static void Postfix1(ref float __result, IItemStack withItemStack)
        {
            float quality = withItemStack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            if (quality > 0.0f) __result = (float)(__result * (1.0f + quality * 0.02f));
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetMiningSpeed")]
        public static void Postfix2(ref float __result, IItemStack itemstack)
        {
            float quality = itemstack?.Attributes.TryGetFloat("quality") ?? 0.0f;
            if (quality > 0.0f) __result = (float)(__result * (1.0f + quality * 0.02f));
        }

        [HarmonyPatch("OnCreatedByCrafting")]
        public static void Postfix(ItemSlot[] allInputslots, ItemSlot outputSlot)
        {
            if (outputSlot.Itemstack == null) return;
            if (outputSlot.Itemstack.Collectible.GetMaxDurability(outputSlot.Itemstack) <= 1) return;
            float quality = 0.0f;
            int count = 0;
            bool useQuality = false;
            foreach(ItemSlot slot in allInputslots)
            {
                if (slot.Itemstack == null) continue;
                float? inputQuality = slot.Itemstack.Attributes.TryGetFloat("quality");

                if (outputSlot.Itemstack.Collectible == slot.Itemstack.Collectible)
                {
                    quality += (inputQuality ?? 0.0f) * 8.0f;
                    count += 8;
                }
                else if (slot.Itemstack.Collectible.Attributes?.IsTrue("useQuality") ?? false)
                {
                    useQuality = true;
                    quality += inputQuality ?? 0.0f;
                    count++;
                }
                else if (inputQuality != null)
                {
                    useQuality = true;
                    quality += 2 * (inputQuality ?? 0.0f);
                    count += 2;
                }
            }
            if (count > 0 && useQuality)
            {
                quality /= count;
                if (quality > 0.05f) outputSlot.Itemstack.Attributes.SetFloat("quality", quality);
            }
        }

        [HarmonyPatch("TryMergeStacks")]
        public static bool Prefix(out ItemStack __state, ItemStackMergeOperation op)
        {
            __state = op.SourceSlot.Itemstack;
            if (op.CurrentPriority != EnumMergePriority.AutoMerge) return true;
            if (!(op.SourceSlot.Itemstack.Collectible.Attributes?.IsTrue("useQuality") ?? false)) return true;
            if (op.SourceSlot.Itemstack.Attributes.GetDecimal("quality") == 
                op.SinkSlot.Itemstack.Attributes.GetDecimal("quality")) return true;
            return XSkills.Instance.XLeveling.Config.mergeQualities;
        }

        [HarmonyPatch("TryMergeStacks")]
        public static void Postfix(ItemStack __state, ItemStackMergeOperation op)
        {
            if (op.MovedQuantity <= 0) return;
            if (__state?.Attributes == null || op.SinkSlot.Itemstack?.Attributes == null) return;
            float quality = (
                __state.Attributes.GetFloat("quality") * (op.MovedQuantity) + 
                op.SinkSlot.Itemstack.Attributes.GetFloat("quality") * (op.SinkSlot.Itemstack.StackSize - op.MovedQuantity)) / 
                op.SinkSlot.Itemstack.StackSize;
            if (quality > 0.0f) op.SinkSlot.Itemstack.Attributes.SetFloat("quality", quality);
        }

        public class TryEatStopState
        {
            public float quality;
            public float temperature;

            public TryEatStopState()
            {
                quality = 0.0f;
                temperature = 0.0f;
            }
        }

        [HarmonyPatch("tryEatStop")]
        public static void Prefix(out TryEatStopState __state, ItemSlot slot, EntityAgent byEntity)
        {
            __state = new TryEatStopState();
            if (byEntity == null || slot?.Itemstack == null) return;
            __state.quality = slot.Itemstack.Attributes?.GetFloat("quality") ?? 0;
            __state.temperature = slot.Itemstack.Collectible.GetTemperature(byEntity.World, slot.Itemstack);
        }

        [HarmonyPatch("tryEatStop")]
        public static void Postfix(CollectibleObject __instance, TryEatStopState __state, float secondsUsed, ItemSlot slot, EntityAgent byEntity)
        {
            if (byEntity == null || slot == null || __state == null) return;
            FoodNutritionProperties nutriProps = __instance.GetNutritionProperties(byEntity.World, slot.Itemstack, byEntity);
            if (byEntity.World is IServerWorldAccessor && nutriProps != null && secondsUsed >= 0.95f)
            {
                Cooking.ApplyQuality(__state.quality, 1.0f, __state.temperature, nutriProps.FoodCategory, EnumFoodCategory.Unknown, byEntity);
            }
        }

        [HarmonyPatch("DoSmelt")]
        public static void Prefix(out DoSmeltState __state, ItemSlot outputSlot)
        {
            __state = new DoSmeltState();
            __state.stackSize = outputSlot.Itemstack?.StackSize ?? 0;
            __state.quality = outputSlot.Itemstack?.Attributes.GetFloat("quality") ?? 0.0f;
        }

        [HarmonyPatch("DoSmelt")]
        public static void Postfix(DoSmeltState __state, IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            InventoryBase inv = cookingSlotsProvider as InventoryBase;
            if (inv == null) return;
            BlockEntity blockEntity = world?.BlockAccessor.GetBlockEntity(inv.Pos);
            BlockEntityBehaviorOwnable ownable = blockEntity?.GetBehavior<BlockEntityBehaviorOwnable>();

            int cooked = (outputSlot.Itemstack?.StackSize ?? 0) - __state.stackSize;
            if (ownable?.Owner == null || cooked <= 0) return;
            DoSmeltCooking(ownable.Owner, outputSlot, cooked, __state.quality);
        }

        internal static bool DoSmeltCooking(IPlayer byPlayer, ItemSlot outputSlot, int cooked, float quality)
        {
            FoodNutritionProperties nutritionProps = outputSlot.Itemstack?.Collectible.NutritionProps;
            if (nutritionProps == null) return false;

            Cooking cooking = byPlayer.Entity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return true;
            cooking.ApplyAbilities(outputSlot, byPlayer, quality, cooked);
            return true;
        }
    }//!class CollectiblePatch
}//!namespace XSkills
