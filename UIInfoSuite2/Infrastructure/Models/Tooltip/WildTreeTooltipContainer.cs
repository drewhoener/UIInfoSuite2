using Netcode;
using StardewModdingAPI;
using StardewValley.GameData.WildTrees;
using StardewValley.TerrainFeatures;
using StardewValley.TokenizableStrings;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Tooltip.Base;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class WildTreeTooltipContainer : LayoutContainer
{
  private const string WildTreeNameField = "UIInfoSuite.ExtendedData/DisplayName";
  private const int MaxTreeGrowthStage = 5;

  private readonly TooltipText _treeDetailsElement = new("UIIS2::UnknownTree", 0.75f, identifier: "TreeDetails");

  private readonly TooltipText _treeNameElement = TooltipText.Bold("UIIS2::UnknownTree", identifier: "TreeName");
  private Tree? _tree;

  public WildTreeTooltipContainer(Tree? tree = null) : base("WildTreeTooltip")
  {
    Direction = LayoutDirection.Column;

    ComponentSpacing = 0;
    AddChildren(_treeNameElement, _treeDetailsElement);
    IsHidden = true;

    Tree = tree;
  }

  public Tree? Tree
  {
    get => _tree;
    set => SetTree(value);
  }

  private void WatchTreeBoolField(NetBool field, bool oldValue, bool newValue)
  {
    UpdateTree();
  }

  private void SetTree(Tree? tree)
  {
    if (tree == _tree)
    {
      return;
    }

    if (_tree != null)
    {
      _tree.stump.fieldChangeEvent -= WatchTreeBoolField;
      _tree.tapped.fieldChangeEvent -= WatchTreeBoolField;
    }

    _tree = tree;

    if (_tree != null)
    {
      _tree.stump.fieldChangeEvent += WatchTreeBoolField;
      _tree.tapped.fieldChangeEvent += WatchTreeBoolField;
    }

    UpdateTree();
  }

  private void UpdateTree()
  {
    if (Tree is null || Tree.tapped.Value)
    {
      IsHidden = true;
      return;
    }

    IsHidden = false;

    bool isStump = Tree.stump.Value;
    string treeTypeName = GetTreeTypeName(Tree);
    string stumpText = isStump ? $" ({I18n.Stump()})" : "";
    _treeNameElement.Text = $"{treeTypeName}{I18n.Tree()}{stumpText}";

    if (Tree.growthStage.Value >= MaxTreeGrowthStage)
    {
      _treeDetailsElement.IsHidden = true;
      return;
    }

    _treeDetailsElement.IsHidden = false;
    _treeDetailsElement.Text = $"{I18n.Stage()} {Tree.growthStage.Value} / {MaxTreeGrowthStage}";
  }

  // See: https://stardewvalleywiki.com/Trees
  private static string GetTreeTypeName(Tree tree)
  {
    switch (tree.treeType.Value)
    {
      case "1":
        return I18n.Oak();
      case "2":
        return I18n.Maple();
      case "3":
        return I18n.Pine();
      case "6":
        return I18n.Palm();
      case "7":
        return I18n.Mushroom();
      case "8":
        return I18n.Mahogany();
      case "9":
        return I18n.PalmJungle();
      case "10":
        return I18n.GreenRainType1();
      case "11":
        return I18n.GreenRainType2();
      case "12":
        return I18n.GreenRainType3();
      case "13":
        return I18n.Mystic();
    }

    // Try to get the tree from the wild tree data
    WildTreeData? data = tree.GetData();
    if (data.CustomFields.TryGetValue(WildTreeNameField, out string? value))
    {
      return TokenParser.ParseText(value);
    }

    ModEntry.Instance.Monitor.LogOnce(
      $"Unknown Tree \"{tree.treeType.Value}\"! If this is a modded tree, you should let the author know to implement the {WildTreeNameField} custom field for proper DisplayName support.",
      LogLevel.Alert
    );
    return $"Unknown (#{tree.treeType.Value})";
  }
}
