using System;
using StardewValley.TerrainFeatures;

namespace UIInfoSuite2.Infrastructure.Events.Args;

public class BushShakeItemArgs(Bush bush) : EventArgs
{
  public Bush Bush = bush;
}
