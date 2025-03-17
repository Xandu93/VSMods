using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.Client.NoObf;
using Vintagestory.API.Config;

namespace XInvTweaks
{
    public class XInvTweaksSystem : ModSystem
    {
        public static InvTweaksConfig Config { get; private set; }
        private static Harmony harmony;

        private ICoreClientAPI capi;
        private ICoreAPI api;
        private string path;
        private ChestSortDialog chestSortDialog;

        public override double ExecuteOrder() => 1.0;

        private static void DoHarmonyPatch()
        {
            if (harmony == null)
            {
                harmony = new Harmony("XInvTweakPatch");
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        public XInvTweaksSystem() : base()
        {}

        public override void StartPre(ICoreAPI api)
        {
            this.api = api;
            LoadConfig();
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.SaveGameLoaded += OnWorldLoaded;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            this.api = api;

            PatchClient();
            RegisterKeys();

            if (Config.extendChestUi)
            {
                chestSortDialog = new ChestSortDialog(capi);

                GuiDialog invDialog = null;
                foreach (GuiDialog dialog in capi.Gui.LoadedGuis)
                {
                    if (dialog is GuiDialogInventory)
                    {
                        invDialog = dialog;
                        break;
                    }
                }

                if (invDialog != null)
                {
                    invDialog.OnOpened += () =>
                    {
                        chestSortDialog.OnInventoryOpend(invDialog?.Composers["maininventory"].Bounds);
                    };
                    invDialog.OnClosed += () =>
                    {
                        chestSortDialog.OnInventoryClosed();
                    };
                }
            }
            else chestSortDialog = null;
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony?.UnpatchAll("XInvTweakPatch");
            harmony = null;
        }

        public void LoadConfig()
        {
            path = "xinvtweaks.json";

            try
            {
                Config = api.LoadModConfig<InvTweaksConfig>(path);
            }
            catch (Exception)
            {
                Config = null;
            }
            if (Config == null) Config = new InvTweaksConfig();
            if (!Config.SortBlacklist.Contains("cart"))
            {
                Config.SortBlacklist.Add("cart");
            }

            if (api.Side == EnumAppSide.Client)
            {
                if (Config.SortOrder.Count == 0)
                {
                    Config.SortOrder.Add("priority");
                    Config.SortOrder.Add("lightinvert");
                    Config.SortOrder.Add("tool");
                    Config.SortOrder.Add("tooltier");
                    Config.SortOrder.Add("block");
                    Config.SortOrder.Add("storageflags");
                    Config.SortOrder.Add("id");
                    Config.SortOrder.Add("name");
                    Config.SortOrder.Add("durability");
                    Config.SortOrder.Add("attackpower");
                    Config.SortOrder.Add("satiety");
                    Config.SortOrder.Add("health");
                    Config.SortOrder.Add("intoxication");
                    Config.SortOrder.Add("stacksize");
                    Config.SortOrder.Add("density");
                    Config.SortOrder.Add("state");
                }
                if (Config.StackOrder.Count == 0)
                {
                    Config.StackOrder.Add("stacksize");
                    Config.StackOrder.Add("durability");
                    Config.StackOrder.Add("transition");
                }
                InventoryUtil.sortOrder = Config.SortOrder;
                InventoryUtil.stackOrder = Config.StackOrder;
                InventoryUtil.priorities = Config.Priorities;
                InventoryUtil.storageFlagsOrder = Config.StorageFlagsOrder;
            }

            api.StoreModConfig(Config, path);
        }

        public void OnWorldLoaded()
        {
            Dictionary<string, int> toAdd = new Dictionary<string, int>();
            List<string> toRemove = new List<string>();

            foreach (CollectibleObject collectible in api.World.Collectibles)
            {
                GroundStorageProperties storageProps = collectible.GetBehavior<CollectibleBehaviorGroundStorable>()?.StorageProps;
                if (storageProps == null) continue;
                Dictionary<string, int>.Enumerator pair = Config.BulkQuanties.GetEnumerator();

                bool found = false;
                while (pair.MoveNext() && !found)
                {
                    if (collectible.WildCardMatch(new AssetLocation(pair.Current.Key)))
                    {
                        int maxTransfer = Math.Min(collectible.MaxStackSize, storageProps.StackingCapacity);
                        int value = Math.Max(Math.Min(pair.Current.Value, maxTransfer), storageProps.BulkTransferQuantity);
                        storageProps.BulkTransferQuantity = value;
                        found = true;

                        if (pair.Current.Value != value)
                        {
                            if (!toAdd.ContainsKey(pair.Current.Key))
                            {
                                toRemove.Add(pair.Current.Key);
                                toAdd.Add(pair.Current.Key, value);
                            }
                        }
                    }
                }

                if (found) continue;

                if (collectible.MaxStackSize <= storageProps.BulkTransferQuantity) continue;
                if (storageProps.StackingCapacity <= storageProps.BulkTransferQuantity) continue;

                string path = collectible.FirstCodePart(0);
                int dashCount = collectible.Code.Path.Count(x => x == '-');
                for(int ii = 0; ii < dashCount; ++ii)
                {
                    path += "-*";
                }

                if (!toAdd.ContainsKey(path))
                {
                    toAdd.Add(path, storageProps.BulkTransferQuantity);
                }
            }

            foreach (string key in toRemove)
            {
                Config.BulkQuanties.Remove(key);
            }

            if (toAdd.Count > 0)
            {
                Dictionary<string, int>.Enumerator iterator = toAdd.GetEnumerator();
                while (iterator.MoveNext())
                {
                    if (!Config.BulkQuanties.ContainsKey(iterator.Current.Key))
                    {
                        Config.BulkQuanties.Add(iterator.Current.Key, iterator.Current.Value);
                    }
                }
            }

            if (Config != null && path != null)
            {
                api.StoreModConfig(Config, path);
            }
        }

        public void PatchClient()
        {
            DoHarmonyPatch();
            if (Config.tools) ManualPatch.PatchMethod(harmony, typeof(CollectibleObject), typeof(CollectibleObjectPatch), "DamageItem");
            //if (Config.groundStorage) ManualPatch.PatchMethod(harmony, typeof(CollectibleBehaviorGroundStorable), typeof(CollectibleObjectPatch), "Interact");
            if (Config.groundStorage) ManualPatch.PatchMethod(harmony, typeof(BlockEntityGroundStorage), typeof(BlockEntityGroundStoragePatch), "OnPlayerInteractStart");
            if (Config.blocks) ManualPatch.PatchMethod(harmony, typeof(Block), typeof(CollectibleObjectPatch), "TryPlaceBlock");
            if (Config.stairs) ManualPatch.PatchMethod(harmony, typeof(BlockStairs), typeof(CollectibleObjectPatch), "TryPlaceBlock");
            //if (Config.piles) ManualPatch.PatchMethod(harmony, typeof(BlockEntityItemPile), typeof(CollectibleObjectPatch), "OnPlayerInteract");
            if (Config.piles) ManualPatch.PatchMethod(harmony, typeof(BlockEntityItemPile), typeof(BlockEntityItemPilePatch), "OnPlayerInteract");
            if (Config.extendChestUi) ManualPatch.PatchMethod(harmony, typeof(GuiDialogBlockEntityInventory), typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiOpened");
            if (Config.extendChestUi) ManualPatch.PatchMethod(harmony, typeof(GuiDialogBlockEntityInventory), typeof(GuiDialogBlockEntityInventoryPatch), "OnGuiClosed");
            if (Config.crateSwitch) ManualPatch.PatchMethod(harmony, typeof(BlockEntityCrate), typeof(BlockEntityCratePatch), "OnBlockInteractStart");

            if (Config.seeds)
            {
                ManualPatch.PatchMethod(harmony, typeof(ItemPlantableSeed), typeof(CollectibleObjectPatch), "OnHeldInteractStart");

                Assembly assembly = capi.ModLoader.GetModSystem("XSkills.XSkills")?.GetType().Assembly;
                Type type = assembly?.GetType("XSkills.XSkillsItemPlantableSeed");
                if (type != null)
                {
                    ManualPatch.PatchMethod(harmony, type, typeof(CollectibleObjectPatch), "OnHeldInteractStart");
                }
            }

            if (Config.strgClick) ManualPatch.PatchMethod(harmony, typeof(InventoryBase), typeof(InventoryBasePatch), "ActivateSlot");
            if (Config.pushPullWheel) ManualPatch.PatchMethod(harmony, typeof(GuiElementItemSlotGridBase), typeof(GuiElementItemSlotGridBasePatch), "OnMouseWheel");
            if (Config.survivalPick) ManualPatch.PatchMethod(harmony, typeof(SystemMouseInWorldInteractions), typeof(SystemMouseInWorldInteractionsPatch), "HandleMouseInteractionsBlockSelected");

            capi.Event.LevelFinalize += OnWorldLoaded;
        }

        public void RegisterKeys()
        {
            capi.Input.RegisterHotKey("pushinventory", Lang.Get("xinvtweaks:pushinventory"), GlKeys.Z, HotkeyType.InventoryHotkeys, false, false, false);
            capi.Input.RegisterHotKey("sortinventories", Lang.Get("xinvtweaks:sortinventories"), GlKeys.Z, HotkeyType.InventoryHotkeys, false, false, true);
            capi.Input.RegisterHotKey("pullinventory", Lang.Get("xinvtweaks:pullinventory"), GlKeys.Z, HotkeyType.InventoryHotkeys, false, true, false);
            capi.Input.RegisterHotKey("sortbackpack", Lang.Get("xinvtweaks:sortbackpack"), GlKeys.Z, HotkeyType.InventoryHotkeys, true, false, false);
            capi.Input.RegisterHotKey("fillbackpack", Lang.Get("xinvtweaks:fillbackpack"), GlKeys.Z, HotkeyType.InventoryHotkeys, true, true, false);
            capi.Input.RegisterHotKey("clearhandslot", Lang.Get("xinvtweaks:clearhandslot"), GlKeys.F, HotkeyType.InventoryHotkeys, false, false, false);

            capi.Input.SetHotKeyHandler("pushinventory", (KeyCombination key) => InventoryUtil.PushInventory(capi));
            capi.Input.SetHotKeyHandler("sortinventories", (KeyCombination key) => InventoryUtil.SortInventories(capi));
            capi.Input.SetHotKeyHandler("pullinventory", (KeyCombination key) => InventoryUtil.PullInventory(capi));
            capi.Input.SetHotKeyHandler("sortbackpack", (KeyCombination key) => InventoryUtil.SortBackpack(capi));
            capi.Input.SetHotKeyHandler("fillbackpack", (KeyCombination key) => InventoryUtil.FillBackpack(capi));
            capi.Input.SetHotKeyHandler("clearhandslot", (KeyCombination key) => InventoryUtil.ClearHandSlot(capi));
        }

        public void OnInventoryOpend(ElementBounds parent)
        {
            chestSortDialog?.OnInventoryOpend(parent);
        }

        public void OnInventoryClosed()
        {
            chestSortDialog?.OnInventoryClosed();
        }

    }//!class XToolSwitchSystem
}//!namespace XInvTweaks
