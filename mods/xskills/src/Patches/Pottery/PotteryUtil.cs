using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// Contains a collection of methods for the pottery skill.
    /// </summary>
    public class PotteryUtil
    {
        /// <summary>
        /// Converts clay in a player inventory to usable voxels for clay form entities.
        /// </summary>
        /// <param name="cfe">The clay form entity.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="entity">The entity. Usually a player.</param>
        /// <returns></returns>
        public static bool AddClay(BlockEntityClayForm cfe, ItemSlot slot, EntityAgent entity)
        {
            if (cfe == null || slot == null) return false;

            if (cfe.AvailableVoxels <= 0)
            {
                if ((slot.Itemstack?.StackSize ?? 0) <= 0)
                {
                    return false;
                }

                slot.TakeOut(1);
                slot.MarkDirty();
                cfe.AvailableVoxels += 25;

                if (entity?.Api == null) return true;

                Pottery pottery = XLeveling.Instance(entity.Api).GetSkill("pottery") as Pottery;
                if (pottery == null) return true;
                PlayerSkill playerSkill = (entity.GetBehavior<PlayerSkillSet>())?[pottery.Id];
                if (playerSkill == null) return true;

                //thrift
                PlayerAbility playerAbility = playerSkill[pottery.ThriftId];
                if (playerAbility == null) return true;
                cfe.AvailableVoxels += playerAbility.Value(0);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Finishes the recipe.
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="entity">The entity.</param>
        public static void FinishRecipe(BlockEntityClayForm clayForm, ItemSlot slot, EntityAgent entity)
        {
            if (clayForm == null) return;
            int ymax = Math.Min(16, clayForm.SelectedRecipe.QuantityLayers);

            for (int y = 0; y < ymax; ++y)
            {
                for (int x = 0; x < 16; ++x)
                {
                    for (int z = 0; z < 16; ++z)
                    {
                        if (clayForm.SelectedRecipe.Voxels[x, y, z] != clayForm.Voxels[x, y, z])
                        {
                            if (clayForm.SelectedRecipe.Voxels[x, y, z])
                            {
                                if (clayForm.AvailableVoxels < 0)
                                {
                                    if (!AddClay(clayForm, slot, entity)) return;
                                }
                                clayForm.AvailableVoxels--;
                            }
                            else clayForm.AvailableVoxels++;
                        }
                        clayForm.Voxels[x, y, z] = clayForm.SelectedRecipe.Voxels[x, y, z];
                    }
                }
            }
        }

        /// <summary>
        /// Determines which portion is finished. 
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <returns></returns>
        public static float FinishedProportion(BlockEntityClayForm clayForm)
        {
            if (clayForm?.SelectedRecipe == null) return 0.0f;
            int finishedVoxel = 0;
            int neededVoxels = 0;
            int voxelCount = 0;

            int ymax = Math.Min(16, clayForm.SelectedRecipe.QuantityLayers);

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < ymax; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (clayForm.SelectedRecipe.Voxels[x, y, z])
                        {
                            neededVoxels++;
                            if (clayForm.Voxels[x, y, z])
                            {
                                voxelCount++;
                                finishedVoxel++;
                            }
                        }
                        else if (clayForm.Voxels[x, y, z])
                        {
                            voxelCount++;
                        }
                    }
                }
            }
            float temp1 = voxelCount < neededVoxels ? (float)voxelCount / neededVoxels : (float)neededVoxels / voxelCount;
            float temp2 = finishedVoxel / voxelCount;
            return temp1 * temp2;
        }

        /// <summary>
        /// Counts the voxels of a clay forming recipe.
        /// </summary>
        /// <param name="recipe">The recipe.</param>
        /// <returns></returns>
        public static int CountVoxels(ClayFormingRecipe recipe)
        {
            int voxelCount = 0;
            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < recipe.QuantityLayers; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        if (recipe.Voxels[x, y, z])
                            voxelCount++;
                    }
                }
            }
            return voxelCount;
        }

        /// <summary>
        /// Applies pottery abilities on the given stack.
        /// </summary>
        /// <param name="byPlayer">The player.</param>
        /// <param name="world">The world.</param>
        /// <param name="outputSlot">The output slot.</param>
        /// <returns></returns>
        public static bool ApplyOnStack(IPlayer byPlayer, IWorldAccessor world, ItemSlot outputSlot)
        {
            Pottery pottery = byPlayer?.Entity?.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("pottery") as Pottery;
            if (pottery == null || world == null) return false;

            PlayerSkill playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[pottery.Id];
            PlayerAbility playerAbility = playerSkill?[pottery.PotteryTimerId];
            if (playerAbility?.Tier > 0)
            {
                NotifyPlayer(world, outputSlot, byPlayer, "xskills:pottery-finished", "xskillsPotteryMsg");
            }

            //inspiration
            playerAbility = playerSkill?[pottery.InspirationId];
            if (playerAbility == null) return false;
            if (playerAbility.Tier <= 0) return false;

            string name = outputSlot.Itemstack.Collectible.Code.FirstCodePart();
            List<CollectibleObject> dest;
            if (!pottery.InspirationCollectibles.TryGetValue(name, out dest)) return false;

            //create mapping list
            if (dest == null)
            {
                dest = new List<CollectibleObject>();
                pottery.InspirationCollectibles[name] = dest;

                foreach(CollectibleObject collectible in world.Collectibles)
                {
                    if (collectible.Code.Path.Contains(name) && collectible != outputSlot.Itemstack.Collectible)
                    {
                        if (collectible.Code.Path.EndsWith("raw")) continue;
                        if (collectible.Code.Path.EndsWith("fired")) continue;
                        dest.Add(collectible);
                    }
                }
            }

            if (dest.Count <= 0) return true;
            if (world.Rand.NextDouble() < playerAbility.FValue(0) / outputSlot.Itemstack.StackSize)
            {
                CollectibleObject obj = dest[world.Rand.Next(dest.Count - 1)];

                ItemStack stack = new ItemStack(obj, outputSlot.Itemstack.StackSize);
                string type = obj.Attributes["defaultType"].AsString();
                if (type != null) stack.Attributes.SetString("type", type);

                ITreeAttribute pairs = outputSlot.Itemstack.Attributes.GetTreeAttribute("temperature");
                if (pairs != null)
                {
                    float temperature = pairs.GetFloat("temperature", 0.0f);
                    double temperatureLastUpdate = pairs.GetDouble("temperatureLastUpdate", 0.0f);
                    if (temperature > 50.0f)
                    {
                        ITreeAttribute tempTree = stack.Attributes.GetOrAddTreeAttribute("temperature");
                        tempTree.SetFloat("temperature", temperature);
                        tempTree.SetDouble("temperatureLastUpdate", temperatureLastUpdate);
                    }
                }
                outputSlot.Itemstack = stack;
                outputSlot.MarkDirty();
            }
            return true;
        }

        /// <summary>
        /// Notifies the player that a process has finished.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="player">The player.</param>
        /// <param name="msg">The MSG.</param>
        /// <param name="attribute">The name of the attribute. Is used for a cooldown to prohibit spam.</param>

        public static void NotifyPlayer(IWorldAccessor world, ItemSlot slot, IPlayer player, string msg, string attribute)
        {
            BlockPos pos = slot.Inventory.Pos;
            Block block = pos != null ? world.BulkBlockAccessor.GetBlock(pos) : null;

            bool emptyInput = (slot.Inventory as InventorySmelting)?[1].Empty ?? true;

            if (block != null && emptyInput)
            {
                double now = world.Calendar.TotalHours;
                double lastMsg = player.Entity.Attributes.GetDouble(attribute);

                if (now > lastMsg + 0.333)
                {
                    player.Entity.Attributes.SetDouble(attribute, now);
                    world.PlaySoundFor(new AssetLocation("sounds/tutorialstepsuccess.ogg"), player);
                    (player as IServerPlayer)?.SendMessage(0,
                        Lang.Get(msg, block.GetPlacedBlockName(world, pos) +
                        " (" + pos.X + ", " + pos.X + pos.Y + ", " + pos.Z + ")"), EnumChatType.Notification);
                }
            }
        }
    }//!class PotteryUtil
}//!namespace XSkills
