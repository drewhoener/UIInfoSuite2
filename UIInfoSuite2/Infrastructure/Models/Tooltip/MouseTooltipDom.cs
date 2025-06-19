using Microsoft.Xna.Framework.Graphics;
using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip;

internal class MouseTooltipDom : LayoutDom
{
  public MouseTooltipDom()
  {
    CropTooltipContainer = new CropTooltipContainer();
    MachineTooltipContainer = new MachineTooltipContainer();
    BuildingTooltipContainer = new BuildingTooltipContainer();
    CropStatusContainer = new CropStatusContainer();
    WildTreeContainer = new WildTreeTooltipContainer();
    FruitTreeContainer = new FruitTreeTooltipContainer();

    Margin.SetAll(5);
    Padding.SetAll(15);

    AddChildren(
      CropTooltipContainer,
      WildTreeContainer,
      FruitTreeContainer,
      CropStatusContainer,
      MachineTooltipContainer,
      BuildingTooltipContainer
    );
  }

  public CropTooltipContainer CropTooltipContainer { get; }
  public WildTreeTooltipContainer WildTreeContainer { get; }

  public FruitTreeTooltipContainer FruitTreeContainer { get; }

  public CropStatusContainer CropStatusContainer { get; }

  public MachineTooltipContainer MachineTooltipContainer { get; }
  public BuildingTooltipContainer BuildingTooltipContainer { get; }

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
  }
}
