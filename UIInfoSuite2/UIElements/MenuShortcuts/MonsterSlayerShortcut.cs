using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public class MonsterSlayerShortcut : BaseMenuShortcut
{
  private const float InitialHeight = 32;
  private const float InitialWidth = 38;

  // Create our custom texture at some point
  private readonly Lazy<Texture2D> _texture = new(
    () =>
    {
      Texture2D customTexture = new(Game1.graphics.GraphicsDevice, (int)InitialWidth, (int)InitialHeight);
      customTexture.SetData(new Color[customTexture.Width * customTexture.Height]);
      var gilTexture = Game1.content.Load<Texture2D>(Path.Combine("Maps", "townInterior"));
      var papersTexture = Game1.content.Load<Texture2D>(Path.Combine("Maps", "townInterior_2"));


      Tools.CopySection(papersTexture, customTexture, new Rectangle(368, 576, 15, 32), new Point(0, 0));
      Tools.CopySection(gilTexture, customTexture, new Rectangle(183, 624, 19, 32), new Point(19, 0));

      return customTexture;
    }
  );

  public MonsterSlayerShortcut(int finalHeight) : base(finalHeight) { }

  public override int RenderedWidth => (int)(InitialWidth * ScaleFactor);

  protected override float ScaleFactor => RenderedHeight / InitialHeight;

  protected override Texture2D Texture => _texture.Value;
  protected override Rectangle SourceRectangle => new(0, 0, (int)InitialWidth, (int)InitialHeight);

  public override bool ShouldDraw => Game1.player.mailReceived.Contains("checkedMonsterBoard");

  protected override string GetHoverText()
  {
    return I18n.SlayerGoals();
  }

  protected override void HandleClickEvent(object? sender, ButtonPressedEventArgs args, Vector2 mouseCoords)
  {
    Game1.RequireLocation<AdventureGuild>("AdventureGuild").showMonsterKillList();
  }
}
