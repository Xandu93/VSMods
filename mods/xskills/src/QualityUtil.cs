using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace XSkills
{
    /// <summary>
    /// Small helper class to save the quality state.
    /// </summary>
    public class QualityState
    {
        /// <summary>
        /// The quality
        /// </summary>
        public float quality;

        /// <summary>
        /// The old quantity
        /// </summary>
        public float oldQuantity;

        /// <summary>
        /// The old quality
        /// </summary>
        public float oldQuality;
    }//!class QualityState

    /// <summary>
    /// Contains some quality related methods.
    /// </summary>
    public class QualityUtil
    {
        /// <summary>
        /// Gets the quality.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <returns></returns>
        public static float GetQuality(ItemSlot slot)
        {
            if (slot == null) return 0.0f;
            return GetQuality(slot.Itemstack);
        }

        /// <summary>
        /// Gets the quality.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <returns></returns>
        public static float GetQuality(ItemStack stack)
        {
            if (stack == null) return 0.0f;
            return stack.Attributes.GetFloat("quality", 0.0f);
        }

        /// <summary>
        /// Gets the quality for a placed block.
        /// </summary>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        /// <returns></returns>
        public static float GetQuality(IWorldAccessor world, BlockPos pos)
        {
            BlockEntityCookedContainer bec = world.BlockAccessor.GetBlockEntity(pos) as BlockEntityCookedContainer;
            if (bec == null) return 0.0f;
            return GetQuality(bec.Inventory[0]?.Itemstack);
        }

        /// <summary>
        /// Adds the quality string.
        /// </summary>
        /// <param name="inSlot">The slot.</param>
        /// <param name="dsc">The string builder.</param>
        public static void AddQualityString(ItemSlot slot, StringBuilder dsc)
        {
            AddQualityString(GetQuality(slot), dsc);
        }

        /// <summary>
        /// Adds the quality string to a string builder.
        /// </summary>
        /// <param name="quality">The quality.</param>
        /// <param name="dsc">The string builder.</param>
        public static void AddQualityString(float quality, StringBuilder dsc)
        {
            if (quality > 0.0f)
            {
                string str = QualityString(quality);
                dsc.AppendLine(str);
            }
        }

        /// <summary>
        /// Picks the quality from a BlockEntityCookedContainer at a position
        /// and transfers it into a stack.
        /// </summary>
        /// <param name="stack">The stack.</param>
        /// <param name="world">The world.</param>
        /// <param name="pos">The position.</param>
        public static void PickQuality(ItemStack stack, IWorldAccessor world, BlockPos pos)
        {
            float quality = GetQuality(world, pos);
            if (quality <= 0.0f) return;
            stack.Attributes.SetFloat("quality", quality);
        }

        /// <summary>
        /// Gets the type of the quality for a collectible.
        /// Types can be: "tool", "armor", "weapon"
        /// </summary>
        /// <param name="collectible">The collectible.</param>
        /// <returns></returns>
        public static string GetQualityType(CollectibleObject collectible)
        {
            if (collectible == null) return null;
            int type = collectible.Attributes?["qualityType"]?.AsInt(-1) ?? -1;
            if (type == -1)
            {
                switch (collectible.Tool)
                {
                    case EnumTool.Chisel:
                    case EnumTool.Shears:
                    case EnumTool.Wrench:
                        type = 0;
                        break;
                }
            }
            if (type < 0)
            {
                if (collectible is ItemWearableAttachment) type = 1;
                else return null;
            }
            string str = null;
            switch (type)
            {
                case 0:
                    str = "tool";
                    break;
                case 1:
                    str = "armor";
                    break;
                case 2:
                    str = "weapon";
                    break;
            }
            return str;
        }

        /// <summary>
        /// Converts the quality to a string representing its value.
        /// </summary>
        /// <param name="quality">The quality.</param>
        /// <param name="formatted">if set to <c>true</c> the string will contain some code to format.</param>
        /// <returns></returns>
        public static string QualityString(float quality, bool formatted = true)
        {
            if (quality > 0.0f)
            {
                if (formatted)
                {
                    if (quality < 1.0f) return string.Format("<font color=\"gray\">" + Lang.Get("xskills:quality-bad") + "({0:N2})</font>", quality);
                    else if (quality < 2.0f) return string.Format("<font color=\"white\">" + Lang.Get("xskills:quality-common") + "({0:N2})</font>", quality);
                    else if (quality < 4.0f) return string.Format("<font color=\"green\">" + Lang.Get("xskills:quality-uncommon") + "({0:N2})</font>", quality);
                    else if (quality < 6.0f) return string.Format("<font color=\"blue\">" + Lang.Get("xskills:quality-rare") + "({0:N2})</font>", quality);
                    else if (quality < 8.0f) return string.Format("<font color=\"orange\">" + Lang.Get("xskills:quality-epic") + "({0:N2})</font>", quality);
                    else return string.Format("<font color=\"red\">" + Lang.Get("xskills:quality-legendary") + "({0:N2})</font>", quality);
                }
                else
                {
                    if (quality < 1.0f) return string.Format(Lang.Get("xskills:quality-bad") + "({0:N2})", quality);
                    else if (quality < 2.0f) return string.Format(Lang.Get("xskills:quality-common") + "({0:N2})", quality);
                    else if (quality < 4.0f) return string.Format(Lang.Get("xskills:quality-uncommon") + "({0:N2})", quality);
                    else if (quality < 6.0f) return string.Format(Lang.Get("xskills:quality-rare") + "({0:N2})", quality);
                    else if (quality < 8.0f) return string.Format(Lang.Get("xskills:quality-epic") + "({0:N2})", quality);
                    else return string.Format(Lang.Get("xskills:quality-legendary") + "({0:N2})", quality);
                }
            }
            return "";
        }
    }//!class QualityUtil
}//!namespace XSkills
