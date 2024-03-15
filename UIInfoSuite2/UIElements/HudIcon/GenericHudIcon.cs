using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.UIElements.Base;

namespace UIInfoSuite2.UIElements.HudIcon
{
    public class GenericHudIcon : AbstractHudIcon
    {
        private readonly Point _dimensions;
        private readonly Texture2D _baseTexture;
        private readonly Rectangle _sourceRect;
        private readonly float _scale;
        private readonly Func<string>? _getHoverTextFunc;
        private readonly Func<bool>? _shouldRenderIconFunc;

        public GenericHudIcon(
            string hoverText,
            Point dimensions,
            Texture2D baseTexture,
            Rectangle sourceRect,
            float scale,
            Func<bool>? shouldRenderIconFunc
        ) : this(dimensions, baseTexture, sourceRect, scale, shouldRenderIconFunc, () => hoverText)
        {
        }

        public GenericHudIcon(
            Point dimensions,
            Texture2D baseTexture,
            Rectangle sourceRect,
            float scale,
            Func<bool>? shouldRenderIconFunc,
            Func<string>? getHoverTextFunc
        )
        {
            _dimensions = dimensions;
            _baseTexture = baseTexture;
            _sourceRect = sourceRect;
            _scale = scale;
            _shouldRenderIconFunc = shouldRenderIconFunc;
            _getHoverTextFunc = getHoverTextFunc;
        }

        protected override ClickableTextureComponent GenerateIcon()
        {
            return new ClickableTextureComponent(
                new Rectangle(0, 0, _dimensions.X, _dimensions.Y),
                _baseTexture,
                _sourceRect,
                _scale
            );
        }

        protected override string GenerateHoverText()
        {
            return _getHoverTextFunc == null ? "" : _getHoverTextFunc.Invoke();
        }

        protected override bool ShouldDrawIcon()
        {
            return !Game1.IsFakedBlackScreen() && (_shouldRenderIconFunc == null || _shouldRenderIconFunc.Invoke());
        }

        protected override bool ShouldRenderHoverText()
        {
            return ShouldDrawIcon() && GetIcon().IsHoveredOver();
        }
    }
}
