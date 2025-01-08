using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace AdvancedChests
{
    /// <summary>
    /// Called when an inventory shall be created.
    /// Should return the inventory.
    /// </summary>
    /// <param name="quantitySLots">The quantity slots.</param>
    /// <param name="key">The key.</param>
    /// <param name="api">The API.</param>
    /// <returns></returns>
    public delegate InventoryGeneric CreateInventoryDelegate(int quantitySlots, string key, ICoreAPI api);

    /// <summary>
    /// The core of the AdvancedChests mod.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.ModSystem" />
    public class AdvancedChestsSystem : ModSystem
    {
        /// <summary>
        /// The API
        /// </summary>
        ICoreAPI api;

        /// <summary>
        /// The shared inventories
        /// </summary>
        protected Dictionary<string, InventoryGeneric> SharedInventories;

        /// <summary>
        /// The personal inventories
        /// A personal inventory is a map wide player specific inventory that can be accessed by a personal chest.
        /// </summary>
        protected Dictionary<string, InventoryGeneric> PersonalInventories;

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public Config Config { get; protected set; }

        /// <summary>
        /// The channel for network communication with the clients.
        /// </summary>
        private INetworkChannel channel;

        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            this.api = api;

            this.Config = api.LoadModConfig<Config>("advancedchests.json") ?? new Config();

            this.Config.inventoryNames.Add(GlobalConstants.backpackInvClassName);
            this.Config.inventoryNames.Add(GlobalConstants.characterInvClassName);
            this.Config.inventoryNames.Add(GlobalConstants.craftingInvClassName);
            this.Config.inventoryNames.Add(GlobalConstants.hotBarInvClassName);
            this.Config.inventoryNames.Add(GlobalConstants.mousecursorInvClassName);
            api.StoreModConfig(this.Config, "advancedchests.json");

            if (api is ICoreServerAPI sapi)
            {
                IServerNetworkChannel channel = sapi.Network.RegisterChannel("AdvancedChests");
                channel.RegisterMessageType(typeof(Config));
                this.channel = channel;

                sapi.Event.GameWorldSave += OnSaveGameSaving;
                sapi.Event.SaveGameLoaded += OnSaveGameLoading;
                sapi.Event.PlayerDeath += OnPlayerDeath;
                sapi.Event.PlayerJoin += OnPlayerJoin;
            }
            else if (api is ICoreClientAPI capi)
            {
                IClientNetworkChannel channel = capi.Network.RegisterChannel("AdvancedChests");
                channel.RegisterMessageType(typeof(Config));
                channel.SetMessageHandler<Config>(this.OnConfigReceived);
                this.channel = channel;
            }
        }

        /// <summary>
        /// Start method, called on both server and client after all mods already received a call to StartPre(), but before Blocks/Items/Entities/Recipes etc are loaded and some time before StartServerSide / StartClientSide.
        /// <br />Typically used to register for events and network packets etc
        /// <br />Typically also used in a mod's core to register the classes for your blocks, items, entities, blockentities, behaviors etc, prior to loading assets
        /// <br /><br />Do not make calls to api.Assets at this stage, the assets may not be found, resulting in errors (even if the json file exists on disk). Use AssetsLoaded() stage instead.
        /// </summary>
        /// <param name="api"></param>
        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            api.RegisterBlockEntityClass("FilterContainer", typeof(BlockEntityFilterContainer));
            api.RegisterBlockEntityClass("PersonalContainer", typeof(BlockEntityPersonalContainer));
            api.RegisterBlockBehaviorClass("PersonalContainer", typeof(BlockBehaviorPersonalContainer));
            api.RegisterBlockEntityClass("Coffin", typeof(BlockEntityCoffin));
            api.RegisterBlockEntityClass("SharedContainer", typeof(BlockEntitySharedContainer));
            api.RegisterBlockEntityClass("SortingContainer", typeof(BlockEntitySortingContainer));
            api.RegisterBlockClass("BlockPersonalChest", typeof(BlockPersonalContainer));
            api.RegisterBlockEntityClass("InfinityContainer", typeof(BlockEntityInfinityContainer));
            api.RegisterBlockEntityClass("VoidContainer", typeof(BlockEntityVoidContainer));
        }

        /// <summary>
        /// Called when the configuration was received.
        /// </summary>
        /// <param name="config">The configuration.</param>
        public void OnConfigReceived(Config config)
        {
            this.Config = config;
        }

        /// <summary>
        /// Called when a player joins the game.
        /// Sends the configuration to the player.
        /// </summary>
        /// <param name="byPlayer"></param>
        public void OnPlayerJoin(IServerPlayer byPlayer)
        {
            (this.channel as IServerNetworkChannel)?.SendPacket(this.Config, byPlayer);
        }

        /// <summary>
        /// Called after all assets were loaded.
        /// </summary>
        /// <param name="api">The API.</param>
        public override void AssetsFinalize(ICoreAPI api)
        {
            base.AssetsFinalize(api);

            api.World.GridRecipes.RemoveAll((GridRecipe recipe) => 
            {
                switch (recipe.Output.Code.FirstCodePart())
                {
                    case "filterchest":
                        if (!this.Config.enableFilterContainer) return true;
                        break;
                    case "personalchest":
                        if (!this.Config.enablePeronalContainer) return true;
                        break;
                    case "coffin":
                        if (!this.Config.enableCoffin) return true;
                        break;
                    case "sharedchest":
                        if (!this.Config.enableSharedContainer) return true;
                        break;
                    case "sortingchest":
                        if (!this.Config.enableSortingContainer) return true;
                        break;
                    case "infinitychest":
                        if (!this.Config.enableInfinityContainer) return true;
                        break;
                    case "voidchest":
                        if (!this.Config.enableVoidContainer) return true;
                        break;
                }
                return false;
            });
        }

        /// <summary>
        /// Called when a player died.
        /// Used to place the coffin.
        /// </summary>
        /// <param name="byPlayer">The by player.</param>
        /// <param name="damageSource">The damage source.</param>
        protected void OnPlayerDeath(IServerPlayer byPlayer, DamageSource damageSource)
        {
            IWorldAccessor world = api.World;
            if (world == null || byPlayer?.Entity == null) return;
            if (world.Config.GetString("deathPunishment", "drop") == "keep") return;

            //search for a coffin in the inventory
            ItemStack coffinStack = null;
            foreach (string invName in Config.inventoryNames)
            {
                IInventory inv = byPlayer.InventoryManager.GetOwnInventory(invName);
                if (inv == null) continue;
                coffinStack = GetCoffinFromInventory(inv);
                if (coffinStack != null) break;
            }
            if (coffinStack == null) return;

            BlockPos pos = byPlayer.Entity.SidedPos.AsBlockPos;
            pos = FindPositionForCoffin(pos, world, coffinStack.Block);
            if (pos == null)
            {
                byPlayer.InventoryManager.TryGiveItemstack(coffinStack);
                return;
            }

            world.BlockAccessor.SetBlock(coffinStack.Id, pos, coffinStack);
            BlockEntityCoffin coffin = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCoffin;
            if (coffin == null) return;

            coffin.SetLabel("RIP\n" + byPlayer.PlayerName, ColorUtil.BlackArgb);
            coffin.TransferPlayerInventory(byPlayer, Config.inventoryNames);
        }

        /// <summary>
        /// Searches for a coffin in the given inventory.
        /// </summary>
        /// <param name="inv">The inventory.</param>
        /// <returns></returns>
        protected ItemStack GetCoffinFromInventory(IInventory inv)
        {
            if (inv == null) return null;
            foreach (ItemSlot slot in inv)
            {
                if (slot.Itemstack?.Block?.EntityClass == "Coffin")
                {
                    ItemStack itemStack = slot.TakeOut(1);
                    return itemStack;
                }
            }
            return null;
        }

        /// <summary>
        /// Finds a position to place the coffin.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="world">The world.</param>
        /// <returns></returns>
        protected BlockPos FindPositionForCoffin(BlockPos pos, IWorldAccessor world, Block blockCoffin)
        {
            if (world.BlockAccessor.GetBlock(pos).Id == 0) return pos.Copy();
            for(int yy = -1; yy < 1; yy++)
            {
                for (int zz = -1; zz < 1; zz++)
                {
                    for (int xx = -1; xx < 1; xx++)
                    {
                        Block block = world.BlockAccessor.GetBlock(new BlockPos(pos.X + xx, pos.Y + yy, pos.Z + zz, pos.dimension));
                        if (block == null) continue;
                        if (block.Id == 0 || block.IsReplacableBy(blockCoffin))
                        {
                            return new BlockPos(pos.X + xx, pos.Y + yy, pos.Z + zz, pos.dimension);
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Loads the inventory dictionary.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="quantitySlots">The quantity slots.</param>
        /// <param name="createInventory">The create inventory.</param>
        /// <returns></returns>
        protected Dictionary<string, InventoryGeneric> LoadInventoryDictionary(string name, int quantitySlots, CreateInventoryDelegate createInventory)
        {
            byte[] data = (api as ICoreServerAPI)?.WorldManager.SaveGame.GetData(name + "Invs");
            Dictionary<string, InventoryGeneric> Inventories = new Dictionary<string, InventoryGeneric>();

            if (data != null)
            {
                Dictionary<string, byte[]> TreeInventories = SerializerUtil.Deserialize<Dictionary<string, byte[]>>(data);
                foreach (KeyValuePair<string, byte[]> entry in TreeInventories)
                {
                    TreeAttribute tree = new TreeAttribute();
                    tree.FromBytes(entry.Value);

                    InventoryGeneric inv = createInventory(quantitySlots, name + "-" + entry.Key, api);
                    Inventories.Add(entry.Key, inv);
                    inv.FromTreeAttributes(tree);
                }
            }
            return Inventories;
        }

        /// <summary>
        /// Called while save game loading.
        /// Saves the inventories.
        /// </summary>
        protected void OnSaveGameLoading()
        {
            if (PersonalInventories == null)
            {
                PersonalInventories =
                    LoadInventoryDictionary("personal",
                    Config.personalChestSize,
                    (int quantitySLots, string key, ICoreAPI api) => 
                    {
                        InventoryPersonal inv = new InventoryPersonal(quantitySLots, key, api);
                        InitPersonalContainer(inv);
                        return inv;
                    });
            }
            if (SharedInventories == null)
            {
                SharedInventories = 
                    LoadInventoryDictionary("shared",
                    Config.sharedChestSize,
                    (int quantitySLots, string key, ICoreAPI api) => new InventoryShared(quantitySLots, key, api));
            }
        }

        /// <summary>
        /// Saves the inventory dictionary.
        /// </summary>
        /// <param name="inventories">The inventories.</param>
        /// <param name="name">The name.</param>
        protected void SaveInventoryDictionary(Dictionary<string, InventoryGeneric> inventories, string name)
        {
            Dictionary<string, byte[]> TreeInventories = new Dictionary<string, byte[]>();
            foreach (KeyValuePair<string, InventoryGeneric> entry in inventories)
            {
                TreeAttribute tree = new TreeAttribute();
                entry.Value.ToTreeAttributes(tree);
                TreeInventories.Add(entry.Key, tree.ToBytes());
            }

            byte[] data = SerializerUtil.Serialize(TreeInventories);
            (api as ICoreServerAPI).WorldManager.SaveGame.StoreData(name + "Invs", data);
        }

        /// <summary>
        /// Saves the inventory dictionaries.
        /// </summary>
        protected void OnSaveGameSaving()
        {
            if (PersonalInventories != null)
            {
                SaveInventoryDictionary(PersonalInventories, "personal");
            }
            if (SharedInventories != null)
            {
                SaveInventoryDictionary(SharedInventories, "shared");
            }
        }

        /// <summary>
        /// Gets a personal inventory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public InventoryGeneric GetPersonalInventory(string key)
        {
            if (PersonalInventories == null) OnSaveGameLoading();
            InventoryGeneric result;
            PersonalInventories.TryGetValue(key, out result);
            return result;
        }

        /// <summary>
        /// Gets or creates a personal inventory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public InventoryGeneric GetOrCreatePersonalInventory(string key)
        {
            InventoryGeneric result = GetPersonalInventory(key);
            if (result != null)
            {
                InventoryShared Shared = result as InventoryShared;
                if (Shared?.Count != Config.personalChestSize) Shared.TryResize(Config.personalChestSize);
                return result;
            }

            result = new InventoryPersonal(Config.personalChestSize, "personal-" + key, api);
            InitPersonalContainer(result);
            PersonalInventories.Add(key, result);
            return result;
        }

        /// <summary>
        /// Gets or creates a personal inventory.
        /// </summary>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public InventoryGeneric GetOrCreatePersonalInventory(IPlayer player)
        {
            return GetOrCreatePersonalInventory(player.PlayerUID);
        }

        /// <summary>
        /// Gets a shared inventory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private InventoryGeneric GetSharedInventory(string key)
        {
            if (SharedInventories == null) OnSaveGameLoading();
            InventoryGeneric result;
            SharedInventories.TryGetValue(key, out result);
            return result;
        }

        /// <summary>
        /// Gets or creates a shared inventory.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public InventoryGeneric GetOrCreateSharedInventory(string key)
        {
            InventoryGeneric result = GetSharedInventory(key);
            if (result != null)
            {
                InventoryShared Shared = result as InventoryShared;
                if (Shared?.Count != Config.sharedChestSize) Shared.TryResize(Config.sharedChestSize);
                return result;
            }

            result = new InventoryShared(Config.sharedChestSize, "shared-" + key, api);
            SharedInventories.Add(key, result);
            return result;
        }

        /// <summary>
        /// Called when the suitability is requested.
        /// Used for personal containers.
        /// </summary>
        /// <param name="inv">The inv.</param>
        /// <param name="sourceSlot">The source slot.</param>
        /// <param name="targetSlot">The target slot.</param>
        /// <param name="isMerge">if set to <c>true</c> [is merge].</param>
        /// <returns></returns>
        protected float OnGetSuitability(InventoryGeneric inv, ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            return (isMerge ? (inv.BaseWeight + 3) : (inv.BaseWeight + 1)) + (sourceSlot.Inventory is InventoryBasePlayer ? 1 : 0);
        }

        /// <summary>
        /// Initializes a personal container.
        /// </summary>
        /// <param name="inv">The inv.</param>
        protected void InitPersonalContainer(InventoryGeneric inv)
        {
            inv.OnGetSuitability = (sourceSlot, targetSlot, isMerge) => OnGetSuitability(inv, sourceSlot, targetSlot, isMerge);
            inv.BaseWeight = 1.0f;
        }

    }//!class AdvancedChestsSystem
}//!namespace AdvancedChests
