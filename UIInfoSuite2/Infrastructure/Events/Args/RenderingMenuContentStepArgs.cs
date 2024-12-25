using System;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Menus;

namespace UIInfoSuite2.Infrastructure.Events.Args;

public class RenderingMenuContentStepArgs(IClickableMenu menu, SpriteBatch spriteBatch) : EventArgs
{
  public IClickableMenu Menu = menu;
  public SpriteBatch SpriteBatch = spriteBatch;
}
