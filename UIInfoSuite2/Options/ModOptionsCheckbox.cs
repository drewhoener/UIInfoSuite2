using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;
using System;

namespace UIInfoSuite2.Options
{
    internal class ModOptionsCheckbox : ModOptionsValueElement<bool>
    {
        private bool _isChecked;
        private readonly Action<bool> _setOption;
        private bool CanClick => _parent is not ModOptionsCheckbox parentCheckbox || parentCheckbox._isChecked;

        public ModOptionsCheckbox(
            string label,
            int whichOption,
            Action<bool>? onValueUpdate,
            Func<bool> getInitialValue,
            Action<bool> setOption,
            ModOptionsElement parent = null)
            : this(new OptionStringWrapper(label, false),
                whichOption,
                (_, b) => onValueUpdate?.Invoke(b),
                getInitialValue,
                setOption,
                parent
            )
        {
        }

        public ModOptionsCheckbox(
            OptionStringWrapper label,
            int whichOption,
            Action<string, bool>? onValueUpdate,
            Func<bool> getInitialValue,
            Action<bool> setOption,
            ModOptionsElement parent = null)
            : base(onValueUpdate, label, whichOption, parent)
        {
            _setOption = setOption;
            _isChecked = getInitialValue();
        }

        public override bool GetValue()
        {
            return _isChecked;
        }

        public override void SetValue(bool newValue)
        {
            _isChecked = newValue;
            _setOption(newValue);
            OnValueUpdate?.Invoke(Label.Str, _isChecked);
        }

        public override void ReceiveLeftClick(int x, int y)
        {
            if (!CanClick)
                return;
            Game1.playSound("drumkit6");
            base.ReceiveLeftClick(x, y);
            SetValue(!_isChecked);
        }

        public override void Draw(SpriteBatch batch, int slotX, int slotY)
        {
            var sourceRect = _isChecked ? OptionsCheckbox.sourceRectChecked : OptionsCheckbox.sourceRectUnchecked;
            batch.Draw(
                Game1.mouseCursors,
                new Vector2(slotX + Bounds.X, slotY + Bounds.Y),
                sourceRect,
                Color.White * (CanClick ? 1f : 0.33f),
                0.0f,
                Vector2.Zero,
                Game1.pixelZoom,
                SpriteEffects.None,
                0.4f
            );
            base.Draw(batch, slotX, slotY);
        }

        public override Point? GetRelativeSnapPoint(Rectangle slotBounds)
        {
            // Based on the value calculated in OptionsPage.snapCursorToCurrentSnappedComponent
            return new Point(Bounds.X + 16, Bounds.Y + 13);
        }
    }
}
