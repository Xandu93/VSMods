using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// Contains some methods for cooking related skills.
    /// </summary>
    public class CookingUtil
    {
        /// <summary>
        /// Gets the Ownable behavior from an inventory.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <returns>the Ownable behavior or null</returns>
        public static BlockEntityBehaviorOwnable GetOwnableFromInventory(InventoryBase inventory)
        {
            if (inventory == null || inventory.Pos == null) return null;
            BlockEntityBehaviorOwnable ownable = inventory.Api.World.BlockAccessor.GetBlockEntity(inventory.Pos)?.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return null;
            if (ownable.ShouldResolveOwner()) ownable.ResolveOwner();
            return ownable;
        }

        /// <summary>
        /// Gets the owner from an inventory.
        /// </summary>
        /// <param name="inventory">The inventory.</param>
        /// <returns></returns>
        public static IPlayer GetOwnerFromInventory(InventoryBase inventory)
        {
            BlockEntityBehaviorOwnable ownable = GetOwnableFromInventory(inventory);
            return ownable?.Owner;
        }

        /// <summary>
        /// Sets the maximum size of the serving.
        /// Used for the canteen cook ability.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <returns>the old max serving size</returns>
        public static int SetMaxServingSize(BlockCookingContainer container, ISlotProvider cookingSlotsProvider)
        {
            int old = container.MaxServingSize;
            IPlayer player = GetOwnerFromInventory(cookingSlotsProvider as InventoryBase);
            if (player?.Entity == null) return old;

            Cooking cooking = player.Entity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return old;

            //canteen cook
            PlayerSkill skill = player.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id];
            PlayerAbility ability = skill?[cooking.CanteenCookId];
            if (ability != null) container.MaxServingSize += (int)(container.MaxServingSize * ability.FValue(0));
            return old;
        }

        /// <summary>
        /// Gets the cooking time multiplier.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static float GetCookingTimeMultiplier(BlockEntity entity)
        {
            IPlayer byPlayer = entity.GetBehavior<BlockEntityBehaviorOwnable>()?.Owner;
            Cooking cooking = byPlayer?.Entity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("cooking") as Cooking;
            if (cooking == null) return 1.0f;
            PlayerSkill skill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[cooking.Id];
            if (skill == null) return 1.0f;

            //fast food
            float mult1 = 1.0f - (skill[cooking.FastFoodId]?.SkillDependentFValue() ?? 0.0f);

            //well done
            float mult2 = 1.0f + (skill[cooking.WellDoneId]?.FValue(3) ?? 0.0f);

            return mult1 * mult2;
        }
    }//!class CookingUtil
}//!namespace XSkills
