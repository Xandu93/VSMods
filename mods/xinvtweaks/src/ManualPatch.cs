using HarmonyLib;
using System;
using System.Reflection;

namespace XInvTweaks
{
    internal class ManualPatch
    {
        static internal void PatchMethod(Harmony harmony, Type type, Type patch, string method)
        {
            if (harmony == null || type == null || patch == null || method == null) return;
            MethodInfo original;
            Type baseType = type;
            do
            {
                original = baseType.GetMethod(method, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                           baseType.GetMethod("get_" + method, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                baseType = baseType.BaseType;
            } while (baseType != null && original == null);
            if (original == null) return;

            MethodInfo prefix = patch.GetMethod(method + "Prefix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo postfix = patch.GetMethod(method + "Postfix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            HarmonyMethod harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
            HarmonyMethod harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

            harmony.Patch(original, harmonyPrefix, harmonyPostfix);
        }

        static internal void PatchConstructor(Harmony harmony, Type type, Type patch)
        {
            if (harmony == null || type == null) return;
            ConstructorInfo original;
            Type baseType = type;
            do
            {
                original = baseType.GetConstructor(new Type[0]);
                ConstructorInfo[] constructors = baseType.GetConstructors();

                if (original == null && constructors.Length > 0)
                {
                    original = constructors[0];
                }

                baseType = baseType.BaseType;
            } while (baseType != null && original == null);
            if (original == null) return;

            MethodInfo prefix = patch.GetMethod("ConstructorPrefix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo postfix = patch.GetMethod("ConstructorPostfix", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            HarmonyMethod harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
            HarmonyMethod harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

            harmony.Patch(original, harmonyPrefix, harmonyPostfix);
        }

    }//!class ManualPatch
}//!namespace XSkills
