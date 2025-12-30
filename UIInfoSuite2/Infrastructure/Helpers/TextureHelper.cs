using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Helpers;

internal static class TextureHelper
{
  public static readonly Dictionary<int, Rectangle> SkillIconRectangles = new()
  {
    { Farmer.farmingSkill, new Rectangle(10, 428, 10, 10) },
    { Farmer.fishingSkill, new Rectangle(20, 428, 10, 10) },
    { Farmer.foragingSkill, new Rectangle(60, 428, 10, 10) },
    { Farmer.miningSkill, new Rectangle(30, 428, 10, 10) },
    { Farmer.combatSkill, new Rectangle(120, 428, 10, 10) },
    { Farmer.luckSkill, new Rectangle(50, 428, 10, 10) }
  };

  public static readonly Rectangle MasteryIconRectangle = new(346, 392, 8, 8);

  public static readonly Dictionary<int, Color> SkillFillColors = new()
  {
    { Farmer.farmingSkill, new Color(255, 251, 35, 0.38f) },
    { Farmer.fishingSkill, new Color(17, 84, 252, 0.63f) },
    { Farmer.foragingSkill, new Color(0, 234, 0, 0.63f) },
    { Farmer.miningSkill, new Color(145, 104, 63, 0.63f) },
    { Farmer.combatSkill, new Color(204, 0, 3, 0.63f) },
    { Farmer.luckSkill, new Color(232, 223, 42, 0.63f) }
  };

  public static readonly Rectangle OutlinedTextureBox = new(0, 256, 60, 60);
}
