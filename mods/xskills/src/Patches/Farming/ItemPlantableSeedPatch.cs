//using HarmonyLib;
//using Vintagestory.API.Common;
//using Vintagestory.GameContent;
//using XLib.XLeveling;

//namespace XSkills
//{
//    [HarmonyPatch(typeof(ItemPlantableSeed))]
//    internal class ItemPlantableSeedPatch
//    {
//        [HarmonyPatch("OnHeldInteractStart")]
//        public static void Postfix(EntityAgent byEntity, BlockSelection blockSel, ref EnumHandHandling handHandling)
//        {
//            if (handHandling != EnumHandHandling.PreventDefault) return;
//            Block cropBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position.UpCopy());
//            if (cropBlock == null) return;

//            Farming farming = byEntity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("farming") as Farming;
//            if (farming == null) return;

//            PlayerSkill skill = byEntity.GetBehavior<PlayerSkillSet>()?[farming.Id];
//            if (skill == null) return;

//            BlockEntityFarmland farmlandBlockEntity = byEntity.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityFarmland;
//            if (farmlandBlockEntity == null) return;

//            PlayerAbility ability = skill[farming.CultivatedSeedsId];
//            if (ability == null || ability.Tier <= 0) return;

//            if (farmlandBlockEntity.roomness > 0) farmlandBlockEntity.TryGrowCrop(byEntity.World.Calendar.TotalHours);
//            if (byEntity.World.Rand.NextDouble() < ability.SkillDependentFValue()) farmlandBlockEntity.TryGrowCrop(byEntity.World.Calendar.TotalHours);
//        }
//    }//!class ItemPlantableSeedPatch
//}//!namespace XSkills
