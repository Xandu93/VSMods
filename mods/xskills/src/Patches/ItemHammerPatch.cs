using HarmonyLib;
using System;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public class ItemHammerPatch : ItemHammer
    {
        static void Callback(EntityAgent byEntity)
        {
            if (byEntity.Controls.HandUse == EnumHandInteract.HeldItemInteract)
            {
                IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
                if (byPlayer == null) return;

                byPlayer.Entity.World.PlaySoundAt(new AssetLocation("sounds/effect/anvilhit"), byPlayer, byPlayer);
                byEntity.World.RegisterCallback((float time) => Callback(byEntity), 628);
            }
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
                byEntity.World.RegisterCallback((float time) => Callback(byEntity), 464);
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
                if (byEntity.World.Side == EnumAppSide.Client)
                {
                    ModelTransform tf = new ModelTransform();
                    tf.EnsureDefaultValues();

                    tf.Rotation.X = 270.0f;
                    tf.Rotation.Y = (float)Math.Abs(Math.Sin(Math.Max(0, secondsUsed * 5 - 0.25f)) * 110);

                    byEntity.Controls.UsingHeldItemTransformBefore = tf;
                }
                return secondsUsed < bh.GetHarvestDuration(byEntity) + 0.15f;
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);

            EntityBehaviorDisassemblable bh = entitySel?.Entity.GetBehavior<EntityBehaviorDisassemblable>();
            if (bh != null && bh.Harvestable && secondsUsed >= bh.GetHarvestDuration(byEntity) - 0.1f)
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
