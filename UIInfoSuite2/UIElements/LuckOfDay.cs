using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Modules;

namespace UIInfoSuite2.UIElements;

internal class LuckOfDay : HudIconModule
{
#region Properties
  private readonly PerScreen<Color> _color = new(() => new Color(Color.White.ToVector4()));

  private static readonly Color Luck1Color = new(87, 255, 106, 255);
  private static readonly Color Luck2Color = new(148, 255, 210, 255);
  private static readonly Color Luck3Color = new(246, 255, 145, 255);
  private static readonly Color Luck4Color = new(255, 255, 255, 255);
  private static readonly Color Luck5Color = new(255, 155, 155, 255);
  private static readonly Color Luck6Color = new(165, 165, 165, 204);
#endregion

#region Lifecycle
  private NewModConfig config;

  public LuckOfDay(IModEvents modEvents, IMonitor logger, NewModConfig config) : base(modEvents, logger)
  {
    this.config = config;
  }

  protected override ClickableIcon CreateIcon()
  {
    return new ClickableIcon(Game1.mouseCursors, new Rectangle(50, 428, 10, 14), 40);
  }

  public override bool ShouldEnable()
  {
    return config.ShowLuckIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    ModEvents.GameLoop.UpdateTicked += OnUpdateTicked;
  }

  public override void OnDisable()
  {
    base.OnDisable();
    ModEvents.GameLoop.UpdateTicked -= OnUpdateTicked;
  }
#endregion

#region Event subscriptions
  private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    CalculateLuck(e);
  }

  protected override void DrawIcon(SpriteBatch batch)
  {
    Icon.Draw(batch, _color.Value, 1f);
  }
#endregion

#region Logic
  private void CalculateLuck(UpdateTickedEventArgs e)
  {
    if (!e.IsMultipleOf(30)) // half second
    {
      return;
    }

    switch (Game1.player.DailyLuck)
    {
      // Spirits are very happy (FeelingLucky)
      case > 0.07:
        Icon.HoverText = I18n.LuckStatus1();
        _color.Value = Luck1Color;
        break;
      // Spirits are in good humor (LuckyButNotTooLucky)
      case <= 0.07 and > 0.02:
        Icon.HoverText = I18n.LuckStatus2();
        _color.Value = Luck2Color;

        break;
      // The spirits feel neutral
      case var l and >= -0.02 and <= 0.02 when l != 0:
        Icon.HoverText = I18n.LuckStatus3();
        _color.Value = Luck3Color;

        break;
      // The spirits feel absolutely neutral
      case 0:
        Icon.HoverText = I18n.LuckStatus4();
        _color.Value = Luck4Color;
        break;
      // The spirits are somewhat annoyed (NotFeelingLuckyAtAll)
      case < -0.02 and >= -0.07:
        Icon.HoverText = I18n.LuckStatus5();
        _color.Value = Luck5Color;

        break;
      // The spirits are very displeased (MaybeStayHome)
      case < -0.07:
        Icon.HoverText = I18n.LuckStatus6();
        _color.Value = Luck6Color;
        break;
    }

    // Rewrite the text, but keep the color
    if (config.ShowExactLuckValue)
    {
      Icon.HoverText = string.Format(I18n.DailyLuckValue(), Game1.player.DailyLuck.ToString("N3"));
    }
  }
#endregion
}
