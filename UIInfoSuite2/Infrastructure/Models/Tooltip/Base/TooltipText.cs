using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

internal class TooltipText : LayoutElement
{
  private readonly TrackableValue<SpriteFont> _font;
  private readonly TrackableValue<bool> _isBold;
  private readonly TrackableValue<float> _scale;
  private readonly TrackableValue<string> _text;
  private Dimensions _textDimensions;

  public TooltipText(
    string text,
    float scale = 1.0f,
    Color? color = null,
    SpriteFont? font = null,
    bool bold = false,
    string? identifier = null
  ) : base(identifier)
  {
    _font = new TrackableValue<SpriteFont>(font ?? Game1.dialogueFont, MeasureAndUpdate, "Font");
    _text = new TrackableValue<string>(text, MeasureAndUpdate, "Text");
    _scale = new TrackableValue<float>(scale, MeasureAndUpdate, "Scale");
    _isBold = new TrackableValue<bool>(bold, MeasureAndUpdate, "Bold");
    Color = color ?? Color.Black;

    Padding.SetAll(0);
    Margin.SetInsets(0, 5, 0, 5);

    MeasureAndUpdate("init");
  }

  public SpriteFont Font
  {
    get => _font.Value;
    set => _font.Value = value;
  }

  public float Scale
  {
    get => _scale.Value;
    set => _scale.Value = value;
  }

  public string Text
  {
    get => _text.Value;
    set => _text.Value = value;
  }

  public bool IsBold
  {
    get => _isBold.Value;
    set => _isBold.Value = value;
  }

  public Color Color { get; set; }

  private void MeasureAndUpdate(string? sender)
  {
    _textDimensions = MeasureString(Text, IsBold, Scale, Font);
    MarkLayoutDirty(sender ?? "unknown");
  }

  /// <summary>Measure the rendered dialogue text size for the given text.</summary>
  /// <param name="text">The text to measure.</param>
  /// <param name="bold">Whether the font is bold.</param>
  /// <param name="scale">The scale to apply to the size.</param>
  /// <param name="font">The font to measure. Defaults to <see cref="Game1.dialogueFont" /> if <c>null</c>.</param>
  public static Dimensions MeasureString(string text, bool bold = false, float scale = 1f, SpriteFont? font = null)
  {
    return bold
      ? new Dimensions(SpriteText.getWidthOfString(text), SpriteText.getHeightOfString(text)) * scale
      : Dimensions.FromVector2((font ?? Game1.dialogueFont).MeasureString(text) * scale);
  }

  public static TooltipText Bold(string text, float scale = 1.0f, Color? color = null, string? identifier = null)
  {
    return new TooltipText(text, scale, color, null, true, identifier);
  }

  public TooltipText SetText(string text)
  {
    Text = text;
    return this;
  }

  public TooltipText SetFont(SpriteFont font)
  {
    Font = font;
    return this;
  }

  public TooltipText SetScale(float scale)
  {
    Scale = scale;
    return this;
  }

  public TooltipText SetColor(Color color)
  {
    Color = color;
    return this;
  }

  protected override void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    positionX += Margin.Left.OrZero() + Padding.Left.OrZero();
    positionY += Margin.Top.OrZero() + Padding.Top.OrZero();

    if (IsBold)
    {
      float originalTextScale = SpriteText.fontPixelZoom;
      SpriteText.fontPixelZoom *= Scale;
      SpriteText.drawString(spriteBatch, Text, positionX, positionY, layerDepth: 1, color: null);
      SpriteText.fontPixelZoom = originalTextScale;
    }
    else
    {
      spriteBatch.DrawString(
        Font,
        Text,
        new Vector2(positionX, positionY),
        Color,
        0,
        Vector2.Zero,
        Scale,
        SpriteEffects.None,
        0f
      );
    }
  }

  protected override Dimensions MeasureContent()
  {
    return _textDimensions;
  }
}
