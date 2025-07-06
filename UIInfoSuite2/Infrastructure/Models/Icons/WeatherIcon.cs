using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal struct Texture2DData
{
  public Texture2DData(Texture2D texture)
  {
    Texture = texture;
    var colors = new Color[texture.Width * texture.Height];
    Dimensions = new Rectangle(0, 0, texture.Width, texture.Height);
    Texture.GetData(colors);
    Colors = colors;
  }

  public Texture2D Texture { get; }
  public Color[] Colors { get; }
  public Rectangle Dimensions { get; }

  public void GetSubTexture(Color[] output, Rectangle clipArea)
  {
    Tools.GetSubTexture(output, Colors, Dimensions, clipArea);
  }
}

internal class WeatherIcon(bool isIslandWeather) : ClickableIcon(IconSheet.Value, new Rectangle(0, 0, 15, 15), 40)
{
  private const int WeatherSheetWidth = 18 * 4;
  private const int WeatherSheetHeight = 18 * 2;

  private static readonly Lazy<Texture2D> IconSheet = new(() => GenerateWeatherTexture(ModEntry.Instance.Helper));
  private static readonly Lazy<ApiManager> ApiManager = new(ModEntry.GetSingleton<ApiManager>);

  private string _lastWeatherValue = "";
  private IWeatherData? _lastCustomWeatherData;
  private bool _shouldDisplayWeather;

  public void DoWeatherCheck()
  {
    if (isIslandWeather)
    {
      if (HasVisitedIsland())
      {
        UpdateIconForWeather(GetIslandWeatherForTomorrow());
      }

      return;
    }

    UpdateIconForWeather(GetValleyWeatherForTomorrow());
  }

  private void UpdateIconForWeather(string weatherStr)
  {

    IWeatherData? weatherData = null;
    if (ApiManager.Value.GetApi(ModCompat.CloudySkies, out ICloudySkiesApi? cloudySkiesApi))
    {
      cloudySkiesApi.TryGetWeather(weatherStr, out weatherData);
    }

    if (weatherStr.EqualsIgnoreCase(_lastWeatherValue) && _lastCustomWeatherData == weatherData)
    {
      return;
    }

    _lastCustomWeatherData = weatherData;
    _lastWeatherValue = weatherStr;
    int iconSize = isIslandWeather ? 18 : 15;
    int iconIndex;

    switch (weatherStr)
    {
      case Game1.weather_rain:
        iconIndex = 0;
        HoverText = isIslandWeather ? I18n.IslandRainNextDay() : I18n.RainNextDay();
        break;

      case Game1.weather_lightning:
        iconIndex = 1;
        HoverText = isIslandWeather ? I18n.IslandThunderstormNextDay() : I18n.ThunderstormNextDay();
        break;

      case Game1.weather_snow:
        iconIndex = 2;
        HoverText = I18n.SnowNextDay();
        break;

      case Game1.weather_green_rain:
        iconIndex = 3;
        HoverText = I18n.RainNextDay();
        break;
      default:
        if (weatherData is null)
        {
          _shouldDisplayWeather = false;
          return;
        }

        iconIndex = 0;
        HoverText = weatherData.Forecast ?? $"Custom Weather: {weatherData.DisplayName}";

        break;
    }

    _shouldDisplayWeather = true;
    if (weatherData is null || string.IsNullOrEmpty(weatherData.TVTexture))
    {
      BaseTexture.Value = IconSheet.Value;
      SetSourceBounds(new Rectangle(iconSize * iconIndex, isIslandWeather ? 18 : 0, iconSize, iconSize));
      return;
    }

    var customTexture = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(weatherData.TVTexture);
    BaseTexture.Value = GenerateCustomWeatherTexture(
      ModEntry.Instance.Helper,
      customTexture,
      new Rectangle(weatherData.TVSource, new Point(13, 13)),
      isIslandWeather
    );
    SetSourceBounds(new Rectangle(0, 0, iconSize, iconSize));
  }

  private static bool HasVisitedIsland()
  {
    return Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed");
  }

  private static string GetValleyWeatherForTomorrow()
  {
    var date = new WorldDate(Game1.Date);
    ++date.TotalDays;
    string tomorrowWeather = Game1.IsMasterGame
      ? Game1.weatherForTomorrow
      : Game1.netWorldState.Value.WeatherForTomorrow;
    return Game1.getWeatherModificationsForDate(date, tomorrowWeather);
  }

  private static string GetIslandWeatherForTomorrow()
  {
    return Game1.netWorldState.Value.GetWeatherForLocation("Island").WeatherForTomorrow;
  }

  protected override bool _ShouldDraw()
  {
    if (isIslandWeather && !HasVisitedIsland())
    {
      return false;
    }

    if (isIslandWeather && !Config.ShowIslandWeather)
    {
      return false;
    }

    return _shouldDisplayWeather && base._ShouldDraw();
  }

