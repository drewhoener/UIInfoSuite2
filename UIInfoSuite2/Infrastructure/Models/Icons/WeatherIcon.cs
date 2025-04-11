using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Extensions;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class WeatherIcon(bool isIslandWeather) : ClickableIcon(IconSheet.Value, new Rectangle(0, 0, 15, 15), 40)
{
  private const int WeatherSheetWidth = 15 * 4 + 18 * 3;
  private const int WeatherSheetHeight = 18;

  private static readonly Lazy<Texture2D> IconSheet = new(() => GenerateWeatherTexture(ModEntry.Instance.Helper));
  private bool _isRainyTomorrow;
  private readonly PerScreen<string> _lastWeatherValue = new(() => "");

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
    if (weatherStr.EqualsIgnoreCase(_lastWeatherValue.Value))
    {
      return;
    }

    _lastWeatherValue.Value = weatherStr;
    int baseOffset = isIslandWeather ? 60 : 0;
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
        if (isIslandWeather)
        {
          _isRainyTomorrow = false;
          return;
        }

        iconIndex = 2;
        HoverText = I18n.SnowNextDay();
        break;

      case Game1.weather_green_rain:
        iconIndex = isIslandWeather ? 2 : 3;
        HoverText = I18n.RainNextDay();
        break;

      default:
        _isRainyTomorrow = false;
        return;
    }

    _isRainyTomorrow = true;
    SetSourceBounds(new Rectangle(baseOffset + iconSize * iconIndex, 0, iconSize, iconSize));
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

    return _isRainyTomorrow && base._ShouldDraw();
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
    Texture2D weatherBorderTexture = Texture2D.FromFile(
      Game1.graphics.GraphicsDevice,
      Path.Combine(helper.DirectoryPath, "assets", "weatherbox.png")
    );
    var weatherBorderColors = new Color[15 * 15];
    var cursorColors = new Color[Game1.mouseCursors.Width * Game1.mouseCursors.Height];
    var cursorColors_1_6 = new Color[Game1.mouseCursors_1_6.Width * Game1.mouseCursors_1_6.Height];
    var bounds = new Rectangle(0, 0, Game1.mouseCursors.Width, Game1.mouseCursors.Height);
    var bounds_1_6 = new Rectangle(0, 0, Game1.mouseCursors_1_6.Width, Game1.mouseCursors_1_6.Height);
    weatherBorderTexture.GetData(weatherBorderColors);
    Game1.mouseCursors.GetData(cursorColors);
    Game1.mouseCursors_1_6.GetData(cursorColors_1_6);
    var subTextureColors = new Color[15 * 15];

    // Copy over the bits we want
    // Border from TV screen
    Tools.GetSubTexture(
      subTextureColors,
      weatherBorderColors,
      new Rectangle(0, 0, 15, 15),
      new Rectangle(0, 0, 15, 15)
    );
    // Copy to each destination
    for (var i = 0; i < 4; i++)
    {
      Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(i * 15, 0, 15, 15));
    }

    // Add in expanded sprites for the island parrot
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(60, 0, 15, 15));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(78, 0, 15, 15));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(96, 0, 15, 15));

    subTextureColors = new Color[13 * 13];
    // Rainy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(504, 333, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(1, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(61, 1, 13, 13));

    // Stormy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(426, 346, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(16, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(79, 1, 13, 13));

    // Snowy Weather
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(465, 346, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(31, 1, 13, 13));

    // Green Rain
    Tools.GetSubTexture(subTextureColors, cursorColors_1_6, bounds_1_6, new Rectangle(178, 363, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(46, 1, 13, 13));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(97, 1, 13, 13));

    // Size of the parrot icon
    subTextureColors = new Color[9 * 14];
    // Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(155, 148, 9, 14));
    Tools.GetSubTexture(subTextureColors, cursorColors, bounds, new Rectangle(146, 149, 9, 14));
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(69, 4, 9, 14), true);
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(87, 4, 9, 14), true);
    Tools.SetSubTexture(subTextureColors, weatherIconColors, WeatherSheetWidth, new Rectangle(105, 4, 9, 14), true);

    iconSheet.SetData(weatherIconColors);

    return iconSheet;
  }
}
