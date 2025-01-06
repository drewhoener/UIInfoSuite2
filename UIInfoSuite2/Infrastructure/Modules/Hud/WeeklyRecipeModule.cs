using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;
using UIInfoSuite2.Compatibility;
using UIInfoSuite2.Infrastructure.Config;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Infrastructure.Models;
using UIInfoSuite2.Infrastructure.Models.Icons;
using UIInfoSuite2.Infrastructure.Modules.Base;

namespace UIInfoSuite2.Infrastructure.Modules.Hud;

// ReSharper disable once ClassNeverInstantiated.Global Instantiated by SimpleInjector
internal class WeeklyRecipeModule(
  IModEvents modEvents,
  IMonitor logger,
  ConfigManager configManager,
  HudIconStorage iconStorage
) : SingleHudIconModule<QueenOfSauceIcon>(modEvents, logger, configManager, iconStorage)
{
  private readonly Dictionary<string, string> _recipesByDescription = new();

  protected override string IconKey => "QueenOfSauce";

  public override bool ShouldEnable()
  {
    return Config.ShowQueenOfSauceIcon;
  }

  public override void OnEnable()
  {
    base.OnEnable();
    LoadRecipes();
    CheckForNewRecipe();

    ModEvents.GameLoop.DayStarted += DoRecipeCheck;
    ModEvents.GameLoop.SaveLoaded += DoRecipeCheck;
    ModEvents.GameLoop.OneSecondUpdateTicked += OnOneSecond;
  }

  public override void OnDisable()
  {
    ModEvents.GameLoop.DayStarted -= DoRecipeCheck;
    ModEvents.GameLoop.SaveLoaded -= DoRecipeCheck;
    ModEvents.GameLoop.OneSecondUpdateTicked -= OnOneSecond;
    base.OnDisable();
  }

  protected override QueenOfSauceIcon GenerateNewIcon()
  {
    return new QueenOfSauceIcon();
  }

  private void DoRecipeCheck(object? sender, EventArgs e)
  {
    CheckForNewRecipe();
  }

  private void OnOneSecond(object? sender, OneSecondUpdateTickedEventArgs e)
  {
    if (Icon.ShouldDraw())
    {
      Icon.UpdateKnowsRecipeCheck();
    }
  }

  private void LoadRecipes()
  {
    if (_recipesByDescription.Count != 0)
    {
      return;
    }

    var recipes = Game1.content.Load<Dictionary<string, string>>(@"Data\TV\CookingChannel");
    IEnumerable<string[]> parseableRecipes =
      recipes.Select(next => next.Value.Split('/')).Where(values => values.Length > 1);
    foreach (string[] values in parseableRecipes)
    {
      _recipesByDescription[values[1]] = values[0];
    }
  }

  private void CheckForNewRecipe()
  {
    Icon.Recipe = null;
    int recipesKnownBeforeTvCall = Game1.player.cookingRecipes.Count();
    string[] dialogue = new QueenOfSauceTv().GetWeeklyRecipe();
    string recipeName = _recipesByDescription.GetOrDefault(dialogue[0], "");
    if (recipeName == string.Empty)
    {
      return;
    }

    var recipe = new CraftingRecipe(recipeName, true);
    if (Game1.player.cookingRecipes.Count() > recipesKnownBeforeTvCall)
    {
      Game1.player.cookingRecipes.Remove(recipe.name);
    }

    Icon.Recipe = recipe;
  }

  private class QueenOfSauceTv : TV
  {
    public string[] GetWeeklyRecipe()
    {
      return base.getWeeklyRecipe();
    }
  }

#region Configuration Setup
  public override string GetConfigPage()
  {
    return ConfigPageNames.HudIcons;
  }

  public override string GetConfigSection()
  {
    return ConfigSectionNames.NotificationIcons;
  }

  public override string? GetSubHeader()
  {
    return Game1.content.LoadString("Strings\\StringsFromCSFiles:TV.cs.13114");
  }

  public override void AddConfigOptions(IGenericModConfigMenuApi modConfigMenuApi, IManifest manifest)
  {
    modConfigMenuApi.AddBoolOption(
      manifest,
      name: I18n.Gmcm_Modules_Icons_Recipes_Enable,
      tooltip: I18n.Gmcm_Modules_Icons_Recipes_Enable_Tooltip,
      getValue: () => Config.ShowQueenOfSauceIcon,
      setValue: value => Config.ShowQueenOfSauceIcon = value
    );
  }
#endregion
}
