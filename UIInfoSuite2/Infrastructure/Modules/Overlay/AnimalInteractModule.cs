using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.GameData.FarmAnimals;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Locations;
using StardewValley.Network;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Interfaces;
using UIInfoSuite2.Infrastructure.Modules.Base;
using UIInfoSuite2.UIElements;

namespace UIInfoSuite2.Infrastructure.Modules.Overlay;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class AnimalInteractModule(IModEvents modEvents, IMonitor logger, ConfigManager configManager)
  : BaseModule(modEvents, logger, configManager), IConfigurable
{
  private readonly PerScreen<float> _alpha = new();
  private readonly PerScreen<float> _yMovementPerDraw = new();

  public override bool ShouldEnable()
  {
    return Config.ShowAnimalsNeedPets;
  }

  public override void OnEnable()
  {
    ModEvents.Display.RenderingHud += OnRenderingHud_DrawAnimalHasProduct;
    ModEvents.Display.RenderingHud += OnRenderingHud_DrawNeedsPetTooltip;
    ModEvents.GameLoop.UpdateTicked += UpdateTicked;
  }

  public override void OnDisable()
  {
    ModEvents.Display.RenderingHud -= OnRenderingHud_DrawAnimalHasProduct;
    ModEvents.Display.RenderingHud -= OnRenderingHud_DrawNeedsPetTooltip;
    ModEvents.GameLoop.UpdateTicked -= UpdateTicked;
  }

  private static bool CanRenderAnimalOverlay(bool allowFarmhouse = false)
  {
    if (!UIElementUtils.IsRenderingNormally() || Game1.activeClickableMenu != null)
    {
      return false;
    }

    GameLocation? currentLoc = Game1.currentLocation;
    if (currentLoc is FarmHouse && !allowFarmhouse)
    {
      return false;
    }

    return currentLoc is AnimalHouse or Farm;
  }

  private void OnRenderingHud_DrawNeedsPetTooltip(object? sender, RenderingHudEventArgs e)
  {
    if (!CanRenderAnimalOverlay(true))
    {
      return;
    }

    DrawIconForFarmAnimals();
    DrawIconForPets();
  }

  private void OnRenderingHud_DrawAnimalHasProduct(object? sender, RenderingHudEventArgs e)
  {
    if (CanRenderAnimalOverlay())
    {
      return;
    }

    DrawAnimalHasProduct();
  }

  private void UpdateTicked(object? sender, UpdateTickedEventArgs e)
  {
    if (!CanRenderAnimalOverlay(true))
    {
      return;
    }

    var sine = (float)Math.Sin(e.Ticks / 20.0);
    _yMovementPerDraw.Value = -6f + 6f * sine;
    _alpha.Value = 0.8f + 0.2f * sine;
  }

  private void DrawAnimalHasProduct()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animalsInCurrentLocation = GetAnimalsInCurrentLocation();
    if (animalsInCurrentLocation == null)
    {
      return;
    }

    foreach ((_, FarmAnimal animal) in animalsInCurrentLocation.Pairs)
    {
      FarmAnimalHarvestType? harvestType = animal.GetHarvestType();
      // Check to make sure the animal is grown, they have produce, and the produce is something that can be harvested
      // directly from them.
      string? currentProduce = animal.currentProduce.Value;
      bool hasAllowedProduce = currentProduce != null && currentProduce != "430"; // Truffle
      bool isGrown = animal.age.Value >= animal.GetAnimalData().DaysToMature;
      if (harvestType == FarmAnimalHarvestType.DropOvernight || animal.IsEmoting || !hasAllowedProduce || !isGrown)
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal);

      // Offset the produce bubble by some sinusoidal value dependent on render time.
      double totalMillis = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
      double sinOffset = Math.Sin(totalMillis / 300.0 + animal.Name.GetHashCode()) * 5.0;
      positionAboveAnimal.Y += (float) sinOffset;

      Game1.spriteBatch.Draw(
        Game1.emoteSpriteSheet,
        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 14f, positionAboveAnimal.Y)),
        new Rectangle(
          3 * (Game1.tileSize / 4) % Game1.emoteSpriteSheet.Width,
          3 * (Game1.tileSize / 4) / Game1.emoteSpriteSheet.Width * (Game1.tileSize / 4),
          Game1.tileSize / 4,
          Game1.tileSize / 4
        ),
        Color.White * 0.9f,
        0.0f,
        Vector2.Zero,
        4f,
        SpriteEffects.None,
        1f
      );

      ParsedItemData? produceData = ItemRegistry.GetData(currentProduce);
      Rectangle sourceRectangle = produceData.GetSourceRect();
      Game1.spriteBatch.Draw(
        produceData.GetTexture(),
        Utility.ModifyCoordinatesForUIScale(new Vector2(positionAboveAnimal.X + 28f, positionAboveAnimal.Y + 8f)),
        sourceRectangle,
        Color.White * 0.9f,
        0.0f,
        Vector2.Zero,
        2.2f,
        SpriteEffects.None,
        1f
      );
    }
  }

  /// <summary>
  ///   Used to determine if we need an offset because of the animal's sprite size
  /// </summary>
  /// <param name="animal"></param>
  /// <returns>If the animal type is a "big" animal</returns>
  private static bool IsLargeAnimal(FarmAnimal animal)
  {
    string animalType = animal.type.Value.ToLower();
    return animalType.Contains("cow") ||
           animalType.Contains("sheep") ||
           animalType.Contains("goat") ||
           animalType.Contains("pig");
  }

  private void DrawIconForFarmAnimals()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animalsInCurrentLocation = GetAnimalsInCurrentLocation();

    if (animalsInCurrentLocation == null)
    {
      return;
    }

    foreach ((_, FarmAnimal animal) in animalsInCurrentLocation.Pairs)
    {
      int animalFriendship = animal.friendshipTowardFarmer.Value;
      if (animal.IsEmoting || animal.wasPet.Value || (animalFriendship >= 1000 && Config.HideAnimalPetOnMaxFriendship))
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(animal);
      if (IsLargeAnimal(animal))
      {
        positionAboveAnimal.X += 50f;
        positionAboveAnimal.Y += 50f;
      }

      DrawPetHand(positionAboveAnimal);
    }
  }

  private void DrawIconForPets()
  {
    foreach (Pet pet in GetPetsInCurrentLocation())
    {
      // Disqualifying checks for the pet
      bool wasPetToday = pet.lastPetDay.Values.Any(day => day == Game1.Date.TotalDays);
      bool isMaxFriendship = pet.friendshipTowardFarmer.Value >= 1000;
      if (wasPetToday || (isMaxFriendship && Config.HideAnimalPetOnMaxFriendship))
      {
        continue;
      }

      Vector2 positionAboveAnimal = GetPetPositionAboveAnimal(pet);

      positionAboveAnimal.X += 50f;
      positionAboveAnimal.Y += 20f;
      DrawPetHand(positionAboveAnimal);
    }
  }

  private void DrawPetHand(Vector2 handPosition)
  {
    Game1.spriteBatch.Draw(
      Game1.mouseCursors,
      Utility.ModifyCoordinatesForUIScale(new Vector2(handPosition.X, handPosition.Y + _yMovementPerDraw.Value)),
      new Rectangle(32, 0, 16, 16),
      Color.White * _alpha.Value,
      0.0f,
      Vector2.Zero,
      4f,
      SpriteEffects.None,
      1f
    );
  }

  private static Vector2 GetPetPositionAboveAnimal(Character animal)
  {
    Vector2 animalPosition = animal.getLocalPosition(Game1.viewport);
    animalPosition.X += 10;
    animalPosition.Y -= 34;
    return animalPosition;
  }

  private static NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? GetAnimalsInCurrentLocation()
  {
    NetLongDictionary<FarmAnimal, NetRef<FarmAnimal>>? animals = Game1.currentLocation switch
    {
      AnimalHouse animalHouse => animalHouse.Animals,
      Farm farm => farm.Animals,
      _ => null
    };

    return animals;
  }

  private static IEnumerable<Pet> GetPetsInCurrentLocation()
  {
    return Game1.currentLocation.characters.Where(character => character is Pet).Cast<Pet>();
  }

#region Configuration Setup
  public string GetConfigPage()
  {
    return ConfigPageNames.Tooltips;
  }

  public string GetConfigSection()
  {
    return ConfigSectionNames.EmptySection;
  }

  public string GetSubHeader()
  {
    return I18n.Gmcm_Group_AnimalTooltips();
  }

  public void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Animals_Enable,
      tooltip: I18n.Gmcm_Modules_Tooltips_Animals_Enable_Tooltip,
      getValue: () => Config.ShowAnimalsNeedPets,
      setValue: value => Config.ShowAnimalsNeedPets = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Animals_HideOnFriends,
      tooltip: I18n.Gmcm_Modules_Tooltips_Animals_HideOnFriends_Tooltip,
      getValue: () => Config.HideAnimalPetOnMaxFriendship,
      setValue: value => Config.HideAnimalPetOnMaxFriendship = value
    );
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Tooltips_Animals_ShowProduce,
      tooltip: I18n.Gmcm_Modules_Tooltips_Animals_ShowProduce_Tooltip,
      getValue: () => Config.ShowAnimalProduceReady,
      setValue: value => Config.ShowAnimalProduceReady = value
    );
  }
#endregion
}
