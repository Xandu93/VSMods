using Vintagestory.API.Common;
using Vintagestory.Common;
using XLib.XLeveling;

namespace XSkills
{
    public class XSkillInventoryBackpack : InventoryPlayerBackPacks
    {
        protected Cooking cooking;

        public XSkillInventoryBackpack(string inventoryId, ICoreAPI api) : base(inventoryId, api) { }
        public XSkillInventoryBackpack(string className, string playerUID, ICoreAPI api) : base(className, playerUID, api) { }

        public override int Count
        {
            get 
            {
                XSkillsPlayerInventory inv = Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;
                if (inv?.Linked ?? false) return base.Count + inv.Count;
                else return base.Count;
            }
        }

        public override ItemSlot this[int slotId] 
        {
            get 
            {
                int count = bagInv.Count + bagInv.BagSlots.Length;
                //int count = backPackContents.Count + backPackSlots.Length;
                if (slotId >= count)
                {
                    int tempId = slotId - count;
                    XSkillsPlayerInventory inv = Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;

                    if (inv != null)
                    {
                        if (tempId < inv.Count && inv.Linked)
                        {
                            return inv[tempId];
                        }
                    }
                }
                return base[slotId];
            }
            set
            {
                int count = bagInv.Count + bagInv.BagSlots.Length;
                //int count = backPackContents.Count + backPackSlots.Length;
                if (slotId >= count)
                {
                    int tempId = slotId - count;
                    XSkillsPlayerInventory inv = Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;

                    if (inv != null)
                    {
                        if (tempId < inv.Count && inv.Linked)
                        {
                            inv[tempId] = value;
                            return;
                        }
                    }
                }
                base[slotId] = value;
            }
        }

        public override object ActivateSlot(int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op)
        {
            int count = bagInv.Count + bagInv.BagSlots.Length;
            //int count = backPackContents.Count + backPackSlots.Length;
            if (slotId >= count && op.ShiftDown)
            {
                int tempId = slotId - count;
                XSkillsPlayerInventory inv = Player?.InventoryManager.GetOwnInventory("xskillshotbar") as XSkillsPlayerInventory;

                if (inv != null)
                {
                    if (tempId < inv.Count && inv.Linked)
                    {
                        return inv.ActivateSlot(tempId, sourceSlot, ref op);
                    }
                }
            }
            return base.ActivateSlot(slotId, sourceSlot, ref op);
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
    }//!class XSkillInventoryBackpack
}//!namespace XSkills
