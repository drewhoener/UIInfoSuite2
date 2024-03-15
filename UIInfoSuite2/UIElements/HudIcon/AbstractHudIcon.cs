using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.UIElements.Base
{
    public abstract class AbstractHudIcon
    {
        private readonly PerScreen<string?> _hoverText = new(() => null);
        private readonly PerScreen<ClickableTextureComponent?> _icon = new(() => null);

        #region Events

        public void RegisterEvents(IModHelper helper)
        {
            helper.Events.Display.RenderingHud += RenderHudIcon;
            helper.Events.Display.RenderedHud += RenderHoverText;
        }

        public void UnregisterEvents(IModHelper helper)
        {
            helper.Events.Display.RenderingHud -= RenderHudIcon;
            helper.Events.Display.RenderedHud -= RenderHoverText;
        }

        /// <summary>
        ///     Helper handler for rendering hover text for a bar element.
        /// </summary>
        /// <param name="sender">Event sender, nullable</param>
        /// <param name="e">Event arguments</param>
        private void RenderHoverText(object? sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (ShouldRenderHoverText())
            {
                DrawHoverText();
            }
        }

        /// <summary>
        ///     Helper handler for rendering the icon on the UI Bar
        /// </summary>
        /// <param name="sender">Event sender, nullable</param>
        /// <param name="e">Event arguments</param>
        private void RenderHudIcon(object? sender, RenderingHudEventArgs e)
        {
            if (Game1.eventUp)
            {
                return;
            }

            DrawIcon();
        }

        #endregion

        #region Generator Functions

        protected ClickableTextureComponent GetIcon(bool forceRegen = false)
        {
            if (forceRegen || _icon.Value == null)
            {
                _icon.Value = GenerateIcon();
            }

            return _icon.Value;
        }

        protected string GetHoverText(bool forceRegen = false)
        {
            if (forceRegen || _hoverText.Value == null)
            {
                _hoverText.Value = GenerateHoverText();
            }

            return _hoverText.Value;
        }

        public void Regenerate()
        {
            _icon.Value = GetIcon(true);
            _hoverText.Value = GetHoverText(true);
        }

        #endregion

        #region Render Functions

        protected abstract ClickableTextureComponent GenerateIcon();
        protected abstract string GenerateHoverText();

        protected abstract bool ShouldRenderHoverText();
        protected abstract bool ShouldDrawIcon();

        protected virtual void DrawDelegate()
        {
            GetIcon().draw(Game1.spriteBatch);
        }

        protected virtual void DrawHoverDelegate()
        {
            IClickableMenu.drawHoverText(Game1.spriteBatch, _hoverText.Value, Game1.dialogueFont);
        }

        protected virtual Point GetRenderPoint()
        {
            return IconHandler.Handler.GetNewIconPosition();
        }

        protected virtual void MoveIconToPos(Point? point = null)
        {
            if (!ShouldDrawIcon())
            {
                return;
            }

            Point iconPos = point ?? GetRenderPoint();
            GetIcon().setPosition(iconPos.X, iconPos.Y);
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

        protected virtual void DrawHoverText()
        {
            if (GetIcon().IsHoveredOver())
            {
                DrawHoverDelegate();
            }
        }

        #endregion
    }
}
