using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace XInvTweaks
{
    public class InvTweaksConfig
    {
        public int delay = 200;
        public int toolSwitchDurability = 0;
        public bool tools = true;
        public bool groundStorage = true;
        public bool seeds = true;
        public bool blocks = true;
        public bool stairs = true;
        public bool piles = true;
        public bool extendChestUi = true;
        public bool strgClick = true;
        //public bool survivalPick = true;
        public bool pushPullWheel = true;
        public bool crateSwitch = true;

        public Dictionary<string, int> BulkQuanties = new Dictionary<string, int>();
        public SortedSet<string> SortBlacklist = new SortedSet<string>();

        public List<string> SortOrder = new List<string>();
        public List<string> StackOrder = new List<string>();
        public List<EnumItemStorageFlags> StorageFlagsOrder = new List<EnumItemStorageFlags>();
        public Dictionary<string, int> Priorities = new Dictionary<string, int>();
        public SortedSet<int> LockedSlots = new SortedSet<int>();

        public InvTweaksConfig()
        {}
    }//!class ToolSwitchConfig
}//!namespace XInvTweaks
