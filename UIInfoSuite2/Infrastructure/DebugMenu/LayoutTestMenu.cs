using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.DebugMenu;

/// <summary>
///   A scrollable test menu that renders a battery of layout containers, one per test case.
///   Open via the "uiis2_layout_test" console command.
/// </summary>
internal class LayoutTestMenu : IClickableMenu
{
  // ── Layout constants ──────────────────────────────────────────────────────
  private const int MenuPadding = 24;
  private const int LabelHeight = 30;
  private const int TestGap = 16;

  private const int TitleHeight = 45;

  // ── Palette ───────────────────────────────────────────────────────────────
  private static readonly Color Red = new(210, 75, 75);
  private static readonly Color Green = new(75, 190, 110);
  private static readonly Color Blue = new(75, 130, 210);
  private static readonly Color Yellow = new(210, 175, 55);
  private static readonly Color Purple = new(185, 95, 210);
  private static readonly Color Orange = new(215, 130, 55);
  private readonly List<TestEntry> _tests = [];
  private int _scrollOffset;
  private int _totalContentHeight;

  public LayoutTestMenu() : base(Math.Max(10, Game1.uiViewport.Width / 2 - 500), 40, 1000, Game1.uiViewport.Height - 80)
  {
    BuildTests();
    LayoutAll();
  }

  // ── Build test cases ──────────────────────────────────────────────────────

