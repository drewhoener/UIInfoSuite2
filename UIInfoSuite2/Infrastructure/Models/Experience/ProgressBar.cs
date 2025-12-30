using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Helpers;

namespace UIInfoSuite2.Infrastructure.Models.Experience;

internal class ProgressBar
{
  protected const int TextureBoxBorderWidth = 12;
  protected static readonly Point BarFillOffset = new(TextureBoxBorderWidth, TextureBoxBorderWidth);
  protected readonly int BarHeight;
  protected readonly int BarWidth;
  protected readonly int DialogBoxHeight;
  protected readonly int DialogBoxWidth;
  private Vector2 _position;
  private float _progress;

  public ProgressBar(Color fillColor, int dialogBoxWidth = 240, int dialogBoxHeight = 64)
  {
    DialogBoxWidth = dialogBoxWidth;
    DialogBoxHeight = dialogBoxHeight;
    BarWidth = dialogBoxWidth - TextureBoxBorderWidth * 2;
    BarHeight = dialogBoxHeight - TextureBoxBorderWidth * 2;
    FillColor = fillColor;

    Position = new Vector2(0, 0);
  }

  public float Progress
  {
    get => _progress;
    set => _progress = Math.Clamp(value, 0.0f, 1.0f);
  }

  public Color FillColor { get; set; }

  public Vector2 Position
  {
    get => _position;
    set
    {
      _position = value;
      Bounds = new Rectangle((int)_position.X, (int)_position.Y, DialogBoxWidth, DialogBoxHeight);
      InnerBounds = new Rectangle(Bounds.X + BarFillOffset.X, Bounds.Y + BarFillOffset.Y, BarWidth, BarHeight);
    }
  }

  public Rectangle Bounds { get; private set; }
  public Rectangle InnerBounds { get; private set; }

  /// <summary>
  ///   Draws content inside the progress bar
  /// </summary>
  protected virtual void DrawInnerContent() { }

  public virtual void Draw(SpriteBatch batch)
  {
    var barWidth = (int)(Progress * BarWidth);
    var drawPos = Position.ToPoint();
    Point barFillPos = drawPos + BarFillOffset;

    IClickableMenu.drawTextureBox(
      batch,
      Game1.menuTexture,
      TextureHelper.OutlinedTextureBox,
      drawPos.X,
      drawPos.Y,
      DialogBoxWidth,
      DialogBoxHeight,
      Color.White
    );

    // Main bar fill
    batch.Draw(Game1.staminaRect, new Rectangle(barFillPos.X, barFillPos.Y, barWidth, BarHeight), FillColor);

    DrawInnerContent();
  }

  protected bool IsMouseOver()
  {
    return Bounds.Contains(Game1.getMouseX(), Game1.getMouseY());
  }
}
