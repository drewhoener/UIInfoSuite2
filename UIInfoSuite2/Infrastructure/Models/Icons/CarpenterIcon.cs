using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class CarpenterIcon : ClickableIcon
{
  private readonly IconTriggerField<bool> _isRobinBuilding;

  public CarpenterIcon(
    Texture2D baseTexture,
    Rectangle sourceBounds,
    float finalSize,
    PrimaryDimension primaryDimension = PrimaryDimension.Width,
    Action<object?, ButtonPressedEventArgs, Vector2>? clickHandlerAction = null,
    SpriteFont? hoverFont = null
  ) : base(baseTexture, sourceBounds, finalSize, primaryDimension, clickHandlerAction, hoverFont)
  {
    _isRobinBuilding = new IconTriggerField<bool>(this, false);
  }

  public bool IsRobinBuilding
  {
    get => _isRobinBuilding.Value;
    set => _isRobinBuilding.Value = value;
  }

  protected override bool _ShouldDraw()
  {
    return IsRobinBuilding && base._ShouldDraw();
  }
}