  /// <summary>
  ///   Creates a custom tilesheet for weather icons.
  ///   Meant to mimic the TV screen, which has a border around it, while the individual icons in the Cursors tilesheet
  ///   don't have a border
  ///   Extracts the border, and each individual weather icon and stitches them together into one separate sheet
  /// </summary>
  private static Texture2D GenerateWeatherTexture(IModHelper helper)
  {
    // Setup Texture sheet as a copy, so as not to disturb existing sprites
    Color[] weatherIconColors;
    var iconSheet = new Texture2D(Game1.graphics.GraphicsDevice, WeatherSheetWidth, WeatherSheetHeight);
    weatherIconColors = new Color[WeatherSheetWidth * WeatherSheetHeight];
    // Use our own texture in case the game's has been overwritten by a content pack.
    // Notably happens with the Cat TV Mod
    var weatherBorderData = new Texture2DData(
      Texture2D.FromFile(Game1.graphics.GraphicsDevice, Path.Combine(helper.DirectoryPath, "assets", "weatherbox.png"))
    );
    var cursorsData = new Texture2DData(Game1.mouseCursors);
    // ReSharper disable once InconsistentNaming
    var cursorsData_1_6 = new Texture2DData(Game1.mouseCursors_1_6);
    var subTextureColors = new Color[15 * 15];
    var dims = new Point(13, 13);
    var offset = new Point(1, 1);

    // Copy over the bits we want
    // Border from TV screen
    weatherBorderData.GetSubTexture(subTextureColors, new Rectangle(0, 0, 15, 15));
    // Copy to each destination on row 1 (18x18 grid alignment)
    for (var i = 0; i < 4; i++)
    {
      Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(i * 18, 0, 15, 15));
      Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(i * 18, 18, 15, 15));
    }

    subTextureColors = new Color[13 * 13];
    // Rainy Weather - row 1, column 1 and island row 2, column 1
    cursorsData.GetSubTexture(subTextureColors, new Rectangle(504, 333, 13, 13));
    CopyToCell(new Point(0, 0));
    CopyToCell(new Point(0, 1));

    // Stormy Weather - row 1, column 2 and island row 2, column 2
    cursorsData.GetSubTexture(subTextureColors, new Rectangle(426, 346, 13, 13));
    CopyToCell(new Point(1, 0));
    CopyToCell(new Point(1, 1));

    // Snowy Weather - row 1, column 3 (no island equivalent)
    cursorsData.GetSubTexture(subTextureColors, new Rectangle(465, 346, 13, 13));
    CopyToCell(new Point(2, 0));
    CopyToCell(new Point(2, 1));

    // Green Rain - row 1, column 4 and island row 2, column 3
    cursorsData_1_6.GetSubTexture(subTextureColors, new Rectangle(178, 363, 13, 13));
    CopyToCell(new Point(3, 0));
    CopyToCell(new Point(3, 1));

    // Size of the parrot icon
    subTextureColors = new Color[9 * 14];
    dims = new Point(9, 14);
    offset = new Point(9, 4);
    cursorsData.GetSubTexture(subTextureColors, new Rectangle(146, 149, 9, 14));
    CopyToCell(new Point(0, 1), true);
    CopyToCell(new Point(1, 1), true);
    CopyToCell(new Point(2, 1), true);
    CopyToCell(new Point(3, 1), true);

    iconSheet.SetData(weatherIconColors);

    return iconSheet;

    void CopyToCell(Point cell, bool overlay = false)
    {
      Point pos = cell * new Point(18, 18) + offset;
      Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(pos, dims), overlay);
    }
  }

  private static Texture2D GenerateCustomWeatherTexture(
    IModHelper helper,
    Texture2D customTexture,
    Rectangle customTextureSource,
    bool islandWeather
  )
  {
    const int iconSize = 18;
    // Setup Texture sheet as a copy, so as not to disturb existing sprites
    Color[] weatherIconColors;
    var iconSheet = new Texture2D(Game1.graphics.GraphicsDevice, iconSize, iconSize);
    var customTextureData = new Texture2DData(customTexture);
    var cursorsData = new Texture2DData(Game1.mouseCursors);
    weatherIconColors = new Color[iconSize * iconSize];
    // Use our own texture in case the game's has been overwritten by a content pack.
    // Notably happens with the Cat TV Mod
    var weatherBorderData = new Texture2DData(
      Texture2D.FromFile(Game1.graphics.GraphicsDevice, Path.Combine(helper.DirectoryPath, "assets", "weatherbox.png"))
    );
    var subTextureColors = new Color[15 * 15];

    // Copy over the bits we want
    // Border from TV screen
    weatherBorderData.GetSubTexture(subTextureColors, new Rectangle(0, 0, 15, 15));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, iconSize, new Rectangle(0, 0, 15, 15));
    // Clip the custom texture area
    customTextureData.GetSubTexture(subTextureColors, customTextureSource);
    Tools.SetSubTexture(subTextureColors, weatherIconColors, iconSize, new Rectangle(1, 1, 13, 13));

    // Size of the parrot icon
    if (islandWeather)
    {
      subTextureColors = new Color[9 * 14];
      cursorsData.GetSubTexture(subTextureColors, new Rectangle(146, 149, 9, 14));
      Tools.SetSubTexture(subTextureColors, weatherIconColors, iconSize, new Rectangle(9, 4, 9, 14), true);
    }

    iconSheet.SetData(weatherIconColors);

    return iconSheet;
  }
}
