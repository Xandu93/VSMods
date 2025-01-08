using System;
using Vintagestory.API.Client;

namespace XLib.XLeveling
{
    /// <summary>
    /// Represents a button for a player ability and allows to increase or decrease the tier of an ability.
    /// The button will manipulate the tier of the given ability when it is clicked.
    /// </summary>
    /// <seealso cref="GuiElementTextButton" />
    public class AbilityButton : GuiElementTextButton
    {
        /// <summary>
        /// Is true when the cursor hovers over the button
        /// </summary>
        public bool CursorHovers { get; private set; }

        /// <summary>
        /// The ability tooltip hud that should be updated when the cursor hovers over this button.
        /// So it can show the informations for the ability the cursor hovers over.
        /// </summary>
        public AbilityTooltip Tooltip { get; private set; }

        /// <summary>
        /// The player ability that this button represents.
        /// </summary>
        public PlayerAbility PlayerAbility { get; private set; }

        /// <summary>
        /// Gets the skill dialog.
        /// </summary>
        /// <value>
        /// The skill dialog.
        /// </value>
        public SkillDialog SkillDialog { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AbilityButton" /> class.
        /// </summary>
        /// <param name="skillDialog">The parent skill dialog.</param>
        /// <param name="bounds">The bounds of this button.</param>
        /// <param name="tooltipDest">The destination hud for the player ability informations.</param>
        /// <param name="playerAbility">The player ability that this button represents.</param>
        /// <exception cref="ArgumentNullException">Is thrown if skillDialog, tooltipDest or playerAbility is null.</exception>
        public AbilityButton(SkillDialog skillDialog, ElementBounds bounds, AbilityTooltip tooltipDest, PlayerAbility playerAbility) : 
            base(
                skillDialog.Client.XLeveling.Api as ICoreClientAPI,
                playerAbility.Ability.DisplayName + " " + playerAbility.Tier + "/" + playerAbility.Ability.MaxTier,
                playerAbility.RequirementsFulfilled(playerAbility.Tier + 1) ? CairoFont.WhiteSmallishText().WithColor(new[] { 0.0d, 0.8d, 0.0d }) : 
                CairoFont.WhiteSmallishText(), 
                CairoFont.WhiteSmallishText().WithColor(new[] { 1.0d, 0.647d, 0.0d }),
                null, bounds)
        {
            this.SkillDialog = skillDialog ?? throw new ArgumentNullException("The skill dialog of an ability button must not be null.");
            this.Tooltip = tooltipDest ?? throw new ArgumentNullException("The tooltip destination of an ability button must not be null.");
            this.PlayerAbility = playerAbility ?? throw new ArgumentNullException("The player ability of an ability button must not be null.");
        }

        /// <summary>
        /// Checks if the cursor hovers over this button.
        /// </summary>
        /// <param name="api">The vintage story core client api.</param>
        /// <param name="args">The mouse event data.</param>
        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseMove(api, args);
            if (this.IsPositionInside(args.X, args.Y))
            {
                if (!this.CursorHovers)
                {
                    this.CursorHovers = true;
                    this.Tooltip.Update(PlayerAbility);
                    this.Tooltip.TryOpen();
                }
            }
            else if (this.CursorHovers)
            {
                this.CursorHovers = false;
                this.Tooltip.TryClose();
            }
        }

        /// <summary>
        /// Called when this button was clicked on.
        /// </summary>
        /// <param name="api">The API.</param>
        /// <param name="args">The arguments.</param>
        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            if (this.SkillDialog.Unlearn)
            {
                PlayerAbility.SetTier(PlayerAbility.Tier - 1);
            }
            else if (this.SkillDialog.UnlearnAbility)
            {
                PlayerAbility.SetTier(0);
            }
            else
            {
                PlayerAbility.SetTier(PlayerAbility.Tier + 1);
            }
            this.Tooltip.TryClose();
            this.SkillDialog.Setup();
        }

    }//!class AbilityButton
}//!namespace XLib.XLeveling 
