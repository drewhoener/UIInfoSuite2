# Patches

This directory contains Harmony patches for standalone behavior not directly tied to a module.
If you're looking for patched behavior tied to a module, you can likely find it alongside the module wherever it
implements `IPatchable`.

## High-Level Overview

### Extensible Item Tooltips

Extensible tooltips patch into `IClickableMenu#drawHoverText` and create hook points for modules to add information to
different parts of the tooltip.
Container management and drawing is implemented in `TooltipExtensionRegistry`.

#### Other Surface Area

* **Item**: Override `getDescriptionWidth` return values to make sure the tooltips grow to fill the container if its
  width is expanded by an extension container.

* **ShopMenu**: Transpile behavior in `performHoverAction` and `draw` to make sure the tooltip registry provides the
  correct hover item to extensions. See the comments in `ShopMenu_Patches.cs` for more details.

### Events

* **PatchBushShakeItem**: Patch after the "Tea Bush" case in `Bush#shake` to fire an event that notifies of a change in
  the bush's contents. Used to notify `ObjectInfoModule` that the bush has shaken, and it might need to re-draw the
  tooltip to show new information.

* **PatchMasteryXpGain**: Patch after `MasteryXpGain` to fire an event that notifies of a change in the player's mastery
  xp. Used to notify the Experience Bar that the player's mastery xp has changed.

* **PatchRenderingMenuContentStep**: Patch the middle of different menu classes to fire an event after the background is
  drawn, but BEFORE the menu and mouse are drawn. This behavior is useful since if you use a `RenderingEvent`, your
  content will be under the background, but if you use a `RenderedEvent`, your content will be above the mouse cursor.
  We want our stuff right in the middle. This behavior isn't used for `GameMenu` when `BetterGameMenu` is installed,
  since it provides an overlay system.
