using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class ToolIcon : ClickableIcon
{
  private readonly PerScreen<Tool?> _tool = new(() => null);

  public ToolIcon() : base(Game1.mouseCursors, new Rectangle(322, 498, 12, 12), 40)
  {
    UpdateTool();
  }

  public Tool? Tool => _tool.Value;

  public void UpdateTool()
  {
    if (_tool.Value == Game1.player.toolBeingUpgraded.Value)
    {
      return;
    }

    _tool.Value = Game1.player.toolBeingUpgraded.Value;
    if (Tool is null)
    {
      return;
    }

    ParsedItemData itemData = ItemRegistry.GetDataOrErrorItem(Tool.QualifiedItemId);
    BaseTexture.Value = itemData.GetTexture();
    SetSourceBounds(itemData.GetSourceRect());

    if (Game1.player.daysLeftForToolUpgrade.Value > 0)
    {
      HoverText = string.Format(
        I18n.DaysUntilToolIsUpgraded(),
        Game1.player.daysLeftForToolUpgrade.Value,
        Tool.DisplayName
      );
    }
    else
    {
      HoverText = string.Format(I18n.ToolIsFinishedBeingUpgraded(), Tool.DisplayName);
    }
  }

  protected override bool _ShouldDraw()
  {
    return Tool is not null;
  }
}
