using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents the dialog that holds all information for the 
    /// different skills and the associated abilities. Also Provides 
    /// an interface to choose and improve abilities.
    /// </summary>
    /// <seealso cref="GuiDialog" />
    public class SkillDialog : GuiDialog
    {
        /// <summary>
        /// The XLeveling client this dialog belongs to.
        /// </summary>
        public XLevelingClient Client { get; private set; }

        /// <summary>
        /// The calculated width of this dialog.
        /// </summary>
        public float Width { get; private set; }

        /// <summary>
        /// True if clicking on an ability button should decrease the tier of the ability.
        /// </summary>
        public bool Unlearn
        {
            get => this.unlearnToggle.On;
            set => this.unlearnToggle.On = value;
        }

        /// <summary>
        /// True if clicking on an ability button should reset the tier of an ability.
        /// </summary>
        public bool UnlearnAbility
        {
            get => this.unlearnAbilityToggle.On;
            set => this.unlearnAbilityToggle.On = value;
        }

        /// <summary>
        /// The horizontal tabs to choose a skill group.
        /// </summary>
        private GuiElementHorizontalTabs groupBar;

        /// <summary>
        /// The horizontal tabs to choose a skill.
        /// </summary>
        private GuiElementHorizontalTabs skillBar;

        /// <summary>
        /// This text field contains information about the currently chosen skill.
        /// </summary>
        private GuiElementRichtext skillinfo;

        /// <summary>
        /// A hud element that shows the ability tooltip.
        /// </summary>
        private AbilityTooltip abilityTooltip;

        /// <summary>
        /// The unlearn toggle button
        /// </summary>
        GuiElementToggleButton unlearnToggle;

        /// <summary>
        /// The unlearn ability toggle button
        /// </summary>
        GuiElementToggleButton unlearnAbilityToggle;

        /// <summary>
        /// The sparring toggle button
        /// </summary>
        GuiElementToggleButton sparringToggle;

        /// <summary>
        /// The skill groups.
        /// </summary>
        Dictionary<string, List<PlayerSkill>> groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkillDialog"/> class.
        /// </summary>
        /// <param name="client">The XLeveling client this dialog belongs to.</param>
        public SkillDialog(XLevelingClient client) : base(client.XLeveling.Api as ICoreClientAPI)
        {
            this.Client = client;
            this.OnOpened += this.OnOpen;
            this.OnClosed += this.OnClose;
            CreateGroups();
        }

        /// <summary>
        /// Creates the groups for the groups tab bar.
        /// </summary>
        protected virtual void CreateGroups()
        {
            groups = new Dictionary<string, List<PlayerSkill>>();
            foreach (PlayerSkill skill in Client.LocalPlayerSkillSet.PlayerSkills)
            {
                if (skill.Skill.Enabled && !skill.Hidden && skill.PlayerAbilities.Count > 0)
                {
                    if (!groups.ContainsKey(skill.Skill.Group))
                    {
                        groups[skill.Skill.Group] = new List<PlayerSkill>();
                    }
                    List<PlayerSkill> groupList = groups[skill.Skill.Group];
                    groupList.Add(skill);
                }
            }
            int maxCount = Math.Min(this.groups.Count, 4);
            foreach (List<PlayerSkill> skills in this.groups.Values)
            {
                maxCount = Math.Max(maxCount, skills.Count);
            }
            this.Width = maxCount * 100.0f;
        }

        /// <summary>
        /// Setups all gui elements of this dialog. This method is called every time a new tab was clicked to create the new buttons.
        /// </summary>
        public virtual void Setup()
        {
            CreateGroups();

            int yy = 20;
            int groupTabsActive = groupBar != null ? groupBar.activeElement : 0;
            int skillTabsActive = skillBar != null ? skillBar.activeElement : 0;
            if (groupTabsActive >= this.groups.Count) groupTabsActive = 0;

            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds groupTabBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 0, yy).WithFixedHeight(24).WithFixedWidth(this.Width);
            yy += 28;
            ElementBounds skillTabBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 0, yy).WithFixedHeight(24).WithFixedWidth(this.Width);
            yy += 28;
            ElementBounds textBounds = ElementBounds.Fixed(0, yy, 140, 200);
            yy += 4;
            ElementBounds unlearnBounds = ElementBounds.FixedOffseted(EnumDialogArea.LeftBottom, 0, -68, 120, 24);
            ElementBounds unlearnAbilityBounds = ElementBounds.FixedOffseted(EnumDialogArea.LeftBottom, 0, -40, 120, 24);
            ElementBounds sparringButton = ElementBounds.FixedOffseted(EnumDialogArea.LeftBottom, 0, -12, 120, 24);

            ElementBounds buttonBounds;
            AbilityButton button;
            PlayerSkill playerSkill;

            if (unlearnToggle == null)
            {
                unlearnToggle = new GuiElementToggleButton(
                    this.capi, null, 
                    Lang.GetUnformatted("xlib:unlearn"), 
                    CairoFont.WhiteDetailText(), 
                    (bool state) => { if (state) { unlearnAbilityToggle.On = false; } }, 
                    unlearnBounds, true);
            }
            else
            {
                unlearnToggle.Bounds = unlearnBounds;
            }

            if (unlearnAbilityToggle == null)
            {
                unlearnAbilityToggle = new GuiElementToggleButton(
                    this.capi, null, 
                    Lang.GetUnformatted("xlib:unlearnAbility"), 
                    CairoFont.WhiteDetailText(),
                    (bool state) => { if (state) { unlearnToggle.On = false; } },
                    unlearnAbilityBounds, true);
            }
            else
            {
                unlearnAbilityToggle.Bounds = unlearnAbilityBounds;
            }

            if (sparringToggle == null)
            {
                sparringToggle = new GuiElementToggleButton(this.capi, null, Lang.GetUnformatted("xlib:sparringmode"), CairoFont.WhiteDetailText(), OnSparringToggle, sparringButton, true);
            }
            else
            {
                sparringToggle.Bounds = sparringButton;
            }
            sparringToggle.On = this.Client.LocalPlayerSkillSet.Sparring;

            //creates a gui tab for each skill group
            GuiTab[] groupTabs = new GuiTab[this.groups.Keys.Count];
            List<PlayerSkill> activeList = null;
            int count = 0;
            foreach (string key in this.groups.Keys)
            {
                groupTabs[count] = new GuiTab();
                groupTabs[count].Name = key;
                groupTabs[count].DataInt = count;
                if (count == groupTabsActive)
                {
                    activeList = this.groups[key];
                }
                count++;
            }

            dialogBounds.BothSizing = ElementSizing.FitToChildren;
            dialogBounds.WithChild(bgBounds);

            bgBounds.horizontalSizing = ElementSizing.FitToChildren;
            bgBounds.verticalSizing = ElementSizing.Fixed;
            bgBounds.WithFixedHeight(yy + 8 * 36);
            bgBounds.WithChildren(groupTabBounds, skillTabBounds, textBounds, unlearnBounds, unlearnAbilityBounds, sparringButton);

            //creates a tab for each skill within the skill group
            GuiTab[] skillTabs = new GuiTab[activeList.Count];
            count = 0;
            foreach (PlayerSkill skill in activeList)
            {
                if (skill.Hidden) continue;
                skillTabs[count] = new GuiTab();
                if (skill.AbilityPoints <= 0)
                    skillTabs[count].Name = skill.Skill.DisplayName;
                else
                    skillTabs[count].Name = skill.Skill.DisplayName + " (" + skill.AbilityPoints + ")";
                skillTabs[count].DataInt = count;
                count++;
            }

            abilityTooltip = new AbilityTooltip(this.capi, dialogBounds);
            abilityTooltip.SkillDialog = this;

            SingleComposer = this.capi.Gui.CreateCompo("SkillDialog", dialogBounds)
                 .AddShadedDialogBG(bgBounds, true)
                 .AddDialogTitleBar(Lang.GetUnformatted("xlib:skills"), OnTitleBarCloseClicked)
                 .AddHorizontalTabs(groupTabs, groupTabBounds, OnGroupTabClicked, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "GroupTabs");

            SingleComposer.AddHorizontalTabs(skillTabs, skillTabBounds, OnSkillTabClicked, CairoFont.WhiteSmallText(), CairoFont.WhiteSmallText(), "SkillTabs");

            SingleComposer
                 .AddRichtext("", CairoFont.WhiteDetailText(), textBounds, "SkillInfo")
                 .AddInteractiveElement(unlearnToggle)
                 .AddInteractiveElement(unlearnAbilityToggle)
                 .AddInteractiveElement(sparringToggle);

            //creates a button for every ability that belongs to the chosen skill
            skillTabsActive = skillTabsActive >= activeList.Count ? 0 : skillTabsActive;
            playerSkill = activeList[skillTabsActive];
            int abilityCount = 0;
            foreach (PlayerAbility playerAbility in playerSkill.PlayerAbilities)
            {
                if (!playerAbility.IsVisible()) continue;
                abilityCount++;
                if (abilityCount > 7) bgBounds.verticalSizing = ElementSizing.FitToChildren;
                buttonBounds = ElementBounds.Fixed(144, yy, 200, 20);
                bgBounds.WithChild(buttonBounds);
                yy += 36;
                button = new AbilityButton(this, buttonBounds, abilityTooltip, playerAbility);
                SingleComposer.AddInteractiveElement(button);
            }

            SingleComposer.Compose();
            this.groupBar = SingleComposer.GetHorizontalTabs("GroupTabs");
            this.skillBar = SingleComposer.GetHorizontalTabs("SkillTabs");
            this.skillinfo = SingleComposer.GetRichtext("SkillInfo");
            this.groupBar.activeElement = groupTabsActive;
            this.skillBar.activeElement = skillTabsActive;
            float expBonus = playerSkill.Skill.GetExperienceMultiplier(playerSkill.PlayerSkillSet, false) - 1.0f;
            string strColor;
            if (expBonus < 0.0f) strColor = "(<font color=\"red\">";
            else if (expBonus > 0.0f) strColor = "(<font color=\"green\">";
            else strColor = "(<font color=\"yellow\">";

            this.skillinfo.SetNewText(
                Lang.GetUnformatted("xlib:skill") + ": " + playerSkill.Skill.DisplayName + "\n" +
                Lang.GetUnformatted("xlib:level") + ": " + playerSkill.Level.ToString() + "\n" +
                Lang.GetUnformatted("xlib:experience") + strColor + (expBonus * 100).ToString("+#;-#;+0") + "%</font>):\n" +
                playerSkill.Experience.ToString("N2") + "/" + playerSkill.RequiredExperience.ToString() + "\n" +
                Lang.GetUnformatted("xlib:abilitypoints") + ":\n" + playerSkill.AbilityPoints + "\n" +
                Lang.GetUnformatted("xlib:unlearnpoints") + ":\n" + playerSkill.PlayerSkillSet.UnlearnPoints.ToString("n2") + "/" +
                this.Client.GetPointsForUnlearn() + "\n" +
                Lang.GetUnformatted("xlib:unlearncooldown") + ":\n" + playerSkill.PlayerSkillSet.UnlearnCooldown.ToString("N2"), 
                CairoFont.WhiteDetailText());
        }

        /// <summary>
        /// Called when the close button on the title bar was clicked.
        /// </summary>
        private void OnTitleBarCloseClicked()
        {
            this.TryClose();
        }

        /// <summary>
        /// Called when a skill group tab was clicked.
        /// </summary>
        /// <param name="clicked">The clicked tab.</param>
        private void OnGroupTabClicked(int clicked)
        {
            this.groupBar.activeElement = clicked;
            this.Setup();
        }

        /// <summary>
        /// Called when a skill tab was clicked.
        /// </summary>
        /// <param name="clicked">The clicked tab.</param>
        private void OnSkillTabClicked(int clicked)
        {
            this.skillBar.activeElement = clicked;
            this.Setup();
        }

        /// <summary>
        /// Called every time this dialog opens.
        /// </summary>
        private void OnOpen()
        {
            this.Setup();
        }

        /// <summary>
        /// Called every time this dialog closes.
        /// </summary>
        private void OnClose()
        {
            this.abilityTooltip.TryClose();
        }

        /// <summary>
        /// Attempts to open this dialogue.
        /// </summary>
        /// <returns>
        /// Was this dialogue successfully opened?
        /// </returns>
        public override bool TryOpen()
        {
            if (this.groups.Count > 0)
            {
                return base.TryOpen();
            }
            return false;
        }

        /// <summary>
        /// Is triggered when the sparring button was pressed.
        /// </summary>
        /// <param name="toggle"></param>
        private void OnSparringToggle(bool toggle)
        {
            this.Client.LocalPlayerSkillSet.Sparring = toggle;
            CommandPackage package = new CommandPackage(EnumXLevelingCommand.SparringMode, toggle ? 1 : 0);
            this.Client.SendPackage(package);
        }

        /// <summary>
        /// The key combination string that toggles this GUI object.
        /// </summary>
        public override string ToggleKeyCombinationCode
        {
            get { return "skilldialoghotkey"; }
        }
    }//!class SkillDialog
}//!namespace xleveling
