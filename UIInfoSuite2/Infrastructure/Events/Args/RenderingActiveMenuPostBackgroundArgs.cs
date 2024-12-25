using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure.Events.Args;

public class RenderingActiveMenuPostBackgroundArgs(GameMenu menu, SpriteBatch spriteBatch) : EventArgs
{
  public GameMenu Menu = menu;
  public SpriteBatch SpriteBatch = spriteBatch;
}
