using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    /// <summary>
    /// The patch for the ModSystemWearableStats class.
    /// </summary>
    [HarmonyPatch(typeof(ModSystemWearableStats))]
    public static class ModSystemWearableStatsPatch
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
            xSkills.Skills.TryGetValue("combat", out skill);
            Combat combat = skill as Combat;

            if (!(combat?.Enabled ?? false)) return false;
            if (original == null) return true;

            if (original.Name == "applyShieldProtection")
            {
                return
                    combat[combat.TankId].Enabled ||
                    combat[combat.DefenderId].Enabled ||
                    combat[combat.GuardianId].Enabled;
            }
            else return true;
        }

        public class applyShieldProtectionState
        {
            public string usetype;
            public bool projectile;
            public float chance;
            public float flatdmgabsorb;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("applyShieldProtection")]
        public static IEnumerable<CodeInstruction> applyShieldProtectionTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> code = new List<CodeInstruction>(instructions);
            int begin = -1;
            for (int ii = 0; ii < code.Count; ++ii)
            {
                if (code[ii].opcode == OpCodes.Callvirt)
                {
                    MethodInfo info = code[ii].operand as MethodInfo;
                    if (info.Name == "NextDouble")
                    {
                        ii++;
                        if (code[ii].opcode == OpCodes.Stloc_S)
                        {
                            begin = ii + 1;
                            break;
                        }

                    }
                }
            }
            if (begin == -1) return code;
            MethodInfo method = typeof(ModSystemWearableStatsPatch).GetMethod("ApplyShieldAbilities");
            List<CodeInstruction> newCode = new()
            {
                new CodeInstruction(OpCodes.Ldarg_1),       //player
                new CodeInstruction(OpCodes.Ldloca_S, 8),   //flatdmgabsorb
                new CodeInstruction(OpCodes.Ldloca_S, 9),   //chance
                new CodeInstruction(OpCodes.Ldloc_S, 7),    //usetype
                new CodeInstruction(OpCodes.Call, method)
            };
            code.InsertRange(begin, newCode);
            return code;
        }

        public static void ApplyShieldAbilities(IPlayer player, ref float flatdmgabsorb, ref float chance, string usetype)
        {
            if (player == null) return;
            PlayerSkillSet skillSet = player.Entity.GetBehavior<PlayerSkillSet>();
            if (skillSet == null) return;
            Combat combat = skillSet.XLeveling?.GetSkill("combat") as Combat;
            if (combat == null) return;
            PlayerSkill playerSkill = skillSet[combat.Id];
            if (playerSkill == null) return;

            PlayerAbility playerAbility = playerSkill[combat.TankId];
            if (playerAbility == null) return;
            flatdmgabsorb *= 1.0f + playerAbility.SkillDependentFValue();

            playerAbility = null;
            if (usetype == "active")
            {
                playerAbility = playerSkill[combat.DefenderId];
            }
            else
            {
                playerAbility = playerSkill[combat.GuardianId];
            }
            if (playerAbility == null) return;
            chance += playerAbility.FValue(0);
        }
    }
}
