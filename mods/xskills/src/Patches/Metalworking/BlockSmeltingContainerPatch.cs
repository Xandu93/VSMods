using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
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
                BlockPos pos = outputSlot.Inventory.Pos;
                Block block = pos != null ? world.BulkBlockAccessor.GetBlock(pos) : null;

                bool emptyInput = (outputSlot.Inventory as InventorySmelting)?[1].Empty ?? true;

                if (block != null && emptyInput)
                {
                    double now = world.Calendar.TotalHours;
                    double lastMsg = player.Entity.Attributes.GetDouble("xskillsCookingMsg");

                    if (now > lastMsg + 0.333)
                    {
                        player.Entity.Attributes.SetDouble("xskillsCookingMsg", now);
                        world.PlaySoundFor(new AssetLocation("sounds/tutorialstepsuccess.ogg"), player);

                        string msg = Lang.Get("xskills:cooking-finished", block.GetPlacedBlockName(world, pos) + " (" + pos.X + ", " + pos.X + pos.Y + ", " + pos.Z + ")");
                        (player as IServerPlayer)?.SendMessage(0, msg, EnumChatType.Notification);
                    }
                }
            }
        }
    }
}
