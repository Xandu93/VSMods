using HarmonyLib;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.Common;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(InventoryPlayerBackPacks))]
    public class InventoryPlayerBackPacksPatch
    {
        public static bool Prepare(MethodBase original)
        {
            XSkills xSkills = XSkills.Instance;
            if (xSkills == null) return false;
            xSkills.Skills.TryGetValue("survival", out Skill skill);
            Survival survival = skill as Survival;

            if (!(survival?.Enabled ?? false)) return false;
            if (original == null) return true;
            return survival[survival.StrongBackId].Enabled;
        }

        [HarmonyPostfix]
        [HarmonyPatch("get_Count")]
        public static void GetCountPostfix(InventoryPlayerBackPacks __instance, ref int __result)
        {
            XSkillsPlayerInventory inv = __instance.Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv?.Linked ?? false) __result += inv.Count;
        }

        [HarmonyPrefix]
        [HarmonyPatch("get_Item")]
        public static bool GetItemPostfix(InventoryPlayerBackPacks __instance, out ItemSlot __result, int slotId)
        {
            __result = null;
            if (slotId < 0) return false;
            int count = __instance.Count;
            if (slotId >= count) return false;

            XSkillsPlayerInventory inv = __instance.Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv == null) return true;
            if (!inv.Linked) return true;
            count -= inv.Count;
            slotId -= count;
            if (slotId < 0) return true;
            __result = inv[slotId];
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("set_Item")]
        public static bool SetItemPrefix(InventoryPlayerBackPacks __instance, int slotId, ItemSlot value)
        {
            if (slotId < 0) return true;

            XSkillsPlayerInventory inv = __instance.Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv == null) return true;
            if (!inv.Linked) return true;
            int count = __instance.Count - inv.Count;
            slotId -= count;
            if (slotId >= inv.Count || slotId < 0) return true;
            inv[slotId] = value;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("ActivateSlot")]
        public static void ActivateSlotPrefix(InventoryPlayerBackPacks __instance, out object __result, int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            __result = null;
            if (slotId < 0 || !op.ShiftDown) return;
            int count = __instance.Count;
            if (slotId >= count) return;

            XSkillsPlayerInventory inv = __instance.Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            if (inv == null) return;
            if (!inv.Linked) return;
            count -= inv.Count;
            slotId -= count;
            if (slotId >= inv.Count || slotId < 0) return;
            __result = inv.ActivateSlot(slotId, sourceSlot, ref op);
        }
    }//!class InventoryPlayerBackPacksPatch
}//!namespace XSkills
