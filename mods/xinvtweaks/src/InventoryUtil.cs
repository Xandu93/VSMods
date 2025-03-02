using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace XInvTweaks
{
    public class InventoryUtil
    {
        internal static List<string> sortOrder = new List<string>();
        internal static List<string> stackOrder = new List<string>();
        internal static List<EnumItemStorageFlags> storageFlagsOrder = new List<EnumItemStorageFlags>();
        internal static Dictionary<string, int> priorities = new Dictionary<string, int>();
        internal static string priority = null;

        internal class CollectibleComparer : IComparer<CollectibleObject>
        {
            public static int SetPrio(CollectibleObject coll)
            {
                int result = 0;
                foreach (KeyValuePair<string, int> entry in priorities)
                {
                    if (coll.WildCardMatch(entry.Key))
                    {
                        result = entry.Value;
                        break;
                    }
                }
                coll.Attributes.Token.Last?.AddAfterSelf(new JProperty("sortpriority", result));
                return result;
            }

            public int Compare(CollectibleObject x, CollectibleObject y)
            {
                if (x == y) return 0;

                if (priority != null)
                {
                    bool xmatch = x.WildCardMatch(priority);
                    bool ymatch = y.WildCardMatch(priority);
                    if (xmatch != ymatch)
                    {
                        if (xmatch) return -1;
                        if (ymatch) return  1;
                    }
                    xmatch = x.GetHeldItemName(new ItemStack(x)).Contains(priority, StringComparison.OrdinalIgnoreCase);
                    ymatch = y.GetHeldItemName(new ItemStack(y)).Contains(priority, StringComparison.OrdinalIgnoreCase);
                    if (xmatch != ymatch)
                    {
                        if (xmatch) return -1;
                        if (ymatch) return 1;
                    }
                }

                if (sortOrder.Count == 0)
                {
                    return x.Id.CompareTo(y.Id);
                }

                int result = 0;
                foreach (string compareType in sortOrder)
                {
                    result = Compare(x, y, compareType);
                    if (result != 0) return result;
                }
                return result;
            }

            public int Compare(CollectibleObject x, CollectibleObject y, string compareType)
            {
                int result = 0;
                switch(compareType)
                {
                    case "id":
                        result = x.Id.CompareTo(y.Id);
                        break;

                    case "idinvert":
                        result = y.Id.CompareTo(x.Id);
                        break;

                    case "name":
                        result = x.GetHeldItemName(new ItemStack(x)).CompareTo(y.GetHeldItemName(new ItemStack(y)));
                        break;

                    case "nameinvert":
                        result = y.GetHeldItemName(new ItemStack(y)).CompareTo(x.GetHeldItemName(new ItemStack(x)));
                        break;

                    case "block":
                        result = x.ItemClass - y.ItemClass;
                        break;

                    case "item":
                        result = y.ItemClass - x.ItemClass;
                        break;

                    case "durability":
                        result = x.Durability.CompareTo(y.Durability);
                        break;

                    case "durabilityinvert":
                        result = y.Durability.CompareTo(x.Durability);
                        break;

                    case "attackpower":
                        result = x.AttackPower.CompareTo(y.AttackPower);
                        break;

                    case "attackpowerinvert":
                        result = y.AttackPower.CompareTo(x.AttackPower);
                        break;

                    case "stacksize":
                        result = x.MaxStackSize.CompareTo(y.MaxStackSize);
                        break;

                    case "stacksizeinvert":
                        result = y.MaxStackSize.CompareTo(x.MaxStackSize);
                        break;

                    case "tool":
                        result = ((int?)x.Tool ?? 100) - ((int?)y.Tool ?? 100);
                        break;

                    case "toolinvert":
                        result = ((int?)y.Tool ?? 100) - ((int?)x.Tool ?? 100);
                        break;

                    case "tooltier":
                        result = x.ToolTier.CompareTo(y.ToolTier);
                        break;

                    case "tooltierinvert":
                        result = y.ToolTier.CompareTo(x.ToolTier);
                        break;

                    case "light":
                        result = x.LightHsv[2] - y.LightHsv[2];
                        break;

                    case "lightinvert":
                        result = y.LightHsv[2] - x.LightHsv[2];
                        break;

                    case "storageflags":
                        foreach (EnumItemStorageFlags flags in storageFlagsOrder)
                        {
                            EnumItemStorageFlags flgasx = x.StorageFlags & flags;
                            EnumItemStorageFlags flgasy = y.StorageFlags & flags;
                            if (flgasx != flgasy)
                            {
                                result = flgasy - flgasx;
                                break;
                            }
                        }
                        break;

                    case "priority":
                        if (priorities.Count > 0 && x.Attributes != null && y.Attributes != null)
                        {
                            int xprio = x.Attributes["sortpriority"].AsInt(-1);
                            int yprio = y.Attributes["sortpriority"].AsInt(-1);

                            if (xprio == -1)
                            {
                                xprio = SetPrio(x);
                            }
                            if (yprio == -1)
                            {
                                yprio = SetPrio(y);
                            }
                            int prioSort = yprio.CompareTo(xprio);
                            if (prioSort != 0) return prioSort;
                        }
                        break;

                    default:
                        break;
                }
                return result;
            }
        }

        internal class SlotComparer : IComparer<ItemSlot>
        {
            private StackComparer StackComparer = new StackComparer();
            public int Compare(ItemSlot x, ItemSlot y)
            {
                return StackComparer.Compare(x.Itemstack, y.Itemstack);
            }
        }

        internal class StackComparer : IComparer<ItemStack>
        {
            public int Compare(ItemStack x, ItemStack y)
            {
                if (x == y) return 0;
                if (stackOrder.Count == 0) return 0;

                int result = 0;
                foreach (string compareType in stackOrder)
                {
                    result = Compare(x, y, compareType);
                    if (result != 0) return result;
                }
                return result;
            }

            public int Compare(ItemStack x, ItemStack y, string compareType)
            {
                int result = 0;
                switch (compareType)
                {
                    case "durability":
                        result =
                            x.Collectible.GetRemainingDurability(x).CompareTo(
                            y.Collectible.GetRemainingDurability(y));
                        break;

                    case "durabilityinvert":
                        result =
                            y.Collectible.GetRemainingDurability(y).CompareTo(
                            x.Collectible.GetRemainingDurability(x));
                        break;

                    case "stacksize":
                        result = y.StackSize.CompareTo(x.StackSize);
                        break;

                    case "stacksizeinvert":
                        result = x.StackSize.CompareTo(y.StackSize);
                        break;

                    default:
                        break;
                }
                return result;
            }
        }

        public static bool PushInventory(ICoreClientAPI capi)
        {
            SortIntoInventory(capi);
            return true;
        }

        public static bool SortInventories(ICoreClientAPI capi)
        {
            SortOpenInventories(capi);
            return true;
        }

        public static bool PullInventory(ICoreClientAPI capi)
        {
            PullInventories(capi);
            return true;
        }

        public static bool SortBackpack(ICoreClientAPI capi)
        {
            IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            SortInventory(capi, backpack, 4, true);
            return true;
        }

        public static bool FillBackpack(ICoreClientAPI capi)
        {
            IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            
            List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;
            foreach (IInventory inventory in inventories)
            {
                if (inventory is InventoryBasePlayer) continue;
                if (inventory is InventoryTrader) continue;
                FillInventory(capi, inventory, hotbar);
                FillInventory(capi, inventory, backpack);
            }
            FillInventory(capi, backpack, hotbar);
            return true;
        }

        public static void SortIntoInventory(ICoreClientAPI capi)
        {
            IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);

            List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

            foreach (IInventory inventory in inventories)
            {
                if (inventory is InventoryBasePlayer) continue;
                Dictionary<CollectibleObject, int> collectibles = new Dictionary<CollectibleObject, int>();

                InventoryTrader trader = inventory as InventoryTrader;
                if (trader != null)
                {
                    foreach (ItemSlot slot in trader.BuyingSlots)
                    {
                        if (slot.Itemstack != null && !collectibles.ContainsKey(slot.Itemstack.Collectible))
                        {
                            if ((slot as ItemSlotTrade)?.TradeItem?.Stock == 0) continue;
                            collectibles.Add(slot.Itemstack.Collectible, slot.StackSize * (slot as ItemSlotTrade)?.TradeItem?.Stock ?? 0);
                        }
                    }
                }
                else
                {
                    foreach (ItemSlot slot in inventory)
                    {
                        if (slot.Itemstack != null && !collectibles.ContainsKey(slot.Itemstack.Collectible))
                        {
                            collectibles.Add(slot.Itemstack.Collectible, 0);
                        }
                    }
                }

                SortIntoInventory(capi, backpack, inventory, collectibles, 4, true);
                SortIntoInventory(capi, hotbar, inventory, collectibles, 0, false);
            }
        }

        public static void SortIntoInventory(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv, Dictionary<CollectibleObject, int> collectibles, int first, bool lockedSlots)
        {
            if (IsBlacklisted(sourceInv)) return;
            if (IsBlacklisted(destInv)) return;

            int slotID = -1;
            foreach (ItemSlot slot in sourceInv)
            {
                slotID++;
                if (slotID < first) continue;
                if (slot.Itemstack == null) continue;
                if (lockedSlots)
                {
                    int reverseSlotId = slotID - sourceInv.Count;
                    if (XInvTweaksSystem.Config.LockedSlots.Contains(slotID - first)) continue;
                    if (XInvTweaksSystem.Config.LockedSlots.Contains(reverseSlotId)) continue;
                }
                CollectibleObject key = slot.Itemstack.Collectible;
                if (collectibles.ContainsKey(key))
                {
                    while (slot.StackSize > 0)
                    {
                        int demand = collectibles[key];
                        WeightedSlot dest = destInv.GetBestSuitedSlot(slot);
                        if (dest == null || dest.slot == null) break;
                        if (dest.slot.Itemstack != null)
                        {
                            //for some reason the game can say it would be a good idea to merge two cooking pots
                            //and then crashes when you actually try to merge them
                            //this should prevent it
                            if (dest.slot.Itemstack.StackSize >= dest.slot.Itemstack.Collectible.MaxStackSize) break;
                        }

                        if (slot is ItemSlotOffhand) break;
                        int transfer = demand > 0 ? Math.Min(demand, slot.StackSize) : slot.StackSize;

                        ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, transfer);
                        object obj = capi.World.Player.InventoryManager.TryTransferTo(slot, dest.slot, ref op);
                        if (obj != null) capi.Network.SendPacketClient(obj);

                        //rare case for specific items where GetBestSuitedSlot does not return a valid slot
                        if (op.MovedQuantity == 0)
                        {
                            foreach(ItemSlot destSlot in destInv)
                            {
                                if (!destSlot.Empty) continue;
                                op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, transfer);
                                obj = capi.World.Player.InventoryManager.TryTransferTo(slot, destSlot, ref op);
                                if (obj != null) capi.Network.SendPacketClient(obj);
                                break;
                            }
                            if (op.MovedQuantity == 0) break;
                        }

                        if (demand <= 0) continue;

                        demand = demand - op.MovedQuantity;
                        if (demand <= 0) collectibles.Remove(key);
                        else collectibles[key] = demand;
                        break;
                    }
                }
            }
        }
        
        private static int GetFlagCount(long value)
        {
            int count = 0;
            while(value != 0)
            {
                value = value & (value - 1);
                count++;
            }
            return count;
        }

        public static void SortInventory(ICoreClientAPI capi, IInventory inventory, int first = 0, bool lockedSlots = false)
        {
            if (inventory is InventorySmelting) return;
            if (inventory is InventoryTrader) return;
            if (inventory.PutLocked || inventory.TakeLocked) return;
            if (IsBlacklisted(inventory)) return;

            //create dictionary
            int counter = -1;
            SortedDictionary<CollectibleObject, List<ItemSlot>> slots = new(new CollectibleComparer());
            Dictionary<EnumItemStorageFlags, List<ItemSlot>> destDic = new();
            
            foreach (ItemSlot slot in inventory)
            {
                counter++;
                if (counter < first) continue;
                if (slot is ItemSlotLiquidOnly) continue;
                int reverseSlotId = counter - inventory.Count;
                if (lockedSlots && XInvTweaksSystem.Config.LockedSlots.Contains(counter - first)) continue;
                if (lockedSlots && XInvTweaksSystem.Config.LockedSlots.Contains(reverseSlotId)) continue;

                destDic.TryGetValue(slot.StorageType, out List<ItemSlot> destList);
                if (destList == null)
                {
                    destList = new List<ItemSlot>();
                    destDic[slot.StorageType] = destList;
                }
                destList.Add(slot);

                if (slot.Itemstack == null)
                {
                    continue;
                }

                List<ItemSlot> slotList;
                if (!slots.TryGetValue(slot.Itemstack.Collectible, out slotList))
                {
                    slotList = new List<ItemSlot>();
                    slots.Add(slot.Itemstack.Collectible, slotList);
                }
                slotList.Add(slot);
            }

            //merge stacks
            bool merged = false;
            foreach (List<ItemSlot> slotList in slots.Values)
            {
                ItemSlot notFull = null;
                List<ItemSlot> removeList = new List<ItemSlot>();
                foreach (ItemSlot slot in slotList)
                {
                    if (slot.StackSize < slot.Itemstack.Collectible.MaxStackSize)
                    {
                        if (notFull == null)
                        {
                            notFull = slot;
                            continue;
                        }

                        ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, slot.StackSize);
                        object obj = capi.World.Player.InventoryManager.TryTransferTo(slot, notFull, ref op);
                        if (obj != null && op.MovedQuantity > 0)
                        {
                            capi.Network.SendPacketClient(obj);
                            merged = true;
                        }

                        if (notFull.StackSize >= notFull.Itemstack.Collectible.MaxStackSize) notFull = null;
                        if (slot.Itemstack == null) removeList.Add(slot);
                        else notFull ??= slot;
                    }
                }

                //remove empty stacks
                foreach (ItemSlot slot in removeList)
                {
                    slotList.Remove(slot);
                }
            }

            if (merged) return;

            //rearrange stacks
            foreach (List<ItemSlot> slotList in slots.Values)
            {
                slotList.Sort(new SlotComparer());
                ItemSlot source;

                while (slotList.Count > 0)
                {
                    //find the best inventory for the stack
                    //must be done for each stack of the same item
                    //because an inventory can become full
                    source = slotList.PopOne();
                    ItemStack stack = source.Itemstack;
                    EnumItemStorageFlags flags = stack.Collectible.StorageFlags;
                    EnumItemStorageFlags containerFlags = 0;
                    int flagCount = 0xffffff;

                    //find the most specific container that fits the item
                    foreach(EnumItemStorageFlags slotFlags in destDic.Keys)
                    {
                        if ((flags & slotFlags) == 0) continue;
                        int count = GetFlagCount((long)slotFlags);
                        if (count < flagCount)
                        {
                            containerFlags = slotFlags;
                            flagCount = count;
                        }
                    }

                    if (containerFlags == 0) break;
                    List<ItemSlot> destList = destDic[containerFlags];
                    if (destList.Count == 0)
                    {
                        destDic.Remove(containerFlags);
                        continue;
                    }

                    foreach (ItemSlot dest in destList)
                    {
                        if (dest == source)
                        {
                            destList.Remove(dest);
                            if (destList.Count == 0)
                            {
                                destDic.Remove(containerFlags);
                            }
                            break;
                        }

                        object obj = dest.Inventory.TryFlipItems(dest.Inventory.GetSlotId(dest), source);
                        if (obj != null)
                        {
                            if (source.Itemstack != null)
                            {
                                //updates the slot list so that it stays correct after switching the items in the slots
                                slots.TryGetValue(source.Itemstack.Collectible, out List<ItemSlot> sourceList);

                                int index = 0;
                                if (sourceList.Count == 0) return;
                                while (sourceList[index] != dest)
                                {
                                    index++;
                                    if (index >= sourceList.Count) return;
                                }
                                sourceList[index] = source;
                            }

                            capi.Network.SendPacketClient(obj);
                            destList.Remove(dest);
                            if (destList.Count == 0)
                            {
                                destDic.Remove(containerFlags);
                            }
                            break;
                        }
                    }
                }
            }
        }

        public static void SortOpenInventories(ICoreClientAPI capi)
        {
            List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

            foreach (IInventory inventory in inventories)
            {
                if (inventory is InventoryBasePlayer) continue;
                SortInventory(capi, inventory);
            }
        }

        public static void PullInventories(ICoreClientAPI capi)
        {
            IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
            IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
            List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

            foreach (IInventory inventory in inventories)
            {
                if (inventory is InventoryBasePlayer) continue;

                PullInventory(capi, inventory, backpack);
                PullInventory(capi, inventory, hotbar);
            }
        }

        public static void FillInventory(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv)
        {
            if (IsBlacklisted(sourceInv)) return;
            if (IsBlacklisted(destInv)) return;

            Dictionary<CollectibleObject, List<ItemSlot>> toFill = new Dictionary<CollectibleObject, List<ItemSlot>>();
            foreach (ItemSlot slot in destInv)
            {
                if (slot.Itemstack == null) continue;
                if (slot.StackSize < slot.MaxSlotStackSize)
                {
                    toFill.TryGetValue(slot.Itemstack.Collectible, out List<ItemSlot> list);
                    if (list == null)
                    {
                        list = new List<ItemSlot>();
                        toFill.Add(slot.Itemstack.Collectible, list);
                    }
                    list.Add(slot);
                }
            }

            foreach (ItemSlot sourceSlot in sourceInv)
            {
                if (sourceSlot.Itemstack == null) continue;
                toFill.TryGetValue(sourceSlot.Itemstack.Collectible, out List<ItemSlot> list);
                if (list == null) continue;
                foreach (ItemSlot destSlot in list)
                {
                    if (destSlot.StackSize >= destSlot.MaxSlotStackSize) continue;
                    ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, sourceSlot.StackSize);
                    object obj = capi.World.Player.InventoryManager.TryTransferTo(sourceSlot, destSlot, ref op);
                    if (obj != null) capi.Network.SendPacketClient(obj);
                    if (sourceSlot.StackSize == 0) break;
                }
            }
        }

        public static void PullInventory(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv)
        {
            if (IsBlacklisted(sourceInv)) return;
            if (IsBlacklisted(destInv)) return;

            int first = 0;
            int last = sourceInv.Count;
            InventoryTrader trader = sourceInv as InventoryTrader;
            if (trader != null)
            {
                first = 35;
                last = trader.Count - 1;
            }

            for (int ii = first; ii < last; ++ii)
            {
                if (sourceInv[ii] is ItemSlotLiquidOnly) continue;
                if (sourceInv[ii].Itemstack == null) continue;
                while (sourceInv[ii].StackSize > 0)
                {
                    WeightedSlot dest = destInv.GetBestSuitedSlot(sourceInv[ii]);
                    if (dest == null || dest.slot == null) break;

                    if (dest.slot.Itemstack != null)
                    {
                        //for some reason the game can say it would be a good idea to merge two cooking pots
                        //and then crashes when you actually try to merge them
                        //this should prevent it
                        if (dest.slot.Itemstack.StackSize >= dest.slot.Itemstack.Collectible.MaxStackSize) break;
                    }

                    if (destInv[ii] is ItemSlotOffhand) break;

                    ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, sourceInv[ii].StackSize);
                    object obj = capi.World.Player.InventoryManager.TryTransferTo(sourceInv[ii], dest.slot, ref op);
                    if (obj != null) capi.Network.SendPacketClient(obj);
                }
            }
        }

        public static bool ClearHandSlot(ICoreClientAPI capi)
        {
            ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
            if (slot == null) return false;
            if (slot.Empty) return false;
            ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.None, EnumModifierKey.SHIFT, EnumMergePriority.AutoMerge);
            object[] objs = capi.World.Player.InventoryManager.TryTransferAway(slot, ref op, true, true);
            if (objs == null) return false;
            foreach (object obj in objs)
            {
                if (obj != null) capi.Network.SendPacketClient(obj);
            }
            return true;
        }

        public static void FindBestSlot(CollectibleObject collectible, IPlayer player, ref ItemSlot bestSlot)
        {
            if (collectible == null) return;
            if (player == null) return;
            ItemSlot tempSlot = null;

            player.InventoryManager.Find((ItemSlot slot) =>
            {
                if (slot.Itemstack?.Collectible == collectible)
                {
                    if (tempSlot != null)
                    {
                        if (tempSlot.Inventory == slot.Inventory)
                        {
                            if (slot.StackSize < tempSlot.StackSize)
                            {
                                tempSlot = slot;
                            }
                        }
                        else if (slot.Inventory.ClassName == GlobalConstants.hotBarInvClassName)
                        {
                            tempSlot = slot;
                        }
                    }
                    else tempSlot = slot;
                }
                return false;
            });
            if (tempSlot != null) bestSlot = tempSlot;
        }

        public static int GetSlotID(ItemSlot slot)
        {
            int slotID = -1;
            if (slot == null) return -1;
            for (int ii = 0; ii < slot.Inventory.Count; ++ii)
            {
                if (slot.Inventory[ii] == slot)
                {
                    slotID = ii;
                    break;
                }
            }
            return slotID;
        }

        static public void FindPushableCollectible(InventoryBase inv, IPlayer player)
        {
            if (!player.Entity.Controls.ShiftKey) return;
            ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;
            ItemSlot bestSlot = null;
            foreach (ItemSlot slot in inv)
            {
                if (slot.Empty) continue;
                CollectibleObject collectible = slot.Itemstack.Collectible;
                if (collectible == hotbarSlot.Itemstack?.Collectible) return;
                FindBestSlot(collectible, player, ref bestSlot);
            }

            int slotID = GetSlotID(bestSlot);
            if (slotID == -1 || bestSlot == null) return;
            object packet = bestSlot.Inventory.TryFlipItems(slotID, player.InventoryManager.ActiveHotbarSlot);
            if (packet != null) (player.Entity.Api as ClientCoreAPI)?.Network.SendPacketClient(packet);
        }

        public static bool IsBlacklisted(IInventory inventory)
        {
            int indexOf = inventory.ClassName.IndexOf('/');
            string invName = indexOf > 0 ? inventory.ClassName.Substring(0, indexOf) : inventory.ClassName;
            return XInvTweaksSystem.Config.SortBlacklist.Contains(invName);
        }
    }//!class InventoryUtil
}//!namespace XInvTweaks
