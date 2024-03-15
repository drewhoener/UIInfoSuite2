using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using System;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure
{
    internal sealed class IconRow
    {
        private int TotalIconWidth { get; set; }
        private int NumIcons { get; set; }
        private int MaxIconHeight { get; set; }

        private readonly int _iconSpacing;

        public IconRow(int iconSpacing)
        {
            _iconSpacing = iconSpacing;
        }

        public void Reset()
        {
            TotalIconWidth = 0;
            NumIcons = 0;
            MaxIconHeight = 0;
        }

        public Point IconOffset(int newIconHeight)
        {
            int yOffset = Math.Abs(MaxIconHeight - newIconHeight) / 2;
            return new Point(TotalIconWidth + NumIcons * _iconSpacing, yOffset);
        }

        public Point AddIcon(ClickableComponent component)
        {
            return AddIcon(component.bounds.Width, component.bounds.Height);
        }

        public Point AddIcon(int iconWidth, int iconHeight)
        {
            NumIcons++;
            MaxIconHeight = Math.Max(iconHeight, MaxIconHeight);
            TotalIconWidth += iconWidth;

            return IconOffset(iconHeight);
        }
    }

    public sealed class IconHandler
    {
        private const int DefaultIconSpacing = 8;
        private static readonly Rectangle DefaultIconSize = new(0, 0, 40, 40);
        public static IconHandler Handler { get; } = new();

        private readonly PerScreen<IconRow> _iconRow = new(() => new IconRow(DefaultIconSpacing));

        private IconHandler()
        {
        }

        public Point GetNewIconPosition()
        {
            return GetNewIconPosition(DefaultIconSize);
        }

        public Point GetNewIconPosition(ClickableComponent component)
        {
            return GetNewIconPosition(component.bounds.Width, component.bounds.Height);
        }

        public Point GetNewIconPosition(Rectangle rect)
        {
            return GetNewIconPosition(rect.Width, rect.Height);
        }

        private Point GetNewIconPosition(int iconWidth, int iconHeight)
        {
            Point posOffset = _iconRow.Value.AddIcon(iconWidth, iconHeight);
            int yPos = (Game1.options.zoomButtons ? 290 : 260) + posOffset.Y;
            int xPosition = Tools.GetWidthInPlayArea() - 70 - posOffset.X;
            if (Game1.player.questLog.Any() || Game1.player.team.specialOrders.Any())
            {
                xPosition -= 65;
            }

            return new Point(xPosition, yPos);
        }

        public void Reset(object sender, EventArgs e)
        {
            _iconRow.Value.Reset();
        }
    }
}
