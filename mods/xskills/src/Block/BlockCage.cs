using System.Collections.Generic;
using System.IO;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// Contains the texture ids for an entity
    /// </summary>
    public class CapturedEntityTextures
    {
        public Dictionary<int, int> TextureIds = new Dictionary<int, int>();
    }//!class CapturedEntityTextures

    /// <summary>
    /// A cage that can capture animals.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.Block" />
    public class BlockCage : Block
    {
        /// <summary>
        /// The husbandry skill
        /// </summary>
        Husbandry husbandry;

        /// <summary>
        /// Called when this block was loaded by the server or the client
        /// </summary>
        /// <param name="api"></param>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            husbandry = XLeveling.Instance(api)?.GetSkill("husbandry") as Husbandry;
        }

        /// <summary>
        /// Determines whether the specified item stack contains a cage with an animal inside.
        /// </summary>
        /// <param name="itemStack">The item stack.</param>
        /// <returns>
        ///   <c>true</c> if the specified item stack is empty; <c>false</c> if the specified item stack contains a cage with an animal inside.
        /// </returns>
        public bool Empty(ItemStack itemStack)
        {
            if (itemStack == null) return false;
            return !itemStack.Attributes.HasAttribute("entityName");
        }

        /// <summary>
        /// Returns the resolved name of the entity.
        /// </summary>
        /// <param name="itemStack">The item stack.</param>
        /// <returns>the resolved name of the entity</returns>
        public string ResolvedEntityName(ItemStack itemStack)
        {
            string name = itemStack.Attributes.GetString("entityName");
            string domain = itemStack.Attributes.GetString("entityShape")?.Split(":")?[0] ?? "game";
            if (name == null) return null;
            else return Lang.Get(domain + ":item-creature-" + name);
        }

        /// <summary>
        /// Determines whether the specified entity is catchable by a specified entity.
        /// </summary>
        /// <param name="byEntity">The entity that tries to capture the entity. Usually a player.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the specified entity is catchable; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsCatchable(EntityAgent byEntity, Entity entity)
        {
            if (IsCatchable(entity))
            {
                PlayerAbility playerAbility = byEntity?.GetBehavior<PlayerSkillSet>()?[husbandry.Id]?[husbandry.CatcherId];
                return playerAbility != null ? playerAbility.Tier > 0 : false;
            }
            return false;
        }

        /// <summary>
        /// Determines whether the specified entity is catchable.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns>
        ///   <c>true</c> if the specified entity is catchable; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsCatchable(Entity entity)
        {
            XSkillsAnimalBehavior animalBehavior = entity.GetBehavior<XSkillsAnimalBehavior>();
            return animalBehavior != null ? animalBehavior.Catchable : false;
        }

        /// <summary>
        /// Called when the player right clicks while holding this block/item in his hands
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent">True when the player pressed the right mouse button on this block. Every subsequent call, while the player holds right mouse down will be false, it gets called every second while right mouse is down</param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!firstEvent || byEntity != null ? byEntity.Controls.Sneak : true) return;

            IPlayer player = (byEntity as EntityPlayer)?.Player;
            BlockPos pos = blockSel?.Position ?? entitySel?.Position?.AsBlockPos;
            if (player != null && pos != null && !byEntity.World.Claims.TryAccess(player, pos, EnumBlockAccessFlags.BuildOrBreak))
            {
                return;
            }

            if (Empty(slot.Itemstack)) Capture(slot, byEntity, entitySel?.Entity, ref handling);
            else Release(slot, byEntity, blockSel, ref handling);
        }

        /// <summary>
        /// Captures the specified entity.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="byEntity">The entity that captures the entity. Usually a player.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="handling">The handling.</param>
        public void Capture(ItemSlot slot, EntityAgent byEntity, Entity entity, ref EnumHandHandling handling)
        {
            if (entity == null || husbandry == null) return;
            if (!entity.Alive) return;
            handling = EnumHandHandling.PreventDefault;

            //simulates an attack to attract attention
            EntityBehaviorEmotionStates emotionStates = entity.GetBehavior<EntityBehaviorEmotionStates>();
            if (emotionStates != null)
            {
                DamageSource damageSource = new DamageSource();
                damageSource.Source = EnumDamageSource.Entity;
                damageSource.SourceEntity = byEntity;
                damageSource.Type = EnumDamageType.BluntAttack;
                float damage = 1.0f;
                emotionStates.OnEntityReceiveDamage(damageSource, ref damage);
            }

            if (!IsCatchable(byEntity, entity)) return;

            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            entity.ToBytes(bw, false);
            bw.Flush();
            byte[] data = ms.ToArray();

            AssetLocation shape = entity.Properties.Client.Shape.Base.Clone();
            shape.WithPathPrefix("shapes/").WithPathPrefix(entity.Properties.Client.Shape.Base.Domain + ":").WithPathAppendix(".json");
            slot.Itemstack.Attributes.SetString("entityName", entity.Code.GetName());
            slot.Itemstack.Attributes.SetString("entityClass", entity.Class);
            slot.Itemstack.Attributes.SetBytes("entityData", data);
            slot.Itemstack.Attributes.SetString("entityShape", shape.Path);
            slot.Itemstack.Attributes.SetInt("entityTextureID", entity.WatchedAttributes.GetInt("textureIndex"));

            if (api.Side == EnumAppSide.Server) entity.Die(EnumDespawnReason.PickedUp);
        }

        /// <summary>
        /// Releases the entity.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="byEntity">The by entity.</param>
        /// <param name="blockSel">The block sel.</param>
        /// <param name="handling">The handling.</param>
        public void Release(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, ref EnumHandHandling handling)
        {
            if (blockSel == null) return;
            handling = EnumHandHandling.PreventDefault;

            if (api.Side == EnumAppSide.Server)
            {
                string entityClass = slot.Itemstack.Attributes.GetString("entityClass");
                byte[] entityData = slot.Itemstack.Attributes.GetBytes("entityData"); ;

                //old data conversion; can be removed in later versions
                if (entityData == null)
                {
                    string oldData = slot.Itemstack.Attributes.GetString("entityData");
                    if (oldData != null) entityData = Ascii85.Decode(oldData);
                }

                slot.Itemstack.Attributes.RemoveAttribute("entityName");
                slot.Itemstack.Attributes.RemoveAttribute("entityClass");
                slot.Itemstack.Attributes.RemoveAttribute("entityData");
                slot.Itemstack.Attributes.RemoveAttribute("entityShape");
                slot.Itemstack.Attributes.RemoveAttribute("entityTextureID");
                slot.MarkDirty();

                if (entityClass == null || entityData == null) return;
                Entity entity = api.World.ClassRegistry.CreateEntity(entityClass);
                if (entity == null) return;

                MemoryStream ms = new MemoryStream(entityData);
                BinaryReader br = new BinaryReader(ms);

                entity.FromBytes(br, false);
                Vec3d pos = new Vec3d(blockSel.Position.X + 0.5f, blockSel.Position.Y + 0.5f, blockSel.Position.Z + 0.5f);

                switch (blockSel.Face.Index)
                {
                    case BlockFacing.indexNORTH:
                        pos = pos.Add(0.0f, 0.0f, -1.0f);
                        break;
                    case BlockFacing.indexEAST:
                        pos = pos.Add(1.0f, 0.0f, 0.0f);
                        break;
                    case BlockFacing.indexSOUTH:
                        pos = pos.Add(0.0f, 0.0f, 1.0f);
                        break;
                    case BlockFacing.indexWEST:
                        pos = pos.Add(-1.0f, 0.0f, 0.0f);
                        break;
                    case BlockFacing.indexUP:
                        pos = pos.Add(0.0f, 1.0f, 0.0f);
                        break;
                    case BlockFacing.indexDOWN:
                        pos = pos.Add(0.0f, -1.0f, 0.0f);
                        break;
                    default:
                        pos = pos.Add(0.0f, 1.0f, 0.0f);
                        break;
                }

                entity.Pos.SetPos(pos);
                entity.ServerPos.SetPos(pos);
                entity.PositionBeforeFalling.Set(pos);
                api.World.SpawnEntity(entity);
            }
        }

        /// <summary>
        /// When a player does a right click while targeting this placed block. Should return true if the event is handled, so that other events can occur, e.g. eating a held item if the block is not interactable with.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="byPlayer"></param>
        /// <param name="blockSel"></param>
        /// <returns>
        /// False if the interaction should be stopped. True if the interaction should continue. If you return false, the interaction will not be synced to the server.
        /// </returns>
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityCage be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityCage;
            if (be == null) return false;

            if (!world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                return false;
            }

            ItemStack stack = null;
            CollectibleObject collectible = world.BlockAccessor.GetBlock(CodeWithVariant("side", "south"));

            if (collectible != null)
                stack = new ItemStack(collectible, 1);
            else if (Drops.Length > 0 && Drops[0].ResolvedItemstack.Collectible is BlockCage) 
                stack = new ItemStack(Drops[0].ResolvedItemstack.Collectible, 1);
            else 
                stack = new ItemStack(this);

            if (be.EntityClass != null && be.EntityData != null && be.EntityName != null)
            {
                stack.Attributes.SetString("entityName", be.EntityName);
                stack.Attributes.SetString("entityClass", be.EntityClass);
                stack.Attributes.SetBytes("entityData", be.EntityData);
                if (be.EntityShape != null) stack.Attributes.SetString("entityShape", be.EntityShape);
                if (be.EntityTextureID != -1) stack.Attributes.SetInt("entityTextureID", be.EntityTextureID);
            }

            if (byPlayer.InventoryManager.TryGiveItemstack(stack))
            {
                world.BlockAccessor.SetBlock(0, blockSel.Position);
                world.PlaySoundAt(new AssetLocation("sounds/block/planks"), blockSel.Position.X + 0.5, blockSel.Position.Y, blockSel.Position.Z + 0.5, byPlayer, false);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the item stack name that is getting displayed.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string name = ResolvedEntityName(itemStack);
            if (name != null) return base.GetHeldItemName(itemStack) + " (" + name + ")";
            return base.GetHeldItemName(itemStack);
        }

        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the text that is getting displayed.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="dsc"></param>
        /// <param name="world"></param>
        /// <param name="withDebugInfo"></param>
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            string name = ResolvedEntityName(inSlot.Itemstack);
            if (name != null) dsc.AppendLine(name);
        }

        /// <summary>
        /// Called by the block info HUD for displaying the blocks name
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        //public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        //{
        //    BlockEntityCage be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCage;
        //    if (be?.EntityName == null) return base.GetPlacedBlockName(world, pos);

        //    string name = Lang.Get("item-creature-" + be.EntityName);
        //    return base.GetPlacedBlockName(world, pos) + " (" + name + ")";
        //}

        /// <summary>
        /// Called by the block info HUD for display the interaction help besides the crosshair
        /// </summary>
        /// <param name="world"></param>
        /// <param name="selection"></param>
        /// <param name="forPlayer"></param>
        /// <returns></returns>
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = "xskills:blockhelp-cage-pickup"
                }
            }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }

        /// <summary>
        /// Interaction help thats displayed above the hotbar, when the player puts this item/block in his active hand slot
        /// </summary>
        /// <param name="inSlot"></param>
        /// <returns></returns>
        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            string entityName = ResolvedEntityName(inSlot.Itemstack);
            if (entityName == null) return new WorldInteraction[]
{
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = "xskills:blockhelp-cage-catch"

                },
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    ActionLangCode = "xskills:blockhelp-cage-place",
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));

            return new WorldInteraction[]
            {
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    ActionLangCode = Lang.Get("xskills:blockhelp-cage-release", entityName)

                },
                new WorldInteraction()
                {
                    MouseButton = EnumMouseButton.Right,
                    HotKeyCode = "sneak",
                    ActionLangCode = "xskills:blockhelp-cage-place",
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }

        /// <summary>
        /// When the player has presed the middle mouse click on the block.
        /// Copies the captured animal.
        /// </summary>
        /// <param name="world"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack itemStack = base.OnPickBlock(world, pos);
            BlockEntityCage be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCage;
            if (be?.EntityName == null) return itemStack;

            itemStack.Attributes.SetString("entityName", be.EntityName);
            itemStack.Attributes.SetString("entityClass", be.EntityClass);
            itemStack.Attributes.SetBytes("entityData", be.EntityData);
            itemStack.Attributes.SetString("entityShape", be.EntityShape);
            itemStack.Attributes.SetInt("entityTextureID", be.EntityTextureID);

            return itemStack;
        }

        #region render stuff
        /// <summary>
        /// The entitiy texture ids
        /// </summary>
        public static Dictionary<string, CapturedEntityTextures> EntitiyTextureIds = new Dictionary<string, CapturedEntityTextures>();
        /// <summary>
        /// The mesh refs
        /// </summary>
        public static Dictionary<string, MultiTextureMeshRef> MeshRefs = new Dictionary<string, MultiTextureMeshRef>();

        /// <summary>
        /// The scalings
        /// </summary>
        public static Dictionary<string, float> Scalings = new Dictionary<string, float>();

        /// <summary>
        /// This method is called before rendering the item stack into GUI, first person hand, third person hand and/or on the ground
        /// The renderinfo object is pre-filled with default values.
        /// </summary>
        /// <param name="capi"></param>
        /// <param name="itemstack"></param>
        /// <param name="target"></param>
        /// <param name="renderinfo"></param>
        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            string entityName = itemstack.Attributes.GetString("entityName");
            string entityShape = itemstack.Attributes.GetString("entityShape");
            int entityTextureID = itemstack.Attributes.GetInt("entityTextureID", -1);

            if (entityName == null || entityTextureID == -1 || entityShape == null)
            {
                base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
                return;
            }
            string key = entityName + "-" + entityTextureID;

            if (!MeshRefs.TryGetValue(key, out MultiTextureMeshRef value))
            {
                capi.Tesselator.TesselateBlock(this, out MeshData cageMeshData);
                CagedEntityMeshData meshData = new CagedEntityMeshData(capi, entityName, entityTextureID, entityShape);
                meshData.Generate();
                if (meshData.MeshData != null)
                {
                    cageMeshData.AddMeshData(meshData.MeshData);
                    MeshRefs[key] = capi.Render.UploadMultiTextureMesh(cageMeshData);
                    renderinfo.ModelRef = MeshRefs[key];
                }
            }
            else renderinfo.ModelRef = value;

        }

        /// <summary>
        /// Called by the texture atlas manager when building up the block atlas. Has to add all of the blocks texture
        /// </summary>
        /// <param name="api"></param>
        /// <param name="textureDict"></param>
        public override void OnCollectTextures(ICoreAPI api, ITextureLocationDictionary textureDict)
        {
            base.OnCollectTextures(api, textureDict);
            lock (this)
            {
                foreach (EntityProperties entity in api.World.EntityTypes)
                {
                    //check whether the entity is catchable
                    //don't need the textures otherwise
                    bool isCatchable = false;
                    JsonObject[] behaviors = entity.Client.BehaviorsAsJsonObj;
                    foreach (JsonObject behavior in behaviors)
                    {
                        string code = behavior["code"].AsString();
                        if (code == "XSkillsAnimal")
                        {
                            bool catchable = behavior["catchable"]?.AsBool() ?? false;
                            if (catchable)
                            {
                                Scalings[entity.Code.Path] = behavior["scale"].AsFloat(1.0f);
                                isCatchable = true;
                                break;
                            }
                        }
                    }
                    if (!isCatchable) continue;

                    CapturedEntityTextures entityTextures = new CapturedEntityTextures();
                    if (entity.Client.FirstTexture != null)
                    {
                        CollectTexture(api, textureDict, entityTextures, entity.Client.FirstTexture, entity.Code);
                        if (entity.Client.FirstTexture.Alternates != null)
                        {
                            foreach (CompositeTexture texture in entity.Client.FirstTexture.Alternates)
                            {
                                CollectTexture(api, textureDict, entityTextures, texture, entity.Code);
                            }
                        }
                    }
                    EntitiyTextureIds[entity.Code.GetName()] = entityTextures;
                }
            }
        }

        /// <summary>
        /// Collects the texture.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="textureDict">The texture dictionary.</param>
        /// <param name="entityTextures">The entity textures.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="code">The code.</param>
        private void CollectTexture(ICoreAPI api, ITextureLocationDictionary textureDict, CapturedEntityTextures entityTextures, CompositeTexture texture, AssetLocation code)
        {
            texture.Bake(api.Assets);
            textureDict.AddTextureLocation(new AssetLocationAndSource(texture.Baked.BakedName, "Entity code ", code));
            entityTextures.TextureIds[entityTextures.TextureIds.Count] = textureDict[new AssetLocationAndSource(texture.Baked.BakedName)];
        }

        #endregion
    }//!public class BlockCage
}//!namespace XSkills
