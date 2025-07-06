using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using UIInfoSuite2.Infrastructure.Extensions;

namespace UIInfoSuite2.Infrastructure.Models.ObjectRange;

internal class RangeCache
{
  private readonly Color _color = Color.White;
  private readonly Dictionary<Vector2, WorldObjectRange> _objects = new();
  private readonly Dictionary<Vector2, Counter<string>> _tiles = new();
  private readonly Dictionary<string, Counter<Vector2>> _tilesByLayer = new();

  public void Add(IEnumerable<Vector2> tiles, string type)
  {
    foreach (Vector2 tile in tiles)
    {
      _tiles.GetOrCreate(tile).Add(type);
      _tilesByLayer.GetOrCreate(type).Add(tile);
    }
  }

  public void Add(WorldObjectRange objectRange)
  {
    _objects[objectRange.CenterTile] = objectRange;
    Add(objectRange.Tiles, objectRange.Type);
  }

  public bool HasObjectRange(Vector2 centerTile)
  {
    return _objects.ContainsKey(centerTile);
  }

  public void Clear()
  {
    _tiles.Clear();
    _tilesByLayer.Clear();
    _objects.Clear();
  }

  public void DrawOverlay(SpriteBatch b, string overlayType)
  {
    var sourceRect = new Rectangle(194, 388, 16, 16);
    float tileToPixelScale = Utility.ModifyCoordinateFromUIScale(Game1.tileSize);
    float pixelToTileScale = Utility.ModifyCoordinateForUIScale(Game1.pixelZoom);
    foreach ((Vector2 tile, int count) in _tilesByLayer.GetOrCreate(overlayType).Pairs)
    {
      Vector2 position = tile * tileToPixelScale;
      b.Draw(
        Game1.mouseCursors,
        Utility.ModifyCoordinatesForUIScale(Game1.GlobalToLocal(Utility.ModifyCoordinatesForUIScale(position))),
        sourceRect,
        (count == 1 ? Color.White : Color.Red) * 0.7f,
        0.0f,
        Vector2.Zero,
        pixelToTileScale,
        SpriteEffects.None,
        0.01f
      );
    }
  }

  public void Draw(SpriteBatch batch)
  {
    List<string> layers = _tilesByLayer.Keys.ToList();
    layers.Sort((a, b) => OverlayType.GetLayer(a).CompareTo(OverlayType.GetLayer(b)));
    foreach (string overlayType in layers)
    {
      DrawOverlay(batch, overlayType);
    }
  }
}
