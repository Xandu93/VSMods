//using HarmonyLib;
//using Vintagestory.API.Common;
//using Vintagestory.API.Common.Entities;
//using Vintagestory.API.Datastructures;
//using Vintagestory.GameContent;

//namespace XSkills
//{
    //public class XSkillsEntityBehaviorCommandable : EntityBehaviorCommandable
    //{
    //    public XSkillsEntityBehaviorCommandable(Entity entity) : base(entity)
    //    { }

    //    public override void OnEntityDespawn(EntityDespawnData despawn)
    //    {
    //        base.OnEntityDespawn(despawn);

    //        string uid = entity.WatchedAttributes.GetString("guardedPlayerUid");
    //        if (uid != null)
    //        {
    //            Entity player = entity.World.PlayerByUid(uid)?.Entity;
    //            if (player == null) return;
    //            int guards = player.WatchedAttributes.GetAsInt("guards");
    //            if (guards > 0) player.WatchedAttributes.SetInt("guards", guards - 1);
    //        }
    //    }

    //    public override void OnEntitySpawn()
    //    {
    //        base.OnEntitySpawn();
    //        entity.World.RegisterCallback((float dt) =>
    //        {
    //            string uid = entity.WatchedAttributes.GetString("guardedPlayerUid");
    //            if (uid != null)
    //            {
    //                Entity player = entity.World.PlayerByUid(uid)?.Entity;
    //                if (player == null) return;
    //                int guards = player.WatchedAttributes.GetAsInt("guards");
    //                player.WatchedAttributes.SetInt("guards", guards + 1);
    //            }
    //        }, 1);
    //    }
    //}

//    [HarmonyPatch(typeof(AiTaskJealousSeekEntity))]
//    public class AiTaskJealousSeekEntityPatch
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch("ShouldExecute")]
//        public static bool ShouldExecutePrefix(AiTaskJealousSeekEntity __instance, ref bool __result)
//        {
//            if (__instance.TargetEntity != null) return true;
//            __result = false;

//            Entity guardedEntity = __instance.GetGuardedEntity();
//            if (guardedEntity == null) return false;
//            float chance = 0.01f;
//            if (__instance.entity.World.Rand.NextDouble() < chance)
//            {
//                return true;
//            }

//            return false;
//        }
//    }

//    [HarmonyPatch(typeof(AiTaskJealousMeleeAttack))]
//    public class AiTaskJealousMeleeAttackPatch
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch("ShouldExecute")]
//        public static bool ShouldExecutePrefix(AiTaskJealousMeleeAttack __instance, ref bool __result)
//        {
//            if (__instance.TargetEntity != null) return true;
//            __result = false;

//            Entity guardedEntity = __instance.GetGuardedEntity();
//            if (guardedEntity == null) return false;
//            float chance = 0.01f;
//            if(__instance.entity.World.Rand.NextDouble() < chance)
//            {
//                return true;
//            }

//            return false;
//        }
//    }
//}
