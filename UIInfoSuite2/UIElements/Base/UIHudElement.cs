using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements.Base
{
    internal abstract class UIHudElement : UIHoverElement
    {
        protected readonly PerScreen<ClickableTextureComponent> Icon = new();

        protected UIHudElement(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        protected sealed override bool ShouldRenderHoverText()
        {
            return ShouldDrawIcon() && Icon.Value.IsHoveredOver();
        }

        #region Event Registration

        protected override void _RegisterInternalEvents()
        {
            base._RegisterInternalEvents();
            Helper.Events.Display.RenderingHud += Internal_RenderHudIcon;
        }

        protected override void _UnregisterInternalEvents()
        {
            base._UnregisterInternalEvents();
            Helper.Events.Display.RenderingHud -= Internal_RenderHudIcon;
        }

        #endregion

        #region Icon Rendering

        /// <summary>
        ///     Helper handler for rendering the icon on the UI Bar
        /// </summary>
        /// <param name="sender">Event sender, nullable</param>
        /// <param name="e">Event arguments</param>
        private void Internal_RenderHudIcon(object? sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp)
            {
                return;
            }

            DrawIcon();
        }

        protected virtual bool ShouldDrawIcon()
        {
            return true;
        }

        protected virtual void MoveIconToPos(Point? point = null)
        {
            if (!ShouldDrawIcon())
            {
                return;
            }

            Point iconPos = point ?? IconHandler.Handler.GetNewIconPosition();
            Icon.Value.setPosition(iconPos.X, iconPos.Y);
        }

        protected virtual void DrawIcon(bool moveAutomatically = true)
        {
            if (moveAutomatically)
            {
                MoveIconToPos();
            }

            if (ShouldDrawIcon())
            {
                DrawDelegate();
            }
        }

        protected virtual void DrawDelegate()
        {
            Icon.Value.draw(Game1.spriteBatch);
        }

        #endregion
    }
}
