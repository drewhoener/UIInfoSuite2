using System.Collections.Generic;
using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData;

namespace UIInfoSuite2.Infrastructure.Helpers;

public enum Sounds
{
  LevelUp
}

public class SoundHelper
{
  private readonly Dictionary<string, AudioCueData> _cueData = new();
  private readonly IMonitor _logger;
  private readonly string _modId = "InfoSuite";

  public SoundHelper(IModHelper helper, IMonitor logger)
  {
    _logger = logger;
    _modId = helper.ModContent.ModID;

    RegisterSound(helper, Sounds.LevelUp, "LevelUp.wav");

    helper.Events.Content.AssetRequested += OnAssetRequested;
  }

  private string GetQualifiedSoundName(Sounds sound)
  {
    return $"{_modId}.sounds.{sound.ToString()}";
  }

  private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
  {
    if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
    {
      e.Edit(asset =>
        {
          IDictionary<string, AudioCueData> data = asset.AsDictionary<string, AudioCueData>().Data;
          data.TryAddMany(_cueData);
        }
      );
    }
  }

  private void RegisterSound(IModHelper helper, Sounds sound, string fileName, string category = "Sound")
  {
    string id = GetQualifiedSoundName(sound);
    _cueData[id] = new AudioCueData
    {
      Id = id,
      Category = category,
      FilePaths = [Path.Combine(helper.DirectoryPath, "assets", fileName)],
      StreamedVorbis = false,
      Looped = false,
      UseReverb = true
    };

    _logger.Log($"Registered Sound: {id}");
  }

  public void Play(Sounds sound)
  {
    Game1.playSound(GetQualifiedSoundName(sound));
  }
}
