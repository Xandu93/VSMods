using Vintagestory.API.Common.Entities;

namespace XLib.XEffects
{
    /// <summary>
    /// XEffects helper class
    /// </summary>
    public static class XEffectsHelper
    {
        /// <summary>
        /// Adds the given effect to the given entity.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="effect">The effect.</param>
        public static void AddEffect(this Entity entity, Effect effect)
        {
            AffectedEntityBehavior affected = entity.GetBehavior<AffectedEntityBehavior>();
            if (affected == null) return; 
            affected.AddEffect(effect);
            affected.MarkDirty();
        }
    }//! class XEffectsHelper
}//! namespace XLib.XEffects
