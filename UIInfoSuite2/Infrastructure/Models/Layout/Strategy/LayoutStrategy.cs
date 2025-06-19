// using System;
// using UIInfoSuite2.Infrastructure.Models.Layout.Enums;
//
// namespace UIInfoSuite2.Infrastructure.Models.Layout.Strategy;
//
// internal abstract class LayoutStrategy(LayoutElement owner)
// {
//   protected readonly LayoutElement Owner = owner;
//
//   // For measuring the element's own content (text, sprites, etc)
//   public abstract Dimensions MeasureOwnContent(int? widthConstraint = null);
//
//   // For arranging the element's own content within its bounds
//   public abstract void ArrangeOwnContent();
//
//   // For measuring child elements (only used by container strategies)
//   public virtual Dimensions MeasureChildren(int? widthConstraint = null) => Dimensions.Empty;
//
//   // For arranging child elements (only used by container strategies)
//   public virtual void ArrangeChildren() { }
//
//   // Combines own content and children measurements
//   public Dimensions MeasureContent()
//   {
//
//     // First pass - measure without constraints to get natural sizes
//     var initialSize = new Dimensions(
//       Math.Max(MeasureOwnContent().Width, MeasureChildren().Width),
//       Math.Max(MeasureOwnContent().Height, MeasureChildren().Height)
//     );
//
//     // Second pass - measure with width constraint
//     var parentWidth = Owner.Parent?.ContentSize.Width;
//     return new Dimensions(
//       Math.Max(MeasureOwnContent(parentWidth).Width, MeasureChildren(parentWidth).Width),
//       Math.Max(MeasureOwnContent(parentWidth).Height, MeasureChildren(parentWidth).Height)
//     );
//   }
// }
//
// // Base strategy for elements with just content
// internal abstract class ElementLayoutStrategy(LayoutElement owner) : LayoutStrategy(owner)
// {
//   // Override with NotSupportedException or return empty to make it clear
//   // these strategies don't handle children
//   public sealed override Dimensions MeasureChildren() =>
//     throw new NotSupportedException("Element strategies do not support child measurement");
//
//   public sealed override void ArrangeChildren() =>
//     throw new NotSupportedException("Element strategies do not support child arrangement");
// }
//
// // Base strategy for containers
// internal abstract class ContainerLayoutStrategy : LayoutStrategy
// {
//   protected ContainerLayoutStrategy(LayoutContainer owner) : base(owner) { }
//
//   // Make it clear these must be implemented for containers
//   public abstract override Dimensions MeasureChildren();
//   public abstract override void ArrangeChildren();
//
//   // Most containers don't have their own content
//   public override Dimensions MeasureOwnContent() => Dimensions.Empty;
//   public override void ArrangeOwnContent() { }
// }


