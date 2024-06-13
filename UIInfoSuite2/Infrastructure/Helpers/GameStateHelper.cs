using System.Collections.Generic;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Helpers;

class ConditionFutureEval
{
  private GameStateQuery.ParsedGameStateQuery[] _queries;
  private List<string> _reasonsInvalid = new();

  private int _nextValidDateThisSeason = -1;
  private int _lastValidDateThisSeason = -1;
  private Season? _nextValidSeason = null;
  private GameLocation? _requiredLocation = null;

  public bool IsDeterminedDate => _nextValidSeason is not null && _nextValidDateThisSeason != -1;
}

public class GameStateHelper
{

}
