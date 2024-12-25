using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Events;
using UIInfoSuite2.Infrastructure.Events.Args;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class ShopHarvestPriceModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  EventsManager eventsManager
) : BaseModule(modEvents, logger, configManager), IConfigurable
{
  public override bool ShouldEnable()
  {
    return Config.ShowHarvestPricesInShop;
  }

  public override void OnEnable()
  {
    eventsManager.OnRenderingMenuContentStep += OnRenderingContentStep;
  }

  public override void OnDisable()
  {
    eventsManager.OnRenderingMenuContentStep -= OnRenderingContentStep;
  }

  private static void OnRenderingContentStep(object? sender, RenderingMenuContentStepArgs e)
  {
    if (e.Menu is not ShopMenu { hoveredItem: Item hoverItem } menu)
    {
      return;
    }

    // draw shop harvest prices
    int value = Tools.GetHarvestPrice(hoverItem);

    if (value <= 0)
    {
      return;
    }

    int xPosition = menu.xPositionOnScreen - 30;
    int yPosition = menu.yPositionOnScreen + 580;
    IClickableMenu.drawTextureBox(Game1.spriteBatch, xPosition + 20, yPosition - 52, 264, 108, Color.White);
    // Title "Harvest Price"
    string textToRender = I18n.HarvestPrice();
    Game1.spriteBatch.DrawString(
      Game1.dialogueFont,
      textToRender,
      new Vector2(xPosition + 30, yPosition - 38),
      Color.Black * 0.2f
    );
    Game1.spriteBatch.DrawString(
      Game1.dialogueFont,
      textToRender,
      new Vector2(xPosition + 32, yPosition - 40),
      Color.Black * 0.8f
    );
    // Tree Icon
    xPosition += 80;
    Game1.spriteBatch.Draw(
      Game1.mouseCursors,
      new Vector2(xPosition, yPosition),
      new Rectangle(60, 428, 10, 10),
      Color.White,
      0,
      Vector2.Zero,
      Game1.pixelZoom,
      SpriteEffects.None,
      0.85f
    );
    //  Coin
    Game1.spriteBatch.Draw(
      Game1.debrisSpriteSheet,
      new Vector2(xPosition + 32, yPosition + 10),
      Game1.getSourceRectForStandardTileSheet(Game1.debrisSpriteSheet, 8, 16, 16),
      Color.White,
      0,
      new Vector2(8, 8),
      4,
      SpriteEffects.None,
      0.95f
    );
    // Price
    Game1.spriteBatch.DrawString(
      Game1.dialogueFont,
      value.ToString(),
      new Vector2(xPosition + 50, yPosition + 6),
      Color.Black * 0.2f
    );
    Game1.spriteBatch.DrawString(
      Game1.dialogueFont,
      value.ToString(),
      new Vector2(xPosition + 52, yPosition + 4),
      Color.Black * 0.8f
    );
  }

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.MenuFeatures;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_ShopFeatures();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable,
      tooltip: I18n.Gmcm_Modules_Menus_HarvestPrices_Enable_Tooltip,
      getValue: () => Config.ShowHarvestPricesInShop,
      setValue: value => Config.ShowHarvestPricesInShop = value
    );
  }
#endregion
}
