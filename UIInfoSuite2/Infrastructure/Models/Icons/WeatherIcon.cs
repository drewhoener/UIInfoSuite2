using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using UIInfoSuite2.Compatibility;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal readonly struct Texture2DData
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

internal record VanillaWeatherRecord(Point texturePos, string WeatherStr, string? IslandWeatherStr = null)
{
  public string GetWeatherStr(bool isIslandWeather)
  {
    return isIslandWeather || string.IsNullOrEmpty(IslandWeatherStr) ? WeatherStr : IslandWeatherStr;
  }
}

internal class WeatherIcon(bool isIslandWeather) : ClickableIcon(Game1.mouseCursors, new Rectangle(0, 0, 15, 15), 40)
{
  private static readonly Dictionary<string, VanillaWeatherRecord> VanillaWeatherData = new()
  {
    [Game1.weather_rain] = new VanillaWeatherRecord(new Point(504, 333), I18n.RainNextDay(), I18n.IslandRainNextDay()),
    [Game1.weather_lightning] = new VanillaWeatherRecord(
      new Point(426, 346),
      I18n.ThunderstormNextDay(), //
      I18n.IslandThunderstormNextDay()
    ),
    [Game1.weather_snow] = new VanillaWeatherRecord(new Point(465, 346), I18n.SnowNextDay()),
    [Game1.weather_green_rain] = new VanillaWeatherRecord(
      new Point(178, 363), // from mouseCursors_1_6
      I18n.RainNextDay()
    )
  };

  private static readonly Lazy<ApiManager> ApiManager = new(ModEntry.GetSingleton<ApiManager>);
  private IWeatherData? _lastCustomWeatherData;

  private string _lastWeatherValue = "";
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
    _shouldDisplayWeather = true;

    if (VanillaWeatherData.TryGetValue(weatherStr, out VanillaWeatherRecord? weatherRecord))
    {
      HoverText = weatherRecord.GetWeatherStr(isIslandWeather);
    }
    else
    {
      if (weatherData is null)
      {
        _shouldDisplayWeather = false;
        return;
      }

      weatherRecord = VanillaWeatherData[Game1.weather_rain];
      HoverText = weatherData.Forecast ?? $"Custom Weather: {weatherData.DisplayName}";
    }

    Texture2D tex = weatherStr == Game1.weather_green_rain ? Game1.mouseCursors_1_6 : Game1.mouseCursors;
    Point pos = weatherRecord.texturePos;
    int iconSize = isIslandWeather ? 18 : 15;

    if (weatherData != null && !string.IsNullOrEmpty(weatherData.TVTexture))
    {
      tex = ModEntry.Instance.Helper.GameContent.Load<Texture2D>(weatherData.TVTexture);
      pos = weatherData.TVSource;
      // The sun texture (and others) aren't quite centered on the first frame because of animations
      // Just use the second frame and hope that it's more centered than the first
      if (weatherData.TVFrames > 1)
      {
        pos.X += 13;
      }
    }

    BaseTexture.Value = GenerateCustomWeatherTexture(
      ModEntry.Instance.Helper,
      tex,
      new Rectangle(pos, new Point(13, 13)),
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
