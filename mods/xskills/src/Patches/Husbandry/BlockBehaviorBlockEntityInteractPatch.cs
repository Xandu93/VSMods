using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockBehaviorBlockEntityInteract))]
    public class BlockBehaviorBlockEntityInteractPatch
    {
        [HarmonyPatch("OnBlockInteractStart")]
        public static void Prefix(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.Block == null) return;
            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use)) return;
            if (blockSel.Block.Code.Path.Contains("empty")) return;

            if (blockSel.Block.Drops != null && blockSel.Block.Drops.Length > 1)
            {
                Husbandry husbandry = XLeveling.Instance(world.Api)?.GetSkill("husbandry") as Husbandry;
                if (husbandry == null) return;
                PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[husbandry.Id];
                PlayerAbility playerAbility = playerSkill?[husbandry.RancherId];
                if (playerAbility == null) return;
                BlockDropItemStack drop = blockSel.Block.Drops[0];

                //experience
                playerSkill.AddExperience(0.1f * blockSel.Block.Drops[0].Quantity.avg);

                if (playerAbility.Tier < 1) return;
                ItemStack stack = drop.GetNextItemStack(playerAbility.FValue(0));
                if ((stack?.StackSize ?? 0) < 1) return;

                if (!byPlayer.InventoryManager.TryGiveItemstack(stack))
                {
                    world.SpawnItemEntity(drop.GetNextItemStack(), blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5));
                }
            }
        }
    }//!class BehaviorCollectFromPatch
}