using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Helpers;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Modules.MenuAdditions.ExtendedItemInfo;

using SObject = StardewValley.Object;

internal class ShippingBinIconElement : LayoutElement
{
  private static readonly Lazy<Texture2D> Texture = new(() => Game1.mouseCursors);
  private static readonly Rectangle BottomSourceRect = new(526, 218, 30, 22);
  private static readonly Rectangle TopSourceRect = new(134, 236, 30, 15);
  private readonly Dimensions _dimensions;
  private readonly AspectLockedDimensions _scaleHelper;

  public ShippingBinIconElement(int finalSize = 32, PrimaryDimension primaryDimension = PrimaryDimension.Width)
  {
    this.WithPadding(0);
    int targetSize = primaryDimension == PrimaryDimension.Width ? finalSize : finalSize - 2;
    _scaleHelper = new AspectLockedDimensions(BottomSourceRect, targetSize, primaryDimension);
    _dimensions = _scaleHelper.Bounds + new Dimensions(0, 2);
  }

  protected override Dimensions MeasureContent()
  {
    return _dimensions;
  }

  public static void DrawShippingBinIcon(SpriteBatch spriteBatch, int positionX, int positionY, float scale)
  {
    Vector2 position = new(positionX, positionY);
    spriteBatch.Draw(
      Texture.Value,
      position,
      BottomSourceRect,
      Color.White,
      0f,
      new Vector2(0, -2),
      scale,
      SpriteEffects.None,
      0.86f
    );
    spriteBatch.Draw(
      Texture.Value,
      position,
      TopSourceRect,
      Color.White,
      0f,
      Vector2.Zero,
      scale,
      SpriteEffects.None,
      0.86f
    );
  }

  protected override void DrawContent(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    DrawShippingBinIcon(spriteBatch, positionX, positionY, _scaleHelper.ScaleFactor);
  }
}

internal class DescriptionContainer : TooltipExtensionContainer
{
  internal static readonly Dictionary<BundleData, LayoutContainer> ContainerCache = new();
  private readonly BundleHelper _bundleHelper;
  private readonly List<LayoutContainer> _bundles = [];
  private readonly Lazy<LayoutContainer> _museumDonationContainer = new(CreateMuseumDonationContainer);

  private readonly Lazy<LibraryMuseum> _museumInstance =
    new(() => Game1.RequireLocation<LibraryMuseum>("ArchaeologyHouse"));

  private readonly Lazy<LayoutContainer> _shippingBinContainer = new(CreateShippingBinContainer);

  public DescriptionContainer(BundleHelper bundleHelper)
  {
    _bundleHelper = bundleHelper;
    Direction = LayoutDirection.Column;
    AutoHideWhenEmpty = true;
  }

  private static LayoutContainer CreateShippingBinContainer()
  {
    return CreateIconRow(new ShippingBinIconElement(), TooltipText.FromAchievement(34, "Full Shipment"));
  }

  private static LayoutContainer GetBundleContainer(BundleRequiredItem bundleDisplayData)
  {
    if (ContainerCache.TryGetValue(bundleDisplayData.Bundle, out LayoutContainer? container))
    {
      return container;
    }

    var bundleText = new TooltipText($"{bundleDisplayData.Bundle.DisplayName} ({bundleDisplayData.ItemData.Count})");

    LayoutContainer newContainer = CreateIconRow(TooltipIcon.FromBundle(bundleDisplayData, 32), bundleText);
    ContainerCache[bundleDisplayData.Bundle] = newContainer;

    return newContainer;
  }

  private static LayoutContainer CreateMuseumDonationContainer()
  {
    NPC? gunther = Game1.getCharacterFromName("Gunther");
    if (gunther == null)
    {
      ModEntry.Instance.Monitor.Log(
        "ExtendedItemInfo: Could not find Gunther in the game, creating a fake one for ourselves.",
        LogLevel.Warn
      );
      gunther = new NPC { Name = "Gunther", Age = 0, Sprite = new AnimatedSprite("Characters\\Gunther") };
    }

    // Gunther has no dedicated mugshot bounds in data, and the default bounds don't scale very well, so we'll use our own.
    // If at some point the Gunther icon doesn't render correctly, this is the first place to investigate (you're welcome future me)
    var guntherBounds = new Rectangle(0, 3, 16, 18);
    var icon = new TooltipIcon(gunther.Sprite.Texture, guntherBounds, 32);

    return CreateIconRow(icon, TooltipText.FromAchievement(5, "Complete Collection"));
  }

  private static LayoutContainer CreateIconRow(LayoutElement icon, TooltipText text)
  {
    LayoutElement iconElement = icon.WithPadding(0).WithMargin(0);
    if (iconElement is TooltipIcon simpleIcon)
    {
      simpleIcon.SetSize(32, PrimaryDimension.Width);
    }

    TooltipText textElement = text.SetFont(Game1.smallFont)
      .SetHasShadow(true)
      .SetShadowType(TooltipShadowType.ItemTooltip)
      .WithPadding(0)
      .WithMargin(0);

    LayoutContainer newContainer = Row(null, 10, iconElement, textElement).WithAlignment(Alignment.Center);
    return newContainer;
  }

  private void ClearBundles()
  {
    RemoveChildren();
  }

  protected override void OnItemChange(Item? hoveredItem)
  {
    ClearBundles();
    if (hoveredItem == null)
    {
      return;
    }

    bool notDonatedYet = _museumInstance.Value.isItemSuitableForDonation(hoveredItem);
    bool notShippedYet = hoveredItem is SObject hoveredObject &&
                         hoveredObject.countsForShippedCollection() &&
                         !Game1.player.basicShipped.ContainsKey(hoveredObject.ItemId) &&
                         hoveredObject.Type != "Fish" &&
                         hoveredObject.Category != SObject.skillBooksCategory;

    foreach (BundleRequiredItem bundleItemData in _bundleHelper.BundlesRequiringItem(hoveredItem))
    {
      LayoutContainer newContainer = GetBundleContainer(bundleItemData);
      _bundles.Add(newContainer);
      AddChildren(newContainer);
      IsHidden = false;
    }

    if (notShippedYet)
    {
      AddChildren(_shippingBinContainer.Value);
      IsHidden = false;
    }

    if (notDonatedYet)
    {
      AddChildren(_museumDonationContainer.Value);
      IsHidden = false;
    }
  }
}
