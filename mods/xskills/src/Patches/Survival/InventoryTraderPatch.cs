using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace XSkills
{
    [HarmonyPatch(typeof(InventoryTrader))]
    public class InventoryTraderPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("GetPlayerAssets")]
        public static void GetPlayerAssetsPrefix(EntityAgent eagent)
        {
            XSkillsPlayerInventory inv = (eagent as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            inv.Linked = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("GetPlayerAssets")]
        public static void GetPlayerAssetsPostfix(EntityAgent eagent)
        {
            XSkillsPlayerInventory inv = (eagent as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            inv.Linked = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch("DeductFromEntity")]
        public static void DeductFromEntityPrefix(EntityAgent eagent)
        {
            XSkillsPlayerInventory inv = (eagent as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            inv.Linked = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("DeductFromEntity")]
        public static void DeductFromEntityPostfix(EntityAgent eagent)
        {
            XSkillsPlayerInventory inv = (eagent as EntityPlayer)?.Player?.InventoryManager?.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
            inv.Linked = true;
        }
    }//!class InventoryTraderPatch
}//!namespace XSkills
