using System;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace XLib.XLeveling
{
    /// <summary>
    /// A book that can be used to gain some experience for a specific skill.
    /// Uses the attributes "skill" and "experience".
    /// </summary>
    public class ItemSkillBook : Item
    {
        /// <summary>
        /// XLeveling mod system
        /// </summary>
        protected XLeveling system;

        /// <summary>
        /// When the player has begun using this item for attacking (left mouse click).
        /// Sets the attributes "skill" and "experience" randomly if not set yet.
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="handling"></param>
        public override void OnHeldAttackStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandHandling handling)
        {
            base.OnHeldAttackStart(slot, byEntity, blockSel, entitySel, ref handling);
            ItemStack stack = slot.Itemstack;

            string skillName = stack?.Attributes.GetString("skill");
            if (skillName != null) return;
            handling = EnumHandHandling.Handled;

            if (api is ServerCoreAPI)
            {
                Skill skill = system.SkillSetTemplate[byEntity.World.Rand.Next(0, system.SkillSetTemplate.Count)];
                float exp = skill.ExpBase * 0.5f;
                stack.Attributes.SetString("skill", skill.Name);
                stack.Attributes.SetFloat("experience", exp);
                slot.MarkDirty();
            }
        }

        /// <summary>
        /// Server Side: Called one the collectible has been registered Client Side: Called
        /// once the collectible has been loaded from server packet
        /// </summary>
        /// <param name="api"></param>
        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            system = XLeveling.Instance(api);
        }

        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the item stack name that is getting displayed.
        /// </summary>
        /// <param name="itemStack"></param>
        /// <returns></returns>
        public override string GetHeldItemName(ItemStack itemStack)
        {
            string skillName = itemStack.Attributes.GetString("skill");
            float exp = (float)itemStack.Attributes.GetDecimal("experience");
            string knowledge = itemStack.Attributes.GetString("knowledge");

            if (knowledge != null)
            {
                string[] strings = knowledge.Split(':');
                string name;
                if (strings.Length == 2) 
                    name = Lang.GetIfExists(strings[0] + ":book-" + strings[1]);
                else 
                    name = Lang.GetIfExists("book-" + knowledge);
                if (name != null) return name;
            }

            Skill skill = system.GetSkill(skillName);
            if (skill == null)
                return Lang.Get("game:item-" + this.Code.Path.Replace("skill", ""));
            return skill.DisplayName + ": " + exp.ToString("0.00");
        }

        /// <summary>
        /// Called by the inventory system when you hover over an item stack. This is the text that is getting displayed.
        /// </summary>
        /// <param name="inSlot"></param>
        /// <param name="dsc"></param>
        /// <param name="world"></param>
        /// <param name="withDebugInfo"></param>
        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            string skillName = inSlot.Itemstack.Attributes.GetString("skill");
            float exp = (float)inSlot.Itemstack.Attributes.GetDecimal("experience");
            string knowledge = inSlot.Itemstack.Attributes.GetString("knowledge");
            Skill skill = system.GetSkill(skillName);

            if (skill != null && exp != 0.0f)
                dsc.AppendLine(Lang.Get("xlib:skillbook-dsc", skill.DisplayName, exp));
            if (knowledge != null) 
                dsc.AppendLine(Lang.Get("xlib:skillbook-dsc2", Lang.Get(knowledge)));
        }

        /// <summary>
        /// Called when the player right clicks while holding this block/item in his hands
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <param name="firstEvent">
        /// True when the player pressed the right mouse button on this block. 
        /// Every subsequent call, while the player holds right mouse down will be false, 
        /// it gets called every second while right mouse is down</param>
        /// <param name="handling">Whether or not to do any subsequent actions. If not set or set to NotHandled, the action will not called on the server.</param>
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

            if (!slot.Empty)
            {
                byEntity.World.RegisterCallback((dt) => playEatSound(byEntity, "eat", 1), 500);
                byEntity.AnimManager?.StartAnimation("eat");
                handling = EnumHandHandling.PreventDefault;
            }
        }

        /// <summary>
        /// Called every frame while the player is using this collectible. Return false to stop the interaction.
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        /// <returns>False if the interaction should be stopped. True if the interaction should continue</returns>
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStep(secondsUsed, slot, byEntity, blockSel, entitySel);

            if (byEntity.World is IClientWorldAccessor)
            {
                return secondsUsed <= 1f;
            }
            return true;
        }

        /// <summary>
        /// Called when the player successfully completed the using action, always called once an interaction is over.
        /// Consumes the book and grants the player the experience.
        /// </summary>
        /// <param name="secondsUsed"></param>
        /// <param name="slot"></param>
        /// <param name="byEntity"></param>
        /// <param name="blockSel"></param>
        /// <param name="entitySel"></param>
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
            byEntity.AnimManager?.StopAnimation("eat");

            if (byEntity.World is IServerWorldAccessor && secondsUsed >= 0.95f)
            {
                IPlayer player = (byEntity as EntityPlayer)?.Player;
                if (player == null) return;

                ItemStack stack = slot.TakeOut(1);
                slot.MarkDirty();

                string skillName = stack.Attributes.GetString("skill");
                float exp = (float)stack.Attributes.GetDecimal("experience");
                string knowledge = stack.Attributes.GetString("knowledge");

                XLeveling system = XLeveling.Instance(api);
                Skill skill = system?.GetSkill(skillName);

                PlayerSkillSet skillSet = byEntity.GetBehavior<PlayerSkillSet>();
                PlayerSkill playerSkill = skill != null ? skillSet?[skill.Id] : null;
                if (exp != 0.0f) playerSkill?.AddExperience(exp, false);
                if (knowledge != null)
                {
                    skillSet.Knowledge.TryGetValue(knowledge, out int value);
                    (XLeveling.Instance(api)?.IXLevelingAPI as XLevelingServer)?.SetPlayerKnowledge(player, knowledge, value + 1);
                }
            }
        }
    }
}
