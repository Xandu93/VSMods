using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace XLib.XLeveling
{
    /// <summary>
    /// Shows informations of an ability.
    /// </summary>
    /// <seealso cref="HudElement" />
    public class AbilityTooltip : HudElement
    {
        /// <summary>
        /// The gui text element that shows the name of the ability.
        /// </summary>
        private GuiElementDynamicText abilityNameText;

        /// <summary>
        /// The gui text element that shows the informations of the ability.
        /// </summary>
        private GuiElementRichtext abilityInfoText;

        /// <summary>
        /// Gets or sets the skill dialog.
        /// </summary>
        /// <value>
        /// The skill dialog.
        /// </value>
        internal SkillDialog SkillDialog { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityTooltip"/> class.
        /// </summary>
        /// <param name="capi">The vintage story core client api.</param>
        /// <param name="parentBounds">The parent bounds. Used to bound this hud to a dialog.</param>
        public AbilityTooltip(ICoreClientAPI capi, ElementBounds parentBounds) : base(capi)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithParent(parentBounds).WithAlignment(EnumDialogArea.RightMiddle).WithFixedOffset(240, 0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds nameBounds = ElementBounds.Fixed(0, 0, 200, 24);
            ElementBounds infoBounds = ElementBounds.Fixed(0, 28, 200, 200);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(nameBounds, infoBounds);

            SingleComposer = capi.Gui.CreateCompo("SkillTooltip", dialogBounds)
                .AddDialogBG(bgBounds, false)
                .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "AbilityName")
                .AddRichtext("", CairoFont.WhiteDetailText(), infoBounds, "AbilityInfo")
                .Compose();
            this.abilityNameText = SingleComposer.GetDynamicText("AbilityName");
            this.abilityInfoText = SingleComposer.GetRichtext("AbilityInfo");
        }

        /// <summary>
        /// Updates this hud to provide informations of the specified player ability.
        /// </summary>
        /// <param name="playerAbility">The player ability.</param>
        public void Update(PlayerAbility playerAbility)
        {
            List<RichTextComponent> components = new List<RichTextComponent>();
            this.abilityNameText.SetNewText(playerAbility.Ability.DisplayName);
            StringBuilder sb = new StringBuilder();

            double[] green = { 0.0, 0.7, 0.0 };
            double[] red = { 0.8, 0.0, 0.0 };
            double[] gray = { 0.5, 0.5, 0.5 };

            //shows the current tier if exists
            if (playerAbility.Tier > 0) sb.Append(playerAbility.Ability.FormattedDescription(playerAbility.Tier));

            //shows the next tier if exists
            if (playerAbility.Tier < playerAbility.Ability.MaxTier)
            {
                if (playerAbility.Tier > 0) sb.Append("\n\n");
                sb.Append(Lang.GetUnformatted("xlib:nexttier"));
                sb.Append(playerAbility.Ability.FormattedDescription(playerAbility.Tier + 1));
            }

            //tier
            sb.Append("\n");
            sb.Append(Lang.GetUnformatted("xlib:currenttier"));
            sb.Append(playerAbility.Tier);
            sb.Append("/");
            sb.Append(playerAbility.Ability.MaxTier);

            components.Add(new RichTextComponent(capi, sb.ToString(), CairoFont.WhiteDetailText()));
            string str;

            if (playerAbility.Tier < playerAbility.Ability.MaxTier)
            {
                //colored required level
                int levelRequired = playerAbility.Ability.RequiredLevel(playerAbility.Tier + 1);
                str = Lang.GetUnformatted("xlib:requiredlevel") + levelRequired;
                if (levelRequired <= playerAbility.PlayerSkill.Level)
                {
                    components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(green)));
                }
                else
                {
                    components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(red)));
                }

                // add placeholder
                int placeholderID = components.Count;
                if (playerAbility.Ability.Requirements.Count > 0) components.Add(null);

                //colored requirements
                bool allFulfilled = true;
                double[] color;
                foreach (Requirement requirement in playerAbility.Ability.Requirements)
                {
                    
                    str = "\n" + requirement.Description(playerAbility);
                    if(requirement.MinimumTier > playerAbility.Tier + 1)
                    {
                        color = gray;
                    }
                    else if (requirement.IsFulfilled(playerAbility, Math.Min(playerAbility.Tier + 1, playerAbility.Ability.MaxTier)))
                    {
                        color = green;
                    }
                    else
                    {
                        color = red;
                        allFulfilled = false;
                    }
                    components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(color)));
                }
                if (playerAbility.Ability.Requirements.Count > 0)
                {
                    string requirementsStr = "\n" + Lang.GetUnformatted("xlib:requirements") + ":";
                    if (allFulfilled)
                    {
                        components[placeholderID] = new RichTextComponent(capi, requirementsStr, CairoFont.WhiteDetailText().WithColor(green));
                    }
                    else
                    {
                        components[placeholderID] = new RichTextComponent(capi, requirementsStr, CairoFont.WhiteDetailText().WithColor(red));
                    }
                }
            }

            //click tooltip
            PlayerSkillSet playerSkillSet = playerAbility.PlayerSkill.PlayerSkillSet;
            if (SkillDialog.Unlearn && playerAbility.Tier > 0)
            {
                str = Lang.Get("xlib:clicktodecreasetier", SkillDialog.Client.GetPointsForUnlearn(), playerSkillSet.UnlearnPoints, SkillDialog.Client.Config.unlearnCooldown, playerSkillSet.UnlearnCooldown / 60.0f);
                components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(red)));

                if (playerSkillSet.UnlearnPoints < SkillDialog.Client.GetPointsForUnlearn())
                {
                    str = Lang.Get("xlib:notenoughpoints");
                    components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(red)));
                }
            }
            else if (playerAbility.Tier < playerAbility.Ability.MaxTier)
            {
                str = Lang.Get("xlib:clicktoincreasetier", 1, playerAbility.PlayerSkill.AbilityPoints);
                components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(green)));

                if (playerAbility.PlayerSkill.AbilityPoints == 0)
                {
                    str = Lang.Get("xlib:notenoughpoints");
                    components.Add(new RichTextComponent(capi, str, CairoFont.WhiteDetailText().WithColor(red)));
                }
            }

            abilityInfoText.SetNewText(components.ToArray());
            SingleComposer.ReCompose();
        }
    }//!class AbilityTooltip
}//!namespace XLib.XLeveling 
