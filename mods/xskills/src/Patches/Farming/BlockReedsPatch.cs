using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockReeds))]
    public class BlockReedsPatch
    {
        //original https://github.com/anegostudios/vssurvivalmod/blob/master/Block/BlockReeds.cs
        [HarmonyPatch("OnBlockBroken")]
        public static bool Prefix(BlockReeds __instance, IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier)
        {
            bool preventDefault = false;

            foreach (BlockBehavior behavior in __instance.BlockBehaviors)
            {
                EnumHandling handled = EnumHandling.PassThrough;

                behavior.OnBlockBroken(world, pos, byPlayer, ref handled);
                if (handled == EnumHandling.PreventDefault) preventDefault = true;
                if (handled == EnumHandling.PreventSubsequent) return false;
            }
            if (preventDefault) return false;

            Farming farming = byPlayer != null ? XLeveling.Instance(byPlayer.Entity.Api)?.GetSkill("farming") as Farming : null;
            PlayerSkill playerSkill = farming != null ? byPlayer?.Entity.GetBehavior<PlayerSkillSet>()?[farming.Id] : null;
            PlayerAbility playerAbility;

            if (world.Side == EnumAppSide.Server && (byPlayer == null || byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative))
            {
                foreach (BlockDropItemStack bdrop in __instance.Drops)
                {
                    float dropMultiplier = 1.0f;
                    if (!(bdrop.ResolvedItemstack?.Item is ItemCattailRoot) && playerSkill != null)
                    {
                        playerAbility = playerSkill[farming.GathererId];
                        dropMultiplier += playerAbility?.SkillDependentFValue() ?? 0.0f;
                    }

                    ItemStack drop = bdrop.GetNextItemStack(dropMultiplier);
                    if (drop != null)
                    {
                        world.SpawnItemEntity(drop, new Vec3d(pos.X + 0.5, pos.Y + 0.5, pos.Z + 0.5), null);
                    }
                }
                world.PlaySoundAt(__instance.Sounds.GetBreakSound(byPlayer), pos.X, pos.Y, pos.Z, byPlayer);
            }

            bool toolUsed = false;
            if (byPlayer != null)
            {
                playerAbility = playerSkill?[farming.CarefulHandsId];
                toolUsed = 
                    byPlayer.InventoryManager.ActiveTool == EnumTool.Knife || 
                    byPlayer.InventoryManager.ActiveTool == EnumTool.Sickle || 
                    byPlayer.InventoryManager.ActiveTool == EnumTool.Scythe || 
                    playerAbility?.Tier > 0;
            }

            if (__instance.Variant["state"] == "normal" && toolUsed)
            {
                world.BlockAccessor.SetBlock(world.GetBlock(__instance.CodeWithVariants(new string[] { "habitat", "state" }, new string[] { "land", "harvested" })).BlockId, pos);
                return false;
            }

            __instance.SpawnBlockBrokenParticles(pos);
            world.BlockAccessor.SetBlock(0, pos);
            return false;
        }
    }//!class BlockReedsPatch
}//!namespace XSkills
