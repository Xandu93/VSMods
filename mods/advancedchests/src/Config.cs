using ProtoBuf;
using System.Collections.Generic;
using System.ComponentModel;

namespace AdvancedChests
{
    /// <summary>
    /// The configuration for the AdvancedChests mod.
    /// </summary>
    [ProtoContract]
    public class Config
    {
        /// <summary>
        /// Enables coffins
        /// </summary>
        [ProtoMember(1)]
        [DefaultValue(true)]
        public bool enableCoffin = true;

        /// <summary>
        /// Enables personal containers
        /// </summary>
        [ProtoMember(2)]
        [DefaultValue(true)]
        public bool enablePeronalContainer = true;

        /// <summary>
        /// Enables filter containers
        /// </summary>
        [ProtoMember(3)]
        [DefaultValue(true)]
        public bool enableFilterContainer = true;

        /// <summary>
        /// Enables shared containers
        /// </summary>
       [ProtoMember(4)]
       [DefaultValue(true)] 
       public bool enableSharedContainer = true;

        /// <summary>
        /// Enables sorting containers
        /// </summary>
        [ProtoMember(5)]
        [DefaultValue(true)] 
        public bool enableSortingContainer = true;

        /// <summary>
        /// Enables sorting containers
        /// </summary>
        [ProtoMember(6)]
        [DefaultValue(false)]
        public bool enableInfinityContainer = false;

        /// <summary>
        /// Enables sorting containers
        /// </summary>
        [ProtoMember(7)]
        [DefaultValue(false)] 
        public bool enableVoidContainer = false;

        /// <summary>
        /// The inventory size of the shared chest
        /// </summary>
        [ProtoMember(8)]
        [DefaultValue(16)]
        public int sharedChestSize = 16;

        /// <summary>
        /// The inventory size of the personal chest
        /// </summary>
        [ProtoMember(9)]
        [DefaultValue(16)]
        public int personalChestSize = 16;

        /// <summary>
        /// The maximal number of shared inventories
        /// </summary>
        [ProtoMember(10)]
        [DefaultValue(100)]
        public int maxSharedInventoryCount = 100;

        /// <summary>
        /// The amount of durability the infinity chest repairs per hour
        /// </summary>
        [ProtoMember(11)]
        [DefaultValue(0.5f)]
        public float infinityChestRepairPerHour = 0.5f;

        /// <summary>
        /// The names of the inventories considered for the coffin
        /// </summary>
        [ProtoMember(12)]
        [DefaultValue(null)]
        public HashSet<string> inventoryNames = new HashSet<string>();
    }//!class Config
}//!namespace AdvancedChests
