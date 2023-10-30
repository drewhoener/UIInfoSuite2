using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Options;

namespace UIInfoSuite2.UIElements.Base
{
    internal abstract class UIHoverElement : UIElementBase
    {
        protected readonly PerScreen<string> HoverText = new(() => string.Empty);

        protected UIHoverElement(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        #region Event Registraton

        protected override void _RegisterInternalEvents()
        {
            Helper.Events.Display.RenderedHud += Internal_RenderHoverText;
        }

        protected override void _UnregisterInternalEvents()
        {
            Helper.Events.Display.RenderedHud -= Internal_RenderHoverText;
        }

        #endregion

        #region Hover Text Handler

        protected abstract bool ShouldRenderHoverText();

        /// <summary>
        ///     Delegate function to render the hover text.
        ///     Doesn't necessarily need to be overridden, this should be enough for most applications
        /// </summary>
        protected virtual void DrawHoverText()
        {
            IClickableMenu.drawHoverText(Game1.spriteBatch, HoverText.Value, Game1.dialogueFont);
        }

        /// <summary>
        ///     Helper handler for rendering hover text for a bar element.
        /// </summary>
        /// <param name="sender">Event sender, nullable</param>
        /// <param name="e">Event arguments</param>
        private void Internal_RenderHoverText(object? sender, RenderedHudEventArgs e)
        {
            // draw hover text
            if (ShouldRenderHoverText())
            {
                DrawHoverText();
            }
        }

        #endregion
    }
}
