using Microsoft.Xna.Framework;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace UIInfoSuite2.Infrastructure.Models.Icons;

public class QueenOfSauceIcon() : ClickableIcon(Game1.mouseCursors, new Rectangle(609, 361, 28, 28), 40)
{
  private readonly PerScreen<bool> _knowsRecipe = new(() => false);
  private CraftingRecipe? _recipe;

  public CraftingRecipe? Recipe
  {
    get => _recipe;
    set
    {
      _recipe = value;
      if (_recipe is not null)
      {
        HoverText = I18n.TodaysRecipe() + _recipe.DisplayName;
      }
    }
  }

  private bool KnowsRecipe
  {
    get => _knowsRecipe.Value;
    set => _knowsRecipe.Value = value;
  }

  public void UpdateKnowsRecipeCheck()
  {
    bool prev = KnowsRecipe;
    KnowsRecipe = Recipe is null || Game1.player.knowsRecipe(Recipe.name);
    if (prev != KnowsRecipe)
    {
      ModEntry.Instance.Monitor.Log($"Player {Game1.player.Name} knows today's recipe, hiding icon.");
    }
  }

  public override bool ShouldDraw()
  {
    bool recipeAvailable = (Game1.dayOfMonth % 7 == 0 || (Game1.dayOfMonth - 3) % 7 == 0) &&
                           Game1.stats.DaysPlayed > 5 &&
                           Recipe is not null &&
                           !KnowsRecipe;
    return base.ShouldDraw() && recipeAvailable;
  }
}
