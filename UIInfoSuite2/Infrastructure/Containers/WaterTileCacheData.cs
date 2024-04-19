using Microsoft.Xna.Framework;

namespace UIInfoSuite2.Infrastructure.Containers;

public record WaterTileCacheData(int X, int Y, int WaterDepth)
{
  public Vector2 BobberTile => new(X, Y);
}
