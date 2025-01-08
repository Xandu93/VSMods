using HarmonyLib;
using System;
using System.Reflection;

namespace XSkills
{
    public class ManualPatch
    {
        static internal void PatchMethod(Harmony harmony, Type type, Type patch, string methodName, string prefixName, string postfixName)
        {
            if (harmony == null || type == null || patch == null || methodName == null) return;
            MethodInfo original;
            Type baseType = type;
            do
            {
                original = baseType.GetMethod(methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ??
                           baseType.GetMethod("get_" + methodName, BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                baseType = baseType.BaseType;
            } while (baseType != null && original == null);
            if (original == null) return;

            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            MethodInfo prefix = prefixName != null ? patch.GetMethod(prefixName, flags) : null;
            MethodInfo postfix = postfixName != null ? patch.GetMethod(postfixName, flags) : null;

            HarmonyMethod harmonyPrefix = prefix != null ? new HarmonyMethod(prefix) : null;
            HarmonyMethod harmonyPostfix = postfix != null ? new HarmonyMethod(postfix) : null;

            harmony.Patch(original, harmonyPrefix, harmonyPostfix);
        }

        static internal void PatchMethod(Harmony harmony, Type type, Type patch, string method)
        {
            PatchMethod(harmony, type, patch, method, method + "Prefix", method + "Postfix");
        }

        static internal void PatchConstructor(Harmony harmony, Type type, Type patch)
        {
            if (harmony == null || type == null) return;
            ConstructorInfo original;
            Type baseType = type;
            do
            {
                original = baseType.GetConstructor(new Type[0]);
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
