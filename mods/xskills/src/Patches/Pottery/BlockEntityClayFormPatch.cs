using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the BlockEntityClayForm class.
    /// </summary>
    [HarmonyPatch(typeof(BlockEntityClayForm))]
    public class BlockEntityClayFormPatch
    {
        /// <summary>
        /// Prepares the Harmony patch.
        /// Only patches the methods if necessary.
        /// </summary>
        /// <param name="original">The method to be patched.</param>
        /// <returns>whether the method should be patched.</returns>
        public static bool Prepare(MethodBase original)
        {
            XSkills xSkills = XSkills.Instance;
            if (xSkills == null) return false;
            Skill skill; 
            xSkills.Skills.TryGetValue("pottery", out skill);
            Pottery pottery = skill as Pottery;

            if (!(pottery?.Enabled ?? false)) return false;
            if (original == null) return true;

            if (original.Name == "OnUseOver")
            {
                return
                    pottery[pottery.InfallibleId].Enabled ||
                    pottery[pottery.PerfectionistId].Enabled ||
                    pottery[pottery.PerfectFitId].Enabled ||
                    pottery[pottery.LayerLayerId].Enabled ||
                    pottery[pottery.ThriftId].Enabled;
            }
            else return true;
        }

        /// <summary>
        /// Harmony Transpiler to replace "OnCopyLayer", "OnRemove" and "OnAdd" with custom methods to allow access to the player data.
        /// see: https://github.com/anegostudios/vssurvivalmod/blob/master/BlockEntity/BEClayForm.cs
        /// </summary>
        /// <param name="instructions">The instructions.</param>
        /// <returns>The new instructions.</returns>
        [HarmonyTranspiler]
        [HarmonyPatch("OnUseOver", new Type[] { typeof(IPlayer), typeof(Vec3i), typeof(BlockFacing), typeof(bool) })]
        public static IEnumerable<CodeInstruction> OnUseOverTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            int onCopyLayer = -1;
            int onRemove = -1;
            int onAdd = -1;

            for (int ii = 0; ii < code.Count; ++ii)
            {
                if (code[ii].opcode == OpCodes.Call)
                {
                    MethodInfo info = code[ii].operand as MethodInfo;
                    if (info.Name == "OnCopyLayer")
                    {
                        onCopyLayer = ii;
                    }
                    else if (info.Name == "OnAdd")
                    {
                        onAdd = ii;
                    }
                    else if (info.Name == "OnRemove")
                    {
                        onRemove = ii;
                    }
                }
            }

            Type patchType = typeof(BlockEntityClayFormPatch);

            //Replaces the original methods and adds the player as last argument.
            if (onRemove >= 0)
            {
                code.Insert(onRemove, new CodeInstruction(OpCodes.Ldarg_1));
                code[onRemove + 1] = new CodeInstruction(OpCodes.Call, patchType.GetMethod("OnRemove"));

                if (onAdd > onRemove) onAdd++;
                if (onCopyLayer > onRemove) onCopyLayer++;
            }
            if (onAdd >= 0)
            {
                code.Insert(onAdd, new CodeInstruction(OpCodes.Ldarg_1));
                code[onAdd + 1] = new CodeInstruction(OpCodes.Call, patchType.GetMethod("OnAdd", new Type[] { typeof(BlockEntityClayForm), typeof(int), typeof(Vec3i), typeof(BlockFacing), typeof(int), typeof(IPlayer) }));

                if (onCopyLayer > onAdd) onCopyLayer++;
            }
            if (onCopyLayer >= 0)
            {
                code.Insert(onCopyLayer, new CodeInstruction(OpCodes.Ldarg_1));
                code[onCopyLayer + 1] = new CodeInstruction(OpCodes.Call, patchType.GetMethod("OnCopyLayer"));
            }
            return code;
        }

        /// <summary>
        /// Stores the state of the CheckIfFinished method.
        /// </summary>
        public class ClayFormCheckIfFinishedState
        {
            public ItemStack workItemStack;
            public ClayFormingRecipe recipe;
            public Pottery pottery;
            public PlayerSkill playerSkill;
        }//!ClayFormCheckIfFinishedState

        /// <summary>
        /// Prefix for the CheckIfFinished method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="byPlayer">The player.</param>
        /// <param name="___workItemStack">The work item stack.</param>
        [HarmonyPrefix]
        [HarmonyPatch("CheckIfFinished")]
        public static void CheckIfFinishedPrefix(BlockEntityClayForm __instance, out ClayFormCheckIfFinishedState __state, IPlayer byPlayer, ItemStack ___workItemStack)
        {
            __state = new ClayFormCheckIfFinishedState();
            __state.workItemStack = ___workItemStack;
            __state.recipe = __instance.SelectedRecipe;

            __state.pottery = XLeveling.Instance(byPlayer.Entity.Api).GetSkill("pottery") as Pottery;
            if (__state.pottery == null) return;
            __state.playerSkill = (byPlayer.Entity.GetBehavior<PlayerSkillSet>() as PlayerSkillSet)?[__state.pottery.Id];
            if (__state.playerSkill == null) return;

            //fast potter
            PlayerAbility playerAbility = __state.playerSkill[__state.pottery.FastPotterId];
            if (playerAbility == null) return;
            if (playerAbility.Tier > 0)
            {
                float finished = PotteryUtil.FinishedProportion(__instance);
                float chanceMult = Math.Min(playerAbility.Value(0) + playerAbility.Value(1) * 0.1f, playerAbility.Value(2)) * 0.01f;
                if (chanceMult * finished * finished >= byPlayer.Entity.World.Rand.NextDouble())
                {
                    PotteryUtil.FinishRecipe(__instance, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
                }
            }
        }

        /// <summary>
        /// Postfix for the CheckIfFinished method.
        /// </summary>
        /// <param name="__instance">The instance.</param>
        /// <param name="__state">The state.</param>
        /// <param name="byPlayer">The player.</param>
        /// <param name="___workItemStack">The work item stack.</param>
        [HarmonyPostfix]
        [HarmonyPatch("CheckIfFinished")]
        public static void CheckIfFinishedPostfix(BlockEntityClayForm __instance, ClayFormCheckIfFinishedState __state, IPlayer byPlayer, ItemStack ___workItemStack)
        {
            if (___workItemStack != null || byPlayer == null || __state.playerSkill == null) return;

            int voxelCount = PotteryUtil.CountVoxels(__state.recipe);

            //experience
            __state.playerSkill.AddExperience(1.0f + voxelCount * 0.002f);

            //jackPot
            PlayerAbility playerAbility = __state.playerSkill[__state.pottery.JackPotId];
            if (playerAbility.SkillDependentValue() * 0.01f >= byPlayer.Entity.World.Rand.NextDouble())
            {
                ItemStack outstack = __state.recipe.Output.ResolvedItemstack.Clone();
                if (!byPlayer.InventoryManager.TryGiveItemstack(outstack))
                {
                    byPlayer.Entity.World.SpawnItemEntity(outstack, byPlayer.Entity.Pos.XYZ.Add(0.5f, 0.5f, 0.5f));
                }
            }
        }

        /// <summary>
        /// Replacement for the default OnAdd method.
        /// Allow access to the player data.
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="voxelPos">The voxel position.</param>
        /// <param name="facing">The facing.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="player">The player.</param>
        /// <returns></returns>
        public static bool OnAdd(BlockEntityClayForm clayForm, int layer, Vec3i voxelPos, BlockFacing facing, int radius, IPlayer player)
        {
            if (voxelPos.Y == layer && facing.IsVertical)
            {
                return OnAdd(clayForm, layer, voxelPos, radius, player);
            }

            if (clayForm.Voxels[voxelPos.X, voxelPos.Y, voxelPos.Z])
            {
                Vec3i offPos = voxelPos.AddCopy(facing);
                if (layer >= 0 && layer < 16 && clayForm.InBounds(offPos, LayerBounds(layer, clayForm.SelectedRecipe)))
                {
                    return OnAdd(clayForm, layer, offPos, radius, player);
                }
            }
            else
            {
                return OnAdd(clayForm, layer, voxelPos, radius, player);
            }
            return false;
        }

        /// <summary>
        /// Replacement for the default OnAdd method.
        /// Allows access to the player data.
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="voxelPos">The voxel position.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="byPlayer">The player.</param>
        /// <returns></returns>
        public static bool OnAdd(BlockEntityClayForm clayForm, int layer, Vec3i voxelPos, int radius, IPlayer byPlayer)
        {
            bool didadd = false;
            bool ignoreWrongVoxels = false;

            //perfectionist
            Pottery pottery = XLeveling.Instance(byPlayer.Entity.Api).GetSkill("pottery") as Pottery;
            PlayerSkill playerSkill = pottery != null ? byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[pottery.Id] : null;

            if (playerSkill != null)
            {
                PlayerAbility playerAbility = playerSkill[pottery.PerfectionistId];
                if (playerAbility?.Tier > 0)
                {
                    //infallible
                    playerAbility = playerSkill[pottery.InfallibleId];
                    if (radius <= playerAbility?.Value(0))
                    {
                        ignoreWrongVoxels = true;
                    }
                }
            }

            Cuboidi bounds = LayerBounds(layer, clayForm.SelectedRecipe);

            for (int dx = -(int)Math.Ceiling(radius / 2f); dx <= radius / 2; dx++)
            {
                for (int dz = -(int)Math.Ceiling(radius / 2f); dz <= radius / 2; dz++)
                {
                    Vec3i offPos = voxelPos.AddCopy(dx, 0, dz);
                    if (clayForm.InBounds(offPos, bounds) && offPos.Y == layer)
                    {
                        if (!clayForm.Voxels[offPos.X, offPos.Y, offPos.Z])
                        {
                            if (!clayForm.SelectedRecipe.Voxels[offPos.X, offPos.Y, offPos.Z] && ignoreWrongVoxels) continue;
                            clayForm.AvailableVoxels--;
                            if (clayForm.AvailableVoxels < 0)
                            {
                                PotteryUtil.AddClay(clayForm, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
                            }
                            didadd = true;
                        }
                        clayForm.Voxels[offPos.X, offPos.Y, offPos.Z] = true;
                    }
                }
            }
            return didadd;
        }

        /// <summary>
        /// Replacement for the default OnRemove method.
        /// Allows access to the player data.
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="voxelPos">The voxel position.</param>
        /// <param name="facing">The facing.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="byPlayer">The player.</param>
        /// <returns></returns>
        public static bool OnRemove(BlockEntityClayForm clayForm, int layer, Vec3i voxelPos, BlockFacing facing, int radius, IPlayer byPlayer)
        {
            bool didremove = false;
            if (voxelPos.Y != layer) return didremove;
            bool ignoreRightVoxels = false;

            //perfect fit
            Pottery pottery = XLeveling.Instance(byPlayer.Entity.Api).GetSkill("pottery") as Pottery;
            PlayerSkill playerSkill = pottery != null ? byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[pottery.Id] : null;

            if (playerSkill != null)
            {
                PlayerAbility playerAbility = playerSkill[pottery.PerfectFitId];
                if (playerAbility?.Tier > 0)
                {
                    //infallible
                    playerAbility = playerSkill[pottery.InfallibleId];
                    if (radius <= playerAbility?.Value(0))
                    {
                        ignoreRightVoxels = true;
                    }
                }
            }

            for (int dx = -(int)Math.Ceiling(radius / 2f); dx <= radius / 2; dx++)
            {
                for (int dz = -(int)Math.Ceiling(radius / 2f); dz <= radius / 2; dz++)
                {
                    Vec3i offPos = voxelPos.AddCopy(dx, 0, dz);

                    if (offPos.X >= 0 && offPos.X < 16 && offPos.Y >= 0 && offPos.Y <= 16 && offPos.Z >= 0 && offPos.Z < 16)
                    {
                        if (clayForm.SelectedRecipe.Voxels[offPos.X, offPos.Y, offPos.Z] && ignoreRightVoxels) continue;

                        bool hadVoxel = clayForm.Voxels[offPos.X, offPos.Y, offPos.Z];
                        didremove |= hadVoxel;

                        clayForm.Voxels[offPos.X, offPos.Y, offPos.Z] = false;
                        if (hadVoxel) clayForm.AvailableVoxels++;
                    }
                }
            }
            return didremove;
        }

        /// <summary>
        /// Replacement for the default OnCopyLayer method.
        /// Allows access to the player data.
        /// </summary>
        /// <param name="clayForm">The clay form.</param>
        /// <param name="layer">The layer.</param>
        /// <param name="byPlayer">The player.</param>
        /// <returns></returns>
        public static bool OnCopyLayer(BlockEntityClayForm clayForm, int layer, IPlayer byPlayer)
        {
            if (layer <= 0 || layer > 15) return false;

            bool didplace = false;
            int quantity = 4;
            bool ignoreWrongVoxel = false;

            //layer layer
            Pottery pottery = XLeveling.Instance(byPlayer.Entity.Api).GetSkill("pottery") as Pottery;
            PlayerSkill playerSkill = pottery != null ? byPlayer.Entity.GetBehavior<PlayerSkillSet>()?[pottery.Id] : null;

            if (playerSkill != null)
            {
                PlayerAbility playerAbility = playerSkill[pottery.LayerLayerId];
                quantity += playerAbility?.Value(0) ?? 0;

                //infallible
                playerAbility = playerSkill[pottery.InfallibleId];
                if (playerAbility?.Tier >= 2) ignoreWrongVoxel = true;
            }

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (clayForm.Voxels[x, layer - 1, z] && !clayForm.Voxels[x, layer, z])
                    {
                        if (!ignoreWrongVoxel || clayForm.SelectedRecipe.Voxels[x, layer, z])
                        {
                            quantity--;
                            clayForm.Voxels[x, layer, z] = true;
                            clayForm.AvailableVoxels--;
                            didplace = true;
                            if (clayForm.AvailableVoxels < 0)
                            {
                                PotteryUtil.AddClay(clayForm, byPlayer.InventoryManager.ActiveHotbarSlot, byPlayer.Entity);
                            }
                        }
                    }
                    if (quantity == 0) return didplace;
                }
            }
            return didplace;
        }

        public static Cuboidi LayerBounds(int layer, ClayFormingRecipe SelectedRecipe)
        {
            Cuboidi bounds = new Cuboidi(8, 8, 8, 8, 8, 8);

            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    if (SelectedRecipe.Voxels[x, layer, z])
                    {
                        bounds.X1 = Math.Min(bounds.X1, x);
                        bounds.X2 = Math.Max(bounds.X2, x);
                        bounds.Z1 = Math.Min(bounds.Z1, z);
                        bounds.Z2 = Math.Max(bounds.Z2, z);
                    }
                }
            }

            return bounds;
        }

    }//!class BlockEntityClayFormPatch
}//!namespace XSkills