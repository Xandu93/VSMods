//using HarmonyLib;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Vintagestory.GameContent;

//namespace XSkills
//{
//    [HarmonyPatch(typeof(BlockEntityTorch))]
//    public class BlockEntityTorchPatch
//    {
//        [HarmonyPostfix]
//        [HarmonyPatch("Initialize")]
//        public static void InitializePostfix(BlockEntityTorch __instance, TransientProperties ___props)
//        {
            //don't know how to access the player
//            ___props.InGameHours *= 2;
//        }
//    }
//}
