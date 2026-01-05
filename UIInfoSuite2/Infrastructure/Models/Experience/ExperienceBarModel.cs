using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Modules.Hud;

namespace UIInfoSuite2.Infrastructure.Models.Experience;

internal class ExperienceBarModel : ProgressBar
{
  private const int MaxAliveTicks = 480;
  private readonly Lazy<ConfigManager> _configManager = ModEntry.LazyGetSingleton<ConfigManager>();

  private readonly int _dialogBoxYOffset;
  private int _aliveTicks = MaxAliveTicks;
  private int _curSkillLevel = 1;
  private int _skillType = Farmer.farmingSkill;
  private XpThreshold _xpThreshold = new(0, 0, 0);

  public ExperienceBarModel(int maxBarWidth = 250) : base(
    TextureHelper.SkillFillColors[Farmer.farmingSkill],
    maxBarWidth
  )
  {
    _dialogBoxYOffset = DialogBoxHeight + 10;
    UpdatePosition();
  }

  private ModConfig Config => _configManager.Value.Config;

  public void SetTrackedSkill(int skillType, XpThreshold xpThreshold)
  {
    _aliveTicks = MaxAliveTicks;
    _skillType = skillType;
    _curSkillLevel = Game1.player.GetUnmodifiedSkillLevel(_skillType);
    FillColor = TextureHelper.SkillFillColors[skillType];
    _xpThreshold = xpThreshold;
    if (_xpThreshold.NextLevelXp != 0)
    {
      Progress = _xpThreshold.LevelXp / (float)_xpThreshold.NextLevelXp;
    }
  }

  private Rectangle GetSkillTextureRect()
  {
    return Tools.IsMasteryLevel() ? TextureHelper.MasteryIconRectangle : TextureHelper.SkillIconRectangles[_skillType];
  }

  private bool ShouldHide()
  {
    return _aliveTicks <= 0 || !Config.ShowExperienceBar;
  }

  public void Update()
  {
    if (ShouldHide() || !Config.AllowExperienceBarToFadeOut)
    {
      return;
    }

    _aliveTicks--;
  }

  public void UpdatePosition()
  {
    Position = new Vector2(
      (int)GetSafeLeftSide() + 16,
      Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Bottom - _dialogBoxYOffset
    );
  }

  protected override void DrawInnerContent()
  {
    var drawPos = new Vector2(InnerBounds.X, InnerBounds.Y - 2);

    if (IsMouseOver())
    {
      string displayedXpStr = _xpThreshold.LevelXp + "/" + _xpThreshold.NextLevelXp;
      Game1.drawWithBorder(displayedXpStr, Color.Black, Color.Black, drawPos);
    }
    else
    {
      var levelText = _curSkillLevel.ToString();
      float skillTextOffset = Game1.dialogueFont.MeasureString(levelText).X;
      Game1.drawWithBorder(levelText, Color.Black * 0.6f, Color.Black, drawPos);

      float skillIconScale = Tools.IsMasteryLevel() ? 3.625f : 2.9f;
      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        new Vector2(drawPos.X + skillTextOffset + 10, drawPos.Y + 7),
        GetSkillTextureRect(),
        Color.White,
        0,
        Vector2.Zero,
        skillIconScale,
        SpriteEffects.None,
        0.85f
      );
    }
  }

  public override void Draw(SpriteBatch batch)
  {
    if (ShouldHide() || _xpThreshold.NextLevelXp == 0)
    {
      return;
    }

    UpdatePosition();
    base.Draw(batch);
  }

  private static float GetSafeLeftSide()
  {
    Rectangle safeArea = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea;

    if (!Game1.isOutdoorMapSmallerThanViewport())
    {
      return safeArea.Left;
    }

    int layerWidthPx = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
    float smallMapOffset = (safeArea.Right - layerWidthPx) / 2.0f;

    return safeArea.Left + smallMapOffset;
  }
}
