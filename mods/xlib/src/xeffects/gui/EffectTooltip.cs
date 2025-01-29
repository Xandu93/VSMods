using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace XLib.XEffects
{
    /// <summary>
    /// Shows informations of an effect.
    /// </summary>
    /// <seealso cref="HudElement" />
    public class EffectTooltip : HudElement
    {
        /// <summary>
        /// The gui text element that shows the name of the effect.
        /// </summary>
        protected GuiElementDynamicText nameText;

        /// <summary>
        /// The gui text element that shows the description of the effect.
        /// </summary>
        protected GuiElementRichtext descText;

        /// <summary>
        /// The gui text element that shows the names of some values.
        /// </summary>
        protected GuiElementDynamicText valueNamesText;

        /// <summary>
        /// The gui text element that shows current values of the effect.
        /// </summary>
        protected GuiElementDynamicText valuesText;

        /// <summary>
        /// The current effect.
        /// </summary>
        public Effect Effect {  get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectTooltip"/> class.
        /// </summary>
        /// <param name="capi">The client api.</param>
        /// <param name="parentBounds">The parent bounds.</param>
        public EffectTooltip(ICoreClientAPI capi, ElementBounds parentBounds) : base(capi)
        {
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithParent(parentBounds).WithAlignment(EnumDialogArea.RightTop).WithFixedOffset(320, 0);
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds nameBounds = ElementBounds.Fixed(0, 0, 280, 24);
            ElementBounds descBounds = ElementBounds.Fixed(0, 24, 280, 0).WithParent(nameBounds).WithAlignment(EnumDialogArea.CenterTop);

            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(nameBounds, descBounds);
            dialogBounds.WithChildren(bgBounds);

            SingleComposer = capi.Gui.CreateCompo("SkillTooltip", dialogBounds)
                .AddDialogBG(bgBounds, false)
                .AddDynamicText("", CairoFont.WhiteSmallishText(), nameBounds, "EffectName")
                .AddRichtext("", CairoFont.WhiteDetailText(), descBounds, "EffectDesc")
                .Compose();

            this.nameText = SingleComposer.GetDynamicText("EffectName");
            this.descText = SingleComposer.GetRichtext("EffectDesc");
        }

        /// <summary>
        /// Updates this hud to provide informations of the specified effect.
        /// </summary>
        /// <param name="effect">The effect.</param>
        public void Update(Effect effect)
        {
            EffectType type = effect.EffectType;
            this.Effect = effect;

            ElementBounds parent = SingleComposer.CurParentBounds.ParentBounds;
            if (parent.absX + parent.OuterWidth * 0.5f > SingleComposer.Api.Gui.WindowBounds.OuterWidth * 0.5f)
            {
                if (parent.absY + parent.OuterHeight * 0.5f < SingleComposer.Api.Gui.WindowBounds.OuterHeight * 0.5f)
                {
                    SingleComposer.Bounds.WithAlignment(EnumDialogArea.LeftTop).WithFixedPosition(-320, 0);
                }
                else
                {
                    SingleComposer.Bounds.WithAlignment(EnumDialogArea.LeftBottom).WithFixedPosition(-320, 0);
                }
            }
            else
            {
                if (parent.absY + parent.OuterHeight * 0.5f < SingleComposer.Api.Gui.WindowBounds.OuterHeight * 0.5f)
                {
                    SingleComposer.Bounds.WithAlignment(EnumDialogArea.RightTop).WithFixedPosition(320, 0);
                }
                else
                {
                    SingleComposer.Bounds.WithAlignment(EnumDialogArea.RightBottom).WithFixedPosition(320, 0);
                }
            }

            string description = "\n" + 
                (type.EffectGroup != null ? Lang.Get("xeffects:group") + ": " + Lang.Get(type.Domain + ':' + type.EffectGroup) + "\n" : "") +
                (type.EffectCategory != null ? Lang.Get("xeffects:category") + ": " + Lang.Get(type.Domain + ':' + type.EffectCategory) + "\n" : "") +
                ((effect.MaxStacks > 1) ? Lang.Get("xeffects:stacks") + ": " + effect.Stacks + "/" + effect.MaxStacks + "\n" : "") +
                ((effect.Duration > 0.0f) ? Lang.Get("xeffects:duration") + ": " + Effect.TimeToString(effect.Runtime) + "/" + Effect.TimeToString(effect.Duration) + "\n" : "") +
                ((effect.Interval > 0.0f) ? Lang.Get("xeffects:interval") + ": " + Effect.TimeToString(effect.Interval) + "\n" : "");

            if (effect is DiseaseEffect disease)
            {
                description += (disease.HealingRate != 0.0f ? Lang.Get("xeffects:healingrate") + ": " + string.Format("{0:0.00####}", disease.HealingRate * 60.0f) : "");
            }

            description += "\n" + effect.GetDescription();

            this.nameText.SetNewText(type.DisplayName);
            this.descText.SetNewText(description, CairoFont.WhiteDetailText());

            SingleComposer.ReCompose();
        }
    }//!class EffectTooltip
}//!namespace XLib.XEffects
