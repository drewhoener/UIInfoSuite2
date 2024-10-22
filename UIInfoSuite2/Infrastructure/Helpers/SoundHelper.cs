﻿using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;
using StardewModdingAPI;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Helpers;

public enum Sounds
{
  LevelUp
}

public class SoundHelper
{
  private readonly IMonitor _logger;
  private bool _initialized;
  private string _modId = "InfoSuite";

  public SoundHelper(IMonitor logger)
  {
    _logger = logger;
  }

  public void Initialize(IModHelper helper)
  {
    if (_initialized)
    {
      throw new InvalidOperationException("Cannot re-initialize sound helper");
    }

    _modId = helper.ModContent.ModID;

    RegisterSound(helper, Sounds.LevelUp, "LevelUp.wav");

    _initialized = true;
  }

  private string GetQualifiedSoundName(Sounds sound)
  {
    return $"{_modId}.sounds.{sound.ToString()}";
  }

  private void RegisterSound(
    IModHelper helper,
    Sounds sound,
    string fileName,
    string category = "Sound",
    int instanceLimit = -1,
    CueDefinition.LimitBehavior? limitBehavior = null
  )
  {
    CueDefinition newCueDefinition = new() { name = GetQualifiedSoundName(sound) };

    if (instanceLimit > 0)
    {
      newCueDefinition.instanceLimit = instanceLimit;
      newCueDefinition.limitBehavior = limitBehavior ?? CueDefinition.LimitBehavior.ReplaceOldest;
    }
    else if (limitBehavior.HasValue)
    {
      newCueDefinition.limitBehavior = limitBehavior.Value;
    }

    SoundEffect audio;
    string filePath = Path.Combine(helper.DirectoryPath, "assets", fileName);
    using (var stream = new FileStream(filePath, FileMode.Open))
    {
      audio = SoundEffect.FromStream(stream);
    }

    newCueDefinition.SetSound(audio, Game1.audioEngine.GetCategoryIndex(category));
    Game1.soundBank.AddCue(newCueDefinition);
    _logger.Log($"Registered Sound: {newCueDefinition.name}");
  }

  public void Play(Sounds sound)
  {
    Game1.playSound(GetQualifiedSoundName(sound));
  }
}
