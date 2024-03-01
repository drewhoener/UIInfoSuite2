﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System;
using System.Linq;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;
using UIInfoSuite2.UIElements.Base;

namespace UIInfoSuite2.UIElements
{
    internal class ShowTravelingMerchant : UIHudElement
    {
        #region Properties
        private bool _travelingMerchantIsHere;
        private bool _travelingMerchantIsVisited;
        private ClickableTextureComponent _travelingMerchantIcon;

        private bool Enabled { get; set; }
        private bool HideWhenVisited { get; set; }

        private readonly IModHelper _helper;
        #endregion


        #region Lifecycle
        public ShowTravelingMerchant(IModHelper helper, ModOptions options) : base(helper, options)
        {
            _helper = helper;
        }


        protected override void SetUpOptions()
        {
            var enabledOption = new ModOptionsCheckbox(
                new OptionStringWrapper(nameof(Options.ShowTravelingMerchant)),
                1,
                (s, b) => OnOptionsChanged(s, b),
                () => Options.ShowTravelingMerchant,
                b => Options.ShowTravelingMerchant = b
            );
            OptionElements.Add(enabledOption);
            OptionElements.Add(new ModOptionsCheckbox(
                new OptionStringWrapper(nameof(Options.HideMerchantWhenVisited)),
                1,
                (_, _) => { }, // Option does not require refresh
                () => Options.HideMerchantWhenVisited,
                b => Options.HideMerchantWhenVisited = b,
                enabledOption
            ));
        }

        protected override void OnOptionsChanged(string? whichOption, dynamic? newValue)
        {
            throw new NotImplementedException();
        }

        protected override void RegisterEvents()
        {
            throw new NotImplementedException();
        }

        protected override void UnregisterEvents()
        {
            throw new NotImplementedException();
        }

        public void ToggleOption(bool showTravelingMerchant)
        {
            Enabled = showTravelingMerchant;

            _helper.Events.Display.RenderingHud -= OnRenderingHud;
            _helper.Events.Display.RenderedHud -= OnRenderedHud;
            _helper.Events.GameLoop.DayStarted -= OnDayStarted;
            _helper.Events.Display.MenuChanged -= OnMenuChanged;

            if (showTravelingMerchant)
            {
                UpdateTravelingMerchant();
                _helper.Events.Display.RenderingHud += OnRenderingHud;
                _helper.Events.Display.RenderedHud += OnRenderedHud;
                _helper.Events.GameLoop.DayStarted += OnDayStarted;
                _helper.Events.Display.MenuChanged += OnMenuChanged;
            }
        }

        public void ToggleHideWhenVisitedOption(bool hideWhenVisited)
        {
            HideWhenVisited = hideWhenVisited;
            ToggleOption(Enabled);
        }
        #endregion


        #region Event subscriptions
        private void OnDayStarted(object sender, EventArgs e)
        {
            UpdateTravelingMerchant();
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            if (e.NewMenu is ShopMenu menu && menu.forSale.Any(s => !(s is Hat)) && Game1.currentLocation.Name == "Forest")
            {
                _travelingMerchantIsVisited = true;
            }
        }

        private void OnRenderingHud(object sender, RenderingHudEventArgs e)
        {
            // Draw icon
            if (!Game1.eventUp && ShouldDrawIcon())
            {
                Point iconPosition = IconHandler.Handler.GetNewIconPosition();
                _travelingMerchantIcon =
                    new ClickableTextureComponent(
                        new Rectangle(iconPosition.X, iconPosition.Y, 40, 40),
                        Game1.mouseCursors,
                        new Rectangle(192, 1411, 20, 20),
                        2f);
                _travelingMerchantIcon.draw(Game1.spriteBatch);
            }
        }

        private void OnRenderedHud(object sender, RenderedHudEventArgs e)
        {
            // Show text on hover
            if (ShouldDrawIcon() && (_travelingMerchantIcon?.containsPoint(Game1.getMouseX(), Game1.getMouseY()) ?? false))
            {
                string hoverText = _helper.SafeGetString(LanguageKeys.TravelingMerchantIsInTown);
                IClickableMenu.drawHoverText(
                    Game1.spriteBatch,
                    hoverText,
                    Game1.dialogueFont
                );
            }
        }
        #endregion


        #region Logic
        private void UpdateTravelingMerchant()
        {
            int dayOfWeek = Game1.dayOfMonth % 7;
            _travelingMerchantIsHere = dayOfWeek == 0 || dayOfWeek == 5;

            _travelingMerchantIsVisited = false;
        }

        private bool ShouldDrawIcon()
        {
            return _travelingMerchantIsHere && (!_travelingMerchantIsVisited || !HideWhenVisited);
        }
        #endregion
    }
}
