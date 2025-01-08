using System.Collections.Generic;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    public class BlockEntityPersonalContainer : BlockEntity
    {
        /// <summary>
        /// The type
        /// </summary>
        public string type = "personal";

        /// <summary>
        /// The default type
        /// </summary>
        public string defaultType;

        /// <summary>
        /// The column quantity 
        /// </summary>
        public int quantityColumns = 4;

        /// <summary>
        /// The dialog title language code
        /// </summary>
        public string dialogTitleLangCode = "chestcontents";

        /// <summary>
        /// The inventory dialog
        /// </summary>
        protected GuiDialogBlockEntity invDialog;

        /// <summary>
        /// The inventory
        /// For servers this should be null
        /// </summary>
        protected InventoryGeneric inv;

        /// <summary>
        /// The own mesh
        /// </summary>
        protected MeshData ownMesh;

        /// <summary>
        /// The collision selection boxes
        /// </summary>
        public Cuboidf[] collisionSelectionBoxes;

        /// <summary>
        /// The renderer rotation
        /// </summary>
        Vec3f rendererRot = new Vec3f();

        /// <summary>
        /// The mesh angle
        /// </summary>
        float meshangle;

        /// <summary>
        /// Gets the dialog title.
        /// </summary>
        /// <value>
        /// The dialog title.
        /// </value>
        public virtual string DialogTitle
        {
            get { return Lang.Get(dialogTitleLangCode); }
        }

        /// <summary>
        /// Gets or sets the mesh angle.
        /// </summary>
        /// <value>
        /// The mesh angle.
        /// </value>
        public virtual float MeshAngle
        {
            get { return meshangle; }
            set
            {
                meshangle = value;
                rendererRot.Y = value * GameMath.RAD2DEG;
            }
        }

        /// <summary>
        /// Gets or sets the open sound.
        /// </summary>
        /// <value>
        /// The open sound.
        /// </value>
        public virtual AssetLocation OpenSound { get; set; } = new AssetLocation("sounds/block/chestopen");

        /// <summary>
        /// Gets or sets the close sound.
        /// </summary>
        /// <value>
        /// The close sound.
        /// </value>
        public virtual AssetLocation CloseSound { get; set; } = new AssetLocation("sounds/block/chestclose");

        /// <summary>
        /// Gets the animation utility.
        /// </summary>
        /// <value>
        /// The animation utility.
        /// </value>
        BlockEntityAnimationUtil AnimUtil
        {
            get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        }

        /// <summary>
        /// This method is called right after the block entity was spawned or right after it was loaded from a newly loaded chunk. You do have access to the world and its blocks at this point.
        /// However if this block entity already existed then FromTreeAttributes is called first!
        /// You should still call the base method to sets the this.api field
        /// </summary>
        /// <param name="api"></param>
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            Init();

            inv = null;
            ICoreClientAPI capi = api as ICoreClientAPI;
            if (capi == null) return;

            AdvancedChestsSystem system = api.ModLoader.GetModSystem<AdvancedChestsSystem>();
            inv = system?.GetOrCreatePersonalInventory(capi.World.Player);
            if (inv != null)
            {
                inv.OnInventoryClosed += OnInventoryClosed;
                inv.OnInventoryOpened += OnInventoryOpened;
            }
        }

        /// <summary>
        /// Initializes this instance.
        /// </summary>
        public virtual void Init()
        {
            if (Block?.Attributes != null)
            {
                collisionSelectionBoxes = Block.Attributes["collisionSelectionBoxes"]?[type]?.AsObject<Cuboidf[]>();
                dialogTitleLangCode = Block.Attributes["dialogTitleLangCode"][type].AsString(dialogTitleLangCode);
                quantityColumns = Block.Attributes["quantityColumns"][type].AsInt(4);
                defaultType = Block.Attributes["defaultType"]?.AsString("personal") ?? "personal";

                if (Block.Attributes["typedOpenSound"][type].Exists)
                {
                    OpenSound = AssetLocation.Create(Block.Attributes["typedOpenSound"][type].AsString(OpenSound.ToShortString()), Block.Code.Domain);
                }
                if (Block.Attributes["typedCloseSound"][type].Exists)
                {
                    CloseSound = AssetLocation.Create(Block.Attributes["typedCloseSound"][type].AsString(CloseSound.ToShortString()), Block.Code.Domain);
                }
            }
        }

        /// <summary>
        /// Called when a player right clicks on the chest.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="byPlayer">The by player.</param>
        public virtual void OnPlayerRightClick(IWorldAccessor world, IPlayer byPlayer)
        {
            ICoreServerAPI sapi = world?.Api as ICoreServerAPI;

            if (sapi != null && byPlayer != null)
            {
                AdvancedChestsSystem system = sapi.ModLoader.GetModSystem<AdvancedChestsSystem>();
                InventoryGeneric inventory = system?.GetOrCreatePersonalInventory(byPlayer);
                if (inventory == null) return;
                //quantityColumns = GameMath.Clamp((int)GameMath.Sqrt(inventory.Count), 1, 12);

                byte[] data;

                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryWriter writer = new BinaryWriter(ms);
                    writer.Write("BlockEntityInventory");
                    writer.Write(DialogTitle);
                    writer.Write((byte)quantityColumns);
                    TreeAttribute tree = new TreeAttribute();
                    inventory.ToTreeAttributes(tree);
                    tree.ToBytes(writer);
                    data = ms.ToArray();
                }

                sapi.Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos, (int)EnumBlockContainerPacketId.OpenInventory,
                    data
                );

                byPlayer.InventoryManager.OpenInventory(inventory);
            }
        }

        /// <summary>
        /// Called when loading the world or when receiving block entity from the server. When overriding, make sure to still call the base method.
        /// FromTreeAttributes is always called before Initialize() is called, so the this.api field is not yet set!
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="worldAccessForResolve">Use this api if you need to resolve blocks/items. Not suggested for other purposes, as the residing chunk may not be loaded at this point</param>
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            MeshAngle = tree.GetFloat("meshAngle", MeshAngle);
        }

        /// <summary>
        /// Converts to treeattributes.
        /// </summary>
        /// <param name="tree"></param>
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetFloat("meshAngle", MeshAngle);
        }

        /// <summary>
        /// Called when the block entity just got placed, not called when it was previously placed and the chunk is loaded. Always called after Initialize()
        /// </summary>
        /// <param name="byItemStack"></param>
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            if (byItemStack?.Attributes != null)
            {
                type = byItemStack.Attributes.GetString("type", defaultType);
            }
            base.OnBlockPlaced(byItemStack);
        }

        /// <summary>
        /// Called whenever a blockentity packet at the blocks position has been received from the server
        /// </summary>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            ICoreClientAPI capi = Api as ICoreClientAPI;
            if (capi == null) return;

            if (packetid == (int)EnumBlockContainerPacketId.OpenInventory)
            {
                if (invDialog != null)
                {
                    if (invDialog?.IsOpened() == true) invDialog.TryClose();
                    invDialog?.Dispose();
                    invDialog = null;
                    return;
                }

                string dialogClassName;
                string dialogTitle;
                int cols;
                TreeAttribute tree = new TreeAttribute();

                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryReader reader = new BinaryReader(ms);
                    dialogClassName = reader.ReadString();
                    dialogTitle = reader.ReadString();
                    cols = reader.ReadByte();
                    tree.FromBytes(reader);
                }

                AdvancedChestsSystem system = capi.ModLoader.GetModSystem<AdvancedChestsSystem>();
                InventoryGeneric inventory = system?.GetOrCreatePersonalInventory(capi.World.Player);
                if (inventory == null) return;

                inventory.FromTreeAttributes(tree);
                inventory.ResolveBlocksOrItems();

                invDialog = new GuiDialogBlockEntityInventory(dialogTitle, inventory, Pos, cols, Api as ICoreClientAPI);

                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                string os = block.Attributes?["openSound"]?.AsString();
                string cs = block.Attributes?["closeSound"]?.AsString();
                AssetLocation opensound = os == null ? null : AssetLocation.Create(os, block.Code.Domain);
                AssetLocation closesound = cs == null ? null : AssetLocation.Create(cs, block.Code.Domain);

                invDialog.OpenSound = opensound ?? this.OpenSound;
                invDialog.CloseSound = closesound ?? this.CloseSound;

                invDialog.TryOpen();
            }
            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                AdvancedChestsSystem system = capi.ModLoader.GetModSystem<AdvancedChestsSystem>();
                InventoryGeneric inventory = system.GetOrCreatePersonalInventory(capi.World.Player);

                capi.World.Player.InventoryManager.CloseInventory(inventory);
                if (invDialog?.IsOpened() == true) invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }

        /// <summary>
        /// Called whenever a blockentity packet at the blocks position has been received from the client
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="packetid"></param>
        /// <param name="data"></param>
        public override void OnReceivedClientPacket(IPlayer fromPlayer, int packetid, byte[] data)
        {
            AdvancedChestsSystem system = Api.ModLoader.GetModSystem<AdvancedChestsSystem>();
            InventoryGeneric inventory = system?.GetOrCreatePersonalInventory(fromPlayer);
            if (inventory == null) return;

            if (packetid < 1000)
            {
                inventory.InvNetworkUtil.HandleClientPacket(fromPlayer, packetid, data);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(new BlockPos(Pos.X, Pos.Y, Pos.Z, Pos.dimension)).MarkModified();
                return;
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                fromPlayer.InventoryManager?.CloseInventory(inventory);
            }
            if (packetid == (int)EnumBlockEntityPacketId.Open)
            {
                fromPlayer.InventoryManager?.OpenInventory(inventory);
            }
        }

        /// <summary>
        /// Called when the inventory was opened.
        /// </summary>
        /// <param name="player">The player.</param>
        public virtual void OnInventoryOpened(IPlayer player)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                AnimUtil?.StartAnimation(new AnimationMetaData()
                {
                    Animation = "lidopen",
                    Code = "lidopen",
                    AnimationSpeed = 1.8f,
                    EaseOutSpeed = 6,
                    EaseInSpeed = 15
                });
            }
        }

        /// <summary>
        /// Called when the inventory was closed.
        /// </summary>
        /// <param name="player">The player.</param>
        public virtual void OnInventoryClosed(IPlayer player)
        {
            AnimUtil?.StopAnimation("lidopen");

            GuiDialogBlockEntity temp = invDialog;
            invDialog = null;
            if (temp == null) return;
            if (temp.IsOpened()) temp.TryClose();
            temp.Dispose();
        }

        /// <summary>
        /// Generates the mesh.
        /// </summary>
        /// <param name="tesselator">The tesselator.</param>
        /// <returns></returns>
        private MeshData GenMesh(ITesselatorAPI tesselator)
        {
            BlockGenericTypedContainer block = Block as BlockGenericTypedContainer;
            if (Block == null)
            {
                block = Api.World.BlockAccessor.GetBlock(Pos) as BlockGenericTypedContainer;
                Block = block;
            }
            if (block == null) return null;
            int rndTexNum = Block.Attributes?["rndTexNum"][type]?.AsInt(0) ?? 0;

            string key = "typedContainerMeshes" + Block.Code.ToShortString();
            Dictionary<string, MeshData> meshes = ObjectCacheUtil.GetOrCreate(Api, key, () =>
            {
                return new Dictionary<string, MeshData>();
            });
            MeshData mesh;

            string shapename = Block.Attributes?["shape"][type].AsString();
            if (shapename == null)
            {
                return null;
            }

            Shape shape = null;
            if (AnimUtil != null)
            {
                string skeydict = "typedContainerShapes";
                Dictionary<string, Shape> shapes = ObjectCacheUtil.GetOrCreate(Api, skeydict, () =>
                {
                    return new Dictionary<string, Shape>();
                });
                string skey = Block.FirstCodePart() + block.Subtype + "-" + "-" + shapename + "-" + rndTexNum;
                if (!shapes.TryGetValue(skey, out shape))
                {
                    shapes[skey] = shape = block.GetShape(Api as ICoreClientAPI, shapename);
                }
            }

            string meshKey = type + block.Subtype + "-" + rndTexNum;
            if (meshes.TryGetValue(meshKey, out mesh))
            {
                if (AnimUtil != null && AnimUtil.renderer == null)
                {
                    AnimUtil.InitializeAnimator(type + "-" + key, mesh, shape, rendererRot);
                }

                return mesh;
            }


            if (rndTexNum > 0) rndTexNum = GameMath.MurmurHash3Mod(Pos.X, Pos.Y, Pos.Z, rndTexNum);

            if (AnimUtil != null)
            {
                if (AnimUtil.renderer == null)
                {
                    var texSource = new GenericContainerTextureSource()
                    {
                        blockTextureSource = tesselator.GetTextureSource(Block, rndTexNum),
                        curType = type
                    };

                    mesh = AnimUtil.InitializeAnimator(type + "-" + key + block.Subtype, shape, texSource, rendererRot);
                }

                return meshes[meshKey] = mesh;
            }
            else
            {
                mesh = block.GenMesh(Api as ICoreClientAPI, type, shapename, tesselator, new Vec3f(), rndTexNum);

                return meshes[meshKey] = mesh;
            }
        }

        /// <summary>
        /// Called on the tesselation stage.
        /// </summary>
        /// <param name="mesher">The mesher.</param>
        /// <param name="tesselator">The tesselator.</param>
        /// <returns></returns>
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
        {
            bool skipmesh = base.OnTesselation(mesher, tesselator);

            if (!skipmesh)
            {
                if (ownMesh == null)
                {
                    ownMesh = GenMesh(tesselator);
                    if (ownMesh == null) return false;
                }

                mesher.AddMeshData(ownMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, MeshAngle, 0));
            }

            return true;
        }

    }//!class BlockEntityPersonalContainer
}//!namespace AdvancedChests
