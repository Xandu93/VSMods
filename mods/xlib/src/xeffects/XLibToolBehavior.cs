using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace XLib.XEffects
{
    /// <summary>
    /// Is used for the momentum effect.
    /// </summary>
    /// <seealso cref="Vintagestory.API.Common.CollectibleBehavior" />
    public class XLibToolBehavior : CollectibleBehavior
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XLibToolBehavior"/> class.
        /// </summary>
        /// <param name="collObj">The collectible object.</param>
        public XLibToolBehavior(CollectibleObject collObj) : base(collObj)
        {}

        /// <summary>
        /// Multiplies resulted mining speed of the item by return value if 'bhHandling' is not equal to 'PassThrough'.
        /// If 'bhHandling' is not set to 'PreventDefault', the mining speed will be multiplied by standard item mining speed.
        /// </summary>
        /// <param name="itemstack"></param>
        /// <param name="blockSel"></param>
        /// <param name="block"></param>
        /// <param name="forPlayer"></param>
        /// <param name="bhHandling"></param>
        /// <returns>
        /// Mining speed multiplier
        /// </returns>
        public override float OnGetMiningSpeed(IItemStack itemstack, BlockSelection blockSel, Block block, IPlayer forPlayer, ref EnumHandling bhHandling)
        {
            if (collObj.Tool == null) return 1.0f;
            AffectedEntityBehavior affected = forPlayer?.Entity?.GetBehavior<AffectedEntityBehavior>();
            if (affected == null) return 1.0f;
            float value = affected.GetMiningSpeedMultiplier(collObj.Tool.Value);
            if (value > 0.0f)
            {
                bhHandling = EnumHandling.Handled;
                return value;
            }
            return 1.0f;
        }
    }//!class XLibTool
}//!XLib.XEffects
