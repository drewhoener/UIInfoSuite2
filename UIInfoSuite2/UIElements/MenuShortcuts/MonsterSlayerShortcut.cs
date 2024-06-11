using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure;

namespace UIInfoSuite2.UIElements.MenuShortcuts;

public class MonsterSlayerShortcut : BaseMenuShortcut
{
  private const float InitialHeight = 32;
  private const float InitialWidth = 38;

  private readonly PerScreen<ClickableTextureComponent?> _menuButton = new(() => null);

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

  private float ScaleFactor => RenderedHeight / InitialHeight;

  public override int RenderedWidth => (int)(InitialWidth * ScaleFactor);

  public override bool ShouldDraw => Game1.player.mailReceived.Contains("checkedMonsterBoard");

  public override void Draw(SpriteBatch batch, int xStart, int yStart)
  {
    _menuButton.Value ??= new ClickableTextureComponent(
      new Rectangle(xStart, yStart, RenderedWidth, RenderedHeight),
      _texture.Value,
      new Rectangle(0, 0, (int)InitialWidth, (int)InitialHeight),
      ScaleFactor
    );

    _menuButton.Value.bounds.X = xStart;
    _menuButton.Value.bounds.Y = yStart;

    _menuButton.Value.draw(batch);
  }

  public override void OnClick(object? sender, ButtonPressedEventArgs args)
  {
    if (args.Button != SButton.MouseLeft ||
        _menuButton.Value is null ||
        Game1.player.CursorSlotItem is not null ||
        Game1.activeClickableMenu is not GameMenu gameMenu ||
        gameMenu.currentTab == GameMenu.mapTab)
    {
      return;
    }

    Vector2 mouseCoords = Utility.ModifyCoordinatesForUIScale(new Vector2(Game1.getMouseX(), Game1.getMouseY()));
    if (!_menuButton.Value.containsPoint((int)mouseCoords.X, (int)mouseCoords.Y))
    {
      return;
    }

    Game1.RequireLocation<AdventureGuild>("AdventureGuild").showMonsterKillList();
  }

  public override void DrawHoverText(SpriteBatch batch)
  {
    if (_menuButton.Value is null || !_menuButton.Value.containsPoint(Game1.getMouseX(), Game1.getMouseY()))
    {
      return;
    }

    IClickableMenu.drawHoverText(batch, I18n.SlayerGoals(), Game1.dialogueFont);
  }
}
