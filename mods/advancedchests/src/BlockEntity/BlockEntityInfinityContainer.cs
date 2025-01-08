using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace AdvancedChests
{
    /// <summary>
    /// A chest where time flows backwards.
    /// </summary>
    /// <seealso cref="Vintagestory.GameContent.BlockEntityLabeledChest" />
    public class BlockEntityInfinityContainer : BlockEntityLabeledChest
    {
        /// <summary>
        /// Gets or sets the last update in total in game hours.
        /// </summary>
        /// <value>
        /// The last update.
        /// </value>
        public double LastUpdate { get; protected set; }

        /// <summary>
        /// Gets or sets the repair per hour.
        /// </summary>
        /// <value>
        /// The repair per hour.
        /// </value>
        public float RepairPerHour { get; set; }

        /// <summary>
        /// Initializes the chest entity.
        /// </summary>
        /// <param name="api">The API.</param>
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            LastUpdate = (int)api.World.Calendar.TotalHours;

            RepairPerHour = api.ModLoader.GetModSystem<AdvancedChestsSystem>()?.Config.infinityChestRepairPerHour ?? 0.5f;
            Inventory.OnAcquireTransitionSpeed += GetPerishRate;
        }

        /// <summary>
        /// Gets the perish rate.
        /// </summary>
        /// <returns></returns>
        public float GetPerishRate(EnumTransitionType transType, ItemStack stack, float baseMul)
        {
            return -0.1f;
        }

        /// <summary>
        /// Called every few seconds.
        /// Repairs items.
        /// </summary>
        /// <param name="dt">The dt.</param>
        protected override void OnTick(float dt)
        {
            base.OnTick(dt);

            double hoursPast = (int)(Api.World.Calendar.TotalHours - LastUpdate);
            int repair = (int)(hoursPast * RepairPerHour);
            if (repair < 1) return;

            LastUpdate += repair / RepairPerHour;

            foreach (ItemSlot slot in Inventory)
            {
                if (slot.Itemstack == null) continue;
                int max = slot.Itemstack.Collectible.GetMaxDurability(slot.Itemstack);
                int remining = slot.Itemstack.Collectible.GetRemainingDurability(slot.Itemstack);

                if (remining >= max) continue;
                slot.Itemstack.Collectible.DamageItem(Api.World, null, slot, Math.Max(-repair, remining - max));
            }
        }

        /// <summary>
        /// Converts to treeattributes.
        /// </summary>
        /// <param name="tree">The tree.</param>
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetDouble("lastUpdate", LastUpdate);
        }

        /// <summary>
        /// Creates an inventory from an attribute tree.
        /// </summary>
        /// <param name="tree">The tree.</param>
        /// <param name="worldForResolving">The world for resolving.</param>
        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            LastUpdate = tree.GetDouble("lastUpdate", LastUpdate);
        }
    }//!class BlockEntityInfinityChest
}//!namespace AdvancedChests
