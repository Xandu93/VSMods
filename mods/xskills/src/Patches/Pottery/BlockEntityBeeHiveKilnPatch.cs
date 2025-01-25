using HarmonyLib;
using System.Linq;
using System.Reflection;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockEntityBeeHiveKiln))]
    public class BlockEntityBeeHiveKilnPatch
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
            xSkills.Skills.TryGetValue("pottery", out Skill skill);
            Pottery pottery = skill as Pottery;

            if (!(pottery?.Enabled ?? false)) return false;
            if (original == null) return true;

            return pottery[pottery.InspirationId].Enabled;
        }

        /// <summary>
        /// Postfix for the Interact method.
        /// Stores the player that used the kiln as the owner of the kiln.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="byPlayer">The by player.</param>
        [HarmonyPostfix]
        [HarmonyPatch("Interact")]
        public static void InteractPostfix(BlockEntityBeeHiveKiln __instance, IPlayer byPlayer)
        {
            if (byPlayer?.PlayerUID == null) return;
            BlockEntityBehaviorOwnable ownable = __instance.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null)
            {
                ownable = new BlockEntityBehaviorOwnable(__instance);
                ownable.Api = __instance.Api;
                __instance.Behaviors.Add(ownable);
            }
            ownable.OwnerString = byPlayer.PlayerUID;
            ownable.ResolveOwner();
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetBlockInfo")]
        public static void GetBlockInfoPostfix(BlockEntityBeeHiveKiln __instance, StringBuilder dsc)
        {
            BlockEntityBehaviorOwnable ownable = __instance.GetBehavior<BlockEntityBehaviorOwnable>();
            if (ownable == null) return;
            IPlayer owner = ownable.Owner;
            if (owner == null) ownable.ResolveOwner();
            if (owner == null) return;
            dsc.AppendLine(Lang.Get("xskills:owner-desc", owner.PlayerName));
        }

        [HarmonyPostfix]
        [HarmonyPatch("ConvertItemToBurned")]
        public static void ConvertItemToBurnedPostfix(BlockEntityBeeHiveKiln __instance, BlockEntityGroundStorage groundStorage)
        {
            IPlayer player = __instance.GetBehavior<BlockEntityBehaviorOwnable>()?.Owner;
            if (player == null) return;
            foreach (ItemSlot slot in groundStorage.Inventory)
            {
                if (slot.Itemstack != null) PotteryUtil.ApplyOnStack(player, __instance.Api.World, slot);
                __instance.MarkDirty(true);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("FromTreeAttributes")]
        public static void FromTreeAttributesPostfix(BlockEntityBeeHiveKiln __instance, ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            BlockEntityBehaviorOwnable ownable = __instance.GetBehavior<BlockEntityBehaviorOwnable>();
            if (!tree.HasAttribute("owner")) return;
            if (ownable == null)
            {
                ownable = new BlockEntityBehaviorOwnable(__instance);
                ownable.Api = __instance.Api;
                __instance.Behaviors.Add(ownable);
            }
            ownable.FromTreeAttributes(tree, worldAccessForResolve);
        }

        [HarmonyPostfix]
        [HarmonyPatch("ToTreeAttributes")]
        public static void ToTreeAttributesPostfix(BlockEntityBeeHiveKiln __instance, ITreeAttribute tree)
        {
            BlockEntityBehaviorOwnable ownable = __instance.GetBehavior<BlockEntityBehaviorOwnable>();
            ownable?.ToTreeAttributes(tree);
        }
    }
}
