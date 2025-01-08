using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// A terminal that allows you to access a special player specific inventory.
    /// Is basically a copy of <seealso cref="Vintagestory.GameContent.BlockGenericTypedContainer" />.
    /// It just exchanges the entity types.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockGenericTypedContainer" />
    public class BlockPersonalContainer : BlockGenericTypedContainer
    {
        /// <summary>
        /// The default type
        /// </summary>
        string defaultType;

        /// <summary>
        /// Called when the block was loaded.
        /// </summary>
        /// <param name="api">The API.</param>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            defaultType = Attributes["defaultType"].AsString("normal-generic");
        }

        /// <summary>
        /// Gets the collision boxes.
        /// </summary>
        /// <param name="blockAccessor">The block accessor.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntityPersonalContainer be = blockAccessor.GetBlockEntity(pos) as BlockEntityPersonalContainer;
            if (be?.collisionSelectionBoxes != null) return be.collisionSelectionBoxes;

            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        /// <summary>
        /// Gets the selection boxes.
        /// </summary>
        /// <param name="blockAccessor">The block accessor.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            BlockEntityPersonalContainer be = blockAccessor.GetBlockEntity(pos) as BlockEntityPersonalContainer;
            if (be?.collisionSelectionBoxes != null) return be.collisionSelectionBoxes;

            return base.GetSelectionBoxes(blockAccessor, pos);
        }

        /// <summary>
        /// Places the block.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="byPlayer">The by player.</param>
        /// <param name="blockSel">The block sel.</param>
        /// <param name="byItemStack">The by item stack.</param>
        /// <returns></returns>
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            bool val = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (val)
            {
                BlockEntityPersonalContainer be = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityPersonalContainer;
                if (be != null)
                {
                    BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
                    double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
                    double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
                    float angleHor = (float)Math.Atan2(dx, dz);


                    string type = be.type;
                    string rotatatableInterval = Attributes?["rotatatableInterval"][type]?.AsString("22.5deg") ?? "22.5deg";

                    if (rotatatableInterval == "22.5degnot45deg")
                    {
                        float rounded90degRad = ((int)Math.Round(angleHor / GameMath.PIHALF)) * GameMath.PIHALF;
                        float deg45rad = GameMath.PIHALF / 4;


                        if (Math.Abs(angleHor - rounded90degRad) >= deg45rad)
                        {
                            be.MeshAngle = rounded90degRad + 22.5f * GameMath.DEG2RAD * Math.Sign(angleHor - rounded90degRad);
                        }
                        else
                        {
                            be.MeshAngle = rounded90degRad;
                        }
                    }
                    if (rotatatableInterval == "22.5deg")
                    {
                        float deg22dot5rad = GameMath.PIHALF / 4;
                        float roundRad = ((int)Math.Round(angleHor / deg22dot5rad)) * deg22dot5rad;
                        be.MeshAngle = roundRad;
                    }
                }
            }

            return val;
        }

        /// <summary>
        /// Gets the decal.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        /// <param name="decalTexSource">The decal tex source.</param>
        /// <param name="decalModelData">The decal model data.</param>
        /// <param name="blockModelData">The block model data.</param>
        public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
        {
            BlockEntityPersonalContainer be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPersonalContainer;
            if (be != null)
            {
                ICoreClientAPI capi = api as ICoreClientAPI;
                string shapename = Attributes["shape"][be.type].AsString();
                if (shapename == null)
                {
                    base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
                    return;
                }

                blockModelData = GenMesh(capi, be.type, shapename);

                AssetLocation shapeloc = new AssetLocation(shapename).WithPathPrefixOnce("shapes/");
                Shape shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + ".json");
                if (shape == null)
                {
                    shape = Vintagestory.API.Common.Shape.TryGet(capi, shapeloc + "1.json");
                }

                MeshData md;
                capi.Tesselator.TesselateShape("typedcontainer-decal", shape, out md, decalTexSource);
                decalModelData = md;

                decalModelData.Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0, be.MeshAngle, 0);

                return;
            }

            base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
        }

        /// <summary>
        /// Called when the block was picked.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
        {
            ItemStack stack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));

            BlockEntityPersonalContainer be = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityPersonalContainer;
            if (be != null)
            {
                stack.Attributes.SetString("type", be.type);
            }
            else
            {
                stack.Attributes.SetString("type", defaultType);
            }

            return stack;
        }

        /// <summary>
        /// Gets the random color.
        /// </summary>
        /// <param name="capi">The capi.</param>
        /// <param name="pos">The position.</param>
        /// <param name="facing">The facing.</param>
        /// <param name="rndIndex">The random index.</param>
        /// <returns></returns>
        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            BlockEntityPersonalContainer be = capi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityPersonalContainer;
            if (be != null)
            {
                CompositeTexture tex = null;
                if (!Textures.TryGetValue(be.type + "-lid", out tex))
                {
                    Textures.TryGetValue(be.type + "-top", out tex);
                }
                return capi.BlockTextureAtlas.GetRandomColor(tex?.Baked == null ? 0 : tex.Baked.TextureSubId, rndIndex);
            }

            return base.GetRandomColor(capi, pos, facing, rndIndex);
        }
    }//!class BlockPersonalContainer
}//!namespace AdvancedChests
