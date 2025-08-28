using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class ItemHammerPatch : ItemHammer
    {
        protected void Animate(string animation, EntityAgent byEntity, bool reset)
        {
            if (reset) byEntity.AnimManager.ResetAnimation(animation);
            else byEntity.AnimManager.StartAnimation(animation);
            float framesound = CollectibleBehaviorAnimationAuthoritative.getSoundAtFrame(byEntity, animation);
            byEntity.AnimManager.RegisterFrameCallback(new AnimFrameCallback()
            {
                Animation = animation,
                Frame = framesound,
                Callback = () =>
                    byEntity.World.PlaySoundAt(
                        new AssetLocation("sounds/effect/anvilhit"),
                        byEntity, (byEntity as EntityPlayer)?.Player, false, 12)
            });

        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            if (handling == EnumHandHandling.Handled || handling == EnumHandHandling.PreventDefault) return;

            PlayerSkill playerSkill = byEntity.GetBehavior<PlayerSkillSet>()?.FindSkill("metalworking");
            if (playerSkill == null) return;
            Metalworking metalworking = playerSkill.Skill as Metalworking;
            if (metalworking == null) return;
            if (playerSkill[metalworking.SalvagerId].Tier <= 0) return;

            EntityBehaviorDisassemblable bh = entitySel?.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (byEntity.Controls.Sneak && entitySel != null && bh != null && bh.Harvestable)
            {
                Animate(bh.Animation, byEntity, false);
                handling = EnumHandHandling.PreventDefault;
                return;
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            bool result = base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);
            if (result) return result;

            EntityBehaviorDisassemblable bh = entitySel?.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (entitySel != null && bh != null && bh.Harvestable)
            {
                RunningAnimation state = byEntity.AnimManager.GetAnimationState(bh.Animation);
                if (state?.AnimProgress >= 1.0f)
                {
                    Animate(bh.Animation, byEntity, true);
                }
                return secondsUsed < bh.GetHarvestDuration(byEntity) + 0.15f;
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            EntityBehaviorDisassemblable bh = entitySel?.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (bh == null) return;
            byEntity.StopAnimation(bh.Animation);

            if (bh.Harvestable && secondsUsed >= bh.GetHarvestDuration(byEntity) - 0.1f)
            {
                PlayerSkill playerSkill = byEntity.GetBehavior<PlayerSkillSet>()?.FindSkill("metalworking");
                if (playerSkill == null) return;
                Metalworking metalworking = playerSkill.Skill as Metalworking;
                if (metalworking == null) return;
                PlayerAbility playerAbility = playerSkill[metalworking.SalvagerId];
                if (playerAbility.Tier <= 0) return;

                bh.SetHarvested((byEntity as EntityPlayer)?.Player, playerAbility.SkillDependentFValue());
                slot?.Itemstack?.Collectible.DamageItem(byEntity.World, byEntity, slot, 3);
            }
        }

        public override bool OnHeldAttackCancel(float secondsPassed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSelection, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            bool result = base.OnHeldAttackCancel(secondsPassed, slot, byEntity, blockSelection, entitySel, cancelReason);
            EntityBehaviorDisassemblable bh = entitySel?.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (bh == null) return result;

            byEntity.StopAnimation(bh.Animation);
            return result;
        }
    }//!class ItemHammerPatch

    [HarmonyPatch(typeof(ItemKnife))]
    public class ItemKnifePatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStart")]
        public static bool OnHeldInteractStartPrefix(EntityAgent byEntity, EntitySelection entitySel)
        {
            if (entitySel == null) return true;
            if (entitySel.Entity.HasBehavior("harvestable")) return true;
            EntityBehaviorHarvestable bh = entitySel.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (byEntity.Controls.Sneak && bh != null && bh.Harvestable) return false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnHeldInteractStop")]
        public static bool OnHeldInteractStopPrefix(EntityAgent byEntity, EntitySelection entitySel)
        {
            if (entitySel == null) return true;
            if (entitySel.Entity.HasBehavior("harvestable")) return true;
            EntityBehaviorHarvestable bh = entitySel.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (byEntity.Controls.Sneak && bh != null && bh.Harvestable) return false;
            return true;
        }
    }//!class ItemKnifePatch
}//!namespace XSkills