  private void BuildTests()
  {
    // ── 1. Row - Basic ────────────────────────────────────────────────────
    Add(
      "Row | spacing=8",
      LayoutContainer.Row("t-row-basic", 8, Box(50, 30, Red), Box(80, 30, Green), Box(60, 30, Blue))
    );

    // ── 2. Column - Basic ─────────────────────────────────────────────────
    Add(
      "Column | spacing=8",
      LayoutContainer.Column("t-col-basic", 8, Box(120, 20, Red), Box(120, 35, Green), Box(120, 25, Blue))
    );

    // ── 3-7. JustifyContent variants (fixed 500px row) ────────────────────
    (string name, JustifyContent value)[] justifyModes =
    [
      ("Center", JustifyContent.Center),
      ("End", JustifyContent.End),
      ("SpaceBetween", JustifyContent.SpaceBetween),
      ("SpaceAround", JustifyContent.SpaceAround),
      ("SpaceEvenly", JustifyContent.SpaceEvenly)
    ];
    foreach ((string name, JustifyContent value) in justifyModes)
    {
      LayoutContainer c = LayoutContainer.Row(
        $"t-jc-{name}",
        0,
        Box(80, 30, Red),
        Box(80, 30, Green),
        Box(80, 30, Blue)
      );
      c.JustifyContent = value;
      c.FixedWidth = 500;
      Add($"Row | JustifyContent={name}  (FixedWidth=500, spacing=0)", c);
    }

    // ── 8-11. AlignItems variants ─────────────────────────────────────────
    // Three boxes with different heights (20, 50, 30) to show cross-axis alignment.
    (string name, AlignItems value)[] alignModes =
    [
      ("Start", AlignItems.Start),
      ("Center", AlignItems.Center),
      ("End", AlignItems.End),
      ("Stretch", AlignItems.Stretch)
    ];
    foreach ((string name, AlignItems value) in alignModes)
    {
      LayoutContainer c = LayoutContainer.Row(
        $"t-ai-{name}",
        8,
        Box(50, 20, Red),
        Box(50, 50, Green),
        Box(50, 30, Blue)
      );
      c.AlignItems = value;
      Add($"Row | AlignItems={name}  (heights: 20 / 50 / 30)", c);
    }

    // ── 12. AlignSelf per-child override ──────────────────────────────────
    // A tall anchor box sets containerCross = 60. Four short boxes each use
    // a different AlignSelf value.
    {
      TestColorBox anchor = Box(30, 60, Yellow); // tallest — sets container height
      TestColorBox s1 = Box(50, 25, Red);        // AlignSelf = Start  (default)
      TestColorBox s2 = Box(50, 25, Green);      // AlignSelf = Center
      TestColorBox s3 = Box(50, 25, Blue);       // AlignSelf = End
      TestColorBox s4 = Box(50, 25, Purple);     // AlignSelf = Stretch

      s2.AlignSelf = AlignItems.Center;
      s3.AlignSelf = AlignItems.End;
      s4.AlignSelf = AlignItems.Stretch;

      LayoutContainer c = LayoutContainer.Row("t-alignself", 8, anchor, s1, s2, s3, s4);
      c.AlignItems = AlignItems.Start;
      Add("Row | AlignSelf: anchor(60) / Start / Center / End / Stretch", c);
    }

    // ── 13. FlexGrow (1 : 2 : 1) ─────────────────────────────────────────
    {
      TestColorBox b1 = Box(60, 30, Red);
      TestColorBox b2 = Box(60, 30, Green);
      TestColorBox b3 = Box(60, 30, Blue);
      b1.FlexGrow = 1f;
      b2.FlexGrow = 2f;
      b3.FlexGrow = 1f;
      LayoutContainer c = LayoutContainer.Row("t-flexgrow", 5, b1, b2, b3);
      c.FixedWidth = 500;
      Add("Row | FlexGrow 1:2:1  (natural=60px each, FixedWidth=500)", c);
    }

    // ── 14. FlexShrink (1 : 2) ────────────────────────────────────────────
    // Two boxes totalling 350px forced into a 200px container.
    {
      TestColorBox b1 = Box(175, 30, Red);
      TestColorBox b2 = Box(175, 30, Green);
      b1.FlexShrink = 1f;
      b2.FlexShrink = 2f;
      LayoutContainer c = LayoutContainer.Row("t-flexshrink", 0, b1, b2);
      c.FixedWidth = 200;
      Add("Row | FlexShrink 1:2  (natural=175px each, FixedWidth=200)", c);
    }

    // ── 15. FlexGrow=0 / FlexShrink=0 (no flex) ──────────────────────────
    {
      TestColorBox b1 = Box(80, 30, Red);
      TestColorBox b2 = Box(80, 30, Green);
      TestColorBox b3 = Box(80, 30, Blue);
      b1.FlexGrow = 0;
      b2.FlexGrow = 0;
      b3.FlexGrow = 0;
      b1.FlexShrink = 0;
      b2.FlexShrink = 0;
      b3.FlexShrink = 0;
      LayoutContainer c = LayoutContainer.Row("t-noflex", 5, b1, b2, b3);
      c.FixedWidth = 500;
      Add("Row | FlexGrow=0 / FlexShrink=0  (no flex, FixedWidth=500)", c);
    }

    // ── 16. RowReverse ────────────────────────────────────────────────────
    // DOM order: Red(1) Green(2) Blue(3). Reversed render: Blue Green Red.
    {
      var c = new LayoutContainer("t-rowrev");
      c.FlexDirection = FlexDirection.RowReverse;
      c.ComponentSpacing = 8;
      c.FixedWidth = 400;
      c.AddChildren(Box(70, 30, Red), Box(70, 30, Green), Box(70, 30, Blue));
      Add("RowReverse | DOM order: Red→Green→Blue, render: Blue←Green←Red  (FixedWidth=400)", c);
    }

    // ── 17. ColumnReverse ─────────────────────────────────────────────────
    {
      var c = new LayoutContainer("t-colrev");
      c.FlexDirection = FlexDirection.ColumnReverse;
      c.ComponentSpacing = 8;
      c.AddChildren(Box(100, 20, Red), Box(100, 25, Green), Box(100, 20, Blue));
      Add("ColumnReverse | DOM order: Red→Green→Blue, render: Blue↑Green↑Red", c);
    }

    // ── 18. Order property ────────────────────────────────────────────────
    // DOM order: Red(Order=2) Green(Order=0) Blue(Order=1)
    // Sorted render order: Green / Blue / Red
    {
      TestColorBox r = Box(70, 30, Red);
      TestColorBox g = Box(70, 30, Green);
      TestColorBox b = Box(70, 30, Blue);
      r.Order = 2;
      g.Order = 0;
      b.Order = 1;
      LayoutContainer c = LayoutContainer.Row("t-order", 8, r, g, b);
      Add("Row | Order: Red(2) Green(0) Blue(1) → renders: Green / Blue / Red", c);
    }

    // ── 19. Nested containers ─────────────────────────────────────────────
    {
      LayoutContainer leftCol = LayoutContainer.Column(
        "t-nest-left",
        4,
        Box(90, 18, Red),
        Box(90, 28, Orange),
        Box(90, 18, Yellow)
      );

      LayoutContainer rightCol = LayoutContainer.Column("t-nest-right", 4, Box(90, 30, Blue), Box(90, 20, Purple));

      LayoutContainer c = LayoutContainer.Row("t-nested", 12, leftCol, rightCol);
      Add("Nested | Row → [Column: Red/Orange/Yellow] + [Column: Blue/Purple]", c);
    }

    // ── 20. Absolute positioning ──────────────────────────────────────────
    // Flow: Green box at top-left. Absolute: Red box pinned to top-right corner.
    {
      TestColorBox flow = Box(80, 40, Green);

      TestColorBox absBox = Box(40, 40, Red);
      absBox.IsAbsolute = true;
      absBox.Position = new Insets { Top = 0, Right = 0 };

      var c = new LayoutContainer("t-abs");
      c.Direction = LayoutContainer.LayoutDirection.Row;
      c.FixedWidth = 300;
      c.FixedHeight = 60;
      c.AddChildren(flow, absBox);
      Add("Absolute | FixedWidth=300 FixedHeight=60, flow=Green(80×40), abs=Red(40×40) pinned top-right", c);
    }

    // ── 21. Margin & Padding ──────────────────────────────────────────────
    // All three boxes are 50×30. Red has no framing. Green has Margin=8 (outer gap).
    // Blue has Padding=8 (inner gap — drawn area shrinks, bounds grow).
    {
      TestColorBox noFrame = Box(50, 30, Red);
      TestColorBox withMargin = Box(50, 30, Green);
      TestColorBox withPadding = Box(50, 30, Blue);

      withMargin.Margin.SetAll(8);
      withPadding.Padding.SetAll(8);

      LayoutContainer c = LayoutContainer.Row("t-margin-pad", 4, noFrame, withMargin, withPadding);
      Add("Margin & Padding | none / Margin=8 / Padding=8  (all 50×30 content)", c);
    }

    // ── 22. AutoHideWhenEmpty ─────────────────────────────────────────────
    // Left container: AutoHide=true, all children hidden → collapses to nothing.
    // Right container: AutoHide=true, one child visible → renders normally.
    {
      TestColorBox hiddenChild1 = Box(60, 30, Red);
      TestColorBox hiddenChild2 = Box(60, 30, Green);
      hiddenChild1.IsHidden = true;
      hiddenChild2.IsHidden = true;

      var autoHideEmpty = new LayoutContainer("t-autohide-empty");
      autoHideEmpty.AutoHideWhenEmpty = true;
      autoHideEmpty.Direction = LayoutContainer.LayoutDirection.Row;
      autoHideEmpty.ComponentSpacing = 4;
      autoHideEmpty.AddChildren(hiddenChild1, hiddenChild2);

      TestColorBox visibleChild = Box(60, 30, Blue);
      TestColorBox hiddenPurple = Box(60, 30, Purple);
      hiddenPurple.IsHidden = true;
      var autoHideVisible = new LayoutContainer("t-autohide-vis");
      autoHideVisible.AutoHideWhenEmpty = true;
      autoHideVisible.Direction = LayoutContainer.LayoutDirection.Row;
      autoHideVisible.ComponentSpacing = 4;
      autoHideVisible.AddChildren(hiddenPurple, visibleChild);

      LayoutContainer outer = LayoutContainer.Row("t-autohide", 20, autoHideEmpty, autoHideVisible);
      Add("AutoHideWhenEmpty | left=all-hidden (empty gap), right=one-visible", outer);
    }

    // ── 23. 9-Grid Alignment shorthand ────────────────────────────────────
    // Container with FixedWidth and FixedHeight. Children placed via Alignment enum.
    {
      var c = new LayoutContainer("t-9grid");
      c.Direction = LayoutContainer.LayoutDirection.Row;
      c.FixedWidth = 400;
      c.FixedHeight = 80;
      c.Alignment = Alignment.Center;
      c.ComponentSpacing = 6;
      c.AddChildren(Box(60, 30, Red), Box(60, 30, Green), Box(60, 30, Blue));
      Add("9-Grid Alignment=Center | FixedWidth=400 FixedHeight=80, children centered", c);
    }
  }

