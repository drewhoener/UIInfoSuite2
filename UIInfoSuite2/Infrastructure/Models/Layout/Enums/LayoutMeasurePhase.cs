namespace UIInfoSuite2.Infrastructure.Models.Layout.Enums;

public enum LayoutMeasurePhase
{
  Initial,    // First pass - measure minimal content sizes
  Constrained // Second pass - measure with constraints from parent/siblings
}
