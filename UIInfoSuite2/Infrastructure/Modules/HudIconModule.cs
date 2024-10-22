using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Infrastructure.Modules;

public abstract class HudIconModule : BaseModule
{
  private ClickableIcon? _icon;

  public HudIconModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager) : base(
    modEvents,
    logger,
    configManager
  ) { }

  protected ClickableIcon Icon
  {
    get
    {
      _icon ??= CreateIcon();

      return _icon;
    }
  }

  public override void OnEnable()
  {
    ModEvents.Input.ButtonPressed += Icon.OnClick;
    ModEvents.Display.RenderingHud += RenderHudIcon;
    ModEvents.Display.RenderedHud += RenderHoverText;
  }

  public override void OnDisable()
  {
    ModEvents.Input.ButtonPressed -= Icon.OnClick;
    ModEvents.Display.RenderingHud -= RenderHudIcon;
    ModEvents.Display.RenderedHud -= RenderHoverText;
  }

  protected abstract ClickableIcon CreateIcon();

  protected virtual bool ShouldDrawIcon()
  {
    return UIElementUtils.IsRenderingNormally();
  }

  protected virtual void DrawIcon(SpriteBatch batch)
  {
    Icon.Draw(batch);
  }

  /// Events *
  /// <summary>
  ///   Helper handler for rendering the icon on the UI Bar
  /// </summary>
  /// <param name="sender">Event sender, nullable</param>
  /// <param name="e">Event arguments</param>
  private void RenderHudIcon(object? sender, RenderingHudEventArgs e)
  {
    if (!ShouldDrawIcon())
    {
      return;
    }

    Point newIconPosition = IconHandler.Handler.GetNewIconPosition();
    Icon.MoveTo(newIconPosition);
    DrawIcon(e.SpriteBatch);
  }

  /// <summary>
  ///   Helper handler for rendering hover text for a bar element.
  /// </summary>
  /// <param name="sender">Event sender, nullable</param>
  /// <param name="e">Event arguments</param>
  private void RenderHoverText(object? sender, RenderedHudEventArgs e)
  {
    if (!ShouldDrawIcon())
    {
      return;
    }

    Icon.DrawHoverText(e.SpriteBatch);
  }
}