  // ── Helpers ────────────────────────────────────────────────────────────────

  private static TestColorBox Box(int w, int h, Color color)
  {
    return new TestColorBox(w, h, color);
  }

  private void Add(string label, LayoutContainer container)
  {
    _tests.Add(new TestEntry(label, container));
  }

  private void LayoutAll()
  {
    _totalContentHeight = 0;
    foreach (TestEntry test in _tests)
    {
      test.Container.Layout();
      _totalContentHeight += LabelHeight + test.Container.Bounds.Height + TestGap;
    }
  }

  // ── IClickableMenu overrides ───────────────────────────────────────────────

  public override void draw(SpriteBatch b)
  {
    // Dim the background
    b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

    // Menu background
    drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

    // Title
    var title = "Layout System Test Menu  |  scroll: mouse wheel  |  close: Escape";
    b.DrawString(
      Game1.smallFont,
      title,
      new Vector2(xPositionOnScreen + MenuPadding, yPositionOnScreen + MenuPadding),
      Color.DimGray
    );

    // Separator
    int titleBottom = yPositionOnScreen + MenuPadding + TitleHeight;
    b.Draw(
      Game1.staminaRect,
      new Rectangle(xPositionOnScreen + MenuPadding, titleBottom - 4, width - MenuPadding * 2, 1),
      Color.Gray * 0.5f
    );

    // Scrollable content area
    int contentLeft = xPositionOnScreen + MenuPadding;
    int contentTop = titleBottom;
    int contentBottom = yPositionOnScreen + height - MenuPadding;
    int visibleHeight = contentBottom - contentTop;

    int y = contentTop - _scrollOffset;

    foreach (TestEntry test in _tests)
    {
      int containerH = test.Container.Bounds.Height;
      int totalH = LabelHeight + containerH + TestGap;

      // Skip if completely off-screen
      if (y + totalH < contentTop || y > contentBottom)
      {
        y += totalH;
        continue;
      }

      // Test group background
      b.Draw(
        Game1.staminaRect,
        new Rectangle(contentLeft - 4, y - 2, width - MenuPadding * 2 + 8, totalH - TestGap + 4),
        new Color(30, 40, 60, 40)
      );

      // Label
      if (y + LabelHeight > contentTop && y < contentBottom)
      {
        b.DrawString(Game1.smallFont, test.Label, new Vector2(contentLeft, y + 2), Color.SlateGray);
      }

      // Container
      int containerY = y + LabelHeight;
      if (containerY < contentBottom && containerY + containerH > contentTop)
      {
        test.Container.Draw(b, contentLeft, containerY);
      }

      y += totalH;
    }

    // Scroll indicator (right edge)
    if (_totalContentHeight > visibleHeight)
    {
      float scrollFraction = (float)_scrollOffset / (_totalContentHeight - visibleHeight);
      int barH = Math.Max(20, (int)((float)visibleHeight / _totalContentHeight * visibleHeight));
      int barY = contentTop + (int)(scrollFraction * (visibleHeight - barH));

      b.Draw(
        Game1.staminaRect,
        new Rectangle(xPositionOnScreen + width - 10, contentTop, 6, visibleHeight),
        Color.Gray * 0.3f
      );
      b.Draw(Game1.staminaRect, new Rectangle(xPositionOnScreen + width - 10, barY, 6, barH), Color.Gray * 0.7f);
    }

    drawMouse(b);
  }

  public override void receiveScrollWheelAction(int direction)
  {
    int maxScroll = Math.Max(0, _totalContentHeight - (height - MenuPadding * 2 - TitleHeight));
    _scrollOffset = Math.Clamp(_scrollOffset - direction / 4, 0, maxScroll);
  }

  public override void receiveKeyPress(Keys key)
  {
    if (key == Keys.Escape)
    {
      exitThisMenu();
    }
  }

  // ── State ─────────────────────────────────────────────────────────────────
  private readonly record struct TestEntry(string Label, LayoutContainer Container);
}
