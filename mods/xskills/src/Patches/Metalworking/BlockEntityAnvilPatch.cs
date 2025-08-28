using HarmonyLib;
using System;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    public static class BlockEntityAnvilExtension
    {
        public static int GetSplitCount(this BlockEntityAnvil anvil) => anvil.WorkItemStack?.Attributes.GetInt("splitCounter") ?? 0;
        public static void SetSplitCount(this BlockEntityAnvil anvil, int value) => anvil.WorkItemStack?.Attributes.SetInt("splitCounter", value);
        public static int GetHitCount(this BlockEntityAnvil anvil) => anvil.WorkItemStack?.Attributes.GetInt("hitCounter") ?? 0;
        public static void SetHitCount(this BlockEntityAnvil anvil, int value) => anvil.WorkItemStack?.Attributes.SetInt("hitCounter", value);
        public static void SetUsedByPlayer(this BlockEntityAnvil anvil, IPlayer player) => anvil.WorkItemStack?.Attributes.SetString("placedBy", player.PlayerUID);
        public static IPlayer GetUsedByPlayer(this BlockEntityAnvil anvil) => anvil.Api.World.PlayerByUid(anvil.WorkItemStack?.Attributes.GetString("placedBy"));
        public static bool GetHelveHammered(this BlockEntityAnvil anvil) => anvil.WorkItemStack?.Attributes.GetBool("helveHammered") ?? false;
        public static void SetHelveHammered(this BlockEntityAnvil anvil, bool value) => anvil.WorkItemStack?.Attributes.SetBool("helveHammered", value);
        public static bool GetWasPlate(this BlockEntityAnvil anvil) => anvil.WorkItemStack?.Attributes.GetBool("wasPlate") ?? false;
        public static void SetWasPlate(this BlockEntityAnvil anvil, bool value) => anvil.WorkItemStack?.Attributes.SetBool("wasPlate", value);
        public static Vec3i[] FindFreeVoxels(this BlockEntityAnvil anvil, int count, Vec3i center, int range)
        {

            bool[,,] recipe = anvil.recipeVoxels;
            List<Vec3i> result = new List<Vec3i>();

            int xMin = Math.Max(center.X - range, 0);
            int xMax = Math.Min(center.X + range, 16);
            int yMin = Math.Max(center.Y - range, 0);
            int yMax = Math.Min(center.Y + range, anvil.SelectedRecipe.QuantityLayers);
            int zMin = Math.Max(center.Z - range, 0);
            int zMax = Math.Min(center.Z + range, 16);

            for (int y = yMin; y < yMax; y++)
            {
                for (int z = zMin; z < zMax; z++)
                {
                    for (int x = xMin; x < xMax; x++)
                    {
                        if (anvil.Voxels[x, y, z] == (byte)EnumVoxelMaterial.Empty && recipe[x, y, z] == true)
                        {
                            result.Add(new Vec3i(x, y, z));
                            if (result.Count >= count) return result.ToArray();
                        }
                    }
                }
            }
            return result.ToArray();
        }

        public static float FinishedProportion(this BlockEntityAnvil anvil)
        {
            int finishedVoxel = 0;
            int neededVoxels = 0;
            int voxelCount = 0;
            bool[,,] recipe = anvil.recipeVoxels;

            if (anvil.SelectedRecipe == null) return -1.0f;

            int ymax = Math.Min(6, anvil.SelectedRecipe.QuantityLayers);

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < ymax; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        byte desiredMat = (byte)(recipe[x, y, z] ? EnumVoxelMaterial.Metal : EnumVoxelMaterial.Empty);
                        if (desiredMat == (byte)EnumVoxelMaterial.Metal)
                        {
                            neededVoxels++;
                            if (anvil.Voxels[x, y, z] == (byte)EnumVoxelMaterial.Metal)
                            {
                                voxelCount++;
                                finishedVoxel++;
                            }
                        }
                        else if (anvil.Voxels[x, y, z] == (byte)EnumVoxelMaterial.Metal)
                        {
                            voxelCount++;
                        }
                    }
                }
            }
            float chance;
            if (voxelCount >= neededVoxels)
                chance = (float)finishedVoxel / (float)voxelCount;
            else
                chance = ((float)finishedVoxel / (float)neededVoxels) * -1.0f;
            return chance;
        }

        public static int FinishRecipe(this BlockEntityAnvil anvil)
        {
            if (anvil.SelectedRecipe == null) return -1;
            int split = 0;
            int ymax = Math.Min(6, anvil.SelectedRecipe.QuantityLayers);
            bool[,,] recipe = anvil.recipeVoxels;

            for (int x = 0; x < 16; x++)
            {
                for (int y = 0; y < ymax; y++)
                {
                    for (int z = 0; z < 16; z++)
                    {
                        byte desiredMat = (byte)(recipe[x, y, z] ? EnumVoxelMaterial.Metal : EnumVoxelMaterial.Empty);
                        if (desiredMat != anvil.Voxels[x, y, z])
                        {
                            if (desiredMat == (byte)EnumVoxelMaterial.Empty && anvil.Voxels[x, y, z] != (byte)EnumVoxelMaterial.Slag)
                            {
                                split++;
                            }
                            else
                            {
                                split--;
                            }
                        }
                        anvil.Voxels[x, y, z] = desiredMat;
                    }
                }
            }
            return split;
        }
    }

    internal class BlockEntityAnvilPatch : ManualPatch
    {
        public const int MAXFORGED = 200;

        public static void Apply(Harmony harmony, Type anvilType)
        {
            Type patch = typeof(BlockEntityAnvilPatch);
            PatchMethod(harmony, anvilType, patch, "TryPut");
            PatchMethod(harmony, anvilType, patch, "OnReceivedClientPacket");
            PatchMethod(harmony, anvilType, patch, "OnUpset");
            PatchMethod(harmony, anvilType, patch, "OnSplit");
            PatchMethod(harmony, anvilType, patch, "OnHit");
            PatchMethod(harmony, anvilType, patch, "onHelveHitSuccess");
            PatchMethod(harmony, anvilType, patch, "CheckIfFinished");
            PatchMethod(harmony, anvilType, patch, "recipeVoxels");
        }

        public static void TryPutPrefix(BlockEntityAnvil __instance, ref bool __state, IPlayer byPlayer)
        {
            CollectibleObject collectible = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack?.Collectible;
            __state = collectible is ItemMetalPlate;
        }

        public static void TryPutPostfix(BlockEntityAnvil __instance, bool __state, IPlayer byPlayer)
        {
            __instance.SetUsedByPlayer(byPlayer);
            if (__state) __instance.SetWasPlate(__state);
        }

        public static void OnReceivedClientPacketPrefix(BlockEntityAnvil __instance, IPlayer player)
        {
            __instance.SetUsedByPlayer(player);
        }

        public static void OnUpsetPrefix(BlockEntityAnvil __instance, Vec3i voxelPos, BlockFacing towardsFace)
        {
            if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Metal)
            {
                __instance.SetHitCount(__instance.GetHitCount() + 1);
            }
        }

        public static void OnSplitPrefix(BlockEntityAnvil __instance, Vec3i voxelPos)
        {
            if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != (byte)EnumVoxelMaterial.Empty)
            {
                __instance.SetHitCount(__instance.GetHitCount() + 1);
                if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Metal)
                {
                    __instance.SetSplitCount(__instance.GetSplitCount() + 1);
                }
            }
        }
        public static bool OnHitPrefix(BlockEntityAnvil __instance, Vec3i voxelPos)
        {
            if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] != (byte)EnumVoxelMaterial.Empty) __instance.SetHitCount(__instance.GetHitCount() + 1);

            IPlayer player = __instance.GetUsedByPlayer() ?? (__instance.Api as ClientCoreAPI)?.World.Player;
            Metalworking metalworking = __instance.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("metalworking") as Metalworking;

            if (player == null || metalworking == null) return true;

            //heavy hits
            PlayerSkill playerSkill = player.Entity?.GetBehavior<PlayerSkillSet>()?[metalworking.Id];
            PlayerAbility playerAbility = playerSkill?[metalworking.HeavyHitsId];
            if (playerAbility == null) return true;

            if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Slag && playerAbility.Tier > 0)
            {
                __instance.OnSplit(voxelPos);
                return false;
            }
            else if (__instance.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z] == (byte)EnumVoxelMaterial.Metal)
            {
                //master smith
                playerAbility = playerSkill[metalworking.MasterSmithId];
                bool[,,] recipe = __instance.recipeVoxels;
                int range = playerAbility.Value(1);

                int movedMax = playerAbility.Value(0);
                if (movedMax <= 0) return true;

                Vec3i[] freeVoxels = __instance.FindFreeVoxels(movedMax, voxelPos, range);
                if (freeVoxels.Length <= 0) return true;
                int moved = 0;

                for (int y = voxelPos.Y; y >= 0 && y >= y - 1 && moved < freeVoxels.Length; y--)
                {
                    for (int z = voxelPos.Z - 1; z <= voxelPos.Z + 1 && moved < freeVoxels.Length; z++)
                    {
                        if (z < 0 || z >= 16) continue;
                        for (int x = voxelPos.X - 1; x <= voxelPos.X + 1 && moved < freeVoxels.Length; x++)
                        {
                            if (x < 0 || x >= 16) continue;
                            if (__instance.Voxels[x, y, z] == (byte)EnumVoxelMaterial.Metal && recipe[x, y, z] == false)
                            {
                                Vec3i voxel = freeVoxels[moved];
                                if (voxel != null)
                                {
                                    __instance.Voxels[x, y, z] = (byte)EnumVoxelMaterial.Empty;
                                    __instance.Voxels[voxel.X, voxel.Y, voxel.Z] = (byte)EnumVoxelMaterial.Metal;
                                    moved++;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }

        public static void onHelveHitSuccessPostfix(BlockEntityAnvil __instance, EnumVoxelMaterial mat, Vec3i usableMetalVoxel)
        {
            __instance.SetHelveHammered(true);
            __instance.SetHitCount(__instance.GetHitCount() + 1);
            if (mat == EnumVoxelMaterial.Metal && usableMetalVoxel == null)
            {
                __instance.SetSplitCount(__instance.GetSplitCount() + 1);
            }
        }

        public static bool CheckIfFinishedPrefix(BlockEntityAnvil __instance, out AnvilState __state, IPlayer byPlayer)
        {
            __state = new AnvilState(__instance);
            if (__state.recipe == null) return true;

            PlayerSkill playerSkill = null;
            PlayerAbility playerAbility;
            float finished = -1.0f;
            MetalworkingConfig config = __state.metalworking.Config as MetalworkingConfig;

            if (__state.metalworking == null || __state.workItemStack == null) return true;
            bool helveHammer = false;

            //machine learning
            if (byPlayer == null)
            {
                helveHammer = true;
                byPlayer = __instance.GetUsedByPlayer();
                playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[__state.metalworking.Id];
                if (playerSkill == null) return true;
                if (playerSkill[__state.metalworking.MachineLearningId]?.Tier <= 0) return true;
            }

            IWorldAccessor world = __instance.Api.World;
            CollectibleObject collectible = __state.workItemStack.Collectible;
            float temperature = collectible.GetTemperature(world, __state.workItemStack);
            playerSkill = playerSkill ?? byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[__state.metalworking.Id];

            //finishing touch
            playerAbility = playerSkill?[__state.metalworking.FinishingTouchId];
            if (playerAbility?.Tier > 0)
            {
                finished = __instance.FinishedProportion();

                if (finished < 0.0f && (config?.allowFinishingTouchExploit ?? false))
                {
                    finished *= -1.0f;
                }

                float chanceMult = Math.Min(playerAbility.Value(0) + playerAbility.Value(1) * 0.1f, playerAbility.Value(2)) * 0.01f;

                if (finished > 0.0f && chanceMult * finished * finished >= world.Rand.NextDouble())
                {
                    __state.splitCount += __instance.FinishRecipe();
                    finished = 1.0f;
                }
            }

            //metal recovery
            playerAbility = playerSkill?[__state.metalworking.MetalRecoveryId];
            int divideBy = playerAbility?.Value(0) ?? 0;
            if (divideBy > 0 && !__state.wasIronBloom)
            {
                int bitsCount = __state.splitCount / divideBy;
                if (bitsCount > 0)
                {
                    string domain = config.useVanillaBits ? "game" : "xskills";
                    string baseMaterial = __state.anvilItemStack.GetBaseMaterial(__state.workItemStack).Collectible.LastCodePart();
                    if (baseMaterial == "steel" && !__instance.Api.ModLoader.IsModEnabled("smithingplus")) baseMaterial = "blistersteel";

                    AssetLocation MetalBitsCode = new AssetLocation(domain, "metalbit" + "-" + baseMaterial);
                    Item metalBitsItem = world.GetItem(MetalBitsCode);
                    __instance.SetSplitCount(__state.splitCount - divideBy * bitsCount);

                    if (metalBitsItem != null)
                    {
                        ItemStack metalBitsStack = new ItemStack(metalBitsItem, bitsCount);
                        metalBitsItem.SetTemperature(world, metalBitsStack, temperature);
                        if (helveHammer || !byPlayer.InventoryManager.TryGiveItemstack(metalBitsStack))
                        {
                            __instance.Api.World.SpawnItemEntity(metalBitsStack, __instance.Pos.ToVec3d().Add(0.5, 1.5, 0.5));
                        }
                    }
                }
            }

            //heating hits
            playerAbility = playerSkill?[__state.metalworking.HeatingHitsId];
            float meltingpoint = 
                __state.anvilItemStack != null ?
                collectible.GetMeltingPoint(world, null, new DummySlot(__state.anvilItemStack.GetBaseMaterial(__state.workItemStack))) :
                0.0f;
            if (meltingpoint > 0.0f && playerAbility != null)
            {
                if (temperature < meltingpoint)
                {
                    temperature = Math.Min(temperature + playerAbility.Value(0), meltingpoint);
                }
            }
            else temperature = temperature + playerAbility.Value(0);
            collectible.SetTemperature(world, __state.workItemStack, temperature);

            //blacksmith
            collectible = __state.recipe.Output.ResolvedItemstack.Collectible;
            playerAbility = playerSkill?[__state.metalworking.BlacksmithId];
            string type = QualityUtil.GetQualityType(collectible);

            if (playerAbility?.Tier > 0 &&
                !(__state.wasPlate && collectible is ItemMetalPlate) && 
                (type != null || 
                collectible.Tool != null || 
                collectible.Code.Path.Contains("head") || 
                collectible.GetMaxDurability(__state.recipe.Output.ResolvedItemstack) > 1))
            {
                int forged = type != null ?
                    byPlayer.Entity.WatchedAttributes.GetTreeAttribute("forged")?.GetInt(type) ?? 0 : 0;
                float quality = Math.Min(forged, MAXFORGED) * 0.01f + Math.Min(playerSkill.Level, 25) * 0.1f;

                //subtract 1.0f for quenching
                quality = Math.Min(quality * playerAbility.Value(0), playerAbility.Value(1) * 0.5f - 1.0f);
                //subtract 2.0f for quenching
                quality = Math.Min(quality + (float)byPlayer.Entity.World.Rand.NextDouble() * quality, playerAbility.Value(1) - 2.0f);

                if (config.qualitySteps > 0.001f)
                {
                    quality = (int)(quality / config.qualitySteps + 0.5f) * config.qualitySteps;
                }

                __state.recipe.Output.ResolvedItemstack.Attributes.SetFloat("quality", quality);
            }

            if (finished >= 1.0f || finished < 0.0f || byPlayer == null) return true;
            else return false;
        }

        public static void CheckIfFinishedPostfix(BlockEntityAnvil __instance, ref AnvilState __state, IPlayer byPlayer)
        {
            //if the workitemstack was set to null, the item was finished
            ItemStack resolvedStack = __state.recipe?.Output.ResolvedItemstack;
            float quality = resolvedStack?.Attributes.GetFloat("quality") ?? 0.0f;
            resolvedStack?.Attributes.RemoveAttribute("quality");
            if (__instance.WorkItemStack != null || __state.metalworking == null) return;

            PlayerSkill playerSkill = null;
            PlayerAbility playerAbility;
            bool allowAbilities = true;
            bool helveHammer = false;
            IWorldAccessor world = __instance.Api.World;

            //machine learning
            if (byPlayer == null)
            {
                helveHammer = true;
                byPlayer = __state.usedBy;
                playerSkill = byPlayer?.Entity?.GetBehavior<PlayerSkillSet>()?[__state.metalworking.Id];
                if (playerSkill == null) return;
                if (playerSkill[__state.metalworking.MachineLearningId]?.Tier <= 0) allowAbilities = false;
            }
            playerSkill = byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[__state.metalworking.Id];
            if (playerSkill == null) return;

            //experience
            MetalworkingConfig config = __state.metalworking.Config as MetalworkingConfig;
            if (config != null && !__state.wasPlate)
            {
                float exp = (__state.metalworking.Config as MetalworkingConfig).expBase + (__state.metalworking.Config as MetalworkingConfig).expPerHit * __state.hitCount;
                if (__state.helveHammer) exp *= 1.0f - config.helveHammerPenalty;
                playerSkill.AddExperience(exp);
            }

            if (!__state.helveHammer)
            {
                //careful hits
                playerAbility = playerSkill[__state.metalworking.HammerExpertId];
                if (playerAbility == null) return;
                ItemStack tool = byPlayer.InventoryManager?.ActiveHotbarSlot?.Itemstack;
                if (tool?.Collectible?.Tool == EnumTool.Hammer)
                {
                    int oldDurability = tool.Attributes.GetInt("durability");
                    int newDurability = oldDurability + (int)(__state.hitCount * playerAbility.SkillDependentFValue());
                    newDurability = Math.Min(newDurability, tool.Collectible.Durability);
                    if (newDurability > oldDurability) tool.Attributes.SetInt("durability", newDurability);
                }
            }

            if (__instance.Api.Side == EnumAppSide.Client || !allowAbilities) return;

            //duplicator
            float temperature = __state.workItemStack.Collectible.GetTemperature(world, __state.workItemStack);
            if ((resolvedStack?.Collectible.CombustibleProps == null || __state.metalworking.IsDuplicatable(__state.recipe)) && !__state.wasPlate)
            {
                playerAbility = playerSkill[__state.metalworking.DuplicatorId];
                if (playerAbility == null) return;
                if (playerAbility.SkillDependentFValue() >= world.Rand.NextDouble())
                {
                    ItemStack outstack = resolvedStack.Clone();
                    outstack.Collectible.SetTemperature(world, outstack, temperature);
                    if (quality > 0.0f) outstack.Attributes.SetFloat("quality", quality);
                    if (helveHammer || !byPlayer.InventoryManager.TryGiveItemstack(outstack))
                    {
                        world.SpawnItemEntity(outstack, __instance.Pos.ToVec3d().Add(0.5, 1.5, 0.5));
                    }
                }
            }

            //count how much tools, armor, weapons have been crafted
            ITreeAttribute tree = byPlayer.Entity.WatchedAttributes.GetOrAddTreeAttribute("forged");
            string type = QualityUtil.GetQualityType(resolvedStack?.Collectible);
            if (type != null) tree.SetInt(type, Math.Min(tree.GetInt(type, 0) + 1, MAXFORGED));
        }

        public static bool recipeVoxelsPrefix(BlockEntityAnvil __instance, out bool[,,] __result)
        {
            if (__instance.SelectedRecipe == null)
            {
                __result = null;
                return false;
            }

            bool[,,] origVoxels = __instance.SelectedRecipe.Voxels;
            bool[,,] rotVoxels = new bool[origVoxels.GetLength(0), origVoxels.GetLength(1), origVoxels.GetLength(2)];
            int rotations = (__instance.rotation / 90) % 4;

            if (rotations == 0)
            {
                __result = origVoxels;
                return false;
            }
            else if (rotations == 1)
            {
                for (int x = 0; x < origVoxels.GetLength(0); x++)
                {
                    for (int y = 0; y < origVoxels.GetLength(1); y++)
                    {
                        for (int z = 0; z < origVoxels.GetLength(2); z++)
                        {
                            rotVoxels[z, y, x] = origVoxels[16 - x - 1, y, z];
                        }
                    }
                }
            }
            else if (rotations == 2)
            {
                for (int x = 0; x < origVoxels.GetLength(0); x++)
                {
                    for (int y = 0; y < origVoxels.GetLength(1); y++)
                    {
                        for (int z = 0; z < origVoxels.GetLength(2); z++)
                        {
                            rotVoxels[x, y, z] = origVoxels[16 - x - 1, y, 16 - z - 1];
                        }
                    }
                }
            }
            else if (rotations == 3)
            {
                for (int x = 0; x < origVoxels.GetLength(0); x++)
                {
                    for (int y = 0; y < origVoxels.GetLength(1); y++)
                    {
                        for (int z = 0; z < origVoxels.GetLength(2); z++)
                        {
                            rotVoxels[z, y, x] = origVoxels[x, y, 16 - z - 1];
                        }
                    }
                }
            }

            __result = rotVoxels;
            return false;
        }
    }//!class BlockEntityAnvilPatch

    public class AnvilState
    {
        public IPlayer usedBy;
        public SmithingRecipe recipe;
        public ItemStack workItemStack;
        public Metalworking metalworking;
        public int splitCount;
        public int hitCount;
        public IAnvilWorkable anvilItemStack;
        public bool wasIronBloom;
        public bool helveHammer;
        public bool wasPlate;

        public AnvilState(BlockEntityAnvil anvil)
        {
            usedBy = anvil.GetUsedByPlayer();
            recipe = anvil.SelectedRecipe;
            workItemStack = anvil.WorkItemStack;
            metalworking = anvil.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("metalworking") as Metalworking;
            splitCount = anvil.GetSplitCount();
            hitCount = anvil.GetHitCount();
            anvilItemStack = anvil.WorkItemStack.Collectible as IAnvilWorkable;
            wasIronBloom = anvil.WorkItemStack.Item as ItemIronBloom != null;
            helveHammer = anvil.GetHelveHammered();
            wasPlate = anvil.GetWasPlate();
        }
    }

    [HarmonyPatch(typeof(ItemWorkItem))]
    internal class ItemWorkItemPatch
    {
        [HarmonyPatch("GetHelveWorkableMode")]
        public static void Postfix(ItemWorkItem __instance, ref EnumHelveWorkableMode __result, BlockEntityAnvil beAnvil)
        {
            if (__result != EnumHelveWorkableMode.NotWorkable) return;
            if (beAnvil?.SelectedRecipe == null) return;

            Metalworking metalworking = beAnvil.Api.ModLoader.GetModSystem<XLeveling>()?.GetSkill("metalworking") as Metalworking;
            if (metalworking == null) return;
            PlayerAbility playerAbility = beAnvil.GetUsedByPlayer()?.Entity?.GetBehavior<PlayerSkillSet>()?[metalworking.Id]?[metalworking.AutomatedSmithingId];
            if (playerAbility == null) return;

            int ymax = playerAbility.Value(0);
            if (ymax <= 0) return;
            bool[,,] voxels = beAnvil.SelectedRecipe.Voxels;
            for (int yy = 0; yy < beAnvil.SelectedRecipe.QuantityLayers; yy++)
            {
                bool search = true;
                for (int xx = 0; xx < voxels.GetLength(0) && search; xx++)
                {
                    for (int zz = 0; zz < voxels.GetLength(2) && search; zz++)
                    {
                        if(voxels[xx,yy,zz])
                        {
                            search = false;
                            if (yy > ymax) return;
                        }
                    }
                }
            }
            __result = EnumHelveWorkableMode.TestSufficientVoxelsWorkable;
        }
    }

}//!namespace XSkills
