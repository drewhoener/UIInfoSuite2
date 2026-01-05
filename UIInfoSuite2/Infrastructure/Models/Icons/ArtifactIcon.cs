using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TokenizableStrings;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

internal class ArtifactIcon() : ClickableIcon(ItemRegistry.GetData("(O)275"), 40)
{
  private int ArtifactCount { get; set; }

  private int SeedSpotCount { get; set; }

  private static string JoinLocationCounts(Dictionary<GameLocation, HashSet<Vector2>> dict)
  {
    List<string> locations = new(dict.Count);
    foreach ((GameLocation location, HashSet<Vector2> tiles) in dict)
    {
      if (tiles.Count <= 0)
      {
        continue;
      }

      string displayName = location is Farm ? location.Name : TokenParser.ParseText(location.DisplayName);
      locations.Add($"  {NormalizeUntranslated(displayName)}: {tiles.Count}");
    }

    return string.Join('\n', locations);
  }

  private static string NormalizeUntranslated(string str)
  {
    if (!str.StartsWith("(no translation:"))
    {
      return str;
    }

    string[] split = str.Split(':');
    return split[1].Trim(')');
  }

  public void UpdateText(
    Dictionary<GameLocation, HashSet<Vector2>> artifactSpots,
    Dictionary<GameLocation, HashSet<Vector2>> seedSpots
  )
  {
    ArtifactCount = artifactSpots.SelectMany(kvp => kvp.Value).Count();
    SeedSpotCount = seedSpots.SelectMany(kvp => kvp.Value).Count();

    var builder = new StringBuilder();
    if (ArtifactCount > 0)
    {
      builder.Append($"Artifact Spots: {ArtifactCount}\n");
      builder.Append(JoinLocationCounts(artifactSpots));
      if (SeedSpotCount > 0)
      {
        builder.Append("\n\n");
      }
    }

    if (SeedSpotCount > 0)
    {
      builder.Append($"Seed Spots: {SeedSpotCount}\n");
      builder.Append(JoinLocationCounts(seedSpots));
    }

    HoverText = builder.ToString();
  }

  protected override bool _ShouldDraw()
  {
    if (ArtifactCount > 0)
    {
      return true;
    }

    return SeedSpotCount > 0 && Config.ShowSeedSpotCount;
  }
}
