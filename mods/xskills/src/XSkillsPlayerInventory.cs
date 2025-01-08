using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Common;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillsPlayerInventory : InventoryBasePlayer
    {
        public static string BackgroundColor { get; set; } = "#BEBEBE";
        protected Cooking cooking;

        ItemSlot[] slots;
        ItemSlot[] buffer;

        //true if the inventory is linked with the backpack. Just for player corpse compatibility
        public bool Linked { get; internal set; }

        public string SwitchWithName { get; set; }

        public float SwitchCD { get; set; }

        public double LastSwitch { get; set; }

        public override int Count
        {
            get => slots.Length;
        }

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0) return null;
                if (slotId >= Count)
                {
                    if(buffer.Length <= slotId) SetBufferSize(slotId + 1);
                    return buffer[slotId];
                }
                return slots[slotId];
            }
            set
            {
                if (slotId >= Count || Count < 0) return;
                slots[slotId] = value;
            }
        }
        public XSkillsPlayerInventory(string className, string playerUID, ICoreAPI api) : base(className, playerUID, api)
        {
            if (className == null)
            {
                this.className = "xskillshotbar";
            }
            baseWeight = 0.8f;
            SwitchWithName = "hotbar";
            this.slots = new ItemSlot[0];
            this.buffer = new ItemSlot[0];
            this.Linked = true;
            this.LastSwitch = 0;
            this.SwitchCD = 3.0f;

            for (int ii = 0; ii < Count; ii++)
            {
                this.slots[ii] = new ItemSlot(this);
                this.slots[ii].HexBackgroundColor = BackgroundColor;
            }
        }

        public XSkillsPlayerInventory(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            if (this.className == null)
            {
                this.className = "xskillshotbar";
            }
            SwitchWithName = "hotbar";
            this.slots = new ItemSlot[0];
            this.buffer = new ItemSlot[0];
            this.Linked = true;
            this.LastSwitch = 0;
            this.SwitchCD = 3.0f;

            for (int ii = 0; ii < Count; ii++)
            {
                this.slots[ii] = new ItemSlot(this);
                this.slots[ii].HexBackgroundColor = BackgroundColor;
            }
        }

        public override void DidModifyItemSlot(ItemSlot slot, ItemStack extractedStack = null)
        {
            int slotId = GetSlotId(slot);

            if (slotId < 0) return;

            MarkSlotDirty(slotId);
            OnItemSlotModified(slot);
            slot.Itemstack?.Collectible?.OnModifiedInInventorySlot(Api.World, slot, extractedStack);
        }

        public void SetSize(int size)
        {
            ItemSlot[] old = this.slots;
            this.slots = new ItemSlot[size];
            for (int ii = 0; ii < Count; ii++)
            {
                this.slots[ii] = new ItemSlot(this);
                this.slots[ii].HexBackgroundColor = BackgroundColor;

                if (ii < old.Length)
                {
                    this.slots[ii].Itemstack = old[ii].Itemstack;
                    this.slots[ii].MarkDirty();
                }
            }

            IWorldAccessor world = this.Api.World;
            IPlayer player = world?.PlayerByUid(this.playerUID);
            if (player?.Entity == null) return;

            for (int jj = Count; jj < old.Length; jj++)
            {
                ItemSlot slot = old[jj];
                if (slot.Itemstack != null)
                {
                    this.Api.World.SpawnItemEntity(slot.Itemstack, player.Entity.Pos.XYZ);
                    slot.Itemstack = null;
                }
            }

            for (int jj = 0; jj < buffer.Length; jj++)
            {
                if(buffer[jj].Itemstack != null)
                {
                    if (jj >= Count || slots[jj].Itemstack != null)
                    {
                        this.Api.World.SpawnItemEntity(buffer[jj].Itemstack, player.Entity.Pos.XYZ);
                        buffer[jj].Itemstack = null;
                        continue;
                    }
                    slots[jj].Itemstack = buffer[jj].Itemstack;
                    slots[jj].MarkDirty();
                }
            }
            SetBufferSize(0);
        }

        private void SetBufferSize(int size)
        {
            ItemSlot[] old = this.buffer;
            this.buffer = new ItemSlot[size];
            for (int ii = 0; ii < buffer.Length; ii++)
            {
                this.buffer[ii] = new ItemSlot(this);
                this.buffer[ii].HexBackgroundColor = BackgroundColor;

                if (ii < old.Length)
                {
                    this.buffer[ii].Itemstack = old[ii].Itemstack;
                }
            }
        }

        public override void OnOwningEntityDeath(Vec3d pos)
        {
            Survival survival = XLeveling.Instance(Api)?.GetSkill("survival") as Survival;
            if (survival != null)
            {
                PlayerAbility playerAbility = Api.World.PlayerByUid(this.playerUID)?.Entity?.GetBehavior<PlayerSkillSet>()?[survival.Id]?[survival.SoulboundBagId];
                if (playerAbility?.Tier > 0)
                {
                    return;
                }
            }
            base.OnOwningEntityDeath(pos);
        }

        public void SwitchInventories()
        {
            if (this.Api.World == null) return;
            double totalHours = this.Api.World.Calendar.TotalHours;
            if ((this.LastSwitch + SwitchCD / 360.0) > totalHours) return;
            this.LastSwitch = totalHours;

            IPlayer player = this.Api.World.PlayerByUid(this.playerUID);
            if (player?.Entity == null) return;
            IInventory toSwitch = player.InventoryManager.GetOwnInventory(this.SwitchWithName);
            if (toSwitch == null) return;

            int max = Math.Min(this.Count, toSwitch.Count);
            for (int ii = 0; ii < max; ii++)
            {
                ItemSlot source = toSwitch[ii];
                object obj = this.TryFlipItems(ii, source);
                (this.Api as ICoreClientAPI)?.Network.SendPacketClient(obj);
                source.MarkDirty();
                this[ii].MarkDirty();
            }

            if (buffer.Length > 0)
            {
                for (int ii = 0; ii < buffer.Length; ii++)
                {
                    ItemSlot slot = buffer[ii];
                    if (slot.Itemstack != null)
                    {
                        this.Api.World.SpawnItemEntity(slot.Itemstack, player.Entity.Pos.XYZ);
                        slot.Itemstack = null;
                    }
                }
                SetBufferSize(0);
            }
        }

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            float suitability = base.GetSuitability(sourceSlot, targetSlot, isMerge);
            float num2 = 0.0f;
            if (sourceSlot.Inventory is CreativeInventoryTab)
            {
                return 0.0f;
            }
            else if (sourceSlot.Inventory is InventoryGeneric)
            {
                ItemStack itemstack = sourceSlot.Itemstack;
                if (itemstack == null || itemstack.Collectible.Tool == null)
                {
                    num2 = 1.0f;
                }
            }
            return (suitability + num2) + ((sourceSlot is ItemSlotOutput || sourceSlot is ItemSlotCraftingOutput) ? 1.0f : 0.0f);
        }

        public override float GetTransitionSpeedMul(EnumTransitionType transType, ItemStack stack)
        {
            float baseValue = base.GetTransitionSpeedMul(transType, stack);

            if (cooking == null)
            {
                cooking = XLeveling.Instance(this.Api)?.GetSkill("cooking") as Cooking;
                if (cooking == null) return baseValue;
            }

            PlayerAbility playerAbility = this.Player.Entity?.GetBehavior<PlayerSkillSet>()?[cooking.Id]?[cooking.SaltyBackpackId];
            if (playerAbility == null) return baseValue;

            if (transType != EnumTransitionType.Perish) return baseValue;
            else return baseValue * playerAbility.FValue(0, 1.0f);
        }

        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            int size = tree.GetAsInt("qslots", 0);
            SetBufferSize(size);

            buffer = SlotsFromTreeAttributes(tree, buffer);
            if (this.slots == null) this.slots = new ItemSlot[0];
            if (this.buffer == null) this.buffer = new ItemSlot[0];
            for (int ii = 0; ii < buffer.Length; ii++)
            {
                buffer[ii].HexBackgroundColor = BackgroundColor;
            }
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            if(slots.Length <= 0 && buffer.Length > 0) SlotsToTreeAttributes(buffer, tree);
            else SlotsToTreeAttributes(slots, tree);
        }
    }//!class XSkillsPlayerInventory
}//!namespace XSkills
