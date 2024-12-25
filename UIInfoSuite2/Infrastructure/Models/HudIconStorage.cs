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

namespace UIInfoSuite2.Infrastructure.Models;

public class HudIconStorage(IModEvents modEvents, IMonitor monitor, ConfigManager configManager, ApiManager apiManager)
{
  private readonly Dictionary<string, ClickableIcon> _icons = new();

  private readonly IMonitor _logger = monitor;
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
    return icon;
  }

  public int RemoveIconWhere(Func<KeyValuePair<string, ClickableIcon>, bool> match)
  {
    return _icons.RemoveWhere(match);
  }

#region Events
  private void RenderIcons(object? sender, RenderingHudEventArgs e)
  {
    int heightOffset = (Game1.options.zoomButtons ? 290 : 260) + Config.HudIconsVerticalOffset;

    // e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(xPosition, yPosition, 40, 40), Color.Red);
    IEnumerable<ClickableIcon[]> rows = _icons.Values.Where(icon => icon.ShouldDraw()).Chunk(Config.HudIconsPerRow);

    foreach (ClickableIcon[] row in rows)
    {
      int largestHeight = row.Max(icon => icon.Dimensions.HeightInt);
      int xPosition = Tools.GetWidthInPlayArea() - (70 + Config.HudIconsHorizontalOffset);
      var idx = 0;

      if (IconHandler.Handler.IsQuestLogPermanent ||
          Game1.player.questLog.Any() ||
          Game1.player.team.specialOrders.Any())
      {
        xPosition -= 65;
      }

      foreach (ClickableIcon clickableIcon in row)
      {
        if (idx > 0)
        {
          xPosition -= clickableIcon.Dimensions.WidthInt + Config.HudIconHorizontalSpacing;
        }

        largestHeight = Math.Max(clickableIcon.Dimensions.HeightInt, largestHeight);
        int baselineHeight = heightOffset + (largestHeight - clickableIcon.Dimensions.HeightInt);

        clickableIcon.MoveTo(xPosition, baselineHeight);
        clickableIcon.AutoDrawDelegate.Invoke(e.SpriteBatch);
        idx++;
      }

      heightOffset += largestHeight + Config.HudIconVerticalSpacing;
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
