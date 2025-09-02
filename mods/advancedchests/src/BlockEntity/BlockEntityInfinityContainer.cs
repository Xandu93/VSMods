using System;
using System.Text;
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

            LastUpdate += repair / RepairPerHour;

            foreach (ItemSlot slot in Inventory)
            {
                ItemStack stack = slot.Itemstack;
                if (stack == null) continue;
                CollectibleObject collectible = stack.Collectible;

                if (repair > 0)
                {
                    int max = collectible.GetMaxDurability(stack);
                    int remining = collectible.GetRemainingDurability(stack);

                    if (remining < max)
                    {
                        collectible.DamageItem(Api.World, null, slot, Math.Max(-repair, remining - max));
                    }
                }

                //transition fix. transition should not be smaller than 0
                if (collectible.RequiresTransitionableTicking(Api.World, stack))
                {
                    TransitionState[] states = collectible.UpdateAndGetTransitionStates(Api.World, slot);
                    if (states == null) continue;
                    foreach (TransitionState state in states)
                    {
                        if(state.TransitionedHours < 0.0f)
                        {
                            collectible.SetTransitionState(stack, state.Props.Type, 0.0f);
                        }
                    }
                }
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
