using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockFullCoating))]
    public class BlockFullCoatingPatch
    {
        [HarmonyPatch("OnLoaded")]
        public static void Postfix(Block __instance, ICoreAPI api)
        {
            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                behavior.OnLoaded(api);
            }
        }

        [HarmonyPatch("GetDrops")]
        public static bool Prefix(Block __instance, ref ItemStack[] __result, BlockFacing[] ___ownFacings, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            bool preventDefault = false;
            List<ItemStack> dropStacks = new List<ItemStack>();

            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                ItemStack[] stacks = behavior.GetDrops(world, pos, byPlayer, ref dropQuantityMultiplier, ref handled);
                if (stacks != null) dropStacks.AddRange(stacks);
                if (handled == EnumHandling.PreventSubsequent)
                {
                    __result = stacks;
                    preventDefault = true;
                }
                if (handled == EnumHandling.PreventDefault) preventDefault = true;
            }
            if (preventDefault)
            {
                __result = dropStacks.ToArray();
                return false;
            }
            for (int ii = 0; ii < __instance.Drops.Length; ii++)
            {
                int quantity = 0;
                if (___ownFacings.Length == 1)
                {
                    float val = dropQuantityMultiplier;
                    quantity += (int)val + (((val - (int)val) > world.Rand.NextDouble()) ? 1 : 0);
                }
                else
                {
                    for (int jj = 0; jj < ___ownFacings.Length; jj++)
                    {
                        float val = __instance.Drops[ii].Quantity.nextFloat() * dropQuantityMultiplier;
                        quantity += (int)val + (((val - (int)val) > world.Rand.NextDouble()) ? 1 : 0);
                    }
                }
                ItemStack stack = __instance.Drops[ii].ResolvedItemstack.Clone();
                stack.StackSize = Math.Max(1, quantity);
                dropStacks.Add(stack);
            }
            __result = dropStacks.ToArray();
            return false;
        }
    }//!class BlockFullCoatingPatch
}//!namespace XSkills
