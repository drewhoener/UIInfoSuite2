using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Infrastructure.Models;

internal record HudIconRow(ClickableIcon[] Icons, int MaxRowHeight);

internal class HudIconStorage(IModEvents modEvents, IMonitor logger, ConfigManager configManager, ApiManager apiManager)
{
  private readonly Dictionary<string, ClickableIcon> _icons = new();

  private List<HudIconRow> _iconRows = [];
  private bool _iconRowsDirty = true;
  private ModConfig Config => configManager.Config;

  public void RegisterEvents()
  {
    modEvents.Display.RenderingHud += RenderIcons;
    modEvents.Display.RenderedHud += RenderHoverText;
    modEvents.Input.ButtonPressed += HandleButtonPress;
  }

  public void UnregisterEvents()
  {
    modEvents.Display.RenderingHud -= RenderIcons;
    modEvents.Display.RenderedHud -= RenderHoverText;
    modEvents.Input.ButtonPressed -= HandleButtonPress;
  }

  public void AddIcon(string key, ClickableIcon icon)
  {
    _icons.Add(key, icon);
    _iconRowsDirty = true;
  }

  public bool HasIcon(string key)
  {
    return _icons.ContainsKey(key);
  }

  public ClickableIcon? GetIcon(string key)
  {
    return _icons.GetValueOrDefault(key);
  }

  public ClickableIcon GetIconUnsafe(string key)
  {
    return _icons[key];
  }

  public ClickableIcon? RemoveIcon(string key)
  {
    ClickableIcon? icon = GetIcon(key);
    if (icon == null)
    {
      return null;
    }

    _icons.Remove(key);
    _iconRowsDirty = true;
    return icon;
  }

  public int RemoveIconWhere(Func<KeyValuePair<string, ClickableIcon>, bool> match)
  {
    int removed = _icons.RemoveWhere(match);
    _iconRowsDirty = true;
    return removed;
  }

  public void MarkRowsDirty()
  {
    _iconRowsDirty = true;
  }

  private void UpdateIconRows()
  {
    foreach ((string key, ClickableIcon icon) in _icons)
    {
      if (!icon.HasRenderingChanged())
      {
        continue;
      }

      _iconRowsDirty = true;
      logger.Log($"Icon {key} has been marked dirty and will change rendered rows.");
    }

    if (!_iconRowsDirty)
    {
      return;
    }

    logger.Log("Icon rows are no longer valid, recalculating...");
    _iconRows = _icons.Values.Where(icon => icon.ShouldDraw())
      .Chunk(Config.HudIconsPerRow)
      .Select(row => new HudIconRow(row, row.Max(icon => icon.Dimensions.Height)))
      .ToList();
    _iconRowsDirty = false;
  }

#region Events
  private void RenderIcons(object? sender, RenderingHudEventArgs e)
  {
    if (!UIElementUtils.IsRenderingNormally())
    {
      if (!_iconRowsDirty)
      {
        logger.Log("Not rendering normally, recalculation needed on next free update.");
      }

      _iconRowsDirty = true;
      return;
    }

    UpdateIconRows();
    int heightOffset = (Game1.options.zoomButtons ? 290 : 260) + Config.HudIconsVerticalOffset;
    bool shouldOffsetForQuestLog = IconHandler.Handler.IsQuestLogPermanent ||
                                   Game1.player.questLog.Any() ||
                                   Game1.player.team.specialOrders.Any();

    // e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(xPosition, yPosition, 40, 40), Color.Red);

    foreach (HudIconRow row in _iconRows)
    {
      int xPosition = Tools.GetWidthInPlayArea() - (70 + Config.HudIconsHorizontalOffset);
      var idx = 0;

      if (shouldOffsetForQuestLog)
      {
        xPosition -= 65;
      }

      foreach (ClickableIcon clickableIcon in row.Icons)
      {
        if (idx > 0)
        {
          xPosition -= clickableIcon.Dimensions.Width + Config.HudIconHorizontalSpacing;
        }

        int baselineHeight = heightOffset + (row.MaxRowHeight - clickableIcon.Dimensions.Height);

        clickableIcon.MoveTo(xPosition, baselineHeight);
        clickableIcon.AutoDrawDelegate.Invoke(e.SpriteBatch);
        idx++;
      }

      heightOffset += row.MaxRowHeight + Config.HudIconVerticalSpacing;
    }
  }

  private void RenderHoverText(object? sender, RenderedHudEventArgs e)
  {
    foreach (ClickableIcon clickableIcon in _icons.Values)
    {
      clickableIcon.DrawHoverText(e.SpriteBatch);
    }
  }

  private void HandleButtonPress(object? sender, ButtonPressedEventArgs e)
  {
    foreach (ClickableIcon clickableIcon in _icons.Values)
    {
      clickableIcon.OnClick(sender, e);
    }
  }
#endregion
}
