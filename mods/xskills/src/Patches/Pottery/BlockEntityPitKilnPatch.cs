using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityPitKiln class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityPitKiln))]
    public class BlockEntityPitKilnPatch
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
            xSkills.Skills.TryGetValue("pottery", out skill);
            Pottery pottery = skill as Pottery;

            if (!(pottery?.Enabled ?? false)) return false;
            if (original == null) return true;

            return pottery[pottery.InspirationId].Enabled;
        }

        /// <summary>
        /// Postfix for the TryIgnite method.
        /// Stores the player that ignited the kiln as the owner of the items.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="byPlayer">The by player.</param>
        [HarmonyPostfix]
        [HarmonyPatch("TryIgnite")]
        public static void TryIgnitePostfix(BlockEntityPitKiln __instance, IPlayer byPlayer)
        {
            if (byPlayer?.PlayerUID == null) return;
            foreach(ItemSlot slot in __instance.Inventory)
            {
                if (slot.Itemstack == null) continue;
                slot.Itemstack.Attributes.SetString("owner", byPlayer.PlayerUID);
                return;
            }
        }

        /// <summary>
        /// Prefix for the OnFired method.
        /// Saves the player as state before the old item is destroyed.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        [HarmonyPrefix]
        [HarmonyPatch("OnFired")]
        public static void OnFiredPrefix(BlockEntityPitKiln __instance, ref IPlayer __state)
        {
            __state = null;
            if (__instance.Inventory == null) return;
            foreach (ItemSlot slot in __instance.Inventory)
            {
                if (slot.Itemstack == null) continue;
                string uid = slot.Itemstack.Attributes?.GetString("owner");
                if (uid == null) continue;
                __state = __instance.Api.World.PlayerByUid(uid);
                break;
            }
        }

        /// <summary>
        /// Postfix for the OnFired method.
        /// Uses the player to apply abilities on the created stacks.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        [HarmonyPostfix]
        [HarmonyPatch("OnFired")]
        public static void OnFiredPostfix(BlockEntityPitKiln __instance, IPlayer __state)
        {
            if (__state == null || __instance.Inventory == null) return;
            foreach (ItemSlot slot in __instance.Inventory)
            {
                if (slot.Itemstack != null) PotteryUtil.ApplyOnStack(__state, __instance.Api.World, slot);
                __instance.MarkDirty(true);
            }
        }
    }//!class BlockEntityPitKilnPatch
}//!namespace XSkills
