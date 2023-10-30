using StardewValley;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure.Extensions
{
    public static class ClickableTextureComponentExtensions
    {
        public static bool IsHoveredOver(this ClickableTextureComponent component)
        {
            return component.containsPoint(Game1.getMouseX(), Game1.getMouseY());
        }
    }
}
