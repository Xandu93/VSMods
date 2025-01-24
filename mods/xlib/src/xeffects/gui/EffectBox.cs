using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace XLib.XEffects
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="GuiElement" />
    public class EffectBox : GuiElement
    {
        /// <summary>
        /// Is true when the cursor hovers over the button
        /// </summary>
        public bool CursorHovers { get; private set; }

        /// <summary>
        /// The effect tooltip hud that should be updated when the cursor hovers over this box.
        /// So it can show the informations for the effect the cursor hovers over.
        /// </summary>
        public EffectTooltip Tooltip { get; private set; }

        /// <summary>
        /// Gets the effect.
        /// </summary>
        /// <value>
        /// The effect.
        /// </value>
        public Effect Effect { get; internal set; }

        private GuiElementDynamicText text;

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectBox" /> class.
        /// </summary>
        /// <param name="capi">The Client API</param>
        /// <param name="effect">The effect.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="tooltipDest">The tooltip dest.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public EffectBox(ICoreClientAPI capi, Effect effect, ElementBounds bounds, EffectTooltip tooltipDest) : base(capi, bounds)
        {
            this.Effect = effect;
            this.Tooltip = tooltipDest;
            this.text = new GuiElementDynamicText(capi, effect.GetName(), CairoFont.WhiteDetailText(),
                ElementBounds.FixedPos(EnumDialogArea.LeftTop, 32, 0).WithFixedSize(200, 32).WithParent(bounds));
        }

        /// <summary>
        /// Adds the elements to a composer.
        /// </summary>
        /// <param name="composer">The composer.</param>
        public void AddToComposer(GuiComposer composer)
        {
            composer.AddInteractiveElement(text);
            ElementBounds iconBounds = ElementBounds.FixedPos(EnumDialogArea.LeftTop, 0, 0).WithFixedSize(32, 32).WithParent(this.Bounds);
            if (Effect.EffectType.IconName != null) composer.AddImageBG(iconBounds, new AssetLocation(Effect.EffectType.IconName));
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        public void Update()
        {
            this.text.SetNewText(Effect.GetName());
        }

        /// <summary>
        /// Checks if the cursor hovers over this button.
        /// </summary>
        /// <param name="api">The vintage story core client api.</param>
        /// <param name="args">The mouse event data.</param>
        public override void OnMouseMove(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseMove(api, args);
            OnMouseMove(args.X, args.Y);
        }

        /// <summary>
        /// Checks if the cursor hovers over this button.
        /// </summary>
        /// <param name="x">The x position of the mouse.</param>
        /// <param name="y">The y position of the mouse.</param>
        public void OnMouseMove(int x, int y)
        {
            if (this.IsPositionInside(x, y))
            {
                if (!this.CursorHovers)
                {
                    this.CursorHovers = true;
                    this.Tooltip.Update(this.Effect);
                    this.Tooltip.TryOpen();
                }
            }
            else if (this.CursorHovers)
            {
                this.CursorHovers = false;
                if (this.Tooltip.Effect == this.Effect)
                {
                    this.Tooltip.TryClose();
                }
            }
        }
    }//!class EffectBox
}//!namespace XLib.XEffects
