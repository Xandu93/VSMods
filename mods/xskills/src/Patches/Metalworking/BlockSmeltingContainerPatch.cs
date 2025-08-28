using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockSmeltingContainer class.
    /// </summary>
    [HarmonyPatch(typeof(BlockSmeltingContainer))]
    public class BlockSmeltingContainerPatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            XSkills xSkills = XSkills.Instance;
            if (xSkills == null) return false;
            Skill skill;
            xSkills.Skills.TryGetValue("metalworking", out skill);
            Metalworking metalworking = skill as Metalworking;

            if (!(metalworking?.Enabled ?? false)) return false;
            if (original == null) return true;

            switch (original.Name)
            {
                case "CanSmelt":
                    return
                        metalworking[metalworking.SenseOfTime].Enabled;
                default:
                    return
                        true;
            }
        }

        /// <summary>
        /// Postfix for the DoSmelt method.
        /// Applies cooking abilities to the cooked item.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="cookingSlotsProvider">The cooking slots provider.</param>
        /// <param name="outputSlot">The output slot.</param>
        [HarmonyPostfix]
        [HarmonyPatch("DoSmelt")]
        public static void DoSmeltPostfix(IWorldAccessor world, ISlotProvider cookingSlotsProvider, ItemSlot outputSlot)
        {
            IPlayer player = CookingUtil.GetOwnerFromInventory(cookingSlotsProvider as InventoryBase);
            if (player?.Entity == null) return;

            Metalworking metalworking = player.Entity.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return;

            PlayerAbility playerAbility = player.Entity.GetBehavior<PlayerSkillSet>()?[metalworking.Id]?[metalworking.SenseOfTime];

            if (playerAbility?.Tier > 0)
            {
                PotteryUtil.NotifyPlayer(world, outputSlot, player, "xskills:cooking-finished", "xskillsCookingMsg");
            }
        }
    }
}
