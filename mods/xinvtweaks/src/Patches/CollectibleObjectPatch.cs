using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    internal class CollectibleObjectPatch
    {
        public static void TryPlaceBlockPostfix(Block __instance, bool __result, IPlayer byPlayer)
        {
            if (!__result) return;
            byPlayer?.Entity?.World.RegisterCallback((float dt) => Callback(__instance, byPlayer.InventoryManager?.ActiveHotbarSlot, byPlayer), XInvTweaksSystem.Config.delay);
        }

        public static void DamageItemPostfix(CollectibleObject __instance, Entity byEntity, ItemSlot itemslot)
        {
            EntityPlayer player = byEntity as EntityPlayer;
            if (player?.Api.Side != EnumAppSide.Client) return;
            if (__instance.Tool == null || itemslot == null) return;
            if (!player.Player.InventoryManager.Inventories.ContainsValue(itemslot.Inventory)) return;

            int durability = itemslot.Itemstack?.Attributes.GetInt("durability", 99999) ?? 0;
            if (XInvTweaksSystem.Config.toolSwitchDurability == 0 && itemslot.Itemstack != null) return;
            if (durability > XInvTweaksSystem.Config.toolSwitchDurability || durability == 0) return;

            ItemSlot bestResult = null;
            player.Player.InventoryManager.Find((ItemSlot slot2) =>
            {
                if (slot2 == itemslot) return false;
                if (slot2.Itemstack?.Collectible?.Tool == __instance.Tool)
                {
                    durability = slot2.Itemstack?.Attributes.GetInt("durability", 99999) ?? 0;
                    if (durability <= XInvTweaksSystem.Config.toolSwitchDurability) return false;

                    if (slot2.Itemstack.Collectible == __instance)
                    {
                        bestResult = slot2;
                        return true;
                    }
                    else if (bestResult == null) bestResult = slot2;
                    //example: prefer pickaxes over pro pickaxes
                    else if (bestResult.Itemstack.Collectible.GetType() != __instance.GetType() &&
                             slot2.Itemstack.Collectible.GetType() == __instance.GetType())
                    {
                        bestResult = slot2;
                    }

                }
                return false;
            });

            if (bestResult != null)
            {
                player.World.RegisterCallback((float dt) =>
                {
                    int slotID = -1;
                    for(int ii = 0; ii <= bestResult.Inventory.Count; ++ii)
                    { 
                        if (bestResult.Inventory[ii] == bestResult)
                        {
                            slotID = ii;
                            break;
                        }
                    }
                    object packet = bestResult.Inventory.TryFlipItems(slotID, itemslot);
                    (player.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
                }, 0);
            }
        }
        static public void InteractPrefix(CollectibleBehaviorGroundStorable __instance, out CollectibleObject __state, ItemSlot itemslot)
        {
            __state = __instance?.collObj ?? itemslot.Itemstack?.Collectible;
        }
        static public void InteractPostfix(CollectibleObject __state, ItemSlot itemslot, EntityAgent byEntity)
        {
            OnHeldInteractStart(__state, itemslot, byEntity);
        }

        static public void OnHeldInteractStartPostfix(CollectibleObject __instance, ItemSlot itemslot, EntityAgent byEntity)
        {
            OnHeldInteractStart(__instance, itemslot, byEntity);
        }
        static public void OnPlayerInteractPrefix(BlockEntityItemPile __instance, out CollectibleObject __state)
        {
            __state = __instance.inventory?[0]?.Itemstack?.Collectible;
        }

        static public void OnPlayerInteractPostfix(BlockEntityItemPile __instance, CollectibleObject __state, bool __result, IPlayer byPlayer)
        {
            if (__result) OnHeldInteractStart(__state, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
        }

        static public void OnHeldInteractStart(CollectibleObject __instance, ItemSlot slot, EntityAgent byEntity)
        {
            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (player == null) return;
            byEntity?.World.RegisterCallback((float dt) => Callback(__instance, slot, player), 100);
        }

        static private void Callback(CollectibleObject collectible, ItemSlot slot, IPlayer player)
        {
            if (player?.Entity?.Api.Side != EnumAppSide.Client || slot == null) return;
            if (!player.InventoryManager.Inventories.ContainsValue(slot.Inventory)) return;
            if (slot.Itemstack != null) return;
            if (slot is ItemSlotBackpack) return;

            ItemSlot bestResult = null;
            player.InventoryManager.Find((ItemSlot slot2) =>
            {
                if (!(slot2.Inventory is InventoryBasePlayer)) return false;
                if (slot2.Itemstack?.Collectible == collectible
                    && !(slot2 is ItemSlotCraftingOutput)
                    && !(slot2 is ItemSlotOffhand))
                {
                    bestResult = slot2;
                    return true;
                }
                return false;
            });

            if (bestResult != null)
            {
                ItemStackMoveOperation op = new ItemStackMoveOperation(player.Entity.World, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, bestResult.StackSize);
                object packet = player.InventoryManager.TryTransferTo(bestResult, slot, ref op);
                (player.Entity.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
            }
        }

    }//!class CollectibleObjectPatch
}//!namespace XInvTweaks
