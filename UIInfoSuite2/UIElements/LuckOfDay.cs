using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;
using UIInfoSuite2.UIElements.Base;

namespace UIInfoSuite2.UIElements
{
    internal class LuckOfDay : UIHudElement
    {
        #region Properties

        private readonly PerScreen<Color> _color = new(() => new Color(Color.White.ToVector4()));

        private static readonly Color Luck1Color = new(87, 255, 106, 255);
        private static readonly Color Luck2Color = new(148, 255, 210, 255);
        private static readonly Color Luck3Color = new(246, 255, 145, 255);
        private static readonly Color Luck4Color = new(255, 255, 255, 255);
        private static readonly Color Luck5Color = new(255, 155, 155, 255);
        private static readonly Color Luck6Color = new(165, 165, 165, 204);

        #endregion

        #region Lifecycle

        public LuckOfDay(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        protected override void SetUpOptions()
        {
            var enabledOption = new ModOptionsCheckbox(
                new OptionStringWrapper(nameof(Options.ShowLuckIcon)),
                1,
                (s, b) => OnOptionsChanged(s, b),
                () => Options.ShowLuckIcon,
                b => Options.ShowLuckIcon = b
            );
            OptionElements.Add(enabledOption);
            OptionElements.Add(
                new ModOptionsCheckbox(
                    new OptionStringWrapper(nameof(Options.ShowExactValue)),
                    1,
                    (_, _) => { }, // Option does not require refresh
                    () => Options.ShowExactValue,
                    b => Options.ShowExactValue = b,
                    enabledOption
                )
            );
        }

        protected override bool ShouldEnable()
        {
            return Options.ShowLuckIcon;
        }

        protected override void OnEnable()
        {
            AdjustIconXToBlackBorder();
        }

        #endregion

        #region Event subscriptions

        protected override void RegisterEvents()
        {
            Helper.Events.Player.Warped += OnWarped;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        }

        protected override void UnregisterEvents()
        {
            Helper.Events.Player.Warped -= OnWarped;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            CalculateLuck(e);
        }

        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            // adjust icon X to black border
            if (e.IsLocalPlayer)
            {
                AdjustIconXToBlackBorder();
            }
        }

        #endregion

        #region Logic

        protected override void DrawDelegate()
        {
            Icon.Value.draw(Game1.spriteBatch, _color.Value, 1f);
        }

        private void CalculateLuck(UpdateTickedEventArgs e)
        {
            if (!e.IsMultipleOf(30))
            {
                return; // half second
            }

            switch (Game1.player.DailyLuck)
            {
                // Spirits are very happy (FeelingLucky)
                case > 0.07:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus1);
                    _color.Value = Luck1Color;
                    break;
                // Spirits are in good humor (LuckyButNotTooLucky)
                case <= 0.07 and > 0.02:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus2);
                    _color.Value = Luck2Color;
                    break;
                // The spirits feel neutral
                case var l and >= -0.02 and <= 0.02 when l != 0:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus3);
                    _color.Value = Luck3Color;
                    break;
                // The spirits feel absolutely neutral
                case 0:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus4);
                    _color.Value = Luck4Color;
                    break;
                // The spirits are somewhat annoyed (NotFeelingLuckyAtAll)
                case < -0.02 and >= -0.07:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus5);
                    _color.Value = Luck5Color;
                    break;
                // The spirits are very displeased (MaybeStayHome)
                case < -0.07:
                    HoverText.Value = Helper.SafeGetString(LanguageKeys.LuckStatus6);
                    _color.Value = Luck6Color;
                    break;
            }

            // Rewrite the text, but keep the color
            if (Options.ShowExactValue)
            {
                HoverText.Value = string.Format(
                    Helper.SafeGetString(LanguageKeys.DailyLuckValue),
                    Game1.player.DailyLuck.ToString("N3")
                );
            }
        }

        private void AdjustIconXToBlackBorder()
        {
            Icon.Value = new ClickableTextureComponent(
                "",
                new Rectangle(Tools.GetWidthInPlayArea() - 134, 290, 10 * Game1.pixelZoom, 10 * Game1.pixelZoom),
                "",
                "",
                Game1.mouseCursors,
                new Rectangle(50, 428, 10, 14),
                Game1.pixelZoom
            );
        }

        #endregion
    }
}
