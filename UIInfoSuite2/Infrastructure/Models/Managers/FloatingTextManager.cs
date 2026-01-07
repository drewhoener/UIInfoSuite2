using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Infrastructure.Config;

namespace UIInfoSuite2.Infrastructure.Models.Managers;

internal class FloatingText(
  string text,
  int lifetimeTicks,
  Vector2 velocity,
  bool fadeOut = true,
  string id = "FloatingText",
  int zIndex = 0,
  int fullAlphaTicks = 0
)
{
  private static readonly Vector2 HeadOffset = new(-28, -130);
  private float _alpha = 1.0f;
  private int _ticksAlive;

  public bool IsAlive => _ticksAlive < lifetimeTicks;
  public string Id => id;

  public int ZIndex => zIndex;

  protected virtual Vector2 GetHeadOffset()
  {
    return HeadOffset;
  }

  protected Vector2 GetTextPosition()
  {
    Vector2 velocityOffset = velocity * _ticksAlive;
    return Game1.player.getLocalPosition(Game1.viewport) + GetHeadOffset() + velocityOffset;
  }

  public void Update()
  {
    _ticksAlive++;
    if (fadeOut && _ticksAlive >= fullAlphaTicks)
    {
      _alpha = 1.0f - _ticksAlive / (float)lifetimeTicks;
    }
  }

  public virtual void Draw(SpriteBatch spriteBatch)
  {
    Vector2 position = GetTextPosition();
    Game1.drawWithBorder(
      text,
      Color.DarkSlateGray * _alpha,
      Color.PaleTurquoise * _alpha,
      Utility.ModifyCoordinatesForUIScale(position),
      0.0f,
      0.8f,
      0.0f
    );
  }
}

internal class FloatingTextManager(
  IModRegistry registry,
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager
)
{
  private readonly PerScreen<List<FloatingText>> _textCache = new(() => []);

  public void Add(FloatingText floatingTextModel)
  {
    _textCache.Value.Add(floatingTextModel);
  }

  public void ClearId(string id)
  {
    _textCache.Value.RemoveAll(text => text.Id == id);
  }

  public void Clear()
  {
    _textCache.Value.Clear();
  }

#region Events
  public void RegisterEvents()
  {
    modEvents.Display.RenderingHud += TickAndRenderText;
    modEvents.GameLoop.SaveLoaded += OnSaveLoaded;
  }

  public void UnregisterEvents()
  {
    modEvents.Display.RenderingHud -= TickAndRenderText;
    modEvents.GameLoop.SaveLoaded -= OnSaveLoaded;
  }

  private void TickAndRenderText(object? sender, RenderingHudEventArgs e)
  {
    // Not the most efficient render method, but we should only have ~10-20 elements in here at a time.
    // The overhead shouldn't be terrible
    _textCache.Value.Sort((a, b) => b.ZIndex - a.ZIndex);

    foreach (FloatingText text in _textCache.Value)
    {
      text.Draw(e.SpriteBatch);
      text.Update();
    }

    _textCache.Value.RemoveAll(text => !text.IsAlive);
  }

  private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
  {
    Clear();
  }
#endregion
}
