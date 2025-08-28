using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using XLib.XLeveling;

namespace XSkills
{
    [HarmonyPatch(typeof(BlockLootVessel))]
    public class BlockLootVesselPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GetDrops")]
        public static void GetDropsPostfix(BlockLootVessel __instance, ref ItemStack[] __result, IWorldAccessor world)
        {
            string code = __instance.LastCodePart();
            float chance = (float)world.Rand.NextDouble();
            float experience = 1.0f;
            string skillName;
            string color;

            switch (code)
            {
                case "seed":
                    skillName = "farming";
                    chance = 0.5f;
                    break;

                case "food":
                    skillName = "cooking";
                    chance = 1.0f;
                    break;

                case "forage":
                    if (chance > 0.66f)
                        skillName = "survival";
                    else if (chance > 0.33f) 
                        skillName = "digging";
                    else 
                        skillName = "forestry";
                    chance = 3.0f;
                    break;

                case "ore":
                    skillName = "mining";
                    chance = 1.0f;
                    break;

                case "tool":
                    if (chance > 0.5f) 
                        skillName = "metalworking";
                    else 
                        skillName = "combat";
                    chance = 2.0f;
                    break;

                case "farming":
                    if (chance > 0.5f)
                    {
                        skillName = "farming";
                        chance = 0.5f;
                    }
                    else
                    {
                        skillName = "husbandry";
                        chance = 2.0f;
                    }
                    break;

                default:
                    return;
            }

            switch(skillName)
            {
                case "farming":
                    color = "darkgreen";
                    break;

                case "cooking":
                    color = "cherryred";
                    break;

                case "survival":
                    color = "olive";
                    break;

                case "digging":
                    color = "orange";
                    break;

                case "forestry":
                    color = "darkolive";
                    break;

                case "mining":
                    color = "darkgray";
                    break;

                case "metalworking":
                    color = "brickred";
                    break;

                case "combat":
                    color = "purpleorange";
                    break;

                case "husbandry":
                    color = "orangebrown";
                    break;

                case "pottery":
                    color = "darkbeige";
                    break;

                case "temporaladaptation":
                    color = "gray";
                    break;

                default:
                    return;
            }

            XLeveling xLeveling = XLeveling.Instance(world.Api);
            if (xLeveling == null) return;

            chance *= xLeveling.Config.skillBookChanceMult / 3.0f;
            if (chance < world.Rand.NextDouble()) return;

            chance = (float)world.Rand.NextDouble();
            if (chance > 0.90f) experience *= 1.00f;
            else if (chance > 0.60f) experience *= 0.50f;
            else if (chance > 0.30f) experience *= 0.25f;
            else experience *= 0.125f;

            AssetLocation asset = new AssetLocation("xlib", "skillbook-aged-" + color);
            Item book = world.GetItem(asset);
            if (book == null) return;

            Skill skill = xLeveling.GetSkill(skillName);
            if (skill == null) return;
            experience *= skill.ExpBase * xLeveling.Config.skillBookExpMult;

            ItemStack stack = new ItemStack(book, 1);
            stack.Attributes.SetString("skill", skillName);
            stack.Attributes.SetFloat("experience", experience);
            __result = __result.AddToArray(stack);
        }
    }
}