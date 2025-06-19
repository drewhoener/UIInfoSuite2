using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace UIInfoSuite2.Infrastructure.Interfaces;

/// <summary>
///   Defines the interface for tooltip components that can be rendered and laid out.
/// </summary>
internal interface ITooltipComponent
{
  /// <summary>
  ///   Gets or sets the offset from the parent component's position where this component should be rendered.
  /// </summary>
  Vector2 LastOffset { get; protected internal set; }

  /// <summary>
  ///   Gets the Margin for this component
  /// </summary>
  Vector2 Margin { get; }

  /// <summary>
  ///   Draws the component at the specified position, moving the rendered position within the safe area
  /// </summary>
  /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
  /// <param name="positionX">The x position to draw at</param>
  /// <param name="positionY">The y position to draw at</param>
  void DrawSafely(SpriteBatch spriteBatch, int positionX = -1, int positionY = -1);

  /// <summary>
  ///   Draws the component at the specified position.
  /// </summary>
  /// <param name="spriteBatch">The SpriteBatch to use for drawing.</param>
  /// <param name="positionX">The x position to draw at</param>
  /// <param name="positionY">The y position to draw at</param>
  void Draw(SpriteBatch spriteBatch, int positionX, int positionY);

  /// <summary>
  ///   Represents the dimensions of the entire component, including external margin and internal padding.
  /// </summary>
  /// <returns></returns>
  Vector2 GetDimensions();

  /// <summary>
  ///   Represents the dimensions of the content inside the component. Includes spacing between elements.
  /// </summary>
  /// <returns></returns>
  Vector2 GetContentDimensions();

  /// <summary>
  ///   Updates the component's state.
  /// </summary>
  void Update();
}
