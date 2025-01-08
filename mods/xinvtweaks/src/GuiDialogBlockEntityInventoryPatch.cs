using Vintagestory.API.Client;

namespace XInvTweaks
{
    internal class GuiDialogBlockEntityInventoryPatch
    {
        public static void OnGuiOpenedPostfix(GuiDialogBlockEntityInventory __instance)
        {
            XInvTweaksSystem system = __instance.Inventory?.Api?.ModLoader.GetModSystem<XInvTweaksSystem>();
            if (system == null) return;
            system.OnInventoryOpend(__instance.SingleComposer.Bounds);
        }

        public static void OnGuiClosedPostfix(GuiDialogBlockEntityInventory __instance)
        {
            XInvTweaksSystem system = __instance.Inventory?.Api?.ModLoader.GetModSystem<XInvTweaksSystem>();
            if (system == null) return;
            system.OnInventoryClosed();
        }

    }//!class GuiDialogBlockEntityInventoryPatch
}//!namespace XInvTweaks
