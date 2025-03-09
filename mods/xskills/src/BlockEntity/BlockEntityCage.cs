using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace XSkills
{    public class BlockEntityCage : BlockEntity
    {
        /// <summary>
        /// Gets the name of the captured entity.
        /// </summary>
        /// <value>
        /// The name of the captured entity.
        /// </value>
        public string EntityName { get; internal set; }

        /// <summary>
        /// Gets the class of the captured entity.
        /// </summary>
        /// <value>
        /// The class of the captured entity.
        /// </value>
        public string EntityClass { get; internal set; }

        /// <summary>
        /// Gets the entity data.
        /// </summary>
        /// <value>
        /// The entity data.
        /// </value>
        public byte[] EntityData { get; internal set; }

        /// <summary>
        /// Gets the entity shape.
        /// </summary>
        /// <value>
        /// The entity shape.
        /// </value>
        public string EntityShape { get; internal set; }

        /// <summary>
        /// Gets the entity texture identifier.
        /// </summary>
        /// <value>
        /// The entity texture identifier.
        /// </value>
        public int EntityTextureID { get; internal set; }

        /// <summary>
        /// Gets or sets the mesh data.
        /// </summary>
        /// <value>
        /// The mesh data.
        /// </value>
        protected CagedEntityMeshData MeshData { get; set; }

        /// <summary>
        /// Gets the animation utility.
        /// </summary>
        /// <value>
        /// The animation utility.
        /// </value>
        //BlockEntityAnimationUtil AnimUtil
        //{
        //    get { return GetBehavior<BEBehaviorAnimatable>()?.animUtil; }
        //}

        /// <summary>
        /// The time past since the last update.
        /// </summary>
        //protected float dt;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlockEntityCage"/> class.
        /// </summary>
        public BlockEntityCage() : base()
        {
            EntityName = null;
            EntityClass = null;
            EntityData = null;
            EntityShape = null;
            EntityTextureID = -1;
        }

        /// <summary>
        /// This method is called right after the block entity was spawned or right after it was loaded from a newly loaded chunk. You do have access to the world and its blocks at this point.
        /// However if this block entity already existed then FromTreeAttributes is called first!
        /// You should still call the base method to sets the this.api field
        /// </summary>
        /// <param name="byPlayer"></param>
        //public virtual void OnTick(float dt)
        //{
        //    this.dt += dt;
        //    if (this.dt < 10000) return;
        //    this.dt = 0.0f;

        //    Shape shape = MeshData?.Shape;
        //    if (shape == null) return;
        //    Animation animation = shape.Animations[Api.World.Rand.Next(shape.Animations.Length - 1)];
        //    AnimUtil?.StartAnimation(new AnimationMetaData()
        //    {
        //        Animation = animation.Name,
        //        Code = animation.Code,
        //        AnimationSpeed = 1.0f,
        //        EaseOutSpeed = 6,
        //        EaseInSpeed = 15
        //    });
        //}

        /// <summary>
        /// Called when the block was broken in survival mode or through explosions and similar. Generally in situations where you probably want
        /// to drop the block entity contents, if it has any
        /// Releases the captured entity
        /// </summary>
        /// <param name="byPlayer"></param>
        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            if (Api.Side == EnumAppSide.Server)
            {
                if (EntityClass == null || EntityData == null) return;
                Entity entity = Api.World.ClassRegistry.CreateEntity(EntityClass);
                if (entity == null) return;

                MemoryStream ms = new MemoryStream(EntityData);
                BinaryReader br = new BinaryReader(ms);

                entity.FromBytes(br, false);

                entity.Pos.SetPos(Pos.Add(0.0f, 0.15f, 0.0f));
                entity.ServerPos.SetPos(Pos.Add(0.0f, 0.15f, 0.0f));
                entity.PositionBeforeFalling.Set(Pos.Add(0.0f, 0.15f, 0.0f));

                Api.World.SpawnEntity(entity);
            }
            base.OnBlockBroken(byPlayer);
        }

        /// <summary>
        /// Called when the block entity just got placed, not called when it was previously placed and the chunk is loaded. Always called after Initialize()
        /// </summary>
        /// <param name="byItemStack"></param>
        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);
            if (byItemStack == null)
            {
                EntityName = null;
                EntityClass = null;
                EntityData = null;
                EntityShape = null;
                EntityTextureID = -1;
            }
            else
            {
                EntityName = byItemStack.Attributes.GetString("entityName");
                EntityClass = byItemStack.Attributes.GetString("entityClass");
                EntityData = byItemStack.Attributes.GetBytes("entityData");
                EntityShape = byItemStack.Attributes.GetString("entityShape");
                EntityTextureID = byItemStack.Attributes.GetInt("entityTextureID", -1);
            }
        }

        /// <summary>
        /// Called when saving the world or when sending the block entity data to the client.
        /// When overriding, make sure to still call the base method.
        /// </summary>
        /// <param name="tree"></param>
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            if (EntityName != null && EntityClass != null && EntityData != null)
            {
                tree.SetString("entityName", EntityName);
                tree.SetString("entityClass", EntityClass);
                tree.SetBytes("entityData", EntityData);
                if (EntityShape != null) tree.SetString("entityShape", EntityShape);
                if (EntityTextureID != -1) tree.SetInt("entityTextureID", EntityTextureID);
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
            EntityName = tree.GetString("entityName");
            EntityClass = tree.GetString("entityClass");
            EntityData = tree.GetBytes("entityData");
            EntityShape = tree.GetString("entityShape");
            EntityTextureID = tree.GetInt("entityTextureID", -1);

            //old data conversion; can be removed in later versions
            if (EntityData == null)
            {
                string oldData = tree.GetString("entityData");
                if (oldData != null) EntityData = Ascii85.Decode(oldData);
            }
        }

        /// <summary>
        /// Let's you add your own meshes to a chunk. Don't reuse the meshdata instance anywhere in your code. Return true to skip the default mesh.
        /// WARNING!
        /// The Tesselator runs in a seperate thread, so you have to make sure the fields and methods you access inside this method are thread safe.
        /// </summary>
        /// <param name="mesher">The chunk mesh, add your stuff here</param>
        /// <param name="tessThreadTesselator">If you need to tesselate something, you should use this tesselator, since using the main thread tesselator can cause race conditions and crash the game</param>
        /// <returns>
        /// True to skip default mesh, false to also add the default mesh
        /// </returns>
        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            GenerateMeshData();
            if (MeshData.MeshData != null) mesher.AddMeshData(MeshData.MeshData);
            return false;
        }

        /// <summary>
        /// Generates the mesh data.
        /// </summary>
        protected virtual void GenerateMeshData()
        {
            MeshData = new CagedEntityMeshData(Api as ICoreClientAPI, EntityName, EntityTextureID, EntityShape);
            MeshData.Generate();
            string variant = Block.CodeEndWithoutParts(0);

            if (MeshData.MeshData != null)
            {
                float rotation = 0.0f;
                if (variant.Contains("north"))
                {
                    rotation = 90.0f;
                }
                if (variant.Contains("west"))
                {
                    rotation = 180.0f;
                }
                if (variant.Contains("south"))
                {
                    rotation = 270.0f;
                }

                //BlockEntityAnimationUtil animationUtil = AnimUtil;
                //if (animationUtil != null && animationUtil.renderer == null)
                //{
                //    string key = "cage-" + Pos.X.ToString() + "/" + Pos.Y.ToString() + "/" + Pos.Z.ToString();
                //    AnimUtil.InitializeAnimator(key, Mesh, MeshData.Shape, new Vec3f(0.0f, rotation, 0.0f));
                //}
                //else
                {
                    ModelTransform transform = new ModelTransform();
                    transform.EnsureDefaultValues();
                    transform.Rotation.Y = rotation;
                    MeshData.MeshData.ModelTransform(transform);
                }
            }
        }
    }//!class BECage

    /// <summary>
    /// Contains shape and mesh data for a captured entity.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Client.ITexPositionSource" />
    public class CagedEntityMeshData : ITexPositionSource
    {
        /// <summary>
        /// Gets or sets the shape.
        /// </summary>
        /// <value>
        /// The shape.
        /// </value>
        public Shape Shape { get; protected set; }

        /// <summary>
        /// Gets or sets the mesh data.
        /// </summary>
        /// <value>
        /// The mesh data.
        /// </value>
        public MeshData MeshData { get; protected set; }

        /// <summary>
        /// The capi
        /// </summary>
        protected ICoreClientAPI capi;

        /// <summary>
        /// The entity name
        /// </summary>
        protected string entityName;

        /// <summary>
        /// The entity texture identifier
        /// </summary>
        protected int entityTextureId;

        /// <summary>
        /// The entity shape
        /// </summary>
        protected string entityShape;

        /// <summary>
        /// Initializes a new instance of the <see cref="CagedEntityMeshData"/> class.
        /// </summary>
        /// <param name="capi">The capi.</param>
        /// <param name="entityName">Name of the entity.</param>
        /// <param name="entityTextureId">The entity texture identifier.</param>
        /// <param name="entityShape">The entity shape.</param>
        public CagedEntityMeshData(ICoreClientAPI capi, string entityName, int entityTextureId, string entityShape)
        {
            this.capi = capi;
            this.entityName = entityName;
            this.entityTextureId = entityTextureId;
            this.entityShape = entityShape;

            this.Shape = null;
            this.MeshData = null;
        }

        /// <summary>
        /// This returns the size of the atlas this texture resides in.
        /// </summary>
        public Size2i AtlasSize
        {
            get => capi?.BlockTextureAtlas.Size;
        }

        /// <summary>
        /// Gets the <see cref="TextureAtlasPosition"/> with the specified texture code.
        /// </summary>
        /// <value>
        /// The <see cref="TextureAtlasPosition"/>.
        /// </value>
        /// <param name="textureCode">The texture code.</param>
        /// <returns></returns>
        public TextureAtlasPosition this[string textureCode]
        {
            get
            {
                if (capi == null) return null;
                CapturedEntityTextures textures;
                BlockCage.EntitiyTextureIds.TryGetValue(entityName, out textures);
                if (textures == null) return null;
                int position = textures.TextureIds[entityTextureId];
                return capi.BlockTextureAtlas.Positions[position];
            }
        }

        /// <summary>
        /// Generates the mesh data.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns></returns>
        public MeshData Generate()
        {
            MeshData meshData = null;
            if (capi != null && entityShape != null)
            {
                Shape = capi.Assets.TryGet(new AssetLocation(entityShape))?.ToObject<Shape>();
                if (Shape == null) return null;
                capi.Tesselator.TesselateShapeWithJointIds("cage", Shape, out meshData, this, new Vec3f());

                float scale;
                if (!BlockCage.Scalings.TryGetValue(entityName, out scale))
                {
                    ModelTransform transform = ModelTransform.NoTransform;
                    transform.Scale = scale;
                    meshData.ModelTransform(transform);
                }
                meshData.Translate(0.0f, 0.0625f - (1.0f - scale) / 2.0f, 0.0f);
            }
            MeshData = meshData;
            return MeshData;
        }
    }//!class CagedEntityRenderer
}//!namespace XSkills
