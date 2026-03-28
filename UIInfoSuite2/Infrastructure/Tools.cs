using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Crops;
using StardewValley.GameData.FruitTrees;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using StardewValley.WorldMaps;
using UIInfoSuite2.Compatibility;
using SObject = StardewValley.Object;

namespace UIInfoSuite2.Infrastructure;

public static class Tools
{
  public static int GetWidthInPlayArea()
  {
    if (!Game1.isOutdoorMapSmallerThanViewport())
    {
      return Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
    }

    int right = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right;
    int totalWidth = Game1.currentLocation.map.Layers[0].LayerWidth * Game1.tileSize;
    int someOtherWidth = Game1.graphics.GraphicsDevice.Viewport.TitleSafeArea.Right - totalWidth;

    return right - someOtherWidth / 2;
  }

  public static int GetSellToStorePrice(Item item)
  {
    if (item is SObject obj)
    {
      return obj.sellToStorePrice();
    }

    return item.salePrice() / 2;
  }

  public static SObject? GetHarvest(Item item)
  {
    if (item is not SObject { Category: SObject.SeedsCategory } seedsObject || seedsObject.ItemId == Crop.mixedSeedsId)
    {
      return null;
    }

    if (seedsObject.IsFruitTreeSapling() && FruitTree.TryGetData(item.ItemId, out FruitTreeData? fruitTreeData))
    {
      // TODO support multiple items returned
      return ItemRegistry.Create<SObject>(fruitTreeData.Fruit[0].ItemId);
    }

    if (Crop.TryGetData(item.ItemId, out CropData cropData) && cropData.HarvestItemId is not null)
    {
      return ItemRegistry.Create<SObject>(cropData.HarvestItemId);
    }

    return null;
  }

  public static int GetHarvestPrice(Item item)
  {
    return GetHarvest(item)?.sellToStorePrice() ?? 0;
  }

