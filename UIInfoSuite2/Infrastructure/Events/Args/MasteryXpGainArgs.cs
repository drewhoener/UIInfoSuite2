using System;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Events.Args;

public class MasteryXpGainArgs(Farmer player, int skillType, int oldXp, int newXp) : EventArgs
{
  public int NewXp = newXp;
  public int OldXp = oldXp;
  public Farmer Player = player;
  public int SkillType = skillType;
}
