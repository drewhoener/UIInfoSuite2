using UIInfoSuite2.Infrastructure.Models.Layout;
using UIInfoSuite2.Infrastructure.Models.Layout.Measurement;

namespace UIInfoSuite2.Infrastructure.Models.Tooltip.ExpBar;

internal class ExpBarPercentageBar : LayoutElement
{
  protected override Dimensions MeasureContent()
  {
    return Dimensions.Empty;
  }
}

internal class ExpBarComponent : LayoutDom
{
  private readonly ExpBarPercentageBar _percentageBar;
}