  public static void DrawMouseCursor()
  {
    if (!Game1.options.hardwareCursor)
    {
      int mouseCursorToRender = Game1.options.gamepadControls ? Game1.mouseCursor + 44 : Game1.mouseCursor;
      Rectangle what = Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, mouseCursorToRender, 16, 16);

      Game1.spriteBatch.Draw(
        Game1.mouseCursors,
        new Vector2(Game1.getMouseX(), Game1.getMouseY()),
        what,
        Color.White,
        0.0f,
        Vector2.Zero,
        Game1.pixelZoom + Game1.dialogueButtonScale / 150.0f,
        SpriteEffects.None,
        1f
      );
    }
  }

  public static IClickableMenu? GetCurrentMenuPage(IClickableMenu? pMenu = null)
  {
    IClickableMenu menu = pMenu ?? Game1.activeClickableMenu;

    if (menu is GameMenu gameMenu)
    {
      return gameMenu.GetCurrentPage();
    }

    var apiManager = ModEntry.GetSingleton<ApiManager>();
    return apiManager.GetApi(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgm) ? bgm.GetCurrentPage(menu) : null;
  }

  public static bool IsGameMenuOpen()
  {
    if (Game1.activeClickableMenu is GameMenu)
    {
      return true;
    }

    var apiManager = ModEntry.GetSingleton<ApiManager>();
    return apiManager.GetApi(ModCompat.BetterGameMenu, out IBetterGameMenuApi? bgm) &&
           bgm.IsMenu(Game1.activeClickableMenu);
  }

  public static Item? GetHoveredItem()
  {
    Item? hoverItem = null;
    IClickableMenu? page = GetCurrentMenuPage();

    if (Game1.activeClickableMenu == null && Game1.onScreenMenus != null)
    {
      hoverItem = Game1.onScreenMenus.OfType<Toolbar>().Select(tb => tb.hoverItem).FirstOrDefault(hi => hi is not null);
    }

    switch (page)
    {
      case InventoryPage inventory:
        hoverItem = inventory.hoveredItem;
        break;
      case CraftingPage crafting:
        hoverItem = crafting.hoverItem;
        break;
      default:
      {
        if (Game1.activeClickableMenu is ItemGrabMenu itemMenu)
        {
          hoverItem = itemMenu.hoveredItem;
        }

        break;
      }
    }

    return hoverItem;
  }

  public static void GetSubTexture(Color[] output, Color[] originalColors, Rectangle sourceBounds, Rectangle clipArea)
  {
    if (output.Length < clipArea.Width * clipArea.Height)
    {
      return;
    }

    var dest = 0;
    for (var yOffset = 0; yOffset < clipArea.Height; yOffset++)
    {
      for (var xOffset = 0; xOffset < clipArea.Width; xOffset++)
      {
        int idx = clipArea.X + xOffset + sourceBounds.Width * (yOffset + clipArea.Y);
        output[dest++] = originalColors[idx];
      }
    }
  }

  public static void SetSubTexture(
    Color[] sourceColors,
    Color[] destColors,
    int destWidth,
    Rectangle destBounds,
    bool overlay = false
  )
  {
    if (sourceColors.Length > destColors.Length || destBounds.Width * destBounds.Height > destColors.Length)
    {
      return;
    }

    var emptyColor = new Color(0, 0, 0, 0);
    var srcIdx = 0;
    for (var yOffset = 0; yOffset < destBounds.Height; yOffset++)
    {
      for (var xOffset = 0; xOffset < destBounds.Width; xOffset++)
      {
        int idx = destBounds.X + xOffset + destWidth * (yOffset + destBounds.Y);
        Color sourcePixel = sourceColors[srcIdx++];

        // If using overlay mode, don't copy transparent pixels
        if (overlay && emptyColor.Equals(sourcePixel))
        {
          continue;
        }

        destColors[idx] = sourcePixel;
      }
    }
  }

  public static void CopySection(
    Texture2D sourceTexture,
    Texture2D destinationTexture,
    Rectangle sourceRectangle,
    Point destinationPosition,
    bool overlayTransparent = false
  )
  {
    // Ensure the source rectangle is within the bounds of the source texture
    if (sourceRectangle.X < 0 ||
        sourceRectangle.Y < 0 ||
        sourceRectangle.X + sourceRectangle.Width > sourceTexture.Width ||
        sourceRectangle.Y + sourceRectangle.Height > sourceTexture.Height)
    {
      throw new ArgumentOutOfRangeException(
        nameof(sourceRectangle),
        "Source rectangle is out of bounds of the source texture."
      );
    }

    // Ensure the destination rectangle is within the bounds of the destination texture
    if (destinationPosition.X < 0 ||
        destinationPosition.Y < 0 ||
        destinationPosition.X + sourceRectangle.Width > destinationTexture.Width ||
        destinationPosition.Y + sourceRectangle.Height > destinationTexture.Height)
    {
      throw new ArgumentOutOfRangeException(
        nameof(destinationPosition),
        "Destination position is out of bounds of the destination texture."
      );
    }

    var emptyColor = new Color(0, 0, 0, 0);
    var sourceData = new Color[sourceRectangle.Width * sourceRectangle.Height];
    sourceTexture.GetData(0, sourceRectangle, sourceData, 0, sourceData.Length);

    // Extract the color data from the destination texture
    var destinationData = new Color[destinationTexture.Width * destinationTexture.Height];
    destinationTexture.GetData(destinationData);

    // Copy the source data into the destination data at the specified position
    for (var y = 0; y < sourceRectangle.Height; y++)
    {
      for (var x = 0; x < sourceRectangle.Width; x++)
      {
        int destIndex = (destinationPosition.Y + y) * destinationTexture.Width + destinationPosition.X + x;
        int sourceIndex = y * sourceRectangle.Width + x;

        Color sourcePixel = sourceData[sourceIndex];

        // If using overlay mode, don't copy transparent pixels
        if (overlayTransparent && emptyColor.Equals(sourcePixel))
        {
          continue;
        }

        destinationData[destIndex] = sourcePixel;
      }
    }

    // Set the modified color data back to the destination texture
    destinationTexture.SetData(destinationData);
  }

  public static IEnumerable<int> GetDaysFromCondition(GameStateQuery.ParsedGameStateQuery parsedGameStateQuery)
  {
    HashSet<int> days = new();
    if (parsedGameStateQuery.Query.Length < 2)
    {
      return days;
    }

    string queryStr = parsedGameStateQuery.Query[0];
    if (!"day_of_month".Equals(queryStr, StringComparison.OrdinalIgnoreCase))
    {
      return days;
    }

    for (var i = 1; i < parsedGameStateQuery.Query.Length; i++)
    {
      string dayStr = parsedGameStateQuery.Query[i];
      if ("even".Equals(dayStr, StringComparison.OrdinalIgnoreCase))
      {
        days.AddRange(Enumerable.Range(1, 28).Where(x => x % 2 == 0));
        continue;
      }

      if ("odd".Equals(dayStr, StringComparison.OrdinalIgnoreCase))
      {
        days.AddRange(Enumerable.Range(1, 28).Where(x => x % 2 != 0));
        continue;
      }

      try
      {
        int parsedInt = int.Parse(dayStr);
        days.Add(parsedInt);
      }
      catch (Exception)
      {
        // ignored
      }
    }

    return parsedGameStateQuery.Negated ? Enumerable.Range(1, 28).Where(x => !days.Contains(x)).ToHashSet() : days;
  }

  public static int? GetNextDayFromCondition(string? condition, bool includeToday = true)
  {
    HashSet<int> days = new();
    if (condition == null)
    {
      return null;
    }

    GameStateQuery.ParsedGameStateQuery[]? conditionEntries = GameStateQuery.Parse(condition);

    foreach (GameStateQuery.ParsedGameStateQuery parsedGameStateQuery in conditionEntries)
    {
      days.AddRange(GetDaysFromCondition(parsedGameStateQuery));
    }

    days.RemoveWhere(day => day < Game1.dayOfMonth || (!includeToday && day == Game1.dayOfMonth));

    return days.Count == 0 ? null : days.Min();
  }

  public static int? GetLastDayFromCondition(string? condition)
  {
    HashSet<int> days = new();
    if (condition == null)
    {
      return null;
    }

    GameStateQuery.ParsedGameStateQuery[]? conditionEntries = GameStateQuery.Parse(condition);

    foreach (GameStateQuery.ParsedGameStateQuery parsedGameStateQuery in conditionEntries)
    {
      days.AddRange(GetDaysFromCondition(parsedGameStateQuery));
    }

    return days.Count == 0 ? null : days.Max();
  }

  public static MapAreaPosition? GetMapPositionDataSafe(GameLocation location, Point position)
  {
    MapAreaPosition? mapAreaPosition = WorldMapManager.GetPositionData(location, position)?.Data;

    return mapAreaPosition ?? WorldMapManager.GetPositionData(Game1.getFarm(), Point.Zero)?.Data;
  }

  public static string Capitalize(string str)
  {
    if (string.IsNullOrEmpty(str))
    {
      return str;
    }

    return char.ToUpper(str[0]) + str.Substring(1);
  }

  public static string GetLocalizedSeasonName(Season season)
  {
    string seasonLocalName = Game1.content.LoadString("Strings\\StringsFromCSFiles:" + Utility.getSeasonKey(season));
    return Capitalize(seasonLocalName);
  }

  public static (int x, int y) CalculateTooltipPosition(
    int width,
    int height,
    int xOffset = 0,
    int yOffset = 0,
    int overrideX = -1,
    int overrideY = -1
  )
  {
    int x = overrideX != -1 ? overrideX : Game1.getOldMouseX() + 32 + xOffset;
    int y = overrideY != -1 ? overrideY : Game1.getOldMouseY() + 32 + yOffset;

    Rectangle safeArea = Utility.getSafeArea();

    // Adjust position if tooltip would go off screen
    if (x + width > safeArea.Right)
    {
      x = safeArea.Right - width;
      y += 16;
    }

    if (y + height > safeArea.Bottom)
    {
      x += 16;
      if (x + width > safeArea.Right)
      {
        x = safeArea.Right - width;
      }

      y = safeArea.Bottom - height;
    }

    return (x, y);
  }

  public static void DrawBoxOutline(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int lineWidth)
  {
    DrawBoxOutline(spriteBatch, rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, color, lineWidth);
  }

  public static void DrawBoxOutline(
    SpriteBatch spriteBatch,
    int x,
    int y,
    int width,
    int height,
    Color color,
    int lineWidth,
    bool dimensionsInternal = false
  )
  {
    int x2 = x + width - lineWidth;
    int y2 = y + height - lineWidth;


    spriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, lineWidth, height), color);
    spriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y, width, lineWidth), color);
    spriteBatch.Draw(Game1.staminaRect, new Rectangle(x2, y, lineWidth, height), color);
    spriteBatch.Draw(Game1.staminaRect, new Rectangle(x, y2, width, lineWidth), color);
  }

  public static bool IsMasteryLevel()
  {
    return Game1.player.Level >= 25;
  }
}
