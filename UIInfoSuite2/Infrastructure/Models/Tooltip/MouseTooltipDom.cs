using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.TerrainFeatures;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class MouseTooltipDom : LayoutDom
{
  public MouseTooltipDom()
  {
    CropTooltipContainer = new CropTooltipContainer();
    BushTooltipContainer = new BushTooltipContainer();
    MachineTooltipContainer = new MachineTooltipContainer();
    BuildingTooltipContainer = new BuildingTooltipContainer();
    CropStatusContainer = new CropStatusContainer();
    WildTreeContainer = new WildTreeTooltipContainer();
    FruitTreeContainer = new FruitTreeTooltipContainer();

    Margin.SetAll(5);
    Padding.SetAll(15);
    Direction = LayoutDirection.Column;

    AddChildren(
      CropTooltipContainer,
      BushTooltipContainer,
      WildTreeContainer,
      FruitTreeContainer,
      CropStatusContainer,
      MachineTooltipContainer,
      BuildingTooltipContainer
    );
  }

  public CropTooltipContainer CropTooltipContainer { get; }

  public BushTooltipContainer BushTooltipContainer { get; }

  public WildTreeTooltipContainer WildTreeContainer { get; }

  public FruitTreeTooltipContainer FruitTreeContainer { get; }

  public CropStatusContainer CropStatusContainer { get; }

  public MachineTooltipContainer MachineTooltipContainer { get; }
  public BuildingTooltipContainer BuildingTooltipContainer { get; }

  public Crop? Crop
  {
    get => CropTooltipContainer.Crop;
    set => CropTooltipContainer.Crop = value;
  }

  public Tree? WildTree
  {
    get => WildTreeContainer.Tree;
    set
    {
      CropStatusContainer.Tree = value;
      WildTreeContainer.Tree = value;
    }
  }

  public FruitTree? FruitTree
  {
    get => FruitTreeContainer.FruitTree;
    set => FruitTreeContainer.FruitTree = value;
  }

  public HoeDirt? HoeDirt
  {
    get => CropStatusContainer.HoeDirt;
    set => CropStatusContainer.HoeDirt = value;
  }

  public Object? Machine
  {
    get => MachineTooltipContainer.Machine;
    set => MachineTooltipContainer.Machine = value;
  }

  public Building? Building
  {
    get => BuildingTooltipContainer.Building;
    set => BuildingTooltipContainer.Building = value;
  }

  public Bush? Bush
  {
    get => BushTooltipContainer.Bush;
    set => BushTooltipContainer.Bush = value;
  }


  protected override void DrawSelf(SpriteBatch spriteBatch, int positionX, int positionY)
  {
    DrawContainerBox(spriteBatch, positionX, positionY);
  }

  public void Reset()
  {
    CropTooltipContainer.Crop = null;
    MachineTooltipContainer.Machine = null;
    BuildingTooltipContainer.Building = null;
    CropStatusContainer.HoeDirt = null;
    CropStatusContainer.Tree = null;
    WildTreeContainer.Tree = null;
    FruitTreeContainer.FruitTree = null;
    BushTooltipContainer.Bush = null;
  }
}
