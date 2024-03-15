using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using UIInfoSuite2.Infrastructure;
using UIInfoSuite2.Infrastructure.Extensions;
using UIInfoSuite2.Options;
using UIInfoSuite2.UIElements.Base;
using UIInfoSuite2.UIElements.HudIcon;

namespace UIInfoSuite2.UIElements
{
    internal class ShowQueenOfSauceIcon : HudIconComponent
    {
        #region Properties

        private readonly Dictionary<string, string> _recipesByDescription = new();
        private Dictionary<string, string> _recipes = new();
        private CraftingRecipe? _todaysRecipe;

        private readonly PerScreen<bool> _drawQueenOfSauceIcon = new();

        #endregion

        #region Life cycle

        public ShowQueenOfSauceIcon(IModHelper helper, ModOptions options) : base(helper, options)
        {
        }

        protected override void Init()
        {
            base.Init();
            LoadRecipes();
        }

        protected override void OnEnable()
        {
            CheckForNewRecipe();
        }

        protected override bool ShouldEnable()
        {
            return Options.ShowWhenNewRecipesAreAvailable;
        }

        protected override void RegisterEvents()
        {
            Helper.Events.GameLoop.DayStarted += OnDayStarted;
            Helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            Helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        protected override void UnregisterEvents()
        {
            Helper.Events.GameLoop.DayStarted -= OnDayStarted;
            Helper.Events.GameLoop.UpdateTicked -= OnUpdateTicked;
            Helper.Events.GameLoop.SaveLoaded -= OnSaveLoaded;
        }

        protected override void SetUpOptions()
        {
            OptionElements.Add(
                new ModOptionsCheckbox(
                    new OptionStringWrapper(nameof(Options.ShowWhenNewRecipesAreAvailable)),
                    1,
                    (s, b) => OnOptionsChanged(s, b),
                    () => Options.ShowWhenNewRecipesAreAvailable,
                    v => Options.ShowWhenNewRecipesAreAvailable = v
                )
            );
        }

        #endregion

        #region Event subscriptions

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            CheckForNewRecipe();
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            CheckForNewRecipe();
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!e.IsOneSecond)
            {
                return;
            }

            if (!_drawQueenOfSauceIcon.Value || _todaysRecipe == null || !Game1.player.knowsRecipe(_todaysRecipe.name))
            {
                return;
            }

            // We must know the recipe, stop checking
            _drawQueenOfSauceIcon.Value = false;
        }

        #endregion

        #region Logic

        protected override List<AbstractHudIcon> GenerateIcons()
        {
            return new List<AbstractHudIcon>
            {
                new GenericHudIcon(
                    new Point(40, 40),
                    Game1.mouseCursors,
                    new Rectangle(609, 361, 28, 28),
                    1.3f,
                    () => _drawQueenOfSauceIcon.Value,
                    GetRecipeHoverText
                )
            };
        }

        private string GetRecipeHoverText()
        {
            return Helper.SafeGetString(LanguageKeys.TodaysRecipe) + _todaysRecipe?.DisplayName;
        }

        private void LoadRecipes()
        {
            if (_recipes.Count != 0)
            {
                return;
            }

            _recipes = Game1.content.Load<Dictionary<string, string>>(@"Data\TV\CookingChannel");
            IEnumerable<string[]> rawRecipes = _recipes
                .Select(next => next.Value.Split('/'))
                .Where(values => values.Length > 1);

            foreach (string[] values in rawRecipes)
            {
                _recipesByDescription[values[1]] = values[0];
            }
        }

        private void CheckForNewRecipe()
        {
            _todaysRecipe = GetTodaysRecipe();
            if (_todaysRecipe == null)
            {
                _drawQueenOfSauceIcon.Value = false;
                return;
            }

            _drawQueenOfSauceIcon.Value = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                                          Game1.stats.DaysPlayed > 5 &&
                                          !Game1.player.knowsRecipe(_todaysRecipe?.name);
        }

        private CraftingRecipe? GetTodaysRecipe()
        {
            // Invoke Call
            int recipesKnownBeforeTvCall = Game1.player.cookingRecipes.Count();
            MethodInfo? getRecipeMethod = typeof(TV).GetMethod(
                "getWeeklyRecipe",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            object? dialogueObject = getRecipeMethod?.Invoke(new TV(), null);
            if (dialogueObject is not string[] dialogue)
            {
                return null;
            }

            // Get recipe name from dict
            string recipeName = _recipesByDescription.SafeGet(dialogue[0], "");
            if (string.IsNullOrEmpty(recipeName))
            {
                return null;
            }

            var recipe = new CraftingRecipe(recipeName, true);

            // If this call resulted in the player learning the recipe, undo that.
            if (Game1.player.cookingRecipes.Count() > recipesKnownBeforeTvCall)
            {
                Game1.player.cookingRecipes.Remove(recipe.name);
            }

            return recipe;
        }

        #endregion
    }
}
